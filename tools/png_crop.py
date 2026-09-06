#!/usr/bin/env python3
"""PNG 잘라 확대 (순수 파이썬 · PIL/ffmpeg/ImageMagick 이 없는 워커 환경용 · UI 비평 §5 보조).

사용:
  python3 tools/png_crop.py <in.png> <out.png> <x> <y> <w> <h> [배율=3]
  python3 tools/png_crop.py --strip <in.png> <out.png> [배율=3]   # 하니스 PNG 의 «UI 띠» 를 자동 탐지해 자른다

«--strip»: screens 브랜치 PNG(540×1168)에서 UI 가 가운데 좁은 띠(≈188×404)로만 찍히는 동안(T58) 띠를 자동으로 찾는다 —
  세로 중앙 행에서 배경(좌우 끝 색)과 다른 첫/끝 열, 가로 중앙 열에서 배경과 다른 첫/끝 행.
8비트 RGB/RGBA · 비인터레이스 PNG 만(유니티 ScreenCapture/RenderTexture 출력이 그렇다). 확대는 최근접(글자 경계 보존).
"""
import struct, sys, zlib


def read_png(path):
    data = open(path, 'rb').read()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', '%s: PNG 아님' % path
    pos, idat, w, h, ct, bd, il = 8, [], 0, 0, 0, 0, 0
    while pos < len(data):
        ln, typ = struct.unpack('>I4s', data[pos:pos + 8]); body = data[pos + 8:pos + 8 + ln]; pos += 12 + ln
        if typ == b'IHDR': w, h, bd, ct, _, _, il = struct.unpack('>IIBBBBB', body)
        elif typ == b'IDAT': idat.append(body)
        elif typ == b'IEND': break
    assert bd == 8 and ct in (2, 6) and il == 0, '%s: 8비트 RGB/RGBA 비인터레이스만 지원 (bd=%d ct=%d il=%d)' % (path, bd, ct, il)
    bpp = 3 if ct == 2 else 4
    raw = zlib.decompress(b''.join(idat)); stride = w * bpp
    rows, prev, p = [], bytearray(stride), 0
    for _ in range(h):
        f = raw[p]; cur = bytearray(raw[p + 1:p + 1 + stride]); p += 1 + stride
        for i in range(stride):
            a = cur[i - bpp] if i >= bpp else 0; b = prev[i]; c = prev[i - bpp] if i >= bpp else 0
            if f == 1: cur[i] = (cur[i] + a) & 255
            elif f == 2: cur[i] = (cur[i] + b) & 255
            elif f == 3: cur[i] = (cur[i] + ((a + b) >> 1)) & 255
            elif f == 4:
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                cur[i] = (cur[i] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        rows.append(cur); prev = cur
    return w, h, bpp, rows


def write_png(path, w, h, bpp, rows):
    raw = b''.join(b'\x00' + bytes(r) for r in rows)
    def chunk(t, b): return struct.pack('>I', len(b)) + t + b + struct.pack('>I', zlib.crc32(t + b) & 0xffffffff)
    ihdr = struct.pack('>IIBBBBB', w, h, 8, 2 if bpp == 3 else 6, 0, 0, 0)
    open(path, 'wb').write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', ihdr) + chunk(b'IDAT', zlib.compress(raw, 6)) + chunk(b'IEND', b''))


def crop_scale(w, h, bpp, rows, x, y, cw, ch, k):
    x, y = max(0, x), max(0, y); cw, ch = min(cw, w - x), min(ch, h - y)
    out = []
    for yy in range(y, y + ch):
        src = rows[yy][x * bpp:(x + cw) * bpp]
        line = bytearray()
        for xx in range(cw):
            line += src[xx * bpp:(xx + 1) * bpp] * k
        for _ in range(k): out.append(line)
    return cw * k, ch * k, out


def find_strip(w, h, bpp, rows):
    """UI 띠 = 가운데 행/열에서 «모서리 배경색» 과 다른 픽셀의 범위."""
    def px(xx, yy): return tuple(rows[yy][xx * bpp:xx * bpp + 3])
    def diff(a, b): return sum(abs(i - j) for i, j in zip(a, b)) > 40
    my, mx = h // 2, w // 2
    bg_l, bg_t = px(0, my), px(mx, 0)
    xs = [xx for xx in range(w) if diff(px(xx, my), bg_l)]
    ys = [yy for yy in range(h) if diff(px(mx, yy), bg_t)]
    if not xs or not ys: return 0, 0, w, h
    return xs[0], ys[0], xs[-1] - xs[0] + 1, ys[-1] - ys[0] + 1


def main(a):
    if not a or a[0] in ('-h', '--help'): print(__doc__); return 0
    if a[0] == '--strip':
        src, dst = a[1], a[2]; k = int(a[3]) if len(a) > 3 else 3
        w, h, bpp, rows = read_png(src); x, y, cw, ch = find_strip(w, h, bpp, rows)
        print('띠: x=%d y=%d w=%d h=%d (원본 %dx%d) → ×%d' % (x, y, cw, ch, w, h, k))
    else:
        src, dst = a[0], a[1]; x, y, cw, ch = map(int, a[2:6]); k = int(a[6]) if len(a) > 6 else 3
        w, h, bpp, rows = read_png(src)
    ow, oh, out = crop_scale(w, h, bpp, rows, x, y, cw, ch, k)
    write_png(dst, ow, oh, bpp, out); print('저장:', dst, '%dx%d' % (ow, oh)); return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
