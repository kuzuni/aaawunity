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
    /// <para>
    /// ⚠ 띠 자리는 <b>프레임의 월드 모서리</b>로 잡는다(결정 458 갈래) — 프레임의 부모는 <see cref="UiKit.CreateSafeArea"/> 이고 바탕의 부모는 캔버스라
    /// «부모 rect 비율» 산술은 노치 여백만큼 어긋난다. 그리고 <b>화면 사각형이 바뀌면 곧바로 다시 잡아야 한다</b>(<see cref="Refresh"/>).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FrameBackdrop : MonoBehaviour
    {
        /// <summary>띠 넷의 이름(테스트·감사용) — 왼쪽·오른쪽·위·아래.</summary>
        public const string LeftName = "Band:left", RightName = "Band:right", TopName = "Band:top", BottomName = "Band:bottom";

        public RectTransform Frame;
        RectTransform _self;
        readonly RectTransform[] _bands = new RectTransform[4];
        readonly Vector3[] _corners = new Vector3[4];
        static readonly Vector4 NoBox = new Vector4(-1f, -1f, -1f, -1f);
        Vector4 _box = NoBox;

        /// <summary>띠 하나(0=왼 1=오른 2=위 3=아래) — 촬영 하니스·게이트가 «띠가 프레임을 덮었나» 를 재는 자리.</summary>
        public RectTransform BandAt(int i) => i >= 0 && i < 4 ? _bands[i] : null;

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

        /// <summary>
        /// 띠를 <b>지금 당장</b> 다시 잡는다 — 캔버스 사각형이 «한 프레임 안에서» 바뀌는 자리에서 부른다.
        /// 촬영 하니스(<c>PlayShot.Save</c>)가 그런 자리다: 캔버스를 잠시 <c>ScreenSpaceCamera</c> 로 돌려 9:19.5 RenderTexture 에 그리는데
        /// 그 사이 <see cref="LateUpdate"/> 가 한 번도 안 돌아, 배치 모드 «가로 화면» 몫으로 잡힌 띠(폭 ≈ 35%)가 그대로 찍혔다(CI #373 `02_battle` 실측: 좌우 176px).
        /// </summary>
        public void Refresh() { _box = NoBox; Apply(); }

        /// <summary>프레임 사각형이 바뀐 만큼만 띠 넷을 다시 잡는다(<see cref="WorldCam.Apply"/> 와 같은 «바뀔 때만» 규약).</summary>
        void Apply()
        {
            if (Frame == null || _self == null) return;
            var self = _self.rect;
            if (self.width <= 1f || self.height <= 1f) return;
            // 프레임의 «월드» 모서리를 바탕의 제 좌표로 옮긴다 — 부모가 달라서(SafeArea ↔ 캔버스) rect 비율 산술은 못 쓴다
            Frame.GetWorldCorners(_corners);
            Vector3 a = _self.InverseTransformPoint(_corners[0]), b = _self.InverseTransformPoint(_corners[2]);
            var inSelf = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            var box = FrameBox(self, inSelf);
            if (box == _box) return;
            _box = box;
            Set(_bands[0], new Vector2(0f, 0f), new Vector2(box.x, 1f));                 // 왼쪽 — 화면 높이 전부
            Set(_bands[1], new Vector2(box.z, 0f), new Vector2(1f, 1f));                 // 오른쪽
            Set(_bands[2], new Vector2(box.x, box.w), new Vector2(box.z, 1f));           // 위 — 프레임 폭 안쪽만
            Set(_bands[3], new Vector2(box.x, 0f), new Vector2(box.z, box.y));           // 아래
        }

        /// <summary>
        /// 바탕 사각형(<paramref name="self"/>) 안에서 프레임(<paramref name="frame"/> · 같은 좌표계)이 차지한 <b>정규 사각형</b>(x0,y0,x1,y1 · 0~1).
        /// 순수 계산이라 자가 이 함수만 따로 잴 수 있다. <b>못 재면 (0,0,1,1)</b> = 띠 넷이 폭 0 — 틀리더라도 «마당을 가리는 쪽» 으로는 절대 안 틀린다.
        /// </summary>
        public static Vector4 FrameBox(Rect self, Rect frame)
        {
            if (self.width <= 1f || self.height <= 1f) return new Vector4(0f, 0f, 1f, 1f);
            float x0 = Mathf.Clamp01((frame.xMin - self.xMin) / self.width);
            float x1 = Mathf.Clamp01((frame.xMax - self.xMin) / self.width);
            float y0 = Mathf.Clamp01((frame.yMin - self.yMin) / self.height);
            float y1 = Mathf.Clamp01((frame.yMax - self.yMin) / self.height);
            if (x1 - x0 < 0.01f || y1 - y0 < 0.01f) return new Vector4(0f, 0f, 1f, 1f);
            return new Vector4(x0, y0, x1, y1);
        }

        static void Set(RectTransform rt, Vector2 min, Vector2 max)
        {
            if (rt == null) return;
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
