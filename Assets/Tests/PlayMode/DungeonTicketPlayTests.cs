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
    /// 던전 티켓(T99 · 주인 2026-09-07)의 <b>화면</b> 쪽 — 규칙 자체는 EditMode <c>DungeonTicketTests</c> 가 못 박고, 여기서는
    /// ⓐ 하루 보충이 화면을 열자마자 돌아 카드 제목 띠가 «2/2» 로 보이고 ⓑ 티켓이 0 이면 «소탕»·«도전» 두 버튼이 «광고 보고 티켓 1개»·«다이아 50 으로 티켓 사기» 로 바뀌며
    /// ⓒ 다이아 버튼을 누르면 실제로 티켓이 늘고 버튼이 원래대로 돌아오고 ⓓ 다이아가 모자라면 꺼져 보이고 눌러도 티켓이 안 늘고(이유 토스트) ⓔ 어느 지점에도 빨간 줄이 없다 를 본다.
    /// </summary>
    public class DungeonTicketPlayTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

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
            _app = null; yield return Frames(3);
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerable<TMP_Text> ActiveTexts() => _app.UiCanvas.GetComponentsInChildren<TMP_Text>(false);
        bool HasText(Func<string, bool> pred) { foreach (var t in ActiveTexts()) if (pred(t.text ?? "")) return true; return false; }
        static bool ClickNamed(Transform root, string name) { var t = root != null ? UiKit.Find(root, name) : null; var b = t != null ? t.GetComponent<Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        static string LabelOf(Transform root, string name)
        {
            var t = root != null ? UiKit.Find(root, name) : null; if (t == null) return null;
            foreach (var x in t.GetComponentsInChildren<TMP_Text>(false)) { string s = (x.text ?? "").Trim(); if (s.Length > 0) return s; }
            return "";
        }

        [UnityTest]
        public IEnumerator TicketsRefillAndTheTwoButtonsBecomeTicketButtonsWhenEmpty()
        {
            yield return Boot();
            var D = _app.Data.Dungeon;
            Assert.IsNotNull(D, "dungeon.json 이 카탈로그(data.dungeon)로 실려야 한다");
            string today = SaveStore.Today();

            // ⓐ 던전 페이지를 열면 하루 보충이 돌아 «2/2»
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(2);
            var root = _app.Current.Root;
            Assert.AreEqual(D.DailyRefill, DungeonTickets.Tickets(_app.Save, D, "hell", today), "첫 접근에 하루 보충(0 → dailyRefill)");
            Assert.IsTrue(HasText(s => s.Trim() == D.DailyRefill + "/" + D.DailyRefill), "카드 제목 띠 티켓 = «보유/보충»");
            _log.AssertNoRed("던전 페이지(티켓)");

            // 티켓이 있으면 레퍼런스 21 그대로 «소탕»·«도전»
            Assert.IsTrue(ClickNamed(UiKit.Find(root, "Card:hell"), "EnterBtn"), "카드 1 입장"); yield return Frames(2);
            var ov = _app.Overlay.Root;
            Assert.AreEqual("소탕", LabelOf(ov, "SweepBtn"), "티켓이 있으면 왼쪽은 «소탕»");
            Assert.AreEqual("도전", LabelOf(ov, "ChallengeBtn"), "티켓이 있으면 오른쪽은 «도전»");
            _app.Overlay.Close(); yield return Frames(1);

            // ⓑ 티켓 0 → 두 버튼이 «티켓 얻기» 로 바뀐다
            _app.Save.DunTickets["hell"] = 0; _app.Save.Gem = 0;
            Assert.IsTrue(ClickNamed(UiKit.Find(root, "Card:hell"), "EnterBtn")); yield return Frames(2);
            ov = _app.Overlay.Root;
            Assert.AreEqual("광고 보고 티켓 1개", LabelOf(ov, "SweepBtn"), "티켓 0 이면 왼쪽은 광고 버튼(주인 T99 3항)");
            StringAssert.Contains("티켓 사기", LabelOf(ov, "ChallengeBtn"), "티켓 0 이면 오른쪽은 다이아 버튼");
            StringAssert.Contains(UiKit.FmtQty(D.GemCost), LabelOf(ov, "ChallengeBtn"), "다이아 값은 표(dungeon.json)에서");

            // ⓓ 다이아가 모자라면 꺼져 보이고(알파 0.5) 눌러도 티켓이 안 는다
            var chal = UiKit.Find(ov, "ChallengeBtn");
            var cg = chal.GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg, "못 사는 버튼은 CanvasGroup 으로 꺼져 보인다");
            Assert.AreEqual(0.5f, cg.alpha, 1e-3f, "다이아 0 이면 알파 0.5");
            Assert.IsTrue(ClickNamed(ov, "ChallengeBtn"), "꺼져 보여도 클릭은 살아 있다(이유 토스트)"); yield return Frames(2);
            Assert.AreEqual(0, DungeonTickets.Tickets(_app.Save, _app.Data.Dungeon, "hell", today), "다이아가 모자라면 티켓이 안 는다");
            Assert.AreEqual(0, _app.Save.Gem, 1e-9, "다이아도 안 빠진다");
            _log.AssertNoRed("티켓 0 · 다이아 부족");

            // ⓒ 다이아를 채우고 누르면 티켓 +1 · 다이아 −값 · 버튼이 «소탕/도전» 으로 돌아온다
            _app.Save.Gem = D.GemCost * 3;
            _app.Overlay.Close(); yield return Frames(1);
            Assert.IsTrue(ClickNamed(UiKit.Find(root, "Card:hell"), "EnterBtn")); yield return Frames(2);
            ov = _app.Overlay.Root;
            Assert.IsTrue(ClickNamed(ov, "ChallengeBtn"), "다이아 티켓 사기"); yield return Frames(2);
            Assert.AreEqual(1, DungeonTickets.Tickets(_app.Save, _app.Data.Dungeon, "hell", today), "티켓 +1");
            Assert.AreEqual(D.GemCost * 2, _app.Save.Gem, 1e-9, "다이아 −" + UiKit.FmtQty(D.GemCost));
            ov = _app.Overlay.Root;
            Assert.AreEqual("소탕", LabelOf(ov, "SweepBtn"), "티켓이 생기면 팝업이 다시 열리며 원래 버튼으로");
            Assert.IsFalse(DungeonTickets.GemLeft(_app.Save, _app.Data.Dungeon, "hell", today), "오늘 다이아 구매는 하루 1번");
            _log.AssertNoRed("다이아로 티켓 사기");

            _app.Overlay.Close(); yield return Frames(1);
            _app.ShowScreen("lobby"); yield return Frames(1);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }

        /// <summary>
        /// T228 2단계 ⓑⓒⓓ — «소탕» 버튼의 화면 쪽. 규칙은 EditMode <c>DungeonSweepTests</c> 가 못 박고 여기서는
        /// ⓐ <b>클리어한 적이 없으면</b> 꺼져 보이고 눌러도 티켓·재화가 안 움직이며 ⓑ 클리어 기록이 생기면 밝아지고
        /// ⓒ 누르면 <b>티켓 1 이 빠지고 표의 sweep 이 실제로 들어오며</b> ⓓ «도전» 이 던전 키를 전투에 실어 보내는 것을 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator SweepIsOffUntilTheDungeonHasBeenClearedAndThenActuallyPays()
        {
            yield return Boot();
            var D = _app.Data.Dungeon;
            string today = SaveStore.Today();
            var entry = D.Of("hell");
            Assert.IsNotNull(entry, "표에 지옥의 문이 있어야 한다");

            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(2);
            var root = _app.Current.Root;
            _app.Save.Gold = 0; _app.Save.PetEgg = 0;

            // ⓐ 클리어한 적이 없다 → 꺼져 보이고, 눌러도 티켓·골드·펫알이 그대로다(이유는 토스트로)
            Assert.AreEqual(0, DungeonSweep.Floor(_app.Save, "hell"), "새 세이브는 클리어 기록 0");
            Assert.IsTrue(ClickNamed(UiKit.Find(root, "Card:hell"), "EnterBtn")); yield return Frames(2);
            var ov = _app.Overlay.Root;
            int before = DungeonTickets.Tickets(_app.Save, D, "hell", today);
            Assert.Greater(before, 0, "티켓은 있는 상태여야 «층» 때문에 막히는 것이 보인다");
            var cg = UiKit.Find(ov, "SweepBtn").GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg, "못 하는 소탕은 CanvasGroup 으로 꺼져 보인다");
            Assert.AreEqual(0.5f, cg.alpha, 1e-3f, "클리어한 적이 없으면 알파 0.5");
            Assert.IsTrue(ClickNamed(ov, "SweepBtn"), "꺼져 보여도 클릭은 살아 있다(이유 토스트)"); yield return Frames(2);
            Assert.AreEqual(before, DungeonTickets.Tickets(_app.Save, D, "hell", today), "막힌 소탕은 티켓을 안 쓴다");
            Assert.AreEqual(0, _app.Save.Gold, 1e-9, "골드도 안 들어온다");
            Assert.AreEqual(0, _app.Save.PetEgg, 1e-9, "펫알도 안 들어온다");
            _log.AssertNoRed("클리어 전 소탕");

            // ⓑⓒ 클리어 기록이 생기면 밝아지고, 누르면 티켓 1 이 빠지며 표의 sweep 이 실제로 들어온다
            DungeonSweep.Record(_app.Save, "hell", 1);
            _app.Overlay.Close(); yield return Frames(1);
            Assert.IsTrue(ClickNamed(UiKit.Find(root, "Card:hell"), "EnterBtn")); yield return Frames(2);
            ov = _app.Overlay.Root;
            cg = UiKit.Find(ov, "SweepBtn").GetComponent<CanvasGroup>();
            Assert.IsTrue(cg == null || cg.alpha > 0.99f, "클리어한 적이 있으면 밝다");
            before = DungeonTickets.Tickets(_app.Save, D, "hell", today);
            Assert.IsTrue(ClickNamed(ov, "SweepBtn"), "소탕"); yield return Frames(2);
            Assert.AreEqual(before - 1, DungeonTickets.Tickets(_app.Save, D, "hell", today), "티켓 정확히 1 장");
            Assert.AreEqual(entry.Sweep.Gold, _app.Save.Gold, 1e-9, "표의 sweep 골드가 들어왔다");
            Assert.AreEqual(entry.Sweep.PetEgg, _app.Save.PetEgg, 1e-9, "펫알도 버려지지 않고 들어왔다");
            Assert.AreNotEqual(entry.First.Gold + entry.First.PetEgg, _app.Save.Gold + _app.Save.PetEgg, "첫 클리어 총액은 소탕이 안 준다");
            _log.AssertNoRed("소탕 지급");

            // ⓓ «도전» 이 «어느 던전» 인지를 전투에 실어 보낸다(이게 없으면 클리어를 아무도 안 적는다)
            _app.Overlay.Close(); yield return Frames(1);
            Assert.IsTrue(ClickNamed(UiKit.Find(root, "Card:hell"), "EnterBtn")); yield return Frames(2);
            ov = _app.Overlay.Root;
            Assert.IsTrue(ClickNamed(ov, "ChallengeBtn"), "도전"); yield return Frames(2);
            var battle = _app.GetScreen<BattleScreen>();
            Assert.AreEqual("hell", battle.DungeonKey, "전투가 «어느 던전» 인지 안다 — 클리어하면 이 키로 기록이 남는다");
            battle.Abort();
            _app.ShowScreen("lobby"); yield return Frames(1);
            _log.AssertNoRed("던전 도전 진입");
            yield return Shutdown();
        }
    }
}
