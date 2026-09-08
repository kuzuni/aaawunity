using System;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T257 2단계 — 퀘스트가 <b>돌아가는</b> 규칙(<see cref="QuestRun"/>)의 자: 세고 · 지우고 · 준다.
    /// <para>
    /// 이 절이 헤드리스로 다 잡히는 것이 요점이다(워커는 PlayMode 를 못 돌린다 · 결정 143) — 팝업은 여기에 물어보기만 하므로
    /// «받았는데 안 들어왔다» · «날이 바뀌었는데 그대로다» 같은 사고가 <b>화면 없이</b> 걸린다.
    /// </para>
    /// 특히 못 박는 둘: ⓐ <b>일일이 지워질 때 주간이 같이 지워지지 않는다</b> ⓑ <b>같은 칸을 두 번 받을 수 없다</b>.
    /// </summary>
    public class QuestRunTests
    {
        static QuestData Table() =>
            QuestData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "quest.json"))));

        static readonly DateTime Mon = new DateTime(2026, 9, 7, 9, 0, 0);    // 월요일
        static readonly DateTime Tue = new DateTime(2026, 9, 8, 9, 0, 0);    // 같은 주 화요일
        static readonly DateTime NextMon = new DateTime(2026, 9, 14, 9, 0, 0);

        static SaveData Fresh(QuestData d, DateTime now)
        {
            var s = new SaveData();
            QuestRun.Roll(s, d, now);
            return s;
        }

        /// <summary>그 줄을 깰 만큼 센다(목표를 표에서 읽어 온다 — 자가 수를 베끼지 않는다).</summary>
        static void Finish(SaveData s, QuestData.Track t, string counter)
        {
            foreach (var q in t.Quests) if (q.Counter == counter) QuestRun.Bump(s, counter, q.Goal);
        }

        [Test]
        public void 첫_Roll_이_칸을_표_길이에_맞춘다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            Assert.AreEqual(d.Daily.Steps.Count, s.QuestDailyGot.Count, "일일 트랙 칸 수");
            Assert.AreEqual(d.Weekly.Steps.Count, s.QuestWeeklyGot.Count, "주간 트랙 칸 수");
            Assert.AreEqual(QuestData.DayKey(Mon), s.QuestDay);
            Assert.AreEqual(d.WeekKey(Mon), s.QuestWeek);
        }

        [Test]
        public void 한_번_센_것이_일일과_주간에_같이_쌓인다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            QuestRun.Bump(s, "kill", 30);
            Assert.AreEqual(30, QuestRun.Count(s, true, "kill"), "일일");
            Assert.AreEqual(30, QuestRun.Count(s, false, "kill"), "주간");
            QuestRun.Bump(s, "kill", 20);
            Assert.AreEqual(50, QuestRun.Count(s, true, "kill"));
            Assert.AreEqual(50, QuestRun.Count(s, false, "kill"));
            // 표에 없는 이름이 들어와도 조용히 쌓이기만 한다(화면은 표에 있는 줄만 그린다).
            QuestRun.Bump(s, "무언가", 3);
            Assert.AreEqual(3, QuestRun.Count(s, true, "무언가"));
        }

        [Test]
        public void 날이_바뀌면_일일만_지워지고_주간은_남는다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            QuestRun.Bump(s, "kill", 50);
            Assert.IsTrue(QuestRun.Roll(s, d, Tue), "날이 바뀌었으니 지워진 것이 있다");
            Assert.AreEqual(0, QuestRun.Count(s, true, "kill"), "일일은 0");
            // ⓐ 이것이 두 표를 갈라 둔 까닭이다 — 한 표였으면 주간도 같이 날아간다.
            Assert.AreEqual(50, QuestRun.Count(s, false, "kill"), "주간은 그대로");
            Assert.AreEqual(QuestData.DayKey(Tue), s.QuestDay);
            Assert.AreEqual(d.WeekKey(Mon), s.QuestWeek, "같은 주라 주는 안 바뀐다");
        }

        [Test]
        public void 주가_바뀌면_주간도_지워진다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            QuestRun.Bump(s, "kill", 2500);
            QuestRun.Roll(s, d, NextMon);
            Assert.AreEqual(0, QuestRun.Count(s, false, "kill"), "주간 0");
            Assert.AreEqual(0, QuestRun.Count(s, true, "kill"), "일일도 0(날도 바뀌었다)");
            Assert.AreEqual("", s.QuestLoginDay, "«로그인 5일» 도 처음부터다");
        }

        [Test]
        public void 로그인은_하루에_한_번만_주간에_센다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            QuestRun.Login(s, d, Mon);
            QuestRun.Login(s, d, Mon.AddHours(3));
            QuestRun.Login(s, d, Mon.AddHours(6));
            Assert.AreEqual(1, QuestRun.Count(s, false, "loginDays"), "하루에 몇 번 켜도 주간은 하루");
            Assert.AreEqual(3, QuestRun.Count(s, true, "login"), "일일 «로그인하기» 는 켤 때마다(목표가 1 이라 뜻은 같다)");
            QuestRun.Login(s, d, Tue);
            Assert.AreEqual(2, QuestRun.Count(s, false, "loginDays"), "다음 날이면 하나 는다");
        }

        [Test]
        public void 메달은_깬_줄의_합이다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            Assert.AreEqual(0, QuestRun.Medal(s, d.Daily, true), "아무것도 안 깼으면 0");
            QuestRun.Bump(s, "kill", 49);
            Assert.AreEqual(0, QuestRun.Medal(s, d.Daily, true), "목표에 하나 모자라면 아직 0");
            QuestRun.Bump(s, "kill", 1);
            Assert.AreEqual(d.Daily.Quests[1].Medal, QuestRun.Medal(s, d.Daily, true), "깬 그 줄의 medal");
            foreach (var q in d.Daily.Quests) Finish(s, d.Daily, q.Counter);
            Assert.AreEqual(d.Daily.MedalTotal, QuestRun.Medal(s, d.Daily, true), "다 깨면 표의 합(130)");
        }

        [Test]
        public void 채운_칸만_받을_수_있다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            Assert.IsFalse(QuestRun.CanClaim(s, d, true, 0), "0 점이면 못 받는다");
            Assert.IsFalse(QuestRun.AnyClaimable(s, d), "빨간 점도 안 뜬다");
            QuestRun.Bump(s, "kill", 50);   // 20 점
            Assert.IsTrue(QuestRun.CanClaim(s, d, true, 0), "20 점이면 첫 칸");
            Assert.IsFalse(QuestRun.CanClaim(s, d, true, 1), "둘째 칸은 아직");
            Assert.IsTrue(QuestRun.AnyClaimable(s, d), "받을 게 있으면 빨간 점");
            Assert.IsFalse(QuestRun.CanClaim(s, d, true, 99), "없는 칸");
        }

        [Test]
        public void 받으면_들어오고_두_번은_안_들어온다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            QuestRun.Bump(s, "kill", 50);
            double before = Mail.Held(s, GachaKeys.Blue);
            Assert.IsTrue(QuestRun.Claim(s, d, true, 0), "받는다");
            Assert.AreEqual(before + 1, Mail.Held(s, GachaKeys.Blue), "파란 키가 실제로 들어온다");
            // ⓑ 두 번 눌러도 두 번 안 들어온다.
            Assert.IsFalse(QuestRun.Claim(s, d, true, 0), "두 번째는 아무 일도 없다");
            Assert.AreEqual(before + 1, Mail.Held(s, GachaKeys.Blue), "수가 안 는다");
            Assert.IsFalse(QuestRun.CanClaim(s, d, true, 0), "받은 칸은 더 못 받는다");
        }

        [Test]
        public void 티켓은_재화가_아니라_그_던전의_보유량으로_들어간다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            foreach (var q in d.Daily.Quests) Finish(s, d.Daily, q.Counter);   // 130 점 = 다섯 칸 전부
            s.DunTickets.TryGetValue("hell", out int hell0);
            s.DunTickets.TryGetValue("expedition", out int exp0);
            Assert.IsTrue(QuestRun.Claim(s, d, true, 1), "40 칸");
            Assert.IsTrue(QuestRun.Claim(s, d, true, 3), "80 칸");
            Assert.AreEqual(hell0 + 1, s.DunTickets["hell"], "40 은 지옥문 티켓");
            Assert.AreEqual(exp0 + 1, s.DunTickets["expedition"], "80 은 원정 티켓");
            // 둘이 서로 다른 던전이라는 것이 주인 말의 핵심이다 — 한 곳에 둘 다 들어가면 안 된다.
            Assert.AreNotEqual(s.DunTickets["hell"], hell0 + 2, "지옥문에 두 장이 들어가지 않았다");
        }

        [Test]
        public void 날이_바뀌면_받은_칸도_다시_열린다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            QuestRun.Bump(s, "kill", 50);
            Assert.IsTrue(QuestRun.Claim(s, d, true, 0));
            QuestRun.Roll(s, d, Tue);
            Assert.IsFalse(s.QuestDailyGot[0], "받은 표시가 지워진다");
            Assert.IsFalse(QuestRun.CanClaim(s, d, true, 0), "다만 점수도 0 이라 아직 못 받는다");
            QuestRun.Bump(s, "kill", 50);
            Assert.IsTrue(QuestRun.CanClaim(s, d, true, 0), "다시 채우면 다시 받을 수 있다");
        }

        [Test]
        public void 세이브에_실려_그대로_돌아온다()
        {
            var d = Table(); var s = Fresh(d, Mon);
            QuestRun.Login(s, d, Mon);
            QuestRun.Bump(s, "kill", 50);
            QuestRun.Bump(s, "chestOpen", 2);
            Assert.IsTrue(QuestRun.Claim(s, d, true, 0));

            var back = SaveData.FromJson(s.ToJson(), TestData.Load());
            Assert.AreEqual(50, QuestRun.Count(back, true, "kill"), "일일 셈");
            Assert.AreEqual(50, QuestRun.Count(back, false, "kill"), "주간 셈");
            Assert.AreEqual(s.QuestDay, back.QuestDay); Assert.AreEqual(s.QuestWeek, back.QuestWeek);
            Assert.AreEqual(s.QuestLoginDay, back.QuestLoginDay, "«로그인 5일» 이 하루를 두 번 세지 않게 하는 값");
            Assert.IsTrue(back.QuestDailyGot[0], "받은 칸이 살아 있어야 껐다 켜서 또 받는 일이 없다");
            Assert.AreEqual(s.QuestDailyGot.Count, back.QuestDailyGot.Count);
        }

        [Test]
        public void 옛_세이브도_깨지지_않는다()
        {
            var d = Table();
            // 이 필드들이 없던 시절의 세이브 — 없으면 기본값이고, 첫 Roll 이 칸을 세운다.
            var old = SaveData.FromJson("{\"v\":2,\"gold\":100}", TestData.Load());
            Assert.IsNotNull(old.QuestDaily); Assert.IsNotNull(old.QuestDailyGot);
            Assert.AreEqual(0, QuestRun.Count(old, true, "kill"));
            QuestRun.Roll(old, d, Mon);
            Assert.AreEqual(d.Daily.Steps.Count, old.QuestDailyGot.Count, "첫 Roll 이 칸을 맞춘다");
            Assert.AreEqual(100, old.Gold, "옛 값은 그대로");
        }
    }
}
