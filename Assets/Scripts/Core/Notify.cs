namespace KkomaKnight.Core
{
    /// <summary>
    /// «지금 받을 수 있는 것이 있는가» — 빨간 점(알림)의 판정을 **한 곳**에 모은다 (T96 ⓔ · 주인 2026-09-07
    /// «광고 보고 획득할 수 있는 재화 있는 경우에도 빨간 점 떠야 함. 알림.»).
    /// 화면(로비 메뉴 ≡ · 사이드 아이콘 · 메뉴 항목)은 여기만 보고 점을 켠다 — 판정이 화면마다 갈리지 않게.
    /// 순수 C# 이라 EditMode 에서 그대로 돈다. 아직 실물이 아닌 항목(우편함 T96-mail · 출석 · 퀘스트 · 특권)은
    /// 세이브에 «받았다» 상태 자체가 없어 **거짓말하지 않고 false** 로 둔다 — 실물이 되는 커밋이 여기 한 줄씩 더한다.
    /// </summary>
    public static class Notify
    {
        /// <summary>데일리 기프트에 지금 «받기» 로 받을 것이 있는가(무료 칸 + 누적이 닿은 줄).</summary>
        public static bool DailyGiftClaimable(SaveData s, DailyGiftData d, string today)
            => DailyGift.AnyClaimable(s, d, today);

        /// <summary>
        /// 데일리 기프트에서 **광고를 보면** 받을 수 있는 다이아가 남았는가 —
        /// 오늘 누적이 상한(<see cref="DailyGiftData.MaxAds"/>)에 안 닿았고 아직 안 받은 줄이 남아 있다.
        /// </summary>
        public static bool DailyGiftAd(SaveData s, DailyGiftData d, string today)
        {
            if (s == null || d == null || d.Milestones.Count == 0) return false;
            DailyGift.Roll(s, d, today);
            if (s.GiftAds >= d.MaxAds) return false;
            for (int i = 0; i < d.Milestones.Count; i++) if (!DailyGift.Claimed(s, i)) return true;
            return false;
        }

        /// <summary>탐험에 쌓인 보상이 있거나 빠른 탐험(광고) 횟수가 남았는가.</summary>
        public static bool ExpeditionClaimable(GameData G, SaveData s, ExpeditionData d, double nowSec, string today)
            => G != null && d != null && Expedition.AnyClaimable(G, s, d, nowSec, today);

        /// <summary>
        /// **광고를 보면 받을 수 있는 재화**가 어디든 남았는가(주인 지시의 핵심) —
        /// 데일리 기프트의 광고 줄 + 빠른 탐험(하루 <see cref="ExpeditionData.QuickAdsPerDay"/>회).
        /// </summary>
        public static bool AdReward(GameData G, SaveData s, double nowSec, string today)
        {
            if (G == null || s == null) return false;
            if (DailyGiftAd(s, G.DailyGift, today)) return true;
            return G.Expedition != null && Expedition.CanQuick(s, G.Expedition, nowSec, today);
        }

        /// <summary>
        /// 로비 메뉴(≡)에 점을 켤 것인가 = 메뉴가 품은 항목 가운데 하나라도 «지금 받을 수 있는 것» 이 있는가.
        /// 지금 메뉴가 품는 것은 우편함 · 설정 · 데일리 기프트 · 퀘스트 · 출석 · 특권 이고,
        /// 그중 판정이 있는 것은 데일리 기프트(수령 + 광고)뿐이다.
        /// </summary>
        public static bool MenuAny(GameData G, SaveData s, double nowSec, string today)
        {
            if (G == null || s == null) return false;
            return DailyGiftClaimable(s, G.DailyGift, today) || DailyGiftAd(s, G.DailyGift, today);
        }

        /// <summary>화면 어디든 지금 받을 수 있는 것이 있는가(메뉴 + 로비에 남은 탐험 · 광고 재화 전부).</summary>
        public static bool Any(GameData G, SaveData s, double nowSec, string today)
        {
            if (G == null || s == null) return false;
            return MenuAny(G, s, nowSec, today)
                || ExpeditionClaimable(G, s, G.Expedition, nowSec, today)
                || AdReward(G, s, nowSec, today);
        }
    }
}
