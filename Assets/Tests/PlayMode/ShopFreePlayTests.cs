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
    /// T259 1·3항 — 상점의 «하루 1번» 자리 둘을 <b>눌러서</b> 잰다(주인 2026-09-09).
    /// <para>
    /// ⓐ 상자 카드의 광고 버튼 = <b>광고를 본 뒤 그 상자 1회 오픈</b>(여태는 무료 다이아였다 · 주인 «지금 다른 방식인 것 같음»)
    /// ⓑ 그 판에서 <b>다이아·열쇠가 안 줄고</b> 뽑기 결과 창이 뜬다 ⓒ 같은 날 두 번째는 <b>막힌다</b>(버튼이 회색)
    /// ⓓ 다이아 100 상품의 가격 버튼이 <b>«Free»</b> 로 떠 있고, 누르면 원화 없이 지급되고 <b>제 가격으로 돌아온다</b>
    /// ⓔ 다이아와 골드는 <b>서로 안 잠근다</b>(주인 08:3X «걍 하루에 1번» · 각각) ⓕ 빨간 줄 0.
    /// </para>
    /// <para>
    /// 규칙 자체(하루 1번 · 마이그레이션)는 EditMode <c>ShopFreeTests</c> 가 못 박고, 여기서는 <b>화면이 그 규칙을 실제로 부르는가</b>만 본다 —
    /// 「버튼이 있다」와 「눌리면 그 일이 난다」는 다른 말이다(T169 2항 · T272 가 같은 자리에서 센 것).
    /// </para>
    /// ⚠ 광고는 <see cref="Overlay.AdCountdown"/> 라 <b>누른 그 프레임에 주지 않는다</b> — 기다리는 꼴은 <c>ExpeditionScreenTests</c> 것을 그대로 썼다(결정 719).
    /// </summary>
    public class ShopFreePlayTests
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
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator OpenShop() { _app.ShowScreen("shop"); yield return Frames(3); }

        /// <summary>광고 오픈이 있는 상자(희귀·전설) 중 <b>작은 카드로 서는</b> 것 하나 — 큰 카드(가장 비싼 상자)에는 광고 버튼이 없다.</summary>
        Transform AdCard()
        {
            var D = _app.Data;
            GachaBox big = null; foreach (var b in D.Gacha.Boxes) if (big == null || b.Cost > big.Cost) big = b;
            foreach (var b in D.Gacha.Boxes)
            {
                if (b == big) continue;
                var card = UiKit.Find(_app.Current.Root, "Box:" + b.Key);
                if (card != null && UiKit.Find(card, "Ad") != null) return card;
            }
            return null;
        }
        static Button Btn(Transform root, string name) { var t = UiKit.Find(root, name); return t != null ? t.GetComponent<Button>() : null; }
        static string PriceText(Transform slot)
        {
            var t = UiKit.Find(slot, "Button_Price/GroupArea/Group/Text (TMP)");
            var x = t != null ? t.GetComponent<TMP_Text>() : null;
            return x != null ? (x.text ?? "").Trim() : null;
        }

        [UnityTest]
        public IEnumerator TheAdButtonOpensThatBoxOnceADayAndCostsNoGems()
        {
            yield return Boot();
            var S = _app.Save; S.Gem = 0; S.Gold = 0;   // 다이아 0 이라 «값을 치른» 갈래면 열릴 수가 없다 — 열리면 광고로 연 것이다
            yield return OpenShop();

            var card = AdCard();
            Assert.IsNotNull(card, "광고 버튼이 있는 상자 카드");
            var ad = Btn(card, "Ad");
            Assert.IsNotNull(ad, "광고 버튼");
            Assert.IsTrue(ad.interactable, "오늘 몫이 남았으면 켜져 있다");

            int pullsBefore = S.Pulls; int invBefore = S.Inv.Count;
            ad.onClick.Invoke(); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "누르면 먼저 모의 광고가 뜬다(그 자리에서 열지 않는다)");

            float t0 = Time.realtimeSinceStartup;
            while (S.Pulls == pullsBefore && Time.realtimeSinceStartup - t0 < 8f) yield return null;
            yield return Frames(2);

            Assert.AreEqual(pullsBefore + 1, S.Pulls, "광고가 끝나면 그 상자가 실제로 1회 열린다(버튼이 도는 것과 여는 것은 다르다)");
            Assert.Greater(S.Inv.Count, invBefore, "연 만큼 장비가 들어온다 — 다이아로 연 것과 같은 경로다");
            Assert.AreEqual(0, S.Gem, 1e-6, "다이아는 한 톨도 안 든다(광고가 값이다)");

            _log.AssertNoRed("상자 광고 오픈");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator TheSecondAdOpenOnTheSameDayIsBlocked()
        {
            yield return Boot();
            var S = _app.Save; S.Gem = 0;
            // 오늘 몫을 이미 쓴 세이브로 연다 — 광고를 두 번 돌리는 것보다 이 편이 «막히는가» 하나만 잰다.
            ShopFree.Take(S, ShopFree.BoxRare, SaveStore.Today());
            ShopFree.Take(S, ShopFree.BoxLegend, SaveStore.Today());
            yield return OpenShop();

            var card = AdCard();
            Assert.IsNotNull(card);
            var ad = Btn(card, "Ad");
            Assert.IsNotNull(ad);
            Assert.IsFalse(ad.interactable, "오늘 몫을 쓴 상자의 광고 버튼은 회색이다 — 이게 없으면 무제한으로 열린다");

            int pullsBefore = S.Pulls;
            ad.onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(pullsBefore, S.Pulls, "눌러도 안 열린다");

            _log.AssertNoRed("상자 광고 오픈(오늘 몫 소진)");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator TheFreeSupplyProductShowsFreeThenGoesBackToItsPrice()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            Assert.IsNotNull(D.Shop, "상품표(shop.json)");
            var pack = D.Shop.FreeGemPack;
            Assert.IsNotNull(pack, "표가 지목한 «무료 보급» 다이아 줄(shop.json 의 free)");
            int idx = D.Shop.GemPacks.IndexOf(pack);
            S.Gem = 0;
            yield return OpenShop();

            var slot = UiKit.Find(_app.Current.Root, "GemPack:" + idx);
            Assert.IsNotNull(slot, "그 상품 칸");
            Assert.AreEqual("Free", PriceText(slot), "오늘 몫이 남았으면 가격 버튼이 «Free» 다(주인 ««1000원» 이라는 버튼이 «Free» 로 바뀌고»)");

            var btn = Btn(slot, "Button_Price");
            Assert.IsNotNull(btn, "가격 버튼");
            btn.onClick.Invoke(); yield return Frames(2);

            Assert.AreEqual(pack.Gem, S.Gem, 1e-6, "누르면 그 상품이 그대로 들어온다");
            Assert.IsFalse(ShopFree.Can(S, ShopFree.Gem, SaveStore.Today()), "오늘 몫을 썼다");
            Assert.AreNotEqual("Free", PriceText(slot), "쓰고 나면 원래 가격 버튼으로 돌아온다(주인 문장)");

            // 둘은 각각 하루 1번이다 — 다이아를 받았다고 골드까지 잠기면 날짜 도장이 하나뿐이던 시절로 돌아간 것이다.
            Assert.IsTrue(ShopFree.Can(S, ShopFree.Gold, SaveStore.Today()), "골드 쪽은 그대로 남는다");

            _log.AssertNoRed("무료 보급 상품");
            yield return Shutdown();
        }
    }
}
