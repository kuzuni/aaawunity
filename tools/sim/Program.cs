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
            bool blockTable = Environment.GetEnvironmentVariable("BLOCK_TABLE") == "1"; int maxPlus = 12; bool nGiven = false; bool fitCurve = false;
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
                    case "--fit-curve": fitCurve = true; break;
                }
            }
            var d = GameData.LoadFromDirectory(FindDataDir());
            Console.WriteLine($"data {d.Tune.Source} · chapters {d.Enemies.Chapters.Count} · perks {d.Perks.Perks.Count}");

            // 막힘 표는 판 수를 적게 쓴다(이분 탐색이 챕터마다 도므로) — 다만 «--n 을 줬는데 조용히 다른 수로 도는» 일은 없게 한다.
            if (fitCurve) return FitCurve(d, seeds.Count > 0 ? seeds[0] : 11, nGiven ? n : 150);
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

        // ───────────────────── «적 곡선 맞추기» (T325 ⓑ 8항) ─────────────────────

        /// <summary>
        /// 8항의 절차를 그대로 도는 자 — <b>구간 경계를 새 과녁</b>(0·5·10·…)에 두고 <b>앞 구간부터 차례로</b> 그 구간의 배율 하나를 찾는다.
        /// <para>구간 <c>i</c> 가 끝나는 챕터가 곧 빌드 <c>i</c> 의 과녁이므로, «그 빌드가 그 챕터에서 딱 10%» 가 되는 배율을 이분 탐색으로 찾으면 된다
        /// (막히는 챕터를 다시 찾을 필요가 없다 — 과녁 한 칸만 재면 된다).</para>
        /// <para>⚠ <b>이 자는 과녁을 정하지 않는다</b> — 과녁은 주인이 준 표에서 오고(<see cref="TargetChapter"/>), 이 자가 움직이는 것은
        /// 주인이 «챕터 밸런스도 다시» 로 **열어 준 것**(적 쪽 곡선) 하나뿐이다. 플레이어·장비·특전·강화 배율은 손대지 않는다(§1).</para>
        /// <para>⚠ <b>파일을 안 쓴다.</b> 찾은 곡선을 <c>tuneOverride.json</c> 꼴로 화면에 찍기만 하고, 그것을 레포에 넣는 것은 사람이 본 뒤에 한다
        /// (결정 969 ⑦ 과 같은 자리 — 자가 곧바로 게임을 고치면 아무도 그 수를 읽지 않는다).</para>
        /// <para>⚑ <b>움직이는 것은 <c>enemiesOverride</c>(적 표 배수)이지 <c>tuneOverride</c> 의 곡선이 아니다(결정 985).</b>
        /// <c>Tune.EBaseHp</c>·<c>EHpSeg</c> 를 읽는 곳은 <c>ChapterLayout.EnemyStats</c> 하나이고 그것을 부르는 것은 <c>LayoutTests</c>(«JSON 대조용») 뿐이라
        /// 진짜 전투에 안 닿는다 — 실측으로 못 박았다(기저 1/60000 · 성장 1.0 에서 노템·3챕터 10.5% → 10.5%).
        /// 그래서 이 자는 <c>enemies.json</c> 에 <b>구워진</b> 값 위에 곱하는 <b>배수</b>를 찾는다. 배수 1 = aaaw 그대로.</para>
        /// <para>⚠ 그래서 배율 범위가 <b>1 아래로도</b> 열려 있다 — «aaaw 보다 약하게» 도 답이 될 수 있다(노템은 지금 과녁보다 <b>일찍</b> 막힌다).
        /// 그리고 <b>1챕터는 늘 1배</b>다(누적이 아직 없다) — 1챕터를 이 손잡이로는 못 바꾼다.</para>
        /// <para>⚠ <b>hp·dmg 에 같은 배율을 준다</b> — 8항이 «구간마다 배율 하나씩» 이라 했고, 정본도 두 값이 거의 같다(1.0292 ↔ 1.0265 …).
        /// 둘을 따로 찾으면 자유도가 둘인데 과녁은 하나라 답이 안 정해진다.</para>
        /// </summary>
        /// <summary>
        /// 구간 배율을 찾는 범위. 아래는 «안 자란다»(1.0).
        /// <para>⚠ 위 끝은 처음에 <b>1.60</b>(정본에서 제일 가파른 구간 1.127 의 갑절 남짓)으로 뒀다가 <b>실측하고 넓혔다</b> —
        /// 주인의 과녁 표(등급마다 5챕터 · +3강마다 5챕터)는 <b>정본 aaaw 곡선보다 훨씬 가파른 성장</b>을 요구해서
        /// 1.60 에서는 열아홉 구간이 전부 위 끝에 붙었다. «정본이 이만하니 이 언저리겠지» 는 잰 값이 아니었다.</para>
        /// </summary>
        const double RateLo = 0.50, RateHi = 4.0;


        static int FitCurve(GameData d, int seed, int n)
        {
            int maxCh = Math.Min(d.Tune.MaxChapter, d.Enemies.Chapters.Count);
            var builds = new List<(string id, int rar, int plus, int at)>();
            foreach (var (id, rar, plus) in BlockBuilds(d, MaxPlusForChapters(d, maxCh)))
            {
                int at = TargetChapter(d, rar, plus);
                if (at <= maxCh) builds.Add((id, rar, plus, at));
            }
            builds.Sort((a, b) => a.at.CompareTo(b.at));

            Console.WriteLine($"\n=== 적 곡선 맞추기 · 시드 {seed} · 각 {n}판 · 과녁마다 클리어율 {BlockPct:F0}% · 챕터 1~{maxCh} ===");
            Console.WriteLine("| 구간 | 그 구간이 맞추는 빌드 | 과녁 | 찾은 배율 | 그 배율에서 % |");
            Console.WriteLine("|---|---|---|---|---|");

            // 정본 적 수치를 먼저 떠 둔다 — 배수는 늘 이 사본에서 다시 계산한다(제자리 곱셈은 거듭하면 제곱된다).
            SnapCanon(d);
            var seg = new List<double[]>();
            int from = 0;
            foreach (var b in builds)
            {
                seg.Add(new double[] { from, 1.0 });                       // 자리부터 만들고 아래에서 값을 넣는다
                int idx = seg.Count - 1;
                // 이분 탐색 — 배율이 클수록 적이 세지므로 클리어율은 내려간다(단조).
                double lo = RateLo, hi = RateHi, got = 0;
                for (int step = 0; step < 9; step++)
                {
                    double mid = (lo + hi) / 2;
                    seg[idx][1] = mid;
                    ApplySeg(d, seg);
                    got = ClearPct(d, b.rar, b.plus, b.at, seed, n);
                    if (got > BlockPct) lo = mid; else hi = mid;           // 너무 쉬우면 더 세게
                }
                seg[idx][1] = (lo + hi) / 2;
                ApplySeg(d, seg);
                got = ClearPct(d, b.rar, b.plus, b.at, seed, n);
                // 위·아래 끝에 붙으면 «찾은 값» 이 아니라 «범위가 모자라다» 는 뜻이다 — 조용히 그럴듯한 수를 적지 않는다.
                string note = seg[idx][1] > RateHi - 1e-3 ? " ⚠ 위 끝(더 세게 못 간다)" : seg[idx][1] < RateLo + 1e-3 ? " ⚠ 아래 끝(더 약하게 못 간다)" : "";
                Console.WriteLine($"| {from}~{b.at} | {b.id} | {b.at} | {seg[idx][1]:F6}{note} | {got:F1}% |");
                from = b.at;
            }

            Console.WriteLine("\n찾은 곡선 — enemiesOverride.json 에 넣을 꼴(사람이 보고 넣는다 · 이 자는 파일을 안 쓴다):");
            Console.WriteLine("  \"hpSeg\": [" + SegJson(seg) + "],");
            Console.WriteLine("  \"dmgSeg\": [" + SegJson(seg) + "]");
            Console.WriteLine("  (그리고 tuneOverride.json 에 \"maxChapter\": " + maxCh + " — 챕터 수는 tune 쪽 칸이고 그쪽은 실제로 닿는다)");
            Console.WriteLine("⚠ 넣기 전에 `--block-table` 로 한 번 더 재라 — 이 자는 과녁 «한 칸» 만 봤고, 막히는 챕터는 그 옆 칸에서 정해질 수도 있다.");
            return 0;
        }

        /// <summary>과녁이 <paramref name="maxCh"/> 를 넘지 않는 마지막 신화 강화 단계 — 표에서 낸다(«+3강마다 +5챕터» · 주인).</summary>
        static int MaxPlusForChapters(GameData d, int maxCh)
        {
            int baseAt = TargetCommon + TargetStep * d.Gear.RarMyth, p = 0;
            while (baseAt + TargetStep * ((p + MythPlusStep) / MythPlusStep) <= maxCh) p += MythPlusStep;
            return p;
        }

        /// <summary>정본 적 수치의 사본 — 배수는 <b>늘 이 사본에서</b> 다시 계산한다(제자리에서 거듭 곱하면 배수가 제곱된다).</summary>
        static double[][] _canonHp, _canonDmg; static double[] _canonBossHp, _canonBossDmg;

        static void SnapCanon(GameData d)
        {
            int n = d.Enemies.Chapters.Count;
            _canonHp = new double[n][]; _canonDmg = new double[n][];
            _canonBossHp = new double[n]; _canonBossDmg = new double[n];
            for (int i = 0; i < n; i++)
            {
                var ch = d.Enemies.Chapters[i];
                _canonHp[i] = new double[ch.Waves.Count]; _canonDmg[i] = new double[ch.Waves.Count];
                for (int w = 0; w < ch.Waves.Count; w++) { _canonHp[i][w] = ch.Waves[w].Hp; _canonDmg[i][w] = ch.Waves[w].Dmg; }
                if (ch.Boss != null) { _canonBossHp[i] = ch.Boss.Hp; _canonBossDmg[i] = ch.Boss.Dmg; }
            }
        }

        /// <summary>
        /// 찾는 중인 구간 표를 <b>적 표</b>(전투가 실제로 읽는 곳)에 배수로 먹인다 — <c>GameData.ApplyEnemiesOverride</c> 와 같은 셈이다.
        /// 정본 사본에서 다시 계산하므로 몇 번을 불러도 배수가 안 쌓인다.
        /// </summary>
        static void ApplySeg(GameData d, List<double[]> seg)
        {
            var a = seg.ToArray();
            for (int i = 0; i < d.Enemies.Chapters.Count; i++)
            {
                var ch = d.Enemies.Chapters[i];
                double k = ChapterLayout.SegGrow(a, ch.C);
                for (int w = 0; w < ch.Waves.Count; w++) { ch.Waves[w].Hp = _canonHp[i][w] * k; ch.Waves[w].Dmg = _canonDmg[i][w] * k; }
                if (ch.Boss != null) { ch.Boss.Hp = _canonBossHp[i] * k; ch.Boss.Dmg = _canonBossDmg[i] * k; }
            }
        }

        static string SegJson(List<double[]> seg)
        {
            var parts = new List<string>();
            foreach (var r in seg) parts.Add($"[{r[0]:F0}, {r[1]:F6}]");
            return string.Join(", ", parts);
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
