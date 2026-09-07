using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T173 — 주인 2026-09-07 «창은 그냥 화면 넘어서 10 정도 더 가면 지워지게 / 8까지 관통이었는데 그냥 그런 거 제한 없애 주고
    /// · 걍 닿으면 다 데미지 주게로». 값은 <c>data/combat.json</c>(aaaw 정본 · 불변)이 아니라
    /// <b>이 레포 전용 덮어쓰기</b> <c>Assets/KkomaKnight/combatOverride.json</c> 에 있고, 엔진은 «pierce ≤ 0 = 상한 없음» 으로 읽는다.
    /// <para>
    /// 이 자가 지키는 것 셋 — ⓐ 덮어쓰기가 <b>헤드리스 경로에도</b> 먹는다(Sim 시드 골든·EditMode 가 게임과 같은 규칙으로 돌아야 한다) ·
    /// ⓑ 정본 파일은 손대지 않았다 · ⓒ 상한 규칙이 한 함수(<see cref="BattleState.PierceCap"/>)에 있다.
    /// </para>
    /// </summary>
    public class CombatOverrideTests
    {
        /// <summary>덮어쓰기 파일이 정한 값 — 바꾸려면 JSON 만 고친다(여기 숫자는 «그 파일과 코드가 같은 값을 본다» 는 확인용).</summary>
        const double SpearReach = 312;
        const int SpearPierce = 0;   // 0 = 무제한

        [Test]
        public void TheOverrideReachesTheHeadlessLoaderToo()
        {
            var D = TestData.Load();
            Assert.AreEqual(SpearReach, D.Combat.SpearReach, 1e-9,
                "창 사거리는 combatOverride.json 값이어야 한다 — Sim 시드 골든이 게임과 다른 규칙으로 돌면 두 표가 영영 안 맞는다");
            Assert.AreEqual(SpearPierce, D.Combat.PierceSpear, "창 관통 = 0(무제한)");
            // 창만이다 — 검기는 주인이 말하지 않았으므로 정본 값 그대로여야 한다
            Assert.AreEqual(2, D.Combat.PierceWave, "검기 관통은 정본 그대로(주인이 창만 말했다)");
            Assert.AreEqual(8, D.Combat.PierceWaveBig, "큰 검기 관통도 정본 그대로");
        }

        [Test]
        public void TheCanonicalCombatJsonIsUntouched()
        {
            // §1 절대 규칙 — data/*.json 은 aaaw 정본이라 손으로 고치지 않는다. 덮어쓰기지 «수정» 이 아님을 여기서 못 박는다.
            var raw = new JNode(MiniJson.Parse(File.ReadAllText(Path.Combine(TestData.Dir, "combat.json"))));
            Assert.AreEqual(352, raw["range"]["spearReach"].Num(), 1e-9, "정본 combat.json 의 spearReach 는 352 그대로여야 한다");
            Assert.AreEqual(8, raw["pierce"]["spear"].Int(), "정본 combat.json 의 pierce.spear 는 8 그대로여야 한다");
        }

        [Test]
        public void PierceZeroOrLessMeansNoLimit()
        {
            Assert.AreEqual(int.MaxValue, BattleState.PierceCap(0), "0 = 상한 없음(주인 «걍 닿으면 다 데미지»)");
            Assert.AreEqual(int.MaxValue, BattleState.PierceCap(-1), "음수도 상한 없음(값이 어쩌다 음수여도 막히지 않는다)");
            Assert.AreEqual(8, BattleState.PierceCap(8), "양수는 그대로 상한");
            Assert.AreEqual(1, BattleState.PierceCap(1));
        }

        /// <summary>
        /// 창 하나가 <b>일렬로 늘어선 적 10 이상</b>을 전부 때린다(지시서 6항) — 상한이 8 이던 때는 여기서 8 에서 끊겼다.
        /// 엔진을 통째로 돌리지 않고 «상한 규칙 + 사거리» 둘로 판정한다: 적 간격(<c>enemyGap</c>) × 마릿수가 사거리 안에 들어오는지까지 같이 본다.
        /// </summary>
        [Test]
        public void OneSpearCoversMoreThanTenEnemiesInARow()
        {
            var D = TestData.Load();
            int cap = BattleState.PierceCap(D.Combat.PierceSpear);
            Assert.Greater(cap, 10, "관통 상한이 10 마리를 넘어야 한다");
            // 사거리 안에 실제로 몇 마리가 들어오나 — 간격 44 · 사거리 312 → 8 마리(정본 352 면 9 마리)
            int inReach = (int)(D.Combat.SpearReach / D.Enemies.EnemyGap) + 1;
            Assert.GreaterOrEqual(inReach, 8, "사거리 안에 최소 8 마리는 든다(주인 «닿으면 다 데미지» 의 «닿는» 범위)");
            // 창이 «지나가며» 때리는 자리(Battle 의 두 번째 판정)는 상한만 보므로 줄이 길어도 안 끊긴다
            var hit = new HashSet<int>();
            for (int i = 0; i < 30 && hit.Count < cap; i++) hit.Add(i);
            Assert.AreEqual(30, hit.Count, "상한이 없으면 30 마리 줄도 끝까지 지나간다");
        }
    }
}
