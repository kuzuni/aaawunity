using System;
using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;

namespace KkomaKnight.Sim
{
    /// <summary>난수 호출 수를 세는 래퍼 (sim.js 트레이스와 소비 횟수를 맞춰 볼 때).</summary>
    sealed class CountingRng : IRng
    {
        readonly IRng _r; public long Count; public List<string> Log; public bool On;
        public CountingRng(IRng r) { _r = r; }
        public double Next()
        {
            Count++;
            if (On && Log != null)
            {
                var st = new System.Diagnostics.StackTrace(1, false);
                string nm = "?";
                for (int i = 0; i < st.FrameCount; i++) { var m = st.GetFrame(i).GetMethod(); if (m.DeclaringType != typeof(RngUtil)) { nm = m.Name; break; } else nm = m.Name; }
                Log.Add(nm);
            }
            return _r.Next();
        }
    }

    /// <summary>
    /// 이식 검증 하니스 — sim.js 실험1(난이도 사다리 7점)을 C# 엔진으로 재현한다.
    ///   dotnet run --project tools/dotnet/Sim -c Release -- [--seeds 11,12,13] [--n 1000] [--mode ladder|3pick|both]
    ///   dotnet run --project tools/dotnet/Sim -c Release -- --chapter C --rar R --plus P --slot S [--trace] [--trace2 RUN]
    /// sim.js 쪽 대조값: (aaaw)  SEED=11 EXP1_N=1000 node sim.js 1   /  SEED=11 EXP1_N=1000 EXP1_PERKMODE=3pick node sim.js 1
    /// 시드 스트림은 sim.js 와 같다: setSeed(s) = mulberry32(s) 하나를 과녁 7칸 × N판이 순서대로 이어 쓴다.
    /// </summary>
    public static class Program
    {
        // sim.js EXP1_TARGETS — (rar, plus, slot, 과녁 챕터). 빌드·챕터는 주인 확정값이고 여기서는 «자» 로만 쓴다.
        static readonly (string id, int rar, int plus, int slot, int at)[] Targets =
        {
            ("노템(장비0·슬롯0)", -1, 0, 0, 3), ("일반 풀셋(슬롯0)", 0, 0, 0, 7), ("희귀 풀셋·슬롯5", 1, 0, 5, 15),
            ("전설 풀셋·슬롯15", 2, 0, 15, 30), ("신화 풀셋·슬롯25", 3, 0, 25, 60), ("신화+9강 풀셋·슬롯50", 3, 9, 50, 100),
            ("신화+9강 풀셋·슬롯100", 3, 9, 100, 125),
        };

        public static int Main(string[] args)
        {
            var seeds = new List<int> { 11, 12, 13 }; int n = 1000; string mode = "both";
            int oneChapter = 0, oneRar = -1, onePlus = 0, oneSlot = 0; bool trace = false; int trace2 = -1;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--seeds": seeds = new List<int>(); foreach (var s in args[++i].Split(',')) seeds.Add(int.Parse(s)); break;
                    case "--n": n = int.Parse(args[++i]); break;
                    case "--mode": mode = args[++i]; break;
                    case "--chapter": oneChapter = int.Parse(args[++i]); break;
                    case "--rar": oneRar = int.Parse(args[++i]); break;
                    case "--plus": onePlus = int.Parse(args[++i]); break;
                    case "--slot": oneSlot = int.Parse(args[++i]); break;
                    case "--trace": trace = true; break;
                    case "--trace2": trace2 = int.Parse(args[++i]); break;
                }
            }
            var d = GameData.LoadFromDirectory(FindDataDir());
            Console.WriteLine($"data {d.Tune.Source} · chapters {d.Enemies.Chapters.Count} · perks {d.Perks.Perks.Count}");

            if (oneChapter > 0 && trace2 >= 0)
            {
                // sim.js /tmp/trace2.js 와 같은 형식의 이벤트 로그 — run 번호 trace2 의 처치·피격을 찍는다
                var crng = new CountingRng(new Mulberry32((uint)seeds[0]));
                var b = GearSystem.MkBuild(d, oneRar, onePlus, oneSlot);
                for (int i = 0; i <= trace2; i++)
                {
                    var opt = LadderOpts(mode == "3pick"); opt.EmitEvents = i == trace2;
                    crng.On = i == trace2; if (crng.On) crng.Log = new List<string>();
                    var G = new BattleState(d, oneChapter, b, crng, new SimPolicy(), opt);
                    while (!G.Over && G.AliveList().Count > 0)
                    {
                        G.Tick();
                        if (i == trace2)
                        {
                            foreach (var ev in G.Events)
                            {
                                if (ev.Kind == EvKind.Kill) Console.WriteLine($"kill t={G.T:F3} rng={crng.Count} kills={G.Kills} ex={ev.Enemy.WorldX} boss={ev.Enemy.IsBoss}");
                                else if (ev.Kind == EvKind.PlayerHit || ev.Kind == EvKind.PlayerEvade || ev.Kind == EvKind.Ward || ev.Kind == EvKind.Ignore) Console.WriteLine($"hitP t={G.T:F3} kind={ev.Kind} hp={G.P.Hp:F3} sh={G.P.Sh:F3}");
                            }
                            G.Events.Clear();
                        }
                    }
                    if (i == trace2) { Console.WriteLine($"END clear={G.Cleared} t={G.T:F3} rng={crng.Count}"); if (Environment.GetEnvironmentVariable("RNGLOG") != null) File.WriteAllLines(Environment.GetEnvironmentVariable("RNGLOG"), crng.Log); }
                }
                return 0;
            }

            if (oneChapter > 0)
            {
                foreach (var seed in seeds)
                {
                    var rng = new Mulberry32((uint)seed);
                    var b = GearSystem.MkBuild(d, oneRar, onePlus, oneSlot);
                    int w = 0;
                    for (int i = 0; i < n; i++)
                    {
                        var r = new BattleState(d, oneChapter, b, rng, new SimPolicy(), LadderOpts(mode == "3pick")).RunToEnd();
                        if (r.Clear) w++;
                        if (trace && (i < 5 || Environment.GetEnvironmentVariable("TRACEALL") != null)) Console.WriteLine($"  run {i}: clear={r.Clear} t={r.Time:F2} lv={r.Level} kills={r.Kills} gold={r.Gold:F0} tries={r.AtkTries} miss={r.Miss} taken={string.Join(",", r.Taken)}");
                    }
                    Console.WriteLine($"seed {seed} chapter {oneChapter} rar {oneRar} +{onePlus} slot {oneSlot}: {100.0 * w / n:F1}%");
                }
                return 0;
            }

            foreach (var m in mode == "both" ? new[] { "ladder", "3pick" } : new[] { mode })
            {
                Console.WriteLine($"\n=== 실험1 재현 · 모드 {m} · 각 {n}판 · 시드 {string.Join(",", seeds)} ===");
                Console.WriteLine("| 조건 | 챕터 | " + string.Join(" | ", seeds.ConvertAll(s => "seed " + s)) + " |");
                Console.WriteLine("|---|---|" + string.Concat(seeds.ConvertAll(s => "---|")));
                var rows = new string[Targets.Length];
                for (int t = 0; t < Targets.Length; t++) rows[t] = $"| {Targets[t].id} | {Targets[t].at} |";
                foreach (var seed in seeds)
                {
                    var rng = new Mulberry32((uint)seed);   // sim.js setSeed(seed): 과녁 전체가 한 스트림
                    for (int t = 0; t < Targets.Length; t++)
                    {
                        var T = Targets[t];
                        var b = GearSystem.MkBuild(d, T.rar, T.plus, T.slot);
                        int w = 0;
                        for (int i = 0; i < n; i++)
                            if (new BattleState(d, T.at, b, rng, new SimPolicy(), LadderOpts(m == "3pick")).RunToEnd().Clear) w++;
                        rows[t] += $" {100.0 * w / n:F1}% |";
                    }
                }
                foreach (var r in rows) Console.WriteLine(r);
            }
            return 0;
        }

        /// <summary>sim.js LADDER_OPTS = {perkMode:'base10', baseStats:'legacy20', gearOpts:false}; 3pick 은 perkMode 만 바꾼다(EXP1_PERKMODE).</summary>
        static RunOptions LadderOpts(bool threePick) => new RunOptions { LadderPerkMode = !threePick, BaseStatsLegacy20 = true, GearOpts = false };

        static string FindDataDir()
        {
            var env = Environment.GetEnvironmentVariable("KKOMA_DATA_DIR");
            if (!string.IsNullOrEmpty(env)) return env;
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                var cand = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "data");
                if (File.Exists(Path.Combine(cand, "tune.json"))) return cand;
            }
            throw new DirectoryNotFoundException("Assets/StreamingAssets/data 를 찾을 수 없다");
        }
    }
}
