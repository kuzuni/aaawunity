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
    }

    static class SetExt { public static T First<T>(this HashSet<T> s) { foreach (var x in s) return x; return default; } }
}
