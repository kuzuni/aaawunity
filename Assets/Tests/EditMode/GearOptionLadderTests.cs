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
        public void OptCountByRarity_IsZeroForCommonAndCapsAtTwo()
        {
            var g = Gear();
            // T515(주인 2026-09-13 «옵션은 최대 2개로 해줘야함 장비들») — 사다리는 그대로 두 칸까지만 오른다.
            Assert.That(g.OptCount(0, 0), Is.EqualTo(0), "원시 = 옵션 0개");
            Assert.That(g.OptCount(1, 0), Is.EqualTo(1), "중세부터 열린다");
            Assert.That(g.OptCount(2, 0), Is.EqualTo(2), "근대 = 2개");
            Assert.That(g.OptCount(g.RarMyth, 0), Is.EqualTo(2), "사이버도 2개 — 상한이다(T515)");
            Assert.That(g.OptCountOpenMax, Is.EqualTo(2), "이 표로 열 수 있는 최대 줄 수 = 2");
        }

        [Test]
        public void MythEnhance_NoLongerOpensRows_TheOwnerCappedItAtTwo()
        {
            var g = Gear();
            // ⚠ 이 자는 뒤집힌 자다 — T89 때는 «강화가 줄을 연다» 를 지켰고, T515(주인 2026-09-13)가 그 사다리를 걷었다.
            Assert.That(g.MythPlusOptAt, Is.Empty, "강화로 열리는 칸이 없다(gearOverride.json mythPlusAt [])");
            foreach (var plus in new[] { 0, 3, 6, 9, 12, 99 })
                Assert.That(g.OptCount(g.RarMyth, plus), Is.EqualTo(2), "사이버 +" + plus + " 도 2개다");
        }

        [Test]
        public void BetweenSteps_TheCountDoesNotGrow()
        {
            var g = Gear();
            foreach (var plus in new[] { 1, 2, 4, 5, 7, 8, 10, 11 }) Assert.That(g.OptCount(g.RarMyth, plus), Is.EqualTo(2), "+" + plus);
            Assert.That(g.OptCount(g.RarMyth, 99), Is.EqualTo(g.OptCountOpenMax), "표를 넘겨도 열 수 있는 최대 줄 수를 넘지 않는다");
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
            Assert.That(g.OptTierName(0), Is.EqualTo(g.RarName[1]));                 // 중세
            Assert.That(g.OptTierName(1), Is.EqualTo(g.RarName[2]));                 // 근대
            Assert.That(GearText.LockSuffix(g.OptTierName(1)), Is.EqualTo(" (" + g.RarName[2] + ")"));
            // 상한(2) 위의 줄은 «열리는 등급» 자체가 없다 — 그리는 쪽도 그 줄을 안 그린다(GearUi.OptionRows · T515).
            Assert.That(g.OptCountOpenMax, Is.EqualTo(2));
        }

        [Test]
        public void TierRarAndMythPlusFlag_MatchTheRowThatUnlocks()
        {
            var g = Gear();
            for (int i = 0; i < g.OptCountOpenMax; i++)
            {
                Assert.That(g.OptNeedsMythPlus(i), Is.False, "상한 안의 줄은 강화가 아니라 등급이 연다(T515) — 줄 " + i);
                int rar = g.OptTierRar(i);
                Assert.That(g.OptCount(rar, 0), Is.GreaterThan(i), "줄 " + i + " 은 그 등급에서 켜져야 한다");
                if (rar > 0) Assert.That(g.OptCount(rar - 1, 0), Is.LessThanOrEqualTo(i), "줄 " + i + " 은 한 등급 아래에서는 잠겨야 한다");
            }
        }
    }
}
