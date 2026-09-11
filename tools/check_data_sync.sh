#!/usr/bin/env bash
# Assets/StreamingAssets/data/*.json 이 aaaw 레포 main 의 data/ 와 같은지 비교한다 (CI 게이트).
#
#   tools/check_data_sync.sh [--sync] [AAAW_DIR]
#
#   AAAW_DIR  aaaw 체크아웃 경로. 없으면 $AAAW_DIR, 그것도 없으면 ./.aaaw-src 에 main 을 얕게 clone 한다.
#   --sync    다르면 aaaw 쪽을 복사해 맞춘다 (수치는 aaaw 가 정본 — 이 레포에서 JSON 을 손으로 고치지 않는다).
#
# exit 0 = 동일 · 1 = 드리프트(또는 --sync 로 복사함) · 2 = aaaw 를 구할 수 없음(또는 낡은 클론으로 --sync 시도)
#
# ⚑⚑ T479 2회차 — **이 자는 «어느 aaaw 와 견줬는가» 를 몰랐다**(결정 1340 · 검수 Q 실측 · 워커 B·L 이 앞뒤를 댔다).
#   견주는 것은 `SRC_DIR` 뿐인데 초록 줄은 «aaaw **main** 과 같다» 라고 적었다. CI 에선 참이다
#   (`ci.yml` 의 `datasync` 잡이 매 런 `actions/checkout` 으로 main 을 새로 받는다 · 워커 L 이 열어 확인).
#   **워커 통에서는 참일 이유가 없다** — `.aaaw-src` 는 회차를 넘어 남아 늙는다.
#   실측(검수 Q): 런 1115 의 레포 트리 ↔ 엿새 묵은 클론이 **일곱 파일 전부 같았고**, 같은 순간 CI 는 드리프트였다.
#   ⇒ 한 자가 같은 순간에 CI 에선 «드리프트», 워커 통에선 «OK — main 과 같다» 를 냈다. **거짓 초록**이다.
#   ⚑ 거짓 빨강은 조사를 부르고 **거짓 초록은 아무것도 안 부른다** — 그래서 이 쪽이 더 비싸다(결정 1340).
#   ⚠⚠ 그리고 거짓 빨강 쪽의 값도 싸지 않았다: 그때 이 자가 찍던 처방(`--sync`)이 **정확히 거꾸로**다 —
#     낡은 클론으로 `--sync` 를 돌리면 **주인이 방금 올린 수치를 옛 값으로 덮는다**(워커 L 실측: 주인의
#     «방어 상한 80 → 90» 이 그 한 커밋이었다). 금지 셋 가운데 하나가 **성실한 손으로** 넘어가는 길이다.
#   그래서 이 회차가 고친 것 셋 — **문턱은 안 세웠다**(결정 493):
#     ⓐ 남아 있던 클론은 **먼저 최신화한다**(워커 L 의 «첫 수»). 못 하면 그 사실을 적는다.
#     ⓑ «견준 자리»(sha · 날짜 · 어디서 왔는가)를 **초록에도 빨강에도 같이** 적는다 — 사람은 sha 만 보고
#        엿새 전 것인지 모른다(워커 B 의 처방을 초록 줄까지 넓힌 것이 결정 1340 의 «거울» 이다).
#     ⓒ 드리프트 처방을 **두 걸음**으로 나눠, «클론 나이를 먼저 의심하라» 를 `--sync` 앞에 놓는다.
#        그리고 **최신인지 확인 못 한 클론으로는 `--sync` 를 아예 안 한다**(rc 2) — 그 한 줄이 막는 것은
#        «문턱» 이 아니라 **되돌리기 어려운 잘못된 쓰기**다.
set -u
HERE="$(cd "$(dirname "$0")/.." && pwd)"
DST="$HERE/Assets/StreamingAssets/data"
SYNC=0; SRC_DIR=""
for a in "$@"; do
  case "$a" in
    --sync) SYNC=1 ;;
    *) SRC_DIR="$a" ;;
  esac
done
SRC_DIR="${SRC_DIR:-${AAAW_DIR:-}}"
# PROV = «이 자리가 어디서 왔는가» · FRESH = «그것이 main 의 끝이라고 말할 수 있는가»(1/0)
PROV="부른 쪽 — 자리는 줬는데 git 이 아니라 나이를 못 잰다"; FRESH=0
GIVEN=0; [ -n "$SRC_DIR" ] && GIVEN=1
if [ -z "$SRC_DIR" ]; then
  SRC_DIR="$HERE/.aaaw-src"
  if [ ! -d "$SRC_DIR/data" ]; then
    echo "· aaaw main 을 $SRC_DIR 에 clone 한다"
    rm -rf "$SRC_DIR"
    git clone --depth 1 --branch main https://github.com/kuzuni/aaaw.git "$SRC_DIR" >/dev/null 2>&1 || { echo "!! aaaw clone 실패"; exit 2; }
    PROV="방금 clone 했다"; FRESH=1
  fi
fi
# ⚑⚑ **3회차(T479 · 결정 1349 · 워커 E 실측)** — 2회차는 «먼저 당긴다» 를 **자리를 제 손으로 고른 갈래에만**
#   걸어 두고, **부른 쪽이 자리를 주면 당기지 않은 채 `FRESH=1`** 을 줬다. 그 근거로 적어 둔 «CI 는 매 런
#   checkout 이라 참» 은 **CI 에서만** 참인데, 워커의 스크래치 실행기도 자리를 넘긴다(워커 E 의 `gates.sh` 가
#   `.aaaw-src` 를 넘겼다) — 그 길로 **거짓 초록이 그대로 살아 있었다.** 곧 «캐물은 적 없이 «그렇다»» 였다.
#   ⇒ **자리가 어디서 왔든 `.git` 이면 똑같이 당긴다.** aaaw 는 읽기만 하니 잃을 것이 없고(미는 것 0),
#     CI 에서는 이미 main 의 끝이라 사실상 무동작이다(네트워크 한 번). 못 당기면 **`FRESH=0` 으로 적는다** —
#     rc 는 안 건드리므로 CI 에 새 빨강이 생기지 않는다. **«안 본다» 를 «봤다» 로 적지 않는 것이 이 절 전체다.**
if [ -d "$SRC_DIR/.git" ]; then
  if git -C "$SRC_DIR" fetch --depth 1 origin main >/dev/null 2>&1 \
     && git -C "$SRC_DIR" reset --hard FETCH_HEAD >/dev/null 2>&1; then
    [ "$GIVEN" = 1 ] && PROV="부른 쪽이 준 자리 · 방금 당겨서 main 의 끝임을 봤다" || PROV="방금 최신화했다"
    FRESH=1
  else
    PROV="⚠ 못 당겼다(네트워크?) — main 의 끝인지 확인 못 함"; FRESH=0
  fi
fi
SRC="$SRC_DIR/data"
[ -d "$SRC" ] || { echo "!! $SRC 가 없다"; exit 2; }
# 견준 자리 — sha 만으로는 «엿새 전 것» 을 못 읽는다(결정 1340). 날짜와 출처를 같이 적는다.
WHERE="$SRC_DIR ($PROV)"
if [ -d "$SRC_DIR/.git" ]; then
  WHERE="aaaw $(git -C "$SRC_DIR" rev-parse --short HEAD 2>/dev/null || echo '?')"
  WHERE="$WHERE ($(git -C "$SRC_DIR" log -1 --format=%cd --date=format:'%Y-%m-%d %H:%M' 2>/dev/null || echo '?') · $PROV)"
fi
echo "· 견준 자리 = $WHERE"

drift=0
for f in "$SRC"/*.json; do
  b="$(basename "$f")"
  if [ ! -f "$DST/$b" ]; then echo "누락: $b"; drift=1; continue; fi
  if ! cmp -s "$f" "$DST/$b"; then echo "다름: $b"; drift=1; fi
done
for f in "$DST"/*.json; do
  b="$(basename "$f")"
  [ -f "$SRC/$b" ] || { echo "aaaw 에 없는 파일: $b"; drift=1; }
done
SRCLINE="$(sed -n 's/.*"_source": "\([^"]*\)".*/\1/p' "$DST/tune.json")"
if [ "$drift" = 0 ]; then
  if [ "$FRESH" = 1 ]; then
    echo "OK — data/*.json 이 aaaw main 과 같다 ($SRCLINE)"   # 견준 자리는 위 줄이 이미 적었다
  else
    # ⚑ 여기가 결정 1340 이 짚은 «거짓 초록» 자리다 — 견준 것은 이 자리뿐이고 그것이 main 의 끝인지 모른다.
    echo "OK(견준 자리 기준) — data/*.json 이 «$WHERE» 와 같다 ($SRCLINE)"
    echo "   ⚠ 이 자리가 aaaw main 의 끝인지는 **확인 못 했다** — 그러니 이 초록은 «main 과 같다» 가 아니다."
    if [ -d "$SRC_DIR/.git" ]; then
      echo "      당긴 뒤 다시 돌린다:  git -C \"$SRC_DIR\" fetch && git -C \"$SRC_DIR\" reset --hard origin/main"
    else
      # 처방이 그 자리에서 실제로 듣는 것이어야 한다 — git 이 아닌 자리에 «fetch 하라» 는 안 듣는다(워커 L 의 ⓒ).
      echo "      그 자리는 git 이 아니라 나이를 잴 수 없다 — 자리를 안 주고 그냥 돌리면(인자 없이) 이 자가 제 클론을 당겨서 본다."
    fi
  fi
  exit 0
fi
if [ "$SYNC" = 1 ]; then
  if [ "$FRESH" != 1 ]; then
    # ⚠⚠ 낡은(또는 나이를 모르는) 자리로 덮으면 주인이 방금 올린 수치가 옛 값으로 되돌아간다.
    #    그 쓰기는 «손으로 안 고쳤고 규약이 준 명령만 썼는데» 금지 셋 중 하나를 넘는 길이다(결정 1340).
    echo "!! --sync 를 안 한다 — 견준 자리가 main 의 끝인지 확인 못 했다($WHERE)"
    echo "   이 상태로 덮으면 **주인이 방금 올린 수치를 옛 값으로 되돌린다**(워커 L 실측: «방어 상한 80 → 90» 이 그 한 커밋이었다)."
    echo "   먼저:  git -C \"$SRC_DIR\" fetch && git -C \"$SRC_DIR\" reset --hard origin/main   → 그러고도 다르면 그때 --sync."
    exit 2
  fi
  cp "$SRC"/*.json "$DST"/
  python3 "$HERE/tools/gen_meta.py" >/dev/null
  echo "복사함 — Assets/StreamingAssets/data 를 aaaw main($WHERE) 으로 맞췄다. 커밋할 것."
  exit 1
fi
echo "!! 드리프트 — 견준 자리는 «$WHERE» 다"
if [ "$FRESH" = 1 ]; then
  echo "   그 자리는 방금 확인한 main 의 끝이다 ⇒ **진짜 드리프트**다: tools/check_data_sync.sh --sync 로 맞추고 커밋할 것."
else
  echo "   ① 먼저 위 날짜를 본다 — 오늘 것이 아니면 뒤처진 것은 **레포가 아니라 이 클론**이다(결정 1340)."
  echo "      첫 수는 --sync 가 아니다:  git -C \"$SRC_DIR\" fetch && git -C \"$SRC_DIR\" reset --hard origin/main"
  echo "      ⚠ 낡은 자리로 --sync 하면 **주인이 방금 올린 수치를 옛 값으로 덮는다** — 이 자가 지금은 그것을 막는다(rc 2)."
  echo "   ② 클론이 최신인데도 다르면 그때가 진짜 드리프트다 — --sync 로 맞추고 커밋할 것."
fi
exit 1
