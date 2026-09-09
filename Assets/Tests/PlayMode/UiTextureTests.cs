using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T72 «질감 3종» 헬퍼 계약(주인 2026-09-06 «Pattern_01_256 거의 모든 UI 에 · 아이콘 뒤 Effect_Light 천천히 회전 · 그라데이션 색감») — 실제 씬(App) 위에 시험 칸을 세우고
    /// ① <see cref="UiKit.PatternBg"/>: «Pattern» RawImage · 텍스처 Repeat(.meta) · uvRect 크기 = 사각형 ÷ 256 · 시간이 멈춘 중(timeScale 0)에도 uvRect.position 이 <b>줄어</b>(= 그림이 오른쪽 위로) 흐른다 · raycast 끔 · 알파 3/255 · 한 타일 10~15초(주인 확정 2026-09-07)
    /// ② <see cref="UiKit.LightBehind"/>: 빛 담개 안 «Glow»(정적 · T155 ⓓ) + «Light» · 아이콘 <b>뒤</b>(형제 순서 앞) · 한 변 = max(아이콘 긴 변 × 1.9, 칸 긴 변 × 1.35) · 아이콘 중심 · <b>시계방향</b> 회전(unscaled) · <see cref="UiKit.SetLightSpinning"/> 으로 멈춤 · 담개는 <b>마스크</b>다(T189 — 빛·서클이 칸 안에서 끝난다 · T172 는 주인 13:2X 로 취소)
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
        /// <summary>나무 «전체» 에서 살아 있는 배경 무늬(«Pattern» RawImage) 수 — T140 게이트(특전 화면 둘은 0).</summary>
        static int CountPatterns(Transform t)
        {
            if (t == null) return 0;
            int n = 0;
            foreach (var raw in t.GetComponentsInChildren<RawImage>(true))
                if (raw != null && raw.name == UiKit.PatternName && raw.gameObject.activeInHierarchy) n++;
            return n;
        }

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
            // T189(주인 13:2X «그냥 밖으로 하는 거 말고 다 안으로 해라 걍») — T172 를 통째로 되돌렸다: 담개는 다시 마스크다.
            // ⚠ 주인 말이 세 번 바뀐 자리다(«밖에» → «상점은 바꾸기 전이 맞았음» → «다 안으로» 최종) — 되살리지 말 것.
            Assert.IsNotNull(mask.GetComponent<RectMask2D>(), "빛 담개 = RectMask2D(빛·글로우 서클이 칸 밖으로 안 나간다 · T189)");
            Assert.AreEqual(host, mask.parent, "LightMask 는 host 의 자식");
            Assert.Less(mask.GetSiblingIndex(), icon.transform.GetSiblingIndex(), "빛살은 아이콘 «뒤»(형제 순서 앞)");
            Assert.Greater(mask.GetSiblingIndex(), raw.transform.GetSiblingIndex(), "빛살은 패턴 위");
            Assert.IsFalse(light.raycastTarget, "Light raycast 끔"); Assert.IsNotNull(light.sprite); Assert.IsTrue(light.sprite.name.StartsWith("Effect_Light"), "스프라이트 = Effect_Light_01_512 (" + light.sprite.name + ")");
            Assert.AreEqual(68f / 255f, light.color.a, 0.01f, "빛살 알파 = 주인 확정 «255 중 68»(2026-09-07)");
            var lrt = light.rectTransform; var irt = icon.rectTransform;
            // T189 — 한 변 = 아이콘 긴 변 × LightScale(T172 가 넣었던 «칸 × 1.35» 하한은 없앴다)
            float side = Mathf.Max(irt.rect.width, irt.rect.height) * UiKit.LightScale;
            Assert.AreEqual(side, lrt.rect.width, 1f, "빛살 한 변 = 아이콘 긴 변 × " + UiKit.LightScale); Assert.AreEqual(side, lrt.rect.height, 1f, "정사각");
            // 그리고 «칸 밖으로 안 나간다» 를 눈에 보이는 성질로 못 박는다(T189 3항) — 마스크가 또 떨어지면 이 줄이 잡는다.
            var mrt = (RectTransform)mask; var hostCorners = new Vector3[4]; var maskCorners = new Vector3[4];
            host.GetWorldCorners(hostCorners); mrt.GetWorldCorners(maskCorners);
            Assert.LessOrEqual(hostCorners[0].x - 0.5f, maskCorners[0].x, "빛 담개 왼쪽이 칸 안(마스크가 자르는 사각형 = 칸)");
            Assert.LessOrEqual(maskCorners[2].x, hostCorners[2].x + 0.5f, "빛 담개 오른쪽이 칸 안");
            Assert.LessOrEqual(hostCorners[0].y - 0.5f, maskCorners[0].y, "빛 담개 아래가 칸 안"); Assert.LessOrEqual(maskCorners[2].y, hostCorners[2].y + 0.5f, "빛 담개 위가 칸 안");
            Assert.IsTrue(mask.GetComponent<RectMask2D>().enabled, "그 마스크가 켜져 있다(꺼 두면 자르지 않는다)");
            var lc = host.InverseTransformPoint(lrt.TransformPoint(lrt.rect.center)); var ic = host.InverseTransformPoint(irt.TransformPoint(irt.rect.center));
            Assert.AreEqual(ic.x, lc.x, 1f, "빛살 중심 x = 아이콘 중심"); Assert.AreEqual(ic.y, lc.y, 1f, "빛살 중심 y = 아이콘 중심");
            Assert.IsTrue(UiKit.HasLight(host), "HasLight");
            // T155 ⓓ(주인 07:3X «모든 이펙트 라이트 들어간 곳에 글로우 서클도 같이») — 같은 사각형·같은 중심 · 빛살 «아래» 겹 · 도는 트윈은 안 늘린다
            Assert.IsTrue(UiKit.HasGlow(host), "빛살 아래 글로우 서클(T155 ⓓ)");
            var grt = (RectTransform)mask.Find(UiKit.GlowName);
            Assert.AreEqual(lrt.rect.width, grt.rect.width, 1f, "글로우 서클 = 빛살과 같은 사각형"); Assert.AreEqual(lrt.rect.height, grt.rect.height, 1f, "정사각");
            Assert.AreEqual(lrt.anchoredPosition.x, grt.anchoredPosition.x, 1f, "중심 x 가 같다"); Assert.AreEqual(lrt.anchoredPosition.y, grt.anchoredPosition.y, 1f, "중심 y 가 같다");
            Assert.Less(grt.GetSiblingIndex(), lrt.GetSiblingIndex(), "글로우 서클은 빛살 «아래»");
            Assert.IsFalse(UiKit.IsTweening(grt), "글로우 서클은 돌지 않는다 — 원이라 티가 안 나고 도는 트윈만 늘어 fps 를 깎는다(T155 4항 «성능»)");
            var gimg = grt.GetComponent<Image>();
            Assert.AreEqual(UiKit.GlowAlpha, gimg.color.a, 0.01f, "글로우 서클 알파 = " + UiKit.GlowAlpha + "(빛살보다 옅다 · 겹치면 더 밝아지므로)");
            Assert.IsFalse(gimg.raycastTarget, "Glow raycast 끔");

            // T174(주인 2026-09-07 10:4X «모든 이펙트 라이트 있는 곳에 파티클 이펙트도 넣어 줘 · 빛 알갱이 먼지가 천천히 퍼지는 느낌으로»)
            // — 진짜 파티클은 UI 위에 못 뜬다(캔버스가 ScreenSpaceOverlay · T144·T181 과 같은 벽)라 작은 Image 몇 장을 트윈으로 흘린다.
            Assert.IsTrue(UiKit.HasDust(host), "빛살 자리에 빛 알갱이 묶음(T174)");
            var drt = (RectTransform)mask.Find(UiKit.DustName);
            Assert.AreEqual(UiKit.DustCount, drt.childCount, "알갱이 수 = UiKit.DustCount(fps 를 재고 줄일 자리가 그 상수 한 곳이다)");
            Assert.Greater(drt.GetSiblingIndex(), lrt.GetSiblingIndex(), "알갱이는 빛살 «위»");
            Assert.AreEqual(lrt.anchoredPosition.x, drt.anchoredPosition.x, 1f, "알갱이 묶음 중심 x = 빛살");
            Assert.AreEqual(lrt.anchoredPosition.y, drt.anchoredPosition.y, 1f, "알갱이 묶음 중심 y = 빛살");
            // **칸마다 트윈 하나**(지시서 4항 ⓐ) — 알갱이마다 따로 걸면 칸당 도는 트윈이 4배로 늘어 fps 가 떨어진다(T129).
            Assert.AreEqual(1, UiKit.TweenCountOn(drt), "알갱이는 칸마다 시퀀스 «하나» 가 전부 움직인다(T174 4항 ⓐ · 알갱이마다 걸면 칸당 4배가 된다)");
            foreach (RectTransform g in drt)
            {
                var gimg2 = g.GetComponent<Image>();
                Assert.IsNotNull(gimg2, "알갱이 그림 " + g.name);
                Assert.IsFalse(gimg2.raycastTarget, "알갱이 raycast 끔 — 글자·버튼을 막으면 안 된다");
                Assert.AreEqual(g.rect.width, g.rect.height, 0.5f, "알갱이는 정사각(preserveAspect 와 짝)");
                Assert.Less(g.rect.width, lrt.rect.width, "알갱이는 빛살보다 작다(먼지지 두 번째 빛살이 아니다)");
            }
            // «보이는 칸만»(T72 4항) 규약에 알갱이도 같이 탄다 — 그 «멈춘다/다시 돈다» 는 아래 timeScale 0 실측 칸에서 잰다.
            // ⚠ 여기서 IsTweening 으로 재면 안 된다 — 멈춘(Pause) 트윈도 «활성» 이라 늘 참이다(이 파일 아래 «CI #145 에서 확인» 줄과 같은 함정 · 결정 508).
            // «시퀀스가 이 묶음을 겨냥한다»(= 재우고 깨울 손잡이가 걸려 있다)는 바로 위 TweenCountOn 줄이 이미 못 박았다.

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
            var d0 = (RectTransform)drt.GetChild(0); float dm0 = d0.anchoredPosition.magnitude;   // T174 알갱이 0번(시퀀스 0초에 꽂혀 있어 이 순간 반드시 흐르는 중이다)
            // T174 회차 3 — 칸이 생긴 «첫 프레임부터» 이미 퍼져 있어야 한다(결정 513). 담개가 아이콘 «뒤» 겹이라
            // 가운데에 겹쳐 서면 그림에 완전히 가려 안 보이고, `screens` PNG 는 화면을 연 지 2~5프레임 만에 찍는다(UiShotsTests.Shot).
            // 안 감았을 때 이 시점의 값은 퍼짐 거리의 1~2% 뿐이라, 절반을 넘으면 감긴 것이 확실하다.
            Assert.Greater(dm0, lrt.rect.width * UiKit.DustDriftMul * 0.5f,
                "알갱이는 첫 프레임부터 이미 퍼져 있다(T174 4항 · 시퀀스를 앞으로 감는다 — 가운데에 겹쳐 서면 아이콘 뒤라 안 보인다)");
            yield return RealSeconds(0.4f);
            var p1 = raw.uvRect.position;
            Assert.Less(p1.x, p0.x, "패턴 uvRect.x 가 줄어야 무늬가 오른쪽으로 간다(결정 157)"); Assert.Less(p1.y, p0.y, "uvRect.y 가 줄어야 무늬가 위로 간다");
            Assert.AreEqual(p0.x - p1.x, p0.y - p1.y, 0.002f, "대각선(오른쪽 위 45°)");
            float ang = Vector3.SignedAngle(r0 * Vector3.up, lrt.localRotation * Vector3.up, Vector3.forward);
            Assert.Less(ang, -1f, "빛살은 시계방향(z 각이 줄어든다) · 0.4s 에 " + ang.ToString("0.0") + "°");
            Assert.Greater(ang, -30f, "천천히(한 바퀴 " + UiKit.LightPeriod + "s)");
            // 알갱이(T174)도 같은 unscaled 결로 흐른다 — 여기서 «흐른다 → 멈춘다 → 다시 흐른다» 를 빛살과 한 자리에서 잰다.
            // 재는 것은 «트윈이 있나» 가 아니라 «자리가 변하나» 다(아래 상점 칸 줄과 같은 까닭 · 결정 508).
            UiKit.SetLightSpinning(host, false);
            var r2 = lrt.localRotation; float dm1 = d0.anchoredPosition.magnitude; yield return RealSeconds(0.25f);
            Assert.Greater(dm1, dm0 + 0.5f, "멈추기 전 0.4s 동안 알갱이가 가운데서 바깥으로 흘렀다(" + dm0.ToString("0.0") + " → " + dm1.ToString("0.0") + "px)");
            Assert.AreEqual(0f, Vector3.SignedAngle(r2 * Vector3.up, lrt.localRotation * Vector3.up, Vector3.forward), 0.01f, "SetLightSpinning(false) 면 멈춘다");
            Assert.AreEqual(dm1, d0.anchoredPosition.magnitude, 0.01f, "스크롤 밖 칸에서는 알갱이도 멈춘다(T174 4항 ⓑ)");
            UiKit.SetLightSpinning(host, true);
            yield return RealSeconds(0.2f);
            Assert.Less(Vector3.SignedAngle(r2 * Vector3.up, lrt.localRotation * Vector3.up, Vector3.forward), -0.5f, "다시 켜면 돈다");
            Assert.Greater(d0.anchoredPosition.magnitude, dm1 + 0.2f, "돌아오면 알갱이도 다시 흐른다");
            Time.timeScale = 1f;

            // 두 번 불러도 조각이 늘지 않는다(갱신만)
            UiKit.PatternBg(host, UiKit.PatternTintDark); UiKit.LightBehind(host, icon.rectTransform); UiKit.Gradient(host);
            Assert.AreEqual(1, CountNamed(host, UiKit.PatternName), "Pattern 1개"); Assert.AreEqual(1, CountNamed(host, UiKit.LightMaskName), "빛 담개 1개");
            Assert.AreEqual(1, CountNamed(host, UiKit.GradientTopName), "GradientTop 1개"); Assert.AreEqual(1, CountNamed(host, UiKit.GradientBottomName), "GradientBottom 1개");
            Assert.AreEqual(1, CountNamed(mask, UiKit.LightName), "Light 1개");
            Assert.AreEqual(1, CountNamed(mask, UiKit.GlowName), "Glow 1개(두 번 불러도 글로우 서클이 늘지 않는다 · T155 ⓓ)");
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

            // T203 — 펫 세부 팝업(14)에는 «무늬도 빛살도 없다»(주인 2026-09-07 «라이트 이펙트가 장비 슬롯 내부에 있던데 그거 없애기» · «패턴 없애기»).
            // 종전에는 이 자리가 «공통 팝업 상자 안 패턴 + 아이콘 뒤 빛살» 의 본보기였다 — 그 계약은 잃지 않는다:
            //  · 무늬 «있음»·층 순서 = 아래 설정 팝업 줄(T140 이 특전 둘을 뺄 때 옮겨 둔 그 자리)
            //  · 빛살 «있음»·순서 = 아래 상점 상자 줄
            //  · 둘의 unscaled 흐름·회전 = 이 파일 맨 앞 «시험 칸» 줄(timeScale 0 실측)
            // 그래서 여기서는 «없다» 만 못 박는다.
            _app.GetScreen<PetScreen>().OpenDetail(0); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var box = UiKit.Find(_app.Overlay.Root, "ui.popup");
            Assert.IsNotNull(box, "펫 세부 = 공통 팝업 상자(ui.popup)");
            Assert.IsFalse(UiKit.HasPattern(box), "펫 세부(14) 상자 안에 흐르는 무늬가 없다(T203 ⓑ)");
            Assert.AreEqual(0, CountPatterns(box), "14 팝업 나무 어디에도 «Pattern» RawImage 가 없다(T203 ⓑ)");

            var cell = UiKit.Find(box, "PetDetailCell"); Assert.IsNotNull(cell, "펫 칸(세부)");
            var item = UiKit.Find(cell, "Item"); Assert.IsNotNull(item, "펫 아이콘(조각의 Item)");
            Assert.IsFalse(UiKit.HasLight(item.parent), "펫 세부 아이콘 뒤에 빛살이 없다(T203 ⓐ)");
            Assert.IsNull(item.parent.Find(UiKit.LightMaskName), "빛 담개(LightMask)까지 남지 않는다 — 끄는 것이 아니라 안 세운다");

            _app.Overlay.Close(); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "닫힘");
            // 종전에는 여기서 «팝업이 닫히면 빛살 트윈도 없다» 를 쟀는데 이제 빛살 자체가 없다(T203 ⓐ).
            // 그 «SetLink 로 트윈이 같이 죽는다» 계약은 이 파일 맨 앞 «시험 칸» 줄(칸 파괴 뒤 트윈 0)이 그대로 지킨다.
            Assert.AreEqual(0, CountPatterns(_app.Overlay.Root), "팝업을 닫으면 어둠 층에 «Pattern» 이 하나도 안 남는다");
            _log.AssertNoRed("T72 화면 적용(팝업 · 펫)");
            yield return Shutdown();
        }

        /// <summary>
        /// T72 2단계 2차(상점 09·10) — 주인 원문 «상점 아이템, 특별 상품 이런 것들 아이콘 뒤에 Effect_Light_01_512 이런 거 있어야 하고 천천히 오른쪽으로 회전하는 느낌» +
        /// «Pattern_01_256 이거들이 모든 UI 에 다 있어야 함». ⓐ 상점 풀스크린 배경에 패턴(어두운 회색 바탕 → 흰 무늬 · 배경 조각 바로 위) ⓑ 빛살은 <b>대형 상자 배너 하나뿐</b>이고
        /// 작은 상자 카드 2·다이아 6·골드 3 에는 <b>없다</b>(T308 · 주인 2026-09-09 09:1X — 옛 계약 «전부 있다» 를 갈아 끼웠다 · T184) ⓒ 그 하나는 <b>시계방향</b>
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
                if (c.name.StartsWith("Box:"))
                {
                    // T308(주인 09:1X «희귀 상자랑 전설 상자는 카드에서 라이트 이펙트 빼기») — 빛살이 남는 것은 **맨 위 대형 배너 하나뿐**이다.
                    // 신화 큰 카드는 주인이 말을 안 해 그대로 두었으므로, 여기서도 «첫 상자만 켜짐» 으로 못 박는다(아래 ⓒ 의 회전 검사가 그 하나를 쓴다).
                    if (firstBox == null) { firstBox = c; Assert.IsTrue(UiKit.HasLight(c), c.name + " 대형 배너는 빛살이 남는다(주인이 말을 안 한 자리)"); }
                    else Assert.IsFalse(UiKit.HasLight(c), c.name + " 작은 상자 카드에는 빛살이 없다(T308 · 주인 지시)");
                    boxes++;
                }
                else if (c.name.StartsWith("GemPack:") || c.name.StartsWith("GoldPack:"))
                {
                    var cell = c.childCount > 0 ? c.GetChild(0) : null;   // 조각(ListItem_ShopItem)
                    Assert.IsNotNull(cell, c.name + " 안의 상품 조각");
                    // T308(주인 «다이아 골드 카드도 라이트 이펙트 빼기») — 담개째로 안 선다.
                    // ⚠ 그래서 «빛살이 아이콘 뒤인가» 를 여기서 더 재면 안 된다 — `cell.Find(LightMaskName)` 이 null 이라 그 줄이 터진다.
                    Assert.IsFalse(UiKit.HasLight(cell), c.name + " 상품 아이콘 뒤 빛살은 없다(T308 · 주인 지시)");
                    Assert.IsFalse(UiKit.HasLightMask(cell), c.name + " 빛 담개도 안 선다(조각을 아예 안 세운다)");
                    Assert.IsNotNull(cell.Find("Icon"), c.name + " 아이콘");
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

            // T181 ⓑ — 빛은 «겹칠수록 밝아지는» 加算이라야 «빛나는 느낌» 이 난다(주인 «glow 빛나는 느낌이 잘 안 든다»).
            // ⓐ 의 Bloom 은 UI 에 안 먹으므로(캔버스가 ScreenSpaceOverlay) UI 쪽은 이 길뿐이다.
            // 재는 것은 «머티리얼이 붙었나» 가 아니라 **섞는 방식**이다 — 이름만 보면 기본 알파 블렌딩으로 되돌아가도 초록이다.
            {
                var lmat = UiKit.LightMaterial();
                if (lmat != null)   // 셰이더가 없는 환경(스텁)에서는 이 칸을 건너뛴다 — 그 경우 그림은 종전 그대로다
                {
                    Assert.AreEqual(UiKit.LightMatName, lmat.name, "빛 머티리얼 이름(게이트가 이것으로 찾는다)");
                    Assert.AreEqual((float)UnityEngine.Rendering.BlendMode.One, lmat.GetFloat("_MyDstMode"), 1e-3f,
                        "加算 = 도착 blend 가 One 이어야 겹칠수록 밝아진다(OneMinusSrcAlpha 로 돌아가면 «흰 판» 이 된다)");
                    Assert.AreEqual((float)UnityEngine.Rendering.BlendMode.SrcAlpha, lmat.GetFloat("_MySrcMode"), 1e-3f,
                        "출발 blend 는 SrcAlpha 그대로 — 주인이 정한 알파(68/255)가 세기를 정한다(One One 이면 알파가 무시돼 하얗게 뜬다)");
                    var bigImg = bigLight.GetComponent<Image>();
                    Assert.AreSame(lmat, bigImg.material, "빛살이 그 한 장을 쓴다");
                    var glow = firstBox.Find(UiKit.LightMaskName + "/" + UiKit.GlowName);
                    if (glow != null) Assert.AreSame(lmat, glow.GetComponent<Image>().material, "글로우 서클도 같은 한 장(둘이 겹친 가운데가 더 밝다)");
                    Assert.AreSame(lmat, UiKit.LightMaterial(), "머티리얼은 «한 장을 나눠 쓴다» — 칸마다 인스턴스를 만들면 드로콜이 는다");
                }
            }

            // ⓓ 4항 «보이는 칸만» — 스크롤 밖 칸의 빛살은 멈춘다.
            // ⚠ T308 로 **재는 대상이 바뀌었다**: 여태는 맨 아래 골드 칸의 빛살로 쟀는데 그 빛을 주인 지시로 뺐다(위 ⓑ).
            //   자를 지우지 않고 **남은 하나(대형 배너)로 옮긴다** — 규칙(T72 4항)은 그대로 살아 있고, 그것을 재는 유일한 빛이 이제 이 하나다.
            //   방향만 뒤집혔다: 배너는 맨 «위» 에 있으므로 맨 아래로 내리면 화면 밖이 된다.
            // 멈춤은 «트윈이 없다» 가 아니라 «각이 안 변한다» 로 잰다 — DOTween.IsTweening 은 멈춘(Pause) 트윈도 «활성» 으로 본다(CI #145 에서 확인)
            shopScreen.ScrollTo(0f); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var stop0 = bigLight.localRotation; yield return RealSeconds(0.4f);
            Assert.AreEqual(0f, Quaternion.Angle(stop0, bigLight.localRotation), 0.01f, "스크롤 밖 칸(맨 위 대형 배너)은 빛살이 멈춘다(T72 4항)");
            shopScreen.ScrollTo(1f); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var g1 = bigLight.localRotation; yield return RealSeconds(0.4f);
            Assert.Less(Vector3.SignedAngle(g1 * Vector3.up, bigLight.localRotation * Vector3.up, Vector3.forward), -0.5f, "다시 맨 위로 올리면 그 빛살이 또 돈다");

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
                    // T190(주인 2026-09-07 13:3X «아이템 슬롯 같은 거에는 빛 효과 없게») — 보상 칸의 빛살은 **빠졌다**.
                    // 담개까지 재는 까닭: 빛살만 끄고 글로우 서클(T155 ⓓ)·알갱이(T174)가 남으면 눈에는 그대로 «빛 효과» 다.
                    Assert.IsFalse(UiKit.HasLight(cell), name + "/" + cell.name + ": 보상 칸에 빛살 없음(T190)");
                    Assert.IsFalse(UiKit.HasLightMask(cell), name + "/" + cell.name + ": 보상 칸에 빛 담개도 없음(T190)");
                    var icon = cell.Find("Icon"); Assert.IsNotNull(icon, cell.name + " 아이콘");
                    cells++;
                }
            }
            // T251 — 여기도 수를 안 박는다(위 EventsScreenTests 와 같은 까닭 · CI run 645 에서 «6 / was 3» 으로 빨갰다).
            //   두 카드가 표에서 받는 종류 수의 합이 곧 그려져야 할 칸 수다.
            // T291 — 층이 생겼으므로 규칙에 «몇 층» 을 같이 묻는다(화면이 그리는 층 = 도전 층). 안 물으면 표의 한 벌을 기대해 그리는 쪽이 빨개진다.
            int wantCells = EventsScreen.CardRewardKinds(_app.Data.Dungeon, "hell", new string[0], DungeonSweep.Challenge(_app.Save, _app.Data.Dungeon, "hell")).Length
                          + EventsScreen.CardRewardKinds(_app.Data.Dungeon, "expedition", new string[0], DungeonSweep.Challenge(_app.Save, _app.Data.Dungeon, "expedition")).Length;
            Assert.Greater(wantCells, 0, "표가 보상 종류를 준다(이게 0 이면 아래 단언이 헛돈다)");
            Assert.AreEqual(wantCells, cells, "던전 보상 아이콘 = 두 카드가 표에서 받는 종류 수의 합");

            // ⓓ 그라데이션 = 카드 제목 띠(위 밝음 · 아래 어둠)
            Assert.IsTrue(UiKit.HasGradient(UiKit.Find(hell, "Head")), "던전 카드 제목 띠에 그라데이션(T72 ③)");

            // 패턴은 오른쪽 위로 흐르고 빛살은 시계방향
            // T190 — «보상 칸 빛살이 시계방향» 은 잴 것이 없어졌다(빛 자체가 없다). 패턴 흐름만 남는다.
            var p0 = praw.uvRect.position;
            yield return RealSeconds(0.4f);
            Assert.Less(praw.uvRect.position.x, p0.x, "던전 배경 패턴도 오른쪽 위로 흐른다");

            // ⓑ 던전 세부 팝업(21) 보상 칸 4개 + 제목 띠 그라데이션
            var enter = UiKit.Find(hell, "EnterBtn").GetComponent<Button>(); Assert.IsNotNull(enter, "입장 버튼");
            enter.onClick.Invoke(); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var ov = _app.Overlay.Root;
            int rcells = 0;
            for (int i = 0; i < 4; i++)
            {
                var cell = UiKit.Find(ov, "RewardCell:" + i); Assert.IsNotNull(cell, "세부 팝업 보상 칸 " + i);
                Assert.IsFalse(UiKit.HasLight(cell) || UiKit.HasLightMask(cell), "세부 팝업 보상 칸 " + i + " 에 빛 없음(T190)"); rcells++;
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
            int rr = 0, rlit = 0;
            foreach (var t in ov.GetComponentsInChildren<Transform>(false))
                if (t.name == "Reward") { rr++; if (UiKit.HasLight(t) || UiKit.HasLightMask(t)) rlit++; }
            // T247 — 옛 값은 «8»(= 네 줄 × 코인·다이아)이었는데 T237 이 표(`arena.json`)의 구간 열여섯을 붙여 32 가 됐다.
            //   ⚠ 여기서 수를 32 로 바꾸면 **같은 함정을 다시 놓는 것**이다 — 주인이 보상 «값» 을 채우면 줄마다 칸 수가
            //   `rewards.Count` 로 바뀌어(빈 줄만 «코인·다이아» 두 칸) 또 깨진다. 그래서 **그리는 코드와 같은 규칙**으로 센다
            //   (`EventsScreen.OpenRankRewards` 의 그 갈래 그대로). 표가 없으면(로드 실패) 종전 네 줄 껍데기다.
            var rank = _app.Data != null ? _app.Data.ArenaRank : null;
            int want = 0;
            if (rank != null && rank.Tiers.Count > 0) foreach (var tier in rank.Tiers) want += tier.Rewards.Count > 0 ? tier.Rewards.Count : 2;
            else want = 4 * 2;
            Assert.AreEqual(want, rr, "순위 보상 칸 = 표의 구간 줄마다 «보상 수(비면 코인·다이아 둘)» 의 합(T237 표 · T247)");
            Assert.AreEqual(0, rlit, "순위 보상 칸에는 빛이 하나도 없다(T190)");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓑⓔ 상인 페이지(26) 상품 11칸 + T72 4항 «안 보는 페이지는 멈춘다»
            ev.ShowPage(EventsScreen.PageMerchant); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var me = UiKit.Find(root, "Page:merchant"); Assert.IsNotNull(me, "상인 페이지");
            int goods = 0; Transform firstGoods = null;
            for (int i = 0; i < 11; i++)
            {
                var card = UiKit.Find(me, "Goods:" + i); Assert.IsNotNull(card, "상품 카드 " + i);
                var ic = card.Find("IconCell"); Assert.IsNotNull(ic, "상품 " + i + " 아이콘 칸");
                // T190 — 상인 페이지 상품 칸은 조각 `ItemFrame_01` 을 쓰므로 «아이템 칸» 판정에 걸려 빛이 빠진다.
                // 주인이 «남긴다» 고 한 것은 **상점(09·10)의 상품 카드**(조각 `ListItem_ShopItem`)다 — 그쪽은 `ShopScreenCarriesPatternAndItemLights` 가 지킨다.
                Assert.IsFalse(UiKit.HasLight(ic) || UiKit.HasLightMask(ic), "상인 상품 " + i + " 칸에 빛 없음(T190)"); if (firstGoods == null) firstGoods = ic; goods++;
            }
            Assert.AreEqual(11, goods, "상인 상품 11칸 전부(레퍼런스 26)");
            Assert.IsNotNull(firstGoods, "상품 칸 하나는 잡혔다");
            // T190 — «보고 있는 페이지만 돈다»(T72 4항)를 여기서 재던 세 걸음은 잴 것이 없어졌다(상인 칸에 빛이 없다).
            // 그 규약 자체는 상점 화면의 `ShopScreenCarriesPatternAndItemLights`(빛이 남는 자리)가 그대로 지킨다.
            ev.ShowPage(EventsScreen.PageArena); yield return Frames(2);
            ev.ShowPage(EventsScreen.PageMerchant); yield return Frames(2);

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
            // T190 — ⚑ 주인이 **이름을 대고 지목한 자리**다(«클리어했을 때 골드 주는 거 슬롯에 빛 효과 같은 그거»). 빛살도 담개도 없다.
            Assert.IsFalse(UiKit.HasLight(goldCell), "클리어 보상(골드) 칸에 빛살 없음(T190 · 주인 13:3X)");
            Assert.IsFalse(UiKit.HasLightMask(goldCell), "클리어 보상 칸에 빛 담개도 없음(T190)");
            // 팝업 시간 정지 중에도(unscaled) 제목 빛살은 시계방향으로 돈다(T110 ⓒ · 이쪽은 조각 제 연출이라 그대로다)
            yield return RealSeconds(0.4f);
            Assert.Less(Vector3.SignedAngle(tr0 * Vector3.up, tfx.localRotation * Vector3.up, Vector3.forward), -0.5f, "제목 빛살도 시계방향으로 돈다(T110 ⓒ)");
            yield return RealSeconds(Overlay.ConfettiSec + 0.6f);
            Assert.IsTrue(UiKit.Find(win, "SampleEffect_Confetti_R") == null || !UiKit.Find(win, "SampleEffect_Confetti_R").gameObject.activeInHierarchy, "폭죽은 다 터지면 사라진다(T110 ⓓ)");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓐⓑ 사망 팝업 — 같은 두 가지
            _app.Overlay.Dead(G, () => { }); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var lose = UiKit.Find(_app.Overlay.Root, "ui.resultLose"); Assert.IsNotNull(lose, "사망 팝업 조각(Play_Result_Lose)");
            Assert.IsFalse(UiKit.HasPattern(lose), "사망 팝업에도 흐르는 무늬가 없다(T110 ⓑ · 같은 «결과 팝업»)");
            var ldim = lose.Find("Dimmed"); Assert.IsNotNull(ldim, "어둠 조각");
            var reward = UiKit.Find(lose, "Reward"); Assert.IsNotNull(reward, "사망 보상 칸");
            Assert.IsFalse(UiKit.HasLight(reward), "사망 보상(골드) 칸에 빛살 없음(T190)");
            Assert.IsFalse(UiKit.HasLightMask(reward), "사망 보상 칸에 빛 담개도 없음(T190)");
            var icon = reward.Find("Icon"); Assert.IsNotNull(icon, "보상 아이콘");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓐ 레벨업 3택(04)도 어둠 위 무늬를 받는다
            var offer = Perks.Offer(_app.Data, G.Taken, false, new Mulberry32(7u));
            if (offer.Count > 0)
            {
                G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
                _app.Overlay.LevelUp(G, _ => { }); yield return Frames(2);
                var perk = UiKit.Find(_app.Overlay.Root, "ui.perkSelect"); Assert.IsNotNull(perk, "레벨업 3택 조각");
                // T140(주인 2026-09-07 «특전 부분에서는 패턴 없었으면함») — 04 도 이제 «없어야» 한다(T110 ⓑ 결과 팝업과 같은 처리 · 여기 있던 IsTrue 를 이 지시가 덮었다)
                Assert.IsFalse(UiKit.HasPattern(perk), "레벨업 3택(04)에는 흐르는 무늬가 없다(T140)");
                Assert.AreEqual(0, CountPatterns(perk), "04 팝업 나무 어디에도 «Pattern» RawImage 가 없다(T140 4항)");
                AssertPerkCardIsReadable(perk);

                // T155 ⓐ(주인 07:2X·07:3X 재지시 «특전 카드 TitleBorder 는 FillCenter 트루») —
                // 이 두 줄이 있었으면 «넘긴 이름을 전부 false» 던 미반영을 바로 잡았다(지시서 T155 1항 «게이트»).
                Image titleBorder = null, outerBorder = null;
                foreach (var im in perk.GetComponentsInChildren<Image>(true))
                {
                    if (im == null || im.sprite == null || !im.sprite.name.Contains("Border")) continue;
                    if (im.name == "TitleBorder" && titleBorder == null) titleBorder = im;
                    else if (im.name == UiKit.BorderName && outerBorder == null) outerBorder = im;
                }
                Assert.IsNotNull(titleBorder, "특전 카드의 제목 띠 링(TitleBorder) — 조각 구성이 바뀌면 이 자가 알려 준다");
                Assert.IsTrue(titleBorder.fillCenter, "TitleBorder 는 FillCenter 켜짐(제목 «띠» 는 가운데가 채워져야 띠 색이 남는다 · T155 ⓐ)");
                if (outerBorder != null) Assert.IsFalse(outerBorder.fillCenter, "바깥 링(Border)은 가운데를 비운다(내용이 보여야 한다) — 기본값 그대로");

                // T155 ⓒ(주인 ««레벨 업» 위에 글로우 서클이랑 이펙트 라이트 있어야 하는데 없더라 · 회전하게») — 리본 «뒤» 에 빛 두 겹
                var titleGlow = UiKit.Find(perk, "TitleGlow"); Assert.IsNotNull(titleGlow, "«레벨 업» 리본 뒤 빛 담개(T155 ⓒ)");
                // ⚑ T320(주인 2026-09-09 인스펙터) — 빛 두 겹이 이제 **한 겹 더 안**에 있다: `TitleGlow › Mask › LightMask › Glow·Light·Dust`.
                //   그 `Mask` 가 «빛의 아래 절반» 을 잘라 리본 뒤에서 위로만 보이게 한다(주인 «반 잘리는 식으로»).
                //   이 자는 «빛이 있는가» 를 재는 자리라 **찾는 곳만** 한 겹 내린다 — 마스크 자체의 수는 `PerkShineTests` 가 잰다.
                var glowMask = titleGlow.Find("Mask"); Assert.IsNotNull(glowMask, "제목 빛을 자르는 사각 마스크(T320 · 주인 구조)");
                Assert.IsTrue(UiKit.HasLight(glowMask), "리본 뒤 도는 이펙트 라이트(T155 ⓒ)");
                Assert.IsTrue(UiKit.HasGlow(glowMask), "리본 뒤 글로우 서클(T155 ⓒ · 아래 겹)");
                var rib = UiKit.Find(perk, "Title_01_NoDeco_Tangerine");
                if (rib != null) Assert.Less(titleGlow.GetSiblingIndex(), rib.GetSiblingIndex(), "빛 두 겹은 리본 «뒤»(형제 순서 앞 — 자식으로 넣으면 리본 «위» 로 그려진다)");
                var ribLight = (RectTransform)glowMask.Find(UiKit.LightMaskName + "/" + UiKit.LightName);
                Assert.IsTrue(UiKit.IsTweening(ribLight), "리본 뒤 빛살은 돈다(주인 «회전하게») — 글로우 서클은 원이라 안 돌린다");
                // 회차 2(결정 479) — 크기를 «칸 긴 변 × 1.9» 에 맡겼더니 리본이 가로로 길어 한 변이 1231px 가 되고
                // 빛이 화면 위 절반을 덮었다(screens run 360 눈 확인). 레퍼런스의 빛은 화면 폭의 47.1% 안이다.
                Assert.AreEqual(UiKit.FrameW * Overlay.TitleGlowR.W / 100f, ribLight.rect.width, 8f,
                    "리본 뒤 빛 한 변 = 레퍼런스 실측 폭(화면의 " + Overlay.TitleGlowR.W + "%) — 리본 세로에 끌려가면 화면을 덮는다");
                Assert.Less(ribLight.rect.width, UiKit.FrameW * 0.6f, "빛이 화면 폭의 60% 를 넘으면 배경이 씻긴다");
                _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
            }

            // T140 — 보유 특전(05)도 무늬가 «없어야» 한다. 상자는 공통(UiKit.Popup)이라 무늬가 깔려 오는데 이 팝업만 뺀다(다른 팝업은 그대로 = 바로 아래 설정 팝업으로 확인).
            _app.Overlay.PerkBook(G, null); yield return Frames(2);
            var bookBox = UiKit.Find(_app.Overlay.Root, "ui.popup.blue"); Assert.IsNotNull(bookBox, "보유 특전 = 공통 팝업 상자");
            Assert.IsFalse(UiKit.HasPattern(bookBox), "보유 특전(05) 상자 안에는 흐르는 무늬가 없다(T140)");
            Assert.AreEqual(0, CountPatterns(bookBox), "05 팝업 나무 어디에도 «Pattern» RawImage 가 없다(T140 4항)");
            Assert.IsTrue(UiKit.HasGradient(bookBox), "무늬만 뺐다 — 그라데이션(T72 ③)은 그대로");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓒ 공통 팝업 상자 = 패턴 «위» 그라데이션(한 곳에서 팝업 전부가 받는다) — 특전 둘만 T140 으로 빠졌으므로 «남은 팝업» 인 설정으로 본다
            _app.Overlay.Settings(); yield return Frames(2);
            var box = UiKit.Find(_app.Overlay.Root, "ui.popup"); Assert.IsNotNull(box, "설정 = 공통 팝업 상자");
            Assert.IsTrue(UiKit.HasGradient(box), "공통 팝업 상자에 그라데이션(T72 ③ · UiKit.Popup 한 곳)");
            var pat = box.Find(UiKit.PatternName); var top = box.Find(UiKit.GradientTopName); var bottom = box.Find(UiKit.GradientBottomName);
            Assert.IsNotNull(pat, "상자 안 패턴(T140 이 뺀 것은 특전 둘뿐 — 다른 팝업은 그대로)"); Assert.IsNotNull(top, "GradientTop"); Assert.IsNotNull(bottom, "GradientBottom");
            Assert.Less(pat.GetSiblingIndex(), top.GetSiblingIndex(), "그라데이션은 패턴 «위»(질감 층 순서 · 결정 171)");
            Assert.Less(top.GetSiblingIndex(), bottom.GetSiblingIndex(), "위 밝음 → 아래 어둠 순서");
            var border = box.Find(UiKit.BorderName); if (border != null) Assert.Less(bottom.GetSiblingIndex(), border.GetSiblingIndex(), "그라데이션은 테두리 아래");
            var ribbon = UiKit.Find(box, "ui.titleBrown"); if (ribbon != null) Assert.Less(bottom.GetSiblingIndex(), ribbon.GetSiblingIndex(), "그라데이션은 명판 아래(제목이 안 가려진다)");
            Assert.IsFalse(top.GetComponent<Image>().raycastTarget, "그라데이션은 클릭을 안 먹는다(배경 탭으로 닫기 그대로)");
            var brt = (RectTransform)box; var trt = (RectTransform)top;
            Assert.AreEqual(brt.rect.width - 2f * UiKit.PopupPatternInset, trt.rect.width, 1f, "둥근 모서리 안쪽으로 " + UiKit.PopupPatternInset + "px 들여 덧댄다");
            _app.Overlay.Close(); yield return Frames(2);
            Time.timeScale = 1f;

            _log.AssertNoRed("T72 화면 적용(승리·사망·레벨업 · 특전 둘 무늬 없음 · 공통 팝업 그라데이션)");
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
            // T166 ⓐ(주인 2026-09-07 09:2X «배경 무늬를 흰색 7/255 로») — T94 ⓐ 의 «잉크 18/255» 를 덮는다.
            // 색과 알파가 둘 다 바뀌므로 둘 다 잰다: ⓐ 흰 무늬(어두운 초록 바탕이라 밝은 쪽) ⓑ 알파 = 7/255.
            Assert.Greater(praw.color.r, 0.9f, "로비 무늬는 흰색(T166 ⓐ · 잉크로 되돌아가면 빨강)");
            Assert.Greater(praw.color.g, 0.9f, "로비 무늬는 흰색(g)"); Assert.Greater(praw.color.b, 0.9f, "로비 무늬는 흰색(b)");
            Assert.AreEqual(7f / 255f, praw.color.a, 0.001f, "로비 무늬 알파 = 7/255(주인 지정 T166 ⓐ)");
            Assert.AreEqual(UiKit.PatternAlphaLobby, praw.color.a, 0.001f, "그 값은 UiKit.PatternAlphaLobby 한 곳에서 온다");
            Assert.IsFalse(praw.raycastTarget, "무늬는 클릭을 안 먹는다(카드·버튼 그대로)");
            Assert.IsTrue(UiKit.HasGradient(lobby), "로비 배경 그라데이션(T72 ③ 3항 «화면 배경»)");
            var gtop = lobby.Find(UiKit.GradientTopName); var gbot = lobby.Find(UiKit.GradientBottomName);
            Assert.IsNotNull(gtop, "GradientTop"); Assert.IsNotNull(gbot, "GradientBottom");
            Assert.Less(pat.GetSiblingIndex(), gtop.GetSiblingIndex(), "그라데이션은 무늬 «위»(질감 층 순서 · 결정 171)");
            Assert.Less(gtop.GetSiblingIndex(), gbot.GetSiblingIndex(), "위 밝음 → 아래 어둠 순서");
            // T245(주인 2026-09-08 «shine 이펙트는 START 버튼에 있어야 함») — T166 ⓑ 가 **챕터 카드**에 걸었던 그 빛을 START 로 옮겼다.
            // 재는 것은 그대로 셋이고 «어디에» 만 바뀐다: ⓐ 머티리얼 인스턴스가 매달려 있다(MaterialOwner = 그 자리가 죽으면 인스턴스도 죽는다)
            // ⓑ 그 인스턴스를 겨냥한 트윈이 돈다(= 되풀이가 걸렸다 · 한 번 훑고 끝이면 여기서 빨강)
            // ⓒ 화면을 세운 직후에는 빛이 «시작 자리»(버튼 밖)에 있다 — PlayShot PNG 가 훑는 중간을 물지 않는다는 계약(ShineLoop 의 첫 AppendInterval).
            // ⓓ **그리고 카드에는 없다** — 옮긴 것이지 «양쪽에 건» 것이 아니다(이 한 줄이 없으면 «옮겼다» 를 아무도 안 지킨다).
            {
                var startT = lobby.Find("Start"); Assert.IsNotNull(startT, "로비 START 버튼");
                var mo = startT.GetComponent<UiKit.MaterialOwner>();
                Assert.IsNotNull(mo, "START 에 shine 머티리얼 인스턴스(T245 ⓒ)");
                Assert.IsNotNull(mo.Mat, "그 인스턴스가 살아 있다");
                Assert.IsTrue(UiKit.IsTweening(mo.Mat), "START shine 이 «되풀이» 로 돈다(T245 ⓒ · 한 번 훑고 끝이면 빨강)");
                Assert.AreEqual(UiKit.ShineFrom, mo.Mat.GetFloat(UiKit.ShineLocationId), 0.001f,
                    "화면을 세운 직후 빛은 시작 자리 = 비평 PNG 가 훑는 중간을 안 문다(ShineLoop 은 한 주기 뒤에 첫 훑기)");
                Assert.AreEqual(5f, UiKit.ShinePeriod, 0.001f, "주기 = 카드에서 쓰던 값 그대로(5초)");
                var cardT = lobby.Find("ChapterCard"); Assert.IsNotNull(cardT, "로비 챕터 카드");
                Assert.IsNull(cardT.GetComponent<UiKit.MaterialOwner>(), "챕터 카드에는 shine 이 **없다**(T245 ⓑ · T166 ⓑ 가 주인 지시로 뒤집혔다)");
            }
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
                    // T116 3단계 ⓑ — 카드 그라데이션은 «흰/잉크 무채색 덧칠» 이 아니라 레퍼런스 11 실측 두 색이다(주인 «더 화려하게 색깔»).
                    // 무채색으로 되돌아가거나 카드마다 색이 뒤바뀌면 여기서 바로 빨개진다.
                    var wantGrad = PrivilegeCardGrad(c.name); Assert.IsNotNull(wantGrad, c.name + " 은 그라데이션 이름이 정해져 있어야 한다");
                    var want = GradientPalette.Of(wantGrad);
                    var gcTop = c.Find(UiKit.GradientTopName).GetComponent<Image>();
                    var gcBot = c.Find(UiKit.GradientBottomName).GetComponent<Image>();
                    AssertGradTint(want.Top, gcTop.color, c.name + " 위 색 = col.grad." + wantGrad + ".top");
                    AssertGradTint(want.Bottom, gcBot.color, c.name + " 아래 색 = col.grad." + wantGrad + ".bottom");
                    // ⚠ T348 — T345(주인 2026-09-10)가 지목한 두 쌍(cardPrivAd 초록→파랑 · cardPrivMonth 하늘→보라)은
                    //    밝기가 되레 «내려간다». «어두운 위 → 밝은 아래» 는 레퍼런스에서 잰 규칙이라 주인이 색을 직접
                    //    지목한 자리에는 대지 않는다 — `GradientPaletteTests` 가 같은 까닭으로 이미 뺐다(결정 957 ①).
                    //    ⚑ 그때 그쪽만 고치고 **여기를 못 봤다**(같은 규칙이 두 자리에 적혀 있었다 · T184 꼴) → main 이 빨갰다.
                    //    같은 규칙이 아직 세 자리에 있다: 여기 · `GradientPaletteTests` · `UiSmokeTests`(상점 카드 T341 · 그쪽은
                    //    주인 값도 방향을 지켜 초록이다). 이 목록을 또 고칠 사람은 **셋을 한 번에** 훑어라.
                    //    위 «표 값 그대로인가»(AssertGradTint) 는 여섯 전부 그대로 잰다 — 빼는 것은 방향 하나뿐이다.
                    if (wantGrad != "cardPrivAd" && wantGrad != "cardPrivMonth")
                        Assert.Greater(UiKit.Luma(gcBot.color), UiKit.Luma(gcTop.color), c.name + " 카드류는 «어두운 위 → 밝은 아래»(T116 · 레퍼런스 방향)");
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
                    // T190(주인 2026-09-07 13:3X «아이템 슬롯 같은 거에는 빛 효과 없게») — 특권 카드의 «보상 칸» 도 조각 ItemFrame_01 이라 빛이 빠졌다.
                    // 카드 «그림» 뒤 빛살은 위 갈래(Pic:)에서 그대로 남는다 — 칸이 아니라 카드이기 때문이다(주인 «특별 상품» 갈래).
                    Assert.IsTrue(UiKit.IsItemCell(frame), "특권 보상 칸은 «아이템 칸»(조각 ItemFrame_01) 이다 — 판정의 근거(T190 1항)");
                    Assert.IsFalse(UiKit.HasLight(frame), "특권 보상 칸에 빛살 없음(T190)");
                    Assert.IsFalse(UiKit.HasLightMask(frame), "특권 보상 칸에 빛 담개도 없음(T190)");
                    cellLights++;
                }
            }
            Assert.AreEqual(4, cards, "특권 카드 4장(레퍼런스 11)");
            Assert.AreEqual(4, heads, "카드 제목 띠 4");
            Assert.AreEqual(4, cellLights, "보상 칸 4(카드마다 다이아 칸 하나) — 빛은 T190 으로 빠졌고 «칸이 넷» 은 그대로 잰다");
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

        /// <summary>T116 3단계 ⓑ — 특권 카드(11) 이름 → 그라데이션 표 이름. 화면 코드의 상수를 그대로 읽어 «두 곳에 박지» 않는다.</summary>
        static string PrivilegeCardGrad(string cardName)
        {
            switch (cardName)
            {
                case "Card:1": return PrivilegeScreen.GradCard1;
                case "Card:2": return PrivilegeScreen.GradCard2;
                case "Card:3": return PrivilegeScreen.GradCard3;
                case "Card:4": return PrivilegeScreen.GradCard4;
                default: return null;
            }
        }

        /// <summary>그라데이션 조각의 tint 가 «실측 색 + 카드 세기(알파)» 인가 — 색은 계열색(무채색 아님)이어야 한다.</summary>
        static void AssertGradTint(Color want, Color got, string what)
        {
            Assert.AreEqual(want.r, got.r, 0.01f, what + " (R)");
            Assert.AreEqual(want.g, got.g, 0.01f, what + " (G)");
            Assert.AreEqual(want.b, got.b, 0.01f, what + " (B)");
            Assert.AreEqual(UiKit.GradientCardAlpha, got.a, 0.01f, what + " 세기 = 카드 덧칠(GradientCardAlpha)");
            float spread = Mathf.Max(got.r, Mathf.Max(got.g, got.b)) - Mathf.Min(got.r, Mathf.Min(got.g, got.b));
            Assert.Greater(spread, 0.1f, what + " 은 무채색 덧칠이 아니라 계열색이어야 한다(주인 «더 화려하게 색깔»)");
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
            // T138 — 주인 «검회색 프레임에 맞닿아 있다» → 띠가 줄 밑단보다 «더 아래» 로 내려와 초상 밑에 여백이 보여야 한다(위 단언은 부등호 방향 때문에 0 이어도 초록이다)
            Assert.Greater(TopBar.FrameBandBelow, 0f, "띠가 줄 밑으로 자라는 여백(T138)");
            Assert.AreEqual(-TopBar.FrameBandBelow, band.offsetMin.y, 0.01f, "띠 아래 여백 = FrameBandBelow(프레임 px · 요소는 하나도 안 움직인다 = 표 ① 불변)");
            Assert.Less(bandRect.yMin, ScreenRect((RectTransform)top).yMin - 1f, "띠 밑단이 탑바 줄 밑단보다 아래(여백이 0 으로 돌아가면 여기서 빨개진다)");

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

        /// <summary>특전 카드 몸통이 흰 설명 글자와 대비를 낸다(T135) — 몸통이 다시 밝아지면(조각 기본값 #D7D3D3) 여기서 바로 빨개진다.
        /// 이 종류(글자는 제 칸 안에 있고 크기·아웃라인·테두리도 맞는데 «바탕과 같은 밝기라 안 읽힌다»)는 여태 어느 게이트도 못 쟀다(T84 · T121 · T130 과 같은 갈래).</summary>
        static void AssertPerkCardIsReadable(Transform perk)
        {
            var card = UiKit.Find(perk, "ui.card");
            Assert.IsNotNull(card, "특전 카드 한 장");
            var area = UiKit.Find(card, "CardFrameArea");
            Assert.IsNotNull(area, "카드의 CardFrameArea");
            var body = UiKit.Find(area, UiKit.CardBodyName);
            Assert.IsNotNull(body, "카드 몸통(" + UiKit.CardBodyName + ")");
            var bodyImg = body.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(bodyImg, "몸통 Image");
            float bodyLuma = UiKit.Luma(bodyImg.color);
            Assert.LessOrEqual(bodyLuma, PerkBodyLumaMax, "특전 카드 몸통이 밝다(휘도 " + bodyLuma.ToString("0.00") + ") — 흰 글자가 안 읽힌다(T135 · 레퍼런스 04 는 어두운 회색 몸통)");
            var desc = UiKit.Find(card, "Text_Value");
            Assert.IsNotNull(desc, "카드 설명 글자");
            var t = desc.GetComponent<TMP_Text>();
            Assert.IsNotNull(t, "설명 Text");
            float gap = Mathf.Abs(UiKit.Luma(t.color) - bodyLuma);
            Assert.GreaterOrEqual(gap, PerkContrastMin, "설명 글자와 몸통의 밝기 차이가 " + gap.ToString("0.00") + " 뿐이다(T135 · 최소 " + PerkContrastMin + ")");
        }
        /// <summary>특전 카드 몸통 휘도 상한(T135) — 레퍼런스 04 실측 #2C2C2C = 0.17 · 조각 기본값 #D7D3D3 = 0.84.</summary>
        const float PerkBodyLumaMax = 0.30f;
        /// <summary>설명 글자와 몸통의 최소 밝기 차이(T135) — 고치기 전 0.16 · 레퍼런스 0.83.</summary>
        const float PerkContrastMin = 0.45f;
    }
}
