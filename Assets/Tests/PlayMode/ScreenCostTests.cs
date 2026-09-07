using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// T129 회차 4 — <b>«로비가 왜 무거운가» 를 «무엇이 몇 개인가» 로 센다.</b>
    /// <para>
    /// 회차 3 이 배포 스모크에서 같은 런 안의 비를 재서 <b>전투가 로비보다 40% 빠르다</b>(`ratio=1.40`)를 얻었고,
    /// 그때 <c>tweens</c> 는 <b>두 화면 다 6</b> 이었다 — 즉 «도는 트윈 수» 로는 그 차이가 설명되지 않는다(결정 531).
    /// 남은 후보는 <b>그리는 양</b>(오버드로)과 <b>캔버스 리빌드</b>다. 이 자는 그중 앞엣것을 잰다.
    /// </para>
    /// <list type="bullet">
    /// <item><b>왜 PlayMode 인가</b> — 스모크는 <b>배포된</b> 빌드를 열므로 새 계측을 넣어도 다음 WebGL 배포까지 못 쓴다.
    /// 이 자는 지금 도는 CI 유니티 잡에서 <b>다음 런에 바로</b> 답을 준다.</item>
    /// <item><b>판정하지 않는다 — 센다.</b> 화면마다 옳은 조각 수는 «주인이 정한 구도» 가 정하는 것이라
    /// 수에 상한을 걸면 UI 작업이 그 자에 걸려 죽는다(T129 는 «연출을 줄여라» 가 아니라 «어디가 무거운지 알아라» 다).
    /// 그래서 <b>로그 한 표</b>를 남기고, 단언은 «잰 것이 실제로 있다» 는 최소한만 건다.</item>
    /// <item><b>fps 를 재지 않는다</b> — 에디터 PlayMode 는 폰도 브라우저도 아니라 그 수는 뜻이 없다(결정 524 의 잡음보다 나쁘다).
    /// 이 자가 답하는 물음은 «로비가 전투보다 <b>더 많이 그리는가</b>» 하나다.</item>
    /// </list>
    /// </summary>
    public class ScreenCostTests
    {
        App _app; PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>한 화면의 «그리는 양» — 켜져 있고 실제로 칠해지는 것만 센다(알파 0·꺼진 것은 비용이 아니다).</summary>
        struct Cost
        {
            public int graphics, images, raws, texts, masks, canvases;
            /// <summary>회차 6 — <b>화면 안으로 잘라 낸</b> 넓이의 합. 이것이 «실제로 칠하는 양»(오버드로)에 가깝다.</summary>
            public float area;
            /// <summary>회차 5 까지 쓰던 «사각형 넓이» 합(자르기 전) — 화면 밖으로 뻗은 몫까지 센다. 회차 사이 비교용으로 남긴다.</summary>
            public float rawArea;
            public System.Collections.Generic.List<KeyValuePair<string, float>> big;   // 회차 5 — 그 넓이를 «누가» 먹는가
            public override string ToString() =>
                "그림 " + graphics + "(Image " + images + " · RawImage " + raws + " · Text " + texts + ")"
                + " · 담개 " + masks + " · 캔버스 " + canvases
                + " · **보이는 넓이 " + area.ToString("0.00") + "화면**(사각형 넓이 " + rawArea.ToString("0.00") + ")";
        }

        /// <summary>조각을 이름으로 찾을 수 있게 «부모/부모/이름» 으로 — 이름만으로는 «Bg» 가 열 개라 못 가른다.</summary>
        static string PathOf(Transform t, int depth = 3)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (var x = t; x != null && parts.Count < depth; x = x.parent) parts.Add(x.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>부팅 캔버스(<c>BootCanvas</c>)가 아직 살아 있는가 — 있으면 그 화면 조각이 로비 표에 섞인다.</summary>
        static bool BootCanvasAlive()
        {
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (cv != null && cv.name == "BootCanvas") return true;
            return false;
        }

        static Cost Measure()
        {
            var c = new Cost();
            c.big = new System.Collections.Generic.List<KeyValuePair<string, float>>();
            float screen = Mathf.Max(1f, Screen.width * (float)Screen.height);
            foreach (var g in Object.FindObjectsByType<Graphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (g == null || !g.isActiveAndEnabled || g.canvasRenderer == null) continue;
                if (g.color.a <= 0.004f) continue;                       // 안 보이는 것은 칠하지 않는다
                c.graphics++;
                if (g is RawImage) c.raws++; else if (g is Image) c.images++; else if (g is Text) c.texts++;
                var rt = g.rectTransform; if (rt == null) continue;
                var w = new Vector3[4]; rt.GetWorldCorners(w);           // 실제 화면에서 차지하는 사각형(스케일·회전 반영)
                float raw = Mathf.Abs((w[2].x - w[0].x) * (w[2].y - w[0].y)) / screen;
                c.rawArea += raw;
                // ⚠ 회차 6 정정 — **화면 밖은 GPU 가 잘라 내므로 픽셀 값을 안 치른다.**
                //    회차 5 는 사각형 넓이를 그대로 더해 `TopFrame`·`BottomFrame` 을 «각각 5.2화면» 으로 셌는데,
                //    그 둘은 `TopBar.FrameOverscan = 4000f` 로 노치·레터박스를 덮으려고 **일부러** 사방 4000px 뻗은 띠다(T106 ⓑⓒ · 결정 254).
                //    뻗은 몫은 화면 밖이라 칠해지지 않는다 → 화면 사각형과 **겹치는 부분만** 센다. 그것이 오버드로다.
                float ix = Mathf.Max(0f, Mathf.Min(Mathf.Max(w[0].x, w[2].x), Screen.width) - Mathf.Max(Mathf.Min(w[0].x, w[2].x), 0f));
                float iy = Mathf.Max(0f, Mathf.Min(Mathf.Max(w[0].y, w[2].y), Screen.height) - Mathf.Max(Mathf.Min(w[0].y, w[2].y), 0f));
                float a = ix * iy / screen;
                c.area += a;
                if (a >= 0.2f) c.big.Add(new KeyValuePair<string, float>(PathOf(g.transform) + "[" + g.GetType().Name + "]", a));
            }
            foreach (var m in Object.FindObjectsByType<RectMask2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (m != null && m.isActiveAndEnabled) c.masks++;
            foreach (var m in Object.FindObjectsByType<Mask>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (m != null && m.isActiveAndEnabled) c.masks++;
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (cv != null && cv.isActiveAndEnabled) c.canvases++;
            return c;
        }

        /// <summary>넓이를 크게 먹는 조각을 큰 것부터 적는다 — 0.2화면(= 화면의 5분의 1) 이상만.</summary>
        static void Append(StringBuilder sb, string who, Cost c)
        {
            var top = c.big.OrderByDescending(kv => kv.Value).Take(8).ToList();
            sb.Append("  ").Append(who).Append(" 에서 **보이는** 넓이를 먹는 조각(0.2화면 이상 · 큰 것부터 · 전부 ")
              .Append(c.big.Count).Append("개, 합 ").Append(c.big.Sum(kv => kv.Value).ToString("0.00")).Append("화면)\n");
            if (top.Count == 0) sb.Append("    (없음 — 넓이가 여러 조각에 고르게 흩어져 있다)\n");
            foreach (var kv in top) sb.Append("    ").Append(kv.Value.ToString("0.00")).Append("화면  ").Append(kv.Key).Append('\n');
        }

        [UnityTest]
        public IEnumerator LobbyAndBattleDrawCountsAreCountedSideBySide()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);

            // 회차 7 — **부팅 캔버스가 사라질 때까지 기다린다.** 안 기다리면 로비 표에 남의 화면이 섞인다.
            // `LoadingScreen.MinSeconds`(0.3s · 깜빡임 방지 · T96-loading) 동안 `BootCanvas` 가 아직 살아 있고
            // 그 안 `ui.titleLoading/Background` 가 **화면 한 장을 통째로 덮는다** — 회차 6 의 표에서 그것이
            // «로비에서 가장 큰 조각(1.00화면)» 으로 잡혔다. 게임 결함이 아니라 **내가 너무 일찍 잰 것**이다
            // (회차 5 때는 우연히 이미 지워져 있어 안 잡혔다 = 회차마다 흔들리는 수였다).
            float bootT0 = Time.realtimeSinceStartup;
            while (BootCanvasAlive() && Time.realtimeSinceStartup - bootT0 < 10f) yield return null;
            yield return Frames(2);

            _app.ShowScreen("lobby"); yield return Frames(3); Canvas.ForceUpdateCanvases();
            var lobby = Measure();
            Assert.IsFalse(BootCanvasAlive(), "부팅 캔버스가 사라진 뒤에 재야 로비 표에 남의 화면이 안 섞인다(회차 7)");

            _app.StartBattle(1); yield return Frames(3); Canvas.ForceUpdateCanvases();
            Assert.AreEqual("battle", _app.Current.Name, "전투로 들어가야 두 화면을 맞댈 수 있다");
            var battle = Measure();

            // 표 한 줄 — 다음 워커가 CI 로그에서 `[T129]` 로 찾아 회차 사이에 맞댄다(스모크 perf 줄과 같은 쓰임).
            var sb = new StringBuilder();
            sb.Append("[T129] 화면별 그리는 양(에디터 PlayMode · fps 아님)\n");
            sb.Append("  로비 = ").Append(lobby).Append('\n');
            sb.Append("  전투 = ").Append(battle).Append('\n');
            sb.Append("  비(로비÷전투) = 그림 ").Append((battle.graphics > 0 ? lobby.graphics / (float)battle.graphics : 0f).ToString("0.00"))
              .Append(" · 보이는 넓이 ").Append((battle.area > 0.01f ? lobby.area / battle.area : 0f).ToString("0.00"))
              .Append(" (사각형 넓이 비 ").Append((battle.rawArea > 0.01f ? lobby.rawArea / battle.rawArea : 0f).ToString("0.00")).Append(")\n");
            // 회차 5 — 넓이는 몇몇 «큰 장» 이 거의 다 먹는다. **누가** 먹는지 이름으로 대야 다음 회차가 그 자리를 열 수 있다.
            Append(sb, "로비", lobby); Append(sb, "전투", battle);
            Debug.Log(sb.ToString());

            // ⚠ 단언은 «잰 것이 실제로 있다» 만 — 조각 수에 상한을 걸면 다음 UI 작업이 이 자에 걸려 죽는다.
            Assert.Greater(lobby.graphics, 0, "로비에서 그림을 세야 한다(0이면 이 자가 아무것도 안 재고 있다)");
            Assert.Greater(battle.graphics, 0, "전투에서 그림을 세야 한다");
            Assert.Greater(lobby.area, 0f, "칠하는 넓이가 0이면 rect 를 못 읽고 있는 것이다");

            _log.AssertNoRed("화면별 그리는 양");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(3);
        }
    }
}
