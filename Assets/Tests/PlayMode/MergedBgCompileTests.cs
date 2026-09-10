using System.Collections;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T225 — «유니티가 이 셰이더를 실제로 세우는가». 이 레포의 <b>첫 자작 셰이더</b>라 물어볼 곳이 여기밖에 없다.
    /// <para>
    /// ⚑ 워커는 셰이더를 로컬에서 컴파일해 볼 수 없고, <b>CI 도 셰이더가 깨졌다고 빨개지지 않는다</b> —
    /// 안 서는 셰이더는 «오류» 가 아니라 <b>자홍색 화면</b>으로 나타나고 테스트는 그대로 초록이다.
    /// <see cref="UiKit.MergedBg"/> 는 그 경우 <c>null</c> 을 돌려 옛 세 겹 길로 떨어지므로 화면은 안 깨지지만,
    /// 그 폴백은 <b>조용하다</b> — 조용한 폴백은 «최적화가 아무 일도 안 하고 있다» 를 숨긴다. 그래서 여기서 소리내어 묻는다.
    /// </para>
    /// </summary>
    public class MergedBgCompileTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(카탈로그 로드)");
            _app = App.I;
            yield return null;
        }

        /// <summary>카탈로그가 머티리얼을 들고, 그 머티리얼의 셰이더가 <b>이 기계에서 선다</b>(=컴파일됐다).</summary>
        [UnityTest]
        public IEnumerator TheShaderActuallyCompiles()
        {
            yield return Boot();

            var mat = _app.Assets.Material(UiKit.MergedBgMatKey);
            Assert.IsNotNull(mat, "카탈로그 «" + UiKit.MergedBgMatKey + "» — 없으면 셰이더가 빌드에서 통째로 빠진다(폰에서 배경이 사라지는 그 길)");
            Assert.IsNotNull(mat.shader, "머티리얼이 셰이더를 못 찾았다(.mat 의 m_Shader guid 를 보라)");
            Assert.AreEqual("KkomaKnight/UiMergedBg", mat.shader.name, "머티리얼이 엉뚱한 셰이더를 물었다");
            // 이 줄이 이 자의 전부다 — 문법 오류·지원 안 되는 명령이 있으면 여기서 false 다.
            Assert.IsTrue(mat.shader.isSupported, "셰이더가 이 기계에서 안 선다 — 컴파일 오류이거나 이 플랫폼이 못 받는 명령이 들어 있다(화면은 자홍색이 된다)");
            _log.AssertNoRed("셰이더 컴파일 확인");
        }

        /// <summary>
        /// 그리고 <see cref="UiKit.MergedBg"/> 가 <b>실제로 겹을 세운다</b> — 재료·머티리얼·이름이 하나라도 어긋나면 <c>null</c> 이다.
        /// 세운 겹이 두 감사(<see cref="UiKit.HasPattern"/>·<see cref="UiKit.HasGradient"/>)에 «있다» 로 잡히는 것까지 같이 본다:
        /// 합치면서 그 둘이 false 가 되면 T72 감사가 통째로 빨개진다(이 자가 그 자리를 미리 막는다).
        /// </summary>
        [UnityTest]
        public IEnumerator ItBuildsOneLayerThatBothAuditsStillSee()
        {
            yield return Boot();

            var host = UiKit.Rect(_app.UiCanvas.transform, "MergedBgProbe");
            UiKit.Stretch(host);
            var raw = UiKit.MergedBg(host, UiKit.PatternTintLobby);
            Assert.IsNotNull(raw, "MergedBg 가 겹을 못 세웠다 — 재료(ui.pattern·ui.gradTop1·ui.gradBottom)·머티리얼·셰이더 중 하나가 없다");
            yield return null;

            Assert.AreEqual(1, host.childCount, "합친 겹은 **한 장**이다(그 한 장이 T225 의 전부다)");
            Assert.IsTrue(UiKit.HasPattern(host), "합친 겹도 «무늬가 있다» 로 잡혀야 한다(T72 ① 감사)");
            Assert.IsTrue(UiKit.HasGradient(host), "합친 겹도 «그라데이션이 있다» 로 잡혀야 한다(T72 ③ 감사)");
            Assert.IsFalse(raw.raycastTarget, "배경 겹은 클릭을 안 먹는다");

            // 무늬 색·알파는 옛 세 겹과 **같은 상수**에서 온다 — 합치면서 값이 두 벌이 되지 않았는지 여기서 잰다.
            var col = raw.material.GetColor("_PatternColor");
            Assert.AreEqual(UiKit.PatternAlphaLobby, col.a, 0.001f, "로비 무늬 알파 = UiKit.PatternAlphaLobby 한 곳에서 온다(T166 ⓐ)");
            Assert.Greater(col.r, 0.9f, "로비 무늬는 흰색(T166 ⓐ)");

            UnityEngine.Object.Destroy(host.gameObject);
            yield return null;
            _log.AssertNoRed("합친 겹 세우기");
        }
    }
}
