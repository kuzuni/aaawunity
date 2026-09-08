using System;
using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T96-menu — 로비 «메뉴»(≡)가 데모 프리팹 <c>Lobby_Menu</c> 로 뜨고, 여섯 항목이 각 팝업을 열며,
    /// 로비에는 중복 버튼이 남지 않고, 받을 것이 있을 때만 빨간 점이 뜨는가(주인 2026-09-07).
    /// <see cref="UiSmokeTests"/> 는 다른 워커가 만지는 중이라 여기 따로 둔다(빨간 줄 0 은 <see cref="PlayLog"/>).
    /// </summary>
    public class LobbyMenuTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }
        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        void ClickNamed(Transform root, string name)
        {
            var t = UiKit.Find(root, name); Assert.IsNotNull(t, "버튼 " + name);
            var b = t.GetComponent<Button>(); Assert.IsNotNull(b, "버튼 컴포넌트 " + name);
            b.onClick.Invoke();
        }
        bool HasText(Func<string, bool> pred) { foreach (var t in _app.UiCanvas.GetComponentsInChildren<TMP_Text>(false)) if (pred(t.text ?? "")) return true; return false; }
        /// <summary>메뉴에 남는 항목 — T148(주인 «데일리기프트, 퀘스트, 출석, 특권은 로비에 걍 꺼내놓는게 나은듯 · 전처럼»)로 <b>둘</b>이 됐다.</summary>
        static readonly string[] Items = { LobbyMenu.ItemMail, LobbyMenu.ItemSettings };
        /// <summary>T148 로 <b>로비로 돌아간</b> 넷 — 메뉴에는 없고 사이드 기둥에 있다(이 자가 양쪽을 다 본다).</summary>
        static readonly string[] BackToLobby = { LobbyScreen.SidePrivilege, LobbyScreen.SideAttendance, LobbyScreen.SideDailyGift, LobbyScreen.SideQuest };
        /// <summary>드롭다운 판 윗변이 ≡ 버튼 윗변보다 위로 올라가도 봐 주는 한도(프레임 px · T139) — 실측 5px(주인이 준 자리)라 한 줄(메뉴 줄 113.9px)의 1/4 을 잡았다. 판이 버튼 «위» 로 떠 버리면(수백 px) 여기서 걸린다.</summary>
        const float PanelOverButtonPx = 28f;

        [UnityTest]
        public IEnumerator MenuOpensThePrefabWithSixRowsInTheOwnersOrder()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);
            var lobby = _app.Current.Root;

            // T148 — 넷은 «전처럼» 로비 사이드 기둥에 있다(T96-menu 가 지웠던 것을 주인 지시로 되살렸다)
            Assert.IsNotNull(UiKit.Find(lobby, "SideL"), "좌 사이드 기둥(특권)"); Assert.IsNotNull(UiKit.Find(lobby, "SideR"), "우 사이드 기둥(출석·데일리 기프트·퀘스트)");
            foreach (var k in BackToLobby)
                Assert.IsNotNull(UiKit.Find(lobby, "Side:" + k), "로비 사이드 칸: " + k);

            ClickNamed(lobby, "Button_Menu"); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "메뉴가 열린다");
            var ov = _app.Overlay.Root;
            var panel = UiKit.Find(ov, LobbyMenu.PanelName);
            Assert.IsNotNull(panel, "프리팹 판(HambergerMenu) — 우리 격자로 다시 만들지 않는다");
            Assert.IsNotNull(UiKit.Find(ov, "Dimmed"), "프리팹 어둠");

            // 남은 두 줄이 순서대로 · 라벨은 우리말 · 나머지 프리팹 줄은 꺼져 있다
            int shown = 0;
            for (int i = 0; i < Items.Length; i++)
            {
                var row = UiKit.Find(ov, "Menu:" + Items[i]);
                Assert.IsNotNull(row, "메뉴 줄 " + Items[i]);
                Assert.IsTrue(row.gameObject.activeInHierarchy, "메뉴 줄이 보인다: " + Items[i]);
                Assert.AreEqual(i, row.GetSiblingIndex(), "메뉴 순서 " + Items[i]);
                shown++;
            }
            Assert.AreEqual(2, shown, "메뉴 항목 2(T148 로 넷이 로비로 갔다)");
            foreach (var s in new[] { "우편함", "설정" })
                Assert.IsTrue(HasText(x => x == s), "메뉴 라벨 «" + s + "»");
            // 로비로 간 넷은 메뉴 «안» 에 줄이 없다 — 한 화면에 같은 입구가 둘이 되지 않게
            foreach (var k in new[] { LobbyMenu.ItemDailyGift, LobbyMenu.ItemQuest, LobbyMenu.ItemAttendance, LobbyMenu.ItemPrivilege })
                Assert.IsNull(UiKit.Find(ov, "Menu:" + k), "메뉴에 남아 있으면 안 되는 줄: " + k);
            _log.AssertNoRed("메뉴 열림");

            // 줄 높이·폭은 프리팹 그대로(T148 로 복제가 없어졌어도 남은 줄이 프리팹 값이어야 한다)
            var first = (RectTransform)UiKit.Find(ov, "Menu:" + Items[0]);
            var last = (RectTransform)UiKit.Find(ov, "Menu:" + Items[Items.Length - 1]);
            Assert.AreEqual(first.rect.height, last.rect.height, 0.5f, "두 줄의 높이가 같다(프리팹 값)");
            Assert.AreEqual(first.rect.width, last.rect.width, 0.5f, "두 줄의 폭이 같다(프리팹 값)");

            yield return Shutdown();
        }

        /// <summary>
        /// T139 — 주인이 스크린샷으로 준 두 가지. ⓐ <b>어둠(Dimmed)을 누르면 닫힌다</b>(«딤 눌러도 꺼지게») ·
        /// ⓑ <b>판 자리</b>가 인스펙터 값 그대로다(앵커 (1,1) · Pivot (0.5,0.5) · x −323 · 가로 382.62 · <b>윗변</b>은 주인 값에서 뽑은 자리 · 세로는 줄 수가 정한다 = T148 로 둘이라 458.87).
        /// 자리는 주인 인스펙터 값이 정본이라 그 값으로 단언하고, «드롭다운이 ≡ 버튼 아래로 늘어진다» 는 성질은 프레임 좌표로 따로 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator DimClickClosesTheMenuAndThePanelSitsWhereTheOwnerPutIt()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);
            var lobby = _app.Current.Root;

            ClickNamed(lobby, "Button_Menu"); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "메뉴가 열린다");
            var ov = _app.Overlay.Root;

            // ⓑ 판 자리 = 주인 인스펙터 값
            var panel = (RectTransform)UiKit.Find(ov, LobbyMenu.PanelName);
            Assert.IsNotNull(panel, "프리팹 판(HambergerMenu)");
            Assert.AreEqual(1f, panel.anchorMin.x, 1e-3f, "판 앵커 Min x = 1(주인 값)");
            Assert.AreEqual(1f, panel.anchorMin.y, 1e-3f, "판 앵커 Min y = 1");
            Assert.AreEqual(panel.anchorMin, panel.anchorMax, "판 앵커 Min == Max(= sizeDelta 가 곧 크기)");
            Assert.AreEqual(0.5f, panel.pivot.x, 1e-3f, "판 Pivot x = 0.5");
            Assert.AreEqual(0.5f, panel.pivot.y, 1e-3f, "판 Pivot y = 0.5");
            Assert.AreEqual(LobbyMenu.PanelX, panel.anchoredPosition.x, 0.5f, "판 Pos x = −323(주인 값)");
            Assert.AreEqual(LobbyMenu.PanelWidth, panel.rect.width, 1f, "판 가로 = 382.62(주인 값)");
            // T148 — 줄이 둘이 되어 세로는 프리팹 값(458.87)이다. **고정하는 것은 «가운데» 가 아니라 «윗변»** 이라
            // 줄 수가 바뀌어도 판은 늘 ≡ 버튼 바로 아래에서 시작한다(주인이 준 자리에서 뽑은 값 · 결정 377 이 미리 짚어 둔 자리).
            Assert.AreEqual(458.87f, panel.rect.height, 1f, "판 세로 = 프리팹 458.87(줄 둘이라 6/4 로 늘리지 않는다)");
            Assert.AreEqual(LobbyMenu.PanelTopY, panel.anchoredPosition.y + panel.rect.height * 0.5f, 0.5f,
                "판 윗변은 주인 값에서 뽑은 자리 그대로여야 한다(줄 수가 바뀌어도 여기서 시작한다)");

            // 드롭다운이 ≡ 버튼에서 «아래로» 늘어지는가 — 프레임 좌표로(자리 값이 다른 rect 기준이면 여기서 드러난다).
            // 판 윗변은 버튼 윗변과 «거의 같다»(실측 955 ↔ 950 = 5px 위로 겹친다 — 드롭다운이 버튼을 살짝 물고 내려오는 꼴이고
            // 주인이 스크린샷으로 준 자리가 그렇다). 그래서 재는 것은 ⓐ 판이 버튼 «위로 떠 있지 않다»(윗변 차이가 한 줄보다 작다)와
            // ⓑ 판이 버튼보다 «아래로» 뻗는다(밑변이 버튼 밑변보다 낮다) 둘이다 — 회차 1 의 «윗변 ≤ 버튼 윗변» 은 5px 때문에 빨개졌다(결정 379).
            var menuBtn = (RectTransform)UiKit.Find(lobby, "Button_Menu");
            var frame = _app.Frame;
            Canvas.ForceUpdateCanvases();
            (float top, float bottom) FrameEdges(RectTransform rt)
            {
                var c = new Vector3[4]; rt.GetWorldCorners(c);
                return (frame.InverseTransformPoint(c[1]).y, frame.InverseTransformPoint(c[0]).y);   // 프레임 y — 위가 크다
            }
            var btnE = FrameEdges(menuBtn); var panelE = FrameEdges(panel);
            Debug.Log($"[T139] 판 프레임 rect = {frame.InverseTransformPoint(panel.position)} · 크기 {panel.rect.width:0.0}×{panel.rect.height:0.0} · "
                + $"≡ 버튼 윗변 {btnE.top:0}/밑변 {btnE.bottom:0} · 판 윗변 {panelE.top:0}/밑변 {panelE.bottom:0}");
            Assert.LessOrEqual(panelE.top - btnE.top, PanelOverButtonPx, "드롭다운 판이 ≡ 버튼 위로 떠 있다(윗변이 버튼 윗변보다 한 줄 이상 높다)");
            Assert.Less(panelE.bottom, btnE.bottom, "드롭다운 판은 ≡ 버튼보다 아래로 늘어진다(밑변이 버튼 밑변보다 낮다)");

            // ⓐ 어둠을 누르면 닫힌다
            var dim = UiKit.Find(ov, "Dimmed");
            Assert.IsNotNull(dim, "프리팹 어둠");
            var dimBtn = dim.GetComponent<Button>();
            Assert.IsNotNull(dimBtn, "어둠에 «누르면 닫기» 가 붙어 있다(T139 ⓐ · OpenPrefab(closeOnDim: true))");
            dimBtn.onClick.Invoke(); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "어둠을 누르면 메뉴가 닫힌다(주인 «딤 눌러도 꺼지게»)");

            // 기본값은 종전 그대로다 — 인자를 안 주면 어둠은 닫기 버튼이 되지 않는다(주인이 말한 것은 메뉴 하나 · 나머지 여섯 자리 불변).
            // 퀘스트·출석 팝업처럼 «제 손으로» 어둠에 닫기를 붙여 둔 자리가 있어 화면 코드로 재면 헷갈린다 → 헬퍼를 직접 부른다.
            _app.Overlay.OpenPrefab("ui.lobbyMenu"); yield return Frames(1);
            var plainDim = UiKit.Find(_app.Overlay.Root, "Dimmed");
            Assert.IsNotNull(plainDim, "같은 조각의 어둠");
            Assert.IsNull(plainDim.GetComponent<Button>(), "OpenPrefab 기본값(closeOnDim: false)에서는 어둠에 «누르면 닫기» 가 안 붙는다");
            _app.Overlay.Close(); yield return Frames(1);

            _log.AssertNoRed("T139 메뉴 드롭다운");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator EveryRowOpensItsPopupAndTheMailHookIsCalled()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);
            var lobby = _app.Current.Root;

            // T148 — 메뉴에 남은 것은 설정뿐이고, 나머지 셋은 «로비 사이드 칸» 이 연다(주인 «전처럼»)
            ClickNamed(lobby, "Button_Menu"); yield return Frames(2);
            ClickNamed(_app.Overlay.Root, "Menu:" + LobbyMenu.ItemSettings); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "설정 팝업이 열린다");
            Assert.IsTrue(HasText(s => s.Contains("음악")), "설정 내용 «음악»");
            Assert.IsNull(UiKit.Find(_app.Overlay.Root, LobbyMenu.PanelName), "메뉴는 닫히고 갈아 끼운다");
            _app.Overlay.Close(); yield return Frames(1);

            (string key, string mark)[] side =
            {
                (LobbyScreen.SideDailyGift, "데일리 기프트"), (LobbyScreen.SideQuest, "퀘스트"), (LobbyScreen.SideAttendance, "출석 보상"),
            };
            foreach (var p in side)
            {
                ClickNamed(lobby, "Side:" + p.key); yield return Frames(2);
                Assert.IsTrue(_app.Overlay.IsOpen, p.key + ": 로비 칸이 팝업을 연다");
                Assert.IsTrue(HasText(s => s.Contains(p.mark)), p.key + ": 내용 «" + p.mark + "»");
                _app.Overlay.Close(); yield return Frames(1);
            }
            _log.AssertNoRed("로비 사이드 팝업 3종 + 설정");

            // 특권 = 페이지(이것도 로비 칸으로 돌아갔다)
            ClickNamed(lobby, "Side:" + LobbyScreen.SidePrivilege); yield return Frames(3);
            Assert.AreEqual("privilege", _app.Current.Name, "특권 페이지로 간다");
            _app.ShowScreen("lobby"); yield return Frames(2); lobby = _app.Current.Root;

            // 우편함 = T96-mail 로 실물이 됐다(훅 폐기) — 항목을 누르면 주인 지목 프리팹 팝업이 뜬다
            ClickNamed(lobby, "Button_Menu"); yield return Frames(2);
            ClickNamed(_app.Overlay.Root, "Menu:" + LobbyMenu.ItemMail); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "우편함이 열린다");
            Assert.IsNotNull(UiKit.FindAny(_app.Overlay.Root, "ui.mailbox", "ui.mailboxEmpty"), "우편함 = Rewards_Mailbox(_Empty) 조각(T96-mail)");
            _log.AssertNoRed("메뉴 우편함");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator RedDotIsOnWhenSomethingCanBeClaimedAndOffWhenNothingCan()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);
            var lobby = _app.Current.Root;
            var G = _app.Data; var S = _app.Save; string today = SaveStore.Today();

            // T148 — ≡ 점은 이제 «우편함» 만 본다(메뉴에 남은 것이 우편함·설정뿐이라).
            var menuDot = UiKit.Find(UiKit.Find(lobby, "Button_Menu"), "MenuDot");
            Assert.IsNotNull(menuDot, "메뉴(≡) 알림 점");
            Assert.AreEqual(Mailbox.Any(_app), menuDot.gameObject.activeSelf, "≡ 점 = 우편함에 받을 것이 있는가");

            // 데일리 기프트 점은 «로비 칸» 으로 돌아갔다 — 받을 것이 있으면 켜지고 다 받으면 꺼진다
            var giftCell = UiKit.Find(lobby, "Side:" + LobbyScreen.SideDailyGift);
            Assert.IsNotNull(giftCell, "로비 «데일리 기프트» 칸(T148)");
            var giftDot = UiKit.Find(giftCell, "GiftDot");
            Assert.IsNotNull(giftDot, "로비 데일리 기프트 알림 점(T77)");
            if (G != null && G.DailyGift != null)
            {
                Assert.IsTrue(DailyGift.AnyClaimable(S, G.DailyGift, today), "새 세이브에는 받을 것이 있다(시험이 성립한다)");
                _app.Current.Refresh(); yield return Frames(1);
                Assert.IsTrue(giftDot.gameObject.activeSelf, "받을 것이 있으면 로비 칸에 점이 켜진다");

                DailyGift.ClaimFree(S, G.DailyGift, today);
                for (int i = 0; i < G.DailyGift.Milestones.Count; i++)
                {
                    while (!DailyGift.CanClaim(S, G.DailyGift, i, today) && S.GiftAds < G.DailyGift.MaxAds) DailyGift.WatchAd(S, G.DailyGift, today);
                    DailyGift.Claim(S, G.DailyGift, i, today);
                }
                _app.Current.Refresh(); yield return Frames(1);
                Assert.IsFalse(DailyGift.AnyClaimable(S, G.DailyGift, today), "다 받으면 받을 것이 없다");
                Assert.IsFalse(giftDot.gameObject.activeSelf, "로비 칸의 점이 꺼진다");
            }
            _log.AssertNoRed("알림 점");

            yield return Shutdown();
        }

        /// <summary>
        /// T162 — 주인 «메뉴 드롭다운 팝업에 클로즈 버튼 없애기». 조각 <c>Lobby_Menu</c> 가 달고 오는 닫기 버튼을 끄고,
        /// 닫는 길은 <b>T139 ⓐ 의 딤 클릭</b>이 잇는다 — 둘은 한 짝이라 여기서 같이 잰다(하나만 되돌리면 «못 닫는 창» 이 된다).
        /// 이름을 «앞머리» 로 재는 까닭 = 데모 조각이 인스턴스마다 이름을 덮어써서(<c>Button_Close</c> ↔ <c>Button_Close_Square_01</c>)
        /// 정확한 이름 하나로 찾으면 놓친다(워커 A 가 우편함에서 밟은 함정).
        /// </summary>
        [UnityTest]
        public IEnumerator TheDropdownHasNoCloseButtonAndTheDimIsStillTheWayOut()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);
            ClickNamed(_app.Current.Root, "Button_Menu"); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "메뉴가 열린다");
            var ov = _app.Overlay.Root;

            int off = 0; var on = new List<string>();
            foreach (var t in ov.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith(LobbyMenu.CloseNamePrefix)) continue;
                if (t.gameObject.activeInHierarchy) on.Add(t.name + "(부모 " + (t.parent != null ? t.parent.name : "?") + ")");
                else off++;
            }
            Debug.Log("[T162] 닫기 버튼 — 켜진 것 " + on.Count + " · 꺼진 것 " + off);
            Assert.IsEmpty(on, "메뉴 드롭다운에 닫기 버튼이 보이면 안 된다(T162) — 켜져 있는 것: " + string.Join(", ", on.ToArray()));

            // 닫는 길은 남아 있다(T139 ⓐ) — 이 단언이 없으면 T162 가 «못 닫는 창» 을 만들었는지 여기서 안 걸린다
            var dim = UiKit.Find(ov, "Dimmed");
            Assert.IsNotNull(dim, "프리팹 어둠");
            var dimBtn = dim.GetComponent<Button>();
            Assert.IsNotNull(dimBtn, "닫기 버튼을 뺐으므로 어둠이 유일한 닫는 길이다(OpenPrefab(closeOnDim: true))");
            dimBtn.onClick.Invoke(); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "어둠을 누르면 닫힌다");

            _log.AssertNoRed("T162 닫기 버튼 제거");
            yield return Shutdown();
        }

    }
}
