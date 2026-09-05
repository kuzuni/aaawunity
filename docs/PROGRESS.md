# PROGRESS — 꼬마기사 키우기 유니티 이식 (aaawunity)

> 갱신 규약은 `docs/ROUTINE.md` §4. 스펙은 aaaw `PLAN.md`(읽기 전용 · 변경 금지). 수치 정본은 aaaw `data/*.json`.

## 작업 상태

| ID | 작업 | 상태 | SID / 워커 | 범위 | 핵심 |
|---|---|---|---|---|---|
| T1 | 프로젝트 뼈대 + JSON 로더 + CI/활성화 워크플로 + README + 운영 문서 | ✅ 완료 (`5228daf` + 주인 «기본» `fe944b3` 합류) | sess-1516-port / 착수 세션 | 전체 뼈대 | dotnet build 0 경고 0 오류 · 순수 C# 테스트 21/21 · 레이아웃/적 스탯 420챕터 전수 = JSON 과 일치 (mulberry32 비트 동일) |
| T2 | 전투 엔진(순수 C#) + 시드 11·12·13 이식 검증 | ✅ 완료 | sess-1516-port / 착수 세션 | Core/Battle*·Perks*·tools/sim | sim.js 실험1 사다리 7점 × 3시드 **21칸 전부 소수점까지 동일**(난수 스트림 비트 일치) · 3pick 모드도 동일 (아래 표) |
| T3 | 레벨업 3택 + 악마의 거래 (유니티 팝업) + 전투 화면 | ✅ 완료 (코드·에셋 배선 — 실물 확인은 WebGL 배포에서) | sess-1516-port / 착수 세션 | Game/BattleScreen·BattleWorld·Overlay·UiKit·Palette·Screens·App · Assets/KkomaKnight(catalog) | 주인 지정 GUI Pro 데모 프리팹으로 팝업 6종 · CharacterMaker/Environment/CFXR 로 전투 월드 · 팝업 중 시간 정지 |
| T4 | 로비 · 장비 · 강화 · 슬롯 · 뽑기 상자 3종 | ✅ 완료 (코드·에셋 배선 — 실물 확인은 WebGL 배포에서) | sess-1516-port / 착수 세션 | Game/GearScreen·GearUi·ForgeScreen·ShopScreen·Screens(로비) | 자동 장착 없음 · 상자 3종(gacha.json) · 세부 팝업 = Character_Hero_Item_Detail_01 · 결과 = Shop_Chest_Open |
| T5 | UI 를 docs/ref 레이아웃에 맞추기 | 미착수 (T3·T4 뒤) | — | Game/Layout* | ref-layout.md ±3%p |

### T1 완료 기록 (2026-09-05 · 착수 세션)

- **주인이 확인할 것 (한 줄)**: Actions 탭에서 CI 가 시크릿 없이 초록인지 + README «내가 할 일» 절차대로 `.alf` 워크플로를 한 번 돌려 볼 것 + 유니티에서 프로젝트를 열어 `SampleScene` 재생 시 «데이터 로드 완료 · 챕터 420 · 특전 100» 이 뜨는지.
- 만든 것
  - `ProjectSettings/`·`Packages/` = 주인 «기본» 커밋(6000.3.8f1 · URP 2D · Input System) 그대로 + 세로 고정·제품명·패키지명·WebGL 압축 끔만 패치. `Assets/Scenes/SampleScene.unity` 에 `Bootstrap` 추가.
  - `Assets/Scripts/Core`(순수 C# · `noEngineReferences`): `MiniJson`(파서) · `GameData`(7개 JSON 타입 로더) · `Rng`(mulberry32 이식 · `IRng`) · `ChapterLayout`(sim.js `chapterLayout`/`enemyStats` 이식 — JSON 대조용) · `GearSystem`(buildPower · gachaPull · fuseMake/fuseAll · autoEquip).
  - `Assets/Scripts/Game`: `DataLoader`(StreamingAssets · WebGL/Android 는 UnityWebRequest) · `Bootstrap` · `UiKit`.
  - `Assets/StreamingAssets/data/*.json` = aaaw main `c7ebe37`(data blob `6e13114`) 복사본. `Assets/Fonts/Jua-Regular.ttf`(OFL).
  - 테스트 21개(EditMode · 유니티 없이도 `dotnet test` 로 돈다) + PlayMode 1개(StreamingAssets 실제 로드).
  - `tools/dotnet/`(sln + Core/Game/Tests/Sim csproj · UnityEngine 참조 어셈블리 = NuGet `UnityEngine.Modules 2021.3.33` + `Unity3D.UnityEngine.UI 2020.3.21`) · `tools/gen_meta.py` · `tools/check_data_sync.sh`.
  - `.github/workflows/activation.yml`(.alf → Artifact) · `ci.yml`(dotnet 항상 · 유니티 단계는 시크릿 게이트 · main push 시 WebGL→gh-pages + Android APK).
  - `docs/ROUTINE.md`(T2~T5 등재) · `docs/claims/README.md` · 이 문서.
- 게이트: `dotnet build` 0/0 · `dotnet test` 21/21 · `gen_meta --check` 초록 · `check_data_sync.sh` OK(aaaw c7ebe37).

### T2 완료 기록 (2026-09-05 · 착수 세션)

- **주인이 확인할 것 (한 줄)**: `dotnet run --project tools/dotnet/Sim -c Release -- --seeds 11,12,13 --n 1000` 의 표가 aaaw 의 `SEED=11 EXP1_N=1000 node sim.js 1`(12·13 도) 결과와 같은지 — 아래 표가 그 대조다.
- 만든 것: `Assets/Scripts/Core/Battle.cs`(엔진 · sim.js `runChapter` 이식 · 결정은 `IBattlePolicy` · 팝업 중 `Pending` 으로 시간 정지) · `BattleTypes.cs`(상태·정책·이벤트·`EngineConst`) · `Perks.cs`(적용·3택 제시·악마 1장·시뮬 정책) · `tools/sim/Program.cs`(실험1 재현 하니스 + 트레이스) · `BattleTests.cs` 9개(황금값 = sim.js 실측).
- **이식 검증 (실험1 · 사다리 7점 · 각 1,000판 · `LADDER_OPTS` = base10·legacy20·gearOpts 끔)** — 표의 값은 C# 이고, sim.js 가 같은 시드로 낸 값과 **21칸 전부 같다**(같은 mulberry32 수열을 같은 순서로 소비하므로 판 단위로 결과가 일치한다 — 챕터 3·7·15·30·60·100·125 에서 100~200판씩 판별 트레이스 대조: 클리어·시간·레벨·공격 수·빗맞음·특전 순서 전부 동일. 골드만 `Math.pow` 마지막 자리 차이로 1 어긋나는 판이 있다).

| 조건 | 챕터 | seed 11 | seed 12 | seed 13 |
|---|---|---|---|---|
| 노템(장비0·슬롯0) | 3 | 11.1% | 12.6% | 12.1% |
| 일반 풀셋(슬롯0) | 7 | 10.0% | 10.1% | 9.7% |
| 희귀 풀셋·슬롯5 | 15 | 10.8% | 9.6% | 10.2% |
| 전설 풀셋·슬롯15 | 30 | 10.5% | 9.7% | 10.6% |
| 신화 풀셋·슬롯25 | 60 | 10.4% | 8.5% | 9.5% |
| 신화+9강 풀셋·슬롯50 | 100 | 8.8% | 10.9% | 9.9% |
| 신화+9강 풀셋·슬롯100 | 125 | 10.2% | 11.5% | 9.9% |

  3택 모드(`EXP1_PERKMODE=3pick`) 대조표는 아래 «3pick» 표(같은 방식 · 21칸 동일).

**3pick 모드 (`EXP1_PERKMODE=3pick` · 각 1,000판 · 3택은 시뮬 정책 «표 순서 앞선 것»)** — sim.js 와 21칸 동일.

| 조건 | 챕터 | seed 11 | seed 12 | seed 13 |
|---|---|---|---|---|
| 노템(장비0·슬롯0) | 3 | 66.9% | 68.2% | 67.9% |
| 일반 풀셋(슬롯0) | 7 | 58.0% | 54.8% | 58.9% |
| 희귀 풀셋·슬롯5 | 15 | 73.7% | 71.9% | 74.1% |
| 전설 풀셋·슬롯15 | 30 | 76.4% | 75.8% | 76.3% |
| 신화 풀셋·슬롯25 | 60 | 82.7% | 80.5% | 81.6% |
| 신화+9강 풀셋·슬롯50 | 100 | 70.6% | 72.7% | 71.9% |
| 신화+9강 풀셋·슬롯100 | 125 | 78.0% | 80.0% | 79.2% |

- 잡은 코드 차이 2건(수치 아님): ⓐ `perks.json` 의 «방어력 증가 I/II/III» 은 탐침 방어가 0 이라 stat 이 비어 있다 → 상수 `PERK_DEF_*` 곱연산을 코드에서 복원. ⓑ «회복 증폭» 은 탐침 축에 `healAmp` 가 없어 stat 이 비어 있다 → `PERK_AMP` 가산 복원. 둘 다 aaaw 수출기(`tools/exportData.js` PROBE_STATS) 보강 제안으로 승인 대기 10번.
- 게이트: `dotnet build` 0/0 · `dotnet test` 30/30 · `gen_meta --check` 초록.

### T3 완료 기록 (2026-09-05 · 착수 세션)

- **주인이 확인할 것 (한 줄)**: WebGL 배포에서 «START → 전투가 돌고, 레벨업 때 특전 3장(초록/파랑/노랑 카드)이 뜨며 고르기 전엔 시간이 멈추는가 · 쉼터/악마/천사 노드에서 팝업이 뜨는가 · 보스를 잡으면 승리 팝업이 뜨는가».
- 만든 것
  - `Game/BattleScreen.cs`: 엔진을 1/30초 고정 틱으로 돌린다(sim.js dt). 팝업(Overlay)·일시정지 중엔 틱 없음. HUD 는 ref-layout ② 자(`Layout`)에 GUI Pro 부품(ResourceBar_Group · Slider_02 · Button_Pause/Info · BasicFrame 반투명 패널 · BuffSlot)을 앵커링. 스탯 8칸은 index.html `STAT_DEFS` 순서·표기 그대로(초록 = 시작값보다 오름). 배속 x1/x2. 클리어 = index.html `openClear`(보너스 `goldClear` · maxChapter/selChapter 갱신) · 사망 = `openDead`(골드 은행).
  - `Game/BattleWorld.cs`: 월드 좌표 = `(worldX − 플레이어x)×zoom + playerX·540`(ui.json camera) → `WorldCam.ToWorld`. 캐릭터 = CharacterMaker `Character.prefab` + `CharacterRig`(파츠 스킨 cm.* · Idle/Walk/Attack/Stun/Dead1/Victory 애니 · AllIn1 HitFlash 0.1초). 지면 = Environment Field/Road 타일 스크롤 · 소품(나무·덤불·돌·버섯) 시드 배치 · 노드 = 쉼터(통+CFXR Fire+버섯) · 악마(돌기둥+죽은 나무+Souls Escape) · 천사(큰 돌+LightGlow). 투사체 = 도끼(포물선 `axeArc`)·화살·창 스프라이트 + Wind Trails · 검기 = Sword Trail. 데미지 팝 = 프레임 층 Text + DOTween(색은 ui.json popHp/popShield · 등급색).
  - `Game/Overlay.cs`: 레벨업 3택 = **Play_Perk_Selection_02**(주인 지정 · 카드 3행 · 등급색 CardFrame_04/ItemFrame_04 Green/Blue/Yellow · 상단 스탯 8칸 · «보유 특전 N») · 보유 특전(PERKS 스크롤) · 쉼터(Popup Green · 체력 회복/경험치) · 악마(Popup Plum · 전설 카드 Yellow · 최대체력 % 차감 문구 · 수락/거절 → 악마의 선물) · 천사(Popup Yellow · 무료 +5% / 광고 3초 카운트다운 +15%) · 승리 = **Play_Result_Win_01**(주인 지정 · 골드/처치/시간 · 다음 챕터/로비로) · 사망 = Play_Result_Lose(팁 3행) · 일시정지 = **Settings**(주인 지정 · 소리 토글=Muted · 재개 · 포기하고 로비로) · 보스 경고 = Play_Warning_Boss 의 Panel_Warning 띠(시간 안 멈춤).
  - `Game/Screens.cs`: 로비 = **Lobby_Default**(주인 지정) 뼈대 — 챕터 ◀▶ · START · 하단 탭 5칸(상점·장비·전투·대장간·설정) · 전투력. 장비/대장간/상점은 T4 자리표시(«4단계에서 채워집니다»).
  - `Game/UiKit.cs`: 캔버스를 **1080×2337**(9:19.5) 로 — GUI Pro 데모 프리팹이 1080 폭 캔버스용이라 그대로 들어간다. `Spawn`(카탈로그 프리팹 인스턴스) → `Adopt`: **TMP → legacy Text(Jua) 변환**(GUI Pro SDF 폰트에 한글이 없음 · 크기/색/정렬 유지) · LayerLab 데모 스크립트 제거. GUI Pro 버튼 프리팹엔 Button 컴포넌트가 없어 `Clickable` 이 붙인다(DOTween 눌림).
  - `Assets/KkomaKnight/catalog.json` → `tools/gen_catalog.py` → `AssetCatalog.asset`(292 항목) + **`docs/assets-map.md`**(무엇을 어디에 썼는지 표). 씬 `Bootstrap.catalog` 에 연결. 프리팹 변형(variant)의 루트 fileID = 베이스 루트 ⊕ 인스턴스 ID(하위 63비트) 규칙을 3표본으로 검증해 생성기에 넣었다.
  - `Assets/KkomaKnight/HitFlash.mat`: AllIn1 URP2D 셰이더 · `HITEFFECT_ON`.
- 게이트: `dotnet build` 0/0 · `dotnet test` 32/32 · `gen_meta --check` 초록 · 유니티 CI EditMode 32 + PlayMode 1 통과(#12).
- 한계(정직하게): 유니티 에디터가 없어 **화면을 직접 보지 못했다**. 프리팹 자식 이름 경로는 YAML 덤프로 확인했지만 변형 프리팹의 인스턴스 이름은 두 후보(`FindAny`)로 잡았다. 첫 WebGL 배포에서 어긋난 것이 있으면 다음 세션이 고친다.
- CI 수리 2건(주인 요청): #11 = 유니티 NUnit(3.5 포크)에 `Does.Contain(object)` 가 없어 EditMode 컴파일 오류 → `Has.No.Member` · dotnet 검사도 NUnit 3.6.1 로 고정(3.5.0 은 net8 에서 테스트 발견이 안 됨 · 3.6.1 이 Does.Contain(object) 를 거부하면서 32개를 찾는 최저 버전 — 로컬 재현 확인). #12 = 테스트 33/33 통과 뒤 unity-test-runner 의 체크 런 게시가 «Resource not accessible by integration»(토큰에 checks:write 없음) → 게시 옵션 제거. **라이선스는 두 번 다 정상 활성화됐다.**

### T4 완료 기록 (2026-09-05 · 착수 세션)

- **주인이 확인할 것 (한 줄)**: WebGL 에서 «상점 → 무료 보급 수령 → 희귀 상자 10회 → 결과 팝업(열린 상자 + 장비 격자) → 장비 탭에서 칸을 눌러 세부 팝업 → 장착 → 슬롯 6칸/공·체·실 숫자가 바뀌고, 같은 장비 3개면 대장간에서 합성(자동/수동)이 되는가».
- 만든 것
  - `Game/GearScreen.cs`: **Character_Hero_Equipment**(주인 지정) — 슬롯 6칸(격자 행 우선 = 왼쪽열 무기·목걸이·갑옷 / 오른쪽열 투구·장갑·신발 = index.html GEAR_COL) · 등급색 ItemFrame_01_Normal_* · 세트 다이아 아이콘 · «↑ 더 좋은 게 있다» · 공/체/실 3칸(`BuildPower`) · 균등 보너스 문구(`EvenBonus`·evenPer·slotLvMax 에서) · 전투력 · 인벤 격자(장착분 먼저 → 등급·강화 내림차순 · NEW · 합성가능 «3») · 하단 합성(N)/상점 · 뒤로. `NavBar` = 하단 탭 5칸을 모든 화면에 같은 배선으로.
  - `Game/GearUi.cs`: 장비 칸 공통(장비 탭·대장간·뽑기 결과가 같은 함수) · **세부 팝업 = Character_Hero_Item_Detail_01**(주인 지정 · 등급 배지 색 · 이름/부위/세트/슬롯 Lv · 슬롯 Lv 바 · 기여 3수치 · 세트 옵션 n/7 + 잠금 조건(등급 이상 / 신화 +3·6·9 — `rarName.length` 경계) · 장착/해제/슬롯 강화(비용 `slotCost`) · 빈 슬롯 팝업(강화만).
  - `Game/ForgeScreen.cs`: ref-layout ⑥ — 재료 3칸 → 결과 미리보기(`FuseMake` 하나만) · 전설→신화 변환 안내(`legendToMythPlus`) · 자동(`FuseAll` · 장착분 제외) · 수동 합성 · 같은 키만 선택 가능(장착분·다른 키 흐리게) · 토스트 문구는 index.html 과 동일.
  - `Game/ShopScreen.cs`: ref-layout ⑤ — 무료 보급(일 1회 · `economy.dailyGem`) · 모의 결제 1종(`iapGem` · ₩110,000 표시 전용) + 잠금 5칸 · 상자 3종 카드(**ListItem_ShopChest** · 상자 그림 = Chest_01 Silver/Gold/Premium · 확률 문구는 rate 에서(0% 등급은 안 적음) · 천장 문구 «신화 확정까지 N회 / 전설 확정까지 N회 / 누적») · 1회/10회(`tenPull.count`) · **결과 팝업 = Shop_Chest_Open**(주인 지정 · 열린 상자 + 얻은 장비 격자 · 최고 등급 · 한 회차 2개 가능 «N개») · 자동 장착 없음.
  - 세이브: 뽑기·합성·장착·슬롯·무료 보급이 즉시 `PlayerPrefs`(SaveStore) 에 기록 — 필드는 index.html `kkoma-knight-v2` 와 같다(T3 SaveData).
- 게이트: `dotnet build` 0/0 · `dotnet test` 32/32 · `gen_meta --check` · `gen_catalog --check`(294) 초록.
- 한계: 장비 탭의 캐릭터 자리는 GUI Pro 샘플 캐릭터를 숨기고 아이콘(UI_Play_Battle)을 두었다 — CharacterMaker 기사를 UI 에 그리려면 RenderTexture 카메라가 필요해 T5 후보로 남긴다(승인 대기 17).

## 주인 결정 (2026-09-05 답변 — 승인 대기 1~9 종결)

- **1 유니티 버전**: 6000.3.8f1 그대로. · **2 브랜치**: `main` 만 — `claude/…` 브랜치는 더 올리지 않는다. · **4~9**: 제안한 기본값대로.
- **3 에셋(플레이스홀더 금지 · 처음부터 주인 에셋)** — 용도별 기준:
  - UI 전부: `Layer Lab/GUI Pro-MinimalGame` **Theme_Light** (버튼·패널·팝업·탭·바·아이콘). 등급 색은 이 테마 색 중에서.
  - 플레이어·적·보스: `Layer Lab/2D Minimal-CharacterMaker` Character 프리팹 + `_Controller` 애니(Idle/Walk/Attack/Stun/Dead…). 파츠 조합으로 구분.
  - 배경·노드(쉼터/악마/천사): `Layer Lab/2D Minimal-Environment`.
  - 타격·치명·회피·소환(도끼/화살/창/번개/검기)·레벨업 이펙트: `JMO Cartoon FX Remaster`.
  - 트윈·팝업 애니: DOTween. 스프라이트 강조(피격 플래시·아웃라인): AllIn1SpriteShader.
  - Odin·AntiCheat·Hot Reload·mcp-unity 는 안 쓴다.
- **(추가 · 2026-09-05)** GUI Pro 의 `Prefabs~DemoLayout`·`Prefabs~DemoScenes` 를 적절히 쓸 것. 화면 지정: **Character_Hero_Equipment = 장비 창 · Lobby_Default = 로비 · Play_Perk_Selection_02 = 특전 선택 · Play_Result_Win_01 = 승리 · Settings = 설정 팝업 · Shop_Chest_Open = 장비 소환(뽑기) 팝업**. 악마·천사·쉼터는 전용 에셋이 없으니 알아서(→ Environment 소품 + CFXR 조합 · assets-map 참조).
  - 에디터 없이 프리팹을 엮으려면 .meta 의 GUID 를 읽어 씬/프리팹 YAML 에 박는다. 어떤 프리팹·스프라이트·이펙트를 어디에 썼는지 **`docs/assets-map.md`** 에 표로 남기고, 단계마다 고른 것을 보고한다(주인이 바꿀 것만 말한다).

## 주인 승인 대기 (한 번에 답해 주시면 됩니다 — 답이 없으면 아래 «기본값» 으로 진행)

> 1~9 는 위 «주인 결정» 으로 종결됐다(이력으로 남긴다). 열린 것은 10번부터.


1. **유니티 버전 = 6000.3.8f1 (주인 «기본» 커밋 `fe944b3` 을 따름).** 지시는 «2022.3 LTS 최신 패치» 였고 처음엔 2022.3.76f1 로 뼈대를 짰으나, 같은 시각에 주인이 main 에 올린 «기본» 프로젝트가 **Unity 6000.3.8f1 + URP 2D + TextMeshPro + Input System + 에셋(Layer Lab GUI Pro/CharacterMaker/Environment · Cartoon FX · AllIn1SpriteShader · DOTween · Odin · AntiCheatToolkit · Hot Reload · mcp-unity)** 이라 두 트리가 양립하지 않았다(URP 17.3·ugui 2.0 은 2022.3 에 없다). **주인 프로젝트를 기준으로 합쳤다**: ProjectSettings/Packages/에셋은 주인 것 그대로, 이 세션의 코드·데이터·CI·문서를 그 위에 얹었다. GameCI 이미지 `unityci/editor:ubuntu-6000.3.8f1-*` 는 존재한다. **2022.3 으로 되돌리길 원하시면** 에셋 패키지가 전부 6000 전용이라 주인 프로젝트를 다시 만들어야 한다 — 한 줄로 알려 주시면 그때 정한다.
   - 주인 프로젝트에 손댄 것: 세로 고정(`defaultScreenOrientation 1` · 가로 자동회전 끔) · productName `KkomaKnight` · companyName `kuzuni` · Android 패키지명 `com.kuzuni.kkomaknight` · WebGL 압축 끔(Pages) · `SampleScene` 에 `Bootstrap` 오브젝트 1개 추가(주인의 Main Camera·Global Light 2D·EventSystem 은 그대로). 나머지 13,988 파일은 그대로다.
   - 주인 에셋(Layer Lab GUI Pro · CharacterMaker 등)을 UI/캐릭터에 **쓸지**는 별도 결정이 필요하다 — 에디터 없이 프리팹/스프라이트를 엮으려면 GUID·서브에셋 ID 를 .meta 에서 읽어 씬/코드에 박아야 한다(가능하지만 공수 큼). 기본값: 1~4단계는 코드 생성 도형(플레이스홀더)으로 가고, 5단계(레이아웃)에서 배치만 맞춘다. 에셋을 입히는 것은 «주인이 지정한 프리팹/스프라이트 이름» 을 받은 뒤 별도 작업으로.
2. **브랜치.** 주인 지시는 «각 단계마다 main 에 커밋·푸시», 세션 시스템 지시는 «`claude/aaawunity-port-71kij4` 브랜치에서 개발». 기본값: **둘 다** 푸시한다(같은 커밋). main 만 원하시면 한 줄로.
3. **활성화 워크플로.** `game-ci/unity-request-activation-file` 이 공식 폐기(deprecated)돼서, 같은 일(`unity-editor -createManualActivationFile`)을 `unityci/editor` 도커 이미지에서 직접 한다. 절차·결과물(.alf Artifact)은 같다.
4. **폰트.** PLAN §2.1 의 Jua(Google Fonts · OFL)를 `Assets/Fonts/Jua-Regular.ttf`(2.1MB) 로 커밋했다 — WebGL/Android 는 OS 한글 폰트를 못 쓰므로 필요하다. «대용량 바이너리 금지» 의 유일한 예외.
5. **Android 빌드 = IL2CPP · ARM64 · 서명 없는 개발용 APK.** 스토어 배포용 keystore 는 시크릿이 필요하니 그때 정한다.
6. **WebGL 압축 끔**(GitHub Pages 가 .br/.gz 헤더를 안 붙여 주므로). 로딩이 느리면 gzip+폴백으로 바꿀 수 있다.
7. **UnityEngine 참조 어셈블리 = NuGet 2021.3.33**(6000 용 패키지가 NuGet 에 없다). 이 게임이 쓰는 API(uGUI 레거시 Text·Image·UnityWebRequest)는 그대로 있다. Unity 6 에서 폐기 경고가 나는 API(`FindObjectOfType` 등)는 피한다. 유니티 실제 컴파일은 CI 의 EditMode 단계가 최종 확인한다.
8. **`expNeed` 표 밖 레벨**: tune.json `expNeedTable` 은 1~30레벨이라, 그 위는 표의 등차(+5)로 연장한다(공식 `5*lv+1` 을 코드에 박지 않으려는 기본값 · 실제로는 한 판 9레벨이 상한이라 닿지 않는다).
9. **enemies.json 의 보스 스탯은 실수 그대로**(`sim.js` 는 반올림 안 함 · `index.html` 은 반올림). 시드 검증 대상이 sim.js 이므로 엔진은 **반올림 안 함**을 따른다(체력바 표시만 정수).

10. **aaaw 수출기 보강 제안 (aaaw 는 읽기 전용이라 여기서 못 고친다).** `tools/exportData.js` 의 `probeEffect` 가 ⓐ 탐침 방어 0 이라 «방어력 증가»(곱연산)의 효과가 비어 나오고 ⓑ `PROBE_STATS` 에 `healAmp` 가 없어 «회복 증폭» 이 비어 나온다. C# 은 상수(`PERK_DEF_M/R/L`·`PERK_AMP`)로 복원했고 sim.js 와 판 단위로 일치한다. 수출기에 탐침 방어 기본치(예: 10)와 `healAmp` 축을 넣으면 코드 특수처리를 지울 수 있다.
11. **sim.js 의 이름 없는 리터럴 21개**(이벤트 발동 거리 95 · 보스 배치 +60 · 랜덤 타겟 범위 −30~540 · 투사체 생성/도달/적중 오프셋 14·10·16 · 적 화살 −18/+8/−60 · 화살 속도 560 · 검기 속도 470 · 적 첫 공격 0.4~1.2 · 보스 1.2 · 이동 중 타이머 상한 0.35 · 데미지 지터 0.92~1.08 · 골드 1~1.8 · 반격 0.7 · 풀피 판정 0.5 · 장비 c/f 옵션의 50%/30%/50%/10%)은 `combat.json` 에 없어 `Assets/Scripts/Core/BattleTypes.cs` 의 `EngineConst` 한 곳에 두었다(이 레포에서 유일한 코드 상수 자리). aaaw 수출기에 축이 추가되면 `CombatData` 로 옮긴다.

12. **캔버스 단위를 1080×2337 로 바꿨다**(GUI Pro 데모 프리팹 = 1080 폭 캔버스). 배치는 전부 프레임 % 라 ref-layout 표는 그대로 쓴다. 되돌릴 이유가 있으면 한 줄로.
13. **GUI Pro 글자(TMP·한글 없는 SDF 폰트)는 인스턴스화 때 legacy Text(Jua)로 바꿔 쓴다.** TMP 로 한글을 내려면 Jua SDF 폰트 에셋을 에디터에서 구워야 한다(에디터 없어 불가). 주인이 에디터로 `Jua SDF.asset` 을 만들어 커밋해 주시면 TMP 유지로 바꿀 수 있다.
14. **쉼터/악마/천사 노드 그림**은 전용 에셋이 없어 조합했다 — 쉼터 = Environment 통(Ork)+CFXR Fire+버섯 · 악마 = 돌기둥+죽은 나무+CFXR Souls Escape · 천사 = 큰 돌+CFXR LightGlow. 바꿀 것만 말해 주시면 catalog.json 한 줄로 바뀐다.
15. **PlayMode 테스트는 dotnet 검사가 컴파일하지 못한다**(UnityEngine.TestTools 가 NuGet 에 없음) — 유니티 CI 가 최종 확인한다. PlayMode 테스트를 늘릴 때는 이 점을 기억.
16. **하단 탭 5칸 = 상점·장비·전투·대장간·설정** 으로 정했다(원본 index.html 은 상점·장비·전투·카드🔒·도전🔒 — 카드/도전은 PLAN 에서 폐지된 자리라 이 레포에 있는 화면으로 채웠다). 원본대로 🔒 두 칸을 원하시면 한 줄로.

17. **장비 탭 캐릭터 그림**: 데모 프리팹의 샘플 캐릭터(Sample_Cha02_l)는 우리 기사가 아니라 숨기고 아이콘으로 대체했다. 기사(CharacterMaker)를 UI 안에 그리려면 RenderTexture 카메라 1개가 필요하다 — 원하시면 T5 에 넣는다.
18. **상점 모의 결제 가격 ₩110,000** 은 index.html GEM_PACKS 의 표시값을 그대로 옮겼다(실결제 없음 · PLAN §11.5). 가격대는 정하신 게 1종뿐이라 나머지 5칸은 잠금으로 채웠다.

## 주인 할 일

- README «내가(주인) 할 일» 5단계 (활성화 워크플로 → .alf → .ulf → 시크릿 3개 → Pages 소스 gh-pages).

## 게이트 현황 스냅샷 — T4 완료 직후

| 게이트 | 결과 |
|---|---|
| `dotnet build tools/dotnet/KkomaKnight.sln -c Release` | 0 경고 · 0 오류 (Core · Game · Tests · Sim) |
| `dotnet test tools/dotnet/Tests` | 32/32 (NUnit 3.6.1 — 유니티 포크와 같은 API 면 · 3.5.0 은 net8 어댑터가 테스트를 못 찾는다) |
| 유니티 CI (#12) | EditMode 32/32 · PlayMode 1/1 |
| `python3 tools/gen_meta.py --check` | 초록 |
| `tools/check_data_sync.sh` | OK — aaaw main `c7ebe37` 과 동일 (`sim.js@0618225…`) |
