namespace KkomaKnight.Core
{
    /// <summary>
    /// T182 <b>3단계</b> — «세로 신축» 의 셈 한 곳(순수 C# · 유니티 없이 자로 잰다).
    /// <para>
    /// 주인 지시는 «9:19 말고 9:16 이나 그런 거도 되게»(11:5X)다. 2단계까지로 <b>넓은 화면</b>(9:16·3:4 태블릿)은 풀렸다 —
    /// 프레임 폭에 상한을 두고 남는 좌우를 배경 띠로 채운다. 남은 것은 <b>키가 더 큰 폰</b>(9:21 등)이다:
    /// 지금은 위·아래에 레터박스가 <b>7.3%</b> 생기고 그만큼이 놀고 있다.
    /// </para>
    /// <b>규칙</b>(지시서 §2 T182 3단계 «위/아래 고정 · 가운데 신축»):
    /// <list type="number">
    /// <item><b>위 띠</b>(기준 프레임의 0 ~ <see cref="TopFixedPct"/>%) — 상단 바·재화 pill·챕터 제목·진행 바. <b>픽셀 높이를 지킨다</b>(위에 붙은 채로).</item>
    /// <item><b>아래 띠</b>(<see cref="BottomFixedPct"/>% ~ 100%) — 하단 패널·탭 바·주 버튼. <b>픽셀 높이를 지킨다</b>(아래에 붙은 채로).</item>
    /// <item><b>가운데</b> — 남는 높이를 <b>전부 가져간다</b>(전투 마당·로비 카드·목록이 길어진다).</item>
    /// </list>
    /// 왜 이 갈래인가 — 프레임을 통째로 늘이면 아이콘·동그라미가 <b>7% 찌그러진다</b>. 위·아래는 «붙어 있어야 하는 것»(바·탭)이라
    /// 픽셀을 지키는 것이 옳고, 늘어난 몫을 가운데가 먹는 것이 세로 게임의 표준 답이다.
    /// <para>
    /// ⚠ <b>이 파일은 셈만 한다</b> — 화면에 붙이는 것(<c>UiKit.Pct</c>·<c>CreateFrame</c>·<c>WorldCam</c> 환산)은 3단계-2 다.
    /// <see cref="K"/> 가 <b>1 이면 모든 함수가 항등</b>이라, 붙이기 전까지 화면은 지금과 한 치도 다르지 않다(T183 «기본값 = 지금 그대로» 와 같은 갈래).
    /// </para>
    /// </summary>
    public static class Stretch
    {
        /// <summary>기준 프레임(레퍼런스 jpg 의 비율) — <c>UiKit.FrameW/FrameH</c> 와 같은 수. 표(`docs/ref-layout.md`)가 전부 이 눈금이다.</summary>
        public const float RefW = 1080f, RefH = 2337f;
        /// <summary>«위 고정 띠» 의 끝(기준 프레임 %) — 표에서 여기까지가 상단 바(3.7~8.2)·메뉴(9.2~13.3)·챕터 제목(11.0~13.6)·진행 바(14.2~15.2)다.</summary>
        public const float TopFixedPct = 20f;
        /// <summary>«아래 고정 띠» 의 시작(기준 프레임 %) — 전투 하단 패널이 69.5 에서 시작하고 배속·라운드 버튼이 63.0~69.5 에 있다.</summary>
        public const float BottomFixedPct = 65f;
        /// <summary>받는 가장 큰 세로비(주인 «9:16 ~ 9:21» 의 위 끝). 이보다 길쭉한 화면은 남는 만큼 2단계 띠(<c>FrameBackdrop</c>)가 채운다.</summary>
        public const float MaxAspect = 21f / 9f;

        /// <summary>기준 프레임의 세로비(= 2337/1080 ≈ 2.1639 = 9:19.475).</summary>
        public static float RefAspect => RefH / RefW;

        /// <summary>
        /// 신축 배수 — «실제 프레임 높이 ÷ 기준 프레임 높이»(폭은 같다). <b>1 이면 지금 그대로</b>.
        /// 넓은 화면(기준보다 납작)은 2단계가 폭 상한으로 처리하므로 여기서는 1 로 막고, <see cref="MaxAspect"/> 위도 막는다.
        /// </summary>
        public static float K(float frameW, float frameH)
        {
            if (frameW <= 0f || frameH <= 0f) return 1f;
            float k = (frameH / frameW) / RefAspect;
            if (k < 1f) return 1f;
            float max = MaxAspect / RefAspect;
            return k > max ? max : k;
        }

        /// <summary>
        /// 기준 프레임의 세로 위치(%) → 늘어난 프레임의 세로 위치(%). 위·아래 띠는 픽셀을 지키고 가운데가 남는 높이를 먹는다.
        /// 단조 증가라 순서가 뒤집히지 않고, <paramref name="k"/> 가 1 이면 항등이다.
        /// </summary>
        public static float MapY(float yPct, float k)
        {
            if (k <= 1f) return yPct;
            float top = TopFixedPct / k;                       // 위 띠: 픽셀 유지 → % 는 줄어든다
            float bottom = 100f - (100f - BottomFixedPct) / k; // 아래 띠: 아래에서 잰 픽셀 유지
            if (yPct <= TopFixedPct) return yPct / k;
            if (yPct >= BottomFixedPct) return 100f - (100f - yPct) / k;
            float t = (yPct - TopFixedPct) / (BottomFixedPct - TopFixedPct);
            return top + t * (bottom - top);
        }

        /// <summary>기준 표의 세로 사각형(위 %, 높이 %) → 늘어난 프레임의 것. 아래 변을 따로 옮겨 «두 변 사이» 로 높이를 낸다(띠를 걸친 칸도 맞는다).</summary>
        public static void MapRow(float yPct, float hPct, float k, out float y, out float h)
        {
            y = MapY(yPct, k);
            h = MapY(yPct + hPct, k) - y;
        }

        /// <summary>늘어난 프레임에서 «가운데» 가 가져간 높이(기준 대비 %p) — 게이트·로그가 «얼마나 벌었나» 를 이 수로 말한다.</summary>
        public static float MiddleGainPct(float k)
        {
            if (k <= 1f) return 0f;
            float refMiddle = BottomFixedPct - TopFixedPct;
            float now = (100f - (100f - BottomFixedPct) / k) - TopFixedPct / k;
            return now * k - refMiddle;   // 기준 프레임의 눈금으로 되돌려 재야 «벌었다» 가 %p 로 읽힌다
        }
    }
}
