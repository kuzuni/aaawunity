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

        /// <summary>
        /// T320 ⓐ — 제목 빛이 <b>주인이 준 구조·값</b>으로 서는가(주인 2026-09-09 인스펙터 스샷 · «저렇게 반 잘리는 식으로 마스크 되게»).
        /// <para>
        /// 여기서 재는 것은 <b>계층 이름 계약</b>과 <b>네 수</b>다 — 그 수는 워커가 고른 것이 아니라 주인이 준 것이라
        /// «px 을 베끼지 않는다»(결정 555)의 예외가 아니라 <b>원문</b>이다(<see cref="Overlay.TitleMaskW"/> 등 상수가 그 출처를 적고 있다).
        /// </para>
        /// ⚠ <b>마지막 한 줄이 요점이다</b> — 빛판 가운데가 마스크 바닥보다 <b>아래</b>여야 «아래 절반이 잘린다».
        /// 여백 부호를 하나라도 뒤집으면 판이 위로 올라가 <b>온전한 원</b>이 되는데, 그것은 이름·크기 단언을 전부 통과한다.
        /// </summary>
        [UnityTest]
        public IEnumerator TitleGlowIsMaskedSoOnlyTheTopHalfShows()
        {
            yield return Boot();
            Time.timeScale = 0f;
            var D = _app.Data;
            var rng = new Mulberry32(11u);
            var G = new BattleState(D, 1, _app.Save.CurBuild(D), rng, new InteractivePolicy(), new RunOptions { EmitEvents = true });
            var offer = Perks.Offer(D, G.Taken, false, rng);
            Assert.Greater(offer.Count, 0, "특전 제안");
            G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
            _app.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick));
            yield return Frames(2);
            UiKit.CompleteAllTweens(); yield return Frames(1);

            var host = UiKit.Find(_app.Overlay.Root, "TitleGlow") as RectTransform;
            Assert.IsNotNull(host, "제목 빛 호스트(TitleGlow)");
            var mask = host.Find("Mask") as RectTransform;
            Assert.IsNotNull(mask, "사각 마스크(TitleGlow/Mask · 주인 구조)");
            var plate = mask.Find(UiKit.LightMaskName) as RectTransform;
            Assert.IsNotNull(plate, "빛판(Mask/LightMask)");
            foreach (var n in new[] { UiKit.GlowName, UiKit.LightName, UiKit.DustName })
                Assert.IsNotNull(plate.Find(n), "빛 세 겹 중 " + n + " (LightMask 아래)");

            var mk = mask.GetComponent<Mask>();
            Assert.IsNotNull(mk, "Mask 컴포넌트");
            Assert.IsFalse(mk.showMaskGraphic, "마스크 판 자체는 안 그린다(Show Mask Graphic ✗)");
            Assert.IsNotNull(mask.GetComponent<Image>(), "스텐실은 그래픽이 있어야 걸린다(빈 흰 Image)");
            Assert.IsNull(mask.GetComponent<RectMask2D>(), "마스크는 하나다(RectMask2D 를 겹쳐 걸지 않는다)");

            Assert.AreEqual(Overlay.TitleMaskW, mask.rect.width, 0.5f, "마스크 폭(주인 값)");
            Assert.AreEqual(Overlay.TitleMaskH, mask.rect.height, 0.5f, "마스크 높이(주인 값)");
            Assert.AreEqual(0f, mask.anchoredPosition.x, 0.5f, "마스크는 가운데");
            // T369(주인 2026-09-10 «타이틀 밑으로 라이트 보이는 경우들 … 타이틀 위로만») — 마스크 바닥은 리본 **몸통** 밑단보다 아래면 안 된다.
            //   이 리본(`Title_01_NoDeco_Tangerine`)의 몸통은 rect 높이의 18.26% 위에서 끝난다(꼬리가 아래로 늘어진 조각 · PNG 실측).
            //   주인 값의 마스크 바닥(27.27%)은 이 리본 몸통 밑단(런 875 `04_perks` 실측 29.5%)보다 **이미 위**라 여기서는 올릴 것이 0 이고
            //   (리본 밑이 (42,32,29) 배경 · 새는 띠 0), `Overlay.RibbonGlowLift` 는 그때만 0 이 아닌 안전망이다 — 올린 만큼은 마스크 자신에서 읽는다.
            var ribbon = UiKit.Find(_app.Overlay.Root, "Title_01_NoDeco_Tangerine") as RectTransform;
            Assert.IsNotNull(ribbon, "«레벨 업» 리본");
            float frac = Overlay.RibbonBodyBottomFrac(ribbon);
            Assert.AreEqual(21f / 115f, frac, 1e-4f, "이 리본 조각(Title_01 계열)의 몸통 밑 여백 비 = 21/115(PNG 실측)");
            float lift = mask.anchoredPosition.y - Overlay.TitleMaskY;
            Assert.GreaterOrEqual(lift, -0.5f, "마스크는 주인 값보다 아래로 내려가지 않는다(«위로만»)");
            float maskBottomW = mask.TransformPoint(new Vector3(0f, mask.rect.yMin, 0f)).y;
            float bodyBottomW = ribbon.TransformPoint(new Vector3(0f, ribbon.rect.yMin + ribbon.rect.height * frac, 0f)).y;
            float k = _app.UiCanvas.transform.lossyScale.y;
            Assert.GreaterOrEqual(maskBottomW, bodyBottomW - 1f * k, "마스크 바닥 ≥ 리본 «몸통» 밑단(월드 · T369 «리본 밑으로 새는 빛 0»)");
            if (maskBottomW >= bodyBottomW) Assert.AreEqual(0f, lift, 0.5f, "몸통보다 이미 위면 주인 값 그대로다(올림 0)");
            Assert.AreEqual(Overlay.TitleMaskY + lift, mask.anchoredPosition.y, 0.5f, "마스크 세로 자리 = 주인 값 + 올린 만큼");

            Assert.AreEqual(Overlay.TitleLightL, plate.offsetMin.x, 0.5f, "빛판 Left(주인 값)");
            Assert.AreEqual(Overlay.TitleLightB - lift, plate.offsetMin.y, 0.5f, "빛판 Bottom(주인 값 − 올린 만큼 · 부채 자리는 그대로)");
            Assert.AreEqual(-Overlay.TitleLightR, plate.offsetMax.x, 0.5f, "빛판 Right(인스펙터 Right → offsetMax 는 부호가 뒤집힌다)");
            Assert.AreEqual(-Overlay.TitleLightT - lift, plate.offsetMax.y, 0.5f, "빛판 Top(같은 부호 규칙 · 같은 되내림)");
            Assert.Greater(plate.rect.width, mask.rect.width, "빛판은 마스크보다 넓다(음수 여백)");
            Assert.Greater(plate.rect.height, mask.rect.height, "빛판은 마스크보다 높다");

            // ⓐ 의 뜻 — 빛판 가운데가 마스크 바닥보다 «아래» 라야 원의 아래 절반이 잘린다.
            float plateCenterY = (plate.offsetMin.y + plate.offsetMax.y) * 0.5f;   // 마스크 가운데 기준
            Assert.Less(plateCenterY, -mask.rect.height * 0.5f + 20f,
                "빛판 가운데가 마스크 바닥 언저리다 = 빛의 아래 절반이 잘린다(주인 «반 잘리는 식으로»)");

            Debug.Log($"[T320] 마스크 {mask.rect.width:0.0}×{mask.rect.height:0.0} @y{mask.anchoredPosition.y:0.0} · " +
                      $"빛판 {plate.rect.width:0.0}×{plate.rect.height:0.0} 가운데 y{plateCenterY:0.0}(마스크 바닥 {-mask.rect.height * 0.5f:0.0})");

            _log.AssertNoRed("제목 빛 마스크");
            yield return Shutdown();
        }
    }
}
