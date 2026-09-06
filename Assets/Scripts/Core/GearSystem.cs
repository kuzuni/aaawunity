using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>장비 한 점. sim.js `{part,type,rar,plus}` + 게임의 uid/NEW 뱃지.</summary>
    public sealed class GearItem
    {
        public int Uid; public string Part, Type; public int Rar, Plus; public bool IsNew;
        public GearItem Clone() => new GearItem { Uid = Uid, Part = Part, Type = Type, Rar = Rar, Plus = Plus, IsNew = IsNew };
        public string GroupKey => Part + "|" + Type + "|" + Rar;
        public override string ToString() => $"{Type} r{Rar}+{Plus}";
    }

    /// <summary>빌드 = 부위별 장착 장비 + 부위별 슬롯 레벨 (sim.js `build={eq,slots}`).</summary>
    public sealed class Build
    {
        public Dictionary<string, GearItem> Eq = new Dictionary<string, GearItem>();
        public Dictionary<string, int> Slots = new Dictionary<string, int>();
        public GearItem EqAt(string part) => Eq.TryGetValue(part, out var g) ? g : null;
        public int SlotAt(string part) => Slots.TryGetValue(part, out var l) ? l : 0;
    }

    public struct Power { public double Atk, Hp, Sh; }

    public sealed class GachaState { public int P50, P10, Pulls; }

    /// <summary>장비 수식 (PLAN §11) — sim.js 와 같은 동사: buildPower · gachaPull · fuseMake · fuseAll · autoEquip.</summary>
    public static class GearSystem
    {
        public static Build MkBuild(GameData D, int rar, int plus, int slotLv, int typeIdx = 0)
        {
            var b = new Build();
            foreach (var pt in D.Gear.Parts)
            {
                b.Eq[pt] = rar < 0 ? null : new GearItem { Part = pt, Type = D.Gear.Types[pt][typeIdx], Rar = rar, Plus = plus };
                b.Slots[pt] = slotLv;
            }
            return b;
        }

        public static double EvenBonus(GameData D, Build b)
        {
            int mn = int.MaxValue;
            foreach (var pt in D.Gear.Parts) mn = Math.Min(mn, b.SlotAt(pt));
            return 1 + D.Gear.EvenStep * Math.Floor((double)mn / D.Gear.EvenPer);
        }

        /// <summary>sim.js `buildPower(b)` — 부위 기여 = 등급 기여 × (1 + plusStep×강화) × 슬롯 배수, 마지막에 균등 보너스.</summary>
        public static Power BuildPower(GameData D, Build b)
        {
            var G = D.Gear; var T = D.Tune;
            double atk = 0, hp = 0, sh = 0;
            foreach (var pt in G.Parts)
            {
                var g = b.EqAt(pt); if (g == null) continue;
                double m = G.SlotMul(b.SlotAt(pt)) * (1 + G.PlusStep * g.Plus);
                atk += G.Atk[g.Rar] * m; hp += G.Hp[g.Rar] * m; sh += G.Sh[g.Rar] * m;
            }
            double ev = EvenBonus(D, b);
            return new Power { Atk = (T.PAtk0 + atk) * ev, Hp = (T.PHp0 + hp) * ev, Sh = (T.PSh0 + sh) * ev };
        }

        /// <summary>부위 하나의 «날» 기여 — 등급 기여 × 강화 × 슬롯(재분배 전 · <see cref="BuildPower"/> 가 더하는 그 값).</summary>
        public static Power Contribution(GameData D, GearItem g, int slotLv)
        {
            double m = D.Gear.SlotMul(slotLv) * (1 + D.Gear.PlusStep * g.Plus);
            return new Power { Atk = D.Gear.Atk[g.Rar] * m, Hp = D.Gear.Hp[g.Rar] * m, Sh = D.Gear.Sh[g.Rar] * m };
        }

        static Power RawAt(GameData D, Build b, string part)
        {
            var g = b.EqAt(part); if (g == null) return new Power();
            return Contribution(D, g, b.SlotAt(part));
        }

        /// <summary>
        /// 부위 하나의 <b>보여 주는</b> 기여 (T88 재분배 · 세부 팝업 07 의 스탯 박스). 주인 지시대로 공격 부위(무기·목걸이·반지)는 <b>공격력만</b>,
        /// 방어 부위(투구·갑옷·신발)는 <b>체력·실드만</b> 갖는다 — 없어지는 몫은 사라지지 않고 <b>같은 역할의 부위들이 원래 비율대로 나눠 갖는다</b>.
        /// 그래서 «부위별 기여의 합 = 재분배 전의 합» 이 정확히 성립하고(마지막 부위가 나머지를 받아 반올림 오차도 0),
        /// 전투에 들어가는 <see cref="BuildPower"/> 는 애초에 건드리지 않으므로 T2 시드 골든이 그대로다.
        /// 같은 역할의 장착 부위가 하나도 없으면(예: 공격 부위 전부 빈 슬롯) 그 몫은 표시할 자리가 없다 — 총합 표시(장비 화면 06 의 3칸)는 여전히 <see cref="BuildPower"/> 라 정확하다.
        /// </summary>
        public static Power ContributionIn(GameData D, Build b, string part)
        {
            if (D == null || b == null || part == null || b.EqAt(part) == null) return new Power();
            var G = D.Gear;
            bool atk = GearRole.IsAttack(part);
            var peers = new List<string>();
            foreach (var pt in G.Parts) if (b.EqAt(pt) != null && GearRole.IsAttack(pt) == atk) peers.Add(pt);
            double tAtk = 0, tHp = 0, tSh = 0;
            foreach (var pt in G.Parts) { var c = RawAt(D, b, pt); tAtk += c.Atk; tHp += c.Hp; tSh += c.Sh; }
            if (atk) return new Power { Atk = ShareOf(peers, part, tAtk, p => RawAt(D, b, p).Atk) };
            return new Power
            {
                Hp = ShareOf(peers, part, tHp, p => RawAt(D, b, p).Hp),
                Sh = ShareOf(peers, part, tSh, p => RawAt(D, b, p).Sh),
            };
        }

        /// <summary>총합 <paramref name="total"/> 을 <paramref name="peers"/> 가 무게대로 나눌 때 <paramref name="part"/> 의 몫. 무게 합이 0 이면 균등 · 마지막 부위는 «총합 − 앞의 합» 이라 합이 정확히 총합이 된다.</summary>
        static double ShareOf(List<string> peers, string part, double total, Func<string, double> weight)
        {
            int n = peers.Count; if (n == 0) return 0;
            int idx = peers.IndexOf(part); if (idx < 0) return 0;
            double sw = 0; foreach (var p in peers) sw += weight(p);
            if (idx < n - 1) return Portion(peers[idx], total, sw, n, weight);
            double acc = 0; for (int i = 0; i < n - 1; i++) acc += Portion(peers[i], total, sw, n, weight);
            return total - acc;
        }
        static double Portion(string p, double total, double sw, int n, Func<string, double> weight) => sw > 0 ? total * weight(p) / sw : total / n;

        /// <summary>
        /// sim.js `gachaPull(st, box)` — 보통 1개, 신화 천장 × 전설 피티가 겹치면 2개(전설 추가). 난수는 뽑기 스트림.
        /// </summary>
        public static List<GearItem> GachaPull(GameData D, GachaState st, GachaBox box, IRng rng)
        {
            var G = D.Gear;
            st.Pulls++; st.P50++; st.P10++;
            bool pityM = box.PityMyth > 0 && st.P50 >= box.PityMyth, pityL = box.PityLegend > 0 && st.P10 >= box.PityLegend;
            int rar;
            if (pityM) rar = G.RarMyth;
            else
            {
                double r = rng.Next() * 100;
                rar = box.RarRoll(r);
                if (pityL && rar < G.RarLegend) rar = G.RarLegend;
            }
            if (rar == G.RarMyth) st.P50 = 0;
            if (rar >= G.RarLegend) st.P10 = 0;
            GearItem Mk(int rr) { var t = G.AllTypes[(int)Math.Floor(rng.Next() * G.AllTypes.Count)]; return new GearItem { Part = t.Part, Type = t.Type, Rar = rr, Plus = 0 }; }
            var out_ = new List<GearItem> { Mk(rar) };
            if (pityM && pityL) out_.Add(Mk(G.RarLegend));
            return out_;
        }

        /// <summary>sim.js `fuseMake(base)` — 같은 부위·종류·등급 3개 → 산출물 규칙 (자동·수동 공용).</summary>
        public static GearItem FuseMake(GameData D, GearItem b)
        {
            var G = D.Gear;
            if (b.Rar < G.RarLegend) return new GearItem { Part = b.Part, Type = b.Type, Rar = b.Rar + 1, Plus = 0 };
            if (b.Rar == G.RarLegend)
            {
                int np = b.Plus + 1;
                return np >= G.LegendToMythPlus
                    ? new GearItem { Part = b.Part, Type = b.Type, Rar = G.RarMyth, Plus = 0 }
                    : new GearItem { Part = b.Part, Type = b.Type, Rar = G.RarLegend, Plus = np };
            }
            return new GearItem { Part = b.Part, Type = b.Type, Rar = G.RarMyth, Plus = b.Plus + 1 };
        }

        /// <summary>재료 3개에서 base(최고 강화) 를 고르고 산출물을 만든다.</summary>
        public static GearItem FuseThree(GameData D, IList<GearItem> mats)
        {
            GearItem best = null;
            foreach (var m in mats) if (best == null || m.Plus > best.Plus) best = m;
            return FuseMake(D, best);
        }

        /// <summary>
        /// sim.js `fuseAll(inv, equipped)` — 합성 가능한 묶음이 없어질 때까지 반복. 만든 개수 반환.
        /// <paramref name="equipped"/> 는 재료에서 뺄 집합(null/빈 집합 = 장착분도 재료 · T24 주인 «대장간에 장착중인 거도 합성 가능하게»).
        /// <paramref name="onFused"/> 는 합성 1회마다(재료 3개 · 산출물 — uid 부여 뒤) 불린다 — 게임은 여기서 <see cref="ReEquipAfterFuse"/> 로 장착 슬롯을 정리한다.
        /// </summary>
        public static int FuseAll(GameData D, List<GearItem> inv, HashSet<GearItem> equipped, Func<GearItem, int> assignUid = null, Action<IList<GearItem>, GearItem> onFused = null)
        {
            bool did = true; int count = 0;
            while (did)
            {
                did = false;
                var groups = new Dictionary<string, List<GearItem>>();
                var order = new List<string>();
                foreach (var g in inv)
                {
                    if (equipped != null && equipped.Contains(g)) continue;
                    if (!groups.TryGetValue(g.GroupKey, out var l)) { l = new List<GearItem>(); groups[g.GroupKey] = l; order.Add(g.GroupKey); }
                    l.Add(g);
                }
                foreach (var k in order)
                {
                    var arr = groups[k];
                    if (arr.Count < 3) continue;
                    arr.Sort((a, b) => b.Plus.CompareTo(a.Plus));
                    var mats = arr.GetRange(0, 3);
                    var made = FuseMake(D, mats[0]);
                    foreach (var m in mats) inv.Remove(m);
                    if (assignUid != null) made.Uid = assignUid(made);
                    inv.Add(made); count++; did = true;
                    onFused?.Invoke(mats, made);
                    break;
                }
            }
            return count;
        }

        /// <summary>
        /// 합성 뒤 장착 슬롯 정리(T24 · 승인 대기 29 기본값): 재료로 사라진 장비가 장착 중이었으면 **산출물이 같은 부위면 그 슬롯에 장착**, 아니면 슬롯을 비운다.
        /// (같은 부위·종류·등급 3개 규칙이라 산출물은 항상 같은 부위 — 자동 장착 금지 원칙의 유일한 예외 · 산출물은 재료보다 항상 좋다.)
        /// 재료가 장착 중이 아니었으면 아무것도 바꾸지 않는다(자동 장착 없음 · aaaw T125 ①-c 그대로). 순수 C# — 수동(FuseMake)·자동(FuseAll onFused) 둘 다 이 함수 하나만 쓴다.
        /// </summary>
        public static void ReEquipAfterFuse(SaveData S, IList<GearItem> mats, GearItem made)
        {
            if (S == null || mats == null) return;
            var parts = new List<string>();
            foreach (var kv in S.Eq) foreach (var m in mats) if (m != null && m.Uid == kv.Value && m.Part == kv.Key) { parts.Add(kv.Key); break; }
            foreach (var part in parts)
            {
                if (made != null && made.Uid > 0 && made.Part == part) S.Eq[part] = made.Uid;
                else S.Eq.Remove(part);
            }
        }

        public static int GearScore(GearItem g) => g.Rar * 1000 + g.Plus;

        /// <summary>sim.js `autoEquip(inv)` — 부위마다 점수 최고품 (시뮬 측정 정책 · 게임은 «↑ 표시» 에만 쓴다).</summary>
        public static Dictionary<string, GearItem> AutoEquip(List<GearItem> inv)
        {
            var eq = new Dictionary<string, GearItem>();
            foreach (var g in inv) { if (!eq.TryGetValue(g.Part, out var b) || GearScore(g) > GearScore(b)) eq[g.Part] = g; }
            return eq;
        }
    }
}
