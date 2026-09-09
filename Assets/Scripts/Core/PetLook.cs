namespace KkomaKnight.Core
{
    /// <summary>
    /// 펫 «id → CharacterMaker 그림 키» 규약 (T293 6항 · 주인 2026-09-09 06:1X «이미지는 그 플레이어 썼던 Maker 그대로»).
    /// <para>
    /// <see cref="GearLook"/> 와 같은 결이다 — <b>키의 꼴만</b> 코드가 정하고 <b>어느 파일인가는 <c>catalog.json</c></b> 이 정한다.
    /// 그래서 그림을 갈아 끼울 때 C# 은 한 줄도 안 바뀐다.
    /// </para>
    /// <para>
    /// 키 셋: <c>cm.pet.&lt;id&gt;.helmet</c> · <c>.chest</c> · <c>.hand</c>(오른손 무기).
    /// <b>오른손 슬롯은 표가 정한다</b> — 등급의 <c>shot</c> 이 <c>bolt</c> 면 지팡이(<c>HandRight/Staff</c>), <c>axe</c> 면 도끼(<c>HandRight/Axe</c>).
    /// 곧 «번개를 쏘는 펫은 지팡이를 든다» 가 <b>규칙 하나</b>로 서고, 표에서 등급의 발사체를 바꾸면 손에 든 것도 같이 바뀐다.
    /// </para>
    /// <para>
    /// ⚑ <b>지팡이는 여기서만 쓴다</b> — §1 3항의 «활·지팡이·완드·창 금지» 는 <b>장비 무기</b> 규칙이고(주인 T17 «Axe, Blunt, Sword 중에 골라서»),
    /// 펫 그림은 T293 6항이 «전설 = 지팡이/완드» 라고 따로 적어 둔 자리다. 두 규칙이 부딪히는 것이 아니라 서로 다른 것을 말한다.
    /// </para>
    /// </summary>
    public static class PetLook
    {
        /// <summary>펫 파츠 키 접두 — 장비의 <c>cm.gear.</c> 와 나란한 자리.</summary>
        public const string Prefix = "cm.pet.";

        /// <summary>오른손 파츠 슬롯 이름 — <c>Character.prefab</c> 의 <c>HandRight/&lt;여기&gt;</c>.</summary>
        public const string SlotAxe = "Axe", SlotStaff = "Staff";

        /// <summary>«방패» 역할(피격 발동)이 왼손에 드는 방패 — 기사 방패를 그대로 쓴다(새 그림 0 · 새 키 0).</summary>
        public const string ShieldKey = "cm.knight.shield";

        public static string HelmetKey(string id) => string.IsNullOrEmpty(id) ? null : Prefix + id + ".helmet";
        public static string ChestKey(string id) => string.IsNullOrEmpty(id) ? null : Prefix + id + ".chest";
        public static string HandKey(string id) => string.IsNullOrEmpty(id) ? null : Prefix + id + ".hand";

        /// <summary>
        /// 오른손 슬롯 — <b>표의 등급 <c>shot</c> 이 정한다</b>(번개 = 지팡이 · 도끼 = 도끼). 표를 모르면 도끼로 둔다.
        /// </summary>
        public static string HandSlot(PetData d, PetData.Pet p)
        {
            var g = d == null ? null : d.GradeOfPet(p);
            return g != null && g.Shot == PetKey.ShotBolt ? SlotStaff : SlotAxe;
        }

        /// <summary>왼손 방패 키 — 피격 발동(«방패») 펫만 든다. 나머지는 null(빈손).</summary>
        public static string Shield(PetData.Pet p) => p != null && p.TriggerKey == PetKey.Hit ? ShieldKey : null;
    }
}
