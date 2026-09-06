namespace KkomaKnight.Core
{
    /// <summary>
    /// 장비 «부위 × 세트 × 등급» → CharacterMaker 그림 키 표. 한 항목에 키가 **둘**(T31 · 주인 2026-09-06 «아이콘용 갑옷과 실제 입는 갑옷이 다르게 — 내 게임도 그렇게»):
    /// <list type="bullet">
    /// <item><b>착용 키</b> <see cref="PartKey(string,string,int)"/> = <c>cm.gear.&lt;부위&gt;.&lt;세트&gt;.&lt;등급&gt;</c> → <c>Parts Pack Base/Parts/…</c>(캐릭터에 입히는 그림 · 전투 <c>BattleWorld</c> · 장비 화면/로비 <c>HeroView</c> · <c>CharacterRig.Skin</c>).</item>
    /// <item><b>아이콘 키</b> <see cref="IconKey(string,string,int)"/> = <c>cmi.gear.&lt;부위&gt;.&lt;세트&gt;.&lt;등급&gt;</c> → <c>Parts Pack Base/Thumbnail/…</c>(같은 이름의 128×128 아이콘 그림 · <c>GearUi.Cell</c> · 장착 슬롯 · 세부 팝업 · 대장간 · 뽑기 결과).</item>
    /// </list>
    /// 두 키는 같은 파일 이름(파츠 ↔ Thumbnail)을 가리킨다(GearLookTests 가 대조). 그림이 있는 부위 = 투구(helm)·무기(weapon)·갑옷(armor). 목걸이·장갑·신발은 외형 미반영 · 아이콘은 GUI Pro 아이콘(<c>gi.*</c> · «일단 아무거나»).
    /// 실제 파일 선택은 <c>Assets/KkomaKnight/catalog.json</c>(→ docs/assets-map.md 표) — 등급이 오를수록 더 화려한 파츠.
    /// 무기는 전부 **근접 무기 — 검(Sword)·방망이(Blunt)·도끼(Axe) 세 계열에서**(주인 지시 T17 · 활·지팡이·완드·창 금지). 아이콘 회전은 없다(주인 2026-09-06 · 45° 취소 · T31 Thumbnail 은 정상 방향).
    /// </summary>
    public static class GearLook
    {
        public const string Helm = "helm", Weapon = "weapon", Armor = "armor";
        /// <summary>외형이 바뀌는 부위(가진 그림이 있는 부위).</summary>
        public static readonly string[] LookParts = { Helm, Weapon, Armor };
        /// <summary>등급 수 — gear.json rarName(일반·희귀·전설·신화)과 같아야 한다(테스트가 대조).</summary>
        public const int RarCount = 4;

        /// <summary>
        /// 파츠 아이콘이 칸 안에서 차지하는 시각 크기(불투명 bbox 의 긴 변 ÷ 칸 한 변) — 주인 지시 T17 «칸의 70~75%».
        /// GUI Pro 128px 아이콘이 프리팹 Item(256 × 스케일 0.6149 = 157px · 그림이 그 85%) 에서 차지하는 비율(≈70%)과 같은 눈높이.
        /// </summary>
        public const double PartIconFill = 0.72;

        public static bool HasLook(string part) => part == Helm || part == Weapon || part == Armor;

        /// <summary>
        /// <b>부위</b> 아이콘 키 (T105 · 주인 2026-09-07 «무슨 장비 부위인지 알려주는 아이콘이어야 함 — 투구·갑옷·신발·무기·목걸이·반지 · 지금처럼 치명타·체력·회피 이런 거 아니고») —
        /// 칸의 다이아 배지와 장착 슬롯의 «여기는 무슨 자리» 표시에 쓴다. <b>세트</b> 아이콘(<c>pi.critical</c>·<c>pi.heart</c>·<c>ui.dodge</c>)은 세트 옵션 줄에서만 계속 쓴다.
        /// <para>
        /// 그림은 <b>이미 카탈로그에 있는 것을 그대로 쓴다</b>(T105 1항 «이미 쓰는 것 우선 재사용» · 새 에셋 0 · 카탈로그 불변) — <c>gi.&lt;부위&gt;.crit</c> 여섯 개다.
        /// 투구·무기·갑옷의 <c>gi.*</c> 는 아이템 아이콘이 CharacterMaker Thumbnail(<c>cmi.*</c>)로 바뀌면서 안 쓰이게 된 자리라 «부위를 뜻하는 일반 그림» 으로 딱 맞고,
        /// 목걸이(룬 펜던트)·반지(<c>glove</c> · T88)·신발은 아이템 아이콘과 같은 그림이라 «그 부위» 로 바로 읽힌다.
        /// </para>
        /// </summary>
        public static string PartIcon(string part)
        {
            // 검 · 투구 · 갑옷 · 신발 · 룬 펜던트(목걸이) · 반지(glove = T88 «장갑 → 반지» · 부위 키는 그대로)
            switch (part)
            {
                case Weapon:
                    return "gi.weapon.crit";
                case Helm:
                    return "gi.helm.crit";
                case Armor:
                    return "gi.armor.crit";
                case "boot":
                    return "gi.boot.crit";
                case "neck":
                    return "gi.neck.crit";
                case "glove":
                    return "gi.glove.crit";
            }
            return null;
        }

        /// <summary>착용 키 접두(입는 파츠 · Parts/) 와 아이콘 키 접두(Thumbnail/) — 카탈로그 키는 <c>접두 + 부위.세트.등급</c>.</summary>
        public const string PartPrefix = "cm.gear.", IconPrefix = "cmi.gear.";

        static string Suffix(string part, string set, int rar)
        {
            if (rar < 0) rar = 0; if (rar >= RarCount) rar = RarCount - 1;
            return part + "." + set + "." + rar;
        }

        /// <summary>착용(입는) 파츠 스프라이트 키 <c>cm.gear.*</c> — 캐릭터 외형 전용. 그림 없는 부위는 null.</summary>
        public static string PartKey(string part, string set, int rar) => HasLook(part) ? PartPrefix + Suffix(part, set, rar) : null;
        public static string PartKey(GameData D, GearItem g) => PartKey(g.Part, D.Gear.SetOf(g.Type), g.Rar);

        /// <summary>장비 아이콘 키 — 그림 있는 부위는 같은 이름의 Thumbnail <c>cmi.gear.*</c>(T31 · 입는 파츠와 분리), 나머지는 GUI Pro 아이콘 <c>gi.&lt;부위&gt;.&lt;세트&gt;</c>.</summary>
        public static string IconKey(string part, string set, int rar) => HasLook(part) ? IconPrefix + Suffix(part, set, rar) : ("gi." + part + "." + set);
        public static string IconKey(GameData D, GearItem g) => IconKey(g.Part, D.Gear.SetOf(g.Type), g.Rar);

        /// <summary>무기 세트 → Character 프리팹의 오른손 슬롯: 체력실드 = 둔기(Blunt) · 치명·회피 = 검(Sword) — 카탈로그의 해당 파츠 폴더(HandRight/Sword·Blunt·Axe)와 맞아야 한다(GearLookTests). 창(Spear)·활(Bow) 슬롯은 장비에 쓰지 않는다(T17).</summary>
        public static string WeaponSlot(string set) => set == "hpsh" ? "Blunt" : "Sword";

        /// <summary>파츠 아이콘 맞춤 결과 — Item RectTransform 에 넣을 sizeDelta(W·H) 와 pivot(불투명 그림의 가운데 · 0~1).</summary>
        public struct IconFit { public double W, H, PivotX, PivotY; }

        /// <summary>
        /// 파츠 아이콘 맞춤 계산(순수 · T17). 스프라이트 rect(<paramref name="rw"/>×<paramref name="rh"/> 픽셀) 안의 불투명 bbox
        /// [<paramref name="bx0"/>,<paramref name="bx1"/>]×[<paramref name="by0"/>,<paramref name="by1"/>](픽셀 · 왼쪽아래 원점)가 칸 한 변 <paramref name="frame"/> 의
        /// <paramref name="fill"/> 배(긴 변 기준)로 보이도록 Item 의 sizeDelta 를 정한다 — Item 의 localScale(<paramref name="localScale"/> · 프리팹 0.6149)은 그대로 두고 그만큼 나눠 보정.
        /// sizeDelta 의 비율을 스프라이트 rect 와 같게 두므로 preserveAspect 여백 없이 «rect 1픽셀 = k 칸픽셀» 로 정확히 그려진다.
        /// pivot 은 bbox 의 가운데 — anchoredPosition 0 이면 그림의 가운데가 칸 가운데에 온다(회전은 하지 않는다 · 주인 취소).
        /// 퇴화 입력(빈 bbox · 0 크기 · 0 스케일)은 rect 전체·스케일 1 로 대체한다.
        /// </summary>
        public static IconFit FitPartIcon(double rw, double rh, double bx0, double by0, double bx1, double by1, double frame, double fill, double localScale)
        {
            if (rw <= 0) rw = 1; if (rh <= 0) rh = 1;
            if (frame <= 0) frame = 1; if (fill <= 0) fill = PartIconFill; if (localScale <= 0) localScale = 1;
            double bw = bx1 - bx0, bh = by1 - by0;
            if (bw <= 0 || bh <= 0) { bx0 = 0; by0 = 0; bx1 = rw; by1 = rh; bw = rw; bh = rh; }
            double k = fill * frame / (bw > bh ? bw : bh);   // 스프라이트 1픽셀 → 칸 픽셀
            return new IconFit { W = rw * k / localScale, H = rh * k / localScale, PivotX = (bx0 + bx1) * 0.5 / rw, PivotY = (by0 + by1) * 0.5 / rh };
        }
    }
}
