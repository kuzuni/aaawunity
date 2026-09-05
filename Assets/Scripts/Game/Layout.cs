namespace KkomaKnight.Game
{
    /// <summary>
    /// aaaw docs/ui/ref-layout.md 의 «자» — 요소별 x/y/w/h (프레임 %). 여기 값이 배치의 단일 정본이고 화면 코드는 이 상수만 쓴다.
    /// (레퍼런스 jpg 1080×2340 · 프레임 390×844 — 둘 다 좌상 0%·우하 100% 로 환산한 값. 판독 오차 ±0.5%p)
    /// </summary>
    public static class Layout
    {
        public struct R { public float X, Y, W, H; public R(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; } }

        // ① 로비
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

        // ② 인게임
        public static readonly R HudPills = new R(2.0f, 5.2f, 36.0f, 2.8f);
        public static readonly R HudMenu = new R(88.0f, 5.2f, 9.5f, 3.3f);
        public static readonly R HudChapTitle = new R(36.0f, 11.0f, 28.0f, 2.6f);
        public static readonly R HudProgress = new R(22.0f, 14.2f, 56.0f, 1.0f);
        public static readonly R HudBuffBar = new R(2.0f, 17.5f, 14.0f, 30.0f);
        public static readonly R GroundBand = new R(0, 30.0f, 100, 21.0f);
        public const float PlayerFootY = 40.0f, EnemyRowY = 40.0f, PlayerHeight = 9.0f, EnemyHeight = 9.0f, EnemyHeightBald = 7.5f;
        public const float PlayerCenterX = 16.0f, FirstEnemyCenterX = 33.4f;
        public const float PlayerFootBarW = 10.3f, EnemyFootBarW = 9.7f;
        public static readonly R HudSpeed = new R(3.0f, 65.0f, 11.0f, 4.0f);
        public static readonly R HudRound = new R(85.0f, 63.0f, 13.0f, 6.5f);
        public static readonly R HudPanel = new R(0, 69.5f, 100, 30.5f);
        public static readonly R HudExp = new R(3.0f, 70.5f, 26.0f, 3.7f);
        public static readonly R HudHp = new R(31.0f, 70.5f, 32.0f, 3.7f);
        public static readonly R HudSh = new R(65.0f, 70.5f, 32.0f, 3.7f);
        public static readonly R HudStats = new R(3.0f, 75.0f, 94.0f, 22.0f);
        public const float HudStatRowPitch = 5.2f, HudStatCellW = 47.0f, HudStatCellH = 5.2f, HudStatColR = 50.0f;
        public static readonly R HudInfo = new R(85.0f, 94.5f, 10.0f, 4.0f);
        public static readonly R HudPerkStrip = new R(3.0f, 94.5f, 80.0f, 4.0f);

        // ③ 장비 탭
        public static readonly R GearStage = new R(0, 8.5f, 100, 26.5f);
        public static readonly R GearSlotColL = new R(8.5f, 9.5f, 14.0f, 21.0f);
        public static readonly R GearSlotColR = new R(77.5f, 9.5f, 14.0f, 21.0f);
        public const float GearSlotPitch = 7.3f, GearSlotH = 6.3f;
        public static readonly R GearHero = new R(35.0f, 14.0f, 30.0f, 19.0f);
        public static readonly R GearStats = new R(10.0f, 36.5f, 79.0f, 4.0f);
        public static readonly R GearForgeBtn = new R(70.0f, 42.3f, 27.0f, 4.2f);
        public static readonly R GearInv = new R(3.0f, 47.5f, 94.0f, 44.5f);
        public const int GearInvCols = 5; public const float GearInvRowPitch = 7.6f, GearInvCellW = 18.4f, GearInvCellH = 7.2f, GearInvGap = 0.6f;

        // ④ 장비 세부 팝업
        public static readonly R GdBox = new R(6.5f, 28.0f, 87.0f, 44.0f);
        public static readonly R GdBadge = new R(39.0f, 27.5f, 22.0f, 2.3f);
        public static readonly R GdIcon = new R(11.0f, 30.5f, 15.0f, 7.0f);
        public static readonly R GdName = new R(28.0f, 31.0f, 50.0f, 3.0f);
        public static readonly R GdMeta = new R(29.0f, 34.5f, 60.0f, 3.0f);
        public static readonly R GdStats = new R(11.0f, 39.5f, 78.0f, 9.5f);
        public static readonly R GdOpts = new R(11.0f, 48.0f, 78.0f, 14.0f);
        public const float GdOptPitch = 2.4f;
        public static readonly R GdCost = new R(11.0f, 62.5f, 78.0f, 3.0f);
        public static readonly R GdBtnL = new R(15.5f, 66.0f, 33.0f, 6.0f);
        public static readonly R GdBtnR = new R(51.5f, 66.0f, 33.0f, 6.0f);
        public static readonly R GdClose = new R(30.0f, 91.5f, 40.0f, 2.0f);

        // ⑤ 상점
        public static readonly R ShopFreeRow = new R(3.0f, 12.5f, 94.0f, 6.5f);
        public static readonly R ShopSec1 = new R(0, 22.5f, 100, 2.5f);
        public static readonly R ShopCardRow1 = new R(3.0f, 27.0f, 94.0f, 18.5f);
        public static readonly R ShopCardRow2 = new R(3.0f, 47.5f, 94.0f, 18.5f);
        public static readonly R ShopSec2 = new R(0, 66.0f, 100, 2.5f);
        public static readonly R ShopCardRow3 = new R(3.0f, 70.5f, 94.0f, 18.5f);
        public const float ShopCardW = 30.0f, ShopCardGap = 2.0f;

        // ⑥ 대장간 (합성) — 참고 장비 합성 업글창.jpg
        public static readonly R ForgeStage = new R(0, 8.5f, 100, 33.0f);
        public static readonly R ForgeResult = new R(9.0f, 12.0f, 21.0f, 9.5f);
        public static readonly R ForgeArrow = new R(16.5f, 22.5f, 6.0f, 3.0f);
        public static readonly R ForgeMats = new R(9.0f, 26.5f, 60.0f, 8.0f);
        public static readonly R ForgeBanner = new R(38.0f, 12.0f, 54.0f, 10.0f);
        public static readonly R ForgeAuto = new R(2.5f, 42.0f, 27.5f, 4.2f);
        public static readonly R ForgeFuse = new R(70.0f, 42.0f, 27.5f, 4.2f);
        public static readonly R ForgeInv = new R(3.0f, 47.5f, 94.0f, 42.0f);
        public static readonly R ForgeBack = new R(3.0f, 92.5f, 16.0f, 5.0f);

        // ⑦ 팝업 공통 (레벨업 3택 · 이벤트)
        public static readonly R OvStats = new R(0, 4.0f, 100, 6.0f);
        public static readonly R OvBanner = new R(10.0f, 14.0f, 80.0f, 6.0f);
        public static readonly R OvSub = new R(10.0f, 20.5f, 80.0f, 3.0f);
        public static readonly R OvCards = new R(7.0f, 26.0f, 86.0f, 52.0f);
        public static readonly R OvFoot = new R(30.0f, 82.0f, 40.0f, 5.0f);
        public static readonly R EvBox = new R(6.0f, 28.0f, 88.0f, 44.0f);
    }
}
