namespace KkomaKnight.Core
{
    /// <summary>
    /// 전투 카메라 원점(월드 x) — <b>«플레이어가 오른쪽으로 걸어가고, 맵·적은 서 있다»</b>.
    /// <para>
    /// ⚑ <b>T510(주인 2026-09-13 «그 플레이어가 실제로 오른쪽으로 가면서 게임이 진행되야하는데 맵이랑 적들이 왼쪽으로 움직이는 방식이더라»)</b> —
    /// 종전 챕터 판은 원점이 <b>플레이어 월드 x 그 자체</b>였다(<c>LayoutX = (worldX − 플레이어 x) × zoom + playerX</c>).
    /// 그러면 플레이어 그림은 <c>ui.json camera.playerX</c>(16%) 한 자리에 <b>붙박이고</b> 세상만 왼쪽으로 흐른다 — 주인이 본 그 꼴이다.
    /// </para>
    /// <para>
    /// 여기서는 원점을 <b>따로 들고</b> 플레이어보다 뒤에 남긴다. 그 «뒤진 거리»(<see cref="Lead"/>)만큼 플레이어 그림이
    /// 화면 오른쪽으로 나아가고, 원점이 안 움직이는 동안 <b>바닥·소품·노드·적은 한 픽셀도 안 흐른다</b>.
    /// 규칙은 셋뿐이다:
    /// <list type="number">
    /// <item>원점은 플레이어를 <b>앞지르지 않는다</b> — 앞지르면 플레이어가 화면 왼쪽 끝(<c>playerX</c>)보다 왼쪽으로 나간다.</item>
    /// <item>플레이어가 띠의 오른쪽 끝(<see cref="MaxLead"/> · <see cref="Layout.BattleCamRightPct"/>)에 닿으면 그때부터 원점이 <b>밀려</b> 같이 간다(무한 스크롤이라 이것 없이는 화면 밖으로 나간다).</item>
    /// <item><b>행군</b>(다음 적이 <see cref="MarchDist"/> 보다 멀다 = 무리와 무리 사이)에서만 원점이 걸음의 <see cref="Layout.BattleCamCatchUp"/> 배로 따라붙어 플레이어를 왼쪽 끝으로 되돌린다 —
    ///       카메라가 다음 무리를 미리 비추고, 새 무리는 늘 왼쪽 끝에서 맞는다(= 무리마다 띠 한 폭을 온전히 걸어간다). <b>무리 안(한 마리 잡고 다음 마리로 가는 걸음)에서는 절대 안 움직인다.</b></item>
    /// </list>
    /// </para>
    /// <para>
    /// ⚑ <b>엔진 좌표·틱·시드는 한 줄도 안 본다</b> — 이 자는 «어디서 보나» 만 정한다(그리기). 그래서 시뮬 골든(<c>BattleTests</c>)과 무관하다.
    /// 순수 C#(Core)에 두는 까닭은 자리 잡기 전체가 PlayMode 라 유니티 없이는 못 도는데, <b>이 규칙만은 순수한 셈</b>이라
    /// EditMode(<c>BattleCamTests</c> · <c>dotnet test</c>)에서 실제 전투를 돌려 가며 잴 수 있기 때문이다(<see cref="Layout.PetGap"/> 과 같은 까닭 · 결정 143).
    /// </para>
    /// </summary>
    public sealed class BattleCam
    {
        /// <summary>플레이어가 원점보다 앞설 수 있는 최대 월드 거리 — 이 거리에서 그림이 띠의 오른쪽 끝에 선다.</summary>
        public double MaxLead { get; }
        /// <summary>
        /// 다음 적까지 이보다 멀면 «행군»(무리 사이)으로 본다 — <c>combat.json</c> 의 <b>멈춤 거리 + 적 간격 × 1.5</b>(74 + 66 = 140 월드 px).
        /// <para>
        /// 잣대: 무리 <b>안</b>의 한 걸음은 «멈춤 거리 + 적 간격»(118)에서 시작하므로 <b>늘 문턱 아래</b>여야 한다(그 걸음에 맵이 흐르면 고친 뜻이 없다).
        /// 무리 <b>사이</b>는 «멈춤 거리 + 노드 간격»(354 · 쉼터·천사·악마면 544)이라 한참 위다. 반 칸(22)은 여벌이다 —
        /// 투사체가 앞의 적을 먼저 잡아 다음 표적이 한 칸 건너뛸 때(162) 를 품는다.
        /// </para>
        /// <para>이 문턱 덕에 <b>무리에 들어서는 마지막 44px</b>(= 한 걸음)은 이미 «싸움» 이라 맵이 서고 플레이어가 <b>걸어 들어간다</b> — 그때 띠를 12%p 쓴다.</para>
        /// </summary>
        public double MarchDist { get; }
        readonly double _catchUp, _snapGap;
        /// <summary>카메라 원점(월드 x) — 화면 <c>playerX</c> 자리에 오는 월드 좌표.</summary>
        public double OriginPX { get; private set; }
        /// <summary>플레이어가 원점보다 앞선 거리(월드 px · 0 이면 그림이 띠의 왼쪽 끝에 있다).</summary>
        public double Lead(double playerPX) => playerPX - OriginPX;

        /// <param name="maxLead">띠의 오른쪽 끝까지의 월드 거리(= (오른쪽 끝 레이아웃 x − playerX) 를 zoom·간격 사상으로 되돌린 값).</param>
        /// <param name="marchDist">«행군» 으로 보는 적과의 거리 문턱(<see cref="MarchDist"/>).</param>
        /// <param name="catchUpMul">행군 중 따라붙기 배수(1 = 안 따라붙는다).</param>
        /// <param name="snapGap">이보다 더 벌어지면 연출이 아니라 «복원»(세이브·탭 복귀)으로 보고 즉시 맞춘다.</param>
        public BattleCam(double maxLead, double marchDist, double catchUpMul, double snapGap)
        {
            MaxLead = maxLead > 0 ? maxLead : 0;
            MarchDist = marchDist > 0 ? marchDist : 0;
            _catchUp = catchUpMul < 1 ? 1 : catchUpMul;
            _snapGap = snapGap > 0 ? snapGap : 0;
        }

        /// <summary>원점을 플레이어에게 맞춘다(판 시작 · 복원) — 그림이 띠의 왼쪽 끝에 선다.</summary>
        public void Reset(double playerPX) { OriginPX = playerPX; }

        /// <summary>
        /// 한 프레임 — 원점을 위 세 규칙대로 옮긴다.
        /// </summary>
        /// <param name="playerPX">화면이 쓰는 플레이어 월드 x(<c>BattleWorld.ShownPX</c>).</param>
        /// <param name="advance">이번 프레임에 그 값이 실제로 나아간 거리(≤0 이면 안 걸었다).</param>
        /// <param name="distToTarget">다음(가장 앞) 살아 있는 적까지의 월드 거리 — 없으면 <see cref="double.PositiveInfinity"/>. <see cref="MarchDist"/> 보다 멀면 «행군» 이다.</param>
        /// <param name="snap">따라잡기(탭 복귀)·복원 — 즉시 맞춘다.</param>
        public void Step(double playerPX, double advance, double distToTarget, bool snap = false)
        {
            double lead = playerPX - OriginPX;
            if (snap || lead < 0 || lead > MaxLead + _snapGap) { OriginPX = playerPX; return; }   // 역행·복원은 따질 것이 없다
            if (advance > 0 && distToTarget > MarchDist)
            {
                OriginPX += advance * _catchUp;                       // ③ 행군에서만 되돌린다
                if (OriginPX > playerPX) OriginPX = playerPX;         // ① 앞지르지 않는다
            }
            if (playerPX - OriginPX > MaxLead) OriginPX = playerPX - MaxLead;   // ② 오른쪽 끝에서 밀린다
        }
    }
}
