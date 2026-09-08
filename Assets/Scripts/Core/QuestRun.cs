using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 퀘스트가 <b>돌아가는</b> 규칙 (T257 2단계) — 표(<see cref="QuestData"/>)는 «무엇을 몇에 준다» 를 갖고, 이 절은 «세고 · 지우고 · 준다» 를 갖는다.
    /// <para>
    /// 화면은 한 줄도 안 나온다 — 팝업(7항)은 이 절에 물어보기만 하면 되고, 그래서 이 단계가 <b>헤드리스로 전부 검사된다</b>
    /// (워커는 PlayMode 를 못 돌린다 · 결정 143). 훅을 거는 자리도 <see cref="Bump"/> 한 줄씩이라 게임 코드에 규칙이 안 샌다.
    /// </para>
    /// <para>
    /// ⚠ <b>일일과 주간은 같은 사건을 따로 센다</b>(<see cref="SaveData.QuestDaily"/> ↔ <see cref="SaveData.QuestWeekly"/>) —
    /// 세는 것은 같지만 <b>지워지는 때가 달라서</b> 한 표에 담으면 일일이 지워질 때 주간까지 같이 지워진다.
    /// </para>
    /// </summary>
    public static class QuestRun
    {
        /// <summary>
        /// 날·주가 넘어갔으면 그 몫을 0 으로 민다 — <b>무엇을 하기 전이든 먼저 부른다</b>(세기 전·화면을 그리기 전·받기 전).
        /// 누적 메달은 따로 저장하지 않고 «깬 줄의 medal 합» 으로 계산하므로(<see cref="Medal"/>) 셈이 0 이 되면 메달도 0 이 되고,
        /// 받은 칸(<c>…Got</c>)도 같이 비워야 다음 날 그 칸을 다시 받을 수 있다.
        /// </summary>
        /// <returns>무언가 지워졌으면 true(화면이 다시 그릴 거리가 있다는 뜻).</returns>
        public static bool Roll(SaveData s, QuestData d, DateTime now)
        {
            if (s == null || d == null) return false;
            bool changed = false;

            string day = QuestData.DayKey(now);
            if (s.QuestDay != day)
            {
                s.QuestDay = day;
                s.QuestDaily.Clear();
                s.QuestDailyGot = New(d.Daily.Steps.Count);
                changed = true;
            }

            string week = d.WeekKey(now);
            if (s.QuestWeek != week)
            {
                s.QuestWeek = week;
                s.QuestWeekly.Clear();
                s.QuestWeeklyGot = New(d.Weekly.Steps.Count);
                // 주가 바뀌면 «로그인 5일» 도 처음부터다 — 안 지우면 지난주 마지막 날이 이번 주 첫날로 셈에 남는다.
                s.QuestLoginDay = "";
                changed = true;
            }

            // 표의 칸 수가 바뀌었을 때(주인이 트랙을 늘리거나 줄였을 때) 길이를 맞춘다 — 짧으면 받을 칸이 사라지고 길면 없는 칸을 받는다.
            changed |= Fit(s.QuestDailyGot, d.Daily.Steps.Count);
            changed |= Fit(s.QuestWeeklyGot, d.Weekly.Steps.Count);
            return changed;
        }

        static List<bool> New(int n) { var l = new List<bool>(); for (int i = 0; i < n; i++) l.Add(false); return l; }

        static bool Fit(List<bool> got, int n)
        {
            if (got == null) return false;
            bool changed = false;
            while (got.Count < n) { got.Add(false); changed = true; }
            while (got.Count > n) { got.RemoveAt(got.Count - 1); changed = true; }
            return changed;
        }

        /// <summary>
        /// 사건 하나를 센다 — <b>일일·주간에 같이</b> 쌓는다(어느 쪽 표에 그 줄이 있는지는 이 절이 안 따진다 · 표에 없으면 아무 줄도 안 읽어 갈 뿐이다).
        /// 훅을 거는 쪽은 «무슨 일이 일어났다» 만 알리면 된다.
        /// </summary>
        public static void Bump(SaveData s, string counter, int n = 1)
        {
            if (s == null || string.IsNullOrEmpty(counter) || n <= 0) return;
            Add(s.QuestDaily, counter, n);
            Add(s.QuestWeekly, counter, n);
        }

        static void Add(Dictionary<string, int> m, string k, int n)
        {
            if (m == null) return;
            m.TryGetValue(k, out int v);
            m[k] = v + n;
        }

        /// <summary>
        /// 접속했다 — 일일 «로그인하기» 는 켤 때마다 1(목표가 1 이라 넘쳐도 뜻이 같다), 주간 «로그인 5일» 은 <b>날이 바뀐 때만</b> 1.
        /// 뒤엣것을 <see cref="Bump"/> 로 세면 하루에 앱을 다섯 번 켠 사람이 주간을 깨 버린다.
        /// </summary>
        public static void Login(SaveData s, QuestData d, DateTime now)
        {
            if (s == null || d == null) return;
            Roll(s, d, now);
            Add(s.QuestDaily, "login", 1);
            string day = QuestData.DayKey(now);
            if (s.QuestLoginDay != day) { s.QuestLoginDay = day; Add(s.QuestWeekly, "loginDays", 1); }
        }

        /// <summary>그 줄의 지금 셈(표에 없던 이름이면 0).</summary>
        public static int Count(SaveData s, bool daily, string counter)
        {
            var m = s == null ? null : (daily ? s.QuestDaily : s.QuestWeekly);
            if (m == null || string.IsNullOrEmpty(counter)) return 0;
            m.TryGetValue(counter, out int v); return v;
        }

        /// <summary>지금 쌓인 메달 — <b>깬 줄의 medal 합</b>이다(따로 저장하지 않는다: 저장하면 셈과 메달이 갈라질 수 있다).</summary>
        public static int Medal(SaveData s, QuestData.Track t, bool daily)
        {
            if (s == null || t == null) return 0;
            int sum = 0;
            foreach (var q in t.Quests) if (q.Done(Count(s, daily, q.Counter))) sum += q.Medal;
            return sum;
        }

        /// <summary>그 칸을 <b>지금 받을 수 있는가</b> — 점수가 찼고(열렸고) 아직 안 받았을 때만.</summary>
        public static bool CanClaim(SaveData s, QuestData d, bool daily, int index)
        {
            if (s == null || d == null) return false;
            var t = daily ? d.Daily : d.Weekly;
            var got = daily ? s.QuestDailyGot : s.QuestWeeklyGot;
            if (index < 0 || index >= t.Steps.Count) return false;
            if (got != null && index < got.Count && got[index]) return false;
            return t.IsOpen(index, Medal(s, t, daily));
        }

        /// <summary>받을 것이 하나라도 있는가 — 빨간 점(T96 ⓔ)이 물어보는 자리다.</summary>
        public static bool AnyClaimable(SaveData s, QuestData d)
        {
            if (s == null || d == null) return false;
            for (int i = 0; i < d.Daily.Steps.Count; i++) if (CanClaim(s, d, true, i)) return true;
            for (int i = 0; i < d.Weekly.Steps.Count; i++) if (CanClaim(s, d, false, i)) return true;
            return false;
        }

        /// <summary>
        /// 그 칸을 받는다 — <b>즉시 지급</b>(주인 T243 «나머지는 걍 즉시 지급»)하고 «받았다» 를 적는다.
        /// <para>
        /// 재화는 <see cref="Mail.Give"/> 한 곳으로 넣는다(이름 → 담는 자리의 짝이 거기 하나여야 «우편으로 받은 골드» 와 «퀘스트로 받은 골드» 가 갈라지지 않는다).
        /// <b>티켓만 다르다</b> — 재화가 아니라 던전마다 따로인 보유량이라(T99) <see cref="SaveData.DunTickets"/> 에 바로 더한다.
        /// 지시서 3항대로 <b>하루 보충 상한은 안 본다</b>(넘어도 그냥 준다).
        /// </para>
        /// 못 받는 칸이면 <b>아무 일도 안 한다</b>(false) — 두 번 눌러도 두 번 안 들어온다.
        /// </summary>
        public static bool Claim(SaveData s, QuestData d, bool daily, int index)
        {
            if (!CanClaim(s, d, daily, index)) return false;
            var t = daily ? d.Daily : d.Weekly;
            var got = daily ? s.QuestDailyGot : s.QuestWeeklyGot;
            foreach (var r in t.Steps[index].Rewards)
            {
                if (r.Item == ItemTicket)
                {
                    if (string.IsNullOrEmpty(r.Dungeon)) continue;   // 어느 던전인지 없으면 줄 곳이 없다(표 검사가 먼저 울어야 하는 자리)
                    s.DunTickets.TryGetValue(r.Dungeon, out int have);
                    s.DunTickets[r.Dungeon] = have + (int)Math.Round(r.Amount);
                }
                else Mail.Give(s, r.Item, r.Amount);
            }
            while (got.Count <= index) got.Add(false);
            got[index] = true;
            return true;
        }

        /// <summary>보상 이름 중 «재화가 아닌» 하나 — 던전 티켓은 던전마다 따로라 <see cref="Mail"/> 이 모른다.</summary>
        public const string ItemTicket = "ticket";
    }
}
