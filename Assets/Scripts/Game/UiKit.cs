using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// uGUI 도우미. 화면은 코드로 세우되 그림은 전부 주인 에셋(GUI Pro-MinimalGame)의 프리팹·스프라이트를 쓴다.
    /// ● 좌표계: 프레임 1080 × 2337 (= 390×844 의 9:19.5 프레임을 GUI Pro 데모 캔버스 폭 1080 으로 맞춘 것).
    ///   GUI Pro 데모 프리팹은 1080 폭 캔버스용이라 그대로 넣으면 크기가 맞는다. 배치는 <see cref="Pct"/>(프레임 %) 로만 한다.
    /// ● GUI Pro 의 글자는 TextMeshPro + 한글 없는 SDF 폰트다 → <see cref="Adopt"/> 가 프리팹을 인스턴스화할 때
    ///   TMP 를 legacy Text(Jua) 로 바꿔 한글이 나오게 한다(크기·색·정렬은 그대로 옮긴다).
    /// </summary>
    public static class UiKit
    {
        public const float FrameW = 1080f, FrameH = 2337f;   // 1080 × (844/390)
        public static Font DefaultFont;
        static Sprite _round, _round8, _circle, _white;
        /// <summary>에디터 «도메인 리로드 끔»(EditorSettings · 플레이 진입 속도) 에서도 정적 상태가 새 판마다 깨끗하게.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _round = _round8 = _circle = _white = null; _staging = null; DefaultFont = null; CharacterRig.TimeScale = 1f; _worldBorders.Clear(); }

        public static Font FontOrBuiltin() => DefaultFont != null ? DefaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        static AssetCatalog Cat => App.I != null ? App.I.Assets : null;

        // ───────────────────────── 기본 스프라이트 (도형이 아니라 마스크·게이지 fill 용) ─────────────────────────
        public static Sprite Round(int r = 12)
        {
            if (r >= 12) return _round ?? (_round = MakeRound(12));
            return _round8 ?? (_round8 = MakeRound(6));
        }
        static Sprite MakeRound(int r)
        {
            int s = r * 2 + 2; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float cx = x < r ? x - r + 0.5f : x >= s - r ? x - (s - r) + 0.5f : 0;
                float cy = y < r ? y - r + 0.5f : y >= s - r ? y - (s - r) + 0.5f : 0;
                float d = Mathf.Sqrt(cx * cx + cy * cy);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }
        public static Sprite Circle()
        {
            if (_circle != null) return _circle;
            int s = 64; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - s / 2f) * (x + 0.5f - s / 2f) + (y + 0.5f - s / 2f) * (y + 0.5f - s / 2f));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(s / 2f - d + 0.5f)));
            }
            tex.Apply();
            return _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100);
        }
        public static Sprite White()
        {
            if (_white != null) return _white;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false); var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white; tex.SetPixels(px); tex.Apply();
            return _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100);
        }

        // ───────────────────────── 캔버스 · 프레임 · 배치 ─────────────────────────
        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var isType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (isType != null) go.AddComponent(isType); else go.AddComponent<StandaloneInputModule>();
        }

        public static Canvas CreateRootCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(FrameW, FrameH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;   // 9:16 폰이든 9:19.5 폰이든 프레임이 통째로 들어온다
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        /// <summary>
        /// T106 — 루트 캔버스 바로 아래 «SafeArea» 사각형(노치·펀치홀을 피한 영역 · <see cref="SafeAreaRoot"/> 가 매 프레임 앵커를 맞춘다).
        /// 화면 UI(<see cref="CreateFrame"/>)는 전부 이 안에 만든다. safeArea 가 화면 전체인 곳(데스크톱·WebGL·에디터)에서는 앵커 0~1 이라 배치가 안 바뀐다.
        /// </summary>
        public static RectTransform CreateSafeArea(Transform canvas)
        {
            var rt = Rect(canvas, "SafeArea");
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
            Ensure<SafeAreaRoot>(rt.gameObject);
            return rt;
        }

        /// <summary>
        /// T182 3단계 — 지금 서 있는 프레임(<see cref="CreateFrame"/> 이 마지막으로 세운 것). «프레임 칸(frame space)» 판정과 세로 신축 배수를 여기서 뽑는다.
        /// </summary>
        public static RectTransform Frame { get; private set; }
        /// <summary>
        /// T182 3단계 — 세로 신축 배수를 <b>일부러 주입</b>하는 자리(자 전용 · null 이면 프레임 실제 크기에서 뽑는다).
        /// 3단계-2 는 «배선» 만 하고 프레임은 아직 9:19.5 고정이라 <see cref="FrameK"/> 가 늘 1 이다 — 그러면 배선이 맞는지 <b>잴 방법이 없다</b>.
        /// 그래서 자가 «9:21 짜리 프레임» 을 흉내 내 배선을 미리 확인한다(3단계-3 이 <see cref="CreateFrame"/> 을 바꾸면 이 주입 없이 그대로 돈다).
        /// </summary>
        public static float? FrameKOverride;
        /// <summary>세로 신축 배수 — 1 이면 <see cref="Stretch"/> 의 모든 함수가 항등이라 배치가 지금과 한 치도 다르지 않다.</summary>
        public static float FrameK
        {
            get
            {
                if (FrameKOverride.HasValue) return FrameKOverride.Value;
                if (Frame == null) return 1f;
                var r = Frame.rect;
                return Core.Stretch.K(r.width, r.height);
            }
        }
        /// <summary>
        /// 이 부모가 «프레임 칸» 인가 — 프레임 자신이거나, 프레임까지 <b>전부 꽉 채운(anchor 0~1)</b> 겹으로만 이어져 있는가.
        /// <para>
        /// 세로 신축은 <b>프레임 좌표계의 줄</b>에만 건다. 팝업 상자 안·목록 칸 안의 %는 «그 상자» 기준이라 그대로 두어야 한다 —
        /// 상자 자체가 신축된 자리에 서면 그 안은 따라 움직인다(두 번 걸면 두 배로 밀린다).
        /// 꽉 채운 겹(<see cref="Stretch(RectTransform, float, float, float, float)"/> · <c>Overlay.Root</c> 같은 층)은 프레임과 같은 사각형이라 프레임 칸으로 센다.
        /// </para>
        /// </summary>
        public static bool FrameSpace(Transform t)
        {
            if (Frame == null) return false;
            for (int i = 0; i < 16 && t != null; i++)
            {
                if (t == Frame) return true;
                var rt = t as RectTransform;
                if (rt == null) return false;
                if (rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one) return false;   // 꽉 채운 겹이 아니면 제 좌표계다
                t = t.parent;
            }
            return false;
        }

        /// <summary>프레임 — index.html #frame. 화면 가운데 · 최대 크기로 letterbox. 비율은 <see cref="FrameFit"/> 이 화면에 맞춰 잡는다(기준 9:19.5 ~ 상한 9:21 · T182 3단계-3).</summary>
        public static RectTransform CreateFrame(Transform canvas)
        {
            var rt = Rect(canvas, "Frame");
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(FrameW, FrameH);
            var arf = rt.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent; arf.aspectRatio = FrameW / FrameH;
            var img = rt.gameObject.AddComponent<Image>(); img.color = Palette.Bg; img.raycastTarget = true;
            Frame = rt;   // T182 3단계 — 프레임 칸 판정·세로 신축 배수의 기준
            FrameFit.Attach(rt);   // T182 3단계-3 — 화면이 기준보다 길쭉하면(9:21 까지) 프레임이 그만큼 길어진다(납작한 화면은 기준 그대로)
            return rt;
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>
        /// 팝업 어둠이 프레임 «밖»(레터박스 · 노치 띠 · T106 이 화면 끝까지 뻗어 놓은 상단·하단 프레임 띠)까지 덮는 여유(px · T104).
        /// 어둠은 <see cref="CreateFrame"/> 안에 있어 Stretch 만 하면 프레임 사각형까지만 덮는데, 상단·하단 띠는 <see cref="TopBar.FrameOverscan"/> 만큼 그 밖으로 뻗어 있어
        /// 팝업이 떠도 그 띠만 밝게 남는다(레퍼런스 12 는 재화 바·탭 바까지 전부 어둡다). 같은 값이라 띠와 정확히 같은 범위를 덮는다.
        /// </summary>
        public const float DimOverscan = TopBar.FrameOverscan;
        /// <summary>
        /// 팝업 뒤 어둠의 알파(T199 · 주인 레퍼런스에 맞춘 실측값). <b>0.85 → 0.985</b> 로 올렸다.
        /// <para>
        /// 까닭 — 이 프로젝트는 <b>Linear 색공간</b>이라 UI 겹침도 선형에서 섞인다. 그래서 «α 0.85» 는 사람 눈으로는 α 0.85 만큼 어둡지 않다:
        /// 흰 픽셀(선형 1.0)이 <c>0.15 × 1.0 + 0.85 × 선형(#12131A ≈ 0.0059)</c> = 0.155 선형 → <b>sRGB 0.43</b> 이 된다.
        /// 워커 H 가 `screens` run 385 에서 잰 값이 정확히 그것이다(팝업이 덮은 열한 화면 전부 최대 <b>0.432</b> · 레퍼런스는 <b>0.141</b> · 결정 512).
        /// </para>
        /// 값을 고르는 셈도 같은 식이다 — 남는 밝기는 거의 전부 <c>(1−α) × 흰색</c> 항이므로 <b>α 만이 지렛대</b>다
        /// (<see cref="Palette.Dim"/> 을 순수 검정으로 바꿔도 α 0.85 에서는 0.42 라 소용이 없다).
        /// α 0.985 → 0.015 + 0.985×0.0059 = 0.0209 선형 → <b>sRGB ≈ 0.16</b> 으로 레퍼런스(0.141) 옆에 선다.
        /// <b>한 회차에 이 변수 하나만</b> 움직이고 매번 다시 잰다(등재 5항) — 자는 <see cref="Assets"/> 밖
        /// `DimDarknessTests`(어둠 위 상단 띠의 가장 밝은 픽셀 ≤ 0.20)가 지킨다.
        /// </summary>
        public const float DimAlpha = 0.985f;
        public static void Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        /// <summary>
        /// 부모 % 배치 — x,y = 왼쪽·위 모서리(%), w,h = 폭·높이(%). ref-layout.md 표를 그대로 넣는다.
        /// <para>
        /// T182 3단계 — 부모가 «프레임 칸»(<see cref="FrameSpace"/>)이면 세로 두 값이 <see cref="Core.Stretch.MapRow"/> 를 지난다:
        /// 위·아래 띠는 픽셀을 지키고 가운데가 남는 높이를 먹는다. <see cref="FrameK"/> 가 1 이면 <b>항등</b>이라 지금 화면은 한 치도 안 바뀐다.
        /// 가로(x·w)는 안 건드린다 — 폭은 2단계(상한 + 좌우 띠)가 이미 «기준 폭 그대로» 로 맞춰 놓았다.
        /// </para>
        /// </summary>
        public static void Pct(RectTransform rt, float x, float y, float w, float h)
        {
            float k = FrameK;
            if (k > 1f && rt != null && FrameSpace(rt.parent)) Core.Stretch.MapRow(y, h, k, out y, out h);
            rt.anchorMin = new Vector2(x / 100f, 1f - (y + h) / 100f);
            rt.anchorMax = new Vector2((x + w) / 100f, 1f - y / 100f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
        }
        public static void Pct(RectTransform rt, Layout.R r) => Pct(rt, r.X, r.Y, r.W, r.H);

        /// <summary>프레임 좌상 기준 px(1080×2337) 로 고정 크기 배치 — 프리팹 고유 크기를 그대로 둘 때.</summary>
        public static void Px(RectTransform rt, float cx, float cy, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(cx, -cy); rt.sizeDelta = new Vector2(w, h); rt.localScale = Vector3.one;
        }
        /// <summary>표의 % 사각형 → 프레임 px 크기(폭·높이). 프리팹 조각을 «본래 크기 그대로 두고 배율로» 넣을 때의 목표 크기(<see cref="FitScale"/>).</summary>
        public static Vector2 PxSize(Layout.R r) => new Vector2(r.W / 100f * FrameW, r.H / 100f * FrameH);
        /// <summary>
        /// 프리팹 조각을 <b>본래 sizeDelta 그대로</b> 두고 부모 한가운데에 균일 배율로 맞춘다(<see cref="PerkFrame"/> 규약 · T13/T34) — 내부 자식이 고정 크기라 sizeDelta 를 줄이면 그림이 안 줄어드는 GUI Pro 조각용.
        /// target = 들어갈 px 크기(보통 <see cref="PxSize"/>) · fill = 그 안에서 차지할 비율. 조각 크기를 모르면(빈 Rect) 아무것도 안 한다.
        /// </summary>
        public static void FitScale(RectTransform piece, Vector2 target, float fill = 1f)
        {
            piece.anchorMin = piece.anchorMax = new Vector2(0.5f, 0.5f); piece.pivot = new Vector2(0.5f, 0.5f); piece.anchoredPosition = Vector2.zero;
            var sz = piece.sizeDelta; if (sz.x <= 1f || sz.y <= 1f || target.x <= 0f || target.y <= 0f) return;
            float s = Mathf.Min(target.x / sz.x, target.y / sz.y) * fill;
            piece.localScale = new Vector3(s, s, 1f);
        }

        // ───────────────────────── 코드 생성 위젯 (글자 · 아이콘 · 게이지) ─────────────────────────
        public static Image Icon(Transform parent, string name, string spriteKey, Color? tint = null)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Cat != null ? Cat.Sprite(spriteKey) : null; img.preserveAspect = true; img.raycastTarget = false;
            if (tint.HasValue) img.color = tint.Value;
            return img;
        }
        /// <summary>9-slice 스프라이트 패널(카탈로그 키). 색은 GUI Pro 팔레트에서.</summary>
        public static Image Panel(Transform parent, string name, string spriteKey, Color color)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Cat != null ? Cat.Sprite(spriteKey) : null; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// 글자 하나. <paramref name="size"/> 가 종류(<paramref name="kind"/>) 하한(<see cref="TextSize"/> · T63 · 본문 40 · 버튼 44 · 보조 36 · 제목 60)보다 작으면 경고 없이 하한으로 올린다.
        /// 정말 작아야 하는 곳(아이콘 위 «+1» 배지 등)만 <see cref="TextKind.Small"/> 을 명시한다(= 지시서의 allowSmall:true). bestFit 최소는 <see cref="TextSize.BestFitMin"/>(32) 아래로 못 내려간다.
        /// </summary>
        /// <param name="outline">
        /// <b>더 이상 아무 일도 하지 않는다(T63-outline · 주인 04:4X «모든 글자들 다 검정 아웃라인»).</b> 아웃라인은 이제 <see cref="EnsureOutline"/> 가 무조건 붙인다.
        /// 인자를 남겨 둔 이유는 이 자리를 <b>위치 인자로</b> 넘기는 호출부가 40여 곳이고 그 파일 대부분이 다른 워커의 살아 있는 lock 안이기 때문이다(결정 227) —
        /// 지우면 그 파일들을 다 고쳐야 해서 규약(«같은 파일은 뒤 번호가 기다린다»)에 걸린다. lock 이 풀리는 대로 화면 워커가 자기 커밋에서 인자를 지우면 된다.
        /// </param>
        public static TMP_Text Text(Transform parent, string s, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool bestFit = false, bool outline = true, TextKind kind = TextKind.Body)
        {
            size = TextSize.Floor(size, kind);
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = TmpFont.Get(); t.text = TextGlyphs.Safe(s); t.fontSize = size; t.color = color; t.alignment = UiKit.TmpAlign(anchor);
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false; t.richText = true;
            if (bestFit) { t.enableAutoSizing = true; t.fontSizeMin = TextSize.BestFitFloor(12, kind); t.fontSizeMax = size; t.overflowMode = TextOverflowModes.Truncate; }
            EnsureOutline(t);
            TextAudit.Mark(t, kind);
            return t;
        }

        /// <summary>검은 아웃라인 색(<see cref="EnsureOutline"/>) — Ink 계열 · α <see cref="OutlineAlpha"/>. <b>α 는 리터럴로 적지 않는다</b>(둘이 어긋나면 자가 색을 어긋남으로 센다).</summary>
        public static readonly Color OutlineColor = new Color(0.1f, 0.06f, 0.05f, OutlineAlpha);
        /// <summary>
        /// 아웃라인 α(게이트가 이 값으로 단언한다).
        /// <para>
        /// <b>T194 회차 2(2026-09-07 15:4X · 결정 507) — 0.85 에서 1 로.</b> 회차 1(비율 0.05 → 0.08)을 `screens` <b>run 382</b> 로 실측하니
        /// 띠는 실제로 두꺼워졌는데(어두운 픽셀 수 <b>5.4배</b> · 프레임 두께 중앙값 2px → <b>4px</b>) <b>레퍼런스의 «순수 검정» 에는 여전히 못 갔다</b>(우리 가장 어두운 픽셀 0.269 ↔ 레퍼런스 <b>0.000</b>).
        /// 까닭은 두께가 아니라 <b>α 가 바닥을 만든다</b>는 것이다 — 노란 리본(휘도 0.815) 위에서 <b>완전히 덮인</b> 테 픽셀조차
        /// <c>0.85 × 0.071 + 0.15 × 0.815 = <b>0.182</b></c> 아래로 못 내려간다. 즉 α 0.85 인 한 레퍼런스의 검정은 <b>셈으로 도달 불가</b>다.
        /// α 1 이면 같은 자리가 <b>0.071</b> 이 된다.
        /// </para>
        /// <b>주인 지시는 «모든 글자들 다 <u>검정</u> 아웃라인»</b>(T63-outline)이고 0.85 는 워커가 고른 값이었다 — 1 로 두는 쪽이 그 말에 더 가깝다.
        /// 어두운 판 위에서는 판과 테가 둘 다 어두워 눈에 차이가 없고, 밝은 판 위에서만 달라진다(그 자리가 이 작업이 부른 자리다).
        /// <b>색·α 를 고치면 자(<see cref="TextAudit"/>)와 <c>DailyGiftLookTests</c> 가 저절로 따라온다</b> — 둘 다 <see cref="OutlineColor"/> 를 그대로 읽는다.
        /// </summary>
        public const float OutlineAlpha = 1f;
        /// <summary>
        /// <b>T221 — 여기 있던 «두께 규칙»(<c>OutlineRatio</c> · <c>OutlineMinPx</c> · <c>OutlineMaxPx</c> · <c>OutlineWidth(size)</c>)을 걷었다.</b>
        /// <para>
        /// T207 ② 가 테를 <b>SDF 머티리얼</b>로 옮긴 뒤(주인 «tmpro로 아웃라인 해야지 진짜 메테리얼로»)
        /// 그 넷은 <b>아무 픽셀도 안 그린다</b> — 두께는 <see cref="TmpFont.OutlineWidth"/>(SDF 비율 0~1) 한 값이 정하고
        /// 글자 크기에 저절로 비례한다. 남은 소비자는 그것을 지키던 자 하나뿐이었다(즉 «자기 자신을 지키는 규칙»).
        /// </para>
        /// <b>살아 있는 것은 색뿐이다</b> — <see cref="OutlineColor"/> 가 <see cref="TmpFont.SetOutline"/> 로 들어간다.
        /// 두께를 손보려는 다음 워커는 <see cref="TmpFont.OutlineWidth"/> 를 본다(T194 가 세 회차 태운 «규격 px ≠ 화면 px» 은 그 방식에는 없다 · 결정 595).
        /// </summary>

        /// <summary>
        /// 글자에 검은 아웃라인을 <b>무조건</b> 붙인다 — 주인 지시 2026-09-07 04:4X «모든 글자들 다 검정 아웃라인 있는 것으로 · 지금 어떤 건 있고 어떤 건 없고 그러네»(T63-outline).
        /// 전에는 ⓐ <c>UiKit.Text(outline:)</c> 인자로 끌 수 있었고 ⓑ <see cref="ConvertTmp"/> 는 «TMP 머티리얼 이름에 Outline 이 있거나 글자가 밝을 때만» 붙였고
        /// ⓒ <see cref="SetText"/>·<see cref="Button"/>·<see cref="Bar.Set"/> 로 들어온 프리팹 글자에는 아무도 안 붙여서 화면마다 있고 없고가 섞였다.
        /// 이제 <b>글자 입구 다섯 곳이 전부 이 한 곳</b>을 거치므로 «어떤 건 없다» 가 구조적으로 불가능하다(결정 226).
        /// 이미 있으면 컴포넌트를 또 만들지 않고 값만 갱신한다(중복 <see cref="Outline"/> 은 그림자가 겹쳐 두꺼워 보인다).
        /// 크기를 안 주면 글자의 현재 <c>fontSize</c>(bestFit 이면 최대 크기)로 잡는다.
        /// </summary>
        /// <summary>
        /// 글자색 «밝음» 기준(상대 휘도 0~1 · 이 값 미만이면 <see cref="EnsureBright"/> 가 흰색으로 올린다) — T111 ⓑ(주인 2026-09-07 07:5X
        /// «모든 글씨 중에 검정 글씨 → 흰 글씨로 바꿔야 함 · 검정 아웃라인으로 통일시켰기 때문에»).
        /// 0.45 = 잉크 계열(Ink 0.13 · InkSoft 0.28 · InkLight 0.41 · Dim 0.08 · Slate 0.32)만 걸리고
        /// 우리 색 코딩(회색 0.62 · 갈색 0.54 · 초록 0.67 · 하늘 0.55 · 주황·노랑·빨강·자수정 0.53~0.80)은 그대로 남는 선이다(결정 250).
        /// </summary>
        public const float TextLumaMin = 0.45f;
        /// <summary>상대 휘도(0.299R + 0.587G + 0.114B) — 게이트(<see cref="TextAudit"/>)도 같은 식을 쓴다.</summary>
        public static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
        /// <summary>어두운 글자색을 흰색으로 올린 값(알파는 그대로) — 밝으면 그대로 돌려준다.</summary>
        public static Color BrightText(Color c) => Luma(c) >= TextLumaMin ? c : new Color(1f, 1f, 1f, c.a);
        /// <summary>
        /// 어두운 글자를 흰 글자로(T111 ⓑ) — <see cref="EnsureOutline"/> 안에서 부르므로 <b>글자 입구 다섯 곳</b>(Text·SetText·Button·ConvertTmp·Bar.Set)과
        /// <see cref="Adopt"/>(조각의 uGUI Text)가 전부 자동으로 받는다. 화면 파일을 한 줄도 안 고치므로 남의 lock 을 침범하지 않는다(결정 250).
        /// 리치 텍스트의 <c>&lt;color=…&gt;</c> 조각(등급색·수치 색)은 <c>Text.color</c> 가 아니라 태그가 정하므로 그대로 남는다.
        /// </summary>
        public static void EnsureBright(TMP_Text t)
        {
            if (t == null) return;
            // T177 — 주인이 색을 못 박은 자리(잠긴 옵션 #666666)는 이 규칙 밖이다. 표식은 DarkText 한 곳에서만 붙인다.
            if (t.GetComponent<OwnerDarkTextTag>() != null) return;
            t.color = BrightText(t.color);
        }

        /// <summary>
        /// 주인이 «어두운 글자» 로 못 박은 자리(T177 · 장비 세부 07 의 잠긴 옵션 줄) — <see cref="OwnerDarkTextTag"/> 를 붙이고 색을 넣는다.
        /// 표식을 붙인 «뒤» 에 색을 다시 넣는 까닭 = <see cref="Label"/>·<see cref="Text"/> 가 만들면서 이미 <see cref="EnsureBright"/>(T111 ⓑ)를 거쳐 흰색이 됐기 때문이다.
        /// 이 함수를 새 자리에 쓸 때는 «어느 주인 지시인가» 를 그 줄에 같이 적는다 — 안 그러면 다음 워커가 «가독성» 이라며 되돌린다.
        /// </summary>
        public static TMP_Text DarkText(TMP_Text t, Color c)
        {
            if (t == null) return null;
            Ensure<OwnerDarkTextTag>(t.gameObject);
            t.color = c;
            return t;
        }

        public static bool EnsureOutline(TMP_Text t, float size = 0f)
        {
            if (t == null) return false;
            EnsureBright(t);   // T111 ⓑ — 아웃라인과 글자색은 짝이다(검은 아웃라인 + 밝은 글자) · 입구 다섯 곳이 전부 이 함수를 거친다
            // T207 ② — 주인이 말한 «진짜 메테리얼» 아웃라인으로 갈아탔다(2026-09-07 17:2X «tmpro로 아웃라인 해야지 진짜 메테리얼로»).
            //  ⓐ 사본을 밀어 겹치던 컴포넌트(uGUI `Outline` 네 장)를 **걷어 낸다** —
            //     SDF 셰이더가 테를 «거리장을 부풀려» 그리므로 정점이 안 늘고, 마름모꼴도 «없던 구멍» 도 원리적으로 안 생긴다(T204 2항).
            //  ⓑ 두께는 **글자 크기에 저절로 비례**한다(SDF 비율 0~1 · 픽셀이 아니다) — T194 가 세 회차 태운
            //     «규격 px ≠ 화면 px» 함정이 이 방식에는 없다. 그래서 `size` 인자는 이제 안 쓴다(호출부는 그대로 둔다).
            //  ⓒ 색·두께는 **폰트 애셋의 공유 머티리얼 한 장**에 건다 — 자리마다 인스턴스를 만들면 배칭이 깨진다(T153 이 shine 에서 겪은 결).
            // uGUI `Outline` 은 유니티 내장이라 조각이 직렬화해 달고 올 수 있다 — 그래서 이 갈래는 남긴다.
            // (T204 의 `TextOutline8` 갈래는 T232 에서 걷었다: 그 컴포넌트를 붙이던 코드가 T207 ② 에서 사라지고
            //  직렬화 참조도 0 이라 «있을 수 없는 것» 을 지키고 있었다.)
            for (var i = 0; i < 2; i++)
            {
                var old4 = t.GetComponent<Outline>(); if (old4 != null) UnityEngine.Object.DestroyImmediate(old4);
            }
            var asset = TmpFont.Get();
            if (asset == null) return false;
            if (t.font != asset) t.font = asset;
            if (t.fontSharedMaterial != asset.material) t.fontSharedMaterial = asset.material;
            return TmpFont.SetOutline(asset, OutlineColor);
        }
        /// <param name="outline">아무 일도 하지 않는다 — <see cref="Text"/> 의 같은 인자 설명 참조(T63-outline · 결정 227).</param>
        public static TMP_Text Label(Transform parent, float x, float y, float w, float h, string s, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool bestFit = true, bool outline = true, TextKind kind = TextKind.Body)
        {
            var t = Text(parent, s, size, color, anchor, bestFit, true, kind);
            Pct(t.rectTransform, x, y, w, h);
            return t;
        }

        /// <summary>가로 게이지 — GUI Pro Slider_02 프리팹(카탈로그 키 ui.slider*) 을 쓰고 값은 Slider 컴포넌트로 넣는다.</summary>
        public sealed class Bar
        {
            public RectTransform Root; public Slider Slider; public TMP_Text Txt; public Image Cap;
            public void Set(double frac, string txt) { if (Slider != null) Slider.value = Mathf.Clamp01((float)frac); if (Txt != null) { Txt.text = TextGlyphs.Safe(txt); EnsureOutline(Txt); } }
        }
        public static Bar MakeBar(Transform parent, string sliderKey, string capIconKey = null)
        {
            var go = Spawn(sliderKey, parent);
            var bar = new Bar { Root = (RectTransform)go.transform, Slider = go.GetComponentInChildren<Slider>(true) };
            if (bar.Slider != null) { bar.Slider.interactable = false; bar.Slider.transition = Selectable.Transition.None; bar.Slider.minValue = 0; bar.Slider.maxValue = 1; foreach (var g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false; }
            bar.Txt = go.GetComponentInChildren<TMP_Text>(true);
            if (bar.Txt != null) { bar.Txt.enableAutoSizing = true; bar.Txt.fontSizeMin = TextSize.BestFitMin; bar.Txt.fontSizeMax = TextSize.Body; bar.Txt.textWrappingMode = TextWrappingModes.NoWrap; }
            if (!string.IsNullOrEmpty(capIconKey))
            {
                bar.Cap = Icon(go.transform, "Cap", capIconKey);
                var rt = bar.Cap.rectTransform; rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(58, 58); rt.anchoredPosition = new Vector2(2, 2);
            }
            return bar;
        }

        // ───────────────────────── 테두리(Border · T69 · 주인 2026-09-06 «행·카드·칸마다 검은 아웃라인») ─────────────────────────
        // 재료 = GUI Pro BasicFrame_Rectangle_01~04_White_Border1/2/3(26×26 · 9-slice border 13 · 흰 조각이라 Ink 로 tint 하면 검은 아웃라인). 코드 도형 0.
        // 조각의 선 굵기는 실측(alpha 행): Border1 4px · Border2 5px · Border3 5px · InnerBorder1_Px7 7px · R0_Border_Px5 5px — 폰(412css px · 프레임 1px ≈ 0.38css px)에서 3px 이상 보이려면
        // 프레임 기준 8px 이상이어야 하므로 pixelsPerUnitMultiplier(= 원본 선 ÷ 목표 선 · < 1 이면 두꺼워진다)로 올린다. 배치 표(ref-layout)는 불변 — 테두리는 칸 «안쪽» 에 그린다.
        /// <summary>테두리 선의 최소 굵기(프레임 px · 폰 ≈ 3px). ROUTINE T69 3항.</summary>
        public const float BorderPx = 8f;
        /// <summary>테두리 색 알파(Ink) — 게이트 하한 0.8 위(ROUTINE T69 5항).</summary>
        public const float BorderAlpha = 0.9f;
        /// <summary>테두리 오브젝트 이름(고정 · 테스트·이름표·<see cref="HasDarkBorder"/> 가 찾는다).</summary>
        public const string BorderName = "Border";
        /// <summary>기본 테두리 조각(굵은 Border3) — 작은 칸(아이콘)은 <see cref="BorderKeySmall"/> · 캡슐(pill)은 <see cref="BorderKeyPill"/>.</summary>
        public const string BorderKey = "fr.rectBorder3", BorderKeySmall = "fr.rectBorder2";
        /// <summary>
        /// 캡슐(pill) 칸 테두리 조각(T69-lobby · 결정 149 가 남긴 «둥근 pill 에 사각 링이 어긋난다» 를 닫는다) — <c>BasicFrame_Rectangle_05_White_Border</c>(87×39 · 9-slice border 44/20/43/19).
        /// 가운데 슬라이스가 0px 이라 가로로 늘리면 위·아래 선만 이어지는 «캡슐» 이 된다(pill 바탕 <c>ResourceBar_Bg</c> 31×31 · border 16/16/15/15 와 같은 방식).
        /// 쓰는 곳: 상단 재화 pill(골드·보석 · <see cref="TopBar"/>) · 전투 HUD pill 2개(처치 수·이번 판 골드).
        /// </summary>
        public const string BorderKeyPill = "fr.pillBorder";
        public static Color BorderInk => Palette.A(Palette.Ink, BorderAlpha);
        /// <summary>조각의 원본 선 굵기(px · 26×26 스프라이트 실측). 모르는 키는 5.</summary>
        public static float BorderNativePx(string key)
        {
            switch (key)
            {
                case "fr.rectBorder": return 4f;
                case "fr.rectInner7": return 7f;
                case BorderKeyPill: return 7f;
                default: return 5f;
            }
        }
        /// <summary>선이 <paramref name="thicknessPx"/> 이상 보이게 하는 Image.pixelsPerUnitMultiplier(1 이 원본 · 작을수록 두껍다 · 1 을 넘기지 않는다 = 원본보다 얇게는 안 만든다).</summary>
        public static float BorderMultiplier(string key, float thicknessPx = BorderPx) => Mathf.Min(1f, BorderNativePx(key) / Mathf.Max(1f, thicknessPx));

        /// <summary>
        /// 칸 하나에 «검은 아웃라인»(T69) — cell 의 맨 앞에 <see cref="BorderName"/> Image(9-slice · <paramref name="borderKey"/> · tint 기본 = <see cref="BorderInk"/> · raycast 끔 · 가운데 비움)를 Stretch 로 덧댄다.
        /// <paramref name="bg"/> 를 주면 맨 뒤에 같은 모양의 바탕(fr.rect · 그 색)도 깐다(칸에 배경이 없을 때만 — 이미 프레임/배경이 있는 조각은 null 로 두고 테두리만). <paramref name="inset"/> = 칸 안쪽으로 들어가는 px(양수).
        /// 이미 있으면 새로 만들지 않고 그 Border 를 갱신한다. 선 굵기는 <see cref="BorderMultiplier"/> 로 프레임 <paramref name="thicknessPx"/>(기본 8) 이상. 아이콘·글자를 테두리 위에 두려면 호출 뒤 그 자식을 <c>SetAsLastSibling</c>.
        /// </summary>
        public static Image Bordered(RectTransform cell, string borderKey = BorderKey, Color? tint = null, float inset = 0f, Color? bg = null, float thicknessPx = BorderPx)
        {
            if (cell == null) return null;
            if (bg.HasValue)
            {
                Image bgImg = null;
                for (int i = 0; i < cell.childCount; i++) if (cell.GetChild(i).name == BorderName + "Bg") { bgImg = cell.GetChild(i).GetComponent<Image>(); break; }
                if (bgImg == null) bgImg = Panel(cell, BorderName + "Bg", "fr.rect", bg.Value); else bgImg.color = bg.Value;
                Stretch(bgImg.rectTransform, inset, inset, inset, inset); bgImg.pixelsPerUnitMultiplier = BorderMultiplier(borderKey, thicknessPx); bgImg.raycastTarget = false;
                bgImg.transform.SetAsFirstSibling();
            }
            Image img = null;
            for (int i = 0; i < cell.childCount; i++) if (cell.GetChild(i).name == BorderName) { img = cell.GetChild(i).GetComponent<Image>(); break; }
            if (img == null) img = Panel(cell, BorderName, borderKey, tint ?? BorderInk);
            else { img.sprite = Cat != null ? Cat.Sprite(borderKey) : img.sprite; img.type = Image.Type.Sliced; img.color = tint ?? BorderInk; }
            Stretch(img.rectTransform, inset, inset, inset, inset);
            img.pixelsPerUnitMultiplier = BorderMultiplier(borderKey, thicknessPx); img.fillCenter = false; img.raycastTarget = false;
            img.transform.SetAsLastSibling();
            return img;
        }

        /// <summary>
        /// 조각이 <b>제 «Border» 자식을 이미 가진</b> 프레임(특전 카드 <c>CardFrame_04</c> 의 «Border»·«TitleBorder» 처럼)의 링을 «검은 아웃라인» 으로(T69 1항 «이미 프레임이 있는 조각은 그 프레임의 테두리를 어둡게 tint») —
        /// 새 Image 를 덧대지 않고 <paramref name="names"/> 에 든 이름의 Image 를 <see cref="BorderInk"/> 로 칠하고 9-slice 선을 프레임 <see cref="BorderPx"/>(8px · 폰 3px) 이상으로 굵힌다(<see cref="GearUi.DarkFrame"/> 이 ItemFrame 에 하는 것과 같은 방식).
        /// 조각이 <paramref name="scale"/> 로 축소돼 있으면 그만큼 더 굵게(화면에서 같은 8px). 안쪽 밝은 선(«InnerBorder»)은 이름을 안 주면 안 건드린다. 돌려주는 값 = 칠한 개수(0 이면 조각 구성이 바뀐 것).
        /// </summary>
        public static int InkFrameBorders(Transform frame, float nativePx = 5f, float scale = 1f, params string[] names)
            => InkFrameBordersFilled(frame, false, nativePx, scale, names);
        /// <summary>
        /// <see cref="InkFrameBorders"/> 와 같되 <b>가운데를 채울지</b>(<paramref name="fillCenter"/>) 를 고른다 —
        /// 주인 지시 «특전 카드의 `TitleBorder` 는 FillCenter 켜짐»(T93 6항 · T155 ⓐ 재지시). 제목 띠는 «링» 이 아니라 «띠» 라
        /// 가운데가 뚫리면 띠 색이 사라진다. 바깥 링(<see cref="BorderName"/>)은 가운데가 비어야 내용이 보이므로 <b>기본값은 그대로 false</b>다
        /// (이 함수는 공용이라 대장간·던전 카드도 쓴다 · 특전 카드에서만 true 를 준다).
        /// </summary>
        public static int InkFrameBordersFilled(Transform frame, bool fillCenter, float nativePx = 5f, float scale = 1f, params string[] names)
        {
            if (frame == null || names == null || names.Length == 0) return 0;
            int n = 0; float px = BorderPx / Mathf.Max(0.05f, scale);
            foreach (var im in frame.GetComponentsInChildren<Image>(true))
            {
                if (im == null || im.sprite == null) continue;
                bool hit = false;
                foreach (var nm in names) if (im.name == nm) { hit = true; break; }
                if (!hit || im.sprite.name.IndexOf("Border", StringComparison.OrdinalIgnoreCase) < 0) continue;
                im.color = BorderInk; im.type = Image.Type.Sliced; im.fillCenter = fillCenter; im.raycastTarget = false;
                im.pixelsPerUnitMultiplier = Mathf.Min(1f, nativePx / Mathf.Max(1f, px));
                n++;
            }
            return n;
        }

        // 월드(SpriteRenderer) 바 — 발밑 2단 바(BattleWorld.MakeBar · T69 8항). 같은 조각을 월드용 Sprite 로 다시 감싼다(pixelsPerUnit 을 «선 = 프레임 8px 에 해당하는 월드 길이» 로 · 텍스처는 주인 것 그대로).
        static readonly Dictionary<string, Sprite> _worldBorders = new Dictionary<string, Sprite>();
        /// <summary>프레임 <see cref="BorderPx"/> 에 해당하는 월드 길이(u) — 프레임 px → 레이아웃 px(× LayoutW/FrameW) → 월드(÷ PPU).</summary>
        public static float WorldBorderLine => WorldLine(BorderPx);
        /// <summary>프레임 <paramref name="thicknessPx"/> 에 해당하는 월드 길이(u).</summary>
        public static float WorldLine(float thicknessPx) => thicknessPx * (WorldCam.LayoutW / FrameW) / WorldCam.PPU;
        /// <summary>
        /// 월드용 테두리 스프라이트(키 + 선 굵기마다 한 번 만들어 재사용 · 9-slice border 그대로 · FullRect).
        /// <paramref name="thicknessPx"/> 는 «그려지는 선» 의 프레임 px — 기본은 전 화면 공용 <see cref="BorderPx"/>(8).
        /// 작은 월드 바처럼 8px 이 칸에 비해 두꺼운 자리는 호출부가 더 얇게 준다(<see cref="BattleWorld.FootBarLinePx"/>).
        /// </summary>
        public static Sprite WorldBorderSprite(string key = BorderKey, float thicknessPx = BorderPx)
        {
            string ck = key + "@" + thicknessPx.ToString("0.###");
            if (_worldBorders.TryGetValue(ck, out var s) && s != null) return s;
            var src = Cat != null ? Cat.Sprite(key) : null; if (src == null) return null;
            float ppu = BorderNativePx(key) / WorldLine(thicknessPx);
            s = Sprite.Create(src.texture, src.rect, new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, src.border);
            s.name = src.name + " (world)";
            _worldBorders[ck] = s;
            return s;
        }
        /// <summary>월드 바(SpriteRenderer) 위에 테두리 한 장 — <paramref name="bar"/> 의 자식 «Border»(Sliced · <paramref name="size"/> = 바 크기 · sortingOrder = <paramref name="order"/> · Ink). 조각이 없으면 null(경고는 카탈로그가).</summary>
        public static SpriteRenderer WorldBorder(Transform bar, Vector2 size, int order, string key = BorderKey, Color? tint = null, float thicknessPx = BorderPx)
        {
            var sp = WorldBorderSprite(key, thicknessPx); if (sp == null || bar == null) return null;
            var go = new GameObject(BorderName); go.transform.SetParent(bar, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.drawMode = SpriteDrawMode.Sliced; sr.size = size; sr.color = tint ?? BorderInk; sr.sortingOrder = order;
            return sr;
        }
        /// <summary>어두운 테두리가 있는가(T69 게이트) — 아래 어딘가에 이름이 «Border» 로 시작하는 활성 Image/SpriteRenderer 가 있고 스프라이트 이름에 Border 가 들어가며 색이 어둡고(밝기 ≤ 0.35) 알파 ≥ 0.8. 프리팹 자체의 Border 조각을 Ink 로 tint 한 경우도 잡힌다.</summary>
        public static bool HasDarkBorder(Transform cell)
        {
            if (cell == null) return false;
            foreach (var im in cell.GetComponentsInChildren<Image>(false))
                if (im != null && im.enabled && im.name.StartsWith(BorderName) && IsDarkBorder(im.sprite, im.color)) return true;
            foreach (var sr in cell.GetComponentsInChildren<SpriteRenderer>(false))
                if (sr != null && sr.enabled && sr.name.StartsWith(BorderName) && IsDarkBorder(sr.sprite, sr.color)) return true;
            return false;
        }
        static bool IsDarkBorder(Sprite sp, Color c)
        {
            if (sp == null || sp.name.IndexOf("Border", StringComparison.OrdinalIgnoreCase) < 0) return false;
            float l = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            return c.a >= 0.8f && l <= 0.35f;
        }

        // ───────────────────────── 질감 3종(T72 · 주인 2026-09-06 «Pattern_01_256 이 거의 모든 UI 에 · 아이콘 뒤 Effect_Light 천천히 회전 · 그라데이션 색감») ─────────────────────────
        // 재료 = GUI Pro Sprite_Common/~Demo/Demo_Image 의 Pattern_01_256(256² · 흰 알파 무늬 · .meta wrapU/V = 0 = Repeat · mipmap 끔) · Effect_Light_01/02_512(흰 빛살) · Gradient_Top_01/02·Gradient_Bottom(폭 4px 흰 알파 세로 띠)
        // + Button_03_White_Gradient(버튼 아래 어둠) · CardFrame_03_White_Gradient(카드 위 밝음). 코드 도형 0 — 색은 전부 tint. 연출 상수는 밸런스가 아니라 여기 한 곳(워커 결정 기록 157~159).
        // 전부 unscaled(팝업 시간 정지 중에도 흐른다) + SetLink(T56 · 대상이 파괴되면 트윈도 죽는다). 화면 적용은 T63/T69 화면 묶음 워커가 같이(한 화면 세 번 만지지 않기).
        /// <summary>질감 조각 이름(고정 · 테스트·감사가 찾는다).</summary>
        public const string PatternName = "Pattern", LightName = "Light", LightMaskName = "LightMask", GradientTopName = "GradientTop", GradientBottomName = "GradientBottom";
        /// <summary>T155 ⓓ — 빛살 아래 겹 글로우 서클의 이름·키·짙기. 이름 «LightMask» 는 T172 로 자르지 않게 됐지만 값은 그대로 둔다(코드·자·문서 열두 자리가 이 이름을 계약으로 쓴다 · 결정 456).</summary>
        public const string GlowName = "Glow", GlowKey = "ui.glow1";
        /// <summary>빛 알갱이 묶음 이름(T174 · 담개 <see cref="LightMaskName"/> 안 · 알갱이는 그 자식 «Dust0»~).</summary>
        public const string DustName = "Dust";
        public const string PatternKey = "ui.pattern", LightKey = "ui.light1", LightKeySmall = "ui.light2", GradTopKey = "ui.gradTop1", GradBottomKey = "ui.gradBottom";
        /// <summary>버튼 아래 어둠(Button_03_White_Gradient · 버튼 모양 9-slice) · 카드 위 밝음(CardFrame_03_White_Gradient) — ③ 그라데이션 3항 우선순위 1·3.</summary>
        public const string BtnGradientKey = "ui.btnGradient", CardGradientKey = "fr.cardGradient3";
        /// <summary>패턴 한 타일이 지나가는 시간(초 · 주인 확정 2026-09-07 «속도 2배» → ROUTINE T72 1항 10~15 · 종전 25).</summary>
        public const float PatternTileSeconds = 12.5f;
        /// <summary>패턴 타일 한 변(프레임 px) — 텍스처 256px 그대로(레퍼런스 01 의 무늬 주기 ≈ 프레임 폭의 1/4).</summary>
        public const float PatternTilePx = 256f;
        /// <summary>패턴 알파 — 주인 확정 2026-09-07 «255 중 3»(= 3/255 ≈ 0.012 · «아주 은은하게» · 종전 0.12 는 레퍼런스보다 진했다). 무늬 픽셀은 알파 255 라 = 그 자리만 1.2% 어둡거나 밝다.</summary>
        public const float PatternAlpha = 3f / 255f;
        /// <summary>밝은 바탕(초록 로비 · 크림 패널) 용 tint = Ink(레퍼런스 01 은 바탕보다 어두운 무늬) · 어두운 바탕(상점 회색 · 팝업 어둠) 용 = White(레퍼런스 09 는 밝은 무늬).</summary>
        /// <summary>
        /// ⚑ 로비(01)만 쓰는 배경 무늬 알파 — 주인이 **두 번** «메인 로비에도 패턴 애니메이션 있어야 하는데 없더라 · 있게 해»(2026-09-07 05:3X · T94 ⓐ) 라고 했다.
        /// 무늬는 이미 깔려 있었지만(T72 6차 `95069e5` · 배포된 빌드에도 들어 있다) 공용 알파 <see cref="PatternAlpha"/> = 3/255 가
        /// 로비의 어두운 초록 바탕에서는 **눈에 안 보인다** — 주인의 «없다» 는 그 뜻으로 본다.
        /// 그래서 **로비에서만** 18/255 로 올린다(다른 화면은 주인이 02:0X 에 확정한 3/255 그대로 · 결정 기록).
        /// </summary>
        /// <para>
        /// ⚑ <b>T166 ⓐ(주인 2026-09-07 09:2X «배경 무늬를 흰색 7/255 로»)</b> — 색과 알파가 <b>둘 다</b> 바뀐다: 잉크 18/255 → <b>흰색 7/255</b>.
        /// 위 T94 ⓐ 는 «안 보인다» 를 알파로 풀었는데, 어두운 초록 바탕에서는 <b>밝은 무늬</b>가 훨씬 적은 알파로도 보인다 — 주인이 그 답을 준 것이다.
        /// 로비만이고 다른 화면은 그대로(공용 <see cref="PatternAlpha"/> 3/255 · 주인이 02:0X 에 확정).
        /// </para>
        public const float PatternAlphaLobby = 7f / 255f;
        /// <summary>로비 배경 무늬 색 — <b>흰 무늬</b>(주인 T166 ⓐ · 어두운 초록 바탕 위라 밝은 쪽이 맞다 · <see cref="PatternAlphaLobby"/>).</summary>
        public static Color PatternTintLobby => Palette.A(Palette.White, PatternAlphaLobby);
        public static Color PatternTintLight => Palette.A(Palette.Ink, PatternAlpha);
        public static Color PatternTintDark => Palette.A(Palette.White, PatternAlpha);
        /// <summary>빛살 한 바퀴(초 · 12~20) · 한 변 = 아이콘 긴 변 × 배(1.6~2.2) · 알파 = 주인 확정 2026-09-07 «255 중 68»(= 68/255 ≈ 0.267 · 종전 0.6 은 아이콘을 덮었다).</summary>
        public const float LightPeriod = 16f, LightScale = 1.9f, LightAlpha = 68f / 255f;
        /// <summary>T155 ⓓ 글로우 서클의 짙기 — 빛살보다 옅다(같은 자리에 겹치므로). T172 가 넣었던 «칸 대비 하한»(`LightOutScale`)은 T189(주인 «다 안으로»)로 **없앴다**.</summary>
        public const float GlowAlpha = 46f / 255f;
        /// <summary>빛 계열이 함께 쓰는 <b>加算(additive)</b> 머티리얼의 이름 — 게이트가 이 이름으로 찾는다(T181 ⓑ).</summary>
        public const string LightMatName = "UiAdditive";
        static Material _lightMat;
        /// <summary>
        /// T181 ⓑ(주인 «glow 같은 거 빛나는 느낌이 잘 안 든다») — 빛살·글로우 서클이 <b>겹칠수록 밝아지게</b> 하는 加算 머티리얼 <b>한 장</b>.
        /// <para>
        /// <b>왜 加算인가</b> — 지금은 보통 알파 블렌딩이라 흰 그림이 «위에 얹힌 흰 판» 으로 보인다(겹쳐도 안 밝아진다).
        /// 加算은 아래 색에 <b>더한다</b> — 그래서 어두운 판 위 빛이 «빛» 으로 읽히고, 두 겹(빛살 + 글로우 서클)이 겹친 가운데가 자연히 더 밝다.
        /// ⓐ 의 Bloom 은 <b>UI 에 안 먹으므로</b>(캔버스가 ScreenSpaceOverlay · PostFx 주석) UI 쪽 «빛나는 느낌» 은 이 길이 유일하다.
        /// </para>
        /// <para>
        /// <b>새 에셋 0</b> — 이미 쓰는 <c>AllIn1SpriteShader/AllIn1SpriteShaderUiMask</c>(<c>mat.perkShine</c> 과 같은 셰이더)를 그대로 쓰고
        /// <b>속성 하나</b>만 바꾼다: <c>_MyDstMode</c> 를 기본 <c>OneMinusSrcAlpha</c>(10) → <c>One</c>(1).
        /// <c>_MySrcMode</c> 는 기본값 <c>SrcAlpha</c>(5) 그대로라 <b>주인이 정한 알파(68/255)가 여전히 세기를 정한다</b> — «One One» 로 두면 알파가 무시돼 하얗게 뜬다.
        /// UiMask 갈래라 <see cref="RectMask2D"/> 잘림(T189 «다 안으로»)도 그대로 받는다.
        /// </para>
        /// 한 장을 <b>모든 빛이 나눠 쓴다</b>(인스턴스 0 · 드로콜·배칭 그대로). 셰이더를 못 찾는 환경이면 <c>null</c> 이고 그러면 지금 그림 그대로다.
        /// </summary>
        public static Material LightMaterial()
        {
            if (_lightMat != null) return _lightMat;
            var sh = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShaderUiMask");
            if (sh == null) return null;
            _lightMat = new Material(sh) { name = LightMatName, hideFlags = HideFlags.DontSave };
            _lightMat.SetFloat("_MyDstMode", (float)UnityEngine.Rendering.BlendMode.One);
            return _lightMat;
        }
        /// <summary>
        /// 빛 알갱이(T174 · 주인 2026-09-07 10:4X «모든 이펙트 라이트 있는 곳에 파티클 이펙트도 넣어 줘 · 빛 알갱이 먼지가 천천히 퍼지는 느낌으로»).
        /// 개수는 <b>칸마다 4</b> — 지시서 4항이 «fps 를 재 보고 정한다» 고 한 자리라 값을 여기 한 곳에 둔다(줄이려면 이 줄만 고친다).
        /// 알갱이 한 변 = 빛살 한 변 × <see cref="DustSizeMul"/> · 퍼져 나가는 거리 = 빛살 한 변 × <see cref="DustDriftMul"/> ·
        /// 한 알갱이가 «났다 사라지는» 데 <see cref="DustPeriod"/> 초(느리게 — 주인 «천천히 퍼지는»).
        /// </summary>
        public const int DustCount = 4;
        public const float DustSizeMul = 0.10f, DustDriftMul = 0.42f, DustPeriod = 5.2f, DustAlpha = 74f / 255f;
        /// <summary>그라데이션 tint — 위 흰 +12% 밝기 · 아래 Ink −18%(ROUTINE T72 3항 팔레트). 화면 «배경» 은 레퍼런스도 이 방향이다(위 밝음 → 아래 어둠 · T116 실측 #3C6833 → #315529).</summary>
        public const float GradientTopAlpha = 0.12f, GradientBottomAlpha = 0.18f;
        /// <summary>
        /// 버튼·팝업 패널·띠의 그라데이션 세기(T116 실측) — 레퍼런스에서 이것들은 <b>사실상 단색</b>이다(주 버튼 #FB9F00 · 로비 배너 #6950C8 · 팝업 패널 #2C2829).
        /// 덧칠이 세면 오히려 레퍼런스에서 멀어져 «칙칙해» 보이므로 배경(0.12/0.18)보다 얕게 깐다.
        /// </summary>
        public const float GradientFlatTopAlpha = 0.08f, GradientFlatBottomAlpha = 0.12f;
        /// <summary>
        /// 카드·타일 그라데이션의 색 세기(T116) — 레퍼런스 카드는 «어두운 위 → 밝은 아래» 의 <b>같은 계열 두 색</b>이라(다이아 #40116D → #AA0CB8)
        /// 무채색 덧칠이 아니라 그 색을 그대로 얹는다. 0.55 = 조각 스프라이트의 부드러운 알파 위에 색이 읽히면서 밑그림(아이콘·글자)이 죽지 않는 세기.
        /// </summary>
        public const float GradientCardAlpha = 0.55f;
        /// <summary>
        /// <b>카드 몸통이 곧 그라데이션인 자리</b>의 세기(T100 ⓓ 회차 2 · 결정 338) — 레퍼런스 10 의 상자 카드는 몸통 전체가 꽉 찬 두 색이라
        /// 덧칠(<see cref="GradientCardAlpha"/>)로는 조각의 회색 바탕이 비쳐 색이 죽는다(실측: 레퍼런스 «Rare» #0182C3 → 우리 #8997A2).
        /// 위·아래 조각이 서로 반대 방향 알파 램프라(<c>ui.gradTop1</c> 흰→투명 · <c>ui.gradBottom</c> 투명→흰) 둘 다 1 이면 몸통이 그 두 색으로 덮인다.
        /// 그림·글자·버튼·테두리는 그 «위» 형제라 그대로 보인다(층 순서 결정 171).
        /// </summary>
        public const float GradientCardSolidAlpha = 1f;
        /// <summary>표에 이름도 바탕색도 없을 때 쓰는 카드 방향 무채색 세기(어두운 위 / 밝은 아래 — 방향만 레퍼런스대로).</summary>
        public const float GradientCardTopAlpha = 0.20f, GradientCardBottomAlpha = 0.14f;
        /// <summary>공통 팝업 상자 안 패턴을 들여 까는 여백(px) — Popup_Box_01~03_White_Bg 의 둥근 모서리 반지름 실측 8px + Bg 자신의 여백 2px(결정 164).</summary>
        public const float PopupPatternInset = 10f;
        /// <summary>
        /// 공통 팝업 제목 리본 크기(px · T75 4항) — 조각 원본은 656×115 인데 그 안 글자 칸이 <c>sizeDelta(−220, −35.5642)</c> 로 들여져 있어 실제 칸이 <b>436×79.4px</b> 였다.
        /// 제목 하한 60 의 한 줄은 <see cref="TextSize.BoxHeight(int, int)"/> = 60×1.4 = <b>84px</b> 라 칸이 4.6px 낮았고, 그래서 모든 팝업 제목이 말없이 <b>56</b> 으로 줄어 그려졌다
        /// (게이트의 «잘림» 판정에는 안 걸리는 쪽 · T63-toast 실측 · 지시서 T75 4항).
        /// 세로를 <b>130</b> 으로 올리면 칸이 94.4px ≥ 84px 이 되어 60 이 그대로 그려진다 — 프레임 높이(2337)로 5.56%(종전 4.92% · ref-layout 표의 리본 행 4.0~4.9% 대비 ±3%p 안).
        /// 가로(656 → 안쪽 436px)는 그대로다: 60 짜리 한글은 한 자 ≈ 60px 라 긴 제목은 <b>폭</b> 때문에 여전히 줄어드는데, 그것은 팝업마다 리본 폭을 넓히는 일이라 §5 비평 회차가 필요하다(T75 행에 남겼다).
        /// </summary>
        public static readonly Vector2 PopupRibbonSize = new Vector2(656f, 130f);

        /// <summary>
        /// 리본 세로를 «글자 칸이 제목 60 의 한 줄(84px)을 담는 높이» 로 맞춘다(T75 4항) — 조각마다 글자 칸 들여쓰기가 달라서 130 이 모든 조각에 통하지 않는다:
        /// <c>Title_01_NoDeco_*</c> 는 세로 inset 35.56(→ 130 이면 칸 94.4 ✔) 이지만 데코가 붙은 조각은 더 들여져 있어 130 으로도 칸이 84 아래로 내려간다.
        /// 그래서 조각의 글자 rect 를 <b>실측</b>해(Stretch 라면 <c>sizeDelta.y</c> 가 음수 inset) 필요한 높이를 계산하고, <see cref="PopupRibbonSize"/> 보다 커야 하면 그만큼 올린다(가로는 안 건드린다).
        /// </summary>
        static void RibbonFit(RectTransform ribbon)
        {
            if (ribbon == null) return;
            var txt = ribbon.GetComponentInChildren<TMP_Text>(true); if (txt == null) return;
            var trt = txt.rectTransform;
            bool stretched = trt.anchorMin.y == 0f && trt.anchorMax.y == 1f;
            float inset = stretched ? Mathf.Max(0f, -trt.sizeDelta.y) : 0f;
            float need = TextSize.BoxHeight(TextSize.Title) + inset;
            if (need > ribbon.sizeDelta.y) ribbon.sizeDelta = new Vector2(ribbon.sizeDelta.x, need);
        }

        /// <summary>
        /// ① 배경 패턴(T72) — <paramref name="host"/> 에 RawImage «Pattern»(Stretch · 텍스처 = ui.pattern · Repeat 타일링 · uvRect 크기 = 사각형 ÷ <paramref name="tilePx"/> · raycast 끔) 을 <paramref name="siblingIndex"/> 자리(기본 0 = host 자신의 배경 Image 바로 위 · 배경이 자식이면 그 다음 index)에 깔고,
        /// uvRect 를 unscaled 로 계속 움직여 무늬가 <b>오른쪽 위로</b> 흐르게 한다(한 타일 <paramref name="tileSeconds"/> 초 · 무한 · Linear). uvRect.position 은 «사각형 왼쪽 아래가 텍스처의 어느 점을 보이나» 라 값이 <b>줄어야</b> 그림이 오른쪽 위로 간다(결정 157 · 지시서의 «(+x,+y)» 는 그림 방향을 말한 것).
        /// 이미 있으면 갱신만(트윈은 다시 시작). 카탈로그에 스프라이트가 없으면 null(경고는 카탈로그가). 어두운 바탕이면 <paramref name="tint"/> = <see cref="PatternTintDark"/>.
        /// <paramref name="inset"/> 은 둥근 모서리 조각(팝업 상자 · 카드) 안쪽으로 들여 까는 여백(px) — 사각형 무늬가 둥근 모서리 밖으로 삐져나오지 않게 한다(T72 2단계).
        /// </summary>
        public static RawImage PatternBg(RectTransform host, Color? tint = null, float tileSeconds = PatternTileSeconds, int siblingIndex = 0, float tilePx = PatternTilePx, float inset = 0f)
        {
            if (host == null) return null;
            var sp = Cat != null ? Cat.Sprite(PatternKey) : null; if (sp == null || sp.texture == null) return null;
            RawImage raw = null;
            for (int i = 0; i < host.childCount; i++) if (host.GetChild(i).name == PatternName) { raw = host.GetChild(i).GetComponent<RawImage>(); break; }
            if (raw == null) { var rt = Rect(host, PatternName); raw = rt.gameObject.AddComponent<RawImage>(); }
            raw.texture = sp.texture; raw.color = tint ?? PatternTintLight; raw.raycastTarget = false;
            Stretch(raw.rectTransform, inset, inset, inset, inset);
            raw.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, Mathf.Max(0, host.childCount - 1)));
            float px = Mathf.Max(1f, tilePx); var rr = raw.rectTransform;
            DOTween.Kill(raw);
            float p = 0f;
            void Apply(float v) { p = v; if (raw != null) raw.uvRect = new Rect(1f - v, 1f - v, Mathf.Max(0.01f, rr.rect.width / px), Mathf.Max(0.01f, rr.rect.height / px)); }
            Apply(0f);
            DOTween.To(() => p, Apply, 1f, Mathf.Max(0.1f, tileSeconds)).SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).SetUpdate(true).SetTarget(raw).SetLink(raw.gameObject);
            return raw;
        }

        /// <summary>
        /// ② 아이콘 뒤 빛살(T72 · <b>T172 로 «칸 안» → «프레임 밖» 으로 뒤집혔다</b>) —
        /// <paramref name="cell"/> 안에 빛 담개(<see cref="LightMaskName"/> · Stretch · <paramref name="inset"/>)를 두고 그 안에
        /// «Glow»(글로우 서클 · 정적) → «Light»(<paramref name="key"/> · 도는 빛살) 두 겹을 넣는다.
        /// <para>
        /// <b>T189(주인 13:2X «그냥 밖으로 하는 거 말고 다 안으로 해라 걍» · T172 통째 취소)</b>: 담개는 다시 <see cref="RectMask2D"/> 다 —
        /// 빛도 글로우 서클도 <b>칸 안에서 끝난다</b>(T72 ② 원문 «프레임 안쪽에서만 보인다» 로 복귀). 한 변은 «아이콘 긴 변 × <paramref name="scale"/>»
        /// (아이콘이 없으면 칸 긴 변 × 그것)이고, T172 가 넣었던 «칸 긴 변 × 1.35» 하한은 없앴다.
        /// ⚠ 주인 말이 세 번 바뀐 자리다 — «밖에»(10:2X) → «상점은 바꾸기 전이 맞았음»(13:1X) → «다 안으로»(13:2X · 최종). 되살리지 말 것.
        /// <paramref name="clip"/> 를 false 로 주는 자리는 «칸이 아닌» 특전 리본 뒤 빛 두 겹(T155 ⓒ) 하나뿐이다.
        /// </para>
        /// <para>
        /// <b>T155 ⓓ(주인 07:3X «모든 이펙트 라이트 들어간 곳에 글로우 서클도 같이»)</b>: 같은 사각형·같은 중심에 <see cref="GlowKey"/> 를
        /// 빛살 <b>아래</b> 겹으로 한 장 깐다. <b>돌리지 않는다</b> — 원이라 돌려도 티가 안 나고 도는 트윈만 늘어 fps 를 깎는다(지시서 T155 4항 «성능»).
        /// </para>
        /// 빛살은 DOLocalRotate(0,0,−360 · FastBeyond360 · Linear · 무한 · unscaled · SetLink) 로 <b>시계방향</b>(주인 «오른쪽으로») 한 바퀴 <paramref name="period"/> 초.
        /// 이미 있으면 갱신만. 스크롤 밖 칸은 <see cref="SetLightSpinning"/> 으로 멈춘다(T72 4항 개수 제한).
        /// </summary>
        /// <summary>아이템 칸 조각 인스턴스의 이름 앞머리 — <see cref="Spawn"/> 이 인스턴스 이름을 카탈로그 키로 두므로(결정 325) «ui.itemFrame.&lt;색&gt;» 이다.</summary>
        public const string ItemFramePrefix = "ui.itemFrame";
        /// <summary>
        /// T190 — 이 칸이 «아이템·보상 칸»(조각 <c>ItemFrame_01_*</c>)인가. <b>빛을 걸지 말지 가르는 판정을 이 한 함수에 모은다</b>
        /// (지시서 T190 1항 «애매하면 그 칸이 ItemFrame_01 을 쓰는가로 판정하고, 판정을 한 함수로 두어 다음 워커가 눈으로 고르지 않게 한다»).
        /// 칸 자신·조상·바로 아래 자식까지 본다 — 우리 코드가 프레임을 «칸으로 세우는» 꼴과 «칸 안에 자식으로 세우는» 꼴 둘 다 쓰기 때문이다.
        /// <b>상점 상품 카드</b>는 다른 조각(<c>ListItem_ShopItem</c>)이라 여기에 안 걸린다 — 주인 13:1X «상점은 바꾸기 전이 맞았음» 대로 빛이 남는다.
        /// </summary>
        public static bool IsItemCell(Transform cell)
        {
            if (cell == null) return false;
            for (var t = cell; t != null; t = t.parent)
                if (IsItemFrameName(t.name)) return true;
            for (int i = 0; i < cell.childCount; i++)
                if (IsItemFrameName(cell.GetChild(i).name)) return true;
            return false;
        }
        /// <summary>
        /// T190 회차 2 — 조각 인스턴스 이름 판정. <b>두 꼴을 다 본다</b>: <see cref="Spawn"/> 이 붙이는 카탈로그 키(«ui.itemFrame.&lt;색&gt;» · 결정 325)와,
        /// 부르는 쪽이 <c>go.name = "ItemFrame_01"</c> 처럼 <b>조각 이름으로 바꿔 놓은</b> 꼴(펫 빈 슬롯·뽑기 결과 칸 등)이다.
        /// 회차 1 은 앞의 것만 봐서 «뽑기 결과 칸이 아이템 칸이 아니다» 라는 틀린 답을 냈다(CI #381 실측).
        /// <para>
        /// ⚠ 뒤 꼴은 <b>«ItemFrame_»</b> 까지 봐야 한다 — 그냥 «ItemFrame» 으로 자르면 상점 상품 카드 조각 안의
        /// <c>ItemFrameArea</c>(쓰지 않아 꺼 두는 자리 · <c>ShopScreen.cs:427</c>)가 걸려 **남겨야 할 상점 빛까지 꺼진다**.
        /// </para>
        /// </summary>
        static bool IsItemFrameName(string n) =>
            !string.IsNullOrEmpty(n) && (n.StartsWith(ItemFramePrefix, StringComparison.Ordinal) || n.StartsWith("ItemFrame_", StringComparison.Ordinal));
        /// <summary>
        /// T190 게이트용 읽기 — 이 칸에 빛 <b>담개</b>(<see cref="LightMaskName"/>)가 서 있나.
        /// 이미 있는 <see cref="HasLight"/>(도는 빛살이 «보이나»)와 다르다: 담개는 빛살과 글로우 서클을 **둘 다** 담으므로,
        /// «빛을 아예 안 걸었다» 를 재려면 담개가 **없어야** 한다(빛살만 끄고 서클이 남는 것을 이 자가 잡는다).
        /// </summary>
        public static bool HasLightMask(Transform cell) => cell != null && cell.Find(LightMaskName) != null;

        public static Image LightBehind(RectTransform cell, RectTransform icon = null, string key = LightKey, float period = LightPeriod, Color? tint = null, float scale = LightScale, float inset = 0f, float sidePx = 0f, bool clip = true, bool dust = true)
        {
            if (cell == null) return null;
            var sp = Cat != null ? Cat.Sprite(key) : null; if (sp == null) return null;
            RectTransform mask = null;
            for (int i = 0; i < cell.childCount; i++) if (cell.GetChild(i).name == LightMaskName) { mask = (RectTransform)cell.GetChild(i); break; }
            if (mask == null) mask = Rect(cell, LightMaskName);
            // T189(주인 13:2X «그냥 밖으로 하는 거 말고 다 안으로 해라 걍» · T172 통째 취소) —
            // 담개에 마스크를 **되살린다**: 빛도 글로우 서클도 칸 안에서 끝난다. T172 회차에 만들어져
            // 마스크가 빠진 담개가 이미 있을 수 있으므로 «없으면 붙이는» 꼴이다.
            // `clip: false` 는 «칸이 아닌 자리» 하나뿐이다 — 특전 «레벨 업» 리본 뒤 빛 두 겹(T155 ⓒ ·
            // 주인이 따로 시킨 연출이라 마스크 규칙과 무관하다고 지시서 T189 2항이 못 박았다).
            if (clip) Ensure<RectMask2D>(mask.gameObject);
            else { var m2d = mask.GetComponent<RectMask2D>(); if (m2d != null) UnityEngine.Object.DestroyImmediate(m2d); }
            Stretch(mask, inset, inset, inset, inset);
            // 형제 자리는 T72 그대로 둔다 — «아이콘 뒤 · 질감층(무늬·그라데이션) 위»(결정 171). 마스크가 없어진 것만으로
            // 빛은 이미 칸 밖으로 번진다(자를 것이 없다) — 자리를 맨 앞(0)으로 내리면 «칸 안» 몫이 프레임 몸통에 가려 사라지는데,
            // 주인 문장은 «밖에도 보이게» 이지 «안에서는 지워라» 가 아니다(결정 456 · 안쪽까지 지우려면 이 한 줄을 0 으로).
            int target = icon != null && icon.parent == cell ? icon.GetSiblingIndex() : 0;
            if (mask.GetSiblingIndex() < target) target--;
            mask.SetSiblingIndex(Mathf.Max(0, target));
            var lt = mask.Find(LightName) as RectTransform;
            Image img;
            if (lt == null) { lt = Rect(mask, LightName); img = lt.gameObject.AddComponent<Image>(); } else img = Ensure<Image>(lt.gameObject);
            img.sprite = sp; img.type = Image.Type.Simple; img.preserveAspect = true; img.raycastTarget = false; img.color = tint ?? Palette.A(Palette.White, LightAlpha);
            { var lm = LightMaterial(); if (lm != null) img.material = lm; }   // T181 ⓑ — 겹칠수록 밝아지는 加算(셰이더가 없으면 지금 그림 그대로)
            lt.anchorMin = lt.anchorMax = new Vector2(0.5f, 0.5f); lt.pivot = new Vector2(0.5f, 0.5f);
            Vector2 refSize = icon != null ? icon.rect.size : cell.rect.size;
            float side = Mathf.Max(refSize.x, refSize.y); if (side <= 1f) side = Mathf.Max(cell.rect.width, cell.rect.height);
            side *= scale;   // T189 — T172 가 넣은 «칸 긴 변 × 1.35» 하한은 없앴다(주인 «다 안으로»)
            // T155 ⓒ 회차 2 — 부르는 쪽이 «한 변» 을 직접 주면 위 두 규칙(아이콘 배·칸 하한)을 쓰지 않는다.
            // 아이콘 뒤 빛살은 «칸» 이 기준이라 저 규칙이 맞지만, 리본처럼 **가로로 긴 판** 뒤에 깔면
            // max(폭,높이)×1.9 가 화면 폭을 넘어 빛이 화면 절반을 덮는다(screens run 360 에서 실제로 그랬다 · 결정 479).
            if (sidePx > 0f) side = sidePx;
            lt.sizeDelta = new Vector2(side, side);
            Vector2 center = Vector2.zero;
            if (icon != null) { var c = mask.InverseTransformPoint(icon.TransformPoint(icon.rect.center)); center = new Vector2(c.x, c.y) - mask.rect.center; }
            lt.anchoredPosition = center;
            DOTween.Kill(lt); lt.localRotation = Quaternion.identity;
            lt.DOLocalRotate(new Vector3(0f, 0f, -360f), Mathf.Max(0.1f, period), RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).SetUpdate(true).SetLink(lt.gameObject);
            GlowUnder(mask, lt, tint);
            DustOver(mask, lt, tint, dust);   // T174 — 빛살 자리마다 «천천히 퍼지는» 알갱이 한 겹(끄려면 dust: false)
            return img;
        }
        /// <summary>
        /// T155 ⓓ — 빛살 <paramref name="light"/> 과 같은 사각형·같은 중심에 글로우 서클 한 장을 <b>아래 겹</b>(형제 맨 앞)으로 깐다.
        /// 정적이다(트윈 0). 카탈로그에 <see cref="GlowKey"/> 가 없는 환경이면 아무 일도 하지 않는다.
        /// </summary>
        static Image GlowUnder(RectTransform host, RectTransform light, Color? tint)
        {
            var sp = Cat != null ? Cat.Sprite(GlowKey) : null; if (sp == null || host == null || light == null) return null;
            var gt = host.Find(GlowName) as RectTransform;
            Image img;
            if (gt == null) { gt = Rect(host, GlowName); img = gt.gameObject.AddComponent<Image>(); } else img = Ensure<Image>(gt.gameObject);
            img.sprite = sp; img.type = Image.Type.Simple; img.preserveAspect = true; img.raycastTarget = false;
            img.color = tint.HasValue ? Palette.A(tint.Value, GlowAlpha) : Palette.A(Palette.White, GlowAlpha);
            { var lm = LightMaterial(); if (lm != null) img.material = lm; }   // T181 ⓑ — 빛살과 같은 加算(둘이 겹친 가운데가 자연히 더 밝다)
            gt.anchorMin = gt.anchorMax = new Vector2(0.5f, 0.5f); gt.pivot = new Vector2(0.5f, 0.5f);
            gt.sizeDelta = light.sizeDelta; gt.anchoredPosition = light.anchoredPosition;
            gt.SetSiblingIndex(0);
            return img;
        }
        /// <summary>
        /// T174 — 빛살 <paramref name="light"/> 과 같은 중심에 <b>빛 알갱이</b>(먼지) 한 겹을 <b>위</b>로 깐다.
        /// <list type="bullet">
        /// <item><b>진짜 파티클이 아니다</b> — UI 캔버스가 <c>ScreenSpaceOverlay</c> 라 <c>ParticleSystem</c> 은 언제나 UI «뒤» 로 간다
        /// (T144·T181 에서 같은 벽을 만났다). 그래서 작은 uGUI <c>Image</c> 몇 장을 트윈으로 흘린다.</item>
        /// <item><b>칸마다 트윈 «하나»</b>(지시서 4항 ⓐ) — 시퀀스 하나가 알갱이 <see cref="DustCount"/>개를 전부 움직인다.
        /// 알갱이마다 트윈을 따로 걸면 칸당 도는 트윈이 4배로 늘어 fps 가 바로 떨어진다(T129).</item>
        /// <item>알갱이는 가운데서 났다가 <b>바깥으로 천천히</b> 흐르며 알파가 0 으로 진다 — 시작 시각을 어긋나게 꽂아
        /// «퍼지는» 결을 만든다(주인 «빛 알갱이 먼지가 천천히 퍼지는 느낌으로»).</item>
        /// <item>담개(<see cref="LightMaskName"/>) 안에 있으므로 <c>clip</c> 규칙(T189 «다 안으로»)과 <see cref="SetLightSpinning"/>
        /// 의 «보이는 칸만» 규약을 <b>빛살과 똑같이</b> 받는다 — 새 예외를 만들지 않는다.</item>
        /// </list>
        /// <paramref name="on"/> 이 거짓이면 이미 있던 알갱이를 지운다(끄는 자리도 한 줄로 되게).
        /// </summary>
        static void DustOver(RectTransform host, RectTransform light, Color? tint, bool on)
        {
            if (host == null || light == null) return;
            var dt = host.Find(DustName) as RectTransform;
            if (!on)
            {
                if (dt != null) { DOTween.Kill(dt); UnityEngine.Object.Destroy(dt.gameObject); }
                return;
            }
            var sp = Cat != null ? Cat.Sprite(GlowKey) : null; if (sp == null) return;   // 조각이 없으면 조용히 아무 일 없음
            if (dt == null) dt = Rect(host, DustName);
            dt.anchorMin = dt.anchorMax = new Vector2(0.5f, 0.5f); dt.pivot = new Vector2(0.5f, 0.5f);
            dt.sizeDelta = light.sizeDelta; dt.anchoredPosition = light.anchoredPosition;
            dt.SetAsLastSibling();                                   // 빛살 «위»(같은 담개 안이라 층 규칙은 그대로)

            float side = Mathf.Max(light.sizeDelta.x, light.sizeDelta.y);
            float grain = Mathf.Max(2f, side * DustSizeMul), drift = side * DustDriftMul;
            var color = Palette.A(tint ?? Palette.White, DustAlpha);

            DOTween.Kill(dt);
            var seq = DOTween.Sequence().SetLink(dt.gameObject).SetUpdate(true).SetLoops(-1, LoopType.Restart);
            for (int i = 0; i < DustCount; i++)
            {
                var g = dt.Find(DustName + i) as RectTransform;
                Image gi;
                if (g == null) { g = Rect(dt, DustName + i); gi = g.gameObject.AddComponent<Image>(); } else gi = Ensure<Image>(g.gameObject);
                gi.sprite = sp; gi.type = Image.Type.Simple; gi.preserveAspect = true; gi.raycastTarget = false; gi.color = color;
                g.anchorMin = g.anchorMax = new Vector2(0.5f, 0.5f); g.pivot = new Vector2(0.5f, 0.5f);
                g.sizeDelta = new Vector2(grain, grain);
                g.anchoredPosition = Vector2.zero;
                // 알갱이마다 다른 방향(고르게 나눈 각 + 반 칸 어긋남)과 다른 시작 시각 — 무작위를 안 쓰므로 스샷이 회차마다 안 흔들린다
                float ang = (i + 0.5f) / DustCount * Mathf.PI * 2f;
                var to = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * drift;
                float at = DustPeriod * i / DustCount;
                seq.Insert(at, g.DOAnchorPos(to, DustPeriod).SetEase(Ease.OutSine).From(Vector2.zero));
                seq.Insert(at, gi.DOFade(0f, DustPeriod).SetEase(Ease.InQuad).From(color));
            }
            seq.SetTarget(dt);                                       // SetLightSpinning 이 «칸 하나» 로 재우고 깨울 수 있게
            // T174 회차 3 — **첫 프레임부터 이미 퍼져 있게** 시퀀스를 앞으로 감는다(결정 513).
            // 안 감으면 알갱이 넷이 «칸이 생긴 순간» 전부 가운데(= 빛살 중심 = 아이콘 중심)에 겹쳐 서는데,
            // 이 담개는 아이콘 «뒤» 겹이라(위 SetSiblingIndex) 그 순간 넷 다 **그림에 완전히 가려 안 보인다**.
            // 그래서 ⓐ 칸이 스크롤로 들어올 때마다 몇 초간 아무것도 없다가 나타나고
            //      ⓑ `screens` PNG 는 화면을 연 지 2~5프레임 만에 찍으므로(UiShotsTests.Shot) **영영 못 담는다**.
            // 감는 자리 = 마지막 알갱이가 막 나기 시작하는 시각 — 넷이 0·¼·½·¾ 만큼 퍼진 «흐르는 중» 이 된다.
            // 무작위가 아니라 상수라 스샷은 회차마다 그대로다(4항 ⓐ 의 «흔들리지 않는다» 를 지킨다).
            seq.Goto(DustPeriod * (DustCount - 1f) / DustCount, true);
        }

        /// <summary>이 칸에 빛 알갱이(T174)가 깔려 있는가(테스트·감사용) — «&lt;담개&gt;/Dust» 가 활성이고 알갱이가 <see cref="DustCount"/>개다.</summary>
        public static bool HasDust(Transform cell)
        {
            var dt = cell != null ? cell.Find(LightMaskName + "/" + DustName) : null;
            if (dt == null || !dt.gameObject.activeInHierarchy) return false;
            int n = 0;
            for (int i = 0; i < dt.childCount; i++)
            {
                var img = dt.GetChild(i).GetComponent<Image>();
                if (img != null && img.enabled && img.sprite != null) n++;
            }
            return n == DustCount;
        }

        /// <summary>이 칸의 빛살 «아래» 에 글로우 서클이 깔려 있는가(테스트·감사용 · T155 ⓓ) — «&lt;담개&gt;/Glow» 가 활성이고 스프라이트가 Glow_Circle.</summary>
        public static bool HasGlow(Transform cell)
        {
            var gt = cell != null ? cell.Find(LightMaskName + "/" + GlowName) : null; if (gt == null || !gt.gameObject.activeInHierarchy) return false;
            var img = gt.GetComponent<Image>(); return img != null && img.enabled && img.sprite != null && img.sprite.name.StartsWith("Glow_Circle");
        }
        /// <summary>빛살 회전 켜기/끄기(스크롤 밖 칸은 끈다 · T72 4항 «보이는 칸만») — 그 칸의 «LightMask/Light» 트윈을 Play/Pause. 없으면 아무 일 없음.</summary>
        public static void SetLightSpinning(RectTransform cell, bool on)
        {
            var lt = cell != null ? cell.Find(LightMaskName + "/" + LightName) : null; if (lt == null) return;
            if (on) DOTween.Play(lt); else DOTween.Pause(lt);
            // T174 4항 ⓑ — 알갱이도 «보이는 칸만» 규약에 같이 태운다(스크롤 밖에서는 멈춘다)
            var dt = cell.Find(LightMaskName + "/" + DustName);
            if (dt != null) { if (on) DOTween.Play(dt); else DOTween.Pause(dt); }
        }
        /// <summary>이 칸에 도는 빛살이 있는가(테스트·감사용) — «LightMask/Light» 가 활성이고 스프라이트 이름에 Effect_Light.</summary>
        public static bool HasLight(Transform cell)
        {
            var lt = cell != null ? cell.Find(LightMaskName + "/" + LightName) : null; if (lt == null || !lt.gameObject.activeInHierarchy) return false;
            var img = lt.GetComponent<Image>(); return img != null && img.enabled && img.sprite != null && img.sprite.name.StartsWith("Effect_Light");
        }
        /// <summary>이 사각형 바로 아래에 패턴 배경이 있는가(테스트·감사용) — 자식 «Pattern» RawImage 가 활성이고 텍스처 이름 Pattern_01_256.</summary>
        public static bool HasPattern(Transform host)
        {
            if (host == null) return false;
            for (int i = 0; i < host.childCount; i++)
            {
                var c = host.GetChild(i); if (c.name != PatternName || !c.gameObject.activeInHierarchy) continue;
                var raw = c.GetComponent<RawImage>(); if (raw != null && raw.enabled && raw.texture != null && raw.texture.name.StartsWith("Pattern_01")) return true;
            }
            return false;
        }

        /// <summary>
        /// ③ 그라데이션 색감(T72) — <paramref name="rt"/> 안에 «GradientTop»(<paramref name="topKey"/> · tint <paramref name="top"/> 기본 흰 α <see cref="GradientTopAlpha"/> = 위 +12% 밝기) 와 «GradientBottom»(<paramref name="bottomKey"/> · <paramref name="bottom"/> 기본 Ink α <see cref="GradientBottomAlpha"/> = 아래 −18%) 두 장을
        /// Stretch(안쪽 <paramref name="inset"/> · 둥근 모서리 조각이면 모서리 반지름만큼) · raycast 끔 · 조각에 9-slice border 가 있으면 Sliced 아니면 Simple 로 <paramref name="siblingIndex"/> 자리(기본 0 = rt 자신의 배경 그림 바로 위 · 글자·아이콘 자식은 그 위에 남는다)에 덧댄다.
        /// 한쪽만 원하면 그 키에 null — 버튼은 (null, "ui.btnGradient") 아래 어둠만, 카드는 ("fr.cardGradient3", …). 이미 있으면 갱신만. 코드 도형 0 — 색은 tint 뿐(색은 점수 밖 · «느낌» 규칙 ⓐ).
        /// 같은 사각형에 <see cref="PatternBg"/> 가 이미 있으면 그 <b>위</b>로 들어간다(질감 층 순서 = 바탕 → 패턴 → 그라데이션 → 빛살 → 내용 · 결정 171).
        /// </summary>
        public static void Gradient(RectTransform rt, Color? top = null, Color? bottom = null, string topKey = GradTopKey, string bottomKey = GradBottomKey, float inset = 0f, int siblingIndex = 0)
        {
            if (rt == null) return;
            int idx = Mathf.Clamp(siblingIndex, 0, Mathf.Max(0, rt.childCount));
            // 질감 층 순서 = 바탕 → «Pattern» → 그라데이션 → 빛살 → 내용. 패턴이 이미 깔려 있으면 그 위에 덧대야 한다
            // (그냥 형제 0 으로 넣으면 패턴이 아래로 밀려 «패턴은 형제 0» 계약이 깨진다 — CI #131·#134·#135 의 T72 빨강 · T82 · 결정 171)
            for (int i = 0; i < rt.childCount; i++) if (rt.GetChild(i).name == PatternName) { idx = Mathf.Max(idx, i + 1); break; }
            if (!string.IsNullOrEmpty(topKey))
            {
                var g = GradientLayer(rt, GradientTopName, topKey, top ?? Palette.A(Palette.White, GradientTopAlpha), inset);
                if (g != null) { g.transform.SetSiblingIndex(idx); idx = g.transform.GetSiblingIndex() + 1; }
            }
            if (!string.IsNullOrEmpty(bottomKey))
            {
                var g = GradientLayer(rt, GradientBottomName, bottomKey, bottom ?? Palette.A(Palette.Ink, GradientBottomAlpha), inset);
                if (g != null) g.transform.SetSiblingIndex(idx);
            }
        }
        /// <summary>
        /// ③ 그라데이션 — <b>카드·타일</b>용(T116 · 주인 «더 화려하게 색깔»). 레퍼런스의 카드는 «어두운 위 → 밝은 아래» 의 같은 계열 <b>두 색</b>이라
        /// <see cref="Gradient"/> 의 기본(위 흰 / 아래 잉크)과 <b>방향도 색도 다르다</b> — 그래서 별도 입구로 둔다(배경·버튼은 종전 그대로).
        /// <para>
        /// <paramref name="paletteName"/> 가 <see cref="GradientPalette.Names"/> 에 있으면 <b>실측 두 색</b>을 그대로 쓰고(카탈로그 <c>col.grad.*</c>),
        /// 없으면 <paramref name="baseColor"/> 에서 <see cref="GradientPalette.CardWay"/> 로 만든다(계열색 유지 · 밝기만 벌린다).
        /// 둘 다 없으면 방향만 레퍼런스대로인 무채색(어두운 위 / 밝은 아래)이다.
        /// </para>
        /// 층 순서·조각·raycast·중복 방지는 <see cref="Gradient"/> 와 같다(그 함수를 그대로 부른다).
        /// <para>
        /// <paramref name="alpha"/> 는 색 세기다 — 기본 <see cref="GradientCardAlpha"/>(덧칠 · 조각의 바탕색이 살아 있는 자리 · 09 상품 카드),
        /// <see cref="GradientCardSolidAlpha"/> 는 <b>몸통이 곧 그라데이션</b>인 자리(10 상자 카드 · T100 ⓓ 회차 2 · 결정 338).
        /// </para>
        /// </summary>
        public static void GradientCard(RectTransform rt, string paletteName = null, Color? baseColor = null, float inset = 0f, int siblingIndex = 0, float alpha = GradientCardAlpha)
        {
            if (rt == null) return;
            Color top, bottom;
            if (!string.IsNullOrEmpty(paletteName) && GradientPalette.Has(paletteName))
            {
                var p = GradientPalette.Of(paletteName);
                top = Palette.A(p.Top, alpha); bottom = Palette.A(p.Bottom, alpha);
            }
            else if (baseColor.HasValue)
            {
                var p = GradientPalette.CardWay(baseColor.Value);
                top = Palette.A(p.Top, alpha); bottom = Palette.A(p.Bottom, alpha);
            }
            else
            {
                top = Palette.A(Palette.Ink, GradientCardTopAlpha); bottom = Palette.A(Palette.White, GradientCardBottomAlpha);
            }
            Gradient(rt, top, bottom, GradTopKey, GradBottomKey, inset, siblingIndex);
        }

        /// <summary>
        /// T344 — <see cref="GradientCard"/> 와 같은데 <b>가로(왼쪽 → 오른쪽)</b>다(주인 2026-09-10 «그라디언트가 왼쪽 오른쪽 이어야 하는데 상하로 되어 있네»).
        /// <para>
        /// 표(<see cref="GradientPalette"/>)의 <c>Top</c> 이 <b>왼쪽</b>, <c>Bottom</c> 이 <b>오른쪽</b> 색이 된다 —
        /// 눕히는 일은 <see cref="GradientSideways"/> 가 하고(왜 늘리기로 못 하는지도 그 자의 주석에 있다), 여기서는 색과 층만 <see cref="GradientCard"/> 와 똑같이 준다.
        /// </para>
        /// ⚠ 세로(<see cref="GradientCard"/>)와 <b>같은 사각형에 둘 다 걸지 않는다</b> — 조각 이름이 같아 나중 것이 앞 것을 덮어쓴다.
        /// </summary>
        public static void GradientCardSide(RectTransform rt, string paletteName = null, Color? baseColor = null, float inset = 0f, int siblingIndex = 0, float alpha = GradientCardAlpha)
        {
            if (rt == null) return;
            GradientCard(rt, paletteName, baseColor, inset, siblingIndex, alpha);
            var side = Ensure<GradientSideways>(rt.gameObject);
            // 크기를 이미 알면 이 프레임에 눕는다(모르면 다음 프레임에 저 스스로)
            if (side != null) side.Apply(true);
        }

        static Image GradientLayer(RectTransform rt, string name, string key, Color tint, float inset)
        {
            var sp = Cat != null ? Cat.Sprite(key) : null; if (sp == null) return null;
            Image img = null;
            for (int i = 0; i < rt.childCount; i++) if (rt.GetChild(i).name == name) { img = rt.GetChild(i).GetComponent<Image>(); break; }
            if (img == null) { var r = Rect(rt, name); img = r.gameObject.AddComponent<Image>(); }
            img.sprite = sp; img.type = sp.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple; img.preserveAspect = false; img.color = tint; img.raycastTarget = false;
            Stretch(img.rectTransform, inset, inset, inset, inset);
            return img;
        }
        /// <summary>그라데이션 조각이 있는가(테스트·감사용) — 자식 «GradientTop» 또는 «GradientBottom» 이 활성이고 스프라이트 이름에 Gradient.</summary>
        public static bool HasGradient(Transform rt)
        {
            if (rt == null) return false;
            for (int i = 0; i < rt.childCount; i++)
            {
                var c = rt.GetChild(i); if ((c.name != GradientTopName && c.name != GradientBottomName) || !c.gameObject.activeInHierarchy) continue;
                var img = c.GetComponent<Image>(); if (img != null && img.enabled && img.sprite != null && img.sprite.name.IndexOf("Gradient", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        // ───────────────────────── 공통 팝업 문법 (docs/ref/README.md «공통 문법» · T36 — T38·T41·T42·T44 가 같이 쓴다) ─────────────────────────
        /// <summary><see cref="Popup"/> 이 만든 조각들 — 안의 내용은 <see cref="Box"/> 에 <see cref="Pct"/> 로 배치한다.</summary>
        public sealed class PopupParts { public RectTransform Dim, Box, Ribbon; public TMP_Text Title, TapClose; }
        /// <summary>
        /// 레퍼런스 공통 팝업: <b>어두운 반투명 배경</b> 위 <b>둥근 패널</b>(Popup_Box 변형 · <paramref name="popupKey"/>) · 제목은 패널 윗변에 걸친 <b>리본/명판</b>(<paramref name="titleKey"/> · 가운데) ·
        /// 프레임 밖 아래 가운데 <b>«탭하여 닫기»</b> 흰 글자(<see cref="Layout.BookClose"/> 줄 · 닫기 X 버튼 없음 · <b>배경 탭으로 닫힘</b> = <paramref name="onTapClose"/>). onTapClose 가 null 이면 닫기 글자·배경 탭 없음(선택을 강제하는 이벤트 팝업).
        /// 조각은 전부 GUI Pro 프리팹 · 코드 도형 0. 상자 안 배치는 돌려준 <see cref="PopupParts.Box"/> 에 Pct 로.
        /// </summary>
        /// <summary>색을 안 쓰는 <b>기본</b> 팝업 상자 키(T130) — 이 키로 세운 상자만 어두운 회색으로 바꾼다. 색 변형(<c>ui.popup.blue</c> 등)은 이벤트가 일부러 색을 쓰는 자리라 그대로 둔다.</summary>
        public const string PopupKeyPlain = "ui.popup";
        /// <summary>기본 팝업 상자를 레퍼런스대로 어둡게(T130) — 조각의 «Bg»·«DecoLine» 을 <see cref="Palette.PopupBox"/>·<see cref="Palette.PopupDeco"/> 로 tint 한다(«Border» 는 원래 검정이라 그대로). 바꿨으면 true.</summary>
        public static bool DarkenPopupBox(RectTransform box, string popupKey)
        {
            if (box == null || popupKey != PopupKeyPlain) return false;
            for (int i = 0; i < box.childCount; i++)
            {
                var c = box.GetChild(i); var img = c.GetComponent<Image>(); if (img == null) continue;
                if (c.name == "Bg") img.color = Palette.PopupBox;
                else if (c.name == "DecoLine") img.color = Palette.PopupDeco;
            }
            return true;
        }
        /// <summary>
        /// T135 — 특전 카드 몸통을 레퍼런스 04 의 어두운 회색(<see cref="Palette.PerkCardBody"/>)으로. 조각(<c>CardFrame_04_*</c>)의 직계 «Bg» 하나만 tint 한다.
        /// 등급을 알려 주는 «TitleBg»(탭)와 테두리(«Border»·«TitleBorder» · T69)·안쪽 밝은 선(«InnerBorder»)은 건드리지 않는다 — 몸통만 뒤집는 일이다.
        /// <see cref="Desaturate"/>·<see cref="InkFrameBorders"/> «뒤» 에 불러야 그 둘이 다시 덮지 않는다.
        /// </summary>
        public static void DarkenCardBody(RectTransform frame)
        {
            if (frame == null) return;
            for (int i = 0; i < frame.childCount; i++)
            {
                var c = frame.GetChild(i);
                if (c.name != CardBodyName) continue;
                var img = c.GetComponent<Image>();
                if (img != null) img.color = Palette.PerkCardBody;
            }
        }
        /// <summary>특전 카드 조각(<c>CardFrame_04_BasePrefab</c>)의 몸통 자식 이름 — 형제는 InnerBorder · Border · TitleBg · TitleBorder(프리팹 실측).</summary>
        public const string CardBodyName = "Bg";

        /// <summary>«판 없음» 모드(<c>boxed: false</c>)가 상자 조각 대신 세우는 투명 칸의 이름 — 게이트가 «상자가 없다» 를 이 이름으로도 읽는다(T141).</summary>
        public const string NoBoxName = "PopupNoBox";

        public static PopupParts Popup(Transform layer, string title, Layout.R rect, Action onTapClose, string popupKey = PopupKeyPlain, string titleKey = "ui.title.tangerine", bool dim = true, bool boxed = true)
        {
            var parts = new PopupParts();
            if (dim)
            {
                // T104 — 어둠은 프레임 «밖»(레터박스 · 노치 · T106 이 화면 끝까지 뻗어 놓은 상단·하단 프레임 띠)까지 덮는다
                var d = Rect(layer, "Dimmed"); Stretch(d, -DimOverscan, -DimOverscan, -DimOverscan, -DimOverscan);
                var di = d.gameObject.AddComponent<Image>(); di.color = Palette.A(Palette.Dim, DimAlpha); di.raycastTarget = true;
                FadeIn(di, DimAlpha);
                parts.Dim = d;
            }
            // T141(주인 2026-09-07 «그 흰색 패널? 통일되게 검정 투명 딤 위에 있는 느낌으로») — 쉼터·악마·천사 세 형제는 판 없이 어둠 위에 바로 얹힌다.
            // 조각 대신 «같은 rect 의 투명 칸» 을 세우므로 내용의 % 자리 계산은 한 줄도 안 바뀌고, 상자에 딸린 무늬·그라데이션·어둡게도 같이 건너뛴다.
            // 어둠(Dimmed)과 리본은 특전 선택(04)처럼 그대로 남는다 — 클릭 차단은 어둠이 맡는다(이미 raycastTarget = true).
            RectTransform box;
            if (!boxed) { box = Rect(layer, NoBoxName); Pct(box, rect); }
            else
            {
                box = SpawnRt(popupKey, layer, rect);
                foreach (var g in box.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = true;   // 상자 뒤로 클릭이 새지 않게
                // T130 — 기본 팝업 상자를 레퍼런스대로 «어두운 회색» 으로. 조각이 색을 변형의 색 덮어쓰기로 가지므로 tint 로 닿는다(새 그림·새 키 0).
                bool darkBox = DarkenPopupBox(box, popupKey);
                // T72 ① 팝업 상자 «안» 배경 패턴(ROUTINE T72 1항 적용 목록) — 조각의 «Bg» 바로 위(DecoLine·Border 아래) · 둥근 모서리 안쪽.
                // 여기 한 곳이라 모든 공통 팝업(Overlay.Box · LobbyPopups · 화면 세부 팝업)이 같이 받는다 — 화면 코드는 한 줄도 안 만진다.
                // 무늬 색은 바탕을 따라간다 — T130 뒤로 기본 상자는 어두우니 흰 무늬, 색 변형(ui.popup.<색>)은 여전히 밝아 Ink 무늬다.
                int patIdx = 0; for (int i = 0; i < box.childCount; i++) if (box.GetChild(i).name == "Bg") { patIdx = i + 1; break; }
                PatternBg(box, darkBox ? PatternTintDark : PatternTintLight, PatternTileSeconds, patIdx, PatternTilePx, PopupPatternInset);
                // T72 ③ 상자 «안» 그라데이션(위 +12% 밝음 · 아래 −18% 어둠) — 패턴 바로 위 · 테두리·리본·내용 아래(질감 층 순서 = 결정 171)
                // 여기 한 곳이라 공통 팝업 전부가 같이 받는다(버튼 공통 적용은 결정 170 대로 계속 보류 · 결정 188).
                // T116 실측 — 레퍼런스의 팝업 패널도 거의 단색(#2C2829 → #201E1F)이라 배경(0.12/0.18)보다 얕게
                Gradient(box, Palette.A(Palette.White, GradientFlatTopAlpha), Palette.A(Palette.Ink, GradientFlatBottomAlpha), inset: PopupPatternInset, siblingIndex: patIdx);
            }
            var ribbon = Spawn(titleKey, box); var rr = (RectTransform)ribbon.transform;
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 1f); rr.pivot = new Vector2(0.5f, 0.5f); rr.sizeDelta = PopupRibbonSize; rr.anchoredPosition = new Vector2(0, 8);
            Ensure<PopupRibbonTag>(ribbon);   // T75 4항 — 게이트가 «UiKit.Popup 이 세운 리본» 만 단언하게(화면이 스스로 세운 리본은 그 화면 워커 몫 · 결정 291)
            RibbonFit(rr);
            var tt = SetText(rr, "Text (TMP)", title, null, TextSize.Title, TextKind.Title); if (tt != null) { tt.enableAutoSizing = true; tt.fontSizeMin = TextSize.BestFitMin; tt.fontSizeMax = TextSize.Title; }
            parts.Box = box; parts.Ribbon = rr; parts.Title = tt;
            if (onTapClose != null)
            {
                var tc = Text(layer, "탭하여 닫기", TextSize.Body, Palette.White, TextAnchor.MiddleCenter, false, true); tc.name = "TapToClose"; tc.fontStyle = FontStyles.Bold;
                Pct(tc.rectTransform, Layout.BookClose);   // 표 «닫기 안내» 자리(y91.5 · 높이는 본문 40 의 줄 높이가 들어가는 2.4 = 56px · T63-settings · 이름표가 이 사각형을 잰다)
                parts.TapClose = tc;
                if (parts.Dim != null) Clickable(parts.Dim, onTapClose, false);
            }
            PopIn(box);
            return parts;
        }

        // ───────────────────────── 주인 에셋(GUI Pro) 프리팹 다루기 ─────────────────────────
        /// <summary>
        /// 카탈로그 프리팹을 parent 밑에 인스턴스화 + <see cref="Adopt"/>. 없으면 빈 RectTransform 을 준다(로그만).
        /// ⚠ T15: 활성 부모 밑에 바로 Instantiate 하면 GUI Pro 데모 스크립트(<c>LayerLab.CasualGame.PanelView.OnEnable</c> · otherPanels 미할당)가
        /// <see cref="Adopt"/> 가 지우기 전에 돌아 <c>UnassignedReferenceException</c>(빌드에선 NRE) 을 던진다 — 설정·세부·전투 팝업을 열 때마다 콘솔 빨간 줄(CI #36 PlayMode 3건).
        /// 그래서 **비활성 대기 오브젝트** 밑에 먼저 만들어 데모 스크립트를 떼고(OnEnable 이 한 번도 안 돈다) 그 다음 parent 로 옮긴다.
        /// </summary>
        public static GameObject Spawn(string prefabKey, Transform parent, bool adopt = true) => SpawnWith(Cat, prefabKey, parent, adopt);

        /// <summary>
        /// <see cref="Spawn"/> 과 같은 손질(데모 스크립트 제거 · <see cref="Adopt"/>)을 하되 **카탈로그를 직접 받는다** —
        /// <see cref="App"/> 이 아직 없는 부팅 로딩 화면(T96-loading)이 쓴다(<see cref="Cat"/> 은 App 이 서야 값이 있다).
        /// </summary>
        public static GameObject SpawnWith(AssetCatalog cat, string prefabKey, Transform parent, bool adopt = true)
        {
            var prefab = cat != null ? cat.Prefab(prefabKey) : null;
            GameObject go;
            if (prefab == null) { go = new GameObject(prefabKey, typeof(RectTransform)); go.transform.SetParent(parent, false); return go; }
            go = UnityEngine.Object.Instantiate(prefab, Staging(), false);
            go.name = prefabKey;
            StripDemoScripts(go);
            StripDemoHighlights(go);
            if (adopt) Adopt(go);
            go.transform.SetParent(parent, false);
            return go;
        }
        static GameObject _staging;
        /// <summary>인스턴스화 전용 비활성 홀더(씬 루트 · 자식은 OnEnable/Awake 가 돌지 않는다). 씬이 바뀌어 파괴되면 다시 만든다.</summary>
        static Transform Staging()
        {
            if (_staging == null) { _staging = new GameObject("UiKit.Staging", typeof(RectTransform)); _staging.SetActive(false); }
            return _staging.transform;
        }
        /// <summary>GUI Pro 데모 스크립트(PanelView · PanelControl) 제거 — 프리팹이 활성화되기 전에 부른다(T15).</summary>
        static void StripDemoScripts(GameObject root)
        {
            foreach (var pv in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (pv == null) continue;
                var tn = pv.GetType().FullName;
                if (tn == "LayerLab.CasualGame.PanelView" || tn == "LayerLab.GUIScripts.PanelControl") UnityEngine.Object.DestroyImmediate(pv);
            }
        }
        /// <summary>
        /// T164 회차 3 — 조각이 달고 오는 데모 하이라이트(<c>HighLight*</c>)를 <b>세우는 자리 한 곳</b>에서 끈다
        /// (주인 09:0X «썡뚱맞게 HighLight1,2 있는데 튀기만 하고 이상함 그거 삭제하라 해»).
        /// <para>
        /// 회차 2 는 <see cref="GearUi.DarkFrame"/>(= <b>아이템 칸</b>을 세우는 자리)에서 껐는데,
        /// 하이라이트를 달고 오는 조각은 아이템 칸만이 아니다 — 실측 <b>9개 조각</b>이 그것을 품는다
        /// (<c>Button_02_BasePrefab</c>·<c>Button_03_BasePrefab</c>·<c>PassFrame_02~04</c>·<c>CardFrame_01</c>·
        /// <c>ItemFrame_01_Normal</c>·<c>Button_Pause_01</c>·<c>Button_Close_Square_01</c>).
        /// 그래서 펫 화면의 «전체 강화» 버튼(<c>ui.btnGray</c> → <c>Button_02_BasePrefab</c>)은 그 손질을 못 받아
        /// CI #332 에서 «[13_pet] 켜진 하이라이트가 4개» 로 빨갰다.
        /// </para>
        /// 우리 코드가 이 자식을 쓰는 자리는 <b>한 곳도 없다</b>(grep 0건) → 조각 원본은 그대로 두고(§1 «프리팹은 부품 · 원본 불변»)
        /// <b>인스턴스만</b> 끈다. <see cref="StripDemoScripts"/> 와 같은 자리(비활성 대기 중)라 한 프레임도 안 번쩍인다.
        /// 잣대는 끄는 쪽·재는 쪽이 갈리지 않게 <see cref="GearUi.HighlightPrefix"/> 하나를 계속 쓴다.
        /// </summary>
        static void StripDemoHighlights(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name.StartsWith(GearUi.HighlightPrefix, StringComparison.Ordinal)) t.gameObject.SetActive(false);
        }
        public static RectTransform SpawnRt(string prefabKey, Transform parent, Layout.R r)
        {
            var go = Spawn(prefabKey, parent); var rt = (RectTransform)go.transform; Pct(rt, r); return rt;
        }

        /// <summary>빨간 알림 점 조각의 카탈로그 키(<see cref="AlertDot"/> · 게이트가 이 키로 조각을 집어 스프라이트를 찾는다).</summary>
        public const string AlertDotKey = "ui.alertDot";

        /// <summary>가격 줄의 다이아 아이콘 한 변 = 버튼 글자 크기(정사각 · 글자 높이와 같게 · 상점 T63 값 그대로).</summary>
        public const int PriceIconSize = TextSize.Button;
        /// <summary>가격 줄 요소 간격(px).</summary>
        public const float PriceGap = 10f;

        /// <summary>
        /// [라벨][아이콘][값] 한 줄 — HorizontalLayoutGroup 이 자식을 선호 크기로 가운데 정렬(글자는 Overflow · rect 가 선호 폭과 같아 반올림으로 줄이 접히지 않게).
        /// <para>상점 상자 카드의 «1회 / [💎 400]» 버튼이 세운 꼴(T63-shop · 결정 142)을 <b>T380 이 여기로 올렸다</b> — 펫 소환 버튼이 같은 꼴을 쓴다
        /// (주인 2026-09-10 «펫부분도 뽑기 버튼 내부 디자인 신화상자 버튼 디자인처럼 · 1회 / 다이아 아이콘 100»). 한 화면에서 «값 줄» 이 두 꼴이면 사람이 두 번 배운다.</para>
        /// <para>T255 — 아이콘·글자 크기를 인자로 받는다(키 버튼이 같은 줄 꼴을 쓴다 · 기본값은 종전 그대로라 다이아 버튼은 한 픽셀도 안 바뀐다).
        /// 줄·값 글자의 이름(<paramref name="rowName"/>·<paramref name="costName"/>)은 부르는 쪽의 이름 계약을 따른다(상점 = Price/Cost · 펫 = Cost/Qty).</para>
        /// </summary>
        public static RectTransform PriceRow(RectTransform parent, Layout.R r, string cost, string before = null, string iconKey = "hud.gem", string iconName = "Gem", int costSize = TextSize.Button, TextKind costKind = TextKind.Button, int iconSize = PriceIconSize, string rowName = "Price", string costName = "Cost")
        {
            var row = Rect(parent, rowName); Pct(row, r);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = PriceGap; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false; hl.childControlWidth = true; hl.childControlHeight = true;
            if (!string.IsNullOrEmpty(before)) { var t = Text(row, before, TextSize.Button, Palette.White, TextAnchor.MiddleCenter, false, true, TextKind.Button); t.name = "Label"; t.textWrappingMode = TextWrappingModes.NoWrap; }
            var ic = Icon(row, iconName, iconKey); ic.preserveAspect = true;
            var le = ic.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = iconSize; le.preferredHeight = iconSize;
            var c = Text(row, cost, costSize, Palette.White, TextAnchor.MiddleCenter, false, true, costKind); c.name = costName; c.textWrappingMode = TextWrappingModes.NoWrap;
            return row;
        }

        /// <summary>
        /// 빨간 알림 점(T136 · 주인 2026-09-07 «빨간점들이 찌그러져있더라») — **점을 세우는 유일한 입구**.
        /// 조각 <c>Alert_Dot_01_Red</c> 는 그림 하나라 rect 가 정사각이 아니면 그대로 늘어난다.
        /// 그래서 자리는 <b>부모 모서리 + px</b>, 크기는 <b>«지름» 하나</b>로만 받는다 — 부모 칸의 %(<see cref="Pct"/>)로 재면
        /// 칸이 가로로 넓을 때 점이 타원이 된다(로비 보조 줄이 55×24 = 2.26:1 이었다).
        /// 세우면서 그림에 <c>preserveAspect</c> 를 켠다 — 나중에 누가 rect 를 찌그러뜨려도 그림만은 원으로 남는 마지막 방어선.
        /// </summary>
        /// <param name="anchor">부모 안의 기준 모서리(오른쪽 위 = (1,1) · 왼쪽 위 = (0,1) · 왼쪽 아래 = (0,0)).</param>
        /// <param name="offsetPx">그 모서리에서 점 «가운데» 까지의 프레임 px(유니티 부호 그대로 — 왼쪽·아래가 음수).</param>
        /// <param name="d">점 지름 px(가로 = 세로).</param>
        public static GameObject AlertDot(Transform parent, string name, Vector2 anchor, Vector2 offsetPx, float d)
        {
            var go = Spawn(AlertDotKey, parent);
            go.name = name;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offsetPx;
            rt.sizeDelta = new Vector2(d, d);
            rt.localScale = Vector3.one;
            foreach (var img in go.GetComponentsInChildren<Image>(true)) img.preserveAspect = true;
            return go;
        }

        /// <summary>
        /// 인스턴스를 이 프로젝트 규칙에 맞춘다 — <b>조각의 TMP 글자에 주인 글꼴을 입히고</b> · LayerLab 데모 스크립트 제거 · 이미지 raycast 끔.
        /// <para>
        /// <b>T207 ② 가 여기를 뒤집었다.</b> 전에는 이 줄이 조각의 TMP 글자를 <b>전부 파괴하고</b> uGUI <c>Text</c> 로 갈아 끼웠다
        /// (그래서 «입력칸을 먼저 걷어 냈다가 다시 세우는» 두 함수도 필요했다). 주인이 «tmpro로 아웃라인 해야지 진짜 메테리얼로» 라고
        /// 정했으므로 <b>부수기를 그만두고 글꼴만 갈아 끼운다</b> — 조각이 정한 크기·정렬·줄바꿈이 손실 없이 살고(전에는 손으로 베껴
        /// 옮겨서 그 자체가 어긋남의 원천이었다 · T194 1-b) 입력칸도 제 <c>TMP_InputField</c> 로 그대로 남는다.
        /// </para>
        /// </summary>
        public static void Adopt(GameObject root)
        {
            StripDemoScripts(root);
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true)) SkinTmp(t);
            foreach (var g in root.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            // 입력칸은 눌려야 한다(바로 위에서 전부 껐다) — 조각의 TMP_InputField 를 그대로 쓰므로 되세울 것이 없다(T207 ②)
            foreach (var f in root.GetComponentsInChildren<TMP_InputField>(true))
            {
                var img = f.GetComponent<Image>(); if (img != null) img.raycastTarget = true;
            }
            var rt = root.transform as RectTransform; if (rt != null) rt.localScale = Vector3.one;
        }

        /// <summary>
        /// T207 ② — 조각이 달고 온 TMP 글자에 <b>우리 규칙만 입힌다</b>(부수지 않는다).
        /// <para>
        /// 하는 일 넷: ⓐ <b>주인 글꼴</b>(Jua TMP 폰트 애셋 · <see cref="TmpFont.Get"/>) ⓑ 없는 글리프 걸러내기(<see cref="TextGlyphs.Safe"/>) ⓒ
        /// <b>크기 하한</b>(T63 — 데모 조각의 12~30 을 그대로 두면 폰에서 안 읽힌다) ⓓ <b>검정 아웃라인</b>(T63 0항 · 이제 SDF 머티리얼이다).
        /// </para>
        /// <b>안 하는 일</b>: 자리·정렬·줄바꿈·굵기를 손대지 않는다 — 그것은 조각이 정한 값이고, 전에 그 값을 손으로 베껴 옮기던 것이
        /// 어긋남의 원천이었다(T194 1-b 의 Δ 가 거기서 나왔다). 크기도 <b>하한보다 크면 그대로</b> 둔다.
        /// </summary>
        static TMP_Text SkinTmp(TMP_Text t)
        {
            if (t == null) return null;
            var asset = TmpFont.Get();
            if (asset != null) { t.font = asset; t.fontSharedMaterial = asset.material; }
            t.text = TextGlyphs.Safe(t.text);
            // ⚠ TMP 와 uGUI 의 결정적인 차이 하나 — <b>자동 크기가 «고른» 값을 TMP 는 `fontSize` 에 되써 넣는다</b>
            // (uGUI 는 `fontSize` 를 그대로 두고 그릴 때만 줄였다). 그래서 자동 크기 글자에서 «우리가 바란 크기» 는
            // `fontSize` 가 아니라 <b>`fontSizeMax`</b> 다 — 하한을 `fontSize` 에만 걸면 다음 레이아웃에서 지워지고
            // 하한 게이트가 «미달» 로 센다(CI #455 에서 실제로 126줄이 그렇게 걸렸다).
            bool auto = t.enableAutoSizing;
            int size = TextSize.Floor(Mathf.Max(12, Mathf.RoundToInt(auto ? Mathf.Max(t.fontSize, t.fontSizeMax) : t.fontSize)));
            if (auto)
            {
                if (t.fontSizeMax < size) t.fontSizeMax = size;             // «올려도 되는 한계» 가 하한을 넘게(실제 크기는 칸이 정한다)
                t.fontSizeMin = TextSize.BestFitFloor(Mathf.Max(10, Mathf.RoundToInt(t.fontSizeMin)));
            }
            else if (size > t.fontSize) t.fontSize = size;                  // 하한만 올린다(조각이 더 크면 그대로)
            t.raycastTarget = false;
            EnsureOutline(t, size);
            return t;
        }
        /// <summary>
        /// T207 ② — <see cref="TextAnchor"/>(우리 코드가 주고받는 값) → TMP <see cref="TextAlignmentOptions"/>.
        /// <para>
        /// <b>왜 표를 태우나</b> — TMP 의 정렬은 «세로 비트 | 가로 비트» 인 <b>깃발</b>이고 uGUI 의 9칸 열거형과 값이 다르다.
        /// 우리 화면 코드는 <c>TextAnchor.MiddleCenter</c> 같은 값을 <b>백 군데 넘게</b> 주고받으므로 공개 API 를 그대로 두고
        /// <b>넣는 자리에서만</b> 이 표를 태운다(그 편이 diff 도 작고 되돌리기도 쉽다).
        /// </para>
        /// 세로 = <c>Top 0x100 · Middle 0x200 · Bottom 0x400</c> · 가로 = <c>Left 1 · Center 2 · Right 4</c> 라
        /// <c>TextAnchor</c> 의 줄·칸을 그대로 자리 이동시키면 된다(<see cref="MapAlign"/> 의 정확한 역방향이다).
        /// </summary>
        public static TextAlignmentOptions TmpAlign(TextAnchor a)
        {
            int v = (int)a; if (v < 0 || v > 8) v = (int)TextAnchor.MiddleCenter;
            return (TextAlignmentOptions)((0x100 << (v / 3)) | (1 << (v % 3)));
        }

        static TextAnchor MapAlign(TextAlignmentOptions a)
        {
            int v = (int)a; int h = v & 0xFF; int vv = v >> 8;
            int col = (h & 1) != 0 ? 0 : (h & 4) != 0 ? 2 : 1;                // left / right / center(+justified)
            int row = (vv & 1) != 0 ? 0 : (vv & 4) != 0 ? 2 : 1;              // top / bottom / middle(+midline·baseline)
            return (TextAnchor)(row * 3 + col);
        }

        /// <summary>이름 경로로 자식 찾기 — "A/B" 는 경로, 한 조각이면 이름으로 재귀 검색(비활성 포함). 없으면 null.</summary>
        public static Transform Find(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path)) return root;
            if (path.IndexOf('/') >= 0)
            {
                var cur = root; bool first = true;
                foreach (var seg in path.Split('/')) { cur = FindByName(cur, seg, first); first = false; if (cur == null) return null; }   // 첫 조각은 깊이 검색 · 뒤는 직계
                return cur;
            }
            return FindByName(root, path, true);
        }
        /// <summary>여러 이름 중 먼저 걸리는 것 (프리팹 변형은 인스턴스 이름이 덧씌워질 수 있어 둘 다 시도한다).</summary>
        public static Transform FindAny(Transform root, params string[] names) { foreach (var n in names) { var t = Find(root, n); if (t != null) return t; } return null; }
        static Transform FindByName(Transform t, string name, bool deep)
        {
            for (int i = 0; i < t.childCount; i++) if (t.GetChild(i).name == name) return t.GetChild(i);
            if (!deep) return null;
            for (int i = 0; i < t.childCount; i++) { var r = FindByName(t.GetChild(i), name, true); if (r != null) return r; }
            return null;
        }
        /// <summary>프리팹 안 글자 바꾸기. <paramref name="size"/> 를 주면 종류 하한(T63)으로 올려 넣고, bestFit 이면 최소도 <see cref="TextSize.BestFitMin"/> 으로. <paramref name="kind"/> 는 표식으로 남는다(게이트 판정).</summary>
        public static TMP_Text SetText(Transform root, string path, string s, Color? color = null, int? size = null, TextKind kind = TextKind.Body)
        {
            var t = Find(root, path); TMP_Text txt = null; if (t != null) { txt = t.GetComponent<TMP_Text>(); if (txt == null) txt = t.GetComponentInChildren<TMP_Text>(true); }
            if (txt == null) { Debug.LogWarning($"[UiKit] 글자 없음: {root.name}/{path}"); return null; }
            txt.text = TextGlyphs.Safe(s); if (color.HasValue) txt.color = color.Value;
            if (size.HasValue) { int sz = TextSize.Floor(size.Value, kind); txt.fontSize = sz; txt.fontSizeMax = sz; }
            if (txt.enableAutoSizing) txt.fontSizeMin = TextSize.BestFitFloor(Mathf.RoundToInt(txt.fontSizeMin), kind);
            EnsureOutline(txt);
            TextAudit.Mark(txt, kind);
            return txt;
        }
        public static Image SetSprite(Transform root, string path, string spriteKey, Color? tint = null)
        {
            var t = Find(root, path); var img = t != null ? t.GetComponent<Image>() : null;
            if (img == null) { Debug.LogWarning($"[UiKit] 이미지 없음: {root.name}/{path}"); return null; }
            if (spriteKey != null && Cat != null) img.sprite = Cat.Sprite(spriteKey);
            if (tint.HasValue) img.color = tint.Value;
            return img;
        }
        public static void Hide(Transform root, params string[] paths) { foreach (var p in paths) { var t = Find(root, p); if (t != null) t.gameObject.SetActive(false); } }
        public static void Show(Transform root, string path, bool on) { var t = Find(root, path); if (t != null) t.gameObject.SetActive(on); }

        /// <summary>아이콘 밑 라벨처럼 2줄로 접히는 짧은 글자의 줄 간격(<c>Text.lineSpacing</c> · T63) — Jua 의 줄 높이가 크기의 1.375 배라 1.0 이면 2줄이 칸을 넘친다 · 0.75 면 2줄 ≈ 크기 × 2.4.</summary>
        public const float CaptionLineSpacing = 0.75f;
        /// <summary>눌림 표시 배율(T22 · 주인 «모든 버튼에 눌림 표시») — 누르는 동안 그림을 이만큼 어둡게(ColorTint pressedColor) · 그림이 없는 히트 영역은 CanvasGroup alpha 를 이만큼.</summary>
        public const float PressedMul = 0.8f;
        /// <summary>눌림 지속 시간(초) — 손을 떼면 이 시간 안에 원래 색으로.</summary>
        public const float PressedFade = 0.05f;
        /// <summary>모든 버튼 공통 ColorTint 표 — 눌림만 어둡게(×<see cref="PressedMul"/>) · highlighted/selected 는 그대로(마우스 올림·선택 잔상 없음) · disabled 는 흰색(비활성 반투명은 <see cref="SetInteractable"/> 의 CanvasGroup 이 «지금처럼» 맡는다).</summary>
        public static ColorBlock PressColors
        {
            get
            {
                var c = ColorBlock.defaultColorBlock;
                c.normalColor = Color.white; c.highlightedColor = Color.white; c.selectedColor = Color.white; c.disabledColor = Color.white;
                c.pressedColor = new Color(PressedMul, PressedMul, PressedMul, 1f);
                c.colorMultiplier = 1f; c.fadeDuration = PressedFade;
                return c;
            }
        }

        /// <summary>
        /// GUI Pro 버튼 프리팹은 Button 컴포넌트가 없다 — 여기서 붙인다(DOTween 눌림 연출 + <b>눌림 표시</b> · T22).
        /// 눌림 표시 = <see cref="Selectable.Transition.ColorTint"/>(<see cref="PressColors"/> · 누르는 동안 ×0.8 어둡게). targetGraphic 은 ⓐ 이 오브젝트의 Image 가 보이면 그것
        /// ⓑ 투명 히트 영역(칸·카드·프리팹 버튼 루트 — 루트에 Image 가 없어 여기서 투명 Image 를 붙인 것)이면 <see cref="PressTarget"/> 이 고른 «보이는 첫 자식 Image»(버튼 배경·칸 프레임·탭 배경)
        /// ⓒ 자식에도 그림이 없으면(어둠 배경 같은 히트 영역) <see cref="PressFeedback"/> 이 누르는 동안 CanvasGroup alpha 를 ×0.8 로. 프리팹은 손대지 않는다(«그대로» 원칙 — 색만 곱한다).
        /// 비활성(interactable=false)은 지금처럼 <see cref="SetInteractable"/> 의 반투명(0.5) 그대로이고 눌러도 안 어두워진다.
        /// </summary>
        public static Button Clickable(Transform t, Action onClick, bool punch = true)
        {
            var go = t.gameObject;
            var img = go.GetComponent<Image>();
            if (img == null) { img = go.AddComponent<Image>(); img.color = new Color(1, 1, 1, 0); }   // 투명 히트 영역
            img.raycastTarget = true;
            var b = Ensure<Button>(go);
            b.transition = Selectable.Transition.ColorTint; b.colors = PressColors;
            b.targetGraphic = PressTarget(t, img);
            Ensure<PressFeedback>(go);   // 그림이 없을 때의 CanvasGroup 눌림 + 자식이 갈아엎힌 뒤(targetGraphic 파괴)의 재선택
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                if (punch) { t.DOKill(true); t.localScale = Vector3.one; t.DOPunchScale(new Vector3(-0.08f, -0.08f, 0), 0.18f, 1, 0.5f).SetUpdate(true).SetLink(go); }   // SetLink = 버튼이 클릭 직후 파괴돼도(인벤 재구성·팝업 갈아끼움) DOTween 이 먼저 죽인다(T56 · 콘솔 노란 줄 0)
                Audio.Sfx("snd.click");   // 모든 버튼의 클릭음은 여기 한 곳(T28)
                onClick?.Invoke();
            });
            return b;
        }
        /// <summary>눌림 색을 입힐 그림 — 루트 Image 가 보이면 그것, 아니면 «보이는(켜져 있고 알파 > 0) 첫 자식 Image»(계층 순서 · 루트~자식 사이가 전부 activeSelf 인 것만 — 루트 위쪽이 아직 꺼져 있어도 고를 수 있게). 없으면 루트의 (투명) Image 를 돌려준다(ColorTint 는 보이지 않고 <see cref="PressFeedback"/> 가 대신 어둡게 한다).</summary>
        public static Graphic PressTarget(Transform root, Image self)
        {
            if (self != null && self.enabled && self.color.a > 0.01f) return self;
            foreach (var im in root.GetComponentsInChildren<Image>(true))
            {
                if (im == null || im == self || !im.enabled || im.color.a <= 0.01f) continue;
                if (IsTextureLayer(im.name)) continue;
                if (!ActiveUpTo(im.transform, root)) continue;
                return im;
            }
            return self;
        }
        /// <summary>질감 3종(T72)이 깐 덧대기 조각인가 — 눌림 색을 여기 입히면 «버튼이 안 어두워진다»(알파 0.01~0.27 짜리 무늬·빛살·그라데이션이 targetGraphic 이 된다). 결정 170 이 ③ 을 버튼에 못 넣게 막았던 이유이고, 여기서 한 번 걸러서 푼다.</summary>
        public static bool IsTextureLayer(string name)
            => name == PatternName || name == LightName || name == LightMaskName || name == GradientTopName || name == GradientBottomName;
        /// <summary>x 에서 root 바로 아래까지 모든 오브젝트가 activeSelf 인가(root 자신과 그 위는 안 본다).</summary>
        static bool ActiveUpTo(Transform x, Transform root)
        {
            for (var p = x; p != null && p != root; p = p.parent) if (!p.gameObject.activeSelf) return false;
            return true;
        }
        /// <summary>눌림 표시가 실제로 보이는 그림이 있는가(targetGraphic 이 살아 있고 알파 > 0) — 테스트·감사용.</summary>
        public static bool HasVisiblePressTarget(Button b)
        {
            if (b == null || b.transition != Selectable.Transition.ColorTint) return false;
            var g = b.targetGraphic; return g != null && g.enabled && g.color.a > 0.01f && g.gameObject.activeInHierarchy;
        }
        public static void SetInteractable(Button b, bool on)
        {
            if (b == null) return; b.interactable = on;
            var cg = Ensure<CanvasGroup>(b); cg.alpha = on ? 1f : 0.5f;
        }

        /// <summary>프리팹 버튼 하나 세우기 — 스폰 · 글자 · <b>③ 그라데이션</b>(T72 3항) · 클릭.</summary>
        public static RectTransform Button(Transform parent, string prefabKey, string label, Action onClick, Layout.R? rect = null)
        {
            var go = Spawn(prefabKey, parent); var rt = (RectTransform)go.transform;
            if (rect.HasValue) Pct(rt, rect.Value);
            ButtonGradient(rt);
            var txt = go.GetComponentInChildren<TMP_Text>(true);
            // 버튼 글자 하한 = TextSize.Button(44 · T63) · bestFit 최소 32
            if (txt != null) { txt.text = TextGlyphs.Safe(label); txt.fontSize = TextSize.Floor(Mathf.RoundToInt(txt.fontSize), TextKind.Button); txt.enableAutoSizing = true; txt.fontSizeMin = TextSize.BestFitMin; txt.fontSizeMax = Mathf.Max(txt.fontSize, TextSize.Button); txt.textWrappingMode = TextWrappingModes.Normal; EnsureOutline(txt); TextAudit.Mark(txt, TextKind.Button); }
            Clickable(rt, onClick);
            return rt;
        }
        public static TMP_Text ButtonText(Component b) => b.GetComponentInChildren<TMP_Text>(true);

        /// <summary>
        /// ③ 그라데이션(T72 3항 · 우선순위 1 «주황/파랑/회색 버튼») — 프리팹 버튼의 <b>보이는 배경 그림 안쪽</b>에 아래 어둠 한 장(<see cref="BtnGradientKey"/> · Ink α <see cref="GradientBottomAlpha"/>)을 덧댄다.
        /// 레퍼런스 06 «Forge» · 13 «Summon»·«Upgrade All» 이 전부 위는 밝고 아래로 갈수록 어둡다.
        /// 배경 «안»(자식)에 넣는 이유 = 형제 순서를 안 건드리고도 «배경 위 · 글자 아래» 가 성립하기 때문이다(글자는 배경의 형제라 배경 서브트리 전체보다 뒤에 그려진다).
        /// 배경 그림이 없는 투명 히트 영역(칸·카드 루트)이면 덧댈 바탕이 없어 넣지 않는다. 눌림 색은 <see cref="PressTarget"/> 이 질감 조각을 건너뛰어 그대로 배경에 입는다(결정 170 해제).
        /// </summary>
        public static void ButtonGradient(RectTransform rt)
        {
            if (rt == null) return;
            var bg = PressTarget(rt, rt.GetComponent<Image>()) as Image;
            if (bg == null || !bg.enabled || bg.color.a <= 0.01f) return;
            // T116 실측 — 레퍼런스의 버튼은 사실상 단색이라(START #FB9F00 · Upgrade #FB9F00 · 광고 #188AFA→#096CFD) 덧칠을 얕게 깐다
            Gradient(bg.rectTransform, bottom: Palette.A(Palette.Ink, GradientFlatBottomAlpha), topKey: null, bottomKey: BtnGradientKey);
        }

        /// <summary>색 변형이 없는 프리팹(CardFrame_04/ItemFrame_04 는 Gray 가 없다)을 회색 등급용으로 — 모든 Image 색을 같은 밝기의 무채색으로 바꾼다(알파 유지 · 흰색은 그대로).</summary>
        public static void Desaturate(Transform root)
        {
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                var c = img.color; float l = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                if (c.r > 0.97f && c.g > 0.97f && c.b > 0.97f) continue;
                img.color = new Color(l * 0.92f, l * 0.90f, l * 0.90f, c.a);
            }
        }
        /// <summary>ItemFrame_04 프리팹의 본래 폭(162 · 높이 165) — 자식(Border 162 · InnerBorder 134 · Icon 128 · Light/Shadow ±53)이 전부 가운데 앵커 고정 크기다.</summary>
        public const float PerkFrameNativeW = 162f, PerkFrameNativeH = 165f;
        /// <summary>특전 등급 프레임(팔각 ItemFrame_04_*) 하나 — 색 이름은 <see cref="Palette.PerkGradeName"/> · gray 는 무채색화. 안에 아이콘을 넣어 돌려준다.
        /// size = 화면에 보일 폭. 프리팹 내부는 고정 크기라 sizeDelta 를 줄여도 테두리·아이콘이 안 줄어든다(T13 · 특전 줄에서 78px 셀에 162px 프레임이 그려져 서로 겹쳤다) → 본래 크기를 두고 <b>배율</b>로 맞춘다(프리팹 «그대로»).</summary>
        public static RectTransform PerkFrame(Transform parent, string colorName, string iconKey, float size)
        {
            var f = Spawn(Palette.FrameKey("ui.itemFrame4", colorName), parent); var rt = (RectTransform)f.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
            float nativeW = rt.sizeDelta.x > 1f ? rt.sizeDelta.x : PerkFrameNativeW, nativeH = rt.sizeDelta.y > 1f ? rt.sizeDelta.y : PerkFrameNativeH;   // 프리팹이 없을 때(빈 Rect)만 기본값
            rt.sizeDelta = new Vector2(nativeW, nativeH);
            float s = size / nativeW; rt.localScale = new Vector3(s, s, 1f);
            if (colorName == "gray") Desaturate(rt);
            var icon = Find(rt, "Icon");
            if (icon != null) SetSprite(rt, "Icon", iconKey, Palette.White);
            else { var ic = Icon(rt, "Icon", iconKey, Palette.White); Pct(ic.rectTransform, 22, 22, 56, 56); }
            return rt;
        }

        /// <summary>컴포넌트가 없으면 붙인다. ⚠ `GetComponent() ?? AddComponent()` 는 에디터에서 «가짜 null»(== 만 재정의) 때문에 AddComponent 가 안 돌아 MissingComponentException 이 난다 — 반드시 이걸 쓴다.</summary>
        /// <summary>표의 글자 높이(프레임 %) → uGUI 글자 크기(px). 글자 덩어리(preferredHeight ≈ 크기 × 1.17)가 그 높이가 되게 0.85 를 곱한다 — 픽셀 상수 대신 표에서 계산(T47 회차 3 · «챕터 제목»).</summary>
        public static int FontForHeight(float hPct) => Mathf.RoundToInt(FrameH * hPct / 100f * 0.85f);
        /// <summary>UI 비평 판정 요소 이름표(T46) — name 은 docs/ref-layout.md 표의 «요소» 열과 글자까지 같게. 자기 사각형을 잰다. <paramref name="textBounds"/> 면 rect 대신 글자 덩어리(uGUI Text preferred 크기 · 정렬 자리)를 잰다(T47 ⓒ · «챕터 제목»).</summary>
        public static UiTag Tag(Transform t, string name, bool textBounds = false) { if (t == null) return null; var tag = Ensure<UiTag>(t.gameObject); tag.Name = name; tag.Members.Clear(); tag.TextBounds = textBounds; return tag; }
        /// <summary>«줄(N칸)» 행의 이름표 — host 아래에 빈 Rect 를 하나 두고 members 사각형의 합집합(⊕)을 잰다(ref-layout ⚑U03 ⓒ).</summary>
        public static UiTag TagGroup(Transform host, string name, params RectTransform[] members)
        {
            if (host == null) return null;
            var r = Rect(host, "Tag:" + name); r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.sizeDelta = Vector2.zero;
            var tag = Tag(r, name); foreach (var m in members) if (m != null) tag.Members.Add(m);
            return tag;
        }
        public static T Ensure<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }
        public static T Ensure<T>(Component on) where T : Component => Ensure<T>(on.gameObject);

        public static void Destroy(Transform t) { if (t != null) UnityEngine.Object.Destroy(t.gameObject); }
        /// <summary>자식 전부 파괴 — 파괴 전에 그 자식들을 겨냥한 트윈을 먼저 죽인다(T49 · 파괴된 오브젝트를 만지는 트윈 = 콘솔 경고 · safeMode 가 조용히 삼키지만 남기지 않는다).</summary>
        /// <summary>자식 전부 제거 — 트윈을 먼저 죽이고(T49), **트리에서 떼어 낸 뒤** 파괴한다(T55 · 결정 80 규칙): <c>Destroy</c> 는 프레임 끝에 실제로 지우므로 같은 프레임의 <c>childCount</c>·<c>Find</c> 가 옛 자식(프리팹 샘플 카드 등)을 다시 보지 않게.</summary>
        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i); KillTweens(c);
                c.SetParent(null, false); c.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }
        /// <summary>root 와 그 아래 모든 Transform·CanvasGroup·Graphic 을 겨냥한 트윈을 죽인다(완료 콜백 없이). 시퀀스는 <c>SetTarget</c> 으로 묶은 대상이 있을 때만 잡힌다 — Overlay 는 자기 마스터 시퀀스를 따로 Kill 한다.</summary>
        public static void KillTweens(Transform root)
        {
            if (root == null) return;
            foreach (var tr in root.GetComponentsInChildren<Transform>(true)) DOTween.Kill(tr);
            foreach (var cg in root.GetComponentsInChildren<CanvasGroup>(true)) DOTween.Kill(cg);
            foreach (var g in root.GetComponentsInChildren<Graphic>(true)) DOTween.Kill(g);
        }

        // ───────────────────────── 연출 ─────────────────────────
        // T49(주인 2026-09-06 «팝업 뜰 때 순서대로 DOTween») — 등장 연출 타이밍 상수는 여기 한 곳(밸런스 수치가 아니라 연출 상수 · 워커 결정 기록 83).
        /// <summary>요소 하나가 뜨는 시간(초 · unscaled).</summary>
        public const float RevealDur = 0.22f;
        /// <summary>등장 시작 스케일(→ 1 · OutBack).</summary>
        public const float RevealFrom = 0.86f;
        /// <summary>카드/줄이 하나씩 뜨는 간격(초). 3택 = 0.22 + 2×0.11 + 0.22 = 0.66s 에 마지막 카드가 다 뜬다.</summary>
        public const float RevealStep = 0.11f;
        /// <summary>팝업 연출 총 길이 상한 — 3택 0.8s · 승/패 1.0s(ROUTINE T49.5 «길면 답답»).</summary>
        public const float RevealMaxPick = 0.8f, RevealMaxResult = 1.0f;

        public static void PopIn(RectTransform rt, float from = 0.82f, float dur = 0.28f, float delay = 0f)
        {
            rt.DOKill(); rt.localScale = Vector3.one * from; rt.DOScale(1f, dur).SetEase(Ease.OutBack).SetDelay(delay).SetUpdate(true).SetLink(rt.gameObject);
        }
        public static void FadeIn(Graphic g, float to, float dur = 0.25f, float delay = 0f)
        {
            var c = g.color; g.color = new Color(c.r, c.g, c.b, 0); g.DOFade(to, dur).SetDelay(delay).SetUpdate(true).SetLink(g.gameObject);
        }
        /// <summary>
        /// 요소 하나의 «등장» 트윈(T49) — 지금 바로 안 보이게(CanvasGroup α 0 · 스케일 <paramref name="from"/> · 클릭 막음) 만들고, 재생되면 α 0→1 + 스케일 →1(OutBack) 뒤 클릭을 연다.
        /// 돌려준 Sequence 를 마스터 시퀀스에 <c>Insert(시각, …)</c> 하면 그 시각에 뜬다(Overlay.At) · 그냥 두면 즉시 재생(unscaled). 완료 콜백(클릭 열기)은 <c>Complete(true)</c>/정상 완료 때 돈다 — Kill 이면 안 돌지만 그때는 오브젝트도 사라진다.
        /// </summary>
        public static Sequence Reveal(RectTransform rt, float from = RevealFrom, float dur = RevealDur)
        {
            var cg = Ensure<CanvasGroup>(rt.gameObject); cg.alpha = 0f; cg.blocksRaycasts = false;
            rt.DOKill(); rt.localScale = Vector3.one * from;
            var s = DOTween.Sequence().SetUpdate(true).SetTarget(rt).SetLink(rt.gameObject);   // SetLink(T56) — 마스터에 Insert 되면 마스터의 링크·Kill 이 대신 지킨다 · 단독 재생이면 이 링크가
            s.Insert(0, cg.DOFade(1f, dur)); s.Insert(0, rt.DOScale(1f, dur).SetEase(Ease.OutBack));
            s.OnComplete(() => { if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; } if (rt != null) rt.localScale = Vector3.one; });
            return s;
        }
        /// <summary><paramref name="items"/> 를 <paramref name="start"/> 부터 <paramref name="step"/> 간격으로 하나씩 <see cref="Reveal"/> — 마스터 시퀀스에 Insert. 돌려주는 값 = 마지막 요소가 다 뜨는 시각.</summary>
        public static float Stagger(Sequence master, IList<RectTransform> items, float start, float step, float from = RevealFrom, float dur = RevealDur)
        {
            float t = start, end = start;
            for (int i = 0; i < items.Count; i++)
            {
                var rt = items[i]; if (rt == null) continue;
                master.Insert(t, Reveal(rt, from, dur)); end = t + dur; t += step;
            }
            return end;
        }

        // ───────────────────────── Shine(T61 · 주인 2026-09-06 «특전 순서대로 등장할 때 shine 효과도 순서대로») ─────────────────────────
        // 카드 프레임 조각(CardFrame_04_*) 의 Image 전부에 mat.perkShine(AllIn1SpriteShaderUiMask · SHINE_ON) 인스턴스를 붙이고, 카드 i 의 Reveal 시작 + ShineLead 에 _ShineLocation 을 ShineFrom→ShineTo 로 한 번 훑는다.
        // 카드 하나 = 머티리얼 인스턴스 하나(MaterialOwner 가 카드 파괴 때 인스턴스도 Destroy · 누수·경고 0). 타이밍 상수는 연출 상수(밸런스 아님 · 워커 결정 기록).
        /// <summary>카드 Reveal 시작 뒤 shine 이 출발하기까지(초). 3택 = 0.22+0.08 · 0.33+0.08 · 0.44+0.08 → 카드가 뜨는 중에 빛이 지나가기 시작한다.</summary>
        public const float ShineLead = 0.08f;
        /// <summary>빛이 카드를 한 번 훑는 시간(초 · InOutSine). 마지막 카드의 꼬리 = 0.44+0.08+0.36 = 0.88s(클릭 열림 0.66·하단 0.77 은 그대로).</summary>
        public const float ShineDur = 0.36f;
        /// <summary>_ShineLocation 의 시작/끝 — 폭(0.12)만큼 카드 밖에서 출발해 밖으로 나간다(0/1 이면 모서리에 빛 조각이 남는다).</summary>
        public const float ShineFrom = -0.2f, ShineTo = 1.2f;
        public static readonly int ShineLocationId = Shader.PropertyToID("_ShineLocation");
        /// <summary>빛이 지나가는 속도 곡선(T153 ⓑ · 주인 «쭉 지나가는») — 종전 <c>InOutSine</c> 은 가장자리에서 느리고 가운데서 빨라 «일정하게» 가 아니다. 등속이 <see cref="Ease.Linear"/> 다.</summary>
        public const Ease ShineEase = Ease.Linear;
        // ───────────────────────── «아이콘 + 글자» 제목 줄 (T170 · 주인 2026-09-07 10:0X «타이틀이 왼쪽으로 치우친다 · 모든 타이틀 다 점검») ─────────────────────────
        /// <summary>제목 줄의 아이콘 폭 / 아이콘과 글자 사이 간격(줄 폭 %) — <see cref="CenterIconTitle"/> 의 «가운데 덩어리» 계산에 쓴다.</summary>
        public const float TitleIconPct = 16f, TitleGapPct = 2f;
        /// <summary>
        /// «아이콘 + 글자» 를 <b>한 덩어리로</b> 줄 가운데에 놓는다(T170 · T101 ⓓ 를 전 화면 공용으로 올린 것).
        /// <para>
        /// 치우침의 뿌리는 <b>아이콘 rect 와 글자 rect 를 따로 놓는 것</b>이다 — 아이콘을 줄 왼쪽 끝(x 0)에 못 박고 글자를 그 옆에서 <b>왼쪽 정렬</b>하면
        /// 줄은 가운데라도 <b>보이는 덩어리</b>는 왼쪽에 쏠린다(특권 ⭐·던전·PvP·아레나 티어가 전부 그 꼴이었다).
        /// 그래서 글자 폭을 <see cref="TextWidthAtFullSize"/> 로 <b>실측</b>해(제 크기로 그릴 때의 폭 · <b><see cref="Text.preferredWidth"/> 는 쓰지 않는다</b> — 그 값은 «지금 줄어든 크기» 로 재므로 되먹임이 생긴다 · 결정 490) «아이콘 + 간격 + 글자» 의 합을 구하고, 그 합을 줄 가운데에 놓는다 —
        /// 글자 길이가 달라도(«던전»·«PvP»·«브론즈»·«특권») 각자 가운데다.
        /// </para>
        /// <para>
        /// ⚠ 주인이 말한 «TMPro 인라인 아이콘»(글자 안에 그림을 넣는 길)은 <b>지금 못 한다</b> — 이 프로젝트는 TMP 를 전부 uGUI <see cref="Text"/>(Jua)로 바꿔 쓰고
        /// (T63 아웃라인 규약이 그 위에 있다) <b>Jua SDF 폰트 애셋이 없어</b> TMP 로 두면 한글이 두부가 된다. SDF 만들기는 에디터 작업(주인)이다.
        /// 그래서 «덩어리를 가운데로» 로 같은 그림을 낸다.
        /// </para>
        /// <paramref name="rowWPct"/> 는 그 줄의 <b>프레임 대비 폭 %</b>(rect 를 안 읽으므로 배치 전에 불러도 된다).
        /// </summary>
        /// <summary>
        /// 그 글자가 <b>제 크기(<see cref="Text.fontSize"/>)로</b> 그려질 때 필요한 폭(프레임 단위 px) — <see cref="CenterIconTitle"/> 이 칸을 잡을 때 쓴다.
        /// <para>
        /// <b>왜 <see cref="Text.preferredWidth"/> 를 안 쓰나(T170 회차 3 · 결정 490)</b> — bestFit 글자에서 그 값은 «지금 rect 안에서 줄어든 크기» 로 잰 폭이다.
        /// 그 폭으로 rect 를 다시 잡으면 <b>작아진 폭 → 더 좁은 칸 → 더 작은 글자</b> 로 스스로 주저앉는 되먹임이 생긴다
        /// (특권 제목이 60 이 아니라 <b>55</b> 로 굳어 있던 까닭이 이것이다 — 칸이 «55 로 그린 폭» 과 정확히 같아 60 이 다시는 안 들어갔다).
        /// 그래서 <b>넉넉한 칸</b>을 주고 재 «최대 크기(= fontSize)로 그릴 때의 폭» 을 얻는다(<see cref="TextAudit.BestFitSize"/> 와 같은 방법 · 새 <see cref="TextGenerator"/>).
        /// </para>
        /// </summary>
        public static float TextWidthAtFullSize(TMP_Text text)
        {
            if (text == null || string.IsNullOrEmpty(text.text)) return 0f;
            // T207 ② — TMP 는 «이 글자를 제 크기로 그리면 얼마나 넓은가» 를 스스로 답한다(GetPreferredValues).
            // 넉넉한 칸을 주는 까닭은 그대로다: 칸이 좁으면 자동 크기가 글자를 줄여 «제 크기» 가 아닌 폭이 나온다.
            return text.GetPreferredValues(FrameW * 4f, FrameH * 4f).x;
        }

        public static void CenterIconTitle(RectTransform icon, TMP_Text text, float rowWPct, float iconPct = TitleIconPct, float gapPct = TitleGapPct)
        {
            if (icon == null || text == null) return;
            float rowPx = Mathf.Max(1f, rowWPct / 100f * FrameW);
            // 4% 여유 — bestFit 은 «칸 안에 들어가야» 고르므로 칸이 필요 폭과 «딱» 같으면 반올림 한 픽셀에 한 단계 줄어든다.
            // 여유는 덩어리 폭에 함께 들어가므로 가운데 계산(TitleBlockOffsetPct = 0)은 그대로다(글자는 왼쪽 정렬이라 오른쪽 여백만 는다).
            float textPct = Mathf.Clamp(TextWidthAtFullSize(text) * 1.04f / rowPx * 100f, 5f, 100f - iconPct - gapPct);
            float startPct = Mathf.Max(0f, (100f - (iconPct + gapPct + textPct)) * 0.5f);
            Pct(icon, startPct, -10, iconPct, 120);
            // 세로는 «부르는 쪽이 잡아 둔 그대로» 둔다(가로만 가운데로 옮기는 함수다 · T170 회차 3 · 결정 490).
            // 여기서 0/100 을 못 박았더니 특권(11) 제목이 120% 짜리 칸(3.0%×1.2 = 84px)에서 100%(70px)로 낮아졌고,
            // bestFit 이 60 을 70px 안에 못 넣어 55 로 줄여 그렸다 — 화면은 «가운데» 가 됐는데 글자만 작아진 것이다.
            var trt = text.rectTransform;
            float textY = (1f - trt.anchorMax.y) * 100f, textH = Mathf.Max(1f, (trt.anchorMax.y - trt.anchorMin.y) * 100f);
            Pct(trt, startPct + iconPct + gapPct, textY, textPct, textH);
        }

        /// <summary>
        /// 제목 덩어리가 줄 가운데에서 얼마나 벗어났나(%p · 왼쪽 여백 − 오른쪽 여백 · 0 이면 정확히 가운데) — <see cref="CenterIconTitle"/> 의 <b>짝이 되는 자</b>다(T170).
        /// 재는 것과 놓는 것을 같은 파일에 두어, 계산이 바뀌면 게이트도 같이 따라오게 한다(테스트가 앵커 산수를 제 손으로 다시 쓰면 둘이 갈라진다).
        /// </summary>
        public static float TitleBlockOffsetPct(RectTransform icon, TMP_Text text)
        {
            if (icon == null || text == null) return 0f;
            float left = icon.anchorMin.x * 100f, right = 100f - text.rectTransform.anchorMax.x * 100f;
            return left - right;
        }

        /// <summary>«이 카드의 shine 머티리얼 인스턴스» 표식 — 카드가 파괴되면 인스턴스도 파괴한다(UI Image 는 MaterialPropertyBlock 을 못 쓰므로 인스턴스가 필요하다 · 인스턴스는 자기 이름이 «PerkShine (Instance)»).</summary>
        public sealed class MaterialOwner : MonoBehaviour
        {
            public Material Mat;
            void OnDestroy() { if (Mat != null) UnityEngine.Object.Destroy(Mat); Mat = null; }
        }
        /// <summary><paramref name="frameRoot"/>(카드 프레임 조각) 아래 모든 Image 에 mat.perkShine 인스턴스를 붙이고 <paramref name="owner"/>(카드 루트)에 <see cref="MaterialOwner"/> 로 매단다.
        /// 글자·아이콘엔 안 붙인다(프레임 조각 안 Image 만 · T52 «한 색» 그대로). 카탈로그에 머티리얼이 없으면 null(연출은 그대로 · 빛만 없음).</summary>
        public static Material ShineMaterial(Transform frameRoot, Transform owner)
        {
            var src = App.I != null ? App.I.Assets.Material("mat.perkShine") : null;
            if (src == null || frameRoot == null || owner == null) return null;
            var inst = new Material(src) { name = src.name + " (Instance)" };
            inst.SetFloat(ShineLocationId, ShineFrom);
            var one = ShineTarget(frameRoot);
            if (one != null) one.material = inst;
            var mo = Ensure<MaterialOwner>(owner.gameObject); mo.Mat = inst;
            return inst;
        }

        /// <summary>
        /// 빛을 물릴 <b>한 장</b>(T153 · 주인 2026-09-07 07:0X «샤인이 일정한 두께로 쭉 지나가는 효과인데 … 얇게 하다가 존나 두껍게 하다가 얇게 하다가 끝남»).
        /// <para>
        /// 종전에는 조각 아래 <b>모든</b> Image(카드 조각이면 <c>Bg</c>·<c>InnerBorder</c>·<c>Border</c>·<c>TitleBg</c>·<c>TitleBorder</c> 다섯 장)에 같은 인스턴스를 물렸다.
        /// 셰이더의 <c>_ShineLocation</c>·<c>_ShineWidth</c> 는 <b>UV(0~1) 기준</b>이라 화면에서 보이는 띠의 폭·자리가 <b>그 Image 의 폭에 비례</b>한다 —
        /// 층마다 폭이 다르니 <b>띠가 다섯 개, 서로 다른 굵기로 서로 다른 속도로</b> 지나가고, 갈라졌을 때는 얇고 겹칠 때는 뭉쳐 두꺼워 보였다. 그것이 주인이 본 «얇→두꺼→얇» 이다.
        /// </para>
        /// 그래서 <b>가장 큰 한 장</b>(= 카드 몸통 · 같은 크기면 <c>Bg</c> 를 먼저)에만 물린다. 층마다 물리려면 Image 마다 인스턴스를 만들어 폭·위치를 그 층 크기로 환산해야 하는데
        /// 값이 다섯 벌이 되므로 지시서가 권하지 않았다(T153 1항).
        /// </summary>
        public static Image ShineTarget(Transform frameRoot)
        {
            if (frameRoot == null) return null;
            // T153 회차 2 — «몸통(Bg)» 이 있으면 그 한 장이다. 회차 1 은 «면적이 가장 큰 한 장» 이었는데
            // CI #359 에서 카드 조각의 가장 큰 Image 가 Bg 가 **아니었다**(테두리·그림자 쪽이 더 넓다) —
            // 그러면 빛이 «테두리 링» 만 훑어 주인이 본 «얇은 띠» 가 그대로 남는다. 빛이 지나갈 면은 몸통이다.
            Image bg = null, best = null; float bestArea = -1f;
            foreach (var img in frameRoot.GetComponentsInChildren<Image>(true))
            {
                if (img == null) continue;
                if (bg == null && img.name == "Bg") bg = img;
                var r = ((RectTransform)img.transform).rect;
                float area = Mathf.Abs(r.width * r.height);
                if (area > bestArea + 0.5f) { best = img; bestArea = area; }
            }
            return bg != null ? bg : best;   // 몸통이 없는 조각(로비 챕터 카드 등)에서는 가장 넓은 한 장
        }
        /// <summary><paramref name="inst"/> 의 _ShineLocation 을 <paramref name="at"/> 초부터 <see cref="ShineDur"/> 동안 <see cref="ShineFrom"/>→<see cref="ShineTo"/> 로 — 마스터 시퀀스에 Insert(스킵·CompleteAll 이면 끝 값 = 화면 밖). 돌려주는 값 = 끝나는 시각.</summary>
        public static float Shine(Sequence master, Material inst, Transform link, float at)
        {
            if (inst == null || master == null) return at;
            float v = ShineFrom; inst.SetFloat(ShineLocationId, v);
            var tw = DOTween.To(() => v, x => { v = x; if (inst != null) inst.SetFloat(ShineLocationId, x); }, ShineTo, ShineDur).SetEase(ShineEase).SetUpdate(true).SetTarget(inst);
            if (link != null) tw.SetLink(link.gameObject);   // SetLink(T56) — 마스터에 Insert 되면 마스터의 링크·Kill 이 대신 지킨다
            master.Insert(at, tw);
            return at + ShineDur;
        }
        /// <summary>
        /// T153 2항 — 빛이 진행률 <paramref name="t01"/>(0~1) 에서 <b>실제로 어디까지</b> 갔는가(<see cref="ShineFrom"/>~<see cref="ShineTo"/>).
        /// <para>
        /// 자(테스트)가 «등속인가» 를 이징 <b>이름</b>이 아니라 값으로 재려면 이징 함수를 불러야 하는데, PlayMode 테스트 어셈블리는
        /// <b>DOTween 을 참조하지 않는다</b>(<c>overrideReferences: true</c> · precompiled 는 nunit 하나뿐). 그래서 «DG 를 쓰는 부분» 은
        /// 여기(=DOTween 을 참조하는 어셈블리)에 두고 자에게는 <b>float</b> 만 돌려준다 — 자가 세 곳을 재서 간격이 같은지 본다(결정 465).
        /// </para>
        /// </summary>
        /// <summary>빛의 속도 곡선 이름(«Linear» …) — 자가 <see cref="ShineEase"/> 를 직접 만지려면 DOTween 을 참조해야 하는데 테스트 어셈블리는 안 한다(결정 465).</summary>
        public static string ShineEaseName => ShineEase.ToString();
        public static float ShineEasedAt(float t01) => DOVirtual.EasedValue(ShineFrom, ShineTo, Mathf.Clamp01(t01), ShineEase);
        /// <summary>«가만히 있는» 그림에 빛이 지나가는 주기(초 · T166 ⓑ · 주인 2026-09-07 09:2X «챕터 카드에 5초마다 shine»). 연출 상수 · 밸런스 아님.</summary>
        public const float ShinePeriod = 5f;
        /// <summary>
        /// <paramref name="inst"/> 의 빛을 <paramref name="period"/> 초마다 한 번씩 지나가게 한다(T166 ⓑ) — <see cref="Shine"/> 은 마스터 시퀀스에 한 번 Insert 하는
        /// «등장 연출» 이라 되풀이가 없다. 여기는 제 시퀀스를 만들어 <b>무한 루프</b>한다(질감 3종 = 패턴 흐름·빛살 회전과 같은 갈래).
        /// <para>
        /// <b>첫 훑기는 한 주기 뒤에 온다</b>(<c>PrependInterval</c>) — 까닭 둘: ⓐ 주인 말 «5초마다» 를 그대로 읽으면 첫 번도 5초 뒤다
        /// ⓑ 화면을 세우자마자 찍는 <see cref="PlayShot"/> PNG 가 <b>훑는 중간</b>을 물지 않는다(무한 루프는 <see cref="CompleteAllTweens"/> 가 못 끝낸다 ·
        /// 그러면 `screens` 비평 그림이 회차마다 밝기가 달라진다 · 결정 기록).
        /// </para>
        /// unscaled(일시정지에도 돈다) · <paramref name="link"/> 가 사라지면 트윈도 같이 죽는다(SetLink · T56) · 머티리얼 인스턴스는 <see cref="MaterialOwner"/> 가 치운다.
        /// </summary>
        public static Sequence ShineLoop(Material inst, Transform link, float period = ShinePeriod)
        {
            if (inst == null) return null;
            inst.SetFloat(ShineLocationId, ShineFrom);
            float gap = Mathf.Max(0.01f, period - ShineDur);
            var seq = DOTween.Sequence().SetUpdate(true).SetTarget(inst);
            seq.AppendInterval(gap);
            seq.Append(DOTween.To(() => inst != null ? inst.GetFloat(ShineLocationId) : ShineFrom, x => { if (inst != null) inst.SetFloat(ShineLocationId, x); }, ShineTo, ShineDur).SetEase(Ease.InOutSine));
            seq.AppendCallback(() => { if (inst != null) inst.SetFloat(ShineLocationId, ShineFrom); });
            seq.SetLoops(-1);
            if (link != null) seq.SetLink(link.gameObject);
            return seq;
        }
        /// <summary><see cref="Stagger"/> 와 같은 <paramref name="start"/>·<paramref name="step"/> 으로 카드마다 shine 을 뒤따르게 한다(카드 i = start + i·step + <see cref="ShineLead"/>) — «등장 순서 = 반짝임 순서».
        /// 카드에 <see cref="MaterialOwner"/>(= <see cref="ShineMaterial"/>) 가 없으면 건너뛴다. <paramref name="starts"/> 에 시작 시각을 순서대로 적어 준다(테스트 · 단조 증가 계약). 돌려주는 값 = 마지막 shine 이 끝나는 시각.</summary>
        public static float StaggerShine(Sequence master, IList<RectTransform> cards, float start, float step, IList<float> starts = null)
        {
            float t = start, end = start;
            for (int i = 0; i < cards.Count; i++)
            {
                var rt = cards[i]; if (rt == null) continue;
                var mo = rt.GetComponent<MaterialOwner>();
                if (mo != null && mo.Mat != null) { float at = t + ShineLead; end = Shine(master, mo.Mat, rt, at); starts?.Add(at); }
                t += step;
            }
            return end;
        }
        /// <summary>도는 트윈 전부 완료(완료 콜백 포함) — 테스트·비평 스크린샷(PlayShot)이 연출 중간을 보지 않게(T49). PlayMode 테스트 어셈블리는 DOTween 을 직접 참조하지 않아 여기로.</summary>
        public static int CompleteAllTweens() => DOTween.CompleteAll(true);
        /// <summary><paramref name="target"/> 을 겨냥한 살아 있는 트윈/시퀀스가 있는가(테스트용 · Close 뒤 0 계약).</summary>
        public static bool IsTweening(object target) => target != null && DOTween.IsTweening(target);
        /// <summary>이 target 에 걸린 트윈 수(멈춘 것 포함 · T174 «칸마다 트윈 하나» 를 재는 자리). 테스트 어셈블리가 DOTween 을 직접 안 부르게 여기 둔다.</summary>
        public static int TweenCountOn(object target)
        {
            if (target == null) return 0;
            var list = DOTween.TweensByTarget(target, true);
            return list != null ? list.Count : 0;
        }
        /// <summary>
        /// 지금 «도는» 트윈 수(T129 ⓑ 계측 · 멈춰 있는 것은 안 센다). 질감 3종(패턴 uvRect 흐름 · 빛살 회전)은 <b>무한 루프</b>라
        /// 화면이 서 있는 동안 계속 프레임을 먹는다 — 로비 fps 가 회차마다 내려온 원인을 «몇 개가 도나» 로 먼저 세려는 자다.
        /// 판정에 쓰지 않는다(수를 줄이는 것은 다음 회차 · 여기서는 재기만 한다). PlayMode 테스트 어셈블리가 DOTween 을 직접 참조하지 않아 여기 둔다.
        /// </summary>
        public static int PlayingTweens() => DOTween.TotalPlayingTweens();
        /// <summary>소리·눌림 없는 탭 영역(연출 스킵용 · T49) — Button 을 만들지 않는다(PressFeedbackTests 의 «모든 Button 은 눌림 표시» 계약 밖). 테스트는 <see cref="Tap.Fire"/> 로 누른다.</summary>
        public static Tap OnTap(Transform t, Action onTap)
        {
            var img = t.GetComponent<Image>(); if (img == null) { img = t.gameObject.AddComponent<Image>(); img.color = new Color(1, 1, 1, 0); }
            img.raycastTarget = true;
            var tap = Ensure<Tap>(t.gameObject); tap.Handler = onTap; return tap;
        }
        public sealed class Tap : MonoBehaviour, IPointerClickHandler
        {
            public Action Handler;
            public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) Fire(); }
            public void Fire() => Handler?.Invoke();
        }

        // ───────────────────────── 숫자 표기 ─────────────────────────
        public static string Fmt(double n)
        {
            n = Math.Round(n);
            double a = Math.Abs(n);
            if (a >= 1e12) return (n / 1e12).ToString("0.##") + "T";
            if (a >= 1e9) return (n / 1e9).ToString("0.##") + "B";
            if (a >= 1e6) return (n / 1e6).ToString("0.##") + "M";
            if (a >= 1e4) return (n / 1e3).ToString("0.#") + "K";
            return n.ToString("#,0");
        }
        public static string FmtQty(double n) => Math.Round(n).ToString("0");
        /// <summary>천 단위 콤마만(«11,253») — <see cref="Fmt"/> 처럼 K·M 으로 줄이지 않는다. 레퍼런스가 전체 자릿수를 그대로 보여 주는 자리에 쓴다(아레나 전투력·승점 · T81).</summary>
        public static string FmtComma(double n) => Math.Round(n).ToString("#,0");
    }

    /// <summary>
    /// 눌림 표시 보조(T22) — <see cref="UiKit.Clickable"/> 가 모든 버튼에 붙인다.
    /// ⓐ Button 의 targetGraphic 이 투명(그림이 없는 히트 영역)이거나 파괴됐으면(칸·줄을 갈아엎은 뒤) 누르는 순간 <see cref="UiKit.PressTarget"/> 으로 다시 고르고,
    ///    그래도 보이는 그림이 없으면 누르는 동안 CanvasGroup alpha 를 ×<see cref="UiKit.PressedMul"/> 로 낮춘다(손을 떼거나 밖으로 나가면 복원).
    /// ⓑ interactable=false 면 아무것도 안 한다(비활성 반투명 그대로). 프리팹·자식은 손대지 않는다.
    /// </summary>
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Button _btn; CanvasGroup _cg; bool _dimmed; float _baseAlpha = 1f;
        /// <summary>지금 CanvasGroup 으로 어둡게 하는 중인가(테스트용).</summary>
        public bool Dimmed => _dimmed;

        Button Btn => _btn != null ? _btn : (_btn = GetComponent<Button>());

        public void OnPointerDown(PointerEventData e)
        {
            var b = Btn; if (b == null || !b.IsInteractable() || e.button != PointerEventData.InputButton.Left) return;
            if (b.transition == Selectable.Transition.ColorTint && !UiKit.HasVisiblePressTarget(b))
            {
                var self = GetComponent<Image>(); var g = UiKit.PressTarget(transform, self);
                if (g != null && g != b.targetGraphic) { b.targetGraphic = g; b.OnPointerDown(e); }   // 다시 고른 그림에 pressed 색을 바로 입힌다(Selectable 은 targetGraphic 교체를 스스로 모른다)
            }
            if (UiKit.HasVisiblePressTarget(b)) return;   // 색으로 보이면 alpha 는 안 건드린다
            _cg = UiKit.Ensure<CanvasGroup>(gameObject);
            if (!_dimmed) { _baseAlpha = _cg.alpha; _cg.alpha = _baseAlpha * UiKit.PressedMul; _dimmed = true; }
        }
        public void OnPointerUp(PointerEventData e) => Restore();
        public void OnPointerExit(PointerEventData e) => Restore();
        void OnDisable() => Restore();
        void Restore()
        {
            if (!_dimmed) return;
            _dimmed = false; if (_cg != null) _cg.alpha = _baseAlpha;
        }
    }
}
