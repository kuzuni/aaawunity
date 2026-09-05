#!/usr/bin/env python3
"""코드가 쓰는 카탈로그 키가 Assets/KkomaKnight/catalog.json 에 전부 있는가 (T12 · 플레이 콘솔 에러 0 게이트).

`gen_catalog.py --check` 는 catalog.json 의 «경로» 가 실재하는지만 본다. 이 스크립트는 반대 방향 —
Assets/Scripts/**/*.cs 의 문자열 리터럴 중 카탈로그 키 꼴(`<접두>.<이름>` · 접두 = catalog.json 에 있는 첫 마디)인 것을 뽑아
catalog.json 에 없으면 실패한다(없는 키는 런타임에 `[AssetCatalog] sprite 없음` 경고 + 빈 그림/NRE 로 이어진다).

- `"env."`, `"ui.itemFrame."` 처럼 점으로 끝나는 리터럴은 «접두 조립»(뒤에 변수를 붙인다)로 보고, 그 접두로 시작하는 키가 하나라도 있으면 통과.
- `"cm.gear." + part + "." + set` 처럼 조립되는 키의 완성형은 여기서 못 본다 — GearLookTests(EditMode) 가 표 전체를 대조한다.
- 키가 아닌 리터럴(데이터 파일 이름 `ui.json` 등)은 IGNORE 에 둔다.

사용: python3 tools/check_catalog_keys.py        # 문제 있으면 exit 1
"""
import json, os, re, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
SPEC = os.path.join(ROOT, 'Assets', 'KkomaKnight', 'catalog.json')
SCRIPTS = os.path.join(ROOT, 'Assets', 'Scripts')
SECTIONS = ('sprites', 'prefabs', 'controllers', 'materials', 'fonts', 'colors')
IGNORE = {'ui.json', 'tune.json', 'gear.json', 'gacha.json', 'perks.json', 'enemies.json', 'chapters.json', 'shop.json'}

def load_keys():
    with open(SPEC, encoding='utf-8') as f: spec = json.load(f)
    keys = set()
    for sec in SECTIONS: keys.update(spec.get(sec, {}).keys())
    return keys

def strip_comments(src):
    src = re.sub(r'/\*.*?\*/', '', src, flags=re.S)
    return re.sub(r'//[^\n]*', '', src)

def main():
    keys = load_keys()
    prefixes = {k.split('.')[0] for k in keys}
    lit_re = re.compile(r'"((?:' + '|'.join(sorted(map(re.escape, prefixes))) + r')\.[A-Za-z0-9_.+-]*)"')
    missing = []; seen = 0
    for dp, _, fs in os.walk(SCRIPTS):
        for fn in sorted(fs):
            if not fn.endswith('.cs'): continue
            path = os.path.join(dp, fn); rel = os.path.relpath(path, ROOT)
            src = strip_comments(open(path, encoding='utf-8', errors='ignore').read())
            for ln, line in enumerate(src.split('\n'), 1):
                for lit in lit_re.findall(line):
                    if lit in IGNORE or lit.endswith('.json'): continue
                    seen += 1
                    if lit in keys: continue
                    # 접두 조립: "env." / "ui.itemFrame." (점으로 끝) 또는 Palette.FrameKey("ui.cardFrame", 색) 처럼 뒤에 ".색" 이 붙는 밑동
                    base = lit if lit.endswith('.') else lit + '.'
                    if any(k.startswith(base) for k in keys): continue
                    missing.append((rel, ln, lit if lit.endswith('.') is False else lit + '*'))
    if missing:
        print(f'catalog 키 누락 {len(missing)}건 (catalog.json 에 없다 — 키를 추가하고 python3 tools/gen_catalog.py 로 재생성):')
        for rel, ln, lit in missing: print(f'  {rel}:{ln}  "{lit}"')
        return 1
    print(f'catalog 키 검사 OK — 리터럴 {seen}개 전부 catalog.json 에 있음 (키 {len(keys)}개)')
    return 0

if __name__ == '__main__':
    sys.exit(main())
