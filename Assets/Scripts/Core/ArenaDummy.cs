using System;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 아레나 껍데기의 «상대 승점·전투력» 표시용 계수표 (<c>Assets/KkomaKnight/arenaDummy.json</c> · T81 · 주인 2026-09-07).
    /// 밸런스 수치가 아니라 <b>화면에 뿌리는 더미</b>다 — 전투 엔진은 이 값을 쓰지 않는다. 값·개수는 전부 파일에서 온다(코드 상수 0 · ROUTINE §1).
    /// </summary>
    public sealed class ArenaDummyData
    {
        /// <summary>내 순위(우리 껍데기는 시상대 가운데 = 1). 이 순위의 계수가 <see cref="PowerMe"/>.</summary>
        public int MeRank = 1;
        /// <summary>1위 계수 · 내 순위 계수 · 바닥 계수 · 바닥에 닿는 순위 · 계단 폭 흔들림(0~1).</summary>
        public double PowerTop = 1.6, PowerMe = 1.0, PowerBottom = 0.6, PowerJitter = 0.05;
        public int PowerBottomRank = 20;
        /// <summary>1위 승점 · 순위마다 빠지는 폭의 최소/최대 · 최소 승점.</summary>
        public double ScoreTop = 2400, ScoreStepMin = 40, ScoreStepMax = 60, ScoreMin = 0;

        public static ArenaDummyData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static ArenaDummyData From(JNode j)
        {
            var d = new ArenaDummyData();
            d.MeRank = (int)j["meRank"].Num(1);
            var p = j["power"];
            d.PowerTop = p["top"].ReqNum("power.top");
            d.PowerMe = p["me"].ReqNum("power.me");
            d.PowerBottom = p["bottom"].ReqNum("power.bottom");
            d.PowerBottomRank = (int)p["bottomRank"].ReqNum("power.bottomRank");
            d.PowerJitter = p["jitter"].Num();
            var s = j["score"];
            d.ScoreTop = s["top"].ReqNum("score.top");
            d.ScoreStepMin = s["stepMin"].ReqNum("score.stepMin");
            d.ScoreStepMax = s["stepMax"].ReqNum("score.stepMax");
            d.ScoreMin = s["min"].Num();
            if (d.MeRank < 1) throw new FormatException("arenaDummy.json: meRank 는 1 이상이어야 한다");
            if (d.PowerBottomRank <= d.MeRank) throw new FormatException("arenaDummy.json: power.bottomRank 는 meRank 보다 커야 한다");
            if (d.PowerMe < d.PowerBottom || d.PowerTop < d.PowerMe) throw new FormatException("arenaDummy.json: power 계수는 top ≥ me ≥ bottom 이어야 한다");
            if (d.ScoreStepMax < d.ScoreStepMin) throw new FormatException("arenaDummy.json: score.stepMax 는 stepMin 이상이어야 한다");
            return d;
        }
    }

    /// <summary>
    /// 아레나 상대의 더미 승점·전투력 (T81 · 순수 C# · 결정적).
    /// <list type="bullet">
    /// <item><b>전투력</b> = 내 전투력 × 계수. 계수 곡선은 «1위 top → 내 순위 me → bottomRank bottom» 꺾은 직선이고 그 아래는 bottom 으로 눕는다.
    /// 흔들림은 <b>계단 폭</b>에 곱한다 — 그래야 순위가 내려갈 때 값이 반드시 낮아진다(레벨에 곱하면 ±5% 가 계단(≈2%)보다 커서 뒤집힌다).</item>
    /// <item><b>승점</b> = 1위 top 에서 순위마다 stepMin~stepMax 씩 빠지는 단조 감소 · <see cref="ArenaDummyData.ScoreMin"/> 에서 멈춘다.</item>
    /// <item>시드는 순위 하나뿐이라 같은 순위는 언제 봐도 같은 값이다(껍데기가 화면을 열 때마다 흔들리지 않는다).</item>
    /// </list>
    /// </summary>
    public static class ArenaDummy
    {
        /// <summary>순위 하나로 만드는 0~1 난수(결정적 · 표시용이라 분포만 고르면 된다).</summary>
        public static double Unit(int rank)
        {
            uint x = (uint)rank * 2654435761u + 0x9E3779B9u;
            x ^= x >> 15; x *= 2246822519u;
            x ^= x >> 13; x *= 3266489917u;
            x ^= x >> 16;
            return x / 4294967296.0;
        }

        /// <summary>앵커 곡선(흔들림 없는 계수).</summary>
        static double BaseFactor(ArenaDummyData d, int rank)
        {
            if (rank <= 1) return d.MeRank <= 1 ? d.PowerMe : d.PowerTop;
            if (rank <= d.MeRank)
            {
                double t = (rank - 1.0) / (d.MeRank - 1.0);
                return d.PowerTop + (d.PowerMe - d.PowerTop) * t;
            }
            if (rank >= d.PowerBottomRank) return d.PowerBottom;
            double u = (rank - (double)d.MeRank) / (d.PowerBottomRank - (double)d.MeRank);
            return d.PowerMe + (d.PowerBottom - d.PowerMe) * u;
        }

        /// <summary>순위의 전투력 계수 — 계단 폭에 ±jitter 를 곱해 쌓는다(단조 감소 보장).</summary>
        public static double Factor(ArenaDummyData d, int rank)
        {
            if (d == null) return 1.0;
            if (rank < 1) rank = 1;
            double f = BaseFactor(d, 1);
            for (int r = 2; r <= rank; r++)
            {
                double step = BaseFactor(d, r - 1) - BaseFactor(d, r);
                if (step < 0) step = 0;
                f -= step * (1.0 + (Unit(r) * 2.0 - 1.0) * d.PowerJitter);
            }
            return f < 0 ? 0 : f;
        }

        /// <summary>순위의 더미 전투력(내 순위면 호출부가 실제 값을 쓴다).</summary>
        public static double Power(ArenaDummyData d, double myPower, int rank)
        {
            if (d == null) return myPower;
            if (rank == d.MeRank) return myPower;
            return Math.Round(myPower * Factor(d, rank));
        }

        /// <summary>순위의 더미 승점(🏆) — 1위 top 에서 순위마다 stepMin~stepMax 씩 단조 감소.</summary>
        public static double Score(ArenaDummyData d, int rank)
        {
            if (d == null) return 0;
            if (rank < 1) rank = 1;
            double v = d.ScoreTop;
            for (int r = 2; r <= rank; r++)
            {
                v -= d.ScoreStepMin + (d.ScoreStepMax - d.ScoreStepMin) * Unit(r);
                if (v <= d.ScoreMin) return d.ScoreMin;
            }
            return Math.Round(v < d.ScoreMin ? d.ScoreMin : v);
        }
    }
}
