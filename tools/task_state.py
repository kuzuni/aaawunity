#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""«이미 끝난 일을 또 잡는» 헛구덩이를 막는 자 (T193).

왜 있나 — 오늘만 세 번 샜다.
  · 워커 J 가 **T149·T151** 을 선점했다가 05:41 커밋이 이미 다 고쳐 놓은 것을 코드에서 알았다(결정 455).
  · 내가 **T188** 을 선점했더니 31분 전에 워커 L 이 끝내 놓았다(결정 479).
  · 내가 **T161** 을 잡으려다 `git log -- <그 파일>` 한 줄로 `2ad1aeaf`(워커 L)를 봤다 — 이미 다 들어 있었다.

**뿌리는 «두 문서가 어긋난다» 는 것이다.** 선점은 `docs/ROUTINE.md` §2 **제목 줄**을 보고 하는데
(§0 4항: «선점 가능한 «가장 앞» 작업»), «끝났다» 는 사실은 `docs/PROGRESS.md` **상태 칸**에 적힌다.
제목의 ✅ 는 사람이 손으로 다는 것이라 자주 빠진다 — T161 은 PROGRESS 가 «✅ 완료 · CI 확인 끝» 인데
ROUTINE 제목에는 ✅ 가 없었다. 그러면 **열린 일로 보인다**. lock 이 없는 것도 신호가 못 된다
(끝내면 반납하므로 «없음» 이 «안 했음» 과 «다 했음» 을 못 가른다).

`check_task_rows.py`(결정 455)는 **PROGRESS 표 안에서** 같은 ID 가 두 줄로 갈라진 것을 잡는다.
이 자는 그 옆칸을 본다 — **ROUTINE 제목 ↔ PROGRESS 상태**가 어긋나는 자리.

**T205 로 하나 더 본다 — «한 번호가 두 작업을 가리킨다».** `docs/claims/README.md` 가
«한 번호는 한 작업만 가리킨다 · 번호 재사용 금지» 라고 규칙은 적어 뒀는데 **자가 없었다**.
2026-09-07 하루에 세 번 났다: T189(→T192) · T190 둘 · T204 둘. 그때마다 사람이 손으로 찾아
보고 커밋을 쓰거나(워커 A 17:11) 선점을 반납했다(워커 A 17:09). 번호가 갈라지면
`T204.lock` 이 «어느 일» 인지 못 가르고 두 워커가 같은 파일을 반대로 민다.
곁들여 «제목은 있는데 PROGRESS 행이 없는» 작업도 **알리기만** 한다(등재 중인 자리가 정상적으로 그 꼴이라
실패로 세지 않는다 — 다만 그 사이 `check_task_rows` 가 그 작업을 못 본다).

**T210 으로 «닫힌 꼴» 을 둘로 넓혔다** — ✅ 완료 말고 **⛔ 폐기·흡수**도 닫힌 자리다.
실측에서 넷(T25→T37 · T27→T38 · T30→T43 · T32→T42)이 표에는 ⛔ 인데 §2 제목엔 아무 표시가 없어
**열린 일로 보였다**. 폐기에는 ✅ 가 아니라 **⛔** 를 단다(✅ 를 달면 «했다» 는 뜻이 된다).
같은 회차에 파서도 고쳤다 — 칸을 `[^|]*` 로 자르면 본문의 **`\|`(escape 된 파이프)** 앞에서 끊겨
상태를 엉뚱한 조각에서 읽는다(내 T196 행이 «iPad\» 를 상태로 읽고 있었다 · `check_task_rows` 는 이미 고쳐져 있었다).

쓰는 법
  python3 tools/task_state.py --check     # 게이트: 번호 중복·제목↔상태 어긋남이 있으면 1 (ROUTINE §3 목록)
  python3 tools/task_state.py T161        # 선점 «직전» 한 줄 — 잡아도 되는지 판정 (0 = 잡아도 된다)
  python3 tools/task_state.py --list      # 전체 표
  python3 tools/task_state.py --self-test # 이 자가 실제로 잡는지
"""
import io
import os
import re
import subprocess
import sys
import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROUTINE = os.path.join(ROOT, "docs", "ROUTINE.md")
PROGRESS = os.path.join(ROOT, "docs", "PROGRESS.md")
CLAIMS = os.path.join(ROOT, "docs", "claims")

# «### T161 — …» · «### T188 ✅ — …» — 꼬리에 -gear 같은 갈래가 붙는 ID 는 이 표의 대상이 아니다(별개 작업).
HEAD = re.compile(r"^###\s+(T\d+)(?![\w-])(.*)$")
# ⚠ 칸을 «`|` 로 자르기» 는 그냥은 안 된다 — 본문에 **`\|` 로 escape 된 파이프**가 있다
# (내 T196 행의 `iPhone\|iPad\|iPod\|Android` 가 그것이다). `[^|]*` 는 그 앞에서 끊겨
# **상태 칸을 엉뚱한 조각에서 읽는다**(그 행은 «iPad\» 를 상태로 읽고 있었다 · T210 실측 · T201 과 같은 갈래).
# 그래서 «앞이 역슬래시가 아닌 `|`» 로만 자른다.
SPLIT = re.compile(r"(?<!\\)\|")
ID_IN_CELL = re.compile(r"^\s*(T\d+)(?![\w-])")


def cells(line):
    """표 한 줄 → (ID, 작업 칸, 상태 칸) · 표 줄이 아니거나 칸이 모자라면 None."""
    if not line.lstrip().startswith("|"):
        return None
    parts = SPLIT.split(line.rstrip("\n"))
    if len(parts) < 4:
        return None
    m = ID_IN_CELL.match(parts[1])
    return (m.group(1), parts[2], parts[3]) if m else None
STALE_MIN = 90          # docs/claims/README.md 의 «90분» 규약과 같은 값


def _fold(s):
    """접어 둔 중복 행 표시(✂·♻)는 상태로 안 센다 — check_task_rows.py 와 같은 규약."""
    return "✂" in s or "♻" in s


def row_ids_all(path=PROGRESS):
    """표에 **한 줄이라도** 있는 ID 전부(접힌 줄 포함) — «행이 없다» 와 «접힌 행만 있다» 를 가르는 데 쓴다(T206)."""
    out = set()
    with io.open(path, encoding="utf-8") as f:
        for line in f:
            c = cells(line)
            if c:
                out.add(c[0])
    return out


def routine_heads(path=ROUTINE, dups=None):
    """ID → (줄번호, 제목에 ✅ 가 있나, 제목 원문).

    `dups` 에 dict 를 주면 **번호가 두 번 이상 붙은 자리**를 ID → [(줄번호, 제목), …] 로 채운다(T205).
    """
    out, seen = {}, {}
    with io.open(path, encoding="utf-8") as f:
        for n, line in enumerate(f, 1):
            m = HEAD.match(line.rstrip("\n"))
            if not m:
                continue
            tid, rest = m.group(1), m.group(2)
            # 제목의 «맨 앞 토막»(첫 — 앞)에 있는 ✅ 만 «이 작업이 끝났다» 는 표시다.
            # 본문 쪽 «✅ 완료(코드 …)» 는 꼬리에 덧붙인 진행 기록이라 제목 표시와 구별한다.
            head_part = rest.split("—")[0]
            # 두 번째 칸 = 제목이 단 «닫힘 표시»(✅ 완료 · ⛔ 폐기·흡수) · 없으면 "" (T210 전에는 bool 이었다 · 참·거짓 쓰임은 그대로 산다)
            closed = "✅" if "✅" in head_part else ("⛔" if "⛔" in head_part else "")
            out.setdefault(tid, (n, closed, rest.strip()))
            seen.setdefault(tid, []).append((n, rest.strip()))
    if dups is not None:
        dups.clear()
        dups.update({t: v for t, v in seen.items() if len(v) > 1})
    return out


def progress_rows(path=PROGRESS):
    """ID → (줄번호, 상태 칸). 같은 ID 가 여러 줄이면 «가장 앞선 상태» 를 쓴다(✅ > 🔄 > ⬜)."""
    # ⛔ = «폐기 · 다른 번호에 흡수»(T210) — ✅ 와 마찬가지로 **닫힌** 꼴이라 선점 대상이 아니다.
    rank = {"✅": 4, "⛔": 3, "🔄": 2, "⬜": 1}
    out = {}
    with io.open(path, encoding="utf-8") as f:
        for n, line in enumerate(f, 1):
            c = cells(line)
            if not c:
                continue
            tid, work, state = c
            if _fold(work) or _fold(state):
                continue
            # ⚠ 칸 «어디에든» ✅ 가 있으면 완료로 세면 안 된다 — «🔄 코드 push … 로컬 게이트 전부 초록 ✅» 같은
            # 진행 기록에도 ✅ 가 흔하고, «**비평 회차 3 = 9.5 ✅**» 처럼 점수 표시로 쓰인 자리도 있다.
            # 상태는 칸 **맨 앞**에 적는 것이 이 표의 규약이라(§4) 앞의 굵게 표시만 벗기고 첫 글자를 본다.
            lead = state.strip().lstrip("*").strip()
            mark = lead[0] if lead[:1] in ("✅", "⛔", "🔄", "⬜") else ""
            prev = out.get(tid)
            if prev is None or rank.get(mark, 0) > rank.get(prev[1], 0):
                out[tid] = (n, mark, state.strip())
    return out


def lock_of(tid):
    """살아 있는 lock 이면 (SID, 나이(분)), 90분을 넘겼으면 (SID, 나이) + stale, 없으면 None."""
    p = os.path.join(CLAIMS, tid + ".lock")
    if not os.path.exists(p):
        return None
    try:
        txt = io.open(p, encoding="utf-8").read().strip().split()
        when = datetime.datetime.strptime(txt[0], "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=datetime.timezone.utc)
        sid = txt[1] if len(txt) > 1 else "(SID 없음)"
    except (OSError, ValueError, IndexError):
        return ("(못 읽음)", 0.0)
    age = (datetime.datetime.now(datetime.timezone.utc) - when).total_seconds() / 60.0
    return (sid, age)


def _git(args):
    try:
        return subprocess.run(["git"] + args, cwd=ROOT, stdout=subprocess.PIPE,
                              stderr=subprocess.DEVNULL, text=True, timeout=60).stdout
    except (OSError, subprocess.SubprocessError):
        return ""


def footprint(tid):
    """그 작업 번호가 **코드에** 남긴 자취 — 파일 목록과 «제목이 그 번호로 시작하는» 커밋들."""
    # 우리가 쓴 것만 본다 — `Assets/` 통째로 훑으면 에셋 팩의 **이진 파일**(.psd 등)이 우연히 걸린다(실측).
    out = _git(["grep", "-l", "-E", tid + r"([^0-9]|$)", "--",
                "Assets/Scripts", "Assets/Tests", "tools", "docs/ref"])
    files = [l for l in out.split("\n") if l]
    commits = []
    log = _git(["log", "--format=%h\t%cI\t%s", "-200"])
    pat = re.compile(r"^" + tid + r"(?![\w-])")
    for line in log.split("\n"):
        parts = line.split("\t")
        if len(parts) == 3 and pat.match(parts[2]):
            commits.append(parts)
    return files, commits


def verdict(tid, heads, rows):
    """(잡아도 되나, 한 줄 판정). «잡아도 되나» 가 거짓이면 그 회차에 그 번호를 선점하지 않는다."""
    lk = lock_of(tid)
    if lk and lk[1] < STALE_MIN:
        return False, "남의 lock 이 살아 있다 — %s · %d분 전(90분 규약)" % (lk[0], lk[1])
    hn, hdone, _ = heads.get(tid, (0, False, ""))
    pn, pmark, ptext = rows.get(tid, (0, "", ""))
    if hdone or pmark == "✅":
        where = "ROUTINE 제목이" if hdone else "PROGRESS 상태가"
        return False, "**끝난 일이다**(%s ✅) — 잡지 마라" % where
    files, commits = footprint(tid)
    if commits:
        h, when, subj = commits[0]
        return False, "⚠ **이미 손댄 흔적** — 커밋 %s(%s) «%s» · 코드 %d곳. 먼저 읽어라" % (
            h, when[:16], subj[:60], len(files))
    if files:
        return False, "⚠ **코드가 이 번호를 %d곳에서 가리킨다** — 먼저 읽어라: %s" % (
            len(files), ", ".join(files[:3]))
    if lk:
        return True, "lock 이 %d분 지났다(90분 초과) — 인계해서 잡아도 된다" % lk[1]
    return True, "깨끗하다 — 선점해도 된다"


def mismatches(heads, rows):
    """«PROGRESS 는 닫혔는데 ROUTINE 제목은 열려 보인다» = 선점 덫. T161·T188 이 빠진 구덩이다.

    **닫힌 꼴은 둘이다(T210)** — ✅ 완료 · ⛔ 폐기·흡수. 제목에 필요한 표시도 각각 그것이다
    (폐기된 일에 ✅ 를 달면 «했다» 는 뜻이 되므로 ✅ 로 대신하지 않는다 · 이미 ✅ 면 그대로 둔다).
    """
    bad = []
    for tid, (pn, mark, ptext) in sorted(rows.items(), key=lambda kv: int(kv[0][1:])):
        if mark not in ("✅", "⛔"):
            continue
        h = heads.get(tid)
        if h is None:
            continue        # ROUTINE §2 에 없는 작업(옛 표만 있는 것)은 선점 대상이 아니다
        if h[1] == "":
            bad.append((tid, h[0], pn, mark, ptext[:70]))
    return bad


def cmd_check(heads, rows, dups=None):
    rc = 0
    # 마지막 «✓ 요약» 줄에 한 번 더 실을 참고 사항들 — T231.
    #    까닭: 이 자의 «(참고 · 실패 아님)» 줄은 실패가 아니라서 종료 코드에 안 잡히고,
    #    워커들은 회차마다 출력을 `| tail -1` 로 자르거나 `>/dev/null` 로 버리고 종료 코드만 본다.
    #    그래서 갈래 ⓒ 가 **T223 을 이름으로 찍고 있었는데 아무도 못 봤고**, 닫힌 그 작업이
    #    표에서 «열린 급한 일» 로 읽혀 한 워커가 회차를 통째로 썼다(결정 640).
    #    막는 자로 올리지는 않는다 — 조율 결함은 알리기만 한다(결정 493). 대신 **끝줄에도 실어** 눈에 걸리게 한다.
    notes = []
    # ⓐ 한 번호가 두 작업을 가리키는가 (T205) — `docs/claims/README.md` 의 «한 번호는 한 작업만» 규칙.
    #    이것이 남으면 `T204.lock` 이 «어느 일» 인지 못 가르고, 두 워커가 같은 파일을 반대로 민다.
    if dups:
        rc = 1
        print("⛔ **한 번호가 여러 작업을 가리킨다** — `docs/claims/README.md`: «한 번호는 한 작업만 가리킨다».")
        print("   그대로 두면 그 번호의 lock 이 어느 일인지 못 가르고, 선점이 겹친다(오늘만 T189·T190·T204 세 번).")
        print("   고침: **늦게 등재된 쪽**을 다음 빈 번호로 옮긴다(제목·PROGRESS 행·본문 참조 함께).")
        for tid, places in sorted(dups.items(), key=lambda kv: int(kv[0][1:])):
            print("  · %s 가 %d곳:" % (tid, len(places)))
            for n, title in places:
                print("      ROUTINE.md:%d  «%s»" % (n, title[:80]))

    # ⓑ 제목은 있는데 PROGRESS 행이 없다 — **실패로 세지 않는다**(등재가 진행 중인 자리가 정상적으로 이 꼴이다).
    #    다만 그 사이에는 `check_task_rows` 가 아무것도 못 보므로 알려는 둔다(워커 A 의 17:11 보고가 그 자리다).
    #    ⚠ «행이 없다» 와 «접힌 행만 있다» 는 다른 일이다(T206) — 접힌 줄(✂·♻)은 «다른 번호로 옮겼다 · 취소됐다» 는
    #    **정상적으로 끝난 꼴**이라 채울 것이 없고, 진짜로 채워야 하는 것은 «행이 아예 없는» 쪽이다.
    #    둘을 한 목록에 섞으면 다음 워커가 이미 닫힌 T172·T189 를 «등재 중» 으로 읽고 손대러 간다.
    orphan = sorted(set(heads) - set(rows), key=lambda t: int(t[1:]))
    seen = row_ids_all()
    missing = [t for t in orphan if t not in seen]
    folded = [t for t in orphan if t in seen]
    if missing:
        print("· (참고 · 실패 아님) ROUTINE §2 제목은 있는데 PROGRESS 표에 **행이 아예 없는** 작업: %s" % " ".join(missing))
        notes.append("표에 행이 없는 작업 %s" % " ".join(missing))
        print("  등재 중이면 곧 채워진다. 오래 남아 있으면 그 사이 `check_task_rows` 가 그 작업을 못 본다(T96 이 그 꼴이었다).")
    if folded:
        print("· (참고 · 손댈 것 없음) 표에 **접힌 행(✂·♻)만** 있는 작업: %s — 다른 번호로 옮겼거나 취소된 자리다." % " ".join(folded))
        notes.append("접힌 행만 있는 작업 %s(손댈 것 없음)" % " ".join(folded))

    # ⓒ 상태 칸이 네 표시(✅ ⛔ 🔄 ⬜) 중 무엇으로도 **시작하지 않는** 행 — 어떤 자도 그 작업의 상태를 못 읽는다.
    #    실패로는 안 센다(표 규약을 어긴 것이지 일이 잘못된 것은 아니다) — 다만 그 행은 이 자와 check_task_rows 의 눈 밖이다.
    blind = sorted([t for t, (n, m, s) in rows.items() if m == ""], key=lambda t: int(t[1:]))
    if blind:
        print("· (참고 · 실패 아님) PROGRESS 상태 칸이 ✅·⛔·🔄·⬜ 중 무엇으로도 시작하지 않는 작업: %s" % " ".join(blind))
        notes.append("상태 칸이 표시로 시작 안 하는 작업 %s" % " ".join(blind))
        print("  그 행은 이 자도 `check_task_rows` 도 상태를 못 읽는다 — 칸 맨 앞에 표시를 하나 붙여 주면 된다(§4 규약).")

    # ⓖ **«⬜ 대기» 인데 그 번호의 lock 이 살아 있다** — 오늘 실제로 났다(T238 · 결정 653):
    #    `T233` 행이 «⬜ 대기 — 선점 안 됨» 인 채로 `T233.lock`(10:37)이 살아 있었고 **고침은 이미 push** 돼 있었다.
    #    이 꼴이 다른 어긋남보다 비싼 까닭은 하나다 — **⬜ 는 일감을 고르는 워커가 «정확히 그것만» 훑는 표시**라
    #    «남이 하는 중» 으로 읽혀 지나칠 여지가 없고 **곧장 중복 착수**로 간다(결정 506 이 값을 치른 그 사고).
    #    ⚠ **죽은 lock(90분 초과)은 안 찍는다** — 그 자리는 규약상 «잡아도 되는» 자리라 찍으면 거짓 경고가 된다.
    #    ⚠ **막지 않는다** — 조율 결함은 알리기만 한다(결정 493·627). 대신 notes 에 실어 끝줄에도 남긴다(T231).
    trap = []
    for tid, (n_, mark, _txt) in rows.items():
        if mark != "⬜":
            continue
        lk = lock_of(tid)
        if lk and lk[1] < STALE_MIN:
            trap.append((tid, n_, lk[0], lk[1]))
    if trap:
        trap.sort(key=lambda t: int(t[0][1:]))
        print("· (참고 · 실패 아님) **«⬜ 대기» 인데 lock 이 살아 있는 작업** — 잡으면 남의 일을 두 번 한다:")
        for tid, n_, sid, age in trap:
            print("  · %-5s PROGRESS.md:%d  ↔  docs/claims/%s.lock  %s · %d분 전" % (tid, n_, tid, sid, age))
        print("  고침: 임자가 상태 칸을 🔄 로 올린다(또는 일을 접었으면 lock 을 지운다).")
        notes.append("⬜ 인데 lock 살아 있음 %s" % " ".join(t[0] for t in trap))

    bad = mismatches(heads, rows)
    if not bad:
        if rc == 0:
            # T231 — 참고 사항을 **끝줄에도** 싣는다(`tail -1` 만 봐도 보이게). 종료 코드는 그대로 0 이다.
            print("✓ task_state: 번호 중복 0 · ROUTINE §2 제목과 PROGRESS 상태가 어긋나는 작업 0개 (제목 %d · 표 %d)%s"
                  % (len(heads), len(rows), (" · ⚠ 참고 %d건 — %s(위 줄에 자세히)" % (len(notes), " · ".join(notes))) if notes else ""))
        return rc
    if notes:
        # 실패로 끝나는 길에서도 참고 사항이 tail 에 남게 한다(빨강만 보고 나가는 회차가 더 흔하다).
        print("⚠ 참고 %d건 — %s" % (len(notes), " · ".join(notes)))
    print("⛔ **선점 덫** — PROGRESS 는 닫혔는데(✅ 완료 · ⛔ 폐기·흡수) ROUTINE §2 제목에는 표시가 없다.")
    print("   다음 워커는 이것을 «열린 일» 로 읽고 한 회차를 통째로 버린다(T161·T188 이 그랬다).")
    print("   고침: `docs/ROUTINE.md` 그 제목 줄의 ID 뒤에 **표에 적힌 그 표시**를 붙인다(✅ 는 ✅ · ⛔ 는 ⛔).")
    for tid, hn, pn, mark, ptext in bad:
        print("  · %-5s %s  ROUTINE.md:%d  ↔  PROGRESS.md:%d  «%s»" % (tid, mark, hn, pn, ptext))
    return 1


def cmd_list(heads, rows):
    for tid in sorted(set(heads) | set(rows), key=lambda t: int(t[1:])):
        ok, why = verdict(tid, heads, rows)
        print("%-5s %s %s" % (tid, "잡아도 됨" if ok else "잡지 마라 ", why))
    return 0


def cmd_one(tid, heads, rows):
    hn, hdone, htext = heads.get(tid, (0, False, "(ROUTINE §2 에 없다)"))
    pn, pmark, ptext = rows.get(tid, (0, "", "(PROGRESS 표에 없다)"))
    lk = lock_of(tid)
    files, commits = footprint(tid)
    print("== %s ==" % tid)
    print("  ROUTINE §2  : %s" % (htext[:100] or "(없다)"))      # 제목 원문에 이미 ✅ 가 들어 있다
    print("  PROGRESS 상태: %s" % (ptext[:100] or "(없다)"))
    print("  lock        : %s" % ("%s · %d분 전%s" % (lk[0], lk[1], " (90분 초과 = 죽은 lock)" if lk[1] >= STALE_MIN else "")
                                  if lk else "없음"))
    print("  코드 자취    : %d곳%s" % (len(files), (" — " + ", ".join(files[:5])) if files else ""))
    for h, when, subj in commits[:3]:
        print("  커밋        : %s %s %s" % (h, when[:16], subj[:70]))
    ok, why = verdict(tid, heads, rows)
    print("  → %s: %s" % ("잡아도 된다" if ok else "잡지 마라", why))
    return 0 if ok else 1


def self_test():
    """오늘 실제로 난 두 사고(T161 · T188)를 자가 잡는지 본다 — «늘 초록인 자» 와 «잡는 자» 를 가른다."""
    import tempfile
    import shutil
    tmp = tempfile.mkdtemp(prefix="task_state_selftest_")
    try:
        r = os.path.join(tmp, "ROUTINE.md")
        p = os.path.join(tmp, "PROGRESS.md")
        # ⓐ T161 꼴 — PROGRESS 는 ✅ 인데 제목에 ✅ 가 없다 → 잡아야 한다.
        # ⚠ «쓰인 적 없는 번호» 를 **글자 그대로** 적으면 안 된다 — 이 파일 자체가 `git grep` 에 걸려
        # «코드 자취가 있다» 로 판정되고 자기 검사가 스스로를 깬다(실제로 그랬다). 그래서 숫자로 만든다.
        free = "T%d" % 9000
        io.open(r, "w", encoding="utf-8").write(
            "### T161 — 장비 이름을 바꾼다 (주인 …)\n### %s — 아직 아무도 안 한 일\n" % free)
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| T161 | 장비 이름 | ✅ **완료 · CI 확인 끝** | 워커 L |\n"
            "| %s | 새 일 | ⬜ 대기 | |\n" % free)
        heads, rows = routine_heads(r), progress_rows(p)
        bad = mismatches(heads, rows)
        if [b[0] for b in bad] != ["T161"]:
            print("⛔ 자기 검사 실패 — T161 꼴(표는 ✅ · 제목은 ✅ 없음)을 못 잡았다: %s" % (bad,))
            return 1
        # ⓑ 제목에 ✅ 를 달면 조용해야 한다(거짓 경고 0).
        io.open(r, "w", encoding="utf-8").write(
            "### T161 ✅ — 장비 이름을 바꾼다 (주인 …)\n### %s — 아직 아무도 안 한 일\n" % free)
        if mismatches(routine_heads(r), rows):
            print("⛔ 자기 검사 실패 — 제목에 ✅ 를 달았는데도 걸린다(거짓 경고)")
            return 1
        # ⓒ 진짜 트리에서 «코드 자취» 판정 — T161 은 이미 손댄 흔적이 있고, 쓰인 적 없는 번호는 깨끗하다.
        # ⚠ git 을 못 쓰는 자리(내려받은 tarball 등)에서는 이 조각을 **건너뛴다** — 여기서 «실패» 로 끝내면
        # CI dotnet 잡이 빨개지고 그 사슬 끝의 gh-pages(주인 폰)까지 멈춘다(결정 493 이 정한 그 원칙).
        if not _git(["rev-parse", "HEAD"]).strip():
            print("✓ task_state --self-test: 문서 대조는 통과(git 이 없어 «코드 자취» 조각은 건너뛴다)")
            return 0
        H, R = routine_heads(), progress_rows()
        if verdict("T161", H, R)[0]:
            print("⛔ 자기 검사 실패 — 실제 트리의 T161 을 «잡아도 된다» 로 판정했다(코드가 이미 있다)")
            return 1
        if not verdict(free, H, R)[0]:
            print("⛔ 자기 검사 실패 — 쓰인 적 없는 %s 를 «잡지 마라» 로 판정했다(거짓 경고)" % free)
            return 1
        # ⓓ **한 번호가 두 작업을 가리키는 자리**(T205) — 오늘 T189·T190·T204 로 세 번 났고
        #    그때마다 사람이 손으로 찾아 보고했다. 가짜 문서로 «잡는가 · 하나뿐이면 조용한가» 를 본다.
        io.open(r, "w", encoding="utf-8").write(
            "### T161 ✅ — 장비 이름을 바꾼다 (주인 …)\n"
            "### %s — 소환 결과 상자\n### %s — 검은 아웃라인\n" % (free, free))
        d = {}
        routine_heads(r, dups=d)
        if list(d) != [free] or len(d[free]) != 2:
            print("⛔ 자기 검사 실패 — 같은 번호가 붙은 제목 둘을 못 잡았다: %s" % (d,))
            return 1
        io.open(r, "w", encoding="utf-8").write(
            "### T161 ✅ — 장비 이름을 바꾼다 (주인 …)\n### %s — 소환 결과 상자\n" % free)
        d = {}
        routine_heads(r, dups=d)
        if d:
            print("⛔ 자기 검사 실패 — 번호가 하나씩인데 중복이라 한다(거짓 경고): %s" % (d,))
            return 1

        # ⓔ **«행이 없다» ↔ «접힌 행만 있다»**(T206) — 접힌 줄(✂·♻)은 `progress_rows` 가 일부러 안 세므로
        #    둘이 한 목록에 섞였다. 그러면 다음 워커가 이미 옮겨졌거나 취소된 번호를 «등재 중» 으로 읽는다.
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n"
            "| T161 | 장비 이름 | ✂ **중복 행 — 살아 있는 기록은 위다** | 워커 L |\n")
        seen = row_ids_all(p)
        if "T161" not in seen:
            print("⛔ 자기 검사 실패 — 접힌 행도 «표에 있는 ID» 로 세야 한다")
            return 1
        io.open(p, "w", encoding="utf-8").write("| ID | 작업 | 상태 | SID |\n")
        if row_ids_all(p):
            print("⛔ 자기 검사 실패 — 행이 하나도 없는 표에서 ID 를 세었다(거짓 경고)")
            return 1

        # ⓔ **폐기(⛔)도 닫힌 꼴이다**(T210) — 표가 ⛔ 인데 제목에 표시가 없으면 잡아야 하고, ⛔ 를 달면 조용해야 한다.
        io.open(r, "w", encoding="utf-8").write("### T161 — 폐기된 일\n")
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| T161 | 폐기 | ⛔ 폐기 → T37 | |\n")
        got = mismatches(routine_heads(r), progress_rows(p))
        if [b[0] for b in got] != ["T161"] or got[0][3] != "⛔":
            print("⛔ 자기 검사 실패 — «표는 ⛔ · 제목엔 표시 없음» 을 못 잡았다: %s" % (got,))
            return 1
        io.open(r, "w", encoding="utf-8").write("### T161 ⛔ — 폐기된 일\n")
        if mismatches(routine_heads(r), progress_rows(p)):
            print("⛔ 자기 검사 실패 — 제목에 ⛔ 를 달았는데도 걸린다(거짓 경고)")
            return 1

        # ⓕ **escape 된 파이프**(`\|`)가 든 칸을 제대로 가르는가(T210) — 못 가르면 상태를 엉뚱한 조각에서 읽는다.
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| T161 | UA 가 `iPhone\\|iPad\\|iPod` 로 갈린다 | ✅ 완료 | |\n")
        got = progress_rows(p)
        if got.get("T161", (0, "", ""))[1] != "✅":
            print("⛔ 자기 검사 실패 — `\\|` 가 든 칸 때문에 상태를 못 읽었다: %s" % (got,))
            return 1

        # ⓖ T231 — «(참고 · 실패 아님)» 줄이 **마지막 요약 줄에도** 실리는가.
        #    이것이 이 회차의 고침이다: 워커가 `| tail -1` 로 잘라 읽어도 참고 사항이 눈에 걸려야 한다.
        #    (그 줄이 안 보여서 닫힌 T223 이 «열린 급한 일» 로 읽힌 사고가 실제로 났다 · 결정 640)
        io.open(r, "w", encoding="utf-8").write("### T161 ✅ — 장비 이름\n")
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| T161 | 장비 이름 | ✅ 완료 | |\n"
            "| %s | 표시가 없는 행 | 대기 중이라고만 적었다 | |\n" % free)
        buf = io.StringIO()
        keep = sys.stdout
        try:
            sys.stdout = buf
            rc_note = cmd_check(routine_heads(r), progress_rows(p))
        finally:
            sys.stdout = keep
        lines = [ln for ln in buf.getvalue().splitlines() if ln.strip()]
        if rc_note != 0:
            print("⛔ 자기 검사 실패 — 참고뿐인데 실패로 끝났다(조율 결함은 막지 않는다 · 결정 493): rc=%s" % rc_note)
            return 1
        if free not in lines[-1]:
            print("⛔ 자기 검사 실패 — 참고 줄이 마지막 요약에 안 실렸다(tail -1 로 못 읽는다): %r" % (lines[-1],))
            return 1

        # ⓗ **«⬜ 인데 살아 있는 lock»(T238 · 결정 653)** — 잡는가 · 그리고 **죽은 lock 은 안 잡는가**.
        #    두 칸을 다 묻는 까닭: 거짓 경고 쪽이 더 나쁘다(90분 지난 lock 자리는 규약상 «잡아도 되는» 자리라
        #    거기서 «잡지 마라» 를 찍으면 이 자가 오히려 일감을 막는다).
        claims = os.path.join(tmp, "claims")
        os.makedirs(claims, exist_ok=True)
        io.open(r, "w", encoding="utf-8").write("### %s — 새 일\n" % free)
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n| %s | 새 일 | ⬜ **대기 — 선점 안 됨** | |\n" % free)
        global CLAIMS
        keep_claims = CLAIMS
        try:
            CLAIMS = claims
            now = datetime.datetime.now(datetime.timezone.utc)
            def _write(minutes):
                when = (now - datetime.timedelta(minutes=minutes)).strftime("%Y-%m-%dT%H:%M:%SZ")
                io.open(os.path.join(claims, free + ".lock"), "w", encoding="utf-8").write(when + " sess-test\n")
            def _run():
                b = io.StringIO(); k = sys.stdout
                try:
                    sys.stdout = b; rc_ = cmd_check(routine_heads(r), progress_rows(p))
                finally:
                    sys.stdout = k
                return rc_, b.getvalue()
            _write(5)                      # 살아 있는 lock
            rc_live, out_live = _run()
            if rc_live != 0:
                print("⛔ 자기 검사 실패 — ⓖ 가 막았다(조율 결함은 알리기만 · 결정 493): rc=%s" % rc_live)
                return 1
            if "lock 이 살아 있는" not in out_live or free not in out_live:
                print("⛔ 자기 검사 실패 — «⬜ + 살아 있는 lock» 을 못 잡았다:\n%s" % out_live)
                return 1
            _write(STALE_MIN + 30)         # 죽은 lock
            rc_stale, out_stale = _run()
            if "lock 이 살아 있는" in out_stale:
                print("⛔ 자기 검사 실패 — 죽은 lock(90분 초과)인데 «잡지 마라» 로 찍었다(거짓 경고):\n%s" % out_stale)
                return 1
        finally:
            CLAIMS = keep_claims

        print("✓ task_state --self-test: 어긋난 짝을 잡고(T161) · ✅ 를 달면 조용하고 · 빈 번호는 통과하고 ·"
              " 같은 번호 두 제목을 잡고 · «행 없음 ↔ 접힌 행만» 을 가르고 · ⛔ 와 `\\|` 도 읽고 ·"
              " 참고 줄이 마지막 요약에도 실리고(T231) · «⬜ + 살아 있는 lock» 을 잡되 죽은 lock 은 안 잡는다(T238)")
        return 0
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


def main(argv):
    if "--self-test" in argv:
        return self_test()
    dups = {}
    heads, rows = routine_heads(dups=dups), progress_rows()
    if "--check" in argv:
        return cmd_check(heads, rows, dups)
    if "--list" in argv:
        return cmd_list(heads, rows)
    ids = [a for a in argv if re.fullmatch(r"T\d+", a)]
    if ids:
        return max(cmd_one(t, heads, rows) for t in ids)
    return cmd_check(heads, rows, dups)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
