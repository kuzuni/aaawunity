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

        // ───────────────────────── 장비 세부 팝업 — 레퍼런스 docs/ref/07_gear_detail.jpg 구도(표 ④ · 공통 팝업 문법 UiKit.Popup · T38 · T27 «Character_Hero_Item_Detail_01 그대로» 폐기) ─────────────────────────
        /// <summary>등급 색 이름 → 박스 윗변 등급 탭(Title_01 명판 변형) 키 — gray(일반)는 갈색 명판.</summary>
        static string BadgeKey(string colorName) => colorName == "gray" ? "ui.titleBrown" : "ui.title." + colorName;
        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
        /// <summary>어두운 pill(fr.r12 · 잉크색) + 글자 — 메타줄 «슬롯 Lv. N/최대»·«부위», 스탯 박스, 옵션 줄, 비용 줄이 같은 조각을 쓴다.</summary>
        static RectTransform Pill(RectTransform parent, string name, Layout.R r, float alpha = 0.85f)
        {
            var p = UiKit.Panel(parent, name, "fr.r12", Palette.A(Palette.Ink, alpha)); UiKit.Pct(p.rectTransform, r); return p.rectTransform;
        }
        /// <summary>
        /// 표 ④ 의 공통 뼈대: 어둠 + 패널(GdBox) + 박스 윗변 <b>등급 탭</b>(GdBadge · 등급색 명판) → 왼쪽 <b>아이콘 칸</b>(GdIcon · 장비 칸 Cell «+N» 포함 · 빈 슬롯은 빈 프레임) · 오른쪽 <b>이름 굵게</b>(GdName) + <b>pill 2</b>(GdMeta · «슬롯 Lv. N/최대» · «부위») → «탭하여 닫기»(배경 탭 = 닫기 · 닫기 X 없음).
        /// 돌려주는 box 안에 스탯 박스(GdStats) · 옵션 줄(GdOpts) · 비용 줄(GdCost) · 버튼 2(GdBtnL/R) 를 Pct 로 놓는다(<see cref="OpenDetail"/> · <see cref="OpenSlot"/>).
        /// </summary>
        static RectTransform DetailFrame(App app, string badge, string colorName, GearItem g, string name, Color nameColor, string pill1, string pill2)
        {
            var ov = app.Overlay; var B = Layout.GdBox;
            string bk = BadgeKey(colorName);
            var box = ov.OpenBox("ui.popup", bk, badge, B, () => ov.Close());
            var rib = UiKit.Find(box, bk); if (rib != null) { var rr = (RectTransform)rib; rr.sizeDelta = UiKit.PxSize(Layout.GdBadge) + new Vector2(70, 36); rr.anchoredPosition = new Vector2(0, 6); }   // 등급 탭 = 표 배지 크기(글자 여유만)
            var slot = UiKit.Rect(box, "IconSlot"); UiKit.Pct(slot, Layout.GdIcon.Within(B));
            if (g != null) { var cell = Cell(slot, app.Data, g, new CellOpts(), null); UiKit.Stretch(cell); }
            else { var e = UiKit.Panel(slot, "Empty", "fr.itemBg", Palette.A(Palette.Gray, 0.55f)); UiKit.Stretch(e.rectTransform); }
            var nmR = Layout.GdName.Within(B);
            var nm = UiKit.Label(box, nmR.X, nmR.Y, nmR.W, nmR.H, name, 44, nameColor, TextAnchor.MiddleLeft, true, true); nm.name = "Name"; nm.fontStyle = FontStyle.Bold;
            var meta = Layout.GdMeta.Within(B);
            var p1 = Pill(box, "Pill1", new Layout.R(meta.X, meta.Y, meta.W * 0.47f, meta.H)); var t1 = UiKit.Text(p1, pill1, 28, Palette.Cream, TextAnchor.MiddleCenter, true, false); UiKit.Stretch(t1.rectTransform, 8, 2, 8, 2);
            var p2 = Pill(box, "Pill2", new Layout.R(meta.X + meta.W * 0.53f, meta.Y, meta.W * 0.47f, meta.H)); var t2 = UiKit.Text(p2, pill2, 28, Palette.Cream, TextAnchor.MiddleCenter, true, false); UiKit.Stretch(t2.rectTransform, 8, 2, 8, 2);
            return box;
        }
        /// <summary>스탯 박스(GdStats) — 머리 «스탯» + 줄 3(공격력 · 체력 · 실드 · 값은 초록 «+N»). 빈 슬롯은 안내 한 줄.</summary>
        static void StatsBox(RectTransform box, GameData D, GearItem g, int lv, bool eqd)
        {
            var st = Layout.GdStats.Within(Layout.GdBox);
            var sp = Pill(box, "Stats", st, 0.75f);
            string gh = Hex(Palette.Green);
            UiKit.Label(sp, 3, 2, 60, 26, "스탯", 28, Palette.Cream, TextAnchor.MiddleLeft, true, false).fontStyle = FontStyle.Bold;
            if (g == null) { UiKit.Label(sp, 3, 30, 94, 66, $"슬롯 1레벨당 이 부위 장비의 공격력·체력·실드 +{D.Gear.SlotStep * 100:0.#}% (상한 Lv.{D.Gear.SlotLvMax})", 24, Palette.CreamDark, TextAnchor.MiddleLeft, true, false); return; }
            var c = GearSystem.Contribution(D, g, lv);
            var rows = new (string icon, string label, double v)[] { (Icons.Stat("dmg"), "공격력", c.Atk), ("pi.heart", "체력", c.Hp), ("pi.shield", "실드", c.Sh) };
            for (int i = 0; i < rows.Length; i++)
            {
                float y = 28 + i * 24;
                var ic = UiKit.Icon(sp, "ic", rows[i].icon); UiKit.Pct(ic.rectTransform, 3, y, 6, 22);
                var t = UiKit.Label(sp, 11, y, 86, 22, $"{rows[i].label}  <color=#{gh}>+{UiKit.Fmt(rows[i].v)}</color>", 26, Palette.Cream, TextAnchor.MiddleLeft, true, false); t.name = "Stat:" + i;
            }
            if (eqd) { var s2 = UiKit.Label(box, st.X + st.W * 0.55f, st.Y + st.H * 0.02f, st.W * 0.44f, st.H * 0.26f, $"슬롯 Lv당 +{D.Gear.SlotStep * 100:0.#}%", 22, Palette.CreamDark, TextAnchor.MiddleRight, true, false); s2.name = "SlotHint"; }
        }
        /// <summary>옵션 줄(GdOpts · 줄 피치 ≤ 2.4%) — 해금 = 등급색 세트 아이콘 + 등급색 글자 · 잠금 = 자물쇠 + 흐린 글자 «(등급 이상)». 규칙(OptCount) 은 기존 그대로.</summary>
        static void OptionRows(RectTransform box, GameData D, GearItem g)
        {
            var region = Layout.GdOpts.Within(Layout.GdBox);
            var opts = D.Gear.Options.TryGetValue(g.Type, out var ol) ? ol : new List<GearOption>();
            int n = D.Gear.OptCount(g.Rar, g.Plus); int R = D.Gear.RarName.Length;
            var host = UiKit.Rect(box, "Options"); UiKit.Pct(host, region);
            if (opts.Count == 0) { UiKit.Label(host, 2, 0, 96, 100, "세트 옵션 없음", 24, Palette.InkLight); return; }
            float pitch = Mathf.Min(Layout.GdOptPitch, Layout.GdOpts.H / opts.Count);   // 프레임 % → 줄 하나가 차지하는 비율
            float rowPct = pitch / Layout.GdOpts.H * 100f;
            for (int i = 0; i < opts.Count; i++)
            {
                bool on = i < n; string tier = i < R ? RarName(D, i) : $"신화 +{(i - R + 1) * 3}강";
                var color = i < R ? Palette.ByName(Palette.RarName(i)) : Palette.Plum;
                var row = Pill(host, "Opt:" + i, new Layout.R(0, i * rowPct, 100, rowPct * 0.92f), on ? 0.7f : 0.45f);
                var ic = UiKit.Icon(row, "ic", on ? SetIcon(Set(D, g)) : "ui.iconLock", on ? color : Palette.A(Palette.Gray, 0.9f)); UiKit.Pct(ic.rectTransform, 1.5f, 12, 5, 76);
                var t = UiKit.Label(row, 8, 0, 90, 100, on ? opts[i].Desc : $"{opts[i].Desc}  ({tier} 이상)", 24, on ? color : Palette.A(Palette.Gray, 0.8f), TextAnchor.MiddleLeft, true, false);
                if (!on) { var cg = UiKit.Ensure<CanvasGroup>(row.gameObject); cg.alpha = 0.75f; }
            }
        }
        /// <summary>비용 줄(GdCost) — 🪙 «보유/비용»(보유가 모자라면 빨강 · 충분하면 초록) · MAX 면 «슬롯 MAX (Lv.N)».</summary>
        static void CostRow(RectTransform box, SaveData S, double cost, bool maxed, int maxLv)
        {
            var r = Layout.GdCost.Within(Layout.GdBox);
            var row = Pill(box, "Cost", r, 0.75f);
            var ic = UiKit.Icon(row, "ic", "pi.coins"); UiKit.Pct(ic.rectTransform, 30, 8, 5, 84);
            string s = maxed ? $"슬롯 MAX (Lv.{maxLv})" : $"<color=#{Hex(S.Gold >= cost ? Palette.Green : Palette.Red)}>{UiKit.Fmt(S.Gold)}</color>/{UiKit.Fmt(cost)}";
            var t = UiKit.Label(row, 36, 0, 40, 100, s, 28, Palette.Cream, TextAnchor.MiddleLeft, true, false); t.name = "CostText";
        }
        /// <summary>표 ④ «장비 세부 팝업»: 등급 탭 → 아이콘 칸(+N) · 이름 · «슬롯 Lv. N/최대»·«부위» pill → 스탯 박스(초록 +값) → 옵션 줄(등급색 · 잠금 흐림) → 비용 줄 → 해제/장착(파랑) · 슬롯 강화(주황) → «탭하여 닫기». 규칙·수치는 예전 그대로.</summary>
        public static void OpenDetail(App app, GearItem g, Action onChanged)
        {
            var D = app.Data; var S = app.Save; var ov = app.Overlay;
            if (g == null) { ov.Close(); return; }
            if (g.IsNew) { g.IsNew = false; app.Persist(); onChanged?.Invoke(); }
            int lv = S.SlotLv(g.Part); double cost = D.Gear.SlotCost(lv); bool eqd = S.IsEquipped(g); bool maxed = lv >= D.Gear.SlotLvMax;
            string colorName = Palette.RarName(g.Rar);
            var box = DetailFrame(app, RarName(D, g.Rar), colorName, g, Name(D, g) + (g.Plus > 0 ? " +" + g.Plus : ""), Palette.ByName(colorName), $"슬롯 Lv. {lv}/{D.Gear.SlotLvMax}", PartName(D, g.Part));
            StatsBox(box, D, g, lv, eqd);
            OptionRows(box, D, g);
            CostRow(box, S, cost, maxed, D.Gear.SlotLvMax);
            var B = Layout.GdBox;
            if (eqd) UiKit.Button(box, "ui.btnBlue", "해제", () => { S.Eq.Remove(g.Part); app.Persist(); Audio.Sfx("snd.equip"); ov.Close(); onChanged?.Invoke(); }, Layout.GdBtnL.Within(B)).name = "BtnL";
            else UiKit.Button(box, "ui.btnBlue", "장착", () => { S.Eq[g.Part] = g.Uid; g.IsNew = false; app.Persist(); Audio.Sfx("snd.equip"); ov.Close(); onChanged?.Invoke(); }, Layout.GdBtnL.Within(B)).name = "BtnL";
            var up = UiKit.Button(box, "ui.btnOrange", maxed ? "슬롯 MAX" : "슬롯 강화", () =>
            {
                double c2 = D.Gear.SlotCost(S.SlotLv(g.Part)); if (S.Gold < c2 || S.SlotLv(g.Part) >= D.Gear.SlotLvMax) { app.Toast("골드가 부족합니다"); return; }
                S.Gold -= c2; S.Slots[g.Part] = S.SlotLv(g.Part) + 1; app.Persist(); onChanged?.Invoke(); OpenDetail(app, g, onChanged);
            }, Layout.GdBtnR.Within(B)); up.name = "BtnR";
            UiKit.SetInteractable(up.GetComponent<Button>(), !maxed && S.Gold >= cost);
        }

        /// <summary>빈 부위 팝업 — 같은 구도(장비 없는 상태 · 등급 탭 = «부위 슬롯» · 빈 아이콘 칸 · 스탯 박스에 슬롯 안내 · 옵션 자리에 «장착된 장비가 없습니다» · 비용 줄 · 강화만 · 탭하여 닫기).</summary>
        public static void OpenSlot(App app, string part, Action onChanged)
        {
            var D = app.Data; var S = app.Save; var ov = app.Overlay;
            int lv = S.SlotLv(part); double cost = D.Gear.SlotCost(lv); bool maxed = lv >= D.Gear.SlotLvMax;
            var box = DetailFrame(app, $"{PartName(D, part)} 슬롯", "gray", null, "비어 있음", Palette.InkLight, $"슬롯 Lv. {lv}/{D.Gear.SlotLvMax}", PartName(D, part));
            StatsBox(box, D, null, lv, false);
            var region = Layout.GdOpts.Within(Layout.GdBox);
            UiKit.Label(box, region.X, region.Y, region.W, region.H, "장착된 장비가 없습니다\n인벤에서 이 부위의 장비를 골라 장착하세요", 28, Palette.InkLight, TextAnchor.MiddleCenter, true, false).name = "EmptyHint";
            CostRow(box, S, cost, maxed, D.Gear.SlotLvMax);
            var up = UiKit.Button(box, "ui.btnOrange", maxed ? "슬롯 MAX" : "슬롯 강화", () =>
            {
                double c2 = D.Gear.SlotCost(S.SlotLv(part)); if (S.Gold < c2 || S.SlotLv(part) >= D.Gear.SlotLvMax) { app.Toast("골드가 부족합니다"); return; }
                S.Gold -= c2; S.Slots[part] = S.SlotLv(part) + 1; app.Persist(); onChanged?.Invoke(); OpenSlot(app, part, onChanged);
            }, Layout.GdBtnR.Within(Layout.GdBox)); up.name = "BtnR";
            UiKit.SetInteractable(up.GetComponent<Button>(), !maxed && S.Gold >= cost);
        }
    }
}
