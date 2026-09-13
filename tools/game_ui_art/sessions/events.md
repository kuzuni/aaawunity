# T519 events 역할 보고서

최종 갱신: 2026-09-13 (격리 작업 브랜치 `work`). 범위는 `EventsScreen.cs`, `Recipes.cs`, `LobbyPopups.cs`와 전용 EditMode 테스트뿐이다. 상점 상자는 `ShopScreen.cs`가 이미 `chest.<rare|legend|myth>` 및 `.open`을 사용하고, 열린 스프라이트가 없으면 닫힌 스프라이트를 계속 표시하므로 코드 변경 없이 보존했다.

## 완료한 코드

- 실제 `gear.json`/`recipe.json` 부위 `weapon, helm, armor, glove, boot, neck`을 각각 `recipe.weapon, recipe.helmet, recipe.armor, recipe.ring, recipe.shoes, recipe.necklace`로 분기했다. 알 수 없는 부위와 랜덤 도안은 기존 `ui.iconScroll`이다. Core에는 UnityEngine 의존성을 추가하지 않았다.
- 상인 Goods, 던전 보상, 로비의 퀘스트/원정/특권 보상 소비처는 새 부위 키를 사용하되 카탈로그에 없으면 `ui.iconScroll`로 돌아간다. 다이아는 `hud.gem` 그대로다.
- 던전 카드 `ui.dungeon.hell`/`ui.dungeon.expedition`, 아레나 입구와 무대 `ui.arena.entry`, 순위 행 `ui.arena.rankRow`, 상인 배너 `ui.arena.merchant`를 선택적으로 삽입했다. 키가 없을 때만 기존 합성 UI를 그대로 만들며 기존 사각형, 글자, 수치, 버튼과 플레이어 초상 위치는 바꾸지 않았다.
- 전용 테스트 `ChihuahuaEventsArtTests`가 실제 여섯 부위 매핑과 랜덤/미지 부위 fallback을 고정한다.

## 통합 입력 / 남은 작업

| 키 | 예상 파일 |
|---|---|
| `recipe.weapon`, `recipe.armor`, `recipe.helmet`, `recipe.shoes`, `recipe.ring`, `recipe.necklace` | `Assets/Art/ChihuahuaGameUI/Recipes/{weapon,armor,helmet,shoes,ring,necklace}.png` |
| `ui.dungeon.expedition`, `ui.dungeon.hell` | `Assets/Art/ChihuahuaGameUI/Dungeons/{expedition,hell}.png` |
| `ui.arena.entry`, `ui.arena.rankRow`, `ui.arena.merchant` | `Assets/Art/ChihuahuaGameUI/Arena/{entry,rank_row,merchant}.png` |
| 기존 `chest.rare`, `chest.legend`, `chest.myth`와 각 `.open` | `Assets/Art/ChihuahuaGameUI/Chests/{rare,legend,myth}_{closed,open}.png` |

통합 세션이 실제 PNG/meta를 추가하고 중앙 catalog를 생성해야 한다. 현재 `check_catalog_keys.py`의 신규 키 오류는 이 미완료 통합 의존성이다(우회하지 않음). 닫힘/열림 상자의 동일 몸체 위치·바닥선과 모든 새 이미지의 최종 크기/가독성은 에셋이 없어 검증 미완료다. Unity 실행 파일과 `dotnet`이 이 환경에 없어 EditMode/C# 실행도 미완료다.

## 검증 기록

- `git diff --check`: 통과.
- `python3 tools/check_catalog_keys.py`: 예상 실패(신규 던전/아레나 키가 중앙 catalog에 아직 없음).
- `python3 tools/test_by_name.py ChihuahuaEventsArtTests --run`: 실행 불가(`dotnet` 없음). Unity를 실행하지 않았으므로 통과로 기록하지 않는다.
