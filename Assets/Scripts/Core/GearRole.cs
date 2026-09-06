using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 부위 «역할» 표 (T88 · 주인 2026-09-07 «무기·목걸이·반지 → 공격력만 · 나머지 부위는 체력이랑 실드 · 공격력·실드·HP 의 총값은 유지»).
    /// <list type="bullet">
    /// <item>부위 키는 aaaw 정본 <c>data/gear.json</c> 그대로다(장갑 = <c>glove</c>) — <b>표시 이름만</b> «반지» 로 덮는다(데이터 파일 불변 · <see cref="DisplayName"/>).</item>
    /// <item>전투에 들어가는 합계 <see cref="GearSystem.BuildPower"/> 는 <b>한 줄도 손대지 않는다</b> — 이 표가 정하는 것은 «그 합계를 부위별로 어떻게 나눠 보여 주는가» 뿐이다(T2 시드 골든 불변).</item>
    /// </list>
    /// </summary>
    public static class GearRole
    {
        /// <summary>공격 부위 — 공격력만 보여 준다(무기 · 목걸이 · 반지(=glove)).</summary>
        public static readonly string[] AttackParts = { "weapon", "neck", "glove" };
        /// <summary>방어 부위 — 체력·실드만 보여 준다(투구 · 갑옷 · 신발).</summary>
        public static readonly string[] DefenseParts = { "helm", "armor", "boot" };

        /// <summary>표시 이름 덮어쓰기 — 주인 지시 «장갑 → 반지»(gear.json 은 aaaw 정본이라 손대지 않는다).</summary>
        static readonly Dictionary<string, string> NameOverride = new Dictionary<string, string> { { "glove", "반지" } };

        public static bool IsAttack(string part) => Array.IndexOf(AttackParts, part) >= 0;
        public static bool IsDefense(string part) => Array.IndexOf(DefenseParts, part) >= 0;

        /// <summary>부위 표시 이름 — 덮어쓰기 표가 먼저, 없으면 gear.json 의 <c>partName</c>, 그것도 없으면 키 그대로.</summary>
        public static string DisplayName(GameData D, string part)
        {
            if (part != null && NameOverride.TryGetValue(part, out var over)) return over;
            if (D != null && part != null && D.Gear.PartName.TryGetValue(part, out var n)) return n;
            return part;
        }
    }
}
