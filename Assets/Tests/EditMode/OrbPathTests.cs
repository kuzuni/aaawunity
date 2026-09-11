using System;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T461 ⓐ — <b>흡수 구슬이 «부드럽게» 가는가</b>를 수로 잰다(주인 2026-09-12 «뭔가 부드럽게 안 가는 느낌인 거 수정»).
    /// <para>
    /// ⚑ <b>«부드럽다» 를 눈으로만 두면 아무도 못 지킨다.</b> 종전 자(<c>Assets/Tests/PlayMode/RewardOrbTests.cs</c>)는
    /// <b>예산</b>(몇 초 안에 끝나는가)만 쟀고 <b>어떻게 가는가</b>는 단언이 0개였다 — 그래서 «도착이 가장 빠른» 꼴이
    /// T109 부터 지금까지 살아남았다. 여기서 재는 것은 그 자리다: <see cref="OrbPath.Ease"/> 가 바뀌면 이 자가 운다.
    /// </para>
    /// <para>⚠ 이 자는 <b>Core</b> 라 매 회차 로컬에서 돈다(결정 143 · <c>Game</c> 은 이 통이 컴파일도 안 한다).</para>
    /// </summary>
    public class OrbPathTests
    {
        const int N = 400;

        /// <summary>실측에 쓴 판 — 선분 800px · 구슬 64px(리워드 팝업에서 실제로 나는 크기 대) · 첫 구슬.</summary>
        static void Sample(out OrbPath.P a, out OrbPath.P b, out OrbPath.P c, int i = 0, float sizePx = 64f)
        {
            a = new OrbPath.P(0f, 0f);
            b = new OrbPath.P(800f, 0f);
            c = OrbPath.Ctrl(a, b, i, sizePx);
        }

        /// <summary>한 칸(1/N)마다 움직인 거리 = 속도(px/칸). <paramref name="eased"/> 가 참이면 <see cref="OrbPath.Ease"/> 를 먹인다.</summary>
        static double[] Speeds(bool eased, int i = 0)
        {
            OrbPath.P a, b, c; Sample(out a, out b, out c, i);
            var sp = new double[N];
            var prev = OrbPath.Bezier(a, c, b, eased ? OrbPath.Ease(0f) : 0f);
            for (int k = 1; k <= N; k++)
            {
                float t = (float)k / N;
                var p = OrbPath.Bezier(a, c, b, eased ? OrbPath.Ease(t) : t);
                double dx = p.X - prev.X, dy = p.Y - prev.Y;
                sp[k - 1] = Math.Sqrt(dx * dx + dy * dy);
                prev = p;
            }
            return sp;
        }

        [Test]
        public void BezierStartsAtSourceAndEndsAtTarget()
        {
            OrbPath.P a, b, c; Sample(out a, out b, out c);
            var s = OrbPath.Bezier(a, c, b, 0f);
            var e = OrbPath.Bezier(a, c, b, 1f);
            Assert.AreEqual(a.X, s.X, 0.001f, "t=0 은 출발이다");
            Assert.AreEqual(a.Y, s.Y, 0.001f, "t=0 은 출발이다");
            Assert.AreEqual(b.X, e.X, 0.001f, "t=1 은 과녁이다");
            Assert.AreEqual(b.Y, e.Y, 0.001f, "t=1 은 과녁이다");
        }

        /// <summary>⚑ 아래 «감속» 단언들이 <b>곧은 선</b>을 재고 있으면 공허하다 — 활이 실제로 서는지를 먼저 못 박는다(결정 1279 가 치른 값).</summary>
        [Test]
        public void MidFlightBowsOutToTheSide()
        {
            OrbPath.P a, b, c; Sample(out a, out b, out c);
            var mid = OrbPath.Bezier(a, c, b, 0.5f);
            Assert.Greater(Math.Abs(mid.Y), 60f,
                "가운데가 직선(y≈0)에서 옆으로 벗어나야 «곡선» 이다 — 실제 " + mid);
            var q = OrbPath.Bezier(a, c, b, 0.25f);
            Assert.Greater(Math.Abs(q.Y), 30f, "1/4 지점도 선 위가 아니다");
        }

        [Test]
        public void EaseRunsFromZeroToOneWithoutGoingBack()
        {
            Assert.AreEqual(0f, OrbPath.Ease(0f), 1e-6f);
            Assert.AreEqual(1f, OrbPath.Ease(1f), 1e-6f);
            Assert.AreEqual(0f, OrbPath.Ease(-0.5f), 1e-6f, "범위 밖은 물린다");
            Assert.AreEqual(1f, OrbPath.Ease(2f), 1e-6f, "범위 밖은 물린다");
            float prev = -1f;
            for (int k = 0; k <= N; k++)
            {
                float v = OrbPath.Ease((float)k / N);
                Assert.GreaterOrEqual(v, prev, "진행률은 되돌아가지 않는다");
                prev = v;
            }
            Assert.AreEqual(0.5f, OrbPath.Ease(0.5f), 1e-3f, "한가운데는 절반이다");
        }

        /// <summary>
        /// ⚑ <b>이 자가 T461 ⓐ 의 알맹이다</b> — 종전 <c>Ease.InQuad</c> 로는 도착 속도가 평균의 <b>2.44배</b>였다(그때는 이 단언이 운다).
        /// </summary>
        [Test]
        public void ArrivesSlowlyInsteadOfAtFullSpeed()
        {
            var sp = Speeds(true);
            double avg = 0; foreach (var v in sp) avg += v; avg /= sp.Length;
            Assert.Greater(avg, 0.0, "재는 판이 비어 있으면 아래가 전부 공허하다");
            Assert.Less(sp[sp.Length - 1], avg * 0.25,
                "도착 속도가 평균의 1/4 밑이어야 «빨려 들어가듯» 보인다 — 실제 " + sp[sp.Length - 1] + " / 평균 " + avg);
            Assert.Less(sp[0], avg * 0.25, "출발도 같은 자리에서 천천히 뜬다");
        }

        /// <summary>지시서 1항 «도착 직전 30% 는 감속» — 꼬리 구간이 <b>단조</b> 감속인가.</summary>
        [Test]
        public void TailThirtyPercentOnlySlowsDown()
        {
            var sp = Speeds(true);
            int from = (int)(sp.Length * OrbPath.TailFrom);
            for (int k = from; k < sp.Length - 1; k++)
                Assert.LessOrEqual(sp[k + 1], sp[k] + 1e-6,
                    "꼬리에서 다시 빨라지는 자리가 있다(칸 " + k + ")");
            Assert.Greater(sp[from], sp[sp.Length - 1] * 5.0,
                "꼬리가 실제로 느려져야 한다(끝이 시작의 1/5 밑) — 공허 방지");
        }

        [Test]
        public void ScaleShrinksOnlyInTheTail()
        {
            Assert.AreEqual(1f, OrbPath.Scale(0f), 1e-6f);
            Assert.AreEqual(1f, OrbPath.Scale(OrbPath.TailFrom), 1e-6f, "꼬리 앞에서는 크기 그대로다");
            Assert.AreEqual(OrbPath.ArriveScale, OrbPath.Scale(1f), 1e-6f);
            Assert.AreEqual(OrbPath.ArriveScale, OrbPath.Scale(3f), 1e-6f, "범위 밖은 물린다");
            float prev = 2f;
            for (int k = 0; k <= N; k++)
            {
                float v = OrbPath.Scale((float)k / N);
                Assert.LessOrEqual(v, prev + 1e-6f, "크기는 커지지 않는다");
                prev = v;
            }
            Assert.Less(OrbPath.ArriveScale, 1f, "«0.8배» 가 1 이 되면 이 절 전체가 공허하다");
        }

        /// <summary>지시서 1항 «무작위 0 · 인덱스로» — 같은 번호는 늘 같은 활, 이웃한 번호는 반대쪽.</summary>
        [Test]
        public void CurveIsDecidedByIndexNotByChance()
        {
            OrbPath.P a = new OrbPath.P(0f, 0f), b = new OrbPath.P(800f, 0f);
            var c0 = OrbPath.Ctrl(a, b, 0, 64f);
            var again = OrbPath.Ctrl(a, b, 0, 64f);
            Assert.AreEqual(c0.X, again.X, 1e-6f, "같은 번호는 늘 같은 자리다(무작위 0)");
            Assert.AreEqual(c0.Y, again.Y, 1e-6f, "같은 번호는 늘 같은 자리다(무작위 0)");

            var c1 = OrbPath.Ctrl(a, b, 1, 64f);
            Assert.Less(c0.Y * c1.Y, 0f, "이웃한 구슬은 좌·우가 갈린다 — 실제 " + c0 + " / " + c1);

            var c2 = OrbPath.Ctrl(a, b, 2, 64f);
            Assert.Greater(c2.Y * c0.Y, 0f, "두 칸 뒤는 같은 쪽이고");
            Assert.Greater(Math.Abs(c2.Y), Math.Abs(c0.Y) + 1f, "곡률은 조금 다르다(인덱스로 갈래를 돌려 쓴다)");
        }

        /// <summary>과녁이 코앞이어도(선분이 0 에 가까워도) 활이 서고 셈이 깨지지 않는다.</summary>
        [Test]
        public void VeryCloseTargetStillBows()
        {
            var a = new OrbPath.P(10f, 10f);
            var b = new OrbPath.P(10f, 10f);
            var c = OrbPath.Ctrl(a, b, 0, 64f);
            var mid = OrbPath.Bezier(a, c, b, 0.5f);
            Assert.IsFalse(float.IsNaN(mid.X) || float.IsNaN(mid.Y), "길이 0 에서도 셈이 성하다");
            double away = Math.Sqrt((mid.X - a.X) * (mid.X - a.X) + (mid.Y - a.Y) * (mid.Y - a.Y));
            Assert.Greater(away, 10.0, "겹친 두 점에서도 구슬 폭만큼은 벗어났다 돌아온다");
        }

        /// <summary>구슬이 여러 개 날 때 이웃끼리 <b>서로 다른 길</b>을 간다 — «뭉쳐 가는» 것처럼 보이던 자리.</summary>
        [Test]
        public void NeighbouringOrbsDoNotShareOnePath()
        {
            for (int i = 0; i < 8; i++)
            {
                var m0 = OrbPath.Bezier(new OrbPath.P(0f, 0f), OrbPath.Ctrl(new OrbPath.P(0f, 0f), new OrbPath.P(800f, 0f), i, 64f), new OrbPath.P(800f, 0f), 0.5f);
                var m1 = OrbPath.Bezier(new OrbPath.P(0f, 0f), OrbPath.Ctrl(new OrbPath.P(0f, 0f), new OrbPath.P(800f, 0f), i + 1, 64f), new OrbPath.P(800f, 0f), 0.5f);
                Assert.Greater(Math.Abs(m0.Y - m1.Y), 30.0, "구슬 " + i + " 와 " + (i + 1) + " 의 가운데가 너무 붙었다");
            }
        }
    }
}
