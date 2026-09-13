# 치와와 UI 작업 세션 분담 및 재개 지점

2026-09-14 시작. 원본 요구사항은 `TODO.md`, `docs/TODO-ChihuahuaGameUI.md`다.
T519는 전체 통합 작업이며 아직 미완료다. T515~T518과 기존 장비 아트 27세트는 변경하지 않는다.

## 실행 환경

- Codex 클라우드: `6aa6d9313e808191bf2ed8eafc74a343` (`kuzuni/aaawunity`). PC 종료와 독립적으로 실행한다.
- 클라우드 작업은 각자 격리된 main 사본에서 변경안을 만든다. main 통합은 조정 세션 한 곳에서 순서대로 수행한다.
- 아트 제작 환경 및 실행 작업 링크는 확인한 뒤 이 문서에 기록한다. 로컬 작업/예약을 PC 종료 후 실행 가능한 것으로 취급하지 않는다.

## 코드 작업 분담

| 역할 | 소유 파일 | 결과 기록 |
|---|---|---|
| world | BattleWorld.cs, Overlay.cs, 해당 신규 전용 테스트 | `tools/game_ui_art/sessions/world.md` |
| events | EventsScreen.cs, Recipes.cs, LobbyPopups.cs, ShopScreen.cs, 해당 신규 전용 테스트 | `tools/game_ui_art/sessions/events.md` |
| shell | UiKit.cs, Palette.cs, Screens.cs, Profile.cs, SeasonPassScreen.cs, LoadingScreen.cs, 해당 신규 전용 테스트 | `tools/game_ui_art/sessions/shell.md` |
| integration | catalog.json, AssetCatalog.asset, assets-map.md, 공통 테스트, PROGRESS/ROUTINE/TODO | 이 문서 및 상세 TODO |

각 역할은 소유 파일만 수정한다. 다른 파일 변경이 필요하면 결과 기록에 정확한 수정안을 남긴다.
이미지 파일이 아직 없으면 카탈로그를 가짜 경로로 변경하거나 완료로 표시하지 않는다.
기존 아트로 안전하게 돌아가는 선택적 연결을 만들고, 필요한 최종 키/경로를 역할 기록에 남긴다.
게임 수치, 저장 인덱스, 플레이어 위치, UI 배치는 유지한다.

## 중단 및 네트워크 오류 복구

1. 완료한 작은 단위마다 산출물과 역할 기록을 저장한다. 전체 대화 대신 이 문서와 역할 기록에서 재개한다.
2. 기록에는 완료 파일, 검사 명령/결과, 미검증 항목, 다음 한 단계, 필요한 아트 키를 적는다.
3. 전송 결과가 불명확하면 작업 목록/원격 커밋을 먼저 확인한다. 같은 작업을 무조건 다시 만들거나 push하지 않는다.
4. 재시도는 제한적으로 하고 같은 오류가 계속되면 구체적인 오류와 재개 명령을 기록한다. 검사 생략을 통과로 쓰지 않는다.
5. 아트는 생성 직후 작업 폴더에 보존하고 SHA-256, 실제 알파, 프롬프트, 검수 상태를 기록한다.
6. 최종 통합 세션이 변경안을 순서대로 적용하고 메타/카탈로그/C#/Unity/실제 화면을 확인한 뒤 main을 갱신한다.

## 완료 조건

상세 TODO의 제작 및 실제 연결 체크리스트가 모두 충족되고 새 커밋의 검증 결과가 확인되어야 완료다.
세션 생성, 코드 변경안 작성, 이미지 저장만으로 전체 완료 처리하지 않는다.

## 실행 기록

- 재사용 후보 5개를 `Assets/Art/ChihuahuaGameUI/Commerce`, `Nodes`에 원본 바이트 그대로 복사하고 새 폴더/파일의 메타만 생성했다. **`hud.gem`만 실제 카탈로그 연결**했다. 닫힌 상자는 열림 버전이 준비된 뒤 함께 연결하며 쉼터는 world 결과 통합 때 연결한다.
- 로컬 검사: 체크포인트 7개 SHA-256 일치, PNG 모드 확인, 메타/카탈로그 생성물/키 검사 통과. C# 빌드 0경고/0오류, 순수 C# 테스트 **587/587 통과**. Unity EditMode/PlayMode와 화면 검증은 아직 미실행.
- 아트 제작용 ChatGPT Work 클라우드 작업 생성 여부는 사용자에게 질문했으며 아직 응답 전이다. 해당 작업을 만들었다고 가정하지 않는다.

- [월드 코드 작업](https://chatgpt.com/codex/tasks/task_e_6aa6d9c05e408329822cc510138c0f8f): 제출 확인, pending.
- [이벤트 코드 작업](https://chatgpt.com/codex/tasks/task_e_6aa6d9cddb548329a987c7ba96a950f0): 제출 확인, pending.
- [로비·공통 UI 코드 작업](https://chatgpt.com/codex/tasks/task_e_6aa6d9d441c08329ab4215fa6e7d1fdd): 제출 확인, pending.
- 각 작업의 원문 요청은 `tools/game_ui_art/sessions/*-prompt.txt`에 보관한다.
- 2026-09-14: 원본 치와와와 체크포인트 7개 직접 검수. 악마 배경 제거를 내장 이미지 도구로 재시도했지만 RGB/체크무늬가 반환돼 미통과. 최종 아트로 채택하지 않았다.
- 기존 로컬 작업 트리의 PSB 메타파일 27개 변경은 이 작업 이전 변경으로 보존한다.
