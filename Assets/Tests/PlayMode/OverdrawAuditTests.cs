using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    /// T217 회차 1 — <b>«같은 자리를 몇 번 칠하는가» 를 부하에 안 흔들리는 자로 센다</b>(등재 3항 «재 보고 고른다» 의 재는 쪽).
    /// <para>
    /// 왜 fps 가 아닌가 — T129 계측 여덟 회차의 결론이다: 같은 빌드(`3a572294`)를 같은 컨테이너에서 두 번 쟀더니
    /// <c>fps 23.8 → 16.4</c> 였다(흩어짐 ±45%). 그 수로는 «겹을 줄여서 나아졌다» 를 절대 못 가른다.
    /// 반면 <b>겹 수·칠하는 넓이</b>는 화면을 세우는 방식이 정하는 값이라 기계 부하에 안 흔들린다
    /// (T129 마지막 줄이 «프레임 대신 드로우콜·배치 수처럼 부하에 안 흔들리는 자를 쓰라» 고 남긴 그 방향).
    /// </para>
    /// 이 회차는 <b>보고만</b> 한다 — «[OverdrawGate]» 표에 화면마다 ⓐ 프레임의 90% 이상을 덮는 «전면 겹» 수
    /// ⓑ 칠하는 넓이의 합 ÷ 프레임 넓이(= 대략의 오버드로) 를 찍고, <b>달아나는 것만</b> 실패로 센다.
    /// 값을 눈금으로 굳히는 것은 T217 이 «겹을 줄일지» 를 정한 뒤다(ClipStrict·PercentAudit 가 밟은 «먼저 표 → 0 이 되면 strict» 순서).
    /// </summary>
    public class OverdrawAuditTests
    {
        App _app; PlayLog _log;

        /// <summary>«전면 겹» 판정선 — 프레임 넓이의 이만큼 이상을 덮으면 화면 한 장을 통째로 칠하는 것으로 센다.</summary>
        const float FullScreenShare = 0.90f;
        /// <summary>이 알파 아래는 «칠하지 않는다» 로 본다(투명 히트 영역 · 꺼진 조각).</summary>
        const float MinAlpha = 0.02f;
        /// <summary>달아남 판정선 — 전면 겹이 이보다 많으면 어느 화면이든 실패(지금 최대는 로비 넷이다 · 여유 두 배).</summary>
        const int RunawayFullLayers = 8;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; }

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

        sealed class Row
        {
            public string Screen;
            /// <summary>프레임의 <see cref="FullScreenShare"/> 이상을 덮는 조각 수(= 화면 한 장을 통째로 칠하는 겹).</summary>
            public int FullLayers;
            /// <summary>칠하는 넓이의 합 ÷ 프레임 넓이 — 1.0 이면 «화면을 한 번 칠한다».</summary>
            public float Overdraw;
            /// <summary>센 조각 수(알파가 있는 활성 Graphic).</summary>
            public int Graphics;
            /// <summary>전면 겹의 이름들(무엇이 겹치는지 사람이 바로 읽게).</summary>
            public string FullNames = "";
        }
        readonly List<Row> _rows = new List<Row>();

        /// <summary>지금 화면의 겹을 센다 — 프레임(<see cref="App.Frame"/>) 안으로 잘라서 «덮는 넓이» 만 더한다.</summary>
        Row Measure(string screen)
        {
            var frame = _app.Frame;
            Canvas.ForceUpdateCanvases();
            var fr = frame.rect; float frameArea = Mathf.Max(1f, fr.width * fr.height);
            float sum = 0f; int full = 0, n = 0; var names = new List<string>();

            foreach (var g in _app.UiCanvas.GetComponentsInChildren<Graphic>(false))
            {
                if (g == null || !g.isActiveAndEnabled) continue;
                if (g.color.a < MinAlpha) continue;
                if (g.canvasRenderer != null && g.canvasRenderer.GetAlpha() < MinAlpha) continue;
                var b = RectTransformUtility.CalculateRelativeRectTransformBounds(frame, g.rectTransform);
                // 프레임 안으로 자른다 — 프레임 밖(레터박스 띠)까지 뻗은 조각의 넓이를 세면 «칠한다» 가 부풀려진다
                float x0 = Mathf.Max(fr.xMin, b.min.x), x1 = Mathf.Min(fr.xMax, b.max.x);
                float y0 = Mathf.Max(fr.yMin, b.min.y), y1 = Mathf.Min(fr.yMax, b.max.y);
                float w = x1 - x0, h = y1 - y0; if (w <= 0f || h <= 0f) continue;
                float share = (w * h) / frameArea;
                sum += share; n++;
                if (share >= FullScreenShare) { full++; if (names.Count < 8) names.Add(g.name); }
            }
            var row = new Row { Screen = screen, FullLayers = full, Overdraw = sum, Graphics = n, FullNames = string.Join(" · ", names) };
            _rows.Add(row);
            return row;
        }

        /// <summary>
        /// 표를 <c>ui-screens/overdraw.json</c> 로도 남긴다 — <see cref="PlayShot.Dirs"/> 가 주는 그 폴더라
        /// CI 의 `screens` 배포가 <c>layout.json</c>·PNG 와 <b>같이 올려 준다</b>.
        /// <para>
        /// 까닭: 유니티 테스트의 <c>Debug.Log</c> 는 결과 XML(아티팩트) 안에만 남고, 워커 환경에서 그 아티팩트는 프록시가 막는다
        /// (블롭 저장소 403). 그래서 «표를 찍었는데 아무도 못 읽는» 일이 생긴다 — `screens` 로 나가면 `curl` 한 번이면 읽힌다
        /// (T185 가 PNG 로 푼 그 문제를 숫자에도 적용한다).
        /// </para>
        /// </summary>
        void WriteJson(float lobbyOverBattle)
        {
            var sb = new StringBuilder();
            sb.Append("{\"_meta\":{\"task\":\"T217\",\"frame\":\"").Append(_app.Frame.rect.width.ToString("0")).Append('x')
              .Append(_app.Frame.rect.height.ToString("0")).Append("\",\"fullScreenShare\":").Append(FullScreenShare.ToString("0.00"))
              .Append(",\"lobbyOverBattle\":").Append(lobbyOverBattle.ToString("0.00")).Append("},\"screens\":{");
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(r.Screen).Append("\":{\"full\":").Append(r.FullLayers)
                  .Append(",\"overdraw\":").Append(r.Overdraw.ToString("0.000"))
                  .Append(",\"graphics\":").Append(r.Graphics)
                  .Append(",\"names\":\"").Append(r.FullNames.Replace("\"", "'")).Append("\"}");
            }
            sb.Append("}}");
            string json = sb.ToString();
            foreach (var dir in PlayShot.Dirs())
            {
                try { Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, "overdraw.json"), json); }
                catch (Exception e) { Debug.LogWarning("[OverdrawGate] overdraw.json 저장 실패(" + dir + "): " + e.Message); }
            }
        }

        [UnityTest]
        public IEnumerator EveryScreenReportsHowManyTimesItPaintsTheSamePlace()
        {
            yield return Boot();
            _app.Save.Gold = 11540; _app.Save.Gem = 543;

            foreach (var key in new[] { "lobby", "battle", "gear", "shop", "pet", "events" })
            {
                // 전투는 «보여 주기» 만으로는 안 선다 — 화면이 스스로 Start 를 받아야 무대·바가 생긴다(App.StartBattle 한 곳)
                if (key == "battle") _app.StartBattle(1); else _app.ShowScreen(key);
                yield return Frames(2);
                UiKit.CompleteAllTweens(); yield return Frames(1);
                Measure(key);
            }
            _app.GetScreen<BattleScreen>()?.Abort();
            _app.ShowScreen("lobby"); yield return Frames(2);

            var sb = new StringBuilder();
            sb.AppendLine($"[OverdrawGate] 화면 {_rows.Count}개(보고만 · T217 회차 1 · 프레임 {_app.Frame.rect.width:0}×{_app.Frame.rect.height:0})");
            sb.AppendLine("| 화면 | 전면 겹 | 오버드로(넓이 합÷프레임) | 조각 수 | 전면 겹 이름 |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var r in _rows)
                sb.AppendLine($"| {r.Screen} | {r.FullLayers} | {r.Overdraw:0.00} | {r.Graphics} | {r.FullNames} |");
            Debug.Log(sb.ToString());

            // 판정(이 회차) — 달아나는 것만 잡는다. 로비가 넷(Background·Pattern·GradientTop·GradientBottom · T129 실측)이라
            // 여유를 두 배로 잡았다: 이 줄은 «어느 회차가 전면 겹을 왕창 더했다» 만 잡고 지금 값에는 손대지 않는다.
            foreach (var r in _rows)
            {
                Assert.LessOrEqual(r.FullLayers, RunawayFullLayers,
                                   $"[{r.Screen}] 화면을 통째로 칠하는 겹이 너무 많다({r.FullNames})");
                Assert.Greater(r.Graphics, 0, $"[{r.Screen}] 조각이 하나는 있어야 한다(측정이 화면을 못 찾았다면 이 줄이 잡는다)");
            }
            // 로비 ↔ 전투 비 — T129 회차 7 이 1.87 로 잰 그 수를 여기서 «부하에 안 흔들리는 자» 로 다시 낸다(추세는 다음 회차가 본다).
            float lobby = 0f, battle = 0f;
            foreach (var r in _rows) { if (r.Screen == "lobby") lobby = r.Overdraw; else if (r.Screen == "battle") battle = r.Overdraw; }
            float ratio = battle > 0.01f ? lobby / battle : 0f;
            if (ratio > 0f) Debug.Log($"[OverdrawGate] 로비 ÷ 전투 = {ratio:0.00} (T129 회차 7 의 fps 기반 추정 1.87 과 견주는 값 · 이 자는 기계 부하에 안 흔들린다)");
            WriteJson(ratio);

            _log.AssertNoRed("화면 여섯 순회");
            // `using System;`(파일 쓰기) 때문에 «Object» 가 둘이 된다 — 유니티 쪽으로 못 박는다
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            yield return Frames(2);
        }
    }
}
