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
    /// T182 <b>3단계-2</b> — 세로 신축 «배선» 의 자. 셈(<see cref="Stretch"/>)은 EditMode 가 이미 재고, 여기서는 <b>그 셈이 화면 좌표에 닿는 길</b>을 본다.
    /// <list type="bullet">
    /// <item>ⓐ 지금(프레임 9:19.5 고정)은 <see cref="UiKit.FrameK"/> 가 <b>1</b> 이고 <see cref="UiKit.Pct"/> 결과가 표 그대로다 — <b>회귀 0</b>.</item>
    /// <item>ⓑ 9:21 짜리 프레임을 흉내 내면(<see cref="UiKit.FrameKOverride"/>) 위·아래 줄은 <b>픽셀을 지키고</b> 가운데가 늘어난다.</item>
    /// <item>ⓒ <b>프레임 칸이 아닌 곳</b>(팝업 상자 안 · 목록 칸 안)은 신축을 <b>안 받는다</b> — 두 번 걸리면 두 배로 밀린다.</item>
    /// <item>ⓓ 꽉 채운 겹(<c>Overlay.Root</c> 같은 층)을 지나 프레임에 닿는 자리는 프레임 칸으로 센다.</item>
    /// </list>
    /// 3단계-3 이 <c>CreateFrame</c> 을 «높이는 화면에서 받는다» 로 바꾸면 이 주입 없이 같은 길이 돈다.
    /// </summary>
    public class FrameStretchWiringTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { UiKit.FrameKOverride = null; SafeAreaRoot.Override = null; _log?.Dispose(); _log = null; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

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
        IEnumerator Shutdown()
        {
            UiKit.FrameKOverride = null;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        /// <summary>표의 한 줄을 그 부모 밑에 놓고 «위 %·높이 %» 를 되읽는다(앵커에서 되돌린다 — 그것이 실제로 그려지는 값이다).</summary>
        static void Place(Transform parent, string name, float y, float h, out float gotY, out float gotH)
        {
            var rt = UiKit.Rect(parent, name);
            UiKit.Pct(rt, 10f, y, 80f, h);
            gotY = (1f - rt.anchorMax.y) * 100f;
            gotH = (rt.anchorMax.y - rt.anchorMin.y) * 100f;
        }

        [UnityTest]
        public IEnumerator FrameRowsStretchOnlyInFrameSpaceAndNothingMovesToday()
        {
            yield return Boot();
            var frame = _app.Frame;
            Assert.AreSame(frame, UiKit.Frame, "UiKit 이 들고 있는 프레임 = 앱의 프레임");

            // ⓐ 오늘 — 프레임이 9:19.5 고정이라 배수가 1 이고 표가 그대로 나온다(회귀 0)
            Assert.AreEqual(1f, UiKit.FrameK, 0.001f, "지금 프레임은 기준 비율이라 신축 0");
            foreach (var (y, h) in new[] { (3.7f, 4.5f), (30.0f, 21.0f), (69.5f, 30.5f), (92.6f, 7.4f) })
            {
                Place(frame, "T182:today", y, h, out var gy, out var gh);
                Assert.AreEqual(y, gy, 0.02f, "오늘은 표 그대로여야 한다(위 %)");
                Assert.AreEqual(h, gh, 0.02f, "오늘은 표 그대로여야 한다(높이 %)");
            }

            // ⓑ 9:21 프레임을 흉내 낸다 — 위·아래는 픽셀 유지 · 가운데는 늘어난다
            float k = Stretch.K(9f, 21f);
            UiKit.FrameKOverride = k;
            Assert.AreEqual(k, UiKit.FrameK, 0.001f);
            Place(frame, "T182:top", 3.7f, 4.5f, out var ty, out var th);
            Debug.Log($"[T182ⓖ] k={k:0.0000} · 상단 바 {3.7:0.0}/{4.5:0.0} → {ty:0.00}/{th:0.00}(픽셀 {ty * k:0.00}/{th * k:0.00})");
            Assert.AreEqual(3.7f, ty * k, 0.02f, "위 띠의 줄은 위에서 잰 픽셀을 지킨다");
            Assert.AreEqual(4.5f, th * k, 0.02f, "위 띠의 줄은 높이 픽셀도 지킨다");

            Place(frame, "T182:tab", 92.6f, 7.4f, out var by, out var bh);
            Assert.AreEqual(100f - 92.6f, (100f - by) * k, 0.02f, "아래 띠의 줄은 아래에서 잰 픽셀을 지킨다");
            Assert.AreEqual(7.4f, bh * k, 0.02f, "탭 바 높이도 픽셀 그대로");

            Place(frame, "T182:ground", 30.0f, 21.0f, out var gy2, out var gh2);
            Assert.Greater(gh2 * k, 21.0f + 0.5f, "가운데 줄(지면 띠)은 늘어난 높이를 나눠 갖는다");

            // ⓒ 프레임 칸이 «아닌» 부모(팝업 상자처럼 제 사각형을 가진 칸)는 신축을 안 받는다
            var box = UiKit.Rect(frame, "T182:box");
            UiKit.Pct(box, 10f, 30f, 80f, 40f);
            Place(box, "T182:inbox", 10f, 20f, out var iy, out var ih);
            Assert.AreEqual(10f, iy, 0.02f, "상자 «안» 의 % 는 그 상자 기준이라 그대로여야 한다");
            Assert.AreEqual(20f, ih, 0.02f, "상자 안 높이도 그대로 — 상자가 이미 신축된 자리에 서 있다");

            // ⓓ 꽉 채운 겹을 지나 프레임에 닿으면 프레임 칸이다
            var layer = UiKit.Rect(frame, "T182:layer"); UiKit.Stretch(layer);
            Assert.IsTrue(UiKit.FrameSpace(layer), "꽉 채운 겹은 프레임과 같은 사각형");
            Place(layer, "T182:onlayer", 3.7f, 4.5f, out var ly, out var lh);
            Assert.AreEqual(3.7f, ly * k, 0.02f, "겹 위의 줄도 프레임 줄과 같이 움직인다");
            Assert.IsFalse(UiKit.FrameSpace(box), "제 사각형을 가진 칸은 프레임 칸이 아니다");

            UiKit.FrameKOverride = null;
            Assert.AreEqual(1f, UiKit.FrameK, 0.001f, "주입을 풀면 곧바로 오늘로 돌아온다");

            _log.AssertNoRed("세로 신축 배선(T182 3단계-2)");
            yield return Shutdown();
        }

        /// <summary>안전 영역을 그 비율의 사각형으로 주입한다(1단계 <c>AspectRatioGateTests.SetRatio</c> 와 같은 방법).</summary>
        void SetRatio(float w, float h)
        {
            float sw = Screen.width, sh = Screen.height, want = w / h;
            float rw = sw, rh = sw / want;
            if (rh > sh) { rh = sh; rw = sh * want; }
            SafeAreaRoot.Override = new Rect((sw - rw) * 0.5f, (sh - rh) * 0.5f, rw, rh);
            foreach (var s in Object.FindObjectsByType<SafeAreaRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) s.Apply(true);
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// T182 <b>3단계-3</b> — 프레임 비율이 실제로 화면을 따라간다(<see cref="FrameFit"/>).
        /// <list type="bullet">
        /// <item>납작한 화면(9:16 · 3:4)과 기준(9:19.5)에서는 <b>기준 비율 그대로</b> — 지금과 한 치도 다르지 않다(회귀 0).</item>
        /// <item>9:21 에서는 프레임이 <b>화면을 꽉 채우고</b> 신축 배수가 1.078 이 된다.</item>
        /// <item>상한(9:21) 위는 더 안 늘어난다 — 남는 위·아래는 2단계 띠 몫이다.</item>
        /// <item><b>마당은 «확대» 가 아니라 «더 보이는» 것</b> — 가로 배율(월드 반폭)이 비율이 바뀌어도 상수다.</item>
        /// </list>
        /// </summary>
        [UnityTest]
        public IEnumerator FrameFollowsTheScreenUpToTheCapAndTheWorldKeepsItsHorizontalScale()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);
            var frame = _app.Frame;

            float HalfW(RectTransform f) { var r = f.rect; return WorldCam.OrthoFor(f) * (r.width / r.height); }

            SetRatio(9f, 19.5f); yield return Frames(2);
            float refHalfW = HalfW(frame), refAspect = frame.rect.height / frame.rect.width;
            Debug.Log($"[T182ⓗ] 9:19.5 프레임 {frame.rect.width:0}×{frame.rect.height:0}(세로비 {refAspect:0.0000}) · k={UiKit.FrameK:0.0000} · 월드 반폭 {refHalfW:0.000}");
            Assert.AreEqual(Stretch.RefAspect, refAspect, 0.01f, "기준 비율에서는 프레임이 기준 그대로");
            Assert.AreEqual(1f, UiKit.FrameK, 0.005f, "기준에서는 신축 0");

            foreach (var (name, w, h) in new[] { ("9:16", 9f, 16f), ("3:4(태블릿)", 3f, 4f) })
            {
                SetRatio(w, h); yield return Frames(2);
                float a = frame.rect.height / frame.rect.width;
                Debug.Log($"[T182ⓗ] {name} 프레임 {frame.rect.width:0}×{frame.rect.height:0}(세로비 {a:0.0000}) · k={UiKit.FrameK:0.0000} · 월드 반폭 {HalfW(frame):0.000}");
                Assert.AreEqual(Stretch.RefAspect, a, 0.01f, name + ": 납작한 화면은 기준 비율 그대로(남는 폭은 2단계 띠 몫)");
                Assert.AreEqual(1f, UiKit.FrameK, 0.005f, name + ": 신축 0");
                Assert.AreEqual(refHalfW, HalfW(frame), 0.01f, name + ": 마당 가로 배율은 상수");
            }

            SetRatio(9f, 21f); yield return Frames(2);
            {
                var safe = (RectTransform)_app.SafeArea;
                float a = frame.rect.height / frame.rect.width;
                Debug.Log($"[T182ⓗ] 9:21 프레임 {frame.rect.width:0}×{frame.rect.height:0}(세로비 {a:0.0000}) · 안전영역 {safe.rect.width:0}×{safe.rect.height:0} · k={UiKit.FrameK:0.0000} · 월드 반폭 {HalfW(frame):0.000}");
                Assert.AreEqual(Stretch.MaxAspect, a, 0.01f, "9:21 은 상한과 같아 프레임이 그 비율로 선다");
                Assert.Greater(frame.rect.height / safe.rect.height, 0.98f, "9:21 에서는 프레임이 화면 높이를 꽉 채운다(위·아래 레터박스 0)");
                Assert.AreEqual(Stretch.K(9f, 21f), UiKit.FrameK, 0.005f, "신축 배수 1.078");
                Assert.AreEqual(refHalfW, HalfW(frame), 0.01f, "마당은 «확대» 가 아니라 «더 보이는» 것 — 가로 배율이 그대로다");
                Assert.AreEqual(WorldCam.LayoutH / 2f / WorldCam.PPU * Stretch.K(9f, 21f), WorldCam.OrthoFor(frame), 0.02f, "세로로는 그만큼 «더 보인다»(확대가 아니다)");
            }

            SetRatio(9f, 26f); yield return Frames(2);
            {
                float a = frame.rect.height / frame.rect.width;
                Debug.Log($"[T182ⓗ] 9:26 프레임 세로비 {a:0.0000} · k={UiKit.FrameK:0.0000}");
                Assert.AreEqual(Stretch.MaxAspect, a, 0.01f, "상한 위는 더 안 늘어난다 — 남는 위·아래는 2단계 띠가 채운다");
            }

            SafeAreaRoot.Override = null;
            foreach (var s in Object.FindObjectsByType<SafeAreaRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) s.Apply(true);
            yield return Frames(2);
            _log.AssertNoRed("프레임 비율이 화면을 따라간다(T182 3단계-3)");
            yield return Shutdown();
        }
    }
}
