using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T63 — 글자 최소 크기 규칙(주인 2026-09-06 «글씨가 너무 작아 안 읽힌다 · 다 바꿔라»). 하한 상수가 지시서 값(본문 40 · 버튼 44 · 보조 36 · 제목 60 · bestFit 최소 32)인지,
    /// <see cref="TextSize.Floor"/>/<see cref="TextSize.BestFitFloor"/> 가 작은 값만 올리고 큰 값·Small 은 그대로 두는지. 화면 실물 검사는 PlayMode TextSizeGateTests.
    /// </summary>
    public class TextSizeTests
    {
        [Test]
        public void FloorsMatchOwnerRule()
        {
            Assert.AreEqual(40, TextSize.Body);
            Assert.AreEqual(44, TextSize.Button);
            Assert.AreEqual(36, TextSize.Aux);
            Assert.AreEqual(60, TextSize.Title);
            Assert.AreEqual(32, TextSize.BestFitMin);
            Assert.AreEqual(TextSize.Body, TextSize.Min(TextKind.Body));
            Assert.AreEqual(TextSize.Button, TextSize.Min(TextKind.Button));
            Assert.AreEqual(TextSize.Aux, TextSize.Min(TextKind.Aux));
            Assert.AreEqual(TextSize.Title, TextSize.Min(TextKind.Title));
            Assert.AreEqual(0, TextSize.Min(TextKind.Small));
        }

        [Test]
        public void FloorRaisesOnlySmallSizes()
        {
            // 등재 세션 실측: 22~30 이 116곳 · 10~21 이 27곳 — 전부 하한으로
            Assert.AreEqual(40, TextSize.Floor(22));
            Assert.AreEqual(40, TextSize.Floor(30));
            Assert.AreEqual(40, TextSize.Floor(12));
            Assert.AreEqual(40, TextSize.Floor(40));
            Assert.AreEqual(52, TextSize.Floor(52));
            Assert.AreEqual(44, TextSize.Floor(30, TextKind.Button));
            Assert.AreEqual(36, TextSize.Floor(22, TextKind.Aux));
            Assert.AreEqual(60, TextSize.Floor(46, TextKind.Title));
            Assert.AreEqual(72, TextSize.Floor(72, TextKind.Title));
        }

        [Test]
        public void SmallKindIsExplicitOptOut()
        {
            Assert.AreEqual(18, TextSize.Floor(18, TextKind.Small));
            Assert.AreEqual(12, TextSize.BestFitFloor(12, TextKind.Small));
        }

        [Test]
        public void BestFitMinNeverBelow32()
        {
            Assert.AreEqual(32, TextSize.BestFitFloor(12));
            Assert.AreEqual(32, TextSize.BestFitFloor(20, TextKind.Button));
            Assert.AreEqual(32, TextSize.BestFitFloor(32));
            Assert.AreEqual(36, TextSize.BestFitFloor(36));
        }

        [Test]
        public void BattleNumbersScaleUp()
        {
            Assert.Greater(TextSize.BattleNumberMul, 1.0f);
            Assert.AreEqual(1.3f, TextSize.BattleNumberMul, 1e-6f);
        }
    }
}
