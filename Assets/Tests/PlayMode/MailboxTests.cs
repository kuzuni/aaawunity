using System;
using System.Collections;
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
    /// 우편함(T96-mail 조각 · <b>범위는 T243</b> — 주인 2026-09-08 11:5X «우편함으로는 <b>아레나 보상만</b> 오게 하고 나머지는 걍 즉시 지급해»):
    /// ⓐ 아레나 우편이 없으면 <c>ui.mailboxEmpty</c> 조각(«비었음» 그림 · 줄 0 · 전체 받기 꺼짐)
    /// ⓑ <b>탐험·데일리 기프트가 받을 수 있는 상태여도 우편함에는 한 줄도 안 뜬다</b>(그 둘은 제 팝업에서 즉시 받는다 — 없어진 보상 0)
    /// ⓒ 아레나 우편이 오면 줄이 서고 «받기» 로 <b>실제 재화가 들어온다</b>(지급은 Core 가 한다) ⓓ 다 받으면 다시 «비었음»
    /// ⓔ 로비 메뉴 «우편함» 이 이 팝업을 열고 ≡ 알림 점은 <b>아레나 우편에만</b> 반응 ⓕ 영문 데모 글자 0 · 빨간 줄 0.
    /// </summary>
    public class MailboxTests
    {
        App _app; PlayLog _log;

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
        static double NowSec() => (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        /// <summary>아레나 우편 한 통(테스트용) — 실제로 넣는 자리는 순위·시즌 정산이 생길 때 배선한다.</summary>
        static MailItem ArenaMail(string id) => Mail1(id, Core.Mail.ItemGold, 1000, Core.Mail.ItemArenaCoin, 30);
        static MailItem Mail1(string id, string i1, double a1, string i2, double a2)
        {
            var m = new MailItem { Id = id, Kind = Core.Mail.KindArena, Title = "아레나 순위 보상", Desc = "" };
            m.Rewards.Add(new ArenaRankData.Reward { Item = i1, Amount = a1 });
            m.Rewards.Add(new ArenaRankData.Reward { Item = i2, Amount = a2 });
            return m;
        }

        /// <summary>이름이 <paramref name="prefix"/> 로 시작하면서 «켜져 있는» 것의 수.</summary>
        static int ActiveNamed(Transform root, string prefix)
        {
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(prefix, StringComparison.Ordinal) && t.gameObject.activeInHierarchy) n++;
            return n;
        }

        static int RowCount(Transform root)
        {
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(Mailbox.RowPrefix, StringComparison.Ordinal) && t.gameObject.activeInHierarchy) n++;
            return n;
        }

        [UnityTest]
        public IEnumerator MailboxCarriesArenaRewardsOnlyAndGivesThem()
        {
            yield return Boot();
            var S = _app.Save; var G = _app.Data;
            Assert.IsNotNull(G.Expedition, "탐험 표(T97)");

            // ⓐ 새 세이브 = 아레나 우편 0 → «비었음» 조각
            Assert.AreEqual(0, Mailbox.Entries(_app).Count, "새 세이브의 우편함은 비어 있다");
            Assert.IsFalse(Mailbox.Any(_app), "Any 도 false");
            Mailbox.Open(_app); yield return Frames(2); Canvas.ForceUpdateCanvases();
            Assert.IsTrue(_app.Overlay.IsOpen, "우편함은 팝업");
            var ov = _app.Overlay.Root;
            Assert.IsNotNull(UiKit.Find(ov, "ui.mailboxEmpty"), "받을 것이 없으면 Rewards_Mailbox_Empty 조각(주인 지목)");
            Assert.AreEqual(0, RowCount(ov), "줄 0");
            var empty = UiKit.Find(ov, "Empty");
            Assert.IsTrue(empty != null && empty.gameObject.activeInHierarchy, "«비었음» 그림이 보인다");
            Assert.IsNull(UiKit.Find(ov, Mailbox.ClaimAllName), "받을 것이 없으면 «전체 받기» 는 없다");
            foreach (var t in ov.GetComponentsInChildren<TMP_Text>(true))
                Assert.AreNotEqual("Mailbox", (t.text ?? "").Trim(), "영문 데모 글자 0(제목은 «우편함»)");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓑ T243 의 핵심 — 탐험이 8시간 쌓이고 데일리 기프트도 받을 수 있는데 우편함은 그대로 비어 있다
            S.ExpSettle = NowSec() - 8 * 3600;
            S.GiftDay = ""; S.GiftFree = false;
            Assert.IsTrue(Core.Expedition.CanClaim(G, S, G.Expedition, NowSec(), SaveStore.Today()), "탐험 보상이 받을 수 있는 상태");
            Assert.AreEqual(0, Mailbox.Entries(_app).Count, "그래도 우편함에는 한 줄도 안 뜬다(주인 «우편함은 아레나 보상만»)");
            Assert.IsFalse(Mailbox.Any(_app), "≡ 점도 안 켜진다");

            // ⓒ 아레나 우편이 오면 줄이 서고 받으면 재화가 들어온다
            Assert.IsTrue(Core.Mail.Add(S, ArenaMail(Mailbox.KeyArena + ":s1")), "아레나 우편은 들어온다");
            double gold0 = S.Gold, coin0 = S.ArenaCoin;
            var entries = Mailbox.Entries(_app);
            Assert.AreEqual(1, entries.Count, "아레나 우편 한 줄");
            Mailbox.Open(_app); yield return Frames(2); Canvas.ForceUpdateCanvases();
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.mailbox"), "받을 것이 있으면 Rewards_Mailbox 조각");
            var row = UiKit.Find(_app.Overlay.Root, Mailbox.RowPrefix + Mailbox.KeyArena + ":s1");
            Assert.IsNotNull(row, "줄 이름 = Mail:<우편 id>");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, Mailbox.ClaimAllName), "«전체 받기» 버튼");
            // T244 — 줄이 서 있는 상태에서도 탭은 꺼져 있고, 목록·«전체 받기» 는 살아 있다(담개를 잘못 잡는 사고 방지).
            Assert.AreEqual(0, ActiveNamed(_app.Overlay.Root, "Tab_02"), "탭 셋은 꺼져 있다(주인 «애초에 탭 필요 x»)");
            Assert.AreEqual(1, RowCount(_app.Overlay.Root), "그런데 우편 줄은 그대로 서 있다");
            var claim = row.GetComponentInChildren<Button>(true); Assert.IsNotNull(claim, "줄의 «받기» 버튼");
            claim.onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(gold0 + 1000, S.Gold, 1e-9, "골드가 실제로 들어온다");
            Assert.AreEqual(coin0 + 30, S.ArenaCoin, 1e-9, "아레나 코인도 버려지지 않고 들어온다");
            Assert.AreEqual(0, Mailbox.Entries(_app).Count, "받고 나면 그 줄은 사라진다");

            // T241 — 받은 것은 이제 토스트 한 줄이 아니라 공통 «리워드» 팝업이 보여 준다.
            // 이 우편은 골드·아레나 코인 둘이므로 칸도 둘이고, 탭해 닫으면 우편함이 다시 그려진다(결정 671: 팝업 안이면 onClose).
            {
                var rv = _app.Overlay.Root;
                Assert.IsNotNull(UiKit.Find(rv, "RewardTitle"), "우편 받기 → 리워드 팝업(T241)");
                Assert.AreEqual(2, RewardPopup.LastCellCount, "골드·아레나 코인 두 칸");
                var tap = UiKit.Find(rv, "Dimmed")?.GetComponent<Button>(); Assert.IsNotNull(tap, "리워드 팝업의 탭하여 닫기");
                tap.onClick.Invoke(); yield return Frames(2);
            }

            // ⓓ 다 받으면 다시 «비었음»
            Canvas.ForceUpdateCanvases();
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.mailboxEmpty"), "다 받으면 «비었음» 프리팹으로 다시 그린다");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓔ 로비 메뉴(≡) 의 «우편함» 항목이 이 팝업을 연다
            _app.ShowScreen("lobby"); yield return Frames(2);
            LobbyMenu.Open(_app); yield return Frames(2);
            var mail = UiKit.Find(_app.Overlay.Root, "Menu:" + LobbyMenu.ItemMail);
            Assert.IsNotNull(mail, "메뉴 항목 «우편함»");
            var mb = mail.GetComponentInChildren<Button>(true); Assert.IsNotNull(mb, "그 줄의 버튼");
            mb.onClick.Invoke(); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "우편함이 열린다");
            Assert.IsNotNull(UiKit.FindAny(_app.Overlay.Root, "ui.mailbox", "ui.mailboxEmpty"), "메뉴 → 우편함 조각");

            // ⓖ T244 — 탭 셋이 꺼져 있다(주인 «애초에 탭 필요 x»). 그리고 «담개를 잘못 잡아 목록까지 끄는» 사고가 없어야 한다.
            Assert.AreEqual(0, ActiveNamed(_app.Overlay.Root, "Tab_02"), "탭 조각은 하나도 켜져 있지 않다");
            Assert.IsNotNull(UiKit.FindAny(_app.Overlay.Root, Mailbox.CloseName, "Button_Close_Square_01"), "탭을 끄면서 닫기까지 끄지 않았다");

            // ⓕ T169 — 닫을 수 있어야 한다(주인 «우편함 팝업 안 닫힌다»).
            var close = UiKit.FindAny(_app.Overlay.Root, Mailbox.CloseName, "Button_Close_Square_01");
            Assert.IsNotNull(close, "조각이 들고 오는 닫기 버튼(" + Mailbox.CloseName + ")");
            var cb = close.GetComponent<Button>();
            Assert.IsNotNull(cb, "그 버튼이 배선돼 있다");
            cb.onClick.Invoke(); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "닫기 버튼으로 닫힌다");

            Mailbox.Open(_app); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "다시 연다");
            var dim = UiKit.Find(_app.Overlay.Root, "Dimmed");
            Assert.IsNotNull(dim, "조각의 어둠");
            var db = dim.GetComponent<Button>();
            Assert.IsNotNull(db, "어둠에도 닫기가 붙는다(OpenPrefab closeOnDim: true · T139 ⓐ 인자)");
            db.onClick.Invoke(); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "딤을 눌러도 닫힌다");

            _log.AssertNoRed("우편함(T243 · 아레나 전용)");
            yield return Shutdown();
        }
    }
}
