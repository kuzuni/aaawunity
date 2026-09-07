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
    /// «휘도 0.30 미만이 이어지는 길이» 를 세면 최빈 <b>3px</b> · 중앙값 <b>4px</b> 다. 우리 프레임은 1080폭이므로 ×1.5 하면 <b>4.5~6px</b> 이고,
    /// 제목 크기 60 에서 그 두께가 나오려면 비율이 <b>0.075~0.10</b> 이어야 한다. 종전 0.05 는 3px 이라 띠의 절반에도 못 미쳤다.
    /// </para>
    /// 씬을 안 띄운다 — 규칙(상수와 식)만 본다. 실제 글자마다 그 두께가 붙었는지는 <c>TextSizeGateTests</c> 의
    /// «[TextOutlineGate] 어긋난 글자 0» 이 전 화면에서 이미 지킨다(<see cref="TextAudit.OutlineStrict"/>).
    /// </summary>
    public class TextOutlineRuleTests
    {
        /// <summary>레퍼런스에서 잰 제목 아웃라인 두께(프레임 1080폭 기준 px) — 이 띠 안이면 통과.</summary>
        const float RefTitleMin = 4.5f, RefTitleMax = 7.5f;

        [Test]
        public void TitleOutlineMatchesReferenceThickness()
        {
            float title = UiKit.OutlineWidth(TextSize.Title);
            Assert.GreaterOrEqual(title, RefTitleMin,
                $"제목({TextSize.Title}) 아웃라인 {title:0.00}px 이 레퍼런스 띠({RefTitleMin}~{RefTitleMax}px)보다 얇다 — " +
                "촬영은 1080 프레임을 540 으로 그리므로 화면에서는 이 값의 절반이고, 2px 밑이면 글자 자신의 안티에일리어싱이 띠를 다 덮어 회색 그림자로만 남는다(T194)");
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
