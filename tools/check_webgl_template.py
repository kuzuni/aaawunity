#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""배포 껍데기(WebGL 템플릿)가 «창을 채우고» «스모크가 알아볼 수 있는» 꼴인지 본다 (T196).

왜 있나 — 유니티 «Default» 템플릿은 생성된 `index.html` 에서 UA 로 갈라
`iPhone|iPad|iPod|Android` 만 화면을 채우고 **그 밖(모든 PC 브라우저)** 은 `canvas.style.width = "960px"` 로
못 박는다. 그래서 배포된 게임이 PC 에서 화면 한구석의 작은 띠로 그려졌다(실측: 창을 넷으로 바꿔도 캔버스는 늘 960×600).
T196 이 전용 템플릿으로 그 갈래를 없앴다.

**이 자가 지키는 것은 그 고침이 아니라 «되돌림» 이다.** 두 가지가 조용히 무너질 수 있고 둘 다 **로컬에서 안 보인다**:
  ⓐ `ProjectSettings.webGLTemplate` 가 `APPLICATION:Default` 로 돌아가거나 템플릿 폴더가 사라진다
     → 다음 빌드부터 다시 960×600 상자. 25분짜리 빌드를 굽고 나서야 안다.
  ⓑ 템플릿 안에서 **스모크(T60 · `tools/webgl_smoke.js`)가 문자열로 찾는 자리**의 이름이 바뀐다
     → 스모크가 «로딩 완료» 를 영영 못 보고, 배포 게이트가 **조용히 무뎌진다**(빨강 없이 약해지는 쪽이라 더 나쁘다).

그래서 이 자는 dotnet 잡(= `unity-test` → `build-webgl` → gh-pages 의 뿌리)에서 **막는다**.
결정 493 의 기준 그대로다 — «✅ 표시 누락» 같은 조율 결함은 보고만 하고, **배포가 깨지는 종류**는 뿌리에서 막는다.

쓰는 법:
  python3 tools/check_webgl_template.py             # 어긋나면 1
  python3 tools/check_webgl_template.py --self-test # 이 자가 실제로 잡는지
"""
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SETTINGS = os.path.join(ROOT, "ProjectSettings", "ProjectSettings.asset")
TEMPLATE_NAME = "KkomaKnight"
TEMPLATE = os.path.join(ROOT, "Assets", "WebGLTemplates", TEMPLATE_NAME, "index.html")

# 스모크가 **문자열로** 찾는 자리(`tools/webgl_smoke.js` 의 주입 정규식과 로딩 판정) — 이름이 바뀌면 T60 이 눈을 잃는다.
SMOKE_HOOKS = [
    (re.compile(r"\bvar\s+canvas\s*="), "`var canvas =` — 스모크가 `createUnityInstance(canvas, config,` 를 이 이름으로 찾는다"),
    (re.compile(r"\bvar\s+config\s*="), "`var config =` — 같은 이유"),
    (re.compile(r"createUnityInstance\s*\(\s*canvas\s*,\s*config\s*,"), "`createUnityInstance(canvas, config,` — 스모크가 이 꼴을 갈아 끼워 인스턴스를 잡는다"),
    (re.compile(r'#unity-loading-bar"\s*\)\s*\.style\.display\s*=\s*"none"'), '`#unity-loading-bar` 의 `style.display = "none"` — 스모크의 «로딩 완료» 신호'),
]

# «창을 채운다» 는 약속. 고정 px 로 못 박으면(=옛 Default 갈래) 그 순간 빨개진다.
FILLS = re.compile(r"#unity-canvas\s*\{[^}]*width:\s*100%[^}]*height:\s*100%", re.S)
PINNED = re.compile(r"canvas\.style\.(width|height)\s*=\s*[\"']\d+px[\"']")


# ⚠ 줄 주석은 `[^\n]*` 로 쓴다 — `re.S` 아래에서 `.*` 는 줄바꿈까지 먹어 **첫 `//` 한 줄이 파일 끝까지 삼킨다**
# (그러면 이 자가 «손잡이가 전부 없다» 고 잘못 외친다 · 실제로 그랬다).
COMMENT = re.compile(r"<!--.*?-->|/\*.*?\*/|^[ \t]*//[^\n]*", re.S | re.M)


def check(settings_text, html):
    """(어긋난 것들, 무엇을 봤나). 파일 내용만 받는 순수 함수 — 자기 검사가 같은 코드를 탄다."""
    bad = []
    # ⚠ 주석은 걷고 본다 — 이 템플릿의 머리 주석이 «옛 Default 는 `canvas.style.width = "960px"` 로 못 박는다» 를
    # **설명으로** 인용하고 있어, 안 걷으면 자가 제 설명문을 파손으로 읽는다(실제로 첫 실행에서 그랬다).
    body = COMMENT.sub("", html) if html is not None else None
    if "webGLTemplate: PROJECT:" + TEMPLATE_NAME not in settings_text:
        bad.append("ProjectSettings 의 `webGLTemplate` 가 `PROJECT:%s` 가 아니다 — 기본 템플릿으로 돌아가면 PC 에서 960×600 상자가 된다(T196)" % TEMPLATE_NAME)
    if html is None:
        bad.append("템플릿이 없다: Assets/WebGLTemplates/%s/index.html" % TEMPLATE_NAME)
        return bad, 0
    if not FILLS.search(body):
        bad.append("템플릿의 `#unity-canvas` 가 `width:100%` + `height:100%` 로 창을 채우지 않는다")
    m = PINNED.search(body)
    if m:
        bad.append("템플릿이 캔버스를 고정 px 로 못 박는다(`%s`) — 옛 Default 갈래로 되돌아간 꼴이다" % m.group(0))
    for pat, why in SMOKE_HOOKS:
        if not pat.search(body):
            bad.append("배포 스모크(T60)가 찾는 자리가 없다 — %s" % why)
    return bad, len(SMOKE_HOOKS)


def self_test():
    """T196 의 그 파손을 가짜 파일로 재현해 ⓐ 잡는지 ⓑ 멀쩡하면 조용한지 본다."""
    good_html = io.open(TEMPLATE, encoding="utf-8").read() if os.path.exists(TEMPLATE) else None
    if good_html is None:
        print("⛔ 자기 검사 실패 — 견본으로 쓸 진짜 템플릿이 없다: %s" % TEMPLATE)
        return 1
    good_set = "webGLTemplate: PROJECT:" + TEMPLATE_NAME

    bad, n = check(good_set, good_html)
    if bad:
        print("⛔ 자기 검사 실패 — 멀쩡한 짝을 걸었다(거짓 경고): %s" % bad[0])
        return 1

    cases = [
        ("ProjectSettings 되돌림", "webGLTemplate: APPLICATION:Default", good_html),
        ("템플릿 삭제", good_set, None),
        ("고정 px 갈래 부활", good_set, good_html + '\n<script>canvas.style.width = "960px";</script>\n'),
        ("스모크 손잡이 이름 변경", good_set, good_html.replace("createUnityInstance(canvas, config,", "createUnityInstance(cv, cfg,")),
    ]
    for name, st, ht in cases:
        if not check(st, ht)[0]:
            print("⛔ 자기 검사 실패 — «%s» 를 못 잡았다" % name)
            return 1

    print("✓ check_webgl_template --self-test: 되돌림 네 가지를 다 잡고(설정·삭제·고정 px·손잡이 이름) 멀쩡하면 조용하다")
    return 0


def main(argv):
    if "--self-test" in argv:
        return self_test()
    try:
        settings_text = io.open(SETTINGS, encoding="utf-8").read()
    except OSError as e:
        print("⛔ ProjectSettings 를 못 읽는다: %s" % e)
        return 1
    html = io.open(TEMPLATE, encoding="utf-8").read() if os.path.exists(TEMPLATE) else None

    bad, n = check(settings_text, html)
    if not bad:
        print("✓ check_webgl_template: 전용 템플릿이 창을 채우고 스모크 손잡이 %d개가 그대로다 (T196)" % n)
        return 0
    print("⛔ 배포 껍데기가 어긋났다 — **다음 빌드가 PC 에서 960×600 상자로 나가거나 배포 스모크가 눈을 잃는다**:")
    for b in bad:
        print("  · " + b)
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
