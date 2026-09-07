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
    /// T126 «촬영 색공간» 게이트 — <see cref="PlayShot"/> 이 남기는 PNG 가 <b>화면과 같은 색</b>(sRGB)이어야 한다.
    /// 이 프로젝트는 Linear 색공간이고 <c>RenderTextureDescriptor</c> 의 <c>sRGB</c> 기본값이 false 라, 한 줄이 빠지면
    /// 카메라가 «선형» 값을 그대로 써 넣어 <c>screens</c> PNG 가 통째로 어둡고 붉게 나온다(코드 #2C2B29 → PNG #060606).
    /// 그림은 «멀쩡한 화면» 처럼 보이므로 크기·테두리·글자 게이트는 아무것도 못 잡았고, §5 색 비평만 조용히 틀렸다.
    /// 그래서 여기서 <b>아는 색 한 장을 찍어 되읽는다</b> — 화면 구도와 무관한 촬영 파이프라인만 재는 자다.
    /// </summary>
    public class PlayShotColorTests
    {
        App _app; PlayLog _log;
        /// <summary>찍어 볼 색 = 상단 프레임 띠(#2C2B29). 선형으로 새면 #060606 이 되어 둘이 확실히 갈린다.</summary>
        static readonly Color32 Probe = new Color32(0x2C, 0x2B, 0x29, 255);
        /// <summary>같은 색이 «선형» 으로 샜을 때의 값 — 이 값이 나오면 sRGB 설정이 되돌아간 것이다.</summary>
        static readonly Color32 Leaked = new Color32(0x06, 0x06, 0x06, 255);
        const int Tol = 6;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; }

        [UnityTest]
        public IEnumerator ScreenshotPngKeepsTheColorTheScreenShows()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I;
            yield return null;
            yield return null;

            // 캔버스 맨 위에 아는 색 한 장을 전면으로 깐다 — RT 가운데 픽셀은 반드시 이 색이다
            var probe = UiKit.Rect(_app.UiCanvas.transform, "T126:ColorProbe");
            UiKit.Stretch(probe);
            var img = probe.gameObject.AddComponent<Image>();
            img.color = Probe;
            img.raycastTarget = false;
            probe.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            yield return null;

            bool shot = PlayShot.Save(_app, "t126_colorspace", null);
            var png = PlayShot.LastPng;
            Color32 c = default; bool decoded = false;
            if (png != null)
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                decoded = tex.LoadImage(png);
                if (decoded) c = tex.GetPixels32()[tex.width / 2 + tex.height / 2 * tex.width];
                Object.Destroy(tex);
            }
            Object.Destroy(probe.gameObject);
            yield return null;

            Assert.IsTrue(shot, "촬영(PlayShot.Save)이 PNG 를 만들어야 한다");
            Assert.IsNotNull(png, "PlayShot.LastPng 가 채워져야 한다(folder=null 은 파일 대신 바이트)");
            Assert.IsTrue(decoded, "촬영한 PNG 가 디코딩돼야 한다");

            string got = string.Format("#{0:X2}{1:X2}{2:X2}", c.r, c.g, c.b);
            Assert.AreEqual(255, c.a, "촬영 PNG 는 불투명해야 한다 — " + got);
            bool leaked = Mathf.Abs(c.r - Leaked.r) <= Tol && Mathf.Abs(c.g - Leaked.g) <= Tol && Mathf.Abs(c.b - Leaked.b) <= Tol;
            Assert.IsFalse(leaked, "촬영 PNG 가 «선형» 값으로 저장됐다(" + got + ") — PlayShot 의 desc.sRGB = true 가 빠졌는지 본다(T126)");
            Assert.LessOrEqual(Mathf.Abs(c.r - Probe.r), Tol, "R 이 화면 색과 달라졌다 — 찍은 색 #2C2B29 · 나온 색 " + got);
            Assert.LessOrEqual(Mathf.Abs(c.g - Probe.g), Tol, "G 이 화면 색과 달라졌다 — 찍은 색 #2C2B29 · 나온 색 " + got);
            Assert.LessOrEqual(Mathf.Abs(c.b - Probe.b), Tol, "B 가 화면 색과 달라졌다 — 찍은 색 #2C2B29 · 나온 색 " + got);

            Time.timeScale = 1f;
            if (_app != null)
            {
                if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject);
                Object.Destroy(_app.gameObject);
            }
            _app = null;
            yield return null;
            yield return null;
        }
    }
}
