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
    /// T182 <b>2단계</b> — 프레임(9:19.5) «밖» 레터박스 띠를 게임 배경으로 채운다(주인 «갤럭시 탭까지도 해상도 받게끔»).
    /// 1단계 자(<see cref="AspectRatioGateTests"/>)가 «태블릿 3:4 에서 남는 띠 <b>38.4%</b>» 를 재 놓았고, 이 자는 그 자리가 <b>덮였는지</b>를 본다.
    /// <list type="bullet">
    /// <item>ⓐ 띠 넷(<c>Band:left/right/top/bottom</c>)이 있고 <b>프레임보다 뒤</b>(캔버스 형제 0 인 <c>Backdrop</c> 안)에 선다.</item>
    /// <item>ⓑ 넓은 화면(3:4·9:16)에서 좌우 띠가 <b>실제로 넓이를 갖고</b>, 띠 + 프레임이 안전 영역을 가로로 <b>다 덮는다</b>(검은 바닥 0).</item>
    /// <item>ⓒ <b>프레임 안은 한 픽셀도 안 덮는다</b> — 전투 마당은 <see cref="WorldCam"/> 이 캔버스 «뒤» 에 그리므로 덮으면 마당이 사라진다(이 자가 그 회귀를 잡는다).</item>
    /// <item>ⓓ 기준 비율(9:19.5)에서는 띠가 거의 0 이라 그림이 종전과 같다.</item>
    /// <item>ⓔ 빨간 줄 0(<see cref="PlayLog"/> · T11 규약).</item>
    /// </list>
    /// 비율은 1단계와 같은 방법(<see cref="SafeAreaRoot.Override"/> 주입)으로 흉내 낸다 — CI 에서 기기 해상도를 못 바꾸기 때문이다.
    /// </summary>
    public class FrameBackdropTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { SafeAreaRoot.Override = null; _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            SafeAreaRoot.Override = null;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
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

        static Rect WorldRect(RectTransform rt)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }
        RectTransform Band(string name) => UiKit.Find(_app.UiCanvas.transform, name) as RectTransform;

        [UnityTest]
        public IEnumerator LetterboxBandsCoverTheSidesAndNeverTheFrame()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            // ⓐ 조각 — 바탕 한 겹 + 띠 넷 · 프레임보다 뒤(형제 0)
            Assert.IsNotNull(_app.Backdrop, "바탕(FrameBackdrop)");
            var backdrop = (RectTransform)_app.Backdrop.transform;
            Assert.AreEqual(0, backdrop.GetSiblingIndex(), "바탕은 캔버스의 첫 형제 = 프레임보다 뒤");
            foreach (var n in new[] { FrameBackdrop.LeftName, FrameBackdrop.RightName, FrameBackdrop.TopName, FrameBackdrop.BottomName })
            {
                var b = Band(n); Assert.IsNotNull(b, "띠 " + n);
                var img = b.GetComponent<Image>(); Assert.IsNotNull(img, n + " 그림");
                Assert.IsFalse(img.raycastTarget, n + " 는 클릭을 안 먹는다(프레임 안 UI 가 받는다)");
                Assert.Greater(img.color.a, 0.9f, n + " 는 불투명해야 검은 바닥을 덮는다");
            }

            // ⓑⓒ 넓은 화면 둘 — 좌우 띠가 생기고, 띠 + 프레임이 가로를 다 덮고, 프레임 «안» 은 안 덮는다
            foreach (var (name, w, h) in new[] { ("3:4(태블릿)", 3f, 4f), ("9:16", 9f, 16f) })
            {
                SetRatio(w, h); yield return Frames(2); Canvas.ForceUpdateCanvases();
                var safe = WorldRect((RectTransform)_app.SafeArea);
                var frame = WorldRect(_app.Frame);
                var left = WorldRect(Band(FrameBackdrop.LeftName));
                var right = WorldRect(Band(FrameBackdrop.RightName));
                float sideBand = (left.width + right.width) / Mathf.Max(1f, safe.width) * 100f;
                Debug.Log($"[T182ⓑ] {name} 안전영역 {safe.width:0}×{safe.height:0} · 프레임 {frame.width:0}×{frame.height:0} · 좌우 띠 {sideBand:0.0}%");
                Assert.Greater(left.width, 1f, name + ": 왼쪽 띠가 실제로 넓이를 갖는다(넓은 화면이라 프레임 옆이 남는다)");
                Assert.Greater(right.width, 1f, name + ": 오른쪽 띠");
                // 띠 + 프레임 = 화면 가로 전부(검은 바닥이 안 남는다)
                Assert.LessOrEqual(left.xMin, safe.xMin + 1f, name + ": 왼쪽 띠가 화면 왼쪽 끝까지");
                Assert.GreaterOrEqual(right.xMax, safe.xMax - 1f, name + ": 오른쪽 띠가 화면 오른쪽 끝까지");
                Assert.LessOrEqual(Mathf.Abs(left.xMax - frame.xMin), 1f, name + ": 왼쪽 띠는 프레임 왼쪽 변에서 끝난다");
                Assert.LessOrEqual(Mathf.Abs(right.xMin - frame.xMax), 1f, name + ": 오른쪽 띠는 프레임 오른쪽 변에서 시작한다");
                // ⓒ 프레임 «안» 은 어느 띠도 안 덮는다 — 덮으면 전투 마당(WorldCam)이 사라진다
                foreach (var n in new[] { FrameBackdrop.LeftName, FrameBackdrop.RightName, FrameBackdrop.TopName, FrameBackdrop.BottomName })
                {
                    var b = WorldRect(Band(n));
                    float ox = Mathf.Min(b.xMax, frame.xMax) - Mathf.Max(b.xMin, frame.xMin);
                    float oy = Mathf.Min(b.yMax, frame.yMax) - Mathf.Max(b.yMin, frame.yMin);
                    Assert.IsTrue(ox <= 1f || oy <= 1f, name + ": " + n + " 가 프레임 안을 덮는다(전투 마당이 사라진다) — 겹침 " + ox.ToString("0.0") + "×" + oy.ToString("0.0"));
                }
            }

            // ⓓ 기준 비율에서는 띠가 거의 없다(그림이 종전과 같다)
            SetRatio(9f, 19.5f); yield return Frames(2); Canvas.ForceUpdateCanvases();
            {
                var safe = WorldRect((RectTransform)_app.SafeArea);
                var left = WorldRect(Band(FrameBackdrop.LeftName));
                var right = WorldRect(Band(FrameBackdrop.RightName));
                float sideBand = (left.width + right.width) / Mathf.Max(1f, safe.width) * 100f;
                Debug.Log($"[T182ⓑ] 9:19.5 좌우 띠 {sideBand:0.0}%");
                Assert.Less(sideBand, 2f, "기준 비율에서는 띠가 거의 0(레터박스가 없다)");
            }

            _log.AssertNoRed("프레임 밖 바탕(T182 2단계)");
            yield return Shutdown();
        }
    }
}
