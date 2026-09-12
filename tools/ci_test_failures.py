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

T503 — 명부가 «**무엇을 안 쟀나**» 까지 답한다. `Assert.Ignore(...)` 로 끝난 케이스는 **실패가 아니라서**
«실패 0» 에 안 잡히면서 «케이스 N건» 에는 그대로 세어진다 ⇒ 그전까지 **재고 통과한 자와 아예 안 잰 자가
이 꼬리에서 똑같이 보였다**. 건너뜀 자체는 허락된 꼴이라(「잴 것이 없는 판은 통과시킨다」 §1 ⓑ) **막지 않고 센다**:
    [CI명부] playmode-results.xml — 자 뭉치 95개 · 케이스 248건(그중 **건너뜀 2건**) · 실패 0
    [CI명부] ⚠ **건너뜀 2건** — 돌았지만 «재지 않았다» …
    [CI명부]   1. ArenaResultTests.StageDiffers — 이 챕터의 무대가 마침 사막이라 …
⚠ **0건이면 한 글자도 안 늘린다** — 이 꼬리는 사람이 매 회차 읽는 자리라 조용할 때 조용해야 한다(갈래 ⓘ 가 그것을 지킨다).

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


SKIP_RESULTS = ("Skipped", "Inconclusive")   # NUnit3: Assert.Ignore → Skipped(label=Ignored) · Assert.Inconclusive → Inconclusive


def _skip_why(tc):
    """건너뛴 까닭 한 줄 — NUnit 은 `<reason><message>` 에 담는다(없으면 label 만이라도)."""
    for xp in ("reason/message", "failure/message"):
        el = tc.find(xp)
        if el is not None and (el.text or "").strip():
            return _first_lines(el.text, 1)[:160]
    return tc.get("label") or ""


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

    ⚑ **T503 — «돌았다» 의 한 켜 안쪽: 돌았지만 «재지 않았다».**
       `Assert.Ignore(...)` 로 끝난 케이스는 **실패가 아니다.** 그래서 위 `bad`(실패 수)에 안 잡히고
       `total`(케이스 수)에는 **그대로 세어진다** ⇒ 꼬리에서 **재고 통과한 자와 아예 안 잰 자가 똑같이 보인다**.
       건너뜀 자체는 죄가 아니다 — 「잴 것이 없는 판은 통과시킨다(§1 ⓑ)」는 주인이 허락한 꼴이다.
       탈은 **그것이 안 보이는 것**이다: 어느 날 조건이 굳어 «늘 건너뛰기» 가 되어도 꼬리는 초록이라
       그 자는 **문서로만 남은 자**가 된다. 실측(2026-09-12): PlayMode 에 `Assert.Ignore` 20곳 이상 ·
       그중 여럿은 판마다 조건이 변한다(`ArenaResultTests` 의 «무대가 마침 사막이라» 꼴).
       그래서 센다 — **막지는 않는다**(결정 493 · 이 자는 애초에 알리는 자다).
    """
    _got, _broken, files = failures(path)
    out = []
    for f in files:
        try:
            tree = ET.parse(f)
        except Exception:                          # 못 읽는 파일은 위 failures() 가 이미 한 줄로 알린다
            continue
        counts, fails, total, bad = {}, {}, 0, 0
        skips, skipped = {}, []                    # T503 — 뭉치별 건너뜀 수 · (풀네임, 까닭) 목록
        for tc in tree.iter("test-case"):
            fx = _fixture_of(tc)
            counts[fx] = counts.get(fx, 0) + 1
            total += 1
            if tc.get("result") == "Failed":
                fails[fx] = fails.get(fx, 0) + 1
                bad += 1
            elif tc.get("result") in SKIP_RESULTS:
                skips[fx] = skips.get(fx, 0) + 1
                skipped.append((tc.get("fullname") or tc.get("name") or "?", _skip_why(tc)))
        # 이름은 마지막 마디(클래스 이름)면 충분하다 — **그 파일 안에서 겹치지 않을 때만** 줄인다.
        short = {}
        for fx in counts:
            short.setdefault(fx.rsplit(".", 1)[-1], []).append(fx)
        names = {fx: (s if len(v) == 1 else fx) for s, v in short.items() for fx in v}
        rows = sorted(((names[fx], counts[fx], fails.get(fx, 0)) for fx in counts), key=lambda r: r[0])
        skip_rows = sorted(((names[fx], n) for fx, n in skips.items()), key=lambda r: r[0])
        out.append((os.path.basename(f), rows, total, bad, skip_rows, skipped))
    return out


ROSTER_TAG = "[CI명부]"
ROSTER_WIDTH = 150       # 한 줄에 담는 글자 수 — 꼬리를 아끼려고 여러 자를 한 줄에 붙인다
ROSTER_MAX = 400         # 이보다 많으면 앞엣것만(목록이 꼬리를 밀어내면 «요약» 이 안 읽힌다)


SKIP_MAX = 8             # 건너뜀 상세는 이만큼만 — 꼬리는 «[CI실패] 요약» 몫을 남겨 둬야 한다(결정 678)


def _report_skips(skip_rows, skipped, echo):
    """**돌았지만 재지 않은** 케이스를 이름·까닭으로 찍는다 — 0건이면 **아무 줄도 안 찍는다**(T503).

    ⚑ 왜 여기(명부)이고 «[CI실패]» 가 아닌가 — 건너뜀은 **실패가 아니다**. 실패 쪽에 섞으면
       «[CI실패] 요약 N건» 이 부풀어 «런의 빨강» 을 잘못 세게 된다(결정 678 의 계약이 그 줄에 걸려 있다).
       건너뜀이 답하는 물음은 «무엇이 돌았나» 쪽이다 — 정확히는 **«돌았는데 무엇을 안 쟀나»**.
    """
    if not skipped:
        return 0
    head = " ".join(f"{name}({n})" for name, n in skip_rows[:SKIP_MAX])
    if len(skip_rows) > SKIP_MAX:
        head += f" 외 {len(skip_rows) - SKIP_MAX}뭉치"
    echo(f"{ROSTER_TAG} ⚠ **건너뜀 {len(skipped)}건** — 돌았지만 «재지 않았다»(`Assert.Ignore` 등). "
         "**실패가 아니라 위 «실패 0» 에 안 잡힌다** — 늘 건너뛰기로 굳으면 그 자는 문서로만 남는다.")
    echo(f"{ROSTER_TAG}   뭉치: {head}")
    for i, (name, why) in enumerate(skipped[:SKIP_MAX], 1):
        echo(f"{ROSTER_TAG}   {i}. {_short(name)}" + (f" — {why}" if why else ""))
    if len(skipped) > SKIP_MAX:
        echo(f"{ROSTER_TAG}   … 그 밖 {len(skipped) - SKIP_MAX}건(앞 {SKIP_MAX}건만 찍는다)")
    return len(skipped)


def report_roster(path, echo=print):
    """«무엇이 돌았나» 를 `이름(건수)` 로 촘촘히 찍는다 — 실패한 뭉치는 `이름(건수✗실패수)`.

    ⚑ **반드시 실패 보고보다 «먼저»** 찍는다. `[CI실패] 요약` 은 건수와 무관하게 **로그의 마지막 줄**이어야
       한다는 계약(결정 678)이 있고, 이 명부를 뒤에 붙이면 그 계약이 깨진다.
    """
    made = 0
    for fname, rows, total, bad, skip_rows, skipped in roster(path):
        # T503 — 건너뜀은 **0건이면 한 글자도 안 늘린다**(이 꼬리는 사람이 매 회차 읽는 자리다 · 결정 667).
        #   0건이 아닐 때는 «케이스 N건» 바로 옆에 붙인다 — 그 수가 그 N 안에 들어 있기 때문이다.
        echo(f"{ROSTER_TAG} {fname} — 자 뭉치 {len(rows)}개 · 케이스 {total}건"
             + (f"(그중 **건너뜀 {len(skipped)}건**)" if skipped else "")
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
        _report_skips(skip_rows, skipped, echo)
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
        # ⚑ T400 — 이 줄이 «러너·라이선스» 만 가리켜서 런 1005 때 워커가 라이선스를 뒤졌다. 실제 까닭은 그 위 단계의
        #   «game-ci CLI 내려받기 404» 였다(unity-test-runner 가 cliVersion=latest 를 받아 오다 실패 · 0초 만에 죽는다).
        #   이 자는 XML 만 보므로 까닭을 알 수 없다 — 그러니 **어디를 볼지**를 순서대로 적어 준다(값싼 것부터).
        # ⚑ T482 — «자리는 있는데 비었다» 와 «그런 자리가 아예 없다» 는 전혀 다른 일인데 위 줄이 둘을 같게 말했다.
        #   앞엣것만 «테스트가 시작조차 못 했다» 이고, 뒤엣것은 **부른 사람이 자리를 잘못 댄 것**이다.
        #   실제로 그렇게 샜다(2026-09-11 · 워커 H): 행마다 «확인 = 다음 완주 런 [CI명부]» 라고 적혀 있어
        #   `ci_test_failures.py 1123` 처럼 **런 번호**를 넘겼고, 자는 그것을 CI 갈래로 읽어 라이선스·도커를 가리켰다.
        #   이 자는 로컬 XML 만 읽는다(런 번호로는 아무것도 못 받는다 · 머리글 «사용:» 줄).
        if not os.path.exists(path):
            echo(f"{TAG} 요약 — 그런 자리가 없다: {path}"
                 "(이 자는 **결과 폴더나 XML 파일**을 받는다 — 런 번호가 아니다. "
                 "런의 빨강을 워커가 읽는 자리는 그 런 유니티 잡 로그의 «[CI실패] 요약» 줄이다)")
            return 0
        echo(f"{TAG} 요약 — 결과 XML 을 못 찾았다: {path}"
             "(테스트가 시작조차 못 했다 — 유니티 잡의 «game-ci/unity-test-runner» 단계를 먼저 보라: "
             "0초 만에 죽었으면 CLI 내려받기 실패(404)나 도커 · 몇 분 뒤 죽었으면 라이선스 · 그 뒤면 진짜 고장이다)")
        return 0
    for f, e in broken:
        echo(f"{TAG} ⚠ XML 을 못 읽었다: {f} — {e}")
    if not got:
        # ⚑ T400 — 이 줄이 «이 런의 빨강은 테스트가 아니다» 였다. 그런데 이 갈래는 **런이 초록일 때도** 지나간다
        #   (런 1007 이 그랬다) — 그러면 꼬리가 있지도 않은 빨강을 말한다. 이 자는 XML 만 보므로 잡이 빨간지 모른다
        #   ⇒ **아는 것만 말하고**(실패 0) 빨강은 «있다면» 으로 둔다.
        echo(f"{TAG} 요약 0건 — XML {len(files)}개에 실패한 케이스가 0. "
             "**이 잡이 빨갛다면** 그 빨강은 테스트가 아니다(러너·빌드·라이선스 · 위 단계를 보라).")
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
        ok &= not any("그런 자리가 없다" in x for x in lines)   # T482 — 빈 폴더는 «없는 자리» 가 아니다
        lasts["ⓒ"] = lines[-1]
        print("ⓒ XML 없음(자리는 있다) —", "OK" if lines else "실패")
    # ⓒ' T482 — 그런 자리가 아예 없을 때. 빈 폴더와 **다른 줄**이어야 한다(둘을 같게 말해 워커가 라이선스를 뒤졌다).
    lines = []
    n = report(os.path.join(tempfile.gettempdir(), "_ci_test_failures_no_such_place_"), lines.append)
    ok &= (n == 0) and any("그런 자리가 없다" in x for x in lines)
    ok &= not any("시작조차 못 했다" in x for x in lines)
    ok &= lines[-1].startswith(f"{TAG} 요약")                  # 결정 678 계약은 이 갈래에서도 그대로
    print("ⓒ' 없는 자리 —", "OK" if any("그런 자리가 없다" in x for x in lines)
          and not any("시작조차 못 했다" in x for x in lines) else "실패")
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
    # ⓗ T503 건너뜀 — «돌았지만 재지 않은» 케이스가 이름·까닭으로 읽히는가.
    #    ⚑ 이 갈래의 핵심은 «실패 0» 과 «건너뜀 2» 가 **한 XML 에 같이** 있는 것이다 —
    #      그 둘이 갈라지지 않던 것이 이 절이 고치는 병이다.
    with tempfile.TemporaryDirectory() as d:
        with open(os.path.join(d, "playmode-results.xml"), "w", encoding="utf-8") as f:
            f.write('<test-run>'
                    '<test-case classname="K.Play.ArenaResultTests" fullname="K.Play.ArenaResultTests.A" result="Passed" />'
                    '<test-case classname="K.Play.ArenaResultTests" fullname="K.Play.ArenaResultTests.B" '
                    'result="Skipped" label="Ignored"><reason><message>무대가 마침 사막이라 둘을 못 가른다</message></reason></test-case>'
                    '<test-case classname="K.Play.PvpStageTests" fullname="K.Play.PvpStageTests.C" '
                    'result="Inconclusive"><reason><message>1대1 노드가 안 섰다</message></reason></test-case>'
                    '</test-run>')
        lines = []
        report_roster(d, lines.append)
        n = report(d, lines.append)
        joined = "\n".join(lines)
        ok &= (n == 0)                                     # 건너뜀은 **실패가 아니다** — 실패 셈에 섞이면 안 된다
        ok &= "건너뜀 2건" in joined                        # Skipped 와 Inconclusive 둘 다 센다
        ok &= "케이스 3건(그중 **건너뜀 2건**)" in joined    # 그 수가 케이스 수 안에 들어 있다는 것까지 말한다
        ok &= "무대가 마침 사막이라" in joined               # 까닭이 읽힌다(reason/message)
        ok &= "1대1 노드가 안 섰다" in joined
        ok &= "ArenaResultTests.B" in joined                # 어느 케이스인지 이름으로 안다
        ok &= lines[-1].startswith(f"{TAG} 요약 0건")        # 결정 678 계약 — 건너뜀을 찍어도 마지막 줄은 그대로
        print("ⓗ 건너뜀(T503) —", "OK" if "건너뜀 2건" in joined and n == 0 else "실패")
    # ⓘ T503 짝 — **건너뜀이 0건이면 한 글자도 안 늘린다**. 이 갈래가 없으면 «늘 시끄러운 꼬리» 를 못 막는다.
    with tempfile.TemporaryDirectory() as d:
        with open(os.path.join(d, "playmode-results.xml"), "w", encoding="utf-8") as f:
            f.write('<test-run>'
                    '<test-case classname="K.Play.A" fullname="K.Play.A.x" result="Passed" />'
                    '<test-case classname="K.Play.A" fullname="K.Play.A.y" result="Failed">'
                    '<failure><message>boom</message></failure></test-case>'
                    '</test-run>')
        lines = []
        report_roster(d, lines.append)
        joined = "\n".join(lines)
        ok &= "건너뜀" not in joined                        # 조용할 때는 조용하다
        ok &= "(그중" not in joined                         # 머리글도 안 늘어난다
        ok &= "케이스 2건 · **실패 1건**" in joined          # 종전 꼴 그대로
        print("ⓘ 건너뜀 0건 = 조용함 —", "OK" if "건너뜀" not in joined else "실패")
    print("✓ ci_test_failures 자기 검사 통과" if ok else "✗ 자기 검사 실패")
    return 0 if ok else 1


if __name__ == "__main__":
    args = [a for a in sys.argv[1:]]
    if "--self-test" in args:
        sys.exit(self_test())
    target = args[0] if args else "unity-test-results"
    # T278 «무엇이 돌았나» — 반드시 아래 실패 보고보다 **먼저**다.
    # ⚑ 감싸 두는 까닭: 이 단계는 **모든 런의 마지막**에서 `if: always()` 로 돈다. 여기서 예외가 나면
    #   그 자체로 단계가 빨개져 **초록 런까지 빨갛게 만든다** — 명부는 «있으면 좋은 것» 이지 판정이 아니다.
    #   그리고 이 자의 본업(실패 목록)은 명부가 죽어도 그대로 나가야 한다.
    try:
        report_roster(target)
    except Exception as e:                          # noqa: BLE001 — 무엇이 터지든 런을 빨갛게 하지 않는다
        print(f"{ROSTER_TAG} 명부를 못 만들었다(런 판정과 무관하다): {type(e).__name__} {str(e)[:200]}")
    report(target)                                  # T239 «무엇이 깨졌나» — 마지막 줄이 «[CI실패] 요약» 이다
    sys.exit(0)                                     # 이 자는 **알리기만** 한다 — 잡을 빨갛게 하는 것은 테스트 러너 몫이다
