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
    /// T96-loading — 부팅에 로딩 화면(주인 지목 프리팹 <c>Title_Loading</c>)이 뜨고, 진행 바가 실제 로드 진행을 따라가며,
    /// 다 읽으면 사라지고 로비가 남는가(주인 2026-09-07). 부팅 경로라 **모든 화면이 이 코드를 지난다** — 빨간 줄 0 이 특히 중요하다.
    /// </summary>
    public class LoadingScreenTests
    {
        PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator BootShowsThePrefabLoadingScreenAndItGoesAwayWhenTheGameIsUp()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

            // ⓐ 부팅 캔버스와 로딩 조각 — App 이 서기 «전»에 잡는다(한 프레임 안에 사라질 수 있어 매 프레임 본다)
            GameObject piece = null; Slider bar = null;
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f)
            {
                if (piece == null)
                {
                    var boot = GameObject.Find("BootCanvas");
                    if (boot != null)
                    {
                        var p = UiKit.Find(boot.transform, LoadingScreen.Key);
                        if (p != null) { piece = p.gameObject; bar = piece.GetComponentInChildren<Slider>(true); }
                    }
                }
                yield return null;
            }
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");

            // 로딩 화면은 «지나가는» 오브젝트다(데이터를 다 읽으면 0.3s 뒤 사라진다) — 위 루프가 그 순간을 놓칠 수 있으므로
            // «떴다» 는 사실은 부팅이 남긴 기록(LoadingScreen.LastShownWasPrefab · 결정 329)으로 판정한다.
            // 이렇게 하면 실패했을 때 «조각이 안 떴다» 와 «떴는데 테스트가 못 잡았다» 가 메시지로 갈린다(CI #234 에서 이것을 못 갈라 한 회차를 버렸다).
            Assert.IsTrue(LoadingScreen.LastShownWasPrefab,
                "부팅에 로딩 화면 조각(" + LoadingScreen.Key + ")이 떠야 한다(T96-loading) — 조각을 못 찾아 Show 가 null 을 돌려줬다"
                + (piece != null ? " ※ 그런데 화면에서는 조각이 잡혔다(기록 쪽이 틀렸다는 뜻)" : ""));
            Assert.AreEqual(0f, LoadingScreen.LastBarMin, 1e-3f, "진행 바 0~1(부팅 기록)");
            Assert.AreEqual(1f, LoadingScreen.LastBarMax, 1e-3f, "진행 바 0~1(부팅 기록)");
            // 살아 있는 조각을 잡았으면 그것으로도 확인한다(잡는 것은 타이밍이라 «못 잡음» 은 실패가 아니다)
            if (piece != null) Assert.IsNotNull(bar, "로딩 진행 바(프리팹 " + LoadingScreen.BarName + ")");

            // ⓑ 다 읽으면 사라진다 — 최소 표시 시간(0.3s)을 넉넉히 넘겨 기다린다
            float t1 = Time.realtimeSinceStartup;
            while (GameObject.Find("BootCanvas") != null && Time.realtimeSinceStartup - t1 < 5f) yield return null;
            Assert.IsNull(GameObject.Find("BootCanvas"), "로드가 끝나면 부팅 캔버스가 사라진다");
            Assert.IsTrue(piece == null, "로딩 조각도 같이 사라진다");

            // ⓒ 그 뒤에는 로비가 남는다 · 빨간 줄 0
            yield return Frames(2);
            Assert.AreEqual("lobby", App.I.Current.Name, "로딩이 끝나면 로비");
            _log.AssertNoRed("부팅 로딩 화면");

            if (App.I != null) { if (App.I.UiCanvas != null) Object.Destroy(App.I.UiCanvas.gameObject); Object.Destroy(App.I.gameObject); }
            yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        [UnityTest]
        public IEnumerator ProgressMovesTheBarAndTheLabel()
        {
            // 화면 조각만 따로 세워 진행률 계약을 잰다(부팅을 다시 돌리지 않는다)
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "App");
            var host = UiKit.CreateRootCanvas("LoadingTestCanvas");
            var ls = LoadingScreen.Show(host.transform, App.I.Assets);
            Assert.IsNotNull(ls, "카탈로그에 " + LoadingScreen.Key + " 가 있어야 한다");
            var bar = ls.Root.GetComponentInChildren<Slider>(true);
            Assert.IsNotNull(bar, "진행 바");
            ls.SetProgress(0.5f); yield return null;
            Assert.AreEqual(0.5f, bar.value, 1e-3f, "진행률 0.5");
            ls.SetProgress(2f); yield return null;
            Assert.AreEqual(1f, bar.value, 1e-3f, "1 을 넘지 않는다");
            ls.SetProgress(-1f); yield return null;
            Assert.AreEqual(0f, bar.value, 1e-3f, "0 아래로 안 간다");
            ls.Hide(); yield return null;
            Assert.IsNull(ls.Root, "Hide 뒤에는 조각이 없다");
            Object.Destroy(host.gameObject);
            yield return Frames(2);
            _log.AssertNoRed("진행률 계약");

            if (App.I != null) { if (App.I.UiCanvas != null) Object.Destroy(App.I.UiCanvas.gameObject); Object.Destroy(App.I.gameObject); }
            yield return Frames(3);
        }
    }
}
