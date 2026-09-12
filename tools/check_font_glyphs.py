#!/usr/bin/env python3
"""글꼴 글리프 게이트 (T224 5항 ⓐ) — 화면에 나가는 글자가 주인 글꼴(Jua)에 **실제로 있는가**.

사용:
  python3 tools/check_font_glyphs.py            # 게이트(없는 글자가 있으면 종료 코드 1)
  python3 tools/check_font_glyphs.py --list     # 글꼴이 가진 한글 음절 수·표본을 같이 찍는다
  python3 tools/check_font_glyphs.py --self-test  # 이 자가 갈래마다 제대로 우는가

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

**⚠ 이 자가 못 보는 자리** (T506 이 자기 검사를 세우며 글자로 박았다):
  · **코드가 글자를 이어 붙여 만드는 말**(`"등급 " + name`)의 `name` 쪽은 안 본다 — 리터럴만 본다.
  · **`desc`·`설명` 꼴 칸은 일부러 뺀다** — 화면에 안 나가는 자리라 없는 글자가 있어도 된다(T224 5항의 «쩄»).
  · 글꼴을 바꾸는 날 이 자의 답이 통째로 바뀐다 — 그것이 이 자가 **코드 상수가 아니라 파일**에 묻는 까닭이다.
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
    if '--self-test' in sys.argv[1:] or '--selftest' in sys.argv[1:]:
        return self_test()
    return run()


def run():
    """실제 검사 한 판 — `main()` 과 나눠 둔 까닭은 자기 검사가 **이것만** 부르기 위해서다
    (`main()` 을 부르면 `sys.argv` 를 다시 읽어 스스로를 끝없이 부른다 · `check_catalog_keys` 가 세우다 밟은 자리).
    ⚠ 함께 바꾼 것: 예전에는 `sys.exit(1)` 로 그 자리에서 죽었다 — 그러면 자기 검사가 **첫 빨강에서 같이 죽는다**."""
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

    # ⚑ 공허 방지 **둘** — 이 자에는 이것이 **없었다**(T506 실측). 이 자의 «없는 글자 0» 은
    #   두 방향으로 거짓이 될 수 있고, 둘 다 결과는 똑같이 «0» 이라 가르는 줄이 없으면 조용한 거짓이 된다:
    #     ① 아무것도 안 읽었다 — 길이 바뀌면 `files` 가 0 인데 «전부 Jua 에 있다» 가 찍힌다.
    #     ② 글꼴이 다 가진 척한다 — cmap 셈이 고장 나 음절 영역을 통째로 채우면 없는 글자가 영영 안 나온다.
    #       ②의 문턱은 **잰 값이 아니라 구조적 사실**이다(결정 930): 이 자가 생긴 까닭 자체가
    #       «한글 음절 영역 11,172자를 다 가진 글꼴이 아니다» 이고(머리글), 실측은 2,367자다.
    #       cmap 이 «11,172자 전부» 라고 답하면 그것은 글꼴이 아니라 **셈**이 그렇게 말하는 것이다.
    if files < 20:
        print('✗ check_font_glyphs: 못 쟀다 — 훑은 파일이 %d개뿐이다(길이 바뀌었나). '
              '이 수로 말하는 «없는 글자 0» 은 못 믿는다.' % files)
        return 1
    if syll < 100 or syll >= 11172:
        print('✗ check_font_glyphs: 못 쟀다 — cmap 이 한글 음절 %d자라고 답한다(실측 2,367자 · 영역 전체 11,172자). '
              'cmap 읽는 셈이 고장 났을 때 이 자는 «없는 글자 0» 으로 조용해진다.' % syll)
        return 1

    if bad:
        print('✗ check_font_glyphs: 글꼴에 없는 한글 %d자 — 화면에서 폭 0 으로 사라진다' % len(bad))
        for ch, wheres in sorted(bad.items()):
            print('  «%s» U+%04X  ←  %s' % (ch, ord(ch), ' · '.join(wheres[:4])))
        print('  고침 = 그 글자를 안 쓰는 문구로 바꾼다(글꼴 교체는 T224 5항이 «지금 더 나쁘다» 로 닫았다).')
        return 1
    print('✓ check_font_glyphs: 파일 %d개의 화면 문자열이 전부 Jua 에 있다 (글꼴 한글 %d자 · 설명 칸 제외)' % (files, syll))
    return 0


def _run_on(tmp, cs_files, json_files, have=None, nfill=25):
    """가짜 저장소 하나를 세우고 이 자를 그 위에서 돌린다 → (rc, 찍은 글).

    `have`(글꼴이 가진 코드포인트)는 진짜 Jua 를 쓰지 않고 **주입**한다 — 이 자기 검사가 묻는 것은
    «cmap 을 잘 읽나» 가 아니라 «읽은 것으로 옳게 판정하나» 다. cmap 셈 자체는 갈래 ⓘ 가 따로 지킨다.
    ⚠ 밑동을 `nfill` 개 채운다 — 공허 방지 문턱(파일 20)을 못 넘기면 재려던 갈래가 전부 «못 쟀다» 로 빨개진다.
    """
    import io as _io, contextlib
    scripts = os.path.join(tmp, 'Assets', 'Scripts')
    data = os.path.join(tmp, 'Assets', 'StreamingAssets', 'data')
    os.makedirs(scripts, exist_ok=True); os.makedirs(data, exist_ok=True)
    for i in range(nfill):
        open(os.path.join(scripts, '_fill%02d.cs' % i), 'w', encoding='utf-8').write('class F%d { }\n' % i)
    for nm, src in cs_files.items():
        open(os.path.join(scripts, nm), 'w', encoding='utf-8').write(src)
    for nm, doc in json_files.items():
        open(os.path.join(data, nm), 'w', encoding='utf-8').write(
            doc if isinstance(doc, str) else json.dumps(doc, ensure_ascii=False))
    g = globals(); old = (g['ROOT'], g['cmap'])
    g['ROOT'] = tmp
    if have is not None:
        g['cmap'] = lambda _p: have
    buf = _io.StringIO()
    try:
        with contextlib.redirect_stdout(buf):
            rc = run()
    finally:
        g['ROOT'], g['cmap'] = old
    return rc, buf.getvalue()


def self_test():
    """⚑ **이 자에는 자기 검사가 없었다** — T492 가 «자기 검사 없는 자 다섯» 으로 세어 둔 그 하나다.

    이 자는 **막는 자**다(`ci.yml` 의 `dotnet` 잡 · `continue-on-error` 없음 · `unity-test` 가 그 잡에 매달려 있다).
    그리고 세우고 보니 **공허 방지가 아예 없었다** — 게다가 이 자의 «0» 은 **두 방향**으로 거짓이 될 수 있다
    (아무것도 안 읽었다 · 글꼴이 다 가진 척한다). 둘 다 화면에서는 **폭 0 으로 글자가 사라지는** 것으로 나타난다.

    ⚠ 갈래 ⓕ·ⓖ 는 «고쳐야 할 것» 이 아니라 **한계를 못 박는 것**이다 — 머리글의 «못 보는 자리» 와 한 몸이다.
    """
    import shutil, tempfile
    # 가짜 글꼴 — 음절 영역의 **앞 2,000자**(가 … 넏)만 가졌다고 친다. ASCII 도 넣어 둔다.
    #   ⚠ 세우다 한 번 밟았다: 처음에 «가·나·다» 셋만 넣었더니 **내가 방금 보탠 공허 방지 ②**(`syll < 100`)에
    #     걸려 갈래 아홉이 전부 «못 쟀다» 로 빨개졌다 — 밑동이 문턱을 못 넘으면 재려던 것을 못 잰다
    #     (`check_catalog_keys` 가 세우다 밟은 그 자리와 같은 꼴이다 · T505 ⓑ).
    #   «쩄»(U+CEC4)은 이 범위 밖이라 «없는 글자» 갈래의 표본으로 그대로 쓴다.
    HAVE = set(range(0xAC00, 0xAC00 + 2000)) | set(range(0x20, 0x7F))
    cases = [
        ('ⓐ 가진 글자만 쓰면 조용하다', {'A.cs': 'class A { string s = "가나다"; }'}, {}, HAVE, 0, '전부 Jua 에 있다'),
        ('ⓑ 없는 글자를 쓰면 **글자·코드포인트·출처**로 운다',
         {'B.cs': 'class B { string s = "쩄다"; }'}, {}, HAVE, 1, 'U+CA44'),
        ('ⓑ² 그 울음에 **어느 파일**인지가 선다',
         {'B.cs': 'class B { string s = "쩄다"; }'}, {}, HAVE, 1, 'B.cs'),
        # ⚠ **부러뜨리기가 이 갈래의 구멍을 잡았다**(T506): 처음엔 주석을 «// 쩄» 처럼 **따옴표 없이** 적었는데,
        #   그러면 `strip_comments` 를 통째로 빼도 이 갈래가 안 울었다 — 뒤의 리터럴 정규식이 따옴표를 찾으니
        #   따옴표 없는 주석은 애초에 리터럴로 안 보인다. 곧 **재려던 장치를 안 지나는 표본**이었다.
        #   실제 모양(주석 처리한 코드 한 줄)으로 바꾸니 물린다 — «자가 운다» 를 봤다고 «그 장치를 쟀다» 가 아니다.
        ('ⓒ 주석 속 글자는 안 센다 — 그 줄을 세면 «주석 처리해 둔 코드» 가 빨강이 된다',
         {'C.cs': 'class C { // string old = "쩄";\n  /* string t = "쩄"; */\n  string s = "가"; }'}, {}, HAVE, 0,
         '전부 Jua 에 있다'),
        ('ⓓ json **값**은 센다',
         {}, {'x.json': {'name': '쩄'}}, HAVE, 1, 'x.json'),
        ('ⓔ json **키**는 안 센다 — 키는 화면에 안 나간다',
         {}, {'x.json': {'쩄': '가'}}, HAVE, 0, '전부 Jua 에 있다'),
        ('ⓕ ⚠ **일부러 뺀다** — `desc`·`설명` 칸은 화면에 안 나가서 없는 글자가 있어도 된다(T224 5항의 «쩄»)',
         {}, {'x.json': {'desc': '쩄', '설명': '쩄'}}, HAVE, 0, '전부 Jua 에 있다'),
        ('ⓖ ⚠ **한계** — 이어 붙이는 말의 변수 쪽은 못 본다(리터럴만 본다)',
         {'G.cs': 'class G { string s = "길 " + name; }'}, {}, HAVE, 0, '전부 Jua 에 있다'),
        ('ⓗ ASCII·기호는 이 자의 물음이 아니다(런타임 표가 지킨다)',
         {'H.cs': 'class H { string s = "HP +12% ★"; }'}, {}, HAVE, 0, '전부 Jua 에 있다'),
    ]
    bad = 0
    tmp = tempfile.mkdtemp(prefix='fontglyphs-')
    try:
        for i, (name, cs, js, have, want_rc, want_in) in enumerate(cases):
            rc, out = _run_on(os.path.join(tmp, 'c%02d' % i), cs, js, have=have)
            ok = (rc == want_rc) and (want_in is None or want_in in out)
            bad += 0 if ok else 1
            print('  %s %s (rc %d, 기대 %d)' % ('✔' if ok else '✘', name, rc, want_rc))
        # ⓘ 공허 방지 ① — 읽은 파일이 거의 없으면 «없는 글자 0» 이 아니라 «못 쟀다» 다.
        rc, out = _run_on(os.path.join(tmp, 'e1'), {}, {}, have=HAVE, nfill=2)
        ok = rc == 1 and '못 쟀다' in out and '2개뿐' in out
        bad += 0 if ok else 1
        print('  %s ⓘ 훑은 파일이 몇 개뿐이면 **«못 쟀다»** 로 운다 (rc %d, 기대 1)' % ('✔' if ok else '✘', rc))
        # ⓙ 공허 방지 ② — cmap 이 «음절 영역 전부» 라고 답하면 그것은 글꼴이 아니라 셈이 그러는 것이다.
        rc, out = _run_on(os.path.join(tmp, 'e2'), {'Z.cs': 'class Z { string s = "쩄"; }'}, {},
                          have=set(range(0xAC00, 0xD7A4)) | set(range(0x20, 0x7F)))
        ok = rc == 1 and '못 쟀다' in out and 'cmap' in out
        bad += 0 if ok else 1
        print('  %s ⓙ cmap 이 «한글 전부» 라고 답하면 «없는 글자 0» 이 아니라 **«못 쟀다»** 로 운다 (rc %d, 기대 1)'
              % ('✔' if ok else '✘', rc))
        # ⓚ 진짜 Jua 로 셈이 실제로 도는가 — 위 갈래들은 `have` 를 주입해서 cmap 셈을 안 건드린다.
        real = cmap(FONT)
        syll = sum(1 for c in range(0xAC00, 0xD7A4) if c in real)
        ok = 100 <= syll < 11172 and ord('가') in real
        bad += 0 if ok else 1
        print('  %s ⓚ 진짜 `Jua-Regular.ttf` 의 cmap 을 읽으면 한글 음절 %d자 (100 ≤ N < 11,172 여야 한다)'
              % ('✔' if ok else '✘', syll))
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    if bad:
        print('✗ check_font_glyphs --self-test: 갈래 %d건이 기대와 다르다' % bad)
        return 1
    print('✓ check_font_glyphs --self-test: 갈래 %d개가 전부 기대대로 갈린다 '
          '(ⓕ·ⓖ 는 «일부러/원리적으로 안 본다» 를 못 박은 갈래다 — 고침이 아니라 한계다)' % (len(cases) + 3))
    return 0


if __name__ == '__main__':
    sys.exit(main())
