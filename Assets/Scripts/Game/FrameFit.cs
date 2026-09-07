using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T182 <b>3단계-3</b> — 프레임의 «비율» 을 화면에 맞춰 다시 잡는다(주인 «9:19 말고 9:16 이나 그런 거도 되게» · «갤럭시 탭까지»).
    /// <para>
    /// <see cref="AspectRatioFitter.aspectRatio"/> 는 <b>상수</b>라 한 번 넣으면 화면이 바뀌어도 그대로다.
    /// 이 한 겹이 매 프레임 부모(<c>SafeArea</c>) 사각형을 보고 그 값을 다시 넣는다(<see cref="FrameBackdrop"/>·<see cref="WorldCam"/> 과 같은 «바뀔 때만» 규약).
    /// </para>
    /// <b>규칙</b>: 프레임 세로비 = <c>clamp(화면 세로비, 기준 2.1639, 상한 2.3333)</c>
    /// <list type="bullet">
    /// <item><b>기준보다 납작한 화면</b>(9:16 · 3:4 태블릿) → 기준 그대로. 폭이 남는 것은 2단계(<see cref="FrameBackdrop"/>)가 좌우 띠로 채운다 — <b>지금과 한 치도 다르지 않다</b>.</item>
    /// <item><b>기준~상한</b>(9:19.5 ~ 9:21) → 화면을 <b>꽉 채운다</b>. 늘어난 높이는 <see cref="Stretch"/> 규칙대로 «가운데» 가 먹는다(위·아래 띠는 픽셀 유지).</item>
    /// <item><b>상한보다 길쭉한 화면</b> → 상한에서 멈춘다. 남는 위·아래는 2단계 띠가 채운다(무한정 늘이면 가운데만 기형으로 길어진다).</item>
    /// </list>
    /// ⚠ <b>가로는 이 규칙이 안 건드린다</b> — 폭 상한·가운데 정렬·좌우 배경은 2단계가 이미 해 놓았다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AspectRatioFitter))]
    public sealed class FrameFit : MonoBehaviour
    {
        AspectRatioFitter _arf;
        RectTransform _self;
        float _last = -1f;

        /// <summary>프레임에 이 한 겹을 붙인다(<see cref="UiKit.CreateFrame"/> 이 부른다).</summary>
        public static FrameFit Attach(RectTransform frame)
        {
            var f = UiKit.Ensure<FrameFit>(frame.gameObject);
            f._self = frame; f._arf = frame.GetComponent<AspectRatioFitter>();
            f.Apply();
            return f;
        }

        void LateUpdate() { Apply(); }

        /// <summary>부모(안전 영역)의 세로비를 규칙에 넣어 프레임 비율을 다시 잡는다 — 바뀐 만큼만.</summary>
        public void Apply()
        {
            if (_arf == null || _self == null) return;
            var parent = _self.parent as RectTransform;
            if (parent == null) return;
            var r = parent.rect;
            if (r.width <= 1f || r.height <= 1f) return;
            float want = Ratio(r.width, r.height);
            if (Mathf.Abs(want - _last) < 0.0005f) return;
            _last = want;
            _arf.aspectRatio = 1f / want;   // AspectRatioFitter 는 «가로/세로» 를 받는다
        }

        /// <summary>화면 세로비 → 프레임 세로비(순수 계산 · 자가 직접 잰다). 기준 아래는 기준으로 · 상한 위는 상한으로 자른다.</summary>
        public static float Ratio(float parentW, float parentH)
        {
            if (parentW <= 0f || parentH <= 0f) return Stretch.RefAspect;
            float a = parentH / parentW;
            if (a < Stretch.RefAspect) return Stretch.RefAspect;
            return a > Stretch.MaxAspect ? Stretch.MaxAspect : a;
        }
    }
}
