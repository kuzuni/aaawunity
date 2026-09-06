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
        /// <summary>장비 아이콘 = <see cref="GearLook"/> 표의 **아이콘 키**(T31 · 투구·무기·갑옷은 CharacterMaker Thumbnail <c>cmi.gear.*</c> — 입는 파츠 <c>cm.gear.*</c> 와 분리 · 목걸이·장갑·신발은 GUI Pro 아이콘(임시)).</summary>
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
                if (item != null) { item.gameObject.SetActive(g != null); if (g != null) { var im = UiKit.SetSprite(frame, "Item", IconKey(D, g), Palette.White); FitIcon(im, g); } }   // GUI Pro 아이콘은 프리팹 Item 크기 그대로 · 파츠 아이콘은 같은 눈높이로 맞춤(T17)
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

        /// <summary>
        /// 장비 아이콘 맞춤(T17 · 주인 «투구·갑옷·무기 아이콘만 작다»). 프리팹 Item(256×256 · 스케일 0.6149)은 GUI Pro 128px 아이콘용이라 그림이 rect 의 ~85% 를 채우지만,
        /// CharacterMaker 그림은 그림이 캔버스를 채우는 비율이 제각각(입는 파츠 33~70% · T31 의 Thumbnail 128×128 도 여백이 있다) → 같은 rect 에서 작게 보였다.
        /// 파츠 아이콘(T31 부터 Thumbnail)은 스프라이트의 **불투명 bbox**(Tight 메시 정점 · <c>Sprite.vertices</c>)가 칸의 <see cref="GearLook.PartIconFill"/>(72%) 를 채우도록 Item 의 sizeDelta·pivot 을 계산(<see cref="GearLook.FitPartIcon"/>)하고,
        /// 회전은 하지 않는다(주인 2026-09-06 «45° 취소» · 프리팹 회전 그대로). GUI Pro 아이콘(목걸이·장갑·신발)은 프리팹 값으로 되돌린다.
        /// 장착 슬롯·인벤 칸·세부 팝업·대장간·뽑기 결과가 전부 이 함수를 거친다(Cell 과 GearScreen 슬롯). 프리팹 값은 <see cref="PartIconFit"/> 이 처음 한 번 기억해 두고 복원한다(슬롯 Item 재사용).
        /// </summary>
        public static void FitIcon(Image im, GearItem g) => FitIcon(im, g != null && GearLook.HasLook(g.Part));
        public static void FitIcon(Image im, bool isPart)
        {
            if (im == null) return;
            var rt = im.rectTransform; var st = UiKit.Ensure<PartIconFit>(im.gameObject); st.Capture(rt);
            var sp = im.sprite;
            if (!isPart || sp == null) { st.Restore(rt); return; }
            var rect = sp.rect; float ppu = sp.pixelsPerUnit > 0 ? sp.pixelsPerUnit : 100f; var piv = sp.pivot;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            var verts = sp.vertices;   // Tight 메시(파츠 .meta spriteMeshType 1) → 정점의 min/max = 불투명 bbox (rect 왼쪽아래 원점 픽셀)
            if (verts != null && verts.Length >= 3)
                foreach (var v in verts) { float px = v.x * ppu + piv.x, py = v.y * ppu + piv.y; if (px < x0) x0 = px; if (py < y0) y0 = py; if (px > x1) x1 = px; if (py > y1) y1 = py; }
            else { x0 = 0; y0 = 0; x1 = rect.width; y1 = rect.height; }
            var frame = rt.parent as RectTransform;
            float fw = frame != null ? Mathf.Min(frame.rect.width, frame.rect.height) : 0f;
            if (fw <= 0f) fw = CellSize(App.I != null ? App.I.Assets : null);   // 레이아웃 전이면 ListItem_EquipMent 본래 한 변(188)
            var fit = GearLook.FitPartIcon(rect.width, rect.height, x0, y0, x1, y1, fw, GearLook.PartIconFill, rt.localScale.x);
            im.preserveAspect = true;
            rt.pivot = new Vector2((float)fit.PivotX, (float)fit.PivotY);
            rt.sizeDelta = new Vector2((float)fit.W, (float)fit.H);
            rt.anchoredPosition = st.Pos;   // 프리팹 자리(가운데) — pivot 이 그림 가운데라 그림이 칸 가운데에 온다
            rt.localRotation = st.Rot;   // 프리팹 회전 그대로(무기 45° 는 주인이 취소)
        }

        /// <summary>
        /// 인벤 격자 — ScrollRect + GridLayout. 돌려주는 Content 에 Cell 을 채운다.
        /// 격자 값(칸 188×188 · 5열 · 세로 간격 · 위아래 패딩)은 **장비 화면 프리팹(Character_Hero_Equipment) 의 Content GridLayoutGroup 에서 그대로 복사**한다 —
        /// 대장간 칸이 장비 화면 칸(ListItem_EquipMent)과 같은 크기·비례가 되게(T8 · 찌그러짐 0). 가로 간격만 view 폭에 맞춰 5열이 딱 들어가게 다시 계산한다.
        /// (예전엔 ref-layout 표 % 로 18.4×7.2 = 199×168 칸을 만들어 정사각 프리팹이 찌그러졌다.)
        /// </summary>
        public static RectTransform Grid(Transform parent, Layout.R rect, out ScrollRect scroll)
        {
            var view = UiKit.Rect(parent, "InvScroll"); UiKit.Pct(view, rect);
            view.gameObject.AddComponent<RectMask2D>();
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            var content = UiKit.Rect(view, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1); content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            CopyEquipmentGrid(grid, UiKit.FrameW * rect.W / 100f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.childAlignment = TextAnchor.UpperLeft; grid.startCorner = GridLayoutGroup.Corner.UpperLeft; grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content; scroll.viewport = view;
            return content;
        }
        /// <summary>장비 화면 프리팹 Content 의 격자 값을 복사한다(칸·세로 간격·패딩·열 수). 프리팹이 없으면 ListItem_EquipMent 의 본래 크기(정사각)로 · 그것도 없으면 표 % 폭의 정사각. 가로 간격 = (폭 − 열×칸) / (열 − 1).</summary>
        static void CopyEquipmentGrid(GridLayoutGroup grid, float viewW)
        {
            var cat = App.I != null ? App.I.Assets : null;
            GridLayoutGroup src = null;
            var eq = cat != null ? cat.Prefab("ui.equipment") : null;
            if (eq != null) { var c = UiKit.Find(eq.transform, "Content"); if (c != null) src = c.GetComponent<GridLayoutGroup>(); }
            if (src != null)
            {
                grid.cellSize = src.cellSize; grid.spacing = src.spacing; grid.constraintCount = src.constraintCount;
                grid.padding = new RectOffset(src.padding.left, src.padding.right, src.padding.top, src.padding.bottom);
            }
            else
            {
                float s = CellSize(cat);
                grid.cellSize = new Vector2(s, s); grid.spacing = new Vector2(0, UiKit.FrameH * (Layout.GearInvRowPitch - Layout.GearInvCellH) / 100f); grid.constraintCount = Layout.GearInvCols;
                grid.padding = new RectOffset(0, 0, (int)(UiKit.FrameH * (Layout.GearInvCell.Y - Layout.GearInv.Y) / 100f), 0);
            }
            int cols = Mathf.Max(1, grid.constraintCount);
            float gapX = cols > 1 ? (viewW - cols * grid.cellSize.x) / (cols - 1) : 0f;
            grid.spacing = new Vector2(Mathf.Max(0f, gapX), grid.spacing.y);   // 5열이 폭에 딱 — 칸 크기는 절대 줄이지 않는다(비례 고정)
        }
        /// <summary>ListItem_EquipMent 프리팹의 본래 한 변(188). 프리팹이 없으면 ref-layout 표의 칸 폭.</summary>
        public static float CellSize(AssetCatalog cat)
        {
            var cell = cat != null ? cat.Prefab("ui.equipCell") : null; var rt = cell != null ? cell.transform as RectTransform : null;
            return rt != null && rt.sizeDelta.x > 0 ? rt.sizeDelta.x : UiKit.FrameW * Layout.GearInvCellW / 100f;
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
                    UiKit.SetText(b1, "Text (TMP)", "해제"); UiKit.Clickable(b1, () => { S.Eq.Remove(g.Part); app.Persist(); Audio.Sfx("snd.equip"); ov.Close(); onChanged?.Invoke(); });
                    UiKit.SetText(b2, "Text_Title", maxed ? "슬롯 MAX" : "슬롯 강화"); UiKit.SetText(b2, "Text", maxed ? $"Lv.{D.Gear.SlotLvMax}" : UiKit.Fmt(cost));
                    var bb = UiKit.Clickable(b2, () => { double c2 = D.Gear.SlotCost(S.SlotLv(g.Part)); if (S.Gold < c2 || S.SlotLv(g.Part) >= D.Gear.SlotLvMax) { app.Toast("골드가 부족합니다"); return; } S.Gold -= c2; S.Slots[g.Part] = S.SlotLv(g.Part) + 1; app.Persist(); onChanged?.Invoke(); OpenDetail(app, g, onChanged); });
                    UiKit.SetInteractable(bb, !maxed && S.Gold >= cost);
                }
                else
                {
                    UiKit.SetText(b1, "Text (TMP)", "장착"); UiKit.Clickable(b1, () => { S.Eq[g.Part] = g.Uid; g.IsNew = false; app.Persist(); Audio.Sfx("snd.equip"); ov.Close(); onChanged?.Invoke(); });
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
