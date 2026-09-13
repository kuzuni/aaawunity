# 특전 100개 · 고유 GPT 아이콘 100종

기존 게임 그림체를 참고해 생성한 기본 효과 그림 59종과 추가 변형 41종을 사용한다. 이제 같은 효과의 등급별 특전도 서로 다른 스프라이트에 연결된다. 수치·확률만 증가하면 기존 발동 기호를 유지하면서 형태와 장식으로 구분하고, 실제 투사체 수가 늘면 그림의 화살·도끼·번개 수도 맞춘다.

기존 리깅 캐릭터, 애니메이션, 카드 배치와 밸런스 데이터는 유지한다.

## 파일과 연결

- `perk-icon-map.json`의 `perks`: 실제 특전 ID 100개와 고유 키·PNG 경로의 연결표. `families`는 59개 효과 계열과 집계용 기본 그림을 설명한다.
- `perk-icon-prompts.json`: 기본 59종의 그림체·레퍼런스·주제.
- `perk-icon-variants.json`: 추가 41종의 효과 설명·기준 그림·최종 생성 프롬프트.
- `Assets/KkomaKnight/PerkIcons/`: 256×256 투명 RGBA PNG.
- `PerkArtwork.Key`와 생성된 `AssetCatalog.asset`: 카드·특전 목록·전투 출처가 사용하는 스프라이트.
- `PerkIconMapTests`: 모든 특전이 실제 임포트된 서로 다른 스프라이트를 읽는지 검사.

다음 수정도 ChatGPT의 이미지 생성으로 해당 PNG만 교체하고 `.meta` GUID를 유지하면 된다. 새 그림 경로와 키를 추가했다면 `python3 tools/gen_meta.py` 및 `python3 tools/gen_catalog.py`를 실행한다.

## 전체 목록

| 그림 | 등급 · 특전 | 실제 효과 | ID |
|---|---|---|---|
| <img src="../../Assets/KkomaKnight/PerkIcons/evHeal.png" width="72" alt="p_evadeHeal"> | 일반 · 회피 시 회복 | 회피 시 33% 확률로 최대 체력 12% 회복 | `p_evadeHeal` |
| <img src="../../Assets/KkomaKnight/PerkIcons/atk.png" width="72" alt="p_atk"> | 일반 · 공격력 증가 | 공격력 +15% | `p_atk` |
| <img src="../../Assets/KkomaKnight/PerkIcons/evade.png" width="72" alt="p_evade"> | 일반 · 회피율 증가 | 회피율 +8 | `p_evade` |
| <img src="../../Assets/KkomaKnight/PerkIcons/arrowEv.png" width="72" alt="p_arrowEv"> | 일반 · 회피 시 화살 | 회피 시 33% 확률로 화살 1개 (공격력의 30%) | `p_arrowEv` |
| <img src="../../Assets/KkomaKnight/PerkIcons/axeHit.png" width="72" alt="p_axeHit"> | 일반 · 피격 시 도끼 | 피격 시 33% 확률로 도끼 1개 (공격력의 50%) | `p_axeHit` |
| <img src="../../Assets/KkomaKnight/PerkIcons/counter.png" width="72" alt="p_counter"> | 일반 · 반격률 증가 | 반격률 +8 | `p_counter` |
| <img src="../../Assets/KkomaKnight/PerkIcons/spearCt.png" width="72" alt="p_spearCt"> | 일반 · 반격 시 창 | 반격 시 창 1개 (공격력의 100% · 8마리 관통) | `p_spearCt` |
| <img src="../../Assets/KkomaKnight/PerkIcons/critR.png" width="72" alt="p_critR"> | 일반 · 치명타 확률 증가 | 치명타 확률 +8 | `p_critR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/critF.png" width="72" alt="p_critF"> | 일반 · 치명타 피해 증가 | 치명타 피해 +30 | `p_critF` |
| <img src="../../Assets/KkomaKnight/PerkIcons/def.png" width="72" alt="p_def"> | 일반 · 방어력 증가 | 방어력 +8% | `p_def` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killSpear.png" width="72" alt="p_killSpearN"> | 일반 · 처치 시 창 | 처치 시 33% 확률로 창 1개 (공격력의 100% · 8마리 관통) | `p_killSpearN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killBolt.png" width="72" alt="p_killBoltN"> | 일반 · 처치 시 번개 | 처치 시 33% 확률로 보이는 적 전부에게 번개 1회씩 (공격력의 75%) | `p_killBoltN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killArrow.png" width="72" alt="p_killArrowN"> | 일반 · 처치 시 화살 | 처치 시 33% 확률로 화살 3개 (공격력의 30%) | `p_killArrowN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killAxe.png" width="72" alt="p_killAxeN"> | 일반 · 처치 시 도끼 | 처치 시 33% 확률로 도끼 2개 (공격력의 50%) | `p_killAxeN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/thorns.png" width="72" alt="p_thornsN"> | 일반 · 가시갑옷 | 가시갑옷 +100% | `p_thornsN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killEvBuff.png" width="72" alt="p_killEvBuff"> | 일반 · 처치 시 회피 버프 | 처치 시 2초간 회피율 +40 | `p_killEvBuff` |
| <img src="../../Assets/KkomaKnight/PerkIcons/collAtk.png" width="72" alt="p_collAtk"> | 일반 · 수집가·공격 | 보유 특전 하나당 공격력 +4% | `p_collAtk` |
| <img src="../../Assets/KkomaKnight/PerkIcons/collCrit.png" width="72" alt="p_collCrit"> | 일반 · 수집가·치명 | 보유 특전 하나당 치명타 확률 +2 | `p_collCrit` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killAtkStk.png" width="72" alt="p_killAtkStk"> | 일반 · 처치 시 공격력 스택 | 처치 시 33% 확률로 공격력 +1%(이 판 동안 누적) | `p_killAtkStk` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killEvStk.png" width="72" alt="p_killEvStk"> | 일반 · 처치 시 회피 스택 | 처치 시 33% 확률로 회피율 +1(이 판 동안 누적) | `p_killEvStk` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killHeal.png" width="72" alt="p_killHealN"> | 일반 · 처치 시 회복 | 처치 시 33% 확률로 최대 체력 6% 회복 | `p_killHealN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/collHp.png" width="72" alt="p_collHp"> | 일반 · 수집가·체력 | 보유 특전 하나당 최대 체력 +7% | `p_collHp` |
| <img src="../../Assets/KkomaKnight/PerkIcons/critStack.png" width="72" alt="p_critStack"> | 일반 · 치명 스택 | 평타 적중마다 치명타 확률 +1(치명타 시 초기화) | `p_critStack` |
| <img src="../../Assets/KkomaKnight/PerkIcons/aspdAtk.png" width="72" alt="p_aspdAtk"> | 일반 · 공격 시 공속 버프 | 공격 시 공격속도 +7% 7초(중첩) | `p_aspdAtk` |
| <img src="../../Assets/KkomaKnight/PerkIcons/execEv.png" width="72" alt="p_execEvN"> | 일반 · 회피 시 즉사 | 회피 시 5% 확률로 그 적 즉사 | `p_execEvN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/stunCrit.png" width="72" alt="p_stunCritN"> | 일반 · 치명타 시 스턴 | 치명타 시 10% 확률로 3초 스턴 | `p_stunCritN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nArrow.png" width="72" alt="p_nArrowN"> | 일반 · 2타 화살 | 2타마다 무작위 적에게 화살 1개 (공격력의 30%) | `p_nArrowN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nAxe.png" width="72" alt="p_nAxeN"> | 일반 · 3타 도끼 | 3타마다 무작위 적에게 도끼 1개 (공격력의 50%) | `p_nAxeN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nBolt.png" width="72" alt="p_nBoltN"> | 일반 · 3타 번개 | 3타마다 무작위 적에게 번개 1회 (공격력의 75%) | `p_nBoltN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nHeal.png" width="72" alt="p_nHealN"> | 일반 · 5타 회복 | 5타마다 최대 체력 6% 회복 | `p_nHealN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/evadeStun.png" width="72" alt="p_evadeStun"> | 일반 · 회피 시 스턴 | 회피 시 30% 확률로 공격한 적 3초 스턴 | `p_evadeStun` |
| <img src="../../Assets/KkomaKnight/PerkIcons/ctCrit.png" width="72" alt="p_ctCritN"> | 일반 · 반격 치명 | 반격 시 치명타 확률 +20 | `p_ctCritN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/ctDmg.png" width="72" alt="p_ctDmgN"> | 일반 · 반격 강화 | 반격 데미지 +30% | `p_ctDmgN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killSureCrit.png" width="72" alt="p_killSureCrit"> | 일반 · 처치 시 확정 치명 | 처치 시 다음 공격은 반드시 치명타 | `p_killSureCrit` |
| <img src="../../Assets/KkomaKnight/PerkIcons/cleave.png" width="72" alt="p_cleaveN"> | 일반 · 관통 베기 | 공격 시 33% 확률로 바로 뒤 적도 같은 데미지 | `p_cleaveN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/ignore.png" width="72" alt="p_ignoreN"> | 일반 · 피해 무시 | 피격 시 20% 확률로 그 피격 데미지 무시 | `p_ignoreN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/noShAtk.png" width="72" alt="p_noShAtk"> | 일반 · 실드 없을 때 공격력 | 실드가 0 인 동안 공격력 +50% | `p_noShAtk` |
| <img src="../../Assets/KkomaKnight/PerkIcons/noShAspd.png" width="72" alt="p_noShAspd"> | 일반 · 실드 없을 때 공속 | 실드가 0 인 동안 공격속도 +30% | `p_noShAspd` |
| <img src="../../Assets/KkomaKnight/PerkIcons/wardHit.png" width="72" alt="p_wardHitN"> | 일반 · 피격 시 방어막 | 피격 시 10% 확률로 방어막 1장 | `p_wardHitN` |
| <img src="../../Assets/KkomaKnight/PerkIcons/fullHp.png" width="72" alt="p_fullHp"> | 희귀 · 풀피 적 강타 | 체력이 가득 찬 적 공격 시 데미지 +100% | `p_fullHp` |
| <img src="../../Assets/KkomaKnight/PerkIcons/repairUp.png" width="72" alt="p_repairUp"> | 희귀 · 수리 증폭 | 실드 수리량 +100% | `p_repairUp` |
| <img src="../../Assets/KkomaKnight/PerkIcons/healUp.png" width="72" alt="p_healUp"> | 희귀 · 회복 증폭 | 체력 회복량 +100% | `p_healUp` |
| <img src="../../Assets/KkomaKnight/PerkIcons/thornsR.png" width="72" alt="p_thornsR"> | 희귀 · 가시갑옷 | 가시갑옷 +200% | `p_thornsR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killSpearR.png" width="72" alt="p_killSpearR"> | 희귀 · 처치 시 창 | 처치 시 66% 확률로 창 1개 (공격력의 100% · 8마리 관통) | `p_killSpearR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killBoltR.png" width="72" alt="p_killBoltR"> | 희귀 · 처치 시 번개 | 처치 시 66% 확률로 보이는 적 전부에게 번개 1회씩 (공격력의 75%) | `p_killBoltR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killArrowR.png" width="72" alt="p_killArrowR"> | 희귀 · 처치 시 화살 | 처치 시 66% 확률로 화살 3개 (공격력의 30%) | `p_killArrowR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killAxeR.png" width="72" alt="p_killAxeR"> | 희귀 · 처치 시 도끼 | 처치 시 66% 확률로 도끼 2개 (공격력의 50%) | `p_killAxeR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/healRepair.png" width="72" alt="p_healRepair"> | 희귀 · 회복 시 수리 | 체력 회복 시 같은 양만큼 실드 수리 | `p_healRepair` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killRepair.png" width="72" alt="p_killRepair"> | 희귀 · 처치 시 수리 | 처치 시 66% 확률로 최대 실드 6% 수리 | `p_killRepair` |
| <img src="../../Assets/KkomaKnight/PerkIcons/critFR.png" width="72" alt="p_critFR"> | 희귀 · 치명타 피해 증가 II | 치명타 피해 +60 | `p_critFR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/execEvR.png" width="72" alt="p_execEvR"> | 희귀 · 회피 시 즉사 II | 회피 시 10% 확률로 그 적 즉사 | `p_execEvR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/stunCritR.png" width="72" alt="p_stunCritR"> | 희귀 · 치명타 시 스턴 II | 치명타 시 20% 확률로 3초 스턴 | `p_stunCritR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nArrowR.png" width="72" alt="p_nArrowR"> | 희귀 · 2타 화살 II | 2타마다 무작위 적에게 화살 2개 (공격력의 30%) | `p_nArrowR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nAxeR.png" width="72" alt="p_nAxeR"> | 희귀 · 3타 도끼 II | 3타마다 무작위 적에게 도끼 2개 (공격력의 50%) | `p_nAxeR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nBoltR.png" width="72" alt="p_nBoltR"> | 희귀 · 3타 번개 II | 3타마다 무작위 적에게 번개 2회 (공격력의 75%) | `p_nBoltR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/critRR.png" width="72" alt="p_critRR"> | 희귀 · 치명타 확률 증가 II | 치명타 확률 +16 | `p_critRR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/counterR.png" width="72" alt="p_counterR"> | 희귀 · 반격률 증가 II | 반격률 +16 | `p_counterR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/atkR.png" width="72" alt="p_atkR"> | 희귀 · 공격력 증가 II | 공격력 +30% | `p_atkR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/evadeR.png" width="72" alt="p_evadeR"> | 희귀 · 회피율 증가 II | 회피율 +16 | `p_evadeR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killDash.png" width="72" alt="p_killDash"> | 희귀 · 처치 시 대시 | 처치 시 같은 웨이브의 다음 적까지 대시 | `p_killDash` |
| <img src="../../Assets/KkomaKnight/PerkIcons/berserkStk.png" width="72" alt="p_berserkStk"> | 희귀 · 버서커 | 처치 시 스택 1 · 평타마다 1 소모하고 그 공격 +100% | `p_berserkStk` |
| <img src="../../Assets/KkomaKnight/PerkIcons/ctCritR.png" width="72" alt="p_ctCritR"> | 희귀 · 반격 치명 II | 반격 시 치명타 확률 +40 | `p_ctCritR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/ctDmgR.png" width="72" alt="p_ctDmgR"> | 희귀 · 반격 강화 II | 반격 데미지 +60% | `p_ctDmgR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/cleaveR.png" width="72" alt="p_cleaveR"> | 희귀 · 관통 베기 II | 공격 시 66% 확률로 바로 뒤 적도 같은 데미지 | `p_cleaveR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/arrowEvR.png" width="72" alt="p_arrowEvR"> | 희귀 · 회피 시 화살 II | 회피 시 66% 확률로 화살 1개 (공격력의 30%) | `p_arrowEvR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/axeHitR.png" width="72" alt="p_axeHitR"> | 희귀 · 피격 시 도끼 II | 피격 시 66% 확률로 도끼 1개 (공격력의 50%) | `p_axeHitR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/evHealR.png" width="72" alt="p_evHealR"> | 희귀 · 회피 시 회복 II | 회피 시 66% 확률로 최대 체력 12% 회복 | `p_evHealR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/evRepair.png" width="72" alt="p_evRepairR"> | 희귀 · 회피 시 수리 | 회피 시 15% 확률로 최대 실드 6% 수리 | `p_evRepairR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/defR.png" width="72" alt="p_defR"> | 희귀 · 방어력 증가 II | 방어력 +16% | `p_defR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/wardHitR.png" width="72" alt="p_wardHitR"> | 희귀 · 피격 시 방어막 II | 피격 시 20% 확률로 방어막 1장 | `p_wardHitR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/critSpear.png" width="72" alt="p_critSpearR"> | 희귀 · 치명 시 창 | 치명타 시 33% 확률로 창 1개 (공격력의 100% · 8마리 관통) | `p_critSpearR` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killSpearL.png" width="72" alt="p_killSpearL"> | 전설 · 처치 시 창 | 처치 시 창 1개 (공격력의 100% · 8마리 관통) | `p_killSpearL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killBoltL.png" width="72" alt="p_killBoltL"> | 전설 · 처치 시 번개 | 처치 시 보이는 적 전부에게 번개 1회씩 (공격력의 75%) | `p_killBoltL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/overkill.png" width="72" alt="p_overkill"> | 전설 · 오버킬 회복 | 처치 시 남은 데미지만큼 체력 회복 | `p_overkill` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killArrowL.png" width="72" alt="p_killArrowL"> | 전설 · 처치 시 화살 | 처치 시 화살 3개 (공격력의 30%) | `p_killArrowL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/killAxeL.png" width="72" alt="p_killAxeL"> | 전설 · 처치 시 도끼 | 처치 시 도끼 2개 (공격력의 50%) | `p_killAxeL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/berserk.png" width="72" alt="p_berserk"> | 전설 · 광전사 | 공격력 300% 가 되는 대신 치명타 확률 0% | `p_berserk` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nobleEye.png" width="72" alt="p_nobleEye"> | 전설 · 귀족의 눈 | 다음 특전부터 최소 희귀 이상만 나온다 | `p_nobleEye` |
| <img src="../../Assets/KkomaKnight/PerkIcons/spearAvatar.png" width="72" alt="p_spearAvatar"> | 전설 · 창의 화신 | 내가 쏘는 모든 화살이 창으로 바뀐다 (창 · 공격력의 100%) | `p_spearAvatar` |
| <img src="../../Assets/KkomaKnight/PerkIcons/thornsL.png" width="72" alt="p_thornsL"> | 전설 · 가시갑옷 | 가시갑옷 +300% | `p_thornsL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/giant.png" width="72" alt="p_giant"> | 전설 · 거인의 힘 | 공격력 +200% 대신 공격속도 2/3 | `p_giant` |
| <img src="../../Assets/KkomaKnight/PerkIcons/execEvL.png" width="72" alt="p_execEvL"> | 전설 · 회피 시 즉사 III | 회피 시 15% 확률로 그 적 즉사 | `p_execEvL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/stunCritL.png" width="72" alt="p_stunCritL"> | 전설 · 치명타 시 스턴 III | 치명타 시 30% 확률로 3초 스턴 | `p_stunCritL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nArrowL.png" width="72" alt="p_nArrowL"> | 전설 · 2타 화살 III | 2타마다 무작위 적에게 화살 3개 (공격력의 30%) | `p_nArrowL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nAxeL.png" width="72" alt="p_nAxeL"> | 전설 · 3타 도끼 III | 3타마다 무작위 적에게 도끼 3개 (공격력의 50%) | `p_nAxeL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nBoltL.png" width="72" alt="p_nBoltL"> | 전설 · 3타 번개 III | 3타마다 무작위 적에게 번개 3회 (공격력의 75%) | `p_nBoltL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/nSpear.png" width="72" alt="p_nSpearL"> | 전설 · 3타 창 | 3타마다 창 1개 (공격력의 100% · 8마리 관통) | `p_nSpearL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/cleaveL.png" width="72" alt="p_cleaveL"> | 전설 · 관통 베기 III | 공격 시 바로 뒤 적도 같은 데미지 | `p_cleaveL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/critSpearL.png" width="72" alt="p_critSpearL"> | 전설 · 치명 시 창 | 치명타 시 66% 확률로 창 1개 (공격력의 100% · 8마리 관통) | `p_critSpearL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/critBolt.png" width="72" alt="p_critBoltL"> | 전설 · 치명 시 번개 | 치명타 시 66% 확률로 보이는 적 전부에게 번개 1회씩 (공격력의 75%) | `p_critBoltL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/arrowEvL.png" width="72" alt="p_arrowEvL"> | 전설 · 회피 시 화살 III | 회피 시 화살 1개 (공격력의 30%) | `p_arrowEvL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/axeHitL.png" width="72" alt="p_axeHitL"> | 전설 · 피격 시 도끼 III | 피격 시 도끼 1개 (공격력의 50%) | `p_axeHitL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/spearEv.png" width="72" alt="p_spearEvL"> | 전설 · 회피 시 창 | 회피 시 33% 확률로 창 1개 (공격력의 100% · 8마리 관통) | `p_spearEvL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/spearHit.png" width="72" alt="p_spearHitL"> | 전설 · 피격 시 창 | 피격 시 33% 확률로 창 1개 (공격력의 100% · 8마리 관통) | `p_spearHitL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/evRepairL.png" width="72" alt="p_evRepairL"> | 전설 · 회피 시 수리 II | 회피 시 25% 확률로 최대 실드 6% 수리 | `p_evRepairL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/defL.png" width="72" alt="p_defL"> | 전설 · 방어력 증가 III | 방어력 +24% | `p_defL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/shWall.png" width="72" alt="p_shWallL"> | 전설 · 실드 방벽 | 실드가 있으면 피격 시 50% 확률로 데미지 무시 | `p_shWallL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/shRef.png" width="72" alt="p_shRefL"> | 전설 · 실드 반사 | 실드가 있으면 피격 시 50% 확률로 그 데미지를 반사 | `p_shRefL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/wardHitL.png" width="72" alt="p_wardHitL"> | 전설 · 피격 시 방어막 III | 피격 시 30% 확률로 방어막 1장 | `p_wardHitL` |
| <img src="../../Assets/KkomaKnight/PerkIcons/evHealL.png" width="72" alt="p_evHealL"> | 전설 · 회피 시 회복 III | 회피 시 최대 체력 12% 회복 | `p_evHealL` |
