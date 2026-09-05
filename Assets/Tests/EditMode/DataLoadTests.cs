using System;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    public class DataLoadTests
    {
        [Test]
        public void LoadsAllSevenFiles()
        {
            var d = TestData.Load();
            Assert.That(d.Tune.MaxChapter, Is.GreaterThan(0));
            Assert.That(d.Enemies.Chapters.Count, Is.EqualTo(d.Tune.MaxChapter));
            Assert.That(d.Perks.Perks.Count, Is.EqualTo(d.Perks.Count));
            Assert.That(d.Gear.AllTypes.Count, Is.EqualTo(d.Gear.Parts.Length * d.Gear.Sets.Length));
            Assert.That(d.Gacha.Boxes.Count, Is.EqualTo(3));
            Assert.That(d.Combat.PlayerSpeed, Is.GreaterThan(0));
            Assert.That(d.Ui.DesignWidth, Is.GreaterThan(0));
        }

        [Test]
        public void AllDataFilesShareOneSimSource()
        {
            var d = TestData.Load();
            // enemies/perks/gear/gacha/combat 는 전부 sim.js 한 blob 에서 뽑힌다 (ui.json 만 index.html).
            Assert.That(d.Tune.Source, Does.StartWith("sim.js@"));
        }

        [Test]
        public void ExpNeedTableIsLinearAndExtrapolates()
        {
            var t = TestData.Load().Tune;
            int step = t.ExpNeedTable[1] - t.ExpNeedTable[0];
            for (int lv = 2; lv < t.ExpNeedTable.Length; lv++)
                Assert.That(t.ExpNeedTable[lv] - t.ExpNeedTable[lv - 1], Is.EqualTo(step), "표가 등차가 아니다 — 연장 규칙을 다시 볼 것");
            int last = t.ExpNeedTable.Length;
            Assert.That(t.ExpNeed(last + 1), Is.EqualTo(t.ExpNeedTable[last - 1] + step));
            Assert.That(t.ExpNeed(1), Is.EqualTo(t.ExpNeedTable[0]));
        }

        [Test]
        public void EveryGearTypeHasFullOptionLadder()
        {
            var g = TestData.Load().Gear;
            foreach (var ty in g.AllTypes)
            {
                var opts = g.Options[ty.Type];
                Assert.That(opts.Count, Is.EqualTo(g.OptMaxCount), ty.Type);
                for (int i = 0; i < opts.Count; i++)
                {
                    Assert.That(opts[i].Slot, Is.EqualTo(i + 1));
                    Assert.That(opts[i].Px.Count + opts[i].Stat.Count, Is.GreaterThan(0), ty.Type + " slot " + (i + 1) + " 효과가 비었다");
                }
            }
            Assert.That(g.OptCount(0, 0), Is.EqualTo(1));
            Assert.That(g.OptCount(g.RarMyth, 0), Is.EqualTo(4));
            Assert.That(g.OptCount(g.RarMyth, 9), Is.EqualTo(g.OptMaxCount));
        }

        [Test]
        public void GachaCumulativeThresholdsMatchRates()
        {
            var ga = TestData.Load().Gacha;
            foreach (var b in ga.Boxes)
            {
                double acc = 0;
                for (int i = b.Rate.Length - 1; i >= 0; i--)
                {
                    acc = Math.Round(acc + b.Rate[i], 6);
                    Assert.That(b.Cum[i], Is.EqualTo(acc).Within(1e-9), b.Key);
                }
                Assert.That(b.Cum[0], Is.EqualTo(100).Within(1e-9));
                Assert.That(b.RarRoll(0), Is.EqualTo(Array.FindLastIndex(b.Rate, r => r > 0)));
                Assert.That(b.RarRoll(99.999), Is.EqualTo(0));
            }
        }

        [Test]
        public void PerkConstantsCoverEveryProbabilityUsedByEngine()
        {
            var p = TestData.Load().Perks;
            foreach (var name in new[] { "PERK_ATK_M", "PERK_DEF_M", "PERK_EVHEAL_CH", "PERK_EVHEAL_F", "PERK_KILL_N", "PERK_THORN_N", "PERK_GIANT_M", "PERK_NHIT_ARROW" })
                Assert.That(p.Consts.ContainsKey(name), name);
            Assert.That(p.GradeRate.Length, Is.EqualTo(3));
            Assert.That(p.OfferPerLevel, Is.EqualTo(3));
        }
    }
}
