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

        public sealed class Row
        {
            public string Screen, Path, Text;
            public TextKind Kind;
            public int FontSize, Min, BestFitMinSize, Used;
            public bool BestFit;
            public float RectW, RectH, PrefW, PrefH;
            public bool FloorBad, BestFitBad, Clipped;
            public override string ToString() =>
                $"[{Screen}] {Path} «{Short(Text)}» {Kind} size {FontSize}(min {Min}){(BestFit ? $" bestFit {BestFitMinSize}~ used {Used}" : "")} rect {RectW:0}×{RectH:0} pref {PrefW:0}×{PrefH:0}" +
                (FloorBad ? " ⛔하한" : "") + (BestFitBad ? " ⛔bestFit최소" : "") + (Clipped ? " ⚠잘림" : "");
        }

        static string Short(string s) { if (string.IsNullOrEmpty(s)) return ""; s = s.Replace("\n", "⏎"); return s.Length > 18 ? s.Substring(0, 18) + "…" : s; }

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
                row.Used = t.resizeTextForBestFit ? BestFitSize(t) : t.fontSize;
                int effective = t.resizeTextForBestFit ? Mathf.Max(t.fontSize, t.resizeTextMaxSize) : t.fontSize;
                row.FloorBad = kind != TextKind.Small && effective < min;
                row.BestFitBad = kind != TextKind.Small && t.resizeTextForBestFit && t.resizeTextMinSize < TextSize.BestFitMin;
                bool wideBad = t.horizontalOverflow == HorizontalWrapMode.Overflow && row.PrefW > row.RectW + 1f;
                bool tallBad = row.PrefH > row.RectH + 1f;
                row.Clipped = wideBad || tallBad;
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
    }
}
