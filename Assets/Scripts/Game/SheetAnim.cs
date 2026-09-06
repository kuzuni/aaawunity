using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 스프라이트 시트 한 번 재생(T70 번개) — <see cref="Fx.PlaySheet"/> 가 붙인다.
    /// 프레임을 순서대로 넘기고 끝나면 <see cref="OnEnd"/> 를 부른 뒤 스스로 파괴한다(DOTween 을 쓰지 않으므로 SetLink 대상이 아니다).
    /// <see cref="Delay"/> 동안은 <see cref="SpriteRenderer"/> 를 끈 채 기다린다(적마다 어긋나게 떨어뜨리는 stagger).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SheetAnim : MonoBehaviour
    {
        public Sprite[] Frames;
        public float Fps = 16f;
        public float Delay;
        /// <summary>마지막 프레임이 끝난 순간(파괴 직전) 한 번 — 번개가 «닿는» 순간의 튀김 이펙트를 여기에 건다.</summary>
        public System.Action OnEnd;

        SpriteRenderer _sr; float _t; int _shown = -1;

        void Awake() { _sr = UiKit.Ensure<SpriteRenderer>(gameObject); }

        void OnEnable() { _t = 0f; _shown = -1; if (_sr != null) _sr.enabled = false; }

        void Update()
        {
            if (_sr == null || Frames == null || Frames.Length == 0) { Object.Destroy(gameObject); return; }
            _t += Time.deltaTime;
            float show = _t - Delay;
            if (show < 0f) return;
            int i = Fps > 0f ? Mathf.FloorToInt(show * Fps) : 0;
            if (i >= Frames.Length)
            {
                var cb = OnEnd; OnEnd = null;
                Object.Destroy(gameObject);
                if (cb != null) cb();
                return;
            }
            if (i != _shown) { _shown = i; _sr.sprite = Frames[i]; _sr.enabled = true; }
        }
    }
}
