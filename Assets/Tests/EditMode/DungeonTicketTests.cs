using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 던전 티켓 규칙(T99 · 주인 2026-09-07 «하루 2개 보충 — 2개 미만일 시에 2개로» · «광고 1개 · 50다이아 1개 · 둘 다 하루에 각각 1번씩, 던전당»)과
    /// 보상 표(지옥의 문 첫 클리어 펫알 11 + 골드 1,000 · 이후 펫알 5 + 골드 1,000 · 원정 첫 5,800 · 이후 3,500)를 못 박는다.
    /// 수치는 전부 <c>Assets/KkomaKnight/dungeon.json</c> 에서 온다 — 이 테스트는 그 파일의 값을 읽어 규칙만 검사한다(코드 상수 0).
    /// </summary>
    public class DungeonTicketTests
    {
        const string Json = @"{
          ""dailyRefill"": 2, ""gemCost"": 50, ""adPerDay"": 1, ""gemPerDay"": 1,
          ""dungeons"": [
            { ""key"": ""hell"", ""first"": { ""petEgg"": 11, ""gold"": 1000 }, ""clear"": { ""petEgg"": 5, ""gold"": 1000 }, ""sweep"": { ""petEgg"": 5, ""gold"": 1000 } },
            { ""key"": ""expedition"", ""first"": { ""gold"": 5800 }, ""clear"": { ""gold"": 3500 }, ""sweep"": { ""gold"": 3500 } }
          ]
        }";

        static DungeonData D() => DungeonData.Parse(Json);
        static SaveData S() => SaveData.NewSave(TestData.Load());

        [Test]
        public void RealFileMatchesTheOwnersNumbers()
        {
            var d = DungeonData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "dungeon.json"))));
            Assert.AreEqual(2, d.DailyRefill, "하루 보충 2개");
            Assert.AreEqual(50, d.GemCost, 1e-9, "티켓 다이아 값 50");
            Assert.AreEqual(1, d.AdPerDay, "광고 하루 1회"); Assert.AreEqual(1, d.GemPerDay, "다이아 하루 1회");
            var hell = d.Of("hell"); Assert.IsNotNull(hell, "지옥의 문");
            Assert.AreEqual(11, hell.First.PetEgg, 1e-9, "지옥의 문 첫 클리어 펫알 11(기본 5 + 첫 보너스 6)");
            Assert.AreEqual(1000, hell.First.Gold, 1e-9, "지옥의 문 첫 클리어 골드 1,000");
            Assert.AreEqual(5, hell.Clear.PetEgg, 1e-9, "이후 클리어 펫알 5"); Assert.AreEqual(1000, hell.Clear.Gold, 1e-9, "이후 클리어 골드 1,000");
            Assert.AreEqual(5, hell.Sweep.PetEgg, 1e-9, "소탕 펫알 5"); Assert.AreEqual(1000, hell.Sweep.Gold, 1e-9, "소탕 골드 1,000");
            var exp = d.Of("expedition"); Assert.IsNotNull(exp, "원정");
            Assert.AreEqual(5800, exp.First.Gold, 1e-9, "원정 첫 클리어 골드 5,800");
            Assert.AreEqual(3500, exp.Clear.Gold, 1e-9, "원정 클리어 골드 3,500"); Assert.AreEqual(3500, exp.Sweep.Gold, 1e-9, "원정 소탕 골드 3,500");
            Assert.AreEqual(0, exp.First.PetEgg, 1e-9, "원정은 펫알을 주지 않는다");
        }

        [Test]
        public void DayRollFillsUpToTwoButNeverAdds()
        {
            var d = D(); var s = S();
            DungeonTickets.Roll(s, d, "2026-09-06");
            Assert.AreEqual(2, DungeonTickets.Tickets(s, d, "hell", "2026-09-06"), "첫 접근 0 → 2");

            s.DunTickets["hell"] = 1;
            DungeonTickets.Roll(s, d, "2026-09-07");
            Assert.AreEqual(2, DungeonTickets.Tickets(s, d, "hell", "2026-09-07"), "1 → 2(모자란 만큼만 채운다)");

            DungeonTickets.Roll(s, d, "2026-09-08");
            Assert.AreEqual(2, DungeonTickets.Tickets(s, d, "hell", "2026-09-08"), "2 → 2(그대로 · 더하지 않는다)");

            s.DunTickets["hell"] = 3;
            DungeonTickets.Roll(s, d, "2026-09-09");
            Assert.AreEqual(3, DungeonTickets.Tickets(s, d, "hell", "2026-09-09"), "3 → 3(줄이지도 않는다)");
        }

        [Test]
        public void SameDayDoesNotRefill()
        {
            var d = D(); var s = S();
            DungeonTickets.Roll(s, d, "2026-09-06");
            s.DunTickets["hell"] = 0;
            DungeonTickets.Roll(s, d, "2026-09-06");
            Assert.AreEqual(0, DungeonTickets.Tickets(s, d, "hell", "2026-09-06"), "같은 날에는 다시 채우지 않는다");
        }

        [Test]
        public void AdGivesOneTicketOncePerDayPerDungeon()
        {
            var d = D(); var s = S();
            DungeonTickets.Roll(s, d, "2026-09-06"); s.DunTickets["hell"] = 0; s.DunTickets["expedition"] = 0;

            Assert.IsTrue(DungeonTickets.CanAd(s, d, "hell", "2026-09-06"), "첫 광고는 가능");
            Assert.IsTrue(DungeonTickets.ClaimAd(s, d, "hell", "2026-09-06"), "광고 1회 → 티켓 +1");
            Assert.AreEqual(1, DungeonTickets.Tickets(s, d, "hell", "2026-09-06"));
            Assert.IsFalse(DungeonTickets.CanAd(s, d, "hell", "2026-09-06"), "같은 날 두 번째 광고는 거부");
            Assert.IsFalse(DungeonTickets.ClaimAd(s, d, "hell", "2026-09-06"), "거부되면 티켓도 안 는다");
            Assert.AreEqual(1, DungeonTickets.Tickets(s, d, "hell", "2026-09-06"));

            Assert.IsTrue(DungeonTickets.CanAd(s, d, "expedition", "2026-09-06"), "던전마다 따로 센다");
            Assert.IsTrue(DungeonTickets.ClaimAd(s, d, "expedition", "2026-09-06"));

            DungeonTickets.Roll(s, d, "2026-09-07");
            Assert.IsTrue(DungeonTickets.CanAd(s, d, "hell", "2026-09-07"), "날짜가 바뀌면 다시 가능");
        }

        [Test]
        public void GemBuysOneTicketOncePerDayAndCostsGems()
        {
            var d = D(); var s = S(); s.Gem = 120;
            DungeonTickets.Roll(s, d, "2026-09-06"); s.DunTickets["hell"] = 0;

            Assert.IsTrue(DungeonTickets.CanBuyGem(s, d, "hell", "2026-09-06"), "다이아 120 ≥ 50");
            Assert.IsTrue(DungeonTickets.BuyGem(s, d, "hell", "2026-09-06"), "50 다이아 → 티켓 +1");
            Assert.AreEqual(70, s.Gem, 1e-9, "다이아 50 차감");
            Assert.AreEqual(1, DungeonTickets.Tickets(s, d, "hell", "2026-09-06"));
            Assert.IsFalse(DungeonTickets.GemLeft(s, d, "hell", "2026-09-06"), "같은 날 두 번째 구매는 거부");
            Assert.IsFalse(DungeonTickets.BuyGem(s, d, "hell", "2026-09-06"));
            Assert.AreEqual(70, s.Gem, 1e-9, "거부되면 다이아도 안 준다");

            Assert.IsTrue(DungeonTickets.GemLeft(s, d, "expedition", "2026-09-06"), "던전마다 따로");
            s.Gem = 10;
            Assert.IsFalse(DungeonTickets.CanBuyGem(s, d, "expedition", "2026-09-06"), "다이아가 모자라면 못 산다");
            Assert.IsFalse(DungeonTickets.BuyGem(s, d, "expedition", "2026-09-06"));
            Assert.AreEqual(10, s.Gem, 1e-9);
        }

        [Test]
        public void ReadyMeansTicketsOrAnAdTicketWaiting()
        {
            var d = D(); var s = S();
            DungeonTickets.Roll(s, d, "2026-09-06");
            Assert.IsTrue(DungeonTickets.Ready(s, d, "hell", "2026-09-06"), "티켓 2 → 빨간 점");
            Assert.IsTrue(DungeonTickets.AnyReady(s, d, "2026-09-06"));

            s.DunTickets["hell"] = 0; s.DunTickets["expedition"] = 0;
            Assert.IsTrue(DungeonTickets.Ready(s, d, "hell", "2026-09-06"), "티켓 0 이라도 광고가 남았으면 빨간 점");
            DungeonTickets.ClaimAd(s, d, "hell", "2026-09-06"); s.DunTickets["hell"] = 0;
            Assert.IsFalse(DungeonTickets.Ready(s, d, "hell", "2026-09-06"), "티켓 0 · 광고도 다 썼으면 점 없음");
            Assert.IsTrue(DungeonTickets.AnyReady(s, d, "2026-09-06"), "다른 던전이 남아 있으면 여전히 있다");
        }

        [Test]
        public void SaveKeepsTicketsAcrossJsonRoundTrip()
        {
            var d = D(); var s = S(); s.Gem = 500;
            DungeonTickets.Roll(s, d, "2026-09-06");
            DungeonTickets.ClaimAd(s, d, "hell", "2026-09-06");
            DungeonTickets.BuyGem(s, d, "expedition", "2026-09-06");

            var back = SaveData.FromJson(s.ToJson(), TestData.Load());
            Assert.AreEqual("2026-09-06", back.DunDay, "날짜가 살아남는다");
            Assert.AreEqual(3, DungeonTickets.Tickets(back, d, "hell", "2026-09-06"), "보충 2 + 광고 1 = 3 이 저장·복원된다");
            Assert.AreEqual(3, DungeonTickets.Tickets(back, d, "expedition", "2026-09-06"), "보충 2 + 다이아 1 = 3");
            Assert.IsFalse(DungeonTickets.CanAd(back, d, "hell", "2026-09-06"), "오늘 쓴 광고 횟수도 살아남는다");
            Assert.IsFalse(DungeonTickets.GemLeft(back, d, "expedition", "2026-09-06"), "오늘 쓴 다이아 횟수도 살아남는다");
            Assert.AreEqual(450, back.Gem, 1e-9, "다이아 500 − 50");
        }
    }
}
