#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""살아 있는 lock 이 «범위 열에 안 적힌 파일» 을 쥐고 있는 것을 알린다 (T347 · 워커 실측 2026-09-09 15:5X).

무엇이 문제였나
  lock 은 **작업**을 잡는데(`docs/claims/README.md`), 워커끼리 실제로 부딪히는 것은 **파일**이다.
  그 둘을 잇는 것이 규약 한 줄뿐이다 — «한 작업이 만지는 파일 범위는 PROGRESS 표의 «범위» 열에
  적힌 폴더다. 두 작업이 같은 파일을 만져야 하면 뒤 번호가 기다린다»(README 27행).
  **그런데 그 열이 실제로 여는 파일을 안 적으면 규약이 통째로 헛돈다.**

  실측(2026-09-09 15:5X · 살아 있는 lock 16개) — 여덟이 «범위 열에 없는 `.cs`» 를 고치고 있었고,
  그 중 `Game/LobbyPopups.cs` 하나에 **주인 작업 여섯**(T303ⓑ·T311·T318·T321·T343·T345)이 줄 서 있었다.
  그 파일을 쥔 것은 T258 인데 T258 의 범위 열에는 그 이름이 없다. 그래서 일감을 고르는 워커에게
  그 파일은 **«비어 있는» 것으로 보이고**, 열어 보고서야(또는 커밋 이력을 뒤져서야) 남의 자리인 줄 안다.
  같은 시각 세 워커가 각각 그것을 다시 알아냈다(C·N·I 가 저마다 «T258 의 살아 있는 lock 안이라 못 연다» 고 적었다).

무엇을 잡나 — **막지 않는다. 알리기만 한다**(결정 493·627 · T238 이 세운 기준: 조율 결함은 알리기만).
  ⓐ 살아 있는 lock(90분 안)마다, 그 작업의 커밋이 실제로 만진 `Assets/Scripts/**/*.cs` 를 모은다.
  ⓑ 그 파일 이름이 그 작업의 «범위» 칸에 없으면 «안 적힌 채로 쥔 파일» 이다.
  ⓒ 그 중 **다른 행이 제 범위로 적어 둔 파일**은 ⚠ 로 따로 세운다 — 그 자리는 짐작이 아니라
     **지금 누군가 기다리고 있는** 자리다(대기 행이면 몇 개인지도 센다).

이 자가 «보는 칸» (T198 규약 — 자를 놓을 때는 사각지대를 적어 둔다)
  · **커밋 제목이 작업 번호로 시작하는 것만** 센다(§6 규약이 그 꼴을 요구한다). 번호를 안 적은 커밋은 못 본다.
  · `Tests/` 아래와 `.json`·`.md` 는 안 본다 — 부딪혀도 값이 싼 자리고, 자잘한 경고로 이 자를 죽이면 안 된다.
  · 얕은 클론(`--depth 1`)이면 이력이 없어 ⓐ 가 **빈손**이다. 그때는 «못 봤다» 고 말한다(조용히 초록이 되면 안 된다).
  · 범위 칸을 **글자로** 견준다(`Game/GearUi` 는 `GearUi.cs` 를 덮는다) — «그리는 18곳» 같은
    **뭉뚱그린 범위는 아무 파일도 안 덮는다**. 그것이 이 자의 뜻이다(뭉뚱그린 범위는 남에게 안 보인다).

쓰기: python3 tools/check_claim_scope.py [--selftest]
  늘 rc=0 이다.
"""
import io
import os
import re
import subprocess
import sys
import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOC = os.path.join(ROOT, "docs", "PROGRESS.md")
CLAIMS = os.path.join(ROOT, "docs", "claims")
LIVE_SECONDS = 90 * 60          # README 11행 — 90분 지난 lock 은 죽은 것
WINDOW = "14.days"              # 이력을 보는 창(작업 하나가 이보다 오래 살면 그 자체가 다른 문제다)

CELL = re.compile(r"(?<!\\)\|")
ROWHEAD = re.compile(r"^\|\s*T\d")


def cells(line):
    parts = CELL.split(line.rstrip("\n"))
    if parts and parts[0].strip() == "":
        parts = parts[1:]
    if parts and parts[-1].strip() == "":
        parts = parts[:-1]
    return parts


def rows(path):
    """{작업ID: (범위 칸, 상태 머리)} — 같은 ID 가 여러 줄이면 **범위를 합친다**(접힌 줄에도 범위가 남아 있다)."""
    scope, head = {}, {}
    with io.open(path, encoding="utf-8") as f:
        for line in f:
            if not ROWHEAD.match(line):
                continue
            c = cells(line)
            if len(c) < 6:
                continue
            tid = c[0].strip().replace("✅", "").replace("🔄", "").replace("⬜", "").strip()
            if not re.match(r"^T\d+[A-Za-z0-9\-·ⓐ-ⓩ]*$", tid):
                continue
            scope[tid] = (scope.get(tid, "") + " · " + c[4]).strip(" ·")
            head.setdefault(tid, c[2].lstrip("*_ ")[:1])
    return scope, head


def live_locks(claims_dir=None, now=None):
    """[(작업ID, SID, 나이(분))] — 90분 안에 갱신된 lock 만."""
    d = claims_dir or CLAIMS
    now = now or datetime.datetime.utcnow()
    out = []
    try:
        names = sorted(f for f in os.listdir(d) if f.endswith(".lock"))
    except OSError:
        return out
    for f in names:
        try:
            parts = io.open(os.path.join(d, f), encoding="utf-8").read().split()
            when = datetime.datetime.strptime(parts[0], "%Y-%m-%dT%H:%M:%SZ")
            sid = parts[1] if len(parts) > 1 else "(SID 없음)"
        except (OSError, ValueError, IndexError):
            continue
        age = (now - when).total_seconds()
        if 0 <= age < LIVE_SECONDS:
            out.append((f[:-5], sid, int(age // 60)))
    return out


def touched(task_id):
    """그 작업의 커밋이 만진 `Assets/Scripts/**/*.cs` (테스트 제외). 이력이 없으면 None."""
    head = task_id.split("-", 1)[0]          # «T288-4» 의 커밋 제목은 «T288 …» 이다
    try:
        out = subprocess.run(
            ["git", "log", "--since=" + WINDOW, "--name-only", "--format=%x00%s"],
            cwd=ROOT, capture_output=True, text=True, timeout=60).stdout
    except (OSError, subprocess.SubprocessError):
        return None
    if not out.strip():
        return None                          # 얕은 클론이거나 창 안에 커밋이 없다
    pat = re.compile(r"^" + re.escape(head) + r"(?![0-9])")
    files = set()
    for blk in out.split("\x00")[1:]:
        lines = blk.strip().split("\n")
        if not lines or not pat.match(lines[0].lstrip("❗ ")):
            continue
        for p in lines[1:]:
            p = p.strip()
            if p.startswith("Assets/Scripts/") and p.endswith(".cs") and "/Tests/" not in p:
                files.add(p)
    return files


def scope_of(task_id, scope):
    """그 lock 의 범위 칸 — **쪼갠 lock 은 제 부모 행의 범위를 쓴다**.

    ⚠ 이 자를 처음 돌렸을 때 `T293-core`·`T300-c`·`T320-b`·`T325-a` 넷이 «범위가 통째로 비었다» 고
       나왔다 — 표에 있는 행은 `T293` 인데 `scope["T293-core"]` 를 찾고 있었다(`docs/claims/README.md`
       가 «작업ID + `-` + 조각» 을 허용하고, `check_task_rows.live_lock_ids` 도 그 꼴을 이미 안다).
       **쪼갠 lock 은 제 행을 따로 갖지 않는 것이 보통**이라, 그것을 모르면 이 자가 내는 말의 절반이 거짓이 된다.
    """
    head = task_id.split("-", 1)[0]
    return (scope.get(task_id, "") + " · " + scope.get(head, "")).strip(" ·")


def undeclared(files, scope_cell):
    """범위 칸이 «글자로» 안 덮는 파일들. `Game/GearUi` 는 `Game/GearUi.cs` 를 덮는다."""
    out = []
    for p in sorted(files):
        stem = os.path.basename(p)[:-3]
        if stem and stem not in scope_cell:
            out.append(p)
    return out


def waiters(path_stem, scope, head, holder):
    """그 파일을 제 범위로 적어 둔 **«⬜ 대기» 행들**.

    ⚠ 닫힌 행(✅)은 안 센다 — 이 표는 반년치 이력이라 `Overlay.cs` 한 파일에만 46행이 붙는다.
       그 46 은 **아무도 기다리지 않는 옛 이야기**고, 그것을 찍으면 진짜 한 줄이 파묻힌다
       (첫 판을 그렇게 만들었다가 내가 못 읽어서 지웠다 · 결정 935 ③).
       «지금 막힌 사람» 만이 이 자가 할 말이다.
    """
    out = []
    for tid, cell in scope.items():
        if tid == holder or tid.split("-", 1)[0] == holder.split("-", 1)[0]:
            continue
        if head.get(tid) == "⬜" and path_stem in cell:
            out.append(tid)
    return sorted(out)


def main():
    scope, head = rows(DOC)
    locks = live_locks()
    if not locks:
        print("✓ check_claim_scope: 살아 있는 lock 0개 — 볼 것이 없다")
        return 0

    blind = []          # (작업ID, SID, 나이, [안 적힌 파일], {안 적힌 파일: [그것을 기다리는 대기 행]})
    no_history = False
    for tid, sid, age in locks:
        files = touched(tid)
        if files is None:
            no_history = True
            continue
        miss = undeclared(files, scope_of(tid, scope))
        if not miss:
            continue
        who = {}
        for p in miss:
            w = waiters(os.path.basename(p)[:-3], scope, head, tid)
            if w:
                who[p] = w
        blind.append((tid, sid, age, miss, who))

    if no_history:
        print("· (참고) 이력이 없어 못 봤다 — 얕은 클론(`--depth 1`)이면 이 자는 아무것도 못 센다.")
        print("    CI 에서 이 줄이 보이면 checkout 단계에 `fetch-depth: 0` 을 준다(안 주면 이 자는 늘 조용하다).")

    if not blind:
        print("✓ check_claim_scope: 살아 있는 lock " + str(len(locks))
              + "개 · 범위 열이 실제로 여는 파일을 다 적는다")
        return 0

    # 막힌 사람이 많은 자리부터 — 표를 훑는 워커가 첫 줄에서 오늘의 병목을 본다.
    blind.sort(key=lambda b: (-sum(len(v) for v in b[4].values()), b[0]))
    hot = sum(len(v) for _, _, _, _, w in blind for v in w.values())
    print("· (참고 · 실패 아님) 살아 있는 lock 이 «범위 열에 없는 파일» 을 쥐고 있다 —")
    print("    일감을 고르는 워커에게 그 파일은 «비어 있는» 것으로 보인다(README 27행이 그 열로 부딪힘을 가른다):")
    for tid, sid, age, miss, who in blind:
        print("  " + tid + " (" + sid + " · " + str(age) + "분 전 갱신) — 안 적힌 채로 쥔 파일 " + str(len(miss)) + "개"
              + ("  ⚠ 그중 " + str(len(who)) + "개를 지금 기다리는 대기 행이 있다" if who else ""))
        for p in sorted(who, key=lambda q: (-len(who[q]), q)):
            print("    ⚠ " + p[len("Assets/Scripts/"):] + "  ← 대기 행 " + str(len(who[p])) + ": " + " ".join(who[p]))
        rest = [p for p in miss if p not in who]
        if rest:
            print("    · 그 밖(기다리는 행 없음): " + " ".join(os.path.basename(p) for p in rest))
    print("  고치는 법: 그 작업의 **«범위» 칸에 실제로 여는 파일을 적는다**(칸 하나 · 커밋 하나 · `[skip ci]`).")
    print("            그러면 다음 워커가 표만 보고 «이 파일은 지금 남의 것» 을 알 수 있다 —")
    print("            지금은 커밋 이력을 뒤지거나 파일을 열어 보고서야 알게 되고, 그것을 매 회차 다시 한다.")
    # T281 계약 — **어느 갈래로 나가든 마지막 줄이 판정이다**(ROUTINE §7 · 결정 678).
    #   이 자는 «막지 않는다» 가 규약이라 빨강 갈래가 없다 → 마지막 줄은 언제나 `✓` 로 시작하고,
    #   센 것을 그 한 줄에 담는다(`| tail -1` 로 읽는 워커가 오늘의 병목을 그 줄에서 본다).
    print("✓ check_claim_scope: 살아 있는 lock " + str(len(locks)) + "개 · 범위에 안 적힌 채 쥔 파일 "
          + str(sum(len(b[3]) for b in blind)) + "개"
          + (" · 대기 행 " + str(hot) + "개가 막혀 있다(가장 막힌 자리 = " + blind[0][0] + " 의 "
             + str(len(blind[0][4])) + "개 파일)" if hot else "")
          + " — 알리기만 한다(결정 493)")
    return 0


def selftest():
    """자기 검사 — 이 자가 «본다» 고 적은 것을 실제로 보는가(T198 규약)."""
    ok = True

    # ⓐ 범위 칸이 파일을 «글자로» 덮으면 조용하다 · 안 덮으면 잡는다.
    got = undeclared({"Assets/Scripts/Game/LobbyPopups.cs", "Assets/Scripts/Game/Overlay.cs"},
                     "`Game/LobbyPopups.cs` · Tests/EditMode")
    if got != ["Assets/Scripts/Game/Overlay.cs"]:
        print("✗ 자기검사 ⓐ: " + repr(got)); ok = False

    # ⓑ 확장자 없이 적은 범위(`Game/GearUi`)도 덮는 것으로 본다.
    if undeclared({"Assets/Scripts/Game/GearUi.cs"}, "`Game/GearUi`·`Palette`"):
        print("✗ 자기검사 ⓑ"); ok = False

    # ⓒ 뭉뚱그린 범위(«그리는 18곳»)는 아무것도 안 덮는다 — 그것이 이 자의 뜻이다.
    if not undeclared({"Assets/Scripts/Game/GearScreen.cs"}, "`Core/GearTier.cs`·그리는 18곳"):
        print("✗ 자기검사 ⓒ"); ok = False

    # ⓓ «⬜ 대기» 행만 센다 — 닫힌 행(T99)은 아무도 안 기다린다 · 제 자신과 제 쪼갠 lock 도 안 센다.
    sc = {"T258": "Core/Achievement.cs", "T343": "`Game/LobbyPopups.cs`",
          "T311": "(남음) `Game/LobbyPopups.cs`", "T99": "`Game/LobbyPopups.cs`",
          "T258-x": "`Game/LobbyPopups.cs`"}
    hd = {"T258": "🔄", "T343": "⬜", "T311": "⬜", "T99": "✅", "T258-x": "⬜"}
    w = waiters("LobbyPopups", sc, hd, "T258")
    if w != ["T311", "T343"]:
        print("✗ 자기검사 ⓓ: " + repr(w)); ok = False

    # ⓔ 쪼갠 lock(`T293-core`)은 제 행이 없다 — 부모 행(`T293`)의 범위를 쓴다.
    if scope_of("T293-core", {"T293": "`Game/BattleWorld`"}) != "`Game/BattleWorld`":
        print("✗ 자기검사 ⓔ"); ok = False

    # ⓕ 90분 규약 — 죽은 lock 은 «살아 있는» 목록에 안 든다(그 자리는 잡아도 되는 자리다).
    import tempfile
    with tempfile.TemporaryDirectory() as d:
        now = datetime.datetime(2026, 9, 9, 16, 0, 0)
        io.open(os.path.join(d, "T1.lock"), "w", encoding="utf-8").write("2026-09-09T15:30:00Z sess-a\n")
        io.open(os.path.join(d, "T2.lock"), "w", encoding="utf-8").write("2026-09-09T14:00:00Z sess-b\n")
        got = [t for t, _, _ in live_locks(d, now)]
        if got != ["T1"]:
            print("✗ 자기검사 ⓕ: " + repr(got)); ok = False

    print(("✓" if ok else "✗") + " check_claim_scope 자기검사 6칸")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(selftest() if "--selftest" in sys.argv else main())
