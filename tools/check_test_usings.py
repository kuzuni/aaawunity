#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""테스트 어셈블리가 «참조하지 않는» 네임스페이스를 `using` 하고 있지 않은가 (결정 465 · main 컴파일 파손 재발 방지).

무엇이 문제였나
  `Assets/Tests/PlayMode/*.asmdef` 은 `overrideReferences: true` 이고 precompiled 참조가 **nunit 하나**다.
  그래서 `Assets/Plugins/…/DOTween.dll` 은 그 어셈블리에 **안 들어온다**. 그런데 워커가 자에
  `using DG.Tweening;` 을 넣어 CI 유니티 잡이 **테스트를 한 개도 못 돌리고** 죽었다
  (`error CS0246: 'DG' could not be found` · screens PNG 0장 · gh-pages 도 막힘 · CI #346~#348).

`tools/check_asmdef.py`(T189)와의 관계 — 그 자는 **`Assets/Scripts`** 만 본다(일반 표로 테스트 폴더를 보면
  오탐 80건이라 일부러 뺐다고 그 자의 머리글에 적혀 있다). 이 자는 그 **빈자리**(테스트 폴더)를 «플러그인 전용
  네임스페이스만» 으로 좁혀서 본다 — 그래서 지금 트리에서 오탐 0이다. 그 자의 «DG.Tweening 은 references 에
  안 적고도 컴파일된다» 는 관찰은 게임 어셈블리 쪽 이야기다: 테스트 어셈블리에서는 **CI #346~#348 이 실제로 죽었다**
  (`overrideReferences: true` + precompiled = nunit 하나). 그 증거가 이 자의 실패 규칙 근거다.

왜 로컬 게이트가 못 잡았나
  결정 143 의 임시 csproj 는 UiKit 을 컴파일해야 하므로 `DOTween.dll` 을 **참조한다** —
  즉 그 사전 점검은 «유니티 asmdef 경계» 를 재현하지 못한다. 이 자가 그 경계만 따로 본다.

무엇을 보나
  아래 표(네임스페이스 → 그것을 가진 어셈블리·dll)에 있는 네임스페이스를 `using` 했는데
  그 어셈블리가 asmdef 의 `references`(또는 `precompiledReferences`)에 없으면 **실패**.
  유니티 «모듈» 어셈블리(UnityEngine.UI · UnityEngine.TestRunner …)는 자동으로 들어오므로 표에 없다.

**⚠ 이 자가 못 보는 자리** (T506 이 자기 검사를 세우며 글자로 박았다):
  · **블록 주석(`/* … */`) 안에서 줄머리에 선 `using` 은 셈에 든다** — `//` 줄은 두 겹으로 막혀 있다
    (`USING` 정규식의 줄머리 표식 + `re.match` 의 자리 물림) 지만 블록 주석은 줄 단위로 못 가른다.
    실측(2026-09-12): 지금 트리의 그런 자리 **0건**이라 살아 있는 오탐은 아니고, 틀리는 방향도
    «시끄러운 쪽» 이다(조용한 거짓이 아니다).
  · **표(`NEEDS`)에 없는 네임스페이스는 안 본다** — 새 플러그인을 넣는 날 그 이름을 여기 더해야 한다.

쓰기: python3 tools/check_test_usings.py [--list]
     python3 tools/check_test_usings.py --self-test   # 이 자가 갈래마다 제대로 우는가
"""
import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TESTS = os.path.join(ROOT, "Assets", "Tests")

# 네임스페이스 → 그것을 담은 어셈블리 이름(asmdef references) · 또는 dll 이름(precompiledReferences)
# 실패로 세는 것 — 그 네임스페이스가 **오로지** 그 어셈블리에만 있는 자리(플러그인·패키지 전용)
NEEDS = {
    "DG.Tweening": ("DOTween.Modules", "DOTween.dll"),
    "TMPro": ("Unity.TextMeshPro", "Unity.TextMeshPro.dll"),
}
# 알림만 하는 것 — 네임스페이스는 유니티 코어 모듈에도 있어서(예 UnityEngine.Rendering.CompareFunction)
# «using 이 있다» 만으로는 못 가른다. 안에서 쓰는 **타입**이 URP 것이면 CI 에서 죽는다 — 사람이 본다.
SOFT = {
    "UnityEngine.Rendering.Universal": ("Unity.RenderPipelines.Universal.Runtime", None),
    "UnityEngine.Rendering": ("Unity.RenderPipelines.Core.Runtime", None),
}
USING = re.compile(r"^\s*using\s+(?:static\s+)?([A-Za-z_][A-Za-z0-9_.]*)\s*;")


def asmdefs():
    out = []
    for base, _dirs, files in os.walk(TESTS):
        for f in files:
            if f.endswith(".asmdef"):
                out.append(os.path.join(base, f))
    return sorted(out)


def has(need, data):
    asm, dll = need
    refs = [str(r) for r in data.get("references", [])]
    pre = [str(r) for r in data.get("precompiledReferences", [])]
    if asm and asm in refs:
        # overrideReferences 여도 asmdef 참조는 이름으로 들어온다
        return True
    if dll and dll in pre:
        return True
    return False


def main():
    if "--self-test" in sys.argv[1:] or "--selftest" in sys.argv[1:]:
        return self_test()
    return run()


def run():
    """실제 검사 한 판 — `main()` 과 나눠 둔 까닭은 자기 검사가 **이것만** 부르기 위해서다
    (`main()` 을 부르면 `sys.argv` 를 다시 읽어 스스로를 끝없이 부른다 · `check_catalog_keys` 가 세우다 밟은 자리)."""
    show = "--list" in sys.argv
    bad, soft, scanned = [], [], 0
    for path in asmdefs():
        folder = os.path.dirname(path)
        try:
            data = json.load(io.open(path, encoding="utf-8"))
        except Exception as e:                                   # noqa: BLE001 — 깨진 asmdef 도 알려야 한다
            print("asmdef 를 읽을 수 없다: " + path + " (" + str(e) + ")")
            return 1
        name = data.get("name", os.path.basename(path))
        for base, _dirs, files in os.walk(folder):
            for f in sorted(files):
                if not f.endswith(".cs"):
                    continue
                p = os.path.join(base, f)
                scanned += 1
                for n, line in enumerate(io.open(p, encoding="utf-8"), 1):
                    m = USING.match(line)
                    if not m:
                        continue
                    ns = m.group(1)
                    # 가장 긴 것부터 맞춘다(UnityEngine.Rendering.Universal 이 UnityEngine.Rendering 보다 먼저)
                    hit = False
                    for key in sorted(NEEDS, key=len, reverse=True):
                        if ns == key or ns.startswith(key + "."):
                            hit = True
                            if not has(NEEDS[key], data):
                                bad.append((os.path.relpath(p, ROOT), n, ns, name, NEEDS[key]))
                            elif show:
                                print("· " + os.path.relpath(p, ROOT) + ":" + str(n) + " " + ns + " ✔ (" + name + ")")
                            break
                    if hit:
                        continue
                    for key in sorted(SOFT, key=len, reverse=True):
                        if ns == key or ns.startswith(key + "."):
                            if not has(SOFT[key], data):
                                soft.append((os.path.relpath(p, ROOT), n, ns, name, SOFT[key]))
                            break

    # ⚑ 공허 방지 — 이 자에는 이것이 **없었다**(T506 실측).
    #   길이 바뀌거나 `os.walk` 이 빈손으로 와도 «참조 없는 네임스페이스 사용 0» 이 그대로 찍히고 rc 0 이었다.
    #   막는 자가 그러면 런은 초록인데 **아무것도 안 재고** 지나간다 — 이 자가 막으려던 사고(CI #346~#348)가
    #   그대로 돌아온다. 문턱은 실측(.cs 178 · 어셈블리 2)의 한참 아래다 — **재는 값이 아니라
    #   «아무것도 안 읽었다» 를 잡는 값이다**(결정 930: 모르는 값에 문턱을 걸지 않는다).
    if not asmdefs() or scanned < 20:
        print("✗ check_test_usings: 못 쟀다 — 테스트 어셈블리 " + str(len(asmdefs()))
              + "개 · .cs " + str(scanned) + "개밖에 못 읽었다(길이 바뀌었나). "
              "이 수로 말하는 «사용 0» 은 못 믿는다.")
        return 1

    if soft:
        print("⚠ 알림(실패는 아니다) — 이 네임스페이스는 유니티 코어에도 있어 «using» 만으로는 못 가른다."
              " 안에서 쓰는 타입이 URP 것이면 CI 유니티 잡이 컴파일 단계에서 죽는다:")
        for p, n, ns, name, need in soft:
            print("  " + p + ":" + str(n) + "  using " + ns + "  → " + name + " 에 «" + str(need[0]) + "» 참조가 없다")

    if bad:
        print("테스트 어셈블리가 참조하지 않는 네임스페이스를 쓰고 있다 — CI 유니티 잡이 컴파일 단계에서 죽는다(결정 465):")
        for p, n, ns, name, need in bad:
            asm, dll = need
            print("  " + p + ":" + str(n) + "  using " + ns
                  + "  → " + name + " 에 «" + str(asm) + "»(또는 precompiled «" + str(dll) + "»)가 없다")
        print("고치는 법: ⓐ 그 부분을 DOTween 을 참조하는 쪽(예 UiKit)의 API 로 옮겨 자에게는 값만 준다(결정 465가 택한 길) ·"
              " 또는 ⓑ asmdef 에 참조를 더한다(이름이 틀리면 또 파손이라 CI 로만 검증된다).")
        return 1
    print("✓ check_test_usings: 테스트 .cs " + str(scanned) + "개 · 어셈블리 " + str(len(asmdefs()))
          + "개 · 참조 없는 네임스페이스 사용 0")
    return 0


ASMDEF_BARE = {"name": "Bare.Tests", "overrideReferences": True, "precompiledReferences": ["nunit.framework.dll"]}
ASMDEF_DOTWEEN = {"name": "Dot.Tests", "overrideReferences": True, "references": ["DOTween.Modules"]}
ASMDEF_DLL = {"name": "Dll.Tests", "overrideReferences": True, "precompiledReferences": ["DOTween.dll"]}


def _run_on(tmp, asmdef, files, nfill=25):
    """가짜 테스트 폴더 하나를 세우고 이 자를 그 위에서 돌린다 → (rc, 찍은 글).

    ⚠ 밑동을 `nfill` 개 채운다 — 공허 방지 문턱(.cs 20)을 못 넘기면 재려던 갈래가 전부
    «못 쟀다» 로 빨개진다(`check_catalog_keys` 가 세우다 밟은 그 자리).
    """
    d = os.path.join(tmp, "Tests")
    os.makedirs(d, exist_ok=True)
    if asmdef is not None:
        with io.open(os.path.join(d, "T.asmdef"), "w", encoding="utf-8") as fh:
            fh.write(asmdef if isinstance(asmdef, str) else json.dumps(asmdef))
    for i in range(nfill):
        with io.open(os.path.join(d, "_fill%02d.cs" % i), "w", encoding="utf-8") as fh:
            fh.write("using System;\nclass F%d { }\n" % i)
    for name, src in files.items():
        with io.open(os.path.join(d, name), "w", encoding="utf-8") as fh:
            fh.write(src)
    g = globals(); old = (g["ROOT"], g["TESTS"])
    g["ROOT"] = tmp; g["TESTS"] = d
    buf = io.StringIO()
    try:
        import contextlib
        with contextlib.redirect_stdout(buf):
            rc = run()
    finally:
        g["ROOT"], g["TESTS"] = old
    return rc, buf.getvalue()


def self_test():
    """⚑ **이 자에는 자기 검사가 없었다** — T492 가 «자기 검사 없는 자 다섯» 으로 세어 둔 그 하나다.

    이 자는 **막는 자**다(`ci.yml` 의 `dotnet` 잡 · `continue-on-error` 없음 · `unity-test` 가 그 잡에 매달려 있다).
    그리고 세우고 보니 **공허 방지가 아예 없었다** — 길이 어긋나면 «사용 0» 이라 찍고 초록으로 지나갔다.
    이 자가 막으려던 사고는 «CI 유니티 잡이 테스트를 한 개도 못 돌리고 죽는 것»(CI #346~#348)이라,
    조용한 거짓의 값이 특히 크다.

    ⚠ 갈래 ⓖ·ⓗ 는 «고쳐야 할 것» 이 아니라 **한계를 못 박는 것**이다 — 머리글의 «못 보는 자리» 와 한 몸이다.
    """
    import shutil, tempfile
    cases = [
        ("ⓐ 참조가 있으면 조용하다",
         ASMDEF_DOTWEEN, {"A.cs": "using DG.Tweening;\nclass A { }\n"}, 0, "사용 0"),
        ("ⓑ 참조 없이 `using DG.Tweening;` 이면 **파일·줄**과 함께 운다",
         ASMDEF_BARE, {"B.cs": "using System;\nusing DG.Tweening;\nclass B { }\n"}, 1, "B.cs:2"),
        ("ⓒ `precompiledReferences` 의 `DOTween.dll` 로도 통과한다(두 길이 다 열려 있다)",
         ASMDEF_DLL, {"C.cs": "using DG.Tweening;\nclass C { }\n"}, 0, "사용 0"),
        ("ⓓ 하위 네임스페이스(`DG.Tweening.Core`)도 문다 — 점 뒤가 달라도 같은 dll 이다",
         ASMDEF_BARE, {"D.cs": "using DG.Tweening.Core;\nclass D { }\n"}, 1, "DG.Tweening.Core"),
        ("ⓔ `TMPro` 도 같은 손으로 문다",
         ASMDEF_BARE, {"E.cs": "using TMPro;\nclass E { }\n"}, 1, "TMPro"),
        ("ⓕ `SOFT` 표의 URP 는 **알림**이다 — 찍되 빨갛게는 안 한다(결정 493 의 «알리는 자»)",
         ASMDEF_BARE, {"F.cs": "using UnityEngine.Rendering.Universal;\nclass F { }\n"}, 0, "알림(실패는 아니다)"),
        ("ⓖ ⚠ **긴 것 먼저** — `…Rendering.Universal` 이 `…Rendering` 으로 잡히면 처방의 어셈블리 이름이 틀려진다",
         ASMDEF_BARE, {"G.cs": "using UnityEngine.Rendering.Universal;\nclass G { }\n"}, 0,
         "Unity.RenderPipelines.Universal.Runtime"),
        ("ⓗ ⚠ **한계** — `//` 로 주석 친 `using` 은 안 센다(그 줄을 세면 «문서에 적었다» 가 빨강이 된다)",
         ASMDEF_BARE, {"H.cs": "// using DG.Tweening;\nclass H { }\n"}, 0, "사용 0"),
        ("ⓘ 깨진 asmdef 는 **조용히 넘기지 않는다** — 못 읽으면 그 자리에서 운다",
         "{ not json", {"I.cs": "class I { }\n"}, 1, "읽을 수 없다"),
    ]
    bad = 0
    tmp = tempfile.mkdtemp(prefix="testusings-")
    try:
        for i, (name, asmdef, files, want_rc, want_in) in enumerate(cases):
            rc, out = _run_on(os.path.join(tmp, "c%02d" % i), asmdef, files)
            ok = (rc == want_rc) and (want_in is None or want_in in out)
            bad += 0 if ok else 1
            print("  %s %s (rc %d, 기대 %d)" % ("✔" if ok else "✘", name, rc, want_rc))
        # ⓙ 공허 방지 — 이 자에 **없던 것**이다(T506 이 보탰다).
        for label, asmdef, nfill, want in (
                ("어셈블리가 0개면", None, 25, "어셈블리 0개"),
                (".cs 를 몇 개밖에 못 읽으면", ASMDEF_BARE, 2, ".cs 2개밖에")):
            rc, out = _run_on(os.path.join(tmp, "e" + label[:3]), asmdef, {}, nfill=nfill)
            ok = rc == 1 and "못 쟀다" in out and want in out
            bad += 0 if ok else 1
            print("  %s ⓙ %s «사용 0» 이 아니라 **«못 쟀다»** 로 운다 (rc %d, 기대 1)"
                  % ("✔" if ok else "✘", label, rc))
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    if bad:
        print("✗ check_test_usings --self-test: 갈래 %d건이 기대와 다르다" % bad)
        return 1
    print("✓ check_test_usings --self-test: 갈래 %d개가 전부 기대대로 갈린다 "
          "(ⓖ·ⓗ 는 «일부러/원리적으로 안 본다» 를 못 박은 갈래다 — 고침이 아니라 한계다)" % (len(cases) + 2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
