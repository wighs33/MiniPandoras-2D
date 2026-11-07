using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiniPandoras.Combat;
using MiniPandoras.Minigames;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 2장 「훔치지 않은 보석」을 저장에서 이어 처음부터 끝까지 돌린다. 탐험 차례마다 주인공을 이벤트 칸에 바로 세운다(StoryDriver).
    /// 옥상 싸움에서는 공격을 연타하고, 글라이더는 그대로 둔다(실패가 없다).
    /// </summary>
    public sealed class Chapter2Tests
    {
        private const string LeftTheBatLine = "냐, 방망이를 그냥 두고 왔군.\n내가 챙겨 왔다.\n손에 쥐는 법을 알려 주지.";

        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter2()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(2);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EscapesTheSnakeCastleAndLandsByTheTown()
        {
            // 힐리아 동부: 랑의 안내 뒤 북쪽 끝(y ≤ 2)에 닿으면 뱀성이 보인다.
            yield return story.UntilExploring("hillia", "2장 첫 탐험");
            Assert.That(story.Runner.Chapter, Is.EqualTo(2), "저장의 장부터 이어 한다");
            Assert.That(story.Combat.EnemyAi.Enemies.Count, Is.EqualTo(story.Combat.Catalog.GroupsIn("hillia").Sum(group => group.Monsters.Count)), "힐리아 동부에 배치표의 몬스터가 다 있다");
            yield return story.StandOn(new Vector2Int(17, 2));
            yield return story.Until(() => story.Lines.Contains(LeftTheBatLine), 10f, "방망이를 두고 들판을 떠나면 랑이 건넨다");
            yield return story.Until(() => story.Session.State.Weapon == "bat", 10f, "튜토리얼을 따라 방망이를 장착한다");

            // 뱀성을 처음 마주하면 컷인으로 강가에서 성을 올려다본 뒤 돌뱀 머리를 비추고, 필드(뱀성 앞)로 돌아온다.
            var cutIn = Object.FindFirstObjectByType<CutInView>(FindObjectsInactive.Include);
            var shots = new List<string>();
            yield return story.Until(() =>
            {
                if (cutIn.gameObject.activeSelf && !shots.Contains(cutIn.ShotName))
                {
                    shots.Add(cutIn.ShotName);
                }

                return !story.Mover.IsFrozen && story.Travel.CurrentMap.Id == "castle" && !story.Travel.IsTravelling;
            }, 120f, "뱀성 앞 탐험");
            Assert.That(shots, Is.EqualTo(new[] { "castleWide", "fallenMap" }), "원경(강가에서 올려다본 뱀성) 다음 근경(돌뱀 머리)");
            Assert.That(cutIn.gameObject.activeSelf, Is.False, "컷인이 닫히고 필드로 돌아온다");
            Assert.That(story.Lines, Has.Member("우와... 커다란 뱀 모양의 성이다."));
            Assert.That(story.Lines, Has.Member("저기에 지도가 떨어져 있는 것 같군."));

            // 떨어진 지도를 주우면 랑이 지도 튜토리얼을 한다.
            Assert.That(story.Session.State.Owns(Items.WorldMap), Is.False);
            yield return story.StandOn(new Vector2Int(33, 69));
            yield return story.Until(() => story.Lines.Contains("길을 잃으면 펼쳐 봐라.\n이제 성으로 가자."), 30f, "지도 튜토리얼 끝");
            Assert.That(story.Session.State.Owns(Items.WorldMap), Is.True, "지도가 가방에 든다");
            Assert.That(Object.FindFirstObjectByType<WorldMapView>(FindObjectsInactive.Include).IsOpen, Is.False, "튜토리얼 끝에 지도를 닫는다");
            Assert.That(story.Session.State.CanEquip(Items.WorldMap), Is.False, "지도는 장착하지 않는다");

            // 뱀성: 산길을 올라 성문을 지나 안뜰(courtyard)에 들어서면 탑을 올려다본다. 루나는 아직 나오지 않고 사람들과 이야기할 수 있다.
            yield return story.StandOn(story.Director.Field.Point("courtyard"));
            yield return story.UntilExploring("castle", "안뜰 구경");
            var shop = Object.FindFirstObjectByType<ShopView>();
            bool shopOpened = false;
            story.Director.StartCoroutine(CloseShopOnceOpen(shop, () => shopOpened = true));
            yield return story.TalkTo("npc0");
            Assert.That(shopOpened, Is.True, "뱀성 입구의 상인은 인사한 뒤 상점을 연다");
            Assert.That(story.Lines, Has.Member("어서 와!\n뱀성 구경은 처음이야?"), "성 밖에서는 사람들과 이야기할 기회가 있다");
            Assert.That(story.Lines, Has.No.Member("아야얏!\n너 뭐야, 이 자식아 (｀Д´)ノ"), "루나는 탑에 들어가기 전에는 나오지 않는다");

            // 본관(keep)으로 들어서면 1층에서 루나가 정면으로 달려와 부딪치고, 보석을 쥐여 준 뒤 사라진다.
            yield return story.StandOn(story.Director.Field.Point("keep"));

            // 뱀탑: 1층 비늘 홀 → 2층 병영 → 3층 보물고를 위층 계단(up)으로 올라가면 꼭대기(뱀 머리 위)에서 탐험이 끝난다.
            foreach ((string floor, string label) in new[] { ("castleHall", "1층 탐험"), ("castleBarracks", "2층 탐험"), ("castleVault", "3층 탐험") })
            {
                yield return story.UntilExploring(floor, label);
                yield return story.StandOn(story.Director.Field.Point("up"));
            }

            // 꼭대기: 경비대장과 끝까지 싸운다. 「차원」이 깨어나 스킬 튜토리얼을 하고, 체력이 바닥나도 쓰러지지 않아 병사들이 몰려오면 글라이더로 날아간다.
            var glider = Object.FindFirstObjectByType<GliderFlight>();
            var touch = Object.FindFirstObjectByType<TouchControlsView>();
            bool flewGlider = false;
            bool charged = false;
            bool pointedAtSkill = false;
            int lowestCaptainHp = int.MaxValue;
            Object.FindFirstObjectByType<TutorialGuide>().Pointed += target => pointedAtSkill |= target == touch.SkillButton(0);
            yield return story.Until(() =>
            {
                story.Fight();
                EnemyFighter captain = story.Combat.Encounters.Boss;
                if (captain != null)
                {
                    charged |= captain.State == EnemyState.Charge;
                    lowestCaptainHp = Mathf.Min(lowestCaptainHp, captain.Hp);
                }

                flewGlider |= glider.Phase == GliderPhase.Play;
                // 조종하지 않으면 화살에 떨어진다. 떨어지는 것은 GliderTests가 본다.
                typeof(GliderFlight).GetProperty(nameof(GliderFlight.Invulnerable)).SetValue(glider, 50);
                return story.Runner.Chapter > 2;
            }, 300f, "옥상 싸움과 글라이더");

            Assert.That(story.Lines, Has.Member("아야얏!\n너 뭐야, 이 자식아 (｀Д´)ノ"), "루나가 대본대로 부딪친다");
            Assert.That(story.Lines, Has.Member("거기까지다, 도둑놈."));
            Assert.That(story.Lines, Has.Member("냐앗?! ...힘이 제대로 깨어나지도 않았잖아!\n흘려보내는 게 고작이냐?!"), "싸우다 「차원」이 깨어난다");
            Assert.That(story.Session.State.Pandoras, Has.Member("phase"), "「차원」을 얻었다");
            Assert.That(pointedAtSkill, Is.True, "싸우는 도중에 튜토리얼이 「차원」 스킬 칸을 가리킨다");
            Assert.That(charged, Is.True, "경비대장이 돌진한다");
            Assert.That(lowestCaptainHp, Is.EqualTo(1), "체력이 바닥나도 경비대장은 쓰러지지 않는다");
            Assert.That(story.Lines, Has.Member("이 정도로 쓰러질 것 같으냐!\n전원, 옥상으로 올라와라!"));
            Assert.That(story.Lines, Has.Member("냐, 저 수는 당해 낼 수 없다!\n저기 글라이더가 있다!"), "병사들이 몰려와 글라이더로 달아난다");
            Assert.That(story.Notices, Has.Member("뱀신의 눈을 받았다."));
            Assert.That(story.Session.State.Owns(Items.SerpentEye), Is.True, "뱀신의 눈이 가방에 들어온다");
            Assert.That(flewGlider, Is.True, "옥상에서 글라이더로 뛰어내린다");
            Assert.That(story.Lines, Has.Member("...죄송합니다......"));
            Assert.That(story.Session.State.HasFlag("rangHurt"), Is.True);
            Assert.That(story.Travel.CurrentMap.Id, Is.EqualTo("town"), "마을 어귀에 내려앉는다");
            Assert.That(SaveStore.Read().Chapter, Is.EqualTo(3), "3장이 시작되며 그 장을 저장한다");
        }

        private static IEnumerator CloseShopOnceOpen(ShopView shop, System.Action opened)
        {
            yield return new WaitUntil(() => shop.IsOpen);
            opened();
            shop.transform.Find("Screen/Frame/Box/Close").GetComponent<Button>().onClick.Invoke();
        }
    }
}
