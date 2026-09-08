#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""«한 기능의 정의와 참조가 서로 다른 커밋으로 나뉘어 밀린» 자리를 찾는다 (T270 · 검수 워커 Q).

무엇이 문제였나
  주인이 로컬 유니티에서 `CS0234 Revive` · `CS0103 GachaKeys` · `CS0246 QuestData` 를 봤다.
  세 파일(`Core/Revive.cs`·`Core/GachaKeys.cs`·`Core/Quest.cs`)은 **origin/main 에 다 있다** —
  즉 «main 이 깨졌다» 가 아니라 **참조하는 파일이 먼저 밀리고 정의하는 파일이 나중에 밀린 «중간 순간»** 을
  주인 에디터가 집은 것이다(ROUTINE ⚑ 2026-09-09 06:4X · §1 «컴파일 안 되는 커밋 금지»).

  이 꼴은 **오늘의 어느 게이트도 못 잡는다** — 게이트는 전부 «지금 트리» 만 보고,
  CI 도 밀린 뒤의 머리 커밋만 본다. 중간 커밋이 컴파일 불가여도 아무도 빨개지지 않는데,
  주인 에디터는 그 순간을 실제로 연다.

무엇을 재는가
  최근 N개 커밋을 하나씩 되짚어 그 시점의 `Assets/**/*.cs` 를 통째로 읽고,
  «**타입 자리**에서 쓰이는데 그 커밋에는 정의가 없는 이름» 과
  «`using KkomaKnight.X;` 인데 그 커밋에 그 네임스페이스가 없는 것» 을 센다.

  검사 대상 이름은 **창 안에서 한 번이라도 정의된 프로젝트 타입뿐**이다 —
  유니티·패키지·플러그인 타입은 애초에 후보에 안 들어가므로 오탐이 되지 않는다.

컴파일러가 아니다 (한계를 먼저 적는다)
  이 자리에는 `dotnet` 이 없는 세션도 있고(설치 경로가 막혀 있다), 있어도 커밋 20개를 각각
  빌드하면 한 회차가 통째로 든다. 그래서 **근사치**로 잰다 — 문자열·주석을 지우고
  «타입 자리»(`new T` · `typeof(T)` · `: T` · `<T>` · `(T)` · `T.Static` · `T 변수`)로 쓰인 것만 센다.
  멤버 이름(`x.Attendance`) · 문자열 속 낱말(`"Day:"`)은 세지 않는다 — 그 둘이 첫 판의 오탐 전부였다.
  못 잡는 것: 메서드·필드 하나가 늦게 밀린 경우(CS0117/CS1061)는 타입이 있으니 안 걸린다.
  즉 **여기가 초록이라고 그 커밋이 컴파일된다는 뜻은 아니다** — «타입이 통째로 없는» 제일 흔한 꼴만 막는다.

자가 정말 무는가
  `Assets/Scripts/Core/GachaKeys.cs` 를 지운 커밋을 하나 만들어 돌리니
    ✗ CS0246  타입 «GachaKeys» 정의 없음 ← Mail.cs, ShopScreen.cs, AttendanceTests.cs, GachaKeysTests.cs
  로 빨개졌다(주인이 본 그 이름 그대로). 되돌리니 초록. 지금 트리에서 최근 51커밋 오탐 0.

쓰는 법
    python3 tools/check_split_push.py            # 최근 20커밋 (HEAD 기준)
    python3 tools/check_split_push.py 60          # 창을 넓혀서
    python3 tools/check_split_push.py 20 origin/main
  깨진 커밋이 있으면 1 로 끝난다. 커밋 하나에 3~5초쯤 걸린다(20개 ≈ 1분 30초).

걸렸으면 어떻게 고치나
  그 기능의 «정의 + 참조» 를 **한 커밋에** 넣는다(파일을 나눠 밀지 말 것). 이미 밀렸으면
  다음 커밋에서 합쳐도 «지나간 중간 순간» 은 못 되돌리므로, 주인이 그 사이에 에디터를 열면
  그대로 빨간 줄을 본다 — 그러니 **미는 순간에** 지키는 것이 유일한 수단이다.
"""
import collections
import io
import os
import re
import subprocess
import sys
import tarfile

DECL = re.compile(r"\b(?:class|struct|enum|interface)\s+([A-Za-z_][A-Za-z0-9_]*)")
NSDEC = re.compile(r"^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)", re.M)
USING = re.compile(r"^\s*using\s+(?:static\s+)?([A-Za-z_][A-Za-z0-9_.]*)\s*;", re.M)
IDENT = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")

# C# 키워드 + 문맥 키워드 — 타입 이름으로 세지 않는다.
KEYWORDS = set("""abstract as base bool break byte case catch char checked class const continue decimal default
delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int
interface internal is lock long namespace new null object operator out override params private protected public
readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof
uint ulong unchecked unsafe ushort using virtual void volatile while var yield get set value where partial""".split())


def strip_noise(src):
    """문자열·문자·주석을 공백으로 지운다(길이는 보존 — 뒤의 자리 계산이 어긋나지 않게)."""
    out, i, n = [], 0, len(src)
    while i < n:
        c = src[i]
        if c == "/" and i + 1 < n and src[i + 1] == "/":
            j = src.find("\n", i)
            j = n if j < 0 else j
            out.append(" " * (j - i))
            i = j
        elif c == "/" and i + 1 < n and src[i + 1] == "*":
            j = src.find("*/", i + 2)
            j = n if j < 0 else j + 2
            out.append(re.sub(r"[^\n]", " ", src[i:j]))
            i = j
        elif c in ('"', "'"):
            verbatim = c == '"' and i > 0 and src[i - 1] == "@"
            j = i + 1
            while j < n:
                if verbatim:
                    if src[j] == '"':
                        if j + 1 < n and src[j + 1] == '"':
                            j += 2
                            continue
                        j += 1
                        break
                    j += 1
                else:
                    if src[j] == "\\":
                        j += 2
                        continue
                    if src[j] == c:
                        j += 1
                        break
                    if src[j] == "\n":
                        break
                    j += 1
            out.append(re.sub(r"[^\n]", " ", src[i:j]))
            i = j
        else:
            out.append(c)
            i += 1
    return "".join(out)


def type_positions(src):
    """«타입 자리»로 쓰인 식별자만 모은다(멤버 이름·선언되는 이름 자신은 뺀다)."""
    hit = set()
    for m in IDENT.finditer(src):
        t = m.group(0)
        if t in KEYWORDS or len(t) < 3:
            continue
        a, b = m.start(), m.end()
        # 앞뒤로 창을 좁혀 본다 — 앞부분을 통째로 뜨면 파일 길이의 제곱이 된다.
        before = src[max(0, a - 48):a].rstrip()
        if before.endswith("."):          # x.Member — 타입 자리가 아니다
            continue
        pw = re.search(r"([A-Za-z_][A-Za-z0-9_]*)\s*$", before)
        pw = pw.group(1) if pw else ""
        if pw in ("class", "struct", "enum", "interface", "namespace", "void"):
            continue                      # 선언되는 이름 자신
        after = src[b:b + 64]
        nxt = after.lstrip()
        prevc = before[-1] if before else ""
        if pw in ("new", "typeof"):
            hit.add(t)
        elif prevc in ":<,(" and (nxt[:1] in ">,)." or re.match(r"[A-Za-z_]", nxt or " ")):
            hit.add(t)                    # : T · <T> · (T x · , T x
        elif nxt.startswith(".") and re.match(r"\.\s*[A-Za-z_]", nxt) and t[0].isupper():
            hit.add(t)                    # T.Static — 소문자는 지역 변수일 수 있어 뺀다
        elif t[0].isupper() and re.match(r"(\[\])?\s+[A-Za-z_][A-Za-z0-9_]*\s*[=;,)]", after):
            hit.add(t)                    # T x = … · T[] x;
    return hit


def snapshot(sha):
    """그 커밋의 Assets/**/*.cs 를 읽어 (정의 타입, 네임스페이스, 참조, using) 넷을 낸다."""
    raw = subprocess.run(["git", "archive", sha, "Assets"], capture_output=True).stdout
    defined, namespaces, refs, usings = set(), set(), {}, collections.defaultdict(list)
    with tarfile.open(fileobj=io.BytesIO(raw)) as tf:
        for m in tf.getmembers():
            if not (m.isfile() and m.name.endswith(".cs")):
                continue
            clean = strip_noise(tf.extractfile(m).read().decode("utf-8", "replace"))
            for d in DECL.finditer(clean):
                defined.add(d.group(1))
            for d in NSDEC.finditer(clean):
                parts = d.group(1).split(".")
                for i in range(1, len(parts) + 1):
                    namespaces.add(".".join(parts[:i]))
            for u in USING.finditer(clean):
                usings[u.group(1)].append(m.name)
            for t in type_positions(clean):
                refs.setdefault(t, []).append(m.name)
    return defined, namespaces, refs, usings


def main():
    n = 20
    ref = "HEAD"
    args = [a for a in sys.argv[1:] if not a.startswith("-")]
    if args:
        n = int(args[0])
    if len(args) > 1:
        ref = args[1]

    log = subprocess.run(["git", "log", "--format=%H %s", f"-{n}", ref],
                         capture_output=True, text=True).stdout.strip()
    if not log:
        print(f"✗ check_split_push: «{ref}» 에서 커밋을 못 읽었다")
        return 2
    commits = [c.split(" ", 1) for c in log.split("\n")]
    commits.reverse()   # 오래된 것부터

    snaps = [(sha, subj) + snapshot(sha) for sha, subj in commits]
    all_types = set().union(*(s[2] for s in snaps))
    proj_ns = {x for s in snaps for x in s[3] if x.split(".")[0] == "KkomaKnight"}

    bad = 0
    for sha, subj, defined, namespaces, refs, usings in snaps:
        miss_t = {t: p for t, p in refs.items() if t in all_types and t not in defined}
        miss_n = {u: p for u, p in usings.items() if u in proj_ns and u not in namespaces}
        if not (miss_t or miss_n):
            continue
        bad += 1
        print(f"\n✗ {sha[:9]} {subj[:78]}")
        for u, ps in sorted(miss_n.items()):
            print(f"    CS0234  네임스페이스 «{u}» 없음 ← {', '.join(os.path.basename(x) for x in ps[:4])}")
        for t, ps in sorted(miss_t.items()):
            print(f"    CS0246  타입 «{t}» 정의 없음 ← {', '.join(os.path.basename(x) for x in ps[:4])}")

    if bad:
        print(f"\n✗ check_split_push: 커밋 {len(snaps)}개 가운데 **{bad}개**가 그 시점에 컴파일 불가다 "
              f"— 그 기능의 «정의 + 참조» 를 한 커밋에 넣는다(ROUTINE §1)")
        return 1
    print(f"✓ check_split_push: 커밋 {len(snaps)}개 전부 «참조는 있는데 정의가 없는» 타입 0 "
          f"(타입 후보 {len(all_types)}개 · 네임스페이스 {len(proj_ns)}개)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
