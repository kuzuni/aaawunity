#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""파이썬 문자열의 **잘못된 escape** 를 잡는다 — 이 통이 볼 수 없는 것을 대신 본다 (T419).

왜 있나 — «내 통에서 조용하다» 가 «CI 에서 조용하다» 를 안 뜻한다.
  `"a \| b"` 처럼 파이썬이 모르는 escape 를 쓰면 값은 백슬래시를 그대로 담지만,
  파서가 그것을 **경고**한다. 그런데 그 경고의 얼굴이 판마다 다르다:
    · 3.6~3.11 : `DeprecationWarning` — **기본 필터에서 안 보인다**
    · 3.12~3.13: `SyntaxWarning`      — 로그에 뜬다(CI 러너가 여기다)
    · 3.14~    : **`SyntaxError`**     — 그 파일이 **아예 안 돈다**
  워커 통은 3.11 이라 첫째 칸에 있고 CI 는 둘째 칸에 있다 — 곧 워커는 제 손으로 만든 것을
  **제 통에서 볼 수 없다.** 2026-09-10 에 실제로 그랬다: `task_state.py`·`check_task_rows.py`
  두 독스트링이 매 런 CI 로그에 경고를 찍고 있었는데 아무도 로컬에서 못 봤다(결정 1186 이
  «안 고쳤다» 로 넘긴 자리). 셋째 칸으로 넘어가면 그 두 자는 **게이트에서 죽는다**.

무엇을 재나 — `tokenize` 로 **문자열 토큰만** 본다.
  ⚠ 정규식(`grep '\\|'`)으로 재면 안 된다. 이 레포에는 `\|` 가 **정당하게** 쓰인 자리가 많다:
    · 주석: `# 본문의 \| 는 글자다`            → 주석은 문자열이 아니다(파서가 안 본다)
    · raw 문자열: `re.compile(r"(?<!\\)\|")`   → raw 에서는 백슬래시가 글자다(경고 없음)
    · 제대로 escape: `"iPhone\\|iPad"`         → `\\` 는 올바른 escape 다
  셋 다 **고칠 것이 없는데** 정규식은 셋 다 잡는다 — 그러면 이 자가 우는 날 아무도 안 읽는다
  (T330 이 값을 치른 그 자리). 그래서 파서가 읽는 것과 **같은 재료**를 읽는다.

⚑ **이 자는 막는다(rc=1).** T415 의 «이력에만 있는 번호» 는 알리기만 하는데(재료가 «커밋 제목의
  낱말» 이라 무를 수 있다), 이 자의 재료는 **파서가 읽는 그 문자열 자체**라 거짓 경고가 원리적으로 없다.
  **자의 세기는 그 자가 보는 재료의 단단함을 넘지 않는다.**

쓰는 법
  python3 tools/check_py_escapes.py            # 게이트: 잘못된 escape 가 있으면 1
  python3 tools/check_py_escapes.py --self-test # 이 자가 실제로 잡는지(그리고 안 울어야 할 셋에 안 우는지)
"""
import io
import os
import sys
import tokenize

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SKIP_DIRS = {".git", "Library", "obj", "bin", "node_modules", ".aaaw-src", "Temp", "Logs"}

# 파이썬이 아는 escape 의 «다음 글자» 전부.
# 줄 끝 백슬래시(줄 잇기)도 올바른 자리라 개행을 넣는다.
VALID_NEXT = set("\n\r\\'\"abfnrtv01234567xNuU")


def bad_escapes_in_source(src, name="<src>"):
    """(줄번호, «\\X») 목록 — raw 아닌 문자열 토큰 안의 잘못된 escape."""
    out = []
    rl = io.BytesIO(src.encode("utf-8")).readline
    for t in tokenize.tokenize(rl):
        if t.type != tokenize.STRING:
            continue
        s = t.string
        prefix = s[:len(s) - len(s.lstrip("rRbBuUfF"))]
        if "r" in prefix.lower():
            continue                      # raw — 백슬래시가 글자다
        body = s[len(prefix):]
        i = 0
        while i < len(body) - 1:
            if body[i] == "\\":
                if body[i + 1] not in VALID_NEXT:
                    out.append((t.start[0], "\\" + body[i + 1]))
                i += 2                    # 올바른 escape 든 아니든 두 글자를 먹는다
                continue                  #   («\\|» 의 뒤 «|» 를 새 백슬래시로 오독하지 않는다)
            i += 1
    return out


def scan(root=ROOT):
    """레포의 `.py` 전부 → [(경로, 줄번호, «\\X»), …]"""
    hits = []
    for base, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
        for fn in sorted(files):
            if not fn.endswith(".py"):
                continue
            p = os.path.join(base, fn)
            rel = os.path.relpath(p, root)
            try:
                src = io.open(p, encoding="utf-8").read()
            except (OSError, UnicodeDecodeError) as e:
                hits.append((rel, 0, "읽기 실패 %s" % e))
                continue
            try:
                for ln, what in bad_escapes_in_source(src, rel):
                    hits.append((rel, ln, what))
            except tokenize.TokenError:
                # 토큰이 안 끊기는 파일은 이 자가 판단하지 않는다 — 그것은 파이썬이 따로 운다.
                continue
    return hits


def main(argv):
    if "--self-test" in argv:
        return self_test()
    hits = scan()
    if not hits:
        return 0
    print("⛔ **잘못된 escape** %d곳 — 파이썬 3.12 는 경고하고 **3.14 는 SyntaxError** 다." % len(hits))
    print("   이 통(파이썬 %d.%d)에서는 DeprecationWarning 이라 **기본 필터에서 안 보인다** — 그래서 이 자가 본다."
          % sys.version_info[:2])
    for rel, ln, what in hits:
        print("  · %s:%s  «%s»" % (rel, ln, what))
    print("   고침: 그 문자열을 raw 로(`r\"\"\"…\"\"\"` · `r\"…\"`) 하거나 백슬래시를 `\\\\` 로 적는다.")
    print("   ⚠ raw 로 바꾸기 전에 그 문자열에 **진짜 escape(`\\n`·`\\t`)가 있는지** 본다 — 있으면 뜻이 바뀐다.")
    return 1


def self_test():
    """잡아야 할 것 하나 + **안 잡아야 할 것 셋**(거짓 경고가 이 자를 죽인다)."""
    cases = [
        # (원본, 잡아야 하나, 무엇을 재나)
        ('x = "a \\| b"',            True,  "그냥 문자열 안의 «\\|»"),
        ('x = """머리\n본문 \\| 꼬리"""', True,  "여러 줄 독스트링 안의 «\\|»"),
        ('x = r"a \\| b"',           False, "raw 문자열 — 백슬래시가 글자다"),
        ('x = "a \\\\| b"',          False, "제대로 escape 된 «\\\\» + «|»"),
        ('# 본문의 \\| 는 글자다\nx = 1', False, "주석 — 파서가 문자열로 안 읽는다"),
        ('x = "줄 끝\\n탭\\t따옴표\\""', False, "진짜 escape 들에는 안 운다"),
    ]
    for src, want, why in cases:
        got = bool(bad_escapes_in_source(src))
        if got != want:
            print("⛔ 자기 검사 실패 — %s: 잡음=%s (기대 %s)\n   원본: %r" % (why, got, want, src))
            return 1
    # 줄번호를 제대로 대는가 — 틀린 줄번호는 «없다» 보다 나쁘다(다음 사람이 엉뚱한 데를 본다).
    got = bad_escapes_in_source('a = 1\nb = 2\nc = "x \\| y"')
    if got != [(3, "\\|")]:
        print("⛔ 자기 검사 실패 — 줄번호·글자를 못 댄다: %s (기대 [(3, '\\\\|')])" % (got,))
        return 1
    # 한 문자열에 둘이면 둘 다 센다(«하나 찾고 끝» 이면 고치다 만 파일이 초록이 된다).
    if len(bad_escapes_in_source('x = "\\| 그리고 \\»"')) != 2:
        print("⛔ 자기 검사 실패 — 한 문자열 안의 둘을 다 안 셌다")
        return 1
    print("✓ check_py_escapes --self-test: «\\|» 를 그냥 문자열·독스트링에서 잡고 · "
          "raw·제대로 escape·주석·진짜 escape 넷에는 안 울고 · 줄번호와 글자를 대고 · 한 줄에 둘이면 둘 다 센다 (T419)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
