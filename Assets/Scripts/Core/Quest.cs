using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 퀘스트 표 (<c>Assets/KkomaKnight/quest.json</c> · T257 · 주인 2026-09-09 04:5X).
    /// <para>
    /// 주인이 <b>전부</b> 줬다 — 일일(동메달) 8줄과 점수(합 <b>130</b>) · 주간(은메달) 8줄과 점수(합 <b>210</b>) ·
    /// 두 트랙의 구간과 상품(일일 20·40·60·80·100 · 주간 30·60·90·120·150). 그래서 이 절에는 <b>지어낸 수가 없다</b>.
    /// 지금 화면(15 «퀘스트» 팝업)은 제목·목표·트랙 숫자가 <b>손으로 박힌 껍데기</b>고 진행도·지급이 없다 — 이 파일이 그 표다.
    /// </para>
    /// <para>
    /// ⚠ 여기까지가 <b>1단계</b>다(표 + 규칙). 진행도·받은 구간을 <b>저장</b>하는 일과 팝업을 그리는 일은 다음 단계다 —
    /// 팝업이 <c>LobbyPopups.cs</c> 안이고 그 파일은 T254 의 lock 이 쥐고 있어서(같은 파일이면 뒤 번호가 기다린다) 화면 없이 할 수 있는 절반을 먼저 놓는다(T237 ⓐⓑ 전례).
    /// </para>
    /// 값·개수는 전부 파일에서 온다 — 코드 상수 없음(<see cref="ArenaRankData"/>·<see cref="DungeonData"/> 와 같은 문법 · 새 꼴 안 만든다).
    /// </summary>
    public sealed class QuestData
    {
        /// <summary>상품 한 칸 — <c>Item</c> 은 게임 쪽 이름이고 아이콘 짝짓기는 화면 몫이다(<c>dungeon.json</c> 이 <c>petEgg</c>·<c>gold</c> 만 적는 것과 같다).</summary>
        public sealed class Reward
        {
            public string Item = "";
            public double Amount;
            /// <summary>
            /// 티켓처럼 <b>어느 던전 것인지</b>가 있어야 뜻이 서는 상품에만 채워진다(<c>hell</c> = 지옥의 문 · <c>expedition</c> = 원정).
            /// 티켓은 던전마다 따로라(T99) 일일 트랙 40 과 80 은 <b>서로 다른 던전</b>의 티켓이다 — 그것을 <c>"ticket:hell"</c> 같은
            /// 한 글자에 묶지 않고 칸을 갈라 둔다(묶으면 읽는 쪽이 문자열을 다시 쪼개야 하고, 쪼개는 규칙이 코드에 숨는다).
            /// </summary>
            public string Dungeon = "";
        }

        /// <summary>퀘스트 한 줄.</summary>
        public sealed class Quest
        {
            /// <summary>화면에 띄우는 글자 — <b>주인이 쓴 그대로</b>다(«적 2,500 죽이기» · «빠른 탐험 보상 받기»). 코드가 다시 짓지 않는다.</summary>
            public string Label = "";
            /// <summary>무엇을 세는가(이미 있는 이벤트에 훅을 건다 · 2단계 몫).</summary>
            public string Counter = "";
            /// <summary>몇이면 깬 것인가 — 주인 글자 안에 든 수 그대로다.</summary>
            public int Goal;
            /// <summary>깨면 얼마가 쌓이는가.</summary>
            public int Medal;
            /// <summary>지금 셈이 이 줄을 깼는가.</summary>
            public bool Done(int count) => count >= Goal;
            /// <summary>줄에 보이는 «진행/목표» 의 진행 쪽 — 목표를 넘어도 목표에서 멈춘다(«34/50» · «50/50»).</summary>
            public int Shown(int count) => count < 0 ? 0 : (count > Goal ? Goal : count);
        }

        /// <summary>트랙 한 칸 — «이만큼 채우면 이것을 받는다».</summary>
        public sealed class Step
        {
            public int Points;
            public readonly List<Reward> Rewards = new List<Reward>();
        }

        /// <summary>일일·주간 한 벌(줄 목록 + 트랙).</summary>
        public sealed class Track
        {
            /// <summary>메달 이름 — 주인의 «동메달»·«은메달»(화면이 그림을 고른다).</summary>
            public string Medal = "";
            public readonly List<Quest> Quests = new List<Quest>();
            public readonly List<Step> Steps = new List<Step>();

            /// <summary>줄을 <b>전부</b> 깼을 때 쌓이는 점수(일일 130 · 주간 210) — 표에서 더한 값이지 박아 둔 수가 아니다.</summary>
            public int MedalTotal
            {
                get { int s = 0; foreach (var q in Quests) s += q.Medal; return s; }
            }

            /// <summary>마지막 칸을 채우고도 남는 점수 — 주간은 <b>60 이 남는다</b>(주인 표 그대로 · 남는 점수는 버린다).</summary>
            public int Spare => Steps.Count == 0 ? MedalTotal : MedalTotal - Steps[Steps.Count - 1].Points;

            /// <summary>이 점수로 <b>받을 수 있게 된</b> 칸 수(«20 채우면 20 자리, 100이면 100 자리»).</summary>
            public int OpenCount(int points)
            {
                int n = 0;
                foreach (var s in Steps) if (points >= s.Points) n++;
                return n;
            }

            /// <summary>그 칸이 이 점수로 열렸는가(칸 번호는 0 부터).</summary>
            public bool IsOpen(int index, int points) =>
                index >= 0 && index < Steps.Count && points >= Steps[index].Points;
        }

        public readonly Track Daily = new Track();
        public readonly Track Weekly = new Track();

        /// <summary>주가 바뀌는 요일(<see cref="DayOfWeek"/> 의 수 · 1 = 월요일). 표에 있는 값이다 — 코드가 정하지 않는다.</summary>
        public int WeekStartDow = 1;

        /// <summary>일일이 갈리는 자리 — 출석·데일리 기프트와 <b>같은 규칙</b>이다(<c>SaveStore.Today()</c> 의 <c>yyyy-MM-dd</c>).</summary>
        public static string DayKey(DateTime now) => now.ToString("yyyy-MM-dd");

        /// <summary>
        /// 주가 갈리는 자리 — 그 주의 <b>시작 날</b>을 일일과 같은 꼴로 적는다(<c>yyyy-MM-dd</c>).
        /// 두 값이 같은 꼴이라 저장된 글자를 <b>비교만</b> 하면 «넘어갔는가» 가 나온다(날짜 산수를 저장 쪽에 다시 쓰지 않는다).
        /// </summary>
        public static string WeekKey(DateTime now, int weekStartDow)
        {
            int dow = (int)now.DayOfWeek;                      // 0 = 일요일
            int back = dow - weekStartDow;
            if (back < 0) back += 7;                           // 시작 요일 이전이면 지난주 것이다
            return now.Date.AddDays(-back).ToString("yyyy-MM-dd");
        }

        /// <summary>이 표의 시작 요일로 잰 주 열쇠.</summary>
        public string WeekKey(DateTime now) => WeekKey(now, WeekStartDow);

        public static QuestData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static QuestData From(JNode j)
        {
            var d = new QuestData { WeekStartDow = (int)j["weekStartDow"].Num(1) };
            if (d.WeekStartDow < 0 || d.WeekStartDow > 6)
                throw new FormatException("quest.json: weekStartDow 는 0(일)~6(토) 이어야 한다 — 지금 " + d.WeekStartDow);
            ReadTrack(j["daily"], d.Daily, "daily");
            ReadTrack(j["weekly"], d.Weekly, "weekly");
            return d;
        }

        static void ReadTrack(JNode j, Track t, string where)
        {
            t.Medal = j["medal"].Str("");
            if (string.IsNullOrEmpty(t.Medal)) throw new FormatException("quest.json: " + where + ".medal 이 비었다");
            foreach (var e in j["quests"].Items())
            {
                var q = new Quest
                {
                    Label = e["label"].Str(""),
                    Counter = e["counter"].Str(""),
                    Goal = (int)e.Req("goal").Num(),
                    Medal = (int)e.Req("medal").Num(),
                };
                if (string.IsNullOrEmpty(q.Label)) throw new FormatException("quest.json: " + where + ".quests[].label 이 비었다");
                if (string.IsNullOrEmpty(q.Counter)) throw new FormatException("quest.json: " + where + " «" + q.Label + "» 의 counter 가 비었다");
                if (q.Goal < 1) throw new FormatException("quest.json: " + where + " «" + q.Label + "» 의 goal 은 1 이상이어야 한다");
                if (q.Medal < 1) throw new FormatException("quest.json: " + where + " «" + q.Label + "» 의 medal 은 1 이상이어야 한다");
                t.Quests.Add(q);
            }
            if (t.Quests.Count == 0) throw new FormatException("quest.json: " + where + ".quests 가 비었다");

            foreach (var e in j["track"].Items())
            {
                var s = new Step { Points = (int)e.Req("points").Num() };
                foreach (var r in e["rewards"].Items())
                {
                    var item = r["item"].Str("");
                    if (string.IsNullOrEmpty(item)) throw new FormatException("quest.json: " + where + " 트랙 " + s.Points + " 의 rewards[].item 이 비었다");
                    double amt = r["amount"].Num(0);
                    if (amt <= 0) throw new FormatException("quest.json: " + where + " 트랙 " + s.Points + " 의 " + item + " 은 0 보다 커야 한다");
                    s.Rewards.Add(new Reward { Item = item, Amount = amt, Dungeon = r["dungeon"].Str("") });
                }
                if (s.Rewards.Count == 0) throw new FormatException("quest.json: " + where + " 트랙 " + s.Points + " 에 상품이 없다");
                t.Steps.Add(s);
            }
            if (t.Steps.Count == 0) throw new FormatException("quest.json: " + where + ".track 이 비었다");
            Check(t, where);
        }

        /// <summary>
        /// 표를 손으로 고치다 나는 <b>조용한 사고</b>를 파일을 읽는 순간 잡는다(<see cref="ArenaRankData"/> 의 그 자와 같은 구실).
        /// <para>
        /// ⓐ 트랙이 <b>오르는 차례</b>가 아니면 «20 채우면 20 자리» 라는 말 자체가 안 선다.
        /// ⓑ <b>마지막 칸이 줄을 다 깨도 못 닿으면</b> 그 상품은 아무도 못 받는데 화면에는 멀쩡히 뜬다 — 가장 비싼 종류의 오타다.
        ///    (주인 표는 일일 130 ≥ 100 · 주간 210 ≥ 150 으로 둘 다 닿는다. 주간은 60 이 남고 그것은 주인이 «버린다» 고 한 몫이다.)
        /// </para>
        /// </summary>
        static void Check(Track t, string where)
        {
            for (int i = 0; i < t.Steps.Count; i++)
            {
                if (t.Steps[i].Points < 1)
                    throw new FormatException("quest.json: " + where + " 트랙 칸의 points 는 1 이상이어야 한다 — 지금 " + t.Steps[i].Points);
                if (i > 0 && t.Steps[i].Points <= t.Steps[i - 1].Points)
                    throw new FormatException("quest.json: " + where + " 트랙이 오르는 차례가 아니다 — " + t.Steps[i - 1].Points + " 다음에 " + t.Steps[i].Points);
            }
            int last = t.Steps[t.Steps.Count - 1].Points, total = t.MedalTotal;
            if (last > total)
                throw new FormatException("quest.json: " + where + " 트랙 마지막 칸(" + last + ")이 줄을 다 깬 점수(" + total + ")보다 크다 — 그 상품은 아무도 못 받는다");
        }
    }
}
