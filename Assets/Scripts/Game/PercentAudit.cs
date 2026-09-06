using System.Collections.Generic;
using System.Text;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 퍼센트 표기 전수 점검(T90 3항 · 주인 2026-09-07 «원래 퍼센트로 써져야 하는 부분들 다 퍼센트로 잘 써지게») —
    /// 화면의 활성 <see cref="Text"/> 를 모아 «비율 스탯 이름 + 부호 숫자» 뒤에 <c>%</c> 가 빠진 줄을 찾는다(<see cref="StatText.Missing"/>).
    /// <para>
    /// 데이터에서 오는 문구(특전 설명 · 장비 세트 옵션)는 <see cref="PerkText.Format"/>·<see cref="GearText.Shorten"/> 이 이미 붙여 주므로 여기 안 걸린다.
    /// 남는 것은 화면 코드가 문자열을 직접 이어 붙이는 자리인데, 지금은 그런 파일이 대부분 다른 워커 lock 안이라 «[PercentGate]» 표로 CI 로그에 남기고
    /// 그 화면 묶음 워커가 <see cref="StatText.Signed"/> 로 바꾼 뒤 <see cref="Strict"/> 를 켠다(<c>TextAudit.ClipStrict</c>·<c>BorderAudit</c> 와 같은 방식 · T90 3항).
    /// </para>
    /// </summary>
    public static class PercentAudit
    {
        /// <summary>% 빠짐을 실패로 셀지 — 표가 0 이 되면 켠다(T90 3항 «먼저 보고만 → 0 이 되면 strict»).</summary>
        // const 가 아니라 static readonly 다 — const false 면 게이트의 «if (Strict)» 가 통째로 «닿지 않는 코드»(CS0162) 경고가 된다.
        public static readonly bool Strict = false;

        public sealed class Row
        {
            public string Screen, Path, Text, Missing;
            public override string ToString() => $"[{Screen}] {Path} «{Short(Text)}» ⛔% 없음 «{Missing}»";
        }

        static string Short(string s) { if (string.IsNullOrEmpty(s)) return ""; s = s.Replace("\n", "⏎"); return s.Length > 24 ? s.Substring(0, 24) + "…" : s; }

        /// <summary>root 아래 활성 Text 가운데 % 가 빠진 비율 스탯 줄만 돌려준다.</summary>
        public static List<Row> Collect(string screen, Transform root)
        {
            var rows = new List<Row>();
            if (root == null) return rows;
            foreach (var t in root.GetComponentsInChildren<Text>(false))
            {
                if (t == null || !t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                string missing = StatText.Missing(t.text);
                if (string.IsNullOrEmpty(missing)) continue;
                rows.Add(new Row { Screen = screen, Path = PathOf(t.transform, root), Text = t.text, Missing = missing });
            }
            return rows;
        }

        static string PathOf(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (var c = t; c != null && c != root && parts.Count < 7; c = c.parent) parts.Add(c.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>화면별 «% 빠진 줄 수 · 조각» 표(마크다운) — CI 로그에서 화면 묶음 워커가 읽는다.</summary>
        public static string Summary(List<Row> rows)
        {
            var byScreen = new Dictionary<string, List<Row>>();
            foreach (var r in rows) { if (!byScreen.TryGetValue(r.Screen, out var l)) byScreen[r.Screen] = l = new List<Row>(); l.Add(r); }
            var sb = new StringBuilder();
            sb.AppendLine("| 화면 | % 빠진 줄 | 조각 |");
            sb.AppendLine("|---|---|---|");
            foreach (var kv in byScreen)
            {
                var frags = new List<string>();
                foreach (var r in kv.Value) foreach (var f in r.Missing.Split('·')) { var s = f.Trim(); if (s.Length > 0 && !frags.Contains(s)) frags.Add(s); }
                sb.AppendLine($"| {kv.Key} | {kv.Value.Count} | {string.Join(" · ", frags)} |");
            }
            return sb.ToString();
        }
    }
}
