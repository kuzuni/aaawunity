#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""결정 번호를 **push 직전에** 붙이는 자 (T331 · 결정 852 ⑦ · 912 꼬리의 제안을 자로 옮긴 것).

무엇이 문제였나
  워커가 회차 «시작» 무렵에 번호를 고르고 회차 «끝» 에 push 한다. 그 사이 10~30분 동안
  다른 워커 열 명이 같은 번호를 고르고 먼저 민다. 그러면 늦게 민 쪽이 규약대로 자기 줄을 옮겨야 하는데,
  그때마다 워커가 손으로 파이썬을 한 조각씩 쓴다 — 2026-09-09 하루에만 **아홉 번**이다(결정 862 ⑥ · 881 ⑥ · 891 · 912 · 925 …).
  손으로 옮기다 옆 번호까지 먹은 사고도 실제로 났다(결정 891 — «결정 881 ⑥» 이 «891 ⑥» 이 됐다).

이 자가 하는 일
  ⓐ `--next`(기본) — 지금 쓸 수 있는 번호 하나를 찍는다. `check_decisions.py --next` 와 같은 셈이다.
  ⓑ `--claim <문구> --sid <세션>` — **내 결정 줄 하나**를 찾아 번호를 «지금 최대 + 1» 로 고치고,
     문서 안의 «결정 <옛번호>» 참조 중 **내 것만** 같이 옮긴다.

«내 것만» 을 어떻게 가리나 (이 자의 핵심)
  참조 한 줄이 내 것인지는 **그 줄에 내 세션 id 가 같이 적혀 있는가**로 가른다.
  이 레포의 관례가 그것을 이미 만들어 뒀다 — 표 행도 §2 회차 블록도 «(sess-XXXX-XXXX · 워커 X · … · 결정 N)» 꼴로 쓴다.
  그래서 SID 가 없는 줄의 «결정 N» 은 **남의 참조**이므로 손대지 않고, 대신 **눈으로 볼 목록으로 찍어 준다**.
  ⚠ 이것이 이 자의 사각지대다 — 한 줄에 내 SID 와 남의 결정 번호가 같이 있으면 남의 것을 옮긴다.
    그래서 바꾼 줄을 전부 찍고, 안 바꾼 줄도 전부 찍는다(사람이 마지막으로 본다).

쓰기
  python3 tools/next_decision.py
  python3 tools/next_decision.py --claim "«뒤에 그린다» 를 자리로" --sid sess-2005-9317
  python3 tools/next_decision.py --claim "…" --sid … --dry     # 바꾸지 않고 무엇이 바뀔지만 본다
  python3 tools/next_decision.py --self-test                    # 이 자 자신의 자(파일을 안 건드린다)

돌아오는 값: 0 = 됐다 · 1 = 못 했다(문구가 0개거나 둘 이상 · 옮길 자리가 없다).
"""
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROGRESS = os.path.join(ROOT, "docs", "PROGRESS.md")
ROUTINE = os.path.join(ROOT, "docs", "ROUTINE.md")
HEAD = "## 워커 결정 기록"
NUM = re.compile(r"^(\d+)\. \*\*")
SID = re.compile(r"sess-[0-9a-z]{4}-[0-9a-z]+")


def entries(text):
    """(번호, 줄 index) 목록 — «워커 결정 기록» 절 안의 «N. **…» 줄만(check_decisions 와 같은 규약)."""
    out, inside = [], False
    for i, line in enumerate(text):
        if line.startswith("## "):
            inside = line.startswith(HEAD)
            continue
        if not inside:
            continue
        m = NUM.match(line)
        if m:
            out.append((int(m.group(1)), i))
    return out


def next_number(text):
    e = entries(text)
    return (max(n for n, _ in e) + 1) if e else 341


def plan(prog_lines, other_lines, phrase, sid):
    """무엇을 어떻게 바꿀지 미리 셈한다 — 파일은 안 건드린다(자기 검사도 이 함수를 쓴다).

    돌려주는 것: (옛번호, 새번호, 내 줄 index, [(파일표시, 줄 index, 옛줄, 새줄)], [남의 참조로 남긴 줄])
    """
    hits = [(n, i) for n, i in entries(prog_lines) if phrase in prog_lines[i]]
    if len(hits) != 1:
        raise LookupError("문구 «%s» 가 결정 줄 %d개에 맞는다 — 하나만 맞아야 한다" % (phrase, len(hits)))
    old, mine = hits[0]
    # ⚑ **내 줄을 빼고** 센다 — 내 번호까지 넣어 «최대 + 1» 을 하면 겹치지도 않았는데 한 칸씩 밀려난다.
    #   (겹침이 없으면 남의 최대 + 1 이 곧 내 번호이므로 아래에서 «옮길 것 없음» 이 된다.)
    others = [n for n, i in entries(prog_lines) if i != mine]
    new = (max(others) + 1) if others else old
    if new <= old:
        new = old          # 이미 내가 제일 큰 번호다 — 옮길 까닭이 없다
    changed, kept = [], []
    ref = re.compile(r"결정 %d\b" % old)
    for tag, lines in (("PROGRESS", prog_lines), ("ROUTINE", other_lines)):
        for i, line in enumerate(lines):
            if not ref.search(line):
                continue
            if sid in line:
                changed.append((tag, i, line, ref.sub("결정 %d" % new, line)))
            else:
                kept.append((tag, i, line.strip()[:120]))
    return old, new, mine, changed, kept


def self_test():
    """이 자 자신의 자 — 파일을 안 건드리고 <see cref="plan"/> 만 돌린다."""
    prog = [
        "## 워커 결정 기록", "",
        "900. **남의 줄** (sess-1111-1111 · 워커 B) — 뭐라뭐라.", "",
        "900. **내 줄 어쩌고** (sess-2222-2222 · 워커 A) — 뭐라뭐라.", "",
        "| T1 | 설명 | 🔄 어쩌고(sess-2222-2222 · 워커 A · 결정 900) | — | — | — |",
        "| T2 | 설명 | 🔄 남의 것(sess-1111-1111 · 워커 B · 결정 900) | — | — | — |",
    ]
    rout = ["> ▸ 회차 (sess-2222-2222 · 워커 A · 결정 900)", "> 남이 쓴 줄 — 결정 900 을 인용만 한다"]
    old, new, mine, changed, kept = plan(prog, rout, "내 줄 어쩌고", "sess-2222-2222")
    assert old == 900 and new == 901, (old, new)
    assert mine == 4, mine
    # 내 SID 가 있는 줄 둘만 바뀐다(PROGRESS 행 하나 · ROUTINE 블록 하나) — 결정 줄 자신은 참조가 아니라 머리라 안 센다
    assert len(changed) == 2, changed
    assert all("결정 901" in c[3] for c in changed), changed
    # 남의 줄 둘은 그대로 두고 «눈으로 볼 목록» 으로만 나온다
    assert len(kept) == 2, kept
    # 이미 내가 제일 큰 번호면 안 옮긴다
    prog2 = ["## 워커 결정 기록", "", "902. **내 줄 어쩌고** (sess-2222-2222) — 뭐.", ""]
    o2, n2, _, ch2, _ = plan(prog2, [], "내 줄 어쩌고", "sess-2222-2222")
    assert o2 == n2 == 902 and ch2 == [], (o2, n2, ch2)
    # 문구가 0개거나 둘 이상이면 아무것도 안 한다
    for bad in ("없는 문구", "**"):
        try:
            plan(prog, rout, bad, "sess-2222-2222")
        except LookupError:
            pass
        else:
            raise AssertionError("문구 «%s» 는 거절해야 한다" % bad)
    # 네 자리 — 2026-09-09 19:1X 에 번호가 1000 을 넘었다. «결정 100» 이 «결정 1004» 를 먹으면 남의 줄을 옮긴다.
    prog3 = [
        "## 워커 결정 기록", "",
        "1004. **내 줄 어쩌고** (sess-2222-2222) — 뭐.", "",
        "1005. **남의 줄** (sess-1111-1111) — 뭐.", "",
        "| T1 | 설명 | 🔄 (sess-2222-2222 · 결정 1004) | — | — | — |",
        "| T2 | 설명 | 🔄 (sess-2222-2222 · 결정 100) | — | — | — |",   # 세 자리 — 내 것이지만 이번에 옮기는 번호가 아니다
    ]
    o3, n3, _, ch3, _ = plan(prog3, [], "내 줄 어쩌고", "sess-2222-2222")
    assert o3 == 1004 and n3 == 1006, (o3, n3)
    assert len(ch3) == 1 and "결정 1006" in ch3[0][3], ch3     # «결정 100» 줄은 안 건드린다
    print("✓ next_decision 자기 검사 6가지 통과")
    return 0


def main(argv):
    if "--self-test" in argv:
        return self_test()
    prog = io.open(PROGRESS, encoding="utf-8").read().split("\n")
    if "--claim" not in argv:
        print(next_number(prog))
        return 0
    try:
        phrase = argv[argv.index("--claim") + 1]
        sid = argv[argv.index("--sid") + 1]
    except (IndexError, ValueError):
        print("쓰기: python3 tools/next_decision.py --claim \"<내 결정 줄의 문구>\" --sid <sess-...> [--dry]")
        return 1
    rout = io.open(ROUTINE, encoding="utf-8").read().split("\n")
    try:
        old, new, mine, changed, kept = plan(prog, rout, phrase, sid)
    except LookupError as e:
        print("✗ " + str(e))
        return 1
    if old == new:
        print("✓ 결정 %d 은 이미 제일 큰 번호다 — 옮길 것이 없다" % old)
        return 0
    print("결정 %d → %d (내 줄 PROGRESS:%d)" % (old, new, mine + 1))
    for tag, i, before, after in changed:
        print("  옮긴다 %s:%d  %s" % (tag, i + 1, after.strip()[:110]))
    for tag, i, line in kept:
        print("  ⚠ 남의 참조로 두었다(내 SID 가 그 줄에 없다) %s:%d  %s" % (tag, i + 1, line))
    if "--dry" in argv:
        print("(--dry — 파일은 안 바꿨다)")
        return 0
    prog[mine] = re.sub(r"^%d\." % old, "%d." % new, prog[mine])
    for tag, i, _, after in changed:
        (prog if tag == "PROGRESS" else rout)[i] = after
    io.open(PROGRESS, "w", encoding="utf-8").write("\n".join(prog))
    if any(t == "ROUTINE" for t, _, _, _ in changed):
        io.open(ROUTINE, "w", encoding="utf-8").write("\n".join(rout))
    print("✓ 옮겼다 — `python3 tools/check_decisions.py` 로 겹침 0 을 확인하고 push 한다")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
