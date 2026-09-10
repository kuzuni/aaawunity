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
    /// ⚑ <b>이 머리글은 «껍데기» 시절의 것이었다(T293 이 닫히며 고쳤다).</b> 지금 이 화면은 <b>다 산다</b> — 소환이 치르고 뽑고 담고(<see cref="Pets.Draw"/>),
    /// 강화·장착이 세이브를 바꾸고, 숫자는 전부 표·세이브에서 온다(<see cref="Pets.EquipPower"/>·<see cref="Pets.TotalPower"/>). ref-layout ⑩·⑪ 표(<see cref="Layout.PetGrid"/> …) 자리에 GUI Pro 조각을 조립한다:
    /// ① 상단 재화 바(<see cref="TopBar"/>) → ② <b>4열 격자 9칸</b>(칸 = ItemFrame_01 조각 + 파란 등급 변형 + GUI Pro 아이콘 · 칸 위 «Lv. 0» · 칸 아래 진행바 «0/0») → ③ <b>합계 줄</b>(«+0 ❤ | +0 🛡 | +0 🗡»)
    /// → ④ <b>«장착중» 띠</b>(어두운 패널 + 초록 라벨 + 슬롯 3 = 열린 빈 칸 1 · 잠금 원 2 · 잠긴 칸은 «N회») → ⑤ 주황 <b>전체 강화 · 빠른 장착</b>(할 것이 있으면 빨간 점 · T380) → ⑥ 주황 <b>소환 · 소환 x10</b>(윗줄 «N회» · 아랫줄 값 줄 🥚/💎 — 무엇으로 몇 번인지는 <see cref="Pets.Offer"/> 하나가 답한다 · T380) → ⑦ 탭 바.
    /// 칸을 누르면 세부 팝업(공통 팝업 문법 <see cref="UiKit.Popup"/> · 명판 없음 · 칸이 상자 윗변에 걸침 · 설명 박스 · «패시브:» 수치 줄 · 강화(회색) · 장착(주황) · «탭하여 닫기»).
    /// 글자(T63-pet · 주인 «글씨 너무 작다»): 전부 <see cref="UiKit"/> 하한(본문 40 · 버튼 44) — 직접 박은 크기는 없다. 진행바 «n/m» 만 표 높이에 안 들어가 바를 <see cref="Layout.PetBarH"/> 로 키웠다(13·14 게이트 잘림 0).
    /// 테두리(T69-pet · 주인 «행·카드·칸마다 검은 아웃라인» + 7항 «아이템류 칸 = 장비 화면의 그 프레임»): 격자 9칸·빈 장착 슬롯·세부 칸의 ItemFrame Border 링을 <see cref="GearUi.DarkFrame"/> 로 Ink 8px · 잠금 슬롯(원)은 <see cref="CircleBorderKey"/> 굵은 원형 조각.
    /// 합계 줄(«+0 ❤ | +0 🛡 | +0 🗡»)은 레퍼런스 13 에 상자가 없는 맨 글자라 <see cref="BorderAudit.Exempt"/>(결정 171). 13·14 는 BorderAudit strict.
    /// 이름 계약(스모크 테스트): 격자 <c>PetGrid/Pet:N</c> · 슬롯 <c>Slots/Slot:N</c> · 버튼 <c>UpgradeAllBtn/QuickEquipBtn/SummonBtn/Summon10Btn</c> · 세부 <c>PetDetailCell/PetUpgradeBtn/PetEquipBtn</c> · 탭 바 <c>ui.tabBar</c>.
    /// 값은 <b>전부 표(<c>pet.json</c>)와 세이브</b>에서 온다 — 화면이 수를 다시 적는 자리는 없다(격자 «Lv. N»·진행바 «조각/필요»·합계 줄·패시브 셋·소환 값 줄 모두).
    /// </summary>
    public sealed class PetScreen : GameScreen
    {
        public override string Name => "pet";
        /// <summary>장착 칸 수 — <b>주인 지시 3</b>(T293 «장착 최대 3개 · 처음 1개 · 100회·200회 해금»).
        /// ⚠ 레퍼런스 <c>13_pet.jpg</c> 는 <b>4칸</b>이다(옛 HTML 판) — <b>주인 지시가 이긴다</b>. 표 ⑩ 도 3칸으로 다시 적었고 그 까닭을 그 자리에 남겼다.</summary>
        public const int SlotCount = Layout.PetSlotCount;
        /// <summary>새 세이브에서 <b>잠겨 있는</b> 칸 수(뽑기 0회 = 첫 칸만 열린다) — 자가 «잠금 원» 을 찾을 때 쓰는 값이고, 잠김 여부 자체는 세이브가 정한다(<see cref="Pets.SlotsOpen(PetData, SaveData)"/>).</summary>
        public const int LockedSlots = SlotCount - 1;
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

            // T293 5항 ⓙ — 0마리일 때의 한 줄(그 자리에 칸이 하나도 안 켜지므로 «무엇을 하면 되는지» 를 말한다)
            {
                // 자리 = **격자 한가운데**(`PetGrid` 의 세로 중앙) — 종전에는 첫 줄 언저리라 텅 빈 격자 위쪽에 홀로 떠서 «고장난 화면» 처럼 보였다(런 971 screens 13 눈 확인).
                var hint = UiKit.Label(grid, Layout.PetCell.X, Layout.PetGrid.Y + Layout.PetGrid.H * 0.5f - 3f, 100 - Layout.PetCell.X * 2, 6,
                                       "펫알로 소환해 보세요", 36, Palette.Cream, TextAnchor.MiddleCenter);
                hint.name = "EmptyHint"; _emptyHint = hint.rectTransform; _emptyHint.gameObject.SetActive(false);
            }

            // ③ 합계 줄 — «+0 ❤ | +0 🛡 | +0 🗡»(펫 시스템 없음 → 0)
            var sum = UiKit.Rect(Root, "SumRow"); UiKit.Pct(sum, Layout.PetSum);
            SumGroup(sum, 0, 26, "pi.heart", Palette.Red); Sep(sum, 30); SumGroup(sum, 38, 26, "pi.shield", Palette.Sky); Sep(sum, 66); SumGroup(sum, 74, 26, "pi.attack", Palette.White);

            // ④ «장착중» 띠 — 어두운 패널 + 초록 꼬리 라벨(조각을 표 칸에 배율로) + 슬롯 3(열린 칸 1 · 잠금 원 2 · 어느 쪽인지는 세이브가 정한다 · RefreshSlots)
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
            var slots = _slots;
            for (int i = 0; i < SlotCount; i++)
            {
                var s = slots[i] = UiKit.Rect(slotsHost, "Slot:" + i); UiKit.Pct(s, Shift(Layout.PetSlot, i * Layout.PetSlotPitch, 0));
                // T293 5항 ⓘ — 칸 하나에 **두 벌**을 세우고 세이브가 고른다: 잠금 원(«Lock» 묶음) ↔ 물건 칸(«Frame» 묶음).
                //   태어날 때 둘 다 세워 두는 까닭 = `Refresh` 가 여러 번 불리므로 그때마다 조각을 새로 spawn 하면 쌓인다(결정 1027 ③ 과 같은 자리).
                var lockPart = UiKit.Rect(s, "LockPart"); UiKit.Stretch(lockPart);
                {
                    var c = UiKit.Icon(lockPart, "Bg", "fr.circle", Palette.A(Palette.Dim, 0.7f)); UiKit.Stretch(c.rectTransform);
                    // T69-pet «검은 아웃라인»: 원형 조각은 9-slice 가 없어 multiplier 로 못 굵힌다 → 굵은 조각(Border2 · 선 9.8%)을 슬롯보다 CircleBorderOut px 크게 = 선 ≥ 8px(폰 3px) · Ink α0.9
                    var b = UiKit.Icon(lockPart, UiKit.BorderName, CircleBorderKey, UiKit.BorderInk); UiKit.Stretch(b.rectTransform, -CircleBorderOut, -CircleBorderOut, -CircleBorderOut, -CircleBorderOut);
                    var lk = UiKit.Icon(lockPart, "Lock", "ui.iconLock"); UiKit.Pct(lk.rectTransform, 28, 28, 44, 44);
                    // 잠긴 칸 밑의 «N회» — 수는 표(`slotUnlockPulls`)에서 오고 `Refresh` 가 쓴다(주인 5항).
                    // ⛑ 런 951 빨강(T63 하한 · `TextSizeGateTests`·`UiSmokeTests`) — 28 은 하한 밑이었고 «뽑기 100회» 는 칸에서 잘렸다.
                    //   ⓐ 종류를 바로 적는다: 이것은 본문이 아니라 **보조 라벨**(«남은 횟수» 류)이라 하한 36(`TextSize.Aux`)이다.
                    //   ⓑ 글자를 «100회» 로 줄였다 — 하한 36 에서 «뽑기 100회» 한 줄은 약 197px 이라 **칸 사이(피치 128px)를 넘어 옆 칸 글자와 겹친다**.
                    //     두 줄로 접으면 띠 아래로 2.7%p 흘러 «전체 강화» 줄까지 내려간다. 완전한 문장은 **누르면 토스트**가 말한다(«뽑기 N회에 열립니다»).
                    //   ⓒ 접힘 금지(NoWrap) — 접히면 높이가 두 배가 되어 다시 잘린다.
                    var need = UiKit.Label(lockPart, -50, 100, 200, 80, "", TextSize.Aux, Palette.Cream, TextAnchor.MiddleCenter); need.name = "LockText";
                    need.textWrappingMode = TextWrappingModes.NoWrap;
                }
                var framePart = UiKit.Rect(s, "FramePart"); UiKit.Stretch(framePart);
                {
                    // 빈 칸 = 회색 등급 프레임 + «+»(Add_1) — ItemFrame_01 은 NormalArea 가 비어 있고 Add_1 이 기본 꺼짐이라 그대로 두면 아무것도 안 보인다(회차 1 감점 · 슬롯 3·4 빠짐)
                    var f = UiKit.Spawn("ui.itemFrame.empty", framePart); f.name = "ItemFrame_01"; var frt = (RectTransform)f.transform; UiKit.FitScale(frt, UiKit.PxSize(Layout.PetSlot));
                    UiKit.Hide(f.transform, "Text_Level", "Focus", "Disable", "Lock", "Add_2", "Item");
                    var area = UiKit.Find(f.transform, "NormalArea"); if (area != null) { UiKit.Clear(area); var v = UiKit.Spawn("ui.itemFrame.gray", area); UiKit.Stretch((RectTransform)v.transform); }
                    UiKit.Show(f.transform, "Add_1", true);
                    GearUi.DarkFrame(frt, frt.localScale.x);   // T69-pet · 7항: 빈 장착 슬롯도 «물건 칸» = ItemFrame 의 Border 링을 Ink 로(조각 배율 0.41 만큼 더 굵게 · 결정 171)
                }
                int si = i;
                UiKit.Clickable(s, () => TapSlot(si));   // T293 5항 — 낀 칸을 누르면 그 펫의 세부 팝업(빈 칸·잠긴 칸은 까닭을 토스트)
            }

            // ⑤ 주황 보조 버튼 2 → ⑥ 주황 소환 버튼 2(가격 자리 없음 · 흐리게 + 누르면 «준비 중» 토스트 · T178) → ⑦ 탭 바
            // T380(주인 2026-09-10 «펫꺼 전체강화·빠른장착 둘다 주황으로 · 할 거리 있으면 빨간점») — 옷은 **늘 주황**이고 «지금 할 것이 있나» 는
            //   **빨간 점**이 말한다(옷을 두 벌 겹치는 T306 꼴을 안 쓴다 — 주인이 색을 고정했으므로 갈아입힐 것이 없다). **누르는 것은 늘 된다**:
            //   없으면 까닭을 토스트로 말한다(특권 «전체 받기» 가 세운 그 문법 · T306). 점이 꺼진 채 아무 말도 안 하는 버튼을 안 만든다.
            var up = UiKit.Button(Root, "ui.btnOrange", "전체 강화", UpgradeAll, Layout.PetUpgradeAll); up.name = "UpgradeAllBtn"; _upAll = up;
            var qe = UiKit.Button(Root, "ui.btnOrange", "빠른 장착", QuickEquip, Layout.PetQuickEquip); qe.name = "QuickEquipBtn"; _quickEq = qe;
            _upAllDot = HelperDot(up, UpgradeDotName); _quickEqDot = HelperDot(qe, QuickEquipDotName);
            var sm = SummonButton("SummonBtn", CountLabel(1), Layout.PetSummon); var sm10 = SummonButton("Summon10Btn", CountLabel(PullCap), Layout.PetSummon10);
            NavBar.Attach(this, Root, "pet");

            // 비평 이름표(T46 · ref-layout ⑩ 의 «요소» 이름 그대로)
            UiKit.Tag(_top.Root, "상단 바");
            UiKit.TagGroup(grid, "펫 격자(9칸)", _cells); UiKit.Tag(_cells[0], "펫 칸(1칸)"); UiKit.Tag(lv0, "펫 Lv 라벨(1칸)"); UiKit.Tag(bar0, "펫 진행바(1칸)");
            UiKit.Tag(sum, "합계 줄"); UiKit.Tag(brt, "장착 띠"); UiKit.Tag(eqLabel, "«장착중» 라벨");
            UiKit.TagGroup(slotsHost, "장착 슬롯 줄(3칸)", slots); UiKit.Tag(slots[0], "장착 슬롯 1칸");
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
        /// 주황 소환 버튼 — <b>상점 신화 상자 큰 카드의 가격 버튼과 한 꼴</b>(T380 · 주인 2026-09-10 «펫부분도 뽑기 버튼 내부 디자인 신화상자 버튼처럼 · 1회 / 다이아 아이콘 100»):
        /// 위 줄 «<b>N회</b>»(지금 몇 번 뽑나 · 펫알이면 그 수) / 아래 줄 [아이콘][값] 한 줄(<see cref="UiKit.PriceRow"/> · 상점과 같은 함수라 간격·크기가 같다).
        /// <para>
        /// T293 ⓘ — 표(<see cref="GameData.Pet"/>)가 실린 뒤로 <b>값을 지어내지 않고 표에서 읽는다</b>. «무엇으로 몇 번» 은 화면이 세지 않고
        /// <see cref="Pets.Offer"/> 하나가 답한다(T293 ⓓ 의 계약 — 두 버튼이 각자 세면 «소환은 펫알인데 x10 은 다이아» 같은 어긋남이 화면에서만 산다).
        /// 윗줄의 «N회» 도 그 답의 <c>Count</c> 다 — 상점 열쇠 버튼이 «쓸 개수»회 로 움직이는 것과 같은 까닭(«1회» 라 적고 3 회가 나가면 거짓말이다 · T255).
        /// </para>
        /// <para>이름 계약(자): 윗줄 <c>Label</c> · 값 줄 <c>Cost</c> 안 <c>Icon</c>·<c>Qty</c>. 프리팹 버튼 자체의 글자는 비워 숨긴다(상점과 같다).</para>
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
            var b = UiKit.Button(Root, "ui.btnOrange", "", () => Pull(ten), r); b.name = name;
            var own = UiKit.ButtonText(b); if (own != null) own.gameObject.SetActive(false);
            // 위 «N회» / 아래 [아이콘][값] — 상점 `ShopScreen.PriceButton(twoLine)` 의 자리·크기 그대로(0~50 / 50~94). 글자·그림은 `Refresh` 가 세이브를 보고 다시 칠한다.
            var top = UiKit.Label(b, 0, 6, 100, 44, label, TextSize.Button, Palette.White, TextAnchor.MiddleCenter, false, true, TextKind.Button); top.name = "Label";
            UiKit.PriceRow(b, new Layout.R(0, 50, 100, 44), "", null, "hud.gem", "Icon", TextSize.Button, TextKind.Button, UiKit.PriceIconSize, "Cost", "Qty");
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

        RectTransform _upAll, _quickEq;
        GameObject _upAllDot, _quickEqDot;

        /// <summary>보조 버튼 둘의 빨간 점 이름(자가 이 이름으로 짚는다 · T380).</summary>
        public const string UpgradeDotName = "UpgradeDot", QuickEquipDotName = "QuickEquipDot";
        /// <summary>점 지름·자리(버튼 오른쪽 위 모서리 안쪽) — 특권 카드 «받기» 버튼의 점(<see cref="PrivilegeScreen.CardDotSize"/>)과 같은 눈금이다(버튼 높이가 비슷하다).</summary>
        public const float HelperDotSize = 34f;
        public static readonly Vector2 HelperDotAnchor = new Vector2(1, 1), HelperDotOffset = new Vector2(-10f, -6f);

        /// <summary>보조 버튼 하나에 점을 달아 둔다(태어날 때는 꺼짐 · 켜고 끄는 것은 <see cref="RefreshHelpers"/> 하나뿐이다 · T366 카드 점과 같은 문법).</summary>
        static GameObject HelperDot(RectTransform btn, string name)
        {
            if (btn == null) return null;
            var dot = UiKit.AlertDot(btn, name, HelperDotAnchor, HelperDotOffset, HelperDotSize);
            dot.SetActive(false);
            return dot;
        }

        /// <summary>지금 강화할 수 있는 펫이 몇 마리인가 — 버튼 글자와 «누르면 무슨 일이 나나» 가 같은 값에서 나온다.</summary>
        int UpgradableCount()
        {
            var d = PD; var s = App != null ? App.Save : null; if (d == null || s == null) return 0;
            int n = 0;
            foreach (var p in d.Pets) if (Pets.CanLevelUp(d, s, p.Id)) n++;
            return n;
        }
        /// <summary>빠른 장착이 지금 채울 수 있는 칸 수 = min(열린 빈 칸, 아직 안 낀 가진 펫).</summary>
        int QuickEquipCount()
        {
            var d = PD; var s = App != null ? App.Save : null; if (d == null || s == null) return 0;
            int open = Pets.SlotsOpen(d, s); if (open <= 0) return 0;
            int empty = 0;
            for (int i = 0; i < open; i++) if (string.IsNullOrEmpty(Pets.EquippedAt(d, s, i))) empty++;
            if (empty <= 0) return 0;
            int spare = 0;
            foreach (var p in d.Pets) if (Pets.Has(s, p.Id) && WornSlot(p.Id) < 0) spare++;
            return spare < empty ? spare : empty;
        }

        /// <summary>
        /// «전체 강화» — 올릴 수 있는 펫을 <b>더 못 올릴 때까지</b> 전부 올린다(주인 5항 «올릴 수 있는 동료 전부»).
        /// <para>규칙은 <see cref="Pets.LevelUp"/> 한 곳이고 여기는 <b>몇 번 부를지</b>만 정한다. 퀘스트 카운터는 <b>올린 횟수만큼</b> 오른다(세부 팝업의 «강화» 와 같은 수 · T257 ⓑ).</para>
        /// </summary>
        void UpgradeAll()
        {
            var d = PD; var s = App.Save; if (d == null || s == null) { App.Toast(NotReadyMsg); return; }
            int ups = 0;
            foreach (var p in d.Pets)
                while (Pets.LevelUp(d, s, p.Id)) ups++;
            if (ups <= 0) { App.Toast("강화할 펫이 없습니다"); return; }
            Quests.Bump(App, Quests.PetUpgrade, ups);
            App.Persist(); Refresh();
            App.Toast(ups + "번 강화했습니다");
        }

        /// <summary>
        /// «빠른 장착» — 열린 <b>빈 칸</b>에 «등급 높은 순 → 레벨 높은 순 → 표 차례» 로 채운다(주인 5항 «열린 슬롯에 등급·레벨 높은 순»).
        /// <para>이미 낀 펫은 건드리지 않는다 — «빠른» 은 «다시 짜기» 가 아니라 «빈 자리 채우기» 다(누른 사람이 고른 것을 안 뒤집는다).</para>
        /// </summary>
        void QuickEquip()
        {
            var d = PD; var s = App.Save; if (d == null || s == null) { App.Toast(NotReadyMsg); return; }
            int open = Pets.SlotsOpen(d, s);
            if (open <= 0) { App.Toast("장착 칸이 아직 안 열렸습니다"); return; }
            var pool = new List<PetData.Pet>();
            foreach (var p in d.Pets) if (Pets.Has(s, p.Id) && WornSlot(p.Id) < 0) pool.Add(p);
            pool.Sort((a, b) =>
            {
                var ga = d.GradeOfPet(a); var gb = d.GradeOfPet(b);
                int ra = ga != null ? ga.Rar : -1, rb = gb != null ? gb.Rar : -1;
                if (ra != rb) return rb.CompareTo(ra);                       // 등급 내림차순
                int la = Pets.Lv(s, a.Id), lb = Pets.Lv(s, b.Id);
                if (la != lb) return lb.CompareTo(la);                       // 그 안에서 레벨 내림차순
                return d.Pets.IndexOf(a).CompareTo(d.Pets.IndexOf(b));       // 마지막은 표 차례(같은 값이면 늘 같은 답)
            });
            int put = 0;
            for (int i = 0; i < open && put < pool.Count; i++)
            {
                if (!string.IsNullOrEmpty(Pets.EquippedAt(d, s, i))) continue;
                if (Pets.Equip(d, s, pool[put].Id, i)) put++;
            }
            if (put <= 0) { App.Toast("장착할 펫이 없습니다"); return; }
            App.Persist(); Refresh();
            App.Toast(put + "마리 장착했습니다");
        }

        /// <summary>그 펫이 낀 칸(없으면 -1) — «장착 ↔ 해제» 를 한 버튼으로 쓰려면 이것 하나면 된다.</summary>
        int WornSlot(string id)
        {
            var d = PD; var s = App != null ? App.Save : null; if (d == null || s == null) return -1;
            for (int i = 0; i < Pets.SlotsOpen(d, s); i++) if (Pets.EquippedAt(d, s, i) == id) return i;
            return -1;
        }

        /// <summary>
        /// 강화 한 번 — 조각을 <see cref="Pets.Need"/> 만큼 쓰고 Lv +1(규칙은 Core 가 갖는다 · 화면은 부르기만).
        /// <para>⚑ 여기가 <b>퀘스트·업적이 기다리던 «펫 강화» 카운터의 유일한 자리</b>다(T257 ⓑ · 광고·다른 입구에 겹쳐 걸지 않는다).</para>
        /// </summary>
        void Upgrade(string id, int index)
        {
            var d = PD; var s = App.Save; if (d == null || s == null) return;
            if (!Pets.LevelUp(d, s, id)) { App.Toast("조각이 모자랍니다"); return; }
            Quests.Bump(App, Quests.PetUpgrade);
            App.Persist(); Refresh();
            App.Overlay.Close(); OpenDetail(index);   // 팝업을 다시 열어 새 레벨·새 목표를 그대로 보여 준다(업적 «받기» 와 같은 길)
        }

        /// <summary>장착 ↔ 해제 — 낀 칸이 있으면 비우고, 없으면 <b>빈 칸부터</b> 채운다(빈 칸이 없으면 첫 칸을 바꿔 낀다).</summary>
        void ToggleEquip(string id, int index)
        {
            var d = PD; var s = App.Save; if (d == null || s == null) return;
            int worn = WornSlot(id);
            if (worn >= 0) Pets.Unequip(s, worn);
            else
            {
                int open = Pets.SlotsOpen(d, s);
                if (open <= 0) { App.Toast("장착 칸이 아직 안 열렸습니다"); return; }
                int slot = 0;
                for (int i = 0; i < open; i++) if (string.IsNullOrEmpty(Pets.EquippedAt(d, s, i))) { slot = i; break; }
                if (!Pets.Equip(d, s, id, slot)) return;
            }
            App.Persist(); Refresh();
            App.Overlay.Close(); OpenDetail(index);
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
            // T293 ⓘ — 표가 실렸으면 «그 펫이 무엇을 하는가» 를 표에서 조립한 글자로 말한다(사람이 따로 안 적는다 · `Pets.Effect`).
            //   표가 없으면(옛 껍데기) 종전 안내 그대로 — 그때는 지어낼 값이 없는 것이 사실이다.
            // 칸 번호는 «화면의 몇 번째» 다 — 무엇이 앉아 있는지는 격자를 채운 그 목록(`_cellPet`)이 안다(T293 5항 ⓙ).
            //   목록이 아직 없으면(껍데기·표 없음) 종전처럼 표 차례로 읽는다.
            var dp = PD;
            var pet = _cellPet != null && index < _cellPet.Count ? _cellPet[index]
                    : dp != null && index < dp.Pets.Count ? dp.Pets[index] : null;
            int petLv = pet != null ? Pets.Lv(App.Save, pet.Id) : 0;
            string descText = pet == null ? "펫 시스템은 준비 중입니다.\n업데이트로 만나요."
                            : petLv >= 1 ? pet.Name + " · Lv " + petLv + "\n" + Pets.Effect(dp, pet)
                                         : pet.Name + " · 아직 없다\n" + Pets.Effect(dp, pet) + "\n소환으로 얻을 수 있습니다.";
            UiKit.Label(desc.transform, 4, 8, 92, 84, descText, 32, Palette.White);
            var pt = UiKit.Label(box, 0, 0, 100, 100, "패시브:", 34, Palette.Cream); pt.name = "PassiveTitle"; pt.fontStyle = FontStyles.Bold; UiKit.Pct(pt.rectTransform, Layout.PdPassiveTitle.Within(Layout.PdBox));
            var pv = UiKit.Rect(box, "PassiveRow"); UiKit.Pct(pv, Layout.PdPassive.Within(Layout.PdBox));
            // 셋을 13 의 합계 줄과 **같은 차례·같은 자리 규칙**으로 세운다(❤ · 🛡 · 🗡) — 한 화면에서 두 차례를 배우게 하지 않는다(격자 차례 = 빠른 장착 차례와 같은 까닭 · 결정 1064).
            //   ⚑ 레퍼런스 14 는 둘(🗡·🛡)뿐이다 — 옛 HTML 판의 펫은 공·실만 줬고, **주인은 «공·체·실» 을 준다고 했다**(슬롯 4 → 3 과 같은 자리: 그림이 아니라 지시를 따른다).
            SumGroup(pv, 0, 26, "pi.heart", Palette.Red); Sep(pv, 30); SumGroup(pv, 38, 26, "pi.shield", Palette.Sky); Sep(pv, 66); SumGroup(pv, 74, 26, "pi.attack", Palette.White);
            // T293 ⓘ — 세부 칸의 «Lv. N» · 진행바(조각/필요) · 패시브 수치를 세이브에서 칠한다(값은 전부 Core 가 낸다).
            if (pet != null)
            {
                int need = Pets.Need(dp, petLv < 1 ? 1 : petLv), frag = Pets.Frag(App.Save, pet.Id);
                var lvT = UiKit.Find(cell, "Lv"); var lvTx = lvT != null ? lvT.GetComponent<TMP_Text>() : null;
                if (lvTx != null) lvTx.text = "Lv. " + petLv;
                var sl = bar != null ? bar.GetComponentInChildren<Slider>(true) : null;
                if (sl != null) sl.value = need > 0 ? Mathf.Clamp01((float)frag / need) : 0f;
                var bt = bar != null ? bar.GetComponentInChildren<TMP_Text>(true) : null;
                if (bt != null) bt.text = frag + "/" + need;   // «Lv N → N+1 : 조각 a/b»(5항) 를 바 안 숫자로
                // 세부 칸도 그 펫의 **등급색**(격자·장착 칸과 같은 헬퍼 · 주인 5항 ⓘ) — `PetCell` 은 태어날 때 파랑 하나로 서므로 여기서 갈아 끼운다.
                PaintFrame(UiKit.Find(cell, "ItemFrame_01"), FrameKeyOf(dp, pet), null);
                var pw = Pets.Equip(App.Data, dp, pet, petLv < 1 ? 1 : petLv);
                int k = 0; var vals = new[] { pw.Hp, pw.Sh, pw.Atk };   // 위 SumGroup 차례와 같아야 한다(❤ · 🛡 · 🗡)
                foreach (var t in pv.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t == null || t.text == "|") continue;
                    if (k < vals.Length) t.text = "+" + UiKit.FmtQty(Math.Round(vals[k]));
                    k++;
                }
            }
            // T293 5항 ⓗ(주인 «강화 가능할 때는 해당 거 버튼 주황») — 옷·눌림이 **한 값**에서 나온다(갈라지면 «주황인데 안 눌리는» 자리가 생긴다 · 결정 1027 과 같은 자리).
            bool canUp = pet != null && Pets.CanLevelUp(dp, App.Save, pet.Id);
            var upB = UiKit.Button(box, canUp ? "ui.btnOrange" : "ui.btnGray", "강화", canUp ? (Action)(() => Upgrade(pet.Id, index)) : () => { }, Layout.PdBtnL.Within(Layout.PdBox)); upB.name = "PetUpgradeBtn";
            if (!canUp) UiKit.SetInteractable(upB.GetComponent<Button>(), false);
            // 장착/해제 — 낀 칸이 있으면 «해제», 없으면 열린 칸에 낀다. 가진 펫이 아니면 눌리지 않는다.
            bool own = pet != null && Pets.Has(App.Save, pet.Id);
            int wornSlot = pet != null ? WornSlot(pet.Id) : -1;
            var eqB = UiKit.Button(box, own ? "ui.btnOrange" : "ui.btnGray", wornSlot >= 0 ? "해제" : "장착",
                                   own ? (Action)(() => ToggleEquip(pet.Id, index)) : () => { }, Layout.PdBtnR.Within(Layout.PdBox));
            eqB.name = "PetEquipBtn";
            if (!own) UiKit.SetInteractable(eqB.GetComponent<Button>(), false);
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
            RefreshCells();
            RefreshSum();
            RefreshHelpers();
            RefreshSlots();
        }

        readonly RectTransform[] _slots = new RectTransform[SlotCount];

        /// <summary>
        /// 장착 칸 셋을 세이브대로 칠한다(주인 5항 ⓘ) — <b>열린 칸</b>은 물건 칸(낀 펫이 있으면 그 그림 + <b>등급색</b> 프레임 · 없으면 «+»),
        /// <b>잠긴 칸</b>은 잠금 원 + «뽑기 N회 해금»(수는 표가 준다 · <see cref="Pets.PullsToOpen"/>).
        /// </summary>
        void RefreshSlots()
        {
            var d = PD; var s = App != null ? App.Save : null; if (d == null || s == null) return;
            int open = Pets.SlotsOpen(d, s);
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i]; if (slot == null) continue;
                bool unlocked = i < open;
                var lockPart = UiKit.Find(slot, "LockPart"); var framePart = UiKit.Find(slot, "FramePart");
                if (lockPart != null) lockPart.gameObject.SetActive(!unlocked);
                if (framePart != null) framePart.gameObject.SetActive(unlocked);
                if (!unlocked)
                {
                    var lt = UiKit.Find(slot, "LockPart/LockText"); var ltx = lt != null ? lt.GetComponent<TMP_Text>() : null;
                    if (ltx != null) ltx.text = TextGlyphs.Safe(Pets.PullsToOpen(d, i, s.PetPulls) + "회");
                    continue;
                }
                string id = Pets.EquippedAt(d, s, i);
                var p = string.IsNullOrEmpty(id) ? null : d.Of(id);
                var frame = UiKit.Find(framePart, "ItemFrame_01"); if (frame == null) continue;
                // 낀 펫이 있으면 그 그림 + 등급색 · 없으면 «+» 그대로(빈 칸)
                UiKit.Show(frame, "Add_1", p == null);
                var item = UiKit.Find(frame, "Item");
                if (item != null)
                {
                    item.gameObject.SetActive(p != null);
                    if (p != null) UiKit.SetSprite(frame, "Item", PetIcon(d, p.Id), Palette.White);
                }
                // 프레임 색 = 그 펫의 등급색(주인 5항 ⓘ «슬롯 부분 등급마다 색») — 빈 칸은 회색.
                { string fk = FrameKeyOf(d, p); if (PaintFrame(frame, fk, _slotFrame[i])) _slotFrame[i] = fk; }
            }
        }

        readonly string[] _slotFrame = new string[SlotCount];
        readonly string[] _cellFrame = new string[Layout.PetCount];

        /// <summary>
        /// 펫 등급 → 칸 변형 키. 장비 칸과 <b>같은 문법</b>(<c>GearUi.Cell</c> <c>:165</c>) = <c>ui.itemFrame.&lt;색 이름&gt;</c> · 없는 펫(빈 칸)은 회색.
        /// <para>⚠ 여기서 <see cref="Palette.FrameKey"/> 를 쓰면 안 된다 — 그 함수는 <b>gray 변형이 없는 조각</b>(팔각 <c>ui.itemFrame4</c>)을 위해 gray 를 green 으로 바꿔 돌려준다.
        /// 이 조각(<c>ui.itemFrame</c>)에는 <b>gray 변형이 실제로 있어서</b>, 그 함수를 쓰면 <b>일반 등급 펫이 초록 칸으로</b> 뜬다.</para>
        /// </summary>
        static string FrameKeyOf(PetData d, PetData.Pet p)
        {
            var g = d != null && p != null ? d.GradeOfPet(p) : null;
            return "ui.itemFrame." + (g != null ? Palette.RarName(g.Rar) : Palette.RarColors[0]);
        }

        /// <summary>
        /// 칸(<c>ItemFrame_01</c>) 안쪽 변형을 <paramref name="key"/> 로 갈아 끼운다 — <b>이미 그 색이면 아무것도 안 한다</b>(아바타 테두리 <c>Screens.cs:439</c> 와 같은 문법 · <c>Refresh</c> 마다 조각을 새로 세우지 않게).
        /// <para>⚑ 갈아 끼운 뒤 <see cref="GearUi.DarkFrame"/> 을 <b>다시</b> 부른다 — 변형이 제 <c>Border</c> 를 달고 오므로 태어날 때 한 번 한 손질은 새 조각에 안 걸린다(결정 1096 · 런 951 빨강).</para>
        /// </summary>
        static bool PaintFrame(Transform frame, string key, string cur)
        {
            if (frame == null || key == cur) return false;
            var area = UiKit.Find(frame, "NormalArea"); if (area == null) return false;
            UiKit.Clear(area); var v = UiKit.Spawn(key, area); UiKit.Stretch((RectTransform)v.transform);
            GearUi.DarkFrame(frame, frame.localScale.x);
            return true;
        }

        /// <summary>장착 칸을 눌렀을 때 — 낀 펫이 있으면 그 <b>세부 팝업</b>, 없으면 까닭을 토스트(눌리는데 아무 일도 안 나는 자리를 안 만든다 · 결정 771).</summary>
        void TapSlot(int slot)
        {
            var d = PD; var s = App != null ? App.Save : null; if (d == null || s == null) { App.Toast(NotReadyMsg); return; }
            if (slot >= Pets.SlotsOpen(d, s)) { App.Toast("뽑기 " + Pets.PullsToOpen(d, slot, s.PetPulls) + "회에 열립니다"); return; }
            string id = Pets.EquippedAt(d, s, slot);
            if (string.IsNullOrEmpty(id)) { App.Toast("빈 칸입니다 — 펫을 골라 장착하세요"); return; }
            int cell = _cellPet != null ? _cellPet.FindIndex(x => x.Id == id) : -1;
            if (cell >= 0) OpenDetail(cell);
        }

        /// <summary>
        /// 보조 버튼 둘의 «지금 할 것이 있나» — 글자 뒤에 <b>할 수 있는 수</b>를 붙이고, 같은 수로 <b>빨간 점</b>을 켠다(T380).
        /// <para>
        /// 옷은 <b>늘 주황</b>이다(주인 2026-09-10 «둘다 주황으로») — 전에 여기 적혀 있던 «두 벌 겹치기» 걱정은 옷을 상태로 바꾸려던 때의 것이라
        /// 이제 없다. 점과 글자 뒤 수는 <b>한 값</b>(<see cref="UpgradableCount"/>·<see cref="QuickEquipCount"/>)에서 나온다 — 갈라지면
        /// «점은 켜졌는데 수는 없는» 자리가 생긴다(T366 ⓑ 가 같은 까닭으로 판정을 한 곳에 뒀다).
        /// </para>
        /// </summary>
        void RefreshHelpers()
        {
            Count(_upAll, _upAllDot, "전체 강화", UpgradableCount());
            Count(_quickEq, _quickEqDot, "빠른 장착", QuickEquipCount());
        }
        static void Count(RectTransform btn, GameObject dot, string label, int n)
        {
            if (dot != null) dot.SetActive(n > 0);
            if (btn == null) return;
            var t = UiKit.ButtonText(btn); if (t == null) return;
            t.text = TextGlyphs.Safe(n > 0 ? label + " " + n : label);
        }

        /// <summary>
        /// 격자 아홉 칸의 «Lv. N» 과 진행바(<c>조각/필요</c>) 를 세이브에서 다시 칠한다 — 안 가진 펫은 <b>Lv. 0 · 흐림</b>.
        /// <para>표의 차례가 곧 격자 차례라(9종 = 칸 9) 칸 <c>i</c> 는 표의 <c>i</c> 번째 펫이다. «가진 것만 보이기»(주인 5항 ⓙ)는 다음 회차 몫 — 칸 이름 계약을 건드리기 때문이다.</para>
        /// </summary>
        void RefreshCells()
        {
            var d = PD; var s = App != null ? App.Save : null; if (d == null || s == null) return;
            // T293 5항 ⓙ(주인 2026-09-09 11:1X «얻은 거만 보이게») — **가진 펫만** 앞에서부터 채우고 나머지 칸은 끈다(빈 칸을 안 남긴다).
            //   차례 = 등급 내림차순 → 표 차례(«빠른 장착» 이 고르는 차례와 같은 규칙 · 사람이 두 곳에서 다른 차례를 보면 안 된다).
            _cellPet = OwnedOrder(d, s);
            for (int i = 0; i < _cells.Length; i++)
            {
                var cell = _cells[i]; if (cell == null) continue;
                bool shown = i < _cellPet.Count;
                cell.gameObject.SetActive(shown);
                if (!shown) continue;
                var p = _cellPet[i];
                int lv = Pets.Lv(s, p.Id);
                bool own = lv >= 1;
                // 칸 그림도 그 펫의 것으로 — 칸 자리는 «화면의 몇 번째» 이고 무엇이 앉는지는 이 목록이 정한다.
                UiKit.SetSprite(cell, "ItemFrame_01/Item", PetIcon(d, p.Id), Palette.White);
                // 칸 프레임 = 그 펫의 등급색(주인 5항 ⓘ «격자 칸도 등급색») — 태어날 때는 파랑 하나로 세우고 여기서 목록대로 갈아 끼운다
                //   (칸 자리는 고정이고 앉는 펫이 바뀌므로, 색은 «누가 앉았나» 를 따라야 한다 · 장착 칸과 같은 헬퍼를 쓴다).
                { string fk = FrameKeyOf(d, p); if (PaintFrame(UiKit.Find(cell, "ItemFrame_01"), fk, _cellFrame[i])) _cellFrame[i] = fk; }
                var lvT = UiKit.Find(cell, "Lv"); var lvTx = lvT != null ? lvT.GetComponent<TMP_Text>() : null;
                if (lvTx != null) lvTx.text = own ? "Lv. " + lv : "Lv. 0";
                var barT = UiKit.Find(cell, "Bar");
                if (barT != null)
                {
                    int need = p != null ? Pets.Need(d, lv < 1 ? 1 : lv) : 0;
                    int frag = p != null ? Pets.Frag(s, p.Id) : 0;
                    var sl = barT.GetComponentInChildren<Slider>(true);
                    if (sl != null) sl.value = need > 0 ? Mathf.Clamp01((float)frag / need) : 0f;
                    var bt = barT.GetComponentInChildren<TMP_Text>(true);
                    if (bt != null) bt.text = frag + "/" + need;
                }
                UiKit.Ensure<CanvasGroup>(cell.gameObject).alpha = 1f;   // 켜진 칸은 전부 «가진 것» 이다(흐린 칸은 이제 없다)
            }
            // 0마리면 «무엇을 하면 되는지» 한 줄로 말한다(주인 5항 ⓙ) — 빈 격자만 두면 «고장난 화면» 으로 읽힌다.
            if (_emptyHint != null) _emptyHint.gameObject.SetActive(_cellPet.Count == 0);
        }

        /// <summary>
        /// 지금 격자에 그릴 차례 — <b>가진 펫만</b>, 등급 내림차순 → 표 차례(주인 5항 ⓙ).
        /// <para>«빠른 장착» 이 고르는 차례와 <b>같은 규칙</b>이다 — 사람이 두 자리에서 다른 차례를 보면 «무엇이 센 펫인가» 를 화면마다 다시 배워야 한다.</para>
        /// </summary>
        List<PetData.Pet> OwnedOrder(PetData d, SaveData s)
        {
            var list = new List<PetData.Pet>();
            if (d == null || s == null) return list;
            foreach (var p in d.Pets) if (Pets.Has(s, p.Id)) list.Add(p);
            list.Sort((a, b) =>
            {
                var ga = d.GradeOfPet(a); var gb = d.GradeOfPet(b);
                int ra = ga != null ? ga.Rar : -1, rb = gb != null ? gb.Rar : -1;
                if (ra != rb) return rb.CompareTo(ra);
                return d.Pets.IndexOf(a).CompareTo(d.Pets.IndexOf(b));
            });
            return list;
        }
        List<PetData.Pet> _cellPet = new List<PetData.Pet>();
        RectTransform _emptyHint;

        /// <summary>합계 줄 — 장착한 펫들이 더해 주는 공·체·실(<see cref="Pets.EquipPower"/> 한 곳에서 온다 · 화면이 다시 세지 않는다).</summary>
        void RefreshSum()
        {
            var d = PD; var s = App != null ? App.Save : null; var G = App != null ? App.Data : null;
            var row = UiKit.Find(Root, "SumRow"); if (row == null || d == null || s == null || G == null) return;
            var p = Pets.EquipPower(G, d, s);
            var texts = row.GetComponentsInChildren<TMP_Text>(true);
            // 줄은 «+수 아이콘» 세 묶음 + 구분자 둘 — 숫자 글자만 골라 순서대로(체·실·공: SumGroup 을 세운 차례 그대로) 칠한다.
            int k = 0; var vals = new[] { p.Hp, p.Sh, p.Atk };
            foreach (var t in texts)
            {
                if (t == null || t.text == "|") continue;
                if (k < vals.Length) t.text = "+" + UiKit.FmtQty(Math.Round(vals[k]));
                k++;
            }
        }

        /// <summary>
        /// 소환 버튼 두 벌의 «지금 무엇으로 몇 번» 을 다시 칠한다 — 값도 아이콘도 <see cref="Pets.Offer"/> 하나에서 나온다.
        /// <para>⚑ 버튼이 스스로 세지 않는다: 펫알이 늘거나 줄면 두 버튼이 <b>같은 함수</b>로 같이 바뀐다(T293 ⓓ).</para>
        /// </summary>
        void RefreshSummon()
        {
            var d = PD; var s = App != null ? App.Save : null; if (d == null || s == null) return;
            Paint(_sum1, Pets.Offer(d, false, s.PetEgg, PullCap));
            Paint(_sum10, Pets.Offer(d, true, s.PetEgg, PullCap));
        }
        /// <summary>윗줄 «N회» — 이 제안이 몇 번을 뽑는가(T380 · 상점 «1회»·«10회» 와 같은 낱말).</summary>
        public static string CountLabel(int count) => count + "회";
        static void Paint(RectTransform btn, Pets.PullOffer o)
        {
            if (btn == null) return;
            var l = UiKit.Find(btn, "Label"); var lt = l != null ? l.GetComponent<TMP_Text>() : null; if (lt != null) lt.text = TextGlyphs.Safe(CountLabel(o.Count));
            var cost = UiKit.Find(btn, "Cost"); if (cost == null) return;
            UiKit.SetSprite(cost, "Icon", o.ByEgg ? "pet.egg" : "hud.gem", Palette.White);
            var q = UiKit.Find(cost, "Qty"); var qt = q != null ? q.GetComponent<TMP_Text>() : null;
            if (qt != null) qt.text = UiKit.FmtComma(o.ByEgg ? o.Egg : o.Diamond);
        }
    }
}
