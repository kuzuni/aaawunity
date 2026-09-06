using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T96 ⓔ — 빨간 점(알림) 판정을 한 곳(<see cref="Notify"/>)에 모은 것의 계약.
    /// 주인 지시 «광고 보고 획득할 수 있는 재화 있는 경우에도 빨간 점 떠야 함» 이 핵심이라
    /// «받을 것» 과 «광고를 보면 받을 것» 을 따로 세운다.
    /// </summary>
    public class NotifyTests
    {
        const string Today = "2026-09-07";

        static DailyGiftData Gift()
        {
            var d = new DailyGiftData { FreeGem = 100 };
            d.Milestones.Add(new DailyGiftData.Milestone { Ads = 1, Gem = 100 });
            d.Milestones.Add(new DailyGiftData.Milestone { Ads = 2, Gem = 200 });
            return d;
        }
        static SaveData Fresh() => new SaveData();

        [Test]
        public void FreshDay_HasSomethingToClaimAndSomethingToWatch()
        {
            var s = Fresh(); var d = Gift();
            Assert.IsTrue(Notify.DailyGiftClaimable(s, d, Today), "무료 칸이 남아 있다");
            Assert.IsTrue(Notify.DailyGiftAd(s, d, Today), "광고를 보면 받을 줄이 남아 있다");
        }

        [Test]
        public void AfterEveryRowIsClaimed_NoDotIsLeft()
        {
            var s = Fresh(); var d = Gift();
            DailyGift.ClaimFree(s, d, Today);
            for (int i = 0; i < d.Milestones.Count; i++)
            {
                while (!DailyGift.CanClaim(s, d, i, Today) && s.GiftAds < d.MaxAds) DailyGift.WatchAd(s, d, Today);
                DailyGift.Claim(s, d, i, Today);
            }
            Assert.IsFalse(Notify.DailyGiftClaimable(s, d, Today), "다 받았으면 받을 것이 없다");
            Assert.IsFalse(Notify.DailyGiftAd(s, d, Today), "다 받았으면 광고를 봐도 받을 것이 없다");
        }

        [Test]
        public void AdsCapped_ButRowsLeft_IsNotAnAdDot()
        {
            var s = Fresh(); var d = Gift();
            while (s.GiftAds < d.MaxAds) DailyGift.WatchAd(s, d, Today);
            Assert.IsFalse(Notify.DailyGiftAd(s, d, Today), "누적 상한에 닿으면 광고로 더 받을 것이 없다");
            Assert.IsTrue(Notify.DailyGiftClaimable(s, d, Today), "그래도 «받기» 로 받을 줄은 남아 있다");
        }

        [Test]
        public void NullsAreSafe()
        {
            Assert.IsFalse(Notify.DailyGiftAd(null, null, Today));
            Assert.IsFalse(Notify.MenuAny(null, null, 0, Today));
            Assert.IsFalse(Notify.Any(null, null, 0, Today));
            Assert.IsFalse(Notify.AdReward(null, new SaveData(), 0, Today));
        }

        [Test]
        public void MenuDot_FollowsTheDailyGift_WhenItIsTheOnlyItemWithAVerdict()
        {
            var G = new GameData { DailyGift = Gift() };
            var s = Fresh();
            Assert.IsTrue(Notify.MenuAny(G, s, 0, Today), "새 날 = 메뉴에 받을 것이 있다");
            Assert.IsTrue(Notify.Any(G, s, 0, Today));
            DailyGift.ClaimFree(s, G.DailyGift, Today);
            for (int i = 0; i < G.DailyGift.Milestones.Count; i++)
            {
                while (!DailyGift.CanClaim(s, G.DailyGift, i, Today) && s.GiftAds < G.DailyGift.MaxAds) DailyGift.WatchAd(s, G.DailyGift, Today);
                DailyGift.Claim(s, G.DailyGift, i, Today);
            }
            Assert.IsFalse(Notify.MenuAny(G, s, 0, Today), "다 받으면 메뉴 점도 꺼진다");
        }
    }
}
