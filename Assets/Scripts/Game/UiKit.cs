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
        static void ResetStatics() { _round = _round8 = _circle = _white = null; _staging = null; DefaultFont = null; CharacterRig.TimeScale = 1f; }

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

        public static Text Text(Transform parent, string s, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool bestFit = false, bool outline = true)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = FontOrBuiltin(); t.text = s; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; t.supportRichText = true;
            if (bestFit) { t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = size; t.verticalOverflow = VerticalWrapMode.Truncate; }
            if (outline) AddOutline(t, size);
            return t;
        }
        static void AddOutline(Text t, float size)
        {
            var ol = t.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.1f, 0.06f, 0.05f, 0.85f);
            float d = Mathf.Clamp(size * 0.05f, 1.5f, 4f); ol.effectDistance = new Vector2(d, -d); ol.useGraphicAlpha = true;
        }
        public static Text Label(Transform parent, float x, float y, float w, float h, string s, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool bestFit = true, bool outline = true)
        {
            var t = Text(parent, s, size, color, anchor, bestFit, outline);
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
            if (bar.Txt != null) { bar.Txt.resizeTextForBestFit = true; bar.Txt.resizeTextMinSize = 12; bar.Txt.resizeTextMaxSize = 40; bar.Txt.horizontalOverflow = HorizontalWrapMode.Overflow; }
            if (!string.IsNullOrEmpty(capIconKey))
            {
                bar.Cap = Icon(go.transform, "Cap", capIconKey);
                var rt = bar.Cap.rectTransform; rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(58, 58); rt.anchoredPosition = new Vector2(2, 2);
            }
            return bar;
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
            t.font = FontOrBuiltin(); t.text = s; t.fontSize = Mathf.Max(12, Mathf.RoundToInt(fs)); t.color = c; t.alignment = MapAlign(al);
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.supportRichText = true; t.raycastTarget = false;
            if (auto) { t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Max(10, (int)mn); t.resizeTextMaxSize = Mathf.Max(12, (int)mx); }
            if (outline || c.r + c.g + c.b > 2.4f) AddOutline(t, fs);
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
        public static Text SetText(Transform root, string path, string s, Color? color = null, int? size = null)
        {
            var t = Find(root, path); Text txt = null; if (t != null) { txt = t.GetComponent<Text>(); if (txt == null) txt = t.GetComponentInChildren<Text>(true); }
            if (txt == null) { Debug.LogWarning($"[UiKit] 글자 없음: {root.name}/{path}"); return null; }
            txt.text = s; if (color.HasValue) txt.color = color.Value; if (size.HasValue) { txt.fontSize = size.Value; txt.resizeTextMaxSize = size.Value; }
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

        /// <summary>GUI Pro 버튼 프리팹은 Button 컴포넌트가 없다 — 여기서 붙인다(첫 Image 가 targetGraphic · DOTween 눌림 연출).</summary>
        public static Button Clickable(Transform t, Action onClick, bool punch = true)
        {
            var go = t.gameObject;
            var img = go.GetComponent<Image>();
            if (img == null) { img = go.AddComponent<Image>(); img.color = new Color(1, 1, 1, 0); }   // 투명 히트 영역
            img.raycastTarget = true;
            var b = Ensure<Button>(go);
            b.targetGraphic = img; b.transition = Selectable.Transition.None;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                if (punch) { t.DOKill(true); t.localScale = Vector3.one; t.DOPunchScale(new Vector3(-0.08f, -0.08f, 0), 0.18f, 1, 0.5f).SetUpdate(true); }
                onClick?.Invoke();
            });
            return b;
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
            if (txt != null) { txt.text = label; txt.resizeTextForBestFit = true; txt.resizeTextMinSize = 14; txt.resizeTextMaxSize = Mathf.Max(txt.fontSize, 20); txt.horizontalOverflow = HorizontalWrapMode.Wrap; }
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
        public static T Ensure<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }
        public static T Ensure<T>(Component on) where T : Component => Ensure<T>(on.gameObject);

        public static void Destroy(Transform t) { if (t != null) UnityEngine.Object.Destroy(t.gameObject); }
        public static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject); }

        // ───────────────────────── 연출 ─────────────────────────
        public static void PopIn(RectTransform rt, float from = 0.82f, float dur = 0.28f)
        {
            rt.DOKill(); rt.localScale = Vector3.one * from; rt.DOScale(1f, dur).SetEase(Ease.OutBack).SetUpdate(true);
        }
        public static void FadeIn(Graphic g, float to, float dur = 0.25f)
        {
            var c = g.color; g.color = new Color(c.r, c.g, c.b, 0); g.DOFade(to, dur).SetUpdate(true);
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
}
