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
            // ⚑ 정본 일곱 파일이 «제대로 실렸는가» 를 재는 자다 — 그래서 밸런스 덮어쓰기를 안 먹인 표로 본다.
            //   «챕터 수 == maxChapter» 는 정본끼리의 약속이고, 이 레포는 주인 지시로 maxChapter 를 줄인다(T325 ⓑ · 챕터 100).
            //   그 줄임은 «덜 실렸다» 가 아니라 «주인이 100 까지만 쓰기로 했다» 이므로 여기서 잴 것이 아니다
            //   (덮어쓰기 뒤의 관계는 GameData.ValidateOverridden 이 «정본이 더 많아도 된다» 로 따로 잰다).
            var d = TestData.PreBalance();
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
            // T89(주인 지시 2026-09-07) — 사다리가 한 칸 뒤로 밀렸다: 일반 0 · 신화 3 · 마지막 줄은 신화 +12강
            Assert.That(g.OptCount(0, 0), Is.EqualTo(0));
            Assert.That(g.OptCount(g.RarMyth, 0), Is.EqualTo(3));
            Assert.That(g.OptCount(g.RarMyth, 9), Is.EqualTo(g.OptMaxCount - 1));
            Assert.That(g.OptCount(g.RarMyth, 12), Is.EqualTo(g.OptMaxCount));
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
                // 양 끝 굴림은 «확률이 0 이 아닌 첫 칸 · 마지막 칸» 을 돌려준다 — 위아래가 짝이다.
                // ⚠ 아래를 «0» 으로 박으면 안 된다: 주인의 전설 상자는 일반이 0% 라(66% 희귀 · 30% 영웅 · 4% 전설)
                //   제일 흔한 등급이 0번 칸이 아니다. 재야 할 것은 자리가 아니라 **규칙**이다.
                Assert.That(b.RarRoll(0), Is.EqualTo(Array.FindLastIndex(b.Rate, r => r > 0)), b.Key + ": 0 은 나올 수 있는 가장 높은 등급");
                Assert.That(b.RarRoll(99.999), Is.EqualTo(Array.FindIndex(b.Rate, r => r > 0)), b.Key + ": 끝은 나올 수 있는 가장 낮은 등급");
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
