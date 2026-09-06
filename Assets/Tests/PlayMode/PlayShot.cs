using System;
using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;
using KkomaKnight.Game;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// UI 비평 하니스(T46)의 공용 도우미 — ⓐ 화면을 <b>540×1170 PNG</b> 로(<see cref="Save"/> · PerkStripTests 의 RenderTexture 방식을 옮김 · 배치 모드에서도 됨)
    /// ⓑ 활성 <see cref="UiTag"/> 를 모아 프레임 % 사각형(<see cref="Layout"/>) ⓒ <c>layout.json</c>·<c>meta.json</c> 쓰기(<see cref="WriteLayout"/>).
    /// 저장 위치 = <c>Application.temporaryCachePath/&lt;folder&gt;</c> + 프로젝트 루트 <c>&lt;folder&gt;/</c>(CI 워크스페이스 → screens 브랜치 배포 · 레포 커밋 금지 · .gitignore).
    /// ⚠ 월드(화면 타깃) 카메라를 수동 Render 하지 않고 RenderTexture 타깃 카메라를 따로 만든다(CI #34 · URP 최종 블릿 크기 불일치 오탐 방지).
    /// </summary>
    public static class PlayShot
    {
        public const int ShotW = 540;
        public static int ShotH => Mathf.RoundToInt(ShotW * UiKit.FrameH / UiKit.FrameW);   // 1169 ≈ 1170(9:19.5)
        public const string DefaultFolder = "ui-screens";
        /// <summary>마지막 <see cref="Save"/> 에서 프레임(<c>app.Frame</c>)이 RenderTexture 를 채운 비율(가로·세로 중 작은 쪽 · 0~1). T58 회귀 단언용 — 첫 screens 배포(CI #83)는 0.348 이었다.</summary>
        public static float LastFrameFill { get; private set; } = -1f;
        /// <summary>마지막 <see cref="Save"/> 의 진단 한 줄(카메라 rect · pixelRect · 캔버스 크기 · 프레임 px 사각형) — CI 로그에서 원인을 읽는다(T58).</summary>
        public static string LastFrameInfo { get; private set; } = "";

        public static IEnumerable<string> Dirs(string folder = DefaultFolder)
        {
            yield return Path.Combine(Application.temporaryCachePath, folder);
            string root = null; try { root = Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } catch { }
            if (!string.IsNullOrEmpty(root)) yield return Path.Combine(root, folder);
        }

        /// <summary>UI 캔버스를 잠시 카메라 모드로 돌려 RenderTexture 에 그린 뒤 PNG 로 저장. 실패해도 테스트는 안 깨진다(경고 1줄 · false). 월드(전투)도 같은 카메라가 그린다(CopyFrom).</summary>
        public static bool Save(App app, string name, string folder = DefaultFolder)
        {
            var canvas = app != null ? app.UiCanvas : null; if (canvas == null) return false;
            int w = ShotW, h = ShotH;
            var oldMode = canvas.renderMode; var oldCam = canvas.worldCamera; float oldPlane = canvas.planeDistance; int oldOrder = canvas.sortingOrder;
            RenderTexture rt = null; GameObject camGo = null; Texture2D tex = null; byte[] png = null;
            LastFrameFill = -1f; LastFrameInfo = "";
            try
            {
                var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGB32, 24) { msaaSamples = 1, useMipMap = false, autoGenerateMips = false, volumeDepth = 1, dimension = UnityEngine.Rendering.TextureDimension.Tex2D };
                var ds = GraphicsFormatUtility.GetDepthStencilFormat(24, 8); if (ds != GraphicsFormat.None) desc.depthStencilFormat = ds;
                rt = new RenderTexture(desc) { name = "PlayShot" }; rt.Create();
                camGo = new GameObject("PlayShotCam", typeof(Camera)); var cam = camGo.GetComponent<Camera>();
                var world = app.WorldCamera;
                if (world != null) { cam.CopyFrom(world); camGo.transform.SetPositionAndRotation(world.transform.position, world.transform.rotation); }
                else { cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.cullingMask = 0; }
                // T58: CopyFrom 은 WorldCam 의 letterbox 뷰포트(cam.rect · 배치 모드 가로 화면에선 폭 ≈35%)까지 복사해 캔버스가 RT 의 188×404 띠에만 그려졌다 → 촬영 카메라는 RT 전체.
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.targetTexture = rt;
                var urp = UiKit.Ensure<UniversalAdditionalCameraData>(camGo); urp.renderType = CameraRenderType.Base; urp.renderPostProcessing = false;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = Mathf.Clamp(1f, cam.nearClipPlane + 0.01f, cam.farClipPlane - 0.01f);
                // T58: 카메라 모드 캔버스는 월드 스프라이트(sortingOrder ≤ 350 · Fx)와 같은 «Default» 층에서 order 로 겨루므로 촬영 중엔 맨 위로(원래 10 · 되돌린다).
                canvas.sortingOrder = short.MaxValue;
                Canvas.ForceUpdateCanvases();
                if (app.Frame != null)
                {
                    var px = FramePixelRect(app.Frame, cam);
                    LastFrameFill = Mathf.Min(px.width / rt.width, px.height / rt.height);
                    LastFrameInfo = $"{name}: cam.rect={cam.rect} pixelRect={cam.pixelRect} canvas.pixelRect={canvas.pixelRect} scale={canvas.scaleFactor:0.###} frame.rect={app.Frame.rect.size} frame.px={px} fill={LastFrameFill:0.###} screen={Screen.width}x{Screen.height}";
                    Debug.Log("[PlayShot] " + LastFrameInfo);
                }
                cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                png = tex.EncodeToPNG();
            }
            catch (Exception e) { Debug.LogWarning("[PlayShot] 스크린샷(RenderTexture) 실패: " + e.Message); }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCam; canvas.planeDistance = oldPlane; canvas.sortingOrder = oldOrder;
                if (camGo != null) { var c = camGo.GetComponent<Camera>(); if (c != null) { c.enabled = false; c.targetTexture = null; } UnityEngine.Object.Destroy(camGo); }
                if (tex != null) UnityEngine.Object.Destroy(tex);
                if (rt != null) { rt.Release(); UnityEngine.Object.Destroy(rt); }
                Canvas.ForceUpdateCanvases();
            }
            if (png == null) return false;
            bool ok = false;
            foreach (var dir in Dirs(folder))
            {
                try { Directory.CreateDirectory(dir); var p = Path.Combine(dir, name + ".png"); File.WriteAllBytes(p, png); Debug.Log("[PlayShot] 스크린샷 저장: " + p); ok = true; }
                catch (Exception e) { Debug.LogWarning("[PlayShot] 스크린샷 저장 실패(" + dir + "): " + e.Message); }
            }
            return ok;
        }

        /// <summary>프레임 RectTransform 의 네 모서리를 촬영 카메라의 픽셀 좌표(RT 기준)로 — 촬영 직전 «프레임이 RT 를 얼마나 채우는가» 를 잰다(T58).</summary>
        static Rect FramePixelRect(RectTransform frame, Camera cam)
        {
            var c = new Vector3[4]; frame.GetWorldCorners(c);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, c[i]);
                x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        /// <summary>활성 이름표 전부 → {요소 이름: [x,y,w,h]}(프레임 % · 소수 1자리). 팝업이 열려 있으면 <b>팝업 층(Overlay)만</b> 잰다 — 뒤 화면(전투 HUD 등)의 이름표가 섞이면 «인포(책) 버튼» 처럼 같은 이름이 둘이 된다(CI #68 로그). 같은 이름이 둘이면 먼저 만난 것만(경고 1줄).</summary>
        public static Dictionary<string, object> Layout(App app)
        {
            var d = new Dictionary<string, object>();
            if (app == null || app.UiCanvas == null || app.Frame == null) return d;
            Canvas.ForceUpdateCanvases();
            var scope = app.Overlay != null && app.Overlay.IsOpen ? (Component)app.Overlay.Root : app.UiCanvas;
            foreach (var tag in scope.GetComponentsInChildren<UiTag>(false))
            {
                if (tag == null || string.IsNullOrEmpty(tag.Name)) continue;
                var r = tag.Measure(app.Frame); if (r == null) continue;
                if (d.ContainsKey(tag.Name)) { Debug.LogWarning("[PlayShot] 이름표 중복(먼저 것만 씀): " + tag.Name); continue; }
                d[tag.Name] = new List<object> { (double)r[0], (double)r[1], (double)r[2], (double)r[3] };
            }
            return d;
        }

        /// <summary>layout.json = {화면: {요소: [x,y,w,h]}, "_missing": [...], "_meta": {...}} 를 저장 폴더마다 쓴다.</summary>
        public static void WriteLayout(Dictionary<string, object> screens, List<object> missing, string folder = DefaultFolder)
        {
            var root = new Dictionary<string, object>();
            foreach (var kv in screens) root[kv.Key] = kv.Value;
            root["_missing"] = missing ?? new List<object>();
            root["_meta"] = new Dictionary<string, object> { { "frame", new List<object> { (double)UiKit.FrameW, (double)UiKit.FrameH } }, { "shot", new List<object> { (double)ShotW, (double)ShotH } }, { "unity", Application.unityVersion }, { "utc", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") } };
            string json = MiniJson.Serialize(root, true);
            foreach (var dir in Dirs(folder))
            {
                try { Directory.CreateDirectory(dir); var p = Path.Combine(dir, "layout.json"); File.WriteAllText(p, json); Debug.Log("[PlayShot] layout.json 저장: " + p); }
                catch (Exception e) { Debug.LogWarning("[PlayShot] layout.json 저장 실패(" + dir + "): " + e.Message); }
            }
        }
    }
}
