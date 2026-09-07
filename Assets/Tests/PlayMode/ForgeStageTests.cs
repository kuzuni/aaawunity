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
    /// T191 — 대장간(08) 무대: <b>불이 «안내 문구 아래 · 바닥 띠 위»</b> 그 사이에서 탄다(레퍼런스 08 의 작은 불씨).
    /// <para>
    /// 여태 이 화면에서 잰 것은 «칸이 있는가 · 글자가 안 잘리는가 · 테두리가 있는가» 였고
    /// <b>«무대 조각끼리 겹치는가»</b> 는 아무도 안 봤다 — 그래서 불꽃 그림의 아래 절반이 바닥 띠 위에
    /// 떠 있는 채로 모든 게이트가 초록이었다(`screens` run 358 실측: 불꽃 y 237~317 · 바닥 윗변 y 297).
    /// </para>
    /// <para>
    /// <b>이웃은 둘이다</b> — 회차 1 은 «바닥» 만 보고 불을 위로 올렸다가 이번엔 «안내 문구» 와 겹쳤다
    /// (run 365 눈 확인: «고르세요» 글자 위에 불꽃 · 결정 488). 한쪽 이웃만 재는 자는 고친 자리를 옆으로 밀 뿐이다.
    /// </para>
    /// <para>
    /// <b>rect 로 잰다</b> — 그림(잉크)은 <c>preserveAspect</c> 로 칸 안에서 위아래 여백을 갖고 가운데 놓이므로,
    /// 칸이 바닥을 안 넘으면 그림도 안 넘는다. 반대로 «그림이 지금 몇 px 인가» 를 재면 조각을 바꿀 때마다 흔들린다.
    /// </para>
    /// </summary>
    public class ForgeStageTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

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

        /// <summary>rect 의 «위 끝 / 아래 끝» — 부모 안 정규화 좌표(0 = 위 · 1 = 아래)로 돌려준다(무대 조각들은 같은 부모라 바로 비교된다).</summary>
        static void Span(RectTransform rt, out float top, out float bottom)
        {
            // UiKit.Pct 는 앵커로 자리를 잡는다(위에서 아래로 재는 좌표) — anchorMax.y 가 «위», anchorMin.y 가 «아래».
            top = 1f - rt.anchorMax.y;
            bottom = 1f - rt.anchorMin.y;
        }

        [UnityTest]
        public IEnumerator FireBurnsInsideTheHearthAndNotOnTheFloor()
        {
            yield return Boot();
            _app.ShowScreen("forge"); yield return Frames(2); Canvas.ForceUpdateCanvases();

            var stage = UiKit.Find(_app.Current.Root, "Stage");
            Assert.IsNotNull(stage, "대장간 무대(Stage)");
            var fire = UiKit.Find(stage, "Fire") as RectTransform;
            var floor = UiKit.Find(stage, "Floor") as RectTransform;
            var hearth = UiKit.Find(stage, "Hearth") as RectTransform;
            var banner = UiKit.Find(_app.Current.Root, "Banner") as RectTransform;   // 안내 문구 상자 — 무대가 아니라 Root 의 자식이다
            Assert.IsNotNull(fire, "화덕 불(Fire)");
            Assert.IsNotNull(floor, "바닥 띠(Floor)");
            Assert.IsNotNull(hearth, "화덕 상자(Hearth)");
            Assert.IsNotNull(banner, "안내 문구 상자(Banner)");

            float fireTop, fireBottom, floorTop, floorBottom, hearthTop, hearthBottom;
            Span(fire, out fireTop, out fireBottom);
            Span(floor, out floorTop, out floorBottom);
            Span(hearth, out hearthTop, out hearthBottom);
            Debug.Log(string.Format("[T191] 불 {0:0.000}~{1:0.000} · 바닥 {2:0.000}~{3:0.000} · 화덕 {4:0.000}~{5:0.000} (무대 안 비율 · 0 = 위)",
                                    fireTop, fireBottom, floorTop, floorBottom, hearthTop, hearthBottom));

            const float eps = 1e-3f;
            Assert.LessOrEqual(fireBottom, floorTop + eps,
                "불이 바닥 띠 위로 걸쳤다 — 불 아래 끝 " + (fireBottom * 100f).ToString("0.0") + "% > 바닥 윗변 " + (floorTop * 100f).ToString("0.0") + "%"
                + " (T191 · 바닥이 불보다 먼저 깔리므로 불이 그 위에 그려져 «화덕 밖에서 타는» 그림이 된다)");
            // 화덕 «안»이라는 것도 같이 못 박는다 — 위로 도망가도 안 된다.
            Assert.GreaterOrEqual(fireTop, hearthTop - eps, "불이 화덕 윗변보다 위로 나갔다(T191)");
            Assert.LessOrEqual(fireBottom, hearthBottom + eps, "불이 화덕 아래 끝보다 아래로 나갔다(T191)");

            // ⚠ 이웃은 **둘**이다 — 회차 1 은 바닥만 보고 불을 올렸다가 안내 문구와 겹쳤다(결정 488).
            // 안내 상자는 무대가 아니라 Root 의 자식이라 좌표계가 다르다 → 프레임 % 로 환산해 견준다.
            float bannerBottomFrame = (1f - banner.anchorMin.y) * 100f;
            float fireTopFrame = fireTop * Layout.ForgeStage.H + Layout.ForgeStage.Y;
            Debug.Log(string.Format("[T191] 안내 상자 아래 끝 {0:0.00}% · 불 윗변 {1:0.00}% (프레임 %)", bannerBottomFrame, fireTopFrame));
            Assert.GreaterOrEqual(fireTopFrame, bannerBottomFrame - 0.1f,
                "불이 안내 문구 상자와 겹친다 — 불 윗변 " + fireTopFrame.ToString("0.00") + "% < 안내 상자 아래 끝 " + bannerBottomFrame.ToString("0.00") + "%"
                + " (T191 회차 2 · 레퍼런스 08 은 불이 안내 문구 «아래»에서 타는 작은 불씨다)");

            _log.AssertNoRed("T191 대장간 무대");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(3);
        }
    }
}
