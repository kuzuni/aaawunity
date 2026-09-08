using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 업적(반복 퀘스트) 표 (<c>Assets/KkomaKnight/achievement.json</c> · T258 · 주인 2026-09-09 05:2X
    /// «업적 부분은 <b>메달 없음. 걍 보상 바로 받음.</b> 업적은 <b>일종의 반복 퀘스트</b> 느낌으로 하면 됨»).
    /// <para>
    /// 퀘스트(<see cref="QuestData"/>)와 갈리는 것 셋 — ⓐ <b>메달 트랙이 없다</b>(깨면 그 보상을 바로 받는다) ·
    /// ⓑ <b>단계가 끝없이 늘어난다</b>(단계 N 의 목표 = <c>goal × N</c>) · ⓒ <b>진행도가 초기화되지 않는다</b>(누적·평생).
    /// </para>
    /// <para>
    /// ⚠ 여기까지가 <b>1회차</b>다(표 + 규칙 + 저장). 화면(퀘스트 팝업 15 의 «업적» 탭)은 다음 회차다 —
    /// 그 팝업이 <c>LobbyPopups.cs</c> 안이고 그 파일이 T257 의 lock 자리라 «같은 파일이면 뒤 번호가 기다린다» 를 따른다.
    /// 화면 없이 할 수 있는 절반을 먼저 놓는 것은 T237 ⓐⓑ·T240·T257 이 이미 밟은 길이다.
    /// </para>
    /// 값·개수는 전부 파일에서 온다 — 코드 상수 없음.
    /// </summary>
    public sealed class AchievementData
    {
        /// <summary>업적 한 줄.</summary>
        public sealed class Row
        {
            /// <summary>화면에 띄우는 글자 — <b>주인이 쓴 그대로</b>다. 코드가 다시 짓지 않는다.</summary>
            public string Label = "";
            /// <summary>무엇을 세는가 — <c>quest.json</c> 과 <b>같은 낱말을 일부러 같이 쓴다</b>(세는 자리가 같아야 두 절이 안 어긋난다).</summary>
            public string Counter = "";
            /// <summary><b>1단계</b>의 목표. 단계 N 의 목표는 <c>Goal × N</c> 이다.</summary>
            public int Goal;
            /// <summary>한 단계를 깨면 받는 것(주인은 전부 다이아로 줬다). 단계마다 <b>같다</b>.</summary>
            public string Item = "";
            public double Amount;

            /// <summary>단계 <paramref name="stage"/>(1부터)의 목표. 0 이하 단계는 1단계로 본다.</summary>
            public int GoalOf(int stage) => Goal * (stage < 1 ? 1 : stage);
        }

        public readonly List<Row> List = new List<Row>();

        public static AchievementData Parse(string json) => From(new JNode(MiniJson.Parse(json)));

        public static AchievementData From(JNode j)
        {
            var d = new AchievementData();
            var list = j["list"];
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                var row = new Row
                {
                    Label = r["label"].Str(""),
                    Counter = r["counter"].Str(""),
                    Goal = (int)r["goal"].ReqNum("goal"),
                    Item = r["item"].Str(""),
                    Amount = r["amount"].ReqNum("amount"),
                };
                // 이 셋이 비면 «영영 못 깨는 줄»·«눌러도 아무 일 없는 줄» 이 되는데, 화면에는 그저 «0/0» 으로만 보인다.
                if (row.Goal <= 0) throw new FormatException("achievement.json: goal 은 0 보다 커야 한다 — " + row.Label);
                if (row.Amount <= 0) throw new FormatException("achievement.json: amount 는 0 보다 커야 한다 — " + row.Label);
                if (row.Counter.Length == 0) throw new FormatException("achievement.json: counter 가 비었다 — " + row.Label);
                d.List.Add(row);
            }
            if (d.List.Count == 0) throw new FormatException("achievement.json: list 가 비었다");
            return d;
        }

        public Row Find(string counter)
        {
            for (int i = 0; i < List.Count; i++) if (List[i].Counter == counter) return List[i];
            return null;
        }
    }

    /// <summary>
    /// 업적 규칙 (T258 · 순수 C# · 저장은 <see cref="SaveData.Ach"/>(누적)·<see cref="SaveData.AchClaimed"/>(받은 단계 수) 두 표).
    /// <list type="bullet">
    /// <item><b>받기는 한 번에 한 단계씩</b>이다(주인 «5 → 10 → 15 → 20 순서로 모두»). 누적 20 에서 처음 열면 <b>네 번</b> 받는다 —
    /// 한꺼번에 몰아주지 않는 까닭은 주인이 «순차» 로 말했기 때문이고, 그래야 리워드 팝업도 단계마다 한 번씩 뜬다.</item>
    /// <item><b>받아도 누적은 줄지 않는다</b> — 줄이면 «평생 기록» 이라는 말이 거짓이 되고, 화면의 «누적/목표» 도 뒤로 간다.</item>
    /// <item><b>출석은 하루 한 번만</b> 오른다(주인 명시) — 그것만 «마지막으로 센 날짜» 를 같이 든다.</item>
    /// </list>
    /// </summary>
    public static class Achievement
    {
        /// <summary>출석처럼 «하루 한 번만» 세는 카운터(주인 명시 · 표의 <c>_attend</c> 와 같은 값).</summary>
        public const string DailyOnce = "attend";

        static int Get(Dictionary<string, int> map, string key)
        {
            if (map == null || key == null) return 0;
            int v; return map.TryGetValue(key, out v) ? (v > 0 ? v : 0) : 0;
        }

        /// <summary>지금까지의 누적(평생).</summary>
        public static int Count(SaveData s, string counter) => s == null ? 0 : Get(s.Ach, counter);

        /// <summary>여태 받은 단계 수.</summary>
        public static int Claimed(SaveData s, string counter) => s == null ? 0 : Get(s.AchClaimed, counter);

        /// <summary>지금 도전 중인 단계(1부터) = 받은 단계 + 1.</summary>
        public static int Stage(SaveData s, string counter) => Claimed(s, counter) + 1;

        /// <summary>이번 단계의 목표(표가 없거나 모르는 줄이면 0).</summary>
        public static int Goal(SaveData s, AchievementData d, string counter)
        {
            var row = d == null ? null : d.Find(counter);
            return row == null ? 0 : row.GoalOf(Stage(s, counter));
        }

        /// <summary>줄에 보이는 «누적/이번 목표» 의 진행 쪽 — 목표를 넘어도 목표에서 멈춘다(«5/10» · «10/10»).</summary>
        public static int Shown(SaveData s, AchievementData d, string counter)
        {
            int goal = Goal(s, d, counter), c = Count(s, counter);
            if (goal <= 0) return 0;
            return c > goal ? goal : (c < 0 ? 0 : c);
        }

        /// <summary>지금 «받기» 가 살아 있는가 = 누적이 이번 단계 목표에 닿았는가.</summary>
        public static bool CanClaim(SaveData s, AchievementData d, string counter)
        {
            int goal = Goal(s, d, counter);
            return goal > 0 && Count(s, counter) >= goal;
        }

        /// <summary>아직 안 받은 단계가 몇인가(밀린 만큼 · 화면의 «받기» 가 몇 번 살아 있는지와 같다).</summary>
        public static int Pending(SaveData s, AchievementData d, string counter)
        {
            var row = d == null ? null : d.Find(counter);
            if (row == null) return 0;
            int c = Count(s, counter);
            if (c <= 0) return 0;
            int done = c / row.Goal;                       // 누적이 넘긴 단계 수(단계 N 목표 = Goal×N 이므로 «몇 배인가» 다)
            int left = done - Claimed(s, counter);
            return left > 0 ? left : 0;
        }

        /// <summary>받을 게 하나라도 있는가(로비·탭 빨간 점 · T96 ⓔ).</summary>
        public static bool AnyClaimable(SaveData s, AchievementData d)
        {
            if (s == null || d == null) return false;
            for (int i = 0; i < d.List.Count; i++) if (CanClaim(s, d, d.List[i].Counter)) return true;
            return false;
        }

        /// <summary>
        /// 한 단계 받는다 — <b>한 번에 하나</b>(주인 «순차»). 받으면 <c>item</c>·<c>amount</c> 를 내주고 받은 단계를 하나 올린다.
        /// 못 받으면 <paramref name="item"/> 이 빈 글자이고 <paramref name="amount"/> 는 0 이다(부르는 쪽이 «줄 것이 없다» 를 그것으로 안다).
        /// </summary>
        public static bool Claim(SaveData s, AchievementData d, string counter, out string item, out double amount)
        {
            item = ""; amount = 0;
            if (s == null || d == null) return false;
            var row = d.Find(counter);
            if (row == null || !CanClaim(s, d, counter)) return false;
            if (s.AchClaimed == null) s.AchClaimed = new Dictionary<string, int>();
            s.AchClaimed[counter] = Claimed(s, counter) + 1;   // 누적(Ach)은 건드리지 않는다 — 평생 기록이다
            item = row.Item; amount = row.Amount;
            return true;
        }

        /// <summary>
        /// 세기 — 이벤트마다 부른다(적 처치는 마릿수라 <paramref name="by"/> 로 여럿). 표에 없는 카운터도 담는다
        /// (표가 늘어날 때 «그 전에 한 것» 이 0 부터 시작하지 않게 · 담아 두는 값은 int 하나라 싸다).
        /// </summary>
        public static void Add(SaveData s, string counter, int by = 1)
        {
            if (s == null || string.IsNullOrEmpty(counter) || by <= 0) return;
            if (s.Ach == null) s.Ach = new Dictionary<string, int>();
            long v = (long)Get(s.Ach, counter) + by;
            s.Ach[counter] = v > int.MaxValue ? int.MaxValue : (int)v;   // 평생 카운터라 넘칠 자리를 막아 둔다
        }

        /// <summary>
        /// 출석처럼 <b>하루 한 번만</b> 세는 것(주인 명시). 같은 날 두 번 부르면 두 번째는 아무 일도 안 한다.
        /// <paramref name="today"/> 는 게임 층이 주는 <c>yyyy-MM-dd</c> — 순수 C# 이라 여기서 시계를 읽지 않는다(<see cref="Expedition"/> 과 같은 규약).
        /// </summary>
        public static void AddOncePerDay(SaveData s, string counter, string today)
        {
            if (s == null || string.IsNullOrEmpty(counter) || string.IsNullOrEmpty(today)) return;
            if (s.AchDay == null) s.AchDay = new Dictionary<string, string>();
            string last;
            if (s.AchDay.TryGetValue(counter, out last) && last == today) return;
            s.AchDay[counter] = today;
            Add(s, counter, 1);
        }
    }
}
