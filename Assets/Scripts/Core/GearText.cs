using System.Text.RegularExpressions;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 장비 세트 옵션 설명의 <b>표시 문구</b>(T63-gear · 주인 «글씨 너무 작다»). 원문은 aaaw 정본 gear.json <c>optionLadder.options[].desc</c>(불변) —
    /// 세부 팝업(07) 옵션 줄이 본문 크기(40)로 한 줄에 들어가게 <b>표시 시점에만</b> 줄인다(ROUTINE T63 2ⓒ «글자를 줄이지 말고 문구를 줄인다» · <see cref="PerkText"/> 와 같은 방식 · 엔진·데이터 불변).
    /// 규칙은 아래 표 한 곳: «트리거 시 N% 확률로 X» → «트리거 시 N%: X»(T53 특전 표기와 같은 «트리거: 내용» 꼴) · «(공격력의 N%)» → «(공격력 N%)» · «체력 N% 미만일 때» → «체력 N% 미만». 짧은 문구(«치명타 확률 +5» 등)는 그대로.
    /// 잠금 줄 꼬리(«(희귀 이상)»)는 <see cref="LockSuffix"/> — «이상» 을 뺀 «(희귀)» · «(신화 +3강)»(자물쇠 아이콘이 «이 등급부터» 를 뜻한다).
    /// </summary>
    public static class GearText
    {
        sealed class Rule { public Regex Rx; public string To; public Rule(string rx, string to) { Rx = new Regex(rx, RegexOptions.CultureInvariant); To = to; } }
        static readonly Rule[] Rules =
        {
            new Rule(@"^(치명타|피격|회피|처치|공격|반격) 시 (\d+%) 확률로 ", "$1 시 $2: "),
            new Rule(@"^체력 (\d+%) 미만일 때 회피 시 (\d+%) 확률로 ", "체력 $1 미만 회피 시 $2: "),
            new Rule(@" \(공격력의 (\d+%)\)", "(공격력 $1)"),
        };

        /// <summary>표시용 옵션 설명 — 규칙에 걸리는 부분만 바꾸고, 비율 스탯에는 <c>%</c> 를 붙인다(<see cref="StatText.Percent"/> · T90 · 여러 규칙이 겹쳐 적용될 수 있다 · 멱등). null/빈 문자열은 그대로.</summary>
        public static string Shorten(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return desc;
            foreach (var r in Rules) desc = r.Rx.Replace(desc, r.To, 1);
            return StatText.Percent(desc);   // T90 — «치명타 확률 +5» → «치명타 확률 +5%»(원문 불변 · 표시 시점에만)
        }

        /// <summary>잠금 줄 꼬리 — «(희귀)» · «(신화 +3강)». 원문 «(희귀 이상)» 의 «이상» 은 자물쇠가 대신한다(한 줄에 들어가게).</summary>
        public static string LockSuffix(string tier) => string.IsNullOrEmpty(tier) ? "" : " (" + tier + ")";
    }
}
