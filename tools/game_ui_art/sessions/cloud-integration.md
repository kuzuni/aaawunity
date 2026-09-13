# T519 cloud integration checkpoint

- 담당 시작: ChatGPT Work cloud 최종 통합 담당이 최신 main에서 T519를 이어받음. 다른 로컬 조정 세션은 상태 확인만 수행.
- 현재 상태: main 7e9da09c 확인. CI 34774345301 attempt 2는 success. T519 공통 lock을 `chatgpt-work-cloud-t519`로 선점하며 다른 유일한 lock T507은 90분 이상 지난 별도 범위라 건드리지 않음. 전용 브랜치 ref는 world 9bc8bc21, commerce ca65a146, identity 9ee4929c, banners deff167d, stats 080618f2로 확인.
- 다음 단계: world·commerce·identity 지정 소유 경로만 수령해 manifest/SHA256/PNG/알파/여백을 검수 → world/events/Screens 코드와 중앙 카탈로그·meta 연결 → 작은 완결 묶음별 CI/화면 검증.

## 수령·연결 묶음

- world 14/14, commerce 16/16은 원격 manifest SHA256·PNG 크기·모드·알파를 재검증했고 직접 contact-sheet 검수했다. 닫힘/열림 상자 바닥선 차이는 중세 0px, 현대 1px, 사이버 0px다.
- identity는 12/13만 승인했다. 원격 `profile_07.png` Git blob은 manifest SHA256과 다르고 PNG IDAT CRC가 깨져 Assets/카탈로그에 등록하지 않았다. `ui.face7`은 기존 main 경로를 유지한다.
- world/events 코드 diff와 shell의 Screens 챕터 지도 부분만 적용했다. shell의 UiKit/Loading/SeasonPass 전체 patch는 적용하지 않았다.
- 중앙 catalog에 world·commerce·승인된 identity·Arena 3종·쉼터 키를 연결하고 새 meta만 생성했다. generated `AssetCatalog.asset`과 `assets-map.md`을 갱신했다.
- 다음 단계: 이 묶음을 main에 보존 → GitHub CI의 C#·EditMode·PlayMode·정적 게이트와 screenshots/PlayLog 확인 → `profile_07.png` 정상본 미수령을 포함한 남은 실제 화면 검수 기록.
