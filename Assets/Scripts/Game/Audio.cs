using System;
using System.Collections.Generic;
using UnityEngine;

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

        bool MuteBgm => App != null && App.Save != null && App.Save.MuteBgm;
        bool MuteSfx => App != null && App.Save != null && App.Save.MuteSfx;

        public void RefreshMute()
        {
            bool m = MuteBgm;
            if (_a != null) _a.mute = m;
            if (_b != null) _b.mute = m;
            if (_sfx != null) _sfx.mute = MuteSfx;
        }

        AudioClip Clip(string key) => App != null && App.Assets != null ? App.Assets.Clip(key) : null;

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
