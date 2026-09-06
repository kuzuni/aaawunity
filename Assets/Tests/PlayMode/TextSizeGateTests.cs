using System.Collections;
using System.Collections.Generic;
using System.Text;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T63 «글자 가독성» 게이트 — UiShotsTests 와 같은 순서로 모든 화면·팝업을 열고 활성 <c>Text</c> 를 전부 모아(<see cref="TextAudit"/>)
    /// ⓐ 종류별 하한(본문 40 · 버튼 44 · 보조 36 · 제목 60 · Small 제외) ⓑ bestFit 최소 ≥ 32 를 단언한다. ⓒ 잘림/넘침(선호 크기 > rect)은 <see cref="TextAudit.ClipStrict"/> 가 켜질 때까지
    /// 화면별 표로 로그에만 남긴다(하위 행 워커가 CI 로그의 «[TextSizeGate]» 표를 읽고 화면을 하나씩 0 으로 만든 뒤 켠다). 이 테스트가 이후 모든 UI 커밋의 회귀 게이트다(ROUTINE T63 4항).
    /// </summary>
    public class TextSizeGateTests
    {
        App _app; PlayLog _log;
        readonly List<TextAudit.Row> _rows = new List<TextAudit.Row>();

        [SetUp] public void SetUp() { _log = new PlayLog(); _rows.Clear(); }
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

        /// <summary>한 화면 — 연출을 끝내고 레이아웃을 굳힌 뒤 모든 루트 캔버스(UI + 월드 발밑 바 등)의 활성 Text 를 모은다.</summary>
        IEnumerator Check(string name)
        {
            UiKit.CompleteAllTweens();
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (cv != null && cv.isRootCanvas) _rows.AddRange(TextAudit.Collect(name, cv.transform));
            yield return Frames(1);
        }
        static bool Press(Transform root, string name) { if (root == null) return false; var t = UiKit.Find(root, name); var b = t != null ? t.GetComponent<UnityEngine.UI.Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        GearItem Give(string part, int rar = 0, int plus = 0)
        {
            foreach (var t in _app.Data.Gear.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, rar, plus); _app.Save.Inv.Add(g); return g; }
            return null;
        }

        [UnityTest]
        public IEnumerator EveryActiveTextMeetsTheMinimumSize()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            S.Gold = 11540; S.Gem = 543;

            // 01 로비 · 12 설정
            Assert.AreEqual("lobby", _app.Current.Name);
            yield return Check("01_lobby");
            _app.Overlay.Settings(); yield return Check("12_settings"); _app.Overlay.Close(); yield return Frames(1);

            // 11 특권 · 15 퀘스트 · 16 출석 · 17 데일리 기프트 · 18 7일 챌린지 · 19 시즌 패스
            _app.ShowScreen("privilege"); yield return Frames(2); yield return Check("11_shop_special"); _app.ShowScreen("lobby"); yield return Frames(1);
            LobbyPopups.Quest(_app); yield return Check("15_quest"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.Attendance(_app); yield return Check("16_attendance"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.DailyGift(_app); yield return Check("17_daily_gift"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.Challenge7(_app); yield return Check("18_challenge7"); _app.Overlay.Close(); yield return Frames(1);
            _app.ShowScreen("pass"); yield return Frames(2); yield return Check("19_pass"); _app.ShowScreen("lobby"); yield return Frames(1);

            // 13 펫 · 14 펫 세부
            _app.ShowScreen("pet"); yield return Frames(2); yield return Check("13_pet");
            (_app.Current as PetScreen)?.OpenDetail(0); yield return Check("14_pet_detail"); _app.Overlay.Close(); yield return Frames(1);

            // 20 던전 · 21 던전 세부 · 22 PvP · 23 아레나 입장 · 24 도전 · 25 순위 보상 · 26 상인
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(2); yield return Check("20_dungeon");
            var ev = _app.GetScreen<EventsScreen>(); var evRoot = _app.Current.Root;
            if (Press(UiKit.Find(evRoot, "Card:hell"), "EnterBtn")) { yield return Check("21_dungeon_detail"); _app.Overlay.Close(); yield return Frames(1); }
            ev.ShowPage(EventsScreen.PagePvp); yield return Check("22_arena");
            ev.ShowPage(EventsScreen.PageArena); yield return Check("23_arena_enter");
            if (Press(evRoot, "ChallengeBtn")) { yield return Check("24_arena_challenge"); _app.Overlay.Close(); yield return Frames(1); }
            if (Press(evRoot, "RewardsBtn")) { yield return Check("25_arena_rank_reward"); _app.Overlay.Close(); yield return Frames(1); }
            ev.ShowPage(EventsScreen.PageMerchant); yield return Check("26_arena_shop");
            _app.ShowScreen("lobby"); yield return Frames(1);

            // 06 장비 · 07 세부 · 08 대장간 · 09/10 상점
            GearItem firstFree = null;
            foreach (var p in D.Gear.Parts) { var g = Give(p, rar: 1, plus: 1); S.Eq[p] = g.Uid; }
            for (int i = 0; i < 10; i++) { var g = Give(D.Gear.Parts[i % D.Gear.Parts.Length], rar: i % 3, plus: i % 2); if (firstFree == null) firstFree = g; }
            _app.ShowScreen("gear"); yield return Frames(2); yield return Check("06_gear");
            if (firstFree != null) { GearUi.OpenDetail(_app, firstFree, null); yield return Check("07_gear_detail"); _app.Overlay.Close(); yield return Frames(1); }
            _app.ShowScreen("forge"); yield return Frames(2); yield return Check("08_gear_fuse");
            _app.ShowScreen("shop"); yield return Frames(2); yield return Check("10_shop_2");
            (_app.Current as ShopScreen)?.ScrollTo(0f); yield return Check("09_shop_1");

            // 02 전투 HUD(3초 · 팝업 닫고) · 04 레벨업 · 05 보유 특전
            _app.StartBattle(1); yield return RealSeconds(3f);
            Assert.AreEqual("battle", _app.Current.Name);
            var bs = _app.GetScreen<BattleScreen>(); var G = bs != null ? bs.G : null; Assert.IsNotNull(G, "전투 상태");
            Time.timeScale = 0f;
            if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; yield return Frames(1); }
            yield return Check("02_battle");
            var rng = new Mulberry32(7u);
            var offer = Perks.Offer(D, G.Taken, false, rng);
            if (offer.Count > 0)
            {
                G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
                _app.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick)); yield return Check("04_perks");
                _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
                for (int i = 0; i < offer.Count && i < 3; i++) G.Taken.Add(offer[i]);
            }
            _app.Overlay.PerkBook(G, null); yield return Check("05_perks_list"); _app.Overlay.Close(); yield return Frames(1);
            Time.timeScale = 1f; _app.ShowScreen("lobby"); yield return Frames(2);

            // 판정
            Assert.Greater(_rows.Count, 50, "활성 Text 가 거의 안 모였다(수집 실패)");
            var floorBad = new List<string>(); var fitBad = new List<string>(); var clipped = new List<string>();
            foreach (var r in _rows)
            {
                if (r.FloorBad) floorBad.Add(r.ToString());
                if (r.BestFitBad) fitBad.Add(r.ToString());
                if (r.Clipped) clipped.Add(r.ToString());
            }
            var sb = new StringBuilder();
            sb.AppendLine($"[TextSizeGate] 활성 Text {_rows.Count} · 하한 미달 {floorBad.Count} · bestFit 미달 {fitBad.Count} · 잘림/넘침 {clipped.Count}(strict={TextAudit.ClipStrict})");
            sb.Append(TextAudit.Summary(_rows));
            if (clipped.Count > 0) { sb.AppendLine("[TextSizeGate] 잘림/넘침 목록(화면별 하위 행이 0 으로 만든다):"); foreach (var c in clipped) sb.AppendLine("  " + c); }
            Debug.Log(sb.ToString());

            Assert.AreEqual(0, floorBad.Count, "글자 하한 미달(T63 · 본문 40 · 버튼 44 · 보조 36 · 제목 60 · Small 은 명시):\n" + string.Join("\n", floorBad));
            Assert.AreEqual(0, fitBad.Count, "bestFit 최소 크기 미달(≥ 32):\n" + string.Join("\n", fitBad));
            if (TextAudit.ClipStrict) Assert.AreEqual(0, clipped.Count, "잘림/넘침(선호 크기 > rect):\n" + string.Join("\n", clipped));
            _log.AssertNoRed("글자 크기 게이트(전 화면)");
            yield return Shutdown();
        }
    }
}
