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
    /// T186 — 데일리 기프트(17)의 «읽히는가» 셋을 <b>숫자로</b> 지킨다(워커 실측 등재 · `screens` run 283·333 · <c>tools/png_contrast.py</c>).
    /// <para>
    /// 등재된 결함은 전부 «대비» 였고 대비는 이제껏 눈으로만 잡혔다(T84 · T78 · T121 · 여기 넷째) — 그래서 <b>색을 재는 단언</b>으로 남긴다:
    /// ⓐ 팝업 상자 몸통이 어두운가(크림 0.92 → <see cref="Palette.PopupBox"/> 0.20) · <b>금색 테(DecoLine)는 살아 있는가</b>
    /// ⓑ 그 위 «⏱ 종료까지 …» 흰 글자가 바탕과 0.35 이상 벌어지는가(ⓐ 가 풀면 같이 풀리는 자리라 따로 잰다)
    /// ⓒ 노란 리본 제목의 검은 아웃라인이 레퍼런스 굵기(<see cref="LobbyPopups.RibbonOutlineRatio"/>)인가
    /// ⓓ «잠금» 버튼이 <b>알파로 흐려진</b> 것이 아니라 <b>어두운 판 + 흰 글자</b>인가(α 로 흐리면 판·글자가 같이 끌려가 대비가 α 만큼 줄어든다).
    /// </para>
    /// 색만 본다 — 자리·크기·수치·연출은 한 줄도 안 잰다(그쪽은 표 ㉒ 과 <c>UiSmokeTests</c> 몫).
    /// </summary>
    public class DailyGiftLookTests
    {
        App _app; PlayLog _log;

        /// <summary>판정선 — 「글자 휘도 ↔ 바로 뒤 바탕 휘도」 차이(T121 3항 · <c>png_contrast.py</c> 와 같은 값·같은 식).</summary>
        const float MinContrast = 0.35f;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        [UnityTest]
        public IEnumerator DailyGiftPopupIsDarkBoxedAndItsTextsAreReadable()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);
            LobbyPopups.DailyGift(_app); yield return Frames(2);
            UiKit.CompleteAllTweens(); yield return Frames(1);

            var box = (RectTransform)UiKit.Find(_app.Overlay.Root, "DailyGiftBox");
            Assert.IsNotNull(box, "데일리 기프트 상자");

            // ⓐ 몸통은 어둡고 금색 테는 살아 있다 ─────────────────────────────
            Image body = null, deco = null;
            for (int i = 0; i < box.childCount; i++)
            {
                var c = box.GetChild(i); var img = c.GetComponent<Image>(); if (img == null) continue;
                if (c.name == UiKit.CardBodyName) body = img;
                else if (c.name == "DecoLine") deco = img;
            }
            Assert.IsNotNull(body, "상자 몸통(«Bg») 조각");
            Assert.AreEqual(Palette.PopupBox, body.color, "몸통은 공통 팝업과 같은 어두운 회색(T186 ⓐ · 고치기 전 크림 0.92)");
            if (deco != null)
                Assert.Greater(Luma(deco.color), 0.5f, "금색 테(DecoLine)는 어둡게 하지 않는다 — 레퍼런스 17 의 «어두운 몸통 + 금색 테»");

            // ⓑ «⏱ 종료까지 …» 줄이 몸통과 0.35 이상 벌어진다 ─────────────────
            var timer = UiKit.Find(box, "Timer");
            Assert.IsNotNull(timer, "종료 시각 줄");
            var timerTxt = timer.GetComponentInChildren<Text>(true);
            Assert.IsNotNull(timerTxt, "종료 시각 글자");
            Assert.Greater(Mathf.Abs(Luma(timerTxt.color) - Luma(body.color)), MinContrast,
                           "«종료까지» 글자 ↔ 상자 몸통 대비(고치기 전 0.08 = 크림 위 흰 글자)");

            // ⓒ 리본 제목의 검은 아웃라인이 레퍼런스 굵기 ──────────────────────
            var rib = UiKit.Find(box, "ui.title.yellow");
            Assert.IsNotNull(rib, "제목 리본");
            var ribTxt = rib.GetComponentInChildren<Text>(true);
            Assert.IsNotNull(ribTxt, "리본 제목 글자");
            var ol = ribTxt.GetComponent<Outline>();
            Assert.IsNotNull(ol, "리본 제목에도 검은 아웃라인(T63-outline)");
            Assert.AreEqual(UiKit.OutlineColor, ol.effectColor, "아웃라인 색은 공통 규격 그대로");
            float size = ribTxt.resizeTextForBestFit ? Mathf.Max(ribTxt.resizeTextMaxSize, ribTxt.fontSize) : ribTxt.fontSize;
            // 회차 2 — «이 자리만 두껍게» 를 되돌렸다(효과 0.006 · 게다가 OutlineStrict 와 부딪친다 · 결정 483).
            // 그래서 여기서 지키는 것은 «공통 규격 그대로인가» 다 — 어긋나면 TextSizeGateTests 가 빨개지는 자리이기도 하다.
            Assert.AreEqual(UiKit.OutlineWidth(size), Mathf.Abs(ol.effectDistance.x), 0.26f,
                            "리본 제목 아웃라인 두께 = 공통 규격(T63-outline · TextAudit.OutlineStrict 가 같은 식으로 잰다)");
            // 다음 회차가 쓸 숫자 — 리본 글자가 화면에서 실제로 몇 px 로 그려지고 아웃라인이 몇 px 인가(캡처는 프레임의 절반 폭이다)
            float lossy = ribTxt.rectTransform.lossyScale.x;
            Debug.Log($"[T186ⓒ] 리본 제목 크기 {size} · 아웃라인 {Mathf.Abs(ol.effectDistance.x):0.0}px · lossyScale {lossy:0.00} " +
                      $"→ 화면 {Mathf.Abs(ol.effectDistance.x) * lossy:0.0}px · ⚠ 규격과 «화면» 은 다른 값이다(이 줄이 그 차를 보여 준다 · T194 결정 522) · " +
                      "레퍼런스 띠를 내는 규격은 프레임 6.6~8.0px = 비율 0.111~0.130(TextOutlineRuleTests)");

            // ⓓ «잠금» 버튼 = 어두운 판 + 흰 글자(알파로 흐리게 하지 않는다) ────
            Button locked = null;
            foreach (var b in _app.Overlay.Root.GetComponentsInChildren<Button>(true))
            {
                var t = b.GetComponentInChildren<Text>(true);
                if (t != null && t.text == "잠금") { locked = b; break; }
            }
            Assert.IsNotNull(locked, "«잠금» 버튼(무료 칸을 받기 전에는 줄이 잠겨 있다)");
            Assert.IsFalse(locked.interactable, "잠긴 버튼은 눌리지 않는다");
            var cg = locked.GetComponent<CanvasGroup>();
            Assert.IsTrue(cg == null || cg.alpha >= 0.99f, "알파로 흐리게 하지 않는다 — α 를 먹이면 판·글자가 같이 끌려가 대비가 α 배로 준다(T186 ⓓ)");
            Assert.AreEqual(Selectable.Transition.ColorTint, locked.transition, "전이는 ColorTint 그대로(«모든 버튼은 ColorTint» = PressFeedbackTests 계약 · disabledColor 가 흰색이라 판 색이 안 흐려진다)");
            var plate = LobbyPopups.PlateOf((RectTransform)locked.transform);
            Assert.IsNotNull(plate, "잠긴 버튼 판(«보이는» 조각 — 루트 Image 는 투명 히트 영역이다)");
            Assert.Less(Luma(plate.color), 0.25f, "판이 어두워야 흰 글자가 뜬다(고치기 전 0.63)");
            var lockTxt = locked.GetComponentInChildren<Text>(true);
            Assert.Greater(Mathf.Abs(Luma(lockTxt.color) - Luma(plate.color)), MinContrast,
                           "«잠금» 글자 ↔ 판 대비(고치기 전 0.16)");

            _log.AssertNoRed("데일리 기프트 팝업 색");
            _app.Overlay.Close(); yield return Frames(2);
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(2);
        }
    }
}
