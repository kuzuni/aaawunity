using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 아레나 더미 승점·전투력(T81 · 주인 2026-09-07 «아레나 부분에 적들 승점이랑 전투력 더미값으로 넣어줘라»).
    /// 값 자체는 <c>Assets/KkomaKnight/arenaDummy.json</c> 에 있고 주인이 바꿀 수 있으므로, 표와 지시가 같은지는 한 테스트에만 두고
    /// 나머지는 «꼴» 을 본다 — 결정적(같은 순위면 언제나 같은 값) · 순위가 내려가면 반드시 낮아짐(단조) · 내 순위는 실제 전투력 · 최소값에서 멈춤.
    /// </summary>
    public class ArenaDummyTests
    {
        const double MyPower = 11253;

        static ArenaDummyData Load() => ArenaDummyData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "arenaDummy.json"))));

        [Test]
        public void Json_IsOwnersTable()
        {
            // ROUTINE T81 1항: 1위 ×1.6 … 내 순위 ×1.0 … 아래 ×0.6 · ±5% · 승점 2,400 부터 순위마다 −40~−60 · 최소 0.
            var d = Load();
            Assert.That(d.PowerTop, Is.EqualTo(1.6).Within(1e-9), "1위 계수");
            Assert.That(d.PowerMe, Is.EqualTo(1.0).Within(1e-9), "내 순위 계수");
            Assert.That(d.PowerBottom, Is.EqualTo(0.6).Within(1e-9), "바닥 계수");
            Assert.That(d.PowerJitter, Is.EqualTo(0.05).Within(1e-9), "±5% 흔들림");
            Assert.That(d.ScoreTop, Is.EqualTo(2400).Within(1e-9), "1위 승점");
            Assert.That(d.ScoreStepMin, Is.EqualTo(40).Within(1e-9), "순위마다 최소 감소");
            Assert.That(d.ScoreStepMax, Is.EqualTo(60).Within(1e-9), "순위마다 최대 감소");
            Assert.That(d.ScoreMin, Is.EqualTo(0).Within(1e-9), "최소 승점");
            Assert.That(d.MeRank, Is.EqualTo(1), "우리 껍데기는 내가 1위(시상대 가운데)");
        }

        [Test]
        public void SameRank_SameValue()
        {
            var d = Load();
            for (int r = 1; r <= 30; r++)
            {
                Assert.That(ArenaDummy.Power(d, MyPower, r), Is.EqualTo(ArenaDummy.Power(d, MyPower, r)), "전투력이 부를 때마다 달라지면 안 된다 (순위 " + r + ")");
                Assert.That(ArenaDummy.Score(d, r), Is.EqualTo(ArenaDummy.Score(d, r)), "승점이 부를 때마다 달라지면 안 된다 (순위 " + r + ")");
            }
            // 표를 다시 읽어도 같아야 한다 — 시드는 순위 하나뿐이다.
            var d2 = Load();
            Assert.That(ArenaDummy.Power(d2, MyPower, 7), Is.EqualTo(ArenaDummy.Power(d, MyPower, 7)), "표를 다시 읽어도 같은 값");
        }

        [Test]
        public void PowerAndScore_FallWithRank()
        {
            var d = Load();
            double prevP = ArenaDummy.Power(d, MyPower, 2), prevS = ArenaDummy.Score(d, 1);
            for (int r = 3; r <= 40; r++)
            {
                double p = ArenaDummy.Power(d, MyPower, r), s = ArenaDummy.Score(d, r);
                Assert.That(p, Is.LessThanOrEqualTo(prevP), "순위 " + r + " 전투력이 위 순위보다 높다");
                Assert.That(s, Is.LessThanOrEqualTo(prevS), "순위 " + r + " 승점이 위 순위보다 높다");
                prevP = p; prevS = s;
            }
        }

        [Test]
        public void MyRow_UsesRealPower()
        {
            var d = Load();
            Assert.That(ArenaDummy.Power(d, MyPower, d.MeRank), Is.EqualTo(MyPower), "내 줄은 실제 전투력 그대로");
            Assert.That(ArenaDummy.Power(d, MyPower, d.MeRank + 1), Is.LessThan(MyPower), "내 아래 순위는 내 전투력보다 낮다");
        }

        [Test]
        public void PowerStaysInsideTheCurve()
        {
            var d = Load();
            // 앵커 곡선 ±(흔들림 누적) 안 — 바닥 아래로 내려가거나 1위 위로 올라가면 표가 뜻을 잃는다.
            for (int r = d.MeRank + 1; r <= 60; r++)
            {
                double f = ArenaDummy.Factor(d, r);
                Assert.That(f, Is.LessThan(d.PowerMe), "순위 " + r + " 계수는 내 계수보다 작아야 한다");
                Assert.That(f, Is.GreaterThan(0), "계수는 0 보다 커야 한다 (순위 " + r + ")");
            }
            Assert.That(ArenaDummy.Factor(d, 200), Is.LessThan(d.PowerBottom + 0.2), "바닥 순위 한참 아래면 바닥 근처");
        }

        [Test]
        public void Score_StopsAtMinimum()
        {
            var d = Load();
            Assert.That(ArenaDummy.Score(d, 1), Is.EqualTo(d.ScoreTop), "1위 = 표의 top");
            Assert.That(ArenaDummy.Score(d, 500), Is.EqualTo(d.ScoreMin), "한참 아래 순위는 최소값에서 멈춘다");
            Assert.That(ArenaDummy.Score(d, 500), Is.GreaterThanOrEqualTo(0), "승점은 음수가 안 된다");
        }

        [Test]
        public void Parse_RejectsBrokenTables()
        {
            Assert.Throws<System.FormatException>(() => ArenaDummyData.Parse("{\"meRank\":0,\"power\":{\"top\":1.6,\"me\":1,\"bottom\":0.6,\"bottomRank\":20},\"score\":{\"top\":2400,\"stepMin\":40,\"stepMax\":60}}"), "meRank 0");
            Assert.Throws<System.FormatException>(() => ArenaDummyData.Parse("{\"meRank\":1,\"power\":{\"top\":1.6,\"me\":1,\"bottom\":0.6,\"bottomRank\":1},\"score\":{\"top\":2400,\"stepMin\":40,\"stepMax\":60}}"), "bottomRank ≤ meRank");
            Assert.Throws<System.FormatException>(() => ArenaDummyData.Parse("{\"meRank\":1,\"power\":{\"top\":0.5,\"me\":1,\"bottom\":0.6,\"bottomRank\":20},\"score\":{\"top\":2400,\"stepMin\":40,\"stepMax\":60}}"), "top < me");
            Assert.Throws<System.FormatException>(() => ArenaDummyData.Parse("{\"meRank\":1,\"power\":{\"top\":1.6,\"me\":1,\"bottom\":0.6,\"bottomRank\":20},\"score\":{\"top\":2400,\"stepMin\":60,\"stepMax\":40}}"), "stepMax < stepMin");
        }
    }
}
