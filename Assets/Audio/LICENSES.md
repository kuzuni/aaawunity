# Assets/Audio — 출처·라이선스 (T28 · 전부 CC0 1.0 = 퍼블릭 도메인 · 출처 표기 의무 없음)

> 이 환경의 프록시는 kenney.nl · opengameart.org · freesound.org 를 막아 **GitHub 미러**에서 `git clone` 으로 받았다(ROUTINE §2 T28.1).
> 형식 OGG(Vorbis) · 파일당 ≤ 300KB(BGM ≤ 1MB) · 합계 ≤ 5MB(§1 바이너리 예외 2 · 주인 지시). WAV 는 `ffmpeg -c:a libvorbis` 로 변환.
> 카탈로그 키(`bgm.*`/`snd.*`)와 쓰는 자리는 `Assets/KkomaKnight/catalog.json` 의 `audio`/`_notes` · `docs/assets-map.md`.

## 배경음 (bgm/)

| 파일 | 원작 | 원작자 | 라이선스 | 받은 곳(GitHub 미러 · 커밋) | 원본 페이지 |
|---|---|---|---|---|---|
| `bgm/lobby.ogg` | «4 Chiptunes (Adventure)» — *Title Screen* (0:11 · 루프) | Juhani Junkala | CC0 1.0 | `petergyang/space-shooter-game` `assets/music/title.wav` @ `0847e78` (README «Music: Juhani Junkala (CC0 License)») → ogg q3 | https://opengameart.org/content/4-chiptunes-adventure |
| `bgm/battle.ogg` | «4 Chiptunes (Adventure)» — *Level 1* (1:14 · 루프) | Juhani Junkala | CC0 1.0 | `petergyang/space-shooter-game` `assets/music/level1.wav` @ `0847e78` → ogg q2 (908KB) | https://opengameart.org/content/4-chiptunes-adventure |
| `bgm/boss.ogg` | «NES Shooter Music (5 tracks, 3 jingles)» — *boss* (0:34 · 루프) | SketchyLogic | CC0 1.0 | `IDoTweaks/the-way-to-teal-just-kill-` `Audio/music/music_boss.ogg` @ `a6645f3` (README «all CC0 chiptune from OpenGameArt.org … SketchyLogic ("NES Shooter Music" pack — … the boss theme)») · 그대로 복사 | https://opengameart.org/content/nes-shooter-music-5-tracks-3-jingles |

- 트랙 식별 근거: 미러 파일이 이름을 바꿔 두어 **길이로 대조**했다 — Title Screen 11.29초 · Level 1 74.25초(원본 목록 0:11 · 1:14 · `Bloxdy/code-api` SOUNDS_AND_MUSIC.md 의 «Juhani Junkala [Retro Game Music Pack] Level 1 (1:14) … Title Screen (0:11)»). 두 미러의 Title Screen 길이가 소수점까지 같다(11.294127초).

## 효과음 (sfx/) — 전부 Kenney (Kenney Vleugels · kenney.nl · CC0 1.0)

받은 곳: `ETdoFresh/kenney.nl`(«A Mirror of Kenney's Assets») @ `45df48c` — 팩마다 `License.txt` 에 «License (Creative Commons Zero, CC0) http://creativecommons.org/publicdomain/zero/1.0/» 명시. 파일은 그대로 복사(이미 OGG).

| 파일 | Kenney 팩 | 원본 파일 | 원본 페이지 |
|---|---|---|---|
| `sfx/click.ogg` | UI Audio | `click1.ogg` | https://kenney.nl/assets/ui-audio |
| `sfx/popup.ogg` | Interface Sounds | `open_001.ogg` | https://kenney.nl/assets/interface-sounds |
| `sfx/hit.ogg` | Impact Sounds | `impactMetal_light_000.ogg` | https://kenney.nl/assets/impact-sounds |
| `sfx/crit.ogg` | Impact Sounds | `impactMetal_heavy_000.ogg` | https://kenney.nl/assets/impact-sounds |
| `sfx/kill.ogg` | Impact Sounds | `impactPunch_heavy_000.ogg` | https://kenney.nl/assets/impact-sounds |
| `sfx/hurt.ogg` | Impact Sounds | `impactPunch_medium_000.ogg` | https://kenney.nl/assets/impact-sounds |
| `sfx/miss.ogg` | RPG Audio | `drawKnife1.ogg` | https://kenney.nl/assets/rpg-audio |
| `sfx/arrow.ogg` | RPG Audio | `drawKnife2.ogg` | https://kenney.nl/assets/rpg-audio |
| `sfx/axe.ogg` | RPG Audio | `chop.ogg` | https://kenney.nl/assets/rpg-audio |
| `sfx/coin.ogg` | RPG Audio | `handleCoins.ogg` | https://kenney.nl/assets/rpg-audio |
| `sfx/gacha.ogg` | RPG Audio | `metalLatch.ogg` | https://kenney.nl/assets/rpg-audio |
| `sfx/equip.ogg` | RPG Audio | `metalClick.ogg` | https://kenney.nl/assets/rpg-audio |
| `sfx/levelup.ogg` | Digital Audio | `powerUp1.ogg` | https://kenney.nl/assets/digital-audio |
| `sfx/perk.ogg` | Digital Audio | `threeTone1.ogg` | https://kenney.nl/assets/digital-audio |
| `sfx/fuse.ogg` | Digital Audio | `zapThreeToneUp.ogg` | https://kenney.nl/assets/digital-audio |
| `sfx/clear.ogg` | Music Jingles | `8-Bit jingles/jingles_NES00.ogg` | https://kenney.nl/assets/music-jingles |
| `sfx/fail.ogg` | Music Jingles | `8-Bit jingles/jingles_NES13.ogg` | https://kenney.nl/assets/music-jingles |

## 바꾸는 법

- 소리만 바꾸려면 같은 이름의 `.ogg` 로 덮어쓰면 된다(카탈로그·코드 불변 · `.meta` 는 그대로).
- 키를 늘리려면 파일을 넣고 `catalog.json` 의 `audio` 에 한 줄 → `python3 tools/gen_meta.py && python3 tools/gen_catalog.py`.
- 이 표에 없는 파일을 넣지 않는다(라이선스가 CC0/PD 가 아니면 쓰지 않는다 — ROUTINE T28.1).
