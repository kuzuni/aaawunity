#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""회차 머리의 «⚑ 신규 주인 지시» 목록이 «지금» 을 거짓으로 말하는 것을 잡는 자 (T431).

왜 있나 — **이 목록은 구조적으로 낡는다.**
`docs/ROUTINE.md` 머리의 «⚑ 신규 주인 지시» 는 모든 워커가 **매 회차 첫머리에 읽는 세 자리** 중 하나다
(나머지 둘 = 👀 · ⛔ · 결정 1212·1219 가 잰 13.5 KB). 주인이 새로 말할 때마다 줄이 하나 늘고,
그 줄은 **«등재만»**(= 아직 아무도 안 잡았다 = 선점 가능)으로 태어나 언젠가 닫힌다.
그런데 닫는 것은 **§2 제목과 PROGRESS 상태 칸**이지 이 줄이 아니다 — 줄은 **가만히 있으면 거짓말이 된다.**

그 함정에는 이미 이름이 있다. 같은 목록 머리의 ⚠ 인용이
«줄이 닫히면 줄 안에 ✅ 를 적는다 — 닫힌 뒤에도 «선점 가능» 같은 **지금에 대한 말**이 남으면
다음 사람이 그것을 그대로 믿는다» 라고 적고, 까닭으로 **T225 가 종결 뒤 여덟 시간을 «선점 가능» 인 채
서 있어** 워커 하나가 그것을 집으려다 `task_state` 의 «잡지 마라» 와 부딪힌 사고(결정 1180)를 든다.

**그 경고문은 서 있는데, 2026-09-10 22:3X 실측에서 그 아래 일곱 줄이 그 경고를 안 지키고 있었다**
(T411·T405·T404·T401·T397·T396·T391 — 전부 §2 제목이 ✅ 인데 줄은 «등재만»). 나 자신이 «선점할 것이 없다»
회차에서 머리의 «T411 · 등재만» 을 읽고 잡으러 갔다가 `task_state` 에게 «끝난 일이다» 를 들었다.
⇒ **사람이 지키기로 한 규약은 사람이 일곱 번 어겼다. 그래서 자에게 넘긴다.**

무엇을 보나 — **줄의 «말» 과 `task_state` 의 «사실» 이 어긋나는가.** 두 방향 다 본다.
  ⓐ **끝난 일인데 열린 말이 서 있다** — 제목이 ✅·⛔ 인데 줄에 «등재만 · 선점 가능 · 선점 안 함 · 열림 · 🔄»
     같은 **지금에 대한 말**이 있고 그 줄 안에 «✅»(닫힘 표시)가 **없다** → 빨강. 다음 사람이 그것을 집는다.
  ⓑ **안 끝난 일인데 줄이 ✅ 를 달고 있다** — 반대 방향의 거짓말이라 **할 일이 사라진다**. → 빨강.

⚠ **글을 지우라는 자가 아니다.** ⓐ 의 고침은 «등재만» 을 **지우는 것이 아니라** 그 줄에 «✅ + 누가 닫았나»
를 **더하는 것**이다(T429 의 손 — 이력은 그대로 두고 지금을 덧붙인다). 그래야 이 목록이
«주인이 뭐라 했나» 의 기록이면서 동시에 «할 일이 있나» 로 읽히는 두 뜻을 다 산다.

쓰는 법
  python3 tools/check_head_list.py             # 게이트 (어긋나면 1)
  python3 tools/check_head_list.py --list      # 열일곱 줄을 판정과 함께 훑는다
  python3 tools/check_head_list.py --self-test # 이 자가 두 방향을 실제로 잡는지 (일부러 깨뜨려 본다)
"""
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import task_state as ts                                    # noqa: E402  (경로를 먼저 세워야 한다)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROUTINE = os.path.join(ROOT, "docs", "ROUTINE.md")

# 목록의 시작과 끝. 끝(«### 👀 …»)부터는 **같은 목록의 옛 부분**이라 여기서 안 본다 —
# 이력은 «찾을 때만» 보는 자리이고(결정 1212), 회차 머리에서 통째로 읽히는 것은 앞쪽뿐이다.
BEGIN = "## ⚑ 신규 주인 지시"
END = "### 👀"

# 줄 꼴: «- **(2026-09-10 · 주인 · T411 · 등재만)** «…» → …»
# ⚠ 괄호 안을 `[^)]*` 로 자르면 **머리 안의 괄호 하나에 자가 눈이 먼다** — 첫 `)` 에서 끊겨 `)**` 를 못 만나고
#   그 줄이 통째로 «없는 줄» 이 된다(조용히 통과 = 가장 나쁜 꼴). 내가 이 자를 만든 회차에 바로 그 일이 났다:
#   T397 줄에 «런 1004 `BattleWorldTests(10)` ✗ 0» 을 적었더니 열여섯 줄이던 훑기가 열여섯 줄 그대로인데
#   **T397 만 사라졌다**(빨강도 아니고 초록도 아니고 «안 봤다»). 그래서 «첫 `)`» 가 아니라 «첫 `)**`» 까지 센다.
BULLET = re.compile(r"^-\s+\*\*\((?P<head>.*?)\)\*\*")
# 위 자가 못 읽는 «- **(…» 꼴 — 조용히 지나가면 안 되므로 따로 세어 빨강으로 올린다.
BULLET_LOOSE = re.compile(r"^-\s+\*\*\(")
TID = re.compile(r"\b(T\d+)(?![\w-])")

# «지금에 대한 말» — 이것이 서 있으면 다음 사람은 «내가 지금 잡을 수 있다» 로 읽는다.
OPEN_WORDS = ("등재만", "선점 가능", "선점 안 함", "열림", "🔄")
CLOSED_MARKS = ("✅", "⛔")


def head_block(text):
    """ROUTINE 전문 → 회차 머리에서 읽히는 «⚑ 신규 주인 지시» 토막. 못 찾으면 ""."""
    try:
        s = text.index(BEGIN)
    except ValueError:
        return ""
    try:
        e = text.index(END, s)
    except ValueError:
        e = len(text)
    return text[s:e]


def entries(text):
    """토막 → [(줄번호, 작업ID, 괄호 안 머리, 줄 전문)] · ID 를 못 읽는 줄은 건너뛴다.

    ID 는 **괄호 안 머리에서만** 뽑는다 — 본문 뒤쪽에는 «T391 과 한 묶음» 처럼 남의 번호가 흔하고,
    그것을 이 줄의 임자로 읽으면 엉뚱한 작업의 상태로 판정한다.
    """
    out = []
    for i, ln in enumerate(text.split("\n"), 1):
        m = BULLET.match(ln)
        if not m:
            continue
        ids = TID.findall(m.group("head"))
        if ids:
            out.append((i, ids[0], m.group("head"), ln))
    return out


def unreadable(text):
    """«- **(» 로 시작하는데 이 자가 못 읽은 줄 → [(줄번호, 왜)].

    **못 읽은 줄을 조용히 지나가면 이 자는 그 줄에 대해 «늘 초록» 이 된다.** 그 꼴을 빨강으로 올린다.
    """
    out = []
    for i, ln in enumerate(text.split("\n"), 1):
        if not BULLET_LOOSE.match(ln):
            continue
        m = BULLET.match(ln)
        if not m:
            out.append((i, "괄호 머리를 못 닫았다(`)**` 가 없다)"))
        elif not TID.findall(m.group("head")):
            out.append((i, "괄호 머리에 작업 번호(T…)가 없다 — 어느 작업의 줄인지 못 가른다"))
    return out


def state_of(tid, heads, rows):
    """그 작업이 «닫힌 꼴» 인가 — §2 제목(✅·⛔) 또는 PROGRESS 상태 칸(✅·⛔).

    둘 중 **하나라도** 닫혔으면 닫힌 것으로 본다(`task_state.verdict` 와 같은 손) —
    제목의 표시는 사람이 손으로 다는 것이라 자주 늦고, 그 사이에도 그 일은 이미 끝나 있다.
    표에도 제목에도 없는 번호는 None(이 자가 판정하지 않는다).
    """
    hmark = heads.get(tid, (0, "", ""))[1]
    pmark = rows.get(tid, (0, ""))[1]
    if tid not in heads and tid not in rows:
        return None
    return hmark in CLOSED_MARKS or pmark in CLOSED_MARKS


def scan(text, heads, rows):
    """→ (빨강 목록, 훑기 목록). 빨강 = (줄번호, ID, 갈래, 한 줄 설명)."""
    bad, seen = [], []
    for lineno, why in unreadable(head_block(text)):
        bad.append((lineno, "?", "ⓒ", "이 자가 그 줄을 못 읽었다 — %s (못 읽은 줄은 «안 본 줄» 이다)" % why))
    for lineno, tid, head, ln in entries(head_block(text)):
        closed = state_of(tid, heads, rows)
        opens = [w for w in OPEN_WORDS if w in ln]
        says_closed = any(m in ln for m in CLOSED_MARKS)
        kind = ""
        if closed is None:
            kind = "?"                                      # 표에도 제목에도 없다 — 등재 중일 수 있다(빨강 아님)
        elif closed and not says_closed:
            # T487 — **가름은 «열린 말이 있는가» 가 아니라 «닫혔다는 말이 없는가» 다.**
            #   옛 꼴은 `opens` 가 있어야만 울었는데, 이 목록에서 가장 흔한 꼴은 **아무 말도 없는 줄**이다
            #   («- **(날짜 · 주인 · T462 · 주인 자리 로컬 세션이 바로 했다)** «…»» — 열린 말 0개).
            #   그 줄은 «⚑ 신규 주인 지시» 라는 **할 일 목록**에 서 있으므로 표시가 없으면 **안 한 일로 읽힌다** —
            #   이 절의 머리글이 스스로 적어 둔 «줄은 가만히 있으면 거짓말이 된다» 가 바로 그 꼴인데 코드가 그것을 안 물었다.
            #   실측(2026-09-12 01:0X · T487): 워커 P 가 한 시간에 손으로 여섯 줄을 고쳤고 이 자는 그 나무에서 «어긋난 것 0» 이었다.
            #   그 뒤에도 같은 꼴이 여섯 줄 더 서 있었고, 옛 ⓐ 가 잡는 꼴은 **0줄**이었다.
            kind = "ⓐ"
            bad.append((lineno, tid, kind,
                        ("끝난 일인데 줄이 «%s» 라고 서 있다(닫힘 표시 없음) — 다음 사람이 이것을 집는다"
                         % " · ".join(opens)) if opens else
                        "끝난 일인데 줄에 **닫힘 표시가 없다** — 할 일 목록의 줄은 표시가 없으면 «안 한 일» 로 읽힌다"))
        elif not closed and says_closed:
            kind = "ⓑ"
            bad.append((lineno, tid, kind,
                        "안 끝난 일인데 줄이 «✅» 를 달고 있다 — 할 일이 목록에서 사라진다"))
        seen.append((lineno, tid, kind, head))
    return bad, seen


def _print_bad(bad):
    print("⛔ **머리 목록이 «지금» 을 거짓으로 말한다** — 모든 워커가 매 회차 첫머리에 읽는 자리다(결정 1180 의 그 함정).")
    for lineno, tid, kind, why in bad:
        print("   docs/ROUTINE.md:%d  %s %s — %s" % (lineno, kind, tid, why))
    print("   고침 ⓐ: 그 줄에 «✅»(+ 누가·어느 회차에 닫았는지)를 **더한다** — «등재만» 은 이력이니 지우지 않는다(T429 의 손).")
    print("   고침 ⓑ: ✅ 를 떼거나, 정말 끝났으면 §2 제목·PROGRESS 상태를 먼저 닫는다(`task_state.py <ID>`).")
    # ⚠ 마지막 한 줄은 «✗» 로 시작해야 한다 — 꼬리만 읽는 눈에 빨강과 초록이 똑같이 생기면 안 된다(T239 의 계약 · 결정 678).
    print("✗ check_head_list: 머리 목록에서 어긋난 줄 %d건 — 그 줄에 닫힘 표시를 더하거나 §2·표를 먼저 닫아라" % len(bad))


def cmd_check():
    text = io.open(ROUTINE, encoding="utf-8").read()
    if not head_block(text):
        # 목록이 통째로 사라졌거나 제목이 바뀌었다 — 조용히 통과하면 이 자가 «늘 초록» 이 된다.
        print("⛔ check_head_list: `%s` 를 못 찾았다 — 목록이 옮겨졌으면 이 자의 BEGIN 도 같이 고쳐라" % BEGIN)
        return 1
    heads, rows = ts.routine_heads(), ts.progress_rows()
    bad, seen = scan(text, heads, rows)
    if bad:
        _print_bad(bad)
        return 1
    unknown = sum(1 for e in seen if e[2] == "?")
    print("✓ check_head_list: 머리 목록 %d줄 · 줄의 말과 §2·표가 어긋난 것 0%s"
          % (len(seen), (" (· 참고 %d줄은 아직 표·제목에 없다 — 등재 중)" % unknown) if unknown else ""))
    return 0


def cmd_list():
    text = io.open(ROUTINE, encoding="utf-8").read()
    heads, rows = ts.routine_heads(), ts.progress_rows()
    bad, seen = scan(text, heads, rows)
    for lineno, tid, kind, head in seen:
        closed = state_of(tid, heads, rows)
        print("  %5d  %-6s %-8s %s" % (lineno, tid,
                                       ("끝났다" if closed else "열려 있다") if closed is not None else "표에 없다",
                                       head[:80]))
    print("  — %d줄 · 빨강 %d" % (len(seen), len(bad)))
    return 1 if bad else 0


def self_test():
    """일부러 깨뜨려 본다 — 두 방향을 다 잡는가, 그리고 옳은 줄에 짖지 않는가."""
    import tempfile
    import shutil
    tmp = tempfile.mkdtemp(prefix="head_list_selftest_")
    try:
        r, p = os.path.join(tmp, "ROUTINE.md"), os.path.join(tmp, "PROGRESS.md")
        # ⚠ 쓰인 적 없는 번호는 **숫자로 만든다** — 글자 그대로 적으면 이 파일이 `git grep` 에 걸린다
        #   (task_state 의 자기 검사가 실제로 그렇게 스스로를 깼다).
        done, open_ = "T%d" % 9001, "T%d" % 9002
        io.open(r, "w", encoding="utf-8").write(
            "### %s ✅ — 이미 끝난 일 (…)\n### %s — 아직 안 한 일 (…)\n" % (done, open_))
        io.open(p, "w", encoding="utf-8").write(
            "| ID | 작업 | 상태 | SID |\n|---|---|---|---|\n"
            "| %s | 끝난 일 | ✅ **완료** | 워커 F |\n"
            "| %s | 남은 일 | ⬜ 대기 | |\n" % (done, open_))
        heads, rows = ts.routine_heads(r), ts.progress_rows(p)

        def block(lines):
            return BEGIN + "\n\n" + "\n".join(lines) + "\n\n" + END + " 옛 부분\n"

        # ⓐ 끝난 일인데 «등재만» 이 서 있다 → 잡아야 한다.
        bad, _ = scan(block(["- **(2026-09-10 · 주인 · %s · 등재만)** «…» → …" % done]), heads, rows)
        if [b[2] for b in bad] != ["ⓐ"]:
            print("⛔ 자기 검사 실패 — 끝난 일의 «등재만» 을 못 잡았다: %s" % (bad,))
            return 1
        # ⓐ' 같은 줄에 ✅ 를 더하면 조용해야 한다(고침이 실제로 먹는가 · 거짓 경고 0).
        bad, _ = scan(block(["- **(2026-09-10 · 주인 · %s · 등재만 → ✅ 닫혔다 · 워커 F)** «…»" % done]), heads, rows)
        if bad:
            print("⛔ 자기 검사 실패 — ✅ 를 더했는데도 짖는다(거짓 경고): %s" % (bad,))
            return 1
        # ⓐ'' **T487 — 열린 말이 하나도 없는 줄**. 이 목록에서 가장 흔한 꼴이고 옛 자는 이것을 통째로 못 봤다
        #     (실측: 워커 P 가 손으로 고친 여섯 줄 · 그 나무에서 이 자는 «어긋난 것 0» 이었다).
        #     ⚠ 이 갈래가 없으면 위 ⓐ 를 «열린 말이 있을 때만» 으로 되돌려도 자기 검사가 통과한다 — 그래서 이 줄이 이 회차의 값이다.
        bad, _ = scan(block(["- **(2026-09-10 · 주인 · %s · 주인 자리 로컬 세션이 바로 했다)** «…»" % done]), heads, rows)
        if [b[2] for b in bad] != ["ⓐ"]:
            print("⛔ 자기 검사 실패 — 끝난 일인데 **아무 말도 없는** 줄을 못 잡았다: %s" % (bad,))
            return 1
        # ⓐ''' 그리고 그 줄에 ✅ 를 더하면 조용해야 한다.
        bad, _ = scan(block(["- **(2026-09-10 · 주인 · %s · 주인 자리 로컬 세션이 바로 했다 → ✅ 닫혔다)** «…»" % done]),
                      heads, rows)
        if bad:
            print("⛔ 자기 검사 실패 — 말 없는 줄에 ✅ 를 더했는데도 짖는다(거짓 경고): %s" % (bad,))
            return 1
        # ⓑ 안 끝난 일인데 ✅ 를 달고 있다 → 잡아야 한다.
        bad, _ = scan(block(["- **(2026-09-10 · 주인 · %s · ✅ 다 했다)** «…»" % open_]), heads, rows)
        if [b[2] for b in bad] != ["ⓑ"]:
            print("⛔ 자기 검사 실패 — 안 끝난 일의 거짓 ✅ 를 못 잡았다: %s" % (bad,))
            return 1
        # ⓒ 열린 일 + 열린 말 = 옳은 줄이다. 여기서 짖으면 «등재만» 을 못 쓰게 된다.
        bad, _ = scan(block(["- **(2026-09-10 · 주인 · %s · 등재만)** «…»" % open_]), heads, rows)
        if bad:
            print("⛔ 자기 검사 실패 — 열린 일의 «등재만» 에 짖었다(거짓 경고): %s" % (bad,))
            return 1
        # ⓓ 본문 뒤쪽의 남의 번호를 이 줄의 임자로 읽으면 안 된다 —
        #    «T391 과 한 묶음» 같은 꼬리가 이 목록에 실제로 있다.
        bad, _ = scan(block(["- **(2026-09-10 · 주인 · %s · 등재만)** «…» → … · %s 와 한 묶음" % (open_, done)]),
                      heads, rows)
        if bad:
            print("⛔ 자기 검사 실패 — 본문의 남의 번호로 판정했다: %s" % (bad,))
            return 1
        # ⓕ ⚠ 머리 안에 괄호가 하나 있어도 그 줄을 봐야 한다 — 이 회차에 실제로 T397 이 그렇게 사라졌다.
        bad, seen = scan(block(["- **(2026-09-10 · 주인 · %s · 등재만 · 런 1004 `Tests(10)` ✗ 0)** «…»" % done]),
                         heads, rows)
        if [b[2] for b in bad] != ["ⓐ"] or len(seen) != 1:
            print("⛔ 자기 검사 실패 — 머리 안의 괄호 하나에 눈이 멀었다(줄이 통째로 사라진다): %s / %s" % (bad, seen))
            return 1
        # ⓖ 그래도 못 읽는 꼴(괄호를 안 닫았다)은 **조용히 지나가면 안 된다** — 빨강으로 올라와야 한다.
        bad, _ = scan(block(["- **(2026-09-10 · 주인 · %s · 등재만 «…»" % done]), heads, rows)
        if [b[2] for b in bad] != ["ⓒ"]:
            print("⛔ 자기 검사 실패 — 못 읽은 줄을 조용히 지나갔다(그 줄에 대해 «늘 초록» 이 된다): %s" % (bad,))
            return 1
        # ⓔ 목록 밖(👀 아래 이력)은 안 본다.
        outside = BEGIN + "\n\n" + END + " 옛 부분\n- **(2026-09-10 · 주인 · %s · 등재만)** «…»\n" % done
        bad, seen = scan(outside, heads, rows)
        if bad or seen:
            print("⛔ 자기 검사 실패 — 👀 아래 이력까지 봤다: %s" % (seen,))
            return 1
        print("✓ check_head_list --self-test: 두 방향(ⓐ 끝난 일의 열린 말 · ⓑ 안 끝난 일의 거짓 ✅)과 "
              "«못 읽은 줄»(ⓒ)을 잡고, 옳은 줄 넷(머리 안 괄호 포함)에는 안 짖는다")
        return 0
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


def main(argv):
    if "--self-test" in argv:
        return self_test()
    if "--list" in argv:
        return cmd_list()
    return cmd_check()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
