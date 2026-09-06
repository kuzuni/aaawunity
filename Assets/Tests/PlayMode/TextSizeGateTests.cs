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
        /// <summary>가장 긴 실제 토스트(ForgeScreen 재료 안내 · 최악의 이름 = «체력실드 목걸이») — 본문 40 으로 두 줄이다.</summary>
        const string LongToast = "같은 부위·종류·등급만 재료가 됩니다 (목걸이 · 체력실드 목걸이 · 신화)";
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
        /// <summary>트윈을 끝내지 않고 모은다 — 보스 경고 띠(<see cref="Overlay.BossWarn"/>)는 트윈이 끝나면 <c>OnComplete</c> 로 스스로 파괴돼 <see cref="Check"/> 로는 못 잡는다(T63-toast).</summary>
        IEnumerator CheckLive(string name)
        {
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (cv != null && cv.isRootCanvas) _rows.AddRange(TextAudit.Collect(name, cv.transform));
            yield return Frames(1);
        }
        List<TextAudit.Row> Rows(string screen) { var l = new List<TextAudit.Row>(); foreach (var r in _rows) if (r.Screen == screen) l.Add(r); return l; }

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
            // T78(주인 2026-09-07) — 18_challenge7 · 19_pass 는 화면째 삭제돼 게이트 대상이 아니다
            _app.ShowScreen("privilege"); yield return Frames(2); yield return Check("11_shop_special"); _app.ShowScreen("lobby"); yield return Frames(1);
            LobbyPopups.Quest(_app); yield return Check("15_quest"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.Attendance(_app); yield return Check("16_attendance"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.DailyGift(_app); yield return Check("17_daily_gift"); _app.Overlay.Close(); yield return Frames(1);

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

            // 결과·이벤트 팝업 5종(T63-results · 레퍼런스 jpg 가 없는 화면이라 번호 대신 이름) — 쉼터 · 악마(+선물) · 천사 · 광고 · 승리 · 패배.
            // 여기까지 안 열어 보면 «[TextSizeGate]» 표에 이 팝업들이 아예 안 나온다(T63-toast 가 ClipStrict 를 켤 때 빈 구멍이 된다).
            G.Gold = 12750; G.Kills = 137;
            _app.Overlay.Rest(G, _ => { }, () => { }); yield return Check("ev_rest"); _app.Overlay.Close(); yield return Frames(1);
            var devilPerk = Perks.OfferDevil(D, G.Taken, rng);
            G.Pending = new PendingDecision { Kind = PendingKind.Devil, DevilPerk = devilPerk };
            _app.Overlay.Devil(G, _ => { }); yield return Check("ev_devil"); _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
            _app.Overlay.DevilGift(devilPerk, null); yield return Check("ev_devil_gift"); _app.Overlay.Close(); yield return Frames(1);
            _app.Overlay.Angel(G, _ => { }); yield return Check("ev_angel"); _app.Overlay.Close(); yield return Frames(1);
            _app.Overlay.AdCountdown(9, () => { }); yield return Check("ev_ad"); _app.Overlay.Close(); yield return Frames(1);
            _app.Overlay.Clear(G, false, () => { }, () => { }); yield return Check("res_win"); _app.Overlay.Close(); yield return Frames(1);
            _app.Overlay.Clear(G, true, () => { }, () => { }); yield return Check("res_win_last"); _app.Overlay.Close(); yield return Frames(1);
            _app.Overlay.Dead(G, () => { }); yield return Check("res_lose"); _app.Overlay.Close(); yield return Frames(1);

            Time.timeScale = 1f; _app.ShowScreen("lobby"); yield return Frames(2);

            // ⑫ 27 토스트 · 28 «데이터 삭제» 확인 팝업 · 29 보스 경고 띠 (T63-toast — 앞서 어느 화면에서도 안 열리던 셋)
            // 가장 긴 실제 토스트(ForgeScreen 재료 안내 · 최악의 이름 = «체력실드 목걸이») — 본문 40 으로 두 줄이라 칸이 모자라면 bestFit 이 말없이 줄인다
            _app.Toast(LongToast); yield return Check("27_toast");
            _app.Overlay.ConfirmReset(); yield return Check("28_confirm_reset");
            _app.Overlay.Close(); yield return Frames(1);
            _app.Overlay.BossWarn(_app.Frame); yield return CheckLive("29_boss_warn");
            UiKit.CompleteAllTweens(); yield return Frames(2);

            // 판정
            Assert.Greater(_rows.Count, 50, "활성 Text 가 거의 안 모였다(수집 실패)");
            var floorBad = new List<string>(); var fitBad = new List<string>(); var clipped = new List<string>(); var noGlyph = new List<string>(); var noOutline = new List<string>();
            foreach (var r in _rows)
            {
                if (r.FloorBad) floorBad.Add(r.ToString());
                if (r.BestFitBad) fitBad.Add(r.ToString());
                if (r.Clipped) clipped.Add(r.ToString());
                if (!string.IsNullOrEmpty(r.Missing)) noGlyph.Add(r.ToString());
                if (r.OutlineBad) noOutline.Add(r.ToString());
            }
            var sb = new StringBuilder();
            sb.AppendLine($"[TextSizeGate] 활성 Text {_rows.Count} · 하한 미달 {floorBad.Count} · bestFit 미달 {fitBad.Count} · 잘림/넘침 {clipped.Count}(strict={TextAudit.ClipStrict})");
            sb.Append(TextAudit.Summary(_rows));
            if (clipped.Count > 0) { sb.AppendLine("[TextSizeGate] 잘림/넘침 목록(화면별 하위 행이 0 으로 만든다):"); foreach (var c in clipped) sb.AppendLine("  " + c); }
            // T75 — 글꼴(Jua)에 글리프가 없어 «폭 0» 으로 사라지는 글자. UiKit 을 거치는 글자는 TextGlyphs.Safe 가 걸러내므로 여기 남는 것은
            // 화면 코드가 Text.text 에 직접 넣는 자리와 대체 글자가 없는 기호(₩ 등)뿐이다 — 그 화면 묶음 워커가 문구를 고치고 GlyphStrict 를 켠다.
            sb.AppendLine($"[GlyphGate] 없는 글자가 있는 줄 {noGlyph.Count}(strict={TextAudit.GlyphStrict} · T75)");
            sb.Append(TextAudit.GlyphSummary(_rows));
            if (noGlyph.Count > 0) { sb.AppendLine("[GlyphGate] 목록:"); foreach (var c in noGlyph) sb.AppendLine("  " + c); }
            // T63-outline — 주인 «모든 글자들 다 검정 아웃라인 있는 것으로 · 지금 어떤 건 있고 어떤 건 없고 그러네».
            // 글자 입구 다섯 곳이 전부 UiKit.EnsureOutline 을 거치므로, 여기 남는 줄은 «UiKit 을 안 거치고 스스로 Text 를 붙인 자리» 뿐이다.
            sb.AppendLine($"[TextOutlineGate] 아웃라인 어긋난 줄 {noOutline.Count}(strict={TextAudit.OutlineStrict} · T63-outline)");
            sb.Append(TextAudit.OutlineSummary(_rows));
            Debug.Log(sb.ToString());

            // ⑫ T63-toast — 표를 찍은 «뒤에» 단언한다(먼저 터지면 위 표가 안 남아 다른 하위 행 워커가 자기 화면 수를 못 읽는다 · CI #119 에서 실제로 그랬다)
            foreach (var r in Rows("27_toast"))
            {
                if (r.Path.IndexOf("ui.toast", System.StringComparison.Ordinal) < 0) continue;
                Assert.GreaterOrEqual(r.Used, TextSize.Body, "토스트 글자가 본문 하한(40)보다 작게 그려진다 — 칸(Layout.Toast)이 두 줄을 못 담는다: " + r);
                Assert.IsFalse(r.Clipped, "토스트 글자가 칸을 넘친다: " + r);
                Assert.IsFalse(r.Text.Contains("·"), "토스트 문구가 TextGlyphs.Safe 를 안 거쳤다 — Jua 에 «·» 글리프가 없어 폭 0 으로 사라진다: " + r);
                Assert.IsTrue(r.Text.Contains("부위/종류/등급"), "가운뎃점이 «/» 로 바뀌어야 한다: " + r);
            }
            foreach (var r in Rows("28_confirm_reset"))
            {
                // Check 는 «화면 전체»(뒤에 깔린 로비까지)를 모으므로 팝업 층(Overlay) 밑만 본다 — 뒤 로비의 아이콘 라벨은 보조 36 이 정상이라
                // 여기서 본문 40 을 들이대면 남의 화면을 잘못 잡는다(CI #119 에서 «스타터팩» 36 으로 실제로 걸렸다 · 워커 E 가 T79 로 «Screen:» 건너뛰기로 먼저 고쳤고, 이 판은
                // 그 반대로 «팝업 층만 본다» 는 흰 목록이라 뒤에 어떤 화면이 깔려도 안전하다).
                if (!r.Path.Contains("/Overlay/")) continue;
                Assert.AreEqual(TextGlyphs.Safe(r.Text), r.Text, "글꼴에 없는 글자(«·» 등)가 그대로 남아 폭 0 으로 사라진다: " + r);
                // 제목 리본(ui.title.*)·«탭하여 닫기» 는 UiKit.Popup 이 모든 팝업에 공통으로 다는 조각이라 이 행의 몫이 아니다 — 리본 글자 칸(656×115 의 안쪽 436×79.4)이
                // 제목 60 의 줄(84px)보다 낮아 bestFit 이 56 으로 줄이는 것은 전 팝업 공통 결함이고 T75 로 등재했다(ClipStrict 를 켜기 전에 고쳐야 한다).
                if (r.Path.Contains("ui.title.") || r.Path.Contains("TapToClose")) continue;
                Assert.IsFalse(r.Clipped, "«데이터 삭제» 확인 팝업 글자가 칸을 넘친다: " + r);
                // 하한은 그 글자의 «종류» 로 잰다(본문 40 · 버튼 44 · 보조 36 · 제목 60)
                Assert.GreaterOrEqual(r.Used, TextSize.Min(r.Kind), "확인 팝업 글자가 종류 하한보다 작게 그려진다: " + r);
            }
            var warn = Rows("29_boss_warn").Find(r => r.Text == "보스");
            Assert.IsNotNull(warn, "보스 경고 띠에 «보스» 글자가 있어야 한다(영문 데모 문구 0 — T34 ⓒ)");
            Assert.AreEqual(TextKind.Title, warn.Kind, "보스 경고 띠 글자는 제목 종류다: " + warn);
            Assert.GreaterOrEqual(warn.Used, TextSize.Title, "보스 경고 띠 글자가 제목 하한(60)보다 작게 그려진다: " + warn);
            Assert.IsFalse(warn.Clipped, "보스 경고 띠 글자가 칸을 넘친다: " + warn);

            Assert.AreEqual(0, floorBad.Count, "글자 하한 미달(T63 · 본문 40 · 버튼 44 · 보조 36 · 제목 60 · Small 은 명시):\n" + string.Join("\n", floorBad));
            Assert.AreEqual(0, fitBad.Count, "bestFit 최소 크기 미달(≥ 32):\n" + string.Join("\n", fitBad));
            if (TextAudit.ClipStrict) Assert.AreEqual(0, clipped.Count, "잘림/넘침(선호 크기 > rect):\n" + string.Join("\n", clipped));
            if (TextAudit.GlyphStrict) Assert.AreEqual(0, noGlyph.Count, "글꼴에 없는 글자(폭 0 으로 사라진다 · T75):\n" + string.Join("\n", noGlyph));
            // 아웃라인 = 있고 · 1개고 · 색이 Ink 고 · 두께가 크기 규칙과 맞아야 한다(T63-outline · 주인 04:4X)
            if (TextAudit.OutlineStrict) Assert.AreEqual(0, noOutline.Count, "검은 아웃라인이 없거나 어긋난 글자(T63-outline · 주인 «모든 글자들 다 검정 아웃라인»):\n" + string.Join("\n", noOutline));
            _log.AssertNoRed("글자 크기 게이트(전 화면)");
            yield return Shutdown();
        }
    }
}
