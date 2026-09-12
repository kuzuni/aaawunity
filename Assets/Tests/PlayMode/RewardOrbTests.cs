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

        /// <summary>
        /// T313 — <b>리워드 팝업의 흡수는 개수와 상관없이 1초 안에 끝난다</b>(주인 2026-09-09 10:0X «흡수 파티클이 느리다 — 1초 안에 전부 흡수»).
        /// <para>
        /// 종전에는 구슬 사이 시차가 개수만큼 쌓여 <b>많이 받을수록 느려졌다</b>(100개면 마지막 구슬이 8초쯤 뒤). 수로 재는 자라 씬을 안 올린다.
        /// </para>
        /// ⚠ <b>전투 구슬은 안 건드렸다</b> 도 같이 잰다 — 예산을 안 넘긴 부름(전투)은 종전 값 그대로여야 한다.
        /// «빨라졌다» 만 재면 «전투까지 같이 빨라졌다» 를 못 잡는다.
        /// </summary>
        [Test]
        public void PopupAbsorbFinishesWithinItsBudgetNoMatterHowManyOrbs()
        {
            float budget = RewardOrbs.PopupBudgetSec;
            foreach (int n in new[] { 1, 2, 5, 20, 50, 100 })
            {
                float last = RewardOrbs.LastArrivalSec(n, 0f, budget);
                Assert.LessOrEqual(last, budget + 1e-4f,
                    "구슬 " + n + "개의 마지막 도착이 예산(" + budget + "초)을 넘었다 — 지금 " + last.ToString("0.###") + "초");
            }

            // 적게 받을 때는 종전 연출이 그대로여야 한다(주인이 정한 «0.8초 곡선» · T109) — 예산은 «넘칠 때만» 조인다.
            RewardOrbs.Pace(1, 0f, budget, out float step1, out float fly1, out float jit1);
            Assert.AreEqual(RewardOrbs.StepSec, step1, 1e-6f, "구슬 하나짜리는 시차가 종전 그대로");
            Assert.AreEqual(RewardOrbs.FlySec, fly1, 1e-6f, "구슬 하나짜리는 비행 시간이 종전 그대로");
            Assert.AreEqual(RewardOrbs.FlyJitter, jit1, 1e-6f, "안 조인 판은 흔들림도 그대로");

            // 조금 받을 때는 시차를 «늘리지» 않는다 — 남는 시간은 비행에 준다(눈으로는 종전과 거의 같다).
            RewardOrbs.Pace(3, 0f, budget, out float step3, out float fly3, out _);
            Assert.AreEqual(RewardOrbs.StepSec, step3, 1e-6f, "구슬 셋이면 시차는 종전 그대로");
            Assert.Greater(fly3, RewardOrbs.FlySec * 0.8f, "그 대신 비행이 거의 종전 길이로 남는다");

            // 조인 판에서는 흔들지 않는다 — 흔들면 마지막 구슬이 예산을 넘는다.
            RewardOrbs.Pace(100, 0f, budget, out float step100, out float fly100, out float jit100);
            Assert.AreEqual(0f, jit100, 1e-6f, "예산에 맞추는 판은 비행 시간을 안 흔든다");
            Assert.Less(step100, RewardOrbs.StepSec, "100개짜리는 시차가 줄어야 한다");
            Assert.GreaterOrEqual(fly100, RewardOrbs.FlyMinSec - 1e-4f, "그래도 «날아간다» 로 보이는 하한은 지킨다");

            // ⚑ 전투(예산 0)는 종전 그대로 — 여기가 «남의 연출을 같이 줄이지 않았나» 를 재는 자리다.
            RewardOrbs.Pace(100, RewardOrbs.HoldSec, 0f, out float bs, out float bf, out float bj);
            Assert.AreEqual(RewardOrbs.StepSec, bs, 1e-6f, "전투 구슬의 시차는 그대로");
            Assert.AreEqual(RewardOrbs.FlySec, bf, 1e-6f, "전투 구슬의 비행 시간도 그대로");
            Assert.AreEqual(RewardOrbs.FlyJitter, bj, 1e-6f);
            Assert.Greater(RewardOrbs.LastArrivalSec(100, RewardOrbs.HoldSec, 0f), 5f,
                "전투 구슬 100개는 여전히 오래 걸린다 — 그것이 종전 연출이고 주인이 고쳐 달라고 한 자리가 아니다");
        }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

        // 구슬 한 벌(시차 + 홉 + 머무름 + 비행 + 도착 팝)의 상한 — RewardOrbs 상수에서 계산해 박은 값이 아니다(결정 191 · T109 로 머무름이 늘었다)
        static float OrbLifeMax(int count, float speed)
            => ((count - 1) * RewardOrbs.StepSec + RewardOrbs.HopSec + RewardOrbs.HoldSec + RewardOrbs.FlySecMax + RewardOrbs.PopSec) / Mathf.Max(0.5f, speed) + 0.35f;

        /// <summary>
        /// 지금 화면에 꼬리가 몇 개 있나 — T144 로 꼬리가 <b>월드 <c>TrailRenderer</c></b> 가 됐고(주인 지시), 머티리얼을 빌릴 월드 스프라이트가
        /// 없는 자리에서는 T109 의 잔상 스프라이트로 물러난다. 둘 중 무엇이든 «꼬리가 있다» 로 센다.
        /// </summary>
        static int TrailCount()
        {
            int n = UnityEngine.Object.FindObjectsByType<TrailRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            foreach (var t in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (t.name == RewardOrbs.TrailName) n++;
            return n;
        }

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
            int trailPeak = 0;
            // T461 ⓑ — 꼬리가 «보이는 자리» 에 있는가. 살아 있는 동안 한 번 잡아 둔다(사라진 뒤엔 못 잰다).
            int trailOrder = int.MinValue;
            while (bs.OrbCount > 0 && Time.realtimeSinceStartup - tOrb < limit + 1f)
            {
                trailPeak = Mathf.Max(trailPeak, TrailCount());
                if (trailOrder == int.MinValue)
                {
                    var tr = UnityEngine.Object.FindFirstObjectByType<TrailRenderer>(FindObjectsInactive.Exclude);
                    if (tr != null) trailOrder = tr.sortingOrder;
                }
                yield return null;
            }
            Assert.AreEqual(0, bs.OrbCount, "구슬은 " + limit.ToString("0.00") + "초 안에 전부 도착해 사라져야 한다");
            // T144(주인 «흡수될 때 트레일 랜더러로») — 구슬이 도는 동안 월드 꼬리가 떠 있었고, 끝나면 하나도 안 남는다(누수 0).
            Assert.Greater(trailPeak, 0, "흡수 중에는 꼬리(TrailRenderer)가 떠 있어야 한다(T144)");
            // T461 ⓑ(주인 2026-09-12 «전투 화면에서 화폐 흡수되는 거는 트레일 렌더러 있게 해야 함») —
            //   «떠 있다» 와 «보인다» 는 다르다. 종전 TrailSortOrder 는 −50 이었고 전투 바닥이 −40 이라
            //   꼬리가 바닥 그림 뒤에 그려졌다 — 자는 T144 부터 초록인데 주인 눈에는 한 번도 안 보였다.
            //   ⚠ 이 레포의 정렬 층은 «Default» 하나뿐이라(ProjectSettings/TagManager) 순서 수만으로 앞뒤가 정해진다 —
            //      층까지 견주는 단언은 늘 참이라 안 쓴다.
            Assert.AreNotEqual(int.MinValue, trailOrder, "꼬리를 한 번도 못 잡았다 — 아래 두 단언이 공허해진다");
            Assert.Greater(trailOrder, BattleWorld.OrderNearProp,
                $"꼬리가 배경(바닥 {BattleWorld.OrderField} · 소품 {BattleWorld.OrderNearProp}) 뒤다 — 전투에서 안 보인다(실제 {trailOrder})");
            var playerRig = UnityEngine.Object.FindObjectsByType<CharacterRig>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int charTop = int.MinValue;
            foreach (var rig in playerRig)
                foreach (var sr in rig.GetComponentsInChildren<SpriteRenderer>(true))
                    charTop = Mathf.Max(charTop, sr.sortingOrder);
            Assert.AreNotEqual(int.MinValue, charTop, "전투에 캐릭터 그림이 하나도 없다 — 아래 단언이 공허해진다");
            Assert.GreaterOrEqual(trailOrder, charTop,
                $"꼬리는 캐릭터보다 앞이어야 한다(지시서 T461 3항 · 꼬리 {trailOrder} · 캐릭터 맨 앞 {charTop})");
            float tTrail = Time.realtimeSinceStartup;
            while (TrailCount() > 0 && Time.realtimeSinceStartup - tTrail < RewardOrbs.TrailTime + 1.5f) yield return null;
            Assert.AreEqual(0, TrailCount(), "구슬이 사라지면 꼬리도 남으면 안 된다(T144 · 누수 0)");
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
                if (trailSeenAt < 0f && TrailCount() > 0) trailSeenAt = now;
                if (tArrive < 0f && Vector2.Distance(p, to) < size * 0.6f) tArrive = now;
                if (tArrive > 0f && orbs.Alive == 0) break;
            }

            Assert.Greater(tDepart, 0f, "구슬이 출발하는 순간을 못 봤다");
            Assert.Greater(tArrive, 0f, "구슬이 목표에 닿는 순간을 못 봤다");
            // ⓐ 머무름 — 홉(0.15s)이 끝나고 최소 0.8s 는 그 자리에 떠 있어야 한다(주인 «1초 정도»)
            Assert.GreaterOrEqual(tDepart, RewardOrbs.HopSec + RewardOrbs.HoldSec * 0.8f,
                $"구슬이 너무 빨리 출발했다 — 홉 뒤 {RewardOrbs.HoldSec}초쯤 머물러야 한다(실측 출발 {tDepart:0.00}s)");
            // ⓑ 전체 = 홉 + 머무름 + 비행 ≈ 1.95초. «출발» 을 어디로 잡느냐(T461 뒤로는 OrbPath.Ease = InOutSine 이라 처음도 끝도 느리다)에
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
            Assert.Greater(off, size * 0.8f, $"경로가 직선에 가깝다 — 활을 그려야 한다(T461 뒤로는 구슬 번호가 좌·우와 곡률을 정한다 · 직선에서 {off:0.0}px 벗어남)");
            // ⓓ 트레일
            Assert.Greater(trailSeenAt, 0f, "구슬 뒤에 꼬리가 남아야 한다(T109 3항 · T144 로 월드 TrailRenderer)");
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

            bool sawOrbs = false, popupOpened = false;
            t0 = Time.realtimeSinceStartup;
            while (!popupOpened && Time.realtimeSinceStartup - t0 < 15f)
            {
                yield return null;
                if (bs.OrbCount > 0) sawOrbs = true;
                if (_app.Overlay.IsOpen)
                {
                    popupOpened = true;
                    // ⛑ T495 — **«앞 프레임의 `Absorbing`» 표본을 걷어냈다**(워커 P 가 런 1142 에서 가려 놓고 갔다 · 결정 1376).
                    //   ⓐ **그 단언은 진짜 어긋남을 잡을 수가 없다** — `BattleScreen.OpenPending`(`:531`)이 `if (Absorbing) return;` 으로
                    //      **흡수 중에는 아예 안 연다**(`showNow` 상한 갈래도 `OpenPending` 안에서 같은 문을 지난다). 곧 «팝업이 열렸다» 는
                    //      그 순간 `Absorbing` 이 거짓이었다는 뜻이고, 같은 프레임에서 재면 **늘 참**이다(공허 · 결정 1279).
                    //   ⓑ **그런데 «앞 프레임» 표본은 낡을 수 있다** — 코루틴은 Update 뒤에 깨어나므로 표본과 다음 프레임의 `OpenPending` 사이에
                    //      LateUpdate(`RewardOrbs`)·DOTween·`AbsorbTick` 이 낀다. 그 사이에 흡수가 끝나면 **옳게 열린 회차가 빨개진다.**
                    //      ⇒ 이 단언은 **참을 못 잡고 거짓만 만든다**. 런 1127~1141 열다섯 번 초록 뒤 코드 0줄인 채 뒤집힌 것이 그 꼴이다.
                    //   ⓒ **대신 «바가 다 찼는가» 를 열린 그 프레임에서 눈에 보이는 값으로 잰다** — 구슬 0 · 표시 경험치 = 엔진 값 ·
                    //      **표시 골드 = 엔진 값**(골드 쪽은 여태 안 쟀다 · `Absorbing` 이 보던 절반이 여기 있다).
                    //      누가 `:531` 의 그 한 줄을 지우면 팝업이 흡수 도중 열리고, 그때는 이 셋이 **그 자리에서** 운다 — 계약은 그대로 지켜진다.
                    Assert.AreEqual(0, bs.OrbCount, "팝업이 열릴 때 날아다니는 구슬이 남아 있으면 안 된다(T85 · 배속 x" + speed + ")");
                    Assert.AreEqual(BattleScreen.ExpTotal(G, _app.Data), bs.ShownExp, 1e-6, "팝업이 열릴 때 표시 경험치는 엔진 값과 같아야 한다(바가 다 찼다)");
                    Assert.AreEqual(G.Gold, bs.ShownGold, 1e-6, "팝업이 열릴 때 표시 골드도 엔진 값과 같아야 한다(금화 쪽도 다 찼다 · T495)");
                    break;
                }
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
