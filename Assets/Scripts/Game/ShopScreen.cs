using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 상점 — ref-layout ⑤. 무료 보급(일 1회 · gacha.json economy.dailyGem) · 모의 결제(iapGem · 실결제 없음) · 장비 뽑기 상자 3종(gacha.json boxes).
    /// 상자 칸은 GUI Pro ListItem_ShopChest · 결과 팝업은 주인 지정 Shop_Chest_Open. 자동 장착 없음 — 인벤에만 담긴다(NEW 뱃지).
    /// </summary>
    public sealed class ShopScreen : GameScreen
    {
        public override string Name => "shop";
        Text _freeTxt; Button _free; RectTransform _chests;
        readonly Dictionary<string, (Button one, Button ten, Text pity)> _box = new Dictionary<string, (Button, Button, Text)>();
        const int IapWon = 110000;   // index.html GEM_PACKS[0].won — 표시 전용 모의 가격 (PLAN §11.5)

        static string Today() => DateTime.Now.ToString("yyyy-MM-dd");

        protected override void Build()
        {
            var D = App.Data;
            var bg = Root.gameObject.AddComponent<Image>(); bg.color = Palette.Hex("#EBDEC0"); bg.raycastTarget = true;
            var pills = UiKit.SpawnRt("ui.resourceBar", Root, new Layout.R(2, 3.7f, 48, 2.8f)); UiKit.SetText(pills, "ResourceBar_Coin/Text (TMP)", "0"); UiKit.SetText(pills, "ResourceBar_Gem/Text (TMP)", "0");
            var title = UiKit.SpawnRt("ui.lineTitle", Root, new Layout.R(30, 7.5f, 40, 4)); UiKit.SetText(title, "Text (TMP)", "상점", Palette.Ink, 44);
            // 무료 보급
            var freeRow = UiKit.Rect(Root, "FreeRow"); UiKit.Pct(freeRow, Layout.ShopFreeRow);
            var fc = UiKit.SpawnRt("ui.frameIvory", freeRow, new Layout.R(0, 0, 49, 100));
            var fi = UiKit.Icon(fc, "Gem", "hud.gem"); UiKit.Pct(fi.rectTransform, 3, 20, 16, 60);
            UiKit.Label(fc, 20, 8, 46, 45, $"💎 {UiKit.FmtQty(D.Gacha.DailyGem)}", 34, Palette.Ink, TextAnchor.MiddleLeft, true, false);
            UiKit.Label(fc, 20, 52, 46, 40, "일일 무료 보급", 24, Palette.InkLight, TextAnchor.MiddleLeft, true, false);
            var fb = UiKit.Button(fc, "ui.btnGreen", "수령", OnFree, new Layout.R(68, 18, 30, 64)); _free = fb.GetComponent<Button>(); _freeTxt = UiKit.ButtonText(fb);
            var lc = UiKit.SpawnRt("ui.frameIvory", freeRow, new Layout.R(51, 0, 49, 100)); var li = UiKit.Icon(lc, "Lock", "pi.lock", Palette.InkLight); UiKit.Pct(li.rectTransform, 6, 25, 14, 50);
            UiKit.Label(lc, 24, 10, 72, 80, "추가 보급 (준비 중)", 26, Palette.InkLight, TextAnchor.MiddleLeft, true, false);
            // 다이아 (모의 결제)
            Section(Layout.ShopSec1, "다이아 (모의 결제 — 실결제 없음)");
            for (int i = 0; i < 6; i++)
            {
                var row = i < 3 ? Layout.ShopCardRow1 : Layout.ShopCardRow2; int col = i % 3;
                float w = (row.W - 2 * Layout.ShopCardGap) / 3f; var r = new Layout.R(row.X + col * (w + Layout.ShopCardGap), row.Y, w, row.H);
                var card = UiKit.SpawnRt("ui.frameIvory", Root, r);
                if (i == 0)
                {
                    var gi = UiKit.Icon(card, "Gem", "ui.gemRed"); UiKit.Pct(gi.rectTransform, 25, 6, 50, 38);
                    UiKit.Label(card, 4, 46, 92, 14, $"💎 {UiKit.FmtQty(D.Gacha.IapGem)}", 32, Palette.Ink);
                    UiKit.Label(card, 4, 60, 92, 10, "다이아 금고", 24, Palette.InkLight);
                    UiKit.Button(card, "ui.btnYellow", $"₩{IapWon:#,0}", () => { App.Save.Gem += D.Gacha.IapGem; App.Persist(); Refresh(); App.Toast($"💎 {UiKit.FmtQty(D.Gacha.IapGem)} 지급 (모의 결제)"); }, new Layout.R(10, 74, 80, 22));
                }
                else { var lk = UiKit.Icon(card, "Lock", "pi.lock", Palette.InkLight); UiKit.Pct(lk.rectTransform, 32, 22, 36, 36); UiKit.Label(card, 4, 62, 92, 20, "준비 중", 26, Palette.InkLight); }
            }
            // 장비 뽑기 상자 3종
            Section(Layout.ShopSec2, "장비 뽑기");
            _chests = UiKit.Rect(Root, "Chests"); UiKit.Pct(_chests, Layout.ShopCardRow3);
            int k = 0;
            foreach (var box in D.Gacha.Boxes)
            {
                float w = (100 - 2 * 2f) / 3f; var card = UiKit.Spawn("ui.shopChest", _chests); var crt = (RectTransform)card.transform; UiKit.Pct(crt, k * (w + 2f), 0, w, 100);
                UiKit.SetText(crt, "Text_Title", box.Name, Palette.Ink, 34);
                UiKit.SetSprite(crt, "Icon", "chest." + box.Key, Palette.White);
                var icon = UiKit.Find(crt, "Icon"); if (icon != null) { var ir = (RectTransform)icon; ir.anchorMin = new Vector2(0.15f, 0.42f); ir.anchorMax = new Vector2(0.85f, 0.86f); ir.offsetMin = ir.offsetMax = Vector2.zero; icon.GetComponent<Image>().preserveAspect = true; }
                var pity = UiKit.SetText(crt, "Text_Limit", "", Palette.InkSoft, 20); if (pity != null) { var pr = pity.rectTransform; pr.anchorMin = new Vector2(0.03f, 0.27f); pr.anchorMax = new Vector2(0.97f, 0.43f); pr.offsetMin = pr.offsetMax = Vector2.zero; pity.resizeTextForBestFit = true; pity.resizeTextMinSize = 12; pity.resizeTextMaxSize = 20; }
                var price = UiKit.Find(crt, "Button_Price"); Button one = null, ten = null;
                if (price != null)
                {
                    var pr = (RectTransform)price; pr.anchorMin = new Vector2(0.04f, 0.02f); pr.anchorMax = new Vector2(0.96f, 0.14f); pr.offsetMin = pr.offsetMax = Vector2.zero;
                    UiKit.SetSprite(price, "Icon", "hud.gem", Palette.White); UiKit.SetText(price, "Text (TMP)", $"1회 {UiKit.FmtQty(box.Cost)}", Palette.White, 30);
                    var bk = box.Key; one = UiKit.Clickable(price, () => Pull(1, bk));
                    var tenRt = UiKit.Button(crt, "ui.btnSmallBlue", $"10회 💎{UiKit.FmtQty(box.Cost * D.Gacha.TenPullCount)}", () => Pull(D.Gacha.TenPullCount, bk), new Layout.R(4, 15, 92, 11)); ten = tenRt.GetComponent<Button>();
                }
                _box[box.Key] = (one, ten, pity);
                k++;
            }
            NavBar.Attach(this, Root, "shop");
        }
        void Section(Layout.R r, string text) { var t = UiKit.SpawnRt("ui.lineTitleL", Root, new Layout.R(r.X + 5, r.Y - 0.5f, r.W - 10, r.H + 1.5f)); UiKit.SetText(t, "Text (TMP)", text, Palette.Ink, 30); }

        static string RateText(GameData D, GachaBox box)
        {
            var parts = new List<string>(); for (int i = box.Rate.Length - 1; i >= 0; i--) if (box.Rate[i] > 0) parts.Add($"{GearUi.RarName(D, i)} {box.Rate[i]:0.#}%");
            return string.Join(" · ", parts);
        }
        static string PityText(GachaBox box, GachaState st)
        {
            var o = new List<string>();
            if (box.PityMyth > 0) o.Add($"신화 확정까지 {Math.Max(0, box.PityMyth - st.P50)}회");
            if (box.PityLegend > 0) o.Add($"전설 확정까지 {Math.Max(0, box.PityLegend - st.P10)}회");
            o.Add($"누적 {st.Pulls}회");
            return string.Join(" · ", o);
        }

        public override void Refresh()
        {
            var D = App.Data; var S = App.Save;
            UiKit.SetText(Root, "ResourceBar_Coin/Text (TMP)", UiKit.Fmt(S.Gold)); UiKit.SetText(Root, "ResourceBar_Gem/Text (TMP)", UiKit.Fmt(S.Gem));
            bool canFree = S.FreeDay != Today(); UiKit.SetInteractable(_free, canFree); if (_freeTxt != null) _freeTxt.text = canFree ? "수령" : "완료";
            foreach (var box in D.Gacha.Boxes)
            {
                if (!_box.TryGetValue(box.Key, out var w)) continue;
                if (!S.GachaBoxes.TryGetValue(box.Key, out var st)) { st = new GachaState(); S.GachaBoxes[box.Key] = st; }
                if (w.pity != null) w.pity.text = RateText(D, box) + "\n" + PityText(box, st);
                UiKit.SetInteractable(w.one, S.Gem >= box.Cost); UiKit.SetInteractable(w.ten, S.Gem >= box.Cost * D.Gacha.TenPullCount);
            }
            NavBar.Refresh(Root);
        }

        void OnFree()
        {
            var S = App.Save; if (S.FreeDay == Today()) return;
            S.Gem += App.Data.Gacha.DailyGem; S.FreeDay = Today(); App.Persist(); Refresh(); App.Toast($"💎 {UiKit.FmtQty(App.Data.Gacha.DailyGem)} 수령!");
        }

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
            // 결과 팝업 — 주인 지정 Shop_Chest_Open
            var root = App.Overlay.OpenPrefab("ui.chestOpen"); var rt = (RectTransform)root.transform;
            UiKit.SetText(rt, "Title_01_NoDeco_Tangerine/Text (TMP)", $"{box.Name} {n}회" + (got.Count > n ? $" · {got.Count}개" : ""));
            UiKit.Hide(rt, "ItemFrame_01");
            UiKit.SetSprite(rt, "Image_Chest", "chest." + box.Key + ".open", Palette.White);
            var sub = UiKit.Label(rt, 6, 16, 88, 4, $"최고 등급 <color=#{ColorUtility.ToHtmlStringRGB(Palette.ByName(Palette.RarName(best.Rar)))}>{GearUi.RarName(D, best.Rar)}</color> · 인벤토리에 담겼습니다 — 장착은 장비 탭에서", 26, Palette.White);
            var grid = UiKit.Rect(rt, "Got"); UiKit.Pct(grid, 6, 21, 88, 36);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>(); float cw = UiKit.FrameW * 0.88f / 4 - 14; gl.cellSize = new Vector2(cw, cw); gl.spacing = new Vector2(14, 14); gl.childAlignment = TextAnchor.UpperCenter; gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 4;
            foreach (var g in got) GearUi.Cell(grid, D, g, new GearUi.CellOpts { IsNew = true }, null);
            var names = new List<string>(); foreach (var g in got) names.Add($"{GearUi.RarName(D, g.Rar)} {GearUi.Name(D, g)}");
            UiKit.Label(rt, 6, 58, 88, 5, string.Join(" · ", names), 22, Palette.CreamDark);
            UiKit.SetText(rt, "Text_TouchContionue", "터치하면 닫기");
            var bgT = UiKit.Find(rt, "Background"); if (bgT != null) UiKit.Clickable(bgT, () => { App.Overlay.Close(); Refresh(); }, false);
            var chest = UiKit.Find(rt, "Chest"); if (chest != null) UiKit.PopIn((RectTransform)chest, 0.5f, 0.5f);
        }
    }
}
