// dotnet 검사 빌드 전용 서명 스텁 — 유니티에서는 실제 Assets/Layer Lab/2D Minimal-CharacterMaker/Common/Scripts 가 쓰인다.
// 실제 API 와 서명이 다르면 CI 의 유니티 단계에서 잡힌다. (원본: PartsManager.cs · PartsType.cs · AnimationEventReceiver.cs · Player.cs)
using System;
using UnityEngine;

namespace LayerLab.ArtMakerUnity
{
    public enum PartsType { Eye, Hair, Helmet, Beard, Chest, Sword, Axe, Bow, Shield, Wand, Staff, Spear, Blunt, Crossbow, SubItem, Arrow, HelmetHair, Skin }
    public enum ColorTargetType { Skin, Hair, Eye, Beard }
    public class PartsManager : MonoBehaviour
    {
        public void Init() { }
        public int GetPartsCount(PartsType type) => 0;
        public int GetActiveIndex(PartsType type) => 0;
        public void EquipParts(PartsType type, int index) { }
        public void UnequipParts(PartsType type) { }
        public void ToggleParts(PartsType type, bool visible) { }
        public void SetColor(ColorTargetType target, Color color) { }
        public Color GetColor(ColorTargetType target) => Color.white;
        public void PlayAnimation(string animName) { }
        public string GetCurrentAnimation() => "";
        public string[] GetAnimationNames() => Array.Empty<string>();
    }
    public class AnimationEventReceiver : MonoBehaviour
    {
        public event Action OnAttackHitEvent;
        public event Action OnSkillHitEvent;
        public void OnAttackHit() { OnAttackHitEvent?.Invoke(); }
        public void OnSkillHit() { OnSkillHitEvent?.Invoke(); }
    }
    public class Player : MonoBehaviour { public static Player Instance { get; private set; } public PartsManager PartsManager => null; public void Init() { } }
}
