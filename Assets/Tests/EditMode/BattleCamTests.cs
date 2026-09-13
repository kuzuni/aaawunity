using System;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T510(주인 2026-09-13 «그 플레이어가 실제로 오른쪽으로 가면서 게임이 진행되야하는데 맵이랑 적들이 왼쪽으로 움직이는 방식 이더라») —
    /// 전투 카메라(<see cref="BattleCam"/>)를 <b>실제 전투를 돌려 가며</b> 잰다.
    /// <para>
    /// ⚑ <b>왜 여기(EditMode·순수 C#)서 잴 수 있나</b>: 화면 좌표는 <c>LayoutX = (worldX − 원점) × zoom + playerX</c> 한 줄이고
    /// 지금 간격 사상은 항등(<see cref="Layout.WorldSpacing"/> = 1)이다. 그래서 <b>그리기 없이</b> 그 한 줄을 그대로 옮겨
    /// 엔진 틱(<c>BattleState.Tick</c>)마다 «플레이어 그림이 어디 있나 · 맵이 흘렀나» 를 셀 수 있다 —
    /// PlayMode(유니티)가 없는 워커 통에서도 <c>dotnet test</c> 로 돈다(<see cref="Layout.PetGap"/> 과 같은 까닭 · 결정 143).
    /// ⚠ 그림 자체(플레이어 리그가 정말 그 자리에 서는가)는 PlayMode 의 <c>PvpStageTests</c> 가 잰다 — 여기서 재는 것은 <b>규칙</b>이다.
    /// </para>
    /// </summary>
    public class BattleCamTests
    {
        /// <summary>프레임 안 레이아웃 폭(<c>WorldCam.LayoutW</c>) — Game 어셈블리라 여기서 못 읽는다(<c>PetBattleGapTests</c> 와 같은 실측 상수).</summary>
        const double LayoutW = 540;

        /// <summary>한 판 — 엔진과 카메라를 같이 돌리며 «화면이 어떻게 보였나» 를 센다.</summary>
        sealed class Run
        {
            public double PlayerX, RightX, Zoom;
            public BattleCam Cam;
            public BattleState G;
            public int Frames, WalkFrames, FightWalkFrames, FightWalkStillMap, MapMovedWhileStanding, OriginWentBack;
            public double MinScreenX = double.MaxValue, MaxScreenX = double.MinValue;
            public double MaxStillRun;              // 맵이 안 흐르는 동안 플레이어 그림이 이어서 오른쪽으로 간 거리(레이아웃 px)
            public double MinLeadAfterFirstWave = double.MaxValue;

            public double ScreenX(double worldX) => (worldX - Cam.OriginPX) * Zoom + PlayerX;
        }

        static Run Play(int chapter, int rare, int plus, int lv, uint seed, int maxFrames = 30 * 400)
        {
            var d = TestData.Load();
            var r = new Run
            {
                PlayerX = d.Ui.PlayerX * LayoutW,
                RightX = Layout.BattleCamRightPct / 100.0 * LayoutW,
                Zoom = d.Ui.CameraZoom,
            };
            r.Cam = new BattleCam((r.RightX - r.PlayerX) / r.Zoom, d.Combat.StopDistance + d.Combat.EnemyGap * 1.5, Layout.BattleCamCatchUp, 600);
            var rng = new Mulberry32(seed);
            r.G = new BattleState(d, chapter, GearSystem.MkBuild(d, rare, plus, lv), rng, new SimPolicy(), new RunOptions());
            r.Cam.Reset(r.G.P.WorldX);

            double prev = r.G.P.WorldX, stillRun = 0; int kills0 = 0;
            while (!r.G.Over && r.Frames < maxFrames)
            {
                r.G.Tick();
                double shown = r.G.P.WorldX, adv = shown - prev;
                double dist = NearestDist(r.G);
                bool fight = dist <= r.Cam.MarchDist;
                double before = r.Cam.OriginPX, beforeScreen = (prev - before) * r.Zoom + r.PlayerX;   // 직전 프레임에 그림이 서 있던 화면 x
                r.Cam.Step(shown, adv, dist);
                bool mapStill = Math.Abs(r.Cam.OriginPX - before) < 1e-9;
                double px = r.ScreenX(shown);

                if (r.Cam.OriginPX < before - 1e-9) r.OriginWentBack++;
                if (px < r.MinScreenX) r.MinScreenX = px;
                if (px > r.MaxScreenX) r.MaxScreenX = px;
                if (adv > 1e-9)
                {
                    r.WalkFrames++;
                    if (fight) { r.FightWalkFrames++; if (mapStill) r.FightWalkStillMap++; }
                    if (mapStill) { stillRun += px - beforeScreen; if (stillRun > r.MaxStillRun) r.MaxStillRun = stillRun; }
                    else stillRun = 0;
                }
                else if (!mapStill) r.MapMovedWhileStanding++;
                if (r.G.Kills > kills0) { kills0 = r.G.Kills; }
                if (kills0 > 0) { double lead = r.Cam.Lead(shown); if (lead < r.MinLeadAfterFirstWave) r.MinLeadAfterFirstWave = lead; }
                prev = shown; r.Frames++;
            }
            return r;
        }

        /// <summary>가장 앞(가장 가까운) 살아 있는 적까지의 거리 — 없으면 무한(= 행군). <c>BattleWorld.TargetDist</c> 와 같은 규칙이다.</summary>
        static double NearestDist(BattleState g)
        {
            double best = double.PositiveInfinity;
            foreach (var n in g.Nodes)
                foreach (var e in n.Enemies)
                    if (!e.Dead && e.Hp > 0 && e.WorldX - g.P.WorldX < best) best = e.WorldX - g.P.WorldX;
            return best;
        }

        // ───────────────────────── 규칙 자체(순수) ─────────────────────────

        [Test]
        public void OriginNeverOvertakesThePlayerAndStopsAtTheBandEdge()
        {
            var cam = new BattleCam(100, 140, 1.5, 600);
            cam.Reset(0);
            // 싸움 걸음(적이 가깝다) — 원점은 한 톨도 안 움직인다(= 맵이 선다)
            for (int i = 0; i < 50; i++) cam.Step(i * 2, 2, 80);
            Assert.That(cam.OriginPX, Is.EqualTo(0), "싸움 중에는 원점이 안 움직인다");
            // 띠를 넘어서면 딱 그 폭만큼만 뒤에 남는다
            cam.Step(140, 2, 80);
            Assert.That(cam.Lead(140), Is.EqualTo(100).Within(1e-9), "띠의 오른쪽 끝에서 원점이 밀린다");
            // 행군(적이 멀다) — 걸음의 1.5배로 따라붙어 앞선 거리가 준다
            double lead0 = cam.Lead(140), px = 140;
            for (int i = 0; i < 10; i++) { px += 2; cam.Step(px, 2, 1000); }
            Assert.That(cam.Lead(px), Is.LessThan(lead0), "행군에서는 따라붙어 플레이어가 왼쪽 끝으로 돌아온다");
            // 끝까지 따라붙어도 앞지르지 않는다(그러면 그림이 화면 왼쪽 끝 밖으로 나간다)
            for (int i = 0; i < 200; i++) { px += 2; cam.Step(px, 2, 1000); }
            Assert.That(cam.Lead(px), Is.EqualTo(0).Within(1e-9), "따라붙기는 플레이어에서 멎는다(앞지르지 않는다)");
        }

        [Test]
        public void StandingStillNeverMovesTheMap()
        {
            var cam = new BattleCam(100, 140, 1.5, 600);
            cam.Reset(0);
            cam.Step(50, 50, 1000);            // 행군 한 걸음
            double origin = cam.OriginPX;
            for (int i = 0; i < 100; i++) cam.Step(50, 0, 1000);   // 그 자리에 서 있다
            Assert.That(cam.OriginPX, Is.EqualTo(origin), "안 걸으면 맵도 안 흐른다 — 주인 지적의 핵심");
        }

        [Test]
        public void CatchUpFollowsTheStepSoSpeedUpAndDashKeepTheSameRatio()
        {
            // 배속 x2·대시는 «한 프레임에 더 걷는 것» 이라, 되돌리는 몫도 걸음에 그대로 비례해야 한다.
            var slow = new BattleCam(200, 140, 1.5, 600); slow.Reset(0);
            var fast = new BattleCam(200, 140, 1.5, 600); fast.Reset(0);
            double a = 0, b = 0;
            for (int i = 0; i < 20; i++) { a += 4; slow.Step(a, 4, 1000); }
            for (int i = 0; i < 10; i++) { b += 8; fast.Step(b, 8, 1000); }
            Assert.That(fast.OriginPX, Is.EqualTo(slow.OriginPX).Within(1e-9), "같은 거리를 걸었으면 프레임 수와 무관하게 같은 자리다");
        }

        [Test]
        public void RestoreSnapsInsteadOfSlidingTheWholeMap()
        {
            var cam = new BattleCam(100, 140, 1.5, 600);
            cam.Reset(0);
            cam.Step(5000, 5000, 1000);   // 탭 복귀 따라잡기 · 세이브 복원처럼 한 번에 멀리 뛴 경우
            Assert.That(cam.Lead(5000), Is.EqualTo(0).Within(1e-9), "복원은 즉시 맞춘다(화면이 5000px 를 흐르지 않는다)");
        }

        // ───────────────────────── 실제 전투로 재기 ─────────────────────────

        [Test]
        public void BandFitsTheFrameAndTheMarchThresholdSitsBetweenStepAndMarch()
        {
            var d = TestData.Load();
            double playerX = d.Ui.PlayerX * LayoutW, rightX = Layout.BattleCamRightPct / 100.0 * LayoutW, zoom = d.Ui.CameraZoom;
            Assert.That(rightX, Is.GreaterThan(playerX), "띠는 왼쪽 끝(ui.json playerX)보다 오른쪽이다");
            // 오른쪽 끝에 선 채로 싸워도 상대가 화면 안에 있어야 한다(멈춤 거리만큼 앞에 선다)
            double foeX = rightX + d.Combat.StopDistance * zoom;
            Assert.That(foeX, Is.LessThan(LayoutW), "띠의 오른쪽 끝에서 싸워도 상대가 화면 밖으로 안 나간다");
            // 행군 문턱: 무리 «안» 의 한 걸음보다 크고, 무리 «사이» 보다 작다
            double march = d.Combat.StopDistance + d.Combat.EnemyGap * 1.5;
            Assert.That(march, Is.GreaterThan(d.Combat.StopDistance + d.Combat.EnemyGap), "무리 안의 걸음은 «싸움» 으로 봐야 한다(그 걸음에 맵이 흐르면 안 된다)");
            Assert.That(march, Is.LessThan(d.Combat.StopDistance + d.Combat.NodeGap), "무리 사이는 «행군» 으로 봐야 한다");
        }

        [Test]
        public void MapNeverSlidesWhileThePlayerIsStandingStill()
        {
            foreach (var r in new[] { Play(1, -1, 0, 0, 11), Play(10, 1, 0, 6, 12), Play(30, 2, 0, 14, 13) })
            {
                Assert.That(r.Frames, Is.GreaterThan(200), "판이 실제로 돌았어야 한다");
                Assert.That(r.MapMovedWhileStanding, Is.EqualTo(0), "서 있는 동안 맵이 흐른 프레임이 하나도 없어야 한다(주인 지적의 핵심)");
                Assert.That(r.OriginWentBack, Is.EqualTo(0), "원점은 뒤로 안 간다(맵이 오른쪽으로 흐르면 안 된다)");
            }
        }

        [Test]
        public void PlayerActuallyWalksRightAcrossTheScreen()
        {
            var r = Play(10, 1, 0, 6, 12);
            Assert.That(r.MinScreenX, Is.EqualTo(r.PlayerX).Within(0.5), "가장 왼쪽은 띠의 왼쪽 끝(ui.json playerX)이다");
            Assert.That(r.MaxScreenX, Is.EqualTo(r.RightX).Within(0.5), "가장 오른쪽은 띠의 오른쪽 끝이다 — 그 밖으로는 절대 안 나간다");
            // 옛 꼴에서는 이 폭이 **0** 이었다(그림이 한 자리에 붙박여 있었다) — 이 자가 그 회귀를 잡는다.
            Assert.That(r.MaxScreenX - r.MinScreenX, Is.GreaterThan(LayoutW * 0.30), "플레이어 그림이 화면 폭의 30% 넘게 오른쪽으로 걸어가야 한다");
            // 그 중 «맵이 선 채로 이어서» 간 거리 — 걸음 하나(적 간격 44 × zoom 1.5 = 66px)보다 한참 커야 «무리를 걸어 나간» 것이다
            Assert.That(r.MaxStillRun, Is.GreaterThan(LayoutW * 0.25), "맵이 선 채로 화면 폭의 4분의 1 넘게 이어서 걸어간 구간이 있어야 한다");
        }

        [Test]
        public void FightStepsKeepTheMapStandingAndMarchesBringThePlayerBack()
        {
            foreach (var r in new[] { Play(1, -1, 0, 0, 11), Play(10, 1, 0, 6, 12) })
            {
                Assert.That(r.FightWalkFrames, Is.GreaterThan(30), "무리 안에서 걷는 프레임이 있어야 시험이 성립한다");
                double still = 100.0 * r.FightWalkStillMap / r.FightWalkFrames;
                Assert.That(still, Is.GreaterThan(70), "무리 안의 걸음은 대부분(>70%) 맵이 선 채로 간다 — 실측 85~96%, 띠의 오른쪽 끝에 닿은 뒤만 밀린다");
                Assert.That(r.MinLeadAfterFirstWave, Is.LessThan(30), "행군이 플레이어를 띠의 왼쪽 끝(±30 월드 px)으로 되돌려 다음 무리를 왼쪽에서 맞는다");
            }
        }
    }
}
