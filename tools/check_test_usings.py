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

쓰기: python3 tools/check_test_usings.py [--list]
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


if __name__ == "__main__":
    sys.exit(main())
