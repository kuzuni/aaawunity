using System.Collections.Generic;
using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// Layer Lab «2D Minimal-CharacterMaker» 의 Character.prefab 인스턴스를 다루는 어댑터.
    /// 프리팹은 파츠 슬롯(투구·무기·…) SpriteRenderer 가 전부 비어 있고 PartsManager 도 없다 —
    /// 그래서 자식 경로로 SpriteRenderer 를 찾아 카탈로그 스프라이트를 꽂고, Animator.Play(상태 이름)으로 애니를 바꾼다.
    /// 애니메이션 이벤트(Attack.anim → OnAttackHit)는 이 컴포넌트가 루트에 있으므로 여기로 온다.
    /// ⚠ 프리팹 자식 이름·구조는 애니 클립이 경로로 묶여 있어 바꾸면 안 된다.
    /// </summary>
    public sealed class CharacterRig : MonoBehaviour
    {
        public const string Idle = "Idle", Walk = "Walk", Run = "Run", Attack = "Attack", Skill = "Skill", Stun = "Stun", Dead = "Dead1", Victory = "Victory", Defeat = "Defeat";

        // 프리팹 자식 경로 (조사 결과 A.2)
        public const string PathBody = "Body", PathHead = "Body/Head", PathEye = "Body/Head/Eye", PathHair = "Body/Head/Hair", PathHairHelmet = "Body/Head/Hair_Helmet",
            PathHelmet = "Body/Head/Helmet", PathBeard = "Body/Head/Beard", PathChest = "Body/Chest",
            PathSword = "HandRight/Sword", PathAxe = "HandRight/Axe", PathSpear = "HandRight/Spear", PathBlunt = "HandRight/Blunt",
            PathBow = "HandRight/Bow/Bow", PathBowLineUp = "HandRight/Bow/Bow_Line_Up", PathBowLineDown = "HandRight/Bow/Bow_Line_Down", PathArrow = "HandRight/Bow/Arrow",
            PathShield = "HandLeft/Shield", PathSubItem = "HandLeft/Sub_Item", PathShadow = "Shadow";

        /// <summary>스킨 명세 — 값은 카탈로그 스프라이트 키(null = 비움). 색은 Body/Head 틴트.</summary>
        public sealed class Skin
        {
            public string Helmet, Chest, Sword, Axe, Spear, Blunt, Bow, Arrow, Shield, SubItem, Hair, HairHelmet, Beard, Eye;
            public Color SkinColor = Color.white;
            public bool BowLines;
        }

        Animator _anim; SpriteRenderer[] _renderers; readonly Dictionary<SpriteRenderer, int> _baseOrder = new Dictionary<SpriteRenderer, int>();
        string _current; System.Action _onAttackHit;
        Material[] _origMats;
        float _attackLen = 1.8333334f, _attackHitAt = 1.0f;   // Attack.anim 길이 · OnAttackHit 이벤트 시각(클립에서 읽고, 못 읽으면 조사값)
        float _speedBase = 1f, _attackEndClock = -1f, _attackHitClock = -1f, _clock;
        /// <summary>월드 시간 배율(배속 x2 등) — <see cref="Tick"/> 이 매 프레임 넘겨준다.</summary>
        public static float TimeScale = 1f;
        /// <summary>Attack.anim 의 OnAttackHit 이벤트가 온 횟수 — 연출 지연 큐가 «칼이 내려온 순간» 을 알아보는 데 쓴다.</summary>
        public int HitCount { get; private set; }

        public static CharacterRig Attach(GameObject instance, System.Action onAttackHit = null)
        {
            var rig = instance.GetComponent<CharacterRig>() ?? instance.AddComponent<CharacterRig>();
            rig._anim = instance.GetComponentInChildren<Animator>(true);
            rig._renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in rig._renderers) rig._baseOrder[r] = r.sortingOrder;
            rig._onAttackHit = onAttackHit;
            if (rig._anim != null && rig._anim.runtimeAnimatorController != null)
                foreach (var c in rig._anim.runtimeAnimatorController.animationClips)
                    if (c != null && c.name == Attack)
                    {
                        rig._attackLen = Mathf.Max(0.1f, c.length);
                        foreach (var e in c.events) if (e.functionName == nameof(OnAttackHit)) rig._attackHitAt = e.time;
                    }
            return rig;
        }

        // Attack.anim 의 AnimationEvent (functionName OnAttackHit · time 1.0) — 루트의 컴포넌트가 받는다
        public void OnAttackHit() { HitCount++; _onAttackHit?.Invoke(); }
        public void OnSkillHit() { HitCount++; _onAttackHit?.Invoke(); }

        /// <summary>
        /// 공격 모션 — 클립을 끊지 않고 끝까지 돌린다(주인 지시 2026-09-05). 다음 공격까지의 간격(interval·초)에 클립(1.83초)이 안 들어가면
        /// 그만큼 빨리 돌린다(최대 ×3 · 그래도 넘치면 회수 동작만 다음 공격에 잘린다). 데미지 연출은 <see cref="HitDelay"/> 뒤(칼이 내려오는 순간)에 붙인다.
        /// </summary>
        public void PlayAttack(double interval)
        {
            if (_anim == null) return;
            float speed = Mathf.Clamp(_attackLen / Mathf.Max(0.2f, (float)interval), 1f, 3f);
            _speedBase = speed; _anim.speed = speed * TimeScale;
            _current = Attack; _anim.Play(Attack, 0, 0f);
            _attackEndClock = _clock + _attackLen / speed; _attackHitClock = _clock + _attackHitAt / speed;
        }
        /// <summary>지금 공격 클립이 아직 끝나지 않았나 — 이 동안 Idle/Walk 로 바꾸지 않는다.</summary>
        public bool Attacking => _current == Attack && _clock < _attackEndClock;
        /// <summary>마지막 PlayAttack 기준, 칼이 내려오는 순간까지 남은 월드 초(음수면 지났다).</summary>
        public float HitDelay => _attackHitClock - _clock;
        /// <summary>매 프레임(월드 dt · 배속 반영) — 내부 시계와 애니 속도 배율.</summary>
        public void Tick(float dt) { _clock += dt; if (_anim != null) _anim.speed = _speedBase * TimeScale; }

        SpriteRenderer Sr(string path) { var t = transform.Find(path); return t != null ? t.GetComponent<SpriteRenderer>() : null; }
        void SetSprite(string path, string key)
        {
            var sr = Sr(path); if (sr == null) return;
            var sp = string.IsNullOrEmpty(key) ? null : App.I.Assets.Sprite(key);
            sr.sprite = sp; sr.gameObject.SetActive(sp != null || path == PathBody || path == PathHead);
        }

        public void Apply(Skin s)
        {
            SetSprite(PathHelmet, s.Helmet); SetSprite(PathChest, s.Chest);
            SetSprite(PathSword, s.Sword); SetSprite(PathAxe, s.Axe); SetSprite(PathSpear, s.Spear); SetSprite(PathBlunt, s.Blunt);
            SetSprite(PathBow, s.Bow); SetSprite(PathArrow, s.Arrow); SetSprite(PathShield, s.Shield); SetSprite(PathSubItem, s.SubItem);
            SetSprite(PathBeard, s.Beard);
            if (!string.IsNullOrEmpty(s.Eye)) SetSprite(PathEye, s.Eye);
            // 투구가 있으면 Hair 대신 Hair_Helmet (PartsManager.SyncHelmetHairVisibility 와 같은 규칙)
            bool helmet = !string.IsNullOrEmpty(s.Helmet);
            SetSprite(PathHair, helmet ? null : s.Hair);
            SetSprite(PathHairHelmet, helmet ? s.HairHelmet : null);
            var up = transform.Find(PathBowLineUp); var dn = transform.Find(PathBowLineDown);
            if (up != null) up.gameObject.SetActive(s.BowLines); if (dn != null) dn.gameObject.SetActive(s.BowLines);
            foreach (var p in new[] { PathBody, PathHead }) { var sr = Sr(p); if (sr != null) sr.color = s.SkinColor; }
        }

        public void Play(string state, bool restart = false)
        {
            if (_anim == null) return;
            if (!restart && _current == state) return;
            _current = state; _speedBase = 1f; _anim.speed = TimeScale; _attackEndClock = -1f;
            _anim.Play(state, 0, 0f);
        }
        public string Current => _current;
        public void SetSpeed(float s) { if (_anim != null) _anim.speed = s; }

        /// <summary>오른쪽 보기 = 프리팹 기본. 적은 왼쪽을 본다(X 스케일 반전).</summary>
        public void Face(bool right) { var s = transform.localScale; s.x = Mathf.Abs(s.x) * (right ? 1 : -1); transform.localScale = s; }
        public void SetScale(float k) { var s = transform.localScale; transform.localScale = new Vector3(Mathf.Sign(s.x) * k, k, 1); }

        /// <summary>정렬 밴드 — 프리팹의 상대 순서(1~13)를 유지한 채 base 를 더한다.</summary>
        public void SetSortingBase(int baseOrder) { foreach (var kv in _baseOrder) if (kv.Key != null) kv.Key.sortingOrder = baseOrder + kv.Value; }

        public void SetAlpha(float a) { foreach (var r in _renderers) if (r != null) { var c = r.color; c.a = a; r.color = c; } }

        /// <summary>피격 플래시 — AllIn1SpriteShader 머티리얼(HITEFFECT_ON)로 잠시 갈아끼운다.</summary>
        public void Flash(Material flashMat, float seconds)
        {
            if (flashMat == null || _renderers == null) return;
            if (_origMats == null) { _origMats = new Material[_renderers.Length]; for (int i = 0; i < _renderers.Length; i++) _origMats[i] = _renderers[i].sharedMaterial; }
            foreach (var r in _renderers) if (r != null) r.sharedMaterial = flashMat;
            CancelInvoke(nameof(Unflash)); Invoke(nameof(Unflash), seconds);
        }
        void Unflash() { if (_origMats == null) return; for (int i = 0; i < _renderers.Length; i++) if (_renderers[i] != null) _renderers[i].sharedMaterial = _origMats[i]; }

        public Bounds Bounds()
        {
            var b = new Bounds(transform.position, Vector3.zero); bool any = false;
            foreach (var r in _renderers) if (r != null && r.enabled && r.gameObject.activeInHierarchy && r.sprite != null && r.color.a > 0.01f) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return b;
        }
    }
}
