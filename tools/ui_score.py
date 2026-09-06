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
        m = re.match(r'^## ([①-⑳])\s*(.*)$', line)
        if m:
            cur = m.group(1); tables[cur] = {'title': m.group(2).strip(), 'rows': []}; in_table = False; continue
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
    if not args and '--all' not in flags: print(__doc__); sys.exit(2)
    tables = parse_ref()
    layout, src = load_layout(args[1] if len(args) > 1 else (args[0] if '--all' in flags and args else None))
    print(f'<!-- layout: {src} · meta: {json.dumps(layout.get("_meta", {}), ensure_ascii=False)} -->')
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
