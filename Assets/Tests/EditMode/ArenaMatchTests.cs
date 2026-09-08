using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T240 5항 — 아레나 한 판의 <b>승점·순위·티어</b> 자. 주인 2026-09-08 11:2X
    /// «이기면 승점 올라가고 순위 올라가고 그런 느낌, 지면 승점 떨어지고».
    /// <para>
    /// 주인 문장을 그대로 자로 옮긴다 — ⓐ 이기면 <b>+8</b> · ⓑ 지면 <b>−6</b> · ⓒ <b>0 미만 없음</b> ·
    /// ⓓ 이기면 순위가 <b>오르고</b>(수가 작아지고) 지면 <b>내린다</b> · ⓔ 순위는 «내 승점보다 높은 더미 수 + 1» 그 셈 그대로다.
    /// </para>
    /// ⚠ 여기 <b>전투는 없다</b> — 무대·결과 화면은 <c>EventsScreen.cs</c>(T236 lock) 뒤 배선 회차 몫이다.
    /// </summary>
    public class ArenaMatchTests
    {
        static ArenaMatchData Load() => ArenaMatchData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "arenaMatch.json"))));
        static ArenaDummyData Dummies() => ArenaDummyData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "arenaDummy.json"))));

        [Test]
        public void TableHasTheNumbersTheOwnerGave()
        {
            var m = Load();
            Assert.AreEqual(8, m.Win, "레퍼런스 34 의 초록 «+8»");
            Assert.AreEqual(-6, m.Lose, "레퍼런스 34 의 빨강 «−6»(부호까지 표에 적혀 있다)");
            Assert.AreEqual(0, m.MinScore, "주인 «0 미만으로는 안 내려간다»");
            Assert.GreaterOrEqual(m.StartScore, m.MinScore);
            Assert.Greater(m.Tiers.Count, 1, "티어가 하나뿐이면 «구간» 이 아니다");
            Assert.AreEqual(m.MinScore, m.Tiers[0].From, "바닥에도 이름이 있어야 한다");
        }

        [Test]
        public void WinningAddsAndLosingSubtractsAndItNeverGoesBelowZero()
        {
            var m = Load();
            Assert.AreEqual(108, ArenaMatch.Apply(m, 100, true));
            Assert.AreEqual(94, ArenaMatch.Apply(m, 100, false));
            // 바닥 — 4 에서 지면 −6 이 아니라 0 에서 멈춘다
            Assert.AreEqual(0, ArenaMatch.Apply(m, 4, false));
            Assert.AreEqual(0, ArenaMatch.Apply(m, 0, false));
            // 그리고 «실제로 움직인 값» 은 −6 이 아니라 −4 다(화면이 −6 이라 적으면 거짓말이 된다)
            Assert.AreEqual(-4, ArenaMatch.Delta(m, 4, false));
            Assert.AreEqual(-6, ArenaMatch.Delta(m, 100, false));
            Assert.AreEqual(8, ArenaMatch.Delta(m, 100, true));
        }

        [Test]
        public void RankIsTheNumberOfDummiesAboveMePlusOne()
        {
            var d = Dummies();
            // 정의 그대로 손으로 센 값과 같은가 — 자가 셈을 «베끼지» 않게 다른 길로 센다
            foreach (var score in new double[] { 0, 100, 500, 1000, 2000, 2400, 9999 })
            {
                int above = 0;
                for (int r = 1; r <= 200; r++) if (ArenaDummy.Score(d, r) > score) above++;
                Assert.AreEqual(above + 1, ArenaMatch.RankOf(d, score), "승점 " + score + " 의 순위");
            }
            // 1위 더미보다 높으면 내가 1위 · 그 값과 같아도 «높은 더미» 는 없으니 1위다
            Assert.AreEqual(1, ArenaMatch.RankOf(d, ArenaDummy.Score(d, 1)));
            Assert.AreEqual(1, ArenaMatch.RankOf(d, ArenaDummy.Score(d, 1) + 1));
        }

        [Test]
        public void WinningRaisesTheRankAndLosingLowersIt()
        {
            var m = Load();
            var d = Dummies();
            // 바닥에서 시작해 이기기만 하면 순위는 «절대 나빠지지 않고» 언젠가 실제로 좋아진다(주인 문장의 앞 절반)
            double s = m.StartScore;
            int r0 = ArenaMatch.RankOf(d, s), worst = r0, best = r0;
            for (int i = 0; i < 400; i++)
            {
                s = ArenaMatch.Apply(m, s, true);
                int r = ArenaMatch.RankOf(d, s);
                Assert.LessOrEqual(r, worst, "이겼는데 순위가 내려갔다(회 " + i + ")");
                worst = r; best = ArenaMatch.BetterRank(best, r);
            }
            Assert.Less(best, r0, "400 판을 이겼는데 순위가 한 칸도 안 올랐다");

            // 지면 내려간다(뒤 절반) — 위에서 올라간 자리에서 내리 지면 순위 수가 커진다
            double top = s;
            int rt = ArenaMatch.RankOf(d, top);
            for (int i = 0; i < 400; i++) top = ArenaMatch.Apply(m, top, false);
            Assert.Greater(ArenaMatch.RankOf(d, top), rt, "400 판을 졌는데 순위가 그대로다");
            Assert.GreaterOrEqual(top, m.MinScore);
        }

        [Test]
        public void TierIsTheNameOfTheScoreBand()
        {
            var m = Load();
            // 바닥과 꼭대기 사이 어디를 찍어도 이름이 나온다 · 경계값은 «그 티어부터» 다
            Assert.AreEqual(m.Tiers[0].Name, ArenaMatch.TierOf(m, m.MinScore));
            for (int i = 0; i < m.Tiers.Count; i++)
            {
                Assert.AreEqual(m.Tiers[i].Name, ArenaMatch.TierOf(m, m.Tiers[i].From), "경계 승점은 그 티어에 든다");
                if (i > 0) Assert.AreEqual(m.Tiers[i - 1].Name, ArenaMatch.TierOf(m, m.Tiers[i].From - 1), "경계 한 칸 아래는 앞 티어다");
            }
            Assert.AreEqual(m.Tiers[m.Tiers.Count - 1].Name, ArenaMatch.TierOf(m, 999999), "표 위로 넘어가도 마지막 티어 이름이 남는다");
        }

        [Test]
        public void BrokenTablesShoutInsteadOfDrawingSomethingPlausible()
        {
            // 이 표의 사고는 조용하다 — 읽는 순간 우는 편이 낫다(T237 과 같은 갈래)
            Assert.Throws<System.FormatException>(() => ArenaMatchData.Parse("{\"win\":-1,\"lose\":-6,\"minScore\":0,\"tiers\":[{\"name\":\"a\",\"from\":0}]}"));
            Assert.Throws<System.FormatException>(() => ArenaMatchData.Parse("{\"win\":8,\"lose\":6,\"minScore\":0,\"tiers\":[{\"name\":\"a\",\"from\":0}]}"));
            Assert.Throws<System.FormatException>(() => ArenaMatchData.Parse("{\"win\":8,\"lose\":-6,\"minScore\":0,\"tiers\":[]}"));
            Assert.Throws<System.FormatException>(() => ArenaMatchData.Parse("{\"win\":8,\"lose\":-6,\"minScore\":0,\"tiers\":[{\"name\":\"a\",\"from\":10}]}"));
            Assert.Throws<System.FormatException>(() => ArenaMatchData.Parse("{\"win\":8,\"lose\":-6,\"minScore\":0,\"tiers\":[{\"name\":\"a\",\"from\":0},{\"name\":\"b\",\"from\":0}]}"));
            Assert.Throws<System.FormatException>(() => ArenaMatchData.Parse("{\"win\":8,\"lose\":-6,\"minScore\":0,\"tiers\":[{\"name\":\"a\",\"from\":0},{\"name\":\"\",\"from\":9}]}"));
            Assert.Throws<System.FormatException>(() => ArenaMatchData.Parse("{\"win\":8,\"lose\":-6,\"minScore\":10,\"startScore\":0,\"tiers\":[{\"name\":\"a\",\"from\":10}]}"));
        }

        [Test]
        public void OldSavesLoadWithoutArenaFieldsAndTheNewOnesRoundTrip()
        {
            var gd = TestData.Load();
            var s = new SaveData();
            Assert.AreEqual(0, s.ArenaScore);
            Assert.AreEqual(0, s.ArenaBest, "0 = 아직 한 판도 안 했다");

            s.ArenaScore = 123; s.ArenaBest = 7;
            var back = SaveData.FromJson(s.ToJson(), gd);
            Assert.AreEqual(123, back.ArenaScore);
            Assert.AreEqual(7, back.ArenaBest);

            // 옛 세이브(필드 없음)도 안 깨진다 — 세이브 버전은 그대로다
            var old = SaveData.FromJson("{\"v\":2,\"gold\":10,\"gem\":1}", gd);
            Assert.AreEqual(0, old.ArenaScore);
            Assert.AreEqual(0, old.ArenaBest);
        }

        [Test]
        public void BestRankKeepsTheHighestAndTreatsZeroAsNone()
        {
            Assert.AreEqual(3, ArenaMatch.BetterRank(0, 3), "0 은 «아직 없다» 라 상대가 이긴다");
            Assert.AreEqual(3, ArenaMatch.BetterRank(3, 0));
            Assert.AreEqual(2, ArenaMatch.BetterRank(5, 2), "수가 작은 쪽이 더 높은 순위다");
            Assert.AreEqual(2, ArenaMatch.BetterRank(2, 5));
            Assert.AreEqual(0, ArenaMatch.BetterRank(0, 0));
        }
    }
}
