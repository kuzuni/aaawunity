#!/usr/bin/env python3
"""UI 비평 «표 점수» (T46 · ROUTINE §5) — docs/ref-layout.md 의 표 ↔ ui-screens/layout.json 대조.

사용:
  python3 tools/ui_score.py <화면> [layout.json]      # 화면 = 01_lobby · 02_battle · 06_gear … (docs/ref 번호 이름 · layout.json 의 키)
  python3 tools/ui_score.py --all [layout.json]       # layout.json 에 있는 화면 전부 요약
layout.json 을 안 주면 ui-screens/layout.json → 없으면 `git show origin/screens:layout.json` (git fetch origin screens 먼저).

판정(§5 · T46.4): 행마다 x·y·w·h 가 전부 ±3%p 안이면 1점 · 하나라도 3~6%p 면 0.5점 · 그 밖(6%p 초과 · 요소 없음)은 0점.
  표 점수 = 10 × 합 ÷ 행 수 (소수 1자리). «(참고·컨테이너)» 행은 세지 않는다.
  ref 에 x 나 w 가 없는 «월드·부분 행»(② 지면 띠 · 발밑 y · 캐릭터 높이 · 바 폭 …)은 layout.json 에 값이 있을 때만 센다(없으면 «측정 없음(월드)» 로 표시만 — 하니스가 캔버스 밖을 못 재므로 · T47 이 BattleWorld 에서 잰다).
  ref 값이 «—» 인 축은 비교하지 않는다(있는 축만).
출력 = 행별 «ref / 게임 / 차 / 판정» 마크다운 표 + 표 점수 + «다음 고칠 것»(0·0.5 행 · 큰 차부터). PROGRESS 점수판에 그대로 붙인다.

표 찾기: ①~⑦ 은 아래 SCREENS 로 고정 · 새 화면 표는 «## ⑨ <화면> — `NN_name.jpg`» 처럼 제목의 백틱 파일명(NN_ 접두)으로 찾는다(⑧ 공통 표는 화면이 아니다).
"""
import json, os, re, subprocess, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
REF = os.path.join(ROOT, 'docs', 'ref-layout.md')
DEFAULT_LAYOUT = os.path.join(ROOT, 'ui-screens', 'layout.json')

# 화면(layout.json 키 · docs/ref 번호 이름) → (표 번호 기호, 행 필터: 이름이 이 접두로 시작하는 행만/제외)
SCREENS = {
    '01_lobby': ('①', None, None),
    '02_battle': ('②', None, None),
    '03_battle_enemy': ('②', None, None),
    '06_gear': ('③', None, None),
    '07_gear_detail': ('④', None, None),
    '09_shop_1': ('⑤', None, '(뽑기 화면)'),      # 상점(1) = «(뽑기 화면)» 행 제외
    '10_shop_2': ('⑤', '(뽑기 화면)', None),      # 상점(2) = «(뽑기 화면)» 행만
    '08_gear_fuse': ('⑥', None, None),
    '04_perks': ('⑦', None, '(인포 팝업)'),       # 선택창 = «(인포 팝업)» 행 제외
    '05_perks_list': ('⑦', '(인포 팝업)', None),  # 인포 팝업 = «(인포 팝업)» 행만
    # T213 — 이벤트 팝업 둘은 ㊱ 한 표를 쓰고 행 이름 앞머리로 갈린다.
    # ⚠ 이름으로 표를 찾는 갈래(`find_table` 아래쪽)는 이 둘을 **못 가른다** — 둘 다 접두가 «ev_» 라 같은 표를 집는다.
    #    그래서 여기 명시한다(⑤·⑦ 과 같은 방식). 표가 «레퍼런스 그림 없는 회귀 자» 인 것은 ㊱ 머리에 적혀 있다.
    'ev_devil': ('㊱', '(악마)', None),
    'ev_angel': ('㊱', '(천사)', None),
    # T219 2단계 — 결과 팝업 셋. 승리 둘(`res_win`·`res_win_last`)은 **글자만 다르고 자리가 같아** 한 표(㊲)를 쓰고,
    # 패배는 조각도 구도도 달라 표를 따로 둔다(㊳). 표를 나눈 덕에 이름표에 앞머리를 안 박아도 된다(㊱ 은 한 표라 «(악마)»·«(천사)» 가 필요했다).
    'res_win': ('㊲', None, None),
    'res_win_last': ('㊲', None, None),
    'res_lose': ('㊳', None, None),
    # T274 — 같은 팝업의 «부활권» 갈래(`canRevive: true`). **표를 ㊳ 과 나눈 까닭**: 한 표에 담으면
    # 부활 두 행이 `res_lose` 쪽에서 «요소 없음 0점» 이 되어 그 표가 영구히 8.6 에 박힌다 —
    # «흔들렸다» 가 아니라 «그 갈래를 안 찍는다» 인데도 그렇다(결정 724). 화면이 둘이라 표도 둘이다.
    'res_lose_revive': ('㊽', None, None),
    # T219 3단계 — 남은 다섯. **다섯 다 여기 적는다**: 이름으로 표를 찾는 갈래는 «ev_» 로 시작하는 화면을
    # 서로 못 가른다(위 ㊱ 주석과 같은 함정 — 실제로 `ev_devil_gift`·`ev_ad` 가 둘 다 ㊴(휴식)로 붙어
    # 2.1점·0.0점이 나왔다. «표가 틀린» 것이 아니라 «표를 잘못 찾은» 것이라 점수만 보면 원인을 못 읽는다).
    'ev_rest': ('㊴', None, None),
    'ev_devil_gift': ('㊵', None, None),
    'ev_ad': ('㊶', None, None),
    # ⚠ `27_toast` 의 layout 에는 **로비 이름표 13개가 같이** 들어 있다(로비 위에 뜬 토스트라서).
    #    ㊷ 에는 «토스트» 것 둘만 두었다 — 로비 몫은 ① 이 이미 재고 있고, 여기서 또 재면 로비가 바뀔 때 이 표까지 흔들린다.
    '27_toast': ('㊷', None, None),
    '28_confirm_reset': ('㊸', None, None),
    # T241 — 공통 «리워드» 획득 팝업(주인 레퍼런스 35). 이름으로 표를 찾는 갈래도 «35_reward» → «`35_reward_popup`» 을
    #        못 집는다(제목의 파일명이 `35_reward_popup` 이라 접두가 다르다) — ㊱ 이 밟은 그 함정이라 여기 적는다.
    '35_reward': ('㊹', None, None),
    # T240 1항 — PvP 인게임(주인 레퍼런스 33). 표 ㊺ 의 열 행 중 «모래 마당» 은 월드(캔버스 밖)라 이름표가 없고,
    #            «원형 버튼 셋» 은 아직 그 기능 자체가 우리 전투에 없다 — 둘 다 «—»(못 잼)로 빠진다.
    '33_pvp_battle': ('㊺', None, None),
    # T266 — 시즌 패스(주인 레퍼런스 19). 이름으로 찾는 갈래도 «19_» 로 이 표를 집기는 하지만,
    #        그 갈래는 «어느 표가 먼저 오는가» 에 기대는 것이라 표가 늘면 조용히 바뀐다(㊱·㊴·㊹ 이 세 번 데인 자리).
    '19_pass': ('㊼', None, None),
    # T267 — 상자 «확률 정보» 팝업(주인 레퍼런스 36·37). 37 은 36 의 스크롤 다른 구간이라 **같은 표**를 쓴다(지시서 5항).
    '36_box_rates': ('㊾', None, None),
    '37_box_rates': ('㊾', None, None),
    # T267 6단계 — 확률 팝업 안 «아이템 세부» 팝업(주인 레퍼런스 38). 표 ㊿ 는 표 ④ 의 «버튼 없는 갈래» 라
    #   행 이름이 ④ 와 **같다** — 이름으로 표를 찾는 갈래에 맡기면 «38_» 접두로 못 찾고 ④ 로도 안 간다.
    '38_box_item_detail': ('㊿', None, None),
}
PASS, HALF = 3.0, 6.0

def num(s):
    s = s.strip().strip('*').strip()
    if s in ('', '—', '-', '–'): return None
    m = re.match(r'^-?\d+(\.\d+)?', s)
    return float(m.group(0)) if m else None

def parse_ref():
    """{기호: {'title': 제목줄, 'rows': [(이름, [x,y,w,h] or None들, 비고)]}} — 요소/x/y/w/h/비고 6열 표만."""
    tables = {}; cur = None; in_table = False
    for line in open(REF, encoding='utf-8'):
        # T213 — 번호가 ㉟ 을 넘겼다(㊱ 부터). 옛 문자 범위는 ㉟ 까지만 받아 **새 표를 조용히 못 본다** —
        #        표를 세워도 «표가 없다» 고 하고 그 화면은 영영 «—» 다(그 침묵이 이 작업을 부른 자리다).
        m = re.match(r'^## ([①-⑳㉑-㉟㊱-㊿])\s*(.*)$', line)
        if m:
            cur = m.group(1); tables[cur] = {'title': m.group(2).strip(), 'rows': []}; in_table = False; continue
        if line.startswith('## '): cur = None; in_table = False; continue   # 기호 없는 절(«⚑ U01 회차 정정» 등)은 표가 아니다 — 마지막 표에 그 절의 표 행이 섞이던 것을 막는다(T44)
        if cur is None: continue
        if line.startswith('|'):
            cells = [c.strip() for c in line.strip().strip('|').split('|')]
            if len(cells) < 5: continue
            if cells[0] in ('요소',) or set(cells[1]) <= set('-: '): in_table = True; continue
            if not in_table: continue
            name = re.sub(r'\s*\((T\d+)\)\s*$', '', cells[0].strip('*').strip())   # «플레이어 높이 (T159)» → 이름만
            name = name.replace('**', '').strip()
            vals = [num(cells[i]) for i in range(1, 5)]
            tables[cur]['rows'].append((name, vals, cells[5] if len(cells) > 5 else ''))
        elif line.strip() == '' and in_table:
            in_table = False
    return tables

def find_table(tables, screen):
    if screen in SCREENS:
        sym, only, exclude = SCREENS[screen]
        return sym, tables.get(sym), only, exclude
    key = screen.split('_')[0] + '_'
    for sym, t in tables.items():
        if re.search(r'`' + re.escape(key) + r'[^`]*\.jpg`', t['title']) or key in t['title']:
            return sym, t, None, None
    return None, None, None, None

def load_layout(path):
    if path and os.path.exists(path): return json.load(open(path, encoding='utf-8')), path
    if os.path.exists(DEFAULT_LAYOUT): return json.load(open(DEFAULT_LAYOUT, encoding='utf-8')), DEFAULT_LAYOUT
    try:
        out = subprocess.check_output(['git', 'show', 'origin/screens:layout.json'], cwd=ROOT, stderr=subprocess.DEVNULL)
        return json.loads(out.decode('utf-8')), 'origin/screens:layout.json'
    except Exception:
        sys.exit('layout.json 을 못 찾았다 — ui-screens/layout.json 을 두거나 `git fetch origin screens` 뒤 다시.')

def fmt(v): return '—' if v is None else ('%.1f' % v)
def fmt4(vals): return ' '.join(a + fmt(v) for a, v in zip('xywh', vals))

def score_screen(tables, layout, screen):
    sym, table, only, exclude = find_table(tables, screen)
    if table is None: return None, f'«{screen}» 에 맞는 표가 docs/ref-layout.md 에 없다 — ⑨~ 로 표를 추가(§5.5)'
    game = layout.get(screen) or {}
    rows_out = []; total = 0.0; counted = 0; world_skipped = []
    for name, ref, note in table['rows']:
        if '(참고·컨테이너)' in name: continue
        if only and not name.startswith(only): continue
        if exclude and name.startswith(exclude): continue
        g = game.get(name)
        is_world = ref[0] is None or ref[2] is None   # x 나 w 가 없는 행 = 캔버스 밖(월드)·부분 행 → layout.json 에 있을 때만 센다
        if g is None and is_world:
            world_skipped.append(name); rows_out.append((name, fmt4(ref), '—', '—', '측정 없음(월드)', None)); continue
        counted += 1
        if g is None:
            rows_out.append((name, fmt4(ref), '없음', '—', '✗ 0', 0.0)); continue
        diffs = []
        for i in range(4):
            if ref[i] is None or i >= len(g) or g[i] is None: diffs.append(None); continue
            diffs.append(float(g[i]) - ref[i])
        worst = max((abs(d) for d in diffs if d is not None), default=0.0)
        pt = 1.0 if worst <= PASS else (0.5 if worst <= HALF else 0.0)
        mark = '○ 1' if pt == 1 else ('△ 0.5' if pt == 0.5 else '✗ 0')
        total += pt
        rows_out.append((name, fmt4(ref), fmt4([float(v) for v in g[:4]]), ' '.join(('%+.1f' % d) if d is not None else '·' for d in diffs), mark, pt))
    score = round(10.0 * total / counted, 1) if counted else 0.0
    lines = [f'### {screen} — 표 {sym} «{table["title"]}» · 표 점수 **{score}/10** ({total:g}/{counted}행)', '',
             '| 행 | ref | 게임 | 차(게임−ref) | 판정 |', '|---|---|---|---|---|']
    for name, r, g, d, mark, _ in rows_out: lines.append(f'| {name} | {r} | {g} | {d} | {mark} |')
    fix = sorted([(name, d, pt) for name, r, g, d, mark, pt in rows_out if pt is not None and pt < 1], key=lambda t: (t[2], t[0]))   # 0점(없음·큰 차) 먼저
    if fix:
        lines += ['', '**다음 고칠 것**(0 · 0.5 행):']
        for name, d, pt in fix: lines.append(f'- {name}: 차 {d} ({pt:g}점)')
    if world_skipped: lines += ['', f'(월드 행 {len(world_skipped)}개는 layout.json 에 값이 없어 세지 않았다: {" · ".join(world_skipped)})']
    extra = [k for k in game if k not in {n for n, _, _ in table['rows']}]
    if extra: lines += ['', f'(표에 없는 이름표 {len(extra)}개 — 이름을 표의 «요소» 열과 같게: {" · ".join(extra)})']
    return score, '\n'.join(lines)

def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    flags = [a for a in sys.argv[1:] if a.startswith('--')]
    wide = '--all' in flags or '--summary' in flags
    if not args and not wide: print(__doc__); sys.exit(2)
    tables = parse_ref()
    layout, src = load_layout(args[1] if len(args) > 1 else (args[0] if wide and args else None))
    print(f'<!-- layout: {src} · meta: {json.dumps(layout.get("_meta", {}), ensure_ascii=False)} -->')
    if '--summary' in flags:
        # T277 — CI 꼬리에 넣으려고 «표만» 찍는다. `--all` 은 646줄이라(실측) 그대로 넣으면
        # T239 의 «실패한 테스트 목록» 을 꼬리 밖으로 도로 밀어낸다(그 사고가 T239 회차 1 → 2 의 자기 정정이었다).
        # 늘 0 으로 끝난다 — §5 표 여덟(㊱~㊸)은 레퍼런스 그림 없는 «회귀 자» 라 **뜻한 변경도 점수를 떨어뜨린다**(T277 3항).
        scored, no_table = [], []
        for screen in [k for k in layout if not k.startswith('_')]:
            score, _ = score_screen(tables, layout, screen)
            (no_table if score is None else scored).append(screen if score is None else (score, screen))
        low = sorted(s for s in scored if s[0] < 10.0)
        missing = layout.get('_missing', [])
        for sc, nm in low:
            print(f'[§5]   {nm} — {fmt(sc)}   ← `python3 tools/ui_score.py {nm}` 로 어느 행인지 본다')
        if no_table:
            print('[§5]   표 없음(ref-layout.md 에 그 화면 절이 없다): ' + ' · '.join(no_table))
        if missing:
            print('[§5]   화면 자체가 안 찍혔다: ' + ' · '.join(map(str, missing)))
        # T281 — **마지막 줄은 판정이다.** 처음 판(T277)은 어느 갈래로 나가든 «(보고만 …)» 한 줄로 끝나서
        # 꼬리로 읽으면 **빨강과 초록이 글자까지 똑같았다** — T281 이 `check_decisions` 에서 «가장 나쁜 꼴» 이라 부른 그것이다.
        # 이 자는 늘 0 으로 끝나므로(§5 표는 «회귀 자» 라 뜻한 변경도 점수를 떨어뜨린다 · T277 3항)
        # `✗` 는 «막는다» 가 아니라 «볼 것이 있다» 는 뜻이고, 그래서 «보고만» 을 판정 줄 안에 같이 적는다.
        n_all = len(scored) + len(no_table)
        if low or no_table or missing:
            head = ' · '.join(f'{nm} {fmt(sc)}' for sc, nm in low[:3]) + (' …' if len(low) > 3 else '')
            print(f'✗ [§5] 화면 {n_all}개 · 10.0 미만 {len(low)}건{" (" + head + ")" if low else ""}'
                  f' · 표 없음 {len(no_table)}건 · 안 찍힌 화면 {len(missing)}개'
                  f' — 보고만(막지 않는다 · T277) · `python3 tools/ui_score.py <화면>` 으로 어느 행인지 본다')
        else:
            print(f'✓ [§5] 화면 {n_all}개 전부 10.0 · 표 없음 0건 · 안 찍힌 화면 0개 (보고만 · T277)')
        sys.exit(0)
    if '--all' in flags:
        summary = ['| 화면 | 표 점수 |', '|---|---|']
        for screen in [k for k in layout if not k.startswith('_')]:
            score, text = score_screen(tables, layout, screen)
            print(text if score is not None else f'### {screen}\n{text}'); print()
            summary.append(f'| {screen} | {fmt(score)} |')
        missing = layout.get('_missing', [])
        if missing: summary += ['', '없음: ' + ' · '.join(map(str, missing))]
        print('\n'.join(summary))
    else:
        score, text = score_screen(tables, layout, args[0])
        print(text); sys.exit(0 if score is not None else 1)

if __name__ == '__main__':
    main()
