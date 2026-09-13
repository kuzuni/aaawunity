using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T319 — 주인 «PvP 뜰 때 <b>중앙에서 두 캐릭터 만나서 싸우는 식</b>으로 해야 함. 내 플레이어가 오른쪽으로 이동 느낌이 아니라».
    /// <para>
    /// 챕터 판은 «플레이어를 화면 한 자리에 붙들고 세상을 흘려보내는» 꼴이라 1대1 판에서도 배경이 흘러 <b>내가 여행하는 그림</b>이 됐다.
    /// 고침은 화면 쪽 한 곳이다 — 아레나 판만 <b>원점을 «둘이 만나는 점» 에 못 박고</b> 그 점을 화면 가운데로 삼는다.
    /// 그러면 ⓐ 배경이 안 흐르고 ⓑ 내가 왼쪽 밖에서 걸어 들어와 ⓒ 가운데에서 상대와 마주 선다.
    /// </para>
    /// <para>
    /// <b>엔진은 한 줄도 안 봤다</b>(거리·속도·판정·시드 그대로) — 그래서 이 자는 «엔진 값» 이 아니라 <b>그려진 자리</b>만 잰다:
    /// 땅이 서 있나 · 플레이어 그림이 만난 뒤 안 움직이나 · 둘이 가운데를 사이에 두고 마주 서 있나.
    /// 그리고 <b>절반은 «챕터는 옛 그대로» 다</b> — 아레나만 보는 자는 «아레나를 고치다 챕터 스크롤을 죽였다» 를 못 잡는다(T240 <see cref="PvpHudTests"/> 와 같은 까닭).
    /// </para>
    /// </summary>
    public class PvpStageTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

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
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
        }

        /// <summary>땅 타일 하나 — 배경이 흐르는지는 이것 하나면 보인다(<c>ScrollGround</c> 가 매 프레임 x 를 다시 놓는다).</summary>
        static Transform GroundTile()
        {
            var world = GameObject.Find("World"); if (world == null) return null;
            var ground = world.transform.Find("Ground"); if (ground == null || ground.childCount == 0) return null;
            return ground.GetChild(0);
        }

        /// <summary>표에 적히는 그대로의 자리(프레임 폭 %) — <see cref="BattleWorld.MeasureLayout"/> 가 재 준 값이다.</summary>
        static float Row(Dictionary<string, float[]> m, string key, int i)
        {
            Assert.IsTrue(m.ContainsKey(key), $"«{key}» 를 재야 한다(BattleWorld.MeasureLayout)");
            return m[key][i];
        }
        static float PlayerCenter(Dictionary<string, float[]> m) => Row(m, "플레이어 중심 x", 0);
        /// <summary>상대 그림의 가운데 x — 표는 왼쪽·폭으로 적으므로 가운데는 여기서 뽑는다.</summary>
        static float FoeCenter(Dictionary<string, float[]> m) => Row(m, "적 높이", 0) + Row(m, "적 높이", 2) * 0.5f;

        /// <summary>T319 ⓐⓑⓒ — 아레나 판: 땅은 안 흐르고, 내가 걸어 들어와, 가운데를 사이에 두고 마주 선 채 <b>안 움직인다</b>.</summary>
        [UnityTest]
        public IEnumerator ArenaRunMeetsAtTheCentreAndTheStageNeverScrolls()
        {
            yield return Boot();

            _app.StartBattle(1, null, null, "ShadowKnight", 3);
            yield return Frames(3);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            Assert.IsTrue(bs.IsArena, "아레나 판이어야 이 자가 성립한다");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            if (G.Nodes.Count != 1) Assert.Ignore("1대1 노드가 안 섰다(아레나 표·순위가 비었다) — 잴 것이 없다");

            // 만난 자리를 오래 들여다보려고 «안 죽는 판» 으로 만든다(BattleWorldTests.Arm 과 같은 손짓 · 값은 화면 시험용이다).
            G.P.Dmg = 0; G.P.Hp = G.P.MaxHp = 1e9;

            var tile = GroundTile(); Assert.IsNotNull(tile, "땅 타일");
            float groundX0 = tile.position.x;
            float startPlayer = PlayerCenter(world.MeasureLayout());

            float prevPlayer = startPlayer, still = 0f, minPlayer = startPlayer, maxPlayer = startPlayer;
            int stillFrames = 0;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 20f && stillFrames < 30 && !G.Over && !_app.Overlay.IsOpen)
            {
                yield return null;
                // ⓐ 배경은 판 내내 서 있다 — 이 한 줄이 주인이 말한 «오른쪽으로 이동하는 느낌» 의 정체다.
                Assert.AreEqual(groundX0, tile.position.x, 1e-4f, "아레나 판에서는 땅이 안 흐른다(화면 원점 고정)");
                float now = PlayerCenter(world.MeasureLayout());
                if (now < minPlayer) minPlayer = now;
                if (now > maxPlayer) maxPlayer = now;
                if (Mathf.Approximately(now, prevPlayer)) { stillFrames++; still = now; } else stillFrames = 0;
                prevPlayer = now;
            }

            // ⓒ 만난 뒤에는 «두 프레임 사이 이동 0» 이 이어진다(ROUTINE 3항 ⓐ).
            Assert.GreaterOrEqual(stillFrames, 30, "만난 뒤 플레이어 그림이 30 프레임을 내리 제자리에 있어야 한다(그 자리에서 싸운다)");
            // ⓑ 그 전에는 왼쪽 밖에서 걸어 들어왔다 — 서 있기만 했으면 «만나러 온» 것이 아니다.
            Assert.Greater(maxPlayer - minPlayer, 15f, "판이 시작해 만나기까지 플레이어 그림이 왼쪽에서 가운데로 걸어 들어와야 한다");
            Assert.Greater(still, startPlayer, "걸어 들어온 방향은 오른쪽(왼쪽 밖 → 가운데)이다");

            var m = world.MeasureLayout();
            float p = PlayerCenter(m), f = FoeCenter(m);
            Assert.Less(p, 50f, "선 자리: 나는 화면 가운데의 왼쪽이다");
            Assert.Greater(f, 50f, "선 자리: 상대는 화면 가운데의 오른쪽이다");
            // 둘이 «가운데에서» 만난다 = 가운데가 둘의 한가운데다. (10% 는 그림 폭 · 무기 삐침을 넉넉히 품는 값이다 —
            //  옛 그림은 내가 16%(ui.json playerX)에 붙박여 있었으므로 한가운데가 40% 언저리였다: 이 자는 그 꼴을 잡는다.)
            Assert.AreEqual(50f, (p + f) * 0.5f, 10f, "둘이 만나는 자리는 화면 가운데다(레퍼런스 33 · 좌우 대칭)");

            _log.AssertNoRed("아레나 판 가운데 대치");
            yield return Shutdown();
        }

        /// <summary>T319 ⓑ(반대쪽) — 챕터 판은 <b>옛 그대로</b>다: 플레이어는 <c>ui.json playerX</c> 자리에 붙박이고 땅이 흐른다.</summary>
        [UnityTest]
        public IEnumerator ChapterRunStillPinsThePlayerAndScrollsTheGround()
        {
            yield return Boot();

            _app.StartBattle(1);
            yield return Frames(3);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            Assert.IsFalse(bs.IsArena, "보통 챕터 판이다");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            var G = bs.G; Assert.IsNotNull(G, "전투 상태");

            var tile = GroundTile(); Assert.IsNotNull(tile, "땅 타일");
            float groundX0 = tile.position.x;
            float pinned = PlayerCenter(world.MeasureLayout());
            bool groundMoved = false;

            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 6f && !G.Over && !_app.Overlay.IsOpen)
            {
                yield return null;
                Assert.AreEqual(pinned, PlayerCenter(world.MeasureLayout()), 0.2f, "챕터 판에서 플레이어 그림은 한 자리에 붙박여 있다(옛 그대로)");
                if (Mathf.Abs(tile.position.x - groundX0) > 1e-3f) groundMoved = true;
            }
            Assert.IsTrue(groundMoved, "챕터 판에서는 땅이 흘러야 한다 — 아레나를 고치다 이쪽 스크롤을 죽이면 여기서 잡힌다");

            _log.AssertNoRed("챕터 판 스크롤");
            yield return Shutdown();
        }
    }
}
