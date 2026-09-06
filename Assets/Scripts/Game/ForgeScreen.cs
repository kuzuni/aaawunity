using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 대장간(합성) = 레퍼런스 <c>docs/ref/08_gear_fuse.jpg</c> 구도(T39 · 주인 2026-09-06 «UI 는 무조건 레퍼런스 기준»). ref-layout ⑥ 표(<see cref="Layout.ForgeStage"/> …) 자리에 GUI Pro·Environment 조각을 조립한다:
    /// ① <b>대장간 무대</b>(위 41% · 어두운 벽 + 바닥 띠 + 화덕(어두운 상자 + 불) + 벽의 연장 + 통) 위에 <b>결과 슬롯</b>(초록 테두리 · 비었을 땐 모루 그림) → ▲(초록) → <b>재료 슬롯</b>(«+» 칸 · 규칙이 «같은 것 3개» 라 3칸 · U02 ⓓ) · 왼쪽 모루 그림 · 오른쪽 <b>안내 문구</b>(어두운 상자 · 흰 글자)
    /// → ② <b>액션바</b>(갈색 띠 · 왼쪽 끝 «자동» 파랑 + 빨간 ! · 오른쪽 끝 «합성» 회색 → 재료 3개면 주황) → ③ <b>인벤 5열 격자</b>(<see cref="GearUi.Grid"/> · 장비 탭과 같은 자리 · 합성 가능 = 초록 프레임 + 빨간 점 · 장착 = «장착중» 글자 · 재료 가능(T24)) → ④ 아래 회색 띠 + 왼쪽 <b>뒤로(◀)</b>.
    /// 규칙은 그대로: 같은 부위·종류·등급 3개 → <see cref="GearSystem.FuseMake"/> 하나만(자동 = FuseAll 도 같은 함수). **장착 중인 장비도 재료다**(T24 · 주인 «대장간에 장착중인 거도 합성 가능하게» — aaaw T125 를 주인이 뒤집음). 장착분이 재료로 사라지면 <see cref="GearSystem.ReEquipAfterFuse"/>(같은 부위면 산출물을 그 슬롯에 · 승인 대기 29 기본값).
    /// 칸은 전부 장비 화면과 같은 ListItem_EquipMent 본래 크기(188 정사각 · T8) — 재료 3칸은 슬롯 자리 가운데에 본래 크기로. 제목 글자·상단 재화 바는 없다(레퍼런스에 없음).
    /// 이름 계약(스모크 테스트): 무대 <c>Stage</c> · 결과 <c>Result</c> · 인벤 <c>Content</c> · 버튼 <c>AutoBtn</c>/<c>FuseBtn</c>(회색)/<c>FuseBtnOn</c>(주황)/<c>BackBtn</c> · 합성 가능 칸의 빨간 점 <c>FuseDot</c> · 장착 글자 <c>EquippedLabel</c>.
    /// </summary>
    public sealed class ForgeScreen : GameScreen
    {
        public override string Name => "forge";
        readonly List<int> _sel = new List<int>();   // 선택 재료 uid (최대 3)
        RectTransform _mats, _result, _content, _fuseOff, _fuseOn; Text _banner, _fuseTxtOff, _fuseTxtOn; Button _auto; GameObject _autoDot;
        readonly RectTransform[] _matSlot = new RectTransform[3];   // 재료 슬롯 자리(Pct) — 칸은 그 가운데 본래 크기

        /// <summary>무대 안 조각 자리(무대 % · 레퍼런스 08 을 눈으로 잰 것 · 전부 점수 밖 «느낌» — 표 행은 무대 사각형뿐).</summary>
        static readonly Layout.R StageFloor = new Layout.R(0, 62, 100, 38);          // 아래 38% 는 바닥(밝은 갈색 띠) · 위는 벽
        static readonly Layout.R StageHearth = new Layout.R(40, 16, 52, 60);         // 화덕(어두운 상자) — 안내 문구가 그 위에 얹힌다
        static readonly Layout.R StageFire = new Layout.R(56, 46, 20, 24);           // 화덕 안 불
        static readonly Layout.R StageBarrelL = new Layout.R(-4, 70, 16, 26), StageBarrelR = new Layout.R(88, 70, 16, 26);
        static readonly Layout.R StageToolA = new Layout.R(8, 5, 9, 14), StageToolB = new Layout.R(19, 5, 9, 14);   // 벽에 걸린 연장(망치·도끼)
        /// <summary>모루 그림(프레임 %) — 결과 슬롯과 재료 슬롯 사이 왼쪽(레퍼런스 x4~36 · y21~29).</summary>
        /// <summary>아래 회색 띠 — 탭바 자리(레퍼런스는 탭바 대신 회색 띠 + 뒤로 버튼).</summary>
        static readonly Layout.R BottomStrip = Layout.TabBar;

        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Color.Lerp(Palette.Slate, Palette.Dim, 0.45f); bg.raycastTarget = true;   // 인벤 바탕 = 어두운 남색(장비 탭과 같은 값 · 색은 점수 밖)
            // T72 ① 배경 패턴(어두운 바탕 → 흰 무늬 α0.12 · 오른쪽 위로 천천히 · 맨 뒤) — 장비 화면(GearScreen)과 같은 값 · 화면 적용은 T69-forge 묶음이 같이(ROUTINE T72 «한 화면 세 번 만지지 않기»)
            UiKit.PatternBg(Root, UiKit.PatternTintDark);
            // T72 ③ 화면 배경 그라데이션(3항 «화면 배경 = 위 밝고 아래 어두운 두 장» · 주인 2026-09-07 «레퍼런스처럼 그라데이션 있게») — 무늬 바로 위 · 무대·슬롯·액션바·격자·띠 아래
            UiKit.Gradient(Root);

            // ① 대장간 무대 — 벽(어두운 갈색) + 바닥 띠 + 화덕(어두운 상자 + 불) + 벽의 연장 2 + 통 2 · 무대 밖은 잘라낸다
            var stage = UiKit.Rect(Root, "Stage"); UiKit.Pct(stage, Layout.ForgeStage); UiKit.Ensure<RectMask2D>(stage.gameObject);
            {
                var wall = UiKit.Panel(stage, "Wall", "fr.rect", Color.Lerp(Palette.InkSoft, Palette.Ink, 0.55f)); UiKit.Stretch(wall.rectTransform);
                var floor = UiKit.Panel(stage, "Floor", "fr.rect", Color.Lerp(Palette.InkLight, Palette.Ink, 0.35f)); UiKit.Pct(floor.rectTransform, StageFloor);
                var hearth = UiKit.SpawnRt("ui.frameDark", stage, StageHearth); hearth.name = "Hearth";
                var fire = UiKit.Icon(stage, "Fire", "ui.fire"); UiKit.Pct(fire.rectTransform, StageFire);
                var toolA = UiKit.Icon(stage, "ToolA", "pi.hammer", Palette.A(Palette.Cream, 0.55f)); UiKit.Pct(toolA.rectTransform, StageToolA);
                var toolB = UiKit.Icon(stage, "ToolB", "pi.axe", Palette.A(Palette.Cream, 0.55f)); UiKit.Pct(toolB.rectTransform, StageToolB);
                var bl = UiKit.Icon(stage, "BarrelL", "env.barrel"); UiKit.Pct(bl.rectTransform, StageBarrelL);
                var br = UiKit.Icon(stage, "BarrelR", "env.barrel"); UiKit.Pct(br.rectTransform, StageBarrelR);
            }
            // ⓐ 모루 그림(AnvilArt)은 주인 지시 2026-09-07 08:1X «대장간에 AnvilArt 빼셈» 으로 없앴다(T113 ⓐ) —
            //   그 자리는 채우지 않고 비워 둔다(위아래 요소 자리 불변). 결과 칸 «안» 의 작은 모루 아이콘은 다른 것이라 그대로 둔다.
            // 결과 슬롯(초록 테두리) · ▲(초록) · 재료 슬롯 3칸(피치 19 · 레퍼런스는 «+» 1칸 — U02 ⓓ 영구 X 행)
            _result = UiKit.Rect(Root, "Result"); UiKit.Pct(_result, Layout.ForgeResult);
            var arrow = UiKit.Icon(Root, "Arrow", "pi.arrow_right", Palette.Cream); UiKit.Pct(arrow.rectTransform, Layout.ForgeArrow); arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            _mats = UiKit.Rect(Root, "Mats"); UiKit.Stretch(_mats);
            for (int i = 0; i < 3; i++) { _matSlot[i] = UiKit.Rect(_mats, "Mat" + i); UiKit.Pct(_matSlot[i], Layout.ForgeMat.X + i * Layout.ForgeMatPitch, Layout.ForgeMat.Y, Layout.ForgeMat.W, Layout.ForgeMat.H); }
            // 안내 문구 — 화덕 위 어두운 상자 + 흰 글자(레퍼런스 «Select equipment to merge»)
            var banner = UiKit.SpawnRt("ui.frameDark", Root, Layout.ForgeBanner); banner.name = "Banner";
            _banner = UiKit.Text(banner, "", 28, Palette.White, TextAnchor.MiddleCenter, true, false); UiKit.Stretch(_banner.rectTransform, 12, 6, 12, 6);

            // ② 액션바 — 갈색 띠 · 왼쪽 끝 «자동»(파랑 + 빨간 !) · 오른쪽 끝 «합성»(회색 → 재료 3개면 주황: 같은 자리에 두 버튼을 두고 하나만 보인다)
            var band = UiKit.Panel(Root, "ActionBar", "fr.rect", Palette.InkLight); UiKit.Pct(band.rectTransform, Layout.ForgeActionBar); band.raycastTarget = true;
            UiKit.Gradient(band.rectTransform);   // T72 ③ 액션바 띠(레퍼런스 08 의 갈색 띠도 위 밝고 아래 어둡다)
            var autoRt = UiKit.Button(Root, "ui.btnBlue", "자동", OnAuto, Layout.ForgeAuto); autoRt.name = "AutoBtn"; _auto = autoRt.GetComponent<Button>();
            { var d = UiKit.Spawn("ui.alertDot", autoRt); var dr = (RectTransform)d.transform; d.name = "AutoDot"; dr.anchorMin = dr.anchorMax = new Vector2(1, 1); dr.pivot = new Vector2(0.5f, 0.5f); dr.anchoredPosition = new Vector2(-4, 4); dr.sizeDelta = new Vector2(52, 52); _autoDot = d; }
            _fuseOff = UiKit.Button(Root, "ui.btnGray", "합성 (0/3)", OnFuse, Layout.ForgeFuse); _fuseOff.name = "FuseBtn"; _fuseTxtOff = UiKit.ButtonText(_fuseOff);
            _fuseOn = UiKit.Button(Root, "ui.btnOrange", "합성 (3/3)", OnFuse, Layout.ForgeFuse); _fuseOn.name = "FuseBtnOn"; _fuseTxtOn = UiKit.ButtonText(_fuseOn);
            _fuseOn.gameObject.SetActive(false);

            // ③ 인벤 5열 격자(장비 탭과 같은 자리·격자 값) → ④ 아래 회색 띠 + 왼쪽 뒤로(◀ · 아이콘만 · 레퍼런스에 글자 없음)
            _content = GearUi.Grid(Root, Layout.ForgeInv, out _);
            var strip = UiKit.Panel(Root, "BottomStrip", "fr.rect", Color.Lerp(Palette.Gray, Palette.Dim, 0.35f)); UiKit.Pct(strip.rectTransform, BottomStrip); strip.raycastTarget = true;
            UiKit.Gradient(strip.rectTransform);   // T72 ③ 아래 띠(탭바 자리)
            var back = UiKit.Button(Root, "ui.btnGray", "", () => App.ShowScreen("gear"), Layout.ForgeBack); back.name = "BackBtn";
            { var t = UiKit.ButtonText(back); if (t != null) t.gameObject.SetActive(false); var ic = UiKit.Icon(back, "Icon", "pi.arrow_left", Palette.Cream); UiKit.Pct(ic.rectTransform, 30, 18, 40, 64); }

            // 비평 이름표(T46 · ref-layout ⑥ 의 «요소» 이름 그대로 · «재료 슬롯» 은 첫 칸 · «합성 버튼» 은 보이는 쪽만 잰다)
            UiKit.Tag(stage, "대장간 무대"); UiKit.Tag(_result, "결과 슬롯"); UiKit.Tag(arrow.transform, "화살표"); UiKit.Tag(_matSlot[0], "재료 슬롯"); UiKit.Tag(banner, "안내 문구");
            UiKit.Tag(band.transform, "액션바"); UiKit.Tag(autoRt, "자동 버튼"); UiKit.Tag(_fuseOff, "합성 버튼"); UiKit.Tag(_fuseOn, "합성 버튼"); UiKit.Tag(_content.parent, "인벤 그리드"); UiKit.Tag(back, "뒤로 버튼");
        }
        protected override void OnHide() { _sel.Clear(); }

        List<GearItem> Mats() { var l = new List<GearItem>(); foreach (var u in _sel) { var g = App.Save.InvById(u); if (g != null) l.Add(g); } return l; }

        public override void Refresh()
        {
            var D = App.Data; var S = App.Save;
            var mats = Mats(); if (mats.Count != _sel.Count) { _sel.Clear(); foreach (var g in mats) _sel.Add(g.Uid); }
            string lock_ = mats.Count > 0 ? GearUi.Key(mats[0]) : null;
            var fk = GearUi.FusableKeys(S);
            // 인벤 — 선택 = 프리팹 Focus · 다른 키만 흐리게(장착분은 흐리지 않는다 — T24 재료 가능) · 합성 가능 = 초록 프레임 + 빨간 점(재료를 고르는 중엔 끔 — index.html renderForge `fus:!lock&&…` 그대로) · 장착중 = «장착중» 글자(레퍼런스 «Equipped»)
            // ⚠ 인벤을 맨 먼저 채운다 — 예전(e64ff41 이전)엔 아래 버튼 처리(SetInteractable 의 CanvasGroup «GetComponent 뒤 ?? AddComponent» 패턴)가 에디터 가짜 null 로
            //   MissingComponentException 을 던져 그 뒤의 인벤 루프가 통째로 건너뛰어졌다(«하단에 장비가 없다» 의 원인). 지금은 UiKit.Ensure 로 고쳐졌지만 순서도 인벤 우선으로 둔다.
            UiKit.Clear(_content);
            if (S.Inv.Count == 0) GearUi.Empty(_content, "장비가 없습니다.\n상점에서 뽑기로 장비를 얻으세요.");
            foreach (var g in GearUi.Sorted(S))
            {
                var gi = g; bool sel = _sel.Contains(g.Uid); bool off = lock_ != null && !sel && GearUi.Key(g) != lock_; bool fus = lock_ == null && fk.Contains(GearUi.Key(g)); bool eqd = S.IsEquipped(g);
                var cell = GearUi.Cell(_content, D, g, new GearUi.CellOpts { Equipped = eqd, EquippedMark = false, Selected = sel, Off = off, Fusable = fus, FusableDot = true }, () => Toggle(gi));
                if (eqd) EquippedTag(cell);
            }
            // 재료 3칸 (ref 재료 슬롯 자리에서 피치 19) — 칸은 슬롯 자리 가운데에 ListItem_EquipMent 본래 크기(188 정사각 · 인벤 칸과 같은 크기·비례) · 빈 칸은 프리팹의 «+»
            for (int i = 0; i < 3; i++)
            {
                UiKit.Clear(_matSlot[i]);
                var g = i < mats.Count ? mats[i] : null; var gi = g;
                GearUi.Cell(_matSlot[i], D, g, new GearUi.CellOpts(), gi != null ? (Action)(() => { _sel.Remove(gi.Uid); Refresh(); }) : null);
            }
            // 결과 슬롯 — 초록 테두리(레퍼런스 «선택 칸») · 비었을 땐 모루 그림
            UiKit.Clear(_result);
            if (mats.Count == 3)
            {
                var basis = Basis(mats); var made = GearSystem.FuseMake(D, basis);
                var cell = GearUi.Cell(_result, D, made, new GearUi.CellOpts(), null);
                bool conv = basis.Rar == D.Gear.RarLegend && made.Rar == D.Gear.RarMyth;
                _banner.text = $"<b>{GearUi.RarName(D, made.Rar)} {GearUi.Name(D, made)}</b>{(made.Plus > 0 ? $" <b>+{made.Plus}</b>" : "")}\n" +
                    (conv ? $"<color=#F3A80E>전설 +{D.Gear.LegendToMythPlus}강 대신 <b>신화 0강</b>으로 바뀝니다</color>\n" : "") + "<size=20>재료 3개가 사라지고 위 장비 1개가 됩니다</size>";
            }
            else
            {
                var empty = GearUi.Cell(_result, D, null, new GearUi.CellOpts(), null);
                var ef = UiKit.Find(empty, "ItemFrame_01"); if (ef != null) UiKit.Show(ef, "Add_1", false);   // 빈 결과 칸은 «+» 대신 모루 그림(레퍼런스)
                var ic = UiKit.Icon(empty, "Anvil", "pi.anvil", Palette.A(Palette.Cream, 0.7f)); UiKit.Pct(ic.rectTransform, 22, 22, 56, 56);
                _banner.text = mats.Count > 0
                    ? $"같은 <b>{GearUi.PartName(D, mats[0].Part)} · {GearUi.Name(D, mats[0])} · {GearUi.RarName(D, mats[0].Rar)}</b> 을(를)\n<b>{3 - mats.Count}개</b> 더 고르세요"
                    : "합성할 장비를\n고르세요";
            }
            // 액션바 — 자동(합성 조합이 있으면 빨간 !) · 합성(재료 3개면 주황 버튼으로 교체)
            UiKit.SetInteractable(_auto, fk.Count > 0); if (_autoDot != null) _autoDot.SetActive(fk.Count > 0);
            bool ready = mats.Count == 3;
            if (_fuseOff != null) { _fuseOff.gameObject.SetActive(!ready); if (_fuseTxtOff != null) _fuseTxtOff.text = $"합성 ({mats.Count}/3)"; UiKit.SetInteractable(_fuseOff.GetComponent<Button>(), false); }
            if (_fuseOn != null) { _fuseOn.gameObject.SetActive(ready); if (_fuseTxtOn != null) _fuseTxtOn.text = $"합성 ({mats.Count}/3)"; }
        }
        /// <summary>칸의 등급색 프레임을 초록(ItemFrame_01_Normal_Green)으로 바꾼다 — 레퍼런스의 «합성 가능» 칸·«선택 칸» 초록 테두리(조각 교체 · 새 그림 없음).</summary>
        // 장착분 표기 «장착중» = 레퍼런스 08 의 «Equipped» — 어두운 띠 위 흰 글자(T63-forge).
        // 띠가 없으면 본문 하한(40)으로 올라간 글자가 장비 그림 위에 그대로 얹혀 안 읽힌다(screens run 95 `08_gear_fuse.png` 실측 — 6칸 전부 뭉갬).
        // 글자 크기 게이트의 «잘림/넘침» 은 «선호 크기 > rect» 만 보므로 이 겹침은 표에 안 나온다(ROUTINE T63 2단계 ⚠).
        // 회차 2(screens run 106): 알파 0.82 로는 장비 그림이 절반쯤 비쳐 여전히 뭉갰다 → 레퍼런스 08 의 «Equipped» 띠처럼 거의 불투명(0.94 · 한 단계 더 어둡게).
        const float TagY = 44, TagH = 32;
        static void EquippedTag(Transform cell)
        {
            var plate = UiKit.Panel(cell, "EquippedPlate", "fr.rect", Palette.A(Color.Lerp(Palette.Dim, Color.black, 0.35f), 0.94f));
            UiKit.Pct(plate.rectTransform, 2, TagY, 96, TagH);
            var lb = UiKit.Label(cell, 4, TagY, 92, TagH, "장착중", TextSize.Body, Palette.White, TextAnchor.MiddleCenter);
            lb.name = "EquippedLabel";
        }

        // ⓑ 초록 프레임(GreenFrame)은 없앴다 — 주인 2026-09-07 08:1X «완성됐을 때의 슬롯 부분이 초록인데 그러지 말고 색 통일»(T113 ⓑ).
        //   이제 결과 칸·선택 칸·«합성 가능» 칸이 다른 칸과 같은 규칙(T103 정본 = 그 아이템의 등급색 ItemFrame_01_Normal_* · 빈 칸은 ui.itemFrame.empty)으로 그려진다.
        //   «합성 가능/완성» 은 색이 아니라 ⓐ 인벤 칸의 빨간 점(GearUi.CellOpts.FusableDot) ⓑ 액션바 «자동» 버튼의 빨간 ! + 활성 ⓒ 재료가 3개면 주황 «합성» 버튼
        //   — 이미 다 있던 표시들로 알린다(결정 273). T69-forge 가 걸어 둔 «GreenFrame 뒤 DarkFrame 재호출»(결정 167)도 같이 사라진다 — 칸은 GearUi.Cell 이 칠한 링을 그대로 쓴다.
        static GearItem Basis(List<GearItem> mats) { var b = mats[0]; foreach (var m in mats) if (m.Plus > b.Plus) b = m; return b; }

        void Toggle(GearItem g)
        {
            var D = App.Data; var S = App.Save; var mats = Mats();
            if (_sel.Contains(g.Uid)) { _sel.Remove(g.Uid); Refresh(); return; }
            // 장착 중인 장비도 재료가 된다(T24) — 예전의 «먼저 해제하세요» 토스트 없음
            if (mats.Count > 0 && GearUi.Key(g) != GearUi.Key(mats[0])) { App.Toast($"같은 부위·종류·등급만 재료가 됩니다 ({GearUi.PartName(D, mats[0].Part)} · {GearUi.Name(D, mats[0])} · {GearUi.RarName(D, mats[0].Rar)})"); return; }
            if (_sel.Count >= 3) { App.Toast("재료는 3개까지입니다"); return; }
            _sel.Add(g.Uid); Refresh();
        }
        void OnFuse()
        {
            var D = App.Data; var S = App.Save; var mats = Mats(); if (mats.Count != 3) return;
            var made = GearSystem.FuseMake(D, Basis(mats));
            foreach (var m in mats) S.Inv.Remove(m);
            made.Uid = S.Uid++; S.Inv.Add(made); S.Fuses++; _sel.Clear();
            GearSystem.ReEquipAfterFuse(S, mats, made);   // 장착분이 재료였으면 산출물을 그 슬롯에(T24 · 승인 대기 29) — 세이브·전투력·외형은 Persist/화면 Refresh 가 S.Eq 에서 다시 읽는다
            App.Persist(); Refresh(); Audio.Sfx("snd.fuse");
            App.Toast($"🔨 {GearUi.RarName(D, made.Rar)} {GearUi.Name(D, made)}{(made.Plus > 0 ? $" +{made.Plus}" : "")} 완성!" + (S.IsEquipped(made) ? " (장착 중이던 재료 자리에 장착)" : ""));
        }
        void OnAuto()
        {
            var D = App.Data; var S = App.Save; int before = S.Inv.Count;
            // 장착분도 포함해 합성(T24 · 제외 집합 없음) — 합성 1회마다 장착 슬롯 정리(산출물이 같은 부위면 그 슬롯에)
            int n = GearSystem.FuseAll(D, S.Inv, null, g => S.Uid++, (mats, made) => GearSystem.ReEquipAfterFuse(S, mats, made));
            foreach (var g in S.Inv) if (g.Uid == 0) g.Uid = S.Uid++;
            S.Fuses += n; _sel.Clear();
            if (n > 0) { App.Persist(); Refresh(); Audio.Sfx("snd.fuse"); App.Toast($"🔨 {n}회 합성 (장비 {before} → {S.Inv.Count})"); }
            else App.Toast("합성할 조합이 없습니다 (같은 부위·종류·등급 3개)");
        }
    }
}
