#!/usr/bin/env python3
"""«이 커밋이 어느 화면 그림을 바꿨나» 를 말해 주는 자 (T282 · ROUTINE §2 T282).

왜 있나 — 결정 598: «자가 초록인데 화면이 틀린» 꼴은 자를 늘려서가 아니라 **전·후 그림을 맞대서** 잡힌다.
그 방법으로 T222(TMP 전환 뒤 한글이 음절 중간에서 끊긴 것)를 잡았는데, 그때까지 그 일은 사람이 손으로 했고
지금도 어느 자도 이것을 안 본다. §5 점수(`ui_score`)는 **이름표의 자리**만 재므로 글자·색·잘림은 못 본다.

사용:
  python3 tools/screens_diff.py [<새 PNG 디렉터리>] [--old-ref origin/screens]
                                [--min 0.2] [--big 3] [--top 5] [--touched-assets yes|no]
    · 새 PNG 디렉터리 = 기본 `ui-screens`(CI 유니티 잡이 그 자리에 찍는다).
    · 옛 그림 = `git show <ref>:<이름>.png` — 기본은 지난 런이 올려 둔 `origin/screens`.
      (CI 에서는 그 앞에 `git fetch --no-tags --depth=1 origin screens` 가 있어야 한다.)
    · --min(0.2%) 아래 = «잡음» 으로 접는다 · --min~--big(3%) = «조금 바뀜» 으로 **이름만** 한 줄 ·
      --big 이상 = 이름·비율·바뀐 y 범위까지 적는다. --top = 그 목록의 최대 줄 수(기본 5).
    · --touched-assets = 이 커밋이 `Assets/` 를 건드렸는가. **회차 2 에서 붙인 자다** —
      안 건드린 커밋인데 그림이 다르면 그것은 «이 커밋이 바꾼 것» 이 아니라 런마다 흔들리는 폭이고,
      자가 그 한 줄을 스스로 붙인다(실측 근거는 ROUTINE §2 T282 6항).
    · --history <이름> = **회차 3**. 화면별 폭을 그 파일에 쌓아 다음 런이 이어받는다
      (옛 값은 `git show <old-ref>:<이름>` 으로 읽고, 새 값은 <디렉터리>/<이름> 으로 써서 `screens` 에 같이 실린다).
      런 5개가 쌓인 화면은 «평소 폭»(최근 값들의 80분위)을 알게 되고, 그때부터 `--big` 을 넘겨도
      **제 평소 폭 안이면 «크게» 로 안 센다** — `27_toast` 처럼 늘 흔들리는 화면이 매 런 목록을 차지하는 것을 막는다.

이 자는 **알리기만 한다 — 늘 `exit 0`.** 그림이 바뀌는 것은 대개 «뜻한 변경» 이라 빨갛게 할 일이 아니고,
빨갛게 두면 워커가 «급하니 지나가자» 로 흐른다(결정 740 과 같은 셈). 사람이 «내가 안 바꾼 화면이 왜 바뀌었지» 를
그 자리에서 보게 하는 것이 값이다.

PNG 는 **표준 라이브러리만으로** 읽는다(zlib) — CI 러너에 PIL 이 있다고 믿지 않는다.
유니티 `EncodeToPNG` 는 8비트 RGBA · 인터레이스 없음이라 그 갈래만 읽으면 된다.
읽을 수 없는 꼴이면 그 화면은 «바이트가 다르다» 까지만 말하고 비율은 «—» 로 둔다.
"""
import json, os, struct, subprocess, sys, time, zlib


def _paeth(a, b, c):
    p = a + b - c
    pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
    if pa <= pb and pa <= pc:
        return a
    return b if pb <= pc else c


def read_png(data):
    """PNG 바이트 → (폭, 높이, 채널수, bytearray 픽셀). 못 읽으면 None.

    8비트 · 인터레이스 0 · 색타입 0(회색)·2(RGB)·6(RGBA) 만 읽는다(유니티가 내는 꼴).
    """
    if data[:8] != b'\x89PNG\r\n\x1a\n':
        return None
    pos, idat, w = 8, [], None
    while pos + 8 <= len(data):
        ln, typ = struct.unpack('>I4s', data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + ln]
        pos += 12 + ln                      # 길이4 + 종류4 + 몸통 + CRC4
        if typ == b'IHDR':
            w, h, depth, color, _comp, _filt, interlace = struct.unpack('>IIBBBBB', body[:13])
            if depth != 8 or interlace != 0 or color not in (0, 2, 6):
                return None
            ch = {0: 1, 2: 3, 6: 4}[color]
        elif typ == b'IDAT':
            idat.append(body)
        elif typ == b'IEND':
            break
    if w is None or not idat:
        return None
    raw = zlib.decompress(b''.join(idat))
    stride = w * ch
    out = bytearray(stride * h)
    prev = bytearray(stride)
    at = 0
    for y in range(h):
        ft = raw[at]; at += 1
        line = bytearray(raw[at:at + stride]); at += stride
        if ft == 1:                                     # Sub
            for i in range(ch, stride):
                line[i] = (line[i] + line[i - ch]) & 255
        elif ft == 2:                                   # Up
            for i in range(stride):
                line[i] = (line[i] + prev[i]) & 255
        elif ft == 3:                                   # Average
            for i in range(stride):
                a = line[i - ch] if i >= ch else 0
                line[i] = (line[i] + ((a + prev[i]) >> 1)) & 255
        elif ft == 4:                                   # Paeth
            for i in range(stride):
                a = line[i - ch] if i >= ch else 0
                c = prev[i - ch] if i >= ch else 0
                line[i] = (line[i] + _paeth(a, prev[i], c)) & 255
        elif ft != 0:
            return None
        out[y * stride:(y + 1) * stride] = line
        prev = line
    return w, h, ch, out


STRONG = 16        # T493 — «세기» 문턱. tol(8) 은 이 렌더의 잡음 바닥 **아래**라, 넓고 옅은 그늘이 수를 채운다.
#   실측 두 표본(회차마다 남겨 둔 PNG 로 전·후를 직접 맞댔다 · T490 2회차의 방법):
#     09_shop_1 (런 1135↔1136)  tol 8  3.112%  → tol 10  0.011%
#     27_toast  (런 1138↔1140)  tol 8 15.706%  → tol 16  0.102%  → tol 64 0.070%(진짜 다른 픽셀 439개)
#   ⚑ tol 자체는 **안 옮긴다** — 이력 열두 칸과 화면별 «평소 폭» 이 전부 tol 8 로 쌓였고, 옮기면 그 수들이 뜻을 잃는다.
#   그래서 «몇 개가 달라졌나»(tol) 옆에 «얼마나 달라졌나»(STRONG) 를 한 수 더 놓기만 한다. 판정(rc)은 안 건드린다.


def diff(a, b, tol=8, strong=STRONG):
    """두 PNG 바이트 → (바뀐 픽셀 비율 %, 바뀐 자리 y 범위, 세기 비율 %) · 못 읽으면 (None, None, None).

    tol = 채널 차가 이보다 커야 «바뀐 픽셀» 로 센다(같은 그림을 두 번 그려도 가장자리 한두 톤은 흔들린다).
    strong = 그중 «옅은 그늘이 아니다» 로 셀 문턱(T493). 같은 한 바퀴에서 세므로 비용이 거의 없다.
    알파는 보지 않는다 — 화면 캡처는 전부 불투명이고, 알파만 흔들리는 것은 눈에 안 보인다.
    """
    pa, pb = read_png(a), read_png(b)
    if pa is None or pb is None:
        return None, None, None
    if pa[:3] != pb[:3]:
        return -1.0, None, -1.0                          # 크기·채널이 다르다 = 통째로 바뀐 것
    w, h, ch, A = pa
    B = pb[3]
    n = 0
    ns = 0
    ymin, ymax = -1, -1
    look = min(ch, 3)                                    # RGB 만 본다
    stride = w * ch
    mA, mB = memoryview(A), memoryview(B)
    for y in range(h):
        base = y * stride
        ra, rb = mA[base:base + stride], mB[base:base + stride]
        if ra == rb:
            continue                                     # 줄이 통째로 같으면 픽셀을 안 센다(대개 이쪽이다 — 이 지름길이 없으면 느려서 못 쓴다)
        hit = False
        for x in range(0, stride, ch):
            d = 0
            for c in range(look):
                v = abs(ra[x + c] - rb[x + c])
                if v > d:
                    d = v
                    if d > strong:
                        break                            # 이미 «세다» — 더 볼 것이 없다(옛 판의 지름길을 그대로 둔다)
            if d > tol:
                n += 1
                hit = True
                if d > strong:
                    ns += 1
        if hit:
            ymax = y
            if ymin < 0:
                ymin = y
    px = w * h
    return 100.0 * n / px, (ymin, ymax) if ymin >= 0 else None, 100.0 * ns / px


def old_bytes(ref, name):
    r = subprocess.run(['git', 'show', f'{ref}:{name}'], stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
    return r.stdout if r.returncode == 0 else None


KEEP = 12          # 화면마다 최근 몇 런을 기억할지
NEED = 5           # 이만큼 쌓여야 «그 화면의 평소 폭» 을 말한다


def usual_of(hist):
    """그 화면이 «가만히 둬도 흔들리는 폭» = 최근 값들의 80분위(표본이 적으면 None)."""
    if not hist or len(hist) < NEED:
        return None
    s = sorted(hist)
    return s[int(0.8 * (len(s) - 1))]


def strength(pct, spct, strong=STRONG):
    """«그 수가 본론인가 그늘인가» 한 마디 — 크게 바뀐 줄에만 덧붙인다(T493).

    자기 검사가 이 함수를 그대로 부른다(판정을 print 안에 두면 시험이 못 만진다).
    ⚑ 말은 «세기가 거의 없다» 쪽에만 붙인다 — 진짜로 바뀐 화면에 군더더기를 안 붙이려는 것이다.
      기준은 «세기가 넓이의 10분의 1 아래» 로, 실측 두 표본(3.112 → 0.011 · 15.706 → 0.102)이
      100분의 1 쪽이라 넉넉히 잡았다.
    """
    if pct is None or spct is None or pct <= 0 or spct < 0:
        return ''
    if spct * 10 >= pct:
        return ''                                        # 세기가 넉넉하다 = 눈에 보이는 변화다. 아무 말도 안 붙인다.
    return f'(그늘 · 세기 {spct:.2f}% — 채널 {strong} 넘는 픽셀은 이만큼뿐)'


def notable(pct, screen, hist, big_min):
    """«크게 바뀜» 으로 셀 것인가 — 두 잣대를 다 넘어야 한다(절대값 · 그 화면의 평소 폭).

    자기 검사가 이 함수를 그대로 부른다(판정을 main 안에 두면 시험이 그 판정을 못 만진다).
    """
    if pct < 0:
        return True                                      # 크기가 달라진 것은 늘 크게
    if pct < big_min:
        return False
    u = usual_of(hist.get(screen))
    return u is None or pct > max(u * 1.5, u + 0.5)


def main(argv):
    d, ref, mn, top, big_min, touched, hist_name = 'ui-screens', 'origin/screens', 0.2, 5, 3.0, None, None
    rest = []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--old-ref' and i + 1 < len(argv):
            ref = argv[i + 1]; i += 2
        elif a == '--min' and i + 1 < len(argv):
            mn = float(argv[i + 1]); i += 2
        elif a == '--big' and i + 1 < len(argv):
            big_min = float(argv[i + 1]); i += 2
        elif a == '--top' and i + 1 < len(argv):
            top = int(argv[i + 1]); i += 2
        elif a == '--touched-assets' and i + 1 < len(argv):
            touched = argv[i + 1].strip().lower() in ('1', 'yes', 'true', 'y'); i += 2
        elif a == '--history' and i + 1 < len(argv):
            hist_name = argv[i + 1]; i += 2
        else:
            rest.append(a); i += 1
    if rest:
        d = rest[0]
    if not os.path.isdir(d):
        print(f'[그림차] 볼 것이 없다 — 디렉터리 «{d}» 가 없다(찍는 단계가 먼저 죽었으면 정상이다).')
        return 0

    names = sorted(f for f in os.listdir(d) if f.endswith('.png'))
    if not names:
        print(f'[그림차] 볼 것이 없다 — «{d}» 에 PNG 0장.')
        return 0

    # T282 회차 3 — 지난 런들이 남긴 «화면별 폭» 을 이어받는다. `screens` 는 force_orphan 이라
    # 커밋 이력이 없지만, 파일 자체가 값을 안고 다니면 `git show <ref>:diff.json` 으로 지난 값을 읽을 수 있다.
    hist = {}
    if hist_name:
        raw = old_bytes(ref, hist_name)
        if raw is None and os.path.exists(os.path.join(d, hist_name)):
            with open(os.path.join(d, hist_name), 'rb') as fh:   # 손으로 돌릴 때 — 옆에 둔 지난 파일을 읽는다
                raw = fh.read()
        if raw:
            try:
                got = json.loads(raw.decode('utf-8'))
                if isinstance(got.get('screens'), dict):
                    hist = {k: [float(x) for x in v][-KEEP:] for k, v in got['screens'].items() if isinstance(v, list)}
            except Exception:
                hist = {}                                # 못 읽으면 처음부터 다시 쌓는다 — 이 자 때문에 런이 멈추지는 않는다

    changed, new, unread, seen = [], [], [], {}
    for nm in names:
        with open(os.path.join(d, nm), 'rb') as fh:
            cur = fh.read()
        old = old_bytes(ref, nm)
        if old is None:
            new.append(nm)
            continue
        if old == cur:
            seen[nm[:-4]] = 0.0
            continue                                     # 바이트가 같으면 그림도 같다 — 여는 값이 없다
        pct, span, spct = diff(cur, old)
        if pct is None:
            unread.append(nm)
        else:
            changed.append((pct, nm, span, spct))
            if pct >= 0:
                seen[nm[:-4]] = round(pct, 3)            # 이력에 쌓는 값은 **종전 그대로**(tol 8) — 열두 칸의 뜻을 안 바꾼다

    changed.sort(reverse=True)
    # «크게» 로 셀지는 두 잣대를 다 넘어야 한다: 절대값(--big) 과 «그 화면의 평소 폭»(있을 때만) — `notable`.
    big = [c for c in changed if notable(c[0], c[1][:-4], hist, big_min)]
    usualy = [c for c in changed if c[0] >= big_min and c not in big]  # 크지만 «그 화면치고는 평소»
    mid = [c for c in changed if mn <= c[0] < big_min]                 # 애매한 자리 — 이름만
    tiny = len(changed) - len(big) - len(usualy) - len(mid)
    print(f'[그림차] 화면 {len(names)}장 · **크게 바뀜(≥{big_min:g}%) {len(big)}개** · 조금 바뀜 {len(mid)}개'
          f'{f" · 새 화면 {len(new)}개" if new else ""}'
          f'{f" · 잡음(<{mn:g}%) {tiny}개" if tiny else ""}'
          f'{f" · 못 읽음 {len(unread)}개" if unread else ""}  (옛 그림 = {ref})')
    for pct, nm, span, spct in big[:top]:
        where = f' · y {span[0]}~{span[1]}' if span else ''
        amount = '크기가 다르다' if pct < 0 else f'{pct:.2f}%'
        print(f'[그림차]   {nm[:-4]} — {amount}{strength(pct, spct)}{where}')
    if len(big) > top:
        print(f'[그림차]   … 그 밖 {len(big) - top}개')
    if usualy:
        print('[그림차]   평소 폭(그 화면은 늘 이만큼 흔들린다): '
              + ' · '.join(f'{nm[:-4]} {pct:.1f}%(평소 {usual_of(hist.get(nm[:-4])):.1f}%)' for pct, nm, _, _ in usualy[:6]))
    if mid:
        print('[그림차]   조금: ' + ' · '.join(f'{nm[:-4]} {pct:.1f}%' for pct, nm, _, _ in mid[:10]))
    for nm in new[:4]:
        print(f'[그림차]   {nm[:-4]} — **새 화면**(옛 그림에 없다)')
    # T282 회차 2 — «이 런이 그림을 바꿀 수 있었나» 를 같이 말한다. Assets 를 안 건드린 커밋인데도
    # 화면이 달라졌다면 그것은 이 커밋의 일이 아니라 **런마다 흔들리는 자리**다(실측: run 611 은 C# 0줄인데 열 장이 달랐다).
    if touched is False and (big or mid):
        print('[그림차] ⚠ 이 커밋은 `Assets/` 를 한 줄도 안 건드렸다 — 위는 «이 커밋이 바꾼 것» 이 아니라 **런마다 흔들리는 폭**이다(움직이는 화면·연출 위상).')
    elif touched is True and not big and not mid:
        print('[그림차] `Assets/` 를 건드린 커밋인데 달라진 그림이 없다 — 코드가 화면에 안 닿았거나(배선 빠짐) 원래 안 보이는 자리다.')
    if len(names) and len(big) > len(names) * 0.7:
        print('[그림차] ⚠ 거의 다 크게 바뀌었다 — 공통 요소(폰트·팔레트·해상도)를 건드린 것이다. 한 장을 눈으로 볼 것.')
    print('[그림차] (보고만 — 이 자는 빨갛게 하지 않는다 · T282 · 뜻한 변경이면 그대로 두면 된다)')

    # 이번 값을 이어 붙여 다시 내보낸다(다음 런이 `git show <ref>:<이 파일>` 로 읽는다).
    if hist_name:
        for k, v in seen.items():
            hist.setdefault(k, []).append(v)
            hist[k] = hist[k][-KEEP:]
        ready = sum(1 for v in hist.values() if len(v) >= NEED)
        try:
            with open(os.path.join(d, hist_name), 'w', encoding='utf-8') as fh:
                json.dump({'v': 1, 'keep': KEEP, 'need': NEED, 'utc': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
                           'screens': {k: v for k, v in sorted(hist.items())}}, fh, ensure_ascii=False)
            print(f'[그림차] 폭 기록 {hist_name} — 화면 {len(hist)}개 · «평소» 를 말할 수 있는 화면 {ready}개'
                  f'(런 {NEED}개부터 · 최근 {KEEP}런까지 기억)')
        except OSError as e:
            print(f'[그림차] 폭 기록을 못 남겼다({e}) — 다음 런은 처음부터 쌓는다(이 자는 그래도 돈다).')
    return 0


def _png(w, h, fill, box=None, color=None):
    """시험용 PNG 한 장을 표준 라이브러리로 만든다(8비트 RGBA · 필터 0)."""
    rows = []
    for y in range(h):
        row = bytearray()
        for x in range(w):
            c = color if (box and box[0] <= x <= box[2] and box[1] <= y <= box[3]) else fill
            row += bytes((c[0], c[1], c[2], 255))
        rows.append(b'\x00' + bytes(row))
    def chunk(t, b):
        return struct.pack('>I', len(b)) + t + b + struct.pack('>I', zlib.crc32(t + b) & 0xffffffff)
    ihdr = struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0)
    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', ihdr)
            + chunk(b'IDAT', zlib.compress(b''.join(rows))) + chunk(b'IEND', b''))


def self_test():
    """다섯 경우로 깨뜨려 본다 — 읽기 · 같은 그림 · 아는 크기의 변경 · 크기 다름 · «평소 폭» 접기.

    T278 이 제 자리에 적어 둔 까닭 그대로다: 이 자가 조용히 망가지면 «[그림차] 가 아무 말도 안 하는»
    꼴이 되는데, 그때는 런이 이미 지나간 뒤라 아무도 못 알아챈다. 순수 함수라 dotnet 잡에서 막는다.
    """
    ok = True

    a = _png(20, 10, (10, 20, 30))
    got = read_png(a)
    r1 = got is not None and got[0] == 20 and got[1] == 10 and got[2] == 4 and len(got[3]) == 20 * 10 * 4
    ok &= r1
    print('ⓐ PNG 읽기 —', 'OK' if r1 else '실패')

    p, span, sp = diff(a, _png(20, 10, (10, 20, 30)))
    r2 = p == 0.0 and span is None and sp == 0.0
    ok &= r2
    print('ⓑ 같은 그림 = 0.00% —', 'OK' if r2 else f'실패({p})')

    # 20×10 중 5×4(=20칸)만 바꾸면 정확히 10.00% 여야 한다. y 범위도 그 자리를 집어야 한다.
    b = _png(20, 10, (10, 20, 30), box=(2, 3, 6, 6), color=(200, 200, 200))
    p, span, sp = diff(a, b)
    r3 = abs(p - 10.0) < 1e-6 and span == (3, 6) and abs(sp - 10.0) < 1e-6
    ok &= r3
    print('ⓒ 아는 크기의 변경 = 10.00% · y 3~6 —', 'OK' if r3 else f'실패({p} {span})')

    p, _, sp = diff(a, _png(21, 10, (10, 20, 30)))
    r4 = p == -1.0 and sp == -1.0
    ok &= r4
    print('ⓓ 크기가 다르면 −1 —', 'OK' if r4 else f'실패({p})')

    # ⓔ 판정 함수를 그대로 부른다 — 같은 10% 라도 «늘 흔들리는 화면» 은 접히고 «조용하던 화면» 은 남는다.
    hist = {'shaky': [9, 10, 11, 12, 13, 10], 'calm': [0, 0, 0, 0, 0, 0]}
    r5 = (not notable(10.0, 'shaky', hist, 3.0)          # 평소 12% 인 화면의 10% = 접힌다
          and notable(10.0, 'calm', hist, 3.0)           # 평소 0% 인 화면의 10% = 크게
          and not notable(2.0, 'calm', hist, 3.0)        # --big 아래는 아무리 조용해도 «크게» 가 아니다
          and notable(-1.0, 'shaky', hist, 3.0))         # 크기가 달라진 것은 평소와 무관하게 크게
    ok &= r5
    print(f'ⓔ 평소 폭 접기(흔들림 평소 {usual_of(hist["shaky"])} · 조용 {usual_of(hist["calm"])}) —',
          'OK' if r5 else '실패')

    r6 = usual_of([1, 2, 3]) is None and notable(4.0, 'new', {'new': [0, 0, 0]}, 3.0)
    ok &= r6                                              # 표본이 모자라면 «평소» 를 말하지 않고 그냥 크게 센다
    print('ⓕ 표본 5런 미만이면 평소를 말하지 않는다 —', 'OK' if r6 else '실패')

    # ⓖ T493 — «넓고 옅은 것» 과 «좁고 진한 것» 을 가른다. 넓이만 보면 앞엣것이 이기는데, 사람이 보는 것은 뒤엣것이다.
    #    실측을 그대로 넣는다: 09_shop_1(3.112 → 세기 0.011) · 27_toast(15.706 → 세기 0.102).
    r7 = (strength(3.112, 0.011) and strength(15.706, 0.102)          # 그늘 = 말이 붙는다
          and not strength(10.0, 5.0)                                 # 세기가 넉넉하면 군더더기를 안 붙인다
          and not strength(10.0, 1.0)                                 # 딱 10분의 1 도 «넉넉» 쪽이다(경계)
          and strength(10.0, 0.99)                                    # 그 바로 아래가 그늘이다
          and not strength(-1.0, -1.0) and not strength(None, None))  # 크기 다름·못 읽음에는 아무 말도 안 붙인다
    ok &= bool(r7)
    print('ⓖ 넓고 옅은 것 ↔ 좁고 진한 것 (T493) —', 'OK' if r7 else '실패')

    # ⓗ 같은 그림 두 장을 «한 칸(10/255) 균일 이동» 시키면 tol 8 은 다 세고 세기(16)는 0 이어야 한다 —
    #    이것이 09_shop_1 에서 실제로 났던 꼴이다(결정 1371).
    faint = _png(20, 10, (20, 30, 40))
    p8, _sp8, s8 = diff(_png(20, 10, (10, 20, 30)), faint)
    r8 = abs(p8 - 100.0) < 1e-6 and s8 == 0.0
    ok &= r8
    print('ⓗ 한 칸 균일 이동 = 넓이 100% · 세기 0% —', 'OK' if r8 else f'실패({p8} {s8})')

    print('✓ screens_diff 자기 검사 통과' if ok else '✗ 자기 검사 실패')
    return 0 if ok else 1


if __name__ == '__main__':
    try:
        sys.exit(self_test() if '--self-test' in sys.argv[1:] else main(sys.argv[1:]))
    except BrokenPipeError:
        # 사람이 `| head` 로 잘라 읽는 자다 — 파이프가 닫혔다고 «자가 터졌다» 로 보이면 안 된다.
        try:
            sys.stdout.close()
        except Exception:
            pass
        sys.exit(0)
