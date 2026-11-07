using UnityEngine;

namespace MiniPandoras.Dialogue
{
    /// <summary>대사의 말투. 웹게임 LineStyle과 같다.</summary>
    public enum LineStyle
    {
        Say,
        /// <summary>속마음. 하늘색 글씨.</summary>
        Thought,
        /// <summary>외침. 굵은 글씨, 더 빨리 찍힌다.</summary>
        Shout,
        /// <summary>내레이션. 화자 이름도 얼굴도 없다.</summary>
        Narration,
    }

    /// <summary>
    /// 대화창 위에 크게 세우는 인물 일러스트. 얼굴 칸(가운데·한 변, 원본 픽셀)으로 인물마다 크기와 높이를 맞춘다.
    /// Whole은 그림 전체가 작은 짐승 하나라서(랑) 얼굴 대신 대화창 위에 앉힌다.
    /// </summary>
    public readonly struct Figure
    {
        public Figure(Sprite image, Vector2 faceCenter, float faceSize, bool whole, bool lead)
        {
            Image = image;
            FaceCenter = faceCenter;
            FaceSize = faceSize;
            Whole = whole;
            Lead = lead;
        }

        public Sprite Image { get; }
        /// <summary>그림 왼쪽 위에서 잰 얼굴 가운데(원본 픽셀, 아래가 +).</summary>
        public Vector2 FaceCenter { get; }
        public float FaceSize { get; }
        public bool Whole { get; }
        /// <summary>주인공이면 왼쪽 자리, 나머지는 오른쪽 자리에 선다.</summary>
        public bool Lead { get; }
    }

    /// <summary>대화창에 띄울 대사 한 줄.</summary>
    public readonly struct DialogueLine
    {
        public DialogueLine(string speakerId, string name, Color nameColor, Sprite portrait, Figure? figure, string text, LineStyle style)
        {
            SpeakerId = speakerId;
            Name = name;
            NameColor = nameColor;
            Portrait = portrait;
            Figure = figure;
            Text = text;
            Style = style;
        }

        public string SpeakerId { get; }
        public string Name { get; }
        public Color NameColor { get; }
        public Sprite Portrait { get; }
        public Figure? Figure { get; }
        public string Text { get; }
        public LineStyle Style { get; }

        /// <summary>같은 대사를 인물 일러스트 없이 띄운다.</summary>
        public DialogueLine WithoutFigure()
        {
            return new DialogueLine(SpeakerId, Name, NameColor, Portrait, null, Text, Style);
        }

        public static DialogueLine Narration(string text)
        {
            return new DialogueLine(null, null, Color.white, null, null, text, LineStyle.Narration);
        }
    }
}
