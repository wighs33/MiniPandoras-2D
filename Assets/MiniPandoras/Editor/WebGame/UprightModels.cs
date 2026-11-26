using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 맵 하나에서 그림 한 장 대신 세우는 모형들(집·가구·통·성벽·배 …). 맵 폴더의 모형 묶음(<see cref="ModelSheet"/>)을 세운 것마다 단다.
    /// 메시와 재질은 Art/Models에 둔다.
    /// </summary>
    internal sealed class UprightModels
    {
        private const string Folder = "Assets/MiniPandoras/Art/Models";
        private const float ArtPixelsPerDot = 2f;
        // 바닥 그림 대신 까는 모형(picture_model.GROUND)
        private const string GroundName = "ground";

        private readonly ModelSheet sheet;

        private UprightModels(ModelSheet sheet)
        {
            this.sheet = sheet;
        }

        /// <summary>새로 들여오기를 시작할 때 지난번에 구운 것을 지운다.</summary>
        internal static void Reset()
        {
            AssetDatabase.DeleteAsset(Folder);
        }

        /// <summary>맵 폴더의 모형들. 모형이 없는 맵이면 비어 있다.</summary>
        internal static UprightModels Load(string folder)
        {
            return new UprightModels(ModelSheet.Load(folder, Folder));
        }

        internal bool Has(UprightData upright)
        {
            return sheet.Has(upright.model);
        }

        /// <summary>바닥 자체가 조각인 맵(돌뱀 머리 위)인지. 그 맵은 바닥 그림 대신 <see cref="AddGround"/>를 깐다.</summary>
        internal bool HasGround => sheet.Has(GroundName);

        /// <summary>모형의 재질(면마다 밝기가 다른 _FACE_LIT)로 그리는지. 높이대로 일으킨 바닥 메시와 가른다.</summary>
        internal static bool IsModel(MeshRenderer renderer)
        {
            return renderer.sharedMaterial != null && renderer.sharedMaterial.IsKeywordEnabled("_FACE_LIT");
        }

        internal void AddGround(Transform root)
        {
            AddRenderer(WebGameImporter.Child(root, "Ground"), GroundName);
        }

        /// <summary>세운 것의 모형을 child에 단다: child를 발밑에 두고 메시를 그린다.</summary>
        internal void Add(Transform child, UprightData upright, MapRelief relief)
        {
            // 그림 자리(left·top, 그림 px)에 기준점(그림 점)을 더하면 발밑이다.
            Vector2 pivot = sheet.PivotOf(upright.model);
            var dots = new Vector2(upright.left / ArtPixelsPerDot + pivot.x, -(upright.top / ArtPixelsPerDot + pivot.y));
            child.localPosition = relief.Lift(dots / MapRelief.DotsPerTile);
            AddRenderer(child, upright.model);
        }

        private void AddRenderer(Transform child, string model)
        {
            child.gameObject.AddComponent<MeshFilter>().sharedMesh = sheet.MeshOf(model);
            var renderer = child.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = sheet.Material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }
}
