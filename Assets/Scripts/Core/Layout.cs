namespace KkomaKnight.Core
{
    /// <summary>
    /// aaaw docs/ui/ref-layout.md (사본: docs/ref-layout.md) 의 «자» — 요소별 x/y/w/h (프레임 %). 여기 값이 배치의 단일 정본이고 화면 코드는 이 상수만 쓴다.
    /// 레퍼런스 jpg 1080×2340 · 프레임 390×844 — 둘 다 좌상 0%·우하 100% 로 환산한 값(판독 오차 ±0.5%p). 판정은 ±3%p.
    /// 순수 C#(Core) 에 두는 이유: EditMode 테스트(LayoutSpecTests)가 표의 행과 이 상수를 대조한다.
    /// </summary>
    public static class Layout
    {
        public struct R
        {
            public float X, Y, W, H;
            public R(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
            /// <summary>outer 안에 놓인 inner(둘 다 프레임 %) 를 outer 기준 % 로 — 팝업 상자 안 요소 배치용.</summary>
            public R Within(R outer) => new R((X - outer.X) / outer.W * 100f, (Y - outer.Y) / outer.H * 100f, W / outer.W * 100f, H / outer.H * 100f);
            /// <summary>세로 중심을 지키며 높이만 h 로(글자 하한 때문에 표 칸이 모자랄 때 ±3%p 안에서 키우는 용도 · T63).</summary>
            public R WithH(float h) => new R(X, Y + (H - h) / 2f, W, h);
            public override string ToString() => $"x{X} y{Y} w{W} h{H}";
        }

        // ① 로비 — 메인로비.jpg
        public static readonly R LobbyTopBar = new R(0, 3.7f, 100, 4.5f);
        public static readonly R LobbyAvatar = new R(2.5f, 3.8f, 10.2f, 4.4f);
        public static readonly R LobbyPills = new R(13.2f, 4.5f, 85.4f, 2.9f);
        // T78(주인 2026-09-07) — 이벤트 배너(시즌 패스)는 삭제 · 표 ① 의 그 자리(24.5/9.2/51.6/5.6)는 비워 둔다(다른 요소를 끌어올리지 않는다)
        public static readonly R LobbyMenu = new R(88.3f, 9.2f, 9.0f, 4.1f);
        // 사이드 기둥 · 보조 줄은 T68(아이콘 1.5~1.8배 · 칸 폭 ≥75%) + T63-lobby(라벨 36 · 2줄) 가 레퍼런스(16.4×21.5 · 37.2×7.0)에서 ±3%p 안으로 키운 값 — ref-layout ① «⚑ T68 회차 정정»
        /// <summary>왼쪽 사이드 기둥 — T78(주인 2026-09-07)로 «특권» 1칸만 남아 높이가 3칸(24.0)의 1/3 = 8.0 이다(x·y·폭·칸 크기는 오른쪽 기둥과 같게 유지 · 위 정렬).</summary>
        public static readonly R LobbySideL = new R(1.4f, 16.0f, 19.0f, 8.0f);
        public static readonly R LobbySideR = new R(79.6f, 16.0f, 19.0f, 24.0f);
        public static readonly R LobbyChapTitle = new R(34.7f, 27.2f, 31.3f, 2.3f);
        public static readonly R LobbyChapUnderline = new R(29.6f, 30.0f, 41.0f, 0.9f);
        public static readonly R LobbyCard = new R(27.9f, 41.0f, 44.5f, 13.7f);
        public static readonly R LobbyArrowL = new R(17.0f, 45.7f, 6.7f, 4.3f);
        public static readonly R LobbyArrowR = new R(76.3f, 45.7f, 6.7f, 4.3f);
        public static readonly R LobbySubRow = new R(30.3f, 59.7f, 39.2f, 9.5f);
        public static readonly R LobbyStart = new R(27.9f, 70.7f, 44.5f, 7.0f);
        public static readonly R TabBar = new R(0, 92.6f, 100, 7.4f);
        public const float TabCenterRaise = 0.6f;
        // ① 표 밖 — 01_lobby.jpg 에서 워커가 직접 잰 값 (T34 · 5% 격자 ±3%p). 재화 pill 줄(LobbyPills)을 세 칸으로 나눈 것과 아래 두 모서리 버튼.
        /// <summary>전투력 칸(칼 아이콘 + 주황 큰 숫자) — 아바타 바로 오른쪽(jpg x 14.6~34%).</summary>
        public static readonly R LobbyPower = new R(13.2f, 4.5f, 22.0f, 2.9f);
        /// <summary>골드 pill(jpg x 39.3~68%) · 보석 pill(jpg x 69.4~97.9%).</summary>
        public static readonly R LobbyGoldPill = new R(38.5f, 4.5f, 29.5f, 2.9f);
        public static readonly R LobbyGemPill = new R(69.5f, 4.5f, 29.0f, 2.9f);
        /// <summary>왼쪽 아래 «성»(잠금 · jpg x 0~18% · y 70.5~78%) · 오른쪽 아래 «이벤트»(방패 · x 82~100%) — START 와 같은 줄, 프레임 가장자리에 붙는다.</summary>
        // 모서리 2개(표 밖) — T68 ① 로 17.0×7.5 → 20.0×8.5(아이콘 1.57배 · START 27.9~72.4 와 안 겹침 · 탭 바 92.6 위)
        // T78(주인 2026-09-07) — 왼쪽 아래 «성»(잠금) 삭제 · 그 자리(0/70.0/20.0/8.5)는 비워 둔다
        /// <summary>로비 오른쪽 아래 «이벤트» 자리 — <b>T168 로 그 버튼은 삭제됐다</b>(하단 탭 맨 오른쪽과 중복 · 주인 지시).
        /// 값은 «비워 둔 자리» 를 적어 두려고 남긴다 — 다른 요소를 여기로 끌어올리지 않는다(T78 이 «성» 을 지웠을 때와 같은 규약).</summary>
        public static readonly R LobbyEvents = new R(80.0f, 70.0f, 20.0f, 8.5f);

        // ② 인게임 — 메인 게임화면.jpg
        public static readonly R HudPills = new R(2.0f, 5.2f, 36.0f, 2.8f);
        public static readonly R HudMenu = new R(88.0f, 5.2f, 9.5f, 3.3f);
        public static readonly R HudChapTitle = new R(36.0f, 11.0f, 28.0f, 2.6f);
        public static readonly R HudProgress = new R(22.0f, 14.2f, 56.0f, 1.0f);
        public static readonly R HudBuffBar = new R(2.0f, 17.5f, 14.0f, 30.0f);   // 주인 지시 ① (표 밖 · 챕터 표시 아래 왼쪽)
        public static readonly R GroundBand = new R(0, 30.0f, 100, 21.0f);
        public const float PlayerFootY = 40.0f, EnemyTopY = 31.0f, PlayerTopY = 30.6f, PlayerHeight = 9.0f, EnemyHeight = 9.0f, EnemyHeightBald = 7.5f;
        public const float HpLabelY = 40.5f, HpLabelH = 3.2f;
        public const float PlayerCenterX = 16.0f, FirstEnemyCenterX = 33.4f, EnemyGapX = 16.5f;
        public const float PlayerFootBarW = 10.3f, EnemyFootBarW = 9.7f;
        /// <summary>
        /// 발밑 2단 숫자 바(T35 · 주인 강조 · `02_battle.jpg`·`03_battle_enemy.jpg`): 체력 라벨 줄(HpLabelY · 높이 HpLabelH 3.2%) 안에 <b>빨강(HP) 위 · 파랑(실드) 아래</b> 같은 높이로 쌓는다 —
        /// 각 단 1.6%(보조 글자 하한 36px 이 안에 들어갈 높이 · T63-battle · 전엔 1.35) · 중심 y = 줄 위에서 0.8 / 2.4 → 두 단이 라벨 줄 3.2% 를 꼭 채운다. 실드 0 이면 파란 단은 숨긴다.
        /// 바 폭 = PlayerFootBarW·EnemyFootBarW × <see cref="FootBarScale"/>.
        /// </summary>
        public const float FootHpBarY = HpLabelY + 0.8f, FootShBarY = HpLabelY + 2.4f, FootBarH = 1.6f, FootShBarH = 1.6f;
        /// <summary>
        /// 발밑 바 폭 배율(T63-battle · 결정 133). T14 때는 캐릭터와 같은 <see cref="CharScale"/>(2/3 · 74px)였는데, 글자 하한(보조 36)으로 «1,239» 가 ≈84px 라 바를 넘쳐 T47 회차 3 눈 비평 감점이 됐다.
        /// 지시서 T63 2항의 순서(줄바꿈 → 칸 보정 → 문구 줄이기)대로 «칸(바)» 을 표 폭(10.3 / 9.7 = 111 / 105px)으로 돌린다 — 표 값이라 ui_score ② 두 행도 ○ 가 된다. 되돌리려면 이 값을 CharScale 로.
        /// </summary>
        public const float FootBarScale = 1f;
        /// <summary>그리기 간격 배율. 2 로 두면 멈춤 거리 밖의 거리를 화면에서 2배로 벌리지만(비균일 사상), 멀리 있는 소품·적이 가까운 것과 다른 속도로 움직여 부자연스럽다(주인 2026-09-05: «전이 나은데»).
        /// 그래서 1(= 예전 그대로 · 모든 것이 같은 속도)로 둔다. 진짜 간격 2배는 엔진 좌표(enemies.json enemyGap/nodeGap/nodeGapEvent) 를 바꿔야 하고 그건 밸런스·시드 골든이 바뀌는 일 — 승인 대기 24.</summary>
        public const float WorldSpacing = 1.0f;
        /// <summary>
        /// 전투 캐릭터 그리기 배율 (주인 지시 2026-09-05 «플레이어·적 크기 2/3» · T14). 표 상수(PlayerHeight·EnemyHeight·보스 ×BossSizeMul)는 ref-layout 표와
        /// LayoutSpecTests 가 대조하므로 그대로 두고, 그리는 쪽(BattleWorld)이 <see cref="CharHeightPct"/> 로 이 배율을 곱한다. 발밑 체력바 폭도 같은 배율(높이는 그대로).
        /// </summary>
        public const float CharScale = 2f / 3f;
        /// <summary>
        /// 전투 맵 그리기 배율 (주인 재지적 2026-09-06 «맵 디자인을 데모 씬에 있는 거 그대로» · T19). 데모 씬(DemoScene_Autumn 등)의 구성 — 바닥 · 길 띠(2.46u) · 물결 경계 위·아래 · 소품 —
        /// 을 <b>통째로</b> 이 배율로 그린다: 길 띠 2.46u → 1.48u(발 줄 40% 를 품는 중심 41%) · 소품 위치·크기 × 0.6 · 씬 폭 ≈27u → 16u. 우리 5.4u 창에 데모 화면(17.8u 창 · 씬의 2/3)과
        /// 같은 밀도로 보인다. 캐릭터는 <see cref="CharScale"/> 그대로(0.69u · 길 띠 안). 표 상수(GroundBand 등)는 손대지 않는다.
        /// </summary>
        public const float MapScale = 0.6f;
        /// <summary>표의 키 %(PlayerHeight 등) → 실제로 그리는 키 % (= × <see cref="CharScale"/>).</summary>
        public static float CharHeightPct(float tablePct) => tablePct * CharScale;
        /// <summary>
        /// 공격 애니 재생 속도 = 클립 길이 ÷ 공격 1회 간격 (하한 1 · 상한 없음 · T14). 간격 T 초 안에 Attack 클립(1.83초)이 끝나야 다음 공격에 모션이 잘리지 않는다.
        /// 예전 상한 ×3 은 공속이 빠르면(간격 &lt; 0.61초) 모션이 다음 공격에 잘렸다 → 상한 폐기. 타격 순간(OnAttackHit)도 같은 배율로 앞당겨진다(CharacterRig.HitDelay 가 속도를 나눈다).
        /// 플레이어 T = 1/공속(EffAspd) · 적 T = meleeInterval/bossInterval/rangedInterval × 슬로우 배율.
        /// </summary>
        public static float AttackAnimSpeed(float clipLen, double interval)
        {
            if (clipLen <= 0f) return 1f;
            double iv = interval > 1e-4 ? interval : 1e-4;   // 0 나눗셈만 막는다(상한이 아니다)
            return (float)System.Math.Max(1.0, clipLen / iv);
        }
        public static readonly R HudSpeed = new R(3.0f, 65.0f, 11.0f, 4.0f);
        public static readonly R HudRound = new R(85.0f, 63.0f, 13.0f, 6.5f);
        public static readonly R HudPanel = new R(0, 69.5f, 100, 30.5f);
        /// <summary>
        /// EXP·HP·실드 바(T143 · 주인 2026-09-07 05:3X «바가 왼쪽으로 치우쳐 보인다 · 바 사이에 틈이 없다») — 자리를 «바 사각형» 이 아니라
        /// <b>«왼쪽 캡(«EXP» 라벨 · ❤ · 🛡)까지 포함한 보이는 상자»</b> 로 잡는다. 캡은 바 rect 밖으로 <see cref="HudBarCapExp"/>·<see cref="HudBarCapIcon"/> 만큼 튀어나오므로,
        /// 표 실측값(3.0 / 31.0 / 65.0)을 그대로 쓰면 보이는 왼쪽 끝이 −0.52%p(화면 밖)가 되고 바 사이 틈(표 2.0%p)이 캡에 먹혀 14.4px 씩 겹쳤다.
        /// 지금 값은 «좌우 여백 2.0%p 대칭 · 틈 1.5%p · 폭 비율 26:32:32 유지» 로 다시 푼 것이다(<c>PerkStripSpecTests</c> 옆 <c>HudBarGeometryTests</c> 가 산술로 지킨다).
        /// </summary>
        public static readonly R HudExp = new R(5.52f, 70.5f, 23.93f, 3.7f);
        public static readonly R HudHp = new R(34.28f, 70.5f, 29.45f, 3.7f);
        public static readonly R HudSh = new R(68.56f, 70.5f, 29.45f, 3.7f);
        /// <summary>바 왼쪽 캡이 바 rect 보다 왼쪽으로 튀어나오는 폭(%p · 프레임 1080 기준 실측) — «EXP» 라벨(104×64 를 x14 에 가운데) 3.52 · ❤·🛡 아이콘(84×84 를 x6 에) 3.33.</summary>
        public const float HudBarCapExp = 3.52f, HudBarCapIcon = 3.33f;
        public static readonly R HudStats = new R(3.0f, 75.0f, 94.0f, 22.0f);
        public static readonly R HudStatCell = new R(3.0f, 75.0f, 47.0f, 5.2f);
        /// <summary>스탯 8칸 격자 — 열 피치(<c>HudStatColR</c>)는 «화면의 절반(50)» 이 아니라 <b>그리드 제 폭의 절반</b>(94.0 / 2 = 47.0)이다(T143 · 주인 «오른쪽으로 치우쳐 보인다»).
        /// 50 이던 동안 오른쪽 열이 53.4~99.6 에 놓여 왼 여백 3.4 / 오른 여백 0.4 로 3%p 치우쳤다 — 47.0 이면 50.4~96.6 으로 좌우 3.4 대칭이다(표 ② «스탯 그리드» 3.0/94.0 은 옳았고 코드가 틀렸다).</summary>
        public const float HudStatRowPitch = 5.2f, HudStatCellW = 47.0f, HudStatCellH = 5.2f, HudStatColR = 47.0f;
        /// <summary>
        /// 보유 특전 «책» 버튼 — <b>주인이 인스펙터 값으로 직접 지정</b>(T142 · 2026-09-07 05:2X · 앵커 Min(0.84, 0) ~ Max(1, 0.044)).
        /// 레퍼런스 실측이 아니라 주인이 준 값이다 — 화면 바닥에 붙고 오른쪽 끝(100%)까지 간다.
        /// <see cref="BattleScreen"/> 이 이 사각형을 <b>그대로</b> 쓴다(종전에는 여기에 ±여유를 더해 그렸다).
        /// </summary>
        public static readonly R HudInfo = new R(84.0f, 95.6f, 16.0f, 4.4f);
        /// <summary>전투 HUD «얻은 특전 미리보기 줄» — 주인이 인스펙터 값으로 지정(T142 · 앵커 Min(0.03, 0) ~ Max(0.834, 0.04186) · 화면 바닥에 붙는다 · 책 버튼과 안 겹친다: 83.4 &lt; 84.0).</summary>
        public static readonly R HudPerkStrip = new R(3.0f, 95.814f, 80.4f, 4.186f);

        // ③ 장비 탭 — 캐릭터 장비.jpg
        /// <summary>장비 무대 — 아래 끝이 갈색 띠(<c>GearScreen.Band</c> 위 = 41.0%)와 맞닿는다(T112 ⓐ · 주인 2026-09-07 «Stage 아래가 Band 랑 맞닿을 정도로 늘려서 스탯이 Stage 내부에 있는 느낌»).
        /// 그림(길·나무·덤불)은 <c>GearScreen.StageArtH</c> 로 환산해 늘리기 전과 <b>같은 자리·같은 크기</b>로 남는다 — 늘어난 만큼 들판이 스탯 줄 뒤까지 이어질 뿐이다.</summary>
        public static readonly R GearStage = new R(0, 8.5f, 100, 32.5f);
        // 슬롯 열 높이 21.0 → 22.3 · 칸 피치 7.3 → 8.0 (T63-gear · 표 ±3%p 보정 — 칸 위 «Lv. N»(본문 40) 과 위 칸 «+N» 배지가 24px 틈에서 겹쳤다 · docs/ref-layout.md «T63-gear 회차 정정»)
        public static readonly R GearSlotColL = new R(8.5f, 9.5f, 14.0f, 22.3f);
        public static readonly R GearSlotColR = new R(77.5f, 9.5f, 14.0f, 22.3f);
        public static readonly R GearSlot = new R(8.5f, 9.5f, 14.0f, 6.3f);
        public const float GearSlotPitch = 8.0f, GearSlotH = 6.3f;
        public static readonly R GearHero = new R(35.0f, 14.0f, 30.0f, 19.0f);
        public static readonly R GearStats = new R(10.0f, 36.5f, 79.0f, 4.0f);
        public static readonly R GearForgeBtn = new R(70.0f, 42.3f, 27.0f, 4.2f);
        public static readonly R GearInv = new R(3.0f, 47.5f, 94.0f, 44.5f);
        public static readonly R GearInvCell = new R(3.0f, 47.8f, 18.4f, 7.2f);
        public const int GearInvCols = 5; public const float GearInvRowPitch = 7.6f, GearInvCellW = 18.4f, GearInvCellH = 7.2f, GearInvGap = 0.6f;

        // ④ 장비 세부 팝업 — 장비 세부팝업.jpg (ov-gear · 닫기는 상자 밖)
        // T63-gear (표 ±3%p 보정 · docs/ref-layout.md «T63-gear 회차 정정»): 옵션 7줄이 본문 40 으로 한 줄씩 들어가게 옵션 목록 48/14 → 49/16 · 스탯 9.5 → 9.0 · 비용 62.5 → 65 · 버튼 66 → 68.5 · 박스 44 → 46.5
        public static readonly R GdBox = new R(6.5f, 28.0f, 87.0f, 46.5f);
        public static readonly R GdBadge = new R(39.0f, 27.5f, 22.0f, 2.3f);
        public static readonly R GdIcon = new R(11.0f, 30.5f, 15.0f, 7.0f);
        public static readonly R GdName = new R(28.0f, 31.0f, 50.0f, 3.0f);
        public static readonly R GdMeta = new R(29.0f, 34.5f, 60.0f, 3.0f);
        public static readonly R GdStats = new R(11.0f, 39.5f, 78.0f, 9.0f);
        public static readonly R GdOpts = new R(11.0f, 49.0f, 78.0f, 16.0f);
        public const float GdOptPitch = 2.4f;
        public static readonly R GdCost = new R(11.0f, 65.0f, 78.0f, 3.0f);
        public static readonly R GdBtns = new R(15.5f, 68.5f, 69.0f, 6.0f);
        public static readonly R GdBtnL = new R(15.5f, 68.5f, 33.0f, 6.0f);
        public static readonly R GdBtnR = new R(51.5f, 68.5f, 33.0f, 6.0f);
        public static readonly R GdClose = new R(30.0f, 91.5f, 40.0f, 2.0f);

        // ⑤ 상점 — 상점 (1).jpg
        public static readonly R ShopFreeRow = new R(3.0f, 12.5f, 94.0f, 6.5f);
        public static readonly R ShopSec1 = new R(0, 22.5f, 100, 2.5f);
        public static readonly R ShopCard1 = new R(3.0f, 27.0f, 30.0f, 18.5f);
        public static readonly R ShopCardRow1 = new R(3.0f, 27.0f, 94.0f, 18.5f);
        public static readonly R ShopCardRow2 = new R(3.0f, 47.5f, 94.0f, 18.5f);
        public static readonly R ShopSec2 = new R(0, 66.0f, 100, 2.5f);
        public static readonly R ShopCardRow3 = new R(3.0f, 70.5f, 94.0f, 18.5f);
        public const float ShopCardW = 30.0f, ShopCardGap = 2.0f, ShopCardRowPitch = 20.5f;

        // ⑥ 대장간/합성 — 장비 합성 업글창.jpg (상단 바 없음 · 탭바 대신 뒤로 버튼)
        public static readonly R ForgeStage = new R(0, 0, 100, 41.0f);
        public static readonly R ForgeResult = new R(8.0f, 11.5f, 22.0f, 10.0f);
        public static readonly R ForgeArrow = new R(17.0f, 22.0f, 8.0f, 4.0f);
        public static readonly R ForgeMat = new R(12.0f, 27.5f, 17.0f, 9.5f);   // 레퍼런스는 1칸 · 게임은 «같은 것 3개» 라 3칸(피치 ForgeMatPitch) — ref-layout U02 ⓓ 영구 X 행
        public const float ForgeMatPitch = 19.0f;
        public static readonly R ForgeBanner = new R(45.0f, 15.0f, 45.0f, 6.0f);
        /// <summary>
        /// 대장간 액션바 — <b>장비 화면의 갈색 띠(<c>GearScreen.Band</c>)와 같은 자리·같은 크기</b>(주인 2026-09-07 08:1X
        /// «대장간에 Action 바 부분도 장비 팝업에 Band 랑 같은 크기랑 위치로» · T113 ⓒ).
        /// 숫자를 베껴 적지 않고 <b>같은 식</b>(스탯 줄 아래 0.5%p ~ 인벤 격자 위)으로 계산한다 — 그래야 T112 가 무대·스탯·인벤을 옮겨도
        /// 두 화면의 띠가 저절로 같이 따라가고 «한 쪽만 어긋나는» 일이 없다(결정 274). 지금 값 = (0, 41.0, 100, 6.5).
        /// </summary>
        public static readonly R ForgeActionBar = new R(0, GearStats.Y + GearStats.H + 0.5f, 100, GearInv.Y - (GearStats.Y + GearStats.H + 0.5f));
        public static readonly R ForgeAuto = new R(2.0f, 42.0f, 28.0f, 4.5f);
        public static readonly R ForgeFuse = new R(70.0f, 42.0f, 27.5f, 4.5f);
        public static readonly R ForgeInv = new R(3.0f, 47.5f, 94.0f, 44.5f);
        public static readonly R ForgeBack = new R(3.0f, 93.5f, 17.0f, 5.0f);

        // ⑦ 특전 — perks.jpg (선택창: 상자 없음 · 카드가 화면에 직접) · perks 뭐뭐 있는지.jpg (인포 팝업: 상자)
        public static readonly R OvStats = new R(0, 4.0f, 100, 6.0f);
        public static readonly R OvStatCell = new R(0, 4.0f, 12.5f, 6.0f);
        public static readonly R OvBanner = new R(20.0f, 26.5f, 60.0f, 3.5f);
        public static readonly R OvSub = new R(30.0f, 31.5f, 40.0f, 3.0f);
        // T155 ⓑ(주인 «특전들 레이아웃이 레퍼런스랑 다른 느낌임 비율 비례 등등») — `docs/ref/04_perks.jpg`(720×1560) 실측으로 고침(결정 474).
        // 카드 세로: 레퍼런스 카드 세 장은 y 577~714 · 763~901 · 950~1087 px = **h 8.78/8.85/8.78%p** · 피치 **11.92/11.99%p**.
        // 우리 종전 값 h 11.0 · 피치 13.0 은 카드가 25% 두껍고 카드3 아래끝이 73.5 vs 레퍼런스 69.68 = **3.8%p 밖**(허용 ±3%p 초과)이었다.
        // ⚠ 아이콘은 이 값을 따라오지 않는다 — 조각이 «정사각» 을 지킨다(CI #359 실측 0.99). 회차 1 에 적었던
        //   «세로를 줄이면 아이콘 비율도 1.26 으로 맞는다» 는 **틀린 추론**이었고, 레퍼런스 팔각도 사실상 정사각이다(결정 480).
        // x·w·y 는 레퍼런스와 ±0.5%p 라 그대로 둔다(카드1 x 5.97 w 88.06 y 36.99 실측).
        public static readonly R OvCard1 = new R(5.5f, 36.5f, 89.0f, 8.8f);
        public static readonly R OvCard2 = new R(5.5f, 48.45f, 89.0f, 8.8f);
        public static readonly R OvCard3 = new R(5.5f, 60.4f, 89.0f, 8.8f);
        public const float OvCardPitch = 11.95f;
        public static readonly R OvCards = new R(5.5f, 36.5f, 89.0f, 32.7f);   // 카드1 위 ~ 카드3 아래(60.4+8.8 = 69.2 · 레퍼런스 69.68)
        /// <summary>특전 카드 왼쪽 팔각 아이콘 — <b>코드가 이 값으로 잡지 않는다</b>(카드 안은 조각의 앵커·비례다). 표 ⑦ ↔ 여기를 맞춰 두는 «기록용» 값이고,
        /// T155 ⓑ 에서 레퍼런스 실측(팔각 102×81px = 가로:세로 <b>1.26</b> · «정사각» 이 아니다)으로 고쳤다. 실제 그림은 카드 세로를 따라온다 — 그래서 카드 h 를 8.8 로 되돌린 것이 이 비율도 같이 맞췄다(결정 474).</summary>
        public static readonly R OvCardIcon = new R(8.5f, 38.5f, 14.4f, 6.3f);
        public static readonly R OvCardText = new R(25.0f, 38.5f, 68.0f, 7.0f);
        public static readonly R OvFoot = new R(31.0f, 79.0f, 38.0f, 7.5f);
        /// <summary>«남은 횟수 : N» — 예외적으로 <b>버튼(OvFoot) 칸의 %</b> 다(그 자식이라 버튼과 같이 뜨고 같이 커진다). 레퍼런스 04 처럼 버튼 «아래»: y 104% = 버튼 아래끝에서 프레임 0.3% 띄우고, h 46% = 프레임 3.45%(80px · 본문 40 한 줄 55px 이 든다) · w 120% 는 «남은 횟수 : 1»(≈330px)이 버튼 폭 410px 안에서 한 줄로 남게 하는 여유.</summary>
        public static readonly R OvFootRemain = new R(-10.0f, 104.0f, 120.0f, 46.0f);
        /// <summary>
        /// 특전 팝업의 «보유 특전(책)» 버튼 — <b>정사각</b>(T154 · 주인 2026-09-07 07:1X «책 아이콘 찌그러져 있더라»).
        /// 종전 9.0 × 6.0%p 는 프레임(1080×2337)에서 97.2 × 140.2px = <b>세로가 1.44배</b>라 조각 그림이 세로로 늘어났다 —
        /// 표 ⑦ 의 그 행 비고가 이미 «작은 정사각» 이라 <b>표의 비고가 옳고 수치가 틀렸던</b> 자리다.
        /// 폭 9.0%p = 97.2px 를 세로로 그대로 옮기면 97.2 / 2337 = <b>4.16%p</b> — 그래서 높이를 그 값으로 잡는다(<c>HudBarGeometryTests</c> 옆 산술 단언이 지킨다).
        /// </summary>
        public static readonly R OvInfo = new R(86.0f, 79.5f, 9.0f, 4.16f);
        public static readonly R BookBox = new R(6.5f, 23.0f, 87.0f, 52.5f);
        public static readonly R BookRibbon = new R(25.0f, 21.5f, 50.0f, 4.0f);
        public static readonly R BookCard = new R(11.0f, 26.5f, 78.0f, 9.5f);
        /// <summary>공통 팝업의 «탭하여 닫기» 줄 — 높이 2.0(46.7px)에는 본문 40 의 줄 높이(50px)가 안 들어가 모든 팝업에서 세로 잘림으로 잡혔다 → 2.4(56px · 표들의 2.0 과 차 0.4 · 자리 y 는 그대로). T63-settings.</summary>
        public static readonly R BookClose = new R(30.0f, 91.5f, 40.0f, 2.4f);
        /// <summary>이벤트 팝업(쉼터·악마·천사 등) — 표에 없는 화면. ⑧ 공통 «팝업 폭 87 · 좌우 여백 6.5» 와 ④ 의 세로(y28 h44)를 따른다.</summary>
        public static readonly R EvBox = new R(6.5f, 28.0f, 87.0f, 44.0f);

        /// <summary>
        /// 토스트 띠(ToastMessage_01) — 레퍼런스에 없는 요소라 자는 글자다. 프리팹은 글자 칸을 상자보다 세로 17.3px 작게 잡으므로
        /// 본문 40 <b>두 줄</b>(<see cref="TextSize.BoxHeight"/> = 112px)이 들어가려면 상자가 129.3px 이상이어야 한다 — 전 5.0%(116.9px · 칸 99.6px)에선
        /// 긴 문구(대장간 재료 안내 · 합성 완료)가 bestFit 으로 32 까지 말없이 줄었다. 6.0%(140.2px · 칸 123.0px) 로 올리고 <b>세로 중심(86.5%)은 그대로</b> 둔다(T63-toast).
        /// </summary>
        public static readonly R Toast = new R(4.0f, 83.5f, 92.0f, 6.0f);
        /// <summary>토스트 프리팹이 글자 칸에서 빼는 세로(ToastMessage_01 의 «Text (TMP)» sizeDelta.y) — 칸 세로 계산·테스트에 쓴다.</summary>
        public const float ToastTextInsetY = 17.2571f;

        // ⑨ 설정 팝업 — docs/ref/12_settings.jpg (T41 · 워커 실측 · 5% 격자 ±0.5%p · docs/ref-layout.md ⑨ 표와 같다)
        /// <summary>설정 상자 — T156 이 줄 둘(프로필 아이콘·닉네임)을 더해 h 21.0 → 30.6. **아래가 아니라 «위» 로 늘렸다**(y 39.6 → 30.0 · 바닥은 60.6 그대로) — 아래로 늘리면 상자 «밖» 링크가 로비 START 버튼과 겹친다(screens run 329 실측 · 결정 445). 상자 밖 셋(개인정보·이용약관·데이터 삭제)은 레퍼런스 자리 그대로다.</summary>
        public static readonly R SetBox = new R(5.8f, 30.0f, 88.3f, 30.6f);
        public static readonly R SetRibbon = new R(25.0f, 27.9f, 50.0f, 4.2f);
        public static readonly R SetRowMusic = new R(13.9f, 33.4f, 72.5f, 3.8f);
        public static readonly R SetRowSound = new R(13.9f, 38.2f, 72.5f, 3.8f);
        public static readonly R SetRowLang = new R(13.9f, 43.0f, 72.5f, 3.8f);
        /// <summary>레퍼런스 12 에 없는 우리 줄 — 주인 T156 «설정에 프로필 아이콘 설정, 닉네임 설정 있어야함»(자리는 언어 줄에서 피치만큼 아래로).</summary>
        public static readonly R SetRowProfile = new R(13.9f, 47.8f, 72.5f, 3.8f);
        public static readonly R SetRowNick = new R(13.9f, 52.6f, 72.5f, 3.8f);
        public const float SetRowPitch = 4.8f;
        public static readonly R SetToggle = new R(72.9f, 33.7f, 13.5f, 3.1f);
        /// <summary>언어 버튼 — 표 ⑨ 는 3.5(81.8px · 조각 글자 칸은 −30 이라 51.8px)라 버튼 글자 46 의 줄 높이 57.5px 를 bestFit 이 41 로 줄였다 → 3.8(칸 58.8px · 안 줄임 · 표와 차 0.3). T63-settings.</summary>
        public static readonly R SetLangBtn = new R(63.9f, 43.1f, 22.5f, 3.8f);
        /// <summary>새 줄 둘의 «변경» 버튼 — 언어 버튼과 같은 꼴·같은 폭에서 피치만큼 아래로(T156).</summary>
        public static readonly R SetProfileBtn = new R(63.9f, 47.9f, 22.5f, 3.8f);
        public static readonly R SetNickBtn = new R(63.9f, 52.7f, 22.5f, 3.8f);
        /// <summary>패널 밖 링크 2 — 높이는 본문 40 의 줄 높이(50px)가 들어가게 2.4(56px · 표 1.9 = 44.4px 에선 세로로 잘렸다). 개인정보 링크의 폭은 한국어 문구 실측(268px = 24.8%)에 여유를 둔 25.5(가운데 50% 고정) — 표 ⑨ 의 20.3 은 영문 «Privacy Policy» 실측이다(ref-layout ⑨ 회차 정정). T63-settings.</summary>
        public static readonly R SetPrivacy = new R(37.2f, 62.6f, 25.5f, 2.4f);
        public static readonly R SetTerms = new R(40.6f, 67.3f, 18.8f, 2.4f);
        /// <summary>레퍼런스에 없는 줄 — T29 «데이터 삭제»(로비) / 전투 일시정지의 «재개»·«포기하고 로비로» 가 링크 아래 이 줄에 선다. T156 회차 2 로 이 셋은 **레퍼런스 자리 그대로**다(상자를 위로 늘렸다 · 결정 445).</summary>
        public static readonly R SetReset = new R(35.0f, 72.0f, 30.0f, 4.0f);
        public static readonly R SetResumeBtn = new R(18.0f, 72.0f, 30.0f, 4.0f);
        public static readonly R SetGiveUpBtn = new R(52.0f, 72.0f, 30.0f, 4.0f);

        // ⑩ 펫 탭 — docs/ref/13_pet.jpg (T42 · 워커 E 실측 · 720×1560 사본 · ±0.5%p · docs/ref-layout.md ⑩ 표와 같다) · 상단 바 = LobbyTopBar · 탭 바 = TabBar
        /// <summary>펫 격자 = 4열 × 3행 아이콘 칸 9개의 합집합(Lv 라벨·진행바 제외). 칸 = 정사각 15.6×7.2 · 열 피치 22.1 · 행 피치 11.5(칸 위 «Lv. N» · 칸 아래 진행바 포함).</summary>
        public static readonly R PetGrid = new R(9.0f, 10.6f, 81.9f, 30.3f);
        public static readonly R PetCell = new R(9.0f, 10.6f, 15.6f, 7.2f);
        public const int PetCols = 4, PetCount = 9; public const float PetColPitch = 22.1f, PetRowPitch = 11.5f;
        /// <summary>칸 위에 걸친 «Lv. N» 글자(첫 칸 기준) · 칸 아래 진행바(첫 칸 기준 · 칸보다 넓다 19.2).</summary>
        public static readonly R PetLv = new R(11.5f, 10.1f, 10.6f, 1.8f);
        public static readonly R PetBar = new R(7.2f, 19.2f, 19.2f, 1.6f);
        /// <summary>
        /// 진행바의 실제 높이(T63-pet · 격자 9칸 + 세부 팝업 공용). 표의 1.6(⑩) / 1.4(⑪) 는 «n/m» 본문 40(선호 높이 39px)이 안 들어가는 높이다 —
        /// 조각(Slider_02)의 글자 rect 가 바보다 2px 작아 35/31px 이 되고 CI #101 게이트가 «잘림» 19건을 셌다. 표는 그대로 두고(레퍼런스 실측 · ±3%p 안이라 ui_score ○)
        /// 바만 표 중심을 지켜 이 높이로 키운다(44px → 글자 rect 42px ≥ 39). 되돌리려면 PetCell 의 <c>WithH</c> 호출을 빼면 표값(1.6/1.4)으로.
        /// </summary>
        public const float PetBarH = 1.9f;
        /// <summary>합계 줄(«+N ❤ | +N 🛡 | +N 🗡») → «장착중» 띠(어두운 패널) + 초록 «장착중» 라벨 + 장착 슬롯 4(잠금 2 · 빈 칸 2 · 피치 11.9).</summary>
        public static readonly R PetSum = new R(18.8f, 58.3f, 63.2f, 2.6f);
        public static readonly R PetEqBand = new R(8.3f, 61.9f, 83.3f, 6.6f);
        public static readonly R PetEqLabel = new R(7.2f, 63.8f, 23.6f, 2.4f);
        public static readonly R PetSlots = new R(43.9f, 63.5f, 42.9f, 3.4f);
        public static readonly R PetSlot = new R(43.9f, 63.5f, 7.2f, 3.4f);
        public const float PetSlotPitch = 11.9f;
        /// <summary>회색 «전체 강화»·«빠른 장착» 한 줄 → 주황 «소환»·«소환 x10» 한 줄(가격 줄 포함 · 더 크다).</summary>
        public static readonly R PetUpgradeAll = new R(11.4f, 72.8f, 36.5f, 6.0f);
        public static readonly R PetQuickEquip = new R(52.1f, 72.8f, 36.5f, 6.0f);
        public static readonly R PetSummon = new R(9.0f, 83.3f, 39.3f, 7.7f);
        public static readonly R PetSummon10 = new R(51.7f, 83.3f, 39.3f, 7.7f);

        // ⑪ 펫 세부 팝업 — docs/ref/14_pet_detail.jpg (T42 · 워커 E 실측 · ⑪ 표와 같다 · 명판 없음 · 칸이 상자 윗변에 걸친다 · 닫기 안내 = BookClose 줄)
        public static readonly R PdBox = new R(7.6f, 34.9f, 84.7f, 31.7f);
        public static readonly R PdCell = new R(41.7f, 32.4f, 16.7f, 7.7f);
        public static readonly R PdBar = new R(39.2f, 41.5f, 21.7f, 1.4f);
        public static readonly R PdDesc = new R(11.1f, 44.9f, 77.8f, 8.0f);
        public static readonly R PdPassiveTitle = new R(42.4f, 54.2f, 15.3f, 2.2f);
        public static readonly R PdPassive = new R(34.7f, 56.7f, 31.3f, 2.6f);
        public static readonly R PdBtnL = new R(13.5f, 59.9f, 35.1f, 5.1f);
        public static readonly R PdBtnR = new R(51.4f, 59.9f, 35.1f, 5.1f);

        // ⑫~⑱ 던전·아레나 — ⚑ T63-events(글자 가독성 · 2026-09-06): 글자가 든 칸 10개의 h 를 «크기 × 1.4» 이상으로 올렸다(제목 3.0/2.9→3.7 = 제목 60 · 부제 1.7→2.5 = 본문 40 ·
        // 시즌 타이머 1.6→2.3 · 조건 문구 2.2→2.6 · 리셋 타이머 2.2→2.6 · 안내 문구 1.4→2.3). 레퍼런스 % 자체는 그대로고 게임 칸만 +0.4~0.9%p(ui_score PASS 3.0%p 안 · ref-layout ⚑ T63-events 회차 정정).
        // ⑫ 던전 페이지 — docs/ref/20_dungeon.jpg (T43 · 워커 B 실측 · 720×1560 사본 픽셀 런 · ±0.5%p · docs/ref-layout.md ⑫ 표와 같다)
        public static readonly R DgTitle = new R(32.0f, 10.8f, 36.0f, 3.7f);
        public static readonly R DgTitleLine = new R(6.0f, 14.8f, 88.0f, 0.6f);
        public static readonly R DgSub = new R(24.0f, 16.7f, 52.0f, 2.5f);
        public static readonly R DgCard1 = new R(3.6f, 20.4f, 92.6f, 26.4f);
        // T101 ⓐ(주인 2026-09-07 «카드들이 간격 없이 너무 딱 달라붙어 있어서 구분이 안 감») — 카드 1 바닥 46.8 과 카드 2 사이에 2.0%p 빈칸(카드 높이 26.4 의 7.6% · 레퍼런스 20 의 두 카드 사이 여백과 같은 정도)
        public static readonly R DgCard2 = new R(3.6f, 48.8f, 92.6f, 26.4f);
        public const float DgCardPitch = 28.4f;
        public static readonly R DgCardHead = new R(3.6f, 20.4f, 92.6f, 3.6f);
        public static readonly R DgCardPic = new R(3.6f, 24.0f, 92.6f, 15.9f);
        public static readonly R DgEnter = new R(63.5f, 38.6f, 29.0f, 5.3f);
        public static readonly R DgRewards = new R(5.0f, 41.1f, 20.0f, 3.4f);
        /// <summary>던전·아레나 화면 공통 바닥 띠(레퍼런스에 5탭 바가 없다 — 뒤로 + 2탭) · 뒤로 · 던전/PvP 2탭.</summary>
        public static readonly R DgFoot = new R(0, 92.3f, 100, 7.7f);
        public static readonly R DgBack = new R(3.5f, 94.0f, 16.0f, 5.3f);
        public static readonly R DgTabs = new R(59.7f, 92.3f, 40.3f, 7.7f);

        // ⑬ 던전 세부 팝업 — docs/ref/21_dungeon_detail.jpg (T43)
        public static readonly R DdBox = new R(12.4f, 24.2f, 75.5f, 51.4f);
        public static readonly R DdHead = new R(12.4f, 24.2f, 75.5f, 3.7f);
        public static readonly R DdPic = new R(13.0f, 27.9f, 74.4f, 14.1f);   // T102 ⓑ — y = DdHead 바닥(24.2+3.7) · 아래 끝(42.0)은 그대로
        public static readonly R DdNote = new R(14.5f, 38.8f, 71.5f, 2.6f);
        public static readonly R DdArrow = new R(21.5f, 45.0f, 6.0f, 4.0f);
        public static readonly R DdFloor = new R(41.7f, 43.0f, 16.6f, 7.6f);
        public static readonly R DdRewards = new R(16.1f, 52.1f, 67.7f, 10.8f);
        public static readonly R DdRewardCells = new R(22.2f, 55.8f, 55.6f, 5.1f);
        public static readonly R DdTicket = new R(44.0f, 64.5f, 12.0f, 2.5f);
        public static readonly R DdBtns = new R(16.1f, 68.3f, 67.7f, 6.1f);

        // ⑭ 아레나(PvP) 페이지 — docs/ref/22_arena.jpg (T43)
        public static readonly R ArTitle = new R(40.0f, 10.8f, 20.0f, 3.7f);
        public static readonly R ArSub = new R(27.5f, 16.7f, 45.0f, 2.5f);
        public static readonly R ArCard = new R(3.6f, 20.3f, 92.6f, 26.5f);
        public static readonly R ArCardHead = new R(3.6f, 20.3f, 92.6f, 3.7f);
        public static readonly R ArCardPic = new R(3.6f, 24.0f, 92.6f, 15.7f);
        public static readonly R ArSeason = new R(5.0f, 38.3f, 35.0f, 2.3f);
        public static readonly R ArEnter = new R(63.5f, 38.6f, 29.0f, 5.3f);
        public static readonly R ArTier = new R(6.0f, 41.0f, 24.0f, 3.9f);

        // ⑮ 아레나 입장 화면 — docs/ref/23_arena_enter.jpg (T43)
        public static readonly R AeStage = new R(0, 8.0f, 100, 34.0f);
        public static readonly R AeTier = new R(37.5f, 9.9f, 26.0f, 3.7f);
        public static readonly R AeSeason = new R(31.0f, 13.8f, 38.0f, 2.3f);
        public static readonly R AeSideIcons = new R(84.7f, 9.6f, 12.5f, 9.8f);
        public static readonly R AePortrait1 = new R(41.7f, 20.2f, 16.6f, 6.4f);
        public static readonly R AePortrait2 = new R(12.5f, 23.4f, 15.3f, 6.4f);
        public static readonly R AePortrait3 = new R(72.2f, 23.4f, 15.3f, 6.4f);
        public static readonly R AePortraits = new R(12.5f, 20.2f, 75.0f, 9.6f);
        public static readonly R AeBanner1 = new R(36.8f, 27.6f, 26.4f, 9.6f);
        public static readonly R AeBanner2 = new R(9.0f, 30.4f, 22.3f, 8.7f);
        public static readonly R AeBanner3 = new R(68.8f, 30.4f, 22.2f, 8.7f);
        public static readonly R AeBanners = new R(9.0f, 27.6f, 82.0f, 11.5f);
        public static readonly R AeList = new R(2.4f, 42.3f, 95.1f, 50.0f);
        public static readonly R AeRow = new R(2.4f, 42.3f, 95.1f, 6.7f);
        public const float AeRowPitch = 7.6f;
        public static readonly R AePromo = new R(0, 89.7f, 100, 2.6f);
        public static readonly R AeChallenge = new R(33.5f, 93.6f, 33.0f, 5.6f);

        // ⑯ 아레나 도전 팝업 — docs/ref/24_arena_challenge.jpg (T43)
        public static readonly R AcBox = new R(4.7f, 20.1f, 90.6f, 60.0f);
        public static readonly R AcHead = new R(4.7f, 20.1f, 90.6f, 4.6f);
        public static readonly R AcInfoRow = new R(8.3f, 27.2f, 83.4f, 2.6f);
        public static readonly R AcList = new R(8.3f, 31.7f, 83.4f, 39.6f);
        public static readonly R AcRow = new R(8.3f, 31.7f, 83.4f, 6.6f);
        public const float AcRowPitch = 8.2f;
        public static readonly R AcRowBtn = new R(61.1f, 32.4f, 29.9f, 5.4f);
        public static readonly R AcRefresh = new R(33.3f, 72.8f, 33.4f, 6.0f);
        /// <summary>
        /// ⑯ 의 «탭하여 닫기» 줄(T214) — 공통 <see cref="BookClose"/>(91.5)보다 <b>4.4%p 아래</b>다. 레퍼런스는 화면마다 이 줄이 다르고
        /// (④ 91.5 · ⑮ 94.4 · ⑯ 95.9) 공통값 하나로는 ⑯ 이 판정 ±3%p 를 넘어 0.5점이었다. 높이는 공통과 같은 2.4(본문 40 의 줄 높이 56px · T63-settings).
        /// <b>여기만 따로 두는 까닭</b> = <see cref="BookClose"/> 를 옮기면 ④(표 91.5 · 지금 정확히 맞는다)가 대신 어긋난다.
        /// </summary>
        public static readonly R AcClose = new R(30.0f, 95.9f, 40.0f, 2.4f);
        /// <summary>
        /// ⑦ 인포 팝업(특전 목록)의 «탭하여 닫기» 줄(T230) — 공통 <see cref="BookClose"/>(91.5)보다 <b>2.9%p 아래</b>다.
        /// <para>
        /// 표 ⑦ 은 이 줄을 오래 <b>91.5</b> 로 적고 있었는데 <b>그 그림에서 잰 값이 아니었다</b> — `docs/ref/05_perks_list.jpg` 실측은 글자 94.23~95.58(칸 ≈94.4)이다.
        /// 표와 코드가 같은 91.5 를 들고 있어 §5 는 «+0.0 ○» 를 줬지만 화면에서는 힌트가 <b>스탯 격자 위에 얹혔다</b>(레퍼런스는 격자 아래 빈 띠).
        /// </para>
        /// <b>공통값을 안 옮기는 까닭</b> = 이 줄은 레퍼런스에서 화면마다 다르다(④ ≈91.0 · ⑪ ≈91.0 · ⑦·⑨·⑬·⑳·㉑·㉒ ≈94.4 · ⑯ ≈96.1) —
        /// <see cref="BookClose"/> 를 내리면 ⑪(표 90.4)이 대신 판정 밖으로 나간다(<see cref="AcClose"/> 와 같은 갈래 · 결정 564).
        /// </summary>
        public static readonly R PerkBookClose = new R(30.0f, 94.4f, 40.0f, 2.4f);

        // ⑰ 아레나 순위 보상 팝업 — docs/ref/25_arena_rank_reward.jpg (T43)
        public static readonly R RrBox = new R(4.7f, 20.6f, 90.6f, 58.9f);
        public static readonly R RrHead = new R(4.7f, 20.6f, 90.6f, 4.8f);
        public static readonly R RrTiers = new R(4.7f, 25.9f, 90.6f, 8.5f);
        public static readonly R RrTimer = new R(30.0f, 35.6f, 40.0f, 2.6f);
        public static readonly R RrNote = new R(25.0f, 38.8f, 50.0f, 2.3f);
        public static readonly R RrList = new R(8.6f, 41.7f, 83.1f, 27.5f);
        public static readonly R RrRow = new R(8.6f, 41.7f, 83.1f, 5.5f);
        public const float RrRowPitch = 7.2f;
        public static readonly R RrTabs = new R(8.6f, 72.4f, 83.1f, 7.1f);

        // ⑱ 아레나 상인 페이지 — docs/ref/26_arena_shop.jpg (T43)
        public static readonly R MeBanner = new R(0, 8.0f, 100, 15.9f);
        public static readonly R MeTitle = new R(38.2f, 9.9f, 23.6f, 3.7f);
        public static readonly R MeSeason = new R(2.8f, 20.2f, 38.9f, 2.3f);
        public static readonly R MeGrid = new R(3.6f, 26.0f, 92.6f, 66.3f);
        public static readonly R MeCard = new R(3.6f, 26.0f, 29.0f, 17.6f);
        public const float MeColPitch = 31.9f, MeRowPitch = 18.9f;
        // ⑲ 특권 페이지 — docs/ref/11_shop_special.jpg (T44 · 워커 F 실측 · ⑲ 표와 같다 · 껍데기) · 상단 바 = LobbyTopBar · 탭 바 없음(바닥 바)
        public static readonly R PrTitle = new R(33.0f, 10.6f, 35.0f, 3.0f);
        public static readonly R PrUnderline = new R(6.0f, 14.6f, 88.0f, 0.8f);
        public static readonly R PrSub = new R(21.0f, 16.5f, 58.0f, 1.8f);
        /// <summary>카드 4장(짧은 카드 1 + 긴 카드 3 · 세로 스크롤) — 카드 1 = 일일 선물(보상 칸 + 버튼) · 카드 2~4 = 제목 띠 + 설명 상자 + 그림 + «매일 수령» 보상 칸 + 버튼. 카드 안 요소는 카드 2 기준 프레임 %(다른 카드는 y 차만큼 옮긴다).</summary>
        public static readonly R PrCard1 = new R(4.0f, 20.1f, 92.0f, 11.1f);
        public static readonly R PrCard1Reward = new R(6.9f, 25.1f, 11.2f, 5.0f);
        public static readonly R PrCard1Btn = new R(63.9f, 25.1f, 29.4f, 4.8f);
        public static readonly R PrCard2 = new R(4.0f, 32.4f, 92.0f, 22.0f);
        public static readonly R PrCardTitle = new R(4.0f, 32.4f, 92.0f, 4.3f);
        public static readonly R PrCardDesc = new R(6.5f, 37.6f, 52.5f, 9.8f);
        public static readonly R PrCardPic = new R(64.6f, 37.5f, 25.7f, 9.9f);
        public static readonly R PrCardReward = new R(36.4f, 48.4f, 11.1f, 5.1f);
        public static readonly R PrCardBtn = new R(63.9f, 48.5f, 29.4f, 4.8f);
        public static readonly R PrCard3 = new R(4.0f, 55.6f, 92.0f, 22.9f);
        public static readonly R PrCard4 = new R(4.0f, 79.8f, 92.0f, 22.9f);
        public static readonly R PrFootBar = new R(0, 93.3f, 100, 6.7f);

        // ㉝ 챕터 보상(Chapter Chest) 페이지 — 레퍼런스 32_lobby_clear.jpg 실측(720×1560) · T98
        /// <summary>파란 제목 리본 «챕터 보상».</summary>
        public static readonly R CcRibbon = new R(6.9f, 14.6f, 86.1f, 4.6f);
        /// <summary>부제 «챕터 N 에서 적 M 처치»(리본과 배너 사이).</summary>
        public static readonly R CcSub = new R(10.0f, 28.8f, 80.0f, 2.6f);
        /// <summary>가운데 챕터 배너(금테 카드) — 좌우 이웃은 같은 크기로 <see cref="CcBannerPitch"/> 만큼 밀어 놓는다(레퍼런스처럼 가장자리만 보인다).</summary>
        public static readonly R CcBanner = new R(21.5f, 32.1f, 57.0f, 27.6f);
        /// <summary>이웃 배너까지의 가로 간격(%p) — 레퍼런스 32 에서 오른쪽 이웃의 왼쪽 끝이 84.5% 쯤 보인다.</summary>
        public const float CcBannerPitch = 63.0f;
        /// <summary>배너 «안» % — 제목 pill «챕터 N».</summary>
        public static readonly R CcBannerTitle = new R(26.2f, 19.8f, 47.8f, 9.3f);
        /// <summary>배너 «안» % — 목표 글자 «적 M 처치»(두 줄).</summary>
        public static readonly R CcBannerGoal = new R(8.0f, 38.4f, 84.0f, 26.7f);
        /// <summary>«보상» 상자.</summary>
        public static readonly R CcRewardBox = new R(20.6f, 63.8f, 58.9f, 10.6f);
        /// <summary>상자 «안» % — 머리 글자 «보상».</summary>
        public static readonly R CcRewardHead = new R(0, 4.0f, 100, 24.0f);
        /// <summary>보상 칸 첫째(다이아) — 둘째(골드)는 x + <see cref="CcRewardPitch"/>.</summary>
        public static readonly R CcRewardCell = new R(39.0f, 67.3f, 9.8f, 4.5f);
        /// <summary>보상 칸 사이 가로 간격(%p).</summary>
        public const float CcRewardPitch = 12.1f;
        /// <summary>«받기» 버튼(못 받으면 회색).</summary>
        public static readonly R CcClaim = new R(33.3f, 77.6f, 33.3f, 5.1f);
        /// <summary>바닥 회색 띠(특권 페이지와 같은 자리).</summary>
        public static readonly R CcFootBar = new R(0, 93.3f, 100, 6.7f);
        /// <summary>왼쪽 아래 뒤로(◀).</summary>
        public static readonly R CcBack = new R(2.8f, 94.2f, 16.7f, 4.5f);
        public static readonly R PrBack = new R(2.5f, 93.9f, 16.9f, 5.1f);
        public static readonly R PrClaimAll = new R(32.2f, 93.9f, 35.6f, 5.1f);

        // T63-lobbypopups — 로비 팝업 6종(11·15~19)의 글자 칸 보정(표 ⑲~㉔ 값은 그대로 · 쓰는 자리에서 WithH · ±3%p 안):
        /// <summary>목록 줄의 제목·카운터·남은 기간·부제 칸 높이 — 본문 40 한 줄(선호 ≈ 39~44px)이 들어가게 2.2% = 51px(표의 1.4~1.8% = 33~42px 는 레퍼런스의 작은 글자 기준 높이).</summary>
        public const float LpLineH = 2.2f;
        /// <summary>목록 줄 진행바 높이 — <see cref="PetBarH"/> 와 같은 이유(Slider_02 조각의 글자 rect 가 바보다 2px 작다 · 44px → 42 ≥ 39).</summary>
        public const float LpBarH = 1.9f;
        /// <summary>페이지 제목(특권 «특권» · 패스 «시즌 패스»)은 제목 종류 60 → 칸 3.2% = 75px(표 ㉔ 2.6% 는 61px 라 모자란다).</summary>
        public const float LpTitleH = 3.2f;
        /// <summary>데일리 기프트 «광고 N회 보기» 제목 칸 폭 — 표 ㉒ 19.2%(207px)가 Jua 40 의 «광고 6회 보기»(≈214px)보다 좁아 줄바꿈되던 것 → 24%(259px · 이름표는 글자 덩어리를 잰다).</summary>
        public const float GfRowTitleW = 24.0f;
        // ⑳ 퀘스트 팝업 — docs/ref/15_quest.jpg (T44 · ⑳ 표와 같다 · 탭 3 은 박스 아래 · 닫기 안내 = BookClose 줄) · T78(주인 2026-09-07): 상자·제목·줄·보상 칸·받기 표시는 GUI Pro Progression_Mission_02 프리팹 조각 — % 는 그대로다
        public static readonly R QsTitleBand = new R(6.4f, 20.0f, 87.2f, 4.9f);
        public static readonly R QsBox = new R(6.4f, 24.9f, 87.2f, 50.4f);
        public static readonly R QsTrackBox = new R(8.6f, 26.0f, 82.8f, 7.3f);
        public static readonly R QsTrackIcons = new R(12.8f, 27.2f, 74.4f, 3.4f);
        public static readonly R QsTrackIcon = new R(12.8f, 27.2f, 6.9f, 3.4f);
        public const int QsTrackCount = 6; public const float QsTrackPitch = 13.5f;
        public static readonly R QsTrackNums = new R(12.8f, 31.0f, 74.4f, 1.4f);
        public static readonly R QsRefresh = new R(28.5f, 34.4f, 39.0f, 1.7f);
        public static readonly R QsListBox = new R(8.6f, 37.1f, 82.8f, 37.8f);
        public static readonly R QsRow1 = new R(12.0f, 38.3f, 76.0f, 5.9f);
        public static readonly R QsRow2 = new R(12.0f, 45.1f, 76.0f, 5.9f);
        public const float QsRowPitch = 6.8f; public const int QsRowCount = 6;
        public static readonly R QsRowMedal = new R(15.6f, 39.1f, 6.3f, 3.2f);
        public static readonly R QsRowTitle = new R(25.4f, 39.2f, 39.9f, 1.6f);
        public static readonly R QsRowBar = new R(25.4f, 41.3f, 39.3f, 1.8f);
        public static readonly R QsRowGo = new R(67.2f, 39.2f, 18.4f, 4.4f);
        public static readonly R QsTabs = new R(10.0f, 75.6f, 80.0f, 5.7f);
        public static readonly R QsTab = new R(10.0f, 75.6f, 25.5f, 5.7f);
        public const float QsTabPitch = 27.3f;

        // ㉑ 출석 팝업 — docs/ref/16_attendance.jpg (T44 · ㉑ 표와 같다 · 노란 리본이 박스 윗변에 걸친다)
        public static readonly R AtRibbon = new R(3.0f, 25.3f, 94.0f, 3.9f);
        public static readonly R AtBox = new R(6.4f, 29.2f, 87.2f, 42.9f);
        public static readonly R AtGrid = new R(9.7f, 30.6f, 80.6f, 26.3f);
        public static readonly R AtCell = new R(9.7f, 30.6f, 25.7f, 12.7f);
        public const int AtCols = 3; public const float AtColPitch = 27.5f, AtRowPitch = 13.6f;
        public static readonly R AtCellHead = new R(9.7f, 30.6f, 25.7f, 2.5f);
        public static readonly R AtCellIcon = new R(15.0f, 34.8f, 15.0f, 7.2f);
        public static readonly R AtDay7 = new R(9.7f, 57.9f, 80.6f, 12.8f);
        public static readonly R AtDay7Head = new R(9.7f, 57.9f, 80.6f, 2.5f);
        public static readonly R AtDay7Rewards = new R(33.1f, 62.1f, 33.8f, 7.1f);
        public static readonly R AtDay7Cell = new R(33.1f, 62.1f, 15.5f, 7.1f);
        public const float AtDay7Pitch = 18.3f;

        // ㉒ 데일리 기프트 팝업 — docs/ref/17_daily_gift.jpg (T77 회차 정정 · 주인 추가 2026-09-07 00:3X «왼쪽 타임라인 없애고 행은 가운데»)
        // 타임라인(선·점)은 없앴고, 광고 줄은 상자(8.6~91.4) 가로 중앙에 폭 74.4 = 상자 안폭의 89.9%(«~90%») — 왼쪽 여백 4.2 = 오른쪽 여백 4.2.
        // 줄 안 요소는 옛 표에서 그대로 −6.6%p 평행 이동(제목·진행바·보상 칸)했고, 진행바만 오른쪽 여백(2.2)을 지키며 63.9 → 70.5 로 넓어졌다.
        public static readonly R GfPic = new R(13.3f, 13.1f, 73.4f, 11.9f);
        public static readonly R GfRibbon = new R(4.2f, 24.9f, 91.6f, 3.9f);
        public static readonly R GfBox = new R(8.6f, 28.8f, 82.8f, 52.6f);
        public static readonly R GfTimer = new R(34.0f, 29.2f, 28.0f, 1.6f);
        public static readonly R GfTodayCell = new R(12.8f, 31.5f, 74.4f, 8.1f);
        public static readonly R GfRow1 = new R(12.8f, 40.1f, 74.4f, 9.3f);
        public static readonly R GfRow2 = new R(12.8f, 50.1f, 74.4f, 9.3f);
        public const float GfRowPitch = 10.0f; public const int GfRowCount = 4;
        public static readonly R GfRowTitle = new R(14.5f, 40.5f, 19.2f, 1.7f);
        public static readonly R GfRowBar = new R(14.5f, 42.8f, 70.5f, 1.3f);
        public static readonly R GfRowReward = new R(14.9f, 44.7f, 9.3f, 4.0f);
        /// <summary>레퍼런스의 ✅ 자리 = 우리는 «광고 보기 / 받기 / 받음 / 잠금» 버튼(T77 · 기능이 생겨 표시 글리프가 버튼이 됐다).
        /// 가운데(71.4%)는 레퍼런스 ✅ 와 같고 폭만 9.4 → 21.6(버튼 44 «광고 보기» 선호 ≈142px 의 164% · 결정 153 «≥110%»), 세로는 3.3 → 3.8(BoxHeight(44) = 61.6px = 2.64% 위).</summary>
        public static readonly R GfRowBtn = new R(60.6f, 44.7f, 21.6f, 3.8f);
        /// <summary>«오늘의 선물» 무료 칸의 같은 버튼 — 줄 버튼과 같은 가운데·폭(레퍼런스도 ✅ 가 같은 x).</summary>
        public static readonly R GfTodayBtn = new R(60.6f, 34.9f, 21.6f, 3.8f);

        // ㉕ 탐험 팝업 — docs/ref/30_expedition.jpg (T97 · 워커 D 실측 · ±0.5%p · 720×1560 격자)
        // 상자 = 그림 띠 + 명판 + 안내/경과/시간당 pill + 보상 격자 + 상한 안내 띠 + 버튼 2개(한 줄).
        public static readonly R ExBox = new R(4.6f, 19.6f, 91.0f, 61.7f);
        public static readonly R ExPic = new R(4.6f, 19.6f, 91.0f, 16.9f);
        public static readonly R ExPlate = new R(4.6f, 36.7f, 91.0f, 4.3f);
        public static readonly R ExInfoBtn = new R(85.0f, 37.4f, 8.0f, 2.9f);
        public static readonly R ExSub = new R(4.6f, 41.6f, 91.0f, 2.2f);          // 보조 36 한 줄(50.4px = 2.16%) — 레퍼런스 1.9 에서 +0.3%p (T63 하한)
        public static readonly R ExTime = new R(4.6f, 44.0f, 91.0f, 3.6f);         // 제목 60 한 줄(84px = 3.6%) — 레퍼런스 2.3 에서 +1.3%p (T63 하한 · 레퍼런스도 이 줄이 가장 큰 글자다)
        public static readonly R ExRatePill1 = new R(28.5f, 47.8f, 19.7f, 2.6f);   // 본문 40(56px = 2.4%)이 들어간다 · 경과 시간이 커진 만큼 +0.8%p 내려온다
        public static readonly R ExRatePill2 = new R(50.1f, 47.8f, 20.4f, 2.6f);
        public static readonly R ExGridBg = new R(4.6f, 50.3f, 91.0f, 17.2f);
        public static readonly R ExCell = new R(11.8f, 51.3f, 11.4f, 5.4f);
        public const float ExCellPitchX = 13.0f, ExCellPitchY = 6.1f; public const int ExCols = 6;
        public static readonly R ExCapNote = new R(4.6f, 68.0f, 91.0f, 4.4f);      // 보조 36 두 줄(100.8px = 4.3%) — 레퍼런스 4.1 에서 +0.3%p
        public static readonly R ExQuickBtn = new R(7.9f, 73.4f, 40.3f, 6.4f);
        public static readonly R ExClaimBtn = new R(51.5f, 73.4f, 40.9f, 6.4f);
        /// <summary>버튼 오른쪽 위 배지(빠른 탐험 남은 횟수 · «!») — 버튼 rect 안 비율(레퍼런스 30 의 빨간 사각 배지).</summary>
        public static readonly R ExBtnBadge = new R(84.0f, -6.0f, 20.0f, 40.0f);

        // ㉖ 빠른 탐험 팝업 — docs/ref/31_expedition_fast.jpg (T97 · 워커 D 실측 · ±0.5%p · 30 위에 겹치는 작은 상자)
        public static readonly R QxBox = new R(5.3f, 31.4f, 89.4f, 36.5f);
        public static readonly R QxPlate = new R(5.3f, 31.4f, 89.4f, 4.5f);
        public static readonly R QxSub = new R(5.3f, 36.6f, 89.4f, 2.2f);          // 보조 36 한 줄 (+0.4%p)
        public static readonly R QxTitle = new R(5.3f, 39.2f, 89.4f, 2.4f);        // 본문 40 한 줄 (+0.4%p)
        public static readonly R QxGridBg = new R(10.8f, 41.9f, 78.2f, 15.2f);
        public static readonly R QxCell = new R(13.9f, 43.5f, 12.2f, 5.1f);
        public const float QxCellPitchX = 15.0f; public const int QxCellCount = 4;
        public static readonly R QxNote = new R(5.3f, 58.4f, 89.4f, 2.4f);         // 본문 40 한 줄 (+0.4%p)
        public static readonly R QxFreeBtn = new R(31.7f, 61.9f, 36.6f, 4.8f);

        // ㉓ 7일 챌린지 팝업 · ㉔ 패스 페이지 — T78(주인 2026-09-07 «7일 챌린지 걍 안 하고 싶음» · «시즌 패스도 삭제»)로 화면째 삭제 · 표 ㉓·㉔ 도 폐기

        // ⑧ 공통
        public const float BodyMarginX = 3.0f, PopupW = 87.0f, PopupMarginX = 6.5f;

        /// <summary>
        /// 전투 HUD «얻은 특전 미리보기 줄»(<see cref="HudPerkStrip"/>) 안 치수 — 비례 정본 = aaaw index.html #perkStrip/.pv-ic/.pv-more CSS(390×844 프레임):
        /// 줄 높이 34px · 셀 28×28 · 간격 4 · 개수 배지 14(오른쪽 위) · «+N» 높이 28 · 좌우 안쪽 7 · 글자 12. 픽셀을 박지 않고 <b>줄의 실제 높이</b>에서 비례로 계산한다(T13 · 해상도가 달라도 유지).
        /// 표시 개수도 상수가 아니라 줄 폭 ÷ (셀+간격) — «+N» 칸까지 넣어 절대 넘치지 않는다. 순수 C# 이라 dotnet 테스트가 검증한다.
        /// </summary>
        public struct PerkStripSpec
        {
            public const float RefRow = 34f, RefCell = 28f, RefGap = 4f, RefBadge = 14f, RefPad = 7f, RefFont = 12f, RefBadgeFont = 10f;
            public float Width, Height, Cell, Gap, Badge, BadgeFont, Pad, Font;
            public PerkStripSpec(float width, float height)
            {
                Width = width; Height = height;
                Cell = height * (RefCell / RefRow); Gap = height * (RefGap / RefRow); Badge = height * (RefBadge / RefRow); Pad = height * (RefPad / RefRow);
                // «+N» 글자는 보조 라벨 하한(T63 · TextSize.Aux 36)을 밑돌지 않는다 — MoreWidth 가 같은 값을 쓰므로 칸 폭과 글자가 어긋나지 않는다. 배지 글자는 아이콘 위 배지(TextKind.Small)라 비례 그대로.
                Font = System.Math.Max(TextSize.Aux, (float)System.Math.Round(height * (RefFont / RefRow))); BadgeFont = System.Math.Max(8f, (float)System.Math.Round(height * (RefBadgeFont / RefRow)));
            }
            /// <summary>«+N» 칸 폭 — 좌우 안쪽 ×2 + 글자('+' 와 숫자 자릿수 · 글꼴 크기 ×0.62/자).</summary>
            public float MoreWidth(int rest) => Pad * 2f + (1 + System.Math.Max(1, rest).ToString().Length) * Font * 0.62f;
            /// <summary>줄에 셀만 채울 때 들어가는 최대 개수 — n·셀 + (n−1)·간격 ≤ 폭.</summary>
            public int Fit => Cell + Gap <= 0 ? 0 : (int)System.Math.Floor((Width + Gap + 0.01f) / (Cell + Gap));
            /// <summary>total 개 중 몇 개를 보이나 — 다 들어가면 전부, 아니면 «+N» 칸(남는 개수의 자릿수 기준 · 넉넉히 total 자릿수)까지 포함해 넘치지 않는 개수.</summary>
            public int Shown(int total)
            {
                if (total <= 0) return 0;
                if (total <= Fit) return total;
                float pitch = Cell + Gap; if (pitch <= 0) return 0;
                int shown = (int)System.Math.Floor((Width - MoreWidth(total) + 0.01f) / pitch);
                return System.Math.Max(0, System.Math.Min(shown, total - 1));
            }
            /// <summary>보이는 셀 n개 + («+N» 이 있으면) 그 칸까지의 전체 폭.</summary>
            public float UsedWidth(int total)
            {
                int n = Shown(total); float w = n * Cell + System.Math.Max(0, n - 1) * Gap;
                if (n < total) w += (n > 0 ? Gap : 0) + MoreWidth(total - n);
                return w;
            }
        }
    }
}
