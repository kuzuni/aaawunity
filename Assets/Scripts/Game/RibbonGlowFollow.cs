using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T320 ⓑ — 리본 뒤 «반 잘린 빛» 담개가 <b>리본을 따라다니게</b> 한다.
    /// <para>
    /// 왜 컴포넌트인가: 공통 팝업(<see cref="UiKit.Popup"/>)이 리본을 세운 <b>뒤에</b> 화면이 그 리본을 제 표 자리로
    /// 다시 잡는 일이 흔하다(<c>LobbyPopups.Ribbon</c> 이 출석·기프트·탐험 …에서 하는 일 · 확률 팝업의 명판도 같다).
    /// 그때마다 화면 코드가 «빛도 옮겨라» 를 한 줄씩 부르게 하면 <b>부르는 곳이 다시 여러 곳</b>이 되고,
    /// 그중 하나를 빠뜨리면 그 팝업만 빛이 옛 자리에 남는다 — 게다가 그 파일들은 자주 <b>남의 lock</b> 안이다.
    /// ⇒ 담개가 스스로 따라가면 화면 코드는 <b>한 줄도 안 바뀐다</b>.
    /// </para>
    /// 값이 실제로 달라졌을 때만 다시 잡는다(리본이 가만히 있으면 아무 일도 안 한다).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RibbonGlowFollow : MonoBehaviour
    {
        public RectTransform Ribbon;
        Vector2 _size, _pos;
        bool _seen;

        void LateUpdate()
        {
            if (Ribbon == null) { enabled = false; return; }
            var size = Ribbon.rect.size;
            var pos = Ribbon.anchoredPosition;
            if (_seen && Approximately(size, _size) && Approximately(pos, _pos)) return;
            _seen = true; _size = size; _pos = pos;
            Overlay.PlaceRibbonGlow((RectTransform)transform, Ribbon);
        }

        static bool Approximately(Vector2 a, Vector2 b) => Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f;
    }
}
