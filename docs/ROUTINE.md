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
- 승인 프롬프트가 뜨는 명령·대화형 편집기(`git rebase -i` 등) 금지. 캡처 PNG·대용량 바이너리 커밋 금지(예외 2개: 폰트 1개 — PLAN §2.1 · 오디오 OGG 합계 ≤ 5MB — T28 주인 지시).
- 작업이 끝나면 lock 삭제 → PROGRESS 갱신 → 커밋 → push. **lock 만 잡는 커밋·문서만 바꾼 커밋은 제목 끝에 `[skip ci]`** 를 붙인다(코드가 안 바뀐 푸시로 25분짜리 유니티 빌드를 또 돌리지 않는다). 코드 커밋에는 절대 붙이지 않는다. **`[skip ci]` 커밋은 코드 커밋과 같은 push 에 묶지 않는다** — GitHub 은 push 의 머리 커밋에 `[skip ci]` 가 있으면 push 전체(앞의 코드 커밋까지)를 건너뛴다(T13 에서 실사고 · 코드 커밋을 먼저 push 하고 문서 커밋을 따로 push · 이미 묶였으면 Actions 의 `workflow_dispatch` 로 수동 실행). **push 실패 시 `git fetch && git rebase origin/main`** 후 재push (자기 lock 이 사라졌으면 진 것 — 작업 버리고 종료).
- 브랜치는 `main` 하나다(주인 결정 — 다른 브랜치에 올리지 않는다). 각 단계가 끝나면 main 에 커밋·푸시하고 PROGRESS 에 «무엇을 확인하면 되는가» 한 줄을 적는다.
- **에셋은 주인 에셋만** (위 지시 ③). 코드 생성 도형·임시 그림 금지. 새 에셋을 쓰면 `docs/assets-map.md` 표에 «용도 · 경로 · GUID(·fileID)» 를 한 줄 추가한다.

## 2. 작업 목록 (순서 고정 — lock ID = 아래 번호)

> T1~T5(주인이 정한 5단계)는 끝났다. **지금 열린 작업은 T17~T31**(T12~T16 은 워커가 먼저 쓴 번호 · 내 T13~T16 은 T20~T23 으로 정정) (T12 = 콘솔 에러 수정 · 최우선 · T13 = 특전 미리보기 줄 비례 · T14 = 전투 캐릭터 크기·공격 애니·사망 모션 · T15 = 프리팹 스폰 PanelView 예외 · 콘솔 에러라 최우선 · T16 = T14 의 CI #39 빨강 후속) — 같은 파일을 만지는 것은 아래 «순서» 대로(앞 번호의 lock 이 사라지고 PROGRESS 행이 ✅ 가 된 뒤에 잡는다). 겹치지 않는 것은 병렬 선점 가능.

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

### T20 — 웨이브 출발 버그 · 버프 스택 표시 = 팔각 프레임 (번호 정정: 구 T13) ✅ (완료 · `81780ac` · CI #42 · PROGRESS 참조)
범위: `Assets/Scripts/Game/BattleWorld.cs` · `BattleScreen.cs`(RefreshBuffBar)
순서: 제약 없음(T14 캐릭터 크기·T16 정지는 끝났다).
1. **주인 지적 «웨이브 내 적을 다 안 죽였는데 출발함»** — 원인부터 규명해 PROGRESS 에 한 줄: ⓐ 엔진(Battle.Tick: alive[0] 까지 걷는다 · sim.js 와 동일해야 하므로 엔진은 못 바꾼다) ⓑ 연출(사망 표시를 Strike 큐가 미루는 동안 엔진은 이미 다음 적으로 걷는다 · 죽은 적이 Dead 루프로 서 있다 · 화면 밖 적) 중 어느 것인지. 고치는 쪽은 **연출**이다: 플레이어가 걷기 시작하는 순간 화면에 «살아 보이는» 적이 남아 있으면 안 된다(사망 연출을 즉시 시작하거나, Walk 전환을 사망 연출이 끝날 때까지 미룬다 — 엔진 좌표는 그대로 두고 표시만).
2. HUD 왼쪽 버프 바(발동 중 스택)의 칸을 `ui.buffSlot`(엉뚱한 프레임) 대신 **특전과 같은 팔각 `ItemFrame_04_*`(`UiKit.PerkFrame` · 등급색)** 로. 스택 수 글자는 그대로 오른쪽 아래.
3. 게이트 + PROGRESS T20 행.

### T21 — 투사체(도끼·화살·창·번개) 표적 = 웨이브 안 무작위 적 (번호 정정: 구 T14)
범위: `Assets/Scripts/Core/Battle.cs`(FireAxe/FireArrows/FireSpear/FireBolts · RandTarget) · `Assets/Scripts/Game/BattleWorld.cs`(투사체 그리기 — T20·T19 와 겹친다)
순서: **T20·T19 완료 뒤**.
1. 먼저 aaaw `sim.js` 의 `randTarget`/`fireAxe`/`fireArrows`/`fireSpear`/`fireBolts` 를 읽고 **원본이 무엇을 표적으로 삼는지** 적는다(무작위 범위 −30~540 · 관통 등). 우리 엔진이 원본과 다르면 엔진을 원본에 맞춘다(시드 골든 BattleTests 가 통과해야 한다 — 골든이 깨지면 엔진이 아니라 내가 틀린 것).
2. 엔진이 원본과 같은데도 화면에서 «맨 앞 적만 맞는 것처럼» 보이면 그리기 문제다: 투사체를 `pr.Target` 의 실제 x 로 날리고 적중 이펙트도 그 적 위치에.
3. 원본 자체가 «맨 앞만» 이라면 주인이 원하는 «웨이브 안 무작위» 는 규칙 변경이라 **승인 대기 27** 로 올리고(골든 재생성 필요) 기본값은 원본 유지.
4. 게이트 + PROGRESS T21 행.

### T22 — 모든 버튼에 눌림 표시 (번호 정정: 구 T15)
범위: `Assets/Scripts/Game/UiKit.cs`(Clickable · Button) · `Screens.cs`(NavBar 탭)
순서: 제약 없음(T10 이 Screens.cs 를 만지면 NavBar 부분은 T10 뒤).
1. `UiKit.Clickable` 로 만드는 **모든** 버튼(프리팹 버튼·탭·카드·칸)에 눌림 피드백: `Button.transition = ColorTint`(pressedColor 는 어둡게 ≈ ×0.8 · highlighted 는 그대로) + 이미 있는 DOPunchScale. targetGraphic 이 투명 히트 영역이면(칸·카드) 자식의 첫 Image 를 targetGraphic 으로 잡거나 CanvasGroup alpha 로 눌림을 보여 준다. 비활성(interactable=false)은 지금처럼 반투명.
2. 하단 탭(NavBar)의 «현재 탭» 강조는 그대로 두고 눌림만 추가.
3. 게이트 + PROGRESS T22 행.

### T23 — 쉼터 «광고 보고 둘 다 얻기» · 클리어 팝업 = 골드만 + «광고 보고 보상 ×2 받기» (번호 정정: 구 T16)
범위: `Assets/Scripts/Game/Overlay.cs`(Rest · Clear) · `Assets/Scripts/Core/Battle.cs`(`ResolveRest` 에 «둘 다» 경로 1개 — 대화형 전용 · SimPolicy 는 절대 고르지 않으므로 시드 골든 불변) · `BattleScreen.cs`(EndRun 보상)
순서: T21 뒤(Battle.cs 공유).
1. **쉼터**: 기존 두 버튼(체력 회복 / 경험치) 아래에 **«광고 보고 둘 다 얻기»**(`ui.btnOrange` · 광고 카운트다운은 천사의 `AdCountdown` 재사용) → `G.ResolveRest(both)` = 회복 + 경험치 둘 다. 엔진에 `ResolveRestBoth()` 를 추가하되 `ResolveRest(bool)` 은 그대로.
2. **클리어 팝업(Play_Result_Win_01)**: 보상 표시는 **골드만**(프리팹의 다른 두 보상 칸은 끈다). 버튼은 «다음 챕터» 대신 **«광고 보고 보상 ×2 받기»**(광고 카운트다운 뒤 클리어 골드를 2배로 지급하고 로비로). 그 아래 작은 글자 버튼 **«그냥 받기»**(1배 · 로비로) 를 둔다 — 광고를 안 보면 못 나가는 것을 막기 위한 기본값(승인 대기 28). «다음 챕터» 진입은 로비의 챕터 화살표로.
3. 게이트 + PROGRESS T23 행 + 승인 대기 28.

### T17 — 장비 아이콘 마무리 (투구·갑옷·무기 크기 · 무기 45° · 근접 무기만) (T7 뒤) 🔄 진행중 (코드 `6918f71` · CI #41 대기 · PROGRESS 참조)
범위: `Assets/Scripts/Game/GearUi.cs`(Cell 아이콘) · `GearScreen.cs`(슬롯 아이콘) · `GearLook` 표(T7 이 만든 것) · catalog
순서: **T7 완료 뒤**(같은 파일·표).
1. **주인 지적 «투구·갑옷·무기 아이콘만 작다»** — CharacterMaker 파츠 스프라이트는 GUI Pro 아이콘(128px)보다 작아 같은 칸에서 작게 보인다. 파츠 아이콘은 칸 안에서 다른 부위 아이콘과 **같은 시각 크기**로(스프라이트 bounds 기준으로 칸의 70~75% 를 채우도록 스케일 · preserveAspect). 장착 슬롯·인벤 칸·세부 팝업·대장간·뽑기 결과 전부.
2. **무기 아이콘은 45° 로 오른쪽 위를 향하게**(RectTransform rotation z = 45 · 칼끝이 오른쪽 위). 전투 캐릭터 손의 무기는 그대로.
3. **무기는 전부 근접 무기** — 활·지팡이·완드 계열 금지. `GearLook` 표에서 무기 종류 × 등급을 **검(Sword)·방망이(Blunt)** 두 계열의 파츠로만 채운다(등급이 오를수록 화려한 것). 카탈로그 키·`docs/assets-map.md` 갱신.
4. 게이트 + PROGRESS T17 행.

### T18 — 배속(x2) 기억
범위: `Assets/Scripts/Game/BattleScreen.cs`(_speed) · `SaveStore.cs`/`SaveData`(필드 1개)
순서: 제약 없음.
1. 배속 버튼 값(x1/x2)을 세이브(`SaveData.Speed` · PlayerPrefs)에 저장하고, 전투 시작 시 그 값으로 시작한다(«2배속으로 하다가 클리어 뒤 다른 챕터 도전하면 다시 1배속» 이 안 되게). index.html `kkoma-knight-v2` 에 없는 필드라 세이브 호환은 «없으면 1».
2. 게이트 + PROGRESS T18 행.

### T19 — 전투 맵 = 데모 씬 «그림» 그대로 (T20 뒤 · 주인 재지적)
범위: `Assets/Scripts/Game/BattleWorld.cs`(BuildGround/BuildProps) · `tools/gen_maps.py` · `Assets/Scripts/Game/MapLayouts.cs`(재생성)
순서: **T20 완료 뒤**(BattleWorld 공유). T21 보다 먼저.
> 주인(2026-09-06): «맵 디자인을 DemoScene_Autumn/DeepForest/Desert/Forest 씬에 있는 거 그대로 가져와서 쓰라니까 안 쓰네? 지금 맵이 디자인 존나 다르던데». 씬을 그림으로 보려면 `python3 tools/demo_render/render_demo_scene.py <폴더>` + `node tools/demo_render/shot.js <폴더>`(Playwright · PNG 는 커밋 금지). 데모 씬의 모습: 평면색 들판 화면 전체 · 가운데 **두꺼운 길 띠(2.46u · 화면 높이의 1/4)** · 길 **위·아래 양쪽** 가장자리에 물결 풀 경계 · 길 위쪽 들판과 아래쪽 들판 모두에 나무·돌·통·꽃이 **빽빽하게**(17.8u 폭 화면에 30~60개).
1. **왜 다른가를 먼저 적는다**(PROGRESS 한 줄). 지금 코드가 틀린 점: ⓐ 물결 경계(Road_up)를 길 **아래쪽만** 깔았다 — 씬에는 위·아래 양쪽(세로 반전 인스턴스 · `m_LocalScale.y = -1`)이다 ⓑ 세로 잘림 — 우리 화면에서 월드가 보이는 띠는 HUD 위(17%)~HUD 패널(69.5%) = 6u 인데 데모는 10u 를 보여 준다 → 데모 y 범위 −5~+5 의 소품 대부분이 잘리거나 HUD 뒤에 숨는다 ⓒ 가로도 5.4u 슬라이스라 한 화면에 소품이 2~4개뿐 → «휑하다».
2. 고치는 법: **데모 구성을 통째로 0.6배**(`Layout.MapScale = 0.6f`)로 그린다 — 길 띠 2.46u→1.48u(발 줄 40% 을 품게 중심 41%), 소품 위치·크기 모두 ×0.6 (씬 폭 27u→16u · 우리 5.4u 창에 데모 화면의 1/3 이 보인다 = 데모 창(17.8u)이 씬의 2/3 을 보이는 것과 같은 밀도). 캐릭터 키는 T14 의 2/3 배율 그대로(0.69u · 길 띠 1.48u 안에 서면 데모의 샘플 캐릭터 비율과 비슷).
3. `tools/gen_maps.py`: Road_up 을 특수 처리하지 말고 **다른 소품과 똑같이 인스턴스 그대로**(x·y·sx·sy · 세로 반전 포함) 표에 넣는다 → 위·아래 양쪽 물결이 씬대로 나온다. 씬 폭 반복은 그대로.
4. 정렬: 소품의 뿌리 y 가 발 줄보다 위면 캐릭터 뒤, 아래면 앞(지금 규칙) — 길 띠 안(캐릭터 줄)에는 소품이 없다.
5. 검증: 워커는 WebGL 을 못 돌리므로, `tools/demo_render` 의 방식으로 **우리 배치 표(MapLayouts × 0.6 · 5.4u 창)** 를 같은 HTML 로 그려 데모 그림과 나란히 보고(PNG 커밋 금지) PROGRESS 에 «같다/다른 점» 한 줄. 대화형 세션이 gh-pages 스크린샷으로 최종 확인한다.
6. 게이트 + PROGRESS T19 행.

### T24 — 대장간: 장착 중 장비도 합성 재료 (T8 뒤 · 주인 2026-09-06)
범위: `Assets/Scripts/Game/ForgeScreen.cs` · `GearUi.cs`(Cell 흐림 옵션) · `Assets/Scripts/Core/GearSystem.cs`(FuseAll 장착 제외 인자)
순서: 제약 없음(T8 ✅ · T17 이 GearUi 아이콘 부분을 만지므로 Cell 의 «흐림» 한 줄만 조심).
1. 주인 «대장간에 장착중인 거도 합성 가능하게». `ForgeScreen.Toggle` 의 장착분 거부(토스트)·흐림을 없애고 장착중 배지(Check)는 유지. «자동» 도 장착분을 포함해 합성(`FuseAll` 호출에서 `EquippedSet` 제외를 뺀다 — 함수 시그니처는 두고 빈 집합을 넘겨도 된다).
2. 장착 중이던 것이 재료로 사라지면: 결과물이 **같은 부위면 그 슬롯에 장착**, 아니면 슬롯 비움(승인 대기 29 기본값). 세이브 갱신·전투력 재계산·외형(GearLook) 갱신까지.
3. aaaw T125(«장착분은 재료가 아니다»)는 주인이 뒤집었다 — PROGRESS 한 줄. UiSmokeTests ③ 의 «장착분 재료 불가» 가정이 있으면 새 규칙으로 고친다.
4. 게이트 + PROGRESS T24 행.

### T25 — 장비 화면: «상점»·«합성» 버튼을 공격력·체력·실드 3칸 바로 아래로 + 캐릭터 크기 (T7 뒤 · 주인 2026-09-06)
범위: `Assets/Scripts/Game/GearScreen.cs` · `HeroView.cs`(렌더 카메라 크기)
순서: **T17 완료 뒤**(GearScreen 공유).
0. **캐릭터가 너무 작다**(주인). Character_Hero_Equipment 프리팹의 샘플 캐릭터(Sample_Cha02_l 자리)가 차지하는 **RectTransform 크기 그대로** HeroView 의 RawImage 를 맞추고, 렌더 카메라의 orthographicSize 를 캐릭터(Character.prefab 키 0.85u)가 그 사각형의 세로 **85~90%** 를 채우도록 잡는다(발이 사각형 아래에서 5% 위 · 머리·투구 위 여백 5%). 로비 초상(T6 의 HeroView 재사용)은 자기 사각형 기준으로 같은 규칙(따로 배율 인자). 근거 수치(프리팹 사각형 px · 카메라 size)를 PROGRESS 에 한 줄.
1. 주인 «장비 부분에서 상점·합성, 공격력·체력·실드 표시하는 곳 밑에 표시되게». 지금 자리(화면 아래·탭바 위)에서 **스탯 3칸 줄 바로 아래**로 옮긴다. 프리팹(Character_Hero_Equipment)에 그 자리 버튼 줄이 있으면 그것을 쓰고, 없으면 `ui.btnGray`(상점)·`ui.btnOrange`(합성 N) 2개를 스탯 줄 밑에 같은 폭으로 나란히. 인벤 격자는 그 아래부터 시작(높이 줄어든 만큼 스크롤).
2. 게이트 + PROGRESS T25 행.

### T26 — 뽑기 확률 검증 (주인 «확률에 안 맞게 뽑히는 것 같다»)
범위: `Assets/Scripts/Core/GearSystem.cs`(뽑기 · 천장) · `Assets/Tests/EditMode/GearTests.cs`(통계 테스트 추가) · `Assets/Scripts/Game/ShopScreen.cs`(표시 문구가 실제 확률과 같은지)
순서: 제약 없음.
1. 먼저 **원본**을 읽는다: aaaw `index.html` 의 뽑기(`pull`/`rollRar`/천장 `pity`)와 `data/gacha.json` 의 상자별 등급 확률·천장. 우리 `GearSystem` 의 굴림이 원본과 **같은 순서·같은 난수 소비**인지 줄 단위로 대조.
2. **통계 테스트**(EditMode · dotnet 도 돌린다): 상자마다 10,000회 뽑아 등급 분포가 gacha.json 확률의 ±1.5%p 안인지 · 천장(전설/신화 확정 회차)이 정확히 그 회차에 발동하는지 · 10회 뽑기가 1회 뽑기 10번과 같은 분포인지. 난수는 `Mulberry32` 시드 고정.
3. 어긋나면 **코드**를 고친다(gacha.json 은 aaaw 정본 · 손대지 않는다). 흔한 원인: 등급 순서(신화→전설→…)와 누적 확률 순서 불일치 · 천장 카운터가 상자별이 아니라 공용 · 0% 등급 처리 · 10회 뽑기의 «최소 희귀 보장» 유무(원본대로).
4. 상점 카드의 확률 문구(«전설 N% …»)가 실제 굴림과 같은 수치인지 확인.
5. 게이트 + PROGRESS T26 행 + 원인 한 줄(정말 어긋났는지, 아니면 표본이 작아 그렇게 보인 것인지 — 후자면 그렇게 적는다).

### T27 — 장비 정보 팝업 = Character_Hero_Item_Detail_01 그대로 (주인 재지적)
범위: `Assets/Scripts/Game/GearUi.cs`(OpenDetail · OpenSlot) · catalog(`ui.itemDetail`)
순서: **T17 완료 뒤**(GearUi 공유). T25 와는 파일이 다르다.
1. 주인 «장비 정보 팝업이 Character_Hero_Item_Detail_01 을 써야 하는데 안 쓰는 듯». 지금 `OpenDetail` 이 `ui.itemDetail` 을 스폰하는지, 스폰하더라도 내부 요소를 옮기거나 우리 상자(Popup_Box)로 감싸 프리팹 모양이 사라졌는지 확인한다. **프리팹을 원형 그대로**(어둠 + 프리팹 루트 스트레치 · 내부 요소 Pct 이동 금지) 두고 글자·아이콘·등급색·버튼 문구만 바꾼다. 세부 정보(이름·부위·세트·슬롯 Lv·기여 3수치·세트 옵션·장착/해제/슬롯 강화)는 프리팹의 해당 자리에.
2. 빈 슬롯 팝업(OpenSlot)도 같은 프리팹(장비 없는 상태 · 강화만).
3. UiSmokeTests ② 가 «세부 팝업 = Character_Hero_Item_Detail_01» 인지 프리팹 이름으로 단언하도록 보강.
4. 게이트 + PROGRESS T27 행 + «주인이 확인할 것».

### T28 — 배경음(BGM) · 효과음(SFX) (주인 2026-09-06 «인터넷에서 받아서 넣어라»)
범위: `Assets/Audio/`(신규 · 오디오 파일 + `LICENSES.md`) · `Assets/Scripts/Game/Audio.cs`(신규 · AudioManager) · 각 화면/팝업의 호출 한 줄씩(Screens · BattleScreen · BattleWorld(타격) · Overlay · GearUi · ShopScreen · ForgeScreen) · `SaveStore`(음소거 2개) · catalog(`bgm.*`/`snd.*`)
순서: 제약 없음(호출은 한 줄씩이라 다른 작업과 겹쳐도 rebase 로 풀린다 — 충돌 나면 내 줄만 다시).
1. **에셋 구하기**: 이 환경의 프록시는 kenney.nl·opengameart.org·freesound.org 를 막는다(디스패처가 확인 · 000). **GitHub 는 열려 있다** → `git clone --depth 1` 로 받을 수 있는 **CC0/퍼블릭 도메인** 팩만 쓴다(예: Kenney 의 GitHub 미러 · OpenGameArt CC0 모음 미러 · «cc0 game sfx» 검색). 라이선스가 CC0/PD 가 아니면 쓰지 않는다. 받은 파일의 출처·라이선스를 `Assets/Audio/LICENSES.md` 에 한 줄씩(URL · 원작자 · 라이선스). **GitHub 에서도 못 구하면** 오디오 시스템(2~4)만 만들고 `Assets/Audio/README.md` 에 «주인이 파일을 이 폴더에 넣으면 catalog 한 줄로 붙는다» 를 적고 PROGRESS 승인 대기 30 에 등재.
2. **바이너리 예외**(§1 «대용량 바이너리 금지» 의 두 번째 예외 · 주인 지시): 형식 OGG(Vorbis) · 파일당 ≤ 300KB · BGM 은 ≤ 1MB · **합계 ≤ 5MB**. WAV 는 ffmpeg 로 OGG 변환(`ffmpeg -i in.wav -c:a libvorbis -q:a 3 out.ogg`). `.meta` 는 gen_meta 로.
3. **필요한 소리**(카탈로그 키): BGM = `bgm.lobby` · `bgm.battle`(맵 4종 공용 1곡 · 있으면 테마별 4곡) · `bgm.boss`. SFX = `snd.click`(모든 버튼 · UiKit.Clickable 에서 한 곳) · `snd.hit` · `snd.crit` · `snd.miss` · `snd.kill` · `snd.hurt`(플레이어 피격) · `snd.levelup` · `snd.perk`(특전 선택) · `snd.coin`(골드 획득) · `snd.popup`(팝업 열림) · `snd.gacha`(상자 열림) · `snd.fuse`(합성) · `snd.equip` · `snd.clear` · `snd.fail` · `snd.arrow`/`snd.axe`(투사체 · 없으면 hit 재사용).
4. **AudioManager**(`Audio.cs` · App 이 만든다 · AudioSource 2개: BGM 루프 1 + SFX 풀): `Audio.Bgm(key)`(같은 곡이면 무시 · 0.5초 크로스페이드) · `Audio.Sfx(key, volume=1, pitchJitter=0.05)` · 화면 전환 시 로비/전투 곡 자동 교체 · 보스 등장(BossWarn)에서 boss 곡 · 배속 x2 여도 피치 그대로. **설정 팝업(Settings 프리팹)의 BGM/SFX 스위치**에 각각 연결(`Save.MuteBgm`·`Save.MuteSfx` — 기존 `Muted` 는 BGM 으로 이관 · 세이브 호환). WebGL 은 첫 터치 뒤에 소리가 난다(브라우저 정책) — START 버튼 클릭에서 AudioContext 를 깨우는 코드 한 줄.
5. UiSmokeTests 에 «BGM 키가 화면마다 바뀌는가 · SFX 호출이 예외 없이 도는가(클립 없어도 경고만)» 를 추가. 클립이 없을 때는 조용히 넘어간다(에러 0).
6. 게이트 + PROGRESS T28 행 + «주인이 확인할 것»(어떤 팩을 어디서 받았는지 표).

### T29 — 설정 팝업에 «데이터 삭제» (주인 2026-09-06)
범위: `Assets/Scripts/Game/Overlay.cs`(Settings 팝업) · `SaveStore.cs`(Reset)
순서: 제약 없음(T28 이 Settings 의 BGM/SFX 스위치 줄을 만진다 — 그 줄만 피한다).
1. Settings 프리팹(그대로 원칙)의 버튼 중 하나를 «데이터 삭제» 로 쓴다(프리팹에 남는 버튼이 없으면 프리팹 안 버튼 줄 아래에 `ui.btnRed` 1개 — 유일한 추가). 누르면 **확인 팝업**(Popup_Box + «정말 삭제할까요? 장비·골드·보석·진행이 모두 사라집니다» · «삭제»(빨강) / «취소»). «삭제» 는 `SaveStore.Reset()`(PlayerPrefs 의 세이브 키 삭제 → 새 세이브 생성 · 배속·음소거 같은 설정값도 초기화) 뒤 로비로 돌아가 화면을 새로 그린다(전투 중이면 전투를 끝내고).
2. UiSmokeTests ① 에 «데이터 삭제 → 확인 → 세이브가 초기값(골드 0 · 장비 0 · 챕터 1)» 단언 추가.
3. 게이트 + PROGRESS T29 행.

### T30 — 하단 탭 «탤런트» → «던전» (World_Dungeon_List · 각 항목 → World_Dungeon_Start1/2) (주인 2026-09-06)
범위: `Assets/Scripts/Game/Screens.cs`(NavBar 탭 · 던전 팝업 진입) · `Overlay.cs`(던전 팝업 2단) · catalog(`ui.dungeonList` = World_Dungeon_List · `ui.dungeonStart1` = World_Dungeon_Start1 · `ui.dungeonStart2` = World_Dungeon_Start2)
순서: 제약 없음(T10 ✅ · T22 가 NavBar 눌림을 만지면 rebase).
1. 탭 5칸을 **상점 · 장비 · 전투 · 던전 · 펫** 으로(«탤런트» 탭·Character_Talent_02 팝업 진입 제거 · 펫은 그대로). 탭 아이콘은 GUI Pro 던전 아이콘.
2. «던전» = `World_Dungeon_List` 프리팹 팝업 **그대로**(요소 이동·삭제 금지 · 제목만 «던전»). 목록의 각 항목 «입장(Enter)» 클릭 → 항목별로 `World_Dungeon_Start1` 또는 `World_Dungeon_Start2` 프리팹 팝업 **그대로**(첫 항목 Start1 · 둘째 Start2 · 그 뒤는 번갈아). 기능은 없다 — 팝업 열고 닫기만(«시작» 버튼은 누르면 팝업이 닫힌다 · 토스트 «준비 중»). 닫기(X) 로 목록으로, 목록의 X 로 원래 화면으로.
3. UiSmokeTests ① 의 «탤런트» 단언을 «던전 목록 → Start1 → 닫기 → 둘째 → Start2 → 닫기» 로 바꾼다.
4. 게이트 + PROGRESS T30 행.

### T31 — 장비 아이콘 = CharacterMaker «Thumbnail» 그림 (입는 파츠와 아이콘을 분리 · 주인 2026-09-06) (T17 뒤)
범위: `GearLook` 표 · `Assets/Scripts/Game/GearUi.cs`(아이콘 키) · catalog(`cmi.*` 신규 키)
순서: **T17 완료 뒤**(같은 표·파일). T25·T27 과는 줄이 다르다.
1. CharacterMaker 팩은 파츠마다 **입는 그림**(`Extenstions/Parts Pack Base/Parts/<부위>/<이름>.png`)과 **아이콘용 그림**(`…/Thumbnail/<부위>/<같은 이름>.png` · Helmet·Chest·Sword·Blunt·Axe·Spear·Bow·Shield 등)이 따로 있다. 주인: «막상 장착 아이콘용 갑옷이랑 실제 입는 갑옷이 다르게 돼 있는데 내 게임도 그렇게». → `GearLook` 표의 각 항목에 **아이콘 키(Thumbnail)** 와 **착용 키(Parts)** 를 둘 다 두고, 장비 칸·슬롯·세부 팝업·뽑기 결과·대장간의 아이콘은 **Thumbnail** 을, 캐릭터(HeroView·전투)는 **Parts** 를 쓴다. 카탈로그 키는 `cmi.<part>.<name>`(Thumbnail) / 기존 `cm.*`(Parts). 같은 이름이 Thumbnail 에 없으면 PROGRESS 에 목록으로 남기고 Parts 그림을 임시로.
2. T17 의 «파츠 아이콘 크기 통일 · 무기 45°» 는 Thumbnail 기준으로 다시 맞춘다(Thumbnail 은 정사각 아이콘이라 대개 회전이 필요 없다 — 무기 Thumbnail 이 이미 기울어져 있으면 45° 회전을 뺀다 · 주인 «45° 오른쪽 위» 는 그림이 그렇게 보이면 된다는 뜻).
3. `docs/assets-map.md` 갱신(gen_catalog).
4. 게이트 + PROGRESS T31 행 + «주인이 확인할 것».

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
