using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T358(주인 2026-09-10 «게임 입장할 때 로딩 좀 화면 되게 하기») — 로비 START 를 <b>실제로 눌러</b> 전투에 들어갈 때
    /// 부팅과 같은 로딩 조각(<c>Title_Loading</c>)이 <b>떴다가</b>, 전투 화면이 선 뒤 <b>사라지는가</b>.
    /// <para>
    /// ⚠ 로딩은 «지나가는» 오브젝트다 — 배치 모드에서는 첫 Update 에 내려가므로(결정 1003) «떴다» 는 <b>누른 직후(yield 전)</b> 에
    /// 잡고 기록(<see cref="LoadingScreen.LastBattleShown"/>)으로도 본다. 사라진 «뒤» 는 시간이 아니라 «<see cref="App.BattleLoading"/> 이 null» 로 기다린다.
    /// </para>
    /// </summary>
    public class BattleLoadingTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        [UnityTest]
        public IEnumerator StartShowsTheLoadingPieceAboveEverythingAndItGoesAwayOnceTheBattleIsUp()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);
            var start = UiKit.Find(_app.Current.Root, "Start"); Assert.IsNotNull(start, "로비 START");
            var btn = start.GetComponent<Button>(); Assert.IsNotNull(btn, "START 는 눌리는 것이어야 한다");
            Assert.IsNull(_app.BattleLoading, "누르기 전에는 로딩이 없다");
            LoadingScreen.LastBattleShown = false;

            // ⓐ 누른 «직후»(같은 호출 안 · Update 가 아직 안 돌았다) — 조각이 Frame 의 맨 위에 서 있다
            btn.onClick.Invoke();
            Assert.IsTrue(LoadingScreen.LastBattleShown, "START 를 누르면 전투 입장 로딩 조각(" + LoadingScreen.Key + ")이 떠야 한다(T358) — Show 가 null 을 돌려줬다");
            var loading = _app.BattleLoading;
            Assert.IsNotNull(loading, "누른 직후에는 로딩이 떠 있다");
            Assert.IsNotNull(loading.Root, "조각 오브젝트");
            Assert.AreEqual(_app.Frame, loading.Root.transform.parent, "로딩은 Frame 바로 아래(Overlay·토스트와 같은 층)에 선다");
            Assert.AreEqual(_app.Frame.childCount - 1, loading.Root.transform.GetSiblingIndex(), "로딩이 맨 위 — 전투 HUD·Overlay·토스트를 덮는다");
            Assert.IsNotNull(UiKit.Find(_app.Frame, LoadingScreen.Key), "이름으로도 찾힌다(부팅 자와 같은 계약)");
            Assert.AreEqual("battle", _app.Current.Name, "로딩 뒤에서 전투 화면은 이미 서 있다(스폰은 동기)");
            Assert.IsNotNull(_app.GetScreen<BattleScreen>().G, "전투 상태도 이미 있다 — 로딩은 그림이지 게임을 멈추는 것이 아니다");

            // ⓑ 전투가 첫 프레임을 그리고 최소 표시 시간이 지나면 사라진다 — 시계가 아니라 «null 이 됐다» 로 기다린다
            float t0 = Time.realtimeSinceStartup;
            while (_app.BattleLoading != null && Time.realtimeSinceStartup - t0 < 5f) yield return null;
            Assert.IsNull(_app.BattleLoading, "전투가 선 뒤에는 로딩이 내려가야 한다(5초 안)");
            Assert.IsNull(UiKit.Find(_app.Frame, LoadingScreen.Key), "조각도 파괴됐다(전투 화면이 선 뒤에는 없다 · 절 2항)");
            Assert.AreEqual("battle", _app.Current.Name, "그 뒤에도 전투 화면이다");
            _log.AssertNoRed("전투 입장 로딩");

            // ⓒ 전투를 떠나면(로비) 로딩이 남아 있을 수 없다 — 떠 있는 채로 나가도 ShowScreen 이 내린다
            _app.StartBattle(1);
            Assert.IsNotNull(_app.BattleLoading, "다시 들어가면 다시 뜬다");
            _app.ShowScreen("lobby"); yield return Frames(2);
            Assert.IsNull(_app.BattleLoading, "로비로 나가면 로딩이 내려간다");
            Assert.IsNull(UiKit.Find(_app.Frame, LoadingScreen.Key), "조각도 없다");
            _log.AssertNoRed("로딩 중 이탈");

            yield return Shutdown();
        }
    }
}
