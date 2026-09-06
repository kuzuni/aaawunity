using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>ItemFrame 의 Item 이 프리팹에서 가진 rect(크기·pivot·자리·회전)를 처음 한 번 기억해 두고, 파츠가 아닌 아이콘(GUI Pro)에는 되돌린다 — 장착 슬롯의 Item 은 Refresh 마다 재사용되므로(무기 → 목걸이 슬롯은 없지만 장착 해제 → 다른 등급 등) 항상 프리팹 값에서 다시 계산한다(T17).</summary>
    public sealed class PartIconFit : MonoBehaviour
    {
        bool _captured; public Vector2 Size, Pivot, Pos; public Quaternion Rot;
        public void Capture(RectTransform rt) { if (_captured) return; _captured = true; Size = rt.sizeDelta; Pivot = rt.pivot; Pos = rt.anchoredPosition; Rot = rt.localRotation; }
        public void Restore(RectTransform rt) { if (!_captured) return; rt.sizeDelta = Size; rt.pivot = Pivot; rt.anchoredPosition = Pos; rt.localRotation = Rot; }
    }
}
