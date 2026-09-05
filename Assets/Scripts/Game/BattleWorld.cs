using System.Collections.Generic;
using DG.Tweening;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 전투 월드 그리기 — 엔진(<see cref="BattleState"/>)은 숫자만 갖고, 여기서 주인 에셋으로 보여준다.
    /// ● 좌표: sim.js 월드 x(레이아웃 px) → 프레임 레이아웃 x = (worldX − 플레이어 x) × zoom + playerX×540 (ui.json camera) → <see cref="WorldCam.ToWorld"/>.
    ///   세로는 ref-layout ② 의 % (지면 띠 30~51% · 발 줄 40%).
    /// ● 캐릭터: Layer Lab CharacterMaker Character.prefab + <see cref="CharacterRig"/> (스킨은 카탈로그 cm.*).
    /// ● 배경·노드: Layer Lab Environment 스프라이트(env.*) · 이펙트: CFXR(fx.*) · 피격 플래시: AllIn1 HitFlash 머티리얼.
    /// </summary>
    public sealed class BattleWorld
    {
        readonly App _app; readonly BattleState G; readonly GameData D;
        readonly Transform _root; readonly RectTransform _pops;
        readonly float _zoom; readonly float _playerX;             // ui.json camera.zoom · playerX(프레임 폭 비율)
        const float CharBaseHeight = 0.85f;                          // Character.prefab 스케일 1 의 키(유니티 단위 · 조사값)
        const float FootY = Layout.PlayerFootY / 100f;

        // 플레이어
        CharacterRig _player; SpriteRenderer _pBarBg, _pBarFill; double _pStrikePrev; bool _pDeadShown;
        // 적
        sealed class EnemyView { public EnemyState E; public CharacterRig Rig; public SpriteRenderer BarBg, BarFill; public double StrikePrev; public float DieT = -1; public GameObject StunFx; }
        readonly Dictionary<EnemyState, EnemyView> _enemies = new Dictionary<EnemyState, EnemyView>();
        // 투사체
        readonly Dictionary<Projectile, GameObject> _projs = new Dictionary<Projectile, GameObject>();
        readonly Dictionary<EnemyArrow, GameObject> _arrows = new Dictionary<EnemyArrow, GameObject>();
        // 노드 · 배경
        sealed class NodeView { public BattleNode N; public GameObject Go; public GameObject FxGo; public bool Dimmed; public bool Warned; }
        readonly List<NodeView> _nodes = new List<NodeView>();
        readonly List<SpriteRenderer> _fieldTiles = new List<SpriteRenderer>(), _roadTiles = new List<SpriteRenderer>(), _trimTiles = new List<SpriteRenderer>();
        sealed class Prop { public SpriteRenderer Sr; public double WorldX; public float YFrac; }
        readonly List<Prop> _props = new List<Prop>();
        float _tileW; int _tileCols;
        double _goldPrev; Vector3 _lastKillPos;

        public BattleWorld(App app, BattleState g, RectTransform popsLayer)
        {
            _app = app; G = g; D = g.D; _pops = popsLayer;
            _zoom = (float)D.Ui.CameraZoom; _playerX = (float)(D.Ui.PlayerX * WorldCam.LayoutW);
            _root = new GameObject("World").transform;
            BuildGround(); BuildProps(); BuildNodes(); BuildPlayer();
            _goldPrev = G.Gold;
        }
        public void Dispose() { if (_root != null) Object.Destroy(_root.gameObject); }

        // ───────────────────────── 좌표 ─────────────────────────
        float LayoutX(double worldX) => (float)((worldX - G.P.WorldX) * _zoom) + _playerX;
        Vector3 Pos(double worldX, float yFrac, float z = 0) => WorldCam.ToWorld(LayoutX(worldX), yFrac, z);
        Vector2 FramePos(double worldX, float yFrac) => new Vector2(LayoutX(worldX) * (UiKit.FrameW / WorldCam.LayoutW), (1f - yFrac) * UiKit.FrameH);
        static float ScaleForHeightPct(float pct) => WorldCam.PctH(pct) / CharBaseHeight;
        static int SortBase(float layoutX) => 100 + Mathf.Clamp((int)((WorldCam.LayoutW + 200 - layoutX) / 6f), 0, 180);

        // ───────────────────────── 배경 ─────────────────────────
        SpriteRenderer Sprite(string key, Transform parent, int order, string name = null)
        {
            var go = new GameObject(name ?? key); go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = _app.Assets.Sprite(key); sr.sortingOrder = order;
            return sr;
        }
        void BuildGround()
        {
            var ground = new GameObject("Ground").transform; ground.SetParent(_root, false);
            float bandTop = Layout.GroundBand.Y / 100f, bandBot = (Layout.GroundBand.Y + Layout.GroundBand.H) / 100f;
            float bandH = WorldCam.PctH(Layout.GroundBand.H);
            var field = _app.Assets.Sprite("env.field");
            float tile = field != null ? field.bounds.size.x : 1.28f;
            int rows = Mathf.Max(1, Mathf.RoundToInt(bandH / tile));
            float scale = bandH / (rows * tile); _tileW = tile * scale;
            _tileCols = Mathf.CeilToInt(WorldCam.LayoutW / WorldCam.PPU / _tileW) + 2;
            // 지면 띠 + 그 아래(HUD 패널 뒤)까지 같은 타일을 깐다 — 아래쪽은 어둡게
            int belowRows = Mathf.CeilToInt(WorldCam.PctH(100 - Layout.GroundBand.Y - Layout.GroundBand.H) / _tileW) + 1;
            for (int r = 0; r < rows + belowRows; r++)
                for (int c = 0; c < _tileCols; c++)
                {
                    var sr = Sprite("env.field", ground, -20, "field"); sr.transform.localScale = Vector3.one * scale;
                    float y = WorldCam.ToWorld(0, bandTop).y - (r + 0.5f) * _tileW;
                    sr.transform.position = new Vector3(0, y, 0); if (r >= rows) sr.color = new Color(0.55f, 0.6f, 0.5f);
                    _fieldTiles.Add(sr);
                }
            // 길(발 줄) + 위 경계 장식
            var road = _app.Assets.Sprite("env.road"); float roadScale = road != null ? _tileW / road.bounds.size.x : scale;
            for (int c = 0; c < _tileCols; c++)
            {
                var sr = Sprite("env.road", ground, -18, "road"); sr.transform.localScale = Vector3.one * roadScale;
                sr.transform.position = new Vector3(0, WorldCam.ToWorld(0, FootY).y - _tileW * 0.15f, 0); _roadTiles.Add(sr);
                var tr = Sprite("env.roadUp", ground, -17, "roadUp"); tr.transform.localScale = Vector3.one * roadScale * 0.5f;
                tr.transform.position = new Vector3(0, WorldCam.ToWorld(0, FootY).y - _tileW * 0.15f + _tileW * 0.5f, 0); _trimTiles.Add(tr);
            }
        }
        void ScrollGround()
        {
            float scroll = (float)(G.P.WorldX * _zoom / WorldCam.PPU);
            float left = WorldCam.ToWorld(0, 0).x - _tileW;
            float off = Mathf.Repeat(scroll, _tileW);
            int cols = _tileCols;
            for (int i = 0; i < _fieldTiles.Count; i++) { var p = _fieldTiles[i].transform.position; p.x = left + (i % cols) * _tileW - off + _tileW * 0.5f; _fieldTiles[i].transform.position = p; }
            for (int i = 0; i < _roadTiles.Count; i++) { var p = _roadTiles[i].transform.position; p.x = left + i * _tileW - off + _tileW * 0.5f; _roadTiles[i].transform.position = p; }
            for (int i = 0; i < _trimTiles.Count; i++) { var p = _trimTiles[i].transform.position; p.x = left + i * _tileW - off + _tileW * 0.5f; _trimTiles[i].transform.position = p; }
        }
        void BuildProps()
        {
            var props = new GameObject("Props").transform; props.SetParent(_root, false);
            double lastX = G.Nodes.Count > 0 ? G.Nodes[G.Nodes.Count - 1].X : 2000;
            var rng = new System.Random(G.Chapter * 7919);
            string[] back = { "env.tree", "env.bush", "env.tree", "env.stoneBig", "env.bush" };
            string[] front = { "env.mushroom", "env.stoneSmall", "env.bush" };
            for (double x = -600; x < lastX + 1200; x += 70 + rng.Next(0, 90))
            {
                bool isFront = rng.NextDouble() < 0.35;
                string key = isFront ? front[rng.Next(front.Length)] : back[rng.Next(back.Length)];
                var sr = Sprite(key, props, isFront ? 380 : -12, "prop"); sr.flipX = rng.NextDouble() < 0.5;
                float s = (isFront ? 0.55f : 0.9f) * (0.85f + (float)rng.NextDouble() * 0.3f);
                sr.transform.localScale = Vector3.one * s;
                _props.Add(new Prop { Sr = sr, WorldX = x, YFrac = isFront ? 0.47f + (float)rng.NextDouble() * 0.03f : 0.32f + (float)rng.NextDouble() * 0.03f });
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
        CharacterRig MakeChar(string name, CharacterRig.Skin skin, float heightPct, bool faceRight)
        {
            var prefab = _app.Assets.Prefab("cm.character");
            GameObject go = prefab != null ? Object.Instantiate(prefab, _root) : new GameObject(name);
            go.name = name;
            var rig = CharacterRig.Attach(go);
            rig.Apply(skin);
            rig.SetScale(ScaleForHeightPct(heightPct)); rig.Face(faceRight);
            rig.Play(CharacterRig.Idle);
            return rig;
        }
        static CharacterRig.Skin KnightSkin(bool shield) => new CharacterRig.Skin { Helmet = "cm.knight.helmet", HairHelmet = "cm.knight.hairHelmet", Chest = "cm.knight.chest", Sword = "cm.knight.sword", Shield = shield ? "cm.knight.shield" : null };
        static CharacterRig.Skin EnemySkin(EnemyState e)
        {
            if (e.IsBoss) return new CharacterRig.Skin { Helmet = "cm.boss.helmet", Chest = "cm.boss.chest", Axe = "cm.boss.axe", SkinColor = new Color(0.38f, 0.30f, 0.42f) };
            if (e.Ranged) return e.Skin % 2 == 0
                ? new CharacterRig.Skin { Helmet = "cm.rangedA.helmet", Bow = "cm.rangedA.bow", Arrow = "cm.rangedA.arrow", BowLines = true }
                : new CharacterRig.Skin { Helmet = "cm.rangedB.helmet", Chest = "cm.rangedB.chest", Bow = "cm.rangedB.bow", Arrow = "cm.rangedB.arrow", BowLines = true };
            switch (e.Skin % 3)
            {
                case 0: return new CharacterRig.Skin { Helmet = "cm.meleeA.helmet", Chest = "cm.meleeA.chest", Sword = "cm.meleeA.sword" };
                case 1: return new CharacterRig.Skin { Chest = "cm.meleeB.chest", Axe = "cm.meleeB.axe" };
                default: return new CharacterRig.Skin { Helmet = "cm.meleeC.helmet", Chest = "cm.meleeC.chest", Sword = "cm.meleeC.sword" };
            }
        }
        void MakeBar(Transform parent, float width, out SpriteRenderer bg, out SpriteRenderer fill, Color fillColor, int order)
        {
            var bgo = new GameObject("BarBg"); bgo.transform.SetParent(parent, false);
            bg = bgo.AddComponent<SpriteRenderer>(); bg.sprite = UiKit.White(); bg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f); bg.sortingOrder = order; bg.drawMode = SpriteDrawMode.Sliced; bg.size = new Vector2(width, 0.07f);
            var fgo = new GameObject("BarFill"); fgo.transform.SetParent(bgo.transform, false);
            fill = fgo.AddComponent<SpriteRenderer>(); fill.sprite = UiKit.White(); fill.color = fillColor; fill.sortingOrder = order + 1; fill.drawMode = SpriteDrawMode.Sliced; fill.size = new Vector2(width - 0.02f, 0.05f);
        }
        static void SetBar(SpriteRenderer bg, SpriteRenderer fill, double frac)
        {
            float w = bg.size.x - 0.02f; float f = Mathf.Clamp01((float)frac);
            fill.size = new Vector2(Mathf.Max(0.001f, w * f), fill.size.y);
            fill.transform.localPosition = new Vector3(-(w - w * f) / 2f, 0, 0);
        }
        void BuildPlayer()
        {
            _player = MakeChar("Player", KnightSkin(G.P.MaxSh > 0), Layout.PlayerHeight, true);
            _player.transform.position = Pos(G.P.WorldX, FootY);
            MakeBar(_player.transform.parent, WorldCam.PctW(Layout.PlayerFootBarW), out _pBarBg, out _pBarFill, Palette.Red, 392);
            _pBarBg.transform.SetParent(_root, false);
        }
        EnemyView Ensure(EnemyState e)
        {
            if (_enemies.TryGetValue(e, out var v)) return v;
            e.Skin = System.Math.Abs(e.Id * 2654435761L % 1000).GetHashCode();
            float h = e.IsBoss ? Layout.EnemyHeight * (float)D.Enemies.BossSizeMul : (!e.Ranged && e.Skin % 3 == 1 ? Layout.EnemyHeightBald : Layout.EnemyHeight);
            v = new EnemyView { E = e, Rig = MakeChar("Enemy" + e.Id, EnemySkin(e), h, false) };
            float barW = (float)(e.IsBoss ? D.Ui.BossBarW : D.Ui.EnemyBarW) / WorldCam.PPU;
            MakeBar(_root, barW, out v.BarBg, out v.BarFill, e.IsBoss ? Palette.Plum : Palette.Red, 395);
            _enemies[e] = v;
            return v;
        }

        // ───────────────────────── 매 프레임 ─────────────────────────
        public void Sync(float dt)
        {
            ScrollGround();
            foreach (var p in _props) { p.Sr.transform.position = Pos(p.WorldX, p.YFrac, 0); p.Sr.enabled = p.Sr.transform.position.x > -4f && p.Sr.transform.position.x < 4f; }
            foreach (var nv in _nodes)
            {
                nv.Go.transform.position = Pos(nv.N.X, FootY - 0.005f);
                bool vis = nv.Go.transform.position.x > -4.5f && nv.Go.transform.position.x < 4.5f; nv.Go.SetActive(vis);
                if (nv.N.Done && !nv.Dimmed) { nv.Dimmed = true; foreach (var sr in nv.Go.GetComponentsInChildren<SpriteRenderer>()) sr.color = Palette.A(sr.color, 0.55f); if (nv.FxGo != null) { Object.Destroy(nv.FxGo); nv.FxGo = null; } }
            }
            // 플레이어
            var P = G.P;
            _player.transform.position = Pos(P.WorldX, FootY);
            _player.SetSortingBase(SortBase(LayoutX(P.WorldX)));
            if (G.Dead) { if (!_pDeadShown) { _pDeadShown = true; _player.Play(CharacterRig.Dead, true); } }
            else if (G.Cleared) _player.Play(CharacterRig.Victory);
            else
            {
                if (P.StrikeT > _pStrikePrev) _player.Play(CharacterRig.Attack, true);
                else if (P.StrikeT <= 0) _player.Play(_moving ? CharacterRig.Walk : CharacterRig.Idle);
            }
            _pStrikePrev = P.StrikeT;
            _pBarBg.transform.position = Pos(P.WorldX, FootY + 0.012f); SetBar(_pBarBg, _pBarFill, P.MaxHp > 0 ? P.Hp / P.MaxHp : 0);
            _pBarBg.gameObject.SetActive(!G.Dead);
            // 적
            var seen = new HashSet<EnemyState>();
            foreach (var n in G.Nodes) foreach (var e in n.Enemies)
            {
                float lx = LayoutX(e.WorldX);
                if (lx > WorldCam.LayoutW + 120 || (e.Dead && !_enemies.ContainsKey(e))) continue;
                var v = Ensure(e); seen.Add(e);
                v.Rig.transform.position = Pos(e.WorldX, FootY);
                v.Rig.SetSortingBase(SortBase(lx));
                if (e.Dead)
                {
                    if (v.DieT < 0) { v.DieT = 0; v.Rig.Play(CharacterRig.Dead, true); _lastKillPos = v.Rig.transform.position; Fx.Spawn("fx.death", v.Rig.transform.position + Vector3.up * 0.4f, 0.8f); v.BarBg.gameObject.SetActive(false); if (v.StunFx != null) Object.Destroy(v.StunFx); }
                    v.DieT += dt; v.Rig.SetAlpha(Mathf.Clamp01(1.2f - v.DieT * 1.5f));
                    if (v.DieT > 0.85f) { Object.Destroy(v.Rig.gameObject); Object.Destroy(v.BarBg.gameObject); _enemies.Remove(e); }
                    continue;
                }
                if (e.Stun > 0) { v.Rig.Play(CharacterRig.Stun); if (v.StunFx == null) { v.StunFx = Fx.Spawn("fx.stun", Vector3.zero, 0.5f, 0, v.Rig.transform, true); if (v.StunFx != null) { v.StunFx.transform.localPosition = new Vector3(0, CharBaseHeight * 1.05f, -0.3f); v.StunFx.transform.localRotation = Quaternion.identity; } } }
                else { if (v.StunFx != null) { Object.Destroy(v.StunFx); v.StunFx = null; }
                    if (e.StrikeT > v.StrikePrev) v.Rig.Play(CharacterRig.Attack, true); else if (e.StrikeT <= 0) v.Rig.Play(CharacterRig.Idle); }
                v.StrikePrev = e.StrikeT;
                var b = v.Rig.Bounds();
                v.BarBg.transform.position = new Vector3(v.Rig.transform.position.x, b.max.y + 0.12f, 0);
                SetBar(v.BarBg, v.BarFill, e.MaxHp > 0 ? e.Hp / e.MaxHp : 0);
                if (e.IsBoss && !_bossWarned && lx < WorldCam.LayoutW) { _bossWarned = true; _app.Overlay.BossWarn(_app.Frame); Fx.Spawn("fx.bossWarn", v.Rig.transform.position + Vector3.up * 1.2f, 0.8f, 2.5f); }
            }
            var gone = new List<EnemyState>(); foreach (var kv in _enemies) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var e in gone) { var v = _enemies[e]; Object.Destroy(v.Rig.gameObject); Object.Destroy(v.BarBg.gameObject); _enemies.Remove(e); }
            // 투사체
            SyncProjectiles();
            // 골드 증가 → 팝 (엔진은 골드 이벤트를 따로 내지 않는다)
            if (G.Gold > _goldPrev + 0.5) { Pop("+" + UiKit.Fmt(G.Gold - _goldPrev) + " G", _lastKillPos + Vector3.up * 0.9f, Palette.PopGold, 34); }
            _goldPrev = G.Gold;
        }
        bool _moving, _bossWarned; double _prevPX;
        public void BeforeTick() { _prevPX = G.P.WorldX; }
        public void AfterTick() { _moving = G.P.WorldX > _prevPX + 1e-6; }

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
                    _projs[pr] = go;
                }
                float yf = FootY - 0.045f; float dx = 0;
                if (pr.Kind == ProjKind.Axe)
                {
                    double span = System.Math.Max(1, pr.TargetX0 - pr.StartX); float t = Mathf.Clamp01((float)((pr.X - pr.StartX) / span));
                    yf -= (float)(D.Ui.AxeArc * span / WorldCam.LayoutW) * Mathf.Sin(t * Mathf.PI) * 0.5f;
                    go.transform.rotation = Quaternion.Euler(0, 0, -t * 720f);
                }
                else go.transform.rotation = Quaternion.Euler(0, 0, pr.Kind == ProjKind.Wave ? 0 : -35f);
                go.transform.position = Pos(pr.X + dx, yf, -0.2f);
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

        public void Handle(BattleEvent ev)
        {
            var flash = _app.Assets.Material("mat.hitFlash");
            switch (ev.Kind)
            {
                case EvKind.Hit:
                {
                    var p = EnemyPos(ev.Enemy);
                    Pop(UiKit.Fmt(ev.Value) + (ev.Crit ? "!" : ""), p + Vector3.up * 0.5f, ev.Crit ? Palette.PopCrit : Palette.White, ev.Crit ? 50 : 38);
                    Fx.Spawn(ev.Crit ? "fx.crit" : "fx.hit", p, ev.Crit ? 0.25f : 0.6f, 1.2f);
                    if (ev.Enemy != null && _enemies.TryGetValue(ev.Enemy, out var v)) { v.Rig.Flash(flash, 0.1f); v.Rig.transform.DOKill(true); v.Rig.transform.DOPunchPosition(new Vector3(0.06f, 0, 0), 0.15f, 1, 0); }
                    break;
                }
                case EvKind.Miss: Pop("MISS", EnemyPos(ev.Enemy, 0.9f), Palette.PopMiss, 30); Fx.Spawn("fx.evade", EnemyPos(ev.Enemy, 0.4f), 0.5f, 1f); break;
                case EvKind.Kill: break;   // 사망 연출은 Sync 에서 (Dead 플래그)
                case EvKind.PlayerHit:
                {
                    if (ev.Value > 0.5) Pop("-" + UiKit.Fmt(ev.Value), PlayerPos(1.1f) + new Vector3((float)D.Ui.PopShieldDx / WorldCam.PPU, 0.15f, 0), Palette.Hex(D.Ui.PopShield), 34);
                    if (ev.Value2 > 0.5) Pop("-" + UiKit.Fmt(ev.Value2), PlayerPos(1.0f), Palette.Hex(D.Ui.PopHp), 40);
                    Fx.Spawn("fx.hit", PlayerPos(0.45f), 0.5f, 1f);
                    _player.Flash(flash, 0.1f);
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
                case EvKind.LevelUp: Pop("LEVEL UP!", PlayerPos(1.3f), Palette.Yellow, 46); Fx.Spawn("fx.levelup", PlayerPos(0.5f), 1f, 2f); break;
                case EvKind.Perk:
                {
                    var perk = ev.Text != null ? D.Perks.Perks.Find(p => p.Id == ev.Text) : null;
                    if (perk != null) Pop(perk.Name, PlayerPos(1.35f), Palette.PerkColor(perk), 34);
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
            var seq = DOTween.Sequence();
            seq.Append(rt.DOScale(1f, 0.12f).SetEase(Ease.OutBack));
            seq.Join(rt.DOAnchorPosY(rt.anchoredPosition.y + 140f, 0.9f).SetEase(Ease.OutCubic));
            seq.Insert(0.45f, t.DOFade(0f, 0.45f));
            seq.OnComplete(() => { if (t != null) Object.Destroy(t.gameObject); });
        }
    }
}
