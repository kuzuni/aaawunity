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
    /// T72 «질감 3종» 헬퍼 계약(주인 2026-09-06 «Pattern_01_256 거의 모든 UI 에 · 아이콘 뒤 Effect_Light 천천히 회전 · 그라데이션 색감») — 실제 씬(App) 위에 시험 칸을 세우고
    /// ① <see cref="UiKit.PatternBg"/>: «Pattern» RawImage · 텍스처 Repeat(.meta) · uvRect 크기 = 사각형 ÷ 256 · 시간이 멈춘 중(timeScale 0)에도 uvRect.position 이 <b>줄어</b>(= 그림이 오른쪽 위로) 흐른다 · raycast 끔 · 알파 0.08~0.15
    /// ② <see cref="UiKit.LightBehind"/>: «LightMask»(RectMask2D) 안 «Light» · 아이콘 <b>뒤</b>(형제 순서 앞) · 한 변 = 아이콘 긴 변 × 1.9 · 아이콘 중심 · <b>시계방향</b> 회전(unscaled) · <see cref="UiKit.SetLightSpinning"/> 으로 멈춤
    /// ③ <see cref="UiKit.Gradient"/>: «GradientTop»/«GradientBottom» 두 장 · Gradient 스프라이트 · 글자·아이콘 아래(형제 순서 앞) · raycast 끔
    /// 공통: 두 번 불러도 조각이 늘지 않고, 칸이 파괴되면 트윈이 남지 않으며(SetLink · T56), 빨간 줄 0(<see cref="PlayLog"/>). 화면별 «어디에 있나» 는 T63/T69 화면 묶음 테스트가 <see cref="UiKit.HasPattern"/>·<see cref="UiKit.HasLight"/>·<see cref="UiKit.HasGradient"/> 로 단언한다.
    /// </summary>
    public class UiTextureTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

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
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }
        static int CountNamed(Transform t, string name) { int n = 0; for (int i = 0; i < t.childCount; i++) if (t.GetChild(i).name == name) n++; return n; }

        [UnityTest]
        public IEnumerator PatternLightGradientHelpersFlowSpinAndLayer()
        {
            yield return Boot();
            // 시험 칸: 프레임 안 80%×40% 초록 판(로비 배경 같은 «밝은 바탕») + 가운데 아이콘
            var host = UiKit.Rect(_app.Frame, "T72Host"); UiKit.Pct(host, 10f, 10f, 80f, 40f);
            var bg = host.gameObject.AddComponent<Image>(); bg.color = Palette.Green; bg.raycastTarget = false;
            var icon = UiKit.Icon(host, "Icon", "ui.iconClock"); UiKit.Pct(icon.rectTransform, 35f, 20f, 30f, 60f);
            Canvas.ForceUpdateCanvases();

            // ① 패턴
            var raw = UiKit.PatternBg(host);
            Assert.IsNotNull(raw, "PatternBg 는 RawImage 를 돌려준다(ui.pattern 카탈로그)");
            Assert.AreEqual(UiKit.PatternName, raw.name); Assert.AreEqual(host, raw.transform.parent, "Pattern 은 host 의 자식");
            Assert.AreEqual(0, raw.transform.GetSiblingIndex(), "Pattern 은 host 배경 바로 위(형제 0) — 아이콘·글자 아래");
            Assert.IsFalse(raw.raycastTarget, "Pattern raycast 끔");
            Assert.IsNotNull(raw.texture, "Pattern 텍스처"); Assert.IsTrue(raw.texture.name.StartsWith("Pattern_01"), "텍스처 = Pattern_01_256 (" + raw.texture.name + ")");
            Assert.AreEqual(TextureWrapMode.Repeat, raw.texture.wrapMode, "Pattern_01_256.png.meta 는 wrapU/V = Repeat(타일링) 이어야 한다");
            Assert.That(raw.color.a, Is.InRange(0.08f, 0.15f), "패턴 알파 0.08~0.15(은은한 무늬)");
            Assert.IsTrue(UiKit.HasPattern(host), "HasPattern");
            Canvas.ForceUpdateCanvases();
            var hr = ((RectTransform)raw.transform).rect;
            Assert.AreEqual(hr.width / UiKit.PatternTilePx, raw.uvRect.width, 0.02f, "uvRect 폭 = 사각형 폭 ÷ 256(타일 한 변 = 프레임 256px)");
            Assert.AreEqual(hr.height / UiKit.PatternTilePx, raw.uvRect.height, 0.02f, "uvRect 높이 = 사각형 높이 ÷ 256");

            // ② 빛살
            var light = UiKit.LightBehind(host, icon.rectTransform);
            Assert.IsNotNull(light, "LightBehind 는 Image 를 돌려준다(ui.light1 카탈로그)");
            Assert.AreEqual(UiKit.LightName, light.name);
            var mask = light.transform.parent; Assert.AreEqual(UiKit.LightMaskName, mask.name, "Light 는 LightMask 안");
            Assert.IsNotNull(mask.GetComponent<RectMask2D>(), "LightMask = RectMask2D(빛살이 칸 밖으로 안 나간다)");
            Assert.AreEqual(host, mask.parent, "LightMask 는 host 의 자식");
            Assert.Less(mask.GetSiblingIndex(), icon.transform.GetSiblingIndex(), "빛살은 아이콘 «뒤»(형제 순서 앞)");
            Assert.Greater(mask.GetSiblingIndex(), raw.transform.GetSiblingIndex(), "빛살은 패턴 위");
            Assert.IsFalse(light.raycastTarget, "Light raycast 끔"); Assert.IsNotNull(light.sprite); Assert.IsTrue(light.sprite.name.StartsWith("Effect_Light"), "스프라이트 = Effect_Light_01_512 (" + light.sprite.name + ")");
            Assert.That(light.color.a, Is.InRange(0.5f, 0.7f), "빛살 알파 0.5~0.7");
            var lrt = light.rectTransform; var irt = icon.rectTransform;
            float side = Mathf.Max(irt.rect.width, irt.rect.height) * UiKit.LightScale;
            Assert.AreEqual(side, lrt.rect.width, 1f, "빛살 한 변 = 아이콘 긴 변 × " + UiKit.LightScale); Assert.AreEqual(side, lrt.rect.height, 1f, "정사각");
            var lc = host.InverseTransformPoint(lrt.TransformPoint(lrt.rect.center)); var ic = host.InverseTransformPoint(irt.TransformPoint(irt.rect.center));
            Assert.AreEqual(ic.x, lc.x, 1f, "빛살 중심 x = 아이콘 중심"); Assert.AreEqual(ic.y, lc.y, 1f, "빛살 중심 y = 아이콘 중심");
            Assert.IsTrue(UiKit.HasLight(host), "HasLight");

            // ③ 그라데이션
            UiKit.Gradient(host);
            var gt = host.Find(UiKit.GradientTopName); var gb = host.Find(UiKit.GradientBottomName);
            Assert.IsNotNull(gt, "GradientTop"); Assert.IsNotNull(gb, "GradientBottom");
            var gti = gt.GetComponent<Image>(); var gbi = gb.GetComponent<Image>();
            Assert.IsTrue(gti.sprite != null && gti.sprite.name.StartsWith("Gradient_Top"), "위 = Gradient_Top_01"); Assert.IsTrue(gbi.sprite != null && gbi.sprite.name.StartsWith("Gradient_Bottom"), "아래 = Gradient_Bottom");
            Assert.IsFalse(gti.raycastTarget); Assert.IsFalse(gbi.raycastTarget);
            Assert.Less(gt.GetSiblingIndex(), icon.transform.GetSiblingIndex(), "그라데이션은 아이콘 아래"); Assert.AreEqual(gt.GetSiblingIndex() + 1, gb.GetSiblingIndex(), "위·아래 두 장이 나란히");
            Assert.Greater(gti.color.a, 0.05f, "위 밝음 알파"); Assert.Greater(gbi.color.a, 0.05f, "아래 어둠 알파");
            Assert.IsTrue(UiKit.HasGradient(host), "HasGradient");
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(host.rect.width, ((RectTransform)gt).rect.width, 0.5f, "그라데이션은 칸 전체 폭(Stretch)");

            // 흐름·회전은 시간이 멈춘 팝업 중에도(unscaled) — timeScale 0 에서 실측
            Time.timeScale = 0f;
            var p0 = raw.uvRect.position; var r0 = lrt.localRotation;
            yield return RealSeconds(0.4f);
            var p1 = raw.uvRect.position;
            Assert.Less(p1.x, p0.x, "패턴 uvRect.x 가 줄어야 무늬가 오른쪽으로 간다(결정 157)"); Assert.Less(p1.y, p0.y, "uvRect.y 가 줄어야 무늬가 위로 간다");
            Assert.AreEqual(p0.x - p1.x, p0.y - p1.y, 0.002f, "대각선(오른쪽 위 45°)");
            float ang = Vector3.SignedAngle(r0 * Vector3.up, lrt.localRotation * Vector3.up, Vector3.forward);
            Assert.Less(ang, -1f, "빛살은 시계방향(z 각이 줄어든다) · 0.4s 에 " + ang.ToString("0.0") + "°");
            Assert.Greater(ang, -30f, "천천히(한 바퀴 " + UiKit.LightPeriod + "s)");
            UiKit.SetLightSpinning(host, false);
            var r2 = lrt.localRotation; yield return RealSeconds(0.25f);
            Assert.AreEqual(0f, Vector3.SignedAngle(r2 * Vector3.up, lrt.localRotation * Vector3.up, Vector3.forward), 0.01f, "SetLightSpinning(false) 면 멈춘다");
            UiKit.SetLightSpinning(host, true);
            yield return RealSeconds(0.2f);
            Assert.Less(Vector3.SignedAngle(r2 * Vector3.up, lrt.localRotation * Vector3.up, Vector3.forward), -0.5f, "다시 켜면 돈다");
            Time.timeScale = 1f;

            // 두 번 불러도 조각이 늘지 않는다(갱신만)
            UiKit.PatternBg(host, UiKit.PatternTintDark); UiKit.LightBehind(host, icon.rectTransform); UiKit.Gradient(host);
            Assert.AreEqual(1, CountNamed(host, UiKit.PatternName), "Pattern 1개"); Assert.AreEqual(1, CountNamed(host, UiKit.LightMaskName), "LightMask 1개");
            Assert.AreEqual(1, CountNamed(host, UiKit.GradientTopName), "GradientTop 1개"); Assert.AreEqual(1, CountNamed(host, UiKit.GradientBottomName), "GradientBottom 1개");
            Assert.AreEqual(1, CountNamed(mask, UiKit.LightName), "Light 1개");
            Assert.Less(mask.GetSiblingIndex(), icon.transform.GetSiblingIndex(), "다시 불러도 빛살은 아이콘 뒤에 남는다(형제 순서)");
            Assert.AreEqual(0, raw.transform.GetSiblingIndex(), "다시 불러도 Pattern 은 형제 0");
            Assert.AreEqual(1f, raw.color.r, 0.001f, "tint 갱신(어두운 바탕용 White)");

            // 파괴 → 트윈 0(SetLink · KillTweens 없이도)
            var rawRef = raw; var lightRef = lrt;
            Object.Destroy(host.gameObject);
            yield return Frames(2);
            Assert.IsFalse(UiKit.IsTweening(rawRef), "칸이 파괴되면 패턴 트윈이 남지 않는다");
            Assert.IsFalse(UiKit.IsTweening(lightRef), "칸이 파괴되면 빛살 트윈이 남지 않는다");
            _log.AssertNoRed("T72 질감 헬퍼");
            yield return Shutdown();
        }

        /// <summary>
        /// T72 2단계(화면 적용) — ⓐ <b>공통 팝업 상자 안</b>에 패턴이 깔린다(<see cref="UiKit.Popup"/> 한 곳이라 팝업 20여 개가 같이 · 조각의 «Bg» 바로 위 · «Border»·«DecoLine» 아래 · 둥근 모서리 안쪽 <see cref="UiKit.PopupPatternInset"/>)
        /// ⓑ <b>펫 탭(13)</b> 풀스크린 배경에 패턴(어두운 바탕 → 흰 무늬) ⓒ <b>펫 세부(14)</b> 아이콘 뒤 빛살(ROUTINE T72 2항 «펫 세부의 아이콘» · 조각 안 «Item» 바로 뒤). 빨간 줄 0.
        /// </summary>
        [UnityTest]
        public IEnumerator PopupBoxAndPetScreenCarryTheTexture()
        {
            yield return Boot();

            // ⓑ 펫 탭 = 풀스크린 배경 패턴(바탕 Image 바로 위 = 형제 0 · 상단 바·격자·탭 바 아래)
            _app.ShowScreen("pet"); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var pet = _app.Current.Root;
            Assert.IsTrue(UiKit.HasPattern(pet), "펫 탭(13) 배경에 패턴(T72 ①)");
            var petPat = pet.Find(UiKit.PatternName);
            Assert.AreEqual(0, petPat.GetSiblingIndex(), "패턴은 바탕 바로 위(형제 0) — 상단 바·격자·탭 바 아래");
            Assert.AreEqual(1f, petPat.GetComponent<RawImage>().color.r, 0.001f, "어두운 바탕이라 흰 무늬(PatternTintDark)");

            // ⓐ·ⓒ 펫 세부 팝업(14) — 공통 팝업 상자 안 패턴 + 아이콘 뒤 빛살
            _app.GetScreen<PetScreen>().OpenDetail(0); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var box = UiKit.Find(_app.Overlay.Root, "ui.popup");
            Assert.IsNotNull(box, "펫 세부 = 공통 팝업 상자(ui.popup)");
            Assert.IsTrue(UiKit.HasPattern(box), "팝업 상자 «안» 에 패턴(T72 ① · UiKit.Popup 한 곳)");
            var pat = box.Find(UiKit.PatternName); var bgc = box.Find("Bg"); var border = box.Find("Border");
            Assert.IsNotNull(bgc, "팝업 조각의 Bg");
            Assert.AreEqual(bgc.GetSiblingIndex() + 1, pat.GetSiblingIndex(), "패턴은 조각의 Bg 바로 위");
            if (border != null) Assert.Less(pat.GetSiblingIndex(), border.GetSiblingIndex(), "패턴은 테두리 아래(테두리가 무늬에 안 가린다)");
            var prt = (RectTransform)pat; var brt = (RectTransform)box;
            Assert.AreEqual(brt.rect.width - 2f * UiKit.PopupPatternInset, prt.rect.width, 1f, "둥근 모서리 안쪽으로 " + UiKit.PopupPatternInset + "px 들여 깐다(사각 무늬가 모서리 밖으로 안 나간다)");
            Assert.IsFalse(prt.GetComponent<RawImage>().raycastTarget, "패턴은 클릭을 안 먹는다(배경 탭으로 닫기 그대로)");

            var cell = UiKit.Find(box, "PetDetailCell"); Assert.IsNotNull(cell, "펫 칸(세부)");
            var item = UiKit.Find(cell, "Item"); Assert.IsNotNull(item, "펫 아이콘(조각의 Item)");
            var frame = item.parent;
            Assert.IsTrue(UiKit.HasLight(frame), "펫 세부 아이콘 뒤 빛살(T72 ②)");
            Assert.Less(frame.Find(UiKit.LightMaskName).GetSiblingIndex(), item.GetSiblingIndex(), "빛살은 아이콘 «뒤»(형제 순서 앞)");

            // 시간이 멈춘 팝업 중에도 흐르고 돈다(unscaled) — 화면 적용도 헬퍼 계약 그대로
            Time.timeScale = 0f;
            var lrt = (RectTransform)frame.Find(UiKit.LightMaskName + "/" + UiKit.LightName);
            var praw = pat.GetComponent<RawImage>(); var p0 = praw.uvRect.position; var r0 = lrt.localRotation;
            yield return RealSeconds(0.4f);
            Assert.Less(praw.uvRect.position.x, p0.x, "팝업 패턴도 오른쪽 위로 흐른다(팝업 시간 정지 중에도)");
            Assert.Less(Vector3.SignedAngle(r0 * Vector3.up, lrt.localRotation * Vector3.up, Vector3.forward), -0.5f, "팝업 빛살도 시계방향으로 돈다");
            Time.timeScale = 1f;

            _app.Overlay.Close(); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "닫힘");
            Assert.IsFalse(UiKit.IsTweening(lrt), "팝업이 닫히면 빛살 트윈도 없다(SetLink · T56)");
            _log.AssertNoRed("T72 화면 적용(팝업 · 펫)");
            yield return Shutdown();
        }
    }
}
