using UnityEditor;

namespace MiniPandoras.Editor.Monsters
{
    /// <summary>
    /// 생성기가 그린 몬스터 시트(Art/PixelGen/Monsters)를 인물 시트로 들여온다. 전투 수치는 적 에셋(Data/Battle/Monsters)이 정본이라 건드리지 않는다.
    /// 그림을 다시 그린 뒤에는 이 메뉴를 돌리고, 새 몬스터면 Refresh Scene Lists로 장면 목록에 시트를 넣는다.
    /// </summary>
    public static class MonsterImporter
    {
        private const string SheetRoot = ArtPaths.RedrawnRoot + "/Monsters";
        private const string SheetFolder = WebGame.WebGameImporter.SheetFolder + "/Monsters";

        [MenuItem("MiniPandoras/Import Monsters")]
        public static void ImportMenu()
        {
            AssetDatabase.Refresh();
            Import();
            AssetDatabase.SaveAssets();
        }

        public static void Import()
        {
            foreach (string folder in AssetDatabase.GetSubFolders(SheetRoot))
            {
                WebGame.WebGameImporter.ImportCharacter(folder, SheetFolder);
            }
        }
    }
}
