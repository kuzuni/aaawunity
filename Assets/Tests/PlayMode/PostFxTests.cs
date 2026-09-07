using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T181 — 주인 2026-09-07 «glow 같은 거 빛나는 느낌 잘 안 드는 거 같던데 포스트 프로세싱 같은 거 설정해야 하는 듯».
    /// 월드에 <b>Bloom</b> 이 실제로 돌게 세워졌는가를 잰다 — 씬 파일이 아니라 <b>코드</b>로 세우므로(결정 458) 이 자가 그 계약의 정본이다.
    /// <para>
    /// 재는 것 셋 — ⓐ 전역 <see cref="Volume"/> 이 하나 서 있고 프로파일에 Bloom 이 있다(값은 <see cref="PostFx"/> 상수 그대로) ·
    /// ⓑ <b>카메라 스위치가 켜져 있다</b>(이것을 빠뜨리면 Volume 이 있어도 아무 일도 안 난다 — 이 작업의 가장 쉬운 실패) ·
    /// ⓒ <see cref="HeroView"/> 의 런타임 카메라는 <b>여전히 꺼져 있다</b>(초상이 뿌예지고 비용만 는다 · 지시서 3항).
    /// </para>
    /// </summary>
    public class PostFxTests
    {
        App _app; PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator BloomIsSetUpOnTheWorldCameraAndNotOnThePortraitCamera()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);

            // ⓐ 전역 Volume + Bloom
            var v = PostFx.Current;
            Assert.IsNotNull(v, "부팅이 전역 Volume 을 세운다(" + PostFx.VolumeName + ")");
            Assert.IsTrue(v.isGlobal, "전역 Volume(카메라가 어디 있든 먹는다)");
            Assert.AreEqual(PostFx.VolumeName, v.gameObject.name, "이름은 게이트가 찾는 그것");
            Assert.IsNotNull(v.sharedProfile, "Volume 프로파일(메모리 인스턴스 · 새 에셋 0)");
            var bloom = PostFx.CurrentBloom;
            Assert.IsNotNull(bloom, "프로파일에 넣은 Bloom");
            Assert.AreEqual(PostFx.BloomThreshold, bloom.threshold.value, 1e-3f, "Bloom threshold = PostFx 상수");
            Assert.AreEqual(PostFx.BloomIntensity, bloom.intensity.value, 1e-3f, "Bloom intensity = PostFx 상수");
            Assert.AreEqual(PostFx.BloomScatter, bloom.scatter.value, 1e-3f, "Bloom scatter = PostFx 상수");
            Assert.IsTrue(bloom.threshold.overrideState && bloom.intensity.overrideState && bloom.scatter.overrideState,
                "값 셋이 override 로 켜져 있어야 실제로 먹는다(override 를 안 켜면 프로파일 기본값이 그대로다)");

            // ⓑ 카메라 스위치 — 이것이 이 작업에서 가장 빠뜨리기 쉬운 한 줄이다
            var cam = Camera.main;
            Assert.IsNotNull(cam, "메인 카메라");
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            Assert.IsNotNull(data, "메인 카메라의 URP 데이터");
            Assert.IsTrue(data.renderPostProcessing, "메인 카메라의 후처리가 켜져 있어야 Bloom 이 실제로 돈다");

            // ⓒ 초상 카메라(HeroView)는 그대로 꺼져 있다
            _app.ShowScreen("lobby"); yield return Frames(3);
            int portraits = 0;
            foreach (var hv in Object.FindObjectsByType<HeroView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (hv == null || hv.Cam == null) continue;
                portraits++;
                var hd = hv.Cam.GetComponent<UniversalAdditionalCameraData>();
                if (hd != null) Assert.IsFalse(hd.renderPostProcessing, "초상 카메라(HeroView)에는 후처리를 켜지 않는다 — " + hv.name);
            }
            Debug.Log("[T181] 초상 카메라 " + portraits + "대 확인 · Bloom = " + PostFx.BloomThreshold + "/" + PostFx.BloomIntensity + "/" + PostFx.BloomScatter);

            _log.AssertNoRed("포스트 프로세싱");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(3);
        }
    }
}
