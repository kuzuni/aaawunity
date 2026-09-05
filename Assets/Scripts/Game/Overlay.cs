using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>팝업 층(index.html #overlay). 3단계에서 채운다 — 지금은 뼈대.</summary>
    public sealed class Overlay
    {
        readonly App _app;
        public RectTransform Root { get; }
        public bool IsOpen => Root.gameObject.activeSelf;
        public Overlay(App app)
        {
            _app = app;
            Root = UiKit.Rect(app.Frame, "Overlay"); UiKit.Stretch(Root);
            Root.gameObject.SetActive(false);
        }
        public void Close() { UiKit.Clear(Root); Root.gameObject.SetActive(false); }
        public void Tick(float dt) { }
    }
}
