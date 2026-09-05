# 꼬마기사 키우기 — 유니티 이식 (aaawunity)

[kuzuni/aaaw](https://github.com/kuzuni/aaaw) 의 HTML 게임 «꼬마기사 키우기» 를 **Unity(6000.3.8f1 · 주인 «기본» 프로젝트 기준)** 로 옮기는 레포.
2D · 모바일 세로(9:16) · 오토배틀 방치형. 규칙·수치의 **정본은 aaaw** 이고 이 레포는 그것을 읽어 실행한다.

| 항목 | 값 |
|---|---|
| 유니티 | **6000.3.8f1** (`ProjectSettings/ProjectVersion.txt` — 주인이 main 에 올린 «기본» 프로젝트(URP 2D · TMP · Input System · Layer Lab 등 에셋) 의 버전을 그대로 따른다) |
| 수치 | `Assets/StreamingAssets/data/*.json` = aaaw `data/` 복사본 (런타임 로드 · 손으로 고치지 않는다) |
| 스펙 | aaaw `PLAN.md` · UI 배치 기준 `docs/ref/*.jpg` + `docs/ui/ref-layout.md` |
| 코드 | `Assets/Scripts/Core` 순수 C# 엔진(UnityEngine 참조 0) · `Assets/Scripts/Game` MonoBehaviour · `Assets/Tests` EditMode/PlayMode |
| 검사 | `tools/dotnet` — 유니티 없이 `dotnet build`/`dotnet test` (UnityEngine 참조 어셈블리는 NuGet) |
| 운영 | `docs/ROUTINE.md`(작업 지시서) · `docs/PROGRESS.md`(진행표) · `docs/claims/`(lock) |

## 내가(주인) 할 일 — 한 번만

시크릿이 없으면 CI 는 dotnet 검사만 돌고 **초록**이다. 유니티 테스트·WebGL/Android 빌드를 켜려면:

1. **Actions → «Unity 라이선스 활성화 파일(.alf)» → Run workflow** (`.github/workflows/activation.yml`).
2. 끝나면 그 실행의 **Artifact `Unity_v6000.3.8f1.alf`** 를 내려받아 압축을 푼다.
3. <https://license.unity3d.com/manual> 에 로그인 → `.alf` 업로드 → **Personal** 선택 → `.ulf` 다운로드.
4. 레포 **Settings → Secrets and variables → Actions** 에 시크릿 3개 등록:
   - `UNITY_LICENSE` = `.ulf` 파일 **내용 전체**(텍스트를 그대로 붙인다)
   - `UNITY_EMAIL` = 유니티 계정 이메일
   - `UNITY_PASSWORD` = 유니티 계정 비밀번호
5. **Settings → Pages → Build and deployment → Source: «Deploy from a branch» → Branch `gh-pages` / `(root)`** → Save.
   (gh-pages 브랜치는 main 에 첫 push 가 빌드를 끝낸 뒤 생긴다. 주소: `https://kuzuni.github.io/aaawunity/`)

그 뒤부터:
- **PR·push 마다** dotnet 검사 + Unity EditMode/PlayMode 테스트.
- **main 에 push** 되면 WebGL 빌드 → `gh-pages` 배포(폰 브라우저로 확인) + **Android APK** 가 그 실행의 Artifact 로 올라간다.

## 로컬에서 확인 (유니티 없이)

```bash
dotnet build tools/dotnet/KkomaKnight.sln -c Release       # 컴파일 (Core · Game · Tests · Sim)
dotnet test  tools/dotnet/Tests/KkomaKnight.Tests.csproj   # EditMode 의 순수 C# 테스트
dotnet run --project tools/dotnet/Sim -c Release           # 이식 검증 하니스 (sim.js 실험1 재현)
tools/check_data_sync.sh [--sync]                          # data/*.json ↔ aaaw main 비교(·복사)
python3 tools/gen_meta.py [--check]                        # 새 에셋의 .meta 생성 / 누락 검사
```

## 구조

```
Assets/
  Scenes/Main.unity              Bootstrap 하나만 놓인 씬 (UI 는 전부 코드로 생성)
  Scripts/Core/                  순수 C#: MiniJson · GameData(JSON 로더) · Rng(mulberry32) · ChapterLayout · GearSystem · (2단계) Battle
  Scripts/Game/                  MonoBehaviour: DataLoader(StreamingAssets) · Bootstrap · UiKit · (3~5단계) 화면들
  StreamingAssets/data/*.json    aaaw data/ 복사본 — CI 가 aaaw main 과 비교한다
  Fonts/Jua-Regular.ttf          Google Fonts Jua (OFL) — PLAN §2.1 의 폰트
  Tests/EditMode, PlayMode
tools/dotnet/                    dotnet 검사 프로젝트 (Core/Game/Tests/Sim + sln)
tools/sim/                       C# 이식 검증 하니스 소스
tools/check_data_sync.sh · tools/gen_meta.py
.github/workflows/activation.yml · ci.yml
docs/ROUTINE.md · PROGRESS.md · claims/
```

## 규칙 (요약 — 상세는 docs/ROUTINE.md)

- aaaw 는 **읽기 전용**. 수치를 바꾸거나 밸런스를 조정하지 않는다. JSON 이 바뀌면 `tools/check_data_sync.sh --sync` 로 가져온다.
- 코드에 수치를 직접 박지 않는다 — `GameData` 에서 읽는다.
- 새 시스템·새 기능은 주인 승인 없이 추가하지 않는다.
- 커밋 전 `dotnet build` 가 초록이어야 한다.
