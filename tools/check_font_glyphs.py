#!/usr/bin/env python3
"""글꼴 글리프 게이트 (T224 5항 ⓐ) — 화면에 나가는 글자가 주인 글꼴(Jua)에 **실제로 있는가**.

사용:
  python3 tools/check_font_glyphs.py            # 게이트(없는 글자가 있으면 종료 코드 1)
  python3 tools/check_font_glyphs.py --list     # 글꼴이 가진 한글 음절 수·표본을 같이 찍는다

왜 이 자가 따로 필요한가 — **`TextGlyphs.CanRender` 로는 구조적으로 못 잡는다.**
그 판정은 «한글 음절 영역(U+AC00~U+D7A3)이면 참» 인데, 그 영역은 **11,172자**이고
Jua 가 실제로 가진 것은 그 일부다. 즉 런타임 자(`TextAudit` → `TextGlyphs.Missing`)는
«Jua 에 없는 한글» 을 **영영 없다고 말하지 않는다** — 화면에서는 폭 0 으로 조용히 사라지는데도.
그래서 판정을 **글꼴 파일의 cmap 실측**으로 옮긴다(코드 상수가 아니라 파일이 답한다).

무엇을 훑나 — «화면에 나가는 문자열» 만
  ⓐ `Assets/Scripts/**/*.cs` 의 **문자열 리터럴**(주석·`///` 문서 주석은 뺀다 — 화면에 안 나간다)
  ⓑ `Assets/StreamingAssets/data/*.json` · `Assets/KkomaKnight/*.json` 의 **값**
     (키는 뺀다 · `desc`·`설명`·`note`·`comment` 처럼 설명 칸도 뺀다 — T224 5항이 «쩄» 하나를 그렇게 걸렀다)
  한글 음절만 센다 — ASCII·기호는 `TextGlyphs.Safe` 가 이미 거르고(표), 그 표는 런타임 자가 지킨다.
"""
import json, os, re, struct, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
FONT = os.path.join(ROOT, 'Assets', 'Fonts', 'Jua-Regular.ttf')
# 설명·주석 칸 — 화면에 안 나가므로 여기 글자는 없어도 된다(T224 5항의 «쩄» 이 이 갈래다).
DESC_KEYS = ('desc', 'description', 'note', 'comment', 'memo', '설명', '주석')
HANGUL = lambda c: '가' <= c <= '힣'


def cmap(path):
    """TTF 의 cmap 에서 코드포인트 집합 — 형식 4(BMP)·12(전체) 만 읽는다(Jua·Noto 둘 다 4 를 갖는다)."""
    b = open(path, 'rb').read()
    n = struct.unpack('>H', b[4:6])[0]
    off = None
    for i in range(n):
        tag, _, o, _ = struct.unpack('>4sIII', b[12 + 16 * i:28 + 16 * i])
        if tag == b'cmap':
            off = o
            break
    if off is None:
        sys.exit('cmap 표가 없다: ' + path)
    tables = struct.unpack('>H', b[off + 2:off + 4])[0]
    subs = []
    for i in range(tables):
        pid, eid, so = struct.unpack('>HHI', b[off + 4 + 8 * i:off + 12 + 8 * i])
        subs.append((pid, eid, off + so))
    got = set()
    for pid, eid, so in subs:
        fmt = struct.unpack('>H', b[so:so + 2])[0]
        if fmt == 4:
            segx2 = struct.unpack('>H', b[so + 6:so + 8])[0]
            seg = segx2 // 2
            end = struct.unpack('>%dH' % seg, b[so + 14:so + 14 + segx2])
            sp = so + 16 + segx2
            start = struct.unpack('>%dH' % seg, b[sp:sp + segx2])
            dp = sp + segx2
            delta = struct.unpack('>%dh' % seg, b[dp:dp + segx2])
            rp = dp + segx2
            rng = struct.unpack('>%dH' % seg, b[rp:rp + segx2])
            for i in range(seg):
                if start[i] > end[i] or start[i] == 0xFFFF:
                    continue
                for c in range(start[i], end[i] + 1):
                    if rng[i] == 0:
                        g = (c + delta[i]) & 0xFFFF
                    else:
                        gi = rp + 2 * i + rng[i] + 2 * (c - start[i])
                        if gi + 2 > len(b):
                            continue
                        g = struct.unpack('>H', b[gi:gi + 2])[0]
                        if g:
                            g = (g + delta[i]) & 0xFFFF
                    if g:
                        got.add(c)
        elif fmt == 12:
            ngroups = struct.unpack('>I', b[so + 12:so + 16])[0]
            for i in range(ngroups):
                s, e, _ = struct.unpack('>III', b[so + 16 + 12 * i:so + 28 + 12 * i])
                got.update(range(s, min(e, 0x10FFFF) + 1))
    return got


def strip_comments(src):
    """C# 주석 제거 — 문자열 안의 «//» 를 지우지 않게 문자열을 먼저 지나간다."""
    out, i, n = [], 0, len(src)
    while i < n:
        c = src[i]
        if c == '"':
            j = i + 1
            if src[i - 1:i] == '@':                       # @"…" 축자 문자열
                while j < n:
                    if src[j] == '"' and src[j + 1:j + 2] != '"':
                        break
                    j += 2 if src[j] == '"' else 1
            else:
                while j < n and src[j] != '"':
                    j += 2 if src[j] == '\\' else 1
            out.append(src[i:j + 1]); i = j + 1; continue
        if c == '/' and src[i + 1:i + 2] == '/':
            i = src.find('\n', i)
            if i < 0: break
            continue
        if c == '/' and src[i + 1:i + 2] == '*':
            i = src.find('*/', i)
            if i < 0: break
            i += 2; continue
        out.append(c); i += 1
    return ''.join(out)


def scan_cs(path):
    src = strip_comments(open(path, encoding='utf-8').read())
    for m in re.finditer(r'"((?:[^"\\\n]|\\.)*)"', src):
        yield m.group(1)


def scan_json(node, key=None):
    if isinstance(node, dict):
        for k, v in node.items():
            yield from scan_json(v, k)
    elif isinstance(node, list):
        for v in node:
            yield from scan_json(v, key)
    elif isinstance(node, str):
        if key and any(d in str(key).lower() for d in DESC_KEYS):
            return                                        # 설명 칸은 화면에 안 나간다
        yield node


def main():
    have = cmap(FONT)
    syll = sum(1 for c in range(0xAC00, 0xD7A4) if c in have)
    bad = {}                                              # 글자 → [출처, …]

    def take(s, where):
        for ch in s:
            if HANGUL(ch) and ord(ch) not in have:
                bad.setdefault(ch, [])
                if where not in bad[ch]:
                    bad[ch].append(where)

    files = 0
    for base, _, names in os.walk(os.path.join(ROOT, 'Assets', 'Scripts')):
        for nm in names:
            if not nm.endswith('.cs'):
                continue
            p = os.path.join(base, nm); files += 1
            rel = os.path.relpath(p, ROOT)
            for s in scan_cs(p):
                take(s, rel)
    for sub in (('Assets', 'StreamingAssets', 'data'), ('Assets', 'KkomaKnight')):
        d = os.path.join(ROOT, *sub)
        if not os.path.isdir(d):
            continue
        for nm in sorted(os.listdir(d)):
            if not nm.endswith('.json'):
                continue
            p = os.path.join(d, nm); files += 1
            rel = os.path.relpath(p, ROOT)
            try:
                doc = json.load(open(p, encoding='utf-8'))
            except Exception:
                continue
            for s in scan_json(doc):
                take(s, rel)

    if '--list' in sys.argv:
        print('Jua-Regular.ttf 한글 음절 %d자 / 11172 (%.1f%%)' % (syll, syll * 100.0 / 11172))
    if bad:
        print('✗ check_font_glyphs: 글꼴에 없는 한글 %d자 — 화면에서 폭 0 으로 사라진다' % len(bad))
        for ch, wheres in sorted(bad.items()):
            print('  «%s» U+%04X  ←  %s' % (ch, ord(ch), ' · '.join(wheres[:4])))
        print('  고침 = 그 글자를 안 쓰는 문구로 바꾼다(글꼴 교체는 T224 5항이 «지금 더 나쁘다» 로 닫았다).')
        sys.exit(1)
    print('✓ check_font_glyphs: 파일 %d개의 화면 문자열이 전부 Jua 에 있다 (글꼴 한글 %d자 · 설명 칸 제외)' % (files, syll))


if __name__ == '__main__':
    main()
