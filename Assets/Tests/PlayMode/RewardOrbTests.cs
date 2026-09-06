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

        // 구슬 한 벌(시차 + 홉 + 머무름 + 비행 + 도착 팝)의 상한 — RewardOrbs 상수에서 계산해 박은 값이 아니다(결정 191 · T109 로 머무름이 늘었다)
        static float OrbLifeMax(int count, float speed)
            => ((count - 1) * RewardOrbs.StepSec + RewardOrbs.HopSec + RewardOrbs.HoldSec + RewardOrbs.FlySecMax + RewardOrbs.PopSec) / Mathf.Max(0.5f, speed) + 0.35f;

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


        /// <summary>
        /// T109(주인 «1초 정도 머물렀다가 랜덤 곡선 그리면서 (트레일 있어야 함) 0.8초 동안 흡수») — 구슬 하나를 직접 날려
        /// ⓐ 머무름 ⓑ 비행 시간 ⓒ 경로가 직선이 아님 ⓓ 잔상(트레일) ⓔ 값이 정확히 한 번 지급되는 것을 잰다.
        /// 전투를 거치지 않고 <see cref="RewardOrbs"/> 를 바로 쓰는 이유 = 킬 타이밍·배속에 흔들리지 않게(시간을 재는 시험이다).
        /// </summary>
        [UnityTest]
        public IEnumerator OrbHoversThenFliesOnACurveWithATrail()
        {
            yield return Boot();
            var layer = UiKit.Rect(_app.UiCanvas.transform, "OrbTestLayer"); UiKit.Stretch(layer);
            var target = UiKit.Rect(layer, "OrbTestTarget"); UiKit.Pct(target, 78, 8, 12, 5);
            yield return Frames(1);

            var orbs = new RewardOrbs(layer);
            var to = orbs.TargetPos(target);
            var from = to + new Vector2(-UiKit.FrameW * 0.55f, UiKit.FrameH * 0.45f);
            const float size = 40f;
            double got = 0; int arrivals = 0;
            Assert.AreEqual(1, orbs.Fly(from, target, "pi.orb", Palette.Green, 1, 10.0, size, 1f, v => { got += v; arrivals++; }), "구슬 1개를 띄운다");

            RectTransform orb = null;
            foreach (var rt in layer.GetComponentsInChildren<RectTransform>(true)) if (rt.name == RewardOrbs.OrbName) orb = rt;
            Assert.IsNotNull(orb, "구슬 오브젝트");

            float t0 = Time.realtimeSinceStartup;
            float tDepart = -1f, tArrive = -1f, trailSeenAt = -1f;
            Vector2 hopPos = Vector2.zero; bool hopTaken = false;
            Vector2 departPos = Vector2.zero, midPos = Vector2.zero; float midAt = -1f;
            float limit = OrbLifeMax(1, 1f) + 1.5f;
            while (Time.realtimeSinceStartup - t0 < limit)
            {
                yield return null;
                if (orb == null) break;
                float now = Time.realtimeSinceStartup - t0;
                var p = orb.anchoredPosition;
                // 홉이 끝난 자리(머무름의 기준점) — 홉 시간 뒤 첫 프레임에서 잡는다
                if (!hopTaken && now >= RewardOrbs.HopSec + 0.02f) { hopPos = p; hopTaken = true; }
                // 출발 = 그 기준점에서 눈에 띄게 벗어난 순간(머무름의 흔들림 폭 0.35×크기 보다 넉넉히 크게 잡는다)
                if (hopTaken && tDepart < 0f && Vector2.Distance(p, hopPos) > size * 0.8f) { tDepart = now; departPos = p; }
                if (tDepart > 0f && midAt < 0f && now - tDepart >= RewardOrbs.FlySec * 0.45f) { midAt = now; midPos = p; }
                if (trailSeenAt < 0f)
                    foreach (var rt in layer.GetComponentsInChildren<RectTransform>(true)) if (rt.name == RewardOrbs.TrailName) { trailSeenAt = now; break; }
                if (tArrive < 0f && Vector2.Distance(p, to) < size * 0.6f) tArrive = now;
                if (tArrive > 0f && orbs.Alive == 0) break;
            }

            Assert.Greater(tDepart, 0f, "구슬이 출발하는 순간을 못 봤다");
            Assert.Greater(tArrive, 0f, "구슬이 목표에 닿는 순간을 못 봤다");
            // ⓐ 머무름 — 홉(0.15s)이 끝나고 최소 0.8s 는 그 자리에 떠 있어야 한다(주인 «1초 정도»)
            Assert.GreaterOrEqual(tDepart, RewardOrbs.HopSec + RewardOrbs.HoldSec * 0.8f,
                $"구슬이 너무 빨리 출발했다 — 홉 뒤 {RewardOrbs.HoldSec}초쯤 머물러야 한다(실측 출발 {tDepart:0.00}s)");
            // ⓑ 전체 = 홉 + 머무름 + 비행 ≈ 1.95초. «출발» 을 어디로 잡느냐(Ease.InQuad 라 처음이 느리다)에
            //    흔들리지 않게 «닿은 시각» 으로 잰다 — 옛 값(머무름 0 · 비행 0.35~0.5)이면 0.65초라 크게 떨어진다.
            float whole = RewardOrbs.HopSec + RewardOrbs.HoldSec + RewardOrbs.FlySec;
            Assert.That(tArrive, Is.InRange(whole - 0.3f, whole + 0.5f),
                $"홉+머무름+비행 = {whole:0.00}초 언저리에 닿아야 한다(실측 {tArrive:0.00}s)");
            float flight = tArrive - tDepart;
            Assert.Greater(flight, RewardOrbs.FlySec * 0.45f, $"비행이 너무 짧다(실측 {flight:0.00}s · 규정 {RewardOrbs.FlySec}s)");
            Assert.LessOrEqual(tDepart, RewardOrbs.HopSec + RewardOrbs.HoldSec + RewardOrbs.FlySec * 0.6f, "머무름이 규정보다 훨씬 길다");
            // ⓒ 직선이 아니다 — 비행 한가운데가 «출발 → 목표» 직선에서 벗어나 있다
            Assert.Greater(midAt, 0f, "비행 한가운데를 못 봤다");
            var ab = to - departPos; float abLen = ab.magnitude;
            float off = abLen < 0.001f ? 0f : Mathf.Abs(ab.x * (midPos.y - departPos.y) - ab.y * (midPos.x - departPos.x)) / abLen;
            Assert.Greater(off, size * 0.8f, $"경로가 직선에 가깝다 — 랜덤 곡선이어야 한다(직선에서 {off:0.0}px 벗어남)");
            // ⓓ 트레일
            Assert.Greater(trailSeenAt, 0f, $"구슬 뒤에 잔상(«{RewardOrbs.TrailName}»)이 남아야 한다(T109 3항)");
            // ⓔ 값은 정확히 한 번, 전부
            float tv = Time.realtimeSinceStartup;
            while (orbs.Alive > 0 && Time.realtimeSinceStartup - tv < 2f) yield return null;
            Assert.AreEqual(1, arrivals, "도착 콜백은 한 번");
            Assert.AreEqual(10.0, got, 1e-9, "값은 남김없이 지급된다");
            _log.AssertNoRed("T109 구슬 연출");

            orbs.Clear(); UnityEngine.Object.Destroy(layer.gameObject); yield return Frames(2);
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
