using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T516 — <b>턴제(라운드) 규칙</b>을 실제 판을 돌려 가며 잰다(주인 2026-09-13 «싸움을 턴제 게임으로 바꿀래 ·
    /// 1웨이브당 15라운드 안에 끝나야 클리어 · 15라운드 넘으면 지는 거임 · 내가 먼저 떄리고 적이 떄리고 ·
    /// 스킬들 쿨타임으로 안 하고 3라운드당 한 번 발동 · 1웨이브당 1마리로 뜨게»).
    /// <para>
    /// ⚑ <b>수를 여기 안 박는다</b> — 15·3·1 은 전부 <c>combatOverride.json</c> 의 <c>turn</c> 칸에서 온다(<see cref="CombatData.TurnRounds"/> …).
    /// 주인이 «12라운드로 줄여» 하면 표 한 칸만 바뀌고 이 자들은 그대로 돈다.
    /// </para>
    /// <para>
    /// ⚠ <b>옛 실시간 규칙을 재는 자는 따로 있다</b>(<c>BattleTests</c> 시드 골든 · <c>ProjectileTargetTests</c> …) —
    /// 그쪽은 <see cref="TestData.PreBalance"/>·<see cref="TestData.RealTime"/> 로 턴제를 끄고 돈다. 두 규칙이 한 엔진에 같이 산다.
    /// </para>
    /// </summary>
    public class TurnBattleTests
    {
        static RunOptions Opt() => new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false };
        static BattleState Run(int chapter, int rare, int plus, int lv, uint seed = 11)
        {
            var d = TestData.Load();
            return new BattleState(d, chapter, GearSystem.MkBuild(d, rare, plus, lv), new Mulberry32(seed), new SimPolicy(), Opt());
        }
        /// <summary>적과 마주 설 때까지(= 라운드가 붙을 때까지) 걷는다.</summary>
        static bool WalkToFoe(BattleState g, int maxTicks = 4000)
        {
            for (int i = 0; i < maxTicks && !g.Over; i++) { g.Tick(); if (g.TurnFoe != null) return true; }
            return false;
        }

        [Test]
        public void TheTableIsTheOwnersTurnRules()
        {
            var d = TestData.Load();
            Assert.That(d.Combat.TurnOn, Is.True, "게임 표는 턴제로 켜져 있다(combatOverride.json turn.on)");
            Assert.That(d.Combat.TurnRounds, Is.EqualTo(15), "주인 «1웨이브당 15라운드»");
            Assert.That(d.Combat.TurnSkillEvery, Is.EqualTo(3), "주인 «3라운드당 한 번»");
            Assert.That(d.Combat.TurnEnemiesPerWave, Is.EqualTo(1), "주인 «1웨이브당 1마리»");
            Assert.That(d.Combat.TurnStepSec, Is.GreaterThan(0), "반턴 간격이 0 이면 한 프레임에 판이 끝난다");
        }

        [Test]
        public void EveryWaveStandsExactlyOneEnemy()
        {
            var g = Run(10, 1, 0, 6);
            int waves = 0;
            foreach (var n in g.Nodes)
            {
                if (n.Type != NodeType.Wave) continue;
                waves++;
                Assert.That(n.Enemies.Count, Is.EqualTo(g.D.Combat.TurnEnemiesPerWave), "웨이브 하나에 적 하나(주인 지시)");
            }
            Assert.That(waves, Is.GreaterThan(1), "이 챕터에 웨이브가 여럿이어야 시험이 성립한다");
            Assert.That(g.TotalEnemies, Is.EqualTo(waves + BossCount(g)), "진행도가 세는 적 수도 «세운 만큼» 이다");
        }
        static int BossCount(BattleState g) { int n = 0; foreach (var nd in g.Nodes) if (nd.Type == NodeType.Boss) n += nd.Enemies.Count; return n; }

        [Test]
        public void RoundsStartWhenTheyMeetAndThePlayerSwingsFirst()
        {
            var g = Run(1, -1, 0, 0);
            Assert.That(g.Round, Is.EqualTo(0), "걷는 동안에는 라운드가 안 센다");
            Assert.That(WalkToFoe(g), Is.True, "적에게 걸어가 마주 서야 한다");
            Assert.That(g.Round, Is.EqualTo(1), "마주 서면 1라운드부터");
            Assert.That(g.PlayerTurn, Is.True, "주인 «내가 먼저 떄리고» — 첫 반턴은 내 것이다");

            // 둘 다 «안 죽는 몸» 으로 세운다 — 재려는 것은 «누가 언제 때리나» 이지 누가 이기나가 아니다
            //   (안 그러면 한 방에 잡히거나 맞아 죽어서 반턴 한 쪽만 보고 끝난다).
            var foe = g.TurnFoe; foe.MaxHp = foe.Hp = 1e12;
            g.P.MaxHp = g.P.Hp = 1e12; g.P.Sh = 0;

            double foeHp = foe.Hp, myHp = g.P.Hp;
            int guard = 0;
            while (g.PlayerTurn && !g.Over && guard++ < 500) g.Tick();          // 내 반턴이 지나갈 때까지
            Assert.That(g.PlayerTurn, Is.False, "내 반턴이 지나면 적 차례다");
            while (!g.PlayerTurn && !g.Over && guard++ < 500) g.Tick();          // 적 반턴이 지나갈 때까지
            Assert.That(g.Round, Is.EqualTo(2), "둘이 한 번씩 때리면 라운드가 하나 오른다");
            // 빗맞을 수 있으므로 «세 라운드 안에 서로 한 대씩은 들어간다» 로 잰다(치명·회피는 지금 규칙 그대로 산다).
            while (g.Round <= 4 && !g.Over && guard++ < 5000) g.Tick();
            Assert.That(foe.Hp, Is.LessThan(foeHp), "내 차례에 적이 맞는다");
            Assert.That(g.P.Hp, Is.LessThan(myHp), "적 차례에 내가 맞는다");
        }

        [Test]
        public void FifteenRoundsIsTheBudgetAndOverrunningItLosesTheRun()
        {
            var g = Run(1, -1, 0, 0);
            Assert.That(WalkToFoe(g), Is.True, "적과 마주 선다");
            int limit = g.RoundLimit;
            Assert.That(limit, Is.EqualTo(15), "표가 준 라운드 예산");
            // «못 잡는 적 · 안 죽는 나» — 그래야 재려는 것(라운드 예산)만 남는다.
            g.TurnFoe.MaxHp = g.TurnFoe.Hp = 1e12;
            g.P.MaxHp = g.P.Hp = 1e12; g.P.Sh = 1e12;

            int guard = 0;
            while (!g.Over && guard++ < 200000) g.Tick();
            Assert.That(g.Over, Is.True, "판이 끝나야 한다");
            Assert.That(g.Dead, Is.True, "라운드를 넘기면 진다(주인 «15라운드 넘으면 지는 거임»)");
            Assert.That(g.LostByRounds, Is.True, "«맞아 죽은 것» 이 아니라 «라운드 초과» 로 갈려 적힌다");
            Assert.That(g.Round, Is.EqualTo(limit + 1), "예산을 한 라운드 넘긴 그 순간에 끝난다");
            Assert.That(g.P.Hp, Is.GreaterThan(0), "체력이 남아 있어도 진다 — 진 까닭이 «맞아서» 가 아니다");
        }

        [Test]
        public void KillingInsideTheBudgetMovesOnAndTheNextWaveCountsFromOneAgain()
        {
            var g = Run(1, 3, 9, 60);        // 한 방에 잡을 만큼 센 몸
            Assert.That(WalkToFoe(g), Is.True, "첫 적과 마주 선다");
            int firstRound = g.Round;
            for (int i = 0; i < 20000 && g.Kills == 0 && !g.Over; i++) g.Tick();
            Assert.That(g.Kills, Is.GreaterThan(0), "예산 안에 잡는다");
            Assert.That(firstRound, Is.LessThanOrEqualTo(g.RoundLimit));
            // 다음 웨이브 — 다시 1라운드부터
            for (int i = 0; i < 20000 && !g.Over; i++) { g.Tick(); if (g.TurnFoe != null && g.Kills > 0 && g.Round == 1) break; }
            Assert.That(g.Round, Is.LessThanOrEqualTo(1).Or.EqualTo(1), "새 웨이브는 1라운드부터 센다");
            Assert.That(g.LostByRounds, Is.False, "잡고 넘어간 판은 라운드 초과가 아니다");
        }

        [Test]
        public void SkillsFireEveryThirdRoundInsteadOfCountingHits()
        {
            var d = TestData.Load();
            // 투사체를 내는 «N타마다» 특전 하나 — 턴제에서는 그것이 «3라운드마다» 가 된다.
            string nHit = null;
            foreach (var kv in d.Perks.NHitPerks) if (kv.Key.StartsWith("p_nAxe") || kv.Key.StartsWith("p_nArrow") || kv.Key.StartsWith("p_nBolt")) { nHit = kv.Key; break; }
            Assert.That(nHit, Is.Not.Null, "표에 투사체를 내는 «N타마다» 특전이 있어야 이 시험이 성립한다");

            var g = new BattleState(d, 1, GearSystem.MkBuild(d, -1, 0, 0), new Mulberry32(7), new SimPolicy(), Opt());
            g.P.Px[nHit] = 1;                 // 특전 보유 = Px 한 칸(엔진의 P.Has 가 그것을 본다)
            Assert.That(WalkToFoe(g), Is.True, "적과 마주 선다");
            g.TurnFoe.MaxHp = g.TurnFoe.Hp = 1e12; g.P.MaxHp = g.P.Hp = 1e12; g.P.Sh = 1e12;   // 둘 다 안 죽는다

            var firedRounds = new HashSet<int>();
            int guard = 0;
            while (!g.Over && g.Round <= 10 && guard++ < 20000)
            {
                int r = g.Round;
                g.Tick();
                if (g.Projs.Count > 0) firedRounds.Add(r);
            }
            Assert.That(g.P.NHit.TryGetValue(nHit, out var hitCount) ? hitCount : 0, Is.EqualTo(0), "«몇 대 때렸나» 셈은 턴제에서 안 쓴다");
            Assert.That(firedRounds.Count, Is.GreaterThan(0), "스킬이 터져야 한다");
            foreach (var r in firedRounds)
                Assert.That(r % d.Combat.TurnSkillEvery, Is.EqualTo(0), "스킬은 " + d.Combat.TurnSkillEvery + "라운드마다만 터진다 — 터진 라운드 " + r);
            Assert.That(firedRounds.Contains(3), Is.True, "3라운드에 터진다");
            Assert.That(firedRounds.Contains(6), Is.True, "6라운드에도 터진다");
        }

        [Test]
        public void TurningItOffPutsTheOldRealTimeRulesBack()
        {
            // 이 한 줄이 시드 골든(T2)의 안전장치다 — 표를 끄면 엔진이 옛 길로 간다.
            var d = TestData.RealTime();
            Assert.That(d.Combat.TurnOn, Is.False);
            var g = new BattleState(d, 1, GearSystem.MkBuild(d, -1, 0, 0), new Mulberry32(11), new SimPolicy(), Opt());
            foreach (var n in g.Nodes) if (n.Type == NodeType.Wave) { Assert.That(n.Enemies.Count, Is.GreaterThan(1), "옛 규칙은 웨이브에 여럿을 세운다"); break; }
            for (int i = 0; i < 500; i++) g.Tick();
            Assert.That(g.Round, Is.EqualTo(0), "옛 규칙에는 라운드가 없다");
            Assert.That(g.TurnFoe, Is.Null);
        }
    }
}
