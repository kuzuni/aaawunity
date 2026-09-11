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
    /// T457 — <b>레벨업이 킬 틱에서 곧바로 창이 되지 않는다</b>(주인 2026-09-12 «그 특전 팝업 뜨기 전에 전투나 이동은 바로 전까지 계속 됐어야 함» · T368 의 뒤).
    /// <para>
    /// 뿌리: 엔진은 킬 틱 안에서 경험치를 주고 같은 틱 끝에서 <c>!HoldLevelUp</c> 이면 곧바로 <c>OpenLevelUp()</c> 한다. 화면이 세우는 <c>HoldLevelUp</c> 은 앞 프레임의
    /// Busy·Absorbing 을 보므로 킬 «직전» 엔 거짓이었다 → 레벨업이 킬 틱에서 <c>Pending</c> 이 되고 화면 루프가 매 프레임 <c>_acc = 0; break</c> → 킬부터 흡수 끝까지 엔진이 얼어
    /// 걷기가 섰다. T368 의 자(<c>BattleScreenTellsTheEngineToHoldTheLevelUpWhileOrbsAreFlying</c>)는 «한 번이라도 켜졌는가» 만 재서 이 순서를 못 봤다.
    /// </para>
    /// <para>
    /// 재는 것(첫 킬이 레벨업이 되게 경험치를 문턱 한 칸 아래로 두고 · 한 방 킬): ⓐ <b>창은 킬 프레임 뒤에</b> 선다(같은 프레임이면 옛 꼴) ⓑ 그 사이에
    /// <b>줄에 든 채 창이 아닌</b> 프레임(<c>PendingLevelUps &gt; 0 · Pending == null</c>)이 있다 ⓒ <b>엔진 시간이 킬 뒤에도 흘렀다</b>(<c>G.T</c> 가 킬 프레임 값보다 크다 — 옛 꼴은 킬 틱에서 얼어 0)
    /// ⓓ 창이 결국 선다(붙잡기가 «영영» 이 아니다) ⓔ 빨간 줄 0.
    /// </para>
    /// ⚠ «걷는 프레임 수» 는 안 잰다 — 다음 적까지의 거리·배속·프레임 길이에 흔들린다(결정 877 ④). 킬 보류(T50 · 칼이 내려오는 동안 틱 보류)는 이 절이 손대지 않은 계약이다.
    /// </summary>
    public class LevelUpQueuePlayTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator 레벨업은_킬_틱에서_창이_되지_않고_줄에_들어_엔진이_계속_돈다()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            var G = bs.G; Assert.IsNotNull(G, "전투 상태"); var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            // 한 방 킬 · 첫 킬이 곧 레벨업(문턱 한 칸 아래) · 노드 팝업은 끈다(BattleWorldTests.Arm 과 같은 꼴 · 원인이 하나만 남게)
            G.P.Dmg = 1e6;
            G.P.Exp = G.D.Tune.ExpNeed(G.P.Level) - 1;
            foreach (var n in G.Nodes) if (n.Type == NodeType.Rest || n.Type == NodeType.Devil || n.Type == NodeType.Angel) n.Done = true;
            Assert.AreEqual(0, G.PendingLevelUps, "판이 서면 줄은 비어 있다(전제 · 원정 시작 특전이 아닌 1챕터)");

            int frame = 0, killFrame = -1, pendingFrame = -1; double tKill = -1, tPending = -1;
            bool queuedSeen = false, opened = false;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 30f && !G.Over && bs.World == world)
            {
                yield return null; frame++;
                if (killFrame < 0 && G.Kills >= 1) { killFrame = frame; tKill = G.T; }
                if (killFrame >= 0 && G.PendingLevelUps > 0 && G.Pending == null) queuedSeen = true;
                if (pendingFrame < 0 && G.Pending != null) { pendingFrame = frame; tPending = G.T; }
                if (_app.Overlay.IsOpen) { opened = true; break; }
            }

            Assert.GreaterOrEqual(killFrame, 0, "30초 안에 첫 킬이 나야 한다 — 안 나면 이 자는 아무것도 못 잰 것이다(판 자체를 보라)");
            Assert.IsTrue(opened, "첫 킬의 레벨업 창이 결국 서야 한다 — 붙잡기는 «영영» 이 아니다(AbsorbMaxWaitSec 상한)");
            Assert.GreaterOrEqual(pendingFrame, 0, "Pending 이 선 프레임을 봤어야 한다");
            // ⓐ 옛 꼴: 킬 틱 끝에서 곧바로 OpenLevelUp → 같은 프레임에 Pending. 고친 꼴: 줄에 들었다가 흡수·연출이 끝난 뒤의 틱에서 선다.
            Assert.Greater(pendingFrame, killFrame, "레벨업 창은 킬 프레임 «뒤» 에 서야 한다 — 같은 프레임이면 킬 틱에서 그대로 창이 된 것(T457 의 멈춤)");
            // ⓑ 줄에 든 채 창이 아닌 프레임 — 이것이 T368 이 뜻한 «아직 Pending 을 안 세운다» 다
            Assert.IsTrue(queuedSeen, "킬 뒤에 «PendingLevelUps > 0 인데 Pending == null» 인 프레임이 있어야 한다(줄에 들어 있다) — 없으면 킬 틱에서 열린 것");
            // ⓒ 엔진 시간이 킬 뒤에도 흘렀다 — 옛 꼴은 Pending 이 선 채라 킬 틱 이후 틱이 0 이다
            Assert.Greater(tPending, tKill, "킬 프레임과 창이 선 프레임 사이에 엔진 시간(G.T)이 흘러야 한다 — 같으면 킬부터 창까지 엔진이 얼어 있던 것(주인이 본 멈춤)");
            _log.AssertNoRed("T457 레벨업 줄");
            yield return Shutdown();
        }
    }
}
