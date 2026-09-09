using System.Collections;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T240 1항 ⓑ — <b>PvP 판에는 하단 HUD 가 없다</b>(주인 레퍼런스 <c>33_pvp_battle.jpg</c>: 바닥이 잔디이고 우하단에 원형 버튼만 있다).
    /// <para>
    /// <b>이 자의 절반은 «켜는 쪽» 이다.</b> 한 <see cref="BattleScreen"/> 이 아레나 판과 챕터 판을 <b>번갈아</b> 연다 —
    /// 끄기만 하고 켜기를 잊으면 «아레나를 한 번 다녀오면 챕터 전투에 HUD 가 없다» 가 되는데, 그 사고는
    /// <b>아레나만 보는 자에게는 안 잡힌다.</b> 그래서 여기서 챕터 → 아레나 → 챕터로 돌아와서 잰다.
    /// </para>
    /// <para>
    /// AUTO 는 <b>누르는 길을 안 걸었다</b>(결정 800 ⓐ) — 우리 전투에 «자동» 이 없다. 그것도 여기서 잰다:
    /// «껍데기라도 눌리게» 두면 «눌리는데 아무 일도 안 나는 버튼» 이 된다(T257 · 결정 768).
    /// </para>
    /// </summary>
    public class PvpHudTests
    {
        App _app; PlayLog _log;
        /// <summary>PvP 에서 꺼져야 하는 챕터 전투 HUD — 이름은 <see cref="BattleScreen"/> 이 붙인 그대로다.</summary>
        static readonly string[] ChapterHud = { "HudPanel", "Bar:EXP", "Bar:HP", "Bar:SH", "PerkStrip", "PerkBook", "SpeedBtn", "PetBtn", "Pills" };

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static bool ActiveByName(Transform root, string name)
        {
            var t = UiKit.Find(root, name);
            return t != null && t.gameObject.activeInHierarchy;
        }
        void AssertHud(bool on, string where)
        {
            var root = _app.Current.Root;
            foreach (var n in ChapterHud)
                Assert.AreEqual(on, ActiveByName(root, n), $"[{where}] «{n}» 은 {(on ? "켜져" : "꺼져")} 있어야 한다");
        }

        /// <summary>칸 안 초상 그림(<see cref="Profile.FaceName"/>)의 스프라이트 — 없으면 null(T262 3항).</summary>
        static Sprite FaceSprite(Transform cell)
        {
            var f = UiKit.Find(cell, Profile.FaceName);
            var im = f != null ? f.GetComponent<Image>() : null;
            return im != null ? im.sprite : null;
        }

        /// <summary>T240 ⓑ — 아레나 판은 하단 HUD 없이 서고, <b>챕터 판으로 돌아오면 다시 선다</b>.</summary>
        [UnityTest]
        public IEnumerator ArenaRunHidesTheChapterHudAndAChapterRunBringsItBack()
        {
            yield return Boot();

            _app.StartBattle(1); yield return Frames(3);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            Assert.IsFalse(bs.IsArena, "먼저 보통 챕터 판이다");
            AssertHud(true, "챕터 판");
            Assert.IsFalse(ActiveByName(_app.Current.Root, "PvpHead"), "챕터 판에는 PvP 머리가 없다");

            _app.StartBattle(1, null, null, "ShadowKnight", 3); yield return Frames(3);
            Assert.IsTrue(bs.IsArena, "아레나 판이다");
            AssertHud(false, "아레나 판");
            Assert.IsTrue(ActiveByName(_app.Current.Root, "PvpHead"), "아레나 판에는 PvP 머리가 선다");
            // T262 3항 — 머리 양쪽 초상은 «프로필과 같은 조각 + 얼굴» 이다(주인 «프레임이 실제 프로필 프레임이랑 디자인이 다르네»).
            //   종전에는 `ui.itemFrame.yellow`(팔각 물건 칸)를 세워 두고 **안이 비어 있었다** — 레퍼런스 33 은 양쪽에 얼굴이 있다.
            {
                var head = UiKit.Find(_app.Current.Root, "PvpHead");
                var my = UiKit.Find(head, "MyFace"); var foe = UiKit.Find(head, "FoeFace");
                Assert.IsNotNull(my, "왼쪽(나) 아바타 칸"); Assert.IsNotNull(foe, "오른쪽(상대) 아바타 칸");
                Assert.IsNotNull(UiKit.Find(my, Profile.FrameKey(_app.Save)), "왼쪽은 내가 고른 프로필 프레임");
                Assert.IsNotNull(UiKit.Find(my, Profile.FaceName), "왼쪽 칸이 비어 있으면 안 된다(내 초상)");
                Assert.IsNotNull(UiKit.Find(foe, Profile.FaceName), "오른쪽 칸이 비어 있으면 안 된다(상대 초상)");
                Assert.IsFalse(GearUi.HasItemFrame(my), "옛 물건 칸(ItemFrame_01)이 남으면 안 된다");
                Assert.AreEqual(_app.Assets.Sprite(Profile.DummyIcon(3)), FaceSprite(foe),
                    "상대 얼굴은 그 순위(3위)의 더미 얼굴 — 23·24 목록이 보여 준 그 얼굴이어야 한다(T262 3항)");
            }

            // ⚑ 여기가 이 자의 핵심 — 끄기만 하고 켜기를 잊는 사고는 아레나만 보면 안 잡힌다.
            _app.StartBattle(1); yield return Frames(3);
            Assert.IsFalse(bs.IsArena, "다시 챕터 판이다");
            AssertHud(true, "아레나 다녀온 뒤 챕터 판");
            Assert.IsFalse(ActiveByName(_app.Current.Root, "PvpHead"), "챕터 판으로 돌아오면 PvP 머리는 다시 꺼진다");

            _log.AssertNoRed("챕터 → 아레나 → 챕터");
            yield return Shutdown();
        }

        /// <summary>
        /// T240 ⓐ — 우하단 «AUTO» 는 <b>아레나 판에만</b> 서고 <b>누르는 길이 없다</b>.
        /// <para>천사·악마는 <b>안 세웠다</b>(그림이 없다 · 결정 800 ⓐ) — «없다» 도 자로 못 박아 둔다:
        /// 다음 사람이 아무 아이콘이나 끼워 셋을 채우면 이 자가 먼저 말한다.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AutoCircleStandsOnlyInArenaAndIsNotWiredToAnything()
        {
            yield return Boot();

            _app.StartBattle(1); yield return Frames(3);
            Assert.IsFalse(ActiveByName(_app.Current.Root, "PvpAutoBtn"), "챕터 판에는 AUTO 가 없다");

            _app.StartBattle(1, null, null, "ShadowKnight", 3); yield return Frames(3);
            var auto = UiKit.Find(_app.Current.Root, "PvpAutoBtn");
            Assert.IsNotNull(auto, "아레나 판에는 AUTO 가 선다");
            Assert.IsTrue(auto.gameObject.activeInHierarchy, "AUTO 가 켜져 있다");
            Assert.IsNull(auto.GetComponent<Button>(), "AUTO 에는 누르는 길이 없다 — 기능이 없는 버튼은 «못 누르게» 가 아니라 «안 걸어 두기» 다(T257)");

            foreach (var n in new[] { "PvpAngelBtn", "PvpDevilBtn" })
                Assert.IsNull(UiKit.Find(_app.Current.Root, n), $"«{n}» 은 아직 없다 — 천사·악마는 UI 아이콘이 없어 안 만들었다(새 그림 금지 · §1)");

            _log.AssertNoRed("아레나 우하단 원형 버튼");
            yield return Shutdown();
        }
    }
}
