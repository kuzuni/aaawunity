#!/usr/bin/env python3
"""유니티 테스트 결과 XML → **실패한 케이스 이름·메시지를 로그 꼬리에 찍는다**(T239).

왜 있나 — 워커는 빨간 런의 까닭을 **로그로만** 읽을 수 있다. 아티팩트(결과 XML) 내려받기는
프록시가 막고(결정 289), `get_job_logs` 는 **꼬리**만 준다. 그런데 유니티 잡은
EditMode → PlayMode 순으로 도는데 마지막에 남는 줄은 **뒤엣것**의 «Exiting with code 0 (Ok)» 라,
EditMode 에서 죽으면 **꼬리 어디에도 «Failed» 가 없다**(2026-09-08 CI #503 에서 669KB 를 당겨도 0건이었다).
그래서 이 자를 잡 끝에서 돌려 **실패 목록을 로그의 마지막 몇 줄로** 만든다.

찍는 것 — 실패가 있으면 «[CI실패] N건» + 케이스마다 `풀네임 · message 첫 줄 · 파일:줄`.
그리고 **어느 갈래로 나가든 마지막 줄은 «[CI실패] 요약 …»** 이다(결정 678):
    실패 있음   [CI실패] 요약 N건 — 이름 셋 외 M
    실패 0건    [CI실패] 요약 0건 — … 이 런의 빨강은 테스트가 아니다
    XML 없음    [CI실패] 요약 — 결과 XML 을 못 찾았다: …
목록은 길어질수록 스스로 꼬리 밖으로 밀려나지만(한 건 세 줄) 이 줄만은 **건수와 무관하게** 뒤처리 바로 앞이다.

T278 — 같은 자가 «**무엇이 돌았나**»(명부)도 찍는다. 실패 목록은 «무엇이 깨졌나» 만 답하는데,
꼬리는 PlayMode 결과 XML 의 **알파벳 앞쪽을 자른다**(682KB 를 당겨도 `M` 앞 픽스처가 안 잡힌다 ·
run 579·582·593 실측). 그래서 «내가 이번에 세운 자가 그 런에 **실렸는가**» 를 아무도 못 읽었다 —
**초록은 «안 깨졌다» 일 뿐 «돌았다» 가 아니다**(asmdef·네임스페이스·글롭이 어긋나면 조용히 안 실린다).
    [CI명부] playmode-results.xml — 자 뭉치 71개 · 케이스 426건 · 실패 0
    [CI명부]  AchievementTests(8) ArenaDuelTests(5) … ZebraTests(2✗1)
`이름(건수)` 로 촘촘히 붙여 **71개가 11줄**이다(실측). ⚠ 명부는 **실패 보고보다 먼저** 찍는다 —
«[CI실패] 요약» 이 마지막 줄이어야 한다는 계약(결정 678)이 뒤에 붙이는 순간 깨진다.

⚑ 워커가 읽는 법 — **꼬리 50줄**을 당겨 이 «요약» 줄부터 본다. 목록 본문까지 보려면 `뒤처리 + 1 + 3N` 넘게 넓힌다.
   («30줄» 이면 모자란다 — 뒤처리 줄 수가 런마다 다르다: 캐시 적중 21줄(#512) · **캐시 저장 31줄**(#521).)

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


def _fixture_of(tc):
    """그 케이스가 속한 자 뭉치 이름 — `classname` 이 있으면 그것, 없으면 풀네임에서 마지막 마디를 뗀다."""
    cn = tc.get("classname")
    if cn:
        return cn
    full = tc.get("fullname") or ""
    return full.rsplit(".", 1)[0] if "." in full else (full or "?")


def roster(path):
    """XML 마다 «어떤 자 뭉치가 몇 건 돌았나» 를 센다 → [(파일, [(이름, 건수, 실패수)], 합계…)].

    ⚑ 왜 있나(T278) — `[CI실패]` 는 «무엇이 깨졌나» 를 답하지만 **«무엇이 돌았나» 는 아무도 안 찍는다.**
       워커는 아티팩트를 못 내려받고(결정 289) `get_job_logs` 는 **꼬리**만 주는데, 그 꼬리가 PlayMode
       결과 XML 의 **알파벳 앞쪽을 자른다**(실측: 682KB 를 당겨도 `M` 앞 픽스처가 안 잡힌다 · run 579·582·593).
       그래서 «내가 이번에 세운 자가 그 런에서 **실제로 실렸는가**» 를 확인할 길이 없었다 — 초록은
       «안 깨졌다» 일 뿐 «돌았다» 가 아니다(asmdef·네임스페이스·글롭이 어긋나면 **조용히 안 실린다**).
    """
    _got, _broken, files = failures(path)
    out = []
    for f in files:
        try:
            tree = ET.parse(f)
        except Exception:                          # 못 읽는 파일은 위 failures() 가 이미 한 줄로 알린다
            continue
        counts, fails, total, bad = {}, {}, 0, 0
        for tc in tree.iter("test-case"):
            fx = _fixture_of(tc)
            counts[fx] = counts.get(fx, 0) + 1
            total += 1
            if tc.get("result") == "Failed":
                fails[fx] = fails.get(fx, 0) + 1
                bad += 1
        # 이름은 마지막 마디(클래스 이름)면 충분하다 — **그 파일 안에서 겹치지 않을 때만** 줄인다.
        short = {}
        for fx in counts:
            short.setdefault(fx.rsplit(".", 1)[-1], []).append(fx)
        names = {fx: (s if len(v) == 1 else fx) for s, v in short.items() for fx in v}
        rows = sorted(((names[fx], counts[fx], fails.get(fx, 0)) for fx in counts), key=lambda r: r[0])
        out.append((os.path.basename(f), rows, total, bad))
    return out


ROSTER_TAG = "[CI명부]"
ROSTER_WIDTH = 150       # 한 줄에 담는 글자 수 — 꼬리를 아끼려고 여러 자를 한 줄에 붙인다
ROSTER_MAX = 400         # 이보다 많으면 앞엣것만(목록이 꼬리를 밀어내면 «요약» 이 안 읽힌다)


def report_roster(path, echo=print):
    """«무엇이 돌았나» 를 `이름(건수)` 로 촘촘히 찍는다 — 실패한 뭉치는 `이름(건수✗실패수)`.

    ⚑ **반드시 실패 보고보다 «먼저»** 찍는다. `[CI실패] 요약` 은 건수와 무관하게 **로그의 마지막 줄**이어야
       한다는 계약(결정 678)이 있고, 이 명부를 뒤에 붙이면 그 계약이 깨진다.
    """
    made = 0
    for fname, rows, total, bad in roster(path):
        echo(f"{ROSTER_TAG} {fname} — 자 뭉치 {len(rows)}개 · 케이스 {total}건"
             + (f" · **실패 {bad}건**" if bad else " · 실패 0"))
        line = ""
        for i, (name, n, f) in enumerate(rows):
            if i >= ROSTER_MAX:
                echo(f"{ROSTER_TAG}  … 그 밖 {len(rows) - ROSTER_MAX}개(앞 {ROSTER_MAX}개만 찍는다)")
                break
            piece = f"{name}({n}✗{f})" if f else f"{name}({n})"
            if len(line) + len(piece) + 1 > ROSTER_WIDTH:
                echo(f"{ROSTER_TAG}  {line}")
                line = ""
            line = (line + " " + piece).strip()
        if line:
            echo(f"{ROSTER_TAG}  {line}")
        made += 1
    if not made:
        echo(f"{ROSTER_TAG} 결과 XML 이 없어 명부를 못 만든다 — 아래 «[CI실패] 요약» 줄을 보라.")
    return made


def report(path, echo=print):
    got, broken, files = failures(path)
    # ⚑ 어느 갈래로 나가든 **마지막 줄은 «[CI실패] 요약» 으로 시작한다**(결정 678).
    #   갈래마다 꼴이 다르면 «꼬리에서 이것만 찾으면 된다» 가 성립하지 않는다 —
    #   실제로 회차 3 은 실패가 있을 때만 요약을 찍어서, 첫 검증 런(초록 · #521)에 요약이 아예 없었다.
    if not files:
        echo(f"{TAG} 요약 — 결과 XML 을 못 찾았다: {path}"
             "(테스트가 시작조차 못 했을 수 있다 — 러너·라이선스 쪽을 보라)")
        return 0
    for f, e in broken:
        echo(f"{TAG} ⚠ XML 을 못 읽었다: {f} — {e}")
    if not got:
        echo(f"{TAG} 요약 0건 — XML {len(files)}개에 실패한 케이스가 없다. "
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
    #   한 건이 최대 세 줄이라 N 건이면 머리글은 끝에서 (잡 뒤처리) + 1 + 3N 줄 뒤로 간다.
    #   → N=7 이면 벌써 50줄 밖이다. 그런데 크게 깨진 런일수록 이 목록이 절실하다.
    #   이 한 줄은 **N 과 무관하게** 뒤처리 바로 앞이라 «작은 꼬리로도 몇 건인지·무엇인지» 는 늘 읽힌다.
    head = " · ".join(_short(n) for n, _msg, _where in got[:SUM_NAMES])
    if len(got) > SUM_NAMES:
        head += f" 외 {len(got) - SUM_NAMES}"
    echo(f"{TAG} 요약 {len(got)}건 — {head}")
    return len(got)


def self_test():
    """네 경우로 깨뜨려 본다 — 실패 있음 · 실패 없음 · XML 없음 · 많이 깨진 런.
    그리고 **네 갈래 모두** 마지막 줄이 «[CI실패] 요약» 인가를 함께 묻는다(결정 678 의 계약)."""
    import tempfile
    ok = True
    lasts = {}
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
        lasts["ⓐ"] = lines[-1]
        print("ⓐ 실패 있음 —", "OK" if n == 1 else "실패")
    with tempfile.TemporaryDirectory() as d:
        with open(os.path.join(d, "r.xml"), "w", encoding="utf-8") as f:
            f.write('<test-run><test-case name="A" result="Passed" /></test-run>')
        lines = []
        n = report(d, lines.append)
        ok &= (n == 0) and any("테스트가 아니다" in x for x in lines)
        lasts["ⓑ"] = lines[-1]
        print("ⓑ 실패 0건 —", "OK" if n == 0 and lines else "실패")
    with tempfile.TemporaryDirectory() as d:
        lines = []
        report(d, lines.append)
        ok &= any("못 찾았다" in x for x in lines)
        lasts["ⓒ"] = lines[-1]
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
        lasts["ⓓ"] = last
        print("ⓓ 많이 깨짐 —", "OK" if n == 12 and "외 9" in last else "실패")
    # ⓔ 계약 — 갈래가 넷이어도 **꼬리에서 찾을 것은 하나**다(«[CI실패] 요약»).
    bad = [k for k, v in lasts.items() if not v.startswith(f"{TAG} 요약")]
    ok &= (len(lasts) == 4) and not bad
    print("ⓔ 마지막 줄 = «요약» (네 갈래) —", "OK" if not bad and len(lasts) == 4 else f"실패 {bad}")
    # ⓕ T278 명부 — «무엇이 돌았나» 가 이름으로 읽히는가 · 실패한 뭉치가 표시되는가 ·
    #    그리고 **명부를 찍어도 마지막 줄은 여전히 «[CI실패] 요약»** 인가(결정 678 계약을 안 깨는가).
    with tempfile.TemporaryDirectory() as d:
        with open(os.path.join(d, "playmode-results.xml"), "w", encoding="utf-8") as f:
            f.write('<test-run>'
                    '<test-case classname="K.Play.AttendancePlayTests" fullname="K.Play.AttendancePlayTests.A" result="Passed" />'
                    '<test-case classname="K.Play.ZebraTests" fullname="K.Play.ZebraTests.A" result="Passed" />'
                    '<test-case classname="K.Play.ZebraTests" fullname="K.Play.ZebraTests.B" result="Failed">'
                    '<failure><message>boom</message></failure></test-case>'
                    '</test-run>')
        lines = []
        report_roster(d, lines.append)
        n = report(d, lines.append)
        joined = "\n".join(lines)
        ok &= "AttendancePlayTests(1)" in joined          # 알파벳 앞쪽 자도 이름으로 읽힌다 — 이 자의 존재 이유다
        ok &= "ZebraTests(2✗1)" in joined                 # 깨진 뭉치는 명부에서도 표가 난다
        ok &= "자 뭉치 2개 · 케이스 3건" in joined
        ok &= lines[-1].startswith(f"{TAG} 요약 1건")      # 명부를 앞에 찍어도 마지막 줄은 그대로 «요약»
        ok &= lines[0].startswith(ROSTER_TAG)             # 명부는 실패 보고보다 **먼저**다
        print("ⓕ 명부(T278) —", "OK" if "AttendancePlayTests(1)" in joined and lines[-1].startswith(f"{TAG} 요약") else "실패")
    with tempfile.TemporaryDirectory() as d:              # ⓖ XML 이 없으면 명부도 그 사실을 한 줄로 말한다
        lines = []
        made = report_roster(d, lines.append)
        ok &= (made == 0) and lines and lines[0].startswith(ROSTER_TAG)
        print("ⓖ 명부 · XML 없음 —", "OK" if made == 0 and lines else "실패")
    print("✓ ci_test_failures 자기 검사 통과" if ok else "✗ 자기 검사 실패")
    return 0 if ok else 1


if __name__ == "__main__":
    args = [a for a in sys.argv[1:]]
    if "--self-test" in args:
        sys.exit(self_test())
    target = args[0] if args else "unity-test-results"
    report_roster(target)                           # T278 «무엇이 돌았나» — 반드시 아래 실패 보고보다 **먼저**다
    report(target)                                  # T239 «무엇이 깨졌나» — 마지막 줄이 «[CI실패] 요약» 이다
    sys.exit(0)                                     # 이 자는 **알리기만** 한다 — 잡을 빨갛게 하는 것은 테스트 러너 몫이다
