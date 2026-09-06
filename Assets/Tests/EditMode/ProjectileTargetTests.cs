using System.Collections.Generic;
using System.Reflection;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T21 — 주인 «투사체(도끼·화살·번개) 표적 = 웨이브 안 무작위». 원본 sim.js `randTarget` = 살아 있는 적 중 «플레이어 앞 −30 ~ +540px» 안에서 <b>무작위 한 명</b>(맨 앞만이 아니다).
    /// 우리 엔진 <c>Battle.RandTarget</c> 이 같은 규칙인지 잠근다(창·검기는 관통형이라 앞에서부터 순서대로 — 원본과 같고 무작위가 아니다).
    /// 시드 골든(BattleTests)이 난수 소비까지 잠그므로 여기서는 분포(맨 앞만 고르지 않음 · 범위 밖은 고르지 않음)만 본다.
    /// </summary>
    public class ProjectileTargetTests
    {
        static RunOptions Ladder() => new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false };

        [Test]
        public void RandTargetPicksAnyEnemyInRangeNotOnlyTheFront()
        {
            var d = TestData.Load(); var rng = new Mulberry32(21); var b = GearSystem.MkBuild(d, -1, 0, 0);
            var G = new BattleState(d, 1, b, rng, new SimPolicy(), Ladder());
            // 첫 웨이브가 사거리(540px) 안에 들어올 때까지 걷는다(멈춤 거리 전 — 아무도 안 죽은 상태)
            int guard = 0;
            while (guard++ < 20000)
            {
                var alive = G.AliveList(); Assert.Greater(alive.Count, 1, "첫 웨이브는 적이 2명 이상");
                double front = alive[0].WorldX - G.P.WorldX;
                if (front < EngineConst.TargetRangeFront - 60) break;
                G.Tick();
            }
            Assert.Less(guard, 20000, "사거리 안에 들어오지 못했다");
            var m = typeof(BattleState).GetMethod("RandTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(m, Is.Not.Null, "Battle.RandTarget");
            var aliveNow = G.AliveList(); var frontE = aliveNow[0];
            var seen = new HashSet<EnemyState>(); int n = 300, nonFront = 0;
            for (int i = 0; i < n; i++)
            {
                var t = (EnemyState)m.Invoke(G, null);
                Assert.That(t, Is.Not.Null, "사거리 안에 적이 있으면 표적이 있다");
                double dd = t.WorldX - G.P.WorldX;
                Assert.That(dd, Is.GreaterThan(EngineConst.TargetRangeBack), "뒤로 30px 넘게 지난 적은 안 고른다(sim.js d > -30)");
                Assert.That(dd, Is.LessThan(EngineConst.TargetRangeFront), "540px 밖은 안 고른다(sim.js d < 540)");
                seen.Add(t); if (t != frontE) nonFront++;
            }
            int inRange = 0; foreach (var e in aliveNow) { double dd = e.WorldX - G.P.WorldX; if (dd > EngineConst.TargetRangeBack && dd < EngineConst.TargetRangeFront) inRange++; }
            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(2), "표적이 한 명(맨 앞)에 몰리지 않는다");
            Assert.That(seen.Count, Is.EqualTo(inRange), "사거리 안 적 전원이 표적 후보다(웨이브 안 무작위)");
            Assert.That(nonFront, Is.GreaterThan(n / 4), "맨 앞이 아닌 적이 충분히 뽑힌다");
        }

        [Test]
        public void TargetRangeConstantsMatchSimJs()
        {
            Assert.That(EngineConst.TargetRangeBack, Is.EqualTo(-30));    // sim.js randTarget: d > -30
            Assert.That(EngineConst.TargetRangeFront, Is.EqualTo(540));   // sim.js randTarget: d < 540
            Assert.That(EngineConst.ProjArriveDx, Is.EqualTo(10));        // 유도형 도달: pr.x >= tgt.worldX - 10
            Assert.That(EngineConst.ProjHitTol, Is.EqualTo(16));          // 관통형 적중: |e.worldX - pr.x| < 16
        }
    }
}
