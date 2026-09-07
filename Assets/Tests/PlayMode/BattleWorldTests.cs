using System;
using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T20 — 주인 지적 «웨이브 내 적을 다 안 죽였는데 출발함». 원인은 연출: 엔진(Battle.Tick · sim.js 와 동일 · 불변)은 킬 다음 틱에 바로 걷는데,
    /// 화면은 사망 연출을 «칼이 내려오는 순간»(Strike · Hold)까지 미루므로 살아 보이는 적을 두고 출발했다.
    /// T50 — 주인 지시 «킬하고 나서 공격 모션 끝나고 나서 걸어가는 모션 나오면서 원래 걷기 속도로 다음 적 가야 함»: T20 의 «멈춤 → 걷기 2배 따라잡기» 폐지.
    /// 화면이 멈춰 있는 동안(<see cref="BattleWorld.KillPending"/> · <see cref="BattleWorld.KillAnimHold"/>) 엔진 틱을 보류(<see cref="BattleWorld.HoldEngine"/>)하므로
    /// 표시 원점 <see cref="BattleWorld.ShownPX"/> 은 늘 엔진 x 와 같고(격차 0) 걷는 속도는 엔진 속도(PlayerSpeed×WalkMul · 대시 ×DashMul) 그대로다.
    /// T51 — ① 대시 특전(p_killDash)도 공격 모션 뒤에 출발해 ×DashMul 로 걷는다 ② 적 사망 «펑» 이펙트(fx.death · CFXR Magic Poof)를 뿌리지 않는다.
    /// 실제 씬에서 한 방에 죽는 전투를 돌리며 매 프레임 단언한다. 빨간 줄 0 은 <see cref="PlayLog"/>(T11 규약 · LogAssert.NoUnexpectedReceived 금지).
    /// </summary>
    public class BattleWorldTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }
        const string DeathFxName = "CFXR Magic Poof(Clone)";   // catalog fx.death 프리팹의 인스턴스 이름(T51 ② · 뿌리지 않아야 한다)

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; Assert.IsNotNull(_app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");
            yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        sealed class Stats
        {
            public int HoldFrames, AnimHoldFrames, WalkFrames, WalkAnimFrames, DashWalkFrames, EnginePausedFrames, DeathFxFrames;
            /// <summary>T110 ⓐ — 화면에 뜬 «+N G» 골드 팝 글자 수(주인 «골드 +49G 이런 거 데미지 텍스트처럼 뜨는 거 하면 안 됨» → 0 이어야 한다).</summary>
            public int GoldPopFrames, DamagePopFrames;
            // DashMoveDt = DashDt 중 «엔진이 실제로 틱을 돈»(표시 원점이 전진한) 프레임의 시간만 (T65)
            public double DashAdv, DashDt, DashMoveDt;
        }

        /// <summary>한 방 킬 전투를 sec 초(실시간) 돌리며 T20·T50·T51 계약을 매 프레임 단언한다.</summary>
        IEnumerator Run(BattleScreen bs, BattleWorld world, BattleState G, float sec, Stats st)
        {
            double prev = world.ShownPX, prevEngine = G.P.WorldX; bool prevHold = world.HoldEngine;
            bool prevPending = world.KillPending, prevAnimHold = world.KillAnimHold;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < sec && !G.Over && !_app.Overlay.IsOpen && bs.World == world)
            {
                yield return null;   // App.Update(틱 → Sync) 뒤에 깨어난다
                var P = G.P;
                double now = world.ShownPX; double adv = now - prev; bool advanced = adv > 1e-9; prev = now;
                bool pending = world.KillPending, animHold = world.KillAnimHold, hold = world.HoldEngine;
                if (pending) st.HoldFrames++;
                if (animHold) st.AnimHoldFrames++;
                if (advanced) st.WalkFrames++;
                // T20 — 사망 연출이 안 나온 적이 있는데 출발하지 않는다
                // (T65) «멈춤이 시작되는 프레임» 은 뺀다 — 한 프레임에 엔진 틱이 여럿 돌면 킬 틱 앞의 걷기 틱(킬 이전의 접근 걸음)이 같은 프레임에 들어 있어
                // 그 프레임의 전진은 정상이다. 멈춤이 이미 걸려 있던(앞 프레임에도 참) 상태에서 전진하면 주인이 지적한 «살아 보이는 적 두고 출발» 이다.
                Assert.IsFalse(advanced && pending && prevPending, "사망 연출이 아직 안 나온 적이 있는데 화면이 출발했다(주인 지적 재현)");
                // T50 ⓑ — 킬 뒤 공격 모션 중에는 표시 원점 이동 0 (같은 이유로 시작 프레임 제외)
                Assert.IsFalse(advanced && animHold && prevAnimHold, "킬 뒤 공격 모션이 아직 안 끝났는데 화면이 출발했다(주인: 공격 모션 끝나고 걸어야 함)");
                // T50 ⓒ — 격차 0: 표시 원점 = 엔진 x (따라잡기 구간이 없다)
                Assert.AreEqual(P.WorldX, now, 1e-6, "표시 원점이 엔진 x 와 같아야 한다(T50 · 따라잡기 없음 — 엔진이 보류된다)");
                // 엔진 보류 — 앞 프레임부터 계속 hold 면 엔진 x 가 그대로
                if (prevHold && hold) { st.EnginePausedFrames++; Assert.AreEqual(prevEngine, P.WorldX, 1e-9, "킬 연출 동안 엔진 틱이 보류돼야 한다(HoldEngine)"); }
                prevHold = hold; prevEngine = P.WorldX; prevPending = pending; prevAnimHold = animHold;
                // T50 ⓐ — 프레임당 이동량 ≤ 원래 걷기 속도 × (프레임 dt + 틱 1개 양자화) — 2배 구간 0 (대시 특전을 가진 판은 ×DashMul 까지 · P.Dash 는 한 프레임 안에서 꺼질 수 있어 보유 여부로 본다)
                bool dashOwned = P.Has("p_killDash");
                double v = G.C.PlayerSpeed * P.WalkMul * (dashOwned ? G.C.DashMul : 1);
                Assert.LessOrEqual(adv, v * (Time.deltaTime + EngineConst.Dt) + 1e-6, "프레임당 이동이 원래 걷기 속도(PlayerSpeed×WalkMul" + (dashOwned ? "×DashMul" : "") + ")를 넘는다 — 따라잡기 가속 금지(T50)");
                if (advanced && world.PlayerAnim == CharacterRig.Walk) st.WalkAnimFrames++;
                // 대시 구간(P.Dash · 보류 아님)의 평균 속도 — 틱이 없는 프레임의 dt 도 넣어 «틱/프레임» 양자화가 평균을 부풀리지 않게
                if (P.Dash && !hold) { st.DashDt += Time.deltaTime; st.DashAdv += adv; if (advanced) { st.DashWalkFrames++; st.DashMoveDt += Time.deltaTime; } }
                // T51 ② — 사망 «펑» 이펙트 없음
                if (GameObject.Find(DeathFxName) != null) st.DeathFxFrames++;
                // T110 ⓐ — 골드 팝 «글자» 는 뜨지 않는다(데미지 숫자는 그대로 떠야 한다)
                foreach (var txt in _app.Frame.GetComponentsInChildren<UnityEngine.UI.Text>(false))
                {
                    if (txt == null || string.IsNullOrEmpty(txt.text)) continue;
                    if (txt.text.EndsWith(" G", StringComparison.Ordinal)) st.GoldPopFrames++;
                    else if (txt.text.Length > 0 && char.IsDigit(txt.text[0]) && txt.transform.parent != null && txt.transform.parent.name == "Pops") st.DamagePopFrames++;
                }
            }
        }

        static void Arm(BattleState G)
        {
            // 한 방에 죽인다(킬 → 출발이 웨이브마다 일어난다) · 레벨업·노드 팝업으로 엔진이 멈추지 않게
            G.P.Dmg = 1e6; G.P.Exp = int.MinValue / 2;
            foreach (var n in G.Nodes) if (n.Type == NodeType.Rest || n.Type == NodeType.Devil || n.Type == NodeType.Angel) n.Done = true;
        }

        [UnityTest]
        public IEnumerator PlayerNeverWalksWhileAKilledEnemyStillLooksAlive()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            double Tick1 = G.C.PlayerSpeed * EngineConst.Dt;   // 엔진 한 틱의 걸음(px · combat.json 에서) — 프레임 지터 허용치
            yield return RealSeconds(0.3f);
            Assert.LessOrEqual(G.P.WorldX - world.ShownPX, Tick1, "시작(킬 없음) 땐 표시 원점이 엔진 x 를 한 틱 안에서 따른다");
            Arm(G);
            Time.timeScale = 3f;   // 첫 웨이브까지 걷는 시간을 줄인다(엔진 틱은 dt 로 돈다)
            var st = new Stats();
            yield return Run(bs, world, G, 12f, st);
            Time.timeScale = 1f;
            Assert.Greater(st.HoldFrames, 0, "킬 연출 대기(칼이 내려오기 전)가 한 번은 있어야 시험이 성립한다");
            Assert.Greater(st.AnimHoldFrames, 0, "킬 뒤 공격 모션 대기(칼이 내려온 뒤 → 모션 끝)가 한 번은 있어야 한다(T50)");
            Assert.Greater(st.EnginePausedFrames, 0, "킬 연출 동안 엔진 틱이 보류된 프레임이 있어야 한다(T50)");
            Assert.Greater(st.WalkFrames, 0, "출발(원점 전진)이 있어야 한다");
            Assert.Greater(st.WalkAnimFrames, 0, "출발은 걷기 모션과 함께여야 한다(T50)");
            Assert.Greater(G.Kills, 0, "킬이 있어야 사망 이펙트 시험이 성립한다");
            Assert.AreEqual(0, st.DeathFxFrames, "적 사망 «펑» 이펙트(fx.death · Magic Poof)를 뿌리면 안 된다(T51 ② · 주인 지시)");
            Assert.AreEqual(0, st.DashWalkFrames, "대시 특전이 없으면 대시 걸음이 없다");
            // T110 ⓐ(주인 2026-09-07) — 골드는 데미지 텍스트처럼 뜨지 않는다. 데미지 팝은 그대로 뜬다(연출을 통째로 끈 것이 아니라는 증거).
            Assert.AreEqual(0, st.GoldPopFrames, "«+N G» 골드 팝 글자가 뜨면 안 된다(T110 ⓐ · 주인 지시)");
            Assert.Greater(st.DamagePopFrames, 0, "데미지 숫자 팝은 그대로 떠야 한다(골드 팝만 없앤 것)");
            _log.AssertNoRed("전투 진행(킬 → 공격 모션 끝 → 걷기)");

            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }

        /// <summary>T51 ① — 특전 «처치 시 대시»(p_killDash): 킬 뒤에도 공격 모션이 끝날 때까지 서 있다가 그다음 ×DashMul 로 걷는다(«멈춤 없이 바로 출발» 취소 · 주인 정정).</summary>
        [UnityTest]
        public IEnumerator KillDashStartsAfterAttackAnimThenWalksAtDashSpeed()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            PerkDef dash = null; foreach (var p in G.PK.Perks) if (p.Id == "p_killDash") dash = p;
            Assert.IsNotNull(dash, "perks.json 에 p_killDash 가 있어야 한다");
            G.PickPerk(dash); Assert.IsTrue(G.P.Has("p_killDash"), "대시 특전 보유");
            Assert.Greater(G.C.DashMul, 1, "dashMul > 1 (combat.json)");
            Arm(G);
            Time.timeScale = 3f;
            var st = new Stats();
            yield return Run(bs, world, G, 12f, st);
            Time.timeScale = 1f;
            Assert.Greater(st.HoldFrames, 0, "킬 연출 대기가 한 번은 있어야 한다");
            Assert.Greater(st.AnimHoldFrames, 0, "대시 특전이 있어도 킬 뒤 공격 모션 대기가 있어야 한다(T51 ① · 바로 출발 금지)");
            Assert.Greater(st.DashWalkFrames, 0, "킬 뒤 대시(P.Dash) 상태로 걷는 프레임이 있어야 한다");
            double avg = st.DashDt > 0 ? st.DashAdv / st.DashDt : 0, walk = G.C.PlayerSpeed * G.P.WalkMul;
            // 아래 하한은 «엔진이 실제로 틱을 돈 프레임» 만으로 잰다(T65) — `P.Dash` 는 킬 순간에 켜지므로(Battle.cs `p_killDash`)
            // 멈춤이 풀리는 프레임(엔진 틱 0 · 이동 0)과 «다음 적이 이미 StopDistance 안이라 대시가 한 틱 만에 꺼지는» 판까지 분모에 들어가
            // 실시간 평균은 대시 속도가 아니라 «대시 창 안에서 걸은 시간 비율» 이 된다. 상한은 반대로 분모가 큰 쪽(전체 DashDt)이 보수적이라 그대로 둔다.
            double moveAvg = st.DashMoveDt > 0 ? st.DashAdv / st.DashMoveDt : 0;
            Assert.Greater(moveAvg, walk * 1.5, "대시 구간 평균 속도(엔진이 틱을 돈 프레임만)가 원래 걷기 속도보다 확실히 빨라야 한다(×DashMul 표시 · 틱 양자화 감안 1.5배 이상)");
            Assert.LessOrEqual(avg, walk * G.C.DashMul + 1e-6, "대시 구간 평균 속도는 ×DashMul 을 넘지 않는다");
            Assert.AreEqual(0, st.DeathFxFrames, "사망 «펑» 이펙트 없음(T51 ②)");
            _log.AssertNoRed("대시 전투 진행");

            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }

        // ───────────────────────── T86 투사체 연출 ─────────────────────────
        /// <summary>«어느 적도 속하지 않는» 웨이브(엔진 적중·표시 상한 밖)로 순수 비행만 재는 시험용 투사체를 만든다 — 필드는 Battle.FireAxe·FireSpear·FireArrows 와 같다.</summary>
        static Projectile Ghost(BattleState G, ProjKind kind, BattleNode ghostWave, EnemyState ghostFoe, double x0, double reach)
        {
            if (kind == ProjKind.Spear || kind == ProjKind.Wave)
                return new Projectile { Kind = kind, X = x0, StartX = x0, Ratio = G.C.RSpear, Spd = kind == ProjKind.Spear ? G.C.SpearSpeed : EngineConst.WaveSpeed, MaxX = x0 + reach, Hit = new HashSet<EnemyState>(), Pierce = 8, Node = ghostWave };
            return new Projectile { Kind = kind, X = x0, StartX = x0, Target = ghostFoe, TargetX0 = ghostFoe.WorldX, Ratio = kind == ProjKind.Axe ? G.C.RAxe : G.C.RArrow, Spd = kind == ProjKind.Axe ? G.C.AxeSpeed : EngineConst.ArrowSpeed };
        }

        /// <summary>
        /// T86 ⓐⓑⓒ — 주인 2026-09-07: «도끼랑 창같은거 바로 안날라간다» · «창이 누워서 일자로 가야 하는데 비스듬한 각으로 간다» · «도끼 회전 너무 빠름 — 1초에 1바퀴».
        /// 킬 연출로 <b>엔진 틱이 보류된(T50 HoldEngine)</b> 순간에 «처치 시» 특전과 같은 도끼·창·화살을 쏘고 —
        /// ⓐ 엔진 x 가 멎어 있는 프레임에도 표시 x 가 <b>엔진과 같은 px/s</b> 로 전진하는가(정규화 t·고정 duration 금지) ⓑ 창 각 ≈ 0° ⓒ 도끼 회전 = 초당 1바퀴 를 잰다.
        /// </summary>
        [UnityTest]
        public IEnumerator ProjectilesFlyRightAwayWhileKillHoldsEngineAndSpearIsFlatAndAxeSpinsOncePerSecond()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            Arm(G);
            Time.timeScale = 3f;   // 첫 킬(= 엔진 보류)까지 걷는 시간을 줄인다
            float t0 = Time.realtimeSinceStartup;
            while (!world.HoldEngine && Time.realtimeSinceStartup - t0 < 30f && !G.Over && !_app.Overlay.IsOpen) yield return null;
            Time.timeScale = 1f;
            Assert.IsTrue(world.HoldEngine, "킬 연출로 엔진이 보류되는 순간이 있어야 시험이 성립한다(T50)");

            var ghostWave = new BattleNode();
            var ghostFoe = new EnemyState { Hp = 1e9, MaxHp = 1e9, WorldX = G.P.WorldX + 4000, Wave = ghostWave };
            double x0 = G.P.WorldX + EngineConst.ProjSpawnDx;
            var axe = Ghost(G, ProjKind.Axe, ghostWave, ghostFoe, x0, 0);
            var spear = Ghost(G, ProjKind.Spear, ghostWave, ghostFoe, x0, 4000);
            var arrow = Ghost(G, ProjKind.Arrow, ghostWave, ghostFoe, x0, 0);
            G.Projs.Add(axe); G.Projs.Add(spear); G.Projs.Add(arrow);
            yield return null;   // 첫 Sync 가 오브젝트를 만든다
            var axeGo = world.ProjGo(axe); var spearGo = world.ProjGo(spear); var arrowGo = world.ProjGo(arrow);
            Assert.IsNotNull(axeGo, "도끼 오브젝트"); Assert.IsNotNull(spearGo, "창 오브젝트"); Assert.IsNotNull(arrowGo, "화살 오브젝트");

            int holdAdvFrames = 0; float world_t = 0, spin = 0;
            double prevShown = world.ProjShownX(axe); float prevAng = axeGo.transform.eulerAngles.z;
            float t1 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t1 < 0.7f && !G.Over && !_app.Overlay.IsOpen)
            {
                bool heldBefore = world.HoldEngine; double engineBefore = axe.X;
                yield return null;
                world_t += Time.deltaTime * bs.Speed;
                double shown = world.ProjShownX(axe); float ang = axeGo.transform.eulerAngles.z;
                // ⓐ 엔진이 보류된 프레임(앞뒤 프레임 모두 보류 · 엔진 x 그대로)에도 화면은 전진한다
                if (heldBefore && world.HoldEngine && Math.Abs(axe.X - engineBefore) < 1e-9 && shown > prevShown + 1e-9) holdAdvFrames++;
                spin += Mathf.Abs(Mathf.DeltaAngle(prevAng, ang));
                prevShown = shown; prevAng = ang;
            }
            Assert.Greater(holdAdvFrames, 0, "엔진 틱이 보류된 프레임에도 도끼가 전진해야 한다 — 주인 지적 «발사되고 바로 안 날아간다»(T86 ⓐ)");
            // ⓐ «거리당 속도» — 표시 전진 = 속도 × 흐른 시간 (고정 duration·정규화 t 금지)
            double flownAxe = world.ProjShownX(axe) - axe.StartX, flownSpear = world.ProjShownX(spear) - spear.StartX;
            Assert.AreEqual(G.C.AxeSpeed * world_t, flownAxe, G.C.AxeSpeed * world_t * 0.25, "도끼 표시 전진 = axeSpeed × 흐른 시간(±25%)");
            Assert.AreEqual(G.C.SpearSpeed * world_t, flownSpear, G.C.SpearSpeed * world_t * 0.25, "창 표시 전진 = spearSpeed × 흐른 시간(±25%)");
            Assert.Greater(flownSpear, flownAxe, "같은 시간이면 빠른 창(520)이 도끼(430)보다 멀리 간다 — 속도는 거리당이다");
            // ⓑ 창도 화살도 수평 — 화살의 −35° 는 T179 에서 0 이 됐다(주인 «화살 각도가 완벽히 누워 있어야 하는데 비스듬하다»).
            // 값은 리터럴로 박지 않고 코드의 상수를 그대로 본다 — 옛 −35° 를 박아 둔 이 줄이 T179 커밋에서 빨개졌던 자리다(결정 425 와 같은 갈래).
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(spearGo.transform.eulerAngles.z, 0f)), 2f, "창은 수평(0°±2°)으로 누워 날아간다(T86 ⓑ · 주인 «비스듬한 각» 지적)");
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(arrowGo.transform.eulerAngles.z, BattleWorld.ArrowAngle)), 2f, "플레이어 화살도 수평(T179 · 화살은 포물선을 안 그리므로 가는 방향과 그림이 같아야 한다)");
            // ⓒ 도끼 회전 = 초당 1바퀴(360°/s) — 비행 거리 비율이 아니라 «날아간 시간»에서 나온다
            Assert.AreEqual(360f * world_t, spin, 360f * world_t * 0.2f + 6f, "도끼 회전은 초당 1바퀴여야 한다(±20% · T86 ⓒ · 주인 «너무 빠름»)");
            float expect = -360f * (float)((world.ProjShownX(axe) - axe.StartX) / axe.Spd);
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(axeGo.transform.eulerAngles.z, expect)), 6f, "도끼 각 = −360° × (날아간 거리 / 속도) (반시계 · 방향 종전)");
            _log.AssertNoRed("투사체 비행(킬 연출 중)");

            G.Projs.Remove(axe); G.Projs.Remove(spear); G.Projs.Remove(arrow); yield return Frames(2);
            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }

        /// <summary>
        /// T86 ⓐ 4-1(주인 2026-09-07 보탬) — «투사체는 거리당 속도(px/s)다 · 시작~도착 시간 고정 금지».
        /// 같은 순간에 사거리 300px·900px 짜리 창을 쏘아 <b>엔진이 실제로 시간을 흘린 만큼</b>(보류 프레임 제외) 비행 시간을 재고 그 비가 거리 비(3배)와 같은지 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator ProjectileFlightTimeGrowsWithDistanceNotFixedDuration()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            Arm(G);
            yield return Frames(2);
            var ghostWave = new BattleNode();
            var ghostFoe = new EnemyState { Hp = 1e9, MaxHp = 1e9, WorldX = G.P.WorldX + 4000, Wave = ghostWave };
            double x0 = G.P.WorldX + EngineConst.ProjSpawnDx;
            var near = Ghost(G, ProjKind.Spear, ghostWave, ghostFoe, x0, 300);
            var far = Ghost(G, ProjKind.Spear, ghostWave, ghostFoe, x0, 900);
            G.Projs.Add(near); G.Projs.Add(far);
            float engine_t = 0; double tNear = -1, tFar = -1; float t0 = Time.realtimeSinceStartup;
            while ((tNear < 0 || tFar < 0) && Time.realtimeSinceStartup - t0 < 30f && !G.Over && !_app.Overlay.IsOpen)
            {
                bool ran = !world.HoldEngine;
                yield return null;
                if (ran) engine_t += Time.deltaTime * bs.Speed;   // 엔진이 보류된 프레임(킬 연출)은 엔진 시간이 흐르지 않는다
                if (tNear < 0 && !G.Projs.Contains(near)) tNear = engine_t;
                if (tFar < 0 && !G.Projs.Contains(far)) tFar = engine_t;
            }
            Assert.Greater(tNear, 0, "300px 창이 사거리 끝에 닿아 사라져야 한다");
            Assert.Greater(tFar, 0, "900px 창이 사거리 끝에 닿아 사라져야 한다");
            Assert.AreEqual(300.0 / G.C.SpearSpeed, tNear, 0.12, "300px 비행 시간 ≈ 300 / spearSpeed");
            Assert.AreEqual(3.0, tFar / tNear, 0.3, "거리가 3배면 비행 시간도 3배 — «시간 고정» 이면 1배가 나온다(T86 4-1)");
            _log.AssertNoRed("거리당 속도");

            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }

        /// <summary>
        /// T108(주인 2026-09-07 «창이 스무스하게 나가지 않고 멈춰 있는 현상 · 창 발사하면 그냥 멈추지 말고 쭉 지나가면서 다 데미지 주고 지나가야 함 · 쩄든 뭐든 멈추면 안 됨»).
        /// T86 이 «킬 연출 중에도 간다» 를 넣었는데도 주인 눈에 멈춰 보인 까닭은 <b>관통형의 «다음에 꿸 적» 걸림쇠</b>였다 —
        /// 엔진이 보류되면 그 걸림쇠도 같이 멎어서 창이 적 앞에 붙어 선다(결정 252). 여기서 재는 것:
        /// ⓐ 적을 <b>줄줄이 세워 둔</b> 채 엔진이 보류돼도 창의 표시 x 가 <b>한 프레임도 안 멈추고</b> 간다
        /// ⓑ <b>좌표 점프가 없다</b>(프레임 간 이동량 ≤ 속도 × dt × 배속 × <see cref="BattleWorld.ProjCatchUpMul"/> · T108 2항 «스냅 금지»)
        /// ⓒ 창은 첫 적을 <b>지나쳐</b> 간다(거기서 서지 않는다).
        /// 엔진의 관통 판정 자체는 이미 aaaw <c>sim.js</c> 와 같다(<c>fireSpear</c> 의 <c>pierce:SPEAR_PIERCE</c> = 8) — 그래서 여기서는 그림만 본다.
        /// </summary>
        [UnityTest]
        public IEnumerator SpearNeverStallsAndFliesThroughEnemiesWithoutSnapping()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            Arm(G);
            Time.timeScale = 3f;
            float t0 = Time.realtimeSinceStartup;
            while (!world.HoldEngine && Time.realtimeSinceStartup - t0 < 30f && !G.Over && !_app.Overlay.IsOpen) yield return null;
            Time.timeScale = 1f;
            Assert.IsTrue(world.HoldEngine, "킬 연출로 엔진이 보류되는 순간이 있어야 시험이 성립한다(T50)");

            // 살아 있는 적이 «창의 길 위에» 있는 상태로 만든다 — 예전 걸림쇠라면 그 적 앞에서 바로 섰다.
            var alive = G.AliveList();
            Assert.Greater(alive.Count, 0, "전투 중이라 살아 있는 적이 있어야 한다");
            EnemyState ahead = null;
            foreach (var e in alive) if (e.WorldX > G.P.WorldX && (ahead == null || e.WorldX < ahead.WorldX)) ahead = e;
            Assert.IsNotNull(ahead, "앞에 있는 적");
            double x0 = G.P.WorldX + EngineConst.ProjSpawnDx;
            // 그 적을 한참 지나치는 사거리
            double reach = (ahead.WorldX - x0) + 1200;
            var spear = Ghost(G, ProjKind.Spear, ahead.Wave, ahead, x0, reach);
            G.Projs.Add(spear);
            yield return null;

            // ⓒ 걸림쇠가 사거리 끝뿐인가 — 앞에 살아 있는 적이 있어도 그 적 자리로 깎이면 안 된다(결정 252 · 이것이 «멈춰 있는 현상» 의 원인이었다)
            Assert.Greater(ahead.WorldX, x0, "적이 창보다 앞에 있어야 시험이 성립한다");
            Assert.Less(ahead.WorldX, spear.MaxX, "적이 창의 사거리 안에 있어야 시험이 성립한다");
            // T171 — T108 은 «앞의 적» 걸림쇠를 없앴고 걸림쇠를 «사거리 끝» 으로 두었는데, 그 사거리 끝이 남은 멈춤의 원인이었다:
            // 사거리 끝에서 창을 지우는 것은 «엔진» 인데 킬 연출 동안 엔진 틱이 보류되므로, 표시가 얼어붙은 상한에 눌려 화면 끝에 붙어 선다.
            // 이제 관통형에는 상한이 없다(주인 «창이 여전히 화면 끝에서 멈추네 가끔씩»).
            Assert.IsTrue(double.IsPositiveInfinity(world.ProjLimit(spear)),
                "관통형(창·검기)에는 표시 걸림쇠가 없어야 한다 — 사거리 끝에 걸어 두면 엔진이 보류된 동안 그 자리에 붙어 선다(T171 · 주인 «화면 끝에서 멈추네»)");

            int frames = 0, stalled = 0; double prev = world.ProjShownX(spear); double worstStep = 0;
            float t1 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t1 < 0.8f && !G.Over && !_app.Overlay.IsOpen && G.Projs.Contains(spear))
            {
                float dtBefore = Time.deltaTime;
                // 팝업·일시정지·판 종료 프레임은 «게임 전체가 멈춘» 것이라 세지 않는다(지시서 T108 1항의 유일한 예외)
                bool running = world.EngineRunning;
                yield return null;
                double shown = world.ProjShownX(spear); double step = shown - prev;
                if (!running || !world.EngineRunning) { prev = shown; continue; }
                frames++;
                if (step <= 1e-9) stalled++;
                // 한 프레임에 «속도 × dt × 배속 × 배율» 보다 더 갔으면 그것이 스냅(튐)이다.
                // dt 는 «둘 중 큰 것» 으로 잰다 — 코루틴이 App.Update 보다 먼저 깨는지 나중에 깨는지는 스크립트 실행 순서에 달렸고(둘 다 Update 단계),
                // 그래서 이 걸음을 만든 Sync 의 dt 가 yield «앞» 프레임의 것일 수도 «뒤» 프레임의 것일 수도 있다. 한쪽만 쓰면 프레임 시간이 튄 순간
                // (CI headless 의 GC·로드)에 멀쩡한 걸음이 «스냅» 으로 잡힌다 — CI #187 의 13.1px 이 그것이었다(T108 확인 회차 · 워커 D).
                double dt = Math.Max(dtBefore, Time.deltaTime);
                double cap = spear.Spd * Math.Max(dt, 1e-4) * Math.Max(1, bs.Speed) * BattleWorld.ProjCatchUpMul + 1.0;
                if (step > cap) worstStep = Math.Max(worstStep, step - cap);
                prev = shown;
            }
            Assert.Greater(frames, 10, "재는 프레임이 있어야 한다");
            Assert.AreEqual(0, stalled, "창은 어떤 상태에서도 멈추면 안 된다 — 멈춘 프레임 " + stalled + "/" + frames + " (T108 1항 · 주인 «쩄든 뭐든 멈추면 안 됨»)");
            Assert.AreEqual(0.0, worstStep, 1e-6, "표시 좌표가 튀었다(스냅) — 초과 이동량 " + worstStep.ToString("0.0") + "px (T108 2항)");
            _log.AssertNoRed("창 관통 비행");

            // ───── T171 — «상한이 엔진과 함께 얼어붙는» 두 자리를 계약으로 못 박는다 ─────
            // 시간을 재는 loop 로 잡지 않는다: 표시가 상한을 넘어설 수 있는 창(窓)은 «엔진이 보류된 동안» 뿐인데 그 길이는 킬 연출에 달렸고,
            // 그걸 기다리는 단언은 CI 에서 간헐로 빨개진다(T134 가 딱 그런 자를 25분짜리 빌드 뒤에서 터뜨렸다). 위 0.8초 loop 이 «멈춤 0» 은 이미 재고 있으니
            // 여기서는 **고침 그 자체 = 상한이 없다** 를 결정적으로 잰다. 상한이 무한이면 `shown > lim` 가지가 아예 닿지 않아 «눌려 서는» 일이 성립하지 않는다.
            {
                // ⓑ 표적이 먼저 죽은 유도형(도끼) — 예전 상한은 «pr.X(엔진 x)» 라 보류 중엔 안 움직여 도끼가 공중에 섰다(주인 T108 1-b «도끼가 여전히 멈춘다»).
                var axe = Ghost(G, ProjKind.Axe, ahead.Wave, ahead, G.P.WorldX + EngineConst.ProjSpawnDx, 0);
                Assert.IsFalse(double.IsPositiveInfinity(world.ProjLimit(axe)),
                    "표적이 살아 있는 유도형은 «맞는 자리» 에 서야 한다 — 이 상한까지 풀면 도끼가 표적을 지나쳐 날아간 뒤에 맞는다(T171 이 건드리지 않는 자리)");
                axe.Target = null;   // 표적이 이미 사라진 상태(엔진이 아직 못 지운 프레임)
                Assert.IsTrue(double.IsPositiveInfinity(world.ProjLimit(axe)),
                    "표적이 사라진 유도형은 «엔진 x» 에 묶이면 안 된다 — 그러면 킬 연출 동안 공중에 선다(T171 3항)");
                // 누수 0(T171 4항 ⓓ) — 표시 층이 만든 그림·좌표는 엔진 목록에서 빠지면 같이 사라져야 한다.
                G.Projs.Add(axe); yield return Frames(2);
                Assert.IsNotNull(world.ProjGo(axe), "표시 층이 도끼 그림을 세워야 시험이 성립한다");
                G.Projs.Remove(axe); yield return Frames(2);
                Assert.IsNull(world.ProjGo(axe), "사라진 도끼의 그림이 남으면 안 된다(누수 0 · T171 4항 ⓓ)");
            }
            _log.AssertNoRed("T171 표적 잃은 도끼");

            G.Projs.Remove(spear); yield return Frames(2);
            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }

        /// <summary>
        /// T179 — 주인 2026-09-07: «적의 화살들이 각도가 완벽히 누워 있어야 하는데 비스듬하다» · «화살이 맞아야 하는데 멈출 때가 있음».
        /// ⓐ 적 화살 그림의 z 회전이 <see cref="BattleWorld.EnemyArrowAngle"/>(수평) 이다 — 종전 <c>Euler(0,0,200f)</c> 는 «180° + 20°» 라 20° 기울어 보였다.
        /// ⓑ 킬 연출로 <b>엔진 틱이 보류된</b> 프레임에도 표시 x 가 한 번도 안 멈추고 왼쪽으로 간다(투사체 T86·T108 과 같은 계약을 적 화살에도).
        /// ⓒ 좌표 점프가 없다(프레임 이동량 ≤ 속도 × dt × 배속 × <see cref="BattleWorld.ProjCatchUpMul"/>).
        /// ⓓ 엔진이 지우면 그림도 사라진다(누수 0).
        /// </summary>
        [UnityTest]
        public IEnumerator EnemyArrowsLieFlatAndKeepFlyingWhileKillHoldsTheEngine()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            Arm(G);
            Time.timeScale = 3f;
            float t0 = Time.realtimeSinceStartup;
            while (!world.HoldEngine && Time.realtimeSinceStartup - t0 < 30f && !G.Over && !_app.Overlay.IsOpen) yield return null;
            Time.timeScale = 1f;
            Assert.IsTrue(world.HoldEngine, "킬 연출로 엔진이 보류되는 순간이 있어야 시험이 성립한다(T50)");

            // 피해 0 짜리 화살을 한참 오른쪽에 둔다 — 엔진의 명중·소멸선(P.WorldX + ArrowHitDx/ArrowCullDx) 밖이라 시험 동안 살아 있다.
            var arrow = new EnemyArrow { X = G.P.WorldX + 3000, Dmg = 0, Friendly = false };
            G.Arrows.Add(arrow);
            yield return null;   // 첫 Sync 가 그림을 만든다
            Assert.AreEqual(1, world.ArrowViewCount, "적 화살 그림이 하나 서야 한다");

            // ⓐ 각 — flipX 로 좌우만 뒤집고 회전은 0 이다(스프라이트 본디 기울기 PNG 실측 0.14°)
            float ang = world.ArrowViewAngle(arrow);
            Assert.IsFalse(float.IsNaN(ang), "적 화살 그림의 각");
            float off = Mathf.Abs(Mathf.DeltaAngle(ang, BattleWorld.EnemyArrowAngle));
            Assert.LessOrEqual(off, 1f, "적 화살은 수평 — 각이 " + ang.ToString("0.0") + "° 라 " + off.ToString("0.0") + "° 기울었다(T179 ⓐ)");

            // ⓑⓒ 보류 중에도 가고, 점프하지 않는다
            int heldFrames = 0, heldAdvanced = 0;
            double prev = world.ArrowShownX(arrow);
            double spd = _app.Data.Combat.EnemyArrowSpeed;
            float t1 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t1 < 0.7f && !G.Over && !_app.Overlay.IsOpen && world.ArrowViewCount > 0)
            {
                bool heldBefore = world.HoldEngine;
                yield return null;
                double now = world.ArrowShownX(arrow);
                double moved = prev - now;                                   // 왼쪽으로 간 거리(양수여야 한다)
                double cap = spd * Time.deltaTime * Mathf.Max(1, world.Speed) * BattleWorld.ProjCatchUpMul + 1e-6;
                Assert.LessOrEqual(moved, cap, "적 화살 좌표가 튀면 안 된다(스냅 0) — 이 프레임 " + moved.ToString("0.0") + " > 상한 " + cap.ToString("0.0"));
                if (heldBefore) { heldFrames++; if (moved > 1e-6) heldAdvanced++; }
                prev = now;
            }
            Assert.Greater(heldFrames, 0, "엔진이 보류된 프레임이 있어야 ⓑ 를 잴 수 있다");
            Assert.AreEqual(heldFrames, heldAdvanced, "엔진 보류 중에도 적 화살은 한 프레임도 안 멈춘다(T179 ⓑ · 멈춘 프레임 " + (heldFrames - heldAdvanced) + ")");
            _log.AssertNoRed("T179 적 화살");

            // ⓓ 엔진이 지우면 그림도 간다
            G.Arrows.Remove(arrow); yield return Frames(2);
            Assert.AreEqual(0, world.ArrowViewCount, "사라진 적 화살의 그림이 남으면 안 된다(누수 0 · T179 ⓓ)");

            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }
    }
}
