using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T194 — 검은 아웃라인 «규칙» 이 레퍼런스 굵기 안에 있는가(<see cref="UiKit.OutlineRatio"/> · <see cref="UiKit.OutlineMaxPx"/>).
    /// <para>
    /// 이 작업은 <b>한 화면의 색</b>이 아니라 <b>게임의 모든 글자에 걸리는 규칙</b>을 옮긴 것이라, 다음 워커가 «너무 굵다» 며 조용히 되돌리면
    /// T63-outline(주인 «모든 글자들 다 검정 아웃라인»)이 다시 회색 그림자로 주저앉는다. 그래서 <b>실측한 띠</b>를 여기 숫자로 박아 둔다.
    /// </para>
    /// <para>
    /// <b>띠의 출처(실측 · 되풀이할 수 있게 적는다)</b> — 레퍼런스 <c>docs/ref/17_daily_gift.jpg</c>(720폭)의 제목 리본을 가로로 468줄 훑어
    /// «휘도 0.30 미만이 이어지는 길이» 를 세면 최빈 <b>3px</b> · 중앙값 <b>4px</b> 다(우리 540폭 캡처로는 <b>2.25~3px</b>).
    /// </para>
    /// <para>
    /// ⚠ <b>그 값을 «규격» 과 곧바로 견주면 안 된다</b>(회차 1 이 그렇게 해서 두 회차를 썼다 · 결정 522) — 규칙 px 는 글자의 <b>로컬</b> 단위이고,
    /// 화면에 어둡게 찍히는 띠는 그보다 얇다. CI 로그의 진단 한 줄이 그 사이를 보여 준다: «리본 제목 크기 60 · 아웃라인 <b>4.8px</b> · lossyScale 0.21 → 화면 <b>1.0px</b>».
    /// 잰 손실은 <b>거의 일정한 2.2 PNG px</b>(비례가 아니라 상수라 얇을수록 통째로 사라진다) → 레퍼런스 띠를 내는 규격은 <b>6.7~7.8 프레임px</b> = 제목 60 에서 비율 <b>0.111~0.130</b>.
    /// </para>
    /// 씬을 안 띄운다 — 규칙(상수와 식)만 본다. 실제 글자마다 그 두께가 붙었는지는 <c>TextSizeGateTests</c> 의
    /// «[TextOutlineGate] 어긋난 글자 0» 이 전 화면에서 이미 지킨다(<see cref="TextAudit.OutlineStrict"/>).
    /// </summary>
    public class TextOutlineRuleTests
    {
        /// <summary>
        /// 레퍼런스에서 잰 제목 아웃라인 두께(프레임 1080폭 기준 px) — 이 띠 안이면 통과.
        /// <para>
        /// <b>회차 3 에서 다시 잡았다</b>(결정 522). 회차 1 의 «레퍼런스 3~4px × 1.5 = 4.5~7.5» 는 <b>규격을 그대로 화면 두께로 여긴</b> 값이라 낙관적이었다 —
        /// 실제로는 안티에일리어싱이 <b>거의 일정한 2.2 PNG px</b> 를 먹는다(CI 로그 «아웃라인 4.8px · lossyScale 0.21 → 화면 1.0px» + `screens` run 391 실측: 우리 띠 3.4% ↔ 레퍼런스 8.3~11.1% · 글자 높이 대비).
        /// 그 손실을 넣고 되풀면 레퍼런스 띠 2.25~3 PNG px 를 내는 규격은 <b>6.7~7.8 프레임px</b> 다.
        /// </para>
        /// </summary>
        const float RefTitleMin = 6.6f, RefTitleMax = 8.0f;

        [Test]
        public void TitleOutlineMatchesReferenceThickness()
        {
            float title = UiKit.OutlineWidth(TextSize.Title);
            Assert.GreaterOrEqual(title, RefTitleMin,
                $"제목({TextSize.Title}) 아웃라인 {title:0.00}px 이 레퍼런스 띠({RefTitleMin}~{RefTitleMax}px)보다 얇다 — " +
                "규격에서 안티에일리어싱이 «거의 일정한» 2.2 PNG px 를 먹으므로, 이 값이 낮으면 어둡게 찍히는 띠가 통째로 사라진다(T194 회차 3 · 결정 522)");
            Assert.LessOrEqual(title, RefTitleMax,
                $"제목({TextSize.Title}) 아웃라인 {title:0.00}px 이 레퍼런스 띠보다 두껍다 — 굵기는 목적이 아니라 레퍼런스를 맞추는 일이다(T194)");
        }

        /// <summary>
        /// 상한(<see cref="UiKit.OutlineMaxPx"/>)이 게임에서 제일 큰 글자를 «자르지» 않는가 —
        /// 상한이 비율보다 낮으면 비율을 올려도 큰 글자에서 뜻이 사라진다(T194 가 4px → 8px 로 올린 까닭).
        /// </summary>
        [Test]
        public void MaxDoesNotClampTheBiggestText()
        {
            float biggest = TextSize.Title * TextSize.BattleNumberMul;   // 전투 숫자 = 제일 큰 글자
            Assert.Greater(UiKit.OutlineMaxPx, biggest * UiKit.OutlineRatio - 0.001f,
                $"제일 큰 글자({biggest:0})의 비율 두께 {biggest * UiKit.OutlineRatio:0.00}px 이 상한 {UiKit.OutlineMaxPx}px 에 잘린다(T194)");
        }

        /// <summary>
        /// 회차 2 — <b>완전히 덮인 테 픽셀이 «검정» 까지 갈 수 있는가</b>(<see cref="UiKit.OutlineAlpha"/>).
        /// <para>
        /// 회차 1 은 띠를 두껍게 했는데 `screens` run 382 의 가장 어두운 픽셀이 <b>0.269</b> 였다(레퍼런스는 <b>0.000</b>).
        /// 두께가 아니라 <b>α 가 바닥을 만든다</b> — 밝은 판 위에서는 완전히 덮인 자리조차 <c>α·테 + (1-α)·판</c> 아래로 못 내려간다.
        /// 노란 리본(휘도 0.815)에서 α 0.85 의 바닥은 <b>0.182</b> 였다. 그 바닥이 판정선을 넘으면 «아무리 두껍게 해도 검정이 안 나온다» 가 된다.
        /// </para>
        /// 여기서 재는 것은 그 <b>바닥</b> 하나다(실제 픽셀은 덮인 정도까지 곱해지므로 언제나 이보다 밝다).
        /// </summary>
        [Test]
        public void FullyCoveredOutlineCanReachBlack()
        {
            const float RibbonLuma = 0.815f;   // 레퍼런스와 같은 노란 리본(실측 · 우리 17 PNG 도 같은 값)
            const float BlackLine = 0.20f;     // «검정» 판정선 — 레퍼런스 테의 속은 0.000 이다
            float a = UiKit.OutlineColor.a;
            float floor = a * UiKit.Luma(UiKit.OutlineColor) + (1f - a) * RibbonLuma;
            Assert.Less(floor, BlackLine,
                $"완전히 덮인 테 픽셀의 바닥이 {floor:0.000} 이라 밝은 판 위에서는 «검정» 이 안 나온다(α {a:0.00}) — " +
                "두께를 아무리 올려도 못 넘는 벽이다(T194 회차 2 · 주인 T63-outline «모든 글자들 다 검정 아웃라인»)");
        }

        /// <summary>
        /// 작은 글자는 하한(<see cref="UiKit.OutlineMinPx"/>)이 아니라 <b>비율</b>이 잡아야 한다 —
        /// 하한이 본문까지 덮으면 «크기에 비례하는 테» 라는 규칙 자체가 없어진다.
        /// </summary>
        [Test]
        public void RatioNotFloorDecidesBodyText()
        {
            Assert.Greater(UiKit.OutlineWidth(TextSize.Body), UiKit.OutlineMinPx,
                $"본문({TextSize.Body})이 하한 {UiKit.OutlineMinPx}px 에 걸려 있다 — 비율이 정해야 한다(T194)");
        }
    }
}
