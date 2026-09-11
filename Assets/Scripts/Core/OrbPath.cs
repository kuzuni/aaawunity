using System;

namespace KkomaKnight.Core
{
    /// <summary>
    /// T461 ⓐ(주인 2026-09-12 «화폐 흡수되는 이펙트가 뭔가 부드럽게 안 가는 느낌인 거 수정») — <b>흡수 구슬이 그리는 길의 셈</b>.
    /// <para>
    /// ⚑ <b>지시서 0항의 «직선» 은 실측과 다르다 — 곡선은 T109 때부터 이미 있었다.</b>
    /// <c>RewardOrbs.Make</c> 는 처음부터 2차 베지어로 날았다. 실제로 «툭툭» 하게 만든 것은 <b>가속의 방향</b>이다:
    /// 비행 트윈이 <c>Ease.InQuad</c>(t²)라 구슬이 <b>도착하는 순간에 가장 빠르고</b> 그 상태로 잘려 «팝» 으로 넘어간다.
    /// 실측(선분 800px · 옆으로 0.33 · 400칸): 도착 속도가 평균의 <b>2.44배</b>다. 사람 눈에 «부드럽지 않다» 는 그 자리다.
    /// </para>
    /// <para>
    /// 그래서 고친 것은 곡선이 아니라 <b>t 를 먹는 방식</b>이다 — <see cref="Ease"/>(InOutSine)로 바꾸면 같은 곡선에서
    /// 도착 속도가 평균의 <b>0.01배</b>로 떨어지고 꼬리 30%가 <b>단조 감속</b>이 된다(그 둘을 <c>OrbPathTests</c> 가 잰다).
    /// </para>
    /// <para>
    /// ⚠ <b>궤도 자체의 고르기(arc-length 재매개화)는 일부러 안 했다</b> — 재 보니 등속(Linear)으로 돌렸을 때
    /// 이 곡선의 속도 편차가 <b>최대/최소 1.4배</b>뿐이다. 눈에 띄는 것은 ease 쪽(797배)이라, 값이 큰 쪽만 고친다.
    /// </para>
    /// <para>
    /// ⚑ <b>왜 Core 인가</b> — <c>Game</c> 은 이 통(<c>tools/dotnet</c>)이 컴파일조차 안 해서(결정 143) 화면 셈은
    /// 전부 PlayMode 몫이 되고, 그것은 «배포까지 가서야 드러난다»(결정 1259·1271 이 <c>ShortNum</c> 으로 낸 길).
    /// 순수 셈만 여기 두면 <b>매 회차 로컬에서</b> 돈다. 붙이는 일(트윈·<c>Vector2</c>)은 <c>Game/RewardOrbs.cs</c> 가 한다.
    /// </para>
    /// </summary>
    public static class OrbPath
    {
        /// <summary>프레임 px 한 점(유니티 <c>Vector2</c> 를 못 쓴다 — Core 는 <c>noEngineReferences</c>). <b>+Y = 위</b>(uGUI <c>anchoredPosition</c> 과 같다).</summary>
        public struct P
        {
            public float X, Y;
            public P(float x, float y) { X = x; Y = y; }
            public override string ToString() => "(" + X + ", " + Y + ")";
        }

        /// <summary>꼬리(감속 + 크기 줄임)가 시작하는 자리 — 지시서 1항 «도착 직전 30% 는 감속».</summary>
        public const float TailFrom = 0.7f;
        /// <summary>도착 순간의 크기 배수 — 지시서 1항 «도착에 가까울수록 0.8 배»(도착 뒤 «팝» 1.15 는 <c>RewardOrbs</c> 가 그대로 이어 간다).</summary>
        public const float ArriveScale = 0.8f;
        /// <summary>제어점을 옆으로 미는 몫(출발~과녁 선분 길이 대비 · T109 의 0.22~0.45 무작위를 대신하는 가운뎃값).</summary>
        public const float BowShare = 0.30f;
        /// <summary>구슬마다 곡률을 조금씩 달리하는 폭 — 지시서 1항 «무작위 0 · 인덱스로». <see cref="BowShares"/> 갈래 수만큼 돌려 쓴다.</summary>
        public const float BowShareStep = 0.06f;
        /// <summary>곡률 갈래 수(0.30 · 0.36 · 0.42 를 돌려 쓴다).</summary>
        public const int BowShares = 3;
        /// <summary>옆으로 미는 최소 길이(구슬 폭 배수) — 아주 가까운 과녁에서도 활이 서게.</summary>
        public const float BowMinInOrbs = 2.5f;
        /// <summary>제어점을 위로 드는 길이(구슬 폭 배수) — T109 의 «1~3배 무작위» 를 대신하는 가운뎃값.</summary>
        public const float RiseInOrbs = 2f;

        /// <summary>
        /// 비행 진행률 — <b>InOutSine</b>(0.5·(1−cos πt)). 출발도 도착도 속도 0 에서 시작·끝난다.
        /// <para>⚠ 이 한 줄이 T461 ⓐ 의 알맹이다. 종전 <c>Ease.InQuad</c> 는 «도착이 가장 빠른» 꼴이라 흡수가 끊기듯 보였다.</para>
        /// </summary>
        public static float Ease(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return (float)(0.5 * (1.0 - Math.Cos(Math.PI * t)));
        }

        /// <summary>
        /// 비행 중 구슬 크기 배수 — 꼬리(<see cref="TailFrom"/>)부터 <see cref="ArriveScale"/> 까지 곧게 줄어든다.
        /// <para>과녁(재화 pill)으로 «빨려 들어가는» 느낌을 내는 자리 — 감속과 <b>같은 창</b>을 쓴다(둘이 어긋나면 «작아졌는데 아직 빠르다» 가 된다).</para>
        /// </summary>
        public static float Scale(float t)
        {
            if (t <= TailFrom) return 1f;
            if (t >= 1f) return ArriveScale;
            float k = (t - TailFrom) / (1f - TailFrom);
            return 1f + (ArriveScale - 1f) * k;
        }

        /// <summary>2차 베지어 — <paramref name="t"/>=0 이 <paramref name="a"/>, 1 이 <paramref name="b"/>.</summary>
        public static P Bezier(P a, P c, P b, float t)
        {
            float u = 1f - t;
            return new P(u * u * a.X + 2f * u * t * c.X + t * t * b.X,
                         u * u * a.Y + 2f * u * t * c.Y + t * t * b.Y);
        }

        /// <summary>
        /// 제어점 — 선분의 가운데에서 <b>진행 방향의 옆</b>으로 밀고 조금 위로 든다.
        /// <para>
        /// ⚑ <b>무작위가 없다</b>(지시서 1항). 좌·우는 <paramref name="i"/> 의 짝·홀로 번갈고 곡률은 <see cref="BowShares"/> 갈래를 돌려 쓴다 —
        /// 종전 <c>Random.value &lt; 0.5f</c> 는 이웃한 구슬이 같은 쪽으로 몰리는 판이 흔해 «뭉쳐 가는» 것처럼 보였고, 무엇보다 <b>잴 수가 없었다</b>.
        /// </para>
        /// </summary>
        /// <param name="a">출발(튀어오른 자리)</param><param name="b">과녁</param>
        /// <param name="i">구슬 번호(0부터)</param><param name="sizePx">구슬 한 변(px)</param>
        public static P Ctrl(P a, P b, int i, float sizePx)
        {
            if (i < 0) i = 0;
            if (sizePx < 0f) sizePx = 0f;
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            float px, py;
            if (len > 0.001f) { px = -dy / len; py = dx / len; }
            else { px = 0f; py = 1f; }
            float side = (i & 1) == 0 ? 1f : -1f;
            float share = BowShare + (i / 2 % BowShares) * BowShareStep;
            float bow = Math.Max(sizePx * BowMinInOrbs, len * share) * side;
            float mx = (a.X + b.X) * 0.5f, my = (a.Y + b.Y) * 0.5f;
            return new P(mx + px * bow, my + py * bow + sizePx * RiseInOrbs);
        }
    }
}
