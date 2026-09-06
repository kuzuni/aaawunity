using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    public class GearTests
    {
        // PLAN §11.7 사다리 7점 확정 스탯 (공/체/실) — 엔진 실측이 0.5% 안에서 같아야 한다 (sim.js LADDER_STAT).
        static readonly object[] Ladder =
        {
            new object[] { -1, 0, 0, 25.0, 150.0, 250.0 },
            new object[] { 0, 0, 0, 50.0, 250.0, 400.0 },
            new object[] { 1, 0, 5, 108.9, 543.4, 868.9 },
            new object[] { 2, 0, 15, 524.7, 2619.1, 4188.9 },
            new object[] { 3, 0, 25, 3742.2, 18703.1, 29921.9 },
            new object[] { 3, 9, 50, 106912.0, 533475.0, 853125.0 },
            new object[] { 3, 9, 100, 190050.0, 948300.0, 1516500.0 },
        };

        [TestCaseSource(nameof(Ladder))]
        public void BuildPowerMatchesLadderTable(int rar, int plus, int slot, double atk, double hp, double sh)
        {
            var d = TestData.Load();
            var pw = GearSystem.BuildPower(d, GearSystem.MkBuild(d, rar, plus, slot));
            Assert.That(pw.Atk, Is.EqualTo(atk).Within(0.5).Percent);
            Assert.That(pw.Hp, Is.EqualTo(hp).Within(0.5).Percent);
            Assert.That(pw.Sh, Is.EqualTo(sh).Within(0.5).Percent);
        }

        [Test]
        public void PlusNineIsExactlyTwentyTimes()
        {
            var d = TestData.Load();
            Assert.That(1 + d.Gear.PlusStep * 9, Is.EqualTo(20.0));
        }

        [Test]
        public void FuseRules()
        {
            var d = TestData.Load(); var G = d.Gear;
            var common = new GearItem { Part = "weapon", Type = "crit_weapon", Rar = 0 };
            Assert.That(GearSystem.FuseMake(d, common).Rar, Is.EqualTo(1));
            var rare = new GearItem { Part = "weapon", Type = "crit_weapon", Rar = 1 };
            Assert.That(GearSystem.FuseMake(d, rare).Rar, Is.EqualTo(G.RarLegend));
            var leg = new GearItem { Part = "weapon", Type = "crit_weapon", Rar = G.RarLegend, Plus = 0 };
            var l1 = GearSystem.FuseMake(d, leg);
            Assert.That(l1.Rar, Is.EqualTo(G.RarLegend)); Assert.That(l1.Plus, Is.EqualTo(1));
            var legMax = new GearItem { Part = "weapon", Type = "crit_weapon", Rar = G.RarLegend, Plus = G.LegendToMythPlus - 1 };
            var m0 = GearSystem.FuseMake(d, legMax);
            Assert.That(m0.Rar, Is.EqualTo(G.RarMyth)); Assert.That(m0.Plus, Is.EqualTo(0));
            var myth = new GearItem { Part = "weapon", Type = "crit_weapon", Rar = G.RarMyth, Plus = 4 };
            Assert.That(GearSystem.FuseMake(d, myth).Plus, Is.EqualTo(5));
            // 주인 확정 제약: 신화 0강 > 전설 최대강 (부위당 공격력)
            double legMaxAtk = G.Atk[G.RarLegend] * (1 + G.PlusStep * (G.LegendToMythPlus - 1));
            Assert.That(G.Atk[G.RarMyth], Is.GreaterThan(legMaxAtk));
        }

        [Test]
        public void FuseAllConsumesThreeAndSkipsEquipped()
        {
            var d = TestData.Load();
            var inv = new List<GearItem>();
            for (int i = 0; i < 4; i++) inv.Add(new GearItem { Uid = i + 1, Part = "helm", Type = "hpsh_helm", Rar = 0 });
            var equipped = new HashSet<GearItem> { inv[0] };
            int n = GearSystem.FuseAll(d, inv, equipped);
            Assert.That(n, Is.EqualTo(1));
            Assert.That(inv.Count, Is.EqualTo(2));
            Assert.That(inv.Contains(equipped.First()), Is.True);
        }

        // ───────── T24 — 장착 중 장비도 합성 재료(주인 2026-09-06) · 재료가 된 장착분의 슬롯엔 산출물(같은 부위)을 장착(승인 대기 29 기본값) ─────────
        static SaveData SaveWith(int n, string part, string type, int rar)
        {
            var S = new SaveData();
            for (int i = 0; i < n; i++) S.Inv.Add(S.NewGear(part, type, rar, 0));
            return S;
        }

        [Test]
        public void FuseAllWithoutExclusionConsumesEquippedAndEquipsTheResult()
        {
            var d = TestData.Load();
            var S = SaveWith(3, "helm", "hpsh_helm", 0);
            S.Eq["helm"] = S.Inv[0].Uid;                                                       // 재료 중 하나가 장착 중
            int n = GearSystem.FuseAll(d, S.Inv, null, g => S.Uid++, (mats, made) => GearSystem.ReEquipAfterFuse(S, mats, made));
            Assert.That(n, Is.EqualTo(1));
            Assert.That(S.Inv.Count, Is.EqualTo(1));
            var eq = S.EquippedGear("helm");
            Assert.That(eq, Is.Not.Null, "장착 슬롯이 비면 안 된다 — 산출물이 그 자리에");
            Assert.That(eq.Rar, Is.EqualTo(1)); Assert.That(eq.Uid, Is.GreaterThan(0)); Assert.That(S.IsEquipped(S.Inv[0]), Is.True);
        }

        [Test]
        public void FuseAllChainKeepsTheSlotOnTheFinalProduct()
        {
            var d = TestData.Load();
            var S = SaveWith(9, "helm", "hpsh_helm", 0);                                         // 9×일반 → 3×희귀 → 1×전설
            S.Eq["helm"] = S.Inv[4].Uid;
            int n = GearSystem.FuseAll(d, S.Inv, null, g => S.Uid++, (mats, made) => GearSystem.ReEquipAfterFuse(S, mats, made));
            Assert.That(n, Is.EqualTo(4));
            Assert.That(S.Inv.Count, Is.EqualTo(1));
            var eq = S.EquippedGear("helm");
            Assert.That(eq, Is.Not.Null); Assert.That(eq.Rar, Is.EqualTo(d.Gear.RarLegend)); Assert.That(eq, Is.SameAs(S.Inv[0]));
        }

        [Test]
        public void FuseDoesNotTouchSlotsWhoseGearWasNotAMaterial()
        {
            var d = TestData.Load();
            var S = SaveWith(3, "helm", "hpsh_helm", 0);
            var other = S.NewGear("helm", "hpsh_helm", 1, 0); S.Inv.Add(other); S.Eq["helm"] = other.Uid;   // 장착분은 다른 키(희귀) — 재료 아님
            var boot = S.NewGear("boot", "crit_boot", 0, 0); S.Inv.Add(boot); S.Eq["boot"] = boot.Uid;
            int n = GearSystem.FuseAll(d, S.Inv, null, g => S.Uid++, (mats, made) => GearSystem.ReEquipAfterFuse(S, mats, made));
            Assert.That(n, Is.EqualTo(1));
            Assert.That(S.Eq["helm"], Is.EqualTo(other.Uid), "재료가 아닌 장착분은 그대로(자동 장착 없음)");
            Assert.That(S.Eq["boot"], Is.EqualTo(boot.Uid));
        }

        [Test]
        public void ReEquipAfterFuseEmptiesTheSlotWhenTheProductIsAnotherPartOrHasNoUid()
        {
            var S = new SaveData();
            var a = S.NewGear("helm", "hpsh_helm", 0, 0); S.Inv.Add(a); S.Eq["helm"] = a.Uid;
            GearSystem.ReEquipAfterFuse(S, new List<GearItem> { a }, new GearItem { Uid = 99, Part = "armor", Type = "hpsh_armor", Rar = 1 });
            Assert.That(S.Eq.ContainsKey("helm"), Is.False, "부위가 다르면 빈 슬롯");
            var b = S.NewGear("helm", "hpsh_helm", 0, 0); S.Inv.Add(b); S.Eq["helm"] = b.Uid;
            GearSystem.ReEquipAfterFuse(S, new List<GearItem> { b }, new GearItem { Uid = 0, Part = "helm", Type = "hpsh_helm", Rar = 1 });
            Assert.That(S.Eq.ContainsKey("helm"), Is.False, "uid 없는 산출물은 가리킬 수 없다 → 빈 슬롯");
            GearSystem.ReEquipAfterFuse(S, null, null); GearSystem.ReEquipAfterFuse(null, new List<GearItem>(), null);   // 퇴화 입력에 예외 없음
        }

        [Test]
        public void GachaPityGivesMythAtCeilingAndLegendBonusWhenOverlapping()
        {
            var d = TestData.Load(); var box = d.Gacha.Box("myth");
            var st = new GachaState { P50 = box.PityMyth - 1, P10 = box.PityLegend - 1 };
            var got = GearSystem.GachaPull(d, st, box, new Mulberry32(1));
            Assert.That(got.Count, Is.EqualTo(2));
            Assert.That(got[0].Rar, Is.EqualTo(d.Gear.RarMyth));
            Assert.That(got[1].Rar, Is.EqualTo(d.Gear.RarLegend));
            Assert.That(st.P50, Is.EqualTo(0)); Assert.That(st.P10, Is.EqualTo(0));
        }

        [Test]
        public void GachaRateDistributionRoughlyMatchesTable()
        {
            var d = TestData.Load(); var box = d.Gacha.Box("rare");
            var st = new GachaState(); var rng = new Mulberry32(7); var cnt = new int[4];
            for (int i = 0; i < 20000; i++) foreach (var g in GearSystem.GachaPull(d, st, box, rng)) cnt[g.Rar]++;
            Assert.That(cnt[1] / 20000.0 * 100, Is.EqualTo(box.Rate[1]).Within(1.5));
            Assert.That(cnt[2] + cnt[3], Is.EqualTo(0));
        }

        // ───────── T26 — 뽑기 확률 검증(주인 «확률에 안 맞게 뽑히는 것 같다») · index.html gachaPull 과 줄 단위 동일 · 통계는 시드 고정 Mulberry32 ─────────
        const int GachaSample = 10000; const double GachaTolPct = 1.5;

        /// <summary>천장·피티가 절대 안 걸리게(매번 새 카운터) 뽑아 «자연 굴림» 만 센다 — 등급별 % 를 돌려준다.</summary>
        static double[] NaturalRollPct(GameData d, GachaBox box, IRng rng, int n)
        {
            var cnt = new int[box.Rate.Length];
            for (int i = 0; i < n; i++) { var got = GearSystem.GachaPull(d, new GachaState(), box, rng); Assert.That(got.Count, Is.EqualTo(1)); cnt[got[0].Rar]++; }
            var pct = new double[cnt.Length]; for (int r = 0; r < cnt.Length; r++) pct[r] = cnt[r] * 100.0 / n; return pct;
        }
        static void AssertPctMatchesTable(GachaBox box, double[] pct, string what)
        {
            for (int r = 0; r < box.Rate.Length; r++)
            {
                if (box.Rate[r] <= 0) Assert.That(pct[r], Is.EqualTo(0), $"{box.Key} {what}: 확률 0 인 등급 {r} 이 나왔다");
                else Assert.That(pct[r], Is.EqualTo(box.Rate[r]).Within(GachaTolPct), $"{box.Key} {what}: 등급 {r} 관측 {pct[r]:0.00}% ↔ 표 {box.Rate[r]}%");
            }
        }

        [Test]
        public void GachaNaturalRollMatchesRateTableForEveryBox()
        {
            var d = TestData.Load(); uint seed = 26;
            foreach (var box in d.Gacha.Boxes)
            {
                var pct = NaturalRollPct(d, box, new Mulberry32(seed++), GachaSample);
                AssertPctMatchesTable(box, pct, "자연 굴림 10,000회");
            }
        }

        [Test]
        public void GachaPityFiresExactlyAtTheCeilingAndNeverLater()
        {
            var d = TestData.Load(); var G = d.Gear;
            foreach (var box in d.Gacha.Boxes)
            {
                var st = new GachaState(); var rng = new Mulberry32(2026);
                int sinceLegend = 0, sinceMyth = 0, pityLegendHits = 0, pityMythHits = 0, overlaps = 0;
                for (int i = 0; i < 2 * GachaSample; i++)
                {
                    sinceLegend++; sinceMyth++;
                    bool expectPityL = box.PityLegend > 0 && sinceLegend >= box.PityLegend, expectPityM = box.PityMyth > 0 && sinceMyth >= box.PityMyth;
                    if (box.PityLegend > 0) Assert.That(sinceLegend, Is.LessThanOrEqualTo(box.PityLegend), $"{box.Key}: 전설 피티가 {box.PityLegend}회를 넘겨 걸렸다");
                    if (box.PityMyth > 0) Assert.That(sinceMyth, Is.LessThanOrEqualTo(box.PityMyth), $"{box.Key}: 신화 천장이 {box.PityMyth}회를 넘겨 걸렸다");
                    var got = GearSystem.GachaPull(d, st, box, rng); int rar = got[0].Rar;
                    if (expectPityM) { Assert.That(rar, Is.EqualTo(G.RarMyth), $"{box.Key}: {box.PityMyth}회째는 신화 확정"); pityMythHits++; }
                    if (expectPityL) { Assert.That(rar, Is.GreaterThanOrEqualTo(G.RarLegend), $"{box.Key}: {box.PityLegend}회째는 전설 이상 확정"); pityLegendHits++; }
                    if (expectPityM && expectPityL) { Assert.That(got.Count, Is.EqualTo(2)); Assert.That(got[1].Rar, Is.EqualTo(G.RarLegend)); overlaps++; }
                    else Assert.That(got.Count, Is.EqualTo(1), $"{box.Key}: 겹침이 아닌데 2개");
                    if (rar == G.RarMyth) sinceMyth = 0;
                    if (rar >= G.RarLegend) sinceLegend = 0;
                    Assert.That(st.P10, Is.EqualTo(sinceLegend)); Assert.That(st.P50, Is.EqualTo(sinceMyth)); Assert.That(st.Pulls, Is.EqualTo(i + 1));
                }
                if (box.PityLegend > 0) Assert.That(pityLegendHits, Is.GreaterThan(0), $"{box.Key}: 전설 피티가 한 번도 안 걸림(표본 부족?)");
                if (box.PityMyth > 0) Assert.That(pityMythHits, Is.GreaterThan(0), $"{box.Key}: 신화 천장이 한 번도 안 걸림(표본 부족?)");
                if (box.PityMyth > 0 && box.PityLegend > 0) Assert.That(overlaps, Is.GreaterThan(0), $"{box.Key}: 천장×피티 겹침이 한 번도 없음(표본 부족?)");
            }
        }

        [Test]
        public void TenPullIsTenSinglePullsAndFreshCounterGuaranteesLegendPerTen()
        {
            var d = TestData.Load(); var G = d.Gear; int ten = d.Gacha.TenPullCount; Assert.That(ten, Is.EqualTo(10));
            foreach (var box in d.Gacha.Boxes)
            {
                // ⓐ 같은 시드·같은 상태에서 «10연차(한 스트림으로 10번)» 와 «1회 ×10(같은 스트림을 이어 씀)» 은 같은 결과 — ShopScreen.Pull(n) 은 GachaPull 을 n 번 도는 것뿐이다.
                var a = new List<string>(); var b = new List<string>();
                { var st = new GachaState(); var rng = new Mulberry32(99); for (int i = 0; i < ten; i++) foreach (var g in GearSystem.GachaPull(d, st, box, rng)) a.Add(g.Rar + ":" + g.Type); }
                { var st = new GachaState(); var rng = new Mulberry32(99); for (int i = 0; i < ten; i++) { var one = GearSystem.GachaPull(d, st, box, rng); foreach (var g in one) b.Add(g.Rar + ":" + g.Type); } }
                Assert.That(a, Is.EqualTo(b), box.Key);
                // ⓑ 10연차 묶음(자연 굴림)의 분포도 표와 같다 — 1,000묶음 × 10 = 10,000
                var cnt = new int[box.Rate.Length]; var rng2 = new Mulberry32(1000);
                for (int k = 0; k < GachaSample / ten; k++) for (int i = 0; i < ten; i++) foreach (var g in GearSystem.GachaPull(d, new GachaState(), box, rng2)) cnt[g.Rar]++;
                var pct = new double[cnt.Length]; for (int r = 0; r < cnt.Length; r++) pct[r] = cnt[r] * 100.0 / GachaSample;
                AssertPctMatchesTable(box, pct, "10연차 1,000묶음");
                // ⓒ 피티가 있는 상자는 카운터 0 에서 시작한 10연차마다 전설 이상이 최소 1개(10회째 피티) · 묶음 크기는 10(겹침이면 11)
                if (box.PityLegend == ten)
                {
                    var rng3 = new Mulberry32(3);
                    for (int k = 0; k < 1000; k++)
                    {
                        var st = new GachaState(); int legendPlus = 0, n = 0;
                        for (int i = 0; i < ten; i++) foreach (var g in GearSystem.GachaPull(d, st, box, rng3)) { n++; if (g.Rar >= G.RarLegend) legendPlus++; }
                        Assert.That(legendPlus, Is.GreaterThanOrEqualTo(1), $"{box.Key}: 새 카운터의 10연차 #{k} 에 전설 이상이 0개");
                        Assert.That(n, Is.EqualTo(ten).Or.EqualTo(ten + 1), box.Key);
                    }
                }
            }
        }

        /// <summary>
        /// ShopScreen.Pull 은 원본(index.html `grand = Math.random`)과 달리 뽑을 때마다 `Mulberry32(TickCount ^ 0x5bd1e995)` 를 새로 만든다 —
        /// «연속 시계 시드의 첫 굴림» 이 편향되면 실제 게임의 1회 연타가 표와 어긋난다. 1ms 간격·16ms 간격(Windows 틱) 둘 다 표 ±1.5%p 여야 한다.
        /// </summary>
        [TestCase(1)]
        [TestCase(16)]
        public void GachaClockSeededSinglePullsAreUnbiased(int tickStep)
        {
            var d = TestData.Load(); uint tick0 = 0x12345678u;
            foreach (var box in d.Gacha.Boxes)
            {
                var cnt = new int[box.Rate.Length];
                for (int k = 0; k < GachaSample; k++)
                {
                    var rng = new Mulberry32((tick0 + (uint)(k * tickStep)) ^ 0x5bd1e995u);
                    cnt[GearSystem.GachaPull(d, new GachaState(), box, rng)[0].Rar]++;
                }
                var pct = new double[cnt.Length]; for (int r = 0; r < cnt.Length; r++) pct[r] = cnt[r] * 100.0 / GachaSample;
                AssertPctMatchesTable(box, pct, $"시계 시드 {tickStep}ms 간격 1회 ×10,000");
            }
        }

        [Test]
        public void GachaTypePickIsUniformAcrossAllTypes()
        {
            var d = TestData.Load(); var box = d.Gacha.Box("rare"); var types = d.Gear.AllTypes; int n = types.Count * 2000;
            var cnt = new Dictionary<string, int>(); foreach (var t in types) cnt[t.Part + "|" + t.Type] = 0;
            var rng = new Mulberry32(77);
            for (int i = 0; i < n; i++) foreach (var g in GearSystem.GachaPull(d, new GachaState(), box, rng)) cnt[g.Part + "|" + g.Type]++;
            double expect = 100.0 / types.Count;
            foreach (var kv in cnt) Assert.That(kv.Value * 100.0 / n, Is.EqualTo(expect).Within(GachaTolPct), $"종류 {kv.Key} 관측 ↔ 균등 {expect:0.00}%");
        }
    }

    static class SetExt { public static T First<T>(this HashSet<T> s) { foreach (var x in s) return x; return default; } }
}
