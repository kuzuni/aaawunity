#!/usr/bin/env python3
"""오디오 게이트 (T64) — Assets/Audio 의 클립이 WebGL 에서 소리가 나는 형식인지.

왜: 유니티 WebGL 은 **압축 오디오를 브라우저에게 그대로 넘긴다**. 배포된 빌드의
`KkomaKnight.framework.js` 를 읽어 확인한 사실 두 가지 —
  ① `_JS_Sound_Load(ptr, length, decompress, fmodSoundType)`
     · `length < 131072`(128KB) 이면 `decompress` 를 1 로 강제해 `audioContext.decodeAudioData(데이터)`
     · 아니면 `new Blob([데이터], {type: jsAudioGetMimeTypeFromType(형식)})` 를 `<audio>` 요소에 물린다
       (`jsAudioGetMimeTypeFromType`: 13 → audio/mpeg · 20 → audio/wav · **그 밖 전부 audio/mp4**)
  ② `_JS_Sound_Load_PCM(channels, length, sampleRate, ptr)` — 비압축(PCM) 클립은 브라우저 코덱을
     **안 거치고** Web Audio 버퍼로 바로 들어간다.
즉 Vorbis(FSB 안의 raw Vorbis · Ogg 프레이밍 없음)는 두 경로 어디서도 못 읽어
BGM 은 «NotSupportedError: no supported source»(<audio>), SFX 는 «EncodingError: Unable to decode
audio data» + «Loading FSB failed» 가 났다(T59/T60 스모크 · 주인 폰에서도 빨간 배너).

그래서 허용 형식은 **브라우저가 읽을 수 있는 것** 뿐이다:
  0 = PCM(비압축 · Load_PCM 경로 · 코덱 무관) · 3 = MP3(audio/mpeg) · 7 = AAC(audio/mp4)
금지: 1 = Vorbis · 2 = ADPCM (둘 다 브라우저 디코더가 없다 · ADPCM 은 유니티 WebGL 런타임에도 없다)

사용: python3 tools/check_audio_webgl.py        # 0 = 통과 · 1 = 위반 목록 출력
"""
import glob, os, re, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
OK_FORMATS = {0: 'PCM', 3: 'MP3', 7: 'AAC'}
BAD_FORMATS = {1: 'Vorbis', 2: 'ADPCM'}


def default_settings(text):
    """`defaultSettings:` 블록의 숫자 필드 → dict."""
    m = re.search(r'^  defaultSettings:\n((?:    .*\n)+)', text, re.M)
    if not m:
        return {}
    return {k: float(v) for k, v in re.findall(r'^    (\w+): ([-\d.]+)$', m.group(1), re.M)}


def main():
    bad, checked = [], 0
    for meta in sorted(glob.glob(os.path.join(ROOT, 'Assets', 'Audio', '**', '*.meta'), recursive=True)):
        text = open(meta, encoding='utf-8').read()
        if 'AudioImporter:' not in text:
            continue
        checked += 1
        rel = os.path.relpath(meta, ROOT)
        d = default_settings(text)
        fmt = int(d.get('compressionFormat', -1))
        if fmt not in OK_FORMATS:
            bad.append(f'{rel}: compressionFormat {fmt}'
                       f'({BAD_FORMATS.get(fmt, "알 수 없음")}) — WebGL 에서 소리가 안 난다 '
                       f'(허용: {", ".join(f"{k}={v}" for k, v in OK_FORMATS.items())})')
        # 플랫폼 오버라이드가 생기면 그것도 같은 규칙(WebGL = BuildTargetGroup 13)
        for group, block in re.findall(r'^    (\d+):\n((?:      .*\n)+)', text, re.M):
            gf = re.search(r'^      compressionFormat: (\d+)$', block, re.M)
            if group == '13' and gf and int(gf.group(1)) not in OK_FORMATS:
                bad.append(f'{rel}: WebGL 오버라이드 compressionFormat {gf.group(1)} — 같은 이유로 금지')
    if bad:
        print('✗ check_audio_webgl: WebGL 에서 못 읽는 오디오 형식 %d건 (T64)' % len(bad))
        for b in bad:
            print('  - ' + b)
        return 1
    print('✓ check_audio_webgl: 오디오 %d개 전부 WebGL 이 읽는 형식 (PCM/MP3/AAC · T64)' % checked)
    return 0


if __name__ == '__main__':
    sys.exit(main())
