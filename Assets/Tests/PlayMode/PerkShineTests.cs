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
    /// T153 — 특전 카드 shine 이 «일정한 두께로 쭉» 지나가는가(주인 2026-09-07 07:0X
    /// «샤인이 일정한 두께로 쭉 지나가는 효과인데 특전꺼 보니까 얇게 하다가 존나 두껍게 하다가 얇게 하다가 끝남»).
    /// <para>
    /// 굵기 자체는 PlayMode 로 못 재므로(셰이더가 그리는 픽셀이다) 지시서 4항대로 <b>그 굵기를 만들어 내는 두 원인</b>을 잰다:
    /// ⓐ 한 카드에서 이 머티리얼을 쓰는 <b>Image 가 하나</b>(= 몸통 «Bg»)다(다섯이면 층마다 다른 굵기·속도의 띠가 겹쳐 «얇→두꺼→얇» 이 된다) ·
    /// ⓑ 빛의 속도가 <b>등속</b>이다(<c>InOutSine</c> 이면 가장자리에서 느리고 가운데서 빠르다).
    /// ⓑ 는 이징 이름을 믿지 않고 <b>실제 값을 세 곳에서 재서</b> 간격이 같은지 본다.
    /// </para>
    /// T61 의 계약(카드마다 인스턴스 하나 · 순서대로 시작 · 카드가 죽으면 인스턴스도 죽는다)은 <see cref="UiSmokeTests"/> 가 계속 지킨다.
    /// </summary>
    public class PerkShineTests
    {
        App _app; PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

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
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        /// <summary>이 머티리얼 인스턴스를 쓰는 Image 수 — 카드 나무 안에서 센다.</summary>
        static int UsersOf(Transform root, Material inst)
        {
            int n = 0;
            foreach (var img in root.GetComponentsInChildren<Image>(true)) if (img != null && img.material == inst) n++;
            return n;
        }

        /// <summary>ⓐ 3택 팝업의 카드마다 «빛을 물린 Image 가 하나» 인가(T153 1항).</summary>
        [UnityTest]
        public IEnumerator ShineIsBoundToASingleImagePerCard()
        {
            yield return Boot();
            // 전투 상태를 세우고 3택을 띄운다(UiSmokeTests 와 같은 방식 · 엔진은 멈춰 둔다)
            Time.timeScale = 0f;
            var D = _app.Data;
            var rng = new Mulberry32(7u);
            var G = new BattleState(D, 1, _app.Save.CurBuild(D), rng, new InteractivePolicy(), new RunOptions { EmitEvents = true });
            var offer = Perks.Offer(D, G.Taken, false, rng);
            Assert.Greater(offer.Count, 0, "특전 제안");
            G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
            _app.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick));
            yield return Frames(2);
            UiKit.CompleteAllTweens(); yield return Frames(1);

            var cards = UiKit.Find(_app.Overlay.Root, "Group_Card");   // 조각의 담개 이름(«Content» 가 아니다 — CI #359 에서 내 자가 여기서 죽었다)
            Assert.IsNotNull(cards, "3택 카드 담개(Group_Card)");
            int seen = 0;
            foreach (Transform card in cards)
            {
                var mo = card.GetComponent<UiKit.MaterialOwner>();
                if (mo == null || mo.Mat == null) continue;   // 머티리얼이 카탈로그에 없는 환경이면 빛 자체가 없다(그때는 이 자가 셀 것이 없다)
                seen++;
                Assert.AreEqual(1, UsersOf(card, mo.Mat),
                    card.name + ": 빛을 물린 Image 는 한 장이어야 한다(여러 장이면 층마다 굵기·속도가 달라 «얇→두꺼→얇» 이 된다 · T153 1항)");
                var target = UiKit.ShineTarget(card);
                Assert.IsNotNull(target, card.name + " 빛을 물릴 한 장");
                Assert.AreSame(mo.Mat, target.material, card.name + ": 빛은 카드 «몸통(Bg)» 한 장에 물린다(없는 조각이면 가장 넓은 한 장 · T153 회차 2)");
            }
            Assert.Greater(seen, 0, "3택 카드에 shine 머티리얼이 하나는 붙어 있어야 한다(T61)");
            _log.AssertNoRed("특전 3택 shine");

            _app.Overlay.Close(); yield return Frames(1);
            yield return Shutdown();
        }

        /// <summary>ⓑ 빛이 «등속» 인가 — 이징 이름이 아니라 <c>_ShineLocation</c> 값을 1/4·1/2·3/4 에서 재서 간격이 같은지 본다(T153 2항).</summary>
        [UnityTest]
        public IEnumerator ShineMovesAtAConstantSpeed()
        {
            yield return Boot();
            Assert.AreEqual("Linear", UiKit.ShineEaseName, "빛의 속도 곡선은 등속(Linear)이어야 한다(주인 «쭉 지나가는»)");

            // ⚠ 이 어셈블리는 DOTween 을 참조하지 않는다(asmdef `overrideReferences: true` · precompiled 는 nunit 하나) —
            // 그래서 «이징을 실제로 재는» 부분은 UiKit.ShineEasedAt(t) 로 옮겨 두고 여기서는 **float 만** 받는다(결정 465).
            // 이름을 믿지 않는 성질은 그대로다: 세 곳을 재서 «간격이 같은가» 를 본다(InOutSine 이면 가운데가 더 크다).
            float At(float f) => UiKit.ShineEasedAt(f);
            float q1 = At(0.25f), q2 = At(0.5f), q3 = At(0.75f);
            float d1 = q2 - q1, d2 = q3 - q2;
            Debug.Log($"[T153] _ShineLocation 1/4 {q1:0.000} · 1/2 {q2:0.000} · 3/4 {q3:0.000} · 간격 {d1:0.000}/{d2:0.000}(등속이면 같다)");
            Assert.Greater(d1, 0f, "빛은 앞으로 간다");
            Assert.AreEqual(d1, d2, (UiKit.ShineTo - UiKit.ShineFrom) * 0.02f,
                "1/4~1/2 와 1/2~3/4 의 이동량이 같아야 한다 = 등속(InOutSine 이면 가운데가 더 크다 · T153 2항)");
            Assert.AreEqual(UiKit.ShineFrom, At(0f), 0.001f, "0 에서는 시작 값"); Assert.AreEqual(UiKit.ShineTo, At(1f), 0.001f, "1 에서는 끝 값(화면 밖)");

            _log.AssertNoRed("shine 등속");
            yield return Shutdown();
        }
    }
}
