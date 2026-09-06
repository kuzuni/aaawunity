using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 비율 스탯 표기(T90 · 주인 2026-09-07 «표기상 «회피 +8» 이런 식으로 된 데가 많은데 «회피 +8%» 이런 식으로, 원래 퍼센트로 써져야 하는 부분들 다 퍼센트로 잘 써지게») —
    /// <b>퍼센트로 읽히는 스탯</b>의 값 뒤에 <c>%</c> 를 <b>표시 시점에만</b> 붙인다(원문 = aaaw 정본 <c>data/perks.json</c>·<c>data/gear.json</c> 의 desc · 불변 · 엔진 불변).
    /// <para>
    /// 정본 실측(<c>.aaaw-src</c>) — 같은 원본 안에서 «최대 체력 +10%»·«방어력 +8%»·«흡혈 +8%» 는 % 가 붙어 있는데
    /// «회피 +8»·«치명타 확률 +5»·«치명타 피해 +20»·«반격률 +10» 만 안 붙어 있다(특전 14줄 + 장비 옵션 4종). 그 빠진 자리를 여기서 메운다.
    /// </para>
    /// 표는 <see cref="Ratio"/> 한 곳이고, 값에 이미 <c>%</c>·<c>×</c>·<c>배</c> 가 붙어 있으면 건드리지 않는다(멱등).
    /// 절대값 스탯(공격력·체력·실드·골드·다이아)은 표에 없으므로 «공격력 1234» 같은 줄은 그대로다.
    /// 화면 문구가 여기를 안 거치고 나가는 자리는 <c>PercentAudit</c>(«[PercentGate]» 표)가 CI 로그로 알려 준다.
    /// </summary>
    public static class StatText
    {
        /// <summary>
        /// 퍼센트로 읽히는 스탯 이름 — 정본 문구(perks.json·gear.json)와 전투 스탯 패널 라벨(BattleScreen)에서 실제로 쓰이는 이름만 담는다.
        /// <b>긴 이름을 앞에</b> 둔다(«회피율» 이 «회피» 보다 앞 · 정규식 대안은 앞에서부터 맞춰 본다).
        /// </summary>
        public static readonly string[] Ratio =
        {
            "치명타 확률", "치명타 피해", "치명타 배율",
            "반격 확률", "반격률",
            "회피율", "회피",
            "흡혈", "방어력", "가시갑옷",
            "최대 체력", "최대 실드",
        };

        // «이름 +N» / «이름 -N» 인데 뒤에 % · × · 배 · 숫자가 안 오는 자리. 부호가 꼭 있어야 한다 — 부호 없는 «공격력 1234»(절대값)를 건드리지 않기 위해서다.
        static readonly Regex Rx = new Regex(
            @"(?<name>" + string.Join("|", Ratio) + @")(?<gap>\s*)(?<sign>[+\-])(?<num>\d+(?:\.\d+)?)(?![\d.]|\s*[%×배])",
            RegexOptions.CultureInvariant);

        /// <summary>표시용 문구 — 비율 스탯의 값 뒤에 <c>%</c> 를 붙인다. 이미 붙어 있으면 그대로(멱등). null/빈 문자열은 그대로.</summary>
        public static string Percent(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return Rx.Replace(text, m => m.Groups["name"].Value + m.Groups["gap"].Value + m.Groups["sign"].Value + m.Groups["num"].Value + "%");
        }

        /// <summary>이 문구에서 <c>%</c> 가 빠진 비율 스탯 조각들(«회피 +8» 꼴 · 중복 없이) — 없으면 빈 문자열. 감사표(«[PercentGate]») 와 테스트가 쓴다.</summary>
        public static string Missing(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            List<string> found = null;
            foreach (Match m in Rx.Matches(text))
            {
                string frag = m.Value;
                if (found == null) found = new List<string>();
                if (!found.Contains(frag)) found.Add(frag);
            }
            if (found == null) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < found.Count; i++) { if (i > 0) sb.Append(" · "); sb.Append(found[i]); }
            return sb.ToString();
        }

        /// <summary>이 이름이 비율 스탯인가(표에 있는가) — 화면 코드가 «라벨 + 값» 을 직접 조립할 때 쓴다.</summary>
        public static bool IsRatio(string statName)
        {
            if (string.IsNullOrEmpty(statName)) return false;
            foreach (var n in Ratio) if (n == statName) return true;
            return false;
        }

        /// <summary>«라벨 값» 한 조각 — 비율 스탯이면 <c>%</c> 가 붙는다(«회피» + 8 → «회피 +8%» · «공격력» + 1234 → «공격력 +1234»). 화면 코드의 문자열 이어 붙이기 대신 쓴다.</summary>
        public static string Signed(string statName, double value, string format = "0.##")
        {
            string num = (value >= 0 ? "+" : "-") + System.Math.Abs(value).ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            return statName + " " + num + (IsRatio(statName) ? "%" : "");
        }
    }
}
