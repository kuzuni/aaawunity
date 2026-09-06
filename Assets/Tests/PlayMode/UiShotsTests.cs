using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T46 «UI 비평 하니스» — UiSmokeTests 와 같은 순서로 화면·팝업을 열어 <b>PNG(540×1170)</b> 와 <b>layout.json</b>(활성 <see cref="UiTag"/> 의 프레임 %) 을 <c>ui-screens/</c> 에 남긴다.
    /// CI(unity-test 잡)가 main push 때 그 폴더를 <c>screens</c> 브랜치로 올리고, 워커가 <c>tools/ui_score.py</c> 로 <c>docs/ref-layout.md</c> 표와 대조해 채점한다(ROUTINE §5).
    /// 파일 이름 = <c>docs/ref/</c> 번호와 같게(01_lobby · 02_battle …). 아직 없는 화면은 건너뛰고 <c>_missing</c> 에 «없음» 으로 적는다.
    /// 검사는 빨간 줄 0(<see cref="PlayLog"/>)과 «PNG·layout 이 하나라도 남았는가» 뿐 — 구도 단언은 UiSmokeTests 가, 채점은 워커가 한다.
    /// </summary>
    public class UiShotsTests
    {
        App _app; PlayLog _log;
        readonly Dictionary<string, object> _layout = new Dictionary<string, object>();
        readonly List<object> _missing = new List<object>();
        int _saved;

        [SetUp] public void SetUp() { _log = new PlayLog(); _layout.Clear(); _missing.Clear(); _saved = 0; }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
        }
        /// <summary>n 프레임 — HeroView(RenderTexture 타깃) 카메라만 강제로 그린다(배치 모드 · CI #34 규약).</summary>
        IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }
        IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return Frames(1); }

        /// <summary>한 장 — 레이아웃을 한 번 더 굳히고(2프레임) PNG + 이름표 사각형.</summary>
        IEnumerator Shot(string name)
        {
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();
            if (PlayShot.Save(_app, name)) _saved++;
            _layout[name] = PlayShot.Layout(_app);
            yield return Frames(1);
        }
        /// <summary>이름으로 버튼을 누른다(onClick 직접 호출 · 입력 장치 없이).</summary>
        static bool Press(Transform root, string name) { if (root == null) return false; var t = UiKit.Find(root, name); var b = t != null ? t.GetComponent<UnityEngine.UI.Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        GearItem Give(string part, int rar = 0, int plus = 0)
        {
            foreach (var t in _app.Data.Gear.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, rar, plus); _app.Save.Inv.Add(g); return g; }
            return null;
        }

        [UnityTest]
        public IEnumerator AllScreensToPngAndLayoutJson()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            S.Gold = 11540; S.Gem = 543;   // 재화 pill 에 숫자가 보이게(레퍼런스 느낌 · 수치는 표시용)

            // 01 로비 · 12 설정
            Assert.AreEqual("lobby", _app.Current.Name);
            yield return Shot("01_lobby");
            _app.Overlay.Settings(); yield return Frames(2); yield return Shot("12_settings"); _app.Overlay.Close(); yield return Frames(1);

            // 11 특권 · 15 퀘스트 · 16 출석 · 17 데일리 기프트 · 18 7일 챌린지 · 19 시즌 패스 (T44 로비 사이드 껍데기 — 페이지 2 + 팝업 4)
            _app.ShowScreen("privilege"); yield return Frames(3); yield return Shot("11_shop_special"); _app.ShowScreen("lobby"); yield return Frames(1);
            LobbyPopups.Quest(_app); yield return Frames(2); yield return Shot("15_quest"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.Attendance(_app); yield return Frames(2); yield return Shot("16_attendance"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.DailyGift(_app); yield return Frames(2); yield return Shot("17_daily_gift"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.Challenge7(_app); yield return Frames(2); yield return Shot("18_challenge7"); _app.Overlay.Close(); yield return Frames(1);
            _app.ShowScreen("pass"); yield return Frames(3); yield return Shot("19_pass"); _app.ShowScreen("lobby"); yield return Frames(1);

            // 13 펫 탭 · 14 펫 세부 (T42 껍데기)
            _app.ShowScreen("pet"); yield return Frames(3); yield return Shot("13_pet");
            (_app.Current as PetScreen)?.OpenDetail(0); yield return Frames(2); yield return Shot("14_pet_detail"); _app.Overlay.Close(); yield return Frames(1);

            // 20 던전 · 21 던전 세부 · 22 PvP · 23 아레나 입장 · 24 도전 · 25 순위 보상 · 26 상인 (T43 껍데기 · 한 화면 «events» 의 페이지 4 + 팝업 3)
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(3); yield return Shot("20_dungeon");
            var ev = _app.GetScreen<EventsScreen>(); var evRoot = _app.Current.Root;
            if (Press(UiKit.Find(evRoot, "Card:hell"), "EnterBtn")) { yield return Frames(2); yield return Shot("21_dungeon_detail"); _app.Overlay.Close(); yield return Frames(1); } else _missing.Add("21_dungeon_detail (던전 카드 입장 버튼 없음)");
            ev.ShowPage(EventsScreen.PagePvp); yield return Frames(2); yield return Shot("22_arena");
            ev.ShowPage(EventsScreen.PageArena); yield return Frames(3); yield return Shot("23_arena_enter");
            if (Press(evRoot, "ChallengeBtn")) { yield return Frames(2); yield return Shot("24_arena_challenge"); _app.Overlay.Close(); yield return Frames(1); } else _missing.Add("24_arena_challenge (도전 버튼 없음)");
            if (Press(evRoot, "RewardsBtn")) { yield return Frames(2); yield return Shot("25_arena_rank_reward"); _app.Overlay.Close(); yield return Frames(1); } else _missing.Add("25_arena_rank_reward (보상 버튼 없음)");
            ev.ShowPage(EventsScreen.PageMerchant); yield return Frames(3); yield return Shot("26_arena_shop");
            _app.ShowScreen("lobby"); yield return Frames(1);

            // 06 장비(전부 장착 + 인벤 10) · 07 세부 · 08 대장간 · 09 상점
            GearItem firstFree = null;
            foreach (var p in D.Gear.Parts) { var g = Give(p, rar: 1, plus: 1); S.Eq[p] = g.Uid; }
            for (int i = 0; i < 10; i++) { var g = Give(D.Gear.Parts[i % D.Gear.Parts.Length], rar: i % 3, plus: i % 2); if (firstFree == null) firstFree = g; }
            _app.ShowScreen("gear"); yield return Frames(3); yield return Shot("06_gear");
            if (firstFree != null) { GearUi.OpenDetail(_app, firstFree, null); yield return Frames(2); yield return Shot("07_gear_detail"); _app.Overlay.Close(); yield return Frames(1); }
            _app.ShowScreen("forge"); yield return Frames(3); yield return Shot("08_gear_fuse");
            // 상점(T40) = 세로 스크롤 한 화면 — 레퍼런스 10 = 맨 위(상자 배너 · 상자 카드 2) · 09 = 끝까지 내린 상태(다이아 · 골드)
            _app.ShowScreen("shop"); yield return Frames(3); yield return Shot("10_shop_2");
            (_app.Current as ShopScreen)?.ScrollTo(0f); yield return Frames(2); yield return Shot("09_shop_1");

            // 02 전투(3초) · 03 적 조우(8초 안에 Engaged 가 되면) · 04 레벨업 · 05 보유 특전
            _app.StartBattle(1); yield return RealSeconds(3f);
            Assert.AreEqual("battle", _app.Current.Name);
            var bs = _app.GetScreen<BattleScreen>(); var G = bs != null ? bs.G : null; Assert.IsNotNull(G, "전투 상태");
            yield return Shot("02_battle");
            bool engaged = false; float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 8f) { if (bs.World != null && bs.World.Engaged && !_app.Overlay.IsOpen) { engaged = true; break; } if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; } yield return Frames(1); }
            if (engaged) yield return Shot("03_battle_enemy"); else _missing.Add("03_battle_enemy (8초 안에 적 조우 없음)");
            Time.timeScale = 0f; _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
            var rng = new Mulberry32(7u);
            var offer = Perks.Offer(D, G.Taken, false, rng);
            if (offer.Count > 0)
            {
                G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
                _app.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick)); yield return Frames(2); yield return Shot("04_perks");
                _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
                for (int i = 0; i < offer.Count && i < 3; i++) G.Taken.Add(offer[i]);
            }
            _app.Overlay.PerkBook(G, null); yield return Frames(2); yield return Shot("05_perks_list"); _app.Overlay.Close(); yield return Frames(1);
            Time.timeScale = 1f; _app.ShowScreen("lobby"); yield return Frames(2);

            // 20~26 은 T43 · 11·15~19 는 T44 가 위에서 찍는다 — 이제 «없음» 화면이 없다(_missing 은 03 조우 실패 때만)
            PlayShot.WriteLayout(_layout, _missing);
            Assert.Greater(_saved, 0, "PNG 가 하나도 안 남았다(RenderTexture 캡처 실패)");
            Assert.IsTrue(_layout.ContainsKey("01_lobby") && ((Dictionary<string, object>)_layout["01_lobby"]).Count > 0, "로비 이름표(UiTag)가 layout.json 에 있어야 한다");
            _log.AssertNoRed("스크린샷 회차(전 화면)");
            yield return Shutdown();
        }
    }
}
