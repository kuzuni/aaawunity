using System;
using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T43 «던전·아레나 껍데기»(docs/ref 20~26) 스모크 — 하단 탭 «던전» → 던전 페이지 → 세부 팝업 → PvP 페이지 → 아레나 입장 → 도전 팝업 · 순위 보상 팝업 → 상인 페이지 → 뒤로 ×3 → 로비,
    /// 그리고 로비 오른쪽 아래 «이벤트» 버튼 → PvP 페이지. 지점마다 빨간 줄 0 · 경로/키 경고 0 · 영문 데모 글자 0 · 핵심 요소 개수(카드 2 · 초상 3 · 배너 3 · 순위 줄 7 · 상대 줄 5 · 보상 줄 4 · 티어 5 · 상품 11) ·
    /// 구도(표 ⑩~⑯ 자리 ±0.5%p) · 껍데기 버튼(소탕·도전·새로고침·상품·일일/시즌 탭)은 눌러도 아무 일 없음 · 페이지 이동과 «탭하여 닫기»(배경 탭)만 동작.
    /// </summary>
    public class EventsScreenTests
    {
        App _app; PlayLog _log; readonly List<string> _warn = new List<string>();
        static readonly string[] Demo = { "Text", "New Text", "Dungeon", "Dungeons", "Enter", "Challenge", "PvP Tickets", "Merchant", "Coming Soon", "Rewards", "Sweep Last", "Free Refresh", "Limit 5/5" };

        [SetUp] public void SetUp() { _warn.Clear(); _log = new PlayLog(); Application.logMessageReceived += OnLog; }
        [TearDown] public void TearDown() { Application.logMessageReceived -= OnLog; _log?.Dispose(); _log = null; Time.timeScale = 1f; }
        void OnLog(string msg, string stack, LogType type) { if (type == LogType.Warning && msg != null && (msg.StartsWith("[UiKit]") || msg.StartsWith("[AssetCatalog]"))) _warn.Add(msg); }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; _warn.Clear();
            yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3); _log.AssertNoRed("종료");
        }
        IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }
        IEnumerable<Text> ActiveTexts() => _app.UiCanvas.GetComponentsInChildren<Text>(false);
        bool HasText(Func<string, bool> pred) { foreach (var t in ActiveTexts()) if (pred(t.text ?? "")) return true; return false; }
        static bool ClickNamed(Transform root, string name) { var t = root != null ? UiKit.Find(root, name) : null; var b = t != null ? t.GetComponent<Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        static int CountNamed(Transform root, string prefix) { int n = 0; if (root == null) return 0; foreach (var t in root.GetComponentsInChildren<Transform>(false)) if (t.name.StartsWith(prefix)) n++; return n; }
        void Check(string where, bool expectOverlay = false)
        {
            _log.AssertNoRed(where);
            if (_warn.Count > 0) { var w = string.Join("\n", _warn); _warn.Clear(); Assert.Fail($"[{where}] 프리팹 경로/카탈로그 키 경고:\n{w}"); }
            foreach (var t in ActiveTexts()) { string s = (t.text ?? "").Trim(); foreach (var d in Demo) if (s == d) Assert.Fail($"[{where}] 영문 데모 글자: {s}"); }
            Assert.AreEqual(expectOverlay, _app.Overlay.IsOpen, $"[{where}] 팝업 열림 = {expectOverlay}");
        }
        static void AtX(RectTransform rt, Layout.R r, string what) { Assert.AreEqual(r.X, rt.anchorMin.x * 100f, 0.5f, what + " x"); Assert.AreEqual(r.X + r.W, rt.anchorMax.x * 100f, 0.5f, what + " 오른쪽"); }
        static void AtY(RectTransform rt, Layout.R r, string what) { Assert.AreEqual(1f - r.Y / 100f, rt.anchorMax.y, 5e-3f, what + " y"); Assert.AreEqual(1f - (r.Y + r.H) / 100f, rt.anchorMin.y, 5e-3f, what + " 아래"); }

        [UnityTest]
        public IEnumerator DungeonArenaPagesAndPopups()
        {
            yield return Boot();
            Assert.AreEqual("lobby", _app.Current.Name);
            var lobby = _app.Current.Root;

            // ① 하단 탭 넷째(던전) → events 화면 던전 페이지
            var tabs = UiKit.Find(lobby, "Tab_01_BottomFlushMenu"); Assert.IsNotNull(tabs, "로비 탭 바");
            var tab = tabs.GetChild(3).GetComponent<Button>(); Assert.IsNotNull(tab, "넷째 탭 버튼(던전)"); tab.onClick.Invoke(); yield return Frames(3);
            Assert.AreEqual("events", _app.Current.Name, "던전 탭 = events 화면");
            var ev = _app.GetScreen<EventsScreen>(); Assert.IsNotNull(ev); Assert.AreEqual(EventsScreen.PageDungeon, ev.Page, "던전 페이지");
            var root = ev.Root; var pg = UiKit.Find(root, "Page:dungeon") as RectTransform; Assert.IsNotNull(pg, "던전 페이지 루트"); Assert.IsTrue(pg.gameObject.activeSelf);
            Assert.IsNotNull(UiKit.Find(root, "TopBar"), "상단 재화 바(공용 TopBar)");
            Assert.IsTrue(HasText(s => s == "던전"), "제목 «던전»"); Assert.IsTrue(HasText(s => s == "던전 티켓은 매일 충전됩니다"), "부제");
            var hell = UiKit.Find(pg, "Card:hell") as RectTransform; var exp = UiKit.Find(pg, "Card:expedition") as RectTransform;
            Assert.IsNotNull(hell, "던전 카드 1"); Assert.IsNotNull(exp, "던전 카드 2"); Assert.IsTrue(HasText(s => s == "지옥의 문") && HasText(s => s == "원정"), "카드 제목 우리말");
            Assert.AreEqual(2, CountNamed(pg, "EnterBtn"), "입장 버튼 2"); Assert.IsTrue(HasText(s => s == "획득 가능"), "«획득 가능»");
            Assert.AreEqual(2, CountNamed(hell, "Cell:"), "카드 1 보상 아이콘 2"); Assert.AreEqual(4, CountNamed(exp, "Cell:"), "카드 2 보상 아이콘 4");
            Assert.IsNotNull(UiKit.Find(pg, "SoonCard"), "준비 중 카드"); Assert.IsTrue(HasText(s => s == "준비 중"), "«준비 중»");
            Assert.IsNotNull(UiKit.Find(pg, "BackBtn"), "뒤로"); Assert.IsNotNull(UiKit.Find(pg, "Tab:dungeon"), "던전 탭"); Assert.IsNotNull(UiKit.Find(pg, "Tab:pvp"), "PvP 탭");
            Assert.IsNull(UiKit.Find(root, "ui.tabBar"), "5탭 바 없음(레퍼런스 20 = 뒤로 + 2탭)");
            AtX(hell, Layout.DgCard1, "카드 1"); AtY(hell, Layout.DgCard1, "카드 1"); AtY(exp, Layout.DgCard2, "카드 2");
            AtY((RectTransform)UiKit.Find(pg, "Foot"), Layout.DgFoot, "바닥 띠"); AtX((RectTransform)UiKit.Find(pg, "BackBtn"), Layout.DgBack, "뒤로"); AtX((RectTransform)UiKit.Find(pg, "Tabs"), Layout.DgTabs, "2탭");
            Check("던전 페이지");

            // ② 카드 1 입장 → 던전 세부 팝업(21) · 소탕/도전은 아무 일 없음 · 배경 탭 닫기
            Assert.IsTrue(ClickNamed(hell, "EnterBtn"), "카드 1 입장"); yield return Frames(2);
            Check("던전 세부 팝업", expectOverlay: true);
            var ov = _app.Overlay.Root;
            Assert.IsTrue(HasText(s => s == "지옥의 문") && HasText(s => s == "층") && HasText(s => s == "보상") && HasText(s => s == "소탕") && HasText(s => s == "도전") && HasText(s => s == "탭하여 닫기"), "세부 팝업 글자(제목·층·보상·소탕·도전·탭하여 닫기)");
            Assert.IsTrue(HasText(s => s == "전설·신화 특전만 등장"), "조건 문구");
            Assert.AreEqual(4, CountNamed(ov, "RewardCell:"), "보상 칸 4"); Assert.IsNotNull(UiKit.Find(ov, "FloorCircle"), "층수 원");
            var box = UiKit.Find(ov, "ui.popup.red") as RectTransform; Assert.IsNotNull(box, "빨간 팝업 패널"); AtX(box, Layout.DdBox, "세부 박스"); AtY(box, Layout.DdBox, "세부 박스");
            Assert.IsNull(UiKit.Find(ov, "Button_Close_01"), "닫기 X 없음");
            Assert.IsTrue(ClickNamed(ov, "SweepBtn") && ClickNamed(ov, "ChallengeBtn") && ClickNamed(ov, "FloorPrev"), "소탕·도전·◀ 누름"); yield return Frames(1);
            Assert.IsTrue(_app.Overlay.IsOpen && _app.Current.Name == "events", "껍데기 버튼은 아무 일 없음");
            Assert.IsTrue(ClickNamed(ov, "Dimmed"), "배경 탭"); yield return Frames(2);
            Check("던전 세부 닫힘");

            // ③ PvP 탭 → 아레나 페이지(22)
            Assert.IsTrue(ClickNamed(pg, "Tab:pvp"), "PvP 탭"); yield return Frames(2);
            Assert.AreEqual(EventsScreen.PagePvp, ev.Page); Assert.IsFalse(pg.gameObject.activeSelf, "던전 페이지 꺼짐");
            var pv = UiKit.Find(root, "Page:pvp") as RectTransform; Assert.IsNotNull(pv, "PvP 페이지"); Assert.IsTrue(pv.gameObject.activeSelf);
            Assert.IsTrue(HasText(s => s == "PvP") && HasText(s => s == "아레나") && HasText(s => s == "브론즈") && HasText(s => s == "준비 중"), "PvP 페이지 글자");
            Assert.IsTrue(HasText(s => s.StartsWith("시즌 종료까지")), "시즌 타이머");
            var arenaCard = UiKit.Find(pv, "Card:arena") as RectTransform; Assert.IsNotNull(arenaCard, "아레나 카드"); AtX(arenaCard, Layout.ArCard, "아레나 카드"); AtY(arenaCard, Layout.ArCard, "아레나 카드");
            Assert.IsNotNull(UiKit.Find(pv, "Tab:pvp"), "PvP 탭(켜짐)");
            Check("PvP 페이지");

            // ④ 입장 → 아레나 입장 화면(23)
            Assert.IsTrue(ClickNamed(pv, "EnterBtn"), "아레나 입장"); yield return Frames(3);
            Assert.AreEqual(EventsScreen.PageArena, ev.Page);
            var ar = UiKit.Find(root, "Page:arena") as RectTransform; Assert.IsNotNull(ar, "아레나 입장 페이지"); Assert.IsTrue(ar.gameObject.activeSelf); Assert.IsFalse(pv.gameObject.activeSelf);
            Assert.IsNotNull(UiKit.Find(ar, "Podium"), "시상대"); Assert.AreEqual(3, CountNamed(ar, "Portrait:"), "시상대 초상 3"); Assert.AreEqual(3, CountNamed(ar, "Banner:"), "시상대 배너 3"); Assert.AreEqual(3, CountNamed(ar, "Crown:"), "왕관 3");
            Assert.AreEqual(7, CountNamed(ar, "RankRow:"), "순위 줄 7(4위~10위)"); Assert.IsTrue(HasText(s => s == "나") && HasText(s => s == "브론즈") && HasText(s => s == "보상") && HasText(s => s == "상인"), "입장 화면 글자");
            Assert.IsTrue(HasText(s => s == "시즌이 끝나면 상위 순위가 승급합니다"), "승급 안내");
            Assert.GreaterOrEqual(UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length, 2, "HeroView 2(상단 바 아바타 + 1위 초상 «나»)");
            Assert.IsNotNull(UiKit.Find(ar, "ChallengeBtn"), "도전"); Assert.IsNotNull(UiKit.Find(ar, "RewardsBtn"), "보상"); Assert.IsNotNull(UiKit.Find(ar, "MerchantBtn"), "상인");
            AtY((RectTransform)UiKit.Find(ar, "Stage"), Layout.AeStage, "무대"); AtX((RectTransform)UiKit.Find(ar, "ChallengeBtn"), Layout.AeChallenge, "도전 버튼"); AtX((RectTransform)UiKit.Find(ar, "RankList"), Layout.AeList, "순위 목록");
            Check("아레나 입장 화면");

            // ⑤ 도전 → 도전 팝업(24) · 줄 버튼·새로고침 아무 일 없음
            Assert.IsTrue(ClickNamed(ar, "ChallengeBtn"), "도전"); yield return Frames(2);
            Check("도전 팝업", expectOverlay: true); ov = _app.Overlay.Root;
            Assert.IsTrue(HasText(s => s == "도전") && HasText(s => s == "무료 새로고침") && HasText(s => s == "탭하여 닫기"), "도전 팝업 글자");
            Assert.AreEqual(5, CountNamed(ov, "FoeRow:"), "상대 줄 5"); Assert.AreEqual(5, CountNamed(ov, "FoeBtn:"), "줄 도전 버튼 5");
            var cbox = UiKit.Find(ov, "ui.popup") as RectTransform; Assert.IsNotNull(cbox); AtX(cbox, Layout.AcBox, "도전 박스"); AtY(cbox, Layout.AcBox, "도전 박스");
            Assert.IsTrue(HasText(s => s == UiKit.Fmt(_app.Power())), "전투력 = 내 값");
            Assert.IsTrue(ClickNamed(ov, "FoeBtn:0") && ClickNamed(ov, "RefreshBtn"), "줄 도전·새로고침 누름"); yield return Frames(1);
            Assert.IsTrue(_app.Overlay.IsOpen, "껍데기 버튼은 아무 일 없음");
            Assert.IsTrue(ClickNamed(ov, "Dimmed"), "배경 탭"); yield return Frames(2);
            Check("도전 팝업 닫힘");

            // ⑥ 보상 → 순위 보상 팝업(25)
            Assert.IsTrue(ClickNamed(ar, "RewardsBtn"), "보상"); yield return Frames(2);
            Check("순위 보상 팝업", expectOverlay: true); ov = _app.Overlay.Root;
            Assert.IsTrue(HasText(s => s == "순위 보상") && HasText(s => s == "일일 보상") && HasText(s => s == "시즌 보상") && HasText(s => s.StartsWith("초기화까지")) && HasText(s => s == "순위 보상은 우편으로 지급됩니다"), "순위 보상 글자");
            Assert.AreEqual(5, CountNamed(ov, "Tier:"), "티어 5"); Assert.AreEqual(4, CountNamed(ov, "RewardRow:"), "보상 줄 4");
            var rbox = UiKit.Find(ov, "ui.popup") as RectTransform; Assert.IsNotNull(rbox); AtX(rbox, Layout.RrBox, "순위 보상 박스"); AtY(rbox, Layout.RrBox, "순위 보상 박스");
            Assert.IsTrue(ClickNamed(ov, "DailyTab") && ClickNamed(ov, "SeasonTab"), "일일/시즌 탭 누름"); yield return Frames(1);
            Assert.IsTrue(_app.Overlay.IsOpen, "탭은 아무 일 없음");
            Assert.IsTrue(ClickNamed(ov, "Dimmed"), "배경 탭"); yield return Frames(2);
            Check("순위 보상 닫힘");

            // ⑦ 상인 → 상인 페이지(26) · 상품은 아무 일 없음
            Assert.IsTrue(ClickNamed(ar, "MerchantBtn"), "상인"); yield return Frames(3);
            Assert.AreEqual(EventsScreen.PageMerchant, ev.Page);
            var me = UiKit.Find(root, "Page:merchant") as RectTransform; Assert.IsNotNull(me, "상인 페이지"); Assert.IsFalse(ar.gameObject.activeSelf);
            Assert.IsTrue(HasText(s => s == "상인") && HasText(s => s == "다이아") && HasText(s => s == "부활 토큰"), "상인 글자");
            Assert.AreEqual(11, CountNamed(me, "Goods:"), "상품 11"); AtX((RectTransform)UiKit.Find(me, "Goods"), Layout.MeGrid, "상품 격자"); AtY((RectTransform)UiKit.Find(me, "Banner"), Layout.MeBanner, "상인 배너");
            Assert.IsTrue(ClickNamed(me, "Goods:0"), "상품 누름"); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen, "상품은 아무 일 없음");
            Check("상인 페이지");

            // ⑧ 뒤로 ×3: 상인 → 아레나 입장 → PvP → 로비
            Assert.IsTrue(ClickNamed(me, "BackBtn"), "상인 뒤로"); yield return Frames(1); Assert.AreEqual(EventsScreen.PageArena, ev.Page, "상인 뒤로 = 아레나 입장");
            Assert.IsTrue(ClickNamed(ar, "BackBtn"), "아레나 뒤로"); yield return Frames(1); Assert.AreEqual(EventsScreen.PagePvp, ev.Page, "아레나 뒤로 = PvP");
            Assert.IsTrue(ClickNamed(pv, "BackBtn"), "PvP 뒤로"); yield return Frames(2); Assert.AreEqual("lobby", _app.Current.Name, "PvP 뒤로 = 로비");
            Check("뒤로 → 로비");

            // ⑨ 로비 오른쪽 아래 «이벤트»(방패) → PvP 페이지 · 던전 탭 → 던전 페이지
            Assert.IsTrue(ClickNamed(lobby, "Side:" + LobbyScreen.SideEvents), "로비 이벤트 버튼"); yield return Frames(2);
            Assert.AreEqual("events", _app.Current.Name); Assert.AreEqual(EventsScreen.PagePvp, ev.Page, "이벤트 버튼 = PvP 페이지");
            Assert.IsTrue(ClickNamed(pv, "Tab:dungeon"), "던전 탭"); yield return Frames(2); Assert.AreEqual(EventsScreen.PageDungeon, ev.Page);
            Assert.IsTrue(ClickNamed(pg, "BackBtn"), "던전 뒤로"); yield return Frames(2); Assert.AreEqual("lobby", _app.Current.Name);
            Check("이벤트 버튼 왕복");
            yield return Shutdown();
        }
    }
}
