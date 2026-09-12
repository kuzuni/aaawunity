#!/usr/bin/env python3
"""오디오 게이트 (T64) — WebGL 에서 소리가 나는 임포트 설정인가.

**무엇이 기준인가**: 주인이 2026-09-07 04:3X 에 «webgl 오디오 잘 들린다» 고 확인한 배포
(gh-pages 가 서비스하던 CI #148 = main `fc9fe35`)의 설정 = **`compressionFormat: 7`(AAC)**.
그 뒤 회차 3(`9ed1c7a`)이 이 설정을 Vorbis(1)로 되돌렸다가 회차 5(`이 커밋`)에서 복원했다 —
같은 사고를 막으려고 게이트로 굳힌다.

**왜 AAC 여야 하나** (배포 빌드 `KkomaKnight.framework.js` 실측):
  · `_JS_Sound_Load(ptr, length, decompress, fmodSoundType)` 는 `length < 131072` 면 `decodeAudioData`,
    아니면 `<audio>` 요소 + `jsAudioGetMimeTypeFromType`(13 → audio/mpeg · 20 → audio/wav · **나머지 → audio/mp4**).
    즉 유니티 WebGL 은 **AAC 를 전제**하고 데이터를 브라우저에 넘긴다.
  · Vorbis 로 두면 FSB 안의 raw Vorbis(Ogg 프레이밍 없음)가 넘어가 브라우저가 못 읽는다.
  · PCM(0)·ADPCM(2)은 빌드가 반영하지 않는다(회차 1 실측: `.data` 크기가 그대로였다).

**주의 — 워커 환경의 headless chromium 으로는 이 설정을 판정할 수 없다**:
headless 는 AAC/MP4 코덱이 없어 AAC 가 제대로 실려 있어도 «no supported source»·«Unable to decode
audio data» 를 찍는다(회차 2 를 «실패» 로 잘못 읽은 원인 · 결정 300). 실기(주인 폰·데스크톱 크롬)가
판정 도구다. 그래서 이 게이트는 «소리가 나는가» 가 아니라 «주인이 확인한 설정이 유지되는가» 를 지킨다.

**⚠ 이 자가 못 보는 자리 둘** (T506 이 자기 검사를 세우며 글자로 박았다 — 몰라서 못 보는 것과 알고 못 보는 것은 다르다):
  · **`Assets/Audio/` 밖의 오디오는 한 건도 안 본다.** 실측(2026-09-12): `Assets` 안의 `AudioImporter` meta 20개가
    전부 `Assets/Audio/`(bgm 3 · sfx 17) 밑이고 그 밖에는 오디오 파일 자체가 0개 ⇒ **지금은 안 샌다.**
    새 소리를 딴 자리에 두는 날 이 자는 조용히 «전부 AAC» 라고 말한다.
  · **WebGL 아닌 플랫폼 오버라이드(그룹 13 이 아닌 것)는 일부러 안 본다** — 이 게이트의 물음은 «WebGL 에서 들리나» 다.

사용: python3 tools/check_audio_webgl.py             # 0 = 통과 · 1 = 위반 목록
     python3 tools/check_audio_webgl.py --self-test # 이 자가 갈래마다 제대로 우는가
"""
import glob, os, re, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
WANT = 7          # AAC — 주인 확인 설정(CI #148)
NAMES = {0: 'PCM', 1: 'Vorbis', 2: 'ADPCM', 3: 'MP3', 7: 'AAC'}


def default_format(text):
    m = re.search(r'^  defaultSettings:\n((?:    .*\n)+)', text, re.M)
    if not m:
        return None
    f = re.search(r'^    compressionFormat: (\d+)$', m.group(1), re.M)
    return int(f.group(1)) if f else None


def main():
    if '--self-test' in sys.argv[1:] or '--selftest' in sys.argv[1:]:
        return self_test()
    return run()


def run():
    """실제 검사 한 판 — `main()` 과 나눠 둔 까닭은 자기 검사가 **이것만** 부르기 위해서다
    (`main()` 을 부르면 `sys.argv` 를 다시 읽어 스스로를 끝없이 부른다 · `check_catalog_keys` 가 세우다 밟은 자리)."""
    bad, checked = [], 0
    for meta in sorted(glob.glob(os.path.join(ROOT, 'Assets', 'Audio', '**', '*.meta'), recursive=True)):
        text = open(meta, encoding='utf-8').read()
        if 'AudioImporter:' not in text:
            continue
        checked += 1
        fmt = default_format(text)
        if fmt != WANT:
            bad.append('%s: compressionFormat %s(%s) — 주인이 «잘 들린다» 고 확인한 설정은 %d(AAC) 다 (T64 · 결정 300)'
                       % (os.path.relpath(meta, ROOT), fmt, NAMES.get(fmt, '?'), WANT))
        # WebGL 오버라이드(BuildTargetGroup 13)가 생기면 그것도 AAC 여야 한다.
        for group, block in re.findall(r'^    (\d+):\n((?:      .*\n)+)', text, re.M):
            g = re.search(r'^      compressionFormat: (\d+)$', block, re.M)
            if group == '13' and g and int(g.group(1)) != WANT:
                bad.append('%s: WebGL 오버라이드 compressionFormat %s — 같은 이유로 %d(AAC) 여야 한다'
                           % (os.path.relpath(meta, ROOT), g.group(1), WANT))
    if not checked:
        print('✗ check_audio_webgl: Assets/Audio 에서 AudioImporter meta 를 못 찾았다 (경로가 바뀌었나)')
        return 1
    if bad:
        print('✗ check_audio_webgl: WebGL 오디오 설정이 어긋난 파일 %d건 (T64)' % len(bad))
        for b in bad:
            print('  - ' + b)
        return 1
    print('✓ check_audio_webgl: 오디오 %d개 전부 AAC(주인 확인 설정 · T64)' % checked)
    return 0


META = """fileFormatVersion: 2
guid: 0123456789abcdef0123456789abcdef
AudioImporter:
  externalObjects: {}
  serializedVersion: 7
  defaultSettings:
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: %(fmt)d
    quality: 1
    conversionMode: 0
%(over)s  preloadAudioData: 0
"""

OVERRIDE = """  platformSettingOverrides:
    %(group)d:
      serializedVersion: 3
      loadType: 0
      sampleRateSetting: 0
      sampleRateOverride: 44100
      compressionFormat: %(fmt)d
      quality: 1
      conversionMode: 0
"""


def _meta(fmt=WANT, over_group=None, over_fmt=WANT):
    over = '' if over_group is None else OVERRIDE % {'group': over_group, 'fmt': over_fmt}
    return META % {'fmt': fmt, 'over': over}


def _run_on(tmp, metas):
    """가짜 저장소 하나를 세우고 이 자를 그 위에서 돌린다 → (rc, 찍은 글)."""
    import io as _io, contextlib
    audio = os.path.join(tmp, 'Assets', 'Audio', 'sfx')
    os.makedirs(audio, exist_ok=True)
    for name, text in metas.items():
        with open(os.path.join(audio, name), 'w', encoding='utf-8') as fh:
            fh.write(text)
    g = globals(); old = g['ROOT']
    g['ROOT'] = tmp
    buf = _io.StringIO()
    try:
        with contextlib.redirect_stdout(buf):
            rc = run()
    finally:
        g['ROOT'] = old
    return rc, buf.getvalue()


def self_test():
    """⚑ **이 자에는 자기 검사가 없었다** — T492 가 «자기 검사 없는 자 다섯» 으로 세어 둔 그 하나다.

    이 자는 **막는 자**다(`ci.yml` 의 `dotnet` 잡 · `continue-on-error` 없음 · `unity-test` 가 그 잡에 매달려 있다).
    조용히 고장 나면 런은 초록인데 **아무것도 안 재고** 지나가고, 그러면 주인 폰에서 소리가 안 난다 —
    이 게이트가 생긴 까닭이 바로 그 사고였다(회차 3 이 Vorbis 로 되돌린 일 · 결정 300).

    ⚠ 갈래 ⓖ 는 «고쳐야 할 것» 이 아니라 **한계를 못 박는 것**이다 — 머리글의 «못 보는 자리 둘» 과 한 몸이다.
    """
    import shutil, tempfile
    FILL = {'fill%02d.wav.meta' % i: _meta() for i in range(6)}   # 밑동 — 늘 성한 소리 여섯

    def metas(extra):
        d = dict(FILL); d.update(extra); return d

    cases = [
        ('ⓐ 전부 AAC 면 조용하다', {}, 0, '전부 AAC'),
        ('ⓑ 기본값이 Vorbis 면 **파일 이름과 낱말**로 운다',
         {'bad.wav.meta': _meta(fmt=1)}, 1, 'bad.wav.meta'),
        ('ⓑ² 그 울음에 «Vorbis» 라는 낱말이 선다(숫자만 찍으면 읽는 사람이 한 번 더 찾는다)',
         {'bad.wav.meta': _meta(fmt=1)}, 1, 'Vorbis'),
        ('ⓒ 기본값이 PCM(0)이어도 운다 — 빌드가 반영조차 안 하는 값이다',
         {'pcm.wav.meta': _meta(fmt=0)}, 1, 'PCM'),
        ('ⓓ WebGL 오버라이드(그룹 13)가 AAC 아니면 운다 — 기본값이 성해도 그쪽이 이긴다',
         {'ov.wav.meta': _meta(fmt=WANT, over_group=13, over_fmt=1)}, 1, 'WebGL 오버라이드'),
        ('ⓔ WebGL 오버라이드가 AAC 면 조용하다',
         {'ov.wav.meta': _meta(fmt=WANT, over_group=13, over_fmt=WANT)}, 0, '전부 AAC'),
        ('ⓕ **WebGL 아닌** 플랫폼 오버라이드(그룹 1)가 Vorbis 여도 안 운다 — 이 자의 물음이 아니다',
         {'ov.wav.meta': _meta(fmt=WANT, over_group=1, over_fmt=1)}, 0, '전부 AAC'),
        ('ⓖ ⚠ **한계** — `AudioImporter:` 가 없는 meta 는 안 센다(텍스처 meta 가 섞여도 «오디오» 수가 안 부푼다)',
         {'tex.png.meta': 'fileFormatVersion: 2\nTextureImporter:\n  compressionFormat: 1\n'}, 0, '오디오 6개'),
    ]
    bad = 0
    tmp = tempfile.mkdtemp(prefix='audiowebgl-')
    try:
        for i, (name, extra, want_rc, want_in) in enumerate(cases):
            d = os.path.join(tmp, 'c%02d' % i)
            rc, out = _run_on(d, metas(extra))
            ok = (rc == want_rc) and (want_in is None or want_in in out)
            bad += 0 if ok else 1
            print("  %s %s (rc %d, 기대 %d)" % ('✔' if ok else '✘', name, rc, want_rc))
        # ⓗ 공허 방지 — 한 건도 못 읽었으면 «전부 AAC» 가 아니라 «못 찾았다» 다.
        #   길이 바뀌거나 glob 이 고장 나도 결과는 «위반 0건» 이라, 그 둘을 가르는 줄이 없으면 이 자는 조용한 거짓이 된다
        #   (결정 1379 가 치른 값 · T505 의 ⓗ 와 같은 손).
        rc, out = _run_on(os.path.join(tmp, 'empty'), {})
        ok = rc == 1 and '못 찾았다' in out
        bad += 0 if ok else 1
        print("  %s ⓗ 한 건도 못 읽었으면 «전부 AAC» 가 아니라 **«못 찾았다»** 로 운다 (rc %d, 기대 1)" % ('✔' if ok else '✘', rc))
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    if bad:
        print('✗ check_audio_webgl --self-test: 갈래 %d건이 기대와 다르다' % bad)
        return 1
    print('✓ check_audio_webgl --self-test: 갈래 %d개가 전부 기대대로 갈린다 '
          '(ⓕ·ⓖ 는 «일부러 안 본다» 를 못 박은 갈래다 — 고침이 아니라 한계다)' % (len(cases) + 1))
    return 0


if __name__ == '__main__':
    sys.exit(main())
