using System;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T257 — 퀘스트 표(<see cref="QuestData"/>)의 자. 주인 2026-09-09 04:5X 가 <b>목록·점수·상품을 전부</b> 줬다.
    /// <para>
    /// 값을 전부 받았다고 자가 <b>표를 통째로 베끼면</b> 그것은 자가 아니라 거울이다(결정 555 가 값을 치른 자리) —
    /// 그래서 여기서 못 박는 것은 <b>주인이 «말로» 준 것</b>과 <b>표가 깨지는 꼴</b> 둘이다:
    /// ⓐ 주인이 입으로 못 박은 수(줄 8개씩 · 합 <b>130</b>·<b>210</b> · 트랙 구간 다섯씩과 그 점수)와 «할 일» 글자 그대로
    /// ⓑ <b>조용한 사고</b>가 나면 파일을 읽는 순간 운다 — 트랙이 안 오르는 차례 · 마지막 칸이 줄을 다 깨도 못 닿음 · goal/medal 이 0.
    /// </para>
    /// ⓑ 가 이 표의 진짜 위험이다: 마지막 칸이 못 닿으면 그 상품은 <b>아무도 못 받는데 화면에는 멀쩡히 뜬다</b>.
    /// </summary>
    public class QuestTests
    {
        static string Path_ => System.IO.Path.Combine("Assets", "KkomaKnight", "quest.json");
        static QuestData Load() => QuestData.Parse(File.ReadAllText(TestData.RepoFile(Path_)));

        // 주인이 쓴 «할 일» 글자 그대로 — 화면이 이 글자를 띄우므로 한 자라도 다르면 주인이 준 말이 아니게 된다.
        static readonly string[] DailyLabels =
        {
            "로그인하기", "적 50개 죽이기", "챕터 두 번 도전", "상자 2번 오픈",
            "장비 합성 2회", "던전 도전 1회", "탐험 보상 받기", "빠른 탐험 보상 받기",
        };
        static readonly string[] WeeklyLabels =
        {
            "적 2,500 죽이기", "로그인 5일", "챕터 도전 25회", "상자 오픈 30회",
            "장비 합성 30회", "던전 클리어 20회", "펫 업그레이드 10번", "탐험 7번 보상 받기",
        };

        [Test]
        public void 주인이_준_줄과_메달_합이_그대로다()
        {
            var d = Load();
            Assert.AreEqual(8, d.Daily.Quests.Count, "일일 줄 수(주인 목록)");
            Assert.AreEqual(8, d.Weekly.Quests.Count, "주간 줄 수(주인 목록)");
            // 주인이 «합» 을 못 박았다 — 낱낱의 점수를 베끼는 대신 이 둘로 오타를 잡는다(한 줄만 틀려도 합이 어긋난다).
            Assert.AreEqual(130, d.Daily.MedalTotal, "일일 메달 합(주인 130)");
            Assert.AreEqual(210, d.Weekly.MedalTotal, "주간 메달 합(주인 210)");
            Assert.AreEqual("bronze", d.Daily.Medal, "일일 = 동메달");
            Assert.AreEqual("silver", d.Weekly.Medal, "주간 = 은메달");
        }

        [Test]
        public void 할_일_글자가_주인이_쓴_그대로다()
        {
            var d = Load();
            for (int i = 0; i < DailyLabels.Length; i++)
                Assert.AreEqual(DailyLabels[i], d.Daily.Quests[i].Label, "일일 " + i + "번 줄");
            for (int i = 0; i < WeeklyLabels.Length; i++)
                Assert.AreEqual(WeeklyLabels[i], d.Weekly.Quests[i].Label, "주간 " + i + "번 줄");
        }

        [Test]
        public void 목표_수는_주인_글자_안에_든_수와_같다()
        {
            var d = Load();
            // 글자에 수가 든 줄은 그 수가 goal 이어야 한다 — «적 50개» 인데 goal 이 5 면 화면 글자와 판정이 갈라진다.
            Assert.AreEqual(50, d.Daily.Quests[1].Goal, "«적 50개 죽이기»");
            Assert.AreEqual(2, d.Daily.Quests[2].Goal, "«챕터 두 번 도전»");
            Assert.AreEqual(2500, d.Weekly.Quests[0].Goal, "«적 2,500 죽이기»");
            Assert.AreEqual(5, d.Weekly.Quests[1].Goal, "«로그인 5일»");
            Assert.AreEqual(7, d.Weekly.Quests[7].Goal, "«탐험 7번 보상 받기»");
            foreach (var q in d.Daily.Quests) Assert.GreaterOrEqual(q.Goal, 1, "일일 «" + q.Label + "» 의 목표");
            foreach (var q in d.Weekly.Quests) Assert.GreaterOrEqual(q.Goal, 1, "주간 «" + q.Label + "» 의 목표");
        }

        [Test]
        public void 주인이_다르게_쓴_낱말은_다른_카운터다()
        {
            var d = Load();
            // «던전 도전»(일일) ↔ «던전 클리어»(주간) 은 주인이 다른 낱말을 썼다 — 하나로 묶으면 도전만 하고 주간이 깨진다.
            Assert.AreEqual("dungeonTry", d.Daily.Quests[5].Counter, "일일 «던전 도전 1회»");
            Assert.AreEqual("dungeonClear", d.Weekly.Quests[5].Counter, "주간 «던전 클리어 20회»");
            // «로그인하기»(하루 한 번) ↔ «로그인 5일»(서로 다른 날 5일)도 세는 것이 다르다.
            Assert.AreEqual("login", d.Daily.Quests[0].Counter, "일일 «로그인하기»");
            Assert.AreEqual("loginDays", d.Weekly.Quests[1].Counter, "주간 «로그인 5일»");
            // 같은 것을 세는 자리는 같은 이름이어야 2단계 훅이 한 곳이다.
            Assert.AreEqual(d.Daily.Quests[1].Counter, d.Weekly.Quests[0].Counter, "적 처치는 일일·주간이 같은 셈");
        }

        [Test]
        public void 트랙_구간과_상품이_주인이_준_그대로다()
        {
            var d = Load();
            Assert.AreEqual(new[] { 20, 40, 60, 80, 100 }, Points(d.Daily), "일일 트랙 구간");
            Assert.AreEqual(new[] { 30, 60, 90, 120, 150 }, Points(d.Weekly), "주간 트랙 구간");
            // 티켓은 던전마다 따로다(T99) — 40 과 80 이 «서로 다른 던전» 이라는 것이 주인 말의 핵심이다.
            var t40 = d.Daily.Steps[1].Rewards[0]; var t80 = d.Daily.Steps[3].Rewards[0];
            Assert.AreEqual("ticket", t40.Item); Assert.AreEqual("hell", t40.Dungeon, "40 = 지옥문 티켓");
            Assert.AreEqual("ticket", t80.Item); Assert.AreEqual("expedition", t80.Dungeon, "80 = 원정 티켓");
            Assert.AreNotEqual(t40.Dungeon, t80.Dungeon, "둘은 서로 다른 던전의 티켓이다");
            // 키 셋의 이름은 **T255 가 정본으로 세운 것**(`GachaKeys`)이다 — 새로 짓지도, 상인 상품표의 «rareKey·epicKey·legendKey» 를 쓰지도 않는다.
            //  그 셋은 «상인이 파는 물건 줄» 의 이름이고, **재화를 더하고 빼는 길**(`Mail.CanPay` → `GachaKeys`)이 아는 이름은 이 색 이름이다.
            //  ⇒ 여기서 이름이 어긋나면 2단계에서 트랙 상품이 **조용히 지급 안 되는** 자리가 된다. 그래서 상수로 못 박는다.
            Assert.AreEqual(GachaKeys.Blue, d.Daily.Steps[0].Rewards[0].Item, "일일 20 = 파란 키");
            Assert.AreEqual(GachaKeys.Purple, d.Weekly.Steps[1].Rewards[0].Item, "주간 60 = 보라 키");
            Assert.AreEqual(GachaKeys.Yellow, d.Weekly.Steps[3].Rewards[0].Item, "주간 120 = 노란 키");
            // 그리고 트랙 상품이 **전부 지급 길이 아는 이름**이어야 한다(티켓은 재화가 아니라 던전 보유량이라 뺀다).
            foreach (var tr in new[] { d.Daily, d.Weekly })
                foreach (var st in tr.Steps)
                    foreach (var rw in st.Rewards)
                        if (rw.Item != "ticket")
                            Assert.IsTrue(Mail.CanPay(rw.Item), "지급 길이 모르는 상품 이름 «" + rw.Item + "» — 2단계에서 조용히 안 들어온다");
            foreach (var s in d.Daily.Steps) Assert.IsNotEmpty(s.Rewards, "일일 트랙 " + s.Points + " 의 상품");
            foreach (var s in d.Weekly.Steps) Assert.IsNotEmpty(s.Rewards, "주간 트랙 " + s.Points + " 의 상품");
        }

        [Test]
        public void 마지막_칸은_줄을_다_깨면_닿고_주간은_60이_남는다()
        {
            var d = Load();
            Assert.LessOrEqual(d.Daily.Steps[4].Points, d.Daily.MedalTotal, "일일 마지막 칸(100) ≤ 합(130)");
            Assert.LessOrEqual(d.Weekly.Steps[4].Points, d.Weekly.MedalTotal, "주간 마지막 칸(150) ≤ 합(210)");
            Assert.AreEqual(30, d.Daily.Spare, "일일은 30 이 남는다");
            // 주인이 «남는 점수는 버린다» 고 한 그 60 — 수가 달라지면 그 말이 가리키는 자리가 없어진다.
            Assert.AreEqual(60, d.Weekly.Spare, "주간은 60 이 남는다(다음 구간 없음 · 주인 «버린다»)");
        }

        [Test]
        public void 채운_만큼만_열린다()
        {
            var t = Load().Daily;
            Assert.AreEqual(0, t.OpenCount(19), "19 점이면 아무 칸도 안 열린다");
            Assert.AreEqual(1, t.OpenCount(20), "20 점이면 20 자리 하나(주인 «20포인트 채워지면 20포인트 부분»)");
            Assert.AreEqual(1, t.OpenCount(39), "39 점이어도 아직 하나");
            Assert.AreEqual(5, t.OpenCount(100), "100 이면 다섯 자리 전부");
            Assert.AreEqual(5, t.OpenCount(130), "합을 다 채워도 칸은 다섯뿐(남는 점수는 버린다)");
            Assert.IsTrue(t.IsOpen(0, 20)); Assert.IsFalse(t.IsOpen(1, 20), "20 점에 40 자리는 아직 안 열린다");
            Assert.IsFalse(t.IsOpen(9, 130), "없는 칸은 안 열린다");
        }

        [Test]
        public void 진행_표시는_목표에서_멈춘다()
        {
            var q = Load().Daily.Quests[1];   // «적 50개 죽이기»
            Assert.AreEqual(0, q.Shown(-3), "음수는 0 으로");
            Assert.AreEqual(34, q.Shown(34), "«34/50»");
            Assert.AreEqual(50, q.Shown(77), "넘겨도 «50/50» — 화면에 77/50 이 뜨지 않는다");
            Assert.IsFalse(q.Done(49)); Assert.IsTrue(q.Done(50)); Assert.IsTrue(q.Done(51));
        }

        // ── 조용한 사고를 파일 읽는 순간 잡는가 ────────────────────────────────
        [Test]
        public void 트랙이_오르는_차례가_아니면_읽다가_운다()
        {
            var e = Assert.Throws<FormatException>(() => QuestData.Parse(Tweak("\"points\": 40,", "\"points\": 10,")));
            StringAssert.Contains("오르는 차례", e.Message);
        }

        [Test]
        public void 마지막_칸이_못_닿으면_읽다가_운다()
        {
            // 아무도 못 받는 상품 — 화면에는 멀쩡히 뜨므로 눈으로는 영영 안 잡힌다.
            var e = Assert.Throws<FormatException>(() => QuestData.Parse(Tweak("\"points\": 100,", "\"points\": 999,")));
            StringAssert.Contains("아무도 못 받는다", e.Message);
        }

        [Test]
        public void 목표가_0_이면_읽다가_운다()
        {
            var e = Assert.Throws<FormatException>(() => QuestData.Parse(Tweak("\"goal\": 50,", "\"goal\": 0,")));
            StringAssert.Contains("goal", e.Message);
        }

        // ── 날짜·주 열쇠 ───────────────────────────────────────────────────────
        [Test]
        public void 일일_열쇠는_출석과_같은_꼴이다()
        {
            Assert.AreEqual("2026-09-08", QuestData.DayKey(new DateTime(2026, 9, 8, 23, 59, 0)));
            Assert.AreNotEqual(QuestData.DayKey(new DateTime(2026, 9, 8, 23, 59, 0)),
                               QuestData.DayKey(new DateTime(2026, 9, 9, 0, 0, 0)), "자정에 갈린다");
        }

        [Test]
        public void 주간_열쇠는_월요일_0시에_갈린다()
        {
            var d = Load();
            Assert.AreEqual(1, d.WeekStartDow, "기준 요일 = 월요일(표가 정한다)");
            // 2026-09-07 은 월요일이다.
            string mon = d.WeekKey(new DateTime(2026, 9, 7, 0, 0, 0));
            Assert.AreEqual("2026-09-07", mon, "월요일 0시가 그 주의 시작");
            Assert.AreEqual(mon, d.WeekKey(new DateTime(2026, 9, 13, 23, 59, 0)), "그 주 일요일 끝까지 같은 주");
            Assert.AreNotEqual(mon, d.WeekKey(new DateTime(2026, 9, 14, 0, 0, 0)), "다음 월요일 0시에 갈린다");
            Assert.AreEqual("2026-08-31", d.WeekKey(new DateTime(2026, 9, 6, 12, 0, 0)), "일요일은 지난주 것이다");
        }

        /// <summary>표 한 곳만 바꾼 사본 — 자가 «표를 고치다 낼 오타» 를 흉내 낸다(원본 파일은 안 건드린다).</summary>
        static string Tweak(string from, string to)
        {
            string json = File.ReadAllText(TestData.RepoFile(Path_));
            int i = json.IndexOf(from, StringComparison.Ordinal);
            Assert.GreaterOrEqual(i, 0, "흉내 낼 자리 «" + from + "» 이 표에 있어야 이 자가 성립한다");
            return json.Substring(0, i) + to + json.Substring(i + from.Length);
        }

        static int[] Points(QuestData.Track t)
        {
            var a = new int[t.Steps.Count];
            for (int i = 0; i < a.Length; i++) a[i] = t.Steps[i].Points;
            return a;
        }
    }
}
