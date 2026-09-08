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
        // ───────────────────────── T167 하단 탭 점 ─────────────────────────
        // 주인 «장비 쪽에 빨간 점 있는 상황이면 장비 하단 네비에도 · 상점도 · 다른 모든 하단 네비 다 마찬가지로».
        // 판정은 Notify.TabAny 한 곳이므로 그 표를 여기서 못 박는다(화면은 이 답을 보여 주기만 한다).
        static GameData GearOnly()
        {
            var G = new GameData();
            G.Gear = new GearData();
            return G;
        }
        static GearItem Item(string part, string type, int rar) => new GearItem { Part = part, Type = type, Rar = rar };

        [Test]
        public void TabAny_Gear_IsTrueWhenSomethingIsNewOrFusable()
        {
            var G = GearOnly(); var s = Fresh();
            Assert.IsFalse(Notify.GearAny(G, s), "인벤이 비면 장비 탭에 할 일이 없다");

            s.Inv.Add(Item("weapon", "crit", 0));
            Assert.IsFalse(Notify.GearAny(G, s), "한 개만 있고 새것도 아니면 아직 아니다");

            s.Inv[0].IsNew = true;
            Assert.IsTrue(Notify.GearAny(G, s), "안 본 새 장비가 있으면 점이 뜬다");

            s.Inv[0].IsNew = false;
            s.Inv.Add(Item("weapon", "crit", 0)); s.Inv.Add(Item("weapon", "crit", 0));
            Assert.IsTrue(Notify.GearAny(G, s), "같은 묶음 3개 = 합성 가능하면 점이 뜬다(대장간 «자동» 과 같은 판정)");
        }

        [Test]
        public void TabAny_Shop_IsTrueUntilTodaysFreeIsTaken()
        {
            var s = Fresh();
            Assert.IsTrue(Notify.ShopAny(s, Today), "오늘 무료 보급을 아직 안 받았으면 상점 탭에 점이 뜬다");
            s.FreeDay = Today;
            Assert.IsFalse(Notify.ShopAny(s, Today), "받고 나면 꺼진다");
        }

        [Test]
        public void TabAny_PetIsAlwaysOff_AndUnknownKeysAreOff()
        {
            var G = GearOnly(); var s = Fresh();
            Assert.IsFalse(Notify.TabAny("pet", G, s, 0, Today), "펫은 시스템이 없어 늘 꺼짐(껍데기 화면으로 부르지 않는다)");
            Assert.IsFalse(Notify.TabAny("없는탭", G, s, 0, Today), "모르는 키는 꺼짐");
            Assert.IsFalse(Notify.TabAny("gear", null, s, 0, Today), "데이터가 없으면 꺼짐(부팅 중에 안 터진다)");
            Assert.IsFalse(Notify.TabAny("gear", G, null, 0, Today), "세이브가 없으면 꺼짐");
        }

        [Test]
        public void TabAny_RoutesEachKeyToItsOwnRule()
        {
            var G = GearOnly(); var s = Fresh();
            s.FreeDay = Today;                                  // 상점은 껐다
            s.Inv.Add(Item("weapon", "crit", 0)); s.Inv[0].IsNew = true;   // 장비만 켠다
            Assert.IsTrue(Notify.TabAny("gear", G, s, 0, Today), "장비 탭은 장비 규칙을 본다");
            Assert.IsFalse(Notify.TabAny("shop", G, s, 0, Today), "상점 탭은 장비 사정에 안 흔들린다");
        }


        // ── T257 퀘스트 빨간 점 ──────────────────────────────────────────────
        static GameData WithQuest()
        {
            var G = new GameData();
            G.Quest = QuestData.Parse(System.IO.File.ReadAllText(
                TestData.RepoFile(System.IO.Path.Combine("Assets", "KkomaKnight", "quest.json"))));
            return G;
        }

        [Test]
        public void 퀘스트_받을_것이_생기면_점이_켜지고_받으면_꺼진다()
        {
            var G = WithQuest(); var s = Fresh();
            Assert.IsFalse(Notify.QuestClaimable(G, s, Today), "아무것도 안 깼으면 꺼짐");
            QuestRun.Bump(s, "kill", 50);            // 20 점 = 첫 칸이 열린다
            Assert.IsTrue(Notify.QuestClaimable(G, s, Today), "받을 것이 생기면 켜짐");
            Assert.IsTrue(QuestRun.Claim(s, G.Quest, true, 0));
            Assert.IsFalse(Notify.QuestClaimable(G, s, Today), "받고 나면 꺼짐");
        }

        [Test]
        public void 어제_채운_칸은_오늘_점을_켜지_않는다()
        {
            // 이 자가 없으면 «묻기 전에 Roll» 을 빠뜨려도 아무도 모른다 — 날이 바뀌어도 어제 것이 계속 켜져 있는다.
            var G = WithQuest(); var s = Fresh();
            QuestRun.Roll(s, G.Quest, new System.DateTime(2026, 9, 7));
            QuestRun.Bump(s, "kill", 50);
            Assert.IsTrue(Notify.QuestClaimable(G, s, "2026-09-07"), "그날은 켜져 있다");
            Assert.IsFalse(Notify.QuestClaimable(G, s, "2026-09-08"), "날이 바뀌면 셈이 0 이라 꺼진다");
        }

        [Test]
        public void 표가_없거나_날짜_글자가_이상하면_거짓말하지_않는다()
        {
            var s = Fresh();
            Assert.IsFalse(Notify.QuestClaimable(new GameData(), s, Today), "표가 없으면 꺼짐(로드 실패 · 화면도 껍데기다)");
            var G = WithQuest();
            QuestRun.Bump(s, "kill", 50);
            Assert.IsFalse(Notify.QuestClaimable(G, s, ""), "날짜 글자가 비면 꺼짐");
            Assert.IsFalse(Notify.QuestClaimable(G, s, "2026/09/07"), "꼴이 다르면 꺼짐");
        }

    }
}
