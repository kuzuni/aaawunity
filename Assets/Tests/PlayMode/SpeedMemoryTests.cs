using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T18 — 배속(x2) 기억. 주인: «2배속으로 하다가 클리어 뒤 다른 챕터 도전하면 다시 1배속» 이 안 되게.
    /// 실제 씬(SampleScene → App)에서 전투에 들어가 배속 버튼을 눌러 x2 로 만든 뒤 ⓐ 세이브(<see cref="SaveData.Speed"/> · PlayerPrefs)에 즉시 기록되는가
    /// ⓑ 로비로 나갔다 다시 전투에 들어가도 x2 로 시작하는가 ⓒ App 을 새로 세워(앱 재시작과 같음) PlayerPrefs 에서 읽어도 x2 인가 ⓓ 다시 누르면 x1 로 돌아가 저장되는가.
    /// 빨간 줄 0 은 <see cref="PlayLog"/>(ROUTINE §1 · LogAssert.NoUnexpectedReceived 금지).
    /// </summary>
    public class SpeedMemoryTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; Assert.IsNotNull(_app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");
            yield return Frames(2);
            _log.AssertNoRed("부팅");
        }

        [UnityTest]
        public IEnumerator SpeedX2SurvivesLobbyRoundTripAndAppRestart()
        {
            yield return Boot();
            _app.StartBattle(1); yield return Frames(2);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); Assert.IsNotNull(bs.G, "전투 상태");
            Assert.AreEqual(1, bs.Speed, "새 세이브의 첫 전투는 x1");
            Assert.AreEqual(1, _app.Save.Speed);
            _log.AssertNoRed("전투 진입");

            // 배속 버튼 → x2 · 세이브에 즉시
            bs.ToggleSpeed(); yield return Frames(2);
            Assert.AreEqual(2, bs.Speed, "버튼 한 번 = x2");
            Assert.AreEqual(2, _app.Save.Speed, "세이브 객체에 기록");
            var stored = SaveData.FromJson(PlayerPrefs.GetString(SaveStore.Key, null), _app.Data);
            Assert.AreEqual(2, stored.Speed, "PlayerPrefs 에 즉시 저장(Persist)");
            _log.AssertNoRed("x2 전환");

            // 로비로 나갔다가 다시 전투(«다른 챕터 도전» 과 같은 길 — Start 가 다시 불린다) → x2 유지
            _app.Overlay.Close(); _app.ShowScreen("lobby"); yield return Frames(2);
            Assert.AreEqual("lobby", _app.Current.Name);
            _app.StartBattle(1); yield return Frames(2);
            bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs.G, "재진입 전투 상태");
            Assert.AreEqual(2, bs.Speed, "재진입해도 x2 로 시작(주인 지적: 다시 1배속이 되면 안 된다)");
            _log.AssertNoRed("재진입");

            // 앱 재시작과 같음 — App 을 지우고 PlayerPrefs 에서 다시 세운다
            var data = _app.Data; var catalog = _app.Assets; var cam = _app.WorldCamera;
            _app.ShowScreen("lobby"); yield return Frames(1);
            if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); yield return Frames(3);
            _log.AssertNoRed("App 종료");
            _app = App.Create(data, catalog, null, cam); yield return Frames(2);
            Assert.AreEqual(2, _app.Save.Speed, "PlayerPrefs 에서 읽은 세이브의 배속 = x2");
            _app.StartBattle(1); yield return Frames(2);
            bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs.G);
            Assert.AreEqual(2, bs.Speed, "앱 재시작 뒤 첫 전투도 x2");
            _log.AssertNoRed("재시작 전투");

            // 다시 누르면 x1 · 그것도 저장
            bs.ToggleSpeed(); yield return Frames(2);
            Assert.AreEqual(1, bs.Speed); Assert.AreEqual(1, _app.Save.Speed);
            Assert.AreEqual(1, SaveData.FromJson(PlayerPrefs.GetString(SaveStore.Key, null), _app.Data).Speed, "x1 도 저장");
            _app.Overlay.Close(); _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("x1 복귀 · 로비");
        }
    }
}
