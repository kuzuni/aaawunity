#!/usr/bin/env bash
# Assets/StreamingAssets/data/*.json 이 aaaw 레포 main 의 data/ 와 같은지 비교한다 (CI 게이트).
#
#   tools/check_data_sync.sh [--sync] [AAAW_DIR]
#
#   AAAW_DIR  aaaw 체크아웃 경로. 없으면 $AAAW_DIR, 그것도 없으면 ./.aaaw-src 에 main 을 얕게 clone 한다.
#   --sync    다르면 aaaw 쪽을 복사해 맞춘다 (수치는 aaaw 가 정본 — 이 레포에서 JSON 을 손으로 고치지 않는다).
#
# exit 0 = 동일 · 1 = 드리프트(또는 --sync 로 복사함) · 2 = aaaw 를 구할 수 없음
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
if [ -z "$SRC_DIR" ]; then
  SRC_DIR="$HERE/.aaaw-src"
  if [ ! -d "$SRC_DIR/data" ]; then
    echo "· aaaw main 을 $SRC_DIR 에 clone 한다"
    rm -rf "$SRC_DIR"
    git clone --depth 1 --branch main https://github.com/kuzuni/aaaw.git "$SRC_DIR" >/dev/null 2>&1 || { echo "!! aaaw clone 실패"; exit 2; }
  fi
fi
SRC="$SRC_DIR/data"
[ -d "$SRC" ] || { echo "!! $SRC 가 없다"; exit 2; }
if [ -d "$SRC_DIR/.git" ]; then echo "· aaaw HEAD = $(git -C "$SRC_DIR" rev-parse --short HEAD)"; fi

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
if [ "$drift" = 0 ]; then
  echo "OK — data/*.json 이 aaaw main 과 같다 ($(sed -n 's/.*"_source": "\([^"]*\)".*/\1/p' "$DST/tune.json"))"
  exit 0
fi
if [ "$SYNC" = 1 ]; then
  cp "$SRC"/*.json "$DST"/
  python3 "$HERE/tools/gen_meta.py" >/dev/null
  echo "복사함 — Assets/StreamingAssets/data 를 aaaw main 으로 맞췄다. 커밋할 것."
  exit 1
fi
echo "!! 드리프트 — tools/check_data_sync.sh --sync 로 맞추고 커밋할 것"
exit 1
