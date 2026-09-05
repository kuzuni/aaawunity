#!/usr/bin/env python3
"""Assets/ 아래 .meta 를 텍스트로 생성한다 (유니티 에디터 없이 작업하는 워커용).

- GUID = md5(에셋 경로) 의 16진 32자 — 결정적이라 어느 워커가 만들어도 같은 값이 나온다.
- 이미 있는 .meta 는 절대 덮어쓰지 않는다 (유니티가 나중에 재직렬화한 것도 보존).
- 대응 에셋이 사라진 고아 .meta 는 지운다.
- `--check` 는 빠진/고아 .meta 가 있으면 exit 1 (CI 게이트).

사용:  python3 tools/gen_meta.py [--check]
"""
import hashlib, os, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
ASSETS = os.path.join(ROOT, 'Assets')

def guid(rel):
    return hashlib.md5(rel.replace('\\', '/').encode('utf-8')).hexdigest()

TAIL = "  userData: \n  assetBundleName: \n  assetBundleVariant: \n"

def body(rel, is_dir):
    if is_dir:
        return "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n" + TAIL
    ext = os.path.splitext(rel)[1].lower()
    if ext == '.cs':
        return ("MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n"
                "  executionOrder: 0\n  icon: {instanceID: 0}\n" + TAIL)
    if ext == '.asmdef':
        return "AssemblyDefinitionImporter:\n  externalObjects: {}\n" + TAIL
    if ext in ('.json', '.txt', '.md', '.csv'):
        return "TextScriptImporter:\n  externalObjects: {}\n" + TAIL
    if ext in ('.ttf', '.otf'):
        name = os.path.splitext(os.path.basename(rel))[0].split('-')[0]
        return ("TrueTypeFontImporter:\n  externalObjects: {}\n  serializedVersion: 4\n  fontSize: 16\n"
                "  forceTextureCase: -2\n  characterSpacing: 0\n  characterPadding: 1\n  includeFontData: 1\n"
                f"  fontNames:\n  - {name}\n  fallbackFontReferences: []\n  customCharacters: \n"
                "  fontRenderingMode: 0\n  ascentCalculationMode: 1\n  useLegacyBoundsCalculation: 0\n"
                "  shouldRoundAdvanceValue: 1\n" + TAIL)
    return "DefaultImporter:\n  externalObjects: {}\n" + TAIL

def main():
    check = '--check' in sys.argv
    missing, orphans = [], []
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        dirnames.sort(); filenames.sort()
        entries = [(d, True) for d in dirnames] + [(f, False) for f in filenames if not f.endswith('.meta')]
        for name, is_dir in entries:
            full = os.path.join(dirpath, name)
            rel = os.path.relpath(full, ROOT).replace('\\', '/')
            meta = full + '.meta'
            if not os.path.exists(meta):
                missing.append(rel)
                if not check:
                    with open(meta, 'w', encoding='utf-8', newline='\n') as f:
                        f.write(f"fileFormatVersion: 2\nguid: {guid(rel)}\n" + body(rel, is_dir))
        for f in filenames:
            if f.endswith('.meta') and not os.path.exists(os.path.join(dirpath, f[:-5])):
                # 폴더 .meta 인데 폴더가 없는 것은 «빈 폴더» 다 (git 은 빈 폴더를 안 담는다) — 고아가 아니다.
                with open(os.path.join(dirpath, f), encoding='utf-8', errors='ignore') as fh:
                    if 'folderAsset: yes' in fh.read(400):
                        continue
                orphans.append(os.path.relpath(os.path.join(dirpath, f), ROOT))
                if not check:
                    os.remove(os.path.join(dirpath, f))
    if check:
        for m in missing: print('missing .meta:', m)
        for o in orphans: print('orphan .meta:', o)
        sys.exit(1 if (missing or orphans) else 0)
    print(f'generated {len(missing)} .meta, removed {len(orphans)} orphan(s)')

if __name__ == '__main__':
    main()
