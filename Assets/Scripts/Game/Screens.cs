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
        /// <summary>
        /// 아이콘 라벨 칸(사이드·보조·모서리 · <see cref="BuildColumn"/>) 안의 아이콘 자리 / 글자 띠 자리(칸 %) — T68 ①(주인 «아이콘 너무 작음» · 1.5~1.8배 · 칸 폭의 ≥ 75%) + T63-lobby(라벨 보조 36 · 2줄 · 잘림 0).
        /// 아이콘이 칸 위 82% 를 차지하고 글자 띠(아래 50%)가 아이콘 아랫부분에 겹친다 — 레퍼런스 01 도 «Daily Gifts»·«7-Day Challenge» 가 아이콘 밑단 위에 얹혀 있다(외곽선 글자).
        /// 2줄 높이 = 36×1.375×(1+<see cref="UiKit.CaptionLineSpacing"/>) ≈ 87px ≤ 글자 띠(사이드 93px · 보조 111px · 모서리 99px) → bestFit 이 줄이지 않는다.
        /// </summary>
        static readonly Layout.R CaptionIcon = new Layout.R(12.5f, 0, 75, 82), CaptionLabel = new Layout.R(1, 50, 98, 50);
        /// <summary>칸 폭 대비 아이콘 최소 비율(T68 ① · 스모크 테스트가 단언).</summary>
        public const float CaptionIconMinW = 0.75f;
        /// <summary>
        /// 챕터 카드 그림 = 프리팹 <c>SampleImage_Map</c>(Image_Map_Forest · 573×709 세로 디오라마 · T68 ④ · 주인 «예전 프리팹 그림이 좋았음» · 결정 34 뒤집음).
        /// 카드 자리(표 ① 27.9/41.0/44.5/13.7 · 가로 1.5:1)는 이름표·클릭 영역으로 그대로 두고, 그림은 카드 폭의 90% 로 카드 <b>바닥에 맞춰</b> 세워 나무 꼭대기가 카드 위로 넘친다(레퍼런스 01 의 디오라마도 카드 상자 위로 솟는다 · 위 끝 = 프레임 31.8% · 챕터 밑줄 30.9% 아래).
        /// 카드 % 로: 폭 90 · 높이 90 × 709/573 = 111.4%·(481/320) ≈ 167.5 · 바닥 정렬 → y = 100 − 167.5.
        /// </summary>
        static readonly Layout.R CardMapImage = new Layout.R(5, -67.5f, 90, 167.5f);

        TopBar _top; Text _chap; Transform _tabs;

        protected override void Build()
        {
            var root = UiKit.Spawn("ui.lobby", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            // 프리팹은 부품 창고 — 쓰지 않는 조각은 끈다(상단 바·사이드 버튼·빨간 START·채팅·부제) · 샘플 지도(SampleImage_Map)는 T68 ④ 부터 챕터 카드 그림으로 쓴다
            UiKit.Hide(rt, "ChatBox", "Group_LeftButtons", "Group_RightButtons", "ResourceBar_Group");
            var oldInfo = UiKit.FindAny(rt, "UserInfo_01", "UserInfo_01_Slider"); if (oldInfo != null) oldInfo.gameObject.SetActive(false);
            var oldStart = UiKit.FindAny(rt, "Button_03_Red", "Button_03_Convex_Red"); if (oldStart != null) oldStart.gameObject.SetActive(false);
            var subT = UiKit.Find(rt, "Text (TMP)"); if (subT != null && subT.parent == rt) subT.gameObject.SetActive(false);
            // 배경 = 프리팹 Background 평면색만(색은 초록 · 결정 33) — 흐린 칼 무늬 Deco 15개는 끈다(T68 ③ · 주인 «데코 별로»)
            var bg = UiKit.Find(rt, "Background"); var bgImg = bg != null ? bg.GetComponent<Image>() : null;
            if (bgImg != null) { bgImg.color = Color.Lerp(Palette.Green, Palette.Ink, 0.42f); bgImg.raycastTarget = true; }
            if (bg != null) for (int i = 0; i < bg.childCount; i++) { var c = bg.GetChild(i); if (c.name.StartsWith("Deco")) c.gameObject.SetActive(false); }

            // ① 상단 재화 바 (아바타 · 전투력 · 골드 · 보석) — 공용 헬퍼 · 비평 이름표(T46 · ref-layout ① 의 «요소» 이름 그대로)
            _top = TopBar.Build(App, rt);
            UiKit.Tag(_top.Root, "상단 바(아바타+재화 줄 전체)"); UiKit.Tag(_top.Avatar, "아바타(정사각)"); UiKit.TagGroup(_top.Root, "재화 pill 줄", _top.PowerCell, _top.GoldPill, _top.GemPill);

            // ② 이벤트 배너(보라 · 패스 껍데기) + 메뉴(≡)
            var banner = UiKit.Spawn("ui.framePlum", rt); var brt = (RectTransform)banner.transform; brt.name = "Banner"; UiKit.Pct(brt, Layout.LobbyBanner);
            {
                var medal = UiKit.Icon(brt, "Icon", "ui.iconMedal"); UiKit.Pct(medal.rectTransform, 3, 12, 17, 76);
                // T63-lobby — 본문 40 한 줄(55px)이 들어가게 띠 46%(60px) · 크기는 하한 상수
                UiKit.Label(brt, 22, 4, 58, 46, "시즌 패스", TextSize.Body, Palette.White, TextAnchor.MiddleLeft);
                var bar = UiKit.MakeBar(brt, "ui.sliderGreen"); UiKit.Pct(bar.Root, 22, 54, 56, 36); bar.Set(0, "준비 중");
                var badge = UiKit.Panel(brt, "Badge", "fr.circle", Palette.Ink); UiKit.Pct(badge.rectTransform, 82, 10, 15, 80);
                // 배지는 배너 오른쪽 끝(레퍼런스 «22» 자리) — FitInParent 는 앵커를 지워 배너 가운데로 보내 «시즌 패스» 글자를 덮었다(T47 회차 3 감점 · 결정 103 과 같은 함정) → 높이에서 폭만 정사각으로
                var arf = UiKit.Ensure<AspectRatioFitter>(badge.gameObject); arf.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth; arf.aspectRatio = 1f;
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
                _chap = UiKit.SetText(trt, "Text (TMP)", "챕터 1", size: UiKit.FontForHeight(t.H));   // 글자 높이 = 표 2.3%(레퍼런스 «CHAPTER 22» 덩어리 · T47 회차 2 에서 1.5% 로 작았다)
                if (_chap != null) UiKit.Tag(_chap.transform, "챕터 제목", textBounds: true); UiKit.Tag(UiKit.Find(trt, "LineDeco"), "챕터 밑줄·선택 화살표");   // 글자 덩어리로 잰다(T47 ⓒ · 조각 rect 는 표보다 6%p 넓다)
            }

            // ⑤ 챕터 카드 = 프리팹 SampleImage_Map 그림(T68 ④ · 코드 조립 카드 폐기) + ◀▶ · 카드 자리(표 ①)는 이름표·클릭 영역
            var card = UiKit.Rect(rt, "ChapterCard"); UiKit.Pct(card, Layout.LobbyCard);
            {
                var map = UiKit.Find(rt, "SampleImage_Map");
                if (map != null)
                {
                    var mrt = (RectTransform)map; mrt.SetParent(card, false); map.gameObject.SetActive(true); UiKit.Pct(mrt, CardMapImage);
                    var mi = map.GetComponent<Image>(); if (mi != null) { mi.preserveAspect = true; mi.raycastTarget = false; }
                }
                UiKit.Clickable(card, () => { Audio.Wake(); App.StartBattle(App.Save.SelChapter); });
                UiKit.Tag(card, "챕터 카드(스테이지 그림)");
            }
            var left = UiKit.Icon(rt, "ArrowL", "pi.arrow_left", Palette.Cream); UiKit.Pct(left.rectTransform, Layout.LobbyArrowL); UiKit.Clickable(left.rectTransform, () => Shift(-1)); UiKit.Tag(left.transform, "좌 화살표");
            var right = UiKit.Icon(rt, "ArrowR", "pi.arrow_right", Palette.Cream); UiKit.Pct(right.rectTransform, Layout.LobbyArrowR); UiKit.Clickable(right.rectTransform, () => Shift(1)); UiKit.Tag(right.transform, "우 화살표");

            // ⑥ 보조 버튼 2(탐험 · 클리어 보상 — 껍데기) → START(주황 · 카드 폭) → 모서리(성 잠금 · 이벤트)
            UiKit.Tag(BuildColumn(rt, "SubRow", Layout.LobbySubRow, true, (SideExplore, "ui.iconMap", "탐험"), (SideClearReward, "ui.iconChestRed", "클리어 보상")), "보조 버튼 2개 줄");
            var start = UiKit.Button(rt, "ui.btnStartOrange", "START", () => { Audio.Wake(); App.StartBattle(App.Save.SelChapter); }, Layout.LobbyStart); start.name = "Start"; UiKit.Tag(start, "START 버튼");   // Wake = WebGL 첫 터치 뒤 잠든 BGM 재개(T28)
            var castle = BuildColumn(rt, "Castle", Layout.LobbyCastle, true, (SideCastle, "ui.iconHome", "성"));
            if (castle != null) { var lk = UiKit.Icon(castle, "Lock", "ui.iconLock"); UiKit.Pct(lk.rectTransform, 30, 22, 40, 36); }
            BuildColumn(rt, "Events", Layout.LobbyEvents, true, (SideEvents, "ui.iconDungeon", "이벤트"));

            // ⑦ 하단 탭 5칸 — 프리팹 탭 바 조각을 표 자리에 (상점 · 장비 · 전투 · 던전 · 펫 — T10 · «탤런트 → 던전» 은 T43)
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
                // T68 ① 아이콘 = 칸 폭의 75%(위 82%) · T63-lobby 라벨 = 보조 하한(36 · ROUTINE T68 1항 «라벨은 T63 하한 36 이상»)으로 2줄까지(«데일리 기프트»·«7일 챌린지»·«클리어 보상» 은 레퍼런스도 2줄) · 칸 바닥에 붙여 아이콘 밑단에 얹는다(레퍼런스 01 과 같은 겹침 · 외곽선 글자)
                var ic = UiKit.Icon(cell, "Icon", it.icon); UiKit.Pct(ic.rectTransform, CaptionIcon);
                var lb = UiKit.Label(cell, CaptionLabel.X, CaptionLabel.Y, CaptionLabel.W, CaptionLabel.H, it.label, TextSize.Aux, Palette.White, TextAnchor.LowerCenter, kind: TextKind.Aux);
                lb.lineSpacing = UiKit.CaptionLineSpacing;
                string key = it.key; UiKit.Clickable(cell, () => OnSide(key));
            }
            return prt;
        }

        /// <summary>사이드 아이콘·배너·보조 버튼·모서리 버튼의 단일 훅 — T43: <see cref="SideEvents"/>(오른쪽 아래 방패) = 아레나(PvP) 페이지 · T44: 특권·패스 = 페이지(<see cref="PrivilegeScreen"/>·<see cref="PassScreen"/>) · 퀘스트·출석·데일리 기프트·7일 챌린지 = 팝업(<see cref="LobbyPopups"/>). 스타터팩·탐험·클리어 보상·성은 아무 일 없음(껍데기).</summary>
        public void OnSide(string key)
        {
            switch (key)
            {
                case SideEvents: EventsScreen.Open(App, EventsScreen.PagePvp); break;
                case SidePrivilege: App.ShowScreen("privilege"); break;
                case SidePass: App.ShowScreen("pass"); break;
                case SideQuest: LobbyPopups.Quest(App); break;
                case SideAttendance: LobbyPopups.Attendance(App); break;
                case SideDailyGift: LobbyPopups.DailyGift(App); break;
                case SideChallenge7: LobbyPopups.Challenge7(App); break;
            }
        }

        void Shift(int d)
        {
            var s = App.Save; int max = Math.Max(1, s.MaxChapter);
            s.SelChapter = Mathf.Clamp(s.SelChapter + d, 1, max); App.Persist(); Refresh();
        }

        /// <summary>챕터 카드 그림은 전 챕터 같은 그림(프리팹의 샘플 지도 스프라이트가 하나뿐 · 새 그림 금지 · T68 ④ · 결정 기록) — 바뀌는 것은 «챕터 N» 글자뿐.</summary>
        public override void Refresh()
        {
            var s = App.Save;
            if (_chap != null) _chap.text = $"챕터 {s.SelChapter}";
            _top?.Refresh();
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
            // 상단 초상은 정지 그림(T68 ② · 주인 «로비 주인공 아이콘이 계속 움직인다») — 장비 화면 가운데 큰 캐릭터(GearScreen)는 그대로 움직인다
            tb.Hero.SetStill(true);
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
    /// 하단 탭 5칸 = <b>상점 · 장비 · 전투 · 던전 · 펫</b> (주인 지시 2026-09-05 · T10 — 대장간·설정 탭은 뺐다 · T43: «탤런트» → «던전» = <see cref="EventsScreen"/> 던전 페이지).
    /// 대장간은 장비 화면의 «합성» 버튼으로만 · 설정은 로비의 메뉴(≡)와 전투의 일시정지에서만 연다.
    /// 로비 프리팹(Lobby_Default)의 Tab_01_BottomFlushMenu 를 다른 화면에도 같은 배선으로 세운다 — 탭 순서 = 프리팹 자식 순서(0~4) 그대로.
    /// 던전 탭 = <see cref="EventsScreen"/>(레퍼런스 20~26 구도 껍데기 · T43 · «탤런트» 를 대체) · 펫 탭 = <see cref="PetScreen"/>(레퍼런스 13 구도 껍데기 · T42).
    /// </summary>
    public static class NavBar
    {
        public static readonly string[] Keys = { "shop", "gear", "battle", "dungeon", "pet" };
        static readonly string[] IconsK = { "ui.shop", "ui.bag", "ui.battle", "ui.iconDungeon", "ui.petIcon" };
        public static readonly string[] Labels = { "상점", "장비", "전투", "던전", "펫" };

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
        /// <summary>탭 이동 — 팝업(설정 등)이 떠 있으면 닫고 간다. 같은 탭은 아무 일 없음.</summary>
        static void Go(App app, string key, string current)
        {
            if (key == current) return;
            switch (key)
            {
                case "battle": app.Overlay.Close(); if (current != "lobby") app.ShowScreen("lobby"); break;
                case "dungeon": app.Overlay.Close(); EventsScreen.Open(app, EventsScreen.PageDungeon); break;   // T43 · 펫은 T42 부터 화면(PetScreen · default 분기)
                default: app.Overlay.Close(); app.ShowScreen(key); break;
            }
        }
        public static void Refresh(RectTransform root) { var bar = UiKit.Find(root, "ui.tabBar"); if (bar != null) bar.SetAsLastSibling(); }
    }
}
