#!/usr/bin/env python3
"""표 행 이름 ↔ 화면 이름표(`UiKit.Tag`) 대조 — **알리기만 한다**(T279).

왜 있나 — §5 의 채점(`tools/ui_score.py`)은 `docs/ref-layout.md` 의 행을 **이름 그대로**
화면 이름표와 맞춘다(결정 592). 이름이 한 글자만 달라도 그 행은 «요소 없음 ✗0» 이 되는데,
**점수만 보면 «자리가 틀렸다» 와 구별이 안 된다**(T266 3단계에서 새 표 ㊼ 이 그 꼴로 여섯 행을 잃었다 · 결정 756).
그래서 이 자는 **layout.json 없이**(소스와 표만 읽고) 돌아 «맞댈 이름조차 없는 행» 을 먼저 찍는다.

막지 않는다(§2·결정 493) — 이름이 어긋나도 **배포된 게임은 멀쩡하다**. 게다가 «표에는 있는데
아직 안 만든 요소»(회귀 자 표·미완성 화면)가 흔해 막는 자로 세우면 빨간 채로 산다.
어느 갈래로 나가든 **마지막 줄은 «[표이름] 요약 …»** 이고 종료 코드는 늘 0 이다.

⚠ **한쪽만 잰다**(표 행 → 이름표). 반대쪽(이름표 → 표 행)은 못 잰다 — «화면 → 소스 파일» 짝이
   이 레포 어디에도 없어서(있는 것은 «화면 → 표 기호» 인 `SCREENS` 뿐이다) 자가 지어내야 하는데,
   절 3항이 바로 그것을 금한다. 이름표는 표에 없는 조각에도 많이 붙어 있어 반대쪽은 어차피 소음이다.

이름표를 «어떻게» 뜯는가 — **여기가 이 자의 값어치다**(T279 재는 회차 실측):
문자열만 정규식으로 긁으면 어긋남이 **28건**으로 보이는데 그중 20건이 헛것이었다. 진짜는 **8건**이다.
헛것의 정체 넷을 다 풀어 준다.
  ⓐ 중첩 호출  `UiKit.Tag(BuildColumn(rt, "SideL", …), "좌 사이드 아이콘 열(1개)")` — 첫 인자에 쉼표가 있다 → 괄호 균형으로 자른다.
  ⓑ 삼항       `UiKit.Tag(col, name == "Col:free" ? "무료 열(파랑)" : …)` — 갈래마다 이름이다 → 인자 안 문자열을 다 받는다.
  ⓒ 이어붙이기 `UiKit.Tag(c, "특전 카드 " + (i + 1))` — 번호가 붙는다 → **앞머리**로 받아 그 앞머리로 시작하는 행을 통과시킨다.
  ⓓ 감싼 자    `Rule(root, "RewardLineTop", Layout.RwLineTop, "위 노란 줄")` → 그 메서드 속 `UiKit.Tag(line, tag)` —
               «제 매개변수를 이름표로 넘기는» 메서드를 찾아 **한 걸음**만 따라가 부름터의 그 자리 문자열을 받는다.
그리고 이름은 늘 **두 번째 인자**다(`Tag(t, name, textBounds)` · `TagGroup(host, name, members…)`) —
마지막 인자로 읽으면 `TagGroup` 이 통째로 샌다(재는 회차에서 이 실수로 «없는 행» 이 8 → 23 으로 부풀었다).

월드 행 — 캔버스 밖이라 이름표가 없고 `BattleWorld` 가 표의 이름 그대로 사전에 담아 돌려준다
(`d["지면(길) 띠"] = …`). 그래서 그 사전 키도 이름의 출처로 센다.

⛔ **«아직 만들 수 없는 요소» 는 안 센다**(3회차) — 표의 **비고**에 `⛔` 가 있으면 그 행은 이름을 적기만 하고
   요약에서 뺀다. 까닭: 그런 행은 **코드로 고칠 수가 없어**(기능 자체가 없다 · 주인 답을 기다린다) 요약에
   눌러앉는데, 그러면 «초록이면 어긋남 0» 이 영영 성립하지 않고 다음 사람이 이 줄을 통째로 무시한다 —
   알리기만 하는 자가 죽는 방식이 정확히 그것이다. 실례: 표 ㊺ 의 «원형 버튼 AUTO·천사·악마» 는
   우리 전투에 그 기능이 **없고**, T240 이 «주인께 물을 것» 으로 올려 두었다(결정 777).
   ⚠ **숨기는 것이 아니다** — 이름은 따로 한 줄로 그대로 찍고 요약 꼬리에 건수를 붙인다.
   ⚠ 표시는 «비고» 에만 단다(행 이름·수치는 안 건드린다) — `ui_score` 채점은 이 표시를 안 보므로
      그 행은 여전히 «요소 없음 ✗0» 이다(점수를 깎는 것과 «고칠 수 있는가» 는 다른 물음이다).

⚑ 워커가 읽는 법 — **dotnet 잡 꼬리 50줄**을 당겨 «[표이름] 요약» 줄부터 본다.
   이 단계는 그 잡의 **마지막**이다(결정 769) — 처음엔 다른 «보고만» 자들 옆(위쪽)에 뒀는데
   뒤에 오는 aaaw 체크아웃(git 잡음 ~40줄)·아티팩트 업로드가 이 줄을 **꼬리 96줄 밖으로** 밀어냈다
   (run 606 실측). 찍히기는 하는데 아무도 못 읽는 줄이었다. `ci.yml` 에서 이 단계 뒤에 무엇을 붙이면
   같은 일이 다시 난다 — T239 가 유니티 잡에서 먼저 데인 자리다(결정 667).

쓰는 법:
    python3 tools/check_table_tags.py            # 보고(늘 0)
    python3 tools/check_table_tags.py --self-test
"""
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ui_score  # noqa: E402 — 표 ↔ 화면 짝(SCREENS)과 표 읽기(parse_ref)를 그대로 빌린다

TAG = '[표이름]'
SRC = os.path.join(ROOT, 'Assets', 'Scripts')
STR = re.compile(r'"((?:[^"\\]|\\.)*)"')
CALL = re.compile(r'\b(?:UiKit\.)?(?:Tag|TagGroup)\s*\(')
#           «제 매개변수를 이름표로 넘기는» 메서드 머리 — `static void Rule(RectTransform root, string name, Layout.R r, string tag)`
SIG = re.compile(r'^\s*(?:public|private|protected|internal|static|\s)*[\w<>,\[\]\.]+\s+(\w+)\s*\(([^)]*)\)\s*$', re.M)
DICT_KEY = re.compile(r'\w\["((?:[^"\\]|\\.)*)"\]\s*=')     # 월드 행 — BattleWorld 의 사전 키
MIN_PREFIX = 4      # 이보다 짧은 앞머리는 아무 행이나 삼킨다 — 안 받는다
NOT_YET = '⛔'      # 표의 «비고» 에 이 표가 있으면 «아직 만들 수 없는 요소» — 이름을 적되 어긋남으로 안 센다(3회차)


def _skip_string(s, i):
    """s[i] == '"' 일 때 닫는 따옴표 **다음** 자리를 돌려준다."""
    i += 1
    while i < len(s):
        if s[i] == '\\': i += 2; continue
        if s[i] == '"': return i + 1
        i += 1
    return i


def call_args(s, i):
    """s[i] == '(' 인 호출의 인자들을 괄호 균형으로 잘라 돌려준다(문자열 속 괄호·쉼표는 무시)."""
    depth = 0; parts = []; cur = ''; j = i
    while j < len(s):
        c = s[j]
        if c == '"':
            e = _skip_string(s, j); cur += s[j:e]; j = e; continue
        if c in '([': depth += 1
        elif c in ')]':
            depth -= 1
            if depth == 0: parts.append(cur[1:]); return parts
        if c == ',' and depth == 1: parts.append(cur[1:] if not parts else cur); cur = ''; j += 1; continue
        cur += c; j += 1
    return parts


def _name_of(parts):
    """이름표 호출의 «이름» 인자 = 늘 두 번째."""
    return parts[1].strip() if len(parts) > 1 else ''


def _wrappers(text):
    """{메서드 이름: 이름표로 넘어가는 인자 자리(0부터)} — `UiKit.Tag(x, tag)` 의 `tag` 가 제 매개변수인 메서드."""
    out = {}
    for m in SIG.finditer(text):
        name, params = m.group(1), m.group(2)
        if name in ('Tag', 'TagGroup'): continue
        names = []
        for p in params.split(','):
            p = p.strip()
            if p: names.append(p.split('=')[0].strip().split()[-1])
        if not names: continue
        body = text[m.end():m.end() + 4000]      # 메서드 머리 바로 뒤 몸통 — 한 걸음만 본다
        for c in CALL.finditer(body):
            arg = _name_of(call_args(body, c.end() - 1))
            if arg in names: out.setdefault(name, names.index(arg))
    return out


def collect_tags(src=SRC):
    """(딱 그 글자 집합, 앞머리 집합) — 화면 소스가 다는 이름표 전부."""
    files = []
    for base, _, names in os.walk(src):
        for f in sorted(names):
            if f.endswith('.cs'):
                try: files.append(open(os.path.join(base, f), encoding='utf-8').read())
                except OSError: pass
    exact, prefix = set(), set()

    def take(expr):
        lits = STR.findall(expr)
        if not lits: return
        exact.update(lits)
        if not re.fullmatch(r'"(?:[^"\\]|\\.)*"', expr.strip()):     # 삼항·이어붙이기 → 앞머리로도 받는다
            prefix.update(L for L in lits if len(L) >= MIN_PREFIX and not L.startswith('('))

    wrappers = {}
    for text in files: wrappers.update(_wrappers(text))
    for text in files:
        for m in CALL.finditer(text):
            take(_name_of(call_args(text, m.end() - 1)))
        for name, idx in wrappers.items():                            # ⓓ 감싼 자 — 부름터의 그 자리
            for m in re.finditer(r'\b' + re.escape(name) + r'\s*\(', text):
                parts = call_args(text, m.end() - 1)
                if len(parts) > idx: take(parts[idx].strip())
        exact.update(DICT_KEY.findall(text))                           # 월드 행(BattleWorld)
    return exact, prefix


def known(name, exact, prefix):
    return name in exact or any(name.startswith(p) for p in prefix)


def rows_by_table(tables=None):
    """{표 기호: [(행 이름, 아직 없는 요소인가)]} — SCREENS 로 화면과 짝지어진 표만(짝 없는 표는 조용히 건너뛴다 · 절 3항).

    `tables` 를 주면 그것을 읽는다(자기 검사가 가짜 표를 먹인다 · 안 주면 `docs/ref-layout.md`)."""
    if tables is None: tables = ui_score.parse_ref()
    out, seen = {}, set()
    for screen in sorted(ui_score.SCREENS):
        sym, only, exclude = ui_score.SCREENS[screen]
        t = tables.get(sym)
        if not t: continue
        for name, ref, note in t['rows']:
            if '(참고·컨테이너)' in name: continue
            if name.startswith('~~'): continue                 # 지운 요소(취소선)는 이름표가 없는 것이 맞다
            # 값이 h 하나뿐인 행은 **요소가 아니라 거리**다(«상단 스탯 줄 ↔ 카드1 간격 — — — 26.5» · ⚑ T154).
            # 이런 행은 붙일 조각 자체가 없어 이름표가 영영 안 생긴다 — 세면 고칠 수 없는 어긋남이 늘 한 건 남는다.
            if ref[3] is not None and all(v is None for v in ref[:3]): continue
            if only and not name.startswith(only): continue
            if exclude and name.startswith(exclude): continue
            if (sym, name) in seen: continue
            seen.add((sym, name))
            out.setdefault(sym, []).append((name, NOT_YET in note))
    return tables, out


def report(out=print):
    exact, prefix = collect_tags()
    tables, per_table = rows_by_table()
    total = missing = notyet = 0
    lines, notyet_lines = [], []
    for sym in sorted(per_table, key=lambda s: -sum(1 for n, _y in per_table[s] if not known(n, exact, prefix))):
        rows = per_table[sym]
        total += len(rows)
        title = tables[sym]['title'][:40]
        bad = [n for n, yet in rows if not yet and not known(n, exact, prefix)]
        pend = [n for n, yet in rows if yet and not known(n, exact, prefix)]
        missing += len(bad); notyet += len(pend)
        if bad:
            lines.append(f'{TAG} {sym} «{title}» {len(bad)}/{len(rows)}행 — ' + ' · '.join(bad))
        if pend:
            notyet_lines.append(f'{TAG} ({NOT_YET} 아직 없는 요소 · 안 센다) {sym} {len(pend)}건 — ' + ' · '.join(pend))
    for line in lines + notyet_lines: out(line)
    tail = f' · {NOT_YET} 아직 없는 요소 {notyet}건은 안 셌다(위에 이름을 적었다)' if notyet else ''
    if missing:
        out(f'{TAG} 요약 {missing}건 / 재는 행 {total} — 이 이름의 행은 §5 에서 «요소 없음 ✗0» 이다(자리 문제가 아니다){tail}')
    else:
        out(f'{TAG} 요약 0건 / 재는 행 {total} — 표 행 이름과 화면 이름표가 다 맞물린다{tail}')
    return missing


# ── 자기 검사 ────────────────────────────────────────────────────────────────
SELF_SRC = '''
class A {
    void Build() {
        UiKit.Tag(BuildColumn(rt, "SideL", Layout.X), "좌 사이드 아이콘 열(1개)");     // ⓐ 중첩
        UiKit.Tag(col, name == "Col:free" ? "무료 열(파랑)" : "유료 1 열(주황)");       // ⓑ 삼항
        UiKit.Tag(c, "특전 카드 " + (i + 1));                                          // ⓒ 이어붙이기
        UiKit.TagGroup(grp, "좌 슬롯열(3칸)", a.Root, b.Root, c.Root);                 // 이름은 두 번째 인자
        UiKit.Tag(box, "머리글", true);                                                 // 세 번째 인자가 있어도 그대로
        Rule(root, "RewardLineTop", Layout.RwLineTop, "위 노란 줄");                   // ⓓ 감싼 자의 부름터
        d["지면(길) 띠"] = new[] { 0f, 1f };                                            // 월드 행
    }
    static void Rule(RectTransform root, string name, Layout.R r, string tag)
    {
        var line = UiKit.Rect(root, name); UiKit.Tag(line, tag);
    }
}
'''


def self_test():
    import tempfile
    ok = True
    with tempfile.TemporaryDirectory() as d:
        open(os.path.join(d, 'A.cs'), 'w', encoding='utf-8').write(SELF_SRC)
        exact, prefix = collect_tags(d)
    for label, name in [('ⓐ 중첩', '좌 사이드 아이콘 열(1개)'), ('ⓑ 삼항', '무료 열(파랑)'),
                        ('ⓑ 삼항(반대 갈래)', '유료 1 열(주황)'), ('두 번째 인자', '좌 슬롯열(3칸)'),
                        ('세 번째 인자', '머리글'), ('ⓓ 감싼 자', '위 노란 줄'), ('월드 행', '지면(길) 띠')]:
        hit = known(name, exact, prefix); ok &= hit
        print(f'{label} «{name}» —', 'OK' if hit else '실패')
    hit = known('특전 카드 3', exact, prefix); ok &= hit
    print('ⓒ 이어붙이기 «특전 카드 3» —', 'OK' if hit else '실패')
    miss = not known('있지도 않은 행 이름', exact, prefix); ok &= miss
    print('없는 이름은 없다고 한다 —', 'OK' if miss else '실패')
    bad = known('좌', exact, prefix); ok &= not bad      # 짧은 앞머리가 아무 행이나 삼키면 안 된다
    print(f'짧은 앞머리(<{MIN_PREFIX}자)는 안 삼킨다 —', 'OK' if not bad else '실패')
    # ⛔ 표시(3회차) — 가짜 표를 먹여 «안 센다 · 그러나 이름은 찍는다» 를 둘 다 본다.
    ref4 = [1.0, 2.0, 3.0, 4.0]
    fake = {'㊺': {'title': '가짜 표', 'rows': [('있는 요소', ref4, '보통 비고'),
                                               ('없는 요소', ref4, '뭐라뭐라'),
                                               ('아직 못 만드는 요소', ref4, f'{NOT_YET} 아직 없다 · 주인 답 대기')]}}
    rows = rows_by_table(fake)[1]['㊺']
    ok &= rows == [('있는 요소', False), ('없는 요소', False), ('아직 못 만드는 요소', True)]
    print(f'{NOT_YET} 는 비고에서만 읽는다 —', 'OK' if rows[2][1] and not rows[1][1] else '실패')
    sink = []
    n = report(sink.append); ok &= isinstance(n, int) and sink and sink[-1].startswith(f'{TAG} 요약')
    print('마지막 줄은 늘 «요약» —', 'OK' if sink and sink[-1].startswith(f'{TAG} 요약') else '실패')
    print('✓ check_table_tags 자기 검사 통과' if ok else '✗ 자기 검사 실패')
    return 0 if ok else 1


if __name__ == '__main__':
    if '--self-test' in sys.argv[1:]:
        sys.exit(self_test())
    try:
        report()
    except Exception as e:      # noqa: BLE001 — 무엇이 터지든 잡을 빨갛게 하지 않는다(알리기만 하는 자다)
        print(f'{TAG} 요약 — 대조를 못 했다(런 판정과 무관하다): {type(e).__name__} {str(e)[:200]}')
    sys.exit(0)
