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
    }

    static class SetExt { public static T First<T>(this HashSet<T> s) { foreach (var x in s) return x; return default; } }
}
