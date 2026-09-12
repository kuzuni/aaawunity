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
import io
import json
import os
import re
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


def refresh_deploy():
    """`gh-pages` 를 따로, **되면 좋고 안 되면 마는** 꼴로 당긴다 (T436).

    ⚠ 위 <see cref="refresh"/> 의 `git fetch origin screens main` 에 `gh-pages` 를 **끼워 넣지 않는다** —
      끼우면 그 가지가 아직 없는 클론(첫 배포 전 · 포크)에서 `fetch` 가 통째로 실패하고,
      그러면 이 자가 «지금 갔다 오지 못했다» 를 첫 줄에 찍어 **성한 게이트를 사고처럼 보이게** 만든다.
      새로 더하는 눈이 원래 있던 눈을 멀게 하면 안 된다.
    """
    subprocess.run(["git", "fetch", "--quiet", "origin", "gh-pages"], capture_output=True, text=True)


# gh-pages 머리 커밋의 메시지 꼴은 `deploy-last-green.yml` 이 정한다 —
#   `WebGL 배포 <소스 sha 40자> (마지막 초록 · T187 · 런 N)` 이고, 그 뒤에 액션이 «민 커밋» sha 를 하나 더 붙인다.
#   그래서 **첫 번째** 40자가 «폰이 지금 돌리는 소스» 다. 그 워크플로의 `pick` 단계도 `... | head -1` 로 같은 규칙을 쓴다.
SHA40_RE = re.compile(r"\b([0-9a-f]{40})\b")


def deploy_source(message):
    """gh-pages 머리 메시지에서 «이 빌드가 구운 소스 커밋» 을 집는다 — 없으면 None."""
    m = SHA40_RE.search(message or "")
    return m.group(1) if m else None


def read_deploy():
    """(소스 sha, 그 배포 커밋의 utc) — `origin/gh-pages` 가 없거나 못 읽으면 None."""
    out = sh("git", "log", "-1", "--format=%ct%n%B", "origin/gh-pages")
    if not out:
        return None
    head, _, body = out.partition("\n")
    try:
        ts = int(head.strip())
    except ValueError:
        return None
    return deploy_source(body), datetime.datetime.fromtimestamp(ts, datetime.timezone.utc)


# ⛳ T451 — «평소 폭» 을 **재서 적어 둔다**. 문턱이 아니라 **자**다(아래 deploy_band_line 의 주석에 그 차이).
#   실측 2026-09-11 11:5X · 표본 = `deploy-last-green.yml` 런 300개(2026-09-09 12:37 ~ 09-11 10:52 · 이틀치).
#   재는 법: 성공 런 가운데 **실행시간(큐 제외) 15분 이상**만 «실제로 구워 민 것» 으로 세었다 —
#     분포가 깨끗하게 둘로 갈린다(1분 미만 142개 = pick 이 go=false 로 끊은 것 · 15~30분 68개 = 구운 것).
#     경계를 5분으로 낮춰도 중앙값 20→19분 · p90 119→96분으로 답이 안 뒤집힌다(그래서 경계가 답을 만들지 않는다).
DEPLOY_BAND = ("평소 폭(2026-09-11 실측 · 배포런 300개/이틀 · T451): "
               "배포 간격 중앙값 **20분** · p75 40분 · p90 119분 · 실측 최대 287분(새벽) · 굽기만 19~30분")


def deploy_why_line(dep_sha, green_sha, verdict):
    """뒤처졌을 때 **왜** 인지 한 줄 — 답에 필요한 두 조각을 이 자가 이미 둘 다 들고 있다. 없으면 None.

    ⛳ T467 — 이 줄이 없어 **한 회차가 통째로 들었다**(2026-09-11 17:5X · 워커 L 실측 · 그 값을 치른 것이 나다).
    그 회차에 내가 한 일을 그대로 적는다: 위 두 줄에서 «폰 43커밋 뒤 · 116분» 과 «유니티 잡 = failure» 를
    **따로따로** 읽고, 배포가 고장 났다고 보아 배포 런 12개와 CI 런 14개를 API 로 받아
    «`pick` 이 초록을 놓치고 있다» 까지 갔다가, 마지막에 **자기가 옳았음**을 확인하고 끝났다.
    ⇒ 두 조각을 **잇기만** 하면 그 걸음이 통째로 없어진다: 유니티 잡이 빨간 동안 폰이 뒤처지는 것은
      고장이 아니라 **T187 이 세운 보호가 일하는 모습**이다(폰에는 «유니티 잡이 초록이던 커밋» 만 간다).

    ⚠ 그 회차가 실제로 걸린 함정도 같이 적는다 — **«런 전체가 success» 와 «폰에 갈 수 있는 초록» 은 다르다.**
      문서만 고친 회차는 `has_code != 'false'` 갈래에서 유니티 잡이 **skipped** 라 런은 success 인데
      `pick` 은 그것을 안 고른다(옳다 · T187·T433). 그래서 «CI 초록인데 왜 배포가 안 되나» 로 다시 샌다 —
      이 자가 보는 «유니티 잡» 은 `screens` 가 적은 **실제로 돈 그 잡**이라 애초에 그 헷갈림이 없는 쪽이다.

    판정은 안 한다(rc 0줄) — 결정 930·1184 그대로, 사실만 눈앞에 놓는다.
    """
    if not dep_sha:
        return None
    if green_sha and dep_sha[:9] == green_sha[:9]:
        return None
    if verdict != "success":
        return ("⤷ **배포가 멈춘 것이 아니라 보낼 초록이 없다** — 유니티 잡이 빨간 동안 폰은 «마지막 초록» 에 "
                "머문다(T187 의 보호가 일하는 모습이다). 빨강이 풀리면 저절로 흐른다 — 배포 쪽은 볼 것 없다(T467)")
    return ("⤷ 유니티 잡은 초록이다 — 곧 굽고 있거나(굽기 19~30분) `pick` 이 아직 그 커밋을 안 골랐다. "
            "아래 폭을 넘기 전에는 배포 쪽을 뒤질 까닭이 없다(T467)")


def deploy_band_line(dep_sha, green_sha):
    """뒤처져 있을 때만 «그런데 그게 평소인가» 를 같이 놓는다 — 없으면 None.

    ⚠ **이것은 문턱이 아니다.** 문턱은 «N분이 넘으면 빨강» 이라 판정을 바꾸고, 결정 930 이 그것을 막았다.
    여기 놓는 것은 **자**다 — 판정도 rc 도 안 건드리고, 읽는 사람 눈앞의 수 옆에 «남들은 이쯤이었다» 를 둘 뿐이다.
    까닭(T451 실측): 이 줄이 없어 **헛 놀람이 실제로 두 번 났다** — «gh-pages 가 13커밋 뒤» · «8커밋·62분 뒤» 가
    둘 다 정상치였는데(각각 굽는 중 · 빨강이라 건너뜀) 그 자리에서는 그것을 알 길이 없었다.
    20분마다 한 번 미는 파이프에서 «62분 뒤» 는 p75 언저리다 — 그 사실 한 줄이면 안 놀랐다.

    같은 커밋이면 안 적는다: 폰이 최신인데 «평소 폭» 을 읽을 까닭이 없고, 매 회차 한 줄이 늘면 그것이 소음이다.
    """
    if not dep_sha:
        return None
    if green_sha and dep_sha[:9] == green_sha[:9]:
        return None
    return DEPLOY_BAND


def deploy_phrase(dep_sha, green_sha, behind):
    """폰에 가 있는 것과 «마지막 초록» 의 사이 — **문턱을 안 쓴다**(사실만 적는다 · 결정 930).

    배포는 원래 «마지막 초록» 을 굽느라 한 커밋쯤 뒤처진다(결정 1193). 그래서 «몇 분이면 늦은 것이다» 를
    이 자가 정할 수 없다 — 정하면 조용한 새벽마다 헛 경보가 나거나, 문턱을 낮춰 달라는 압력이 생긴다.
    이 자가 하는 일은 **그 수를 눈앞에 놓는 것**이다(결정 1184 — 자가 옳게 도는데 사실이 안 보이던 자리).

    ⛳ T451 — 그 «수» 옆에 **잴 자**가 생겼다(<see cref="deploy_band_line"/>). 판정은 여전히 안 한다:
      T436 ⑤ 가 남긴 물음은 «몇 분이면 늦은가»(문턱)였는데, 재 보니 답할 물음이 아니었다 —
      간격이 중앙값 20분인데 p90 이 119분이라 **꼬리가 본체의 여섯 배**다(새벽엔 밀 초록이 안 생겨 길어진다).
      그런 분포에 문턱을 박으면 조용한 시간마다 빨개진다 — 결정 930 이 걱정한 그 꼴 그대로다. 그래서 자만 놓는다.
    """
    if not dep_sha:
        return "그 배포가 어느 커밋을 구웠는지 메시지에 안 적혀 있다(꼴이 바뀌었나)"
    if green_sha and dep_sha[:9] == green_sha[:9]:
        return "마지막 초록과 **같은 커밋**이다"
    if behind is None:
        return "마지막 초록과의 사이를 못 셌다(그 커밋이 이 클론에 없다)"
    if behind < 0:
        return "마지막 초록의 조상이 아니다 — 가지가 갈렸거나 되돌린 자리다"
    return f"마지막 초록보다 **{behind}커밋 뒤**다"


def count_between(old, new):
    """old..new 커밋 수 — old 가 new 의 조상이 아니거나 둘 중 하나가 없으면 None/−1."""
    if not old or not new:
        return None
    if subprocess.run(["git", "merge-base", "--is-ancestor", old, new],
                      capture_output=True, text=True).returncode != 0:
        return -1
    out = sh("git", "rev-list", "--count", f"{old}..{new}")
    try:
        return int(out)
    except ValueError:
        return None


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


def parse_iso(s):
    """`git log %cI` 의 시각 한 줄 → tz 를 아는 datetime · 못 읽으면 None(터지지 않는다).

    ⚠ 파이썬 3.10 아래의 `fromisoformat` 은 «+00:00» 은 읽고 «Z» 는 못 읽는다 — 그 한 글자만 바꿔 준다.
    """
    s = (s or "").strip()
    if not s:
        return None
    try:
        return datetime.datetime.fromisoformat(s.replace("Z", "+00:00"))
    except ValueError:
        return None


def minutes_since(stamp):
    """그 시각에서 지금까지 몇 분 — 미래로 적힌 시각(시계 어긋남)은 0 으로 본다."""
    if stamp is None:
        return None
    return max(0, int((datetime.datetime.now(datetime.timezone.utc) - stamp).total_seconds() // 60))


def debt(sha):
    """그 sha 뒤로 main 에 쌓인 «유니티 잡을 부르는» 커밋 수 → (총, 부르는 수, 구멍, **가장 오래 기다린 빚의 시각**).

    ⚑ T433 이전에는 «`[skip ci]` 아닌 것» 하나로 셌다. 이제는 **문서만 바꾼 커밋도 안 부른다** —
      안 맞추면 문서만 오가는 조용한 회차마다 이 자가 헛 경보를 낸다(고침을 넣은 회차에 같이 맞췄다).

    ⚑⚑ **네 번째 값이 T438 이다.** 판정이 오래 «마지막 답의 나이» 를 썼는데, T433 이 그 수의 뜻을 갈라 놓았다:
      전에는 «표식 없는 푸시 = 유니티 답» 이라 답이 늙는 유일한 길이 «빚이 안 풀리는 것» 이었다.
      지금은 **아무 빚 없이 답만 늙는 구간이 정상**(문서만 오가는 회차)이고,
      그 뒤 첫 코드 푸시가 그 늙은 나이를 **통째로 물려받아** 곧바로 ✗ 가 된다.
      2026-09-11 02:3X 실측이 그것이다 — «빚 1개가 61분째 답을 못 받았다» 인데 그 빚은 **7분 전**에 밀렸고
      런 1062 가 그때 이미 돌고 있었다. 그래서 재야 할 것은 **그 빚이 실제로 기다린 시간**이다.
    """
    # ⚑ 커밋마다 `git show` 를 부르지 않는다 — 낡은 `meta.json` 은 빚이 300개까지 간다(T286 실측).
    #   `git log --name-only` 한 번으로 «메시지 전체 + 그 커밋이 건드린 파일» 을 같이 받는다.
    #   `%x01` 이 커밋 경계 · `%x02` 가 «메시지 끝» 이다(둘 다 파일 이름에는 못 들어가는 글자).
    #   ⚑ `%s`(제목)가 아니라 `%B`(전체)인 까닭은 위 `SKIP_TOKENS` 주석에 적힌 실측이다.
    # `--diff-merges=cc` — 머지 커밋도 «충돌을 풀며 손댄 파일» 을 내놓는다(안 주면 머지는 늘 «빚» 으로 센다).
    #   ⚠ 옛 git 에 그 옵션이 없으면 **빈 답**이 오는데, 그것을 «빚 0» 으로 읽으면 이 자가 눈이 먼다 —
    #     빈 답이면 옵션 없이 한 번 더 물어본다(그 판에선 머지가 빚으로 세어지지만, 그쪽이 안전하다).
    rng = f"{sha}..origin/main"
    # `%cI` = 그 커밋의 시각(ISO · 타임존 포함) · `\x03` 이 «메시지 끝» 이다(파일 이름에 못 들어가는 글자).
    fmt = "\x01%cI\x02%B\x03"
    log = sh("git", "log", "--name-only", "--diff-merges=cc", "--format=" + fmt, rng)
    if not log:
        log = sh("git", "log", "--name-only", "--format=" + fmt, rng)
    if not log:
        return 0, 0, [], None, None
    total = calling = 0
    silent = []
    oldest = None                       # 가장 오래 기다린 «부르는» 커밋의 시각 (git log 는 새 것부터 준다)
    tip_calls = None                    # **맨 앞(= 가장 새) 커밋이 CI 를 부르는가** — 아래 T497 이 이것으로 «임자 없는 빚» 을 가른다
    for chunk in log.split("\x01"):
        if not chunk.strip():
            continue
        when, _, rest = chunk.partition("\x02")
        message, _, rest = rest.partition("\x03")
        files = [r for r in rest.split("\n") if r.strip()]
        total += 1
        if tip_calls is None:           # git log 는 새 것부터 주므로 첫 조각이 곧 끝 커밋이다
            tip_calls = commit_calls_ci(message, files)
        if commit_calls_ci(message, files):
            calling += 1
            stamp = parse_iso(when)
            # ⚠ 시각을 못 읽으면 **그 커밋을 없는 셈 치지 않는다** — 못 읽은 채로 지나가면
            #   빚이 있는데 «기다린 적 없다» 가 되어 이 자가 눈이 먼다. 아래 main 이 그 갈래를 받는다.
            if stamp is not None and (oldest is None or stamp < oldest):
                oldest = stamp
        elif has_skip_token(message) and files and not all(is_doc_path(f) for f in files):
            # ⚑ 코드를 건드렸는데 표식 때문에 안 돈 커밋 — **구멍**이다(T433 2회차).
            #   대개 «본문에 그 규약을 인용» 한 사고다. 실측 2건 중 하나는 본문이
            #   «이 커밋은 … 없이 민다» 였다 — 그 문장 자신이 CI 를 껐다.
            silent.append(message.split("\n")[0][:70])
    return total, calling, silent, oldest, tip_calls


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
    total, calling, silent, oldest, tip_calls = debt(m["sha"])
    verdict = m.get("tests", "?")
    # ⛳ T438 — 판정에 쓰는 수는 **빚이 실제로 기다린 시간**이다(위 `debt` 의 설명 참조).
    #   시각을 못 읽었는데 빚은 있는 갈래에서는 «잴 수 없다» 를 «괜찮다» 로 읽지 않는다 —
    #   종전 수(마지막 답의 나이)로 되물러 판정한다. 그쪽이 안전하다(놓치는 것보다 헛 경보가 낫다).
    wait = minutes_since(oldest)
    fallback = calling > 0 and wait is None
    judged = age if fallback else wait

    print(f"· 마지막 완주 유니티 잡 = 런 {m['run']} ({m['sha'][:9]} · {m['utc']} · {age}분 전) "
          f"· tests={verdict} · PNG {m.get('shots', '?')}장")
    if calling > 0:
        waited = (f"{wait}분째 기다린다" if wait is not None
                  else "기다린 시간을 못 읽었다 — 답의 나이로 대신 잰다")
        print(f"· 그 뒤 main 커밋 {total}개(그 중 CI 를 부르는 것 {calling}개 · 가장 오래된 빚이 {waited})")
    else:
        # 빚 0 이면 답이 아무리 늙어도 기다리는 것이 없다 — T433 뒤로는 문서만 오가는 회차가 정상적으로 이 꼴이다.
        print(f"· 그 뒤 main 커밋 {total}개(그 중 CI 를 부르는 것 0개 — 기다리는 것이 없다)")

    # ⛳ T436 — **폰에 가 있는 것도 같이 적는다.** 위 두 줄은 «CI 게이트» 만 본다(`screens` 는 유니티 잡이 쓴다).
    #   2026-09-11 00:4X 에 T435 가 배포 스모크를 `--play`(strict)로 올리면서 **새 상태가 생겼다** —
    #   굽기 직후 봇이 빨개지면 gh-pages 로 안 민다. 그러면 **CI 는 초록·빚 0 인데 폰만 옛 빌드에 묶인다.**
    #   그 조합은 T435 이전에는 있을 수 없었고(스모크가 보고만이라 늘 밀렸다), 지금은 이 자가 그때도 «✓» 를 찍는다.
    #   배포 런의 `::warning::` 은 그 런 로그 안에만 있고 워커는 `ci.yml` 런만 본다(T239·T278) ⇒ **아무도 안 본다.**
    #   ⚠ 판정에는 안 넣는다 — 배포는 원래 한 커밋쯤 뒤처지고(결정 1193), 여기서는 **보여 주기만** 한다.
    #   ⛳ T451(2026-09-11) — 이 주석이 ««몇 분이면 늦다» 를 재 본 적이 없다» 라고 하던 자리다. **이제 재 봤고,
    #     그래도 문턱은 안 박는다** — 재 보니 문턱을 박으면 안 되는 분포였기 때문이다(간격 중앙값 20분 · p90 119분).
    #     대신 뒤처졌을 때만 «평소는 이쯤» 한 줄을 같이 놓는다(DEPLOY_BAND · 판정 0줄 · rc 안 건드림).
    if "--no-fetch" not in args:
        refresh_deploy()
    dep = read_deploy()
    if dep is None:
        print("· 배포(gh-pages) = 못 읽었다 — 아직 첫 배포 전이거나 그 가지를 안 받아 왔다")
    else:
        dep_sha, dep_when = dep
        dep_age = int((datetime.datetime.now(datetime.timezone.utc) - dep_when).total_seconds() // 60)
        behind = count_between(dep_sha, m["sha"]) if dep_sha else None
        print(f"· 배포(gh-pages · 주인 폰) = 소스 {(dep_sha or '?')[:9]} · {dep_age}분 전 "
              f"· {deploy_phrase(dep_sha, m['sha'], behind)} (보고만 · T436)")
        why = deploy_why_line(dep_sha, m["sha"], verdict)
        if why:
            print(f"  {why}")
        band = deploy_band_line(dep_sha, m["sha"])
        if band:
            print(f"  └ {band}")

    bad = []
    if calling > 0 and judged > limit:
        bad.append(f"빚 {calling}개가 {judged}분째 답을 못 받았다(한계 {limit}분"
                   + (" · 시각을 못 읽어 답의 나이로 잰 값이다)" if fallback else ")"))
        # ⛑ T497 — **«기다리는 빚» 과 «임자 없는 빚» 은 다른 것이고 처방도 다르다.**
        #   ⓐ 기전: `concurrency` 그룹은 «도는 것 1 + 기다리는 것 1» 만 두므로 **셋째 푸시가 오면 줄 서 있던 런을 죽인다**
        #      (`cancel-in-progress: false` 가 막는 것은 «도는 것을 자르는 것» 뿐이다 — `ci.yml:11~13` 의 주석 그대로).
        #   ⓑ ⚑ **T433 뒤로 그 구멍이 스스로 아물지 않는다.** 전에는 뒤따라온 문서 푸시도 유니티를 돌려서
        #      **밀려 죽은 코드 커밋을 같은 나무로 덮어** 줬다. 지금 문서 푸시는 유니티를 건너뛰므로(T433) 덮지 않는다.
        #      곧 T433 은 큐를 아끼면서 **«밀려 죽은 런이 저절로 갚아지던 길» 을 같이 닫았다** — 아무도 안 적어 둔 값이다.
        #   ⓒ 가르는 법(이 통엔 GitHub API 가 없다 · 실측 2026-09-12 런 1144 취소 → 1145 문서라 유니티 건너뜀):
        #      **빚이 한 런 길이보다 오래됐는데 끝 커밋이 CI 를 안 부른다** = 그 빚을 담을 런은 이미 사라졌다.
        #      (아직 도는 중이면 그 나이가 한계를 안 넘는다 — 그래서 이 줄은 ✗ 갈래 안에만 선다.)
        if tip_calls is False:
            print("· ⚠⚠ **이 빚은 «기다리는 중» 이 아니라 «임자가 없다»** — 그 빚을 담을 런이 이미 사라졌다"
                  "(줄 서 있다가 뒤 푸시에 밀려 cancelled) · 그리고 **그 뒤 커밋은 CI 를 안 부르므로 대신 갚아 주지 않는다**(T433).")
            print("  ⤷ 처방: **사람이 런을 한 번 띄운다**(Actions → CI → Run workflow · `main`). "
                  "안 띄우면 **다음 코드 푸시가 이 빚을 물려받고, 그 사람이 남의 빨강을 받는다**(T497).")
            print("  ⤷ ⚠ **띄운 직후에도 이 줄은 그대로 선다** — 이 통엔 GitHub API 가 없어 «지금 도는 런» 을 못 본다. "
                  "판정은 `origin/screens:meta.json` 이 움직여야 바뀐다(≈20분) — **두 번 띄우지 마라**.")
    if verdict != "success":
        bad.append(f"마지막 답이 «{verdict}» 다")
    if m.get("shots", 1) == 0:
        bad.append("그 런이 PNG 를 0장 만들었다(테스트가 시작조차 못 했을 수 있다)")
    if silent:
        # T433 2회차 — «빚» 과 다른 종류의 구멍이다. 빚은 «답을 기다리는 중» 이지만
        #   이것은 **아무도 안 기다리는 커밋**이다(런이 아예 안 섰다).
        for s in silent:
            print(f"· ⚠ 코드를 건드렸는데 건너뛰기 표식 탓에 런이 **안 섰다**: {s}")
        # T480 — 이 줄이 «본문» 만 가리키고 있었다. 2026-09-08~11 실측: 이렇게 걸린 코드 커밋 넷 중
        #   **셋은 제목에 일부러 박은 표식**이고(85180a16·44fe653e·b0a68908) 본문 인용은 하나뿐이다(5549d021).
        #   본문만 뒤지라고 하면 셋에 대해서는 찾을 것이 없어 읽는 사람이 «자가 틀렸나» 로 샌다.
        #   그 셋 중 85180a16 은 PlayMode 컴파일 고장을 고친 커밋이었다 — 가장 판정이 필요한 것이 제 손으로 껐다.
        bad.append(f"CI 가 아예 안 돈 코드 커밋 {len(silent)}개 — 메시지의 **제목이든 본문이든** 표식을 보라"
                   f"(일부러 박은 것도 인용한 것도 GitHub 은 같게 읽는다 · 위 «⚠» 줄)")

    stale = "" if fresh else " · ⚠ 낡은 ref 로 잰 값이다(T286 — 위 첫 줄)"
    if bad:
        print("✗ check_gate_age: " + " · ".join(bad) +
              " — 유니티 잡 로그의 **머리**를 보라(라이선스·러너는 꼬리에 안 나온다 · T283/T284) · 보고만(막지 않는다)" + stale)
    else:
        waited = f" · 가장 오래된 빚 {judged}분째" if calling > 0 and judged is not None else ""
        print(f"✓ check_gate_age: 마지막 답이 {age}분 전 · 빚 {calling}개{waited} · tests=success (보고만 · T285){stale}")
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
        # (빚, **가장 오래된 빚이 기다린 분**, tests, PNG, 기대) — 이름이 곧 그 갈래의 뜻이다
        # ⛳ T438 — 둘째 칸은 «마지막 답의 나이» 가 아니라 «빚이 기다린 시간» 이다. 그 둘은 T433 뒤로 다른 수다.
        (0, 600, "success", 41, "✓", "아무도 안 밀면 열 시간이 지나도 정상(조용한 새벽)"),
        (17, 115, "success", 41, "✗", "빚이 쌓였는데 답이 없다 = 2026-09-09 그 사고"),
        (3, 10, "success", 41, "✓", "빚이 있어도 방금 답했으면 정상"),
        # ⚑ 2026-09-11 02:3X 의 그 헛 경보 — 답은 61분 늙었는데 빚은 7분 전에 밀렸고 런이 이미 돌고 있었다.
        (1, 7, "success", 41, "✓", "⚑ 문서만 오가 답이 늙은 뒤 갓 밀린 코드 푸시 — 빚이 7분째면 정상(T438 이 고친 그 자리)"),
        (1, 61, "success", 41, "✗", "그 빚이 정말 61분째 기다리면 그때는 운다(고쳐도 잡을 것은 잡는다)"),
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

    # ⛳ T436 — «폰에 가 있는 것» 을 읽는 눈. 셋이 실제 gh-pages 머리 메시지 그대로다.
    #   ⚠ 액션이 메시지 꼬리에 «민 커밋» sha 를 하나 더 붙이므로 **첫 번째** 40자를 집어야 한다 —
    #     둘째를 집으면 «폰이 돌리는 소스» 가 아니라 «그때 main 머리» 를 말하게 되고, 그 둘은 늘 다르다.
    G1 = "12056fe20bd97e6edab5d462d5bb52ce9bf20c75"
    G2 = "b64bbdabbdfdfb97519bf69e06a83e6b709f9037"
    deploys = [
        ("실제 꼴 — 첫 sha 가 소스다(꼬리의 것은 액션이 붙인 «민 커밋»)",
         f"WebGL 배포 {G1} (마지막 초록 · T187 · 런 34545756886)\n\n{G2}", G1),
        ("sha 하나뿐이어도 그것이다", f"WebGL 배포 {G1} (마지막 초록 · T187 · 런 1)", G1),
        ("sha 가 없으면 None — 꼴이 바뀐 것이라 조용히 틀리지 않고 그렇게 적는다", "Deploy from GitHub Actions", None),
        ("빈 메시지", "", None),
        ("None 이어도 안 터진다", None, None),
        ("짧은 sha 는 안 집는다(40자만이 그 꼴이다)", "WebGL 배포 12056fe2 (짧게 적었다)", None),
    ]
    for why, msg, want in deploys:
        got = deploy_source(msg)
        ok = got == want
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} 소스={str(got)[:9]} (기대 {str(want)[:9]}) — {why}")

    # 그 사이를 **말로 옮기는** 자리 — 문턱이 없으므로 갈래는 넷뿐이고, 넷 다 사실이다.
    phrases = [
        ("소스를 못 집었으면 그 사실을 말한다", None, G1, 3, "안 적혀"),
        ("같은 커밋이면 폰이 최신이다", G1, G1, 0, "같은 커밋"),
        ("뒤처졌으면 몇 커밋인지 말한다", G1, G2, 4, "4커밋 뒤"),
        ("조상이 아니면(되돌림·가지) 그렇게 말한다", G1, G2, -1, "조상이 아니다"),
        ("셀 수 없으면 못 셌다고 말한다", G1, G2, None, "못 셌다"),
    ]
    for why, dep, green, behind, need in phrases:
        got = deploy_phrase(dep, green, behind)
        ok = need in got
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} «{got}» — {why}")

    # ⛳ T467 — «왜 뒤처졌나» 한 줄. 이 자가 갈라야 하는 것은 **«배포를 뒤져라» 와 «뒤질 것 없다»** 둘뿐이다.
    whys = [
        ("폰이 최신이면 안 적는다 — 뒤처지지 않았는데 까닭을 읽을 까닭이 없다", G1, G1, "success", None),
        ("뒤처졌는데 유니티 잡이 빨갛다 = 보낼 초록이 없는 것이다(뒤질 것 없다)", G1, G2, "failure", "보낼 초록이 없다"),
        ("유니티 잡이 초록인데 뒤처졌다 = 굽는 중이거나 아직 안 골랐다", G1, G2, "success", "굽고 있거나"),
        ("답을 못 읽었어도(«?») 초록은 아니므로 빨강 쪽으로 읽는다 — 조용한 것이 제일 나쁘다", G1, G2, "?", "보낼 초록이 없다"),
        ("소스를 못 집었으면 안 적는다 — 견줄 수가 없다", None, G2, "failure", None),
    ]
    for why, dep, green, verd, need in whys:
        got = deploy_why_line(dep, green, verd)
        ok = (got is None) if need is None else (got is not None and need in got)
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} 까닭={'(안 적음)' if got is None else need} — {why}")

    # ⚠ 그 줄도 **판정 밖**이어야 한다 — 이 자는 보는 자이지 막는 자가 아니다(결정 493·627·930).
    why_pure = "deploy_why_line" not in inspect.getsource(main).split("bad = []")[1]
    bad += 0 if why_pure else 1
    print(f"  {'✔' if why_pure else '✘'} 까닭 줄도 판정부(bad) 밖에만 있다 — 보고만이다(T467)")

    # ⛳ T451 — «평소 폭» 을 놓는 자리. 재는 것이 아니라 **언제 입을 여는가**를 잰다(소음이 되면 다음 사람이 지운다).
    bands = [
        ("폰이 최신이면 안 적는다 — 최신인데 «평소 폭» 을 읽을 까닭이 없다", G1, G1, False),
        ("뒤처졌으면 적는다 — 놀랄지 말지를 그 줄이 가른다", G1, G2, True),
        ("소스를 못 집었으면 안 적는다 — 견줄 수가 없다", None, G2, False),
        ("마지막 초록을 모르면 적는다 — 견줄 것이 없으니 폭이라도 있어야 한다", G1, None, True),
    ]
    for why, dep, green, want in bands:
        got = deploy_band_line(dep, green) is not None
        ok = got == want
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} 폭을 적는다={got} (기대 {want}) — {why}")

    # ⚠ 그 줄이 **판정을 안 건드리는가** — 이 자가 문턱으로 굳는 순간 결정 930 이 막은 그 자리가 된다.
    band_pure = "DEPLOY_BAND" not in inspect.getsource(main).split("bad = []")[1]
    bad += 0 if band_pure else 1
    print(f"  {'✔' if band_pure else '✘'} 평소 폭은 판정부(bad) 밖에만 있다 — 자이지 문턱이 아니다(T451)")

    # ⚠ 새 눈이 **원래 있던 눈을 멀게 하지 않는가** — `gh-pages` 를 위쪽 fetch 에 끼우면
    #   그 가지가 없는 클론에서 fetch 가 통째로 실패해 이 자가 «갔다 오지 못했다» 를 찍는다(성한 게이트가 사고로 보인다).
    body2 = inspect.getsource(refresh).replace(refresh.__doc__ or "", "")
    apart = "gh-pages" not in body2
    bad += 0 if apart else 1
    print(f"  {'✔' if apart else '✘'} refresh 는 gh-pages 를 안 당긴다 — 따로·되면 좋고 식으로 받는다(T436)")

    # ⛳ T438 — 시각을 읽는 두 조각. 여기서 틀리면 위 표가 아무리 옳아도 판정에 엉뚱한 수가 들어간다.
    stamps = [
        ("«Z» 로 끝나는 꼴(파이썬 3.10 아래 fromisoformat 이 못 읽는 그것)", "2026-09-11T02:25:59Z", True),
        ("«+00:00» 꼴 — git log %cI 가 주는 것", "2026-09-11T02:25:59+00:00", True),
        ("다른 타임존도 읽는다", "2026-09-11T11:25:59+09:00", True),
        ("못 읽는 글자는 None — 터지지 않는다", "어제쯤", False),
        ("빈 줄도 None", "", False),
        ("None 도 None", None, False),
    ]
    for why, s, want in stamps:
        got = parse_iso(s) is not None
        ok = got == want
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} 읽었다={got} (기대 {want}) — {why}")
    fut = minutes_since(datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(hours=3))
    ok = fut == 0
    bad += 0 if ok else 1
    print(f"  {'✔' if ok else '✘'} 미래로 적힌 시각 = {fut}분 (기대 0) — 시계가 어긋난 러너의 커밋이 «-180분째 기다린다» 가 되면 안 된다")
    ok = minutes_since(None) is None
    bad += 0 if ok else 1
    print(f"  {'✔' if ok else '✘'} 시각이 없으면 None — «0분» 이 아니다(0 으로 읽으면 빚이 영원히 안 늙는다)")

    # ⛳⛳ T438 — **진짜 저장소로** 낸다. 위 표는 내가 넣은 수를 내가 읽은 것이라
    #     «debt() 가 시각을 정말 집는가» 를 못 본다(T433 3회차가 바로 그 자리에서 걸렸다).
    import tempfile
    import shutil
    tmp = tempfile.mkdtemp(prefix="gate_age_selftest_")
    cwd = os.getcwd()
    try:
        os.chdir(tmp)
        def git(*a, **kw):
            subprocess.run(("git",) + a, capture_output=True, text=True, env=kw.get("env"))
        env = dict(os.environ, GIT_AUTHOR_NAME="t", GIT_AUTHOR_EMAIL="t@t",
                   GIT_COMMITTER_NAME="t", GIT_COMMITTER_EMAIL="t@t")
        git("init", "-q", "-b", "main")
        io.open("README.md", "w", encoding="utf-8").write("x\n")
        subprocess.run(["git", "add", "-A"], capture_output=True)
        subprocess.run(["git", "commit", "-q", "-m", "밑동"], capture_output=True, env=env)
        base = subprocess.run(["git", "rev-parse", "HEAD"], capture_output=True, text=True).stdout.strip()
        # 문서 커밋 하나를 **두 시간 전**으로, 코드 커밋 하나를 **지금**으로 — 실측 그 꼴이다.
        old = (datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(hours=2)).strftime("%Y-%m-%dT%H:%M:%S+00:00")
        os.makedirs("docs", exist_ok=True)
        io.open("docs/PROGRESS.md", "w", encoding="utf-8").write("문서\n")
        subprocess.run(["git", "add", "-A"], capture_output=True)
        subprocess.run(["git", "commit", "-q", "-m", "문서만"], capture_output=True,
                       env=dict(env, GIT_AUTHOR_DATE=old, GIT_COMMITTER_DATE=old))
        io.open("code.cs", "w", encoding="utf-8").write("// 코드\n")
        subprocess.run(["git", "add", "-A"], capture_output=True)
        subprocess.run(["git", "commit", "-q", "-m", "코드"], capture_output=True, env=env)
        git("branch", "-f", "___origin_main")     # debt() 가 보는 이름을 이 판에서 흉내 낸다
        real_sh = globals()["sh"]
        globals()["sh"] = lambda *a: real_sh(*[x.replace("origin/main", "___origin_main") for x in a])
        try:
            total, calling, silent, oldest, tip_calls = debt(base)
        finally:
            globals()["sh"] = real_sh
        wait = minutes_since(oldest)
        ok = (total, calling, silent) == (2, 1, []) and wait is not None and wait <= 2
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} 진짜 저장소: 커밋 {total}개 · 빚 {calling}개 · 가장 오래된 빚 {wait}분째 "
              f"(기대 2·1·0~2분) — **두 시간 전 문서 커밋이 빚의 나이를 늙히지 않는다**(T438 의 본론)")
        # ⛑ T497 ⓐ — **끝 커밋이 코드면 «기다리는 빚»** 이다(그 푸시가 제 런을 갖고 있다).
        ok = tip_calls is True
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} 끝 커밋이 코드 = 기다리는 빚(tip_calls={tip_calls} · 기대 True) — "
              f"이 갈래에서는 «임자 없다» 줄이 서면 안 된다")
        # ⛑ T497 ⓑ — **그 위에 문서 커밋 하나를 얹으면 «임자 없는 빚»** 이다(실측 런 1144→1145 그 꼴).
        io.open("docs/PROGRESS.md", "a", encoding="utf-8").write("뒤따라온 문서\n")
        subprocess.run(["git", "add", "-A"], capture_output=True)
        subprocess.run(["git", "commit", "-q", "-m", "문서만 둘째"], capture_output=True, env=env)
        git("branch", "-f", "___origin_main")
        globals()["sh"] = lambda *a: real_sh(*[x.replace("origin/main", "___origin_main") for x in a])
        try:
            total2, calling2, _, _, tip2 = debt(base)
        finally:
            globals()["sh"] = real_sh
        # ⚠ **빚 수는 그대로여야 한다** — 문서 커밋은 빚이 아니다. 그것까지 같이 봐야 «tip 만 보고 빚을 잃는» 고장을 잡는다.
        ok = tip2 is False and (total2, calling2) == (3, 1)
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} 끝 커밋이 문서 = **임자 없는 빚**(tip_calls={tip2} · 커밋 {total2}개 · 빚 {calling2}개 · "
              f"기대 False·3·1) — 밀려 죽은 런을 문서 푸시가 **안 갚는다**(T433 뒤 · T497)")
    finally:
        os.chdir(cwd)
        shutil.rmtree(tmp, ignore_errors=True)

    if bad:
        print(f"✗ check_gate_age --self-test: 갈래 {bad}건이 기대와 다르다 — 위 표의 판정 규칙을 보라")
        return 1
    print(f"✓ check_gate_age --self-test: 갈래 {len(cases) + len(holes) + len(calls) + len(deploys) + len(phrases) + len(bands) + len(whys) + len(stamps) + 7}개가 전부 기대대로 갈린다")
    return 0


if __name__ == "__main__":
    sys.exit(main())
