#!/usr/bin/env python3
"""이음매 자 — PNG 를 **세로로 훑어** «어디서 무엇이 바뀌는가» 를 색 토막으로 찍는다.

왜 도구인가 (T361 · 결정 1015 ⑥ 다음 회차):
    T361 을 세 사람이 세 번 쟀는데(결정 998 · 1006 · 1015) 매번 **같은 스크립트를 손으로 다시 썼다**.
    게다가 세 번 중 두 번은 «한 열만 재서» 틀렸다 — 결정 1012(워커 M)·1006 ③(워커 H)이 서로 모르고
    같은 결론에 닿았다: **한 열의 평균으로는 «무엇» 인지 못 가른다 · 가르는 것은 x 방향의 모양이다.**
    그래서 이 자는 처음부터 **열을 여럿** 훑고, 열마다 나온 토막을 나란히 찍는다.
    같은 y 에서 열마다 색이 다르면 그것은 «층(테두리·상자면)» 이 아니라 **가운데만 밝은 것(빛·글자)** 이다.

쓰는 법:
    python3 tools/seam_scan.py <png> --y 300:360                 # 기본 열 18·30·70%
    python3 tools/seam_scan.py <png> --x 18,30,70 --y 300:360    # 열을 직접 고른다
    python3 tools/seam_scan.py <png> --y 300:360 --rows          # 토막 말고 한 줄씩 raw 로
    python3 tools/seam_scan.py --selftest                        # 자가 검사(그림 없이)

읽는 법:
    한 토막 = «이 y 부터 이 y 까지 같은 색» 이다(--tol 로 같음의 폭을 준다).
    열 셋의 토막 경계가 **같은 y** 면 가로로 곧은 층이다(테두리·상자 윗면 …).
    한 열에만 있는 토막은 그 x 에만 있는 것이다(제목 글자·가운데가 밝은 빛무리).
"""
import argparse
import os
import sys


def _load(path):
    try:
        from PIL import Image
    except ImportError:  # pragma: no cover - 환경에 PIL 이 없을 때
        sys.exit("PIL 이 없다: pip install pillow")
    return Image.open(path).convert("RGB")


def columns(img, xs_pct):
    """x 비율(%) 목록 → [(x픽셀, [(r,g,b), …y 순]), …]"""
    w, h = img.size
    px = img.load()
    out = []
    for p in xs_pct:
        x = min(w - 1, max(0, int(round(w * p / 100.0))))
        out.append((x, [px[x, y] for y in range(h)]))
    return out


def runs(col, y0, y1, tol):
    """세로 한 줄을 «같은 색이 이어지는 토막» 으로 접는다."""
    segs = []
    for y in range(y0, y1):
        c = col[y]
        if segs and _near(segs[-1][2], c, tol):
            segs[-1][1] = y
        else:
            segs.append([y, y, c])
    return [(a, b, c) for a, b, c in segs]


def _near(a, b, tol):
    return abs(a[0] - b[0]) <= tol and abs(a[1] - b[1]) <= tol and abs(a[2] - b[2]) <= tol


def _fmt(c):
    return "%3d,%3d,%3d" % c


def scan(path, xs_pct, y0, y1, tol, rows):
    img = _load(path)
    w, h = img.size
    y1 = h if y1 is None else min(h, y1)
    y0 = max(0, y0)
    cols = columns(img, xs_pct)
    print("· %s  %dx%d  y %d~%d  열 %s (tol %d)"
          % (os.path.basename(path), w, h, y0, y1 - 1,
             " ".join("%g%%=x%d" % (p, x) for p, (x, _) in zip(xs_pct, cols)), tol))
    if rows:
        print("   y   | " + " | ".join("x%-4d      " % x for x, _ in cols))
        for y in range(y0, y1):
            print(" %4d  | " % y + " | ".join(_fmt(c[y]) for _, c in cols))
        return 0
    for x, col in cols:
        print("  x%d:" % x)
        for a, b, c in runs(col, y0, y1, tol):
            print("    y%4d~%-4d (%2d줄)  %s" % (a, b, b - a + 1, _fmt(c)))
    return 0


def selftest():
    """그림 파일 없이 자기 논리를 잰다 — 층 하나와 «가운데만 밝은 것» 하나를 심고 갈리는지 본다."""
    dark, band, face = (10, 10, 10), (0, 0, 0), (85, 85, 85)
    glow = (204, 164, 29)
    # 열 둘: 왼쪽은 층만 · 가운데는 같은 y 에 빛이 얹힌다.
    left = [dark] * 5 + [band] * 5 + [face] * 5
    mid = [glow] * 5 + [band] * 5 + [face] * 5
    rl = runs(left, 0, 15, 6)
    rm = runs(mid, 0, 15, 6)
    assert [a for a, _, _ in rl] == [0, 5, 10], rl
    assert [a for a, _, _ in rm] == [0, 5, 10], rm
    # 경계는 같은 y 인데(층) 첫 토막의 색만 다르다 ⇒ «가운데만 밝은 것» 이 가려진다.
    assert rl[1][2] == rm[1][2] and rl[2][2] == rm[2][2], (rl, rm)
    assert not _near(rl[0][2], rm[0][2], 6), (rl[0], rm[0])
    # tol 이 크면 층이 뭉개진다 — 문턱이 결과를 만든다는 것을 자로 못 박는다.
    assert len(runs(left, 0, 15, 90)) == 1
    # 한 줄짜리 토막도 접히지 않고 남는다(테두리 1px 을 놓치면 이 자가 쓸모없다).
    one = [dark] * 3 + [band] + [face] * 3
    assert [(a, b) for a, b, _ in runs(one, 0, 7, 6)] == [(0, 2), (3, 3), (4, 6)]
    print("✓ seam_scan 자가 검사 통과 — 층/빛 가르기 · tol 효과 · 1px 토막 보존")
    return 0


def main(argv=None):
    ap = argparse.ArgumentParser(description="PNG 세로 훑기(이음매 자)")
    ap.add_argument("png", nargs="?", help="읽을 PNG")
    ap.add_argument("--x", default="18,30,70", help="열 위치 비율(%%) 쉼표 목록 (기본 18,30,70)")
    ap.add_argument("--y", default=":", help="y 범위 «시작:끝» (끝 제외 · 비우면 전체)")
    ap.add_argument("--tol", type=int, default=6, help="«같은 색» 의 폭 (기본 6)")
    ap.add_argument("--rows", action="store_true", help="토막 대신 한 줄씩 raw 로 찍는다")
    ap.add_argument("--selftest", action="store_true", help="그림 없이 자가 검사")
    a = ap.parse_args(argv)
    if a.selftest:
        return selftest()
    if not a.png:
        ap.error("PNG 를 달라 (또는 --selftest)")
    s, _, e = a.y.partition(":")
    y0 = int(s) if s else 0
    y1 = int(e) if e else None
    xs = [float(v) for v in a.x.split(",") if v.strip()]
    return scan(a.png, xs, y0, y1, a.tol, a.rows)


if __name__ == "__main__":
    try:
        sys.exit(main())
    except BrokenPipeError:
        # `| head` 로 잘라 읽는 것이 이 자의 기본 쓰임이다 — 거기서 역추적이 튀어나오면 자를 못 믿는다.
        os.dup2(os.open(os.devnull, os.O_WRONLY), sys.stdout.fileno())
        sys.exit(0)
