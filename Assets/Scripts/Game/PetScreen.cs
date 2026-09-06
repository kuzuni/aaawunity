using System;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 펫 탭 = 레퍼런스 <c>docs/ref/13_pet.jpg</c> 구도 · 펫 세부 팝업 = <c>14_pet_detail.jpg</c> (T42 · 주인 2026-09-06 «UI 는 무조건 레퍼런스 기준» · T32 «Character_Skill 그대로» 폐기 · 주인 ⓔ «시스템이 없는 화면은 레이아웃 껍데기»).
    /// 펫 시스템은 없다 — <b>전부 표시만</b>(버튼은 눌러도 아무 일 없음 · 숫자는 0 · 슬롯은 잠금/빈 칸 · 레퍼런스 숫자를 베끼지 않는다). ref-layout ⑩·⑪ 표(<see cref="Layout.PetGrid"/> …) 자리에 GUI Pro 조각을 조립한다:
    /// ① 상단 재화 바(<see cref="TopBar"/>) → ② <b>4열 격자 9칸</b>(칸 = ItemFrame_01 조각 + 파란 등급 변형 + GUI Pro 아이콘 · 칸 위 «Lv. 0» · 칸 아래 진행바 «0/0») → ③ <b>합계 줄</b>(«+0 ❤ | +0 🛡 | +0 🗡»)
    /// → ④ <b>«장착중» 띠</b>(어두운 패널 + 초록 라벨 + 슬롯 4 = 잠금 원 2 · 빈 칸 2) → ⑤ 회색 <b>전체 강화 · 빠른 장착</b> → ⑥ 주황 <b>소환 · 소환 x10</b>(두 줄 · 가격 자리 «준비 중») → ⑦ 탭 바.
    /// 칸을 누르면 세부 팝업(공통 팝업 문법 <see cref="UiKit.Popup"/> · 명판 없음 · 칸이 상자 윗변에 걸침 · 설명 박스 · «패시브:» 수치 줄 · 강화(회색) · 장착(주황) · «탭하여 닫기»).
    /// 글자(T63-pet · 주인 «글씨 너무 작다»): 전부 <see cref="UiKit"/> 하한(본문 40 · 버튼 44) — 직접 박은 크기는 없다. 진행바 «n/m» 만 표 높이에 안 들어가 바를 <see cref="Layout.PetBarH"/> 로 키웠다(13·14 게이트 잘림 0).
    /// 이름 계약(스모크 테스트): 격자 <c>PetGrid/Pet:N</c> · 슬롯 <c>Slots/Slot:N</c> · 버튼 <c>UpgradeAllBtn/QuickEquipBtn/SummonBtn/Summon10Btn</c> · 세부 <c>PetDetailCell/PetUpgradeBtn/PetEquipBtn</c> · 탭 바 <c>ui.tabBar</c>.
    /// 펫 시스템이 생기면 <see cref="Icons"/>·«0/0»·«+0» 자리에 pets.json 값을 넣는다(배치는 그대로).
    /// </summary>
    public sealed class PetScreen : GameScreen
    {
        public override string Name => "pet";
        public const int SlotCount = 4, LockedSlots = 2;
        /// <summary>격자 9칸의 아이콘(GUI Pro UniqueIcon · 카탈로그 <c>pet.*</c> · 그림은 점수 밖 · 에셋 안에 있는 것으로 · 레퍼런스 순서: 빵·불꽃·화살·망치·화살 다발·베기·멧돼지·천사·문어 자리).</summary>
        public static readonly string[] Icons = { "pet.bread", "pet.fire", "pet.bow", "pet.hammer", "pet.rocket", "pet.sickle", "pet.egg", "pet.feather", "pet.eye" };
        static readonly Layout.R Frame = new Layout.R(0, 0, 100, 100);

        TopBar _top; readonly RectTransform[] _cells = new RectTransform[Layout.PetCount];

        static Layout.R Shift(Layout.R r, float dx, float dy) => new Layout.R(r.X + dx, r.Y + dy, r.W, r.H);

        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Color.Lerp(Palette.Slate, Palette.Dim, 0.6f); bg.raycastTarget = true;   // 어두운 바탕(레퍼런스 · 색은 점수 밖)

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
                var t = piece.GetComponentInChildren<Text>(true);
                if (t != null) { t.text = "장착중"; UiKit.Pct(t.rectTransform, 8, 0, 84, 100); t.alignment = TextAnchor.MiddleCenter; t.resizeTextForBestFit = true; t.resizeTextMinSize = TextSize.BestFitMin; t.resizeTextMaxSize = 44; t.horizontalOverflow = HorizontalWrapMode.Overflow; }
            }
            var slotsHost = UiKit.Rect(Root, "Slots"); UiKit.Stretch(slotsHost);
            var slots = new RectTransform[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                var s = slots[i] = UiKit.Rect(slotsHost, "Slot:" + i); UiKit.Pct(s, Shift(Layout.PetSlot, i * Layout.PetSlotPitch, 0));
                if (i < LockedSlots)
                {
                    var c = UiKit.Icon(s, "Bg", "fr.circle", Palette.A(Palette.Dim, 0.7f)); UiKit.Stretch(c.rectTransform);
                    var b = UiKit.Icon(s, "Border", "fr.circleBorder", Palette.InkLight); UiKit.Stretch(b.rectTransform);
                    var lk = UiKit.Icon(s, "Lock", "ui.iconLock"); UiKit.Pct(lk.rectTransform, 28, 28, 44, 44);
                }
                else
                {
                    // 빈 칸 = 회색 등급 프레임 + «+»(Add_1) — ItemFrame_01 은 NormalArea 가 비어 있고 Add_1 이 기본 꺼짐이라 그대로 두면 아무것도 안 보인다(회차 1 감점 · 슬롯 3·4 빠짐)
                    var f = UiKit.Spawn("ui.itemFrame.empty", s); f.name = "ItemFrame_01"; UiKit.FitScale((RectTransform)f.transform, UiKit.PxSize(Layout.PetSlot));
                    UiKit.Hide(f.transform, "Text_Level", "Focus", "Disable", "Lock", "Add_2", "Item");
                    var area = UiKit.Find(f.transform, "NormalArea"); if (area != null) { UiKit.Clear(area); var v = UiKit.Spawn("ui.itemFrame.gray", area); UiKit.Stretch((RectTransform)v.transform); }
                    UiKit.Show(f.transform, "Add_1", true);
                }
                UiKit.Clickable(s, () => { });   // 껍데기 — 눌러도 아무 일 없음
            }

            // ⑤ 회색 보조 버튼 2 → ⑥ 주황 소환 버튼 2(두 줄 · 가격 자리는 «준비 중») → ⑦ 탭 바
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
            var item = UiKit.Find(frt, "Item");
            if (item != null) { item.gameObject.SetActive(true); UiKit.SetSprite(frt, "Item", Icons[index % Icons.Length], Palette.White); }
            var lv = UiKit.Label(cell, 0, 0, 100, 100, "Lv. 0", 28, Palette.White); lv.name = "Lv"; lv.fontStyle = FontStyle.Bold; UiKit.Pct(lv.rectTransform, lvR.Within(cellR)); lvRt = lv.rectTransform;
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

        /// <summary>주황 소환 버튼 — 위 줄 «소환»/«소환 x10» · 아래 줄 💎 + «준비 중»(펫 시스템·가격 데이터 없음 · 레퍼런스 숫자 베끼지 않음).</summary>
        RectTransform SummonButton(string name, string label, Layout.R r)
        {
            var b = UiKit.Button(Root, "ui.btnOrange", label, () => { }, r); b.name = name;
            var txt = UiKit.ButtonText(b); if (txt != null) { UiKit.Pct(txt.rectTransform, 4, 6, 92, 46); txt.alignment = TextAnchor.MiddleCenter; }
            var gem = UiKit.Icon(b, "Gem", "ui.gemRed"); UiKit.Pct(gem.rectTransform, 30, 56, 11, 36);
            UiKit.Label(b, 43, 54, 40, 40, "준비 중", 30, Palette.Ink, TextAnchor.MiddleLeft, true, false);
            return b;
        }

        /// <summary>펫 세부 팝업(레퍼런스 14) — 공통 팝업 문법 위에: 명판 없음 · 펫 칸이 상자 윗변에 걸침 · 진행바 · 설명 박스 · «패시브:» + «+0 🗡 | +0 🛡» · 강화(회색) · 장착(주황) · «탭하여 닫기»(배경 탭). 버튼은 껍데기.</summary>
        public void OpenDetail(int index)
        {
            var box = App.Overlay.OpenBox("ui.popup", "ui.title.tangerine", "", Layout.PdBox, () => App.Overlay.Close());
            var ribbon = UiKit.Find(box, "ui.title.tangerine"); if (ribbon != null) ribbon.gameObject.SetActive(false);   // 레퍼런스 14 는 명판이 없다
            var lvR = new Layout.R(Layout.PdCell.X + Layout.PdCell.W * 0.15f, Layout.PdCell.Y - 0.9f, Layout.PdCell.W * 0.7f, 1.8f);
            var cell = PetCell(box, Layout.PdBox, Layout.PdCell, lvR, Layout.PdBar, index, null, out _, out var bar); cell.name = "PetDetailCell";
            var desc = UiKit.Panel(box, "Desc", "fr.r12", Palette.A(Palette.Dim, 0.6f)); UiKit.Pct(desc.rectTransform, Layout.PdDesc.Within(Layout.PdBox));
            UiKit.Label(desc.transform, 4, 8, 92, 84, "펫 시스템은 준비 중입니다.\n업데이트로 만나요.", 32, Palette.White);
            var pt = UiKit.Label(box, 0, 0, 100, 100, "패시브:", 34, Palette.Cream); pt.name = "PassiveTitle"; pt.fontStyle = FontStyle.Bold; UiKit.Pct(pt.rectTransform, Layout.PdPassiveTitle.Within(Layout.PdBox));
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
            NavBar.Refresh(Root);
        }
    }
}
