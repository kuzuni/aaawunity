using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 데일리 기프트(T77 · 주인 2026-09-07) — 수치표(<c>Assets/KkomaKnight/dailyGift.json</c>)와 규칙(<see cref="DailyGift"/>).
    /// 주인이 값을 바꿀 수 있으므로 «표가 주인 지시와 같은가» 는 한 테스트에만 두고, 나머지는 값이 아니라 «꼴»(순서 잠금 · 중복 수령 불가 · 매일 초기화)을 본다.
    /// </summary>
    public class DailyGiftTests
    {
        const string D0 = "2026-09-07", D1 = "2026-09-08";

        static DailyGiftData Load() => DailyGiftData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "dailyGift.json"))));
        static SaveData NewSave() => SaveData.NewSave(TestData.Load());

        /// <summary>무료 칸까지 받아 줄 0 을 연 상태.</summary>
        static void OpenFirstRow(SaveData s, DailyGiftData d)
        {
            DailyGift.Roll(s, d, D0);
            DailyGift.ClaimFree(s, d, D0);
        }

        [Test]
        public void Json_IsOwnersTable()
        {
            // 주인 원문(2026-09-07): «광고 1회 = 다이아 100 / 2회 = 200 / 3회 선물 = 300 / 6회 선물 = 300» + 추가 «무료 1칸 = 다이아 100» · 매일 초기화.
            var d = Load();
            Assert.That(d.ResetDaily, Is.True, "매일 초기화");
            Assert.That(d.FreeGem, Is.EqualTo(100), "무료 «오늘의 선물» 칸 = 다이아 100");
            Assert.That(d.Milestones.Count, Is.EqualTo(4), "광고 줄 4개");
            Assert.That(d.Milestones[0].Ads, Is.EqualTo(1)); Assert.That(d.Milestones[0].Gem, Is.EqualTo(100));
            Assert.That(d.Milestones[1].Ads, Is.EqualTo(2)); Assert.That(d.Milestones[1].Gem, Is.EqualTo(200));
            Assert.That(d.Milestones[2].Ads, Is.EqualTo(3)); Assert.That(d.Milestones[2].Gem, Is.EqualTo(300)); Assert.That(d.Milestones[2].Gift, Is.True, "3회 = «선물» 줄");
            Assert.That(d.Milestones[3].Ads, Is.EqualTo(6)); Assert.That(d.Milestones[3].Gem, Is.EqualTo(300)); Assert.That(d.Milestones[3].Gift, Is.True, "6회 = «선물» 줄");
            Assert.That(d.MaxAds, Is.EqualTo(6), "하루 광고 상한 = 마지막 줄");
            Assert.That(d.MaxGemPerDay, Is.EqualTo(1000), "하루 최대 = 무료 100 + 100 + 200 + 300 + 300");
        }

        [Test]
        public void Json_MilestonesArePositiveAndAscending()
        {
            var d = Load();
            for (int i = 0; i < d.Milestones.Count; i++)
            {
                Assert.That(d.Milestones[i].Ads, Is.GreaterThan(0)); Assert.That(d.Milestones[i].Gem, Is.GreaterThan(0));
                if (i > 0) Assert.That(d.Milestones[i].Ads, Is.GreaterThan(d.Milestones[i - 1].Ads), "누적 광고 횟수 오름차순");
            }
        }

        [Test]
        public void FreeGift_OncePerDay()
        {
            var d = Load(); var s = NewSave();
            Assert.That(DailyGift.CanFree(s, d, D0), Is.True, "하루 첫 무료 칸");
            Assert.That(DailyGift.ClaimFree(s, d, D0), Is.EqualTo(d.FreeGem));
            Assert.That(s.Gem, Is.EqualTo(d.FreeGem));
            Assert.That(DailyGift.CanFree(s, d, D0), Is.False, "같은 날 두 번은 못 받는다");
            Assert.That(DailyGift.ClaimFree(s, d, D0), Is.EqualTo(0));
            Assert.That(s.Gem, Is.EqualTo(d.FreeGem), "중복 수령으로 다이아가 늘지 않는다");
        }

        [Test]
        public void Rows_LockedUntilFreeGiftClaimed()
        {
            // 주인 추가(00:3X): «줄은 위에서 아래로 순서대로 잠금» — 줄 0 은 무료 칸을 받아야 열린다.
            var d = Load(); var s = NewSave();
            for (int i = 0; i < d.Milestones.Count; i++) DailyGift.WatchAd(s, d, D0);
            Assert.That(DailyGift.Locked(s, d, 0, D0), Is.True, "무료 칸 전에는 줄 0 도 잠김");
            Assert.That(DailyGift.CanClaim(s, d, 0, D0), Is.False);
            DailyGift.ClaimFree(s, d, D0);
            Assert.That(DailyGift.Locked(s, d, 0, D0), Is.False, "무료 칸을 받으면 줄 0 이 열린다");
        }

        [Test]
        public void Rows_UnlockTopDownOneByOne()
        {
            var d = Load(); var s = NewSave();
            OpenFirstRow(s, d);
            for (int k = 0; k < d.MaxAds; k++) DailyGift.WatchAd(s, d, D0);   // 누적은 끝까지 채워도
            for (int i = 1; i < d.Milestones.Count; i++)
                Assert.That(DailyGift.CanClaim(s, d, i, D0), Is.False, $"줄 {i} 는 앞 줄을 받기 전엔 못 받는다");
            for (int i = 0; i < d.Milestones.Count; i++)
            {
                Assert.That(DailyGift.CanClaim(s, d, i, D0), Is.True, $"앞 줄을 받았으니 줄 {i} 가 열린다");
                Assert.That(DailyGift.Claim(s, d, i, D0), Is.EqualTo(d.Milestones[i].Gem));
            }
        }

        [Test]
        public void Claim_NeedsEnoughAdsAndOnlyOnce()
        {
            var d = Load(); var s = NewSave();
            OpenFirstRow(s, d);
            Assert.That(DailyGift.CanClaim(s, d, 0, D0), Is.False, "광고 0회 = 아직");
            DailyGift.WatchAd(s, d, D0);
            Assert.That(DailyGift.CanClaim(s, d, 0, D0), Is.True);
            double got = DailyGift.Claim(s, d, 0, D0);
            Assert.That(got, Is.EqualTo(d.Milestones[0].Gem));
            Assert.That(DailyGift.Claim(s, d, 0, D0), Is.EqualTo(0), "같은 줄 두 번은 못 받는다");
            Assert.That(s.Gem, Is.EqualTo(d.FreeGem + d.Milestones[0].Gem));
        }

        [Test]
        public void Ads_StopAtLastMilestone()
        {
            var d = Load(); var s = NewSave();
            for (int k = 0; k < d.MaxAds + 5; k++) DailyGift.WatchAd(s, d, D0);
            Assert.That(s.GiftAds, Is.EqualTo(d.MaxAds), "누적은 마지막 줄에서 멈춘다");
        }

        [Test]
        public void MaxGemPerDay_IsTableSum()
        {
            var d = Load(); var s = NewSave();
            DailyGift.Roll(s, d, D0);
            DailyGift.ClaimFree(s, d, D0);
            for (int k = 0; k < d.MaxAds; k++) DailyGift.WatchAd(s, d, D0);
            for (int i = 0; i < d.Milestones.Count; i++) DailyGift.Claim(s, d, i, D0);
            Assert.That(s.Gem, Is.EqualTo(d.MaxGemPerDay), "하루 최대 = 무료 + 모든 줄(표 합계)");
            Assert.That(DailyGift.ClaimedGem(s, d), Is.EqualTo(d.MaxGemPerDay));
            Assert.That(DailyGift.AnyClaimable(s, d, D0), Is.False, "다 받으면 빨간 점 없음");
        }

        [Test]
        public void NewDay_ResetsAdsAndClaims()
        {
            var d = Load(); var s = NewSave();
            DailyGift.Roll(s, d, D0); DailyGift.ClaimFree(s, d, D0);
            DailyGift.WatchAd(s, d, D0); DailyGift.Claim(s, d, 0, D0);
            double gemAfterDay0 = s.Gem;
            Assert.That(DailyGift.Roll(s, d, D1), Is.True, "날짜가 바뀌면 초기화");
            Assert.That(s.GiftAds, Is.EqualTo(0));
            Assert.That(s.GiftFree, Is.False);
            Assert.That(DailyGift.Claimed(s, 0), Is.False);
            Assert.That(s.Gem, Is.EqualTo(gemAfterDay0), "초기화가 다이아를 건드리지 않는다");
            Assert.That(DailyGift.CanFree(s, d, D1), Is.True, "새 날 무료 칸 다시");
        }

        [Test]
        public void AnyClaimable_IsTheRedDotRule()
        {
            var d = Load(); var s = NewSave();
            Assert.That(DailyGift.AnyClaimable(s, d, D0), Is.True, "무료 칸이 남아 있으면 빨간 점");
            DailyGift.ClaimFree(s, d, D0);
            Assert.That(DailyGift.AnyClaimable(s, d, D0), Is.False, "받을 게 없으면 점 없음");
            DailyGift.WatchAd(s, d, D0);
            Assert.That(DailyGift.AnyClaimable(s, d, D0), Is.True, "줄 0 이 열렸고 누적이 닿았다");
        }

        [Test]
        public void OldSave_WithoutGiftFields_LoadsAndWorks()
        {
            // 옛 세이브(이 필드가 없는 JSON) 는 기본값으로 열려야 한다 — index.html 세이브 v2 호환(Speed·FreeDay 와 같은 방식).
            var D = TestData.Load();
            var s = SaveData.FromJson("{\"v\":2,\"gold\":10,\"gem\":5,\"maxChapter\":1,\"selChapter\":1}", D);
            Assert.That(s.GiftDay, Is.EqualTo(""));
            Assert.That(s.GiftAds, Is.EqualTo(0));
            Assert.That(s.GiftFree, Is.False);
            Assert.That(s.GiftClaimed, Is.Not.Null);
            var d = Load();
            Assert.That(DailyGift.Roll(s, d, D0), Is.True);
            Assert.That(s.GiftClaimed.Count, Is.EqualTo(d.Milestones.Count), "수령 표 길이를 표에 맞춘다");
            Assert.That(DailyGift.CanFree(s, d, D0), Is.True);
        }

        [Test]
        public void Save_RoundTripsGiftFields()
        {
            var D = TestData.Load(); var d = Load();
            var s = SaveData.NewSave(D);
            DailyGift.Roll(s, d, D0); DailyGift.ClaimFree(s, d, D0);
            DailyGift.WatchAd(s, d, D0); DailyGift.WatchAd(s, d, D0);
            DailyGift.Claim(s, d, 0, D0);
            var back = SaveData.FromJson(s.ToJson(), D);
            Assert.That(back.GiftDay, Is.EqualTo(D0));
            Assert.That(back.GiftAds, Is.EqualTo(2));
            Assert.That(back.GiftFree, Is.True);
            Assert.That(back.GiftClaimed.Count, Is.EqualTo(d.Milestones.Count));
            Assert.That(DailyGift.Claimed(back, 0), Is.True);
            Assert.That(DailyGift.Claimed(back, 1), Is.False);
            Assert.That(DailyGift.CanClaim(back, d, 1, D0), Is.True, "다시 켜도 이어서 받는다");
        }

        [Test]
        public void ClaimedRowCount_ShrinksAndGrowsWithTable()
        {
            // 표 줄 수가 바뀌어도(주인이 dailyGift.json 을 고쳐도) 옛 수령 표가 그대로 남거나 넘치지 않는다.
            var D = TestData.Load(); var s = SaveData.NewSave(D);
            var small = DailyGiftData.Parse("{\"freeGift\":{\"gem\":10},\"milestones\":[{\"ads\":1,\"gem\":10}]}");
            DailyGift.Roll(s, small, D0);
            Assert.That(s.GiftClaimed.Count, Is.EqualTo(1));
            var big = Load();
            DailyGift.Roll(s, big, D0);
            Assert.That(s.GiftClaimed.Count, Is.EqualTo(big.Milestones.Count));
        }
    }
}
