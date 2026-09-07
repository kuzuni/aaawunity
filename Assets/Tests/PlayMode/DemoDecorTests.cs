using System;
using System.Collections;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T164 회차 3 — 조각이 달고 오는 «데모 장식» 이 <b>세우는 자리 한 곳</b>에서 꺼지는가.
    /// <para>
    /// 회차 2 는 아이템 칸을 세우는 자리(<c>GearUi.DarkFrame</c>)에서만 껐고, 그래서 펫 화면의
    /// «전체 강화» 버튼처럼 <b>칸이 아닌</b> 조각은 하이라이트가 켜진 채 남았다(CI #332 «[13_pet] … 4개»).
    /// 화면별 스모크(<c>UiSmokeTests.AssertNoHighlights</c>)는 «그 화면을 훑을 때만» 잡으므로
    /// 새 화면·새 조각이 늘면 또 새는 자다. 여기서는 <b>원인 자리</b>를 직접 잰다 —
    /// «하이라이트를 달고 오는 조각을 <see cref="UiKit.Spawn"/> 으로 세우면 켜진 것이 0» 이면
    /// 어느 화면이 그것을 쓰든 조용하다.
    /// </para>
    /// </summary>
    public class DemoDecorTests
    {
        App _app; PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        /// <summary>«HighLight» 로 시작하는 자손을 센다 — <paramref name="activeOnly"/> 면 켜진 것만.</summary>
        static int Highlights(Transform root, bool activeOnly)
        {
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || !t.name.StartsWith(GearUi.HighlightPrefix, StringComparison.Ordinal)) continue;
                if (activeOnly && !t.gameObject.activeInHierarchy) continue;
                n++;
            }
            return n;
        }

        /// <summary>
        /// 하이라이트를 달고 오는 조각 셋을 «칸 손질(<c>DarkFrame</c>) 없이» 그냥 세워도 켜진 것이 0이어야 한다.
        /// <c>ui.btnGray</c> 가 그 자리다 — 펫 «전체 강화» 버튼이 쓰는 조각이고(그래서 13_pet 이 빨갰다) 칸이 아니다.
        /// </summary>
        [UnityTest]
        public IEnumerator SpawnedPiecesCarryNoLiveDemoHighlights()
        {
            yield return Boot();
            var host = new GameObject("T164Host", typeof(RectTransform));
            host.transform.SetParent(_app.UiCanvas.transform, false);

            string[] keys = { "ui.btnGray", "ui.btnBlue", "ui.itemFrame.empty" };
            int carried = 0;
            foreach (var key in keys)
            {
                var go = UiKit.Spawn(key, host.transform);
                yield return Frames(1);
                int all = Highlights(go.transform, false);
                carried += all;
                Assert.AreEqual(0, Highlights(go.transform, true),
                    key + ": 세우자마자 켜진 «HighLight» 가 있으면 안 된다(T164 · 칸이 아닌 조각도 이것을 달고 온다)");
            }
            Assert.Greater(carried, 0,
                "셋 중 하나는 «HighLight» 를 달고 와야 이 자가 무언가를 재는 것이다 — 0이면 조각이 바뀐 것이니 키를 다시 고른다");
            Assert.AreEqual(0, Highlights(_app.UiCanvas.transform, true), "화면 전체에도 켜진 하이라이트 0(T164)");
            _log.AssertNoRed("데모 장식");

            UnityEngine.Object.Destroy(host); yield return Frames(1);
            yield return Shutdown();
        }
    }
}
