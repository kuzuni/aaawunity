using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>T23 — 쉼터 «광고 보고 둘 다 얻기» = <see cref="BattleState.ResolveRestBoth"/> (회복 + 경험치). 대화형 전용 — SimPolicy 는 안 고르므로 시드 골든(BattleTests)은 그대로다.</summary>
    public class RestBothTests
    {
        static RunOptions Ladder() => new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false };

        [Test]
        public void ResolveRestBothHealsAndGivesExp()
        {
            var d = TestData.Load(); var G = new BattleState(d, 1, GearSystem.MkBuild(d, -1, 0, 0), new Mulberry32(23), new SimPolicy(), Ladder());
            var P = G.P; P.Hp = P.MaxHp * 0.5; int exp0 = P.Exp, lv0 = P.Level; double hp0 = P.Hp;
            G.Pending = new PendingDecision { Kind = PendingKind.Rest };
            G.ResolveRestBoth();
            Assert.That(G.Pending == null || G.Pending.Kind == PendingKind.LevelUp, "쉼터 보류가 풀린다(레벨업이 이어질 수는 있다)");
            double healed = System.Math.Min(P.MaxHp, hp0 + G.C.RestHeal * (1 + P.HealAmp));
            Assert.That(P.Hp, Is.EqualTo(healed).Within(1e-6), "체력 회복 = ResolveRest(true) 와 같은 양");
            bool leveled = P.Level > lv0;
            Assert.That(leveled || P.Exp == exp0 + G.C.RestExp, Is.True, "경험치 = ResolveRest(false) 와 같은 양(레벨업으로 소비됐으면 레벨이 올랐다)");
        }

        [Test]
        public void ResolveRestBothIgnoredWithoutRestPending()
        {
            var d = TestData.Load(); var G = new BattleState(d, 1, GearSystem.MkBuild(d, -1, 0, 0), new Mulberry32(23), new SimPolicy(), Ladder());
            var P = G.P; P.Hp = P.MaxHp * 0.5; int exp0 = P.Exp; double hp0 = P.Hp;
            G.ResolveRestBoth();
            Assert.That(P.Hp, Is.EqualTo(hp0)); Assert.That(P.Exp, Is.EqualTo(exp0));
            G.Pending = new PendingDecision { Kind = PendingKind.Angel };
            G.ResolveRestBoth();
            Assert.That(P.Hp, Is.EqualTo(hp0)); Assert.That(P.Exp, Is.EqualTo(exp0)); Assert.That(G.Pending.Kind, Is.EqualTo(PendingKind.Angel), "다른 보류는 건드리지 않는다");
        }
    }
}
