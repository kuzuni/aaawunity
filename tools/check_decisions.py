#!/usr/bin/env python3
"""«워커 결정 기록» 번호가 겹치는지 보는 자 (T131).

왜 있나 — 워커 여럿이 30분 안에 같은 파일을 미는데, 번호는 각자 «지금 제일 큰 것 + 1» 로 고른다.
그래서 fetch 한 뒤 남이 먼저 밀면 같은 번호가 둘이 된다(2026-09-07 하루에만 341·342·344·345·346 이
각각 둘씩 · 전례 결정 207~210 · 336). 이 기록은 **번호로 서로를 가리키는 문서**라 가리키는 곳이 둘이면
«결정 344 대로» 가 무슨 뜻인지 알 수 없게 된다.

쓰는 법 (커밋 «직전» 에 · ROUTINE §3 게이트 목록):
  python3 tools/check_decisions.py          # 겹치면 1 로 끝난다(겹친 번호와 줄을 찍는다)
  python3 tools/check_decisions.py --next   # 다음에 쓸 번호 하나만 찍는다(= 지금 제일 큰 것 + 1)

규약(ROUTINE §1) — 겹쳤을 때 **늦게 push 한 쪽이 옮긴다**. 누가 늦었는지는
  git log -1 --format=%cI -S"<번호>. **<그 줄 첫 낱말>" -- docs/PROGRESS.md
로 각 줄이 처음 실린 커밋 시각을 재서 가린다. 옮길 때는 본문뿐 아니라 그 번호를 가리키는
docs/·Assets/ 의 «결정 N» 참조도 같이 옮긴다(이 자는 참조까지는 못 센다 · 사람이 본다).
"""
import re
import sys

DOC = "docs/PROGRESS.md"
HEAD = "## 워커 결정 기록"
NUM = re.compile(r"^(\d+)\. \*\*")

# 동결선 — 이 번호 «미만» 의 겹침은 세기만 하고 실패로 보지 않는다(T131 · 결정 356).
# 왜: 자를 처음 대 보니 80~336 에 40쌍 넘는 옛 겹침이 있었다. 그것들은 이미 수십 개의
# 커밋 메시지·코드 주석이 그 번호로 가리키고 있어(예: 어느 커밋이든 게이트 줄의 «결정 143»)
# 지금 와서 옮기면 그 참조가 전부 틀린 곳을 가리키게 된다 — 고치는 것이 더 나쁘다.
# 그래서 옛것은 «맥락으로 읽는다» 로 두고, 이 선부터는 겹치지 않게 지킨다.
FROZEN_BELOW = 341


def entries(path=DOC):
    """(번호, 줄번호, 줄 앞머리) 목록 — «워커 결정 기록» 절 안의 «N. **…» 줄만."""
    out = []
    inside = False
    with open(path, encoding="utf-8") as f:
        for i, line in enumerate(f, 1):
            if line.startswith("## "):
                inside = line.startswith(HEAD)
                continue
            if not inside:
                continue
            m = NUM.match(line)
            if m:
                out.append((int(m.group(1)), i, line[:90].rstrip()))
    return out


def main(argv):
    try:
        rows = entries()
    except OSError as e:
        print("읽기 실패: %s" % e)
        return 2
    if not rows:
        print("«%s» 절에서 «N. **…» 줄을 하나도 못 찾았다 — 형식이 바뀌었는지 본다" % HEAD)
        return 2

    if "--next" in argv:
        print(max(n for n, _, _ in rows) + 1)
        return 0

    seen = {}
    for n, ln, head in rows:
        seen.setdefault(n, []).append((ln, head))
    dup = {n: v for n, v in seen.items() if len(v) > 1}
    old = sorted(n for n in dup if n < FROZEN_BELOW)
    new = sorted(n for n in dup if n >= FROZEN_BELOW)
    nxt = max(seen) + 1
    if old:
        print("· 옛 겹침 %d쌍(%d 미만 · 동결 · 맥락으로 읽는다): %s"
              % (len(old), FROZEN_BELOW, ", ".join(str(n) for n in old)))
    if new:
        print("결정 번호가 겹친다 — 늦게 push 한 쪽이 옮긴다(ROUTINE §1 · 참조 «결정 N» 도 같이):")
        for n in new:
            print("  %d 이 %d 곳:" % (n, len(dup[n])))
            for ln, head in dup[n]:
                print("    %s:%d  %s" % (DOC, ln, head))
        print("다음에 쓸 번호: %d" % nxt)
        return 1
    print("✓ check_decisions: 결정 %d개 · %d 이상 번호 겹침 0 (다음 번호 %d)" % (len(rows), FROZEN_BELOW, nxt))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
