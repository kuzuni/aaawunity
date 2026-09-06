using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T22 — 주인 «모든 버튼에 눌림 표시». <see cref="UiKit.Clickable"/> 로 만드는 모든 버튼이 누르는 동안 어두워지는가.
    /// ① 단독 캔버스(App 없이): 보이는 Image 루트 → ColorTint(pressed ×0.8 · highlighted 그대로) 가 실제로 CanvasRenderer 색을 어둡게 하고 떼면 복원 · 비활성은 안 어두워짐
    /// ② 투명 히트 영역 + 자식 그림 → 자식의 첫 보이는 Image 가 targetGraphic ③ 자식에도 그림이 없으면 CanvasGroup alpha ×0.8(PressFeedback) ④ targetGraphic 이 파괴된 뒤 누르면 다시 고른다
    /// ⑤ 실제 씬(SampleScene → App): 로비·장비·상점·대장간 화면의 <b>활성 Button 전부</b>가 눌림 표시를 갖는가(ColorTint + 보이는 그림 또는 PressFeedback) · 하단 탭 5칸은 전부 보이는 그림에 색을 입힌다 · 현재 탭 강조(Focus)는 그대로 · 빨간 줄 0.
    /// 포인터는 입력 장치 없이 <see cref="ExecuteEvents"/> 로 흉내 낸다. 빨간 줄 검사는 <see cref="PlayLog.AssertNoRed"/>(LogAssert.NoUnexpectedReceived 금지 · ROUTINE §1).
    /// </summary>
    public class PressFeedbackTests
    {
        PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }

        static PointerEventData Pointer() => new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        static void Press(GameObject go) { var e = Pointer(); ExecuteEvents.Execute(go, e, ExecuteEvents.pointerEnterHandler); ExecuteEvents.Execute(go, e, ExecuteEvents.pointerDownHandler); }
        static void Release(GameObject go) { var e = Pointer(); ExecuteEvents.Execute(go, e, ExecuteEvents.pointerUpHandler); ExecuteEvents.Execute(go, e, ExecuteEvents.pointerExitHandler); }

        static Canvas TestCanvas()
        {
            var canvasGo = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = canvasGo.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; return c;
        }
        static RectTransform Node(Transform parent, string name, Color? imageColor)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(200, 100);
            if (imageColor.HasValue) { var im = go.AddComponent<Image>(); im.color = imageColor.Value; }
            return rt;
        }
        static float Gray(Color c) => (c.r + c.g + c.b) / 3f;

        // ───────────────────────── ① 보이는 루트 Image — ColorTint 가 실제로 어둡게 · 떼면 복원 · 비활성은 그대로 ─────────────────────────
        [UnityTest]
        public IEnumerator VisibleRootImageDarkensWhilePressedAndRestores()
        {
            var canvas = TestCanvas();
            var rt = Node(canvas.transform, "Btn", new Color(0.9f, 0.5f, 0.2f, 1f));
            int clicks = 0; var b = UiKit.Clickable(rt, () => clicks++);
            yield return Frames(1);

            Assert.AreEqual(Selectable.Transition.ColorTint, b.transition, "눌림 표시 = ColorTint");
            Assert.AreEqual(rt.GetComponent<Image>(), b.targetGraphic, "루트 Image 가 보이면 그것이 targetGraphic");
            Assert.AreEqual(UiKit.PressedMul, b.colors.pressedColor.r, 1e-4f, "pressedColor ≈ ×0.8");
            Assert.AreEqual(b.colors.normalColor, b.colors.highlightedColor, "highlighted 는 그대로(normal 과 같음)");
            Assert.AreEqual(Color.white, b.colors.disabledColor, "disabled 색은 흰색(비활성 반투명은 CanvasGroup 이 맡는다)");
            Assert.IsNotNull(rt.GetComponent<PressFeedback>(), "PressFeedback 이 모든 Clickable 에 붙는다");
            var cr = b.targetGraphic.canvasRenderer;
            Assert.AreEqual(1f, Gray(cr.GetColor()), 1e-3f, "누르기 전 CanvasRenderer 색 = 흰색(원래 그림색 그대로)");

            Press(rt.gameObject); yield return RealSeconds(0.3f);
            Assert.AreEqual(UiKit.PressedMul, Gray(cr.GetColor()), 0.02f, "누르는 동안 CanvasRenderer 색이 ×0.8 로 어두워야 한다");
            Assert.AreEqual(new Color(0.9f, 0.5f, 0.2f, 1f), rt.GetComponent<Image>().color, "Image.color(그림색) 자체는 안 바뀐다 — CanvasRenderer 곱색만");
            Assert.IsFalse(rt.GetComponent<PressFeedback>().Dimmed, "그림으로 보이면 CanvasGroup alpha 는 안 건드린다");

            Release(rt.gameObject); yield return RealSeconds(0.3f);
            Assert.AreEqual(1f, Gray(cr.GetColor()), 0.02f, "손을 떼면 원래 색으로");
            Assert.AreEqual(0, clicks, "pointerDown/Up 만으로는 onClick 이 안 불린다(클릭 이벤트가 따로)");

            // 비활성 — SetInteractable 의 반투명(0.5) 그대로 · 눌러도 안 어두워짐
            UiKit.SetInteractable(b, false); yield return Frames(1);
            Assert.AreEqual(0.5f, rt.GetComponent<CanvasGroup>().alpha, 1e-4f, "비활성 = CanvasGroup 0.5(지금처럼)");
            Press(rt.gameObject); yield return RealSeconds(0.3f);
            Assert.AreEqual(1f, Gray(cr.GetColor()), 0.02f, "비활성은 눌러도 색이 안 변한다(disabledColor 흰색)");
            Assert.AreEqual(0.5f, rt.GetComponent<CanvasGroup>().alpha, 1e-4f, "비활성은 눌러도 alpha 그대로");
            Release(rt.gameObject); yield return Frames(1);
            _log.AssertNoRed("① 보이는 루트 Image");
            Object.Destroy(canvas.gameObject); yield return Frames(1);
        }

        // ───────────────────────── ② 투명 히트 영역 + 자식 그림 → 자식의 첫 보이는 Image ─────────────────────────
        [UnityTest]
        public IEnumerator TransparentRootUsesFirstVisibleChildImage()
        {
            var canvas = TestCanvas();
            var root = Node(canvas.transform, "Cell", null);                       // 루트에 Image 없음 → Clickable 이 투명 Image 를 붙인다
            var hidden = Node(root, "Hidden", Color.red); hidden.gameObject.SetActive(false);   // 꺼진 자식은 건너뛴다
            var clear = Node(root, "Clear", new Color(1, 1, 1, 0));               // 투명 자식도 건너뛴다
            var bg = Node(root, "Bg", Color.blue); var icon = Node(bg, "Icon", Color.green);
            var b = UiKit.Clickable(root, () => { });
            yield return Frames(1);

            var self = root.GetComponent<Image>();
            Assert.IsNotNull(self); Assert.AreEqual(0f, self.color.a, 1e-4f, "루트에는 투명 히트 Image");
            Assert.AreEqual(bg.GetComponent<Image>(), b.targetGraphic, "보이는 첫 자식 Image(Bg) 가 targetGraphic — 꺼진 것·투명한 것은 건너뛴다");
            Assert.IsTrue(UiKit.HasVisiblePressTarget(b));

            Press(root.gameObject); yield return RealSeconds(0.3f);
            Assert.AreEqual(UiKit.PressedMul, Gray(bg.GetComponent<Image>().canvasRenderer.GetColor()), 0.02f, "누르면 Bg 가 어두워진다");
            Assert.AreEqual(1f, Gray(icon.GetComponent<Image>().canvasRenderer.GetColor()), 1e-3f, "targetGraphic 이 아닌 자식은 그대로");
            Assert.IsFalse(root.GetComponent<PressFeedback>().Dimmed, "그림이 있으면 alpha 는 안 건드린다");
            Release(root.gameObject); yield return RealSeconds(0.3f);
            Assert.AreEqual(1f, Gray(bg.GetComponent<Image>().canvasRenderer.GetColor()), 0.02f);

            // ④ 자식을 갈아엎어 targetGraphic 이 파괴되면 — 다음 누름에서 다시 고른다
            Object.Destroy(bg.gameObject); yield return Frames(1);
            var bg2 = Node(root, "Bg2", Color.yellow); yield return Frames(1);
            Assert.IsFalse(UiKit.HasVisiblePressTarget(b), "파괴된 targetGraphic 은 «보이는 그림» 이 아니다");
            Press(root.gameObject); yield return RealSeconds(0.3f);
            Assert.AreEqual(bg2.GetComponent<Image>(), b.targetGraphic, "누르는 순간 새 자식(Bg2) 을 다시 고른다");
            Assert.AreEqual(UiKit.PressedMul, Gray(bg2.GetComponent<Image>().canvasRenderer.GetColor()), 0.02f, "다시 고른 그림이 바로 어두워진다");
            Release(root.gameObject); yield return RealSeconds(0.3f);
            Assert.AreEqual(1f, Gray(bg2.GetComponent<Image>().canvasRenderer.GetColor()), 0.02f);
            _log.AssertNoRed("② 투명 히트 + 자식 그림");
            Object.Destroy(canvas.gameObject); yield return Frames(1);
        }

        // ───────────────────────── ③ 그림이 전혀 없는 히트 영역 → CanvasGroup alpha ×0.8 ─────────────────────────
        [UnityTest]
        public IEnumerator NoImageAnywhereDimsWithCanvasGroup()
        {
            var canvas = TestCanvas();
            var root = Node(canvas.transform, "Hit", null);
            var b = UiKit.Clickable(root, () => { }, false);
            yield return Frames(1);
            Assert.IsFalse(UiKit.HasVisiblePressTarget(b), "보이는 그림이 없다");
            var pf = root.GetComponent<PressFeedback>(); Assert.IsNotNull(pf);

            Press(root.gameObject); yield return Frames(1);
            var cg = root.GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg, "누르는 순간 CanvasGroup 을 붙인다"); Assert.IsTrue(pf.Dimmed);
            Assert.AreEqual(UiKit.PressedMul, cg.alpha, 1e-4f, "누르는 동안 alpha ×0.8");
            Release(root.gameObject); yield return Frames(1);
            Assert.IsFalse(pf.Dimmed); Assert.AreEqual(1f, cg.alpha, 1e-4f, "떼면 복원");

            // 이미 반투명(0.4 · 예: 대장간 재료 흐림)이면 그 값 기준으로 곱하고 그 값으로 복원
            cg.alpha = 0.4f; Press(root.gameObject); yield return Frames(1);
            Assert.AreEqual(0.4f * UiKit.PressedMul, cg.alpha, 1e-4f);
            Release(root.gameObject); yield return Frames(1); Assert.AreEqual(0.4f, cg.alpha, 1e-4f);

            // 밖으로 나가도(pointerExit) 복원 · 비활성은 아무것도 안 함
            cg.alpha = 1f; Press(root.gameObject); yield return Frames(1); Assert.IsTrue(pf.Dimmed);
            ExecuteEvents.Execute(root.gameObject, Pointer(), ExecuteEvents.pointerExitHandler); yield return Frames(1);
            Assert.IsFalse(pf.Dimmed); Assert.AreEqual(1f, cg.alpha, 1e-4f, "밖으로 나가면 복원");
            UiKit.SetInteractable(b, false); Press(root.gameObject); yield return Frames(1);
            Assert.IsFalse(pf.Dimmed); Assert.AreEqual(0.5f, cg.alpha, 1e-4f, "비활성은 반투명 0.5 그대로 · 눌러도 변화 없음");
            Release(root.gameObject); yield return Frames(1);
            _log.AssertNoRed("③ 그림 없는 히트 영역");
            Object.Destroy(canvas.gameObject); yield return Frames(1);
        }

        // ───────────────────────── ⑤ 실제 씬 — 모든 화면의 활성 Button 전부 · 하단 탭 5칸 · 현재 탭 강조 유지 ─────────────────────────
        [UnityTest]
        public IEnumerator EveryButtonOnEveryScreenHasPressFeedback()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            var app = App.I; Assert.IsNotNull(app, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            yield return RenderFrames(2);
            _log.AssertNoRed("부팅");

            // 장비가 있어야 인벤 칸(투명 루트 + 프레임) 버튼이 생긴다
            var G = app.Data.Gear; int made = 0;
            foreach (var t in G.AllTypes) { if (made >= 6) break; app.Save.Inv.Add(app.Save.NewGear(t.Part, t.Type, made % 4, 0)); made++; }

            foreach (var screen in new[] { "lobby", "gear", "shop", "forge" })
            {
                app.Overlay.Close(); app.ShowScreen(screen); yield return RenderFrames(2);
                var bad = new List<string>(); int n = 0, tinted = 0, dimmed = 0;
                foreach (var b in app.UiCanvas.GetComponentsInChildren<Button>(false))
                {
                    n++;
                    bool tint = UiKit.HasVisiblePressTarget(b); bool pf = b.GetComponent<PressFeedback>() != null;
                    if (tint) tinted++; else if (pf) dimmed++;
                    if (b.transition != Selectable.Transition.ColorTint) bad.Add(PathOf(b.transform) + " :: transition=" + b.transition);
                    else if (!tint && !pf) bad.Add(PathOf(b.transform) + " :: 보이는 그림도 PressFeedback 도 없음");
                    else if (Mathf.Abs(b.colors.pressedColor.r - UiKit.PressedMul) > 1e-3f) bad.Add(PathOf(b.transform) + " :: pressedColor=" + b.colors.pressedColor);
                }
                Assert.Greater(n, 0, $"[{screen}] 버튼이 하나는 있어야 한다");
                if (bad.Count > 0) Assert.Fail($"[{screen}] 눌림 표시 없는 버튼 {bad.Count}/{n}건:\n" + string.Join("\n", bad));
                Debug.Log($"[PressFeedbackTests] {screen}: 버튼 {n} · 색 눌림 {tinted} · alpha 눌림 {dimmed}");

                // 하단 탭 5칸 — 전부 «보이는 그림에 색» · 현재 탭 강조(Focus 켜짐) 그대로 (대장간은 탭 바가 없다 — 장비 화면 «합성» 으로만 진입 · T10)
                if (screen == "forge") { _log.AssertNoRed(screen + " 눌림 표시 검사"); continue; }
                var tabs = UiKit.Find(app.Current.Root, "Tab_01_BottomFlushMenu");
                if (tabs == null) tabs = UiKit.Find(app.Current.Root, "ui.tabBar");
                Assert.IsNotNull(tabs, $"[{screen}] 하단 탭 바");
                int tabBtns = 0;
                for (int i = 0; i < tabs.childCount && i < NavBar.Keys.Length; i++)
                {
                    var tab = tabs.GetChild(i); var tb = tab.GetComponent<Button>();
                    Assert.IsNotNull(tb, $"[{screen}] 탭 {i} 에 Button"); tabBtns++;
                    Assert.IsTrue(UiKit.HasVisiblePressTarget(tb), $"[{screen}] 탭 {NavBar.Labels[i]} 은 보이는 그림(Normal/Focus 배경)에 색을 입혀야 한다");
                    bool on = NavBar.Keys[i] == screen || (NavBar.Keys[i] == "battle" && screen == "lobby");
                    var focus = UiKit.Find(tab, "Focus"); var normal = UiKit.Find(tab, "Normal");
                    if (focus != null) Assert.AreEqual(on, focus.gameObject.activeSelf, $"[{screen}] 탭 {NavBar.Labels[i]} Focus = 현재 탭 강조 그대로");
                    if (normal != null) Assert.AreEqual(!on, normal.gameObject.activeSelf, $"[{screen}] 탭 {NavBar.Labels[i]} Normal");
                }
                Assert.AreEqual(5, tabBtns, $"[{screen}] 탭 5칸 전부 버튼");

                // 탭 하나를 실제로 눌러 본다(장치 없이) — 어두워지고 · 떼면 복원 · 빨간 줄 0
                var probe = tabs.GetChild(0).GetComponent<Button>(); var g = probe.targetGraphic;
                Press(probe.gameObject); yield return RealSeconds(0.3f);
                Assert.AreEqual(UiKit.PressedMul, Gray(g.canvasRenderer.GetColor()), 0.02f, $"[{screen}] 탭을 누르면 배경이 ×0.8");
                Release(probe.gameObject); yield return RealSeconds(0.3f);
                Assert.AreEqual(1f, Gray(g.canvasRenderer.GetColor()), 0.02f, $"[{screen}] 떼면 복원");
                _log.AssertNoRed(screen + " 눌림 표시 검사");
            }

            // 팝업(장비 세부 · 설정) 의 버튼도
            app.ShowScreen("gear"); yield return RenderFrames(1);
            var first = app.Save.Inv.Count > 0 ? app.Save.Inv[0] : null; Assert.IsNotNull(first);
            GearUi.OpenDetail(app, first, null); yield return RenderFrames(2);
            AssertAllButtons(app.Overlay.Root, "장비 세부 팝업");
            app.Overlay.Close(); app.Overlay.Settings(); yield return RenderFrames(2);
            AssertAllButtons(app.Overlay.Root, "설정 팝업");
            app.Overlay.Close(); yield return RenderFrames(1);
            _log.AssertNoRed("팝업 눌림 표시 검사");

            Time.timeScale = 1f;
            if (app.UiCanvas != null) Object.Destroy(app.UiCanvas.gameObject); Object.Destroy(app.gameObject);
            yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        static void AssertAllButtons(Transform root, string where)
        {
            var bad = new List<string>(); int n = 0;
            foreach (var b in root.GetComponentsInChildren<Button>(false))
            {
                n++;
                if (b.transition != Selectable.Transition.ColorTint || (!UiKit.HasVisiblePressTarget(b) && b.GetComponent<PressFeedback>() == null)) bad.Add(PathOf(b.transform));
            }
            Assert.Greater(n, 0, $"[{where}] 버튼이 하나는 있어야 한다");
            if (bad.Count > 0) Assert.Fail($"[{where}] 눌림 표시 없는 버튼 {bad.Count}/{n}건:\n" + string.Join("\n", bad));
        }
        static string PathOf(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
        /// <summary>n 프레임 — 살아 있는 HeroView 카메라(RenderTexture)만 수동 렌더(배치 모드 · 화면 타깃 카메라는 수동 렌더 금지 — CI #34).</summary>
        static IEnumerator RenderFrames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }
    }
}
