using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 특권(11) <b>카드표</b>(<c>Assets/KkomaKnight/privilege.json</c> · T264 · 주인 2026-09-09 06:2X).
    /// <para>
    /// 카드 넷 — «데일리 기프트»(공짜 · 하루 다이아 30) · «광고 제거»(2,400 / 매일 50) ·
    /// «월간 카드»(600 / 매일 300 · 기간 있음) · «평생 다이아»(4,000 / 매일 300).
    /// 값·개수는 전부 파일에서 온다(<see cref="AttendanceData"/>·<see cref="DungeonData"/> 와 같은 문법 — 새 꼴 안 만든다).
    /// </para>
    /// 보상 한 칸은 <see cref="ArenaRankData.Reward"/> 를 그대로 쓴다(이 레포의 «보상 한 칸» 공통 꼴).
    /// </summary>
    public sealed class PrivilegeData
    {
        /// <summary>카드 하나.</summary>
        public sealed class Card
        {
            /// <summary>세이브·코드가 쓰는 키(<c>dailyGift</c>·<c>adRemove</c>·<c>monthly</c>·<c>lifetime</c>).</summary>
            public string Key;
            /// <summary>화면에 뜨는 이름.</summary>
            public string Name;
            /// <summary><b>공짜 카드</b>(사지 않아도 누구나 매일 받는다) — «데일리 기프트» 하나뿐이다.</summary>
            public bool Free;
            /// <summary><b>기간이 있는 카드</b>(<see cref="PrivilegeData.MonthlyDays"/> 일 뒤 매일 지급이 멈춘다) — «월간 카드» 하나뿐이다.</summary>
            public bool Expires;
            /// <summary>살 때 <b>한 번</b> 주는 것.</summary>
            public readonly List<ArenaRankData.Reward> BuyNow = new List<ArenaRankData.Reward>();
            /// <summary>가진 사람이 <b>하루 1번</b> 받는 것.</summary>
            public readonly List<ArenaRankData.Reward> Daily = new List<ArenaRankData.Reward>();
        }

        public readonly List<Card> Cards = new List<Card>();
        /// <summary>기간 있는 카드가 사는 날수(주인이 안 줘서 이름값 그대로 30 이 기본 · 파일이 정한다).</summary>
        public int MonthlyDays = 30;

        /// <summary><paramref name="key"/> 카드(없으면 null).</summary>
        public Card Of(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var c in Cards) if (c.Key == key) return c;
            return null;
        }

        public static PrivilegeData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static PrivilegeData From(JNode j)
        {
            var p = new PrivilegeData();
            p.MonthlyDays = (int)j["monthlyDays"].Num(30);
            if (p.MonthlyDays <= 0) throw new FormatException("privilege.json: monthlyDays 는 0 보다 커야 한다");
            foreach (var e in j["cards"].Items())
            {
                var c = new Card
                {
                    Key = e["key"].Str(""),
                    Name = e["name"].Str(""),
                    Free = e["free"].Bool(false),
                    Expires = e["expires"].Bool(false),
                };
                if (string.IsNullOrEmpty(c.Key)) throw new FormatException("privilege.json: cards[].key 가 비었다");
                if (string.IsNullOrEmpty(c.Name)) throw new FormatException("privilege.json: " + c.Key + " 의 name 이 비었다");
                if (p.Of(c.Key) != null) throw new FormatException("privilege.json: 카드 키가 겹친다 — " + c.Key);
                Read(e["buyNow"], c.BuyNow, c.Key + " buyNow");
                Read(e["daily"], c.Daily, c.Key + " daily");
                // 공짜 카드에 «살 때 주는 것» 이 있으면 아무도 못 받는다 — 살 수가 없기 때문이다(조용히 사라지는 보상).
                if (c.Free && c.BuyNow.Count > 0) throw new FormatException("privilege.json: " + c.Key + " 는 공짜 카드인데 buyNow 가 있다 — 살 수가 없어 아무도 못 받는다");
                if (c.Daily.Count == 0) throw new FormatException("privilege.json: " + c.Key + " 에 매일 받을 것이 없다");
                p.Cards.Add(c);
            }
            if (p.Cards.Count == 0) throw new FormatException("privilege.json: cards 가 비어 있다");
            return p;
        }

        static void Read(JNode arr, List<ArenaRankData.Reward> into, string what)
        {
            foreach (var r in arr.Items())
            {
                var item = r["item"].Str("");
                double amt = r["amount"].Num(0);
                if (string.IsNullOrEmpty(item)) throw new FormatException("privilege.json: " + what + " 의 item 이 비었다");
                if (amt <= 0) throw new FormatException("privilege.json: " + what + " 의 " + item + " 은 0 보다 커야 한다");
                // 담을 자리가 없는 이름은 «주면 조용히 사라지는 보상» 이 된다 — 읽는 순간 운다(T253·T243 결정 663 과 같은 갈래).
                if (!Mail.CanPay(item)) throw new FormatException("privilege.json: " + what + " 의 " + item + " 은 담을 자리가 없다");
                into.Add(new ArenaRankData.Reward { Item = item, Amount = amt });
            }
        }
    }

    /// <summary>
    /// 특권 규칙(T264 · 순수 C# · 저장은 <see cref="SaveData.PrivBuy"/>·<see cref="SaveData.PrivDay"/> 두 칸).
    /// <list type="bullet">
    /// <item><b>공짜 카드는 누구나</b> — «데일리 기프트» 는 사지 않아도 하루 1번 받는다(주인 «하루마다 30개씩 주는 거임»).</item>
    /// <item><b>산 카드만 매일</b> — 나머지 셋은 <see cref="Buy"/> 를 지난 사람만 받는다.</item>
    /// <item><b>받기는 카드마다 따로 하루 1회</b>(주인 명시) — 날짜가 바뀌면 넷 다 다시 열린다.</item>
    /// <item><b>월간 카드는 기간이 지나면 매일 지급이 멈춘다</b> — 산 날부터 <see cref="PrivilegeData.MonthlyDays"/> 일.
    ///       주인이 «만료되면 어떻게 되나» 를 말한 적이 없어 <b>지어내지 않았다</b>: 카드는 남되 받기만 막힌다(다시 살 수 있다).</item>
    /// </list>
    /// 지급은 <see cref="Mail.Give"/> 한 곳을 지난다(T243) — 특권으로 받은 다이아가 다른 칸에 들어가는 사고를 막는다.
    /// <para>저장(디스크 쓰기)·토스트·리워드 팝업·빨간 점은 부르는 쪽(게임 층) 몫이다.</para>
    /// </summary>
    public static class Privilege
    {
        /// <summary>이 카드를 가졌는가 — 공짜 카드는 <b>언제나 참</b>이고, 나머지는 산 적이 있어야 한다(기간은 여기서 안 본다).</summary>
        public static bool Owned(SaveData s, PrivilegeData.Card c)
        {
            if (s == null || c == null) return false;
            if (c.Free) return true;
            return s.PrivBuy != null && s.PrivBuy.ContainsKey(c.Key);
        }

        /// <summary>산 날(«yyyy-MM-dd» · 안 샀거나 공짜면 빈 문자열).</summary>
        public static string BoughtOn(SaveData s, PrivilegeData.Card c)
        {
            if (s == null || c == null || c.Free || s.PrivBuy == null) return "";
            string v; return s.PrivBuy.TryGetValue(c.Key, out v) ? (v ?? "") : "";
        }

        /// <summary>
        /// 기간이 지났는가 — <see cref="PrivilegeData.Card.Expires"/> 인 카드만 해당한다.
        /// 산 날을 <b>1일차</b>로 세어 <paramref name="days"/> 일까지 살아 있다(30일권을 산 날 포함 30일).
        /// 날짜를 못 읽으면 <b>안 지난 것으로 본다</b> — 세이브가 깨졌다고 산 것을 뺏지 않는다.
        /// </summary>
        public static bool Expired(SaveData s, PrivilegeData.Card c, int days, string today)
        {
            if (c == null || !c.Expires || !Owned(s, c)) return false;
            DateTime from, now;
            if (!DateTime.TryParse(BoughtOn(s, c), out from)) return false;
            if (!DateTime.TryParse(today, out now)) return false;
            return (now.Date - from.Date).TotalDays >= days;
        }

        /// <summary>오늘 이 카드를 받을 수 있는가.</summary>
        public static bool Can(SaveData s, PrivilegeData d, PrivilegeData.Card c, string today)
        {
            if (s == null || d == null || c == null) return false;
            if (!Owned(s, c)) return false;
            if (Expired(s, c, d.MonthlyDays, today)) return false;
            return LastDay(s, c) != today;
        }

        /// <summary>못 받는 까닭 한 줄(받을 수 있으면 빈 문자열) — <b>화면 토스트가 이 글자를 그대로 띄운다</b>(T228 갈래).</summary>
        public static string Why(SaveData s, PrivilegeData d, PrivilegeData.Card c, string today)
        {
            if (c == null) return "";
            if (!Owned(s, c)) return c.Name + " 을(를) 먼저 구매해야 합니다";
            if (d != null && Expired(s, c, d.MonthlyDays, today)) return c.Name + " 기간이 끝났습니다";
            if (LastDay(s, c) == today) return "오늘 " + c.Name + " 보상은 이미 받았습니다";
            return "";
        }

        /// <summary>마지막으로 받은 날(«yyyy-MM-dd» · 받은 적 없으면 빈 문자열).</summary>
        public static string LastDay(SaveData s, PrivilegeData.Card c)
        {
            if (s == null || c == null || s.PrivDay == null) return "";
            string v; return s.PrivDay.TryGetValue(c.Key, out v) ? (v ?? "") : "";
        }

        /// <summary>
        /// 카드를 <b>산다</b> — 이미 가졌으면 <c>false</c> 이고 아무것도 안 바뀐다(공짜 카드도 못 산다).
        /// 되면 «살 때 주는 것» 을 세이브에 더하고 산 날을 적는다.
        /// <para>⚠ <b>값을 치르는 것은 여기가 아니다</b> — 상점의 다이아 상품과 같은 모의 결제라 «얼마를 냈나» 는 화면 몫이다(ROUTINE §2 T264 3항).</para>
        /// </summary>
        public static bool Buy(SaveData s, PrivilegeData.Card c, string today)
        {
            if (s == null || c == null || c.Free) return false;
            if (s.PrivBuy == null) s.PrivBuy = new Dictionary<string, string>();
            if (s.PrivBuy.ContainsKey(c.Key)) return false;
            foreach (var r in c.BuyNow) Mail.Give(s, r.Item, r.Amount);
            s.PrivBuy[c.Key] = today ?? "";
            return true;
        }

        /// <summary>
        /// 오늘 몫을 <b>실제로 받는다</b> — 못 받으면 <c>null</c> 이고 <b>아무것도 안 바뀐다</b>.
        /// 되면 표의 매일 보상을 세이브에 더하고 «받은 날» 을 오늘로 옮긴 뒤 그 카드를 돌려준다(화면이 받은 것을 그대로 띄운다).
        /// </summary>
        public static PrivilegeData.Card Claim(SaveData s, PrivilegeData d, PrivilegeData.Card c, string today)
        {
            if (!Can(s, d, c, today)) return null;
            foreach (var r in c.Daily) Mail.Give(s, r.Item, r.Amount);
            if (s.PrivDay == null) s.PrivDay = new Dictionary<string, string>();
            s.PrivDay[c.Key] = today ?? "";
            return c;
        }

        /// <summary>오늘 받을 게 하나라도 있는가 — <b>빨간 점이 이것만 본다</b>(T96 ⓔ).</summary>
        public static bool AnyClaimable(SaveData s, PrivilegeData d, string today)
        {
            if (s == null || d == null) return false;
            foreach (var c in d.Cards) if (Can(s, d, c, today)) return true;
            return false;
        }

        /// <summary>받은 것 한 줄(«다이아 30») — 화면이 토스트로 그대로 쓴다(<see cref="Attendance.Summary"/> 와 같은 꼴).</summary>
        public static string Summary(List<ArenaRankData.Reward> rewards)
        {
            if (rewards == null) return "";
            string s = "";
            foreach (var r in rewards)
            {
                if (s.Length > 0) s += " · ";
                s += Mail.Name(r.Item) + " " + Math.Round(r.Amount).ToString("#,0");
            }
            return s;
        }
    }
}
