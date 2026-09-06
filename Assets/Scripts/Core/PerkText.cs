using System.Text.RegularExpressions;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 특전 설명 표기 «트리거: 내용» (주인 2026-09-06 · T53) — «처치 시 33% 확률로 …» → «처치 시: 33% 확률로 …».
    /// 원문은 aaaw 정본 perks.json 의 desc(불변) · 표시 시점에만 바꾼다(엔진·데이터 불변). 트리거 구가 없는 상시 능력치(«공격력 +30%» 등)는 «패시브: 공격력 +30%»(주인 정정 09:3X).
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

        /// <summary>상시 능력치 접두어(주인 정정 09:3X «상시 같은 거는 패시브:») — 100개 전부 «무언가: » 로 시작하게 된다.</summary>
        public const string PassivePrefix = "패시브: ";
        static readonly Regex AlreadyRx = new Regex(@"^[^:]{1,24}: ", RegexOptions.CultureInvariant);   // 이미 «트리거: » 꼴(멱등)

        /// <summary>표시용 설명 — 트리거 구가 있으면 «트리거: 내용», 없으면 «패시브: 원문». null/빈 문자열은 그대로 돌려준다.</summary>
        public static string Format(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return desc;
            foreach (var r in Rules)
            {
                var m = r.Rx.Match(desc);
                if (m.Success) return r.Rx.Replace(desc, r.To, 1);
            }
            if (AlreadyRx.IsMatch(desc)) return desc;
            return PassivePrefix + desc;
        }

        /// <summary>원문에 트리거 구가 있는가(«처치 시 …» 등 · Rules 에 걸리는가) — 없으면 «패시브: » 가 붙는다. 테스트·진단용.</summary>
        public static bool HasTrigger(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return false;
            foreach (var r in Rules) if (r.Rx.IsMatch(desc)) return true;
            return false;
        }
    }
}
