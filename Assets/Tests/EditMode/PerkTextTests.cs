using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T53 — 특전 설명 «트리거: 내용» 표기(주인 2026-09-06 · 상시 능력치는 «패시브: …» · 09:3X 정정). 정본 perks.json(100개 · 불변)의 desc 를 <see cref="PerkText.Format"/> 에 넣은 결과를
    /// **100개 전수 기대값 표**와 대조한다 — 트리거 구가 있는 76개는 그 트리거로, 없는 24개(상시 능력치)는 «패시브: » 로 — 100개 전부 콜론이 있다. 새 특전이 생기거나 desc 가 바뀌면 여기서 잡힌다.
    /// </summary>
    public class PerkTextTests
    {
        static readonly Dictionary<string, string> Expected = new Dictionary<string, string>
        {
            { "p_evadeHeal", "회피 시: 33% 확률로 최대 체력 12% 회복" },
            { "p_atk", "패시브: 공격력 +15%" },
            { "p_evade", "패시브: 회피율 +8" },
            { "p_arrowEv", "회피 시: 33% 확률로 화살 1개 (공격력의 30%)" },
            { "p_axeHit", "피격 시: 33% 확률로 도끼 1개 (공격력의 50%)" },
            { "p_counter", "패시브: 반격률 +8" },
            { "p_spearCt", "반격 시: 창 1개 (공격력의 100% · 8마리 관통)" },
            { "p_critR", "패시브: 치명타 확률 +8" },
            { "p_critF", "패시브: 치명타 피해 +30" },
            { "p_def", "패시브: 방어력 +8%" },
            { "p_killSpearN", "처치 시: 33% 확률로 창 1개 (공격력의 100% · 8마리 관통)" },
            { "p_killBoltN", "처치 시: 33% 확률로 보이는 적 전부에게 번개 1회씩 (공격력의 75%)" },
            { "p_killArrowN", "처치 시: 33% 확률로 화살 3개 (공격력의 30%)" },
            { "p_killAxeN", "처치 시: 33% 확률로 도끼 2개 (공격력의 50%)" },
            { "p_thornsN", "패시브: 가시갑옷 +100%" },
            { "p_killEvBuff", "처치 시: 2초간 회피율 +40" },
            { "p_collAtk", "특전 하나당: 공격력 +4%" },
            { "p_collCrit", "특전 하나당: 치명타 확률 +2" },
            { "p_killAtkStk", "처치 시: 33% 확률로 공격력 +1%(이 판 동안 누적)" },
            { "p_killEvStk", "처치 시: 33% 확률로 회피율 +1(이 판 동안 누적)" },
            { "p_killHealN", "처치 시: 33% 확률로 최대 체력 6% 회복" },
            { "p_collHp", "특전 하나당: 최대 체력 +7%" },
            { "p_critStack", "평타 적중마다: 치명타 확률 +1(치명타 시 초기화)" },
            { "p_aspdAtk", "공격 시: 공격속도 +7% 7초(중첩)" },
            { "p_execEvN", "회피 시: 5% 확률로 그 적 즉사" },
            { "p_stunCritN", "치명타 시: 10% 확률로 3초 스턴" },
            { "p_nArrowN", "2타마다: 무작위 적에게 화살 1개 (공격력의 30%)" },
            { "p_nAxeN", "3타마다: 무작위 적에게 도끼 1개 (공격력의 50%)" },
            { "p_nBoltN", "3타마다: 무작위 적에게 번개 1회 (공격력의 75%)" },
            { "p_nHealN", "5타마다: 최대 체력 6% 회복" },
            { "p_evadeStun", "회피 시: 30% 확률로 공격한 적 3초 스턴" },
            { "p_ctCritN", "반격 시: 치명타 확률 +20" },
            { "p_ctDmgN", "패시브: 반격 데미지 +30%" },
            { "p_killSureCrit", "처치 시: 다음 공격은 반드시 치명타" },
            { "p_cleaveN", "공격 시: 33% 확률로 바로 뒤 적도 같은 데미지" },
            { "p_ignoreN", "피격 시: 20% 확률로 그 피격 데미지 무시" },
            { "p_noShAtk", "실드 0 일 때: 공격력 +50%" },
            { "p_noShAspd", "실드 0 일 때: 공격속도 +30%" },
            { "p_wardHitN", "피격 시: 10% 확률로 방어막 1장" },
            { "p_fullHp", "공격 시(체력 가득 찬 적): 데미지 +100%" },
            { "p_repairUp", "패시브: 실드 수리량 +100%" },
            { "p_healUp", "패시브: 체력 회복량 +100%" },
            { "p_thornsR", "패시브: 가시갑옷 +200%" },
            { "p_killSpearR", "처치 시: 66% 확률로 창 1개 (공격력의 100% · 8마리 관통)" },
            { "p_killBoltR", "처치 시: 66% 확률로 보이는 적 전부에게 번개 1회씩 (공격력의 75%)" },
            { "p_killArrowR", "처치 시: 66% 확률로 화살 3개 (공격력의 30%)" },
            { "p_killAxeR", "처치 시: 66% 확률로 도끼 2개 (공격력의 50%)" },
            { "p_healRepair", "체력 회복 시: 같은 양만큼 실드 수리" },
            { "p_killRepair", "처치 시: 66% 확률로 최대 실드 6% 수리" },
            { "p_critFR", "패시브: 치명타 피해 +60" },
            { "p_execEvR", "회피 시: 10% 확률로 그 적 즉사" },
            { "p_stunCritR", "치명타 시: 20% 확률로 3초 스턴" },
            { "p_nArrowR", "2타마다: 무작위 적에게 화살 2개 (공격력의 30%)" },
            { "p_nAxeR", "3타마다: 무작위 적에게 도끼 2개 (공격력의 50%)" },
            { "p_nBoltR", "3타마다: 무작위 적에게 번개 2회 (공격력의 75%)" },
            { "p_critRR", "패시브: 치명타 확률 +16" },
            { "p_counterR", "패시브: 반격률 +16" },
            { "p_atkR", "패시브: 공격력 +30%" },
            { "p_evadeR", "패시브: 회피율 +16" },
            { "p_killDash", "처치 시: 같은 웨이브의 다음 적까지 대시" },
            { "p_berserkStk", "처치 시: 스택 1 · 평타마다 1 소모하고 그 공격 +100%" },
            { "p_ctCritR", "반격 시: 치명타 확률 +40" },
            { "p_ctDmgR", "패시브: 반격 데미지 +60%" },
            { "p_cleaveR", "공격 시: 66% 확률로 바로 뒤 적도 같은 데미지" },
            { "p_arrowEvR", "회피 시: 66% 확률로 화살 1개 (공격력의 30%)" },
            { "p_axeHitR", "피격 시: 66% 확률로 도끼 1개 (공격력의 50%)" },
            { "p_evHealR", "회피 시: 66% 확률로 최대 체력 12% 회복" },
            { "p_evRepairR", "회피 시: 15% 확률로 최대 실드 6% 수리" },
            { "p_defR", "패시브: 방어력 +16%" },
            { "p_wardHitR", "피격 시: 20% 확률로 방어막 1장" },
            { "p_critSpearR", "치명타 시: 33% 확률로 창 1개 (공격력의 100% · 8마리 관통)" },
            { "p_killSpearL", "처치 시: 창 1개 (공격력의 100% · 8마리 관통)" },
            { "p_killBoltL", "처치 시: 보이는 적 전부에게 번개 1회씩 (공격력의 75%)" },
            { "p_overkill", "처치 시: 남은 데미지만큼 체력 회복" },
            { "p_killArrowL", "처치 시: 화살 3개 (공격력의 30%)" },
            { "p_killAxeL", "처치 시: 도끼 2개 (공격력의 50%)" },
            { "p_berserk", "패시브: 공격력 300% 가 되는 대신 치명타 확률 0%" },
            { "p_nobleEye", "패시브: 다음 특전부터 최소 희귀 이상만 나온다" },
            { "p_spearAvatar", "패시브: 내가 쏘는 모든 화살이 창으로 바뀐다 (창 · 공격력의 100%)" },
            { "p_thornsL", "패시브: 가시갑옷 +300%" },
            { "p_giant", "패시브: 공격력 +200% 대신 공격속도 2/3" },
            { "p_execEvL", "회피 시: 15% 확률로 그 적 즉사" },
            { "p_stunCritL", "치명타 시: 30% 확률로 3초 스턴" },
            { "p_nArrowL", "2타마다: 무작위 적에게 화살 3개 (공격력의 30%)" },
            { "p_nAxeL", "3타마다: 무작위 적에게 도끼 3개 (공격력의 50%)" },
            { "p_nBoltL", "3타마다: 무작위 적에게 번개 3회 (공격력의 75%)" },
            { "p_nSpearL", "3타마다: 창 1개 (공격력의 100% · 8마리 관통)" },
            { "p_cleaveL", "공격 시: 바로 뒤 적도 같은 데미지" },
            { "p_critSpearL", "치명타 시: 66% 확률로 창 1개 (공격력의 100% · 8마리 관통)" },
            { "p_critBoltL", "치명타 시: 66% 확률로 보이는 적 전부에게 번개 1회씩 (공격력의 75%)" },
            { "p_arrowEvL", "회피 시: 화살 1개 (공격력의 30%)" },
            { "p_axeHitL", "피격 시: 도끼 1개 (공격력의 50%)" },
            { "p_spearEvL", "회피 시: 33% 확률로 창 1개 (공격력의 100% · 8마리 관통)" },
            { "p_spearHitL", "피격 시: 33% 확률로 창 1개 (공격력의 100% · 8마리 관통)" },
            { "p_evRepairL", "회피 시: 25% 확률로 최대 실드 6% 수리" },
            { "p_defL", "패시브: 방어력 +24%" },
            { "p_shWallL", "피격 시(실드 있을 때): 50% 확률로 데미지 무시" },
            { "p_shRefL", "피격 시(실드 있을 때): 50% 확률로 그 데미지를 반사" },
            { "p_wardHitL", "피격 시: 30% 확률로 방어막 1장" },
            { "p_evHealL", "회피 시: 최대 체력 12% 회복" },
        };

        [Test]
        public void AllHundredPerks_FormatMatchesExpectedTable()
        {
            var d = TestData.Load();
            Assert.That(d.Perks.Perks.Count, Is.EqualTo(Expected.Count), "특전 수 = 기대값 표 크기(100)");
            int trig = 0, passive = 0;
            foreach (var p in d.Perks.Perks)
            {
                Assert.That(Expected.ContainsKey(p.Id), Is.True, "기대값 표에 없는 특전: " + p.Id);
                string got = PerkText.Format(p.Desc);
                // T90 — 표는 T53 «트리거: 내용» 의 정본 그대로 두고, 비율 스탯 % 는 기댓값에도 씌운다(«회피율 +8» → «회피율 +8%» · T87 이 TextGlyphs 로 한 방식과 같다)
                Assert.That(got, Is.EqualTo(StatText.Percent(Expected[p.Id])), p.Id + " 원문 «" + p.Desc + "»");
                if (got.StartsWith(PerkText.PassivePrefix)) passive++; else trig++;
            }
            Assert.That(trig, Is.EqualTo(76), "트리거 구가 있는 특전 수");
            Assert.That(passive, Is.EqualTo(24), "상시 능력치(«패시브: ») 수");
        }

        [Test]
        public void EveryPerk_HasColonAndBodyIsOriginalTail()
        {
            var d = TestData.Load();
            foreach (var p in d.Perks.Perks)
            {
                string got = PerkText.Format(p.Desc);
                int c = got.IndexOf(": ", System.StringComparison.Ordinal);
                Assert.That(c, Is.GreaterThan(0), p.Id + " 콜론+한 칸(100개 전부)");
                string trigger = got.Substring(0, c), body = got.Substring(c + 2);
                // 원문에 T90 의 % 만 씌운 것과 대조한다 — 문구를 줄이거나 늘리지 않는다는 뜻은 그대로다
                string src = StatText.Percent(p.Desc);
                Assert.That(src.EndsWith(body), Is.True, p.Id + " 콜론 뒤는 원문의 꼬리 그대로");
                if (PerkText.HasTrigger(p.Desc)) Assert.That(trigger, Is.Not.EqualTo("패시브"), p.Id + " 트리거 특전은 «패시브» 가 아니다");
                else { Assert.That(got, Is.EqualTo(PerkText.PassivePrefix + src), p.Id + " 상시 능력치 = «패시브: 원문»"); }
                Assert.That(trigger.Contains("<"), Is.False, p.Id + " 트리거 구에 리치 텍스트 없음(T52 한 색)");
                Assert.That(got.Contains("<color"), Is.False, p.Id + " 부분 색 없음");
            }
        }

        [Test]
        public void Format_IsIdempotentAndNullSafe()
        {
            var d = TestData.Load();
            foreach (var p in d.Perks.Perks) Assert.That(PerkText.Format(PerkText.Format(p.Desc)), Is.EqualTo(PerkText.Format(p.Desc)), p.Id + " 멱등");
            Assert.That(PerkText.Format(null), Is.Null); Assert.That(PerkText.Format(""), Is.EqualTo(""));
            Assert.That(PerkText.Format("처치 시 X"), Is.EqualTo("처치 시: X"));
            Assert.That(PerkText.Format("3타마다 X"), Is.EqualTo("3타마다: X"));
            Assert.That(PerkText.Format("실드가 있으면 피격 시 X"), Is.EqualTo("피격 시(실드 있을 때): X"));
            Assert.That(PerkText.Format("공격력 +15%"), Is.EqualTo("패시브: 공격력 +15%"));
            Assert.That(PerkText.Format("패시브: 공격력 +15%"), Is.EqualTo("패시브: 공격력 +15%"));
        }
    }
}
