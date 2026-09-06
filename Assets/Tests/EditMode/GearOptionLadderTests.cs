using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T89 — 세트 옵션 개방 사다리가 한 칸 뒤로 밀렸다(주인 지시 2026-09-07 «일반 등급에서는 옵션 안 열리게 ·
    /// 희귀에서부터 · 신화 12강이 되면 마지막 흡혈 +8% 개방»). 정본 <c>data/gear.json</c> 은 그대로고
    /// <see cref="GearData"/> 가 읽은 뒤에 밀어 준다 — 그 결과와 «잠긴 줄 꼬리표» 를 여기서 지킨다.
    /// </summary>
    public class GearOptionLadderTests
    {
        static GearData Gear() => TestData.Load().Gear;

        [Test]
        public void OptCountByRarity_IsZeroForCommonAndOpensFromRare()
        {
            var g = Gear();
            Assert.That(g.OptCount(0, 0), Is.EqualTo(0), "일반 = 옵션 0개");
            Assert.That(g.OptCount(1, 0), Is.EqualTo(1), "희귀부터 열린다");
            Assert.That(g.OptCount(2, 0), Is.EqualTo(2), "전설 = 2개");
            Assert.That(g.OptCount(g.RarMyth, 0), Is.EqualTo(3), "신화 = 3개");
        }

        [Test]
        public void MythEnhance_OpensOneRowAtEachStepUpToTwelve()
        {
            var g = Gear();
            Assert.That(g.MythPlusOptAt, Is.EqualTo(new[] { 3, 6, 9, 12 }));
            Assert.That(g.OptCount(g.RarMyth, 3), Is.EqualTo(4));
            Assert.That(g.OptCount(g.RarMyth, 6), Is.EqualTo(5));
            Assert.That(g.OptCount(g.RarMyth, 9), Is.EqualTo(6));
            Assert.That(g.OptCount(g.RarMyth, 12), Is.EqualTo(7));
            Assert.That(g.OptCount(g.RarMyth, 12), Is.EqualTo(g.OptMaxCount), "마지막 줄(흡혈 +8%)이 신화 +12강에서 열린다");
        }

        [Test]
        public void BetweenSteps_TheCountDoesNotGrow()
        {
            var g = Gear();
            foreach (var plus in new[] { 1, 2 }) Assert.That(g.OptCount(g.RarMyth, plus), Is.EqualTo(3), "+" + plus);
            foreach (var plus in new[] { 4, 5 }) Assert.That(g.OptCount(g.RarMyth, plus), Is.EqualTo(4), "+" + plus);
            foreach (var plus in new[] { 7, 8 }) Assert.That(g.OptCount(g.RarMyth, plus), Is.EqualTo(5), "+" + plus);
            foreach (var plus in new[] { 10, 11 }) Assert.That(g.OptCount(g.RarMyth, plus), Is.EqualTo(6), "+" + plus);
            Assert.That(g.OptCount(g.RarMyth, 99), Is.EqualTo(g.OptMaxCount), "표를 넘겨도 최대 줄 수를 넘지 않는다");
        }

        [Test]
        public void RowCountIsUnchanged_SevenRowsPerType()
        {
            var g = Gear();
            Assert.That(g.OptMaxCount, Is.EqualTo(7));
            foreach (var ty in g.AllTypes) Assert.That(g.Options[ty.Type].Count, Is.EqualTo(7), ty.Type);
        }

        [Test]
        public void TierName_ReadsTheLadderTable_NotAHardCodedTripleStep()
        {
            var g = Gear();
            Assert.That(g.OptTierName(0), Is.EqualTo(g.RarName[1]));                 // 희귀
            Assert.That(g.OptTierName(1), Is.EqualTo(g.RarName[2]));                 // 전설
            Assert.That(g.OptTierName(2), Is.EqualTo(g.RarName[g.RarMyth]));         // 신화
            Assert.That(g.OptTierName(3), Is.EqualTo(g.RarName[g.RarMyth] + " +3강"));
            Assert.That(g.OptTierName(6), Is.EqualTo(g.RarName[g.RarMyth] + " +12강"));
            Assert.That(GearText.LockSuffix(g.OptTierName(6)), Is.EqualTo(" (" + g.RarName[g.RarMyth] + " +12강)"));
        }

        [Test]
        public void TierRarAndMythPlusFlag_MatchTheRowThatUnlocks()
        {
            var g = Gear();
            for (int i = 0; i < g.OptMaxCount; i++)
            {
                bool mythPlus = g.OptNeedsMythPlus(i);
                Assert.That(mythPlus, Is.EqualTo(i >= 3), "줄 " + i);
                int rar = g.OptTierRar(i);
                Assert.That(g.OptCount(rar, mythPlus ? g.MythPlusOptAt[i - 3] : 0), Is.GreaterThan(i), "줄 " + i + " 은 그 단계에서 켜져야 한다");
                if (!mythPlus && rar > 0) Assert.That(g.OptCount(rar - 1, 0), Is.LessThanOrEqualTo(i), "줄 " + i + " 은 한 등급 아래에서는 잠겨야 한다");
            }
        }
    }
}
