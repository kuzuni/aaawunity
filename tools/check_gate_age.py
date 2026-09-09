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
    python3 tools/check_gate_age.py --self-test
  `git fetch origin screens main` 을 먼저 해 두면 정확하다(안 해도 있는 것으로 잰다).
"""
import datetime
import json
import subprocess
import sys

DEFAULT_MINUTES = 45   # 유니티 잡 ~9분 + CI 를 부르는 커밋이 ~5분마다 → 성한 날은 이 선을 안 넘는다(실측)


def sh(*a):
    return subprocess.run(a, capture_output=True, text=True).stdout.strip()


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

    if bad:
        print("✗ check_gate_age: " + " · ".join(bad) +
              " — 유니티 잡 로그의 **머리**를 보라(라이선스·러너는 꼬리에 안 나온다 · T283/T284) · 보고만(막지 않는다)")
    else:
        print(f"✓ check_gate_age: 마지막 답이 {age}분 전 · 빚 {calling}개 · tests=success (보고만 · T285)")
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
