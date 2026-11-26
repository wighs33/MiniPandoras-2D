using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 맵 바닥 그림을 높이(<see cref="MapRelief"/>)대로 일으킨 메시. 위에서 비스듬히 본 그림이라 높이는 열마다 북쪽에서 남쪽으로 읽힌다:
    /// 한 열은 높이가 고르게 이어지는 조각(평평한 바닥·벽 윗면, 세운 벽면, 비탈)의 줄이다. 이웃 열의 조각 줄이 같고 경계가 조금만 어긋나면
    /// 두 열 사이 선에서 경계를 가운데로 맞춰 이어 붙인다. 그래서 둥근 벽·굽은 동굴 벽이 점 열마다 계단지지 않고 매끈하게 휜다.
    /// 조각 줄이 다르거나 크게 어긋나는 곳(방 사이 벽의 모서리)은 끊고, 높은 쪽 가장자리를 아래로 내려 옆면을 메운다.
    /// 그림은 텍스처 좌표로 원래 자리를 그대로 짚으므로 도트가 늘거나 줄지 않는다. 메운 면(그림에 없는 옆면·끝면·뒷면)에는 높은 쪽 윗면의 그림을
    /// 가장자리에서 안쪽으로 내려간 깊이만큼 이어 붙이고 그늘진 듯 어둡게 칠한다.
    /// 그림이 투명한 곳(떠 있는 섬 밖의 허공)은 메시를 만들지 않는다. 만들면 가장자리가 이웃 점의 색을 섞어 읽어 허공에 가는 선이 생긴다.
    /// </summary>
    internal static class ReliefMesh
    {
        // 그림에 없는 옆면(동·서를 보는 면), 남쪽을 보는 끝면, 북쪽을 보는 뒷면의 밝기.
        private static readonly Color32 SideShade = new Color32(110, 106, 104, 255);
        private static readonly Color32 FrontShade = new Color32(150, 146, 142, 255);
        private static readonly Color32 BackShade = new Color32(70, 68, 66, 255);
        private static readonly Color32 Painted = new Color32(255, 255, 255, 255);
        // 높이는 0.5점 단위라 이보다 작은 차이는 같은 높이다.
        private const float Same = 0.01f;
        // 이웃 두 열의 조각 경계가 이만큼(점) 안에서 어긋나면 한 곡면으로 이어 붙인다. 더 어긋나면 꺾인 모서리로 끊는다.
        private const float MaxSlant = 12f;

        /// <param name="painted">점마다 그림이 칠해졌는지(투명하지 않은지). 점 (x, 줄)은 [줄 × 너비 + x]다.</param>
        public static Mesh Build(MapRelief relief, bool[] painted, string name)
        {
            var columns = new Columns(relief, painted);
            var quads = new QuadList(relief.Width, relief.Height);
            AddColumns(columns, quads);
            AddSideWalls(relief, columns, quads);
            return quads.ToMesh(name);
        }

        /// <summary>
        /// 열(또는 똑같은 열이 이어진 구간)마다 조각을 네모로 붙이고, 한 열 안에서 위아래 조각의 높이가 끊기는 곳은 끝면·뒷면으로 메운다.
        /// </summary>
        private static void AddColumns(Columns columns, QuadList quads)
        {
            int spanStart = 0;
            for (int x = 1; x <= columns.Width; x++)
            {
                if (x < columns.Width && columns.IsPlain(x) && columns.IsPlain(spanStart) && columns.SameAs(spanStart, x))
                {
                    continue;
                }

                AddSpan(columns, spanStart, x, quads);
                spanStart = x;
            }
        }

        /// <summary>열 x0~x1(끝 열은 들지 않는다)은 모두 같은 조각 줄이다. 왼쪽 선과 오른쪽 선의 경계 줄로 조각을 붙인다.</summary>
        private static void AddSpan(Columns columns, int x0, int x1, QuadList quads)
        {
            List<Piece> pieces = columns.PiecesOf(x0);
            float[] left = columns.LeftEdge(x0);
            float[] right = columns.RightEdge(x1 - 1);
            for (int i = 0; i < pieces.Count; i++)
            {
                Piece piece = pieces[i];
                if (columns.AnyPainted(x0, x1, piece.StartRow, piece.EndRow))
                {
                    quads.AddPainted(x0, x1, left[i], left[i + 1], right[i], right[i + 1], piece.Top, piece.Bottom);
                }

                float below = i + 1 < pieces.Count ? pieces[i + 1].Top : 0f;
                bool lastRow = i + 1 == pieces.Count;
                if (Mathf.Abs(piece.Bottom - below) < Same || (lastRow && piece.Bottom <= 0f))
                {
                    continue;
                }

                // 북쪽 조각이 높으면 카메라를 보는 끝면, 남쪽이 높으면 보이지 않는 뒷면이다. 맵 남쪽 끝의 높은 곳은 땅(0)까지 내린다.
                bool front = piece.Bottom > below;
                int edgeRow = front ? piece.EndRow - 1 : piece.EndRow;
                if (!columns.AnyPainted(x0, x1, edgeRow, edgeRow + 1))
                {
                    continue;
                }

                quads.AddEnd(x0, x1, left[i + 1], right[i + 1], Mathf.Max(piece.Bottom, below), Mathf.Min(piece.Bottom, below), front);
            }
        }

        /// <summary>
        /// 끊긴 열 사이 선(맵 왼쪽·오른쪽 끝 포함)에서 동·서 이웃 점의 높이가 다르면 높은 쪽 가장자리를 낮은 쪽의 밑바닥까지 수직으로 내린다
        /// (방 사이 벽의 옆면 …). 맵 끝의 높은 곳은 땅(0)까지 내린다.
        /// </summary>
        private static void AddSideWalls(MapRelief relief, Columns columns, QuadList quads)
        {
            for (int line = 0; line <= relief.Width; line++)
            {
                if (columns.IsJoined(line))
                {
                    continue;
                }

                SideRun run = SideRun.None;
                for (int row = 0; row <= relief.Height; row++)
                {
                    SideRun next = row < relief.Height ? SideAt(relief, columns, line, row) : SideRun.None;
                    if (run.Continues(next, row))
                    {
                        continue;
                    }

                    if (!run.IsNone)
                    {
                        quads.AddSide(line, run, row);
                    }

                    run = next;
                }
            }
        }

        private static SideRun SideAt(MapRelief relief, Columns columns, int line, int row)
        {
            bool hasLeft = line > 0;
            bool hasRight = line < relief.Width;
            float leftTop = hasLeft ? relief.TopOf(line - 1, row) : 0f;
            float leftBottom = hasLeft ? relief.BottomOf(line - 1, row) : 0f;
            float rightTop = hasRight ? relief.TopOf(line, row) : 0f;
            float rightBottom = hasRight ? relief.BottomOf(line, row) : 0f;
            if (Mathf.Abs(leftTop - rightTop) < Same && Mathf.Abs(leftBottom - rightBottom) < Same)
            {
                return SideRun.None;
            }

            bool rightIsHigher = rightTop + rightBottom > leftTop + leftBottom;
            if ((rightIsHigher && !hasRight) || (!rightIsHigher && !hasLeft))
            {
                // 맵 밖이 더 높은 곳(맵 끝의 물 …)은 메우지 않는다.
                return SideRun.None;
            }

            int x = rightIsHigher ? line : line - 1;
            if (!columns.IsPainted(x, row))
            {
                return SideRun.None;
            }

            float floorBelow = rightIsHigher ? (hasLeft ? columns.BaseOf(line - 1, row) : 0f) : (hasRight ? columns.BaseOf(line, row) : 0f);
            float top = relief.TopOf(x, row);
            float drop = relief.DropOf(x, row);
            // 바닥 높이 아래로 매달린 면(떠 있는 섬의 바위, 바깥 뱃전)은 얇은 판이라 옆면이 없다. 그 곁의 바닥 높이 평평한 곳도
            // 덩어리가 아니라(허공·갑판) 내려 메울 것이 없다.
            bool lowerHangs = (rightIsHigher ? leftTop : rightTop) <= Same && (rightIsHigher ? leftTop - leftBottom : rightTop - rightBottom) > 0.75f;
            bool higherHangs = drop > 0.75f && top <= Same;
            if (higherHangs || (lowerHangs && drop == 0f && Mathf.Abs(top) < Same))
            {
                return SideRun.None;
            }
            // 세운 면이 제 밑바닥까지 내려가면 같은 면 위의 선이라 메울 것이 없다.
            if (drop > 0.75f && Mathf.Abs(columns.BaseOf(x, row) - floorBelow) < Same)
            {
                return SideRun.None;
            }

            if (top <= floorBelow + Same && top - drop <= floorBelow + Same)
            {
                return SideRun.None;
            }

            // 낮은 쪽이 벽면이고 높은 쪽 열에도 바로 아래 벽면이 있으면, 이 옆면은 크게 꺾인 벽면의 한 조각이다.
            int lower = rightIsHigher ? line - 1 : line;
            bool lowerIsFace = lower >= 0 && lower < relief.Width && relief.DropOf(lower, row) > 0.75f;
            (int faceBottom, float faceBase) = lowerIsFace ? columns.FaceBelow(x, row) : (-1, 0f);
            return new SideRun(rightIsHigher, row, columns.StartOf(x, row), top, drop, floorBelow, faceBottom, faceBase);
        }

        /// <summary>한 열 안에서 높이가 고르게 이어지는 점들(한 조각).</summary>
        private readonly struct Piece
        {
            public Piece(int startRow, int endRow, float top, float drop)
            {
                StartRow = startRow;
                EndRow = endRow;
                Top = top;
                Drop = drop;
            }

            public int StartRow { get; }
            /// <summary>조각 아래 끝(이 줄은 들지 않는다).</summary>
            public int EndRow { get; }
            /// <summary>조각 윗변의 높이.</summary>
            public float Top { get; }
            public float Drop { get; }
            /// <summary>조각 아랫변의 높이.</summary>
            public float Bottom => Top - Drop * (EndRow - StartRow);

            /// <summary>같은 모양의 조각(어긋난 줄은 보지 않는다): 같은 기울기로 같은 높이에서 같은 높이까지.</summary>
            public bool SameShape(Piece other)
            {
                return Drop == other.Drop && Mathf.Abs(Top - other.Top) < Same && Mathf.Abs(Bottom - other.Bottom) < Same;
            }
        }

        /// <summary>
        /// 열마다 조각 줄, 점마다 그 점이 든 조각의 시작 줄·밑바닥 높이, 그리고 열 사이 선마다 이어 붙이는지와 양쪽 가장자리의 경계 줄.
        /// </summary>
        private sealed class Columns
        {
            private readonly List<Piece>[] pieces;
            private readonly float[][] own;
            private readonly float[][] leftEdge;
            private readonly float[][] rightEdge;
            private readonly bool[] joined;
            private readonly int[] start;
            private readonly float[] floor;
            private readonly bool[] painted;

            public Columns(MapRelief relief, bool[] painted)
            {
                Width = relief.Width;
                this.painted = painted;
                pieces = new List<Piece>[Width];
                own = new float[Width][];
                start = new int[Width * relief.Height];
                floor = new float[Width * relief.Height];
                for (int x = 0; x < Width; x++)
                {
                    pieces[x] = Split(relief, x);
                    own[x] = Boundaries(pieces[x]);
                }

                joined = new bool[Width + 1];
                for (int line = 1; line < Width; line++)
                {
                    joined[line] = Joins(line - 1, line);
                }

                leftEdge = new float[Width][];
                rightEdge = new float[Width][];
                for (int x = 0; x < Width; x++)
                {
                    leftEdge[x] = joined[x] ? Middle(own[x - 1], own[x]) : own[x];
                    rightEdge[x] = joined[x + 1] ? Middle(own[x], own[x + 1]) : own[x];
                }
            }

            public int Width { get; }

            public List<Piece> PiecesOf(int x)
            {
                return pieces[x];
            }

            /// <summary>열 사이 선(열 line − 1과 line 사이)에서 두 열을 이어 붙였는지. 맵 끝 선은 늘 끊겨 있다.</summary>
            public bool IsJoined(int line)
            {
                return joined[line];
            }

            /// <summary>열 x 왼쪽 선의 조각 경계 줄(조각 수 + 1개).</summary>
            public float[] LeftEdge(int x)
            {
                return leftEdge[x];
            }

            public float[] RightEdge(int x)
            {
                return rightEdge[x];
            }

            /// <summary>양쪽 선의 경계가 제 경계 그대로인 열(곡면에 걸치지 않아 옆 열과 한 네모로 묶을 수 있다).</summary>
            public bool IsPlain(int x)
            {
                return Equal(leftEdge[x], own[x]) && Equal(rightEdge[x], own[x]);
            }

            /// <summary>두 열의 조각 줄과 경계가 똑같은지.</summary>
            public bool SameAs(int a, int b)
            {
                if (pieces[a].Count != pieces[b].Count || !Equal(own[a], own[b]))
                {
                    return false;
                }

                for (int i = 0; i < pieces[a].Count; i++)
                {
                    if (!pieces[a][i].SameShape(pieces[b][i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int StartOf(int x, int row)
            {
                return start[row * Width + x];
            }

            public bool IsPainted(int x, int row)
            {
                return painted[row * Width + x];
            }

            /// <summary>점 열 x0~x1, 줄 row0~row1(끝은 들지 않는다)에 칠한 점이 하나라도 있는지.</summary>
            public bool AnyPainted(int x0, int x1, int row0, int row1)
            {
                for (int row = row0; row < row1; row++)
                {
                    for (int x = x0; x < x1; x++)
                    {
                        if (painted[row * Width + x])
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>그 점이 든 조각의 아래 끝 높이(벽면이면 그 벽이 선 바닥, 평평하면 그 높이).</summary>
            public float BaseOf(int x, int row)
            {
                return floor[row * Width + x];
            }

            /// <summary>
            /// 그 점이 든 평평한 조각 바로 아래(남쪽)에 이어지는 벽면이 있으면 그 벽면의 아래 끝 줄과 그 높이. 없으면 (-1, 0).
            /// </summary>
            public (int bottomRow, float baseHeight) FaceBelow(int x, int row)
            {
                List<Piece> column = pieces[x];
                int first = StartOf(x, row);
                for (int i = 0; i + 1 < column.Count; i++)
                {
                    if (column[i].StartRow != first)
                    {
                        continue;
                    }

                    Piece face = column[i + 1];
                    if (column[i].Drop == 0f && face.Drop > 0.75f && Mathf.Abs(face.Top - column[i].Top) < Same)
                    {
                        return (face.EndRow, face.Bottom);
                    }

                    break;
                }

                return (-1, 0f);
            }

            /// <summary>
            /// 두 열이 한 곡면인지: 조각 줄이 같은 모양이고 경계가 MaxSlant 안에서만 어긋난다.
            /// 아래가 끊긴 채 매달린 면(떠 있는 섬의 바위)이 있는 열은 잇지 않는다. 이으면 어긋난 만큼 바위 점이 그 아래 허공 판에 납작하게 그려진다.
            /// </summary>
            private bool Joins(int a, int b)
            {
                if (pieces[a].Count != pieces[b].Count || HasDanglingFace(pieces[a]) || HasDanglingFace(pieces[b]))
                {
                    return false;
                }

                for (int i = 0; i < pieces[a].Count; i++)
                {
                    if (!pieces[a][i].SameShape(pieces[b][i]))
                    {
                        return false;
                    }
                }

                for (int k = 0; k < own[a].Length; k++)
                {
                    if (Mathf.Abs(own[a][k] - own[b][k]) > MaxSlant)
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>바닥 높이 아래로 매달렸는데 그 아래 조각과 높이가 이어지지 않는 벽면이 있는지.</summary>
            private static bool HasDanglingFace(List<Piece> column)
            {
                for (int i = 0; i + 1 < column.Count; i++)
                {
                    Piece face = column[i];
                    if (face.Drop > 0.75f && face.Bottom < -Same && Mathf.Abs(column[i + 1].Top - face.Bottom) > Same)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>한 열을 위에서부터 조각으로 나눈다. 점마다 든 조각의 시작 줄과 밑바닥 높이도 적는다.</summary>
            private List<Piece> Split(MapRelief relief, int x)
            {
                var column = new List<Piece>();
                int first = 0;
                for (int row = 1; row <= relief.Height; row++)
                {
                    bool joins = row < relief.Height && relief.DropOf(x, row) == relief.DropOf(x, first)
                        && Mathf.Abs(relief.TopOf(x, row) - relief.BottomOf(x, row - 1)) < Same;
                    if (joins)
                    {
                        continue;
                    }

                    var piece = new Piece(first, row, relief.TopOf(x, first), relief.DropOf(x, first));
                    column.Add(piece);
                    for (int k = first; k < row; k++)
                    {
                        start[k * Width + x] = first;
                        floor[k * Width + x] = piece.Bottom;
                    }

                    first = row;
                }

                return column;
            }

            private static float[] Boundaries(List<Piece> column)
            {
                var rows = new float[column.Count + 1];
                for (int i = 0; i < column.Count; i++)
                {
                    rows[i] = column[i].StartRow;
                }

                rows[column.Count] = column[column.Count - 1].EndRow;
                return rows;
            }

            private static float[] Middle(float[] a, float[] b)
            {
                var rows = new float[a.Length];
                for (int k = 0; k < a.Length; k++)
                {
                    rows[k] = (a[k] + b[k]) / 2f;
                }

                return rows;
            }

            private static bool Equal(float[] a, float[] b)
            {
                if (a.Length != b.Length)
                {
                    return false;
                }

                for (int k = 0; k < a.Length; k++)
                {
                    if (a[k] != b[k])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>한 세로선을 따라 이어지는 옆면 조각: 같은 쪽의 같은 조각이 같은 높이까지 내려온다.</summary>
        private readonly struct SideRun
        {
            public static readonly SideRun None = new SideRun(false, -1, -1, 0f, 0f, 0f, -1, 0f);

            public SideRun(bool rightIsHigher, int row, int pieceStart, float top, float drop, float floor, int faceBottom, float faceBase)
            {
                RightIsHigher = rightIsHigher;
                StartRow = row;
                PieceStart = pieceStart;
                Top = top;
                Drop = drop;
                Floor = floor;
                FaceBottom = faceBottom;
                FaceBase = faceBase;
            }

            public bool IsNone => StartRow < 0;
            public bool RightIsHigher { get; }
            public int StartRow { get; }
            /// <summary>높은 쪽 점이 든 조각의 시작 줄(조각이 바뀌면 옆면도 끊는다).</summary>
            public int PieceStart { get; }
            /// <summary>첫 줄 윗변에서 높은 쪽 높이.</summary>
            public float Top { get; }
            public float Drop { get; }
            /// <summary>내려가는 바닥 높이.</summary>
            public float Floor { get; }
            /// <summary>꺾인 벽면의 조각이면 그 열 벽면의 아래 끝 줄(그림을 빌려 온다), 아니면 -1.</summary>
            public int FaceBottom { get; }
            /// <summary>그 벽면 아래 끝의 높이.</summary>
            public float FaceBase { get; }

            public bool Continues(SideRun next, int row)
            {
                return !IsNone && !next.IsNone && next.RightIsHigher == RightIsHigher && next.PieceStart == PieceStart
                    && next.Floor == Floor && next.FaceBottom == FaceBottom && Mathf.Abs(next.Top - (Top - Drop * (row - StartRow))) < Same;
            }
        }

        /// <summary>네모들을 모아 메시 하나로. 법선은 모두 땅의 위쪽이라 그림에 칠한 밝기를 그대로 받는다(세운 그림과 같다).</summary>
        private sealed class QuadList
        {
            private readonly int width;
            private readonly int height;
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();
            private readonly List<Color32> colors = new List<Color32>();
            private readonly List<int> triangles = new List<int>();

            public QuadList(int width, int height)
            {
                this.width = width;
                this.height = height;
            }

            /// <summary>그림 그대로 붙는 조각: 점 열 x0~x1, 왼쪽 선의 줄 leftTop~leftBottom, 오른쪽 선의 줄 rightTop~rightBottom, 윗변·아랫변 높이.</summary>
            public void AddPainted(int x0, int x1, float leftTop, float leftBottom, float rightTop, float rightBottom, float top, float bottom)
            {
                Add(MapRelief.Place(x0, leftBottom, bottom), MapRelief.Place(x1, rightBottom, bottom), MapRelief.Place(x0, leftTop, top), MapRelief.Place(x1, rightTop, top),
                    Uv(x0, leftBottom), Uv(x1, rightBottom), Uv(x0, leftTop), Uv(x1, rightTop), Painted);
            }

            /// <summary>세로선 line을 따라 높은 쪽 가장자리(run.StartRow~endRow)를 run.Floor까지 내린 옆면.</summary>
            public void AddSide(int line, SideRun run, int endRow)
            {
                float topAtEnd = run.Top - run.Drop * (endRow - run.StartRow);
                Vector3 upperStart = MapRelief.Place(line, run.StartRow, run.Top);
                Vector3 upperEnd = MapRelief.Place(line, endRow, topAtEnd);
                Vector3 lowerStart = MapRelief.Place(line, run.StartRow + run.Top - run.Floor, run.Floor);
                Vector3 lowerEnd = MapRelief.Place(line, endRow + topAtEnd - run.Floor, run.Floor);
                float inward = run.RightIsHigher ? 1f : -1f;
                if (run.FaceBottom >= 0)
                {
                    // 크게 꺾인 벽면의 조각: 높은 쪽 열 벽면의 같은 높이 그림을 붙인다(벽면과 같은 밝기).
                    float u = line + inward * 0.5f;
                    Vector2 high = Uv(u, run.FaceBottom - (run.Top - run.FaceBase));
                    Vector2 low = Uv(u, run.FaceBottom - (run.Floor - run.FaceBase));
                    Add(lowerEnd, lowerStart, upperEnd, upperStart, low, low, high, high, Painted);
                    return;
                }

                // 아래로 내려간 만큼 높은 쪽 안으로 들어간 자리의 그림을 붙인다. 가장자리 점의 가운데부터 읽어야
                // 바깥(투명한 허공 …) 점과 섞인 반투명 선이 생기지 않는다.
                float edge = line + inward * 0.5f;
                Vector2 uvUpperStart = Uv(edge, run.StartRow);
                Vector2 uvUpperEnd = Uv(edge, endRow);
                Vector2 uvLowerStart = Uv(edge + inward * (run.Top - run.Floor), run.StartRow);
                Vector2 uvLowerEnd = Uv(edge + inward * (topAtEnd - run.Floor), endRow);
                Add(lowerEnd, lowerStart, upperEnd, upperStart, uvLowerEnd, uvLowerStart, uvUpperEnd, uvUpperStart, SideShade);
            }

            /// <summary>
            /// 점 열 x0~x1에서 두 조각 사이 경계(왼쪽 선의 줄 leftRow, 오른쪽 선의 줄 rightRow)의 높은 쪽 가장자리(높이 high)를 low까지 내린
            /// 끝면(front: 북쪽이 높다) 또는 뒷면.
            /// </summary>
            public void AddEnd(int x0, int x1, float leftRow, float rightRow, float high, float low, bool front)
            {
                Vector3 upperLeft = MapRelief.Place(x0, leftRow, high);
                Vector3 upperRight = MapRelief.Place(x1, rightRow, high);
                Vector3 lowerLeft = MapRelief.Place(x0, leftRow + high - low, low);
                Vector3 lowerRight = MapRelief.Place(x1, rightRow + high - low, low);
                // 끝면은 북쪽(높은 쪽) 안으로, 뒷면은 남쪽 안으로 내려간 깊이만큼 들어간 자리의 그림을 붙인다(가장자리 점의 가운데부터).
                float toward = front ? -1f : 1f;
                float edgeLeft = leftRow + toward * 0.5f;
                float edgeRight = rightRow + toward * 0.5f;
                float depth = toward * (high - low);
                Add(lowerLeft, lowerRight, upperLeft, upperRight, Uv(x0, edgeLeft + depth), Uv(x1, edgeRight + depth), Uv(x0, edgeLeft), Uv(x1, edgeRight),
                    front ? FrontShade : BackShade);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                if (vertices.Count > 65535)
                {
                    mesh.indexFormat = IndexFormat.UInt32;
                }

                var normals = new Vector3[vertices.Count];
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = Upright.Up;
                }

                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetColors(colors);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }

            private Vector2 Uv(float x, float row)
            {
                return new Vector2(x / width, 1f - row / height);
            }

            private void Add(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight,
                Vector2 uvBottomLeft, Vector2 uvBottomRight, Vector2 uvTopLeft, Vector2 uvTopRight, Color32 color)
            {
                int first = vertices.Count;
                vertices.Add(bottomLeft);
                vertices.Add(bottomRight);
                vertices.Add(topLeft);
                vertices.Add(topRight);
                uvs.Add(uvBottomLeft);
                uvs.Add(uvBottomRight);
                uvs.Add(uvTopLeft);
                uvs.Add(uvTopRight);
                for (int i = 0; i < 4; i++)
                {
                    colors.Add(color);
                }

                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
                triangles.Add(first + 3);
                triangles.Add(first + 1);
            }
        }
    }
}
