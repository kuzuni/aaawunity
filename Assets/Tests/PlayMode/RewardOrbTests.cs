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
    /// T85 — 주인 지시 «적 죽이면 경험치랑 골드가 적 죽은 거에서 나와서 각각의 UI 에 흡수되는 애니메이션 ·
    /// 흡수될 때 숫자가 애니메이션으로 차게 · 그거 다 차고 나서 레벨업이면 특전창».
    /// 엔진(<see cref="BattleState"/>)은 킬 순간에 이미 골드·경험치를 올린다(시드 골든 불변) — 여기서 보는 것은 <b>표시값과 팝업 타이밍</b>뿐이다.
    /// ⓐ 킬 뒤 구슬(<see cref="RewardOrbs.OrbName"/>)이 생기고 곧 전부 사라진다 ⓑ 그 뒤 표시 골드·경험치 = 엔진 값
    /// ⓒ 레벨업이 걸린 킬에서 «흡수가 끝나기 전» 에는 특전창이 열리지 않는다 ⓓ 배속 x2 에서도 같은 순서 ⓔ 빨간 줄 0(<see cref="PlayLog"/> · T11 규약).
    /// </summary>
    public class RewardOrbTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

        // 구슬 한 벌(시차 + 홉 + 비행 + 도착 팝)의 상한 — RewardOrbs 상수에서 계산해 박은 값이 아니다(결정 191)
        static float OrbLifeMax(int count, float speed)
            => ((count - 1) * RewardOrbs.StepSec + RewardOrbs.HopSec + RewardOrbs.FlySecMax + RewardOrbs.PopSec) / Mathf.Max(0.5f, speed) + 0.35f;

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

        /// <summary>한 방 킬(킬이 자주 나게) + 이벤트 노드는 미리 끝내 팝업으로 멈추지 않게.</summary>
        static void Arm(BattleState G, bool allowLevelUp)
        {
            G.P.Dmg = 1e6;
            if (!allowLevelUp) G.P.Exp = int.MinValue / 2;
            foreach (var n in G.Nodes) if (n.Type == NodeType.Rest || n.Type == NodeType.Devil || n.Type == NodeType.Angel) n.Done = true;
        }

        /// <summary>ⓐ·ⓑ — 킬 자리에서 구슬이 나와 HUD 로 날아가 사라지고, 그 뒤 표시 골드·경험치가 엔진 값과 정확히 같아진다.</summary>
        [UnityTest]
        public IEnumerator OrbsFlyFromTheKillAndTheHudCatchesUpExactly()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            Arm(G, false);
            Assert.AreEqual(G.Gold, bs.ShownGold, 1e-6, "판을 시작하면 표시 골드 = 엔진 골드(0)");

            Time.timeScale = 3f;   // 첫 적까지 걷는 시간을 줄인다
            float t0 = Time.realtimeSinceStartup;
            while (G.Kills == 0 && Time.realtimeSinceStartup - t0 < 30f && !G.Over) yield return null;
            Time.timeScale = 1f;
            Assert.Greater(G.Kills, 0, "30초 안에 적을 한 번은 죽여야 시험이 성립한다");
            Assert.Greater(G.Gold, 0, "킬이면 엔진 골드가 올라 있어야 한다(엔진 불변 — 즉시 오른다)");

            // 사망 연출이 시작되면(칼이 내려온 뒤) 구슬이 튀어나온다
            t0 = Time.realtimeSinceStartup;
            while (bs.OrbCount == 0 && Time.realtimeSinceStartup - t0 < 5f) yield return null;
            Assert.Greater(bs.OrbCount, 0, "적이 쓰러지는 순간 그 자리에서 보상 구슬이 나와야 한다(주인 지시)");
            int peak = bs.OrbCount;
            Assert.IsNotNull(GameObject.Find(RewardOrbs.OrbName), "구슬 오브젝트(이름 «" + RewardOrbs.OrbName + "»)가 화면에 있어야 한다");
            Assert.Less(bs.ShownGold, G.Gold, "구슬이 도착하기 전에는 표시 골드가 엔진 값보다 작아야 한다(«흡수될 때 차오른다»)");

            G.P.Dmg = 0;   // 이 뒤로는 새 킬이 없다 — 이번 한 벌의 수명만 잰다
            float tOrb = Time.realtimeSinceStartup, limit = OrbLifeMax(peak, 1f);
            while (bs.OrbCount > 0 && Time.realtimeSinceStartup - tOrb < limit + 1f) yield return null;
            Assert.AreEqual(0, bs.OrbCount, "구슬은 " + limit.ToString("0.00") + "초 안에 전부 도착해 사라져야 한다");
            Assert.LessOrEqual(Time.realtimeSinceStartup - tOrb, limit, "구슬 수명이 상한(시차+홉+비행+도착 팝)을 넘었다");

            t0 = Time.realtimeSinceStartup;
            while (bs.Absorbing && Time.realtimeSinceStartup - t0 < 2f) yield return null;
            Assert.IsFalse(bs.Absorbing, "구슬이 다 도착했으면 카운트업도 곧 끝나야 한다");
            Assert.AreEqual(G.Gold, bs.ShownGold, 1e-6, "흡수가 끝나면 표시 골드 = 엔진 골드");
            Assert.AreEqual(BattleScreen.ExpTotal(G, _app.Data), bs.ShownExp, 1e-6, "흡수가 끝나면 표시 누적 경험치 = 엔진 값");
            Assert.LessOrEqual(bs.ShownGold, G.Gold + 1e-9, "표시값은 엔진 값을 넘지 않는다");
            _log.AssertNoRed("보상 흡수");

            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }

        /// <summary>ⓒ·ⓓ — 레벨업 특전창은 «구슬이 다 흡수되고 바가 다 찬 뒤에» 열린다. 배속 x2 에서도 순서가 같다.</summary>
        [UnityTest]
        public IEnumerator LevelUpPopupOpensOnlyAfterTheBarIsFull([Values(1, 2)] int speed)
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            Arm(G, true);
            if (speed == 2 && bs.Speed != 2) bs.ToggleSpeed();
            Assert.AreEqual(speed, bs.Speed, "배속이 시험 값이어야 한다");
            G.P.Exp = _app.Data.Tune.ExpNeed(G.P.Level) - 1;   // 다음 킬이면 레벨업(엔진 규칙 그대로 · 수치는 tune.json 에서 읽는다)

            Time.timeScale = 3f;
            float t0 = Time.realtimeSinceStartup;
            while (G.Kills == 0 && Time.realtimeSinceStartup - t0 < 30f && !G.Over) yield return null;
            Time.timeScale = 1f;
            Assert.Greater(G.Kills, 0, "30초 안에 적을 한 번은 죽여야 한다");

            bool sawOrbs = false, popupOpened = false; bool prevAbsorbing = bs.Absorbing;
            t0 = Time.realtimeSinceStartup;
            while (!popupOpened && Time.realtimeSinceStartup - t0 < 15f)
            {
                yield return null;
                if (bs.OrbCount > 0) sawOrbs = true;
                if (_app.Overlay.IsOpen)
                {
                    popupOpened = true;
                    // 앞 프레임 끝에서 흡수가 진행 중이었다면 «다 차기 전에» 연 것이다(주인 지시 위반)
                    Assert.IsFalse(prevAbsorbing, "흡수가 끝나기 전에 팝업이 열렸다 — 바가 다 찬 뒤에 열려야 한다(T85 · 배속 x" + speed + ")");
                    Assert.AreEqual(0, bs.OrbCount, "팝업이 열릴 때 날아다니는 구슬이 남아 있으면 안 된다");
                    Assert.AreEqual(BattleScreen.ExpTotal(G, _app.Data), bs.ShownExp, 1e-6, "팝업이 열릴 때 표시 경험치는 엔진 값과 같아야 한다(바가 다 찼다)");
                    break;
                }
                prevAbsorbing = bs.Absorbing;
            }
            Assert.IsTrue(sawOrbs, "레벨업이 걸린 킬에서도 구슬이 나와야 한다");
            Assert.IsTrue(popupOpened, "레벨업이면 흡수가 끝난 뒤 특전창이 열려야 한다(15초 안)");
            Assert.Greater(G.P.Level, 1, "엔진 레벨이 올라 있어야 한다(엔진 불변 — 킬 즉시)");
            _log.AssertNoRed("레벨업 대기 → 특전창");

            _app.Overlay.Close(); yield return Frames(2);
            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }
    }
}
