using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T13 — 전투 HUD «얻은 특전 미리보기 줄»(PerkStrip) 비례. 주인 증상: 특전을 여러 개 얻으면 아이콘이 너무 커서 서로 가린다.
    /// 실제 씬(SampleScene → App → 전투)에서 특전 3개 / 12개+중복 1 을 강제로 얻은 뒤 한 프레임 뒤에
    ///   ⓐ 모든 셀(프레임·배지·«+N» 자식까지)이 PerkStrip rect 안에 있고 ⓑ 셀끼리 겹치지 않으며 ⓒ 셀 = 줄 높이의 28/34(index.html) ⓓ 줄이 오른쪽 책 버튼과 안 겹치는가 를 <see cref="RectTransformUtility"/> 로 단언하고,
    ///   ⓔ 화면을 PNG 로 남긴다(<c>Application.temporaryCachePath/perkstrip-screens</c> + 프로젝트 루트 <c>perkstrip-screens/</c> → CI 아티팩트 «perkstrip-screens» · 레포 커밋 금지).
    /// 배치 모드(CI)엔 GameView 가 없어 <see cref="ScreenCapture"/> 가 못 찍으므로 UI 캔버스를 잠시 카메라 모드로 돌려 <b>RenderTexture 타깃</b> 카메라로 직접 그린다(HeroView 와 같은 방식 · 화면 타깃 카메라 수동 렌더는 금지 — CI #34)(에디터에선 GameView 캡처도 함께).
    /// 빨간 줄 검사는 <see cref="PlayLog.AssertNoRed"/>(LogAssert.NoUnexpectedReceived 금지 · ROUTINE §1).
    /// </summary>
    public class PerkStripTests
    {
        App _app;
        readonly List<string> _warn = new List<string>();
        PlayLog _log;   // 빨간 줄(Error·Exception·Assert) 수집 — LogAssert.NoUnexpectedReceived 는 Debug.Log 도 실패로 보므로 쓰지 않는다(PlayLog 주석 · T11)

        [SetUp] public void SetUp() { _warn.Clear(); _log = new PlayLog(); Application.logMessageReceived += OnLog; }
        [TearDown] public void TearDown() { Application.logMessageReceived -= OnLog; _log?.Dispose(); _log = null; Time.timeScale = 1f; }
        void OnLog(string msg, string stack, LogType type)
        {
            if (type != LogType.Warning || msg == null) return;
            if (msg.StartsWith("[UiKit]") || msg.StartsWith("[AssetCatalog]")) _warn.Add(msg);
        }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I;
            Assert.IsNotNull(_app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");
            _warn.Clear();
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
            _log.AssertNoRed("종료(App·캔버스 파괴 뒤)");
        }
        /// <summary>n 프레임 — HeroView(RenderTexture 타깃) 카메라만 강제로 그린다. ⚠ 월드(화면 타깃) 카메라는 수동 Render 금지 — 배치 모드에서 URP 최종 블릿 크기 불일치 에러를 도구가 만든다(CI #34 · UiSmokeTests 주석).</summary>
        IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }
        IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return Frames(1); }
        void Check(string where)
        {
            _log.AssertNoRed(where);
            if (_warn.Count > 0) { var w = string.Join("\n", _warn); _warn.Clear(); Assert.Fail($"[{where}] 프리팹 경로/카탈로그 키 경고:\n{w}"); }
        }

        // ───────────────────────── 기하 검사 ─────────────────────────
        static string Fmt(Rect r) => $"x[{r.xMin:0.#}..{r.xMax:0.#}] y[{r.yMin:0.#}..{r.yMax:0.#}]";
        static Rect RelBounds(RectTransform parent, RectTransform child)
        {
            var b = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, child);   // 자식·손자(프레임·배지) 전부 포함 · 배율 반영
            return Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
        }
        static void AssertInside(Rect inner, Rect outer, float tol, string what)
        {
            Assert.IsTrue(inner.xMin >= outer.xMin - tol && inner.xMax <= outer.xMax + tol && inner.yMin >= outer.yMin - tol && inner.yMax <= outer.yMax + tol,
                $"{what} 가 밖으로 나갔다: {Fmt(inner)} ⊄ {Fmt(outer)}");
        }

        /// <summary>줄의 활성 자식(셀·«+N»)을 왼쪽부터 순서대로 — ⓐ 안에 ⓑ 안 겹침 ⓒ 비례 ⓓ 책 버튼과 분리. 반환 = (셀 수, «+N» 글자).</summary>
        (int cells, string more) AssertStrip(RectTransform strip, RectTransform book, int distinct, string where)
        {
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(strip);
            var spec = BattleScreen.PerkStripMetrics(strip);
            Assert.Greater(strip.rect.width, 1f, "줄 폭"); Assert.Greater(strip.rect.height, 1f, "줄 높이");
            var kids = new List<RectTransform>();
            for (int i = 0; i < strip.childCount; i++) { var c = strip.GetChild(i) as RectTransform; if (c != null && c.gameObject.activeSelf) kids.Add(c); }
            var boxes = new List<(RectTransform rt, Rect r)>();
            foreach (var k in kids) boxes.Add((k, RelBounds(strip, k)));
            boxes.Sort((a, b) => a.r.xMin.CompareTo(b.r.xMin));
            int cells = 0; string more = null;
            foreach (var (rt, r) in boxes)
            {
                var txt = rt.GetComponent<Text>();
                if (txt != null) { more = txt.text; AssertInside(r, strip.rect, 1f, $"[{where}] «{txt.text}» 칸"); continue; }
                cells++;
                AssertInside(r, strip.rect, 1f, $"[{where}] 셀 {rt.name}");
                // ⓒ 셀 = 줄 높이의 28/34 · 프레임(프리팹 본래 162 를 배율로) 이 셀 안에 — 자식 전체 bounds 로(종전엔 78px 셀에 162px 프레임이 그려져 겹쳤다)
                Assert.That(rt.rect.height / strip.rect.height, Is.EqualTo(28f / 34f).Within(0.02f), $"[{where}] 셀 높이 비례(index.html .pv-ic 28/34)");
                Assert.That(rt.rect.width, Is.EqualTo(rt.rect.height).Within(0.5f), "셀은 정사각");
                Assert.Greater(rt.childCount, 0, "셀 안에 프레임이 있어야 한다");
                var frame = rt.GetChild(0) as RectTransform;
                var fb = RelBounds(rt, frame);
                AssertInside(fb, rt.rect, 1.5f, $"[{where}] 셀 {rt.name} 의 프레임(자식 포함)");
                Assert.That(fb.width, Is.EqualTo(rt.rect.width).Within(3f), "프레임이 셀을 꽉 채운다(너무 작지도 않다)");
                for (int c = 1; c < rt.childCount; c++) { var badge = rt.GetChild(c) as RectTransform; if (badge != null && badge.gameObject.activeSelf) AssertInside(RelBounds(rt, badge), rt.rect, 1f, $"[{where}] 셀 {rt.name} 의 개수 배지"); }
            }
            // ⓑ 이웃끼리 안 겹침(간격 = 줄 높이의 4/34)
            for (int i = 1; i < boxes.Count; i++)
            {
                var a = boxes[i - 1].r; var b = boxes[i].r;
                Assert.IsTrue(b.xMin >= a.xMax - 0.5f, $"[{where}] {boxes[i - 1].rt.name}{Fmt(a)} 와 {boxes[i].rt.name}{Fmt(b)} 가 겹친다");
                Assert.That(b.xMin - a.xMax, Is.EqualTo(spec.Gap).Within(1f), $"[{where}] 간격 = 줄 높이의 4/34");
            }
            // 개수 = 순수 계산(Layout.PerkStripSpec)과 같다 · «+N» 이 있으면 N = 숨긴 개수
            Assert.AreEqual(spec.Shown(distinct), cells, $"[{where}] 보이는 셀 수 = 줄 폭 ÷ (셀+간격)");
            if (cells < distinct) Assert.AreEqual("+" + (distinct - cells), more, $"[{where}] «+N»"); else Assert.IsNull(more, $"[{where}] 다 들어가면 «+N» 없음");
            // ⓓ 줄이 오른쪽 책 버튼(보유 특전) 과 안 겹침
            if (book != null)
            {
                var root = (RectTransform)strip.parent;
                var sb = RelBounds(root, strip); var bb = RelBounds(root, book);
                Assert.IsTrue(sb.xMax <= bb.xMin + 0.5f, $"[{where}] 특전 줄 {Fmt(sb)} 이 책 버튼 {Fmt(bb)} 을 덮는다");
            }
            return (cells, more);
        }

        // ───────────────────────── 스크린샷 ─────────────────────────
        static IEnumerable<string> ScreenDirs()
        {
            yield return Path.Combine(Application.temporaryCachePath, "perkstrip-screens");
            string root = null; try { root = Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } catch { }
            if (!string.IsNullOrEmpty(root)) yield return Path.Combine(root, "perkstrip-screens");   // CI 워크스페이스 → actions/upload-artifact «perkstrip-screens»
        }
        /// <summary>UI 캔버스를 잠시 카메라 모드로 돌려 RenderTexture 에 그린 뒤 PNG 로 — 배치 모드에서도 된다. 실패해도 테스트는 안 깨진다(경고 1줄).</summary>
        IEnumerator SaveScreens(string name)
        {
            var canvas = _app != null ? _app.UiCanvas : null; if (canvas == null) yield break;
            int w = 540, h = Mathf.RoundToInt(540f * UiKit.FrameH / UiKit.FrameW);
            var oldMode = canvas.renderMode; var oldCam = canvas.worldCamera; float oldPlane = canvas.planeDistance;
            RenderTexture rt = null; GameObject camGo = null; Texture2D tex = null; byte[] png = null;
            try
            {
                // HeroView.CreateTargetTexture 와 같은 규칙(색 ARGB32 + 깊이 24·스텐실 8 = Renderer2D 설정과 맞춤 · 주인 콘솔 에러 ①②) — 세로 화면이라 정사각이 아닐 뿐
                var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGB32, 24) { msaaSamples = 1, useMipMap = false, autoGenerateMips = false, volumeDepth = 1, dimension = UnityEngine.Rendering.TextureDimension.Tex2D };
                var ds = GraphicsFormatUtility.GetDepthStencilFormat(24, 8); if (ds != GraphicsFormat.None) desc.depthStencilFormat = ds;
                rt = new RenderTexture(desc) { name = "PerkStripShot" }; rt.Create();
                camGo = new GameObject("PerkStripShotCam", typeof(Camera)); var cam = camGo.GetComponent<Camera>();
                var world = _app.WorldCamera;
                if (world != null) { cam.CopyFrom(world); camGo.transform.SetPositionAndRotation(world.transform.position, world.transform.rotation); }
                else { cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.cullingMask = 0; }
                cam.targetTexture = rt;
                var urp = UiKit.Ensure<UniversalAdditionalCameraData>(camGo); urp.renderType = CameraRenderType.Base; urp.renderPostProcessing = false;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = Mathf.Clamp(1f, cam.nearClipPlane + 0.01f, cam.farClipPlane - 0.01f);
                Canvas.ForceUpdateCanvases();
                cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                png = tex.EncodeToPNG();
            }
            catch (Exception e) { Debug.LogWarning("[PerkStripTests] 스크린샷(RenderTexture) 실패 — 기하 단언은 별도로 통과했다: " + e.Message); }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCam; canvas.planeDistance = oldPlane;
                if (camGo != null) { var c = camGo.GetComponent<Camera>(); if (c != null) { c.enabled = false; c.targetTexture = null; } UnityEngine.Object.Destroy(camGo); }
                if (tex != null) UnityEngine.Object.Destroy(tex);
                if (rt != null) { rt.Release(); UnityEngine.Object.Destroy(rt); }
                Canvas.ForceUpdateCanvases();
            }
            if (png != null)
                foreach (var dir in ScreenDirs())
                {
                    try { Directory.CreateDirectory(dir); var p = Path.Combine(dir, name + ".png"); File.WriteAllBytes(p, png); Debug.Log("[PerkStripTests] 스크린샷 저장: " + p); }
                    catch (Exception e) { Debug.LogWarning("[PerkStripTests] 스크린샷 저장 실패(" + dir + "): " + e.Message); }
                }
            if (!Application.isBatchMode)
            {
                // 에디터·플레이어에선 GameView 그대로도 남긴다(주인 확인용 · 끝 프레임에 찍힌다)
                foreach (var dir in ScreenDirs()) { try { Directory.CreateDirectory(dir); ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + "-gameview.png")); break; } catch { } }
                yield return Frames(2);
            }
            yield return Frames(1);
        }

        // ───────────────────────── 테스트 ─────────────────────────
        /// <summary>특전 3개(다 들어감 · «+N» 없음) → 12개+중복 1(«+N» 으로 접힘 · 배지) — 둘 다 겹침 0 · 줄 안 · 비례 28/34 · 스크린샷.</summary>
        [UnityTest]
        public IEnumerator TwelvePerksFitWithoutOverlapAndScreenshot()
        {
            yield return Boot();
            var D = _app.Data;
            _app.StartBattle(1);
            yield return RealSeconds(0.5f);
            Assert.AreEqual("battle", _app.Current.Name);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            Time.timeScale = 0f; _app.Overlay.Close(); G.Pending = null; yield return Frames(1);   // 엔진 정지(팝업·레벨업이 끼어들지 않게) · HUD 갱신(Tick 의 RefreshHud)은 계속 돈다
            var strip = UiKit.Find(bs.Root, "PerkStrip") as RectTransform; Assert.IsNotNull(strip, "PerkStrip");
            var book = UiKit.Find(bs.Root, "PerkBook") as RectTransform;
            var all = D.Perks.Perks; Assert.GreaterOrEqual(all.Count, 13, "perks.json 특전 수");

            // ① 3개 — 전부 보인다
            G.Taken.Clear(); for (int i = 0; i < 3; i++) G.Taken.Add(all[i]);
            yield return Frames(2);
            var r3 = AssertStrip(strip, book, 3, "특전 3개");
            Assert.AreEqual(3, r3.cells); Assert.IsNull(r3.more);
            Check("특전 3개");
            yield return SaveScreens("perkstrip-03");

            // ② 서로 다른 12개 + 첫 특전 중복 1 (주인 증상 재현 상태) — «+N» 으로 접히고 배지가 붙는다
            G.Taken.Clear(); for (int i = 0; i < 12; i++) G.Taken.Add(all[i]); G.Taken.Add(all[0]);
            yield return Frames(2);
            var r12 = AssertStrip(strip, book, 12, "특전 12개+중복");
            Assert.Less(r12.cells, 12, "12개는 한 줄에 다 안 들어가므로 «+N» 으로 접혀야 한다(종전 상수 11 = 넘침)");
            Assert.GreaterOrEqual(r12.cells, 6, "그래도 6개 이상은 보여야 한다");
            Assert.IsNotNull(r12.more, "«+N»");
            var first = strip.GetChild(0) as RectTransform; Assert.AreEqual(all[0].Id, first.name, "첫 셀 = 첫 특전");
            var badge = first.GetComponentInChildren<Text>(false); Assert.IsNotNull(badge, "중복 특전엔 개수 배지가 있다"); Assert.AreEqual("2", badge.text, "배지 = 중복 개수");
            var bookCount = book != null ? book.GetComponentInChildren<Text>(false) : null; if (bookCount != null) Assert.AreEqual("13", bookCount.text, "책 버튼 개수 = 얻은 특전 수(중복 포함)");
            Check("특전 12개+중복");
            yield return SaveScreens("perkstrip-12");

            // ③ 다시 줄여도(«+N» 사라짐) 잔여 셀이 없다
            G.Taken.Clear(); G.Taken.Add(all[5]); yield return Frames(2);
            var r1 = AssertStrip(strip, book, 1, "특전 1개"); Assert.AreEqual(1, r1.cells); Assert.IsNull(r1.more);
            Check("특전 1개");

            Time.timeScale = 1f; yield return RealSeconds(0.3f);
            _app.ShowScreen("lobby"); yield return Frames(3);
            Check("전투 → 로비");
            yield return Shutdown();
        }
    }
}
