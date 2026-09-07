using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 프레임(9:19.5) «밖» 레터박스 띠를 게임 배경으로 채운다 — T182 2단계(주인 2026-09-07 11:5X «9:16 이나 그런 거도 되게» · «갤럭시 탭까지도 해상도 받게끔»).
    /// <para>
    /// 1단계 실측(<c>AspectRatioGateTests</c>)이 남긴 «남는 띠»: 9:16 <b>17.8%</b> · 9:18 7.6% · 9:19.5 0.1% · 9:21 7.3% · <b>3:4(태블릿) 38.4%</b>.
    /// 그 자리가 지금은 캔버스 바닥(검정)이라 태블릿에서 화면의 3분의 1 이 검은 띠였다. 여기서 <b>배경색 + 흐르는 무늬</b>(T72 ①)로 덮어 «그림이 이어지게» 한다.
    /// </para>
    /// <b>프레임 «안» 은 한 픽셀도 덮지 않는다</b> — 네 띠(좌·우·위·아래)가 프레임 사각형을 비켜 간다.
    /// 전투 월드는 <see cref="WorldCam"/> 이 카메라 viewport 를 프레임에 맞춰 «캔버스 뒤» 에 그리므로, 프레임을 덮으면 마당이 통째로 사라진다(그래서 통짜 배경 한 장이 아니라 띠 넷이다).
    /// 배치 표·§5 채점은 한 줄도 안 바뀐다 — 요소가 아니라 프레임 밖 바탕이고, 프레임 안 좌표계(<see cref="Layout"/>)를 건드리지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FrameBackdrop : MonoBehaviour
    {
        /// <summary>띠 넷의 이름(테스트·감사용) — 왼쪽·오른쪽·위·아래.</summary>
        public const string LeftName = "Band:left", RightName = "Band:right", TopName = "Band:top", BottomName = "Band:bottom";

        public RectTransform Frame;
        RectTransform _self;
        readonly RectTransform[] _bands = new RectTransform[4];
        Vector2 _lastSelf = new Vector2(-1, -1), _lastFrame = new Vector2(-1, -1);

        /// <summary>바탕 한 겹을 세운다 — <paramref name="parent"/>(보통 캔버스) 를 꽉 채우고 프레임보다 «뒤»(형제 0) 에 선다.</summary>
        public static FrameBackdrop Create(Transform parent, RectTransform frame)
        {
            var rt = UiKit.Rect(parent, "Backdrop");
            UiKit.Stretch(rt);
            rt.SetSiblingIndex(0);
            var fb = rt.gameObject.AddComponent<FrameBackdrop>();
            fb.Frame = frame; fb._self = rt;
            for (int i = 0; i < 4; i++)
            {
                var band = UiKit.Rect(rt, i == 0 ? LeftName : i == 1 ? RightName : i == 2 ? TopName : BottomName);
                var img = band.gameObject.AddComponent<Image>();
                img.color = Palette.Bg; img.raycastTarget = false;   // 클릭은 프레임 안 UI 가 받는다(띠는 그림만)
                UiKit.PatternBg(band, UiKit.PatternTintDark);         // T72 ① — 프레임 안 배경과 같은 결(카탈로그가 아직 없으면 색만 남는다)
                fb._bands[i] = band;
            }
            fb.Apply();
            return fb;
        }

        void LateUpdate() { Apply(); }

        /// <summary>프레임 사각형이 바뀐 만큼만 띠 넷을 다시 잡는다(<see cref="WorldCam.Apply"/> 와 같은 «바뀔 때만» 규약).</summary>
        void Apply()
        {
            if (Frame == null || _self == null) return;
            var self = _self.rect.size; var frame = Frame.rect.size * Frame.lossyScale.x / Mathf.Max(1e-4f, _self.lossyScale.x);
            if (self.x <= 1f || self.y <= 1f) return;
            if (self == _lastSelf && frame == _lastFrame) return;
            _lastSelf = self; _lastFrame = frame;

            // 프레임은 부모 가운데에 선다(CreateFrame 의 AspectRatioFitter) — 남는 띠를 «절반 비율» 로 잡는다
            float hw = Mathf.Clamp01(frame.x / self.x) * 0.5f, hh = Mathf.Clamp01(frame.y / self.y) * 0.5f;
            Set(_bands[0], new Vector2(0f, 0f), new Vector2(0.5f - hw, 1f));                     // 왼쪽 — 화면 높이 전부
            Set(_bands[1], new Vector2(0.5f + hw, 0f), new Vector2(1f, 1f));                     // 오른쪽
            Set(_bands[2], new Vector2(0.5f - hw, 0.5f + hh), new Vector2(0.5f + hw, 1f));       // 위 — 프레임 폭 안쪽만
            Set(_bands[3], new Vector2(0.5f - hw, 0f), new Vector2(0.5f + hw, 0.5f - hh));       // 아래
        }

        static void Set(RectTransform rt, Vector2 min, Vector2 max)
        {
            if (rt == null) return;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
