using System.Collections;
using DG.Tweening;
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
    /// T394 — 탐험(30) 팝업의 플레이어가 «오른쪽으로 계속 걸어가는 것처럼» 보인다(주인 2026-09-10).
    /// ⓐ 팝업을 열면 풀 띠(<see cref="LobbyPopups.ExGroundName"/> · RawImage)의 <c>uvRect.x</c> 가 두 프레임 사이에 <b>늘어난다</b>(= 그림이 오른쪽→왼쪽으로 흐른다) · 소품(나무)도 왼쪽으로 민다
    /// ⓑ 기사는 <see cref="CharacterRig.Walk"/> 상태로 오른쪽을 본다(Animator 의 현재 상태로 잰다 · 적은 그대로 Idle)
    /// ⓒ 팝업을 닫으면 흐름 트윈(<see cref="LobbyPopups.ExWalkTweenId"/>)이 죽는다(<c>SetLink</c>) ⓓ 빨간 줄 0.
    /// </summary>
    public class ExpeditionWalkPlayTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.Overlay != null && _app.Overlay.IsOpen) _app.Overlay.Close(); if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static bool WalkTweenAlive()
        {
            var list = DOTween.TweensById(LobbyPopups.ExWalkTweenId, true);
            return list != null && list.Count > 0;
        }

        [UnityTest]
        public IEnumerator 탐험_팝업의_배경은_왼쪽으로_흐르고_기사는_걷는다()
        {
            yield return Boot();
            if (_app.Data == null || _app.Data.Expedition == null) { yield return Shutdown(); Assert.Ignore("탐험 표가 없다 — 잴 것이 없다"); }
            Assert.Greater(LobbyPopups.ExWalkPxPerSec(_app.Data), 0f, "표(combat.playerSpeed × ui.zoom)에서 걷기 속도가 나와야 한다(전제)");

            LobbyPopups.Expedition(_app); yield return Frames(2);
            var box = UiKit.Find(_app.Overlay.Root, "ExpeditionBox"); Assert.IsNotNull(box, "탐험 팝업 상자");
            var pic = UiKit.Find(box, "Picture"); Assert.IsNotNull(pic, "그림 띠");
            var groundT = UiKit.Find(pic, LobbyPopups.ExGroundName); Assert.IsNotNull(groundT, "풀 띠(" + LobbyPopups.ExGroundName + ")");
            var ground = groundT.GetComponent<RawImage>();
            Assert.IsNotNull(ground, "풀 띠는 uvRect 가 흐르는 RawImage 여야 한다(T394)");
            Assert.IsNotNull(ground.texture, "풀 띠 텍스처(env.roadUp)");
            Assert.AreEqual(TextureWrapMode.Repeat, ground.texture.wrapModeU, "uvRect 가 1 을 넘어 흐르려면 텍스처가 Repeat 여야 한다(.meta wrapU = 0)");
            Assert.IsTrue(WalkTweenAlive(), "흐름 트윈이 돌고 있다");

            // ⓐ 두 프레임 사이에 uvRect.x 가 늘어난다(= 그림이 오른쪽→왼쪽) · 나무도 왼쪽으로(앵커 x 가 준다 · 한 바퀴 되돌림 전이므로 두 프레임이면 단조)
            var tree = UiKit.Find(pic, "Tree1") as RectTransform; Assert.IsNotNull(tree, "나무");
            float x0 = ground.uvRect.x, tx0 = tree.anchorMin.x;
            yield return Frames(2);
            float x1 = ground.uvRect.x, tx1 = tree.anchorMin.x;
            Assert.Greater(x1, x0, "두 프레임 사이에 uvRect.x 가 늘어야 한다(흐름)");
            Assert.Less(tx1, tx0, "나무가 왼쪽으로 밀려야 한다(소품도 같이 간다)");
            Assert.Greater(ground.uvRect.width, 1f, "띠 폭이 타일 하나보다 넓다 — 타일이 이어져 보이려면 uvRect 폭이 1 을 넘는다");

            // ⓑ 기사는 걷기 상태 · 오른쪽 보기 · 적은 Idle 그대로
            var knight = UiKit.Find(pic, "Knight"); Assert.IsNotNull(knight, "기사 칸");
            var hv = knight.GetComponentInChildren<HeroView>(true); Assert.IsNotNull(hv, "기사 HeroView");
            Assert.IsTrue(hv.Walking, "기사는 걷기 모드(SetWalking)");
            Assert.IsNotNull(hv.Rig, "리그");
            Assert.AreEqual(CharacterRig.Walk, hv.Rig.Current, "리그의 현재 상태는 Walk");
            var anim = hv.Rig.GetComponentInChildren<Animator>(true); Assert.IsNotNull(anim, "Animator");
            Assert.IsTrue(anim.GetCurrentAnimatorStateInfo(0).IsName(CharacterRig.Walk), "Animator 의 현재 상태가 «걷기»");
            Assert.Greater(anim.speed, 0f, "걷기가 멈춰 있으면 안 된다");
            Assert.Greater(hv.Rig.transform.localScale.x, 0f, "오른쪽을 본다");
            var foe = UiKit.Find(pic, "Foe"); Assert.IsNotNull(foe, "적 칸");
            var fv = foe.GetComponentInChildren<HeroView>(true); Assert.IsNotNull(fv);
            Assert.IsFalse(fv.Walking, "적은 제자리 Idle");
            Assert.AreEqual(CharacterRig.Idle, fv.Rig.Current, "적의 현재 상태는 Idle");

            // ⓒ 닫으면 멈춘다
            _app.Overlay.Close(); yield return Frames(2);
            Assert.IsFalse(WalkTweenAlive(), "팝업을 닫으면 흐름 트윈이 죽는다(SetLink)");

            _log.AssertNoRed("T394 탐험 걷기");
            yield return Shutdown();
        }
    }
}
