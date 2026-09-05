using System;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 장비 탭 = 주인 지정 GUI Pro 데모 프리팹 **Character_Hero_Equipment 그대로**(T7 · 주인 지시 2026-09-05).
    /// 프리팹 요소를 옮기거나 지우지 않고 글자·그림·개수만 바꾼다: 슬롯 6칸(프리팹 격자 164px 그대로 · 부위 라벨 없음 · «Lv.N» 만) ·
    /// 가운데 캐릭터 = 플레이어 프리팹(<see cref="HeroView"/> · 장착 외형 반영) · 전투력 · 공/체/실 3칸 · 인벤 = ListItem_EquipMent 격자(장착분 숨김) ·
    /// 상단은 TopBar 없이 오른쪽 위 골드만 · 하단 버튼 = 상점 / 합성(대장간). «균등 보너스» 문구 없음. 자동 장착 없음(«↑» 대신 프리팹의 빨간 점).
    /// </summary>
    public sealed class GearScreen : GameScreen
    {
        public override string Name => "gear";
        RectTransform _rt; Transform _slots, _content; Text _power, _atk, _hp, _sh, _fuseTxt, _gold; HeroView _hero;
        const int SlotCount = 6;

        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Palette.Hex("#EBDEC0"); bg.raycastTarget = true;   // 탭바 뒤까지 프리팹 Background 색
            var root = UiKit.Spawn("ui.equipment", Root); _rt = (RectTransform)root.transform;
            UiKit.Pct(_rt, 0, 0, 100, Layout.TabBar.Y);   // 프리팹 통째로 — 아래 탭바(⑧) 위까지. 내부 요소는 프리팹 값 그대로
            UiKit.Hide(_rt, "Label_Tapered_01_Yellow", "Tab_02_BoxMenu_Icon");
            var title = UiKit.Find(_rt, "Title_LineDeco_02_s"); if (title != null) UiKit.SetText(title, "Text (TMP)", "장비");
            // 상단: TopBar 없음 · 오른쪽 위 골드만 (ResourceBar_Group 의 Coin 칸만 보이게)
            var top = UiKit.Find(_rt, "Top");
            if (top != null)
            {
                var bar = UiKit.Spawn("ui.resourceBar", top); var br = (RectTransform)bar.transform;
                var hl = bar.GetComponent<HorizontalLayoutGroup>(); if (hl != null) hl.enabled = false;
                UiKit.Hide(br, "ResourceBar_Gem", "ResourceBar_GemStone");
                br.anchorMin = br.anchorMax = new Vector2(1, 0.5f); br.pivot = new Vector2(1, 0.5f); br.sizeDelta = new Vector2(250, 65.1f); br.anchoredPosition = new Vector2(-24, 0);
                var coin = UiKit.Find(br, "ResourceBar_Coin"); if (coin != null) UiKit.Stretch((RectTransform)coin);
                _gold = UiKit.SetText(br, "ResourceBar_Coin/Text (TMP)", "0");
            }
            // 슬롯 격자 — 프리팹 GridLayoutGroup(164×164 · 2열 · 세로 우선) 그대로 → 왼쪽열 0~2 = 무기·목걸이·갑옷 · 오른쪽열 3~5 = 투구·장갑·신발 (index.html GEAR_COL)
            _slots = UiKit.Find(_rt, "Group_Slot");
            // 가운데 캐릭터 = 플레이어 프리팹 (샘플 그림은 끄고 같은 자리에 HeroView)
            var character = UiKit.Find(_rt, "Character");
            if (character != null) { var ci = character.GetComponent<Image>(); if (ci != null) ci.enabled = false; _hero = HeroView.Attach((RectTransform)character, HeroView.PlayerSkin(App)); var arf = UiKit.Ensure<AspectRatioFitter>(_hero.gameObject); arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent; arf.aspectRatio = 1f; }   // 정사각 텍스처 — 314×398 자리에서 찌그러지지 않게
            var mid1 = UiKit.Find(_rt, "Middle1");
            if (mid1 != null)
            {
                UiKit.Hide(mid1, "Text_Level");   // 프리팹의 «Lv.9» 자리 — 이 게임엔 캐릭터 레벨이 없다(균등 보너스 문구도 넣지 않는다)
                var grp = UiKit.Find(mid1, "Group"); if (grp != null) { _power = UiKit.SetText(grp, "Text", "0"); UiKit.SetSprite(grp, "Icon", "ui.battle", Palette.White); }
            }
            // 공/체/실 3칸 — 프리팹 Group_List 의 3행 그대로
            var list = UiKit.Find(_rt, "Group_List");
            if (list != null && list.childCount >= 3)
            {
                _atk = UiKit.SetText(list.GetChild(0), "Text (TMP)", "0"); UiKit.SetSprite(list.GetChild(0), "Icon", "pi.attack", Palette.InkSoft);
                _hp = UiKit.SetText(list.GetChild(1), "Text (TMP)", "0"); UiKit.SetSprite(list.GetChild(1), "Icon", "pi.heart", Palette.Red);
                _sh = UiKit.SetText(list.GetChild(2), "Text (TMP)", "0"); UiKit.SetSprite(list.GetChild(2), "Icon", "pi.shield", Palette.Sky);
            }
            // 인벤 격자 — 프리팹 ScrollRect/Content(188 칸 · 5열) 그대로 · 칸은 ListItem_EquipMent
            _content = UiKit.Find(_rt, "Content");
            if (_content != null)
            {
                UiKit.Clear(_content);
                var sr = _rt.GetComponentInChildren<ScrollRect>(true);
                if (sr != null) { sr.scrollSensitivity = 40; var vp = sr.viewport != null ? sr.viewport.GetComponent<Image>() : null; if (vp != null) vp.raycastTarget = true; }
            }
            // 하단 — 뒤로(로비) · 상점 · 합성(대장간)
            var back = UiKit.Find(_rt, "ArrowIconButton_03_Back"); if (back != null) UiKit.Clickable(back, () => App.ShowScreen("lobby"));
            var btns = UiKit.Find(_rt, "Group_Buttons");
            if (btns != null)
            {
                var shop = UiKit.FindAny(btns, "Button_02_Blue"); if (shop == null && btns.childCount > 0) shop = btns.GetChild(0);
                var fuse = UiKit.FindAny(btns, "Button_02_Convex_Green"); if (fuse == null && btns.childCount > 1) fuse = btns.GetChild(1);
                if (shop != null) { var t = shop.GetComponentInChildren<Text>(true); if (t != null) t.text = "상점"; UiKit.Clickable(shop, () => App.ShowScreen("shop")); }
                if (fuse != null) { _fuseTxt = fuse.GetComponentInChildren<Text>(true); if (_fuseTxt != null) _fuseTxt.text = "합성"; UiKit.Clickable(fuse, () => App.ShowScreen("forge")); }
            }
            NavBar.Attach(this, Root, "gear");
        }

        static string PartAt(int slotIndex) => slotIndex < 3 ? GearUi.ColLeft[slotIndex] : GearUi.ColRight[slotIndex - 3];

        public override void Refresh()
        {
            var D = App.Data; var S = App.Save;
            // 슬롯 6칸 — 프리팹 슬롯의 ItemFrame_01/Item 에 스프라이트만 꽂는다(크기 그대로)
            if (_slots != null)
            {
                for (int i = 0; i < _slots.childCount && i < SlotCount; i++)
                {
                    var slot = _slots.GetChild(i); string part = PartAt(i); var g = S.EquippedGear(part); int lv = S.SlotLv(part);
                    var frame = UiKit.Find(slot, "ItemFrame_01");
                    if (frame != null)
                    {
                        var area = UiKit.Find(frame, "NormalArea");
                        if (area != null) { UiKit.Clear(area); if (g != null) { var f = UiKit.Spawn("ui.itemFrame." + Palette.RarName(g.Rar), area); UiKit.Stretch((RectTransform)f.transform); } }
                        var item = UiKit.Find(frame, "Item");
                        if (item != null) { item.gameObject.SetActive(g != null); if (g != null) { var im = UiKit.SetSprite(frame, "Item", GearLook.IconKey(D, g), Palette.White); if (im != null) im.preserveAspect = true; } }
                        UiKit.Show(frame, "Add_1", g == null); UiKit.Show(frame, "Add_2", false); UiKit.Show(frame, "Lock", false); UiKit.Show(frame, "Focus", false); UiKit.Show(frame, "Disable", false);
                    }
                    var dia = UiKit.FindAny(slot, "BasicFrame_Diamond_01_NoBorder_Plum", "BasicFrame_Diamond_H48_NoBorder_Plum");
                    if (dia != null) { dia.gameObject.SetActive(g != null); if (g != null) UiKit.SetSprite(dia, "Icon", GearUi.SetIcon(GearUi.Set(D, g)), Palette.White); }
                    var lvl = slot.Find("Text_Level");   // 부위 이름(«갑옷·장갑·투구…»)은 적지 않는다 — 슬롯 레벨(+강화)만
                    if (lvl != null) { lvl.gameObject.SetActive(true); var t = lvl.GetComponent<Text>(); if (t != null) t.text = $"Lv.{lv}" + (g != null && g.Plus > 0 ? $" +{g.Plus}" : ""); }
                    UiKit.Show(slot, "Alert_Dot_01_Red", GearUi.BetterInInv(S, part));   // 프리팹의 빨간 점 = 인벤에 더 좋은 게 있다(자동 장착 없음)
                    string p = part; var gg = g;
                    UiKit.Clickable(slot, () => { if (gg != null) GearUi.OpenDetail(App, gg, Refresh); else GearUi.OpenSlot(App, p, Refresh); });
                }
            }
            // 스탯 · 전투력 · 골드 · 합성 버튼 · 캐릭터 외형
            var pw = GearSystem.BuildPower(D, S.CurBuild(D));
            if (_atk != null) _atk.text = UiKit.Fmt(Math.Round(pw.Atk)); if (_hp != null) _hp.text = UiKit.Fmt(Math.Round(pw.Hp)); if (_sh != null) _sh.text = UiKit.Fmt(Math.Round(pw.Sh));
            if (_power != null) _power.text = UiKit.Fmt(App.Power());
            if (_gold != null) _gold.text = UiKit.Fmt(S.Gold);
            var fkeys = GearUi.FusableKeys(S);
            if (_fuseTxt != null) _fuseTxt.text = fkeys.Count > 0 ? $"합성 ({fkeys.Count}) !" : "합성";
            _hero?.SetSkin(HeroView.PlayerSkin(App));
            // 인벤 격자 — 장착 중인 장비는 숨긴다(«장착중» 배지 없음 · 합성 점도 여기서는 끔)
            if (_content != null)
            {
                UiKit.Clear(_content); int shown = 0;
                foreach (var g in GearUi.Sorted(S))
                {
                    if (S.IsEquipped(g)) continue;
                    shown++; var gi = g;
                    GearUi.Cell(_content, D, g, new GearUi.CellOpts { IsNew = g.IsNew }, () => GearUi.OpenDetail(App, gi, Refresh));
                }
                if (shown == 0) GearUi.Empty(_content, S.Inv.Count == 0 ? "장비가 없습니다.\n상점에서 뽑기로 장비를 얻으세요." : "장착하지 않은 장비가 없습니다.");
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
