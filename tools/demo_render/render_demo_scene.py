#!/usr/bin/env python3
"""Layer Lab Environment 데모 씬(DemoScene_*.unity)을 유니티 없이 그림으로 본다 — 씬의 PrefabInstance 를 읽어 스프라이트를 씬 좌표대로 놓은 HTML 을 만든다.
(카메라 ortho 5 → 10u 높이 · 1600×900 · PPU 90 · 정렬 = Field < Road < Road_up < 나머지는 y 내림차순)
사용: python3 tools/demo_render/render_demo_scene.py <출력폴더>  → <출력폴더>/DemoScene_*.html
      NODE_PATH=$(npm root -g) node tools/demo_render/shot.js <출력폴더>   → PNG (PNG 는 커밋 금지 · 비교용)
"""
import re, os, sys, struct, base64
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..')
ENV = os.path.join(ROOT, 'Assets', 'Layer Lab', '2D Minimal-Environment')
out = sys.argv[1] if len(sys.argv) > 1 else '.'
os.makedirs(out, exist_ok=True)
g2p = {}
for dp, dn, fn in os.walk(ENV):
    for f in fn:
        if f.endswith('.meta'):
            p = os.path.join(dp, f); m = re.search(r'guid: ([0-9a-f]{32})', open(p, encoding='utf-8', errors='ignore').read())
            if m: g2p[m.group(1)] = p[:-5]
def png_size(p):
    with open(p, 'rb') as f: f.seek(16); return struct.unpack('>II', f.read(8))
def meta_pivot(png):
    t = open(png + '.meta', encoding='utf-8', errors='ignore').read()
    a = re.search(r'alignment: (\d+)', t); pv = re.search(r'spritePivot: \{x: ([\d.]+), y: ([\d.]+)\}', t)
    return (float(pv.group(1)), float(pv.group(2))) if a and a.group(1) == '9' and pv else (0.5, 0.5)
def prefab_sprite(pp):
    t = open(pp, encoding='utf-8', errors='ignore').read()
    m = re.search(r'm_Sprite: \{fileID: \d+, guid: ([0-9a-f]{32})', t); return g2p.get(m.group(1)) if m else None
for scene in ['DemoScene_Forest', 'DemoScene_Autumn', 'DemoScene_Desert', 'DemoScene_DeepForest']:
    t = open(os.path.join(ENV, 'Environment 1', 'Scene', scene + '.unity'), encoding='utf-8', errors='ignore').read()
    tpos = {}
    for m in re.finditer(r'--- !u!4 &(\d+)\nTransform:.*?m_LocalPosition: \{x: (-?[\d.e-]+), y: (-?[\d.e-]+)', t, re.S): tpos[m.group(1)] = (float(m.group(2)), float(m.group(3)))
    items = []
    for b in t.split('--- !u!')[1:]:
        if not b.startswith('1001 '): continue
        g = re.search(r'm_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]{32})', b)
        if not g or g.group(1) not in g2p: continue
        pp = g2p[g.group(1)]; sp = prefab_sprite(pp)
        if not sp or not sp.endswith('.png'): continue
        def val(k, d):
            m = re.search(r'propertyPath: ' + re.escape(k) + r'\n\s+value: (-?[\d.e-]+)', b); return float(m.group(1)) if m else d
        x, y = val('m_LocalPosition.x', 0), val('m_LocalPosition.y', 0); sx, sy = val('m_LocalScale.x', 1), val('m_LocalScale.y', 1)
        par = re.search(r'm_TransformParent: \{fileID: (\d+)\}', b)
        if par and par.group(1) in tpos: x += tpos[par.group(1)][0]; y += tpos[par.group(1)][1]
        w, h = png_size(sp); px, py = meta_pivot(sp)
        items.append(dict(sp=sp, x=x, y=y, sx=sx, sy=sy, w=w, h=h, px=px, py=py, name=os.path.basename(pp)))
    PPU = 90; W, H = 1600, 900
    order = {'Field': 0, 'Road': 1, 'Road_up': 2}
    def key(it):
        base = re.sub(r'_(Autumn|DeepForest|Forest|Desert)\.prefab$', '', it['name']).replace('.prefab', '')
        return (order.get(base, 3), -it['y'])
    html = [f'<html><body style="margin:0;background:#31465a"><div style="position:relative;width:{W}px;height:{H}px;overflow:hidden">']
    for it in sorted(items, key=key):
        wu = it['w'] / 100 * abs(it['sx']); hu = it['h'] / 100 * abs(it['sy'])
        left = W / 2 + (it['x'] - wu * (it['px'] if it['sx'] > 0 else 1 - it['px'])) * PPU
        top = H / 2 - (it['y'] + hu * (1 - it['py'])) * PPU
        data = base64.b64encode(open(it['sp'], 'rb').read()).decode()
        flip = 'transform:scaleX(-1);' if it['sx'] < 0 else ''
        html.append(f'<img src="data:image/png;base64,{data}" style="position:absolute;left:{left:.1f}px;top:{top:.1f}px;width:{wu*PPU:.1f}px;height:{hu*PPU:.1f}px;{flip}">')
    html.append('</div></body></html>')
    open(os.path.join(out, scene + '.html'), 'w').write(''.join(html)); print(scene, len(items), 'instances')
