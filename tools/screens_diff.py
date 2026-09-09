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


def diff(a, b, tol=8):
    """두 PNG 바이트 → (바뀐 픽셀 비율 %, 바뀐 자리 y 범위) · 못 읽으면 (None, None).

    tol = 채널 차가 이보다 커야 «바뀐 픽셀» 로 센다(같은 그림을 두 번 그려도 가장자리 한두 톤은 흔들린다).
    알파는 보지 않는다 — 화면 캡처는 전부 불투명이고, 알파만 흔들리는 것은 눈에 안 보인다.
    """
    pa, pb = read_png(a), read_png(b)
    if pa is None or pb is None:
        return None, None
    if pa[:3] != pb[:3]:
        return -1.0, None                                # 크기·채널이 다르다 = 통째로 바뀐 것
    w, h, ch, A = pa
    B = pb[3]
    n = 0
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
            for c in range(look):
                if abs(ra[x + c] - rb[x + c]) > tol:
                    n += 1
                    hit = True
                    break
        if hit:
            ymax = y
            if ymin < 0:
                ymin = y
    return 100.0 * n / (w * h), (ymin, ymax) if ymin >= 0 else None


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
        pct, span = diff(cur, old)
        if pct is None:
            unread.append(nm)
        else:
            changed.append((pct, nm, span))
            if pct >= 0:
                seen[nm[:-4]] = round(pct, 3)

    changed.sort(reverse=True)
    # «크게» 로 셀지는 두 잣대를 다 넘어야 한다: 절대값(--big) 과 «그 화면의 평소 폭»(있을 때만).
    def notable(pct, nm):
        if pct < 0:
            return True                                  # 크기가 달라진 것은 늘 크게
        if pct < big_min:
            return False
        u = usual_of(hist.get(nm[:-4]))
        return u is None or pct > max(u * 1.5, u + 0.5)

    big = [c for c in changed if notable(c[0], c[1])]
    usualy = [c for c in changed if c[0] >= big_min and c not in big]  # 크지만 «그 화면치고는 평소»
    mid = [c for c in changed if mn <= c[0] < big_min]                 # 애매한 자리 — 이름만
    tiny = len(changed) - len(big) - len(usualy) - len(mid)
    print(f'[그림차] 화면 {len(names)}장 · **크게 바뀜(≥{big_min:g}%) {len(big)}개** · 조금 바뀜 {len(mid)}개'
          f'{f" · 새 화면 {len(new)}개" if new else ""}'
          f'{f" · 잡음(<{mn:g}%) {tiny}개" if tiny else ""}'
          f'{f" · 못 읽음 {len(unread)}개" if unread else ""}  (옛 그림 = {ref})')
    for pct, nm, span in big[:top]:
        where = f' · y {span[0]}~{span[1]}' if span else ''
        amount = '크기가 다르다' if pct < 0 else f'{pct:.2f}%'
        print(f'[그림차]   {nm[:-4]} — {amount}{where}')
    if len(big) > top:
        print(f'[그림차]   … 그 밖 {len(big) - top}개')
    if usualy:
        print('[그림차]   평소 폭(그 화면은 늘 이만큼 흔들린다): '
              + ' · '.join(f'{nm[:-4]} {pct:.1f}%(평소 {usual_of(hist.get(nm[:-4])):.1f}%)' for pct, nm, _ in usualy[:6]))
    if mid:
        print('[그림차]   조금: ' + ' · '.join(f'{nm[:-4]} {pct:.1f}%' for pct, nm, _ in mid[:10]))
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


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
