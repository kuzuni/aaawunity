# 루틴 작업 지시서 (병렬 워커 공용 — 유일 지시서)

> 이 문서와 aaaw 의 `PLAN.md`(스펙 정본 · 읽기 전용) 만 보고 작업한다. 보고·기록은 전부 **한국어**.
> 이 레포는 **aaaw 의 HTML 게임을 유니티로 이식**하는 곳이다. 게임 규칙·수치는 aaaw 가 정본이고 여기서는 **바꾸지 않는다**.

## ⚑ 신규 주인 지시 (위 항목이 최신)

- **(2026-09-06 · 11:4X UTC) ⚑⚑ 주인 지시 — 게임 전체적으로 글씨가 너무 작아 안 읽힌다. 가독성 있게 다 바꿔라.** → **T63** 등재(§2 · UiKit 최소 글자 크기 하한(본문 40 · 버튼 44 · 보조 36 · 제목 60 · bestFit min 32) + 화면별 전수 점검 + 잘림 0 게이트 · 화면 단위 하위 lock · 루틴이 잡는다).

- **(2026-09-06 · 11:3X UTC) ⚑⚑ 주인 지시 — 랭킹(아레나 순위 · `23`) UI 가 유난히 안 맞는다. GUI Pro 에 거의 같은 프리팹(`Social_Ranking` · `ListItem_Ranking`)이 있으니 그걸 조금 변형해서 쓰라.** → **T62** 등재(§2 · T43 뒤 · 비평 8.0). **같은 시각 주인 확인: 폰에서 게임이 «잘 된다»** — gh-pages 는 여전히 20b11aa(d6f66eb) 그대로이므로 T59 크래시는 같은 빌드에서 일시적으로 난 것 → T59 는 원인 기록 + T60 스모크 게이트로 마무리(아래 T59 메모).

- **(2026-09-06 · 11:1X UTC) ⚑⚑ 주인 지시 — 특전 카드가 순서대로 등장할 때 «Shine» 효과(AllIn1SpriteShader)도 카드 순서대로.** → **T61** 등재(§2 · T49 stagger 위에 · UI 용 `AllIn1SpriteShaderUiMask` 머티리얼 · 루틴이 잡는다).

- **(2026-09-06 · 10:4X UTC) ⚑⚑⚑ 주인 지시 — gh-pages 배포(첫 WebGL 배포 · main d6f66eb)를 폰에서 열었더니 첫 화면에서 «RangeError: Maximum call stack size exceeded»(wasm 무한 재귀). 앞으로 **항상 배포·커밋·push 전에 에러를 확인하고, 배포된 게임에 들어가 봐서도 에러가 뜨는지 확인하고 고쳐라** — 상시 규칙(§1).** → **T59**(크래시 수정 · 최우선) + **T60**(배포 스모크 게이트: headless 브라우저로 콘솔 에러 0·로비·전투 확인 뒤에만 배포) 등재(§2).

- **(2026-09-06 · 09:2X UTC) ⚑⚑ 주인 지시 — 특전 설명은 «트리거: 내용» 꼴로: «처치 시: 33% 확률로 …» · «피격 시: …» · «3타마다: …».** → **T53** 등재(§2 · 표시 시점 변환 · perks.json 불변 · T52 뒤 · 루틴이 잡는다). **상시 능력치는 «패시브: …»**(09:3X 정정).

- **(2026-09-06 · 09:2X UTC) ⚑⚑ 주인 지시 — 특전 글씨에 색을 섞지 않는다(수치만 연두색 = 안 읽힘). T36 «수치 초록» 취소 · 특전 설명은 한 색.** → **T52** 등재(§2 · Overlay.GreenNumbers 호출 제거 · 제약 없음 · 루틴이 잡는다).

- **(2026-09-06 · 09:1X UTC) ⚑⚑ 주인 지시 — ① 특전 «처치 시 대시» 도 «공격 모션 끝 → 그다음 ×5 걷기»(T50 5항 «대시는 바로 출발» 을 뒤집음) · ② 적이 죽을 때 «펑» 터지는 이펙트(fx.death) 없애기.** → **T51** 등재(§2 · T50 뒤 · 같은 BattleWorld.Sync · T50 워커가 이어 잡아도 됨).

- **(2026-09-06 · 09:0X UTC) ⚑⚑ 주인 지시 — 적을 죽인 뒤에는 «공격 모션이 끝나고 → 걷기 모션이 나오면서 → 원래 걷기 속도(132)로» 다음 적까지 간다. 지금의 2배 따라잡기(T20 · CatchUpMul=2) 폐지. 특전 «처치 시 대시»(×5)는 그대로.** → **T50** 등재(§2 · BattleWorld.Sync 표시 원점만 · 엔진 불변 · 제약 없음 · 루틴이 잡는다).

- **(2026-09-06 · 08:1X UTC) ⚑⚑ 주인 지시 — 팝업 등장 연출을 DOTween 으로 «순서대로»: 레벨업 특전 카드가 뜰 때 하나씩 순서대로, 졌을 때 팝업·이겼을 때 팝업도 같은 식 연출.** → **T49** 등재(§2 · Overlay.LevelUp·PerkBook·Clear·Dead · 제약 없음 · 루틴이 잡는다).

- **(2026-09-06 · 07:5X UTC) ⚑⚑⚑ 주인 지시 — UI 는 «비평하면서» 만든다. 화면마다 레퍼런스와 대조해 10점 만점으로 채점하고 8점이 될 때까지 고친다.** 채점 기준은 **레이아웃 · 비례 · 비율**(레퍼런스와 최대한 같아야 함). **아이콘·그림이 안 비슷한 것은 감점 아님 — 새로 만들지 말고 일단 에셋 안에 있는 것으로**(주인 원문: «아이콘이 안 비슷해서 새로 생성할 필요는 없음 · 걍 에셋 내에 있는 거로»). → **T46**(비평 하니스: 전 화면 PNG + layout.json → `screens` 브랜치 · `tools/ui_score.py`) 을 먼저 만들고, **T34~T44 전부 «비평 점수 ≥ 8.0/10» 이 ✅ 조건**이다(T34·T35 는 이미 코드가 끝났으니 **T47** 에서 재비평). 절차는 §5 «UI 비평 회차».

- **(2026-09-06 · 06:4X UTC) ⚑⚑⚑ 주인 지시 — UI 는 무조건 `docs/ref/*.jpg`(주인 «올빼미» 폴더 26장 · 색인 `docs/ref/README.md`) 기준으로, 최대한 비슷한 레이아웃 느낌으로 만든다. 이전에 내린 UI 지시들(«데모 프리팹 그대로» — Lobby_Default·Character_Hero_Equipment·Shop_List·Settings·Character_Talent_02·Character_Hero_Item_Detail_01(T27)·World_Dungeon_List(T30)·Character_Skill(T32) «그대로 써») 은 전부 이것으로 대체한다.** → **T34~T44** 등재(§2) · T25 는 T37 에 흡수 · **T27·T30·T32 는 폐기**(잡지 않는다 · 탭 «탤런트→던전» 만 T43 이 이어받음). 원칙:
  - ⓐ **배치·비율·구도의 정본 = 레퍼런스 jpg**(프레임 % 로 ±3%p · ①~⑦ 은 `docs/ref-layout.md` 의 실측표가 그대로 유효 · 나머지 화면은 워커가 jpg 에서 직접 잰다). 화면의 «느낌»(어떤 요소가 어디에 · 어떤 크기 비례로 · 어떤 순서로) 을 맞춘다 — 색·폰트·그림체는 여전히 점수 밖이지만 버튼 색 규칙(주=주황 · 보조=회색 · 광고/정보=파랑)은 따른다.
  - ⓑ 그림 재료는 여전히 **주인 에셋만**(GUI Pro 스프라이트·프리팹 조각 · CharacterMaker · Environment). 데모 프리팹은 «그대로 세우는 것» 이 아니라 **부품으로 뜯어 레퍼런스 구도로 다시 조립**한다 — 요소 이동·숨김·복제·크기 변경 전부 허용 · 코드 도형·새 그림 그리기는 금지. 어느 조각을 썼는지 `docs/assets-map.md` 에 남긴다.
  - ⓒ 글자·수치·개수는 우리 데이터(한국어 · 영문 데모 문구 0). 수치는 JSON 에서.
  - ⓓ 화면마다 T11 PlayMode 스모크가 열고 빨간 줄 0 · 핵심 요소 개수 단언을 레퍼런스 기준으로 갱신한다(예: 로비 사이드 아이콘 3+3 · 전투 스탯 8칸 · 상점 상자 3 + 다이아 6 + 골드 3). 프리팹 이름으로 «그대로» 를 단언하던 테스트는 구도 단언으로 바꾼다.
  - ⓔ 시스템이 없는 화면(특권·퀘스트·출석·데일리 기프트·7일 챌린지·패스·던전·아레나·펫)은 **레이아웃 껍데기**(레퍼런스 구도 · 우리 데이터 없으면 레퍼런스의 글자를 한국어로 · 버튼은 눌려도 아무 일 없음 · 닫기만) 로 만든다(T42·T43·T44). 기능은 만들지 않는다.

- **(2026-09-06) ⚑⚑ 주인 지시 — 전투 화면의 HP 바·실드 바는 «메인 게임화면»(`02_battle.jpg`·`03_battle_enemy.jpg`) 처럼 되어 있어야 한다.** HUD 아래에 **EXP(초록 라벨 · 0/6) · ❤ HP(빨강 · 1055/1055) · 🛡 실드(파랑 · 2258/2258) 세 바가 한 줄**(각 바 안에 «현재/최대» 숫자) · 적·플레이어 **발밑은 빨강(HP) 위에 파랑(실드) 2단 숫자 바**(숫자가 바 안에 흰 글자 · 바 폭은 캐릭터 폭) · 적 조우 시 상단 챕터 진행바가 주황으로 찬다. → **T35** 에 포함(최우선 항목).

- **(2026-09-06) ⚑⚑⚑ 주인 지시 — «주인 승인 대기» 폐지. 루틴은 주인 승인·허락을 기다리지 않는다 — 자동 승인으로 보고 알아서 정해 고친다.** 판단이 필요한 것은 워커가 그 자리에서 정해 **바로 적용**하고, 무엇을 왜 그렇게 정했는지 PROGRESS «워커 결정 기록» 에 한 줄 남긴다(주인이 나중에 뒤집으면 그때 고친다). 열려 있던 승인 대기 10~30 도 기다리지 않는다 — **주인이 원한다고 적힌 것**(24 간격 2배 · 27 투사체 무작위 표적 · 28 «그냥 받기» 유지 등)은 그대로 실행(시드 골든이 깨지면 aaaw 사본의 sim.js 를 같은 규칙으로 고쳐 골든을 다시 뽑고 PROGRESS 에 표를 남긴다 · aaaw 원본은 여전히 불변), **워커가 제안한 것**은 각 항목의 «기본값» 으로 확정. 남는 금지는 셋뿐: aaaw 레포 수정 · `data/*.json` 손대기 · 주인이 시키지 않은 밸런스 수치 변경.

- **(2026-09-05 · 21:4X UTC) ⚑ 주인 지시 — 전투 화면 하단 «얻은 특전 미리보기 줄»(PerkStrip) 의 아이콘이 너무 커서 서로 가린다. 비례를 레퍼런스에 맞추고, 스크린샷을 찍어 가며 확인하라.** → T13 등재(수정·검증은 워커).

- **(2026-09-05 · 21:2X UTC) ⚑⚑⚑ 주인 지시 — 플레이하면 유니티 콘솔에 빨간 에러가 «항상 존나» 뜬다. 루틴이 매번 플레이 상태를 검증해 콘솔 에러·예외·경고(빨간색)를 전부 찾아 고쳐라. 한 번이 아니라 상시 규칙이다.** 주인이 붙인 원문 로그(PROGRESS «주인 콘솔 에러 보고함» 에 그대로 보존):
  - `Renderer2D Pass: Fake or uninitialized surface is not supported for attachment 0.` / `EndRenderPass: Not inside a Renderpass` (둘 다 `UnityEngine.Rendering.RenderPipelineManager:DoRenderLoop_Internal`) — 플레이 중 매 프레임.
  - 등재 세션 진단(수정은 워커가 · T12): `HeroView.BuildStage` 가 `new RenderTexture(texSize, texSize, 0, ARGB32)`(깊이 0) 를 런타임 카메라 `targetTexture` 로 쓰는데, `Assets/Settings/Renderer2D.asset` 은 `m_UseDepthStencilBuffer: 1` 이라 URP 2D 렌더그래프가 없는 깊이 표면을 attachment 로 붙이려다 실패 → 두 에러가 짝으로 뜬다. 후보 수정 = RenderTexture 에 깊이 24(또는 `depthStencilFormat = D24_UNorm_S8_UInt`) 부여 · 런타임 Camera 에 `UniversalAdditionalCameraData`(Base) 보장 · `OnDestroy` 순서(카메라 비활성 → targetTexture 해제 → Release). HeroView 는 로비(T6)·장비(T7) 두 곳에서 쓰인다.
  - **«다른 에러들도 있는지 다 확인해서 고쳐라»** — 위 두 개만이 아니다. 모든 화면·팝업·전투를 실제로 열었을 때 콘솔에 빨간 줄이 0 이어야 한다. 워커는 유니티 에디터가 없으므로 ⓐ PlayMode 스모크 테스트(T11 · `LogAssert.NoUnexpectedReceived` · CI 가 실행)로 잡고 ⓑ 코드 감사(런타임 카메라/RenderTexture · 프리팹 자식 `Find` 실패 · catalog 키 누락 · 가짜 null · 에디터 전용 API)로 잡고 ⓒ 주인이 붙인 로그는 «주인 콘솔 에러 보고함» 에서 매 세션 읽어 작업으로 올린다. 완료 기록에는 «플레이 콘솔 에러 0 을 무엇으로 확인했는가» 를 반드시 적는다.

- **(2026-09-05 · 20:0X UTC) ⚑⚑⚑ 주인 지시 — 이제부터 대화형 세션은 직접 작업하지 않고 할 일을 등재만 한다 · 실제 작업은 병렬 루틴(워커 A~D · 매시 :05/:20/:35/:50)이 §2 의 T6 이후를 lock 으로 선점해 한다(aaaw 방식 그대로).** 이번에 등재한 작업 = **T6~T11** (§2 참조 · 범위가 겹치는 것은 순서 고정). 주인 원문 요지(전부 UI · 밸런스·엔진 무관):
  - 장비 화면: 장착 슬롯의 장비 아이콘 크기가 **Character_Hero_Equipment 프리팹 그대로**여야 한다(지금은 너무 작다). «균등 보너스 +0% — 최저 슬롯 Lv.0 …» 문구 **삭제**. 슬롯마다 적힌 «갑옷·장갑·투구» 같은 부위 라벨 **전부 숨김**. 상단 가운데 캐릭터 자리 = **내 플레이어 프리팹(CharacterMaker)** 이 보여야 한다. **투구·무기·갑옷은 장착하면 외형에 바로 반영**(장비 화면에서도, 전투에서도). 장비 아이콘은 CharacterMaker 의 투구·무기·갑옷 파츠 그림으로, 그림이 없는 부위(목걸이·장갑·신발)는 일단 아무 그림으로 채운다. 장비 화면에서 **TopBar 를 표시하지 말고 오른쪽 상단에 골드만**. 인벤 리스트 칸은 **`Prefabs~DemoLayout/ListItem_EquipMent`**(«이게 지금 딱 레이아웃 좋다»). **장착한 장비는 하단 리스트에서 숨긴다**(«장착중» 표기 없음). 뽑기 결과 화면은 **Shop_Chest_Open 그대로**(«하라 했는데 안 했네»).
  - 대장간: 하단에 장비들이 **다 있어야 하는데 없다**(버그) → 수정. 합성 가능한 것은 **오른쪽 위 빨간 점**. 슬롯 크기·비례가 **장비 화면 칸과 같아야** 한다(지금 찌그러짐). 대장간에서는 **장착중 표기를 한다**(재료 불가).
  - 로비: **Lobby_Default 프리팹 그대로**(«레이아웃 좋은데 바꿔버렸다»). 기존 TopBar 폐기. 제목 «꼬마기사 키우기» 표시 안 함. 오른쪽 상단 장비 버튼 삭제. 프리팹의 왼쪽 상단 아이콘 1개·오른쪽 상단 아이콘 2개는 **그대로 두되 기능 없음**(나중 업데이트). 프리팹 상단 바의 **맨 왼쪽은 캐릭터 모습**, 그 다음 **전투력 · 골드 · 보석 순**. 전투력은 프리팹에서 «25/55» 라고 적힌 자리.
  - 상점: **`Prefabs~DemoScenes/Shop_List` 그대로, 레이아웃 비율 그대로**(«쓰라니까 다 바꿔버리네»). `ListItem_ShopPackage` **3개 = 뽑기 상자 3종**(각각 1회 뽑기·10회 뽑기). `ListItem_ShopItem` 들은 **다이아 6개(₩1,000 · ₩10,000 · ₩30,000 · ₩50,000 · ₩80,000 · ₩110,000 · 모의 결제) + 골드 3개(1,000 · 3,000 · 10,000 골드 · 다이아 소모)**.
  - 하단 네비: **대장간·설정 탭을 빼고 그 자리에 탤런트·펫**. 탤런트·펫은 **Character_Talent_02 프리팹 팝업만**(기능 없음). 설정 팝업은 **`Prefabs~DemoScenes/Settings` 그대로**(«그대로 좀 써»).
  - **UI 버그가 너무 많다 → UI 테스트를 만들어라**(에디터에서 `MissingComponentException` 등 실제 예외가 났다 — 원인은 `GetComponent() ?? AddComponent()` 의 에디터 가짜 null · `UiKit.Ensure<T>` 로 고쳤다 · 같은 패턴 재발 금지 게이트 필요).
  - 공통 원칙(주인 반복 지시): **데모 프리팹은 «그대로» 쓴다** — 내부 요소를 옮기거나 지우지 말고, 글자·그림·개수만 우리 데이터로 바꾼다. 프리팹에 없는 것을 새로 그려 넣지 않는다. 배치를 «개선» 하지 않는다.
  - **→ 2026-09-06 지시로 대체됨**: 배치 정본은 `docs/ref/*.jpg`. 프리팹은 부품이다(맨 위 ⚑ 항목). «새로 그려 넣지 않는다» 만 살아 있다.

- **(2026-09-05 · T3 중)** ⑤ GUI Pro 의 `Prefabs~DemoLayout`·`Prefabs~DemoScenes` 를 적절히 쓴다. 화면 지정: Character_Hero_Equipment = 장비 창 · Lobby_Default = 로비 · Play_Perk_Selection_02 = 특전 선택 · Play_Result_Win_01 = 승리 · Settings = 설정 팝업 · Shop_Chest_Open = 장비 소환(뽑기) 팝업. 악마·천사·쉼터는 전용 에셋이 없으니 알아서. ⑥ CI 가 빨개지면 로그를 끝까지 읽고 원인(라이선스/컴파일)을 먼저 말한 뒤 고친다 — **gh-pages 가 생기는 게 최우선**.

- **(2026-09-05 · 승인 대기 답변)** ① 유니티 6000.3.8f1 그대로. ② **브랜치는 main 만**(claude/ 브랜치 폐기). ③ **플레이스홀더 도형 금지 — 처음부터 주인 에셋으로**: UI = `Layer Lab/GUI Pro-MinimalGame`(Theme_Light) · 캐릭터 = `2D Minimal-CharacterMaker`(Character 프리팹 + `_Controller` 애니 · 파츠 조합) · 배경/노드 = `2D Minimal-Environment` · 이펙트 = `JMO Cartoon FX Remaster` · 트윈 = DOTween · 스프라이트 강조 = AllIn1SpriteShader. Odin·AntiCheat·Hot Reload·mcp-unity 는 안 쓴다. 프리팹은 .meta GUID 로 씬/프리팹 YAML 에 박고, **`docs/assets-map.md`** 에 무엇을 어디에 썼는지 표로 남긴다. 단계마다 고른 것을 보고한다. ④ 4~9 는 기본값.

- **(2026-09-05 · 착수 지시)** aaaw 의 «꼬마기사 키우기» 를 이 레포에 유니티로 이식하라.
  - aaaw 는 참조만 하고 절대 수정하지 않는다. 커밋·푸시는 aaawunity 에만.
  - 수치 정본 = aaaw `data/*.json`(커밋 `cf7426c` 이후 main). 코드에 숫자를 박지 말고 JSON 을 로드해서 쓴다. 수치 변경·밸런스 조정 금지.
  - UI 배치 기준 = aaaw `docs/ref/*.jpg` (+ `docs/ui/ref-layout.md` 의 % 표). 껍데기가 아니라 **배치·비율·비례**를 맞춘다.
  - `index.html`/`sim.js` 는 동작 참고용 — 한 줄씩 옮기지 말고 유니티 구조로 다시 짠다.
  - 유니티 에디터도 MCP 도 없다 — C#·씬·프리팹·.meta·ProjectSettings 를 **전부 텍스트로** 쓴다. 유니티 버전은 주인이 main 에 올린 «기본» 프로젝트(6000.3.8f1) 를 따른다(원 지시는 2022.3 LTS — 승인 대기 1번).
  - 2D · 모바일 세로(9:16) · 오토배틀 방치형. **새 시스템·새 기능은 주인 승인 없이 추가 금지.**
  - 매 커밋 전 `dotnet build` 로 컴파일 확인. 컴파일 안 되는 커밋 금지.
  - 전투 엔진은 MonoBehaviour 와 분리된 순수 C# (EditMode 테스트 가능).
  - 이식 검증: sim.js 와 같은 시드(11·12·13)에서 같은 챕터 클리어율(±2%p). 어긋나면 수치가 아니라 **코드 차이**를 고친다.
  - CI: 시크릿 없으면 dotnet 검사만(빨개지면 안 됨) · 있으면 PR/push 마다 EditMode/PlayMode, main push 시 WebGL→gh-pages + Android APK Artifact.
  - 판단이 필요한 건 한 번에 모아서 묻고(PROGRESS «주인 승인 대기»), 나머지는 기본값으로 진행.

## 0. 세션 시작 절차 (모든 워커 공통 · 계정 1 = A~D · 계정 2 = E~H · §6)

1. `git fetch && git checkout -B main origin/main` (pull --rebase 금지, 로컬 잔재 위에서 작업 금지)
2. SID 발급: `sess-HHMM-$RANDOM` (예: sess-0512-23481)
3. aaaw `PLAN.md`(스펙) → 이 문서 → `docs/ref/README.md`(UI 레퍼런스 색인 · UI 작업이면 해당 jpg 를 `Read` 로 직접 본다) → `docs/PROGRESS.md` → `docs/claims/` 순서로 읽는다. **PROGRESS 의 «주인 콘솔 에러 보고함» 도 반드시 읽는다** — 아직 작업으로 안 올라간 항목이 있으면 «가장 큰 번호 +1» 로 등재하고, 그것이 선점 가능한 가장 앞 작업이 된다(콘솔 에러 수정은 UI 작업보다 우선). aaaw 는 `git clone --depth 1 https://github.com/kuzuni/aaaw .aaaw-src` 로 옆에 둔다(커밋 금지 폴더 · .gitignore).
4. [2. 작업 목록]에서 **선점 가능한 가장 앞 작업**을 lock 으로 선점한다 (규약: `docs/claims/README.md`).
5. 선점할 작업이 없으면(전부 lock 또는 전부 완료): 게이트(§3)를 재실행해 검증만 하고, 이상 없으면 **커밋 없이 조용히 종료**. 이상이 있으면 PROGRESS 에 등재하고 종료.

## 1. 절대 규칙

- **플레이 콘솔 에러 0 (주인 상시 지시 2026-09-05).** 플레이 중 유니티 콘솔에 빨간 줄(에러·예외·Assert)이 하나라도 뜨는 상태로 작업을 «완료» 라 적지 않는다. 화면/전투/팝업 코드를 바꾼 커밋은 T11 의 PlayMode 스모크 테스트(`Assets/Tests/PlayMode/UiSmokeTests.cs`)가 그 화면을 열어 빨간 줄 0 을 검증해야 한다(테스트가 없으면 같은 커밋에 추가). 검사 도우미는 `PlayLog.AssertNoRed` — **`LogAssert.NoUnexpectedReceived()` 는 쓰지 않는다**(이 프로젝트의 Test Framework 1.6 은 일반 `Debug.Log` 도 «예상 밖 로그» 로 실패시킨다 · CI #33 회귀). 런타임 Camera·RenderTexture·씬 오브젝트를 새로 만들면 URP 2D(Renderer2D · 깊이/스텐실 사용) 와 호환되는지 반드시 확인한다. **DOTween 트윈을 만들면 반드시 `.SetLink(대상 gameObject)` 를 붙인다**(T56 · 대상이 어떤 경로로 파괴돼도 «has been destroyed» 노란 경고가 안 난다 · `UiKit.Clear` 의 KillTweens 는 Clear 경로만 지킨다). 주인이 붙인 콘솔 로그는 다 고칠 때까지 «주인 콘솔 에러 보고함» 에 남기고, 고친 항목에는 커밋 해시와 원인을 적는다.
- **배포·커밋·push 전에 에러를 확인하고, 배포된 게임(gh-pages)에 들어가 봐서도 에러가 뜨는지 확인한 뒤 고친다(주인 상시 지시 2026-09-06 · WebGL 첫 배포가 첫 화면에서 스택 오버플로로 죽었다 → T59).** 코드 커밋의 ✅ 조건 = CI 유니티 잡 초록 **+ 배포 스모크 초록**(T60 이 만드는 `tools/webgl_smoke.sh` · headless 브라우저로 gh-pages 를 열어 콘솔 에러 0 · 로비 도달 · 전투 진입). 워커는 세션마다 gh-pages 최신 빌드를 한 번 열어 보고(스크립트) 빨강이면 **자기 작업보다 먼저** 고친다(T59 방식으로 등재 · lock). 에디터/PlayMode 초록은 WebGL 초록이 아니다(스레드·System.IO·리플렉션·스택 크기가 다르다).
- **aaaw 레포 수정 금지.** 수치(`data/*.json`)는 `tools/check_data_sync.sh --sync` 로만 가져온다. JSON 을 손으로 고치지 않는다.
- **코드에 게임 수치를 직접 박지 않는다** — `KkomaKnight.Core.GameData` 에서 읽는다. 상수가 JSON 에 없으면 이 레포 전용 JSON(`Assets/KkomaKnight/*.json` · shop.json 방식)에 넣고 «워커 결정 기록» 에 한 줄 적는다(코드 상수 금지는 그대로).
- **새 콘텐츠(특전/시스템/수치 체계) 임의 추가 금지.** 화면 껍데기(T42~T44)는 콘텐츠가 아니다. **주인 승인을 기다리는 일은 없다(2026-09-06)** — 판단이 필요하면 스스로 정해 적용하고 «워커 결정 기록» 에 남긴다.
- **한 줄에 문장이 여럿인 코드 줄 끝에 `// 주석` 을 붙이지 않는다**(CI #87 · 결정 기록 참조: 첫 문장만 바꾸며 주석을 붙였다가 뒤 문장 3개가 주석이 됨 · dotnet 은 못 잡고 PlayMode 만 잡는다). 주석은 윗줄에 · push 전에 `git show <해시> -- Assets | grep -E "^\+" | grep -E ";\s*//"` 로 새 줄을 훑는다.
- **커밋 전 게이트**: `dotnet build tools/dotnet/KkomaKnight.sln -c Release` 초록 · `dotnet test tools/dotnet/Tests` 초록 · `python3 tools/gen_meta.py --check` 초록. 새 에셋을 만들면 `python3 tools/gen_meta.py` 로 .meta 를 만든다(GUID 결정적).
- 전투 엔진(`Assets/Scripts/Core`)에는 `UnityEngine` 을 참조하지 않는다(asmdef `noEngineReferences: true` · dotnet 이 강제한다).
- 승인 프롬프트가 뜨는 명령·대화형 편집기(`git rebase -i` 등) 금지. 캡처 PNG·대용량 바이너리 커밋 금지(예외 2개: 폰트 1개 — PLAN §2.1 · 오디오 OGG 합계 ≤ 5MB — T28 주인 지시).
- 작업이 끝나면 lock 삭제 → PROGRESS 갱신 → 커밋 → push. **lock 만 잡는 커밋·문서만 바꾼 커밋은 제목 끝에 `[skip ci]`** 를 붙인다(코드가 안 바뀐 푸시로 25분짜리 유니티 빌드를 또 돌리지 않는다). 코드 커밋에는 절대 붙이지 않는다. **`[skip ci]` 커밋은 코드 커밋과 같은 push 에 묶지 않는다** — GitHub 은 push 의 머리 커밋에 `[skip ci]` 가 있으면 push 전체(앞의 코드 커밋까지)를 건너뛴다(T13 에서 실사고 · 코드 커밋을 먼저 push 하고 문서 커밋을 따로 push · 이미 묶였으면 Actions 의 `workflow_dispatch` 로 수동 실행). **push 실패 시 `git fetch && git rebase origin/main`** 후 재push (자기 lock 이 사라졌으면 진 것 — 작업 버리고 종료).
- 브랜치는 `main` 하나다(주인 결정 — 다른 브랜치에 올리지 않는다). 각 단계가 끝나면 main 에 커밋·푸시하고 PROGRESS 에 «무엇을 확인하면 되는가» 한 줄을 적는다.
- **에셋은 주인 에셋만** (위 지시 ③). 코드 생성 도형·임시 그림 금지. 새 에셋을 쓰면 `docs/assets-map.md` 표에 «용도 · 경로 · GUID(·fileID)» 를 한 줄 추가한다.

## 2. 작업 목록 (순서 고정 — lock ID = 아래 번호)

> T1~T5(주인이 정한 5단계)는 끝났다. **지금 열린 작업은 T17~T63**(T64 = WebGL 오디오(BGM «no supported source» · SFX «Loading FSB failed» · T59/T60 스모크에서 발견 · 제약 없음) · T59 🔄 워커 G 진단 빌드 push(심볼 Embedded · 부팅 마커 · 데스크톱 headless 재현 불가 · PROGRESS T59 진행 기록) · T60 코드 완료(워커 G · CI build-webgl 잡의 스모크 step 이 확인) · T63 = 전체 글자 가독성 · (T62 = 아레나 순위 = Social_Ranking 프리팹 변형 · (T61 = 특전 카드 Shine 순서대로 · (T59 = WebGL 배포 크래시 최우선 · T60 = 배포 스모크 게이트 · (T58 = 비평 하니스 PNG 촬영 결함(UI 띠 34.8% · 월드 스프라이트 겹침 · T46 뒤) · T57 ✅ · T47 🔄(회차 1 · 로비 9.7 ✅ · 전투 8.6 ✅ 캔버스 · 남은 코드 3건) · T46 ✅(screens CI #83 · T58 열림) · T44 ✅(비평 9.5~10.0) · T41 ✅(비평 10.0) · T36 ✅(비평 9.5) · T38 ✅(비평 8.5) · T56 = 플레이 콘솔 노란 줄 0 — DOTween 세이프 모드 경고(파괴된 오브젝트 겨냥 트윈 · `SetLink`) · T55 = CI #76·#77 빨강 후속(T49 회귀 · 최우선) · T49 = 팝업 등장 연출 DOTween 순서대로 · T50 ✅(엔진 틱 보류 · 워커 H) · T54 ✅ · T51 ✅(T50 과 같은 커밋) · T52 = 특전 글자 한 색 · T53 = 특전 설명 «트리거: 내용» 표기 · 2026-09-06) — **T34~T44 = UI 를 `docs/ref` 레퍼런스 구도로(2026-09-06 · 최우선 · «프리팹 그대로» 계열 지시를 대체 · T25 흡수 · T27·T30·T32 폐기)**. 이전 묶음: T17~T33(T12~T16 은 워커가 먼저 쓴 번호 · 내 T13~T16 은 T20~T23 으로 정정) (T12 = 콘솔 에러 수정 · 최우선 · T13 = 특전 미리보기 줄 비례 · T14 = 전투 캐릭터 크기·공격 애니·사망 모션 · T15 = 프리팹 스폰 PanelView 예외 · 콘솔 에러라 최우선 · T16 = T14 의 CI #39 빨강 후속) — 같은 파일을 만지는 것은 아래 «순서» 대로(앞 번호의 lock 이 사라지고 PROGRESS 행이 ✅ 가 된 뒤에 잡는다). 겹치지 않는 것은 병렬 선점 가능.

### T1 — 프로젝트 뼈대 + JSON 로더 + CI/활성화 워크플로 + README ✅ (완료 · PROGRESS 참조)

### T2 — 전투 엔진 (순수 C#) + 이식 검증 ✅ (완료 · PROGRESS 참조 — 21칸 전부 sim.js 와 동일)
범위: `Assets/Scripts/Core/Battle*.cs` · `Assets/Scripts/Core/Perks*.cs` · `tools/sim/` · `Assets/Tests/EditMode/Battle*.cs`
1. sim.js `runChapter` 를 `KkomaKnight.Core` 에 이식 — 챕터·웨이브·노드(쉼터/악마/천사/보스)·원거리 배치(enemies.json 의 `ranged[]` 그대로)·투사체(도끼/화살/창/검기 · 적 화살)·스턴·실드·방어막·특전 100종·장비 세트 옵션.
   - 난수는 `IRng` 하나로만 굴리고 **sim.js 와 같은 순서로 소비**한다(치명 굴림 → 회피 굴림, 골드 `rand(1,1.8)` 도 소비 등). 그래야 시드 하니스가 sim.js 와 같은 수열을 밟는다.
   - 결정이 필요한 자리(쉼터·악마·천사·레벨업 3택)는 `IBattlePolicy` 로 뺀다. 시뮬 정책 = 쉼터 «항상 경험치» · 악마 «항상 수락» · 천사 «항상 +5%» · 3택 «표 순서 앞선 것».
   - 게임(유니티)은 정책이 «보류(null)» 를 돌려 엔진이 멈추고, 팝업이 답을 넣으면 재개한다 — PLAN §2.4 «팝업 중 시간 완전 정지».
2. `tools/sim/Program.cs` 에 sim.js 실험1(사다리 7점 · `LADDER_OPTS` = base10 · legacy20 · gearOpts:false) 재현. `SEED=11·12·13`, 각 1,000판. `node sim.js 1` 의 같은 시드 출력과 **±2%p** 안이어야 한다. 3택 모드(`EXP1_PERKMODE=3pick`)도 함께 찍는다.
3. 어긋나면 수치가 아니라 코드 차이를 고친다. 결과표(시드×7칸)를 PROGRESS 에 남긴다.
4. EditMode 테스트: 결정적 시드에서 한 챕터 결과가 고정값인가(회귀 방지) · 방어막→피해 무시→피해 순서 · 소환 적중 = 공격 트리거 · PROC_TICK_CAP.

### T3 — 레벨업 특전 3택 + 악마의 거래 (유니티 팝업) ✅ (완료 · PROGRESS 참조 — 실물 확인은 WebGL 배포에서)
범위: `Assets/Scripts/Game/Battle*.cs` · `Assets/Scripts/Game/Overlay*.cs` · `Assets/Scripts/Game/Hud*.cs`
1. 전투 화면(카메라 줌 1.5 · 플레이어 x 16% · 발밑 바 2/3 — `ui.json`)을 코드로 생성한 uGUI/스프라이트로 그린다(적·투사체·데미지 팝은 엔진 상태에서 읽는다).
2. 레벨업 3택 카드(등급색 테두리 + 등급 이름), 📘 보유 특전, 악마 카드(미리 굴린 한 장 그대로 · 최대체력 30% 차감), 쉼터(체력 260 / 경험치 26), 천사(+5% / 광고 3초 +15%). 팝업 중 `Tick` 정지.
3. 상단 스탯 줄 8칸(공격력·방어력·공격속도·반격·치확·회피·치배·흡혈) · 버프 아이콘 열 · 특전 미리보기 줄.

### T4 — 로비 · 장비 · 강화 · 슬롯 · 뽑기 상자 3종 ✅ (완료 · PROGRESS 참조 — 실물 확인은 WebGL 배포에서)
범위: `Assets/Scripts/Game/Screens.cs`(Lobby·Gear·Forge·Shop) · 새 파일은 `Gear*.cs` · `Forge*.cs` · `Shop*.cs` · `Assets/KkomaKnight/catalog.json`(키 추가)
> 방법(T3 에서 확립): 화면은 주인 지정 GUI Pro 데모 프리팹을 `UiKit.Spawn` 으로 세우고 자식 이름으로 글자/아이콘/버튼을 바꾼다(`docs/assets-map.md` · `/tmp` 덤프는 `python3 tools/…` 없이 prefab YAML 을 직접 읽는다). 장비 = **Character_Hero_Equipment** · 소환 결과 = **Shop_Chest_Open** · 장비 아이콘 = catalog `gi.<부위>.<세트>` · 등급 색 = `Palette.RarName`(gray/blue/yellow/plum) 의 ItemFrame_01_Normal_* 변형. 새 에셋 키는 catalog.json 에 추가하고 `python3 tools/gen_catalog.py` 로 재생성(assets-map 도 같이 갱신된다).
1. 세이브(PlayerPrefs 에 JSON — index.html `kkoma-knight-v2` 와 같은 필드: gold·gem·maxChapter·selChapter·inv·eq·slots·gachaBoxes·uid·freeDay).
2. 로비(T3 에서 Lobby_Default 로 뼈대 완료: 챕터 ◀▶ · START · 하단 5탭 상점·장비·전투·대장간·설정) — 남은 것: 최고 챕터/해금 표시 다듬기 · 장비 버튼.
3. 장비 탭(좌우 슬롯열 3+3 · 캐릭터 · 공/체/실 3칸 · 균등 보너스 · 합성 버튼 · 인벤 5열) · 세부 팝업(등급 배지·아이콘·이름·스탯·옵션 7줄 잠금 표시·슬롯 강화 비용·장착/해제) · 대장간(수동 3칸 + 자동 · 장착분 제외 · `FuseMake` 하나만).
4. 상점(무료 보급 2,500/일 · 모의 결제 12,000 · 상자 3종: 가격·확률·천장 문구는 `gacha.json` 에서 · 뽑기 결과 팝업 · 자동 장착 없음 · NEW 뱃지).

### T5 — UI 를 docs/ref 레이아웃에 맞추기 ✅ (완료 · PROGRESS 참조 — 실물 확인은 WebGL 배포에서)
범위: `Assets/Scripts/Game/Layout*.cs` + 각 화면의 배치 상수
> 시작점: T3/T4 화면은 이미 `Layout` 상수(ref-layout ①~⑦ %)로 앵커링돼 있다 — 남은 것은 데모 프리팹을 통째로 세운 화면(로비 Lobby_Default · 장비 Character_Hero_Equipment · 팝업들)의 내부 요소를 표 % 로 옮기는 일과 실물(WebGL) 대조. 승인 대기 17(장비 탭 기사 RenderTexture)도 여기서.
1. aaaw `docs/ui/ref-layout.md` 의 표(요소별 x/y/w/h · 프레임 %)를 배치의 단일 정본으로 코드에 옮긴다(9:19.5 레퍼런스 → 프레임 % 환산).
2. 화면마다 요소를 그 % 자리에 앵커링한다(±3%p). 색·폰트·그림체는 점수 밖 — 배치·비율·비례만.
3. 검증: 에디터 없이 되는 만큼 — 배치 상수가 표와 같은지 EditMode 테스트로 대조하고, 실물 확인은 WebGL 배포에서 주인이 폰으로 한다.

### T6 — 로비 = Lobby_Default 그대로
범위: `Assets/Scripts/Game/Screens.cs`(LobbyScreen) · `Assets/Scripts/Game/TopBar.cs`(삭제) · catalog(로비용 키)
순서: 제약 없음(먼저 잡아도 됨). T10 이 이 파일(NavBar)을 뒤이어 만진다.
1. `ui.lobby`(Lobby_Default) 인스턴스를 **원형 그대로** 둔다 — 요소를 Pct 로 옮기지 않는다(T5 의 재앵커링을 되돌린다). 프리팹 안 요소는 글자·숫자만 바꾼다.
2. 지운다: 우리가 넣은 배너(«꼬마기사 키우기»)·`TopBar`(파일째 삭제 · 다른 화면 참조도 제거 — 장비 화면은 T7 이 골드만 남긴다) · 오른쪽 상단 «장비» 버튼(`Group_RightButtons` 의 우리 배선). 프리팹 원래의 왼쪽 상단 아이콘 1개 · 오른쪽 상단 아이콘 2개는 **보이게 두고 클릭 기능 없음**.
3. 프리팹 상단 바: 맨 왼쪽 초상 자리 = **플레이어 모습**(CharacterMaker Character 프리팹 · 장착 외형 반영 · UI 안에 그리는 방법은 T7 과 같은 `HeroView` 헬퍼를 쓴다 — T7 이 먼저면 그것을, 아니면 여기서 만들고 T7 이 재사용) · 그 오른쪽 «25/55» 자리 = **전투력**(`GearScreen.BuildPower` 와 같은 값 · «전투력 N» 또는 숫자만) · 그 다음 **골드 · 보석**. 순서는 «캐릭터 · 전투력 · 골드 · 보석».
4. 챕터 표시·카드·좌우 화살·START·하단 탭은 프리팹 자리 그대로(기능 유지). 챕터 이동 로직 그대로.
5. 게이트 + PROGRESS T6 행 + «주인이 확인할 것» 한 줄. `docs/assets-map.md` 갱신(gen_catalog).

### T7 — 장비 화면 = Character_Hero_Equipment 그대로 + 외형 반영
범위: `Assets/Scripts/Game/GearScreen.cs` · `GearUi.cs` · `CharacterRig.cs`(외형 매핑 헬퍼) · `BattleWorld.cs`(플레이어 스킨 한 줄) · catalog(장비 아이콘 키)
순서: 제약 없음. **T8 은 이 작업 뒤**(GearUi 공유).
1. 장착 슬롯 6칸: 프리팹의 슬롯 크기·아이콘 크기 **그대로**(우리 Cell 을 억지로 축소해 넣지 않는다 — 프리팹 슬롯 안 Icon 이미지에 스프라이트만 꽂는다). 슬롯의 부위 라벨(«갑옷·장갑·투구…») 전부 비활성. «균등 보너스 …» 문구 삭제(`EvenBonus` 표시 제거 · 계산 함수는 남겨도 됨).
2. 상단 가운데 캐릭터 = **플레이어**: `HeroView` 헬퍼(신규 · `Assets/Scripts/Game/HeroView.cs`) — RenderTexture 카메라(Culling 전용 레이어) 로 CharacterMaker Character 프리팹(Idle)을 RawImage 에 그린다. 프리팹의 샘플 캐릭터 이미지는 끈다. 로비(T6) 도 이 헬퍼를 쓴다.
3. **장착 외형**: 투구·무기·갑옷 장비(gear.json 부위 helmet/weapon/armor 에 해당하는 것)를 장착하면 `CharacterRig.Skin` 의 Helmet/Sword(또는 Axe·Spear…)/Chest 가 바뀐다 — 매핑 표 `GearLook`(신규 · 장비 «종류(kind)» × 등급 → 카탈로그 `cm.*` 스프라이트 키) 를 한 곳에 두고, `BattleWorld.KnightSkin` 과 `HeroView` 가 같은 표를 쓴다. 등급이 오를수록 더 화려한 파츠(CharacterMaker Parts Pack 에서 고른다 · `docs/assets-map.md` 에 표로). 그림이 없는 부위(목걸이·장갑·신발)는 외형 미반영.
4. 장비 **아이콘** = 같은 표의 파츠 스프라이트(투구·무기·갑옷). 목걸이·장갑·신발은 GUI Pro 아이콘 중 아무거나로 임시(«일단 아무거나» · 주인 확정: 워커가 고른다).
5. 화면 상단: **TopBar 없음 · 오른쪽 상단에 골드만**(프리팹의 재화 바 중 골드 칸만 보이게).
5-1. **«상점»·«합성» 버튼은 공격력·체력·실드 3칸 바로 아래**(주인 2026-09-06). 지금처럼 화면 맨 아래(탭바 위)가 아니라 스탯 3칸 줄 밑에 가로로 나란히 — 프리팹에 그 자리에 버튼 줄이 있으면 그것을 쓰고, 없으면 `ui.btnGray`/`ui.btnOrange` 2개를 스탯 줄 바로 아래 같은 폭으로. 인벤 격자는 그 아래부터.
6. 인벤 리스트 칸 = **`ListItem_EquipMent`**(카탈로그 `ui.equipCell` 신규) — 프리팹 비율 그대로 · 등급색·아이콘·+N 만 바꾼다. **장착 중인 장비는 리스트에서 숨긴다**(«장착중» 배지 없음).
7. `GearUi.Cell` 은 대장간(T8)·뽑기 결과(T9)도 쓴다 — `CellOpts` 에 «장착중 표기 on/off · 합성 가능 빨간 점» 옵션을 두고 여기서는 둘 다 끈다.
8. 게이트 + PROGRESS T7 행 + «주인이 확인할 것».

### T8 — 대장간 정리 (T7 뒤)
범위: `Assets/Scripts/Game/ForgeScreen.cs`
순서: **T7 완료 뒤**.
1. 하단 인벤에 장비가 **전부** 보인다(지금 안 보이는 원인 규명 — Grid/Content 크기·ScrollRect·Pct 겹침 — PROGRESS 에 원인 한 줄).
2. 칸 = T7 의 `ListItem_EquipMent` 칸과 **같은 크기·비례**(찌그러짐 0 · 5열 격자에서 셀 aspect 고정).
3. 합성 가능(같은 부위·종류·등급 3개 이상)한 칸은 **오른쪽 위 빨간 점**(GUI Pro 의 알림 점 스프라이트 · 카탈로그 키 `ui.redDot`).
4. 여기서는 **장착중 표기 유지**(배지) — 그러나 **장착 중인 장비도 재료로 쓸 수 있다**(주인 2026-09-06 «대장간에 장착중인 거도 합성 가능하게») → `ForgeScreen.Toggle` 의 장착분 거부·흐리게 처리를 없애고, `GearSystem.FuseAll/FuseMake` 호출에서 장착 제외(`EquippedSet`)를 빼서 자동 합성도 장착분을 포함한다. 장착 중이던 것이 재료로 사라지면 **결과물이 같은 부위면 그 슬롯에 장착**, 아니면 슬롯을 비운다(승인 대기 29 기본값). aaaw 의 T125(«장착분은 재료가 아니다») 는 주인이 뒤집었다 — PROGRESS 에 한 줄.
5. 게이트 + PROGRESS T8 행.

### T9 — 상점 = Shop_List 그대로 + 뽑기 결과 = Shop_Chest_Open 그대로
범위: `Assets/Scripts/Game/ShopScreen.cs` · catalog(상점 키) · `Assets/Scripts/Core/GameData.cs`(상점 상품표 로더 — JSON 이 아니라 코드 상수 금지 → 아래 5 참조)
순서: 제약 없음.
1. `Shop_List` 프리팹을 **원형 그대로**(스크롤·섹션·비율). 우리가 만든 카드 레이아웃(3 상자 카드·격자)은 버린다.
2. `ListItem_ShopPackage` ×3 = 상자 3종(gacha.json 순서) — 각 항목에 **1회 뽑기 · 10회 뽑기** 버튼(가격은 gacha.json). 프리팹에 버튼이 1개면 두 번째 버튼은 같은 프리팹의 버튼을 복제해 옆에(«그대로» 원칙의 유일한 예외 · PROGRESS 에 적는다).
3. `ListItem_ShopItem` ×9 = **다이아 6종**(₩1,000 · ₩10,000 · ₩30,000 · ₩50,000 · ₩80,000 · ₩110,000 · 모의 결제 = 누르면 바로 지급) + **골드 3종**(1,000 · 3,000 · 10,000 골드 · 다이아 소모).
4. 뽑기 결과 팝업 = `ui.chestOpen`(Shop_Chest_Open) **그대로** — 열린 상자 그림 + 얻은 장비 격자(칸 = T7 의 `ListItem_EquipMent` 가 준비돼 있으면 그것, 아니면 현 `GearUi.Cell`). 지금 코드가 이 프리팹을 정말 쓰는지 확인하고, 안 쓰면 바꾼다(주인: «하라 했는데 안 했네»).
5. **수치**(주인 확정 2026-09-05 «그렇게 해라» = 아래 기본값 그대로): 다이아 6종의 **다이아 개수**와 골드 3종의 **다이아 가격** → `Assets/StreamingAssets/data/` 는 aaaw 동기 폴더라 못 넣는다. **`Assets/KkomaKnight/shop.json`**(이 레포 전용 · 승인 대기 25 의 기본값) 을 만들어 거기서 읽는다(코드 상수 금지 규칙의 예외가 아니라 «JSON 에서 읽기» 그대로). 기본값: 다이아 100·1,100·3,500·6,000·10,000·14,000 / 골드 1,000=다이아 30 · 3,000=80 · 10,000=250. 주인이 바꾸면 파일만 고친다.
6. 게이트 + PROGRESS T9 행 + 승인 대기 25 갱신.

### T10 — 하단 네비 5칸 = 상점·장비·전투·탤런트·펫 + 설정 = Settings 그대로 (T6 뒤)
범위: `Assets/Scripts/Game/Screens.cs`(NavBar · 새 팝업 진입) · `Assets/Scripts/Game/Overlay.cs`(Settings·Talent 팝업) · catalog(`ui.talent` = Character_Talent_02)
순서: **T6 완료 뒤**(Screens.cs 공유).
1. 탭 = 상점 · 장비 · 전투 · **탤런트 · 펫**(대장간·설정 탭 제거 · 대장간은 장비 화면의 «합성» 버튼으로만 진입 · 설정은 로비의 메뉴(≡) 버튼과 전투의 일시정지에서).
2. 탤런트·펫 = `Character_Talent_02` 프리팹 팝업 **그대로**(제목만 «탤런트»/«펫» · 기능 없음 · 닫기만). 탭 아이콘은 GUI Pro 아이콘 중 탤런트/펫에 맞는 것.
3. 설정 팝업 = `ui.settings`(Settings) **그대로** — 지금 구현이 프리팹 요소를 옮겼으면 되돌린다. 동작하는 것만 연결(사운드 토글은 값만 저장 · 나머지 버튼은 눌러도 아무 일 없음).
4. 게이트 + PROGRESS T10 행.

### T11 — UI 스모크 테스트(PlayMode) + 가짜 null 게이트
범위: `Assets/Tests/PlayMode/` · `tools/check_unity_null.sh`(신규) · `.github/workflows/ci.yml`(게이트 한 줄) · `docs/ROUTINE.md` §3
순서: 제약 없음(다른 작업과 파일 안 겹침). 화면 코드가 바뀌면 테스트도 따라 고친다(같은 워커가 아니어도 됨).
1. PlayMode 테스트(유니티 CI 가 돌린다 · dotnet 은 컴파일 못 하므로 `tools/dotnet` 에 넣지 않는다): `App` 을 세우고 **모든 화면(로비·장비·대장간·상점·전투)과 팝업(레벨업·보유 특전·쉼터·악마·천사·사망·클리어·일시정지·설정·탤런트·펫·세부·뽑기 결과·슬롯)을 하나씩 연다** → ⓐ 예외·에러 로그 0(`LogAssert.NoUnexpectedReceived`) ⓑ 프리팹 잔여 글자(«Text», «Remain», «New Text», 영문 데모 문구) 가 활성 Text 에 없음 ⓒ 화면마다 핵심 요소 존재(예: 장비 슬롯 6 · 상점 상자 3 · 탭 5) ⓓ 전투는 3초 틱 뒤 예외 0.
2. `tools/check_unity_null.sh`: `GetComponent…() ??` · `Find(...) ??` 패턴이 `Assets/Scripts` 에 0건인지(있으면 실패 · 메시지에 `UiKit.Ensure<T>` 안내). CI dotnet 잡과 §3 게이트에 추가.
3. 게이트 + PROGRESS T11 행(테스트 수 · CI 런 번호).

### T12 — 플레이 콘솔 에러 0 : URP 2D 렌더 에러(HeroView RenderTexture) + 전 화면 런타임 에러 감사 (최우선 · 제약 없음)
범위: `Assets/Scripts/Game/HeroView.cs` · (필요시) `WorldCam.cs`·`App.cs`·`BattleWorld.cs` 의 카메라/RenderTexture 코드 · `Assets/Tests/PlayMode/`(HeroView 렌더 1프레임 테스트 추가 — T11 과 파일이 겹치면 T11 워커와 별도 파일 `HeroViewTests.cs`)
순서: 제약 없음 — **다른 T 보다 먼저 잡는다**(주인: 플레이할 때마다 뜬다).
1. 주인 로그 재현 원인 확정: `Renderer2D Pass: Fake or uninitialized surface is not supported for attachment 0.` + `EndRenderPass: Not inside a Renderpass`. 등재 진단(⚑ 최신 항목)대로 `HeroView.BuildStage` 의 깊이 0 RenderTexture 가 유력 — `Renderer2D.asset` `m_UseDepthStencilBuffer: 1` 과 충돌. 확정 근거를 PROGRESS 에 한 줄.
2. 수정(에셋 설정을 끄는 게 아니라 코드가 렌더러 설정에 맞춘다): RenderTexture 깊이 24 + `depthStencilFormat` 명시 · 런타임 Camera 에 `UniversalAdditionalCameraData` 를 `Ensure` 하고 renderType Base · `OnDisable/OnDestroy` 에서 카메라 먼저 끄고 `targetTexture=null` → `Release()` → `Destroy` 순서 · RawImage 가 해제된 텍스처를 참조하지 않게. 로비·장비 두 HeroView 인스턴스와 전투 진입/이탈 왕복 후에도 에러 0.
3. **다른 에러 전수 감사**(주인: «다른 에러들도 있는지 다 확인해서 고쳐라»): `Assets/Scripts` 전체에서 ⓐ 프리팹 자식 `Find`/`GetComponent` 결과를 null 검사 없이 쓰는 곳 ⓑ catalog 키가 `catalog.json` 에 없는 곳(`gen_catalog.py --check` 는 경로만 보므로 코드의 문자열 키를 추출해 대조하는 스크립트를 `tools/check_catalog_keys.py` 로 추가) ⓒ 에디터 전용 API/`Resources.Load` 실패 ⓓ 코루틴/DOTween 이 파괴된 오브젝트를 만지는 곳(화면 전환 직후 NRE) ⓔ TMP/폰트·머티리얼 누락 경고. 발견 항목은 고치거나(범위 안) 새 T 로 등재(범위 밖).
4. 검증: PlayMode 테스트 `HeroViewTests` — `HeroView` 를 세우고 `yield return new WaitForEndOfFrame()` ×3 뒤 `LogAssert.NoUnexpectedReceived()`. dotnet 게이트는 PlayMode 를 못 돌리므로 CI(유니티) 런 번호를 PROGRESS 에 적고, CI 시크릿이 없어 유니티 잡이 안 돌면 «주인이 에디터에서 확인할 것: 플레이 → 로비·장비·전투 왕복 → 콘솔 빨간 줄 0» 을 적는다.
5. 게이트 + PROGRESS T12 행 + «주인 콘솔 에러 보고함» 의 해당 항목에 ✅·커밋 해시·원인 한 줄.

### T13 — 전투 HUD «얻은 특전 미리보기 줄»(PerkStrip) 비례 수정 — 아이콘이 서로 가림 (제약 없음)
범위: `Assets/Scripts/Game/BattleScreen.cs`(`RefreshPerkStrip` · 85~87행 PerkStrip 생성) · `UiKit.PerkFrame`(필요시) · `Assets/Scripts/Core/Layout.cs`(`HudPerkStrip` 은 표값 — 바꾸지 않는다) · `Assets/Tests/PlayMode/PerkStripTests.cs`(신규)
순서: 제약 없음. T11/T12 와 파일이 겹치지 않게 테스트는 별도 파일.
1. 주인 증상: 특전을 여러 개 얻으면 하단 미리보기 아이콘이 **너무 크게 그려져 서로 겹친다**. 등재 세션 관찰(확정은 워커): 줄 = `Layout.HudPerkStrip`(높이 4.0% ≈ 93px · 폭 80% ≈ 864px) · 셀 `sizeDelta 78×84` + 간격 8 을 `HorizontalLayoutGroup(childControl* = false)` 에 최대 11개 → 11×86 = 946px > 864px 로 폭을 넘친다. 또 `UiKit.PerkFrame` 이 `ui.itemFrame4` 프리팹을 셀 안에 `size×165/162` 로 세우는데 프리팹 내부(그림자·광택 등 자식)가 셀보다 크게 뻗을 수 있다 — 실제 어느 쪽이 겹침을 만드는지 프리팹 YAML(RectTransform 트리)과 계산으로 확정해 PROGRESS 에 한 줄.
2. 비례 정본 = aaaw `index.html` 404~415행 CSS: 줄 높이 34px · 아이콘 28×28 · 간격 4px · «+N» 은 높이 28 · 개수 배지는 오른쪽 위 14px (390×844 프레임 기준). 우리 프레임(1080×2337)에서는 **줄 높이의 28/34 = 82% 를 셀 한 변**으로, 간격은 4/34, 배지는 14/34 로 — 픽셀 상수를 박지 말고 `HudPerkStrip` 의 실제 rect 높이에서 계산한다(해상도가 달라도 비례 유지). 표시 개수 `max` 도 상수 11 이 아니라 **줄 폭 ÷ (셀+간격)** 으로 계산해 «+N» 까지 포함해서 절대 넘치지 않게 한다.
3. 프레임 프리팹 내부가 셀을 넘으면 프리팹을 바꾸지 말고(«그대로» 원칙) 셀에 `RectMask2D` 를 두거나 프레임 rt 를 셀 크기에 맞춘다. 아이콘은 프레임 안 `Icon` 자식에 그대로.
4. **스크린샷 확인(주인 지시)**: 워커는 에디터가 없으므로 ⓐ PlayMode 테스트 `PerkStripTests` 가 특전 12개를 강제로 얻은 상태를 만들고(`G.Taken` 에 서로 다른 id 12개 + 중복 1개) 한 프레임 뒤 **모든 셀 rect 가 서로 겹치지 않고 PerkStrip rect 안에 있는지** 를 `RectTransformUtility` 로 단언 ⓑ 같은 테스트에서 `ScreenCapture.CaptureScreenshot` 으로 PNG 를 **`Application.temporaryCachePath`/CI 아티팩트**에 남긴다(레포 커밋 금지 — `.github/workflows/ci.yml` 의 PlayMode 잡에 `actions/upload-artifact` 한 줄 · 이름 `perkstrip-screens`). PROGRESS 에 CI 런 번호와 아티팩트 이름을 적어 주인이 내려받아 보게 한다. CI 유니티 잡이 안 돌면 «주인이 에디터에서 확인할 것 — 특전 10개 이상 얻은 뒤 하단 줄이 안 겹치는지» 를 적는다.
5. 게이트(§3 + 플레이 콘솔 에러 0) + PROGRESS T13 행.

### T14 — 전투 캐릭터 크기 2/3 · 공속 비례 공격 애니 · 사망 모션 루프 금지
> (등재 시 T12 로 적혔던 것을 번호 규약(«가장 큰 번호 +1» · 재사용 금지 · T12 = 콘솔 에러 수정 완료)에 따라 T14 로 바로잡음 — sess-2136-22274 · lock 은 `T14.lock`)
범위: `Assets/Scripts/Game/BattleWorld.cs`(캐릭터 크기 · Dead 재생) · `Assets/Scripts/Game/CharacterRig.cs`(PlayAttack 속도 · Dead 비루프) · `Assets/Scripts/Core/Layout.cs`(PlayerHeight/EnemyHeight 상수 — LayoutSpecTests 가 표와 대조하므로 표 값은 두고 **배율 상수** 를 따로 둔다)
순서: 제약 없음(T7 이 BattleWorld 의 KnightSkin 한 줄을 만진다 — 그 줄만 피한다).
1. **플레이어·적 크기 2/3**: 지금 키(PlayerHeight 9% · EnemyHeight 9% · 보스 ×BossSizeMul)에 `Layout.CharScale = 2f/3f` 를 곱한다(표 상수는 그대로 · 테스트 유지). 발밑 체력바 폭도 같은 비율.
2. **공격 애니 속도 = 공속 비례**: 공격 1회 간격이 T 초면 Attack 클립(1.83초)이 **T 초 안에 끝나도록** 속도를 올린다 — 속도 = 클립길이 / T (하한 1 · 상한 없음). 플레이어 T = 1/공속(EffAspd) · 적 T = meleeInterval/bossInterval/rangedInterval(슬로우 배율 포함). 지금은 상한 ×3 이라 공속이 빠르면 모션이 다음 공격에 잘린다 → 상한을 없앤다. 타격 순간(OnAttackHit 1.0초 지점)도 같은 배율로 앞당겨진다(이미 HitDelay 가 속도를 나눈다 — 확인만).
3. **사망 모션 루프 금지**: Dead1.anim 이 루프(`m_LoopTime: 1`)라 죽은 뒤 다시 일어나는 것처럼 보인다. 에셋을 고치지 말고(주인 에셋 불변) `CharacterRig.Play(Dead)` 뒤 클립 길이만큼 지나면 **Animator 를 멈춘다**(`_anim.speed = 0` 또는 마지막 프레임에서 정지) — 플레이어·적 모두. Victory/Defeat 도 루프면 같은 처리(정지 시점 = 클립 끝).
4. 게이트 + PROGRESS T14 행 + «주인이 확인할 것».

### T15 — 플레이 콘솔 에러 0 : 데모 프리팹 스폰 시 `PanelView.OnEnable` 예외 (최우선 · 제약 없음)
범위: `Assets/Scripts/Game/UiKit.cs`(`Spawn`) · 회귀 확인 = T11 의 PlayMode 스모크(CI)
순서: 제약 없음(T11 워커와 파일이 겹치지 않는다 — 테스트는 손대지 않는다).
1. 원인(CI #36 · https://github.com/kuzuni/aaawunity/actions/runs/33995378223 · `UiSmokeTests` 3건 전부 같은 스택): `UiKit.Spawn` 이 `Instantiate(prefab, parent, false)` 를 **활성 부모** 밑에 하므로 GUI Pro 데모 스크립트 `LayerLab.CasualGame.PanelView.OnEnable`(`otherPanels[i].SetActive` · 배열 미할당)이 `Adopt` 가 스크립트를 지우기 **전에** 돌아 `UnassignedReferenceException` 을 던진다(에디터 플레이에서도 설정·장비 세부·전투 팝업을 열 때마다 빨간 줄 — 빌드에선 NRE). 스택: `PanelView.OnEnable ← Object.Instantiate ← UiKit.Spawn ← Overlay.SettingsPopup`.
2. 수정(에셋을 고치지 않는다 · 주인 에셋 불변): `Spawn` 이 **비활성 대기 오브젝트** 밑에 먼저 인스턴스화 → `Adopt`(PanelView/PanelControl 제거 · TMP 변환) → `SetParent(parent, false)` 순서로 바꿔 데모 스크립트의 OnEnable 이 한 번도 돌지 않게 한다. `adopt=false` 호출은 지금 없으므로 그 경로도 같은 순서(스크립트 제거만은 항상).
3. 게이트 + PROGRESS T15 행 + 콘솔 에러 0 확인 수단 = 코드 커밋의 CI PlayMode(`UiSmokeTests` 3건이 초록으로).

### T16 — CI 런 #39 빨강(T14 코드 커밋 `0ee1e18`) 후속: 사망/승리 정지 `Frozen` 계약을 같은 프레임에 성립시키기 ✅ (완료 · `2b71ea1` · CI #40 초록 · PROGRESS 참조)
범위: `Assets/Scripts/Game/CharacterRig.cs`(`Update` 의 정지 1줄) · 테스트 불변(`Assets/Tests/PlayMode/CharacterRigTests.cs` 는 손대지 않는다)
순서: 제약 없음.
1. 원인(CI #39 · PlayMode `CharacterRigTests.PlayerDeathInBattleFreezesUnderDeadPopup` 1건 · 나머지 10건 Passed): `Animator.Play(상태, 0, 0.999)` 는 다음 애니 평가 때 적용되는데 `_frozen` 은 즉시 켜져, 코루틴이 그 프레임에 읽은 `normalizedTime` 이 직전 값(0.967)이라 1초 뒤 정지점(0.999)과 불일치. 정지 자체는 정상.
2. 수정: Play 직후 `_anim.Update(0f)` 즉시 평가(코드 커밋 `2b71ea1`). 확인 수단 = CI 런 #40 유니티 잡(PlayMode 11/11). 빨가면 로그의 `result="Failed"` test-case 메시지를 PROGRESS T16 기록에 붙이고 다시 고친다.
3. 게이트 + PROGRESS T16 행 ✅ + lock 삭제.

### T20 — 웨이브 출발 버그 · 버프 스택 표시 = 팔각 프레임 (번호 정정: 구 T13) ✅ (완료 · `81780ac` · CI #43 초록 · PROGRESS 참조)
범위: `Assets/Scripts/Game/BattleWorld.cs` · `BattleScreen.cs`(RefreshBuffBar)
순서: 제약 없음(T14 캐릭터 크기·T16 정지는 끝났다).
1. **주인 지적 «웨이브 내 적을 다 안 죽였는데 출발함»** — 원인부터 규명해 PROGRESS 에 한 줄: ⓐ 엔진(Battle.Tick: alive[0] 까지 걷는다 · sim.js 와 동일해야 하므로 엔진은 못 바꾼다) ⓑ 연출(사망 표시를 Strike 큐가 미루는 동안 엔진은 이미 다음 적으로 걷는다 · 죽은 적이 Dead 루프로 서 있다 · 화면 밖 적) 중 어느 것인지. 고치는 쪽은 **연출**이다: 플레이어가 걷기 시작하는 순간 화면에 «살아 보이는» 적이 남아 있으면 안 된다(사망 연출을 즉시 시작하거나, Walk 전환을 사망 연출이 끝날 때까지 미룬다 — 엔진 좌표는 그대로 두고 표시만).
2. HUD 왼쪽 버프 바(발동 중 스택)의 칸을 `ui.buffSlot`(엉뚱한 프레임) 대신 **특전과 같은 팔각 `ItemFrame_04_*`(`UiKit.PerkFrame` · 등급색)** 로. 스택 수 글자는 그대로 오른쪽 아래.
3. 게이트 + PROGRESS T20 행.

### T21 — 투사체(도끼·화살·창·번개) 표적 = 웨이브 안 무작위 적 (번호 정정: 구 T14) ✅ (완료 · `39f4d1b` · 원본이 이미 무작위(−30~540px) · 엔진·그리기 변경 없음 · 승인 대기 27 종결 · PROGRESS 참조)
범위: `Assets/Scripts/Core/Battle.cs`(FireAxe/FireArrows/FireSpear/FireBolts · RandTarget) · `Assets/Scripts/Game/BattleWorld.cs`(투사체 그리기 — T20·T19 와 겹친다)
순서: **T20·T19 완료 뒤**.
1. 먼저 aaaw `sim.js` 의 `randTarget`/`fireAxe`/`fireArrows`/`fireSpear`/`fireBolts` 를 읽고 **원본이 무엇을 표적으로 삼는지** 적는다(무작위 범위 −30~540 · 관통 등). 우리 엔진이 원본과 다르면 엔진을 원본에 맞춘다(시드 골든 BattleTests 가 통과해야 한다 — 골든이 깨지면 엔진이 아니라 내가 틀린 것).
2. 엔진이 원본과 같은데도 화면에서 «맨 앞 적만 맞는 것처럼» 보이면 그리기 문제다: 투사체를 `pr.Target` 의 실제 x 로 날리고 적중 이펙트도 그 적 위치에.
3. 원본 자체가 «맨 앞만» 이라면 주인이 원하는 «웨이브 안 무작위» 는 규칙 변경이라 **승인 대기 27** 로 올리고(골든 재생성 필요) 기본값은 원본 유지.
4. 게이트 + PROGRESS T21 행.

### T22 — 모든 버튼에 눌림 표시 (번호 정정: 구 T15) ✅ (완료 · `f748948` · CI #45 초록(#44 는 취소) · PROGRESS 참조)
범위: `Assets/Scripts/Game/UiKit.cs`(Clickable · Button) · `Screens.cs`(NavBar 탭)
순서: 제약 없음(T10 이 Screens.cs 를 만지면 NavBar 부분은 T10 뒤).
1. `UiKit.Clickable` 로 만드는 **모든** 버튼(프리팹 버튼·탭·카드·칸)에 눌림 피드백: `Button.transition = ColorTint`(pressedColor 는 어둡게 ≈ ×0.8 · highlighted 는 그대로) + 이미 있는 DOPunchScale. targetGraphic 이 투명 히트 영역이면(칸·카드) 자식의 첫 Image 를 targetGraphic 으로 잡거나 CanvasGroup alpha 로 눌림을 보여 준다. 비활성(interactable=false)은 지금처럼 반투명.
2. 하단 탭(NavBar)의 «현재 탭» 강조는 그대로 두고 눌림만 추가.
3. 게이트 + PROGRESS T22 행.

### T23 — 쉼터 «광고 보고 둘 다 얻기» · 클리어 팝업 = 골드만 + «광고 보고 보상 ×2 받기» (번호 정정: 구 T16) ✅ (완료 · `9ea5d9d` · CI #51 · PROGRESS 참조)
범위: `Assets/Scripts/Game/Overlay.cs`(Rest · Clear) · `Assets/Scripts/Core/Battle.cs`(`ResolveRest` 에 «둘 다» 경로 1개 — 대화형 전용 · SimPolicy 는 절대 고르지 않으므로 시드 골든 불변) · `BattleScreen.cs`(EndRun 보상)
순서: T21 뒤(Battle.cs 공유).
1. **쉼터**: 기존 두 버튼(체력 회복 / 경험치) 아래에 **«광고 보고 둘 다 얻기»**(`ui.btnOrange` · 광고 카운트다운은 천사의 `AdCountdown` 재사용) → `G.ResolveRest(both)` = 회복 + 경험치 둘 다. 엔진에 `ResolveRestBoth()` 를 추가하되 `ResolveRest(bool)` 은 그대로.
2. **클리어 팝업(Play_Result_Win_01)**: 보상 표시는 **골드만**(프리팹의 다른 두 보상 칸은 끈다). 버튼은 «다음 챕터» 대신 **«광고 보고 보상 ×2 받기»**(광고 카운트다운 뒤 클리어 골드를 2배로 지급하고 로비로). 그 아래 작은 글자 버튼 **«그냥 받기»**(1배 · 로비로) 를 둔다 — 광고를 안 보면 못 나가는 것을 막기 위한 기본값(승인 대기 28). «다음 챕터» 진입은 로비의 챕터 화살표로.
3. 게이트 + PROGRESS T23 행 + 승인 대기 28.

### T17 — 장비 아이콘 마무리 (투구·갑옷·무기 크기 · ~~무기 45°~~ 취소 · 근접 무기만) (T7 뒤) ✅ (완료 · `6918f71`+`8c8d60e`+`ec1a91a` · CI #47 초록 · PROGRESS 참조)
범위: `Assets/Scripts/Game/GearUi.cs`(Cell 아이콘) · `GearScreen.cs`(슬롯 아이콘) · `GearLook` 표(T7 이 만든 것) · catalog
순서: **T7 완료 뒤**(같은 파일·표).
1. **주인 지적 «투구·갑옷·무기 아이콘만 작다»** — CharacterMaker 파츠 스프라이트는 GUI Pro 아이콘(128px)보다 작아 같은 칸에서 작게 보인다. 파츠 아이콘은 칸 안에서 다른 부위 아이콘과 **같은 시각 크기**로(스프라이트 bounds 기준으로 칸의 70~75% 를 채우도록 스케일 · preserveAspect). 장착 슬롯·인벤 칸·세부 팝업·대장간·뽑기 결과 전부.
2. ~~무기 아이콘 45° 회전~~ — **취소**(주인 2026-09-06: Thumbnail 그림을 쓰면 투구·무기·갑옷 전부 정상 방향이라 기울일 필요 없다 · T31). 회전 0.
3. **무기는 전부 근접 무기** — 활·지팡이·완드·창 계열 금지. `GearLook` 표에서 무기 종류 × 등급을 **검(Sword)·방망이(Blunt)·도끼(Axe)** 세 계열의 파츠에서 고른다(주인 2026-09-06 «Axe, Blunt, Sword 중에 골라서» · 등급이 오를수록 화려한 것 · 착용 파츠 슬롯은 HandRight/Sword·Blunt·Axe 중 종류에 맞는 것). 카탈로그 키·`docs/assets-map.md` 갱신.
4. 게이트 + PROGRESS T17 행.

### T18 — 배속(x2) 기억 ✅ (완료 · `73e38de` · CI #45 · PROGRESS 참조)
범위: `Assets/Scripts/Game/BattleScreen.cs`(_speed) · `SaveStore.cs`/`SaveData`(필드 1개)
순서: 제약 없음.
1. 배속 버튼 값(x1/x2)을 세이브(`SaveData.Speed` · PlayerPrefs)에 저장하고, 전투 시작 시 그 값으로 시작한다(«2배속으로 하다가 클리어 뒤 다른 챕터 도전하면 다시 1배속» 이 안 되게). index.html `kkoma-knight-v2` 에 없는 필드라 세이브 호환은 «없으면 1».
2. 게이트 + PROGRESS T18 행.

### T19 — 전투 맵 = 데모 씬 «그림» 그대로 (T20 뒤 · 주인 재지적) ✅ (완료 · `dedeffb` · CI #48 · PROGRESS 참조 — 씬에는 세로 반전이 없고 Road_Up/Road_Down 두 그룹이 같은 스프라이트를 양쪽에 둔다)
범위: `Assets/Scripts/Game/BattleWorld.cs`(BuildGround/BuildProps) · `tools/gen_maps.py` · `Assets/Scripts/Game/MapLayouts.cs`(재생성)
순서: **T20 완료 뒤**(BattleWorld 공유). T21 보다 먼저.
> 주인(2026-09-06): «맵 디자인을 DemoScene_Autumn/DeepForest/Desert/Forest 씬에 있는 거 그대로 가져와서 쓰라니까 안 쓰네? 지금 맵이 디자인 존나 다르던데». 씬을 그림으로 보려면 `python3 tools/demo_render/render_demo_scene.py <폴더>` + `node tools/demo_render/shot.js <폴더>`(Playwright · PNG 는 커밋 금지). 데모 씬의 모습: 평면색 들판 화면 전체 · 가운데 **두꺼운 길 띠(2.46u · 화면 높이의 1/4)** · 길 **위·아래 양쪽** 가장자리에 물결 풀 경계 · 길 위쪽 들판과 아래쪽 들판 모두에 나무·돌·통·꽃이 **빽빽하게**(17.8u 폭 화면에 30~60개).
1. **왜 다른가를 먼저 적는다**(PROGRESS 한 줄). 지금 코드가 틀린 점: ⓐ 물결 경계(Road_up)를 길 **아래쪽만** 깔았다 — 씬에는 위·아래 양쪽(세로 반전 인스턴스 · `m_LocalScale.y = -1`)이다 ⓑ 세로 잘림 — 우리 화면에서 월드가 보이는 띠는 HUD 위(17%)~HUD 패널(69.5%) = 6u 인데 데모는 10u 를 보여 준다 → 데모 y 범위 −5~+5 의 소품 대부분이 잘리거나 HUD 뒤에 숨는다 ⓒ 가로도 5.4u 슬라이스라 한 화면에 소품이 2~4개뿐 → «휑하다».
2. 고치는 법: **데모 구성을 통째로 0.6배**(`Layout.MapScale = 0.6f`)로 그린다 — 길 띠 2.46u→1.48u(발 줄 40% 을 품게 중심 41%), 소품 위치·크기 모두 ×0.6 (씬 폭 27u→16u · 우리 5.4u 창에 데모 화면의 1/3 이 보인다 = 데모 창(17.8u)이 씬의 2/3 을 보이는 것과 같은 밀도). 캐릭터 키는 T14 의 2/3 배율 그대로(0.69u · 길 띠 1.48u 안에 서면 데모의 샘플 캐릭터 비율과 비슷).
3. `tools/gen_maps.py`: Road_up 을 특수 처리하지 말고 **다른 소품과 똑같이 인스턴스 그대로**(x·y·sx·sy · 세로 반전 포함) 표에 넣는다 → 위·아래 양쪽 물결이 씬대로 나온다. 씬 폭 반복은 그대로.
4. 정렬: 소품의 뿌리 y 가 발 줄보다 위면 캐릭터 뒤, 아래면 앞(지금 규칙) — 길 띠 안(캐릭터 줄)에는 소품이 없다.
5. 검증: 워커는 WebGL 을 못 돌리므로, `tools/demo_render` 의 방식으로 **우리 배치 표(MapLayouts × 0.6 · 5.4u 창)** 를 같은 HTML 로 그려 데모 그림과 나란히 보고(PNG 커밋 금지) PROGRESS 에 «같다/다른 점» 한 줄. 대화형 세션이 gh-pages 스크린샷으로 최종 확인한다.
6. 게이트 + PROGRESS T19 행.

### T24 — 대장간: 장착 중 장비도 합성 재료 (T8 뒤 · 주인 2026-09-06) ✅ (완료 · `920fe0b` · PROGRESS 참조)
범위: `Assets/Scripts/Game/ForgeScreen.cs` · `GearUi.cs`(Cell 흐림 옵션) · `Assets/Scripts/Core/GearSystem.cs`(FuseAll 장착 제외 인자)
순서: 제약 없음(T8 ✅ · T17 이 GearUi 아이콘 부분을 만지므로 Cell 의 «흐림» 한 줄만 조심).
1. 주인 «대장간에 장착중인 거도 합성 가능하게». `ForgeScreen.Toggle` 의 장착분 거부(토스트)·흐림을 없애고 장착중 배지(Check)는 유지. «자동» 도 장착분을 포함해 합성(`FuseAll` 호출에서 `EquippedSet` 제외를 뺀다 — 함수 시그니처는 두고 빈 집합을 넘겨도 된다).
2. 장착 중이던 것이 재료로 사라지면: 결과물이 **같은 부위면 그 슬롯에 장착**, 아니면 슬롯 비움(승인 대기 29 기본값). 세이브 갱신·전투력 재계산·외형(GearLook) 갱신까지.
3. aaaw T125(«장착분은 재료가 아니다»)는 주인이 뒤집었다 — PROGRESS 한 줄. UiSmokeTests ③ 의 «장착분 재료 불가» 가정이 있으면 새 규칙으로 고친다.
4. 게이트 + PROGRESS T24 행.

### T25 — 장비 화면: «상점»·«합성» 버튼을 공격력·체력·실드 3칸 바로 아래로 + 캐릭터 크기 (T7 뒤 · 주인 2026-09-06) — **⛔ T37 에 흡수(2026-09-06 UI 레퍼런스 지시) · 잡지 않는다**
범위: `Assets/Scripts/Game/GearScreen.cs` · `HeroView.cs`(렌더 카메라 크기)
순서: **T17 완료 뒤**(GearScreen 공유).
0. **캐릭터가 너무 작다**(주인). Character_Hero_Equipment 프리팹의 샘플 캐릭터(Sample_Cha02_l 자리)가 차지하는 **RectTransform 크기 그대로** HeroView 의 RawImage 를 맞추고, 렌더 카메라의 orthographicSize 를 캐릭터(Character.prefab 키 0.85u)가 그 사각형의 세로 **85~90%** 를 채우도록 잡는다(발이 사각형 아래에서 5% 위 · 머리·투구 위 여백 5%). 로비 초상(T6 의 HeroView 재사용)은 자기 사각형 기준으로 같은 규칙(따로 배율 인자). 근거 수치(프리팹 사각형 px · 카메라 size)를 PROGRESS 에 한 줄.
1. 주인 «장비 부분에서 상점·합성, 공격력·체력·실드 표시하는 곳 밑에 표시되게». 지금 자리(화면 아래·탭바 위)에서 **스탯 3칸 줄 바로 아래**로 옮긴다. 프리팹(Character_Hero_Equipment)에 그 자리 버튼 줄이 있으면 그것을 쓰고, 없으면 `ui.btnGray`(상점)·`ui.btnOrange`(합성 N) 2개를 스탯 줄 밑에 같은 폭으로 나란히. 인벤 격자는 그 아래부터 시작(높이 줄어든 만큼 스크롤).
2. 게이트 + PROGRESS T25 행.

### T26 — 뽑기 확률 검증 (주인 «확률에 안 맞게 뽑히는 것 같다») ✅ (완료 · `9781557` · 어긋난 곳 없음 — 테스트 +6 만 · CI #50 · PROGRESS 참조)
범위: `Assets/Scripts/Core/GearSystem.cs`(뽑기 · 천장) · `Assets/Tests/EditMode/GearTests.cs`(통계 테스트 추가) · `Assets/Scripts/Game/ShopScreen.cs`(표시 문구가 실제 확률과 같은지)
순서: 제약 없음.
1. 먼저 **원본**을 읽는다: aaaw `index.html` 의 뽑기(`pull`/`rollRar`/천장 `pity`)와 `data/gacha.json` 의 상자별 등급 확률·천장. 우리 `GearSystem` 의 굴림이 원본과 **같은 순서·같은 난수 소비**인지 줄 단위로 대조.
2. **통계 테스트**(EditMode · dotnet 도 돌린다): 상자마다 10,000회 뽑아 등급 분포가 gacha.json 확률의 ±1.5%p 안인지 · 천장(전설/신화 확정 회차)이 정확히 그 회차에 발동하는지 · 10회 뽑기가 1회 뽑기 10번과 같은 분포인지. 난수는 `Mulberry32` 시드 고정.
3. 어긋나면 **코드**를 고친다(gacha.json 은 aaaw 정본 · 손대지 않는다). 흔한 원인: 등급 순서(신화→전설→…)와 누적 확률 순서 불일치 · 천장 카운터가 상자별이 아니라 공용 · 0% 등급 처리 · 10회 뽑기의 «최소 희귀 보장» 유무(원본대로).
4. 상점 카드의 확률 문구(«전설 N% …»)가 실제 굴림과 같은 수치인지 확인.
5. 게이트 + PROGRESS T26 행 + 원인 한 줄(정말 어긋났는지, 아니면 표본이 작아 그렇게 보인 것인지 — 후자면 그렇게 적는다).

### T27 — 장비 정보 팝업 = Character_Hero_Item_Detail_01 그대로 (주인 재지적) — **⛔ 폐기 → T38(레퍼런스 `07` 구도) · 잡지 않는다**
범위: `Assets/Scripts/Game/GearUi.cs`(OpenDetail · OpenSlot) · catalog(`ui.itemDetail`)
순서: **T17 완료 뒤**(GearUi 공유). T25 와는 파일이 다르다.
1. 주인 «장비 정보 팝업이 Character_Hero_Item_Detail_01 을 써야 하는데 안 쓰는 듯». 지금 `OpenDetail` 이 `ui.itemDetail` 을 스폰하는지, 스폰하더라도 내부 요소를 옮기거나 우리 상자(Popup_Box)로 감싸 프리팹 모양이 사라졌는지 확인한다. **프리팹을 원형 그대로**(어둠 + 프리팹 루트 스트레치 · 내부 요소 Pct 이동 금지) 두고 글자·아이콘·등급색·버튼 문구만 바꾼다. 세부 정보(이름·부위·세트·슬롯 Lv·기여 3수치·세트 옵션·장착/해제/슬롯 강화)는 프리팹의 해당 자리에.
2. 빈 슬롯 팝업(OpenSlot)도 같은 프리팹(장비 없는 상태 · 강화만).
3. UiSmokeTests ② 가 «세부 팝업 = Character_Hero_Item_Detail_01» 인지 프리팹 이름으로 단언하도록 보강.
4. 게이트 + PROGRESS T27 행 + «주인이 확인할 것».

### T28 — 배경음(BGM) · 효과음(SFX) (주인 2026-09-06 «인터넷에서 받아서 넣어라») ✅ (완료 · `9c1eb54` · CI #52(취소되면 #53) · PROGRESS 참조 — CC0 20개 GitHub 미러 · Audio/AudioManager · Settings BGM/SFX 스위치)
범위: `Assets/Audio/`(신규 · 오디오 파일 + `LICENSES.md`) · `Assets/Scripts/Game/Audio.cs`(신규 · AudioManager) · 각 화면/팝업의 호출 한 줄씩(Screens · BattleScreen · BattleWorld(타격) · Overlay · GearUi · ShopScreen · ForgeScreen) · `SaveStore`(음소거 2개) · catalog(`bgm.*`/`snd.*`)
순서: 제약 없음(호출은 한 줄씩이라 다른 작업과 겹쳐도 rebase 로 풀린다 — 충돌 나면 내 줄만 다시).
1. **에셋 구하기**: 이 환경의 프록시는 kenney.nl·opengameart.org·freesound.org 를 막는다(디스패처가 확인 · 000). **GitHub 는 열려 있다** → `git clone --depth 1` 로 받을 수 있는 **CC0/퍼블릭 도메인** 팩만 쓴다(예: Kenney 의 GitHub 미러 · OpenGameArt CC0 모음 미러 · «cc0 game sfx» 검색). 라이선스가 CC0/PD 가 아니면 쓰지 않는다. 받은 파일의 출처·라이선스를 `Assets/Audio/LICENSES.md` 에 한 줄씩(URL · 원작자 · 라이선스). **GitHub 에서도 못 구하면** 오디오 시스템(2~4)만 만들고 `Assets/Audio/README.md` 에 «주인이 파일을 이 폴더에 넣으면 catalog 한 줄로 붙는다» 를 적고 PROGRESS 승인 대기 30 에 등재.
2. **바이너리 예외**(§1 «대용량 바이너리 금지» 의 두 번째 예외 · 주인 지시): 형식 OGG(Vorbis) · 파일당 ≤ 300KB · BGM 은 ≤ 1MB · **합계 ≤ 5MB**. WAV 는 ffmpeg 로 OGG 변환(`ffmpeg -i in.wav -c:a libvorbis -q:a 3 out.ogg`). `.meta` 는 gen_meta 로.
3. **필요한 소리**(카탈로그 키): BGM = `bgm.lobby` · `bgm.battle`(맵 4종 공용 1곡 · 있으면 테마별 4곡) · `bgm.boss`. SFX = `snd.click`(모든 버튼 · UiKit.Clickable 에서 한 곳) · `snd.hit` · `snd.crit` · `snd.miss` · `snd.kill` · `snd.hurt`(플레이어 피격) · `snd.levelup` · `snd.perk`(특전 선택) · `snd.coin`(골드 획득) · `snd.popup`(팝업 열림) · `snd.gacha`(상자 열림) · `snd.fuse`(합성) · `snd.equip` · `snd.clear` · `snd.fail` · `snd.arrow`/`snd.axe`(투사체 · 없으면 hit 재사용).
4. **AudioManager**(`Audio.cs` · App 이 만든다 · AudioSource 2개: BGM 루프 1 + SFX 풀): `Audio.Bgm(key)`(같은 곡이면 무시 · 0.5초 크로스페이드) · `Audio.Sfx(key, volume=1, pitchJitter=0.05)` · 화면 전환 시 로비/전투 곡 자동 교체 · 보스 등장(BossWarn)에서 boss 곡 · 배속 x2 여도 피치 그대로. **설정 팝업(Settings 프리팹)의 BGM/SFX 스위치**에 각각 연결(`Save.MuteBgm`·`Save.MuteSfx` — 기존 `Muted` 는 BGM 으로 이관 · 세이브 호환). WebGL 은 첫 터치 뒤에 소리가 난다(브라우저 정책) — START 버튼 클릭에서 AudioContext 를 깨우는 코드 한 줄.
5. UiSmokeTests 에 «BGM 키가 화면마다 바뀌는가 · SFX 호출이 예외 없이 도는가(클립 없어도 경고만)» 를 추가. 클립이 없을 때는 조용히 넘어간다(에러 0).
6. 게이트 + PROGRESS T28 행 + «주인이 확인할 것»(어떤 팩을 어디서 받았는지 표).

### T29 — 설정 팝업에 «데이터 삭제» (주인 2026-09-06) ✅ (완료 · `5e92205` · CI #57 · PROGRESS 참조 — 빨간 «Account Delete» 자리 · 확인 팝업 · SaveStore.Reset)
범위: `Assets/Scripts/Game/Overlay.cs`(Settings 팝업) · `SaveStore.cs`(Reset)
순서: 제약 없음(T28 이 Settings 의 BGM/SFX 스위치 줄을 만진다 — 그 줄만 피한다).
1. Settings 프리팹(그대로 원칙)의 버튼 중 하나를 «데이터 삭제» 로 쓴다(프리팹에 남는 버튼이 없으면 프리팹 안 버튼 줄 아래에 `ui.btnRed` 1개 — 유일한 추가). 누르면 **확인 팝업**(Popup_Box + «정말 삭제할까요? 장비·골드·보석·진행이 모두 사라집니다» · «삭제»(빨강) / «취소»). «삭제» 는 `SaveStore.Reset()`(PlayerPrefs 의 세이브 키 삭제 → 새 세이브 생성 · 배속·음소거 같은 설정값도 초기화) 뒤 로비로 돌아가 화면을 새로 그린다(전투 중이면 전투를 끝내고).
2. UiSmokeTests ① 에 «데이터 삭제 → 확인 → 세이브가 초기값(골드 0 · 장비 0 · 챕터 1)» 단언 추가.
3. 게이트 + PROGRESS T29 행.

### T30 — 하단 탭 «탤런트» → «던전» (World_Dungeon_List · 각 항목 → World_Dungeon_Start1/2) (주인 2026-09-06) — **⛔ 폐기 → T43(레퍼런스 `20`~`26` 구도 · 탭 이름 변경 포함) · 잡지 않는다**
범위: `Assets/Scripts/Game/Screens.cs`(NavBar 탭 · 던전 팝업 진입) · `Overlay.cs`(던전 팝업 2단) · catalog(`ui.dungeonList` = World_Dungeon_List · `ui.dungeonStart1` = World_Dungeon_Start1 · `ui.dungeonStart2` = World_Dungeon_Start2)
순서: 제약 없음(T10 ✅ · T22 가 NavBar 눌림을 만지면 rebase).
1. 탭 5칸을 **상점 · 장비 · 전투 · 던전 · 펫** 으로(«탤런트» 탭·Character_Talent_02 팝업 진입 제거 · 펫은 그대로). 탭 아이콘은 GUI Pro 던전 아이콘.
2. «던전» = `World_Dungeon_List` 프리팹 팝업 **그대로**(요소 이동·삭제 금지 · 제목만 «던전»). 목록의 각 항목 «입장(Enter)» 클릭 → 항목별로 `World_Dungeon_Start1` 또는 `World_Dungeon_Start2` 프리팹 팝업 **그대로**(첫 항목 Start1 · 둘째 Start2 · 그 뒤는 번갈아). 기능은 없다 — 팝업 열고 닫기만(«시작» 버튼은 누르면 팝업이 닫힌다 · 토스트 «준비 중»). 닫기(X) 로 목록으로, 목록의 X 로 원래 화면으로.
3. UiSmokeTests ① 의 «탤런트» 단언을 «던전 목록 → Start1 → 닫기 → 둘째 → Start2 → 닫기» 로 바꾼다.
4. 게이트 + PROGRESS T30 행.

### T31 — 장비 아이콘 = CharacterMaker «Thumbnail» 그림 (입는 파츠와 아이콘을 분리 · 주인 2026-09-06) (T17 뒤) ✅ (완료 · `07bbc86` · CI #58 · PROGRESS 참조 — cm.gear.* 착용 / cmi.gear.* 아이콘 · Thumbnail 36개 전부 같은 이름 · 임시 대체 0)
범위: `GearLook` 표 · `Assets/Scripts/Game/GearUi.cs`(아이콘 키) · catalog(`cmi.*` 신규 키)
순서: **T17 완료 뒤**(같은 표·파일). T25·T27 과는 줄이 다르다.
1. CharacterMaker 팩은 파츠마다 **입는 그림**(`Extenstions/Parts Pack Base/Parts/<부위>/<이름>.png`)과 **아이콘용 그림**(`…/Thumbnail/<부위>/<같은 이름>.png` · Helmet·Chest·Sword·Blunt·Axe·Spear·Bow·Shield 등)이 따로 있다. 주인: «막상 장착 아이콘용 갑옷이랑 실제 입는 갑옷이 다르게 돼 있는데 내 게임도 그렇게». → `GearLook` 표의 각 항목에 **아이콘 키(Thumbnail)** 와 **착용 키(Parts)** 를 둘 다 두고, 장비 칸·슬롯·세부 팝업·뽑기 결과·대장간의 아이콘은 **Thumbnail** 을, 캐릭터(HeroView·전투)는 **Parts** 를 쓴다. 카탈로그 키는 `cmi.<part>.<name>`(Thumbnail) / 기존 `cm.*`(Parts). 같은 이름이 Thumbnail 에 없으면 PROGRESS 에 목록으로 남기고 Parts 그림을 임시로.
2. T17 의 «파츠 아이콘 크기 통일» 은 Thumbnail 기준으로 다시 맞춘다. **회전은 0**(주인 확정: Thumbnail 은 투구·무기·갑옷 전부 정상 방향 — 45° 는 취소).
3. `docs/assets-map.md` 갱신(gen_catalog).
4. 게이트 + PROGRESS T31 행 + «주인이 확인할 것».

### T32 — 펫 팝업 = Character_Skill 그대로 · 항목 클릭 → Character_Skill_Detail 그대로 (주인 2026-09-06) — **⛔ 폐기 → T42(레퍼런스 `13`·`14` 구도) · 잡지 않는다**
범위: `Assets/Scripts/Game/Overlay.cs`(펫 팝업 2단) · `Screens.cs`(펫 탭 진입 한 줄) · catalog(`ui.pet` = Character_Skill · `ui.petDetail` = Character_Skill_Detail)
순서: 제약 없음(T30 이 던전 탭·Screens 를 만지면 rebase · 펫 탭 진입 줄만 겹친다).
1. 하단 탭 «펫» = `Character_Skill` 프리팹 팝업 **그대로**(요소 이동·삭제 금지 · 제목만 «펫» · 지금의 Character_Talent_02 팝업은 버린다). 목록의 각 항목 클릭 → `Character_Skill_Detail` 프리팹 팝업 **그대로**(제목·이름만 «펫 N»). 기능은 없다 — 열고 닫기만(버튼은 누르면 토스트 «준비 중»). 닫기(X) 로 목록으로, 목록의 X 로 원래 화면으로.
2. UiSmokeTests ① 의 «펫» 단언을 «Character_Skill → 항목 → Character_Skill_Detail → 닫기 → 닫기» 로 바꾼다(프리팹 이름으로 단언).
3. 게이트 + PROGRESS T32 행.

### T33 — 전투 HUD 웨이브 수 표시 제거 (주인 2026-09-06) ✅ (완료 · `5938425` · CI #55 · PROGRESS 참조)
범위: `Assets/Scripts/Game/BattleScreen.cs`(HUD `_round` · `ui.frameDark` 라운드 상자)
순서: 제약 없음(T20 이 BattleScreen 의 버프 바를 만지면 rebase · 다른 줄).
1. 오른쪽 «웨이브 N/M» 상자(`Layout.HudRound` 자리 · `_round`)를 만들지 않는다(코드·갱신 함께 제거 · Layout 상수는 표 대조 테스트 때문에 남긴다). 다른 HUD 요소는 그대로.
2. UiSmokeTests ⑤ 의 «HUD 웨이브» 단언을 지운다.
3. 게이트 + PROGRESS T33 행.

### T34 — 로비 = `docs/ref/01_lobby.jpg` 구도 (UI 레퍼런스 · 최우선) ✅ (완료 · `d6d1411` · 상단 재화 바 = `TopBar` 헬퍼(Screens.cs) — T37·T40·T42·T43 은 `TopBar.Build(App, root)` 한 줄로 · 사이드/배너/모서리 버튼은 `LobbyScreen.OnSide(key)` 훅 · PROGRESS 참조) · **코드 ✅ · 비평 회차 1 = 9.7 ✅(screens CI #83 · 워커 E · T47)**
범위: `Assets/Scripts/Game/Screens.cs`(LobbyScreen · NavBar 훅) · `HeroView.cs`(초상) · catalog(로비 조각 키) · `Assets/Tests/PlayMode/UiSmokeTests.cs`(로비 단언)
순서: **T22(버튼 눌림 · Screens NavBar) 완료 뒤**. T42·T43·T44 가 이 화면의 버튼 훅을 이어 쓴다.
1. `docs/ref/README.md` «01 로비» + `ref-layout.md` ① 표대로: 상단 재화 바(아바타 · 전투력 · 골드 · 보석) → 이벤트 배너(보라 · 진행바 · 레벨 뱃지 — 패스 껍데기 T44 진입) + 메뉴(≡ · 설정) → 왼쪽 세로 아이콘 3(스타터팩·특권·7일 챌린지) / 오른쪽 세로 3(출석·데일리 기프트·퀘스트) → «CHAPTER N» + 밑줄 화살 → 챕터 카드(1.25:1) + ◀▶ → 보조 버튼 2(탐험·클리어 보상 — 껍데기) → START(주황 · 카드 폭) → 왼쪽 아래 성(잠금) · 오른쪽 아래 이벤트(방패 — T43 던전/아레나 진입) → 탭 바(상점·장비·전투·던전·펫).
2. 재료 = GUI Pro 조각(Lobby_Default 의 상단 바·탭 바·버튼을 뜯어 쓴다 · 세로 아이콘은 GUI Pro 아이콘 + 라벨). 사이드 아이콘·배너·보조 버튼은 T44 가 채울 때까지 **눌러도 아무 일 없음**(`LobbyScreen.OnSide(string key)` 훅 하나로 모아 둔다).
3. 게이트 + 스모크 단언(사이드 3+3 · START · 탭 5) + PROGRESS T34 행.

### T35 — 전투 HUD = `02_battle.jpg`·`03_battle_enemy.jpg` 구도 + **HP·실드 바(주인 강조)** (T33 뒤) ✅ (완료 · `4773d23` · CI #56 · PROGRESS 참조) · **코드 ✅ · 비평 회차 1 = 8.6 ✅(캔버스 12/14 · screens CI #83 · 워커 E) · 월드 8행·02 촬영은 T47 회차 2(코드)**
범위: `Assets/Scripts/Game/BattleScreen.cs`(HUD 전체) · `BattleWorld.cs`(발밑 2단 바) · `Assets/Scripts/Core/Layout.cs`(HUD 상수 — 표값을 바꾸면 `ref-layout.md` ② 와 `LayoutSpecTests` 도 같이) · catalog
순서: **T33(웨이브 수 제거 · 같은 HUD 영역) 뒤**. T19·T21·T23 과는 같은 파일이지만 다른 영역(지형·투사체·EndRun) — rebase 로 합치고 충돌 나면 뒤 번호가 다시.
1. **바 3개 한 줄**(HUD 스탯 격자 바로 위): EXP(초록 라벨 «EXP» + 검정 바 + «0/6») · ❤ HP(빨강 바 · «1055/1055») · 🛡 실드(파랑 바 · «2258/2258») — 각 바 왼쪽에 아이콘, 바 안에 흰 숫자. 실드 = 엔진의 방어막/실드 값(없으면 0/최대).
2. **발밑 2단 바**: 적·플레이어 모두 빨강(HP) 위에 파랑(실드) — 숫자를 바 안에 흰 글자로 · 바 폭 = 캐릭터 폭(T14 의 2/3 배율 유지). 실드 0 이면 파란 단은 숨긴다.
3. 상단: 왼쪽 작은 pill 2(처치 수 · 이번 판 골드) · 가운데 «CHAPTER N» + 진행바(웨이브 진행 · 적 조우 시 주황 · 숫자 없음 — T33) · 오른쪽 메뉴(≡ · 일시정지) · 왼쪽 아래 배속 «x1/x2»(T18 기억) · 오른쪽 아래 둥근 버튼(펫 — 껍데기).
4. **스탯 8칸 2열×4행**(아이콘 · 이름 작게 · 값 크게 · 버프 중 값 초록) → 맨 아래 특전 미리보기 줄(T13 비례 유지) + 오른쪽 📘(보유 특전 팝업). 왼쪽 버프 스택은 팔각 프레임(T20 그대로).
5. 게이트 + PlayMode(전투 3초 · 바 3개 존재 · 발밑 바 2단) + PROGRESS T35 행.

### T36 — 레벨업 3택 · 보유 특전 팝업 = `04_perks.jpg`·`05_perks_list.jpg` 구도 · **✅ 조건 = 비평 ≥ 8.0/10(§5)** ✅ (완료 · 코드 `1a9cec4` · CI #59 · 공통 팝업 헬퍼 = `UiKit.Popup` · **비평 회차 1 = 04 9.5 · 05 9.5**(CI #83 screens 첫 PNG · 워커 H) · PROGRESS 참조)
범위: `Assets/Scripts/Game/Overlay.cs`(LevelUp · PerkList) · `UiKit.cs`(PerkFrame·카드·공통 팝업 헬퍼 `UiKit.Popup(title, ribbon)`) · catalog
순서: 제약 없음(T23·T29 가 Overlay 의 다른 팝업을 만진다 — rebase). **공통 팝업 헬퍼는 여기서 먼저 만든다**(T38·T41·T42·T44 가 쓴다).
1. 3택: 배경 어둡게 · 상단 스탯 8칸을 한 줄 미니 아이콘으로 · 노란 광선 + «Level Up!» 리본 · «새 특전을 고르세요» · **카드 3장 세로 전폭**(왼쪽 위 등급 탭 · 왼쪽 팔각 아이콘 · 오른쪽 설명 · ~~수치 초록~~ → T52 로 취소(한 색)) · 아래 «새로고침 무료» 주황 + «남은 횟수 : N» · 오른쪽 📘. 규칙(새로고침 횟수 등)은 기존 코드 그대로 — 배치만.
2. 보유 특전: 명판 «특전» + 긴 패널 + 같은 카드 형식 세로 나열(스크롤) · «탭하여 닫기».
3. 악마·천사·쉼터 팝업은 같은 패널·명판·버튼 문법으로 통일(레퍼런스 없음 · README «공통 문법» 절).
4. 게이트 + PROGRESS T36 행.

### T37 — 장비 화면 = `06_gear.jpg` 구도 (T17 뒤 · T25 흡수) · **✅ 조건 = 비평 ≥ 8.0/10(§5)** ✅ (완료 · 코드 `e031e1c` · CI #60 · **비평 회차 1 = 10.0**(screens CI #83 · 워커 G) · PROGRESS 참조)
범위: `Assets/Scripts/Game/GearScreen.cs` · `GearUi.cs`(칸 문법) · `HeroView.cs`(무대 배경 · 캐릭터 크기) · catalog
순서: **T17 완료 뒤**(GearScreen/GearUi 공유). T31(아이콘 키)은 다른 줄 — rebase. T38·T39 가 뒤따른다. **T25 는 이 작업에 흡수**(캐릭터 크기 · 버튼 위치 = 레퍼런스의 자리).
1. 상단 재화 바 → **캐릭터 무대**(Environment 배경 · 가운데 큰 플레이어 — 무대 세로의 85~90% · 좌 3 / 우 3 슬롯 · 슬롯 위 «Lv. N» · «+N» 배지) → **스탯 3칸 한 줄**(공 · ❤ · 🛡) → 그 바로 아래 오른쪽 **«대장간» 주황 버튼**(합성 가능하면 빨간 !) + 왼쪽 «상점»(회색) → **인벤 5열 격자**(스크롤 · 등급색 프레임 · 부위 소아이콘 · +N) → 탭 바. ref-layout ④.
2. 칸 문법(등급색 프레임 · 왼쪽 위 부위 아이콘 · 왼쪽 아래 +N · 가운데 «장착중») 을 `GearUi.Cell` 한 곳에 — 세부·대장간·뽑기 결과가 같은 칸을 쓴다.
3. 게이트 + PROGRESS T37 행.

### T38 — 장비 세부 팝업 = `07_gear_detail.jpg` 구도 (T37 뒤 · T27 대체) · **✅ 조건 = 비평 ≥ 8.0/10(§5)** ✅ (완료 · 코드 `7346617` · CI #63 · **비평 회차 1 = 8.5**(표 9.0 · 등급 배지 폭만 0점 · CI #83 screens · 워커 H · 선택 처방 = 점수판) · PROGRESS 참조)
범위: `Assets/Scripts/Game/GearUi.cs`(OpenDetail · OpenSlot) 또는 `GearDetail.cs`(신규)
순서: **T37 뒤**. T27(«Character_Hero_Item_Detail_01 그대로»)은 **폐기** — 그 프리팹은 부품으로만.
1. 위 등급 탭 → 패널: 왼쪽 아이콘 칸(+N) · 오른쪽 이름 굵게 + «레벨 N/최대» · «부위» 두 pill → 스탯 박스(초록 +값) → 옵션 줄(등급색 · 잠금은 자물쇠 + 흐림) → 비용 줄(골드 · 재료) → **해제(파랑) · 강화(주황)** → «탭하여 닫기». 규칙·수치는 기존 코드 그대로. 빈 슬롯 팝업도 같은 구도(장비 없는 상태 · 강화만).
2. 게이트 + PROGRESS T38 행.

### T39 — 대장간 = `08_gear_fuse.jpg` 구도 (T37 뒤) · **✅ 조건 = 비평 ≥ 8.0/10(§5)** ✅ (완료 · 코드 `48d05a6` · CI #66 · **비평 회차 1 = 9.5**(screens CI #83 · 워커 G · −0.5 인벤 순서 = 선택 처방) · PROGRESS 참조)
범위: `Assets/Scripts/Game/ForgeScreen.cs`
1. 위 절반 대장간 그림(Environment/GUI Pro 조각) 위에 **선택 칸(초록 테두리) · 모루 · + 칸** + «합성할 장비를 고르세요» → **자동(파랑 · !) · 합성(회색→가능하면 주황)** 한 줄 → 인벤 5열(합성 가능 = 초록 프레임 + 빨간 ! · 장착 = «장착중» 배지 · T24 대로 재료 가능) → 왼쪽 아래 뒤로(◀). ref-layout ⑥. `FuseMake` 규칙 그대로.
2. 게이트 + PROGRESS T39 행.

### T40 — 상점 = `09_shop_1.jpg`·`10_shop_2.jpg` 구도 · **✅ 조건 = 비평 ≥ 8.0/10(§5)** ✅ (완료 · 코드 `c922d5a` · CI #64 · **비평 회차 1 = 09 10.0 · 10 10.0**(screens CI #83 · 워커 G) · PROGRESS 참조)
범위: `Assets/Scripts/Game/ShopScreen.cs` · catalog
순서: 제약 없음(T26 이 확률 문구 한 줄을 만진다 — rebase).
1. 상단 재화 바 → 천막 띠 → **전설(최상위) 상자 큰 카드**(그림 왼쪽 · 설명·천장 문구 오른쪽 · «열기 💎가격» · «10회 💎가격») → **나머지 상자 2칸 나란히**(광고 버튼 + 보석 가격 · «무료까지 hh:mm:ss» = 무료 보급) → «다이아» 섹션 3열×2행(수량 · 그림 · 이름 · ₩) → «골드» 3열×1행(💎가격) → 탭 바. 수치는 `gacha.json`·`shop.json` 그대로. ref-layout ⑦.
2. 뽑기 결과 팝업은 공통 팝업 문법(명판 · 패널 · 격자 = GearUi.Cell · 탭하여 닫기).
3. 게이트 + PROGRESS T40 행.

### T41 — 설정 팝업 = `12_settings.jpg` 구도 (T29 뒤) · **✅ 조건 = 비평 ≥ 8.0/10(§5)** ✅ (완료 · 코드 `b0bf87d` · CI #61 · 표 ⑨ 신설 · **비평 회차 1 = 10.0**(screens CI #83 · 워커 E) · PROGRESS 참조)
범위: `Assets/Scripts/Game/Overlay.cs`(Settings)
순서: **T29(데이터 삭제) 뒤** · T28 의 BGM/SFX 스위치 줄은 유지.
1. 작은 패널 · 명판 «설정» · **음악 / 효과음 토글 스위치**(T28 연결 유지) · **언어 버튼**(«한국어» 표시만) · 패널 아래 개인정보 처리방침 · 이용약관 링크 글자(눌러도 아무 일 없음) · 그 아래 T29 의 **«데이터 삭제» 빨간 작은 버튼** · «탭하여 닫기».
2. 게이트 + PROGRESS T41 행.

### T42 — 펫 탭 = `13_pet.jpg` 구도 + 펫 세부 = `14_pet_detail.jpg` (껍데기 · T32 대체) ✅ (완료 · 코드 `66818af` · **비평 13 = 9.0 · 14 = 10.0 ✅(회차 1 · CI #83)** · 회차 2 감점 제거 `d6f66eb` · CI #84 · PROGRESS 참조)
범위: `Assets/Scripts/Game/PetScreen.cs`(신규) · `Screens.cs`(NavBar 펫 탭 → PetScreen 한 줄) · catalog
순서: 제약 없음(Screens 한 줄 — rebase). T32(«Character_Skill 그대로»)는 **폐기** — Character_Skill·Character_Skill_Detail 프리팹은 부품으로만.
1. 상단 재화 바 → **4열 격자**(GUI Pro 아이콘 8~9개 · 칸 위 «Lv. N» · 아래 진행바 «n/m» · 2칸 «장착중») → 합계 줄(«+N ❤ | +N 🛡 | +N 🗡») → «장착중» 띠 + 슬롯 4(잠금 2 · 장착 2) → 전체 강화 · 빠른 장착(회색) → 소환 💎 · 소환 x10 💎(주황) → 탭 바. **전부 표시만**(누르면 아무 일 없음 · 데이터 없음 → 레퍼런스 숫자를 그대로 두지 말고 0/잠금 표시).
2. 칸을 누르면 세부 팝업(칸 + 진행바 · 설명 박스 · «패시브:» · 강화(회색) · 장착(주황) · 탭하여 닫기).
3. 게이트 + 스모크(탭 5 · 격자 존재) + PROGRESS T42 행.

### T43 — 던전 탭 = 던전 `20`·`21` + 아레나 `22`~`26` 껍데기 (T30 대체) · **✅ 조건 = 비평 ≥ 8.0/10(§5)** ✅ (완료 · 코드 `6acbcdb` · CI #70 · **비평 회차 1 = 7장 8.9~10.0**(screens CI #83 · 워커 G) · 회차 2 코드 `bf7f5d8`(초상 위치 · ◀ 색 · 카드 클립 · 확인 = 그 CI) · PROGRESS 참조)
범위: `Assets/Scripts/Game/EventsScreen.cs`(신규) · `Screens.cs`(NavBar «탤런트» → «던전» 탭 · 로비 오른쪽 아래 «이벤트» 버튼도 여기로) · catalog
순서: 제약 없음(Screens NavBar 줄 — T22 뒤). T30(«World_Dungeon_List 그대로»)은 **폐기** — 탭 이름 변경(탤런트 → 던전)만 여기서 이어받고, World_Dungeon_* 프리팹은 부품으로만.
1. 던전 페이지(제목 «던전» · 큰 카드 2(제목 띠 + 티켓 · 그림 · «획득 가능» 보상 아이콘 · 입장(주황 · !)) · «준비 중» 카드 · 하단 던전/아레나 2탭 + 뒤로) · 던전 세부 팝업(그림 띠 · ◀ 층수 ▶ · 보상 4칸 · 소탕(파랑) · 도전(주황)) · 아레나 페이지(경기장 카드 · 시즌 타이머 · 티어 · 입장) · 입장 화면(시상대 1·2·3 · 오른쪽 위 보상·상인 · 순위 목록 · 바닥 도전 🎫x1) · 도전 팝업(상대 5줄 · 무료 새로고침) · 순위 보상 팝업(티어 띠 · 1~4위 줄 · 일일/시즌 탭) · 상인 페이지(배너 · 3열 상품 격자 · 한도 · 코인 가격). **전부 표시만**(버튼은 눌러도 아무 일 없음 · 뒤로/탭하여 닫기만 동작).
2. 게이트 + 스모크(페이지·팝업 전부 열고 빨간 줄 0) + PROGRESS T43 행.

### T44 — 로비 사이드 팝업 껍데기 6종 = `11`·`15`·`16`·`17`·`18`·`19` (T34 뒤) · **✅ 조건 = 비평 ≥ 8.0/10(§5)** ✅ (완료 · 코드 `423cd98` · CI #73 · 워커 F · 표 ⑲~㉔ 신설 · `LobbyPopups.cs` · **비평 회차 1 = 11·15·17·18·19 10.0 · 16 9.5**(screens CI #83 · 워커 E) · PROGRESS 참조)
범위: `Assets/Scripts/Game/LobbyPopups.cs`(신규) · `Screens.cs`(T34 의 `OnSide` 훅 연결 한 줄씩) · catalog
1. 특권(11 · 페이지 · 카드 세로 나열 · «전체 받기» 바닥 바) · 퀘스트(15 · 파란 명판 · 점수 트랙 · 줄 목록 · 일일/주간/업적 탭) · 출석(16 · 노란 리본 · 3×2 + 7일 칸) · 데일리 기프트(17 · 선물 그림 · 세로 타임라인 · 광고 N회 줄 4) · 7일 챌린지(18 · 빨간 리본 · 배너 · 점수 트랙 · Days 1~7 세로 탭 + 과제 줄) · 패스(19 · 시즌 배너 · 3열 세로 트랙 · 바닥 3버튼). 각각 레퍼런스 구도 그대로, 글자는 한국어, **버튼은 눌려도 아무 일 없음 · 배경 탭으로 닫힘**.
2. 게이트 + 스모크(6개 열고 빨간 줄 0) + PROGRESS T44 행.

### T45 — CI #51 빨강(T23 코드 커밋 `9ea5d9d`) 후속: PlayMode `MapThemeTests` 사막 물결 경계 정렬 1건 — T19 회귀 ✅ (완료 · `11a737d` · CI #53 유니티 잡 초록 · PROGRESS 참조)
범위: `Assets/Scripts/Game/BattleWorld.cs`(`BuildProps` 의 `flat` 판정 1줄) · 테스트 불변(`Assets/Tests/PlayMode/MapThemeTests.cs` 는 손대지 않는다)
순서: 제약 없음.
1. 원인(CI #51 · https://github.com/kuzuni/aaawunity/actions/runs/34017948738 · PlayMode 21건 중 `MapThemeTests.AllFourThemesMatchDemoSceneComposition` 1건 실패 · 나머지 20 + EditMode 78 Passed): 메시지 «챕터 4 (desert) 물결 경계는 길 바로 위(납작 · 데모 렌더 순서) Expected: -16 But was: 389». `BuildProps` 가 물결 경계를 «납작(스프라이트 높이 × Sy < 0.35u)» 으로 골라 -16 을 주는데, `Road_up_Desert.png` 만 **43px = 0.43u**(Autumn 34 · DeepForest 33 · Forest 33px) 라 문턱을 넘어 일반 소품 규칙(y 로 381+…)로 떨어진다. T19 커밋 `dedeffb` 의 CI #48·#50 이 뒤 push 로 취소돼 그 세션이 못 본 회귀.
2. 수정: `flat` 판정에 **키가 `.roadUp` 이면 무조건 납작**을 더한다(물결 경계는 늘 길 바로 위 = 데모 렌더 순서 · 문턱 수치는 그대로). 에셋·테스트 불변.
3. 게이트 + PROGRESS T45 행 + 확인 수단 = 이 코드 커밋의 CI 유니티 잡(PlayMode `MapThemeTests` 4테마 Passed) — 같은 런이 T28·T23 의 확인 수단이 된다.

### T46 — UI 비평 하니스: 전 화면 스크린샷 + layout.json → `screens` 브랜치 + `tools/ui_score.py` (최우선 · 제약 없음) ✅ (완료 · 코드 `d47a55f`(+`1442ce7`) · **첫 `screens` 배포 = CI #83**(26 PNG + layout.json + meta.json) · 회차 1 실사용 22 화면 · 촬영 결함은 T58 · 종결 = 워커 E lock 인계 · PROGRESS T46 진행 기록)
범위: `Assets/Tests/PlayMode/UiShotsTests.cs`(신규) · `Assets/Tests/PlayMode/PlayShot.cs`(신규 · PerkStripTests 의 `SaveScreens` 를 옮겨 공용화) · `Assets/Scripts/Game/UiKit.cs`(`UiKit.Tag(go, "이름")` 한 함수 · 판정 요소에 이름표) · `.github/workflows/ci.yml`(unity-test 잡 끝에 «screens 브랜치 배포» 1단계) · `tools/ui_score.py`(신규) · `docs/ref-layout.md`(⑧~ 새 화면 표 자리)
순서: 제약 없음 — **T36~T44 보다 먼저 잡는다**(이게 없으면 그 작업들이 ✅ 를 못 단다). 다른 UI 워커와 겹치는 파일은 UiKit 한 함수뿐(rebase).
1. **PNG**: `UiShotsTests` 가 UiSmokeTests 와 같은 순서로 모든 화면·팝업(로비 · 전투 HUD(적 조우 상태 포함) · 레벨업 3택 · 보유 특전 · 장비 · 장비 세부 · 대장간 · 상점 · 설정 · 펫 · 펫 세부 · 던전 · 던전 세부 · 아레나 5종 · 사이드 팝업 6종 — 아직 없는 화면은 건너뛰고 «없음» 으로 기록)을 열어 `PlayShot.Save("lobby")` 처럼 **540×1170 PNG** 를 `ui-screens/<이름>.png` 에 남긴다(PerkStripTests 의 RenderTexture 방식 그대로 · 배치 모드에서도 됨). 파일 이름 = `docs/ref/` 번호와 같게(`01_lobby.png` · `02_battle.png` …).
2. **layout.json**: 같은 테스트가 화면마다 `UiKit.Tag` 가 붙은 요소의 **프레임 % 사각형(x·y·w·h · 좌상단 0 · 우하단 100)** 을 `ui-screens/layout.json` 에 `{화면: {이름: [x,y,w,h]}}` 로 쓴다. 이름은 `docs/ref-layout.md` 표의 «요소» 열과 **글자까지 같게**(예: «아바타(정사각)» · «START 버튼» · «챕터 카드(스테이지 그림)»). 화면 작업자는 자기 화면의 판정 요소에 Tag 를 단다(T46 은 로비·전투·장비·상점의 기존 요소에 먼저 단다).
3. **CI → `screens` 브랜치**: unity-test 잡 끝에 `peaceiris/actions-gh-pages@v4`(publish_dir `ui-screens` · publish_branch `screens` · force_orphan · main push 때만 · `if: always()` 아님 — 테스트가 초록일 때만) 로 올린다. `meta.json` 에 커밋 sha·CI 런 번호. **PNG 커밋 금지 규칙의 예외 = 이 브랜치뿐**(main 에는 절대 안 넣는다). 워커는 `git fetch origin screens && git show origin/screens:01_lobby.png > /tmp/01_lobby.png` 로 받아 `Read` 로 본다(프록시가 Actions 아티팩트·blob 은 막지만 github.com git 은 열려 있다 — T16 세션 로그 확인).
4. **`tools/ui_score.py <화면> [layout.json]`**: `docs/ref-layout.md` 의 해당 표(①~⑦ · 새 화면은 ⑧~ 로 워커가 추가)와 layout.json 을 대조해 **표 점수** 를 낸다 — 행마다 x·y·w·h 네 값이 전부 ±3%p 안이면 1점, 하나라도 3~6%p 면 0.5점, 그 밖(6%p 초과 · 요소 없음)은 0점 · `표 점수 = 10 × 합 ÷ 행 수`(소수 1자리). «(참고·컨테이너)» 행은 세지 않는다. 출력 = 행별 «ref / 게임 / 차 / 판정» 표(마크다운 · PROGRESS 에 그대로 붙인다).
5. **비평(사람 눈 몫 · 워커가 한다)**: PNG 와 `docs/ref/NN.jpg` 를 나란히 `Read` 로 보고 표가 못 잡는 것 — 겹침 · 잘림 · 순서 뒤바뀜 · 빠진 요소 · 비례가 눈에 띄게 다른 덩어리 — 를 **최대 −2.0 까지 감점**(항목당 −0.5 · 이유 한 줄씩). **아이콘·그림·색·폰트·글자체는 감점 금지**(주인 지시). `최종 = 표 점수 − 감점`. **8.0 이상이어야 ✅**.
6. 게이트 + PROGRESS T46 행 + «UI 비평 점수판» 절 신설(§5 형식) + `docs/ref-layout.md` 머리에 «채점 = tools/ui_score.py» 한 줄.

### T47 — 로비(T34) · 전투 HUD(T35) 비평 회차 (T46 뒤) — **🔄 회차 1 끝(워커 E · 01 = 9.7 ✅ · 03 = 8.6 ✅ 캔버스 · 02 = 촬영 오염) → 회차 2 채점 끝(CI #88 · 01 9.3 · 02/03 8.9 ✅) → 회차 3 코드 push `b8658b6`(워커 F · 챕터 제목 글자 크기 · 적 바 폭 · 표 ①② 챕터 제목 x/w «—») · 남은 일 = 그 CI 의 screens 로 01·02·03 재채점 후 ✅(F 가 11:55 UTC 쯤 깨어나 · 안 깨어나면 다음 워커 · PROGRESS T47 회차 2 진행 기록)**
범위: T34·T35 의 파일(Screens.cs Lobby · BattleScreen/BattleWorld HUD) · `docs/ref-layout.md` ①·② 표(틀린 행이 있으면 정정 + 회차 로그)
순서: **T46 완료 뒤**(screens 브랜치에 첫 PNG 가 올라온 뒤).
1. §5 대로 로비·전투 두 화면을 채점한다(표 + 눈). 8.0 미만이면 고치고 push → 다음 CI → 다시 채점. 8.0 이 될 때까지 회차를 잇는다(회차마다 점수판에 한 줄).
2. 8.0 이상이면 PROGRESS T34·T35 행에 «비평 N.N ✅(회차 k)» 를 붙이고 T47 을 ✅.
3. **회차 2(코드 3건 · 2026-09-06 워커 E 등재 · 결정 102)**: ⓐ `Assets/Tests/PlayMode/UiShotsTests.cs:125` — `Shot("02_battle")` 앞에 `if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; yield return Frames(1); }`(3초 안에 레벨업 팝업이 떠 02 가 특전 카드 화면으로 찍힘 · 03 루프와 같은 처리) ⓑ 월드 8행(② 표의 발밑 y·적 행 y·플레이어/적 높이·체력 라벨 줄·중심 x·바 폭 2)을 `BattleWorld` 에서 재서 `PlayShot.Layout` 결과 사전에 표와 같은 이름으로 넣는다(결정 51 · 하니스는 캔버스만 잰다) ⓒ «챕터 제목» 이름표를 조각 루트가 아니라 글자(Text) rect 에(`Screens.cs:77` 로비 · `BattleScreen.cs:111` 전투 — 조각은 표 ±6/12 여유로 세워 w 가 +6~8 크다) → 예상 로비 10.0 · 전투 캔버스 13/14. 그 뒤 CI → screens → `ui_score.py 02_battle`·`03_battle_enemy` 로 22행 기준 재채점 + T58 이 고쳐졌으면 눈 비평(발밑 2단 바 · 캐릭터 크기).

### T48 — CI #66 빨강(`48d05a6` · T39 코드 push) 후속: PlayMode 4건(T39 대장간 2 · T40 상점 2) — main 빨강 = gh-pages·screens 브랜치 안 생김 (최우선 · 제약 없음) ✅ (완료 · 대장간 = B `07c044d` + A `b13ac22`(ⓓ) · 상점 = D `d2df257` + ⓔ `353527e` · 확인 CI #75 · PROGRESS 참조)
범위: `Assets/Scripts/Game/ForgeScreen.cs` · `ShopScreen.cs` · `Screens.cs`(NavBar) · 테스트는 «구도 단언이 새 구도와 어긋난 것» 만 고친다(규칙을 지우지 않는다)
순서: 제약 없음 — T39·T40 을 만든 워커가 있으면 그가, 없으면 다음 워커가 잡는다.
1. 로그(https://github.com/kuzuni/aaawunity/actions/runs/34021396912 · PlayMode 25 중 5 실패 · 5번째 장비 세부 `ui.title.blue` 는 T38 이 `bb51e3e` 로 고침): ⓐ `UiSmokeTests.ForgeShowsAllAndFuses` «뒤로 = 왼쪽 아래 Expected: greater than 0.9 But was: 0.015»(UiSmokeTests.cs:398 — 뒤로 버튼 anchor 를 보는 단언 · T39 가 뒤로를 «아래 회색 띠 + ◀» 로 바꿈) ⓑ `ForgeEquippedFuseTests.EquippedGearIsAMaterialAndTheProductTakesItsSlot` «장착중 배지(Check)는 유지 Expected: True But was: False»(ForgeEquippedFuseTests.cs:93 — T39 가 «장착중» 글자로 바꿔 Check 배지가 꺼짐 → 테스트를 «장착중 표기(Check 또는 글자)» 로) ⓒ `UiSmokeTests.ShopBoxesAndChestOpenPopup` «상자 카드 3 Expected: 3 But was: 0»(UiSmokeTests.cs:437 — T40 재조립 뒤 이름/개수 계약) ⓓ `PressFeedbackTests.EveryButtonOnEveryScreenHasPressFeedback` «[shop] 탭 0 에 Button Expected: not null»(PressFeedbackTests.cs:198 — 상점 화면 탭 바에 Clickable 이 안 붙음).
2. 게이트 + PROGRESS T48 행 + 확인 수단 = 코드 커밋의 CI 유니티 잡(PlayMode 전부 Passed).

### 신규 작업 등재
- 버그·후속 작업 발견 시 PROGRESS 표에 **이미 쓰인 번호 중 가장 큰 것 +1** 로 등재 (번호 재사용 금지, 한 번호 = 한 작업).

### T49 — 팝업 등장 연출 = DOTween «순서대로»: 레벨업 3택 특전 카드 · 승리(클리어) 팝업 · 패배(사망) 팝업 (주인 2026-09-06 · T36·T23 코드 뒤 · 제약 없음) ✅ (완료 · `fdb8d35` · CI #76 · 3택 0.77s · 클리어 0.94s · 사망 0.98s · 배경 탭 = 스킵 · PROGRESS 참조)
범위: `Assets/Scripts/Game/Overlay.cs`(LevelUp · PerkBook · Clear · Dead) · `UiKit.cs`(연출 헬퍼 — 기존 `PopIn(rt, from, dur)`·`FadeIn` 에 <b>delay</b> 인자 또는 `Stagger(items, step)` 추가 · 타이밍 상수는 UiKit 한 곳) · `Assets/Tests/PlayMode/UiSmokeTests.cs`(연출 뒤 단언) · `PlayShot`(T46 스크린샷은 연출이 끝난 뒤)
순서: 제약 없음 — T36(3택)·T23(클리어)·T41(설정) 코드는 끝났다. Overlay 를 만지는 열린 작업 = T44(LobbyPopups 신규 파일 · Screens 훅뿐) · T47(전투 HUD) — 파일이 겹치지 않는다. 다른 워커가 Overlay 를 만졌으면 rebase.
주인 원문(2026-09-06 · 08:1X UTC): «특전 뜰 때 순서대로 dotween 으로 뜨게 애니메이션 · 졌을 때 팝업도, 이겼을 때 팝업도 그런 식 연출로».
1. **레벨업 3택**(`Overlay.LevelUp`): 지금은 카드 3장이 `UiKit.PopIn` 으로 <b>동시에</b> 뜬다 → 배경 페이드 → 리본 «레벨 업!» 팝 → 부제 → <b>카드 3장이 위에서 아래로 하나씩</b>(간격 0.10~0.15s · 스케일 0.85→1 + 알파 0→1 · `Ease.OutBack`) → 마지막에 «새로고침 무료»·«남은 횟수»·📘. 연출 중 카드 클릭은 막았다가 마지막 카드가 다 뜨면 연다(또는 탭 = 스킵 → 즉시 전부 표시) — 워커가 정하고 «워커 결정 기록» 한 줄. «새로고침» 으로 다시 굴릴 때도 같은 연출.
2. **보유 특전**(`PerkBook`): 같은 카드 형식이므로 같은 stagger — 첫 화면에 보이는 카드(뷰포트 안)만 순서대로, 스크롤 밖은 즉시 표시.
3. **승리(클리어)**(`Overlay.Clear` · Play_Result_Win_01): 배경 → 제목 «클리어!» 팝(지금의 Title PopIn 유지) → «챕터 N»·해금 문구 → 보상 칸(골드 · 숫자는 0→G.Gold 카운트업 = `DOVirtual.Int`/`DOTween.To` · 선택) → 버튼 2개(광고 ×2 · 그냥 받기)가 <b>순서대로</b> 아래에서 떠오르며 페이드. 컨페티(SampleEffect_Confetti)는 지금처럼 숨김(켜고 싶으면 워커 결정 기록).
4. **패배(사망)**(`Overlay.Dead` · Play_Result_Lose): 배경 → «쓰러졌다...» 제목 → 보상(골드) → 팁 3줄이 한 줄씩 순서대로 → «로비로» 버튼 · «터치하면 로비로» 마지막. 배경 탭 = 연출 중이면 스킵(즉시 전부 표시), 끝난 뒤면 닫기.
5. **공통 규칙**: 팝업 중 시간 정지이므로 모든 트윈 `SetUpdate(true)`(unscaled · 기존 PopIn/FadeIn 과 같게). `Overlay.Close()`/`UiKit.Clear` 에서 남은 트윈을 `DOKill` — 파괴된 오브젝트를 겨냥한 트윈 경고·MissingReference 는 콘솔 빨간/노란 줄(§1 «플레이 콘솔 에러 0»). 총 길이 = 3택 ≤ 0.8s · 승/패 ≤ 1.0s(길면 답답). 연출 타이밍 상수는 밸런스 수치가 아니므로 UiKit 상수로 두되 «워커 결정 기록» 에 한 줄. 새 에셋·코드 도형 0(기존 조각을 움직이기만).
6. **테스트**: PlayMode `UiSmokeTests` — 팝업 연 직후에도 요소는 <b>존재</b>(개수·이름 단언은 그대로 · 알파/스케일 단언은 `DOTween.CompleteAll()` 뒤) · `LogAssert.NoUnexpectedReceived` · Close 뒤 해당 오브젝트를 겨냥한 트윈 0. `PlayShot`(T46) 은 `DOTween.CompleteAll()` 뒤에 찍어 비평 PNG 가 연출 중간을 찍지 않게.
7. 게이트 + PROGRESS T49 행 + 완료 기록(«무엇으로 확인했는가» = CI 유니티 잡 PlayMode 전부 Passed).

### T50 — 킬 뒤 이동 = «공격 모션 끝 → 걷기 모션 → 원래 걷기 속도(132·walkMul)로 다음 적» · 2배 따라잡기 폐지 (주인 2026-09-06 · T20 연출 수정 · 화면만 · 엔진 불변) ✅ (완료 · `07873b4` · 워커 H · 격차 대신 엔진 틱 보류(`HoldEngine` · 결정 107) · 확인 = 그 커밋의 CI PlayMode `BattleWorldTests` · PROGRESS 참조)
범위: `Assets/Scripts/Game/BattleWorld.cs`(`Sync` 의 표시 원점 `_shownPX` · `CatchUpMul`·`KillPending`·`_moving` · 플레이어 공격 모션 상태) · `CharacterRig.cs`(공격 중 여부 노출이 모자라면) · `Assets/Tests/PlayMode/BattleWorldTests.cs`(T20 테스트 갱신)
순서: 제약 없음 — T47(전투 HUD 비평)이 BattleScreen/BattleWorld 를 만질 수 있으니 rebase. **엔진(`Core/Battle.cs` 671행 · `P.WorldX += PlayerSpeed*WalkMul*(Dash?DashMul:1)*dt`)은 손대지 않는다** — sim.js 와 1:1 이고 시드 골든이 걸려 있다.
주인 원문(2026-09-06 · 09:0X UTC): «킬하고 나서 공격 모션 끝나고 나서 걸어가는 모션 나오면서 원래 걷기 속도로 다음 적 가야 함. 특전 부분은 그대로.»
지금(T20): 킬 뒤 «칼이 내려올 때까지» 표시 원점을 멈춘 뒤 **걷기 2배**(`CatchUpMul = 2`)로 엔진 x 를 따라잡는다 → 화면에 «잠깐 멈춤 → 2배 빠른 걸음 → 평소 걸음» 이 보인다. 주인은 이게 싫다.
1. **순서**: 킬 타격(칼 내려옴 · 적 사망 연출 시작) → 플레이어 **공격 모션이 끝날 때까지** 표시 원점 정지(지금은 `KillPending` 만 보고 멈추는데, 공격 애니가 남아 있어도 위치가 먼저 움직인다 → `_player.Attacking` 도 정지 조건에 넣는다) → 공격 모션이 끝나면 **걷기 모션과 함께** 출발.
2. **속도**: 출발 뒤 표시 원점은 **`PlayerSpeed × WalkMul × (Dash ? DashMul : 1)` 그대로**(= 132px/s · 대시 특전이면 660) — `CatchUpMul` 을 1 로(또는 상수 삭제). 엔진은 킬 다음 틱에 이미 출발했으므로 표시가 엔진보다 «멈춘 시간 × 132» 만큼 뒤에 있게 되는데, **엔진이 다음 적 앞(74px)에서 멈추는 동안 같은 속도로 자연히 따라잡는다**(엔진이 서 있으면 격차는 줄기만 한다). 빠르게 걷는 구간은 없어야 한다.
3. **격차 안전장치**: 격차가 계속 쌓이는 경우(공속이 높아 한 방에 죽여 엔진이 거의 안 서는 구간)에만 `SnapGap`(600) 보다 훨씬 작은 문턱(예: 한 적 간격의 절반 ~ 150px)에서 조용히 맞춘다(눈에 띄는 가속 대신 한 번에 · 또는 문턱 이하로 유지되면 아무것도 안 함). 문턱 값·방식은 워커가 정하고 «워커 결정 기록» 한 줄.
4. **타격 연출**: 표시 플레이어가 아직 도착 전인데 엔진이 다음 적을 쳤으면(격차 구간) 그 타격 연출(칼 모션·데미지 팝)은 **표시 플레이어가 사거리에 들어온 뒤**로 미룬다(지금 Strike 큐의 `At` 에 «도착 시각» 을 더하는 정도). 멀리서 허공을 치는 모습 금지. 표시 HP(ShownHp)는 지금 규칙(칼이 내려온 뒤) 그대로.
5. **특전 «처치 시 대시»(p_killDash · ×5)** — ⚠ 주인 정정(09:1X · T51): 대시도 **공격 모션이 끝난 뒤** 출발하고, 그다음 ×5(660px/s) 로 걷는다. «멈춤 없이 바로 출발» 은 취소. T50 워커가 이 줄을 봤으면 여기서 같이 처리하고 T51 ① 을 ✅ 로 적는다(T51 ② 사망 이펙트 제거는 별도).
6. **테스트**(PlayMode `BattleWorldTests` · T20 것 갱신): ⓐ 킬 뒤 표시 원점의 프레임당 이동량 ≤ `PlayerSpeed×WalkMul×dt`(대시 아닐 때 2배 구간 0) ⓑ 공격 모션 중(`Attacking`)에는 표시 원점 이동 0 ⓒ 다음 적 앞에서 격차가 0 으로 수렴(엔진 정지 중) ⓓ `LogAssert.NoUnexpectedReceived`. 시드 골든(EditMode)은 건드릴 이유가 없다 — 바뀌면 엔진을 건드린 것이므로 되돌린다.
7. 게이트 + PROGRESS T50 행 + 완료 기록(«무엇으로 확인했는가» = CI 유니티 잡 PlayMode 전부 Passed · 가능하면 T46 하니스 전투 PNG 는 참고만).

### T51 — ① 특전 «처치 시 대시»도 «공격 모션 끝 → 그다음 ×5 로 걷기» · ② 적 사망 «펑» 이펙트(fx.death Magic Poof) 제거 (주인 2026-09-06 · T50 뒤 · 같은 파일) ✅ (완료 · `07873b4` · T50 과 같은 커밋 · 워커 H · PROGRESS 참조)
범위: `Assets/Scripts/Game/BattleWorld.cs`(`Sync` 표시 원점 — T50 이 만든 «공격 모션 끝까지 정지» 규칙에 대시도 포함 · 454행 `Fx.Spawn("fx.death", …)` 제거) · `Assets/Tests/PlayMode/BattleWorldTests.cs` · `docs/assets-map.md`(fx.death 행 «미사용» 표기 · catalog 키는 남겨도 된다)
순서: **T50 뒤**(같은 `Sync` 코드 · T50 lock 이 풀리고 PROGRESS T50 이 ✅ 된 뒤) — T50 워커가 아직 작업 중이면 그 워커가 이어서 잡아도 된다.
주인 원문(2026-09-06 · 09:1X UTC): «특전 부분도 생각해 보니까 킬하고 나서 공격 모션 끝나고 5배로 걷는 속도 되어야 하는 거임» · «죽을 때 펑 하고 터지는 이펙트 없애기».
1. **대시 정정**(T50 5항 «대시 중에는 멈춤 없이 바로 출발» 을 뒤집는다): 대시 특전이 있어도 킬 뒤 **플레이어 공격 모션이 끝날 때까지 표시 원점 정지** → 끝나면 걷기 모션과 함께 **×DashMul(5) = 660px/s** 로 출발. 엔진(`Battle.cs` 671행 · `P.Dash`)은 그대로 — 엔진은 즉시 대시하므로 격차가 잠깐 생기지만 표시가 5배로 곧 따라잡는다(격차 안전장치는 T50 것 공용). 대시가 아닐 때는 T50 그대로 132.
2. **사망 이펙트 제거**: 적이 죽을 때 `Fx.Spawn("fx.death", …)`(CFXR Magic Poof · «펑») 를 부르지 않는다. 사망 모션(`CharacterRig.Dead`) + 알파 페이드(DieT · 0.85s) + `snd.kill` 효과음은 그대로. 플레이어 사망도 같은 이펙트를 쓰면 같이 뺀다. 다른 fx(피격·치명·회피·레벨업)는 손대지 않는다.
3. **테스트**(PlayMode `BattleWorldTests`): ⓐ 대시 특전 상태에서 킬 뒤 공격 모션 중 표시 이동 0 · 끝난 뒤 프레임당 이동 ≈ `PlayerSpeed×WalkMul×DashMul×dt` ⓑ 적 사망 시 fx.death 인스턴스 0(Fx 스폰 카운트 또는 이름으로 `FindObjectsOfType`) ⓒ `LogAssert.NoUnexpectedReceived`.
4. 게이트 + PROGRESS T51 행 + 완료 기록(확인 수단 = CI 유니티 잡 PlayMode 전부 Passed).

### T52 — 특전 설명 글자 한 색(수치 연두색 강조 제거) (주인 2026-09-06 · 제약 없음 · T49 와 같은 Overlay 파일 → rebase) ✅ (완료 · `8f78d2a` · PROGRESS 참조 · «남은 횟수 N» 주황은 유지 = 결정 88)
범위: `Assets/Scripts/Game/Overlay.cs`(`GreenNumbers` 63~70행 · `PerkCard` 106행 `UiKit.SetText(rt, "Text_Value", GreenNumbers(p.Desc), Palette.Ink, 34)`) · `Assets/Tests/PlayMode/UiSmokeTests.cs`(582행 GreenNumbers 단언) · `docs/ref/README.md`(04 항목 «수치 초록» 문구에 «주인 취소 2026-09-06» 표기) · 특전 설명이 쓰이는 다른 자리(악마 거래·천사·보유 특전·전투 PerkStrip 툴팁 등 `<color` 를 넣는 곳 전부)
주인 원문(2026-09-06 · 09:2X UTC): «특전들 글씨가 색깔 다르게 하는 거 하지 말기 · 연두색 섞여 있는데 존나 안 읽힌다». T36 1항 «수치 초록»(레퍼런스 04) 은 이 지시로 **취소**.
1. 특전 카드 설명(`Text_Value`)은 **한 색**(`Palette.Ink`) 으로 — `GreenNumbers` 호출을 없애고 `p.Desc` 를 그대로 넣는다. 함수는 지우거나 남기되 호출 0(남기면 «미사용 · 주인 취소» 주석).
2. 같은 규칙을 **모든 특전 글자**에: 레벨업 3택 · 보유 특전(PerkBook) · 악마의 거래 · 천사 · 쉼터 · 전투 하단 PerkStrip 관련 글자에 리치 텍스트 `<color>` 로 부분 색을 넣는 곳이 있으면 전부 한 색으로. 등급 리본 글자(«일반/희귀/…» · 흰색)와 카드 프레임 등급색은 글자 색이 아니므로 그대로. 「남은 횟수 : N」 의 N 강조(171행) 도 같은 연두색이면 같이 뺀다(워커 판단 · 결정 기록 한 줄).
3. 가독성: 설명 글자 크기(34)·굵기는 그대로 두되, 한 색으로 바꾼 뒤 흐린 회색이면 `Palette.Ink`(진한 색) 로 통일.
4. 테스트: `UiSmokeTests` 582행 단언을 «설명 텍스트에 `<color` 없음 · 원문 `p.Desc` 그대로» 로 바꾼다 · `LogAssert.NoUnexpectedReceived`.
5. 게이트 + PROGRESS T52 행 + 완료 기록(확인 수단 = CI PlayMode + screens 브랜치 04/05 PNG 를 `Read` 로 한 번 보고 «글자 한 색» 확인).

### T53 — 특전 설명 표기 = «트리거: 내용» (예: «처치 시: 33% 확률로 …» · «피격 시: …» · «3타마다: …») (주인 2026-09-06 · T52 뒤 · 같은 Overlay 파일) ✅ (완료 · `574ae4f` + `6e2d9cc` · Core/PerkText · 트리거 76 · 패시브 24 · PROGRESS 참조)
범위: `Assets/Scripts/Game/Overlay.cs`(`PerkCard` 의 설명 문자열 가공 · T52 가 만든 «한 색» 자리) 또는 `UiKit`/새 정적 헬퍼 `PerkText.Format(desc)` · 이 레포 전용 JSON 이 필요하면 `Assets/KkomaKnight/perkText.json`(shop.json 방식) · `Assets/Tests/EditMode`(순수 문자열 변환 테스트 · 100개 전수) · `Assets/Tests/PlayMode/UiSmokeTests`
순서: **T52 뒤**(같은 `PerkCard` 설명 줄) — T52 워커가 이어서 잡아도 된다.
주인 원문(2026-09-06 · 09:2X UTC): «처치시: 33퍼 확률로 어쩌구저쩌구 / 피격시: 33퍼 확률로 어쩌구저쩌구 / 3타마다: 어쩌구저쩌구 이런 식으로 표기하기».
⚠ 설명 원문은 `Assets/StreamingAssets/data/perks.json` 의 `desc`(aaaw 정본 · 100개) — **JSON 은 손대지 않는다**(§1). 표기는 **표시 시점에 코드로** 바꾼다(엔진·데이터 불변).
1. 변환 규칙(`desc` 앞머리의 트리거 구를 떼어 «트리거: 나머지» 로): «처치 시 X» → «처치 시: X» · «피격 시 X» → «피격 시: X» · «공격 시 X» → «공격 시: X» · «반격 시 X» → «반격 시: X» · «N타마다 X» → «N타마다: X» · «실드가 있으면 피격 시 X» → «피격 시(실드 있을 때): X»(또는 «실드 있을 때 피격 시: X» · 워커 결정) · «실드가 0 인 동안 X» → «실드 0 일 때: X» · «보유 특전 하나당 X» → «특전 하나당: X». 트리거가 없는 상시 능력치(«공격력 +30%» · «방어력 +8%» · «가시갑옷 +100%» · «다음 특전부터 최소 희귀 이상» 등)는 **«패시브: X»**(주인 정정 09:3X «상시 같은 거는 패시브: 이렇게») — 즉 100개 전부 «무언가: » 접두어가 붙는다.
2. 확률 표기는 원문 그대로 «33% 확률로»(주인 원문의 «33퍼» 는 말투) · 콜론 뒤 한 칸 띄움 · 콜론 앞 트리거 구는 굵게 하지 않는다(T52 «한 색» · 굵기도 통일).
3. 구현은 정규식/접두어 표 한 곳(정적 헬퍼) — 100개 전수를 EditMode 테스트로 돌려 «트리거 구가 있는 71개는 그 트리거로, 없는 29개는 «패시브: » 로 — 100개 전부 콜론이 있다» 를 단언(표는 테스트 안에 기대값 목록으로 · 새 특전이 생기면 테스트가 잡는다).
4. 같은 표기를 특전 설명이 보이는 모든 자리에(3택 · 보유 특전 · 악마 거래 · 천사 · PerkStrip 툴팁 등 · T52 와 같은 목록).
5. 게이트 + PROGRESS T53 행 + 완료 기록(확인 = EditMode 전수 테스트 + CI PlayMode + screens 04/05 PNG 한 번 보기).

### T54 — CI #75(·#71) 빨강 후속: PlayMode `EventsScreenTests.DungeonArenaPagesAndPopups` «[상인 페이지] 영문 데모 글자: Text» 1건 (최우선 · 제약 없음 · 워커 A · lock 파일은 `T50.lock` · 코드 `faa0d30`) ✅ (완료 · CI #77·#82 초록 · lock 은 90분 경과로 워커 H 가 인계하며 종결 · 결정 108)
범위: `Assets/Scripts/Game/EventsScreen.cs`(상인 페이지 상품 카드의 CardFrame_04 `Text_Title`)
> 번호: A 가 09:09 에 `T50.lock` 으로 선점했는데 09:14 등재 세션이 같은 번호로 «킬 뒤 이동» 을 올렸다(그 뒤 T51~T53 이 그 번호 기준으로 이어짐) → 규약 «최대 +1» 대로 A 의 후속에 T54 를 준다(PROGRESS 워커 결정 87). A 가 끝나며 `T50.lock` 을 지우면 그때 T50 을 잡는다.
1. 원인(CI #75 로그 · PlayMode 26 중 1 실패 · EditMode 83/83): 상인 페이지 상품 카드가 CardFrame_04 원본의 `Text_Title`(«Text») 을 켜 둔 채 제목 Label 을 따로 얹어 데모 잔여 글자 검사에 걸림 → ShopScreen 상자 카드처럼 `Text_Title` 자리를 제목으로 쓴다(`faa0d30`).
2. 게이트 + PROGRESS T54 행 + 확인 수단 = `faa0d30` 이 포함된 CI 유니티 잡(PlayMode 전부 Passed → `screens`·gh-pages 첫 배포).

### T55 — CI #76·#77 빨강 후속(T49 코드 `fdb8d35` 회귀): PlayMode `UiSmokeTests.BattleTicksAndAllBattlePopups` «카드 수 = 제안 수 Expected: 3 But was: 6» 1건 — main 빨강 = `screens`·gh-pages 안 생김 (최우선 · 제약 없음) ✅ (완료 · `028133e` · UiKit.Clear 떼고 파괴 · 확인 = 그 커밋의 CI 런 · PROGRESS 참조)
범위: `Assets/Scripts/Game/UiKit.cs`(`Clear` 한 줄) · 테스트 불변(`UiSmokeTests.cs:570` 단언은 T49 의 계약 그대로)
1. 원인(CI #76 https://github.com/kuzuni/aaawunity/actions/runs/34023880052 · #77 https://github.com/kuzuni/aaawunity/actions/runs/34024048144 · PlayMode 26 중 1 · EditMode 83/83): T49 가 «카드 수 = 제안 수» 단언을 `Overlay.LevelUp()` **직후(같은 프레임)** 로 옮겼다(연출 중에도 요소가 존재해야 하므로). `LevelUp` 은 `UiKit.Clear(group)` 으로 프리팹(Play_Perk_Selection_02) 의 샘플 카드 3장을 지우고 3장을 새로 만드는데, `Clear` 가 `Destroy`(프레임 끝에 실제 제거)만 하므로 같은 프레임의 `childCount` 는 3 + 3 = 6. 예전 단언은 프레임을 넘긴 뒤라 3 이었다.
2. 수정: `UiKit.Clear` 가 자식을 **트리에서 먼저 떼고**(`SetParent(null, false)` · 비활성) 파괴한다 — T48 상점 껍데기와 같은 규칙(결정 80 · `Find`/`childCount` 가 같은 프레임에 옛 것을 보지 않게). 트윈 Kill 순서는 그대로(먼저).
3. 게이트 + PROGRESS T55 행 + 확인 수단 = 이 코드 커밋의 CI 유니티 잡(PlayMode 전부 Passed → `screens`·gh-pages 첫 배포).

### T56 — 플레이 콘솔 노란 줄 0: DOTween 세이프 모드 경고(파괴된 오브젝트를 겨냥한 트윈 · CI #77 유니티 로그 «safe mode captured 59 errors» = missing target 47 + startup 12) — 모든 트윈에 `SetLink(gameObject)` (§1 · T12 감사 ⓓ 부류 · 제약 없음) ✅ (코드 완료 · `262ca21` · 확인 = CI #82 로그의 «SAFE MODE captured N» = 0 · PROGRESS 참조)
범위: `Assets/Scripts/Game/UiKit.cs`(Clickable 눌림 punch · PopIn · FadeIn · Reveal — **`Clear` 는 T55 몫 · 손대지 않는다**) · `Overlay.cs`(마스터 시퀀스 · 골드 카운트업 · BossWarn 띠) · `BattleWorld.cs`(피격 punch · 데미지 팝) · 테스트 불변
순서: 제약 없음(T55 와 같은 UiKit 파일이지만 다른 줄 · 한 줄씩이라 rebase 로 풀린다 · T50/T51 의 `Sync` 와도 다른 줄).
1. 원인(CI #77 https://github.com/kuzuni/aaawunity/actions/runs/34024048144 유니티 로그 · 테스트별 경고 수: ForgeShowsAllAndFuses 26 · LobbySettingsTalentPetToast 8 · PlayerNeverWalks… 7 · DungeonArenaPagesAndPopups 6 · ForgeEquippedFuse 4 · 그 밖 8): 트윈 원점 = DOPunchScale 27(버튼 눌림 · `UiKit.Clickable`) · DOScale 11(`PopIn`/`Reveal`) · DOFade 11(`FadeIn`/`Reveal`/BossWarn) · DOAnchorPosY 5(데미지 팝) · DOPunchPosition 1(피격). 대상 오브젝트가 `UiKit.Clear`(T49 `KillTweens`) 가 아닌 경로 — 화면 루트·월드 루트·캔버스 `Destroy` · 인벤/셀 재구성 · 팝업 갈아끼움 — 로 파괴되면 DOTween 이 다음 갱신에서 «Target or field is missing/null»(노란 경고 · safeMode 가 조용히 kill), 시작 전에 파괴되면 «Tween startup failed» 를 찍는다. 에디터 플레이에서도 같은 경로(합성 뒤 인벤 재구성 · 전투 종료 · 팝업 갈아끼움)에서 노란 줄이 뜬다 — T49 완료 기록이 «파괴된 오브젝트를 만지는 트윈 경고 = §1 콘솔 줄» 이라 적은 그 부류.
2. 수정: 트윈을 만드는 모든 자리(8곳)에 `.SetLink(대상 gameObject)`(DOTween `LinkBehaviour.KillOnDestroy` 기본 · DLL 에 API 있음) — 오브젝트가 어떤 경로로 파괴돼도 DOTween 이 그 트윈을 먼저 죽여 경고가 안 난다. 기존 `KillTweens`(T49) 는 그대로(이중 안전). 에셋·연출 타이밍·테스트 불변. 새 트윈을 만드는 규칙: **`SetLink` 를 붙인다**(§1 에 한 줄).
3. 확인 수단: 코드 커밋의 CI 유니티 잡 로그에서 «DOTWEEN ► … SAFE MODE ► captured N errors» 줄이 사라지거나 N 이 0 (남으면 test-case 별 원점을 PROGRESS 에 적어 다음 회차) · PlayMode 전부 Passed. dotnet build 가 실제 `DOTween.dll` 을 참조하므로 API 존재는 컴파일이 보증.
4. 게이트 + PROGRESS T56 행 + 워커 결정 기록 한 줄.

### T57 — CI #82 빨강 후속(유니티 테스트는 전부 초록): «screens 브랜치용 meta.json» 단계 «ui-screens/meta.json: Permission denied» — 첫 `screens`·gh-pages 배포가 또 막힘 (최우선 · ci.yml 한 줄) ✅ (완료 · `d399e3f` · 워커 H · 확인 = CI #83 에서 screens 브랜치 생성 · PROGRESS 참조)
범위: `.github/workflows/ci.yml`(unity-test 잡 «screens 브랜치용 meta.json» 단계 1줄) · 코드·테스트 불변
순서: 제약 없음(T46 lock 이 살아 있어도 T46 의 파일 범위 중 ci.yml 의 이 한 단계만 · 결정 92).
1. 원인(CI #82 https://github.com/kuzuni/aaawunity/actions/runs/34025135763 · `262ca21` · 로그: «Test run completed. Exiting with code 0 (Ok)» 뒤 «/…/sh: line 2: ui-screens/meta.json: Permission denied» · exit 1): `game-ci/unity-test-runner` 는 docker 컨테이너(root)로 돌고 `UiShotsTests`/`PlayShot` 이 `/github/workspace/ui-screens/` 에 PNG·layout.json 을 쓴다 → 워크스페이스에 **root 소유 폴더**가 남는다. 다음 단계는 러너 사용자(runner)의 셸이라 그 폴더 안에 `meta.json` 을 못 만든다. #76 이전 런은 테스트가 빨개서 이 단계까지 오지 않아(`if: success()`) 처음 드러났다.
2. 수정: `printf … > ui-screens/meta.json` 앞에 `sudo chown -R "$(id -u):$(id -g)" ui-screens`(GitHub 호스트 러너는 sudo 무비밀번호). peaceiris 배포는 그 폴더를 그대로 읽는다.
3. 확인 수단 = 코드 커밋의 CI 런: unity-test 잡 초록 + «screens 브랜치로 배포» 단계 초록 → `git fetch origin screens` 가 된다(첫 PNG·layout.json·meta.json). 그러면 T47·T36~T44 비평 회차 시작.

### T58 — UI 비평 하니스 PNG 촬영 결함: UI 프레임이 PNG 가운데 188×404(34.8%) 띠로만 · 월드 스프라이트가 UI 위에 겹침 (첫 `screens` 배포 CI #83 · 26장 전부 · T46 뒤 · 같은 PlayShot.cs) ✅ (완료 · 코드 `0a036a3` · 원인 = CopyFrom 이 WorldCam letterbox rect 복사 · 확인 = CI #87 로그 fill 0.999 + CI #88 screens run 88 PNG 전체 화면 · PROGRESS T58 진행 기록)
범위: `Assets/Tests/PlayMode/PlayShot.cs`(`Save`) · (필요 시) `UiShotsTests.cs` 단언 1개 · 코드(게임)·layout.json 계약 불변
순서: **T46 뒤**(T46 lock 이 사라지고 PROGRESS T46 이 ✅ 된 뒤) — T46 워커 C 가 T46 안에서 고쳐도 된다(그러면 이 번호는 «T46 에 흡수» 로 닫는다).
1. 증상(sess-0958-19455 · 워커 H 가 `git show origin/screens:01_lobby.png` 등을 `Read`): 540×1168 PNG 에서 UI 프레임(9:19.5)이 **가운데 x176~363 · y383~786(188×404 px = 프레임 폭의 34.8%)** 에만 그려지고, 나머지는 월드 카메라 그림(로비 = 하늘색 배경색 · 전투 = 주황 들판) · 전투 계열(02~05)은 나무·캐릭터 스프라이트가 UI **위에** 확대돼 겹친다. `layout.json` 은 `UiTag.Measure(app.Frame)` 의 프레임 % 라 정상(T36 회차 1 이 표 점수 10.0 으로 확인) — 눈 비평만 막힌다(임시 = `tools/png_crop.py --strip`).
2. 원인 확정부터(로그 한 줄): `Save` 안에서 `Canvas.ForceUpdateCanvases()` 뒤 `canvas.pixelRect · canvas.scaleFactor · app.Frame.rect · Screen.width×height · cam.pixelRect` 를 `Debug.Log` 로 남기고 CI 로그에서 읽는다. 후보 = ⓐ `CanvasScaler(ScaleWithScreenSize · Expand)` 가 **Screen.width/height(배치 모드 화면)** 로 scaleFactor 를 정하는데 캔버스 pixelRect 는 RT 카메라(540×1168) 라 프레임(AspectRatioFitter FitInParent)이 작게 들어감 ⓑ ForceUpdateCanvases 한 번으로 AspectRatioFitter 가 새 부모 크기를 못 받음(한 프레임 더 · `LayoutRebuilder.ForceRebuildLayoutImmediate(Frame)`) ⓒ 월드 스프라이트 겹침 = 캔버스가 카메라 모드라 월드 투명 정렬에 섞임(`canvas.sortingOrder = 32767` 또는 **2패스**: 월드 카메라 → RT 먼저, 그 다음 UI 만(cullingMask = UI 층 · clearFlags Depth)).
3. 수정(촬영 코드만 · 게임 코드 0): 촬영 동안 `CanvasScaler` 를 `ConstantPixelSize · scaleFactor = ShotW/FrameW(0.5)` 로 바꿨다가 되돌리거나(프레임 1080×2337 → 540×1168 꽉 참), 캔버스를 건드리지 않고 UI 전용 카메라(Overlay 캔버스는 카메라 RT 로 못 찍으므로 ScreenSpaceCamera 유지)로 2패스. 어느 쪽이든 **PNG 에서 프레임이 ≥ 95% 를 채우고 UI 가 맨 위**여야 한다. `UiShotsTests` 에 «`app.Frame` 의 픽셀 사각형(카메라 기준) 이 RT 의 ≥ 95%» 단언 1개(있으면 다음 회귀를 CI 가 잡는다).
4. 확인 = 코드 커밋의 CI 유니티 잡 초록 → 새 `screens` 배포의 `01_lobby.png` 를 `Read` 로 봤을 때 화면 전체가 UI. 그 뒤 점수판의 «T58 뒤 다시 본다» 항목(T36 글자 덩어리 −0.5 등)을 다음 회차에서 재평가.
5. 게이트 + PROGRESS T58 행 + 결정 기록 한 줄.

### T59 — ⚑⚑⚑ WebGL 배포 크래시: 페이지 열자마자 «RangeError: Maximum call stack size exceeded»(invoke_iii → wasm 재귀) — 주인 폰 Chrome 스크린샷 2026-09-06 (최우선 · 제약 없음 · 다른 작업보다 먼저) — **🔄 진행(워커 G · sess-1042-7886 · 코드 `2cd0eb1`)**: 배포된 wasm 그대로를 headless chromium 으로 열면(데스크톱·Pixel 7 흉내·JS 스택 100KB·Liftoff 전용·KST/ko-KR·터치·센서 이벤트 전부) **재현되지 않고 로비까지 뜬다** · 주인 스택의 `wasm-function[147169]`·`[147187]` 은 그 wasm 에서 둘 다 «MethodInfo→invoker_method 로 되부르는 IL2CPP 미해결 호출 스텁»(직접 호출자 0 · 함수 포인터로만 불림 · 크기 94/80B) = «컴파일된 본체가 없는 메서드를 함수 포인터/가상 호출로 부를 때 스텁↔invoker 무한 상호재귀» 꼴 → 어느 메서드인지는 **이름 있는 스택**이 있어야 한다 → 이 커밋 = `webGLDebugSymbols: 2`(Embedded · 스택에 C# 이름) + 부팅 마커 4개(`boot: save/audio/ui` · `ready lobby`) + T60 스모크. **다음 워커**: ⓐ 그 CI 의 gh-pages 를 `tools/webgl_smoke.sh --gh-pages` 로 열어 초록 확인 ⓑ 주인이 폰에서 다시 열어 준 스크린샷(이제 함수 이름이 보인다)의 이름을 PROGRESS «주인 콘솔 에러 보고함 ④» 에서 읽고 그 메서드를 고친다(스트리핑이면 `Assets/link.xml` 보존 · 제네릭이면 명시 인스턴스) ⓒ 스크린샷 전엔 링크 후보를 미리 좁힌다(PROGRESS T59 진행 기록의 «후보» 절). 자세한 근거는 PROGRESS T59 진행 기록.
범위: 원인이 있는 C# 어디든(`Assets/Scripts/Game/*` 우선) · `ProjectSettings/ProjectSettings.asset`(`webGLDebugSymbols` · `webGLExceptionSupport` 진단용) · `Assets/Tests/PlayMode`(부팅 스모크 회귀) · 필요하면 `.github/workflows/ci.yml`
배포 상태: gh-pages `20b11aa`(10:37 UTC · **첫 WebGL 배포**) = main `d6f66eb`. 즉 WebGL 에서 게임이 돌아간 적이 아직 없다 — 에디터/PlayMode 에서만 초록이었다. 스택: `invoke_iii (KkomaKnight.framework.js:9:473704)` → `wasm-function[147169]` → `[147187]` → … 반복 = C# 쪽 **무한 재귀**(또는 WebGL 의 작은 스택을 넘는 깊은 재귀). 첫 화면(로딩 직후)에서 난다.
⚑ **주인 확인(11:3X UTC): «어쨌든 핸드폰으로 잘 된다».** gh-pages 배포는 그대로 `20b11aa`(d6f66eb) 라 **같은 빌드**에서 첫 로드(10:3X)엔 죽고 지금은 된다 = 일시적(첫 로드 캐시/메모리·오디오 디코드·탭 복귀 등 후보). 워커 G(sess-1042 · 헤드리스 재현 실패 · wasm 디코드까지 감)는 **이 세션 안에서** 조사 결과(재현 안 됨 · 후보 · 스택 함수 147169/147187 의 정체)를 PROGRESS 완료 기록에 남기고 T59 를 «원인 미확정 · 주인 확인으로 종결» 로 닫는다. 남는 일은 T60(배포 스모크 게이트 · 모바일 UA 포함) 과 «다시 나면 재등재». 더 파지 않는다.
1. **재현·읽을 수 있는 스택 확보**: ⓐ `webGLDebugSymbols: 1`(Embedded) 로 켜서 wasm 함수 이름이 스택에 나오게(빌드 한 번 더 · 크기 늘어나도 진단 동안은 허용) · 진단 뒤 되돌릴지는 워커 결정 기록. ⓑ 워커 환경에서 `npx playwright` + chromium headless(`--use-gl=angle --use-angle=swiftshader`)로 https://kuzuni.github.io/aaawunity/ 를 열어 `pageerror`·`console.error` 를 파일로 받는다(T60 의 `tools/webgl_smoke.sh` 를 여기서 먼저 만들어도 된다). WebGL 컨텍스트가 안 떠도 이 에러는 wasm 안이라 그대로 재현된다.
2. **코드 감사(재귀 후보 · 스택 이름이 나오기 전에도 볼 것)**: 정적 초기화 고리(`Palette.Cat → App.I.Assets.Color → AssetCatalog.Build → …Palette`) · `AssetCatalog.Build/Prefab/Sprite` 의 «없으면 Build» 재진입 · `UiKit.Spawn` 대체 경로 · `Screens.Go ↔ Show` · `TopBar.Build` · `Overlay.Close → 콜백 → Close` · `Bootstrap.Start → DataLoader(WebGL 은 UnityWebRequest 코루틴 경로 · 에디터와 다른 분기) → OnLoaded → App.Show` · 프로퍼티가 자기 자신을 돌려주는 실수(`X => X`) · `Audio` 초기화 · DOTween `OnComplete` 안에서 같은 트윈 재시작. WebGL 은 스레드 없음·`System.IO` 없음·리플렉션 제한 — 에디터에서만 도는 분기(`#if UNITY_EDITOR` 밖의 에디터 전용 API)도 본다.
3. **고친 뒤 회귀**: PlayMode 에 «부팅 스모크»(Bootstrap → 데이터 로드(UnityWebRequest 경로를 `file://` 로 강제) → 로비 → 전투 진입 → 팝업 1개) 가 없으면 만든다 · `LogAssert.NoUnexpectedReceived`.
4. **확인 수단** = ⓐ CI 유니티 잡 초록 ⓑ 새 gh-pages 배포를 1 의 headless 스크립트로 열어 콘솔 에러 0 + 로비 도달 ⓒ PROGRESS 완료 기록에 «원인 한 줄(어느 함수가 어느 함수를 다시 불렀나)» 과 «배포 URL 에서 무엇으로 확인했나» 를 적는다. 주인이 폰에서 다시 열어 본다.

### T60 — 배포 스모크 게이트: WebGL 빌드를 headless 브라우저로 열어 «콘솔 에러 0 · 로비 도달 · 전투 진입» 을 확인한 뒤에만 gh-pages 에 배포 (주인 상시 지시 2026-09-06 · T59 뒤 또는 T59 워커가 같이) — **코드 완료(워커 G · sess-1042-7886 · `2cd0eb1` · T59 와 같은 커밋)**: `tools/webgl_smoke.js`(playwright 판정기) + `tools/webgl_smoke.sh`(`--gh-pages` = git 으로 받아 로컬 서버 · `--dir` · URL) + `App.DebugGo`/`ready lobby`/`ready battle` 마커 + `ci.yml` build-webgl 의 «배포 스모크» step(빨강이면 배포 step 안 돎 · Artifact `webgl-smoke`) + 배포 뒤 gh-pages URL 재확인(5회 재시도 · Artifact `webgl-smoke-ghpages`). **확인 = 그 커밋의 CI build-webgl 잡** — 스모크 step 초록 + gh-pages 갱신이면 ✅ 로 닫는다(다음 워커 · 오디오 경고는 T64). 워커 로컬 실행은 §1 규칙대로 세션마다 `tools/webgl_smoke.sh --gh-pages`(이 환경은 kuzuni.github.io 가 프록시 403 이라 git 경유).
범위: `.github/workflows/ci.yml`(`build-webgl` 잡 · 배포 step 앞에 스모크 step) · `tools/webgl_smoke.sh` + `tools/webgl_smoke.js`(playwright) · `Assets/Scripts/Game/App.cs`(진단 훅: 로비 준비 시 `Debug.Log("[KkomaKnight] ready lobby")` · 전투 진입 시 `ready battle` · JS 에서 `SendMessage("App","DebugGo","battle")` 로 전투 자동 진입 — 릴리스에서도 무해한 로그 한 줄) · ROUTINE §1·§3(이 커밋에서 규칙은 이미 적음)
주인 원문(2026-09-06 · 10:4X UTC): «항상 배포나 커밋 푸시 하기 전에 에러 확인하고 겜 들어가 봐서도 에러 뜨는지 확인하고 고치고 그러라 하셈».
1. `build-webgl` 잡: 빌드 → `python3 -m http.server` 로 `build/WebGL/KkomaKnight` 를 띄움 → `npx playwright@latest` chromium headless(swiftshader)로 열어 ⓐ `pageerror`·`console.error` 0 ⓑ Unity 로딩 완료(`unityInstance` 존재 · 로딩바 사라짐) ⓒ 콘솔에 `[KkomaKnight] ready lobby` ⓓ `SendMessage("App","DebugGo","battle")` 뒤 10초 동안 에러 0 + `ready battle` — 하나라도 실패하면 **배포 step 을 건너뛴다**(gh-pages 는 마지막 초록 빌드 유지) 그리고 잡을 빨강으로. 스크린샷 PNG 는 Actions artifact 로만(커밋 금지).
2. 배포 뒤 같은 스크립트를 gh-pages URL 로 한 번 더(경로·압축(`webGLCompressionFormat`)·캐시 차이).
3. 워커용: `tools/webgl_smoke.sh [URL]`(기본 = gh-pages) — 코드 커밋 전/후 워커가 직접 돌려 완료 기록에 결과 한 줄. playwright 설치가 환경에서 막히면 «워커 결정 기록» 에 남기고 CI 결과로 대신한다.
4. 게이트 + PROGRESS T60 행.

### T61 — 특전 카드 등장 순서에 맞춘 «Shine»(AllIn1SpriteShader) — 카드가 하나씩 뜰 때 반짝임도 하나씩 (주인 2026-09-06 · T49 뒤 · 제약 없음)
범위: `Assets/Scripts/Game/Overlay.cs`(LevelUp 148행 · PerkBook 224행 — T49 의 `UiKit.Stagger` 자리) · `UiKit.cs`(`Stagger`/`Reveal` 에 «드러난 뒤 shine» 훅 · shine 트윈 헬퍼) · **새 머티리얼** `Assets/KkomaKnight/PerkShine.mat`(쉐이더 = `AllIn1SpriteShaderUiMask` · SHINE_ON · 카탈로그 키 `mat.perkShine` · .meta 는 `gen_meta.py`) · `Assets/KkomaKnight/catalog.json` · `docs/assets-map.md` · `Assets/Tests/PlayMode/UiSmokeTests`
순서: **T49 뒤**(✅ · `fdb8d35`) — 같은 stagger 코드 위에 얹는다. T52·T53 도 끝났으니 Overlay 충돌 없음(rebase 만).
주인 원문(2026-09-06 · 11:1X UTC): «그 특전들 순서대로 등장할 때 shine 효과도 순서대로 돼야 함 · 올인원 스프라이트 쉐이더».
1. **재료** = AllIn1SpriteShader(이미 `mat.hitFlash` 로 쓰는 에셋 · 주인 에셋 규칙 ⓑ). UI Image 에는 **`AllIn1SpriteShaderUiMask`** 쉐이더(UI 마스크·RectMask2D 호환 · `_ShineLocation` 188행 · `SHINE_ON` 키워드)를 쓴 머티리얼 하나를 새로 만든다 — 프로퍼티: `_ShineColor` 흰색(알파 0.6~0.8) · `_ShineWidth` 0.10~0.15 · `_ShineRotate` 약 0.6rad(왼쪽 위 → 오른쪽 아래) · `_ShineGlow` 0~5 · `_ShineLocation` 은 코드가 0→1 로 움직인다. 머티리얼 YAML 은 `HitFlash.mat` 을 본떠 텍스트로 쓴다(키워드 `SHINE_ON` · GUID 결정적).
2. **어디에**: 카드의 **프레임 그림**(PerkCard 의 `CardFrame_*` Image · 등급색 프레임) 에 이 머티리얼을 붙인다(글자·아이콘엔 안 붙임 · 글자는 T52 «한 색» 그대로). 카드마다 **머티리얼 인스턴스**(`new Material(mat)` 또는 `MaterialPropertyBlock` 은 UI Image 가 못 쓰니 인스턴스) — 카드가 파괴될 때 인스턴스도 `Destroy`(누수·콘솔 경고 0 · §1).
3. **타이밍**: T49 의 `UiKit.Stagger` 가 카드 i 를 `start + i·step` 에 드러낸다(3택 = 0.22 · 0.33 · 0.44s · 각 0.28s Reveal). shine 은 **각 카드의 Reveal 이 끝나는 시점**(또는 Reveal 시작 + 0.1s · 워커가 보고 정함)에 `_ShineLocation` 0→1 을 **0.35~0.45s** 로 한 번 훑고(`DOTween.To` · `Ease.InOutSine` · `SetUpdate(true)` · 마스터 시퀀스에 `Insert`) 끝나면 `_ShineLocation` 을 화면 밖(0 또는 1)에 둔다. 카드 3장이면 shine 도 3번, 같은 간격으로 뒤따라간다 → «등장 순서 = 반짝임 순서». 스킵(탭)·`CompleteAllTweens` 때는 shine 도 끝 상태로.
4. **보유 특전(PerkBook)**·«새로고침» 재굴림도 같은 규칙(T49 와 같은 목록). 승리/패배 팝업엔 붙이지 않는다(카드가 아니다 · 주인이 특전만 말함).
5. **테스트**(PlayMode `UiSmokeTests` ⑤ 확장): 3택을 연 뒤 ⓐ 카드 프레임 Image 의 material 쉐이더 이름에 `AllIn1SpriteShaderUiMask` ⓑ shine 트윈이 카드 순서대로 시작(마스터 시퀀스 안 Insert 시각이 단조 증가 — `UiKit` 이 시작 시각 목록을 돌려주게) ⓒ `CompleteAllTweens` 뒤 `_ShineLocation` 이 끝 값 ⓓ Close 뒤 머티리얼 인스턴스 0(`Resources.FindObjectsOfTypeAll<Material>` 이름 «PerkShine (Instance)» 0) ⓔ `LogAssert.NoUnexpectedReceived`. WebGL 에서 쉐이더 키워드가 빠지지 않게 머티리얼 에셋에 키워드를 박는다(런타임 `EnableKeyword` 만 쓰면 스트리핑될 수 있다 · T59 규칙: 배포 스모크에서도 확인).
6. 게이트 + assets-map 한 줄 + PROGRESS T61 행 + 완료 기록(확인 = CI PlayMode + screens 04 PNG 는 참고 · 배포 스모크).

### T62 — 아레나 «순위» 화면(`23_arena_enter.jpg` · 시상대 1·2·3위 + 순위 목록) = GUI Pro **`Social_Ranking` + `ListItem_Ranking`** 프리팹을 조금 변형해서 (주인 2026-09-06 «랭킹 UI 유난히 안 맞음 · 유사한 프리팹 있으니 거기서 변형» · T43 뒤 · 비평 ≥ 8.0)
범위: `Assets/Scripts/Game/EventsScreen.cs`(T43 이 만든 아레나 입장(23) 페이지 · 순위 목록 부분 — 필요하면 25 «순위 보상» 의 보상 줄도 같은 ListItem 으로) · `Assets/KkomaKnight/catalog.json`(`ui.socialRanking` = `Prefabs~DemoScenes/Social_Ranking.prefab` · `ui.listRanking` = `Prefabs~DemoLayout/ListItem_Ranking.prefab`) · `docs/assets-map.md` · `Core/Layout.cs`·`docs/ref-layout.md`(23 표는 그대로 · 실측 보정만) · `Assets/Tests/PlayMode/EventsScreenTests.cs`·`UiShotsTests`(23 PNG)
순서: T43(✅ 코드 · 비평 회차 진행 중) 뒤 — T43 비평 lock 과 겹치면 그 워커가 이어서 잡는다.
주인 원문(2026-09-06 · 11:3X UTC): «랭킹 부분 UI 유난히 안 맞음. 사실 랭킹 부분 UI 프리팹 유사한 게 프리팹으로 있어서 거기서 조금 변형해서 쓰면 거의 똑같은데 참고해».
1. **재료**: `Theme_Light/Prefabs/Prefabs~DemoScenes/Social_Ranking.prefab`(시상대 + 목록 데모 화면) 과 `Prefabs~DemoLayout/ListItem_Ranking.prefab`(한 줄: 등수 · 초상 · 이름 · 점수 — 데모의 RankingNum_1·2·3 그림 포함). 규칙은 ⓑ 그대로 — 통째로 세우는 게 아니라 **부품으로 뜯어 레퍼런스 23 구도에 맞춘다**. 다만 주인 말대로 이 프리팹은 23 과 거의 같으니 **조각 이동은 최소**(시상대 3자리 · 목록 줄 형식은 프리팹 그대로 두고 위치·크기만 표 ⑬(23) 에 맞춤).
2. **시상대**: 프리팹의 1·2·3위 자리(초상 + 이름 + 배너)를 그대로 쓰고, 우리 데이터(껍데기 · 상대 초상 = `ui.iconFoe*` · 이름 «도전자 N» · 전투력)로 글자만. 1위 왕관 = `ui.iconCrownGold`(assets-map 261 행) 유지.
3. **순위 목록**: `ListItem_Ranking` 줄을 목록 개수만큼 복제 — 등수(1~3 은 프리팹의 RankingNum 그림 · 4 이상은 숫자) · 초상 · 이름 · 전투력 · 🏆 점수(레퍼런스 23 의 줄 구성) · «나» 줄은 강조(프리팹에 Focus 변형이 있으면 그것). 스크롤은 기존 UiKit 방식.
4. **오른쪽 위 Rewards · Merchant 아이콘 · 바닥 «Challenge 🎫x1»** 은 T43 것 유지(위치만 표대로). 25 «순위 보상» 의 «1~4위 보상 줄» 도 같은 `ListItem_Ranking` 으로 바꾸면 통일되니 워커가 보고 정한다(결정 기록).
5. **비평**: §5 절차 — screens 브랜치 23 PNG 를 `docs/ref/23_arena_enter.jpg` 와 나란히 `Read` 로 보고 10점 채점 · 8.0 이상이 ✅. T43 회차 기록 아래에 «T62 회차» 로 잇는다.
6. 게이트 + assets-map 두 줄 + PROGRESS T62 행 + 완료 기록(확인 = CI PlayMode `EventsScreenTests` + screens 23 PNG 점수 + 배포 스모크).

### T63 — ⚑⚑ 게임 전체 글자 가독성: «너무 작아서 안 읽히는» 글자 전부 키우기 — 최소 크기 규칙 + 화면별 전수 점검 (주인 2026-09-06 · 최우선급 · UI 화면 전부 · 비평과 함께)
범위: `Assets/Scripts/Game/UiKit.cs`(`Text`/`Label`/`Button`/`SetText` 헬퍼 · **최소 글자 크기 상수 + 강제 하한** · bestFit 의 min/max) · 화면 코드 전부(`Screens`·`GearScreen`·`GearUi`·`ForgeScreen`·`ShopScreen`·`PetScreen`·`EventsScreen`·`LobbyPopups`·`BattleScreen`·`BattleWorld`(데미지 팝·HP 바 숫자)·`Overlay`) · `Core/Layout.cs`·`docs/ref-layout.md`(글자 칸 높이가 모자라면 표 보정 · ±3%p 안에서) · `Assets/Tests/EditMode`·`PlayMode`(글자 크기 하한 게이트)
순서: 제약 없음 — 다른 UI 작업(T62 · 비평 회차)과 같은 파일을 만지므로 **화면 단위로 lock 을 나눠**(T63-로비 · T63-전투 · … 식으로 PROGRESS 에 하위 행) 작은 커밋으로 rebase 하며 진행. UiKit 하한은 **맨 먼저 한 커밋**으로.
주인 원문(2026-09-06 · 11:4X UTC): «게임 전체적으로 글씨가 너무 작아서 안 읽히는 게 많음. 가독성 있게 다 바꾸셈».
현황(등재 세션 실측 · 프레임 1080×2337 기준 · 폰 폭 412css px 면 프레임 1px ≈ 0.38css px): `UiKit.Text/Label` 호출의 글자 크기 분포 = **22~30 이 116곳**(폰에서 8~11px · 안 읽힘) · 10~21 이 27곳(아이콘 라벨·pill) · 34~40 이 35곳 · 44 이상이 제목·숫자. bestFit `resizeTextMinSize` 가 12~20 인 곳 14곳(자동으로 더 작아진다).
1. **하한 규칙(UiKit 상수 · 한 곳)**: 본문·설명·목록 줄 = **최소 40**(폰 ≈15px) · 버튼 글자 = **최소 44** · 보조 라벨(pill 숫자·«남은 횟수»·타이머·등수) = **최소 36** · 제목/명판 = **60 이상** · 데미지 팝·전투 숫자 = 지금보다 1.3배 · `resizeTextMinSize` 는 **32 아래로 못 내려가게**. `UiKit.Text/Label/SetText` 가 `size < MinBody` 를 받으면 하한으로 올리고(경고 없이) — 정말 작아야 하는 곳(아이콘 위 «+1» 배지 등)은 `allowSmall:true` 로 명시. 굵기: Jua 는 굵기 변형이 없으니 **outline(이미 기본 true) 유지 + 어두운 글자/밝은 배경 대비 확인**(회색 InkSoft 는 InkLight 대신 Ink 로 올리는 쪽).
2. **레이아웃 대응**: 글자를 키우면 칸이 넘친다 → ⓐ 줄바꿈 허용(`horizontalOverflow = Wrap` · 설명은 2줄까지) ⓑ 칸 높이가 표(ref-layout)보다 작으면 표 ±3%p 안에서 높이 보정 ⓒ 그래도 안 들어가면 **글자를 줄이지 말고 문구를 줄인다**(«공격력의 100% · 8마리 관통» → «공격력 100% · 8마리 관통» 식 · 데이터 JSON 은 불변 · 표시 문구 표 = T53 의 PerkText 헬퍼에). 잘림(…)·겹침 0.
3. **전수 점검 순서**(화면마다 하위 행 · 각각 screens PNG 를 `Read` 로 보고 «폰 폭 412 로 축소해도 읽히나» 를 기준으로): ① 로비(01) ② 전투 HUD·발밑 바 숫자·데미지 팝(02·03) ③ 레벨업 3택·보유 특전(04·05 · 설명 40 · T52·T53 뒤) ④ 장비·세부(06·07) ⑤ 대장간(08) ⑥ 상점(09·10) ⑦ 설정(12) ⑧ 펫(13·14) ⑨ 던전·아레나(20~26 · T62 와 조율) ⑩ 로비 팝업 6종(11·15~19) ⑪ 승리·패배·악마·천사·쉼터 팝업 ⑫ 토스트·확인 팝업.
4. **게이트**: EditMode/PlayMode 테스트 — 모든 화면을 열고 `Text` 전부를 모아 `fontSize ≥ 하한(allowSmall 제외)` · bestFit min ≥ 32 · 잘림 검사(`preferredWidth/Height` 가 rect 보다 크면 실패 · 2줄 허용 칸은 예외 목록) · `LogAssert.NoUnexpectedReceived`. 이 테스트가 이후 모든 UI 커밋의 회귀 게이트.
5. **비평 점수**는 배치 표 기준이라 글자 크기와 별개지만, 글자를 키워 칸을 옮겼으면 그 화면은 §5 회차를 한 번 더 돈다(8.0 유지).
6. PROGRESS T63 행(+ 하위 행) + 완료 기록(«화면별 최소 글자 크기 before→after 표» 한 줄씩 · 확인 = CI PlayMode 게이트 + screens PNG + 배포 스모크 · 주인 폰 확인).
### T64 — WebGL 오디오: BGM(bgm.*) 이 «NotSupportedError: Failed to load because no supported source was found» · SFX(snd.*) 가 «EncodingError: Unable to decode audio data» + «Loading FSB failed for audio clip "click"» (T59/T60 스모크에서 발견 · 워커 G 등재 2026-09-06 · 제약 없음)
범위: `Assets/Audio/**/*.ogg.meta`(임포트 설정 · WebGL 플랫폼 오버라이드) · (필요시) `Assets/Scripts/Game/Audio.cs` · `tools/webgl_smoke.js`(끝나면 오디오 예외 제거 · `AUDIO_RE`)
1. 사실(워커 G · 배포 `20b11aa` 를 headless chromium 으로): 데이터 로드 직후 BGM 재생(`Audio.Bgm("bgm.lobby")`)에서 유니티 로더 에러 핸들러가 `NotSupportedError`(HTML `<audio>` 매체 요소) 를 잡고, 첫 클릭의 SFX 에서 `decodeAudioData` 가 `EncodingError` → «Loading FSB failed». **브라우저 탓이 아니다** — 같은 브라우저에서 `Assets/Audio/bgm/lobby.ogg` 원본을 `decodeAudioData`(11.3초 OK)·`<audio>`(MIME audio/ogg·audio/mp4 둘 다 canplay) 로 직접 돌리면 된다. 즉 **유니티가 JS 로 넘기는 데이터(FSB 안의 Vorbis · Ogg 프레이밍 없음)** 를 브라우저가 못 읽는 것. 프레임워크의 `jsAudioGetMimeTypeFromType` 은 13(MP3)·20(WAV) 외엔 전부 `audio/mp4`(AAC) 로 넘긴다 = 유니티 WebGL 의 «압축 유지(CompressedInMemory)» 는 AAC/MP3 전제.
2. 고칠 방향(워커가 정한다 · 결정 기록 한 줄): ⓐ BGM 3곡 = WebGL 플랫폼 오버라이드 `loadType: 0`(DecompressOnLoad · Web Audio 가 Vorbis 를 디코드 · 메모리 ≈ 40MB) 또는 AAC(에디터 인코더가 리눅스 CI 에서 되는지 먼저 확인) ⓑ SFX 는 이미 DecompressOnLoad 인데도 실패 → `preloadAudioData`/`loadInBackground`/FSB 포장 문제 — CI 유니티 잡 로그와 새 배포 스모크(`AUDIO⚠` 줄)로 확인 ⓒ 폰(Android Chrome · iOS Safari 는 Vorbis 디코드 불가) 에서 소리가 나는지는 주인 확인.
3. ✅ 조건: `tools/webgl_smoke.sh --gh-pages --strict-audio` 초록(오디오 예외 없이) → `webgl_smoke.js` 의 T64 예외(`AUDIO_RE` 경고 강등)를 지우고 CI 도 `--strict-audio` 로.

## 3. 게이트 (커밋 전 · 세션 종료 전)

```bash
dotnet build tools/dotnet/KkomaKnight.sln -c Release --nologo     # 컴파일
dotnet test tools/dotnet/Tests/KkomaKnight.Tests.csproj -c Release --no-build   # 순수 C# 테스트 (NUnit 3.6.1 = 유니티 포크와 같은 API 면 — Does.Contain(object) 같은 신형 API 금지)
python3 tools/gen_meta.py --check                                 # .meta 누락/고아
python3 tools/gen_catalog.py --check                              # catalog.json 의 에셋 경로가 전부 실재하는가 (에셋 키를 바꿨으면 --check 대신 그냥 실행해 재생성)
python3 tools/check_catalog_keys.py                               # 반대 방향: 코드의 카탈로그 키 리터럴이 전부 catalog.json 에 있는가 (없으면 런타임 «sprite 없음» 경고·빈 그림 — T12 · CI dotnet 잡에도 있음)
tools/check_unity_null.sh                                         # 유니티 가짜 null 게이트: Assets/Scripts 에 «GetComponent…() ??»·«Find(…) ??» 0건 (있으면 에디터 MissingComponentException — UiKit.Ensure<T> 로 · T11 · CI dotnet 잡에도 있음)
tools/check_data_sync.sh [.aaaw-src]                              # data ↔ aaaw main
dotnet run --project tools/dotnet/Sim -c Release -- --seeds 11,12,13  # (T2 이후) 이식 검증
```

**플레이 콘솔 에러 0 게이트(주인 상시 지시)**: 화면·전투·팝업 코드를 바꿨으면 `Assets/Tests/PlayMode/UiSmokeTests.cs` 가 그 화면을 열고 `PlayLog.AssertNoRed`(빨간 줄 0) + `[UiKit]`/`[AssetCatalog]` 경고 0 + 데모 잔여 글자 0 을 통과해야 한다(CI 유니티 잡 · 워커는 로컬에서 못 돌리므로 CI 런 번호로 확인 · 버튼 라벨을 바꿨으면 테스트의 라벨도 같이). 확인 수단이 없으면 완료 기록에 «주인이 에디터 플레이로 확인할 것 — 콘솔 빨간 줄 0» 을 명시한다. 주인이 붙인 콘솔 로그가 «주인 콘솔 에러 보고함» 에 미해결로 남아 있으면 게이트 미통과로 본다.

## 5. UI 비평 회차 (주인 지시 2026-09-06 · UI 작업의 ✅ 조건)

1. **채점 대상·기준**: 레이아웃 · 비례 · 비율만(레퍼런스 `docs/ref/NN.jpg` 와 «최대한 같게»). 아이콘·그림·색·폰트는 채점 밖 — **에셋 안에 있는 것으로 채우고 새 그림은 만들지 않는다**.
2. **회차 절차**(T46 하니스): 코드 push → CI(≈25분 · `[skip ci]` 금지) → `screens` 브랜치에 PNG·layout.json → 워커가 `tools/ui_score.py <화면>` 로 **표 점수** → PNG 와 ref jpg 를 `Read` 로 나란히 보고(하니스 PNG 가 띠로 찍히는 동안(T58)은 `python3 tools/png_crop.py --strip <png> <out.png> 3` 으로 UI 띠를 3배 확대해 본다 · PIL·ffmpeg 없이 됨 · 구도는 판독되나 글자는 안 읽힌다) **감점(최대 −2.0 · 항목당 −0.5 · 이유 한 줄)** → `최종 = 표 − 감점`. 회차를 이어갈 워커는 자기 세션이 끝나기 전에 `send_later`(T16 세션이 쓴 방식)로 CI 뒤에 다시 깨어나거나, 다음 워커가 PROGRESS 점수판의 «다음 고칠 것» 을 읽고 잇는다(같은 T 번호 · lock 규약 그대로).
3. **✅ 조건**: 최종 **8.0 이상**. 미만이면 «다음 고칠 것» 을 적고 고친다(회차마다 점수가 올라야 한다 · 같은 점수 2회면 표 행 자체를 의심하고 `ref-layout.md` 회차 정정 절에 근거를 적는다).
4. **점수판 형식**(PROGRESS «UI 비평 점수판» 표 한 줄): `| 화면 | T | 회차 | 커밋 · CI 런 | 표 점수(통과/행) | 감점·이유 | 최종 | 다음 고칠 것 |`. 회차 로그의 행별 대조표는 그 T 의 완료/진행 기록에 붙인다.
5. 새 화면(설정·펫·던전·아레나·사이드 팝업 등)은 그 화면 작업자가 먼저 `docs/ref-layout.md` 에 **⑧~ 표를 추가**(jpg 에 5% 격자 기준으로 판독 · ±0.5%p 오차 명시)하고 나서 채점한다 — 표가 없으면 채점 불가 = ✅ 불가.

## 6. 다른 계정의 워커 합류 (E~H · 주인 지시 2026-09-06 «aaaw 처럼 루틴 다른 계정도 같이»)

> 이 절은 **두 번째 claude.ai 계정에서 연 Claude Code 세션**이 그대로 따라 하면 되게 써 두었다. 그 세션은 이 절만 읽고 `/schedule` 로 루틴 4개를 만든다. 주인이 할 일은 ①뿐이다.

### ① 주인이 먼저 할 것 (계정 2 쪽에서 · 한 번만)
1. 계정 2 의 claude.ai → **GitHub 연결**에 `kuzuni/aaawunity` 가 보이고 **push 가 되어야** 한다(같은 GitHub 사용자 kuzuni 를 연결하면 끝 · 다른 GitHub 사용자면 레포 Settings → Collaborators 에 **Write** 로 추가). 확인법: 계정 2 에서 클라우드 세션을 열어 `git push origin main` 이 되는지(빈 커밋 말고 `docs/claims/README.md` 끝에 «계정 2 확인 YYYY-MM-DD» 한 줄 추가로).
2. 계정 2 에 **환경(Environment)** 이 하나 있어야 한다(기본 «Default» 면 된다 · 프록시 정책은 계정 1 과 같다고 가정 — 다르면 dotnet 설치가 막힐 수 있으니 첫 런 로그를 본다).

### ② 계정 2 의 Claude Code 세션이 할 것 — 루틴 4개 생성 (`/schedule` → create)
- **이름**: `꼬마기사 유니티 이식 병렬 워커 E (:12)` · `… F (:27)` · `… G (:42)` · `… H (:57)`
- **cron(UTC · 분 슬롯)**: E `12 * * * *` · F `27 * * * *` · G `42 * * * *` · H `57 * * * *` — 계정 1 의 A~D(:05/:20/:35/:50) 사이사이에 들어가 8개가 7~8분 간격으로 돈다.
- **레포**: `https://github.com/kuzuni/aaawunity` · **모델**: `claude-fable-5-1` · **도구**: Bash, Read, Write, Edit, Glob, Grep, Task, WebFetch · **환경**: 그 계정의 Default.
- **프롬프트**(아래 블록을 그대로 · `X` 를 E/F/G/H 로만 바꾼다 · 계정 1 의 A~D 프롬프트와 글자까지 같다):

```
너는 «꼬마기사 키우기 유니티 이식»(kuzuni/aaawunity) 병렬 워커 X 다. 여러 워커(A~H · A~D 는 매시 :05/:20/:35/:50 · E~H 는 :12/:27/:42/:57)가 두 계정에서 동시에 돌아간다.
1. 먼저 `git fetch && git checkout -B main origin/main` 으로 최신 상태에서 시작한다 (pull --rebase 금지). 이 환경은 detached HEAD 로 체크아웃되므로 이어서 `git branch --set-upstream-to=origin/main main` 을 해 둔다.
2. **docs/ROUTINE.md 가 유일 지시서다.** 거기 적힌 세션 시작 절차·절대 규칙·작업 목록·게이트를 그대로 따른다. UI 작업은 docs/ref/README.md 와 docs/ref/*.jpg(주인 레퍼런스)를 Read 로 직접 보고 그 구도에 맞춘다. 참조 레포 kuzuni/aaaw 는 ROUTINE.md 의 방법대로 옆에 clone 해서 읽기만 하고 절대 수정·푸시하지 않는다. 수치(data/*.json)는 손으로 고치지 않는다. 지시서가 사라졌거나 읽을 수 없으면 아무것도 수정하지 말고 «지시서 없음» 으로 보고하고 종료한다.
3. 작업 전 반드시 docs/claims/ 의 선점 lock 을 먼저 잡고(규약: docs/claims/README.md · lock 은 커밋·push 가 성공해야 유효), 남의 lock 이 잡은 작업은 피한다. 선점할 작업이 없으면 게이트만 재실행하고 커밋 없이 조용히 종료한다.
4. 커밋 전 `dotnet build`·`dotnet test`·`python3 tools/gen_meta.py --check` 가 초록이어야 한다. 컴파일 안 되는 커밋 금지. 새 에셋을 만들면 .meta 를 같이 만든다.
5. 승인 프롬프트가 뜨는 명령·대화형 편집기 금지. 캡처 PNG·대용량 바이너리 커밋 금지(ROUTINE.md 가 허용한 예외는 된다). **주인의 승인·허락을 기다리지 않는다(주인 지시 2026-09-06): 판단이 필요한 것은 네가 그 자리에서 정해 바로 적용하고, 정한 내용과 이유를 PROGRESS «워커 결정 기록» 에 한 줄 남긴다. «주인 승인 대기» 에 올려 두고 멈추는 일은 없다.** 여전히 금지인 것은 셋뿐: aaaw 레포 수정 · data/*.json 손대기 · 주인이 시키지 않은 밸런스 수치 변경.
6. 모든 보고·커밋 메시지·PROGRESS 기록은 한국어로. 브랜치는 main 만 쓴다. 커밋 작성자는 `git -c user.name=kuzuni -c user.email=<이 계정의 이메일>` 로 하고, 커밋 제목 끝에 `(sess-HHMM-NNNNN · 워커 X)` 를 붙인다.
```

- 만든 뒤 **4개의 routine ID 와 첫 런 링크**를 이 문서 아래 «③ 등록 기록» 표에 적어 커밋한다(`[skip ci]`).

### ③ 등록 기록 (계정 2 세션이 채운다)

| 워커 | 슬롯 | routine ID | 계정(이메일) | 첫 런 | 비고 |
|---|---|---|---|---|---|
| E | :12 | `trig_01QJGbo5Lm88r9KXfMEcSwoz` | 계정 2 (kimmoon1995@gmail.com) | 08:12 UTC 런 https://claude.ai/code/session_01XXX3UG6B5HSq7XnQJApnD7 (T42 완료 · sess-0813-16889 · 루틴 https://claude.ai/code/routines/trig_01QJGbo5Lm88r9KXfMEcSwoz) | 2026-09-06 08:02 UTC 생성 · claude-fable-5-1 · env Default(env_016Xis527zoBbZPqrtAZVQ6x) · enabled |
| F | :27 | `trig_011tPCMoYGAyWKTUjPGDeRdN` | 계정 2 (kimmoon1995@gmail.com) | 08:27 UTC 런 https://claude.ai/code/session_018JuWedxu25LuhVHcjw4uzu (루틴 https://claude.ai/code/routines/trig_011tPCMoYGAyWKTUjPGDeRdN) | 동일 |
| G | :42 | `trig_01D222YdDfLGVEVM5jba2Bbj` | 계정 2 (kimmoon1995@gmail.com) | 08:42 UTC 런 https://claude.ai/code/session_0135YDfHdrRg9ydHLwBEBrV9 (루틴 https://claude.ai/code/routines/trig_01D222YdDfLGVEVM5jba2Bbj) | 동일 |
| H | :57 | `trig_01829E6pqXt2hmUv9ZCjrg8z` | 계정 2 (kimmoon1995@gmail.com) | 2026-09-06 08:57 UTC 예정 · https://claude.ai/code/routines/trig_01829E6pqXt2hmUv9ZCjrg8z | 동일 |

> 등록 시점(08:03 UTC)에는 아직 한 번도 돌지 않아 «첫 런» 열은 예정 시각 + 루틴 페이지 링크다. 실제 런 세션 링크는 그 루틴 페이지의 실행 목록에서 본다. 계정 2 push 권한 확인 커밋: 0155784.

### ④ 규약은 계정과 무관하게 같다
- lock·SID·90분 규약(`docs/claims/README.md`)은 그대로 — **다른 계정의 lock 도 남의 lock** 이다. SID 는 `sess-HHMM-$RANDOM` 이라 계정이 달라도 안 겹친다.
- 문서·코드 커밋을 나눠 push 하는 규칙(`[skip ci]` · §1)도 그대로. 8개가 돌면 push 충돌이 잦으니 **push 실패 → `git fetch && git rebase origin/main` → 재push** 를 습관처럼.
- 계정 2 워커가 잡으면 안 되는 것은 없다 — 작업표의 «순서» 만 지키면 된다.
- 계정 2 의 환경에서 `dotnet` 이나 GitHub MCP 가 안 되면 PROGRESS «워커 결정 기록» 에 «계정 2 환경 차이: …» 한 줄을 남기고 게이트를 돌릴 수 있는 만큼만 돌린 뒤 코드 커밋은 하지 않는다(문서 작업만).

## 4. PROGRESS.md 기록 규약

- 표의 자기 작업 행을 갱신: 상태(진행중/완료/대기) · SID · 워커 · 핵심 수치.
- 완료 시 반드시: 게이트 결과(테스트 수 · 빌드 초록) + 커밋 해시 + **«주인이 확인할 것» 한 줄** + (UI 작업이면) **비평 최종 점수 ≥ 8.0 과 점수판 행**(§5). + **«플레이 콘솔 에러 0 을 무엇으로 확인했는가»**(PlayMode 테스트 이름·CI 런 / 또는 «주인 에디터 확인 요청»).
- 판단이 필요한 것은 **기다리지 않고 스스로 정해 적용**하고(2026-09-06 주인 지시), PROGRESS «워커 결정 기록» 에 번호를 이어 «무엇을 · 왜 · 되돌리려면 어디» 한 줄로 남긴다. «주인 승인 대기» 절은 이력으로만 남는다(새 항목 추가 금지).
