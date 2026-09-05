using System;
using LayerLab.ArtMakerUnity;
using UnityEngine;

namespace KkomaKnight.Adapters
{
    /// <summary>
    /// Layer Lab «2D Minimal-CharacterMaker» 의 Character 프리팹을 감싸는 얇은 어댑터.
    /// 게임 코드는 이 클래스만 보고(LayerLab 타입을 직접 안 씀), dotnet 검사 빌드는 tools/dotnet/Stubs 의 서명 스텁으로 컴파일한다.
    /// 프리팹의 `Player`(싱글턴 · 중복이면 스스로 Destroy) 는 쓰지 않고 PartsManager 를 직접 다룬다.
    /// </summary>
    public sealed class CharacterRig : MonoBehaviour
    {
        public const string AnimIdle = "Idle", AnimWalk = "Walk", AnimAttack = "Attack", AnimHit = "Hit", AnimStun = "Stun", AnimDead = "Dead", AnimSkill = "Skill";

        PartsManager _pm;
        AnimationEventReceiver _ev;
        SpriteRenderer[] _renderers;
        string _current;

        /// <summary>파츠 명세 — 카탈로그(catalog.json «parts.*»)에서 온 인덱스. -1 = 벗김.</summary>
        [Serializable]
        public struct Parts
        {
            public int Skin, Eye, Hair, Helmet, Beard, Chest, Sword, Axe, Bow, Spear, Blunt, Shield, SubItem;
            public Color SkinColor, HairColor;
            public static Parts Default => new Parts { Skin = 0, Eye = 0, Hair = -1, Helmet = -1, Beard = -1, Chest = -1, Sword = -1, Axe = -1, Bow = -1, Spear = -1, Blunt = -1, Shield = -1, SubItem = -1, SkinColor = Color.white, HairColor = Color.white };
        }

        /// <summary>Character 프리팹 인스턴스에 붙여 초기화한다. attackHit 는 공격 애니의 타격 프레임 콜백.</summary>
        public static CharacterRig Attach(GameObject instance, Action attackHit = null)
        {
            var rig = instance.GetComponent<CharacterRig>() ?? instance.AddComponent<CharacterRig>();
            rig._pm = instance.GetComponentInChildren<PartsManager>(true);
            rig._ev = instance.GetComponentInChildren<AnimationEventReceiver>(true);
            var player = instance.GetComponentInChildren<Player>(true);
            if (player != null) Destroy(player);   // 싱글턴 컴포넌트 — 두 번째 인스턴스를 스스로 파괴하므로 떼어낸다
            if (rig._pm != null) rig._pm.Init();
            rig._renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
            if (rig._ev != null && attackHit != null) rig._ev.OnAttackHitEvent += attackHit;
            return rig;
        }

        public void Apply(Parts p)
        {
            if (_pm == null) return;
            Set(PartsType.Skin, p.Skin); Set(PartsType.Eye, p.Eye); Set(PartsType.Hair, p.Hair); Set(PartsType.Helmet, p.Helmet); Set(PartsType.Beard, p.Beard); Set(PartsType.Chest, p.Chest);
            Set(PartsType.Sword, p.Sword); Set(PartsType.Axe, p.Axe); Set(PartsType.Bow, p.Bow); Set(PartsType.Spear, p.Spear); Set(PartsType.Blunt, p.Blunt); Set(PartsType.Shield, p.Shield); Set(PartsType.SubItem, p.SubItem);
            if (p.SkinColor.a > 0) _pm.SetColor(ColorTargetType.Skin, p.SkinColor);
            if (p.HairColor.a > 0) _pm.SetColor(ColorTargetType.Hair, p.HairColor);
        }
        void Set(PartsType t, int idx)
        {
            if (_pm.GetPartsCount(t) == 0) return;
            if (idx < 0) _pm.UnequipParts(t); else _pm.EquipParts(t, idx);
        }
        public int Count(PartsType t) => _pm != null ? _pm.GetPartsCount(t) : 0;

        public void Play(string anim, bool restart = false)
        {
            if (_pm == null) return;
            if (!restart && _current == anim) return;
            _current = anim; _pm.PlayAnimation(anim);
        }
        public string Current => _current;
        public string[] AnimationNames => _pm != null ? _pm.GetAnimationNames() : Array.Empty<string>();

        /// <summary>왼쪽을 보게(적은 플레이어를 향한다) — 프리팹 기본이 오른쪽 보기라 X 스케일을 뒤집는다.</summary>
        public void Face(bool right) { var s = transform.localScale; s.x = Mathf.Abs(s.x) * (right ? 1 : -1); transform.localScale = s; }

        public void SetSortingOrder(int order) { if (_renderers == null) return; foreach (var r in _renderers) r.sortingOrder = order + (r.sortingOrder % 100); }
        public void SetLayer(string layer) { if (_renderers == null) return; foreach (var r in _renderers) r.sortingLayerName = layer; }

        /// <summary>전 렌더러의 머티리얼을 갈아끼운다(AllIn1SpriteShader 피격 플래시용). 원래 머티리얼은 돌려받는다.</summary>
        public Material[] SwapMaterials(Material m)
        {
            if (_renderers == null) return Array.Empty<Material>();
            var old = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) { old[i] = _renderers[i].sharedMaterial; _renderers[i].sharedMaterial = m; }
            return old;
        }
        public void RestoreMaterials(Material[] old)
        {
            if (_renderers == null || old == null) return;
            for (int i = 0; i < _renderers.Length && i < old.Length; i++) if (_renderers[i] != null) _renderers[i].sharedMaterial = old[i];
        }
        public void SetAlpha(float a) { if (_renderers == null) return; foreach (var r in _renderers) { var c = r.color; c.a = a; r.color = c; } }
        public Bounds Bounds()
        {
            var b = new Bounds(transform.position, Vector3.zero); bool any = false;
            if (_renderers != null) foreach (var r in _renderers) if (r.enabled && r.gameObject.activeInHierarchy && r.sprite != null) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return b;
        }
    }
}
