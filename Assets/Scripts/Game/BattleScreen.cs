using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 인게임 화면 — 엔진(<see cref="BattleState"/>) 을 고정 틱(1/30초 · sim.js dt)으로 돌리고 <see cref="BattleWorld"/> 와 HUD 를 갱신한다.
    /// 팝업(Overlay)이 열려 있거나 일시정지면 시간이 흐르지 않는다(T79 규칙). 배치는 <see cref="Layout"/> ② 인게임 자.
    /// </summary>
    public sealed class BattleScreen : GameScreen
    {
        public override string Name => "battle";

        /// <summary>index.html STAT_DEFS — 8칸 · 같은 순서 · 같은 값 표기. HUD 하단 그리드와 특전 팝업 상단 줄이 함께 쓴다.</summary>
        public sealed class StatDef
        {
            public string Key, Label; public Func<BattleState, double> Cur; public Func<double, string> Show;
            public string Fmt(BattleState G) => Show(Cur(G));
            public bool Up(BattleState G, Dictionary<string, double> baseStats) => baseStats != null && baseStats.TryGetValue(Key, out var b) && Cur(G) > b + 0.001;
        }
        public static readonly StatDef[] StatDefs =
        {
            new StatDef { Key = "dmg", Label = "공격력", Cur = g => g.EffDmg(), Show = v => UiKit.Fmt(v) },
            new StatDef { Key = "def", Label = "방어력", Cur = g => g.EffDef(), Show = v => v.ToString("0.0") + "%" },
            new StatDef { Key = "aspd", Label = "공격속도", Cur = g => g.EffAspd(), Show = v => v.ToString("0.00") + "/s" },
            new StatDef { Key = "counter", Label = "반격 확률", Cur = g => g.EffCounter(), Show = v => v.ToString("0.0") + "%" },
            new StatDef { Key = "critR", Label = "치명타 확률", Cur = g => g.EffCritR(), Show = v => v.ToString("0") + "%" },
            new StatDef { Key = "evade", Label = "회피", Cur = g => g.EffEvade(), Show = v => v.ToString("0.0") + "%" },
            new StatDef { Key = "critF", Label = "치명타 배율", Cur = g => g.EffCritF(), Show = v => v.ToString("0") + "%" },
            new StatDef { Key = "steal", Label = "흡혈", Cur = g => g.EffSteal(), Show = v => v.ToString("0") + "%" },
        };

        public BattleState G { get; private set; }
        public Dictionary<string, double> BaseStats { get; private set; }
        BattleWorld _world;
        /// <summary>월드 그리기(테스트·진단용 읽기 전용 — T20 PlayMode 테스트가 표시 원점·킬 연출 대기를 본다).</summary>
        public BattleWorld World => _world;
        double _acc; int _speed = 1; bool _paused, _ended;
        RectTransform _pops;

        // HUD (T35 · 레퍼런스 02_battle.jpg / 03_battle_enemy.jpg 구도)
        TMP_Text _kills, _gold, _chapTitle, _speedTxt;
        // T240 1항 — PvP 머리(빨간 VS 바 · 금테 배지 · 아바타 칸 둘 · 이름 둘 · 전투력 줄 둘). 챕터 판에서는 꺼 둔다.
        RectTransform _chapTitleBox, _pvpHead;
        /// <summary>
        /// T240 ⓑ(결정 800) — <b>PvP 에서 끄는</b> 챕터 전투 HUD 묶음(하단 패널 · 바 셋 · 스탯 8칸 · 특전 줄 · 책 · 배속 · 우하단 원형 · 상단 pill 둘).
        /// <para>주인 레퍼런스 <c>33_pvp_battle.jpg</c> 의 PvP 화면에는 <b>하단 패널이 통째로 없다</b> — 바닥이 잔디이고 우하단에 원형 버튼만 있다.</para>
        /// </summary>
        RectTransform[] _chapterHud;
        TMP_Text _pvpMyName, _pvpFoeName, _pvpMyPower, _pvpFoePower;
        /// <summary>PvP 머리의 초상 칸 둘(T262 3항) — 조각은 <see cref="Profile.Frame"/> 이 세운다. 판마다 얼굴이 달라지므로 «세우는 자리» 가 아니라 «켜는 자리»(ShowPvpHead)에서 채운다.</summary>
        RectTransform _pvpMyFace, _pvpFoeFace;
        UiKit.Bar _prog, _exp, _hp, _sh; Image _progFill;
        RectTransform _buffBar, _perkStrip; TMP_Text _perkCount; HorizontalLayoutGroup _perkStripLayout;
        readonly TMP_Text[] _statVals = new TMP_Text[StatDefs.Length];
        string _perkStripKey = "", _buffKey = "";
        // T85 — 보상 흡수 연출: 엔진 값(G.Gold · P.Exp)은 킬 순간에 이미 올라 있고(불변), 화면은 «구슬이 도착한 만큼» 만 올린다.
        RectTransform _goldPill, _orbLayer; RewardOrbs _orbs;
        double _shownGold, _shownExp;        // 표시값 — 골드 · 경험치는 «누적»(레벨 경계를 넘으면 바가 다시 찬다)
        double _goldTarget, _expTarget;      // 도착분까지 반영된 목표(카운트업이 여기로 간다)
        double _goldRate, _expRate;          // 카운트업 속도(초당) — 목표가 늘면 다시 계산해 CountUpSec 안에 따라잡는다
        double _flyGold, _flyExp;            // 아직 날아가는 중인 구슬이 들고 있는 값
        float _overWait;                     // 사망·클리어에서 흡수를 기다린 시간(AbsorbMaxWaitSec 넘으면 강제 완료)
        int _questKills;                     // T257 4항 — 퀘스트에 이미 넘긴 처치 수(이 판) · 판이 끝날 때 «G.Kills - 이것» 만 넘긴다
        const float CountUpSec = 0.2f, AbsorbMaxWaitSec = 0.6f, OrbSizePx = 64f;
        const int OrbMinCount = 3, OrbBossCount = 8;

        protected override void Build()
        {
            _pops = UiKit.Rect(Root, "Pops"); UiKit.Stretch(_pops);
            // ── 상단(레퍼런스 02): 왼쪽 작은 pill 2(처치 수 · 이번 판 골드) · 오른쪽 메뉴(≡) · 가운데 «챕터 N» + 진행바 ──
            // pill 2 = ResourceBar_Group 의 Gem·Coin 두 칸을 뜯어(가로 레이아웃 끔 · 세 번째 GemStone 은 끔) HudPills 안에 나란히 — 왼쪽 칸은 아이콘만 해골(pi.skull)로 바꿔 «처치 수»
            var pills = UiKit.SpawnRt("ui.resourceBar", Root, Layout.HudPills); pills.name = "Pills";
            var phl = pills.GetComponent<HorizontalLayoutGroup>(); if (phl != null) phl.enabled = false;
            UiKit.Hide(pills, "ResourceBar_GemStone");
            var killPill = UiKit.Find(pills, "ResourceBar_Gem") as RectTransform; var goldPill = UiKit.Find(pills, "ResourceBar_Coin") as RectTransform;
            if (killPill != null) { killPill.name = "Pill:kills"; UiKit.Pct(killPill, 0, 0, 46, 100); UiKit.SetSprite(killPill, "Icon", "pi.skull"); _kills = UiKit.SetText(killPill, "Text (TMP)", "0"); }
            if (goldPill != null) { goldPill.name = "Pill:gold"; UiKit.Pct(goldPill, 54, 0, 46, 100); _gold = UiKit.SetText(goldPill, "Text (TMP)", "0"); _goldPill = goldPill; }
            // T69-lobby — HUD pill 2개도 «검은 아웃라인»(결정 149 가 «둥근 pill 에 사각 링이 어긋난다» 며 미룬 것 · 캡슐 조각 BorderKeyPill 로 닫는다 · 레퍼런스 02 의 pill 도 검은 외곽선)
            foreach (var pill in new[] { killPill, goldPill })
            {
                if (pill == null) continue;
                UiKit.Bordered(pill, UiKit.BorderKeyPill);
                var picon = UiKit.Find(pill, "Icon");
                if (picon != null) picon.SetAsLastSibling();
            }
            var menu = UiKit.SpawnRt("ui.btnMenu", Root, Layout.HudMenu); menu.name = "Button_Menu"; UiKit.Clickable(menu, OnPause);   // ≡ → 일시정지 팝업(재개 · 로비로 · 설정)
            var title = UiKit.SpawnRt("ui.lineTitle", Root, new Layout.R(Layout.HudChapTitle.X - 6, Layout.HudChapTitle.Y - 1.2f, Layout.HudChapTitle.W + 12, Layout.HudChapTitle.H + 2.4f));
            _chapTitleBox = title;
            _chapTitle = UiKit.SetText(title, "Text (TMP)", "챕터 1", size: UiKit.FontForHeight(Layout.HudChapTitle.H));   // 글자 높이 = 표 2.6%(T47 회차 2 에서 1.5% 로 작았다)
            UiKit.Show(title, "LineDeco", false);   // T111 ⓐ — 주인 2026-09-07 «챕터 아래에 LineDeco 들은 없애줘 · 로비, 전투 화면 둘 다»(글자·자리는 그대로)
            // 진행바 = 검정 홈에 주황이 차는 바(레퍼런스 02·03) — 값은 노드(웨이브) 진행(RefreshHud) · 숫자 없음(T33) · 적 조우 중엔 주황, 걷는 중엔 노랑
            _prog = UiKit.MakeBar(Root, "ui.sliderYellow"); UiKit.Pct(_prog.Root, Layout.HudProgress); _prog.Root.name = "Bar:Progress"; if (_prog.Txt != null) _prog.Txt.gameObject.SetActive(false);
            _progFill = _prog.Slider != null && _prog.Slider.fillRect != null ? _prog.Slider.fillRect.GetComponent<Image>() : null;
            // T69 8항의 «바 테두리» 규칙을 챕터 진행 바에도(레퍼런스 02 의 검정 홈 = 어두운 아웃라인) — BorderGate strict 02_battle «진행 바 Bar:Progress 테두리 없음»(CI #117·#119 · T80) · 자리·크기 불변
            UiKit.Bordered(_prog.Root);
            // 버프 바 (왼쪽 세로 · 팔각 프레임 · T20 그대로)
            _buffBar = UiKit.Rect(Root, "BuffBar"); UiKit.Pct(_buffBar, Layout.HudBuffBar);
            var vl = _buffBar.gameObject.AddComponent<VerticalLayoutGroup>(); vl.childAlignment = TextAnchor.UpperLeft; vl.spacing = 10; vl.childForceExpandWidth = false; vl.childForceExpandHeight = false; vl.childControlWidth = false; vl.childControlHeight = false;
            // ── 패널 바로 위: 왼쪽 아래 배속 «x1/x2»(T18 기억) · 오른쪽 아래 둥근 펫 버튼(껍데기 · 레퍼런스 자리 = HudRound · T33 이 비운 자리) ──
            var spd = UiKit.SpawnRt("ui.btnSmallBlue", Root, Layout.HudSpeed); spd.name = "SpeedBtn"; _speedTxt = UiKit.ButtonText(spd); UiKit.Clickable(spd, ToggleSpeed);
            var pet = UiKit.Rect(Root, "PetBtn"); UiKit.Pct(pet, Layout.HudRound);
            var petBg = UiKit.Icon(pet, "Bg", "fr.circle", Palette.Plum); UiKit.Stretch(petBg.rectTransform);              // 보라 원(GUI Pro 원형 프레임 · 색은 팔레트)
            var petBd = UiKit.Icon(pet, "Border", "fr.circleBorder", Palette.Cream); UiKit.Stretch(petBd.rectTransform);
            var petIc = UiKit.Icon(pet, "Icon", "ui.petIcon"); UiKit.Pct(petIc.rectTransform, 24, 24, 52, 52);
            UiKit.Clickable(pet, () => { });                                                                                 // 껍데기 — 눌러도 아무 일 없음(주인 ⓔ · 펫 시스템 없음)
            // ── 하단 패널(30.5%): 바 3개 한 줄 → 스탯 8칸(2열×4행) → 특전 미리보기 줄 + 📘 ──
            UiKit.SpawnRt("ui.frameDark", Root, Layout.HudPanel).name = "HudPanel";
            // EXP = 초록 라벨 «EXP» + 바 + «현재/필요»(주인 강조 · 레벨 숫자는 레퍼런스에 없어 안 쓴다 · 워커 결정) · ❤ HP 빨강 · 🛡 실드 파랑 — 각 바 왼쪽에 아이콘, 바 안에 흰 «현재/최대»
            _exp = UiKit.MakeBar(Root, "ui.sliderGreen"); UiKit.Pct(_exp.Root, Layout.HudExp); _exp.Root.name = "Bar:EXP";
            {
                var cap = UiKit.Panel(_exp.Root, "Cap", "fr.r12", Palette.Green); var crt = cap.rectTransform;
                crt.anchorMin = crt.anchorMax = new Vector2(0, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f); crt.sizeDelta = new Vector2(104, 64); crt.anchoredPosition = new Vector2(14, 2);
                var ct = UiKit.Text(cap.transform, "EXP", 34, Palette.White, TextAnchor.MiddleCenter, false, true); ct.fontStyle = FontStyles.Bold; UiKit.Stretch(ct.rectTransform);
            }
            _hp = UiKit.MakeBar(Root, "ui.sliderRed", "pi.heart"); UiKit.Pct(_hp.Root, Layout.HudHp); _hp.Root.name = "Bar:HP";
            _sh = UiKit.MakeBar(Root, "ui.sliderBlue", "pi.shield"); UiKit.Pct(_sh.Root, Layout.HudSh); _sh.Root.name = "Bar:SH";
            foreach (var b in new[] { _hp, _sh }) if (b.Cap != null) { b.Cap.rectTransform.sizeDelta = new Vector2(84, 84); b.Cap.rectTransform.anchoredPosition = new Vector2(6, 2); }   // 아이콘이 바보다 조금 크게(레퍼런스)
            foreach (var b in new[] { _exp, _hp, _sh }) if (b.Txt != null) { b.Txt.color = Palette.White; b.Txt.fontStyle = FontStyles.Bold; b.Txt.alignment = UiKit.TmpAlign(TextAnchor.MiddleCenter); }
            // T69 8항(주인 «HP·실드 바 테두리») — 세 바에 검은 아웃라인(맨 앞) · 왼쪽 캡(«EXP» 라벨 · ❤ · 🛡)은 테두리 위로 · 바 자리·크기 불변
            foreach (var b in new[] { _exp, _hp, _sh }) { UiKit.Bordered(b.Root); var capT = UiKit.Find(b.Root, "Cap"); if (capT != null) capT.SetAsLastSibling(); }
            // 스탯 8칸 = 칸마다 어두운 상자(ui.frameDark) · 왼쪽 아이콘 · 오른쪽에 이름(보조 36 · 위) + 값(본문 40 · 아래) · 버프 중 값 초록(레퍼런스 02) — 자리 = 표(HudStats · 행 피치 5.2) · 상자 사이 틈 0.4%
            // T125 3항 — 이름 글자는 CreamDark(#E3CDAA) 였는데 칸이 «갈색» 이라 값(흰색)에 비해 눈에 띄게 묻혔다(screens run 239 02 PNG 3배 확대).
            // Cream(#F5E9D0) 으로 한 단 올려 대비를 세운다 — 값보다는 여전히 어두워 «이름 → 값» 위계는 레퍼런스 02 처럼 남는다.
            // T63-battle: 칸 높이 4.8%(112px) = 이름 46%(51px ≥ 36×1.375) + 값 52%(58px ≥ 40×1.375) — 전엔 4.6% 칸에 40%/48% 라 bestFit 이 이름을 32 · 값을 37 로 몰래 줄였다(게이트 표엔 안 걸림 · T63 진행 기록 ⚠)
            for (int i = 0; i < StatDefs.Length; i++)
            {
                int col = i % 2, row = i / 2;
                var cell = UiKit.SpawnRt("ui.frameDark", Root, new Layout.R(Layout.HudStats.X + col * Layout.HudStatColR + 0.4f, Layout.HudStats.Y + row * Layout.HudStatRowPitch + 0.2f, Layout.HudStatCellW - 0.8f, Layout.HudStatCellH - 0.4f));
                cell.name = "stat:" + StatDefs[i].Key;
                var ic = UiKit.Icon(cell, "ic", Icons.Stat(StatDefs[i].Key)); UiKit.Pct(ic.rectTransform, 3, 12, 15, 76);
                var lb = UiKit.Label(cell, 21, 1, 76, 46, StatDefs[i].Label, TextSize.Aux, Palette.Cream, TextAnchor.MiddleLeft, kind: TextKind.Aux); lb.name = "Label";
                _statVals[i] = UiKit.Label(cell, 21, 47, 76, 52, "", TextSize.Body, Palette.White, TextAnchor.MiddleLeft); _statVals[i].name = "Value"; _statVals[i].fontStyle = FontStyles.Bold;
                // T69 — 스탯 칸마다 검은 아웃라인(어두운 상자 위 · 자리 불변)
                UiKit.Bordered(cell);
            }
            // 보유 특전 = 책 모양 버튼(특전 선택 팝업의 Book 과 같은 그림 · 위에 개수) — 주인 지시 2026-09-05
            // T142 — 자리는 주인이 인스펙터로 준 값 그대로다(Layout.HudInfo · 화면 바닥 + 오른쪽 끝). 종전처럼 여기서 ±여유를 더하지 않는다.
            var info = UiKit.Rect(Root, "PerkBook"); UiKit.Pct(info, Layout.HudInfo);
            var book = UiKit.Icon(info, "Book", "ui.bookBlue"); UiKit.Stretch(book.rectTransform);
            _perkCount = UiKit.Text(info, "0", 30, Palette.White, TextAnchor.MiddleCenter, false, true); UiKit.Pct(_perkCount.rectTransform, 45, 40, 55, 50);
            UiKit.Clickable(info, () => { if (G != null && !App.Overlay.IsOpen) App.Overlay.PerkBook(G, null); });
            _perkStrip = UiKit.Rect(Root, "PerkStrip"); UiKit.Pct(_perkStrip, Layout.HudPerkStrip);
            var hl = _perkStripLayout = _perkStrip.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.childAlignment = TextAnchor.MiddleLeft; hl.spacing = 8; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false; hl.childControlWidth = false; hl.childControlHeight = false;   // spacing 은 RefreshPerkStrip 이 줄 높이에서 다시 계산
            var stripHit = _perkStrip.gameObject.AddComponent<Image>(); stripHit.color = new Color(0, 0, 0, 0); UiKit.Clickable(_perkStrip, () => { if (G != null && !App.Overlay.IsOpen) App.Overlay.PerkBook(G, null); }, false);
            // 비평 이름표(T46 · ref-layout ② 의 «요소» 이름 그대로) — 월드 행(지면 띠 · 발밑 y · 캐릭터 높이 · 바 폭)은 캔버스 밖이라 여기 없다(T47 이 BattleWorld 에서 잰다)
            UiKit.TagGroup(Root, "상단 HUD pill 2개", killPill, goldPill); UiKit.Tag(menu, "메뉴(☰) 버튼");
            if (_chapTitle != null) UiKit.Tag(_chapTitle.transform, "챕터 제목", textBounds: true); UiKit.Tag(_prog.Root, "진행 바");   // 글자 덩어리로 잰다(T47 ⓒ · 조각은 ±6/12 여유)
            UiKit.Tag(spd, "배속 버튼"); UiKit.Tag(pet, "우하단 원형 버튼"); UiKit.Tag(UiKit.Find(Root, "HudPanel"), "하단 패널");
            UiKit.Tag(_exp.Root, "EXP 바"); UiKit.Tag(_hp.Root, "HP 바"); UiKit.Tag(_sh.Root, "실드 바");
            var cells = new RectTransform[StatDefs.Length]; for (int i = 0; i < StatDefs.Length; i++) cells[i] = UiKit.Find(Root, "stat:" + StatDefs[i].Key) as RectTransform;
            UiKit.TagGroup(Root, "스탯 그리드", cells); UiKit.Tag(cells[0], "스탯칸(1칸)"); UiKit.Tag(info, "인포(책) 버튼");

            BuildPvpHead();   // T240 1항 — 아레나 판에서만 켜진다(Start 가 IsArena 로 켠다)
            BuildPvpRound();  // T240 1항 — 우하단 «AUTO» (PvpHead 안이라 같이 켜지고 꺼진다)
            // T240 ⓑ(결정 800) — PvP 에서 **끄는** 챕터 전투 HUD 묶음. 만들어 두고 끄는 꼴은 PvP 머리와 같다:
            //   지우지 않으므로 RefreshHud 가 그대로 써도 되고(꺼진 글자에 쓰는 것은 값이 없을 뿐 탈이 없다),
            //   챕터 판으로 돌아오면 다시 켜기만 하면 된다(한 화면이 두 판을 번갈아 연다).
            var hud = new List<RectTransform> { pills, spd, pet, UiKit.Find(Root, "HudPanel") as RectTransform, _exp.Root, _hp.Root, _sh.Root, _perkStrip, info };
            hud.AddRange(cells);
            _chapterHud = hud.ToArray();
            // T85 — 보상 구슬 층은 HUD «위» (마지막 형제): 구슬이 하단 패널 안의 EXP 바까지 가려지지 않고 날아가야 한다. 글자·이름표 없음(비평 표·게이트 불변).
            _orbLayer = UiKit.Rect(Root, "Orbs"); UiKit.Stretch(_orbLayer); _orbLayer.SetAsLastSibling();
            _orbs = new RewardOrbs(_orbLayer);
        }

        // ───────────────────────── 시작 · 종료 ─────────────────────────
        /// <summary>
        /// 판을 시작한다. <paramref name="run"/> 는 <b>던전 판 규칙</b>(T183 · 시작 특전 N · 시작 레벨 · 특전 등급 하한)이고
        /// <c>null</c> 이면 <b>지금까지와 똑같은 일반 챕터 전투</b>다(기본값 = 아무 데도 안 닿는다).
        /// </summary>
        public void Start(int chapter, DungeonData.RunRule run = null, string dungeonKey = null, string arenaFoe = null, int arenaFoeRank = 0)
        {
            _dunKey = dungeonKey;
            _arenaFoe = arenaFoe; _arenaFoeRank = arenaFoeRank;   // T240 — 아레나 «도전» 으로 들어온 판이면 상대 이름(null = 아니다) · 순위는 결과 화면(34)의 얼굴에도 쓴다(T262 3항)   // T228 ⓓ — 이 판이 «어느 던전» 인가(null = 일반 챕터 전투)
            var D = App.Data;
            var rng = new Mulberry32((uint)Environment.TickCount ^ 0x9E3779B9u);
            var opt = new RunOptions { EmitEvents = true };
            if (run != null) { opt.StartPerks = run.StartPerks; opt.StartLevel = run.StartLevel; opt.MinPerkGrade = run.MinPerkGrade; }
            opt.ArenaDuelFoe = DuelFoe(D, arenaFoe, arenaFoeRank);   // T240 3항 — 아레나 판이면 «웨이브 없는 1대1»(null 이면 종전 챕터 전투 그대로)
            _exitPage = _arenaFoe != null ? EventsScreen.PageArena       // T240 — 아레나 판은 아레나 화면(23)으로 되돌린다
                      : run != null ? EventsScreen.PageDungeon : null;   // T183 4단계 — 던전에서 들어온 판은 던전 화면으로 되돌린다(일반 전투는 그대로 로비)
            G = new BattleState(D, chapter, App.Save.CurBuild(D), rng, new InteractivePolicy(), opt);
            BaseStats = new Dictionary<string, double>(); foreach (var d in StatDefs) BaseStats[d.Key] = d.Cur(G);
            _world?.Dispose(); UiKit.Clear(_pops);   // 팝 층은 새 월드를 만들기 «전에» 비운다(발밑 숫자 글자가 팝 층에 산다 · T35)
            _world = new BattleWorld(App, G, _pops);
            _world.KillShown = OnKillShown;   // T85 — 시체가 쓰러지는 순간 그 자리에서 보상 구슬이 튀어나온다
            SnapShown();                      // 새 판은 표시값 = 엔진 값(0)에서 시작
            _questKills = 0;   // T257 — 새 판은 «넘긴 처치» 도 0 부터(부활은 판을 잇는 것이라 여기 안 온다)
            _acc = 0; _speed = App.Save.Speed; _paused = false; _ended = false; _revivesUsed = 0; _perkStripKey = ""; _buffKey = ""; _lastReal = 0;   // 배속은 세이브에서(T18 · 클리어 뒤 다음 챕터도 그대로) · 새 판 첫 프레임이 «공백» 으로 잡히지 않게
            Audio.Bgm("bgm.battle");   // 새 판(클리어 뒤 다음 챕터 포함)은 전투 곡부터 — 보스 곡이었으면 되돌린다(T28)
            ShowPvpHead(D, arenaFoe, arenaFoeRank);   // T240 1항 — 아레나면 PvP 머리를 켜고 챕터 제목·진행 바를 끈다
            RefreshHud();
        }
        /// <summary>
        /// T240 3항 — 아레나 판의 <b>상대 하나</b>를 푼다: 순위 → 상대 전투력(<see cref="ArenaDummy.Power"/>) → 스탯(<see cref="ArenaFoe.Of(ArenaFoeData, double, double, double, double)"/>).
        /// <para>
        /// 아레나 판이 아니거나(<paramref name="arenaFoe"/> 가 <c>null</c>) 표·순위가 없으면 <c>null</c> 을 돌려준다 —
        /// 그러면 <b>종전대로 챕터 전투</b>가 열린다. <b>못 읽었다고 아무 수나 지어내 판을 세우지 않는다</b>(§1).
        /// </para>
        /// <para>공격/체력 «몫» 은 <b>내 빌드의 실제 몫</b>이다 — 그래야 «전투력이 같으면 대등한 판» 이 된다(결정 715).</para>
        /// </summary>
        ArenaFoe.Stats? DuelFoe(GameData D, string arenaFoe, int rank)
        {
            if (arenaFoe == null || rank <= 0 || D == null || D.ArenaFoe == null || D.ArenaDummy == null) return null;
            double foePower = ArenaDummy.Power(D.ArenaDummy, App.Power(), rank);
            if (foePower <= 0) return null;
            var pw = GearSystem.BuildPower(D, App.Save.CurBuild(D));
            return ArenaFoe.Of(D.ArenaFoe, foePower, pw.Atk, pw.Hp, pw.Sh);
        }


        // ───────────────────────── T240 1항 PvP 머리(레퍼런스 33 · 표 ㊺) ─────────────────────────
        /// <summary>
        /// PvP 인게임의 머리 — <b>빨간 VS 바</b> 하나에 양쪽 정보 줄(금테 아바타 칸 · 이름 · 전투력)이 걸린다.
        /// 자리는 전부 <see cref="Layout"/> 의 <c>Pvp*</c>(표 ㊺ 실측)이고 <b>이 파일에 자리 수치를 박지 않는다</b>.
        /// <para>
        /// ⚠ <b>만들어 두고 꺼 둔다</b> — 챕터 전투에서 이 묶음이 켜져 있으면 §5 채점(02·03)과 테두리 감사가 «없던 요소» 를 세게 된다.
        /// 켜는 것은 <see cref="Start"/> 가 <see cref="IsArena"/> 로 한 번만 한다.
        /// </para>
        /// <para>
        /// ⚠ <b>«챕터 N» 제목과 진행 바는 같이 끈다</b> — PvP 는 웨이브가 없어 진행 바가 <b>잴 것이 없고</b>(늘 0 이거나 한 칸),
        /// 챕터 번호도 판을 안 바꾼다(1대1 은 챕터 표를 안 읽는다 · 결정 725). 켜 두면 «있는데 뜻이 없는 눈금» 이 된다.
        /// </para>
        /// </summary>
        void BuildPvpHead()
        {
            _pvpHead = UiKit.Rect(Root, "PvpHead"); UiKit.Stretch(_pvpHead);

            var bar = UiKit.Panel(_pvpHead, "VsBar", "fr.rect", Palette.ArenaVsBar);
            UiKit.Pct(bar.rectTransform, Layout.PvpBar);
            UiKit.Tag(bar.transform, "빨간 VS 바");

            // 금테 원 배지 — 조각은 이미 있는 것(우하단 원형 버튼이 쓰는 fr.circle/fr.circleBorder)이다. 새 그림 0(§1).
            var badge = UiKit.Rect(_pvpHead, "VsBadge"); UiKit.Pct(badge, Layout.PvpBadge);
            var bg = UiKit.Icon(badge, "Bg", "fr.circle", Palette.Ink); UiKit.Stretch(bg.rectTransform);
            var bd = UiKit.Icon(badge, "Border", "fr.circleBorder", Palette.Yellow); UiKit.Stretch(bd.rectTransform);
            var vs = UiKit.Label(badge, 0, 0, 100, 100, "VS", TextSize.Body, Palette.Yellow);
            vs.name = "VsText"; vs.fontStyle = FontStyles.Bold;
            UiKit.Tag(badge, "VS 배지(금·원)");

            PvpSide(true, out _pvpMyName, out _pvpMyPower);
            PvpSide(false, out _pvpFoeName, out _pvpFoePower);

            _pvpHead.gameObject.SetActive(false);
        }

        /// <summary>PvP 머리의 한쪽(아바타 칸 · 이름 · 전투력 줄) — 왼쪽이 나, 오른쪽이 상대이고 자리는 좌우 대칭이다(표 ㊺).</summary>
        void PvpSide(bool mine, out TMP_Text name, out TMP_Text power)
        {
            var faceR = mine ? Layout.PvpMyFace : Layout.PvpFoeFace;
            var nameR = mine ? Layout.PvpMyName : Layout.PvpFoeName;
            var powR = mine ? Layout.PvpMyPower : Layout.PvpFoePower;
            string who = mine ? "My" : "Foe";

            var box = UiKit.Rect(_pvpHead, who + "Face"); UiKit.Pct(box, faceR);
            // T262 3항 — 조각을 여기서 박지 않는다. 종전에는 `ui.itemFrame.yellow`(팔각 물건 칸)를 세워 두고 **안이 비어 있었다** —
            // 레퍼런스 33 의 머리 양쪽은 «프로필 프레임 + 얼굴» 이다(좌상단 프로필과 같은 둥근 네모). 얼굴은 판마다 달라지므로 ShowPvpHead 가 채운다.
            if (mine) _pvpMyFace = box; else _pvpFoeFace = box;
            UiKit.Tag(box, mine ? "아바타 칸(왼쪽)" : "아바타 칸(오른쪽)");

            // 이름 — 내 쪽은 칸 오른쪽에 붙어 왼쪽 정렬, 상대 쪽은 칸 왼쪽에 붙어 오른쪽 정렬(레퍼런스 33 그대로).
            var anchor = mine ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            name = UiKit.Label(_pvpHead, nameR.X, nameR.Y, nameR.W, nameR.H, "", TextSize.Body, Palette.White, anchor);
            name.name = who + "Name"; name.fontStyle = FontStyles.Bold;
            UiKit.Tag(name.transform, mine ? "이름(왼쪽)" : "이름(오른쪽)", textBounds: true);

            // 전투력 줄 = 검 아이콘 + 수. 아이콘은 이미 쓰는 것(ui.battle · 아레나 순위 줄이 같은 뜻으로 쓴다).
            var row = UiKit.Rect(_pvpHead, who + "Power"); UiKit.Pct(row, powR);
            var ic = UiKit.Icon(row, "Icon", "ui.battle");
            UiKit.Pct(ic.rectTransform, mine ? 0 : 88, 0, 12, 100);
            power = UiKit.Label(row, mine ? 14 : 0, 0, 86, 100, "", TextSize.Aux, Palette.White, anchor, kind: TextKind.Aux);
            power.name = who + "PowerText";
            UiKit.Tag(row, mine ? "전투력 줄(왼쪽)" : "전투력 줄(오른쪽)");
        }

        /// <summary>
        /// T240 1항 «원형 버튼» — 레퍼런스 33 의 우하단 셋 가운데 <b>AUTO 하나만</b> 세운다(결정 800 ⓐ).
        /// <para>
        /// <b>천사·악마는 안 만들었다</b> — 기능이 없는 것보다 <b>그림이 없는 것</b>이 먼저다:
        /// 카탈로그에 천사·악마 <b>UI 아이콘이 없고</b>(<c>fx.angel</c>·<c>fx.devil</c> 은 월드 파티클 프리팹이다) §1 이 <b>새 그림을 금한다</b>.
        /// 다른 아이콘을 끼워 «비슷하게» 만드는 순간 그 화면은 지어낸 것이 된다(워커 B 가 상대 장비에서 선 자리와 같다 · 결정 749).
        /// </para>
        /// <para>
        /// <b>누르는 길을 안 걸었다</b>(<see cref="UiKit.Clickable"/> 없음) — 우리 전투에 «자동» 이라는 기능 자체가 없다(판은 원래 스스로 돈다).
        /// 걸어 두면 «눌리는데 아무 일도 안 나는 버튼» 이 되는데, 그것을 막는 싼 길은 «못 누르게» 가 아니라 <b>«안 걸어 두기»</b> 다(T257 · 결정 768).
        /// 기능이 생기는 날 <see cref="Layout.PvpAuto"/> 자리는 그대로 두고 여기에 한 줄만 더하면 된다.
        /// </para>
        /// </summary>
        void BuildPvpRound()
        {
            var auto = UiKit.Rect(_pvpHead, "PvpAutoBtn"); UiKit.Pct(auto, Layout.PvpAuto);
            var bg = UiKit.Icon(auto, "Bg", "fr.circle", Palette.Yellow); UiKit.Stretch(bg.rectTransform);
            var bd = UiKit.Icon(auto, "Border", "fr.circleBorder", Palette.Cream); UiKit.Stretch(bd.rectTransform);
            var t = UiKit.Label(auto, 0, 0, 100, 100, "AUTO", TextSize.Aux, Palette.Ink, kind: TextKind.Aux);
            t.name = "AutoText"; t.fontStyle = FontStyles.Bold;
            UiKit.Tag(auto, "원형 버튼 AUTO");
        }

        /// <summary>
        /// T240 1항 — 이 판이 아레나면 PvP 머리를 켜고 «챕터 N» 제목·진행 바를 끈다(챕터 판이면 그 반대).
        /// <paramref name="foeRank"/> 가 0(모름)이면 상대 전투력은 «—» 로 둔다 — <b>모르는 수를 지어내지 않는다</b>.
        /// </summary>
        void ShowPvpHead(GameData D, string foeName, int foeRank)
        {
            bool on = IsArena;
            if (_pvpHead != null) _pvpHead.gameObject.SetActive(on);
            // T240 ⓑ — PvP 면 챕터 HUD 를 통째로 끈다(레퍼런스 33 에 하단 패널이 없다 · 결정 800).
            //   ⚠ 켜는 쪽도 반드시 있어야 한다 — 한 BattleScreen 이 아레나 판과 챕터 판을 번갈아 연다.
            if (_chapterHud != null) foreach (var rt in _chapterHud) if (rt != null) rt.gameObject.SetActive(!on);
            if (_chapTitleBox != null) _chapTitleBox.gameObject.SetActive(!on);
            if (_prog != null && _prog.Root != null) _prog.Root.gameObject.SetActive(!on);
            if (!on) return;

            // T262 3항 — 왼쪽은 «나»(프로필에서 고른 프레임 색·얼굴), 오른쪽은 그 순위의 더미 얼굴(23·24 목록과 같은 얼굴 · Profile.DummyIcon)
            Profile.Frame(_pvpMyFace, Profile.FrameKey(App.Save), Profile.CurrentIcon(App.Save));
            Profile.Frame(_pvpFoeFace, Profile.FrameKeyPrefix + Profile.Colors[0], Profile.DummyIcon(foeRank));
            if (_pvpMyName != null) _pvpMyName.text = Nickname.Of(App.Save);
            if (_pvpFoeName != null) _pvpFoeName.text = foeName ?? "";
            if (_pvpMyPower != null) _pvpMyPower.text = UiKit.Fmt(App.Power());
            if (_pvpFoePower != null)
            {
                bool known = foeRank > 0 && D != null && D.ArenaDummy != null;
                _pvpFoePower.text = known ? UiKit.Fmt(ArenaDummy.Power(D.ArenaDummy, App.Power(), foeRank)) : "—";
            }
        }

        protected override void OnHide() { _world?.Dispose(); _world = null; UiKit.Clear(_pops); _orbs?.Clear(); _flyGold = _flyExp = 0; }

        /// <summary>현재 배속(x1/x2) — 테스트·진단용 읽기.</summary>
        public int Speed => _speed;
        /// <summary>배속 버튼 — x1 ↔ x2. 값은 세이브(<see cref="SaveData.Speed"/>)에 즉시 기록해 다음 판(클리어 뒤 다음 챕터 · 로비에서 재진입 · 앱 재시작)도 같은 배속으로 시작한다(T18).</summary>
        public void ToggleSpeed()
        {
            _speed = _speed == SaveData.SpeedMin ? SaveData.SpeedMax : SaveData.SpeedMin;
            App.Save.Speed = _speed; App.Persist();
            RefreshHud();
        }

        /// <summary>
        /// T183 4단계 — <b>판이 끝나면 갈 곳</b>. <c>null</c> = 로비(지금까지와 똑같은 일반 챕터 전투)이고,
        /// 던전 판(<see cref="Start(int, DungeonData.RunRule)"/> 의 <c>run != null</c>)이면 그 던전 화면의 페이지 키다 —
        /// 던전에서 들어간 판이 끝났는데 로비로 튕기면 티켓을 또 쓰러 갈 자리를 사람이 다시 찾아야 한다.
        /// </summary>
        string _exitPage;
        /// <summary>테스트·진단용 읽기 — 이 판이 끝나면 갈 곳(<c>null</c> = 로비).</summary>
        public string ExitPage => _exitPage;
        /// <summary>
        /// T228 ⓓ — 이 판이 <b>어느 던전</b> 에서 들어온 것인가(<c>null</c> = 일반 챕터 전투).
        /// <para><see cref="_exitPage"/> 는 «던전 화면으로 돌아가라» 는 <b>페이지</b> 키 하나뿐이라 어느 던전인지 모른다 — 그래서 키를 따로 싣는다.
        /// 이것이 없으면 «클리어한 던전만 소탕» 규칙이 영원히 안 켜진다(클리어를 아무도 안 적으므로).</para>
        /// </summary>
        string _dunKey;
        /// <summary>T240 — 아레나 «도전» 으로 들어온 판의 <b>상대 이름</b>(<c>null</c> = 아레나가 아니다). 이 값 하나가 <see cref="EndRun"/> 의 아레나 갈래를 켠다.</summary>
        string _arenaFoe; int _arenaFoeRank;
        /// <summary>T254 — 이 판에서 이미 쓴 부활 횟수(<see cref="KkomaKnight.Core.Revive.PerRun"/> 까지). 새 판마다 0 으로 돌아간다.</summary>
        int _revivesUsed;
        /// <summary>이 판에서 쓴 부활 횟수(자가 읽는다).</summary>
        public int RevivesUsed => _revivesUsed;

        /// <summary>
        /// T254 2항 — 부활권 1 을 쓰고 <b>그 자리에서 이어서</b> 진행한다(주인 «HP·실드 가득 · 그 자리에서»).
        /// 규칙·되살리기는 <see cref="KkomaKnight.Core.Revive"/> 한 곳이고, 여기서는 «팝업을 닫고 판을 다시 굴린다» 만 한다.
        /// </summary>
        void ReviveNow()
        {
            if (G == null) return;
            if (!KkomaKnight.Core.Revive.Use(App.Save, G, ref _revivesUsed)) return;
            App.Persist();                 // 저장은 여기 한 번(Revive 는 순수 C# 이라 디스크를 안 만진다)
            _ended = false; _overWait = 0; // 판을 다시 굴린다 — «끝났다» 표식만 내린다
            App.Overlay.Close();
            RefreshHud();
            Debug.Log("[T254] 부활 — 남은 부활권 " + App.Save.Revive + " · 이 판 " + _revivesUsed + "/" + KkomaKnight.Core.Revive.PerRun);
        }

        /// <summary>지금 판이 아레나 판인가(자가 읽는다).</summary>
        public bool IsArena => _arenaFoe != null;
        /// <summary>테스트·진단용 읽기 — 이 판이 들어온 던전 키(<c>null</c> = 일반 전투).</summary>
        public string DungeonKey => _dunKey;
        /// <summary>판이 끝나 화면을 뜨는 길 한 곳 — 클리어·사망·포기 셋이 모두 여기를 지난다(«로비로» 를 네 군데에 박아 두지 않는다).</summary>
        void ExitBattle()
        {
            App.Overlay.Close();
            if (_exitPage != null) EventsScreen.Open(App, _exitPage);
            else App.ShowScreen("lobby");
        }

        /// <summary>
        /// T241 — 판을 나간 <b>뒤에</b> «무엇을 받았는지» 를 공통 리워드 팝업으로 보여 준다(던전 클리어 보상).
        /// <para>
        /// <b>왜 나간 뒤인가</b> — 지급은 판이 끝나는 순간 이미 끝났고(T243 «즉시 지급»), 이 팝업은 <b>보여 주는 것</b>이다.
        /// 클리어 팝업(«그냥 받기»·«광고 ×2»)이 떠 있는 위에 겹쳐 띄우면 두 팝업이 한 층을 다투고, 뒤엣것이 앞엣것의 트윈을 죽인다(<c>RewardPopup.Show</c> 첫 줄이 <c>Overlay.Close</c> 다).
        /// 나간 자리(던전 페이지)는 <b>화면</b>이라 <c>onClose</c> 도 필요 없다(결정 701).
        /// </para>
        /// 보상이 없으면(표가 비었거나 던전 판이 아니면) 그냥 나가기만 한다 — <b>얻은 게 없는데 뜨는 팝업은 금지</b>다.
        /// </summary>
        void ExitBattleWithPrize(DungeonData.Reward prize)
        {
            ExitBattle();
            if (prize == null || !prize.Any) return;
            // T291 — 칸을 여기서 세지 않는다: 소탕 쪽과 셈이 갈리면 한쪽만 새 보상(레시피·키)을 빠뜨린다(실제로 그랬다).
            RewardPopup.Show(RewardPopup.ItemsOf(prize));
        }
        void EndAndExit()
        {
            if (G != null && !_ended) { _ended = true; FlushQuestKills(); App.Save.Gold += Math.Round(G.Gold); App.Persist(); }
            ExitBattle();
        }

        /// <summary>
        /// T257 4항 — 이 판에서 죽인 적을 퀘스트에 <b>판이 끝날 때 한 번에</b> 넘긴다(«적 50개 죽이기» · 주간 «2,500»).
        /// <para>
        /// 틱마다 넘기지 않는 까닭: <see cref="Quests.Bump"/> 는 셀 때마다 세이브를 쓴다. 한 판에 수십 번 죽는 자리라
        /// 그대로 걸면 <b>죽을 때마다 세이브 전체를 직렬화</b>하게 된다(WebGL 에선 그것이 곧 프레임이다).
        /// 골드도 판이 끝나야 은행에 들어가는 게임이라(«판을 버리면 골드도 없다» · <see cref="Abort"/>) 셈이 같은 결을 탄다.
        /// </para>
        /// 클리어·사망(<see cref="EndRun"/>)·포기(<see cref="EndAndExit"/>) 셋에서 부르고, 두 번 불려도 넘긴 몫만큼은 다시 안 센다.
        /// </summary>
        void FlushQuestKills()
        {
            if (G == null) return;
            int add = G.Kills - _questKills;
            if (add <= 0) return;
            _questKills = G.Kills;
            Quests.Bump(App, Quests.Kill, add);
        }
        /// <summary>판을 버린다(T29 «데이터 삭제» — 골드를 은행에 넣지 않는다 · 로비 전환은 호출자가). 전투 중이 아니면 아무 일 없음.</summary>
        public void Abort()
        {
            if (G == null) return;
            _ended = true; G = null; _paused = false;
            _world?.Dispose(); _world = null; UiKit.Clear(_pops); _orbs?.Clear(); _flyGold = _flyExp = 0;
        }
        void OnPause()
        {
            if (G == null || G.Over || App.Overlay.IsOpen) return;
            _paused = true;
            App.Overlay.Pause(() => _paused = false, () => { _paused = false; EndAndExit(); });
        }

        // ───────────────────────── 매 프레임 ─────────────────────────
        float _lastReal; const float CatchUpMaxSec = 600f;   // 탭이 숨겨져(브라우저 rAF 정지·앱 백그라운드) 멈춘 시간을 돌아올 때 따라잡는 상한(10분)
        public override void Tick(float dt)
        {
            if (G == null || _world == null) return;
            // 백그라운드 따라잡기 — runInBackground 로도 못 도는 경우(탭 숨김·앱 백그라운드)엔 실제 시간 공백만큼 틱을 몰아서 돈다(연출 없이 · 팝업이 뜨면 거기서 멈춘다)
            float now = Time.realtimeSinceStartup; float gap = _lastReal > 0 ? now - _lastReal - dt : 0; _lastReal = now;
            int maxTicks = 8; bool catchUp = false;
            if (gap > 1f && !App.Overlay.IsOpen && !_paused && !G.Over) { float add = Mathf.Min(gap, CatchUpMaxSec); _acc += add * _speed; maxTicks += Mathf.CeilToInt(add * _speed / (float)EngineConst.Dt); catchUp = true; _world.Silent = true; }
            if (!App.Overlay.IsOpen && !_paused && !G.Over)
            {
                _acc += dt * _speed;
                int guard = 0;
                while (_acc >= EngineConst.Dt && guard++ < maxTicks)
                {
                    // 팝업(레벨업·이벤트)은 남은 타격 연출(칼이 내려오는 순간)이 끝난 뒤 연다 — 그 동안 엔진 시간은 멈춘 채 애니만 돈다
                    if (G.Pending != null) { if (!_world.Busy && !Absorbing) OpenPending(); _acc = 0; break; }
                    // 킬 연출(칼 내려옴 → 적 사망 → 플레이어 공격 모션 끝) 동안 엔진 틱 보류(T50) — 틱 순서 불변 · 풀리면 격차 없이 원래 걷기 속도로 출발.
                    // ⚑ T312 회차 2(주인 «도끼가 적에 닿았는데 바로 안 없어지고 데미지도 늦다») — **보류 중에도 «투사체만» 은 나아가고 맞는다.**
                    //   여태는 보류가 엔진 전체를 세웠는데 투사체 **그림**은 T86 ⓐ 로 계속 날아가서, 도끼가 맞는 자리(`ProjLimit`)에 **닿은 채로 서서**
                    //   보류가 풀리기를 기다렸다 — 주인이 본 «닿았는데 안 사라지고 늦다» 가 그것이다. 적을 연달아 잡을수록 보류가 잦아 더 자주 보인다.
                    //   ⚠ **회차 1 이 이 한 줄을 넣었다가 되돌린 까닭은 «계약 둘» 이었다**(런 795): 자가 시간을 «보류 프레임은 안 센다» 로 재고 있었고,
                    //     또 다른 자가 «도끼 표적이 살아 있다» 를 전투 진행에 기대고 있었다. 이번 회차는 **그 둘을 같이 고쳤다**(자 쪽 · `BattleWorldTests`) —
                    //     계약이 틀린 것이 아니라 «투사체 시계» 가 이제 보류 프레임에도 흐르므로 그것을 재는 스톱워치도 같이 흘러야 한다.
                    //   틱 순서·시뮬 동일성: `StepProjectiles` 는 `Tick` 안의 그 자리를 그대로 떼어 낸 것이고 헤드리스(`RunToEnd`)에는 보류가 없어 골든이 안 바뀐다.
                    //   나아가는 양은 **버리려던 그 시간(`_acc`)** 이다 — 종전에는 보류 프레임에서 `_acc = 0` 으로 **그냥 버렸다**.
                    //   그것을 투사체에 주면 «엔진이 돌았다면 흘렀을 만큼» 과 같아지고(배속·프레임 길이 그대로), 자의 스톱워치(`Time.deltaTime × 배속`)와도 같은 시계가 된다.
                    if (_world.HoldEngine) { G.StepProjectiles(_acc); _acc = 0; break; }
                    _world.BeforeTick(); G.Tick(); _world.AfterTick();
                    _acc -= EngineConst.Dt;
                    if (G.Pending != null) { if (!_world.Busy && !Absorbing) OpenPending(); _acc = 0; break; }
                    if (G.Over) break;
                }
                if (G.Pending == null && G.PendingLevelUps > 0 && !G.Over) { /* 엔진이 다음 틱에 스스로 연다 */ }
            }
            if (catchUp) { _world.Silent = false; _acc = Math.Min(_acc, EngineConst.Dt); }
            foreach (var ev in G.Events) _world.Handle(ev);   // AfterTick 이 틱마다 비우므로 보통 비어 있다
            G.Events.Clear();
            _world.TimeScale = _speed;
            // T86 ⓐ — 엔진 시간이 «흐르는» 프레임인가(팝업·일시정지·판 종료면 아니다). 킬 연출로 틱만 보류된 프레임(HoldEngine)은 «흐르는 중» 이라 투사체가 계속 난다.
            _world.EngineRunning = !App.Overlay.IsOpen && !_paused && !G.Over;
            // 표시 투사체도 같은 배속으로(T108 · 스냅 없이 엔진과 붙어 간다)
            _world.Speed = _speed;
            _world.Sync(dt * _speed);
            AbsorbTick(dt * _speed);   // T85 — 구슬이 도착한 만큼 표시 숫자·바가 차오른다(엔진 값 불변)
            RefreshHud();
            // 사망·클리어 팝업도 흡수가 끝난 뒤에 — 다만 무한 대기 금지(AbsorbMaxWaitSec 넘으면 남은 값을 즉시 적립하고 연다)
            if (G.Over && !_ended && !App.Overlay.IsOpen && !_world.Busy)
            {
                if (Absorbing && _overWait < AbsorbMaxWaitSec) _overWait += dt;
                else { if (Absorbing) FinishAbsorb(); EndRun(); }
            }
        }

        void OpenPending()
        {
            var p = G.Pending; if (p == null) return;
            if (Absorbing) return;   // T85 — 바가 다 찬 «뒤에» 연다(주인 지시 · 여러 레벨이면 차고 → 팝업 → 다시 차고 → 팝업)
            switch (p.Kind)
            {
                case PendingKind.LevelUp: App.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick)); break;
                case PendingKind.Rest: App.Overlay.Rest(G, heal => G.ResolveRest(heal), () => G.ResolveRestBoth()); break;   // T23 — «광고 보고 둘 다 얻기»
                case PendingKind.Devil:
                {
                    var perk = p.DevilPerk;
                    App.Overlay.Devil(G, accept => { G.ResolveDevil(accept); if (accept) App.Overlay.DevilGift(perk, null); });
                    break;
                }
                case PendingKind.Angel: App.Overlay.Angel(G, mult => G.ResolveAngel(mult)); break;
            }
        }

        void EndRun()
        {
            _ended = true;
            FlushQuestKills();   // T257 — 아레나 갈래로 갈라지기 «전에»: 죽인 것은 어느 판에서든 죽인 것이다(표도 판 종류를 안 따진다)
            var D = App.Data; var S = App.Save;
            if (_arenaFoe != null) { EndArenaRun(D, S); return; }   // T240 — 아레나 판은 챕터 진행·클리어 보상이 아니라 «승점» 이 결과다
            // T137 — 챕터 보상 진행도(적 1/3·2/3·전멸)는 «이기든 지든» 여기 한 곳에서 남는다: max(기존, 이번 판 처치)
            ChapterChest.RecordKills(S, G.Chapter, G.Kills);
            if (G.Cleared)
            {
                double bonus = Math.Round(D.Tune.GoldClear(G.Chapter));   // index.html openClear: 클리어 보너스 = TUNE.goldClear(chapter)
                G.Gold += bonus;
                bool last = G.Chapter >= D.Tune.MaxChapter;
                int next = Math.Min(G.Chapter + 1, D.Tune.MaxChapter);
                S.MaxChapter = Math.Min(Math.Max(S.MaxChapter, G.Chapter + 1), D.Tune.MaxChapter);
                // T228 ⓓ — 던전에서 들어온 판을 «깼다» 고 남긴다. 이것 하나가 소탕의 조건이다(주인 «도전을 해서 클리어를 했었던 챕터만 소탕이 가능한 건데»).
                // ⚑ T291 — **그 날이 왔다**: 층이 있는 던전(표에 `floors` 가 있다 · 원정)은 `GrantClear` 가 안에서 `Challenge`(= 최고층 + 1)를 받아
                //    그 층 보상을 주고 그 층을 기록한다. 층이 없는 던전(지옥의 문)은 `Challenge` 가 늘 1 이라 예전과 한 치도 안 다르다.
                //    ⚠ 아래 `Record(…, 1)` 은 그대로 둔다 — 표가 비어 `GrantClear` 가 일찍 돌아가도 «깬 적 있다» 는 남아야 하고,
                //    `Record` 는 «올라가기만» 하므로 층이 이미 더 높으면 아무 일도 안 한다(무해한 두 번째 호출).
                // T241 — **던전 판을 깼으면 표(dungeon.json)의 클리어 보상을 준다**(첫 클리어면 first · 그 뒤 clear).
                //   여태 이 자리는 «깼다» 만 남기고 보상은 아무도 안 줬다 — 세부 팝업(21)이 그 표를 보여 주기만 했다.
                //   판정·지급·기록을 GrantClear 한 곳이 순서대로 한다(«첫» 은 기록 전에 물어야 한다).
                var dunPrize = _dunKey != null ? DungeonSweep.GrantClear(S, D.Dungeon, _dunKey) : null;
                if (_dunKey != null) DungeonSweep.Record(S, _dunKey, 1);   // 표가 없거나 비어도 «깬 적 있다» 는 남는다(소탕의 조건 · GrantClear 가 이미 남겼으면 무해한 두 번째 호출이다)
                // T257 4항 — 주간 «던전 클리어 20회». 던전에서 들어온 판을 «깬» 이 한 자리가 그 사건이다(일반 챕터 클리어는 아니다 · T241 의 지급과 같은 조건을 본다).
                if (!string.IsNullOrEmpty(_dunKey)) Quests.Bump(App, Quests.DungeonClear);
                S.SelChapter = next; S.Gold += Math.Round(G.Gold); App.Persist();   // 1배는 여기서 은행에(«그냥 받기» = 이대로 로비로)
                // T23 — «광고 보고 보상 ×2 받기» = 광고 카운트다운 뒤 이 판의 골드(처치 + 클리어 보너스)를 한 번 더 지급 → 2배 · 로비로. «다음 챕터» 는 로비의 챕터 화살표(SelChapter = next 로 이미 맞춰 둠).
                App.Overlay.Clear(G, last,
                    () => { S.Gold += Math.Round(G.Gold); App.Persist(); App.Toast($"광고 보상 ×2 · +{UiKit.Fmt(Math.Round(G.Gold))} G"); ExitBattleWithPrize(dunPrize); },
                    () => ExitBattleWithPrize(dunPrize));
            }
            else
            {
                S.Gold += Math.Round(G.Gold); App.Persist();
                // T254 — 부활권 선택지. «이 판에서 아직 쓸 수 있는가» 는 개수와 따로다 —
                //   0 개여도 그 자리는 보여 준다(비활성 + 어디서 구하는지). 이미 한 번 썼으면 자리 자체를 안 낸다.
                bool canRevive = _revivesUsed < KkomaKnight.Core.Revive.PerRun;
                App.Overlay.Dead(G, () => ExitBattle(), ReviveNow, S.Revive, canRevive);
            }
        }

        /// <summary>
        /// T240 4·5항 — <b>아레나 판</b>이 끝났다: 승점·순위를 옮기고(<see cref="ArenaMatch.Settle"/>) 결과 화면(<see cref="ArenaResult"/>)을 띄운다.
        /// <para>
        /// <b>일반 전투와 갈리는 것 셋</b> — ⓐ <c>MaxChapter</c>·<c>SelChapter</c> 를 <b>안 건드린다</b>(아레나에서 이겨도 챕터가 열리면 안 된다) ·
        /// ⓑ 클리어 보너스 골드를 <b>안 준다</b>(그 보상은 챕터 진행의 몫이다) · ⓒ <see cref="DungeonSweep.Record"/> 도 안 부른다(던전이 아니다).
        /// 판에서 주운 골드(<c>G.Gold</c>)는 그대로 은행에 넣는다 — 그것은 «판을 돈 삯» 이라 어느 판이든 같다.
        /// </para>
        /// <para>⚠ <b>«이겼는가» = <c>G.Cleared</c></b> 다. 지금 아레나 판은 «지금 고른 챕터를 도는 판»(T183 던전과 같은 꼴)이라
        /// 전멸시키면 이기고 죽으면 진다. 1·2항(콜로세움 무대 · 양쪽 플레이어 1대1)이 서면 그때 «누가 이겼나» 도 그 규칙이 정한다.</para>
        /// </summary>
        void EndArenaRun(GameData D, SaveData S)
        {
            ChapterChest.RecordKills(S, G.Chapter, G.Kills);   // 처치 진행도는 «이기든 지든» 남는다(T137) — 판을 돈 것은 사실이다
            S.Gold += Math.Round(G.Gold);
            var o = ArenaMatch.Settle(S, D != null ? D.ArenaMatch : null, D != null ? D.ArenaDummy : null, G.Cleared);
            App.Persist();                                     // 저장은 여기 한 번뿐이다(Settle 은 순수 C# 이라 디스크를 안 만진다)
            Debug.Log($"[T240] 아레나 결과 {(o.Win ? "승" : "패")} · 승점 {o.Before:0} → {o.After:0}({o.Delta:+0;-0;0}) · 순위 {o.RankBefore} → {o.RankAfter} · 티어 {o.Tier}");
            // T262 3항 — 34 도 33·23·24 와 같은 얼굴이어야 한다(내 것은 프로필에서 고른 것 · 상대는 그 순위의 더미)
            ArenaResult.Show(o, Nickname.Of(S), _arenaFoe, Profile.CurrentIcon(S), Profile.DummyIcon(_arenaFoeRank), ExitBattle);
        }

        // ───────────────────────── T85 · 보상 흡수(표시값) ─────────────────────────
        /// <summary>누적 경험치(레벨 1부터) — 표시값이 레벨 경계를 넘어가며 차오를 수 있게 «늘기만 하는 한 수» 로 본다. 엔진 값(P.Exp·P.Level)은 읽기만 한다.</summary>
        public static double ExpTotal(BattleState g, GameData d)
        {
            if (g == null || d == null) return 0;
            double t = g.P.Exp;
            for (int lv = 1; lv < g.P.Level; lv++) t += d.Tune.ExpNeed(lv);
            return t;
        }
        /// <summary>누적 경험치 → (이 레벨에서 찬 양 · 필요량). 흡수가 끝나면 엔진 (P.Exp · ExpNeed(P.Level)) 과 같은 값이 나온다.</summary>
        public static void ExpBar(double total, GameData d, out double cur, out int need)
        {
            cur = total; need = d != null ? d.Tune.ExpNeed(1) : 0;
            if (d == null) return;
            for (int lv = 1; lv < 9999; lv++)
            {
                need = d.Tune.ExpNeed(lv);
                if (need <= 0 || cur < need) return;
                cur -= need;
            }
        }
        /// <summary>보상 흡수가 진행 중인가 — 구슬이 날고 있거나 표시 숫자·바가 아직 차는 중. 레벨업·사망·클리어 팝업은 이것이 끝난 뒤에 연다(주인 «다 차고 나서»).</summary>
        public bool Absorbing => (_orbs != null && _orbs.Busy) || _flyGold > 1e-4 || _flyExp > 1e-4
            || _goldTarget - _shownGold > 1e-4 || _expTarget - _shownExp > 1e-4
            || (G != null && (G.Gold - (_goldTarget + _flyGold) > 1e-4 || ExpTotal(G, App.Data) - (_expTarget + _flyExp) > 1e-4));   // 엔진이 이미 준 값이 아직 화면에 안 올라온 구간(칼이 안 내려온 킬)도 «차는 중» 이다
        /// <summary>표시 골드 · 표시 누적 경험치(테스트·진단용 읽기).</summary>
        public double ShownGold => _shownGold; public double ShownExp => _shownExp;
        /// <summary>날아가는 중인 구슬 수(테스트·진단용 읽기).</summary>
        public int OrbCount => _orbs != null ? _orbs.Alive : 0;

        /// <summary>표시값을 엔진 값으로 즉시 맞춘다(새 판 · 화면 재진입 · 탭 복귀 따라잡기).</summary>
        void SnapShown()
        {
            _orbs?.Clear();
            _flyGold = _flyExp = 0; _goldRate = _expRate = 0; _overWait = 0;
            _shownGold = _goldTarget = G != null ? G.Gold : 0;
            _shownExp = _expTarget = G != null ? ExpTotal(G, App.Data) : 0;
        }
        /// <summary>남은 구슬을 즉시 도착시키고 카운트업도 끝낸다 — 사망·클리어 팝업이 0.6초 넘게 기다리지 않게.</summary>
        void FinishAbsorb()
        {
            _orbs?.FinishNow();
            if (G != null) { _goldTarget = G.Gold; _expTarget = ExpTotal(G, App.Data); }
            _shownGold = _goldTarget; _shownExp = _expTarget; _flyGold = _flyExp = 0; _goldRate = _expRate = 0;
        }
        /// <summary>적의 사망 연출이 시작된 순간(<see cref="BattleWorld.KillShown"/>) — 그 자리에서 경험치 구슬·골드 코인이 튀어나와 EXP 바·골드 pill 로 날아간다.</summary>
        void OnKillShown(Vector3 worldPos, bool boss)
        {
            if (G == null || _orbs == null) return;
            var from = WorldCam.ToFrame(worldPos);
            // 화면 밖에서 죽은 적(전투는 스크롤한다)은 구슬 없이 표시값만 바로 올린다
            bool onScreen = from.x > -OrbSizePx && from.x < UiKit.FrameW + OrbSizePx && from.y > -OrbSizePx && from.y < UiKit.FrameH + OrbSizePx;
            int n = boss ? OrbBossCount : OrbMinCount + G.Kills % 3;   // 3~5개(보스 8) · 화면 동시 상한은 RewardOrbs.MaxAlive
            double expGap = ExpTotal(G, App.Data) - (_expTarget + _flyExp);
            if (expGap > 1e-4)
            {
                int made = onScreen ? _orbs.Fly(from, _exp.Root, "pi.orb", Palette.Green, n, expGap, OrbSizePx, _speed, OnExpArrive) : 0;
                if (made > 0) _flyExp += expGap; else _expTarget += expGap;
            }
            double goldGap = G.Gold - (_goldTarget + _flyGold);
            if (goldGap > 1e-4)
            {
                int made = onScreen ? _orbs.Fly(from, _goldPill, "ui.coin", Palette.White, n, goldGap, OrbSizePx, _speed, OnGoldArrive) : 0;
                if (made > 0) _flyGold += goldGap; else _goldTarget += goldGap;
            }
        }
        void OnExpArrive(double v) { _flyExp = Math.Max(0, _flyExp - v); _expTarget += v; }
        void OnGoldArrive(double v) { _flyGold = Math.Max(0, _flyGold - v); _goldTarget += v; }

        /// <summary>구슬이 도착한 만큼 표시 숫자·바를 <see cref="CountUpSec"/> 안에 따라 올린다 — 표시값은 엔진 값을 넘지 않는다.</summary>
        void AbsorbTick(float dt)
        {
            if (G == null) return;
            double engineExp = ExpTotal(G, App.Data);
            if (_world != null && _world.Silent) { SnapShown(); return; }   // 탭 복귀 따라잡기는 즉시 맞춘다(T50 SnapGap 감각)
            // 구슬이 안 붙은 증가(화면 밖 킬 · 클리어 보너스)는 그대로 카운트업 — 단 «칼이 아직 안 내려온» 킬의 몫은 구슬이 가져가게 기다린다
            if (_world == null || !_world.KillPending)
            {
                double g = G.Gold - (_goldTarget + _flyGold); if (g > 1e-4) _goldTarget += g;
                double e = engineExp - (_expTarget + _flyExp); if (e > 1e-4) _expTarget += e;
            }
            if (_goldTarget > G.Gold + 1e-4) { _goldTarget = G.Gold; if (_shownGold > _goldTarget) _shownGold = _goldTarget; }
            if (_expTarget > engineExp + 1e-4) { _expTarget = engineExp; if (_shownExp > _expTarget) _shownExp = _expTarget; }
            CountUp(ref _shownGold, ref _goldRate, _goldTarget, dt);
            CountUp(ref _shownExp, ref _expRate, _expTarget, dt);
        }
        /// <summary>남은 차이를 <see cref="CountUpSec"/> 안에 메운다 — 도착이 겹쳐 목표가 늘면 그만큼 빨라진다(도착 순서대로 «차오름» 이 이어진다).</summary>
        static void CountUp(ref double cur, ref double rate, double target, float dt)
        {
            double d = target - cur;
            if (d <= 1e-9) { cur = target; rate = 0; return; }
            double need = d / CountUpSec;
            if (need > rate) rate = need;
            cur += rate * dt;
            if (cur >= target) { cur = target; rate = 0; }
        }

        // ───────────────────────── HUD ─────────────────────────
        void RefreshHud()
        {
            if (G == null) return;
            var P = G.P; var D = App.Data;
            if (_gold != null) _gold.text = UiKit.Fmt(_shownGold);   // T85 — 표시 골드(구슬이 도착한 만큼) · 은행에 넣는 값은 엔진 G.Gold 그대로
            if (_kills != null) _kills.text = G.Kills.ToString();
            if (_chapTitle != null) _chapTitle.text = $"챕터 {G.Chapter}";
            // 진행바(T35) = 노드(웨이브·이벤트·보스) 진행 — 끝난 노드 수 + 지금 싸우는 웨이브의 처치 비율 → 적을 잡을수록 찬다 · 적 조우 중엔 주황, 걷는 중엔 노랑(레퍼런스 03 «적 발견»)
            _prog.Set(ChapterProgress(G), null);
            if (_progFill != null) _progFill.color = _world != null && _world.Engaged ? Palette.Orange : Palette.Yellow;
            if (_speedTxt != null) _speedTxt.text = "x" + _speed;
            // T85 — EXP 바도 «표시 누적 경험치» 로 그린다(흡수가 끝나면 엔진 값과 정확히 같다 · 레벨 경계를 넘으면 차고 → 팝업 → 다시 찬다)
            ExpBar(_shownExp, D, out double expCur, out int need);
            _exp.Set(need > 0 ? expCur / need : 0, $"{(long)Math.Floor(expCur)}/{need}");
            double hp = _world != null ? _world.ShownHp : P.Hp, sh = _world != null ? _world.ShownSh : P.Sh;   // 표시 체력 — 칼이 내려온 순간에 깎인다
            _hp.Set(P.MaxHp > 0 ? hp / P.MaxHp : 0, $"{UiKit.Fmt(hp)}/{UiKit.Fmt(P.MaxHp)}");
            _sh.Set(P.MaxSh > 0 ? sh / P.MaxSh : 0, P.MaxSh > 0 ? $"{UiKit.Fmt(sh)}/{UiKit.Fmt(P.MaxSh)}" : "실드 없음");
            for (int i = 0; i < StatDefs.Length; i++) { var d = StatDefs[i]; _statVals[i].text = d.Fmt(G); _statVals[i].color = d.Up(G, BaseStats) ? Palette.Green : Palette.White; }
            RefreshPerkStrip(); RefreshBuffBar();
        }
        /// <summary>챕터 진행 0~1 — 끝난 노드(웨이브/보스 = 적 전멸 · 이벤트 = Done) 수 + 지금 싸우는 첫 미완 웨이브의 처치 비율, ÷ 노드 수. 엔진 값만 읽는다(테스트가 같은 식으로 검산).</summary>
        public static double ChapterProgress(BattleState G)
        {
            if (G == null || G.Nodes.Count == 0) return 0;
            int total = G.Nodes.Count, done = 0; double frac = 0; bool curFound = false;
            foreach (var n in G.Nodes)
            {
                bool fight = n.Type == NodeType.Wave || n.Type == NodeType.Boss;
                bool finished = fight ? n.Enemies.Count > 0 && n.Enemies.TrueForAll(e => e.Dead) : n.Done;
                if (finished) { done++; continue; }
                if (!curFound && fight && n.Enemies.Count > 0) { curFound = true; int dead = 0; foreach (var e in n.Enemies) if (e.Dead) dead++; frac = (double)dead / n.Enemies.Count; }
            }
            return Math.Min(1, (done + frac) / total);
        }
        void RefreshPerkStrip()
        {
            // 얻은 순서대로 · 중복은 ×N · 넘치면 +N (index.html 주인 지시 ②)
            var order = new List<string>(); var count = new Dictionary<string, int>();
            foreach (var p in G.Taken) { if (!count.ContainsKey(p.Id)) { count[p.Id] = 0; order.Add(p.Id); } count[p.Id]++; }
            if (_perkCount != null) _perkCount.text = G.Taken.Count.ToString();
            // 비례(T13) = index.html #perkStrip CSS 를 줄의 «실제» 높이·폭에서 계산(Layout.PerkStripSpec · 픽셀 상수 없음). 줄 크기가 바뀌면(첫 프레임 → 레이아웃 뒤) 키가 달라져 다시 그린다.
            var m = PerkStripMetrics(_perkStrip);
            string key = string.Join(",", order) + "|" + G.Taken.Count + "|" + Mathf.RoundToInt(m.Width) + "x" + Mathf.RoundToInt(m.Height);
            if (key == _perkStripKey) return; _perkStripKey = key;
            UiKit.Clear(_perkStrip);
            if (_perkStripLayout != null) _perkStripLayout.spacing = m.Gap;
            int shown = m.Shown(order.Count);   // 줄 폭 ÷ (셀+간격) · «+N» 까지 포함해 넘치지 않는 개수(상수 11 폐기)
            for (int i = 0; i < shown; i++)
            {
                string id = order[i];
                var perk = App.Data.Perks.Perks.Find(x => x.Id == id); if (perk == null) continue;
                // 팔각 등급 프레임(ItemFrame_04_* · 특전 카드와 같은 모양) — 주인 지시 2026-09-05. 프레임은 프리팹 본래 크기를 배율로 줄인다(UiKit.PerkFrame) — 셀 밖으로 안 나간다.
                var cell = UiKit.Rect(_perkStrip, id); cell.sizeDelta = new Vector2(m.Cell, m.Cell);
                UiKit.PerkFrame(cell, Palette.PerkGradeName(perk.Grade), Icons.Perk(id), m.Cell);
                if (count[id] > 1)
                {
                    // 개수 배지 — 오른쪽 위 모서리(.pv-ic .cnt · 14/34). 셀 안쪽 모서리에 두어 이웃 셀·줄 밖으로 안 나간다.
                    var n = UiKit.Text(cell, count[id].ToString(), (int)m.BadgeFont, Palette.White, kind: TextKind.Small);   // 아이콘 위 개수 배지 = 지시서 T63 의 «정말 작아야 하는 배지»(14/34 비례 그대로)
                    var nr = n.rectTransform; nr.anchorMin = nr.anchorMax = new Vector2(1f, 1f); nr.pivot = new Vector2(1f, 1f); nr.anchoredPosition = Vector2.zero; nr.sizeDelta = new Vector2(m.Badge, m.Badge);
                    n.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }
            if (shown < order.Count)
            {
                var more = UiKit.Text(_perkStrip, "+" + (order.Count - shown), (int)m.Font, Palette.CreamDark, kind: TextKind.Aux);   // m.Font 는 이미 보조 하한(36) 이상(PerkStripSpec)
                more.rectTransform.sizeDelta = new Vector2(m.MoreWidth(order.Count - shown), m.Cell); more.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        /// <summary>특전 줄 치수 — 폭·높이는 실제 rect 에서(레이아웃 전이면 화면 루트 → 프레임 상수 순으로 대체) · 나머지는 <see cref="Layout.PerkStripSpec"/> 비례.</summary>
        public static Layout.PerkStripSpec PerkStripMetrics(RectTransform strip)
        {
            float w = strip != null ? strip.rect.width : 0, h = strip != null ? strip.rect.height : 0;
            if (w <= 1f || h <= 1f)
            {
                var parent = strip != null ? strip.parent as RectTransform : null;
                float pw = parent != null && parent.rect.width > 1f ? parent.rect.width : UiKit.FrameW, ph = parent != null && parent.rect.height > 1f ? parent.rect.height : UiKit.FrameH;
                w = pw * Layout.HudPerkStrip.W / 100f; h = ph * Layout.HudPerkStrip.H / 100f;
            }
            return new Layout.PerkStripSpec(w, h);
        }

        void RefreshBuffBar()
        {
            // 발동 중 버프 — 출처(특전) 별 묶음 · 등급색 테두리 + 중첩 수 (index.html renderBuffBar)
            var groups = new List<KeyValuePair<string, int>>(); var idx = new Dictionary<string, int>();
            foreach (var kv in G.P.Buffs) foreach (var b in kv.Value)
            {
                string id = b.Tag ?? ("#" + kv.Key);
                if (idx.TryGetValue(id, out var i)) groups[i] = new KeyValuePair<string, int>(id, groups[i].Value + 1);
                else { idx[id] = groups.Count; groups.Add(new KeyValuePair<string, int>(id, 1)); }
            }
            var sb = new System.Text.StringBuilder(); foreach (var g in groups) sb.Append(g.Key).Append(':').Append(g.Value).Append(',');
            string key = sb.ToString(); if (key == _buffKey) return; _buffKey = key;
            UiKit.Clear(_buffBar);
            foreach (var g in groups)
            {
                var perk = App.Data.Perks.Perks.Find(x => x.Id == g.Key);
                var cell = UiKit.Rect(_buffBar, g.Key); cell.sizeDelta = new Vector2(88, 88);
                // 칸 = 특전과 같은 팔각 등급 프레임(ItemFrame_04_* · UiKit.PerkFrame · 배율로 셀에 맞춤 · 특전 없는 버프는 gray) — ui.buffSlot(엉뚱한 프레임) 대신(T20 · 주인 지시). 아이콘은 프레임 안 Icon 자식에.
                string icon = perk != null ? Icons.Perk(perk.Id) : Icons.Stat(g.Key.TrimStart('#') == "atk" ? "dmg" : g.Key.TrimStart('#'));
                UiKit.PerkFrame(cell, perk != null ? Palette.PerkGradeName(perk.Grade) : "gray", icon, cell.sizeDelta.x);
                // 스택 수는 그대로 오른쪽 아래(프레임 위에 그려지도록 뒤에 만든다)
                if (g.Value > 1) { var n = UiKit.Text(cell, g.Value.ToString(), TextSize.Aux, Palette.White, kind: TextKind.Aux); UiKit.Pct(n.rectTransform, 45, 45, 55, 55); n.textWrappingMode = TextWrappingModes.NoWrap; }   // 중첩 수 = 보조 36(전 24) · 칸 48px(T63-battle)
            }
        }
    }
}
