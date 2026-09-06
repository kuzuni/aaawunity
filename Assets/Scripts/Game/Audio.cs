using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 배경음(BGM)·효과음(SFX) 관리자 (T28 · 주인 2026-09-06 «인터넷에서 받아서 넣어라»).
    /// 클립은 카탈로그 <c>bgm.*</c>/<c>snd.*</c>(Assets/Audio/*.ogg · 전부 CC0 · 출처는 Assets/Audio/LICENSES.md) — <see cref="App.Create"/> 가 <see cref="Create"/> 로 세운다.
    /// ● <see cref="Bgm"/>: 같은 곡이면 무시 · 다른 곡이면 0.5초 크로스페이드(AudioSource 2개를 번갈아) · 루프. 화면 전환(<see cref="App.ShowScreen"/>)이 로비/전투 곡을 고르고
    ///   보스 등장(<see cref="BattleWorld"/> BossWarn)이 boss 곡으로 바꾼다. 배속 x2 여도 피치 그대로(엔진 배속은 Time.timeScale 이 아니라 틱 수 — 소리는 영향 없음).
    /// ● <see cref="Sfx"/>: PlayOneShot(풀 1개 · 겹침 허용) · 볼륨 · 피치 지터(±5%). 클립이 없으면 조용히 넘어간다(에러 0 · 카탈로그 경고만).
    /// ● 음소거 = 세이브(<see cref="Core.SaveData.MuteBgm"/>·<see cref="Core.SaveData.MuteSfx"/>) — 설정 팝업(Settings 프리팹)의 BGM/SFX 스위치(<see cref="Overlay"/>)가 바꾸고 <see cref="ApplyMute"/> 로 반영.
    /// ● WebGL 은 첫 사용자 입력 뒤에야 소리가 난다(브라우저 AudioContext 정책) — <see cref="Wake"/> 를 START 클릭에서 불러 잠들어 있던 BGM 을 다시 Play 한다(유니티가 컨텍스트를 깨운 뒤 재생이 붙는다).
    /// 순수 정적 API + MonoBehaviour 1개(<see cref="AudioManager"/>) — 테스트는 <see cref="CurrentBgm"/>·<see cref="LastSfx"/> 로 확인한다.
    /// </summary>
    public static class Audio
    {
        public const float CrossFadeSec = 0.5f;
        public const float DefaultPitchJitter = 0.05f;
        public const float BgmVolume = 0.55f;

        static AudioManager _m;
        public static bool Ready => _m != null;
        /// <summary>지금 재생 중(또는 페이드 인 중)인 배경음 키 — 없으면 null.</summary>
        public static string CurrentBgm => _m != null ? _m.CurrentKey : null;
        /// <summary>마지막으로 요청된 효과음 키(클립 유무와 무관) — 테스트용.</summary>
        public static string LastSfx { get; private set; }
        /// <summary>실제로 재생된 효과음 수(클립이 있어서 PlayOneShot 까지 간 것) — 테스트용.</summary>
        public static int SfxPlayed { get; private set; }

        public static AudioManager Create(App app)
        {
            if (_m != null) return _m;
            var go = new GameObject("Audio");
            if (app != null) go.transform.SetParent(app.transform, false);
            _m = go.AddComponent<AudioManager>();
            _m.App = app;
            ApplyMute();
            return _m;
        }
        /// <summary>App 이 파괴될 때(테스트 Shutdown) 정적 참조를 놓는다 — 다음 App 이 새로 만든다.</summary>
        internal static void Release(AudioManager m) { if (_m == m) _m = null; }

        public static void Bgm(string key)
        {
            if (_m == null) return;
            _m.PlayBgm(key);
        }
        public static void StopBgm() { if (_m != null) _m.PlayBgm(null); }

        public static void Sfx(string key, float volume = 1f, float pitchJitter = DefaultPitchJitter)
        {
            LastSfx = key;
            if (_m == null || string.IsNullOrEmpty(key)) return;
            if (_m.PlaySfx(key, volume, pitchJitter)) SfxPlayed++;
        }

        /// <summary>세이브의 음소거 값을 소스에 반영(BGM 은 mute · SFX 는 다음 재생부터).</summary>
        public static void ApplyMute() { if (_m != null) _m.RefreshMute(); }

        /// <summary>WebGL 첫 터치 뒤 소리 깨우기 — 잠들어 있던 BGM 소스를 다시 Play(다른 플랫폼에서는 아무 일 없음).</summary>
        public static void Wake() { if (_m != null) _m.Wake(); }

        /// <summary>테스트용 초기화(카운터).</summary>
        public static void ResetStats() { LastSfx = null; SfxPlayed = 0; }
    }

    /// <summary>AudioSource 3개(BGM A/B · SFX) 를 가진 실제 재생기 — <see cref="Audio"/> 만 부른다.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public App App;
        AudioSource _a, _b, _sfx;   // _a = 현재 곡 · _b = 페이드 아웃 중인 이전 곡 (교체 시 swap)
        float _fade;               // 남은 크로스페이드 시간(0 이면 없음)
        string _key;
        public string CurrentKey => _key;
        public AudioSource BgmSource => _a;

        /// <summary>WebGL 에서 원본 .ogg 로 받아 온 클립(키 → 클립) — 카탈로그 클립보다 우선한다(T64).</summary>
        readonly Dictionary<string, AudioClip> _streamed = new Dictionary<string, AudioClip>();
        /// <summary>
        /// StreamingAssets 의 원본 .ogg 를 받아 쓰는가 — <b>지금은 어느 플랫폼에서도 안 쓴다</b>(T64 회차 4 · 결정 217).
        /// 회차 3 은 WebGL 에서 이 경로를 켰는데, 유니티 WebGL 네이티브가 ogg <b>스트리밍</b>을 아예 거부하고
        /// «Streaming of 'ogg' on this platform is not supported» 를 빨간 줄(console.error)로 찍는다 —
        /// 키 20 × 시도 2 = 40건(CI #155·#158 의 배포 스모크 실측 · 받아 온 클립은 0/20 이라 소리에는 보탬이 0 이었다).
        /// 규칙 §1 «플레이 콘솔 에러 0» 과 배포 게이트를 같이 깨서 17:1X 이후 모든 작업이 gh-pages 에 못 올라갔다 → 껐다.
        /// StreamingAssets 원본과 아래 코드는 남겨 둔다 — 브라우저 <c>decodeAudioData</c> 를 .jslib 로 부르는 다음 길의 재료다.
        /// </summary>
        public static bool UseStreamed => false;
        /// <summary>받아 온 클립 수(테스트·로그용).</summary>
        public int StreamedCount => _streamed.Count;

        /// <summary>카탈로그 키(<c>bgm.lobby</c>·<c>snd.click</c>) → StreamingAssets 안의 파일 경로(<c>audio/bgm/lobby.ogg</c>).</summary>
        public static string StreamedFile(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            int dot = key.IndexOf('.');
            if (dot <= 0 || dot >= key.Length - 1) return null;
            string folder = key.Substring(0, dot) == "bgm" ? "bgm" : "sfx";
            return "audio/" + folder + "/" + key.Substring(dot + 1) + ".ogg";
        }

        void Awake()
        {
            _a = Make("BGM A", true); _b = Make("BGM B", true); _sfx = Make("SFX", false);
        }
        AudioSource Make(string name, bool loop)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f; s.ignoreListenerPause = true; s.volume = loop ? Audio.BgmVolume : 1f;
            return s;
        }
        void OnDestroy() { Audio.Release(this); }

        void Start() { if (UseStreamed) StartCoroutine(LoadStreamed()); }

        /// <summary>
        /// WebGL 전용(T64): 카탈로그의 오디오 키를 돌며 <c>StreamingAssets/audio/…​.ogg</c> 원본을 받아 클립으로 만든다.
        /// 유니티 WebGL 빌드는 임포터의 <c>compressionFormat</c> 을 반영하지 않고(회차 1 PCM · 회차 2 AAC 둘 다 무시 · PROGRESS T64)
        /// FSB 안의 raw Vorbis 를 브라우저에 넘겨 «no supported source»·«Unable to decode audio data» 가 났다.
        /// 원본 .ogg 는 **Ogg 컨테이너째** 넘어가므로 브라우저가 그대로 디코드한다(워커 G 가 같은 파일로 확인).
        /// 실패해도 조용히 카탈로그 클립으로 돌아간다(빨간 줄 0 — 규칙 §1).
        /// </summary>
        System.Collections.IEnumerator LoadStreamed()
        {
            if (App == null || App.Assets == null) yield break;
            var keys = new List<string>();
            foreach (var e in App.Assets.audio) if (!string.IsNullOrEmpty(e.key)) keys.Add(e.key);
            foreach (var key in keys)
            {
                string file = StreamedFile(key);
                if (string.IsNullOrEmpty(file)) continue;
                string url = Application.streamingAssetsPath + "/" + file;
                // 형식 인자는 플랫폼마다 취급이 다르다(WebGL 은 브라우저가 판단한다는 보고가 있다) — OGGVORBIS 로 먼저, 실패하면 UNKNOWN 으로 한 번 더.
                for (int attempt = 0; attempt < 2 && !_streamed.ContainsKey(key); attempt++)
                {
                    var type = attempt == 0 ? AudioType.OGGVORBIS : AudioType.UNKNOWN;
                    using (var req = UnityWebRequestMultimedia.GetAudioClip(url, type))
                    {
                        yield return req.SendWebRequest();
                        if (req.result != UnityWebRequest.Result.Success)
                        {
                            if (attempt == 1) Debug.Log("[Audio] 원본 ogg 받기 실패(카탈로그 클립으로) " + key + " · " + req.error);
                            continue;
                        }
                        var clip = DownloadHandlerAudioClip.GetContent(req);
                        if (clip == null) continue;
                        clip.name = key;
                        _streamed[key] = clip;
                        // 지금 틀고 있는 곡이면 받아 온 클립으로 바꿔 이어 튼다(첫 로비 BGM 이 이 경우다).
                        if (key == _key && _a != null && _a.clip != clip)
                        {
                            float v = _a.volume; _a.clip = clip; _a.volume = v; _a.Play();
                        }
                    }
                }
            }
            Debug.Log("[Audio] StreamingAssets 원본 클립 " + _streamed.Count + "/" + keys.Count);
        }

        bool MuteBgm => App != null && App.Save != null && App.Save.MuteBgm;
        bool MuteSfx => App != null && App.Save != null && App.Save.MuteSfx;

        public void RefreshMute()
        {
            bool m = MuteBgm;
            if (_a != null) _a.mute = m;
            if (_b != null) _b.mute = m;
            if (_sfx != null) _sfx.mute = MuteSfx;
        }

        /// <summary>받아 온 원본 클립(WebGL · T64)이 있으면 그것을, 없으면 카탈로그 클립을 준다.</summary>
        AudioClip Clip(string key)
        {
            if (!string.IsNullOrEmpty(key) && _streamed.TryGetValue(key, out var streamed) && streamed != null) return streamed;
            return App != null && App.Assets != null ? App.Assets.Clip(key) : null;
        }

        public void PlayBgm(string key)
        {
            if (key == _key) return;   // 같은 곡이면 무시(페이드도 없음)
            var clip = string.IsNullOrEmpty(key) ? null : Clip(key);
            _key = key;
            // 이전 곡 → _b 로 보내 페이드 아웃, 새 곡 → _a 에서 페이드 인 (둘 다 비어 있으면 그냥 시작)
            var old = _a; _a = _b; _b = old;
            _a.clip = clip; _a.volume = 0f; _a.pitch = 1f; _a.mute = MuteBgm;
            if (clip != null) _a.Play(); else _a.Stop();
            _fade = _b.isPlaying ? Audio.CrossFadeSec : 0f;
            if (_fade <= 0f) { _a.volume = Audio.BgmVolume; _b.Stop(); }
        }

        void Update()
        {
            if (_fade <= 0f) return;
            _fade -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(1f - _fade / Audio.CrossFadeSec);
            _a.volume = Audio.BgmVolume * t; _b.volume = Audio.BgmVolume * (1f - t);
            if (_fade <= 0f) { _b.Stop(); _b.clip = null; _a.volume = Audio.BgmVolume; }
        }

        public bool PlaySfx(string key, float volume, float pitchJitter)
        {
            if (MuteSfx) return false;
            var clip = Clip(key);
            if (clip == null || _sfx == null) return false;
            _sfx.pitch = 1f + (pitchJitter > 0f ? UnityEngine.Random.Range(-pitchJitter, pitchJitter) : 0f);
            _sfx.PlayOneShot(clip, Mathf.Clamp01(volume));
            return true;
        }

        public void Wake()
        {
            if (_a != null && _a.clip != null && !_a.isPlaying && !string.IsNullOrEmpty(_key)) _a.Play();
        }
    }
}
