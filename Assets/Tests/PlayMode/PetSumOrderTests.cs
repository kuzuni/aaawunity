using KkomaKnight.Game;
using NUnit.Framework;

namespace KkomaKnight.Tests.Play
{
    /// <summary>T475 — 펫 화면 합계 줄·미리보기 줄의 차례는 공·체·실이다(주인 2026-09-12 «펫 부분도 공체실 순서로 표시해야 함 · 체실공으로 표시돼 있음 지금»).</summary>
    public class PetSumOrderTests
    {
        [Test]
        public void 합계_줄_차례는_공_체_실()
        {
            CollectionAssert.AreEqual(new[] { "pi.attack", "pi.heart", "pi.shield" }, PetScreen.SumIcons, "T475 — 주인 «공체실 순서로» (종전 체·실·공)");
            CollectionAssert.AreEqual(new[] { 1.0, 2.0, 3.0 }, PetScreen.InSumOrder(atk: 1, hp: 2, sh: 3), "값도 같은 차례 — 공·체·실");
        }
    }
}
