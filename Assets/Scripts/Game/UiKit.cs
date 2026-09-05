using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// uGUI 를 코드로 세우는 도우미. 프리팹·씬을 손으로 쓰는 대신 런타임 생성이 이 프로젝트의 기본 전략이다.
    /// 배치는 전부 «프레임 % » (<see cref="Pct"/>) — aaaw docs/ui/ref-layout.md 의 표를 그대로 옮길 수 있게.
    /// </summary>
    public static class UiKit
    {
        public static Font DefaultFont;
        static Sprite _round, _round8, _circle, _white;

        public static Font FontOrBuiltin() => DefaultFont != null ? DefaultFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>둥근 사각형 9-slice 스프라이트 (모서리 반지름 r 픽셀).</summary>
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
                float a = Mathf.Clamp01(r - d + 0.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
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
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;   // 9:16 폰이든 9:19.5 폰이든 프레임(9:19)이 통째로 들어온다
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        /// <summary>9:19 프레임 — index.html #frame. 화면 가운데 · 최대 크기로 letterbox.</summary>
        public static RectTransform CreateFrame(Transform canvas)
        {
            var rt = Rect(canvas, "Frame");
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(390, 844);
            var arf = rt.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent; arf.aspectRatio = 390f / 844f;
            var img = rt.gameObject.AddComponent<Image>(); img.color = Palette.Panel; img.raycastTarget = true;
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

        /// <summary>프레임 % 배치 — x,y = 왼쪽·위 모서리(%), w,h = 폭·높이(%). ref-layout.md 표를 그대로 넣는다.</summary>
        public static void Pct(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(x / 100f, 1f - (y + h) / 100f);
            rt.anchorMax = new Vector2((x + w) / 100f, 1f - y / 100f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded) { img.sprite = Round(); img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; }
            img.raycastTarget = false;
            return img;
        }

        public static Text Text(Transform parent, string s, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool bestFit = false)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = FontOrBuiltin(); t.text = s; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; t.supportRichText = true;
            if (bestFit) { t.resizeTextForBestFit = true; t.resizeTextMinSize = 8; t.resizeTextMaxSize = size; t.verticalOverflow = VerticalWrapMode.Truncate; }
            var ol = rt.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0, 0, 0, 0.35f); ol.effectDistance = new Vector2(1, -1);
            return t;
        }
        public static Text Label(Transform parent, float x, float y, float w, float h, string s, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, bool bestFit = true)
        {
            var t = Text(parent, s, size, color, anchor, bestFit);
            Pct(t.rectTransform, x, y, w, h);
            return t;
        }

        public static Button Button(Transform parent, string name, string label, Color bg, Color fg, int fontSize, Action onClick, bool bestFit = true)
        {
            var img = Panel(parent, name, bg); img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors; cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1); cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f); b.colors = cb;
            var t = Text(img.transform, label, fontSize, fg, TextAnchor.MiddleCenter, bestFit);
            Stretch(t.rectTransform, 4, 2, 4, 2);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }
        public static Text ButtonText(Button b) => b.GetComponentInChildren<Text>();

        /// <summary>가로 게이지 — 배경 + fill(왼쪽 앵커) + 캡 글자 + 가운데 숫자. index.html .bar 구조.</summary>
        public sealed class Bar { public RectTransform Root; public Image Fill; public Text Txt, Cap; public void Set(double frac, string txt) { Fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)frac), 1); if (Txt != null) Txt.text = txt; } }
        public static Bar MakeBar(Transform parent, string name, Color fill, string cap, int fontSize = 11)
        {
            var bg = Panel(parent, name, new Color(0.08f, 0.08f, 0.1f, 0.85f)); bg.sprite = Round(6);
            var f = Panel(bg.transform, "Fill", fill); f.sprite = Round(6);
            f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = new Vector2(0, 1); f.rectTransform.offsetMin = new Vector2(2, 2); f.rectTransform.offsetMax = new Vector2(-2, -2);
            var bar = new Bar { Root = bg.rectTransform, Fill = f };
            if (!string.IsNullOrEmpty(cap)) { bar.Cap = Text(bg.transform, cap, fontSize, Palette.Ink, TextAnchor.MiddleLeft, true); Pct(bar.Cap.rectTransform, 2, 0, 24, 100); }
            bar.Txt = Text(bg.transform, "", fontSize, Palette.Ink, TextAnchor.MiddleCenter, true); Stretch(bar.Txt.rectTransform, 2, 0, 2, 0);
            return bar;
        }

        public static void SetInteractable(Button b, bool on) { b.interactable = on; var t = ButtonText(b); if (t != null) t.color = on ? Palette.A(t.color, 1f) : Palette.A(t.color, 0.5f); }
        public static void Destroy(Transform t) { if (t != null) UnityEngine.Object.Destroy(t.gameObject); }
        public static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject); }

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
