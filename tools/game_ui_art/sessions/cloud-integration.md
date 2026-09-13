# T519 cloud integration checkpoint

- 담당 시작: ChatGPT Work cloud 최종 통합 담당이 최신 main에서 T519를 이어받음. 다른 로컬 조정 세션은 상태 확인만 수행.
- 현재 상태: main 7e9da09c 확인. CI 34774345301 attempt 2는 success. T519 공통 lock을 `chatgpt-work-cloud-t519`로 선점하며 다른 유일한 lock T507은 90분 이상 지난 별도 범위라 건드리지 않음. 전용 브랜치 ref는 world 9bc8bc21, commerce ca65a146, identity 9ee4929c, banners deff167d, stats 080618f2로 확인.
- 다음 단계: world·commerce·identity 지정 소유 경로만 수령해 manifest/SHA256/PNG/알파/여백을 검수 → world/events/Screens 코드와 중앙 카탈로그·meta 연결 → 작은 완결 묶음별 CI/화면 검증.

## 수령·연결 묶음

- world 14/14, commerce 16/16은 원격 manifest SHA256·PNG 크기·모드·알파를 재검증했고 직접 contact-sheet 검수했다. 닫힘/열림 상자 바닥선 차이는 중세 0px, 현대 1px, 사이버 0px다.
- identity 원격본은 12/13만 승인했다. 손상된 `profile_07.png` Git blob은 사용하지 않고, 원본 치와와와 인접 프로필을 첨부한 built-in imagegen으로 우주 테마 정상본을 재생성했다. 1254×1254 RGBA·알파 0–255·여백·오른쪽 시선 검수 후 기존 `ui.face7` 키에 연결해 9개 순서를 유지했다.
- world/events 코드 diff와 shell의 Screens 챕터 지도 부분만 적용했다. shell의 UiKit/Loading/SeasonPass 전체 patch는 적용하지 않았다.
- 중앙 catalog에 world·commerce·승인된 identity·Arena 3종·쉼터 키를 연결하고 새 meta만 생성했다. generated `AssetCatalog.asset`과 `assets-map.md`을 갱신했다.
- 다음 단계: 프로필 복구 묶음을 main에 보존 → GitHub CI의 C#·EditMode·PlayMode·정적 게이트와 screenshots/PlayLog 확인 → 남은 실제 화면 검수 기록.

## CI 수정 기록

- `c3844a84` / run `34778500618`: dotnet build 실패. static helper의 인스턴스 `App` 참조, PrivilegeScreen 범위 밖 helper 호출, Game 테스트 2개의 EditMode/Core 어셈블리 오배치를 수정했다.
- `444965e8` / run `34779970324`: 잔여 1건(`EventsScreen.Add` static 경로가 instance `ArtKey` 호출)으로 dotnet build 실패. `ArtKey`가 `App.I.Assets`를 읽는 static fallback helper가 되도록 수정했다.
- `24867201` / run `34780081444`: dotnet build·순수 C# 596개·정적 게이트는 success. Unity runner 단계는 failure이고 결과 artifact가 보존됐으며 후속 배포 단계 완료를 기다리며 실패 테스트/PlayLog를 분석 중이다.
- Unity XML 확정: EditMode 596/596, PlayMode 255/257. 실패는 `BorderGateTests.BattleBarsHaveBordersAndCellTagsAreAudited`(Arena rankRow 3화면의 대체 링 누락)와 `EventsScreenTests.DungeonArenaPagesAndPopups`(완성형 merchant Artwork 뒤에도 옛 fallback Counter를 요구한 stale assertion) 두 건이다.
- 수정: rankRow 바깥에 `UiKit.Bordered` 링을 추가하고, merchant 테스트는 실제 `ui.arena.merchant` 스프라이트·Stretch·fallback 비중복을 검증한다. 로컬 meta/catalog/keys/asmdef/test-usings/stale-asserts 검사는 모두 통과했다.
- `9319fbe6` / run `34781230229`: Unity runner success. 결과 XML은 EditMode 596/596, PlayMode 257/257, 실패·skip 0이며 screens 52장을 배포했다. T519 관련 16장(로비·프로필·상점·상자·던전·Arena·이벤트 팝업)을 직접 확인했다.
- 다음 단계: 챕터 지도 4종과 던전 BattleWorld 2종을 별도 실제 screenshot으로 남기고, Unity가 던전 field/road/props 및 rest/devil/angel 노드 SpriteRenderer를 실제 생성하는지 추가 검증한다.

## 클라우드 heartbeat

- 유일 예약: `aaawunity 클라우드 통합 이어서 완료` — 활성, 매시간.
- 다음 실행 확인: 2026-09-14 06:28 KST.
- 관리 대화: https://chatgpt.com/c/6aa70785-f894-83e8-87b2-0cbc295dd7c8
- 기존 이 통합 대화를 우선 재개하며 실행 중 CI/수정을 중복하지 않는다. 로컬 모니터는 중지했고 새 예약·새 작업 번호·승인 우회 담당을 만들지 않는다.

## 증거 캡처 보정

- `19ac3f7e` / run `34782251122`: EditMode 596/596, PlayMode 258/258 success, screens 58장. 런타임 던전/노드 SpriteRenderer 검사는 통과했다.
- 직접 본 신규 6장 중 챕터 지도 4종과 지옥 월드는 정상. 원정 월드는 촬영 순간 열린 레벨업 팝업이 가려 최종 통과로 세지 않았다.
- 다음 단계: 기존 02_battle과 같은 방식으로 촬영 직전 시간 정지·우발 팝업 닫기를 적용해 원정/지옥 월드 두 장을 다시 확인한다.

## 완료

- 최종 코드 `f871456b`, CI `34783274626`: EditMode 596/596, PlayMode 258/258, 실패·skip 0. meta/catalog/keys/asmdef/test-usings/stale-asserts와 data sync 모두 success.
- screens run 1207은 tests success, 58장. T519 관련 기존 16장과 챕터 지도 4종·던전 월드 2종을 직접 확인했다. 원정 월드의 첫 가림 캡처는 보정 후 다시 확인했다.
- 계획한 world 14, commerce 16, identity 13, banners 5, stats 12와 기존 후보/옵션/패턴의 지정 범위를 실제 연결했다. profile 9개 순서·선택 인덱스, 게임 수치·세이브·UI 배치·플레이어 위치는 유지했다.
- T519 완료 문서를 갱신하고 공통 lock을 반납한다. 유일한 매시간 cloud heartbeat는 완료 상태에서 중지한다.
