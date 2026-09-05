using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 전투 엔진 이식 검증. 황금값은 aaaw sim.js 를 같은 시드로 돌려 얻은 실측이다
    /// (SEED 하니스: setSeed(11) → runChapter(...) 첫 판들). 난수 소비 순서가 한 곳이라도 어긋나면 여기서 빨개진다.
    /// </summary>
    public class BattleTests
    {
        static RunOptions Ladder(bool threePick = false) => new RunOptions { LadderPerkMode = !threePick, BaseStatsLegacy20 = true, GearOpts = false };

        [Test]
        public void GoldenRun_Seed11_Chapter3_NoGear_Ladder()
        {
            // node: SEED=11 · mkBuild(-1,0,0) · LADDER_OPTS · 첫 두 판 → clear=false t=83.17 lv=6 tries=73 miss=14 / clear=true t=88.43 tries=83 miss=11
            var d = TestData.Load(); var rng = new Mulberry32(11); var b = GearSystem.MkBuild(d, -1, 0, 0);
            var r0 = new BattleState(d, 3, b, rng, new SimPolicy(), Ladder()).RunToEnd();
            Assert.That(r0.Clear, Is.False); Assert.That(r0.Time, Is.EqualTo(83.17).Within(0.01)); Assert.That(r0.Level, Is.EqualTo(6));
            Assert.That(r0.AtkTries, Is.EqualTo(73)); Assert.That(r0.Miss, Is.EqualTo(14)); Assert.That(r0.Gold, Is.EqualTo(30));
            Assert.That(r0.Taken, Is.EqualTo(new[] { "p_evadeHeal", "p_atk", "p_evade", "p_arrowEv", "p_axeHit", "p_counter" }));
            var r1 = new BattleState(d, 3, b, rng, new SimPolicy(), Ladder()).RunToEnd();
            Assert.That(r1.Clear, Is.True); Assert.That(r1.Time, Is.EqualTo(88.43).Within(0.01)); Assert.That(r1.AtkTries, Is.EqualTo(83)); Assert.That(r1.Miss, Is.EqualTo(11));
        }

        [Test]
        public void GoldenRun_Seed11_Chapter15_Rare_ThreePick()
        {
            // node: SEED=11 · mkBuild(1,0,5) · 3pick · 첫 판 → clear=false t=80.33 lv=7 tries=79 miss=15 · 둘째 판 clear=true t=61.17 tries=71
            var d = TestData.Load(); var rng = new Mulberry32(11); var b = GearSystem.MkBuild(d, 1, 0, 5);
            var r0 = new BattleState(d, 15, b, rng, new SimPolicy(), Ladder(true)).RunToEnd();
            Assert.That(r0.Clear, Is.False); Assert.That(r0.Time, Is.EqualTo(80.33).Within(0.01)); Assert.That(r0.AtkTries, Is.EqualTo(79)); Assert.That(r0.Miss, Is.EqualTo(15));
            Assert.That(r0.Taken, Is.EqualTo(new[] { "p_evade", "p_evadeHeal", "p_stunCritL", "p_critFR", "p_atk", "p_killArrowR", "p_critStack" }));
            var r1 = new BattleState(d, 15, b, rng, new SimPolicy(), Ladder(true)).RunToEnd();
            Assert.That(r1.Clear, Is.True); Assert.That(r1.Time, Is.EqualTo(61.17).Within(0.01)); Assert.That(r1.AtkTries, Is.EqualTo(71));
            Assert.That(r1.Taken[0], Is.EqualTo("p_spearAvatar")); Assert.That(r1.Taken[6], Is.EqualTo("p_berserk"));
        }

        [Test]
        public void GoldenRate_Seed11_Chapter60_Myth_ThreePick_100Runs()
        {
            // node: SEED=11 · mkBuild(3,0,25) · 3pick · 100판 → 82.0%  (판별 특전: 회복 증폭·방어력 증가·가시갑옷·관통 베기 등 전부 걸린다)
            var d = TestData.Load(); var rng = new Mulberry32(11); var b = GearSystem.MkBuild(d, 3, 0, 25);
            int w = 0;
            for (int i = 0; i < 100; i++) if (new BattleState(d, 60, b, rng, new SimPolicy(), Ladder(true)).RunToEnd().Clear) w++;
            Assert.That(w, Is.EqualTo(82));
        }

        [Test]
        public void GoldenRate_Seed11_Chapter3_Ladder_200Runs_IsTenPointFive()
        {
            var d = TestData.Load(); var rng = new Mulberry32(11); var b = GearSystem.MkBuild(d, -1, 0, 0);
            int w = 0;
            for (int i = 0; i < 200; i++) if (new BattleState(d, 3, b, rng, new SimPolicy(), Ladder()).RunToEnd().Clear) w++;
            Assert.That(w, Is.EqualTo(21));   // node: 10.5%
        }

        [Test]
        public void InteractivePolicy_PausesOnRestAndResumesAfterResolve()
        {
            var d = TestData.Load(); var rng = new Mulberry32(5); var b = GearSystem.MkBuild(d, 3, 9, 100);
            var G = new BattleState(d, 1, b, rng, new InteractivePolicy(), new RunOptions { EmitEvents = true });
            int guard = 0;
            while (G.Pending == null && !G.Over && guard++ < 100000) G.Tick();
            Assert.That(G.Pending, Is.Not.Null, "이벤트나 레벨업에서 멈춰야 한다");
            double t = G.T;
            Assert.That(G.Tick(), Is.False, "Pending 중에는 시간이 흐르지 않는다");
            Assert.That(G.T, Is.EqualTo(t));
            var kind = G.Pending.Kind;
            switch (kind)
            {
                case PendingKind.Rest: G.ResolveRest(false); break;
                case PendingKind.Devil: Assert.That(G.Pending.DevilPerk.Grade, Is.EqualTo(d.Perks.DevilGrade)); G.ResolveDevil(true); break;
                case PendingKind.Angel: G.ResolveAngel(SimPolicy.AngelFree); break;
                case PendingKind.LevelUp:
                    Assert.That(G.Pending.Offer.Count, Is.InRange(1, d.Perks.OfferPerLevel));
                    foreach (var p in G.Pending.Offer) Assert.That(p.Grade, Is.EqualTo(G.Pending.Offer[0].Grade), "3장은 같은 등급");
                    G.ResolveLevelUp(G.Pending.Offer[0]); break;
            }
            if (G.Pending == null) Assert.That(G.Tick(), Is.True);
        }

        [Test]
        public void InteractiveRun_FinishesWhenAllDecisionsAnswered()
        {
            var d = TestData.Load(); var rng = new Mulberry32(3); var b = GearSystem.MkBuild(d, 3, 9, 100);
            var G = new BattleState(d, 2, b, rng, new InteractivePolicy(), new RunOptions());
            int guard = 0; int levelUps = 0;
            while (!G.Over && G.AliveList().Count > 0 && guard++ < 200000)
            {
                if (G.Pending != null)
                {
                    switch (G.Pending.Kind)
                    {
                        case PendingKind.Rest: G.ResolveRest(true); break;
                        case PendingKind.Devil: G.ResolveDevil(false); break;
                        case PendingKind.Angel: G.ResolveAngel(SimPolicy.AngelAd); break;
                        case PendingKind.LevelUp: levelUps++; G.ResolveLevelUp(G.Pending.Offer[G.Pending.Offer.Count - 1]); break;
                    }
                    continue;
                }
                G.Tick();
            }
            Assert.That(G.Cleared, Is.True, "신화+9강·슬롯100 은 챕터 2 를 반드시 깬다");
            Assert.That(levelUps, Is.GreaterThan(0));
            Assert.That(G.Taken.Count, Is.EqualTo(levelUps));
            Assert.That(G.Blessings.Count, Is.EqualTo(1));
        }

        [Test]
        public void DevilTakesThirtyPercentOfMaxHpFromMax()
        {
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, 1, 0, 0);
            var G = new BattleState(d, 1, b, new Mulberry32(1), new SimPolicy(), new RunOptions());
            double max = G.P.MaxHp; G.P.Hp = max;
            G.PayDevilCost();
            Assert.That(G.P.MaxHp, Is.EqualTo(max * (1 - d.Perks.DevilCostMaxHp)).Within(1e-9));
            Assert.That(G.P.Hp, Is.EqualTo(G.P.MaxHp));
        }

        [Test]
        public void PerkApply_MultiplicativeAndAdditiveAxes()
        {
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, 1, 0, 0);
            var G = new BattleState(d, 1, b, new Mulberry32(1), new SimPolicy(), new RunOptions { BaseStatsLegacy20 = true });
            double dmg0 = G.P.Dmg, def0 = G.P.Def;
            G.PickPerk(d.Perks.ById("p_atk")); Assert.That(G.P.Dmg, Is.EqualTo(dmg0 * d.Perks.C("PERK_ATK_M")).Within(1e-9));
            G.PickPerk(d.Perks.ById("p_def")); Assert.That(G.P.Def, Is.EqualTo(def0 * d.Perks.C("PERK_DEF_M")).Within(1e-9));
            G.PickPerk(d.Perks.ById("p_healUp")); Assert.That(G.P.HealAmp, Is.EqualTo(d.Perks.C("PERK_AMP")));
            G.PickPerk(d.Perks.ById("p_evade")); Assert.That(G.P.Evade, Is.EqualTo(20 + d.Perks.C("PERK_EVADE_A")));
            G.PickPerk(d.Perks.ById("p_killSpearN")); Assert.That(G.P.PxGet("p_killSpear"), Is.EqualTo(d.Perks.C("PERK_KILL_N")));
            G.PickPerk(d.Perks.ById("p_killSpearL")); Assert.That(G.P.PxGet("p_killSpear"), Is.EqualTo(d.Perks.C("PERK_KILL_L")), "처치 소환은 최댓값 갱신");
            G.PickPerk(d.Perks.ById("p_thornsN")); G.PickPerk(d.Perks.ById("p_thornsR"));
            Assert.That(G.P.PxGet("p_thorns"), Is.EqualTo(d.Perks.C("PERK_THORN_N") + d.Perks.C("PERK_THORN_R")), "가시갑옷은 가산");
            G.PickPerk(d.Perks.ById("p_berserk")); Assert.That(G.EffCritR(), Is.EqualTo(0), "광전사 = 치확 0 고정");
        }

        [Test]
        public void OfferPerks_SameGradeAndNoDuplicates()
        {
            var d = TestData.Load(); var rng = new Mulberry32(9); var taken = new List<PerkDef>();
            for (int i = 0; i < 200; i++)
            {
                var o = Perks.Offer(d, taken, false, rng);
                Assert.That(o.Count, Is.InRange(1, 3));
                for (int a = 0; a < o.Count; a++) { Assert.That(o[a].Grade, Is.EqualTo(o[0].Grade)); for (int c = a + 1; c < o.Count; c++) Assert.That(o[a], Is.Not.SameAs(o[c])); Assert.That(taken, Does.Not.Contain(o[a])); }
            }
            // 귀족의 눈: 일반 제외
            for (int i = 0; i < 100; i++) foreach (var p in Perks.Offer(d, taken, true, rng)) Assert.That(p.Grade, Is.GreaterThan(0));
            // 전설을 다 얻으면 악마는 null
            foreach (var p in d.Perks.Perks) if (p.Grade == d.Perks.DevilGrade) taken.Add(p);
            Assert.That(Perks.OfferDevil(d, taken, rng), Is.Null);
        }
    }
}
