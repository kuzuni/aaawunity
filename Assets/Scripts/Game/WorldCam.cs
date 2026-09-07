using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 전투 월드 카메라 — UI 프레임(9:19)과 같은 화면 영역을 비춘다(viewport rect 를 프레임에 맞춘다).
    /// 좌표 규약: 레이아웃 단위(index.html LW=540 → 프레임 폭) 100 = 유니티 1 단위. 프레임 높이 = 540 × (2337/1080) = 1168.5 레이아웃 단위(프레임 실제 비율 · T182 3단계 · 결정 552).
    /// 월드 x(sim.js 좌표) → 화면: (worldX − cam) × zoom + PLAYER_SCREEN_X (ui.json camera.zoom·playerX).
    /// </summary>
    public sealed class WorldCam : MonoBehaviour
    {
        /// <summary>
        /// 프레임 안 레이아웃 눈금(index.html LW=540) — <b>세로는 프레임 실제 비율에서 뽑는다</b>(T182 3단계 · 지시서 5-58항 · 결정 552).
        /// <para>
        /// ⚠ 여기 오래 «<c>LayoutW × 19/9 = 1140</c>»(세로비 2.1111) 이 박혀 있었는데 프레임은 <c>1080×2337</c> = <b>2.1639</b> 다.
        /// 카메라 세로는 이 수가 잡고 가로는 viewport(프레임 실제)가 따라오므로, 그 차이만큼 <b>가로 한 단위가 100 이 아니라 102.5px</b> 이 되어
        /// <see cref="ToWorld"/> 로 놓은 것이 전부 바깥으로 <b>2.5%</b> 밀려 있었다 — §5 채점표의 «플레이어 중심 x 표 16.0 ↔ 실측 15.1» 이 그 지문이다.
        /// 프레임 비율에서 뽑으면 카메라가 보여 주는 가로가 정확히 <see cref="LayoutW"/> 가 되어 표와 화면이 같아진다.
        /// </para>
        /// </summary>
        public const float LayoutW = 540f, LayoutH = LayoutW * UiKit.FrameH / UiKit.FrameW, PPU = 100f;
        public RectTransform Frame;
        Camera _cam;
        Rect _last;

        public static WorldCam Attach(Camera cam, RectTransform frame)
        {
            var wc = cam.gameObject.GetComponent<WorldCam>(); if (wc == null) wc = cam.gameObject.AddComponent<WorldCam>();
            wc._cam = cam; wc.Frame = frame;
            cam.orthographic = true; cam.orthographicSize = OrthoFor(frame);
            cam.transform.position = new Vector3(0, 0, -10);
            wc.Apply();
            return wc;
        }

        void LateUpdate() { Apply(); }

        void Apply()
        {
            if (_cam == null || Frame == null) return;
            var corners = new Vector3[4]; Frame.GetWorldCorners(corners);   // Screen Space Overlay 캔버스 → 픽셀 좌표
            float x = corners[0].x / Screen.width, y = corners[0].y / Screen.height;
            float w = (corners[2].x - corners[0].x) / Screen.width, h = (corners[2].y - corners[0].y) / Screen.height;
            var r = new Rect(Mathf.Clamp01(x), Mathf.Clamp01(y), Mathf.Clamp01(w), Mathf.Clamp01(h));
            if (r != _last) { _cam.rect = r; _last = r; }
            float ortho = OrthoFor(Frame);
            if (!Mathf.Approximately(_cam.orthographicSize, ortho)) _cam.orthographicSize = ortho;
        }

        /// <summary>
        /// T182 3단계-3 — 프레임이 길어지면 마당을 <b>«더 보여 준다»</b>(확대하지 않는다).
        /// <para>
        /// 카메라의 <c>orthographicSize</c> 는 «세로 절반» 이고 가로는 viewport 비율이 따라오므로,
        /// 기준 값(<see cref="LayoutH"/>/2)을 그대로 두면 프레임이 길어질 때 <b>가로가 좁아져 그림이 7.8% 확대된다</b>.
        /// 그래서 세로비가 커진 만큼 <c>orthographicSize</c> 도 같이 키운다 — 그러면 <b>가로 배율이 상수</b>가 되고
        /// 늘어난 높이는 «마당이 더 보이는» 몫이 된다(세로 신축 규칙 «가운데가 남는 높이를 먹는다» 와 같은 뜻).
        /// </para>
        /// 기준 비율에서는 배수가 <b>정확히 1</b> 이라 지금과 한 치도 다르지 않다.
        /// ✅ 옛 <b>2.5% 어긋남</b>(<see cref="LayoutH"/> 가 19/9 였다 · 결정 552)은 3단계-4 에서 고쳤다 — 이제 카메라가 보여 주는 가로가 정확히 <see cref="LayoutW"/> 다.
        /// </summary>
        public static float OrthoFor(RectTransform frame)
        {
            float baseOrtho = LayoutH / 2f / PPU;
            if (frame == null) return baseOrtho;
            var r = frame.rect;
            if (r.width <= 1f || r.height <= 1f) return baseOrtho;
            float grow = (r.height / r.width) / Core.Stretch.RefAspect;
            if (grow < 1f) grow = 1f;
            return baseOrtho * grow;
        }

        /// <summary>프레임 안 레이아웃 좌표(x: 0~540 왼→오, yFrac: 0~1 위→아래) → 유니티 월드.</summary>
        public static Vector3 ToWorld(float layoutX, float yFrac, float z = 0)
            => new Vector3((layoutX - LayoutW / 2f) / PPU, (0.5f - yFrac) * LayoutH / PPU, z);
        /// <summary>
        /// 유니티 월드 좌표 → 프레임 px(<see cref="UiKit.FrameW"/>×<see cref="UiKit.FrameH"/> · 왼쪽 아래가 0,0).
        /// 데미지 팝·발밑 숫자·보상 구슬(T85)이 월드 위치를 UI 층에 옮길 때 쓰는 하나뿐인 변환.
        /// </summary>
        public static Vector2 ToFrame(Vector3 worldPos)
        {
            float lx = worldPos.x * PPU + LayoutW / 2f;
            float yFrac = 0.5f - worldPos.y * PPU / LayoutH;
            return new Vector2(lx * (UiKit.FrameW / LayoutW), (1f - yFrac) * UiKit.FrameH);
        }
        /// <summary>
        /// <see cref="ToFrame"/> 의 <b>역변환</b> — 프레임 px(왼쪽 아래 0,0) → 유니티 월드.
        /// UI 층에서 움직이는 것(보상 구슬 · T85)을 월드 렌더러(<c>TrailRenderer</c> · T144)로 따라가게 할 때 쓴다.
        /// </summary>
        public static Vector3 FromFrame(Vector2 framePos, float z = 0)
        {
            float lx = framePos.x * LayoutW / Mathf.Max(1f, UiKit.FrameW);
            float yFrac = 1f - framePos.y / Mathf.Max(1f, UiKit.FrameH);
            return ToWorld(lx, yFrac, z);
        }
        /// <summary>프레임 % 높이 → 유니티 단위.</summary>
        public static float PctH(float pct) => pct / 100f * LayoutH / PPU;
        public static float PctW(float pct) => pct / 100f * LayoutW / PPU;
    }
}
