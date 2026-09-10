using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// <b>«어느 빌드가 몇 챕터에서 막히는가» 를 재는 자</b> (T325 7·8·9항 · 주인 2026-09-09 «노템 풀 5챕터 막히고 · 일반 풀 10 · 희귀 15 …»).
    /// <para>
    /// ⚠ <b>게임은 이 파일을 안 부른다</b> — 재기만 하는 자다(<c>ChapterLayout.EnemyStats</c> 가 «JSON 대조용» 인 것과 같은 자리).
    /// 여기 있는 까닭은 <b>두 곳이 같은 셈을 써야</b> 하기 때문이다: <c>tools/sim --block-table</c>(사람이 표를 보는 곳)과
    /// <c>BalanceLadderTests</c>(CI 가 «주인 과녁이 아직 맞나» 를 지키는 곳). 셈을 두 벌 두면 어느 날 둘이 다른 말을 하고,
    /// 그때 «어느 쪽이 맞나» 를 가릴 방법이 없다.
    /// </para>
    /// <para>
    /// ⚑ <b>이 자는 과녁을 «정하지» 않는다</b> — 과녁은 주인이 준 표에서 오고(<see cref="TargetChapter"/>), 등급 수가 바뀌면 저절로 따라온다.
    /// 밸런스를 맞추는 것은 사람(<c>enemiesOverride.json</c> 의 곡선)이고 이 자는 «지금 어디에 있나» 만 답한다.
    /// </para>
    /// </summary>
    public static class BalanceLadder
    {
        /// <summary>«막힌다» 의 값 — 그 빌드로 그 챕터의 클리어율이 이만큼(%) 아래로 내려가면 막힌 것이다(주인 «예전에 챕터들 밸런스 맞췄던 식으로» = sim.js 실험1 기준 ≈10%).</summary>
        public const double BlockPct = 10.0;

        /// <summary>과녁 셈(주인 2026-09-09 12:5X) — 노템 5 · 일반 풀 10 · 그 위 등급마다 +5 · 신화 위는 <b>«한 칸» 마다 +5</b>(갓 35 · 초월 40 · 불멸 45 · 무한 50 · …).</summary>
        public const int TargetNoGear = 5, TargetCommon = 10, TargetStep = 5;

        /// <summary>주인이 «+3강마다 +5챕터» 를 말한 그 시절의 강화 배율(<c>gear.json enhance.plusStep</c> = 19/9)과 그 «한 칸».</summary>
        /// <remarks>
        /// ⚠ <b>지금 표를 따라가면 안 되는 수</b>다 — 이것은 «주인이 그 말을 했을 때 서 있던 자리» 라 <b>굳어 있어야</b> 한다.
        /// 살아 있는 값(<c>d.Gear.PlusStep</c>)은 <see cref="MythPlusStepFor"/> 가 읽는다.
        /// </remarks>
        public const double OwnerPlusStep = 19.0 / 9.0;
        /// <summary><see cref="OwnerPlusStep"/> 참조 — 주인이 말한 «+3강».</summary>
        public const int OwnerMythPlusStep = 3;

        /// <summary>
        /// 신화 위 사다리의 <b>«한 칸» 이 강화 몇 단계인가</b> — 주인 «+3강마다 +5챕터»(T325 4항 ⓕ)를 <b>지금 배율에서 같은 뜻으로</b> 읽는다.
        /// <para>힘은 <c>1 + plusStep × plus</c> 로 <b>plus 에 대해 선형</b>이라, 배율이 <c>k</c> 배 줄면 <b>같은 힘을 얻는 데 강화가 <c>k</c> 배 든다</b> —
        /// 곧 <c>plus</c> 를 <c>k</c> 배로 늘리면 사다리 <b>스무</b> 줄이 <b>하나도 안 움직인다</b>(적 곡선·장비 기여를 한 자도 안 건드리고).</para>
        /// <para>T405(주인 2026-09-10 «그 전설 2강보다 신화 0강이 세야 함»)가 배율을 2.111 → 0.1 로 줄이며 열었다 —
        /// 그 제약은 <c>1 + 2·plusStep &lt; 1.25</c>(전설 120 → 신화 150) 이라 배율을 줄이는 것 말고 길이 없고,
        /// 줄이면 «+3강» 이 ×7.33 에서 ×1.3 이 되어 <b>주인이 준 다른 말(«챕터 수는 100»)이 같이 죽는다</b>. 이 한 칸이 그 둘을 같이 살린다.</para>
        /// </summary>
        public static int MythPlusStepFor(GameData d)
        {
            double p = d != null && d.Gear != null ? d.Gear.PlusStep : OwnerPlusStep;
            if (p <= 0) return OwnerMythPlusStep;
            return Math.Max(1, (int)Math.Round(OwnerMythPlusStep * OwnerPlusStep / p));
        }

        /// <summary>과녁 챕터 — <b>표에서 낸다</b>(등급 수·<c>rarMyth</c> 가 바뀌면 저절로 따라온다 · 인덱스 리터럴 0).</summary>
        public static int TargetChapter(GameData d, int rar, int plus)
        {
            if (rar < 0) return TargetNoGear;
            int baseAt = TargetCommon + TargetStep * rar;
            if (plus <= 0) return baseAt;
            return baseAt + TargetStep * (plus / MythPlusStepFor(d));
        }

        /// <summary>재는 빌드 목록 — 전부 <b>노강·슬롯 0</b>(주인 «풀» 의 뜻). 이름은 표에서 낸다.</summary>
        public static List<(string id, int rar, int plus)> Builds(GameData d, int maxPlus)
        {
            var list = new List<(string, int, int)> { ("노템", -1, 0) };
            for (int r = 0; r < d.Gear.RarName.Length; r++) list.Add(($"{d.Gear.RarName[r]} 풀", r, 0));
            int step = MythPlusStepFor(d);
            for (int p = step; p <= maxPlus; p += step)
            {
                string nm = TierName(d, p);
                list.Add(($"{(nm ?? d.Gear.RarName[d.Gear.RarMyth] + " +" + p)} 풀", d.Gear.RarMyth, p));
            }
            return list;
        }

        /// <summary>신화 위 «표시 등급» 이름(갓·초월·…) — 표(<c>gearTier.json</c>)가 안 실린 하니스에서는 null 이고 그때는 «신화 +N» 으로 적는다.</summary>
        public static string TierName(GameData d, int plus)
        {
            if (d.GearTier == null) return null;
            var s = GearTier.Of(d.GearTier, d.Gear.RarMyth, plus, d.Gear.RarMyth, d.Gear.RarName[d.Gear.RarMyth], "");
            return s.IsTier ? s.Name : null;
        }

        /// <summary>sim.js <c>LADDER_OPTS</c> = <c>{perkMode:'base10', baseStats:'legacy20', gearOpts:false}</c> — 주인 «특전 선택 기준은 전에 밸런스 테스트 했을 때처럼»(T325 0항).</summary>
        public static RunOptions LadderOpts() => new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false };

        /// <summary>그 빌드로 그 챕터를 <paramref name="n"/> 판 돌아 클리어율(%).</summary>
        /// <remarks>
        /// ⚠ 판마다 «그 (빌드, 챕터) 만의» 새 스트림을 쓴다 — 이분 탐색은 데이터에 따라 챕터를 다른 차례로 들르므로,
        /// 사다리 모드처럼 스트림 하나를 이어 쓰면 <b>같은 칸이 탐색 경로에 따라 다른 값</b>을 낸다(되풀이가 안 된다).
        /// 사다리 모드가 스트림을 잇는 것은 sim.js 와 수를 맞추려는 계약이고, 이 모드는 그 계약 밖이다.
        /// </remarks>
        public static double ClearPct(GameData d, int rar, int plus, int chapter, int seed, int n)
        {
            var rng = new Mulberry32((uint)(seed * 1000003 + chapter * 1009 + (rar + 1) * 101 + plus));
            var b = GearSystem.MkBuild(d, rar, plus, 0);
            int w = 0;
            for (int i = 0; i < n; i++) if (new BattleState(d, chapter, b, rng, new SimPolicy(), LadderOpts()).RunToEnd().Clear) w++;
            return 100.0 * w / n;
        }

        /// <summary>그 빌드가 <b>처음으로 막히는</b> 챕터 — 클리어율은 챕터가 오를수록 내려가므로 이분 탐색으로 찾는다(1~<paramref name="maxCh"/> · 끝까지 안 막히면 <paramref name="maxCh"/>+1).</summary>
        public static int BlockChapter(GameData d, int rar, int plus, int seed, int n, int maxCh)
        {
            int lo = 1, hi = maxCh;
            if (ClearPct(d, rar, plus, hi, seed, n) >= BlockPct) return maxCh + 1;    // 끝까지 안 막힌다
            if (ClearPct(d, rar, plus, lo, seed, n) < BlockPct) return lo;            // 1챕터부터 막힌다
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (ClearPct(d, rar, plus, mid, seed, n) >= BlockPct) lo = mid; else hi = mid;
            }
            return hi;
        }

        /// <summary>과녁이 <paramref name="maxCh"/> 안에 드는 가장 높은 신화 강화 단계(<see cref="Builds"/> 의 끝을 정한다).</summary>
        public static int MaxPlusForChapters(GameData d, int maxCh)
        {
            int baseAt = TargetCommon + TargetStep * d.Gear.RarMyth, p = 0, step = MythPlusStepFor(d);
            while (baseAt + TargetStep * ((p + step) / step) <= maxCh) p += step;
            return p;
        }
    }
}
