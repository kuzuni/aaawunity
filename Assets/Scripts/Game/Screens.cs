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
    /// <item>메뉴(≡ = 설정 팝업). <b>이벤트 배너(시즌 패스)는 주인 2026-09-07 지시로 삭제 — 그 자리는 비워 둔다</b>(T78 · 다른 요소를 끌어올리지 않는다).</item>
    /// <item>왼쪽 세로 아이콘 <b>1</b>(특권 · T78 로 스타터팩·7일 챌린지 삭제) / 오른쪽 3(출석·데일리 기프트·퀘스트) — 어두운 반투명 기둥 + 아이콘 + 라벨 · 전부 <see cref="OnSide"/> 훅 하나로.</item>
    /// <item>«챕터 N» 제목(프리팹 Title_LineDeco 조각 · 밑줄 포함) → 챕터 카드(어두운 테두리 + 이번 챕터 전투 맵 테마의 Environment 바닥·길·소품) + ◀▶ → 보조 버튼 2(탐험·클리어 보상 · 껍데기) → START(주황 · 카드와 같은 폭) → 오른쪽 아래 이벤트(T43 진입) → 탭 바. <b>왼쪽 아래 «성»(잠금)은 주인 2026-09-07 지시로 삭제</b>(T78).</item>
    /// </list>
    /// </summary>
    public sealed class LobbyScreen : GameScreen
    {
        public override string Name => "lobby";
        /// <summary>사이드·보조·모서리 버튼 키 — T43(events) · T44(나머지) 가 <see cref="OnSide"/> 에서 이어받는다. T78(주인 2026-09-07)로 <c>starter</c>·<c>challenge7</c>·<c>pass</c>·<c>castle</c> 네 키는 삭제됐다.</summary>
        public const string SidePrivilege = "privilege", SideAttendance = "attendance", SideDailyGift = "dailyGift", SideQuest = "quest";
        public const string SideExplore = "explore", SideClearReward = "clearReward", SideEvents = "events";
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
        /// <summary>«데일리 기프트» 사이드 아이콘의 빨간 알림 점 — 지금 받을 수 있는 줄이 하나라도 있으면 켠다(T77 · <see cref="Refresh"/>).</summary>
        GameObject _giftDot;
        /// <summary>«탐험» 보조 버튼의 빨간 알림 점 — 받을 것이 쌓였거나 빠른 탐험 횟수가 남으면 켠다(T97 · <see cref="Refresh"/>).</summary>
        GameObject _expDot, _chestDot;
        /// <summary>메뉴(≡) 버튼의 빨간 알림 점 — 메뉴가 품은 항목에 지금 받을 것이 있으면 켠다(T96 ⓔ · <see cref="Core.Notify.MenuAny"/>).</summary>
        GameObject _menuDot;

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
            // T72 ① 배경 패턴 + ③ 배경 그라데이션 (주인 «로비 배경에 이 패턴이 오른쪽 상단으로 천천히 올라간다» · 레퍼런스 01 은 초록보다 «어두운» 칼 무늬 → Ink 무늬)
            // 자리는 배경 조각 «바로 위» 형제 = 7항 «패턴은 배경 층에만»(상단 재화 바·사이드 기둥·챕터 카드·START·탭 바는 전부 뒤에 오는 형제라 무늬가 그 안으로 비치지 않는다)
            int bgIdx = bg != null && bg.parent == rt ? bg.GetSiblingIndex() + 1 : 0;
            UiKit.PatternBg(rt, UiKit.PatternTintLobby, UiKit.PatternTileSeconds, bgIdx);   // T94 ⓐ — 로비만 18/255(공용 3/255 는 이 바탕에서 안 보인다)
            UiKit.Gradient(rt, siblingIndex: bgIdx);   // 위 +12% 밝음 · 아래 −18% 어둠(3항 «화면 배경» · 헬퍼가 패턴 위로 넣는다)

            // ① 상단 재화 바 (아바타 · 전투력 · 골드 · 보석) — 공용 헬퍼 · 비평 이름표(T46 · ref-layout ① 의 «요소» 이름 그대로)
            _top = TopBar.Build(App, rt);
            UiKit.Tag(_top.Root, "상단 바(아바타+재화 줄 전체)"); UiKit.Tag(_top.Avatar, "아바타(정사각)"); UiKit.TagGroup(_top.Root, "재화 pill 줄", _top.PowerCell, _top.GoldPill, _top.GemPill);

            // ② 메뉴(≡) — 이벤트 배너(시즌 패스)는 T78(주인 2026-09-07 «시즌 패스도 삭제»)로 없앴다 · 표 ① 의 배너 자리(24.5/9.2/51.6/5.6)는 비워 두고 아래 요소를 끌어올리지 않는다
            var menu = UiKit.Find(rt, "Button_Menu");
            if (menu != null)
            {
                // T96-menu(주인 2026-09-07) — ≡ 는 이제 «메뉴» 팝업(Lobby_Menu 프리팹)을 연다: 우편함 · 설정 · 데일리 기프트 · 퀘스트 · 출석 · 특권
                var mrt = (RectTransform)menu; mrt.SetParent(rt, false); UiKit.Pct(mrt, Layout.LobbyMenu);
                UiKit.Clickable(mrt, () => LobbyMenu.Open(App)); UiKit.Tag(mrt, "메뉴(☰) 버튼");
                // 메뉴 안에 받을 것이 있으면 켠다(T96 ⓔ · Refresh 가 켜고 끈다)
                var dot = UiKit.Spawn("ui.alertDot", mrt); dot.name = "MenuDot";
                UiKit.Pct((RectTransform)dot.transform, 62, 2, 34, 34); _menuDot = dot; dot.SetActive(false);
            }

            // ③ 사이드 아이콘 기둥 — **T96-menu(주인 2026-09-07 «중복된 거는 메뉴 안으로 넣는 걸로»)로 지웠다.**
            // 특권(좌 1칸)·출석·데일리 기프트·퀘스트(우 3칸)는 전부 ≡ 메뉴 안에 있으므로 로비에 두 번 나오지 않는다.
            // 남는 것은 메뉴에 없는 것뿐 — 보조 줄(탐험 · 클리어 보상)과 오른쪽 아래 «이벤트». 자리(Layout.LobbySideL/R)는 표에 남겨 둔다(비운다).

            // ④ 챕터 제목(프리팹 Title_LineDeco 조각 = 글자 + 밑줄 장식 · 표의 제목 행 ∪ 밑줄 행 자리)
            var title = UiKit.FindAny(rt, "Title_LineDeco_01_Blue", "Title_LineDeco_01_l");
            if (title != null)
            {
                var trt = (RectTransform)title; trt.SetParent(rt, false);
                var t = Layout.LobbyChapTitle; var u = Layout.LobbyChapUnderline;
                UiKit.Pct(trt, u.X, t.Y, u.W, u.Y + u.H - t.Y);
                _chap = UiKit.SetText(trt, "Text (TMP)", "챕터 1", size: UiKit.FontForHeight(t.H));   // 글자 높이 = 표 2.3%(레퍼런스 «CHAPTER 22» 덩어리 · T47 회차 2 에서 1.5% 로 작았다)
                // T111 ⓐ — 주인 2026-09-07 «챕터 아래에 LineDeco 들은 없애줘 · 로비, 전투 화면 둘 다»: 조각 안의 밑줄 장식만 끈다(글자·자리는 그대로 · 이름표도 없앤다 = 채점 행이 사라진다)
                UiKit.Show(trt, "LineDeco", false);
                if (_chap != null) UiKit.Tag(_chap.transform, "챕터 제목", textBounds: true);   // 글자 덩어리로 잰다(T47 ⓒ · 조각 rect 는 표보다 6%p 넓다)
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
                // T94 ⓑ(주인 2026-09-07 05:3X «메인 로비에 Border 있는 것들은 걍 없애셈») — T69-lobby 가 넣었던 카드 검은 링을 뺀다.
                // 로비만 예외이고 다른 화면의 T69 테두리는 그대로다(BorderAudit.StrictScreens 에서 01_lobby 만 뺐다).
                UiKit.Tag(card, "챕터 카드(스테이지 그림)");
            }
            var left = UiKit.Icon(rt, "ArrowL", "pi.arrow_left", Palette.Cream); UiKit.Pct(left.rectTransform, Layout.LobbyArrowL); UiKit.Clickable(left.rectTransform, () => Shift(-1)); UiKit.Tag(left.transform, "좌 화살표");
            var right = UiKit.Icon(rt, "ArrowR", "pi.arrow_right", Palette.Cream); UiKit.Pct(right.rectTransform, Layout.LobbyArrowR); UiKit.Clickable(right.rectTransform, () => Shift(1)); UiKit.Tag(right.transform, "우 화살표");

            // ⑥ 보조 버튼 2(탐험 · 클리어 보상 — 껍데기) → START(주황 · 카드 폭) → 모서리(이벤트 · T78 로 «성» 삭제)
            UiKit.Tag(BuildColumn(rt, "SubRow", Layout.LobbySubRow, true, (SideExplore, "ui.iconMap", "탐험"), (SideClearReward, "ui.iconChestRed", "클리어 보상")), "보조 버튼 2개 줄");
            var start = UiKit.Button(rt, "ui.btnStartOrange", "START", () => { Audio.Wake(); App.StartBattle(App.Save.SelChapter); }, Layout.LobbyStart); start.name = "Start"; UiKit.Tag(start, "START 버튼");   // Wake = WebGL 첫 터치 뒤 잠든 BGM 재개(T28)
            // T78 — 왼쪽 아래 «성»(집 아이콘 + 자물쇠 · 결정 32)은 주인 2026-09-07 지시로 삭제 · 오른쪽 아래 «이벤트» 는 레퍼런스 01 자리 그대로 둔다
            BuildColumn(rt, "Events", Layout.LobbyEvents, true, (SideEvents, "ui.iconDungeon", "이벤트"));

            // ⑦ 하단 탭 5칸 — 프리팹 탭 바 조각을 표 자리에 (상점 · 장비 · 전투 · 던전 · 펫 — T10 · «탤런트 → 던전» 은 T43)
            NavBar.BottomFrame(rt);   // T106 ⓓ — 로비는 탭 바를 프리팹에서 가져오므로 띠를 여기서 따로 깐다(탭 바보다 먼저 = 바 뒤)
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
                // T77 — «데일리 기프트» 칸만 빨간 알림 점(아이콘 오른쪽 위 · 받을 수 있는 줄이 있을 때만 · Refresh 가 켜고 끈다)
                if (it.key == SideDailyGift)
                {
                    var dot = UiKit.Spawn("ui.alertDot", cell); dot.name = "GiftDot";
                    var drt = (RectTransform)dot.transform; UiKit.Pct(drt, 68, 2, 26, 11); _giftDot = dot; dot.SetActive(false);
                }
                if (it.key == SideExplore)
                {
                    var dot = UiKit.Spawn("ui.alertDot", cell); dot.name = "ExpDot";
                    var drt = (RectTransform)dot.transform; UiKit.Pct(drt, 68, 2, 26, 11); _expDot = dot; dot.SetActive(false);
                }
                // T98 3항 — 받을 수 있는 챕터 보상이 있으면 빨간 점(T96 ⓔ 와 같은 규칙)
                if (it.key == SideClearReward)
                {
                    var dot = UiKit.Spawn("ui.alertDot", cell); dot.name = "ChestDot";
                    var drt = (RectTransform)dot.transform; UiKit.Pct(drt, 68, 2, 26, 11); _chestDot = dot; dot.SetActive(false);
                }
                string key = it.key; UiKit.Clickable(cell, () => OnSide(key));
            }
            // T94 ⓑ — 기둥 «상자» 의 검은 링도 뺀다(주인 «로비에 Border 있는 것들은 걍 없애셈» · 위 카드와 같은 까닭).
            return prt;
        }

        /// <summary>사이드 아이콘·보조 버튼·모서리 버튼의 단일 훅 — T43: <see cref="SideEvents"/>(오른쪽 아래 방패) = 아레나(PvP) 페이지 · T44: 특권 = 페이지(<see cref="PrivilegeScreen"/>) · 퀘스트·출석·데일리 기프트 = 팝업(<see cref="LobbyPopups"/>). «탐험» = T97 방치·오프라인 보상 팝업(껍데기 아님) · 클리어 보상은 아무 일 없음(껍데기). T78 로 패스·7일 챌린지·스타터팩·성은 사라졌다.</summary>
        public void OnSide(string key)
        {
            switch (key)
            {
                // T107 — 주인 «이벤트 열면 무조건 던전부터 뜨게»(아레나·상인은 그 안에서 넘어간다)
                case SideEvents: EventsScreen.Open(App, EventsScreen.PageDungeon); break;
                case SidePrivilege: App.ShowScreen("privilege"); break;
                case SideQuest: LobbyPopups.Quest(App); break;
                case SideAttendance: LobbyPopups.Attendance(App); break;
                case SideDailyGift: LobbyPopups.DailyGift(App); break;
                case SideExplore: LobbyPopups.Expedition(App); break;   // T97 — 방치·오프라인 보상(껍데기 아님)
                // T98 — 챕터 보상(Chapter Chest) 페이지(껍데기 아님 · 레퍼런스 32)
                case SideClearReward: ChapterChestScreen.Open(App); break;
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
            // T77 — 데일리 기프트에 받을 것이 있으면 사이드 아이콘에 빨간 점
            if (_giftDot != null) _giftDot.SetActive(Core.DailyGift.AnyClaimable(s, App.Data != null ? App.Data.DailyGift : null, SaveStore.Today()));
            // T97 — 탐험에 쌓인 것이 있거나 빠른 탐험 횟수가 남으면 «탐험» 보조 버튼에 빨간 점
            if (_expDot != null) _expDot.SetActive(App.Data != null && App.Data.Expedition != null
                && Core.Expedition.AnyClaimable(App.Data, s, App.Data.Expedition, LobbyPopups.NowSec(), SaveStore.Today()));
            // T98 3항 — 받을 수 있는 챕터 보상이 있으면 «클리어 보상» 보조 버튼에 빨간 점
            if (_chestDot != null) _chestDot.SetActive(Core.ChapterChest.AnyClaimable(App.Data, s));
            // T96 ⓔ — 메뉴 안(데일리 기프트 · 광고로 받을 재화 …)에 받을 것이 있으면 ≡ 에 빨간 점
            if (_menuDot != null) _menuDot.SetActive(Core.Notify.MenuAny(App.Data, s, LobbyPopups.NowSec(), SaveStore.Today()));
        }
    }

    /// <summary>
    /// 상단 재화 바 — 레퍼런스 공통 문법(docs/ref/README.md · 로비·장비·상점·펫·던전·아레나 풀스크린 화면 공통 · 전투 HUD 에는 없음):
    /// 맨 왼쪽 <b>정사각 아바타(노란 테두리 · 내 플레이어 가슴 위 · HeroView)</b> → <b>전투력(칼 아이콘 + 주황 큰 숫자)</b> → <b>골드 pill</b> → <b>보석 pill</b>. 프레임 맨 위에서 3.7% 띄움(<see cref="Layout.LobbyTopBar"/>).
    /// 재료: 아바타 = <c>ui.profileFrame.&lt;색&gt;</c>(ProfileFrame_02 · 색은 프로필에서 고른다 = T96-profile · 기본 노랑 · 본래 177px 을 배율로 · 누르면 <see cref="Profile.OpenAvatar"/>) · pill = ResourceBar_Group 의 Coin/Gem 칸 · 아이콘 = ui.battle. 화면마다 이 한 줄이면 된다: <c>_top = TopBar.Build(App, root);</c> + Refresh 에서 <c>_top.Refresh()</c>.
    /// </summary>
    public sealed class TopBar
    {
        public RectTransform Root; public HeroView Hero; public Text Power, Gold, Gem;
        /// <summary>UI 비평 이름표용 조각(T46) — 아바타 칸 · 전투력 칸 · 골드 pill · 보석 pill. 화면이 표의 이름으로 <see cref="UiKit.Tag"/>/<see cref="UiKit.TagGroup"/> 을 단다.</summary>
        public RectTransform Avatar, PowerCell, GoldPill, GemPill;
        readonly App _app;
        TopBar(App app) { _app = app; }

        /// <summary>칸 바탕 오브젝트 이름(고정 · T72 7항 게이트가 찾는다).</summary>
        public const string CellBgName = "CellBg";
        /// <summary>상단 프레임 띠 오브젝트 이름(고정 · T106 게이트가 찾는다).</summary>
        public const string FrameName = "TopFrame";
        /// <summary>
        /// 띠가 탑바 줄 밖으로 더 뻗는 길이(프레임 px · T106 ⓑ «탑바만 감싸지 말고 탑바 위 부분까지 전부 · 화면 끝까지»).
        /// 위로는 SafeArea(노치)와 레터박스를 넘어 화면 맨 위까지, 옆으로는 좌우 레터박스까지 덮는다 — uGUI 는 마스크가 없으면 자식을 부모 rect 밖에도 그린다.
        /// 어떤 화면비에서도 남게 프레임 높이(2337)보다 넉넉히 잡는다(화면 밖은 어차피 안 그려진다).
        /// </summary>
        public const float FrameOverscan = 4000f;

        /// <summary>
        /// 칸 하나에 <b>불투명</b> 바탕(T72 7항 · 주인 재차 2026-09-07 «탑바를 프레임으로 감싸서 패턴이 침범하지 않는 것처럼») —
        /// 조각이 제 바탕(직계 «Bg» · 재화 pill 의 캡슐)을 가진 칸은 <b>모양을 지키려고 그 조각을 그대로 두고</b> 색만 <see cref="Palette.TopCell"/>(불투명)로 칠하고
        /// (GUI Pro 원본은 #1E1E1F 알파 0.749 = 반투명이라 무늬가 비쳤다), 바탕이 없는 칸(아바타·전투력)은 <see cref="CellBgName"/> 이름의 fr.rect 를 맨 뒤에 깐다. raycast 는 끈다.
        /// </summary>
        static void Opaque(RectTransform cell)
        {
            if (cell == null) return;
            for (int i = 0; i < cell.childCount; i++)
            {
                if (cell.GetChild(i).name != "Bg") continue;
                var own = cell.GetChild(i).GetComponent<Image>();
                if (own == null) continue;
                own.color = Palette.TopCell;
                return;
            }
            var bg = UiKit.Panel(cell, CellBgName, "fr.rect", Palette.TopCell);
            UiKit.Stretch(bg.rectTransform);
            bg.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// T106 ⓑⓒ — 탑바 줄부터 <b>화면 맨 위까지</b> 이어지는 프레임 띠 한 장(<see cref="FrameName"/> · 형제 맨 뒤 = 칸·글자 아래 · raycast 끔).
        /// 색은 레퍼런스 실측(<see cref="Palette.TopFrame"/>) · 위·좌·우로 <see cref="FrameOverscan"/> 만큼 뻗어 SafeArea(노치)와 레터박스를 덮는다.
        /// T72 7항 ⓐ 의 «줄만 두르던 링» 은 여기서 뺐다 — 띠가 위로 이어지므로 링을 두면 이어진 띠 한가운데에 가로줄이 생긴다(결정 254).
        /// </summary>
        static void FrameBand(RectTransform root)
        {
            var band = UiKit.Panel(root, FrameName, "fr.rect", Palette.TopFrame);
            var brt = band.rectTransform;
            UiKit.Stretch(brt);
            brt.offsetMin = new Vector2(-FrameOverscan, 0f);
            brt.offsetMax = new Vector2(FrameOverscan, FrameOverscan);
            band.transform.SetAsFirstSibling();
        }

        /// <summary>parent(프레임 크기 화면 루트) 안에 표 ① 자리로 세운다. showPower=false 면 전투력 칸을 뺀다(레퍼런스에 전투력이 없는 화면용).</summary>
        public static TopBar Build(App app, RectTransform parent, bool showPower = true)
        {
            var tb = new TopBar(app);
            var root = UiKit.Rect(parent, "TopBar"); UiKit.Pct(root, Layout.LobbyTopBar); tb.Root = root;
            var top = Layout.LobbyTopBar;
            // 아바타 — ProfileFrame_02 조각(색은 프로필에서 고른다 · T96-profile · 기본 노랑) · 조각은 본래 크기 그대로 두고 아바타 칸에 배율로 · 누르면 프로필 팝업
            var slot = UiKit.Rect(root, "Avatar"); UiKit.Pct(slot, Layout.LobbyAvatar.Within(top)); tb.Avatar = slot;
            Opaque(slot);   // T72 7항 ⓑ — 칸 제 불투명 바탕(노란 초상 프레임 뒤로 패턴이 비치지 않게)
            // T96-profile — 테두리 조각은 «고른 색»(세이브 · 기본 노랑 = 종전 UserInfo_01_Slider 안의 그것과 같은 프리팹)을 카탈로그에서 바로 세운다
            AvatarFrame(app, tb, slot);
            // 아바타를 누르면 프로필(아바타 고르기) — T96-profile · 조각에 버튼이 없으므로 칸 자체에 붙인다
            UiKit.Clickable(slot, () => Profile.OpenAvatar(app));
            // 상단 초상은 정지 그림(T68 ② · 주인 «로비 주인공 아이콘이 계속 움직인다») — 장비 화면 가운데 큰 캐릭터(GearScreen)는 그대로 움직인다
            tb.Hero.SetStill(true);
            // 전투력 — 칼 아이콘 + 주황 큰 숫자(숫자만 · 레퍼런스에 라벨 없음)
            if (showPower)
            {
                var pw = UiKit.Rect(root, "PowerCell"); UiKit.Pct(pw, Layout.LobbyPower.Within(top)); tb.PowerCell = pw;
                // T72 7항 ⓑ — 전투력 칸도 제 불투명 pill 바탕 + 캡슐 테두리(재화 pill 과 같은 조각·같은 8px)
                Opaque(pw); UiKit.Bordered(pw, UiKit.BorderKeyPill);
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
            // T69-lobby — 재화 pill 2개에 «검은 아웃라인»(레퍼런스 01 의 골드·보석 pill 은 검은 외곽선) · 캡슐 조각(BorderKeyPill · 결정 149 가 남긴 «둥근 pill» 을 닫는다)
            // 아이콘(코인·보석)은 pill 왼쪽 끝에 걸치므로 테두리 위로 올린다(전투 HUD 바의 Cap 과 같은 처리)
            foreach (var pill in new[] { tb.GoldPill, tb.GemPill })
            {
                if (pill == null) continue;
                Opaque(pill);   // T72 7항 ⓑ — 조각의 캡슐 바탕을 불투명으로(원본 알파 0.749 → 1 · 무늬가 숫자 뒤에서 어른거리던 곳)
                UiKit.Bordered(pill, UiKit.BorderKeyPill);
                var picon = UiKit.Find(pill, "Icon");
                if (picon != null) picon.SetAsLastSibling();
            }
            // T72 7항 ⓐ + T106 ⓑⓒ — 줄 전체를 불투명 띠로 깔되(패턴 침범 0) 그 띠가 탑바 위 화면 끝까지 이어진다(레퍼런스 01 의 상단 띠가 그렇게 생겼다)
            FrameBand(root);
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
            AvatarFrame(_app, this, Avatar);   // T96-profile — 프로필에서 색을 고르면 세이브만 바뀌므로 여기서 조각을 갈아 끼운다(색이 같으면 아무 일도 안 한다)
        }

        /// <summary>
        /// 아바타 칸의 테두리 조각을 «지금 고른 색»(<see cref="Profile.FrameKey"/>)으로 세운다 — <b>이미 그 색이면 아무것도 안 한다</b>.
        /// <para>
        /// 왜 <see cref="Refresh"/> 도 부르나: 프로필 팝업의 «선택» 은 세이브에 색을 남기고 화면 <c>Refresh</c> 만 부르는데,
        /// 조각은 <see cref="Build"/> 때 한 번 세워진 그대로라 <b>탑바가 옛 색으로 남았다</b>(CI #233 `ProfileTests` 빨강 · 결정 320).
        /// 색이 바뀐 순간에만 갈아 끼우므로 매 <c>Refresh</c>(재화 갱신)마다 초상이 다시 붙어 깜빡이는 일은 없다.
        /// </para>
        /// 옛 조각은 <see cref="UiKit.Clear"/> 와 같은 법으로 <b>먼저 부모에서 떼고</b> 지운다 — <c>Destroy</c> 는 프레임 끝에 처리되므로
        /// 떼지 않으면 같은 프레임에 새 조각과 옛 조각이 둘 다 <see cref="UiKit.Find"/> 에 걸린다.
        /// </summary>
        static void AvatarFrame(App app, TopBar tb, RectTransform slot)
        {
            if (app == null || tb == null || slot == null) return;
            string key = Profile.FrameKey(app.Save);
            if (UiKit.Find(slot, key) != null) return;
            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                var c = slot.GetChild(i);
                if (!c.name.StartsWith(Profile.FrameKeyPrefix, StringComparison.Ordinal)) continue;
                c.SetParent(null, false); c.gameObject.SetActive(false); UnityEngine.Object.Destroy(c.gameObject);
            }
            var frameGo = UiKit.Spawn(key, slot);
            var frame = frameGo != null ? frameGo.transform : null;
            if (frame != null)
            {
                var frt = (RectTransform)frame;
                UiKit.FitScale(frt, UiKit.PxSize(Layout.LobbyAvatar));
                var mask = UiKit.FindAny(frt, "Bg_MainColor(Mask)", "Mask"); if (mask == null) mask = frt;
                UiKit.Hide(mask, "Character");
                tb.Hero = HeroView.Attach((RectTransform)mask, HeroView.PlayerSkin(app));
                tb.Hero.SetFraming(1.6f, 0.45f);   // 가슴 위(레퍼런스 아바타)
            }
            else { tb.Hero = HeroView.Attach(slot, HeroView.PlayerSkin(app)); tb.Hero.SetFraming(1.6f, 0.45f); }
            tb.Hero?.SetStill(true);   // 상단 초상은 정지 그림(T68 ② · 갈아 끼운 뒤에도 그대로)
        }
    }

    /// <summary>
    /// 하단 탭 5칸 = <b>상점 · 장비 · 전투 · 펫 · 탤런트</b> (T107 · 주인 2026-09-07 «던전 메뉴 빼셈 — 이벤트랑 중복 · 거기에 펫 넣고 맨 오른쪽에는 탤런트» · 대장간·설정 탭은 T10 부터 없다).
    /// 대장간은 장비 화면의 «합성» 버튼으로만 · 설정은 로비의 메뉴(≡)와 전투의 일시정지에서만 연다.
    /// 로비 프리팹(Lobby_Default)의 Tab_01_BottomFlushMenu 를 다른 화면에도 같은 배선으로 세운다 — 탭 순서 = 프리팹 자식 순서(0~4) 그대로.
    /// 펫 탭 = <see cref="PetScreen"/>(레퍼런스 13 구도 껍데기 · T42) · 탤런트 탭 = 주인이 지목한 <c>Character_Talent_02</c> 프리팹 팝업(<see cref="Overlay.TalentPet"/> · 껍데기 · T107).
    /// <b>던전(이벤트)은 탭에서 빠졌다</b> — 로비 오른쪽 아래 «이벤트» 버튼으로만 열고, 열면 언제나 <see cref="EventsScreen.PageDungeon"/> 이 먼저 보인다(T107 · 주인 «이벤트 열면 무조건 던전부터»).
    /// </summary>
    public static class NavBar
    {
        // T107(주인 2026-09-07 «하단에 던전 메뉴 있는 거 빼셈 — 이벤트랑 어차피 중복됨 · 던전 메뉴 빼고 거기에 펫 넣고, 맨 오른쪽에는 탤런트»)
        public static readonly string[] Keys = { "shop", "gear", "battle", "pet", "talent" };
        static readonly string[] IconsK = { "ui.shop", "ui.bag", "ui.battle", "ui.petIcon", "ui.iconTalent" };
        public static readonly string[] Labels = { "상점", "장비", "전투", "펫", "탤런트" };

        /// <summary>하단 프레임 띠 오브젝트 이름(고정 · T106 ⓓ 게이트가 찾는다).</summary>
        public const string BottomFrameName = "BottomFrame";

        public static void Attach(GameScreen screen, RectTransform root, string current)
        {
            BottomFrame(root);
            var bar = UiKit.SpawnRt("ui.tabBar", root, Layout.TabBar);
            Wire(screen.App, bar, current);
        }

        /// <summary>
        /// 하단 프레임 띠 — 아래 SafeArea(제스처 바)·레터박스만큼 탭 바 바탕을 화면 끝까지 늘린다(T106 ⓓ · 결정 255).
        /// 탭 바 «안» 이 아니라 **형제**로 둔다(<see cref="Wire"/> 가 바의 자식 0~4 를 탭으로 배선하므로 자식을 늘리면 탭이 밀린다) ·
        /// **탭 바보다 먼저** 불러야 띠가 바 뒤에 깔린다. 탭 바를 <see cref="Attach"/> 로 세우지 않고 프리팹 것을 쓰는 화면(로비)도 이것을 부른다.
        /// </summary>
        public static Image BottomFrame(RectTransform root)
        {
            var band = UiKit.Panel(root, BottomFrameName, "fr.rect", Palette.TopFrame);
            var brt = band.rectTransform; UiKit.Pct(brt, Layout.TabBar);
            brt.offsetMin = new Vector2(-TopBar.FrameOverscan, -TopBar.FrameOverscan); brt.offsetMax = new Vector2(TopBar.FrameOverscan, 0f);
            return band;
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
                // T107 — 맨 오른쪽 «탤런트» = 주인이 지목한 Character_Talent_02 프리팹 팝업(껍데기 · Overlay.TalentPet) · 던전은 탭에서 빠지고 로비 «이벤트» 로만 연다
                case "talent": app.Overlay.TalentPet("talent"); break;
                // 펫은 T42 부터 화면(PetScreen)
                default: app.Overlay.Close(); app.ShowScreen(key); break;
            }
        }
        public static void Refresh(RectTransform root) { var bar = UiKit.Find(root, "ui.tabBar"); if (bar != null) bar.SetAsLastSibling(); }
    }
}
