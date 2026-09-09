using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T302 — 패스(19) 열 그라데이션이 <b>표 색 그대로</b> 그려지는가(주인 2026-09-09 «패스 부분 그라데이션이 전혀 레퍼런스랑 다름 · 해결하소»).
    /// <para>
    /// ⚠ <b>이 자는 «찍힌 픽셀» 을 본다</b> — 조각이 제대로 섰는가가 아니라 <b>사람 눈에 무슨 색이 보이는가</b> 가 물음이라서다.
    /// 원래 고장은 «열 바탕을 흰색으로 깔고 그 위에 <b>페이드</b> 조각을 얹은» 것이었는데, 코드만 읽으면 멀쩡해 보인다
    /// (표 값도 맞았다 · <c>GradientCard</c> 도 제대로 불렀다). 화면에서만 «표 색 ½ + 흰색 ½» 로 죽어 있었다.
    /// </para>
    /// <para>
    /// 그래서 재는 것은 <b>«표 색에 가까운가, 아니면 흰색과 반씩 섞인 색에 가까운가»</b> 두 갈래다 —
    /// 페이드 조각의 농도 곡선을 모르니 가운데 값을 못 박지 않고, <b>고장 났을 때의 색</b>을 같이 견주어 갈래를 가른다.
    /// 수는 하나도 안 베낀다(결정 555): 표(<see cref="GradientPalette"/>)에서 읽어 온다.
    /// </para>
    /// 표본 자리는 <b>열의 왼쪽 여백</b>이다 — 보상 칸이 열 가운데를 덮으므로 칸 없는 띠에서 재야 «칸 색» 을 물지 않는다
    /// (T266 1단계에서 그라데이션을 두 번 잘못 잰 자리가 바로 이것이다 · 결정 723).
    /// </summary>
    public class SeasonPassLookTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>열 이름 → (표 이름, 칸이 없는 왼쪽 여백의 x%) — 자리 수는 화면 코드의 rect 와 같은 뜻이다(열 왼쪽 끝 ~ 칸 왼쪽 끝 사이).</summary>
        static readonly (string Col, string Grad, float X)[] Cols =
        {
            ("Col:free",  "passFree",  5.0f),    // 열 1.9~31.9 · 칸 11.7~29.2
            ("Col:paid1", "passPaid1", 38.0f),   // 열 35.3~65.9 · 칸 41.7~61.1
            ("Col:paid2", "passPaid2", 70.0f),   // 열 66.7~98.2 · 칸 75.0~92.8
        };
        /// <summary>열 위쪽 표본 — 열 윗변(31.4%) 바로 아래이면서 첫 칸(35.1%)보다 위.</summary>
        const float TopY = 32.6f;

        static float Dist(Color32 a, Color b)
        {
            float dr = a.r - b.r * 255f, dg = a.g - b.g * 255f, db = a.b - b.b * 255f;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        [UnityTest]
        public IEnumerator ColumnGradientsKeepTheTableColorInsteadOfFadingToWhite()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I;
            yield return Frames(2);

            SeasonPassScreen.Open(_app);
            yield return Frames(3);
            UiKit.CompleteAllTweens();
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();

            Assert.IsTrue(PlayShot.Save(_app, "t302_pass_columns", null), "촬영이 PNG 를 만들어야 한다");
            var png = PlayShot.LastPng;
            Assert.IsNotNull(png, "PlayShot.LastPng");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(png), "PNG 디코딩");
            var px = tex.GetPixels32();
            int W = tex.width, H = tex.height;

            foreach (var c in Cols)
            {
                Assert.IsTrue(GradientPalette.Has(c.Grad), "표에 " + c.Grad + " 가 있다");
                var pair = GradientPalette.Of(c.Grad);

                int ix = Mathf.Clamp(Mathf.RoundToInt(W * c.X / 100f), 0, W - 1);
                int iy = Mathf.Clamp(Mathf.RoundToInt(H * (1f - TopY / 100f)), 0, H - 1);   // 화면 %는 위에서, 텍스처는 아래에서 센다
                var got = px[iy * W + ix];

                float toTable = Dist(got, pair.Top);
                float toWashed = Dist(got, Color.Lerp(pair.Top, Color.white, 0.5f));
                string where = c.Col + " 위쪽(" + c.X + "%, " + TopY + "%) 실제 "
                             + string.Format("#{0:X2}{1:X2}{2:X2}", got.r, got.g, got.b);

                // ⓐ 고장 났을 때의 색(표 색 ½ + 흰색 ½)보다 **표 색에 더 가깝다** — 이것이 주인이 본 그 차이다.
                Assert.Less(toTable, toWashed,
                    where + " — 표 색보다 «흰색과 반씩 섞인 색» 에 더 가깝다(열 바탕이 흰색으로 비치고 있다 · T302)");
                // ⓑ 그리고 아주 멀지는 않다. ⚠ **문턱을 좁게 잡지 않았다** — 워커는 PlayMode 를 못 돌리므로(결정 143)
                //   이 수가 실제로 어디쯤 떨어지는지 내가 못 보고 넣는다. 좁게 잡으면 **화면이 옳은데 자가 빨개진다**.
                //   갈래를 가르는 일은 ⓐ 가 이미 한다(문턱이 없다) — ⓑ 는 «영 딴 색» 만 잡으면 된다.
                //   고장 났을 때의 거리는 흰색과 반씩이라 ≈192 이므로 90 이면 그 갈래와 겹치지 않는다.
                Assert.Less(toTable, 90f, where + " — 표 색에서 너무 멀다(기대 " + ColorUtility.ToHtmlStringRGB(pair.Top) + ")");
            }
            Object.Destroy(tex);

            _log.AssertNoRed("패스 열 그라데이션(T302)");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(2);
        }
    }
}
