using System;
using System.Collections;
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
        bool HasText(Func<string, bool> pred) { foreach (var t in _app.UiCanvas.GetComponentsInChildren<Text>(false)) if (pred(t.text ?? "")) return true; return false; }
        static readonly string[] Items = { LobbyMenu.ItemMail, LobbyMenu.ItemSettings, LobbyMenu.ItemDailyGift, LobbyMenu.ItemQuest, LobbyMenu.ItemAttendance, LobbyMenu.ItemPrivilege };

        [UnityTest]
        public IEnumerator MenuOpensThePrefabWithSixRowsInTheOwnersOrder()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);
            var lobby = _app.Current.Root;

            // 로비에는 메뉴로 옮긴 버튼이 남아 있지 않다(주인 «중복된 거는 메뉴 안으로»)
            Assert.IsNull(UiKit.Find(lobby, "SideL"), "좌 사이드 기둥 삭제"); Assert.IsNull(UiKit.Find(lobby, "SideR"), "우 사이드 기둥 삭제");
            foreach (var k in new[] { LobbyScreen.SideQuest, LobbyScreen.SideAttendance, LobbyScreen.SideDailyGift, LobbyScreen.SidePrivilege })
                Assert.IsNull(UiKit.Find(lobby, "Side:" + k), "로비에 중복 버튼 없음: " + k);

            ClickNamed(lobby, "Button_Menu"); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "메뉴가 열린다");
            var ov = _app.Overlay.Root;
            var panel = UiKit.Find(ov, LobbyMenu.PanelName);
            Assert.IsNotNull(panel, "프리팹 판(HambergerMenu) — 우리 격자로 다시 만들지 않는다");
            Assert.IsNotNull(UiKit.Find(ov, "Dimmed"), "프리팹 어둠");

            // 여섯 줄이 주인이 부른 순서대로 · 라벨은 우리말
            int shown = 0;
            for (int i = 0; i < Items.Length; i++)
            {
                var row = UiKit.Find(ov, "Menu:" + Items[i]);
                Assert.IsNotNull(row, "메뉴 줄 " + Items[i]);
                Assert.IsTrue(row.gameObject.activeInHierarchy, "메뉴 줄이 보인다: " + Items[i]);
                Assert.AreEqual(i, row.GetSiblingIndex(), "메뉴 순서 " + Items[i]);
                shown++;
            }
            Assert.AreEqual(6, shown, "메뉴 항목 6");
            foreach (var s in new[] { "우편함", "설정", "데일리 기프트", "퀘스트", "출석", "특권" })
                Assert.IsTrue(HasText(x => x == s), "메뉴 라벨 «" + s + "»");
            _log.AssertNoRed("메뉴 열림");

            // 줄 높이·아이콘은 프리팹 그대로(복제한 두 줄도 같은 크기)
            var first = (RectTransform)UiKit.Find(ov, "Menu:" + Items[0]);
            var last = (RectTransform)UiKit.Find(ov, "Menu:" + Items[Items.Length - 1]);
            Assert.AreEqual(first.rect.height, last.rect.height, 0.5f, "복제한 줄도 프리팹 줄과 같은 높이");
            Assert.AreEqual(first.rect.width, last.rect.width, 0.5f, "복제한 줄도 프리팹 줄과 같은 폭");

            yield return Shutdown();
        }

        /// <summary>
        /// T139 — 주인이 스크린샷으로 준 두 가지. ⓐ <b>어둠(Dimmed)을 누르면 닫힌다</b>(«딤 눌러도 꺼지게») ·
        /// ⓑ <b>판 자리</b>가 인스펙터 값 그대로다(앵커 (1,1) · Pivot (0.5,0.5) · Pos (−323,−558) · 가로 382.62 · 세로는 줄 수 계산값 688.305).
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
            Assert.AreEqual(LobbyMenu.PanelPos.x, panel.anchoredPosition.x, 0.5f, "판 Pos x = −323(주인 값)");
            Assert.AreEqual(LobbyMenu.PanelPos.y, panel.anchoredPosition.y, 0.5f, "판 Pos y = −558(주인 값)");
            Assert.AreEqual(LobbyMenu.PanelWidth, panel.rect.width, 1f, "판 가로 = 382.62(주인 값)");
            Assert.AreEqual(688.305f, panel.rect.height, 1f, "판 세로 = 688.305(= 프리팹 458.87 × 줄 6/4 · 주인 값과 같다)");

            // 드롭다운이 ≡ 버튼 «아래» 로 늘어지는가 — 프레임 좌표로(자리 값이 다른 rect 기준이면 여기서 드러난다)
            var menuBtn = (RectTransform)UiKit.Find(lobby, "Button_Menu");
            var frame = _app.Frame;
            Canvas.ForceUpdateCanvases();
            float PanelTopY(RectTransform rt)
            {
                var c = new Vector3[4]; rt.GetWorldCorners(c);
                return frame.InverseTransformPoint(c[1]).y;   // 왼쪽 위 모서리의 프레임 y
            }
            float btnTop = PanelTopY(menuBtn), panelTop = PanelTopY(panel);
            Debug.Log($"[T139] 판 프레임 rect = {frame.InverseTransformPoint(panel.position)} · 크기 {panel.rect.width:0.0}×{panel.rect.height:0.0} · ≡ 버튼 윗변 y {btnTop:0} · 판 윗변 y {panelTop:0}");
            Assert.LessOrEqual(panelTop, btnTop, "드롭다운 판은 ≡ 버튼 윗변보다 아래에서 시작한다(버튼 위로 솟지 않는다)");

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

            (string key, string mark)[] pops =
            {
                (LobbyMenu.ItemSettings, "음악"), (LobbyMenu.ItemDailyGift, "데일리 기프트"),
                (LobbyMenu.ItemQuest, "퀘스트"), (LobbyMenu.ItemAttendance, "출석 보상"),
            };
            foreach (var p in pops)
            {
                ClickNamed(lobby, "Button_Menu"); yield return Frames(2);
                ClickNamed(_app.Overlay.Root, "Menu:" + p.key); yield return Frames(2);
                Assert.IsTrue(_app.Overlay.IsOpen, p.key + ": 팝업이 열린다");
                Assert.IsTrue(HasText(s => s.Contains(p.mark)), p.key + ": 내용 «" + p.mark + "»");
                Assert.IsNull(UiKit.Find(_app.Overlay.Root, LobbyMenu.PanelName), p.key + ": 메뉴는 닫히고 갈아 끼운다");
                _app.Overlay.Close(); yield return Frames(1);
            }
            _log.AssertNoRed("메뉴 항목 팝업 4종");

            // 특권 = 페이지
            ClickNamed(lobby, "Button_Menu"); yield return Frames(2);
            ClickNamed(_app.Overlay.Root, "Menu:" + LobbyMenu.ItemPrivilege); yield return Frames(3);
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

            var menuDot = UiKit.Find(UiKit.Find(lobby, "Button_Menu"), "MenuDot");
            Assert.IsNotNull(menuDot, "메뉴(≡) 알림 점");
            Assert.AreEqual(Notify.MenuAny(G, S, LobbyPopups.NowSec(), today), menuDot.gameObject.activeSelf, "≡ 점 = Notify.MenuAny");

            // 데일리 기프트를 다 받으면 점이 꺼진다(판정이 있는 항목이 그것뿐이다)
            if (G != null && G.DailyGift != null)
            {
                DailyGift.ClaimFree(S, G.DailyGift, today);
                for (int i = 0; i < G.DailyGift.Milestones.Count; i++)
                {
                    while (!DailyGift.CanClaim(S, G.DailyGift, i, today) && S.GiftAds < G.DailyGift.MaxAds) DailyGift.WatchAd(S, G.DailyGift, today);
                    DailyGift.Claim(S, G.DailyGift, i, today);
                }
                _app.Current.Refresh(); yield return Frames(1);
                Assert.IsFalse(Notify.MenuAny(G, S, LobbyPopups.NowSec(), today), "다 받으면 메뉴에 받을 것이 없다");
                Assert.IsFalse(menuDot.gameObject.activeSelf, "≡ 점이 꺼진다");

                ClickNamed(lobby, "Button_Menu"); yield return Frames(2);
                var giftRow = UiKit.Find(_app.Overlay.Root, "Menu:" + LobbyMenu.ItemDailyGift);
                var rowDot = UiKit.Find(giftRow, "AlertDot");
                Assert.IsNotNull(rowDot, "메뉴 줄 알림 점");
                Assert.IsFalse(rowDot.gameObject.activeSelf, "다 받은 데일리 기프트 줄에는 점이 없다");
                _app.Overlay.Close(); yield return Frames(1);
            }
            _log.AssertNoRed("알림 점");

            yield return Shutdown();
        }
    }
}
