using System.Collections;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T28 배경음·효과음 — 실제 씬(SampleScene · Bootstrap → App)에서
    /// ⓐ BGM 키가 화면마다 바뀌는가(로비 → 전투 → 로비 · 보스 등장 → boss 곡 · 새 판 → 전투 곡) ⓑ 카탈로그 오디오 키 20개가 전부 클립을 갖는가(경고 0)
    /// ⓒ SFX 호출이 예외 없이 도는가(있는 키 · 없는 키 · 음소거 상태) ⓓ 설정 팝업의 BGM/SFX 스위치가 세이브(MuteBgm/MuteSfx)와 소스 mute 를 바꾸는가.
    /// UiSmokeTests 와 파일을 나눈 이유: T29~T33 이 UiSmokeTests 를 같은 시각에 고치므로 충돌을 피한다(같은 PlayMode 어셈블리 · 같은 PlayLog 게이트).
    /// 배치 모드(CI)엔 오디오 장치가 없어 «소리가 났다» 는 못 재고, AudioSource.clip/isPlaying/mute 와 Audio.CurrentBgm/SfxPlayed 로 잰다.
    /// </summary>
    public class AudioTests
    {
        static readonly string[] Keys =
        {
            "bgm.lobby", "bgm.battle", "bgm.boss",
            "snd.click", "snd.popup", "snd.hit", "snd.crit", "snd.miss", "snd.kill", "snd.hurt", "snd.levelup", "snd.perk",
            "snd.coin", "snd.gacha", "snd.fuse", "snd.equip", "snd.clear", "snd.fail", "snd.arrow", "snd.axe",
        };

        App _app; PlayLog _log; int _catalogWarn;
        [SetUp] public void SetUp() { _log = new PlayLog(); _catalogWarn = 0; Application.logMessageReceived += OnLog; Audio.ResetStats(); }
        [TearDown] public void TearDown() { Application.logMessageReceived -= OnLog; _log?.Dispose(); _log = null; Time.timeScale = 1f; }
        void OnLog(string msg, string stack, LogType type) { if (type == LogType.Warning && msg != null && msg.StartsWith("[AssetCatalog] clip 없음")) _catalogWarn++; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return null; yield return null;
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return null; yield return null; yield return null;
            _log.AssertNoRed("종료");
        }
        IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }
        static AudioManager Manager() => Object.FindFirstObjectByType<AudioManager>();

        [UnityTest]
        public IEnumerator CatalogHasEveryAudioKeyAndBgmFollowsScreens()
        {
            yield return Boot();
            Assert.IsTrue(Audio.Ready, "App.Create 가 Audio 를 세운다");
            var m = Manager(); Assert.IsNotNull(m, "AudioManager 가 App 밑에 있어야 한다");
            // ⓑ 키 20개 전부 클립(경고 0)
            foreach (var k in Keys) Assert.IsNotNull(_app.Assets.Clip(k), "카탈로그 오디오 클립: " + k);
            Assert.AreEqual(0, _catalogWarn, "clip 없음 경고 0");
            // ⓐ 로비 곡 → 전투 곡 → (보스) → 로비 곡
            Assert.AreEqual("bgm.lobby", Audio.CurrentBgm, "로비 = bgm.lobby");
            Assert.IsNotNull(m.BgmSource.clip, "로비 BGM 소스에 클립"); Assert.IsTrue(m.BgmSource.loop, "BGM 루프"); Assert.IsTrue(m.BgmSource.isPlaying, "로비 BGM 재생 중");
            _app.ShowScreen("gear"); yield return null; Assert.AreEqual("bgm.lobby", Audio.CurrentBgm, "장비 화면도 로비 곡(같은 곡이면 무시)");
            _app.StartBattle(1); yield return null;
            Assert.AreEqual("bgm.battle", Audio.CurrentBgm, "전투 = bgm.battle");
            yield return RealSeconds(0.7f);   // 크로스페이드(0.5초) 끝
            Assert.AreEqual(Audio.BgmVolume, m.BgmSource.volume, 0.01f, "페이드 뒤 볼륨 = BgmVolume");
            Assert.AreEqual("bgm.battle", m.BgmSource.clip != null ? Audio.CurrentBgm : null);
            Audio.Bgm("bgm.boss"); yield return null; Assert.AreEqual("bgm.boss", Audio.CurrentBgm, "보스 곡");
            var bs = _app.GetScreen<BattleScreen>(); bs.Start(1); yield return null;
            Assert.AreEqual("bgm.battle", Audio.CurrentBgm, "새 판은 전투 곡으로 되돌린다");
            _app.ShowScreen("lobby"); yield return null; Assert.AreEqual("bgm.lobby", Audio.CurrentBgm, "로비로 돌아오면 로비 곡");
            _log.AssertNoRed("BGM 화면 전환");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator SfxPlaysWithoutErrorsEvenForMissingKeysAndMute()
        {
            yield return Boot();
            Audio.ResetStats();
            // ⓒ 있는 키 → 실제 재생 · 없는 키 → 경고 1 · 예외 0 · 빈 키 → 아무 일 없음
            Audio.Sfx("snd.click"); Audio.Sfx("snd.hit", 0.8f); Audio.Sfx("snd.crit", 1f, 0f);
            Assert.AreEqual(3, Audio.SfxPlayed, "클립이 있는 효과음 3개 재생");
            int before = _catalogWarn; Audio.Sfx("snd.없는키"); Assert.AreEqual(before + 1, _catalogWarn, "없는 키 = 경고 1(에러 아님)"); Assert.AreEqual(3, Audio.SfxPlayed, "없는 키는 재생 안 함");
            Audio.Sfx(null); Audio.Sfx(""); Assert.AreEqual(3, Audio.SfxPlayed);
            // 버튼 클릭 → snd.click 이 한 곳(UiKit.Clickable)에서
            var go = new GameObject("btn", typeof(RectTransform)); go.transform.SetParent(_app.Frame, false);
            bool clicked = false; var b = UiKit.Clickable(go.transform, () => clicked = true);
            Audio.ResetStats(); b.onClick.Invoke(); yield return null;
            Assert.IsTrue(clicked); Assert.AreEqual("snd.click", Audio.LastSfx, "클릭음"); Assert.AreEqual(1, Audio.SfxPlayed);
            // 음소거(SFX) → 요청은 기록되나 재생 0 · 해제 → 다시 재생
            _app.Save.MuteSfx = true; Audio.ApplyMute(); Audio.ResetStats(); Audio.Sfx("snd.coin"); Assert.AreEqual("snd.coin", Audio.LastSfx); Assert.AreEqual(0, Audio.SfxPlayed, "효과음 음소거");
            _app.Save.MuteSfx = false; Audio.ApplyMute(); Audio.Sfx("snd.coin"); Assert.AreEqual(1, Audio.SfxPlayed);
            // ⓓ 설정 팝업 스위치 — BGM/SFX 각각 세이브 + 소스 mute
            var m = Manager();
            _app.Overlay.Settings(); yield return null; yield return null;
            var bgm = UiKit.Find(_app.Overlay.Root, "BGM"); var sfx = UiKit.Find(_app.Overlay.Root, "SFX");
            Assert.IsNotNull(bgm, "Settings 프리팹 BGM 줄"); Assert.IsNotNull(sfx, "Settings 프리팹 SFX 줄");
            var bsw = UiKit.Find(bgm, "Swich_01").GetComponent<UnityEngine.UI.Button>(); var ssw = UiKit.Find(sfx, "Swich_01").GetComponent<UnityEngine.UI.Button>();
            Assert.IsFalse(_app.Save.MuteBgm); Assert.IsFalse(m.BgmSource.mute);
            bsw.onClick.Invoke(); yield return null; Assert.IsTrue(_app.Save.MuteBgm, "BGM 스위치 → MuteBgm"); Assert.IsTrue(m.BgmSource.mute, "BGM 소스 mute"); Assert.IsTrue(_app.Save.Muted, "옛 Muted 별칭 = MuteBgm");
            bsw.onClick.Invoke(); yield return null; Assert.IsFalse(_app.Save.MuteBgm); Assert.IsFalse(m.BgmSource.mute);
            ssw.onClick.Invoke(); yield return null; Assert.IsTrue(_app.Save.MuteSfx, "SFX 스위치 → MuteSfx"); Assert.IsFalse(_app.Save.MuteBgm, "SFX 스위치는 BGM 에 영향 없음");
            ssw.onClick.Invoke(); yield return null; Assert.IsFalse(_app.Save.MuteSfx);
            // 세이브 왕복
            var back = SaveStore.Load(_app.Data); Assert.IsFalse(back.MuteBgm); Assert.IsFalse(back.MuteSfx);
            _app.Overlay.Close(); yield return null;
            Audio.Wake();   // 예외 0 만 본다(WebGL 전용 동작)
            _log.AssertNoRed("SFX·스위치");
            yield return Shutdown();
        }
    }
}
