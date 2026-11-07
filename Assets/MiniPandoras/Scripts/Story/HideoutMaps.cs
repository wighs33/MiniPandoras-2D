using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 힐리아 마을 둘레의 밤과 마을 끝 숲길·낡은 아지트의 준비. 같은 맵을 3장(쫓기는 밤)과 5장(깨어난 뒤)이 함께 쓰고,
    /// 6장 기억 속에서는 같은 지도를 원래 이름으로 보여 주므로 장 코드 밖으로 뺐다.
    /// </summary>
    public static class HideoutMaps
    {
        private static readonly Color PathNight = new Color32(0x0e, 0x14, 0x30, 0xff);
        private static readonly Color HideoutNight = new Color32(0x0a, 0x0c, 0x1c, 0xff);
        private const float TownNightCover = 0.55f;
        private const float HideoutNightCover = 0.5f;

        /// <summary>지금(3·5장)의 숲길과 아지트 이름. 6장 기억 속에서는 같은 지도를 원래 이름으로 보여 준다.</summary>
        public static void UsePresentNames(StoryDirector s)
        {
            s.Field.RenameMap("hideoutPath", T("hideoutMaps.usePresentNames.1"));
            s.Field.RenameMap("hideout", T("hideoutMaps.usePresentNames.2"));
            s.Field.RenameMap("hideoutUpper", T("hideoutMaps.usePresentNames.3"));
            s.Field.RenameMap("hideoutRoom", T("hideoutMaps.usePresentNames.4"));
        }

        /// <summary>힐리아 마을 둘레의 밤(3·5장).</summary>
        public static void NightOverTheTown(StoryDirector s)
        {
            s.Field.SetAmbient(PathNight, TownNightCover);
        }

        /// <summary>병사들에게 쫓기는 밤(2장 끝, 3장). 마을 둘레의 비는 이때만 내린다.</summary>
        public static void RainyNightOverTheTown(StoryDirector s)
        {
            NightOverTheTown(s);
            s.Field.SetWeather(MapWeather.Rain);
        }

        /// <summary>아무도 찾지 않는 밤의 숲길(3·5장).</summary>
        public static void SetupHideoutPathNow(StoryDirector s)
        {
            NightOverTheTown(s);
            s.Field.Sign("sign", T("hideoutMaps.setupHideoutPathNow.1"));
        }

        /// <summary>등불 하나뿐인 어두운 낡은 아지트(3·5장). 뒷문은 잠겨 있다.</summary>
        public static void SetupHideoutNow(StoryDirector s)
        {
            s.Field.SetAmbient(HideoutNight, HideoutNightCover);
            s.Field.LockWarp("out", T("hideoutMaps.setupHideoutNow.1"));
        }

        /// <summary>낡은 아지트의 2층과 방도 1층처럼 어둡다.</summary>
        public static void SetupUpstairsNow(StoryDirector s)
        {
            s.Field.SetAmbient(HideoutNight, HideoutNightCover);
        }
    }
}
