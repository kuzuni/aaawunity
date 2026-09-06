using System;
using System.Collections.Generic;
using KkomaKnight.Core;
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
        Text _kills, _gold, _chapTitle, _speedTxt;
        UiKit.Bar _prog, _exp, _hp, _sh; Image _progFill;
        RectTransform _buffBar, _perkStrip; Text _perkCount; HorizontalLayoutGroup _perkStripLayout;
        readonly Text[] _statVals = new Text[StatDefs.Length];
        string _perkStripKey = "", _buffKey = "";
        // T85 — 보상 흡수 연출: 엔진 값(G.Gold · P.Exp)은 킬 순간에 이미 올라 있고(불변), 화면은 «구슬이 도착한 만큼» 만 올린다.
        RectTransform _goldPill, _orbLayer; RewardOrbs _orbs;
        double _shownGold, _shownExp;        // 표시값 — 골드 · 경험치는 «누적»(레벨 경계를 넘으면 바가 다시 찬다)
        double _goldTarget, _expTarget;      // 도착분까지 반영된 목표(카운트업이 여기로 간다)
        double _goldRate, _expRate;          // 카운트업 속도(초당) — 목표가 늘면 다시 계산해 CountUpSec 안에 따라잡는다
        double _flyGold, _flyExp;            // 아직 날아가는 중인 구슬이 들고 있는 값
        float _overWait;                     // 사망·클리어에서 흡수를 기다린 시간(AbsorbMaxWaitSec 넘으면 강제 완료)
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
            _chapTitle = UiKit.SetText(title, "Text (TMP)", "챕터 1", size: UiKit.FontForHeight(Layout.HudChapTitle.H));   // 글자 높이 = 표 2.6%(T47 회차 2 에서 1.5% 로 작았다)
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
                var ct = UiKit.Text(cap.transform, "EXP", 34, Palette.White, TextAnchor.MiddleCenter, false, true); ct.fontStyle = FontStyle.Bold; UiKit.Stretch(ct.rectTransform);
            }
            _hp = UiKit.MakeBar(Root, "ui.sliderRed", "pi.heart"); UiKit.Pct(_hp.Root, Layout.HudHp); _hp.Root.name = "Bar:HP";
            _sh = UiKit.MakeBar(Root, "ui.sliderBlue", "pi.shield"); UiKit.Pct(_sh.Root, Layout.HudSh); _sh.Root.name = "Bar:SH";
            foreach (var b in new[] { _hp, _sh }) if (b.Cap != null) { b.Cap.rectTransform.sizeDelta = new Vector2(84, 84); b.Cap.rectTransform.anchoredPosition = new Vector2(6, 2); }   // 아이콘이 바보다 조금 크게(레퍼런스)
            foreach (var b in new[] { _exp, _hp, _sh }) if (b.Txt != null) { b.Txt.color = Palette.White; b.Txt.fontStyle = FontStyle.Bold; b.Txt.alignment = TextAnchor.MiddleCenter; }
            // T69 8항(주인 «HP·실드 바 테두리») — 세 바에 검은 아웃라인(맨 앞) · 왼쪽 캡(«EXP» 라벨 · ❤ · 🛡)은 테두리 위로 · 바 자리·크기 불변
            foreach (var b in new[] { _exp, _hp, _sh }) { UiKit.Bordered(b.Root); var capT = UiKit.Find(b.Root, "Cap"); if (capT != null) capT.SetAsLastSibling(); }
            // 스탯 8칸 = 칸마다 어두운 상자(ui.frameDark) · 왼쪽 아이콘 · 오른쪽에 이름(보조 36 · 위) + 값(본문 40 · 아래) · 버프 중 값 초록(레퍼런스 02) — 자리 = 표(HudStats · 행 피치 5.2) · 상자 사이 틈 0.4%
            // T63-battle: 칸 높이 4.8%(112px) = 이름 46%(51px ≥ 36×1.375) + 값 52%(58px ≥ 40×1.375) — 전엔 4.6% 칸에 40%/48% 라 bestFit 이 이름을 32 · 값을 37 로 몰래 줄였다(게이트 표엔 안 걸림 · T63 진행 기록 ⚠)
            for (int i = 0; i < StatDefs.Length; i++)
            {
                int col = i % 2, row = i / 2;
                var cell = UiKit.SpawnRt("ui.frameDark", Root, new Layout.R(Layout.HudStats.X + col * Layout.HudStatColR + 0.4f, Layout.HudStats.Y + row * Layout.HudStatRowPitch + 0.2f, Layout.HudStatCellW - 0.8f, Layout.HudStatCellH - 0.4f));
                cell.name = "stat:" + StatDefs[i].Key;
                var ic = UiKit.Icon(cell, "ic", Icons.Stat(StatDefs[i].Key)); UiKit.Pct(ic.rectTransform, 3, 12, 15, 76);
                var lb = UiKit.Label(cell, 21, 1, 76, 46, StatDefs[i].Label, TextSize.Aux, Palette.CreamDark, TextAnchor.MiddleLeft, kind: TextKind.Aux); lb.name = "Label";
                _statVals[i] = UiKit.Label(cell, 21, 47, 76, 52, "", TextSize.Body, Palette.White, TextAnchor.MiddleLeft); _statVals[i].name = "Value"; _statVals[i].fontStyle = FontStyle.Bold;
                // T69 — 스탯 칸마다 검은 아웃라인(어두운 상자 위 · 자리 불변)
                UiKit.Bordered(cell);
            }
            // 보유 특전 = 책 모양 버튼(특전 선택 팝업의 Book 과 같은 그림 · 위에 개수) — 주인 지시 2026-09-05
            var info = UiKit.Rect(Root, "PerkBook"); UiKit.Pct(info, Layout.HudInfo.X - 1, Layout.HudInfo.Y - 1.5f, Layout.HudInfo.W + 2, Layout.HudInfo.H + 3);
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
            // T85 — 보상 구슬 층은 HUD «위» (마지막 형제): 구슬이 하단 패널 안의 EXP 바까지 가려지지 않고 날아가야 한다. 글자·이름표 없음(비평 표·게이트 불변).
            _orbLayer = UiKit.Rect(Root, "Orbs"); UiKit.Stretch(_orbLayer); _orbLayer.SetAsLastSibling();
            _orbs = new RewardOrbs(_orbLayer);
        }

        // ───────────────────────── 시작 · 종료 ─────────────────────────
        public void Start(int chapter)
        {
            var D = App.Data;
            var rng = new Mulberry32((uint)Environment.TickCount ^ 0x9E3779B9u);
            G = new BattleState(D, chapter, App.Save.CurBuild(D), rng, new InteractivePolicy(), new RunOptions { EmitEvents = true });
            BaseStats = new Dictionary<string, double>(); foreach (var d in StatDefs) BaseStats[d.Key] = d.Cur(G);
            _world?.Dispose(); UiKit.Clear(_pops);   // 팝 층은 새 월드를 만들기 «전에» 비운다(발밑 숫자 글자가 팝 층에 산다 · T35)
            _world = new BattleWorld(App, G, _pops);
            _world.KillShown = OnKillShown;   // T85 — 시체가 쓰러지는 순간 그 자리에서 보상 구슬이 튀어나온다
            SnapShown();                      // 새 판은 표시값 = 엔진 값(0)에서 시작
            _acc = 0; _speed = App.Save.Speed; _paused = false; _ended = false; _perkStripKey = ""; _buffKey = ""; _lastReal = 0;   // 배속은 세이브에서(T18 · 클리어 뒤 다음 챕터도 그대로) · 새 판 첫 프레임이 «공백» 으로 잡히지 않게
            Audio.Bgm("bgm.battle");   // 새 판(클리어 뒤 다음 챕터 포함)은 전투 곡부터 — 보스 곡이었으면 되돌린다(T28)
            RefreshHud();
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

        void EndToLobby()
        {
            if (G != null && !_ended) { _ended = true; App.Save.Gold += Math.Round(G.Gold); App.Persist(); }
            App.Overlay.Close();
            App.ShowScreen("lobby");
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
            App.Overlay.Pause(() => _paused = false, () => { _paused = false; EndToLobby(); });
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
                    // 킬 연출(칼 내려옴 → 적 사망 → 플레이어 공격 모션 끝) 동안 엔진 틱 보류(T50) — 틱 순서 불변 · 풀리면 격차 없이 원래 걷기 속도로 출발(누적분은 버려 몰아치기 없음)
                    if (_world.HoldEngine) { _acc = 0; break; }
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
            var D = App.Data; var S = App.Save;
            if (G.Cleared)
            {
                double bonus = Math.Round(D.Tune.GoldClear(G.Chapter));   // index.html openClear: 클리어 보너스 = TUNE.goldClear(chapter)
                G.Gold += bonus;
                bool last = G.Chapter >= D.Tune.MaxChapter;
                int next = Math.Min(G.Chapter + 1, D.Tune.MaxChapter);
                S.MaxChapter = Math.Min(Math.Max(S.MaxChapter, G.Chapter + 1), D.Tune.MaxChapter);
                S.SelChapter = next; S.Gold += Math.Round(G.Gold); App.Persist();   // 1배는 여기서 은행에(«그냥 받기» = 이대로 로비로)
                // T23 — «광고 보고 보상 ×2 받기» = 광고 카운트다운 뒤 이 판의 골드(처치 + 클리어 보너스)를 한 번 더 지급 → 2배 · 로비로. «다음 챕터» 는 로비의 챕터 화살표(SelChapter = next 로 이미 맞춰 둠).
                App.Overlay.Clear(G, last,
                    () => { S.Gold += Math.Round(G.Gold); App.Persist(); App.Toast($"광고 보상 ×2 · +{UiKit.Fmt(Math.Round(G.Gold))} G"); App.Overlay.Close(); App.ShowScreen("lobby"); },
                    () => { App.Overlay.Close(); App.ShowScreen("lobby"); });
            }
            else
            {
                S.Gold += Math.Round(G.Gold); App.Persist();
                App.Overlay.Dead(G, () => { App.Overlay.Close(); App.ShowScreen("lobby"); });
            }
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
                    n.horizontalOverflow = HorizontalWrapMode.Overflow;
                }
            }
            if (shown < order.Count)
            {
                var more = UiKit.Text(_perkStrip, "+" + (order.Count - shown), (int)m.Font, Palette.CreamDark, kind: TextKind.Aux);   // m.Font 는 이미 보조 하한(36) 이상(PerkStripSpec)
                more.rectTransform.sizeDelta = new Vector2(m.MoreWidth(order.Count - shown), m.Cell); more.horizontalOverflow = HorizontalWrapMode.Overflow;
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
                if (g.Value > 1) { var n = UiKit.Text(cell, g.Value.ToString(), TextSize.Aux, Palette.White, kind: TextKind.Aux); UiKit.Pct(n.rectTransform, 45, 45, 55, 55); n.horizontalOverflow = HorizontalWrapMode.Overflow; }   // 중첩 수 = 보조 36(전 24) · 칸 48px(T63-battle)
            }
        }
    }
}
