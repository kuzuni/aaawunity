using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 전투 월드 카메라 — UI 프레임(9:19)과 같은 화면 영역을 비춘다(viewport rect 를 프레임에 맞춘다).
    /// 좌표 규약: 레이아웃 단위(index.html LW=540 → 프레임 폭) 100 = 유니티 1 단위. 프레임 높이 = 540×19/9 = 1140 레이아웃 단위.
    /// 월드 x(sim.js 좌표) → 화면: (worldX − cam) × zoom + PLAYER_SCREEN_X (ui.json camera.zoom·playerX).
    /// </summary>
    public sealed class WorldCam : MonoBehaviour
    {
        public const float LayoutW = 540f, LayoutH = LayoutW * 19f / 9f, PPU = 100f;
        public RectTransform Frame;
        Camera _cam;
        Rect _last;

        public static WorldCam Attach(Camera cam, RectTransform frame)
        {
            var wc = cam.gameObject.GetComponent<WorldCam>() ?? cam.gameObject.AddComponent<WorldCam>();
            wc._cam = cam; wc.Frame = frame;
            cam.orthographic = true; cam.orthographicSize = LayoutH / 2f / PPU;
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
        }

        /// <summary>프레임 안 레이아웃 좌표(x: 0~540 왼→오, yFrac: 0~1 위→아래) → 유니티 월드.</summary>
        public static Vector3 ToWorld(float layoutX, float yFrac, float z = 0)
            => new Vector3((layoutX - LayoutW / 2f) / PPU, (0.5f - yFrac) * LayoutH / PPU, z);
        /// <summary>프레임 % 높이 → 유니티 단위.</summary>
        public static float PctH(float pct) => pct / 100f * LayoutH / PPU;
        public static float PctW(float pct) => pct / 100f * LayoutW / PPU;
    }
}
