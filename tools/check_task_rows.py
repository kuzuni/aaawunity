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
