using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T517 — <b>굴리는 옵션 아홉 축</b>(주인 2026-09-13 «장비,펫,탈것 옵션은 … 이런식으로 바꿔줘 기존꺼 버리고»).
    /// <para>
    /// ⚑ <b>수를 여기 안 박는다</b> — 아홉 축의 이름·범위는 전부 <c>gearOverride.json</c> 의 <c>optionAxes</c> 에서 온다.
    /// 주인이 «치명타 피해를 1~80 으로» 하면 표 한 칸이 바뀌고 이 자들은 그대로 돈다.
    /// </para>
    /// <para>
    /// ⚠ <b>옛 «종류마다 7줄» 을 재는 자는 따로 있다</b>(시드 골든 · <c>Sim</c>) — 그쪽은 <see cref="RunOptions.GearOpts"/> 가 꺼져 있어 흔들리지 않는다.
    /// </para>
    /// </summary>
    public class GearRolledOptionTests
    {
        static GameData D() => TestData.Load();

        [Test]
        public void TheTableCarriesTheOwnersNineAxes()
        {
            var d = D();
            Assert.That(d.Gear.RolledOpts, Is.True, "표에 굴리는 축이 있어야 새 규칙으로 돈다");
            Assert.That(d.Gear.OptAxes.Count, Is.EqualTo(9), "주인이 준 축은 아홉이다");
            // 주인 원문의 범위를 그대로 — 이 자가 «표가 주인 말과 같은가» 를 지킨다.
            var want = new Dictionary<string, (double lo, double hi)>
            {
                { "atkP", (1, 10) }, { "critR", (1, 8) }, { "critF", (1, 50) }, { "hpP", (1, 10) },
                { "dbl", (1, 10) }, { "skillDmg", (1, 10) }, { "steal", (1, 10) }, { "evade", (1, 3) }, { "regen", (1, 3) },
            };
            foreach (var kv in want)
            {
                var ax = d.Gear.Axis(kv.Key);
                Assert.That(ax, Is.Not.Null, "표에 없는 축 " + kv.Key);
                Assert.That(ax.Min, Is.EqualTo(kv.Value.lo), kv.Key + " 아래끝");
                Assert.That(ax.Max, Is.EqualTo(kv.Value.hi), kv.Key + " 위끝");
                Assert.That(ax.Name, Is.Not.Null.And.Not.Empty, kv.Key + " 이름이 비었다(화면에 그 줄이 빈칸으로 나간다)");
            }
        }

        [Test]
        public void EveryRolledValueSitsInsideItsAxisRange()
        {
            var d = D();
            for (int uid = 1; uid <= 500; uid++)
                foreach (var o in GearSystem.Roll(d, uid))
                {
                    var ax = d.Gear.Axis(o.Key);
                    Assert.That(ax, Is.Not.Null, "굴림이 표에 없는 축을 냈다: " + o.Key);
                    Assert.That(o.Val, Is.GreaterThanOrEqualTo(ax.Min).And.LessThanOrEqualTo(ax.Max), ax.Key + " 이 범위를 벗어났다 — " + o.Val);
                    Assert.That(o.Val, Is.EqualTo(System.Math.Floor(o.Val)), "값은 정수다 — " + ax.Key + " " + o.Val);
                }
        }

        [Test]
        public void OneItemNeverRollsTheSameAxisTwice()
        {
            var d = D();
            for (int uid = 1; uid <= 500; uid++)
            {
                var seen = new HashSet<string>();
                foreach (var o in GearSystem.Roll(d, uid))
                    Assert.That(seen.Add(o.Key), Is.True, "한 자루에 같은 축이 두 줄 — uid " + uid + " · " + o.Key);
            }
        }

        [Test]
        public void ItRollsAsManyLinesAsTheTableCanEverOpen()
        {
            var d = D();
            // T515 가 상한을 2 로 낮췄다. 굴리는 줄 수는 «등급별 줄 수» 가 아니라 «열릴 수 있는 최대» 다 —
            //   그래야 강화로 한 줄이 더 열리는 날 «이미 굴려 둔 값이 드러날 뿐» 이고 새 값이 그때 생기지 않는다.
            Assert.That(GearSystem.Roll(d, 7).Count, Is.EqualTo(d.Gear.OptCountOpenMax));
        }

        [Test]
        public void TheSameItemRollsTheSameValuesForever()
        {
            var d = D();
            var a = GearSystem.Roll(d, 4242);
            var b = GearSystem.Roll(d, 4242);
            Assert.That(a.Count, Is.EqualTo(b.Count));
            for (int i = 0; i < a.Count; i++) { Assert.That(a[i].Key, Is.EqualTo(b[i].Key)); Assert.That(a[i].Val, Is.EqualTo(b[i].Val)); }
            // 그리고 이웃한 번호는 서로 달라야 한다 — 연속으로 뽑은 열 자루가 같은 값이면 «굴린다» 가 거짓말이다.
            var seen = new HashSet<string>();
            for (int uid = 1; uid <= 40; uid++)
            {
                var s = "";
                foreach (var o in GearSystem.Roll(d, uid)) s += o.Key + o.Val + "|";
                seen.Add(s);
            }
            Assert.That(seen.Count, Is.GreaterThan(20), "마흔 자루가 스무 가지도 못 되면 굴림이 뭉쳐 있다");
        }

        [Test]
        public void AnItemKeepsItsRollAndTheSaveWritesItDown()
        {
            var d = D();
            var s = SaveData.NewSave(d);
            var g = s.NewGear(d.Gear.Parts[0], d.Gear.Types[d.Gear.Parts[0]][0], d.Gear.RarMyth, 0);
            s.Inv.Add(g);
            var rolled = GearSystem.OptsOf(d, g);
            Assert.That(rolled.Count, Is.GreaterThan(0), "번호를 받은 자루는 옵션을 든다");
            Assert.That(g.Opts.Count, Is.EqualTo(rolled.Count), "굴린 값이 그 자루에 담긴다(다음에 또 굴리지 않는다)");

            var back = SaveData.FromJson(s.ToJson(), d);
            var g2 = back.InvById(g.Uid);
            Assert.That(g2, Is.Not.Null, "세이브를 돌아 나와도 그 자루가 있다");
            Assert.That(g2.Opts.Count, Is.EqualTo(g.Opts.Count), "옵션 줄 수가 그대로다");
            for (int i = 0; i < g.Opts.Count; i++)
            {
                Assert.That(g2.Opts[i].Key, Is.EqualTo(g.Opts[i].Key), "축이 그대로다");
                Assert.That(g2.Opts[i].Val, Is.EqualTo(g.Opts[i].Val), "값이 그대로다");
            }
        }

        [Test]
        public void AnOldSaveWithNoRolledOptionsGetsThemWithoutLosingAnything()
        {
            var d = D();
            // T517 앞의 세이브 = inv 칸에 «o» 가 없다. 그 자루도 인벤에 그대로 있어야 하고, 들여다보는 순간 굴려진다.
            // ⚠ «rarN» 을 같이 적는다 — 이 레포의 T517 앞 세이브는 이미 등급이 다섯이다. 그 칸이 없으면 «등급 넷짜리 옛 세이브» 로 읽혀
            //   이관(MigrateGearRar)이 등급을 한 칸 밀고, 밀린 값이 표 밖이라 그 자루가 지워진다(자를 처음 쓸 때 실제로 그랬다).
            int myth = d.Gear.RarMyth;
            string old = "{\"rarN\":" + d.Gear.RarName.Length + ",\"inv\":[{\"u\":9,\"part\":\"" + d.Gear.Parts[0] + "\",\"type\":\"" + d.Gear.Types[d.Gear.Parts[0]][0] + "\",\"rar\":" + myth + ",\"plus\":0}],\"uid\":10}";
            var s = SaveData.FromJson(old, d);
            var g = s.InvById(9);
            Assert.That(g, Is.Not.Null, "옛 세이브의 장비가 사라지면 안 된다");
            Assert.That(g.Opts.Count, Is.EqualTo(0), "읽은 직후에는 비어 있다(적힌 적이 없으니까)");
            Assert.That(GearSystem.OptsOf(d, g).Count, Is.EqualTo(d.Gear.OptCountOpenMax), "들여다보면 그 자리에서 굴려진다");
            Assert.That(g.Opts.Count, Is.EqualTo(d.Gear.OptCountOpenMax), "그리고 담긴다 — 다음 저장에 적힌다");
        }

        [Test]
        public void AnItemWithNoUidNeitherRollsNorRemembers()
        {
            var d = D();
            // 번호를 아직 못 받은 자루(시험용 MkBuild · 합성 직후)를 굴리면 번호를 받은 뒤와 값이 달라진다 — 그래서 안 굴린다.
            var b = GearSystem.MkBuild(d, d.Gear.RarMyth, 0, 0);
            var g = b.EqAt(d.Gear.Parts[0]);
            Assert.That(g.Uid, Is.EqualTo(0));
            Assert.That(GearSystem.OptsOf(d, g).Count, Is.EqualTo(0), "번호 없는 자루는 안 굴린다");
            Assert.That(g.Opts.Count, Is.EqualTo(0), "담지도 않는다(그 값이 세이브로 굳으면 안 된다)");
        }

        [Test]
        public void HowManyLinesAreOpenFollowsTheRarityLadder()
        {
            var d = D();
            var part = d.Gear.Parts[0]; var type = d.Gear.Types[part][0];
            var s = SaveData.NewSave(d);
            for (int rar = 0; rar < d.Gear.RarName.Length; rar++)
            {
                var g = s.NewGear(part, type, rar, 0); s.Inv.Add(g);
                Assert.That(GearSystem.OpenOptCount(d, g), Is.EqualTo(System.Math.Min(d.Gear.OptCount(rar, 0), d.Gear.OptCountOpenMax)),
                    d.Gear.RarName[rar] + " 등급에서 열린 줄 수가 사다리와 다르다");
            }
        }

        // ───────────────────────── 축이 판에서 실제로 도는가 ─────────────────────────
        //  ⚠ «표에 있다» 와 «판이 그것을 읽는다» 는 다른 물음이다. 아래는 뒤엣것을 잰다 —
        //    굴린 값을 손으로 심고, 그 축 하나만 다른 두 판을 돌려 **눈에 보이는 차이**를 본다.

        static RunOptions Opt() => new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = true };
        /// <summary>모든 부위에 «그 축 하나만» 든 장비를 채운 빌드(값은 번호가 아니라 여기서 정한다).</summary>
        static Build BuildWith(GameData d, string axis, double val)
        {
            var b = new Build();
            int uid = 1;
            foreach (var pt in d.Gear.Parts)
            {
                var g = new GearItem { Uid = uid++, Part = pt, Type = d.Gear.Types[pt][0], Rar = d.Gear.RarMyth, Plus = 0 };
                g.Opts.Add(new RolledOpt { Key = axis, Val = val });
                b.Eq[pt] = g; b.Slots[pt] = 0;
            }
            return b;
        }
        static BattleState Run(GameData d, Build b, uint seed = 11, int chapter = 1)
            => new BattleState(d, chapter, b, new Mulberry32(seed), new SimPolicy(), Opt());

        [Test]
        public void AttackAndHealthAxesMoveTheStatsTheyName()
        {
            var d = D();
            var flat = Run(d, BuildWith(d, "atkP", 0));
            var atk = Run(d, BuildWith(d, "atkP", 10));
            Assert.That(atk.P.Dmg, Is.GreaterThan(flat.P.Dmg), "«공격력 +10%» 가 힘을 안 올렸다");
            var hp = Run(d, BuildWith(d, "hpP", 10));
            Assert.That(hp.P.MaxHp, Is.GreaterThan(flat.P.MaxHp), "«체력 +10%» 가 체력을 안 올렸다");
        }

        [Test]
        public void TheFourStatAxesLandOnTheirOwnStat()
        {
            var d = D();
            var b0 = Run(d, BuildWith(d, "atkP", 0));
            Assert.That(Run(d, BuildWith(d, "critR", 5)).P.CritR, Is.GreaterThan(b0.P.CritR), "치명타 확률");
            Assert.That(Run(d, BuildWith(d, "critF", 40)).P.CritF, Is.GreaterThan(b0.P.CritF), "치명타 피해");
            Assert.That(Run(d, BuildWith(d, "evade", 3)).P.Evade, Is.GreaterThan(b0.P.Evade), "회피 확률");
            Assert.That(Run(d, BuildWith(d, "steal", 9)).P.Steal, Is.GreaterThan(b0.P.Steal), "생명력 흡수");
        }

        [Test]
        public void TheThreeRoundAxesReachThePlayer()
        {
            var d = D();
            Assert.That(Run(d, BuildWith(d, "dbl", 7)).P.OptDbl, Is.GreaterThan(0), "더블어택 확률이 판에 안 들어갔다");
            Assert.That(Run(d, BuildWith(d, "skillDmg", 7)).P.OptSkillDmg, Is.GreaterThan(0), "스킬 피해가 판에 안 들어갔다");
            Assert.That(Run(d, BuildWith(d, "regen", 3)).P.OptRegen, Is.GreaterThan(0), "라운드당 회복이 판에 안 들어갔다");
        }

        [Test]
        public void DoubleAttackFiresAtMostOncePerRound()
        {
            var d = D();
            Assert.That(d.Combat.TurnOn, Is.True, "이 자는 턴제 규칙을 잰다(T516)");
            // 확률 100% 로 못 박고 «라운드 수보다 더 터지지 않는가» 를 본다 — 주인 «최대 한번 더블어택하는거임 라운드당».
            var g = Run(d, BuildWith(d, "dbl", 100));
            int guard = 0;
            while (!g.Over && g.TurnFoe == null && guard++ < 4000) g.Tick();
            Assert.That(g.TurnFoe, Is.Not.Null, "적과 마주 서야 라운드가 돈다");
            g.TurnFoe.MaxHp = g.TurnFoe.Hp = 1e12; g.P.MaxHp = g.P.Hp = 1e12; g.P.Sh = 1e12;   // 둘 다 안 죽는다
            int startRound = g.Round, before = g.DoubleHits;
            while (!g.Over && g.Round < startRound + 5 && guard++ < 40000) g.Tick();
            int rounds = g.Round - startRound, hits = g.DoubleHits - before;
            Assert.That(hits, Is.GreaterThan(0), "확률 100% 인데 한 번도 안 터졌다");
            Assert.That(hits, Is.LessThanOrEqualTo(rounds), $"라운드 {rounds}번에 더블어택 {hits}번 — 라운드당 한 번을 넘었다");
        }

        [Test]
        public void RoundHealPutsHealthBackAsRoundsPass()
        {
            var d = D();
            var g = Run(d, BuildWith(d, "regen", 3));
            int guard = 0;
            while (!g.Over && g.TurnFoe == null && guard++ < 4000) g.Tick();
            Assert.That(g.TurnFoe, Is.Not.Null);
            g.TurnFoe.MaxHp = g.TurnFoe.Hp = 1e12;                 // 적은 안 죽는다(라운드를 벌어야 한다)
            g.P.Sh = 0; g.P.Hp = g.P.MaxHp * 0.5;                  // 반쯤 깎아 두고 회복이 도는지 본다
            double low = g.P.Hp;
            int startRound = g.Round;
            // 적이 때리는 것보다 회복이 큰지를 재는 것이 아니다 — «라운드가 오를 때 회복이 도는가» 다.
            //   그래서 적 피해를 0 으로 두고 회복만 남긴다.
            g.TurnFoe.Dmg = 0;
            while (!g.Over && g.Round < startRound + 3 && guard++ < 40000) g.Tick();
            Assert.That(g.P.Hp, Is.GreaterThan(low), "라운드가 지나도 체력이 안 돌아왔다");
        }

        [Test]
        public void SkillDamageOnlyMultipliesSkills()
        {
            var d = D();
            // 맨손 타격은 안 곱한다(주인이 «스킬데미지» 라 했다) — 같은 시드에서 첫 타격 피해가 같아야 한다.
            var a = Run(d, BuildWith(d, "skillDmg", 0));
            var b = Run(d, BuildWith(d, "skillDmg", 10));
            Assert.That(b.P.Dmg, Is.EqualTo(a.P.Dmg), "스킬 피해가 맨손 힘을 건드리면 안 된다");
            // 여섯 부위가 다 그 축을 들었으므로 **쌓인다**(옵션은 부위마다 따로 붙는다) — 10 이 아니라 10 × 부위 수다.
            Assert.That(b.P.OptSkillDmg, Is.EqualTo(10.0 * d.Gear.Parts.Length), "옵션은 부위마다 쌓인다");
        }

        [Test]
        public void TurningTheTableOffPutsTheOldSetOptionsBack()
        {
            // 표에서 축을 걷으면 엔진이 옛 «종류마다 7줄» 로 돌아간다 — 두 규칙이 한 엔진에 산다(T516 과 같은 갈래 · 결정 1416).
            var d = D();
            d.Gear.OptAxes = new List<GearOptAxis>();
            Assert.That(d.Gear.RolledOpts, Is.False);
            var s = SaveData.NewSave(d);
            var g = s.NewGear(d.Gear.Parts[0], d.Gear.Types[d.Gear.Parts[0]][0], d.Gear.RarMyth, 0); s.Inv.Add(g);
            Assert.That(GearSystem.OptsOf(d, g).Count, Is.EqualTo(0), "축이 없으면 아무것도 안 굴린다");
        }
    }
}
