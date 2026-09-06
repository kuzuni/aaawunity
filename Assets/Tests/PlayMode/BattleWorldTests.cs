using System;
using System.Collections;
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
            public double DashAdv, DashDt;
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
                if (P.Dash && !hold) { st.DashDt += Time.deltaTime; st.DashAdv += adv; if (advanced) st.DashWalkFrames++; }
                // T51 ② — 사망 «펑» 이펙트 없음
                if (GameObject.Find(DeathFxName) != null) st.DeathFxFrames++;
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
            Assert.Greater(avg, walk * 1.5, "대시 구간 평균 속도가 원래 걷기 속도보다 확실히 빨라야 한다(×DashMul 표시 · 틱 양자화 감안 1.5배 이상)");
            Assert.LessOrEqual(avg, walk * G.C.DashMul + 1e-6, "대시 구간 평균 속도는 ×DashMul 을 넘지 않는다");
            Assert.AreEqual(0, st.DeathFxFrames, "사망 «펑» 이펙트 없음(T51 ②)");
            _log.AssertNoRed("대시 전투 진행");

            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }
    }
}
