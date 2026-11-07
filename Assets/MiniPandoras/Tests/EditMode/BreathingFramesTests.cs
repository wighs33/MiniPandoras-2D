using System.Collections.Generic;
using System.IO;
using MiniPandoras.Characters;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>구운 숨쉬는 그림이 서 있는 그림과 겹치고, 머리를 건드리지 않는지 본다.</summary>
    public sealed class BreathingFramesTests
    {
        private const string SheetFolder = "Assets/MiniPandoras/Data/Characters";
        // 가장 위에서 바뀌는 인물도 몸 높이의 22.9%부터다. 머리가 큰 인물은 얼굴이 이보다 아래까지 오지만, 모두에게 맞는 선을 쓴다.
        private const float UntouchedHeadShare = 0.2f;
        private static readonly Facing[] Facings = { Facing.Down, Facing.Right, Facing.Up };

        [Test]
        public void MainCastBreathesInEveryDirection()
        {
            foreach (string id in new[] { "hero", "diel", "luna" })
            {
                CharacterSheet sheet = Load(id);
                foreach (Facing facing in Facings)
                {
                    Assert.That(sheet.BreathFramesFor(facing).Count, Is.EqualTo(3), $"{id} {facing}");
                }
            }
        }

        [Test]
        public void BreathFramesSitExactlyOnTheStandingFrame()
        {
            foreach (CharacterSheet sheet in BreathingSheets())
            {
                foreach (Facing facing in Facings)
                {
                    Sprite standing = sheet.FramesFor(facing)[0];
                    foreach (Sprite breath in sheet.BreathFramesFor(facing))
                    {
                        Assert.That(breath.rect.size, Is.EqualTo(standing.rect.size), breath.name);
                        Assert.That(breath.pivot, Is.EqualTo(standing.pivot), $"{sheet.Id}: 기준점이 다르면 숨쉴 때 몸이 밀린다");
                    }
                }
            }
        }

        [Test]
        public void HeadStaysPixelForPixel()
        {
            foreach (CharacterSheet sheet in BreathingSheets())
            {
                foreach (Facing facing in Facings)
                {
                    Texture2D standing = Read(sheet.FramesFor(facing)[0]);
                    foreach (Sprite breath in sheet.BreathFramesFor(facing))
                    {
                        Texture2D baked = Read(breath);
                        Assert.That(ChangedRowsInHead(standing, baked), Is.Zero, $"{sheet.Id} {breath.name}");
                        Object.DestroyImmediate(baked);
                    }

                    Object.DestroyImmediate(standing);
                }
            }
        }

        private static int ChangedRowsInHead(Texture2D standing, Texture2D baked)
        {
            Color32[] before = standing.GetPixels32();
            Color32[] after = baked.GetPixels32();
            int width = standing.width;
            int bodyTop = TopVisibleRow(before, width);
            int bodyBottom = BottomVisibleRow(before, width);
            int headEnd = bodyTop - Mathf.CeilToInt((bodyTop - bodyBottom + 1) * UntouchedHeadShare);
            int changed = 0;
            for (int row = bodyTop; row > headEnd; row--)
            {
                for (int x = 0; x < width; x++)
                {
                    int at = row * width + x;
                    if (!before[at].Equals(after[at]))
                    {
                        changed++;
                        break;
                    }
                }
            }

            return changed;
        }

        // 텍스처의 줄 번호는 아래에서 위로 센다.
        private static int TopVisibleRow(Color32[] pixels, int width)
        {
            int row = pixels.Length / width - 1;
            while (!RowHasPixels(pixels, width, row))
            {
                row--;
            }

            return row;
        }

        private static int BottomVisibleRow(Color32[] pixels, int width)
        {
            int row = 0;
            while (!RowHasPixels(pixels, width, row))
            {
                row++;
            }

            return row;
        }

        private static bool RowHasPixels(Color32[] pixels, int width, int row)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[row * width + x].a > 0)
                {
                    return true;
                }
            }

            return false;
        }

        // 임포터가 읽기를 꺼 둔 텍스처라 PNG를 직접 읽는다.
        private static Texture2D Read(Sprite sprite)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
            return texture;
        }

        private static CharacterSheet Load(string id)
        {
            return AssetDatabase.LoadAssetAtPath<CharacterSheet>($"{SheetFolder}/{id}.asset");
        }

        private static IEnumerable<CharacterSheet> BreathingSheets()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterSheet", new[] { SheetFolder }))
            {
                var sheet = AssetDatabase.LoadAssetAtPath<CharacterSheet>(AssetDatabase.GUIDToAssetPath(guid));
                if (sheet.BreathFramesFor(Facing.Down).Count > 0)
                {
                    yield return sheet;
                }
            }
        }
    }
}
