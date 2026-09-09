using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T344 — 그라데이션 두 겹을 <b>가로(왼쪽 → 오른쪽)</b>로 눕힌다(주인 2026-09-10 «패스들 … 그라디언트가 왼쪽 오른쪽 이어야 하는데 상하로 되어 있네»).
    /// <para>
    /// ⚠ <b>조각을 새로 만들지 않는다</b>(§1 «코드 도형 0 · 새 그림 0»). 우리 그라데이션 조각은 <c>ui.gradTop1</c>(4×259 · 위가 밝다) ·
    /// <c>ui.gradBottom</c>(4×543 · 아래가 밝다) 두 장의 <b>세로 램프</b>다. 그것을 <b>90° 돌려</b> 쓰면 그대로 가로 램프가 된다 —
    /// 돌리면 <c>GradientTop</c> 의 밝은 끝이 <b>왼쪽</b>, <c>GradientBottom</c> 의 밝은 끝이 <b>오른쪽</b>으로 간다.
    /// 그래서 표(<see cref="GradientPalette"/>)의 <c>Top</c> 이 «왼쪽 색», <c>Bottom</c> 이 «오른쪽 색» 이 된다.
    /// </para>
    /// <para>
    /// ⚠ <b>돌린 사각형은 «늘리기(Stretch)» 로 못 채운다</b> — 앵커 0~1 로 늘린 뒤 돌리면 긴 쪽이 부모 밖으로 나가고 짧은 쪽에 빈 띠가 남는다.
    /// 그래서 가운데 앵커 + <b>가로·세로를 바꿔 넣은</b> 고정 크기로 잡는다. 부모 크기는 레이아웃이 돈 <b>뒤</b>에야 정해지고(그 전에는 0),
    /// 이 열은 해상도로 바뀔 수 있으므로 <see cref="SafeAreaRoot"/> 와 같은 꼴로 <b>바뀌었을 때만</b> 다시 잡는다(안 바뀌면 매 프레임 비용 0).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GradientSideways : MonoBehaviour
    {
        /// <summary>돌리는 각(도) — <c>+90°</c> 라야 위(밝은 끝)가 <b>왼쪽</b>으로 간다.</summary>
        public const float TurnDeg = 90f;

        RectTransform _rt; Vector2 _last; bool _has;

        void Awake() { _rt = (RectTransform)transform; Apply(true); }
        void OnEnable() { Apply(true); }
        void Update() { Apply(false); }

        /// <summary>지금 부모 크기로 두 겹을 다시 눕힌다. <paramref name="force"/> 가 아니면 크기가 그대로일 때 아무 일도 안 한다.</summary>
        public void Apply(bool force)
        {
            if (_rt == null) _rt = (RectTransform)transform;
            var size = _rt.rect.size;
            // 레이아웃 전 — 다음 프레임에 다시 본다(_has 를 안 세운다)
            if (size.x <= 0f || size.y <= 0f) return;
            if (!force && _has && size == _last) return;
            _last = size; _has = true;
            Lay(_rt.Find(UiKit.GradientTopName) as RectTransform, size);
            Lay(_rt.Find(UiKit.GradientBottomName) as RectTransform, size);
        }

        /// <summary>자(테스트)용 — 이 겹이 눕혀져 있는가(90° · 가운데 앵커 · 가로·세로가 부모와 바뀌어 있다).</summary>
        public static bool IsSideways(RectTransform layer)
        {
            if (layer == null) return false;
            var parent = layer.parent as RectTransform; if (parent == null) return false;
            float z = Mathf.DeltaAngle(0f, layer.localEulerAngles.z);
            return Mathf.Abs(z - TurnDeg) < 0.5f
                && layer.anchorMin == new Vector2(0.5f, 0.5f) && layer.anchorMax == layer.anchorMin
                && Mathf.Abs(layer.sizeDelta.x - parent.rect.height) < 0.5f
                && Mathf.Abs(layer.sizeDelta.y - parent.rect.width) < 0.5f;
        }

        static void Lay(RectTransform t, Vector2 size)
        {
            if (t == null) return;
            t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f);
            t.pivot = new Vector2(0.5f, 0.5f);
            t.anchoredPosition = Vector2.zero;
            // 돌린 뒤에 부모를 꽉 채우도록 가로·세로를 바꿔 넣는다
            t.sizeDelta = new Vector2(size.y, size.x);
            t.localEulerAngles = new Vector3(0f, 0f, TurnDeg);
        }
    }
}
