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

            // ① 로비 오른쪽 아래 «이벤트» → events 화면 던전 페이지 (T107 · 하단 탭에서 던전은 빠졌고 이벤트를 열면 언제나 던전이 먼저)
            var evBtn = UiKit.Find(lobby, "Events"); Assert.IsNotNull(evBtn, "로비 «이벤트» 버튼");
            var evClick = evBtn.GetComponentInChildren<Button>(true); Assert.IsNotNull(evClick, "«이벤트» 버튼의 Button"); evClick.onClick.Invoke(); yield return Frames(3);
            Assert.AreEqual("events", _app.Current.Name, "«이벤트» = events 화면");
            CollectionAssert.DoesNotContain(NavBar.Keys, "dungeon", "하단 탭에 던전 없음(T107)");
            var ev = _app.GetScreen<EventsScreen>(); Assert.IsNotNull(ev); Assert.AreEqual(EventsScreen.PageDungeon, ev.Page, "던전 페이지");
            var root = ev.Root; var pg = UiKit.Find(root, "Page:dungeon") as RectTransform; Assert.IsNotNull(pg, "던전 페이지 루트"); Assert.IsTrue(pg.gameObject.activeSelf);
            Assert.IsNotNull(UiKit.Find(root, "TopBar"), "상단 재화 바(공용 TopBar)");
            Assert.IsTrue(HasText(s => s == "던전"), "제목 «던전»"); Assert.IsTrue(HasText(s => s == "던전 티켓은 매일 충전됩니다"), "부제");
            var hell = UiKit.Find(pg, "Card:hell") as RectTransform; var exp = UiKit.Find(pg, "Card:expedition") as RectTransform;
            Assert.IsNotNull(hell, "던전 카드 1"); Assert.IsNotNull(exp, "던전 카드 2"); Assert.IsTrue(HasText(s => s == "지옥의 문") && HasText(s => s == "원정"), "카드 제목 우리말");
            Assert.AreEqual(2, CountNamed(pg, "EnterBtn"), "입장 버튼 2"); Assert.IsTrue(HasText(s => s == "획득 가능"), "«획득 가능»");
            Assert.AreEqual(2, CountNamed(hell, "Cell:"), "카드 1 보상 아이콘 2"); Assert.AreEqual(4, CountNamed(exp, "Cell:"), "카드 2 보상 아이콘 4");
            // T101 ⓑ(주인 «준비 중이라 써 있는 거 없애줘») — 20·22 두 페이지에서 사라졌다
            Assert.IsNull(UiKit.Find(pg, "SoonCard"), "준비 중 카드는 없어야 한다(T101 ⓑ)"); Assert.IsFalse(HasText(s => s == "준비 중"), "«준비 중» 글자도 없다");
            // T101 ⓐ — 두 카드 사이에 눈에 보이는 빈칸(카드 높이의 6% 이상)
            Assert.GreaterOrEqual(Layout.DgCard2.Y - (Layout.DgCard1.Y + Layout.DgCard1.H), Layout.DgCard1.H * 0.06f, "카드 1·2 사이 간격 ≥ 카드 높이의 6%(T101 ⓐ)");
            // T101 ⓒ — 카드 자체를 감싸는 직사각형 링(그림 띠만이 아니라 네 변)
            foreach (var c in new[] { hell, exp })
            {
                Transform ring = null; for (int i = 0; i < c.childCount; i++) if (c.GetChild(i).name == UiKit.BorderName) ring = c.GetChild(i);
                Assert.IsNotNull(ring, "카드 rect 에 «Border» 링(T101 ⓒ)");
                var ri = ring.GetComponent<Image>(); Assert.IsNotNull(ri, "링은 Image");
                Assert.IsTrue(ri.sprite != null && ri.sprite.name.Contains("Rectangle"), "직사각형 조각이어야 한다(지금 " + (ri.sprite != null ? ri.sprite.name : "null") + ")");
                Assert.IsFalse(ri.fillCenter, "링은 가운데 비움"); Assert.IsFalse(ri.raycastTarget, "링 raycast 끔");
            }
            // T101 ⓓ — 제목 줄이 가운데(아이콘 + 글자 덩어리의 좌우 여백 차 ≤ 2%p)
            {
                var trow = UiKit.Find(pg, "Title") as RectTransform; Assert.IsNotNull(trow, "제목 줄");
                var tic = UiKit.Find(trow, "Icon") as RectTransform; var ttx = FindText(trow, "Text"); Assert.IsNotNull(tic, "제목 아이콘"); Assert.IsNotNull(ttx, "제목 글자");
                float left = tic.anchorMin.x * 100f, right = 100f - ttx.rectTransform.anchorMax.x * 100f;
                Assert.AreEqual(left, right, 2.0f, "제목 덩어리가 가운데(왼쪽 여백 " + left.ToString("0.0") + " ↔ 오른쪽 " + right.ToString("0.0") + " · T101 ⓓ)");
            }
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
            // 기댓값에 TextGlyphs.Safe 를 씌운다 — 화면에 나갈 때 UiKit 이 «·» 를 «/» 로 바꾼다(T75 · Jua 에 글리프가 없어 폭 0 으로 사라진다)
            Assert.IsTrue(HasText(s => s == TextGlyphs.Safe("전설·신화 특전만 등장")), "조건 문구");
            // T102 ⓐ — 조각이 달고 오던 빨간 장식 선은 꺼져 있다 · ⓑ — 그림 띠가 제목 띠 바닥에 딱 붙는다
            foreach (var t in ov.GetComponentsInChildren<Transform>(true))
                if (t.name.Contains("DecoLine") || t.name.Contains("LineDeco"))
                    Assert.IsFalse(t.gameObject.activeInHierarchy, "세부 팝업의 장식 선은 꺼져 있어야 한다(T102 ⓐ · " + t.name + ")");
            Assert.AreEqual(Layout.DdHead.Y + Layout.DdHead.H, Layout.DdPic.Y, 0.1f, "그림 띠 y = 제목 띠 바닥(T102 ⓑ)");
            { var picRt = UiKit.Find(ov, "Pic") as RectTransform; Assert.IsNotNull(picRt, "그림 띠"); AtY(picRt, Layout.DdPic.Within(Layout.DdBox), "그림 띠"); }
            Assert.AreEqual(4, CountNamed(ov, "RewardCell:"), "보상 칸 4"); Assert.IsNotNull(UiKit.Find(ov, "FloorCircle"), "층수 원");
            var box = UiKit.Find(ov, "ui.popup.red") as RectTransform; Assert.IsNotNull(box, "빨간 팝업 패널"); AtX(box, Layout.DdBox, "세부 박스"); AtY(box, Layout.DdBox, "세부 박스");
            Assert.IsNull(UiKit.Find(ov, "Button_Close_01"), "닫기 X 없음");
            { var arrowImg = UiKit.Find(ov, "FloorPrev")?.GetComponent<Image>(); Assert.IsNotNull(arrowImg, "층수 ◀"); Assert.AreNotEqual(Palette.Cream, arrowImg.color, "층수 ◀ 는 크림 패널과 다른 색(크림이면 안 보임 · T43 비평 회차 1)"); }
            Assert.IsTrue(ClickNamed(ov, "SweepBtn") && ClickNamed(ov, "ChallengeBtn") && ClickNamed(ov, "FloorPrev"), "소탕·도전·◀ 누름"); yield return Frames(1);
            Assert.IsTrue(_app.Overlay.IsOpen && _app.Current.Name == "events", "껍데기 버튼은 아무 일 없음");
            Assert.IsTrue(ClickNamed(ov, "Dimmed"), "배경 탭"); yield return Frames(2);
            Check("던전 세부 닫힘");

            // ③ PvP 탭 → 아레나 페이지(22)
            Assert.IsTrue(ClickNamed(pg, "Tab:pvp"), "PvP 탭"); yield return Frames(2);
            Assert.AreEqual(EventsScreen.PagePvp, ev.Page); Assert.IsFalse(pg.gameObject.activeSelf, "던전 페이지 꺼짐");
            var pv = UiKit.Find(root, "Page:pvp") as RectTransform; Assert.IsNotNull(pv, "PvP 페이지"); Assert.IsTrue(pv.gameObject.activeSelf);
            Assert.IsTrue(HasText(s => s == "PvP") && HasText(s => s == "아레나") && HasText(s => s == "브론즈"), "PvP 페이지 글자"); Assert.IsFalse(HasText(s => s == "준비 중"), "«준비 중» 은 22 에서도 삭제(T101 ⓑ)");
            Assert.IsTrue(HasText(s => s.StartsWith("시즌 종료까지")), "시즌 타이머");
            var arenaCard = UiKit.Find(pv, "Card:arena") as RectTransform; Assert.IsNotNull(arenaCard, "아레나 카드"); AtX(arenaCard, Layout.ArCard, "아레나 카드"); AtY(arenaCard, Layout.ArCard, "아레나 카드");
            Assert.IsNotNull(UiKit.Find(pv, "Tab:pvp"), "PvP 탭(켜짐)");
            Check("PvP 페이지");

            // ④ 입장 → 아레나 입장 화면(23)
            Assert.IsTrue(ClickNamed(pv, "EnterBtn"), "아레나 입장"); yield return Frames(3);
            Assert.AreEqual(EventsScreen.PageArena, ev.Page);
            var ar = UiKit.Find(root, "Page:arena") as RectTransform; Assert.IsNotNull(ar, "아레나 입장 페이지"); Assert.IsTrue(ar.gameObject.activeSelf); Assert.IsFalse(pv.gameObject.activeSelf);
            Assert.IsNotNull(UiKit.Find(ar, "Podium"), "시상대"); Assert.AreEqual(3, CountNamed(ar, "Portrait:"), "시상대 초상 3"); Assert.AreEqual(3, CountNamed(ar, "Banner:"), "시상대 배너 3"); Assert.AreEqual(3, CountNamed(ar, "Crown:"), "왕관 3");
            Assert.AreEqual(7, CountNamed(ar, "RankRow:"), "순위 줄 7(4위~10위)");
            { var face = UiKit.Find(UiKit.Find(ar, "RankRow:4"), "Face") as RectTransform; Assert.IsNotNull(face, "순위 줄 초상"); Assert.Less(face.anchorMax.x, 0.35f, "순위 줄 초상은 줄 왼쪽(등수 옆 · 레퍼런스 23) — FitInParent 가 줄 가운데로 보내던 회귀(T43 비평 회차 1)"); } Assert.IsTrue(HasText(s => s == "나") && HasText(s => s == "브론즈") && HasText(s => s == "보상") && HasText(s => s == "상인"), "입장 화면 글자");
            Assert.IsTrue(HasText(s => s == "시즌이 끝나면 상위 순위가 승급합니다"), "승급 안내");
            // T62 — 시상대 배너 = Social_Ranking 조각(Cloth + Group_Trophy) · 순위 줄 = ListItem_Ranking 조각(Text_RankingNum + ProfileArea→Face + Group_Trophy)
            { var b1 = UiKit.Find(ar, "Banner:1"); Assert.IsNotNull(UiKit.Find(b1, "Cloth"), "시상대 배너 조각(Social_Ranking/Podium)"); Assert.IsNotNull(UiKit.Find(b1, "Group_Trophy"), "배너 🏆 무리"); }
            { var r4 = UiKit.Find(ar, "RankRow:4"); Assert.IsNotNull(UiKit.Find(r4, "Text_RankingNum"), "순위 줄 등수(ListItem_Ranking 조각)"); Assert.IsNotNull(UiKit.Find(r4, "Group_Trophy"), "순위 줄 🏆 무리"); Assert.IsNull(UiKit.Find(r4, "ProfileArea"), "초상 자리는 «Face» 로 이름 바꾼다");
              // 회차 1 감점: ListFrame_02 는 Theme_Light 의 밝은 크림 줄이라 레퍼런스(어두운 줄)와 정반대 + 흰 이름 글자가 안 읽혔다 → 어둡게 덮는다
              var bg = UiKit.Find(r4, "ListFrame_02/Normal/Bg"); Assert.IsNotNull(bg, "순위 줄 프레임 몸통"); var bgc = bg.GetComponent<Image>().color; Assert.Less(bgc.r + bgc.g + bgc.b, 1.5f, "순위 줄은 어두워야 한다(밝은 크림 프리팹 색 회귀)"); }
            Assert.IsTrue(HasText(s => s == "도전자 4") && HasText(s => s == "꼬마기사"), "순위 이름(껍데기 · 상대 «도전자 N» · 내 자리 «꼬마기사»)");
            Assert.GreaterOrEqual(UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length, 2, "HeroView 2(상단 바 아바타 + 1위 초상 «나»)");
            Assert.IsNotNull(UiKit.Find(ar, "ChallengeBtn"), "도전"); Assert.IsNotNull(UiKit.Find(ar, "RewardsBtn"), "보상"); Assert.IsNotNull(UiKit.Find(ar, "MerchantBtn"), "상인");
            AtY((RectTransform)UiKit.Find(ar, "Stage"), Layout.AeStage, "무대"); AtX((RectTransform)UiKit.Find(ar, "ChallengeBtn"), Layout.AeChallenge, "도전 버튼"); AtX((RectTransform)UiKit.Find(ar, "RankList"), Layout.AeList, "순위 목록");
            Check("아레나 입장 화면");

            // ⑤ 도전 → 도전 팝업(24) · 줄 버튼·새로고침 아무 일 없음
            Assert.IsTrue(ClickNamed(ar, "ChallengeBtn"), "도전"); yield return Frames(2);
            Check("도전 팝업", expectOverlay: true); ov = _app.Overlay.Root;
            Assert.IsTrue(HasText(s => s == "도전") && HasText(s => s == "무료 새로고침") && HasText(s => s == "탭하여 닫기"), "도전 팝업 글자");
            Assert.AreEqual(5, CountNamed(ov, "FoeRow:"), "상대 줄 5"); Assert.AreEqual(5, CountNamed(ov, "FoeBtn:"), "줄 도전 버튼 5");
            { var face = UiKit.Find(UiKit.Find(ov, "FoeRow:0"), "Face") as RectTransform; Assert.IsNotNull(face, "상대 줄 초상"); Assert.Less(face.anchorMax.x, 0.3f, "상대 줄 초상은 줄 왼쪽(레퍼런스 24) — T43 비평 회차 1 회귀"); }
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
            Assert.AreEqual(11, CountNamed(me, "Goods:"), "상품 11"); Assert.IsNotNull(UiKit.Find(me, "Goods:0").GetComponent<RectMask2D>(), "상품 카드는 카드 사각형으로 클립(CardFrame_04 고정 폭 자식이 삐져나오던 것 · T43 비평 회차 1)"); AtX((RectTransform)UiKit.Find(me, "Goods"), Layout.MeGrid, "상품 격자"); AtY((RectTransform)UiKit.Find(me, "Banner"), Layout.MeBanner, "상인 배너");
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

        // ───────────────────────── T63-events: 글자 가독성 ─────────────────────────

        /// <summary>한 화면의 활성 글자를 모아 «실제로 그려지는 크기»(bestFit 결과)가 보조 하한(36) 밑으로 내려가지 않는지 · 선호 크기가 칸을 안 넘는지 본다.
        /// <paramref name="skip"/> 로 시작하는 경로는 다른 하위 행 담당이라 아예 뺀다(TopBar = T63-lobby · TapToClose = T63-toast).</summary>
        void Readable(string screen, Transform root, params string[] skip) => Readable(screen, root, null, skip);

        /// <summary><paramref name="clipSkip"/> 로 시작하는 경로는 «크기» 만 보고 «잘림» 은 안 본다 — 칸이 프리팹 안에 있어 이 작업이 못 정하는 자리(23 의 `Social_Ranking`·`ListItem_Ranking` 조각 = T62).</summary>
        void Readable(string screen, Transform root, string[] clipSkip, params string[] skip)
        {
            Canvas.ForceUpdateCanvases();
            var rows = TextAudit.Collect(screen, root);
            Assert.Greater(rows.Count, 3, $"[{screen}] 글자가 거의 안 모였다(수집 실패)");
            var small = new List<string>(); var clipped = new List<string>();
            foreach (var r in rows)
            {
                bool skipIt = false;
                foreach (var sk in skip) if (r.Path != null && r.Path.StartsWith(sk)) { skipIt = true; break; }
                if (skipIt) continue;
                if (r.Used < TextSize.Aux) small.Add(r.ToString());
                bool clipExempt = false;
                if (clipSkip != null) foreach (var ck in clipSkip) if (r.Path != null && r.Path.StartsWith(ck)) { clipExempt = true; break; }
                if (r.Clipped && !clipExempt) clipped.Add(r.ToString());
            }
            Assert.AreEqual(0, small.Count, $"[{screen}] 실제 그려지는 크기가 보조 36 미만(T63 · 주인 «글씨가 너무 작아 안 읽힌다»):\n" + string.Join("\n", small));
            Assert.AreEqual(0, clipped.Count, $"[{screen}] 잘림/넘침(선호 크기 > 칸 · 칸 h ≥ 크기 × 1.4 로 잡는다):\n" + string.Join("\n", clipped));
        }

        static Text FindText(Transform root, string path)
        {
            var t = UiKit.Find(root, path); Assert.IsNotNull(t, $"«{path}» 를 못 찾음");
            var txt = t.GetComponent<Text>(); if (txt == null) txt = t.GetComponentInChildren<Text>(true);
            Assert.IsNotNull(txt, $"«{path}» 안에 글자가 없음"); return txt;
        }
        static int MaxSize(Text t) => t.resizeTextForBestFit ? Mathf.Max(t.fontSize, t.resizeTextMaxSize) : t.fontSize;

        /// <summary>
        /// T63-events(⑨ 던전·아레나 20~26) — 20·21·22·23·24·25·26 의 모든 활성 글자가 «실제 36 이상 · 잘림 0» 인지.
        /// 23(아레나 입장)은 T62(순위 화면 = <c>Social_Ranking</c> 프리팹 변형)가 ✅ 로 닫힌 뒤 회차 2 에서 넣었다 — 프리팹 조각 안 칸(순위 줄·시상대 배너)은 «잘림» 면제.
        /// 자리 수치는 <c>Layout</c> ⑫~⑱ 와 `docs/ref-layout.md` ⚑ T63-events 회차 정정(칸 h ≥ 글자 크기 × 1.4)이 정본.
        /// </summary>
        [UnityTest]
        public IEnumerator EventsTextsAreReadable()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(3);
            var ev = _app.GetScreen<EventsScreen>(); var root = _app.Current.Root;

            // 20 던전 — 제목 60 · 부제 40 · 카드 제목 48 · «획득 가능» 36 · 탭 라벨 36 (T101 ⓑ 로 «준비 중» 은 없어졌다)
            var pg = UiKit.Find(root, "Page:" + EventsScreen.PageDungeon);
            Readable("20_dungeon", pg);
            Assert.AreEqual(TextSize.Title, MaxSize(FindText(pg, "Title")), "던전 제목 = 제목 60");
            Assert.GreaterOrEqual(MaxSize(FindText(UiKit.Find(pg, "Card:hell"), "Head")), TextSize.Body, "카드 제목 띠(48)는 본문 40 하한 위");

            // 21 던전 세부 팝업 — 제목 띠 60 · 조건 문구 40 · «첫 클리어» 배지 36 · 티켓 수 = 흰 글자 + 검은 아웃라인(T111 ⓑ)
            Assert.IsTrue(ClickNamed(UiKit.Find(pg, "Card:hell"), "EnterBtn"), "던전 입장"); yield return Frames(3);
            Readable("21_dungeon_detail", _app.Overlay.Root, "TapToClose");
            var ticket = FindText(_app.Overlay.Root, "Ticket");
            // 주인 2026-09-07 07:5X «모든 글씨 중에 검정 글씨 → 흰 글씨로 바꿔야 함 · 검정 아웃라인으로 통일시켰기 때문에»(T111 ⓑ) —
            // 전에는 «크림 패널 위라 잉크색» 이 규칙이었고 이 줄이 그것을 못 박고 있었다. 이제 크림 패널 위에서도 흰 글자이고,
            // 읽히게 하는 몫은 검은 아웃라인(T63-outline)이 맡는다 → 기댓값을 «밝은 글자 + 아웃라인 있음» 으로 뒤집는다(결정 274).
            Assert.GreaterOrEqual(UiKit.Luma(ticket.color), UiKit.TextLumaMin, "크림 패널 위 티켓 수도 흰 글자다(T111 ⓑ)");
            Assert.IsNotNull(ticket.GetComponent<Outline>(), "그 흰 글자에는 검은 아웃라인이 붙어 있어야 읽힌다(T63-outline)");
            _app.Overlay.Close(); yield return Frames(2);

            // 22 PvP — 시즌 타이머 36(칸 h2.3) · 티어 줄 40
            ev.ShowPage(EventsScreen.PagePvp); yield return Frames(3);
            Readable("22_arena", UiKit.Find(root, "Page:" + EventsScreen.PagePvp));

            // 23 아레나 입장 — 티어 제목 60 · 시즌 타이머 36 · 오른쪽 «보상»·«상인» 36 · 왕관 번호 36 · «나» 꼬리표 36 · 승급 안내 40
            // 순위 줄·시상대 배너의 글자 칸은 T62 가 쓰는 프리팹 조각(ListItem_Ranking · Social_Ranking) 안이라 «잘림» 은 면제하고 크기만 본다.
            ev.ShowPage(EventsScreen.PageArena); yield return Frames(3);
            var ae = UiKit.Find(root, "Page:" + EventsScreen.PageArena);
            Readable("23_arena_enter", ae, new[] { "RankList", "Podium/Banner" });
            Assert.AreEqual(TextSize.Title, MaxSize(FindText(ae, "TierTitle")), "아레나 티어 제목 = 제목 60");

            // 24 도전 팝업 · 25 순위 보상 팝업
            Assert.IsTrue(ClickNamed(root, "ChallengeBtn"), "도전 버튼"); yield return Frames(3);
            Readable("24_arena_challenge", _app.Overlay.Root, "TapToClose");
            _app.Overlay.Close(); yield return Frames(2);
            Assert.IsTrue(ClickNamed(root, "RewardsBtn"), "보상 버튼"); yield return Frames(3);
            Readable("25_arena_rank_reward", _app.Overlay.Root, "TapToClose");
            _app.Overlay.Close(); yield return Frames(2);

            // 26 상인 — 제목 60 · 타이머 36 · 카드 제목 40 · «한도» 는 크림 카드 위라 잉크색
            ev.ShowPage(EventsScreen.PageMerchant); yield return Frames(3);
            var me = UiKit.Find(root, "Page:" + EventsScreen.PageMerchant);
            Readable("26_arena_shop", me);
            Assert.AreEqual(TextSize.Title, MaxSize(FindText(me, "Title")), "상인 제목 = 제목 60");
            _log.AssertNoRed("글자 가독성(20·21·22·23·24·25·26)");
            yield return Shutdown();
        }

        // ───────────────────────── T81: 아레나 적 승점·전투력 더미값 ─────────────────────────

        static readonly System.Text.RegularExpressions.Regex Commaed =
            new System.Text.RegularExpressions.Regex(@"^\d{1,3}(,\d{3})*$");

        static double ParseNum(string s, string what)
        {
            Assert.IsNotNull(s, what + " 글자가 없다");
            string t = s.Trim();
            Assert.AreNotEqual("0", t, what + " 가 아직 «0» 이다(더미값이 안 들어갔다)");
            Assert.AreNotEqual("—", t, what + " 가 아직 «—» 다(계수 표를 못 읽었다)");
            Assert.IsTrue(Commaed.IsMatch(t), what + " 는 천 단위 콤마 숫자여야 한다: «" + t + "»");
            return double.Parse(t.Replace(",", ""), System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// T81(주인 2026-09-07 «아레나 부분에 적들 승점이랑 전투력 더미값으로 넣어줘라») — 순위 목록(23)·도전 5줄(24)·시상대 배너의
        /// 승점·전투력이 «0»·«—» 이 아니고 천 단위 콤마이며, 순위가 내려갈수록 낮아지는지. 값 규칙 자체는 EditMode <c>ArenaDummyTests</c> 가 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator ArenaDummyNumbersAreFilled()
        {
            yield return Boot();
            Assert.IsNotNull(_app.Data.ArenaDummy, "arenaDummy.json 이 로드돼야 한다(카탈로그 data.arenaDummy)");
            EventsScreen.Open(_app, EventsScreen.PageArena); yield return Frames(3);
            var ev = _app.GetScreen<EventsScreen>(); var root = _app.Current.Root;
            var ae = UiKit.Find(root, "Page:" + EventsScreen.PageArena);

            // 23 순위 목록(4위~) — ⚔ 전투력(Text_GuildName) · 🏆 승점(Text_Value)
            double prevP = double.MaxValue, prevS = double.MaxValue;
            int rows = 0;
            for (int rank = 4; rank <= 10; rank++)
            {
                var row = UiKit.Find(ae, "RankRow:" + rank); if (row == null) continue;
                rows++;
                double p = ParseNum(FindText(row, "Text_GuildName").text, "순위 " + rank + " 전투력");
                double sc = ParseNum(FindText(row, "Text_Value").text, "순위 " + rank + " 승점");
                Assert.LessOrEqual(p, prevP, "순위 " + rank + " 전투력이 위 순위보다 높다");
                Assert.LessOrEqual(sc, prevS, "순위 " + rank + " 승점이 위 순위보다 높다");
                prevP = p; prevS = sc;
            }
            Assert.Greater(rows, 3, "순위 줄을 못 찾았다");
            Assert.Less(prevP, _app.Power(), "아래 순위 전투력은 내 전투력보다 낮아야 한다");

            // 시상대 배너 1·2·3 — 🏆 승점
            for (int i = 1; i <= 3; i++)
            {
                var b = UiKit.Find(ae, "Banner:" + i); Assert.IsNotNull(b, "시상대 배너 " + i);
                ParseNum(FindText(b, "Text_Value").text, "시상대 " + i + "위 승점");
            }

            // 24 도전 팝업 — 상대 5줄(이름 «도전자 N» · ⚔ 전투력 · 🏆 승점) · 오른쪽 위 내 전투력
            Assert.IsTrue(ClickNamed(root, "ChallengeBtn"), "도전 버튼"); yield return Frames(3);
            var box = _app.Overlay.Root;
            int foes = 0;
            for (int i = 0; i < 5; i++)
            {
                var row = UiKit.Find(box, "FoeRow:" + i); if (row == null) continue;
                foes++;
                var pills = row.GetComponentsInChildren<Text>(false);
                int found = 0; foreach (var t in pills) { string tx = (t.text ?? "").Trim(); if (Commaed.IsMatch(tx)) found++; }
                Assert.GreaterOrEqual(found, 2, "상대 줄 " + i + " 에 전투력·승점 두 숫자가 있어야 한다");
                Assert.IsTrue(HasText(x => x.Contains("도전자")), "상대 줄에 이름이 있어야 한다");
            }
            Assert.AreEqual(5, foes, "도전 팝업 상대 5줄");
            Assert.IsTrue(HasText(x => x.Trim() == UiKit.FmtComma(_app.Power())), "도전 팝업 오른쪽 위 = 내 실제 전투력(콤마)");
            _app.Overlay.Close(); yield return Frames(2);
            Check("아레나 더미값", false);
            yield return Shutdown();
        }
    }
}
