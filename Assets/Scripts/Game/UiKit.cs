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

        /// <summary>9:19.5 프레임 — index.html #frame. 화면 가운데 · 최대 크기로 letterbox.</summary>
        public static RectTransform CreateFrame(Transform canvas)
        {
            var rt = Rect(canvas, "Frame");
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(FrameW, FrameH);
            var arf = rt.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent; arf.aspectRatio = FrameW / FrameH;
            var img = rt.gameObject.AddComponent<Image>(); img.color = Palette.Bg; img.raycastTarget = true;
            return rt;
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        /// <summary>부모 % 배치 — x,y = 왼쪽·위 모서리(%), w,h = 폭·높이(%). ref-layout.md 표를 그대로 넣는다.</summary>
        public static void Pct(RectTransform rt, float x, float y, float w, float h)
        {
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
        public static Text Text(Transform parent, string s, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool bestFit = false, bool outline = true, TextKind kind = TextKind.Body)
        {
            size = TextSize.Floor(size, kind);
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = FontOrBuiltin(); t.text = s; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; t.supportRichText = true;
            if (bestFit) { t.resizeTextForBestFit = true; t.resizeTextMinSize = TextSize.BestFitFloor(12, kind); t.resizeTextMaxSize = size; t.verticalOverflow = VerticalWrapMode.Truncate; }
            if (outline) AddOutline(t, size);
            TextAudit.Mark(t, kind);
            return t;
        }
        static void AddOutline(Text t, float size)
        {
            var ol = t.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.1f, 0.06f, 0.05f, 0.85f);
            float d = Mathf.Clamp(size * 0.05f, 1.5f, 4f); ol.effectDistance = new Vector2(d, -d); ol.useGraphicAlpha = true;
        }
        public static Text Label(Transform parent, float x, float y, float w, float h, string s, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool bestFit = true, bool outline = true, TextKind kind = TextKind.Body)
        {
            var t = Text(parent, s, size, color, anchor, bestFit, outline, kind);
            Pct(t.rectTransform, x, y, w, h);
            return t;
        }

        /// <summary>가로 게이지 — GUI Pro Slider_02 프리팹(카탈로그 키 ui.slider*) 을 쓰고 값은 Slider 컴포넌트로 넣는다.</summary>
        public sealed class Bar
        {
            public RectTransform Root; public Slider Slider; public Text Txt; public Image Cap;
            public void Set(double frac, string txt) { if (Slider != null) Slider.value = Mathf.Clamp01((float)frac); if (Txt != null) Txt.text = txt; }
        }
        public static Bar MakeBar(Transform parent, string sliderKey, string capIconKey = null)
        {
            var go = Spawn(sliderKey, parent);
            var bar = new Bar { Root = (RectTransform)go.transform, Slider = go.GetComponentInChildren<Slider>(true) };
            if (bar.Slider != null) { bar.Slider.interactable = false; bar.Slider.transition = Selectable.Transition.None; bar.Slider.minValue = 0; bar.Slider.maxValue = 1; foreach (var g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false; }
            bar.Txt = go.GetComponentInChildren<Text>(true);
            if (bar.Txt != null) { bar.Txt.resizeTextForBestFit = true; bar.Txt.resizeTextMinSize = TextSize.BestFitMin; bar.Txt.resizeTextMaxSize = TextSize.Body; bar.Txt.horizontalOverflow = HorizontalWrapMode.Overflow; }
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

        // 월드(SpriteRenderer) 바 — 발밑 2단 바(BattleWorld.MakeBar · T69 8항). 같은 조각을 월드용 Sprite 로 다시 감싼다(pixelsPerUnit 을 «선 = 프레임 8px 에 해당하는 월드 길이» 로 · 텍스처는 주인 것 그대로).
        static readonly Dictionary<string, Sprite> _worldBorders = new Dictionary<string, Sprite>();
        /// <summary>프레임 <see cref="BorderPx"/> 에 해당하는 월드 길이(u) — 프레임 px → 레이아웃 px(× LayoutW/FrameW) → 월드(÷ PPU).</summary>
        public static float WorldBorderLine => BorderPx * (WorldCam.LayoutW / FrameW) / WorldCam.PPU;
        /// <summary>월드용 테두리 스프라이트(키마다 한 번 만들어 재사용 · 9-slice border 그대로 · FullRect).</summary>
        public static Sprite WorldBorderSprite(string key = BorderKey)
        {
            if (_worldBorders.TryGetValue(key, out var s) && s != null) return s;
            var src = Cat != null ? Cat.Sprite(key) : null; if (src == null) return null;
            float ppu = BorderNativePx(key) / WorldBorderLine;
            s = Sprite.Create(src.texture, src.rect, new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, src.border);
            s.name = src.name + " (world)";
            _worldBorders[key] = s;
            return s;
        }
        /// <summary>월드 바(SpriteRenderer) 위에 테두리 한 장 — <paramref name="bar"/> 의 자식 «Border»(Sliced · <paramref name="size"/> = 바 크기 · sortingOrder = <paramref name="order"/> · Ink). 조각이 없으면 null(경고는 카탈로그가).</summary>
        public static SpriteRenderer WorldBorder(Transform bar, Vector2 size, int order, string key = BorderKey, Color? tint = null)
        {
            var sp = WorldBorderSprite(key); if (sp == null || bar == null) return null;
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

        // ───────────────────────── 공통 팝업 문법 (docs/ref/README.md «공통 문법» · T36 — T38·T41·T42·T44 가 같이 쓴다) ─────────────────────────
        /// <summary><see cref="Popup"/> 이 만든 조각들 — 안의 내용은 <see cref="Box"/> 에 <see cref="Pct"/> 로 배치한다.</summary>
        public sealed class PopupParts { public RectTransform Dim, Box, Ribbon; public Text Title, TapClose; }
        /// <summary>
        /// 레퍼런스 공통 팝업: <b>어두운 반투명 배경</b> 위 <b>둥근 패널</b>(Popup_Box 변형 · <paramref name="popupKey"/>) · 제목은 패널 윗변에 걸친 <b>리본/명판</b>(<paramref name="titleKey"/> · 가운데) ·
        /// 프레임 밖 아래 가운데 <b>«탭하여 닫기»</b> 흰 글자(<see cref="Layout.BookClose"/> 줄 · 닫기 X 버튼 없음 · <b>배경 탭으로 닫힘</b> = <paramref name="onTapClose"/>). onTapClose 가 null 이면 닫기 글자·배경 탭 없음(선택을 강제하는 이벤트 팝업).
        /// 조각은 전부 GUI Pro 프리팹 · 코드 도형 0. 상자 안 배치는 돌려준 <see cref="PopupParts.Box"/> 에 Pct 로.
        /// </summary>
        public static PopupParts Popup(Transform layer, string title, Layout.R rect, Action onTapClose, string popupKey = "ui.popup", string titleKey = "ui.title.tangerine", bool dim = true)
        {
            var parts = new PopupParts();
            if (dim)
            {
                var d = Rect(layer, "Dimmed"); Stretch(d);
                var di = d.gameObject.AddComponent<Image>(); di.color = Palette.A(Palette.Dim, 0.85f); di.raycastTarget = true;
                FadeIn(di, 0.85f);
                parts.Dim = d;
            }
            var box = SpawnRt(popupKey, layer, rect);
            foreach (var g in box.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = true;   // 상자 뒤로 클릭이 새지 않게
            var ribbon = Spawn(titleKey, box); var rr = (RectTransform)ribbon.transform;
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 1f); rr.pivot = new Vector2(0.5f, 0.5f); rr.sizeDelta = new Vector2(656, 115); rr.anchoredPosition = new Vector2(0, 8);
            var tt = SetText(rr, "Text (TMP)", title, null, TextSize.Title, TextKind.Title); if (tt != null) { tt.resizeTextForBestFit = true; tt.resizeTextMinSize = TextSize.BestFitMin; tt.resizeTextMaxSize = TextSize.Title; }
            parts.Box = box; parts.Ribbon = rr; parts.Title = tt;
            if (onTapClose != null)
            {
                var tc = Text(layer, "탭하여 닫기", TextSize.Body, Palette.White, TextAnchor.MiddleCenter, false, true); tc.name = "TapToClose"; tc.fontStyle = FontStyle.Bold;
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
        public static GameObject Spawn(string prefabKey, Transform parent, bool adopt = true)
        {
            var prefab = Cat != null ? Cat.Prefab(prefabKey) : null;
            GameObject go;
            if (prefab == null) { go = new GameObject(prefabKey, typeof(RectTransform)); go.transform.SetParent(parent, false); return go; }
            go = UnityEngine.Object.Instantiate(prefab, Staging(), false);
            go.name = prefabKey;
            StripDemoScripts(go);
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
        public static RectTransform SpawnRt(string prefabKey, Transform parent, Layout.R r)
        {
            var go = Spawn(prefabKey, parent); var rt = (RectTransform)go.transform; Pct(rt, r); return rt;
        }

        /// <summary>인스턴스를 이 프로젝트 규칙에 맞춘다 — TMP → Text(Jua) · LayerLab 데모 스크립트 제거 · 이미지 raycast 끔.</summary>
        public static void Adopt(GameObject root)
        {
            StripDemoScripts(root);
            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true)) ConvertTmp(tmp);
            foreach (var g in root.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            var rt = root.transform as RectTransform; if (rt != null) rt.localScale = Vector3.one;
        }

        static Text ConvertTmp(TMP_Text tmp)
        {
            var go = tmp.gameObject;
            string s = tmp.text; float fs = tmp.fontSize; Color c = tmp.color; var al = tmp.alignment;
            bool auto = tmp.enableAutoSizing; float mn = tmp.fontSizeMin, mx = tmp.fontSizeMax;
            bool outline = tmp.fontSharedMaterial != null && tmp.fontSharedMaterial.name.IndexOf("Outline", StringComparison.OrdinalIgnoreCase) >= 0;
            UnityEngine.Object.DestroyImmediate(tmp);
            var t = go.AddComponent<Text>();
            // 프리팹 글자도 하한(T63) — 데모 프리팹의 작은 크기(12~30)를 그대로 옮기면 폰에서 안 읽힌다 · 종류는 Body(버튼은 Button() 이 다시 올린다)
            int size = TextSize.Floor(Mathf.Max(12, Mathf.RoundToInt(fs)));
            t.font = FontOrBuiltin(); t.text = s; t.fontSize = size; t.color = c; t.alignment = MapAlign(al);
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.supportRichText = true; t.raycastTarget = false;
            if (auto) { t.resizeTextForBestFit = true; t.resizeTextMinSize = TextSize.BestFitFloor(Mathf.Max(10, (int)mn)); t.resizeTextMaxSize = TextSize.Floor(Mathf.Max(12, (int)mx)); }
            if (outline || c.r + c.g + c.b > 2.4f) AddOutline(t, size);
            return t;
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
        public static Text SetText(Transform root, string path, string s, Color? color = null, int? size = null, TextKind kind = TextKind.Body)
        {
            var t = Find(root, path); Text txt = null; if (t != null) { txt = t.GetComponent<Text>(); if (txt == null) txt = t.GetComponentInChildren<Text>(true); }
            if (txt == null) { Debug.LogWarning($"[UiKit] 글자 없음: {root.name}/{path}"); return null; }
            txt.text = s; if (color.HasValue) txt.color = color.Value;
            if (size.HasValue) { int sz = TextSize.Floor(size.Value, kind); txt.fontSize = sz; txt.resizeTextMaxSize = sz; }
            if (txt.resizeTextForBestFit) txt.resizeTextMinSize = TextSize.BestFitFloor(txt.resizeTextMinSize, kind);
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
                if (!ActiveUpTo(im.transform, root)) continue;
                return im;
            }
            return self;
        }
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

        /// <summary>프리팹 버튼 하나 세우기 — 스폰 · 글자 · 클릭.</summary>
        public static RectTransform Button(Transform parent, string prefabKey, string label, Action onClick, Layout.R? rect = null)
        {
            var go = Spawn(prefabKey, parent); var rt = (RectTransform)go.transform;
            if (rect.HasValue) Pct(rt, rect.Value);
            var txt = go.GetComponentInChildren<Text>(true);
            // 버튼 글자 하한 = TextSize.Button(44 · T63) · bestFit 최소 32
            if (txt != null) { txt.text = label; txt.fontSize = TextSize.Floor(txt.fontSize, TextKind.Button); txt.resizeTextForBestFit = true; txt.resizeTextMinSize = TextSize.BestFitMin; txt.resizeTextMaxSize = Mathf.Max(txt.fontSize, TextSize.Button); txt.horizontalOverflow = HorizontalWrapMode.Wrap; TextAudit.Mark(txt, TextKind.Button); }
            Clickable(rt, onClick);
            return rt;
        }
        public static Text ButtonText(Component b) => b.GetComponentInChildren<Text>(true);

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
            foreach (var img in frameRoot.GetComponentsInChildren<Image>(true)) img.material = inst;
            var mo = Ensure<MaterialOwner>(owner.gameObject); mo.Mat = inst;
            return inst;
        }
        /// <summary><paramref name="inst"/> 의 _ShineLocation 을 <paramref name="at"/> 초부터 <see cref="ShineDur"/> 동안 <see cref="ShineFrom"/>→<see cref="ShineTo"/> 로 — 마스터 시퀀스에 Insert(스킵·CompleteAll 이면 끝 값 = 화면 밖). 돌려주는 값 = 끝나는 시각.</summary>
        public static float Shine(Sequence master, Material inst, Transform link, float at)
        {
            if (inst == null || master == null) return at;
            float v = ShineFrom; inst.SetFloat(ShineLocationId, v);
            var tw = DOTween.To(() => v, x => { v = x; if (inst != null) inst.SetFloat(ShineLocationId, x); }, ShineTo, ShineDur).SetEase(Ease.InOutSine).SetUpdate(true).SetTarget(inst);
            if (link != null) tw.SetLink(link.gameObject);   // SetLink(T56) — 마스터에 Insert 되면 마스터의 링크·Kill 이 대신 지킨다
            master.Insert(at, tw);
            return at + ShineDur;
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
