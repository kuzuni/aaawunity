using System;
using System.Collections.Generic;
using DG.Tweening;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 팝업 층(index.html #overlay). 팝업이 열려 있는 동안 전투 시간은 흐르지 않는다(BattleScreen 이 IsOpen 을 본다).
    /// 그림은 전부 GUI Pro 데모 프리팹 — 주인 지정: 레벨업 3택 = Play_Perk_Selection_02 · 승리 = Play_Result_Win_01 ·
    /// 설정 = Settings. 나머지(쉼터·악마·천사·사망·보유 특전·광고)는 Popup_Box_02 + Title_01 리본 + Button_02 로 조립한다.
    /// </summary>
    public sealed class Overlay
    {
        readonly App _app;
        public RectTransform Root { get; }
        GameObject _cur;
        float _countdown; Action _onCountdown; Text _countText;
        public bool IsOpen => Root.gameObject.activeSelf;

        public Overlay(App app)
        {
            _app = app;
            Root = UiKit.Rect(app.Frame, "Overlay"); UiKit.Stretch(Root);
            Root.gameObject.SetActive(false);
        }

        // ───────────────────────── 공통 ─────────────────────────
        void Begin()
        {
            UiKit.Clear(Root);
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            _countdown = 0; _onCountdown = null; _countText = null;
        }
        public void Close() { UiKit.Clear(Root); Root.gameObject.SetActive(false); _cur = null; _countdown = 0; }

        /// <summary>어둠 + 팝업 상자(Popup_Box_02 변형) + 리본 제목. 돌려주는 RectTransform 안에서 Pct 로 내용을 배치한다.</summary>
        RectTransform Box(string popupKey, string titleKey, string title, Layout.R rect, bool dim = true)
        {
            Begin();
            if (dim)
            {
                var d = UiKit.Rect(Root, "Dimmed"); UiKit.Stretch(d);
                var di = d.gameObject.AddComponent<Image>(); di.color = Palette.A(Palette.Dim, 0.85f); di.raycastTarget = true;
                UiKit.FadeIn(di, 0.85f);
            }
            var box = UiKit.SpawnRt(popupKey, Root, rect);
            foreach (var g in box.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = true;   // 상자 뒤로 클릭이 새지 않게
            var ribbon = UiKit.Spawn(titleKey, box);
            var rr = (RectTransform)ribbon.transform;
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 1f); rr.pivot = new Vector2(0.5f, 0.5f); rr.sizeDelta = new Vector2(656, 115); rr.anchoredPosition = new Vector2(0, 8);
            var tt = UiKit.SetText(rr, "Text (TMP)", title); if (tt != null) { tt.resizeTextForBestFit = true; tt.resizeTextMinSize = 20; tt.resizeTextMaxSize = 52; }
            UiKit.PopIn(box);
            _cur = box.gameObject;
            return box;
        }
        Text Sub(RectTransform box, string s, float y = 9, float h = 7, int size = 36, Color? c = null)
            => UiKit.Label(box, 6, y, 88, h, s, size, c ?? Palette.InkSoft, TextAnchor.MiddleCenter, true, false);

        /// <summary>특전 카드 한 장(ListItem_StageBuff_02) — 등급 색으로 CardFrame_04/ItemFrame_04 를 갈아 끼우고 이름·설명을 넣는다.</summary>
        public RectTransform PerkCard(Transform parent, PerkDef p, string colorName, Action onClick)
        {
            var card = UiKit.Spawn("ui.card", parent); var rt = (RectTransform)card.transform;
            var frameArea = UiKit.Find(rt, "CardFrameArea"); var itemArea = UiKit.Find(rt, "ItemFrameArea");
            if (frameArea != null) { UiKit.Clear(frameArea); var f = UiKit.Spawn("ui.cardFrame." + colorName, frameArea); UiKit.Stretch((RectTransform)f.transform);
                var tb = UiKit.Find(f.transform, "TitleBg"); if (tb != null) { var gl = UiKit.Text(tb, p.GradeName ?? "", 28, Palette.White, TextAnchor.MiddleCenter, true); UiKit.Stretch(gl.rectTransform, 8, 4, 8, 4); } }
            if (itemArea != null) { UiKit.Clear(itemArea); var f = UiKit.Spawn("ui.itemFrame4." + colorName, itemArea); var frt = (RectTransform)f.transform; frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f); frt.anchoredPosition = Vector2.zero; frt.sizeDelta = new Vector2(162, 165);
                UiKit.SetSprite(frt, "Icon", Icons.Perk(p.Id), Palette.White); }
            UiKit.Hide(rt, "Focus");
            var name = UiKit.SetText(rt, "Text", p.Name, Palette.Ink, 40);
            if (name != null) { name.alignment = TextAnchor.MiddleLeft; var nr = name.rectTransform; nr.anchorMin = new Vector2(0.24f, 0.55f); nr.anchorMax = new Vector2(0.98f, 0.95f); nr.offsetMin = nr.offsetMax = Vector2.zero; name.resizeTextForBestFit = true; name.resizeTextMaxSize = 40; }
            var desc = UiKit.SetText(rt, "Text_Value", p.Desc, Palette.InkSoft, 30);
            if (desc != null) { desc.alignment = TextAnchor.UpperLeft; var dr = desc.rectTransform; dr.anchorMin = new Vector2(0.24f, 0.08f); dr.anchorMax = new Vector2(0.98f, 0.55f); dr.offsetMin = dr.offsetMax = Vector2.zero; desc.resizeTextForBestFit = true; desc.resizeTextMaxSize = 30; desc.resizeTextMinSize = 16; }
            if (onClick != null) UiKit.Clickable(rt, onClick);
            return rt;
        }

        /// <summary>상단 스탯 줄(8칸) — index.html STAT_DEFS 순서 · HUD 와 같은 아이콘·값 (ref-layout ⑦ OvStats).</summary>
        void StatsRow(RectTransform parent, BattleState G, Layout.R r)
        {
            var row = UiKit.Rect(parent, "Stats"); UiKit.Pct(row, r);
            var bg = row.gameObject.AddComponent<Image>(); bg.sprite = App.I.Assets.Sprite("fr.r12"); bg.type = Image.Type.Sliced; bg.color = Palette.A(Palette.Dim, 0.6f); bg.raycastTarget = false;
            var defs = BattleScreen.StatDefs; float cw = 100f / defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var ic = UiKit.Icon(row, "ic", Icons.Stat(d.Key)); UiKit.Pct(ic.rectTransform, i * cw + cw * 0.22f, 8, cw * 0.56f, 44);
                var v = UiKit.Label(row, i * cw, 54, cw, 40, d.Fmt(G), 26, d.Up(G, _app.GetScreen<BattleScreen>()?.BaseStats) ? Palette.Green : Palette.White);
            }
        }

        // ───────────────────────── 레벨 업 3택 (주인 지정 Play_Perk_Selection_02) ─────────────────────────
        public void LevelUp(BattleState G, Action<PerkDef> onPick)
        {
            Begin();
            var offer = G.Pending?.Offer ?? new List<PerkDef>();
            var root = UiKit.Spawn("ui.perkSelect", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            var dim = UiKit.Find(rt, "Dimmed"); if (dim != null) { var di = dim.GetComponent<Image>(); if (di != null) { di.raycastTarget = true; UiKit.FadeIn(di, 0.85f); } }
            UiKit.SetText(rt, "Title_01_NoDeco_Tangerine/Text (TMP)", "레벨 업!");
            var sub = UiKit.Find(rt, "Text (TMP)"); if (sub != null) UiKit.SetText(rt, "Text (TMP)", "특전 하나를 고르세요");
            var group = UiKit.Find(rt, "Group_Card");
            if (group != null)
            {
                UiKit.Clear(group);
                foreach (var p in offer)
                {
                    var perk = p;
                    var card = PerkCard(group, perk, Palette.PerkGradeName(perk.Grade), () => { Close(); onPick(perk); });
                    var le = card.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 220; le.preferredWidth = 964;
                    UiKit.PopIn(card, 0.9f, 0.3f);
                }
            }
            var btn = UiKit.Find(rt, "Button_02_Orange");
            if (btn != null) { UiKit.SetText(btn, "Text (TMP)", $"보유 특전 {G.Taken.Count}"); UiKit.Clickable(btn, () => PerkBook(G, () => LevelUp(G, onPick))); }
            var book = UiKit.Find(rt, "Book"); if (book != null) { UiKit.SetText(book, "Text (TMP)", G.Taken.Count.ToString()); UiKit.Clickable(book, () => PerkBook(G, () => LevelUp(G, onPick))); }
            StatsRow(rt, G, Layout.OvStats);
        }

        // ───────────────────────── 보유 특전 (PERKS) ─────────────────────────
        public void PerkBook(BattleState G, Action onBack)
        {
            var box = Box("ui.popup.blue", "ui.title.sky", "PERKS", new Layout.R(4, 12, 92, 76));
            Sub(box, $"이번 원정에서 얻은 특전 ({G.Taken.Count})", 7, 5, 32);
            // 목록 — 같은 특전은 묶어 ×N
            var groups = new List<KeyValuePair<PerkDef, int>>();
            foreach (var p in G.Taken) { int i = groups.FindIndex(k => k.Key.Id == p.Id); if (i >= 0) groups[i] = new KeyValuePair<PerkDef, int>(p, groups[i].Value + 1); else groups.Add(new KeyValuePair<PerkDef, int>(p, 1)); }
            var view = UiKit.Rect(box, "Scroll"); UiKit.Pct(view, 4, 13, 92, 73);
            view.gameObject.AddComponent<RectMask2D>();
            var sr = view.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40;
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            var content = UiKit.Rect(view, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1); content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>(); vl.spacing = 12; vl.childForceExpandHeight = false; vl.childForceExpandWidth = true; vl.childControlHeight = true; vl.childControlWidth = true; vl.padding = new RectOffset(0, 0, 4, 4);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content; sr.viewport = view;
            if (groups.Count == 0) Sub(box, "아직 획득한 특전이 없습니다", 40, 8, 34, Palette.InkLight);
            foreach (var kv in groups)
            {
                var card = PerkCard(content, kv.Key, Palette.PerkGradeName(kv.Key.Grade), null);
                var le = card.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 200;
                if (kv.Value > 1) { var n = UiKit.Text(card, "×" + kv.Value, 36, Palette.Yellow, TextAnchor.MiddleRight); UiKit.Pct(n.rectTransform, 80, 4, 18, 40); }
            }
            foreach (var b in G.Blessings) Sub(box, b, 87, 4, 26, Palette.Orange);
            UiKit.Button(box, "ui.btnBlue", onBack != null ? "← 특전 선택으로" : "닫기", () => { Close(); onBack?.Invoke(); }, new Layout.R(25, 88, 50, 8));
        }

        // ───────────────────────── 쉼터 ─────────────────────────
        public void Rest(BattleState G, Action<bool> onChoose)
        {
            var box = Box("ui.popup.green", "ui.title.green", "쉼터", Layout.EvBox);
            Sub(box, "모닥불 앞에서 잠시 쉬어갑니다", 10, 7, 34);
            var ic = UiKit.Icon(box, "Fire", "ui.fire"); UiKit.Pct(ic.rectTransform, 35, 20, 30, 28);
            string heal = G.C.RestHeal <= 1 ? $"최대 체력 {Math.Round(G.C.RestHeal * 100)}%" : $"체력 {UiKit.Fmt(G.C.RestHeal)}";
            UiKit.Button(box, "ui.btnGreen", $"체력 회복 (+{heal})", () => { Close(); onChoose(true); }, new Layout.R(10, 56, 80, 12));
            UiKit.Button(box, "ui.btnBlue", $"경험치 +{G.C.RestExp}", () => { Close(); onChoose(false); }, new Layout.R(10, 72, 80, 12));
            Sub(box, "다음 레벨에 가까워집니다", 85, 6, 26, Palette.InkLight);
        }

        // ───────────────────────── 악마의 거래 ─────────────────────────
        public void Devil(BattleState G, Action<bool> onChoose)
        {
            var perk = G.Pending?.DevilPerk;
            var box = Box("ui.popup.plum", "ui.title.plum", "악마의 거래", new Layout.R(4, 24, 92, 52));
            Sub(box, "\"네 생명을 바치면... 이 힘을 주지\"", 9, 6, 32, Palette.Plum);
            if (perk != null) { var card = PerkCard(box, perk, "yellow", null); UiKit.Pct(card, 2, 18, 96, 22); }
            double cost = G.C.DevilCostMaxHp > 0 ? G.C.DevilCostMaxHp : G.PK.DevilCostMaxHp;
            Sub(box, $"최대 체력이 {Math.Round(cost * 100)}% 줄어든 채 진행 · 위 전설 특전 1개를 획득", 43, 10, 28, Palette.InkSoft);
            UiKit.Button(box, "ui.btnRed", "거래 수락", () => { Close(); onChoose(true); }, new Layout.R(8, 60, 40, 13));
            UiKit.Button(box, "ui.btnGray", "거절", () => { Close(); onChoose(false); }, new Layout.R(52, 60, 40, 13));
        }
        public void DevilGift(PerkDef perk, Action onOk)
        {
            var box = Box("ui.popup.plum", "ui.title.plum", "악마의 선물", new Layout.R(6, 30, 88, 40));
            Sub(box, "전설 특전을 얻었습니다", 11, 7, 32, Palette.Plum);
            if (perk != null) { var card = PerkCard(box, perk, "yellow", null); UiKit.Pct(card, 2, 24, 96, 28); }
            UiKit.Button(box, "ui.btnOrange", "계속", () => { Close(); onOk?.Invoke(); }, new Layout.R(25, 66, 50, 16));
        }

        // ───────────────────────── 천사의 축복 ─────────────────────────
        public void Angel(BattleState G, Action<double> onChoose)
        {
            var box = Box("ui.popup.yellow", "ui.title.yellow", "천사의 축복", Layout.EvBox);
            Sub(box, "\"용사여, 축복을 내리노라\"", 10, 7, 34, Palette.Orange);
            var ic = UiKit.Icon(box, "Wing", "pi.wing", Palette.Yellow); UiKit.Pct(ic.rectTransform, 35, 20, 30, 26);
            UiKit.Button(box, "ui.btnGreen", $"무료 축복 · 공격력 +{Math.Round((SimPolicy.AngelFree - 1) * 100)}%", () => { Close(); onChoose(SimPolicy.AngelFree); }, new Layout.R(10, 54, 80, 12));
            var ad = UiKit.Button(box, "ui.btnOrange", $"광고 보고 공격력 +{Math.Round((SimPolicy.AngelAd - 1) * 100)}%", () => AdCountdown(3, () => Blessed(onChoose)), new Layout.R(10, 70, 80, 12));
            var adIc = UiKit.Icon(ad, "Ad", "hud.alertAd"); UiKit.Pct(adIc.rectTransform, 84, -22, 18, 50);
            Sub(box, "더 강한 축복", 85, 6, 26, Palette.InkLight);
        }
        void Blessed(Action<double> onChoose)
        {
            var box = Box("ui.popup.yellow", "ui.title.yellow", "축복 강화!", new Layout.R(6, 32, 88, 36));
            Sub(box, $"공격력이 {Math.Round((SimPolicy.AngelAd - 1) * 100)}% 증가했습니다", 18, 14, 36, Palette.Orange);
            UiKit.Button(box, "ui.btnOrange", "계속 전진", () => { Close(); onChoose(SimPolicy.AngelAd); }, new Layout.R(25, 62, 50, 18));
        }
        /// <summary>광고 자리(모의) — 3초 카운트다운 (T3 결정 «광고는 3초 카운트다운으로 대체»).</summary>
        public void AdCountdown(int seconds, Action onDone)
        {
            var box = Box("ui.popup", "ui.title.tangerine", "광고 시청 중...", new Layout.R(10, 36, 80, 28));
            var ic = UiKit.Icon(box, "Ad", "ui.ad"); UiKit.Pct(ic.rectTransform, 38, 18, 24, 34);
            _countText = Sub(box, seconds.ToString(), 56, 30, 72, Palette.Ink);
            _countdown = seconds; _onCountdown = onDone;
        }

        // ───────────────────────── 클리어 (주인 지정 Play_Result_Win_01) ─────────────────────────
        public void Clear(BattleState G, bool last, Action onNext, Action onLobby)
        {
            Begin();
            var root = UiKit.Spawn("ui.resultWin", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            var dim = UiKit.Find(rt, "Dimmed"); if (dim != null) { var di = dim.GetComponent<Image>(); if (di != null) { di.raycastTarget = true; UiKit.FadeIn(di, 0.85f); } }
            UiKit.SetText(rt, "Text", $"챕터 {G.Chapter}");
            UiKit.SetText(rt, "Text (1)", last ? "모든 챕터를 클리어했습니다!" : $"챕터 {G.Chapter + 1} 해금!");
            UiKit.SetText(rt, "Title_01_NoDeco_Tangerine/Text (TMP)", "클리어!");
            UiKit.SetText(rt, "Title_LineDeco_01_s_White/Text (TMP)", "클리어 보상");
            var items = UiKit.Find(rt, "Group_RewardItem");
            if (items != null && items.childCount >= 3)
            {
                Reward(items.GetChild(0), "ui.coin", UiKit.Fmt(G.Gold));
                Reward(items.GetChild(1), "ui.skull", G.Kills.ToString());
                Reward(items.GetChild(2), "ui.hourglass", Math.Round(G.T) + "s");
            }
            UiKit.Hide(rt, "Text_TouchContionue");
            var grp = UiKit.Find(rt, "Group_Buttons");
            var b1 = grp != null && grp.childCount > 0 ? grp.GetChild(0) : null; var b2 = grp != null && grp.childCount > 1 ? grp.GetChild(1) : null;
            if (b1 != null) { if (last) b1.gameObject.SetActive(false); else { UiKit.SetText(b1, "Text (TMP)", "다음 챕터"); UiKit.Clickable(b1, () => { Close(); onNext(); }); } }
            if (b2 != null) { UiKit.SetText(b2, "Text (TMP)", "로비로"); UiKit.Clickable(b2, () => { Close(); onLobby(); }); }
            var title = UiKit.Find(rt, "Title"); if (title != null) UiKit.PopIn((RectTransform)title, 0.6f, 0.45f);
            UiKit.Hide(rt, "SampleEffect_Confetti");
        }
        static void Reward(Transform cell, string iconKey, string value) { UiKit.SetSprite(cell, "Icon", iconKey, Palette.White); UiKit.SetText(cell, "Text (TMP)", value); }

        // ───────────────────────── 사망 (Play_Result_Lose) ─────────────────────────
        public void Dead(BattleState G, Action onLobby)
        {
            Begin();
            var root = UiKit.Spawn("ui.resultLose", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            var dim = UiKit.Find(rt, "Dimmed"); if (dim != null) { var di = dim.GetComponent<Image>(); if (di != null) { di.raycastTarget = true; UiKit.FadeIn(di, 0.85f); } }
            UiKit.SetText(rt, "Title_LineDeco_01_s_White/Text (TMP)", "쓰러졌다...");
            var reward = UiKit.Find(rt, "Reward"); if (reward != null) Reward(reward, "ui.coin", UiKit.Fmt(G.Gold));
            var list = UiKit.Find(rt, "Group_List");
            string[] tips = { $"처치 {G.Kills} · 골드 {UiKit.Fmt(G.Gold)} 획득", "골드로 장비 슬롯을 강화하고 다시 도전하세요!", "장비 3개를 합성하면 등급이 오릅니다" };
            string[] icons = { "ui.skull", "ui.anvil", "ui.bookRed" };
            if (list != null) for (int i = 0; i < list.childCount && i < tips.Length; i++) { UiKit.SetText(list.GetChild(i), "Text (TMP)", tips[i]); UiKit.SetSprite(list.GetChild(i), "Icon", icons[i], Palette.White); }
            UiKit.SetText(rt, "Text_TouchContionue", "터치하면 로비로");
            UiKit.Button(rt, "ui.btnBlue", "로비로", () => { Close(); onLobby(); }, new Layout.R(30, 80, 40, 6));
            var hit = UiKit.Find(rt, "Dimmed"); if (hit != null) UiKit.Clickable(hit, () => { Close(); onLobby(); }, false);
        }

        // ───────────────────────── 일시정지 / 설정 (주인 지정 Settings) ─────────────────────────
        public void Pause(Action onResume, Action onGiveUp)
        {
            Begin();
            var root = UiKit.Spawn("ui.settings", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            var dim = UiKit.Find(rt, "Dimmed"); if (dim != null) { var di = dim.GetComponent<Image>(); if (di != null) { di.raycastTarget = true; UiKit.FadeIn(di, 0.85f); } }
            UiKit.SetText(rt, "Title_Tapered_01_Brown/Text (TMP)", "일시정지");
            var bgm = UiKit.Find(rt, "BGM");
            if (bgm != null)
            {
                UiKit.SetText(bgm, "Text", "소리");
                var sw = UiKit.Find(bgm, "Swich_01");
                if (sw != null) { ApplySwitch(sw, !_app.Save.Muted); UiKit.Clickable(sw, () => { _app.Save.Muted = !_app.Save.Muted; _app.Persist(); ApplySwitch(sw, !_app.Save.Muted); }, false); }
            }
            UiKit.Hide(rt, "SFX", "Haptic", "Language", "Group_Button_1", "Text_Privacy", "Text_TermsOfService");
            var g2 = UiKit.Find(rt, "Group_Button_2");
            if (g2 != null && g2.childCount >= 2)
            {
                UiKit.SetText(g2.GetChild(0), "Text (TMP)", "재개"); UiKit.Clickable(g2.GetChild(0), () => { Close(); onResume(); });
                UiKit.SetText(g2.GetChild(1), "Text (TMP)", "포기하고 로비로"); UiKit.Clickable(g2.GetChild(1), () => { Close(); onGiveUp(); });
            }
            var uid = UiKit.Find(rt, "UID"); if (uid != null) { UiKit.SetText(uid, "Text", $"세이브 ID  {_app.Save.Uid}"); UiKit.Hide(uid, "Icon"); }
            UiKit.SetText(rt, "Text_Version", "v" + Application.version);
            var close = UiKit.Find(rt, "Button_Close_01"); if (close != null) UiKit.Clickable(close, () => { Close(); onResume(); });
            var popup = UiKit.Find(rt, "Popup"); if (popup != null) UiKit.PopIn((RectTransform)popup);
        }
        static void ApplySwitch(Transform sw, bool on) { UiKit.Show(sw, "On", on); UiKit.Show(sw, "Off", !on); }

        // ───────────────────────── 보스 경고 띠 (Play_Warning_Boss 의 Panel_Warning) — 시간 안 멈춤 ─────────────────────────
        public void BossWarn(Transform parent)
        {
            var whole = UiKit.Spawn("ui.bossWarn", parent);
            var panel = UiKit.Find(whole.transform, "Panel_Warning");
            if (panel == null) { UnityEngine.Object.Destroy(whole); return; }
            panel.SetParent(parent, false); UnityEngine.Object.Destroy(whole);
            var prt = (RectTransform)panel; UiKit.Stretch(prt); prt.SetAsLastSibling();
            foreach (var g in panel.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            UiKit.SetText(panel, "Text (TMP)", "BOSS");
            var cg = panel.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0; cg.blocksRaycasts = false;
            DOTween.Sequence().Append(cg.DOFade(1, 0.2f)).AppendInterval(1.4f).Append(cg.DOFade(0, 0.4f)).OnComplete(() => UnityEngine.Object.Destroy(panel.gameObject)).SetUpdate(true);
            var w = UiKit.Find(panel, "Warning"); if (w != null) UiKit.PopIn((RectTransform)w, 1.3f, 0.35f);
        }

        public void Tick(float dt)
        {
            if (_countdown > 0)
            {
                _countdown -= dt;
                if (_countText != null) _countText.text = Mathf.CeilToInt(Mathf.Max(0, _countdown)).ToString();
                if (_countdown <= 0) { var cb = _onCountdown; _onCountdown = null; cb?.Invoke(); }
            }
        }
    }
}
