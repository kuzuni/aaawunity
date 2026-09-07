using System.Collections;
using System.Collections.Generic;
using System.Text;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T182 <b>1단계 — 자와 표 먼저</b>(주인 2026-09-07 11:5X «UI 9:19 말고 9:16 이나 그런 것도 되게 · 갤럭시 탭까지 · 3:4 까지» · 세로만).
    /// <para>
    /// 고치기 전에 <b>«지금 코드가 비율마다 무엇을 잘못하는가» 를 재는 자</b>를 먼저 둔다. 비율은 <see cref="SafeAreaRoot.Override"/> 로 흉내 낸다 —
    /// 안전 영역을 그 비율의 사각형으로 주면 <c>UiKit.CreateFrame</c> 의 <c>AspectRatioFitter</c> 가 그 안에 프레임을 넣으므로,
    /// 기기 해상도를 못 바꾸는 CI 에서도 «그 비율의 화면» 과 같은 배치가 나온다(화면 코드·프레임 상수는 한 줄도 안 건드린다).
    /// </para>
    /// 이 회차는 <b>보고만</b> 한다 — «[AspectGate]» 표에 비율 × 화면마다 ⓐ 프레임이 안전 영역의 몇 %를 쓰는지(남는 띠) ⓑ 글자 잘림 수를 찍고,
    /// <b>빨간 줄 0</b> 만 실패로 센다(<c>TextAudit.ClipStrict</c>·<c>PercentAudit.Strict</c> 가 밟은 순서 · ROUTINE §2 T182 3항).
    /// 2·3단계(태블릿 폭 상한 · 세로 신축)가 들어오면 이 표의 «남는 띠» 가 줄어드는 것으로 판정한다.
    /// </summary>
    public class AspectRatioGateTests
    {
        App _app; PlayLog _log;

        /// <summary>재는 비율(가로:세로) — 주인이 말한 범위: 9:16(넓은 폰) · 9:18 · 9:19.5(레퍼런스·지금 기준) · 9:21(긴 폰) · 3:4(갤럭시 탭).</summary>
        static readonly (string Name, float W, float H)[] Ratios =
        {
            ("9:16", 9f, 16f), ("9:18", 9f, 18f), ("9:19.5", 9f, 19.5f), ("9:21", 9f, 21f), ("3:4(태블릿)", 3f, 4f),
        };

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { SafeAreaRoot.Override = null; _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>안전 영역을 그 비율의 사각형으로 주입한다(화면 가운데 · 화면 안에 들어가는 가장 큰 크기).</summary>
        void SetRatio(float w, float h)
        {
            float sw = Screen.width, sh = Screen.height;
            float want = w / h;
            float rw = sw, rh = sw / want;
            if (rh > sh) { rh = sh; rw = sh * want; }
            SafeAreaRoot.Override = new Rect((sw - rw) * 0.5f, (sh - rh) * 0.5f, rw, rh);
            foreach (var s in Object.FindObjectsByType<SafeAreaRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) s.Apply(true);
            Canvas.ForceUpdateCanvases();
        }

        sealed class Row
        {
            public string Ratio, Screen;
            public float SafeW, SafeH, FrameW, FrameH;
            public int Texts, Clipped;
            /// <summary>프레임이 안전 영역에서 «안 쓰는» 넓이 비율(%) — 레터박스 띠. 0 에 가까울수록 화면을 꽉 쓴다.</summary>
            public float WastePct => SafeW * SafeH <= 0f ? 0f : 100f * (1f - FrameW * FrameH / (SafeW * SafeH));
        }
        readonly List<Row> _rows = new List<Row>();

        IEnumerator Measure(string ratio, string screen)
        {
            UiKit.CompleteAllTweens();
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();
            var safe = (RectTransform)_app.SafeArea; var frame = _app.Frame;
            var rows = TextAudit.Collect(screen, _app.UiCanvas.transform);
            int clipped = 0; foreach (var r in rows) if (r.Clipped) clipped++;
            _rows.Add(new Row
            {
                Ratio = ratio, Screen = screen,
                SafeW = safe.rect.width, SafeH = safe.rect.height,
                FrameW = frame.rect.width, FrameH = frame.rect.height,
                Texts = rows.Count, Clipped = clipped,
            });
            yield return Frames(1);
        }

        [UnityTest]
        public IEnumerator EveryAspectRatioKeepsTheScreensUsableAndTheTableShowsWhatIsWasted()
        {
            yield return Boot();
            _app.Save.Gold = 11540; _app.Save.Gem = 543;

            foreach (var (name, w, h) in Ratios)
            {
                SetRatio(w, h);
                yield return Frames(2);

                _app.ShowScreen("lobby"); yield return Frames(2); yield return Measure(name, "01_lobby");
                _app.ShowScreen("gear"); yield return Frames(2); yield return Measure(name, "06_gear");
                _app.ShowScreen("shop"); yield return Frames(2); yield return Measure(name, "09_shop");
                _app.ShowScreen("lobby"); yield return Frames(1);
            }
            SafeAreaRoot.Override = null;
            foreach (var s in Object.FindObjectsByType<SafeAreaRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None)) s.Apply(true);
            yield return Frames(2);

            // 표 — 다음 단계(태블릿 폭 상한 · 세로 신축)가 이 «남는 띠» 를 줄이는 것으로 판정한다
            var sb = new StringBuilder();
            sb.AppendLine($"[AspectGate] 비율 {Ratios.Length} × 화면 3 = {_rows.Count}칸(보고만 · T182 1단계 · 화면 {Screen.width}×{Screen.height})");
            sb.AppendLine("| 비율 | 화면 | 안전영역 px | 프레임 px | 남는 띠 % | 활성 Text | 잘림 |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var r in _rows)
                sb.AppendLine($"| {r.Ratio} | {r.Screen} | {r.SafeW:0}×{r.SafeH:0} | {r.FrameW:0}×{r.FrameH:0} | {r.WastePct:0.0} | {r.Texts} | {r.Clipped} |");
            Debug.Log(sb.ToString());

            // 판정(이 회차) — 어느 비율에서도 ⓐ 프레임이 생기고 ⓑ 안전 영역을 넘지 않고 ⓒ 글자가 안 잘리고 ⓓ 빨간 줄 0.
            foreach (var r in _rows)
            {
                Assert.Greater(r.FrameW, 1f, $"[{r.Ratio}] {r.Screen} 프레임 폭");
                Assert.Greater(r.FrameH, 1f, $"[{r.Ratio}] {r.Screen} 프레임 높이");
                Assert.LessOrEqual(r.FrameW, r.SafeW + 1f, $"[{r.Ratio}] {r.Screen} 프레임이 안전 영역보다 넓다");
                Assert.LessOrEqual(r.FrameH, r.SafeH + 1f, $"[{r.Ratio}] {r.Screen} 프레임이 안전 영역보다 높다");
                // 글자 잘림은 «기준 비율(9:19.5)» 에서만 실패로 센다 — 다른 비율에서 잘리는 것이 바로 2·3단계가 고칠 결함이라
                // 지금 실패로 세면 «아직 안 고친 것» 때문에 main 이 빨개진다(ClipStrict·PercentAudit 가 밟은 «먼저 표 → 0 이 되면 strict» 순서).
                if (r.Ratio == "9:19.5") Assert.AreEqual(0, r.Clipped, $"[{r.Ratio}] {r.Screen} 글자 잘림(T63 자로 잰다)");
            }
            int otherClipped = 0; foreach (var r in _rows) if (r.Ratio != "9:19.5") otherClipped += r.Clipped;
            Debug.Log($"[AspectGate] 기준 비율 밖에서 잘린 글자 {otherClipped}개 — 0 이 되면 위 단언을 모든 비율로 넓힌다(T182 2·3단계).");
            _log.AssertNoRed("여러 비율(9:16 ~ 3:4) 순회");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(2);
        }
    }
}
