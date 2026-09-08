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
        /// <summary>
        /// <b>고친 자</b>(<see cref="Row.Paint"/>)가 쓰는 알파 판정선 — <see cref="MinAlpha"/> 0.02 가
        /// <b>배경 무늬 두 알파 사이에 놓여 있었다</b>(T223 회차 3 · 결정 617):
        /// 로비만 <c>UiKit.PatternAlphaLobby</c> 7/255 = 0.0275 라 «전면 겹» 으로 세어지고,
        /// <b>나머지 전 화면</b>은 <c>UiKit.PatternAlpha</c> 3/255 = 0.0118 이라 <b>통째로 안 세어졌다</b>.
        /// 같은 크기의 같은 사각형인데 화면마다 세고 안 센 것이다 — 그 상태로는 화면끼리 못 견준다.
        /// 그래서 이 판정선은 둘 <b>아래</b>(1/255)로 둔다: 알파 0(순전한 히트 영역)만 빠지고 그리는 것은 다 센다.
        /// </summary>
        const float PaintAlpha = 1f / 255f;
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
            /// <summary>
            /// 가장 넓게 칠하는 조각 다섯(«부모/이름» · 프레임 대비 넓이) — T223 1항이 요구한 칸이다.
            /// 화면 «합계» 만으로는 «어디가 겹치는가» 를 못 가른다(상점 7.38 이 큰 겹 하나 때문인지 카드 백 장 때문인지 모른다).
            /// </summary>
            public List<KeyValuePair<string, float>> Top = new List<KeyValuePair<string, float>>();
            /// <summary>
            /// <b>고친 자</b> — <see cref="Overdraw"/> 와 같은 뜻이되 «자가 세지만 GPU 는 안 칠하는» 셋을 뺀 값(T223 회차 3·4 · 결정 617).
            /// <list type="bullet">
            /// <item>9-slice 링(<c>fillCenter = false</c>)은 가운데를 안 그린다 → rect 가 아니라 <b>테 띠</b>로 센다.</item>
            /// <item><see cref="RectMask2D"/> 안 조각은 그 사각형 밖이 잘린다 → <b>교집합</b>으로 센다(빛살·글로우·스크롤 내용).</item>
            /// <item>알파 판정선을 <see cref="PaintAlpha"/> 로 내려 <b>배경 무늬를 전 화면에서 같이</b> 센다.</item>
            /// </list>
            /// <see cref="Overdraw"/> 를 <b>덮어쓰지 않고 칸을 하나 더 두는</b> 까닭: 그 수는 세 런 연속 같은 값으로
            /// 이어져 온 계열이라(로비 4.943 → 4.948 → 4.948) 정의를 바꾸면 지난 실측과 못 잇는다.
            /// </summary>
            public float Paint;
            /// <summary>
            /// <b>고침 셋이 각각 얼마를 움직였나</b>(회차 5 · 결정 628) — <c>Overdraw + DAlpha + DMask + DRing = Paint</c>.
            /// <para>
            /// 왜 나눠서 내나 — 회차 4 는 셋의 합만 냈고, 셈으로 예고한 값(상점 6.7~7.4)이 실측(<b>7.806</b>)과 어긋났는데
            /// <b>어느 몫이 틀렸는지 가릴 수가 없었다</b>. 합계만 있는 자는 «움직였다» 는 알려 줘도 «왜» 는 못 알려 준다.
            /// </para>
            /// <see cref="DAlpha"/> 는 «판정선을 내려 새로 센 것»(≥0 · 대부분 배경 무늬 한 장),
            /// <see cref="DMask"/> 는 «마스크 밖이라 잘린 것»(≤0), <see cref="DRing"/> 은 «링의 가운데»(≤0).
            /// </summary>
            public float DAlpha, DMask, DRing;
        }
        readonly List<Row> _rows = new List<Row>();

        /// <summary>`overdraw.json` 에 남기는 «가장 넓은 조각» 수(T223 1항).</summary>
        const int TopCount = 5;

        /// <summary>
        /// 9-slice «링»(<c>fillCenter = false</c>)이 <b>제 rect 중 실제로 칠하는 몫</b> — 가운데를 안 그리므로 테 띠뿐이다.
        /// <para>
        /// 테 두께는 uGUI 가 <c>Image.GenerateSlicedSprite</c> 에서 쓰는 식 그대로다:
        /// <c>sprite.border ÷ (Image.pixelsPerUnit × Image.pixelsPerUnitMultiplier)</c>.
        /// 링이 아니거나 9-slice 테가 없는 스프라이트면 1(= rect 전체).
        /// </para>
        /// <b>왜 이것이 필요한가</b> — `UiKit.Bordered` 가 카드마다 이 링을 한 장씩 얹는데(`UiKit.cs:452`),
        /// 자는 rect 를 재서 «카드 한 장을 통째로 칠한다» 로 셌다. 상점만 여섯 장 = <b>0.62화면이 허수</b>였다(T223 회차 2).
        /// </summary>
        static float RingFactor(Graphic g, float w, float h)
        {
            var img = g as Image;
            if (img == null || img.fillCenter || img.type != Image.Type.Sliced) return 1f;
            var sp = img.sprite; if (sp == null) return 1f;
            var bd = sp.border; if (bd.sqrMagnitude <= 0f) return 1f;
            float ppu = Mathf.Max(0.0001f, img.pixelsPerUnit * img.pixelsPerUnitMultiplier);
            float innerW = Mathf.Max(0f, w - (bd.x + bd.z) / ppu);
            float innerH = Mathf.Max(0f, h - (bd.y + bd.w) / ppu);
            float a = w * h;
            return a <= 0f ? 0f : Mathf.Clamp01((a - innerW * innerH) / a);
        }

        /// <summary>
        /// 조각을 <b>조상의 <see cref="RectMask2D"/> 사각형들</b>로 마저 자른다(프레임 자르기 «뒤»).
        /// <para>
        /// 왜 — `UiKit.LightBehind` 는 «아이콘 긴 변 × 1.9» 짜리 정사각 두 장(빛+글로우)을 깔고 <b>칸 크기 마스크로 자른다</b>.
        /// 자는 그 마스크를 안 보고 rect 를 재서 상점에서만 <b>1.28화면</b>을 셌다(대형 상자는 한 변 694px 로 칸 1015×608 을 넘는다).
        /// 스크롤 내용(`ScrollRect` 뷰포트도 <c>RectMask2D</c> 다)도 같은 까닭으로 부풀려져 있었다. T223 회차 3.
        /// </para>
        /// <c>padding</c>·<c>softness</c> 는 이 저장소에서 전부 0 이라 안 본다(쓰기 시작하면 여기도 같이 봐야 한다).
        /// </summary>
        static void ClipByMasks(RectTransform frame, Transform t, ref float x0, ref float y0, ref float x1, ref float y1)
        {
            for (var p = t; p != null && p != frame.parent; p = p.parent)
            {
                var m = p.GetComponent<RectMask2D>();
                if (m == null || !m.isActiveAndEnabled) continue;
                var mb = RectTransformUtility.CalculateRelativeRectTransformBounds(frame, (RectTransform)p);
                x0 = Mathf.Max(x0, mb.min.x); y0 = Mathf.Max(y0, mb.min.y);
                x1 = Mathf.Min(x1, mb.max.x); y1 = Mathf.Min(y1, mb.max.y);
            }
        }

        /// <summary>지금 화면의 겹을 센다 — 프레임(<see cref="App.Frame"/>) 안으로 잘라서 «덮는 넓이» 만 더한다.</summary>
        Row Measure(string screen)
        {
            var frame = _app.Frame;
            Canvas.ForceUpdateCanvases();
            var fr = frame.rect; float frameArea = Mathf.Max(1f, fr.width * fr.height);
            float sum = 0f, paint = 0f, rawPaint = 0f, maskPaint = 0f; int full = 0, n = 0; var names = new List<string>();
            var all = new List<KeyValuePair<string, float>>();

            foreach (var g in _app.UiCanvas.GetComponentsInChildren<Graphic>(false))
            {
                if (g == null || !g.isActiveAndEnabled) continue;
                // 판정선이 둘이다 — 옛 계열(MinAlpha)은 그대로 두고, 고친 자(PaintAlpha)는 무늬까지 같이 센다
                float a = g.color.a;
                if (g.canvasRenderer != null) a = Mathf.Min(a, g.canvasRenderer.GetAlpha());
                if (a < PaintAlpha) continue;
                var b = RectTransformUtility.CalculateRelativeRectTransformBounds(frame, g.rectTransform);
                // 프레임 안으로 자른다 — 프레임 밖(레터박스 띠)까지 뻗은 조각의 넓이를 세면 «칠한다» 가 부풀려진다
                float x0 = Mathf.Max(fr.xMin, b.min.x), x1 = Mathf.Min(fr.xMax, b.max.x);
                float y0 = Mathf.Max(fr.yMin, b.min.y), y1 = Mathf.Min(fr.yMax, b.max.y);
                float w = x1 - x0, h = y1 - y0; if (w <= 0f || h <= 0f) continue;
                float share = (w * h) / frameArea;

                // ── 고친 자: 마스크 교집합 + 링은 테 띠만 ──
                // 세 몫을 «따로» 쌓는다 — 합계만 내면 «값이 왜 움직였는가» 를 못 가른다.
                // 회차 4 가 셈으로 예고한 수(상점 6.7~7.4)와 실측(7.806)이 어긋난 뒤 붙인 칸이다(회차 5 · 결정 628).
                rawPaint += share;
                float mx0 = x0, my0 = y0, mx1 = x1, my1 = y1;
                ClipByMasks(frame, g.transform.parent, ref mx0, ref my0, ref mx1, ref my1);
                float mw = mx1 - mx0, mh = my1 - my0;
                if (mw > 0f && mh > 0f)
                {
                    float masked = (mw * mh) / frameArea;
                    maskPaint += masked;
                    var lr = g.rectTransform.rect;
                    paint += masked * RingFactor(g, lr.width, lr.height);
                }

                // ── 옛 계열: 정의를 한 글자도 안 바꾼다(지난 세 런과 이어서 읽힌다) ──
                if (g.color.a < MinAlpha) continue;
                if (g.canvasRenderer != null && g.canvasRenderer.GetAlpha() < MinAlpha) continue;
                sum += share; n++;
                if (share >= FullScreenShare) { full++; if (names.Count < 8) names.Add(g.name); }
                // «부모/이름» 으로 적는다 — 같은 이름 조각(«Bg»·«Icon»)이 수십 개라 이름만으로는 어느 자리인지 못 찾는다
                var par = g.transform.parent;
                all.Add(new KeyValuePair<string, float>((par != null ? par.name + "/" : "") + g.name, share));
            }
            all.Sort((x, y) => y.Value.CompareTo(x.Value));
            var row = new Row { Screen = screen, FullLayers = full, Overdraw = sum, Paint = paint, Graphics = n, FullNames = string.Join(" · ", names),
                                DAlpha = rawPaint - sum, DMask = maskPaint - rawPaint, DRing = paint - maskPaint };
            for (int i = 0; i < all.Count && i < TopCount; i++) row.Top.Add(all[i]);
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
                  // T223 회차 4 — «자가 세지만 GPU 는 안 칠하는» 셋을 뺀 값(링 · 마스크 · 무늬 알파). 옛 칸은 그대로 둔다.
                  .Append(",\"paint\":").Append(r.Paint.ToString("0.000"))
                  // T223 회차 5 — 고침 셋이 각각 얼마를 움직였나(overdraw + dAlpha + dMask + dRing = paint)
                  .Append(",\"dAlpha\":").Append(r.DAlpha.ToString("0.000"))
                  .Append(",\"dMask\":").Append(r.DMask.ToString("0.000"))
                  .Append(",\"dRing\":").Append(r.DRing.ToString("0.000"))
                  .Append(",\"graphics\":").Append(r.Graphics)
                  .Append(",\"names\":\"").Append(r.FullNames.Replace("\"", "'")).Append('"');
                // T223 1항 — «어디가 겹치는가» 는 합계가 아니라 이 칸이 답한다
                sb.Append(",\"top\":[");
                for (int k = 0; k < r.Top.Count; k++)
                {
                    if (k > 0) sb.Append(',');
                    sb.Append("{\"n\":\"").Append(r.Top[k].Key.Replace("\"", "'")).Append("\",\"s\":")
                      .Append(r.Top[k].Value.ToString("0.000")).Append('}');
                }
                sb.Append("]}");
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
            sb.AppendLine("| 화면 | 전면 겹 | 오버드로(옛 계열) | **칠하는 넓이(고친 자)** | 차 | 무늬(+) | 마스크(−) | 링(−) | 조각 수 | 전면 겹 이름 | 가장 넓은 조각 다섯 |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var r in _rows)
            {
                var top = new StringBuilder();
                for (int k = 0; k < r.Top.Count; k++) { if (k > 0) top.Append(" · "); top.Append(r.Top[k].Key).Append(' ').Append(r.Top[k].Value.ToString("0.00")); }
                sb.AppendLine($"| {r.Screen} | {r.FullLayers} | {r.Overdraw:0.00} | **{r.Paint:0.00}** | {r.Paint - r.Overdraw:+0.00;-0.00;0.00} | {r.DAlpha:+0.00;-0.00;0.00} | {r.DMask:+0.00;-0.00;0.00} | {r.DRing:+0.00;-0.00;0.00} | {r.Graphics} | {r.FullNames} | {top} |");
            }
            sb.AppendLine("· 「칠하는 넓이」 = 오버드로 + 무늬(+) + 마스크(−) + 링(−) — 셋이 각각 얼마를 움직였는지 같이 찍는다(T223 회차 4·5 · 결정 617·630)");
            Debug.Log(sb.ToString());

            // 판정(이 회차) — 달아나는 것만 잡는다. 로비가 넷(Background·Pattern·GradientTop·GradientBottom · T129 실측)이라
            // 여유를 두 배로 잡았다: 이 줄은 «어느 회차가 전면 겹을 왕창 더했다» 만 잡고 지금 값에는 손대지 않는다.
            foreach (var r in _rows)
            {
                Assert.LessOrEqual(r.FullLayers, RunawayFullLayers,
                                   $"[{r.Screen}] 화면을 통째로 칠하는 겹이 너무 많다({r.FullNames})");
                Assert.Greater(r.Graphics, 0, $"[{r.Screen}] 조각이 하나는 있어야 한다(측정이 화면을 못 찾았다면 이 줄이 잡는다)");
                // 고친 자가 «켜져 있는가» 만 본다 — 값에는 눈금을 안 박는다(이 자는 여전히 보고만 한다).
                // 0 이면 링·마스크 자르기가 전부를 지웠다는 뜻이라 그것이 결함이다.
                Assert.Greater(r.Paint, 0f, $"[{r.Screen}] 고친 자가 0 을 냈다 — 링·마스크 자르기가 화면을 통째로 지웠다");
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
