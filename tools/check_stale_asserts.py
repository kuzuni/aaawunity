#!/usr/bin/env python3
"""바꾼 값·이름을 «아직 박아 두고 있는» 테스트 자리를 커밋 «전» 에 찾아 준다 (T184).

왜 있나 — 2026-09-07 오전에 main 이 세 시간 빨갰는데 원인 다섯이 **전부 같은 종류**였다:

  T168  로비 «이벤트» 버튼을 지웠는데 `UiSmokeTests` 가 오브젝트 이름 "Events" 로 아직 찾는다
  T179  `ArrowAngle` 을 -35 → 0 으로 바꿨는데 옛 단언이 `-35f` 를 리터럴로 박고 있다
  T161  `typeName` 을 안 쓰기로 했는데 옛 이름을 박아 둔 게이트 셋이 남아 있다
  T166  shine 사용자를 하나 더 만들었는데 누수 자가 «씬 전체 개수» 를 세고 있다
  T177  잠긴 글자를 #666666 으로 했는데 `UiSmokeTests` 의 T84 «밝기 ≥ 0.55» 단언이 남아 있다

규칙은 이미 결정 425·426·427·429 에 **네 번** 적혔다. 네 번 적고도 네 번 놓쳤으면 사람이 아니라 자가 할 일이다.
로컬에서 못 걸리는 까닭도 분명하다 — 이 자리는 전부 **PlayMode** 라 `dotnet test` 가 안 돌리고,
임시 csproj 사전 점검(결정 143)은 **컴파일만** 본다. `UiKit.Find(...)` 가 null 을 돌려주는 것은 컴파일 오류가 아니다.

무엇을 하나 — 작업 트리(또는 지정한 범위)의 diff 에서 `Assets/Scripts/**` 가 **지운** 것 중
  ⓐ 문자열 리터럴("Events" · " (신화)" 같은 것)
  ⓑ 숫자 리터럴(-35f · 0.55f · 200f …)
  ⓒ public 멤버 이름(지워지거나 이름이 바뀐 const·필드·메서드)
을 모아, 그것이 `Assets/Tests/**` 에 **아직 남아 있으면** 파일·줄로 찍는다.
그 자리가 «옛 전제를 든 단언» 이면 고치고, 아니면(우연히 같은 글자) 그냥 지나가면 된다.

쓰는 법 (커밋 «직전» · ROUTINE §3 게이트 목록):
  python3 tools/check_stale_asserts.py                 # 작업 트리(아직 커밋 안 한 것)
  python3 tools/check_stale_asserts.py HEAD~1          # 마지막 커밋이 무엇을 남겼나
  python3 tools/check_stale_asserts.py --strict        # 하나라도 있으면 1 로 끝난다(기본은 찍기만 하고 0)

기본이 «찍기만» 인 까닭 — 같은 글자가 우연히 겹치는 일(흔한 낱말·0.5f 같은 수)이 있어서
사람이 한 번 보고 넘길 자리가 섞인다. **찾아 주는 것이 일의 9할**이고, 판정은 워커가 한다.
"""
import re
import subprocess
import sys

SRC = "Assets/Scripts/"
TESTS = "Assets/Tests/"

# 너무 흔해서 찍어 봐야 소음인 것 — 이 자가 노리는 것은 «그 자리에만 있는» 값·이름이다
NOISE_STRINGS = {"", " ", "\\n", "/", ".", ",", "-", "+", "%", "Text", "Image", "Bg", "Icon", "Content"}
NOISE_NUMS = {"0", "1", "2", "3", "4", "0f", "1f", "2f", "0.5f", "1.0f", "0.0f", "100", "255", "10", "-1"}
MIN_STR = 3        # 3글자 미만 문자열은 우연이 너무 잦다
MIN_NAME = 5       # 짧은 이름도 마찬가지
# 여러 자리에 걸리는 값은 «그 자리 값» 이 아니라 흔한 상수다(0.9f 가 알파로 여섯 군데에서 걸렸다) —
# 수는 3자리, 문자열·이름은 6자리를 넘으면 버린다. 이 자가 노리는 것은 «한 자리를 콕 가리키는» 값이다.
MAX_HITS_NUM = 3
MAX_HITS_TEXT = 6

STR_RE = re.compile(r'"((?:[^"\\]|\\.){2,80})"')
NUM_RE = re.compile(r'(?<![\w.])(-?\d+(?:\.\d+)?f?)(?![\w.])')
NAME_RE = re.compile(r'\b(?:public|internal)\s+(?:static\s+|const\s+|readonly\s+|sealed\s+|override\s+|virtual\s+)*'
                     r'[\w<>,\[\]\.]+\s+(\w+)\s*(?:=|\(|=>|;|\{)')


def diff(rev):
    cmd = ["git", "diff", "-U0"] + ([rev] if rev else []) + ["--", SRC]
    return subprocess.run(cmd, capture_output=True, text=True).stdout


def removed_lines(text):
    out = []
    for line in text.split("\n"):
        if line.startswith("-") and not line.startswith("---"):
            out.append(line[1:])
    return out


def added_text(text):
    return "\n".join(l[1:] for l in text.split("\n") if l.startswith("+") and not l.startswith("+++"))


def tokens(rem_lines, add_text):
    """지운 줄에서 뽑되, 같은 diff 가 «다시 넣은» 것은 뺀다(자리만 옮긴 것은 결함이 아니다)."""
    strs, nums, names = set(), set(), set()
    for line in rem_lines:
        if line.lstrip().startswith(("//", "/*", "*", "///")):
            continue   # 주석 줄은 값이 아니다
        for m in STR_RE.finditer(line):
            v = m.group(1)
            if len(v) >= MIN_STR and v not in NOISE_STRINGS:
                strs.add(v)
        for m in NUM_RE.finditer(line):
            v = m.group(1)
            if v not in NOISE_NUMS:
                nums.add(v)
        for m in NAME_RE.finditer(line):
            v = m.group(1)
            if len(v) >= MIN_NAME:
                names.add(v)
    strs = {v for v in strs if '"%s"' % v not in add_text}
    nums = {v for v in nums if not re.search(r'(?<![\w.])%s(?![\w.])' % re.escape(v), add_text)}
    names = {v for v in names if not re.search(r'\b%s\b' % re.escape(v), add_text)}
    return strs, nums, names


def hits(needle, literal, at=None):
    """<at> 트리(없으면 작업 트리)의 Assets/Tests 에서 그 값을 쓰는 줄 — git grep 이라 무시 파일을 안 판다.

    ⚠ <at> 이 중요하다: 과거 커밋을 검증할 때 작업 «트리» 를 뒤지면 이미 고쳐진 뒤라 늘 0 이 나온다
    (이 자를 만들며 T179 로 실제로 겪었다 · 결정 참조)."""
    pat = ('"%s"' % needle) if literal == "str" else (r'\b%s\b' % re.escape(needle) if literal == "name"
                                                     else r'(?<![\w.])%s(?![\w.])' % re.escape(needle))
    cmd = ["git", "grep", "-n", "--fixed-strings" if literal == "str" else "-P", pat]
    if at:
        cmd.append(at)
    cmd += ["--", TESTS]
    r = subprocess.run(cmd, capture_output=True, text=True)
    return [l for l in r.stdout.split("\n") if l.strip()][:MAX_HITS_TEXT + 1]


def grep_tree(rev):
    """어느 트리에서 테스트를 찾을 것인가 — 「A..B」 면 B, 「HEAD~1」 처럼 한쪽만 주면 작업 트리(None)."""
    if rev and ".." in rev:
        right = rev.split("..")[-1].strip()
        return right or "HEAD"
    return None


def main(argv):
    strict = "--strict" in argv
    rev = next((a for a in argv if not a.startswith("--")), None)
    d = diff(rev)
    at = grep_tree(rev)
    if not d.strip():
        print("✓ check_stale_asserts: %s 에 바뀐 것이 없다" % SRC)
        return 0
    strs, nums, names = tokens(removed_lines(d), added_text(d))

    found = []
    for group, kind, label in ((strs, "str", "문자열"), (names, "name", "이름"), (nums, "num", "수")):
        for v in sorted(group):
            h = hits(v, kind, at)
            cap = MAX_HITS_NUM if kind == "num" else MAX_HITS_TEXT
            if h and len(h) <= cap:
                found.append((label, v, h))

    if not found:
        print("✓ check_stale_asserts: 지운 값·이름을 아직 가리키는 테스트 자리 0 "
              "(문자열 %d · 이름 %d · 수 %d 를 훑었다)" % (len(strs), len(names), len(nums)))
        return 0

    print("⚠ 이 diff 가 지운 값·이름을 «아직» 가리키는 테스트 자리 %d 건 — 옛 전제를 든 단언인지 하나씩 본다"
          "(결정 425·426·427·429 가 네 번 놓친 그 종류다):" % len(found))
    for label, v, h in found:
        print("  · 지운 %s «%s»" % (label, v))
        for line in h:
            print("      %s" % line[:180])
    print("고칠 것이면 같은 커밋에서 고치고, 우연히 같은 글자면 그냥 지나간다(이 자는 찾아 줄 뿐 판정하지 않는다).")
    return 1 if strict else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
