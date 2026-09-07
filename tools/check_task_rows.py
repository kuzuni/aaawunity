#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""docs/PROGRESS.md 의 «같은 작업이 두 줄에 있고 상태가 어긋나는» 것을 잡는다 (T149·T150·T151·T165 사고 · 결정 455).

무엇이 문제였나
  표가 길어져 워커들이 아래쪽에 **새 표를 하나 더** 만들었고, 같은 작업이 두 줄에 남았다.
  위 줄은 «✅ 완료(확인 끝)» 인데 아래 줄은 «⬜ 대기 — 선점 안 됨» 이라, 아래 줄만 본 워커가
  **이미 끝난 일을 다시 잡는다**. 실제로 워커 J 가 T149·T151 을 선점했다가 코드를 읽고서야
  05:41 커밋(`1b215aa4` · 워커 C)이 셋을 이미 다 고쳐 놓은 것을 알았다 — 한 회차가 그냥 샜다.

무엇을 잡나
  ⓐ 같은 ID 가 여러 줄에 있고 ⓑ 한 줄은 «대기(⬜)» 인데 다른 줄은 «진행(🔄)» 이거나 «완료(✅)» 면 **실패**.
  이미 «✂»·«♻»(중복 행 표시)로 접어 둔 줄은 세지 않는다 — 그것이 이 사고를 막는 올바른 꼴이다.
  ID 는 표 첫 칸 글자 그대로 본다(«T63» 과 «T63-lobby» 는 다른 작업이다 · 꼬리의 ✅ 같은 표시는 떼고 본다).

쓰기: python3 tools/check_task_rows.py [--list]
  --list = 겹치는 줄을 전부 (실패가 아니어도) 보여 준다.
"""
import io
import os
import re
import sys

DOC = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "docs", "PROGRESS.md")

ROW = re.compile(r"^\|([^|]+)\|([^|]*)\|([^|]*)\|")
FOLDED = ("✂", "♻")          # 이미 «중복 행» 이라고 접어 둔 줄
WAITING = "⬜"
LIVE = ("✅", "🔄")
CLOSED = re.compile(r"✅\s*\*{0,2}\s*(종결|완료|눈 확인까지 끝)")   # 본문에 적힌 «닫았다» 표시 (ⓓ)


def rows(path):
    out = []
    with io.open(path, encoding="utf-8") as f:
        for n, line in enumerate(f, 1):
            m = ROW.match(line.rstrip("\n"))
            if not m:
                continue
            tid = m.group(1).strip()
            tid = tid.replace("✅", "").replace("🔄", "").replace("⬜", "").strip()
            if not re.match(r"^T\d+[A-Za-z0-9\-·ⓐ-ⓩ]*$", tid):
                continue
            out.append((n, tid, m.group(3).strip()))
    return out


def main():
    show_all = "--list" in sys.argv
    if not os.path.exists(DOC):
        print("PROGRESS.md 가 없다: " + DOC)
        return 1
    by_id = {}
    for n, tid, status in rows(DOC):
        by_id.setdefault(tid, []).append((n, status))

    # ⓒ 한 줄 안에서 어긋난 것 — 머리는 «⬜ 대기» 인데 본문에 «코드 push»·✅·🔄 가 있다.
    #    워커들이 상태 칸 «뒤» 에 회차 기록을 덧붙이면서 머리를 안 고쳐 생긴다(T170 실측 · 결정 500).
    #    표를 훑는 워커는 머리만 본다 — 그래서 끝난 일을 «선점 안 됨» 으로 읽고 다시 잡는다(T149·T151 사고와 같은 결).
    inner = []
    for tid, items in by_id.items():
        for n, status in items:
            if status.startswith(WAITING) and re.search(r"코드 push|✅|🔄", status):
                inner.append((tid, n, status))

    # ⓓ 머리는 «🔄 진행» 인데 본문에 «✅ 종결»·«✅ 완료» 가 적혀 있다 (T197 · 결정 500 의 짝).
    #    ⓒ 가 «⬜ → 실은 하는 중» 을 잡는다면 이 자는 «🔄 → 실은 끝났다» 를 잡는다. 사고는 한 번 더 나쁘다 —
    #    «🔄» 는 «남이 하는 중» 으로 읽히므로 다음 워커는 «남은 일 = 확인뿐» 만 보고 **이미 끝난 확인을 다시 한다**
    #    (실측: T159·T160·T177 은 워커 B 가 11:2X 에 PNG 로 닫았는데 개별 세 줄의 머리가 🔄 로 남아
    #     워커 F 가 15:3X 에 lock 을 셋 잡고 같은 확인을 통째로 되풀이했다 · 한 회차가 그냥 샜다).
    #    인용(«✅ 종결» 기록은…)과 남의 작업 이야기(«T156 ✅ 종결»)는 세지 않는다 — 앞 글자로 가른다.
    closed = []
    for tid, items in by_id.items():
        for n, status in items:
            if not status.lstrip("*_ ").startswith("🔄"):
                continue
            for m in CLOSED.finditer(status):
                pre = status[:m.start()]
                if pre[-1:] in ("«", "(", "“"):        # 인용·괄호 안 = 남의 이야기
                    continue
                if re.search(r"T\d+[^\s]*\s*$", pre):        # «T156 ✅ 종결» = 다른 작업 이야기
                    continue
                closed.append((tid, n, status))
                break

    dups = {k: v for k, v in by_id.items() if len(v) > 1}
    bad = []
    for tid, items in dups.items():
        live = [it for it in items if not any(f in it[1] for f in FOLDED)]
        waiting = [it for it in live if it[1].startswith(WAITING)]
        moving = [it for it in live if it[1].lstrip("*_ ").startswith(LIVE)]
        if waiting and moving:
            bad.append((tid, waiting, moving))

    if show_all:
        for tid in sorted(dups, key=lambda s: (len(s), s)):
            print("· " + tid + ": " + " · ".join(str(n) + "행 " + s[:24] for n, s in dups[tid]))

    if inner:
        print("한 줄 안에서 상태가 어긋난다 — 머리는 «⬜ 대기» 인데 본문에 «코드 push»·✅·🔄 가 있다(결정 500):")
        for tid, n, status in sorted(inner, key=lambda x: x[1]):
            print("  " + tid + " — " + str(n) + "행: " + status[:70].replace("\n", " ") + " …")
        print("고치는 법: 상태 칸 **머리**를 실제 상태(✅·🔄)로 바꾼다 — 뒤에 붙인 회차 기록은 그대로 둔다(이력이다).")
        return 1

    if closed:
        print("머리는 «🔄 진행» 인데 본문에는 «✅ 종결/완료» 가 적혀 있다 — 다음 워커가 «남은 일 = 확인뿐» 으로 읽고 끝난 확인을 되풀이한다(T197):")
        for tid, n, status in sorted(closed, key=lambda x: x[1]):
            print("  " + tid + " — " + str(n) + "행: " + status[:70].replace("\n", " ") + " …")
        print("고치는 법: 상태 칸 **머리**를 ✅ 로 바꾸고 «(이력)» 뒤에 옛 머리를 그대로 남긴다 —")
        print("            «닫았다» 는 본문 «뒤» 가 아니라 **머리**에 있어야 한다(표를 훑는 워커는 머리만 본다).")
        print("            정말로 아직 하는 중이면 본문의 그 «✅ 종결» 이 남의 작업 이야기인지 보고, 그렇다면 «T160 ✅ 종결» 처럼 작업 번호를 앞에 적는다.")
        return 1

    if bad:
        print("같은 작업이 두 줄에 있고 상태가 어긋난다 — «대기» 줄만 본 워커가 끝난 일을 다시 잡는다:")
        for tid, waiting, moving in sorted(bad, key=lambda x: x[0]):
            print("  " + tid + " — 대기 줄 " + ", ".join(str(n) + "행" for n, _ in waiting)
                  + " / 살아 있는 줄 " + ", ".join(str(n) + "행" for n, _ in moving))
        print("고치는 법 ⓐ 두 줄이 «같은 작업» 이면: 낡은 줄의 상태 칸을 «✂ 중복 행 — 살아 있는 기록은 N행이다» 로 바꾼다(지우지 않는다 · 이력이다).")
        print("고치는 법 ⓑ 두 줄이 «다른 작업인데 번호만 같으면» 접지 말고 **번호를 옮긴다** — 주인이 부른 쪽이 번호를 갖고 워커가 등재한 쪽이 다음 빈 번호로 간다")
        print("            (규약 «한 번호는 한 작업» · 결정 435 = T182 전례 · 결정 494 = T190 을 접었다가 되살린 사고). **먼저 두 줄의 제목을 읽고 ⓐ·ⓑ 를 가른다.**")
        return 1

    print("✓ check_task_rows: 표 행 " + str(sum(len(v) for v in by_id.values()))
          + "개 · 작업 " + str(len(by_id)) + "개 · «대기 ↔ 진행/완료» 어긋난 중복 0"
          + (" (중복 ID " + str(len(dups)) + "개는 전부 접혀 있거나 상태가 같다)" if dups else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
