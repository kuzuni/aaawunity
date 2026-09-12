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
을 모아, 그것이 `Assets/Tests/**` **또는 봇 각본(`Game/Playthrough.cs`)** 에 **아직 남아 있으면** 파일·줄로 찍는다.
그 자리가 «옛 전제를 든 단언» 이면 고치고, 아니면(우연히 같은 글자) 그냥 지나가면 된다.

쓰는 법 (커밋 «직전» · ROUTINE §3 게이트 목록):
  python3 tools/check_stale_asserts.py                 # 작업 트리(아직 커밋 안 한 것)
  python3 tools/check_stale_asserts.py HEAD~1          # 마지막 커밋이 무엇을 남겼나
  python3 tools/check_stale_asserts.py --strict        # 하나라도 있으면 1 로 끝난다(기본은 찍기만 하고 0)
  python3 tools/check_stale_asserts.py --self-test     # 이 자가 고장 났는지 (갈래 열하나 · T507)

⛑ **T507 — «못 봤다» 는 «깨끗하다» 와 딴 줄이다.** 전에는 오타 난 sha(`deadbeef~1..deadbeef`)에도
`✓ … 바뀐 것이 없다` · rc=0 으로 답했다 — **정직한 «깨끗하다» 와 한 글자도 다르지 않았다.**
그런데 이 자를 sha 로 돌리는 사람은 위 §1 규약대로 **거의 다 빨강을 쫓는 중**이고 sha 는 손으로 옮겨 적는다.
지금은 git 의 `returncode`·`stderr` 를 같이 보고 «그 범위를 **못 읽었다** … 이 답은 «깨끗하다» 가 아니라 «모른다» 다»
로 **딴 줄**을 찍는다(판정 rc 는 안 바꿨다 — 알리는 자다 · 결정 493 · T482 가 `ci_test_failures` 에서 놓은 꼴 그대로).

⛑ **T507 2회차 — 같은 병이 «옆문» 에 하나 더 있었다**(워커 J 가 1회차 뒤에 짚었다 · 결정 1406 ①).
1회차는 **`git diff` 쪽만** 갈랐다. `hits()` 가 부르는 **`git grep` 은 찾을 경로가 없어도 rc 1 · stdout 빈손 · stderr 도 빈손**이라
(실측) «안 걸렸다» 와 한 글자도 다르지 않았고, **1회차가 보탠 «훑은 수» 가 그 절반짜리 0 을 더 믿음직해 보이게** 만들었다 —
그 수는 **diff 쪽**을 증명하지 grep 쪽을 증명하지 않는다. 그래서 **증인이 둘**이다:
  · diff 쪽 = «문자열 N · 이름 N · 수 N 를 훑어»
  · grep 쪽 = «**자 파일 M개**에서 찾았다» (`searched_count`) — `M == 0` 이면 «**찾을 자리를 못 봤다**» 로 딴 줄

기본이 «찍기만» 인 까닭 — 같은 글자가 우연히 겹치는 일(흔한 낱말·0.5f 같은 수)이 있어서
사람이 한 번 보고 넘길 자리가 섞인다. **찾아 주는 것이 일의 9할**이고, 판정은 워커가 한다.

⚑⚑ **빨강을 진단하려면 «되돌려» 돌려라 — 답이 이미 여기 있었던 적이 있다** (T484 · 2026-09-11 실측):

  python3 tools/check_stale_asserts.py <sha>~1..<sha>      # 그 커밋이 남긴 옛 단언

  T462 의 `62bdebb0` 으로 그렇게 돌리면 «지운 문자열 «ProgressBar» → `Assets/Tests/PlayMode/UiSmokeTests.cs:850`»
  이 나오는데 **그것이 런 1096·1097 의 빨강 ① 그 자체**다. 그런데 그 뒤 **셋**(검수 Q 결정 1290 ⓐ ·
  워커 P · 워커 G)이 각자 CI 로그에서 **같은 자리를 다시 찾아냈다** — 이 자는 «커밋 직전에 쓰라» 고만
  적혀 있었고, 정작 «남이 밀고 난 뒤 빨강을 보는 사람» 은 아무도 안 돌렸다.
  ⇒ 규약 한 줄로 ROUTINE §1 에 세웠다(진단 전에 그 범위로 한 번).

⚠ **그 표본의 점수를 같이 적어 둔다 — 1적중 · 1소음 · 3놓침**(`62bdebb0`):
  · 적중 = 위 «ProgressBar».
  · 소음 = **같은 지운 줄**에 있던 `0.75f` 가 `PerkShineTests:105` 의 `At(0.75f)` 에 걸렸다(위 «흔한 수» 그것이다).
  · 놓침 = T462 의 나머지 셋(열 그라데이션 · 머리 배지 · 줄 번호). 셋 다 **이름도 값도 그대로인데 «뜻» 이
    바뀐** 자리라 이 자의 눈(«지운 값·이름»)에 **원리적으로 안 든다**. 같은 까닭으로 `7fccc87c`(차례만 바꿈)·
    `f63eb6c4`(글꼴이 옛 문턱을 깸)도 0건이다.
  ⇒ **이 자가 0 이라고 해서 «옛 단언 없음» 이 아니다.** 그리고 이 자를 더 세게 만드는 쪽은 소음이 먼저 늘어난다
    (오늘 표본이 이미 1:1) — «뜻이 바뀐 값» 을 보는 눈은 아직 없고, 그것을 세우려면 표본이 더 필요하다.
"""
import re
import subprocess
import sys

SRC = "Assets/Scripts/"
TESTS = "Assets/Tests/"
# T300 결정 1072 — **자와 같은 일을 하는데 소스 폴더에 사는 파일이 하나 있다.**
#   `Playthrough.cs` 는 배포 빌드에서 게임을 «놀아 보는» 봇의 각본이고, 화면을 **이름 계약**(`UiKit.Find("SummonBtn")` ·
#   `TapIn(root, "Pet:0")` …)으로 집는다 — 곧 `Assets/Tests` 의 자와 똑같이 «옛 이름을 박아 둔 채» 낡는다.
#   그런데 이 자가 `Assets/Tests` 만 훑어서, 2026-09-10 에 화면이 바뀌었을 때
#   같은 고침이 자 쪽만 옮겨지고 각본은 그대로 남았다(그 각본은 CI 가 아니라 **배포 스모크**에서만 도는데,
#   꺼진 버튼도 `onClick` 은 돌아서 **빨개지지도 않고 조용히 아무것도 안 누른다**).
#   ⇒ 훑는 자리에 그 한 파일을 더한다. 「자」의 뜻은 폴더가 아니라 «이름 계약에 기대어 화면을 집는가» 다.
BOT = "Assets/Scripts/Game/Playthrough.cs"
SEARCH = (TESTS, BOT)

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
    """그 범위가 `Assets/Scripts/` 에서 무엇을 바꿨나 → (diff 글, 못 읽었으면 그 까닭).

    ⛑ **T507** — 전에는 `.stdout` 만 돌려줬다. 그러면 git 이 «unknown revision» 으로 죽어도 stdout 이 비고,
    그 빈 것이 **«지운 것이 없다» 와 같은 길**로 들어가 `✓ … 바뀐 것이 없다` 로 나갔다(rc=0).
    곧 **오타 난 sha 의 답과 정직한 «깨끗하다» 가 한 글자도 다르지 않았다.**
    <br>⚑ 이 자는 **ROUTINE §1 이 «빨강을 진단하기 전에 그 범위로 한 번 돌려라» 로 세워 둔 자**다(T484).
    그러니 sha 로 이 자를 돌리는 사람은 거의 다 **빨강을 쫓는 중**이고 sha 는 손으로 옮겨 적는다 —
    한 글자 틀리면 «그 커밋은 옛 단언을 안 남겼다» 로 읽고 **딴 데를 뒤진다**. 자는 아무것도 안 봤는데.
    <br>**T482** 가 `ci_test_failures` 에서 이미 가른 병이다(«자리는 있는데 비었다» ↔ «그런 자리가 아예 없다» ·
    그 앞에 워커 H 가 런 번호를 넘겨 자가 라이선스를 가리킨 실사고가 있었다). 같은 꼴로 가른다.
    """
    cmd = ["git", "diff", "-U0"] + ([rev] if rev else []) + ["--", SRC]
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode != 0:
        why = " / ".join(l.strip() for l in r.stderr.split("\n") if l.strip())[:300]
        return "", (why or "git 이 %d 로 끝났는데 아무 말도 안 남겼다" % r.returncode)
    return r.stdout, None


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
    """<at> 트리(없으면 작업 트리)의 Assets/Tests **와 봇 각본**(<see cref="BOT"/>)에서 그 값을 쓰는 줄 — git grep 이라 무시 파일을 안 판다.

    ⚠ <at> 이 중요하다: 과거 커밋을 검증할 때 작업 «트리» 를 뒤지면 이미 고쳐진 뒤라 늘 0 이 나온다
    (이 자를 만들며 T179 로 실제로 겪었다 · 결정 참조)."""
    pat = ('"%s"' % needle) if literal == "str" else (r'\b%s\b' % re.escape(needle) if literal == "name"
                                                     else r'(?<![\w.])%s(?![\w.])' % re.escape(needle))
    cmd = ["git", "grep", "-n", "--fixed-strings" if literal == "str" else "-P", pat]
    if at:
        cmd.append(at)
    cmd += ["--"] + list(SEARCH)
    r = subprocess.run(cmd, capture_output=True, text=True)
    return [l for l in r.stdout.split("\n") if l.strip()][:MAX_HITS_TEXT + 1]


def searched_count(at=None):
    """`SEARCH`(자 폴더 + 봇 각본) 아래 파일이 그 트리에 **몇 개 있나** — 0 이면 이 자는 «찾은» 것이 아니다.

    ⛑ **T507 2회차 — 같은 병이 «옆문» 에 그대로 있었다**(워커 J 가 1회차 뒤에 짚었다 · 결정 1406 ①).
    1회차는 `git diff` 가 죽은 것을 «깨끗하다» 와 갈랐다. 그런데 **`git grep` 쪽은 그대로였다**:
    `git grep` 은 **찾을 경로가 없으면 rc 1 · stdout 빈손 · stderr 도 빈손**으로 돌아오고(실측),
    그 빈손은 «안 걸렸다» 와 **한 글자도 다르지 않다**. 자 폴더가 없는 트리에서 실제로
    `✓ … 가리키는 자리 0 … (문자열 1 · 이름 0 · 수 0 를 훑었다)` · rc 0 이 나왔다.
    <br>⚠ **그리고 1회차가 보탠 «훑은 수» 가 그 답을 더 믿음직해 보이게 만들었다** —
    그 수는 **diff 쪽**을 증명하지 **grep 쪽**을 증명하지 않는다. 그래서 이 수를 같이 찍는다:
    «문자열 N · 이름 N · 수 N 를 훑어 **자 파일 M개**에서 찾았다». `M == 0` 이면 딴 줄로 «못 쟀다» 다.
    """
    if at:
        cmd = ["git", "ls-tree", "-r", "--name-only", at, "--"] + list(SEARCH)
    else:
        cmd = ["git", "ls-files", "--"] + list(SEARCH)
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode != 0:
        return -1                                  # 못 셌다 — 아래에서 «못 쟀다» 로 나간다
    return len([l for l in r.stdout.split("\n") if l.strip()])


def grep_tree(rev):
    """어느 트리에서 테스트를 찾을 것인가 — 「A..B」 면 B, 「HEAD~1」 처럼 한쪽만 주면 작업 트리(None)."""
    if rev and ".." in rev:
        right = rev.split("..")[-1].strip()
        return right or "HEAD"
    return None


def main(argv):
    strict = "--strict" in argv
    rev = next((a for a in argv if not a.startswith("--")), None)
    d, broke = diff(rev)
    at = grep_tree(rev)
    if broke:
        # ⛑ T507 — **«못 봤다» 는 «깨끗하다» 와 딴 줄이어야 한다.** 여기서 `✓ … 바뀐 것이 없다` 로 나가면
        #   빨강을 쫓는 사람이 «이 커밋은 옛 단언을 안 남겼다» 로 읽고 딴 데를 뒤진다(T482 와 같은 병).
        #   그래서 ⓐ 표식을 ✓ 가 아닌 것으로 ⓑ git 이 한 말을 그대로 ⓒ 이 자가 받는 꼴까지 적는다.
        print("⚠ check_stale_asserts: 그 범위를 **못 읽었다** — 그래서 이 답은 «깨끗하다» 가 **아니라** «모른다» 다: %s" % broke)
        print("   받는 꼴: `HEAD~1` 처럼 한쪽만 · `<sha>~1..<sha>` 처럼 범위 · 아무것도 안 주면 작업 트리."
              " (sha 를 손으로 옮겨 적었으면 한 글자 틀렸는지 먼저 보라 — 이 자를 sha 로 돌리는 사람은 거의 다 빨강을 쫓는 중이다)")
        return 0                                  # 판정은 안 바꾼다(알리는 자 · 결정 493 · T482 가 놓은 꼴 그대로)
    if not d.strip():
        print("✓ check_stale_asserts: %s 에 바뀐 것이 없다" % SRC)
        return 0
    strs, nums, names = tokens(removed_lines(d), added_text(d))
    # ⛑ T507 2회차 — **찾을 자리가 있었나**(위 `searched_count` 의 까닭 · 워커 J 의 보탬 ①).
    #   이 물음은 위 «지운 값이 있었나»(diff 쪽)와 **딴 물음**이다. 둘 중 하나만 답해 놓고
    #   «자리 0» 이라 말하면, 그 «0» 은 절반만 증명된 0 이다.
    seen = searched_count(at)
    if seen <= 0:
        where = " · ".join(SEARCH)
        print("⚠ check_stale_asserts: **찾을 자리를 못 봤다** — `%s` 아래 파일이 %s. "
              "그래서 이 답은 «안 걸렸다» 가 **아니라** «모른다» 다(`git grep` 은 경로가 없어도 rc 1 · 빈손으로 돌아온다)."
              % (where, "0개다" if seen == 0 else "몇 개인지 못 셌다"))
        print("   지운 값은 뽑았다(문자열 %d · 이름 %d · 수 %d) — **그 수는 diff 쪽만 증명한다.** "
              "과거 트리를 볼 때(`A..B`)는 그 트리에 자 폴더가 있는지부터 보라." % (len(strs), len(names), len(nums)))
        return 0                                  # 판정은 안 바꾼다(알리는 자 · 결정 493)

    found = []
    for group, kind, label in ((strs, "str", "문자열"), (names, "name", "이름"), (nums, "num", "수")):
        for v in sorted(group):
            h = hits(v, kind, at)
            cap = MAX_HITS_NUM if kind == "num" else MAX_HITS_TEXT
            if h and len(h) <= cap:
                found.append((label, v, h))

    if not found:
        # ⛑ T507 2회차 — «자 파일 M개» 를 같이 찍는다. 그것이 **grep 쪽** 증인이다(위 주석).
        print("✓ check_stale_asserts: 지운 값·이름을 아직 가리키는 자리 0 (자 + 봇 각본) "
              "(문자열 %d · 이름 %d · 수 %d 를 훑어 자 파일 %d개에서 찾았다)"
              % (len(strs), len(names), len(nums), seen))
        return 0

    print("⚠ 이 diff 가 지운 값·이름을 «아직» 가리키는 테스트 자리 %d 건 — 옛 전제를 든 단언인지 하나씩 본다"
          "(결정 425·426·427·429 가 네 번 놓친 그 종류다):" % len(found))
    for label, v, h in found:
        print("  · 지운 %s «%s»" % (label, v))
        for line in h:
            print("      %s" % line[:180])
    print("고칠 것이면 같은 커밋에서 고치고, 우연히 같은 글자면 그냥 지나간다(이 자는 찾아 줄 뿐 판정하지 않는다).")
    # T281 — 마지막 줄은 판정이다(꼬리 한 줄로 읽는 워커가 빨강을 초록으로 읽지 않게).
    #   ⚑ 이 자는 «판정하지 않는» 자라 표시도 그렇게 읽히게 쓴다 — «걸린 자리 N건(사람이 본다)».
    print("✗ check_stale_asserts: 지운 값을 아직 가리키는 자리 %d건 — 우연히 같은 글자일 수 있다(사람이 하나씩 본다%s)"
          % (len(found), " · --strict 라 rc=1" if strict else " · rc=0"))
    return 1 if strict else 0


def self_test():
    """⛑ **T507** — 이 자에는 자기 검사가 **아예 없었다**(T492 가 «자기 검사 없는 자 다섯» 으로 세어 두고,
    T505 가 하나 · T506 이 셋을 집으며 이 자를 «다음 손 몫» 으로 남긴 그 자리다).

    ⚑ **제 레포 이력에 기대지 않는다 — 임시 git 레포를 세워 거기서 돌린다.**
    `62bdebb0` 같은 진짜 표본으로 갈래를 세우면 `ci.yml` 의 `fetch-depth`(지금 300)가 언젠가 그 커밋을 못 담는 날
    이 자가 **제 잘못이 아닌 까닭으로** 빨개진다 — 그러면 다음 사람이 갈래를 지운다. 그 표본은 사람이 손으로 보는 몫이고,
    갈래는 **이 자가 통째로 들고 있는 것**(diff 읽기 · 판정 · «못 읽었다» 가름)만 묻는다.

    ⚑⚑ 갈래의 **핵심은 ⓐ↔ⓑ 한 쌍**이다 — «못 읽었다» 와 «바뀐 것이 없다» 가 **딴 줄인가**.
    그 둘이 한 줄이던 것이 이 절이 고친 병이고, 한쪽만 묻는 갈래는 그 병을 다시 못 잡는다.
    """
    import os
    import tempfile
    ok = True
    here = os.getcwd()
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    tool = os.path.join(root, "tools", "check_stale_asserts.py")

    def run(cwd, *args):
        r = subprocess.run([sys.executable, tool] + list(args), capture_output=True, text=True, cwd=cwd)
        return r.stdout + r.stderr, r.returncode

    def git(cwd, *args):
        return subprocess.run(["git", "-c", "user.name=t", "-c", "user.email=t@t",
                               "-c", "commit.gpgsign=false"] + list(args),
                              capture_output=True, text=True, cwd=cwd)

    # ⛑ **이름표와 단언을 하나로 묶는다** — T507 을 세우며 일부러 부러뜨려 보니(git 이 한 말을 감추기 ·
    #   판정 rc 를 1 로 바꾸기) **갈래는 실패하는데 그 줄은 «OK» 를 찍었다**. 단언 일부만 이름표에 옮겨
    #   적어 뒀기 때문이다. 그러면 다음 사람이 «어느 갈래가 물었나» 를 이름표로 읽고 **틀린 자리를 뒤진다**.
    #   내가 T503 에서 적은 것과 같은 꼴이다 — 자에게 «무엇을 묻는지» 를 두 곳에 적으면 두 곳이 갈린다.
    branch = []

    def check(label, **conds):
        bad = [k for k, v in conds.items() if not v]
        branch.append((label, bad))
        print("%s —" % label, "OK" if not bad else "실패: " + " · ".join(bad))
        return not bad

    with tempfile.TemporaryDirectory() as d:
        os.makedirs(os.path.join(d, "Assets", "Scripts", "Game"))
        os.makedirs(os.path.join(d, "Assets", "Tests", "PlayMode"))
        src = os.path.join(d, "Assets", "Scripts", "Game", "Foo.cs")
        tst = os.path.join(d, "Assets", "Tests", "PlayMode", "FooTests.cs")
        # 자 쪽은 두 커밋 내내 그대로다 — «옛 이름을 박아 둔 채 남은 자» 그것이다.
        open(tst, "w", encoding="utf-8").write(
            'class FooTests { void A() { Find("ZzyzxPanel"); Assert(4321f); } }\n')
        open(src, "w", encoding="utf-8").write(
            'class Foo { const string N = "ZzyzxPanel"; const float K = 4321f; }\n')
        git(d, "init", "-q", "-b", "main")
        git(d, "add", "-A")
        git(d, "commit", "-qm", "a")
        open(os.path.join(d, "docs_only.md"), "w", encoding="utf-8").write("x\n")
        git(d, "add", "-A")
        git(d, "commit", "-qm", "docs only")          # Assets/Scripts 를 안 건드린 커밋
        open(src, "w", encoding="utf-8").write('class Foo { }\n')   # 둘 다 지운다
        git(d, "add", "-A")
        git(d, "commit", "-qm", "remove")
        head = git(d, "rev-parse", "HEAD").stdout.strip()

        # ⓐ 못 읽은 리비전 — «깨끗하다» 가 아니라 «모른다» 로 나가는가
        out, rc = run(d, "deadbeefdeadbeef~1..deadbeefdeadbeef")
        ok &= check("ⓐ 못 읽은 리비전 = «모른다»",
                    **{"«못 읽었다»·«모른다» 로 말한다": "못 읽었다" in out and "모른다" in out,
                       "«바뀐 것이 없다» 로는 안 나간다(이 절의 고침 그 자체)": "바뀐 것이 없다" not in out,
                       "git 이 한 말을 그대로 보여 준다": "bad revision" in out,
                       "판정 rc 는 안 바뀐다(알리는 자 · 결정 493)": rc == 0})

        # ⓑ ⓐ 의 짝 — 정직한 «바뀐 것이 없다» 는 **그대로 ✓** 이고 «못 읽었다» 가 아니다
        out, rc = run(d, "HEAD~2..HEAD~1")
        ok &= check("ⓑ 안 바뀐 범위 = «깨끗하다»",
                    **{"«바뀐 것이 없다» 로 말한다": "바뀐 것이 없다" in out,
                       "«못 읽었다» 와 섞이지 않는다(ⓐ 의 짝)": "못 읽었다" not in out,
                       "✓ 로 시작한다": out.lstrip().startswith("✓")})

        # ⓒ 적중 — 지운 문자열·수가 자 쪽에 남은 것을 이름과 줄로 집는가
        out, rc = run(d, "HEAD~1..HEAD")
        ok &= check("ⓒ 적중(문자열·수 · 파일·줄)",
                    **{"지운 문자열을 집는다": "ZzyzxPanel" in out,
                       "지운 수를 집는다": "4321f" in out,
                       "어느 파일인지까지 말한다": "FooTests.cs" in out,
                       "둘을 2건으로 센다": "2건" in out,
                       "마지막 줄이 판정이다(T281)": out.rstrip().split("\n")[-1].startswith("✗")})

        # ⓓ 작업 트리 갈래 — 인자 없이도 돌고, 그때도 두 줄이 안 섞인다
        open(src, "w", encoding="utf-8").write('class Foo { const string M = "ZzyzxPanel"; }\n')
        git(d, "add", "-A")
        git(d, "commit", "-qm", "back")
        open(src, "w", encoding="utf-8").write('class Foo { }\n')   # 커밋 안 한 채로 지운다
        out, rc = run(d)
        ok &= check("ⓓ 작업 트리(인자 없음)",
                    **{"커밋 안 한 지움도 집는다": "ZzyzxPanel" in out,
                       "«못 읽었다» 와 섞이지 않는다": "못 읽었다" not in out})

        # ⓔ --strict — 걸린 것이 있으면 rc=1(기본은 0)
        out0, rc0 = run(d)
        out1, rc1 = run(d, "--strict")
        ok &= check("ⓔ --strict 가 rc 를 가른다",
                    **{"기본은 rc=0 (찍기만 한다)": rc0 == 0,
                       "--strict 면 rc=1": rc1 == 1})

        # ⓕ 못 읽은 리비전에 --strict 를 줘도 «못 읽었다» 는 판정을 사칭하지 않는다
        out, rc = run(d, "--strict", "nosuchrevzz")
        ok &= check("ⓕ --strict + 못 읽음",
                    **{"«못 읽었다» 로 말한다": "못 읽었다" in out,
                       "판정을 사칭하지 않는다": "지운 값" not in out})

        # ⓖ ⛑ **봇 각본도 훑는 자리다**(결정 1072 · `Assets/Scripts/Game/Playthrough.cs`).
        #   그 한 파일은 «자» 가 아니라 소스 폴더에 사는데, 화면을 **이름 계약**으로 집으므로 똑같이 낡는다 —
        #   2026-09-10 에 고침이 자 쪽만 옮겨지고 각본이 그대로 남은 실사고 뒤에 더한 자리다.
        #   ⚑ 이 갈래를 세운 까닭: T507 을 세우며 `SEARCH` 에서 `BOT` 을 **빼 봤더니 여섯 갈래가 전부 통과했다**.
        #      실사고 뒤에 일부러 더한 자리를 **아무 자도 안 지키고 있었다**.
        bot = os.path.join(d, "Assets", "Scripts", "Game", "Playthrough.cs")
        open(bot, "w", encoding="utf-8").write('class Playthrough { void Run() { Find("QwertyNode"); } }\n')
        open(src, "w", encoding="utf-8").write('class Foo { const string P = "QwertyNode"; }\n')
        git(d, "add", "-A")
        git(d, "commit", "-qm", "bot")
        open(src, "w", encoding="utf-8").write('class Foo { }\n')   # 소스에서만 지운다 — 각본은 그대로
        git(d, "add", "-A")
        git(d, "commit", "-qm", "drop")
        out, rc = run(d, "HEAD~1..HEAD")
        ok &= check("ⓖ 봇 각본도 훑는다(결정 1072)",
                    **{"각본에 남은 옛 이름을 집는다": "QwertyNode" in out,
                       "그 파일 이름을 말한다": "Playthrough.cs" in out})

        # ⓗ **공허 방지** — «아무것도 안 읽었다» 를 «0건» 과 가른다(워커 J 가 T508 등재 글에 적어 둔 ① · 결정 1379).
        #   이 자의 «0건» 줄은 **훑은 수**를 같이 찍는다(«문자열 N · 이름 N · 수 N»). 그 수가 없으면
        #   «지운 값이 없다» 와 «지운 값을 못 뽑았다»(정규식이 고장 났다·길이 어긋났다)가 **한 꼴**이 된다.
        open(src, "w", encoding="utf-8").write('class Foo { const string R = "MmnopqWidget"; }\n')
        git(d, "add", "-A")
        git(d, "commit", "-qm", "add lonely")
        open(src, "w", encoding="utf-8").write('class Foo { }\n')   # 자 쪽엔 없는 값을 지운다 → 걸릴 것이 0
        git(d, "add", "-A")
        git(d, "commit", "-qm", "drop lonely")
        out, rc = run(d, "HEAD~1..HEAD")
        ok &= check("ⓗ «0건» 이 증인 **둘**을 같이 말한다(공허 방지)",
                    **{"0건으로 말한다": "자리 0" in out,
                       "diff 쪽 증인 — 뽑은 수를 찍는다": "를 훑어" in out,
                       "그 수가 0 이 아니다(= 뽑기는 했다)": "문자열 0 · 이름 0 · 수 0" not in out,
                       # ⛑ T507 2회차(워커 J ①) — 위 수는 **diff 쪽만** 증명한다. grep 쪽 증인이 따로 있어야 한다.
                       "grep 쪽 증인 — 찾은 자 파일 수를 찍는다": "자 파일" in out and "개에서 찾았다" in out,
                       "그 파일 수가 0 이 아니다": "자 파일 0개" not in out})

        # ⓘ ⛑ **한계를 못 박는다** — 워커 J 의 ②(«이 자만의 조심 · 오탐이 나는 꼴을 그대로 적어 둔다»).
        #   이 자는 «우연히 같은 글자» 에 잦게 걸린다(문서가 적은 `62bdebb0` 표본이 1적중 1소음이다).
        #   그래서 두 한계를 **갈래로 박아 둔다** — 다음 사람이 «소음을 줄이자» 며 상한을 건드릴 때 여기서 걸린다:
        #     ⓐ 여러 자리에 걸리는 수는 **아예 안 찍는다**(MAX_HITS_NUM) — 그것은 «그 자리 값» 이 아니라 흔한 상수다
        #     ⓑ 한 자리에만 걸리면 **찍는다**(우연이어도) — 이 자는 찾아 줄 뿐 판정하지 않는다
        for i in range(MAX_HITS_NUM + 2):
            open(os.path.join(d, "Assets", "Tests", "PlayMode", "N%d.cs" % i), "w", encoding="utf-8").write(
                "class N%d { void A() { Use(7654f); } }\n" % i)
        open(os.path.join(d, "Assets", "Tests", "PlayMode", "One.cs"), "w", encoding="utf-8").write(
            "class One { void A() { Use(8765f); } }\n")
        open(src, "w", encoding="utf-8").write('class Foo { const float A = 7654f, B = 8765f; }\n')
        git(d, "add", "-A")
        git(d, "commit", "-qm", "common+rare")
        open(src, "w", encoding="utf-8").write('class Foo { }\n')
        git(d, "add", "-A")
        git(d, "commit", "-qm", "drop both")
        out, rc = run(d, "HEAD~1..HEAD")
        ok &= check("ⓘ 한계를 못 박는다(흔한 수는 버리고 한 자리는 찍는다)",
                    **{"%d자리 넘게 걸리는 수는 안 찍는다(MAX_HITS_NUM=%d)" % (MAX_HITS_NUM, MAX_HITS_NUM):
                           "7654f" not in out,
                       "한 자리에만 걸리면 찍는다(우연이어도)": "8765f" in out,
                       "«판정하지 않는다» 를 말한다": "판정하지 않는다" in out})

    # ⓙ ⛑ **옆문** — `git grep` 은 **찾을 경로가 없어도 rc 1 · stdout 빈손 · stderr 도 빈손**이다(실측).
    #    그러니 «자 폴더가 통째로 없는 트리» 를 봐도 «안 걸렸다» 와 한 글자도 안 달랐다.
    #    1회차가 `git diff` 쪽만 갈라 놓고 «고쳤다» 고 적었는데, 워커 J 가 그 회차 뒤에 이 옆문을 짚었다(결정 1406 ①).
    #    ⚠ 더 나쁜 것은 1회차가 보탠 «훑은 수» 가 그 절반짜리 0을 **더 믿음직해 보이게** 만든 것이다.
    with tempfile.TemporaryDirectory() as d2:
        os.makedirs(os.path.join(d2, "Assets", "Scripts", "Game"))
        s2 = os.path.join(d2, "Assets", "Scripts", "Game", "Foo.cs")
        open(s2, "w", encoding="utf-8").write('class Foo { const string N = "VvwxyzPanel"; }\n')
        git(d2, "init", "-q", "-b", "main")
        git(d2, "add", "-A")
        git(d2, "commit", "-qm", "a")
        open(s2, "w", encoding="utf-8").write('class Foo { }\n')
        git(d2, "add", "-A")
        git(d2, "commit", "-qm", "b")               # 자 폴더가 **한 번도 없던** 트리다
        out, rc = run(d2, "HEAD~1..HEAD")
        ok &= check("ⓙ 찾을 자리가 없으면 «모른다»(옆문 · git grep 은 빈손으로 돌아온다)",
                    **{"«찾을 자리를 못 봤다» 로 말한다": "찾을 자리를 못 봤다" in out,
                       "«모른다» 라고 못 박는다": "«모른다»" in out,
                       "«자리 0» 으로는 안 나간다(이 회차의 고침 그 자체)": "자리 0" not in out,
                       "diff 쪽은 봤다고 구별해 말한다": "diff 쪽만 증명한다" in out,
                       "판정 rc 는 안 바뀐다": rc == 0})

    # ⓚ ⛑ **이 자가 원리적으로 못 보는 것을 갈래로 굳힌다**(워커 J ② · 결정 1406 ②).
    #    머리글이 제 점수를 «1적중 · 1소음 · 3놓침» 으로 적어 뒀는데 ⓘ 가 굳힌 것은 «1소음» 뿐이었다.
    #    «3놓침» — **이름도 값도 그대로인데 «뜻» 만 바뀐** 자리(열 그라데이션·머리 배지·줄 번호) — 은 아직 **글**이라,
    #    자가 바뀌면 조용히 거짓이 된다. 그래서 «리터럴은 그대로 두고 뜻만 바꾼 diff → 0건» 을 박아 둔다.
    #    ⚑ 이 갈래는 **«이 자의 0건은 «옛 단언 없음» 이 아니다» 를 자와 함께 살게 한다** —
    #      누가 «이 자가 뜻 바뀜도 잡는다» 고 믿고 기대면, 그 믿음이 틀렸다는 것이 여기 적혀 있다.
    with tempfile.TemporaryDirectory() as d3:
        os.makedirs(os.path.join(d3, "Assets", "Scripts", "Game"))
        os.makedirs(os.path.join(d3, "Assets", "Tests", "PlayMode"))
        s3 = os.path.join(d3, "Assets", "Scripts", "Game", "Foo.cs")
        open(os.path.join(d3, "Assets", "Tests", "PlayMode", "MeaningTests.cs"), "w", encoding="utf-8").write(
            'class MeaningTests { void A() { Assert(Rows(), 3); } }\n')
        open(s3, "w", encoding="utf-8").write('class Foo { int Rows() { return 3; } }   // 줄 수\n')
        git(d3, "init", "-q", "-b", "main")
        git(d3, "add", "-A")
        git(d3, "commit", "-qm", "a")
        # 값도 이름도 그대로 두고 **뜻만** 바꾼다 — 3 이 «줄 수» 에서 «칸 수» 가 됐다
        open(s3, "w", encoding="utf-8").write('class Foo { int Rows() { return 3; } }   // 이제는 칸 수다\n')
        git(d3, "add", "-A")
        git(d3, "commit", "-qm", "meaning only")
        out, rc = run(d3, "HEAD~1..HEAD")
        ok &= check("ⓚ 뜻만 바뀐 자리는 **원리적으로 못 본다**(머리글의 «3놓침» 을 굳힌다)",
                    **{"0건으로 나간다": "자리 0" in out or "바뀐 것이 없다" in out,
                       "«모른다» 로 새지 않는다(찾을 자리는 있었다)": "찾을 자리를 못 봤다" not in out,
                       "판정 rc 는 0": rc == 0})

    os.chdir(here)
    dead = [l for l, b in branch if b]
    print("✓ check_stale_asserts 자기 검사 통과 (갈래 %d)" % len(branch) if ok
          else "✗ 자기 검사 실패 — 물린 갈래: " + " · ".join(dead))
    return 0 if ok else 1


if __name__ == "__main__":
    if "--self-test" in sys.argv[1:]:
        sys.exit(self_test())
    sys.exit(main(sys.argv[1:]))
