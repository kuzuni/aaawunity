using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>mulberry32·챕터 레이아웃·적 스탯 이식이 enemies.json(= sim.js 실측) 과 한 칸도 어긋나지 않는가.</summary>
    public class LayoutTests
    {
        [Test]
        public void Mulberry32MatchesJsReferenceStream()
        {
            // node -e "const m=(a=>()=>{a|=0;a=a+0x6D2B79F5|0;let t=Math.imul(a^a>>>15,1|a);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296})(11); console.log(m(),m(),m())"
            var r = new Mulberry32(11);
            Assert.That(r.Next(), Is.EqualTo(0.5115870486479253).Within(1e-15));
            Assert.That(r.Next(), Is.EqualTo(0.5299464082345366).Within(1e-15));
            Assert.That(r.Next(), Is.EqualTo(0.6081185641232878).Within(1e-15));
        }

        [Test]
        public void GeneratedLayoutMatchesJsonForEveryChapter()
        {
            var d = TestData.Load();
            for (int c = 1; c <= d.Tune.MaxChapter; c++)
            {
                var gen = ChapterLayout.Generate(d.Enemies, c);
                var js = d.Enemies.Chapter(c);
                Assert.That(gen.Count, Is.EqualTo(js.Nodes.Count), "chapter " + c);
                for (int i = 0; i < gen.Count; i++)
                {
                    Assert.That(gen[i].Type, Is.EqualTo(js.Nodes[i].Type), $"chapter {c} node {i}");
                    if (gen[i].Type != NodeType.Wave) continue;
                    Assert.That(gen[i].Size, Is.EqualTo(js.Nodes[i].Size), $"chapter {c} node {i} size");
                    Assert.That(gen[i].Ranged, Is.EqualTo(js.Nodes[i].Ranged), $"chapter {c} node {i} ranged");
                }
                Assert.That(ChapterLayout.EnemyCount(d.Enemies, c), Is.EqualTo(js.EnemyCount));
                Assert.That(ChapterLayout.WaveSizes(d.Enemies, c), Is.EqualTo(js.WaveSizes));
            }
        }

        [Test]
        public void EnemyStatsFormulaMatchesJsonForEveryChapter()
        {
            var d = TestData.Load();
            for (int c = 1; c <= d.Tune.MaxChapter; c++)
            {
                var js = d.Enemies.Chapter(c);
                for (int w = 0; w < js.Waves.Count; w++)
                {
                    ChapterLayout.EnemyStats(d.Tune, c, w, out var hp, out var dmg);
                    Assert.That(hp, Is.EqualTo(js.Waves[w].Hp), $"chapter {c} wave {w} hp");
                    Assert.That(dmg, Is.EqualTo(js.Waves[w].Dmg), $"chapter {c} wave {w} dmg");
                }
                ChapterLayout.EnemyStats(d.Tune, c, js.Boss.W, out var bh, out var bd);
                Assert.That(bh * d.Enemies.BossHpMul, Is.EqualTo(js.Boss.Hp), $"chapter {c} boss hp");
                Assert.That(bd * d.Enemies.BossDmgMul, Is.EqualTo(js.Boss.Dmg), $"chapter {c} boss dmg");
            }
        }
    }
}
