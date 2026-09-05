using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 챕터 레이아웃. **정본은 enemies.json 의 chapters[]** (sim.js `chapterLayout(c)`·`enemyStats` 를 뽑은 것) 이고,
    /// 여기의 <see cref="Generate"/> 는 같은 알고리즘의 이식본으로 «JSON 과 한 챕터도 어긋나지 않는가» 를
    /// EditMode 테스트가 대조하는 데 쓴다 (mulberry32 이식·시드 소비 순서 검증). 게임은 JSON 을 읽는다.
    /// </summary>
    public static class ChapterLayout
    {
        public static int EnemyCount(EnemiesData E, int c)
            => c < E.CurveFrom ? E.CurveEarly : Math.Min(E.CurveCap, E.CurveEarly + (c - (E.CurveFrom - 1)));

        public static int[] WaveSizes(EnemiesData E, int c)
        {
            int n = EnemyCount(E, c) - 1, b = n / E.Waves, r = n % E.Waves;
            var out_ = new int[E.Waves];
            for (int i = 0; i < E.Waves; i++) out_[i] = b + (i < r ? 1 : 0);
            return out_;
        }

        public static int RangedPool(EnemiesData E, int c) => EnemyCount(E, c) - 1 - E.Waves;
        public static int RangedBase(EnemiesData E, int c) => (int)RngUtil.JsRound(E.RangedRate * RangedPool(E, c));
        public static int RangedCount(EnemiesData E, int c, int jit)
        {
            if (c <= E.RangedZeroUntil) return 0;
            int ramp = c - E.RangedZeroUntil, B = RangedBase(E, c);
            return ramp <= B ? ramp : Math.Max(0, B + jit);
        }

        /// <summary>sim.js `chapterLayout(c)` 그대로 — 시드 소비 순서: 이벤트 셔플 → 흔들림 j → 원거리 자리.</summary>
        public static List<NodeData> Generate(EnemiesData E, int c)
        {
            var rnd = Mulberry32.FromJsNumber((double)c * 1013904223 + 77);
            var sizes = WaveSizes(E, c);
            var evs = new List<NodeType> { NodeType.Devil, NodeType.Angel };
            for (int i = 0; i < E.Rests; i++) evs.Add(NodeType.Rest);
            for (int i = evs.Count - 1; i > 0; i--)
            {
                int j = (int)Math.Floor(rnd.Next() * (i + 1));
                var t = evs[i]; evs[i] = evs[j]; evs[j] = t;
            }
            var out_ = new List<NodeData>();
            for (int i = 0; i < E.Waves; i++)
            {
                out_.Add(new NodeData { Type = NodeType.Wave, Size = sizes[i], Ranged = new bool[sizes[i]] });
                if (i < evs.Count) out_.Add(new NodeData { Type = evs[i] });
            }
            out_.Add(new NodeData { Type = NodeType.Boss });
            int jit = (int)Math.Floor(rnd.Next() * (2 * E.RangedJitter + 1)) - E.RangedJitter;
            int want = RangedCount(E, c, jit);
            var pool = new List<int[]>();
            for (int i = 0; i < out_.Count; i++)
                if (out_[i].Type == NodeType.Wave) for (int j = 1; j < out_[i].Size; j++) pool.Add(new[] { i, j });
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int k = (int)Math.Floor(rnd.Next() * (i + 1));
                var t = pool[i]; pool[i] = pool[k]; pool[k] = t;
            }
            for (int q = 0; q < want && q < pool.Count; q++) out_[pool[q][0]].Ranged[pool[q][1]] = true;
            return out_;
        }

        /// <summary>sim.js `enemyStats(c,w)` 이식 (JSON 대조용). 결과는 JS Math.round 와 같은 반올림.</summary>
        public static void EnemyStats(TuneData T, int c, int w, out double hp, out double dmg)
        {
            hp = T.EBaseHp * SegGrow(T.EHpSeg, c) * (1 + T.WaveHp * w);
            dmg = T.EBaseDmg * SegGrow(T.EDmgSeg, c) * (1 + T.WaveDmg * w);
            if (c >= 10) { hp *= T.WallHp; dmg *= T.WallDmg; }
            if (c >= 15) { hp *= T.Wall2Hp; dmg *= T.Wall2Dmg; }
            if (c >= 90) { hp *= T.Wall3Hp; dmg *= T.Wall3Dmg; }
            if (c >= T.Wall4At) { hp *= T.Wall4Hp; dmg *= T.Wall4Dmg; }
            hp = RngUtil.JsRound(hp); dmg = RngUtil.JsRound(dmg);
        }

        static double SegRate(double[][] seg, int c) { double r = seg[0][1]; foreach (var s in seg) if (c >= s[0]) r = s[1]; return r; }
        static double SegGrow(double[][] seg, int c) { double v = 1; for (int k = 1; k < c; k++) v *= SegRate(seg, k); return v; }
    }
}
