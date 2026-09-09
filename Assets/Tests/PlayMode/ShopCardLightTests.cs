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
    /// T308 — <b>상점 카드의 빛살 정리</b>(주인 2026-09-09 09:1X «희귀 상자랑 전설 상자는 카드에서 라이트 이펙트 빼기 · 전설 상자 거는 패턴 효과 있게 하기» + «다이아 골드 카드도 라이트 이펙트 빼기»).
    /// <para>
    /// ⓐ 상점 화면에 남는 빛 담개는 <b>큰 카드(신화) 것 하나뿐</b>이다 — 주인이 큰 카드는 말한 적이 없어 그대로 두었고, 나머지(작은 상자 둘·다이아·골드)는 다 뺐다.
    /// ⓑ 전설 상자 카드에만 무늬가 깔리고 <b>움직인다</b>(<c>uvRect</c> 가 두 프레임 사이에 달라진다) · 희귀 카드에는 무늬가 없다.
    /// </para>
    /// ⚠ <b>«하나뿐» 로 재는 까닭</b> — 카드마다 «없다» 를 적으면 새 카드가 생길 때 자가 조용히 통과한다.
    /// 화면 전체에서 세면 <b>새로 늘어난 빛도 이 자가 먼저 말한다</b>(주인 지시는 «이 카드에서 빼라» 가 아니라 «이 화면에서 그 효과를 정리하라» 였다).
    /// </summary>
    public class ShopCardLightTests
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

        static List<Transform> AllNamed(Transform root, string name)
        {
            var o = new List<Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) o.Add(t);
            return o;
        }
        static bool Under(Transform t, Transform ancestor)
        {
            for (var p = t; p != null; p = p.parent) if (p == ancestor) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator OnlyTheBigChestCardKeepsItsLight()
        {
            yield return Boot();
            var D = _app.Data;
            _app.ShowScreen("shop"); yield return Frames(2);
            var root = _app.Current.Root;
            var content = UiKit.Find(root, "Content"); Assert.IsNotNull(content, "상점 Content");

            var big = UiKit.Find(content, "Box:" + ShopScreen.BigBox(D).Key); Assert.IsNotNull(big, "큰 상자 카드");
            var masks = AllNamed(root, UiKit.LightMaskName);
            Assert.AreEqual(1, masks.Count, "상점에 남는 빛 담개는 큰 카드 것 하나뿐이다(T308) — 지금 " + masks.Count + "개");
            Assert.IsTrue(Under(masks[0], big), "그 하나는 큰 카드(신화) 안에 있어야 한다");

            // 작은 상자 카드 둘 — 하나하나도 확인한다(위 «하나뿐» 이 큰 카드 쪽에서 틀어져도 이 줄이 자리를 짚어 준다)
            foreach (var box in D.Gacha.Boxes)
            {
                if (box.Key == ShopScreen.BigBox(D).Key) continue;
                var card = UiKit.Find(content, "Box:" + box.Key); Assert.IsNotNull(card, "상자 카드 " + box.Key);
                Assert.IsFalse(UiKit.HasLightMask(card), "작은 상자 카드 «" + box.Key + "» 에 빛살이 남아 있다(T308 이 뺀 것)");
            }
            _log.AssertNoRed("상점(빛살 정리)");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator OnlyTheLegendCardWearsAMovingPattern()
        {
            yield return Boot();
            var D = _app.Data;
            _app.ShowScreen("shop"); yield return Frames(2);
            var content = UiKit.Find(_app.Current.Root, "Content");

            var legend = UiKit.Find(content, "Box:" + GachaKeys.BoxLegend); Assert.IsNotNull(legend, "전설 상자 카드");
            var pattern = UiKit.Find(legend, UiKit.PatternName); Assert.IsNotNull(pattern, "전설 카드 안 무늬(T308 ⓑ)");
            var raw = pattern.GetComponent<RawImage>(); Assert.IsNotNull(raw, "무늬는 RawImage(uvRect 트윈)");
            Assert.Greater(raw.color.a, UiKit.PatternAlpha, "카드 무늬는 화면 배경 무늬(3/255)보다 진해야 보인다(ShopScreen.CardPatternAlpha)");

            // 움직이는가 — 두 프레임 사이에 uvRect 가 달라진다(DOTween 이 SetUpdate(true) 로 돈다)
            var before = raw.uvRect;
            yield return Frames(3);
            Assert.AreNotEqual(before, raw.uvRect, "전설 카드 무늬가 안 움직인다(주인 «패턴 효과 있게»)");

            // 희귀 카드에는 무늬가 없다 — 주인이 전설만 말했다
            var rare = UiKit.Find(content, "Box:" + GachaKeys.BoxRare); Assert.IsNotNull(rare, "희귀 상자 카드");
            Assert.IsNull(UiKit.Find(rare, UiKit.PatternName), "희귀 카드에는 무늬를 깔지 않는다(주인이 전설만 말했다)");

            _log.AssertNoRed("상점(전설 카드 무늬)");
            yield return Shutdown();
        }
    }
}
