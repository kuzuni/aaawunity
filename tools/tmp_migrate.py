#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""T207 ② 준비 — uGUI ``Text`` → TMP 타입 치환의 «대응표» 와 «기계 치환기».

지시서 §2 T207 이 ② 를 **한 커밋 · 한 워커 · fleet 전체가 비킨다** 로 못 박아 두었다.
그 한 커밋이 손대야 하는 자리가 지금 **파일 67개**라, 손으로 치면 어디를 빠뜨렸는지 아무도 모른다.
이 자가 하는 일은 둘이다:

  ``--report``  이번 공사의 **크기와 갈래**를 센다(무엇이 기계로 되고 무엇이 손을 타는가).
  ``--apply``   그 중 **갈래 ⓐ(이름만 바뀌는 것)만** 실제로 바꾼다. ⓑⓒ 는 **일부러 손 댄다** —
                값·타입이 달라지는 자리를 기계가 조용히 «되는 꼴» 로 바꿔 놓으면 화면이 틀린 채 초록이 된다.

■ 왜 «전부 자동» 이 아닌가 — 세 갈래로 가른 근거
  ⓐ **이름만 바뀜**: `supportRichText`→`richText` 처럼 뜻이 그대로다. 기계가 옳다.
  ⓑ **값·타입이 바뀜**: `alignment` 는 `TextAnchor`(9개) ↔ `TextAlignmentOptions`(비트 플래그)로 **표가 필요**하고
     (`UiKit.MapAlign` 이 이미 그 표다 · 거꾸로 가는 표는 아직 없다), `fontSize` 는 **int ↔ float** 라
     `int sz = t.fontSize` 가 조용히 안 컴파일된다. 기계가 이름만 갈면 «컴파일은 되는데 정렬이 틀린» 꼴이 나온다.
  ⓒ **대응이 없음**: `preferredWidth/Height`(TMP 는 `GetPreferredValues()`) 같은 자리다. 다시 써야 한다.

■ 낱말 «Text» 를 세는 법 — 주석·문자열은 안 센다
  이 저장소 주석은 한국어로 «uGUI `Text` 로 갈아 끼운다» 같은 말을 **439곳**에서 한다.
  주석까지 치환하면 기록이 뜻을 잃는다(그리고 report 의 숫자가 3배로 부풀어 공사 크기를 못 잰다).
  그래서 `//`·`/* */`·`"…"`·`'…'`·`@"…"` 를 지나친 **코드 자리만** 센다.

사용:
    tools/tmp_migrate.py --report            # 갈래별 자리 수(파일별)
    tools/tmp_migrate.py --report --sites    # 갈래 ⓑⓒ 자리를 파일:줄 로 전부 뽑는다(② 가 손으로 볼 목록)
    tools/tmp_migrate.py --apply             # 갈래 ⓐ 만 제자리 치환(ⓑⓒ 는 그대로 남는다)
    tools/tmp_migrate.py --apply --out DIR   # 원본을 두고 DIR 에 사본으로 (자가 점검용)
종료 코드 0 = 훑기 성공 · 2 = 인자 잘못.
"""
import argparse
import os
import re
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCAN = ("Assets/Scripts", "Assets/Tests")

# ── 갈래 ⓐ: 이름만 바뀐다(기계가 옳다) ─────────────────────────────────────────
# 왼쪽은 «.» 뒤에 오는 uGUI 속성 이름 · 오른쪽은 TMP 이름.
RENAME = {
    "supportRichText": "richText",
    "resizeTextForBestFit": "enableAutoSizing",
    "resizeTextMinSize": "fontSizeMin",
    "resizeTextMaxSize": "fontSizeMax",
}
# 타입 낱말 치환 — 읽는 자리는 `TMP_Text`(TextMeshProUGUI 의 밑동 · 둘 다 받는다),
# 세우는 자리(`AddComponent<Text>()`)만 구체 타입이어야 한다.
TYPE_READ = "TMP_Text"
TYPE_MAKE = "TextMeshProUGUI"

# ── 갈래 ⓑ: 값·타입이 바뀐다(반드시 사람이 본다) ──────────────────────────────
HAND_VALUE = {
    "alignment": "TextAnchor(9개) → TextAlignmentOptions(비트) · 표가 필요하다(UiKit.MapAlign 의 역방향)",
    "horizontalOverflow": "HorizontalWrapMode → textWrappingMode/overflowMode(두 속성으로 갈린다)",
    "verticalOverflow": "VerticalWrapMode → overflowMode(Truncate/Overflow 의 뜻이 다르다)",
    "fontSize": "int → float · `int x = t.fontSize` 가 안 컴파일된다(TextSize.Floor 자리 전부)",
    "font": "Font → TMP_FontAsset(TmpFont.Get() 이 그 자리다)",
    "lineSpacing": "배수(uGUI) → 글자 크기 % (TMP) · 0.75 를 그대로 옮기면 줄이 겹친다",
}
# ── 갈래 ⓒ: 대응이 없다(다시 쓴다) ───────────────────────────────────────────
HAND_NONE = {
    "preferredWidth": "TMP 는 GetPreferredValues().x",
    "preferredHeight": "TMP 는 GetPreferredValues().y",
    "cachedTextGenerator": "TMP 에는 TextGenerator 가 없다(textInfo 로 다시 쓴다)",
}


def strip_noncode(src):
    """주석·문자열 자리를 공백으로 덮은 사본(줄 수·열 수는 그대로 · 정규식이 코드만 보게)."""
    out = list(src)
    i, n = 0, len(src)
    while i < n:
        c = src[i]
        if c == '/' and i + 1 < n and src[i + 1] == '/':
            while i < n and src[i] != '\n':
                out[i] = ' '
                i += 1
        elif c == '/' and i + 1 < n and src[i + 1] == '*':
            while i < n and not (src[i] == '*' and i + 1 < n and src[i + 1] == '/'):
                if src[i] != '\n':
                    out[i] = ' '
                i += 1
            for _ in range(2):
                if i < n:
                    out[i] = ' '
                    i += 1
        elif c == '@' and i + 1 < n and src[i + 1] == '"':
            out[i] = ' '
            i += 2
            while i < n:
                if src[i] == '"' and i + 1 < n and src[i + 1] == '"':
                    out[i] = out[i + 1] = ' '
                    i += 2
                    continue
                if src[i] == '"':
                    out[i] = ' '
                    i += 1
                    break
                if src[i] != '\n':
                    out[i] = ' '
                i += 1
        elif c in '"\'':
            q = c
            out[i] = ' '
            i += 1
            while i < n and src[i] != q:
                if src[i] == '\\' and i + 1 < n:
                    out[i] = out[i + 1] = ' '
                    i += 2
                    continue
                if src[i] != '\n':
                    out[i] = ' '
                i += 1
            if i < n:
                out[i] = ' '
                i += 1
        else:
            i += 1
    return "".join(out)


def files():
    for base in SCAN:
        for d, _, fs in os.walk(os.path.join(ROOT, base)):
            for f in sorted(fs):
                if f.endswith(".cs"):
                    yield os.path.join(d, f)


# ── 낱말 «Text» 중 «타입 자리» 만 고른다 ─────────────────────────────────────
# `\bText\b` 로 세면 **틀린다** — 이 저장소에는 타입이 아닌 «Text» 가 널려 있다:
#   `catalog.Text("data.shop")`(카탈로그 자) · `UiKit.Text(...)`(우리 글자 공장) ·
#   `EvKind.Text`(전투 이벤트 갈래) · `public string Text;`(필드 이름) · `using System.Text;`.
# 그것까지 치환하면 **컴파일도 안 되고** 공사 크기도 3배로 부풀어 ② 가 예산을 잘못 잡는다.
# 그래서 «타입으로 쓰인 자리» 넷만 고른다: 제네릭 인자 · 선언(`Text x`) · 캐스트/`as`/`is` · `typeof`.
RE_MAKE = re.compile(r"AddComponent\s*<\s*(Text)\s*>")
RE_GENERIC = re.compile(r"<[^<>();{}]*>")            # `<Text>` · `<Text, int>` · `List<Text>`
RE_IN_GENERIC = re.compile(r"(?<![\w.])Text(?![\w])")
RE_DECL = re.compile(r"(?<![\w.])(Text)(?=\s+[A-Za-z_])")          # `Text _status;` · `Text txt = …`
RE_CAST = re.compile(r"(?<![\w.])(?:as|is)\s+(Text)(?![\w])")       # `g as Text`
RE_PAREN = re.compile(r"\(\s*(Text)\s*\)(?=\s*[A-Za-z_(])")         # `(Text)obj` — 캐스트만(빈 인자 `(Text)` 아님)
RE_TYPEOF = re.compile(r"typeof\s*\(\s*(Text)\s*\)")
RE_ARRAY = re.compile(r"(?<![\w.])(Text)(?=\s*\[\s*\])")            # `Text[]`


def type_spans(code):
    """코드에서 «타입으로 쓰인 Text» 의 (시작, 끝) 자리 — 만드는 자리(AddComponent)는 따로 돌려준다."""
    make, read = set(), set()
    for m in RE_MAKE.finditer(code):
        make.add((m.start(1), m.end(1)))
    for g in RE_GENERIC.finditer(code):
        for m in RE_IN_GENERIC.finditer(g.group()):
            span = (g.start() + m.start(), g.start() + m.end())
            if span not in make:
                read.add(span)
    for rx in (RE_DECL, RE_CAST, RE_PAREN, RE_TYPEOF, RE_ARRAY):
        for m in rx.finditer(code):
            span = (m.start(1), m.end(1))
            if span not in make:
                read.add(span)
    return make, read


def scan_one(path):
    """한 파일의 갈래별 자리 수와 (갈래 ⓑⓒ) 줄 목록."""
    src = open(path, encoding="utf-8").read()
    code = strip_noncode(src)
    rows = {"type": 0, "make": 0, "rename": 0, "value": [], "none": []}
    make, read = type_spans(code)
    rows["make"] = len(make)
    rows["type"] = len(make) + len(read)
    for name in RENAME:
        rows["rename"] += len(re.findall(r"\." + name + r"\b", code))
    lines = code.split("\n")
    for kind, table in (("value", HAND_VALUE), ("none", HAND_NONE)):
        for i, ln in enumerate(lines, 1):
            for name in table:
                if re.search(r"\." + name + r"\b", ln):
                    rows[kind].append((i, name, src.split("\n")[i - 1].strip()[:120]))
    return src, code, rows


def apply_one(src, code):
    """갈래 ⓐ 만 바꾼 사본(주석·문자열은 손대지 않는다 — `code` 를 자로 삼아 원본 위치만 고친다)."""
    # 뒤에서 앞으로 고친다 — 앞을 먼저 고치면 뒤 위치가 밀린다.
    edits = []
    make, read = type_spans(code)
    for a, b in make:
        edits.append((a, b, TYPE_MAKE))
    for a, b in read:
        edits.append((a, b, TYPE_READ))
    for name, new in RENAME.items():
        for m in re.finditer(r"\.(" + name + r")\b", code):
            edits.append((m.start(1), m.end(1), new))
    out = src
    for a, b, new in sorted(edits, key=lambda e: -e[0]):
        out = out[:a] + new + out[b:]
    n = len(edits)
    if n and "using TMPro;" not in out:
        # `using` 뭉치의 알파벳 자리에 끼운다(이 저장소 관례 · 없으면 첫 using 앞)
        us = [m for m in re.finditer(r"^using [^\n]+;\n", out, re.M)]
        if us:
            at = next((m.start() for m in us if m.group() > "using TMPro;"), us[-1].end())
            out = out[:at] + "using TMPro;\n" + out[at:]
        n += 1
    return out, n


def main():
    ap = argparse.ArgumentParser(add_help=True)
    ap.add_argument("--report", action="store_true", help="갈래별 자리 수를 센다")
    ap.add_argument("--sites", action="store_true", help="갈래 ⓑⓒ 자리를 파일:줄 로 전부 뽑는다")
    ap.add_argument("--apply", action="store_true", help="갈래 ⓐ 만 치환한다")
    ap.add_argument("--out", default=None, help="--apply 결과를 이 폴더에 사본으로(원본 안 건드림)")
    a = ap.parse_args()
    if not (a.report or a.apply):
        ap.print_help()
        return 2

    tot = {"type": 0, "make": 0, "rename": 0, "value": 0, "none": 0}
    per, hands = [], []
    changed = 0
    for p in files():
        src, code, r = scan_one(p)
        if not (r["type"] or r["rename"] or r["value"] or r["none"]):
            continue
        rel = os.path.relpath(p, ROOT)
        for k in ("type", "make", "rename"):
            tot[k] += r[k]
        tot["value"] += len(r["value"])
        tot["none"] += len(r["none"])
        per.append((rel, r))
        for kind in ("value", "none"):
            for ln, name, text in r[kind]:
                hands.append((rel, ln, kind, name, text))
        if a.apply:
            out, n = apply_one(src, code)
            if out != src:
                dst = p if not a.out else os.path.join(a.out, rel)
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                open(dst, "w", encoding="utf-8").write(out)
                changed += 1
            elif a.out:
                dst = os.path.join(a.out, rel)
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                shutil.copyfile(p, dst)

    if a.report:
        print("[tmp_migrate] T207 ② 공사 크기 — 파일 %d개(주석·문자열은 안 센다)" % len(per))
        print("  ⓐ 기계: 타입 «Text» %d(그 중 AddComponent %d) · 이름만 바뀌는 속성 %d"
              % (tot["type"], tot["make"], tot["rename"]))
        print("  ⓑ 손(값·타입 바뀜): %d" % tot["value"])
        print("  ⓒ 손(대응 없음): %d" % tot["none"])
        print("  ── 파일별(ⓐ/ⓑ/ⓒ) ──")
        for rel, r in sorted(per, key=lambda x: -(x[1]["type"] + x[1]["rename"])):
            print("   %4d/%3d/%2d  %s" % (r["type"] + r["rename"], len(r["value"]), len(r["none"]), rel))
        if a.sites:
            print("  ── 손을 타는 자리 전부(② 가 하나씩 본다) ──")
            for rel, ln, kind, name, text in hands:
                why = (HAND_VALUE if kind == "value" else HAND_NONE)[name]
                print("   %s:%d  .%s — %s" % (rel, ln, name, why))
                print("        %s" % text)
    if a.apply:
        where = a.out if a.out else "제자리"
        print("[tmp_migrate] 갈래 ⓐ 치환: 파일 %d개 고침 → %s" % (changed, where))
        print("  ⚠ ⓑ %d · ⓒ %d 자리는 **그대로 남았다** — 그 자리를 손으로 마치기 전에는 컴파일이 안 된다(일부러 그렇게 뒀다)."
              % (tot["value"], tot["none"]))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except BrokenPipeError:   # `| head` 로 잘라 볼 때
        sys.exit(0)
