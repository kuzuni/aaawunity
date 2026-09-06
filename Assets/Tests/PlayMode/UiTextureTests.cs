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

        /// <summary>
        /// T72 2단계 2차(상점 09·10) — 주인 원문 «상점 아이템, 특별 상품 이런 것들 아이콘 뒤에 Effect_Light_01_512 이런 거 있어야 하고 천천히 오른쪽으로 회전하는 느낌» +
        /// «Pattern_01_256 이거들이 모든 UI 에 다 있어야 함». ⓐ 상점 풀스크린 배경에 패턴(어두운 회색 바탕 → 흰 무늬 · 배경 조각 바로 위) ⓑ 대형 상자 배너·상자 카드 2·다이아 6·골드 3 의 그림 뒤 빛살 ⓒ 빛살은 <b>시계방향</b>
        /// ⓓ T72 4항 = 스크롤 밖 칸은 멈춘다(맨 위로 올리면 아래 칸이 정지 · 내리면 다시 돈다). 빨간 줄 0.
        /// </summary>
        [UnityTest]
        public IEnumerator ShopScreenCarriesPatternAndItemLights()
        {
            yield return Boot();
            _app.ShowScreen("shop"); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var shop = _app.Current.Root;

            // ⓐ 배경 패턴 — 배경 조각(Background)이 형제 0 이므로 패턴은 그 «바로 위» 형제 1
            Assert.IsTrue(UiKit.HasPattern(shop), "상점 배경에 패턴(T72 ①)");
            var pat = shop.Find(UiKit.PatternName);
            Assert.AreEqual(1, pat.GetSiblingIndex(), "패턴은 어두운 바탕 조각 바로 위(천막·스크롤·상단 바·탭 바 아래)");
            var praw = pat.GetComponent<RawImage>();
            Assert.AreEqual(1f, praw.color.r, 0.001f, "어두운 바탕이라 흰 무늬(PatternTintDark)");
            Assert.IsFalse(praw.raycastTarget, "패턴은 클릭을 안 먹는다(스크롤 그대로)");

            // ⓑ 그림 뒤 빛살 — 대형 배너 1 + 상자 카드 2 + 다이아 6 + 골드 3
            var content = UiKit.Find(shop, "Scroll/Content");
            Assert.IsNotNull(content, "상점 스크롤 Content");
            int boxes = 0, packs = 0; Transform firstBox = null;
            for (int i = 0; i < content.childCount; i++)
            {
                var c = content.GetChild(i);
                if (c.name.StartsWith("Box:")) { Assert.IsTrue(UiKit.HasLight(c), c.name + " 상자 그림 뒤 빛살"); if (firstBox == null) firstBox = c; boxes++; }
                else if (c.name.StartsWith("GemPack:") || c.name.StartsWith("GoldPack:"))
                {
                    var cell = c.childCount > 0 ? c.GetChild(0) : null;   // 조각(ListItem_ShopItem)
                    Assert.IsNotNull(cell, c.name + " 안의 상품 조각");
                    Assert.IsTrue(UiKit.HasLight(cell), c.name + " 상품 아이콘 뒤 빛살");
                    var icon = cell.Find("Icon"); Assert.IsNotNull(icon, c.name + " 아이콘");
                    Assert.Less(cell.Find(UiKit.LightMaskName).GetSiblingIndex(), icon.GetSiblingIndex(), c.name + ": 빛살은 아이콘 «뒤»(형제 순서 앞)");
                    packs++;
                }
            }
            Assert.AreEqual(3, boxes, "상자 = 대형 배너 1 + 카드 2(레퍼런스 10)");
            Assert.AreEqual(9, packs, "상품 칸 = 다이아 6 + 골드 3(레퍼런스 09)");

            // ⓒ 시계방향 + 패턴 흐름 — 팝업이 아니어도 unscaled 로 돈다
            var shopScreen = _app.GetScreen<ShopScreen>();
            shopScreen.ScrollTo(1f); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var bigLight = (RectTransform)firstBox.Find(UiKit.LightMaskName + "/" + UiKit.LightName);   // 맨 위(10) 에서 보이는 대형 상자 배너
            var p0 = praw.uvRect.position; var r0 = bigLight.localRotation;
            yield return RealSeconds(0.4f);
            Assert.Less(praw.uvRect.position.x, p0.x, "상점 패턴도 오른쪽 위로 흐른다");
            Assert.Less(Vector3.SignedAngle(r0 * Vector3.up, bigLight.localRotation * Vector3.up, Vector3.forward), -0.5f, "빛살은 시계방향(주인 «오른쪽으로»)");

            // ⓓ 4항 «보이는 칸만» — 맨 위(10) 에서는 맨 아래 골드 칸이 멈춰 있다
            var lastGold = UiKit.Find(content, "GoldPack:2");
            Assert.IsNotNull(lastGold, "골드 마지막 칸");
            var goldLight = lastGold.GetChild(0).Find(UiKit.LightMaskName + "/" + UiKit.LightName);
            // 멈춤은 «트윈이 없다» 가 아니라 «각이 안 변한다» 로 잰다 — DOTween.IsTweening 은 멈춘(Pause) 트윈도 «활성» 으로 본다(CI #145 에서 확인)
            var stop0 = goldLight.localRotation; yield return RealSeconds(0.4f);
            Assert.AreEqual(0f, Quaternion.Angle(stop0, goldLight.localRotation), 0.01f, "스크롤 밖 칸(맨 아래 골드)은 빛살이 멈춘다(T72 4항)");
            shopScreen.ScrollTo(0f); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var g1 = goldLight.localRotation; yield return RealSeconds(0.4f);
            Assert.Less(Vector3.SignedAngle(g1 * Vector3.up, goldLight.localRotation * Vector3.up, Vector3.forward), -0.5f, "맨 아래로 내리면 그 칸 빛살이 다시 돈다");

            _log.AssertNoRed("T72 화면 적용(상점)");
            yield return Shutdown();
        }

        /// <summary>
        /// T72 2단계 3차(던전·아레나 20~26) — ⓐ 네 페이지가 같이 쓰는 풀스크린 배경 패턴(어두운 바탕 → 흰 무늬 · 바탕 조각 바로 위 · 오른쪽 위로) ⓑ 던전 카드 보상 아이콘(2+4)·던전 세부 팝업 보상 칸(4)·순위 보상 팝업 보상 칸(8)·상인 상품 칸(11) 아이콘 뒤 빛살(시계방향)
        /// ⓒ 순위 보상 팝업의 붉은 티어 띠 안 무늬(레퍼런스 25) ⓓ ③ 그라데이션 = 카드·팝업 제목 띠 ⓔ T72 4항 = 상인 페이지를 안 보고 있으면 그 빛살은 멈춘다. 빨간 줄 0.
        /// </summary>
        [UnityTest]
        public IEnumerator DungeonArenaScreensCarryPatternAndRewardLights()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var ev = _app.GetScreen<EventsScreen>(); Assert.IsNotNull(ev, "던전·아레나 화면");
            var root = ev.Root;

            // ⓐ 배경 패턴 — 바탕 조각 «Bg» 가 형제 0 이므로 패턴은 형제 1(페이지·상단 바는 그 위)
            Assert.IsTrue(UiKit.HasPattern(root), "던전·아레나 배경에 패턴(T72 ①)");
            var pat = root.Find(UiKit.PatternName);
            Assert.AreEqual(1, pat.GetSiblingIndex(), "패턴은 어두운 바탕 조각 바로 위");
            var praw = pat.GetComponent<RawImage>();
            Assert.AreEqual(1f, praw.color.r, 0.001f, "어두운 바탕이라 흰 무늬(PatternTintDark)");
            Assert.IsFalse(praw.raycastTarget, "패턴은 클릭을 안 먹는다");

            // ⓑ 던전 카드 보상 아이콘 뒤 빛살(카드 1 = 2칸 · 카드 2 = 4칸)
            var hell = UiKit.Find(root, "Card:hell"); var exp = UiKit.Find(root, "Card:expedition");
            Assert.IsNotNull(hell); Assert.IsNotNull(exp);
            int cells = 0;
            foreach (var name in new[] { "Card:hell", "Card:expedition" })
            {
                var rew = UiKit.Find(UiKit.Find(root, name), "Rewards"); Assert.IsNotNull(rew, name + " 보상 아이콘 줄");
                for (int i = 0; i < rew.childCount; i++)
                {
                    var cell = rew.GetChild(i); if (!cell.name.StartsWith("Cell:")) continue;
                    Assert.IsTrue(UiKit.HasLight(cell), name + "/" + cell.name + " 보상 아이콘 뒤 빛살");
                    var icon = cell.Find("Icon"); Assert.IsNotNull(icon, cell.name + " 아이콘");
                    Assert.Less(cell.Find(UiKit.LightMaskName).GetSiblingIndex(), icon.GetSiblingIndex(), cell.name + ": 빛살은 아이콘 «뒤»(형제 순서 앞)");
                    cells++;
                }
            }
            Assert.AreEqual(6, cells, "던전 보상 아이콘 = 카드 1 의 2 + 카드 2 의 4(레퍼런스 20)");

            // ⓓ 그라데이션 = 카드 제목 띠(위 밝음 · 아래 어둠)
            Assert.IsTrue(UiKit.HasGradient(UiKit.Find(hell, "Head")), "던전 카드 제목 띠에 그라데이션(T72 ③)");

            // 패턴은 오른쪽 위로 흐르고 빛살은 시계방향
            var firstLight = (RectTransform)UiKit.Find(hell, "Cell:0").Find(UiKit.LightMaskName + "/" + UiKit.LightName);
            var p0 = praw.uvRect.position; var r0 = firstLight.localRotation;
            yield return RealSeconds(0.4f);
            Assert.Less(praw.uvRect.position.x, p0.x, "던전 배경 패턴도 오른쪽 위로 흐른다");
            Assert.Less(Vector3.SignedAngle(r0 * Vector3.up, firstLight.localRotation * Vector3.up, Vector3.forward), -0.5f, "보상 칸 빛살은 시계방향");

            // ⓑ 던전 세부 팝업(21) 보상 칸 4개 + 제목 띠 그라데이션
            var enter = UiKit.Find(hell, "EnterBtn").GetComponent<Button>(); Assert.IsNotNull(enter, "입장 버튼");
            enter.onClick.Invoke(); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var ov = _app.Overlay.Root;
            int rcells = 0;
            for (int i = 0; i < 4; i++)
            {
                var cell = UiKit.Find(ov, "RewardCell:" + i); Assert.IsNotNull(cell, "세부 팝업 보상 칸 " + i);
                Assert.IsTrue(UiKit.HasLight(cell), "세부 팝업 보상 칸 " + i + " 아이콘 뒤 빛살"); rcells++;
            }
            Assert.AreEqual(4, rcells, "세부 팝업 보상 칸 4(레퍼런스 21)");
            Assert.IsTrue(UiKit.HasGradient(UiKit.Find(ov, "Head")), "팝업 제목 띠에 그라데이션(T72 ③)");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓒ 순위 보상 팝업(25) — 붉은 티어 띠 안 무늬 + 보상 칸 8개 빛살
            ev.ShowPage(EventsScreen.PageArena); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var rewardsBtn = UiKit.Find(root, "RewardsBtn").GetComponent<Button>(); Assert.IsNotNull(rewardsBtn, "아레나 «보상» 버튼");
            rewardsBtn.onClick.Invoke(); yield return Frames(2); Canvas.ForceUpdateCanvases();
            ov = _app.Overlay.Root;
            var band = UiKit.Find(ov, "Tiers"); Assert.IsNotNull(band, "티어 띠");
            Assert.IsTrue(UiKit.HasPattern(band), "붉은 티어 띠 안에도 무늬(레퍼런스 25)");
            int rr = 0;
            foreach (var t in ov.GetComponentsInChildren<Transform>(false))
                if (t.name == "Reward" && UiKit.HasLight(t)) rr++;
            Assert.AreEqual(8, rr, "순위 보상 칸 = 4줄 × (코인·다이아) 전부 빛살");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓑⓔ 상인 페이지(26) 상품 11칸 + T72 4항 «안 보는 페이지는 멈춘다»
            ev.ShowPage(EventsScreen.PageMerchant); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var me = UiKit.Find(root, "Page:merchant"); Assert.IsNotNull(me, "상인 페이지");
            int goods = 0; Transform firstGoods = null;
            for (int i = 0; i < 11; i++)
            {
                var card = UiKit.Find(me, "Goods:" + i); Assert.IsNotNull(card, "상품 카드 " + i);
                var ic = card.Find("IconCell"); Assert.IsNotNull(ic, "상품 " + i + " 아이콘 칸");
                Assert.IsTrue(UiKit.HasLight(ic), "상품 " + i + " 아이콘 뒤 빛살"); if (firstGoods == null) firstGoods = ic; goods++;
            }
            Assert.AreEqual(11, goods, "상인 상품 11칸 전부(레퍼런스 26)");
            var gl = (RectTransform)firstGoods.Find(UiKit.LightMaskName + "/" + UiKit.LightName);
            var g0 = gl.localRotation; yield return RealSeconds(0.4f);
            Assert.Less(Vector3.SignedAngle(g0 * Vector3.up, gl.localRotation * Vector3.up, Vector3.forward), -0.5f, "보고 있는 상인 페이지의 상품 빛살은 돈다");
            // 멈춤은 «트윈이 없다» 가 아니라 «각이 안 변한다» 로 잰다(DOTween.IsTweening 은 멈춘 트윈도 참 · CI #145)
            ev.ShowPage(EventsScreen.PageArena); yield return Frames(2);
            var g1 = gl.localRotation; yield return RealSeconds(0.4f);
            Assert.AreEqual(0f, Quaternion.Angle(g1, gl.localRotation), 0.01f, "다른 페이지로 가면 상인 상품 빛살은 멈춘다(T72 4항)");
            ev.ShowPage(EventsScreen.PageMerchant); yield return Frames(2);
            var g2 = gl.localRotation; yield return RealSeconds(0.4f);
            Assert.Less(Vector3.SignedAngle(g2 * Vector3.up, gl.localRotation * Vector3.up, Vector3.forward), -0.5f, "돌아오면 다시 돈다");

            _log.AssertNoRed("T72 화면 적용(던전·아레나)");
            yield return Shutdown();
        }
    }
}
