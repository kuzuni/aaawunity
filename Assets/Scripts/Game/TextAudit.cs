using System.Collections.Generic;
using System.Text;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>글자 종류 표식(T63) — <see cref="UiKit"/> 가 Body 가 아닌 종류(Button·Aux·Title·Small)로 만든 Text 에 붙인다. 하한 게이트가 이걸 보고 종류별 하한을 적용한다.</summary>
    public sealed class TextKindTag : MonoBehaviour
    {
        public TextKind Kind = TextKind.Body;
    }

    /// <summary>
    /// 글자 크기 전수 점검(T63 4항) — 활성 <see cref="Text"/> 를 모아 «하한 미달 · bestFit 최소 미달 · 잘림/넘침» 을 판정한다.
    /// PlayMode 게이트(TextSizeGateTests)가 모든 화면을 열고 부른다. 하한·bestFit 은 즉시 실패, 잘림은 <see cref="ClipStrict"/> 가 켜질 때까지 보고만(화면별 하위 행이 하나씩 0 으로 만든다).
    /// </summary>
    public static class TextAudit
    {
        /// <summary>
        /// 잘림/넘침을 실패로 셀지 — 화면 12묶음 전수 점검(T63 3항)이 끝나 마지막 묶음(T63-toast)이 <b>켰다</b>(2026-09-06 15:5X).
        /// <para>
        /// 근거 = CI #126(<c>cb6a252</c> · https://github.com/kuzuni/aaawunity/actions/runs/34043194031) 의 게이트 로그
        /// «[TextSizeGate] 활성 Text 1298 · 하한 미달 0 · bestFit 미달 0 · <b>잘림/넘침 0</b>» — 표의 36 화면(01~29 · ev_* · res_*)이 전부 0 이다.
        /// 그 런에서 아직 🔄 이던 묶음(⑤ 대장간 08 · ⑥ 상점 09/10 · ⑨ 던전·아레나 20~26)의 화면도 이미 0 이었다.
        /// </para>
        /// 이제부터 글자가 칸을 넘치면 그 커밋이 빨개진다 — 실패 메시지에 «화면 / 경로 / 글자 / rect / pref» 가 그대로 찍히니 그 화면을 만진 워커가
        /// 칸을 키우거나(표 ±3%p) 문구를 줄인다(지시서 T63 2항 순서 ⓐ 줄바꿈 → ⓑ 칸 → ⓒ 문구). 되돌릴 일이 있으면 이 한 줄을 false 로.
        /// </summary>
        public const bool ClipStrict = true;

        /// <summary>
        /// 글꼴에 없는 글자(폭 0 으로 사라지는 글자 · T75)를 실패로 셀지 — 아직 <b>보고만</b> 한다.
        /// <para>
        /// T75 2단계(sess-1813-10924 · 워커 A)가 <see cref="UiKit"/> 의 글자 입구 다섯 곳(<c>Text</c>·<c>SetText</c>·<c>Button</c>·<c>ConvertTmp</c>·<c>Bar.Set</c>)에
        /// <see cref="TextGlyphs.Safe"/> 를 걸어 «UiKit 을 거치는 글자» 는 전부 걸러진다. 남는 것은 화면 코드가 <c>Text.text</c> 에 <b>직접</b> 넣는 자리(35곳)와
        /// 대체 글자가 없는 기호(<c>₩</c> · <c>💎</c> 같은 것 — 문구를 고쳐야 한다)뿐인데, 그 파일들이 지금 전부 다른 워커의 lock 안이다.
        /// </para>
        /// 그래서 «[GlyphGate]» 표로 화면·경로·글자를 CI 로그에 남겨 두고(<see cref="GlyphSummary"/>), 그 화면 묶음 워커가 하나씩 0 으로 만든 뒤 이 한 줄을 true 로 켠다
        /// (<see cref="ClipStrict"/> 와 같은 방식 · T75 5항).
        /// </summary>
        public const bool GlyphStrict = false;

        public sealed class Row
        {
            public string Screen, Path, Text;
            /// <summary>이 줄에서 글꼴이 못 그리는 글자(중복 없이) — 빈 문자열이면 없다(<see cref="TextGlyphs.Missing"/>).</summary>
            public string Missing;
            public TextKind Kind;
            public int FontSize, Min, BestFitMinSize, Used;
            public bool BestFit;
            public float RectW, RectH, PrefW, PrefH;
            public bool FloorBad, BestFitBad, Clipped;
            /// <summary>이 글자에 붙은 <see cref="Outline"/> 수(0 = 없다 · 2 이상 = 그림자가 겹쳐 두꺼워 보인다 · T63-outline).</summary>
            public int Outlines;
            /// <summary>아웃라인이 규칙(<see cref="UiKit.OutlineColor"/> · 두께 <see cref="UiKit.OutlineWidth"/>)과 어긋나는가 — 없거나 겹치거나 색·두께가 다르면 참.</summary>
            public bool OutlineBad;
            /// <summary>어긋난 까닭 한 마디(«없음» · «2개» · «색» · «두께 2.0≠3.0») — 없으면 빈 문자열.</summary>
            public string OutlineWhy = "";
            /// <summary>글자색 휘도(<see cref="UiKit.Luma"/>) 와 «어두운 글자» 판정(T111 ⓑ · <see cref="UiKit.TextLumaMin"/> 미만이면 참).</summary>
            public float Luma; public bool DarkBad;
            public override string ToString() =>
                $"[{Screen}] {Path} «{Short(Text)}» {Kind} size {FontSize}(min {Min}){(BestFit ? $" bestFit {BestFitMinSize}~ used {Used}" : "")} rect {RectW:0}×{RectH:0} pref {PrefW:0}×{PrefH:0}" +
                (FloorBad ? " ⛔하한" : "") + (BestFitBad ? " ⛔bestFit최소" : "") + (Clipped ? " ⚠잘림" : "") +
                (string.IsNullOrEmpty(Missing) ? "" : " ⚠없는글자 «" + Missing + "»") +
                (OutlineBad ? " ⛔아웃라인 " + OutlineWhy : "") + (DarkBad ? $" ⛔검정글씨 휘도 {Luma:0.00}" : "");
        }

        static string Short(string s) { if (string.IsNullOrEmpty(s)) return ""; s = s.Replace("\n", "⏎"); return s.Length > 18 ? s.Substring(0, 18) + "…" : s; }

        /// <summary>
        /// 아웃라인 판정(T63-outline · 주인 04:4X «모든 글자들 다 검정 아웃라인 · 어떤 건 있고 어떤 건 없고») —
        /// <see cref="Outline"/> 이 정확히 1개이고 색이 <see cref="UiKit.OutlineColor"/> 이며 두께가 <see cref="UiKit.OutlineWidth"/> 와 맞아야 통과.
        /// 두께는 «쓰이는 크기»(bestFit 이면 최대 크기)로 재는데, <see cref="UiKit.EnsureOutline"/> 가 붙일 때 쓰는 크기와 같은 식이다.
        /// </summary>
        static void FillOutline(Row row, Text t)
        {
            var ols = t.GetComponents<Outline>();
            row.Outlines = ols.Length;
            if (ols.Length == 0) { row.OutlineBad = true; row.OutlineWhy = "없음"; return; }
            if (ols.Length > 1) { row.OutlineBad = true; row.OutlineWhy = ols.Length + "개"; return; }
            var ol = ols[0];
            var c = ol.effectColor;
            if (Mathf.Abs(c.r - UiKit.OutlineColor.r) > 0.02f || Mathf.Abs(c.g - UiKit.OutlineColor.g) > 0.02f ||
                Mathf.Abs(c.b - UiKit.OutlineColor.b) > 0.02f || Mathf.Abs(c.a - UiKit.OutlineColor.a) > 0.02f)
            { row.OutlineBad = true; row.OutlineWhy = $"색 {c.r:0.00},{c.g:0.00},{c.b:0.00},{c.a:0.00}"; return; }
            float want = UiKit.OutlineWidth(t.resizeTextForBestFit ? Mathf.Max(t.resizeTextMaxSize, t.fontSize) : t.fontSize);
            float got = Mathf.Abs(ol.effectDistance.x);
            if (Mathf.Abs(got - want) > 0.26f) { row.OutlineBad = true; row.OutlineWhy = $"두께 {got:0.0}≠{want:0.0}"; }
        }

        /// <summary>
        /// «[TextOutlineGate]» 표 — 아웃라인이 없거나 어긋난 줄만 화면·경로·글자·까닭으로 찍는다(T63-outline).
        /// 0 줄이면 «없음 0» 한 줄. <see cref="OutlineStrict"/> 가 켜지면 PlayMode 게이트가 이 목록으로 단언한다.
        /// </summary>
        public static string OutlineSummary(List<Row> rows)
        {
            var sb = new StringBuilder();
            var bad = new List<Row>();
            foreach (var r in rows) if (r.OutlineBad) bad.Add(r);
            sb.Append("[TextOutlineGate] 아웃라인 어긋난 글자 ").Append(bad.Count).Append('/').Append(rows.Count);
            if (bad.Count == 0) { sb.Append(" — 없음 0 ✔"); return sb.ToString(); }
            foreach (var r in bad) sb.Append('\n').Append("  · [").Append(r.Screen).Append("] ").Append(r.Path)
                .Append(" «").Append(Short(r.Text)).Append("» ").Append(r.OutlineWhy);
            return sb.ToString();
        }

        /// <summary>
        /// 아웃라인 단언을 실제로 «틀리면 빨강» 으로 켤 것인가(T63-outline).
        /// 글자 입구 다섯 곳(<c>Text</c>·<c>SetText</c>·<c>Button</c>·<c>ConvertTmp</c>·<c>Bar.Set</c>)과 <see cref="UiKit.Adopt"/>(조각의 uGUI Text)가
        /// 전부 <see cref="UiKit.EnsureOutline"/> 를 거치므로 «없음 0» 이 나와야 정상이다.
        /// 첫 회차(`2bd5ff8a`)는 false 로 두고 표만 찍었다 — 워커 컨테이너에는 유니티가 없어 PlayMode 를 못 돌리기 때문이다.
        /// <b>CI [#173](https://github.com/kuzuni/aaawunity/actions/runs/34055981490) 로그에서 «어긋난 글자 0/1190 — 없음 0 ✔» 를 실측했으므로 켠다</b>
        /// (전 화면 활성 Text 1,190개가 전부 통과 · 유니티 잡도 success · <see cref="ClipStrict"/>·<see cref="GlyphStrict"/> 와 같은 방식 · 결정 245).
        /// 이제부터 UiKit 을 안 거치고 스스로 <c>Text</c> 를 붙이는 자리가 새로 생기면 그 커밋에서 바로 빨강이 된다.
        /// </summary>
        public const bool OutlineStrict = true;

        /// <summary>
        /// «[TextColorGate]» 표 — 검정·짙은 글자(<see cref="UiKit.TextLumaMin"/> 미만)만 화면·경로·글자·휘도로 찍는다(T111 ⓑ ·
        /// 주인 2026-09-07 07:5X «모든 글씨 중에 검정 글씨 → 흰 글씨로»). 0 줄이면 «없음 0 ✔» 한 줄.
        /// </summary>
        public static string ColorSummary(List<Row> rows)
        {
            var sb = new StringBuilder();
            var bad = new List<Row>();
            foreach (var r in rows) if (r.DarkBad) bad.Add(r);
            sb.Append("[TextColorGate] 검정·짙은 글자 ").Append(bad.Count).Append('/').Append(rows.Count);
            if (bad.Count == 0) { sb.Append(" — 없음 0 ✔"); return sb.ToString(); }
            foreach (var r in bad) sb.Append('\n').Append("  · [").Append(r.Screen).Append("] ").Append(r.Path)
                .Append(" «").Append(Short(r.Text)).Append("» 휘도 ").Append(r.Luma.ToString("0.00"));
            return sb.ToString();
        }

        /// <summary>
        /// 검정 글씨 단언을 «틀리면 빨강» 으로 켤 것인가(T111 ⓑ) — 글자 입구 다섯 곳과 <see cref="UiKit.Adopt"/> 가 전부
        /// <see cref="UiKit.EnsureOutline"/>(→ <see cref="UiKit.EnsureBright"/>) 를 거치므로 «없음 0» 이 나와야 정상이다.
        /// 첫 회차는 표만 찍었고(false), <b>CI [#194](https://github.com/kuzuni/aaawunity/actions/runs/34060006084) 로그에서 «검정·짙은 글자 0/1164 — 없음 0 ✔» 를 실측했으므로 켠다</b>
        /// (<see cref="OutlineStrict"/> 가 결정 245 에서 밟은 순서 그대로 · 결정 275).
        /// 이제부터 화면 코드가 <c>Text.color</c> 를 어둡게 덮어쓰면 그 커밋이 바로 빨강이 된다.
        /// </summary>
        public const bool ColorStrict = true;

        public static TextKind KindOf(Text t)
        {
            var tag = t != null ? t.GetComponent<TextKindTag>() : null;
            return tag != null ? tag.Kind : TextKind.Body;
        }

        /// <summary>종류를 기록한다 — Body 는 표식 없음(있으면 Body 로 되돌림).</summary>
        public static void Mark(Text t, TextKind kind)
        {
            if (t == null) return;
            if (kind == TextKind.Body)
            {
                var old = t.GetComponent<TextKindTag>();
                if (old != null) old.Kind = TextKind.Body;
                return;
            }
            UiKit.Ensure<TextKindTag>(t.gameObject).Kind = kind;
        }

        /// <summary>root 아래 활성 Text 전부(빈 글자 제외)를 판정해 돌려준다. 잘림 = 가로 Overflow 인데 선호 폭이 rect 보다 크거나, 선호 높이가 rect 높이보다 큰 것.</summary>
        public static List<Row> Collect(string screen, Transform root)
        {
            var rows = new List<Row>();
            if (root == null) return rows;
            foreach (var t in root.GetComponentsInChildren<Text>(false))
            {
                if (t == null || !t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                var kind = KindOf(t);
                int min = TextSize.Min(kind);
                var r = t.rectTransform.rect;
                var row = new Row
                {
                    Screen = screen, Path = PathOf(t.transform, root), Text = t.text, Kind = kind,
                    FontSize = t.fontSize, Min = min, BestFit = t.resizeTextForBestFit, BestFitMinSize = t.resizeTextMinSize,
                    RectW = r.width, RectH = r.height, PrefW = t.preferredWidth, PrefH = t.preferredHeight,
                };
                row.Missing = TextGlyphs.Missing(t.text);
                row.Used = t.resizeTextForBestFit ? BestFitSize(t) : t.fontSize;
                int effective = t.resizeTextForBestFit ? Mathf.Max(t.fontSize, t.resizeTextMaxSize) : t.fontSize;
                row.FloorBad = kind != TextKind.Small && effective < min;
                row.BestFitBad = kind != TextKind.Small && t.resizeTextForBestFit && t.resizeTextMinSize < TextSize.BestFitMin;
                bool wideBad = t.horizontalOverflow == HorizontalWrapMode.Overflow && row.PrefW > row.RectW + 1f;
                bool tallBad = row.PrefH > row.RectH + 1f;
                row.Clipped = wideBad || tallBad;
                FillOutline(row, t);
                row.Luma = UiKit.Luma(t.color);
                row.DarkBad = t.color.a > 0.2f && row.Luma < UiKit.TextLumaMin;   // T111 ⓑ — 알파가 거의 0 인 숨긴 글자는 세지 않는다
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>bestFit 이 실제로 고른 크기(글자 단위) — <c>cachedTextGenerator.fontSizeUsedForBestFit</c> 는 캔버스 scaleFactor 가 곱해진 값이라(CI #94 표의 «최소 크기(실제) 6~8») scaleFactor 1 로 다시 굴린다(T63-lobby).</summary>
        public static int BestFitSize(Text t)
        {
            var s = t.GetGenerationSettings(t.rectTransform.rect.size); s.scaleFactor = 1f;
            var g = new TextGenerator(); g.Populate(t.text, s);
            return Mathf.Max(g.fontSizeUsedForBestFit, 0);
        }

        static string PathOf(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (var c = t; c != null && c != root && parts.Count < 7; c = c.parent) parts.Add(c.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>화면별 «최소 글자 크기 · 하한 미달 · 잘림 수» 표(마크다운) — CI 로그에서 하위 행 워커가 읽는다.</summary>
        public static string Summary(List<Row> rows)
        {
            var byScreen = new Dictionary<string, List<Row>>();
            foreach (var r in rows) { if (!byScreen.TryGetValue(r.Screen, out var l)) byScreen[r.Screen] = l = new List<Row>(); l.Add(r); }
            var sb = new StringBuilder();
            sb.AppendLine("| 화면 | Text 수 | 최소 크기(실제) | 하한 미달 | bestFit 미달 | 잘림/넘침 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var kv in byScreen)
            {
                int minUsed = int.MaxValue, fb = 0, bb = 0, cl = 0;
                foreach (var r in kv.Value) { if (r.Used > 0 && r.Used < minUsed) minUsed = r.Used; if (r.FloorBad) fb++; if (r.BestFitBad) bb++; if (r.Clipped) cl++; }
                sb.AppendLine($"| {kv.Key} | {kv.Value.Count} | {(minUsed == int.MaxValue ? 0 : minUsed)} | {fb} | {bb} | {cl} |");
            }
            return sb.ToString();
        }

        /// <summary>화면별 «글꼴에 없는 글자» 표(마크다운 · T75) — 그 화면 묶음 워커가 CI 로그에서 읽고 문구를 고친다.</summary>
        public static string GlyphSummary(List<Row> rows)
        {
            var byScreen = new Dictionary<string, List<Row>>();
            foreach (var r in rows) { if (string.IsNullOrEmpty(r.Missing)) continue; if (!byScreen.TryGetValue(r.Screen, out var l)) byScreen[r.Screen] = l = new List<Row>(); l.Add(r); }
            var sb = new StringBuilder();
            sb.AppendLine("| 화면 | 없는 글자가 있는 줄 | 글자들 |");
            sb.AppendLine("|---|---|---|");
            foreach (var kv in byScreen)
            {
                var chars = new StringBuilder();
                foreach (var r in kv.Value) foreach (char c in r.Missing) { bool seen = false; for (int i = 0; i < chars.Length; i++) if (chars[i] == c) { seen = true; break; } if (!seen) chars.Append(c); }
                sb.AppendLine($"| {kv.Key} | {kv.Value.Count} | {chars} |");
            }
            return sb.ToString();
        }
    }
}
