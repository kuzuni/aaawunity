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
    /// T199 — 팝업 뒤 어둠이 <b>레퍼런스만큼</b> 어두운가(주인 레퍼런스 = 정본).
    /// <para>
    /// 워커 H 가 `screens` run 385 를 재서 넘긴 표(결정 512) — 상단 재화 바 띠(세로 3.5~9.0%)의 <b>가장 밝은 픽셀</b>:
    /// 안 덮인 01 로비 <b>1.000</b> · 우리 팝업 뒤 <b>0.432</b> · 레퍼런스 <b>0.141</b>.
    /// 팝업이 덮은 열한 화면이 전부 0.432 라 «자리마다의 실수» 가 아니라 <b>어둠 한 곳</b>이었다.
    /// </para>
    /// <para>
    /// 뿌리는 색공간이다 — Linear 에서 섞이므로 α 0.85 는 <c>0.15×1.0 + 0.85×선형(#12131A)</c> = sRGB <b>0.43</b> 이 된다.
    /// 그래서 <see cref="UiKit.DimAlpha"/> 를 <b>0.985</b> 로 올렸다(남는 밝기는 거의 전부 <c>(1−α)×흰색</c> 항이라 α 만이 지렛대다).
    /// </para>
    /// 이 자는 «화면을 실제로 찍어» 그 띠를 되읽는다 — 코드의 α 를 믿지 않고 <b>합성된 픽셀</b>을 본다
    /// (α 를 되돌리거나, 어둠이 상단 띠를 못 덮게 되면 여기서 빨개진다).
    /// </summary>
    public class DimDarknessTests
    {
        App _app; PlayLog _log;
        /// <summary>재는 띠 = 상단 재화 바(프레임 세로 3.5~9.0% · 워커 H 가 쓴 자리 그대로).</summary>
        const float BandTop = 0.035f, BandBottom = 0.090f;
        /// <summary>판정선 — 레퍼런스 0.141 과 우리 종전 0.432 사이에서, 레퍼런스 쪽에 붙여 잡는다(등재 5항 «자는 ≤ 0.20 한 줄»).</summary>
        const float MaxAllowed = 0.20f;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static float Luma(Color32 c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;

        /// <summary>찍은 PNG 의 상단 띠에서 «평균 · 상위 5% · 최대» 밝기를 돌려준다(워커 H 의 표와 같은 자).</summary>
        static bool BandStats(byte[] png, out float mean, out float p95, out float max)
        {
            mean = p95 = max = -1f;
            if (png == null) return false;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(png)) { Object.Destroy(tex); return false; }
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();
            var vals = new System.Collections.Generic.List<float>(w * 64);
            // GetPixels32 는 **아래에서 위로** 담기므로 «화면 위 띠» 는 큰 y 쪽이다
            int yTop = Mathf.Clamp(Mathf.RoundToInt(h * (1f - BandBottom)), 0, h - 1);
            int yBot = Mathf.Clamp(Mathf.RoundToInt(h * (1f - BandTop)), 0, h - 1);
            for (int y = yTop; y <= yBot; y++)
                for (int x = 0; x < w; x++)
                    vals.Add(Luma(px[x + y * w]));
            Object.Destroy(tex);
            if (vals.Count == 0) return false;
            vals.Sort();
            float sum = 0f; foreach (var v in vals) sum += v;
            mean = sum / vals.Count;
            p95 = vals[Mathf.Clamp((int)(vals.Count * 0.95f), 0, vals.Count - 1)];
            max = vals[vals.Count - 1];
            return true;
        }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);
            _log.AssertNoRed("부팅");
        }

        /// <summary>팝업을 열고 실제로 찍어, 상단 재화 바 띠가 레퍼런스만큼 어두운지 본다.</summary>
        [UnityTest]
        public IEnumerator PopupDimIsAsDarkAsTheReference()
        {
            yield return Boot();

            // 덮이기 «전» 을 먼저 재 둔다 — 이 자가 «어둠» 을 재고 있다는 증거가 된다(안 덮인 화면은 밝아야 한다)
            Canvas.ForceUpdateCanvases(); yield return Frames(1);
            Assert.IsTrue(PlayShot.Save(_app, "t199_before", null), "촬영(전)");
            float m0, p0, x0;
            Assert.IsTrue(BandStats(PlayShot.LastPng, out m0, out p0, out x0), "찍은 PNG 를 되읽는다(전)");

            // 공통 팝업 하나를 연다(어둠은 UiKit.Popup 한 곳에서 온다)
            _app.Overlay.Settings();
            yield return Frames(2); UiKit.CompleteAllTweens(); yield return Frames(1);
            var box = UiKit.Find(_app.Overlay.Root, "ui.popup"); Assert.IsNotNull(box, "설정 = 공통 팝업 상자");
            var dim = UiKit.Find(_app.Overlay.Root, "Dimmed"); Assert.IsNotNull(dim, "팝업 뒤 어둠(Dimmed)");

            Canvas.ForceUpdateCanvases(); yield return Frames(1);
            Assert.IsTrue(PlayShot.Save(_app, "t199_after", null), "촬영(후)");
            float m1, p1, x1;
            Assert.IsTrue(BandStats(PlayShot.LastPng, out m1, out p1, out x1), "찍은 PNG 를 되읽는다(후)");

            Debug.Log($"[T199] 상단 띠 밝기 — 덮이기 전 평균 {m0:0.000}/상위5% {p0:0.000}/최대 {x0:0.000}" +
                      $" → 팝업 뒤 평균 {m1:0.000}/상위5% {p1:0.000}/최대 {x1:0.000}" +
                      $" (레퍼런스 최대 0.141 · 종전 0.432 · DimAlpha {UiKit.DimAlpha})");

            Assert.Greater(x0, MaxAllowed, "덮이기 «전» 은 밝아야 한다 — 그래야 이 자가 어둠을 재고 있는 것이다");
            Assert.LessOrEqual(x1, MaxAllowed,
                $"팝업 뒤 상단 띠의 가장 밝은 픽셀이 {MaxAllowed} 이하여야 한다(레퍼런스 0.141 · 선형 합성 탓에 α 0.85 는 0.43 이었다 · T199)");
            Assert.Less(x1, x0, "팝업이 덮으면 그 띠는 덮이기 전보다 어두워야 한다");
            _log.AssertNoRed("팝업 어둠");
        }
    }
}
