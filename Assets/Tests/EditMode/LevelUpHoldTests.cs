using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T368 — 주인 2026-09-10 «화폐 흡수돼서 경험치 올라서 레벨업 되기 전까지는 행동들 멈추면 안 됨 ·
    /// 걷는 거를 멈춘다던지 전투를 멈춘다던지 그런 거 ㄴㄴ · 정상적으로 하다가 흡수돼서 레벨업 되고 나서 특전 화면 뜰 때 멈춰야 함».
    /// <para>
    /// <b>재는 것은 «화면» 이 아니라 규칙이다</b> — 워커는 PlayMode 를 못 돌리므로(결정 143) 이 절의 심장인
    /// <see cref="BattleState.HoldLevelUp"/> 을 순수 C# 으로 잰다. 화면 배선(흡수 중에 이 값을 세우는 것)은
    /// <c>BattleScreen</c> 쪽이고 CI 가 본다.
    /// </para>
    /// <para>
    /// ⚑ <b>이 자가 지키는 것은 «멈추지 않는다» 하나가 아니라 셋이다</b>:
    /// ① 붙잡는 동안 <b>엔진이 돈다</b>(시간·자리가 나아간다) ② 그동안 레벨업이 <b>사라지지 않는다</b>(줄에 남는다) ·
    /// ③ 놓으면 <b>그 자리에서 열린다</b>. ②가 빠지면 «안 멈추는데 특전을 안 주는» 더 나쁜 회귀가 되고,
    /// 그것은 화면만 보면 «고쳐진 것» 처럼 보인다.
    /// </para>
    /// </summary>
    public class LevelUpHoldTests
    {
        /// <summary>
        /// 붙잡은 채로 «레벨업이 줄에 설 때» 까지 돌린다.
        /// <para>
        /// ⚑ <b>멈추는 조건에 <c>Pending != null</c> 을 같이 둔 까닭</b>(이 자를 처음 쓴 회차의 실측) —
        /// 처음에는 <c>while (PendingLevelUps == 0 &amp;&amp; !Over)</c> 로만 돌렸는데, <b>고침을 빼서 일부러 깨 보니 빨강이 아니라
        /// <c>Ignore</c>(초록)</b> 였다: 붙잡기가 없으면 레벨업이 <b>얻은 그 틱에</b> 창으로 바뀌어 <c>PendingLevelUps</c> 가
        /// 0 을 벗어나지 못하고, 루프는 판이 끝나서 빠져나와 «잴 것이 없다» 로 지나간다.
        /// 곧 <b>재려던 고장이 있을 때 오히려 조용해지는 자</b>였다(결정 966 의 «거울» 과 같은 꼴 · 화면만 보면 고쳐진 것처럼 보인다).
        /// ⇒ 두 끝을 다 잡고 나와서 <b>어느 쪽으로 나왔는지</b>를 부르는 쪽이 단언한다.
        /// </para>
        /// </summary>
        static BattleState RunHeldUntilLevelUpIsDueOrShown()
        {
            var d = TestData.Load(); var rng = new Mulberry32(5); var b = GearSystem.MkBuild(d, 3, 9, 100);
            var G = new BattleState(d, 1, b, rng, new InteractivePolicy(), new RunOptions { EmitEvents = true });
            // 이벤트 노드(쉼터·악마·천사)는 저희끼리 Pending 을 세워 «레벨업이 아닌 멈춤» 을 만든다 — 이 자가 재려는 것이 아니라 치운다.
            foreach (var n in G.Nodes) if (n.Type == NodeType.Rest || n.Type == NodeType.Devil || n.Type == NodeType.Angel) n.Done = true;
            G.HoldLevelUp = true;   // 붙잡은 채로 돌린다 = «흡수 중» 을 흉내 낸 것
            int guard = 0;
            while (G.Pending == null && G.PendingLevelUps == 0 && !G.Over && guard++ < 200000) G.Tick();
            return G;
        }

        /// <summary>두 자가 함께 쓰는 관문 — 붙잡았는데 창이 섰으면 <b>그 자리에서 빨강</b>이고, 아무 일도 없었으면 잴 것이 없다.</summary>
        static void AssertHeldNotShown(BattleState G)
        {
            Assert.That(G.Pending, Is.Null,
                "붙잡았는데(HoldLevelUp = true) 창이 섰다 — 그 순간 엔진이 멈추고 주인이 본 «걷기·전투가 멈춘다» 가 된다");
            if (G.Over || G.PendingLevelUps == 0) Assert.Ignore("이 판에서는 레벨업이 줄에 서지 않았다 — 잴 것이 없다");
        }

        /// <summary>붙잡는 동안 <b>엔진이 계속 돈다</b> — 주인이 본 «걷기·전투가 멈춘다» 가 여기서 갈린다.</summary>
        [Test]
        public void HoldLevelUp_붙잡는_동안_엔진이_계속_돈다()
        {
            var G = RunHeldUntilLevelUpIsDueOrShown();
            AssertHeldNotShown(G);

            double t0 = G.T, x0 = G.P.WorldX;
            for (int i = 0; i < 60; i++) Assert.That(G.Tick(), Is.True, "붙잡는 동안에도 틱은 진행한다");
            Assert.That(G.T, Is.GreaterThan(t0), "시간이 흐른다");
            Assert.That(G.P.WorldX, Is.GreaterThanOrEqualTo(x0), "자리가 뒤로 가지는 않는다(적 앞이면 제자리에서 싸운다)");
            Assert.That(G.PendingLevelUps, Is.GreaterThan(0), "그동안 레벨업은 줄에 그대로 남는다 — 사라지면 특전을 영영 못 받는다");
            Assert.That(G.Pending, Is.Null, "붙잡는 동안에는 끝까지 Pending 이 안 선다");
        }

        /// <summary>놓으면 <b>그 다음 틱에</b> 창이 선다 — 붙잡기가 «미루기» 이지 «없애기» 가 아님을 못 박는다.</summary>
        [Test]
        public void HoldLevelUp_놓으면_바로_다음_틱에_창이_선다()
        {
            var G = RunHeldUntilLevelUpIsDueOrShown();
            AssertHeldNotShown(G);

            int queued = G.PendingLevelUps;
            G.HoldLevelUp = false;
            G.Tick();
            Assert.That(G.Pending, Is.Not.Null, "놓으면 창이 선다");
            Assert.That(G.Pending.Kind, Is.EqualTo(PendingKind.LevelUp));
            Assert.That(G.PendingLevelUps, Is.EqualTo(queued - 1), "줄에서 하나가 빠져 창이 됐다");
            // 그리고 창이 선 뒤에는 종전 규약 그대로 시간이 멈춘다(T2 · 이 절이 바꾸지 않은 자리).
            double t = G.T;
            Assert.That(G.Tick(), Is.False, "Pending 중에는 시간이 흐르지 않는다 — 이 규약은 그대로다");
            Assert.That(G.T, Is.EqualTo(t));
        }

        /// <summary>
        /// 기본값이 <c>false</c> 다 — 헤드리스·골든이 이 절 때문에 달라지지 않는다는 뜻이다.
        /// <para>⚠ 이 한 줄이 «시뮬 동일성» 을 지킨다: 값이 기본으로 켜져 있으면 <c>RunToEnd</c> 가 레벨업을 영영 안 열어 골든이 통째로 바뀐다.</para>
        /// </summary>
        [Test]
        public void HoldLevelUp_기본값은_꺼짐이라_헤드리스는_그대로다()
        {
            // ⚑ 표는 `PreBalance()` 다 — 아래 골든이 `BattleTests` 의 그 수이고 그쪽도 같은 표로 잰다(T325 · 결정 1033).
            //   `Load()` 로 두면 밸런스가 바뀌는 날 이 자가 «이 절이 골든을 흔들었다» 고 거짓말한다 — 흔든 것은 밸런스다.
            var d = TestData.PreBalance(); var rng = new Mulberry32(11); var b = GearSystem.MkBuild(d, -1, 0, 0);
            var G = new BattleState(d, 3, b, rng, new SimPolicy(), new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false });
            Assert.That(G.HoldLevelUp, Is.False, "기본은 꺼짐 — 화면만 세운다");

            // 골든 한 판이 이 절 앞뒤로 같은지까지 본다(BattleTests 의 그 수 그대로 · 수를 새로 베끼지 않았다).
            var r = G.RunToEnd();
            Assert.That(r.Time, Is.EqualTo(83.17).Within(0.01), "골든이 안 움직인다");
            Assert.That(r.Level, Is.EqualTo(6));
        }
    }
}
