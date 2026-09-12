#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""이 푸시가 «유니티가 읽는 나무» 를 건드렸는가 (T498 · 워커 F · 2026-09-12)

**한 자에 담은 까닭.** T433 이 세운 잣대(«문서만 바뀐 푸시는 `unity-test` 를 건너뛴다»)는
`ci.yml` 의 `gate` 잡과 `tools/check_gate_age.py` **두 곳**에 같은 말로 적혀 있어야 했고,
ROUTINE §3 이 «두 곳이 갈리면 자는 «빚 3개» 라 우는데 CI 는 애초에 돌 생각이 없다» 를
경고로 달아 두었다. 갈릴 수 있는 것은 언젠가 갈린다 — 그래서 **잣대를 여기 한 자에 담고
둘이 이것을 부른다**(ci.yml gate 단계 · check_gate_age).

**T498 이 넓힌 것.** 옛 잣대는 «문서냐 아니냐» 만 갈랐다. 그런데 `tools/check_*.py` 는
문서가 아니면서도 **유니티가 읽는 나무(`Assets/**`·`Packages/**`·`ProjectSettings/**`) 밖**이라
유니티 잡이 답할 것이 **없다**. 실측(2026-09-12 · 창 36시간): 커밋 313개 중 옛 잣대로 «코드» 인
것 87개, 그중 코드가 전부 `tools/**`(비 C#)인 것이 **25개(29%)** — 유니티 잡 넷 중 하나가
답할 것이 없는 채로 16~20분을 돌았다. 그 파이프는 «도는 것 1 + 기다리는 것 1» 뿐이라
**헛도는 런은 느린 것이 아니라 남의 코드 런을 죽이는 것**이다(결정 1347 ② · T497 기전 ①).

**목록을 손으로 안 적는다.** 「`tools/` 는 유니티와 무관」 은 **거짓**이다 — 유니티 잡 안에서
`tools/screens_diff.py`·`tools/ui_score.py`·`tools/ci_test_failures.py` 가 돌고 `build-webgl` 은
`tools/webgl_smoke.sh` 를 부른다. 그래서 «무관» 을 **워크플로에서 뽑는다**:
  ① `if:` 가 `has_code` 를 보는 잡 = 유니티 쪽 잡. 그것을 `needs:` 하는 잡도 유니티 쪽이다.
     (`ci.yml` 밖의 워크플로는 통째로 유니티 쪽으로 센다 — `deploy-last-green` 은 WebGL 을 굽는다.)
  ② 그 잡들의 몸통이 부르는 `tools/...` 를 모으고, **그 파일이 다시 부르는 것까지 닫는다**
     (`webgl_smoke.sh` → `webgl_smoke.js` 처럼 한 겹 더 들어가는 자리가 실제로 있다).
  ③ 그 폐포 **밖**의 `tools/` 만 «유니티가 안 읽는 것» 이다.
곧 누가 유니티 잡에 새 `tools/` 한 줄을 넣으면 그 파일은 **그 순간부터** 자동으로 코드가 된다.

**fail-safe 는 언제나 «돈다» 쪽이다**(T433 이 세운 방향 · §1 「테스트 0개는 빨간 테스트보다 나쁘다」).
워크플로를 못 읽으면 · 목록이 비면 · 조금이라도 의심스러우면 **코드로 센다**.

쓰는 법:
    python3 tools/ci_unity_paths.py --files a.py b.md   # 「돈다/건너뛴다」 + 까닭 (rc 0)
    python3 tools/ci_unity_paths.py --list              # 유니티 쪽 잡 · 폐포에 든 tools 목록
    python3 tools/ci_unity_paths.py --self-test         # 갈래별 자기 검사 (CI dotnet 잡)
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WORKFLOWS = os.path.join(".github", "workflows")

# ⚑ 잣대를 담은 자 자신은 **늘 코드**다 — 잣대를 바꾸는 커밋을 그 잣대로 재면
#   틀린 잣대가 스스로를 건너뛰게 만든다. 이 두 자는 폐포 밖이어도 코드로 센다.
ALWAYS_CODE = ("tools/ci_unity_paths.py",)

JOB_RE = re.compile(r"^  ([A-Za-z0-9_\-]+):\s*$")
TOOLS_RE = re.compile(r"tools/[A-Za-z0-9_\-./]+")
NEEDS_RE = re.compile(r"^\s*needs:\s*(.+)$")


def _read(path):
    try:
        with io.open(path, encoding="utf-8", errors="replace") as fh:
            return fh.read()
    except (IOError, OSError):
        return None


def workflow_files(root=ROOT):
    d = os.path.join(root, WORKFLOWS)
    try:
        names = sorted(os.listdir(d))
    except (IOError, OSError):
        return []
    return [os.path.join(d, n) for n in names if n.endswith((".yml", ".yaml"))]


def split_jobs(text):
    """워크플로 한 편 → {잡 이름: 몸통}. `jobs:` 아래 2칸 들여쓴 열쇠가 잡이다."""
    lines = text.split("\n")
    try:
        start = next(i for i, ln in enumerate(lines) if ln.rstrip() == "jobs:")
    except StopIteration:
        return {}
    jobs, name, body = {}, None, []
    for ln in lines[start + 1:]:
        m = JOB_RE.match(ln)
        if m:
            if name is not None:
                jobs[name] = "\n".join(body)
            name, body = m.group(1), []
        elif name is not None:
            # 들여쓰기가 풀리면(뿌리 열쇠) 잡 구역이 끝난 것이다.
            if ln.strip() and not ln.startswith("  "):
                break
            body.append(ln)
    if name is not None:
        jobs[name] = "\n".join(body)
    return jobs


def needs_of(body):
    out = set()
    for ln in body.split("\n"):
        m = NEEDS_RE.match(ln)
        if m:
            out |= {t.strip(" []'\"") for t in m.group(1).split(",") if t.strip(" []'\"")}
    return out


def unity_jobs(root=ROOT):
    """유니티 쪽 잡 → {(파일이름, 잡이름): 몸통}. 못 읽으면 빈 사전(= 부르는 쪽이 fail-safe)."""
    out = {}
    for path in workflow_files(root):
        text = _read(path)
        if text is None:
            continue
        base = os.path.basename(path)
        jobs = split_jobs(text)
        if base != "ci.yml":
            # ci.yml 밖은 통째로 유니티 쪽으로 센다(배포 워크플로가 WebGL 을 굽는다).
            for name, body in jobs.items():
                out[(base, name)] = body
            continue
        seeds = {n for n, b in jobs.items() if "has_code" in b and re.search(r"^\s*if:", b, re.M)}
        grown = True
        while grown:
            grown = False
            for name, body in jobs.items():
                if name not in seeds and needs_of(body) & seeds:
                    seeds.add(name)
                    grown = True
        for name in seeds:
            out[(base, name)] = jobs[name]
    return out


_CACHE = {}


def unity_tools(root=ROOT):
    """유니티 쪽 잡이 (곧바로 또는 건너서) 부르는 `tools/...` 의 폐포.

    ⚑ 값을 기억해 둔다 — `check_gate_age` 는 커밋마다 파일마다 이것을 묻는다(창 하나에 수백 번).
    기억이 없으면 그때마다 워크플로와 `tools/` 를 통째로 훑어 회차 머리가 몇 초씩 선다.
    """
    if root in _CACHE:
        return _CACHE[root]
    seeds = set()
    for body in unity_jobs(root).values():
        seeds |= set(TOOLS_RE.findall(body))
    # 폐포 — 그 파일이 다시 부르는 tools 경로와, tools/ 안 파일의 **이름**까지 따라간다
    # (`"$(dirname "$0")/webgl_smoke.js"` 처럼 경로 없이 형제를 부르는 자리가 있다).
    names = {}
    for dirpath, _dirs, files in os.walk(os.path.join(root, "tools")):
        for f in files:
            rel = os.path.relpath(os.path.join(dirpath, f), root).replace(os.sep, "/")
            names.setdefault(f, set()).add(rel)
    closure, queue = set(), list(seeds)
    while queue:
        cur = queue.pop()
        if cur in closure:
            continue
        closure.add(cur)
        text = _read(os.path.join(root, cur))
        if text is None:
            continue
        nxt = set(TOOLS_RE.findall(text))
        for f, rels in names.items():
            if len(f) > 4 and f in text:
                nxt |= rels
        queue.extend(n for n in nxt if n not in closure)
    _CACHE[root] = closure
    return closure


def is_doc(path):
    """T433 이 세운 «문서» — `docs/**` 와 뿌리의 `*.md`."""
    return path.startswith("docs/") or ("/" not in path and path.endswith(".md"))


def blind(path, tools=None, root=ROOT):
    """유니티 잡이 **답할 것이 없는** 파일인가."""
    if path in ALWAYS_CODE:
        return False
    if is_doc(path):
        return True
    if not path.startswith("tools/"):
        return False
    if path.startswith("tools/dotnet/"):
        # C# 이다 — 넉넉하게 코드로 센다(안 재 본 자리는 «돈다» 쪽으로 틀린다).
        return False
    if tools is None:
        tools = unity_tools(root)
    if not tools:
        return False        # 워크플로를 못 읽었다 → 전부 코드(fail-safe)
    return path not in tools


def verdict(files, root=ROOT):
    """(코드인가, 까닭 한 줄)."""
    files = [f for f in files if f.strip()]
    if not files:
        return True, "바뀐 파일이 0개다"
    tools = unity_tools(root)
    if not tools:
        return True, "워크플로에서 유니티 쪽 잡을 못 읽었다(fail-safe)"
    code = [f for f in files if not blind(f, tools, root)]
    if not code:
        docs = [f for f in files if is_doc(f)]
        return False, ("바뀐 파일 %d개가 전부 유니티 밖이다(문서 %d · 유니티가 안 부르는 tools %d)"
                       % (len(files), len(docs), len(files) - len(docs)))
    return True, "유니티가 읽는 파일이 있다(%s · 모두 %d개)" % (code[0], len(files))


# ────────────────────────────── 자기 검사 ──────────────────────────────

CI_FAKE = """name: CI
on: [push]
jobs:
  dotnet:
    steps:
      - run: python3 tools/check_alpha.py
  gate:
    outputs:
      has_code: x
  unity-test:
    if: needs.gate.outputs.has_code != 'false'
    steps:
      - run: python3 tools/screens_fake.py
  build-webgl:
    needs: [unity-test, gate]
    steps:
      - run: tools/smoke_fake.sh
"""


def self_test():
    import shutil
    import tempfile
    bad = 0

    def ok(name, got, want):
        nonlocal bad
        if got != want:
            bad += 1
            print("  ✗ %s — 얻은 것 %r · 바란 것 %r" % (name, got, want))
        else:
            print("  · %s ✔" % name)

    # ① 잡 쪼개기
    jobs = split_jobs(CI_FAKE)
    ok("잡 넷을 쪼갠다", sorted(jobs), ["build-webgl", "dotnet", "gate", "unity-test"])
    ok("needs 를 읽는다", needs_of(jobs["build-webgl"]), {"unity-test", "gate"})
    ok("jobs 가 없으면 빈 사전", split_jobs("name: x\non: [push]\n"), {})

    # ② 가짜 레포에서 폐포
    tmp = tempfile.mkdtemp(prefix="t498-")
    try:
        os.makedirs(os.path.join(tmp, WORKFLOWS))
        os.makedirs(os.path.join(tmp, "tools"))
        with io.open(os.path.join(tmp, WORKFLOWS, "ci.yml"), "w", encoding="utf-8") as fh:
            fh.write(CI_FAKE)
        for n, body in (("check_alpha.py", "print(1)\n"),
                        ("screens_fake.py", "print(2)\n"),
                        ("smoke_fake.sh", "#!/bin/sh\nexec node \"$(dirname \"$0\")/smoke_fake.js\"\n"),
                        ("smoke_fake.js", "console.log(3)\n")):
            with io.open(os.path.join(tmp, "tools", n), "w", encoding="utf-8") as fh:
                fh.write(body)

        got = unity_tools(tmp)
        ok("유니티 잡이 부르는 자가 든다", "tools/screens_fake.py" in got, True)
        ok("build-webgl 것도 든다", "tools/smoke_fake.sh" in got, True)
        ok("⚑ 형제까지 닫는다(경로 없이 부른 .js)", "tools/smoke_fake.js" in got, True)
        ok("dotnet 잡만 부르는 자는 안 든다", "tools/check_alpha.py" in got, False)

        ok("dotnet 전용 자 = 안 읽는 것", blind("tools/check_alpha.py", got, tmp), True)
        ok("유니티가 부르는 자 = 코드", blind("tools/screens_fake.py", got, tmp), False)
        ok("문서 = 안 읽는 것", blind("docs/PROGRESS.md", got, tmp), True)
        ok("뿌리 md = 안 읽는 것", blind("README.md", got, tmp), True)
        ok("Assets = 코드", blind("Assets/Scripts/A.cs", got, tmp), False)
        ok("tools/dotnet = 넉넉하게 코드", blind("tools/dotnet/Sim/P.cs", got, tmp), False)
        ok("잣대 자신은 늘 코드", blind("tools/ci_unity_paths.py", got, tmp), False)
        ok("워크플로 자체 = 코드", blind(".github/workflows/ci.yml", got, tmp), False)

        ok("섞이면 돈다", verdict(["docs/A.md", "Assets/B.cs"], tmp)[0], True)
        ok("문서+검사자만이면 건너뛴다", verdict(["docs/A.md", "tools/check_alpha.py"], tmp)[0], False)
        ok("빈 목록이면 돈다", verdict([], tmp)[0], True)

        # ③ ⚑ 일부러 부러뜨린다(T249) — 워크플로를 치우면 **전부 코드**로 떨어져야 한다
        shutil.rmtree(os.path.join(tmp, WORKFLOWS))
        _CACHE.pop(tmp, None)   # 기억을 비운다 — 안 비우면 이 갈래가 옛 답을 보고 조용히 통과한다
        ok("워크플로가 없으면 폐포가 빈다", unity_tools(tmp), set())
        ok("그때는 검사자도 코드다(fail-safe)", blind("tools/check_alpha.py", None, tmp), False)
        ok("그때는 판정도 «돈다»", verdict(["tools/check_alpha.py"], tmp)[0], True)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    # ④ 진짜 레포 — 오늘 실제로 도는 자리
    real = unity_tools(ROOT)
    ok("진짜 ci.yml 에서 폐포가 선다", len(real) > 0, True)
    ok("screens_diff 는 유니티 쪽이다", "tools/screens_diff.py" in real, True)
    ok("webgl_smoke.sh 는 유니티 쪽이다", "tools/webgl_smoke.sh" in real, True)
    ok("check_gate_age 는 dotnet 전용이다", "tools/check_gate_age.py" in real, False)

    if bad:
        print("✗ ci_unity_paths --self-test: %d갈래 실패" % bad)
        return 1
    print("✓ ci_unity_paths --self-test: 모든 갈래 통과")
    return 0


def main(argv):
    if "--self-test" in argv:
        return self_test()
    if "--list" in argv:
        jobs = unity_jobs()
        print("유니티 쪽 잡 %d개:" % len(jobs))
        for f, n in sorted(jobs):
            print("  · %s : %s" % (f, n))
        tools = unity_tools()
        print("그 잡들이 (건너서라도) 부르는 tools %d개 — 이것만 «코드» 다:" % len(tools))
        for t in sorted(tools):
            print("  · %s" % t)
        return 0
    if "--files" in argv:
        files = argv[argv.index("--files") + 1:]
    else:
        files = [ln.strip() for ln in sys.stdin.read().split("\n") if ln.strip()]
    code, why = verdict(files)
    print("%s — %s" % ("돈다" if code else "건너뛴다", why))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
