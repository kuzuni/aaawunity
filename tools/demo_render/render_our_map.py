#!/usr/bin/env python3
"""우리 전투 맵(MapLayouts × Layout.MapScale · BattleWorld 의 좌표 규칙)을 유니티 없이 그림으로 본다 — 데모 씬 렌더(render_demo_scene.py)와 나란히 비교용(T19).
프레임 540×1140(9:19 · 1 레이아웃 px = 1 px) · 데모 1u = 100 × MapScale px · 길 중심 = 프레임 41% · 정렬 = Field < Road < 납작(Road_up·풀꽃) < 나머지(발 줄 위는 뒤, 아래는 앞).
HUD 가 가리는 위(0~17.5%)·아래(69.5%~) 띠는 반투명 검정으로, 발 줄(40%)은 빨간 선으로 표시한다. 캐릭터는 CharScale 키(0.69u)의 회색 상자.
사용: python3 tools/demo_render/render_our_map.py <출력폴더> [씬 x 오프셋들 · 기본 0,9,18]  → <출력폴더>/our_<theme>_x<off>.html
      NODE_PATH=$(npm root -g) node tools/demo_render/shot_our.js <출력폴더>   → PNG (커밋 금지)
"""
import re, os, sys, struct, base64, glob
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..')
out = sys.argv[1] if len(sys.argv) > 1 else '.'
offs = [float(v) for v in (sys.argv[2].split(',') if len(sys.argv) > 2 else ['0', '9', '18'])]
os.makedirs(out, exist_ok=True)
src = open(os.path.join(ROOT, 'Assets', 'Scripts', 'Game', 'MapLayouts.cs'), encoding='utf-8').read()
lay = open(os.path.join(ROOT, 'Assets', 'Scripts', 'Core', 'Layout.cs'), encoding='utf-8').read()
MAP = float(re.search(r'MapScale = ([\d.]+)f', lay).group(1)); CHAR = eval(re.search(r'CharScale = ([\d./f ]+);', lay).group(1).replace('f', ''))
FIELD_Y, FIELD_SX, FIELD_SY = [float(v) for v in re.search(r'FieldY = (-?[\d.]+)f, FieldScaleX = ([\d.]+)f, FieldScaleY = ([\d.]+)f', src).groups()]
ROAD_Y, ROAD_SX, ROAD_SY = [float(v) for v in re.search(r'RoadCenterY = (-?[\d.]+)f, RoadScaleX = ([\d.]+)f, RoadScaleY = ([\d.]+)f', src).groups()]
cat = __import__('json').load(open(os.path.join(ROOT, 'Assets', 'KkomaKnight', 'catalog.json'), encoding='utf-8'))['sprites']
W, H, PPU = 540, 1140, 100
ROAD_FRAC, FOOT_FRAC, HUD_TOP, HUD_BOT = 0.41, 0.40, 0.175, 0.695
U = PPU * MAP                      # 데모 1u → px
def png_size(p):
    with open(p, 'rb') as f: f.seek(16); return struct.unpack('>II', f.read(8))
def demo_y_px(y): return H * (ROAD_FRAC - (y - ROAD_Y) * U / H)
cache = {}
def img(path):
    if path not in cache: cache[path] = base64.b64encode(open(os.path.join(ROOT, path), 'rb').read()).decode()
    return cache[path]
def place(html, key, x_px, y_px, sx, sy, z=None):
    p = cat[key]; w, h = png_size(os.path.join(ROOT, p))
    wp, hp = w / 100 * abs(sx) * U, h / 100 * abs(sy) * U
    flip = 'transform:scaleX(-1);' if sx < 0 else ''
    html.append(f'<img src="data:image/png;base64,{img(p)}" style="position:absolute;left:{x_px - wp / 2:.1f}px;top:{y_px - hp / 2:.1f}px;width:{wp:.1f}px;height:{hp:.1f}px;{flip}">')
for theme in ['autumn', 'deepForest', 'forest', 'desert']:
    cap = theme[0].upper() + theme[1:]
    width = float(re.search(cap + r'Width = ([\d.]+)f', src).group(1))
    body = re.search(r'P\[\] ' + cap + r' =\n\s+\{(.*?)\};', src, re.S).group(1)
    rows = [(k, float(x), float(y), float(sx), float(sy)) for k, x, y, sx, sy in re.findall(r'new P\("([^"]+)", (-?[\d.]+)f, (-?[\d.]+)f, (-?[\d.]+)f, (-?[\d.]+)f\)', body)]
    foot_demo_y = ROAD_Y + (ROAD_FRAC - FOOT_FRAC) * H / U
    for off in offs:
        html = [f'<html><body style="margin:0;background:#000"><div style="position:relative;width:{W}px;height:{H}px;overflow:hidden">']
        # 바닥 · 길 (가로 반복)
        fw = 1.28 * FIELD_SX * U; rw = 1.28 * ROAD_SX * U
        n = int(W / fw) + 3
        for i in range(-n, n + 1): place(html, f'env.{theme}.field', W / 2 + i * fw - (off * U) % fw, demo_y_px(FIELD_Y), FIELD_SX, FIELD_SY)
        n = int(W / rw) + 3
        for i in range(-n, n + 1): place(html, f'env.{theme}.road', W / 2 + i * rw - (off * U) % rw, demo_y_px(ROAD_Y), ROAD_SX, ROAD_SY)
        # 소품 — 주기 반복 · BattleWorld 와 같은 정렬(납작 → 발 줄 위(y 내림차순) → 캐릭터 → 발 줄 아래(y 내림차순))
        items = []
        for k0 in range(-2, 3):
            for k, x, y, sx, sy in rows:
                xp = W / 2 + (x + k0 * width - off) * U
                if xp < -400 or xp > W + 400: continue
                yf = (demo_y_px(y)) / H
                if yf < -0.15 or yf > 0.72: continue
                p = cat[k]; w, h = png_size(os.path.join(ROOT, p)); flat = k.endswith('.roadUp') or h / 100 * abs(sy) < 0.35   # T45: 물결 경계는 높이와 무관하게 납작(Road_up_Desert 43px)
                order = -16 if flat else (max(-60, -12 - int((y - foot_demo_y) * 3)) if y > foot_demo_y else min(470, 381 + int((foot_demo_y - y) * 5)))
                items.append((order, -y, k, xp, demo_y_px(y), sx, sy))
        vis = sum(1 for it in items if 0 <= it[3] <= W and HUD_TOP * H <= it[4] <= HUD_BOT * H and not it[2].endswith('.roadUp'))
        drawn_char = False
        for order, _, k, xp, yp, sx, sy in sorted(items, key=lambda it: (it[0], it[1])):
            if order >= 100 and not drawn_char:
                ch_h = 0.09 * CHAR * H; ch_w = ch_h * 0.7; fx, fy = 0.16 * W, FOOT_FRAC * H
                html.append(f'<div style="position:absolute;left:{fx - ch_w / 2:.0f}px;top:{fy - ch_h:.0f}px;width:{ch_w:.0f}px;height:{ch_h:.0f}px;background:rgba(80,80,90,.85);border:2px solid #fff;box-sizing:border-box"></div>'); drawn_char = True
            place(html, k, xp, yp, sx, sy)
        if not drawn_char:
            ch_h = 0.09 * CHAR * H; ch_w = ch_h * 0.7; fx, fy = 0.16 * W, FOOT_FRAC * H
            html.append(f'<div style="position:absolute;left:{fx - ch_w / 2:.0f}px;top:{fy - ch_h:.0f}px;width:{ch_w:.0f}px;height:{ch_h:.0f}px;background:rgba(80,80,90,.85);border:2px solid #fff;box-sizing:border-box"></div>')
        html.append(f'<div style="position:absolute;left:0;top:0;width:{W}px;height:{HUD_TOP * H:.0f}px;background:rgba(0,0,0,.45)"></div>')
        html.append(f'<div style="position:absolute;left:0;top:{HUD_BOT * H:.0f}px;width:{W}px;height:{H - HUD_BOT * H:.0f}px;background:rgba(0,0,0,.45)"></div>')
        html.append(f'<div style="position:absolute;left:0;top:{FOOT_FRAC * H:.0f}px;width:{W}px;height:1px;background:#f00"></div>')
        html.append(f'<div style="position:absolute;left:4px;top:{HUD_TOP * H + 4:.0f}px;color:#fff;font:14px sans-serif;text-shadow:0 0 3px #000">{theme} · x {off:g} · 창 안 소품 {vis}</div>')
        html.append('</div></body></html>')
        open(os.path.join(out, f'our_{theme}_x{off:g}.html'), 'w').write(''.join(html))
        print(theme, 'x', off, 'visible props', vis)
