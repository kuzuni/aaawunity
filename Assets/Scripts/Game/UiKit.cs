using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>uGUI 를 코드로 세우는 최소 도우미. 폰트는 Assets/Fonts/Jua-Regular.ttf (Resources 아님 — Bootstrap 이 주입).</summary>
    public static class UiKit
    {
        public static Font DefaultFont;

        public static Font FontOrBuiltin()
        {
            if (DefaultFont != null) return DefaultFont;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static Canvas CreateRootCanvas(string name)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
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

        public static Text Text(Transform parent, string s, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = FontOrBuiltin();
            t.text = s; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
