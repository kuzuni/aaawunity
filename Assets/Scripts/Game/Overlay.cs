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
            Audio.Sfx("snd.popup");   // 팝업 열림음은 여기 한 곳(T28) — 클리어/사망은 자기 징글을 덧붙인다
        }
        public void Close() { UiKit.Clear(Root); Root.gameObject.SetActive(false); _cur = null; _countdown = 0; }

        /// <summary>어둠 + 팝업 상자(Popup_Box_02 변형) + 리본 제목 = 공통 팝업 문법(<see cref="UiKit.Popup"/> · T36). 돌려주는 RectTransform 안에서 Pct 로 내용을 배치한다.
        /// <paramref name="onTapClose"/> 를 주면 프레임 아래 «탭하여 닫기» + 배경 탭으로 닫힌다(정보 팝업) · null 이면 선택을 강제하는 이벤트 팝업(쉼터·악마·천사).</summary>
        RectTransform Box(string popupKey, string titleKey, string title, Layout.R rect, Action onTapClose = null)
        {
            Begin();
            var parts = UiKit.Popup(Root, title, rect, onTapClose, popupKey, titleKey);
            _cur = parts.Box.gameObject;
            return parts.Box;
        }
        /// <summary>설명 글의 숫자(«+30%» · «33%» · «2초» …)만 초록으로(레퍼런스 04 «수치 초록» · T36). 리치 텍스트가 이미 있으면 그대로.</summary>
        static readonly System.Text.RegularExpressions.Regex NumRx = new System.Text.RegularExpressions.Regex(@"[+\-−]?\d[\d,]*(\.\d+)?(%|초|배|회|칸|x)?");
        public static string GreenNumbers(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('<') >= 0) return s;
            string hex = ColorUtility.ToHtmlStringRGB(Palette.Green);
            return NumRx.Replace(s, m => $"<color=#{hex}>{m.Value}</color>");
        }
        /// <summary>데모 프리팹 하나를 팝업 층에 그대로 세운다(Dimmed 가 있으면 클릭 차단·페이드).</summary>
        public GameObject OpenPrefab(string key)
        {
            Begin();
            var root = UiKit.Spawn(key, Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            var dim = UiKit.Find(rt, "Dimmed"); if (dim != null) { var di = dim.GetComponent<Image>(); if (di != null) { di.raycastTarget = true; UiKit.FadeIn(di, 0.85f); } }
            var bg = UiKit.Find(rt, "Background"); if (bg != null) { var bi = bg.GetComponent<Image>(); if (bi != null) bi.raycastTarget = true; }
            _cur = root;
            return root;
        }
        /// <summary>공통 팝업 상자를 연다 — onTapClose 를 주면 «탭하여 닫기» + 배경 탭으로 닫히는 정보 팝업(상점 뽑기 결과·확률 정보 · T40).</summary>
        public RectTransform OpenBox(string popupKey, string titleKey, string title, Layout.R rect, Action onTapClose = null) => Box(popupKey, titleKey, title, rect, onTapClose);

        Text Sub(RectTransform box, string s, float y = 9, float h = 7, int size = 36, Color? c = null)
            => UiKit.Label(box, 6, y, 88, h, s, size, c ?? Palette.InkSoft, TextAnchor.MiddleCenter, true, false);

        /// <summary>특전 카드 한 장(ListItem_StageBuff_02) — 등급 색으로 CardFrame_04/ItemFrame_04 를 갈아 끼운다(gray 는 무채색화).
        /// 글자는 등급 이름(리본)과 설명만 — 특전 이름은 넣지 않는다(주인 지시 2026-09-05 «제목은 빼고 일반 이라고만 · 내용만»).</summary>
        public RectTransform PerkCard(Transform parent, PerkDef p, string colorName, Action onClick)
        {
            var card = UiKit.Spawn("ui.card", parent); var rt = (RectTransform)card.transform;
            var frameArea = UiKit.Find(rt, "CardFrameArea"); var itemArea = UiKit.Find(rt, "ItemFrameArea");
            if (frameArea != null)
            {
                UiKit.Clear(frameArea); var f = UiKit.Spawn(Palette.FrameKey("ui.cardFrame", colorName), frameArea); var frt = (RectTransform)f.transform; UiKit.Stretch(frt);
                if (colorName == "gray") UiKit.Desaturate(frt);
                foreach (var old in frt.GetComponentsInChildren<Text>(true)) old.gameObject.SetActive(false);   // 프리팹의 남은 글자("Text_Title" 등) 전부 끄기 — 주인: «Text 라고 빨간 글씨 없애줘»
                var tb = UiKit.Find(frt, "TitleBg"); if (tb == null) tb = UiKit.Find(frt, "Text_Title");
                var host = tb != null ? tb : frt;
                var gl = UiKit.Text(host, p.GradeName ?? "", 30, Palette.White, TextAnchor.MiddleCenter, true);
                if (tb != null) UiKit.Stretch(gl.rectTransform, 8, 4, 8, 4); else UiKit.Pct(gl.rectTransform, 5, 0, 40, 22);
            }
            if (itemArea != null) { UiKit.Clear(itemArea); UiKit.PerkFrame(itemArea, colorName, Icons.Perk(p.Id), 162); }
            UiKit.Hide(rt, "Focus");
            var nameT = rt.Find("Text"); if (nameT != null) nameT.gameObject.SetActive(false);   // 카드 직계 "Text"(특전 이름) — 깊은 검색이면 프레임 안 글자에 잡힐 수 있어 직계로
            var desc = UiKit.SetText(rt, "Text_Value", GreenNumbers(p.Desc), Palette.Ink, 34);   // 수치만 초록(레퍼런스 04 · T36)
            if (desc != null) { desc.alignment = TextAnchor.MiddleLeft; var dr = desc.rectTransform; dr.anchorMin = new Vector2(0.24f, 0.08f); dr.anchorMax = new Vector2(0.97f, 0.92f); dr.offsetMin = dr.offsetMax = Vector2.zero; desc.resizeTextForBestFit = true; desc.resizeTextMaxSize = 34; desc.resizeTextMinSize = 18; desc.horizontalOverflow = HorizontalWrapMode.Wrap; }
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
            // 표 ⑦ 선택창 — 상자 없음 · 배너 20/26.5 · 부제 30/31.5 · 카드 x5.5 w89 h11 피치 13 · 하단 버튼 31/79 · 인포 86/79.5
            var ribbon = UiKit.Find(rt, "Title_01_NoDeco_Tangerine"); if (ribbon != null) UiKit.Pct((RectTransform)ribbon, Layout.OvBanner.X, Layout.OvBanner.Y - 0.7f, Layout.OvBanner.W, Layout.OvBanner.H + 1.4f);
            UiKit.SetText(rt, "Title_01_NoDeco_Tangerine/Text (TMP)", "레벨 업!");
            var sub = UiKit.Find(rt, "Text (TMP)"); if (sub != null) { UiKit.Pct((RectTransform)sub, Layout.OvSub); UiKit.SetText(rt, "Text (TMP)", "새 특전을 고르세요"); }   // 레퍼런스 04 «Choose a New Perk»
            var group = UiKit.Find(rt, "Group_Card");
            if (group != null)
            {
                UiKit.Clear(group); UiKit.Pct((RectTransform)group, Layout.OvCards);
                var vl = group.GetComponent<VerticalLayoutGroup>();
                if (vl != null) { vl.padding = new RectOffset(0, 0, 0, 0); vl.spacing = UiKit.FrameH * (Layout.OvCardPitch - Layout.OvCard1.H) / 100f; vl.childAlignment = TextAnchor.UpperCenter; vl.childForceExpandHeight = false; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandWidth = true; }
                foreach (var p in offer)
                {
                    var perk = p;
                    var card = PerkCard(group, perk, Palette.PerkGradeName(perk.Grade), () => { Close(); onPick(perk); });
                    var le = card.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = UiKit.FrameH * Layout.OvCard1.H / 100f;
                    UiKit.PopIn(card, 0.9f, 0.3f);
                }
            }
            // 주황 버튼 = «새로고침 무료»(3장 다시 굴림 · 팝업당 EngineConst.RerollPerLevelUp 번) + 그 아래 «남은 횟수 : N»(프리팹의 Remain 글자 자리 · 레퍼런스 04 «Refresh Free / Remain : 1» · T36). 보유 특전은 오른쪽 Book 으로.
            var btn = UiKit.Find(rt, "Button_02_Orange");
            if (btn != null)
            {
                UiKit.Pct((RectTransform)btn, Layout.OvFoot);
                int left = G.RerollsLeft;
                if (left <= 0) btn.gameObject.SetActive(false);   // 더 못 하면 숨긴다 (주인)
                else
                {
                    bool labeled = false; string hex = ColorUtility.ToHtmlStringRGB(Palette.Orange);
                    foreach (var t in btn.GetComponentsInChildren<Text>(true))
                    {
                        if (t.text != null && t.text.IndexOf("Remain", StringComparison.OrdinalIgnoreCase) >= 0) { t.text = $"남은 횟수 : <color=#{hex}>{left}</color>"; t.supportRichText = true; t.gameObject.SetActive(true); continue; }
                        if (!labeled) { t.text = "새로고침 무료"; labeled = true; } else t.gameObject.SetActive(false);
                    }
                    UiKit.Clickable(btn, () => { if (G.RerollOffer()) LevelUp(G, onPick); });
                }
            }
            var book = UiKit.Find(rt, "Book"); if (book != null) { UiKit.Pct((RectTransform)book, Layout.OvInfo); UiKit.SetText(book, "Text (TMP)", G.Taken.Count.ToString()); UiKit.Clickable(book, () => PerkBook(G, () => LevelUp(G, onPick))); }
            StatsRow(rt, G, Layout.OvStats);
            // T46 이름표(표 ⑦ 선택창 행 · «요소» 글자 그대로) — 하니스가 layout.json 으로 잰다
            if (ribbon != null) UiKit.Tag(ribbon, "배너(Level Up!)"); if (sub != null) UiKit.Tag(sub, "부제(Choose…)");
            if (group != null) for (int i = 0; i < group.childCount && i < 3; i++)
            {
                var c = group.GetChild(i); UiKit.Tag(c, "특전 카드 " + (i + 1));
                if (i == 0) { var ia = UiKit.Find(c, "ItemFrameArea"); if (ia != null) UiKit.Tag(ia, "카드 아이콘"); var tv = UiKit.Find(c, "Text_Value"); if (tv != null) UiKit.Tag(tv, "카드 문구"); }
            }
            if (btn != null && btn.gameObject.activeSelf) UiKit.Tag(btn, "하단 버튼"); if (book != null) UiKit.Tag(book, "인포(책) 버튼");
            var statsRow = UiKit.Find(rt, "Stats");
            if (statsRow != null)
            {
                var members = new List<RectTransform>(); foreach (Transform c in statsRow) if (c is RectTransform m) members.Add(m);   // [아이콘0, 값0, 아이콘1, 값1, …]
                UiKit.TagGroup(statsRow, "상단 스탯 줄(8칸)", members.ToArray());
                if (members.Count >= 2) UiKit.TagGroup(statsRow, "상단 스탯 칸(1칸)", members[0], members[1]);
            }
        }

        // ───────────────────────── 보유 특전 (PERKS) ─────────────────────────
        /// <summary>보유 특전 — 레퍼런스 05 구도(T36): 명판 «특전» + 긴 패널(BookBox) + 같은 카드 형식 세로 나열(스크롤) + 프레임 아래 «탭하여 닫기»(배경 탭 = 닫기 → <paramref name="onBack"/> · 닫기 버튼 없음). 같은 특전은 ×N.</summary>
        public void PerkBook(BattleState G, Action onBack)
        {
            var box = Box("ui.popup.blue", "ui.title.sky", "특전", Layout.BookBox, () => { Close(); onBack?.Invoke(); });   // 표 ⑦ 인포 팝업 y23 h52.5 · 리본 25/21.5 w50 h4 · 닫기 안내 y91.5(상자 밖)
            var rib = UiKit.Find(box, "ui.title.sky"); if (rib != null) { var rr = (RectTransform)rib; rr.sizeDelta = new Vector2(UiKit.FrameW * Layout.BookRibbon.W / 100f, UiKit.FrameH * Layout.BookRibbon.H / 100f + 20); }
            // 목록 — 같은 특전은 묶어 ×N
            var groups = new List<KeyValuePair<PerkDef, int>>();
            foreach (var p in G.Taken) { int i = groups.FindIndex(k => k.Key.Id == p.Id); if (i >= 0) groups[i] = new KeyValuePair<PerkDef, int>(p, groups[i].Value + 1); else groups.Add(new KeyValuePair<PerkDef, int>(p, 1)); }
            var cardIn = Layout.BookCard.Within(Layout.BookBox);
            var view = UiKit.Rect(box, "Scroll"); UiKit.Pct(view, cardIn.X, cardIn.Y, cardIn.W, 100 - cardIn.Y - 4);
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
                var le = card.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = UiKit.FrameH * Layout.BookCard.H / 100f;
                if (kv.Value > 1) { var n = UiKit.Text(card, "×" + kv.Value, 36, Palette.Yellow, TextAnchor.MiddleRight); UiKit.Pct(n.rectTransform, 80, 4, 18, 40); }
            }
            foreach (var b in G.Blessings) Sub(box, b, 93, 5, 24, Palette.Orange);
            var tap = UiKit.Find(Root, "TapToClose");
            if (onBack != null && tap != null) { var t = tap.GetComponent<Text>(); if (t != null) t.text = "탭하여 특전 선택으로"; }   // 레벨업에서 열었으면 배경 탭 = 선택으로 복귀
            // T46 이름표(표 ⑦ «(인포 팝업)» 행)
            UiKit.Tag(box, "(인포 팝업) 박스"); if (rib != null) UiKit.Tag(rib, "(인포 팝업) 제목 리본"); if (content.childCount > 0) UiKit.Tag(content.GetChild(0), "(인포 팝업) 목록 카드"); if (tap != null) UiKit.Tag(tap, "(인포 팝업) 닫기 안내");
        }

        // ───────────────────────── 쉼터 ─────────────────────────
        /// <summary>쉼터 — 체력 회복 / 경험치 / (T23) «광고 보고 둘 다 얻기»(광고 카운트다운 = 천사의 <see cref="AdCountdown"/> 재사용 → <paramref name="onBoth"/>).</summary>
        public void Rest(BattleState G, Action<bool> onChoose, Action onBoth = null)
        {
            var box = Box("ui.popup.green", "ui.title.green", "쉼터", Layout.EvBox);
            Sub(box, "모닥불 앞에서 잠시 쉬어갑니다", 9, 7, 34);
            var ic = UiKit.Icon(box, "Fire", "ui.fire"); UiKit.Pct(ic.rectTransform, 37, 17, 26, 24);
            string heal = G.C.RestHeal <= 1 ? $"최대 체력 {Math.Round(G.C.RestHeal * 100)}%" : $"체력 {UiKit.Fmt(G.C.RestHeal)}";
            UiKit.Button(box, "ui.btnGreen", $"체력 회복 (+{heal})", () => { Close(); onChoose(true); }, new Layout.R(10, 45, 80, 11));
            UiKit.Button(box, "ui.btnBlue", $"경험치 +{G.C.RestExp}", () => { Close(); onChoose(false); }, new Layout.R(10, 58, 80, 11));
            if (onBoth != null)
            {
                var ad = UiKit.Button(box, "ui.btnOrange", "광고 보고 둘 다 얻기", () => AdCountdown(3, () => { Close(); onBoth(); }), new Layout.R(10, 71, 80, 12));
                var adIc = UiKit.Icon(ad, "Ad", "hud.alertAd"); UiKit.Pct(adIc.rectTransform, 84, -22, 18, 50);
            }
            Sub(box, "다음 레벨에 가까워집니다", 86, 6, 26, Palette.InkLight);
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
        /// <summary>
        /// 클리어 팝업(Play_Result_Win_01 그대로) — T23(주인): 보상 표시는 <b>골드만</b>(프리팹의 나머지 두 보상 칸은 끈다) · 프리팹의 «Get x2»(광고 아이콘) 버튼 = «광고 보고 보상 ×2 받기»
        /// (광고 카운트다운 뒤 <paramref name="onDouble"/> → 골드 2배 · 로비로) · 프리팹의 «Home» 버튼 = «그냥 받기»(1배 · 로비로 · 승인 대기 28 기본값). «다음 챕터» 진입은 로비의 챕터 화살표로.
        /// </summary>
        public void Clear(BattleState G, bool last, Action onDouble, Action onLobby)
        {
            Begin(); Audio.Sfx("snd.clear");
            var root = UiKit.Spawn("ui.resultWin", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            var dim = UiKit.Find(rt, "Dimmed"); if (dim != null) { var di = dim.GetComponent<Image>(); if (di != null) { di.raycastTarget = true; UiKit.FadeIn(di, 0.85f); } }
            UiKit.SetText(rt, "Text", $"챕터 {G.Chapter}");
            UiKit.SetText(rt, "Text (1)", last ? "모든 챕터를 클리어했습니다!" : $"챕터 {G.Chapter + 1} 해금!");
            UiKit.SetText(rt, "Title_01_NoDeco_Tangerine/Text (TMP)", "클리어!");
            UiKit.SetText(rt, "Title_LineDeco_01_s_White/Text (TMP)", "클리어 보상");
            var items = UiKit.Find(rt, "Group_RewardItem");
            if (items != null && items.childCount >= 1)
            {
                Reward(items.GetChild(0), "ui.coin", UiKit.Fmt(G.Gold));
                for (int i = 1; i < items.childCount; i++) items.GetChild(i).gameObject.SetActive(false);   // 골드만(주인 T23) — 프리팹 칸을 옮기지 않고 끈다
            }
            UiKit.Hide(rt, "Text_TouchContionue");
            var grp = UiKit.Find(rt, "Group_Buttons");
            var b1 = grp != null && grp.childCount > 0 ? grp.GetChild(0) : null; var b2 = grp != null && grp.childCount > 1 ? grp.GetChild(1) : null;
            if (b1 != null) { UiKit.SetText(b1, "Text (TMP)", "광고 보고 보상 ×2 받기"); UiKit.Clickable(b1, () => AdCountdown(3, () => { Close(); onDouble(); })); }
            if (b2 != null) { UiKit.SetText(b2, "Text (TMP)", "그냥 받기"); UiKit.Clickable(b2, () => { Close(); onLobby(); }); }
            var title = UiKit.Find(rt, "Title"); if (title != null) UiKit.PopIn((RectTransform)title, 0.6f, 0.45f);
            UiKit.Hide(rt, "SampleEffect_Confetti");
        }
        static void Reward(Transform cell, string iconKey, string value) { UiKit.SetSprite(cell, "Icon", iconKey, Palette.White); UiKit.SetText(cell, "Text (TMP)", value); }

        // ───────────────────────── 사망 (Play_Result_Lose) ─────────────────────────
        public void Dead(BattleState G, Action onLobby)
        {
            Begin(); Audio.Sfx("snd.fail");
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

        // ───────────────────────── 설정 / 일시정지 — 레퍼런스 12_settings.jpg 구도 (T41 · 표 ⑨ · «Settings 프리팹 그대로»(T10) 는 부품 규칙으로 대체) ─────────────────────────
        /// <summary>로비 메뉴(≡)의 설정 팝업 — 작은 패널 · 명판 «설정» · 음악/효과음 토글 · 언어 버튼(표시만) · 패널 아래 개인정보/이용약관 링크 글자(눌러도 아무 일 없음) · 그 아래 «데이터 삭제»(T29) · «탭하여 닫기».</summary>
        public void Settings() => SettingsPopup("설정", null, null);
        /// <summary>전투 일시정지 — 같은 팝업. 링크 아래 줄이 «재개»(주황)·«포기하고 로비로»(회색) · 배경 탭 = 재개.</summary>
        public void Pause(Action onResume, Action onGiveUp) => SettingsPopup("일시정지", onResume, onGiveUp);

        /// <summary>
        /// 공통 팝업 문법(<see cref="UiKit.Popup"/>) 위에 표 ⑨ 자리로 조립한다(재료 = ui.popup 패널 · ui.titleBrown 명판 · pi.music/pi.sound/pi.globe 아이콘 · ui.switch(Swich_01 · On/Off 자식) · ui.btnGray/btnRed/btnOrange).
        /// 동작하는 것: 음악 = Save.MuteBgm · 효과음 = Save.MuteSfx(T28 · 각각 저장 + Audio 즉시 반영) · «데이터 삭제»(로비) · «재개»/«포기»(전투) · 배경 탭 = 닫기(전투에선 재개). 언어 버튼·링크 글자는 표시만.
        /// 이름 계약(AudioTests·UiSmokeTests): 줄 «BGM»·«SFX» 안에 «Swich_01».
        /// </summary>
        void SettingsPopup(string title, Action onResume, Action onGiveUp)
        {
            Action closeAndResume = () => { Close(); onResume?.Invoke(); };
            var box = Box("ui.popup", "ui.titleBrown", title, Layout.SetBox, closeAndResume);
            var rib = UiKit.Find(box, "ui.titleBrown"); if (rib != null) ((RectTransform)rib).sizeDelta = UiKit.PxSize(Layout.SetRibbon);
            // 줄 3개(음악 · 효과음 · 언어) — 아이콘 + 라벨 + 오른쪽 끝 토글/버튼. 자리 = 표 ⑨ 를 상자 기준 % 로
            RectTransform Row(string name, Layout.R r, string iconKey, string label)
            {
                var row = UiKit.Rect(box, name); UiKit.Pct(row, r.Within(Layout.SetBox));
                var ic = UiKit.Icon(row, "Icon", iconKey, Palette.Ink); UiKit.Pct(ic.rectTransform, 0, 5, 8, 90);
                var t = UiKit.Label(row, 11, 0, 50, 100, label, 40, Palette.Ink, TextAnchor.MiddleLeft, true, false); t.name = "Text";
                return row;
            }
            var bgm = Row("BGM", Layout.SetRowMusic, "pi.music", "음악");
            var sfx = Row("SFX", Layout.SetRowSound, "pi.sound", "효과음");
            var lang = Row("Language", Layout.SetRowLang, "pi.globe", "언어");
            // 토글 = Swich_01 조각(본래 크기 그대로 · 배율로 표 «토글» 칸에) — On/Off 자식으로 상태 표시(ApplySwitch)
            RectTransform Toggle(RectTransform row, Layout.R rowRect, Layout.R at, bool on, Action onClick)
            {
                var host = UiKit.Rect(row, "ToggleHost"); UiKit.Pct(host, at.Within(rowRect));   // 표 «토글» 자리(프레임 %) → 줄 기준 %
                var sw = UiKit.Spawn("ui.switch", host); var srt = (RectTransform)sw.transform; srt.name = "Swich_01";
                UiKit.FitScale(srt, UiKit.PxSize(at));   // 조각 본래 크기 그대로 · 배율로 칸에
                ApplySwitch(srt, on); UiKit.Clickable(srt, onClick, false);
                return srt;
            }
            RectTransform bsw = null, ssw = null;
            bsw = Toggle(bgm, Layout.SetRowMusic, Layout.SetToggle, !_app.Save.MuteBgm, () => { _app.Save.MuteBgm = !_app.Save.MuteBgm; _app.Persist(); Audio.ApplyMute(); ApplySwitch(bsw, !_app.Save.MuteBgm); });
            var sfxToggle = new Layout.R(Layout.SetToggle.X, Layout.SetToggle.Y + Layout.SetRowPitch, Layout.SetToggle.W, Layout.SetToggle.H);
            ssw = Toggle(sfx, Layout.SetRowSound, sfxToggle, !_app.Save.MuteSfx, () => { _app.Save.MuteSfx = !_app.Save.MuteSfx; _app.Persist(); Audio.ApplyMute(); ApplySwitch(ssw, !_app.Save.MuteSfx); });
            // 언어 버튼 = 회색 보조 버튼 «한국어»(표시만 · 언어 시스템 없음)
            var lb = UiKit.Button(box, "ui.btnGray", "한국어", () => { }, Layout.SetLangBtn.Within(Layout.SetBox)); lb.name = "LangBtn";
            // 패널 밖 아래 — 링크 글자 2(눌러도 아무 일 없음) · 그 아래 줄 = 로비: «데이터 삭제»(T29) / 전투: «재개»·«포기하고 로비로»
            var pv = UiKit.Text(Root, "개인정보 처리방침", 30, Palette.Sky, TextAnchor.MiddleCenter, false, true); pv.name = "Privacy"; pv.horizontalOverflow = HorizontalWrapMode.Overflow; UiKit.Pct(pv.rectTransform, Layout.SetPrivacy);   // 사각형 = 표 그대로(이름표) · 글자는 넘쳐도 된다
            var tm = UiKit.Text(Root, "이용약관", 30, Palette.Sky, TextAnchor.MiddleCenter, false, true); tm.name = "Terms"; tm.horizontalOverflow = HorizontalWrapMode.Overflow; UiKit.Pct(tm.rectTransform, Layout.SetTerms);
            RectTransform lastRow;
            if (onResume != null || onGiveUp != null)
            {
                lastRow = UiKit.Button(Root, "ui.btnOrange", "재개", closeAndResume, Layout.SetResumeBtn);
                UiKit.Button(Root, "ui.btnGray", "포기하고 로비로", () => { Close(); onGiveUp?.Invoke(); }, Layout.SetGiveUpBtn);
            }
            else lastRow = UiKit.Button(Root, "ui.btnRed", "데이터 삭제", ConfirmReset, Layout.SetReset);
            // T46 이름표(표 ⑨ «요소» 글자 그대로)
            UiKit.Tag(box, "팝업 박스"); if (rib != null) UiKit.Tag(rib, "제목 리본(Settings)");
            UiKit.Tag(bgm, "음악 줄"); UiKit.Tag(sfx, "효과음 줄"); UiKit.Tag(lang, "언어 줄");
            if (bsw != null && bsw.parent != null) UiKit.Tag(bsw.parent, "토글(1개)"); UiKit.Tag(lb, "언어 버튼");
            UiKit.Tag(pv.rectTransform, "개인정보 링크"); UiKit.Tag(tm.rectTransform, "이용약관 링크");
            if (onResume == null && onGiveUp == null) UiKit.Tag(lastRow, "데이터 삭제 버튼");
            var tapC = UiKit.Find(Root, "TapToClose"); if (tapC != null) UiKit.Tag(tapC, "닫기 안내");
        }
        static void ApplySwitch(Transform sw, bool on) { UiKit.Show(sw, "On", on); UiKit.Show(sw, "Off", !on); }

        /// <summary>
        /// «데이터 삭제» 확인 팝업(T29) — 빨간 Popup_Box + 리본 «데이터 삭제» + 경고 글 + «삭제»(빨강 · <see cref="App.ResetSave"/>) / «취소»(회색 · 설정 팝업으로 되돌아간다).
        /// 설정 팝업 위에 겹치지 않고 갈아 끼운다(Box 가 팝업 층을 비운다) — 취소하면 설정을 다시 연다.
        /// </summary>
        public void ConfirmReset()
        {
            var box = Box("ui.popup.red", "ui.title.red", "데이터 삭제", new Layout.R(6, 32, 88, 36));
            Sub(box, "정말 삭제할까요?", 16, 12, 40, Palette.Ink);
            Sub(box, "장비·골드·보석·진행이 모두 사라집니다.\n되돌릴 수 없습니다.", 34, 22, 30);
            UiKit.Button(box, "ui.btnRed", "삭제", () => _app.ResetSave(), new Layout.R(8, 66, 40, 18));
            UiKit.Button(box, "ui.btnGray", "취소", () => Settings(), new Layout.R(52, 66, 40, 18));
        }

        // ───────────────────────── 탤런트 / 펫 (주인 지정 Character_Talent_02 — 프리팹 «그대로» · 기능 없음 · T10) ─────────────────────────
        /// <summary>
        /// 하단 탭 «탤런트»·«펫» 팝업 — Character_Talent_02 데모 프리팹을 통째로(배경·재화 바·패스 줄·하단 탭 바) 그대로 세운다. 내용은 데모 그대로이고 기능은 없다(주인: «나중 업데이트»).
        /// 제목 = 프리팹 안 탭 바의 켜진 탭 라벨(«Talent» 자리) 을 «탤런트»/«펫» 으로. 닫기 = 프리팹의 하단 탭 바를 <see cref="NavBar.Wire"/> 로 배선해 다른 탭을 누르면 닫히며 그 화면으로 간다(프리팹에 닫기 버튼이 없어 새로 그리지 않는다).
        /// </summary>
        public void TalentPet(string kind)
        {
            Begin();
            var root = UiKit.Spawn("ui.talent", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            foreach (var g in rt.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = true;   // 뒤 화면으로 클릭이 새지 않게 (탭은 Clickable 이 다시 켠다)
            var res = UiKit.Find(rt, "ResourceBar_Group");
            if (res != null) { UiKit.SetText(res, "ResourceBar_Coin/Text (TMP)", UiKit.Fmt(_app.Save.Gold)); UiKit.SetText(res, "ResourceBar_Gem/Text (TMP)", UiKit.Fmt(_app.Save.Gem)); }
            var tabs = UiKit.Find(rt, "Tab_01_BottomFlushMenu");
            if (tabs != null) NavBar.Wire(_app, tabs, kind);
            _cur = root;
        }

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
