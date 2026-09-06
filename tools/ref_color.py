#!/usr/bin/env python3
"""레퍼런스 jpg 에서 «요소의 위/아래 색» 을 재는 자(T116 · 주인 2026-09-07 «그라디안트 레퍼런스 부분 참고해서 더 화려하게 색깔»).

주인 지시가 «레퍼런스에서 그 요소의 위쪽 색·아래쪽 색을 각각 재서 표를 만들라» 이므로, 눈대중 대신 이 자로 잰다.
사각형을 세로로 N 등분해 각 띠의 **중앙값(median) 색**을 찍는다 — 평균이 아니라 중앙값이라 칸 안의 아이콘·글자·테두리에
덜 흔들린다(그래도 아이콘을 크게 물면 한 띠만 엉뚱한 색이 나오니, 그 띠는 버리고 좌우로 좁혀 다시 잰다).

사용(컨테이너에 PIL 이 붙은 인터프리터로 · 워커 환경은 `apt-get install -y python3-pil` 뒤 `/usr/bin/python3.12`):
  /usr/bin/python3.12 tools/ref_color.py 09_shop_1.jpg "다이아카드=40,440,225,645" "골드카드=40,1130,225,1335"
  → 다이아카드 ['#40116D', '#5E0D83', '#780A91', '#9710AA', '#AA0CB8']      (맨 앞 = 맨 위 띠 · 맨 뒤 = 맨 아래 띠)

좌표는 `docs/ref/*.jpg` 의 픽셀(사본은 720×1560)이다. 잰 값은 `Assets/KkomaKnight/catalog.json` 의 `col.grad.*` 로 들어가고
`Assets/Scripts/Game/GradientPalette.cs` 가 그 표를 읽는다(코드에 색을 박지 않는다 · ROUTINE §1).
"""
import sys

USAGE = __doc__


def bands(im, rect, n=5, step=None):
    """rect(x0,y0,x1,y1) 를 세로 n 등분해 띠마다 중앙값 색(#RRGGBB)을 돌려준다."""
    x0, y0, x1, y1 = rect
    px = im.convert('RGB').load()
    h = (y1 - y0) / float(n)
    step = step or max(1, (x1 - x0) // 40)
    out = []
    for i in range(n):
        cols = []
        for y in range(int(y0 + i * h) + 1, int(y0 + (i + 1) * h) - 1):
            for x in range(x0 + 2, x1 - 2, step):
                cols.append(px[x, y])
        if not cols:
            out.append(None)
            continue
        cols.sort(key=lambda c: c[0] * 3 + c[1] * 6 + c[2])   # 밝기순(사람 눈 가중치)
        out.append('#%02X%02X%02X' % cols[len(cols) // 2])
    return out


def main(argv):
    if len(argv) < 3:
        print(USAGE)
        return 2
    try:
        from PIL import Image
    except Exception as e:   # noqa: BLE001 — 인터프리터 안내가 목적
        print('PIL 이 없다: %s\n  apt-get install -y python3-pil 뒤 /usr/bin/python3.12 로 실행한다.' % e)
        return 3
    import os
    root = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
    name = argv[1]
    path = name if os.path.exists(name) else os.path.join(root, 'docs', 'ref', name)
    im = Image.open(path)
    for spec in argv[2:]:
        label, rest = spec.split('=', 1)
        rect = [int(v) for v in rest.split(',')]
        print(label, bands(im, rect))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
