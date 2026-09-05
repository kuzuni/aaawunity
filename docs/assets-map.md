# 에셋 사용 지도 (assets-map)

> `tools/gen_catalog.py` 가 `Assets/KkomaKnight/catalog.json` 에서 생성한다 — 손으로 고치지 말고 catalog.json 을 고칠 것.
> 키는 코드(`App.Assets.Sprite("key")` 등)에서 쓰는 이름, 경로는 주인 에셋의 실제 위치다. 주인이 바꾸고 싶은 줄만 말해 주면 그 줄의 경로를 바꾼다.

| 종류 | 키 | 에셋 (경로#스프라이트) | ID | 쓰는 자리 |
|---|---|---|---|---|
| sprites | `cm.knight.helmet` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_014_SilverBlue.png` | fileID 21300000 | 플레이어(꼬마기사) 투구 — SilverBlue. Character.prefab 의 Body/Head/Helmet 슬롯 |
| sprites | `cm.knight.hairHelmet` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Character/Helmet_Hair/Body_Helmet_Hair_001.png` | fileID 21300000 | 투구 쓸 때 보이는 머리카락(Hair_Helmet 슬롯) |
| sprites | `cm.knight.chest` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_014_BlueGold.png` | fileID 21300000 | 플레이어 갑옷 BlueGold |
| sprites | `cm.knight.sword` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Sword/FA_WP_Main_Sword_001_Silver.png` | fileID 21300000 | 플레이어 무기 — 은빛 검(HandRight/Sword) |
| sprites | `cm.knight.shield` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandLeft/Shield/FA_WP_Sub_Shield_004_Silver.png` | fileID 21300000 | 플레이어 방패(HandLeft/Shield) — 실드 수치가 있을 때만 켠다 |
| sprites | `cm.meleeA.helmet` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_009_Red.png` | fileID 21300000 | 근접 적 A(뾰족 투구+검) — enemies.json 근접 웨이브 스킨 0 |
| sprites | `cm.meleeA.chest` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_005_Green.png` | fileID 21300000 |  |
| sprites | `cm.meleeA.sword` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Sword/FA_WP_Main_Sword_002_Wood.png` | fileID 21300000 |  |
| sprites | `cm.meleeB.chest` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_002_Gray.png` | fileID 21300000 | 근접 적 B(회색 투구+도끼) — 스킨 1 (주인 지시 2026-09-05: 적은 전부 모자를 쓴다 · 맨머리 없음) |
| sprites | `cm.meleeB.axe` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Axe/FA_Wp_Main_Axe_001_WoodGray.png` | fileID 21300000 |  |
| sprites | `cm.meleeC.helmet` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_002_Brown.png` | fileID 21300000 | 근접 적 C(두건+검) — 스킨 2 |
| sprites | `cm.meleeC.chest` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_002_Dark.png` | fileID 21300000 |  |
| sprites | `cm.meleeC.sword` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Sword/FA_WP_Main_Sword_006_BrownGray.png` | fileID 21300000 |  |
| sprites | `cm.rangedA.helmet` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_019_Silver.png` | fileID 21300000 | 원거리 적 A(두건+활) — ranged 플래그 스킨 0 |
| sprites | `cm.rangedA.bow` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Bow/FA_WP_Main_Bow_001_WoodGreen.png` | fileID 21300000 |  |
| sprites | `cm.rangedA.arrow` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Arrow/FA_Consumable_Arrow_002_SilverGreen.png` | fileID 21300000 |  |
| sprites | `cm.rangedB.helmet` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_003_Wood.png` | fileID 21300000 | 원거리 적 B(뾰족+활) — 스킨 1 |
| sprites | `cm.rangedB.chest` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_003_BrownWhite.png` | fileID 21300000 |  |
| sprites | `cm.rangedB.bow` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Bow/FA_WP_Main_Bow_002_Wood.png` | fileID 21300000 |  |
| sprites | `cm.rangedB.arrow` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Arrow/FA_Consumable_Arrow_001_YellowWood.png` | fileID 21300000 |  |
| sprites | `cm.bow.lineUp` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Bow/Bow_Line_Up.png` | fileID 21300000 | 활 시위(Bow_Line_Up/Down) — 원거리 적만 켠다 |
| sprites | `cm.bow.lineDown` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Bow/Bow_Line_Down.png` | fileID 21300000 |  |
| sprites | `cm.boss.helmet` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_021_Dark.png` | fileID 21300000 | 보스 — 검은 투구 · BlackGold 갑옷 · 붉은 대형 도끼 · 피부 틴트 (0.38,0.30,0.42) · 크기 ×BossSizeMul |
| sprites | `cm.boss.chest` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_028_BlackGold.png` | fileID 21300000 |  |
| sprites | `cm.boss.axe` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Axe/FA_WP_Main_Axe_011_RedDark.png` | fileID 21300000 |  |
| sprites | `cm.spear` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Spear/FA_WP_Main_Spear_001_WoodGray.png` | fileID 21300000 | 특전 «창» 투사체 스프라이트(플레이어 무기가 아닌 투사체용) |
| sprites | `cm.gear.helm.crit.0` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_002_Brown.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 치명 세트 · 일반(0) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.crit.1` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_009_Red.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 치명 세트 · 희귀(1) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.crit.2` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_033_GoldRed.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 치명 세트 · 전설(2) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.crit.3` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_038_RedGold.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 치명 세트 · 신화(3) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.hpsh.0` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_005_GrayWood.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 체력실드 세트 · 일반(0) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.hpsh.1` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_016_Blue.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 체력실드 세트 · 희귀(1) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.hpsh.2` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_020_SilverGold.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 체력실드 세트 · 전설(2) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.hpsh.3` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_036_WhiteGold.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 체력실드 세트 · 신화(3) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.evade.0` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_003_Wood.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 회피 세트 · 일반(0) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.evade.1` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_011_Green.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 회피 세트 · 희귀(1) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.evade.2` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_037_SilverGreen.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 회피 세트 · 전설(2) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.helm.evade.3` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_034_GoldPurple.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 투구 · 회피 세트 · 신화(3) → Character 프리팹 Body/Head/Helmet |
| sprites | `cm.gear.weapon.crit.0` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Sword/FA_WP_Main_Sword_002_Wood.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 치명 세트 · 일반(0) → Character 프리팹 HandRight/Sword(검) |
| sprites | `cm.gear.weapon.crit.1` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Sword/FA_WP_Main_Sword_001_Blue.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 치명 세트 · 희귀(1) → Character 프리팹 HandRight/Sword(검) |
| sprites | `cm.gear.weapon.crit.2` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Sword/FA_WP_Main_Sword_003_GoldRed.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 치명 세트 · 전설(2) → Character 프리팹 HandRight/Sword(검) |
| sprites | `cm.gear.weapon.crit.3` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Sword/FA_WP_Main_Sword_003_PurpleSilver.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 치명 세트 · 신화(3) → Character 프리팹 HandRight/Sword(검) |
| sprites | `cm.gear.weapon.hpsh.0` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Blunt/FA_WP_Main_Blunt_001_Wood.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 체력실드 세트 · 일반(0) → Character 프리팹 HandRight/Blunt(둔기) |
| sprites | `cm.gear.weapon.hpsh.1` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Blunt/FA_WP_Main_Blunt_002_Gray.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 체력실드 세트 · 희귀(1) → Character 프리팹 HandRight/Blunt(둔기) |
| sprites | `cm.gear.weapon.hpsh.2` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Blunt/FA_WP_Main_Blunt_007_YellowWood.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 체력실드 세트 · 전설(2) → Character 프리팹 HandRight/Blunt(둔기) |
| sprites | `cm.gear.weapon.hpsh.3` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Blunt/FA_WP_Main_Blunt_006_OrangeGray.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 체력실드 세트 · 신화(3) → Character 프리팹 HandRight/Blunt(둔기) |
| sprites | `cm.gear.weapon.evade.0` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Spear/FA_WP_Main_Spear_001_Wood.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 회피 세트 · 일반(0) → Character 프리팹 HandRight/Spear(창) |
| sprites | `cm.gear.weapon.evade.1` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Spear/FA_WP_Main_Spear_004_WoodBlue.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 회피 세트 · 희귀(1) → Character 프리팹 HandRight/Spear(창) |
| sprites | `cm.gear.weapon.evade.2` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Spear/FA_WP_Main_Spear_008_GrayGold.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 회피 세트 · 전설(2) → Character 프리팹 HandRight/Spear(창) |
| sprites | `cm.gear.weapon.evade.3` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/HandRight/Spear/FA_WP_Main_Spear_010_MetalBlue.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 무기 · 회피 세트 · 신화(3) → Character 프리팹 HandRight/Spear(창) |
| sprites | `cm.gear.armor.crit.0` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_004_Brown.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 치명 세트 · 일반(0) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.crit.1` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_014_BlueRed.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 치명 세트 · 희귀(1) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.crit.2` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_015_RedGold.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 치명 세트 · 전설(2) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.crit.3` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_028_RedGold.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 치명 세트 · 신화(3) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.hpsh.0` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_006_Gray.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 체력실드 세트 · 일반(0) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.hpsh.1` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_029_Blue.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 체력실드 세트 · 희귀(1) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.hpsh.2` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_023_SilverGold.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 체력실드 세트 · 전설(2) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.hpsh.3` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_027_GoldBlue.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 체력실드 세트 · 신화(3) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.evade.0` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_011_Wood.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 회피 세트 · 일반(0) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.evade.1` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_013_Green.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 회피 세트 · 희귀(1) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.evade.2` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_015_GreenSilver.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 회피 세트 · 전설(2) → Character 프리팹 Body/Chest |
| sprites | `cm.gear.armor.evade.3` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Chest/FA_Chest_029_Purple.png` | fileID 21300000 | 장착 외형+아이콘(GearLook 표 · T7) — 갑옷 · 회피 세트 · 신화(3) → Character 프리팹 Body/Chest |
| sprites | `env.field` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Field_Forest.png` | fileID 21300000 | (구) 단일 숲 바닥 — 지금은 env.<theme>.field 를 쓴다 · 폴백 |
| sprites | `env.road` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Road_Forest.png` | fileID 21300000 | 바닥 위 길 타일 — 캐릭터 발 줄 |
| sprites | `env.roadUp` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Road_up_Forest.png` | fileID 21300000 | 길 위 경계 장식(253×33) |
| sprites | `env.tree` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Green_01.png` | fileID 21300000 | 지면 뒤 소품(나무) — 챕터마다 시드 고정 배치 |
| sprites | `env.bush` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Green_01.png` | fileID 21300000 | 지면 소품(덤불) |
| sprites | `env.mushroom` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Mushroom/Mushroom_Pink_01.png` | fileID 21300000 | 쉼터 노드 옆 버섯 · 지면 소품 |
| sprites | `env.barrel` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Ork.png` | fileID 21300000 | 쉼터 노드 — 통(Ork) + CFXR Fire(모닥불) + 버섯 (Environment 팩에 모닥불이 없어 이렇게 조합 · 주인 «알아서») |
| sprites | `env.monolith` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_11.png` | fileID 21300000 | 악마 노드 — 회색 돌기둥 + CFXR2 Souls Escape + 죽은 나무 |
| sprites | `env.stoneBig` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_12.png` | fileID 21300000 | 천사 노드 — 큰 돌 + CFXR3 LightGlow A(Loop) |
| sprites | `env.deadTree` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Dead_Tree_Brown_03.png` | fileID 21300000 | 악마 노드 옆 죽은 나무 |
| sprites | `env.stoneSmall` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Brown_06.png` | fileID 21300000 | 지면 소품(작은 돌) |
| sprites | `pi.attack` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/attack.png` | fileID 21300000 | 스탯 «공격력» 아이콘 (HUD 스탯 그리드 · 특전 팝업 상단 줄 · 특전 카드) |
| sprites | `pi.defense` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/defense.png` | fileID 21300000 | 스탯 «방어력» |
| sprites | `pi.atk_spd` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/atk_spd.png` | fileID 21300000 | 스탯 «공격속도» |
| sprites | `pi.fist` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/fist.png` | fileID 21300000 | 스탯 «반격 확률» |
| sprites | `pi.critical` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/critical.png` | fileID 21300000 | 스탯 «치명타 확률» |
| sprites | `pi.damage` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/damage.png` | fileID 21300000 | 스탯 «치명타 배율» |
| sprites | `pi.wing` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/wing.png` | fileID 21300000 | 회피 계열 특전 · 천사 팝업 |
| sprites | `pi.drop` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/drop.png` | fileID 21300000 | 스탯 «흡혈»(피 한 방울) |
| sprites | `pi.heart` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/heart_1.png` | fileID 21300000 | 체력 바 캡 · 회복 특전 |
| sprites | `pi.heart_round` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/heart_round.png` | fileID 21300000 |  |
| sprites | `pi.shield` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/shield.png` | fileID 21300000 | 실드 바 캡 · 수리/방어막 특전 |
| sprites | `pi.star` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/star_1.png` | fileID 21300000 | 경험치 바 캡 · 수집가 특전 |
| sprites | `pi.thunder` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/thunder.png` | fileID 21300000 | 번개 특전 |
| sprites | `pi.axe` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/axe_1.png` | fileID 21300000 | 도끼 특전 |
| sprites | `pi.arrowhead` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/arrowhead.png` | fileID 21300000 | 화살 특전 |
| sprites | `pi.dagger` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/dagger_1.png` | fileID 21300000 | 창 특전(픽토 아이콘에 창이 없어 단검으로 대체) |
| sprites | `pi.dagger2` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/dagger_2.png` | fileID 21300000 |  |
| sprites | `pi.stun` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/stun.png` | fileID 21300000 | 스턴 특전 |
| sprites | `pi.move_spd` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/move_spd.png` | fileID 21300000 | 처치 시 대시 |
| sprites | `pi.fire` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/fire.png` | fileID 21300000 | 버서커/광전사 · 쉼터 팝업 아이콘 |
| sprites | `pi.power` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/power.png` | fileID 21300000 | 거인의 힘 |
| sprites | `pi.crown` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/crown_1.png` | fileID 21300000 | 귀족의 눈 |
| sprites | `pi.block` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/block.png` | fileID 21300000 | 피해 무시 · 실드 방벽 |
| sprites | `pi.growth` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/growth.png` | fileID 21300000 | 처치 시 스택 특전 |
| sprites | `pi.skull` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/skull_1.png` | fileID 21300000 | 즉사 특전 · 사망 화면 |
| sprites | `pi.skull3` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/skull_3.png` | fileID 21300000 |  |
| sprites | `pi.target` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/target_1.png` | fileID 21300000 |  |
| sprites | `pi.swirl` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/swirl.png` | fileID 21300000 |  |
| sprites | `pi.pause` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/pause.png` | fileID 21300000 | HUD 일시정지 버튼 |
| sprites | `pi.play` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/play.png` | fileID 21300000 |  |
| sprites | `pi.info` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/info_round.png` | fileID 21300000 |  |
| sprites | `pi.book` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/book_open.png` | fileID 21300000 | 보유 특전(PERKS) 책 아이콘 — HUD Info 버튼 |
| sprites | `pi.home` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/home.png` | fileID 21300000 |  |
| sprites | `pi.setting` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/setting_1.png` | fileID 21300000 |  |
| sprites | `pi.sound` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/sound.png` | fileID 21300000 |  |
| sprites | `pi.sound_mute` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/sound_mute.png` | fileID 21300000 |  |
| sprites | `pi.video` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/video.png` | fileID 21300000 |  |
| sprites | `pi.refresh` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/refresh.png` | fileID 21300000 |  |
| sprites | `pi.cancle` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/cancle.png` | fileID 21300000 |  |
| sprites | `pi.check` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/check.png` | fileID 21300000 |  |
| sprites | `pi.lock` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/lock.png` | fileID 21300000 |  |
| sprites | `pi.anvil` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/anvil.png` | fileID 21300000 |  |
| sprites | `pi.chest` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/chest.png` | fileID 21300000 |  |
| sprites | `pi.bag` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/bag_1.png` | fileID 21300000 |  |
| sprites | `pi.shop` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/shop.png` | fileID 21300000 |  |
| sprites | `pi.battle` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/battle.png` | fileID 21300000 |  |
| sprites | `pi.coins` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/coins_1.png` | fileID 21300000 |  |
| sprites | `pi.gem` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/gem_3.png` | fileID 21300000 |  |
| sprites | `pi.exit` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/exit_1.png` | fileID 21300000 |  |
| sprites | `pi.arrow_left` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/arrow_left.png` | fileID 21300000 |  |
| sprites | `pi.arrow_right` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/arrow_right.png` | fileID 21300000 |  |
| sprites | `pi.time` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/time_1.png` | fileID 21300000 |  |
| sprites | `pi.boss` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/boss.png` | fileID 21300000 |  |
| sprites | `pi.magic` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/magic_symbol_1.png` | fileID 21300000 | 악마 팝업 아이콘 |
| sprites | `pi.heart_break` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/heart_break.png` | fileID 21300000 |  |
| sprites | `pi.sleep` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/sleep.png` | fileID 21300000 |  |
| sprites | `pi.leaf` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/leaf.png` | fileID 21300000 |  |
| sprites | `pi.hammer` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/hammer_1.png` | fileID 21300000 |  |
| sprites | `pi.gift` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/gift.png` | fileID 21300000 |  |
| sprites | `pi.potion` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/potion_1.png` | fileID 21300000 |  |
| sprites | `pi.wand` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/wand_star.png` | fileID 21300000 |  |
| sprites | `pi.energy` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/energy.png` | fileID 21300000 |  |
| sprites | `pi.necklace` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/necklace.png` | fileID 21300000 |  |
| sprites | `ui.dodge` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Stat_Dodge_01.png` | fileID 21300000 | 스탯 «회피» (UniqueIcon Stat_Dodge_01) |
| sprites | `ui.speed` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/UI_Common_Speed_01_Yellow.png` | fileID 21300000 | HUD 배속 버튼(x1/x2) |
| sprites | `ui.skull` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/UI_Play_Skull_01.png` | fileID 21300000 | 클리어 팝업 «처치 수» |
| sprites | `ui.coin` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Economy_Coin_02_Gold.png` | fileID 21300000 | 골드 보상 아이콘(클리어/사망 팝업 GetItem_Reward) |
| sprites | `ui.gemRed` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Economy_Gem_04_Red.png` | fileID 21300000 |  |
| sprites | `ui.bookBlue` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Item_Book_01_Blue.png` | fileID 21300000 | 사망 팝업 팁 행 아이콘 |
| sprites | `ui.bookRed` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Item_Book_03_Red.png` | fileID 21300000 |  |
| sprites | `ui.anvil` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Item_Anvil_01_Light.png` | fileID 21300000 | 사망 팝업 팁(합성) · 대장간 탭 |
| sprites | `ui.hourglass` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Item_Hourglass_01_Gold.png` | fileID 21300000 | 클리어 팝업 «걸린 시간» |
| sprites | `ui.trophy` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/UI_Rewards_Trophy_01_Gold.png` | fileID 21300000 |  |
| sprites | `ui.settings` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/UI_System_Setting_01.png` | fileID 21300000 | 설정/일시정지 팝업 (주인 지정 Settings · T10 부터 프리팹 원형 그대로 — 줄·버튼·글자 전부 보이고 글자만 우리말) — 배경음 스위치만 값 저장(Save.Muted) · 닫기(X) · 전투에서만 아래 버튼 2개 = 재개/포기하고 로비로 · 나머지 기능 없음. 로비 메뉴(≡) 와 전투 일시정지에서 연다 |
| sprites | `ui.bag` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/UI_Common_Bag_01_Brown.png` | fileID 21300000 |  |
| sprites | `ui.shop` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/UI_Shop_01_Red.png` | fileID 21300000 |  |
| sprites | `ui.battle` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/UI_Play_Battle_01_Color.png` | fileID 21300000 |  |
| sprites | `ui.ad` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/UI_Common_AD_01_Yellow.png` | fileID 21300000 |  |
| sprites | `ui.fire` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Misc_Fire_01_Red.png` | fileID 21300000 |  |
| sprites | `ui.potionRed` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Consumable_Potion_01_Red.png` | fileID 21300000 |  |
| sprites | `ui.gift` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/UI_Rewards_Gift_01_Yellow.png` | fileID 21300000 |  |
| sprites | `ui.talentIcon` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Economy_Star_01_Yellow.png` | fileID 21300000 | 하단 탭 «탤런트» 아이콘 (Economy_Star_01_Yellow — 워커 선택 · 바꾸려면 경로 한 줄) |
| sprites | `ui.petIcon` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Item_Egg_01.png` | fileID 21300000 | 하단 탭 «펫» 아이콘 (Item_Egg_01 펫 알 — 워커 선택 · 바꾸려면 경로 한 줄) |
| sprites | `gi.weapon.crit` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Weapons_Sword_01.png` | fileID 21300000 | 장비 아이콘 — 무기(치명 세트=검 / 체력실드=해머 / 회피=창). 세트별로 다른 그림 · 등급은 ItemFrame 색 |
| sprites | `gi.weapon.hpsh` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Weapons_Hammer_01.png` | fileID 21300000 |  |
| sprites | `gi.weapon.evade` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Weapons_Spear_01.png` | fileID 21300000 |  |
| sprites | `gi.helm.crit` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Helmet_01.png` | fileID 21300000 |  |
| sprites | `gi.helm.hpsh` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Helmet_02.png` | fileID 21300000 |  |
| sprites | `gi.helm.evade` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Helmet_03.png` | fileID 21300000 |  |
| sprites | `gi.armor.crit` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Chest_01.png` | fileID 21300000 |  |
| sprites | `gi.armor.hpsh` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Shield_03_Blue.png` | fileID 21300000 |  |
| sprites | `gi.armor.evade` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Belt_03.png` | fileID 21300000 |  |
| sprites | `gi.glove.crit` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Glove_02_Red.png` | fileID 21300000 |  |
| sprites | `gi.glove.hpsh` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Glove_02_Blue.png` | fileID 21300000 |  |
| sprites | `gi.glove.evade` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Glove_02_Blue.png` | fileID 21300000 |  |
| sprites | `gi.boot.crit` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Boots_01.png` | fileID 21300000 |  |
| sprites | `gi.boot.hpsh` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Boots_02.png` | fileID 21300000 |  |
| sprites | `gi.boot.evade` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Boots_01.png` | fileID 21300000 |  |
| sprites | `gi.neck.crit` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Ring_01_Gold.png` | fileID 21300000 | 목걸이 아이콘이 팩에 없어 반지/룬으로 대체 |
| sprites | `gi.neck.hpsh` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Gear_Ring_01_Silver.png` | fileID 21300000 |  |
| sprites | `gi.neck.evade` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/128/Economy_Rune_01.png` | fileID 21300000 |  |
| sprites | `chest.rare` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/Chest/Chest_01_Silver.Png` | fileID 21300000 | 상점 «희귀 상자» = 은 상자 · 전설 = 금 · 신화 = 프리미엄 (Shop_Chest_Open 팝업의 Image_Chest 에 *_Open 을 꽂는다) |
| sprites | `chest.rare.open` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/Chest/Chest_01_Silver_Open.Png` | fileID 21300000 |  |
| sprites | `chest.legend` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/Chest/Chest_01_Gold.Png` | fileID 21300000 |  |
| sprites | `chest.legend.open` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/Chest/Chest_01_Gold_Open.Png` | fileID 21300000 |  |
| sprites | `chest.myth` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/Chest/Chest_01_Premium.Png` | fileID 21300000 |  |
| sprites | `chest.myth.open` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/Chest/Chest_01_Premium_Open.Png` | fileID 21300000 |  |
| sprites | `hud.gold` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/HUD/ResourceBar_Icon_Gold.png` | fileID 21300000 |  |
| sprites | `hud.gem` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/HUD/ResourceBar_Icon_Gem.png` | fileID 21300000 |  |
| sprites | `hud.resourceBg` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/HUD/ResourceBar_Bg.png` | fileID 21300000 |  |
| sprites | `hud.orbBg` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/HUD/HUD_Orb_01_Bg_1.png` | fileID 21300000 |  |
| sprites | `hud.gradeGem` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/HUD/Grade_Gem_01.png` | fileID 21300000 |  |
| sprites | `hud.gradeGemEmpty` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/HUD/Grade_Gem_01_Empty.png` | fileID 21300000 |  |
| sprites | `hud.alertAd` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/HUD/Alert_Ad_01.png` | fileID 21300000 |  |
| sprites | `fr.rect` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/BasicFrame/BasicFrame_Rectangle_01~04_White_Bg.png` | fileID 21300000 |  |
| sprites | `fr.rectBorder` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/BasicFrame/BasicFrame_Rectangle_01~04_White_Border1.png` | fileID 21300000 |  |
| sprites | `fr.r12` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/BasicFrame/BasicFrame_Rectangle_R12_Bg.png` | fileID 21300000 |  |
| sprites | `fr.circle` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/BasicFrame/BasicFrame_Circle_H86_White_Bg.png` | fileID 21300000 |  |
| sprites | `fr.circleBorder` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/BasicFrame/BasicFrame_Circle_H70_White_Border.png` | fileID 21300000 |  |
| sprites | `fr.sliderBg` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Slider/Slider_02_White_Bg.png` | fileID 21300000 |  |
| sprites | `fr.buffSlot` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/HUD/BuffSlot_01_Bg.png` | fileID 21300000 |  |
| sprites | `fr.toast` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/HUD/ToastMessage_01.png` | fileID 21300000 |  |
| sprites | `fr.itemBg` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/ItemFrame/ItemFrame_01_White_Bg.png` | fileID 21300000 |  |
| sprites | `fr.itemBorder` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/ItemFrame/ItemFrame_01_White_Border.png` | fileID 21300000 |  |
| sprites | `fr.itemGlow` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/ItemFrame/ItemFrame_01_White_FocusGlow.png` | fileID 21300000 |  |
| sprites | `fr.itemFocus` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Frame/ItemFrame/ItemFrame_01_White_FocusBorder.png` | fileID 21300000 |  |
| sprites | `fr.button` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Button/Button_01_White_Bg.Png` | fileID 21300000 |  |
| sprites | `fr.buttonInner` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Button/Button_01_White_InnerBorder1.Png` | fileID 21300000 |  |
| sprites | `fr.label` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Label/Label_Tapered_02_White_Bg.png` | fileID 21300000 |  |
| sprites | `fr.labelBorder` | `Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common/Label/Label_Tapered_02_White_Border.png` | fileID 21300000 |  |
| sprites | `fr.lineDeco` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/Title/Title_LineDeco_01_s_White.png` | fileID 21300000 |  |
| sprites | `fr.titleTangerine` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/Title/Title_01_NoDeco_Tangerine.Png` | fileID 21300000 |  |
| sprites | `fr.titlePlum` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/Title/Title_01_NoDeco_Plum.Png` | fileID 21300000 |  |
| sprites | `fr.titleYellow` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/Title/Title_01_NoDeco_Yellow.Png` | fileID 21300000 |  |
| sprites | `fr.titleGreen` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/Title/Title_01_NoDeco_Green.Png` | fileID 21300000 |  |
| sprites | `fr.titleRed` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/Title/Title_01_NoDeco_Red.Png` | fileID 21300000 |  |
| sprites | `fr.titleSky` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Sprites/Title/Title_01_NoDeco_Sky.Png` | fileID 21300000 |  |
| sprites | `cm.meleeB.helmet` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Extenstions/Parts Pack Base/Parts/Helmet/FA_Helmet_010_Gray.png` | fileID 21300000 | 근접 적 B 투구 — FA_Helmet_010_Gray (맨머리 스킨을 없애기 위해 추가) |
| sprites | `env.autumn.field` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Field_Autumn.png` | fileID 21300000 | 전투 맵 «DemoScene_Autumn» 바닥(평면색 타일) — 챕터 (n-1)%4 순환: 1=autumn 2=deepForest 3=forest 4=desert (주인 지시 2026-09-05) |
| sprites | `env.autumn.road` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Road_Autumn.png` | fileID 21300000 | «DemoScene_Autumn» 길 띠(발 줄) |
| sprites | `env.autumn.roadUp` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Road_up_Autumn.png` | fileID 21300000 | «DemoScene_Autumn» 길 위 물결 경계(데모 씬처럼 반 겹쳐 깐다) |
| sprites | `env.deepForest.field` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Field_DeepForest.png` | fileID 21300000 | 전투 맵 «DemoScene_DeepForest» 바닥(평면색 타일) — 챕터 (n-1)%4 순환: 1=autumn 2=deepForest 3=forest 4=desert (주인 지시 2026-09-05) |
| sprites | `env.deepForest.road` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Road_DeepForest.png` | fileID 21300000 | «DemoScene_DeepForest» 길 띠(발 줄) |
| sprites | `env.deepForest.roadUp` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Road_up_DeepForest.png` | fileID 21300000 | «DemoScene_DeepForest» 길 위 물결 경계(데모 씬처럼 반 겹쳐 깐다) |
| sprites | `env.forest.field` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Field_Forest.png` | fileID 21300000 | 전투 맵 «DemoScene_Forest» 바닥(평면색 타일) — 챕터 (n-1)%4 순환: 1=autumn 2=deepForest 3=forest 4=desert (주인 지시 2026-09-05) |
| sprites | `env.forest.road` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Road_Forest.png` | fileID 21300000 | «DemoScene_Forest» 길 띠(발 줄) |
| sprites | `env.forest.roadUp` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Road_up_Forest.png` | fileID 21300000 | «DemoScene_Forest» 길 위 물결 경계(데모 씬처럼 반 겹쳐 깐다) |
| sprites | `env.desert.field` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Field_Desert.png` | fileID 21300000 | 전투 맵 «DemoScene_Desert» 바닥(평면색 타일) — 챕터 (n-1)%4 순환: 1=autumn 2=deepForest 3=forest 4=desert (주인 지시 2026-09-05) |
| sprites | `env.desert.road` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Road_Desert.png` | fileID 21300000 | «DemoScene_Desert» 길 띠(발 줄) |
| sprites | `env.desert.roadUp` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Field/Road_up_Desert.png` | fileID 21300000 | «DemoScene_Desert» 길 위 물결 경계(데모 씬처럼 반 겹쳐 깐다) |
| sprites | `env.autumn.Birch_Yellow_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Birch_Yellow_02.png` | fileID 21300000 | «DemoScene_Autumn» 소품 — tools/gen_maps.py 가 씬 배치를 그대로 MapLayouts.cs 로 굽는다 (이 테마 소품 18종 · 인스턴스 73개) |
| sprites | `env.autumn.Small_Tree_Orange_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Small_Tree_Orange_01.png` | fileID 21300000 |  |
| sprites | `env.autumn.Autumn_Flower_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Autumn_Flower_02.png` | fileID 21300000 |  |
| sprites | `env.autumn.Autumn_Flower_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Autumn_Flower_01.png` | fileID 21300000 |  |
| sprites | `env.autumn.Stone_Brown_05` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Brown_05.png` | fileID 21300000 |  |
| sprites | `env.autumn.Ork` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Ork.png` | fileID 21300000 |  |
| sprites | `env.autumn.Tree_Orange_06` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Orange_06.png` | fileID 21300000 |  |
| sprites | `env.autumn.Stone_Brown_07` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Brown_07.png` | fileID 21300000 |  |
| sprites | `env.autumn.Birch_Yellow_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Birch_Yellow_01.png` | fileID 21300000 |  |
| sprites | `env.autumn.Tree_Orange_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Orange_03.png` | fileID 21300000 |  |
| sprites | `env.autumn.Tree_Orange_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Orange_01.png` | fileID 21300000 |  |
| sprites | `env.autumn.Stone_Brown_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Brown_03.png` | fileID 21300000 |  |
| sprites | `env.autumn.Stone_Brown_04` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Brown_04.png` | fileID 21300000 |  |
| sprites | `env.autumn.Tree_Orange_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Orange_02.png` | fileID 21300000 |  |
| sprites | `env.autumn.Stone_Brown_06` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Brown_06.png` | fileID 21300000 |  |
| sprites | `env.autumn.Stone_Brown_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Brown_02.png` | fileID 21300000 |  |
| sprites | `env.autumn.Stone_Brown_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Brown_01.png` | fileID 21300000 |  |
| sprites | `env.autumn.Small_Tree_Orange_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Small_Tree_Orange_03.png` | fileID 21300000 |  |
| sprites | `env.deepForest.DeepForest_Flower_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/DeepForest_Flower_02.png` | fileID 21300000 | «DemoScene_DeepForest» 소품 — tools/gen_maps.py 가 씬 배치를 그대로 MapLayouts.cs 로 굽는다 (이 테마 소품 39종 · 인스턴스 104개) |
| sprites | `env.deepForest.DeepForest_Grass` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/DeepForest_Grass.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Dead_Tree_Brown_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Dead_Tree_Brown_03.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Mushroom_Yellow_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Mushroom/Mushroom_Yellow_03.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Stone_Gray2_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray2_01.png` | fileID 21300000 |  |
| sprites | `env.deepForest.DeepForest_Flower_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/DeepForest_Flower_01.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Ork` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Ork.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Bush_Green_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Green_03.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Stone_Gray2_07` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray2_07.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Tree_Green_13` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Green_13.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Tree_Green_09` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Green_09.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Small_Tree_Yellow_Green_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Small_Tree_Yellow_Green_02.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Bush_Green_05` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Green_05.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Dead_Tree_Brown_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Dead_Tree_Brown_01.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Bush_Green_04` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Green_04.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Tree_Green_15` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Green_15.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Stone_Gray2_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray2_03.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Mushroom_Yellow_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Mushroom/Mushroom_Yellow_02.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Stone_Gray2_06` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray2_06.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Stone_Gray2_04` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray2_04.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Tree_Yellow_Green_07` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_07.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Tree_Green_14` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Green_14.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Mushroom_Yellow_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Mushroom/Mushroom_Yellow_01.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Tree_Green_08` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Green_08.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Bush_Green_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Green_01.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Small_Tree_Green_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Small_Tree_Green_01.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Mushroom_Pink_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Mushroom/Mushroom_Pink_01.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Stone_Gray2_11` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray2_11.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Stone_Gray2_08` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray2_08.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Tree_Yellow_Green_12` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_12.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Dead_Tree_Brown_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Dead_Tree_Brown_02.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Bush_Green_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Green_02.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Stone_Gray2_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray2_02.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Tree_Green_10` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Green_10.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Small_Tree_Green_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Small_Tree_Green_03.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Mushroom_Pink_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Mushroom/Mushroom_Pink_02.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Tree_Green_07` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Green_07.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Stone_Gray2_05` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray2_05.png` | fileID 21300000 |  |
| sprites | `env.deepForest.Small_Tree_Yellow_Green_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Small_Tree_Yellow_Green_01.png` | fileID 21300000 |  |
| sprites | `env.forest.Forest_Flower_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Forest_Flower_01.png` | fileID 21300000 | «DemoScene_Forest» 소품 — tools/gen_maps.py 가 씬 배치를 그대로 MapLayouts.cs 로 굽는다 (이 테마 소품 33종 · 인스턴스 99개) |
| sprites | `env.forest.Bush_Yellow_Green_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Yellow_Green_01.png` | fileID 21300000 |  |
| sprites | `env.forest.Forest_Grass` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Forest_Grass.png` | fileID 21300000 |  |
| sprites | `env.forest.Tree_Yellow_Green_15` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_15.png` | fileID 21300000 |  |
| sprites | `env.forest.Small_Tree_Yellow_Green_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Small_Tree_Yellow_Green_03.png` | fileID 21300000 |  |
| sprites | `env.forest.Forest_Flower_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Forest_Flower_02.png` | fileID 21300000 |  |
| sprites | `env.forest.Tree_Yellow_Green_07` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_07.png` | fileID 21300000 |  |
| sprites | `env.forest.Tree_Yellow_Green_14` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_14.png` | fileID 21300000 |  |
| sprites | `env.forest.Small_Tree_Yellow_Green_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Small_Tree_Yellow_Green_01.png` | fileID 21300000 |  |
| sprites | `env.forest.Stone_Gray1_11` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_11.png` | fileID 21300000 |  |
| sprites | `env.forest.Tree_Yellow_Green_10` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_10.png` | fileID 21300000 |  |
| sprites | `env.forest.Bush_Yellow_Green_05` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Yellow_Green_05.png` | fileID 21300000 |  |
| sprites | `env.forest.Small_Tree_Yellow_Green_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Small_Tree_Yellow_Green_02.png` | fileID 21300000 |  |
| sprites | `env.forest.Stone_Gray1_06` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_06.png` | fileID 21300000 |  |
| sprites | `env.forest.Stone_Gray1_07` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_07.png` | fileID 21300000 |  |
| sprites | `env.forest.Tree_Yellow_Green_13` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_13.png` | fileID 21300000 |  |
| sprites | `env.forest.Bush_Yellow_Green_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Yellow_Green_02.png` | fileID 21300000 |  |
| sprites | `env.forest.Stone_Gray1_05` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_05.png` | fileID 21300000 |  |
| sprites | `env.forest.Bush_Yellow_Green_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Yellow_Green_03.png` | fileID 21300000 |  |
| sprites | `env.forest.Stone_Gray1_04` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_04.png` | fileID 21300000 |  |
| sprites | `env.forest.Stone_Gray1_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_01.png` | fileID 21300000 |  |
| sprites | `env.forest.Stone_Gray1_08` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_08.png` | fileID 21300000 |  |
| sprites | `env.forest.Stone_Gray1_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_03.png` | fileID 21300000 |  |
| sprites | `env.forest.Ork` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Ork.png` | fileID 21300000 |  |
| sprites | `env.forest.Tree_Yellow_Green_08` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_08.png` | fileID 21300000 |  |
| sprites | `env.forest.Stone_Gray1_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_02.png` | fileID 21300000 |  |
| sprites | `env.forest.Tree_Yellow_Green_09` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_09.png` | fileID 21300000 |  |
| sprites | `env.forest.Tree_Yellow_Green_12` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_12.png` | fileID 21300000 |  |
| sprites | `env.forest.Mushroom_Wihte_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Mushroom/Mushroom_Wihte_03.png` | fileID 21300000 |  |
| sprites | `env.forest.Bush_Yellow_Green_04` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Bush/Bush_Yellow_Green_04.png` | fileID 21300000 |  |
| sprites | `env.forest.Mushroom_Yellow_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Mushroom/Mushroom_Yellow_02.png` | fileID 21300000 |  |
| sprites | `env.forest.Tree_Yellow_Green_04` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Yellow_Green_04.png` | fileID 21300000 |  |
| sprites | `env.forest.Mushroom_Wihte_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Mushroom/Mushroom_Wihte_01.png` | fileID 21300000 |  |
| sprites | `env.desert.Desert_Dune` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Desert_Dune.png` | fileID 21300000 | «DemoScene_Desert» 소품 — tools/gen_maps.py 가 씬 배치를 그대로 MapLayouts.cs 로 굽는다 (이 테마 소품 22종 · 인스턴스 53개) |
| sprites | `env.desert.Stone_Gray1_07` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_07.png` | fileID 21300000 |  |
| sprites | `env.desert.Stone_Gray1_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_03.png` | fileID 21300000 |  |
| sprites | `env.desert.Ork` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Ork.png` | fileID 21300000 |  |
| sprites | `env.desert.Coconut_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Coconut_02.png` | fileID 21300000 |  |
| sprites | `env.desert.Plam_Yellow_Green_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Plam_Yellow_Green_03.png` | fileID 21300000 |  |
| sprites | `env.desert.Coconut_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Etc/Coconut_01.png` | fileID 21300000 |  |
| sprites | `env.desert.Tree_Bare_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Tree_Bare_01.png` | fileID 21300000 |  |
| sprites | `env.desert.Cactus_Green2_04` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Cactus/Cactus_Green2_04.png` | fileID 21300000 |  |
| sprites | `env.desert.Stone_Gray1_05` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_05.png` | fileID 21300000 |  |
| sprites | `env.desert.Plam_Yellow_Green_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Plam_Yellow_Green_02.png` | fileID 21300000 |  |
| sprites | `env.desert.Plam_Yellow_Green_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Plam_Yellow_Green_01.png` | fileID 21300000 |  |
| sprites | `env.desert.Plam_Green_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Plam_Green_03.png` | fileID 21300000 |  |
| sprites | `env.desert.Cactus_Green2_01` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Cactus/Cactus_Green2_01.png` | fileID 21300000 |  |
| sprites | `env.desert.Plam_Green_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree/Plam_Green_02.png` | fileID 21300000 |  |
| sprites | `env.desert.Stone_Gray1_12` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_12.png` | fileID 21300000 |  |
| sprites | `env.desert.Stone_Gray1_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_02.png` | fileID 21300000 |  |
| sprites | `env.desert.Stone_Gray1_06` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Stone/Stone_Gray1_06.png` | fileID 21300000 |  |
| sprites | `env.desert.Cactus_Green1_02` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Cactus/Cactus_Green1_02.png` | fileID 21300000 |  |
| sprites | `env.desert.Cactus_Green2_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Cactus/Cactus_Green2_03.png` | fileID 21300000 |  |
| sprites | `env.desert.Cactus_Green1_03` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Cactus/Cactus_Green1_03.png` | fileID 21300000 |  |
| sprites | `env.desert.Cactus_Green2_05` | `Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Cactus/Cactus_Green2_05.png` | fileID 21300000 |  |
| prefabs | `cm.character` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Common/Prefabs/Character.prefab` | root 1824668350962886144 | CharacterMaker Character 프리팹 — 전투의 플레이어·적(BattleWorld.MakeChar) 과 UI 초상(HeroView · RenderTexture 카메라 · 레이어 30) 이 같은 프리팹을 쓴다 |
| prefabs | `fx.hit` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit A (Red).prefab` | root 4021103657954561961 | 적 피격 (CFXR Hit A Red) |
| prefabs | `fx.crit` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue).prefab` | root 2708598583397607911 | 치명타 피격 (Impact Glowing HDR Blue · 0.2 배) |
| prefabs | `fx.evade` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR3 Hit Misc F Smoke.prefab` | root 141433446842962269 | 회피 연기 |
| prefabs | `fx.death` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magic Poof.prefab` | root 9157105887711914197 | 적 사망 Magic Poof |
| prefabs | `fx.heal` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Nature/CFXR3 Shield Leaves A (Lit).prefab` | root 4772634663576830964 | 회복 (Shield Leaves) |
| prefabs | `fx.bolt` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Electric/CFXR3 Hit Electric C (Air).prefab` | root 1141330259687333427 | 번개 특전 (Hit Electric C) |
| prefabs | `fx.trail` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Nature/CFXR4 Wind Trails.prefab` | root 3696007233179127096 | 도끼·창 투사체 꼬리 (Wind Trails) |
| prefabs | `fx.wave` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Plain/CFXR4 Sword Trail PLAIN (360 Spiral).prefab` | root 6710139492206580332 | 검기(wave) 투사체 (Sword Trail PLAIN 360 Spiral) |
| prefabs | `fx.levelup` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Light/CFXR3 Hit Light B (Air).prefab` | root 8771972552311404799 | 레벨 업 (Hit Light B) |
| prefabs | `fx.stun` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Magic Misc/CFXR4 Falling Stars.prefab` | root 7305185502956871417 | 스턴 별 (Falling Stars · 회전 리셋) |
| prefabs | `fx.bossWarn` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Eerie/CFXR2 Skull Head Alt.prefab` | root 5985634496115995773 | 보스 등장 (Skull Head Alt) |
| prefabs | `fx.devil` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Eerie/CFXR2 Souls Escape.prefab` | root 5642766282230003982 | 악마 노드 상시 (Souls Escape) |
| prefabs | `fx.angel` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Light/CFXR3 LightGlow A (Loop).prefab` | root 1590415177872986601 | 천사 노드 상시 (LightGlow A Loop) |
| prefabs | `fx.ward` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Magic Misc/CFXR3 Magic Aura A (Runic).prefab` | root 3280519133390621005 | 방어막 획득 (Magic Aura A Runic) |
| prefabs | `fx.fire` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR Fire.prefab` | root 6294508013172393196 | 쉼터 모닥불 상시 (CFXR Fire) |
| prefabs | `fx.fireHit` | `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR3 Hit Fire B (Air).prefab` | root 2504214343621411470 |  |
| prefabs | `ui.perkSelect` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoScenes/Play_Perk_Selection_02.prefab` | root 17609411814228231 | 레벨업 3택 팝업 (주인 지정 Play_Perk_Selection_02) — 카드 3행 · 하단 버튼 → 보유 특전 |
| prefabs | `ui.resultWin` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoScenes/Play_Result_Win_01.prefab` | root 9196921582786606425 | 챕터 클리어 팝업 (주인 지정 Play_Result_Win_01) |
| prefabs | `ui.resultLose` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoScenes/Play_Result_Lose.prefab` | root 1181429915366586777 | 사망 팝업 (Play_Result_Lose — 팁 3행 + 골드) |
| prefabs | `ui.settings` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoScenes/Settings.prefab` | root 5432435844308285772 | 설정/일시정지 팝업 (주인 지정 Settings · T10 부터 프리팹 원형 그대로 — 줄·버튼·글자 전부 보이고 글자만 우리말) — 배경음 스위치만 값 저장(Save.Muted) · 닫기(X) · 전투에서만 아래 버튼 2개 = 재개/포기하고 로비로 · 나머지 기능 없음. 로비 메뉴(≡) 와 전투 일시정지에서 연다 |
| prefabs | `ui.chestOpen` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoScenes/Shop_Chest_Open.prefab` | root 8655925205862286414 | 장비 소환 결과 팝업 (주인 지정 Shop_Chest_Open) — 열린 상자 chest.*.open + 얻은 장비 격자 |
| prefabs | `ui.lobby` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoScenes/Lobby_Default.prefab` | root 4248817929678912515 | 로비 (주인 지정 Lobby_Default · T6 부터 원형 그대로 · 요소 이동 없음) — 상단 UserInfo_01 초상 = HeroView(내 플레이어 CharacterMaker) · «25 / 55» 슬라이더 자리 = 전투력 · ResourceBar_Group = 골드·보석 · 챕터 제목(«Battle 1» 자리) · START · 하단 탭. ChatBox 만 끈다 |
| prefabs | `ui.itemDetail` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoScenes/Character_Hero_Item_Detail_01.prefab` | root 7414256810885239513 | 장비 세부 팝업 (Character_Hero_Item_Detail_01) — 등급 배지 · 아이콘 · 이름 · 슬롯 Lv 바 · 기여/옵션 목록 · 장착/해제/슬롯 강화 |
| prefabs | `ui.shopChest` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoLayout/ListItem_ShopChest.prefab` | root 4146958044734260073 | 상점 뽑기 상자 칸 (ListItem_ShopChest) — 상자 그림 chest.* · 확률/천장 문구 · 1회/10회 |
| prefabs | `ui.equipment` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoScenes/Character_Hero_Equipment.prefab` | root 2550204177070044198 | 장비 탭 (주인 지정 Character_Hero_Equipment · T7 부터 원형 그대로 · 요소 이동 없음) — 슬롯 6(프리팹 격자·Item 크기 그대로 · 부위 라벨 없음) · 가운데 Character 자리 = HeroView(장착 외형) · 전투력 · 공/체/실 · 인벤 = ListItem_EquipMent 격자(장착분 숨김) · 오른쪽 위 골드만 · 하단 상점/합성 |
| prefabs | `ui.equipCell` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoLayout/ListItem_EquipMent.prefab` | root 6644578971798763600 | 장비 칸(주인 지정 ListItem_EquipMent · 188×188 «이게 지금 딱 레이아웃 좋다») — 장비 화면 인벤 · 대장간 · 뽑기 결과 · 세부 팝업이 같은 칸(GearUi.Cell). NormalArea 에 등급색 ItemFrame_01_Normal_* · Item 에 아이콘 · Text_Level 에 +N · TypeArea 에 세트 아이콘 · Check = 장착중(대장간만) |
| prefabs | `ui.bossWarn` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoScenes/Play_Warning_Boss.prefab` | root 8909184347632126517 | 보스 경고 띠 (Play_Warning_Boss 의 Panel_Warning 만 떼어 쓴다) |
| prefabs | `ui.card` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoLayout/ListItem_StageBuff_02.prefab` | root 4696827470629324998 | 특전 카드 행(ListItem_StageBuff_02) — 등급별로 CardFrame_04_* / ItemFrame_04_* 색 교체 |
| prefabs | `ui.getItem` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoLayout/GetItem_Reward.prefab` | root 1117423787393482267 | 보상 칸(아이콘+수량) |
| prefabs | `ui.userInfo` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoLayout/UserInfo_02.prefab` | root 7349254385446408793 |  |
| prefabs | `ui.resourceBar` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_HUD/ResourceBar_Group.prefab` | root 1683025373553149541 | 상단 재화 바(골드·젬) — HUD · 로비는 Lobby_Default 안에 든 인스턴스를 그대로 쓴다 |
| prefabs | `ui.buffSlot` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_HUD/BuffSlot_01.prefab` | root 6338776735158097901 | HUD 왼쪽 버프 아이콘 칸 |
| prefabs | `ui.alertDot` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_HUD/Alert_Dot_01_Red.prefab` | root 6436244074252915597 | 빨간 알림 점(Alert_Dot_01_Red 47×47) — 장비 칸의 «합성 가능» 점(대장간 · 오른쪽 위 · T8 · ROUTINE 의 ui.redDot = 이 키) · NEW 점(왼쪽 아래) · 장착 슬롯의 «인벤에 더 좋은 게 있다» 는 프리팹 자체의 같은 점 |
| prefabs | `ui.toast` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_HUD/ToastMessage_01.prefab` | root 2774420329267464172 | 토스트 |
| prefabs | `ui.sliderRed` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Slider/Slider_02_Red.prefab` | root 9209254320531227673 | HP 바 (Slider_02 — Slider_01 은 fill 스프라이트 GUID 가 깨져 있어 안 쓴다) |
| prefabs | `ui.sliderSky` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Slider/Slider_02_Sky.prefab` | root 5561013251832034622 | 경험치 바 |
| prefabs | `ui.sliderBlue` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Slider/Slider_02_Blue.prefab` | root 6735977904678686521 | 실드 바 |
| prefabs | `ui.sliderGreen` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Slider/Slider_02_LightGreen.prefab` | root 8554564898920242427 |  |
| prefabs | `ui.sliderYellow` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Slider/Slider_02_Yellow.prefab` | root 1587652259353826145 | 진행도 바 |
| prefabs | `ui.btnPause` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_Pause_01.prefab` | root 4145695849633270983 | HUD 일시정지 |
| prefabs | `ui.btnInfo` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_Info.prefab` | root 5447371863407438132 | HUD 보유 특전(인포) |
| prefabs | `ui.btnMenu` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_Menu.prefab` | root 2270988460478775910 |  |
| prefabs | `ui.btnClose` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_Close_Square_01.prefab` | root 6286116089868610410 | 팝업 닫기(빨간 X) |
| prefabs | `ui.btnOrange` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_02_Orange.prefab` | root 5376548450129869775 | 주 버튼(계속·다음 챕터·광고) |
| prefabs | `ui.btnBlue` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_02_Blue.prefab` | root 3169995017952804435 | 보조 버튼(로비로·경험치·재개) |
| prefabs | `ui.btnGreen` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_02_Green.prefab` | root 9150553914163084570 | 회복·무료 축복 |
| prefabs | `ui.btnRed` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_02_Red.prefab` | root 7765488801804226672 | 악마 거래 수락 · 포기 |
| prefabs | `ui.btnGray` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_02_Gray.prefab` | root 1107728591113721123 | 거절 |
| prefabs | `ui.btnPlum` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_02_Plum.prefab` | root 1483688008532254904 |  |
| prefabs | `ui.btnYellow` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_02_Yellow.prefab` | root 2103124822572054393 |  |
| prefabs | `ui.btnSmallBlue` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_01_Blue.prefab` | root 4112720960120376265 | HUD 배속(x1/x2) 작은 버튼 |
| prefabs | `ui.btnSmallOrange` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_01_Orange.prefab` | root 7667054196127442761 |  |
| prefabs | `ui.btnStart` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Button_03_Red.prefab` | root 1623786085314472190 | 로비 START (Button_03_Red) |
| prefabs | `ui.frameDark` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/BasicFrame/BasicFrame_Square_R12_NoBorder_TransperDark.prefab` | root 3811192130493240385 | HUD 하단 반투명 패널 · 라운드 표시 |
| prefabs | `ui.frameIvory` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/BasicFrame/BasicFrame_Rectangle_01_Border_Ivory.prefab` | root 1280354769812679374 |  |
| prefabs | `ui.frameDarkBorder` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/BasicFrame/BasicFrame_SquareSharpEdge_01_l_Border_TransperDark.prefab` | root 450577865911699251 |  |
| prefabs | `ui.lineTitle` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Title/Title_LineDeco_01_s.prefab` | root 7786740578430808921 | 챕터 제목 밑줄 장식 (Title_LineDeco_01_s) |
| prefabs | `ui.lineTitleL` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Title/Title_LineDeco_01_l.prefab` | root 6209652781757596530 |  |
| prefabs | `ui.title.tangerine` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Title/Title_01_NoDeco_Tangerine.prefab` | root 5560263905569063613 | 팝업 리본 제목(기본) · plum=악마 · yellow=천사/클리어 · green=쉼터 · red=사망 · sky=보유 특전 |
| prefabs | `ui.title.plum` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Title/Title_01_NoDeco_Plum.prefab` | root 2306795138762171581 |  |
| prefabs | `ui.title.yellow` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Title/Title_01_NoDeco_Yellow.prefab` | root 5325593855183924205 |  |
| prefabs | `ui.title.green` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Title/Title_01_NoDeco_Green.prefab` | root 5467225822093011633 |  |
| prefabs | `ui.title.red` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Title/Title_01_NoDeco_Red.prefab` | root 9092593472852531757 |  |
| prefabs | `ui.title.sky` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Title/Title_01_NoDeco_Sky.prefab` | root 2565615909246734251 |  |
| prefabs | `ui.titleBrown` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Title/Title_Tapered_01_Brown.prefab` | root 8538020173010104235 |  |
| prefabs | `ui.popup` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Popup/Popup_Box_02_DecoLine_Basic.prefab` | root 921659412208554214 | 팝업 상자(Popup_Box_02_DecoLine) — 색 변형은 이벤트별 |
| prefabs | `ui.popup.green` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Popup/Popup_Box_02_DecoLine_Basic_Green.prefab` | root 568353929908025126 |  |
| prefabs | `ui.popup.plum` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Popup/Popup_Box_02_DecoLine_Basic_Plum.prefab` | root 568353929908025126 |  |
| prefabs | `ui.popup.yellow` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Popup/Popup_Box_02_DecoLine_Basic_Yellow.prefab` | root 568353929908025126 |  |
| prefabs | `ui.popup.blue` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Popup/Popup_Box_02_DecoLine_Basic_Blue.prefab` | root 568353929908025126 |  |
| prefabs | `ui.popup.red` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Popup/Popup_Box_02_DecoLine_Basic_Red.prefab` | root 568353929908025126 |  |
| prefabs | `ui.cardFrame.green` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/CardFrame/CardFrame_04_Green.prefab` | root 1566347371041086186 | 특전 등급 색 — 일반=green · 희귀=blue · 전설=yellow · 악마=plum |
| prefabs | `ui.cardFrame.blue` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/CardFrame/CardFrame_04_Blue.prefab` | root 7872468069465443681 |  |
| prefabs | `ui.cardFrame.yellow` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/CardFrame/CardFrame_04_Yellow.prefab` | root 1077005215484883201 |  |
| prefabs | `ui.cardFrame.plum` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/CardFrame/CardFrame_04_Plum.prefab` | root 4553865390594142357 |  |
| prefabs | `ui.cardFrame.red` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/CardFrame/CardFrame_04_Red.prefab` | root 8439892150457609206 |  |
| prefabs | `ui.cardFrame.brown` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/CardFrame/CardFrame_04_Brown.prefab` | root 5408260530917275344 |  |
| prefabs | `ui.itemFrame4.green` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_04_Green.prefab` | root 7006952170427634985 |  |
| prefabs | `ui.itemFrame4.blue` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_04_Blue.prefab` | root 1924394583819080972 |  |
| prefabs | `ui.itemFrame4.yellow` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_04_Yellow.prefab` | root 8828104227884993375 |  |
| prefabs | `ui.itemFrame4.plum` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_04_Plum.prefab` | root 555581468595160020 |  |
| prefabs | `ui.itemFrame4.red` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_04_Red.prefab` | root 8815560267049084126 |  |
| prefabs | `ui.itemFrame4.brown` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_04_Brown.prefab` | root 3784552692394773143 |  |
| prefabs | `ui.itemFrame.gray` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_01_Normal_Gray.prefab` | root 1201065372117892905 | 장비 등급 색 — 일반=gray · 희귀=blue · 전설=yellow · 신화=plum (4단계) |
| prefabs | `ui.itemFrame.blue` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_01_Normal_Blue.prefab` | root 8934315173311436198 |  |
| prefabs | `ui.itemFrame.yellow` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_01_Normal_Yellow.prefab` | root 4448737683355882266 |  |
| prefabs | `ui.itemFrame.plum` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_01_Normal_Plum.prefab` | root 2235254707959506410 |  |
| prefabs | `ui.itemFrame.red` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_01_Normal_Red.prefab` | root 6024362918899194262 |  |
| prefabs | `ui.itemFrame.green` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_01_Normal_Green.prefab` | root 6172468895370388958 |  |
| prefabs | `ui.itemFrame.empty` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Frame/ItemFrame/ItemFrame_01.prefab` | root 8001034410668339735 |  |
| prefabs | `ui.label.green` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Label/Label_Tapered_02_Green.prefab` | root 8545814299521033444 | 등급 라벨(pill) |
| prefabs | `ui.label.blue` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Label/Label_Tapered_02_Blue.prefab` | root 132865572412744726 |  |
| prefabs | `ui.label.yellow` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Label/Label_Tapered_02_Yellow.prefab` | root 4492099945815703624 |  |
| prefabs | `ui.label.plum` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Label/Label_Tapered_02_Plum.prefab` | root 1063311140513659405 |  |
| prefabs | `ui.label.red` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Label/Label_Tapered_02_Red.prefab` | root 165765810441978131 |  |
| prefabs | `ui.label.brown` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Label/Label_Tapered_02_Brown.prefab` | root 8612671380362345720 |  |
| prefabs | `ui.tabBar` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Button/Tab_01_BottomFlushMenu.prefab` | root 9068459138202833997 | 하단 탭 5칸 → 상점·장비·전투(가운데)·탤런트·펫 (T10 · 대장간은 장비 화면 «합성» 버튼 · 설정은 로비 메뉴(≡)/전투 일시정지) |
| prefabs | `ui.switch` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs_Control/Swich_01.prefab` | root 6431523672475112463 |  |
| prefabs | `ui.talent` | `Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Light/Prefabs/Prefabs~DemoScenes/Character_Talent_02.prefab` | root 4786098786137720783 | 탤런트·펫 팝업 (주인 지정 Character_Talent_02 · T10 · 프리팹 통째로 그대로 · 기능 없음) — 하단 탭 «탤런트»·«펫» 이 연다 · 재화 바 = 골드·보석 · 프리팹 안 탭 바로 닫는다(다른 탭 → 그 화면) |
| controllers | `cm.controller` | `Assets/Layer Lab/2D Minimal-CharacterMaker/Common/Animations/_Controller.controller` | fileID 9100000 |  |
| materials | `mat.hitFlash` | `Assets/KkomaKnight/HitFlash.mat` | fileID 2100000 | AllIn1SpriteShader(URP 2D) HITEFFECT_ON 머티리얼 — 피격 순간 0.1초 하양 플래시 |
| fonts | `font.ui` | `Assets/Fonts/Jua-Regular.ttf` | fileID 12800000 | UI 글꼴 Jua (Google Fonts OFL) — GUI Pro 의 SDF 폰트에 한글이 없어 TMP 를 런타임에 legacy Text 로 바꿔 쓴다 |
| colors | `col.gray` | `#A39B9D` | #A39B9D | GUI Pro Theme_Light 팔레트 (Button_01 색 오버라이드에서 읽음) — 등급/세트/텍스트 색은 전부 여기서 고른다 |
| colors | `col.green` | `#85D048` | #85D048 |  |
| colors | `col.blue` | `#5BB0F0` | #5BB0F0 |  |
| colors | `col.sky` | `#35A6E1` | #35A6E1 |  |
| colors | `col.yellow` | `#FFCC00` | #FFCC00 |  |
| colors | `col.orange` | `#FF8612` | #FF8612 |  |
| colors | `col.plum` | `#C76EF7` | #C76EF7 |  |
| colors | `col.red` | `#FB5951` | #FB5951 |  |
| colors | `col.brown` | `#B97A54` | #B97A54 |  |
| colors | `col.mint` | `#03E4B7` | #03E4B7 |  |
| colors | `col.ink` | `#341B19` | #341B19 |  |
| colors | `col.inkSoft` | `#633B37` | #633B37 |  |
| colors | `col.inkLight` | `#8B5C45` | #8B5C45 |  |
| colors | `col.cream` | `#F5E9D0` | #F5E9D0 |  |
| colors | `col.creamDark` | `#E3CDAA` | #E3CDAA |  |
| colors | `col.dim` | `#12131A` | #12131A |  |
| colors | `col.hpFill` | `#FD4840` | #FD4840 |  |
| colors | `col.shFill` | `#5875F2` | #5875F2 |  |
| colors | `col.expFill` | `#35A6E1` | #35A6E1 |  |
| colors | `col.goldFill` | `#F3A80E` | #F3A80E |  |
| colors | `col.slate` | `#415760` | #415760 |  |
