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
            /// <summary>받는 다이아.</summary>
            public double Gem;
            /// <summary>레퍼런스 17 의 «선물» 줄(표시용 · 규칙에는 영향 없음).</summary>
            public bool Gift;
        }
        /// <summary>날짜가 바뀌면 누적·수령을 초기화하는가(주인 «매일 초기화»).</summary>
        public bool ResetDaily = true;
        /// <summary>«오늘의 선물» 무료 1칸 다이아(광고 없이 하루 1회 · 주인 확정 2026-09-07 00:3X).</summary>
        public double FreeGem;
        public List<Milestone> Milestones = new List<Milestone>();

        /// <summary>하루에 셀 수 있는 광고 상한 = 마지막 줄의 누적 횟수(그 위로는 세지 않는다).</summary>
        public int MaxAds => Milestones.Count == 0 ? 0 : Milestones[Milestones.Count - 1].Ads;
        /// <summary>하루 최대 다이아(무료 칸 + 모든 줄).</summary>
        public double MaxGemPerDay { get { double g = FreeGem; foreach (var m in Milestones) g += m.Gem; return g; } }

        public static DailyGiftData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static DailyGiftData From(JNode j)
        {
            var d = new DailyGiftData();
            d.ResetDaily = j.Has("resetDaily") ? j["resetDaily"].Bool(true) : true;
            d.FreeGem = j["freeGift"]["gem"].Num();
            foreach (var m in j["milestones"].Items())
                d.Milestones.Add(new Milestone { Ads = (int)m["ads"].ReqNum("milestones.ads"), Gem = m["gem"].ReqNum("milestones.gem"), Gift = m["gift"].Bool() });
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
            if (s == null || d == null || d.FreeGem <= 0) return false;
            Roll(s, d, today);
            return !s.GiftFree;
        }

        /// <summary>무료 칸 수령 — 받은 다이아(0 이면 못 받음). 저장은 호출부(게임 층)가 한다.</summary>
        public static double ClaimFree(SaveData s, DailyGiftData d, string today)
        {
            if (!CanFree(s, d, today)) return 0;
            s.GiftFree = true; s.Gem += d.FreeGem;
            return d.FreeGem;
        }

        /// <summary>줄 <paramref name="i"/> 가 «앞 줄 미수령» 으로 잠겨 있는가(줄 0 은 무료 칸을 받아야 열린다).</summary>
        public static bool Locked(SaveData s, DailyGiftData d, int i, string today)
        {
            if (s == null || d == null || i < 0 || i >= d.Milestones.Count) return true;
            Roll(s, d, today);
            if (i == 0) return d.FreeGem > 0 && !s.GiftFree;
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
            s.GiftClaimed[i] = true; s.Gem += d.Milestones[i].Gem;
            return d.Milestones[i].Gem;
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
            double g = s.GiftFree ? d.FreeGem : 0;
            for (int i = 0; i < d.Milestones.Count; i++) if (Claimed(s, i)) g += d.Milestones[i].Gem;
            return g;
        }
    }
}
