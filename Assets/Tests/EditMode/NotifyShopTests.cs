using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T355 — 하단 네비 «상점» 탭 점의 판정(<see cref="Notify.ShopAny"/>)은 무료 보급 자리 넷(<see cref="ShopFree.All"/>)을 전부 본다.
    /// 옛 판정은 <see cref="SaveData.FreeDay"/>(= 다이아 자리) 하나만 봐서, 다이아만 받으면 골드·광고 상자가 남아도 점이 꺼졌다(주인 2026-09-10).
    /// </summary>
    public class NotifyShopTests
    {
        const string Today = "2026-09-10";

        [Test]
        public void 자리_넷_중_하나라도_남았으면_켜지고_다_쓰면_꺼진다()
        {
            var s = new SaveData();
            Assert.IsTrue(Notify.ShopAny(s, Today), "아무것도 안 받은 오늘 — 켜진다");
            foreach (var t in ShopFree.All) Assert.IsTrue(ShopFree.Take(s, t, Today), t + " 오늘 몫을 쓴다");
            Assert.IsFalse(Notify.ShopAny(s, Today), "넷 다 오늘 받았다 — 꺼진다");
            s.FreeDays.Remove(ShopFree.BoxLegend);
            Assert.IsTrue(Notify.ShopAny(s, Today), "하나만 남아도 켜진다(광고 상자)");
        }

        [Test]
        public void 다이아만_받았으면_아직_켜져_있다_옛_판정은_여기서_꺼졌다()
        {
            var s = new SaveData();
            Assert.IsTrue(ShopFree.Take(s, ShopFree.Gem, Today));
            Assert.AreEqual(Today, s.FreeDay, "옛 칸(FreeDay)은 다이아 자리와 같은 값이다");
            Assert.IsTrue(Notify.ShopAny(s, Today), "골드·광고 상자가 남았으니 켜진다 — 옛 판정(FreeDay != today)은 거짓이었다(T355)");
        }

        [Test]
        public void 날이_바뀌면_다시_켜진다()
        {
            var s = new SaveData();
            foreach (var t in ShopFree.All) ShopFree.Take(s, t, Today);
            Assert.IsFalse(Notify.ShopAny(s, Today));
            Assert.IsTrue(Notify.ShopAny(s, "2026-09-11"), "다음 날 — 넷 다 다시 남는다");
        }
    }
}
