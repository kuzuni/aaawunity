using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 상점 = 주인 지정 GUI Pro 데모 프리팹 **Shop_List 그대로**(T9 · 주인 지시 2026-09-05 «쓰라니까 다 바꿔버리네» — 스크롤·섹션·비율 그대로).
    /// 프리팹 요소를 옮기거나 지우지 않고 글자·그림·개수만 바꾼다:
    /// ● <b>ListItem_ShopPackage ×3</b>(프리팹의 2개 + 복제 1개) = 뽑기 상자 3종(gacha.json 순서) — 이름 · 3칸 = 상위 등급 확률 · 배지 = 천장 · 1회/10회(프리팹 Button_Price 와 그 복제 — «그대로» 원칙의 예외).
    /// ● Title_DailyDeals(+Timer_01 = 자정까지) + Group_Item 의 ListItem_ShopItem = 일일 무료 보급(gacha.json economy.dailyGem) · 추가 보급(잠김 · index.html 그대로).
    /// ● Title_Gem + Group_Gem1/Gem2 = <b>다이아 6종</b>(shop.json gemPacks · ₩ 모의 결제 · 누르면 바로 지급) · Title_Gold + Group_Gold = <b>골드 3종</b>(goldPacks · 다이아 소모) — 칸은 전부 <b>ListItem_ShopItem</b>.
    /// ● 하단 Tab_02_BoxMenu_Text 3칸 = 뽑기·다이아·골드(그 섹션으로 스크롤) · Tab_01_BottomFlushMenu = NavBar · 상단 ResourceBar_Group = 골드·보석.
    /// ● 끄는 것: Group_Chest(ListItem_ShopChest ×3) · Title_Silver/Group_Silver(이 게임에 없는 상품). 뽑기 결과 = <b>Shop_Chest_Open 그대로</b>(<see cref="Pull"/>). 자동 장착 없음 — 인벤에만 담긴다(NEW 뱃지).
    /// </summary>
    public sealed class ShopScreen : GameScreen
    {
        public override string Name => "shop";
        RectTransform _rt, _content; ScrollRect _scroll;
        Text _gold, _gem, _timer, _freeTxt, _freeSub; Button _free;
        Transform _secGem, _secGold;
        readonly Dictionary<string, BoxWidgets> _box = new Dictionary<string, BoxWidgets>();
        readonly List<(Button btn, Func<bool> can)> _gated = new List<(Button, Func<bool>)>();
        readonly List<Transform> _subTabs = new List<Transform>();
        float _timerT;
        sealed class BoxWidgets { public Button One, Ten; public Text Title; }

        static string Today() => DateTime.Now.ToString("yyyy-MM-dd");
        static bool CanFree(SaveData S) => S.FreeDay != Today();

        protected override void Build()
        {
            var D = App.Data;
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Palette.Hex("#EBDEC0"); bg.raycastTarget = true;
            var root = UiKit.Spawn("ui.shopList", Root); _rt = (RectTransform)root.transform; UiKit.Stretch(_rt);   // 프리팹 통째로(하단 탭 바까지 프리팹 것) — 내부 요소는 프리팹 값 그대로
            // 상단 재화 = 프리팹 ResourceBar_Group(골드 · 보석 · 세 번째 GemStone 은 이 게임에 없다 — 로비와 같게 끔)
            var res = UiKit.Find(_rt, "ResourceBar_Group");
            if (res != null) { UiKit.Hide(res, "ResourceBar_GemStone"); _gold = UiKit.SetText(res, "ResourceBar_Coin/Text (TMP)", "0"); _gem = UiKit.SetText(res, "ResourceBar_Gem/Text (TMP)", "0"); }
            // 스크롤 = 프리팹 ScrollRect/Viewport/Content 그대로
            _scroll = _rt.GetComponentInChildren<ScrollRect>(true);
            if (_scroll != null) { _scroll.scrollSensitivity = 40; var vp = _scroll.viewport != null ? _scroll.viewport.GetComponent<Image>() : null; if (vp != null) vp.raycastTarget = true; _content = _scroll.content; }
            if (_content == null) _content = UiKit.Find(_rt, "Content") as RectTransform;
            var top = UiKit.Find(_content, "Top"); if (top != null) UiKit.SetText(top, "Text_Title", "상점");
            // ① 뽑기 상자 3종 = ListItem_ShopPackage — 프리팹에 2개뿐이라 마지막 것을 복제해 3개(«그대로» 원칙의 예외 · PROGRESS 기록)
            var pkgs = new List<Transform>();
            if (_content != null) for (int i = 0; i < _content.childCount; i++) { var c = _content.GetChild(i); if (c.name.StartsWith("ListItem_ShopPackage")) pkgs.Add(c); }
            int need = D.Gacha.Boxes.Count;
            while (pkgs.Count > 0 && pkgs.Count < need) { var src = pkgs[pkgs.Count - 1]; var dup = UnityEngine.Object.Instantiate(src.gameObject, src.parent, false); dup.name = src.name + " (copy)"; dup.transform.SetSiblingIndex(src.GetSiblingIndex() + 1); pkgs.Add(dup.transform); }
            for (int i = 0; i < pkgs.Count; i++) { if (i < need) { pkgs[i].gameObject.SetActive(true); BindBox(pkgs[i], D.Gacha.Boxes[i]); } else pkgs[i].gameObject.SetActive(false); }
            // ② 일일 무료 보급 = Title_DailyDeals(+Timer_01) · Group_Item 의 칸(ListItem_ShopItem) = 무료 보급 · 추가 보급(잠김 · index.html «준비 중») · 나머지 칸은 끔
            var daily = UiKit.Find(_content, "Title_DailyDeals");
            if (daily != null) { UiKit.SetText(daily, "Text (TMP)", "일일 무료 보급"); _timer = UiKit.SetText(daily, "Timer_01/Text (TMP)", ""); }
            var grpItem = UiKit.Find(_content, "Group_Item");
            if (grpItem != null)
            {
                var cells = Children(grpItem);
                if (cells.Count > 0) { var c = cells[0]; _free = BindItem(c, "무료 보급", "hud.gem", UiKit.FmtQty(D.Gacha.DailyGem), "", null, "수령", OnFree); _freeTxt = PriceText(c); var lim = UiKit.Find(c, "Text_Limit"); _freeSub = lim != null ? lim.GetComponent<Text>() : null; }
                if (cells.Count > 1) { var b = BindItem(cells[1], "추가 보급", "pi.lock", "", "준비 중", null, "잠김", null); UiKit.SetInteractable(b, false); }
                for (int i = 2; i < cells.Count; i++) cells[i].gameObject.SetActive(false);
            }
            UiKit.Hide(_content, "Group_Chest", "Title_Silver", "Group_Silver");   // 이 게임에 없는 상품 줄(상자 칸은 위 ShopPackage 가 맡는다)
            // ③ 다이아 6종 = Title_Gem + Group_Gem1/Gem2 · ④ 골드 3종 = Title_Gold + Group_Gold — 프리팹의 ListItem_ShopGem/ShopGold 는 끄고 같은 자리에 ListItem_ShopItem(주인 지정 칸)
            var gems = D.Shop != null ? D.Shop.GemPacks : new List<ShopData.GemPack>();
            var golds = D.Shop != null ? D.Shop.GoldPacks : new List<ShopData.GoldPack>();
            _secGem = UiKit.Find(_content, "Title_Gem"); if (_secGem != null) { UiKit.SetText(_secGem, "Text (TMP)", "다이아 (모의 결제 — 실결제 없음)"); _secGem.gameObject.SetActive(gems.Count > 0); }
            FillGroup(UiKit.Find(_content, "Group_Gem1"), gems.Count, 0, (cell, i) => BindGemPack(cell, gems[i]));
            FillGroup(UiKit.Find(_content, "Group_Gem2"), gems.Count, 3, (cell, i) => BindGemPack(cell, gems[i]));
            _secGold = UiKit.Find(_content, "Title_Gold"); if (_secGold != null) { UiKit.SetText(_secGold, "Text (TMP)", "골드 (다이아 소모)"); _secGold.gameObject.SetActive(golds.Count > 0); }
            FillGroup(UiKit.Find(_content, "Group_Gold"), golds.Count, 0, (cell, i) => BindGoldPack(cell, golds[i]));
            // 하단 소탭 3칸(Tab_02_BoxMenu_Text · 데모 Special/Deal/Resources) = 뽑기 · 다이아 · 골드 — 누르면 그 섹션으로 스크롤
            var sub = UiKit.Find(_rt, "Tab_02_BoxMenu_Text");
            if (sub != null)
            {
                string[] labels = { "뽑기", "다이아", "골드" };
                for (int i = 0; i < sub.childCount && i < labels.Length; i++)
                {
                    var tab = sub.GetChild(i); int k = i; UiKit.SetText(tab, "Text (TMP)", labels[i]); _subTabs.Add(tab);
                    UiKit.Clickable(tab, () => { SelectSub(k); ScrollTo(k == 1 ? _secGem : k == 2 ? _secGold : null); });
                }
                SelectSub(0);
            }
            // 하단 탭 5칸 = 프리팹 Tab_01_BottomFlushMenu 그대로(상점 · 장비 · 전투 · 탤런트 · 펫 — T10 배선)
            var tabs = UiKit.Find(_rt, "Tab_01_BottomFlushMenu"); if (tabs != null) NavBar.Wire(App, tabs, "shop");
        }

        static List<Transform> Children(Transform t) { var l = new List<Transform>(); if (t != null) for (int i = 0; i < t.childCount; i++) l.Add(t.GetChild(i)); return l; }

        /// <summary>상품 줄(HorizontalLayoutGroup · 프리팹 칸 3개)을 우리 상품으로 — 프리팹 칸은 끄고 같은 줄에 ListItem_ShopItem 을 필요한 만큼(최대 3) 세운다. 상품이 없으면 줄을 끈다.</summary>
        void FillGroup(Transform group, int count, int offset, Action<Transform, int> bind)
        {
            if (group == null) return;
            foreach (var c in Children(group)) c.gameObject.SetActive(false);
            int n = Mathf.Clamp(count - offset, 0, 3);
            group.gameObject.SetActive(n > 0);
            for (int i = 0; i < n; i++) { var cell = UiKit.Spawn("ui.shopItem", group); cell.name = "ui.shopItem:" + (offset + i); bind(cell.transform, offset + i); }
        }

        // ───────────────────────── 뽑기 상자 (ListItem_ShopPackage) ─────────────────────────
        void BindBox(Transform pkg, GachaBox box)
        {
            var D = App.Data; var w = new BoxWidgets();
            w.Title = UiKit.SetText(pkg, "Text_Title", box.Name);
            if (w.Title != null) { w.Title.alignment = TextAnchor.MiddleLeft; w.Title.resizeTextForBestFit = false; w.Title.fontSize = 40; w.Title.horizontalOverflow = HorizontalWrapMode.Overflow; w.Title.verticalOverflow = VerticalWrapMode.Overflow; }
            // 3칸(Group_Items/List) = 상위 등급 확률 — index.html gachaRateText 순서(높은 등급부터 · 0% 등급은 안 적는다) · 칸이 3개라 상위 3등급까지
            var rates = new List<(int rar, double rate)>(); for (int i = box.Rate.Length - 1; i >= 0; i--) if (box.Rate[i] > 0) rates.Add((i, box.Rate[i]));
            var cells = Children(UiKit.Find(pkg, "Group_Items"));
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i]; bool on = i < rates.Count; cell.gameObject.SetActive(on); if (!on) continue;
                UiKit.SetSprite(cell, "Icon", "hud.gradeGem", Palette.ByName(Palette.RarName(rates[i].rar))); UiKit.Show(cell, "Icon_Infinity", false);
                var t = UiKit.SetText(cell, "Text (TMP)", $"{GearUi.RarName(D, rates[i].rar)} {rates[i].rate:0.#}%");
                if (t != null) { t.gameObject.SetActive(true); t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = 26; t.horizontalOverflow = HorizontalWrapMode.Wrap; }
            }
            // 배지(Badge · 데모 «BEST») = 천장 — 상자마다 있는 천장만(index.html pityText 규약 · 희귀 상자는 없어 배지를 끈다)
            var badge = UiKit.Find(pkg, "Badge");
            if (badge != null)
            {
                int pity = box.PityMyth > 0 ? box.PityMyth : box.PityLegend;
                badge.gameObject.SetActive(pity > 0);
                if (pity > 0) { var bt = badge.GetComponentInChildren<Text>(true); if (bt != null) { bt.text = $"천장\n{pity}회"; bt.resizeTextForBestFit = true; bt.resizeTextMinSize = 12; bt.resizeTextMaxSize = 30; bt.horizontalOverflow = HorizontalWrapMode.Wrap; } }
            }
            // 1회 · 10회 — 프리팹 Button_Price(오른쪽) = 10회, 그 복제(왼쪽) = 1회 (index.html 순서 1회 → 10회)
            var price = UiKit.Find(pkg, "Button_Price") as RectTransform; string key = box.Key;
            if (price != null)
            {
                var one = UnityEngine.Object.Instantiate(price.gameObject, price.parent, false); one.name = "Button_Price1"; var oneRt = (RectTransform)one.transform;
                oneRt.anchoredPosition = price.anchoredPosition - new Vector2(price.sizeDelta.x + 12f, 0);
                PriceLabel(oneRt, $"1회 💎{UiKit.FmtQty(box.Cost)}"); PriceLabel(price, $"10회 💎{UiKit.FmtQty(box.Cost * D.Gacha.TenPullCount)}");
                w.One = UiKit.Clickable(oneRt, () => Pull(1, key)); w.Ten = UiKit.Clickable(price, () => Pull(D.Gacha.TenPullCount, key));
            }
            _box[box.Key] = w;
        }
        static void PriceLabel(Transform btn, string s) { var t = btn.GetComponentInChildren<Text>(true); if (t != null) { t.text = s; t.resizeTextForBestFit = true; t.resizeTextMinSize = 16; t.resizeTextMaxSize = 40; t.horizontalOverflow = HorizontalWrapMode.Wrap; } }

        static string PityText(GachaBox box, GachaState st)
        {
            var o = new List<string>();
            if (box.PityMyth > 0) o.Add($"신화 확정 {Math.Max(0, box.PityMyth - st.P50)}회");
            if (box.PityLegend > 0) o.Add($"전설 확정 {Math.Max(0, box.PityLegend - st.P10)}회");
            o.Add($"누적 {st.Pulls}회");
            return string.Join(" · ", o);
        }

        // ───────────────────────── 상품 칸 (ListItem_ShopItem) ─────────────────────────
        /// <summary>칸 하나 — Text_Title 상품명 · Icon 재화 그림 · Text_ItemNum 수량 · Text_Limit 부제 · Button_Price(GroupArea Icon + Text) 가격. priceIconKey 가 null 이면 가격 아이콘을 끈다(₩ · «수령»).</summary>
        Button BindItem(Transform cell, string title, string iconKey, string num, string sub, string priceIconKey, string price, Action onClick)
        {
            UiKit.SetText(cell, "Text_Title", title);
            var im = UiKit.SetSprite(cell, "Icon", iconKey, Palette.White); if (im != null) im.preserveAspect = true;
            UiKit.SetText(cell, "Text_ItemNum", num);
            UiKit.SetText(cell, "Text_Limit", sub);
            var btn = UiKit.Find(cell, "Button_Price"); if (btn == null) return null;
            var pi = UiKit.Find(btn, "GroupArea/Group/Icon"); if (pi != null) { pi.gameObject.SetActive(priceIconKey != null); if (priceIconKey != null) UiKit.SetSprite(btn, "GroupArea/Group/Icon", priceIconKey, Palette.White); }
            var pt = PriceText(cell); if (pt != null) { pt.text = price; pt.resizeTextForBestFit = true; pt.resizeTextMinSize = 16; pt.resizeTextMaxSize = 44; pt.horizontalOverflow = HorizontalWrapMode.Overflow; }
            var inner = UiKit.Find(btn, "Button_02_Yellow"); if (inner != null) { var it = inner.Find("Text (TMP)"); if (it != null) it.gameObject.SetActive(false); }   // 버튼 프리팹 자체의 «Button» 글자 — 값은 GroupArea 의 글자가 맡는다
            return UiKit.Clickable(btn, onClick ?? (() => { }));
        }
        static Text PriceText(Transform cell) { var t = UiKit.Find(cell, "GroupArea/Group/Text (TMP)"); return t != null ? t.GetComponent<Text>() : null; }

        void BindGemPack(Transform cell, ShopData.GemPack p)
        {
            BindItem(cell, "다이아", "hud.gem", UiKit.FmtQty(p.Gem), "모의 결제", null, $"₩{p.Won:#,0}", () =>
            {
                App.Save.Gem += p.Gem; App.Persist(); Refresh(); App.Toast($"💎 {UiKit.FmtQty(p.Gem)} 지급 (모의 결제)");
            });
        }
        void BindGoldPack(Transform cell, ShopData.GoldPack p)
        {
            var b = BindItem(cell, "골드", "hud.gold", UiKit.FmtQty(p.Gold), "다이아로 구매", "hud.gem", UiKit.FmtQty(p.Gem), () =>
            {
                var S = App.Save; if (S.Gem < p.Gem) { App.Toast("💎 다이아가 부족합니다"); return; }
                S.Gem -= p.Gem; S.Gold += p.Gold; App.Persist(); Refresh(); App.Toast($"골드 {UiKit.Fmt(p.Gold)} 구매!");
            });
            if (b != null) _gated.Add((b, () => App.Save.Gem >= p.Gem));
        }

        // ───────────────────────── 소탭 · 스크롤 ─────────────────────────
        void SelectSub(int k) { for (int i = 0; i < _subTabs.Count; i++) { UiKit.Show(_subTabs[i], "Focus", i == k); UiKit.Show(_subTabs[i], "Normal_01", i != k); } }
        /// <summary>섹션 제목이 뷰포트 위에 오게 스크롤. null = 맨 위(뽑기 상자).</summary>
        void ScrollTo(Transform target)
        {
            if (_scroll == null || _content == null) return;
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            var t = target as RectTransform;
            if (t == null || _scroll.viewport == null) { _scroll.verticalNormalizedPosition = 1f; return; }
            float fromTop = -(_content.InverseTransformPoint(t.position).y + t.rect.height * (1f - t.pivot.y));   // Content 위 끝(pivot y=1) 에서 제목 위 끝까지
            float max = _content.rect.height - _scroll.viewport.rect.height;
            _scroll.verticalNormalizedPosition = max > 0 ? 1f - Mathf.Clamp01(fromTop / max) : 1f;
        }

        // ───────────────────────── 갱신 ─────────────────────────
        public override void Refresh()
        {
            var D = App.Data; var S = App.Save;
            if (_gold != null) _gold.text = UiKit.Fmt(S.Gold);
            if (_gem != null) _gem.text = UiKit.Fmt(S.Gem);
            bool canFree = CanFree(S); UiKit.SetInteractable(_free, canFree);
            if (_freeTxt != null) _freeTxt.text = canFree ? "수령" : "완료";
            if (_freeSub != null) _freeSub.text = canFree ? "오늘 수령 가능" : "내일 다시";
            foreach (var box in D.Gacha.Boxes)
            {
                if (!_box.TryGetValue(box.Key, out var w)) continue;
                if (!S.GachaBoxes.TryGetValue(box.Key, out var st)) { st = new GachaState(); S.GachaBoxes[box.Key] = st; }
                if (w.Title != null) w.Title.text = $"{box.Name}\n<size=20>{PityText(box, st)}</size>";
                UiKit.SetInteractable(w.One, S.Gem >= box.Cost); UiKit.SetInteractable(w.Ten, S.Gem >= box.Cost * D.Gacha.TenPullCount);
            }
            foreach (var g in _gated) UiKit.SetInteractable(g.btn, g.can());
            UpdateTimer();
        }
        public override void Tick(float dt) { _timerT += dt; if (_timerT >= 1f) { _timerT = 0f; UpdateTimer(); } }
        /// <summary>Timer_01 = 다음 무료 보급(자정)까지 남은 시간 · 수령 전이면 «지금 수령 가능».</summary>
        void UpdateTimer()
        {
            if (_timer == null) return;
            if (CanFree(App.Save)) { _timer.text = "지금 수령 가능"; return; }
            var left = DateTime.Today.AddDays(1) - DateTime.Now; if (left.Ticks < 0) left = TimeSpan.Zero;
            _timer.text = $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
        }

        void OnFree()
        {
            var S = App.Save; if (!CanFree(S)) return;
            S.Gem += App.Data.Gacha.DailyGem; S.FreeDay = Today(); App.Persist(); Refresh(); App.Toast($"💎 {UiKit.FmtQty(App.Data.Gacha.DailyGem)} 수령!");
        }

        // ───────────────────────── 뽑기 → 결과 팝업 = Shop_Chest_Open 그대로 ─────────────────────────
        void Pull(int n, string boxKey)
        {
            var D = App.Data; var S = App.Save;
            GachaBox box = null; foreach (var b in D.Gacha.Boxes) if (b.Key == boxKey) box = b; if (box == null) return;
            if (!S.GachaBoxes.TryGetValue(boxKey, out var st)) { st = new GachaState(); S.GachaBoxes[boxKey] = st; }
            double cost = box.Cost * n; if (S.Gem < cost) { App.Toast("💎 다이아가 부족합니다"); return; }
            S.Gem -= cost;
            var rng = new Mulberry32((uint)Environment.TickCount ^ 0x5bd1e995u);
            var got = new List<GearItem>();
            for (int i = 0; i < n; i++)
            {
                foreach (var raw in GearSystem.GachaPull(D, st, box, rng)) { var g = S.NewGear(raw.Part, raw.Type, raw.Rar, raw.Plus); g.IsNew = true; S.Inv.Add(g); got.Add(g); }
                S.Pulls++;
            }
            App.Persist(); Refresh();
            var best = got[0]; foreach (var g in got) if (GearSystem.GearScore(g) > GearSystem.GearScore(best)) best = g;
            // 결과 팝업 — 주인 지정 Shop_Chest_Open 그대로: 리본 제목 · 열린 상자 그림 · 프리팹의 보상 칸(ItemFrame_01) 자리에 얻은 장비 격자(칸 = ListItem_EquipMent · 4열)
            var root = App.Overlay.OpenPrefab("ui.chestOpen"); var rt = (RectTransform)root.transform;
            UiKit.SetText(rt, "Title_01_NoDeco_Tangerine/Text (TMP)", $"{box.Name} {n}회" + (got.Count > n ? $" · {got.Count}개" : ""));
            var slot = UiKit.Find(rt, "ItemFrame_01") as RectTransform; float cy = slot != null ? slot.anchoredPosition.y : 217f; if (slot != null) slot.gameObject.SetActive(false);
            UiKit.SetSprite(rt, "Image_Chest", "chest." + box.Key + ".open", Palette.White);
            float cs = GearUi.CellSize(App.Assets), gap = 14f; const int cols = 4, rows = 3;
            var grid = UiKit.Rect(rt, "Got"); grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 0.5f); grid.pivot = new Vector2(0.5f, 0.5f);
            grid.sizeDelta = new Vector2(cols * cs + (cols - 1) * gap, rows * cs + (rows - 1) * gap); grid.anchoredPosition = new Vector2(0, cy);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(cs, cs); gl.spacing = new Vector2(gap, gap); gl.childAlignment = TextAnchor.MiddleCenter; gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = cols;
            foreach (var g in got) GearUi.Cell(grid, D, g, new GearUi.CellOpts { IsNew = true }, null);
            var sub = UiKit.Text(rt, $"최고 등급 <color=#{ColorUtility.ToHtmlStringRGB(Palette.ByName(Palette.RarName(best.Rar)))}>{GearUi.RarName(D, best.Rar)}</color> · 인벤토리에 담겼습니다 — 장착은 장비 탭에서", 26, Palette.White, TextAnchor.MiddleCenter, true);
            sub.rectTransform.anchorMin = sub.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); sub.rectTransform.sizeDelta = new Vector2(960, 48); sub.rectTransform.anchoredPosition = new Vector2(0, cy + grid.sizeDelta.y / 2f + 36f);
            var names = new List<string>(); foreach (var g in got) names.Add($"{GearUi.RarName(D, g.Rar)} {GearUi.Name(D, g)}");
            var chest = UiKit.Find(rt, "Chest") as RectTransform; float chestBottom = chest != null ? chest.anchoredPosition.y - chest.rect.height / 2f : -674f;
            var list = UiKit.Text(rt, string.Join(" · ", names), 22, Palette.CreamDark, TextAnchor.UpperCenter, true);
            list.rectTransform.anchorMin = list.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); list.rectTransform.sizeDelta = new Vector2(960, 130); list.rectTransform.anchoredPosition = new Vector2(0, chestBottom - 100f);
            UiKit.SetText(rt, "Text_TouchContionue", "터치하면 닫기");
            var bgT = UiKit.Find(rt, "Background"); if (bgT != null) UiKit.Clickable(bgT, () => { App.Overlay.Close(); Refresh(); }, false);
            if (chest != null) UiKit.PopIn(chest, 0.5f, 0.5f);
        }
    }
}
