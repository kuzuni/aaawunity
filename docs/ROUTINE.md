# 루틴 작업 지시서 (병렬 워커 공용 — 유일 지시서)

> 이 문서와 aaaw 의 `PLAN.md`(스펙 정본 · 읽기 전용) 만 보고 작업한다. 보고·기록은 전부 **한국어**.
> 이 레포는 **aaaw 의 HTML 게임을 유니티로 이식**하는 곳이다. 게임 규칙·수치는 aaaw 가 정본이고 여기서는 **바꾸지 않는다**.

## ⚑ 신규 주인 지시 (위 항목이 최신)

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

> 주인이 정한 5단계다. 앞 단계의 산출물 위에 다음 단계가 얹힌다. T1·T2 는 끝났다 — **T3 과 T4 는 파일이 겹치지 않아 병렬 선점 가능**(«범위» 열 참조). T5 는 T3·T4 뒤.

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

### T4 — 로비 · 장비 · 강화 · 슬롯 · 뽑기 상자 3종
범위: `Assets/Scripts/Game/Screens.cs`(Lobby·Gear·Forge·Shop) · 새 파일은 `Gear*.cs` · `Forge*.cs` · `Shop*.cs` · `Assets/KkomaKnight/catalog.json`(키 추가)
> 방법(T3 에서 확립): 화면은 주인 지정 GUI Pro 데모 프리팹을 `UiKit.Spawn` 으로 세우고 자식 이름으로 글자/아이콘/버튼을 바꾼다(`docs/assets-map.md` · `/tmp` 덤프는 `python3 tools/…` 없이 prefab YAML 을 직접 읽는다). 장비 = **Character_Hero_Equipment** · 소환 결과 = **Shop_Chest_Open** · 장비 아이콘 = catalog `gi.<부위>.<세트>` · 등급 색 = `Palette.RarName`(gray/blue/yellow/plum) 의 ItemFrame_01_Normal_* 변형. 새 에셋 키는 catalog.json 에 추가하고 `python3 tools/gen_catalog.py` 로 재생성(assets-map 도 같이 갱신된다).
1. 세이브(PlayerPrefs 에 JSON — index.html `kkoma-knight-v2` 와 같은 필드: gold·gem·maxChapter·selChapter·inv·eq·slots·gachaBoxes·uid·freeDay).
2. 로비(T3 에서 Lobby_Default 로 뼈대 완료: 챕터 ◀▶ · START · 하단 5탭 상점·장비·전투·대장간·설정) — 남은 것: 최고 챕터/해금 표시 다듬기 · 장비 버튼.
3. 장비 탭(좌우 슬롯열 3+3 · 캐릭터 · 공/체/실 3칸 · 균등 보너스 · 합성 버튼 · 인벤 5열) · 세부 팝업(등급 배지·아이콘·이름·스탯·옵션 7줄 잠금 표시·슬롯 강화 비용·장착/해제) · 대장간(수동 3칸 + 자동 · 장착분 제외 · `FuseMake` 하나만).
4. 상점(무료 보급 2,500/일 · 모의 결제 12,000 · 상자 3종: 가격·확률·천장 문구는 `gacha.json` 에서 · 뽑기 결과 팝업 · 자동 장착 없음 · NEW 뱃지).

### T5 — UI 를 docs/ref 레이아웃에 맞추기
범위: `Assets/Scripts/Game/Layout*.cs` + 각 화면의 배치 상수
1. aaaw `docs/ui/ref-layout.md` 의 표(요소별 x/y/w/h · 프레임 %)를 배치의 단일 정본으로 코드에 옮긴다(9:19.5 레퍼런스 → 프레임 % 환산).
2. 화면마다 요소를 그 % 자리에 앵커링한다(±3%p). 색·폰트·그림체는 점수 밖 — 배치·비율·비례만.
3. 검증: 에디터 없이 되는 만큼 — 배치 상수가 표와 같은지 EditMode 테스트로 대조하고, 실물 확인은 WebGL 배포에서 주인이 폰으로 한다.

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
