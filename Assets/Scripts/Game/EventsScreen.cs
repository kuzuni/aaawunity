using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 던전·아레나 <b>껍데기</b> 화면(T43 · 주인 지시 2026-09-06 ⓔ «시스템이 없는 화면은 레이아웃 껍데기» · T30 «World_Dungeon_List 그대로» 는 폐기 — 그 프리팹은 쓰지 않는다).
    /// 구도의 정본 = docs/ref 20~26 + ref-layout ⑫~⑱ 표(<see cref="Layout.DgTitle"/> 계열 · 프레임 % · ±3%p). 한 화면(«events») 안에 페이지 4장이 있고 한 번에 하나만 켠다:
    /// <list type="bullet">
    /// <item><b>던전</b>(20 · <see cref="PageDungeon"/>) — 제목 «던전» + 밑줄 + 부제 → 큰 카드 2(제목 띠 + 티켓 · 그림 · «획득 가능» 보상 아이콘 · 입장(주황 · !)) → «준비 중» 카드 → 바닥 띠(뒤로 ◀ + 던전/PvP 2탭). 카드 «입장» → 던전 세부 팝업(21).</item>
    /// <item><b>PvP</b>(22 · <see cref="PagePvp"/>) — 제목 «PvP» → 아레나 카드(경기장 그림 · 시즌 타이머 · 티어 · 입장) → «준비 중» → 같은 바닥 띠. «입장» → 아레나 입장 페이지.</item>
    /// <item><b>아레나 입장</b>(23 · <see cref="PageArena"/>) — 시상대 무대(티어 제목 · 시즌 타이머 · 1·2·3위 초상+배너 · 오른쪽 위 보상·상인) → 순위 목록(4위~) → 승급 안내 → 바닥(뒤로 + 도전 🎫x1). 도전 → 도전 팝업(24) · 보상 → 순위 보상 팝업(25) · 상인 → 상인 페이지.</item>
    /// <item><b>상인</b>(26 · <see cref="PageMerchant"/>) — 상인 배너(제목 · 시즌 타이머) → 3열 상품 격자(세로 스크롤) → 바닥(뒤로).</item>
    /// </list>
    /// <b>전부 표시만</b> — 던전·아레나·상인 시스템은 없다: 입장/도전/소탕/새로고침/상품/탭(일일·시즌) 버튼은 눌러도 아무 일 없고, 페이지 이동(입장·보상·상인·뒤로·2탭)과 «탭하여 닫기» 만 동작한다.
    /// 숫자는 레퍼런스 값을 베끼지 않는다(티켓 0 · 점수 0 · 이름 «—» · 시즌 타이머 «--:--:--» · 전투력만 내 값). 상단 재화 바 = 공용 <see cref="TopBar"/>(한 개 · 페이지 공용).
    /// 그림 재료는 주인 에셋만 — GUI Pro 프레임/버튼/아이콘 조각 + Environment 조각(카드 그림 = 들판·소품) · 코드 도형 0 · 새 그림 0. 팝업은 공통 문법(<see cref="UiKit.Popup"/> · 어둠 · 패널 · «탭하여 닫기»)
    /// 위에 레퍼런스대로 <b>평평한 제목 띠</b>(리본 대신 · 워커 결정 기록)를 얹는다.
    /// 이름 계약(테스트): 페이지 <c>Page:dungeon/pvp/arena/merchant</c> · 카드 <c>Card:hell/expedition/arena</c> · 입장 <c>EnterBtn</c> · <c>SoonCard</c> · 바닥 <c>Foot</c>/<c>BackBtn</c>/<c>Tab:dungeon</c>/<c>Tab:pvp</c> ·
    /// 아레나 <c>Podium</c>/<c>RankRow:N</c>/<c>ChallengeBtn</c>/<c>RewardsBtn</c>/<c>MerchantBtn</c> · 상인 <c>Goods:N</c> · 팝업 <c>Head</c>/<c>FloorCircle</c>/<c>RewardCell:N</c>/<c>FoeRow:N</c>/<c>RewardRow:N</c>.
    /// </summary>
    public sealed class EventsScreen : GameScreen
    {
        public override string Name => "events";
        public const string PageDungeon = "dungeon", PagePvp = "pvp", PageArena = "arena", PageMerchant = "merchant";

        /// <summary>던전 카드 2장(레퍼런스 «Portal to Hell»·«Expedition» → 우리말 · 시스템 없음 · 그림 = Environment 조각 · 티켓 아이콘 · 보상 아이콘 키).</summary>
        static readonly (string key, string title, string ticket, string field, Color tint, string[] props, string[] rewards)[] Dungeons =
        {
            ("hell", "지옥의 문", "ui.iconTicketGold", "env.desert.field", new Color(0.55f, 0.18f, 0.12f), new[] { "env.desert.Stone_Gray1_07", "env.monolith", "env.desert.Stone_Gray1_03", "env.desert.Tree_Bare_01" }, new[] { "ui.iconOrb", "ui.bookBlue" }),
            ("expedition", "원정", "ui.iconTicketBlue", "env.forest.field", new Color(0.72f, 0.88f, 1.0f), new[] { "env.deepForest.Dead_Tree_Brown_01", "env.stoneBig", "env.deepForest.Dead_Tree_Brown_02", "env.forest.Stone_Gray1_11" }, new[] { "ui.iconScroll", "ui.iconKeyBlue", "ui.iconKeyPurple", "ui.iconKeyGold" }),
        };
        /// <summary>카드 그림 안 소품 자리(그림 % · 4개) · 길 띠.</summary>
        static readonly Layout.R[] PropSlots = { new Layout.R(6, 10, 22, 70), new Layout.R(36, 4, 28, 80), new Layout.R(66, 12, 22, 66), new Layout.R(80, 40, 18, 55) };
        static readonly Layout.R PicRoad = new Layout.R(0, 62, 100, 18);
        /// <summary>«획득 가능» 라벨(카드 1 기준 · 보상 아이콘 줄 바로 위 · 레퍼런스 y38.2 h1.4).</summary>
        static readonly Layout.R FoundLabel = new Layout.R(5.0f, 38.2f, 20.0f, 1.4f);
        /// <summary>아레나 상대 초상(껍데기 · 순환) · 순위 목록 줄 수 · 도전 팝업 줄 수 · 순위 보상 줄 수 · 상인 상품.</summary>
        static readonly string[] Foes = { "ui.iconFoe1", "ui.iconFoe2", "ui.iconFoe3", "ui.iconFoe4" };
        const int RankRows = 7, FoeRows = 5, RewardRows = 4;
        static readonly (string title, string icon)[] Goods =
        {
            ("다이아", "ui.iconGemPurple"), ("무기 도안", "ui.iconScroll"), ("갑옷 도안", "ui.iconScroll"), ("투구 도안", "ui.iconScroll"), ("신발 도안", "ui.iconScroll"), ("반지 도안", "ui.iconScroll"),
            ("목걸이 도안", "ui.iconScroll"), ("희귀 열쇠", "ui.iconKeyBlue"), ("에픽 열쇠", "ui.iconKeyPurple"), ("전설 열쇠", "ui.iconKeyGold"), ("부활 토큰", "ui.iconRevive"),
        };
        static readonly (string label, string icon)[] Tiers = { ("브론즈", "ui.iconMedalBronze"), ("실버", "ui.iconMedalSilver"), ("골드", "ui.iconMedal"), ("플래티넘", "ui.iconGemBlue"), ("다이아", "ui.iconGemPurple") };
        const string NoTime = "--:--:--";

        static Color FootColor => Palette.Hex("#47443F");
        static Color BgColor => Palette.Hex("#2C2B29");
        static Color CardBody => Palette.Hex("#333333");
        static Color DeepRed => Palette.Hex("#792E2B");
        static Color ArenaRed => Palette.Hex("#9F212F");
        static Color CardBlue => Palette.Hex("#4F99DE");
        static Color SoonGray => Palette.Hex("#1F1F1F");

        TopBar _top; string _page = PageDungeon;
        readonly Dictionary<string, RectTransform> _pages = new Dictionary<string, RectTransform>();
        readonly List<Text> _powerTexts = new List<Text>();
        HeroView _me;

        /// <summary>화면 열기 + 페이지 선택(탭 «던전» · 로비 «이벤트» 버튼 · 테스트).</summary>
        public static void Open(App app, string page)
        {
            app.Overlay.Close();
            app.ShowScreen("events");
            app.GetScreen<EventsScreen>()?.ShowPage(page);
        }
        public string Page => _page;

        protected override void Build()
        {
            // 배경 = 어두운 바탕(레퍼런스 #2C2B29 · GUI Pro 사각 프레임 조각 틴트) — 뒤로 클릭이 새지 않게 raycast
            var bg = UiKit.Panel(Root, "Bg", "fr.rect", BgColor); UiKit.Stretch(bg.rectTransform); bg.raycastTarget = true;
            ShowPage(_page);
            // 상단 재화 바 — 페이지 공용(맨 위에 그린다) · 이름표 «상단 바»(⑩·⑫·⑬·⑯ 공통)
            _top = TopBar.Build(App, Root); UiKit.Tag(_top.Root, "상단 바");
        }

        /// <summary>페이지 전환 — 처음 열 때 만든다(아레나 페이지의 HeroView 가 켜진 부모 밑에서 서게).</summary>
        public void ShowPage(string page)
        {
            if (string.IsNullOrEmpty(page)) page = PageDungeon;
            _page = page;
            foreach (var kv in _pages) kv.Value.gameObject.SetActive(kv.Key == page);
            if (!_pages.ContainsKey(page))
            {
                var root = UiKit.Rect(Root, "Page:" + page); UiKit.Stretch(root); root.SetAsFirstSibling();
                var bg = UiKit.Find(Root, "Bg"); if (bg != null) bg.SetAsFirstSibling();
                _pages[page] = root;
                switch (page)
                {
                    case PagePvp: BuildPvp(root); break;
                    case PageArena: BuildArena(root); break;
                    case PageMerchant: BuildMerchant(root); break;
                    default: BuildDungeon(root); break;
                }
            }
            if (_top != null) _top.Root.SetAsLastSibling();
            Refresh();
        }

        public override void Refresh()
        {
            _top?.Refresh();
            string pw = UiKit.Fmt(App.Power());
            foreach (var t in _powerTexts) if (t != null) t.text = pw;
            _me?.SetSkin(HeroView.PlayerSkin(App));
        }

        // ───────────────────────── ⑩ 던전 페이지 (20) ─────────────────────────
        void BuildDungeon(RectTransform pg)
        {
            TitleRow(pg, Layout.DgTitle, "ui.iconDungeon", "던전", "제목(Dungeons)");
            UiKit.Tag(UiKit.Label(pg, Layout.DgSub.X, Layout.DgSub.Y, Layout.DgSub.W, Layout.DgSub.H, "던전 티켓은 매일 충전됩니다", 24, Palette.Gray).transform, "부제");
            for (int i = 0; i < Dungeons.Length; i++)
            {
                var d = Dungeons[i]; float dy = i * Layout.DgCardPitch;
                var rect = i == 0 ? Layout.DgCard1 : Layout.DgCard2;
                var card = UiKit.Rect(pg, "Card:" + d.key); UiKit.Pct(card, rect);
                var body = UiKit.Spawn("ui.frameDarkBorder", card); UiKit.Stretch((RectTransform)body.transform);
                var fill = UiKit.Panel(card, "Fill", "fr.r12", CardBody); UiKit.Stretch(fill.rectTransform, 4, 4, 4, 4);
                // 제목 띠(카드 1 빨강 · 카드 2 파랑) — 왼쪽 이름 · 오른쪽 🎫 0/2
                var head = UiKit.Panel(card, "Head", "fr.r12", i == 0 ? DeepRed : CardBlue); UiKit.Pct(head.rectTransform, Shift(Layout.DgCardHead, dy).Within(rect));
                UiKit.Label(head.transform, 2.5f, 0, 60, 100, d.title, 36, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                TicketPill(head.transform, new Layout.R(84, 12, 14, 76), d.ticket, "0/2");
                // 그림(Environment 들판 + 길 + 소품 · 카드 1 = 붉은 사막(지옥) · 카드 2 = 흰 들판(설원))
                var pic = UiKit.Rect(card, "Pic"); UiKit.Pct(pic, Shift(Layout.DgCardPic, dy).Within(rect));
                Stage(pic, d.field, d.tint, d.props);
                // «획득 가능» + 보상 아이콘(초록 프레임) 줄 · 입장(주황 · 빨간 !)
                var fl = Shift(FoundLabel, dy).Within(rect);
                UiKit.Label(card, fl.X, fl.Y, fl.W, fl.H, "획득 가능", 22, Palette.White, TextAnchor.MiddleLeft);
                var rew = UiKit.Rect(card, "Rewards"); var rr = Layout.DgRewards; rr.W = d.rewards.Length == 2 ? rr.W : 40.0f;
                UiKit.Pct(rew, Shift(rr, dy).Within(rect));
                IconRow(rew, rr, d.rewards, "ui.itemFrame.green");
                var enter = UiKit.Button(card, "ui.btnOrange", "입장", Noop, Shift(Layout.DgEnter, dy).Within(rect)); enter.name = "EnterBtn"; AlertDot(enter);
                string key = d.key; UiKit.Clickable(enter, () => OpenDungeonDetail(key));
                if (i == 0)
                {
                    UiKit.Tag(card, "던전 카드 1"); UiKit.Tag(head.transform, "카드 제목 띠"); UiKit.Tag(pic, "카드 그림"); UiKit.Tag(enter, "입장 버튼"); UiKit.Tag(rew, "보상 아이콘 줄");
                }
                else UiKit.Tag(card, "던전 카드 2");
            }
            UiKit.Tag(SoonCard(pg, Layout.DgSoon), "준비 중 카드");
            Foot(pg, PageDungeon, () => App.ShowScreen("lobby"));
        }

        // ───────────────────────── ⑫ 아레나(PvP) 페이지 (22) ─────────────────────────
        void BuildPvp(RectTransform pg)
        {
            TitleRow(pg, Layout.ArTitle, "ui.iconPvp", "PvP", "제목(PvP)");
            UiKit.Tag(UiKit.Label(pg, Layout.ArSub.X, Layout.ArSub.Y, Layout.ArSub.W, Layout.ArSub.H, "PvP 티켓은 매일 충전됩니다", 24, Palette.Gray).transform, "부제");
            var rect = Layout.ArCard;
            var card = UiKit.Rect(pg, "Card:arena"); UiKit.Pct(card, rect);
            var body = UiKit.Spawn("ui.frameDarkBorder", card); UiKit.Stretch((RectTransform)body.transform);
            var fill = UiKit.Panel(card, "Fill", "fr.r12", CardBody); UiKit.Stretch(fill.rectTransform, 4, 4, 4, 4);
            var head = UiKit.Panel(card, "Head", "fr.r12", ArenaRed); UiKit.Pct(head.rectTransform, Layout.ArCardHead.Within(rect));
            UiKit.Label(head.transform, 2.5f, 0, 60, 100, "아레나", 36, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            TicketPill(head.transform, new Layout.R(84, 12, 14, 76), "ui.iconTokenRed", "0/5");
            // 경기장 그림 = 모래 들판 + 기둥(석주) + 돌
            var pic = UiKit.Rect(card, "Pic"); UiKit.Pct(pic, Layout.ArCardPic.Within(rect));
            Stage(pic, "env.desert.field", new Color(0.93f, 0.82f, 0.55f), new[] { "env.monolith", "env.stoneBig", "env.monolith", "env.desert.Stone_Gray1_05" });
            var sky = UiKit.Panel(pic, "Sky", "fr.rect", Palette.Hex("#79C8F2")); UiKit.Pct(sky.rectTransform, 0, 0, 100, 42); sky.transform.SetSiblingIndex(1);
            var season = UiKit.Rect(card, "Season"); UiKit.Pct(season, Layout.ArSeason.Within(rect));
            UiKit.Label(season, 0, 0, 100, 100, "시즌 종료까지: " + NoTime, 22, Palette.White, TextAnchor.MiddleLeft);
            var enter = UiKit.Button(card, "ui.btnOrange", "입장", () => ShowPage(PageArena), Layout.ArEnter.Within(rect)); enter.name = "EnterBtn"; AlertDot(enter);
            var tier = UiKit.Rect(card, "Tier"); UiKit.Pct(tier, Layout.ArTier.Within(rect));
            var med = UiKit.Icon(tier, "Icon", "ui.iconMedalBronze"); UiKit.Pct(med.rectTransform, 0, 0, 24, 100);
            UiKit.Label(tier, 28, 0, 72, 100, "브론즈", 34, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            UiKit.Tag(card, "아레나 카드"); UiKit.Tag(head.transform, "카드 제목 띠"); UiKit.Tag(pic, "카드 그림"); UiKit.Tag(season, "시즌 타이머"); UiKit.Tag(enter, "입장 버튼"); UiKit.Tag(tier, "티어 줄");
            UiKit.Tag(SoonCard(pg, Layout.ArSoon), "준비 중 카드");
            Foot(pg, PagePvp, () => App.ShowScreen("lobby"));
        }

        // ───────────────────────── ⑬ 아레나 입장 화면 (23) ─────────────────────────
        void BuildArena(RectTransform pg)
        {
            // 시상대 무대 = 어두운 성 안(사각 프레임 조각) + 붉은 카펫 + 양쪽 기둥(어두운 상자) + 횃불 + 갈색 턱
            var stage = UiKit.Panel(pg, "Stage", "fr.rect", Palette.Hex("#262A31")); UiKit.Pct(stage.rectTransform, Layout.AeStage); UiKit.Tag(stage.transform, "시상대 무대");
            {
                var carpet = UiKit.Panel(stage.transform, "Carpet", "fr.rect", Palette.Hex("#7A1424")); UiKit.Pct(carpet.rectTransform, 38, 0, 24, 100);
                var pl = UiKit.Spawn("ui.frameDark", stage.transform); UiKit.Pct((RectTransform)pl.transform, 2, 8, 9, 88);
                var pr = UiKit.Spawn("ui.frameDark", stage.transform); UiKit.Pct((RectTransform)pr.transform, 89, 8, 9, 88);
                var f1 = UiKit.Icon(stage.transform, "Torch", "ui.fire"); UiKit.Pct(f1.rectTransform, 5, 8, 8, 12);
                var f2 = UiKit.Icon(stage.transform, "Torch", "ui.fire"); UiKit.Pct(f2.rectTransform, 87, 8, 8, 12);
                var ledge = UiKit.Panel(stage.transform, "Ledge", "fr.rect", Palette.Hex("#6B4A2E")); UiKit.Pct(ledge.rectTransform, 0, 94, 100, 6);
            }
            // 티어 제목 · 시즌 타이머 · 오른쪽 위 보상/상인
            var tier = UiKit.Rect(pg, "TierTitle"); UiKit.Pct(tier, Layout.AeTier); UiKit.Tag(tier, "티어 제목");
            { var m = UiKit.Icon(tier, "Icon", "ui.iconMedalBronze"); UiKit.Pct(m.rectTransform, 0, 0, 22, 100); UiKit.Label(tier, 26, 0, 74, 100, "브론즈", 40, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold; }
            var season = UiKit.Rect(pg, "Season"); UiKit.Pct(season, Layout.AeSeason); UiKit.Tag(season, "시즌 타이머");
            { var c = UiKit.Icon(season, "Icon", "ui.iconClock"); UiKit.Pct(c.rectTransform, 0, 0, 10, 100); UiKit.Label(season, 12, 0, 88, 100, "시즌 종료까지: " + NoTime, 24, Palette.White, TextAnchor.MiddleLeft); }
            var side = UiKit.Rect(pg, "SideIcons"); UiKit.Pct(side, Layout.AeSideIcons); UiKit.Tag(side, "우측 아이콘 열(2개)");
            {
                var a = UiKit.Rect(side, "RewardsBtn"); UiKit.Pct(a, 0, 0, 100, 50); var ai = UiKit.Icon(a, "Icon", "ui.iconGiftBlue"); UiKit.Pct(ai.rectTransform, 15, 0, 70, 66); UiKit.Label(a, 0, 66, 100, 34, "보상", 22, Palette.White); UiKit.Clickable(a, OpenRankRewards);
                var b = UiKit.Rect(side, "MerchantBtn"); UiKit.Pct(b, 0, 50, 100, 50); var bi = UiKit.Icon(b, "Icon", "ui.iconMerchant"); UiKit.Pct(bi.rectTransform, 15, 0, 70, 66); UiKit.Label(b, 0, 66, 100, 34, "상인", 22, Palette.White); UiKit.Clickable(b, () => ShowPage(PageMerchant));
            }
            // 시상대 초상 3(가운데 = 나 · HeroView 가슴 위) + 왕관 번호 + «나» 꼬리표 · 배너 3(이름 + 🏆 점수)
            var podium = UiKit.Rect(pg, "Podium"); UiKit.Stretch(podium);
            var p1 = Portrait(podium, "Portrait:1", Layout.AePortrait1, "ui.itemFrame.yellow", null); _me = HeroView.Attach(UiKit.Find(p1, "Inner") as RectTransform, HeroView.PlayerSkin(App)); _me.SetFraming(1.6f, 0.45f);
            var p2 = Portrait(podium, "Portrait:2", Layout.AePortrait2, "ui.itemFrame.plum", Foes[0]);
            var p3 = Portrait(podium, "Portrait:3", Layout.AePortrait3, "ui.itemFrame.green", Foes[1]);
            Crown(podium, Layout.AePortrait1, "ui.iconCrownGold", "1"); Crown(podium, Layout.AePortrait2, "ui.iconCrownSilver", "2"); Crown(podium, Layout.AePortrait3, "ui.iconCrownBronze", "3");
            { var you = UiKit.Panel(podium, "You", "fr.r12", Palette.Hex("#C2223B")); UiKit.Pct(you.rectTransform, 44.2f, 26.6f, 11.4f, 1.6f); UiKit.Label(you.transform, 0, 0, 100, 100, "나", 18, Palette.White); }
            UiKit.TagGroup(podium, "시상대 초상(3개)", p1, p2, p3); UiKit.Tag(p1, "1위 초상");
            var b1 = Banner(podium, "Banner:1", Layout.AeBanner1, Palette.Hex("#8E1C2F"), "나", true);
            var b2 = Banner(podium, "Banner:2", Layout.AeBanner2, Palette.Hex("#2E6FB5"), "—", false);
            var b3 = Banner(podium, "Banner:3", Layout.AeBanner3, Palette.Hex("#2E8B63"), "—", false);
            UiKit.TagGroup(podium, "시상대 배너(3개)", b1, b2, b3);
            // 순위 목록(4위~ · 세로 스크롤 · 바닥 띠에 가린다)
            var list = ScrollBox(pg, "RankList", Layout.AeList, RankRows * Layout.AeRowPitch, out var content); UiKit.Tag(list, "순위 목록");
            for (int i = 0; i < RankRows; i++)
            {
                var r = Layout.AeRow; r.Y += i * Layout.AeRowPitch;
                var row = Place(content, "RankRow:" + (i + 4), r, Layout.AeList.Y);
                var fr = UiKit.Spawn("ui.frameDark", row); UiKit.Stretch((RectTransform)fr.transform);
                UiKit.Label(row, 2, 0, 9, 100, (i + 4).ToString(), 36, Palette.Gray).fontStyle = FontStyle.Bold;
                var pf = Portrait(row, "Face", new Layout.R(13, 10, 11.5f, 80), "ui.itemFrame.yellow", Foes[i % Foes.Length], true);
                UiKit.Label(row, 27, 6, 40, 46, "—", 32, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                var pw = UiKit.Icon(row, "PowerIcon", "ui.battle"); UiKit.Pct(pw.rectTransform, 27, 54, 4.5f, 40); UiKit.Label(row, 32.5f, 52, 30, 44, "0", 26, Palette.Orange, TextAnchor.MiddleLeft);
                var tr = UiKit.Icon(row, "Trophy", "ui.trophy"); UiKit.Pct(tr.rectTransform, 76, 22, 7, 56); UiKit.Label(row, 84, 0, 14, 100, "0", 32, Palette.Yellow, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                if (i == 0) UiKit.Tag(row, "순위 줄(1칸)");
            }
            var promo = UiKit.Panel(pg, "Promo", "fr.rect", Palette.A(Palette.Dim, 0.85f)); UiKit.Pct(promo.rectTransform, Layout.AePromo); UiKit.Tag(promo.transform, "승급 안내");
            UiKit.Label(promo.transform, 0, 0, 100, 100, "시즌이 끝나면 상위 순위가 승급합니다", 24, Palette.White);
            // 바닥: 뒤로(→ PvP 페이지) + 도전 🎫x1(→ 도전 팝업)
            Foot(pg, null, () => ShowPage(PagePvp));
            var ch = UiKit.Button(pg, "ui.btnOrange", "도전", OpenChallenge, Layout.AeChallenge); ch.name = "ChallengeBtn"; UiKit.Tag(ch, "도전 버튼");
            TicketCost(ch, "ui.iconTokenRed");
        }

        // ───────────────────────── ⑯ 상인 페이지 (26) ─────────────────────────
        void BuildMerchant(RectTransform pg)
        {
            var banner = UiKit.Panel(pg, "Banner", "fr.rect", Palette.Hex("#3F3532")); UiKit.Pct(banner.rectTransform, Layout.MeBanner); UiKit.Tag(banner.transform, "상인 배너");
            {
                var shelf = UiKit.Panel(banner.transform, "Shelf", "fr.rect", Palette.Hex("#5A4636")); UiKit.Pct(shelf.rectTransform, 0, 74, 100, 20);
                var ledge = UiKit.Panel(banner.transform, "Ledge", "fr.rect", Palette.Hex("#6B4A2E")); UiKit.Pct(ledge.rectTransform, 0, 94, 100, 6);
                var chest = UiKit.Icon(banner.transform, "Chest", "ui.iconChestRed"); UiKit.Pct(chest.rectTransform, 66, 34, 20, 48);
                var coins = UiKit.Icon(banner.transform, "Coins", "pi.coins", Palette.Yellow); UiKit.Pct(coins.rectTransform, 12, 40, 16, 40);
                var keeper = UiKit.Icon(banner.transform, "Keeper", "ui.iconMerchant"); UiKit.Pct(keeper.rectTransform, 38, 24, 24, 58);
                var bar1 = UiKit.Icon(banner.transform, "Barrel", "env.barrel"); UiKit.Pct(bar1.rectTransform, 88, 46, 10, 36);
            }
            var title = UiKit.Label(pg, Layout.MeTitle.X, Layout.MeTitle.Y, Layout.MeTitle.W, Layout.MeTitle.H, "상인", 40, Palette.White); title.fontStyle = FontStyle.Bold; UiKit.Tag(title.transform, "제목(Merchant)");
            var season = UiKit.Rect(pg, "Season"); UiKit.Pct(season, Layout.MeSeason); UiKit.Tag(season, "시즌 타이머");
            { var c = UiKit.Icon(season, "Icon", "ui.iconClock"); UiKit.Pct(c.rectTransform, 0, 0, 8, 100); UiKit.Label(season, 10, 0, 90, 100, "시즌 종료까지: " + NoTime, 22, Palette.White, TextAnchor.MiddleLeft); }
            int rows = (Goods.Length + 2) / 3;
            var grid = ScrollBox(pg, "Goods", Layout.MeGrid, (rows - 1) * Layout.MeRowPitch + Layout.MeCard.H + 1.5f, out var content); UiKit.Tag(grid, "상품 격자");
            for (int i = 0; i < Goods.Length; i++)
            {
                var g = Goods[i]; var r = Layout.MeCard; r.X += (i % 3) * Layout.MeColPitch; r.Y += (i / 3) * Layout.MeRowPitch;
                var card = Place(content, "Goods:" + i, r, Layout.MeGrid.Y);
                var frame = UiKit.Spawn("ui.cardFrame.blue", card); UiKit.Stretch((RectTransform)frame.transform);
                UiKit.Label(card, 4, 3, 92, 13, g.title, 26, Palette.White).fontStyle = FontStyle.Bold;
                var ic = UiKit.Rect(card, "IconCell"); UiKit.Pct(ic, 26, 20, 48, 38);
                var f = UiKit.Spawn("ui.itemFrame.blue", ic); UiKit.Stretch((RectTransform)f.transform); var im = UiKit.Icon(ic, "Icon", g.icon); UiKit.Pct(im.rectTransform, 15, 15, 70, 70);
                UiKit.Label(card, 4, 62, 92, 12, "한도 —", 22, Palette.White);
                var price = UiKit.Panel(card, "Price", "fr.r12", Palette.Cream); UiKit.Pct(price.rectTransform, 5, 79, 90, 17);
                var coin = UiKit.Icon(price.transform, "Icon", "ui.iconArenaCoin"); UiKit.Pct(coin.rectTransform, 8, 12, 22, 76); UiKit.Label(price.transform, 32, 0, 62, 100, "—", 26, Palette.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                UiKit.Clickable(card, Noop);
                if (i == 0) UiKit.Tag(card, "상품 카드(1칸)");
            }
            Foot(pg, null, () => ShowPage(PageArena));
        }

        // ───────────────────────── ⑪ 던전 세부 팝업 (21) ─────────────────────────
        void OpenDungeonDetail(string key)
        {
            var d = Dungeons[0]; foreach (var x in Dungeons) if (x.key == key) d = x;
            var box = App.Overlay.OpenBox("ui.popup.red", "ui.title.red", d.title, Layout.DdBox, () => App.Overlay.Close());
            FlatHead(box, Layout.DdBox, Layout.DdHead, DeepRed, d.title);
            var pic = UiKit.Rect(box, "Pic"); UiKit.Pct(pic, Layout.DdPic.Within(Layout.DdBox)); Stage(pic, d.field, d.tint, d.props); UiKit.Tag(pic, "그림 띠");
            var note = UiKit.Panel(box, "Note", "fr.r12", Palette.A(Palette.Hex("#3A1216"), 0.92f)); UiKit.Pct(note.rectTransform, Layout.DdNote.Within(Layout.DdBox)); UiKit.Tag(note.transform, "조건 문구");
            UiKit.Label(note.transform, 2, 0, 96, 100, "전설·신화 특전만 등장", 22, Palette.Red);
            var arrow = UiKit.Icon(box, "FloorPrev", "pi.arrow_left", Palette.Cream); UiKit.Pct(arrow.rectTransform, Layout.DdArrow.Within(Layout.DdBox)); UiKit.Clickable(arrow.transform, Noop); UiKit.Tag(arrow.transform, "층수 화살표");
            var circle = UiKit.Panel(box, "FloorCircle", "fr.circle", Palette.Hex("#141414")); UiKit.Pct(circle.rectTransform, Layout.DdFloor.Within(Layout.DdBox)); UiKit.Tag(circle.transform, "층수 원");
            UiKit.Label(circle.transform, 0, 8, 100, 56, "1", 56, Palette.Orange).fontStyle = FontStyle.Bold; UiKit.Label(circle.transform, 0, 62, 100, 30, "층", 22, Palette.Orange);
            var rewards = UiKit.Spawn("ui.frameDark", box); var rrt = (RectTransform)rewards.transform; rrt.name = "Rewards"; UiKit.Pct(rrt, Layout.DdRewards.Within(Layout.DdBox)); UiKit.Tag(rrt, "보상 박스");
            UiKit.Label(rrt, 0, 3, 100, 24, "보상", 28, Palette.White).fontStyle = FontStyle.Bold;
            var cells = UiKit.Rect(box, "RewardCells"); UiKit.Pct(cells, Layout.DdRewardCells.Within(Layout.DdBox));
            var icons = new List<string>(d.rewards); while (icons.Count < 4) icons.Add(icons.Count == 3 ? "ui.coin" : "ui.bookBlue");
            var cellRts = IconRow(cells, Layout.DdRewardCells, icons.ToArray(), "ui.itemFrame.green", "RewardCell:");
            for (int i = 0; i < 2 && i < cellRts.Count; i++) { var first = UiKit.Panel(cellRts[i], "First", "fr.r12", Palette.Red); UiKit.Pct(first.rectTransform, 30, -22, 76, 26); UiKit.Label(first.transform, 0, 0, 100, 100, "첫 클리어", 12, Palette.White); }
            UiKit.TagGroup(box, "보상 칸(4개)", cellRts.ToArray());
            var ticket = UiKit.Rect(box, "Ticket"); UiKit.Pct(ticket, Layout.DdTicket.Within(Layout.DdBox)); UiKit.Tag(ticket, "티켓 줄");
            { var ti = UiKit.Icon(ticket, "Icon", d.ticket); UiKit.Pct(ti.rectTransform, 10, 0, 34, 100); UiKit.Label(ticket, 50, 0, 50, 100, "0", 28, Palette.White, TextAnchor.MiddleLeft); }
            var bt = Layout.DdBtns; float half = bt.W * 0.485f;
            var sweep = UiKit.Button(box, "ui.btnBlue", "소탕", Noop, new Layout.R(bt.X, bt.Y, half, bt.H).Within(Layout.DdBox)); sweep.name = "SweepBtn"; TicketCost(sweep, d.ticket);
            var chal = UiKit.Button(box, "ui.btnOrange", "도전", Noop, new Layout.R(bt.X + bt.W - half, bt.Y, half, bt.H).Within(Layout.DdBox)); chal.name = "ChallengeBtn"; TicketCost(chal, d.ticket);
            UiKit.TagGroup(box, "버튼 2개", sweep, chal);
            TagClose();
        }

        // ───────────────────────── ⑭ 도전 팝업 (24) ─────────────────────────
        void OpenChallenge()
        {
            var box = App.Overlay.OpenBox("ui.popup", "ui.titleBrown", "도전", Layout.AcBox, () => App.Overlay.Close());
            FlatHead(box, Layout.AcBox, Layout.AcHead, Palette.Hex("#6E6A64"), "도전");
            var info = UiKit.Rect(box, "InfoRow"); UiKit.Pct(info, Layout.AcInfoRow.Within(Layout.AcBox)); UiKit.Tag(info, "티켓·전투력 줄");
            {
                var pill = UiKit.Panel(info, "TicketPill", "fr.r12", Palette.Hex("#1E1E1E")); UiKit.Pct(pill.rectTransform, 0, -30, 28, 160);
                var ti = UiKit.Icon(pill.transform, "Icon", "ui.iconTokenRed"); UiKit.Pct(ti.rectTransform, 2, 5, 24, 90); UiKit.Label(pill.transform, 30, 0, 66, 100, "0", 28, Palette.White);
                var pw = UiKit.Icon(info, "PowerIcon", "ui.battle"); UiKit.Pct(pw.rectTransform, 70, -30, 8, 160);
                var pt = UiKit.Label(info, 79, -30, 21, 160, "0", 32, Palette.Orange, TextAnchor.MiddleLeft); pt.fontStyle = FontStyle.Bold; _powerTexts.Add(pt); pt.text = UiKit.Fmt(App.Power());
            }
            var list = UiKit.Rect(box, "FoeList"); UiKit.Pct(list, Layout.AcList.Within(Layout.AcBox)); UiKit.Tag(list, "상대 목록(5줄)");
            for (int i = 0; i < FoeRows; i++)
            {
                var r = Layout.AcRow; r.Y += i * Layout.AcRowPitch;
                var row = UiKit.Rect(box, "FoeRow:" + i); UiKit.Pct(row, r.Within(Layout.AcBox));
                var fr = UiKit.Spawn("ui.frameDark", row); UiKit.Stretch((RectTransform)fr.transform);
                Portrait(row, "Face", new Layout.R(2.5f, 12, 11.5f, 76), "ui.itemFrame.yellow", Foes[i % Foes.Length], true);
                UiKit.Label(row, 16, 6, 44, 42, "—", 30, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                Pill(row, new Layout.R(16, 54, 19, 38), "ui.battle", "0", Palette.Orange); Pill(row, new Layout.R(37, 54, 19, 38), "ui.trophy", "0", Palette.Yellow);
                var br = Layout.AcRowBtn; br.Y += i * Layout.AcRowPitch;
                var b = UiKit.Button(box, "ui.btnOrange", "도전", Noop, br.Within(Layout.AcBox)); b.name = "FoeBtn:" + i; TicketCost(b, "ui.iconTokenRed");
                if (i == 0) { UiKit.Tag(row, "상대 줄(1칸)"); UiKit.Tag(b, "줄 도전 버튼"); }
            }
            var refresh = UiKit.Button(box, "ui.btnOrange", "무료 새로고침", Noop, Layout.AcRefresh.Within(Layout.AcBox)); refresh.name = "RefreshBtn"; UiKit.Tag(refresh, "무료 새로고침 버튼");
            TagClose();
        }

        // ───────────────────────── ⑮ 순위 보상 팝업 (25) ─────────────────────────
        void OpenRankRewards()
        {
            var box = App.Overlay.OpenBox("ui.popup", "ui.titleBrown", "순위 보상", Layout.RrBox, () => App.Overlay.Close());
            FlatHead(box, Layout.RrBox, Layout.RrHead, Palette.Hex("#6E6A64"), "순위 보상");
            var band = UiKit.Panel(box, "Tiers", "fr.rect", ArenaRed); UiKit.Pct(band.rectTransform, Layout.RrTiers.Within(Layout.RrBox)); UiKit.Tag(band.transform, "티어 띠");
            UiKit.Ensure<RectMask2D>(band.gameObject);
            for (int i = 0; i < Tiers.Length; i++)
            {
                var cell = UiKit.Rect(band.transform, "Tier:" + i); UiKit.Pct(cell, i * 20.5f, 0, 20, 100);
                var ic = UiKit.Icon(cell, "Icon", Tiers[i].icon); UiKit.Pct(ic.rectTransform, 22, 10, 56, 54); UiKit.Label(cell, 0, 66, 100, 30, Tiers[i].label, 22, Palette.White).fontStyle = FontStyle.Bold;
                if (i > 0) { var dash = UiKit.Panel(band.transform, "Dash", "fr.rect", Palette.Hex("#5A1520")); UiKit.Pct(dash.rectTransform, i * 20.5f - 2.2f, 44, 1.6f, 6); }
            }
            var timer = UiKit.Rect(box, "Timer"); UiKit.Pct(timer, Layout.RrTimer.Within(Layout.RrBox)); UiKit.Tag(timer, "리셋 타이머");
            { var c = UiKit.Icon(timer, "Icon", "ui.iconClock"); UiKit.Pct(c.rectTransform, 0, 0, 10, 100); UiKit.Label(timer, 12, 0, 88, 100, "초기화까지: " + NoTime, 24, Palette.White, TextAnchor.MiddleLeft); }
            var rn = Layout.RrNote.Within(Layout.RrBox);
            UiKit.Tag(UiKit.Label(box, rn.X, rn.Y, rn.W, rn.H, "순위 보상은 우편으로 지급됩니다", 22, Palette.White).transform, "안내 문구");
            var list = UiKit.Rect(box, "RewardList"); UiKit.Pct(list, Layout.RrList.Within(Layout.RrBox)); UiKit.Tag(list, "보상 목록(4줄)");
            string[] crowns = { "ui.iconCrownGold", "ui.iconCrownSilver", "ui.iconCrownBronze" };
            for (int i = 0; i < RewardRows; i++)
            {
                var r = Layout.RrRow; r.Y += i * Layout.RrRowPitch;
                var row = UiKit.Rect(box, "RewardRow:" + i); UiKit.Pct(row, r.Within(Layout.RrBox));
                var fr = UiKit.Spawn("ui.frameDark", row); UiKit.Stretch((RectTransform)fr.transform);
                if (i < crowns.Length) { var cr = UiKit.Icon(row, "Crown", crowns[i]); UiKit.Pct(cr.rectTransform, 2, 8, 14, 84); UiKit.Label(row, 2, 30, 14, 50, (i + 1).ToString(), 24, Palette.White).fontStyle = FontStyle.Bold; }
                else UiKit.Label(row, 2, 0, 14, 100, (i + 1).ToString(), 34, Palette.White).fontStyle = FontStyle.Bold;
                RewardCell(row, new Layout.R(20, 8, 13, 84), "ui.itemFrame.green", "ui.iconArenaCoin"); RewardCell(row, new Layout.R(35, 8, 13, 84), "ui.itemFrame.plum", "ui.iconGemPurple");
                if (i == 0) UiKit.Tag(row, "보상 줄(1칸)");
            }
            var tabs = UiKit.Rect(box, "Tabs"); UiKit.Pct(tabs, Layout.RrTabs.Within(Layout.RrBox)); UiKit.Tag(tabs, "하단 탭(2개)");
            var daily = UiKit.Button(tabs, "ui.btnGray", "일일 보상", Noop, new Layout.R(0, 0, 48.5f, 100)); daily.name = "DailyTab";
            var seasonTab = UiKit.Button(tabs, "ui.btnGray", "시즌 보상", Noop, new Layout.R(51.5f, 0, 48.5f, 100)); seasonTab.name = "SeasonTab"; UiKit.Ensure<CanvasGroup>(seasonTab.gameObject).alpha = 0.7f;
            TagClose();
        }

        // ───────────────────────── 조립 도우미 ─────────────────────────
        static void Noop() { }
        static Layout.R Shift(Layout.R r, float dy) { r.Y += dy; return r; }

        /// <summary>제목 줄 = 아이콘 + 굵은 글자(가운데) + 밑줄(프리팹 Title_LineDeco 의 선 조각 · 가운데 ◇).</summary>
        void TitleRow(RectTransform pg, Layout.R rect, string icon, string text, string tag)
        {
            var row = UiKit.Rect(pg, "Title"); UiKit.Pct(row, rect); UiKit.Tag(row, tag);
            var ic = UiKit.Icon(row, "Icon", icon); UiKit.Pct(ic.rectTransform, 0, -10, 22, 120);
            UiKit.Label(row, 26, 0, 74, 100, text, 44, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            var line = UiKit.Spawn("ui.lineTitle", pg); var lrt = (RectTransform)line.transform; lrt.name = "TitleLine";
            var t = UiKit.Find(lrt, "Text (TMP)"); if (t != null) t.gameObject.SetActive(false);
            var deco = UiKit.Find(lrt, "LineDeco") as RectTransform;
            if (deco != null) { deco.SetParent(pg, false); UiKit.Pct(deco, Layout.DgTitleLine); deco.name = "TitleLine"; UiKit.Tag(deco, "제목 밑줄"); foreach (var g in deco.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false; line.SetActive(false); }
            else { UiKit.Pct(lrt, Layout.DgTitleLine); UiKit.Tag(lrt, "제목 밑줄"); }
        }
        /// <summary>«준비 중» 카드 — 어두운 테두리 상자 + 더 어두운 속 + 회색 글자(레퍼런스 «Coming Soon» + 물음표 무늬 · 무늬는 새 그림이라 생략).</summary>
        static RectTransform SoonCard(RectTransform pg, Layout.R rect)
        {
            var card = UiKit.Rect(pg, "SoonCard"); UiKit.Pct(card, rect);
            var body = UiKit.Spawn("ui.frameDarkBorder", card); UiKit.Stretch((RectTransform)body.transform);
            var fill = UiKit.Panel(card, "Fill", "fr.r12", SoonGray); UiKit.Stretch(fill.rectTransform, 4, 4, 4, 4);
            UiKit.Label(card, 0, 0, 100, 100, "준비 중", 48, Palette.Hex("#5C5C5C")).fontStyle = FontStyle.Bold;
            return card;
        }
        /// <summary>바닥 띠(회색) + 뒤로(◀ · 회색) + (tabs != null 이면) 던전/PvP 2탭 — 현재 탭은 위로 솟고 밝은 배경 + 라벨(레퍼런스).</summary>
        void Foot(RectTransform pg, string activeTab, Action onBack)
        {
            var foot = UiKit.Panel(pg, "Foot", "fr.rect", FootColor); UiKit.Pct(foot.rectTransform, Layout.DgFoot); foot.raycastTarget = true; UiKit.Tag(foot.transform, "하단 바");
            var back = UiKit.Button(pg, "ui.btnGray", "", onBack, Layout.DgBack); back.name = "BackBtn"; UiKit.Tag(back, "뒤로 버튼");
            { var t = UiKit.ButtonText(back); if (t != null) t.gameObject.SetActive(false); var ic = UiKit.Icon(back, "Icon", "pi.arrow_left", Palette.Cream); UiKit.Pct(ic.rectTransform, 30, 18, 40, 64); }
            if (activeTab == null) return;
            var tabs = UiKit.Rect(pg, "Tabs"); UiKit.Pct(tabs, Layout.DgTabs); UiKit.Tag(tabs, "던전/PvP 탭(2칸)");
            (string key, string icon, string label)[] items = { (PageDungeon, "ui.iconDungeon", "던전"), (PagePvp, "ui.iconPvp", "PvP") };
            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i]; bool on = it.key == activeTab;
                var cell = UiKit.Rect(tabs, "Tab:" + it.key); UiKit.Pct(cell, i * 50f, on ? 0 : 14, 50, on ? 100 : 86);
                var bg = UiKit.Panel(cell, "Bg", "fr.r12", on ? Palette.Hex("#6B6862") : Palette.Hex("#3B3936"));
                UiKit.Stretch(bg.rectTransform, 2, 0, 2, -10); bg.raycastTarget = true;
                var ic = UiKit.Icon(cell, "Icon", it.icon); UiKit.Pct(ic.rectTransform, 22, on ? 6 : 14, 56, on ? 56 : 72);
                if (on) UiKit.Label(cell, 0, 64, 100, 30, it.label, 22, Palette.White).fontStyle = FontStyle.Bold;
                var dot = UiKit.Spawn("ui.alertDot", cell); var dr = (RectTransform)dot.transform; dr.anchorMin = dr.anchorMax = new Vector2(1, 1); dr.pivot = new Vector2(0.5f, 0.5f); dr.anchoredPosition = new Vector2(-14, -10); dr.sizeDelta = new Vector2(40, 40);
                string key = it.key; UiKit.Clickable(cell, () => ShowPage(key));
            }
        }
        /// <summary>카드 그림 = 들판(틴트) + 길 띠 + 소품 4(RectMask2D · Environment 조각 · 코드 도형 0).</summary>
        static void Stage(RectTransform host, string field, Color tint, string[] props)
        {
            UiKit.Ensure<RectMask2D>(host.gameObject);
            var f = UiKit.Icon(host, "Field", field, tint); f.preserveAspect = false; UiKit.Stretch(f.rectTransform);
            var road = UiKit.Icon(host, "Road", "env.road", Palette.A(tint, 0.85f)); road.preserveAspect = false; UiKit.Pct(road.rectTransform, PicRoad);
            for (int i = 0; i < props.Length && i < PropSlots.Length; i++) { var p = UiKit.Icon(host, "Prop" + i, props[i]); UiKit.Pct(p.rectTransform, PropSlots[i]); }
        }
        /// <summary>제목 띠 안 오른쪽 티켓 pill(아이콘 + «0/2»).</summary>
        static void TicketPill(Transform head, Layout.R r, string icon, string text)
        {
            var cell = UiKit.Rect(head, "Ticket"); UiKit.Pct(cell, r);
            var ic = UiKit.Icon(cell, "Icon", icon); UiKit.Pct(ic.rectTransform, 0, 0, 34, 100); UiKit.Label(cell, 38, 0, 62, 100, text, 26, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        }
        /// <summary>정사각 아이콘 칸 줄(프레임 조각 + 아이콘) — 칸 한 변 = 줄 높이 · 왼쪽부터 · 간격은 남는 폭을 등분. rowRect = 줄의 프레임 % 사각형(정사각 환산용).</summary>
        static List<RectTransform> IconRow(RectTransform row, Layout.R rowRect, string[] icons, string frameKey, string namePrefix = "Cell:")
        {
            var res = new List<RectTransform>();
            float rowW = Mathf.Max(1e-3f, rowRect.W / 100f * UiKit.FrameW), rowH = Mathf.Max(1e-3f, rowRect.H / 100f * UiKit.FrameH);
            float cellW = Mathf.Min(100f, rowH / rowW * 100f);   // 줄 높이(px)를 줄 폭 % 로 — 정사각 칸
            int n = icons.Length; float gap = n > 1 ? Mathf.Max(0, (100f - n * cellW) / (n - 1)) : 0;
            for (int i = 0; i < n; i++)
            {
                var cell = UiKit.Rect(row, namePrefix + i); UiKit.Pct(cell, i * (cellW + gap), 0, cellW, 100);
                var f = UiKit.Spawn(frameKey, cell); UiKit.Stretch((RectTransform)f.transform);
                var ic = UiKit.Icon(cell, "Icon", icons[i]); UiKit.Pct(ic.rectTransform, 16, 16, 68, 68);
                res.Add(cell);
            }
            return res;
        }
        static void RewardCell(RectTransform row, Layout.R r, string frameKey, string icon)
        {
            var cell = UiKit.Rect(row, "Reward"); UiKit.Pct(cell, r);
            var f = UiKit.Spawn(frameKey, cell); UiKit.Stretch((RectTransform)f.transform);
            var ic = UiKit.Icon(cell, "Icon", icon); UiKit.Pct(ic.rectTransform, 16, 12, 68, 68);
            UiKit.Label(cell, 0, 70, 100, 28, "—", 16, Palette.White);
        }
        /// <summary>버튼 오른쪽 위 빨간 알림 점(GUI Pro 조각).</summary>
        static void AlertDot(RectTransform btn)
        {
            var d = UiKit.Spawn("ui.alertDot", btn); var dr = (RectTransform)d.transform; d.name = "Dot";
            dr.anchorMin = dr.anchorMax = new Vector2(1, 1); dr.pivot = new Vector2(0.5f, 0.5f); dr.anchoredPosition = new Vector2(-4, 4); dr.sizeDelta = new Vector2(52, 52);
        }
        /// <summary>버튼 글자 아래 «🎫 x1» 줄(아이콘 + 글자) — 글자를 위로 올리고 아래에 작은 줄.</summary>
        static void TicketCost(RectTransform btn, string icon)
        {
            var t = UiKit.ButtonText(btn); if (t != null) { var trt = t.rectTransform; trt.anchorMin = new Vector2(0, 0.42f); trt.anchorMax = new Vector2(1, 1); trt.offsetMin = trt.offsetMax = Vector2.zero; }
            var cost = UiKit.Rect(btn, "Cost"); UiKit.Pct(cost, 30, 58, 40, 34);
            var ic = UiKit.Icon(cost, "Icon", icon); UiKit.Pct(ic.rectTransform, 0, 0, 40, 100); UiKit.Label(cost, 44, 0, 56, 100, "x1", 22, Palette.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        }
        /// <summary>초상 칸 = 프레임 조각 + (아이콘 | HeroView 자리 «Inner»).</summary>
        static RectTransform Portrait(RectTransform parent, string name, Layout.R r, string frameKey, string icon, bool aspect = false)
        {
            var cell = UiKit.Rect(parent, name); UiKit.Pct(cell, r);
            if (aspect) { var arf = UiKit.Ensure<AspectRatioFitter>(cell.gameObject); arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent; arf.aspectRatio = 1f; }
            var f = UiKit.Spawn(frameKey, cell); UiKit.Stretch((RectTransform)f.transform);
            var inner = UiKit.Rect(cell, "Inner"); UiKit.Pct(inner, 10, 10, 80, 80); UiKit.Ensure<RectMask2D>(inner.gameObject);
            if (icon != null) { var ic = UiKit.Icon(inner, "Icon", icon); UiKit.Stretch(ic.rectTransform); }
            return cell;
        }
        static void Crown(RectTransform parent, Layout.R portrait, string icon, string num)
        {
            var c = UiKit.Rect(parent, "Crown:" + num); UiKit.Pct(c, portrait.X + portrait.W * 0.25f, portrait.Y - 3.0f, portrait.W * 0.5f, 2.8f);
            var ic = UiKit.Icon(c, "Icon", icon); UiKit.Stretch(ic.rectTransform); UiKit.Label(c, 0, 25, 100, 75, num, 20, Palette.White).fontStyle = FontStyle.Bold;
        }
        /// <summary>시상대 배너(펜던트 라벨 조각 · 틴트) — 이름 + 🏆 점수.</summary>
        RectTransform Banner(RectTransform parent, string name, Layout.R r, Color color, string who, bool me)
        {
            var b = UiKit.Rect(parent, name); UiKit.Pct(b, r);
            var img = UiKit.Panel(b, "Cloth", "fr.label", color); UiKit.Stretch(img.rectTransform);
            UiKit.Label(b, 4, 10, 92, 34, who, 26, Palette.White).fontStyle = FontStyle.Bold;
            var tr = UiKit.Icon(b, "Trophy", "ui.trophy"); UiKit.Pct(tr.rectTransform, 22, 50, 20, 34); UiKit.Label(b, 46, 50, 40, 34, "0", 28, Palette.Yellow, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            if (me) { var pw = UiKit.Label(b, 4, 84, 92, 16, UiKit.Fmt(App.Power()), 16, Palette.Orange); _powerTexts.Add(pw); }
            return b;
        }
        static void Pill(RectTransform row, Layout.R r, string icon, string text, Color color)
        {
            var p = UiKit.Panel(row, "Pill", "fr.r12", Palette.Hex("#1E1E1E")); UiKit.Pct(p.rectTransform, r);
            var ic = UiKit.Icon(p.transform, "Icon", icon); UiKit.Pct(ic.rectTransform, 6, 10, 26, 80); UiKit.Label(p.transform, 36, 0, 60, 100, text, 22, color, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        }
        /// <summary>공통 팝업 위에 레퍼런스의 <b>평평한 제목 띠</b> — 리본(Title 조각)은 끄고 박스 윗변에 색 띠 + 굵은 흰 글자(워커 결정 기록).</summary>
        static void FlatHead(RectTransform box, Layout.R boxRect, Layout.R headRect, Color color, string title)
        {
            foreach (var key in new[] { "ui.title.red", "ui.titleBrown", "ui.title.tangerine" }) { var rb = UiKit.Find(box, key); if (rb != null) rb.gameObject.SetActive(false); }
            var head = UiKit.Panel(box, "Head", "fr.r12", color); UiKit.Pct(head.rectTransform, headRect.Within(boxRect)); head.raycastTarget = true; UiKit.Tag(head.transform, "제목 띠");
            UiKit.Label(head.transform, 4, 0, 92, 100, title, 36, Palette.White).fontStyle = FontStyle.Bold;
            UiKit.Tag(box, "팝업 박스");
        }
        void TagClose() { var tap = UiKit.Find(App.Overlay.Root, "TapToClose"); if (tap != null) UiKit.Tag(tap, "닫기 안내"); }
        /// <summary>세로 스크롤 창(RectMask2D + ScrollRect) — 내용 높이는 프레임 %.</summary>
        static RectTransform ScrollBox(RectTransform pg, string name, Layout.R rect, float contentHPct, out RectTransform content)
        {
            var view = UiKit.Rect(pg, name); UiKit.Pct(view, rect); UiKit.Ensure<RectMask2D>(view.gameObject);
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            var scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            content = UiKit.Rect(view, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero; content.sizeDelta = new Vector2(0, contentHPct / 100f * UiKit.FrameH);
            scroll.content = content; scroll.viewport = view;
            return view;
        }
        /// <summary>스크롤 Content 안 자리 — r 은 프레임 %(표값 그대로 · y 는 top 부터 아래로). 가로는 창 폭 기준 · 세로는 프레임 px(ShopScreen.Place 와 같은 식).</summary>
        static RectTransform Place(RectTransform content, string name, Layout.R r, float top)
        {
            var view = (RectTransform)content.parent; float vx = view.anchorMin.x * 100f, vw = (view.anchorMax.x - view.anchorMin.x) * 100f;
            var rt = UiKit.Rect(content, name);
            rt.anchorMin = new Vector2((r.X - vx) / vw, 1f); rt.anchorMax = new Vector2((r.X - vx + r.W) / vw, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0, -(r.Y - top + r.H) / 100f * UiKit.FrameH); rt.offsetMax = new Vector2(0, -(r.Y - top) / 100f * UiKit.FrameH);
            rt.localScale = Vector3.one;
            return rt;
        }
    }
}
