using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T264 — 특권(11) 카드의 자. 주인 2026-09-09 06:2X
    /// «특권 부분에 맨 위에 «데일리 기프트» 라는 섹션? 그런 거 카드처럼 만들어 주고 그거 다이아 30개 주게 하기. 하루마다 30개씩 주는 거임, 받기 버튼 클릭.»
    /// + 카드 3종의 즉시·매일 값(광고 제거 2,400·50 · 월간 카드 600·300 · 평생 다이아 4,000·300).
    /// <para>
    /// 여덟 값이 <b>전부 주인이 준 것</b>이라 자가 그대로 못 박는다(위임으로 채운 칸이 없어 «자가 표의 거울» 문제가 없다 · 결정 555).
    /// 그 위에 <b>규칙</b>을 잰다 — 공짜 카드는 누구나 · 산 카드만 매일 · 받기는 카드마다 따로 하루 1회 · 월간은 기간이 지나면 멈춘다.
    /// </para>
    /// </summary>
    public class PrivilegeTests
    {
        const string D1 = "2026-09-09", D2 = "2026-09-10";

        static PrivilegeData Load() => PrivilegeData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "privilege.json"))));

        static SaveData Fresh() => new SaveData();

        [Test]
        public void 주인이_준_여덟_값이_그대로_있다()
        {
            var d = Load();
            Assert.AreEqual(4, d.Cards.Count, "카드 넷");

            var gift = d.Of("dailyGift");
            Assert.IsNotNull(gift, "맨 위 «데일리 기프트» 카드");
            Assert.IsTrue(gift.Free, "데일리 기프트는 사는 카드가 아니다(주인 «그거 다이아 30개 주게 하기»)");
            Assert.AreEqual(0, gift.BuyNow.Count, "공짜 카드에는 «살 때 주는 것» 이 없다");
            Assert.AreEqual(1, gift.Daily.Count);
            Assert.AreEqual(Mail.ItemGem, gift.Daily[0].Item);
            Assert.AreEqual(30, gift.Daily[0].Amount, 0.0001, "하루 다이아 30");

            Check(d, "adRemove", 2400, 50);
            Check(d, "monthly", 600, 300);
            Check(d, "lifetime", 4000, 300);

            Assert.IsTrue(d.Of("monthly").Expires, "월간 카드만 기간이 있다");
            Assert.IsFalse(d.Of("lifetime").Expires, "«평생» 은 기간이 없다");
            Assert.AreEqual(30, d.MonthlyDays, "기간은 주인이 안 줘서 이름값 그대로 30 이 기본(ROUTINE §2 T264 2항)");
        }

        static void Check(PrivilegeData d, string key, double buy, double daily)
        {
            var c = d.Of(key);
            Assert.IsNotNull(c, key);
            Assert.AreEqual(1, c.BuyNow.Count, key + " 는 살 때 한 가지를 준다");
            Assert.AreEqual(Mail.ItemGem, c.BuyNow[0].Item, key + " 즉시 보상은 다이아");
            Assert.AreEqual(buy, c.BuyNow[0].Amount, 0.0001, key + " 구매 즉시");
            Assert.AreEqual(1, c.Daily.Count);
            Assert.AreEqual(Mail.ItemGem, c.Daily[0].Item, key + " 매일 보상은 다이아");
            Assert.AreEqual(daily, c.Daily[0].Amount, 0.0001, key + " 매일");
        }

        [Test]
        public void 공짜_카드는_사지_않아도_하루에_한_번_받는다()
        {
            var d = Load(); var s = Fresh(); var gift = d.Of("dailyGift");
            double before = s.Gem;

            Assert.IsTrue(Privilege.Owned(s, gift), "데일리 기프트는 누구나 가진 것이다");
            Assert.IsTrue(Privilege.Can(s, d, gift, D1));
            Assert.IsNotNull(Privilege.Claim(s, d, gift, D1));
            Assert.AreEqual(before + 30, s.Gem, 0.0001, "다이아 30 이 실제로 들어간다");

            Assert.IsFalse(Privilege.Can(s, d, gift, D1), "같은 날 두 번은 못 받는다");
            Assert.IsNull(Privilege.Claim(s, d, gift, D1));
            Assert.AreEqual(before + 30, s.Gem, 0.0001, "못 받은 회차는 아무것도 안 바꾼다");
            StringAssert.Contains("이미 받았", Privilege.Why(s, d, gift, D1));

            Assert.IsTrue(Privilege.Can(s, d, gift, D2), "날짜가 바뀌면 다시 열린다");
            Assert.IsNotNull(Privilege.Claim(s, d, gift, D2));
            Assert.AreEqual(before + 60, s.Gem, 0.0001);
        }

        [Test]
        public void 안_산_카드는_매일_지급이_없다()
        {
            var d = Load(); var s = Fresh(); var c = d.Of("lifetime");
            Assert.IsFalse(Privilege.Owned(s, c));
            Assert.IsFalse(Privilege.Can(s, d, c, D1));
            Assert.IsNull(Privilege.Claim(s, d, c, D1));
            Assert.AreEqual(0, s.Gem, 0.0001, "안 산 카드는 한 톨도 안 준다");
            StringAssert.Contains("구매", Privilege.Why(s, d, c, D1));
        }

        [Test]
        public void 사면_즉시_보상이_한_번_들어가고_두_번_못_산다()
        {
            var d = Load(); var s = Fresh(); var c = d.Of("adRemove");
            Assert.IsTrue(Privilege.Buy(s, c, D1));
            Assert.AreEqual(2400, s.Gem, 0.0001, "구매 즉시 다이아 2,400");
            Assert.IsTrue(Privilege.Owned(s, c));

            Assert.IsFalse(Privilege.Buy(s, c, D1), "이미 가진 카드는 못 산다");
            Assert.AreEqual(2400, s.Gem, 0.0001, "두 번째 구매는 아무것도 안 바꾼다");

            Assert.IsTrue(Privilege.Can(s, d, c, D1), "산 날에도 그날치 매일 보상을 받는다");
            Privilege.Claim(s, d, c, D1);
            Assert.AreEqual(2450, s.Gem, 0.0001, "즉시 2,400 + 매일 50");
        }

        [Test]
        public void 공짜_카드는_살_수_없다()
        {
            var d = Load(); var s = Fresh();
            Assert.IsFalse(Privilege.Buy(s, d.Of("dailyGift"), D1), "공짜 카드에 «구매» 는 없다");
            Assert.AreEqual(0, s.Gem, 0.0001);
        }

        [Test]
        public void 받기는_카드마다_따로_하루_한_번이다()
        {
            var d = Load(); var s = Fresh();
            Privilege.Buy(s, d.Of("lifetime"), D1);
            s.Gem = 0;   // 즉시 보상은 이 자의 관심이 아니다

            Assert.IsNotNull(Privilege.Claim(s, d, d.Of("dailyGift"), D1));
            Assert.IsTrue(Privilege.Can(s, d, d.Of("lifetime"), D1), "한 카드를 받았다고 다른 카드가 막히면 안 된다(주인 명시)");
            Assert.IsNotNull(Privilege.Claim(s, d, d.Of("lifetime"), D1));
            Assert.AreEqual(330, s.Gem, 0.0001, "기프트 30 + 평생 300");

            Assert.IsFalse(Privilege.AnyClaimable(s, d, D1), "오늘 것은 다 받았다");
            Assert.IsTrue(Privilege.AnyClaimable(s, d, D2), "날짜가 바뀌면 빨간 점이 다시 켜진다");
        }

        [Test]
        public void 월간_카드는_삼십일이_지나면_매일_지급이_멈춘다()
        {
            var d = Load(); var s = Fresh(); var c = d.Of("monthly");
            Privilege.Buy(s, c, "2026-09-01");

            Assert.IsFalse(Privilege.Expired(s, c, d.MonthlyDays, "2026-09-30"), "산 날을 1일차로 세어 30일까지는 산다");
            Assert.IsTrue(Privilege.Can(s, d, c, "2026-09-30"));

            Assert.IsTrue(Privilege.Expired(s, c, d.MonthlyDays, "2026-10-01"), "31일째에 끝난다");
            Assert.IsFalse(Privilege.Can(s, d, c, "2026-10-01"));
            StringAssert.Contains("기간", Privilege.Why(s, d, c, "2026-10-01"));

            Assert.IsFalse(Privilege.Expired(s, d.Of("lifetime"), d.MonthlyDays, "2030-01-01"), "기간 없는 카드는 안 끝난다");
        }

        [Test]
        public void 세이브를_거쳐도_산_것과_받은_날이_남는다()
        {
            var d = Load(); var s = Fresh();
            Privilege.Buy(s, d.Of("monthly"), D1);
            Privilege.Claim(s, d, d.Of("dailyGift"), D1);

            var back = SaveData.FromJson(s.ToJson(), TestData.Load());
            Assert.IsTrue(Privilege.Owned(back, d.Of("monthly")), "산 카드가 살아남는다");
            Assert.AreEqual(D1, Privilege.BoughtOn(back, d.Of("monthly")), "산 날도(월간 기간이 이 값에 달렸다)");
            Assert.AreEqual(D1, Privilege.LastDay(back, d.Of("dailyGift")), "받은 날도");
            Assert.IsFalse(Privilege.Can(back, d, d.Of("dailyGift"), D1), "불러온 뒤에도 오늘 것은 이미 받은 상태다");
        }

        [Test]
        public void 옛_세이브에_이_칸이_없어도_읽힌다()
        {
            var d = Load();
            var back = SaveData.FromJson("{\"v\":2,\"gold\":100}", TestData.Load());
            Assert.IsNotNull(back.PrivBuy); Assert.IsNotNull(back.PrivDay);
            Assert.IsFalse(Privilege.Owned(back, d.Of("monthly")), "안 산 상태");
            Assert.IsTrue(Privilege.Can(back, d, d.Of("dailyGift"), D1), "공짜 카드는 옛 세이브에서도 바로 받을 수 있다");
        }

        [Test]
        public void 담을_자리가_없는_보상은_읽는_순간_운다()
        {
            // 조용히 사라지는 보상을 만들지 않는다(T253·T243 과 같은 갈래).
            Assert.Throws<System.FormatException>(() => PrivilegeData.Parse(
                "{\"monthlyDays\":30,\"cards\":[{\"key\":\"x\",\"name\":\"엑스\",\"daily\":[{\"item\":\"없는것\",\"amount\":1}]}]}"));
            Assert.Throws<System.FormatException>(() => PrivilegeData.Parse(
                "{\"monthlyDays\":30,\"cards\":[{\"key\":\"x\",\"name\":\"엑스\",\"daily\":[]}]}"));
            Assert.Throws<System.FormatException>(() => PrivilegeData.Parse(
                "{\"monthlyDays\":30,\"cards\":[{\"key\":\"x\",\"name\":\"엑스\",\"free\":true,\"buyNow\":[{\"item\":\"gem\",\"amount\":1}],\"daily\":[{\"item\":\"gem\",\"amount\":1}]}]}"));
        }
    }
}
