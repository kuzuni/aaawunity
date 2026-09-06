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
    /// T96-mail — 로비 메뉴(≡)의 «우편함»(주인 2026-09-07 «Rewards_Mailbox·Rewards_Mailbox_Empty 이거 좀 써라 프리팹들»):
    /// ⓐ 받을 것이 하나도 없으면 <c>ui.mailboxEmpty</c> 조각(«비었음» 그림 · 줄 0 · 전체 받기 꺼짐)
    /// ⓑ 받을 것이 생기면 <c>ui.mailbox</c> 조각에 줄이 서고(<c>Mail:expedition</c>) «받기» 로 <b>실제 재화가 들어온다</b>(지급은 Core 가 한다)
    /// ⓒ 다 받으면 다시 «비었음» ⓓ 로비 메뉴 항목 «우편함» 이 이 팝업을 연다 ⓔ 영문 데모 글자 0 · 빨간 줄 0.
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

        /// <summary>지금 받을 수 있는 것을 전부 받아 «빈 우편함» 상태로 만든다(시작 상태가 날짜에 따라 갈리지 않게).</summary>
        void DrainAll()
        {
            for (int guard = 0; guard < 32; guard++)
            {
                var list = Mailbox.Entries(_app);
                if (list.Count == 0) return;
                foreach (var e in list) e.Claim?.Invoke(_app);
            }
            Assert.Fail("우편함이 비워지지 않는다(무한 반복)");
        }

        static int RowCount(Transform root)
        {
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(Mailbox.RowPrefix, StringComparison.Ordinal) && t.gameObject.activeInHierarchy) n++;
            return n;
        }

        [UnityTest]
        public IEnumerator MailboxShowsClaimablesAndGivesThem()
        {
            yield return Boot();
            var S = _app.Save; var G = _app.Data;
            Assert.IsNotNull(G.Expedition, "탐험 표(T97)");

            // ⓐ 빈 우편함 — 받을 것을 전부 비우고 연다
            DrainAll();
            S.ExpSettle = NowSec();   // 탐험도 방금 정산한 것으로
            Assert.AreEqual(0, Mailbox.Entries(_app).Count, "받을 것 0");
            Assert.IsFalse(Mailbox.Any(_app), "Any 도 false");
            Mailbox.Open(_app); yield return Frames(2); Canvas.ForceUpdateCanvases();
            Assert.IsTrue(_app.Overlay.IsOpen, "우편함은 팝업");
            var ov = _app.Overlay.Root;
            Assert.IsNotNull(UiKit.Find(ov, "ui.mailboxEmpty"), "받을 것이 없으면 Rewards_Mailbox_Empty 조각(주인 지목)");
            Assert.AreEqual(0, RowCount(ov), "줄 0");
            var empty = UiKit.Find(ov, "Empty");
            Assert.IsTrue(empty != null && empty.gameObject.activeInHierarchy, "«비었음» 그림이 보인다");
            Assert.IsNull(UiKit.Find(ov, Mailbox.ClaimAllName), "받을 것이 없으면 «전체 받기» 는 없다");
            foreach (var t in ov.GetComponentsInChildren<Text>(true))
                Assert.AreNotEqual("Mailbox", (t.text ?? "").Trim(), "영문 데모 글자 0(제목은 «우편함»)");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓑ 탐험 보상이 쌓이면 줄이 선다 — 8시간 전에 정산한 것으로 되돌린다(상한 안)
            S.ExpSettle = NowSec() - 8 * 3600;
            var entries = Mailbox.Entries(_app);
            Assert.GreaterOrEqual(entries.Count, 1, "탐험 보상이 줄로 뜬다");
            Assert.AreEqual(Mailbox.KeyExpedition, entries[0].Key, "첫 줄 = 탐험 보상");
            double gold0 = S.Gold, gem0 = S.Gem;

            Mailbox.Open(_app); yield return Frames(2); Canvas.ForceUpdateCanvases();
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.mailbox"), "받을 것이 있으면 Rewards_Mailbox 조각");
            var row = UiKit.Find(_app.Overlay.Root, Mailbox.RowPrefix + Mailbox.KeyExpedition);
            Assert.IsNotNull(row, "줄 이름 = Mail:expedition");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, Mailbox.ClaimAllName), "«전체 받기» 버튼");
            var claim = row.GetComponentInChildren<Button>(true); Assert.IsNotNull(claim, "줄의 «받기» 버튼");
            claim.onClick.Invoke(); yield return Frames(2);

            Assert.Greater(S.Gold + S.Gem, gold0 + gem0, "«받기» 로 재화가 실제로 들어온다(지급은 Core 가 한다)");
            Assert.AreEqual(0, Mailbox.Entries(_app).Count, "받고 나면 그 줄은 사라진다");
            // ⓒ 다 받으면 다시 «비었음»
            Canvas.ForceUpdateCanvases();
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.mailboxEmpty"), "다 받으면 «비었음» 프리팹으로 다시 그린다");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓓ 로비 메뉴(≡) 의 «우편함» 항목이 이 팝업을 연다
            _app.ShowScreen("lobby"); yield return Frames(2);
            LobbyMenu.Open(_app); yield return Frames(2);
            var mail = UiKit.Find(_app.Overlay.Root, "Menu:" + LobbyMenu.ItemMail);
            Assert.IsNotNull(mail, "메뉴 항목 «우편함»");
            var mb = mail.GetComponentInChildren<Button>(true); Assert.IsNotNull(mb, "그 줄의 버튼");
            mb.onClick.Invoke(); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "우편함이 열린다");
            Assert.IsNotNull(UiKit.FindAny(_app.Overlay.Root, "ui.mailbox", "ui.mailboxEmpty"), "메뉴 → 우편함 조각");

            _log.AssertNoRed("T96-mail 우편함");
            yield return Shutdown();
        }
    }
}
