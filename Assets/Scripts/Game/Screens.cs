using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 로비 = <b>docs/ref/01_lobby.jpg 구도</b>(T34 · 주인 지시 2026-09-06 «UI 는 무조건 레퍼런스 jpg 기준» — «Lobby_Default 그대로»(T6) 를 대체).
    /// 배치의 정본 = <see cref="Layout"/> ① 표(ref-layout.md · 프레임 % · ±3%p). 그림 재료는 주인 에셋만 — Lobby_Default 프리팹은 <b>부품 창고</b>로 쓴다
    /// (배경+Deco · 메뉴(≡) · 챕터 제목 조각 · 탭 바를 뜯어 표 자리에 놓고, 나머지 조각은 끈다 · 코드 도형 없음).
    /// <list type="bullet">
    /// <item>상단 재화 바 = <see cref="TopBar"/>(아바타 = HeroView 가슴 위 · 전투력(칼 + 주황 숫자) · 골드 pill · 보석 pill) — 장비·상점·펫·던전 화면(T37·T40·T42·T43)도 같은 헬퍼를 쓴다.</item>
    /// <item>이벤트 배너(보라 · 진행바 · 레벨 배지 · 패스 껍데기) + 메뉴(≡ = 설정 팝업).</item>
    /// <item>왼쪽 세로 아이콘 3(스타터팩·특권·7일 챌린지) / 오른쪽 3(출석·데일리 기프트·퀘스트) — 어두운 반투명 기둥 + 아이콘 + 라벨 · 전부 <see cref="OnSide"/> 훅 하나로(T44 가 채울 때까지 아무 일 없음).</item>
    /// <item>«챕터 N» 제목(프리팹 Title_LineDeco 조각 · 밑줄 포함) → 챕터 카드(어두운 테두리 + 이번 챕터 전투 맵 테마의 Environment 바닥·길·소품) + ◀▶ → 보조 버튼 2(탐험·클리어 보상 · 껍데기) → START(주황 · 카드와 같은 폭) → 왼쪽 아래 성(잠금) · 오른쪽 아래 이벤트(T43 진입) → 탭 바.</item>
    /// </list>
    /// </summary>
    public sealed class LobbyScreen : GameScreen
    {
        public override string Name => "lobby";
        /// <summary>사이드·보조·모서리 버튼 키 — T43(events) · T44(나머지) 가 <see cref="OnSide"/> 에서 이어받는다.</summary>
        public const string SideStarter = "starter", SidePrivilege = "privilege", SideChallenge7 = "challenge7", SideAttendance = "attendance", SideDailyGift = "dailyGift", SideQuest = "quest";
        public const string SidePass = "pass", SideExplore = "explore", SideClearReward = "clearReward", SideCastle = "castle", SideEvents = "events";
        /// <summary>챕터 카드 안 소품 개수(레퍼런스 카드 = 나무 2 · 돌 2 · 덤불) · 카드 안 길 띠(세로 %) · 소품 자리(카드 % · 왼쪽 위/오른쪽 위/왼쪽 아래/오른쪽 아래).</summary>
        const int CardProps = 4;
        static readonly Layout.R CardRoad = new Layout.R(0, 52, 100, 20);
        static readonly Layout.R[] CardPropSlots = { new Layout.R(6, 6, 30, 46), new Layout.R(62, 4, 32, 50), new Layout.R(10, 56, 24, 40), new Layout.R(66, 58, 26, 38) };

        TopBar _top; Text _chap; Transform _tabs;
        RectTransform _card; Image _cardField, _cardRoad; readonly Image[] _cardProps = new Image[CardProps]; string _cardTheme;

        protected override void Build()
        {
            var root = UiKit.Spawn("ui.lobby", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            // 프리팹은 부품 창고 — 쓰지 않는 조각은 끈다(상단 바·사이드 버튼·샘플 지도·빨간 START·채팅·부제)
            UiKit.Hide(rt, "ChatBox", "Group_LeftButtons", "Group_RightButtons", "SampleImage_Map", "ResourceBar_Group");
            var oldInfo = UiKit.FindAny(rt, "UserInfo_01", "UserInfo_01_Slider"); if (oldInfo != null) oldInfo.gameObject.SetActive(false);
            var oldStart = UiKit.FindAny(rt, "Button_03_Red", "Button_03_Convex_Red"); if (oldStart != null) oldStart.gameObject.SetActive(false);
            var subT = UiKit.Find(rt, "Text (TMP)"); if (subT != null && subT.parent == rt) subT.gameObject.SetActive(false);
            // 배경 = 프리팹 Background(평면색 + 흐린 칼 Deco 패턴 — 레퍼런스의 초록 바탕·칼 무늬와 같은 구성) · 색만 초록으로(색은 점수 밖 · 느낌만)
            var bg = UiKit.Find(rt, "Background"); var bgImg = bg != null ? bg.GetComponent<Image>() : null;
            if (bgImg != null) { bgImg.color = Color.Lerp(Palette.Green, Palette.Ink, 0.42f); bgImg.raycastTarget = true; }

            // ① 상단 재화 바 (아바타 · 전투력 · 골드 · 보석) — 공용 헬퍼 · 비평 이름표(T46 · ref-layout ① 의 «요소» 이름 그대로)
            _top = TopBar.Build(App, rt);
            UiKit.Tag(_top.Root, "상단 바(아바타+재화 줄 전체)"); UiKit.Tag(_top.Avatar, "아바타(정사각)"); UiKit.TagGroup(_top.Root, "재화 pill 줄", _top.PowerCell, _top.GoldPill, _top.GemPill);

            // ② 이벤트 배너(보라 · 패스 껍데기) + 메뉴(≡)
            var banner = UiKit.Spawn("ui.framePlum", rt); var brt = (RectTransform)banner.transform; brt.name = "Banner"; UiKit.Pct(brt, Layout.LobbyBanner);
            {
                var medal = UiKit.Icon(brt, "Icon", "ui.iconMedal"); UiKit.Pct(medal.rectTransform, 3, 12, 17, 76);
                UiKit.Label(brt, 22, 6, 58, 42, "시즌 패스", 34, Palette.White, TextAnchor.MiddleLeft);
                var bar = UiKit.MakeBar(brt, "ui.sliderGreen"); UiKit.Pct(bar.Root, 22, 54, 56, 36); bar.Set(0, "준비 중");
                var badge = UiKit.Panel(brt, "Badge", "fr.circle", Palette.Ink); UiKit.Pct(badge.rectTransform, 82, 10, 15, 80);
                var arf = UiKit.Ensure<AspectRatioFitter>(badge.gameObject); arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent; arf.aspectRatio = 1f;
                UiKit.Label(badge.transform, 0, 0, 100, 100, "1", 40, Palette.White);
                UiKit.Clickable(brt, () => OnSide(SidePass));
                UiKit.Tag(brt, "이벤트 배너");
            }
            var menu = UiKit.Find(rt, "Button_Menu");
            if (menu != null) { var mrt = (RectTransform)menu; mrt.SetParent(rt, false); UiKit.Pct(mrt, Layout.LobbyMenu); UiKit.Clickable(mrt, () => App.Overlay.Settings()); UiKit.Tag(mrt, "메뉴(☰) 버튼"); }

            // ③ 사이드 아이콘 기둥 — 왼쪽 3 · 오른쪽 3 (레퍼런스 순서)
            UiKit.Tag(BuildColumn(rt, "SideL", Layout.LobbySideL, false, (SideStarter, "ui.iconGiftRed", "스타터팩"), (SidePrivilege, "ui.iconCrown", "특권"), (SideChallenge7, "ui.iconTarget", "7일 챌린지")), "좌 사이드 아이콘 열(3개)");
            UiKit.Tag(BuildColumn(rt, "SideR", Layout.LobbySideR, false, (SideAttendance, "ui.iconCalendar", "출석"), (SideDailyGift, "ui.iconBalloon", "데일리 기프트"), (SideQuest, "ui.iconQuest", "퀘스트")), "우 사이드 아이콘 열(3개)");

            // ④ 챕터 제목(프리팹 Title_LineDeco 조각 = 글자 + 밑줄 장식 · 표의 제목 행 ∪ 밑줄 행 자리)
            var title = UiKit.FindAny(rt, "Title_LineDeco_01_Blue", "Title_LineDeco_01_l");
            if (title != null)
            {
                var trt = (RectTransform)title; trt.SetParent(rt, false);
                var t = Layout.LobbyChapTitle; var u = Layout.LobbyChapUnderline;
                UiKit.Pct(trt, u.X, t.Y, u.W, u.Y + u.H - t.Y);
                _chap = UiKit.SetText(trt, "Text (TMP)", "챕터 1");
                if (_chap != null) UiKit.Tag(_chap.transform, "챕터 제목"); UiKit.Tag(UiKit.Find(trt, "LineDeco"), "챕터 밑줄·선택 화살표");
            }

            // ⑤ 챕터 카드(어두운 테두리 상자 안에 이번 챕터 테마의 바닥·길·소품) + ◀▶
            _card = UiKit.Rect(rt, "ChapterCard"); UiKit.Pct(_card, Layout.LobbyCard);
            {
                var frame = UiKit.Spawn("ui.frameDarkBorder", _card); UiKit.Stretch((RectTransform)frame.transform);
                var inner = UiKit.Rect(_card, "Stage"); UiKit.Pct(inner, 3, 4, 94, 92); UiKit.Ensure<RectMask2D>(inner.gameObject);
                _cardField = UiKit.Icon(inner, "Field", "env.field"); _cardField.preserveAspect = false; UiKit.Stretch(_cardField.rectTransform);
                _cardRoad = UiKit.Icon(inner, "Road", "env.road"); _cardRoad.preserveAspect = false; UiKit.Pct(_cardRoad.rectTransform, CardRoad);
                for (int i = 0; i < CardProps; i++) { _cardProps[i] = UiKit.Icon(inner, "Prop" + i, "env.tree"); UiKit.Pct(_cardProps[i].rectTransform, CardPropSlots[i]); }
                UiKit.Clickable(_card, () => { Audio.Wake(); App.StartBattle(App.Save.SelChapter); });
                UiKit.Tag(_card, "챕터 카드(스테이지 그림)");
            }
            var left = UiKit.Icon(rt, "ArrowL", "pi.arrow_left", Palette.Cream); UiKit.Pct(left.rectTransform, Layout.LobbyArrowL); UiKit.Clickable(left.rectTransform, () => Shift(-1)); UiKit.Tag(left.transform, "좌 화살표");
            var right = UiKit.Icon(rt, "ArrowR", "pi.arrow_right", Palette.Cream); UiKit.Pct(right.rectTransform, Layout.LobbyArrowR); UiKit.Clickable(right.rectTransform, () => Shift(1)); UiKit.Tag(right.transform, "우 화살표");

            // ⑥ 보조 버튼 2(탐험 · 클리어 보상 — 껍데기) → START(주황 · 카드 폭) → 모서리(성 잠금 · 이벤트)
            UiKit.Tag(BuildColumn(rt, "SubRow", Layout.LobbySubRow, true, (SideExplore, "ui.iconMap", "탐험"), (SideClearReward, "ui.iconChestRed", "클리어 보상")), "보조 버튼 2개 줄");
            var start = UiKit.Button(rt, "ui.btnStartOrange", "START", () => { Audio.Wake(); App.StartBattle(App.Save.SelChapter); }, Layout.LobbyStart); start.name = "Start"; UiKit.Tag(start, "START 버튼");   // Wake = WebGL 첫 터치 뒤 잠든 BGM 재개(T28)
            var castle = BuildColumn(rt, "Castle", Layout.LobbyCastle, true, (SideCastle, "ui.iconHome", "성"));
            if (castle != null) { var lk = UiKit.Icon(castle, "Lock", "ui.iconLock"); UiKit.Pct(lk.rectTransform, 30, 22, 40, 36); }
            BuildColumn(rt, "Events", Layout.LobbyEvents, true, (SideEvents, "ui.iconDungeon", "이벤트"));

            // ⑦ 하단 탭 5칸 — 프리팹 탭 바 조각을 표 자리에 (상점 · 장비 · 전투 · 탤런트 · 펫 — T10 · 이름 변경은 T43)
            _tabs = UiKit.Find(rt, "Tab_01_BottomFlushMenu");
            if (_tabs != null) { var tt = (RectTransform)_tabs; tt.SetParent(rt, false); UiKit.Pct(tt, Layout.TabBar); NavBar.Wire(App, _tabs, "lobby"); UiKit.Tag(tt, "하단 탭바"); }
        }

        /// <summary>
        /// 아이콘 + 라벨 버튼 묶음 — 어두운 반투명 기둥(<c>ui.frameDark</c>) 안에 세로(사이드) 또는 가로(보조 줄·모서리) 로 등분. 칸 이름 = <c>Side:&lt;key&gt;</c>(스모크 테스트가 센다).
        /// 버튼은 전부 <see cref="OnSide"/> 로 모인다. 아이콘은 GUI Pro 아이콘 스프라이트 · 라벨은 우리말(레퍼런스 영문 0).
        /// </summary>
        RectTransform BuildColumn(RectTransform parent, string name, Layout.R rect, bool horizontal, params (string key, string icon, string label)[] items)
        {
            var panel = UiKit.Spawn("ui.frameDark", parent); var prt = (RectTransform)panel.transform; prt.name = name; UiKit.Pct(prt, rect);
            int n = Mathf.Max(1, items.Length); float step = 100f / n;
            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i]; var cell = UiKit.Rect(prt, "Side:" + it.key);
                if (horizontal) UiKit.Pct(cell, i * step, 0, step, 100); else UiKit.Pct(cell, 0, i * step, 100, step);
                var ic = UiKit.Icon(cell, "Icon", it.icon); UiKit.Pct(ic.rectTransform, 14, 6, 72, 58);
                UiKit.Label(cell, 2, 62, 96, 34, it.label, 26, Palette.White, TextAnchor.UpperCenter);
                string key = it.key; UiKit.Clickable(cell, () => OnSide(key));
            }
            return prt;
        }

        /// <summary>사이드 아이콘·배너·보조 버튼·모서리 버튼의 단일 훅 — 지금은 아무 일 없음(주인: 껍데기). T43 이 <see cref="SideEvents"/> 를, T44 가 나머지를 여기서 팝업으로 잇는다.</summary>
        public void OnSide(string key) { }

        void Shift(int d)
        {
            var s = App.Save; int max = Math.Max(1, s.MaxChapter);
            s.SelChapter = Mathf.Clamp(s.SelChapter + d, 1, max); App.Persist(); Refresh();
        }

        /// <summary>챕터 카드 그림 = 그 챕터 전투 맵 테마(BattleWorld.Theme · (n−1)%4 순환)의 Environment 바닥·길 + 소품 4개(테마 표에서 챕터별로 고정 선택 · 물결 경계·납작한 풀꽃 제외).</summary>
        void RefreshCard(int chapter)
        {
            if (_cardField == null || App.Assets == null) return;
            var theme = BattleWorld.Theme.ForChapter(chapter);
            if (_cardTheme == theme.Name) return;
            _cardTheme = theme.Name;
            _cardField.sprite = App.Assets.Sprite(theme.Field) ?? App.Assets.Sprite("env.field");
            _cardRoad.sprite = App.Assets.Sprite(theme.Road) ?? App.Assets.Sprite("env.road");
            var picks = new List<string>();
            foreach (var p in MapLayouts.Of(theme.Name)) if (IsCardProp(p.Key) && !picks.Contains(p.Key)) picks.Add(p.Key);
            for (int i = 0; i < CardProps; i++)
            {
                var im = _cardProps[i]; if (im == null) continue;
                if (picks.Count == 0) { im.enabled = false; continue; }
                string key = picks[(chapter * 7 + i * 3) % picks.Count];
                im.sprite = App.Assets.Sprite(key); im.enabled = im.sprite != null; im.preserveAspect = true;
            }
        }
        /// <summary>카드에 넣을 만한 소품 — 나무·돌·덤불·선인장·야자·버섯. 물결 경계(roadUp)·풀·꽃·모래언덕은 뺀다.</summary>
        static bool IsCardProp(string key)
        {
            if (key.EndsWith(".roadUp") || key.EndsWith(".field") || key.EndsWith(".road")) return false;
            string n = key.Substring(key.LastIndexOf('.') + 1);
            return n.StartsWith("Tree") || n.StartsWith("Small_Tree") || n.StartsWith("Birch") || n.StartsWith("Stone") || n.StartsWith("Bush") || n.StartsWith("Cactus") || n.StartsWith("Plam") || n.StartsWith("Mushroom") || n.StartsWith("Dead_Tree");
        }

        public override void Refresh()
        {
            var s = App.Save;
            if (_chap != null) _chap.text = $"챕터 {s.SelChapter}";
            _top?.Refresh();
            RefreshCard(s.SelChapter);
        }
    }

    /// <summary>
    /// 상단 재화 바 — 레퍼런스 공통 문법(docs/ref/README.md · 로비·장비·상점·펫·던전·아레나 풀스크린 화면 공통 · 전투 HUD 에는 없음):
    /// 맨 왼쪽 <b>정사각 아바타(노란 테두리 · 내 플레이어 가슴 위 · HeroView)</b> → <b>전투력(칼 아이콘 + 주황 큰 숫자)</b> → <b>골드 pill</b> → <b>보석 pill</b>. 프레임 맨 위에서 3.7% 띄움(<see cref="Layout.LobbyTopBar"/>).
    /// 재료: 아바타 = UserInfo_01_Slider 의 ProfileFrame_02_Yellow 조각(본래 177px · 배율로) · pill = ResourceBar_Group 의 Coin/Gem 칸 · 아이콘 = ui.battle. 화면마다 이 한 줄이면 된다: <c>_top = TopBar.Build(App, root);</c> + Refresh 에서 <c>_top.Refresh()</c>.
    /// </summary>
    public sealed class TopBar
    {
        public RectTransform Root; public HeroView Hero; public Text Power, Gold, Gem;
        /// <summary>UI 비평 이름표용 조각(T46) — 아바타 칸 · 전투력 칸 · 골드 pill · 보석 pill. 화면이 표의 이름으로 <see cref="UiKit.Tag"/>/<see cref="UiKit.TagGroup"/> 을 단다.</summary>
        public RectTransform Avatar, PowerCell, GoldPill, GemPill;
        readonly App _app;
        TopBar(App app) { _app = app; }

        /// <summary>parent(프레임 크기 화면 루트) 안에 표 ① 자리로 세운다. showPower=false 면 전투력 칸을 뺀다(레퍼런스에 전투력이 없는 화면용).</summary>
        public static TopBar Build(App app, RectTransform parent, bool showPower = true)
        {
            var tb = new TopBar(app);
            var root = UiKit.Rect(parent, "TopBar"); UiKit.Pct(root, Layout.LobbyTopBar); tb.Root = root;
            var top = Layout.LobbyTopBar;
            // 아바타 — UserInfo_01_Slider 에서 ProfileFrame_02_Yellow 조각만(나머지 이름·길드·슬라이더·바탕 프레임은 끔) · 조각은 본래 크기 그대로 두고 아바타 칸에 배율로
            var slot = UiKit.Rect(root, "Avatar"); UiKit.Pct(slot, Layout.LobbyAvatar.Within(top)); tb.Avatar = slot;
            var info = UiKit.Spawn("ui.userInfoSlider", slot); var irt = (RectTransform)info.transform;
            var frame = UiKit.FindAny(irt, "ProfileFrame_02_Yellow", "ProfileFrame_02");
            if (frame != null)
            {
                for (int i = irt.childCount - 1; i >= 0; i--) { var c = irt.GetChild(i); if (c != frame) c.gameObject.SetActive(false); }
                var frt = (RectTransform)frame; frt.SetParent(slot, false); info.SetActive(false);
                UiKit.FitScale(frt, UiKit.PxSize(Layout.LobbyAvatar));
                var mask = UiKit.FindAny(frt, "Bg_MainColor(Mask)", "Mask"); if (mask == null) mask = frt;
                UiKit.Hide(mask, "Character");
                tb.Hero = HeroView.Attach((RectTransform)mask, HeroView.PlayerSkin(app));
                tb.Hero.SetFraming(1.6f, 0.45f);   // 가슴 위(레퍼런스 아바타)
            }
            else { info.SetActive(false); tb.Hero = HeroView.Attach(slot, HeroView.PlayerSkin(app)); tb.Hero.SetFraming(1.6f, 0.45f); }
            // 전투력 — 칼 아이콘 + 주황 큰 숫자(숫자만 · 레퍼런스에 라벨 없음)
            if (showPower)
            {
                var pw = UiKit.Rect(root, "PowerCell"); UiKit.Pct(pw, Layout.LobbyPower.Within(top)); tb.PowerCell = pw;
                var ic = UiKit.Icon(pw, "Icon", "ui.battle"); UiKit.Pct(ic.rectTransform, 0, 0, 26, 100);
                tb.Power = UiKit.Label(pw, 28, 0, 72, 100, "0", 46, Palette.Orange, TextAnchor.MiddleLeft); tb.Power.name = "Power";
            }
            // 골드 · 보석 pill = ResourceBar_Group 의 두 칸(세 번째 GemStone 은 이 게임에 없다) — 묶음의 가로 레이아웃을 끄고 표 자리로
            var res = UiKit.Spawn("ui.resourceBar", root); var rrt = (RectTransform)res.transform; UiKit.Stretch(rrt);
            var hl = res.GetComponent<HorizontalLayoutGroup>(); if (hl != null) hl.enabled = false;
            UiKit.Hide(rrt, "ResourceBar_GemStone");
            var coin = UiKit.Find(rrt, "ResourceBar_Coin"); if (coin != null) { UiKit.Pct((RectTransform)coin, Layout.LobbyGoldPill.Within(top)); tb.GoldPill = (RectTransform)coin; }
            var gem = UiKit.Find(rrt, "ResourceBar_Gem"); if (gem != null) { UiKit.Pct((RectTransform)gem, Layout.LobbyGemPill.Within(top)); tb.GemPill = (RectTransform)gem; }
            tb.Gold = UiKit.SetText(rrt, "ResourceBar_Coin/Text (TMP)", "0"); tb.Gem = UiKit.SetText(rrt, "ResourceBar_Gem/Text (TMP)", "0");
            tb.Refresh();
            return tb;
        }

        public void Refresh()
        {
            var s = _app.Save;
            if (Power != null) Power.text = UiKit.Fmt(_app.Power());
            if (Gold != null) Gold.text = UiKit.Fmt(s.Gold);
            if (Gem != null) Gem.text = UiKit.Fmt(s.Gem);
            Hero?.SetSkin(HeroView.PlayerSkin(_app));
        }
    }

    /// <summary>
    /// 하단 탭 5칸 = <b>상점 · 장비 · 전투 · 탤런트 · 펫</b> (주인 지시 2026-09-05 · T10 — 대장간·설정 탭은 뺐다).
    /// 대장간은 장비 화면의 «합성» 버튼으로만 · 설정은 로비의 메뉴(≡)와 전투의 일시정지에서만 연다.
    /// 로비 프리팹(Lobby_Default)의 Tab_01_BottomFlushMenu 를 다른 화면에도 같은 배선으로 세운다 — 탭 순서 = 프리팹 자식 순서(0~4) 그대로.
    /// 탤런트 탭 = <see cref="Overlay.TalentPet"/>(Character_Talent_02 프리팹 팝업 · 기능 없음 · 팝업 안 탭 바로 닫는다 · T43 이 «던전» 으로 바꾼다) · 펫 탭 = <see cref="PetScreen"/>(레퍼런스 13 구도 껍데기 · T42).
    /// </summary>
    public static class NavBar
    {
        public static readonly string[] Keys = { "shop", "gear", "battle", "talent", "pet" };
        static readonly string[] IconsK = { "ui.shop", "ui.bag", "ui.battle", "ui.talentIcon", "ui.petIcon" };
        public static readonly string[] Labels = { "상점", "장비", "전투", "탤런트", "펫" };

        public static void Attach(GameScreen screen, RectTransform root, string current)
        {
            var bar = UiKit.SpawnRt("ui.tabBar", root, Layout.TabBar);
            Wire(screen.App, bar, current);
        }
        /// <summary>탭 바(Tab_01_BottomFlushMenu 인스턴스)의 자식 5개에 아이콘·라벨·클릭을 배선한다. current = 켜 둘 탭(«lobby» 는 전투 탭).</summary>
        public static void Wire(App app, Transform bar, string current)
        {
            for (int i = 0; i < bar.childCount && i < Keys.Length; i++)
            {
                var tab = bar.GetChild(i); int k = i;
                UiKit.SetSprite(tab, "Normal/Icon", IconsK[i], Palette.White); UiKit.SetSprite(tab, "Focus/Icon_Focus", IconsK[i], Palette.White);
                UiKit.SetText(tab, "Focus/Text (TMP)", Labels[i]);
                bool on = Keys[i] == current || (Keys[i] == "battle" && current == "lobby");
                UiKit.Show(tab, "Focus", on); UiKit.Show(tab, "Normal", !on);   // «현재 탭» 강조 = 프리팹의 Focus/Normal 전환 그대로(T22 는 손대지 않는다)
                UiKit.Clickable(tab, () => Go(app, Keys[k], current));   // 눌림 표시(T22) = Clickable 의 ColorTint — 탭 루트는 그림이 없어 켜져 있는 쪽(Normal/Focus)의 첫 Image 가 어두워진다
            }
        }
        /// <summary>탭 이동 — 팝업(탤런트/설정)이 떠 있으면 닫고 간다. 같은 탭은 아무 일 없음.</summary>
        static void Go(App app, string key, string current)
        {
            if (key == current) return;
            switch (key)
            {
                case "battle": app.Overlay.Close(); if (current != "lobby") app.ShowScreen("lobby"); break;
                case "talent": app.Overlay.TalentPet(key); break;   // 탤런트는 T43 이 «던전» 으로 바꾼다 · 펫은 T42 부터 화면(PetScreen · default 분기)
                default: app.Overlay.Close(); app.ShowScreen(key); break;
            }
        }
        public static void Refresh(RectTransform root) { var bar = UiKit.Find(root, "ui.tabBar"); if (bar != null) bar.SetAsLastSibling(); }
    }
}
