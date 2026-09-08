#!/usr/bin/env python3
"""유니티 테스트 결과 XML → **실패한 케이스 이름·메시지를 로그 꼬리에 찍는다**(T239).

왜 있나 — 워커는 빨간 런의 까닭을 **로그로만** 읽을 수 있다. 아티팩트(결과 XML) 내려받기는
프록시가 막고(결정 289), `get_job_logs` 는 **꼬리**만 준다. 그런데 유니티 잡은
EditMode → PlayMode 순으로 도는데 마지막에 남는 줄은 **뒤엣것**의 «Exiting with code 0 (Ok)» 라,
EditMode 에서 죽으면 **꼬리 어디에도 «Failed» 가 없다**(2026-09-08 CI #503 에서 669KB 를 당겨도 0건이었다).
그래서 이 자를 잡 끝에서 돌려 **실패 목록을 로그의 마지막 몇 줄로** 만든다.

찍는 것 — 실패가 있으면 «[CI실패] N건» + 케이스마다 `풀네임 · message 첫 줄 · 파일:줄`,
없으면 «[CI실패] 0건 — 이 런의 빨강은 테스트가 아니다» 한 줄(그 한 줄이 다음 사람의 30분을 아낀다).
그리고 **맨 마지막 한 줄**이 «[CI실패] 요약 N건 — 이름 셋 외 M» 이다 — 목록은 길어질수록 스스로
꼬리 밖으로 밀려나는데(한 건 세 줄), 이 줄만은 N 과 무관하게 끝에서 22줄 안쪽에 남는다(결정 670).

⚑ `ci.yml` 에서 이 단계는 **유니티 잡의 마지막 단계**여야 한다 — 뒤에 오는 `screens` 배포가
   파일 42개를 한 줄씩 찍어 120줄 넘게 밀어낸다(CI #512 실측 · 결정 667).

사용:  python3 tools/ci_test_failures.py <결과 폴더 또는 XML>   ·   --self-test
"""
import os
import sys
import xml.etree.ElementTree as ET

TAG = "[CI실패]"
MAX_CASES = 40          # 한 런에서 이보다 많이 깨지면 앞엣것만 — 목록이 로그를 밀어내면 뜻이 없다
MSG_CHARS = 400         # 메시지 한 건의 길이(NUnit 메시지는 수십 줄이 되기도 한다)
SUM_NAMES = 3           # 마지막 «요약» 한 줄에 담는 이름 수


def _first_lines(text, n=3):
    """메시지의 앞 n 줄만 — NUnit 은 «Expected/But was» 를 첫 몇 줄에 담는다."""
    if not text:
        return ""
    lines = [ln.strip() for ln in text.replace("\r", "").split("\n") if ln.strip()]
    return " / ".join(lines[:n])[:MSG_CHARS]


def _stack_where(text):
    """스택에서 «우리 파일:줄» 첫 자리 — Assets/ 로 시작하는 줄이 임자를 가리킨다."""
    if not text:
        return ""
    for ln in text.replace("\r", "").split("\n"):
        i = ln.find("Assets/")
        if i >= 0:
            return ln[i:].strip()[:160]
    return ""


def _short(name):
    """«…Play.BattleWorldTests.EnemyArrows…» → «BattleWorldTests.EnemyArrows…» — 요약 한 줄에 여럿을 담으려고 뒤 두 마디만."""
    parts = name.split(".")
    return ".".join(parts[-2:]) if len(parts) >= 2 else name


def failures(path):
    """<test-case result="Failed"> 를 (풀네임, 메시지, 자리) 로 모은다. 파일이 깨졌으면 그 사실도 한 줄로."""
    out, broken = [], []
    files = []
    if os.path.isdir(path):
        for root, _dirs, names in os.walk(path):
            for n in sorted(names):
                if n.lower().endswith(".xml"):
                    files.append(os.path.join(root, n))
    elif os.path.exists(path):
        files.append(path)
    for f in files:
        try:
            tree = ET.parse(f)
        except Exception as e:                      # 결과가 반만 쓰였을 수도 있다 — 조용히 넘기지 않는다
            broken.append((f, str(e)[:160]))
            continue
        for tc in tree.iter("test-case"):
            if tc.get("result") != "Failed":
                continue
            fail = tc.find("failure")
            msg = stack = ""
            if fail is not None:
                m, s = fail.find("message"), fail.find("stack-trace")
                msg = _first_lines(m.text if m is not None else "")
                stack = _stack_where(s.text if s is not None else "")
            out.append((tc.get("fullname") or tc.get("name") or "?", msg, stack))
    return out, broken, files


def report(path, echo=print):
    got, broken, files = failures(path)
    if not files:
        echo(f"{TAG} 결과 XML 을 못 찾았다: {path}(테스트가 시작조차 못 했을 수 있다 — 러너·라이선스 쪽을 보라)")
        return 0
    for f, e in broken:
        echo(f"{TAG} ⚠ XML 을 못 읽었다: {f} — {e}")
    if not got:
        echo(f"{TAG} 0건 — XML {len(files)}개에 실패한 케이스가 없다. "
             "이 런의 빨강은 **테스트가 아니다**(러너·빌드·라이선스 쪽을 보라).")
        return 0
    echo(f"{TAG} {len(got)}건 — XML {len(files)}개에서 모았다(아티팩트는 프록시에 막히므로 이 목록이 워커가 읽는 유일한 자리다).")
    for i, (name, msg, where) in enumerate(got[:MAX_CASES], 1):
        echo(f"{TAG}  {i}. {name}")
        if msg:
            echo(f"{TAG}     ↳ {msg}")
        if where:
            echo(f"{TAG}     ↳ {where}")
    if len(got) > MAX_CASES:
        echo(f"{TAG}  … 그 밖 {len(got) - MAX_CASES}건(앞 {MAX_CASES}건만 찍는다)")
    # ⚑ 이 «요약» 한 줄은 **늘 맨 마지막**이어야 한다 — 목록 자신이 꼬리를 밀어내기 때문이다.
    #   한 건이 최대 세 줄이라 N 건이면 머리글은 끝에서 (잡 뒤처리 ≈21줄 · CI #512 실측) + 1 + 3N 줄 뒤로 간다
    #   → N=7 이면 벌써 50줄 밖이다. 그런데 크게 깨진 런일수록 이 목록이 절실하다.
    #   이 한 줄은 N 과 무관하게 **끝에서 22줄 안쪽**이라 «작은 꼬리로도 몇 건인지·무엇인지» 는 늘 읽힌다.
    head = " · ".join(_short(n) for n, _msg, _where in got[:SUM_NAMES])
    if len(got) > SUM_NAMES:
        head += f" 외 {len(got) - SUM_NAMES}"
    echo(f"{TAG} 요약 {len(got)}건 — {head}")
    return len(got)


def self_test():
    """네 경우로 깨뜨려 본다 — 실패 있음 · 실패 없음 · XML 없음 · 많이 깨진 런(요약 줄이 끝에 있나)."""
    import tempfile
    ok = True
    with tempfile.TemporaryDirectory() as d:
        with open(os.path.join(d, "editmode-results.xml"), "w", encoding="utf-8") as f:
            f.write('<test-run><test-suite><test-case name="A" fullname="N.A" result="Passed" />'
                    '<test-case name="B" fullname="N.B" result="Failed"><failure>'
                    '<message>Expected: 0\nBut was: 2</message>'
                    '<stack-trace>at X () in /github/workspace/Assets/Tests/PlayMode/Foo.cs:129</stack-trace>'
                    '</failure></test-case></test-suite></test-run>')
        lines = []
        n = report(d, lines.append)
        ok &= (n == 1) and any("N.B" in x for x in lines) and any("But was: 2" in x for x in lines)
        ok &= any("Assets/Tests/PlayMode/Foo.cs:129" in x for x in lines)
        ok &= lines[-1].startswith(f"{TAG} 요약 1건")      # 요약은 **맨 마지막 줄**이다
        print("ⓐ 실패 있음 —", "OK" if n == 1 else "실패")
    with tempfile.TemporaryDirectory() as d:
        with open(os.path.join(d, "r.xml"), "w", encoding="utf-8") as f:
            f.write('<test-run><test-case name="A" result="Passed" /></test-run>')
        lines = []
        n = report(d, lines.append)
        ok &= (n == 0) and any("테스트가 아니다" in x for x in lines)
        print("ⓑ 실패 0건 —", "OK" if n == 0 and lines else "실패")
    with tempfile.TemporaryDirectory() as d:
        lines = []
        report(d, lines.append)
        ok &= any("못 찾았다" in x for x in lines)
        print("ⓒ XML 없음 —", "OK" if lines else "실패")
    with tempfile.TemporaryDirectory() as d:            # ⓓ 많이 깨진 런 — 목록이 길어져도 요약은 끝에서 한 줄
        cases = "".join(f'<test-case fullname="N.C{i}.M{i}" result="Failed">'
                        f'<failure><message>boom {i}</message></failure></test-case>'
                        for i in range(12))
        with open(os.path.join(d, "r.xml"), "w", encoding="utf-8") as f:
            f.write(f"<test-run>{cases}</test-run>")
        lines = []
        n = report(d, lines.append)
        last = lines[-1]
        ok &= (n == 12) and last.startswith(f"{TAG} 요약 12건") and "외 9" in last
        ok &= "C0.M0" in last                            # 첫 자리는 요약만 봐도 안다
        print("ⓓ 많이 깨짐 —", "OK" if n == 12 and "외 9" in last else "실패")
    print("✓ ci_test_failures 자기 검사 통과" if ok else "✗ 자기 검사 실패")
    return 0 if ok else 1


if __name__ == "__main__":
    args = [a for a in sys.argv[1:]]
    if "--self-test" in args:
        sys.exit(self_test())
    report(args[0] if args else "unity-test-results")
    sys.exit(0)                                     # 이 자는 **알리기만** 한다 — 잡을 빨갛게 하는 것은 테스트 러너 몫이다
