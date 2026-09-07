#!/usr/bin/env python3
"""글자 ↔ 바탕 «대비» 를 PNG 에서 재는 자 (T132 · ROUTINE §5 보조 · 순수 파이썬 · PIL 없이 돈다).

왜 있나 — «글자가 바탕에 먹힌다» 는 결함은 게이트가 못 잡는다(크기·아웃라인·잘림·테두리는 재지만 «대비» 는 아무도 안 잰다).
그래서 T84 · T78 퀘스트 줄 · T121 07 이름줄 · T121 15 퀘스트 줄까지 **네 번 다 눈으로만** 잡혔고, 잡을 때마다 워커가
그때그때 픽셀 뽑는 코드를 새로 썼다. 그 손일을 한 곳에 모은다 — `tools/ref_color.py`(색 재는 자)와 같은 갈래다.

재는 법: 사각형 안 픽셀을 모아 **최빈색 = 바탕**, **휘도가 가장 높은 색 = 글자(밝은 글자 기준)** 로 보고 상대휘도 차이를 낸다.
상대휘도 = 0.299R + 0.587G + 0.114B (워커 H 가 T121 실측에 쓴 식 그대로 · `UiKit.Luma` 와 같다).
어두운 글자를 재려면 `--dark` — 휘도가 가장 낮은 색을 글자로 본다.

판정선(제안 · T121 3항): **차이 ≥ 0.35 통과**. 실측 근거 — 07 이름줄 0.09(주인이 지적한 진짜 결함) · 12 설정 줄 0.48(결함 아님).

사용:
  python3 tools/png_contrast.py <png> "<이름>=x,y,w,h" ["<이름>=x,y,w,h" …] [--dark] [--min 0.35]
  python3 tools/png_contrast.py 21.png "층수=230,520,80,60"
좌표는 그 PNG 의 픽셀이다(`screens` PNG 는 540×1168 · `docs/ref/*.jpg` 는 이 자가 아니라 `tools/ref_color.py` 로 잰다 — jpg 는 못 읽는다).
`screens` PNG 는 CI 가 sRGB 로 저장한다(T126 뒤) — 그 전 런(≤ 245)의 PNG 는 선형 값이라 여기서 잰 값도 믿으면 안 된다.

내보내는 값(줄마다): 이름 · 바탕 #RRGGBB(휘도) · 글자 #RRGGBB(휘도) · 차이 · 판정(✅/❌). 하나라도 ❌ 면 종료 코드 1.
"""
import importlib.util, os, sys, collections

ROOT = os.path.dirname(os.path.abspath(__file__))


def _read_png(path):
    """png_crop.py 의 PNG 리더를 그대로 쓴다(8비트 RGB/RGBA · 비인터레이스)."""
    spec = importlib.util.spec_from_file_location('png_crop', os.path.join(ROOT, 'png_crop.py'))
    mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(mod)
    return mod.read_png(path)


def luma(c):
    return (0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]) / 255.0


def hexcolor(c):
    return '#%02X%02X%02X' % (c[0], c[1], c[2])


def measure(rows, bpp, w, h, rect, dark=False):
    """사각형 안의 (바탕색, 글자색, 차이) — 바탕 = 최빈색 · 글자 = 가장 밝은(또는 --dark 면 가장 어두운) 색."""
    x, y, rw, rh = rect
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(w, x + rw), min(h, y + rh)
    if x1 <= x0 or y1 <= y0:
        raise SystemExit('사각형이 그림 밖이다: %s (그림 %d×%d)' % (rect, w, h))
    cnt = collections.Counter()
    for yy in range(y0, y1):
        row = rows[yy]
        for xx in range(x0, x1):
            cnt[tuple(row[xx * bpp:xx * bpp + 3])] += 1
    bg = cnt.most_common(1)[0][0]
    ink = min(cnt, key=luma) if dark else max(cnt, key=luma)
    return bg, ink, abs(luma(ink) - luma(bg))


def main(argv):
    args = [a for a in argv if not a.startswith('--')]
    dark = '--dark' in argv
    need = 0.35
    for i, a in enumerate(argv):
        if a == '--min' and i + 1 < len(argv):
            need = float(argv[i + 1]); args = [x for x in args if x != argv[i + 1]]
    if len(args) < 2:
        print(__doc__); return 2
    path, specs = args[0], args[1:]
    w, h, bpp, rows = _read_png(path)
    bad = 0
    print('| 자리 | 바탕 | 글자 | 차이 | 판정(≥ %.2f) |' % need)
    print('|---|---|---|---|---|')
    for spec in specs:
        name, _, nums = spec.partition('=')
        try:
            rect = [int(v) for v in nums.split(',')]
            if len(rect) != 4:
                raise ValueError
        except ValueError:
            raise SystemExit('자리는 "<이름>=x,y,w,h" 꼴이다: %s' % spec)
        bg, ink, diff = measure(rows, bpp, w, h, rect, dark)
        ok = diff >= need
        if not ok:
            bad += 1
        print('| %s | %s %.2f | %s %.2f | **%.2f** | %s |' % (
            name, hexcolor(bg), luma(bg), hexcolor(ink), luma(ink), diff, '✅' if ok else '❌'))
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
