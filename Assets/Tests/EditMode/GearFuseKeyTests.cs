using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T114 — 합성 조건 완화(주인 2026-09-07 «대장간 합성할 때 전설 전까지는 같은 부위 장비면 합성 가능하게 · 개수만 맞으면 되게»):
    /// 재료 등급이 <b>전설 미만</b>(일반·희귀)이면 묶음 키에서 <b>종류</b>를 뺀다(부위·등급만) · <b>전설 이상</b>은 종전대로 부위·종류·등급.
    /// 개수(3개)·부위 규칙은 그대로다. <b>⚑ aaaw sim.js 의 fuseAll 과 달라지는 지점</b>이라 그 경계를 여기서 못 박는다.
    /// </summary>
    public class GearFuseKeyTests
    {
        static GearItem Mk(string part, string type, int rar, int plus = 0, int uid = 0)
            => new GearItem { Uid = uid, Part = part, Type = type, Rar = rar, Plus = plus };

        [Test]
        public void CommonAndRareIgnoreTypeButLegendDoesNot()
        {
            var d = TestData.Load(); var G = d.Gear;
            // ⓐ 일반(0) — 같은 부위·등급이면 종류가 달라도 같은 묶음
            Assert.That(GearSystem.FuseKey(d, Mk("helm", "crit_helm", 0)), Is.EqualTo(GearSystem.FuseKey(d, Mk("helm", "hpsh_helm", 0))), "일반은 종류 무관");
            // ⓑ 희귀(1) 도 같다
            Assert.That(GearSystem.FuseKey(d, Mk("helm", "crit_helm", 1)), Is.EqualTo(GearSystem.FuseKey(d, Mk("helm", "evade_helm", 1))), "희귀도 종류 무관");
            // ⓒ 전설(RarLegend) 이상은 종류가 다르면 다른 묶음
            Assert.That(GearSystem.FuseKey(d, Mk("helm", "crit_helm", G.RarLegend)), Is.Not.EqualTo(GearSystem.FuseKey(d, Mk("helm", "hpsh_helm", G.RarLegend))), "전설은 종류까지 같아야 한다");
            Assert.That(GearSystem.FuseKey(d, Mk("helm", "crit_helm", G.RarMyth)), Is.Not.EqualTo(GearSystem.FuseKey(d, Mk("helm", "hpsh_helm", G.RarMyth))), "신화도 종전대로");
            // ⓓ 부위가 다르면 등급이 같아도 다른 묶음
            Assert.That(GearSystem.FuseKey(d, Mk("helm", "crit_helm", 0)), Is.Not.EqualTo(GearSystem.FuseKey(d, Mk("armor", "crit_armor", 0))), "부위는 언제나 같아야 한다");
            // 등급이 다르면 당연히 다른 묶음
            Assert.That(GearSystem.FuseKey(d, Mk("helm", "crit_helm", 0)), Is.Not.EqualTo(GearSystem.FuseKey(d, Mk("helm", "crit_helm", 1))), "등급은 언제나 같아야 한다");
            // 데이터가 없으면(부팅 전) 종전 규칙 — 완화가 기본값이 되지 않게
            Assert.That(GearSystem.FuseKey(null, Mk("helm", "crit_helm", 0)), Is.Not.EqualTo(GearSystem.FuseKey(null, Mk("helm", "hpsh_helm", 0))), "데이터 없이는 종전 규칙");
        }

        [Test]
        public void FuseAllMixesTypesBelowLegend()
        {
            var d = TestData.Load();
            // ⓐ 일반 투구 세 종류 → 한 번 합성돼 희귀 투구 하나가 남는다
            var inv = new List<GearItem> { Mk("helm", "crit_helm", 0, 0, 1), Mk("helm", "hpsh_helm", 0, 0, 2), Mk("helm", "evade_helm", 0, 0, 3) };
            int n = GearSystem.FuseAll(d, inv, null);
            Assert.That(n, Is.EqualTo(1), "종류가 달라도 일반 3개는 합성된다");
            Assert.That(inv.Count, Is.EqualTo(1));
            Assert.That(inv[0].Rar, Is.EqualTo(1), "산출물은 한 등급 위");
            Assert.That(inv[0].Part, Is.EqualTo("helm"), "부위는 그대로");
        }

        [Test]
        public void FuseAllStillNeedsSameTypeAtLegendAndSamePartAlways()
        {
            var d = TestData.Load(); var G = d.Gear;
            // ⓒ 전설 세 종류는 안 된다
            var leg = new List<GearItem> { Mk("helm", "crit_helm", G.RarLegend, 0, 1), Mk("helm", "hpsh_helm", G.RarLegend, 0, 2), Mk("helm", "evade_helm", G.RarLegend, 0, 3) };
            Assert.That(GearSystem.FuseAll(d, leg, null), Is.EqualTo(0), "전설은 종류가 달라 안 묶인다");
            Assert.That(leg.Count, Is.EqualTo(3), "재료가 그대로 남는다");
            // ⓓ 부위가 다르면 등급이 같아도 안 된다
            var parts = new List<GearItem> { Mk("helm", "crit_helm", 0, 0, 1), Mk("armor", "crit_armor", 0, 0, 2), Mk("boot", "crit_boot", 0, 0, 3) };
            Assert.That(GearSystem.FuseAll(d, parts, null), Is.EqualTo(0), "부위가 다르면 안 된다");
            // ⓔ 개수 규칙 불변 — 2개면 안 되고 3개부터
            var two = new List<GearItem> { Mk("helm", "crit_helm", 0, 0, 1), Mk("helm", "hpsh_helm", 0, 0, 2) };
            Assert.That(GearSystem.FuseAll(d, two, null), Is.EqualTo(0), "2개면 안 된다");
            two.Add(Mk("helm", "evade_helm", 0, 0, 3));
            Assert.That(GearSystem.FuseAll(d, two, null), Is.EqualTo(1), "3개부터 된다");
        }

        [Test]
        public void OutputTypeFollowsTheMostUpgradedMaterial()
        {
            var d = TestData.Load();
            // 산출물의 종류 = base(강화가 가장 높은 재료)의 종류 — 동률이면 목록에서 먼저 나온 것(현행 정렬 유지)
            var inv = new List<GearItem> { Mk("helm", "crit_helm", 0, 0, 1), Mk("helm", "hpsh_helm", 0, 2, 2), Mk("helm", "evade_helm", 0, 1, 3) };
            Assert.That(GearSystem.FuseAll(d, inv, null), Is.EqualTo(1));
            Assert.That(inv[0].Type, Is.EqualTo("hpsh_helm"), "가장 많이 강화된 재료(+2)의 종류를 따른다");

            var tie = new List<GearItem> { Mk("helm", "evade_helm", 0, 0, 1), Mk("helm", "crit_helm", 0, 0, 2), Mk("helm", "hpsh_helm", 0, 0, 3) };
            Assert.That(GearSystem.FuseAll(d, tie, null), Is.EqualTo(1));
            Assert.That(tie[0].Type, Is.EqualTo("evade_helm"), "동률이면 먼저 나온 것");
        }
    }
}
