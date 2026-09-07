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

사용: python3 tools/check_audio_webgl.py        # 0 = 통과 · 1 = 위반 목록
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


if __name__ == '__main__':
    sys.exit(main())
