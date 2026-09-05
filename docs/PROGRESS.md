# PROGRESS — 꼬마기사 키우기 유니티 이식 (aaawunity)

> 갱신 규약은 `docs/ROUTINE.md` §4. 스펙은 aaaw `PLAN.md`(읽기 전용 · 변경 금지). 수치 정본은 aaaw `data/*.json`.

## 작업 상태

| ID | 작업 | 상태 | SID / 워커 | 범위 | 핵심 |
|---|---|---|---|---|---|
| T1 | 프로젝트 뼈대 + JSON 로더 + CI/활성화 워크플로 + README + 운영 문서 | ✅ 완료 (`5228daf` + 주인 «기본» `fe944b3` 합류) | sess-1516-port / 착수 세션 | 전체 뼈대 | dotnet build 0 경고 0 오류 · 순수 C# 테스트 21/21 · 레이아웃/적 스탯 420챕터 전수 = JSON 과 일치 (mulberry32 비트 동일) |
| T2 | 전투 엔진(순수 C#) + 시드 11·12·13 이식 검증 | ✅ 완료 | sess-1516-port / 착수 세션 | Core/Battle*·Perks*·tools/sim | sim.js 실험1 사다리 7점 × 3시드 **21칸 전부 소수점까지 동일**(난수 스트림 비트 일치) · 3pick 모드도 동일 (아래 표) |
| T3 | 레벨업 3택 + 악마의 거래 (유니티 팝업) + 전투 화면 | ✅ 완료 (코드·에셋 배선 — 실물 확인은 WebGL 배포에서) | sess-1516-port / 착수 세션 | Game/BattleScreen·BattleWorld·Overlay·UiKit·Palette·Screens·App · Assets/KkomaKnight(catalog) | 주인 지정 GUI Pro 데모 프리팹으로 팝업 6종 · CharacterMaker/Environment/CFXR 로 전투 월드 · 팝업 중 시간 정지 |
| T4 | 로비 · 장비 · 강화 · 슬롯 · 뽑기 상자 3종 | ✅ 완료 (코드·에셋 배선 — 실물 확인은 WebGL 배포에서) | sess-1516-port / 착수 세션 | Game/GearScreen·GearUi·ForgeScreen·ShopScreen·Screens(로비) | 자동 장착 없음 · 상자 3종(gacha.json) · 세부 팝업 = Character_Hero_Item_Detail_01 · 결과 = Shop_Chest_Open |
| T5 | UI 를 docs/ref 레이아웃에 맞추기 | ✅ 완료 (배치 상수 = 표 · 실물 확인은 WebGL 배포에서) | sess-1516-port / 착수 세션 | Core/Layout · Game/* 배치 · Tests/LayoutSpecTests | ref-layout ①~⑦ 표를 `Layout` 상수로 · 표 ↔ 상수 자동 대조 테스트 8개 · 60fps · 백그라운드 실행 · 주인 피드백 일괄(특전 카드/색/새로고침 · 공격 모션 · 발밑 체력바 · 간격 2배 · 맵 4종 순환) |
| T6 | 로비 = Lobby_Default 그대로 (TopBar 폐기 · 캐릭터·전투력·골드·보석) | ✅ 완료 (`814d59d` · 실물 확인은 WebGL 배포에서) | sess-2034-9487 / 워커 B | Game/Screens(로비) · HeroView(신규) · TopBar 삭제 | 프리팹 요소 이동 0 · 초상 = HeroView(RenderTexture) · «25/55» = 전투력 · dotnet 0/0 · 테스트 40/40 |
| T7 | 장비 화면 = Character_Hero_Equipment 그대로 + 장착 외형 반영 + ListItem_EquipMent | ✅ 완료 (`ff53ebb` · 실물 확인은 WebGL 배포에서) | sess-2036-27996 / 워커 C | Core/GearLook(신규) · Game/GearScreen · GearUi · CharacterRig · BattleWorld(스킨) · HeroView(T6 것 재사용) · Tests/GearLookTests | 프리팹 요소 이동 0 · 슬롯 Item 크기 그대로 · 파츠 36종 표 · dotnet 0/0 · 테스트 45/45 |
| T8 | 대장간 정리 (인벤 전부 · 빨간 점 · 칸 비례) — T7 뒤 | ✅ 완료 (`41e524c` · 실물 확인은 WebGL/에디터에서) | sess-2113-28861 / 워커 A | Game/ForgeScreen · GearUi(Grid) · catalog(`ui.alertDot` 노트) | 인벤 격자 = 장비 화면 프리팹 격자 값 복사(188 정사각 · 5열 · 찌그러짐 0) · 재료 3칸도 본래 크기 · 빨간 점 · 장착중 Check+흐림 · 인벤 누락 원인 = 가짜 null 예외로 Refresh 중단 · dotnet 0/0 · 테스트 45/45 |
| T9 | 상점 = Shop_List 그대로 (상자 3 · 다이아 6 · 골드 3) + 뽑기 결과 = Shop_Chest_Open | ✅ 완료 (`4506ed4` · 실물 확인은 WebGL/에디터에서) | sess-2136-22274 / 워커 C | Game/ShopScreen · Core/GameData(ShopData) · Game/AssetCatalog(texts) · Bootstrap · KkomaKnight/shop.json(신규) · catalog(ui.shopList·ui.shopItem·data.shop) · tools/gen_catalog·check_catalog_keys · Tests/ShopDataTests | 프리팹 요소 이동 0 · ShopPackage ×3 = 상자(1회/10회 · 등급 확률 3칸 · 천장 배지) · ShopItem ×(2+6+3) = 무료 보급·다이아·골드 · 소탭 3칸 스크롤 · 결과 격자 = ListItem_EquipMent · dotnet 0/0 · 테스트 50/50 |
| T10 | 하단 네비 = 상점·장비·전투·탤런트·펫 + Settings 그대로 — T6 뒤 | ✅ 완료 (`dce33d6` · 실물 확인은 WebGL 배포에서) | sess-2052-15499 / 워커 D | Game/Screens(NavBar 이사) · Overlay(Settings·TalentPet) · GearScreen(NavBar 제거) · catalog(ui.talent·ui.talentIcon·ui.petIcon) | 탭 = 상점·장비·전투·탤런트·펫 · Settings 프리팹 요소 숨김 0 · Character_Talent_02 통째로 · dotnet 0/0 · 테스트 45/45 |
| T11 | UI 스모크 PlayMode 테스트 + 가짜 null 게이트 | ✅ 완료 (`bddcf98` + `ab9b192` + `bb196d1` · 실물 확인 = CI 런 #38 유니티 잡 초록 — PlayMode 전부 통과 · UiSmokeTests 5/5 · HeroViewTests 2/2) | sess-2150-31726 / 워커 D | Tests/PlayMode/UiSmokeTests·PlayLog(신규) · HeroViewTests(검사 도우미 교체) · tools/check_unity_null.sh(신규) · ci.yml(게이트 1줄) · ROUTINE §1·§3 | 화면 5 + 팝업 17종을 실제 씬에서 열어 화면마다 빨간 줄 0·경로/키 경고 0·데모 잔여 글자 0·핵심 요소(슬롯 6·상자 3·탭 5) · 전투 3초 틱 · 가짜 null 패턴 0건 게이트(CI) · **CI #33 회귀 원인 확정·수정**(NoUnexpectedReceived 가 Debug.Log 도 실패로 봄) · dotnet 0/0 · 테스트 50/50 |
| T12 | **플레이 콘솔 에러 0** — URP 2D 렌더 에러(HeroView RenderTexture 깊이 0) 수정 + 전 화면 런타임 에러 전수 감사 (최우선) | ✅ 완료 (`2203550` · 실물 확인 = CI PlayMode HeroViewTests + 주인 에디터 플레이) | sess-2121-23849 / 워커 B | Game/HeroView · Game.asmdef(URP 참조) · Tests/PlayMode/HeroViewTests(신규) · tools/check_catalog_keys.py(신규) · ci.yml(게이트 1줄) · dotnet Stubs/URP.cs | RenderTexture 깊이 24·스텐실 8 · URP Base 카메라 데이터 명시 · 파괴 순서 · PlayMode 2개(단독 렌더 · 씬 로비→장비→전투→로비 왕복) · 카탈로그 키 552개 전부 실재 · dotnet 0/0 · 테스트 45/45 |
| T13 | 전투 HUD 특전 미리보기 줄(PerkStrip) 비례 — 아이콘이 서로 가림 · index.html 34/28/4px 비례로 · 넘침 0 · 스크린샷 아티팩트 | ✅ 완료 (`50860f2` · **CI 런 #38 유니티 잡 초록** — PlayMode `PerkStripTests` 통과 · 아티팩트 `perkstrip-screens`(PNG 2장) 업로드 확인 · 최종은 주인 에디터) | sess-2206-21029 / 워커 A | Core/Layout(`PerkStripSpec` 순수 계산 · 표값 불변) · Game/BattleScreen(RefreshPerkStrip) · Game/UiKit(PerkFrame 배율) · Tests/EditMode/PerkStripSpecTests(신규 5) · Tests/PlayMode/PerkStripTests(신규 1) · ci.yml(아티팩트 1단계) · .gitignore | **원인 확정 = 프리팹 내부 고정 크기**(ItemFrame_04 자식 Border 162·Icon 128 이 가운데 앵커라 sizeDelta 축소가 안 먹음 → 78px 셀에 162px 프레임) → 배율로 · 셀 = 줄 높이 28/34 · 간격 4/34 · 배지 14/34 · «+N» 12/34 · 개수 = 폭 ÷ 피치(상수 11 폐기 · 넘침 0) · dotnet 0/0 · 테스트 55/55 · 시뮬 21칸 동일 |
| T14 | 전투 캐릭터 크기 2/3 · 공속 비례 공격 애니 속도(상한 없음) · 사망/승리 모션 루프 금지 | ✅ 완료 (`0ee1e18` · 실물 확인 = CI PlayMode `CharacterRigTests` 3개 + 주인 에디터 플레이) | sess-2220-32398 / 워커 B | Core/Layout(`CharScale`·`CharHeightPct`·`AttackAnimSpeed` · 표값 불변) · Game/BattleWorld(키·발밑 바 폭 배율) · Game/CharacterRig(공격 속도 · 사망/승리/패배 클립 끝 정지) · Tests/EditMode/CharScaleAnimTests(신규 4) · Tests/PlayMode/CharacterRigTests(신규 3) | 키 = 표 % × 2/3(플레이어·적·보스 ×sizeMul) · 바 폭 × 2/3 · 공격 속도 = 클립 ÷ 간격(하한 1 · 상한 ×3 폐기) · Dead1/Victory/Defeat(루프 에셋 불변)는 Animator 자기 시계로 마지막 프레임(0.999)에서 speed 0 · dotnet 0/0 · 테스트 59/59 · 시뮬 21칸 동일 |
| T15 | **플레이 콘솔 에러 0** — 데모 프리팹 스폰 시 `PanelView.OnEnable` 예외(UnassignedReferenceException · `UiKit.Spawn` 의 Instantiate 중 · 설정/장비 세부/전투 팝업) — CI #36 PlayMode 3건 실패 원인 | 🔄 코드 완료 · CI 확인 중 (`b001d5f` · 이 세션이 유니티 잡 결과를 읽어 ✅ 로 바꾼다) | sess-2136-22274 / 워커 C | Game/UiKit(Spawn·Staging·StripDemoScripts) | Instantiate 를 비활성 홀더 밑에서 → 데모 스크립트 제거 → parent 이동(OnEnable 0회) · dotnet 0/0 · 테스트 55/55 |

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

### T5 완료 기록 (2026-09-05 · 착수 세션)

- **주인이 확인할 것 (한 줄)**: WebGL(gh-pages) 에서 «전투 진입 → 챕터 1 은 가을(Autumn) · 2 깊은숲 · 3 숲 · 4 사막 맵인가 / 적끼리·쉼터·악마·천사 간격이 전보다 2배로 벌어졌나 / 공격 모션이 끝까지 나오고 칼이 내려오는 순간에 숫자·체력바가 깎이나 / 체력바가 발밑·실드바(파랑)가 그 아래인가 / 레벨업 카드에 «일반·희귀·전설» 리본 + 설명만(회색·노랑·빨강) · 주황 버튼이 새로고침(1회) · 오른쪽 아래 책 버튼이 보유 특전인가 / HUD 왼쪽 아래 특전 아이콘이 팔각 프레임인가 / 적이 전부 투구를 썼나».
- 만든 것
  - `Core/Layout.cs`(Game 에서 Core 로 이동): ref-layout ①~⑧ 의 모든 요소를 표 그대로 상수로(로비·인게임·장비·세부·상점·대장간·특전·공통). `R.Within(outer)` 로 팝업 안 상대 %.
  - `Tests/EditMode/LayoutSpecTests.cs` + `docs/ref-layout.md`(aaaw 표 사본): 표를 파싱해 상수와 0.05 안에서 대조하는 테스트 8개(총 40).
  - 모든 화면을 표 자리에 다시 앵커링: 로비(`TopBar` 공통 아바타+재화 · 배너 · 카드 · 시작 · 탭바) · 인게임 HUD(바 3개 · 스탯 8칸 · 배속 · 라운드) · 장비/세부 · 상점 · 대장간(⑥ 상단바 없음 · 뒤로) · 특전 3택(⑦ 배너·부제·카드 피치 13·하단 버튼·인포) · 보유 특전 · 이벤트 상자.
  - `Bootstrap`: vSync 끔 + `targetFrameRate 60`(WebGL 은 브라우저 rAF) · `runInBackground` · 화면 꺼짐 방지 (주인 «60fps · 백그라운드»).
  - **주인 피드백 일괄(스크린샷)**
    - 특전 카드(`Overlay.PerkCard`): 이름 없이 등급 리본 + 설명만 · 색 일반=회색 · 희귀=노랑 · 전설=빨강(`Palette.PerkGradeName` · CardFrame/ItemFrame 에 Gray 변형이 없어 Green 을 `UiKit.Desaturate` 로 무채색화).
    - 특전 선택의 주황 버튼 = **새로고침**(`BattleState.RerollOffer` · 같은 `Perks.Offer` 굴림 · 팝업당 `EngineConst.RerollPerLevelUp = 1`) · 보유 특전은 Book 아이콘으로.
    - HUD 오른쪽 아래 인포 = 책 모양(`ui.bookBlue` + 개수) · 왼쪽 아래 획득 특전 = 팔각 `ItemFrame_04_*`(`UiKit.PerkFrame`).
    - 공격 모션: `CharacterRig.PlayAttack(interval)` 이 Attack.anim(1.83초)을 끊지 않고 끝까지 돌린다(간격보다 길면 최대 ×3 배속) · 타격 연출(팝·플래시·체력바·사망)은 클립의 OnAttackHit(1.0초) 순간까지 `BattleWorld.Strike` 큐에 묶어 두고 그때 푼다(엔진 판정은 그대로 · 표시 체력 `ShownHp/ShownSh` 만 늦게 깎임 · 팝업(레벨업/사망)도 연출이 끝난 뒤 연다).
    - 적은 전부 투구(맨머리 B 스킨에 `cm.meleeB.helmet` 추가) · 원거리 적은 활+화살+시위.
    - 체력바는 발밑(`Layout.FootHpBarY`) · 플레이어 실드바(파랑)는 그 아래(`FootShBarY`).
    - 적·쉼터·악마·천사 간격 2배: 그리기 배율 `Layout.WorldSpacing = 2`(멈춤 거리 74 안쪽은 1배 · 150px 램프로 부드럽게) — 엔진 좌표(enemyGap 44 · nodeGap 280 · nodeGapEvent 470)와 시드 골든은 그대로(승인 대기 22).
    - 전투 맵 4종 순환(`BattleWorld.Theme` · 주인 지정 DemoScene_Autumn/DeepForest/Forest/Desert): 챕터 (n−1)%4 → 가을·깊은숲·숲·사막. 데모 씬 구성 그대로 — 평면색 바닥(화면 전체) · 길 띠(35~47%) · 길 윗변 물결 경계(반 겹침) · 풀·꽃·둔덕 흩뿌림 · 나무(뒤)·덤불(뒤/앞)·돌·버섯(앞). 카탈로그 `env.<theme>.*` 68키(`docs/assets-map.md`).
- 게이트: `dotnet build` 0/0 · `dotnet test` 40/40 · `gen_meta --check` · `gen_catalog --check`(359) · `check_data_sync` OK.
- 한계: 배치·연출은 에디터 없이 짠 것이라 실물(WebGL) 확인이 필요하다 — 특히 물결 경계의 y·큰 나무 크기(1.6~2.1u 로 줄임 · 데모 비율이면 HUD 를 덮는다)·팔각 프레임 크기.

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


### 2026-09-05 답변 2 — 승인 대기 19~23 종결
- **19 백그라운드 실행 → 재확인: «새 시스템은 아니고, 컴퓨터로 유튜브 보면서 할 수 있게 백그라운드에서도 돌게».** `runInBackground = true`(창/탭이 포커스를 잃어도 계속 돈다 · 유튜브를 옆 창에 두고 볼 때). 브라우저가 탭을 숨기면(다른 탭으로 전환) rAF 자체가 멈춰 어떤 유니티 WebGL 도 못 도므로, 돌아올 때 멈춘 실제 시간(최대 10분)만큼 엔진 틱을 연출 없이 몰아서 따라잡는다(`BattleScreen` CatchUp · 레벨업/이벤트 팝업이 뜨면 거기서 멈춤 · 방치 보상 같은 새 시스템 아님). Android 백그라운드 복귀도 같은 길.
- **21 새로고침 → 무료 · 새 특전 팝업이 뜰 때마다 1번.** 구현과 같다(`EngineConst.RerollPerLevelUp = 1` · 팝업마다 `Pending.Rerolls` 초기화 · 비용 없음).
- **22 간격 2배 → 그리기 배율 유지(«냅둬»)** 였으나, 에디터에서 보고 «배경 소품·적의 이동 속도가 서로 달라 부자연스럽다 · 전이 나은데» → **배율 1 로 되돌림**(예전과 같은 균일 이동). 진짜 2배는 승인 대기 24.
- **23 맵 순서 → OK.** Autumn → DeepForest → Forest → Desert.

### 2026-09-05 답변 3 — 에디터 플레이 피드백
- **에디터 오류 `MissingComponentException (Button_02_Orange · CanvasGroup)`** → 원인은 `GetComponent() ?? AddComponent()` 패턴: 에디터의 «가짜 null»(== 만 재정의)에 `??` 가 걸리지 않아 컴포넌트가 안 붙었다. `UiKit.Ensure<T>` 로 전부 교체(6곳).
- **«배경 소품·적 이동 속도가 달라 부자연스럽다 · 전이 나은데»** → 간격 배율 1 로 되돌림(승인 대기 24 참조).
- **«배경은 내가 말한 씬 참고한 거 맞냐 · 그대로 복사해도 된다 · 배치가 맘에 든다»** → `tools/gen_maps.py` 가 DemoScene_Autumn/DeepForest/Forest/Desert 의 소품 인스턴스(위치·반전·크기)를 씬 파일에서 읽어 `MapLayouts.cs` 표로 굽고, `BattleWorld` 가 그 표를 씬 폭(≈27u)마다 반복해 그대로 깐다(길 중심 y −0.402 ↔ 프레임 41% · 1u = 100/zoom 월드 px). 바닥(Field)·길(Road · 2.46u 띠 = 지면 띠 30~52%)·물결 경계(Road_up · 데모 y/피치)도 데모 치수. 샘플 캐릭터만 제외.
- **특전 카드의 빨간 «Text»** → CardFrame_04 안 `Text_Title` 잔여 글자 — 프레임 안 글자를 전부 끄고 등급 리본만 넣는다.
- **새로고침 버튼의 «Remain : 1/1»** → 끔. 라벨은 «새로고침» 만. **더 못 하면 버튼 자체를 숨김**.
- **플레이 진입이 느리다** → EditorSettings «Enter Play Mode Options» 켜고 도메인 리로드 끔(`m_EnterPlayModeOptions: 1` · 씬 리로드는 유지). 정적 상태는 `UiKit.ResetStatics`(SubsystemRegistration) 로 판마다 초기화.

### 2026-09-05 답변 4 — 승인 대기 25~26 «그렇게 해라»
- **25 상점 수치**: 기본값 확정 — 다이아 100·1,100·3,500·6,000·10,000·14,000(₩1,000·1만·3만·5만·8만·11만) / 골드 1,000=다이아 30 · 3,000=80 · 10,000=250. `Assets/KkomaKnight/shop.json`(T9) 에 이 값으로.
- **26 장비 외형/아이콘 매핑**: 워커가 CharacterMaker 파츠(종류 × 등급)로 고르고 `GearLook` 표 + `docs/assets-map.md` 에 남긴다(T7). 목걸이·장갑·신발은 GUI Pro 아이콘 임시.

## 주인 승인 대기 (한 번에 답해 주시면 됩니다 — 답이 없으면 아래 «기본값» 으로 진행)

> 1~9 · 19~23 · 25~26 은 위 «주인 결정» 으로 종결됐다(이력으로 남긴다). 열린 것은 10번부터.


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


24. **간격 2배를 제대로 하려면 엔진 좌표를 바꿔야 한다** — `enemies.json` layout 의 `enemyGap 44→88 · nodeGap 280→560 · nodeGapEvent 470→940`. 그러면 모든 것이 같은 속도로 흐르면서 간격만 2배가 되고 칼 닿는 거리(74)는 그대로다. 대신 한 판 걷는 시간이 늘어 **밸런스가 바뀐다**(원거리 적이 다가오는 동안 더 쏜다 · 판 시간 상한 900초에 가까워진다) 하고, 시드 골든(BattleTests)이 깨져 sim.js 를 같은 값으로 고쳐 골든을 다시 뽑아야 한다(Node 로 가능 · aaaw 는 건드리지 않고 사본으로). **«해» 한 줄이면 진행**, 아니면 지금처럼 1배.

25. **상점 수치 — 종결(주인 «그렇게 해라» · T9 `4506ed4` 반영).** `Assets/KkomaKnight/shop.json` 에 다이아 100·1,100·3,500·6,000·10,000·14,000(₩1,000·1만·3만·5만·8만·11만) / 골드 1,000=다이아 30 · 3,000=80 · 10,000=250. 바꾸려면 이 파일의 숫자만(코드·카탈로그 손댈 것 없음). 참고: aaaw `gacha.json economy.iapGem` = 12,000(index.html 의 ₩110,000 칸)과 여기 ₩110,000 = 14,000 이 다르다 — 주인 확정값(shop.json)을 따랐고 iapGem 은 상점에서 더 쓰지 않는다. 12,000 으로 맞추길 원하시면 숫자 한 줄.
26. **장비 → 외형/아이콘 매핑(T7)** — 투구·무기·갑옷은 CharacterMaker 파츠(종류 × 등급 → 파츠 스프라이트 · `GearLook` 표 · `docs/assets-map.md`)로, 목걸이·장갑·신발은 그림이 없어 GUI Pro 아이콘 임시. 마음에 안 드는 파츠는 표의 한 줄로 바뀐다.

### T6 완료 기록 (2026-09-05 · sess-2034-9487 · 워커 B)

- **주인이 확인할 것 (한 줄)**: 로비가 Lobby_Default 데모 그대로인가 — 상단 바 맨 왼쪽 초상이 내 기사(움직이는 Idle)·그 옆 «전투력 N»·그 위 줄이 골드·보석 순인가 / «꼬마기사 키우기» 배너·오른쪽 상단 «장비» 버튼이 사라졌고 왼쪽 위 1개·오른쪽 위 2개 아이콘은 보이되 눌러도 아무 일 없는가 / 챕터 제목이 «Battle 1» 자리에 «챕터 N» 으로, 카드 양옆 ◀▶ 로 챕터가 바뀌는가.
- 만든 것
  - `Screens.cs`(LobbyScreen): 프리팹 안 요소의 Pct 재앵커링(T5)을 전부 걷어냄 — 자리 이동 0. 글자만 교체: UserInfo_01 `Text_UserName` = «꼬마기사» · `Text_GuildName` = «최고 챕터 N» · `Slider_Level_01` 의 «25 / 55» = **«전투력 N»**(`App.Power()` = GearScreen.BuildPower 와 같은 값 · 게이지는 꽉 채움) · Level 배지 = 최고 챕터 · `ResourceBar_Group` Coin/Gem = 골드·보석(GemStone 칸은 프리팹처럼 꺼진 채) · `Title_LineDeco_01_Blue` «Battle 1» = «챕터 N» · «Whisperwood» = 이번 챕터 맵 이름(가을 숲/깊은 숲/숲/사막 · `BattleWorld.Theme`) · START 그대로.
  - 왼쪽 위(Button_Ticket·Button_ADRemove 겹침 = 아이콘 1개로 보임)·오른쪽 위(Button_Mission·Button_Inventory) 아이콘: 프리팹 그대로 보이고 클릭 없음 · 영문 라벨만 «티켓·광고 제거·미션·가방». 메뉴(≡) = 일시정지/설정 팝업(T10 이 Settings 그대로 로 다듬는다).
  - `HeroView.cs`(신규 · T7 공용): 레이어 30 무대에 CharacterMaker Character 프리팹(Idle) + 그 레이어만 찍는 RenderTexture 카메라 → 호스트(초상 마스크) 안 RawImage. 월드 카메라 컬링 마스크에서 레이어 30 제외 · 화면이 꺼지면 무대도 꺼짐. **T7 훅 = `HeroView.PlayerSkin(App)`** — 지금은 기본 기사 파츠(전투 KnightSkin 과 동일) · T7 이 이 한 함수를 GearLook 로 바꾸면 로비·장비 화면이 같이 반영된다. 로비 `Refresh()` 가 매번 `SetSkin(PlayerSkin(App))` 을 부른다.
  - `TopBar.cs` 파일 삭제 · GearScreen/ShopScreen 의 `TopBar.Attach/Refresh` 줄 제거(장비 화면 오른쪽 위 골드는 T7 · 상점 상단은 T9 가 Shop_List 그대로).
  - catalog `_notes`(ui.lobby · cm.character · ui.resourceBar) 갱신 → `docs/assets-map.md` 재생성(새 키 없음 · AssetCatalog.asset 변화 없음).
- «그대로» 원칙의 예외 2건(PROGRESS 기록 규약): ① 챕터 ◀▶ 는 프리팹에 없어 카드(SampleImage_Map) 양옆에 Button_01_Blue 2개를 뒀다(챕터 선택 기능 유지 — 다른 방법 원하면 한 줄). ② 데모 채팅 줄 ChatBox 는 끔(이 게임에 채팅이 없다 · 이전부터 꺼져 있던 요소).
- 게이트: `dotnet build` 0/0 · `dotnet test` 40/40 · `gen_meta --check` · `gen_catalog --check`(419) · `check_data_sync` OK(aaaw `0707999`). Sim 시드 검증은 Core 를 안 건드려 생략.
- 워커 메모: 이 실행 환경엔 dotnet 이 없고 dot.net 설치 스크립트 호스트가 막혀 있다 — `packages.microsoft.com/ubuntu/22.04/prod/pool/main` 의 .deb(dotnet-sdk-8.0 8.0.424 · runtime/hostfxr/host/targeting-pack 8.0.30 · aspnetcore 8.0.30 · netstandard 2.1) 를 받아 `dpkg -x` 로 `~/dotnet8` 에 풀고 `DOTNET_ROOT=~/dotnet8/usr/share/dotnet` 로 게이트를 돌렸다(NuGet 은 api.nuget.org 로 됨).
- 한계: RenderTexture 초상은 에디터 없이 짠 것 — URP 2D 에서 알파 배경·카메라 맞춤(Bounds ×1.12)이 실물에서 어긋나면 `HeroView.Fit` 의 배율 한 줄.

### T7 완료 기록 (2026-09-05 · sess-2036-27996 · 워커 C)

- **주인이 확인할 것 (한 줄)**: 장비 탭이 Character_Hero_Equipment 데모 그대로인가 — 장착 슬롯 6칸의 아이콘이 프리팹 크기(164 칸 · Item 그대로)이고 «갑옷·장갑…» 부위 글자가 없는가(«Lv.N» 만) / 균등 보너스 문구가 사라졌는가 / 가운데에 내 기사(Idle)가 서 있고 투구·무기·갑옷을 장착하면 **장비 화면과 전투에서 바로** 그 파츠로 바뀌는가(치명 무기 = 검 · 체력실드 = 둔기 · 회피 = 창) / 상단에 TopBar 없이 오른쪽 위 골드만 있는가 / 하단 인벤 칸이 ListItem_EquipMent(188 격자 · 등급색 · +N · 세트 다이아 아이콘)이고 장착한 장비는 리스트에서 빠지는가 / 슬롯의 빨간 점 = 인벤에 더 좋은 게 있을 때만 켜지는가.
- 만든 것
  - `Core/GearLook.cs`(신규 · 순수 C#): **부위(투구·무기·갑옷) × 세트(치명·체력실드·회피) × 등급(4) → 카탈로그 `cm.gear.<부위>.<세트>.<등급>` 36키** 표 하나 — 전투(`BattleWorld` 플레이어 스킨)·장비 화면/로비(`HeroView`)·장비 아이콘(`GearUi.Cell`·슬롯)이 전부 이 표를 쓴다(승인 대기 26 «워커가 고른다»). 무기는 세트별 손 슬롯: 치명 = Sword · 체력실드 = Blunt(둔기 · 아이콘도 망치) · 회피 = Spear. 목걸이·장갑·신발은 그림이 없어 외형 미반영 · 아이콘은 기존 GUI Pro `gi.*`(임시). 파일 선택은 catalog.json(→ `docs/assets-map.md` 표 · 등급이 오를수록 금/보라 등 화려한 파츠 · 세트 색 = 치명 빨강/금 · 체력실드 파랑/은 · 회피 초록/보라).
  - `CharacterRig.PlayerSkin(D, S, shield)`: 기본 기사(cm.knight.*) 위에 장착 파츠를 덮는다. `BattleWorld.BuildPlayer` 가 이것을 쓴다(장비를 바꾸면 다음 전투부터 반영 · 전투 중 장비 변경은 없다). `HeroView.PlayerSkin(App)`(T6 의 훅) 도 여기로 연결 — 로비 초상도 자동으로 장착 외형.
  - `GearScreen.cs`: 프리팹 **원형 그대로**(T4/T5 의 Pct 재앵커링·슬롯 축소·Group_Slot/Group_List/ScrollRect 이동 전부 걷어냄 — 자리 이동 0). 슬롯 = 프리팹 GridLayoutGroup(164 · 2열 세로 우선 → 왼쪽열 무기·목걸이·갑옷 / 오른쪽열 투구·장갑·신발 = index.html GEAR_COL) · ItemFrame_01/Item 에 스프라이트만(크기 그대로) · NormalArea 에 등급색 · 다이아 = 세트 아이콘 · `Text_Level` = «Lv.N(+강화)» 만(부위 이름 없음) · 프리팹의 `Alert_Dot_01_Red` = «인벤에 더 좋은 게 있다»(코드 도형 ↑ 폐기). Character 자리 = `HeroView`(샘플 그림 끔 · 정사각 텍스처라 AspectRatioFitter 1:1). 균등 보너스 `Text_Level` 숨김(계산 함수 `EvenBonus` 는 남김). Group = 전투력 · Group_List 3행 = 공/체/실. 상단 = 프리팹 Top 오른쪽에 ResourceBar_Group 의 **Coin 칸만**(TopBar 없음). 하단 = 프리팹 Bottom 그대로(뒤로 = 로비 · Button_02_Blue = 상점 · Button_02_Convex_Green = 합성(N)). 프리팹 루트는 탭바(⑧ y 92.6) 위까지만 차지(내부 앵커는 그대로 · 화면이 짧아질 뿐).
  - `GearUi.Cell` = **ListItem_EquipMent**(카탈로그 `ui.equipCell` 신규 · 188×188): NormalArea 등급색 프레임(+2px 프리팹 값) · Item 아이콘(비례 유지) · Text_Level = «+N» · TypeArea 다이아 = 세트 아이콘 · 프리팹 Focus = 선택 · 프리팹 Check = 장착중(옵션) · 빨간 점 = 합성 가능(옵션 · Alert_Dot_01_Red 오른쪽 위) · NEW = 왼쪽 아래 점. `CellOpts.EquippedMark / FusableDot` 로 표기 on/off — 장비 화면은 둘 다 끔(장착분은 리스트에서 숨김), 대장간은 켬(ForgeScreen 한 줄 · T8 이 칸 비례를 마저 맞춘다). 세부 팝업·뽑기 결과도 같은 칸.
  - `Palette.Icons.Gear` 삭제(아이콘 표가 둘이 되지 않게 · 호출자 없음).
  - 테스트 `GearLookTests` 5개: 등급 수 = gear.json · 투구/무기/갑옷 × 세트 × 등급 36키가 catalog.json 에 실재 · 나머지 부위는 gi.* 실재 · 키 규칙 · 무기 슬롯.
- 게이트: `dotnet build` 0/0 · `dotnet test` **45/45** · `gen_meta --check` · `gen_catalog --check`(456) · `check_data_sync` OK(aaaw `0707999`) · `GetComponent…() ??` 패턴 0건. Sim 시드 검증은 엔진을 안 건드려 생략.
- 워커 메모: 이 환경도 dotnet 이 없고 dot.net 스크립트 호스트가 막혀 있다 — `apt-get install dotnet-sdk-8.0`(archive.ubuntu.com 은 열려 있다 · Ubuntu 24.04 자체 패키지 8.0.1xx) 로 3분 안에 설치됐다. HeroView 는 T6(워커 B)이 먼저 만들어 그것을 재사용(내 것은 버림 · PlayerSkin 훅만 채움).
- 한계: 에디터 없이 짠 것 — 특히 ⓐ Top 오른쪽 골드 칸 위치(x −24 · 세로 가운데)가 제목과 겹치면 `GearScreen.Build` 의 anchoredPosition 한 줄 ⓑ CharacterMaker 파츠 그림을 슬롯 Item(157px)·칸 아이콘으로 쓰면 작게 보일 수 있다(preserveAspect) — 크게 원하면 catalog 의 파츠를 바꾸지 말고 `Item` 스케일 한 줄 ⓒ 둔기(Blunt)·창(Spear) 파츠는 Attack.anim 이 HandRight 를 흔드는 대로 같이 움직인다(검과 같은 궤적).

### T10 완료 기록 (2026-09-05 · sess-2052-15499 · 워커 D)

- **주인이 확인할 것 (한 줄)**: 하단 탭 5칸이 «상점·장비·전투·탤런트·펫» 인가(대장간·설정 탭 없음 · 대장간은 장비 화면 «합성» 버튼으로만) / 탤런트·펫 탭을 누르면 Character_Talent_02 데모 화면(배경·패스 줄·재화 바)이 그대로 뜨고 그 안 하단 탭으로 다른 화면으로 나가지는가 / 로비 메뉴(≡)와 전투 일시정지의 설정 팝업이 Settings 데모 그대로인가 — 배경음·효과음·진동·언어 4줄 + 버튼 4개 + 세이브 ID + 버전 + 약관 글자가 모두 보이고, 배경음 스위치만 켜고 끄기가 저장되며, 전투에서만 아래 두 버튼이 «재개»·«포기하고 로비로» 인가.
- 만든 것
  - `Screens.cs`: `NavBar` 를 GearScreen.cs 에서 이곳으로 옮김(API `Attach/Wire/Refresh` 동일 · 장비/상점 화면 호출부 그대로). 탭 = **상점 · 장비 · 전투 · 탤런트 · 펫**(프리팹 자식 순서 0~4 그대로). 탭 이동 시 팝업이 떠 있으면 닫고 간다. 로비 메뉴(≡) → `Overlay.Settings()`.
  - `Overlay.cs`: `Settings()`(로비 · 제목 «설정») / `Pause(onResume, onGiveUp)`(전투 · 제목 «일시정지») 가 같은 `SettingsPopup` 을 쓴다 — **Settings 프리팹의 줄·버튼·글자를 하나도 끄지 않는다**(예전에 숨기던 SFX·Haptic·Language·Group_Button_1·약관 글자·UID 아이콘 전부 복원). 글자만 우리말: 배경음·효과음·진동·언어/한국어·평가하기·로그인·고객 지원·계정 삭제·개인정보 처리방침·이용약관·세이브 ID·버전. 동작하는 것 = 배경음 스위치(Save.Muted) · 닫기(X) · 전투에서만 Group_Button_2 = 재개(파랑)/포기하고 로비로(빨강). 나머지는 눌러도 아무 일 없음(주인 «나중 업데이트»).
  - `Overlay.TalentPet(kind)`: **Character_Talent_02 프리팹 통째로**(배경 그라데이션·패턴 · ResourceBar_Group = 골드·보석 · 패스 줄 6개 데모 그대로 · 하단 탭 바). 프리팹 안 탭 바를 `NavBar.Wire` 로 배선해 켜진 탭 라벨이 «탤런트»/«펫» 이 되고, 다른 탭을 누르면 팝업이 닫히며 그 화면으로 간다. 프리팹에 닫기 버튼이 없어 **새로 그리지 않았다**(«그대로» 원칙 · 닫기 = 탭 바).
  - catalog: `ui.talent`(Character_Talent_02) · `ui.talentIcon`(Economy_Star_01_Yellow · 워커 선택) · `ui.petIcon`(Item_Egg_01 펫 알 · 워커 선택) → `AssetCatalog.asset`·`docs/assets-map.md` 재생성(T7 것과 합쳐 459). 바꾸려면 catalog.json 경로 한 줄.
- 기본값으로 정한 것(주인이 바꿀 것만): ⓐ 탭 아이콘 = 별(탤런트)·알(펫). ⓑ 로비 설정 팝업의 아래 버튼 2개는 데모 라벨 그대로 «고객 지원·계정 삭제»(기능 없음) — 거슬리면 한 줄로 빈 라벨/다른 글자로. ⓒ Character_Talent_02 의 패스 줄 내용(스탯 아이콘·단계 숫자)은 데모 그대로.
- 게이트(리베이스 뒤 T7 합류 상태에서 재실행): `dotnet build` 0/0 · `dotnet test` **45/45** · `gen_meta --check` · `gen_catalog --check`(459) · `check_data_sync` OK(aaaw `0707999`). Sim 시드 검증은 Core 를 안 건드려 생략.
- 워커 메모: dotnet 은 T6 워커 방식(packages.microsoft.com .deb → `dpkg -x` → `~/dotnet8`) 으로 설치해 게이트를 돌렸다. lock 커밋(`90abcd3`)은 `[skip ci]` 규칙이 push 직후 ROUTINE 에 추가돼 붙이지 못했다(그 뒤 커밋부터 준수).
- 한계: 에디터 없이 짠 것 — ⓐ Character_Talent_02 는 화면 전체 프리팹이라 Overlay 층에 Stretch 로 세웠다(로비 위를 덮음 · 실물에서 배경이 비치면 `TalentPet` 의 raycast/배경 한 줄) ⓑ Settings 의 «한국어» 버튼·약관 글자는 눌러도 아무 일 없음이 의도.

### T8 완료 기록 (2026-09-05 · sess-2113-28861 · 워커 A)

- **주인이 확인할 것 (한 줄)**: 장비 화면 «합성» 으로 들어간 대장간에서 — 하단 인벤에 장비가 **전부**(장착분 포함 · 장착분은 체크 표시 + 흐리게) 보이는가 / 칸이 장비 화면 인벤 칸과 **같은 크기·비례**(ListItem_EquipMent 188 정사각 · 5열 · 찌그러짐 없음)인가 / 같은 부위·종류·등급이 3개 이상인 칸 **오른쪽 위 빨간 점**이 켜지고 재료를 하나 고르면 점이 사라지는가(index.html 과 같음) / 위 재료 3칸·결과 칸도 같은 정사각 칸인가.
- **«하단에 장비가 없다» 원인(한 줄)**: 격자·ScrollRect·Pct 겹침이 아니라 **`Refresh()` 중단** — 주인이 본 시점(e64ff41 이전)엔 인벤 루프 **앞**의 `UiKit.SetInteractable(합성 버튼)` 이 CanvasGroup 을 «GetComponent 뒤 ?? AddComponent» 로 붙이다 에디터 가짜 null 로 `MissingComponentException`(주인 로그의 «Button_02_Orange CanvasGroup» = 대장간 합성 버튼 `ui.btnOrange`) 을 던져 그 뒤 인벤 루프가 통째로 건너뛰어졌다. e64ff41(`UiKit.Ensure`) 이 예외를 없앴고, T8 은 인벤 채우기를 `Refresh()` 맨 앞으로 옮겨 같은 종류의 사고가 인벤을 다시 비우지 못하게 했다.
- 만든 것
  - `GearUi.Grid`: ref-layout 표 % 로 만들던 칸(18.4×7.2% = 199×168 · 정사각 프리팹이 찌그러짐) 대신 **장비 화면 프리팹(Character_Hero_Equipment) 의 Content GridLayoutGroup 값을 그대로 복사**(cellSize 188×188 · spacing.y 24.5 · padding 6/20 · 5열) — 대장간 칸이 장비 화면 칸과 같은 크기·비례가 되도록 «값을 베끼는» 방식(수치를 코드에 박지 않음). 가로 간격만 view 폭(94% = 1015px)에 맞춰 (폭 − 5×188)/4 로 재계산해 5열이 딱 들어간다 — 칸 크기는 절대 줄이지 않는다. 프리팹이 없을 때의 대체 = `ui.equipCell` 본래 한 변(정사각) → 그것도 없으면 표 % 폭의 정사각.
  - `ForgeScreen`: 재료 3칸은 Pct(17×9.5% = 184×222) 로 늘리지 않고 슬롯 자리(`Mat0~2` · 위치는 표 그대로) **가운데에 칸 본래 크기(188)** 로 둔다. 결과 칸은 원래부터 본래 크기. 인벤: 장착중 = 프리팹 `Check` + 흐리게(재료 불가 · 토스트 그대로) · 합성 가능 = `ui.alertDot`(Alert_Dot_01_Red · 오른쪽 위 · `CellOpts.FusableDot`) — 재료를 고르는 중엔 점을 끔(index.html `renderForge` 의 `fus:!lock&&…` 그대로). `FusableKeys` 는 index.html `fusableKeys` 와 같이 장착분도 셈(정본 유지).
  - catalog `_notes` 에 `ui.alertDot` 용도 기록(ROUTINE T8 의 «`ui.redDot`» 는 같은 에셋이라 **키를 하나 더 만들지 않고** 기존 키를 쓴다) → `docs/assets-map.md` 재생성(새 키 없음 · 459).
- 게이트: `dotnet build` 0/0 · `dotnet test` **45/45** · `gen_meta --check` · `gen_catalog --check`(459) · `check_data_sync` OK(aaaw `0707999`) · 코드의 «GetComponent…() ??» 패턴 0건(UiKit 설명 주석 1줄만). Sim 시드 검증은 Core 를 안 건드려 생략.
- **플레이 콘솔 에러 0 을 무엇으로 확인했는가**: PlayMode 스모크 테스트는 아직 없다(T11 대기 · `Assets/Tests/PlayMode` 미생성) → **주인이 에디터 플레이로 확인할 것 — 장비 → 합성(대장간) → 칸 클릭·자동·합성 → 콘솔 빨간 줄 0**. 코드 감사: 이번에 넣은 코드는 프리팹·자식 `Find`·GridLayoutGroup 이 없을 때 전부 대체 경로로 내려가고(null 검사) 새 카메라·RenderTexture 는 만들지 않았다. T11 워커는 스모크 테스트에 «대장간 열기 + 칸 3개 선택 + 합성» 을 넣어 주면 된다.
- 워커 메모: 이 환경엔 dotnet 이 없다 — `apt-get update && apt-get install -y dotnet-sdk-8.0`(Ubuntu 24.04 자체 패키지 · update 없이 바로 install 하면 404) 로 설치해 게이트를 돌렸다.
- 한계: 에디터 없이 짠 것 — ⓐ 프리팹 Content 격자의 세로 간격(24.5)·패딩을 그대로 쓰므로 표 ⑥ 의 «행 피치 7.6%» 와는 다르다(주인 «칸 비례 = 장비 화면과 같게» 가 우선) ⓑ 재료 칸(188)이 슬롯 자리 폭(184)보다 2px 씩 넘친다(보이지 않는 차이 · 거슬리면 `Layout.ForgeMat.W` 한 줄).

### T12 완료 기록 (2026-09-05 · sess-2121-23849 · 워커 B)

- **주인이 확인할 것 (한 줄)**: 에디터 플레이 → 로비(초상) → 장비(가운데 캐릭터) → 전투 → 일시정지 «포기하고 로비로» → 다시 장비 — 콘솔에 `Renderer2D Pass: Fake or uninitialized surface…` / `EndRenderPass: Not inside a Renderpass` 가 **더 이상 안 뜨는지**(둘 다 사라져야 정상). 뜨면 원문을 «주인 콘솔 에러 보고함» ④ 로 붙여 주시면 된다.
- **원인 확정 근거(한 줄)**: 프로젝트 렌더러는 `Assets/Settings/Renderer2D.asset` 하나(URP 17.3 · `m_UseDepthStencilBuffer: 1`)인데, `HeroView.BuildStage` 가 깊이 0(`depthStencilFormat None`) RenderTexture 를 런타임 카메라 타깃으로 썼다 → Renderer2D 렌더그래프가 카메라 타깃의 깊이/스텐실 표면을 attachment 로 import 하다 «없는 표면(fake)» 이라 렌더패스 시작 실패(①) → 짝이 안 맞는 End(②). 이 카메라는 로비·장비 두 화면에서 매 프레임 그리므로 «플레이하면 항상» 이 설명된다. 씬의 Main Camera 는 화면(백버퍼)에 그리므로 무관. 코드에서 RenderTexture/Camera 를 만드는 곳은 `HeroView` 뿐(감사 ⓔ).
- 만든 것
  - `HeroView.CreateTargetTexture(size, name)`: `RenderTextureDescriptor(ARGB32, depthBufferBits 24)` + `depthStencilFormat = GraphicsFormatUtility.GetDepthStencilFormat(24, 8)`(D24S8 · 없으면 D32S8) — 에셋 설정(`m_UseDepthStencilBuffer`)을 끄지 않고 코드가 렌더러에 맞춘다. 카메라는 설정을 다 넣은 뒤 켠다 · `UniversalAdditionalCameraData` 를 `UiKit.Ensure` 로 붙여 Base · 후처리/그림자/깊이·색 텍스처 요청 없음 · AA 없음(파이프라인이 늦게 자동 생성하는 것에 기대지 않는다). `OnDestroy` 순서 고정 · 무대가 꺼진 동안엔 `Animator.Play` 를 부르지 않고 `OnEnable` 에서 Idle 재생 · `_count` 를 `SubsystemRegistration` 에서 0 으로(도메인 리로드 끔 대비 · UiKit 규약). 로비(T6)·장비(T7) 두 인스턴스가 같은 코드.
  - `KkomaKnight.Game.asmdef`·`Tests.PlayMode.asmdef` 에 `Unity.RenderPipelines.Universal.Runtime` 참조 · dotnet 검사 빌드용 스텁 `tools/dotnet/Stubs/URP.cs`(TMPro 스텁과 같은 방식 · 쓰는 표면만).
  - `Assets/Tests/PlayMode/HeroViewTests.cs` **2개**: ① `StandaloneHeroViewRendersWithoutErrors` — App 없이 HeroView 를 세워 텍스처 depth>0 · depthStencilFormat≠None · URP 데이터 Base 단언 → `Camera.Render()` 3프레임 → 끄고 켜기 → 파괴, 단계마다 `LogAssert.NoUnexpectedReceived` ② `SceneLobbyGearBattleRoundTripNoErrors` — `SampleScene` 을 실제로 로드(Bootstrap → 데이터 → App) → 로비 → 장비 → 전투 1초(월드 카메라도 강제 렌더 · HeroView 레이어 제외 단언) → 로비 → 장비 → 로비 → App 파괴, 단계마다 에러 0. 배치 모드 CI 는 GameView 를 안 그리므로 `Camera.Render()` 로 URP 루프(주인 스택의 `DoRenderLoop_Internal`)를 직접 밟는다 · `WaitForEndOfFrame` 은 배치 모드에서 영영 안 돌아와 쓰지 않았다.
  - `tools/check_catalog_keys.py`(신규 게이트 · ci.yml dotnet 잡 + ROUTINE §3): `Assets/Scripts` 의 카탈로그 키 리터럴 552개 ↔ `catalog.json` 458키 대조(접두 조립 `"env."`·`FrameKey("ui.cardFrame", 색)` 허용 · 데이터 파일 이름 제외). 지금 누락 0. 없는 키를 일부러 넣으면 잡히는 것 확인.
- **감사 결과(ⓐ~ⓔ · `Assets/Scripts/Game` 20파일 전수)** — 새로 확인된 «정상 플레이 중 빨간 줄» 원인 **0**:
  - ⓐ 가짜 null `?? AddComponent` 패턴 0건(e64ff41 이후). 프리팹 자식 `Find` 는 전부 null 검사 뒤 사용. `(RectTransform)` 캐스트는 uGUI 프리팹 노드라 안전. `App.Assets` 무가드 사용처(Overlay/CharacterRig/BattleWorld)는 카탈로그가 씬에 연결돼 있어 정상 플레이에선 안 터진다(미연결이면 Bootstrap 이 이미 LogError).
  - ⓑ 카탈로그 키: 위 스크립트로 리터럴 전부 실재. 조립 키(`cm.gear.*`·`gi.*`)는 EditMode `GearLookTests` 가 표 전체를 대조.
  - ⓒ `UnityEditor.*` 사용 0 · `Resources.Load` 는 내장 폰트 1곳(`FontOrBuiltin` · DefaultFont 가 있으면 안 탄다) · `Camera.main` 은 Bootstrap 1곳(씬에 MainCamera 태그 있음).
  - ⓓ DOTween: `Assets/Resources/DOTweenSettings.asset` `useSafeMode: 1`(safeModeOptions.logBehaviour Warning) — 파괴된 타깃의 트윈(버튼 눌림 연출 뒤 즉시 Close · 팝업 PopIn · 데미지 팝)은 safe mode 가 자동 kill 하므로 빨간 줄 없음. 코루틴은 Bootstrap 의 데이터 로드 1개뿐(콜백은 `_status`/`_boot` null 검사 필요 없음 — 로드 중 파괴 경로 없음).
  - ⓔ 런타임 Camera/RenderTexture 는 HeroView 뿐(수정) · `cm.character` 프리팹 Animator 에 컨트롤러 연결돼 있음(`m_Controller` 있음) · 폰트 = 씬 주입 Jua → 없으면 내장 · `mat.hitFlash` 는 null 이면 Flash 가 건너뜀.
  - 에러 아님(참고 · 등재 안 함): `UiKit.SetText/SetSprite` 의 «글자/이미지 없음» 은 노란 경고이고 프리팹 경로가 실제로 있는지는 에디터/CI PlayMode 가 확인 — T11 스모크 테스트가 화면마다 열어 잡는다. `BattleScreen.Tick` 의 백그라운드 따라잡기(최대 600초분)는 한 프레임이 길어질 뿐 에러가 아니다.
- 게이트: `dotnet build` 0/0 · `dotnet test` **45/45** · `gen_meta --check` · `gen_catalog --check`(459) · **`check_catalog_keys` OK(552/458)** · `check_data_sync` OK(aaaw `0707999`). Sim 시드 검증은 Core 를 안 건드려 생략.
- **플레이 콘솔 에러 0 을 무엇으로 확인했는가**: **CI 런 #33**(https://github.com/kuzuni/aaawunity/actions/runs/33994447750 · `2203550` 을 포함한 첫 유니티 잡 — `2203550` 자체는 [skip ci] 문서 커밋과 한 번에 push 돼 자기 런이 없다) 의 PlayMode 결과 XML: `HeroViewTests.StandaloneHeroViewRendersWithoutErrors` **Passed**(깊이/스텐실 단언 + `Camera.Render()` 3프레임 + 끄고 켜기 + 파괴), EditMode 50/50, 그리고 잡 로그 전체(3,593줄)에 `Fake or uninitialized surface` / `EndRenderPass` 문자열 **0건**. `SceneLobbyGearBattleRoundTripNoErrors` 는 **Failed** 였는데 원인은 콘솔 에러가 아니라 내가 쓴 `LogAssert.NoUnexpectedReceived()` 가 Bootstrap 의 일반 `Debug.Log`(«data loaded …») 까지 실패로 본 것(Test Framework 1.6) — 워커 D(T11) 가 `PlayLog.AssertNoRed` 로 교체(`ab9b192`) 하고 배치 모드에서 화면 타깃 카메라를 수동 렌더하면 생기는 도구 자체 에러 때문에 씬 왕복의 `WorldCamera.Render()` 도 뺐다(`bb196d1`). **씬 왕복 테스트 = CI 런 #38 Passed**(https://github.com/kuzuni/aaawunity/actions/runs/33995925475 · `b001d5f` · 두 번째 체크인 22:5X 에 로그 원문으로 확인: `HeroViewTests` 2/2 Passed · PlayMode 9/9 · EditMode 55/55 · `Fake or uninitialized surface`/`EndRenderPass`/`Unhandled log message` 0건). #35/#36 의 빨강은 T15(`PanelView.OnEnable` 예외 · 워커 C) 원인이었고 HeroView 와 무관. CI 러너(xvfb · 소프트웨어 GL) 와 주인 GPU 가 다를 수 있으므로 **주인 에디터 플레이 확인**(위 «주인이 확인할 것»)이 최종.
- 워커 메모: dotnet 은 `apt-get update && apt-get install -y dotnet-sdk-8.0` 로 설치(T8 워커 메모 그대로). PlayMode 테스트를 로컬에서 못 돌리므로 테스트 API 는 유니티 6(`FindObjectsByType(FindObjectsInactive, FindObjectsSortMode)` · `RenderTexture.depthStencilFormat`) 기준으로 썼다.

### T9 완료 기록 (2026-09-05 · sess-2136-22274 · 워커 C)

- **주인이 확인할 것 (한 줄)**: 상점 탭이 Shop_List 데모 그대로인가 — 위에서부터 «상점» 제목 · **ListItem_ShopPackage 3장 = 희귀/전설/신화 상자**(왼쪽 아래 이름+천장/누적 줄 · 오른쪽 위 3칸 = 상위 등급 확률 · 오른쪽 위 배지 = «천장 N회»(희귀 상자는 없음) · 오른쪽 아래 **1회 / 10회** 버튼 · 다이아 부족이면 흐림) → «일일 무료 보급» 줄(오른쪽 타이머 = 자정까지 / «지금 수령 가능» · 칸 2개 = 무료 보급 «수령»/«완료» · 추가 보급 «준비 중» 잠김) → «다이아 (모의 결제 — 실결제 없음)» ListItem_ShopItem 6칸(₩ 버튼 누르면 바로 지급) → «골드 (다이아 소모)» 3칸(💎 가격 · 부족하면 흐림) / 하단 소탭 «뽑기·다이아·골드» 를 누르면 그 섹션으로 스크롤되고 / 맨 아래 탭 5칸이 다른 화면과 같은가 / 뽑기 결과가 **Shop_Chest_Open**(리본 제목 «신화 상자 10회» · 열린 상자 · 상자 위쪽 보상 자리에 ListItem_EquipMent 칸 4열 격자 · 상자 아래 이름 목록 · «터치하면 닫기»)인가.
- 만든 것
  - `Assets/KkomaKnight/shop.json`(신규 · 이 레포 전용 · 승인 대기 25 주인 확정값): `gemPacks` 6(won·gem) · `goldPacks` 3(gold·gem). StreamingAssets/data 는 aaaw 동기 폴더라 못 넣으므로 **카탈로그 `texts` 섹션(`data.shop` · TextAsset fileID 4900000)** 으로 빌드에 참조된다 — `AssetCatalog.texts`/`Text(key)` · `gen_catalog.py` texts 섹션 · `check_catalog_keys.py` 가 texts 도 본다. `Bootstrap.LoadShop` 이 데이터 로드 뒤 `GameData.Shop`(순수 C# `ShopData.Parse`) 에 올린다 — 없으면 LogError 1줄 + 상점은 다이아/골드 줄 없이 뜬다(예외 없음).
  - `ShopScreen.cs`: **Shop_List 원형 그대로**(T4 의 코드 카드 레이아웃 폐기 · Layout.Shop* 상수는 LayoutSpecTests 대조용으로만 남음 · 요소 이동 0). 프리팹 ScrollRect/Content/섹션 순서 그대로 쓰고 글자·그림·개수만 바꿈: ① `ListItem_ShopPackage` = 상자(Text_Title = 이름 + `<size=20>` 천장/누적 줄 · Group_Items 3칸 = 높은 등급부터 확률(아이콘 = Grade_Gem 등급색 · 0% 등급 생략 · index.html gachaRateText 순서) · Badge = 천장 · Button_Price(프리팹) = 10회 · 그 복제 = 1회) ② `Title_DailyDeals` = «일일 무료 보급» + `Timer_01` = 자정까지 카운트다운(Tick 1초) · `Group_Item` 의 ShopItem 칸 = 무료 보급(gacha.json dailyGem · «수령»/«완료») · 추가 보급(잠김 · index.html «준비 중» 그대로) ③ `Title_Gem`+`Group_Gem1/2` = 다이아 6(Text_Title «다이아» · Icon = HUD 보석 · Text_ItemNum = 개수 · Text_Limit «모의 결제» · Button = ₩) ④ `Title_Gold`+`Group_Gold` = 골드 3(Icon = HUD 골드 · Button = 💎 가격 · 부족 시 흐림+토스트) ⑤ `Tab_02_BoxMenu_Text`(데모 Special/Deal/Resources) = «뽑기·다이아·골드» 소탭 → 섹션 제목이 뷰포트 위에 오게 스크롤 ⑥ `Tab_01_BottomFlushMenu` = NavBar.Wire(T10 배선 그대로) ⑦ `ResourceBar_Group` = 골드·보석(GemStone 칸은 로비처럼 끔).
  - 뽑기 결과 = `ui.chestOpen`(Shop_Chest_Open) — **T4 부터 이미 이 프리팹을 썼다**(ROUTINE T9.4 «정말 쓰는지 확인» → 씀). 이번에 프리팹의 보상 1칸(`ItemFrame_01` · 가운데 위 y+217) **자리에** 얻은 장비 격자(칸 = `GearUi.Cell` = ListItem_EquipMent 188 본래 크기 · 4열 · 최대 3행 · 세로 가운데 = 프리팹 칸 자리)를 두고, «최고 등급» 줄은 격자 위 · 이름 목록은 상자 아래(프리팹 Chest 의 rect 로 계산 · 픽셀 상수 없음). 배경 터치 = 닫기.
  - catalog: `ui.shopList`(Shop_List) · `ui.shopItem`(ListItem_ShopItem) · `data.shop` 추가 · `ui.shopChest`(ListItem_ShopChest · 미사용) 삭제 → `AssetCatalog.asset`·`docs/assets-map.md` 재생성(461).
  - 테스트 `ShopDataTests` 5개: shop.json 꼴(다이아 6 · 골드 3 · 양수 · 오름차순 · 골드 최저가 ≤ 다이아 최소 상품) · Parse 최소 JSON · 빈 상품표는 예외. `TestData.RepoFile`(레포 루트 기준 경로) 추가.
- «그대로» 원칙의 예외(PROGRESS 기록 규약): ① 프리팹에 ShopPackage 가 2장뿐이라 두 번째를 **복제**해 3장(주인 «3개»). ② 1회/10회 = 프리팹 `Button_Price` 하나를 **복제**해 왼쪽에(ROUTINE T9.2 가 허용한 유일한 예외 · 10회가 프리팹 원래 자리). ③ 끈 것(지우지 않음): `Group_Chest`(ListItem_ShopChest 3) · `Title_Silver`/`Group_Silver` · `Group_Item` 3번째 칸 · `Group_Gem1/2`·`Group_Gold` 의 데모 칸(ListItem_ShopGem/ShopGold — 같은 줄에 주인 지정 ListItem_ShopItem 을 세움) · ResourceBar 의 GemStone 칸.
- 기본값으로 정한 것(주인이 바꿀 것만): ⓐ 일일 무료 보급(index.html 의 기능)은 ROUTINE T9 목록에 없지만 기능을 빼지 않으려고 `Title_DailyDeals`+`Group_Item` 줄에 뒀다(«Daily Deals» 자리 = 무료 보급 · Timer_01 = 자정까지). 없애려면 한 줄. ⓑ 상자 카드의 확률 3칸은 프리팹 칸이 3개라 **상위 3등급**만 — 신화 상자는 «일반 65.2%» 가 카드에 안 적힌다(희귀·전설 상자는 등급이 3개 이하라 전부 적힘). ⓒ 배지 = «천장 N회»(신화 50 · 전설 10 · 희귀 상자는 배지 없음). ⓓ 다이아 칸 부제 «모의 결제» · 골드 칸 부제 «다이아로 구매» · 골드 섹션 제목 «골드 (다이아 소모)». ⓔ 버튼 글자 «1회 💎80»(index.html 표기 그대로 · Jua 폰트에 💎 글리프가 없으면 빈 칸으로 보일 수 있음 — 거슬리면 «1회 80» 으로 한 줄).
- 게이트: `dotnet build` 0/0 · `dotnet test` **50/50** · `gen_meta --check` · `gen_catalog --check`(461) · `check_catalog_keys` OK(547/460) · `check_data_sync` OK(aaaw `0707999`) · `GetComponent…() ??` 패턴 0건 · Sim 시드 11·12·13 사다리 21칸 = T2 표와 동일(Core 는 ShopData 추가뿐 · 엔진 무변경).
- **CI 근거(후속 · 22:2X)**: 코드 커밋 `4506ed4` 의 CI #33 유니티 잡은 빨강이었으나 원인은 `HeroViewTests` 의 `LogAssert.NoUnexpectedReceived` 가 Bootstrap 일반 로그를 실패로 본 검사 도우미 과잉 판정(T11 워커가 확정·`PlayLog.AssertNoRed` 로 교체) — 상점 코드 무관. T11 의 상점 스모크 테스트 `UiSmokeTests.ShopBoxesAndChestOpenPopup`(상자 3 이름 · 탭 5 · 1회/10회 클릭 · Shop_Chest_Open 열림 · 빨간 줄 0 · 데모 잔여 글자 0)은 **CI #36(https://github.com/kuzuni/aaawunity/actions/runs/33995378223) 에서 Passed**.
- **플레이 콘솔 에러 0 을 무엇으로 확인했는가**: PlayMode 스모크 테스트는 T11(워커 D · 진행 중)이 만든다 → **주인이 에디터 플레이로 확인할 것 — 로비 → 상점 탭 → 소탭 3개 · 무료 보급 수령 · 다이아 ₩1,000 · 골드 1,000 · 희귀 상자 1회/10회 → 결과 팝업 닫기 → 콘솔 빨간 줄 0**. 코드 감사: 프리팹 자식 `Find`/`GetComponent` 는 전부 null 검사 뒤 사용(프리팹이 없어도 빈 화면 + 노란 경고뿐) · 새 카메라·RenderTexture 없음 · 카탈로그 키 전부 실재(게이트) · shop.json 미연결/파싱 실패는 LogError 1줄 뒤 상품 없이 진행(예외 없음). T11 워커에게: 스모크 테스트에 «상점 열기 → 소탭 클릭 → 뽑기 1회(다이아 세이브에 넣고) → 결과 팝업 닫기» 를 넣어 주면 된다(`ShopScreen` 은 `App.ShowScreen("shop")` · 결과 팝업은 `Overlay` 의 `ui.chestOpen` 루트).
- 워커 메모: dotnet 은 `apt-get update && apt-get install -y dotnet-sdk-8.0`(T8 워커 방식) 로 설치. 프리팹 구조는 YAML 을 직접 파싱해(scratchpad 스크립트 · 커밋 안 함) 자식 이름·앵커·중첩 인스턴스 부모를 읽었다 — Shop_List 의 Content 는 VerticalLayoutGroup, 각 Group_* 는 HorizontalLayoutGroup(칸 3개 · 자기 크기 유지) 이라 칸을 끄고 같은 크기의 ShopItem 을 세우면 줄 비율이 그대로다.
- 한계: 에디터 없이 짠 것 — ⓐ ShopPackage 의 Text_Title 두 줄(이름 40 + 천장 20)이 79px 높이 안에 들어가게 계산했지만 실물에서 1회 버튼(왼쪽 복제 · x≈482~722)과 둘째 줄이 스치면 `BindBox` 의 `<size=20>` 한 줄 ⓑ Timer_01 은 ContentSizeFitter 라 글자 길이만큼 늘어난다(«지금 수령 가능» 이 길면 Title_LineDeco 글자와 겹칠 수 있음 → 문구 한 줄) ⓒ 소탭 스크롤은 섹션 제목의 위 끝을 뷰포트 위에 맞춘다(끝 섹션은 Content 끝에 걸려 덜 올라감 — 정상) ⓓ Grade_Gem 아이콘의 등급색 틴트가 실물에서 탁하면 `BindBox` 의 스프라이트 키 한 줄.

### T11 완료 기록 (2026-09-05 · sess-2150-31726 · 워커 D) — CI 런 #38 초록으로 종결

- **주인이 확인할 것 (한 줄)**: GitHub Actions 의 «Unity EditMode + PlayMode 테스트» 잡(CI 런 #38 · https://github.com/kuzuni/aaawunity/actions/runs/33995925475)에서 `UiSmokeTests` 5개 + `HeroViewTests` 2개가 초록인가 — 빨간 것이 있으면 실패 메시지가 «[어느 화면] 플레이 콘솔 빨간 줄 N줄 / 데모 프리팹 잔여 글자 N건 / 프리팹 경로·카탈로그 키 경고 N건 / 어느 요소가 없음» 을 오브젝트 경로와 함께 그대로 적으므로, 그 줄을 «주인 콘솔 에러 보고함» 에 붙여 주시면 다음 워커가 고친다.
- **CI 런 이력(이 작업의 코드 커밋 3개 · 원인은 전부 로그로 확정)**: #34(`bddcf98`) = 5개 전부 첫 검사 지점에서 «Unhandled log … [Error] `BlitFinalToBackBuffer/Draw UIToolkit/uGUI Overlay: The dimensions or sample count of attachment 0 do not match RenderPass specifications (461 x 578 1AA) vs (640 x 480 1AA)`» — 배치 모드에서 **화면 타깃(메인·월드) 카메라를 수동 `Camera.Render()`** 하면 URP 최종 블릿의 백버퍼 크기가 어긋나 검사 도구가 스스로 만든 에러(RenderTexture 타깃인 HeroView 카메라 수동 렌더는 깨끗함 · 실제 플레이 루프에서는 안 난다) → `bb196d1` 에서 두 테스트의 월드 카메라 수동 렌더 제거(HeroView 카메라만). #35(`ab9b192`) = PlayLog 교체만 든 중간 커밋(같은 오탐으로 빨감 · 무시). **#36(`bb196d1`) = PlayMode 8개 중 5개 통과(상점·대장간·HeroView 2·데이터 로드) · 3개(설정/장비 세부/전투 팝업을 여는 테스트) 가 진짜 예외 `UnassignedReferenceException: The variable otherPanels of PanelView has not been assigned`(GUI Pro 데모 스크립트 `PanelView.OnEnable` 이 `UiKit.Spawn` 의 Instantiate 중 실행) 로 실패 = 스모크 테스트가 잡은 실제 플레이 콘솔 에러** → 워커 C 가 T15 로 등재·수정(`b001d5f` · `UiKit.Spawn` 이 비활성 대기 오브젝트 밑에 인스턴스화한 뒤 데모 스크립트를 떼고 옮김) · 런 #38 이 그 확인.
- **CI #33(T9 커밋) 빨간 잡 원인 확정(한 줄)**: PlayMode `HeroViewTests.SceneLobbyGearBattleRoundTripNoErrors` 실패 = `Unhandled log message: '[Log] [KkomaKnight] data loaded …'` — **`LogAssert.NoUnexpectedReceived()` 가 Bootstrap 의 일반 `Debug.Log` 까지 «예상 밖 로그» 로 실패시킨다**(Test Framework 1.6.0). 콘솔 에러가 아니라 검사 도우미의 과잉 판정. 수정 = `PlayLog.AssertNoRed`(Error·Exception·Assert 만 수집 · 어느 화면인지 함께 나열)로 두 테스트 파일 교체(`ab9b192`) · ROUTINE §1·§3 에 «NoUnexpectedReceived 금지» 명시. EditMode 50/50 · DataLoaderPlayTests · HeroView 단독 렌더 테스트는 #33 에서 이미 초록(T12 의 URP 수정은 CI 러너에서 에러 0 확인됨).
- 만든 것
  - `Assets/Tests/PlayMode/UiSmokeTests.cs` **5개**(각각 SampleScene 을 새 세이브로 올려 App 을 세운 뒤): ① `LobbySettingsTalentPetToast` — 로비(탭 5 · 초상 HeroView · START · 전투력 · 챕터 ◀▶) → 설정(Settings 그대로 · 배경음 스위치 = Save.Muted · X 닫기) → 탤런트·펫(Character_Talent_02 · 켜진 탭 라벨) → 토스트. ② `GearScreenDetailSlotAndEquip` — 6부위 장비를 만들어 장비 화면(Group_Slot 6 · 탭 5 · 인벤 칸 = 장비 수) → 세부 팝업(Character_Hero_Item_Detail_01) «장착» → 장착분이 리스트에서 빠짐 → 세부 팝업(장착중) «슬롯 강화»(Lv 0→1) · X 닫기 → 6부위 전부 장착(투구·무기·갑옷 외형 = GearLook 경로) → 빈 슬롯 팝업 «닫기». ③ `ForgeShowsAllAndFuses` — 같은 키 3개 + 장착분 1개로 대장간(인벤 = 장비 전부 · 빨간 점 ≥3) → 칸 3개 클릭 «합성 (3/3)» → 합성(3→1 · Fuses 1) → «자동»(조합 없음 토스트) → «← 장비». ④ `ShopBoxesAndChestOpenPopup` — 다이아를 주고 상점(gacha.json 상자 3종 이름이 화면에 있음 · 탭 5) → «1회» 뽑기 → 결과 팝업이 **Shop_Chest_Open(`ui.chestOpen`) 인지** + 얻은 장비 칸 수 → «10회» 뽑기(≥10개). ⑤ `BattleTicksAndAllBattlePopups` — 챕터 1 전투 3초(엔진 시간 > 0 · HUD 챕터/웨이브) → `Time.timeScale = 0` 으로 엔진을 멈추고 팝업 하나씩: 레벨업 3택(Play_Perk_Selection_02 · 카드 수 = 제안 수 · 첫 카드 선택 → Taken 1) → 보유 특전 → 쉼터(경험치) → 악마(거절) → 악마의 선물(계속) → 천사(무료 축복) → 광고 카운트다운 1초 → 일시정지(재개) → 클리어(Play_Result_Win_01 · 로비로) → 사망(Play_Result_Lose · 로비로) → 엔진 재개 0.5초 → 로비.
  - 화면/팝업마다 `Check()`: ⓐ `PlayLog.AssertNoRed`(빨간 줄 0) + `Application.logMessageReceived` 로 잡은 **`[UiKit] 글자/이미지 없음`·`[AssetCatalog] … 없음` 경고 0**(잘못된 프리팹 자식 경로·없는 키 = 빈 그림) ⓑ 활성 Text 에 **데모 잔여 글자 0** — 표는 카탈로그 프리팹 100개의 YAML `m_text` + 중첩 인스턴스 `m_Modifications` 에서 뽑은 66개(«Text»·«Remain»·«Whisperwood»·«Touch to Continue»·«Player ID»·«Equip All»·«Stage Buff»·탭 «Shop/Hero/Battle/Research/Dungeon» …) · 숫자만인 것과 의도된 «ON/OFF»·«START»·«BOSS» 는 제외 ⓒ 핵심 요소 ⓓ 전투 3초. 배치 모드라 HeroView·월드 카메라는 매 프레임 `Camera.Render()` 로 직접 그린다(HeroViewTests 와 같은 방식).
  - `Assets/Tests/PlayMode/PlayLog.cs`(신규 · 공용): 빨간 줄만 모아 `AssertNoRed(where)`. HeroViewTests 도 이것을 쓴다.
  - `tools/check_unity_null.sh`(신규): `Assets/Scripts` 의 «`GetComponent…() ??`·`AddComponent() ??`·`Find…() ??`·`as RectTransform ??`» 0건(주석 줄 제외) — 있으면 `UiKit.Ensure<T>` 안내와 함께 exit 1. 일부러 넣은 샘플 2건이 잡히는 것 확인. `.github/workflows/ci.yml` dotnet 잡(#34 에서 초록 확인)과 ROUTINE §3 게이트에 추가.
- 기본값으로 정한 것(주인이 바꿀 것만): ⓐ 탤런트·펫 팝업은 데모 내용 그대로(T10 · 주인 «기능 없음»)라 **잔여 글자 검사만 뺐다**(빨간 줄·경고는 검사). ⓑ 상점 검사는 T9 의 Shop_List 구현에 맞춰 «상자 이름 3개가 보인다 · «1회»/«10회» 라벨 버튼 · 결과 = `ui.chestOpen`» 만 본다(레이아웃은 안 본다). ⓒ 전투 HUD 특전 줄(T13 진행 중)은 빨간 줄 0 만 본다(비례는 T13 의 `PerkStripTests`).
- 게이트(리베이스 뒤 T9 합류 상태에서 재실행): `dotnet build` 0/0 · `dotnet test` **50/50** · `gen_meta --check` · `gen_catalog --check`(461) · `check_catalog_keys` OK(547/460) · `check_unity_null` 0건 · `check_data_sync` OK(aaaw `0707999`). Sim 시드 검증은 Core 를 안 건드려 생략. PlayMode 파일 3개는 dotnet 이 못 돌리므로 UnityEngine 참조 어셈블리 + TestTools 스텁으로 **문법 컴파일만** 스크래치 프로젝트에서 확인(0 오류).
- **플레이 콘솔 에러 0 을 무엇으로 확인했는가**: 위 PlayMode `UiSmokeTests` 5개 + `HeroViewTests` 2개 — 코드 커밋 `ab9b192` 의 CI 유니티 잡(CI 런 #38 · https://github.com/kuzuni/aaawunity/actions/runs/33995925475)이 확인 수단. → **#38 결과(22:34 UTC): 유니티 잡 초록 — PlayMode 전부 통과(UiSmokeTests 5 · HeroViewTests 2 · DataLoader 1 · T13/T14 의 PlayMode 포함) · EditMode 전부 통과.** #36 에서 잡힌 PanelView 예외는 T15(`b001d5f`) 로 사라졌다. 그 뒤 WebGL(gh-pages)·Android 빌드 잡이 처음으로 테스트 게이트를 넘어 돌기 시작했다. #34(`bddcf98`)는 PlayLog 교체 전 코드라 #33 과 같은 이유로 빨갈 수 있다(무시). CI 러너(xvfb·Mesa)가 못 보는 것(실제 GPU 렌더 경고)은 여전히 주인 플레이가 최종.
- 워커 메모: dotnet 은 `apt-get update && apt-get install -y dotnet-sdk-8.0`(T8 메모 그대로). 테스트는 입력 장치 없이 `Button.onClick.Invoke()` 로 누른다(라벨 글자로 찾음) — 화면 코드에서 버튼 라벨을 바꾸면 이 테스트의 라벨도 같이 바꾼다. 데모 잔여 글자 표를 다시 뽑는 법: 프리팹 YAML 의 `m_text:` 와 `propertyPath: m_text` 다음 `value:` 를 긁으면 된다(스크립트는 레포에 안 넣었다 · 표는 테스트 상단 상수).
- 한계: 에디터 없이 짠 것 — ⓐ 첫 CI 런에서 «잔여 글자» 표가 실제 화면의 글자를 잡아내면 그것은 버그(고칠 것)이지 표의 오류가 아니다 · 표에는 우리 코드가 바꾸는 것이 확실한 항목(«Name» = 장비 제목 자리 · «Reward» = 뽑기 결과 제목 등)만 넣었다. ⓑ 배치 모드에서 `Time.timeScale = 0` 동안 DOTween(`SetUpdate(true)`)·토스트·카운트다운은 unscaled 로 돈다.

### T13 완료 기록 (2026-09-05 · sess-2206-21029 · 워커 A)

- **주인이 확인할 것 (한 줄)**: 전투에서 특전을 10개 이상 얻은 뒤 하단(책 버튼 왼쪽) 미리보기 줄이 — 팔각 아이콘이 **줄 높이보다 조금 작은 정사각**(레퍼런스 28/34)으로 한 줄에 가지런히 놓이고, 서로 겹치거나 책 버튼을 덮지 않으며, 다 못 들어가는 만큼 «+N» 으로 접히고, 같은 특전을 두 번 얻으면 오른쪽 위에 작은 숫자가 붙는가. 또는 CI 유니티 잡의 아티팩트 **`perkstrip-screens`**(`perkstrip-03.png` = 3개 · `perkstrip-12.png` = 12개+중복) 를 내려받아 보면 된다.
- **겹침 원인 확정(한 줄)**: 등재 관찰의 두 후보 중 **ⓑ 프리팹 내부**가 원인 — `ItemFrame_04_BasePrefab` YAML 은 루트 162×165 에 자식 Border 162×164 · InnerBorder/InnerBg 134 · Icon 128 · Light/Shadow 45×46(±53) 이 전부 **가운데 앵커 고정 크기**(스트레치는 Bg 하나뿐)라, `UiKit.PerkFrame` 이 루트 `sizeDelta` 를 76 으로 줄여도 테두리·아이콘은 162px 그대로 그려졌다 → 78+8 피치 위에 162px 프레임 = 이웃을 절반 넘게 덮음(주인: «너무 커서 서로 가린다»). ⓐ 폭 넘침(11×78+10×8 = 938 > 864)도 사실이지만 그건 책 버튼 쪽으로 밀리는 문제고, 아이콘끼리 겹친 직접 원인은 아니었다(둘 다 고침). 프리팹 자식은 루트 rect 밖으로 안 뻗는다(Light/Shadow 모서리 ≤ ±76 < ±81) → RectMask2D 불필요.
- 만든 것
  - `UiKit.PerkFrame`: 프리팹 본래 크기(162×165)를 두고 **배율**(`size/162`)로 맞춘다 — 프리팹 «그대로»(내부 요소 손 안 댐) · 특전 카드(Overlay · 162)는 배율 1 로 종전과 동일.
  - `Core/Layout.PerkStripSpec`(순수 C# · `HudPerkStrip` 표값은 그대로): 줄의 **실제 폭·높이**를 받아 셀 = 28/34 · 간격 = 4/34 · 개수 배지 = 14/34(글자 10/34) · «+N» 안쪽 7/34 · 글자 12/34 를 비례로 계산(픽셀 상수 없음 · 해상도가 달라도 유지). `Shown(total)` = 줄 폭 ÷ (셀+간격), 다 안 들어가면 «+N» 칸 폭(안쪽 ×2 + 글자 폭 추정)까지 빼고 계산 → `UsedWidth ≤ Width` 를 0~100개 전부 EditMode 로 단언.
  - `BattleScreen.RefreshPerkStrip`: `PerkStripMetrics(rect)`(레이아웃 전이면 화면 루트 → 프레임 상수 순으로 대체) → 셀 정사각 · `HorizontalLayoutGroup.spacing` = 간격 · 배지는 셀 오른쪽 위 모서리 안쪽(줄 밖으로 안 나감) · «+N» 은 높이 = 셀. 다시 그리기 키에 줄 크기(폭×높이)를 넣어 첫 프레임(레이아웃 전)에 만든 줄이 다음 프레임에 스스로 맞춰진다. 1080×2337 에서 셀 ≈ 77 · 간격 ≈ 11 · 9개까지 그대로, 10개부터 8개 + «+N».
  - 테스트: EditMode `PerkStripSpecTests` 5개(비례 = CSS 값 · 해상도 배율 · 0~100개 넘침 0 · 딱 들어가면 «+N» 없음/하나 더면 접힘 · 0/좁은 줄 안전) — dotnet 이 돈다. PlayMode `PerkStripTests` 1개: 실제 씬 전투에서 `G.Taken` 에 3개 → 12개+중복 1 → 1개 순으로 넣고 한 프레임 뒤 `RectTransformUtility.CalculateRelativeRectTransformBounds` 로 ⓐ 셀·배지·«+N» 전부 PerkStrip rect 안 ⓑ 이웃끼리 안 겹침(간격 = 4/34) ⓒ 셀 높이/줄 높이 = 28/34 ±0.02 · 프레임(자식 포함)이 셀 안·셀을 채움 ⓓ 줄이 책 버튼(PerkBook) 을 안 덮음 ⓔ 보이는 수 = `PerkStripSpec.Shown` · «+N» 글자 · 배지 «2» · 책 개수 «13» 을 단언하고, 빨간 줄 0(`PlayLog.AssertNoRed` · T11 규약) · 경로/키 경고 0.
  - **스크린샷(주인 지시)**: 같은 테스트가 UI 캔버스를 잠시 ScreenSpaceCamera 로 돌려 **RenderTexture 타깃** 카메라(HeroView 와 같은 D24S8 규칙 · 월드 카메라 설정 복사 → 전투 배경 포함)로 540×1169 PNG 를 `Application.temporaryCachePath/perkstrip-screens/` 와 프로젝트 루트 `perkstrip-screens/`(CI 워크스페이스) 두 곳에 남긴다 → `ci.yml` 유니티 테스트 잡의 `actions/upload-artifact` **`perkstrip-screens`**(파일 없으면 조용히 건너뜀). 에디터에서 돌리면 `-gameview.png`(ScreenCapture)도 함께. `.gitignore` 에 `/perkstrip-screens/`(레포 커밋 금지). 캡처가 실패해도 기하 단언은 별도라 테스트는 안 깨진다(경고 1줄).
- 게이트: `dotnet build` 0/0 · `dotnet test` **55/55**(+5) · `gen_meta --check` · `gen_catalog --check`(461) · `check_catalog_keys` OK(547/460) · `check_unity_null` 0건 · `check_data_sync` OK(aaaw `0707999`) · Sim 시드 11·12·13 사다리 21칸 = T2 표와 동일(Core 는 순수 계산 struct 추가뿐 · 엔진 무변경). PlayMode 테스트는 dotnet 이 못 돌리므로 스크래치 프로젝트(TestTools·`FindObjectsByType` 스텁)로 **컴파일만** 확인했다(커밋 안 함).
- **플레이 콘솔 에러 0 을 무엇으로 확인했는가**: PlayMode `PerkStripTests.TwelvePerksFitWithoutOverlapAndScreenshot`(전투 진입 → 특전 3/12+1/1 → 로비 · 단계마다 `PlayLog.AssertNoRed`) — 코드 커밋 `50860f2` 의 CI 유니티 잡 = **CI 런 #37**(https://github.com/kuzuni/aaawunity/actions/runs/33995640935 · workflow_dispatch · head `22a7197` = 코드 포함)이 확인 수단 — 코드 커밋과 `[skip ci]` 문서 커밋을 **한 push 에 묶어** 올리는 바람에 push 머리 커밋의 `[skip ci]` 로 push 전체가 건너뛰어져(GitHub 규칙) 수동으로 큐잉했다(교훈은 ROUTINE §1 에 한 줄). **결과(이 세션이 22:47 UTC 에 읽음)**: #37 은 유니티 잡 빨강 — 그러나 `perkstrip-screens` 아티팩트(PNG 2장 · https://github.com/kuzuni/aaawunity/actions/runs/33995640935/artifacts/9978063845)는 올라왔고, 그 head(`22a7197`)는 T15(`UiKit.Spawn` 중 `PanelView.OnEnable` 예외 · CI #36 부터 PlayMode 3건)가 고쳐지기 전이다. T15 수정 `b001d5f` 를 포함한 **CI 런 #38(https://github.com/kuzuni/aaawunity/actions/runs/33995925475) 유니티 잡 초록** = PlayMode 전부 통과(`PerkStripTests` 포함) · 아티팩트 `perkstrip-screens` 57KB(https://github.com/kuzuni/aaawunity/actions/runs/33995925475/artifacts/9978150369) → ✅ 확인 수단 충족. 주인은 #38 의 아티팩트를 내려받아 보면 된다. 런타임 카메라는 테스트 안에서만 만들고(RenderTexture 타깃 · 끄고 → targetTexture null → 파괴 순서) 게임 코드엔 새 카메라·RenderTexture 없음. 최종은 **주인 에디터 플레이**(위 «주인이 확인할 것»).
- 워커 메모: 리베이스로 코드 커밋 해시가 `3b8558c → 50860f2` 로 바뀌었다(문서 커밋 `22a7197` 제목의 3b8558c 는 이것). dotnet 은 `apt-get update && apt-get install -y dotnet-sdk-8.0`(T8 메모 그대로). 프리팹 rect 트리는 YAML 을 직접 파싱해(scratchpad 스크립트 · 커밋 안 함) 변형 → 베이스(`Shared/Prefabs_CommonBase/Base_Frame/ItemFrame_04_BasePrefab`) 순으로 읽었다. 작업 중 워커 D(T11)가 `LogAssert.NoUnexpectedReceived` 금지·월드 카메라 수동 렌더 금지를 올려 그 규약대로 고쳐 실었다.
- 한계: 에디터 없이 짠 것 — ⓐ «+N» 칸 폭은 글자 폭 추정(글꼴 ×0.62/자)이라 Jua 에서 글자가 더 넓으면 칸 밖으로 살짝 삐져 보일 수 있다(줄 밖으론 안 나감 · 거슬리면 `PerkStripSpec.MoreWidth` 의 0.62 한 줄) ⓑ 배지는 배경 없는 흰 글자+외곽선(index.html 은 등급색 원) — 프리팹에 없는 것을 새로 그리지 않는다는 원칙대로 두었다 ⓒ CI 러너가 `-nographics` 면 PNG 가 검게 나올 수 있다(그때는 주인 에디터에서 테스트를 돌리면 `-gameview.png` 가 남는다).

### T14 완료 기록 (2026-09-05 · sess-2220-32398 · 워커 B)

- **주인이 확인할 것 (한 줄)**: 전투에서 — 플레이어·적·보스가 전보다 **2/3 크기**(발밑 체력바·실드바 폭도 같이 줄고 높이는 그대로)인가 / 공속이 빠른 빌드(공격 간격 0.6초 미만)에서도 칼질 모션이 **다음 공격 전에 끝까지 나오고** 칼이 내려오는 순간에 숫자가 뜨는가(느린 빌드는 전과 같은 1배) / 플레이어가 죽으면 쓰러진 자세로 **그대로 멈추고**(다시 일어나지 않음) 사망 팝업 아래에서도 그대로인가 / 보스를 잡으면 승리 모션이 한 번 나오고 마지막 자세에서 멈추는가.
- 만든 것
  - `Core/Layout`: `CharScale = 2/3` + `CharHeightPct(표%)` — 표 상수 `PlayerHeight`/`EnemyHeight`(9%)는 그대로(LayoutSpecTests 가 ref-layout 표와 대조) · 그리는 쪽이 곱한다. `AttackAnimSpeed(clipLen, interval) = max(1, 클립 ÷ 간격)` 순수 함수(상한 없음 · 0 나눗셈만 막음).
  - `BattleWorld.MakeChar`: 키 = 표 % × CharScale(플레이어 9%→6% · 적 9%→6% · 보스 9%×sizeMul×2/3). 발밑 바 = 플레이어 `PlayerFootBarW` × 2/3 · 적/보스 `ui.json enemyBarW/bossBarW` × 2/3(높이 `FootBarH` 그대로). 스턴 이펙트는 리그 자식(로컬 좌표)이라 같이 줄어든다.
  - `CharacterRig.PlayAttack`: 예전 `Clamp(클립/max(0.2, 간격), 1, 3)` → `Layout.AttackAnimSpeed`. 플레이어 간격 = 1/EffAspd · 적 = melee/boss/rangedInterval × 슬로우 배율(BattleWorld.AfterTick · 기존 그대로 · 확인만). 타격 순간 `HitDelay = OnAttackHit(1.0초) ÷ 속도` 라 같은 배율로 앞당겨진다(PlayMode 테스트가 간격 0.3초에서 HitDelay ≤ 0.3 단언).
  - **사망 모션 루프 금지**: CharacterMaker 클립은 **전부** `m_LoopTime: 1`(Dead1 1.0초 · Dead2/3 · Victory 1.33초 · Defeat 3초 · Idle/Walk/Stun 도) — 에셋은 손대지 않고 `CharacterRig.Update` 가 Dead1/Victory/Defeat 재생 중 Animator 의 자기 시계(`GetCurrentAnimatorStateInfo.normalizedTime` + 이번 프레임 진행분)로 «감기기 전» 에 `Play(state, 0, 0.999)` + `speed = 0` → 첫 프레임(일어선 자세)이 한 번도 안 보인다. `Tick`/`SetSpeed` 는 멈춘 동안 속도를 되살리지 않고, 다른 상태(Idle/Walk/Attack)를 Play 하면 풀린다. 월드 틱(`Tick`) 이 아니라 Animator 시계를 쓰므로 팝업이 떠 있어도(사망 팝업 아래) 정확히 클립 끝에서 멈춘다. 적은 사망 0.85초 뒤 제거되므로(Dead1 1.0초) 종전에도 안 감겼고, 플레이어(사망 팝업 아래 계속 보임)·승리가 대상.
  - 테스트: EditMode `CharScaleAnimTests` 4개(배율 2/3 · 표 상수 불변 · 간격 안에 끝남(6개 간격) · 상한 없음(0.3초 → 6.1배 · 0.05초 → 36배) · 퇴화 입력 유한) — dotnet 이 돈다. PlayMode `CharacterRigTests` 3개: ① 전투 1초 뒤 플레이어 `localScale` = `PctH(9%×2/3)/CharBaseHeight` · 적 = 일반/보스 둘 중 하나 · `BarBg` 폭 = 배율 값이 있고 예전 값은 없음 ② 단독 리그(`cm.character`) — `PlayAttack(0.3)` 속도 = 클립÷0.3(>3) · HitDelay ≤ 0.3 · `PlayAttack(5)` = 1 · `Dead` 1.8초 뒤 `Frozen`·speed 0·`Dead1` 상태·normalizedTime ∈ [0.9, 1) · 0.6초 더 지나도 진행 없음 · `Idle` 로 풀림 · `Victory` 도 정지 · `Walk` 는 안 멈춤 ③ 실제 전투에서 체력 1 로 두고 첫 타격 사망 → 사망 팝업이 열린 채 `Frozen`·`Dead1`·1초 뒤에도 normalizedTime 불변 — 단계마다 `PlayLog.AssertNoRed`(T11 규약).
- 기본값으로 정한 것(주인이 바꿀 것만): ⓐ 데미지 팝·이펙트의 세로 오프셋(`EnemyPos(up 0.45)`·`PlayerPos(up 0.5)`·사망 이펙트 +0.4u)은 월드 단위 고정이라 캐릭터가 작아진 만큼 머리 위 여백이 조금 넓어 보인다 — 거슬리면 `BattleWorld` 의 그 상수들에 `Layout.CharScale` 을 곱하는 한 줄. ⓑ `Defeat` 클립은 현재 아무 데서도 안 쓰지만(사망 = Dead1) ROUTINE 대로 같은 정지 처리에 넣어 두었다. ⓒ 정지 지점 0.999(마지막 키 59/60 = 0.983 뒤 · 1.0 은 처음으로 감김).
- 게이트(리베이스 뒤 T15 합류 상태에서 재실행): `dotnet build` 0/0 · `dotnet test` **59/59**(+4) · `gen_meta --check` · `gen_catalog --check`(461) · `check_catalog_keys` OK(547/460) · `check_unity_null` 0건 · `check_data_sync` OK(aaaw `0618225`) · Sim 시드 11·12·13 사다리·3pick 21칸 = T2 표와 동일(Core 는 순수 상수/함수 추가뿐 · 엔진 무변경). PlayMode 파일은 dotnet 이 못 돌리므로 스크래치 프로젝트(TestTools 스텁)로 **컴파일만** 확인(0 오류 · 커밋 안 함).
- **플레이 콘솔 에러 0 을 무엇으로 확인했는가**: PlayMode `CharacterRigTests` 3개(전투 진입 → 크기 검사 → 로비 / 단독 리그 공격·사망·승리 / 전투 사망 → 팝업 → 로비 · 단계마다 `PlayLog.AssertNoRed`) + 기존 `UiSmokeTests.BattleTicksAndAllBattlePopups` — 코드 커밋 `0ee1e18` 의 CI 유니티 잡이 확인 수단(이 세션은 push 뒤 종료하므로 **다음 워커가 CI 런을 읽어** 빨간 잡이면 고친다 · 같은 런에 T15 `b001d5f` 의 UiKit.Spawn 수정도 함께 들어간다). 게임 코드에 새 카메라·RenderTexture·코루틴 없음(Update 하나 · null/비활성 가드). 최종은 **주인 에디터 플레이**(위 «주인이 확인할 것»).
- 워커 메모: dotnet 은 `apt-get update && apt-get install -y dotnet-sdk-8.0`(T8 메모 그대로 · 3분). 클립 루프·길이는 `Assets/Layer Lab/2D Minimal-CharacterMaker/Common/Animations/*.anim` 의 `m_LoopTime`/`m_StopTime` 을 직접 읽었다(전부 루프 · Attack 1.8333 · OnAttackHit time 1.0).
- 한계: 에디터 없이 짠 것 — ⓐ `Animator.Play(state, 0, 0.999)` 뒤 `speed = 0` 이 실물에서 마지막 키 자세를 보이는지(이론상 0.983 이후 마지막 키 유지) 는 CI/주인이 확인 · 감기는 한 프레임이 보이면 `FreezeAt` 한 줄 ⓑ 2/3 크기에서 발밑 바가 캐릭터보다 넓어 보이면 `Layout.CharScale` 이 아니라 `PlayerFootBarW`/`enemyBarW` 배율을 따로 두는 한 줄(지금은 «같은 비율» 지시 그대로).

### T15 완료 기록 (2026-09-05 · sess-2136-22274 · 워커 C) — CI 확인 중

- **주인이 확인할 것 (한 줄)**: 에디터 플레이 → 로비 메뉴(≡) 설정 팝업 · 장비 칸 세부 팝업 · 전투 일시정지/레벨업 팝업을 열 때 콘솔에 `UnassignedReferenceException: The variable otherPanels of PanelView has not been assigned` 가 **더 이상 안 뜨는지**.
- **원인 확정(한 줄)**: `UiKit.Spawn` 이 `Instantiate(prefab, parent, false)` 를 활성 부모 밑에 해서 GUI Pro 데모 스크립트 `LayerLab.CasualGame.PanelView.OnEnable`(`otherPanels[i].SetActive` · 배열 미할당)이 `Adopt` 의 스크립트 제거보다 **먼저** 실행됐다 — CI #36(https://github.com/kuzuni/aaawunity/actions/runs/33995378223) `UiSmokeTests` 3건(LobbySettingsTalentPetToast · GearScreenDetailSlotAndEquip · BattleTicksAndAllBattlePopups)의 스택이 전부 `PanelView.OnEnable ← Object.Instantiate ← UiKit.Spawn(UiKit.cs:205) ← Overlay.SettingsPopup` 이다. 상점(`ShopBoxesAndChestOpenPopup`)·대장간·HeroView 2건은 같은 런에서 Passed.
- 수정(`b001d5f` · 에셋 불변): `Spawn` = 비활성 홀더 `UiKit.Staging`(씬 루트 · 파괴되면 재생성 · 도메인 리로드 끔 대비 `ResetStatics`) 밑에 인스턴스화 → `StripDemoScripts`(PanelView·PanelControl `DestroyImmediate`) → `Adopt` → `SetParent(parent, false)`. 데모 스크립트의 OnEnable/Awake 가 한 번도 돌지 않는다. `adopt=false` 경로도 스크립트 제거는 항상.
- 게이트: `dotnet build` 0/0 · `dotnet test` 55/55 · `gen_meta --check` · `gen_catalog --check`(461) · `check_catalog_keys` OK · `check_unity_null` 0건.
- **플레이 콘솔 에러 0 을 무엇으로 확인했는가**: 코드 커밋 `b001d5f` 의 CI 유니티 잡(PlayMode `UiSmokeTests` 3건이 초록으로 바뀌는가) — 이 세션이 결과를 읽어 여기에 런 번호를 적는다.

## 주인 콘솔 에러 보고함 (주인이 붙인 원문 — 다 고칠 때까지 남긴다 · 워커는 매 세션 읽고 작업으로 올린다)

> 주인 상시 지시(2026-09-05): **플레이하면 콘솔에 빨간 에러가 항상 뜬다. 루틴이 매번 플레이 상태를 검증해 다 고쳐라. 다른 에러도 있는지 전부 확인해라.** 새 로그는 아래에 번호를 이어 붙이면 된다(원문 그대로).

| # | 원문(발생 상황) | 상태 | 작업 | 원인/커밋 |
|---|---|---|---|---|
| ① | `Renderer2D Pass: Fake or uninitialized surface is not supported for attachment 0.`<br>`UnityEngine.Rendering.RenderPipelineManager:DoRenderLoop_Internal (UnityEngine.Rendering.RenderPipelineAsset,intptr,UnityEngine.Object,Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle)` — 에디터 플레이 중 반복 | ✅ 수정 (`2203550`) — 주인 에디터 재확인 대기 | T12 | **확정**: `HeroView.BuildStage` 가 `new RenderTexture(512, 512, 0, ARGB32)`(깊이 0 · `depthStencilFormat None`) 을 런타임 카메라 `targetTexture` 로 썼다. 프로젝트의 유일한 렌더러 `Renderer2D.asset` 은 `m_UseDepthStencilBuffer: 1` 이라 렌더그래프가 카메라 타깃의 깊이 표면을 attachment 로 import 하는데 그 표면이 없어(«Fake or uninitialized surface») 렌더패스 시작이 실패한다. 이 카메라는 로비·장비 화면에서 매 프레임 그리므로 «항상» 뜬다. 수정 = `HeroView.CreateTargetTexture`(깊이 24 + `GetDepthStencilFormat(24, 8)` = D24S8/D32S8) + `UniversalAdditionalCameraData`(Base) 명시. 회귀 방지 = `HeroViewTests.StandaloneHeroViewRendersWithoutErrors`(depth>0 · depthStencilFormat≠None 단언 + `Camera.Render()` 3프레임 · 빨간 줄 0) — **CI #33 에서 Passed · 로그에 이 에러 문자열 0건**. |
| ② | `EndRenderPass: Not inside a Renderpass`<br>`UnityEngine.Rendering.RenderPipelineManager:DoRenderLoop_Internal (…)` — ① 과 짝으로 매 프레임 | ✅ 수정 (`2203550`) — 주인 에디터 재확인 대기 | T12 | ① 과 같은 원인(렌더패스 시작이 실패한 뒤 End 가 불려 짝이 안 맞음) — ① 이 없어지면 같이 사라진다. 추가로 `HeroView.OnDestroy` 순서를 «카메라 끔 → targetTexture null → RawImage 끔 → 무대 파괴 → Release → Destroy» 로 고정해 해제된 텍스처를 파이프라인이 만지지 않게 했다. |
| ③ | (주인: «다른 에러들도 있는지 다 확인해서 고쳐라» — 위 둘 외 미기재 항목 전수 감사) | ✅ 감사 완료 (`2203550` · 결과는 T12 완료 기록) — 새로 확인된 빨간 줄 원인 0 · 재발 방지 게이트 2개(`check_catalog_keys.py` · PlayMode 왕복 테스트) | T12 §3 | 감사 결과: 실제 플레이에서 빨간 줄을 내는 곳은 ①② 뿐. 나머지는 «데이터/에셋이 빠졌을 때만»(카탈로그 미연결 · 프리팹 자식 경로 오타 → 노란 경고) 이거나 DOTween safeMode(켜짐 · `useSafeMode: 1`)가 조용히 kill 하는 트윈. 프리팹 경로 경고는 T11 스모크 테스트가 화면마다 잡는다. |

## 주인 할 일

- README «내가(주인) 할 일» 5단계 (활성화 워크플로 → .alf → .ulf → 시크릿 3개 → Pages 소스 gh-pages).

## 게이트 현황 스냅샷 — T5 완료 직후

| 게이트 | 결과 |
|---|---|
| `dotnet build tools/dotnet/KkomaKnight.sln -c Release` | 0 경고 · 0 오류 (Core · Game · Tests · Sim) |
| `dotnet test tools/dotnet/Tests` | 40/40 (NUnit 3.6.1 — 유니티 포크와 같은 API 면 · 3.5.0 은 net8 어댑터가 테스트를 못 찾는다) |
| 유니티 CI (#14) | EditMode 32/32 · PlayMode 1/1 · WebGL → gh-pages · Android APK 아티팩트 (T5 커밋의 CI 는 푸시 뒤 확인) |
| `python3 tools/gen_meta.py --check` | 초록 |
| `tools/check_data_sync.sh` | OK — aaaw main `c7ebe37` 과 동일 (`sim.js@0618225…`) |
