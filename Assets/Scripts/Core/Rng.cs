using System;

namespace KkomaKnight.Core
{
    /// <summary>난수 공급원. 엔진은 이 인터페이스로만 굴린다 (JS 의 Math.random 자리).</summary>
    public interface IRng
    {
        /// <summary>[0,1) 의 double — JS Math.random() 과 같은 계약.</summary>
        double Next();
    }

    /// <summary>
    /// sim.js / index.html 의 mulberry32 이식. 같은 시드에서 **비트 단위로 같은 수열**을 낸다
    /// (챕터 레이아웃 시드·시드 하니스 SEED=11/12/13 재현에 쓴다).
    /// JS 원문: a|=0; a=a+0x6D2B79F5|0; t=imul(a^a>>>15,1|a); t=t+imul(t^t>>>7,61|t)^t; return (t^t>>>14)>>>0)/2^32
    /// </summary>
    public sealed class Mulberry32 : IRng
    {
        uint _a;
        public Mulberry32(uint seed) { _a = seed; }
        /// <summary>JS 의 `seed|0` (ToInt32) 를 그대로 흉내 — double 로 계산한 시드를 32비트로 접는다.</summary>
        public static Mulberry32 FromJsNumber(double seed) => new Mulberry32(ToUint32(seed));

        public static uint ToUint32(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return 0;
            double t = Math.Truncate(v);
            double m = t % 4294967296.0;
            if (m < 0) m += 4294967296.0;
            return (uint)m;
        }

        public double Next()
        {
            unchecked
            {
                _a += 0x6D2B79F5u;
                uint t = (_a ^ (_a >> 15)) * (1u | _a);
                t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }

    /// <summary>비시드 모드 — System.Random 을 감싼다 (게임 본편).</summary>
    public sealed class SystemRng : IRng
    {
        readonly Random _r;
        public SystemRng() { _r = new Random(); }
        public SystemRng(int seed) { _r = new Random(seed); }
        public double Next() => _r.NextDouble();
    }

    public static class RngUtil
    {
        /// <summary>sim.js `rand(a,b)` = a + random*(b-a)</summary>
        public static double Range(this IRng r, double a, double b) => a + r.Next() * (b - a);
        /// <summary>sim.js `pick(arr)` = arr[floor(random*len)]</summary>
        public static T Pick<T>(this IRng r, System.Collections.Generic.IList<T> a) => a[(int)Math.Floor(r.Next() * a.Count)];
        /// <summary>JS Math.round — .5 는 +∞ 쪽으로 (C# 기본 Round 는 은행가 반올림이라 쓰지 않는다).</summary>
        public static double JsRound(double v) => Math.Floor(v + 0.5);
    }
}
