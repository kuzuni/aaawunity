#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""유니티 게이트가 «언제 마지막으로 답했는가» — 빚진 커밋이 쌓이는데 답이 안 오면 알린다 (T285 · 검수 Q).

무엇이 문제였나 (2026-09-09 01:18~ · T283)
  `UNITY_LICENSE` 가 거절되면서 유니티 잡이 **1~3분 만에 죽고 결과 XML·PNG 를 하나도 안 만드는** 상태가 됐다.
  런은 «완주» 하고 목록에는 남는데 **아무것도 안 만든다** — 그래서
    · `screens` 는 마지막 초록에 멈춰 있고(PNG 0장이면 배포를 건너뛴다)
    · «최근 런이 초록인가» 만 보는 눈에는 **안 보인다**
  실제로 이 사고는 **50분 뒤에야** 사람 눈에 띄었고, 그 사이 커밋 17개가 PlayMode 검증 없이 지나갔다.

무엇을 재는가
  `origin/screens:meta.json`(유니티 잡이 완주할 때마다 쓰는 한 줄)의 **나이**와 **그 뒤로 쌓인 빚**을 같이 본다.
    나이만 보면 안 된다 — 아무도 안 밀면 안 움직이는 것이 **정상**이라 조용한 새벽마다 헛것이 난다.
    그래서 «그 sha 뒤로 CI 를 부른 커밋이 몇인가» 를 같이 세고, **빚이 있는데 답이 오래 없을 때만** 운다.
    ⚑ «부른 커밋» 의 뜻은 2026-09-10 에 바뀌었다(T433) — 건너뛰기 표식이 **메시지 어디에도** 없고
      **문서 아닌 파일을 건드린** 커밋이다. 아래 `commit_calls_ci` 와 `ci.yml` 의 `gate.code` 가 한 쌍이다.

이 자는 막지 않는다 (늘 exit 0)
  게이트가 늦는 것은 **빌드 결함이 아니다**(결정 493 의 기준). 게다가 오늘처럼 원인이
  «주인만 고칠 수 있는 라이선스» 이면 막아 봐야 워커의 손을 묶을 뿐이다. 마지막 줄만 판정으로 남긴다(T281).

쓰는 법
    python3 tools/check_gate_age.py            # 기본: 빚 1개 이상 + 45분 넘으면 ✗
    python3 tools/check_gate_age.py --minutes 90
    python3 tools/check_gate_age.py --no-fetch  # 이미 받아 둔 자리(CI·오프라인)에서 당기지 않고 잰다
    python3 tools/check_gate_age.py --self-test
  당기는 것은 **자가 스스로** 한다(T286) — 못 당기면 «내 ref 로 잰 값» 이라고 첫 줄에 적는다.
"""
import datetime
import json
import subprocess
import sys

DEFAULT_MINUTES = 45   # 유니티 잡 ~9분 + CI 를 부르는 커밋이 ~5분마다 → 성한 날은 이 선을 안 넘는다(실측)


def sh(*a):
    return subprocess.run(a, capture_output=True, text=True).stdout.strip()


def refresh():
    """
    재기 «전에» 자가 스스로 `origin/screens`·`origin/main` 을 당겨 온다 (T286).

    안 당기면 **이 세션이 시작할 때 받은 ref 로 잰다** — 워커 세션은 `screens` 를 따로 안 당기므로
    그것이 곧 «며칠 전 것으로 잰다» 이고, 자는 그 사실을 모른 채 «마지막 답이 2343분 전 · 빚 306개» 를 찍는다(실측 · 2026-09-09 04:3X).
    틀린 방향이 나쁘다: 괜찮은데 우는 것이 아니라 **세 시간짜리 사고를 이틀짜리로 키워 보여 준다**.

    돌려주는 값은 «지금 갔다 왔는가» — 못 갔으면 부르는 쪽이 그 사실을 <b>출력에 적는다</b>(조용히 낡은 수를 내놓지 않는다 · 결정 690).

    ⚠ <b>`--depth` 를 쓰지 않는다</b> — 이 회차에 `--depth 1` 로 당겼다가 <b>워커의 온전한 클론이 shallow 로 바뀌어</b>
    바로 아래 <see cref="debt"/> 의 «빚 세기» 가 1 로 주저앉았다(그리고 이력을 읽는 다른 자들도 같이 눈이 먼다).
    빠르자고 붙인 한 낱말이 «이 자가 재려던 바로 그 수» 를 망가뜨렸다 — 자기 검사에 그 갈래를 넣어 뒀다.
    """
    r = subprocess.run(["git", "fetch", "--quiet", "origin", "screens", "main"], capture_output=True, text=True)
    return r.returncode == 0


def read_meta():
    """screens 의 meta.json — 없으면 로컬 ui-screens/meta.json."""
    out = sh("git", "show", "origin/screens:meta.json")
    if not out:
        try:
            out = open("ui-screens/meta.json", encoding="utf-8").read()
        except OSError:
            return None
    try:
        return json.loads(out)
    except ValueError:
        return None


def is_doc_path(p):
    """이 파일 하나가 «문서» 인가 — `ci.yml` 의 `gate.code` 단계와 **같은 잣대**여야 한다(T433).

    두 자리가 갈리면 이 자가 «빚 3개» 라 우는데 CI 는 애초에 돌 생각이 없는 꼴이 난다.
    바꿀 일이 생기면 **두 곳을 같이** 바꾼다(`.github/workflows/ci.yml` 의 `- id: code`).
    """
    return p.startswith("docs/") or ("/" not in p and p.endswith(".md"))


# GitHub 이 «이 push 는 건너뛴다» 로 읽는 표식 전부 — **제목이 아니라 머리 커밋의 «메시지 전체»** 를 본다.
#   ⚑⚑ 2026-09-11 00:0X 실측(T433 2회차 · 워커 G) — 내가 이 자를 세운 **그 커밋 자신이 CI 를 안 돌았다**.
#   제목에는 없었고 **본문에 그 규약을 «인용»** 했다(«… 규약은 그대로 둔다 — 그것은 dotnet 잡까지 아낀다»).
#   GitHub 은 인용과 지시를 안 가린다. 그래서 ⓐ 커밋 메시지에 이 표식을 **쓰는 것과 말하는 것이 같은 일**이고,
#   ⓑ 제목만 보던 이 자는 그런 커밋을 «CI 를 부른다» 로 세어 **없는 빚을 셌다**.
SKIP_TOKENS = ("[skip ci]", "[ci skip]", "[no ci]", "[skip actions]", "[actions skip]")


def has_skip_token(message):
    """그 메시지가 CI 를 건너뛰라고 말하는가 — **제목 말고 메시지 전체**를 본다(위 주석의 실측)."""
    low = (message or "").lower()
    return any(tok in low for tok in SKIP_TOKENS)


def commit_calls_ci(message, files):
    """그 커밋이 **유니티 잡을 부르는가** (T433).

    둘 다 있어야 부른다 — ⓐ **메시지 전체**에 건너뛰기 표식이 없고 ⓑ 문서 아닌 파일을 하나라도 건드렸다.
    ⓑ 가 T433 으로 새로 생겼다: 2026-09-10 부터 `ci.yml` 의 `unity-test` 가 «코드 0줄인 push» 를 건너뛴다.
    ⚠ **파일 목록을 못 읽으면 «부른다» 로 본다** — 자와 CI 가 같은 방향(fail-safe)으로 틀리게 둔다.
    """
    if has_skip_token(message):
        return False
    if files is None:
        return True
    if not files:
        return True
    return not all(is_doc_path(f) for f in files)


def debt(sha):
    """그 sha 뒤로 main 에 쌓인 «유니티 잡을 부르는» 커밋 수.

    ⚑ T433 이전에는 «`[skip ci]` 아닌 것» 하나로 셌다. 이제는 **문서만 바꾼 커밋도 안 부른다** —
      안 맞추면 문서만 오가는 조용한 회차마다 이 자가 헛 경보를 낸다(고침을 넣은 회차에 같이 맞췄다).
    """
    # ⚑ 커밋마다 `git show` 를 부르지 않는다 — 낡은 `meta.json` 은 빚이 300개까지 간다(T286 실측).
    #   `git log --name-only` 한 번으로 «메시지 전체 + 그 커밋이 건드린 파일» 을 같이 받는다.
    #   `%x01` 이 커밋 경계 · `%x02` 가 «메시지 끝» 이다(둘 다 파일 이름에는 못 들어가는 글자).
    #   ⚑ `%s`(제목)가 아니라 `%B`(전체)인 까닭은 위 `SKIP_TOKENS` 주석에 적힌 실측이다.
    # `--diff-merges=cc` — 머지 커밋도 «충돌을 풀며 손댄 파일» 을 내놓는다(안 주면 머지는 늘 «빚» 으로 센다).
    #   ⚠ 옛 git 에 그 옵션이 없으면 **빈 답**이 오는데, 그것을 «빚 0» 으로 읽으면 이 자가 눈이 먼다 —
    #     빈 답이면 옵션 없이 한 번 더 물어본다(그 판에선 머지가 빚으로 세어지지만, 그쪽이 안전하다).
    rng = f"{sha}..origin/main"
    fmt = "\x01%B\x02"
    log = sh("git", "log", "--name-only", "--diff-merges=cc", "--format=" + fmt, rng)
    if not log:
        log = sh("git", "log", "--name-only", "--format=" + fmt, rng)
    if not log:
        return 0, 0, []
    total = calling = 0
    silent = []
    for chunk in log.split("\x01"):
        if not chunk.strip():
            continue
        message, _, rest = chunk.partition("\x02")
        files = [r for r in rest.split("\n") if r.strip()]
        total += 1
        if commit_calls_ci(message, files):
            calling += 1
        elif has_skip_token(message) and files and not all(is_doc_path(f) for f in files):
            # ⚑ 코드를 건드렸는데 표식 때문에 안 돈 커밋 — **구멍**이다(T433 2회차).
            #   대개 «본문에 그 규약을 인용» 한 사고다. 실측 2건 중 하나는 본문이
            #   «이 커밋은 … 없이 민다» 였다 — 그 문장 자신이 CI 를 껐다.
            silent.append(message.split("\n")[0][:70])
    return total, calling, silent


def main():
    args = sys.argv[1:]
    if "--self-test" in args:
        return self_test()
    limit = DEFAULT_MINUTES
    if "--minutes" in args:
        limit = int(args[args.index("--minutes") + 1])

    # T286 — 먼저 당기고 잰다. 못 당기면 그 사실이 첫 줄에 뜬다(«내 ref 로 잰 값» 이라는 말이 없으면 낡은 수가 사고처럼 보인다).
    fresh = False if "--no-fetch" in args else refresh()
    if not fresh:
        why = "--no-fetch 로 껐다" if "--no-fetch" in args else "`git fetch` 가 안 됐다(네트워크·권한)"
        print(f"⚠ 지금 갔다 오지 못했다({why}) — 아래는 **내 ref 로 잰 값**이라 실제보다 낡았을 수 있다(T286).")

    m = read_meta()
    if m is None:
        print("· screens 의 meta.json 을 못 읽었다 — `git fetch origin screens` 를 먼저 하거나, 아직 첫 배포 전이다.")
        print("✓ check_gate_age: 잴 것이 없다(판정 보류 · 이 자는 막지 않는다)")
        return 0

    when = datetime.datetime.strptime(m["utc"], "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=datetime.timezone.utc)
    age = int((datetime.datetime.now(datetime.timezone.utc) - when).total_seconds() // 60)
    total, calling, silent = debt(m["sha"])
    verdict = m.get("tests", "?")

    print(f"· 마지막 완주 유니티 잡 = 런 {m['run']} ({m['sha'][:9]} · {m['utc']} · {age}분 전) "
          f"· tests={verdict} · PNG {m.get('shots', '?')}장")
    print(f"· 그 뒤 main 커밋 {total}개(그 중 CI 를 부르는 것 {calling}개)")

    bad = []
    if calling > 0 and age > limit:
        bad.append(f"빚 {calling}개가 {age}분째 답을 못 받았다(한계 {limit}분)")
    if verdict != "success":
        bad.append(f"마지막 답이 «{verdict}» 다")
    if m.get("shots", 1) == 0:
        bad.append("그 런이 PNG 를 0장 만들었다(테스트가 시작조차 못 했을 수 있다)")
    if silent:
        # T433 2회차 — «빚» 과 다른 종류의 구멍이다. 빚은 «답을 기다리는 중» 이지만
        #   이것은 **아무도 안 기다리는 커밋**이다(런이 아예 안 섰다).
        for s in silent:
            print(f"· ⚠ 코드를 건드렸는데 건너뛰기 표식 탓에 런이 **안 섰다**: {s}")
        bad.append(f"CI 가 아예 안 돈 코드 커밋 {len(silent)}개 — 메시지 **본문**의 표식을 보라"
                   f"(인용해도 GitHub 은 지시로 읽는다 · 위 «⚠» 줄)")

    stale = "" if fresh else " · ⚠ 낡은 ref 로 잰 값이다(T286 — 위 첫 줄)"
    if bad:
        print("✗ check_gate_age: " + " · ".join(bad) +
              " — 유니티 잡 로그의 **머리**를 보라(라이선스·러너는 꼬리에 안 나온다 · T283/T284) · 보고만(막지 않는다)" + stale)
    else:
        print(f"✓ check_gate_age: 마지막 답이 {age}분 전 · 빚 {calling}개 · tests=success (보고만 · T285){stale}")
    return 0


def self_test():
    """판정 갈래가 실제로 갈리는가 — 값을 손으로 넣어 두 갈래를 다 내 본다."""
    def verdict(calling, age, tests, shots, limit=DEFAULT_MINUTES, silent=0):
        bad = []
        if calling > 0 and age > limit:
            bad.append("빚")
        if tests != "success":
            bad.append("답")
        if shots == 0:
            bad.append("PNG")
        if silent:
            bad.append("구멍")
        return "✗" if bad else "✓"

    cases = [
        # (빚, 나이, tests, PNG, 기대) — 이름이 곧 그 갈래의 뜻이다
        (0, 600, "success", 41, "✓", "아무도 안 밀면 열 시간이 지나도 정상(조용한 새벽)"),
        (17, 115, "success", 41, "✗", "빚이 쌓였는데 답이 없다 = 2026-09-09 그 사고"),
        (3, 10, "success", 41, "✓", "빚이 있어도 방금 답했으면 정상"),
        (0, 10, "failure", 41, "✗", "빚이 없어도 마지막 답이 빨강이면 운다"),
        (1, 10, "success", 0, "✗", "완주했는데 PNG 0장 = 테스트가 시작도 못 했다"),
    ]
    # T433 2회차 — «빚» 과 다른 종류의 구멍: 런이 **아예 안 선** 코드 커밋.
    holes = [
        (0, 5, "success", 41, 1, "✗", "다 조용하고 답도 방금 왔는데, 런이 안 선 코드 커밋이 하나 있다"),
        (0, 5, "success", 41, 0, "✓", "그 구멍이 0이면 조용한 것이 정말 조용한 것이다"),
    ]
    bad = 0
    # T286 — 갈래 표 밖의 덫 하나를 같이 지킨다: refresh 가 «--depth» 로 당기면
    #   워커의 온전한 클론이 shallow 가 되고, 그러면 이 자가 재려던 «빚» 이 1 로 주저앉는다(2026-09-09 실측).
    import inspect
    body = inspect.getsource(refresh).replace(refresh.__doc__ or "", "")   # 설명글에는 그 낱말이 «하지 마라» 로 들어 있다
    deep = "--depth" not in body
    bad += 0 if deep else 1
    print(f"  {'✔' if deep else '✘'} refresh 가 --depth 없이 당긴다 — shallow 로 바뀌면 debt() 가 눈이 먼다(T286)")
    for calling, age, tests, shots, want, why in cases:
        got = verdict(calling, age, tests, shots)
        ok = got == want
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} {got} (기대 {want}) — {why}")

    for calling, age, tests, shots, silent, want, why in holes:
        got = verdict(calling, age, tests, shots, silent=silent)
        ok = got == want
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} {got} (기대 {want}) — {why}")

    # T433 — «무엇이 유니티 잡을 부르는가» 도 같이 잰다. 이 잣대가 `ci.yml` 의 `gate.code` 와 갈리면
    #   자는 «빚 3개» 라 우는데 CI 는 애초에 돌 생각이 없는 꼴이 난다(그래서 갈래를 여기 박아 둔다).
    calls = [
        ("docs 만 바꾼 커밋", "T430 ✅ 닫음", ["docs/PROGRESS.md", "docs/ROUTINE.md"], False),
        ("lock 만 잡은 커밋", "T433 선점", ["docs/claims/T433.lock"], False),
        ("뿌리 README.md 만", "README 손질", ["README.md"], False),
        ("코드가 한 줄이라도 있으면", "T433 고침", ["docs/PROGRESS.md", "tools/check_gate_age.py"], True),
        ("에셋", "T384 컨페티", ["Assets/Scripts/Game/ArenaResult.cs"], True),
        ("워크플로 자신", "ci.yml", [".github/workflows/ci.yml"], True),
        ("이름만 docs 로 시작하는 폴더", "x", ["docsgen/Thing.cs"], True),
        ("건너뛰기 표식은 코드가 있어도 안 부른다", "T433 문서 [skip" + " ci]", ["Assets/x.cs"], False),
        # ⚑⚑ 이 두 줄이 2026-09-11 00:0X 의 실측이다 — **이 자를 세운 커밋 자신이 CI 를 안 돌았다**.
        ("⚑ 표식이 «본문» 에만 있어도 안 부른다(그 사고 그대로)",
         "T433 1회차 — 유니티 잡만 비킨다\n\n  · [skip" + " ci] 규약은 그대로 둔다 — dotnet 잡까지 아낀다.",
         ["Assets/x.cs", ".github/workflows/ci.yml"], False),
        ("다른 철자도 본다", "고침\n\n[ci" + " skip] 로 적은 사람도 있다", ["tools/x.py"], False),
        ("대문자로 적어도 본다", "고침\n\n[SKIP" + " CI]", ["tools/x.py"], False),
        ("비슷하지만 표식이 아닌 말은 안 센다", "skip 규약을 ci 에서 논한다", ["tools/x.py"], True),
        ("파일을 못 읽으면 «부른다» 로 (fail-safe)", "머지", [], True),
        ("파일 목록이 None 이어도 «부른다»", "머지", None, True),
    ]
    for why, subject, files, want in calls:
        got = commit_calls_ci(subject, files)
        ok = got == want
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} 부른다={got} (기대 {want}) — {why}")

    if bad:
        print(f"✗ check_gate_age --self-test: 갈래 {bad}건이 기대와 다르다 — 위 표의 판정 규칙을 보라")
        return 1
    print(f"✓ check_gate_age --self-test: 갈래 {len(cases) + len(holes) + len(calls)}개가 전부 기대대로 갈린다")
    return 0


if __name__ == "__main__":
    sys.exit(main())
