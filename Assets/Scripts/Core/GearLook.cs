namespace KkomaKnight.Core
{
    /// <summary>
    /// 장비 «부위 × 세트 × 등급» → CharacterMaker 파츠 스프라이트 키(카탈로그 <c>cm.gear.&lt;부위&gt;.&lt;세트&gt;.&lt;등급&gt;</c>) 표.
    /// 장착 외형(전투 <c>BattleWorld</c> · 장비 화면/로비 <c>HeroView</c>)과 장비 아이콘(<c>GearUi.Cell</c> · 슬롯)이 **같은 표**를 쓴다(주인 지시 2026-09-05 · 승인 대기 26).
    /// 그림이 있는 부위 = 투구(helm)·무기(weapon)·갑옷(armor). 목걸이·장갑·신발은 외형 미반영 · 아이콘은 GUI Pro 아이콘(<c>gi.*</c> · «일단 아무거나»).
    /// 실제 파일 선택은 <c>Assets/KkomaKnight/catalog.json</c>(→ docs/assets-map.md 표) — 등급이 오를수록 더 화려한 파츠.
    /// </summary>
    public static class GearLook
    {
        public const string Helm = "helm", Weapon = "weapon", Armor = "armor";
        /// <summary>외형이 바뀌는 부위(가진 그림이 있는 부위).</summary>
        public static readonly string[] LookParts = { Helm, Weapon, Armor };
        /// <summary>등급 수 — gear.json rarName(일반·희귀·전설·신화)과 같아야 한다(테스트가 대조).</summary>
        public const int RarCount = 4;

        public static bool HasLook(string part) => part == Helm || part == Weapon || part == Armor;

        /// <summary>파츠 스프라이트 키 — 그림 없는 부위는 null.</summary>
        public static string PartKey(string part, string set, int rar)
        {
            if (!HasLook(part)) return null;
            if (rar < 0) rar = 0; if (rar >= RarCount) rar = RarCount - 1;
            return "cm.gear." + part + "." + set + "." + rar;
        }
        public static string PartKey(GameData D, GearItem g) => PartKey(g.Part, D.Gear.SetOf(g.Type), g.Rar);

        /// <summary>장비 아이콘 키 — 그림 있는 부위는 파츠 스프레이트 그대로, 나머지는 GUI Pro 아이콘 <c>gi.&lt;부위&gt;.&lt;세트&gt;</c>.</summary>
        public static string IconKey(string part, string set, int rar) => PartKey(part, set, rar) ?? ("gi." + part + "." + set);
        public static string IconKey(GameData D, GearItem g) => IconKey(g.Part, D.Gear.SetOf(g.Type), g.Rar);

        /// <summary>무기 세트 → Character 프리팹의 오른손 슬롯: 치명 = 검(Sword) · 체력실드 = 둔기(Blunt) · 회피 = 창(Spear).</summary>
        public static string WeaponSlot(string set) => set == "hpsh" ? "Blunt" : set == "evade" ? "Spear" : "Sword";
    }
}
