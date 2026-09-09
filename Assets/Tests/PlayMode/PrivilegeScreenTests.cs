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

        /// <summary>
        /// T306 — <b>켜져 있는 버튼 옷</b>(주황·회색 두 벌 가운데). 이름은 «어느 것을 볼까» 의 손잡이일 뿐이고,
        /// 재는 것은 <see cref="SkinInk"/> 가 주는 <b>조각이 들고 온 색</b>이다(이름으로 판정하면 내가 붙인 이름을 내가 다시 읽는 거울이 된다 · 결정 906).
        /// </summary>
        static Transform LiveSkin(Transform page, string name)
        {
            var warm = Find(page, name); var cold = Find(page, name + "#Gray");
            if (warm != null && warm.gameObject.activeInHierarchy) return warm;
            if (cold != null && cold.gameObject.activeInHierarchy) return cold;
            return null;
        }
        /// <summary>
        /// 버튼이 실제로 입고 있는 <b>잉크</b> — 조각(프리팹)이 제 배경 그림에 박아 둔 색이다.
        /// <para>
        /// ⚠ <b>스프라이트 이름으로 재면 안 된다</b> — 이 조각들의 <c>m_Sprite</c> 는 레포에 없는 guid 를 가리켜 런타임에 null 이고
        /// (Layer Lab 원본 텍스처가 안 들어와 있다), 눈에 보이는 것은 조각이 박아 둔 <c>m_Color</c> 다. 즉 «그림 이름» 으로 재는 자는
        /// 두 옷 다 빈 글자를 보고 <b>조용히 통과</b>한다. 여기서 읽는 색은 <b>내 코드가 쓰는 값이 아니라</b> 조각이 들고 온 값이라 거울이 아니다(결정 906).
        /// </para>
        /// </summary>
        static Color SkinInk(Transform t)
        {
            if (t == null) return Color.clear;
            var g = UiKit.PressTarget(t, t.GetComponent<Image>());
            return g != null ? g.color : Color.clear;
        }
        static bool SameInk(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;

        /// <summary>
        /// T306(주인 2026-09-09 09:1X «구매 전 주황 · 받기 전 주황 · 받은 후 못 받는 상태 회색 · 전체 받기는 받을 게 있을 때만 주황») —
        /// <b>버튼 옷이 «지금 할 것이 있는가» 를 따라간다.</b>
        /// <para>
        /// 색 이름(#RRGGBB)도, 조각 키도 안 박는다. 재는 것은 <b>관계</b> 셋이다 —
        /// ⓐ «구매 전» 과 «받기 전» 이 <b>같은 옷</b>이고 ⓑ «받은 뒤» 는 <b>다른 옷</b>이며 ⓒ 그 다른 옷이 «받을 것이 없는 전체 받기» 와 <b>같은 옷</b>이다.
        /// 조각을 갈아 끼우거나 주황이 다른 주황이 되어도 이 셋은 그대로 서고, 규칙이 끊기면 셋 다 무너진다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ButtonSkinFollowsWhetherThereIsAnythingToDo()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save; var PD = D.Privilege;
            Assert.IsNotNull(PD, "privilege.json");
            string today = SaveStore.Today();

            _app.ShowScreen("privilege");
            yield return Frames(2);
            var page = _app.Current.Root;

            // ── ⓐ 아직 아무것도 안 한 화면: 공짜 카드는 «받기 전», 산 적 없는 카드는 «구매 전» — 주인 말로 둘 다 주황이다.
            Assert.IsFalse(Privilege.Owned(S, PD.Of("lifetime")), "«평생 다이아» 는 아직 안 샀다");
            Assert.IsTrue(Privilege.Can(S, PD, PD.Of("dailyGift"), today), "공짜 카드의 오늘 몫이 남아 있다");
            var warm = SkinInk(LiveSkin(page, "CardBtn:4"));   // 구매 전
            Assert.Greater(warm.a, 0.01f, "«구매 전» 카드에 켜져 있는(= 보이는) 버튼 옷이 있어야 한다");
            // 두 벌이 정말 «다른 옷» 인가 — 같은 옷을 두 벌 세워 두면 아래 단언이 전부 조용히 통과한다.
            Assert.IsFalse(SameInk(warm, SkinInk(Find(page, "CardBtn:4#Gray"))),
                "겹쳐 둔 두 벌은 서로 다른 색이어야 한다(같으면 이 자가 아무것도 못 잰다)");
            Assert.IsTrue(SameInk(warm, SkinInk(LiveSkin(page, "CardBtn:1"))),
                "주인 규칙 — «구매 전» 과 «받기 전» 은 같은 옷(주황)이다");

            // ── ⓑ 공짜 카드를 받으면 그 카드는 «오늘은 더 할 것이 없다» → 옷이 바뀐다.
            Assert.IsTrue(ClickNamed(page, "CardBtn:1"), "«받기» 가 눌린다");
            yield return CloseReward();
            var cold = SkinInk(LiveSkin(page, "CardBtn:1"));
            Assert.IsFalse(SameInk(warm, cold),
                "주인 규칙 — 받은 뒤(이제 못 받는 상태)의 옷은 앞의 주황과 달라야 한다(회색)");

            // ── ⓒ 사고 그날치까지 받은 카드도 같은 «회색» 으로 간다 — 갈래가 달라도 도착하는 옷은 하나다.
            Assert.IsTrue(ClickNamed(page, "CardBtn:4"), "«구매» 가 눌린다");
            yield return CloseReward();
            Assert.IsTrue(SameInk(warm, SkinInk(LiveSkin(page, "CardBtn:4"))),
                "산 그날 아직 그날치가 남았으면 여전히 주황(«받기 전»)이다");
            Assert.IsTrue(ClickNamed(page, "CardBtn:4"), "산 그날의 «받기» 가 눌린다");
            yield return CloseReward();
            Assert.IsTrue(SameInk(cold, SkinInk(LiveSkin(page, "CardBtn:4"))),
                "다 받은 카드는 공짜 카드와 같은 회색으로 간다");

            // ── ⓓ «전체 받기» — 지금 남은 것은 안 산 카드 둘뿐이라 받을 것이 없다 → 회색.
            Assert.IsFalse(Privilege.AnyClaimable(S, PD, today), "받을 것이 없는 상태를 만든다");
            Assert.IsTrue(SameInk(cold, SkinInk(LiveSkin(page, "ClaimAllBtn"))),
                "주인 규칙 — 받을 것이 없으면 «전체 받기» 는 회색");

            // ── ⓔ 카드 하나를 더 사서 받을 것을 만들면 «전체 받기» 가 주황으로 돌아온다(한 방향만 재면 늘 회색인 코드도 통과한다).
            Assert.IsTrue(Privilege.Buy(S, PD.Of("monthly"), today), "월간 카드를 산다");
            _app.Current.Refresh();
            yield return Frames(1);
            Assert.IsTrue(Privilege.AnyClaimable(S, PD, today), "이제 받을 것이 있다");
            Assert.IsTrue(SameInk(warm, SkinInk(LiveSkin(page, "ClaimAllBtn"))),
                "주인 규칙 — 받을 것이 있으면 «전체 받기» 는 주황");

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
