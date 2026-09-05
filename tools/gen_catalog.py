#!/usr/bin/env python3
"""catalog.json(용도 → 에셋) → Assets/KkomaKnight/AssetCatalog.asset(YAML) + docs/assets-map.md.

catalog.json 형식:
{
  "sprites":     { "<key>": "Assets/…/sheet.png#SpriteName" | "Assets/…/single.png", ... },
  "prefabs":     { "<key>": "Assets/…/X.prefab", ... },
  "controllers": { "<key>": "Assets/…/X.controller", ... },
  "materials":   { "<key>": "Assets/…/X.mat", ... },
  "fonts":       { "<key>": "Assets/…/X.ttf", ... },
  "colors":      { "<key>": "#RRGGBB[AA]", ... },
  "texts":       { "<key>": "Assets/…/X.json", ... },   # 이 레포 전용 JSON(TextAsset · fileID 4900000) — StreamingAssets 밖 파일을 빌드에 넣는 참조
  "_notes":      { "<key>": "이 에셋을 여기에 쓴 이유/자리", ... }
}
GUID 는 .meta 에서, 스프라이트 fileID 는 .png.meta 의 internalIDToNameTable(멀티) 또는 21300000(단일),
프리팹 root fileID 는 .prefab 의 최상위 GameObject(Transform m_Father: {fileID: 0}) 에서 읽는다.
사용: python3 tools/gen_catalog.py [--check]   (--check: 경로/스프라이트 이름이 전부 실재하는지만 검사)
"""
import json, os, re, sys, hashlib

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
SPEC = os.path.join(ROOT, 'Assets', 'KkomaKnight', 'catalog.json')
OUT = os.path.join(ROOT, 'Assets', 'KkomaKnight', 'AssetCatalog.asset')
DOC = os.path.join(ROOT, 'docs', 'assets-map.md')

def meta_guid(path):
    m = os.path.join(ROOT, path + '.meta')
    if not os.path.exists(m): raise FileNotFoundError('.meta 없음: ' + path)
    with open(m, encoding='utf-8', errors='ignore') as f:
        for line in f:
            if line.startswith('guid:'): return line.split(':', 1)[1].strip()
    raise ValueError('guid 없음: ' + path)

def sprite_fileid(png, name):
    """멀티 스프라이트: internalIDToNameTable 의 (fileID, name). 단일: 21300000."""
    m = os.path.join(ROOT, png + '.meta')
    txt = open(m, encoding='utf-8', errors='ignore').read()
    table = re.findall(r'first:\s*\n\s*213:\s*(-?\d+)\s*\n\s*second:\s*(.+)', txt)
    if name is None:
        if 'spriteMode: 1' in txt or not table: return 21300000
        raise ValueError(f'{png}: 멀티 스프라이트인데 #이름 이 없다 (후보: {", ".join(n for _, n in table[:12])} …)')
    for fid, nm in table:
        if nm.strip() == name: return int(fid)
    # spriteSheet.sprites 블록의 name/internalID 로 한 번 더
    for blk in re.finditer(r'- serializedVersion: 2\s*\n\s*name: (.+)\n(?:.*\n){0,40}?\s*internalID: (-?\d+)', txt):
        if blk.group(1).strip() == name: return int(blk.group(2))
    raise ValueError(f'{png}: 스프라이트 «{name}» 없음 (후보: {", ".join(n for _, n in table[:20])} …)')

_GUID2PATH = None
def guid_to_path(guid):
    """Assets 아래 .prefab.meta 를 한 번 훑어 guid → 프리팹 경로 표를 만든다 (변형 프리팹의 베이스를 따라가기 위해)."""
    global _GUID2PATH
    if _GUID2PATH is None:
        _GUID2PATH = {}
        for dp, _, fs in os.walk(os.path.join(ROOT, 'Assets')):
            for fn in fs:
                if fn.endswith('.prefab.meta'):
                    full = os.path.join(dp, fn)
                    with open(full, encoding='utf-8', errors='ignore') as f:
                        for line in f:
                            if line.startswith('guid:'): _GUID2PATH[line.split(':', 1)[1].strip()] = os.path.relpath(full[:-5], ROOT); break
    return _GUID2PATH.get(guid)

def prefab_root(prefab):
    """프리팹 루트 GameObject 의 fileID. 변형(variant) 프리팹은 로컬 Transform 이 없고 루트가 PrefabInstance 라서
    베이스 프리팹의 루트 fileID 와 인스턴스 fileID 를 XOR 한 값(유니티 규칙 · 하위 63비트)이 루트 GO 의 fileID 가 된다."""
    txt = open(os.path.join(ROOT, prefab), encoding='utf-8', errors='ignore').read()
    for m in re.finditer(r'--- !u!(4|224) &(\d+)\n(?:.*\n){0,40}?\s*m_GameObject: \{fileID: (\d+)\}(?:.*\n){0,40}?\s*m_Father: \{fileID: 0\}', txt):
        return int(m.group(3))
    for m in re.finditer(r'--- !u!1001 &(\d+)\nPrefabInstance:(?:.*\n){0,12}?\s*m_TransformParent: \{fileID: 0\}(?:.*\n)*?\s*m_SourcePrefab: \{fileID: \d+, guid: (\w+), type: 3\}', txt):
        inst, guid = int(m.group(1)), m.group(2)
        base = guid_to_path(guid)
        if not base: raise ValueError(f'{prefab}: 베이스 프리팹(guid {guid})을 못 찾았다')
        return (prefab_root(base) ^ inst) & 0x7FFFFFFFFFFFFFFF
    raise ValueError('프리팹 루트를 못 찾았다: ' + prefab)

def ref(fileid, guid, typ): return f'{{fileID: {fileid}, guid: {guid}, type: {typ}}}'

def hexcolor(h):
    h = h.lstrip('#'); r, g, b = int(h[0:2], 16) / 255, int(h[2:4], 16) / 255, int(h[4:6], 16) / 255
    a = int(h[6:8], 16) / 255 if len(h) >= 8 else 1.0
    return f'{{r: {r:.6g}, g: {g:.6g}, b: {b:.6g}, a: {a:.6g}}}'

def main():
    check = '--check' in sys.argv
    spec = json.load(open(SPEC, encoding='utf-8'))
    notes = spec.get('_notes', {})
    rows = []
    y = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:', '--- !u!114 &11400000', 'MonoBehaviour:',
         '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}', '  m_PrefabAsset: {fileID: 0}',
         '  m_GameObject: {fileID: 0}', '  m_Enabled: 1', '  m_EditorHideFlags: 0',
         f'  m_Script: {ref(11500000, hashlib.md5(b"Assets/Scripts/Game/AssetCatalog.cs").hexdigest(), 3)}',
         '  m_Name: AssetCatalog', '  m_EditorClassIdentifier: ']
    def section(field, items, fn):
        y.append(f'  {field}:' + ('' if items else ' []'))
        for key, val in items.items():
            entry = fn(key, val)
            y.append(f'  - key: {key}')
            y.append(f'    {entry[0]}: {entry[1]}')
            rows.append((field, key, val, entry[2], notes.get(key, '')))
    def sp(key, val):
        png, _, name = val.partition('#'); name = name or None
        fid = sprite_fileid(png, name); return ('sprite', ref(fid, meta_guid(png), 3), f'fileID {fid}')
    def pf(key, val): fid = prefab_root(val); return ('prefab', ref(fid, meta_guid(val), 3), f'root {fid}')
    def ct(key, val): return ('controller', ref(9100000, meta_guid(val), 2), 'fileID 9100000')
    def mt(key, val): return ('material', ref(2100000, meta_guid(val), 2), 'fileID 2100000')
    def fo(key, val): return ('font', ref(12800000, meta_guid(val), 3), 'fileID 12800000')
    def co(key, val): return ('color', hexcolor(val), val)
    def tx(key, val):
        if not os.path.exists(os.path.join(ROOT, val)): raise FileNotFoundError('텍스트 파일 없음: ' + val)
        return ('text', ref(4900000, meta_guid(val), 3), 'fileID 4900000')
    section('sprites', spec.get('sprites', {}), sp)
    section('prefabs', spec.get('prefabs', {}), pf)
    section('controllers', spec.get('controllers', {}), ct)
    section('materials', spec.get('materials', {}), mt)
    section('fonts', spec.get('fonts', {}), fo)
    section('colors', spec.get('colors', {}), co)
    section('texts', spec.get('texts', {}), tx)
    if check:
        print(f'catalog OK — {len(rows)} entries'); return
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(y) + '\n')
    with open(DOC, 'w', encoding='utf-8', newline='\n') as f:
        f.write('# 에셋 사용 지도 (assets-map)\n\n> `tools/gen_catalog.py` 가 `Assets/KkomaKnight/catalog.json` 에서 생성한다 — 손으로 고치지 말고 catalog.json 을 고칠 것.\n'
                '> 키는 코드(`App.Assets.Sprite("key")` 등)에서 쓰는 이름, 경로는 주인 에셋의 실제 위치다. 주인이 바꾸고 싶은 줄만 말해 주면 그 줄의 경로를 바꾼다.\n\n')
        f.write('| 종류 | 키 | 에셋 (경로#스프라이트) | ID | 쓰는 자리 |\n|---|---|---|---|---|\n')
        for kind, key, val, idinfo, note in rows: f.write(f'| {kind} | `{key}` | `{val}` | {idinfo} | {note} |\n')
    print(f'wrote {OUT} and {DOC} — {len(rows)} entries')

if __name__ == '__main__':
    main()
