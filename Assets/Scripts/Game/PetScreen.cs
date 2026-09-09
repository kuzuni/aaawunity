using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 펫 탭 = 레퍼런스 <c>docs/ref/13_pet.jpg</c> 구도 · 펫 세부 팝업 = <c>14_pet_detail.jpg</c> (T42 · 주인 2026-09-06 «UI 는 무조건 레퍼런스 기준» · T32 «Character_Skill 그대로» 폐기 · 주인 ⓔ «시스템이 없는 화면은 레이아웃 껍데기»).
    /// 펫 시스템은 없다 — <b>전부 표시만</b>(버튼은 눌러도 아무 일 없음 · 숫자는 0 · 슬롯은 잠금/빈 칸 · 레퍼런스 숫자를 베끼지 않는다). ref-layout ⑩·⑪ 표(<see cref="Layout.PetGrid"/> …) 자리에 GUI Pro 조각을 조립한다:
    /// ① 상단 재화 바(<see cref="TopBar"/>) → ② <b>4열 격자 9칸</b>(칸 = ItemFrame_01 조각 + 파란 등급 변형 + GUI Pro 아이콘 · 칸 위 «Lv. 0» · 칸 아래 진행바 «0/0») → ③ <b>합계 줄</b>(«+0 ❤ | +0 🛡 | +0 🗡»)
    /// → ④ <b>«장착중» 띠</b>(어두운 패널 + 초록 라벨 + 슬롯 4 = 잠금 원 2 · 빈 칸 2) → ⑤ 회색 <b>전체 강화 · 빠른 장착</b> → ⑥ 주황 <b>소환 · 소환 x10</b>(가격 자리는 <b>비어 있다</b> — 펫 시스템이 없어 값을 지어내지 않는다 · 흐리게 + 누르면 토스트 · T178) → ⑦ 탭 바.
    /// 칸을 누르면 세부 팝업(공통 팝업 문법 <see cref="UiKit.Popup"/> · 명판 없음 · 칸이 상자 윗변에 걸침 · 설명 박스 · «패시브:» 수치 줄 · 강화(회색) · 장착(주황) · «탭하여 닫기»).
    /// 글자(T63-pet · 주인 «글씨 너무 작다»): 전부 <see cref="UiKit"/> 하한(본문 40 · 버튼 44) — 직접 박은 크기는 없다. 진행바 «n/m» 만 표 높이에 안 들어가 바를 <see cref="Layout.PetBarH"/> 로 키웠다(13·14 게이트 잘림 0).
    /// 테두리(T69-pet · 주인 «행·카드·칸마다 검은 아웃라인» + 7항 «아이템류 칸 = 장비 화면의 그 프레임»): 격자 9칸·빈 장착 슬롯·세부 칸의 ItemFrame Border 링을 <see cref="GearUi.DarkFrame"/> 로 Ink 8px · 잠금 슬롯(원)은 <see cref="CircleBorderKey"/> 굵은 원형 조각.
    /// 합계 줄(«+0 ❤ | +0 🛡 | +0 🗡»)은 레퍼런스 13 에 상자가 없는 맨 글자라 <see cref="BorderAudit.Exempt"/>(결정 171). 13·14 는 BorderAudit strict.
    /// 이름 계약(스모크 테스트): 격자 <c>PetGrid/Pet:N</c> · 슬롯 <c>Slots/Slot:N</c> · 버튼 <c>UpgradeAllBtn/QuickEquipBtn/SummonBtn/Summon10Btn</c> · 세부 <c>PetDetailCell/PetUpgradeBtn/PetEquipBtn</c> · 탭 바 <c>ui.tabBar</c>.
    /// 펫 시스템이 생기면 <see cref="Icons"/>·«0/0»·«+0» 자리에 pets.json 값을 넣는다(배치는 그대로).
    /// </summary>
    public sealed class PetScreen : GameScreen
    {
        public override string Name => "pet";
        public const int SlotCount = 4, LockedSlots = 2;
        /// <summary>잠금 슬롯(원)의 «검은 아웃라인» 조각(T69-pet) = <c>BasicFrame_Circle_H69_White_Border2</c>(82×84 · 선 8px = 지름의 9.8% 실측) · 슬롯 rect 보다 <see cref="CircleBorderOut"/> px 밖으로 키워 지름 ≈ 84 → 선 ≈ 8.2px(≥ <see cref="UiKit.BorderPx"/>). 원형은 9-slice 가 없어 multiplier 로는 못 굵힌다(결정 171).</summary>
        public const string CircleBorderKey = "fr.circleBorder2";
        public const float CircleBorderOut = 3f;
        /// <summary>격자 9칸의 아이콘(GUI Pro UniqueIcon · 카탈로그 <c>pet.*</c> · 그림은 점수 밖 · 에셋 안에 있는 것으로 · 레퍼런스 순서: 빵·불꽃·화살·망치·화살 다발·베기·멧돼지·천사·문어 자리).</summary>
        public static readonly string[] Icons = { "pet.bread", "pet.fire", "pet.bow", "pet.hammer", "pet.rocket", "pet.sickle", "pet.egg", "pet.feather", "pet.eye" };
        static readonly Layout.R Frame = new Layout.R(0, 0, 100, 100);

        TopBar _top; readonly RectTransform[] _cells = new RectTransform[Layout.PetCount];

        static Layout.R Shift(Layout.R r, float dx, float dy) => new Layout.R(r.X + dx, r.Y + dy, r.W, r.H);

        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Color.Lerp(Palette.Slate, Palette.Dim, 0.6f); bg.raycastTarget = true;   // 어두운 바탕(레퍼런스 · 색은 점수 밖)
            UiKit.PatternBg(Root, UiKit.PatternTintDark);   // T72 ① 배경 패턴(어두운 바탕 → 흰 무늬 α0.12 · 오른쪽 위로 천천히 · 바탕 바로 위 = 형제 0)

            // ① 상단 재화 바 — 공용 헬퍼(아바타 · 전투력 · 골드 · 보석)
            _top = TopBar.Build(App, Root);

            // ② 4열 격자 9칸 — 칸(정사각 조각) · 위 «Lv. 0» · 아래 진행바 «0/0» · 클릭 = 세부 팝업
            var grid = UiKit.Rect(Root, "PetGrid"); UiKit.Stretch(grid);
            RectTransform lv0 = null, bar0 = null;
            for (int i = 0; i < Layout.PetCount; i++)
            {
                int col = i % Layout.PetCols, row = i / Layout.PetCols; float dx = col * Layout.PetColPitch, dy = row * Layout.PetRowPitch;
                int idx = i;
                _cells[i] = PetCell(grid, Frame, Shift(Layout.PetCell, dx, dy), Shift(Layout.PetLv, dx, dy), Shift(Layout.PetBar, dx, dy), i, () => OpenDetail(idx), out var lv, out var bar);
                if (i == 0) { lv0 = lv; bar0 = bar; }
            }

            // ③ 합계 줄 — «+0 ❤ | +0 🛡 | +0 🗡»(펫 시스템 없음 → 0)
            var sum = UiKit.Rect(Root, "SumRow"); UiKit.Pct(sum, Layout.PetSum);
            SumGroup(sum, 0, 26, "pi.heart", Palette.Red); Sep(sum, 30); SumGroup(sum, 38, 26, "pi.shield", Palette.Sky); Sep(sum, 66); SumGroup(sum, 74, 26, "pi.attack", Palette.White);

            // ④ «장착중» 띠 — 어두운 패널 + 초록 꼬리 라벨(조각을 표 칸에 배율로) + 슬롯 4(잠금 원 2 · 빈 칸 2)
            var band = UiKit.Spawn("ui.frameDark", Root); var brt = (RectTransform)band.transform; brt.name = "EqBand"; UiKit.Pct(brt, Layout.PetEqBand);
            var eqLabel = UiKit.Rect(Root, "EqLabel"); UiKit.Pct(eqLabel, Layout.PetEqLabel);
            {
                // 조각(Label_Tapered_02)은 글자 폭에 맞춰 스스로 줄어드는 조각(HorizontalLayoutGroup + ContentSizeFitter · Bg/Border 는 stretch 9-slice) — 회차 1 감점(폭이 레퍼런스의 절반) → 자기 크기 조절을 끄고 표 칸에 꽉 채운다
                var piece = UiKit.Spawn("ui.label.green", eqLabel); var prt = (RectTransform)piece.transform;
                var csf = piece.GetComponent<ContentSizeFitter>(); if (csf != null) csf.enabled = false;
                var hl = piece.GetComponent<HorizontalLayoutGroup>(); if (hl != null) hl.enabled = false;
                UiKit.Stretch(prt);
                var t = piece.GetComponentInChildren<TMP_Text>(true);
                // T194 에서 이 줄 끝에 `EnsureOutline` 을 더했던 까닭은 «크기를 바꿨으면 두께도 다시 잰다» 였다
                // (그때는 테가 «크기 × 비율 = px» 라 bestFit 최대만 44 로 올리면 자가 재는 크기와 어긋났다).
                // T221 — 그 까닭은 T207 ② 로 사라졌다: 테가 SDF 머티리얼이라 두께가 크기에 저절로 비례한다.
                // 그래도 이 호출은 남긴다 — 조각 글자에 주인 글꼴·검정 테를 입히는 입구가 바로 이 함수다.
                if (t != null) { t.text = "장착중"; UiKit.Pct(t.rectTransform, 8, 0, 84, 100); t.alignment = UiKit.TmpAlign(TextAnchor.MiddleCenter); t.enableAutoSizing = true; t.fontSizeMin = TextSize.BestFitMin; t.fontSizeMax = 44; t.textWrappingMode = TextWrappingModes.NoWrap; UiKit.EnsureOutline(t); }
            }
            var slotsHost = UiKit.Rect(Root, "Slots"); UiKit.Stretch(slotsHost);
            var slots = new RectTransform[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                var s = slots[i] = UiKit.Rect(slotsHost, "Slot:" + i); UiKit.Pct(s, Shift(Layout.PetSlot, i * Layout.PetSlotPitch, 0));
                if (i < LockedSlots)
                {
                    var c = UiKit.Icon(s, "Bg", "fr.circle", Palette.A(Palette.Dim, 0.7f)); UiKit.Stretch(c.rectTransform);
                    // T69-pet «검은 아웃라인»: 원형 조각은 9-slice 가 없어 multiplier 로 못 굵힌다 → 굵은 조각(Border2 · 선 9.8%)을 슬롯보다 CircleBorderOut px 크게 = 선 ≥ 8px(폰 3px) · Ink α0.9
                    var b = UiKit.Icon(s, UiKit.BorderName, CircleBorderKey, UiKit.BorderInk); UiKit.Stretch(b.rectTransform, -CircleBorderOut, -CircleBorderOut, -CircleBorderOut, -CircleBorderOut);
                    var lk = UiKit.Icon(s, "Lock", "ui.iconLock"); UiKit.Pct(lk.rectTransform, 28, 28, 44, 44);
                }
                else
                {
                    // 빈 칸 = 회색 등급 프레임 + «+»(Add_1) — ItemFrame_01 은 NormalArea 가 비어 있고 Add_1 이 기본 꺼짐이라 그대로 두면 아무것도 안 보인다(회차 1 감점 · 슬롯 3·4 빠짐)
                    var f = UiKit.Spawn("ui.itemFrame.empty", s); f.name = "ItemFrame_01"; var frt = (RectTransform)f.transform; UiKit.FitScale(frt, UiKit.PxSize(Layout.PetSlot));
                    UiKit.Hide(f.transform, "Text_Level", "Focus", "Disable", "Lock", "Add_2", "Item");
                    var area = UiKit.Find(f.transform, "NormalArea"); if (area != null) { UiKit.Clear(area); var v = UiKit.Spawn("ui.itemFrame.gray", area); UiKit.Stretch((RectTransform)v.transform); }
                    UiKit.Show(f.transform, "Add_1", true);
                    GearUi.DarkFrame(frt, frt.localScale.x);   // T69-pet · 7항: 빈 장착 슬롯도 «물건 칸» = ItemFrame 의 Border 링을 Ink 로(조각 배율 0.41 만큼 더 굵게 · 결정 171)
                }
                UiKit.Clickable(s, () => { });   // 껍데기 — 눌러도 아무 일 없음
            }

            // ⑤ 회색 보조 버튼 2 → ⑥ 주황 소환 버튼 2(가격 자리 없음 · 흐리게 + 누르면 «준비 중» 토스트 · T178) → ⑦ 탭 바
            var up = UiKit.Button(Root, "ui.btnGray", "전체 강화", () => { }, Layout.PetUpgradeAll); up.name = "UpgradeAllBtn";
            var qe = UiKit.Button(Root, "ui.btnGray", "빠른 장착", () => { }, Layout.PetQuickEquip); qe.name = "QuickEquipBtn";
            var sm = SummonButton("SummonBtn", "소환", Layout.PetSummon); var sm10 = SummonButton("Summon10Btn", "소환 x10", Layout.PetSummon10);
            NavBar.Attach(this, Root, "pet");

            // 비평 이름표(T46 · ref-layout ⑩ 의 «요소» 이름 그대로)
            UiKit.Tag(_top.Root, "상단 바");
            UiKit.TagGroup(grid, "펫 격자(9칸)", _cells); UiKit.Tag(_cells[0], "펫 칸(1칸)"); UiKit.Tag(lv0, "펫 Lv 라벨(1칸)"); UiKit.Tag(bar0, "펫 진행바(1칸)");
            UiKit.Tag(sum, "합계 줄"); UiKit.Tag(brt, "장착 띠"); UiKit.Tag(eqLabel, "«장착중» 라벨");
            UiKit.TagGroup(slotsHost, "장착 슬롯 줄(4칸)", slots); UiKit.Tag(slots[0], "장착 슬롯 1칸");
            UiKit.Tag(up, "전체 강화 버튼"); UiKit.Tag(qe, "빠른 장착 버튼"); UiKit.Tag(sm, "소환 버튼"); UiKit.Tag(sm10, "소환 x10 버튼");
            UiKit.Tag(UiKit.Find(Root, "ui.tabBar"), "하단 탭바");
        }

        /// <summary>
        /// 펫 칸 하나 — <paramref name="parent"/>(프레임 % 사각형 <paramref name="parentR"/>) 안에 칸(<paramref name="cellR"/>) · 칸 위 «Lv. 0»(<paramref name="lvR"/>) · 칸 아래 진행바(<paramref name="barR"/>) 를 놓는다(전부 프레임 % · 상자 안에서는 Within 으로 환산).
        /// 칸 = ItemFrame_01 조각(본래 190px · 배율로 표 크기에) + NormalArea 에 파란 등급 변형 + Item 에 GUI Pro 아이콘. Lv 글자·진행바는 칸의 자식(칸 밖으로 나가도 된다 · 이름표는 칸 자체를 잰다).
        /// </summary>
        static RectTransform PetCell(Transform parent, Layout.R parentR, Layout.R cellR, Layout.R lvR, Layout.R barR, int index, Action onClick, out RectTransform lvRt, out RectTransform barRt)
        {
            var cell = UiKit.Rect(parent, "Pet:" + index); UiKit.Pct(cell, cellR.Within(parentR));
            var frame = UiKit.Spawn("ui.itemFrame.empty", cell); frame.name = "ItemFrame_01"; var frt = (RectTransform)frame.transform;
            UiKit.FitScale(frt, UiKit.PxSize(cellR));
            UiKit.Hide(frt, "Text_Level", "Focus", "Disable", "Lock", "Add_1", "Add_2");
            var area = UiKit.Find(frt, "NormalArea");
            if (area != null) { UiKit.Clear(area); var f = UiKit.Spawn("ui.itemFrame.blue", area); UiKit.Stretch((RectTransform)f.transform); }
            GearUi.DarkFrame(frt, frt.localScale.x);   // T69-pet · 7항: 펫 칸 = 장비 화면의 그 프레임 → Border 링 Ink + 화면 8px(격자 0.89 · 세부 0.95 배율 보정 · 등급색은 파란 변형의 Bg·InnerBorder 가 그대로)
            var item = UiKit.Find(frt, "Item");
            if (item != null) { item.gameObject.SetActive(true); UiKit.SetSprite(frt, "Item", Icons[index % Icons.Length], Palette.White); }
            var lv = UiKit.Label(cell, 0, 0, 100, 100, "Lv. 0", 28, Palette.White); lv.name = "Lv"; lv.fontStyle = FontStyles.Bold; UiKit.Pct(lv.rectTransform, lvR.Within(cellR)); lvRt = lv.rectTransform;
            // 진행바 «n/m» 은 본문 40 — 표 높이(1.6/1.4%)엔 안 들어가므로 표 중심을 지켜 Layout.PetBarH 로 키운다(T63-pet · 게이트 잘림 0)
            var bar = UiKit.MakeBar(cell, "ui.sliderYellow"); bar.Root.name = "Bar"; UiKit.Pct(bar.Root, barR.WithH(Layout.PetBarH).Within(cellR)); bar.Set(0, "0/0"); barRt = bar.Root;
            if (onClick != null) UiKit.Clickable(cell, onClick);
            return cell;
        }

        /// <summary>합계·패시브 줄의 한 묶음 — «+0» 숫자(오른쪽 정렬) + 아이콘. 부모 % 로 x·w.</summary>
        static void SumGroup(Transform row, float x, float w, string icon, Color tint)
        {
            UiKit.Label(row, x, 0, w * 0.66f, 100, "+0", 40, Palette.White, TextAnchor.MiddleRight);
            var ic = UiKit.Icon(row, "Icon", icon, tint); UiKit.Pct(ic.rectTransform, x + w * 0.7f, 0, w * 0.3f, 100);
        }
        static void Sep(Transform row, float x) => UiKit.Label(row, x, 0, 6, 100, "|", 40, Palette.Cream);

        /// <summary>펫 시스템이 아직 없다는 안내 — 버튼을 눌렀을 때 뜨는 한 줄(T178 · 세부 팝업의 설명과 같은 말이라 한 곳에 둔다).</summary>
        public const string NotReadyMsg = "펫 시스템은 준비 중입니다";
        /// <summary>
        /// 주황 소환 버튼 — 위 줄 «소환»/«소환 x10». <b>가격 자리는 비운다</b>(펫 시스템·가격 데이터가 없다 · 레퍼런스 숫자를 베끼지 않는다 · §1).
        /// <para>
        /// T178(주인이 던전 20 에서 «준비 중» 카드를 지우라고 했다 = T101 ⓑ) — 여기에도 «💎 준비 중» 이 남아 있었다.
        /// 주인이 싫다고 한 표기를 다른 화면에 두지 않는다: <b>글자를 지우고</b> 버튼을 흐리게(<see cref="Dim"/> 0.5) 해서 «지금은 못 누른다» 를 보이고,
        /// 눌렀을 때 <b>토스트로 까닭</b>을 말한다. 이 레포에 이미 있는 문법이다(T99 던전 티켓 · 결정 205 · <c>EventsScreen.Dim</c>).
        /// 가격 숫자를 지어내지 않고, 새 기능도 만들지 않는다(결정 <b>432</b>).
        /// </para>
        /// </summary>
        RectTransform SummonButton(string name, string label, Layout.R r) => SummonButton(name, label, r, name == "Summon10Btn");

        /// <summary>
        /// 주황 소환 버튼 — 위 줄 «소환»/«소환 x10», 아래 줄 <b>지금 치를 값</b>(펫알 🥚 또는 다이아 💎).
        /// <para>
        /// T293 ⓘ — 표(<see cref="GameData.Pet"/>)가 실린 뒤로 <b>값을 지어내지 않고 표에서 읽는다</b>. «무엇으로 몇 번» 은 화면이 세지 않고
        /// <see cref="Pets.Offer"/> 하나가 답한다(T293 ⓓ 의 계약 — 두 버튼이 각자 세면 «소환은 펫알인데 x10 은 다이아» 같은 어긋남이 화면에서만 산다).
        /// </para>
        /// <para>표가 없으면(옛 껍데기) 종전 그대로 — 글자만, 흐리게, 누르면 «준비 중» 토스트(T178 · 결정 432).</para>
        /// </summary>
        RectTransform SummonButton(string name, string label, Layout.R r, bool ten)
        {
            var d = PD;
            if (d == null)
            {
                var shell = UiKit.Button(Root, "ui.btnOrange", label, () => App.Toast(NotReadyMsg), r); shell.name = name;
                var st = UiKit.ButtonText(shell); if (st != null) { UiKit.Pct(st.rectTransform, 4, 6, 92, 88); st.alignment = UiKit.TmpAlign(TextAnchor.MiddleCenter); }
                Dim(shell, false);
                return shell;
            }
            var b = UiKit.Button(Root, "ui.btnOrange", label, () => Pull(ten), r); b.name = name;
            var txt = UiKit.ButtonText(b); if (txt != null) { UiKit.Pct(txt.rectTransform, 4, 46, 92, 48); txt.alignment = UiKit.TmpAlign(TextAnchor.MiddleCenter); }
            // 값 줄 — 아이콘 + 숫자. 어느 쪽인지는 `Refresh` 가 세이브를 보고 다시 칠한다(태어날 때는 표의 다이아 값).
            var cost = UiKit.Rect(b, "Cost"); UiKit.Pct(cost, 4, 6, 92, 38);
            UiKit.Icon(cost, "Icon", "hud.gem", Palette.White);
            UiKit.Label(cost, 0, 0, 100, 100, "", 30, Palette.White, TextAnchor.MiddleCenter).name = "Qty";
            if (ten) _sum10 = b; else _sum1 = b;
            return b;
        }
        RectTransform _sum1, _sum10;

        /// <summary>펫 표(부팅이 든 것) — 없으면 종전 껍데기 그대로 돈다(T293 ⓗ 로더가 못 읽으면 null).</summary>
        PetData PD => App != null && App.Data != null ? App.Data.Pet : null;
        /// <summary>한 번에 쓸 수 있는 최대 개수 = 상자 «10회» 와 같은 표 값(코드에 10 을 안 박는다 · T293 ⓓ).</summary>
        int PullCap => App != null && App.Data != null && App.Data.Gacha != null ? App.Data.Gacha.TenPullCount : 10;

        /// <summary>
        /// 소환 한 번 — <see cref="Pets.Offer"/> 가 정한 값으로 <see cref="Pets.Draw"/> 가 치르고 뽑고 담는다(화면은 값을 다시 세지 않는다).
        /// <para>못 치르면 까닭을 토스트로 말한다 — 눌리는데 아무 일도 안 나는 자리를 안 만든다(결정 771).</para>
        /// </summary>
        void Pull(bool ten)
        {
            var d = PD; var s = App.Save; if (d == null || s == null) return;
            var offer = Pets.Offer(d, ten, s.PetEgg, PullCap);
            if (!Pets.CanDraw(s, offer))
            {
                App.Toast(offer.ByEgg ? "펫알이 모자랍니다" : "다이아가 모자랍니다");
                return;
            }
            // 뽑기 난수는 이 레포의 다른 뽑기 자리와 같은 꼴 — 시드 있는 Mulberry32(엔진 시드 골든과 아무 상관이 없다)
            var rng = new Mulberry32((uint)Environment.TickCount ^ 0x2545F491u);
            var got = Pets.Draw(d, s, rng, offer);
            if (got == null || got.Count == 0) return;
            Quests.Ach(App, Quests.AchPetGacha, got.Count);   // T258 이 자리를 기다리고 있었다 — 펫 뽑기 입구는 여기 하나뿐이다(두 번 세지 않게 다른 자리에 안 건다) · **누른 횟수가 아니라 뽑은 횟수**로 센다(x10 = 10)
            App.Persist();
            Refresh();
            // 결과는 보상 팝업 한 벌로 — 같은 펫이 여러 마리 나오면 한 칸에 개수로 모은다(상자 결과 창과 같은 읽는 법).
            var order = new List<string>(); var count = new Dictionary<string, int>();
            foreach (var p in got)
            {
                if (!count.ContainsKey(p.Id)) { count[p.Id] = 0; order.Add(p.Id); }
                count[p.Id]++;
            }
            var items = new List<RewardPopup.Item>();
            foreach (var id in order)
            {
                var p = d.Of(id); if (p == null) continue;
                items.Add(RewardPopup.Item.Of(PetIcon(d, id), p.Name + (count[id] > 1 ? " x" + count[id] : ""), amount: count[id]));
            }
            if (items.Count > 0) RewardPopup.Show(items, null);
        }

        /// <summary>펫 한 마리의 칸 그림 — 표의 차례가 곧 격자 차례라 그 자리의 아이콘을 쓴다(그림 9벌은 <see cref="PetLook"/> 이 따로 갖는다 · 격자 칸은 아이콘 문법).</summary>
        static string PetIcon(PetData d, string id)
        {
            int i = d != null ? d.Pets.FindIndex(p => p.Id == id) : -1;
            return Icons[(i < 0 ? 0 : i) % Icons.Length];
        }
        /// <summary>«눌리기는 하되 꺼져 보이는» 버튼(알파 0.5) — 까닭을 토스트로 알려야 해서 <see cref="UiKit.SetInteractable"/>(클릭까지 막는다) 대신 쓴다(<c>EventsScreen.Dim</c> 과 같은 문법 · T99 · 결정 205).</summary>
        static void Dim(RectTransform btn, bool on)
        {
            if (btn == null) return;
            UiKit.Ensure<CanvasGroup>(btn.gameObject).alpha = on ? 1f : 0.5f;
        }

        /// <summary>펫 세부 팝업(레퍼런스 14) — 공통 팝업 문법 위에: 명판 없음 · 펫 칸이 상자 윗변에 걸침 · 진행바 · 설명 박스 · «패시브:» + «+0 🗡 | +0 🛡» · 강화(회색) · 장착(주황) · «탭하여 닫기»(배경 탭). 버튼은 껍데기.</summary>
        public void OpenDetail(int index)
        {
            var box = App.Overlay.OpenBox("ui.popup", "ui.title.tangerine", "", Layout.PdBox, () => App.Overlay.Close());
            var ribbon = UiKit.Find(box, "ui.title.tangerine"); if (ribbon != null) ribbon.gameObject.SetActive(false);   // 레퍼런스 14 는 명판이 없다
            var lvR = new Layout.R(Layout.PdCell.X + Layout.PdCell.W * 0.15f, Layout.PdCell.Y - 0.9f, Layout.PdCell.W * 0.7f, 1.8f);
            var cell = PetCell(box, Layout.PdBox, Layout.PdCell, lvR, Layout.PdBar, index, null, out _, out var bar); cell.name = "PetDetailCell";
            // T203 ⓐ — 여기에는 빛살을 안 넣는다(주인 2026-09-07 «펫 세부 팝업에 보면 라이트 이펙트가 장비 슬롯 내부에 있던데 그거 없애기»).
            // 종전에는 T72 ② 로 «Item» 뒤에 UiKit.LightBehind 를 걸었고, 그 자리 주석이 «T190 범위 밖 — 주인이 «펫도» 라고 하면 뺀다» 라고 적어 두었다.
            // 주인이 그 «펫도» 를 말했으므로 뺀다. T190(아이템·보상 슬롯)과 지시가 하나로 모인 자리라 되살릴 일은 없을 것이나, 되돌리려면 이 자리에 LightBehind 한 줄.
            // T203 ⓑ — 이 팝업에는 무늬를 안 깐다(주인 «펫 세부 팝업에는 패턴 없애기»). 공통 팝업이 상자 «안» 에 까는 T72 ② 를 이 자리에서만 지운다.
            // 지우는 것은 Overlay.NoPattern 한 곳이다(T140 이 특전 화면에 쓴 그 함수 그대로 · 끄지 않고 지우는 까닭은 무늬 트윈이 SetLink 로 조각에 묶여 같이 죽기 때문).
            // 펫 «탭»(13)의 풀스크린 무늬(위 Build 의 UiKit.PatternBg)는 주인이 말한 자리가 아니므로 그대로 둔다.
            Overlay.NoPattern(box);
            var desc = UiKit.Panel(box, "Desc", "fr.r12", Palette.A(Palette.Dim, 0.6f)); UiKit.Pct(desc.rectTransform, Layout.PdDesc.Within(Layout.PdBox));
            UiKit.Label(desc.transform, 4, 8, 92, 84, "펫 시스템은 준비 중입니다.\n업데이트로 만나요.", 32, Palette.White);
            var pt = UiKit.Label(box, 0, 0, 100, 100, "패시브:", 34, Palette.Cream); pt.name = "PassiveTitle"; pt.fontStyle = FontStyles.Bold; UiKit.Pct(pt.rectTransform, Layout.PdPassiveTitle.Within(Layout.PdBox));
            var pv = UiKit.Rect(box, "PassiveRow"); UiKit.Pct(pv, Layout.PdPassive.Within(Layout.PdBox));
            SumGroup(pv, 0, 40, "pi.attack", Palette.White); Sep(pv, 47); SumGroup(pv, 60, 40, "pi.shield", Palette.Sky);
            var upB = UiKit.Button(box, "ui.btnGray", "강화", () => { }, Layout.PdBtnL.Within(Layout.PdBox)); upB.name = "PetUpgradeBtn";
            var eqB = UiKit.Button(box, "ui.btnOrange", "장착", () => { }, Layout.PdBtnR.Within(Layout.PdBox)); eqB.name = "PetEquipBtn";
            // 비평 이름표(ref-layout ⑪)
            UiKit.Tag(box, "팝업 박스"); UiKit.Tag(cell, "펫 칸(세부)"); UiKit.Tag(bar, "진행바(세부)"); UiKit.Tag(desc.transform, "설명 박스");
            UiKit.Tag(pt.transform, "패시브 제목"); UiKit.Tag(pv, "패시브 수치 줄"); UiKit.Tag(upB, "강화 버튼"); UiKit.Tag(eqB, "장착 버튼");
            var tc = UiKit.Find(App.Overlay.Root, "TapToClose"); if (tc != null) UiKit.Tag(tc, "닫기 안내");
        }

        public override void Refresh()
        {
            _top?.Refresh();
            NavBar.Refresh(App, Root);   // T167 — 탭 점도 같이 갱신(합성·NEW 가 사라지면 장비 탭 점이 꺼진다)
            RefreshSummon();
        }

        /// <summary>
        /// 소환 버튼 두 벌의 «지금 무엇으로 몇 번» 을 다시 칠한다 — 값도 아이콘도 <see cref="Pets.Offer"/> 하나에서 나온다.
        /// <para>⚑ 버튼이 스스로 세지 않는다: 펫알이 늘거나 줄면 두 버튼이 <b>같은 함수</b>로 같이 바뀐다(T293 ⓓ).</para>
        /// </summary>
        void RefreshSummon()
        {
            var d = PD; var s = App != null ? App.Save : null; if (d == null || s == null) return;
            Paint(_sum1, Pets.Offer(d, false, s.PetEgg, PullCap), "소환");
            Paint(_sum10, Pets.Offer(d, true, s.PetEgg, PullCap), "소환 x" + PullCap);
        }
        static void Paint(RectTransform btn, Pets.PullOffer o, string label)
        {
            if (btn == null) return;
            var t = UiKit.ButtonText(btn); if (t != null) t.text = TextGlyphs.Safe(o.ByEgg ? label + " (" + o.Count + "회)" : label);
            var cost = UiKit.Find(btn, "Cost"); if (cost == null) return;
            UiKit.SetSprite(cost, "Icon", o.ByEgg ? "pet.egg" : "hud.gem", Palette.White);
            var q = UiKit.Find(cost, "Qty"); var qt = q != null ? q.GetComponent<TMP_Text>() : null;
            if (qt != null) qt.text = UiKit.FmtComma(o.ByEgg ? o.Egg : o.Diamond);
        }
    }
}
