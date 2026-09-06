#!/usr/bin/env python3
"""오디오 게이트 (T64) — WebGL 에서 소리가 나려면 지켜야 하는 불변식.

**왜 임포트 설정이 아니라 이걸 보나** (두 회차의 배포 실측 · PROGRESS T64 진행 기록):
  · 유니티 WebGL 은 압축 오디오를 **브라우저에게 그대로 넘긴다** — 배포 빌드의 `KkomaKnight.framework.js` 에서
    `_JS_Sound_Load(ptr, length, decompress, fmodSoundType)` 는 `length < 131072` 면 `decodeAudioData` 를 강제하고
    아니면 `<audio>` 요소에 `jsAudioGetMimeTypeFromType`(13 → audio/mpeg · 20 → audio/wav · 나머지 → audio/mp4) 로 물린다.
  · 그런데 이 프로젝트의 WebGL 빌드는 임포터의 `compressionFormat` 을 **반영하지 않는다** — 회차 1(PCM 0)·회차 2(AAC 7)
    둘 다 무시되고 소스 Vorbis 가 그대로 실렸다(`loadType` 만 반영됐다 = 재임포트는 되고 있다).
    FSB 안의 raw Vorbis 는 Ogg 프레이밍이 없어 브라우저가 못 읽는다 → «no supported source» · «Unable to decode audio data».
  · 그래서 회차 3 은 유니티 오디오 파이프라인을 **우회**한다: `Assets/StreamingAssets/audio/**.ogg` 원본을 런타임에
    `UnityWebRequestMultimedia` 로 받아(브라우저가 **Ogg 컨테이너째** 디코드한다) 카탈로그 클립 대신 쓴다
    (`Game/Audio.cs` 의 `AudioManager.LoadStreamed` · WebGL 에서만 돈다).

이 게이트가 지키는 것: **카탈로그의 `bgm.*`·`snd.*` 키마다 StreamingAssets 원본 파일이 있어야 한다**
(`Audio.cs` 의 `StreamedFile()` 과 같은 규칙: `bgm.lobby` → `audio/bgm/lobby.ogg` · `snd.click` → `audio/sfx/click.ogg`).
파일이 없으면 그 소리는 WebGL 에서 조용히 안 난다(에러는 아니라 아무도 모른다) — 그래서 CI 에서 막는다.

사용: python3 tools/check_audio_webgl.py        # 0 = 통과 · 1 = 위반 목록
"""
import json, os, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
CATALOG = os.path.join(ROOT, 'Assets', 'KkomaKnight', 'catalog.json')
STREAM = os.path.join(ROOT, 'Assets', 'StreamingAssets')


def streamed_file(key):
    """Audio.cs 의 AudioManager.StreamedFile 과 같은 규칙."""
    if '.' not in key:
        return None
    head, name = key.split('.', 1)
    if not name:
        return None
    return 'audio/%s/%s.ogg' % ('bgm' if head == 'bgm' else 'sfx', name)


def audio_keys():
    doc = json.load(open(CATALOG, encoding='utf-8'))
    out = {}
    def walk(node):
        if isinstance(node, dict):
            for k, v in node.items():
                if isinstance(v, str) and (k.startswith('bgm.') or k.startswith('snd.')):
                    out[k] = v
                else:
                    walk(v)
        elif isinstance(node, list):
            for v in node:
                walk(v)
    walk(doc)
    return out


def main():
    keys = audio_keys()
    if not keys:
        print('✗ check_audio_webgl: catalog.json 에서 bgm.*/snd.* 키를 못 찾았다 (경로·형식이 바뀌었나)')
        return 1
    bad = []
    for key, src in sorted(keys.items()):
        rel = streamed_file(key)
        if rel is None:
            bad.append('%s: 키 이름이 «묶음.이름» 꼴이 아니라 StreamingAssets 경로를 만들 수 없다' % key)
            continue
        path = os.path.join(STREAM, rel.replace('/', os.sep))
        if not os.path.isfile(path):
            bad.append('%s(%s): Assets/StreamingAssets/%s 없음 — WebGL 에서 이 소리는 조용히 안 난다 (원본 .ogg 를 그 자리에 복사하고 python3 tools/gen_meta.py)' % (key, src, rel))
        elif os.path.getsize(path) == 0:
            bad.append('%s: Assets/StreamingAssets/%s 가 0바이트' % (key, rel))
    extra = []
    for folder in ('bgm', 'sfx'):
        d = os.path.join(STREAM, 'audio', folder)
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if not f.endswith('.ogg'):
                continue
            key = ('bgm.' if folder == 'bgm' else 'snd.') + f[:-4]
            if key not in keys:
                extra.append('audio/%s/%s (카탈로그에 %s 키가 없다 — 쓰지 않는 파일이면 지운다)' % (folder, f, key))
    if bad:
        print('✗ check_audio_webgl: WebGL 원본 오디오 %d건 빠짐 (T64)' % len(bad))
        for b in bad:
            print('  - ' + b)
        return 1
    msg = '✓ check_audio_webgl: 카탈로그 오디오 %d개 전부 StreamingAssets 원본 있음 (T64 · WebGL 은 이 원본을 받아 쓴다)' % len(keys)
    if extra:
        msg += '\n  · 참고(실패는 아님): 카탈로그에 없는 원본 %d개 — %s' % (len(extra), ' · '.join(extra))
    print(msg)
    return 0


if __name__ == '__main__':
    sys.exit(main())
