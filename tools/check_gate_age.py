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
    그래서 «그 sha 뒤로 CI 를 부른 커밋(= `[skip ci]` 아닌 것)이 몇인가» 를 같이 세고,
    **빚이 있는데 답이 오래 없을 때만** 운다.

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


def debt(sha):
    """그 sha 뒤로 main 에 쌓인 «CI 를 부르는» 커밋 수 — `[skip ci]` 는 CI 를 안 부르므로 뺀다."""
    log = sh("git", "log", "--format=%s", f"{sha}..origin/main")
    if not log:
        return 0, 0
    lines = log.split("\n")
    return len(lines), len([x for x in lines if "[skip ci]" not in x])


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
    total, calling = debt(m["sha"])
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

    stale = "" if fresh else " · ⚠ 낡은 ref 로 잰 값이다(T286 — 위 첫 줄)"
    if bad:
        print("✗ check_gate_age: " + " · ".join(bad) +
              " — 유니티 잡 로그의 **머리**를 보라(라이선스·러너는 꼬리에 안 나온다 · T283/T284) · 보고만(막지 않는다)" + stale)
    else:
        print(f"✓ check_gate_age: 마지막 답이 {age}분 전 · 빚 {calling}개 · tests=success (보고만 · T285){stale}")
    return 0


def self_test():
    """판정 갈래가 실제로 갈리는가 — 값을 손으로 넣어 두 갈래를 다 내 본다."""
    def verdict(calling, age, tests, shots, limit=DEFAULT_MINUTES):
        bad = []
        if calling > 0 and age > limit:
            bad.append("빚")
        if tests != "success":
            bad.append("답")
        if shots == 0:
            bad.append("PNG")
        return "✗" if bad else "✓"

    cases = [
        # (빚, 나이, tests, PNG, 기대) — 이름이 곧 그 갈래의 뜻이다
        (0, 600, "success", 41, "✓", "아무도 안 밀면 열 시간이 지나도 정상(조용한 새벽)"),
        (17, 115, "success", 41, "✗", "빚이 쌓였는데 답이 없다 = 2026-09-09 그 사고"),
        (3, 10, "success", 41, "✓", "빚이 있어도 방금 답했으면 정상"),
        (0, 10, "failure", 41, "✗", "빚이 없어도 마지막 답이 빨강이면 운다"),
        (1, 10, "success", 0, "✗", "완주했는데 PNG 0장 = 테스트가 시작도 못 했다"),
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
    if bad:
        print(f"✗ check_gate_age --self-test: 갈래 {bad}건이 기대와 다르다 — 위 표의 판정 규칙을 보라")
        return 1
    print(f"✓ check_gate_age --self-test: 갈래 {len(cases)}개가 전부 기대대로 갈린다")
    return 0


if __name__ == "__main__":
    sys.exit(main())
