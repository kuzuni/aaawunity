using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 특전(perks.json 100종) 적용·제시. sim.js 의 mkPerks().ap / offerPerks / offerDevilPerk / simPickPerk / pickPerk 이식.
    /// 수치는 전부 perks.json constants 에서 읽는다. px 플래그·최댓값 갱신(p_kill*)은 JSON 의 effect.px 를 그대로 쓴다.
    /// </summary>
    public static class Perks
    {
        /// <summary>«같은 이름·다른 등급» 처치 소환 4계열의 합산 키 (perks.json PERK_AGG_KEYS 중 확률 최댓값 갱신분).</summary>
        static readonly string[] AggMaxKeys = { "p_killSpear", "p_killBolt", "p_killArrow", "p_killAxe" };

        /// <summary>sim.js `perk.ap(p)` — 스탯 변화(곱/가산)는 id 별 규칙, px 플래그는 JSON effect.px 그대로.</summary>
        public static void Apply(GameData D, PlayerState p, PerkDef perk)
        {
            var C = D.Perks;
            // ① px 플래그 — JSON 실측대로. 집계 키(p_killSpear·p_killBolt·p_killArrow·p_killAxe)는 최댓값, p_thorns 는 가산, 나머지는 대입.
            foreach (var kv in perk.Px)
            {
                string k = kv.Key; double v = kv.Value;
                if (k == "p_thorns") p.Px[k] = p.PxGet(k) + v;                       // 가시갑옷 — 가산 중첩
                else if (Array.IndexOf(AggMaxKeys, k) >= 0) p.Px[k] = Math.Max(p.PxGet(k), v);   // 처치 시 소환 — 확률 최댓값
                else p.Px[k] = v;
            }
            // ⚑ perks.json 의 effect 는 «탐침 플레이어(공 100 · 방어 0 · healAmp 미탐침)» 실측이라 두 종류가 비어 나온다:
            //   ⓐ 방어력 곱연산(p_def/p_defR/p_defL — 0 × 1.08 = 0 이라 변화 없음)  ⓑ 회복 증폭(p_healUp — PROBE_STATS 에 healAmp 없음).
            //   sim.js 동작(`p.def*=PERK_DEF_M` · `p.healAmp+=PERK_AMP`)을 상수로 복원한다 — PROGRESS 승인 대기(aaaw 수출기 보강 제안).
            switch (perk.Id)
            {
                case "p_def": p.Def *= C.C("PERK_DEF_M"); break;
                case "p_defR": p.Def *= C.C("PERK_DEF_R"); break;
                case "p_defL": p.Def *= C.C("PERK_DEF_L"); break;
                case "p_healUp": p.HealAmp += C.C("PERK_AMP"); break;
            }
            // ② 스탯 — 가산(critR/critF/counter/evade/repairAmp/steal)은 JSON 델타 그대로, 곱연산(dmg/aspd)은 상수.
            foreach (var kv in perk.Stat)
            {
                double delta = kv.Value.To - kv.Value.From;
                switch (kv.Key)
                {
                    case "critR": p.CritR += delta; break;
                    case "critF": p.CritF += delta; break;
                    case "counter": p.Counter += delta; break;
                    case "evade": p.Evade += delta; break;
                    case "repairAmp": p.RepairAmp += delta; break;
                    case "steal": p.Steal += delta; break;
                    case "healAmp": if (perk.Id != "p_healUp") p.HealAmp += delta; break;
                    case "def": if (!perk.Id.StartsWith("p_def")) p.Def += delta; break;
                    case "dmg": p.Dmg *= AtkMulOf(C, perk.Id); break;
                    case "aspd": p.Aspd *= C.C("PERK_GIANT_ASPD"); break;
                    default: throw new InvalidOperationException("perks.json: 모르는 스탯 축 " + kv.Key + " (" + perk.Id + ")");
                }
            }
        }

        static double AtkMulOf(PerksData C, string id)
        {
            switch (id)
            {
                case "p_atk": return C.C("PERK_ATK_M");
                case "p_atkR": return C.C("PERK_ATK_R");
                case "p_berserk": return C.C("PERK_BERSERK_M");
                case "p_giant": return C.C("PERK_GIANT_M");
            }
            throw new InvalidOperationException("공격력 곱연산 특전인데 상수 매핑이 없다: " + id);
        }
        /// <summary>
        /// sim.js `offerPerks(taken, noble)` — 등급 1회 굴림(빈 등급 가중치 0 · 귀족의 눈은 일반 제외) → 그 등급에서 최대 3장(중복 없음).
        /// 난수 소비: 등급 1회 + 카드 수만큼.
        /// </summary>
        public static List<PerkDef> Offer(GameData D, IList<PerkDef> taken, bool noble, IRng rng)
        {
            var C = D.Perks;
            var cand = new List<PerkDef>();
            foreach (var p in C.Perks) if (!taken.Contains(p)) cand.Add(p);
            var out_ = new List<PerkDef>();
            if (cand.Count == 0) return out_;
            var w = new double[3];
            for (int g = 0; g < 3; g++) { bool any = false; foreach (var p in cand) if (p.Grade == g) { any = true; break; } w[g] = any ? C.GradeRate[g] : 0; }
            if (noble && (w[1] > 0 || w[2] > 0)) w[0] = 0;
            double tot = w[0] + w[1] + w[2];
            double r = rng.Next() * tot; int gg = 0;
            for (gg = 0; gg < 3; gg++) { if (r < w[gg]) break; r -= w[gg]; }
            if (gg > 2 || w[gg] == 0) { gg = 2; while (gg > 0 && w[gg] == 0) gg--; }
            var pool = new List<PerkDef>();
            foreach (var p in cand) if (p.Grade == gg) pool.Add(p);
            for (int i = 0; i < C.OfferPerLevel && pool.Count > 0; i++)
            {
                int idx = (int)Math.Floor(rng.Next() * pool.Count);
                out_.Add(pool[idx]); pool.RemoveAt(idx);
            }
            return out_;
        }

        /// <summary>sim.js `offerDevilPerk(taken)` — 아직 안 얻은 전설 중 무작위 1장, 없으면 null.</summary>
        public static PerkDef OfferDevil(GameData D, IList<PerkDef> taken, IRng rng)
        {
            var pool = new List<PerkDef>();
            foreach (var p in D.Perks.Perks) if (p.Grade == D.Perks.DevilGrade && !taken.Contains(p)) pool.Add(p);
            if (pool.Count == 0) return null;
            return pool[(int)Math.Floor(rng.Next() * pool.Count)];
        }

        /// <summary>sim.js `simPickPerk(offer)` — 표 순서가 가장 앞선 것 (시뮬 측정 정책).</summary>
        public static PerkDef SimPick(IList<PerkDef> offer)
        {
            PerkDef b = offer[0];
            foreach (var p in offer) if (p.Order < b.Order) b = p;
            return b;
        }
    }
}
