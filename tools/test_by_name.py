#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""«내 자가 이 하니스에 이름으로 섰는가» — `--filter` 가 조용히 무시되는 자리를 대신한다 (T456).

왜 있나 — **이 하니스에서 `dotnet test --filter` 는 아무 일도 안 한다. 그런데 초록이다.**
  2026-09-11 실측(워커 I):
    · `dotnet test … --filter "FullyQualifiedName~ZZZNoSuch"` → **`Passed! … Total: 569`** · rc 0 · 경고 한 줄 없음
    · `dotnet vstest … --TestCaseFilter:"…"` 도 같다 — 거르는 쪽이 아니라 **어댑터**가 무시한다
  곧 **없는 이름을 줘도 569개가 다 돌고 «초록» 이 나온다.** 「내 자만 이름으로 돌려 확인했다」 는
  그래서 **아무것도 확인하지 않은 것**이다 — T278 이 CI 에서 이름 붙인 그 함정(«✗ 0 은 «안 실렸다» 와
  «다 통과했다» 를 못 가른다»)이 로컬 하니스에도 그대로 있고, 여기서는 **더 나쁘다**:
  CI 에는 `[CI명부]` 가 있는데 로컬에는 그것마저 없다고 여겨져 왔다.

  ⚠ 그리고 이 길은 이미 문서에 **권해져 있다** — `docs/PROGRESS.md` T442 ⑥ 이 열린 물음의 첫 걸음으로
  «`dotnet` 이 있는 워커가 `PetTests` 만 따로 한 번 돌리면(`--filter`) 끝난다» 를 적어 두었다.
  그대로 하면 **569개가 다 돌고 초록**이라 «주인 통만의 것» 이라는 **틀린 답**이 나온다.
  함정이 무서운 까닭은 조심성 없는 사람이 빠져서가 아니라 **조심한 사람이 빠져서**다.

무엇이 문제인가(뿌리) — `NUnit 3.6.1` ↔ `NUnit3TestAdapter 4.5.0` 의 짝이다.
  같은 소스·같은 어댑터에 **NUnit 만 3.13.3** 으로 올린 스크래치 프로젝트에서 재 보면
  569/569 초록은 그대로인 채 없는 이름은 **«No test matches the given testcase filter»** 로 운다.
  ⛔ **그래도 올리지 마라.** 3.6.1 은 **유니티 포크와 같은 API 면**을 지키려고 박아 둔 값이다
  (ROUTINE §3 — «`Does.Contain(object)` 같은 신형 API 금지»). 올리면 `--filter` 의 조용한 거짓말이
  **«유니티가 거부할 API 가 여기서는 통과한다»** 는 더 조용한 거짓말로 바뀐다.
  ⇒ 뿌리는 **못 뽑는 것이 아니라 뽑으면 안 되는 것**이다. 그래서 문을 따로 낸다.

무엇을 재나 — `dotnet vstest --ListFullyQualifiedTests` 다. **이것은 무시되지 않는다.**
  569줄의 `Namespace.Class.Method` 가 그대로 떨어진다 — 곧 **로컬 `[CI명부]`** 다.
  `dotnet test --list-tests` 도 도는데 그쪽은 **메서드 이름만** 찍어 `PetTests` 같은 **뭉치 이름으로는 못 찾는다**.

쓰는 법
  python3 tools/test_by_name.py ShortNum            # 그 이름의 자가 명부에 섰는가(빠르다 · 안 돌린다)
  python3 tools/test_by_name.py PetTests ShortNum   # 조각 여럿 — 하나라도 0개면 1 로 끝난다
  python3 tools/test_by_name.py ShortNum --run      # 명부 확인 + 전체 자 한 판(569개) · 둘 다 초록이어야 0
  python3 tools/test_by_name.py --self-test         # 이 자가 실제로 가르는지
  옵션: -c Release (기본 Debug) · --list 로 명부 전부 찍기

⚑ **왜 «그 자만» 돌리지 않고 전체를 돌리나** — 못 한다. 고르는 문이 막힌 것이 이 자의 존재 이유다.
  대신 T278 의 그 꼴을 그대로 쓴다: **① 명부에 이름이 섰는가 ② 실패 0 인가.** 둘이 같이 서면 확인이 된다.
"""
import argparse
import os
import re
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSPROJ = os.path.join(ROOT, "tools", "dotnet", "Tests", "KkomaKnight.Tests.csproj")


def _run(cmd, cwd=ROOT):
    p = subprocess.run(cmd, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                       universal_newlines=True)
    return p.returncode, p.stdout


def target_path(config):
    """빌드된 테스트 어셈블리 경로 — 판(Debug/Release)을 MSBuild 에게 직접 묻는다(경로를 손으로 짓지 않는다)."""
    # ⚠ `dotnet msbuild` 는 `-c` 를 모른다(«Switch: -c» 로 죽는다) — 판은 `-p:Configuration=` 으로만 준다.
    rc, out = _run(["dotnet", "msbuild", CSPROJ, "-p:Configuration=" + config, "-getProperty:TargetPath"])
    if rc != 0:
        return None, out
    path = out.strip().splitlines()[-1].strip() if out.strip() else ""
    return (path or None), out


def list_names(config="Debug"):
    """명부를 뽑는다 — (이름들, 군말). 실패하면 이름들이 None 이다(«0개» 와 반드시 갈라 둔다)."""
    rc, out = _run(["dotnet", "build", CSPROJ, "-c", config, "--nologo", "-v", "q"])
    if rc != 0:
        return None, "빌드가 실패했다:\n" + out[-2000:]
    path, out = target_path(config)
    if not path or not os.path.exists(path):
        return None, "테스트 어셈블리를 못 찾았다:\n" + out[-2000:]
    with tempfile.NamedTemporaryFile(suffix=".txt", delete=False) as f:
        listing = f.name
    try:
        rc, out = _run(["dotnet", "vstest", path,
                        "--ListFullyQualifiedTests", "--ListTestsTargetPath:" + listing])
        if rc != 0:
            return None, "명부 뽑기가 실패했다:\n" + out[-2000:]
        with open(listing, encoding="utf-8") as fh:
            names = [ln.strip() for ln in fh if ln.strip()]
    finally:
        try:
            os.unlink(listing)
        except OSError:
            pass
    return names, ""


def run_all(config="Debug"):
    """전체 자 한 판 — (rc, 요약 한 줄)."""
    rc, out = _run(["dotnet", "test", CSPROJ, "-c", config, "--nologo", "-v", "q"])
    line = ""
    for ln in out.splitlines():
        if ln.strip().startswith(("Passed!", "Failed!")):
            line = ln.strip()
    return rc, (line or out.strip().splitlines()[-1] if out.strip() else "")


def matches(names, frag):
    low = frag.lower()
    return [n for n in names if low in n.lower()]


def check(frags, config="Debug", do_run=False, show_list=False,
          lister=list_names, runner=run_all, out=sys.stdout):
    """명부를 뽑아 조각마다 맞대 본다. 0 = 조각이 전부 섰다(그리고 --run 이면 자도 초록이다)."""
    names, why = lister(config)
    if names is None:
        out.write("✗ test_by_name: 명부를 못 뽑았다 — %s\n" % (why.strip().splitlines()[0] if why.strip() else "까닭 모름"))
        out.write("   («명부 0개» 가 아니라 «명부를 못 봤다» 다 — 이 둘을 같은 것으로 읽으면 그 회차는 아무것도 안 본 것이다)\n")
        out.write("✗ test_by_name: 확인 0건 — 위 군말을 먼저 고쳐라\n")
        return 1
    out.write("· 하니스 명부 = 자 %d개 (`dotnet vstest --ListFullyQualifiedTests` · 이 길은 안 무시된다)\n" % len(names))
    if show_list:
        for n in names:
            out.write("    %s\n" % n)
    missing = []
    for frag in frags:
        hit = matches(names, frag)
        if not hit:
            missing.append(frag)
            out.write("  ⛔ «%s» → **0개**\n" % frag)
            continue
        out.write("  · «%s» → %d개\n" % (frag, len(hit)))
        for n in hit[:6]:
            out.write("      %s\n" % n)
        if len(hit) > 6:
            out.write("      … +%d개\n" % (len(hit) - 6))
    if missing:
        out.write("✗ test_by_name: 이름이 안 선 조각 %d개(%s) — **초록을 봐도 그 자가 돈 증거가 아니다.**"
                  " `--filter` 로 «돌려 봤다» 고 적지 마라(이 하니스에서 그 문은 조용히 무시된다 · T456)\n"
                  % (len(missing), " ".join(missing)))
        return 1
    if not do_run:
        out.write("✓ test_by_name: 조각 %d개가 전부 이름으로 섰다 (자 %d개 중 · 돌리진 않았다 — `--run` 을 붙이면 전체 한 판)\n"
                  % (len(frags), len(names)))
        return 0
    rc, summary = runner(config)
    out.write("· 전체 한 판: %s\n" % summary)
    if rc != 0:
        out.write("✗ test_by_name: 이름은 섰는데 **자가 빨갛다** — 위 요약을 보라\n")
        return 1
    out.write("✓ test_by_name: 조각 %d개가 이름으로 섰고 자 %d개가 전부 초록이다 (T278 의 두 조건이 같이 섰다)\n"
              % (len(frags), len(names)))
    return 0


# ──────────────────────────────────────────────────────────────────────────────
def self_test():
    import io
    FAKE = ["KkomaKnight.Tests.ShortNumTests.FmtIsByteIdentical",
            "KkomaKnight.Tests.ShortNumCellTests.CellShrinksFrom1e3",
            "KkomaKnight.Tests.PetTests.BrokenTableCries"]
    ok = True

    def say(tag, good, detail=""):
        nonlocal ok
        print(("  ✓ " if good else "  ⛔ ") + tag + (" — " + detail if detail else ""))
        if not good:
            ok = False

    def lister_ok(config="Debug"):
        return list(FAKE), ""

    def lister_dead(config="Debug"):
        return None, "빌드가 실패했다: CS0246\n둘째 줄"

    def lister_empty(config="Debug"):
        return [], ""

    def runner_green(config="Debug"):
        return 0, "Passed!  - Failed: 0, Passed: 3"

    def runner_red(config="Debug"):
        return 1, "Failed!  - Failed: 1, Passed: 2"

    def go(frags, **kw):
        buf = io.StringIO()
        rc = check(frags, out=buf, **kw)
        return rc, buf.getvalue()

    # ⓐ 없는 이름은 막는다 — 이 자의 존재 이유 그것 하나다.
    rc, txt = go(["ZZZNoSuch"], lister=lister_ok)
    say("ⓐ 없는 이름 → 1", rc == 1 and "0개" in txt and txt.strip().splitlines()[-1].startswith("✗"),
        "rc=%d" % rc)

    # ⓑ 있는 이름은 통과한다(그리고 몇 개인지 센다).
    rc, txt = go(["ShortNum"], lister=lister_ok)
    say("ⓑ 있는 이름 → 0", rc == 0 and "2개" in txt and txt.strip().splitlines()[-1].startswith("✓"),
        "rc=%d" % rc)

    # ⓒ 뭉치(클래스) 이름으로도 찾는다 — `--list-tests` 로는 못 하는 그 자리다.
    rc, txt = go(["PetTests"], lister=lister_ok)
    say("ⓒ 뭉치 이름 → 0", rc == 0 and "1개" in txt, "rc=%d" % rc)

    # ⓓ 조각 둘 중 하나만 없으면 막는다 — «하나는 섰으니 됐다» 로 읽히면 안 된다.
    rc, txt = go(["ShortNum", "ZZZNoSuch"], lister=lister_ok)
    say("ⓓ 둘 중 하나가 없다 → 1", rc == 1 and "ZZZNoSuch" in txt.strip().splitlines()[-1], "rc=%d" % rc)

    # ⓔ 대소문자는 안 가린다(사람이 적는 조각은 흔히 소문자다).
    rc, _ = go(["shortnum"], lister=lister_ok)
    say("ⓔ 소문자 조각 → 0", rc == 0, "rc=%d" % rc)

    # ⓕ 이름이 서도 자가 빨가면 막는다 — 두 조건이 **같이** 서야 확인이다.
    rc, txt = go(["ShortNum"], lister=lister_ok, do_run=True, runner=runner_red)
    say("ⓕ 이름 섰으나 빨강 → 1", rc == 1 and "빨갛다" in txt, "rc=%d" % rc)
    rc, txt = go(["ShortNum"], lister=lister_ok, do_run=True, runner=runner_green)
    say("ⓕ' 이름 섰고 초록 → 0", rc == 0 and "초록" in txt.strip().splitlines()[-1], "rc=%d" % rc)

    # ⓖ **명부를 못 뽑은 것**과 «명부가 비었다» 를 가른다 — 둘 다 막지만 말이 달라야 한다.
    rc, txt = go(["ShortNum"], lister=lister_dead)
    say("ⓖ 명부를 못 뽑았다 → 1", rc == 1 and "못 뽑았다" in txt and "CS0246" in txt, "rc=%d" % rc)
    rc, txt = go(["ShortNum"], lister=lister_empty)
    say("ⓖ' 명부가 0개다 → 1", rc == 1 and "0개" in txt and "못 뽑았다" not in txt, "rc=%d" % rc)

    # ⓗ 맞대기 그 자체.
    say("ⓗ matches 는 부분 일치다", matches(FAKE, "Tests.Pet") == [FAKE[2]] and matches(FAKE, "q") == [])

    print(("✓ " if ok else "✗ ") + "test_by_name --self-test: 갈래 열 %s" % ("전부 통과" if ok else "에서 위 ⛔ 를 보라"))
    return 0 if ok else 1


def main(argv=None):
    ap = argparse.ArgumentParser(add_help=True, description="이름으로 자가 섰는지 확인한다(이 하니스의 `--filter` 는 조용히 무시된다 · T456)")
    ap.add_argument("frags", nargs="*", help="자 이름 조각(뭉치 이름도 된다) — 여럿이면 전부 서야 0")
    ap.add_argument("-c", "--config", default="Debug", help="빌드 판 (기본 Debug)")
    ap.add_argument("--run", action="store_true", help="이름 확인 뒤 전체 자 한 판도 돌린다")
    ap.add_argument("--list", action="store_true", help="명부를 전부 찍는다")
    ap.add_argument("--self-test", action="store_true", dest="selftest")
    a = ap.parse_args(argv)
    if a.selftest:
        return self_test()
    if not a.frags and not a.list:
        ap.print_help()
        return 2
    return check(a.frags, config=a.config, do_run=a.run, show_list=a.list)


if __name__ == "__main__":
    sys.exit(main())
