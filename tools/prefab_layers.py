#!/usr/bin/env python3
"""프리팹 «칠하는 겹» 세기 (T223 · ROUTINE §2 T223 3항).

무엇을 재는가 — 프리팹 하나가 **자기 칸을 몇 번 칠하는가**.
`OverdrawAuditTests` 는 화면 «합계» 만 낸다(오버드로 7.38 · 조각 208). 어느 조각이 넓은지를
모르면 손댈 데를 못 고르는데, 그 자 파일은 T217 lock 이라 이 절을 잡은 워커가 못 넓힌다
(ROUTINE §2 T223 5항 «같은 파일 두 워커 금지»). 그래서 **화면 쪽만 보는 자**를 따로 둔다 —
유니티를 안 띄우고 `.prefab` YAML 을 직접 읽어 그림(Image·RawImage) 겹을 세고,
각 겹이 **뿌리 칸의 몇 %를 덮는가**를 앵커에서 계산한다.

한계(먼저 적어 둔다):
  · 코드가 런타임에 `UiKit.Pct`·`Hide` 로 바꾸는 것은 반영 못 한다 — 그래서 «조각이 원래
    몇 겹인가» 를 재는 자이지 «화면이 실제로 몇 번 칠하는가» 를 재는 자가 아니다.
    실측의 정본은 언제나 `screens/overdraw.json` 이다.
  · 뿌리 크기를 안 주면 조각의 «원래 크기» 로 잰다. 화면이 조각을 다른 크기로 늘여 쓰면
    고정 px 로 물린 테·안쪽 선의 몫이 달라지므로 `--at` 로 그 칸 크기를 주는 편이 옳다
    (T223 회차 3 이 큰 카드 ↔ 작은 카드에서 이 차이를 만났다).
  · `m_FillCenter: 0` 인 9-slice 는 «(링)» 으로 표시만 한다 — rect 는 칸 전체지만 칠하는
    것은 테뿐이라 그 줄의 넓이는 **위쪽 한계**다.

쓰기:
  python3 tools/prefab_layers.py ui.cardFrame.blue ui.shopItem      # 카탈로그 키
  python3 tools/prefab_layers.py ui.cardFrame.plum --at=1015x607    # 화면에 놓이는 칸 크기로
  python3 tools/prefab_layers.py ui.shopItem --tree                 # 계층까지
  python3 tools/prefab_layers.py --self-test
"""
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CATALOG = os.path.join(ROOT, 'Assets', 'KkomaKnight', 'catalog.json')

DOC = re.compile(r'^--- !u!(\d+) &(\d+)( stripped)?\s*$', re.M)


def guid_index():
    """guid → 애셋 경로(중첩 프리팹을 따라가는 데 쓴다)."""
    idx = {}
    for base, dirs, files in os.walk(os.path.join(ROOT, 'Assets')):
        dirs[:] = [d for d in dirs if d != '.git']
        for f in files:
            if not f.endswith('.meta'):
                continue
            p = os.path.join(base, f)
            try:
                with open(p, encoding='utf-8', errors='replace') as fh:
                    for line in fh:
                        if line.startswith('guid:'):
                            idx[line.split(':', 1)[1].strip()] = p[:-5]
                            break
            except OSError:
                pass
    return idx


def split_docs(text):
    """YAML 문서를 (classId, fileId, 본문) 으로 자른다."""
    out = []
    marks = list(DOC.finditer(text))
    for i, m in enumerate(marks):
        end = marks[i + 1].start() if i + 1 < len(marks) else len(text)
        out.append((int(m.group(1)), m.group(2), text[m.end():end], bool(m.group(3))))
    return out


def _f(body, key, default=None):
    m = re.search(r'^\s*%s:\s*(.+)$' % re.escape(key), body, re.M)
    return m.group(1).strip() if m else default


def _xy(body, key):
    m = re.search(r'^\s*%s:\s*\{x:\s*([-\d.eE]+),\s*y:\s*([-\d.eE]+)\}' % re.escape(key), body, re.M)
    return (float(m.group(1)), float(m.group(2))) if m else None


def _fileid(v):
    if not v:
        return None
    m = re.search(r'fileID:\s*(\d+)', v)
    return m.group(1) if m and m.group(1) != '0' else None


def coverage(body):
    """
    RectTransform 한 칸의 크기 규칙 = <b>(앵커 폭, 앵커 높이, sizeDelta x, sizeDelta y)</b>.
    유니티가 쓰는 식 그대로다 — 실제 크기 = 부모 크기 × 앵커 폭 + sizeDelta.
    그래서 부모 픽셀만 알면 «고정 크기» 칸도 비율이 나온다(전에는 `?` 로 뒀다).
    """
    amin, amax = _xy(body, 'm_AnchorMin'), _xy(body, 'm_AnchorMax')
    sd = _xy(body, 'm_SizeDelta') or (0.0, 0.0)
    if amin is None or amax is None:
        return None
    return (amax[0] - amin[0], amax[1] - amin[1], sd[0], sd[1])


def size_of(spec, parent):
    """(앵커·sizeDelta) + 부모 픽셀 크기 → 이 칸의 픽셀 크기. 부모를 모르면 None."""
    if spec is None or parent is None:
        return None
    w = parent[0] * spec[0] + spec[2]
    h = parent[1] * spec[1] + spec[3]
    return (w, h) if w > 0 and h > 0 else None


class Node(object):
    def __init__(self, name, cov, exact, paint, kind):
        self.name, self.cov, self.exact, self.paint, self.kind = name, cov, exact, paint, kind
        self.kids = []


def mods_rect(body):
    """인스턴스가 덮어쓴 앵커·크기 규칙(없으면 None) — 중첩 조각의 자리는 원본이 아니라 이쪽이 정한다."""
    vals = {}
    for m in re.finditer(r'propertyPath:\s*(m_Anchor(?:Min|Max)\.[xy]|m_SizeDelta\.[xy])\s*\n\s*value:\s*([-\d.eE]+)', body):
        vals[m.group(1)] = float(m.group(2))
    need = ('m_AnchorMin.x', 'm_AnchorMin.y', 'm_AnchorMax.x', 'm_AnchorMax.y')
    if not all(k in vals for k in need):
        return None
    return (vals['m_AnchorMax.x'] - vals['m_AnchorMin.x'], vals['m_AnchorMax.y'] - vals['m_AnchorMin.y'],
            vals.get('m_SizeDelta.x', 0.0), vals.get('m_SizeDelta.y', 0.0))


def mods_off(body):
    """변형(variant)·인스턴스가 «끈» 자식의 fileID 들 — `m_IsActive: 0` 덮어쓰기만 본다."""
    off = set()
    for m in re.finditer(r'-\s+target:\s*\{fileID:\s*(\d+)[^}]*\}\s*\n\s*propertyPath:\s*m_IsActive\s*\n\s*value:\s*0\b', body):
        off.add(m.group(1))
    return off


def load(path, gidx, depth=0, off=frozenset()):
    """프리팹 하나를 그림 겹 트리로 읽는다(중첩 프리팹은 따라 들어간다)."""
    with open(path, encoding='utf-8', errors='replace') as fh:
        text = fh.read()
    docs = split_docs(text)
    go, rt, paint, children, roots, nested = {}, {}, {}, {}, [], []
    # 벗겨진(stripped) RectTransform = «중첩 조각의 뿌리» 자리표. 그 밑에 붙는 자식은 중첩 조각의 뿌리에 붙는다.
    strip_of = {}
    subs = {}
    for cls, fid, body, stripped in docs:
        if stripped:
            if cls == 224 or cls == 4:
                strip_of[fid] = _fileid(_f(body, 'm_PrefabInstance'))
            continue
        if cls == 1:
            go[fid] = {'name': _f(body, 'm_Name', '?').strip(), 'on': _f(body, 'm_IsActive', '1') == '1',
                       'comps': re.findall(r'component:\s*\{fileID:\s*(\d+)\}', body)}
        elif cls == 224 or cls == 4:
            father = _fileid(_f(body, 'm_Father'))
            rt[fid] = {'go': _fileid(_f(body, 'm_GameObject')), 'father': father,
                       'cov': coverage(body) if cls == 224 else None,
                       'order': re.findall(r'\{fileID:\s*(\d+)\}', _f(body, 'm_Children', '') or '')}
            children.setdefault(father, []).append(fid)
            if father is None:
                roots.append(fid)
        elif cls == 114:
            kind = None
            if re.search(r'^\s*m_Sprite:', body, re.M):
                kind = 'Image'
            elif re.search(r'^\s*m_Texture:', body, re.M) and re.search(r'^\s*m_UVRect:', body, re.M):
                kind = 'RawImage'
            if kind:
                a = re.search(r'm_Color:\s*\{r:\s*[-\d.eE]+,\s*g:\s*[-\d.eE]+,\s*b:\s*[-\d.eE]+,\s*a:\s*([-\d.eE]+)\}', body)
                # 9-slice 링(m_FillCenter: 0)은 «가운데를 안 그린다» — rect 는 칸 전체지만 칠하는 것은 테두리뿐이다
                ring = _f(body, 'm_FillCenter', '1') == '0'
                paint[_fileid(_f(body, 'm_GameObject'))] = (kind + ('(링)' if ring else ''), float(a.group(1)) if a else 1.0)
        elif cls == 1001 and depth < 4:
            g = re.search(r'm_SourcePrefab:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]+)', body)
            src = gidx.get(g.group(1)) if g else None
            if src and os.path.exists(src) and os.path.abspath(src) != os.path.abspath(path):
                sub = load(src, gidx, depth + 1, mods_off(body))
                if sub:
                    sub.name = '[' + os.path.basename(src)[:-7] + ']'
                    r = mods_rect(body)
                    if r:
                        sub.cov = r
                    father = _fileid(_f(body, 'm_TransformParent'))
                    children.setdefault(father, []).append(('nested', sub))
                    nested.append(sub)
                    subs[fid] = sub

    def build(fid):
        r = rt.get(fid)
        if r is None:
            return None
        g = go.get(r['go'], {})
        if not g.get('on', True) or r['go'] in off or fid in off:
            return None
        p = paint.get(r['go'])
        n = Node(g.get('name', '?'), r['cov'], True, p is not None and p[1] >= 0.02, p[0] if p else '')
        for c in children.get(fid, []):
            if isinstance(c, tuple):
                n.kids.append(c[1])
            else:
                k = build(c)
                if k:
                    n.kids.append(k)
        return n

    # 벗겨진 뿌리 밑에 «덧붙인» 자식(변형이 더한 글자·그림)을 그 중첩 조각의 뿌리에 매단다
    for sfid, inst in strip_of.items():
        sub = subs.get(inst)
        if sub is None:
            continue
        for c in children.get(sfid, []):
            k = c[1] if isinstance(c, tuple) else build(c)
            if k:
                sub.kids.append(k)

    for r in roots:
        n = build(r)
        if n:
            return n
    # 변형(variant) 프리팹은 제 RectTransform 이 없고 «중첩 하나» 뿐이다 — 그 뿌리가 곧 이 프리팹이다
    return nested[0] if len(nested) == 1 else None


def walk(n, size=None, depth=0, out=None, path='', root=None, at=None):
    """
    뿌리 칸을 1 로 놓고 겹마다 «칸의 몇 배를 덮는가» 를 매긴다.
    픽셀 크기를 위에서 아래로 물려 주므로 «고정 크기» 칸도 비율이 나온다.

    <paramref name="at"/> = 이 조각이 <b>화면에서 실제로 놓이는 크기</b>(px). 코드가
    <c>UiKit.Stretch</c>·<c>Pct</c> 로 칸에 맞춰 늘이므로 조각의 «원래 크기» 로 잰 비율은
    작은 칸에서 틀어진다(고정 px 로 물린 테·안쪽 선의 몫이 칸이 작을수록 커진다).
    """
    out = [] if out is None else out
    here = path + ('/' if path else '') + n.name
    if depth == 0:
        # 뿌리 크기 = 준 크기(at) 또는 제 sizeDelta(앵커 폭이 0 인 «고정» 뿌리)
        size = at or ((n.cov[2], n.cov[3]) if (n.cov and n.cov[2] > 0 and n.cov[3] > 0) else None)
        root = size
    else:
        size = size_of(n.cov, size)
    share = (size[0] * size[1]) / (root[0] * root[1]) if (size and root) else (1.0 if depth == 0 else None)
    if n.paint:
        out.append((here, n.kind, share, True, depth))
    for k in n.kids:
        walk(k, size, depth + 1, out, here, root)
    return out


def resolve(key):
    cat = json.load(open(CATALOG, encoding='utf-8'))
    for group in ('prefabs', 'sprites'):
        if key in cat.get(group, {}):
            return os.path.join(ROOT, cat[group][key])
    if os.path.exists(os.path.join(ROOT, key)):
        return os.path.join(ROOT, key)
    return None


def self_test():
    """앵커 → 넓이 계산이 옳은가(칸 하나가 곧 넓이 1)."""
    def rect(ax, ay, bx, by, dw, dh):
        return ('m_AnchorMin: {x: %g, y: %g}\nm_AnchorMax: {x: %g, y: %g}\nm_SizeDelta: {x: %g, y: %g}\n'
                % (ax, ay, bx, by, dw, dh))

    parent = (100.0, 200.0)
    cases = [
        # 부모를 꽉 채우는 칸 = 부모와 같은 크기
        (rect(0, 0, 1, 1, 0, 0), parent, (100.0, 200.0)),
        # 4분의 1 칸
        (rect(0, 0, 0.5, 0.5, 0, 0), parent, (50.0, 100.0)),
        # 고정 크기 칸 — 앵커 폭 0 이라 sizeDelta 가 곧 크기(전에는 `?` 였다)
        (rect(0.5, 0.5, 0.5, 0.5, 80, 40), parent, (80.0, 40.0)),
        # 꽉 채우되 안쪽으로 10씩 물린 칸(음수 sizeDelta)
        (rect(0, 0, 1, 1, -20, -20), parent, (80.0, 180.0)),
        # 가로만 늘이고 세로는 고정
        (rect(0, 0.5, 1, 0.5, 0, 60), parent, (100.0, 60.0)),
        # 부모를 모르면 크기도 모른다
        (rect(0, 0, 1, 1, 0, 0), None, None),
        # 크기가 0 이하로 접히면 «칠하지 않는다»
        (rect(0, 0, 1, 1, -100, 0), parent, None),
    ]
    bad = 0
    for body, par, want in cases:
        got = size_of(coverage(body), par)
        ok = (got is None and want is None) or (got and want and abs(got[0] - want[0]) < 1e-6 and abs(got[1] - want[1]) < 1e-6)
        if not ok:
            bad += 1
            print('  ✗ %r ← 부모 %r → %r (바란 것 %r)' % (body.replace('\n', ' '), par, got, want))
    # 인스턴스 덮어쓰기도 같은 규칙으로 읽힌다
    inst = ('    - target: {fileID: 1}\n      propertyPath: m_AnchorMin.x\n      value: 0\n'
            '    - target: {fileID: 1}\n      propertyPath: m_AnchorMin.y\n      value: 0\n'
            '    - target: {fileID: 1}\n      propertyPath: m_AnchorMax.x\n      value: 1\n'
            '    - target: {fileID: 1}\n      propertyPath: m_AnchorMax.y\n      value: 1\n')
    if size_of(mods_rect(inst), parent) != (100.0, 200.0):
        bad += 1
        print('  ✗ 인스턴스 덮어쓰기 → %r' % (size_of(mods_rect(inst), parent),))
    if mods_rect('    - target: {fileID: 1}\n      propertyPath: m_Name\n      value: x\n') is not None:
        bad += 1
        print('  ✗ 앵커를 안 덮어쓴 인스턴스는 None 이어야 한다')
    print('자가 검사 %d/%d' % (len(cases) + 2 - bad, len(cases) + 2))
    return 1 if bad else 0


def main(argv):
    if '--self-test' in argv:
        return self_test()
    tree = '--tree' in argv
    at = None
    for a in argv:
        if a.startswith('--at='):
            w, _, h = a[5:].partition('x')
            at = (float(w), float(h))
    keys = [a for a in argv if not a.startswith('--')]
    if not keys:
        print(__doc__)
        return 2
    gidx = guid_index()
    for key in keys:
        p = resolve(key)
        if not p or not os.path.exists(p):
            print('%s — 못 찾았다' % key)
            continue
        n = load(p, gidx)
        if n is None:
            print('%s — 읽었지만 뿌리를 못 찾았다' % key)
            continue
        rows = walk(n, at=at)
        tot = sum(r[2] for r in rows if r[2] is not None)
        unk = sum(1 for r in rows if r[2] is None)
        print('%s  (%s)%s' % (key, os.path.basename(p), (' · 칸 %gx%gpx 로 놓고 잰다' % at) if at else ''))
        print('  칠하는 겹 %d개 · 칸 넓이 합 %.2f배%s' % (len(rows), tot, (' · 비율 모름 %d개' % unk) if unk else ''))
        for path_, kind, share, exact, depth in sorted(rows, key=lambda r: -(r[2] or 0)):
            if tree or share is None or share >= 0.10:
                print('    %-7s %-6s %s' % ('%.2f' % share if share is not None else '?',
                                            ('' if exact else '~') + kind[:5], path_))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
