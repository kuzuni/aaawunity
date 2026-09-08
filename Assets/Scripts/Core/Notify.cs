using System.Collections.Generic;

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
        /// 데일리 기프트의 광고 줄 + 빠른 탐험(<b>보유 충전</b> · <see cref="ExpeditionData.QuickChargeHours"/>시간마다 1회 · 최대 <see cref="ExpeditionData.QuickMax"/> · T265).
        /// </summary>
        public static bool AdReward(GameData G, SaveData s, double nowSec, string today)
        {
            if (G == null || s == null) return false;
            if (DailyGiftAd(s, G.DailyGift, today)) return true;
            return G.Expedition != null && Expedition.CanQuick(s, G.Expedition, nowSec, today);
        }

        /// <summary>
        /// «데일리 기프트에 지금 받을 것이 있는가»(수령 + 광고) — 이름은 T96-menu 때 메뉴가 그것을 품고 있어서 붙었다.
        /// <para>⚠ <b>T148 로 데일리 기프트가 로비로 돌아가</b> ≡ 점은 더 이상 이것을 안 본다(우편함만 본다 · <c>LobbyScreen.Refresh</c>).
        /// 이 함수는 <see cref="Any"/>(화면 어디든 받을 것이 있는가)의 한 조각으로 그대로 남는다 — 데일리 기프트는 여전히 받을 수 있기 때문이다.</para>
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

        /// <summary>
        /// 장비 탭에 «지금 할 일» 이 있는가(T167) — ⓐ 아직 안 본 새 장비(<see cref="GearItem.IsNew"/>)가 있거나
        /// ⓑ 합성 가능한 묶음이 있다(같은 <see cref="GearSystem.FuseKey"/> 3개 이상 = 대장간 «자동» 점과 **같은 판정**).
        /// 화면(장비·대장간)이 제각각 세지 않게 판정을 여기 한 곳에 둔다(T96 ⓔ 규약).
        /// </summary>
        public static bool GearAny(GameData G, SaveData s)
        {
            if (s == null || s.Inv == null) return false;
            var cnt = new Dictionary<string, int>();
            foreach (var g in s.Inv)
            {
                if (g == null) continue;
                if (g.IsNew) return true;
                var k = GearSystem.FuseKey(G, g);
                int c = (cnt.TryGetValue(k, out var v) ? v : 0) + 1; cnt[k] = c;
                if (c >= 3) return true;
            }
            return false;
        }

        /// <summary>상점 탭 — 오늘 «무료 보급»(하루 1회)을 아직 안 받았다(<c>ShopScreen.CanFree</c> 와 같은 판정).</summary>
        public static bool ShopAny(SaveData s, string today) => s != null && s.FreeDay != today;

        /// <summary>
        /// 하단 탭 <paramref name="key"/> 에 빨간 점을 켜야 하는가 (T167 · 주인 2026-09-07 09:3X
        /// «장비 쪽에 빨간 점 있는 상황이면 장비 하단 네비에도 · 상점도 · 다른 모든 하단 네비 다 마찬가지로»).
        /// <para>
        /// «그 탭에 들어가면 꺼지는 것» 이 아니라 <b>조건이 사라져야 꺼진다</b>(주인 문장 그대로 «있는 상황이면 뜬다») —
        /// 그래서 «봤다» 를 적는 새 세이브 칸을 만들지 않는다(지시서 4항).
        /// </para>
        /// 펫은 시스템이 아직 없어 **늘 꺼짐**이다(껍데기 화면에 점을 켜면 눌러도 할 일이 없다) — 시스템이 생기면 여기 한 줄이 는다.
        /// </summary>
        public static bool TabAny(string key, GameData G, SaveData s, double nowSec, string today)
        {
            if (G == null || s == null) return false;
            switch (key)
            {
                case "gear": return GearAny(G, s);
                case "shop": return ShopAny(s, today);
                case "battle": return Any(G, s, nowSec, today);          // 전투 = 로비 · 로비 안에서 켜지는 것들의 합
                case "events": return DungeonTickets.AnyReady(s, G.Dungeon, today);   // 던전 티켓이 있거나 광고로 받을 수 있다(T99 6항과 같은 판정)
                default: return false;                                    // pet — 시스템 없음
            }
        }
    }
}
