# PROGRESS — 꼬마기사 키우기 유니티 이식 (aaawunity)

> 갱신 규약은 `docs/ROUTINE.md` §4. 스펙은 aaaw `PLAN.md`(읽기 전용 · 변경 금지). 수치 정본은 aaaw `data/*.json`.

## 작업 상태

| ID | 작업 | 상태 | SID / 워커 | 범위 | 핵심 |
|---|---|---|---|---|---|
| T1 | 프로젝트 뼈대 + JSON 로더 + CI/활성화 워크플로 + README + 운영 문서 | ✅ 완료 | sess-1516-port / 착수 세션 | 전체 뼈대 | dotnet build 0 경고 0 오류 · 순수 C# 테스트 21/21 · 레이아웃/적 스탯 420챕터 전수 = JSON 과 일치 (mulberry32 비트 동일) |
| T2 | 전투 엔진(순수 C#) + 시드 11·12·13 이식 검증 | 미착수 | — | Core/Battle*·Perks*·tools/sim | sim.js 실험1 ±2%p |
| T3 | 레벨업 3택 + 악마의 거래 (유니티 팝업) | 미착수 (T2 뒤) | — | Game/Battle*·Overlay*·Hud* | 팝업 중 시간 정지 |
| T4 | 로비 · 장비 · 강화 · 슬롯 · 뽑기 상자 3종 | 미착수 | — | Game/Lobby*·Gear*·Forge*·Shop*·Save* | 자동 장착 없음 · 상자 3종 |
| T5 | UI 를 docs/ref 레이아웃에 맞추기 | 미착수 (T3·T4 뒤) | — | Game/Layout* | ref-layout.md ±3%p |

### T1 완료 기록 (2026-09-05 · 착수 세션)

- **주인이 확인할 것 (한 줄)**: Actions 탭에서 CI 가 시크릿 없이 초록인지 + README «내가 할 일» 절차대로 `.alf` 워크플로를 한 번 돌려 볼 것.
- 만든 것
  - `ProjectSettings/`(2022.3.76f1 · 세로 고정 · Android IL2CPP/ARM64 · WebGL 압축 끔) · `Packages/manifest.json`(ugui · test-framework · 필요 모듈만).
  - `Assets/Scripts/Core`(순수 C# · `noEngineReferences`): `MiniJson`(파서) · `GameData`(7개 JSON 타입 로더) · `Rng`(mulberry32 이식 · `IRng`) · `ChapterLayout`(sim.js `chapterLayout`/`enemyStats` 이식 — JSON 대조용) · `GearSystem`(buildPower · gachaPull · fuseMake/fuseAll · autoEquip).
  - `Assets/Scripts/Game`: `DataLoader`(StreamingAssets · WebGL/Android 는 UnityWebRequest) · `Bootstrap` · `UiKit`.
  - `Assets/StreamingAssets/data/*.json` = aaaw main `c7ebe37`(data blob `6e13114`) 복사본. `Assets/Fonts/Jua-Regular.ttf`(OFL).
  - 테스트 21개(EditMode · 유니티 없이도 `dotnet test` 로 돈다) + PlayMode 1개(StreamingAssets 실제 로드).
  - `tools/dotnet/`(sln + Core/Game/Tests/Sim csproj · UnityEngine 참조 어셈블리 = NuGet `UnityEngine.Modules 2021.3.33` + `Unity3D.UnityEngine.UI 2020.3.21`) · `tools/gen_meta.py` · `tools/check_data_sync.sh`.
  - `.github/workflows/activation.yml`(.alf → Artifact) · `ci.yml`(dotnet 항상 · 유니티 단계는 시크릿 게이트 · main push 시 WebGL→gh-pages + Android APK).
  - `docs/ROUTINE.md`(T2~T5 등재) · `docs/claims/README.md` · 이 문서.
- 게이트: `dotnet build` 0/0 · `dotnet test` 21/21 · `gen_meta --check` 초록 · `check_data_sync.sh` OK(aaaw c7ebe37).

## 주인 승인 대기 (한 번에 답해 주시면 됩니다 — 답이 없으면 아래 «기본값» 으로 진행)

1. **유니티 버전 = 2022.3.76f1.** «2022.3 LTS 최신 패치» 를 확인하려 했으나 이 환경에서 unity.com 이 막혀 있어, GameCI(`unityci/editor`) 이미지가 있는 2022.3 계열 중 가장 높은 76f1 을 골랐다(2026-05-15 이미지). 더 높은 패치가 있으면 `ProjectSettings/ProjectVersion.txt` 한 줄 + README 한 줄만 바꾼다.
2. **브랜치.** 주인 지시는 «각 단계마다 main 에 커밋·푸시», 세션 시스템 지시는 «`claude/aaawunity-port-71kij4` 브랜치에서 개발». 기본값: **둘 다** 푸시한다(같은 커밋). main 만 원하시면 한 줄로.
3. **활성화 워크플로.** `game-ci/unity-request-activation-file` 이 공식 폐기(deprecated)돼서, 같은 일(`unity-editor -createManualActivationFile`)을 `unityci/editor` 도커 이미지에서 직접 한다. 절차·결과물(.alf Artifact)은 같다.
4. **폰트.** PLAN §2.1 의 Jua(Google Fonts · OFL)를 `Assets/Fonts/Jua-Regular.ttf`(2.1MB) 로 커밋했다 — WebGL/Android 는 OS 한글 폰트를 못 쓰므로 필요하다. «대용량 바이너리 금지» 의 유일한 예외.
5. **Android 빌드 = IL2CPP · ARM64 · 서명 없는 개발용 APK.** 스토어 배포용 keystore 는 시크릿이 필요하니 그때 정한다.
6. **WebGL 압축 끔**(GitHub Pages 가 .br/.gz 헤더를 안 붙여 주므로). 로딩이 느리면 gzip+폴백으로 바꿀 수 있다.
7. **UnityEngine 참조 어셈블리 = NuGet 2021.3.33**(2022.3 용 패키지가 NuGet 에 없다). 이 게임이 쓰는 API(uGUI·Sprite·UnityWebRequest)는 2021.3 과 2022.3 이 같다. 유니티 실제 컴파일은 CI 의 EditMode 단계가 최종 확인한다.
8. **`expNeed` 표 밖 레벨**: tune.json `expNeedTable` 은 1~30레벨이라, 그 위는 표의 등차(+5)로 연장한다(공식 `5*lv+1` 을 코드에 박지 않으려는 기본값 · 실제로는 한 판 9레벨이 상한이라 닿지 않는다).
9. **enemies.json 의 보스 스탯은 실수 그대로**(`sim.js` 는 반올림 안 함 · `index.html` 은 반올림). 시드 검증 대상이 sim.js 이므로 엔진은 **반올림 안 함**을 따른다(체력바 표시만 정수).

## 주인 할 일

- README «내가(주인) 할 일» 5단계 (활성화 워크플로 → .alf → .ulf → 시크릿 3개 → Pages 소스 gh-pages).

## 게이트 현황 스냅샷 — T1 완료 직후

| 게이트 | 결과 |
|---|---|
| `dotnet build tools/dotnet/KkomaKnight.sln -c Release` | 0 경고 · 0 오류 (Core · Game · Tests · Sim) |
| `dotnet test tools/dotnet/Tests` | 21/21 |
| `python3 tools/gen_meta.py --check` | 초록 |
| `tools/check_data_sync.sh` | OK — aaaw main `c7ebe37` 과 동일 (`sim.js@0618225…`) |
