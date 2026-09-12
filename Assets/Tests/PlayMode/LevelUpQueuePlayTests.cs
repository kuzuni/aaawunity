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
            // ⚑ T496 ⓐ — «Pending 이 선 뒤 창이 열릴 때까지» 를 따로 센다(등재 글 ⑦ⓒ 가 «고치기 전에 그 수부터» 로 박아 둔 그 수).
            //   그 구간은 BattleScreen:489 의 `if (G.Pending != null) { _acc = 0; break; }` 때문에 **엔진이 얼어 있는** 구간이다.
            //   프레임 수와 그동안 흐른 엔진 시간(G.T) 둘 다 잰다 — «몇 프레임» 만으로는 «얼었는가» 를 못 가른다.
            int openFrame = -1; double tOpen = -1; float rtPending = -1f, rtOpen = -1f;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 30f && !G.Over && bs.World == world)
            {
                yield return null; frame++;
                if (killFrame < 0 && G.Kills >= 1) { killFrame = frame; tKill = G.T; }
                if (killFrame >= 0 && G.PendingLevelUps > 0 && G.Pending == null) queuedSeen = true;
                if (pendingFrame < 0 && G.Pending != null) { pendingFrame = frame; tPending = G.T; rtPending = Time.realtimeSinceStartup; }
                if (_app.Overlay.IsOpen) { opened = true; openFrame = frame; tOpen = G.T; rtOpen = Time.realtimeSinceStartup; break; }
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
            // ⚑⚑ T496 ⓐ — **재기만 한다. 판정은 안 한다**(결정 493 · 등재 글 ⑦ⓒ).
            //   등재 글 ③ 이 소스로 가른 것: 상한(`AbsorbMaxWaitSec`)이 넘으면 `Pending` 은 서는데 `OpenPending` 의
            //   첫 줄 `if (Absorbing) return;` 이 여는 것을 도로 막는다 ⇒ 그 사이 엔진이 언다. 그 «언 구간» 의 크기가
            //   여기서 나온다: `holdFrames`(프레임) · `holdT`(그동안 흐른 엔진 시간 — 얼었으면 0 언저리).
            //   ⚠ **문턱을 안 박는다** — 몇 프레임이면 «주인 눈에 보이는가» 를 아직 아무도 모른다(등재 글 ⑦ⓑ).
            //     수를 먼저 쌓고, 켤지는 그 수를 읽는 사람이 정한다(`HighLevelTextGate` 가 지나온 길).
            //   ⚠ **찍기만 하면 아무도 못 읽는다**(결정 636 · T455 3회차가 값을 치른 자리) — 유니티 테스트의
            //     `Debug.Log` 는 결과 XML 안에만 남고 그 아티팩트는 프록시가 막는다. 그래서 `PlayShot.Dirs()` 에 쓴다:
            //     CI 가 `screens` 브랜치로 같이 올리므로 `git show origin/screens:t496_hold.json` 한 번이면 읽힌다.
            int holdFrames = (openFrame >= 0 && pendingFrame >= 0) ? openFrame - pendingFrame : -1;
            double holdT = (tOpen >= 0 && tPending >= 0) ? tOpen - tPending : -1;
            WriteHoldReport(killFrame, pendingFrame, openFrame, tKill, tPending, tOpen, holdFrames, holdT, (rtOpen >= 0f && rtPending >= 0f) ? rtOpen - rtPending : -1f);

            // ⚑ 공허 방지 — 이 수가 «0 이다» 와 «못 쟀다» 를 가른다(T455 2회차가 «가드가 스스로 공허했다» 로 치른 값).
            Assert.GreaterOrEqual(openFrame, 0, "창이 선 프레임을 못 잡았다 — 그러면 위에 쓴 수는 «잰 것» 이 아니다");
            Assert.GreaterOrEqual(holdFrames, 0, "Pending 이 선 프레임이 창이 선 프레임보다 뒤다 — 셈이 뒤집혔다(수를 믿지 마라)");

            // ⛑⛑ **T496 2회차 — 이제 막는다.** 1회차는 «수를 아직 아무도 모른다» 라 판정을 안 박았다. 그 수가 나왔다:
            //   런 1147 에서 `hold 127프레임 · engineTime 0` = **≈2.3초 동안 엔진이 얼어 있었다**(결정 1379 · 검수 Q 가 fps 를 되짚었다).
            //   고친 뒤에는 상한이 넘는 순간 `FinishAbsorb()` 로 적립하고 바로 열리므로 그 구간이 **한두 프레임**이다.
            //   ⚑ 문턱은 **프레임이 아니라 실제 시간**으로 잰다 — 프레임 수는 러너 fps 에 매여 폰으로 이식이 안 되고(결정 1379 ⑤),
            //     «엔진이 얼었다» 는 사람이 초로 느끼는 것이다. 0.5초는 잰 두 값(2.3초 ↔ 한 프레임 ≈0.02초) **사이의 넓은 자리**다 —
            //     느린 러너에서도 0.5초면 스물몇 프레임이라 우연히 안 걸린다(결정 930 — 모르는 값으로 문턱을 박지 않는다. 이건 아는 값이다).
            float holdReal = (rtOpen >= 0f && rtPending >= 0f) ? rtOpen - rtPending : -1f;
            Assert.GreaterOrEqual(holdReal, 0f, "실제 시간을 못 쟀다 — 그러면 아래 판정은 공허하다");
            Assert.Less(holdReal, 0.5f,
                "`Pending` 이 선 뒤 창이 열릴 때까지 " + holdReal.ToString("0.###") + "초 걸렸다(" + holdFrames + "프레임) — "
                + "그 동안 엔진은 매 프레임 `_acc = 0; break` 로 **얼어 있다**(주인이 두 번 말한 그 멈춤 · T368·T457). "
                + "T496 이 고친 자리가 되돌아왔는지 보라: 상한(`AbsorbMaxWaitSec`)이 넘으면 `OpenNow(showNow)` 가 "
                + "`FinishAbsorb()` 로 남은 값을 적립해 `Absorbing` 을 거짓으로 만든 뒤 열어야 한다. 잰 이력: 고치기 전 2.3초 · 고친 뒤 한두 프레임.");

            _log.AssertNoRed("T457 레벨업 줄");
            yield return Shutdown();
        }

        /// <summary>
        /// T496 ⓐ — 잰 수를 <b>워커가 읽을 수 있는 자리</b>에 쓴다(<c>ui-screens/t496_hold.json</c> → `screens` 브랜치).
        /// <para>⚑ <see cref="KkomaKnight.Tests.Play.HighLevelTextGateTests"/> 가 같은 까닭으로 먼저 밟은 길이다(결정 636).</para>
        /// </summary>
        static void WriteHoldReport(int killFrame, int pendingFrame, int openFrame,
                                    double tKill, double tPending, double tOpen,
                                    int holdFrames, double holdT, float holdReal)
        {
            string json =
                "{\"_meta\":{\"task\":\"T496\",\"what\":\"Pending 이 선 뒤 창이 열릴 때까지 — 그 사이 엔진은 언다(BattleScreen:489)\"}"
              + ",\"frames\":{\"kill\":" + killFrame + ",\"pending\":" + pendingFrame + ",\"open\":" + openFrame
              + ",\"hold\":" + holdFrames + "}"
              + ",\"engineTime\":{\"kill\":" + tKill.ToString("0.####") + ",\"pending\":" + tPending.ToString("0.####")
              + ",\"open\":" + tOpen.ToString("0.####") + ",\"hold\":" + holdT.ToString("0.####") + "}"
              + ",\"holdRealSec\":" + holdReal.ToString("0.####")
              + ",\"note\":\"holdT 가 0 이면 그 구간 동안 엔진이 얼어 있었다(틱 0). holdRealSec 이 사람이 느끼는 멈춤이고 0.5초에서 막는다 — 고치기 전 2.3초(런 1147) · 고친 뒤 한두 프레임.\"}";
            foreach (var dir in PlayShot.Dirs())
            {
                try { System.IO.Directory.CreateDirectory(dir); System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "t496_hold.json"), json); }
                catch (System.Exception e) { Debug.LogWarning("[T496] t496_hold.json 저장 실패(" + dir + "): " + e.Message); }
            }
        }
    }
}
