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
    /// T264 2단계 — 특권(11) 페이지의 자. 규칙 자체는 EditMode <c>PrivilegeTests</c> 가 본다(값 여덟·하루 1회·월간 만료 …).
    /// <para>
    /// <b>여기서 재는 것은 «버튼이 그 규칙에 배선됐는가» 하나다.</b> 검수 워커 Q 가 T272 에서 세어 둔 빈자리가 바로 이것이었다 —
    /// 이름 붙은 버튼 36개 가운데 PlayMode 자가 <b>이름조차 안 부르는</b> 넷이 있었고 특권의 <c>CardBtn</c> 이 그중 하나였다.
    /// 규칙은 EditMode 가 촘촘히 재는데 «화면의 그 버튼이 그 규칙을 정말 부르는가» 만 아무도 안 봤다.
    /// 그래서 이 자는 <b>«있다 · interactable 이다» 로 끝내지 않고 실제로 누르고 다이아가 표대로 늘어나는지</b>까지 본다.
    /// </para>
    /// ⓐ 공짜 카드를 눌러 다이아가 표값만큼 늘고 ⓑ 두 번째는 잠기고 ⓒ 안 산 카드는 «구매» 로 눌려 즉시 보상이 들어오고
    /// ⓓ 산 뒤 그날치를 또 받을 수 있고 ⓔ «전체 받기» 가 남은 것을 쓸어 담고 ⓕ 빨간 줄 0(<see cref="PlayLog"/> · T11 규약).
    /// </summary>
    public class PrivilegeScreenTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; Assert.IsNotNull(_app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");
            yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { var r = Find(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }
        static Button BtnNamed(Transform root, string name)
        {
            var t = Find(root, name); return t != null ? t.GetComponent<Button>() : null;
        }
        static bool ClickNamed(Transform root, string name)
        {
            var b = BtnNamed(root, name);
            if (b == null || !b.IsInteractable()) return false;
            b.onClick.Invoke(); return true;
        }
        static string LabelOf(Transform root, string name)
        {
            var t = Find(root, name); var x = t != null ? t.GetComponentInChildren<TMP_Text>(true) : null;
            return x != null ? x.text : null;
        }

        /// <summary>리워드 팝업(T241)이 떠 있으면 닫는다 — 그것이 덮고 있으면 다음 버튼이 안 눌린다.</summary>
        IEnumerator CloseReward()
        {
            yield return Frames(2);
            var ov = _app != null && _app.Overlay != null ? _app.Overlay.Root : null;
            if (ov != null) { var tap = Find(ov, "TapToClose"); if (tap != null) { var b = tap.GetComponent<Button>(); if (b != null) b.onClick.Invoke(); } }
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator PrivilegeCardsActuallyPayWhenTheirButtonIsPressed()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            Assert.IsNotNull(D.Privilege, "privilege.json 이 카탈로그(data.privilege)에서 로드돼야 한다");
            var PD = D.Privilege;

            _app.ShowScreen("privilege");
            yield return Frames(2);
            var page = _app.Current != null ? _app.Current.Root : null;
            Assert.IsNotNull(page, "특권 페이지");

            // ── ⓐ 공짜 카드(«데일리 기프트») — 사지 않아도 눌리고, 표값만큼 다이아가 는다.
            var gift = PD.Of("dailyGift");
            Assert.IsNotNull(gift, "맨 위 «데일리 기프트» 카드(주인 2026-09-09)");
            double want = 0; foreach (var r in gift.Daily) if (r.Item == Mail.ItemGem) want = r.Amount;
            Assert.Greater(want, 0, "표가 매일 줄 다이아를 갖고 있어야 한다");

            double before = S.Gem;
            Assert.IsTrue(ClickNamed(page, "CardBtn:1"), "«데일리 기프트» 받기 버튼이 눌린다");
            yield return CloseReward();
            Assert.AreEqual(before + want, S.Gem, 0.0001,
                "누르면 표에 적힌 다이아가 실제로 들어와야 한다 — 이 한 줄이 T272 가 짚은 빈자리다(버튼이 규칙에 배선됐는가)");

            // ── ⓑ 같은 날 두 번째는 잠긴다(«오늘 받기 완료»).
            var b1 = BtnNamed(page, "CardBtn:1");
            Assert.IsNotNull(b1);
            Assert.IsFalse(b1.IsInteractable(), "오늘 몫을 받았으면 버튼이 회색이어야 한다(주인 «오늘 받기 완료»)");
            double after = S.Gem;
            b1.onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(after, S.Gem, 0.0001, "잠긴 버튼을 억지로 눌러도 한 톨도 더 주지 않는다");

            // ── ⓒ 안 산 카드는 «구매» 다 — 누르면 즉시 보상이 들어온다.
            var life = PD.Of("lifetime");
            Assert.IsNotNull(life);
            double buyNow = 0; foreach (var r in life.BuyNow) if (r.Item == Mail.ItemGem) buyNow = r.Amount;
            double daily = 0; foreach (var r in life.Daily) if (r.Item == Mail.ItemGem) daily = r.Amount;
            Assert.Greater(buyNow, 0); Assert.Greater(daily, 0);

            Assert.IsFalse(Privilege.Owned(S, life), "아직 안 산 상태에서 시작한다");
            Assert.AreEqual("구매", LabelOf(page, "CardBtn:4"), "안 산 카드의 버튼 글자는 «구매»");
            double g0 = S.Gem;
            Assert.IsTrue(ClickNamed(page, "CardBtn:4"), "«평생 다이아» 구매 버튼이 눌린다");
            yield return CloseReward();
            Assert.IsTrue(Privilege.Owned(S, life), "누르면 실제로 산 것이 된다");
            Assert.AreEqual(g0 + buyNow, S.Gem, 0.0001, "구매 즉시 보상이 표대로 들어온다");

            // ── ⓓ 산 뒤에는 그날치 «받기» 가 또 열린다.
            Assert.AreEqual("받기", LabelOf(page, "CardBtn:4"), "산 카드의 버튼 글자는 «받기»");
            double g1 = S.Gem;
            Assert.IsTrue(ClickNamed(page, "CardBtn:4"), "산 그날에도 그날치를 받는다");
            yield return CloseReward();
            Assert.AreEqual(g1 + daily, S.Gem, 0.0001, "매일 보상이 표대로 들어온다");

            // ── ⓔ «전체 받기» 는 남은 것을 쓸어 담는다(지금 남은 것은 안 산 카드 둘뿐이라 아무것도 안 준다 → 토스트).
            Assert.IsFalse(Privilege.AnyClaimable(S, PD, SaveStore.Today()), "산 카드·공짜 카드의 오늘 몫은 다 받았다");
            double g2 = S.Gem;
            var all = BtnNamed(page, "ClaimAllBtn");
            Assert.IsNotNull(all, "«전체 받기» 버튼");
            all.onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(g2, S.Gem, 0.0001, "받을 것이 없으면 «전체 받기» 도 한 톨도 안 준다");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator ClaimAllSweepsEveryCardThatIsDueInOnepress()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            var PD = D.Privilege; Assert.IsNotNull(PD);
            string today = SaveStore.Today();

            // 카드 셋을 미리 사 둔다(구매 보상은 이 자의 관심이 아니라 «전체 받기» 만 본다).
            foreach (var key in new[] { "adRemove", "monthly", "lifetime" }) Privilege.Buy(S, PD.Of(key), today);
            double due = 0;
            foreach (var c in PD.Cards) if (Privilege.Can(S, PD, c, today)) foreach (var r in c.Daily) if (r.Item == Mail.ItemGem) due += r.Amount;
            Assert.Greater(due, 0, "카드 넷의 오늘 몫이 남아 있어야 한다");

            _app.ShowScreen("privilege");
            yield return Frames(2);
            var page = _app.Current.Root;

            double g0 = S.Gem;
            Assert.IsTrue(ClickNamed(page, "ClaimAllBtn"), "«전체 받기» 가 눌린다");
            yield return CloseReward();
            Assert.AreEqual(g0 + due, S.Gem, 0.0001, "한 번에 카드 넷의 오늘 몫이 전부 들어온다");
            Assert.IsFalse(Privilege.AnyClaimable(S, PD, today), "쓸고 나면 남는 것이 없다");

            yield return Shutdown();
        }
    }
}
