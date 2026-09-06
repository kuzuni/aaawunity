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
        /// <summary>열려 있는 동안 매 프레임(unscaled) 부르는 훅 — 팝업 안 «자정까지 남은 시간» 같은 1초 갱신용(T77). <see cref="Begin"/>·<see cref="Close"/> 가 비운다(다음 팝업으로 새지 않는다).</summary>
        public Action OnTick;
        Sequence _reveal;   // 등장 연출 마스터 시퀀스(T49) — 팝업마다 Begin 에서 새로, Close 에서 Kill
        readonly List<float> _shineStarts = new List<float>();   // T61 — 이번 팝업에서 shine 이 시작하는 시각(카드 순서대로 · 테스트가 단조 증가를 단언)
        /// <summary>이번 팝업의 카드 shine 시작 시각 목록(T61 · 카드 순서 = 반짝임 순서). Begin 마다 비운다.</summary>
        public IReadOnlyList<float> ShineStarts => _shineStarts;
        public bool IsOpen => Root.gameObject.activeSelf;
        /// <summary>등장 연출이 아직 도는 중인가(T49). 테스트는 <c>DOTween.CompleteAll(true)</c> 또는 <see cref="Skip"/> 뒤에 알파/스케일을 단언한다.</summary>
        public bool Revealing => _reveal != null && _reveal.IsActive() && !_reveal.IsComplete();
        /// <summary>연출 스킵 = 즉시 전부 표시(완료 콜백까지 · 클릭 열림).</summary>
        public void Skip() { if (_reveal != null && _reveal.IsActive()) _reveal.Complete(true); }
        Sequence Seq() { if (_reveal == null || !_reveal.IsActive()) _reveal = DOTween.Sequence().SetUpdate(true).SetTarget(Root).SetLink(Root.gameObject); return _reveal; }   // SetLink(T56) — 팝업 층이 통째로 파괴돼도(앱 종료) 경고 0
        void KillReveal() { if (_reveal != null && _reveal.IsActive()) _reveal.Kill(); _reveal = null; }
        /// <summary><paramref name="rt"/> 를 <paramref name="t"/> 초에 뜨게 한다(마스터 시퀀스에 Insert). 돌려주는 값 = 다 뜨는 시각.</summary>
        float At(float t, Transform rt, float from = UiKit.RevealFrom)
        {
            if (rt == null) return t;
            Seq().Insert(t, UiKit.Reveal((RectTransform)rt, from)); return t + UiKit.RevealDur;
        }

        public Overlay(App app)
        {
            _app = app;
            Root = UiKit.Rect(app.Frame, "Overlay"); UiKit.Stretch(Root);
            Root.gameObject.SetActive(false);
        }

        // ───────────────────────── 공통 ─────────────────────────
        void Begin()
        {
            KillReveal(); UiKit.Clear(Root); _shineStarts.Clear();
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            _countdown = 0; _onCountdown = null; _countText = null; OnTick = null;
            Audio.Sfx("snd.popup");   // 팝업 열림음은 여기 한 곳(T28) — 클리어/사망은 자기 징글을 덧붙인다
        }
        /// <summary>닫기 — 연출 시퀀스와 팝업 층 자식을 겨냥한 트윈을 전부 죽인 뒤 파괴한다(T49 · 파괴된 오브젝트를 만지는 트윈 0).</summary>
        public void Close() { KillReveal(); UiKit.Clear(Root); Root.gameObject.SetActive(false); _cur = null; _countdown = 0; OnTick = null; }

        /// <summary>어둠 + 팝업 상자(Popup_Box_02 변형) + 리본 제목 = 공통 팝업 문법(<see cref="UiKit.Popup"/> · T36). 돌려주는 RectTransform 안에서 Pct 로 내용을 배치한다.
        /// <paramref name="onTapClose"/> 를 주면 프레임 아래 «탭하여 닫기» + 배경 탭으로 닫힌다(정보 팝업) · null 이면 선택을 강제하는 이벤트 팝업(쉼터·악마·천사).</summary>
        RectTransform Box(string popupKey, string titleKey, string title, Layout.R rect, Action onTapClose = null)
        {
            Begin();
            var parts = UiKit.Popup(Root, title, rect, onTapClose, popupKey, titleKey);
            _cur = parts.Box.gameObject;
            return parts.Box;
        }
        /// <summary>
        /// 상자 없이 어둠 위에 바로 조립되는 <b>프리팹 팝업</b>(레벨업 3택 · 승리 · 사망)의 배경 무늬(T72 ①) — 어둠 조각 «Dimmed» 바로 위 형제에 흰 무늬를 깐다(어두운 바탕 = <see cref="UiKit.PatternTintDark"/>).
        /// 공통 팝업 상자는 <see cref="UiKit.Popup"/> 이 상자 «안» 에 깔아 주므로 여기서 부르지 않는다(무늬가 겹치지 않는다).
        /// </summary>
        static void DimPattern(RectTransform rt)
        {
            if (rt == null) return;
            int idx = 0;
            for (int i = 0; i < rt.childCount; i++) if (rt.GetChild(i).name == "Dimmed") { idx = i + 1; break; }
            UiKit.PatternBg(rt, UiKit.PatternTintDark, UiKit.PatternTileSeconds, idx);
        }
        /// <summary>보상 칸 그림 뒤 빛살(T72 ② · 작은 조각 <see cref="UiKit.LightKeySmall"/>) — 조각 rect 가 다 잡힌 <b>뒤</b>에 건다(그 전에는 한 변이 0 이 된다 · 결정 174).</summary>
        static void RewardLight(Transform cell)
        {
            if (cell == null) return;
            Canvas.ForceUpdateCanvases();
            UiKit.LightBehind((RectTransform)cell, UiKit.Find(cell, "Icon") as RectTransform, UiKit.LightKeySmall);
        }

        // (T36 의 «수치만 초록» GreenNumbers 는 주인 취소(2026-09-06 «연두색 섞여 있으면 안 읽힌다» · T52) — 특전 설명은 한 색(Palette.Ink) · 리치 텍스트 부분 색 없음)
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
        /// 글자는 등급 이름(리본)과 설명만 — 특전 이름은 넣지 않는다(주인 지시 2026-09-05 «제목은 빼고 일반 이라고만 · 내용만»).
        /// <paramref name="shine"/> = 프레임 조각에 T61 shine 머티리얼 인스턴스(순서대로 뜨는 3택·보유 특전 카드만 · 악마/천사의 한 장은 안 붙인다).</summary>
        public RectTransform PerkCard(Transform parent, PerkDef p, string colorName, Action onClick, bool shine = false)
        {
            var card = UiKit.Spawn("ui.card", parent); var rt = (RectTransform)card.transform;
            var frameArea = UiKit.Find(rt, "CardFrameArea"); var itemArea = UiKit.Find(rt, "ItemFrameArea");
            if (frameArea != null)
            {
                UiKit.Clear(frameArea); var f = UiKit.Spawn(Palette.FrameKey("ui.cardFrame", colorName), frameArea); var frt = (RectTransform)f.transform; UiKit.Stretch(frt);
                if (colorName == "gray") UiKit.Desaturate(frt);
                if (shine) UiKit.ShineMaterial(frt, rt);   // T61 — 프레임 그림(Border·Bg·InnerBorder·제목 탭)에만 · 글자·아이콘은 그대로
                foreach (var old in frt.GetComponentsInChildren<Text>(true)) old.gameObject.SetActive(false);   // 프리팹의 남은 글자("Text_Title" 등) 전부 끄기 — 주인: «Text 라고 빨간 글씨 없애줘»
                var tb = UiKit.Find(frt, "TitleBg"); if (tb == null) tb = UiKit.Find(frt, "Text_Title");
                var host = tb != null ? tb : frt;
                // 등급 이름(«일반»·«희귀»·«전설») — 밝은 탭(회색·노랑) 위 흰 글자는 대비가 없어 안 읽혔다(T63-perks · screens run 95 04/05) → 탭 밝기로 잉크/흰색
                var gl = UiKit.Text(host, p.GradeName ?? "", TextSize.Body, Palette.OnFrame(colorName), TextAnchor.MiddleCenter, true);
                // 위아래 여백 4px 를 빼면 글자 칸이 탭(48px)보다 8px 작아져 bestFit 이 본문 40 을 못 넣고 줄인다(T63-perks · screens run 95 실측 = 흰 채움 28px ≈ 37) → 세로는 탭 전체를 쓴다
                if (tb != null) UiKit.Stretch(gl.rectTransform, 8, 0, 8, 0); else UiKit.Pct(gl.rectTransform, 5, 0, 40, 22);
            }
            if (itemArea != null) { UiKit.Clear(itemArea); UiKit.PerkFrame(itemArea, colorName, Icons.Perk(p.Id), 162); }
            UiKit.Hide(rt, "Focus");
            var nameT = rt.Find("Text"); if (nameT != null) nameT.gameObject.SetActive(false);   // 카드 직계 "Text"(특전 이름) — 깊은 검색이면 프레임 안 글자에 잡힐 수 있어 직계로
            var desc = UiKit.SetText(rt, "Text_Value", PerkText.Format(p.Desc), Palette.Ink, TextSize.Body);   // 설명은 한 색(T52) · «트리거: 내용» · 상시는 «패시브: …»(T53 · 원문 perks.json 불변)
            if (desc != null) { desc.alignment = TextAnchor.MiddleLeft; var dr = desc.rectTransform; dr.anchorMin = new Vector2(0.24f, 0.08f); dr.anchorMax = new Vector2(0.97f, 0.92f); dr.offsetMin = dr.offsetMax = Vector2.zero; desc.resizeTextForBestFit = true; desc.resizeTextMaxSize = TextSize.Body; desc.resizeTextMinSize = TextSize.BestFitMin; desc.horizontalOverflow = HorizontalWrapMode.Wrap; }
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
                var v = UiKit.Label(row, i * cw, 54, cw, 40, d.Fmt(G), TextSize.Body, d.Up(G, _app.GetScreen<BattleScreen>()?.BaseStats) ? Palette.Green : Palette.White);
            }
        }

        // ───────────────────────── 레벨 업 3택 (주인 지정 Play_Perk_Selection_02) ─────────────────────────
        /// <summary>
        /// 레벨업 3택. 등장 연출(T49 · 주인 «특전 뜰 때 순서대로 DOTween»): 배경 페이드 → 리본 «레벨 업!»(0.05s) → 부제(0.15s) → <b>카드 3장이 위에서 아래로 하나씩</b>(0.22s 부터 0.11s 간격 · 스케일 0.86→1 + α 0→1 · OutBack)
        /// → 마지막에 «새로고침 무료»·«남은 횟수»·📘(0.55s) — 전부 0.77s 안(≤ 0.8s). 연출 중엔 카드가 클릭을 안 받고(<see cref="UiKit.Reveal"/> 가 raycast 를 막는다) <b>배경 탭 = 스킵</b>(즉시 전부 표시 · 워커 결정 83) · 연출이 끝나면 카드 선택. «새로고침» 도 같은 연출.
        /// </summary>
        public void LevelUp(BattleState G, Action<PerkDef> onPick)
        {
            Begin();
            var offer = G.Pending?.Offer ?? new List<PerkDef>();
            var root = UiKit.Spawn("ui.perkSelect", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            var dim = UiKit.Find(rt, "Dimmed"); if (dim != null) { var di = dim.GetComponent<Image>(); if (di != null) { di.raycastTarget = true; UiKit.FadeIn(di, 0.85f); } UiKit.OnTap(dim, () => { if (Revealing) Skip(); }); }
            DimPattern(rt);
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
                var cards = new List<RectTransform>();
                foreach (var p in offer)
                {
                    var perk = p;
                    var card = PerkCard(group, perk, Palette.PerkGradeName(perk.Grade), () => { Close(); onPick(perk); }, shine: true);
                    var le = card.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = UiKit.FrameH * Layout.OvCard1.H / 100f;
                    cards.Add(card);
                }
                UiKit.Stagger(Seq(), cards, 2 * UiKit.RevealStep, UiKit.RevealStep);   // 카드 = 0.22 · 0.33 · 0.44 → 0.66s 에 마지막이 다 뜬다
                UiKit.StaggerShine(Seq(), cards, 2 * UiKit.RevealStep, UiKit.RevealStep, _shineStarts);   // T61 — shine 도 같은 순서(0.30 · 0.41 · 0.52 시작 · 각 0.36s · 마지막 꼬리 0.88s)
            }
            At(0.05f, ribbon); At(0.15f, sub);
            float foot = 5 * UiKit.RevealStep;   // 0.55s — 하단 버튼·남은 횟수·📘 은 마지막 카드와 겹쳐 뜨며 0.77s 에 끝난다(≤ RevealMaxPick)
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
                        if (t.text != null && t.text.IndexOf("Remain", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            t.text = $"남은 횟수 : <color=#{hex}>{left}</color>"; t.supportRichText = true; t.gameObject.SetActive(true);
                            // 레퍼런스 04 는 «Remain : 1» 이 버튼 «아래» — 프리팹 자리 그대로 두면 버튼을 표 자리(OvFoot)로 키운 뒤 글자가 버튼 위에 얹혀 아랫줄이 반쯤 잘리고 주황 숫자가 주황 버튼에 묻힌다(T63-perks)
                            t.transform.SetParent(btn, false); UiKit.Pct(t.rectTransform, Layout.OvFootRemain); t.alignment = TextAnchor.MiddleCenter;
                            continue;
                        }
                        if (!labeled) { t.text = "새로고침 무료"; labeled = true; } else t.gameObject.SetActive(false);
                    }
                    UiKit.Clickable(btn, () => { if (G.RerollOffer()) LevelUp(G, onPick); });
                    At(foot, btn);
                }
            }
            var book = UiKit.Find(rt, "Book"); if (book != null) { UiKit.Pct((RectTransform)book, Layout.OvInfo); UiKit.SetText(book, "Text (TMP)", G.Taken.Count.ToString()); UiKit.Clickable(book, () => PerkBook(G, () => LevelUp(G, onPick))); At(foot, book); }
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
            if (groups.Count == 0) Sub(box, "아직 획득한 특전이 없습니다", 40, 8, TextSize.Body, Palette.Ink);   // 크림 패널 위 InkLight 는 대비가 모자란다(T63-perks · 지시서 T63 1항 «회색은 Ink 로»)
            var cards = new List<RectTransform>();
            foreach (var kv in groups)
            {
                var card = PerkCard(content, kv.Key, Palette.PerkGradeName(kv.Key.Grade), null, shine: true);
                var le = card.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = UiKit.FrameH * Layout.BookCard.H / 100f;
                // ×N 은 밝은 회색 카드 위 — 노랑은 대비가 없어 안 읽힌다(T63-perks)
                if (kv.Value > 1) { var n = UiKit.Text(card, "×" + kv.Value, TextSize.Body, Palette.Ink, TextAnchor.MiddleRight); UiKit.Pct(n.rectTransform, 80, 4, 18, 40); }
                cards.Add(card);
            }
            // T49 — 3택과 같은 stagger · 첫 화면(뷰포트 안)에 보이는 카드만 순서대로, 스크롤 밖은 즉시 표시. 상자 PopIn(0.28s) 뒤 0.15s 부터 · 전체 ≤ 0.8s 가 되게 간격을 줄인다.
            {
                float viewPx = UiKit.FrameH * Layout.BookBox.H / 100f * (100 - cardIn.Y - 4) / 100f, pitchPx = UiKit.FrameH * Layout.BookCard.H / 100f + vl.spacing;
                int visible = Mathf.Min(Mathf.FloorToInt(viewPx / pitchPx) + 1, cards.Count);
                if (visible > 0)
                {
                    float start = 0.15f, step = Mathf.Min(UiKit.RevealStep, (UiKit.RevealMaxPick - start - UiKit.RevealDur) / Mathf.Max(1, visible - 1));
                    UiKit.Stagger(Seq(), cards.GetRange(0, visible), start, step);
                    UiKit.StaggerShine(Seq(), cards.GetRange(0, visible), start, step, _shineStarts);   // T61 — 보이는 카드만 순서대로 shine(스크롤 밖 카드는 즉시 표시 · 빛 없음)
                }
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
            Sub(box, "모닥불 앞에서 잠시 쉬어갑니다", 9, 7, TextSize.Body);
            var ic = UiKit.Icon(box, "Fire", "ui.fire"); UiKit.Pct(ic.rectTransform, 37, 17, 26, 24);
            string heal = G.C.RestHeal <= 1 ? $"최대 체력 {Math.Round(G.C.RestHeal * 100)}%" : $"체력 {UiKit.Fmt(G.C.RestHeal)}";
            UiKit.Button(box, "ui.btnGreen", $"체력 회복 (+{heal})", () => { Close(); onChoose(true); }, new Layout.R(10, 45, 80, 11));
            UiKit.Button(box, "ui.btnBlue", $"경험치 +{G.C.RestExp}", () => { Close(); onChoose(false); }, new Layout.R(10, 58, 80, 11));
            if (onBoth != null)
            {
                var ad = UiKit.Button(box, "ui.btnOrange", "광고 보고 둘 다 얻기", () => AdCountdown(3, () => { Close(); onBoth(); }), new Layout.R(10, 71, 80, 12));
                var adIc = UiKit.Icon(ad, "Ad", "hud.alertAd"); UiKit.Pct(adIc.rectTransform, 84, -22, 18, 50);
            }
            Sub(box, "다음 레벨에 가까워집니다", 86, 6, TextSize.Body, Palette.InkSoft);   // T63-perks — 밝은 패널 위 InkLight → InkSoft
        }

        // ───────────────────────── 악마의 거래 ─────────────────────────
        public void Devil(BattleState G, Action<bool> onChoose)
        {
            var perk = G.Pending?.DevilPerk;
            var box = Box("ui.popup.plum", "ui.title.plum", "악마의 거래", new Layout.R(4, 24, 92, 52));
            Sub(box, "\"네 생명을 바치면... 이 힘을 주지\"", 9, 6, TextSize.Body, Palette.Plum);
            if (perk != null) { var card = PerkCard(box, perk, "yellow", null); UiKit.Pct(card, 2, 18, 96, 22); }
            double cost = G.C.DevilCostMaxHp > 0 ? G.C.DevilCostMaxHp : G.PK.DevilCostMaxHp;
            Sub(box, $"최대 체력이 {Math.Round(cost * 100)}% 줄어든 채 진행 · 위 전설 특전 1개를 획득", 43, 10, TextSize.Body, Palette.InkSoft);
            UiKit.Button(box, "ui.btnRed", "거래 수락", () => { Close(); onChoose(true); }, new Layout.R(8, 60, 40, 13));
            UiKit.Button(box, "ui.btnGray", "거절", () => { Close(); onChoose(false); }, new Layout.R(52, 60, 40, 13));
        }
        public void DevilGift(PerkDef perk, Action onOk)
        {
            var box = Box("ui.popup.plum", "ui.title.plum", "악마의 선물", new Layout.R(6, 30, 88, 40));
            Sub(box, "전설 특전을 얻었습니다", 11, 7, TextSize.Body, Palette.Plum);
            if (perk != null) { var card = PerkCard(box, perk, "yellow", null); UiKit.Pct(card, 2, 24, 96, 28); }
            UiKit.Button(box, "ui.btnOrange", "계속", () => { Close(); onOk?.Invoke(); }, new Layout.R(25, 66, 50, 16));
        }

        // ───────────────────────── 천사의 축복 ─────────────────────────
        public void Angel(BattleState G, Action<double> onChoose)
        {
            var box = Box("ui.popup.yellow", "ui.title.yellow", "천사의 축복", Layout.EvBox);
            Sub(box, "\"용사여, 축복을 내리노라\"", 10, 7, TextSize.Body, Palette.Orange);
            var ic = UiKit.Icon(box, "Wing", "pi.wing", Palette.Yellow); UiKit.Pct(ic.rectTransform, 35, 20, 30, 26);
            UiKit.Button(box, "ui.btnGreen", $"무료 축복 · 공격력 +{Math.Round((SimPolicy.AngelFree - 1) * 100)}%", () => { Close(); onChoose(SimPolicy.AngelFree); }, new Layout.R(10, 54, 80, 12));
            var ad = UiKit.Button(box, "ui.btnOrange", $"광고 보고 공격력 +{Math.Round((SimPolicy.AngelAd - 1) * 100)}%", () => AdCountdown(3, () => Blessed(onChoose)), new Layout.R(10, 70, 80, 12));
            var adIc = UiKit.Icon(ad, "Ad", "hud.alertAd"); UiKit.Pct(adIc.rectTransform, 84, -22, 18, 50);
            Sub(box, "더 강한 축복", 85, 6, TextSize.Body, Palette.InkSoft);   // T63-perks — 밝은 패널 위 InkLight → InkSoft
        }
        void Blessed(Action<double> onChoose)
        {
            var box = Box("ui.popup.yellow", "ui.title.yellow", "축복 강화!", new Layout.R(6, 32, 88, 36));
            Sub(box, $"공격력이 {Math.Round((SimPolicy.AngelAd - 1) * 100)}% 증가했습니다", 18, 14, TextSize.Body, Palette.Orange);
            UiKit.Button(box, "ui.btnOrange", "계속 전진", () => { Close(); onChoose(SimPolicy.AngelAd); }, new Layout.R(25, 62, 50, 18));
        }
        /// <summary>광고 카운트다운 숫자 크기 — 본문 하한(40)보다 «크게» 보여야 뜻이 있는 자리다(칸 30% = 196px 에 한 줄 88px). T63-results.</summary>
        public const int AdCountSize = 72;
        /// <summary>광고 자리(모의) — 3초 카운트다운 (T3 결정 «광고는 3초 카운트다운으로 대체»).</summary>
        public void AdCountdown(int seconds, Action onDone)
        {
            var box = Box("ui.popup", "ui.title.tangerine", "광고 시청 중...", new Layout.R(10, 36, 80, 28));
            var ic = UiKit.Icon(box, "Ad", "ui.ad"); UiKit.Pct(ic.rectTransform, 38, 18, 24, 34);
            _countText = Sub(box, seconds.ToString(), 56, 30, AdCountSize, Palette.Ink);
            _countdown = seconds; _onCountdown = onDone;
        }

        // ───────────────────────── 클리어 (주인 지정 Play_Result_Win_01) ─────────────────────────
        /// <summary>
        /// 클리어 팝업(Play_Result_Win_01 그대로) — T23(주인): 보상 표시는 <b>골드만</b>(프리팹의 나머지 두 보상 칸은 끈다) · 프리팹의 «Get x2»(광고 아이콘) 버튼 = «광고 보고 ×2»(<see cref="ClearAdLabel"/>)
        /// (광고 카운트다운 뒤 <paramref name="onDouble"/> → 골드 2배 · 로비로) · 프리팹의 «Home» 버튼 = «그냥 받기»(1배 · 로비로 · 승인 대기 28 기본값). «다음 챕터» 진입은 로비의 챕터 화살표로.
        /// </summary>
        /// <remarks>등장 연출(T49 · 주인 «이겼을 때 팝업도 같은 식»): 배경 → 제목 «클리어!»(0.05s · 스케일 0.6→1) → «챕터 N»·해금 문구(0.2s) → 보상 띠 + 골드 칸(0.35s · 숫자 0→G.Gold 카운트업 0.4s) → 버튼 2개가 <b>순서대로</b>(광고 ×2 0.6s · 그냥 받기 0.72s · 스케일+페이드 — 버튼 줄은 HorizontalLayoutGroup 이라 위치 트윈 대신 · 워커 결정 84) → 0.94s 에 끝(≤ 1.0s). 배경 탭 = 연출 중이면 스킵(닫히지는 않는다 — 선택 강제). 컨페티는 그대로 숨김.</remarks>
        /// <summary>클리어 팝업의 광고 ×2 버튼 글자(T23) — 프리팹 버튼 322×130 의 글자 칸이 300×100 이라 «광고 보고 보상 ×2 받기»(버튼 하한 44 로 ≈435px) 는 두 줄로 접혔다.
        /// 글자를 줄이지 않고 문구를 줄여 한 줄에 넣는다(T63 3항 순서 ⓒ · 테스트 라벨도 이 상수를 쓴다).</summary>
        public const string ClearAdLabel = "광고 보고 ×2";
        public void Clear(BattleState G, bool last, Action onDouble, Action onLobby)
        {
            Begin(); Audio.Sfx("snd.clear");
            var root = UiKit.Spawn("ui.resultWin", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            var dim = UiKit.Find(rt, "Dimmed"); if (dim != null) { var di = dim.GetComponent<Image>(); if (di != null) { di.raycastTarget = true; UiKit.FadeIn(di, 0.85f); } UiKit.OnTap(dim, () => { if (Revealing) Skip(); }); }
            DimPattern(rt);
            var chap = UiKit.SetText(rt, "Text", $"챕터 {G.Chapter}");
            var unlock = UiKit.SetText(rt, "Text (1)", last ? "모든 챕터 클리어!" : $"챕터 {G.Chapter + 1} 해금!");   // 프리팹 칸 528×61 — 본문 40 한 줄에 들어가는 길이로(T63-results)
            UiKit.SetText(rt, "Title_01_NoDeco_Tangerine/Text (TMP)", "클리어!");
            UiKit.SetText(rt, "Title_LineDeco_01_s_White/Text (TMP)", "클리어 보상");
            var items = UiKit.Find(rt, "Group_RewardItem"); Text goldText = null; Transform goldCell = null;
            if (items != null && items.childCount >= 1)
            {
                goldCell = items.GetChild(0);
                goldText = Reward(goldCell, "ui.coin", UiKit.Fmt(G.Gold));
                for (int i = 1; i < items.childCount; i++) items.GetChild(i).gameObject.SetActive(false);   // 골드만(주인 T23) — 프리팹 칸을 옮기지 않고 끈다
            }
            UiKit.Hide(rt, "Text_TouchContionue");
            var grp = UiKit.Find(rt, "Group_Buttons");
            var b1 = grp != null && grp.childCount > 0 ? grp.GetChild(0) : null; var b2 = grp != null && grp.childCount > 1 ? grp.GetChild(1) : null;
            if (b1 != null) { UiKit.SetText(b1, "Text (TMP)", ClearAdLabel); UiKit.Clickable(b1, () => AdCountdown(3, () => { Close(); onDouble(); })); }
            if (b2 != null) { UiKit.SetText(b2, "Text (TMP)", "그냥 받기"); UiKit.Clickable(b2, () => { Close(); onLobby(); }); }
            UiKit.Hide(rt, "SampleEffect_Confetti");
            // 순서 — 제목 → 챕터/해금 → 보상(카운트업) → 버튼 2 순서대로
            At(0.05f, UiKit.Find(rt, "Title"), 0.6f);
            if (chap != null) At(0.2f, chap.transform); if (unlock != null) At(0.2f, unlock.transform);
            At(0.35f, UiKit.Find(rt, "Title_LineDeco_01_s_White")); if (items != null) At(0.35f, items);
            if (goldText != null && G.Gold > 0)
            {
                double v = 0, target = Math.Round(G.Gold); goldText.text = UiKit.Fmt(0);
                Seq().Insert(0.35f, DOTween.To(() => v, x => { v = x; if (goldText != null) goldText.text = UiKit.Fmt(x); }, target, 0.4f).SetEase(Ease.OutQuad).SetTarget(goldText).SetLink(goldText.gameObject));
            }
            At(0.6f, b1); At(0.72f, b2);
            // T72 ② 클리어 보상(골드) 그림 뒤 빛살 — 배치가 끝난 뒤(결정 174)
            RewardLight(goldCell);
        }
        static Text Reward(Transform cell, string iconKey, string value)
        {
            UiKit.SetSprite(cell, "Icon", iconKey, Palette.White);
            var t = UiKit.SetText(cell, "Text (TMP)", value);
            // 값 칸은 프리팹이 좌우 15px 씩 비워 121px 뿐이라 «12.3K» 가 본문 40 에 안 들어간다(bestFit 이 30 대로 내림) → 여백만 2px 로 줄여 147px (칸 151 · 아이콘·자리 불변 · T63-results)
            if (t != null) { var r = t.rectTransform; r.offsetMin = new Vector2(2f, r.offsetMin.y); r.offsetMax = new Vector2(-2f, r.offsetMax.y); }
            return t;
        }

        // ───────────────────────── 사망 (Play_Result_Lose) ─────────────────────────
        /// <summary>사망 팝업. 등장 연출(T49 · 주인 «졌을 때 팝업도»): 배경 → «쓰러졌다...»(0.05s) → 보상 골드(0.2s) → 팁 3줄이 <b>한 줄씩</b>(0.35 · 0.46 · 0.57s) → «로비로»(0.68s) → «터치하면 로비로»(0.76s) → 0.98s 에 끝(≤ 1.0s). <b>배경 탭 = 연출 중이면 스킵</b>(즉시 전부 표시) · 끝난 뒤면 로비로.</summary>
        public void Dead(BattleState G, Action onLobby)
        {
            Begin(); Audio.Sfx("snd.fail");
            var root = UiKit.Spawn("ui.resultLose", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            var dim = UiKit.Find(rt, "Dimmed"); if (dim != null) { var di = dim.GetComponent<Image>(); if (di != null) { di.raycastTarget = true; UiKit.FadeIn(di, 0.85f); } }
            DimPattern(rt);
            UiKit.SetText(rt, "Title_LineDeco_01_s_White/Text (TMP)", "쓰러졌다...");
            var reward = UiKit.Find(rt, "Reward"); if (reward != null) Reward(reward, "ui.coin", UiKit.Fmt(G.Gold));
            var list = UiKit.Find(rt, "Group_List");
            string[] tips = { $"처치 {G.Kills} · 골드 {UiKit.Fmt(G.Gold)} 획득", "골드로 장비를 강화해 다시 도전!", "장비 3개를 합성하면 등급이 오릅니다" };   // 줄 글자 칸 730×82 — 셋 다 본문 40 한 줄(T63-results)
            string[] icons = { "ui.skull", "ui.anvil", "ui.bookRed" };
            var rows = new List<RectTransform>();
            if (list != null) for (int i = 0; i < list.childCount && i < tips.Length; i++) { UiKit.SetText(list.GetChild(i), "Text (TMP)", tips[i]); UiKit.SetSprite(list.GetChild(i), "Icon", icons[i], Palette.White); rows.Add((RectTransform)list.GetChild(i)); }
            var touch = UiKit.SetText(rt, "Text_TouchContionue", "터치하면 로비로");
            var lobbyBtn = UiKit.Button(rt, "ui.btnBlue", "로비로", () => { Close(); onLobby(); }, new Layout.R(30, 80, 40, 6));
            var hit = UiKit.Find(rt, "Dimmed"); if (hit != null) UiKit.Clickable(hit, () => { if (Revealing) Skip(); else { Close(); onLobby(); } }, false);
            // 순서 — 제목 → 보상 → 팁 한 줄씩 → 로비로 → 터치 안내
            At(0.05f, UiKit.Find(rt, "Title_LineDeco_01_s_White")); At(0.2f, reward);
            float tipsEnd = UiKit.Stagger(Seq(), rows, 0.35f, UiKit.RevealStep);   // 0.35 · 0.46 · 0.57 → 0.79
            At(tipsEnd - UiKit.RevealStep, lobbyBtn); if (touch != null) At(tipsEnd - 0.03f, touch.transform);
            // T72 ② 사망 보상(골드) 그림 뒤 빛살 — 배치가 끝난 뒤(결정 174)
            RewardLight(reward);
        }

        // ───────────────────────── 설정 / 일시정지 — 레퍼런스 12_settings.jpg 구도 (T41 · 표 ⑨ · «Settings 프리팹 그대로»(T10) 는 부품 규칙으로 대체) ─────────────────────────
        /// <summary>로비 메뉴(≡)의 설정 팝업 — 작은 패널 · 명판 «설정» · 음악/효과음 토글 · 언어 버튼(표시만) · 패널 아래 개인정보/이용약관 링크 글자(눌러도 아무 일 없음) · 그 아래 «데이터 삭제»(T29) · «탭하여 닫기».</summary>
        /// <summary>설정 줄 라벨(음악·효과음·언어) 크기 — 레퍼런스 12 는 라벨 잉크가 명판 «Settings» 와 거의 같다(720×1560 사본 실측 ≈ 39px = 프레임 ≈ 58). 본문 하한 40 은 그 2/3 라 폰에서 15px 밖에 안 된다 → 56(줄 칸 88.8px 에 한 줄 70px · 폰 ≈ 21px). T63-settings.</summary>
        public const int SetRowLabelSize = 56;

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
                var t = UiKit.Label(row, 11, 0, 50, 100, label, SetRowLabelSize, Palette.Ink, TextAnchor.MiddleLeft, true, false); t.name = "Text";
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
            // 링크 2 = 본문 하한(T63-settings · 30 이 하한으로 올라가던 것을 명시) · 사각형은 글자를 담는다(전에는 표 그대로라 «개인정보 처리방침» 268px 이 219px 칸에서 좌우로 넘쳤다 → Layout.SetPrivacy 보정)
            var pv = UiKit.Text(Root, "개인정보 처리방침", TextSize.Body, Palette.Sky, TextAnchor.MiddleCenter, false, true); pv.name = "Privacy"; pv.horizontalOverflow = HorizontalWrapMode.Overflow; UiKit.Pct(pv.rectTransform, Layout.SetPrivacy);
            var tm = UiKit.Text(Root, "이용약관", TextSize.Body, Palette.Sky, TextAnchor.MiddleCenter, false, true); tm.name = "Terms"; tm.horizontalOverflow = HorizontalWrapMode.Overflow; UiKit.Pct(tm.rectTransform, Layout.SetTerms);
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
            // 크기는 종류로만(§1) · 가운뎃점 «·» 은 Jua 에 글리프가 없어 폭 0 으로 사라진다(«장비골드보석진행이…») → 쉼표 열거로(T63-toast)
            Sub(box, "정말 삭제할까요?", 16, 12, TextSize.Body, Palette.Ink);
            Sub(box, "장비, 골드, 보석, 진행이 모두 사라집니다.\n되돌릴 수 없습니다.", 34, 22, TextSize.Body);
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
            // 영문 데모 문구 0(T34 ⓒ) · 제목 종류로 표식(게이트 판정) — 프리팹 크기 100 은 그대로 두고, 글자 칸 445.7×141.9 에 «보스» 한 줄(159×140px)이 든다
            UiKit.SetText(panel, "Text (TMP)", "보스", kind: TextKind.Title);
            var cg = panel.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0; cg.blocksRaycasts = false;
            DOTween.Sequence().Append(cg.DOFade(1, 0.2f)).AppendInterval(1.4f).Append(cg.DOFade(0, 0.4f)).OnComplete(() => UnityEngine.Object.Destroy(panel.gameObject)).SetUpdate(true).SetLink(panel.gameObject);   // SetLink(T56) — 전투가 먼저 끝나 HUD 가 파괴되면 띠 트윈도 같이
            var w = UiKit.Find(panel, "Warning"); if (w != null) UiKit.PopIn((RectTransform)w, 1.3f, 0.35f);
        }

        public void Tick(float dt)
        {
            if (IsOpen) OnTick?.Invoke();
            if (_countdown > 0)
            {
                _countdown -= dt;
                if (_countText != null) _countText.text = Mathf.CeilToInt(Mathf.Max(0, _countdown)).ToString();
                if (_countdown <= 0) { var cb = _onCountdown; _onCountdown = null; cb?.Invoke(); }
            }
        }
    }
}
