using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 데일리 기프트 수치표 (<c>Assets/KkomaKnight/dailyGift.json</c> · T77 · 주인 2026-09-07).
    /// 하루 누적 광고 횟수가 <see cref="Milestone.Ads"/> 에 닿으면 그 줄의 다이아를 «받기» 로 받는다. 값·개수는 전부 파일에서 온다 — 코드 상수 없음.
    /// </summary>
    public sealed class DailyGiftData
    {
        public sealed class Milestone
        {
            /// <summary>이 줄이 열리는 «하루 누적 광고 횟수».</summary>
            public int Ads;
            /// <summary>
            /// 받는 다이아 — <b>옛 이름이자 짧은 길</b>. 읽으면 «이 줄이 다이아일 때의 개수»(아니면 0)이고,
            /// 쓰면 «다이아 그만큼» 이 된다(<see cref="Item"/>·<see cref="Amount"/> 를 같이 맞춘다).
            /// T254 로 칸마다 다른 것을 주게 됐는데, 이 이름으로 세워 둔 자리(자·화면)가 여럿이라 그대로 살려 둔다.
            /// </summary>
            public double Gem
            {
                get => Item == Mail.ItemGem ? Amount : 0;
                set { Item = Mail.ItemGem; Amount = value; }
            }
            /// <summary>받는 것의 <b>이름</b>(<see cref="Mail"/> 의 재화 이름 · 적혀 있지 않으면 다이아).
            /// T254 — 주인이 «50다이아 / 5펫알 / 1보라키 / 1부활 / 300다이아» 로 칸마다 다른 것을 못 박아서 이 칸이 생겼다.</summary>
            public string Item = Mail.ItemGem;
            /// <summary>받는 <b>개수</b>(다이아면 <see cref="Gem"/> 과 같은 수다).</summary>
            public double Amount;
            /// <summary>레퍼런스 17 의 «선물» 줄(표시용 · 규칙에는 영향 없음).</summary>
            public bool Gift;
        }
        /// <summary>날짜가 바뀌면 누적·수령을 초기화하는가(주인 «매일 초기화»).</summary>
        public bool ResetDaily = true;
        /// <summary>«오늘의 선물» 무료 1칸 다이아 — <b>옛 이름이자 짧은 길</b>(<see cref="Milestone.Gem"/> 과 같은 규약).</summary>
        public double FreeGem
        {
            get => FreeItem == Mail.ItemGem ? FreeAmount : 0;
            set { FreeItem = Mail.ItemGem; FreeAmount = value; }
        }
        /// <summary>무료 1칸이 주는 것의 이름·개수(T254 · 적혀 있지 않으면 다이아).</summary>
        public string FreeItem = Mail.ItemGem;
        /// <summary>무료 1칸 개수.</summary>
        public double FreeAmount;
        /// <summary>무료 칸이 있는가 — 개수가 0 이면 없는 것으로 본다(옛 <see cref="FreeGem"/> 판정을 이름에 상관없이 쓰게).</summary>
        public bool HasFree => FreeAmount > 0;
        public List<Milestone> Milestones = new List<Milestone>();

        /// <summary>하루에 셀 수 있는 광고 상한 = 마지막 줄의 누적 횟수(그 위로는 세지 않는다).</summary>
        public int MaxAds => Milestones.Count == 0 ? 0 : Milestones[Milestones.Count - 1].Ads;
        /// <summary>하루 최대 다이아(무료 칸 + 모든 줄).</summary>
        public double MaxGemPerDay { get { double g = FreeGem; foreach (var m in Milestones) g += m.Gem; return g; } }
        /// <summary>줄 <paramref name="i"/> 가 주는 것(<see cref="Milestone.Item"/>·<see cref="Milestone.Amount"/>) — 화면·리워드 팝업이 쓴다.</summary>
        public Milestone Row(int i) => i >= 0 && i < Milestones.Count ? Milestones[i] : null;

        public static DailyGiftData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static DailyGiftData From(JNode j)
        {
            var d = new DailyGiftData();
            d.ResetDaily = j.Has("resetDaily") ? j["resetDaily"].Bool(true) : true;
            var fg = j["freeGift"];
            d.FreeItem = fg["item"].Str(Mail.ItemGem);
            if (string.IsNullOrEmpty(d.FreeItem)) d.FreeItem = Mail.ItemGem;
            d.FreeAmount = fg.Has("amount") ? fg["amount"].Num() : fg["gem"].Num();
            foreach (var m in j["milestones"].Items())
            {
                string item = m["item"].Str(Mail.ItemGem);
                if (string.IsNullOrEmpty(item)) item = Mail.ItemGem;
                double amt = m.Has("amount") ? m["amount"].Num() : m["gem"].ReqNum("milestones.gem");
                d.Milestones.Add(new Milestone
                {
                    Ads = (int)m["ads"].ReqNum("milestones.ads"),
                    Item = item, Amount = amt,
                    Gift = m["gift"].Bool(),
                });
            }
            // 담을 자리가 없는 이름은 읽는 순간 운다 — 그대로 두면 «받았는데 아무것도 안 늘어나는» 칸이 된다(결정 633 과 같은 갈래).
            if (!Mail.CanPay(d.FreeItem) && d.FreeAmount > 0) throw new FormatException("dailyGift.json: freeGift.item «" + d.FreeItem + "» 은 담을 자리가 없다");
            foreach (var m in d.Milestones)
                if (!Mail.CanPay(m.Item)) throw new FormatException("dailyGift.json: milestones.item «" + m.Item + "» 은 담을 자리가 없다");
            if (d.Milestones.Count == 0) throw new FormatException("dailyGift.json: milestones 가 비어 있다");
            for (int i = 1; i < d.Milestones.Count; i++)
                if (d.Milestones[i].Ads <= d.Milestones[i - 1].Ads) throw new FormatException("dailyGift.json: milestones.ads 는 오름차순이어야 한다");
            return d;
        }
    }

    /// <summary>
    /// 데일리 기프트 규칙 (T77 · 순수 C# · 저장은 <see cref="SaveData"/> 의 <c>GiftDay/GiftAds/GiftFree/GiftClaimed</c> 네 필드).
    /// <list type="bullet">
    /// <item>날짜(<c>yyyy-MM-dd</c> 로컬 · 게임 층의 <c>SaveStore.Today()</c>)가 바뀌면 <see cref="Roll"/> 이 누적·수령을 초기화한다.</item>
    /// <item>«오늘의 선물» 무료 1칸을 먼저 받아야 줄 1 이 열리고, 줄 i 는 줄 i−1 을 받아야 열린다(주인 추가 2026-09-07 00:3X «위에서 아래로 순서대로»).</item>
    /// <item>광고는 잠긴 줄에서도 누적된다 — 누적만 되고 받기는 순서대로(ROUTINE T77 7항 · 결정 기록).</item>
    /// </list>
    /// </summary>
    public static class DailyGift
    {
        /// <summary>날짜가 바뀌었으면 초기화한다(하루 첫 접근마다 호출 · 초기화했으면 true).</summary>
        public static bool Roll(SaveData s, DailyGiftData d, string today)
        {
            if (s == null || d == null) return false;
            if (s.GiftDay == today) { Fit(s, d); return false; }
            if (!d.ResetDaily && !string.IsNullOrEmpty(s.GiftDay)) { Fit(s, d); return false; }
            s.GiftDay = today; s.GiftAds = 0; s.GiftFree = false;
            s.GiftClaimed = new List<bool>();
            Fit(s, d);
            return true;
        }

        /// <summary>수령 표의 길이를 표 개수에 맞춘다(줄을 늘리거나 줄여도 옛 세이브가 깨지지 않게).</summary>
        static void Fit(SaveData s, DailyGiftData d)
        {
            if (s.GiftClaimed == null) s.GiftClaimed = new List<bool>();
            while (s.GiftClaimed.Count < d.Milestones.Count) s.GiftClaimed.Add(false);
            while (s.GiftClaimed.Count > d.Milestones.Count) s.GiftClaimed.RemoveAt(s.GiftClaimed.Count - 1);
            if (s.GiftAds < 0) s.GiftAds = 0;
            if (s.GiftAds > d.MaxAds) s.GiftAds = d.MaxAds;
        }

        public static bool Claimed(SaveData s, int i) => s != null && s.GiftClaimed != null && i >= 0 && i < s.GiftClaimed.Count && s.GiftClaimed[i];

        /// <summary>무료 «오늘의 선물» 칸을 받을 수 있는가.</summary>
        public static bool CanFree(SaveData s, DailyGiftData d, string today)
        {
            if (s == null || d == null || !d.HasFree) return false;
            Roll(s, d, today);
            return !s.GiftFree;
        }

        /// <summary>무료 칸 수령 — 받은 다이아(0 이면 못 받음). 저장은 호출부(게임 층)가 한다.</summary>
        public static double ClaimFree(SaveData s, DailyGiftData d, string today)
        {
            if (!CanFree(s, d, today)) return 0;
            s.GiftFree = true;
            Mail.Give(s, d.FreeItem, d.FreeAmount);   // T254 — 이름 → 담는 자리의 짝은 Mail 한 곳이다(다이아만이 아니게 됐다)
            return d.FreeAmount;
        }

        /// <summary>줄 <paramref name="i"/> 가 «앞 줄 미수령» 으로 잠겨 있는가(줄 0 은 무료 칸을 받아야 열린다).</summary>
        public static bool Locked(SaveData s, DailyGiftData d, int i, string today)
        {
            if (s == null || d == null || i < 0 || i >= d.Milestones.Count) return true;
            Roll(s, d, today);
            if (i == 0) return d.HasFree && !s.GiftFree;
            return !Claimed(s, i - 1);
        }

        /// <summary>광고 1회 시청 — 누적을 올린다(상한 = <see cref="DailyGiftData.MaxAds"/>). 잠긴 줄에서도 누적은 된다.</summary>
        public static int WatchAd(SaveData s, DailyGiftData d, string today)
        {
            if (s == null || d == null) return 0;
            Roll(s, d, today);
            if (s.GiftAds < d.MaxAds) s.GiftAds++;
            return s.GiftAds;
        }

        /// <summary>줄 <paramref name="i"/> 를 지금 받을 수 있는가 = 열려 있고 · 누적이 닿았고 · 아직 안 받았다.</summary>
        public static bool CanClaim(SaveData s, DailyGiftData d, int i, string today)
        {
            if (s == null || d == null || i < 0 || i >= d.Milestones.Count) return false;
            Roll(s, d, today);
            if (Locked(s, d, i, today)) return false;
            return s.GiftAds >= d.Milestones[i].Ads && !Claimed(s, i);
        }

        /// <summary>줄 <paramref name="i"/> 수령 — 받은 다이아(0 이면 못 받음). 저장은 호출부가 한다.</summary>
        public static double Claim(SaveData s, DailyGiftData d, int i, string today)
        {
            if (!CanClaim(s, d, i, today)) return 0;
            var m = d.Milestones[i];
            s.GiftClaimed[i] = true;
            Mail.Give(s, m.Item, m.Amount);   // T254 — 다이아만이 아니다(펫알·보라 키·부활권)
            return m.Amount;
        }

        /// <summary>지금 받을 수 있는 것이 하나라도 있는가(로비 사이드 아이콘 빨간 점).</summary>
        public static bool AnyClaimable(SaveData s, DailyGiftData d, string today)
        {
            if (s == null || d == null) return false;
            if (CanFree(s, d, today)) return true;
            for (int i = 0; i < d.Milestones.Count; i++) if (CanClaim(s, d, i, today)) return true;
            return false;
        }

        /// <summary>오늘 이미 받은 다이아 합계(테스트·표시용).</summary>
        public static double ClaimedGem(SaveData s, DailyGiftData d)
        {
            if (s == null || d == null) return 0;
            double g = s.GiftFree && d.FreeItem == Mail.ItemGem ? d.FreeGem : 0;
            for (int i = 0; i < d.Milestones.Count; i++) if (Claimed(s, i)) g += d.Milestones[i].Gem;
            return g;
        }
    }
}
