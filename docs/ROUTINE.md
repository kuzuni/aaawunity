# 루틴 작업 지시서 (병렬 워커 공용 — 유일 지시서)

> 이 문서와 aaaw 의 `PLAN.md`(스펙 정본 · 읽기 전용) 만 보고 작업한다. 보고·기록은 전부 **한국어**.
> 이 레포는 **aaaw 의 HTML 게임을 유니티로 이식**하는 곳이다. 게임 규칙·수치는 aaaw 가 정본이고 여기서는 **바꾸지 않는다**.

## ⚑ 신규 주인 지시 (위 항목이 최신)

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
3. aaaw `PLAN.md`(스펙) → 이 문서 → `docs/PROGRESS.md` → `docs/claims/` 순서로 읽는다. aaaw 는 `git clone --depth 1 https://github.com/kuzuni/aaaw .aaaw-src` 로 옆에 둔다(커밋 금지 폴더 · .gitignore).
4. [2. 작업 목록]에서 **선점 가능한 가장 앞 작업**을 lock 으로 선점한다 (규약: `docs/claims/README.md`).
5. 선점할 작업이 없으면(전부 lock 또는 전부 완료): 게이트(§3)를 재실행해 검증만 하고, 이상 없으면 **커밋 없이 조용히 종료**. 이상이 있으면 PROGRESS 에 등재하고 종료.

## 1. 절대 규칙

- **aaaw 레포 수정 금지.** 수치(`data/*.json`)는 `tools/check_data_sync.sh --sync` 로만 가져온다. JSON 을 손으로 고치지 않는다.
- **코드에 게임 수치를 직접 박지 않는다** — `KkomaKnight.Core.GameData` 에서 읽는다. 상수가 JSON 에 없으면 «주인 승인 대기» 에 등재하고, 그때까지는 aaaw 의 `tools/exportData.js` 에 축을 더하는 제안을 적는다(이 레포에서 임의 상수 금지).
- **새 콘텐츠(특전/시스템/수치 체계) 임의 추가 금지.** 원하면 PROGRESS «주인 승인 대기» 에 등재만.
- **커밋 전 게이트**: `dotnet build tools/dotnet/KkomaKnight.sln -c Release` 초록 · `dotnet test tools/dotnet/Tests` 초록 · `python3 tools/gen_meta.py --check` 초록. 새 에셋을 만들면 `python3 tools/gen_meta.py` 로 .meta 를 만든다(GUID 결정적).
- 전투 엔진(`Assets/Scripts/Core`)에는 `UnityEngine` 을 참조하지 않는다(asmdef `noEngineReferences: true` · dotnet 이 강제한다).
- 승인 프롬프트가 뜨는 명령·대화형 편집기(`git rebase -i` 등) 금지. 캡처 PNG·대용량 바이너리 커밋 금지(폰트 1개는 예외 — PLAN §2.1).
- 작업이 끝나면 lock 삭제 → PROGRESS 갱신 → 커밋 → push. **push 실패 시 `git fetch && git rebase origin/main`** 후 재push (자기 lock 이 사라졌으면 진 것 — 작업 버리고 종료).
- 브랜치는 `main` 하나다(주인 결정 — 다른 브랜치에 올리지 않는다). 각 단계가 끝나면 main 에 커밋·푸시하고 PROGRESS 에 «무엇을 확인하면 되는가» 한 줄을 적는다.
- **에셋은 주인 에셋만** (위 지시 ③). 코드 생성 도형·임시 그림 금지. 새 에셋을 쓰면 `docs/assets-map.md` 표에 «용도 · 경로 · GUID(·fileID)» 를 한 줄 추가한다.

## 2. 작업 목록 (순서 고정 — lock ID = 아래 번호)

> T1~T5(주인이 정한 5단계)는 끝났다. **지금 열린 작업은 T6~T11** — 같은 파일을 만지는 것은 아래 «순서» 대로(앞 번호의 lock 이 사라지고 PROGRESS 행이 ✅ 가 된 뒤에 잡는다). 겹치지 않는 것은 병렬 선점 가능.

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
4. 장비 **아이콘** = 같은 표의 파츠 스프라이트(투구·무기·갑옷). 목걸이·장갑·신발은 GUI Pro 아이콘 중 아무거나로 임시(«일단 아무거나») — 승인 대기에 «어떤 그림으로 할지» 등재.
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
5. **수치**: 다이아 6종의 **다이아 개수**와 골드 3종의 **다이아 가격**은 주인이 안 정했다 → `Assets/StreamingAssets/data/` 는 aaaw 동기 폴더라 못 넣는다. **`Assets/KkomaKnight/shop.json`**(이 레포 전용 · 승인 대기 25 의 기본값) 을 만들어 거기서 읽는다(코드 상수 금지 규칙의 예외가 아니라 «JSON 에서 읽기» 그대로). 기본값: 다이아 100·1,100·3,500·6,000·10,000·14,000 / 골드 1,000=다이아 30 · 3,000=80 · 10,000=250. 주인이 바꾸면 파일만 고친다.
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

### 신규 작업 등재
- 버그·후속 작업 발견 시 PROGRESS 표에 **이미 쓰인 번호 중 가장 큰 것 +1** 로 등재 (번호 재사용 금지, 한 번호 = 한 작업).

## 3. 게이트 (커밋 전 · 세션 종료 전)

```bash
dotnet build tools/dotnet/KkomaKnight.sln -c Release --nologo     # 컴파일
dotnet test tools/dotnet/Tests/KkomaKnight.Tests.csproj -c Release --no-build   # 순수 C# 테스트 (NUnit 3.6.1 = 유니티 포크와 같은 API 면 — Does.Contain(object) 같은 신형 API 금지)
python3 tools/gen_meta.py --check                                 # .meta 누락/고아
python3 tools/gen_catalog.py --check                              # catalog.json 의 에셋 경로가 전부 실재하는가 (에셋 키를 바꿨으면 --check 대신 그냥 실행해 재생성)
tools/check_data_sync.sh [.aaaw-src]                              # data ↔ aaaw main
dotnet run --project tools/dotnet/Sim -c Release -- --seeds 11,12,13  # (T2 이후) 이식 검증
```

## 4. PROGRESS.md 기록 규약

- 표의 자기 작업 행을 갱신: 상태(진행중/완료/대기) · SID · 워커 · 핵심 수치.
- 완료 시 반드시: 게이트 결과(테스트 수 · 빌드 초록) + 커밋 해시 + **«주인이 확인할 것» 한 줄**.
- 판단이 필요한 것은 «주인 승인 대기» 절에 번호를 붙여 모아 둔다(한 번에 묻기 위해). 기본값으로 진행했으면 그 기본값도 같이 적는다.
