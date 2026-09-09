using System;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T311 2항 — «새로고침까지» 시계(주인 2026-09-09 «새로고침까지 부분 실제 시간으로 카운트 되게»).
    /// <para>
    /// ⚠ 이 자의 요점은 «초를 맞게 세나» 가 아니라 <b>«화면이 말하는 시각과 실제로 지워지는 시각이 같은가»</b> 다.
    /// 둘이 갈라지면 화면은 «3일 남음» 이라 적는데 이미 지워져 있고, 그것은 빨간 줄도 자도 안 난다 —
    /// 주인 폰에서 «어제 안 받았는데 사라졌다» 로만 나타난다.
    /// </para>
    /// 그래서 <b>남은 초를 앞으로 감아 <see cref="QuestRun.Roll"/> 이 그 순간에 실제로 지우는지</b> 를 본다(수를 견주지 않는다 · 결정 555).
    /// </summary>
    public class QuestResetClockTests
    {
        static QuestData Table() =>
            QuestData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "quest.json"))));

        static SaveData Rolled(QuestData d, DateTime now)
        {
            var s = new SaveData();
            QuestRun.Roll(s, d, now);
            return s;
        }

        [Test]
        public void 일일은_다음_날_자정까지다()
        {
            var t = new DateTime(2026, 9, 9, 13, 24, 36);
            // 13:24:36 → 다음 날 00:00 까지 10시간 35분 24초
            Assert.AreEqual(10 * 3600 + 35 * 60 + 24, QuestRun.SecondsToDailyReset(t), 1e-6);
            Assert.AreEqual(24 * 3600, QuestRun.SecondsToDailyReset(t.Date), 1e-6, "자정 직후면 꼬박 하루가 남는다");
            Assert.AreEqual(1, QuestRun.SecondsToDailyReset(t.Date.AddDays(1).AddSeconds(-1)), 1e-6, "23:59:59 면 1초");
        }

        [Test]
        public void 주간은_다음_주_시작_요일_자정까지다()
        {
            var d = Table();
            var t = new DateTime(2026, 9, 9, 13, 24, 36);            // 수요일
            var start = QuestRun.WeekStart(d, t);
            Assert.AreEqual(d.WeekStartDow, (int)start.DayOfWeek, "주 시작 날의 요일 = 표의 weekStartDow");
            Assert.AreEqual(TimeSpan.Zero, start.TimeOfDay, "주 시작은 그 날 00:00");
            Assert.LessOrEqual(start, t, "주 시작은 지금보다 앞이다");
            Assert.AreEqual((start.AddDays(7) - t).TotalSeconds, QuestRun.SecondsToWeeklyReset(d, t), 1e-6);
        }

        [Test]
        public void 남은_초는_한_주와_하루를_안_넘고_음수도_아니다()
        {
            var d = Table();
            // 한 주를 시간 단위로 훑는다 — 어느 시각에 물어도 범위 밖이 없어야 한다.
            var t0 = new DateTime(2026, 9, 6, 0, 0, 0);
            for (int h = 0; h < 24 * 7; h++)
            {
                var t = t0.AddHours(h);
                double day = QuestRun.SecondsToDailyReset(t), week = QuestRun.SecondsToWeeklyReset(d, t);
                Assert.Greater(day, 0, "일일 남은 초는 0 보다 크다 @" + t);
                Assert.LessOrEqual(day, 24 * 3600, "하루를 넘지 않는다 @" + t);
                Assert.Greater(week, 0, "주간 남은 초는 0 보다 크다 @" + t);
                Assert.LessOrEqual(week, 7 * 24 * 3600, "한 주를 넘지 않는다 @" + t);
            }
        }

        /// <summary>
        /// 이 자가 이 파일의 <b>요점</b>이다 — 남은 초만큼 감으면 <see cref="QuestRun.Roll"/> 이 <b>그때</b> 지운다.
        /// 시각을 두 곳에서 따로 셈하면(화면이 제 나름대로 요일 산수를 하면) 여기서 갈라진다.
        /// </summary>
        [Test]
        public void 남았다고_적은_그_순간에_실제로_지워진다()
        {
            var d = Table();
            var now = new DateTime(2026, 9, 9, 13, 24, 36);

            // ── 일일
            {
                var s = Rolled(d, now);
                QuestRun.Bump(s, "kill", 7);
                double left = QuestRun.SecondsToDailyReset(now);
                Assert.IsFalse(QuestRun.Roll(s, d, now.AddSeconds(left - 1)), "1초 전에는 아직 안 지운다");
                Assert.AreEqual(7, QuestRun.Count(s, true, "kill"), "1초 전에는 셈이 그대로다");
                Assert.IsTrue(QuestRun.Roll(s, d, now.AddSeconds(left)), "남았다고 적은 그 초에 지운다");
                Assert.AreEqual(0, QuestRun.Count(s, true, "kill"), "일일 셈이 0 이 된다");
            }
            // ── 주간
            {
                var s = Rolled(d, now);
                QuestRun.Bump(s, "kill", 7);
                double left = QuestRun.SecondsToWeeklyReset(d, now);
                QuestRun.Roll(s, d, now.AddSeconds(left - 1));
                Assert.AreEqual(7, QuestRun.Count(s, false, "kill"), "1초 전에는 주간 셈이 그대로다");
                QuestRun.Roll(s, d, now.AddSeconds(left));
                Assert.AreEqual(0, QuestRun.Count(s, false, "kill"), "남았다고 적은 그 초에 주간이 지워진다");
            }
        }

        /// <summary>
        /// 주 시작 요일을 바꿔도 «적힌 시각 ↔ 지워지는 시각» 이 같이 움직인다 —
        /// <see cref="QuestRun.WeekStart"/> 가 요일 산수를 <b>베끼지 않고</b> <see cref="QuestData.WeekKey(DateTime)"/> 를 되읽는 까닭이 이것이다.
        /// </summary>
        [Test]
        public void 주_시작_요일을_바꿔도_둘이_같이_움직인다()
        {
            var d = Table();
            var now = new DateTime(2026, 9, 9, 13, 24, 36);
            for (int dow = 0; dow < 7; dow++)
            {
                d.WeekStartDow = dow;
                var s = Rolled(d, now);
                QuestRun.Bump(s, "kill", 3);
                double left = QuestRun.SecondsToWeeklyReset(d, now);
                QuestRun.Roll(s, d, now.AddSeconds(left - 1));
                Assert.AreEqual(3, QuestRun.Count(s, false, "kill"), "weekStartDow=" + dow + " · 1초 전");
                QuestRun.Roll(s, d, now.AddSeconds(left));
                Assert.AreEqual(0, QuestRun.Count(s, false, "kill"), "weekStartDow=" + dow + " · 그 초");
            }
        }
    }
}
