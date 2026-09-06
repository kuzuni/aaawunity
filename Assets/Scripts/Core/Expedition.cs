using System;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 탐험 수치표 (<c>Assets/KkomaKnight/expedition.json</c> · T97 · 주인 2026-09-07).
    /// 값·상한은 전부 파일에서 온다 — 코드 상수 없음(<see cref="DailyGiftData"/> 와 같은 방식).
    /// </summary>
    public sealed class ExpeditionData
    {
        /// <summary>쌓이는 상한(시간) — 레퍼런스 30 «Max Explore Time: 8h». 넘으면 그대로 멈춘다.</summary>
        public double MaxHours = 8;
        /// <summary>방치 1시간 = 그 챕터 적 몇 마리 처치분 골드인가.</summary>
        public double GoldKillsPerHour;
        /// <summary>sim.js <c>goldKill</c> 의 <c>rand(1,1.8)</c> 평균 — 방치는 난수 없이 이 평균값으로 준다.</summary>
        public double GoldRandAvg = 1.4;
        /// <summary>시간당 다이아(챕터와 무관한 고정 · 레퍼런스 30 «10/h»).</summary>
        public double GemPerHour;
        /// <summary>«받기» 가 열리는 최소 누적(분) — 그 전에는 레퍼런스 31 처럼 «다음까지 mm:ss» 를 보여 준다.</summary>
        public double MinClaimMinutes = 1;
        /// <summary>빠른 탐험 한 번이 주는 시간(레퍼런스 31 «5 hours»).</summary>
        public double QuickHours = 5;
        /// <summary>빠른 탐험 하루 횟수(레퍼런스 31 의 버튼 배지 «3»).</summary>
        public int QuickAdsPerDay = 3;

        public double MaxSeconds => MaxHours * 3600.0;
        public double MinClaimSeconds => MinClaimMinutes * 60.0;

        public static ExpeditionData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static ExpeditionData From(JNode j)
        {
            var d = new ExpeditionData();
            d.MaxHours = j["maxHours"].ReqNum("maxHours");
            d.GoldKillsPerHour = j["goldKillsPerHour"].ReqNum("goldKillsPerHour");
            d.GoldRandAvg = j["goldRandAvg"].ReqNum("goldRandAvg");
            d.GemPerHour = j["gemPerHour"].ReqNum("gemPerHour");
            d.MinClaimMinutes = j.Has("minClaimMinutes") ? j["minClaimMinutes"].Num() : 1;
            d.QuickHours = j["quickHours"].ReqNum("quickHours");
            d.QuickAdsPerDay = (int)j["quickAdsPerDay"].ReqNum("quickAdsPerDay");
            if (d.MaxHours <= 0) throw new FormatException("expedition.json: maxHours 는 0 보다 커야 한다");
            if (d.QuickHours <= 0) throw new FormatException("expedition.json: quickHours 는 0 보다 커야 한다");
            if (d.QuickAdsPerDay < 0) throw new FormatException("expedition.json: quickAdsPerDay 는 0 이상이어야 한다");
            return d;
        }
    }

    /// <summary>
    /// 탐험(방치·오프라인 보상) 규칙 (T97 · 순수 C# · 저장은 <see cref="SaveData"/> 의 <c>ExpSettle/ExpQuickDay/ExpQuickUsed</c> 세 필드).
    /// <list type="bullet">
    /// <item><b>켜 두든 꺼 두든 같은 속도로 쌓인다</b>(주인) — 저장하는 것은 «마지막 정산 시각»(UTC 초) 하나이고, 쌓인 양은 «(지금 − 마지막) × 시간당 비율» 로 <b>열 때·받을 때 계산</b>한다.</item>
    /// <item>상한 <see cref="ExpeditionData.MaxHours"/> 를 넘으면 멈춘다 — 기기 시계를 미래로 돌려도 8시간치 위로는 못 받는다.</item>
    /// <item>시계를 뒤로 돌리면(지금 &lt; 마지막) 음수가 되지 않게 마지막 정산 시각을 지금으로 당긴다(그 사이 것은 사라진다 · 되돌림 이득 0).</item>
    /// <item>빠른 탐험은 <b>누적에 더하지 않고 즉시 지급</b>한다(중복 수령 방지) · 하루 횟수는 날짜가 바뀌면 초기화.</item>
    /// </list>
    /// 시간은 게임 층이 준다(<c>nowSec</c> = UTC 유닉스 초) — 순수 C# 규칙이라 여기서 시계를 읽지 않는다.
    /// </summary>
    public static class Expedition
    {
        /// <summary>세이브의 «마지막 정산 시각» 이 아직 없으면 지금으로, 미래면 지금으로 당긴다(시계 되돌림 방어). 날짜가 바뀌었으면 빠른 탐험 횟수를 초기화한다.</summary>
        public static void Roll(SaveData s, ExpeditionData d, double nowSec, string today)
        {
            if (s == null) return;
            if (s.ExpSettle <= 0 || s.ExpSettle > nowSec) s.ExpSettle = nowSec;   // 첫 실행 · 시계 되돌림
            if (s.ExpQuickDay != today) { s.ExpQuickDay = today; s.ExpQuickUsed = 0; }
            if (s.ExpQuickUsed < 0) s.ExpQuickUsed = 0;
            if (d != null && s.ExpQuickUsed > d.QuickAdsPerDay) s.ExpQuickUsed = d.QuickAdsPerDay;
        }

        /// <summary>쌓인 시간(초 · 상한까지). 열 때·받을 때·1초 갱신마다 부른다.</summary>
        public static double ElapsedSec(SaveData s, ExpeditionData d, double nowSec, string today)
        {
            if (s == null || d == null) return 0;
            Roll(s, d, nowSec, today);
            double e = nowSec - s.ExpSettle;
            if (e < 0) e = 0;
            if (e > d.MaxSeconds) e = d.MaxSeconds;
            return e;
        }

        /// <summary>시간당 골드 — 진행 챕터(<see cref="SaveData.MaxChapter"/>)의 처치 골드 결정부 × 난수 평균 × 시간당 처치 수.</summary>
        public static double GoldPerHour(GameData G, SaveData s, ExpeditionData d)
        {
            if (G == null || s == null || d == null) return 0;
            int c = s.MaxChapter < 1 ? 1 : s.MaxChapter;
            return G.Tune.GoldKillBaseAt(c) * d.GoldRandAvg * d.GoldKillsPerHour;
        }

        /// <summary>시간당 다이아(챕터와 무관한 고정).</summary>
        public static double GemPerHour(ExpeditionData d) => d == null ? 0 : d.GemPerHour;

        /// <summary>지금까지 쌓여 «받을 수 있는» 골드·다이아(정수로 내림 — 화면 숫자와 지급이 같아야 한다).</summary>
        public static void Pending(GameData G, SaveData s, ExpeditionData d, double nowSec, string today, out double gold, out double gem)
        {
            double h = ElapsedSec(s, d, nowSec, today) / 3600.0;
            gold = Math.Floor(GoldPerHour(G, s, d) * h);
            gem = Math.Floor(GemPerHour(d) * h);
        }

        /// <summary>«받기» 를 지금 누를 수 있는가 = 최소 누적(분)을 넘겼고 받을 것이 1 이상 있다.</summary>
        public static bool CanClaim(GameData G, SaveData s, ExpeditionData d, double nowSec, string today)
        {
            if (G == null || s == null || d == null) return false;
            if (ElapsedSec(s, d, nowSec, today) < d.MinClaimSeconds) return false;
            Pending(G, s, d, nowSec, today, out double gold, out double gem);
            return gold >= 1 || gem >= 1;
        }

        /// <summary>«받기» — 쌓인 골드·다이아를 주고 마지막 정산 시각을 지금으로. 못 받으면 0(저장은 호출부가 한다).</summary>
        public static void Claim(GameData G, SaveData s, ExpeditionData d, double nowSec, string today, out double gold, out double gem)
        {
            gold = 0; gem = 0;
            if (!CanClaim(G, s, d, nowSec, today)) return;
            Pending(G, s, d, nowSec, today, out gold, out gem);
            s.Gold += gold; s.Gem += gem;
            s.ExpSettle = nowSec;
        }

        /// <summary>«받기» 까지 남은 초(0 이면 지금 받을 수 있다) — 레퍼런스 31 의 «Claim in: mm:ss».</summary>
        public static double SecondsToClaim(SaveData s, ExpeditionData d, double nowSec, string today)
        {
            if (s == null || d == null) return 0;
            double left = d.MinClaimSeconds - ElapsedSec(s, d, nowSec, today);
            return left > 0 ? left : 0;
        }

        /// <summary>오늘 남은 빠른 탐험 횟수(레퍼런스 31 버튼 배지).</summary>
        public static int QuickLeft(SaveData s, ExpeditionData d, double nowSec, string today)
        {
            if (s == null || d == null) return 0;
            Roll(s, d, nowSec, today);
            int left = d.QuickAdsPerDay - s.ExpQuickUsed;
            return left > 0 ? left : 0;
        }

        public static bool CanQuick(SaveData s, ExpeditionData d, double nowSec, string today) => QuickLeft(s, d, nowSec, today) > 0;

        /// <summary>빠른 탐험 한 번이 주는 골드·다이아(= 시간당 비율 × <see cref="ExpeditionData.QuickHours"/> · 레퍼런스 31 «가능한 보상» 칸).</summary>
        public static void QuickReward(GameData G, SaveData s, ExpeditionData d, out double gold, out double gem)
        {
            double h = d == null ? 0 : d.QuickHours;
            gold = Math.Floor(GoldPerHour(G, s, d) * h);
            gem = Math.Floor(GemPerHour(d) * h);
        }

        /// <summary>빠른 탐험 수령(광고를 다 본 뒤에 부른다) — <b>누적에 더하지 않고 즉시 지급</b>하고 오늘 횟수를 하나 쓴다. 못 쓰면 0.</summary>
        public static void ClaimQuick(GameData G, SaveData s, ExpeditionData d, double nowSec, string today, out double gold, out double gem)
        {
            gold = 0; gem = 0;
            if (!CanQuick(s, d, nowSec, today)) return;
            QuickReward(G, s, d, out gold, out gem);
            s.Gold += gold; s.Gem += gem;
            s.ExpQuickUsed++;
        }

        /// <summary>로비 아이콘 빨간 점 — 받을 것이 있거나(누적) 빠른 탐험 횟수가 남았다(ROUTINE T97 5항).</summary>
        public static bool AnyClaimable(GameData G, SaveData s, ExpeditionData d, double nowSec, string today)
            => CanClaim(G, s, d, nowSec, today) || CanQuick(s, d, nowSec, today);
    }
}
