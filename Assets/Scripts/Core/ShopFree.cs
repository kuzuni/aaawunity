namespace KkomaKnight.Core
{
    /// <summary>
    /// 상점의 <b>«하루 1번» 자리들</b> — 무료 보급(다이아·골드)과 상자 광고 오픈(희귀·전설) (T259 4항 · 주인 2026-09-09).
    /// <para>
    /// <b>왜 절이 따로 생겼나</b> — 여태 이 규칙은 세이브 필드 하나(<see cref="SaveData.FreeDay"/>)와
    /// <c>ShopScreen.CanFree</c>/<c>OnFree</c> 두 줄에만 있었다. 주인이 «다이아 100 도 무료 보급마다 1회 · 1,000골드도 마찬가지 ·
    /// 상자 광고는 그 상자를 1회 오픈» 이라고 자리를 <b>넷</b>으로 늘리는 순간, 날짜 도장 하나로는 못 센다
    /// (하나로 두면 다이아를 받은 날 골드가 같이 잠긴다).
    /// </para>
    /// <para>
    /// <b>«오늘» 은 부르는 쪽이 준다</b> — Core 는 시계를 안 본다(<see cref="Attendance"/>·<see cref="DungeonTickets"/> 와 같은 규약).
    /// 게임 층이 <c>SaveStore.Today()</c>(<c>yyyy-MM-dd</c> 로컬)를 넘긴다. 그래야 자가 «날이 바뀌었다» 를 손으로 만들 수 있다.
    /// </para>
    /// <para>
    /// <b>날짜 하나로 충분한 까닭</b> — 넷 다 «하루 1회» 다(지시서 4항 «하루 1회라 날짜로 충분하다»).
    /// 언젠가 «하루 N회» 가 되면 날짜만으로는 못 세고 <b>횟수 칸</b>이 따로 있어야 한다
    /// (던전이 그 꼴이다 — <see cref="SaveData.DunAdUsed"/> 가 날짜 도장 <see cref="SaveData.DunDay"/> 옆에 따로 있다).
    /// 그때 고칠 자리를 <see cref="PerDay"/> 한 줄에 모아 뒀다.
    /// </para>
    /// </summary>
    public static class ShopFree
    {
        /// <summary>다이아 무료 보급(상품 목록의 «다이아 100» 이 «Free» 로 바뀌는 자리 · 주인 «100다이아 부분 상품도 무료 보급 때마다 1회 Free»).</summary>
        public const string Gem = "freeGem";
        /// <summary>골드 무료 보급(«1,000골드 부분도 마찬가지»).</summary>
        public const string Gold = "freeGold";
        /// <summary>희귀 상자 광고 오픈(주인 «광고 버튼 클릭 시 광고를 본 다음에 해당 상자 1회 오픈»).</summary>
        public const string BoxRare = "adBoxRare";
        /// <summary>전설 상자 광고 오픈. <b>신화는 없다</b> — 주인이 «희귀 상자·전설 상자» 라고만 했고 없는 자리를 만들지 않는다(지시서 1항).</summary>
        public const string BoxLegend = "adBoxLegend";

        /// <summary>이 절이 아는 자리 전부 — 자가 «빠진 자리 없이 다 독립인가» 를 이 목록으로 훑는다.</summary>
        public static readonly string[] All = { Gem, Gold, BoxRare, BoxLegend };

        /// <summary>
        /// 한 자리를 하루에 몇 번 쓸 수 있는가 — 주인이 준 것은 «1회» 뿐이라 <b>1</b> 이다(지시서 1·3항).
        /// <para>
        /// ⚠ <b>이 수를 올리려면 여기 한 줄로는 안 된다</b> — 지금 세이브가 갖는 것은 «마지막 받은 날» 하나라
        /// 2 로 바꿔도 «오늘 두 번째» 를 셀 수가 없다. 올리는 회차는 <see cref="SaveData.DunAdUsed"/> 처럼
        /// «오늘 몇 번 썼나» 칸을 같이 두어야 한다. 그 사실을 여기 적어 두지 않으면 다음 사람이 상수만 고치고
        /// «올렸는데 안 되네» 를 겪는다(고쳐도 자는 초록이다 — 자도 날짜만 보기 때문이다).
        /// </para>
        /// </summary>
        public const int PerDay = 1;

        /// <summary>그 자리를 <b>마지막으로 쓴 날</b>(<c>yyyy-MM-dd</c> · 한 번도 안 썼으면 빈 문자열).</summary>
        public static string DayOf(SaveData s, string target)
        {
            if (s == null || string.IsNullOrEmpty(target)) return "";
            return s.FreeDays.TryGetValue(target, out var d) ? (d ?? "") : "";
        }

        /// <summary>오늘 그 자리를 쓸 수 있는가 — «마지막으로 쓴 날 ≠ 오늘».</summary>
        public static bool Can(SaveData s, string target, string today)
        {
            if (s == null || string.IsNullOrEmpty(target) || string.IsNullOrEmpty(today)) return false;
            return DayOf(s, target) != today;
        }

        /// <summary>
        /// 그 자리를 <b>쓴 것으로 적는다</b> — 됐으면 true(오늘 이미 썼으면 false 이고 아무것도 안 바꾼다).
        /// <para>
        /// <b>이것이 빗장이다.</b> 지급(다이아를 더하거나 상자를 여는 일)은 부르는 쪽 몫이고, 이 절은 «오늘 몫을 이미 썼는가» 만 센다 —
        /// 그래서 <b>지급보다 먼저</b> 불러 true 인 것을 보고 지급해야 한다. 순서가 뒤집히면 «주고 나서 못 준다고 하는» 자리가 된다.
        /// </para>
        /// <para>저장(디스크 쓰기)은 부르는 쪽 몫이다 — 이 절은 순수 C# 이다(<see cref="Revive"/>·<see cref="ArenaTickets"/> 와 같은 규약).</para>
        /// </summary>
        public static bool Take(SaveData s, string target, string today)
        {
            if (!Can(s, target, today)) return false;
            s.FreeDays[target] = today;
            return true;
        }
    }
}
