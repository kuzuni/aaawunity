using System;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T240 3항 — 상대(더미) <b>전투력 하나 → 스탯</b> 규칙의 자. 지시서가 «이름·아바타·전투력에서 스탯을 규칙으로 뽑아» 로 맡긴 자리다.
    /// <para>
    /// 여기서 재는 것은 «세다/약하다» 가 아니라 <b>규칙이 스스로 말이 되는가</b> 다 —
    /// ⓐ 스탯을 도로 합치면 그 전투력이 나오는가(되돌아오는가) ⓑ 전투력이 같으면 대등한가 ⓒ 극단이 막히는가 ⓓ 표를 못 읽으면 조용히 0 인가.
    /// 세기 조절(<c>hpMul</c>·<c>dmgMul</c>)은 <b>주인이 «세다/약하다» 를 말했을 때</b> 고칠 손잡이라 값 자체는 자로 박지 않는다.
    /// </para>
    /// ⚠ 이 규칙은 <b>엔진에 아직 안 닿는다</b>(순수 계산기) — 그래서 챕터 전투의 시드 골든(T2)이 움직일 자리가 없다.
    /// </summary>
    public class ArenaFoeTests
    {
        static ArenaFoeData Load() => ArenaFoeData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "arenaMatch.json"))));

        [Test]
        public void TableWeightsAreTheSameFormulaThePowerNumberIsMadeWith()
        {
            var f = Load();
            // App.Power() = round(Atk×8 + (Hp+Sh)×1.5). 이 둘이 갈리면 «내 전투력 100» 과 «상대 전투력 100» 이 다른 세기가 된다.
            Assert.AreEqual(8, f.AtkWeight, "공격 무게 = App.Power() 의 8");
            Assert.AreEqual(1.5, f.HpWeight, "체력 무게 = App.Power() 의 1.5");
            Assert.That(f.MinAtkShare, Is.GreaterThan(0), "몫 하한이 0 이면 «때리지 못하는 상대» 가 나온다");
            Assert.That(f.MaxAtkShare, Is.LessThan(1), "몫 상한이 1 이면 «한 대에 죽는 상대» 가 나온다");
            Assert.That(f.MinAtkShare, Is.LessThan(f.MaxAtkShare));
        }

        [Test]
        public void StatsAddBackUpToTheSamePowerTheyCameFrom()
        {
            var f = Load();
            // ⚠ 이 «되돌아옴» 은 세기 손잡이가 1 일 때의 약속이다 — 주인이 «상대가 세다/약하다» 고 해서 hpMul·dmgMul 을 돌리면
            //   표시된 전투력과 실제 세기가 «일부러» 갈린다. 그때 이 자를 고쳐야 하는 것이 아니라, 그 사실을 알고 돌리는 것이다.
            //   그래서 실패로 세지 않고 여기서 멈춘다(다음 워커가 «자가 막는다» 고 오해하지 않게).
            if (Math.Abs(f.HpMul - 1) > 1e-9 || Math.Abs(f.DmgMul - 1) > 1e-9)
                Assert.Ignore($"세기 손잡이가 1 이 아니다(hpMul {f.HpMul} · dmgMul {f.DmgMul}) — 표시 전투력과 실제 세기를 일부러 갈라 둔 상태라 이 약속은 적용되지 않는다");
            // 되돌아오는가 — 이 한 줄이 «전투력» 이라는 말을 뜻있게 만든다(안 되돌아오면 표시된 수와 실제 세기가 다른 것이다).
            foreach (var power in new[] { 100.0, 5_000.0, 14_730.0, 1_000_000.0 })
                foreach (var share in new[] { 0.2, 0.5, 0.8 })
                {
                    var s = ArenaFoe.Of(f, power, share);
                    Assert.That(ArenaFoe.PowerOf(f, s.Dmg, s.MaxHp), Is.EqualTo(power).Within(power * 1e-9),
                                $"전투력 {power} · 몫 {share} 를 풀었다가 도로 합치면 그대로여야 한다");
                }
        }

        [Test]
        public void SamePowerMeansAnEvenMatchBecauseTheFoeMirrorsMyOwnShare()
        {
            var f = Load();
            // 내가 «공격형» 이면 상대도 공격형, 내가 «체력형» 이면 상대도 체력형 — 몫을 상대마다 지어내면 같은 전투력인데
            // 어떤 상대는 한 방에 죽고 어떤 상대는 안 죽는, 설명할 수 없는 판이 된다.
            if (Math.Abs(f.HpMul - 1) > 1e-9 || Math.Abs(f.DmgMul - 1) > 1e-9)
                Assert.Ignore("세기 손잡이가 1 이 아니다 — «대등» 을 일부러 깬 상태라 이 약속은 적용되지 않는다(위 자와 같은 까닭)");
            foreach (var me in new[] { new[] { 100.0, 200.0, 0.0 }, new[] { 40.0, 900.0, 60.0 }, new[] { 300.0, 50.0, 0.0 } })
            {
                double myPower = ArenaFoe.PowerOf(f, me[0], me[1], me[2]);
                var s = ArenaFoe.Of(f, myPower, me[0], me[1], me[2]);
                double share = ArenaFoe.AtkShareOf(f, me[0], me[1], me[2]);
                // 몫이 하한·상한 안이면 «상대 = 나» 여야 한다(잘린 경우는 아래 자가 따로 본다).
                if (share > f.MinAtkShare && share < f.MaxAtkShare)
                {
                    Assert.That(s.Dmg, Is.EqualTo(me[0]).Within(Math.Max(1e-9, me[0] * 1e-9)), "같은 전투력이면 상대 공격 = 내 공격");
                    Assert.That(s.MaxHp, Is.EqualTo(me[1] + me[2]).Within(Math.Max(1e-9, (me[1] + me[2]) * 1e-9)), "같은 전투력이면 상대 체력 = 내 체력+실드");
                }
            }
        }

        [Test]
        public void AStrongerFoeIsStrongerInBothStatsAndTheOrderNeverFlips()
        {
            var f = Load();
            double share = 0.5;
            ArenaFoe.Stats prev = ArenaFoe.Of(f, 100, share);
            foreach (var power in new[] { 200.0, 400.0, 800.0, 1600.0 })
            {
                var s = ArenaFoe.Of(f, power, share);
                Assert.Greater(s.Dmg, prev.Dmg, "전투력이 높은 상대가 더 세게 때린다");
                Assert.Greater(s.MaxHp, prev.MaxHp, "전투력이 높은 상대가 더 튼튼하다");
                prev = s;
            }
        }

        [Test]
        public void ExtremeBuildsGetClampedSoNoOneIsUnhittableOrOneShot()
        {
            var f = Load();
            // 공격만 있는 빌드 · 체력만 있는 빌드 — 자르지 않으면 상대가 «체력 0»(한 대에 죽음) 이나 «공격 0»(영원히 안 끝남) 이 된다.
            double allAtk = ArenaFoe.AtkShareOf(f, 1000, 0, 0);
            double allHp = ArenaFoe.AtkShareOf(f, 0, 1000, 0);
            Assert.That(allAtk, Is.EqualTo(f.MaxAtkShare).Within(1e-9), "공격만 있는 빌드도 상한에서 멈춘다");
            Assert.That(allHp, Is.EqualTo(f.MinAtkShare).Within(1e-9), "체력만 있는 빌드도 하한에서 멈춘다");
            foreach (var s in new[] { ArenaFoe.Of(f, 5000, allAtk), ArenaFoe.Of(f, 5000, allHp) })
            {
                Assert.Greater(s.Dmg, 0, "어떤 빌드에서도 상대는 때릴 수 있다");
                Assert.Greater(s.MaxHp, 0, "어떤 빌드에서도 상대는 한 대에 안 죽는다");
            }
            // 표 밖의 몫이 들어와도 같은 자리에서 잘린다(부르는 쪽 실수를 규칙이 흡수한다).
            Assert.That(ArenaFoe.Of(f, 5000, 5.0).Dmg, Is.EqualTo(ArenaFoe.Of(f, 5000, f.MaxAtkShare).Dmg).Within(1e-9));
            Assert.That(ArenaFoe.Of(f, 5000, -5.0).Dmg, Is.EqualTo(ArenaFoe.Of(f, 5000, f.MinAtkShare).Dmg).Within(1e-9));
        }

        [Test]
        public void NoTableMeansZeroNotAMadeUpNumber()
        {
            // 표를 못 읽었다고 아무 수나 지어내면 «상대가 왜 이렇게 센가» 를 아무도 설명 못 한다 — 0 이면 부르는 쪽이 바로 안다.
            var s = ArenaFoe.Of(null, 9999, 0.5);
            Assert.AreEqual(0, s.Dmg); Assert.AreEqual(0, s.MaxHp);
            Assert.AreEqual(0, ArenaFoe.PowerOf(null, 1, 1, 1));
            // 전투력이 0 이하인 상대도 마찬가지(더미 표가 비면 0 이 나온다).
            var f = Load();
            Assert.AreEqual(0, ArenaFoe.Of(f, 0, 0.5).Dmg);
            Assert.AreEqual(0, ArenaFoe.Of(f, -10, 0.5).MaxHp);
        }

        [Test]
        public void ABrokenTableCriesWhenItIsReadNotWhenTheMatchStarts()
        {
            // ArenaMatch.From 과 같은 규약(T237 전례) — 이 표의 사고는 조용해서, 읽는 순간 우는 편이 낫다.
            Assert.Throws<FormatException>(() => ArenaFoeData.Parse("{\"foe\":{\"atkWeight\":0}}"), "무게 0 = 전투력을 스탯으로 못 나눈다");
            Assert.Throws<FormatException>(() => ArenaFoeData.Parse("{\"foe\":{\"minAtkShare\":0.9,\"maxAtkShare\":0.1}}"), "하한이 상한보다 크다");
            Assert.Throws<FormatException>(() => ArenaFoeData.Parse("{\"foe\":{\"maxAtkShare\":1.4}}"), "몫은 1 을 넘을 수 없다");
            Assert.Throws<FormatException>(() => ArenaFoeData.Parse("{\"foe\":{\"dmgMul\":0}}"), "세기 손잡이가 0 이면 «안 때리는 상대» 다");
            // 반대쪽: «foe 칸이 아예 없는» 옛 표는 기본값으로 조용히 읽힌다(표가 늘기 전 파일과도 맞는다).
            var f = ArenaFoeData.Parse("{\"win\":8}");
            Assert.AreEqual(8, f.AtkWeight); Assert.AreEqual(1.5, f.HpWeight);
        }
    }
}
