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
            public override string ToString() => $"x{X} y{Y} w{W} h{H}";
        }

        // ① 로비 — 메인로비.jpg
        public static readonly R LobbyTopBar = new R(0, 3.7f, 100, 4.5f);
        public static readonly R LobbyAvatar = new R(2.5f, 3.8f, 10.2f, 4.4f);
        public static readonly R LobbyPills = new R(13.2f, 4.5f, 85.4f, 2.9f);
        public static readonly R LobbyBanner = new R(24.5f, 9.2f, 51.6f, 5.6f);
        public static readonly R LobbyMenu = new R(88.3f, 9.2f, 9.0f, 4.1f);
        public static readonly R LobbySideL = new R(1.4f, 16.0f, 16.4f, 21.5f);
        public static readonly R LobbySideR = new R(82.5f, 16.0f, 16.4f, 21.5f);
        public static readonly R LobbyChapTitle = new R(34.7f, 27.2f, 31.3f, 2.3f);
        public static readonly R LobbyChapUnderline = new R(29.6f, 30.0f, 41.0f, 0.9f);
        public static readonly R LobbyCard = new R(27.9f, 41.0f, 44.5f, 13.7f);
        public static readonly R LobbyArrowL = new R(17.0f, 45.7f, 6.7f, 4.3f);
        public static readonly R LobbyArrowR = new R(76.3f, 45.7f, 6.7f, 4.3f);
        public static readonly R LobbySubRow = new R(31.3f, 60.2f, 37.2f, 7.0f);
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
        public static readonly R LobbyCastle = new R(0, 70.5f, 17.0f, 7.5f);
        public static readonly R LobbyEvents = new R(83.0f, 70.5f, 17.0f, 7.5f);

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
        /// <summary>발밑 체력바 중심 y(HpLabelY 줄 안) · 그 아래 실드바(파랑) — 주인 지시 2026-09-05 «hp바는 캐릭터 하단 · 실드바는 hp바 밑».</summary>
        public const float FootHpBarY = HpLabelY + 0.9f, FootShBarY = HpLabelY + 2.2f, FootBarH = 0.55f, FootShBarH = 0.4f;
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
        public static readonly R GearSlotColL = new R(8.5f, 9.5f, 14.0f, 21.0f);
        public static readonly R GearSlotColR = new R(77.5f, 9.5f, 14.0f, 21.0f);
        public static readonly R GearSlot = new R(8.5f, 9.5f, 14.0f, 6.3f);
        public const float GearSlotPitch = 7.3f, GearSlotH = 6.3f;
        public static readonly R GearHero = new R(35.0f, 14.0f, 30.0f, 19.0f);
        public static readonly R GearStats = new R(10.0f, 36.5f, 79.0f, 4.0f);
        public static readonly R GearForgeBtn = new R(70.0f, 42.3f, 27.0f, 4.2f);
        public static readonly R GearInv = new R(3.0f, 47.5f, 94.0f, 44.5f);
        public static readonly R GearInvCell = new R(3.0f, 47.8f, 18.4f, 7.2f);
        public const int GearInvCols = 5; public const float GearInvRowPitch = 7.6f, GearInvCellW = 18.4f, GearInvCellH = 7.2f, GearInvGap = 0.6f;

        // ④ 장비 세부 팝업 — 장비 세부팝업.jpg (ov-gear · 닫기는 상자 밖)
        public static readonly R GdBox = new R(6.5f, 28.0f, 87.0f, 44.0f);
        public static readonly R GdBadge = new R(39.0f, 27.5f, 22.0f, 2.3f);
        public static readonly R GdIcon = new R(11.0f, 30.5f, 15.0f, 7.0f);
        public static readonly R GdName = new R(28.0f, 31.0f, 50.0f, 3.0f);
        public static readonly R GdMeta = new R(29.0f, 34.5f, 60.0f, 3.0f);
        public static readonly R GdStats = new R(11.0f, 39.5f, 78.0f, 9.5f);
        public static readonly R GdOpts = new R(11.0f, 48.0f, 78.0f, 14.0f);
        public const float GdOptPitch = 2.4f;
        public static readonly R GdCost = new R(11.0f, 62.5f, 78.0f, 3.0f);
        public static readonly R GdBtns = new R(15.5f, 66.0f, 69.0f, 6.0f);
        public static readonly R GdBtnL = new R(15.5f, 66.0f, 33.0f, 6.0f);
        public static readonly R GdBtnR = new R(51.5f, 66.0f, 33.0f, 6.0f);
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
        public static readonly R OvInfo = new R(86.0f, 79.5f, 9.0f, 6.0f);
        public static readonly R BookBox = new R(6.5f, 23.0f, 87.0f, 52.5f);
        public static readonly R BookRibbon = new R(25.0f, 21.5f, 50.0f, 4.0f);
        public static readonly R BookCard = new R(11.0f, 26.5f, 78.0f, 9.5f);
        public static readonly R BookClose = new R(30.0f, 91.5f, 40.0f, 2.0f);
        /// <summary>이벤트 팝업(쉼터·악마·천사 등) — 표에 없는 화면. ⑧ 공통 «팝업 폭 87 · 좌우 여백 6.5» 와 ④ 의 세로(y28 h44)를 따른다.</summary>
        public static readonly R EvBox = new R(6.5f, 28.0f, 87.0f, 44.0f);

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
                Font = System.Math.Max(8f, (float)System.Math.Round(height * (RefFont / RefRow))); BadgeFont = System.Math.Max(8f, (float)System.Math.Round(height * (RefBadgeFont / RefRow)));
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
