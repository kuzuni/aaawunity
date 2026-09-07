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
    /// ① <see cref="UiKit.PatternBg"/>: «Pattern» RawImage · 텍스처 Repeat(.meta) · uvRect 크기 = 사각형 ÷ 256 · 시간이 멈춘 중(timeScale 0)에도 uvRect.position 이 <b>줄어</b>(= 그림이 오른쪽 위로) 흐른다 · raycast 끔 · 알파 3/255 · 한 타일 10~15초(주인 확정 2026-09-07)
    /// ② <see cref="UiKit.LightBehind"/>: «LightMask»(RectMask2D) 안 «Light» · 아이콘 <b>뒤</b>(형제 순서 앞) · 한 변 = 아이콘 긴 변 × 1.9 · 아이콘 중심 · <b>시계방향</b> 회전(unscaled) · <see cref="UiKit.SetLightSpinning"/> 으로 멈춤
    /// ③ <see cref="UiKit.Gradient"/>: «GradientTop»/«GradientBottom» 두 장 · Gradient 스프라이트 · 글자·아이콘 아래(형제 순서 앞) · raycast 끔
    /// 공통: 두 번 불러도 조각이 늘지 않고, 칸이 파괴되면 트윈이 남지 않으며(SetLink · T56), 빨간 줄 0(<see cref="PlayLog"/>). 화면별 «어디에 있나» 는 T63/T69 화면 묶음 테스트가 <see cref="UiKit.HasPattern"/>·<see cref="UiKit.HasLight"/>·<see cref="UiKit.HasGradient"/> 로 단언한다.
    /// </summary>
    public class UiTextureTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; SafeAreaRoot.Override = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

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
            Assert.AreEqual(3f / 255f, raw.color.a, 0.002f, "패턴 알파 = 주인 확정 «255 중 3»(2026-09-07 · 아주 은은하게)");
            Assert.That(UiKit.PatternTileSeconds, Is.InRange(10f, 15f), "패턴 한 타일 10~15초(주인 확정 «속도 2배»)");
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
            Assert.AreEqual(68f / 255f, light.color.a, 0.01f, "빛살 알파 = 주인 확정 «255 중 68»(2026-09-07)");
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

        /// <summary>
        /// T72 2단계 4차(결과 팝업 + 공통 팝업 그라데이션) — ⓐ 상자 없이 어둠 위에 조립되는 프리팹 팝업(레벨업 3택 04 · 승리 · 사망)의 어둠 «바로 위» 배경 무늬(흰 무늬 · 오른쪽 위로)
        /// ⓑ 승리·사망 팝업 보상 칸의 골드 그림 뒤 빛살(시계방향 · 아이콘 «뒤» 형제 · 한 변 &gt; 0 = 배치가 끝난 뒤에 걸었다는 뜻 · 결정 174)
        /// ⓒ ③ 그라데이션을 <see cref="UiKit.Popup"/> 한 곳에서 공통 팝업 상자에 깐다(패턴 «위» · 층 순서 결정 171). 팝업 시간 정지(timeScale 0) 중에도 흐르고 돈다. 빨간 줄 0.
        /// </summary>
        [UnityTest]
        public IEnumerator ResultPopupsCarryPatternAndRewardLights()
        {
            yield return Boot();
            _app.StartBattle(1); yield return Frames(2);
            var bs = _app.GetScreen<BattleScreen>(); var G = bs != null ? bs.G : null; Assert.IsNotNull(G, "전투 상태");
            Time.timeScale = 0f;
            if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; yield return Frames(1); }
            G.Gold = 12750; G.Kills = 137;

            // ⓐⓑ 승리 팝업 — 어둠 바로 위 무늬 + 보상(골드) 칸 빛살
            _app.Overlay.Clear(G, false, () => { }, () => { }); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var win = UiKit.Find(_app.Overlay.Root, "ui.resultWin"); Assert.IsNotNull(win, "승리 팝업 조각(Play_Result_Win_01)");
            // T110 ⓑ(주인 2026-09-07 «클리어 팝업에는 패턴으로 움직이는 그거 있으면 안 됨») — 결과 팝업은 이제 무늬가 «없어야» 한다(T72 ① 은 다른 화면 그대로)
            Assert.IsFalse(UiKit.HasPattern(win), "승리 팝업에는 흐르는 무늬가 없다(T110 ⓑ · 주인 지시로 T72 ① 에서 뺐다)");
            var wdim = win.Find("Dimmed"); Assert.IsNotNull(wdim, "어둠 조각");
            // T110 ⓒ — 제목 조각 안 빛살(SampleEffect)은 켜져 있고 시계방향으로 돈다(unscaled · 팝업 중 시간이 멈춰도)
            var tfx = UiKit.Find(win, "SampleEffect"); Assert.IsNotNull(tfx, "제목 빛살 조각(SampleEffect)");
            Assert.IsTrue(tfx.gameObject.activeInHierarchy, "제목 빛살은 켜져 있다(T110 ⓒ)");
            var tr0 = tfx.localRotation;
            // T110 ⓓ — 등장 폭죽 두 장(프리팹 조각 + 좌우 반전 복제)이 생겼다가 사라진다
            var cfL = UiKit.Find(win, "SampleEffect_Confetti"); var cfR = UiKit.Find(win, "SampleEffect_Confetti_R");
            Assert.IsNotNull(cfL, "폭죽 왼쪽(T110 ⓓ)"); Assert.IsNotNull(cfR, "폭죽 오른쪽(좌우 반전 복제)");
            Assert.IsTrue(cfL.gameObject.activeInHierarchy, "폭죽은 켜져 있다(여태 Hide 로 꺼 두던 조각)");
            var items = UiKit.Find(win, "Group_RewardItem"); Assert.IsNotNull(items, "보상 줄");
            var goldCell = items.GetChild(0);
            Assert.IsTrue(UiKit.HasLight(goldCell), "클리어 보상(골드) 그림 뒤 빛살(T72 ②)");
            var wlight = (RectTransform)goldCell.Find(UiKit.LightMaskName + "/" + UiKit.LightName);
            Assert.Greater(wlight.rect.width, 1f, "빛살 한 변 > 0 — 배치가 끝난 뒤에 걸었다(결정 174)");
            // 팝업 시간 정지 중에도(unscaled) 보상 빛살과 제목 빛살이 시계방향으로 돈다
            var wr0 = wlight.localRotation;
            yield return RealSeconds(0.4f);
            Assert.Less(Vector3.SignedAngle(wr0 * Vector3.up, wlight.localRotation * Vector3.up, Vector3.forward), -0.5f, "보상 빛살은 시계방향");
            Assert.Less(Vector3.SignedAngle(tr0 * Vector3.up, tfx.localRotation * Vector3.up, Vector3.forward), -0.5f, "제목 빛살도 시계방향으로 돈다(T110 ⓒ)");
            yield return RealSeconds(Overlay.ConfettiSec + 0.6f);
            Assert.IsTrue(UiKit.Find(win, "SampleEffect_Confetti_R") == null || !UiKit.Find(win, "SampleEffect_Confetti_R").gameObject.activeInHierarchy, "폭죽은 다 터지면 사라진다(T110 ⓓ)");
            _app.Overlay.Close(); yield return Frames(2);
            Assert.IsFalse(UiKit.IsTweening(wlight), "팝업이 닫히면 빛살 트윈도 없다(SetLink · T56)");

            // ⓐⓑ 사망 팝업 — 같은 두 가지
            _app.Overlay.Dead(G, () => { }); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var lose = UiKit.Find(_app.Overlay.Root, "ui.resultLose"); Assert.IsNotNull(lose, "사망 팝업 조각(Play_Result_Lose)");
            Assert.IsFalse(UiKit.HasPattern(lose), "사망 팝업에도 흐르는 무늬가 없다(T110 ⓑ · 같은 «결과 팝업»)");
            var ldim = lose.Find("Dimmed"); Assert.IsNotNull(ldim, "어둠 조각");
            var reward = UiKit.Find(lose, "Reward"); Assert.IsNotNull(reward, "사망 보상 칸");
            Assert.IsTrue(UiKit.HasLight(reward), "사망 보상(골드) 그림 뒤 빛살(T72 ②)");
            var icon = reward.Find("Icon"); Assert.IsNotNull(icon, "보상 아이콘");
            Assert.Less(reward.Find(UiKit.LightMaskName).GetSiblingIndex(), icon.GetSiblingIndex(), "빛살은 아이콘 «뒤»(형제 순서 앞)");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓐ 레벨업 3택(04)도 어둠 위 무늬를 받는다
            var offer = Perks.Offer(_app.Data, G.Taken, false, new Mulberry32(7u));
            if (offer.Count > 0)
            {
                G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
                _app.Overlay.LevelUp(G, _ => { }); yield return Frames(2);
                var perk = UiKit.Find(_app.Overlay.Root, "ui.perkSelect"); Assert.IsNotNull(perk, "레벨업 3택 조각");
                Assert.IsTrue(UiKit.HasPattern(perk), "레벨업 3택(04) 배경에도 패턴(T72 ①)");
                _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
            }

            // ⓒ 공통 팝업 상자 = 패턴 «위» 그라데이션(한 곳에서 팝업 전부가 받는다)
            _app.Overlay.PerkBook(G, null); yield return Frames(2);
            var box = UiKit.Find(_app.Overlay.Root, "ui.popup.blue"); Assert.IsNotNull(box, "보유 특전 = 공통 팝업 상자");
            Assert.IsTrue(UiKit.HasGradient(box), "공통 팝업 상자에 그라데이션(T72 ③ · UiKit.Popup 한 곳)");
            var pat = box.Find(UiKit.PatternName); var top = box.Find(UiKit.GradientTopName); var bottom = box.Find(UiKit.GradientBottomName);
            Assert.IsNotNull(pat, "상자 안 패턴"); Assert.IsNotNull(top, "GradientTop"); Assert.IsNotNull(bottom, "GradientBottom");
            Assert.Less(pat.GetSiblingIndex(), top.GetSiblingIndex(), "그라데이션은 패턴 «위»(질감 층 순서 · 결정 171)");
            Assert.Less(top.GetSiblingIndex(), bottom.GetSiblingIndex(), "위 밝음 → 아래 어둠 순서");
            var border = box.Find(UiKit.BorderName); if (border != null) Assert.Less(bottom.GetSiblingIndex(), border.GetSiblingIndex(), "그라데이션은 테두리 아래");
            var ribbon = UiKit.Find(box, "ui.title.sky"); if (ribbon != null) Assert.Less(bottom.GetSiblingIndex(), ribbon.GetSiblingIndex(), "그라데이션은 리본 아래(제목이 안 가려진다)");
            Assert.IsFalse(top.GetComponent<Image>().raycastTarget, "그라데이션은 클릭을 안 먹는다(배경 탭으로 닫기 그대로)");
            var brt = (RectTransform)box; var trt = (RectTransform)top;
            Assert.AreEqual(brt.rect.width - 2f * UiKit.PopupPatternInset, trt.rect.width, 1f, "둥근 모서리 안쪽으로 " + UiKit.PopupPatternInset + "px 들여 덧댄다");
            _app.Overlay.Close(); yield return Frames(2);
            Time.timeScale = 1f;

            _log.AssertNoRed("T72 화면 적용(승리·사망·레벨업 · 공통 팝업 그라데이션)");
            yield return Shutdown();
        }

        /// <summary>
        /// T72 2단계 6차 — 남은 두 화면(<b>로비 01</b> · <b>특권 11</b>):
        /// ⓐ 로비 배경에 무늬(주인 원문 «로비에는 배경 부분에 이 패턴이 있는데 오른쪽 상단으로 천천히 올라가고» · 초록 바탕이라 레퍼런스 01 처럼 <b>어두운</b> Ink 무늬) + ③ 배경 그라데이션(위 밝음 → 아래 어둠)
        /// ⓑ 두 층은 <b>배경 조각 바로 위</b> 형제라 상단 재화 바·사이드 기둥·챕터 카드·START 는 전부 그 «위» = 무늬가 정보 UI 안으로 안 비친다(T72 7항 «패턴은 배경 층에만»)
        /// ⓒ 특권 페이지(11)는 풀스크린 무늬(어두운 바탕 → 흰 무늬) · 카드 4장은 카드 안 무늬 + 그라데이션(둥근 모서리 안쪽 4px) · 제목 띠 4 그라데이션
        /// ⓓ 보상 칸(다이아) 4 = 조각 안 «Item» 뒤 빛살 · 긴 카드 3의 그림 뒤 빛살은 카드 안 질감층 <b>위</b> · 시계방향(시간 정지 중에도). 빨간 줄 0.
        /// </summary>
        [UnityTest]
        public IEnumerator LobbyAndPrivilegePageCarryTheTexture()
        {
            yield return Boot();

            // ⓐⓑ 로비(01)
            _app.ShowScreen("lobby"); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var bgT = UiKit.Find(_app.Current.Root, "Background"); Assert.IsNotNull(bgT, "로비 프리팹의 배경 조각");
            var lobby = (RectTransform)bgT.parent;
            Assert.IsTrue(UiKit.HasPattern(lobby), "로비 배경에 무늬(T72 ①)");
            var pat = lobby.Find(UiKit.PatternName);
            Assert.AreEqual(bgT.GetSiblingIndex() + 1, pat.GetSiblingIndex(), "무늬는 배경 조각 «바로 위»");
            var praw = pat.GetComponent<RawImage>();
            Assert.Less(praw.color.r, 0.5f, "초록 바탕이라 어두운 무늬(PatternTintLight = Ink · 레퍼런스 01)");
            // T94 ⓐ — 로비만 18/255(주인이 두 번 «로비에 패턴이 없다» 고 했다 · 공용 3/255 는 이 어두운 초록 바탕에서 안 보인다) · 다른 화면은 3/255 그대로
            Assert.AreEqual(UiKit.PatternAlphaLobby, praw.color.a, 0.001f, "로비 무늬 알파 = 18/255(T94 ⓐ)");
            Assert.Greater(UiKit.PatternAlphaLobby, UiKit.PatternAlpha, "로비 무늬는 공용보다 진하다");
            Assert.IsFalse(praw.raycastTarget, "무늬는 클릭을 안 먹는다(카드·버튼 그대로)");
            Assert.IsTrue(UiKit.HasGradient(lobby), "로비 배경 그라데이션(T72 ③ 3항 «화면 배경»)");
            var gtop = lobby.Find(UiKit.GradientTopName); var gbot = lobby.Find(UiKit.GradientBottomName);
            Assert.IsNotNull(gtop, "GradientTop"); Assert.IsNotNull(gbot, "GradientBottom");
            Assert.Less(pat.GetSiblingIndex(), gtop.GetSiblingIndex(), "그라데이션은 무늬 «위»(질감 층 순서 · 결정 171)");
            Assert.Less(gtop.GetSiblingIndex(), gbot.GetSiblingIndex(), "위 밝음 → 아래 어둠 순서");
            foreach (var n in new[] { "TopBar", "SubRow", "ChapterCard", "Start" })   // T96-menu 로 사이드 기둥 둘은 없다
            {
                var t = lobby.Find(n); Assert.IsNotNull(t, "로비 " + n);
                Assert.Greater(t.GetSiblingIndex(), gbot.GetSiblingIndex(), n + " 은 질감 층보다 위 = 무늬 침범 0(T72 7항)");
            }

            // ⓒⓓ 특권 페이지(11)
            _app.ShowScreen("privilege"); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var pv = _app.Current.Root;
            Assert.IsTrue(UiKit.HasPattern(pv), "특권 페이지(11) 배경에 무늬(주인 «특별 상품들도 마찬가지»)");
            var ppat = pv.Find(UiKit.PatternName);
            Assert.AreEqual(0, ppat.GetSiblingIndex(), "바탕은 Root 자신의 Image → 무늬는 형제 0(상단 바·카드·바닥 바 아래)");
            Assert.AreEqual(1f, ppat.GetComponent<RawImage>().color.r, 0.001f, "어두운 바탕이라 흰 무늬(PatternTintDark · 레퍼런스 11)");
            var content = UiKit.Find(pv, "Scroll/Content"); Assert.IsNotNull(content, "특권 스크롤 Content");
            int cards = 0, heads = 0, cellLights = 0, picLights = 0; RectTransform lastPicLight = null;
            for (int i = 0; i < content.childCount; i++)
            {
                var c = content.GetChild(i);
                if (c.name.StartsWith("Card:"))
                {
                    cards++;
                    Assert.IsTrue(UiKit.HasPattern(c), c.name + " 카드 안 무늬(T72 ①)");
                    Assert.IsTrue(UiKit.HasGradient(c), c.name + " 카드 그라데이션(T72 ③)");
                    var cpat = (RectTransform)c.Find(UiKit.PatternName); var crt = (RectTransform)c;
                    Assert.AreEqual(crt.rect.width - 8f, cpat.rect.width, 1f, c.name + " 무늬는 둥근 모서리 안쪽으로 4px 들여 깐다");
                    var mask = c.Find(UiKit.LightMaskName);
                    if (mask != null)
                    {
                        picLights++;
                        Assert.IsTrue(UiKit.HasLight(c), c.name + " 그림 뒤 빛살(T72 ②)");
                        // 빛살은 질감층(무늬·그라데이션) «위» 여야 한다(결정 224). 전에는 «형제 맨 뒤» 로 잰다 — 그런데 T69-lobbypopups 가
                        // 그 뒤에 검은 링(«Border»)을 카드 맨 위에 덧대므로(결정 301) 이제 맨 뒤는 링이다 → «질감층보다 위» 로 잰다(결정 303).
                        var cgrad = c.Find(UiKit.GradientBottomName); Assert.IsNotNull(cgrad, c.name + " 카드 아래 어둠(그라데이션)");
                        Assert.Greater(mask.GetSiblingIndex(), cgrad.GetSiblingIndex(), c.name + " 빛살은 카드 안 질감층(무늬·그라데이션) 위");
                        var cring = c.Find(UiKit.BorderName);
                        if (cring != null) Assert.Greater(cring.GetSiblingIndex(), mask.GetSiblingIndex(), c.name + " 검은 링은 빛살보다 위(T69 · 결정 301)");
                        lastPicLight = (RectTransform)mask.Find(UiKit.LightName);
                    }
                }
                else if (c.name.StartsWith("Head:"))
                {
                    heads++;
                    Assert.IsTrue(UiKit.HasGradient(c), c.name + " 제목 띠 그라데이션(레퍼런스 11 의 띠)");
                }
                else if (c.name == "Cell")
                {
                    var item = UiKit.Find(c, "Item"); Assert.IsNotNull(item, "보상 칸 그림(조각의 Item)");
                    var frame = item.parent;
                    Assert.IsTrue(UiKit.HasLight(frame), "보상 칸 그림 뒤 빛살(T72 ② · 작은 조각)");
                    Assert.Less(frame.Find(UiKit.LightMaskName).GetSiblingIndex(), item.GetSiblingIndex(), "빛살은 그림 «뒤»(형제 순서 앞)");
                    cellLights++;
                }
            }
            Assert.AreEqual(4, cards, "특권 카드 4장(레퍼런스 11)");
            Assert.AreEqual(4, heads, "카드 제목 띠 4");
            Assert.AreEqual(4, cellLights, "보상 칸 빛살 4(카드마다 다이아 칸 하나)");
            Assert.AreEqual(3, picLights, "긴 카드 3장의 그림 뒤 빛살(카드 1 은 그림이 없다)");

            // 시간이 멈춰도 흐르고 돈다(unscaled) — 화면 적용도 헬퍼 계약 그대로
            Assert.IsNotNull(lastPicLight, "카드 그림 빛살 조각");
            Time.timeScale = 0f;
            var pvRaw = ppat.GetComponent<RawImage>(); var p0 = pvRaw.uvRect.position; var r0 = lastPicLight.localRotation;
            yield return RealSeconds(0.4f);
            Assert.Less(pvRaw.uvRect.position.x, p0.x, "특권 무늬도 오른쪽 위로 흐른다(시간 정지 중에도)");
            Assert.Less(Vector3.SignedAngle(r0 * Vector3.up, lastPicLight.localRotation * Vector3.up, Vector3.forward), -0.5f, "카드 그림 빛살은 시계방향");
            Time.timeScale = 1f;

            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("T72 화면 적용(로비 01 · 특권 11)");
            yield return Shutdown();
        }

        /// <summary>
        /// T72 2단계 7차(대장간 08 = T72 의 마지막 화면) — ③ 그라데이션이 ⓐ 화면 배경(무늬 «위» · 무대·슬롯·격자·띠 아래) ⓑ 액션바 띠 ⓒ 아래 띠에 깔린다.
        /// ① 무늬는 T69-forge 가 이미 깔았으므로 여기서는 «무늬 위에 그라데이션» 층 순서만 못 박는다(결정 171). 빨간 줄 0.
        /// </summary>
        [UnityTest]
        public IEnumerator ForgeScreenCarriesTheBackgroundGradient()
        {
            yield return Boot();
            _app.ShowScreen("forge"); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var forge = _app.Current.Root;

            Assert.IsTrue(UiKit.HasPattern(forge), "대장간 배경 무늬(T72 ① · T69-forge)");
            Assert.IsTrue(UiKit.HasGradient(forge), "대장간 배경 그라데이션(T72 ③ 3항 «화면 배경»)");
            var pat = forge.Find(UiKit.PatternName); var top = forge.Find(UiKit.GradientTopName); var bottom = forge.Find(UiKit.GradientBottomName);
            Assert.Less(pat.GetSiblingIndex(), top.GetSiblingIndex(), "그라데이션은 무늬 «위»(질감 층 순서 · 결정 171)");
            Assert.Less(top.GetSiblingIndex(), bottom.GetSiblingIndex(), "위 밝음 → 아래 어둠 순서");
            Assert.IsFalse(top.GetComponent<Image>().raycastTarget, "그라데이션은 클릭을 안 먹는다(격자 스크롤·버튼 그대로)");
            foreach (var n in new[] { "Stage", "Result", "ActionBar", "BottomStrip" })
            {
                var t = forge.Find(n); Assert.IsNotNull(t, "대장간 " + n);
                Assert.Greater(t.GetSiblingIndex(), bottom.GetSiblingIndex(), n + " 은 질감 층보다 위(무늬·그라데이션이 내용을 덮지 않는다)");
            }
            foreach (var n in new[] { "ActionBar", "BottomStrip" })
                Assert.IsTrue(UiKit.HasGradient(forge.Find(n)), n + " 띠에 그라데이션(T72 ③ · 레퍼런스 08 의 띠)");

            _log.AssertNoRed("T72 화면 적용(대장간 08 그라데이션)");
            yield return Shutdown();
        }

        /// <summary>«버튼 배경 안» 그라데이션 조각을 찾는다(<see cref="UiKit.ButtonGradient"/> 는 배경 그림의 자식으로 넣는다).</summary>
        static Image BottomGradientUnder(Transform btn)
        {
            foreach (var im in btn.GetComponentsInChildren<Image>(true))
                if (im != null && im.name == UiKit.GradientBottomName) return im;
            return null;
        }

        /// <summary>
        /// T72 2단계 5차 — ③ 그라데이션을 <see cref="UiKit.Button"/> 한 곳에서 <b>모든 프리팹 버튼</b>에(3항 우선순위 1 «주황/파랑/회색 버튼» · 레퍼런스 06 «Forge» · 13 «Summon» 이 전부 위 밝고 아래 어둡다):
        /// ⓐ 세 색 버튼 모두 배경 그림 «안»에 «GradientBottom»(Button_03_White_Gradient · Ink · raycast 끔) 한 장 · 두 번 세워도 안 늘어난다
        /// ⓑ 눌림 표시는 그대로 = <see cref="UiKit.PressTarget"/> 이 질감 조각(무늬·빛살·그라데이션)을 건너뛰어 <b>배경 그림</b>을 고른다(결정 170 이 ③ 을 버튼에 못 넣게 막던 이유를 여기서 푼다)
        /// ⓒ 실제 화면(장비 06 «대장간» 주황 버튼)도 같은 그라데이션을 화면 코드 0 줄로 받는다. 빨간 줄 0.
        /// </summary>
        [UnityTest]
        public IEnumerator PrefabButtonsCarryBottomGradientAndKeepPressTint()
        {
            yield return Boot();
            var host = UiKit.Rect(_app.Frame, "T72BtnHost"); UiKit.Pct(host, 10f, 10f, 80f, 40f);
            string[] keys = { "ui.btnOrange", "ui.btnBlue", "ui.btnGray" };
            for (int i = 0; i < keys.Length; i++)
            {
                var btn = UiKit.Button(host, keys[i], "시험", () => { }, new Layout.R(0, i * 33f, 100, 30f));
                btn.name = "T72Btn:" + keys[i];
                Canvas.ForceUpdateCanvases();
                var grad = BottomGradientUnder(btn);
                Assert.IsNotNull(grad, "[" + keys[i] + "] 버튼에 아래 어둠 그라데이션(T72 ③ · UiKit.Button 한 곳)");
                Assert.IsNotNull(grad.sprite); Assert.IsTrue(grad.sprite.name.IndexOf("Gradient", System.StringComparison.OrdinalIgnoreCase) >= 0, "[" + keys[i] + "] 조각 = Gradient 스프라이트 (" + grad.sprite.name + ")");
                Assert.IsFalse(grad.raycastTarget, "[" + keys[i] + "] 그라데이션은 클릭을 안 먹는다");
                Assert.Greater(grad.color.a, 0.05f, "[" + keys[i] + "] 아래 어둠 알파");
                var bg = grad.transform.parent.GetComponent<Image>();
                Assert.IsNotNull(bg, "[" + keys[i] + "] 그라데이션은 «보이는 배경 그림» 안에 넣는다(배경 위 · 글자 아래)");
                Assert.Greater(bg.color.a, 0.01f, "[" + keys[i] + "] 그 배경은 보이는 그림");

                var b = btn.GetComponent<Button>(); Assert.IsNotNull(b, "[" + keys[i] + "] Button");
                Assert.IsTrue(UiKit.HasVisiblePressTarget(b), "[" + keys[i] + "] 눌림 색이 보이는 그림에 입는다");
                Assert.IsFalse(UiKit.IsTextureLayer(b.targetGraphic.name), "[" + keys[i] + "] 눌림 대상이 질감 조각이면 안 된다(결정 170 해제 조건 · 지금은 " + b.targetGraphic.name + ")");

                UiKit.ButtonGradient(btn);   // 두 번 불러도 조각은 하나
                int n = 0; foreach (var im in btn.GetComponentsInChildren<Image>(true)) if (im.name == UiKit.GradientBottomName) n++;
                Assert.AreEqual(1, n, "[" + keys[i] + "] 다시 불러도 그라데이션은 한 장");
            }

            // ⓒ 실제 화면 — 장비(06) 의 «대장간» 주황 버튼도 화면 코드 0 줄로 같이 받는다
            _app.ShowScreen("gear"); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var gear = _app.GetScreen<GearScreen>(); Assert.IsNotNull(gear, "장비 화면");
            var forge = UiKit.Find(gear.Root, "ForgeBtn");
            if (forge != null) Assert.IsNotNull(BottomGradientUnder(forge), "장비 06 «대장간» 버튼도 아래 어둠 그라데이션(레퍼런스 06 Forge)");

            _log.AssertNoRed("T72 ③ 버튼 그라데이션");
            yield return Shutdown();
        }

        /// <summary>이 칸의 «불투명 바탕» Image — 직계 자식 중 <see cref="TopBar.CellBgName"/>·«Bg»·«BorderBg» 중 먼저 걸리는 것.</summary>
        static Image CellBackdrop(Transform cell)
        {
            if (cell == null) return null;
            for (int i = 0; i < cell.childCount; i++)
            {
                var c = cell.GetChild(i);
                if (c.name != TopBar.CellBgName && c.name != "Bg" && c.name != TopBar.FrameName) continue;
                var im = c.GetComponent<Image>(); if (im != null && im.enabled) return im;
            }
            return null;
        }
        /// <summary>이 사각형 «안 어디에도» 배경 패턴이 없다(자기 자식이든 손자든) — 탑바 판정.</summary>
        static bool AnyPatternInside(Transform host)
        {
            foreach (var raw in host.GetComponentsInChildren<RawImage>(true))
                if (raw != null && raw.name == UiKit.PatternName) return true;
            return false;
        }

        /// <summary>
        /// T72 7항(주인 재차 지시 2026-09-07 «상단에 전투력·골드·다이아 보여주는 부분을 프레임으로 감싸서 패턴들이 움직이는 게 그 부분을 침범하지 않는 것처럼 보이게») —
        /// <see cref="TopBar.Build"/> 한 곳을 쓰는 화면(01 로비 · 06 장비 · 09 상점 · 13 펫 · 20 던전) 전부에서
        /// ⓐ 탑바 줄 전체가 <b>불투명 프레임 띠</b>(<see cref="TopBar.FrameName"/> · 형제 맨 뒤 · 알파 1 · 레퍼런스 색 <see cref="Palette.TopFrame"/> · T106 으로 화면 맨 위까지 이어진다) 로 감싸여 있고
        /// ⓑ 칸마다(아바타 · 전투력 · 골드 pill · 보석 pill) 제 <b>불투명</b> 바탕(알파 ≥ 0.9)이 있으며
        /// ⓒ 패턴 RawImage 는 <b>배경 층에만</b> 있다 = 탑바 «안» 에는 하나도 없고, 화면 배경의 패턴은 탑바보다 <b>뒤</b>(형제 순서 앞)에 깔린다.
        /// 판정 = 이 셋 + 빨간 줄 0(눈 확인은 screens 01·06·09·13·20 PNG 의 탑바 칸 안 «무늬 0»).
        /// </summary>
        [UnityTest]
        public IEnumerator TopBarIsFramedSoThePatternCannotReachIt()
        {
            yield return Boot();
            _app.Save.Gold = 11540; _app.Save.Gem = 443;

            foreach (var screen in new[] { "lobby", "gear", "shop", "pet", "events" })
            {
                if (screen == "events") EventsScreen.Open(_app, EventsScreen.PageDungeon); else _app.ShowScreen(screen);
                yield return Frames(3); Canvas.ForceUpdateCanvases();
                var root = _app.Current.Root; string w = "[" + screen + "] ";
                var top = UiKit.Find(root, "TopBar"); Assert.IsNotNull(top, w + "상단 재화 바(TopBar)");

                // ⓐ 줄 전체를 감싼 불투명 프레임 띠(T106 으로 화면 맨 위까지 이어진다 · 링은 없다 = 이어진 띠에 가로줄이 생기면 안 된다)
                var band = CellBackdrop(top);
                Assert.IsNotNull(band, w + "탑바 프레임 띠(" + TopBar.FrameName + " · T72 7항 ⓐ + T106 ⓑ)");
                Assert.AreEqual(TopBar.FrameName, band.name, w + "띠 이름");
                Assert.AreEqual(0, band.transform.GetSiblingIndex(), w + "띠는 맨 뒤(칸·글자 아래)");
                Assert.GreaterOrEqual(band.color.a, 0.9f, w + "띠는 불투명이라야 패턴이 안 비친다");
                Assert.AreEqual(Palette.TopFrame.r, band.color.r, 0.01f, w + "띠 색 = 레퍼런스 01 상단 띠 실측(T106 ⓒ)");
                Assert.IsFalse(band.raycastTarget, w + "띠는 클릭을 안 먹는다");
                Assert.IsNull(top.Find(UiKit.BorderName), w + "탑바 줄만 두르는 링은 없다(T106 · 이어진 띠 한가운데 가로줄 금지 · 결정 254)");

                // ⓑ 칸마다 제 불투명 바탕
                foreach (var cell in new[] { "Avatar", "PowerCell", "ResourceBar_Coin", "ResourceBar_Gem" })
                {
                    var c = UiKit.Find(top, cell);
                    if (c == null) continue;   // 전투력 없는 화면(showPower:false)
                    var bg = CellBackdrop(c);
                    Assert.IsNotNull(bg, w + "«" + cell + "» 칸 바탕(T72 7항 ⓑ)");
                    Assert.GreaterOrEqual(bg.color.a, 0.9f, w + "«" + cell + "» 칸 바탕은 불투명(GUI Pro 원본 0.749 로 두면 무늬가 비친다)");
                    if (bg.name == TopBar.CellBgName) Assert.IsFalse(bg.raycastTarget, w + "«" + cell + "» 바탕은 클릭을 안 먹는다");
                }

                // ⓒ 패턴은 배경 층에만
                Assert.IsFalse(AnyPatternInside(top), w + "탑바 «안» 에는 패턴이 없다(T72 7항 ⓒ)");
                var pat = root.Find(UiKit.PatternName);
                if (pat != null) Assert.Less(pat.GetSiblingIndex(), top.GetSiblingIndex(), w + "화면 배경 패턴은 탑바보다 뒤(형제 순서 앞)");
            }

            _log.AssertNoRed("T72 7항 탑바 프레임");
            yield return Shutdown();
        }

        /// <summary>사각형의 화면 좌표 사각형(ScreenSpaceOverlay 캔버스는 월드 = 화면 픽셀).</summary>
        static Rect ScreenRect(RectTransform rt)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            float x0 = Mathf.Min(c[0].x, c[2].x), x1 = Mathf.Max(c[0].x, c[2].x);
            float y0 = Mathf.Min(c[0].y, c[2].y), y1 = Mathf.Max(c[0].y, c[2].y);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        /// <summary>
        /// T106(주인 2026-09-07 «모바일로 낼 거니까 SafeArea 만들어서 그 안에서 UI 만들도록 · 카메라 때매 UI 안 보이는 일 없게 · SafeArea 넘어서까지 그 프레임이 위를 다 감싸야 한다») —
        /// ⓐ 화면 UI(<see cref="App.Frame"/>)는 <see cref="App.SafeArea"/> 안에 있고, 노치를 흉내 내 safeArea 를 줄이면 프레임도 그만큼 줄어 <b>탑바 글자·pill 이 안전 영역 안</b>에 들어온다
        /// ⓑ 상단 프레임 띠는 그 반대로 <b>안전 영역을 넘어 화면 맨 위</b>(그리고 좌우 끝)까지 덮는다 — 노치 자리가 그 색으로 채워진다
        /// ⓒ 하단 탭 바 띠도 화면 <b>아래 끝</b>까지(제스처 바 자리) ⓓ safeArea 가 화면 전체면 배치가 <b>예전 그대로</b>(회귀 0). 빨간 줄 0.
        /// </summary>
        [UnityTest]
        public IEnumerator SafeAreaHoldsTheUiAndTheFrameCoversTheNotch()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2); Canvas.ForceUpdateCanvases();

            var canvasRt = (RectTransform)_app.UiCanvas.transform;
            var sa = _app.SafeArea; Assert.IsNotNull(sa, "SafeArea 사각형(T106)");
            Assert.AreEqual(_app.UiCanvas.transform, sa.parent, "SafeArea 는 루트 캔버스 바로 아래");
            Assert.AreEqual(sa, _app.Frame.parent, "화면 UI(Frame)는 SafeArea 안에 만든다");
            var saRoot = sa.GetComponent<SafeAreaRoot>(); Assert.IsNotNull(saRoot, "SafeAreaRoot");

            // ⓓ 먼저 «지금 그대로»(safeArea = 화면 전체) 를 재 둔다 — 노치를 되돌린 뒤 픽셀까지 같아야 한다(회귀 0)
            var frameBefore = ScreenRect(_app.Frame);
            Assert.AreEqual(0f, sa.anchorMin.y, 1e-4f, "노치가 없으면 SafeArea 는 화면 전체(아래)");
            Assert.AreEqual(1f, sa.anchorMax.y, 1e-4f, "노치가 없으면 SafeArea 는 화면 전체(위)");

            // ⓐ 노치를 흉내 낸다 — 위 12% 를 카메라가 먹었다고 치고 다시 그린다
            float notch = Mathf.Round(Screen.height * 0.12f);
            SafeAreaRoot.Override = new Rect(0f, 0f, Screen.width, Screen.height - notch);
            saRoot.Apply(true); yield return Frames(2); Canvas.ForceUpdateCanvases();

            var saRect = ScreenRect(sa);
            Assert.AreEqual(Screen.height - notch, saRect.yMax, 2f, "SafeArea 위 끝이 노치만큼 내려온다");
            var frameNotch = ScreenRect(_app.Frame);
            Assert.LessOrEqual(frameNotch.yMax, saRect.yMax + 1f, "프레임이 안전 영역 안으로 들어온다");

            var top = UiKit.Find(_app.Current.Root, "TopBar"); Assert.IsNotNull(top, "탑바");
            foreach (var cell in new[] { "Avatar", "PowerCell", "ResourceBar_Coin", "ResourceBar_Gem" })
            {
                var c = UiKit.Find(top, cell) as RectTransform; if (c == null) continue;
                Assert.LessOrEqual(ScreenRect(c).yMax, saRect.yMax + 1f, "«" + cell + "» 은 안전 영역 안(노치에 안 가린다)");
            }

            // ⓑ 프레임 띠는 안전 영역을 넘어 화면 맨 위까지
            var band = (RectTransform)top.Find(TopBar.FrameName); Assert.IsNotNull(band, "상단 프레임 띠");
            var bandRect = ScreenRect(band);
            Assert.GreaterOrEqual(bandRect.yMax, Screen.height, "띠는 화면 맨 위까지(노치·레터박스를 덮는다 · 주인 «SafeArea 넘어서까지»)");
            Assert.LessOrEqual(bandRect.xMin, 0f, "띠는 화면 왼쪽 끝까지"); Assert.GreaterOrEqual(bandRect.xMax, Screen.width, "띠는 화면 오른쪽 끝까지");
            Assert.LessOrEqual(bandRect.yMin, ScreenRect((RectTransform)top).yMin + 1f, "띠의 아래 끝 = 탑바 줄 아래(그 아래는 화면이 보인다)");

            // ⓒ 하단 탭 바 띠는 화면 아래 끝까지
            var bottom = UiKit.Find(_app.Current.Root, NavBar.BottomFrameName) as RectTransform;
            Assert.IsNotNull(bottom, "하단 프레임 띠(T106 ⓓ)");
            Assert.LessOrEqual(ScreenRect(bottom).yMin, 0f, "하단 띠는 화면 아래 끝까지(제스처 바 자리)");

            // ⓓ 되돌리면 예전 배치 그대로
            SafeAreaRoot.Override = null;
            saRoot.Apply(true); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var frameAfter = ScreenRect(_app.Frame);
            Assert.AreEqual(frameBefore.xMin, frameAfter.xMin, 0.5f, "safeArea 가 화면 전체면 배치가 예전 그대로(x)");
            Assert.AreEqual(frameBefore.yMin, frameAfter.yMin, 0.5f, "같음(y)");
            Assert.AreEqual(frameBefore.width, frameAfter.width, 0.5f, "같음(폭)");
            Assert.AreEqual(frameBefore.height, frameAfter.height, 0.5f, "같음(높이)");

            // T122 — 하단 띠는 탭 바보다 «뒤»(형제 순서 앞)여야 한다. 로비는 탭 바가 프리팹이 달고 온 자식이라
            // 새로 만든 띠가 형제 맨 뒤(= 맨 위)에 붙어 탭 바를 통째로 덮었다(screens run 218 실측). 두 경로(프리팹·Attach) 다 잰다.
            foreach (var screen in new[] { "lobby", "shop" })
            {
                _app.ShowScreen(screen); yield return Frames(2); Canvas.ForceUpdateCanvases();
                var sr = _app.Current.Root; string w2 = "[" + screen + "] ";
                // 띠가 어느 «부모» 아래 서는지는 화면마다 다르다 — 상점은 화면 루트(NavBar.Attach), 로비는 프리팹 루트(ui.lobby)다
                // (로비는 프리팹을 통째로 세우고 그 안에서 조립하므로 BottomFrame·탭 바가 둘 다 프리팹 루트의 자식이다 · T122 회차 2).
                // 가리느냐 마느냐는 «같은 부모 안에서의 형제 순서» 로 정해지므로, 띠를 깊이 찾아 그 부모를 기준으로 잰다.
                var bandT = UiKit.Find(sr, NavBar.BottomFrameName);
                Assert.IsNotNull(bandT, w2 + "하단 프레임 띠");
                var bar = UiKit.FindAny(sr, "Tab_01_BottomFlushMenu", "ui.tabBar");
                Assert.IsNotNull(bar, w2 + "하단 탭 바");
                var barTop = bar; while (barTop != null && barTop.parent != bandT.parent) barTop = barTop.parent;
                Assert.IsNotNull(barTop, w2 + "띠와 탭 바가 같은 부모 아래에 있다");
                Assert.Less(bandT.GetSiblingIndex(), barTop.GetSiblingIndex(), w2 + "띠는 탭 바 뒤(= 탭 아이콘·라벨을 가리지 않는다 · T122)");
            }

            _log.AssertNoRed("T106 SafeArea · 상단 프레임");
            yield return Shutdown();
        }
    }
}
