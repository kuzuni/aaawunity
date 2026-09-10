using System;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T325 <b>9항</b> — 주인이 준 «막히는 챕터» 표가 아직 서 있는가.
    /// <para>
    /// 주인 2026-09-09: «노템 풀 5챕터 막히고 · 일반 풀 10 · 희귀 15 · 영웅 20 · 전설 25 · 신화 30 · 다음 등급 35 …»
    /// (그리고 4항 ⓕ «+3강마다 +5챕터»). 그 표는 <see cref="BalanceLadder.TargetChapter"/> 가 <b>표에서</b> 낸다 —
    /// 등급 수가 바뀌면 과녁도 저절로 따라오므로 여기에 챕터 수를 손으로 적지 않는다.
    /// </para>
    /// <para>
    /// ⚑ <b>이 자가 없으면 밸런스는 아무도 안 지킨다.</b> <c>tools/sim --block-table</c> 은 <b>사람이 볼 때만</b> 도는 자라,
    /// 누가 <c>enemiesOverride.json</c> 의 곡선이나 <c>gearOverride.json</c> 의 기여를 건드려도 CI 는 조용하다.
    /// 그 둘이 쓰는 셈은 <see cref="BalanceLadder"/> 하나로 모아 뒀다 — 표를 보는 눈과 지키는 자가 같은 수를 봐야 한다.
    /// </para>
    /// <para>
    /// ⚠ <b>이 자는 «클리어율이 정확히 10%» 를 요구하지 않는다.</b> 판이 유한해서 실측은 과녁에서 3~27% 로 흩어진다
    /// (2026-09-10 실측 · 시드 11 · 각 100판 · 스물한 빌드). 좁게 조이면 곡선을 손볼 때마다 애먼 빨강이 뜨고,
    /// 그러면 다음 사람이 기댓값을 낮춰서 초록을 만든다(결정 930 이 값을 치른 그 손).
    /// 그래서 <b>«벽이 과녁 언저리에 서 있는가»</b> 만 잰다 — 과녁에서는 반도 못 넘고, 다섯 챕터 아래에서는 넘어간다.
    /// 이 자가 빨개지는 뜻은 «10%가 아니다» 가 아니라 <b>«그 빌드의 벽이 주인이 말한 자리에서 사라졌다»</b> 이다.
    /// </para>
    /// </summary>
    public class BalanceLadderTests
    {
        const int Seed = 11;          // 사다리 골든이 쓰는 그 시드(T2)
        const int Runs = 100;         // 되풀이는 (빌드, 챕터) 로 씨를 내므로 흔들리지 않는다 — 판 수는 «흩어짐 폭» 만 정한다
        const int StepBelow = BalanceLadder.TargetStep;   // «한 칸 아래» = 과녁 사이 간격(주인 표가 +5 씩이다)

        const double HardAtTarget = 50.0;   // 과녁에서 이만큼을 넘으면 «막힌다» 가 아니다 (실측 최대 27%)
        const double EasyBelow = 15.0;      // 과녁 한 칸 아래에서 이만큼도 안 되면 «넘어간다» 가 아니다 (실측 최소 24%)

        static GameData Data() => GameData.LoadFromDirectory(TestData.Dir);

        /// <summary>
        /// 주인 표의 <b>모든 줄</b>에서 벽이 과녁 언저리에 서 있는가 — 노템부터 신화 +42(챕터 100)까지 스물한 빌드.
        /// </summary>
        [Test]
        public void EveryBuildHitsItsWallAroundTheChapterTheOwnerAskedFor()
        {
            var d = Data();
            int maxCh = Math.Min(d.Tune.MaxChapter, d.Enemies.Chapters.Count);
            var builds = BalanceLadder.Builds(d, BalanceLadder.MaxPlusForChapters(d, maxCh));
            Assert.That(builds.Count, Is.GreaterThan(d.Gear.RarName.Length),
                "빌드 목록이 등급 수보다 많아야 한다 — 신화 위 강화 단계가 안 들어왔다면 과녁 표의 절반을 안 재는 것이다");

            foreach (var (id, rar, plus) in builds)
            {
                int at = Math.Min(BalanceLadder.TargetChapter(d, rar, plus), maxCh);
                double here = BalanceLadder.ClearPct(d, rar, plus, at, Seed, Runs);
                Assert.That(here, Is.LessThan(HardAtTarget),
                    $"«{id}» 가 과녁 {at}챕터를 {here:F1}% 로 넘는다 — 주인 표에서 그 자리는 막히는 자리다");

                int below = Math.Max(1, at - StepBelow);
                if (below == at) continue;                       // 과녁이 1챕터면 «한 칸 아래» 가 없다
                double easier = BalanceLadder.ClearPct(d, rar, plus, below, Seed, Runs);
                Assert.That(easier, Is.GreaterThanOrEqualTo(EasyBelow),
                    $"«{id}» 가 과녁 한 칸 아래 {below}챕터조차 {easier:F1}% 다 — 벽이 주인이 말한 자리보다 앞에 섰다");
            }
        }

        /// <summary>
        /// 과녁 표 자체가 주인이 준 말과 같은가 — <b>수를 재지 않고 셈만</b> 잰다(빠르다 · 위 자가 느려 [Explicit] 로 가더라도 이 줄은 남는다).
        /// </summary>
        [Test]
        public void TheTargetTableIsTheOneTheOwnerDictated()
        {
            var d = Data();
            Assert.That(BalanceLadder.TargetChapter(d, -1, 0), Is.EqualTo(5), "노템 풀 5챕터(주인)");
            Assert.That(BalanceLadder.TargetChapter(d, 0, 0), Is.EqualTo(10), "일반 풀 10");
            // 그 위 등급은 «표에서» 낸다 — 영웅이 끼면 20 이 영웅이고 전설이 25 로 밀린다.
            for (int r = 1; r < d.Gear.RarName.Length; r++)
                Assert.That(BalanceLadder.TargetChapter(d, r, 0), Is.EqualTo(10 + 5 * r),
                    $"«{d.Gear.RarName[r]} 풀» 은 등급마다 +5 다");
            // 신화 위는 «+3강마다 +5챕터»(4항 ⓕ)
            int myth = BalanceLadder.TargetChapter(d, d.Gear.RarMyth, 0);
            Assert.That(BalanceLadder.TargetChapter(d, d.Gear.RarMyth, 3), Is.EqualTo(myth + 5), "신화 +3 = 신화 +5챕터");
            Assert.That(BalanceLadder.TargetChapter(d, d.Gear.RarMyth, 12), Is.EqualTo(myth + 20), "신화 +12 = 신화 +20챕터");
            // 주인 «챕터 수는 100» — 그 끝이 사다리의 마지막 줄과 맞는가
            Assert.That(BalanceLadder.TargetChapter(d, d.Gear.RarMyth, BalanceLadder.MaxPlusForChapters(d, d.Tune.MaxChapter)),
                        Is.EqualTo(d.Tune.MaxChapter), "사다리의 마지막 줄이 마지막 챕터에 닿는다");
        }
    }
}
