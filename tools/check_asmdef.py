#!/usr/bin/env python3
"""`using` 한 유니티 «패키지» 네임스페이스가 그 폴더의 asmdef 참조에 들어 있는지 본다 (T189).

왜 있나 — 2026-09-07 12:0X 에 CI 유니티 잡이 **테스트를 한 개도 못 돌리고** 죽었다:

  Assets/Scripts/Game/PostFx.cs(32,16): error CS0246: The type or namespace name 'Volume' …

`Volume`·`VolumeProfile` 은 `Unity.RenderPipelines.**Core**.Runtime`(네임스페이스 `UnityEngine.Rendering`)에 있는데
`KkomaKnight.Game.asmdef` 은 `…Universal.Runtime` 만 참조하고 있었다. **빨간 테스트보다 나쁜 상태**다
(테스트가 0개라 아무 게이트도 판정을 못 하고, `screens`·gh-pages 도 같이 막힌다).

**로컬 게이트로는 구조적으로 못 잡는다** — `tools/dotnet` 하니스는 NuGet 참조 어셈블리로 컴파일하느라
URP·TMP 타입을 아예 안 물고, asmdef 을 읽지도 않는다. 그래서 `dotnet build` 도 임시 csproj 사전 점검(결정 143)도
**초록인데** 유니티만 죽는다. 워커 A 가 규칙으로 적었지만(§1), 오늘 «규칙만»으로 네 번 놓친 전례가 있어(T184) 자로 만든다.

무엇을 하나 — `Assets/Scripts` 아래 `.cs` 마다 «가장 가까운 조상 asmdef» 을 찾고,
그 파일의 `using` 중 **아래 표에 있는** 네임스페이스가 asmdef 의 `references` 에 없으면 파일·줄로 찍는다.
표에 없는 네임스페이스는 **아무 말도 안 한다**(오탐 0 이 이 자의 값어치다 — 유니티 없이 «모든» 매핑을 알 수는 없다).
새 패키지를 쓰기 시작하면 표에 한 줄 더한다.

**한계 — `Assets/Tests` 는 일부러 안 본다.** 테스트 asmdef 은 `overrideReferences`·`precompiledReferences` 와
자동 참조가 얽혀 있어(예: `DG.Tweening` 을 `references` 에 안 적고도 컴파일된다) 이 표로는 모델링이 안 된다.
넣어 봤더니 **지금 멀쩡히 컴파일되는 트리에서 오탐 80건**이 났다 — 자가 소음이 되면 아무도 안 본다.
실제 파손이 난 자리는 게임 코드였고(`Assets/Scripts/Game/PostFx.cs`), 거기만 본다.

쓰는 법 (커밋 «직전» · ROUTINE §3 게이트 목록):
  python3 tools/check_asmdef.py            # 어긋나면 1 로 끝난다
  python3 tools/check_asmdef.py --list     # 표와 asmdef 참조를 찍어만 본다
"""
import json
import os
import re
import sys

# ⚠ `Assets/Tests` 는 **일부러 뺐다**(아래 «한계» 참조) — 테스트 asmdef 은 precompiledReferences·자동 참조로
# 풀리는 것이 있어 이 표로는 모델링이 안 되고, 넣으면 오탐이 80건 난다(실측). 파손이 실제로 난 자리는 게임 코드다.
ROOTS = ["Assets/Scripts"]

# 네임스페이스 → 그것을 담은 어셈블리(= asmdef references 에 적는 이름).
# 근거: 워커 A 의 실측(커밋 `ad0d3d2e` · 결정 464)과 이 레포가 실제로 참조하는 이름들.
# ⚠ 긴 네임스페이스를 먼저 본다(`UnityEngine.Rendering.Universal` 이 `UnityEngine.Rendering` 보다 앞).
NS_TO_ASM = [
    ("UnityEngine.Rendering.Universal", "Unity.RenderPipelines.Universal.Runtime"),
    ("UnityEditor.Rendering.Universal", "Unity.RenderPipelines.Universal.Editor"),
    ("UnityEngine.Rendering", "Unity.RenderPipelines.Core.Runtime"),
    ("TMPro", "Unity.TextMeshPro"),
    ("DG.Tweening", "DOTween.Modules"),
    ("UnityEngine.TestTools", "UnityEngine.TestRunner"),
    ("UnityEditor.TestTools", "UnityEditor.TestRunner"),
    ("NUnit.Framework", "nunit.framework.dll"),
    ("UnityEngine.InputSystem", "Unity.InputSystem"),
    ("Unity.Mathematics", "Unity.Mathematics"),
    ("UnityEngine.U2D.Animation", "Unity.2D.Animation.Runtime"),
]

USING_RE = re.compile(r'^\s*using\s+(?:static\s+)?([A-Za-z_][\w.]*)\s*;')


def asmdefs(roots=None):
    """폴더 → asmdef 내용. 파일이 놓인 폴더에서 위로 올라가며 가장 가까운 것을 쓴다."""
    out = {}
    for root in (roots if roots is not None else ROOTS):
        for dirpath, _dirs, files in os.walk(root):
            for f in files:
                if f.endswith(".asmdef"):
                    p = os.path.join(dirpath, f)
                    try:
                        out[os.path.normpath(dirpath)] = (p, json.load(open(p, encoding="utf-8")))
                    except (OSError, ValueError) as e:
                        print("!! asmdef 을 못 읽는다: %s (%s)" % (p, e))
    return out


def owner(path, table):
    d = os.path.dirname(os.path.normpath(path))
    while True:
        if d in table:
            return table[d]
        nd = os.path.dirname(d)
        if nd == d or not d:
            return None
        d = nd


def refs_of(data):
    """references + precompiledReferences 를 합쳐 본다 — GUID 로 적힌 것은 이름을 모르니 건너뛴다.
    (`nunit.framework.dll` 처럼 «dll» 은 references 가 아니라 precompiledReferences 에 적힌다.)"""
    out = {r for r in data.get("references", []) if not r.startswith("GUID:")}
    out |= set(data.get("precompiledReferences", []))
    return out


def scan(roots, table):
    """`roots` 아래 `.cs` 를 훑어 «asmdef 참조가 빠진 using» 을 모은다 — 본 검사와 자기 검사가 **같은 코드**를 탄다."""
    bad, scanned = [], 0
    for root in roots:
        for dirpath, _dirs, files in os.walk(root):
            for f in files:
                if not f.endswith(".cs"):
                    continue
                path = os.path.join(dirpath, f)
                own = owner(path, table)
                if own is None:
                    continue
                asmpath, data = own
                have = refs_of(data)
                scanned += 1
                try:
                    lines = open(path, encoding="utf-8").read().split("\n")
                except OSError:
                    continue
                for i, line in enumerate(lines, 1):
                    m = USING_RE.match(line)
                    if not m:
                        continue
                    ns = m.group(1)
                    for pref, asm in NS_TO_ASM:
                        if ns == pref or ns.startswith(pref + "."):
                            if asm not in have:
                                bad.append((path, i, ns, asm, asmpath))
                            break

    return bad, scanned


SELF_TEST_NS, SELF_TEST_ASM = "UnityEngine.Rendering", "Unity.RenderPipelines.Core.Runtime"


def self_test():
    """
    **이 자가 실제로 잡는가**를 확인한다 — 2026-09-07 의 그 파손(`PostFx.cs` 의 `Volume` · 결정 464)을
    가짜 트리로 재현해 ⓐ 참조가 빠지면 **1** 로 끝나고 ⓑ 넣으면 **0** 인지 본다.
    자기 검사가 없으면 «늘 초록인 자» 와 «잡는 자» 를 구별할 수 없다(T134 의 `webgl_smoke --self-test` 전례).
    """
    import shutil
    import tempfile
    tmp = tempfile.mkdtemp(prefix="check_asmdef_selftest_")
    try:
        d = os.path.join(tmp, "Game")
        os.makedirs(d)
        open(os.path.join(d, "Fake.cs"), "w", encoding="utf-8").write(
            "using UnityEngine;\nusing %s;\nclass Fake { }\n" % SELF_TEST_NS)

        def run(refs):
            json.dump({"name": "Fake", "references": refs},
                      open(os.path.join(d, "Fake.asmdef"), "w", encoding="utf-8"))
            return scan([tmp], asmdefs([tmp]))

        bad, scanned = run([])                       # ⓐ 참조를 뺐다 → 잡아야 한다
        if scanned != 1 or not bad:
            print("⛔ 자기 검사 실패 — 참조가 빠졌는데 못 잡았다(훑은 파일 %d · 걸린 것 %d)" % (scanned, len(bad)))
            return 1
        if bad[0][3] != SELF_TEST_ASM:
            print("⛔ 자기 검사 실패 — 필요한 어셈블리를 «%s» 로 잘못 짚었다(«%s» 여야 한다)" % (bad[0][3], SELF_TEST_ASM))
            return 1

        bad2, _ = run([SELF_TEST_ASM])               # ⓑ 넣었다 → 조용해야 한다
        if bad2:
            print("⛔ 자기 검사 실패 — 참조를 넣었는데도 걸린다(거짓 경고 · %s)" % (bad2[0],))
            return 1

        print("✓ check_asmdef --self-test: 참조를 빼면 잡고(→ %s) 넣으면 조용하다" % SELF_TEST_ASM)
        return 0
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


def main(argv):
    if "--self-test" in argv:
        return self_test()
    table = asmdefs()
    if "--list" in argv:
        for d, (p, data) in sorted(table.items()):
            print("· %s\n    참조: %s" % (p, ", ".join(sorted(refs_of(data))) or "(없음)"))
        print("\n네임스페이스 → 어셈블리 표:")
        for ns, asm in NS_TO_ASM:
            print("  %-34s → %s" % (ns, asm))
        return 0

    bad, scanned = scan(ROOTS, table)
    if not bad:
        print("✓ check_asmdef: `.cs` %d개의 using 이 전부 제 asmdef 참조 안에 있다 (표 %d줄)" % (scanned, len(NS_TO_ASM)))
        return 0

    print("⛔ asmdef 참조가 빠졌다 — **유니티 CI 가 컴파일 단계에서 죽는다**(테스트 0개 · 빨간 테스트보다 나쁘다):")
    seen = set()
    for path, ln, ns, asm, asmpath in bad:
        key = (asmpath, asm)
        print("  · %s:%d  `using %s;`" % (path, ln, ns))
        if key not in seen:
            seen.add(key)
            print("      → %s 의 \"references\" 에 \"%s\" 를 넣는다" % (asmpath, asm))
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
