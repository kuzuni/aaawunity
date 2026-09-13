# T519 cloud integration checkpoint

- 담당 시작: ChatGPT Work cloud 최종 통합 담당이 최신 main에서 T519를 이어받음. 다른 로컬 조정 세션은 상태 확인만 수행.
- 현재 상태: main 7e9da09c 확인. CI 34774345301 attempt 2는 success. T519 공통 lock을 `chatgpt-work-cloud-t519`로 선점하며 다른 유일한 lock T507은 90분 이상 지난 별도 범위라 건드리지 않음. 전용 브랜치 ref는 world 9bc8bc21, commerce ca65a146, identity 9ee4929c, banners deff167d, stats 080618f2로 확인.
- 다음 단계: world·commerce·identity 지정 소유 경로만 수령해 manifest/SHA256/PNG/알파/여백을 검수 → world/events/Screens 코드와 중앙 카탈로그·meta 연결 → 작은 완결 묶음별 CI/화면 검증.
