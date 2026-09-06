using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T106 — 모바일 노치·펀치홀·둥근 모서리를 피해 UI 를 «안전 영역» 안에 둔다
    /// (주인 2026-09-07: «이 게임 모바일로 낼 거니까 SafeArea 만들어서 그 안에서 UI 만들도록 · 카메라 때매 UI 안 보이는 일 없게»).
    /// 루트 캔버스 바로 아래 «SafeArea» 사각형이 <see cref="Screen.safeArea"/> 를 <b>앵커(0~1)</b> 로 옮겨 담고, 화면 UI(<see cref="App.Frame"/>)는 전부 그 자식이다 —
    /// 픽셀을 박지 않으므로 해상도·회전이 바뀌어도 같은 식이 그대로 성립한다.
    /// 데스크톱·WebGL 처럼 safeArea 가 화면 전체인 곳에서는 앵커가 0~1 이라 <b>배치가 한 픽셀도 안 바뀐다</b>(회귀 0 · 배치 표 <c>ref-layout.md</c> 도 그대로).
    /// 상단 프레임 띠(<see cref="TopBar.FrameName"/>)와 하단 탭 바 띠는 이 영역을 <b>넘어</b> 화면 끝까지 뻗는다(주인 «SafeArea 넘어서까지 그 프레임이 위를 다 감싼다»).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SafeAreaRoot : MonoBehaviour
    {
        /// <summary>테스트 주입용 — 값이 있으면 <see cref="Screen.safeArea"/> 대신 이 값을 쓴다(PlayMode 에서 노치를 흉내 낸다). null 이면 실제 값.</summary>
        public static Rect? Override;

        /// <summary>지금 적용할 안전 영역(픽셀 · 화면 좌표계 · 왼쪽 아래가 원점).</summary>
        public static Rect Current => Override ?? Screen.safeArea;

        RectTransform _rt; Rect _last; int _w, _h; bool _has;

        void Awake() { _rt = (RectTransform)transform; Apply(true); }
        void OnEnable() { Apply(true); }
        void Update() { Apply(false); }

        /// <summary>지금 값으로 앵커를 맞춘다. <paramref name="force"/> 가 아니면 화면 크기·safeArea 가 그대로일 때 아무 일도 안 한다(매 프레임 비용 0).</summary>
        public void Apply(bool force)
        {
            if (_rt == null) _rt = (RectTransform)transform;
            var area = Current;
            int w = Screen.width, h = Screen.height;
            if (!force && _has && area == _last && w == _w && h == _h) return;
            if (w <= 0 || h <= 0) return;
            _last = area; _w = w; _h = h; _has = true;
            var min = new Vector2(area.xMin / w, area.yMin / h);
            var max = new Vector2(area.xMax / w, area.yMax / h);
            // 값이 이상하면(플랫폼이 0 을 주거나 화면 밖) 화면 전체로 — 노치 대응이 UI 를 없애는 일은 없어야 한다
            if (min.x < 0f || min.y < 0f || max.x > 1f || max.y > 1f || max.x - min.x < 0.2f || max.y - min.y < 0.2f) { min = Vector2.zero; max = Vector2.one; }
            _rt.anchorMin = min; _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero; _rt.offsetMax = Vector2.zero;
        }
    }
}
