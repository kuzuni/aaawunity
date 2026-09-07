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
        /// <summary>«획득 가능» 라벨(카드 1 기준 · 보상 아이콘 줄 바로 위 · 레퍼런스 y38.2 h1.4 · T63-events 에서 h2.3 = 보조 36 × 1.4 · 보상 줄 y41.1 과 안 겹친다).</summary>
        static readonly Layout.R FoundLabel = new Layout.R(5.0f, 38.2f, 20.0f, 2.3f);
        /// <summary>
        /// 던전 세부(21) 보상 칸의 «최초» 배지 자리(칸 % · T123) — 레퍼런스 21 처럼 <b>칸 안 오른쪽 위</b>이고 위로만 살짝 걸친다.
        /// 좌우는 칸을 안 넘고(x ≥ 0 · x+w ≤ 100) 위로 넘는 폭은 칸 높이의 10% 뿐이라 «보상» 제목과 부딪치지 않는다.
        /// 세로 46% = 119px 칸에서 55px ≥ 보조 36 × 1.4(=50.4px · <see cref="TextSize.LineBox"/>)라 글자가 안 눌린다.
        /// </summary>
        static readonly Layout.R FirstBadge = new Layout.R(36f, -10f, 62f, 46f);
        /// <summary>
        /// 순위 보상 팝업(25) 하단 탭 버튼이 탭 줄에서 쓰는 세로 비율(% · T127) — 줄의 밑변 = 팝업 박스 rect 의 밑변이라
        /// 꽉 채우면 조각의 «보이는» 크림 바닥 밖으로 버튼이 삐져나온다. 72% 면 버튼 높이 5.1%p 로 레퍼런스 25(≈5.3%p)와 같고
        /// 버튼 아래 여백이 2.0%p 생긴다. 버튼 칸 세로는 166px × 0.72 ≈ 120px 이라 버튼 글자 44(칸 62px)에 넉넉하다.
        /// </summary>
        const float TabBtnH = 72f;
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
        /// <summary>아레나 껍데기 표시 이름 — 상대는 «도전자 N» · 내 자리는 게임 주인공 이름(저장 데이터에 플레이어 이름이 없다 · 워커 결정 기록 T62).</summary>
        const string MeName = "꼬마기사";
        static string FoeName(int rank) => "도전자 " + rank;
        /// <summary>껍데기 숫자 자리 — 값을 못 만들 때만 쓴다(계수 JSON 이 없을 때 · 0 을 쓰면 실제 값처럼 보인다).</summary>
        const string Dash = "—";
        /// <summary>도전 팝업(24)에 세우는 상대 5명의 순위 — 내가 1위(시상대 가운데)라 바로 아래 순위들이다(T81).</summary>
        static int FoeRank(int i) => i + 2;

        /// <summary>순위의 더미 전투력 글자(내 순위면 실제 전투력 · 계수 표가 없으면 «—») — T81 · 주인 2026-09-07.</summary>
        string DummyPower(int rank)
        {
            var d = App != null && App.Data != null ? App.Data.ArenaDummy : null;
            if (d == null) return Dash;
            return UiKit.FmtComma(ArenaDummy.Power(d, App.Power(), rank));
        }
        /// <summary>순위의 더미 승점(🏆) 글자 — 계수 표가 없으면 «—».</summary>
        string DummyScore(int rank)
        {
            var d = App != null && App.Data != null ? App.Data.ArenaDummy : null;
            return d == null ? Dash : UiKit.FmtComma(ArenaDummy.Score(d, rank));
        }

        static Color FootColor => Palette.Hex("#47443F");
        static Color BgColor => Palette.Hex("#2C2B29");
        static Color CardBody => Palette.Hex("#333333");
        static Color DeepRed => Palette.Hex("#792E2B");
        static Color ArenaRed => Palette.Hex("#9F212F");
        static Color CardBlue => Palette.Hex("#4F99DE");
        /// <summary>카드 제목 띠(«지옥의 문»·«원정»·«아레나») 글자 크기 — 본문 40 보다 크고 제목 60 보다 작다(띠 높이 3.6%=84px · 60 은 안 들어간다 · 레퍼런스 20 의 카드 이름도 페이지 제목보다 작다).</summary>
        const int CardTitleSize = 48;
        /// <summary>제목 줄의 아이콘 폭·아이콘과 글자 사이 간격(줄 폭 %) — T101 ⓓ 의 «가운데 덩어리» 계산에 쓴다(전 22%/4%p 였던 것을 아이콘만 남기고 좁혔다).</summary>
        const float TitleIconPct = 16f, TitleGapPct = 2f;
        /// <summary>T72 ③ 제목 띠 그라데이션을 들여 까는 여백(px) — 띠 조각(fr.r12)의 둥근 모서리 반지름 12px 의 1/3(사각 그라데이션이 모서리 밖으로 안 삐져나오는 선 · 워커 결정 기록).</summary>
        const float HeadGradientInset = 4f;
        /// <summary>순위 줄(ListItem_Ranking) 색 — 레퍼런스 23 의 어두운 줄(몸통 · 등수 칸 · 테두리). Theme_Light 프리팹의 크림색을 덮는다(T62 회차 1).</summary>
        static Color RowBody => Palette.Hex("#3A3734");
        static Color RowLeft => Palette.Hex("#302D2A");
        static Color RowBorder => Palette.Hex("#57514A");

        TopBar _top; string _page = PageDungeon;
        /// <summary>내 전투력이 바뀌면(장비·강화) 같이 다시 써야 하는 더미 전투력 글자 — (글자, 순위).</summary>
        readonly List<KeyValuePair<Text, int>> _dummyPowerTexts = new List<KeyValuePair<Text, int>>();
        readonly Dictionary<string, RectTransform> _pages = new Dictionary<string, RectTransform>();
        readonly List<Text> _powerTexts = new List<Text>();
        /// <summary>던전 카드 제목 띠의 티켓 글자(글자, 던전 키) — 티켓이 늘면 <see cref="Refresh"/> 가 다시 쓴다(T99).</summary>
        readonly List<KeyValuePair<Text, string>> _ticketTexts = new List<KeyValuePair<Text, string>>();
        /// <summary>던전 카드 «입장» 버튼의 빨간 점(점, 던전 키) — «지금 할 일» 이 있을 때만 켠다(T99 6항).</summary>
        readonly List<KeyValuePair<GameObject, string>> _ticketDots = new List<KeyValuePair<GameObject, string>>();
        HeroView _me;
        /// <summary>T72 ② 빛살을 걸 자리 — 배치가 끝난 뒤에 한꺼번에 건다(% 앵커 아이콘은 Build 중 rect 가 0 이라 빛살 한 변이 0 이 된다 · 결정 174).</summary>
        readonly List<(RectTransform host, RectTransform icon, string key)> _lightPlan = new List<(RectTransform, RectTransform, string)>();
        /// <summary>제목 줄(아이콘·글자·줄 폭 %) — 배치가 끝난 뒤 글자 폭을 실측해 «아이콘 + 글자» 덩어리를 가운데로 옮긴다(T101 ⓓ).</summary>
        readonly List<(RectTransform row, RectTransform icon, Text text, float rowWPct)> _titlePlan = new List<(RectTransform, RectTransform, Text, float)>();
        /// <summary>T72 4항 «보이는 칸만» — 상인 페이지(26)는 상품이 11칸이라 스크롤 창과 겹치는 칸만 돌린다.</summary>
        readonly List<RectTransform> _goodsCells = new List<RectTransform>();
        ScrollRect _goodsScroll;
        readonly Vector3[] _corners = new Vector3[4];

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
                // T72 ① 배경 패턴 — 어두운 바탕(#2C2B29) 위 «흰» 무늬가 오른쪽 위로 천천히 흐른다(주인 «거의 모든 UI 에»).
                // 페이지는 처음 열 때 만들고 그때마다 맨 앞으로 오므로, 바탕 조각 바로 위(형제 1) 자리를 여기서 다시 잡아 준다(네 페이지 공용 한 장).
                UiKit.PatternBg(Root, UiKit.PatternTintDark, UiKit.PatternTileSeconds, bg != null ? 1 : 0);
                _pages[page] = root;
                switch (page)
                {
                    case PagePvp: BuildPvp(root); break;
                    case PageArena: BuildArena(root); break;
                    case PageMerchant: BuildMerchant(root); break;
                    default: BuildDungeon(root); break;
                }
                ApplyLights();
            }
            if (_top != null) _top.Root.SetAsLastSibling();
            // T72 4항 — 상인 상품 빛살은 그 페이지를 보고 있을 때, 그중에서도 스크롤 창에 걸친 칸만 돈다
            if (_goodsCells.Count > 0)
            {
                if (page == PageMerchant) UpdateGoodsSpin();
                else foreach (var c in _goodsCells) UiKit.SetLightSpinning(c, false);
            }
            Refresh();
        }

        public override void Refresh()
        {
            _top?.Refresh();
            string pw = UiKit.FmtComma(App.Power());
            foreach (var t in _powerTexts) if (t != null) t.text = pw;
            for (int i = _dummyPowerTexts.Count - 1; i >= 0; i--)
            {
                var kv = _dummyPowerTexts[i];
                if (kv.Key == null) { _dummyPowerTexts.RemoveAt(i); continue; }
                kv.Key.text = DummyPower(kv.Value);
            }
            for (int i = _ticketTexts.Count - 1; i >= 0; i--)
            {
                var kv = _ticketTexts[i];
                if (kv.Key == null) { _ticketTexts.RemoveAt(i); continue; }
                kv.Key.text = TicketText(kv.Value);
            }
            for (int i = _ticketDots.Count - 1; i >= 0; i--)
            {
                var kv = _ticketDots[i];
                if (kv.Key == null) { _ticketDots.RemoveAt(i); continue; }
                kv.Key.SetActive(Dun == null || DungeonTickets.Ready(App.Save, Dun, kv.Value, Today()));
            }
            _me?.SetSkin(HeroView.PlayerSkin(App));
        }

        // ───────────────────────── ⑩ 던전 페이지 (20) ─────────────────────────
        void BuildDungeon(RectTransform pg)
        {
            _ticketTexts.Clear(); _ticketDots.Clear();
            RollTickets();   // T99 1항 — 하루가 바뀌었으면 던전마다 «2 미만이면 2로» 채운다(더하지 않는다)
            TitleRow(pg, Layout.DgTitle, "ui.iconDungeon", "던전", "제목(Dungeons)");
            UiKit.Tag(UiKit.Label(pg, Layout.DgSub.X, Layout.DgSub.Y, Layout.DgSub.W, Layout.DgSub.H, "던전 티켓은 매일 충전됩니다", TextSize.Body, Palette.Gray).transform, "부제");
            for (int i = 0; i < Dungeons.Length; i++)
            {
                var d = Dungeons[i]; float dy = i * Layout.DgCardPitch;
                var rect = i == 0 ? Layout.DgCard1 : Layout.DgCard2;
                var card = UiKit.Rect(pg, "Card:" + d.key); UiKit.Pct(card, rect);
                var body = UiKit.Spawn("ui.frameDarkBorder", card); UiKit.Stretch((RectTransform)body.transform);
                var fill = UiKit.Panel(card, "Fill", "fr.r12", CardBody); UiKit.Stretch(fill.rectTransform, 4, 4, 4, 4);
                // T101 ⓒ(주인 «보더가 직사각형용 보더가 아니더라 · 카드 자체를 감싸는 느낌으로») — 조각(SquareSharpEdge)의 테두리 대신 9-slice 직사각형 링을 카드 rect 네 변에
                UiKit.Bordered(card);
                // 제목 띠(카드 1 빨강 · 카드 2 파랑) — 왼쪽 이름 · 오른쪽 🎫 0/2
                var head = UiKit.Panel(card, "Head", "fr.r12", i == 0 ? DeepRed : CardBlue); UiKit.Pct(head.rectTransform, Shift(Layout.DgCardHead, dy).Within(rect));
                UiKit.Gradient(head.rectTransform, inset: HeadGradientInset);
                UiKit.Label(head.transform, 2.5f, 0, 60, 100, d.title, CardTitleSize, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                _ticketTexts.Add(new KeyValuePair<Text, string>(TicketPill(head.transform, new Layout.R(84, 12, 14, 76), d.ticket, TicketText(d.key)), d.key));
                // 그림(Environment 들판 + 길 + 소품 · 카드 1 = 붉은 사막(지옥) · 카드 2 = 흰 들판(설원))
                var pic = UiKit.Rect(card, "Pic"); UiKit.Pct(pic, Shift(Layout.DgCardPic, dy).Within(rect));
                Stage(pic, d.field, d.tint, d.props);
                // T69-events: 그림 띠도 «칸» 이다 — 레퍼런스 20 의 그림은 위(제목 띠)·아래(카드 몸통)·양옆이 전부 검은 선으로 끊긴다(그림이 카드 안에서 따로 논다)
                UiKit.Bordered(pic);
                // «획득 가능» + 보상 아이콘(초록 프레임) 줄 · 입장(주황 · 빨간 !)
                var fl = Shift(FoundLabel, dy).Within(rect);
                UiKit.Label(card, fl.X, fl.Y, fl.W, fl.H, "획득 가능", TextSize.Aux, Palette.White, TextAnchor.MiddleLeft, kind: TextKind.Aux);
                var rew = UiKit.Rect(card, "Rewards"); var rr = Layout.DgRewards; rr.W = d.rewards.Length == 2 ? rr.W : 40.0f;
                UiKit.Pct(rew, Shift(rr, dy).Within(rect));
                // T72 ② 보상 아이콘 뒤 빛살(작은 칸이라 Effect_Light_02) — 던전 카드는 항상 보이므로 스크롤 제한 없이 돈다
                foreach (var cell in IconRow(rew, rr, d.rewards, "ui.itemFrame.green")) PlanLight(cell);
                var enter = UiKit.Button(card, "ui.btnOrange", "입장", Noop, Shift(Layout.DgEnter, dy).Within(rect)); enter.name = "EnterBtn";
                // T99 6항 — 빨간 점은 «지금 할 일이 있을 때만»(티켓이 있거나 광고로 하나 받을 수 있다) · 상태가 바뀌면 Refresh 가 켜고 끈다
                var dot = AlertDot(enter);
                if (dot != null) { dot.SetActive(Dun == null || DungeonTickets.Ready(App.Save, Dun, d.key, Today())); _ticketDots.Add(new KeyValuePair<GameObject, string>(dot, d.key)); }
                string key = d.key; UiKit.Clickable(enter, () => OpenDungeonDetail(key));
                if (i == 0)
                {
                    UiKit.Tag(card, "던전 카드 1"); UiKit.Tag(head.transform, "카드 제목 띠"); UiKit.Tag(pic, "카드 그림"); UiKit.Tag(enter, "입장 버튼"); UiKit.Tag(rew, "보상 아이콘 줄");
                }
                else UiKit.Tag(card, "던전 카드 2");
            }
            Foot(pg, PageDungeon, () => App.ShowScreen("lobby"));
        }

        // ───────────────────────── ⑫ 아레나(PvP) 페이지 (22) ─────────────────────────
        void BuildPvp(RectTransform pg)
        {
            TitleRow(pg, Layout.ArTitle, "ui.iconPvp", "PvP", "제목(PvP)");
            UiKit.Tag(UiKit.Label(pg, Layout.ArSub.X, Layout.ArSub.Y, Layout.ArSub.W, Layout.ArSub.H, "PvP 티켓은 매일 충전됩니다", TextSize.Body, Palette.Gray).transform, "부제");
            var rect = Layout.ArCard;
            var card = UiKit.Rect(pg, "Card:arena"); UiKit.Pct(card, rect);
            var body = UiKit.Spawn("ui.frameDarkBorder", card); UiKit.Stretch((RectTransform)body.transform);
            var fill = UiKit.Panel(card, "Fill", "fr.r12", CardBody); UiKit.Stretch(fill.rectTransform, 4, 4, 4, 4);
            UiKit.Bordered(card);   // T101 ⓒ — 던전 카드와 같은 직사각형 링
            var head = UiKit.Panel(card, "Head", "fr.r12", ArenaRed); UiKit.Pct(head.rectTransform, Layout.ArCardHead.Within(rect));
            UiKit.Gradient(head.rectTransform, inset: HeadGradientInset);
            UiKit.Label(head.transform, 2.5f, 0, 60, 100, "아레나", CardTitleSize, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            TicketPill(head.transform, new Layout.R(84, 12, 14, 76), "ui.iconTokenRed", "0/5");
            // 경기장 그림 = 모래 들판 + 기둥(석주) + 돌
            var pic = UiKit.Rect(card, "Pic"); UiKit.Pct(pic, Layout.ArCardPic.Within(rect));
            Stage(pic, "env.desert.field", new Color(0.93f, 0.82f, 0.55f), new[] { "env.monolith", "env.stoneBig", "env.monolith", "env.desert.Stone_Gray1_05" });
            var sky = UiKit.Panel(pic, "Sky", "fr.rect", Palette.Hex("#79C8F2")); UiKit.Pct(sky.rectTransform, 0, 0, 100, 42); sky.transform.SetSiblingIndex(1);
            // T69-events: 던전 카드와 같은 그림 띠 테두리(레퍼런스 22 의 경기장 그림도 위·아래가 검은 선으로 끊긴다) — 하늘을 끼운 «뒤»에 걸어 링이 맨 앞에 온다
            UiKit.Bordered(pic);
            var season = UiKit.Rect(card, "Season"); UiKit.Pct(season, Layout.ArSeason.Within(rect));
            UiKit.Label(season, 0, 0, 100, 100, "시즌 종료까지: " + NoTime, TextSize.Aux, Palette.White, TextAnchor.MiddleLeft, kind: TextKind.Aux);
            var enter = UiKit.Button(card, "ui.btnOrange", "입장", () => ShowPage(PageArena), Layout.ArEnter.Within(rect)); enter.name = "EnterBtn"; AlertDot(enter);
            var tier = UiKit.Rect(card, "Tier"); UiKit.Pct(tier, Layout.ArTier.Within(rect));
            var med = UiKit.Icon(tier, "Icon", "ui.iconMedalBronze"); UiKit.Pct(med.rectTransform, 0, 0, 24, 100);
            UiKit.Label(tier, 28, 0, 72, 100, "브론즈", 34, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            UiKit.Tag(card, "아레나 카드"); UiKit.Tag(head.transform, "카드 제목 띠"); UiKit.Tag(pic, "카드 그림"); UiKit.Tag(season, "시즌 타이머"); UiKit.Tag(enter, "입장 버튼"); UiKit.Tag(tier, "티어 줄");
            Foot(pg, PagePvp, () => App.ShowScreen("lobby"));
        }

        // ───────────────────────── ⑬ 아레나 입장 화면 (23) ─────────────────────────
        void BuildArena(RectTransform pg)
        {
            // 시상대 무대 = 어두운 성 안(사각 프레임 조각) + 붉은 카펫 + 양쪽 기둥(어두운 상자) + 횃불 + 갈색 턱
            var stage = UiKit.Panel(pg, "Stage", "fr.rect", Palette.Hex("#262A31")); UiKit.Pct(stage.rectTransform, Layout.AeStage); UiKit.Tag(stage.transform, "시상대 무대");
            {
                var carpet = UiKit.Panel(stage.transform, "Carpet", "fr.rect", Palette.Hex("#5E1220")); UiKit.Pct(carpet.rectTransform, 39, 13, 22, 81);
                var pl = UiKit.Spawn("ui.frameDark", stage.transform); UiKit.Pct((RectTransform)pl.transform, 2, 8, 9, 88);
                var pr = UiKit.Spawn("ui.frameDark", stage.transform); UiKit.Pct((RectTransform)pr.transform, 89, 8, 9, 88);
                var f1 = UiKit.Icon(stage.transform, "Torch", "ui.fire"); UiKit.Pct(f1.rectTransform, 5, 8, 8, 12);
                var f2 = UiKit.Icon(stage.transform, "Torch", "ui.fire"); UiKit.Pct(f2.rectTransform, 87, 8, 8, 12);
                var ledge = UiKit.Panel(stage.transform, "Ledge", "fr.rect", Palette.Hex("#6B4A2E")); UiKit.Pct(ledge.rectTransform, 0, 94, 100, 6);
            }
            // 티어 제목 · 시즌 타이머 · 오른쪽 위 보상/상인
            var tier = UiKit.Rect(pg, "TierTitle"); UiKit.Pct(tier, Layout.AeTier); UiKit.Tag(tier, "티어 제목");
            { var m = UiKit.Icon(tier, "Icon", "ui.iconMedalBronze"); UiKit.Pct(m.rectTransform, 0, 0, 22, 100); UiKit.Label(tier, 26, 0, 74, 100, "브론즈", TextSize.Title, Palette.White, TextAnchor.MiddleLeft, kind: TextKind.Title).fontStyle = FontStyle.Bold; }
            var season = UiKit.Rect(pg, "Season"); UiKit.Pct(season, Layout.AeSeason); UiKit.Tag(season, "시즌 타이머");
            { var c = UiKit.Icon(season, "Icon", "ui.iconClock"); UiKit.Pct(c.rectTransform, 0, 0, 10, 100); UiKit.Label(season, 12, 0, 88, 100, "시즌 종료까지: " + NoTime, TextSize.Aux, Palette.White, TextAnchor.MiddleLeft, kind: TextKind.Aux); }
            var side = UiKit.Rect(pg, "SideIcons"); UiKit.Pct(side, Layout.AeSideIcons); UiKit.Tag(side, "우측 아이콘 열(2개)");
            {
                var a = UiKit.Rect(side, "RewardsBtn"); UiKit.Pct(a, 0, 0, 100, 50); var ai = UiKit.Icon(a, "Icon", "ui.iconGiftBlue"); UiKit.Pct(ai.rectTransform, 15, 0, 70, 54); UiKit.Label(a, 0, 55, 100, 45, "보상", TextSize.Aux, Palette.White, kind: TextKind.Aux); UiKit.Clickable(a, OpenRankRewards);
                var b = UiKit.Rect(side, "MerchantBtn"); UiKit.Pct(b, 0, 50, 100, 50); var bi = UiKit.Icon(b, "Icon", "ui.iconMerchant"); UiKit.Pct(bi.rectTransform, 15, 0, 70, 54); UiKit.Label(b, 0, 55, 100, 45, "상인", TextSize.Aux, Palette.White, kind: TextKind.Aux); UiKit.Clickable(b, () => ShowPage(PageMerchant));
            }
            // 시상대 초상 3(가운데 = 나 · HeroView 가슴 위) + 왕관 번호 · 배너 3 = Social_Ranking 조각(T62) · 맨 위에 «나» 꼬리표
            var podium = UiKit.Rect(pg, "Podium"); UiKit.Stretch(podium);
            var p1 = Portrait(podium, "Portrait:1", Layout.AePortrait1, "ui.itemFrame.yellow", null); _me = HeroView.Attach(UiKit.Find(p1, "Inner") as RectTransform, HeroView.PlayerSkin(App)); _me.SetFraming(1.6f, 0.45f);
            var p2 = Portrait(podium, "Portrait:2", Layout.AePortrait2, "ui.itemFrame.plum", Foes[0]);
            var p3 = Portrait(podium, "Portrait:3", Layout.AePortrait3, "ui.itemFrame.green", Foes[1]);
            Crown(podium, Layout.AePortrait1, "ui.iconCrownGold", "1"); Crown(podium, Layout.AePortrait2, "ui.iconCrownSilver", "2"); Crown(podium, Layout.AePortrait3, "ui.iconCrownBronze", "3");
            UiKit.TagGroup(podium, "시상대 초상(3개)", p1, p2, p3); UiKit.Tag(p1, "1위 초상");
            var proto = RankProto(pg);
            var b1 = Banner(podium, "Banner:1", Layout.AeBanner1, proto, "1st", MeName, 1);
            var b2 = Banner(podium, "Banner:2", Layout.AeBanner2, proto, "2st", FoeName(2), 2);
            var b3 = Banner(podium, "Banner:3", Layout.AeBanner3, proto, "3st", FoeName(3), 3);
            UiKit.Destroy(proto);
            UiKit.TagGroup(podium, "시상대 배너(3개)", b1, b2, b3);
            { var you = UiKit.Panel(podium, "You", "fr.r12", Palette.Hex("#C2223B")); UiKit.Pct(you.rectTransform, 44.2f, 26.2f, 11.4f, 2.3f); UiKit.Label(you.transform, 0, 0, 100, 100, "나", 36, Palette.White, kind: TextKind.Aux); }
            // 순위 목록(4위~ · 세로 스크롤 · 바닥 띠에 가린다)
            var list = ScrollBox(pg, "RankList", Layout.AeList, RankRows * Layout.AeRowPitch, out var content); UiKit.Tag(list, "순위 목록");
            for (int i = 0; i < RankRows; i++)
            {
                var r = Layout.AeRow; r.Y += i * Layout.AeRowPitch;
                var row = Place(content, "RankRow:" + (i + 4), r, Layout.AeList.Y);
                RankItem(row, i + 4, Foes[i % Foes.Length]);
                if (i == 0) UiKit.Tag(row, "순위 줄(1칸)");
            }
            // T124 — 승급 안내 띠는 «불투명» 이다: 레퍼런스 23 도 목록 마지막 줄 위에 걸치지만 띠가 꽉 찬 어두운 막대라 뒤 줄이 안 비친다.
            // α0.85 이던 동안에는 10위 줄의 흰 이름·트로피 숫자가 안내 글자 사이로 비쳐 두 글자가 서로 먹었다(screens 218 실측 · 게이트가 못 재는 종류).
            var promo = UiKit.Panel(pg, "Promo", "fr.rect", Palette.Dim); UiKit.Pct(promo.rectTransform, Layout.AePromo); UiKit.Tag(promo.transform, "승급 안내");
            UiKit.Label(promo.transform, 0, 0, 100, 100, "시즌이 끝나면 상위 순위가 승급합니다", TextSize.Body, Palette.White);
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
            var title = UiKit.Label(pg, Layout.MeTitle.X, Layout.MeTitle.Y, Layout.MeTitle.W, Layout.MeTitle.H, "상인", TextSize.Title, Palette.White, kind: TextKind.Title); title.fontStyle = FontStyle.Bold; title.gameObject.name = "Title"; UiKit.Tag(title.transform, "제목(Merchant)");
            var season = UiKit.Rect(pg, "Season"); UiKit.Pct(season, Layout.MeSeason); UiKit.Tag(season, "시즌 타이머");
            { var c = UiKit.Icon(season, "Icon", "ui.iconClock"); UiKit.Pct(c.rectTransform, 0, 0, 8, 100); UiKit.Label(season, 10, 0, 90, 100, "시즌 종료까지: " + NoTime, TextSize.Aux, Palette.White, TextAnchor.MiddleLeft, kind: TextKind.Aux); }
            int rows = (Goods.Length + 2) / 3;
            var grid = ScrollBox(pg, "Goods", Layout.MeGrid, (rows - 1) * Layout.MeRowPitch + Layout.MeCard.H + 1.5f, out var content); UiKit.Tag(grid, "상품 격자");
            // T72 4항 — 상품 11칸의 빛살은 «보이는 칸만» 돈다(스크롤할 때마다 다시 고른다)
            _goodsScroll = grid.GetComponent<ScrollRect>();
            if (_goodsScroll != null) _goodsScroll.onValueChanged.AddListener(_ => UpdateGoodsSpin());
            for (int i = 0; i < Goods.Length; i++)
            {
                var g = Goods[i]; var r = Layout.MeCard; r.X += (i % 3) * Layout.MeColPitch; r.Y += (i / 3) * Layout.MeRowPitch;
                var card = Place(content, "Goods:" + i, r, Layout.MeGrid.Y);
                UiKit.Ensure<RectMask2D>(card.gameObject);   // CardFrame_04 는 964px 폭 원본이라 고정 폭 자식(제목 띠)이 29% 카드 밖으로 삐져나온다 — 옆 카드가 없는 마지막 칸에서 조각이 보였다(T43 비평 회차 1 · 26 감점 원인)
                var frame = UiKit.Spawn("ui.cardFrame.blue", card); UiKit.Stretch((RectTransform)frame.transform);
                // 제목 = 프리팹의 Text_Title 자리(원본 CardFrame_04_BasePrefab_LightBg 의 «Text» 글자 · ShopScreen 상자 카드와 같은 식) — 따로 Label 을 얹으면 «Text» 가 활성으로 남아 T50(CI #71·#75 «[상인 페이지] 영문 데모 글자: Text»)
                var gt = UiKit.SetText(frame.transform, "Text_Title", g.title, Palette.White, TextSize.Body);
                if (gt != null) { UiKit.Pct(gt.rectTransform, 4, 2, 92, 15); gt.alignment = TextAnchor.MiddleCenter; gt.fontStyle = FontStyle.Bold; gt.resizeTextForBestFit = true; gt.resizeTextMinSize = TextSize.BestFitMin; gt.resizeTextMaxSize = TextSize.Body; }
                else UiKit.Label(card, 4, 2, 92, 15, g.title, TextSize.Body, Palette.White).fontStyle = FontStyle.Bold;
                var ic = UiKit.Rect(card, "IconCell"); UiKit.Pct(ic, 26, 20, 48, 38);
                var f = UiKit.Spawn("ui.itemFrame.blue", ic); UiKit.Stretch((RectTransform)f.transform); var im = UiKit.Icon(ic, "Icon", g.icon); UiKit.Pct(im.rectTransform, 15, 15, 70, 70);
                GearUi.DarkFrame(f.transform);   // T115 — 워커 I 가 «우연히 통과» 로 지목한 자리(f08a7fe 커밋 메시지)
                // T72 ② 상품 아이콘 뒤 빛살(주인 «상점 아이템 … 아이콘 뒤에 Effect_Light» · 상인 페이지도 상점이다)
                PlanLight(ic); _goodsCells.Add(ic);
                UiKit.Label(card, 4, 60, 92, 15, "한도 —", TextSize.Aux, Palette.Ink, kind: TextKind.Aux);
                var price = UiKit.Panel(card, "Price", "fr.r12", Palette.Cream); UiKit.Pct(price.rectTransform, 5, 79, 90, 17);
                var coin = UiKit.Icon(price.transform, "Icon", "ui.iconArenaCoin"); UiKit.Pct(coin.rectTransform, 8, 12, 22, 76); UiKit.Label(price.transform, 32, 0, 62, 100, "—", TextSize.Body, Palette.Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                UiKit.Clickable(card, Noop);
                if (i == 0) UiKit.Tag(card, "상품 카드(1칸)");
            }
            Foot(pg, null, () => ShowPage(PageArena));
        }

        // ───────────────────────── ⑪ 던전 세부 팝업 (21) ─────────────────────────
        void OpenDungeonDetail(string key)
        {
            var d = Dungeons[0]; foreach (var x in Dungeons) if (x.key == key) d = x;
            RollTickets();
            var box = App.Overlay.OpenBox("ui.popup.red", "ui.title.red", d.title, Layout.DdBox, () => App.Overlay.Close());
            FlatHead(box, Layout.DdBox, Layout.DdHead, DeepRed, d.title);
            // T102 ⓐ(주인 «DecoLine 이거 빨간색인 거 없애 줘야 함») — 조각이 달고 오는 장식 선을 이 팝업에서만 끈다(조각 원본은 안 고친다 · 상점 섹션 헤더의 LineDeco 는 다른 것이라 그대로)
            HideDeco(box);
            var pic = UiKit.Rect(box, "Pic"); UiKit.Pct(pic, Layout.DdPic.Within(Layout.DdBox)); Stage(pic, d.field, d.tint, d.props); UiKit.Tag(pic, "그림 띠");
            var note = UiKit.Panel(box, "Note", "fr.r12", Palette.A(Palette.Hex("#3A1216"), 0.92f)); UiKit.Pct(note.rectTransform, Layout.DdNote.Within(Layout.DdBox)); UiKit.Tag(note.transform, "조건 문구");
            UiKit.Label(note.transform, 2, 0, 96, 100, "전설·신화 특전만 등장", TextSize.Body, Palette.Red);
            // 층수 ◀ 색 = Gray: 크림 패널 위라 Cream 이면 안 보인다(T43 비평 회차 1 · 21 감점 원인) · 레퍼런스도 회색 화살표. ⚠ 한 줄에 문장 4개 — 뒤에 // 주석을 붙이면 Pct·Clickable·Tag 가 주석 처리된다(CI #87 회귀)
            var arrow = UiKit.Icon(box, "FloorPrev", "pi.arrow_left", Palette.Gray); UiKit.Pct(arrow.rectTransform, Layout.DdArrow.Within(Layout.DdBox)); UiKit.Clickable(arrow.transform, Noop); UiKit.Tag(arrow.transform, "층수 화살표");
            var circle = UiKit.Panel(box, "FloorCircle", "fr.circle", Palette.Hex("#141414")); UiKit.Pct(circle.rectTransform, Layout.DdFloor.Within(Layout.DdBox)); UiKit.Tag(circle.transform, "층수 원");
            UiKit.Label(circle.transform, 0, 8, 100, 56, "1", 56, Palette.Orange).fontStyle = FontStyle.Bold; UiKit.Label(circle.transform, 0, 62, 100, 32, "층", TextSize.Aux, Palette.Orange, kind: TextKind.Aux);
            var rewards = UiKit.Spawn("ui.frameDark", box); var rrt = (RectTransform)rewards.transform; rrt.name = "Rewards"; UiKit.Pct(rrt, Layout.DdRewards.Within(Layout.DdBox)); UiKit.Tag(rrt, "보상 박스");
            UiKit.Label(rrt, 0, 3, 100, 24, "보상", TextSize.Body, Palette.White).fontStyle = FontStyle.Bold;
            var cells = UiKit.Rect(box, "RewardCells"); UiKit.Pct(cells, Layout.DdRewardCells.Within(Layout.DdBox));
            // T99 4항 — 보상 칸은 표(dungeon.json)가 만든다: «첫 클리어 총액» 칸들(빨간 «최초» 배지 · T123) + «이후 클리어» 칸들.
            // 지옥의 문 = 펫알 11 · 골드 1,000(첫) + 펫알 5 · 골드 1,000 = 네 칸이라 레퍼런스 21(초록 프레임 4 · 앞 두 칸에 FIRST 배지)과 같은 꼴이고 표 ⑪ 도 그대로다.
            var rewardDefs = RewardCells(key, d.rewards);
            var cellRts = IconRow(cells, Layout.DdRewardCells, Icons(rewardDefs), "ui.itemFrame.green", "RewardCell:", true);
            for (int i = 0; i < cellRts.Count && i < rewardDefs.Count; i++)
            {
                UiKit.Label(cellRts[i], 0, 58, 100, 42, rewardDefs[i].amount, TextSize.Aux, Palette.White, kind: TextKind.Aux).fontStyle = FontStyle.Bold;
                // T123 — «최초» 배지는 레퍼런스 21 처럼 «칸 안 오른쪽 위»(칸 폭 안 · 위로만 살짝 걸침)다.
                // 전에는 «첫 클리어» 다섯 글자가 보조 36 으로 칸 폭 119px 에 안 들어가 배지를 114%(136px)로 넓혔고(T74),
                // 그 바람에 배지가 좌우로 삐져나와 옆 칸 배지와 붙고 위로 34%(40px)나 솟아 «보상» 제목을 덮었다(screens 218 실측).
                // 글자를 줄이는 쪽으로 고친다 — 크기 36(T63 보조 하한)은 그대로 두고 낱말만 «최초»(레퍼런스 «FIRST» 와 같은 뜻·같은 자리)로.
                if (!rewardDefs[i].first) continue;
                var badge = UiKit.Panel(cellRts[i], "First", "fr.r12", Palette.Red); UiKit.Pct(badge.rectTransform, FirstBadge);
                UiKit.Label(badge.transform, 0, 0, 100, 100, "최초", TextSize.Aux, Palette.White, kind: TextKind.Aux);
            }
            UiKit.TagGroup(box, "보상 칸(" + cellRts.Count + "개)", cellRts.ToArray());
            foreach (var cell in cellRts) PlanLight(cell);
            var ticket = UiKit.Rect(box, "Ticket"); UiKit.Pct(ticket, Layout.DdTicket.Within(Layout.DdBox)); UiKit.Tag(ticket, "티켓 줄");
            { var ti = UiKit.Icon(ticket, "Icon", d.ticket); UiKit.Pct(ti.rectTransform, 10, 0, 34, 100); UiKit.Label(ticket, 50, 0, 50, 100, Dun == null ? "--" : Tickets(key).ToString(), TextSize.Body, Palette.Ink, TextAnchor.MiddleLeft); }
            var bt = Layout.DdBtns; float half = bt.W * 0.485f;
            var leftRect = new Layout.R(bt.X, bt.Y, half, bt.H).Within(Layout.DdBox);
            var rightRect = new Layout.R(bt.X + bt.W - half, bt.Y, half, bt.H).Within(Layout.DdBox);
            RectTransform sweep, chal;
            if (Dun == null || Tickets(key) > 0)
            {
                // 티켓이 있으면 레퍼런스 21 그대로 — 소탕(파랑 · 클리어한 층만)·도전(주황) · 아직 껍데기라 눌러도 아무 일 없다
                sweep = UiKit.Button(box, "ui.btnBlue", "소탕", Noop, leftRect); sweep.name = "SweepBtn"; TicketCost(sweep, d.ticket);
                chal = UiKit.Button(box, "ui.btnOrange", "도전", Noop, rightRect); chal.name = "ChallengeBtn"; TicketCost(chal, d.ticket);
            }
            else
            {
                // T99 3항 — 티켓이 0 이면 두 버튼이 «티켓 얻기» 로 바뀐다(왼쪽 = 광고 · 오른쪽 = 다이아). 하루치를 다 썼거나 다이아가 모자라면
                // 꺼져 보이게(알파 0.5) 두되 클릭은 살려 이유를 토스트로 알린다(주인 «비활성(이유 토스트)» · 워커 결정 기록).
                string dungeonKey = key;
                sweep = UiKit.Button(box, "ui.btnBlue", "광고 보고 티켓 1개", () => AdTicket(dungeonKey), leftRect); sweep.name = "SweepBtn";
                Dim(sweep, DungeonTickets.CanAd(App.Save, Dun, key, Today()));
                chal = UiKit.Button(box, "ui.btnOrange", "다이아 " + UiKit.FmtQty(Dun.GemCost) + " 으로 티켓 사기", () => BuyTicket(dungeonKey), rightRect); chal.name = "ChallengeBtn";
                Dim(chal, DungeonTickets.CanBuyGem(App.Save, Dun, key, Today()));
            }
            UiKit.TagGroup(box, "버튼 2개", sweep, chal);
            ApplyLights();
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
                var ti = UiKit.Icon(pill.transform, "Icon", "ui.iconTokenRed"); UiKit.Pct(ti.rectTransform, 2, 5, 24, 90); UiKit.Label(pill.transform, 30, 0, 66, 100, "0", TextSize.Body, Palette.White);
                var pw = UiKit.Icon(info, "PowerIcon", "ui.battle"); UiKit.Pct(pw.rectTransform, 70, -30, 8, 160);
                var pt = UiKit.Label(info, 79, -30, 21, 160, "0", TextSize.Body, Palette.Orange, TextAnchor.MiddleLeft); pt.fontStyle = FontStyle.Bold; _powerTexts.Add(pt); pt.text = UiKit.FmtComma(App.Power());
            }
            var list = UiKit.Rect(box, "FoeList"); UiKit.Pct(list, Layout.AcList.Within(Layout.AcBox)); UiKit.Tag(list, "상대 목록(5줄)");
            for (int i = 0; i < FoeRows; i++)
            {
                var r = Layout.AcRow; r.Y += i * Layout.AcRowPitch;
                var row = UiKit.Rect(box, "FoeRow:" + i); UiKit.Pct(row, r.Within(Layout.AcBox));
                var fr = UiKit.Spawn("ui.frameDark", row); UiKit.Stretch((RectTransform)fr.transform);
                // T69-events: 줄 자체에 Ink 링(레퍼런스 24 는 상대 5줄이 각자 검은 외곽선 상자다) — 조각 «ui.frameDark» 는 이름 그대로 NoBorder 라 링이 없었다.
                // 게이트는 줄 안 초상 프레임(ItemFrame)의 링 때문에 이미 통과했지만 눈에는 줄 테두리가 없었다 — 결정 184 와 같은 함정이라 줄에 직접 건다.
                UiKit.Bordered(row);
                Portrait(row, "Face", new Layout.R(2.5f, 12, 11.5f, 76), "ui.itemFrame.yellow", Foes[i % Foes.Length], true);
                UiKit.Label(row, 16, 6, 44, 42, FoeName(FoeRank(i)), TextSize.Body, Palette.White, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                _dummyPowerTexts.Add(new KeyValuePair<Text, int>(Pill(row, new Layout.R(16, 54, 19, 38), "ui.battle", DummyPower(FoeRank(i)), Palette.Orange), FoeRank(i)));
                Pill(row, new Layout.R(37, 54, 19, 38), "ui.trophy", DummyScore(FoeRank(i)), Palette.Yellow);
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
            // T72 ① 붉은 티어 띠 안에도 무늬(레퍼런스 25 의 띠는 트로피 무늬가 반복된다) — 띠는 RectMask2D 라 무늬가 밖으로 안 샌다
            UiKit.PatternBg(band.rectTransform, UiKit.PatternTintDark, UiKit.PatternTileSeconds);
            for (int i = 0; i < Tiers.Length; i++)
            {
                var cell = UiKit.Rect(band.transform, "Tier:" + i); UiKit.Pct(cell, i * 20.5f, 0, 20, 100);
                var ic = UiKit.Icon(cell, "Icon", Tiers[i].icon); UiKit.Pct(ic.rectTransform, 22, 10, 56, 54); UiKit.Label(cell, 0, 66, 100, 30, Tiers[i].label, TextSize.Body, Palette.White).fontStyle = FontStyle.Bold;
                if (i > 0) { var dash = UiKit.Panel(band.transform, "Dash", "fr.rect", Palette.Hex("#5A1520")); UiKit.Pct(dash.rectTransform, i * 20.5f - 2.2f, 44, 1.6f, 6); }
            }
            var timer = UiKit.Rect(box, "Timer"); UiKit.Pct(timer, Layout.RrTimer.Within(Layout.RrBox)); UiKit.Tag(timer, "리셋 타이머");
            { var c = UiKit.Icon(timer, "Icon", "ui.iconClock"); UiKit.Pct(c.rectTransform, 0, 0, 10, 100); UiKit.Label(timer, 12, 0, 88, 100, "초기화까지: " + NoTime, TextSize.Body, Palette.Ink, TextAnchor.MiddleLeft); }
            var rn = Layout.RrNote.Within(Layout.RrBox);
            UiKit.Tag(UiKit.Label(box, rn.X, rn.Y, rn.W, rn.H, "순위 보상은 우편으로 지급됩니다", TextSize.Aux, Palette.Ink, kind: TextKind.Aux).transform, "안내 문구");
            var list = UiKit.Rect(box, "RewardList"); UiKit.Pct(list, Layout.RrList.Within(Layout.RrBox)); UiKit.Tag(list, "보상 목록(4줄)");
            string[] crowns = { "ui.iconCrownGold", "ui.iconCrownSilver", "ui.iconCrownBronze" };
            for (int i = 0; i < RewardRows; i++)
            {
                var r = Layout.RrRow; r.Y += i * Layout.RrRowPitch;
                var row = UiKit.Rect(box, "RewardRow:" + i); UiKit.Pct(row, r.Within(Layout.RrBox));
                var fr = UiKit.Spawn("ui.frameDark", row); UiKit.Stretch((RectTransform)fr.transform);
                // T69-events: 24 의 상대 줄과 같은 이유로 보상 줄에도 Ink 링(레퍼런스 25 의 4줄은 각자 검은 외곽선 상자)
                UiKit.Bordered(row);
                if (i < crowns.Length) { var cr = UiKit.Icon(row, "Crown", crowns[i]); UiKit.Pct(cr.rectTransform, 2, 8, 14, 84); UiKit.Label(row, 2, 30, 14, 50, (i + 1).ToString(), TextSize.Body, Palette.White).fontStyle = FontStyle.Bold; }
                else UiKit.Label(row, 2, 0, 14, 100, (i + 1).ToString(), TextSize.Body, Palette.White).fontStyle = FontStyle.Bold;
                // T72 ② 보상 칸(코인·다이아) 아이콘 뒤 빛살 — 팝업이라 스크롤 제한 없이 여덟 칸이 같이 돈다(닫으면 SetLink 로 같이 죽는다)
                PlanLight(RewardCell(row, new Layout.R(20, 8, 13, 84), "ui.itemFrame.green", "ui.iconArenaCoin")); PlanLight(RewardCell(row, new Layout.R(35, 8, 13, 84), "ui.itemFrame.plum", "ui.iconGemPurple"));
                if (i == 0) UiKit.Tag(row, "보상 줄(1칸)");
            }
            var tabs = UiKit.Rect(box, "Tabs"); UiKit.Pct(tabs, Layout.RrTabs.Within(Layout.RrBox)); UiKit.Tag(tabs, "하단 탭(2개)");
            // T127 — 버튼은 탭 줄(표 ⑰ 의 «하단 탭») 안에서 위쪽 TabBtnH% 만 쓴다: 줄의 밑변은 팝업 박스 rect 의 밑변과 같은데
            // 팝업 조각의 «보이는» 크림 바닥은 그보다 위라, 줄을 꽉 채우면 버튼 아래쪽이 크림 밖(검은 배경) 으로 삐져나온다(screens 243 실측).
            // 레퍼런스 25 도 버튼 아래에 여백이 있고 버튼 높이가 줄보다 낮다 — 배치 표는 «줄» 을 잰 값이라 한 칸도 안 바꾼다.
            var daily = UiKit.Button(tabs, "ui.btnGray", "일일 보상", Noop, new Layout.R(0, 0, 48.5f, TabBtnH)); daily.name = "DailyTab";
            var seasonTab = UiKit.Button(tabs, "ui.btnGray", "시즌 보상", Noop, new Layout.R(51.5f, 0, 48.5f, TabBtnH)); seasonTab.name = "SeasonTab"; UiKit.Ensure<CanvasGroup>(seasonTab.gameObject).alpha = 0.7f;
            ApplyLights();
            TagClose();
        }

        // ───────────────────────── 던전 티켓(T99 · 주인 2026-09-07) ─────────────────────────
        /// <summary>티켓 표(<c>dungeon.json</c>) — 없으면 null(티켓 «--» · 보충·구매 없음).</summary>
        DungeonData Dun => App != null && App.Data != null ? App.Data.Dungeon : null;
        /// <summary>«오늘»(출석·데일리 기프트와 같은 날짜 규칙).</summary>
        static string Today() => SaveStore.Today();
        /// <summary>하루가 바뀌었으면 던전마다 티켓을 보충하고(2 미만 → 2) 그날의 광고·다이아 횟수를 되돌린다 — 바뀌었으면 저장.</summary>
        void RollTickets() { var d = Dun; if (d == null) return; if (DungeonTickets.Roll(App.Save, d, Today())) App.Persist(); }
        int Tickets(string key) { var d = Dun; return d == null ? 0 : DungeonTickets.Tickets(App.Save, d, key, Today()); }
        /// <summary>카드 제목 띠의 «보유/하루 보충»(표가 없으면 «--»).</summary>
        string TicketText(string key) { var d = Dun; return d == null ? "--" : Tickets(key) + "/" + d.DailyRefill; }

        /// <summary>보상 칸 한 개 — 아이콘 키 · 수량 글자 · «최초»(첫 클리어) 배지인가.</summary>
        readonly struct RewardCellDef
        {
            public readonly string icon, amount; public readonly bool first;
            public RewardCellDef(string icon, string amount, bool first) { this.icon = icon; this.amount = amount; this.first = first; }
        }
        /// <summary>
        /// 던전 세부(21)의 보상 칸 목록을 표(<c>dungeon.json</c>)에서 만든다 — «첫 클리어 총액» 칸들(배지) 다음에 «이후 클리어» 칸들.
        /// 표가 없으면 옛 껍데기 그대로(카드의 아이콘 목록을 네 칸으로 채운다 · 수량 글자 없음).
        /// </summary>
        List<RewardCellDef> RewardCells(string key, string[] fallbackIcons)
        {
            var list = new List<RewardCellDef>();
            var e = Dun != null ? Dun.Of(key) : null;
            if (e == null)
            {
                var icons = new List<string>(fallbackIcons);
                while (icons.Count < 4) icons.Add(icons.Count == 3 ? "ui.coin" : "ui.bookBlue");
                for (int i = 0; i < icons.Count; i++) list.Add(new RewardCellDef(icons[i], "", i < 2));
                return list;
            }
            Add(list, e.First, true); Add(list, e.Clear, false);
            return list;
        }
        static void Add(List<RewardCellDef> list, DungeonData.Reward r, bool first)
        {
            if (r == null) return;
            if (r.PetEgg > 0) list.Add(new RewardCellDef("pet.egg", UiKit.FmtComma(r.PetEgg), first));
            if (r.Gold > 0) list.Add(new RewardCellDef("ui.coin", UiKit.FmtComma(r.Gold), first));
        }
        static string[] Icons(List<RewardCellDef> cells)
        {
            var a = new string[cells.Count];
            for (int i = 0; i < cells.Count; i++) a[i] = cells[i].icon;
            return a;
        }
        /// <summary>«광고 보고 티켓» — 모의 광고(T23·T77 과 같은 <see cref="Overlay.AdCountdown"/>) 뒤 티켓 +1 · 팝업을 다시 연다.</summary>
        void AdTicket(string key)
        {
            var d = Dun; if (d == null) return;
            if (!DungeonTickets.CanAd(App.Save, d, key, Today())) { App.Toast("오늘 광고 티켓은 이미 받았습니다"); return; }
            App.Overlay.AdCountdown(TicketAdSeconds, () =>
            {
                if (DungeonTickets.ClaimAd(App.Save, d, key, Today())) { App.Persist(); App.Toast("티켓 1개를 받았습니다"); }
                App.Current?.Refresh();
                OpenDungeonDetail(key);
            });
        }
        /// <summary>«다이아로 티켓» — 하루치가 남고 다이아가 충분할 때만 · 산 뒤 팝업을 다시 연다.</summary>
        void BuyTicket(string key)
        {
            var d = Dun; if (d == null) return;
            if (!DungeonTickets.GemLeft(App.Save, d, key, Today())) { App.Toast("오늘 다이아 티켓은 이미 샀습니다"); return; }
            if (App.Save.Gem < d.GemCost) { App.Toast("다이아가 모자랍니다"); return; }
            if (DungeonTickets.BuyGem(App.Save, d, key, Today())) { App.Persist(); App.Toast("티켓 1개를 샀습니다"); }
            App.Current?.Refresh();
            OpenDungeonDetail(key);
        }
        /// <summary>«눌리기는 하되 꺼져 보이는» 버튼(알파 0.5) — 이유를 토스트로 알려야 해서 <see cref="UiKit.SetInteractable"/>(클릭까지 막는다) 대신 쓴다(워커 결정 기록).</summary>
        static void Dim(RectTransform btn, bool on)
        {
            if (btn == null) return;
            UiKit.Ensure<CanvasGroup>(btn.gameObject).alpha = on ? 1f : 0.5f;
        }
        /// <summary>모의 광고 카운트다운 길이(초) — 데일리 기프트(<see cref="LobbyPopups.GiftAdSeconds"/>)와 같은 값.</summary>
        const int TicketAdSeconds = 3;

        // ───────────────────────── 조립 도우미 ─────────────────────────
        static void Noop() { }

        /// <summary>T72 ② 빛살 예약 — 칸 안 «Icon» 뒤에 걸 자리를 적어 둔다(실제로 거는 것은 <see cref="ApplyLights"/> · 결정 174).</summary>
        void PlanLight(RectTransform cell, string key = UiKit.LightKeySmall)
        {
            if (cell == null) return;
            _lightPlan.Add((cell, cell.Find("Icon") as RectTransform, key));
        }
        /// <summary>예약해 둔 빛살을 «배치가 끝난 뒤»에 한꺼번에 건다 — 그 전에는 % 앵커 아이콘의 rect 가 0 이라 빛살 한 변이 0 이 된다(결정 174).</summary>
        /// <summary>«아이콘 + 글자» 를 한 덩어리로 줄 가운데에 놓는다(T101 ⓓ) — 글자 폭은 <see cref="Text.preferredWidth"/> 실측이라 «던전»·«PvP»·«상인» 이 각자 가운데다.</summary>
        static void CenterTitle(RectTransform icon, Text text, float rowWPct)
        {
            if (icon == null || text == null) return;
            float rowPx = Mathf.Max(1f, rowWPct / 100f * UiKit.FrameW);
            float textPct = Mathf.Clamp(text.preferredWidth / rowPx * 100f, 5f, 100f - TitleIconPct - TitleGapPct);
            float startPct = Mathf.Max(0f, (100f - (TitleIconPct + TitleGapPct + textPct)) * 0.5f);
            UiKit.Pct(icon, startPct, -10, TitleIconPct, 120);
            UiKit.Pct(text.rectTransform, startPct + TitleIconPct + TitleGapPct, 0, textPct, 100);
        }

        void ApplyLights()
        {
            if (_titlePlan.Count > 0)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var t in _titlePlan) if (t.text != null) CenterTitle(t.icon, t.text, t.rowWPct);
                _titlePlan.Clear();
            }
            if (_lightPlan.Count == 0) return;
            Canvas.ForceUpdateCanvases();
            foreach (var l in _lightPlan) UiKit.LightBehind(l.host, l.icon, l.key);
            _lightPlan.Clear();
        }
        /// <summary>T72 4항 «보이는 칸만» — 상인 페이지(26) 상품 격자에서 스크롤 창과 세로로 겹치는 칸의 빛살만 돌린다(칸 11개).</summary>
        void UpdateGoodsSpin()
        {
            if (_goodsScroll == null || _goodsScroll.viewport == null) return;
            var view = _goodsScroll.viewport; view.GetWorldCorners(_corners);
            float vBottom = _corners[0].y, vTop = _corners[1].y;
            foreach (var cell in _goodsCells)
            {
                if (cell == null) continue;
                cell.GetWorldCorners(_corners);
                UiKit.SetLightSpinning(cell, _corners[1].y > vBottom && _corners[0].y < vTop);
            }
        }
        static Layout.R Shift(Layout.R r, float dy) { r.Y += dy; return r; }

        /// <summary>제목 줄 = 아이콘 + 굵은 글자(가운데) + 밑줄(프리팹 Title_LineDeco 의 선 조각 · 가운데 ◇).</summary>
        void TitleRow(RectTransform pg, Layout.R rect, string icon, string text, string tag)
        {
            var row = UiKit.Rect(pg, "Title"); UiKit.Pct(row, rect); UiKit.Tag(row, tag);
            var ic = UiKit.Icon(row, "Icon", icon);
            var title = UiKit.Label(row, 0, 0, 100, 100, text, TextSize.Title, Palette.White, TextAnchor.MiddleLeft, kind: TextKind.Title); title.fontStyle = FontStyle.Bold;
            // T101 ⓓ(주인 2026-09-07 «상단 «던전» 타이틀이 왼쪽으로 치우쳐 있음 · 아이콘은 지울 필요 없고 걍 중앙에») —
            // 아이콘을 rect 왼쪽 끝에 못 박고 글자를 26% 부터 왼쪽 정렬하던 것을 «아이콘 + 글자» 한 덩어리로 묶어 가운데에 놓는다.
            // 글자 폭은 실측(preferredWidth)이라 «던전»·«PvP»·«상인» 길이가 달라도 각자 가운데다.
            CenterTitle(ic.rectTransform, title, rect.W);
            _titlePlan.Add((row, ic.rectTransform, title, rect.W));   // 글자 폭 실측은 배치가 끝난 뒤 한 번 더(ApplyLights · 결정 174 와 같은 이유)
            var line = UiKit.Spawn("ui.lineTitle", pg); var lrt = (RectTransform)line.transform; lrt.name = "TitleLine";
            var t = UiKit.Find(lrt, "Text (TMP)"); if (t != null) t.gameObject.SetActive(false);
            var deco = UiKit.Find(lrt, "LineDeco") as RectTransform;
            if (deco != null) { deco.SetParent(pg, false); UiKit.Pct(deco, Layout.DgTitleLine); deco.name = "TitleLine"; UiKit.Tag(deco, "제목 밑줄"); foreach (var g in deco.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false; line.SetActive(false); }
            else { UiKit.Pct(lrt, Layout.DgTitleLine); UiKit.Tag(lrt, "제목 밑줄"); }
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
                // T69-events: 하단 탭 2칸도 «칸» — 레퍼런스 20·22 의 던전/PvP 탭은 각자 검은 외곽선 상자다(아이콘·라벨은 뒤에 얹혀 링 위)
                UiKit.Bordered(bg.rectTransform);
                var ic = UiKit.Icon(cell, "Icon", it.icon); UiKit.Pct(ic.rectTransform, 22, on ? 6 : 14, 56, on ? 56 : 72);
                if (on) UiKit.Label(cell, 0, 62, 100, 34, it.label, TextSize.Aux, Palette.White, kind: TextKind.Aux).fontStyle = FontStyle.Bold;
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
        static Text TicketPill(Transform head, Layout.R r, string icon, string text)
        {
            var cell = UiKit.Rect(head, "Ticket"); UiKit.Pct(cell, r);
            var ic = UiKit.Icon(cell, "Icon", icon); UiKit.Pct(ic.rectTransform, 0, 0, 34, 100);
            var t = UiKit.Label(cell, 38, 0, 62, 100, text, TextSize.Body, Palette.White, TextAnchor.MiddleLeft); t.fontStyle = FontStyle.Bold;
            return t;
        }
        /// <summary>정사각 아이콘 칸 줄(프레임 조각 + 아이콘) — 칸 한 변 = 줄 높이 · 왼쪽부터 · 간격은 남는 폭을 등분. rowRect = 줄의 프레임 % 사각형(정사각 환산용).</summary>
        static List<RectTransform> IconRow(RectTransform row, Layout.R rowRect, string[] icons, string frameKey, string namePrefix = "Cell:", bool amountBelow = false)
        {
            var res = new List<RectTransform>();
            float rowW = Mathf.Max(1e-3f, rowRect.W / 100f * UiKit.FrameW), rowH = Mathf.Max(1e-3f, rowRect.H / 100f * UiKit.FrameH);
            float cellW = Mathf.Min(100f, rowH / rowW * 100f);   // 줄 높이(px)를 줄 폭 % 로 — 정사각 칸
            int n = icons.Length; float gap = n > 1 ? Mathf.Max(0, (100f - n * cellW) / (n - 1)) : 0;
            for (int i = 0; i < n; i++)
            {
                var cell = UiKit.Rect(row, namePrefix + i); UiKit.Pct(cell, i * (cellW + gap), 0, cellW, 100);
                var f = UiKit.Spawn(frameKey, cell); UiKit.Stretch((RectTransform)f.transform);
                GearUi.DarkFrame(f.transform);   // T115 · T69 7항 — 조각 제 Border 링을 Ink 8px 로 + 결정 184 계약(가운데 비움 · raycast 끔 · 링이 형제 맨 뒤)
                // 수량 글자가 아래 34% 를 쓰는 칸(던전 세부 보상 · T99)은 아이콘을 위로 올려 겹치지 않게 한다 — 레퍼런스 21 도 «그림 위 · 숫자 아래» 다
                var ic = UiKit.Icon(cell, "Icon", icons[i]); UiKit.Pct(ic.rectTransform, amountBelow ? 22 : 16, amountBelow ? 4 : 16, amountBelow ? 56 : 68, amountBelow ? 56 : 68);
                res.Add(cell);
            }
            return res;
        }
        static RectTransform RewardCell(RectTransform row, Layout.R r, string frameKey, string icon)
        {
            var cell = UiKit.Rect(row, "Reward"); UiKit.Pct(cell, r);
            var f = UiKit.Spawn(frameKey, cell); UiKit.Stretch((RectTransform)f.transform);
            GearUi.DarkFrame(f.transform);   // T115
            var ic = UiKit.Icon(cell, "Icon", icon); UiKit.Pct(ic.rectTransform, 16, 12, 68, 68);
            UiKit.Label(cell, 0, 50, 100, 50, "—", TextSize.Aux, Palette.White, kind: TextKind.Aux);
            return cell;
        }
        /// <summary>버튼 오른쪽 위 빨간 알림 점(GUI Pro 조각).</summary>
        static GameObject AlertDot(RectTransform btn)
        {
            var d = UiKit.Spawn("ui.alertDot", btn); var dr = (RectTransform)d.transform; d.name = "Dot";
            dr.anchorMin = dr.anchorMax = new Vector2(1, 1); dr.pivot = new Vector2(0.5f, 0.5f); dr.anchoredPosition = new Vector2(-4, 4); dr.sizeDelta = new Vector2(52, 52);
            return d;
        }
        /// <summary>버튼 글자 아래 «🎫 x1» 줄(아이콘 + 글자) — 글자를 위로 올리고 아래에 작은 줄.</summary>
        static void TicketCost(RectTransform btn, string icon)
        {
            var t = UiKit.ButtonText(btn); if (t != null) { var trt = t.rectTransform; trt.anchorMin = new Vector2(0, 0.42f); trt.anchorMax = new Vector2(1, 1); trt.offsetMin = trt.offsetMax = Vector2.zero; }
            var cost = UiKit.Rect(btn, "Cost"); UiKit.Pct(cost, 28, 56, 44, 44);
            var ic = UiKit.Icon(cost, "Icon", icon); UiKit.Pct(ic.rectTransform, 0, 0, 40, 100); UiKit.Label(cost, 44, 0, 56, 100, "x1", TextSize.Aux, Palette.White, TextAnchor.MiddleLeft, kind: TextKind.Aux).fontStyle = FontStyle.Bold;
        }
        /// <summary>초상 칸 = 프레임 조각 + (아이콘 | HeroView 자리 «Inner»).</summary>
        static RectTransform Portrait(RectTransform parent, string name, Layout.R r, string frameKey, string icon, bool aspect = false)
        {
            var cell = UiKit.Rect(parent, name); UiKit.Pct(cell, r);
            // 정사각 맞춤은 HeightControlsWidth — FitInParent 는 앵커를 부모(줄) 전체로 펴고 가운데 정렬해 초상이 줄 한가운데로 갔다(T43 비평 회차 1 · 23·24 감점 원인)
            if (aspect) { var arf = UiKit.Ensure<AspectRatioFitter>(cell.gameObject); arf.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth; arf.aspectRatio = 1f; }
            var f = UiKit.Spawn(frameKey, cell); UiKit.Stretch((RectTransform)f.transform);
            GearUi.DarkFrame(f.transform);   // T115
            var inner = UiKit.Rect(cell, "Inner"); UiKit.Pct(inner, 10, 10, 80, 80); UiKit.Ensure<RectMask2D>(inner.gameObject);
            if (icon != null) { var ic = UiKit.Icon(inner, "Icon", icon); UiKit.Stretch(ic.rectTransform); }
            return cell;
        }
        static void Crown(RectTransform parent, Layout.R portrait, string icon, string num)
        {
            var c = UiKit.Rect(parent, "Crown:" + num); UiKit.Pct(c, portrait.X + portrait.W * 0.25f, portrait.Y - 3.0f, portrait.W * 0.5f, 2.8f);
            var ic = UiKit.Icon(c, "Icon", icon); UiKit.Stretch(ic.rectTransform); UiKit.Label(c, 0, 20, 100, 80, num, TextSize.Aux, Palette.White, kind: TextKind.Aux).fontStyle = FontStyle.Bold;
        }
        /// <summary>
        /// 시상대 배너 = GUI Pro <c>Social_Ranking</c> 의 <c>Group_RankingPodium/&lt;자리&gt;/Podium</c> 조각 그대로(펜던트 배너 슬라이스 + <c>Text_Name</c> + <c>Group_Trophy</c>(🏆 + 점수)) —
        /// 표 ⑮ «시상대 배너» 자리에 늘려 넣고 글자만 우리 것으로 바꾼다(T62 · 주인 «랭킹 UI 는 그 프리팹에서 조금 변형해 쓰라»).
        /// 조각을 못 찾으면(카탈로그 미스) 색 판으로 그린다 — 화면이 비지 않게.
        /// </summary>
        RectTransform Banner(RectTransform parent, string name, Layout.R r, Transform proto, string seat, string who, int rank)
        {
            var b = UiKit.Rect(parent, name); UiKit.Pct(b, r);
            var seatT = proto != null ? UiKit.Find(proto, seat) : null;
            var piece = seatT != null ? UiKit.Find(seatT, "Podium") : null;
            if (piece == null)
            {
                var img = UiKit.Panel(b, "Cloth", "fr.label", DeepRed); UiKit.Stretch(img.rectTransform);
                UiKit.Label(b, 4, 8, 92, 32, who, 40, Palette.White).fontStyle = FontStyle.Bold;
                UiKit.Label(b, 4, 44, 92, 30, DummyScore(rank), 40, Palette.Yellow).fontStyle = FontStyle.Bold;
                return b;
            }
            piece.SetParent(b, false); piece.name = "Cloth"; UiKit.Stretch((RectTransform)piece);
            // 배너 안 두 줄 — 이름(위) · 🏆 점수(아래). 조각의 px 자리는 배너 본래 크기(296×279)용이라 표 자리 비율로 다시 잡는다.
            var nm = UiKit.Find(piece, "Text_Name") as RectTransform; if (nm != null) UiKit.Pct(nm, 4, 20, 92, 24);
            var grp = UiKit.Find(piece, "Group_Trophy") as RectTransform;
            if (grp != null)
            {
                UiKit.Pct(grp, 6, 44, 88, 30);
                var h = grp.GetComponent<HorizontalLayoutGroup>(); if (h != null) h.childAlignment = TextAnchor.MiddleCenter;
            }
            var nt = UiKit.SetText(piece, "Text_Name", who, Palette.White, 40); if (nt != null) nt.fontStyle = FontStyle.Bold;
            UiKit.SetText(piece, "Text_Value", DummyScore(rank), Palette.Yellow, 40);
            return b;
        }

        /// <summary>Social_Ranking 프리팹을 <b>꺼진 채로</b> 한 번 세운다 — 시상대 배너 조각 3개를 떼어 쓰고 바로 버린다(데모 화면 전체를 켜지 않는다 · T62).</summary>
        static Transform RankProto(RectTransform pg)
        {
            var hold = UiKit.Rect(pg, "RankProto"); hold.gameObject.SetActive(false);
            UiKit.Spawn("ui.socialRanking", hold);
            return hold;
        }

        /// <summary>
        /// 순위 줄 = GUI Pro <c>ListItem_Ranking</c> 프리팹 그대로(어두운 줄 프레임 · 등수 · 초상 · 이름 · 아래 배지 줄 · 오른쪽 🏆) —
        /// 본래 크기 988×158 이 표 ⑮ «순위 줄»(95.1%×6.7% = 1027×157) 과 같아 늘려 넣기만 하면 레퍼런스 23 줄 구성이 그대로 선다(T62).
        /// 우리 것으로 바꾸는 건 글자 넷과 아이콘 둘뿐 — 길드 배지 자리는 레퍼런스의 «칼 + 전투력» 줄로 쓴다.
        /// </summary>
        void RankItem(RectTransform row, int rank, string icon)
        {
            var item = UiKit.Spawn("ui.listRanking", row); var irt = (RectTransform)item.transform; UiKit.Stretch(irt);
            DarkenListFrame(irt);
            UiKit.SetText(irt, "Text_RankingNum", rank.ToString(), Palette.Gray, 44, TextKind.Aux);
            var nt = UiKit.SetText(irt, "Text_Name", FoeName(rank), Palette.White, 44); if (nt != null) nt.fontStyle = FontStyle.Bold;
            UiKit.SetText(irt, "Text_Value", DummyScore(rank), Palette.Yellow, 40);
            UiKit.Hide(irt, "Icon_NoGuild", "Text_NoGuild");
            UiKit.Show(irt, "Icon_GuildBadge", true); UiKit.Show(irt, "Text_GuildName", true);
            UiKit.SetSprite(irt, "Icon_GuildBadge", "ui.battle", Palette.White);
            var gt = UiKit.SetText(irt, "Text_GuildName", DummyPower(rank), Palette.Orange, 36, TextKind.Aux);
            if (gt != null) _dummyPowerTexts.Add(new KeyValuePair<Text, int>(gt, rank));
            var face = UiKit.Find(irt, "ProfileArea");
            if (face != null)
            {
                face.name = "Face";
                var ci = UiKit.SetSprite(face, "Character", icon, Palette.White); if (ci != null) ci.preserveAspect = true;
            }
        }
        /// <summary>
        /// 줄 프레임 색만 어둡게(T62 회차 1 감점) — <c>ListItem_Ranking</c> 이 물고 오는 <c>ListFrame_02</c> 는 Theme_Light 의 <b>밝은 크림</b> 줄이라
        /// 레퍼런스 23(어두운 바탕 위 어두운 줄)과 정반대이고 그 위의 흰 이름 글자가 안 읽혔다. 조각(테두리 스프라이트·9-slice)은 그대로 두고 색만 바꾼다.
        /// </summary>
        static void DarkenListFrame(Transform root)
        {
            var frame = UiKit.Find(root, "ListFrame_02"); if (frame == null) return;
            var bg = UiKit.Find(frame, "Normal/Bg"); if (bg != null) { var im = bg.GetComponent<Image>(); if (im != null) im.color = RowBody; }
            var left = UiKit.Find(frame, "Normal/BgLeft"); if (left != null) { var im = left.GetComponent<Image>(); if (im != null) im.color = RowLeft; }
            var border = UiKit.Find(frame, "Normal/Border1"); if (border != null) { var im = border.GetComponent<Image>(); if (im != null) im.color = RowBorder; }
        }

        static Text Pill(RectTransform row, Layout.R r, string icon, string text, Color color)
        {
            var p = UiKit.Panel(row, "Pill", "fr.r12", Palette.Hex("#1E1E1E")); UiKit.Pct(p.rectTransform, r);
            var ic = UiKit.Icon(p.transform, "Icon", icon); UiKit.Pct(ic.rectTransform, 6, 10, 26, 80);
            var t = UiKit.Label(p.transform, 36, 0, 60, 100, text, TextSize.Body, color, TextAnchor.MiddleLeft); t.fontStyle = FontStyle.Bold;
            return t;
        }
        /// <summary>공통 팝업 위에 레퍼런스의 <b>평평한 제목 띠</b> — 리본(Title 조각)은 끄고 박스 윗변에 색 띠 + 굵은 흰 글자(워커 결정 기록).</summary>
        static void FlatHead(RectTransform box, Layout.R boxRect, Layout.R headRect, Color color, string title)
        {
            foreach (var key in new[] { "ui.title.red", "ui.titleBrown", "ui.title.tangerine" }) { var rb = UiKit.Find(box, key); if (rb != null) rb.gameObject.SetActive(false); }
            var head = UiKit.Panel(box, "Head", "fr.r12", color); UiKit.Pct(head.rectTransform, headRect.Within(boxRect)); head.raycastTarget = true; UiKit.Tag(head.transform, "제목 띠");
            UiKit.Gradient(head.rectTransform, inset: HeadGradientInset);
            UiKit.Label(head.transform, 4, 0, 92, 100, title, TextSize.Title, Palette.White, kind: TextKind.Title).fontStyle = FontStyle.Bold;
            UiKit.Tag(box, "팝업 박스");
        }
        /// <summary>팝업 조각이 달고 오는 장식 선(«DecoLine»·«LineDeco» 계열)을 전부 끈다 — T102 ⓐ(21 세부 팝업의 빨간 선).</summary>
        static void HideDeco(RectTransform box)
        {
            if (box == null) return;
            foreach (var t in box.GetComponentsInChildren<Transform>(true))
                if (t != null && (t.name.Contains("DecoLine") || t.name.Contains("LineDeco"))) t.gameObject.SetActive(false);
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
