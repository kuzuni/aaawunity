using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 장비 탭 — 주인 지정 GUI Pro 데모 프리팹 **Character_Hero_Equipment** 를 세우고 내용을 바꾼다.
    /// 좌우 슬롯 3+3(무기·목걸이·갑옷 / 투구·장갑·신발) · 공/체/실 3칸 · 균등 보너스 · 인벤 격자(장착분 먼저) · 합성 버튼.
    /// 자동 장착 없음(주인 확정 T125) — «↑ 더 좋은 게 있다» 표시만.
    /// </summary>
    public sealed class GearScreen : GameScreen
    {
        public override string Name => "gear";
        RectTransform _rt; Transform _slots, _content; Text _power, _even, _atk, _hp, _sh; Transform _fuseBtn;
        static readonly string[] GridOrder = { "weapon", "helm", "neck", "glove", "armor", "boot" };   // 2열 격자 행 우선 = 왼쪽열/오른쪽열 (GEAR_COL)

        protected override void Build()
        {
            var root = UiKit.Spawn("ui.equipment", Root); _rt = (RectTransform)root.transform; UiKit.Stretch(_rt);
            UiKit.Hide(_rt, "Label_Tapered_01_Yellow", "Tab_02_BoxMenu_Icon", "Character");
            var title = UiKit.Find(_rt, "Title_LineDeco_02_s"); if (title != null) UiKit.SetText(title, "Text (TMP)", "장비", Palette.Ink, 56);
            var hero = UiKit.Icon(_rt, "Hero", "ui.battle"); UiKit.Pct(hero.rectTransform, Layout.GearHero.X + 5, Layout.GearHero.Y, Layout.GearHero.W - 10, Layout.GearHero.H - 5);
            _slots = UiKit.Find(_rt, "Group_Slot");
            var mid1 = UiKit.Find(_rt, "Middle1");
            if (mid1 != null)
            {
                _even = UiKit.SetText(mid1, "Text_Level", "", Palette.InkSoft, 30);
                var grp = UiKit.Find(mid1, "Group"); if (grp != null) { _power = UiKit.SetText(grp, "Text", "0", Palette.Orange, 44); UiKit.SetSprite(grp, "Icon", "ui.battle", Palette.White); }
            }
            var list = UiKit.Find(_rt, "Group_List");
            if (list != null && list.childCount >= 3)
            {
                _atk = UiKit.SetText(list.GetChild(0), "Text (TMP)", "0", Palette.InkSoft); UiKit.SetSprite(list.GetChild(0), "Icon", "pi.attack", Palette.InkSoft);
                _hp = UiKit.SetText(list.GetChild(1), "Text (TMP)", "0", Palette.InkSoft); UiKit.SetSprite(list.GetChild(1), "Icon", "pi.heart", Palette.Red);
                _sh = UiKit.SetText(list.GetChild(2), "Text (TMP)", "0", Palette.InkSoft); UiKit.SetSprite(list.GetChild(2), "Icon", "pi.shield", Palette.Sky);
            }
            _content = UiKit.Find(_rt, "Content");
            if (_content != null)
            {
                UiKit.Clear(_content);
                var grid = _content.GetComponent<GridLayoutGroup>();
                if (grid != null) { float cellW = UiKit.FrameW * 0.94f / Layout.GearInvCols - 12; grid.cellSize = new Vector2(cellW, cellW); grid.spacing = new Vector2(12, 14); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = Layout.GearInvCols; grid.childAlignment = TextAnchor.UpperCenter; }
                var fit = _content.GetComponent<ContentSizeFitter>() ?? _content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var sr = _rt.GetComponentInChildren<ScrollRect>(true); if (sr != null) { sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40; var vp = sr.viewport != null ? sr.viewport.GetComponent<Image>() : null; if (vp != null) vp.raycastTarget = true; }
            }
            var bottom = UiKit.Find(_rt, "Bottom");
            if (bottom != null)
            {
                var back = UiKit.Find(bottom, "ArrowIconButton_03_Back"); if (back != null) UiKit.Clickable(back, () => App.ShowScreen("lobby"));
                var btns = UiKit.Find(bottom, "Group_Buttons");
                if (btns != null && btns.childCount >= 2)
                {
                    _fuseBtn = btns.GetChild(0); UiKit.Clickable(_fuseBtn, () => App.ShowScreen("forge"));
                    UiKit.SetText(btns.GetChild(1), "Text (TMP)", "상점"); UiKit.Clickable(btns.GetChild(1), () => App.ShowScreen("shop"));
                }
            }
            NavBar.Attach(this, Root, "gear");
        }

        public override void Refresh()
        {
            var D = App.Data; var S = App.Save;
            // 슬롯 6칸
            if (_slots != null)
            {
                var fk = GearUi.FusableKeys(S);
                for (int i = 0; i < _slots.childCount && i < GridOrder.Length; i++)
                {
                    var slot = _slots.GetChild(i); string part = GridOrder[i]; var g = S.EquippedGear(part); int lv = S.SlotLv(part);
                    var frame = UiKit.Find(slot, "ItemFrame_01");
                    if (frame != null)
                    {
                        var area = UiKit.Find(frame, "NormalArea"); if (area != null) { UiKit.Clear(area); if (g != null) { var f = UiKit.Spawn("ui.itemFrame." + Palette.RarName(g.Rar), area); UiKit.Stretch((RectTransform)f.transform); } }
                        var item = UiKit.Find(frame, "Item"); if (item != null) { item.gameObject.SetActive(g != null); if (g != null) UiKit.SetSprite(frame, "Item", GearUi.IconKey(D, g), Palette.White); var ir = (RectTransform)item; ir.anchorMin = new Vector2(0.14f, 0.14f); ir.anchorMax = new Vector2(0.86f, 0.86f); ir.offsetMin = ir.offsetMax = Vector2.zero; }
                        UiKit.Show(frame, "Add_1", g == null); UiKit.Show(frame, "Add_2", false); UiKit.Show(frame, "Lock", false);
                    }
                    var dia = UiKit.FindAny(slot, "BasicFrame_Diamond_H48_NoBorder_Plum", "BasicFrame_Diamond_01_NoBorder_Plum");
                    if (dia != null) { dia.gameObject.SetActive(g != null); if (g != null) { UiKit.SetSprite(dia, "Icon", GearUi.SetIcon(GearUi.Set(D, g)), Palette.White); var db = dia.GetComponent<Image>() ?? dia.GetComponentInChildren<Image>(); } }
                    var lvl = slot.Find("Text_Level"); if (lvl != null) { lvl.gameObject.SetActive(true); var t = lvl.GetComponent<Text>(); if (t != null) { t.text = $"{GearUi.PartName(D, part)} Lv.{lv}" + (g != null && g.Plus > 0 ? $"  +{g.Plus}" : ""); t.color = Palette.White; } }
                    UiKit.Hide(slot, "Alert_Dot_01_Red");
                    var upOld = slot.Find("Up"); if (upOld != null) UnityEngine.Object.Destroy(upOld.gameObject);
                    if (GearUi.BetterInInv(S, part)) { var up = UiKit.Panel(slot, "Up", "fr.circle", Palette.Green); UiKit.Pct(up.rectTransform, 74, 2, 24, 24); var ut = UiKit.Text(up.transform, "↑", 30, Palette.White); UiKit.Stretch(ut.rectTransform); }
                    string p = part; var gg = g;
                    UiKit.Clickable(slot, () => { if (gg != null) GearUi.OpenDetail(App, gg, Refresh); else GearUi.OpenSlot(App, p, Refresh); });
                }
            }
            // 스탯 · 균등 · 전투력
            var pw = GearSystem.BuildPower(D, S.CurBuild(D));
            if (_atk != null) _atk.text = UiKit.Fmt(Math.Round(pw.Atk)); if (_hp != null) _hp.text = UiKit.Fmt(Math.Round(pw.Hp)); if (_sh != null) _sh.text = UiKit.Fmt(Math.Round(pw.Sh));
            if (_power != null) _power.text = UiKit.Fmt(App.Power());
            int mn = int.MaxValue; foreach (var pt in D.Gear.Parts) mn = Math.Min(mn, S.SlotLv(pt)); if (mn == int.MaxValue) mn = 0;
            double bon = Math.Round((GearSystem.EvenBonus(D, S.CurBuild(D)) - 1) * 100);
            int nextEven = (mn / D.Gear.EvenPer + 1) * D.Gear.EvenPer;
            if (_even != null) _even.text = $"균등 보너스 +{bon}% — 최저 슬롯 Lv.{mn} " + (nextEven > D.Gear.SlotLvMax ? $"(슬롯 상한 Lv.{D.Gear.SlotLvMax} — 최대)" : $"(6슬롯 전부 Lv.{nextEven} 이면 +{bon + D.Gear.EvenStep * 100:0.#}%)");
            // 합성 버튼
            var fkeys = GearUi.FusableKeys(S);
            if (_fuseBtn != null) UiKit.SetText(_fuseBtn, "Text (TMP)", fkeys.Count > 0 ? $"합성 ({fkeys.Count}) !" : "합성");
            // 인벤 격자
            if (_content != null)
            {
                UiKit.Clear(_content);
                if (S.Inv.Count == 0) GearUi.Empty(_content, "장비가 없습니다.\n상점에서 뽑기로 장비를 얻으세요.");
                foreach (var g in GearUi.Sorted(S))
                {
                    var gi = g;
                    GearUi.Cell(_content, D, g, new GearUi.CellOpts { Equipped = S.IsEquipped(g), IsNew = g.IsNew, Fusable = fkeys.Contains(GearUi.Key(g)) }, () => GearUi.OpenDetail(App, gi, Refresh));
                }
            }
            NavBar.Refresh(Root);
        }
    }

    /// <summary>하단 탭 5칸(상점·장비·전투·대장간·설정) — 로비 프리팹의 Tab_01_BottomFlushMenu 를 다른 화면에도 같은 배선으로 세운다.</summary>
    public static class NavBar
    {
        static readonly string[] Keys = { "shop", "gear", "battle", "forge", "settings" };
        static readonly string[] IconsK = { "ui.shop", "ui.bag", "ui.battle", "ui.anvil", "ui.settings" };
        static readonly string[] Labels = { "상점", "장비", "전투", "대장간", "설정" };

        public static void Attach(GameScreen screen, RectTransform root, string current)
        {
            var bar = UiKit.SpawnRt("ui.tabBar", root, Layout.TabBar);
            Wire(screen.App, bar, current);
        }
        public static void Wire(App app, Transform bar, string current)
        {
            for (int i = 0; i < bar.childCount && i < Keys.Length; i++)
            {
                var tab = bar.GetChild(i); int k = i;
                UiKit.SetSprite(tab, "Normal/Icon", IconsK[i], Palette.White); UiKit.SetSprite(tab, "Focus/Icon_Focus", IconsK[i], Palette.White);
                UiKit.SetText(tab, "Focus/Text (TMP)", Labels[i]);
                bool on = Keys[i] == current || (Keys[i] == "battle" && current == "lobby");
                UiKit.Show(tab, "Focus", on); UiKit.Show(tab, "Normal", !on);
                UiKit.Clickable(tab, () =>
                {
                    switch (Keys[k])
                    {
                        case "battle": app.ShowScreen("lobby"); break;
                        case "settings": app.Overlay.Pause(() => { }, () => { }); break;
                        default: if (Keys[k] != current) app.ShowScreen(Keys[k]); break;
                    }
                });
            }
        }
        public static void Refresh(RectTransform root) { var bar = UiKit.Find(root, "ui.tabBar"); if (bar != null) bar.SetAsLastSibling(); }
    }
}
