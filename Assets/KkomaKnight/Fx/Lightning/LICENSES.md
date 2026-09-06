# Assets/KkomaKnight/Fx/Lightning — 출처·라이선스 (T70 · CC0 1.0 = 퍼블릭 도메인 · 출처 표기 의무 없음)

> 주인 지시(2026-09-06 12:2X UTC) «번개 이펙트 뭐 인터넷에서 에셋 다운받아서 되게 해줘» — T28(오디오)과 같은 규칙으로 받았다.
> 이 환경의 프록시는 kenney.nl · opengameart.org · itch.io · jsDelivr 를 막고(`connect_rejected`) **GitHub 만 뚫린다**(결정 153) — 그래서 GitHub 레포에서 `git clone` 으로 받았다.
> 라이선스 원문은 `LICENSES/superpowers-asset-packs-CC0-1.0.txt`(레포 뿌리 · 원본 `LICENSE.txt` 그대로).
> 카탈로그 키와 쓰는 자리는 `Assets/KkomaKnight/catalog.json` 의 `sprites`/`_notes` · `docs/assets-map.md`.

| 파일 | 원작 | 원작자 | 라이선스 | 받은 곳(GitHub · 커밋) | 원본 페이지 |
|---|---|---|---|---|---|
| `lightning-bolt.png` (840×86 · 3.9KB) | *Superpowers Asset Packs* — `rpg-battle-system/fx/2.png` (번개 주문 시트) | Pixel-boy (Sparklin Labs) | CC0 1.0 | `sparklinlabs/superpowers-asset-packs` `rpg-battle-system/fx/2.png` @ `e8674a0` (레포 README·`LICENSE.txt` 에 «released under the Creative Commons Zero (CC0) license») · **바이트 그대로 복사**(md5 `b0a9c6b9b886d051bf31e1d46e8120eb`) | http://sparklinlabs.itch.io/superpowers |

## 시트 규격 (코드가 아는 값 — `Fx.LightningCols`/`LightningFrames`)

- 가로 6칸(칸 140×86px) · 마지막 칸은 빈 칸이다(원본 그대로).
- 칸 0 = 가는 노란 볼트 · 1 = 굵은 흰-하늘 볼트(가지 포함) · **2 = 같은 모양의 새까만 실루엣** · 3 = 옅어지는 하늘색 · 4 = 남은 잔광.
- **칸 2 는 안 쓴다**(재생 순서 0 → 1 → 3 → 4). 원본은 «반전 섬광» 프레임이지만 우리 전투 배경(밝은 숲 맵)에서는 검은 번개가 한 프레임 튀는 «그림 깨짐» 으로 보인다 — PROGRESS «워커 결정 기록» 참조. 파일 자체는 손대지 않았다.
- 칸마다 볼트가 조금씩 왼쪽 아래로 내려간다(칸 0 bbox x 39~138 · y 1~55 → 칸 4 x 1~99 · y 39~84) = 원본이 이미 «비스듬히 내리꽂히는» 애니다. 코드는 이 대각선이 **수직**이 되도록 시트를 통째로 돌려(`Fx.LightningTiltDeg` 55°) 하늘에서 적에게 떨어지게 만든다.
