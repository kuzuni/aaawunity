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
    /// 수정 = 표시 원점 <see cref="BattleWorld.ShownPX"/> 가 사망 연출이 안 나온 적(<see cref="BattleWorld.KillPending"/>)이 있는 동안 멈춘다.
    /// 실제 씬에서 한 방에 죽는 전투를 돌리며 매 프레임 «출발(원점 전진) ∧ 킬 연출 대기» 가 한 번도 없음을 단언하고, 대기가 풀리면 엔진 x 를 따라잡는지 본다.
    /// 빨간 줄 0 은 <see cref="PlayLog"/>(T11 규약 · LogAssert.NoUnexpectedReceived 금지).
    /// </summary>
    public class BattleWorldTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }

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
            // 한 방에 죽인다(킬 → 다음 틱 출발이 웨이브마다 일어난다) · 레벨업·노드 팝업으로 엔진이 멈추지 않게
            G.P.Dmg = 1e6; G.P.Exp = int.MinValue / 2;
            foreach (var n in G.Nodes) if (n.Type == NodeType.Rest || n.Type == NodeType.Devil || n.Type == NodeType.Angel) n.Done = true;
            Time.timeScale = 3f;   // 첫 웨이브까지 걷는 시간을 줄인다(엔진 틱은 dt 로 돈다)
            int holdFrames = 0, walkFrames = 0, catchUpFrames = 0; double prev = world.ShownPX, maxGap = 0;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 12f && !G.Over && !_app.Overlay.IsOpen && bs.World == world)
            {
                yield return null;   // App.Update(틱 → Sync) 뒤에 깨어난다
                double now = world.ShownPX; bool advanced = now > prev + 1e-9; prev = now;
                bool pending = world.KillPending;
                if (pending) holdFrames++;
                if (advanced) walkFrames++;
                Assert.IsFalse(advanced && pending, "사망 연출이 아직 안 나온 적이 있는데 화면이 출발했다(주인 지적 재현)");
                Assert.LessOrEqual(now, G.P.WorldX + 1e-6, "표시 원점이 엔진 x 를 앞지르지 않는다");
                double gap = G.P.WorldX - now; maxGap = Math.Max(maxGap, gap);
                if (!pending && gap > Tick1) catchUpFrames++;
            }
            Time.timeScale = 1f;
            Assert.Greater(holdFrames, 0, "킬 연출 대기(칼이 내려오기 전)가 한 번은 있어야 시험이 성립한다");
            Assert.Greater(walkFrames, 0, "출발(원점 전진)이 있어야 한다");
            Assert.Greater(catchUpFrames, 0, "대기가 풀린 뒤 따라잡기 구간이 있어야 한다");
            Assert.Greater(maxGap, Tick1 * 4, "대기 중 엔진 x 가 앞서 나간다(엔진 좌표 불변 · 웨이브 안 적 간격 44px 이상)");
            // 대기가 없는 채로 잠시 → 격차가 닫혀 있다(따라잡기 = 걷기 2배 · 격차는 한 번의 대기(≤ 공격 간격) 안에 닫힌다)
            float t1 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t1 < 3f && (world.KillPending || G.P.WorldX - world.ShownPX > Tick1) && bs.World == world && !_app.Overlay.IsOpen) yield return null;
            if (bs.World == world && !_app.Overlay.IsOpen) Assert.LessOrEqual(G.P.WorldX - world.ShownPX, Tick1, "대기가 풀리면 표시 원점이 엔진 x 를 따라잡는다");
            _log.AssertNoRed("전투 진행(킬 → 출발 · 따라잡기)");

            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }
    }
}
