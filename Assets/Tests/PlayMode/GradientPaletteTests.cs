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
            foreach (var card in new[] { "cardGem", "cardGold", "cardBlue" })
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
    }
}
