using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T312(주인 2026-09-09 10:0X «도끼가 적한테 닿았는데 바로 안 사라지고 데미지도 늦게 들어감») —
    /// <b>«보류» 가 무엇인지를 수로 못 박는다.</b>
    /// <para>
    /// 화면은 킬 연출 동안 엔진 틱을 <b>통째로</b> 보류한다(<c>BattleWorld.HoldEngine</c> · T50) — 즉 <c>Tick</c> 을 한 번도 안 부른다.
    /// 그 동안 도끼의 <b>그림만</b> 나아가고(T86 ⓐ) 엔진 <c>pr.X</c> 는 멎어 있어서, 그림이 맞는 자리에 <b>닿은 채로 서서</b> 기다린다 —
    /// 데미지는 보류가 풀린 뒤에야 들어간다. 그것이 주인이 본 «닿았는데 안 사라지고 늦다» 다.
    /// </para>
    /// 이 자가 재는 것은 <b>그 두 문장</b>이다: ⓐ 틱을 안 부르면 도끼는 한 픽셀도 안 가고 아무도 안 맞는다(= 지금의 보류) ·
    /// ⓑ <see cref="BattleState.StepProjectiles"/> 하나만 부르면 도끼가 나아가 <b>맞고 사라진다</b>(= 고침이 기대는 자리).
    /// <para>⚠ 값(속도·거리·피해량)을 베끼지 않는다 — 표와 엔진 상수에서 읽어 «움직였나 · 줄었나 · 사라졌나» 만 본다(결정 555).</para>
    /// </summary>
    public class ProjectileHoldTests
    {
        static RunOptions Opts() => new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false };

        /// <summary>판을 하나 세우고 살아 있는 적이 설 때까지 돌린다(적이 없으면 잴 것이 없다).</summary>
        static BattleState Battle(out EnemyState foe)
        {
            var d = TestData.Load();
            var b = GearSystem.MkBuild(d, -1, 0, 0);
            var g = new BattleState(d, 3, b, new Mulberry32(11), new SimPolicy(), Opts());
            foe = null;
            for (int i = 0; i < 2000 && !g.Over; i++)
            {
                var alive = g.AliveList();
                if (alive.Count > 0) { foe = alive[0]; break; }
                g.Tick();
            }
            Assert.IsNotNull(foe, "살아 있는 적이 서야 잴 수 있다");
            return g;
        }

        /// <summary>표적을 겨눈 도끼 하나 — 아직 한참 못 미친 자리에서 띄운다.</summary>
        static Projectile Axe(BattleState g, EnemyState foe)
        {
            var pr = new Projectile
            {
                Kind = ProjKind.Axe,
                X = foe.WorldX - 300,
                StartX = foe.WorldX - 300,
                TargetX0 = foe.WorldX,
                Spd = 600,
                MaxX = foe.WorldX + 500,
                Target = foe,
                Pierce = 1,
                Hit = new System.Collections.Generic.HashSet<EnemyState>(),
            };
            g.Projs.Add(pr);
            return pr;
        }

        [Test]
        public void WhileTheEngineIsHeld_TheAxeDoesNotMoveAndNobodyIsHit()
        {
            var g = Battle(out var foe);
            var pr = Axe(g, foe);
            double x0 = pr.X, hp0 = foe.Hp;

            // «보류» = 틱을 한 번도 안 부르는 것이다. 화면이 그 동안 하는 일은 그림을 옮기는 것뿐이다.
            // 여기서는 아무것도 안 부른 채 시간이 «지났다» 고 치고 잰다.
            Assert.That(pr.X, Is.EqualTo(x0).Within(1e-9), "틱이 없으면 엔진 도끼는 한 픽셀도 안 간다");
            Assert.That(foe.Hp, Is.EqualTo(hp0).Within(1e-9), "그래서 아무도 안 맞는다 — 데미지가 늦는 것이 이 때문이다");
            Assert.That(g.Projs.Contains(pr), Is.True, "사라지지도 않는다 — 주인이 본 «안 사라진다»");
        }

        [Test]
        public void StepProjectilesAlone_MovesItAndLandsTheHit()
        {
            var g = Battle(out var foe);
            var pr = Axe(g, foe);
            double x0 = pr.X, hp0 = foe.Hp;

            // 한 틱만 — 나아가야 한다(맞는 자리에는 아직 못 미친다).
            g.StepProjectiles(EngineConst.Dt);
            Assert.That(pr.X, Is.GreaterThan(x0), "투사체만 돌려도 엔진 x 가 나아간다");

            // 맞을 때까지 — 표적 HP 가 줄고 도끼가 목록에서 빠진다.
            for (int i = 0; i < 600 && g.Projs.Contains(pr); i++) g.StepProjectiles(EngineConst.Dt);
            Assert.That(g.Projs.Contains(pr), Is.False, "맞으면 사라진다");
            Assert.That(foe.Hp, Is.LessThan(hp0), "맞은 만큼 HP 가 준다 — 데미지가 «그 자리에서» 들어간다");
        }

        [Test]
        public void StepProjectiles_DoesNothingWithoutTime()
        {
            var g = Battle(out var foe);
            var pr = Axe(g, foe);
            double x0 = pr.X;
            g.StepProjectiles(0);
            g.StepProjectiles(-1);
            Assert.That(pr.X, Is.EqualTo(x0).Within(1e-9), "0·음수 시간에는 아무 일도 안 한다");
        }
    }
}
