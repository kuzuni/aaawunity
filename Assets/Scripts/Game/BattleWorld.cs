using System.Collections.Generic;
using DG.Tweening;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 전투 월드 그리기 — 엔진(<see cref="BattleState"/>)은 숫자만 갖고, 여기서 주인 에셋으로 보여준다.
    /// ● 좌표: sim.js 월드 x(레이아웃 px) → 프레임 레이아웃 x = 플레이어 x + zoom × <see cref="Spread"/>(worldX − 플레이어 x) → <see cref="WorldCam.ToWorld"/>.
    ///   Spread 는 멈춤 거리(stopDistance) 안은 1배, 그 밖은 <see cref="Layout.WorldSpacing"/>(2배)로 벌린다(주인 지시 «적·노드 간격 2배» · 엔진 좌표 불변).
    ///   세로는 ref-layout ② 의 % (발 줄 40%).
    /// ● 맵: Layer Lab Environment 데모 씬 4종(Autumn·DeepForest·Forest·Desert)을 챕터 (n−1)%4 로 순환 — 바닥·길 띠는 데모 치수, 소품은 데모 씬 배치 그대로(<see cref="MapLayouts"/> · tools/gen_maps.py) 씬 폭마다 반복.
    /// ● 캐릭터: CharacterMaker Character.prefab + <see cref="CharacterRig"/>. 키 = 표 %(PlayerHeight·EnemyHeight) × <see cref="Layout.CharScale"/>(2/3 · 발밑 바 폭도 같은 배율 · T14).
    ///   공격 모션은 끊지 않고 간격 안에 끝나게 배속(<see cref="Layout.AttackAnimSpeed"/>), 데미지 연출(팝·플래시·체력바·사망)은 «칼이 내려오는 순간»(Attack.anim OnAttackHit)까지 미룬다(<see cref="Strike"/>).
    ///   사망·승리 클립은 루프 에셋이라 끝에서 Animator 를 멈춘다(<see cref="CharacterRig"/> · T14).
    ///   킬 뒤에는 «칼이 내려옴 → 적 사망 연출 → 플레이어 공격 모션 끝 → 걷기 모션으로 원래 걷기 속도(PlayerSpeed×WalkMul · 대시 특전이면 ×DashMul)» 순서(주인 2026-09-06 · T50·T51).
    ///   그 동안 엔진 틱을 보류(<see cref="HoldEngine"/> · BattleScreen.Tick)하므로 표시 원점 <see cref="ShownPX"/> 과 엔진 x 의 격차가 생기지 않는다 — T20 의 «멈춤 → 2배 따라잡기» 는 폐지. 엔진 좌표·틱 순서 불변.
    /// ● 발밑 바(T35 · 주인 강조 · `02_battle.jpg`): 빨강(HP) 위에 파랑(실드) 2단 · 각 단 안에 흰 숫자(«현재») · 바 폭 = 캐릭터 폭(2/3 배율) · 실드 0 이면 파란 단 숨김.
    ///   숫자는 막대의 자식으로 선 <b>월드 TMP</b> 다(T502 · <see cref="FootText"/>) — 글자 크기는 종전대로 프레임 px 로 재고(<see cref="WorldPerPx"/>) 자리는 막대가 정한다. 데미지 팝(<see cref="Pop"/>)도 같은 꼴로 월드에 뜬다.
    /// </summary>
    public sealed class BattleWorld
    {
        readonly App _app; readonly BattleState G; readonly GameData D;
        readonly Transform _root; readonly RectTransform _pops;   // _pops 는 이제 «팝을 띄우는 화면인가» 표시로만 남았다 — 팝·발밑 숫자는 월드(_root)에 선다(T502)
        Transform _popRoot;   // 데미지 팝이 모이는 월드 자리(«Pops» · _root 의 자식 · 자가 이 이름으로 팝을 센다)
        readonly float _zoom; readonly float _playerX;             // ui.json camera.zoom · playerX(프레임 폭 비율)
        // ⚑ T319(주인 2026-09-09 11:2X «PvP 뜰 때 중앙에서 두 캐릭터 만나서 싸우는 식 · 내 플레이어가 오른쪽으로 이동 느낌이 아니라») —
        //   챕터 판은 «플레이어를 화면 한 자리(_playerX)에 붙들고 세상을 흘려보내는» 꼴이다(LayoutX 의 원점이 _shownPX 라서).
        //   그래서 1대1 판에서도 배경이 흐르고 **내가 오른쪽으로 가는 느낌**이 난다.
        //   아레나 판은 그 반대로 둔다: **원점을 «둘이 만나는 점» 에 못 박고**(배경이 안 흐른다) 화면 가운데를 그 점으로 삼는다 —
        //   그러면 내가 왼쪽에서 걸어 들어와 가운데에서 상대와 마주 선다(엔진 좌표·틱은 한 줄도 안 바뀐다).
        readonly bool _fixedOrigin; readonly double _originPX; readonly float _originScreenX;
        public const float CharBaseHeight = 0.85f;                          // Character.prefab 스케일 1 의 키(유니티 단위 · 조사값)
        const float FootY = Layout.PlayerFootY / 100f;
        const float RoadCenterFrac = 0.41f;                          // 데모 씬의 길 중심(y −0.402)이 놓이는 프레임 비율 — 발 줄 40% 을 품는다(띠는 이 줄을 가운데로 ±RoadBandH/2 = 30.5~51.5% · ref-layout 지면 띠 30~51 과 같은 자리)
        /// <summary>데모 씬 1u 가 화면에서 차지하는 프레임 높이 비율 — 데모 구성을 통째로 <see cref="Layout.MapScale"/>(0.6) 배로 그린다(T19 · 1u → 0.6 유니티 단위 = 프레임의 1/19).</summary>
        const float UnitFrac = WorldCam.PPU * Layout.MapScale / WorldCam.LayoutH;
        /// <summary>
        /// 지면(길) 띠의 프레임 높이(%) — <c>docs/ref-layout.md</c> ② «지면(길) 띠 y30.0 h21.0» 의 h (T215).
        /// <para>
        /// 데모 씬의 길은 <see cref="MapLayouts.RoadScaleY"/> 그대로 두면 <b>3.15u = 16.2%</b> 라 표보다 4.8%p 얇았다(§5 02·03 의 마지막 감점).
        /// «데모 씬 그대로»(T19)를 어기는 것이 아니다 — 길 그림(<c>Road_*.png</c>)은 <b>128×128 이 한 가지 색으로 채워진 판</b>이라
        /// 세로로 늘려도 그림이 상하지 않는다(늘리는 것은 그림이 아니라 «띠의 자리» 다). 레퍼런스 02 와 원본 HTML 게임이 둘 다 30~51% 로 같은 쪽을 가리킨다.
        /// </para>
        /// </summary>
        public const float RoadBandH = 21.0f;
        /// <summary>데모 길 띠의 높이(데모 u) — 그림 1.28u(128px ÷ PPU 100) × 데모 스케일 = 3.15u.</summary>
        public const float RoadDemoH = 1.28f * MapLayouts.RoadScaleY;
        /// <summary>데모 띠(16.2%)를 표 높이(<see cref="RoadBandH"/>)로 만드는 세로 배수 — 길 타일 스케일에 곱한다.</summary>
        public static float RoadStretchY => RoadBandH / 100f / (RoadDemoH * UnitFrac);
        /// <summary>띠가 늘어난 만큼(위·아래 각각) 물결 경계(<c>*.roadUp</c>)도 바깥으로 옮기는 폭(데모 u) — 안 옮기면 띠 경계와 물결이 갈라진다.</summary>
        public static float RoadEdgeShift => RoadDemoH * 0.5f * (RoadStretchY - 1f);
        /// <summary>
        /// 월드 정렬 층(T71) — 바닥 &lt; 길 &lt; 납작(물결 경계·풀꽃) &lt; 길 위쪽 소품(멀수록 뒤 · <see cref="OrderNearProp"/> 에서 1u 당 −3 · 하한 <see cref="OrderFarProp"/>) &lt; 캐릭터 &lt; 길 아래쪽 소품(381~).
        /// 예전엔 바닥 −20 · 위쪽 소품 하한 −60 이라 발 줄에서 2.7u 이상 위(y ≥ 2.46 · 테마마다 큰 나무 5~10그루)의 소품이 바닥 뒤로 숨었다 — 주인 «위쪽엔 나무가 적다» 의 원인.
        /// </summary>
        public const int OrderField = -40, OrderRoad = -38, OrderFlat = -36, OrderFarProp = -35, OrderNearProp = -12;
        const float SpreadRamp = 150f;                              // 1배 → WorldSpacing 배로 부드럽게 넘어가는 월드 px 구간

        // 플레이어
        CharacterRig _player; SpriteRenderer _pBarBg, _pBarFill, _pShBg, _pShFill; TMP_Text _pHpTxt, _pShTxt; double _pStrikeTick; bool _pDeadShown; EnemyState _pTarget;
        int _holdPlayer;                                             // 아직 «칼이 안 내려온» 적 공격 수 — 0 일 때만 표시 체력을 엔진 값으로 맞춘다

        // 펫 — 장착 펫이 플레이어 뒤를 따라 걷는다(T293 9항 · 주인 «플레이어 뒤에 따라오는 느낌»).
        // ⚑ 엔진 상태를 하나도 안 만든다 — 시뮬은 펫이 있든 없든 한 톨도 안 달라지고, 여기 있는 것은 «플레이어를 따라 그리는 그림» 뿐이다.
        readonly List<CharacterRig> _pets = new List<CharacterRig>();
        PetData _petData;
        // 캐릭터 프리팹 안 조각 순서가 0~13 이라 이만큼 내려야 펫이 «통째로» 플레이어 뒤에 간다.
        // (자리로만 정하면 안 된다 — SortBase 는 왼쪽일수록 앞이라, 뒤에 선 펫이 오히려 앞에 그려진다.)
        const int PetSortBack = 14;
        /// <summary>자·진단용 — 지금 세워 둔 펫 리그(읽기 전용 · 미장착이면 빈 목록).</summary>
        public IReadOnlyList<CharacterRig> PetRigs => _pets;
        public double ShownHp { get; private set; } public double ShownSh { get; private set; }
        /// <summary>플레이어 발밑 2단 바(T35) — 테스트·진단용 읽기: 빨강 HP 바 · 파랑 실드 바 · 각 단 안의 숫자 글자.</summary>
        public SpriteRenderer PlayerHpBar => _pBarBg; public SpriteRenderer PlayerShBar => _pShBg; public TMP_Text PlayerHpText => _pHpTxt; public TMP_Text PlayerShText => _pShTxt;
        /// <summary>적과 조우 중인가(살아 있는 적이 화면 안) — HUD 상단 진행바가 이때 주황으로 찬다(T35 · 레퍼런스 03).</summary>
        public bool Engaged { get; private set; }
        // 적
        sealed class EnemyView { public EnemyState E; public CharacterRig Rig; public SpriteRenderer BarBg, BarFill; public TMP_Text BarTxt; public double StrikeTick; public float DieT = -1; public GameObject StunFx; public double ShownHp; public int Hold; }
        readonly Dictionary<EnemyState, EnemyView> _enemies = new Dictionary<EnemyState, EnemyView>();
        // 연출 지연 — 공격 모션의 타격 순간까지 묶어 두는 이벤트
        sealed class Strike { public CharacterRig Rig; public int HitCount0; public float At; public EnemyState Target; public bool OnPlayer; public readonly List<BattleEvent> Evs = new List<BattleEvent>(); }
        readonly List<Strike> _strikes = new List<Strike>();
        Strike _pStrike; readonly Dictionary<EnemyState, Strike> _eStrikes = new Dictionary<EnemyState, Strike>();
        float _clock;
        /// <summary>타격 연출이 아직 남아 있나 — 화면(BattleScreen)은 이 동안 팝업(레벨업·사망)을 열지 않고 기다린다.</summary>
        public bool Busy => _strikes.Count > 0;
        // T20/T50 — 표시 기준 x(스크롤 원점). 엔진은 킬 다음 틱(1/30초)에 바로 다음 적으로 걷지만(Battle.Tick · alive[0] · sim.js 와 동일 · 불변),
        // 화면은 사망 연출을 «칼이 내려오는 순간»(Strike · Hold)까지 미루고(T20), 그 뒤 플레이어 공격 모션이 끝날 때까지 서 있다가 걷기 모션으로 원래 속도로 출발해야 한다(주인 2026-09-06 · T50).
        // T20 은 그 동안 원점만 멈추고 풀리면 걷기 2배로 엔진을 따라잡았다 → 주인이 «2배 걸음» 을 거부(T50). 지금은 화면이 멈춰 있는 동안 엔진 틱 자체를 보류한다(HoldEngine · BattleScreen.Tick):
        // 틱 순서는 그대로라 시뮬 결과(시드 골든)는 불변이고 실시간 길이만 늘어나며, 원점은 늘 엔진 x 와 같다(격차 0 → 걷는 속도 = 엔진 속도 = PlayerSpeed×WalkMul×(Dash?DashMul:1) 그대로).
        // 탭 복귀(Silent)·역행·SnapGap 초과(세이브 복원 등)만 즉시 맞춘다.
        double _shownPX;
        const double SnapGap = 600;
        bool _killAnimHold;                                          // 킬 타격(칼 내려옴)이 나온 뒤 공격 모션이 끝날 때까지 — 출발 금지 + 엔진 보류(T50)
        // T65 — 멈춤이 «시작되는» 프레임은 얼리지 않고 엔진 x 로 맞춘다. 한 프레임에 엔진 틱이 여럿 돌면(배속·낮은 fps) 킬 틱 앞에 걷기 틱이 같은 프레임에 이미 들어 있고,
        // 그 프레임부터 얼리면 그 걸음(≤ 한 프레임 분)만큼 격차가 남는다(CI #91~#93 빨강 = 격차 4.4px = 걷기 한 틱 132×1/30). 다음 프레임부터 얼리면 격차 0 이 유지된다.
        bool _heldPrevFrame;
        /// <summary>화면이 쓰는 플레이어 월드 x(스크롤 원점) — 엔진 <c>P.WorldX</c> 와 같다(킬 연출 중에는 엔진이 보류되므로 격차가 생기지 않는다).</summary>
        public double ShownPX => _shownPX;
        /// <summary>죽었지만 아직 사망 연출이 시작되지 않은(칼이 안 내려온) 적이 있는가 — 이 동안 화면은 출발하지 않는다(T20).</summary>
        public bool KillPending { get { foreach (var kv in _enemies) if (kv.Key.Dead && kv.Value.Hold > 0) return true; return false; } }
        /// <summary>킬 타격이 나온 뒤 플레이어 공격 모션이 아직 끝나지 않았는가 — 이 동안 화면은 출발하지 않는다(T50 · 대시 특전도 같다 · T51).</summary>
        public bool KillAnimHold => _killAnimHold && _player != null && _player.Attacking;
        /// <summary>엔진 틱을 보류해야 하는가 = 킬 연출 대기(<see cref="KillPending"/>) 또는 킬 뒤 공격 모션(<see cref="KillAnimHold"/>) — BattleScreen.Tick 이 이 동안 틱을 돌리지 않는다(탭 복귀 따라잡기 <see cref="Silent"/> 는 예외).</summary>
        /// <summary>
        /// T457(주인 2026-09-12 «그 특전 팝업 뜨기 전에 전투나 이동은 바로 전까지 계속 됐어야 함» · «여전히 특전 뜨기 전에 계속 움직이는 거랑 되고 있게 하라니까 · 특전 딱 떴을 때 게임 정지되는 느낌으로» · «경험치 흡수하는 효과 나오면서부터 이미 이동하는 거랑 공격 멈추고 있잖아») — 킬 연출(칼 내려오기 전 · 킬 뒤 공격 모션) 동안 엔진 틱을 세우던 T50 의 보류를 <b>기본으로 끈다</b>.
        /// 주인이 본 «흡수 효과가 나오면서부터 이동·공격이 멈춘다» 가 바로 이 보류였다 — 마지막 적을 잡는 그 순간부터 특전 창이 뜰 때까지
        /// 엔진이 서 있었다(T368 은 «창을 늦게 연다» 만 했고 이 보류는 그대로였다). 이제 멈추는 것은 특전 창이 실제로 뜰 때(팝업 timeScale 0 · T3)뿐이다.
        /// <para>참이면 옛 T50 꼴로 돌아간다 — 그 꼴의 자(<c>BattleWorldTests</c> 의 킬 보류 갈래)가 켜고 잰다. 게임은 늘 거짓.</para>
        /// </summary>
        public static bool HoldEngineOnKill = false;
        public bool HoldEngine => HoldEngineOnKill && !Silent && (KillPending || KillAnimHold);
        /// <summary>플레이어 리그의 현재 애니 상태 이름(테스트·진단용).</summary>
        public string PlayerAnim => _player != null ? _player.Current : null;
        /// <summary>
        /// T85 — 적의 «사망 연출이 시작되는» 순간(칼이 내려온 뒤 · 자리 · 보스인가)을 화면에 알린다.
        /// 엔진은 킬 틱에 이미 경험치·골드를 올렸지만(불변) 보상 구슬은 시체가 실제로 쓰러지는 이 순간에 튀어나온다.
        /// 따라잡기(<see cref="Silent"/>) 중에는 부르지 않는다 — 그때는 화면이 표시값을 즉시 맞춘다.
        /// </summary>
        public System.Action<Vector3, bool> KillShown;
        /// <summary>배속(x1/x2) — 애니 속도와 지연 시계에 함께 건다.</summary>
        public float TimeScale = 1f;
        /// <summary>따라잡기 중(탭 숨김 뒤 복귀) — 공격 모션·팝·이펙트를 만들지 않고 이벤트만 비운다.</summary>
        public bool Silent;
        /// <summary>
        /// 엔진 시간이 «흐르는 중» 인가 — 팝업이 떠 있거나 일시정지·판이 끝난 프레임이면 false(BattleScreen.Tick 이 매 프레임 넣어 준다).
        /// T86 ⓐ: 투사체 표시 x 는 이 동안에만 전진한다(킬 연출로 엔진 틱이 보류된 <see cref="HoldEngine"/> 프레임도 «흐르는 중» 이다 — 그래서 도끼·창이 제자리에 뜨지 않는다).
        /// </summary>
        public bool EngineRunning = true;
        /// <summary>
        /// 전투 배속(x1/x2 · <see cref="BattleScreen"/> 이 매 프레임 넣어 준다 · T108).
        /// 엔진 배속은 <c>Time.timeScale</c> 이 아니라 «한 프레임에 도는 틱 수» 라서, 표시 x 도 같은 배로 가야 엔진과 안 벌어진다
        /// (지시서 T108 1항 «unscaled 아님 = 전투 배속은 따른다»). 예전에는 벌어진 만큼을 <c>shown = pr.X</c> 로 스냅해 메웠고 그게 «튀는 순간» 이었다.
        /// <para>
        /// ⚠ <b>T397 — 걸음에는 이 값을 곱하지 않는다.</b> <see cref="Sync"/> 가 받는 <c>dt</c> 는 <see cref="BattleScreen"/> 이 이미 배속을 곱해 넣는 «엔진 초»다
        /// (<c>_world.Sync(dt * _speed)</c>). 여기서 한 번 더 곱하면 x2 에서 그림이 엔진의 <b>두 배</b>로 날아 «맞는 자리»(<see cref="ProjLimit"/>)에 먼저 닿아
        /// 엔진이 올 때까지 <b>서 있는다</b> — 주인이 본 «적 앞에서 멈추는 경우» 와 «닿았는데 데미지가 늦다» 가 그것이다(결정 1136). 값은 진단·자(배속 읽기)용으로만 남는다.
        /// </para>
        /// </summary>
        public int Speed = 1;
        // 투사체
        readonly Dictionary<Projectile, GameObject> _projs = new Dictionary<Projectile, GameObject>();
        /// <summary>투사체 표시 x(T86 ⓐ) — 엔진 x 와 «같은 px/s»(pr.Spd)로 매 프레임 전진하고, 엔진이 앞서면 엔진을 따르며, 엔진이 맞히는 자리는 앞지르지 않는다.</summary>
        readonly Dictionary<Projectile, double> _projX = new Dictionary<Projectile, double>();
        readonly Dictionary<EnemyArrow, GameObject> _arrows = new Dictionary<EnemyArrow, GameObject>();
        /// <summary>적 화살의 «표시 x»(T179 ⓑ) — <see cref="_projX"/> 와 같은 구실이다. 엔진 좌표(<c>a.X</c>)를 그대로 그리면 킬 연출로 엔진 틱이 보류된 동안 화살이 공중에 선다.</summary>
        readonly Dictionary<EnemyArrow, double> _arrowX = new Dictionary<EnemyArrow, double>();
        // 노드 · 배경
        sealed class NodeView { public BattleNode N; public GameObject Go; public GameObject FxGo; public bool Dimmed; }
        readonly List<NodeView> _nodes = new List<NodeView>();
        readonly List<SpriteRenderer> _fieldTiles = new List<SpriteRenderer>(), _roadTiles = new List<SpriteRenderer>();
        sealed class Prop { public SpriteRenderer Sr; public double WorldX; public float YFrac; }
        readonly List<Prop> _props = new List<Prop>();
        float _tileW; int _tileCols;
        double _goldPrev; Vector3 _lastKillPos;
        readonly Theme _theme;

        /// <summary>데모 씬 한 벌 — 바닥·길 키(env.&lt;name&gt;.field/road · <see cref="MapLayouts"/> 가 굽는다) · 물결 경계·소품 배치는 <see cref="MapLayouts"/> 표.</summary>
        public sealed class Theme
        {
            public string Name;
            public string Field => MapLayouts.FieldOf(Name); public string Road => MapLayouts.RoadOf(Name);
            public static readonly Theme[] All = { new Theme { Name = "autumn" }, new Theme { Name = "deepForest" }, new Theme { Name = "forest" }, new Theme { Name = "desert" } };
            /// <summary>챕터 → 테마: 1=Autumn 2=DeepForest 3=Forest 4=Desert, 5=Autumn … (주인 지시 «4개 순환»).</summary>
            public static Theme ForChapter(int chapter) => All[((chapter - 1) % All.Length + All.Length) % All.Length];
            /// <summary>
            /// T240 1항 — 아레나(PvP) 판의 무대. 주인 레퍼런스 <c>33_pvp_battle.jpg</c> 의 가운데는 <b>모래 마당</b>이고
            /// 챕터 전투(풀밭·숲)와 한눈에 갈려야 한다 — 그래서 챕터 순환에서 빼고 <b>사막 바닥을 고정</b>으로 쓴다(새 그림 0 · §1).
            /// <para>⚠ 이것은 «데모 씬 그대로»(T19)를 어기는 것이 아니다 — 고르는 것이 «어느 챕터냐» 에서 «어느 판이냐» 로 바뀔 뿐 바닥·길 조각과 치수는 그 테마 그대로다.</para>
            /// </summary>
            public static readonly Theme Arena = All[3];
        }

        /// <summary>
        /// 지금 만드는 판이 아레나(PvP) 판인가 — <see cref="BattleScreen.IsArena"/> 를 <b>되읽는다</b>.
        /// <para>
        /// ⚠ <b>왜 생성자 인자가 아닌가</b>: 인자로 받으려면 <c>BattleScreen.cs</c> 의 생성 줄을 고쳐야 하는데
        /// 그 파일은 지금 <b>T254(부활권 · 살아 있는 lock)의 범위</b>다. 남의 lock 안 파일을 «편해서» 고치지 않는다(§1 · claims 규약).
        /// 되읽기가 안전한 까닭은 순서가 이미 못 박혀 있기 때문이다 — <c>BattleScreen.Start</c> 는 <c>_arenaFoe</c> 를 <b>먼저</b> 넣고
        /// 그 뒤에 <c>new BattleWorld(...)</c> 를 부른다(그 반대면 첫 판만 테마가 틀렸을 것이다).
        /// </para>
        /// <para>T254 lock 이 풀리면 <b>생성자 인자 한 개</b>로 바꾸는 것이 맞다(그때는 되읽기도, 이 주석도 지운다).</para>
        /// </summary>
        static bool IsArenaRun(App app)
        {
            var screen = app != null ? app.GetScreen<BattleScreen>() : null;
            return screen != null && screen.IsArena;
        }

        /// <summary>월드 루트(Ground·Props·Nodes 의 부모) — 테스트·진단용 읽기(T19 PlayMode 맵 테스트가 바닥·길·소품 스케일을 본다).</summary>
        public Transform Root => _root;
        bool _isArena;
        /// <summary>이 판이 아레나(PvP) 판인가 — 무대와 상대 외형이 이 값으로 갈린다(T240 1·2항). 테스트·진단용 읽기.</summary>
        public bool IsArena => _isArena;
        /// <summary>이 판의 맵 테마 — 테스트·진단용 읽기.</summary>
        public Theme MapTheme => _theme;

        // ───────────────────────── 비평 하니스 월드 행(T47 ⓑ · ref-layout ② 의 캔버스 밖 행) ─────────────────────────
        /// <summary>월드 스프라이트 Bounds → 프레임 %(좌상 0 · 우하 100) [x, y, w, h]. 카메라 = 프레임과 같은 영역(WorldCam · 세로 LayoutH · 가로는 프레임 비율).</summary>
        static float[] FrameRect(Bounds b)
        {
            float halfH = WorldCam.LayoutH / WorldCam.PPU / 2f, halfW = halfH * UiKit.FrameW / UiKit.FrameH;
            float x = (b.min.x + halfW) / (2f * halfW) * 100f, y = (halfH - b.max.y) / (2f * halfH) * 100f;
            float w = b.size.x / (2f * halfW) * 100f, h = b.size.y / (2f * halfH) * 100f;
            return new[] { R1(x), R1(y), R1(w), R1(h) };
        }
        static float R1(float v) => Mathf.Round(v * 10f) / 10f;
        static float PctX(float worldX) { float halfH = WorldCam.LayoutH / WorldCam.PPU / 2f, halfW = halfH * UiKit.FrameW / UiKit.FrameH; return R1((worldX + halfW) / (2f * halfW) * 100f); }
        static float PctY(float worldY) { float halfH = WorldCam.LayoutH / WorldCam.PPU / 2f; return R1((halfH - worldY) / (2f * halfH) * 100f); }
        /// <summary>
        /// 비평 하니스(T46·T47)가 캔버스 이름표(UiTag)로 못 재는 <b>월드 행</b>을 ref-layout ② 표의 «요소» 이름 그대로 프레임 % 로 돌려준다 — 지면(길) 띠 · 플레이어 발밑 y · 적 행 y · 플레이어/적 높이 · 체력 라벨 줄 · 플레이어 중심 x · 발밑 바 폭 2.
        /// 값은 <b>지금 그려진 것</b>(CharScale 2/3 · 데모 길 띠 × MapScale)이라 표와 다를 수 있다 — 채점이 그 차이를 그대로 보인다(ui_score.py 는 ref 에 값이 있는 축만 비교 · «플레이어 중심 x» 는 x 에 중심을 넣는다).
        /// 적은 화면 안(LayoutW 안)의 살아 있는 적 중 가장 가까운 것 하나 · 없으면 적 행 3개는 뺀다.
        /// </summary>
        public Dictionary<string, float[]> MeasureLayout()
        {
            var d = new Dictionary<string, float[]>();
            if (_roadTiles.Count > 0 && _roadTiles[0] != null)
            {
                var rb = _roadTiles[0].bounds; var r = FrameRect(rb);
                d["지면(길) 띠"] = new[] { 0f, r[1], 100f, r[3] };
                // T240 1항 — 아레나 판에서는 이 띠가 «모래 마당» 이다(표 ㊺ 의 그 행). 같은 물건에 이름만 하나 더 붙인다.
                // ⚠ 이름을 안 붙이면 자가 그 행을 «없는 요소» 로 읽어 0 점을 주고, **자리가 맞는지 틀리는지조차 안 보인다** —
                //    지금은 실제로 어긋나 있고(챕터 길 자리 그대로다) 그 어긋남을 «점수로 보이게» 하는 것이 이 줄의 일이다.
                if (_isArena) d["모래 마당(타원)"] = new[] { 0f, r[1], 100f, r[3] };
            }
            if (_player != null)
            {
                var pb = FrameRect(_player.Bounds()); float foot = PctY(_player.transform.position.y);
                d["플레이어 높이"] = new[] { pb[0], pb[1], pb[2], R1(foot - pb[1]) };   // 표 정의 = «투구 장식 끝 ~ 발밑»(발 아래로 내려온 칼·손은 뺀다)
                d["플레이어 발밑 y"] = new[] { pb[0], foot, pb[2], 0f };
                d["플레이어 중심 x"] = new[] { PctX(_player.transform.position.x), pb[1], pb[2], pb[3] };
            }
            if (_pBarBg != null && _pBarBg.gameObject.activeSelf)
            {
                var hb = _pBarBg.bounds; d["플레이어 발밑 바 폭"] = FrameRect(hb);
                if (_pShBg != null && _pShBg.gameObject.activeSelf) hb.Encapsulate(_pShBg.bounds);
                d["체력 라벨 줄"] = FrameRect(hb);
            }
            EnemyView near = null; float nearLx = float.MaxValue;
            foreach (var kv in _enemies)
            {
                var v = kv.Value; if (v.E.Dead || v.Rig == null || v.DieT >= 0) continue;
                float lx = FoeLayoutX(v.E.WorldX); if (lx >= WorldCam.LayoutW || lx >= nearLx) continue;
                near = v; nearLx = lx;
            }
            if (near != null)
            {
                var eb = FrameRect(near.Rig.Bounds()); float efoot = PctY(near.Rig.transform.position.y);
                d["적 높이"] = new[] { eb[0], eb[1], eb[2], R1(efoot - eb[1]) }; d["적 행 y"] = new[] { eb[0], eb[1], eb[2], 0f };
                if (near.BarBg != null && near.BarBg.gameObject.activeSelf) d["적 발밑 바 폭"] = FrameRect(near.BarBg.bounds);
            }
            return d;
        }

        public BattleWorld(App app, BattleState g, RectTransform popsLayer)
        {
            _app = app; G = g; D = g.D; _pops = popsLayer;
            _zoom = (float)D.Ui.CameraZoom; _playerX = (float)(D.Ui.PlayerX * WorldCam.LayoutW);
            // T240 1항 — 아레나 판은 챕터와 무관하게 «모래 마당»(레퍼런스 33). 일반 판은 종전대로 챕터가 무대를 정한다.
            _isArena = IsArenaRun(app);
            _theme = _isArena ? Theme.Arena : Theme.ForChapter(g.Chapter);
            // T319 — 아레나 판의 «만나는 점» = 둘의 한가운데다.
            //   1대1 판은 상대가 `D.Enemies.NodeGap` 에 서 있고(Battle.BuildDuelNode) 내가 거기서 StopDistance 만큼 앞에 멈춘다.
            //   즉 만난 뒤 두 사람은 [gap − stop, gap] 에 서므로 그 한가운데(gap − stop/2)를 화면 가운데에 둔다 — 좌우 대칭이다.
            if (_isArena)
            {
                _fixedOrigin = true;
                _originPX = D.Enemies.NodeGap - g.C.StopDistance * 0.5;
                _originScreenX = WorldCam.LayoutW * 0.5f;
            }
            _shownPX = G.P.WorldX; _heldPrevFrame = false;
            _root = new GameObject("World").transform;
            BuildGround(); BuildProps(); BuildNodes(); BuildPlayer();
            _goldPrev = G.Gold; ShownHp = G.P.Hp; ShownSh = G.P.Sh;
        }
        public void Dispose()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            // 발밑 숫자·팝은 월드(_root 아래 · 막대의 자식)에 산다(T502) — 월드와 함께 사라진다. 손잡이만 놓는다.
            _pHpTxt = _pShTxt = null; _popRoot = null;
        }

        // ───────────────────────── 좌표 ─────────────────────────
        /// <summary>플레이어 기준 월드 거리 → 화면용 거리. 멈춤 거리 안(칼 닿는 거리)은 1배, SpreadRamp 를 지나며 WorldSpacing 배로. 뒤(음수)는 1배.</summary>
        float Spread(double d)
        {
            float stop = (float)G.C.StopDistance, mul = Layout.WorldSpacing;
            if (mul <= 1f || d <= stop) return (float)d;   // 배율 1 = 예전과 같은 균일 사상(모든 것이 같은 속도로 흐른다)
            float u = (float)d - stop;
            if (u <= SpreadRamp) return stop + u + (mul - 1f) * u * u / (2f * SpreadRamp);
            return stop + SpreadRamp + (mul - 1f) * SpreadRamp / 2f + mul * (u - SpreadRamp);
        }
        // 원점 = 표시 기준 x(T20) — 킬 연출 중에는 엔진 x 보다 뒤.
        // T319 — 아레나 판만 «만나는 점» 에 못 박는다: 원점이 안 움직이므로 **배경이 안 흐르고** 플레이어 그림이 제 엔진 x 를 따라 걸어 들어온다.
        float LayoutX(double worldX) => _fixedOrigin
            ? Spread(worldX - _originPX) * _zoom + _originScreenX
            : Spread(worldX - _shownPX) * _zoom + _playerX;
        Vector3 Pos(double worldX, float yFrac, float z = 0) => WorldCam.ToWorld(LayoutX(worldX), yFrac, z);
        /// <summary>
        /// T319 ⓑ — 아레나 판의 상대가 «오른쪽 밖에서 같은 속도로 가운데를 향해 걸어오는» 그림.
        /// <para>
        /// 엔진은 1대1 상대를 <c>NodeGap</c> 에 <b>세워 둔다</b>(적은 안 걷는다) — 그래서 화면 쪽에서만 <b>플레이어의 화면 x 를 가운데 기준으로 거울</b> 삼는다.
        /// 값(거리·속도·판정·시드)은 한 줄도 안 본다: 거울이라 다가오는 속도가 플레이어와 <b>정확히 같고</b>, 둘이 만나는 순간
        /// (플레이어 엔진 x = <c>NodeGap − StopDistance</c>) 이 식은 <see cref="LayoutX"/> 와 <b>같은 값</b>이 된다 —
        /// <c>2·가운데 − (−stop/2·zoom + 가운데) = 가운데 + stop/2·zoom = LayoutX(NodeGap)</c>.
        /// 즉 <b>싸움이 시작된 뒤로는 아무것도 안 바뀐다</b>(투사체·이펙트가 쓰는 자리도 그대로). 다른 판(챕터)에서는 손대지 않는다.
        /// </para>
        /// </summary>
        float FoeLayoutX(double worldX) => _fixedOrigin ? 2f * _originScreenX - LayoutX(_shownPX) : LayoutX(worldX);
        Vector3 FoePos(double worldX, float yFrac, float z = 0) => WorldCam.ToWorld(FoeLayoutX(worldX), yFrac, z);
        static float ScaleForHeightPct(float pct) => WorldCam.PctH(pct) / CharBaseHeight;
        static int SortBase(float layoutX) => 100 + Mathf.Clamp((int)((WorldCam.LayoutW + 200 - layoutX) / 6f), 0, 180);
        static bool OnScreen(Vector3 p, float margin = 4.5f) => p.x > -margin && p.x < margin;

        // ───────────────────────── 배경 (데모 씬 구성: 평면 바닥 · 길 띠 · 물결 경계 · 풀꽃 · 소품) ─────────────────────────
        SpriteRenderer Sprite(string key, Transform parent, int order, string name = null)
        {
            var go = new GameObject(name ?? key); go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _app.Assets.Sprite(key); sr.sortingOrder = order;
            return sr;
        }
        /// <summary>
        /// 바닥·길 — 데모 씬의 Field(128px × 스케일 (22.46, 20.35) = 28.7 × 26u 평면 · y −0.4)·Road(× (22.47, 2.4624) = 28.8 × 3.15u 띠 · y −0.402) 인스턴스를 <b>같은 스케일 × MapScale</b> 로 두고 가로로만 이어 붙인다(T19 · «그림 그대로»).
        /// 데모 씬처럼 바닥이 화면 전체(어둡게 하지 않는다) · 길 띠는 데모 치수 그대로 축소돼 발 줄(40%)을 품는다. 물결 경계(Road_up)는 소품 표에 들어 있다(<see cref="BuildProps"/>).
        /// </summary>
        void BuildGround()
        {
            var ground = new GameObject("Ground").transform; ground.SetParent(_root, false);
            var field = _app.Assets.Sprite(_theme.Field) ?? _app.Assets.Sprite("env.field");
            var fieldScale = new Vector3(MapLayouts.FieldScaleX * Layout.MapScale, MapLayouts.FieldScaleY * Layout.MapScale, 1f);   // 데모: 128px × (22.46, 20.35) = 28.7 × 26.0u 평면
            _tileW = (field != null ? field.bounds.size.x : 1.28f) * fieldScale.x;
            _tileCols = Mathf.CeilToInt(WorldCam.LayoutW / WorldCam.PPU / _tileW) + 2;
            float fieldY = WorldCam.ToWorld(0, DemoY(MapLayouts.FieldY)).y;
            float tileH = (field != null ? field.bounds.size.y : 1.28f) * fieldScale.y;
            int rows = Mathf.Max(1, Mathf.CeilToInt(WorldCam.LayoutH / WorldCam.PPU / tileH));   // 15.6u 평면 하나가 프레임(11.4u)을 다 덮는다 — 그림이 더 작으면 세로로도 이어 붙인다
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < _tileCols; c++)
                {
                    var sr = Sprite(_theme.Field, ground, OrderField, "field"); if (sr.sprite == null) sr.sprite = field;
                    sr.transform.localScale = fieldScale;
                    sr.transform.position = new Vector3(0, fieldY + (r - (rows - 1) * 0.5f) * tileH, 0);
                    _fieldTiles.Add(sr);
                }
            var road = _app.Assets.Sprite(_theme.Road) ?? _app.Assets.Sprite("env.road");
            // 세로만 표 높이로 늘린다(T215 · <see cref="RoadStretchY"/>) — 가로·자리는 데모 그대로다. 길 그림이 한 색 판이라 늘려도 그림이 안 상한다.
            var roadScale = new Vector3(MapLayouts.RoadScaleX * Layout.MapScale, MapLayouts.RoadScaleY * Layout.MapScale * RoadStretchY, 1f);       // 데모: 128px × (22.47, 2.46) = 28.8 × 3.15u 띠
            float roadY = WorldCam.ToWorld(0, DemoY(MapLayouts.RoadCenterY)).y;
            float roadW = (road != null ? road.bounds.size.x : 1.28f) * roadScale.x; int roadCols = Mathf.CeilToInt(WorldCam.LayoutW / WorldCam.PPU / roadW) + 2;
            for (int c = 0; c < roadCols; c++)
            {
                var sr = Sprite(_theme.Road, ground, OrderRoad, "road"); if (sr.sprite == null) sr.sprite = road;
                sr.transform.localScale = roadScale; sr.transform.position = new Vector3(0, roadY, 0); _roadTiles.Add(sr);
            }
        }
        void ScrollGround()
        {
            // T319 ⓒ — 아레나 판은 바닥도 안 흐른다. 땅 타일만 «표시 원점» 이 아니라 _shownPX 를 직접 봤는데(원점은 LayoutX 가 든다)
            //   그대로 두면 소품·노드·사람은 서 있는데 바닥 무늬만 흘러 «내가 오른쪽으로 가는 느낌» 이 그대로 남는다 — 주인이 지적한 그 느낌이다.
            float scroll = (float)((_fixedOrigin ? _originPX : _shownPX) * _zoom / WorldCam.PPU);
            float left = WorldCam.ToWorld(0, 0).x - _tileW;
            float off = Mathf.Repeat(scroll, _tileW);
            for (int i = 0; i < _fieldTiles.Count; i++) { var p = _fieldTiles[i].transform.position; p.x = left + (i % _tileCols) * _tileW - off + _tileW * 0.5f; _fieldTiles[i].transform.position = p; }
            float rw = _roadTiles.Count > 0 && _roadTiles[0].sprite != null ? _roadTiles[0].sprite.bounds.size.x * _roadTiles[0].transform.localScale.x : _tileW;
            float roff = Mathf.Repeat(scroll, rw);
            for (int i = 0; i < _roadTiles.Count; i++) { var p = _roadTiles[i].transform.position; p.x = left + i * rw - roff + rw * 0.5f; _roadTiles[i].transform.position = p; }
        }
        Prop AddProp(Transform parent, string key, double worldX, float yFrac, float scale, int order, bool flip)
        {
            var sr = Sprite(key, parent, order, "prop"); sr.flipX = flip; sr.transform.localScale = Vector3.one * scale;
            var p = new Prop { Sr = sr, WorldX = worldX, YFrac = yFrac }; _props.Add(p); return p;
        }
        /// <summary>데모 씬 y(길 중심 −0.402) → 프레임 비율 (× MapScale).</summary>
        public static float DemoY(float y) => RoadCenterFrac - (y - MapLayouts.RoadCenterY) * UnitFrac;
        /// <summary>데모 씬 1u → 월드 px(엔진 좌표 단위 · 화면에서 zoom 배가 곱해져 100 × MapScale 레이아웃 px 가 된다).</summary>
        float UnitPx => WorldCam.PPU * Layout.MapScale / _zoom;
        /// <summary>
        /// 소품 · 물결 경계 — 주인 지정 데모 씬(DemoScene_Autumn/DeepForest/Forest/Desert)의 인스턴스를 **그대로**(위치·반전·크기 · 부모 그룹 합성 · <see cref="MapLayouts"/> 표) 씬 폭마다 반복해 깐다
        /// (주인: «그 씬 배치가 맘에 들어서 그대로 복사해도 된다» · 2026-09-06 «맵 디자인을 데모 씬에 있는 거 그대로»). 전체를 <see cref="Layout.MapScale"/>(0.6) 배로: 위치 × 0.6 · 스프라이트 스케일 × 0.6.
        /// 물결 경계(Road_up)는 씬처럼 길 위(y ≈ +1.13)·아래 양쪽 — 표에 소품과 똑같이 들어 있다(T19). **아래쪽 것은 y 반전(flipY)** 하고 위쪽 것을 길 중심에 대칭시킨 자리(y = 2·RoadCenterY − 위 y ≈ −1.94)에 둔다(주인 2026-09-06 · T71 ①):
        /// Road_up 은 위가 평평하고 아래가 물결(길 위로 늘어진 풀)이라 아래쪽 것을 그대로 두면 물결이 길 밖(들판)을 향해 «직선 경계 + 밑에 점선» 으로 보였다 → 뒤집으면 위 경계와 같은 물결이 길 안쪽을 향한다.
        /// 정렬: 발 줄(40%)보다 위에 뿌리를 둔 소품은 캐릭터 뒤(<see cref="OrderNearProp"/> → 멀수록 뒤 · 하한 <see cref="OrderFarProp"/> = 바닥·길·납작보다 앞), 아래는 앞 · 납작한 것(풀·꽃·물결 경계)은 늘 바닥(길 바로 위 · <see cref="OrderFlat"/>) — 데모 렌더 순서(Field &lt; Road &lt; Road_up &lt; 나머지 y 내림차순)와 같다.
        /// T71 ② «위쪽에 나무가 적다» 의 원인은 표가 아니라 이 정렬이었다(위쪽 하한이 −60 이라 y ≥ 2.46 소품이 바닥 −20 뒤로 숨음 · 테마마다 5~10개) — 표는 데모 그대로 두고 하한만 바닥 앞으로 올렸다(미러 배치는 안 한다 · PROGRESS 결정 157).
        /// </summary>
        void BuildProps()
        {
            var props = new GameObject("Props").transform; props.SetParent(_root, false);
            double lastX = G.Nodes.Count > 0 ? G.Nodes[G.Nodes.Count - 1].X : 2000;
            double from = -700, to = lastX + 1400;
            float unitPx = UnitPx;
            float footDemoY = MapLayouts.RoadCenterY + (RoadCenterFrac - FootY) / UnitFrac;   // 발 줄의 데모 y (≈ −0.21)
            var layout = MapLayouts.Of(_theme.Name); double period = MapLayouts.WidthOf(_theme.Name) * unitPx;
            double start = System.Math.Floor(from / period) * period;
            // T215 — 띠를 세로로 늘렸으므로 물결 경계도 같은 폭만큼 위·아래로 옮긴다(경계와 띠 끝의 관계는 데모 그대로 유지된다).
            float upperEdgeY = UpperRoadEdgeY(layout) + RoadEdgeShift;
            for (double x0 = start; x0 < to; x0 += period)
                foreach (var p in layout)
                {
                    bool roadUp = p.Key.EndsWith(".roadUp"); bool lowerEdge = roadUp && p.Y < MapLayouts.RoadCenterY;
                    float y = roadUp ? (lowerEdge ? 2f * MapLayouts.RoadCenterY - upperEdgeY : p.Y + RoadEdgeShift) : p.Y;    // 아래 경계 = 위 경계의 길 중심 대칭 자리(T71 ①) · 위 경계는 늘어난 만큼 위로(T215)
                    float yf = DemoY(y);
                    if (yf < -0.15f || yf > 0.72f) continue;                        // 화면 위 밖 · HUD 패널 뒤는 만들지 않는다
                    var sp = _app.Assets.Sprite(p.Key);
                    bool flat = roadUp || (sp != null && sp.bounds.size.y * Mathf.Abs(p.Sy) < 0.35f);   // 풀·꽃(납작) 은 늘 바닥 — 데모 치수 기준. 물결 경계(roadUp)는 높이와 무관하게 늘 길 바로 위(Road_up_Desert 만 43px = 0.43u 라 문턱을 넘겼다 · T45 · CI #51)
                    int order = flat ? OrderFlat : p.Y > footDemoY ? Mathf.Max(OrderFarProp, OrderNearProp - (int)((p.Y - footDemoY) * 3f)) : Mathf.Min(470, 381 + (int)((footDemoY - p.Y) * 5f));
                    var pr = AddProp(props, p.Key, x0 + p.X * unitPx, yf, 1f, order, false);
                    pr.Sr.transform.localScale = new Vector3(p.Sx * Layout.MapScale, p.Sy * Layout.MapScale, 1f);
                    pr.Sr.flipY = lowerEdge;                                        // T71 ① — 아래쪽 물결 경계는 y 반전
                }
        }
        /// <summary>표에서 길 위쪽 물결 경계(Road_up · y &gt; 길 중심) 행의 y — 아래쪽 경계를 이 값의 길 중심 대칭에 둔다(T71 ①). 표에 없으면 데모 값 1.134.</summary>
        public static float UpperRoadEdgeY(MapLayouts.P[] layout)
        {
            float y = float.MinValue;
            foreach (var p in layout) if (p.Key.EndsWith(".roadUp") && p.Y > MapLayouts.RoadCenterY) y = Mathf.Max(y, p.Y);
            return y > float.MinValue ? y : 1.134f;
        }
        void BuildNodes()
        {
            var parent = new GameObject("Nodes").transform; parent.SetParent(_root, false);
            foreach (var n in G.Nodes)
            {
                if (n.Type != NodeType.Rest && n.Type != NodeType.Devil && n.Type != NodeType.Angel) continue;
                var go = new GameObject("node:" + n.Type); go.transform.SetParent(parent, false);
                var nv = new NodeView { N = n, Go = go };
                switch (n.Type)
                {
                    case NodeType.Rest:
                    {   // 통 + 모닥불 + 버섯 (Environment 팩에 모닥불이 없어 이렇게 조합 — 주인 «알아서»)
                        var b = Sprite("env.barrel", go.transform, 90); b.transform.localPosition = new Vector3(-0.35f, 0, 0); b.transform.localScale = Vector3.one * 0.8f;
                        var m = Sprite("env.mushroom", go.transform, 91); m.transform.localPosition = new Vector3(0.45f, -0.02f, 0); m.transform.localScale = Vector3.one * 0.7f;
                        nv.FxGo = Fx.Spawn("fx.fire", Vector3.zero, 0.6f, 0, go.transform, true); if (nv.FxGo != null) nv.FxGo.transform.localPosition = new Vector3(0.1f, 0.05f, -0.5f);
                        break;
                    }
                    case NodeType.Devil:
                    {   // 돌기둥 + 죽은 나무 + 영혼 이펙트
                        var s = Sprite("env.monolith", go.transform, 88); s.transform.localScale = Vector3.one * 0.75f; s.color = new Color(0.75f, 0.65f, 0.85f);
                        var t = Sprite("env.deadTree", go.transform, 86); t.transform.localPosition = new Vector3(-0.6f, 0, 0); t.transform.localScale = Vector3.one * 0.8f;
                        nv.FxGo = Fx.Spawn("fx.devil", Vector3.zero, 0.7f, 0, go.transform, true); if (nv.FxGo != null) nv.FxGo.transform.localPosition = new Vector3(0, 0.6f, -0.5f);
                        break;
                    }
                    case NodeType.Angel:
                    {   // 큰 돌 + 빛 이펙트
                        var s = Sprite("env.stoneBig", go.transform, 88); s.transform.localScale = Vector3.one * 0.6f; s.color = new Color(1f, 0.98f, 0.85f);
                        nv.FxGo = Fx.Spawn("fx.angel", Vector3.zero, 0.9f, 0, go.transform, true); if (nv.FxGo != null) nv.FxGo.transform.localPosition = new Vector3(0, 0.9f, -0.5f);
                        break;
                    }
                }
                _nodes.Add(nv);
            }
        }

        // ───────────────────────── 캐릭터 ─────────────────────────
        /// <summary>캐릭터 한 명 — heightPct 는 ref-layout 표의 키 %(PlayerHeight·EnemyHeight·보스 배수) · 실제 그리는 키는 × <see cref="Layout.CharScale"/>(2/3 · 주인 지시 · T14).</summary>
        CharacterRig MakeChar(string name, CharacterRig.Skin skin, float heightPct, bool faceRight)
        {
            var prefab = _app.Assets.Prefab("cm.character");
            GameObject go = prefab != null ? Object.Instantiate(prefab, _root) : new GameObject(name);
            go.name = name;
            var rig = CharacterRig.Attach(go);
            rig.Apply(skin);
            rig.SetScale(ScaleForHeightPct(Layout.CharHeightPct(heightPct))); rig.Face(faceRight);
            rig.Play(CharacterRig.Idle);
            return rig;
        }
        /// <summary>
        /// T240 2항 — 이 판의 <b>상대 외형</b>. 챕터 전투는 종전 몹 스킨이고, <b>아레나(PvP) 판은 «플레이어 캐릭터»</b> 다
        /// (주인 메모: «적도 몹이 아니라 플레이어 캐릭터» · 레퍼런스 <c>33_pvp_battle.jpg</c>).
        /// <para>
        /// ⚠ <b>«상대 장비를 입힌 모습» 은 못 만든다</b> — 더미(<c>arenaDummy.json</c>)가 갖는 것은 이름·아바타·전투력뿐이라
        /// 상대가 무엇을 꼈는지가 <b>어디에도 없다</b>. 그래서 <b>기사 기본 외형</b>(<c>PlayerSkin(D, null, …)</c> = 장비 0)으로 세운다 —
        /// 없는 장비를 골라 입히면 그 순간 «지어낸 데이터» 가 된다(§1).
        /// </para>
        /// <para>
        /// ⚠ <b>내 캐릭터와 같은 모습이 될 수 있다</b>(내가 장비를 안 꼈을 때). 그래도 <b>자리와 바라보는 쪽</b>이 갈린다 —
        /// 상대는 화면 오른쪽에서 왼쪽을 본다(<c>faceRight: false</c>). 여기서 색을 섞어 «달라 보이게» 하는 것은
        /// 주인이 말한 적 없는 연출이라 <b>안 했다</b>. 갈라 보이게 하려면 상대 장비가 데이터로 와야 한다.
        /// </para>
        /// <para>보스는 아레나에서 안 나오지만(1대1 은 적 하나 · 보스 아님) 갈래를 명시해 둔다 — 언젠가 «보스전 아레나» 가 생겨도 이 줄이 조용히 틀리지 않게.</para>
        /// </summary>
        CharacterRig.Skin FoeSkin(EnemyState e)
        {
            if (_isArena && !e.IsBoss) return CharacterRig.PlayerSkin(D, null, false);
            return EnemySkin(e);
        }

        /// <summary>적 스킨 — 전부 투구를 쓴다(주인 지시 «적들은 전부 모자 쓴 상태») · 원거리는 활+화살+시위.</summary>
        static CharacterRig.Skin EnemySkin(EnemyState e)
        {
            if (e.IsBoss) return new CharacterRig.Skin { Helmet = "cm.boss.helmet", Chest = "cm.boss.chest", Axe = "cm.boss.axe", SkinColor = new Color(0.38f, 0.30f, 0.42f) };
            if (e.Ranged) return e.Skin % 2 == 0
                ? new CharacterRig.Skin { Helmet = "cm.rangedA.helmet", Bow = "cm.rangedA.bow", Arrow = "cm.rangedA.arrow", BowLines = true }
                : new CharacterRig.Skin { Helmet = "cm.rangedB.helmet", Chest = "cm.rangedB.chest", Bow = "cm.rangedB.bow", Arrow = "cm.rangedB.arrow", BowLines = true };
            switch (e.Skin % 3)
            {
                case 0: return new CharacterRig.Skin { Helmet = "cm.meleeA.helmet", Chest = "cm.meleeA.chest", Sword = "cm.meleeA.sword" };
                case 1: return new CharacterRig.Skin { Helmet = "cm.meleeB.helmet", Chest = "cm.meleeB.chest", Axe = "cm.meleeB.axe" };
                default: return new CharacterRig.Skin { Helmet = "cm.meleeC.helmet", Chest = "cm.meleeC.chest", Sword = "cm.meleeC.sword" };
            }
        }
        /// <summary>
        /// 발밑 HP·실드 바(플레이어·적)의 테두리 조각 — <b>주인 지목</b>(T145 · 2026-09-07 «그 플레이어, 적 hp바랑 실드 바 보더
        /// `BasicFrame_Rectangle_01~04_White_InnerBorder1_Px7` 이거로 해줘야함»). 공용 <see cref="UiKit.BorderKey"/>(Border3)가 아니다.
        /// 하단 HUD 의 EXP·HP·실드 세 바는 주인이 «플레이어·적» 이라고 못 박아 공용 조각 그대로 둔다.
        /// 게이트(<c>BorderGateTests.AssertWorldBarBorder</c>)가 이 키로 선 굵기를 계산하므로 여기만 바꾸면 게이트도 따라온다.
        /// </summary>
        public const string FootBarBorderKey = "fr.rectInner7";
        /// <summary>
        /// 발밑 바 테두리의 «그려지는 선» 굵기(프레임 px) — 전 화면 공용 <see cref="UiKit.BorderPx"/>(8)가 아니라 <b>레퍼런스 02 실측 비율</b>로 얇게(결정 405).
        /// <para>
        /// 왜(회차 1 이 만든 회귀 · screens run 283 실측): 옛 조각 <c>fr.rectBorder3</c> 은 26×26 에 <c>spriteBorder 13/13</c> 이라
        /// <b>가운데 칸이 0px</b> = 9-slice 로 늘릴 자리가 없어 실제로는 <b>네 모서리 조각만</b> 그렸다. 새 조각은 가운데 칸이 2px 있어
        /// <b>사방에 연속된 띠</b>를 제대로 그린다 — 그 자체는 옳아진 것이지만, 그 띠가 프레임 8px 라
        /// 높이 37.4px(<see cref="Layout.FootBarH"/> 1.6%)인 이 바에서는 위·아래로 <b>16px</b> 을 먹어 빨강·파랑 채움이 반으로 줄었다.
        /// </para>
        /// <para>
        /// 값(레퍼런스 <c>docs/ref/02_battle.jpg</c> 720px 사본의 세로 단면 x190·x193 실측): 어두운 선 <b>3px</b> · 빨강 채움 14px · 파랑 채움 14px
        /// ⇒ 바 한 단 ≈ 20px 이고 <b>선 ÷ 단 = 0.15</b>. 그래서 이 상수도 리터럴이 아니라 <b>단 높이의 0.15</b> 로 잰다(1080/720 환산이 필요 없다).
        /// </para>
        /// 되돌리려면 이 한 줄. 전 화면 공용 <see cref="UiKit.BorderPx"/> 는 안 건드린다 — T69 가 수십 화면에 걸어 둔 값이다.
        /// </summary>
        public static float FootBarLinePx => UiKit.FrameH * Layout.FootBarH / 100f * 0.15f;
        void MakeBar(Transform parent, float width, float height, out SpriteRenderer bg, out SpriteRenderer fill, Color fillColor, int order)
        {
            var bgo = new GameObject("BarBg"); bgo.transform.SetParent(parent, false);
            bg = bgo.AddComponent<SpriteRenderer>(); bg.sprite = UiKit.White(); bg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f); bg.sortingOrder = order; bg.drawMode = SpriteDrawMode.Sliced; bg.size = new Vector2(width, height);
            var fgo = new GameObject("BarFill"); fgo.transform.SetParent(bgo.transform, false);
            fill = fgo.AddComponent<SpriteRenderer>(); fill.sprite = UiKit.White(); fill.color = fillColor; fill.sortingOrder = order + 1; fill.drawMode = SpriteDrawMode.Sliced; fill.size = new Vector2(width - 0.02f, height - 0.02f);
            // T69 8항(주인 «HP·실드 바도 Border») — 월드용 Sprite 로 감싸 바 위에 한 장(fill + 1 · 바 폭·높이 그대로 = 표 «발밑 바 폭» 이름표 불변).
            // 조각은 주인 지목(T145) 대로 fr.rectInner7 = BasicFrame_..._InnerBorder1_Px7 — 그려지는 선 굵기는 그대로 프레임 8px 다(WorldBorderSprite 가 ppu 로 맞춘다).
            // 선 굵기는 공용 8px 이 아니라 FootBarLinePx(단 높이의 0.15 = 레퍼런스 실측) — 8px 이면 띠가 채움을 반이나 먹는다(결정 405).
            UiKit.WorldBorder(bgo.transform, new Vector2(width, height), order + 2, FootBarBorderKey, thicknessPx: FootBarLinePx);
        }
        static void SetBar(SpriteRenderer bg, SpriteRenderer fill, double frac)
        {
            float w = bg.size.x - 0.02f; float f = Mathf.Clamp01((float)frac);
            fill.size = new Vector2(Mathf.Max(0.001f, w * f), fill.size.y);
            fill.transform.localPosition = new Vector3(-(w - w * f) / 2f, 0, 0);
        }
        /// <summary>
        /// 프레임 px 한 칸 = 월드 이만큼(T502). 팝·발밑 숫자는 글자 크기를 종전대로 <b>프레임 px</b> 로 재고(자·하한·실측이 전부 그 단위다)
        /// transform 배율 하나로 월드에 세운다 — 그러면 <c>fontSize</c>·<c>preferredWidth</c>·rect 는 종전 숫자 그대로이고 화면에서도 같은 크기다(줌 없음 기준).
        /// </summary>
        public const float WorldPerPx = WorldCam.LayoutW / UiKit.FrameW / WorldCam.PPU;
        /// <summary>발밑 숫자의 정렬 순서 = 막대 순서 + 이 값(막대 바탕 = order · 채움 = +1 · 테 = +2 · 숫자는 그 위).</summary>
        public const int FootTextOrderAdd = 3;
        /// <summary>데미지 팝의 정렬 순서 — 막대(392~398)·투사체(350) 위. 아이콘은 글자 위.</summary>
        public const int PopOrder = 400, PopIconOrder = 401;
        /// <summary>데미지 팝이 모이는 월드 오브젝트 이름(자가 이 이름의 자식을 «데미지 팝» 으로 센다).</summary>
        public const string PopsName = "Pops";

        /// <summary>
        /// 월드 글자 하나(T502) — <see cref="UiKit.Text"/> 와 같은 글꼴·테·하한 규칙을 타되 uGUI 가 아니라 <b>3D TextMeshPro</b> 로 <paramref name="parent"/> 아래에 선다.
        /// <c>isOrthographic</c> 이라 글자 크기 1 = 로컬 1 단위이고, 배율 <see cref="WorldPerPx"/> 가 그것을 «프레임 px 와 화면에서 같은 크기» 로 만든다.
        /// 카메라·줌과 같이 움직이는 것은 이 글자가 월드 오브젝트이기 때문이지 변환 때문이 아니다(종전 <c>WorldCam.ToFrame</c> 은 이 자리에서 사라졌다).
        /// </summary>
        static TMP_Text WorldText(Transform parent, string name, string s, int size, Color color, int order, TextKind kind)
        {
            size = TextSize.Floor(size, kind);
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshPro>();
            t.isOrthographic = true;
            t.font = TmpFont.Get(); t.text = TextGlyphs.Safe(s); t.fontSize = size; t.color = color; t.alignment = UiKit.TmpAlign(TextAnchor.MiddleCenter);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow; t.richText = true;
            t.sortingOrder = order;
            UiKit.EnsureOutline(t);   // 검은 테 + 주인 글꼴 — uGUI 글자와 같은 공유 머티리얼 한 장(T207 ②)
            TextAudit.Mark(t, kind);
            var rt = t.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one * WorldPerPx;
            return t;
        }
        /// <summary>발밑 바 안의 숫자(T35) — 막대의 <b>자식</b>으로 선 월드 TMP(흰 글자 · 외곽선 · T502). 크기는 <see cref="FootFontSize"/>(바 높이에서 잰다 · 픽셀 상수 없음). 글자 칸 높이는 «올린 뒤» 크기로(전엔 올리기 전 크기라 게이트 «잘림»).</summary>
        TMP_Text FootText(string name, Transform bar, int order)
        {
            if (_pops == null || bar == null) return null;
            int size = FootFontSize;
            // TextKind.Small = «정말 작아야 하는 곳»(하한 없음 · 호출부가 명시) — 이 자리만의 T63 예외다. 까닭은 FootFontSize 주석(결정 361).
            var t = WorldText(bar, name, "", size, Palette.White, order, TextKind.Small);
            size = Mathf.RoundToInt(t.fontSize);
            // Bold 를 안 준다(T125 회차 4 · 결정 449) — 이 자리는 화면에서 가장 작은 글자라 Bold 면 획이 서로 붙어 숫자가 흰 덩어리가 된다.
            // 실측: 레퍼런스와 글자 bbox 는 사실상 같은데(540 환산 30×10.5 대 27×11) 단 안 흰 픽셀 비율이 0.12 대 0.38 이었다 = 크기가 아니라 굵기.
            t.fontStyle = FontStyles.Normal;
            var rt = t.rectTransform; rt.sizeDelta = new Vector2(400, size * 1.4f); rt.localPosition = Vector3.zero;   // 막대 한가운데 — 막대가 움직이면 같이 간다(변환 0)
            return t;
        }
        /// <summary>숫자 글자 갱신 — 자리는 막대의 자식이라 저절로 한가운데다(T502 · 종전의 월드 → 프레임 변환이 통째로 없어졌다). 글자는 바뀔 때만 다시 쓴다.</summary>
        static void PlaceFootText(TMP_Text t, string s, bool visible, float barPctW)
        {
            if (t == null) return;
            if (t.gameObject.activeSelf != visible) t.gameObject.SetActive(visible);
            if (!visible) return;
            if (t.text != s) { t.text = s; FitFootText(t, barPctW); }
        }

        /// <summary>발밑 바 숫자가 바 «안» 에 들도록 글자 크기를 줄이는 <b>안전판</b>(T125 ⓑ) — 글자가 바뀔 때만 부른다(<c>preferredWidth</c> 는 레이아웃을 건드린다).</summary>
        /// <param name="barPctW">바 폭(프레임 %) — 플레이어 <see cref="Layout.PlayerFootBarW"/> · 적 <see cref="Layout.EnemyFootBarW"/> (둘 다 <see cref="Layout.FootBarScale"/> 곱한 값).</param>
        /// <remarks>
        /// ⚠ <b>지금 값으로는 한 번도 걸리지 않는다</b> — 자세한 실측은 <see cref="FootTextFill"/> 주석(결정 361). 여기 남겨 두는 이유는
        /// 이 글자가 <c>horizontalOverflow = Overflow</c> 라 <b>바 폭과 무관하게</b> 그려지기 때문이다: 나중에 표기가 길어지거나(다섯 자리·꼬리표 두 글자)
        /// T63 하한(<see cref="TextKind.Aux"/>)이 올라가면 그때는 실제로 바를 넘게 되고, 그 자리를 이 함수가 막는다.
        /// 하한은 <see cref="MinFootFont"/> — 그 아래로는 안 줄이고, 그래도 넘치면 그냥 넘치게 둔다(안 보이는 것보다 낫다).
        /// </remarks>
        static void FitFootText(TMP_Text t, float barPctW)
        {
            if (t == null || barPctW <= 0f) return;
            float room = UiKit.FrameW * barPctW / 100f * FootTextFill;
            for (int guard = 0; guard < 12; guard++)
            {
                if (t.preferredWidth <= room || t.fontSize <= MinFootFont) break;
                t.fontSize = Mathf.Max(MinFootFont, t.fontSize - 2);
            }
        }
        /// <summary>
        /// 발밑 바 숫자가 쓸 수 있는 바 <b>폭</b> 비율 — 레퍼런스 02 실측 0.74(720px 사본에서 바 ≈70px · «1055»·«2258» ≈52px)에 맞춘 값.
        /// <para>
        /// ⚠ <b>정정(결정 361)</b>: «숫자가 바 <b>폭</b>을 넘어 뭉갠다» 는 T125 ⓑ 의 등재 전제는 <b>사실이 아니었다</b>. screens 캡처를 픽셀로 재면
        /// 우리 숫자는 처음부터 바보다 좁았다 — run 239 폭비 <b>0.70</b> · run 246·255 <b>0.72</b>(레퍼런스가 오히려 더 꽉 찬 0.74).
        /// 그래서 회차 1(<see cref="FootNum"/> 의 콤마 제거)은 글자를 40→36px 로 실제로 줄였지만, 회차 2 의 이 상수 0.86→0.72 는
        /// <b>아무것도 바꾸지 않았다</b>(<see cref="FitFootText"/> 의 줄이기 고리가 한 번도 돌지 않는다).
        /// 뭉갠 것은 폭이 아니라 <b>높이</b>였다 — <see cref="FootTextHeight"/> 참조.
        /// </para>
        /// </summary>
        const float FootTextFill = 0.72f;
        /// <summary>
        /// 발밑 바 숫자 크기 = 바 높이(<see cref="Layout.FootBarH"/> % · 프레임 px) × 이 값 — <b>레퍼런스 02 실측</b>(결정 361).
        /// <para>
        /// 실측(레퍼런스 720px 사본 / 우리 screens run 255 · 빨간 HP 단): 레퍼런스는 바 18px 안에 숫자 잉크가 <b>9px = 바 높이의 0.50</b> 이고
        /// 바 넓이의 <b>12%</b>만 흰 픽셀이다. 우리는 바 17px 에 잉크 <b>14px = 0.82</b> 이고 흰 픽셀이 <b>49%</b> — 숫자가 단을 거의 덮어
        /// 빨강·파랑 채움이 가려지고 획 사이가 메워져 «흰 덩어리» 로 보였다. 폭이 아니라 <b>높이가 범인</b>이다.
        /// <para>
        /// ⚠ <b>위 «0.50» 은 잘못 잰 값이다(결정 449)</b> — 흰 픽셀을 «바 사각형 안» 에서만 찾았는데 <b>레퍼런스의 숫자는 바 위·아래로 넘쳐 나간다</b>
        /// (레퍼런스 02 의 «1055» 는 빨간 단 윗변을 넘는다). 제대로 재면 레퍼런스 글자는 720px 에서 40×14 = <b>540 환산 30×10.5</b> 이고
        /// 우리는 <b>27×11</b> 로 <b>사실상 같다</b>. 즉 이 상수가 데려온 최종 크기 26 은 <b>결과적으로 옳았고</b>(되돌리지 않는다),
        /// 그것을 고른 근거 숫자만 틀렸다. 남은 «뭉개짐» 의 진짜 원인은 크기가 아니라 <b>굵기</b>다 — <see cref="FootText"/> 의 <c>fontStyle</c> 주석.
        /// </para>
        /// </para>
        /// <para>
        /// 왜 그렇게 컸나: 전에는 «바 높이 × 0.8» 로 재 놓고 <c>TextKind.Aux</c> 로 만들어 T63 하한(<see cref="TextSize.Aux"/> 36)이
        /// 그 값을 <b>도로 끌어올렸다</b>. 그래서 <see cref="FootText"/> 는 이제 <c>TextKind.Small</c>(하한 없음 · 호출부 명시)을 쓴다.
        /// 값 = 레퍼런스 비 0.50 ÷ 우리 글자의 «잉크 ÷ 크기» ≈ 0.78 ⇒ <b>0.64</b>.
        /// </para>
        /// </summary>
        const float FootTextHeight = 0.64f;
        /// <summary>
        /// 발밑 바 숫자의 하한(T63 «글씨 작다» 와의 절충 — 이 아래로는 안 줄인다 · 배지급 글자 크기).
        /// T63 의 본문·보조 하한(40·36)보다 낮은 것은 <b>이 자리만의 예외</b>다: 바 높이가 표 ② 상수로 정해져 있어 글자를 키우면 단을 덮고,
        /// 레퍼런스 02 도 이 숫자만은 작게 그린다(화면에서 가장 작은 글자). 다른 자리에는 적용되지 않는다.
        /// 지금은 이 하한이 실제로 <b>걸린다</b> — 바 높이에서 잰 값(≈24)보다 크므로 최종 크기는 이 값이다.
        /// <para>
        /// 값 <b>29</b> 는 <b>레퍼런스 글자 상자에 맞춘 것</b>이다(결정 450 · `screens` run 333 실측):
        /// 레퍼런스 02 의 «1055» 는 540 환산 <b>30×10</b> 인데 크기 26 일 때 우리는 <b>27×11</b> 이라 조금 작았다 ⇒ 26 × 30/27 ≈ 29.
        /// 같은 실측에서 크기 26 은 <b>획이 너무 얇다</b>는 것도 드러났다 — 540 캡처에서 휘도 0.90 을 넘는 픽셀이
        /// 레퍼런스는 233개인데 우리는 <b>0개</b>(가장 밝은 픽셀 0.85)라 흰 글자가 회색으로 뜬다.
        /// 같은 PNG 의 본문급 글자(HUD 바 줄)는 1.00 에 376개라 <b>캡처 탓이 아니라 이 크기 탓</b>이다.
        /// </para>
        /// 주인 지시 T63 «글씨가 작아 안 읽힌다» 와도 같은 방향이다(26 → 29). 되돌리려면 이 한 줄.
        /// </summary>
        public const int MinFootFont = 29;
        /// <summary>발밑 바 숫자의 최종 글자 크기 — 바 높이에서 재고(<see cref="FootTextHeight"/>) <see cref="MinFootFont"/> 로 받친다. 게이트가 같은 식으로 단언한다.</summary>
        public static int FootFontSize => Mathf.Max(MinFootFont, Mathf.RoundToInt(UiKit.FrameH * Layout.FootBarH / 100f * FootTextHeight));
        /// <summary>발밑 바 숫자 표기 — 레퍼런스 02·03 처럼 <b>천 단위 콤마 없이</b>(«1239») 쓴다. 큰 수의 K/M 꼬리표는 <see cref="UiKit.Fmt"/> 그대로 남는다(T125 ⓑ).</summary>
        static string FootNum(double v) => UiKit.Fmt(System.Math.Ceiling(v)).Replace(",", "");
        /// <summary>
        /// 이 판에서 <b>플레이어 뒤를 따라 걸을 펫</b>을 정한다(T293 9항). 빈 목록·null 이면 한 마리도 안 세운다.
        /// <para>
        /// ⚠ <b>세이브를 여기서 안 읽는다</b> — 무엇을 꼈는지는 <b>부르는 쪽</b>이 준다(<c>Battle</c> 의 <c>RunOptions.Pets</c> 와 같은 꼴 · ⓑ).
        /// <c>SaveData.Pets</c> 가 열리는 회차는 이 함수를 <b>한 번 부르기만</b> 하면 되고, 그 전까지는 아무 일도 안 일어난다.
        /// </para>
        /// <para>다시 부르면 세워 둔 것을 지우고 새로 세운다 — 판 도중에 장착이 바뀌는 길은 아직 없지만, 남겨 두면 «두 벌이 겹쳐 선» 판이 된다.</para>
        /// </summary>
        public void SetPets(PetData d, IList<PetData.Pet> pets)
        {
            foreach (var r in _pets) if (r != null) Object.Destroy(r.gameObject);
            _pets.Clear();
            _petData = d;
            if (d == null || pets == null) return;
            int n = pets.Count < d.Slots ? pets.Count : d.Slots;   // 열린 칸보다 많이 들어와도 표가 정한 수까지만 선다
            for (int i = 0; i < n; i++)
            {
                var p = pets[i]; if (p == null) continue;
                var rig = MakeChar("Pet" + i, CharacterRig.PetSkin(d, p), Layout.PlayerHeight * (float)d.BattleScale, true);
                rig.transform.position = Pos(_shownPX - d.BattleGapDx * (i + 1), FootY);
                _pets.Add(rig);
            }
            SyncPets();
        }

        /// <summary>
        /// 펫을 플레이어에 맞춰 놓는다 — 자리(뒤로 <c>gapDx</c> 씩) · 그리는 순서(플레이어보다 뒤) · 동작.
        /// <para>
        /// 동작은 <b>플레이어가 지금 하는 것</b>을 따라간다(걷기·대기·사망·승리·패배). 다만 <b>공격은 안 따라한다</b> —
        /// 9항이 준 것은 «따라 걷는다» 이고, 펫의 발동 효과(도끼·번개)는 엔진이 플레이어 자리에서 이미 낸다(ⓑ).
        /// 여기서 펫에게 공격 모션을 주면 «때리는 것처럼 보이는데 아무 데미지도 안 나는» 그림이 된다.
        /// </para>
        /// </summary>
        void SyncPets()
        {
            if (_pets.Count == 0 || _petData == null) return;
            int baseOrder = SortBase(LayoutX(_shownPX));
            string state = _player == null ? CharacterRig.Idle : _player.Current;
            if (state == CharacterRig.Attack || state == CharacterRig.Skill) state = _moving ? CharacterRig.Walk : CharacterRig.Idle;
            // T404 ⓐ — 표 값을 쓰되 맨 뒤 펫이 화면 왼쪽 밖으로 안 나가게 죈다(셈은 Core/Layout · 까닭도 거기 적었다)
            float gap = Layout.PetGap((float)_petData.BattleGapDx, LayoutX(_shownPX), _pets.Count, _zoom, WorldCam.LayoutW);
            for (int i = 0; i < _pets.Count; i++)
            {
                var r = _pets[i]; if (r == null) continue;
                r.transform.position = Pos(_shownPX - gap * (i + 1), FootY);
                r.SetSortingBase(baseOrder - PetSortBack * (i + 1));
                r.Play(state);
            }
        }

        void BuildPlayer()
        {
            _player = MakeChar("Player", CharacterRig.PlayerSkin(D, _app.Save, G.P.MaxSh > 0), Layout.PlayerHeight, true);   // 장착 외형 반영 — 장비 화면(HeroView)과 같은 표(GearLook)
            _player.transform.position = Pos(_shownPX, FootY);
            // 발밑 2단 바(T35 · 주인 강조): 빨강(HP) 위 · 파랑(실드) 아래 · 같은 높이 · 각 단 안에 흰 숫자. 폭 = 표 폭 × FootBarScale(T63-battle · 숫자 36px 이 들어가게 표 폭 그대로 · 결정 133)
            float pBarW = WorldCam.PctW(Layout.PlayerFootBarW) * Layout.FootBarScale;
            MakeBar(_root, pBarW, WorldCam.PctH(Layout.FootBarH), out _pBarBg, out _pBarFill, Palette.Red, 392);
            MakeBar(_root, pBarW, WorldCam.PctH(Layout.FootShBarH), out _pShBg, out _pShFill, Palette.Hex(D.Ui.PopShield), 392);
        }
        EnemyView Ensure(EnemyState e)
        {
            if (_enemies.TryGetValue(e, out var v)) return v;
            e.Skin = System.Math.Abs(e.Id * 2654435761L % 1000).GetHashCode();
            float h = e.IsBoss ? Layout.EnemyHeight * (float)D.Enemies.BossSizeMul : Layout.EnemyHeight;
            v = new EnemyView { E = e, Rig = MakeChar("Enemy" + e.Id, FoeSkin(e), h, false), StrikeTick = e.StrikeT, ShownHp = e.Hp };
            // 바 폭 = 표(ref-layout ② «적 발밑 바 폭» 9.7 · 플레이어 10.3 과 거의 같다) × FootBarScale(T63-battle · 플레이어 바와 같은 자) — ui.json enemyBarW(37px = 6.9%) 를 쓰면 플레이어 바의 2/3 폭이 돼 레퍼런스와 어긋났다(T47 회차 2). 보스는 ui.json 의 보스/잡몹 비율만 빌린다.
            float barW = WorldCam.PctW(Layout.EnemyFootBarW) * Layout.FootBarScale * (e.IsBoss && D.Ui.EnemyBarW > 0 ? (float)(D.Ui.BossBarW / D.Ui.EnemyBarW) : 1f);
            MakeBar(_root, barW, WorldCam.PctH(Layout.FootBarH), out v.BarBg, out v.BarFill, e.IsBoss ? Palette.Plum : Palette.Red, 395);
            v.BarTxt = FootText("FootTxt:Enemy" + e.Id, v.BarBg.transform, 395 + FootTextOrderAdd);   // 적은 실드가 없으므로(엔진 EnemyState 에 Sh 없음) 빨간 단 하나 + 숫자(레퍼런스 03 «2555»)
            _enemies[e] = v;
            return v;
        }
        void Remove(EnemyView v) { Object.Destroy(v.Rig.gameObject); Object.Destroy(v.BarBg.gameObject); if (v.BarTxt != null) Object.Destroy(v.BarTxt.gameObject); if (v.StunFx != null) Object.Destroy(v.StunFx); _enemies.Remove(v.E); }
        /// <summary>화면에 보이는 적 발밑 바 수 — 테스트·진단용.</summary>
        public int EnemyBarCount { get { int n = 0; foreach (var kv in _enemies) if (kv.Value.BarBg != null && kv.Value.BarBg.gameObject.activeSelf) n++; return n; } }
        /// <summary>보이는 적 바마다 숫자 글자가 켜져 있고 표시 체력(정수)과 같은가 — 테스트·진단용.</summary>
        public bool EnemyBarTextsConsistent()
        {
            foreach (var kv in _enemies)
            {
                var v = kv.Value; if (v.BarBg == null || !v.BarBg.gameObject.activeSelf) continue;
                if (v.BarTxt == null || !v.BarTxt.gameObject.activeSelf || v.BarTxt.text != FootNum(v.ShownHp)) return false;
            }
            return true;
        }

        // ───────────────────────── 틱 훅 (BattleScreen 이 엔진 틱 전후로 부른다) ─────────────────────────
        bool _moving, _engineMoving, _bossWarned; double _prevPX;
        public void BeforeTick()
        {
            _prevPX = G.P.WorldX;
            // 이번 틱의 플레이어 표적 = 가장 앞(가장 작은 x)의 살아 있는 적 (Battle.Tick 의 alive[0] 과 같은 규칙)
            _pTarget = null; foreach (var n in G.Nodes) foreach (var e in n.Enemies) if (e.Hp > 0 && (_pTarget == null || e.WorldX < _pTarget.WorldX)) _pTarget = e;
        }
        public void AfterTick()
        {
            _engineMoving = G.P.WorldX > _prevPX + 1e-6;   // 엔진이 이번 틱에 걸었나 — 화면의 Walk 는 Sync 에서 표시 원점 기준으로 정한다(T20)
            var P = G.P; _pStrike = null; _eStrikes.Clear();
            if (Silent) { _pStrikeTick = P.StrikeT; foreach (var kv in _enemies) kv.Value.StrikeTick = kv.Key.StrikeT; G.Events.Clear(); _shownPX = P.WorldX; _heldPrevFrame = false; return; }
            // 플레이어가 이번 틱에 휘둘렀나 → 공격 모션(간격 = 1/공속) + 연출 묶음
            if (P.StrikeT > _pStrikeTick && !G.Dead)
            {
                _player.PlayAttack(1.0 / System.Math.Max(0.05, G.EffAspd()));
                _pStrike = new Strike { Rig = _player, HitCount0 = _player.HitCount, At = _clock + Mathf.Max(0.02f, _player.HitDelay) + 0.05f, Target = _pTarget };
                if (_pTarget != null) { var tv = Ensure(_pTarget); tv.Hold++; }
                _strikes.Add(_pStrike);
            }
            _pStrikeTick = P.StrikeT;
            // 적들이 이번 틱에 휘둘렀나
            foreach (var kv in _enemies)
            {
                var e = kv.Key; var v = kv.Value;
                if (e.StrikeT > v.StrikeTick && !e.Dead)
                {
                    double ivm = e.Slow > 0 ? G.C.SlowMul : 1;
                    v.Rig.PlayAttack((e.IsBoss ? G.C.BossInterval : e.Ranged ? G.C.RangedInterval : G.C.MeleeInterval) * ivm);
                    if (!e.Ranged)
                    {   // 근접 적의 타격 연출(플레이어 피격·회피·방어막·반격)은 칼이 내려올 때 — 원거리는 화살이 따로 날아간다
                        var s = new Strike { Rig = v.Rig, HitCount0 = v.Rig.HitCount, At = _clock + Mathf.Max(0.02f, v.Rig.HitDelay) + 0.05f, OnPlayer = true, Target = e };
                        _eStrikes[e] = s; _strikes.Add(s); _holdPlayer++;
                    }
                }
                v.StrikeTick = e.StrikeT;
            }
            // 이벤트 — 타격 묶음에 속하면 미루고, 아니면 바로
            foreach (var ev in G.Events) Route(ev);
            G.Events.Clear();
        }
        void Route(BattleEvent ev)
        {
            if (_pStrike != null && ev.Enemy != null && ev.Enemy == _pStrike.Target && (ev.Kind == EvKind.Hit || ev.Kind == EvKind.Miss || ev.Kind == EvKind.Kill || ev.Kind == EvKind.Stun)) { _pStrike.Evs.Add(ev); return; }
            if (ev.Enemy != null && _eStrikes.TryGetValue(ev.Enemy, out var s) &&
                (ev.Kind == EvKind.PlayerHit || ev.Kind == EvKind.PlayerEvade || ev.Kind == EvKind.Ward || ev.Kind == EvKind.Ignore || ev.Kind == EvKind.Counter || ev.Kind == EvKind.Reflect)) { s.Evs.Add(ev); return; }
            Present(ev);
        }
        void FlushStrikes(bool force)
        {
            for (int i = _strikes.Count - 1; i >= 0; i--)
            {
                var s = _strikes[i];
                bool due = force || s.Rig == null || s.Rig.HitCount > s.HitCount0 || _clock >= s.At;
                if (!due) continue;
                _strikes.RemoveAt(i);
                foreach (var ev in s.Evs) Present(ev);
                if (s.OnPlayer) _holdPlayer = System.Math.Max(0, _holdPlayer - 1);
                else if (s.Target != null && _enemies.TryGetValue(s.Target, out var v)) v.Hold = System.Math.Max(0, v.Hold - 1);
                // 킬 타격이 내려왔다 → 플레이어 공격 모션이 끝날 때까지 출발하지 않는다(T50 · Sync 가 Attacking 이 끝나면 푼다)
                if (!s.OnPlayer && s.Rig == _player && s.Target != null && s.Target.Dead && !force) _killAnimHold = true;
            }
        }

        // ───────────────────────── 매 프레임 ─────────────────────────
        /// <param name="dt">월드 초(배속 반영).</param>
        public void Sync(float dt)
        {
            _clock += dt; CharacterRig.TimeScale = TimeScale;
            FlushStrikes(false);
            ScrollGround();
            foreach (var p in _props) { var pos = Pos(p.WorldX, p.YFrac, 0); p.Sr.transform.position = pos; p.Sr.enabled = OnScreen(pos, 5.5f); }
            foreach (var nv in _nodes)
            {
                nv.Go.transform.position = Pos(nv.N.X, FootY - 0.005f);
                nv.Go.SetActive(OnScreen(nv.Go.transform.position));
                if (nv.N.Done && !nv.Dimmed) { nv.Dimmed = true; foreach (var sr in nv.Go.GetComponentsInChildren<SpriteRenderer>()) sr.color = Palette.A(sr.color, 0.55f); if (nv.FxGo != null) { Object.Destroy(nv.FxGo); nv.FxGo = null; } }
            }
            // 플레이어 — 표시 체력은 «칼이 내려온 뒤» 에만 엔진 값으로
            var P = G.P;
            if (_holdPlayer == 0) { ShownHp = P.Hp; ShownSh = P.Sh; }
            // 표시 원점(T20/T50): 킬 연출 대기(칼 내려오기 전) · 킬 뒤 공격 모션 중에는 멈춤 — 그 동안 엔진도 보류(HoldEngine)되므로 풀리면 격차 없이 엔진 걸음 그대로 출발한다(따라잡기 없음 · 원래 걷기 속도)
            _player.Tick(dt);
            if (_killAnimHold && !_player.Attacking) _killAnimHold = false;   // 공격 모션이 끝났다 → 걷기 모션과 함께 출발
            // T457 — 엔진을 안 세우면 화면 원점도 세우지 않는다(세우면 T20 의 «풀리며 2배 걸음» 이 돌아온다). 스위치 하나가 둘을 같이 정한다.
            double gap = P.WorldX - _shownPX; bool hold = HoldEngineOnKill && (KillPending || KillAnimHold);
            // 멈춤 시작 프레임(!_heldPrevFrame)은 엔진 x 로 맞추고(그 프레임의 걷기 틱은 킬 이전의 접근 걸음이다 · T65), 그다음 프레임부터 얼린다
            if (Silent || !hold || !_heldPrevFrame || gap < 0 || gap > SnapGap) _shownPX = P.WorldX;
            _heldPrevFrame = hold;
            _moving = !hold && _engineMoving;
            _player.transform.position = Pos(_shownPX, FootY);
            _player.SetSortingBase(SortBase(LayoutX(_shownPX)));
            if (G.Dead) { if (!_pDeadShown && _holdPlayer == 0) { _pDeadShown = true; _player.Play(CharacterRig.Dead, true); } }
            else if (G.Cleared) { if (!_player.Attacking) _player.Play(CharacterRig.Victory); }
            else if (!_player.Attacking) _player.Play(_moving ? CharacterRig.Walk : CharacterRig.Idle);
            SyncPets();   // ⚑ 플레이어 동작을 정한 «뒤» 에 — 앞에 두면 펫이 한 프레임 늦은 동작을 따라한다
            _pBarBg.transform.position = Pos(_shownPX, Layout.FootHpBarY / 100f); SetBar(_pBarBg, _pBarFill, P.MaxHp > 0 ? ShownHp / P.MaxHp : 0);
            _pBarBg.gameObject.SetActive(!_pDeadShown);
            _pShBg.transform.position = Pos(_shownPX, Layout.FootShBarY / 100f); SetBar(_pShBg, _pShFill, P.MaxSh > 0 ? ShownSh / P.MaxSh : 0);
            _pShBg.gameObject.SetActive(!_pDeadShown && P.MaxSh > 0);   // 실드 0 이면 파란 단 숨김(T35)
            if (_pHpTxt == null) { _pHpTxt = FootText("FootTxt:PlayerHp", _pBarBg.transform, 392 + FootTextOrderAdd); _pShTxt = FootText("FootTxt:PlayerSh", _pShBg.transform, 392 + FootTextOrderAdd); }   // 막대의 자식(T502) — 첫 Sync 에서 만든다(종전 자리 그대로)
            PlaceFootText(_pHpTxt, FootNum(ShownHp), _pBarBg.gameObject.activeSelf, Layout.PlayerFootBarW * Layout.FootBarScale);
            PlaceFootText(_pShTxt, FootNum(ShownSh), _pShBg.gameObject.activeSelf, Layout.PlayerFootBarW * Layout.FootBarScale);
            // 적
            var seen = new HashSet<EnemyState>(); bool engaged = false;
            foreach (var n in G.Nodes) foreach (var e in n.Enemies)
            {
                float lx = FoeLayoutX(e.WorldX);
                if (lx > WorldCam.LayoutW + 120 || (e.Dead && !_enemies.ContainsKey(e))) continue;
                var v = Ensure(e); seen.Add(e);
                v.Rig.Tick(dt);
                v.Rig.transform.position = FoePos(e.WorldX, FootY);
                v.Rig.SetSortingBase(SortBase(lx));
                if (v.Hold == 0) v.ShownHp = e.Hp;
                if (e.Dead && v.Hold == 0)
                {
                    // 사망 = 모션(Dead1 · 끝에서 정지) + 알파 페이드 + snd.kill — «펑» 이펙트(fx.death Magic Poof)는 주인 지시로 뿌리지 않는다(T51 · 2026-09-06)
                    if (v.DieT < 0) { v.DieT = 0; v.Rig.Play(CharacterRig.Dead, true); _lastKillPos = v.Rig.transform.position; if (!Silent) Audio.Sfx("snd.kill", 0.9f); v.BarBg.gameObject.SetActive(false); PlaceFootText(v.BarTxt, "", false, Layout.EnemyFootBarW * Layout.FootBarScale); if (v.StunFx != null) { Object.Destroy(v.StunFx); v.StunFx = null; } if (!Silent && KillShown != null) KillShown(_lastKillPos, e.IsBoss); }
                    v.DieT += dt; v.Rig.SetAlpha(Mathf.Clamp01(1.2f - v.DieT * 1.5f));
                    if (v.DieT > 0.85f) Remove(v);
                    continue;
                }
                if (e.Stun > 0 && !e.Dead) { v.Rig.Play(CharacterRig.Stun); if (v.StunFx == null) { v.StunFx = Fx.Spawn("fx.stun", Vector3.zero, 0.5f, 0, v.Rig.transform, true); if (v.StunFx != null) { v.StunFx.transform.localPosition = new Vector3(0, CharBaseHeight * 1.05f, -0.3f); v.StunFx.transform.localRotation = Quaternion.identity; } } }
                // T319 ⓑ — 아레나 판에서 상대가 «걸어오는» 동안에는 걷기 클립을 돈다(거울이라 플레이어가 걸으면 상대도 그만큼 다가온다).
                //   만나면 플레이어와 같은 시각에 멎으므로(_moving = false) 대기 클립으로 돌아온다.
                else { if (v.StunFx != null) { Object.Destroy(v.StunFx); v.StunFx = null; } if (!v.Rig.Attacking) v.Rig.Play(_fixedOrigin && _moving ? CharacterRig.Walk : CharacterRig.Idle); }
                v.BarBg.transform.position = FoePos(e.WorldX, Layout.FootHpBarY / 100f);
                SetBar(v.BarBg, v.BarFill, e.MaxHp > 0 ? v.ShownHp / e.MaxHp : 0);
                PlaceFootText(v.BarTxt, FootNum(v.ShownHp), v.BarBg.gameObject.activeSelf, Layout.EnemyFootBarW * Layout.FootBarScale);
                if (!e.Dead && lx < WorldCam.LayoutW) engaged = true;
                // T235(주인 2026-09-08 09:1X «보스라고 보스 연출 안 떠도 된다») — 경고 띠(Overlay.BossWarn)와 터지는 이펙트(fx.bossWarn)를 뺐다.
                // 남긴 것은 보스 곡 하나다: 주인이 말한 것은 «연출» 이고 곡은 분위기라 지어내지 않는다(§1). 곡까지 빼려면 주인 한마디면 된다.
                // _bossWarned 표식도 그 곡 때문에 그대로 남는다 — 한 판에 한 번만 바꾸게 하는 구실이 여전히 필요하다.
                if (e.IsBoss && !_bossWarned && lx < WorldCam.LayoutW) { _bossWarned = true; Audio.Bgm("bgm.boss"); }
            }
            var gone = new List<EnemyView>(); foreach (var kv in _enemies) if (!seen.Contains(kv.Key)) gone.Add(kv.Value);
            foreach (var v in gone) Remove(v);
            Engaged = engaged;
            // 투사체
            SyncProjectiles(dt);
            // 골드 증가 → 팝 (엔진은 골드 이벤트를 따로 내지 않는다)
            // T110 ⓐ(주인 2026-09-07 «골드 +49G 이런 거 데미지 텍스트처럼 뜨는 거 하면 안 됨») — 골드 팝 «글자» 는 없앴다.
            // 골드가 는 것은 T85·T109 의 흡수 구슬 + 상단 골드 pill 카운트업으로만 보여 준다(동전 소리는 그대로 · 주인 지적은 글자다).
            if (G.Gold > _goldPrev + 0.5 && !Silent) Audio.Sfx("snd.coin", 0.7f);
            _goldPrev = G.Gold;
        }

        // ───────────────────────── 투사체(T86) ─────────────────────────
        // 주인 2026-09-07: ⓐ «도끼랑 창같은거 바로 안날라간다» ⓑ «창이 누워서 일자로 가야 하는데 비스듬한 각으로 간다» ⓒ «도끼 회전 너무 빠름 — 1초에 1바퀴».
        // ⓐ 원인 = 도끼·창은 대부분 «처치 시» 특전이 쏘는데(Battle.cs:400·436) 그 킬 연출 동안 엔진 틱이 보류(HoldEngine · T50)돼 pr.X 가 멎는다 → 발사하고 제자리에 뜬다.
        //    처방 = 엔진은 그대로 두고(판정·시드 골든 불변) «표시 x» 를 따로 든다 — 엔진과 «같은 px/s»(pr.Spd)로 매 프레임 전진(거리당 속도 · 시간 고정 금지 = 지시서 4-1),
        //    엔진이 앞서면 즉시 엔진을 따르고(격차 0), 엔진이 맞히는 자리(ProjLimit)는 앞지르지 않는다 — 그래서 «맞기 전에 지나가 버리는» 그림이 안 나온다.
        const float AxeSpinDegPerSec = 360f;   // ⓒ 초당 1바퀴 — 정규화 t(비행 거리 비율)가 아니라 «날아간 시간»(거리/속도)에서 뽑는다(거리가 달라도 초당 속도는 같다)
        const float SpearAngle = 0f;           // ⓑ 창은 수평 — 스프라이트 FA_WP_Main_Spear_001 은 이미 오른쪽으로 누워 있다(PNG 실측 기울기 1.2° · 보정 불필요)
        /// <summary>
        /// 플레이어 화살 각 — <b>수평</b>(T179 · 주인 «화살 각도가 완벽히 누워 있어야 하는데 비스듬하다»).
        /// 종전 −35° 는 «쏘아 올린 활» 느낌으로 준 값인데, 화살은 <see cref="ProjKind.Axe"/> 와 달리 <b>포물선을 안 그린다</b>(같은 <c>yf</c> 로 직선 비행) —
        /// 즉 가는 방향과 그림이 어긋나 있었다. 스프라이트 <c>FA_Consumable_Arrow_002</c> 의 본디 기울기는 PNG 실측 <b>0.05°</b>(사실상 수평)라 보정도 필요 없다(결정 415).
        /// </summary>
        public const float ArrowAngle = 0f;
        /// <summary>
        /// 적 화살 각 — 적 화살은 <b>왼쪽</b>으로 나므로 <c>flipX = true</c> 로 좌우만 뒤집고 회전은 <b>0°</b>(T179 ⓐ).
        /// 종전 <c>Euler(0,0,200f)</c> 는 «180°(왼쪽 보기) + 20°» 라 그 20° 가 그대로 기울기로 보였다 — 주인이 말한 «비스듬» 이 이것이다.
        /// 스프라이트 <c>FA_Consumable_Arrow_001</c> 의 본디 기울기는 PNG 실측 <b>0.14°</b>(불투명 픽셀의 주축)라 보정 없이 0 이면 수평이다(<see cref="SpearAngle"/> 과 같은 판단).
        /// </summary>
        public const float EnemyArrowAngle = 0f;

        /// <summary>투사체의 표시 x(T86 ⓐ · 테스트·진단용) — 화면에 없으면 엔진 x.</summary>
        public double ProjShownX(Projectile pr) => pr != null && _projX.TryGetValue(pr, out double x) ? x : (pr != null ? pr.X : 0);
        /// <summary>적 화살의 표시 x(T179 ⓑ 게이트용) — 그림이 실제로 서 있는 자리다(엔진 <c>a.X</c> 가 아니라).</summary>
        public double ArrowShownX(EnemyArrow a) => a != null && _arrowX.TryGetValue(a, out double x) ? x : (a != null ? a.X : 0);
        /// <summary>화면에 서 있는 적 화살 그림 수(T179 ⓓ 누수 0 게이트용).</summary>
        public int ArrowViewCount => _arrows.Count;
        /// <summary>적 화살 그림이 <b>지금 보이는가</b>(T233 게이트용) — 명중선에 닿으면 끈다. 그림이 아예 없으면 false.</summary>
        public bool ArrowViewVisible(EnemyArrow a) => a != null && _arrows.TryGetValue(a, out var go) && go != null && go.activeSelf;
        /// <summary>엔진이 «맞았다» 고 보는 자리(테스트가 같은 식을 베끼지 않게 · <c>Battle.cs</c> 의 그 선과 같다).</summary>
        public double ArrowHitLine => G != null && G.P != null ? G.P.WorldX + EngineConst.ArrowHitDx : 0;
        /// <summary>적 화살 그림의 z 회전(T179 ⓐ 게이트용) — 없으면 <c>float.NaN</c>.</summary>
        public float ArrowViewAngle(EnemyArrow a) => a != null && _arrows.TryGetValue(a, out var go) && go != null ? go.transform.eulerAngles.z : float.NaN;
        /// <summary>투사체의 화면 오브젝트(T86 · 테스트·진단용 · 각도 확인).</summary>
        public GameObject ProjGo(Projectile pr) { if (pr != null && _projs.TryGetValue(pr, out var go)) return go; return null; }

        /// <summary>
        /// 엔진이 이 투사체를 «맞히는 자리» — 표시 x 는 여기를 앞지르지 않는다(T86 ⓐ).
        /// <para>
        /// <b>T108(주인 2026-09-07 «창 발사하면 그냥 멈추지 말고 쭉 지나가면서 다 데미지 주고 지나가야 함 · 쩄든 뭐든 멈추면 안 됨»)에서
        /// 관통형(창·검기)의 «다음에 꿸 적» 걸림쇠를 없앴다</b>(결정 252). 그것이 주인이 본 «멈춰 있는 현상» 의 원인이었다 —
        /// 창은 대부분 처치 시 특전이 쏘므로 스폰 프레임이 곧 킬 틱이고, 그동안 엔진 틱이 보류(<c>HoldEngine</c> · T50)돼 <c>pr.X</c> 가 멎는다.
        /// 그러면 걸림쇠(= 바로 앞 적의 적중 시작 자리)도 같이 멎으므로 표시 창이 <b>그 적 앞에 붙어 선 채로</b> 엔진이 풀리기를 기다렸다.
        /// 적이 촘촘한 웨이브에서는 적마다 이 일이 되풀이돼 «끊겨 보인다».
        /// </para>
        /// 관통형은 이제 <b>사거리 끝(<c>MaxX</c>)까지 한 번도 안 멈추고</b> 간다 — 엔진은 판정만 하고(관통·피해·마릿수 상한은 그대로),
        /// 표시가 적을 살짝 먼저 지나가도 엔진이 곧 같은 적을 꿰므로 그림과 판정이 어긋나 보이지 않는다.
        /// <para>
        /// <b>T171(주인 2026-09-07 «창이 여전히 화면 끝에서 멈추네 가끔씩»)</b> — 남아 있던 두 «상한» 을 마저 없앤다. 둘 다 같은 병이다:
        /// <b>상한이 엔진과 함께 얼어붙는데 표시만 계속 흐르는 것</b>. 킬 연출 동안 엔진 틱은 보류(<see cref="HoldEngine"/> · T50/T86)되지만
        /// 표시는 «흐르는 중» 이라(<see cref="EngineRunning"/> 은 팝업·일시정지에만 false) 표시가 얼어붙은 상한에 눌려 <b>그 자리에 붙어 선다</b>.
        /// <list type="bullet">
        /// <item>관통형의 상한이 <c>MaxX</c>(사거리 끝) 였다 — 그 창을 지우는 것은 <b>엔진</b>(<c>Battle.cs</c> <c>pr.X &gt; pr.MaxX</c>)인데
        /// 보류 중엔 못 지운다 → 사거리 끝(대개 화면 오른쪽)에 서 있는다 = 주인이 본 «화면 끝에서 멈춤», 킬 연출과 겹칠 때만이라 «가끔».</item>
        /// <item>유도형의 상한이 표적이 죽으면 <c>pr.X</c>(엔진 x) 였다 — 보류 중엔 그 값이 안 움직이니 도끼가 <b>공중에 선다</b>(주인 T108 1-b «도끼가 여전히 멈춘다»).</item>
        /// </list>
        /// 그래서 둘 다 <b>상한을 두지 않는다</b>: 사거리 끝을 지나 화면 밖으로 계속 날아가고, 엔진이 다음 틱에 지우면 그림도 사라진다
        /// (화면에서는 «쭉 지나가 사라졌다» = 주인 T108 «쭉 지나가면서» 와 같은 그림). <b>엔진은 한 줄도 안 건드린다 — 시드 골든 불변.</b>
        /// </para>
        /// <b>표적이 살아 있는 유도형의 상한은 그대로 둔다</b> — 그걸 풀면 도끼가 표적을 지나쳐 날아간 뒤에야 맞는 그림이 된다.
        /// </summary>
        public double ProjLimit(Projectile pr)
        {
            if (pr.Kind == ProjKind.Spear || pr.Kind == ProjKind.Wave) return double.PositiveInfinity;   // 관통형: 상한 없음(T171)
            return pr.Target != null && pr.Target.Hp > 0 ? pr.Target.WorldX - EngineConst.ProjArriveDx : double.PositiveInfinity;   // 유도형: 표적이 살아 있을 때만 «맞는 자리»
        }

        /// <summary>
        /// 사거리 끝을 지난 투사체의 그림을 감추는 자리(T171) — 사거리 끝 + 레이아웃 폭의 이 비율.
        /// 상한을 없앴으므로 엔진이 지우기 전까지는 계속 날아간다. 화면 밖이라 어차피 안 보이지만,
        /// 엔진 보류가 길어질 때 좌표가 하염없이 커지지 않게 <b>그림만</b> 끈다(엔진 목록·<c>_projs</c> 는 그대로 두어 정리 경로는 한 곳 = 누수 0).
        /// </summary>
        public const float ProjOffscreenPad = 0.35f;

        /// <summary>
        /// 표시 x 가 엔진 x 를 따라잡을 때 한 프레임에 갈 수 있는 최대 배율(T108 2항 «스냅 금지»).
        /// 엔진이 앞서 있으면 예전에는 <c>shown = pr.X</c> 로 <b>툭 끌어당겼다</b> — 킬 연출이 풀리는 순간 창이 순간이동한 것이 그것이다.
        /// 이제는 평소 속도의 이 배까지만 더 가며 부드럽게 좁힌다(게이트도 이 값으로 «프레임 간 이동량 ≤ 속도 × dt × 1.5» 를 단언한다).
        /// </summary>
        public const float ProjCatchUpMul = 1.5f;

        /// <summary>투사체 그림을 표시 x <paramref name="shown"/> 에 놓는다(도끼는 포물선 y + 회전 · 나머지는 각만). <see cref="SyncProjectiles"/> 와 «지우는 프레임의 닿음»(T397 ⓑ)이 같이 쓴다.</summary>
        void PlaceProjectile(Projectile pr, GameObject go, double shown)
        {
            float yf = FootY - 0.045f;
            if (pr.Kind == ProjKind.Axe)
            {
                double span = System.Math.Max(1, pr.TargetX0 - pr.StartX); float t = Mathf.Clamp01((float)((shown - pr.StartX) / span));
                yf -= (float)(D.Ui.AxeArc * span / WorldCam.LayoutW) * Mathf.Sin(t * Mathf.PI) * 0.5f;
                float flownSec = pr.Spd > 1e-6 ? (float)((shown - pr.StartX) / pr.Spd) : 0f;      // ⓒ 날아간 «시간» × 360°/s = 초당 1바퀴(반시계 · 방향 종전 그대로)
                go.transform.rotation = Quaternion.Euler(0, 0, -flownSec * AxeSpinDegPerSec);
            }
            else go.transform.rotation = Quaternion.Euler(0, 0, pr.Kind == ProjKind.Wave ? 0 : pr.Kind == ProjKind.Spear ? SpearAngle : ArrowAngle);
            go.transform.position = Pos(shown, yf, -0.2f);
        }
        /// <summary>
        /// 엔진이 뺀 투사체 → 지우던 프레임의 표시 x(T397 ⓑ 로 끌어다 놓은 뒤의 값 · 자·진단용). 최근 <see cref="GoneKeep"/> 개만 둔다 — 한 판에 투사체가 수백이라 다 들고 있지 않는다.
        /// </summary>
        public readonly Dictionary<Projectile, double> ProjGoneAt = new Dictionary<Projectile, double>();
        readonly Queue<Projectile> _goneOrder = new Queue<Projectile>();
        public const int GoneKeep = 32;
        void RememberGone(Projectile pr, double shownAtGone)
        {
            ProjGoneAt[pr] = shownAtGone; _goneOrder.Enqueue(pr);
            while (_goneOrder.Count > GoneKeep) { var old = _goneOrder.Dequeue(); if (!_goneOrder.Contains(old)) ProjGoneAt.Remove(old); }
        }

        void SyncProjectiles(float dt)
        {
            var live = new HashSet<Projectile>(G.Projs);
            foreach (var pr in G.Projs)
            {
                if (!_projs.TryGetValue(pr, out var go))
                {
                    go = new GameObject("proj:" + pr.Kind); go.transform.SetParent(_root, false);
                    if (pr.Kind == ProjKind.Wave) { var fx = Fx.Spawn("fx.wave", Vector3.zero, 0.6f, 0, go.transform, true); if (fx != null) fx.transform.localRotation = Quaternion.Euler(0, 0, -90); }
                    else
                    {
                        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 350;
                        sr.sprite = _app.Assets.Sprite(pr.Kind == ProjKind.Axe ? "cm.meleeB.axe" : pr.Kind == ProjKind.Spear ? "cm.spear" : "cm.rangedA.arrow");
                        go.transform.localScale = Vector3.one * (pr.Kind == ProjKind.Spear ? 1.1f : 0.9f);
                        var trail = Fx.Spawn("fx.trail", Vector3.zero, 0.35f, 0, go.transform, true); if (trail != null) trail.transform.localPosition = Vector3.zero;
                    }
                    if (!Silent) Audio.Sfx(pr.Kind == ProjKind.Axe ? "snd.axe" : "snd.arrow", 0.6f);   // 발사음(T28) — 도끼/그 외(화살·창·검기)
                    _projs[pr] = go; _projX[pr] = pr.X;
                }
                // 표시 x(T86 ⓐ) — 엔진과 같은 px/s 로 전진(정규화 t·고정 duration 금지) · 엔진이 앞서면 엔진 · 적중 자리는 안 앞지른다
                if (!_projX.TryGetValue(pr, out double shown)) shown = pr.X;
                if (Silent) shown = pr.X;
                else
                {
                    // 팝업·일시정지·판 종료(EngineRunning=false)일 때만 선다 — 킬 연출로 엔진이 보류된 동안에도 간다(T108 1항)
                    // T397 — dt 는 이미 배속이 곱해진 «엔진 초» 다(BattleScreen `Sync(dt * _speed)`). 여기서 Speed 를 또 곱으면 x2 에서 그림이 엔진의 두 배로 난다.
                    double frameStep = pr.Spd * dt * ProjCatchUpMul;   // 이 프레임에 화면이 움직일 수 있는 최대(스냅 금지의 상한)
                    if (EngineRunning)
                    {
                        double step = pr.Spd * dt;
                        // 엔진이 앞서 있으면 스냅하지 않고 «조금 더 빨리» 좁힌다(T108 2항 · 최대 ProjCatchUpMul 배)
                        if (shown < pr.X) step = System.Math.Min(pr.X - shown, step * ProjCatchUpMul);
                        shown += step;
                    }
                    // T397 ⓐ — 유도형(도끼·화살)의 그림은 엔진 x 를 «한 틱 걸음» 이상 앞서지 않는다. 엔진은 틱(1/30초) 단위로, 그림은 프레임 단위로 가므로 한 틱 안의 보간은
                    //   허용하되, 엔진이 늦어지면(틱 상한 · 따라잡기) 그림이 먼저 «맞는 자리» 에 가서 서 있는 대신 엔진 걸음에 맞춰 늦어진다 — 닿는 순간 = 엔진 타격 틱.
                    //   관통형(창·검기)은 안 건다 — 그쪽은 «어떤 상태에서도 안 멈춘다»(T108·T171)가 계약이고 맞는 자리가 없다.
                    bool homingKind = pr.Kind != ProjKind.Spear && pr.Kind != ProjKind.Wave;
                    if (homingKind) { double lead = pr.X + pr.Spd * EngineConst.Dt; if (shown > lead) shown = lead; }
                    // 적중 자리(ProjLimit)를 앞질렀으면 되돌리되 «한 프레임 걸음» 까지만 — 여기서 바로 끌어당기면 그것도 스냅이다(T108 2항 · 표적이 걸어오면 유도형의 상한이 뒤로 밀린다).
                    double lim = ProjLimit(pr);
                    if (shown > lim) { double target = System.Math.Max(pr.X, lim); shown = target >= shown ? target : System.Math.Max(target, shown - frameStep); }
                }
                _projX[pr] = shown;
                PlaceProjectile(pr, go, shown);
                // T171 — 사거리 끝을 한참 지나면 그림만 끈다(화면 밖이라 안 보이던 것이지만 좌표가 커지는 것을 여기서 멈춘다).
                // 엔진 목록·_projs 는 안 건드린다 — 정리는 아래 dead 한 곳이라야 누수가 없다.
                // **관통형에만 건다** — `MaxX` 를 엔진이 채우는 것은 창·검기뿐이고(Battle.cs 431·436), 유도형(도끼·화살)은 0 으로 남는다.
                // 그것을 모르고 `MaxX + 여유` 를 유도형에도 쓰면 «0 + 189px» 이라 스폰하자마자 도끼가 사라진다. 유도형은 표적 곁에서 엔진이 곧 지우므로 멀리 못 간다.
                bool piercing = pr.Kind == ProjKind.Spear || pr.Kind == ProjKind.Wave;
                bool gone = !Silent && piercing && shown > pr.MaxX + ProjOffscreenPad * WorldCam.LayoutW;
                if (go.activeSelf == gone) go.SetActive(!gone);
            }
            var dead = new List<Projectile>(); foreach (var kv in _projs) if (!live.Contains(kv.Key)) dead.Add(kv.Key);
            foreach (var k in dead)
            {
                // T397 ⓑ — 엔진이 이번 틱에 «맞혀서» 뺀 유도형(도끼·화살)인데 그림이 아직 맞는 자리 앞이면, 지우는 이 프레임에 그 자리까지 끌어다 놓고 지운다
                //   (Destroy 는 프레임 끝이라 이 프레임은 그 자리에 그려진다 = «닿음» · 피격 연출(Hit 이벤트)도 같은 프레임). 표적이 먼저 죽어 빠진 것(엔진 x 가 맞는 자리 앞)은
                //   닿은 적이 없으니 그대로 지운다. 유도형은 ⓐ 로 늘 엔진 한 틱 안에 붙어 있어 이 걸음은 길어야 한 틱이다 — 순간이동이 아니다.
                double gx = _projX.TryGetValue(k, out double sx) ? sx : k.X;
                bool homing = k.Kind != ProjKind.Spear && k.Kind != ProjKind.Wave;
                if (homing && k.Target != null && !Silent)
                {
                    double arrive = k.Target.WorldX - EngineConst.ProjArriveDx;
                    if (k.X >= arrive && gx < arrive) { gx = arrive; PlaceProjectile(k, _projs[k], gx); }
                }
                RememberGone(k, gx);
                Object.Destroy(_projs[k]); _projs.Remove(k); _projX.Remove(k);
            }
            var liveA = new HashSet<EnemyArrow>(G.Arrows);
            foreach (var a in G.Arrows)
            {
                if (!_arrows.TryGetValue(a, out var go))
                {
                    go = new GameObject("arrow"); go.transform.SetParent(_root, false);
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _app.Assets.Sprite("cm.rangedB.arrow"); sr.sortingOrder = 350; sr.flipX = true;
                    go.transform.localScale = Vector3.one * 0.85f; go.transform.rotation = Quaternion.Euler(0, 0, EnemyArrowAngle);
                    if (!Silent) Audio.Sfx("snd.arrow", 0.5f);
                    _arrows[a] = go; _arrowX[a] = a.X;
                }
                // 표시 x(T179 ⓑ) — 투사체(T86 ⓐ · 위 SyncProjectiles)와 같은 방식이다. 적 화살은 왼쪽으로 나므로 부호만 뒤집힌다.
                if (!_arrowX.TryGetValue(a, out double ashown)) ashown = a.X;
                if (Silent) ashown = a.X;
                else if (EngineRunning)   // 팝업·일시정지·판 종료일 때만 선다 — 킬 연출로 엔진이 보류된 동안에도 간다(그래서 화살이 공중에 안 뜬다)
                {
                    double astep = D.Combat.EnemyArrowSpeed * dt;   // T397 — dt 는 이미 배속이 곱해진 엔진 초(투사체와 같은 고침)
                    if (ashown > a.X) astep = System.Math.Min(ashown - a.X, astep * ProjCatchUpMul);   // 엔진이 앞서(= 더 왼쪽) 있으면 스냅하지 않고 조금 더 빨리 좁힌다
                    ashown -= astep;
                    // ⚠ T397 ⛑ — 적 화살에는 «엔진 한 틱 앞섬 상한» 을 걸지 않는다. 도끼·창은 킬 연출 보류 중에도 `StepProjectiles` 로 엔진 x 가 흐르지만
                    //   적 화살은 `Tick` 안에서만 나아가 보류 동안 `a.X` 가 멎는다 — 상한을 걸면 그림도 한 틱 뒤에 서서 «보류 중에도 안 멈춘다»(T179 ⓑ)가 깨진다(런 997 빨강 · 결정 1141 ⛑).
                    //   맞는 자리 클램프(아래 `ahit`)가 화살의 상한이다.
                }
                // 엔진이 «맞았다» 고 보는 자리(Battle.cs `a.X <= P.WorldX + ArrowHitDx`)를 앞지르지 않는다 — 앞지르면 맞기도 전에 플레이어를 지나가 버린다(투사체의 ProjLimit 과 같은 구실).
                double ahit = G.P.WorldX + EngineConst.ArrowHitDx;
                if (ashown < ahit) ashown = System.Math.Min(a.X, ahit);
                _arrowX[a] = ashown;
                go.transform.position = Pos(ashown, FootY - 0.05f, -0.2f);
                // T233 — 명중선에 닿는 순간 그림을 끈다(주인 2026-09-08 «맞는 순간 바로 안 사라지고 멈췄다 사라진다»).
                // 왜 «멈춰» 보였나: 표시는 늘 엔진보다 먼저 이 선에 닿고(태어난 프레임에 둘이 같아 걸음 상한이 안 걸린다),
                // 닿으면 위 클램프가 그림을 그 자리에 세운다 — 엔진이 다음 틱에 지울 때까지 서 있는 것이 «기다림» 이다.
                // 클램프는 그대로 둔다(맞기 전에 플레이어를 지나가지 않는다는 구실은 옳다) — 서 있는 대신 사라지게만 한다.
                // 꼴은 바로 위 투사체의 `gone`(T171) 그대로다: 엔진 목록·_arrows 는 안 건드리고(정리는 아래 deadA 한 곳) 그림만 끈다.
                bool goneA = !Silent && ashown <= ahit;
                if (go.activeSelf == goneA) go.SetActive(!goneA);
            }
            var deadA = new List<EnemyArrow>(); foreach (var kv in _arrows) if (!liveA.Contains(kv.Key)) deadA.Add(kv.Key);
            foreach (var k in deadA) { Object.Destroy(_arrows[k]); _arrows.Remove(k); _arrowX.Remove(k); }
        }

        // ───────────────────────── 연출 이벤트 ─────────────────────────
        Vector3 EnemyPos(EnemyState e, float up = 0.45f) => e != null ? FoePos(e.WorldX, FootY) + Vector3.up * up : _player.transform.position + Vector3.up * up;

        // ───────────────────────── 번개 특전(T70 · 주인 «번개 이펙트 인터넷에서 에셋 받아서 되게 해줘») ─────────────────────────
        /// <summary>번개 한 줄기의 세로 길이 = 적 키의 이 배(지시서 T70 2항 «적 키의 1.5~2배»).</summary>
        public const float BoltHeightMul = 1.8f;
        /// <summary>같은 틱에 여러 적에게 떨어질 때 적마다 어긋나는 시차(초 · 지시서 T70 2항 · T49 stagger 감각).</summary>
        public const float BoltStagger = 0.05f;
        public const string LightningName = "Lightning";
        float _boltClock = -1f; int _boltSeq;

        /// <summary>
        /// 적 하나에게 번개 한 줄기 — 하늘에서 발밑까지 내리꽂히고(시트 애니 · <see cref="Fx.PlaySheet"/>), 닿는 순간 종전 전기 튀김(`fx.bolt` · CFXR)을 작게.
        /// 시트(`fx.lightning`)가 없으면 종전 그대로 튀김만 뿌린다.
        /// </summary>
        void Lightning(EnemyState e)
        {
            if (_boltClock != _clock) { _boltClock = _clock; _boltSeq = 0; }
            float delay = _boltSeq++ * BoltStagger;
            float hPct = e != null && e.IsBoss ? Layout.EnemyHeight * (float)D.Enemies.BossSizeMul : Layout.EnemyHeight;
            float span = WorldCam.PctH(hPct) * BoltHeightMul;
            var hit = EnemyPos(e, 0.5f);
            var go = Fx.PlaySheet("fx.lightning", Fx.LightningCols, Fx.LightningFrames, Fx.LightningFps,
                                  EnemyPos(e, 0f) + Vector3.up * (span * 0.5f), span / Fx.LightningSpanAtScale1,
                                  Fx.LightningTiltDeg, delay, _root, LightningName,
                                  () => { if (_root != null) Fx.Spawn("fx.bolt", hit, 0.6f, 1.2f, _root); });
            if (go == null) Fx.Spawn("fx.bolt", hit, 0.6f, 1.2f, _root);
        }
        Vector3 PlayerPos(float up = 0.5f) => _player.transform.position + Vector3.up * up;

        /// <summary>BattleScreen 호환 — 이벤트는 <see cref="AfterTick"/> 이 틱마다 직접 처리한다(타격 묶음 판별에 틱 경계가 필요).</summary>
        public void Handle(BattleEvent ev) => Route(ev);

        void Present(BattleEvent ev)
        {
            var flash = _app.Assets.Material("mat.hitFlash");
            switch (ev.Kind)
            {
                case EvKind.Hit:
                {
                    var p = EnemyPos(ev.Enemy);
                    // T152 — 치명타는 숫자 뒤 «!» 대신 왼쪽에 치명타 아이콘(주인 «치명타 아이콘+데미지») · 색·크기는 종전 그대로
                    Pop(UiKit.Fmt(ev.Value), p + Vector3.up * 0.5f, ev.Crit ? Palette.PopCrit : Palette.White, ev.Crit ? 50 : 38, SrcIcon(ev.Src) ?? (ev.Crit ? CritIconKey : null));   // T458 2항 — 출처가 있으면 그 그림이 이긴다(치명타는 색·크기로도 말한다)
                    // T504(주인 2026-09-12 «힛 이펙트가 너무 반짝임 · 너무 글로우임») — CFXR 가산 프리팹(fx.hit/fx.crit) 대신 알파 블렌드 알갱이 버스트 · 치명타는 더 많이·크게·치명타 색
                    Fx.HitBurst(p, ev.Crit ? Palette.PopCrit : Fx.HitGrain, ev.Crit ? 1.4f : 1f, ev.Crit ? 16 : 10);
                    Audio.Sfx(ev.Crit ? "snd.crit" : "snd.hit", ev.Crit ? 1f : 0.8f);
                    if (ev.Enemy != null && _enemies.TryGetValue(ev.Enemy, out var v)) { v.Rig.Flash(flash, CharacterRig.HitFlashSeconds); v.Rig.transform.DOKill(true); v.Rig.transform.DOPunchPosition(new Vector3(0.06f, 0, 0), 0.15f, 1, 0).SetLink(v.Rig.gameObject); }   // SetLink(T56) — 사망 연출 뒤 Remove 로 파괴돼도 경고 0
                    break;
                }
                case EvKind.Miss: Pop("MISS", EnemyPos(ev.Enemy, 0.9f), Palette.PopMiss, 30); Fx.Spawn("fx.evade", EnemyPos(ev.Enemy, 0.4f), 0.5f, 1f); Audio.Sfx("snd.miss", 0.6f); break;
                case EvKind.Kill: break;   // 사망 연출은 Sync 에서 (Dead 플래그 · Hold 가 풀린 뒤)
                case EvKind.PlayerHit:
                {
                    if (ev.Value > 0.5) Pop("-" + UiKit.Fmt(ev.Value), PlayerPos(1.1f) + new Vector3((float)D.Ui.PopShieldDx / WorldCam.PPU, 0.15f, 0), Palette.Hex(D.Ui.PopShield), 34);
                    if (ev.Value2 > 0.5) Pop("-" + UiKit.Fmt(ev.Value2), PlayerPos(1.0f), Palette.Hex(D.Ui.PopHp), 40);
                    Fx.HitBurst(PlayerPos(0.45f), Fx.HitGrain, 0.8f, 8);   // T504
                    _player.Flash(flash, CharacterRig.HitFlashSeconds);
                    Audio.Sfx("snd.hurt", 0.8f);
                    break;
                }
                case EvKind.PlayerEvade: Pop("회피", PlayerPos(1.1f), Palette.PopEvade, 34); Fx.Spawn("fx.evade", PlayerPos(0.3f), 0.5f, 1f); break;
                case EvKind.Ward: if (ev.Value >= 0) { Pop("방어막", PlayerPos(1.2f), Palette.Sky, 30, SrcIcon(ev.Src)); Fx.Spawn("fx.ward", PlayerPos(0.4f), 0.6f, 1.5f); } else Pop("막음!", PlayerPos(1.1f), Palette.Sky, 34); break;
                case EvKind.Ignore: Pop("무시", PlayerPos(1.1f), Palette.Gray, 32); break;
                case EvKind.Heal: Pop("+" + UiKit.Fmt(ev.Value), PlayerPos(1.05f), Palette.PopHeal, 36, SrcIcon(ev.Src)); Fx.Spawn("fx.heal", PlayerPos(0.4f), 0.7f, 1.5f); break;
                case EvKind.Repair: Pop("+" + UiKit.Fmt(ev.Value), PlayerPos(1.2f) + Vector3.left * 0.2f, Palette.Hex(D.Ui.PopShield), 32, SrcIcon(ev.Src)); break;
                case EvKind.Stun: Pop("스턴", EnemyPos(ev.Enemy, 1.0f), Palette.Yellow, 30, SrcIcon(ev.Src)); break;
                case EvKind.Bolt: Lightning(ev.Enemy); break;
                case EvKind.Reflect: Pop("반사 " + UiKit.Fmt(ev.Value), EnemyPos(ev.Enemy, 0.95f), Palette.Sky, 32, SrcIcon(ev.Src)); break;
                // T152 3항 — 반격 팝도 같은 표기로 맞춘다(같은 «치명타» 를 두 가지로 적지 않는다 · 결정 기록)
                case EvKind.Counter: Pop("반격 " + UiKit.Fmt(ev.Value), EnemyPos(ev.Enemy, 0.95f), Palette.Orange, 34, SrcIcon(ev.Src) ?? (ev.Crit ? CritIconKey : null)); Fx.Spawn("fx.hit", EnemyPos(ev.Enemy), 0.5f, 1f); break;
                case EvKind.LevelUp: Pop("LEVEL UP!", PlayerPos(1.3f), Palette.Yellow, 46); Fx.Spawn("fx.levelup", PlayerPos(0.5f), 1f, 2f); Audio.Sfx("snd.levelup"); break;
                case EvKind.Perk:
                {
                    var perk = ev.Text != null ? D.Perks.Perks.Find(p => p.Id == ev.Text) : null;
                    if (perk != null) { Pop(perk.Name, PlayerPos(1.35f), Palette.PerkColor(perk), 34); Audio.Sfx("snd.perk"); }
                    break;
                }
                case EvKind.Proj: case EvKind.BossWarn: case EvKind.Text: default: break;
            }
        }

        /// <summary>
        /// 치명타 팝 앞에 붙는 아이콘의 카탈로그 키(T152 · 주인 2026-09-07 «치명타 데미지일시에 데미지 텍스트 치명타 아이콘+데미지 이런식으로») —
        /// 스탯 «치명타 확률» 이 쓰는 그림 그대로라 <b>새 그림 0</b>(§1 «에셋은 주인 에셋만»).
        /// </summary>
        public const string CritIconKey = "pi.critical";

        /// <summary>
        /// T458 2항 — 뜬 글자 왼쪽에 <b>«왜 떴는지»</b> 그림(주인 2026-09-12 «해당 특전이나 해당 장비로 인해 번개 나왔으면 해당 꺼 아이콘이 데미지 텍스트에 떠야 함»).
        /// 엔진이 실어 준 출처(<see cref="BattleEvent.Src"/> · T458 1항)를 <b>이미 있는 그림</b>으로 옮긴다 — 새 그림 0.
        /// <para>
        /// · 특전 = <see cref="Icons.Perk"/>(특전 카드·버프 칸이 쓰는 그 키 그대로 · 계열이 같으면 같은 그림) ·
        /// 장비 = <b>무엇이 났는지</b>로 갈린 낱말 셋(<see cref="BattleState.SrcGearAxe"/>·<see cref="BattleState.SrcGearHeal"/>·<see cref="BattleState.SrcGearThorns"/>) ·
        /// <see cref="BattleState.SrcPet"/> = 펫 뱃지와 같은 그림.
        /// </para>
        /// <para>
        /// ⚠ <b>T491 — 여기 «장비» 한 낱말을 두면 안 된다.</b> 전에는 <c>"gear"</c> 하나가 다섯 자리를 겹쳐 썼고 이 줄이 그것을
        /// 무조건 도끼로 옮겼다 ⇒ 장비가 준 <b>«+회복» 숫자에 도끼가 붙어 떴다</b>. 게다가 그때 내가 바로 이 자리에
        /// «오늘 장비 소환은 셋 다 도끼다» 라고 적어 둬서, 낱말에 자리를 더 얹은 다음 회차가 <b>그 문장을 믿고 지나갔다</b>.
        /// </para>
        /// <para>
        /// <b>같은 효과면 특전이 줬든 장비가 줬든 같은 그림</b>이다 — 회복은 <c>p_evadeHeal</c> 이 받는 하트,
        /// 가시는 <c>p_thorns</c> 가 받는 그 그림. 새 그림은 0장이고, 주인이 보는 것은 «무엇 때문에 떴나» 라서
        /// «어느 장비냐» 보다 «무엇이 났나» 가 그 물음에 곧장 답한다.
        /// </para>
        /// <para>⚠ <b>모르면 <c>null</c></b> — 없는 출처에 아무 그림이나 붙이면 화면이 «아는 척» 을 한다(T458 1항이 엔진에서 지킨 그 규칙을 화면에서도 지킨다).</para>
        /// </summary>
        public static string SrcIcon(string src)
        {
            if (string.IsNullOrEmpty(src)) return null;
            if (src == BattleState.SrcGearAxe) return "pi.axe";
            if (src == BattleState.SrcGearHeal) return "pi.heart";       // p_evadeHeal 이 받는 그 그림
            if (src == BattleState.SrcGearThorns) return "pi.damage";    // p_thorns 가 받는 그 그림
            if (src == BattleState.SrcPet) return "ui.petIcon";
            return src.StartsWith("p_") ? Icons.Perk(src) : null;
        }
        /// <summary>팝 아이콘 한 변 = 글자 크기의 이 배(숫자 높이와 눈으로 같아 보이는 비율).</summary>
        public const float PopIconMul = 1f;
        /// <summary>아이콘과 숫자 사이 틈(프레임 px).</summary>
        public const float PopIconGap = 6f;
        /// <summary>팝 아이콘 오브젝트 이름(게이트가 이 이름으로도 찾는다).</summary>
        public const string PopIconName = "PopIcon";
        /// <summary>팝이 떠오르는 높이(프레임 px · 종전 값 그대로) — 월드에서는 × <see cref="WorldPerPx"/>.</summary>
        public const float PopRisePx = 140f;

        Transform PopRoot()
        {
            if (_popRoot == null) { _popRoot = new GameObject(PopsName).transform; _popRoot.SetParent(_root, false); }
            return _popRoot;
        }

        /// <summary>데미지 팝 — <b>월드</b>(«Pops» · _root 아래)에 3D TMP 를 띄우고 DOTween 으로 올라가며 사라진다(T502 · 종전엔 프레임 층 uGUI 였다).
        /// 크기는 호출부 값 × <see cref="TextSize.BattleNumberMul"/>(1.3 · T63 «데미지 팝·전투 숫자는 지금보다 1.3배») 뒤 본문 하한 — 그 값은 프레임 px 이고 <see cref="WorldPerPx"/> 가 화면에서 같은 크기로 만든다.
        /// <paramref name="iconKey"/> 를 주면 숫자 <b>왼쪽</b>에 그 그림이 붙어 «아이콘 + 데미지» 한 덩어리로 뜬다(T152).</summary>
        public void Pop(string s, Vector3 worldPos, Color color, int size, string iconKey = null)
        {
            if (_pops == null || _root == null) return;
            size = Mathf.RoundToInt(size * TextSize.BattleNumberMul);
            var t = WorldText(PopRoot(), "Pop", s, size, color, PopOrder, TextKind.Body);
            size = Mathf.RoundToInt(t.fontSize);
            t.rectTransform.sizeDelta = new Vector2(400, size * 1.5f);
            // 월드 자리 그대로 + 좌우 흔들기(종전 ±30px)
            var tr = t.transform;
            tr.position = new Vector3(worldPos.x + Random.Range(-30f, 30f) * WorldPerPx, worldPos.y, worldPos.z);
            tr.localScale = Vector3.one * (WorldPerPx * 0.6f);
            var icon = PopIcon(t, iconKey, size);   // T152 — 글자의 자식이라 아래 트윈 하나에 아이콘까지 같이 따라 올라간다
            var seq = DOTween.Sequence().SetLink(t.gameObject);   // SetLink(T56) — 전투 종료로 월드가 먼저 파괴돼도 경고 0
            seq.Append(tr.DOScale(WorldPerPx, 0.12f).SetEase(Ease.OutBack));
            seq.Join(tr.DOLocalMoveY(tr.localPosition.y + PopRisePx * WorldPerPx, 0.9f).SetEase(Ease.OutCubic));
            seq.Insert(0.45f, t.DOFade(0f, 0.45f));
            if (icon != null) seq.Insert(0.45f, icon.DOFade(0f, 0.45f));   // 글자와 같이 사라진다(아이콘만 남지 않게)
            seq.OnComplete(() => { if (t != null) Object.Destroy(t.gameObject); });   // 아이콘은 자식이라 같이 사라진다(누수 0)
        }

        /// <summary>
        /// 팝 숫자 <b>왼쪽</b>에 아이콘 하나(T152 · T502 부터 SpriteRenderer). 글자 rect(폭 400 · 가운데 정렬) 안에서 숫자가 실제로 차지하는 폭(<c>preferredWidth</c>)을 재
        /// 그 왼쪽에 붙이고, «아이콘 + 틈 + 숫자» 덩어리가 원래 자리에 가운데로 남도록 글자를 그 절반만큼 오른쪽으로 민다.
        /// 아이콘은 글자의 자식이라 자리·크기는 글자의 로컬 단위(= 프레임 px)로 적는다 — 한 변 <c>d</c> px 정사각에 비율을 지켜 맞춘다(옛 <c>preserveAspect</c>).
        /// 키가 없으면 아무것도 안 만든다(종전 팝 그대로).
        /// </summary>
        SpriteRenderer PopIcon(TMP_Text t, string iconKey, int size)
        {
            if (string.IsNullOrEmpty(iconKey)) return null;
            float d = size * PopIconMul;
            var go = new GameObject(PopIconName); go.transform.SetParent(t.transform, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _app.Assets.Sprite(iconKey); sr.sortingOrder = PopIconOrder;
            float b = sr.sprite != null ? Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y) : 0f;
            go.transform.localScale = Vector3.one * (b > 1e-4f ? d / b : 1f);
            go.transform.localPosition = new Vector3(-(t.preferredWidth * 0.5f + PopIconGap + d * 0.5f), 0f, 0f);
            t.transform.localPosition += new Vector3((d + PopIconGap) * 0.5f * WorldPerPx, 0f, 0f);
            return sr;
        }
    }
}
