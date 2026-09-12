#!/usr/bin/env python3
"""코드가 쓰는 카탈로그 키가 Assets/KkomaKnight/catalog.json 에 전부 있는가 (T12 · 플레이 콘솔 에러 0 게이트).

`gen_catalog.py --check` 는 catalog.json 의 «경로» 가 실재하는지만 본다. 이 스크립트는 반대 방향 —
Assets/Scripts/**/*.cs 의 문자열 리터럴 중 카탈로그 키 꼴(`<접두>.<이름>` · 접두 = catalog.json 에 있는 첫 마디)인 것을 뽑아
catalog.json 에 없으면 실패한다(없는 키는 런타임에 `[AssetCatalog] sprite 없음` 경고 + 빈 그림/NRE 로 이어진다).

- `"env."`, `"ui.itemFrame."` 처럼 점으로 끝나는 리터럴은 «접두 조립»(뒤에 변수를 붙인다)로 보고, 그 접두로 시작하는 키가 하나라도 있으면 통과.
- `"cm.gear." + part + "." + set` 처럼 조립되는 키의 완성형은 여기서 못 본다 — GearLookTests(EditMode) 가 표 전체를 대조한다.
- 키가 아닌 리터럴(데이터 파일 이름 `ui.json` 등)은 IGNORE 에 둔다.

⚑ **이 자가 원리적으로 못 보는 자리**(T505 에서 자기 검사로 못 박았다) — 위 `lit_re` 는 «접두가 catalog.json 에 **이미 있는**»
리터럴만 본다. 그래서 코드가 **아예 새 접두**(`"vfx.hitGlow"` 처럼 catalog 에 `vfx.*` 가 하나도 없는 꼴)를 쓰면 **한 건도 안 걸린다.**
그 꼴이 곧 이 자가 막으려던 사고(런타임 «없음» 경고)의 가장 큰 얼굴인데, 접두를 안 보면 «점 있는 보통 글자»(`"Sprites/Default"`)와
가릴 수가 없어 이렇게 만든 것이다 — **의도한 한계다**. 실측(2026-09-12 · 검수 Q): 카탈로그를 먹는 부름
(`Assets.Sprite`·`Fx.Spawn`·`Audio.Sfx`·`Icons.Perk` …)에 박힌 리터럴 **49종 중 이 꼴 0종** ⇒ 지금은 새는 것이 없다.
메우려면 «부름의 첫 인자» 쪽에서 보면 된다(접두와 무관하게 그것은 반드시 카탈로그 키다) — 다음 손 몫.
⚠ 한계 둘째(같은 회차에 자기 검사가 꺼냈다) — 키 **이름 자리가 ASCII 뿐**이다(`[A-Za-z0-9_.+-]*`).
한글이 섞인 키는 «키 꼴» 로도 안 보인다. 실측: 지금 키 738개 중 그 집합을 벗어나는 것 **0개** ⇒ 살아 있는 구멍은 아니다.

사용: python3 tools/check_catalog_keys.py        # 문제 있으면 exit 1
      python3 tools/check_catalog_keys.py --self-test
"""
import json, os, re, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
SPEC = os.path.join(ROOT, 'Assets', 'KkomaKnight', 'catalog.json')
SCRIPTS = os.path.join(ROOT, 'Assets', 'Scripts')
SECTIONS = ('sprites', 'prefabs', 'controllers', 'materials', 'fonts', 'colors', 'texts', 'audio')
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
    if '--self-test' in sys.argv[1:] or '--selftest' in sys.argv[1:]:
        return self_test()
    return run()


def run():
    """실제 검사 한 판 — `main()` 과 나눠 둔 까닭은 자기 검사가 **이것만** 부르기 위해서다
    (`main()` 을 부르면 `sys.argv` 를 다시 읽어 스스로를 끝없이 부른다 · 세우다 한 번 밟았다)."""
    keys = load_keys()
    prefixes = {k.split('.')[0] for k in keys}
    lit_re = re.compile(r'"((?:' + '|'.join(sorted(map(re.escape, prefixes))) + r')\.[A-Za-z0-9_.+-]*)"')
    missing = []; seen = 0; files = 0
    for dp, _, fs in os.walk(SCRIPTS):
        for fn in sorted(fs):
            if not fn.endswith('.cs'): continue
            files += 1
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
    # ⛑ T505 — **공허 방지**. 길이 어긋나거나(`SCRIPTS` 가 옮겨졌다) 정규식이 고장 나도 결과는 똑같이 «누락 0건» 이라,
    #   그 둘을 가르는 줄이 없으면 이 자는 «조용한 거짓» 이 된다 — 그런데 이 자는 **막는 자**라(T12 · 플레이 콘솔 에러 0)
    #   조용히 거짓이면 없는 키가 그대로 실린다. 실측(2026-09-12): 파일 100개 · 리터럴 1232개 · 키 737개.
    #   문턱은 그 수의 **한참 아래**로 둔다 — 재는 값이 아니라 «아무것도 안 읽었다» 를 잡는 값이다(결정 930).
    if files < 20 or seen < 50:
        print(f'✗ catalog 키 검사가 **아무것도 못 읽었다** — .cs {files}개 · 카탈로그 꼴 리터럴 {seen}개(키 {len(keys)}개). '
              f'«누락 0건» 이 아니라 «못 쟀다» 다(T505 공허 방지) — 경로({SCRIPTS})와 catalog.json 을 보라.')
        return 1
    print(f'catalog 키 검사 OK — 리터럴 {seen}개 전부 catalog.json 에 있음 (키 {len(keys)}개 · .cs {files}개)')
    return 0


# ────────────────────────────── 자기 검사 (T505) ──────────────────────────────

def _run_on(tmp, catalog, files):
    """가짜 저장소 하나를 세우고 이 자를 그 위에서 돌린다 → (rc, 찍은 글)."""
    import io as _io, contextlib
    os.makedirs(os.path.join(tmp, 'Assets', 'KkomaKnight'), exist_ok=True)
    os.makedirs(os.path.join(tmp, 'Assets', 'Scripts'), exist_ok=True)
    with open(os.path.join(tmp, 'Assets', 'KkomaKnight', 'catalog.json'), 'w', encoding='utf-8') as fh:
        json.dump(catalog, fh, ensure_ascii=False)
    for name, src in files.items():
        with open(os.path.join(tmp, 'Assets', 'Scripts', name), 'w', encoding='utf-8') as fh:
            fh.write(src)
    g = globals(); old = (g['ROOT'], g['SPEC'], g['SCRIPTS'])
    g['ROOT'] = tmp
    g['SPEC'] = os.path.join(tmp, 'Assets', 'KkomaKnight', 'catalog.json')
    g['SCRIPTS'] = os.path.join(tmp, 'Assets', 'Scripts')
    buf = _io.StringIO()
    try:
        with contextlib.redirect_stdout(buf):
            rc = run()
    finally:
        g['ROOT'], g['SPEC'], g['SCRIPTS'] = old
    return rc, buf.getvalue()


def self_test():
    """⚑ **이 자에는 자기 검사가 없었다**(T492 가 «자기 검사 없는 자 다섯» 으로 세어 둔 그 하나).

    막는 자이면서 조용히 고장 날 수 있는 자리는 «누락 0건» 하나뿐이라, 갈래마다 **다른 줄**에서 울게 세운다.
    ⚠ 갈래 ⓖ 는 «고쳐야 할 것» 이 아니라 **한계를 못 박는 것**이다 — 머리글의 그 설명과 한 몸이다.
    """
    import shutil, tempfile
    # 넉넉한 밑동 — 공허 방지 문턱(파일 20 · 리터럴 50)을 넘기려고 가짜 **파일 25개**를 채운다.
    #   ⚠ 세우다 한 번 밟았다: 리터럴만 채우고 **파일 하나**에 몰아 두면 `files < 20` 에 걸려
    #     갈래 일곱이 전부 «못 쟀다» 로 빨개진다 — 밑동이 문턱을 못 넘으면 재려던 것을 못 잰다.
    NFILL = 25
    CAT = {'sprites': {'ui.k%d' % i: 'x' for i in range(NFILL * 3)}, 'audio': {'snd.hit': 'x'},
           'prefabs': {'env.tree': 'x', 'env.rock': 'x'}, 'colors': {'ui.cardFrame.blue': 'x'}}
    def files(extra):
        d = dict(extra)
        for i in range(NFILL):
            d['_fill%d.cs' % i] = 'class F%d { string[] s = { "ui.k%d", "ui.k%d", "ui.k%d" }; }' % (
                i, i * 3, i * 3 + 1, i * 3 + 2)
        return d

    cases = [
        ('ⓐ 아는 키는 조용하다', {'A.cs': 'class A { void M(){ Sprite("snd.hit"); } }'}, 0, None),
        # ⚠ 없는 키를 **ASCII** 로 적는다 — 세우다 여기서 한 번 걸렸다: `lit_re` 의 이름 자리가
        #   `[A-Za-z0-9_.+-]*` 라 «snd.없는것» 은 **키 꼴로도 안 보인다**(그래서 안 울었다).
        #   실측으로 확인했다: 지금 카탈로그 키 738개 중 그 글자 집합을 벗어나는 것 **0개** ⇒ 살아 있는 구멍은 아니다.
        ('ⓑ 접두는 아는데 키가 없으면 **파일·줄**과 함께 운다',
         {'B.cs': 'class B {\n  void M(){ Sprite("snd.nope"); }\n}'}, 1, 'B.cs:2'),
        ('ⓒ 점으로 끝나는 «접두 조립» 은 그 접두로 시작하는 키가 있으면 조용하다',
         {'C.cs': 'class C { void M(){ Sprite("env." + n); } }'}, 0, None),
        ('ⓓ 밑동 + «.색» 꼴도 조용하다(`ui.cardFrame` ← `ui.cardFrame.blue`)',
         {'D.cs': 'class D { void M(){ Frame("ui.cardFrame", c); } }'}, 0, None),
        ('ⓔ 주석 속 키는 안 센다 — 그 줄을 세면 «문서에 적었다» 가 빨강이 된다',
         {'E.cs': 'class E { // Sprite("snd.없는것") 을 쓰지 마라\n  void M(){} }'}, 0, None),
        ('ⓕ 데이터 파일 이름(`ui.json`)은 키가 아니다',
         {'F.cs': 'class F { void M(){ Load("ui.json"); } }'}, 0, None),
        ('ⓖ ⚠ **한계** — 아예 새 접두(`vfx.*`)는 **안 걸린다**(머리글에 적어 둔 그 자리)',
         {'G.cs': 'class G { void M(){ Sprite("vfx.hitGlow"); } }'}, 0, None),
    ]
    bad = 0
    tmp = tempfile.mkdtemp(prefix='catkeys-')
    try:
        for name, extra, want_rc, want_in in cases:
            d = os.path.join(tmp, name.split()[0]); os.makedirs(d, exist_ok=True)
            rc, out = _run_on(d, CAT, files(extra))
            ok = (rc == want_rc) and (want_in is None or want_in in out)
            bad += 0 if ok else 1
            print(f"  {'✔' if ok else '✘'} {name} (rc {rc}, 기대 {want_rc})")
        # ⓗ 공허 방지 — 소스가 거의 없으면 «누락 0» 이 아니라 «못 쟀다» 다.
        d = os.path.join(tmp, 'h'); os.makedirs(d, exist_ok=True)
        rc, out = _run_on(d, CAT, {'One.cs': 'class One { void M(){ Sprite("snd.hit"); } }'})
        ok = rc == 1 and '못 읽었다' in out
        bad += 0 if ok else 1
        print(f"  {'✔' if ok else '✘'} ⓗ 읽은 것이 거의 없으면 «누락 0» 이 아니라 **«못 쟀다»** 로 운다 (rc {rc}, 기대 1)")
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    if bad:
        print(f'✗ check_catalog_keys --self-test: 갈래 {bad}건이 기대와 다르다')
        return 1
    print(f'✓ check_catalog_keys --self-test: 갈래 {len(cases) + 1}개가 전부 기대대로 갈린다 '
          f'(ⓖ 는 «못 본다» 를 못 박은 갈래다 — 고침이 아니라 한계다)')
    return 0

if __name__ == '__main__':
    sys.exit(main())
