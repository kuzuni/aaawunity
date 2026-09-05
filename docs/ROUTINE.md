# 루틴 작업 지시서 (병렬 워커 공용 — 유일 지시서)

> 이 문서와 aaaw 의 `PLAN.md`(스펙 정본 · 읽기 전용) 만 보고 작업한다. 보고·기록은 전부 **한국어**.
> 이 레포는 **aaaw 의 HTML 게임을 유니티로 이식**하는 곳이다. 게임 규칙·수치는 aaaw 가 정본이고 여기서는 **바꾸지 않는다**.

## ⚑ 신규 주인 지시 (위 항목이 최신)

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

## 0. 세션 시작 절차 (모든 워커 공통)

1. `git fetch && git checkout -B main origin/main` (pull --rebase 금지, 로컬 잔재 위에서 작업 금지)
2. SID 발급: `sess-HHMM-$RANDOM` (예: sess-0512-23481)
3. aaaw `PLAN.md`(스펙) → 이 문서 → `docs/PROGRESS.md` → `docs/claims/` 순서로 읽는다. **PROGRESS 의 «주인 콘솔 에러 보고함» 도 반드시 읽는다** — 아직 작업으로 안 올라간 항목이 있으면 «가장 큰 번호 +1» 로 등재하고, 그것이 선점 가능한 가장 앞 작업이 된다(콘솔 에러 수정은 UI 작업보다 우선). aaaw 는 `git clone --depth 1 https://github.com/kuzuni/aaaw .aaaw-src` 로 옆에 둔다(커밋 금지 폴더 · .gitignore).
4. [2. 작업 목록]에서 **선점 가능한 가장 앞 작업**을 lock 으로 선점한다 (규약: `docs/claims/README.md`).
5. 선점할 작업이 없으면(전부 lock 또는 전부 완료): 게이트(§3)를 재실행해 검증만 하고, 이상 없으면 **커밋 없이 조용히 종료**. 이상이 있으면 PROGRESS 에 등재하고 종료.

## 1. 절대 규칙

- **플레이 콘솔 에러 0 (주인 상시 지시 2026-09-05).** 플레이 중 유니티 콘솔에 빨간 줄(에러·예외·Assert)이 하나라도 뜨는 상태로 작업을 «완료» 라 적지 않는다. 화면/전투/팝업 코드를 바꾼 커밋은 T11 의 PlayMode 스모크 테스트(`Assets/Tests/PlayMode/UiSmokeTests.cs`)가 그 화면을 열어 빨간 줄 0 을 검증해야 한다(테스트가 없으면 같은 커밋에 추가). 검사 도우미는 `PlayLog.AssertNoRed` — **`LogAssert.NoUnexpectedReceived()` 는 쓰지 않는다**(이 프로젝트의 Test Framework 1.6 은 일반 `Debug.Log` 도 «예상 밖 로그» 로 실패시킨다 · CI #33 회귀). 런타임 Camera·RenderTexture·씬 오브젝트를 새로 만들면 URP 2D(Renderer2D · 깊이/스텐실 사용) 와 호환되는지 반드시 확인한다. 주인이 붙인 콘솔 로그는 다 고칠 때까지 «주인 콘솔 에러 보고함» 에 남기고, 고친 항목에는 커밋 해시와 원인을 적는다.
- **aaaw 레포 수정 금지.** 수치(`data/*.json`)는 `tools/check_data_sync.sh --sync` 로만 가져온다. JSON 을 손으로 고치지 않는다.
- **코드에 게임 수치를 직접 박지 않는다** — `KkomaKnight.Core.GameData` 에서 읽는다. 상수가 JSON 에 없으면 «주인 승인 대기» 에 등재하고, 그때까지는 aaaw 의 `tools/exportData.js` 에 축을 더하는 제안을 적는다(이 레포에서 임의 상수 금지).
- **새 콘텐츠(특전/시스템/수치 체계) 임의 추가 금지.** 원하면 PROGRESS «주인 승인 대기» 에 등재만.
- **커밋 전 게이트**: `dotnet build tools/dotnet/KkomaKnight.sln -c Release` 초록 · `dotnet test tools/dotnet/Tests` 초록 · `python3 tools/gen_meta.py --check` 초록. 새 에셋을 만들면 `python3 tools/gen_meta.py` 로 .meta 를 만든다(GUID 결정적).
- 전투 엔진(`Assets/Scripts/Core`)에는 `UnityEngine` 을 참조하지 않는다(asmdef `noEngineReferences: true` · dotnet 이 강제한다).
- 승인 프롬프트가 뜨는 명령·대화형 편집기(`git rebase -i` 등) 금지. 캡처 PNG·대용량 바이너리 커밋 금지(폰트 1개는 예외 — PLAN §2.1).
- 작업이 끝나면 lock 삭제 → PROGRESS 갱신 → 커밋 → push. **lock 만 잡는 커밋·문서만 바꾼 커밋은 제목 끝에 `[skip ci]`** 를 붙인다(코드가 안 바뀐 푸시로 25분짜리 유니티 빌드를 또 돌리지 않는다). 코드 커밋에는 절대 붙이지 않는다. **`[skip ci]` 커밋은 코드 커밋과 같은 push 에 묶지 않는다** — GitHub 은 push 의 머리 커밋에 `[skip ci]` 가 있으면 push 전체(앞의 코드 커밋까지)를 건너뛴다(T13 에서 실사고 · 코드 커밋을 먼저 push 하고 문서 커밋을 따로 push · 이미 묶였으면 Actions 의 `workflow_dispatch` 로 수동 실행). **push 실패 시 `git fetch && git rebase origin/main`** 후 재push (자기 lock 이 사라졌으면 진 것 — 작업 버리고 종료).
- 브랜치는 `main` 하나다(주인 결정 — 다른 브랜치에 올리지 않는다). 각 단계가 끝나면 main 에 커밋·푸시하고 PROGRESS 에 «무엇을 확인하면 되는가» 한 줄을 적는다.
- **에셋은 주인 에셋만** (위 지시 ③). 코드 생성 도형·임시 그림 금지. 새 에셋을 쓰면 `docs/assets-map.md` 표에 «용도 · 경로 · GUID(·fileID)» 를 한 줄 추가한다.

## 2. 작업 목록 (순서 고정 — lock ID = 아래 번호)

> T1~T5(주인이 정한 5단계)는 끝났다. **지금 열린 작업은 T6~T14** (T12 = 콘솔 에러 수정 · 최우선 · T13 = 특전 미리보기 줄 비례 · T14 = 전투 캐릭터 크기·공격 애니·사망 모션) — 같은 파일을 만지는 것은 아래 «순서» 대로(앞 번호의 lock 이 사라지고 PROGRESS 행이 ✅ 가 된 뒤에 잡는다). 겹치지 않는 것은 병렬 선점 가능.

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
6. 인벤 리스트 칸 = **`ListItem_EquipMent`**(카탈로그 `ui.equipCell` 신규) — 프리팹 비율 그대로 · 등급색·아이콘·+N 만 바꾼다. **장착 중인 장비는 리스트에서 숨긴다**(«장착중» 배지 없음).
7. `GearUi.Cell` 은 대장간(T8)·뽑기 결과(T9)도 쓴다 — `CellOpts` 에 «장착중 표기 on/off · 합성 가능 빨간 점» 옵션을 두고 여기서는 둘 다 끈다.
8. 게이트 + PROGRESS T7 행 + «주인이 확인할 것».

### T8 — 대장간 정리 (T7 뒤)
범위: `Assets/Scripts/Game/ForgeScreen.cs`
순서: **T7 완료 뒤**.
1. 하단 인벤에 장비가 **전부** 보인다(지금 안 보이는 원인 규명 — Grid/Content 크기·ScrollRect·Pct 겹침 — PROGRESS 에 원인 한 줄).
2. 칸 = T7 의 `ListItem_EquipMent` 칸과 **같은 크기·비례**(찌그러짐 0 · 5열 격자에서 셀 aspect 고정).
3. 합성 가능(같은 부위·종류·등급 3개 이상)한 칸은 **오른쪽 위 빨간 점**(GUI Pro 의 알림 점 스프라이트 · 카탈로그 키 `ui.redDot`).
4. 여기서는 **장착중 표기 유지**(재료 불가 · 흐리게).
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

### 신규 작업 등재
- 버그·후속 작업 발견 시 PROGRESS 표에 **이미 쓰인 번호 중 가장 큰 것 +1** 로 등재 (번호 재사용 금지, 한 번호 = 한 작업).

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

## 4. PROGRESS.md 기록 규약

- 표의 자기 작업 행을 갱신: 상태(진행중/완료/대기) · SID · 워커 · 핵심 수치.
- 완료 시 반드시: 게이트 결과(테스트 수 · 빌드 초록) + 커밋 해시 + **«주인이 확인할 것» 한 줄**. + **«플레이 콘솔 에러 0 을 무엇으로 확인했는가»**(PlayMode 테스트 이름·CI 런 / 또는 «주인 에디터 확인 요청»).
- 판단이 필요한 것은 «주인 승인 대기» 절에 번호를 붙여 모아 둔다(한 번에 묻기 위해). 기본값으로 진행했으면 그 기본값도 같이 적는다.
