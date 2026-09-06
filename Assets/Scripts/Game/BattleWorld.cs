using System.Collections.Generic;
using DG.Tweening;
using KkomaKnight.Core;
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
    ///   스크롤 원점은 엔진 x 가 아니라 <see cref="ShownPX"/> — 사망 연출이 아직 안 나온 적이 있으면 출발을 미룬다(T20 · 엔진 좌표 불변).
    /// ● 발밑 바(T35 · 주인 강조 · `02_battle.jpg`): 빨강(HP) 위에 파랑(실드) 2단 · 각 단 안에 흰 숫자(«현재») · 바 폭 = 캐릭터 폭(2/3 배율) · 실드 0 이면 파란 단 숨김.
    ///   숫자는 월드가 아니라 팝 층(<c>_pops</c> · 프레임 px)의 uGUI Text 로 그리고 매 프레임 바 위치로 옮긴다(<see cref="FootText"/> · Pop 과 같은 월드→프레임 변환 · 픽셀 크기가 결정적).
    /// </summary>
    public sealed class BattleWorld
    {
        readonly App _app; readonly BattleState G; readonly GameData D;
        readonly Transform _root; readonly RectTransform _pops;
        readonly float _zoom; readonly float _playerX;             // ui.json camera.zoom · playerX(프레임 폭 비율)
        public const float CharBaseHeight = 0.85f;                          // Character.prefab 스케일 1 의 키(유니티 단위 · 조사값)
        const float FootY = Layout.PlayerFootY / 100f;
        const float RoadCenterFrac = 0.41f;                          // 데모 씬의 길 중심(y −0.402)이 놓이는 프레임 비율 — 발 줄 40% 을 품는다(길 띠 1.48u = ±6.5% · 34.5~47.5% · ref-layout 지면 띠 30~51% 안)
        /// <summary>데모 씬 1u 가 화면에서 차지하는 프레임 높이 비율 — 데모 구성을 통째로 <see cref="Layout.MapScale"/>(0.6) 배로 그린다(T19 · 1u → 0.6 유니티 단위 = 프레임의 1/19).</summary>
        const float UnitFrac = WorldCam.PPU * Layout.MapScale / WorldCam.LayoutH;
        const float SpreadRamp = 150f;                               // 1배 → WorldSpacing 배로 부드럽게 넘어가는 월드 px 구간

        // 플레이어
        CharacterRig _player; SpriteRenderer _pBarBg, _pBarFill, _pShBg, _pShFill; Text _pHpTxt, _pShTxt; double _pStrikeTick; bool _pDeadShown; EnemyState _pTarget;
        int _holdPlayer;                                             // 아직 «칼이 안 내려온» 적 공격 수 — 0 일 때만 표시 체력을 엔진 값으로 맞춘다
        public double ShownHp { get; private set; } public double ShownSh { get; private set; }
        /// <summary>플레이어 발밑 2단 바(T35) — 테스트·진단용 읽기: 빨강 HP 바 · 파랑 실드 바 · 각 단 안의 숫자 글자.</summary>
        public SpriteRenderer PlayerHpBar => _pBarBg; public SpriteRenderer PlayerShBar => _pShBg; public Text PlayerHpText => _pHpTxt; public Text PlayerShText => _pShTxt;
        /// <summary>적과 조우 중인가(살아 있는 적이 화면 안) — HUD 상단 진행바가 이때 주황으로 찬다(T35 · 레퍼런스 03).</summary>
        public bool Engaged { get; private set; }
        // 적
        sealed class EnemyView { public EnemyState E; public CharacterRig Rig; public SpriteRenderer BarBg, BarFill; public Text BarTxt; public double StrikeTick; public float DieT = -1; public GameObject StunFx; public double ShownHp; public int Hold; }
        readonly Dictionary<EnemyState, EnemyView> _enemies = new Dictionary<EnemyState, EnemyView>();
        // 연출 지연 — 공격 모션의 타격 순간까지 묶어 두는 이벤트
        sealed class Strike { public CharacterRig Rig; public int HitCount0; public float At; public EnemyState Target; public bool OnPlayer; public readonly List<BattleEvent> Evs = new List<BattleEvent>(); }
        readonly List<Strike> _strikes = new List<Strike>();
        Strike _pStrike; readonly Dictionary<EnemyState, Strike> _eStrikes = new Dictionary<EnemyState, Strike>();
        float _clock;
        /// <summary>타격 연출이 아직 남아 있나 — 화면(BattleScreen)은 이 동안 팝업(레벨업·사망)을 열지 않고 기다린다.</summary>
        public bool Busy => _strikes.Count > 0;
        // T20 — 표시 기준 x(스크롤 원점). 엔진은 킬 다음 틱(1/30초)에 바로 다음 적으로 걷지만(Battle.Tick · alive[0] · sim.js 와 동일 · 불변),
        // 화면은 사망 연출을 «칼이 내려오는 순간»(Strike · Hold)까지 미루므로 그대로 두면 «살아 보이는» 적을 두고 출발한다(주인 지적).
        // → 죽었는데 아직 사망 연출이 안 나온 적(Dead && Hold>0)이 하나라도 있으면 표시 원점을 멈추고, 풀리면 걷기 속도 CatchUpMul 배로 엔진 x 를 따라잡는다. 엔진 좌표는 손대지 않는다.
        double _shownPX;
        const double CatchUpMul = 2, SnapGap = 600;                 // 따라잡기 = 걷기 2배 · 그보다 큰 격차(탭 복귀 등)는 그냥 맞춘다
        /// <summary>화면이 쓰는 플레이어 월드 x(스크롤 원점) — 킬 연출이 걸려 있는 동안 엔진 <c>P.WorldX</c> 보다 뒤에 머문다.</summary>
        public double ShownPX => _shownPX;
        /// <summary>죽었지만 아직 사망 연출이 시작되지 않은(칼이 안 내려온) 적이 있는가 — 이 동안 화면은 출발하지 않는다.</summary>
        public bool KillPending { get { foreach (var kv in _enemies) if (kv.Key.Dead && kv.Value.Hold > 0) return true; return false; } }
        /// <summary>배속(x1/x2) — 애니 속도와 지연 시계에 함께 건다.</summary>
        public float TimeScale = 1f;
        /// <summary>따라잡기 중(탭 숨김 뒤 복귀) — 공격 모션·팝·이펙트를 만들지 않고 이벤트만 비운다.</summary>
        public bool Silent;
        // 투사체
        readonly Dictionary<Projectile, GameObject> _projs = new Dictionary<Projectile, GameObject>();
        readonly Dictionary<EnemyArrow, GameObject> _arrows = new Dictionary<EnemyArrow, GameObject>();
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
        }

        /// <summary>월드 루트(Ground·Props·Nodes 의 부모) — 테스트·진단용 읽기(T19 PlayMode 맵 테스트가 바닥·길·소품 스케일을 본다).</summary>
        public Transform Root => _root;
        /// <summary>이 판의 맵 테마 — 테스트·진단용 읽기.</summary>
        public Theme MapTheme => _theme;

        public BattleWorld(App app, BattleState g, RectTransform popsLayer)
        {
            _app = app; G = g; D = g.D; _pops = popsLayer;
            _zoom = (float)D.Ui.CameraZoom; _playerX = (float)(D.Ui.PlayerX * WorldCam.LayoutW);
            _theme = Theme.ForChapter(g.Chapter);
            _shownPX = G.P.WorldX;
            _root = new GameObject("World").transform;
            BuildGround(); BuildProps(); BuildNodes(); BuildPlayer();
            _goldPrev = G.Gold; ShownHp = G.P.Hp; ShownSh = G.P.Sh;
        }
        public void Dispose()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            // 발밑 숫자는 팝 층(uGUI)에 있다 — 월드와 함께 지운다(화면이 Pops 를 통째로 비우기도 하지만 순서에 기대지 않는다)
            if (_pHpTxt != null) Object.Destroy(_pHpTxt.gameObject); if (_pShTxt != null) Object.Destroy(_pShTxt.gameObject); _pHpTxt = _pShTxt = null;
            foreach (var kv in _enemies) if (kv.Value.BarTxt != null) Object.Destroy(kv.Value.BarTxt.gameObject);
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
        float LayoutX(double worldX) => Spread(worldX - _shownPX) * _zoom + _playerX;   // 원점 = 표시 기준 x(T20) — 킬 연출 중에는 엔진 x 보다 뒤
        Vector3 Pos(double worldX, float yFrac, float z = 0) => WorldCam.ToWorld(LayoutX(worldX), yFrac, z);
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
                    var sr = Sprite(_theme.Field, ground, -20, "field"); if (sr.sprite == null) sr.sprite = field;
                    sr.transform.localScale = fieldScale;
                    sr.transform.position = new Vector3(0, fieldY + (r - (rows - 1) * 0.5f) * tileH, 0);
                    _fieldTiles.Add(sr);
                }
            var road = _app.Assets.Sprite(_theme.Road) ?? _app.Assets.Sprite("env.road");
            var roadScale = new Vector3(MapLayouts.RoadScaleX * Layout.MapScale, MapLayouts.RoadScaleY * Layout.MapScale, 1f);       // 데모: 128px × (22.47, 2.46) = 28.8 × 3.15u 띠
            float roadY = WorldCam.ToWorld(0, DemoY(MapLayouts.RoadCenterY)).y;
            float roadW = (road != null ? road.bounds.size.x : 1.28f) * roadScale.x; int roadCols = Mathf.CeilToInt(WorldCam.LayoutW / WorldCam.PPU / roadW) + 2;
            for (int c = 0; c < roadCols; c++)
            {
                var sr = Sprite(_theme.Road, ground, -18, "road"); if (sr.sprite == null) sr.sprite = road;
                sr.transform.localScale = roadScale; sr.transform.position = new Vector3(0, roadY, 0); _roadTiles.Add(sr);
            }
        }
        void ScrollGround()
        {
            float scroll = (float)(_shownPX * _zoom / WorldCam.PPU);
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
        /// 물결 경계(Road_up)는 씬처럼 길 위(y ≈ +1.13)·아래(≈ −2.0) 양쪽 — 표에 소품과 똑같이 들어 있어 따로 처리하지 않는다(T19).
        /// 정렬: 발 줄(40%)보다 위에 뿌리를 둔 소품은 캐릭터 뒤, 아래는 앞 · 납작한 것(풀·꽃·물결 경계)은 늘 바닥(길 바로 위) — 데모 렌더 순서(Field &lt; Road &lt; Road_up &lt; 나머지 y 내림차순)와 같다.
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
            for (double x0 = start; x0 < to; x0 += period)
                foreach (var p in layout)
                {
                    float yf = DemoY(p.Y);
                    if (yf < -0.15f || yf > 0.72f) continue;                        // 화면 위 밖 · HUD 패널 뒤는 만들지 않는다
                    var sp = _app.Assets.Sprite(p.Key);
                    bool flat = p.Key.EndsWith(".roadUp") || (sp != null && sp.bounds.size.y * Mathf.Abs(p.Sy) < 0.35f);   // 풀·꽃(납작) 은 늘 바닥 — 데모 치수 기준. 물결 경계(roadUp)는 높이와 무관하게 늘 길 바로 위(Road_up_Desert 만 43px = 0.43u 라 문턱을 넘겼다 · T45 · CI #51)
                    int order = flat ? -16 : p.Y > footDemoY ? Mathf.Max(-60, -12 - (int)((p.Y - footDemoY) * 3f)) : Mathf.Min(470, 381 + (int)((footDemoY - p.Y) * 5f));
                    var pr = AddProp(props, p.Key, x0 + p.X * unitPx, yf, 1f, order, false);
                    pr.Sr.transform.localScale = new Vector3(p.Sx * Layout.MapScale, p.Sy * Layout.MapScale, 1f);
                }
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
        void MakeBar(Transform parent, float width, float height, out SpriteRenderer bg, out SpriteRenderer fill, Color fillColor, int order)
        {
            var bgo = new GameObject("BarBg"); bgo.transform.SetParent(parent, false);
            bg = bgo.AddComponent<SpriteRenderer>(); bg.sprite = UiKit.White(); bg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f); bg.sortingOrder = order; bg.drawMode = SpriteDrawMode.Sliced; bg.size = new Vector2(width, height);
            var fgo = new GameObject("BarFill"); fgo.transform.SetParent(bgo.transform, false);
            fill = fgo.AddComponent<SpriteRenderer>(); fill.sprite = UiKit.White(); fill.color = fillColor; fill.sortingOrder = order + 1; fill.drawMode = SpriteDrawMode.Sliced; fill.size = new Vector2(width - 0.02f, height - 0.02f);
        }
        static void SetBar(SpriteRenderer bg, SpriteRenderer fill, double frac)
        {
            float w = bg.size.x - 0.02f; float f = Mathf.Clamp01((float)frac);
            fill.size = new Vector2(Mathf.Max(0.001f, w * f), fill.size.y);
            fill.transform.localPosition = new Vector3(-(w - w * f) / 2f, 0, 0);
        }
        /// <summary>발밑 바 안의 숫자(T35) — 팝 층의 uGUI Text(흰 글자 · 외곽선). 글자 높이는 바 높이(FootBarH % · 프레임 px)에서 잰다 — 픽셀 상수 없음.</summary>
        Text FootText(string name)
        {
            if (_pops == null) return null;
            int size = Mathf.RoundToInt(UiKit.FrameH * Layout.FootBarH / 100f * 0.8f);
            var t = UiKit.Text(_pops, "", size, Palette.White, TextAnchor.MiddleCenter, false, true); t.name = name;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
            var rt = t.rectTransform; rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(400, size * 1.4f);
            return t;
        }
        /// <summary>숫자 글자를 바(월드 위치)의 한가운데로 — Pop 과 같은 월드 → 레이아웃 → 프레임 px 변환. 글자는 바뀔 때만 다시 쓴다(uGUI 재구성 최소화).</summary>
        static void PlaceFootText(Text t, Vector3 worldPos, string s, bool visible)
        {
            if (t == null) return;
            if (t.gameObject.activeSelf != visible) t.gameObject.SetActive(visible);
            if (!visible) return;
            float lx = (worldPos.x * WorldCam.PPU) + WorldCam.LayoutW / 2f;
            float yFrac = 0.5f - worldPos.y * WorldCam.PPU / WorldCam.LayoutH;
            t.rectTransform.anchoredPosition = new Vector2(lx * (UiKit.FrameW / WorldCam.LayoutW), (1f - yFrac) * UiKit.FrameH);
            if (t.text != s) t.text = s;
        }
        void BuildPlayer()
        {
            _player = MakeChar("Player", CharacterRig.PlayerSkin(D, _app.Save, G.P.MaxSh > 0), Layout.PlayerHeight, true);   // 장착 외형 반영 — 장비 화면(HeroView)과 같은 표(GearLook)
            _player.transform.position = Pos(_shownPX, FootY);
            // 발밑 2단 바(T35 · 주인 강조): 빨강(HP) 위 · 파랑(실드) 아래 · 같은 높이 · 각 단 안에 흰 숫자. 폭은 캐릭터와 같은 배율(2/3 · T14)
            float pBarW = WorldCam.PctW(Layout.PlayerFootBarW) * Layout.CharScale;
            MakeBar(_root, pBarW, WorldCam.PctH(Layout.FootBarH), out _pBarBg, out _pBarFill, Palette.Red, 392);
            MakeBar(_root, pBarW, WorldCam.PctH(Layout.FootShBarH), out _pShBg, out _pShFill, Palette.Hex(D.Ui.PopShield), 392);
        }
        EnemyView Ensure(EnemyState e)
        {
            if (_enemies.TryGetValue(e, out var v)) return v;
            e.Skin = System.Math.Abs(e.Id * 2654435761L % 1000).GetHashCode();
            float h = e.IsBoss ? Layout.EnemyHeight * (float)D.Enemies.BossSizeMul : Layout.EnemyHeight;
            v = new EnemyView { E = e, Rig = MakeChar("Enemy" + e.Id, EnemySkin(e), h, false), StrikeTick = e.StrikeT, ShownHp = e.Hp };
            float barW = (float)(e.IsBoss ? D.Ui.BossBarW : D.Ui.EnemyBarW) / WorldCam.PPU * Layout.CharScale;   // 캐릭터와 같은 배율(2/3 · T14)
            MakeBar(_root, barW, WorldCam.PctH(Layout.FootBarH), out v.BarBg, out v.BarFill, e.IsBoss ? Palette.Plum : Palette.Red, 395);
            v.BarTxt = FootText("FootTxt:Enemy" + e.Id);   // 적은 실드가 없으므로(엔진 EnemyState 에 Sh 없음) 빨간 단 하나 + 숫자(레퍼런스 03 «2555»)
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
                if (v.BarTxt == null || !v.BarTxt.gameObject.activeSelf || v.BarTxt.text != UiKit.Fmt(System.Math.Ceiling(v.ShownHp))) return false;
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
            if (Silent) { _pStrikeTick = P.StrikeT; foreach (var kv in _enemies) kv.Value.StrikeTick = kv.Key.StrikeT; G.Events.Clear(); _shownPX = P.WorldX; return; }
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
            // 표시 원점(T20): 사망 연출이 아직 안 나온 적이 있으면 멈춤 · 없으면 걷기 CatchUpMul 배까지로 엔진 x 를 따라잡는다(평소엔 격차 0 → 엔진 걸음 그대로)
            double gap = P.WorldX - _shownPX; bool hold = KillPending;
            if (Silent || gap < 0 || gap > SnapGap) _shownPX = P.WorldX;
            else if (!hold && gap > 0) { double v = G.C.PlayerSpeed * P.WalkMul * (P.Dash ? G.C.DashMul : 1) * CatchUpMul; _shownPX = System.Math.Min(P.WorldX, _shownPX + v * dt); }
            _moving = !hold && (_engineMoving || P.WorldX - _shownPX > 1.0);
            _player.Tick(dt);
            _player.transform.position = Pos(_shownPX, FootY);
            _player.SetSortingBase(SortBase(LayoutX(_shownPX)));
            if (G.Dead) { if (!_pDeadShown && _holdPlayer == 0) { _pDeadShown = true; _player.Play(CharacterRig.Dead, true); } }
            else if (G.Cleared) { if (!_player.Attacking) _player.Play(CharacterRig.Victory); }
            else if (!_player.Attacking) _player.Play(_moving ? CharacterRig.Walk : CharacterRig.Idle);
            _pBarBg.transform.position = Pos(_shownPX, Layout.FootHpBarY / 100f); SetBar(_pBarBg, _pBarFill, P.MaxHp > 0 ? ShownHp / P.MaxHp : 0);
            _pBarBg.gameObject.SetActive(!_pDeadShown);
            _pShBg.transform.position = Pos(_shownPX, Layout.FootShBarY / 100f); SetBar(_pShBg, _pShFill, P.MaxSh > 0 ? ShownSh / P.MaxSh : 0);
            _pShBg.gameObject.SetActive(!_pDeadShown && P.MaxSh > 0);   // 실드 0 이면 파란 단 숨김(T35)
            if (_pHpTxt == null) { _pHpTxt = FootText("FootTxt:PlayerHp"); _pShTxt = FootText("FootTxt:PlayerSh"); }   // 팝 층은 화면이 새 판마다 비우므로 여기서(첫 Sync) 만든다
            PlaceFootText(_pHpTxt, _pBarBg.transform.position, UiKit.Fmt(System.Math.Ceiling(ShownHp)), _pBarBg.gameObject.activeSelf);
            PlaceFootText(_pShTxt, _pShBg.transform.position, UiKit.Fmt(System.Math.Ceiling(ShownSh)), _pShBg.gameObject.activeSelf);
            // 적
            var seen = new HashSet<EnemyState>(); bool engaged = false;
            foreach (var n in G.Nodes) foreach (var e in n.Enemies)
            {
                float lx = LayoutX(e.WorldX);
                if (lx > WorldCam.LayoutW + 120 || (e.Dead && !_enemies.ContainsKey(e))) continue;
                var v = Ensure(e); seen.Add(e);
                v.Rig.Tick(dt);
                v.Rig.transform.position = Pos(e.WorldX, FootY);
                v.Rig.SetSortingBase(SortBase(lx));
                if (v.Hold == 0) v.ShownHp = e.Hp;
                if (e.Dead && v.Hold == 0)
                {
                    if (v.DieT < 0) { v.DieT = 0; v.Rig.Play(CharacterRig.Dead, true); _lastKillPos = v.Rig.transform.position; Fx.Spawn("fx.death", v.Rig.transform.position + Vector3.up * 0.4f, 0.8f); if (!Silent) Audio.Sfx("snd.kill", 0.9f); v.BarBg.gameObject.SetActive(false); PlaceFootText(v.BarTxt, Vector3.zero, "", false); if (v.StunFx != null) { Object.Destroy(v.StunFx); v.StunFx = null; } }
                    v.DieT += dt; v.Rig.SetAlpha(Mathf.Clamp01(1.2f - v.DieT * 1.5f));
                    if (v.DieT > 0.85f) Remove(v);
                    continue;
                }
                if (e.Stun > 0 && !e.Dead) { v.Rig.Play(CharacterRig.Stun); if (v.StunFx == null) { v.StunFx = Fx.Spawn("fx.stun", Vector3.zero, 0.5f, 0, v.Rig.transform, true); if (v.StunFx != null) { v.StunFx.transform.localPosition = new Vector3(0, CharBaseHeight * 1.05f, -0.3f); v.StunFx.transform.localRotation = Quaternion.identity; } } }
                else { if (v.StunFx != null) { Object.Destroy(v.StunFx); v.StunFx = null; } if (!v.Rig.Attacking) v.Rig.Play(CharacterRig.Idle); }
                v.BarBg.transform.position = Pos(e.WorldX, Layout.FootHpBarY / 100f);
                SetBar(v.BarBg, v.BarFill, e.MaxHp > 0 ? v.ShownHp / e.MaxHp : 0);
                PlaceFootText(v.BarTxt, v.BarBg.transform.position, UiKit.Fmt(System.Math.Ceiling(v.ShownHp)), v.BarBg.gameObject.activeSelf);
                if (!e.Dead && lx < WorldCam.LayoutW) engaged = true;
                if (e.IsBoss && !_bossWarned && lx < WorldCam.LayoutW) { _bossWarned = true; _app.Overlay.BossWarn(_app.Frame); Fx.Spawn("fx.bossWarn", v.Rig.transform.position + Vector3.up * 1.2f, 0.8f, 2.5f); Audio.Bgm("bgm.boss"); }
            }
            var gone = new List<EnemyView>(); foreach (var kv in _enemies) if (!seen.Contains(kv.Key)) gone.Add(kv.Value);
            foreach (var v in gone) Remove(v);
            Engaged = engaged;
            // 투사체
            SyncProjectiles();
            // 골드 증가 → 팝 (엔진은 골드 이벤트를 따로 내지 않는다)
            if (G.Gold > _goldPrev + 0.5) { Pop("+" + UiKit.Fmt(G.Gold - _goldPrev) + " G", _lastKillPos + Vector3.up * 0.9f, Palette.PopGold, 34); if (!Silent) Audio.Sfx("snd.coin", 0.7f); }
            _goldPrev = G.Gold;
        }

        void SyncProjectiles()
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
                    _projs[pr] = go;
                }
                float yf = FootY - 0.045f;
                if (pr.Kind == ProjKind.Axe)
                {
                    double span = System.Math.Max(1, pr.TargetX0 - pr.StartX); float t = Mathf.Clamp01((float)((pr.X - pr.StartX) / span));
                    yf -= (float)(D.Ui.AxeArc * span / WorldCam.LayoutW) * Mathf.Sin(t * Mathf.PI) * 0.5f;
                    go.transform.rotation = Quaternion.Euler(0, 0, -t * 720f);
                }
                else go.transform.rotation = Quaternion.Euler(0, 0, pr.Kind == ProjKind.Wave ? 0 : -35f);
                go.transform.position = Pos(pr.X, yf, -0.2f);
            }
            var dead = new List<Projectile>(); foreach (var kv in _projs) if (!live.Contains(kv.Key)) dead.Add(kv.Key);
            foreach (var k in dead) { Object.Destroy(_projs[k]); _projs.Remove(k); }
            var liveA = new HashSet<EnemyArrow>(G.Arrows);
            foreach (var a in G.Arrows)
            {
                if (!_arrows.TryGetValue(a, out var go))
                {
                    go = new GameObject("arrow"); go.transform.SetParent(_root, false);
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _app.Assets.Sprite("cm.rangedB.arrow"); sr.sortingOrder = 350; sr.flipX = true;
                    go.transform.localScale = Vector3.one * 0.85f; go.transform.rotation = Quaternion.Euler(0, 0, 200f);
                    if (!Silent) Audio.Sfx("snd.arrow", 0.5f);
                    _arrows[a] = go;
                }
                go.transform.position = Pos(a.X, FootY - 0.05f, -0.2f);
            }
            var deadA = new List<EnemyArrow>(); foreach (var kv in _arrows) if (!liveA.Contains(kv.Key)) deadA.Add(kv.Key);
            foreach (var k in deadA) { Object.Destroy(_arrows[k]); _arrows.Remove(k); }
        }

        // ───────────────────────── 연출 이벤트 ─────────────────────────
        Vector3 EnemyPos(EnemyState e, float up = 0.45f) => e != null ? Pos(e.WorldX, FootY) + Vector3.up * up : _player.transform.position + Vector3.up * up;
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
                    Pop(UiKit.Fmt(ev.Value) + (ev.Crit ? "!" : ""), p + Vector3.up * 0.5f, ev.Crit ? Palette.PopCrit : Palette.White, ev.Crit ? 50 : 38);
                    Fx.Spawn(ev.Crit ? "fx.crit" : "fx.hit", p, ev.Crit ? 0.25f : 0.6f, 1.2f);
                    Audio.Sfx(ev.Crit ? "snd.crit" : "snd.hit", ev.Crit ? 1f : 0.8f);
                    if (ev.Enemy != null && _enemies.TryGetValue(ev.Enemy, out var v)) { v.Rig.Flash(flash, 0.1f); v.Rig.transform.DOKill(true); v.Rig.transform.DOPunchPosition(new Vector3(0.06f, 0, 0), 0.15f, 1, 0).SetLink(v.Rig.gameObject); }   // SetLink(T56) — 사망 연출 뒤 Remove 로 파괴돼도 경고 0
                    break;
                }
                case EvKind.Miss: Pop("MISS", EnemyPos(ev.Enemy, 0.9f), Palette.PopMiss, 30); Fx.Spawn("fx.evade", EnemyPos(ev.Enemy, 0.4f), 0.5f, 1f); Audio.Sfx("snd.miss", 0.6f); break;
                case EvKind.Kill: break;   // 사망 연출은 Sync 에서 (Dead 플래그 · Hold 가 풀린 뒤)
                case EvKind.PlayerHit:
                {
                    if (ev.Value > 0.5) Pop("-" + UiKit.Fmt(ev.Value), PlayerPos(1.1f) + new Vector3((float)D.Ui.PopShieldDx / WorldCam.PPU, 0.15f, 0), Palette.Hex(D.Ui.PopShield), 34);
                    if (ev.Value2 > 0.5) Pop("-" + UiKit.Fmt(ev.Value2), PlayerPos(1.0f), Palette.Hex(D.Ui.PopHp), 40);
                    Fx.Spawn("fx.hit", PlayerPos(0.45f), 0.5f, 1f);
                    _player.Flash(flash, 0.1f);
                    Audio.Sfx("snd.hurt", 0.8f);
                    break;
                }
                case EvKind.PlayerEvade: Pop("회피", PlayerPos(1.1f), Palette.PopEvade, 34); Fx.Spawn("fx.evade", PlayerPos(0.3f), 0.5f, 1f); break;
                case EvKind.Ward: if (ev.Value >= 0) { Pop("방어막", PlayerPos(1.2f), Palette.Sky, 30); Fx.Spawn("fx.ward", PlayerPos(0.4f), 0.6f, 1.5f); } else Pop("막음!", PlayerPos(1.1f), Palette.Sky, 34); break;
                case EvKind.Ignore: Pop("무시", PlayerPos(1.1f), Palette.Gray, 32); break;
                case EvKind.Heal: Pop("+" + UiKit.Fmt(ev.Value), PlayerPos(1.05f), Palette.PopHeal, 36); Fx.Spawn("fx.heal", PlayerPos(0.4f), 0.7f, 1.5f); break;
                case EvKind.Repair: Pop("+" + UiKit.Fmt(ev.Value), PlayerPos(1.2f) + Vector3.left * 0.2f, Palette.Hex(D.Ui.PopShield), 32); break;
                case EvKind.Stun: Pop("스턴", EnemyPos(ev.Enemy, 1.0f), Palette.Yellow, 30); break;
                case EvKind.Bolt: Fx.Spawn("fx.bolt", EnemyPos(ev.Enemy, 0.5f), 0.6f, 1.2f); break;
                case EvKind.Reflect: Pop("반사 " + UiKit.Fmt(ev.Value), EnemyPos(ev.Enemy, 0.95f), Palette.Sky, 32); break;
                case EvKind.Counter: Pop("반격 " + UiKit.Fmt(ev.Value) + (ev.Crit ? "!" : ""), EnemyPos(ev.Enemy, 0.95f), Palette.Orange, 34); Fx.Spawn("fx.hit", EnemyPos(ev.Enemy), 0.5f, 1f); break;
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

        /// <summary>데미지 팝 — 프레임(UI) 층에 Text 를 띄우고 DOTween 으로 올라가며 사라진다.</summary>
        public void Pop(string s, Vector3 worldPos, Color color, int size)
        {
            if (_pops == null) return;
            var t = UiKit.Text(_pops, s, size, color, TextAnchor.MiddleCenter, false, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var rt = t.rectTransform; rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(400, 80);
            // 월드 → 레이아웃 → 프레임 px
            float lx = (worldPos.x * WorldCam.PPU) + WorldCam.LayoutW / 2f;
            float yFrac = 0.5f - worldPos.y * WorldCam.PPU / WorldCam.LayoutH;
            rt.anchoredPosition = new Vector2(lx * (UiKit.FrameW / WorldCam.LayoutW) + Random.Range(-30f, 30f), (1f - yFrac) * UiKit.FrameH);
            rt.localScale = Vector3.one * 0.6f;
            var seq = DOTween.Sequence().SetLink(t.gameObject);   // SetLink(T56) — 전투 종료로 팝 층이 먼저 파괴돼도 경고 0
            seq.Append(rt.DOScale(1f, 0.12f).SetEase(Ease.OutBack));
            seq.Join(rt.DOAnchorPosY(rt.anchoredPosition.y + 140f, 0.9f).SetEase(Ease.OutCubic));
            seq.Insert(0.45f, t.DOFade(0f, 0.45f));
            seq.OnComplete(() => { if (t != null) Object.Destroy(t.gameObject); });
        }
    }
}
