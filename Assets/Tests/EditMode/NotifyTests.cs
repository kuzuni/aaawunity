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
        public void TabAny_Gear_IsTrueWhenSlotCanBeUpgradedOrSomethingIsFusable()
        {
            var G = GearOnly(); var s = Fresh();
            Assert.IsFalse(Notify.GearAny(G, s), "인벤이 비면 장비 탭에 할 일이 없다");

            s.Inv.Add(Item("weapon", "crit", 0));
            Assert.IsFalse(Notify.GearAny(G, s), "한 개만 있고 합성도 안 되면 아직 아니다");

            // T357(주인 2026-09-10 «슬롯 강화할 부분도 없는데 빨간점 안 꺼지더라») — «안 본 새 장비» 는 이제 점을 안 켠다.
            //   그 갈래만 스스로 안 꺼졌다(세부 팝업을 열어야 꺼진다) — 뽑기로 여럿 얻고 안 열어 보면 점이 영영 켜져 있었다.
            s.Inv[0].IsNew = true;
            Assert.IsFalse(Notify.GearAny(G, s),
                "«안 본 새 장비» 만으로는 점이 안 뜬다 — 스스로 안 꺼지는 조건은 «할 일» 이 아니라 «지워지지 않는 자국» 이다(T357)");

            s.Inv[0].IsNew = false;
            s.Inv.Add(Item("weapon", "crit", 0)); s.Inv.Add(Item("weapon", "crit", 0));
            Assert.IsTrue(Notify.GearAny(G, s), "같은 묶음 3개 = 합성 가능하면 점이 뜬다(대장간 «자동» 과 같은 판정)");
        }

        /// <summary>
        /// T357 — 주인이 이 점을 읽는 뜻(«슬롯 강화할 부분»)이 실제로 점을 켜고, <b>강화하고 나면 꺼지는가</b>.
        /// <para>재는 것이 «켜진다» 만이면 늘 켜져 있는 코드도 통과한다 — 이 절이 고친 고장이 바로 그 꼴이므로 <b>끄는 쪽</b>을 같이 잰다.</para>
        /// </summary>
        [Test]
        public void TabAny_Gear_TurnsOffOnceTheSlotHasBeenUpgraded()
        {
            var G = GearOnly(); var s = Fresh();
            G.Gear.Parts = new[] { "weapon" };
            //   ⚠ 표를 두 칸 준다 — 한 칸만 주면 Lv 1 의 값이 표 밖이라 `SlotCostBase·G`(둘 다 0)로 떨어져
            //     «공짜로 또 강화된다» 가 되고, 그러면 아래 «꺼진다» 가 내 fixture 때문에 빨개진다(실제로 한 번 그랬다).
            G.Gear.SlotLvMax = 5; G.Gear.SlotCostTable = new double[] { 100, 500 };
            s.Gold = 0;
            Assert.IsFalse(Notify.GearAny(G, s), "골드가 없으면 강화할 수 없으니 점이 꺼져 있다");

            s.Gold = 100;
            Assert.IsTrue(Notify.GearAny(G, s), "강화할 골드가 생기면 점이 뜬다 — 주인이 이 점을 그렇게 읽는다");

            Assert.IsTrue(GearSystem.SlotUp(G, s, "weapon", out _), "강화가 실제로 된다");
            Assert.IsFalse(Notify.GearAny(G, s),
                "강화하고 나면 골드가 빠져 점이 저절로 꺼진다 — «사용자가 그 일을 하면 꺼진다» 가 이 점의 조건이다(T357)");
        }

        [Test]
        public void TabAny_Shop_IsTrueUntilTodaysFreeIsTaken()
        {
            var s = Fresh();
            Assert.IsTrue(Notify.ShopAny(s, Today), "오늘 무료 보급을 아직 안 받았으면 상점 탭에 점이 뜬다");
            // T355(주인 2026-09-10) — 다이아 자리(옛 FreeDay)만 받아도 골드·광고 상자가 남았으면 점은 켜져 있다(옛 판정이 여기서 거짓이었다)
            s.FreeDay = Today;
            Assert.IsTrue(Notify.ShopAny(s, Today), "다이아만 받았다 — 나머지 셋이 남았으니 아직 켜진다(T355)");
            foreach (var t in ShopFree.All) ShopFree.Take(s, t, Today);   // T355 — 옛 FreeDay(다이아 자리 하나)가 아니라 무료 보급 자리 넷을 다 써야 꺼진다
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
            foreach (var t in ShopFree.All) ShopFree.Take(s, t, Today);   // T355 — 옛 FreeDay(다이아 자리 하나)가 아니라 무료 보급 자리 넷을 다 써야 꺼진다                                  // 상점은 껐다
            // T357 — 장비를 켜는 방법을 «안 본 새 장비» 에서 «합성 가능» 으로 바꿨다.
            //   이 자가 재는 것은 «gear 키가 장비 규칙으로 가는가»(라우팅)이지 무엇이 점을 켜는가가 아니다 —
            //   그 «무엇» 은 위 두 자가 따로 잰다. 켜는 방법이 바뀌었으니 여기서는 **살아 있는 조건** 으로 켠다.
            for (int i = 0; i < 3; i++) s.Inv.Add(Item("weapon", "crit", 0));   // 장비만 켠다(합성 가능)
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
