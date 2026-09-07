using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 던전 티켓·보상 수치표 (<c>Assets/KkomaKnight/dungeon.json</c> · T99 · 주인 2026-09-07).
    /// 값·개수는 전부 파일에서 온다 — 코드 상수 없음(ROUTINE §1 «코드에 게임 수치를 직접 박지 않는다»).
    /// ⚠ 이 수치는 주인이 직접 준 값이라 aaaw 원본(<c>data/*.json</c>)과 다를 수 있다 — 원본은 손대지 않는다(ROUTINE T99 5항).
    /// </summary>
    public sealed class DungeonData
    {
        /// <summary>보상 한 벌(없는 항목은 0 — 원정은 골드만 준다).</summary>
        public sealed class Reward
        {
            /// <summary>펫알 개수(지옥의 문).</summary>
            public double PetEgg;
            public double Gold;
            public bool Any => PetEgg > 0 || Gold > 0;
        }

        public sealed class Entry
        {
            /// <summary>던전 키 — 화면의 <c>Card:&lt;key&gt;</c> 와 같다(hell · expedition).</summary>
            public string Key = "";
            /// <summary>첫 클리어에 받는 <b>합계</b>(보너스가 아니라 총액 · 주인 «첫 클리어 시 총 펫알 11 + 골드 1000»).</summary>
            public Reward First = new Reward();
            /// <summary>두 번째부터의 클리어 보상.</summary>
            public Reward Clear = new Reward();
            /// <summary>소탕(클리어한 층만 가능) 보상.</summary>
            public Reward Sweep = new Reward();
            /// <summary>이 던전으로 «실제 도전» 할 때의 판 규칙(T183 · 주인 2026-09-07 12:0X) — 표는 <c>dungeon.json</c> 의 <c>run</c> 이고 코드에 숫자를 안 박는다.</summary>
            public RunRule Run = new RunRule();
        }

        /// <summary>하루가 바뀌면 <b>이 수 미만일 때만</b> 이 수로 채운다(주인 «2개 미만일 시에 2개로» — 더하지 않는다).</summary>
        /// <summary>
        /// 던전 판 규칙(T183) — 기본값은 <b>일반 챕터 전투와 똑같다</b>(시작 특전 0 · 레벨 1 · 등급 제한 없음).
        /// 그래서 이 표를 안 주면 엔진이 지금과 한 치도 다르게 안 돈다(시드 골든이 그대로인 근거).
        /// </summary>
        public sealed class RunRule
        {
            /// <summary>판을 시작하자마자 자동으로 집어 주는 특전 수(원정 5 · 지옥의 문 0).</summary>
            public int StartPerks;
            /// <summary>시작 레벨(원정 5 · 지옥의 문 1) — 다음 렙업 필요 경험치는 엔진이 <c>ExpNeed(Level)</c> 로 저절로 이 레벨 기준이 된다.</summary>
            public int StartLevel = 1;
            /// <summary>굴릴 수 있는 특전 등급의 <b>하한</b>(0 = 제한 없음 · 2 = 맨 위 등급만 = 주인의 «전설·신화만»).</summary>
            public int MinPerkGrade;
            /// <summary>기본값 그대로인가(= 일반 전투와 같은 판) — 게이트·분기가 이 하나만 본다.</summary>
            public bool IsPlain => StartPerks == 0 && StartLevel <= 1 && MinPerkGrade <= 0;
        }
        public int DailyRefill = 2;
        /// <summary>티켓 1개를 사는 다이아 값.</summary>
        public double GemCost = 50;
        /// <summary>던전마다 하루에 «광고 보고 티켓» 을 받을 수 있는 횟수.</summary>
        public int AdPerDay = 1;
        /// <summary>던전마다 하루에 «다이아로 티켓» 을 살 수 있는 횟수.</summary>
        public int GemPerDay = 1;
        public List<Entry> Dungeons = new List<Entry>();

        public Entry Of(string key)
        {
            if (key == null) return null;
            foreach (var e in Dungeons) if (e.Key == key) return e;
            return null;
        }

        public static DungeonData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static DungeonData From(JNode j)
        {
            var d = new DungeonData();
            d.DailyRefill = (int)j["dailyRefill"].ReqNum("dailyRefill");
            d.GemCost = j["gemCost"].ReqNum("gemCost");
            d.AdPerDay = (int)j["adPerDay"].ReqNum("adPerDay");
            d.GemPerDay = (int)j["gemPerDay"].ReqNum("gemPerDay");
            foreach (var e in j["dungeons"].Items())
            {
                var key = e["key"].Str("");
                if (string.IsNullOrEmpty(key)) throw new FormatException("dungeon.json: dungeons[].key 가 비었다");
                d.Dungeons.Add(new Entry { Key = key, First = Rew(e["first"]), Clear = Rew(e["clear"]), Sweep = Rew(e["sweep"]), Run = Rule(e["run"], key) });
            }
            if (d.Dungeons.Count == 0) throw new FormatException("dungeon.json: dungeons 가 비어 있다");
            if (d.DailyRefill < 0) throw new FormatException("dungeon.json: dailyRefill 은 0 이상이어야 한다");
            if (d.GemCost < 0) throw new FormatException("dungeon.json: gemCost 는 0 이상이어야 한다");
            if (d.AdPerDay < 0 || d.GemPerDay < 0) throw new FormatException("dungeon.json: adPerDay·gemPerDay 는 0 이상이어야 한다");
            return d;
        }
        static Reward Rew(JNode j) => new Reward { PetEgg = j["petEgg"].Num(), Gold = j["gold"].Num() };
        /// <summary>«run» 블록 → <see cref="RunRule"/>(없으면 기본값 = 일반 전투와 같은 판 · T183).</summary>
        static RunRule Rule(JNode j, string key)
        {
            var r = new RunRule { StartPerks = (int)j["startPerks"].Num(), MinPerkGrade = (int)j["minPerkGrade"].Num() };
            int lv = (int)j["startLevel"].Num();
            r.StartLevel = lv > 0 ? lv : 1;
            if (r.StartPerks < 0) throw new FormatException("dungeon.json: " + key + ".run.startPerks 는 0 이상이어야 한다");
            if (r.MinPerkGrade < 0) throw new FormatException("dungeon.json: " + key + ".run.minPerkGrade 는 0 이상이어야 한다");
            return r;
        }
    }

    /// <summary>
    /// 던전 티켓 규칙 (T99 · 순수 C# · 저장은 <see cref="SaveData"/> 의 <c>DunDay/DunTickets/DunAdUsed/DunGemUsed</c> 네 필드).
    /// <list type="bullet">
    /// <item>날짜(<c>yyyy-MM-dd</c> 로컬 · 게임 층의 <c>SaveStore.Today()</c>)가 바뀌면 <see cref="Roll"/> 이 <b>던전마다</b> «보유 &lt; dailyRefill 이면 dailyRefill 로» 채우고 그날의 광고·다이아 횟수를 되돌린다.</item>
    /// <item>보유가 이미 많으면 그대로 둔다 — 더하지 않는다(주인 «2개 미만일 시에 2개로 채워 주는 느낌»).</item>
    /// <item>광고·다이아는 <b>던전별로 각각</b> 하루 <see cref="DungeonData.AdPerDay"/>·<see cref="DungeonData.GemPerDay"/> 번.</item>
    /// </list>
    /// 던전 전투가 없는 껍데기 단계라 «티켓을 쓰는» 길은 아직 없다(소탕·도전은 눌러도 아무 일 없음 · ROUTINE T43 ⓔ).
    /// </summary>
    public static class DungeonTickets
    {
        /// <summary>날짜가 바뀌었으면 티켓을 채우고 하루치 횟수를 되돌린다(하루 첫 접근마다 호출 · 바꿨으면 true).</summary>
        public static bool Roll(SaveData s, DungeonData d, string today)
        {
            if (s == null || d == null) return false;
            Fit(s, d);
            if (s.DunDay == today) return false;
            s.DunDay = today;
            s.DunAdUsed.Clear(); s.DunGemUsed.Clear();
            foreach (var e in d.Dungeons)
                if (Get(s.DunTickets, e.Key) < d.DailyRefill) s.DunTickets[e.Key] = d.DailyRefill;
            return true;
        }

        /// <summary>표에 없는 던전의 낡은 값·음수를 정리한다(표가 바뀌어도 옛 세이브가 깨지지 않게).</summary>
        static void Fit(SaveData s, DungeonData d)
        {
            if (s.DunTickets == null) s.DunTickets = new Dictionary<string, int>();
            if (s.DunAdUsed == null) s.DunAdUsed = new Dictionary<string, int>();
            if (s.DunGemUsed == null) s.DunGemUsed = new Dictionary<string, int>();
            foreach (var e in d.Dungeons)
            {
                if (Get(s.DunTickets, e.Key) < 0) s.DunTickets[e.Key] = 0;
                if (Get(s.DunAdUsed, e.Key) < 0) s.DunAdUsed[e.Key] = 0;
                if (Get(s.DunGemUsed, e.Key) < 0) s.DunGemUsed[e.Key] = 0;
                if (Get(s.DunAdUsed, e.Key) > d.AdPerDay) s.DunAdUsed[e.Key] = d.AdPerDay;
                if (Get(s.DunGemUsed, e.Key) > d.GemPerDay) s.DunGemUsed[e.Key] = d.GemPerDay;
            }
        }

        static int Get(Dictionary<string, int> map, string key) => map != null && key != null && map.TryGetValue(key, out var v) ? v : 0;

        /// <summary>지금 보유한 티켓(날짜 넘김 보충을 먼저 반영한다).</summary>
        public static int Tickets(SaveData s, DungeonData d, string key, string today)
        {
            if (s == null || d == null || d.Of(key) == null) return 0;
            Roll(s, d, today);
            return Get(s.DunTickets, key);
        }

        /// <summary>
        /// 티켓 1 을 쓴다(T183 — «도전» 이 판을 열 때) — 있으면 하나 줄이고 true, 없으면 아무것도 안 하고 false.
        /// 저장은 호출부(게임 층)가 한다(<see cref="ClaimAd"/>·<see cref="BuyGem"/> 과 같은 규약).
        /// </summary>
        public static bool Spend(SaveData s, DungeonData d, string key, string today)
        {
            if (s == null || d == null || d.Of(key) == null) return false;
            Roll(s, d, today);
            int have = Get(s.DunTickets, key);
            if (have <= 0) return false;
            s.DunTickets[key] = have - 1;
            return true;
        }

        /// <summary>오늘 이 던전에서 «광고 보고 티켓» 이 남았는가.</summary>
        public static bool CanAd(SaveData s, DungeonData d, string key, string today)
        {
            if (s == null || d == null || d.Of(key) == null || d.AdPerDay <= 0) return false;
            Roll(s, d, today);
            return Get(s.DunAdUsed, key) < d.AdPerDay;
        }

        /// <summary>광고 1회 → 티켓 +1(하루 <see cref="DungeonData.AdPerDay"/> 번 · 받았으면 true). 저장은 호출부(게임 층)가 한다.</summary>
        public static bool ClaimAd(SaveData s, DungeonData d, string key, string today)
        {
            if (!CanAd(s, d, key, today)) return false;
            s.DunAdUsed[key] = Get(s.DunAdUsed, key) + 1;
            s.DunTickets[key] = Get(s.DunTickets, key) + 1;
            return true;
        }

        /// <summary>오늘 이 던전에서 «다이아로 티켓» 이 남았는가(다이아가 모자라도 «남았다» 는 true — 살 수 있는지는 <see cref="CanBuyGem"/>).</summary>
        public static bool GemLeft(SaveData s, DungeonData d, string key, string today)
        {
            if (s == null || d == null || d.Of(key) == null || d.GemPerDay <= 0) return false;
            Roll(s, d, today);
            return Get(s.DunGemUsed, key) < d.GemPerDay;
        }

        /// <summary>지금 다이아로 티켓을 살 수 있는가(하루치가 남았고 다이아도 충분).</summary>
        public static bool CanBuyGem(SaveData s, DungeonData d, string key, string today) => GemLeft(s, d, key, today) && s.Gem >= d.GemCost;

        /// <summary>다이아 <see cref="DungeonData.GemCost"/> → 티켓 +1(샀으면 true). 저장은 호출부가 한다.</summary>
        public static bool BuyGem(SaveData s, DungeonData d, string key, string today)
        {
            if (!CanBuyGem(s, d, key, today)) return false;
            s.Gem -= d.GemCost;
            s.DunGemUsed[key] = Get(s.DunGemUsed, key) + 1;
            s.DunTickets[key] = Get(s.DunTickets, key) + 1;
            return true;
        }

        /// <summary>이 던전에 «지금 할 일» 이 있는가 = 티켓이 있거나 광고로 하나 받을 수 있다(빨간 점 · ROUTINE T99 6항).</summary>
        public static bool Ready(SaveData s, DungeonData d, string key, string today) => Tickets(s, d, key, today) > 0 || CanAd(s, d, key, today);

        /// <summary>던전 가운데 하나라도 «지금 할 일» 이 있는가(로비·하단 탭의 빨간 점).</summary>
        public static bool AnyReady(SaveData s, DungeonData d, string today)
        {
            if (s == null || d == null) return false;
            foreach (var e in d.Dungeons) if (Ready(s, d, e.Key, today)) return true;
            return false;
        }
    }
}
