using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 출석(16) <b>7일 보상표</b>(<c>Assets/KkomaKnight/attendance.json</c> · T253 · 주인 2026-09-09 04:3X
    /// «출석 날짜별 (7일) 보상 — 1. 3000골드 2. 파란키 2 3. 100다이아 4. 펫알 10 5. 5000골드 6. 보라키 1 7. 10k골드, 1000다이아»).
    /// <para>값·개수는 전부 파일에서 온다 — 코드 상수 없음(<see cref="ArenaRankData"/>·<see cref="DungeonData"/> 와 같은 문법 · 새 꼴 안 만든다).</para>
    /// 보상 한 칸의 꼴은 <see cref="ArenaRankData.Reward"/> 를 그대로 쓴다 — 이 레포는 이미 그것을 «보상 한 칸» 의 공통 꼴로 쓰고 있다(<see cref="MailItem.Rewards"/>).
    /// </summary>
    public sealed class AttendanceData
    {
        /// <summary>하루 칸 하나.</summary>
        public sealed class Day
        {
            /// <summary>몇 일차인가(1 부터).</summary>
            public int No;
            public readonly List<ArenaRankData.Reward> Rewards = new List<ArenaRankData.Reward>();
        }

        public readonly List<Day> Days = new List<Day>();

        /// <summary><paramref name="no"/> 일차 칸(없으면 null).</summary>
        public Day Of(int no)
        {
            foreach (var d in Days) if (d.No == no) return d;
            return null;
        }

        public static AttendanceData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static AttendanceData From(JNode j)
        {
            var a = new AttendanceData();
            foreach (var e in j["days"].Items())
            {
                var d = new Day { No = (int)e["day"].ReqNum("days[].day") };
                foreach (var r in e["rewards"].Items())
                {
                    var item = r["item"].Str("");
                    double amt = r["amount"].Num(0);
                    if (string.IsNullOrEmpty(item)) throw new FormatException("attendance.json: " + d.No + "일차 rewards[].item 이 비었다");
                    if (amt <= 0) throw new FormatException("attendance.json: " + d.No + "일차 " + item + " 은 0 보다 커야 한다");
                    // 담을 자리가 없는 이름은 «주면 조용히 사라지는 보상» 이 된다 — 읽는 순간 운다(T243 결정 663 과 같은 갈래).
                    if (!Mail.CanPay(item)) throw new FormatException("attendance.json: " + d.No + "일차 " + item + " 은 담을 자리가 없다");
                    d.Rewards.Add(new ArenaRankData.Reward { Item = item, Amount = amt });
                }
                if (d.Rewards.Count == 0) throw new FormatException("attendance.json: " + d.No + "일차에 보상이 없다");
                a.Days.Add(d);
            }
            if (a.Days.Count == 0) throw new FormatException("attendance.json: days 가 비어 있다");
            for (int i = 0; i < a.Days.Count; i++)
                if (a.Days[i].No != i + 1)
                    throw new FormatException("attendance.json: 일차는 1 부터 하나씩 이어져야 한다 — " + (i + 1) + "번째가 " + a.Days[i].No + "일차다");
            return a;
        }
    }

    /// <summary>
    /// 출석 규칙(T253 · 순수 C# · 저장은 <see cref="SaveData.AttDone"/>·<see cref="SaveData.AttDay"/> 두 필드).
    /// <list type="bullet">
    /// <item><b>하루에 한 칸</b> — 오늘 이미 받았으면 못 받는다(<see cref="Can"/>).</item>
    /// <item><b>날짜가 바뀌면 다음 칸</b> — 칸은 «며칠 연속인가» 가 아니라 <b>받은 칸 수</b>로 나아간다(하루 걸러 와도 다음 칸이다).
    ///       주인이 «연속이 끊기면 처음으로» 를 말한 적이 없어 <b>지어내지 않는다</b>(§1).</item>
    /// <item><b>일곱 칸을 다 받으면 끝</b> — 8일째에 무엇을 하는지도 주인이 말한 적이 없다. 되돌려 다시 돌리지 않고 «다 받았다» 로 둔다.</item>
    /// </list>
    /// 지급은 <see cref="Mail.Give"/> 한 곳을 지난다 — 우편으로 받은 골드와 출석으로 받은 골드가 다른 칸에 들어가는 사고를 막는다(T243).
    /// <para>저장(디스크 쓰기)·토스트·팝업은 부르는 쪽(게임 층) 몫이다.</para>
    /// </summary>
    public static class Attendance
    {
        /// <summary>다음에 받을 일차(1 부터 · <b>0 = 다 받았다</b>).</summary>
        public static int Next(SaveData s, AttendanceData d)
        {
            if (s == null || d == null) return 0;
            int done = s.AttDone < 0 ? 0 : s.AttDone;
            return done >= d.Days.Count ? 0 : done + 1;
        }

        /// <summary>오늘 받을 수 있는가 — 남은 칸이 있고 오늘 아직 안 받았다.</summary>
        public static bool Can(SaveData s, AttendanceData d, string today)
            => Next(s, d) > 0 && s.AttDay != today;

        /// <summary>못 받는 까닭 한 줄(받을 수 있으면 빈 문자열) — <b>화면 토스트가 이 글자를 그대로 띄운다</b>(말투도 여기서 정한다 · T228 갈래).</summary>
        public static string Why(SaveData s, AttendanceData d, string today)
        {
            if (Next(s, d) <= 0) return "출석 보상을 모두 받았습니다";
            if (s.AttDay == today) return "오늘 출석 보상은 이미 받았습니다";
            return "";
        }

        /// <summary>오늘 받을 칸(못 받으면 null) — 화면이 «무엇을 받나» 를 미리 보여 줄 때 쓴다.</summary>
        public static AttendanceData.Day Prize(SaveData s, AttendanceData d, string today)
            => Can(s, d, today) ? d.Of(Next(s, d)) : null;

        /// <summary>
        /// 오늘 칸을 <b>실제로 받는다</b> — 못 받으면 <c>null</c> 이고 <b>아무것도 안 바뀐다</b>.
        /// 되면 표의 보상을 세이브에 더하고 «받은 칸 수»·«받은 날짜» 를 옮긴 뒤 그 칸을 돌려준다(화면이 받은 것을 그대로 띄운다).
        /// </summary>
        public static AttendanceData.Day Claim(SaveData s, AttendanceData d, string today)
        {
            var day = Prize(s, d, today);
            if (day == null) return null;
            foreach (var r in day.Rewards) Mail.Give(s, r.Item, r.Amount);
            s.AttDone = day.No;
            s.AttDay = today;
            return day;
        }

        /// <summary>이미 받은 칸인가(화면의 ✅).</summary>
        public static bool Claimed(SaveData s, int no) => s != null && no >= 1 && no <= s.AttDone;

        /// <summary>오늘 강조할 칸(0 = 없다) — 오늘 받을 수 있는 칸만 강조한다(이미 받았으면 강조하지 않는다).</summary>
        public static int Today(SaveData s, AttendanceData d, string today) => Can(s, d, today) ? Next(s, d) : 0;

        /// <summary>받은 것 한 줄(«골드 3,000» · «골드 10,000 · 다이아 1,000») — 화면이 토스트로 그대로 쓴다.</summary>
        public static string Summary(AttendanceData.Day day)
        {
            if (day == null) return "";
            string s = "";
            foreach (var r in day.Rewards)
            {
                if (s.Length > 0) s += " · ";
                s += Mail.Name(r.Item) + " " + Math.Round(r.Amount).ToString("#,0");
            }
            return s;
        }
    }
}
