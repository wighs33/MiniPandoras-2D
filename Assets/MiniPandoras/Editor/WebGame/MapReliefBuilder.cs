using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 벽·절벽을 그린 맵(relief.png가 있는 맵)의 바닥: 바닥 그림을 높이대로 일으킨 메시(<see cref="ReliefMesh"/>).
    /// 바닥 장식(먼지·양탄자·벽에 건 걸개·판자로 막은 창)은 벽면을 따라 함께 서야 하므로 따로 눕히지 않고 바닥 그림에 겹쳐 굽는다.
    /// 모습(낡은 아지트 ↔ 전성기)마다 장식이 다르면 모습마다 그림을 하나씩 굽고, 메시는 함께 쓴다. 그 바닥은 모습 묶음 아래에 두어 모습과 함께 켜진다.
    /// 그림·메시·재질은 Art/Relief에 둔다.
    /// </summary>
    internal static class MapReliefBuilder
    {
        private const string Folder = "Assets/MiniPandoras/Art/Relief";

        /// <summary>새로 들여오기를 시작할 때 지난번에 구운 것을 지운다.</summary>
        internal static void Reset()
        {
            AssetDatabase.DeleteAsset(Folder);
            EnsureFolder();
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder).Replace('\\', '/'), Path.GetFileName(Folder));
            }
        }

        /// <param name="looks">모습 이름 → 모습 묶음. 모습이 없는 맵은 비어 있다.</param>
        internal static void AddGround(Transform root, MapData data, MapRelief relief, string groundPath, string decalFolder, Dictionary<string, Transform> looks)
        {
            EnsureFolder();
            Mesh mesh = AssetFiles.Save(ReliefMesh.Build(relief, PaintedDots(groundPath, relief), "Relief " + data.id), $"{Folder}/{data.id}.asset");
            DecalData[] shared = System.Array.FindAll(data.decals, decal => MapImporter.IsShared(decal.look));
            if (looks.Count == 0)
            {
                AddRenderer(root, mesh, Paint(data.id, groundPath, decalFolder, shared));
                return;
            }

            foreach (KeyValuePair<string, Transform> look in looks)
            {
                var decals = new List<DecalData>(shared);
                decals.AddRange(System.Array.FindAll(data.decals, decal => decal.look == look.Key));
                AddRenderer(look.Value, mesh, Paint($"{data.id}_{look.Key}", groundPath, decalFolder, decals.ToArray()));
            }
        }

        /// <summary>높이 지도의 점마다 바닥 그림이 칠해졌는지(그 점을 덮는 그림 px 가운데 하나라도 투명하지 않은지).</summary>
        private static bool[] PaintedDots(string groundPath, MapRelief relief)
        {
            var ground = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ground.LoadImage(File.ReadAllBytes(groundPath));
            Color32[] pixels = ground.GetPixels32();
            int width = ground.width;
            int height = ground.height;
            Object.DestroyImmediate(ground);
            int scale = width / relief.Width;
            var painted = new bool[relief.Width * relief.Height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a > 0)
                    {
                        // 텍스처는 아랫줄부터, 높이 지도는 윗줄부터 센다.
                        int row = (height - 1 - y) / scale;
                        painted[row * relief.Width + x / scale] = true;
                    }
                }
            }

            return painted;
        }

        private static void AddRenderer(Transform parent, Mesh mesh, Material material)
        {
            Transform ground = WebGameImporter.Child(parent, "Ground");
            ground.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = ground.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        /// <summary>바닥 그림에 장식을 겹친 그림과 그 그림을 입힌 재질(해를 받는 Sprite Lit 3D의 카드 메시 모드).</summary>
        private static Material Paint(string name, string groundPath, string decalFolder, DecalData[] decals)
        {
            var ground = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ground.LoadImage(File.ReadAllBytes(groundPath));
            Color32[] canvas = ground.GetPixels32();
            int width = ground.width;
            int height = ground.height;
            Object.DestroyImmediate(ground);
            foreach (DecalData decal in decals)
            {
                Overlay(canvas, width, height, $"{decalFolder}/{decal.image}.png", decal.left, decal.top);
            }

            string path = $"{Folder}/{name}.png";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(canvas);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Material material = FieldSprites.CardMeshMaterial(FieldSprites.ImportPicture(path), name);
            if (!FieldSprites.HasHoles(canvas))
            {
                FieldSprites.MakeSolid(material);
            }

            return AssetFiles.Save(material, $"{Folder}/{name}.mat");
        }

        /// <summary>장식 그림을 그림 px 자리(left, top: 왼쪽 위 기준)에 알파로 겹친다.</summary>
        private static void Overlay(Color32[] canvas, int width, int height, string path, int left, int top)
        {
            var decal = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            decal.LoadImage(File.ReadAllBytes(path));
            Color32[] pixels = decal.GetPixels32();
            for (int row = 0; row < decal.height; row++)
            {
                int y = top + row;
                if (y < 0 || y >= height)
                {
                    continue;
                }

                for (int column = 0; column < decal.width; column++)
                {
                    int x = left + column;
                    Color32 over = pixels[(decal.height - 1 - row) * decal.width + column];
                    if (x < 0 || x >= width || over.a == 0)
                    {
                        continue;
                    }

                    int i = (height - 1 - y) * width + x;
                    Color32 under = canvas[i];
                    float a = over.a / 255f;
                    canvas[i] = new Color32((byte)Mathf.Lerp(under.r, over.r, a), (byte)Mathf.Lerp(under.g, over.g, a), (byte)Mathf.Lerp(under.b, over.b, a),
                        (byte)Mathf.Max(under.a, over.a));
                }
            }

            Object.DestroyImmediate(decal);
        }
    }
}
