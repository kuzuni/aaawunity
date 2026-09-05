using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 대장간(합성) — 참고 docs/ref/장비 합성 업글창.jpg · ref-layout ⑥. 재료 3칸 → ▲ → 결과 미리보기 · 자동/합성 버튼 · 인벤 격자.
    /// 규칙: 같은 부위·종류·등급 3개 → <see cref="GearSystem.FuseMake"/> 하나만 쓴다(자동 = FuseAll 도 같은 함수). 장착분은 재료가 아니다(주인 확정 T125).
    /// </summary>
    public sealed class ForgeScreen : GameScreen
    {
        public override string Name => "forge";
        readonly List<int> _sel = new List<int>();   // 선택 재료 uid (최대 3)
        RectTransform _mats, _result, _content; Text _banner, _autoTxt, _fuseTxt; Button _auto, _fuse;

        protected override void Build()
        {
            var bg = Root.gameObject.AddComponent<Image>(); bg.color = Palette.Hex("#EBDEC0"); bg.raycastTarget = true;   // Character_Hero_Equipment 의 Background 색
            var pills = UiKit.SpawnRt("ui.resourceBar", Root, new Layout.R(2, 3.7f, 48, 2.8f)); UiKit.SetText(pills, "ResourceBar_Coin/Text (TMP)", "0"); UiKit.SetText(pills, "ResourceBar_Gem/Text (TMP)", "0");
            var title = UiKit.SpawnRt("ui.lineTitle", Root, new Layout.R(30, 7.5f, 40, 4)); UiKit.SetText(title, "Text (TMP)", "대장간", Palette.Ink, 44);
            var stage = UiKit.Panel(Root, "Stage", "fr.rect", Palette.CreamDark); UiKit.Pct(stage.rectTransform, Layout.ForgeStage);
            _mats = UiKit.Rect(Root, "Mats"); UiKit.Pct(_mats, Layout.ForgeMats);
            var arrow = UiKit.Icon(Root, "Arrow", "pi.arrow_right", Palette.InkLight); UiKit.Pct(arrow.rectTransform, Layout.ForgeArrow); arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            _result = UiKit.Rect(Root, "Result"); UiKit.Pct(_result, Layout.ForgeResult);
            var banner = UiKit.SpawnRt("ui.frameIvory", Root, Layout.ForgeBanner);
            _banner = UiKit.Text(banner, "", 28, Palette.InkSoft, TextAnchor.MiddleCenter, true, false); UiKit.Stretch(_banner.rectTransform, 16, 8, 16, 8);
            var autoRt = UiKit.Button(Root, "ui.btnBlue", "자동", OnAuto, Layout.ForgeAuto); _auto = autoRt.GetComponent<Button>(); _autoTxt = UiKit.ButtonText(autoRt);
            var fuseRt = UiKit.Button(Root, "ui.btnOrange", "합성 (0/3)", OnFuse, Layout.ForgeFuse); _fuse = fuseRt.GetComponent<Button>(); _fuseTxt = UiKit.ButtonText(fuseRt);
            _content = GearUi.Grid(Root, Layout.ForgeInv, out _);
            UiKit.Button(Root, "ui.btnGray", "← 장비", () => App.ShowScreen("gear"), Layout.ForgeBack);
            NavBar.Attach(this, Root, "forge");
        }
        protected override void OnHide() { _sel.Clear(); }

        List<GearItem> Mats() { var l = new List<GearItem>(); foreach (var u in _sel) { var g = App.Save.InvById(u); if (g != null) l.Add(g); } return l; }

        public override void Refresh()
        {
            var D = App.Data; var S = App.Save;
            var mats = Mats(); if (mats.Count != _sel.Count) { _sel.Clear(); foreach (var g in mats) _sel.Add(g.Uid); }
            string lock_ = mats.Count > 0 ? GearUi.Key(mats[0]) : null;
            UiKit.SetText(Root, "ResourceBar_Coin/Text (TMP)", UiKit.Fmt(S.Gold)); UiKit.SetText(Root, "ResourceBar_Gem/Text (TMP)", UiKit.Fmt(S.Gem));
            // 재료 3칸
            UiKit.Clear(_mats);
            for (int i = 0; i < 3; i++)
            {
                var g = i < mats.Count ? mats[i] : null; var gi = g;
                var cell = GearUi.Cell(_mats, D, g, new GearUi.CellOpts(), gi != null ? (Action)(() => { _sel.Remove(gi.Uid); Refresh(); }) : null);
                UiKit.Pct(cell, i * 34f, 0, 30f, 100f);
            }
            // 결과 미리보기
            UiKit.Clear(_result);
            if (mats.Count == 3)
            {
                var basis = Basis(mats); var made = GearSystem.FuseMake(D, basis);
                GearUi.Cell(_result, D, made, new GearUi.CellOpts(), null);
                bool conv = basis.Rar == D.Gear.RarLegend && made.Rar == D.Gear.RarMyth;
                _banner.text = $"<b>{GearUi.RarName(D, made.Rar)} {GearUi.Name(D, made)}</b>{(made.Plus > 0 ? $" <b>+{made.Plus}</b>" : "")}\n" +
                    (conv ? $"<color=#F3A80E>전설 +{D.Gear.LegendToMythPlus}강 대신 <b>신화 0강</b>으로 바뀝니다</color>\n" : "") + "<size=22>재료 3개가 사라지고 위 장비 1개가 됩니다</size>";
            }
            else
            {
                var empty = GearUi.Cell(_result, D, null, new GearUi.CellOpts(), null);
                var ic = UiKit.Icon(empty, "Anvil", "pi.hammer", Palette.InkLight); UiKit.Pct(ic.rectTransform, 25, 25, 50, 50);
                _banner.text = mats.Count > 0
                    ? $"같은 <b>{GearUi.PartName(D, mats[0].Part)} · {GearUi.Name(D, mats[0])} · {GearUi.RarName(D, mats[0].Rar)}</b> 을(를)\n<b>{3 - mats.Count}개</b> 더 고르세요"
                    : "합성할 장비를 선택하세요\n<size=22>같은 부위·종류·등급 3개</size>";
            }
            var fk = GearUi.FusableKeys(S);
            UiKit.SetInteractable(_auto, fk.Count > 0); if (_autoTxt != null) _autoTxt.text = fk.Count > 0 ? $"자동 ({fk.Count}) !" : "자동";
            UiKit.SetInteractable(_fuse, mats.Count == 3); if (_fuseTxt != null) _fuseTxt.text = $"합성 ({mats.Count}/3)";
            // 인벤 — 선택 초록 · 다른 키/장착분 흐리게
            UiKit.Clear(_content);
            if (S.Inv.Count == 0) GearUi.Empty(_content, "장비가 없습니다.\n상점에서 뽑기로 장비를 얻으세요.");
            foreach (var g in GearUi.Sorted(S))
            {
                var gi = g; bool sel = _sel.Contains(g.Uid); bool off = S.IsEquipped(g) || (lock_ != null && !sel && GearUi.Key(g) != lock_);
                GearUi.Cell(_content, D, g, new GearUi.CellOpts { Equipped = S.IsEquipped(g), Selected = sel, Off = off, Fusable = lock_ == null && fk.Contains(GearUi.Key(g)) }, () => Toggle(gi));
            }
            NavBar.Refresh(Root);
        }
        static GearItem Basis(List<GearItem> mats) { var b = mats[0]; foreach (var m in mats) if (m.Plus > b.Plus) b = m; return b; }

        void Toggle(GearItem g)
        {
            var D = App.Data; var S = App.Save; var mats = Mats();
            if (_sel.Contains(g.Uid)) { _sel.Remove(g.Uid); Refresh(); return; }
            if (S.IsEquipped(g)) { App.Toast("장착 중인 장비는 재료가 되지 않습니다 — 먼저 해제하세요"); return; }
            if (mats.Count > 0 && GearUi.Key(g) != GearUi.Key(mats[0])) { App.Toast($"같은 부위·종류·등급만 재료가 됩니다 ({GearUi.PartName(D, mats[0].Part)} · {GearUi.Name(D, mats[0])} · {GearUi.RarName(D, mats[0].Rar)})"); return; }
            if (_sel.Count >= 3) { App.Toast("재료는 3개까지입니다"); return; }
            _sel.Add(g.Uid); Refresh();
        }
        void OnFuse()
        {
            var D = App.Data; var S = App.Save; var mats = Mats(); if (mats.Count != 3) return;
            var made = GearSystem.FuseMake(D, Basis(mats));
            foreach (var m in mats) S.Inv.Remove(m);
            made.Uid = S.Uid++; S.Inv.Add(made); S.Fuses++; _sel.Clear(); App.Persist(); Refresh();
            App.Toast($"🔨 {GearUi.RarName(D, made.Rar)} {GearUi.Name(D, made)}{(made.Plus > 0 ? $" +{made.Plus}" : "")} 완성!");
        }
        void OnAuto()
        {
            var D = App.Data; var S = App.Save; int before = S.Inv.Count;
            int n = GearSystem.FuseAll(D, S.Inv, S.EquippedSet(D), g => S.Uid++);
            foreach (var g in S.Inv) if (g.Uid == 0) g.Uid = S.Uid++;
            S.Fuses += n; _sel.Clear();
            if (n > 0) { App.Persist(); Refresh(); App.Toast($"🔨 {n}회 합성 (장비 {before} → {S.Inv.Count})"); }
            else App.Toast("합성할 조합이 없습니다 (같은 부위·종류·등급 3개)");
        }
    }
}
