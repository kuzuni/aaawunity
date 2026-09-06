#!/usr/bin/env bash
# WebGL 배포 스모크 (T59·T60 · 주인 상시 지시 2026-09-06 «배포·push 전 에러 확인 · 게임 들어가 봐서 확인»).
#
# 빌드를 headless chromium(playwright)으로 열어 ⓐ 콘솔 에러 0 ⓑ 로딩 완료 ⓒ «[KkomaKnight] ready lobby» ⓓ(--battle) «ready battle» 을 본다.
# 판정은 tools/webgl_smoke.js · 이 셸은 «무엇을 열지» 만 고른다:
#   tools/webgl_smoke.sh --gh-pages [옵션]          # origin/gh-pages 를 git 으로 받아 로컬 http.server 로 띄워 연다(워커 기본 · 이 환경은 프록시가 kuzuni.github.io 를 403 으로 막는다)
#   tools/webgl_smoke.sh --dir build/WebGL/KkomaKnight [옵션]   # 로컬 빌드 폴더(CI build-webgl 잡)
#   tools/webgl_smoke.sh https://kuzuni.github.io/aaawunity/ [옵션]  # URL 직접(CI 배포 뒤 재확인)
# 옵션(그대로 webgl_smoke.js 로): --battle · --require-marker · --strict-audio · --strict-net · --no-fps · --timeout SEC · --shot out.png · --log out.txt
#   --retries N  : URL 모드에서 N번(60초 간격) 재시도 — gh-pages CDN 반영 지연용
# 종료 코드 = webgl_smoke.js 그대로(0 초록 · 1 빨강 · 3 playwright 없음 · 4 실행 실패).
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MODE=url; TARGET=""; DIR=""; RETRIES=1; ARGS=()
while [ $# -gt 0 ]; do
  case "$1" in
    --gh-pages) MODE=ghpages; shift;;
    --dir) MODE=dir; DIR="$2"; shift 2;;
    --retries) RETRIES="$2"; shift 2;;
    http://*|https://*) MODE=url; TARGET="$1"; shift;;
    *) ARGS+=("$1"); shift;;
  esac
done

if ! command -v node >/dev/null 2>&1; then echo "[smoke] node 없음"; exit 3; fi
NODE_PATH_G="$(npm root -g 2>/dev/null || true)"

serve_dir() {  # $1 = 폴더 → 로컬 http.server (빈 포트) · 전역 SRV_PID / SRV_URL
  local port
  port=$(python3 -c 'import socket; s=socket.socket(); s.bind(("127.0.0.1",0)); print(s.getsockname()[1]); s.close()')
  ( cd "$1" && python3 -m http.server "$port" --bind 127.0.0.1 >/dev/null 2>&1 ) &
  SRV_PID=$!; SRV_URL="http://127.0.0.1:$port/index.html"
  for i in 1 2 3 4 5 6 7 8 9 10; do curl -sfI "$SRV_URL" >/dev/null 2>&1 && return 0; sleep 0.5; done
  echo "[smoke] 로컬 서버가 안 뜬다: $1"; return 1
}

TMP=""
case "$MODE" in
  ghpages)
    TMP="$(mktemp -d)"
    ( cd "$ROOT" && git fetch -q origin gh-pages && git archive origin/gh-pages | tar -x -C "$TMP" ) || { echo "[smoke] origin/gh-pages 를 못 받았다"; exit 4; }
    echo "[smoke] gh-pages $(cd "$ROOT" && git log -1 --format='%h %s' origin/gh-pages) → $TMP"
    serve_dir "$TMP" || exit 4; TARGET="$SRV_URL";;
  dir)
    [ -f "$DIR/index.html" ] || { echo "[smoke] $DIR/index.html 없음"; exit 4; }
    serve_dir "$DIR" || exit 4; TARGET="$SRV_URL";;
  url) [ -n "$TARGET" ] || { echo "사용: tools/webgl_smoke.sh (--gh-pages | --dir DIR | URL) [--battle] [--require-marker] [--shot P] [--log P]"; exit 2; };;
esac

rc=1
for ((i = 1; i <= RETRIES; i++)); do
  NODE_PATH="${NODE_PATH:-}${NODE_PATH:+:}$NODE_PATH_G" node "$ROOT/tools/webgl_smoke.js" "$TARGET" "${ARGS[@]}"; rc=$?
  [ $rc -eq 0 ] && break
  [ $i -lt $RETRIES ] && { echo "[smoke] 재시도 $i/$RETRIES — 60초 뒤"; sleep 60; }
done
[ -n "${SRV_PID:-}" ] && kill "$SRV_PID" >/dev/null 2>&1
[ -n "$TMP" ] && rm -rf "$TMP"
exit $rc
