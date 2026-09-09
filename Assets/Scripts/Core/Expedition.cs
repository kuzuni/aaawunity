using System;
using System.Collections.Generic;

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
        /// <summary>빠른 탐험 하루 횟수 — <b>T265 로 안 쓴다</b>(충전제로 바뀌었다 · 되돌릴 때만 되살린다).</summary>
        public int QuickAdsPerDay = 3;
        /// <summary>
        /// 빠른 탐험 <b>한 번이 차는 데 걸리는 시간</b> — <b>2시간</b>(주인 2026-09-09 08:2X «3시간에 한 번이라던 거 2시간으로 하자» · T270 이 T265 의 3시간을 정정).
        /// <para>되돌리려면 <b>여기와 <c>expedition.json</c> 의 <c>quickChargeHours</c> 두 곳</b>만 3 으로 되돌린다 — 화면 글자·카운트다운은 이 값을 읽어 쓰므로 저절로 따라온다.</para>
        /// </summary>
        public double QuickChargeHours = 2;
        /// <summary>빠른 탐험 <b>최대 보유</b>(주인 «3번 받을 수 있는 거임» · 레퍼런스 31 버튼 배지 «3»).</summary>
        public int QuickMax = 3;
        /// <summary>
        /// <b>시간당 레시피</b>(T321 · 주인 2026-09-09 12:0X «탐험 보상으로 1시간에 1개씩 레시피 중 하나 드랍되게 · 투구, 무기 그런 식의 장비 부위 레시피»).
        /// 부위는 <b>여섯 중 균등 무작위</b>이고 <b>받는 순간</b> 정해진다(<see cref="QuestRun.RollRecipes"/> 와 같은 규칙 한 벌).
        /// <para>
        /// <b>0 이면 레시피가 아예 안 나온다</b> — 지금 0 인 까닭은 <b>주는 화면이 아직 없기 때문</b>이다.
        /// 탐험 팝업(<c>LobbyPopups</c>)이 남의 lock 안이라 «받기» 가 아직 난수를 안 넘긴다(<see cref="Claim(GameData, SaveData, ExpeditionData, double, string, out double, out double)"/> 참조).
        /// 화면 회차가 이 수를 1 로 올린다 — T290 의 <c>perLevel</c> 이 밟은 그 순서다.
        /// </para>
        /// </summary>
        public double RecipePerHour;
        public double QuickChargeSeconds => QuickChargeHours * 3600.0;

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
            // T265 — 충전제. 옛 표(quickAdsPerDay 만 있는 것)도 그대로 읽히게 «없으면 기본값» 이다.
            d.QuickAdsPerDay = j.Has("quickAdsPerDay") ? (int)j["quickAdsPerDay"].Num() : 3;
            d.QuickChargeHours = j.Has("quickChargeHours") ? j["quickChargeHours"].Num() : 2;   // 기본값도 2 로(T270 · 표에 키가 없던 옛 판을 읽을 때만 쓰인다)
            d.QuickMax = j.Has("quickMax") ? (int)j["quickMax"].Num() : 3;
            d.RecipePerHour = j.Has("recipePerHour") ? j["recipePerHour"].Num() : 0;   // T321 — 없으면 0(옛 표 호환 · 레시피가 안 나온다)
            if (d.RecipePerHour < 0) throw new FormatException("expedition.json: recipePerHour 는 0 이상이어야 한다");
            if (d.MaxHours <= 0) throw new FormatException("expedition.json: maxHours 는 0 보다 커야 한다");
            if (d.QuickHours <= 0) throw new FormatException("expedition.json: quickHours 는 0 보다 커야 한다");
            if (d.QuickAdsPerDay < 0) throw new FormatException("expedition.json: quickAdsPerDay 는 0 이상이어야 한다");
            // 이 둘이 0 이면 «영영 안 찬다»·«한 번도 못 쓴다» 가 되는데, 화면에는 그저 «0회» 로만 보여 아무도 못 찾는다.
            if (d.QuickChargeHours <= 0) throw new FormatException("expedition.json: quickChargeHours 는 0 보다 커야 한다");
            if (d.QuickMax <= 0) throw new FormatException("expedition.json: quickMax 는 0 보다 커야 한다");
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
        /// <summary>세이브의 «마지막 정산 시각» 이 아직 없으면 지금으로, 미래면 지금으로 당긴다(시계 되돌림 방어).
        /// 빠른 탐험은 <b>충전</b>을 굴린다(T265) — 지난 시간만큼 채우고 상한에서 멈춘다.</summary>
        public static void Roll(SaveData s, ExpeditionData d, double nowSec, string today)
        {
            if (s == null) return;
            if (s.ExpSettle <= 0 || s.ExpSettle > nowSec) s.ExpSettle = nowSec;   // 첫 실행 · 시계 되돌림
            RollQuick(s, d, nowSec);
        }

        /// <summary>
        /// 빠른 탐험 충전 (T265 · 주인 원문 «3시간에 한 번씩 초기화 · 3시간에 한 번씩 3번 받을 수 있는 거임» → <b>주기는 T270 에서 2시간으로 정정됐다</b> · 횟수 3 은 그대로).
        /// <list type="bullet">
        /// <item><b>옛 세이브·첫 실행은 «가득» 으로 시작</b>한다(<c>ExpQuickAt == 0</c>). 0 으로 시작하면 여태 하루 3회를 쓰던 사람에게서
        /// 아무 말 없이 아홉 시간을 빼앗는 셈이라, 규칙이 바뀔 때 손해 보는 쪽이 사람이 되면 안 된다(결정 기록).</item>
        /// <item><b>한 주기가 지나면 보유가 상한이 된다</b>(T270 ⓐ · 주인 «2시간마다 3개 전부 리필») — 두 주기가 지나도 3 이고, 1 남았을 때도 3 이다(4 가 아니다).</item>
        /// <item><b>꽉 차 있으면 기준 시각을 지금으로 붙든다</b> — 안 그러면 하루 꽉 차 있던 사람이 한 번 쓰는 순간 남은 여덟 칸이 한꺼번에 들어온다.</item>
        /// <item>시계를 뒤로 돌리면(지금 &lt; 기준) 기준을 지금으로 당긴다 — 되돌림 이득 0(누적 쪽과 같은 규약).</item>
        /// </list>
        /// </summary>
        public static void RollQuick(SaveData s, ExpeditionData d, double nowSec)
        {
            if (s == null || d == null) return;
            if (s.ExpQuickCharge < 0) s.ExpQuickCharge = 0;
            int max = d.QuickMax;
            if (s.ExpQuickAt <= 0)                      // 첫 실행 · 옛 세이브 → 가득 채우고 시작한다
            {
                s.ExpQuickCharge = max;
                s.ExpQuickAt = nowSec;
                return;
            }
            if (s.ExpQuickAt > nowSec) s.ExpQuickAt = nowSec;                 // 시계 되돌림
            if (s.ExpQuickCharge > max) s.ExpQuickCharge = max;
            double per = d.QuickChargeSeconds;
            // T270 ⓐ 정정(주인 2026-09-09 «빠른 탐험은 **2시간마다 3개 전부 리필** · 1개씩 충전 아님») —
            // 한 주기가 지나면 «한 칸» 이 아니라 **보유가 상한이 된다**. 1 남았든 0 이든 결과는 같은 3 이고, 넘치지도 않는다.
            // 그래서 남는 시간을 이월할 것이 없다(가득 차면 아래에서 기준 시각을 «지금» 으로 붙든다 = 다음 주기는 «다 쓴 순간» 부터).
            if (s.ExpQuickCharge < max && per > 0 && nowSec - s.ExpQuickAt >= per) s.ExpQuickCharge = max;
            if (s.ExpQuickCharge >= max) s.ExpQuickAt = nowSec;                // 꽉 차면 «다 쓰는 순간» 부터 다시 센다
        }

        /// <summary>다음 한 칸이 찰 때까지 남은 초. 꽉 차 있으면 <b>0</b>(화면은 «충전 완료» 로 적는다 · T265 3항).</summary>
        public static double NextQuickSec(SaveData s, ExpeditionData d, double nowSec)
        {
            if (s == null || d == null) return 0;
            RollQuick(s, d, nowSec);
            if (s.ExpQuickCharge >= d.QuickMax) return 0;
            double left = d.QuickChargeSeconds - (nowSec - s.ExpQuickAt);
            if (left < 0) left = 0;
            return left;
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

        /// <summary>
        /// 지금까지 쌓여 «받을 수 있는» <b>레시피 개수</b>(T321 · 내림 — 화면 숫자와 지급이 같아야 한다).
        /// 부위는 여기서 안 정한다 — <b>받는 순간</b> 난수가 정한다(<see cref="Claim(GameData, SaveData, ExpeditionData, double, string, RecipeData, IRng, out double, out double, out Dictionary{string, int})"/>).
        /// </summary>
        public static int RecipesPending(SaveData s, ExpeditionData d, double nowSec, string today)
        {
            if (s == null || d == null || d.RecipePerHour <= 0) return 0;
            double n = Math.Floor(d.RecipePerHour * (ElapsedSec(s, d, nowSec, today) / 3600.0));
            return n <= 0 ? 0 : (n > int.MaxValue ? int.MaxValue : (int)n);
        }

        /// <summary>«받기» 를 지금 누를 수 있는가 = 최소 누적(분)을 넘겼고 받을 것이 1 이상 있다(골드·다이아·레시피 중 하나라도).</summary>
        public static bool CanClaim(GameData G, SaveData s, ExpeditionData d, double nowSec, string today)
        {
            if (G == null || s == null || d == null) return false;
            if (ElapsedSec(s, d, nowSec, today) < d.MinClaimSeconds) return false;
            Pending(G, s, d, nowSec, today, out double gold, out double gem);
            return gold >= 1 || gem >= 1 || RecipesPending(s, d, nowSec, today) >= 1;   // T321
        }

        /// <summary>
        /// «받기»(<b>옛 서명</b> · 난수를 안 받는다) — 쌓인 골드·다이아를 주고 마지막 정산 시각을 지금으로. 못 받으면 0(저장은 호출부가 한다).
        /// <para>
        /// ⚠ <b>T321 — 레시피가 쌓여 있으면 이 서명은 아무것도 안 준다</b>(0 · 정산 시각도 안 건드린다).
        /// 까닭: 이 갈래가 골드·다이아만 주고 <see cref="SaveData.ExpSettle"/> 을 지금으로 밀면 <b>쌓인 레시피가 조용히 사라진다</b> —
        /// 빨간 줄도 자도 안 나고 주인 폰에서만 «레시피가 안 오는데?» 로 나타난다(워커 E 가 T292 에서 «반쪽으로 주느니 안 준다» 로 적은 그 자리).
        /// <b>멈추는 쪽으로 틀리게</b> 둔 것이다 — 표를 1 로 켜는 회차가 부르는 쪽을 아래 서명으로 안 바꾸면 «받기가 안 된다» 로 시끄럽게 드러난다(조용히 먹지 않는다).
        /// 표가 <c>recipePerHour 0</c> 인 동안은 이 갈래가 예전과 정확히 같다.
        /// </para>
        /// </summary>
        public static void Claim(GameData G, SaveData s, ExpeditionData d, double nowSec, string today, out double gold, out double gem)
        {
            gold = 0; gem = 0;
            if (RecipesPending(s, d, nowSec, today) >= 1) return;   // T321 — 위 주석: 반쪽으로 주느니 안 준다
            if (!CanClaim(G, s, d, nowSec, today)) return;
            Pending(G, s, d, nowSec, today, out gold, out gem);
            s.Gold += gold; s.Gem += gem;
            s.ExpSettle = nowSec;
        }

        /// <summary>
        /// «받기»(T321 · <b>레시피까지</b>) — 골드·다이아에 더해 쌓인 레시피를 <b>부위 무작위</b>로 준다.
        /// <paramref name="recipes"/> 는 부위별 개수(리워드 팝업이 «투구 레시피 ×2 …» 로 묶어 그리는 값 · 없으면 빈 표).
        /// <para>난수·표가 없으면 레시피 갈래만 0 이고 골드·다이아는 그대로 준다 — 단 <b>줄 것이 있는데 못 주는</b> 판이면 아무것도 안 준다(위 옛 서명과 같은 규칙).</para>
        /// </summary>
        public static void Claim(GameData G, SaveData s, ExpeditionData d, double nowSec, string today,
                                 RecipeData rd, IRng rng, out double gold, out double gem, out Dictionary<string, int> recipes)
        {
            gold = 0; gem = 0; recipes = new Dictionary<string, int>();
            int n = RecipesPending(s, d, nowSec, today);
            if (n >= 1 && (rd == null || rng == null)) return;   // 줄 것이 있는데 줄 길이 없다 — 정산 시각을 안 밀어 다음 기회에 그대로 남는다
            if (!CanClaim(G, s, d, nowSec, today)) return;
            Pending(G, s, d, nowSec, today, out gold, out gem);
            if (n >= 1)
            {
                recipes = QuestRun.RollRecipes(rd, rng, n);
                foreach (var kv in recipes) Recipes.Add(s, kv.Key, kv.Value);
            }
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

        /// <summary>지금 쓸 수 있는 빠른 탐험 횟수 = <b>보유 충전</b>(레퍼런스 31 버튼 배지 · T265 로 «하루 남은 횟수» 에서 바뀌었다).</summary>
        public static int QuickLeft(SaveData s, ExpeditionData d, double nowSec, string today)
        {
            if (s == null || d == null) return 0;
            Roll(s, d, nowSec, today);
            return s.ExpQuickCharge > 0 ? s.ExpQuickCharge : 0;
        }

        public static bool CanQuick(SaveData s, ExpeditionData d, double nowSec, string today) => QuickLeft(s, d, nowSec, today) > 0;

        /// <summary>빠른 탐험 한 번이 주는 골드·다이아(= 시간당 비율 × <see cref="ExpeditionData.QuickHours"/> · 레퍼런스 31 «가능한 보상» 칸).</summary>
        public static void QuickReward(GameData G, SaveData s, ExpeditionData d, out double gold, out double gem)
        {
            double h = d == null ? 0 : d.QuickHours;
            gold = Math.Floor(GoldPerHour(G, s, d) * h);
            gem = Math.Floor(GemPerHour(d) * h);
        }

        /// <summary>빠른 탐험 한 번이 주는 <b>레시피 개수</b>(= <see cref="ExpeditionData.QuickHours"/> 시간분 · T321 2항 «빠른 탐험도 레시피 5개»).</summary>
        public static int QuickRecipes(ExpeditionData d)
        {
            if (d == null || d.RecipePerHour <= 0) return 0;
            double n = Math.Floor(d.RecipePerHour * d.QuickHours);
            return n <= 0 ? 0 : (int)n;
        }

        /// <summary>
        /// 빠른 탐험 수령(<b>옛 서명</b> · 광고를 다 본 뒤에 부른다) — <b>누적에 더하지 않고 즉시 지급</b>하고 충전을 하나 쓴다. 못 쓰면 0.
        /// <para>⚠ T321 — 줄 레시피가 있는 판이면 <b>아무것도 안 준다</b>(충전도 안 쓴다). 까닭은 <see cref="Claim(GameData, SaveData, ExpeditionData, double, string, out double, out double)"/> 와 같다.</para>
        /// </summary>
        public static void ClaimQuick(GameData G, SaveData s, ExpeditionData d, double nowSec, string today, out double gold, out double gem)
        {
            gold = 0; gem = 0;
            if (QuickRecipes(d) >= 1) return;   // T321 — 반쪽으로 주느니 안 준다(충전은 그대로 남는다)
            if (!CanQuick(s, d, nowSec, today)) return;
            QuickReward(G, s, d, out gold, out gem);
            s.Gold += gold; s.Gem += gem;
            // 쓰는 순간이 다음 충전의 시작이다 — Roll 이 꽉 찬 동안 기준을 지금으로 붙들어 두므로 여기서 시각을 안 만져도 된다.
            s.ExpQuickCharge--;
        }

        /// <summary>빠른 탐험 수령(T321 · <b>레시피까지</b>) — <paramref name="recipes"/> 는 부위별 개수(리워드 팝업이 묶어 그리는 값).</summary>
        public static void ClaimQuick(GameData G, SaveData s, ExpeditionData d, double nowSec, string today,
                                      RecipeData rd, IRng rng, out double gold, out double gem, out Dictionary<string, int> recipes)
        {
            gold = 0; gem = 0; recipes = new Dictionary<string, int>();
            int n = QuickRecipes(d);
            if (n >= 1 && (rd == null || rng == null)) return;   // 줄 것이 있는데 줄 길이 없다 — 충전을 안 쓴다
            if (!CanQuick(s, d, nowSec, today)) return;
            QuickReward(G, s, d, out gold, out gem);
            if (n >= 1)
            {
                recipes = QuestRun.RollRecipes(rd, rng, n);
                foreach (var kv in recipes) Recipes.Add(s, kv.Key, kv.Value);
            }
            s.Gold += gold; s.Gem += gem;
            s.ExpQuickCharge--;
        }

        /// <summary>로비 아이콘 빨간 점 — 받을 것이 있거나(누적) 빠른 탐험 횟수가 남았다(ROUTINE T97 5항).</summary>
        public static bool AnyClaimable(GameData G, SaveData s, ExpeditionData d, double nowSec, string today)
            => CanClaim(G, s, d, nowSec, today) || CanQuick(s, d, nowSec, today);
    }
}
