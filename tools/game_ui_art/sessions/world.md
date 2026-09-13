# T519 world 역할 보고서

## 상태

- 완료: `BattleWorld.cs`에 던전 키 `expedition`/`hell` 테마 선택과 새 아트가 모두 있을 때만 켜지는 원자적 fallback을 연결했다.
- 완료: 생성물 `MapLayouts.cs`를 건드리지 않고 `DungeonMapLayouts.cs`에 고대 유적/지옥 전용 3-prop 반복 배치를 분리했다.
- 완료: 큰 원본 PNG도 sprite bounds에 맞춰 field/road의 기존 월드 footprint 및 prop/node 목표 화면 높이로 정규화한다.
- 완료: 월드 노드와 Rest/Devil/Angel 팝업에 `node.rest`, `node.devil`, `node.angel`을 선택적으로 연결했다. 키가 없으면 기존 합성/아이콘 표시를 유지한다.
- 유지: 노드 엔진 좌표·기능, UI rect, 게임 수치, 저장 데이터, 플레이어 위치는 변경하지 않았다.

## 통합 계약

| 키 | 최종 예상 파일 |
|---|---|
| `node.rest` | `Assets/Art/ChihuahuaGameUI/Nodes/rest.png` |
| `node.devil` | `Assets/Art/ChihuahuaGameUI/Nodes/devil.png` |
| `node.angel` | `Assets/Art/ChihuahuaGameUI/Nodes/angel.png` |
| `env.expedition.field` | `Assets/Art/ChihuahuaGameUI/World/Expedition/field.png` |
| `env.expedition.road` | `Assets/Art/ChihuahuaGameUI/World/Expedition/road.png` |
| `env.expedition.prop1`~`prop3` | `Assets/Art/ChihuahuaGameUI/World/Expedition/prop1.png`~`prop3.png` |
| `env.hell.field` | `Assets/Art/ChihuahuaGameUI/World/Hell/field.png` |
| `env.hell.road` | `Assets/Art/ChihuahuaGameUI/World/Hell/road.png` |
| `env.hell.prop1`~`prop3` | `Assets/Art/ChihuahuaGameUI/World/Hell/prop1.png`~`prop3.png` |

## 검증 / 이어받기

- 전용 EditMode 계약 테스트 `ChihuahuaWorldArtConnectionTests.cs` 추가.
- 통과: `git diff --check`, `python3 tools/check_catalog_keys.py`, `python3 tools/check_asmdef.py`, `python3 tools/check_test_usings.py`, `python3 tools/check_stale_asserts.py`.
- 환경 제한: `dotnet build tools/dotnet/KkomaKnight.sln --no-restore`는 이 격리 환경에 `dotnet` 실행 파일이 없어 실행되지 않았다. Unity 실행 파일도 없어 EditMode/PlayMode는 실행하지 못했으며 통과로 간주하지 않는다.
- 주의: 현재 카탈로그 검사기는 카탈로그에 아직 존재하지 않는 새 접두(`node.*`) 및 조립 문자열(`env.` + dungeon)을 잡지 못해 통과했다. 아래 키는 중앙 카탈로그에 실제로 아직 없으며 통합 의존성이다.
- 아직 미검증: Unity EditMode/PlayMode 실행 및 실제 PNG가 들어간 화면의 월드 크기/겹침/알파 육안 검사.
- 통합 세션 다음 단계: 위 13개 RGBA PNG와 `.meta`를 예상 경로에 추가하고 중앙 `catalog.json`을 연결한 뒤 생성물 재생성. 새 키 리터럴 카탈로그 검사는 그 전까지 예상 의존성으로 실패하며 우회하면 안 된다.
- 실제 아트가 모두 들어오기 전에는 던전 5개 키 중 하나라도 없으면 해당 던전 전체가 기존 챕터 테마로 돌아간다.
