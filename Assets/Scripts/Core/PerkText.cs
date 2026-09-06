using System.Text.RegularExpressions;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 특전 설명 표기 «트리거: 내용» (주인 2026-09-06 · T53) — «처치 시 33% 확률로 …» → «처치 시: 33% 확률로 …».
    /// 원문은 aaaw 정본 perks.json 의 desc(불변) · 표시 시점에만 바꾼다(엔진·데이터 불변). 트리거 구가 없는 상시 능력치(«공격력 +30%» 등)는 원문 그대로.
    /// 규칙은 아래 표 한 곳 — 앞머리 트리거 구를 떼어 «트리거: 나머지» 로. 이미 콜론이 붙어 있으면(«처치 시: …») 다시 안 붙는다(멱등).
    /// 색·굵기는 넣지 않는다(T52 «특전 글자 한 색»).
    /// </summary>
    public static class PerkText
    {
        sealed class Rule { public Regex Rx; public string To; public Rule(string rx, string to) { Rx = new Regex(rx, RegexOptions.CultureInvariant); To = to; } }
        // 앞선 규칙이 먼저 — «실드가 있으면 피격 시» 는 «피격 시» 보다 앞에 둔다
        static readonly Rule[] Rules =
        {
            new Rule(@"^실드가 있으면 피격 시 ", "피격 시(실드 있을 때): "),
            new Rule(@"^실드가 0 인 동안 ", "실드 0 일 때: "),
            new Rule(@"^보유 특전 하나당 ", "특전 하나당: "),
            new Rule(@"^체력이 가득 찬 적 공격 시 ", "공격 시(체력 가득 찬 적): "),
            new Rule(@"^체력 회복 시 ", "체력 회복 시: "),
            new Rule(@"^(처치|피격|공격|반격|회피|치명타) 시 ", "$1 시: "),
            new Rule(@"^(\d+타마다) ", "$1: "),
            new Rule(@"^평타 적중마다 ", "평타 적중마다: "),
        };

        /// <summary>표시용 설명 — 트리거 구가 있으면 «트리거: 내용», 없으면 원문 그대로. null/빈 문자열은 그대로 돌려준다.</summary>
        public static string Format(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return desc;
            foreach (var r in Rules)
            {
                var m = r.Rx.Match(desc);
                if (m.Success) return r.Rx.Replace(desc, r.To, 1);
            }
            return desc;
        }

        /// <summary>트리거 구가 있는 설명인가(= Format 이 원문을 바꾸는가) — 테스트·진단용.</summary>
        public static bool HasTrigger(string desc) => !string.IsNullOrEmpty(desc) && Format(desc) != desc;
    }
}
