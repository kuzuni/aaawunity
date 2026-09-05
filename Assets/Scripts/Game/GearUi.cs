using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 장비 그리기 공통 — 인벤 칸(ItemFrame_01_Normal_* 등급색) · 이름 · 정렬 · 합성 가능 키. 장비 탭·대장간·뽑기 결과가 같은 함수를 쓴다
    /// (index.html invCellHTML/invSorted/fusableKeys 와 같은 구조 — 세 화면이 갈라지지 않게).
    /// </summary>
    public static class GearUi
    {
        public static readonly string[] ColLeft = { "weapon", "neck", "armor" }, ColRight = { "helm", "glove", "boot" };   // index.html GEAR_COL

        public static string Name(GameData D, GearItem g) => D.Gear.TypeName.TryGetValue(g.Type, out var n) ? n : g.Type;
        public static string Set(GameData D, GearItem g) => D.Gear.SetOf(g.Type);
        public static string SetLabel(GameData D, GearItem g) => (D.Gear.SetName.TryGetValue(Set(D, g), out var n) ? n : Set(D, g)) + " 세트";
        public static string PartName(GameData D, string part) => D.Gear.PartName.TryGetValue(part, out var n) ? n : part;
        public static string RarName(GameData D, int rar) => rar >= 0 && rar < D.Gear.RarName.Length ? D.Gear.RarName[rar] : rar.ToString();
        /// <summary>장비 아이콘 = 장착 외형과 같은 표(<see cref="GearLook"/>) — 투구·무기·갑옷은 CharacterMaker 파츠 그림, 목걸이·장갑·신발은 GUI Pro 아이콘(임시).</summary>
        public static string IconKey(GameData D, GearItem g) => GearLook.IconKey(D, g);
        public static string SetIcon(string set) => set == "crit" ? "pi.critical" : set == "hpsh" ? "pi.heart" : "ui.dodge";
        public static string Key(GearItem g) => g.Part + "|" + g.Type + "|" + g.Rar;

        /// <summary>인벤 정렬 — 장착분 먼저, 그다음 등급·강화 내림차순, 부위 이름.</summary>
        public static List<GearItem> Sorted(SaveData S)
        {
            var list = new List<GearItem>(S.Inv);
            list.Sort((a, b) =>
            {
                int ea = S.IsEquipped(a) ? 1 : 0, eb = S.IsEquipped(b) ? 1 : 0; if (ea != eb) return eb - ea;
                int sa = GearSystem.GearScore(a), sb = GearSystem.GearScore(b); if (sa != sb) return sb - sa;
                return string.CompareOrdinal(a.Part, b.Part);
            });
            return list;
        }
        /// <summary>같은 부위·종류·등급이 3개 이상인 키.</summary>
        public static HashSet<string> FusableKeys(SaveData S)
        {
            var cnt = new Dictionary<string, int>(); foreach (var g in S.Inv) { var k = Key(g); cnt[k] = (cnt.TryGetValue(k, out var c) ? c : 0) + 1; }
            var set = new HashSet<string>(); foreach (var kv in cnt) if (kv.Value >= 3) set.Add(kv.Key); return set;
        }
        /// <summary>인벤에 더 좋은 게 있다(↑) — 자동 장착은 없고 표시만.</summary>
        public static bool BetterInInv(SaveData S, string part)
        {
            var cur = S.EquippedGear(part); GearItem best = null;
            foreach (var g in S.Inv) { if (g.Part != part || S.IsEquipped(g)) continue; if (best == null || GearSystem.GearScore(g) > GearSystem.GearScore(best)) best = g; }
            return best != null && (cur == null || GearSystem.GearScore(best) > GearSystem.GearScore(cur));
        }

        /// <summary>칸 옵션 — Equipped/Fusable 은 «상태», EquippedMark/FusableDot 은 «표기 켬»(장비 화면은 둘 다 끔 · 대장간(T8)은 켬 — ROUTINE T7.7).</summary>
        public sealed class CellOpts { public bool Equipped, IsNew, Fusable, Selected, Off; public bool EquippedMark, FusableDot; }

        /// <summary>
        /// 장비 칸 하나 = 주인 지정 **ListItem_EquipMent**(카탈로그 <c>ui.equipCell</c> · 188×188) 그대로 — 프리팹 요소를 옮기지 않고 등급색 프레임·아이콘·«+N»·세트 다이아 아이콘만 우리 데이터로 바꾼다.
        /// 크기는 부모가 정한다(격자 188 · 다른 자리는 anchors stretch). 장착중 = 프리팹의 Check(옵션) · 합성 가능 = 오른쪽 위 빨간 점(Alert_Dot_01_Red · 옵션) · NEW = 왼쪽 아래 점.
        /// </summary>
        public static RectTransform Cell(Transform parent, GameData D, GearItem g, CellOpts o, Action onClick)
        {
            o = o ?? new CellOpts();
            var cell = (RectTransform)UiKit.Spawn("ui.equipCell", parent).transform; cell.name = "gear:" + (g != null ? g.Uid.ToString() : "empty");
            var frame = UiKit.Find(cell, "ItemFrame_01");
            if (frame != null)
            {
                var area = UiKit.Find(frame, "NormalArea");
                if (area != null) { UiKit.Clear(area); if (g != null) { var f = UiKit.Spawn("ui.itemFrame." + Palette.RarName(g.Rar), area); UiKit.Stretch((RectTransform)f.transform, -1, -1, -1, -1); } }   // 프리팹의 Normal_Plum 자리(+2px) 에 등급색 변형
                var item = UiKit.Find(frame, "Item");
                if (item != null) { item.gameObject.SetActive(g != null); if (g != null) { var im = UiKit.SetSprite(frame, "Item", IconKey(D, g), Palette.White); if (im != null) im.preserveAspect = true; } }   // 프리팹 Item 크기 그대로
                UiKit.Show(frame, "Add_1", g == null); UiKit.Show(frame, "Add_2", false); UiKit.Show(frame, "Lock", false); UiKit.Show(frame, "Disable", false);
                UiKit.Show(frame, "Focus", g != null && o.Selected);   // 프리팹의 Focus(테두리 글로우) = 선택
            }
            UiKit.SetText(cell, "Text_Level", g != null && g.Plus > 0 ? "+" + g.Plus : "");
            var type = UiKit.Find(cell, "TypeArea");
            if (type != null) { type.gameObject.SetActive(g != null); if (g != null) UiKit.SetSprite(type, "Icon", SetIcon(Set(D, g)), Palette.White); }
            UiKit.Show(cell, "Check", g != null && o.Equipped && o.EquippedMark);
            if (g != null && o.Fusable && o.FusableDot) { var d = UiKit.Spawn("ui.alertDot", cell); var dr = (RectTransform)d.transform; d.name = "FuseDot"; dr.anchorMin = dr.anchorMax = new Vector2(1, 1); dr.pivot = new Vector2(0.5f, 0.5f); dr.anchoredPosition = new Vector2(-14, -14); dr.sizeDelta = new Vector2(47, 47); }
            if (g != null && o.IsNew) { var n = UiKit.Spawn("ui.alertDot", cell); var nr = (RectTransform)n.transform; n.name = "New"; nr.anchorMin = nr.anchorMax = new Vector2(0, 0); nr.pivot = new Vector2(0.5f, 0.5f); nr.anchoredPosition = new Vector2(18, 18); nr.sizeDelta = new Vector2(44, 44); var nt = UiKit.Text(nr, "N", 22, Palette.White); UiKit.Stretch(nt.rectTransform); }
            if (o.Off) { var cg = UiKit.Ensure<CanvasGroup>(cell.gameObject); cg.alpha = 0.4f; }
            if (onClick != null) UiKit.Clickable(cell, onClick);
            return cell;
        }

        /// <summary>인벤 격자 — ScrollRect + GridLayout(5열 · ref-layout GearInvCols). 돌려주는 Content 에 Cell 을 채운다.</summary>
        public static RectTransform Grid(Transform parent, Layout.R rect, out ScrollRect scroll)
        {
            var view = UiKit.Rect(parent, "InvScroll"); UiKit.Pct(view, rect);
            view.gameObject.AddComponent<RectMask2D>();
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            var content = UiKit.Rect(view, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1); content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            // 표 ③⑥: 칸 18.4×7.2 · 간격 0.6 · 행 피치 7.6 (장비 탭과 대장간이 같은 격자)
            grid.cellSize = new Vector2(UiKit.FrameW * Layout.GearInvCellW / 100f, UiKit.FrameH * Layout.GearInvCellH / 100f);
            grid.spacing = new Vector2(UiKit.FrameW * Layout.GearInvGap / 100f, UiKit.FrameH * (Layout.GearInvRowPitch - Layout.GearInvCellH) / 100f);
            grid.padding = new RectOffset(0, 0, (int)(UiKit.FrameH * (Layout.GearInvCell.Y - Layout.GearInv.Y) / 100f), 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = Layout.GearInvCols; grid.childAlignment = TextAnchor.UpperLeft;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content; scroll.viewport = view;
            return content;
        }
        public static void Empty(Transform content, string msg)
        {
            var t = UiKit.Text(content, msg, 30, Palette.InkLight, TextAnchor.MiddleCenter);
            var le = t.gameObject.AddComponent<LayoutElement>(); le.ignoreLayout = true;
            t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1); t.rectTransform.pivot = new Vector2(0.5f, 1); t.rectTransform.anchoredPosition = new Vector2(0, -20); t.rectTransform.sizeDelta = new Vector2(0, 160);
        }

        // ───────────────────────── 장비 세부 팝업 (주인 지정 Character_Hero_Item_Detail_01 · 참고 장비 세부팝업.jpg) ─────────────────────────
        public static void OpenDetail(App app, GearItem g, Action onChanged)
        {
            var D = app.Data; var S = app.Save; var ov = app.Overlay;
            if (g == null) { ov.Close(); return; }
            if (g.IsNew) { g.IsNew = false; app.Persist(); onChanged?.Invoke(); }
            int lv = S.SlotLv(g.Part); double cost = D.Gear.SlotCost(lv); bool eqd = S.IsEquipped(g); bool maxed = lv >= D.Gear.SlotLvMax;
            var root = ov.OpenPrefab("ui.itemDetail"); var rt = (RectTransform)root.transform;
            var popup = UiKit.Find(rt, "Popup"); if (popup != null) UiKit.Pct((RectTransform)popup, Layout.GdBox);
            var B = Layout.GdBox;
            // 등급 배지 — Label_Tapered_01 의 Bg 를 등급색으로
            var badge = UiKit.FindAny(rt, "Label_Tapered_01_Plum", "Label_Tapered_01");
            if (badge != null) { var br = (RectTransform)badge; UiKit.Pct(br, Layout.GdBadge.Within(B)); UiKit.SetText(badge, "Text (TMP)", RarName(D, g.Rar)); UiKit.SetSprite(badge, "Bg", null, Palette.ByName(Palette.RarName(g.Rar))); UiKit.SetSprite(badge, "Deco", null, Palette.A(Palette.White, 0.25f)); }
            var slot = UiKit.Find(rt, "Slot");
            if (slot != null) { UiKit.Pct((RectTransform)slot, Layout.GdIcon.Within(B)); UiKit.Clear(slot); Cell(slot, D, g, new CellOpts(), null); }
            var nm = UiKit.SetText(rt, "Text_ItemName", Name(D, g) + (g.Plus > 0 ? " +" + g.Plus : ""), Palette.ByName(Palette.RarName(g.Rar)));
            if (nm != null) { UiKit.Pct(nm.rectTransform, Layout.GdName.Within(B)); nm.alignment = TextAnchor.MiddleLeft; nm.resizeTextForBestFit = true; nm.resizeTextMaxSize = 48; }
            var meta = UiKit.SetText(rt, "Text_Level", $"{PartName(D, g.Part)} · {SetLabel(D, g)} · 슬롯 Lv.{lv}", Palette.InkSoft, 28);
            if (meta != null) { UiKit.Pct(meta.rectTransform, Layout.GdMeta.Within(B)); meta.alignment = TextAnchor.MiddleLeft; meta.resizeTextForBestFit = true; meta.resizeTextMaxSize = 28; }
            UiKit.Hide(rt, "Slider_Upgrade_01", "Text_GearStats", "LineFrame_01_s_Brown");
            var list = UiKit.Find(rt, "Group_Buff");
            if (list != null)
            {
                // 스탯 섹션(y39.5 h9.5) + 옵션 목록(y48 h14) 을 한 목록으로 — 줄 피치 2.4
                var region = new Layout.R(Layout.GdStats.X, Layout.GdStats.Y, Layout.GdStats.W, Layout.GdOpts.Y + Layout.GdOpts.H - Layout.GdStats.Y);
                UiKit.Pct((RectTransform)list, region.Within(B));
                var tpl = list.childCount > 0 ? list.GetChild(0).gameObject : null;
                var rows = new List<GameObject>(); for (int i = 0; i < list.childCount; i++) rows.Add(list.GetChild(i).gameObject);
                var c = GearSystem.Contribution(D, g, lv);
                var lines = new List<(string, Color)>
                {
                    ($"공격력 +{UiKit.Fmt(c.Atk)}    체력 +{UiKit.Fmt(c.Hp)}    실드 +{UiKit.Fmt(c.Sh)}", Palette.Ink),
                };
                if (eqd) lines.Add(($"슬롯 1레벨당 이 부위 장비의 공격력·체력·실드 +{D.Gear.SlotStep * 100:0.#}% (상한 Lv.{D.Gear.SlotLvMax})", Palette.InkLight));
                var opts = D.Gear.Options.TryGetValue(g.Type, out var ol) ? ol : new List<GearOption>();
                int n = D.Gear.OptCount(g.Rar, g.Plus); int R = D.Gear.RarName.Length;
                lines.Add(($"세트 옵션 ({n}/{opts.Count} 해금)", Palette.Ink));
                for (int i = 0; i < opts.Count; i++)
                {
                    bool on = i < n; string need = i < R ? $"{RarName(D, i)} 이상" : $"신화 +{(i - R + 1) * 3}강";
                    lines.Add(((on ? "◆ " : "🔒 ") + opts[i].Desc + (on ? "" : $"  ({need})"), on ? Palette.InkSoft : Palette.A(Palette.InkLight, 0.7f)));
                }
                if (eqd) { var costTxt = UiKit.Label(popup != null ? (RectTransform)popup : rt, 0, 0, 0, 0, $"슬롯 강화 Lv.{lv} → {(maxed ? "MAX" : "Lv." + (lv + 1))}   {(maxed ? $"상한 Lv.{D.Gear.SlotLvMax}" : "🪙 " + UiKit.Fmt(cost))}", 28, Palette.Orange, TextAnchor.MiddleLeft, true, false); UiKit.Pct(costTxt.rectTransform, Layout.GdCost.Within(B)); }
                float rowH = UiKit.FrameH * Layout.GdOptPitch / 100f;
                for (int i = 0; i < lines.Count; i++)
                {
                    GameObject row = i < rows.Count ? rows[i] : (tpl != null ? UnityEngine.Object.Instantiate(tpl, list) : null);
                    if (row == null) break;
                    row.SetActive(true);
                    var txt = UiKit.SetText(row.transform, "Text_Buff", lines[i].Item1, lines[i].Item2, 24);
                    if (txt != null) { txt.resizeTextForBestFit = true; txt.resizeTextMinSize = 12; txt.resizeTextMaxSize = 24; txt.horizontalOverflow = HorizontalWrapMode.Wrap; txt.verticalOverflow = VerticalWrapMode.Truncate; }
                    var le = UiKit.Ensure<LayoutElement>(row); le.preferredHeight = rowH; le.minHeight = rowH;
                }
                for (int i = lines.Count; i < rows.Count; i++) rows[i].SetActive(false);
                var vl = list.GetComponent<VerticalLayoutGroup>(); if (vl != null) { vl.spacing = 0; vl.padding = new RectOffset(0, 0, 0, 0); vl.childForceExpandHeight = false; vl.childControlHeight = true; vl.childAlignment = TextAnchor.UpperLeft; }
            }
            var btns = UiKit.Find(rt, "Group_Buttons");
            if (btns != null && btns.childCount >= 2)
            {
                foreach (var lg in btns.GetComponents<LayoutGroup>()) lg.enabled = false;
                UiKit.Pct((RectTransform)btns, Layout.GdBtns.Within(B));
                var b1 = btns.GetChild(0); var b2 = btns.GetChild(1);
                var l = Layout.GdBtnL.Within(Layout.GdBtns); var r = Layout.GdBtnR.Within(Layout.GdBtns);
                UiKit.Pct((RectTransform)b1, l.X, 0, l.W, 100); UiKit.Pct((RectTransform)b2, r.X, 0, r.W, 100);
                if (eqd)
                {
                    UiKit.SetText(b1, "Text (TMP)", "해제"); UiKit.Clickable(b1, () => { S.Eq.Remove(g.Part); app.Persist(); ov.Close(); onChanged?.Invoke(); });
                    UiKit.SetText(b2, "Text_Title", maxed ? "슬롯 MAX" : "슬롯 강화"); UiKit.SetText(b2, "Text", maxed ? $"Lv.{D.Gear.SlotLvMax}" : UiKit.Fmt(cost));
                    var bb = UiKit.Clickable(b2, () => { double c2 = D.Gear.SlotCost(S.SlotLv(g.Part)); if (S.Gold < c2 || S.SlotLv(g.Part) >= D.Gear.SlotLvMax) { app.Toast("골드가 부족합니다"); return; } S.Gold -= c2; S.Slots[g.Part] = S.SlotLv(g.Part) + 1; app.Persist(); onChanged?.Invoke(); OpenDetail(app, g, onChanged); });
                    UiKit.SetInteractable(bb, !maxed && S.Gold >= cost);
                }
                else
                {
                    UiKit.SetText(b1, "Text (TMP)", "장착"); UiKit.Clickable(b1, () => { S.Eq[g.Part] = g.Uid; g.IsNew = false; app.Persist(); ov.Close(); onChanged?.Invoke(); });
                    b2.gameObject.SetActive(false);
                }
            }
            var close = UiKit.Find(rt, "Button_Close_01");
            if (close != null) { close.SetParent(rt, false); UiKit.Pct((RectTransform)close, 45, Layout.GdClose.Y - 2.5f, 10, Layout.GdClose.H + 3f); UiKit.Clickable(close, () => ov.Close()); }   // 닫기는 상자 밖 y91.5
        }

        /// <summary>빈 부위 팝업 — 슬롯 강화만.</summary>
        public static void OpenSlot(App app, string part, Action onChanged)
        {
            var D = app.Data; var S = app.Save; var ov = app.Overlay;
            int lv = S.SlotLv(part); double cost = D.Gear.SlotCost(lv); bool maxed = lv >= D.Gear.SlotLvMax;
            var box = ov.OpenBox("ui.popup", "ui.title.tangerine", $"{PartName(D, part)} 슬롯", new Layout.R(8, 32, 84, 34));
            UiKit.Label(box, 6, 12, 88, 10, "장착된 장비가 없습니다", 32, Palette.InkSoft);
            UiKit.Label(box, 6, 26, 88, 12, $"슬롯 강화 Lv.{lv} → {(maxed ? "MAX" : "Lv." + (lv + 1))}   {(maxed ? $"상한 Lv.{D.Gear.SlotLvMax}" : "🪙 " + UiKit.Fmt(cost))}", 30, Palette.Orange);
            UiKit.Label(box, 6, 40, 88, 14, $"슬롯 1레벨당 이 부위 장비의 공격력·체력·실드 +{D.Gear.SlotStep * 100:0.#}% (상한 Lv.{D.Gear.SlotLvMax})", 24, Palette.InkLight);
            var up = UiKit.Button(box, "ui.btnGreen", maxed ? "슬롯 MAX" : $"슬롯 강화 🪙{UiKit.Fmt(cost)}", () =>
            {
                double c2 = D.Gear.SlotCost(S.SlotLv(part)); if (S.Gold < c2 || S.SlotLv(part) >= D.Gear.SlotLvMax) { app.Toast("골드가 부족합니다"); return; }
                S.Gold -= c2; S.Slots[part] = S.SlotLv(part) + 1; app.Persist(); onChanged?.Invoke(); OpenSlot(app, part, onChanged);
            }, new Layout.R(10, 58, 52, 18));
            UiKit.SetInteractable(up.GetComponent<Button>(), !maxed && S.Gold >= cost);
            UiKit.Button(box, "ui.btnGray", "닫기", () => ov.Close(), new Layout.R(66, 58, 26, 18));
        }
    }
}
