using System.Collections;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T116 1단계 — 그라데이션 «두 색» 표(<see cref="GradientPalette"/>)의 계약. 레퍼런스를 <c>tools/ref_color.py</c> 로 재서 넣은 값이
    /// 카탈로그(<c>col.grad.*</c>)에서 실제로 읽히는지, 방향(카드는 어두운 위 → 밝은 아래 · 배경은 그 반대)이 지켜지는지 본다.
    /// 2단계에서 <c>UiKit.Gradient</c> 가 이 표를 쓰게 되면 «화면에 실제로 그 색이 깔렸나» 단언이 여기 붙는다.
    /// </summary>
    public class GradientPaletteTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; }

        static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(카탈로그 로드)");
            _app = App.I;
            yield return null;
        }

        [UnityTest]
        public IEnumerator MeasuredPairsComeFromTheCatalogAndKeepTheirDirection()
        {
            yield return Boot();
            Assert.IsNotNull(_app.Assets, "카탈로그");

            foreach (var name in GradientPalette.Names)
            {
                var p = GradientPalette.Of(name);
                Assert.IsTrue(GradientPalette.Has(name), name + " 은 표에 있어야 한다");
                // 카탈로그에서 읽혔는가 — 폴백(흰색 한 쌍)이 아니고 두 색이 서로 다르다
                Assert.AreNotEqual(p.Top, p.Bottom, name + " 은 위/아래가 다른 두 색이어야 한다(단색이면 그라데이션이 아니다)");
                Assert.Less(Luma(p.Top) + Luma(p.Bottom), 1.98f, name + " 이 흰색 한 쌍이면 카탈로그에서 못 읽은 것이다: " + p.Top + " / " + p.Bottom);
            }

            // 방향 — 카드류는 «어두운 위 → 밝은 아래»(레퍼런스 09·11 실측), 배경은 «밝은 위 → 어두운 아래»(레퍼런스 01 실측)
            // 3단계 ⓑ 에서 특권 카드 2~4 의 실측 쌍이 늘었다(cardPriv*) — 카드류는 다 같은 방향이어야 한다
            foreach (var card in new[] { "cardGem", "cardGold", "cardBlue", "cardPrivAd", "cardPrivMonth", "cardPrivLife" })
            {
                var p = GradientPalette.Of(card);
                Assert.Less(Luma(p.Top), Luma(p.Bottom), card + " 카드는 위가 어둡고 아래가 밝아야 한다(레퍼런스 방향)");
            }
            var bg = GradientPalette.Of("bgLobby");
            Assert.Greater(Luma(bg.Top), Luma(bg.Bottom), "화면 배경은 위가 밝고 아래가 어두워야 한다(레퍼런스 01 실측)");

            // 표에 없는 요소 — 바탕색에서 만든 두 색도 같은 방향이고 계열색(색상)을 잃지 않는다
            var baseC = new Color(0.35f, 0.55f, 0.30f, 1f);
            var made = GradientPalette.CardWay(baseC);
            Assert.Less(Luma(made.Top), Luma(made.Bottom), "CardWay 도 어두운 위 → 밝은 아래");
            Assert.Greater(made.Bottom.g, made.Bottom.r, "계열색(초록)이 유지돼야 한다");
            var madeBg = GradientPalette.BackgroundWay(baseC);
            Assert.Greater(Luma(madeBg.Top), Luma(madeBg.Bottom), "BackgroundWay 는 밝은 위 → 어두운 아래");
            Assert.AreEqual(baseC.a, made.Top.a, 0.001f, "알파는 건드리지 않는다");

            _log.AssertNoRed("그라데이션 두 색 표(T116)");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return null;
        }

        /// <summary>2단계 — <see cref="UiKit.GradientCard"/> 가 «어두운 위 → 밝은 아래» 로 실측 색을 깔고, 버튼·팝업은 얕게 깐다(레퍼런스가 단색이라).</summary>
        [UnityTest]
        public IEnumerator CardGradientPutsTheDarkColorOnTopAndFlatSurfacesStayFaint()
        {
            yield return Boot();
            var host = UiKit.Rect(_app.Frame, "T116Host"); UiKit.Pct(host, 10f, 10f, 60f, 30f);
            host.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.white;
            yield return null;

            // ⓐ 표 이름으로 — 실측 두 색이 그대로 tint 로 들어간다(어두운 위 · 밝은 아래)
            UiKit.GradientCard(host, "cardGem");
            var top = host.Find(UiKit.GradientTopName); var bottom = host.Find(UiKit.GradientBottomName);
            Assert.IsNotNull(top, "GradientTop"); Assert.IsNotNull(bottom, "GradientBottom");
            var ti = top.GetComponent<UnityEngine.UI.Image>(); var bi = bottom.GetComponent<UnityEngine.UI.Image>();
            var pair = GradientPalette.Of("cardGem");
            Assert.AreEqual(pair.Top.r, ti.color.r, 0.01f, "위 = 실측 어두운 색"); Assert.AreEqual(pair.Bottom.b, bi.color.b, 0.01f, "아래 = 실측 밝은 색");
            Assert.Less(Luma(ti.color), Luma(bi.color), "카드는 어두운 위 → 밝은 아래(레퍼런스 방향)");
            Assert.AreEqual(UiKit.GradientCardAlpha, ti.color.a, 0.001f, "색이 읽히는 세기");
            Assert.IsFalse(ti.raycastTarget); Assert.IsFalse(bi.raycastTarget);
            Assert.AreEqual(top.GetSiblingIndex() + 1, bottom.GetSiblingIndex(), "위·아래는 이웃한 형제");
            Assert.IsTrue(UiKit.HasGradient(host), "HasGradient 는 그대로 참");

            // ⓑ 표에 없는 칸 — 바탕색에서 계열색을 유지한 채 만든다(두 번 불러도 조각이 안 늘어난다)
            UiKit.GradientCard(host, baseColor: new Color(0.30f, 0.55f, 0.35f, 1f));
            int tops = 0, bottoms = 0;
            for (int i = 0; i < host.childCount; i++)
            {
                if (host.GetChild(i).name == UiKit.GradientTopName) tops++;
                if (host.GetChild(i).name == UiKit.GradientBottomName) bottoms++;
            }
            Assert.AreEqual(1, tops); Assert.AreEqual(1, bottoms);
            ti = host.Find(UiKit.GradientTopName).GetComponent<UnityEngine.UI.Image>();
            bi = host.Find(UiKit.GradientBottomName).GetComponent<UnityEngine.UI.Image>();
            Assert.Less(Luma(ti.color), Luma(bi.color), "바탕색으로 만들어도 어두운 위 → 밝은 아래");
            Assert.Greater(bi.color.g, bi.color.r, "계열색(초록)이 유지된다");

            // ⓒ 버튼·팝업 패널은 레퍼런스가 단색이라 얕다 — 배경 기본(0.12/0.18)보다 작아야 한다
            Assert.Less(UiKit.GradientFlatTopAlpha, UiKit.GradientTopAlpha, "버튼·패널 위 덧칠은 배경보다 얕다");
            Assert.Less(UiKit.GradientFlatBottomAlpha, UiKit.GradientBottomAlpha, "버튼·패널 아래 덧칠은 배경보다 얕다");
            Assert.Greater(UiKit.GradientFlatTopAlpha, 0.05f, "그래도 0 은 아니다(게이트 하한)");

            Object.Destroy(host.gameObject);
            yield return null;
            _log.AssertNoRed("카드 그라데이션(T116 2단계)");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return null;
        }
    }
}
