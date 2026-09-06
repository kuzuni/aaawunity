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
        public static readonly R LobbyBanner = new R(24.5f, 9.2f, 51.6f, 5.6f);
        public static readonly R LobbyMenu = new R(88.3f, 9.2f, 9.0f, 4.1f);
        // 사이드 기둥 · 보조 줄은 T68(아이콘 1.5~1.8배 · 칸 폭 ≥75%) + T63-lobby(라벨 36 · 2줄) 가 레퍼런스(16.4×21.5 · 37.2×7.0)에서 ±3%p 안으로 키운 값 — ref-layout ① «⚑ T68 회차 정정»
        public static readonly R LobbySideL = new R(1.4f, 16.0f, 19.0f, 24.0f);
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
        public static readonly R LobbyCastle = new R(0, 70.0f, 20.0f, 8.5f);
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
        public static readonly R HudExp = new R(3.0f, 70.5f, 26.0f, 3.7f);
        public static readonly R HudHp = new R(31.0f, 70.5f, 32.0f, 3.7f);
        public static readonly R HudSh = new R(65.0f, 70.5f, 32.0f, 3.7f);
        public static readonly R HudStats = new R(3.0f, 75.0f, 94.0f, 22.0f);
        public static readonly R HudStatCell = new R(3.0f, 75.0f, 47.0f, 5.2f);
        public const float HudStatRowPitch = 5.2f, HudStatCellW = 47.0f, HudStatCellH = 5.2f, HudStatColR = 50.0f;
        public static readonly R HudInfo = new R(85.0f, 94.5f, 10.0f, 4.0f);
        public static readonly R HudPerkStrip = new R(3.0f, 94.5f, 80.0f, 4.0f);   // 주인 지시 ② (표 밖 · 인포 버튼 행 왼쪽)

        // ③ 장비 탭 — 캐릭터 장비.jpg
        public static readonly R GearStage = new R(0, 8.5f, 100, 26.5f);
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
        public static readonly R ForgeActionBar = new R(0, 42.0f, 100, 5.0f);
        public static readonly R ForgeAuto = new R(2.0f, 42.0f, 28.0f, 4.5f);
        public static readonly R ForgeFuse = new R(70.0f, 42.0f, 27.5f, 4.5f);
        public static readonly R ForgeInv = new R(3.0f, 47.5f, 94.0f, 44.5f);
        public static readonly R ForgeBack = new R(3.0f, 93.5f, 17.0f, 5.0f);

        // ⑦ 특전 — perks.jpg (선택창: 상자 없음 · 카드가 화면에 직접) · perks 뭐뭐 있는지.jpg (인포 팝업: 상자)
        public static readonly R OvStats = new R(0, 4.0f, 100, 6.0f);
        public static readonly R OvStatCell = new R(0, 4.0f, 12.5f, 6.0f);
        public static readonly R OvBanner = new R(20.0f, 26.5f, 60.0f, 3.5f);
        public static readonly R OvSub = new R(30.0f, 31.5f, 40.0f, 3.0f);
        public static readonly R OvCard1 = new R(5.5f, 36.5f, 89.0f, 11.0f);
        public static readonly R OvCard2 = new R(5.5f, 49.5f, 89.0f, 11.0f);
        public static readonly R OvCard3 = new R(5.5f, 62.5f, 89.0f, 11.0f);
        public const float OvCardPitch = 13.0f;
        public static readonly R OvCards = new R(5.5f, 36.5f, 89.0f, 37.0f);
        public static readonly R OvCardIcon = new R(8.0f, 38.0f, 14.0f, 8.0f);
        public static readonly R OvCardText = new R(25.0f, 38.5f, 68.0f, 7.0f);
        public static readonly R OvFoot = new R(31.0f, 79.0f, 38.0f, 7.5f);
        /// <summary>«남은 횟수 : N» — 예외적으로 <b>버튼(OvFoot) 칸의 %</b> 다(그 자식이라 버튼과 같이 뜨고 같이 커진다). 레퍼런스 04 처럼 버튼 «아래»: y 104% = 버튼 아래끝에서 프레임 0.3% 띄우고, h 46% = 프레임 3.45%(80px · 본문 40 한 줄 55px 이 든다) · w 120% 는 «남은 횟수 : 1»(≈330px)이 버튼 폭 410px 안에서 한 줄로 남게 하는 여유.</summary>
        public static readonly R OvFootRemain = new R(-10.0f, 104.0f, 120.0f, 46.0f);
        public static readonly R OvInfo = new R(86.0f, 79.5f, 9.0f, 6.0f);
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
        public static readonly R SetBox = new R(5.8f, 39.6f, 88.3f, 21.0f);
        public static readonly R SetRibbon = new R(25.0f, 37.5f, 50.0f, 4.2f);
        public static readonly R SetRowMusic = new R(13.9f, 43.0f, 72.5f, 3.8f);
        public static readonly R SetRowSound = new R(13.9f, 47.8f, 72.5f, 3.8f);
        public static readonly R SetRowLang = new R(13.9f, 52.6f, 72.5f, 3.8f);
        public const float SetRowPitch = 4.8f;
        public static readonly R SetToggle = new R(72.9f, 43.3f, 13.5f, 3.1f);
        /// <summary>언어 버튼 — 표 ⑨ 는 3.5(81.8px · 조각 글자 칸은 −30 이라 51.8px)라 버튼 글자 46 의 줄 높이 57.5px 를 bestFit 이 41 로 줄였다 → 3.8(칸 58.8px · 안 줄임 · 표와 차 0.3). T63-settings.</summary>
        public static readonly R SetLangBtn = new R(63.9f, 52.7f, 22.5f, 3.8f);
        /// <summary>패널 밖 링크 2 — 높이는 본문 40 의 줄 높이(50px)가 들어가게 2.4(56px · 표 1.9 = 44.4px 에선 세로로 잘렸다). 개인정보 링크의 폭은 한국어 문구 실측(268px = 24.8%)에 여유를 둔 25.5(가운데 50% 고정) — 표 ⑨ 의 20.3 은 영문 «Privacy Policy» 실측이다(ref-layout ⑨ 회차 정정). T63-settings.</summary>
        public static readonly R SetPrivacy = new R(37.2f, 62.6f, 25.5f, 2.4f);
        public static readonly R SetTerms = new R(40.6f, 67.3f, 18.8f, 2.4f);
        /// <summary>레퍼런스에 없는 줄 — T29 «데이터 삭제»(로비) / 전투 일시정지의 «재개»·«포기하고 로비로» 가 링크 아래 이 줄에 선다.</summary>
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
        public static readonly R DgCard2 = new R(3.6f, 47.1f, 92.6f, 26.5f);
        public const float DgCardPitch = 26.7f;
        public static readonly R DgCardHead = new R(3.6f, 20.4f, 92.6f, 3.6f);
        public static readonly R DgCardPic = new R(3.6f, 24.0f, 92.6f, 15.9f);
        public static readonly R DgEnter = new R(63.5f, 38.6f, 29.0f, 5.3f);
        public static readonly R DgRewards = new R(5.0f, 41.1f, 20.0f, 3.4f);
        public static readonly R DgSoon = new R(3.6f, 73.8f, 92.6f, 19.7f);
        /// <summary>던전·아레나 화면 공통 바닥 띠(레퍼런스에 5탭 바가 없다 — 뒤로 + 2탭) · 뒤로 · 던전/PvP 2탭.</summary>
        public static readonly R DgFoot = new R(0, 92.3f, 100, 7.7f);
        public static readonly R DgBack = new R(3.5f, 94.0f, 16.0f, 5.3f);
        public static readonly R DgTabs = new R(59.7f, 92.3f, 40.3f, 7.7f);

        // ⑬ 던전 세부 팝업 — docs/ref/21_dungeon_detail.jpg (T43)
        public static readonly R DdBox = new R(12.4f, 24.2f, 75.5f, 51.4f);
        public static readonly R DdHead = new R(12.4f, 24.2f, 75.5f, 3.7f);
        public static readonly R DdPic = new R(13.0f, 28.2f, 74.4f, 13.8f);
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
        public static readonly R ArSoon = new R(3.6f, 47.1f, 92.6f, 25.0f);

        // ⑮ 아레나 입장 화면 — docs/ref/23_arena_enter.jpg (T43)
        public static readonly R AeStage = new R(0, 8.0f, 100, 34.0f);
        public static readonly R AeTier = new R(37.5f, 9.9f, 26.0f, 2.9f);
        public static readonly R AeSeason = new R(31.0f, 13.5f, 38.0f, 1.9f);
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
        // ⑳ 퀘스트 팝업 — docs/ref/15_quest.jpg (T44 · ⑳ 표와 같다 · 제목은 리본이 아니라 박스 폭 파란 띠 · 탭 3 은 박스 아래 · 닫기 안내 = BookClose 줄)
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

        // ㉒ 데일리 기프트 팝업 — docs/ref/17_daily_gift.jpg (T44 · ㉒ 표와 같다 · 선물 그림이 리본 위 · 왼쪽 밖 타임라인)
        public static readonly R GfPic = new R(13.3f, 13.1f, 73.4f, 11.9f);
        public static readonly R GfRibbon = new R(4.2f, 24.9f, 91.6f, 3.9f);
        public static readonly R GfBox = new R(8.6f, 28.8f, 82.8f, 52.6f);
        public static readonly R GfTimer = new R(34.0f, 29.2f, 28.0f, 1.6f);
        public static readonly R GfTodayCell = new R(12.8f, 31.5f, 74.4f, 8.1f);
        public static readonly R GfRow1 = new R(19.4f, 40.1f, 67.8f, 9.3f);
        public static readonly R GfRow2 = new R(19.4f, 50.1f, 67.8f, 9.3f);
        public const float GfRowPitch = 10.0f; public const int GfRowCount = 4;
        public static readonly R GfRowTitle = new R(21.1f, 40.5f, 19.2f, 1.7f);
        public static readonly R GfRowBar = new R(21.1f, 42.8f, 63.9f, 1.3f);
        public static readonly R GfRowReward = new R(21.5f, 44.7f, 9.3f, 4.0f);
        public static readonly R GfRowCheck = new R(66.7f, 44.9f, 9.4f, 3.3f);
        public static readonly R GfTimeline = new R(14.0f, 44.6f, 1.5f, 30.4f);
        public static readonly R GfTimelineDot = new R(12.2f, 43.8f, 5.0f, 2.0f);

        // ㉓ 7일 챌린지 팝업 — docs/ref/18_challenge7.jpg (T44 · ㉓ 표와 같다 · 빨간 리본 · 배너 그림 · 왼쪽 일차 탭 7 + 오른쪽 과제 줄)
        public static readonly R C7Ribbon = new R(3.0f, 15.9f, 94.0f, 4.3f);
        public static readonly R C7Box = new R(6.4f, 20.2f, 87.2f, 61.2f);
        public static readonly R C7Timer = new R(36.0f, 20.8f, 28.0f, 1.6f);
        public static readonly R C7Banner = new R(6.4f, 23.4f, 87.2f, 16.2f);
        public static readonly R C7Info = new R(86.1f, 24.2f, 5.6f, 2.6f);
        public static readonly R C7TrackIcons = new R(12.8f, 41.3f, 74.4f, 3.6f);
        public static readonly R C7TrackIcon = new R(12.8f, 41.3f, 7.3f, 3.6f);
        public const int C7TrackCount = 6; public const float C7TrackPitch = 13.42f;
        public static readonly R C7TrackNums = new R(12.8f, 45.2f, 74.4f, 1.5f);
        public static readonly R C7DayTabs = new R(10.6f, 48.7f, 15.0f, 30.8f);
        public static readonly R C7DayTab = new R(10.6f, 48.7f, 15.0f, 3.5f);
        public const int C7DayCount = 7; public const float C7DayPitch = 4.55f;
        public static readonly R C7ListBox = new R(30.0f, 48.1f, 60.3f, 32.0f);
        public static readonly R C7Row1 = new R(30.6f, 48.7f, 59.0f, 8.0f);
        public static readonly R C7Row2 = new R(30.6f, 57.6f, 59.0f, 8.0f);
        public const float C7RowPitch = 8.9f; public const int C7RowCount = 4;
        public static readonly R C7RowTitle = new R(33.3f, 49.0f, 29.2f, 1.6f);
        public static readonly R C7RowRewards = new R(33.3f, 51.8f, 19.2f, 4.5f);
        public static readonly R C7RowReward = new R(33.3f, 51.8f, 9.1f, 4.5f);
        public const float C7RowRewardPitch = 10.0f;
        public static readonly R C7RowGo = new R(66.7f, 52.2f, 20.1f, 3.6f);

        // ㉔ 패스 페이지 — docs/ref/19_pass.jpg (T44 · ㉔ 표와 같다 · 상단 바 = LobbyTopBar · 탭 바 없음 · 3열 세로 트랙 스크롤)
        public static readonly R PsBanner = new R(0, 8.3f, 100, 19.6f);
        public static readonly R PsTitle = new R(26.4f, 12.8f, 47.2f, 2.6f);
        public static readonly R PsRemain = new R(33.0f, 19.6f, 34.0f, 1.4f);
        public static readonly R PsBar = new R(23.6f, 21.3f, 47.4f, 1.9f);
        public static readonly R PsBarIcon = new R(16.0f, 20.6f, 7.6f, 3.1f);
        public static readonly R PsLevelBadge = new R(76.4f, 20.5f, 7.6f, 3.2f);
        public static readonly R PsHint = new R(0, 24.4f, 100, 2.5f);
        public static readonly R PsBrownBand = new R(0, 27.9f, 100, 3.5f);
        public static readonly R PsTrack = new R(2.0f, 31.4f, 96.0f, 60.9f);
        /// <summary>3열 x 경계(프레임 %): 무료 1.9~32.4 · 노란 레벨 줄 32.4~35.4 · 유료1 35.4~66.0 · 유료2 66.0~98.0.</summary>
        public static readonly R PsColFree = new R(1.9f, 31.4f, 30.5f, 60.9f);
        public static readonly R PsColPaid1 = new R(35.4f, 31.4f, 30.6f, 60.9f);
        public static readonly R PsColPaid2 = new R(66.0f, 31.4f, 32.0f, 60.9f);
        public static readonly R PsLevelLine = new R(32.4f, 31.4f, 3.0f, 60.9f);
        public static readonly R PsLevelBadgeRow = new R(29.4f, 36.5f, 7.8f, 3.6f);
        public static readonly R PsRow1 = new R(11.1f, 35.4f, 77.5f, 5.8f);
        public static readonly R PsCellFree = new R(11.1f, 35.4f, 12.5f, 5.8f);
        public static readonly R PsCellPaid = new R(43.8f, 35.4f, 12.5f, 5.8f);
        public const float PsColPitch = 32.5f, PsRowPitch = 10.3f; public const int PsRowCount = 8;
        public static readonly R PsCurPill = new R(24.3f, 72.6f, 18.1f, 3.0f);
        public static readonly R PsClaimAll = new R(3.9f, 86.2f, 27.8f, 4.8f);
        public static readonly R PsBuy1 = new R(35.8f, 86.2f, 28.4f, 4.8f);
        public static readonly R PsBuy2 = new R(68.1f, 86.2f, 28.4f, 4.8f);
        public static readonly R PsBack = new R(2.5f, 93.7f, 16.9f, 5.3f);
        public static readonly R PsBannerTab = new R(80.6f, 92.3f, 18.3f, 7.7f);
        public static readonly R PsFootBand = new R(0, 92.3f, 100, 7.7f);

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
