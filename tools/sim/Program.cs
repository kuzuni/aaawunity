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
            bool blockTable = Environment.GetEnvironmentVariable("BLOCK_TABLE") == "1"; int maxPlus = 12; bool nGiven = false;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--seeds": seeds = new List<int>(); foreach (var s in args[++i].Split(',')) seeds.Add(int.Parse(s)); break;
                    case "--n": n = int.Parse(args[++i]); nGiven = true; break;
                    case "--mode": mode = args[++i]; break;
                    case "--chapter": oneChapter = int.Parse(args[++i]); break;
                    case "--rar": oneRar = int.Parse(args[++i]); break;
                    case "--plus": onePlus = int.Parse(args[++i]); break;
                    case "--slot": oneSlot = int.Parse(args[++i]); break;
                    case "--trace": trace = true; break;
                    case "--trace2": trace2 = int.Parse(args[++i]); break;
                    case "--block-table": blockTable = true; break;
                    case "--max-plus": maxPlus = int.Parse(args[++i]); break;
                }
            }
            var d = GameData.LoadFromDirectory(FindDataDir());
            Console.WriteLine($"data {d.Tune.Source} · chapters {d.Enemies.Chapters.Count} · perks {d.Perks.Perks.Count}");

            // 막힘 표는 판 수를 적게 쓴다(이분 탐색이 챕터마다 도므로) — 다만 «--n 을 줬는데 조용히 다른 수로 도는» 일은 없게 한다.
            if (blockTable) return BlockTable(d, seeds.Count > 0 ? seeds[0] : 11, nGiven ? n : 200, maxPlus);

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

        // ───────────────────────── «빌드 표 → 막히는 챕터» (T325 ⓑ 8항) ─────────────────────────

        /// <summary>«막힌다» 의 값 — 그 빌드로 그 챕터의 클리어율이 이만큼(%) 아래로 내려가는 첫 챕터다(주인 «예전에 챕터들 밸런스 맞췄던 식으로» = 실험1 기준 ≈10%).</summary>
        const double BlockPct = 10.0;
        /// <summary>과녁 셈(주인 2026-09-09 12:5X) — 노템 5 · 일반 풀 10 · 그 위 등급마다 +5 · 신화 위는 <b>+3강마다 +5</b>(갓 35 · 초월 40 · 불멸 45 · 무한 50 · … · 무한 +42 = 100).</summary>
        const int TargetNoGear = 5, TargetCommon = 10, TargetStep = 5, MythPlusStep = 3;

        /// <summary>과녁 챕터 — <b>표에서 낸다</b>(등급 수·rarMyth 가 바뀌면 저절로 따라온다 · 인덱스 리터럴 0).</summary>
        static int TargetChapter(GameData d, int rar, int plus)
        {
            if (rar < 0) return TargetNoGear;
            int baseAt = TargetCommon + TargetStep * rar;
            if (plus <= 0) return baseAt;
            return baseAt + TargetStep * (plus / MythPlusStep);
        }

        /// <summary>재는 빌드 목록 — 전부 <b>노강·슬롯 0</b>(주인 «풀» 의 뜻 · 옛 <see cref="Targets"/> 는 슬롯이 섞여 있어 이 표와 다르다).</summary>
        static List<(string id, int rar, int plus)> BlockBuilds(GameData d, int maxPlus)
        {
            var list = new List<(string, int, int)> { ("노템", -1, 0) };
            for (int r = 0; r < d.Gear.RarName.Length; r++) list.Add(($"{d.Gear.RarName[r]} 풀", r, 0));
            for (int p = MythPlusStep; p <= maxPlus; p += MythPlusStep)
            {
                string nm = GearTierName(d, p);
                list.Add(($"{(nm ?? d.Gear.RarName[d.Gear.RarMyth] + " +" + p)} 풀", d.Gear.RarMyth, p));
            }
            return list;
        }

        /// <summary>신화 위 «표시 등급» 이름(갓·초월·…) — 표(<c>gearTier.json</c>)는 Bootstrap 이 싣는 것이라 하니스엔 없다. 없으면 null 이고 «신화 +N» 으로 적는다.</summary>
        static string GearTierName(GameData d, int plus)
        {
            if (d.GearTier == null) return null;
            var s = GearTier.Of(d.GearTier, d.Gear.RarMyth, plus, d.Gear.RarMyth, d.Gear.RarName[d.Gear.RarMyth], "");
            return s.IsTier ? s.Name : null;
        }

        /// <summary>그 빌드로 그 챕터를 <paramref name="n"/> 판 돌아 클리어율(%).</summary>
        static double ClearPct(GameData d, int rar, int plus, int chapter, int seed, int n)
        {
            // ⚠ 판마다 «그 (빌드, 챕터) 만의» 새 스트림을 쓴다 — 이분 탐색은 데이터에 따라 챕터를 다른 차례로 들르므로,
            //   사다리 모드처럼 스트림 하나를 이어 쓰면 **같은 칸이 탐색 경로에 따라 다른 값**을 낸다(되풀이가 안 된다).
            //   사다리 모드가 스트림을 잇는 것은 sim.js 와 수를 맞추려는 계약이고, 이 모드는 그 계약 밖이다.
            var rng = new Mulberry32((uint)(seed * 1000003 + chapter * 1009 + (rar + 1) * 101 + plus));
            var b = GearSystem.MkBuild(d, rar, plus, 0);
            int w = 0;
            for (int i = 0; i < n; i++) if (new BattleState(d, chapter, b, rng, new SimPolicy(), LadderOpts(false)).RunToEnd().Clear) w++;
            return 100.0 * w / n;
        }

        /// <summary>
        /// 8항이 시킨 자 — <b>«빌드 표 → 막히는 챕터»</b> 를 한 번에 찍는다(<c>BLOCK_TABLE=1</c> 또는 <c>--block-table</c>).
        /// <para>클리어율은 챕터가 오를수록 내려가므로 <b>이분 탐색</b>으로 «처음으로 10% 아래인 챕터» 를 찾는다 —
        /// 100 챕터를 전부 도는 것보다 ~15배 싸고, 8항이 허락한 «과녁 ±2 만 돈다» 보다 정확하다.</para>
        /// <para>⚠ <b>이 자는 밸런스를 «맞추지» 않는다 — 재기만 한다.</b> 곡선(<c>tuneOverride.json</c>)을 고치는 것은 사람이고,
        /// 이 표는 그 사람이 «지금 어디에 있나» 를 보는 계기다. 값을 여기서 자동으로 고치면 주인이 준 과녁이 아니라
        /// 이 자가 정한 과녁으로 게임이 맞춰진다.</para>
        /// </summary>
        static int BlockTable(GameData d, int seed, int n, int maxPlus)
        {
            int maxCh = Math.Min(d.Tune.MaxChapter, d.Enemies.Chapters.Count);
            var builds = BlockBuilds(d, maxPlus);
            Console.WriteLine($"\n=== 막히는 챕터 표 · 시드 {seed} · 각 {n}판 · «막힘» = 클리어율 < {BlockPct:F0}% · 챕터 1~{maxCh} ===");
            Console.WriteLine("| 빌드 | 과녁 | 실제 막힘 | 그 챕터 % | 과녁에서 % |");
            Console.WriteLine("|---|---|---|---|---|");
            foreach (var (id, rar, plus) in builds)
            {
                int want = Math.Min(TargetChapter(d, rar, plus), maxCh);
                // 이분 탐색 — «lo 는 아직 뚫린다 · hi 는 막힌다» 를 지키며 좁힌다.
                int lo = 1, hi = maxCh;
                if (ClearPct(d, rar, plus, hi, seed, n) >= BlockPct) { lo = hi; }        // 끝까지 안 막힌다
                else
                {
                    if (ClearPct(d, rar, plus, lo, seed, n) < BlockPct) hi = lo;         // 1챕터부터 막힌다
                    else while (hi - lo > 1) { int mid = (lo + hi) / 2; if (ClearPct(d, rar, plus, mid, seed, n) >= BlockPct) lo = mid; else hi = mid; }
                }
                int block = hi;
                string blockTxt = lo == maxCh ? $"> {maxCh}" : block.ToString();
                Console.WriteLine($"| {id} | {want} | {blockTxt} | {ClearPct(d, rar, plus, Math.Min(block, maxCh), seed, n):F1}% | {ClearPct(d, rar, plus, want, seed, n):F1}% |");
            }
            Console.WriteLine("(과녁 = 주인이 준 표에서 낸 값 · 실제 = 지금 이 레포의 수치로 잰 값 · 둘이 벌어진 만큼이 tuneOverride 곡선이 메울 몫이다)");
            return 0;
        }

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
