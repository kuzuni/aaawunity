using System.Collections;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T12 «플레이 콘솔 에러 0» — HeroView(런타임 카메라 + RenderTexture · URP 2D) 가 실제로 몇 프레임 그려도 콘솔에 빨간 줄이 없는가.
    /// 주인 로그 «Renderer2D Pass: Fake or uninitialized surface is not supported for attachment 0» / «EndRenderPass: Not inside a Renderpass» 의 회귀 방지.
    /// 배치 모드(CI)에서는 GameView 가 안 그려지므로 <see cref="Camera.Render"/> 로 URP 렌더 루프(주인 스택의 DoRenderLoop_Internal)를 직접 밟는다.
    /// (<c>WaitForEndOfFrame</c> 은 배치 모드에서 영영 안 돌아오므로 쓰지 않는다.)
    /// 빨간 줄 검사는 <see cref="PlayLog"/> — <c>LogAssert.NoUnexpectedReceived()</c> 는 Bootstrap 의 일반 Debug.Log 까지 실패로 봐서 CI 런 #33 에서 씬 왕복 테스트가 깨졌다(T11 이 교체).
    /// </summary>
    public class HeroViewTests
    {
        PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>살아 있는 HeroView 카메라를 전부 강제로 한 번씩 그린다(GameView 없는 배치 모드에서도 URP 2D 패스가 실제로 돈다).</summary>
        static void RenderAllHeroViews()
        {
            foreach (var hv in Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
        }

        static IEnumerator RenderFrames(int n) { for (int i = 0; i < n; i++) { RenderAllHeroViews(); yield return null; } }

        /// <summary>HeroView 만 단독으로(App 없이) 세워 3프레임 렌더 → 에러 0 · 텍스처에 깊이/스텐실이 있다 · 끄고 켜기 · 파괴 뒤에도 에러 0.</summary>
        [UnityTest]
        public IEnumerator StandaloneHeroViewRendersWithoutErrors()
        {
            var canvasGo = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var host = new GameObject("Host", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(canvasGo.transform, false); host.sizeDelta = new Vector2(200, 200);

            var hv = HeroView.Attach(host);
            Assert.IsNotNull(hv.Texture, "RenderTexture 가 만들어져야 한다");
            Assert.IsTrue(hv.Texture.IsCreated(), "RenderTexture.Create() 가 호출돼야 한다");
            Assert.Greater(hv.Texture.depth, 0, "URP 2D(Renderer2D · 깊이/스텐실 사용) 카메라 타깃은 깊이 버퍼가 있어야 한다(주인 콘솔 에러 ①②)");
            Assert.AreNotEqual(UnityEngine.Experimental.Rendering.GraphicsFormat.None, hv.Texture.depthStencilFormat, "depthStencilFormat 이 None 이면 안 된다");
            Assert.IsNotNull(hv.Cam); Assert.IsTrue(hv.Cam.enabled); Assert.AreEqual(hv.Texture, hv.Cam.targetTexture);
            var data = hv.Cam.GetComponent<UniversalAdditionalCameraData>();
            Assert.IsNotNull(data, "런타임 카메라에 URP 카메라 데이터가 있어야 한다");
            Assert.AreEqual(CameraRenderType.Base, data.renderType);

            yield return RenderFrames(3);
            _log.AssertNoRed("HeroView");

            // 화면이 꺼지면 무대도 꺼진다 · 다시 켜면 살아난다
            hv.gameObject.SetActive(false); yield return Frames(2);
            Assert.IsFalse(hv.Cam.isActiveAndEnabled, "뷰가 꺼지면 카메라도 꺼져야 한다");
            hv.gameObject.SetActive(true); yield return RenderFrames(2);
            _log.AssertNoRed("HeroView");

            Object.Destroy(canvasGo);
            yield return Frames(3);
            _log.AssertNoRed("HeroView");
        }

        /// <summary>
        /// 실제 씬(SampleScene · Bootstrap → App) 을 올려 로비(HeroView 1) → 장비(HeroView 2) → 전투 진입 → 로비 복귀를 한 바퀴 돌며 콘솔 에러 0.
        /// 주인: «플레이 → 로비·장비·전투 왕복 → 콘솔 빨간 줄 0».
        /// </summary>
        [UnityTest]
        public IEnumerator SceneLobbyGearBattleRoundTripNoErrors()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            var app = App.I;
            Assert.IsNotNull(app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");

            yield return RenderFrames(3);                       // 로비(HeroView 1)
            Assert.AreEqual("lobby", app.Current.Name);
            Assert.GreaterOrEqual(Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length, 1, "로비에 플레이어 초상(HeroView)이 있어야 한다");
            _log.AssertNoRed("HeroView");

            app.ShowScreen("gear"); yield return RenderFrames(3);   // 장비(HeroView 2)
            Assert.AreEqual("gear", app.Current.Name);
            _log.AssertNoRed("HeroView");

            app.StartBattle(1);                                 // 전투 진입 — 월드 카메라는 HeroView 레이어를 안 본다
            float t1 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t1 < 1.0f) { if (app.WorldCamera != null) app.WorldCamera.Render(); yield return null; }
            Assert.AreEqual("battle", app.Current.Name);
            if (app.WorldCamera != null) Assert.AreEqual(0, app.WorldCamera.cullingMask & (1 << HeroView.Layer), "전투 카메라는 HeroView 레이어를 보면 안 된다");
            _log.AssertNoRed("HeroView");

            app.ShowScreen("lobby"); yield return RenderFrames(3);  // 로비 복귀(HeroView 1 재활성)
            app.ShowScreen("gear"); yield return RenderFrames(3);   // 장비 재진입
            app.ShowScreen("lobby"); yield return RenderFrames(3);
            _log.AssertNoRed("HeroView");

            Object.Destroy(app.gameObject);
            yield return Frames(3);
            _log.AssertNoRed("HeroView");
        }
    }
}
