using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// 검은 아웃라인 «규칙» 을 지키는 자(T63-outline · 주인 «모든 글자들 다 검정 아웃라인»).
    /// <para>
    /// <b>T221 에서 재는 대상을 갈아 끼웠다.</b> T194·T204 시절 이 자는 <c>UiKit.OutlineRatio</c>(글자 크기 × 비율 = 프레임px)를 지켰는데,
    /// <b>T207 ② 가 테를 SDF 머티리얼로 옮긴 뒤 그 상수는 아무 픽셀도 안 그린다</b>(주인 «tmpro로 아웃라인 해야지 진짜 메테리얼로»).
    /// 그런데도 자는 초록이었다 — <b>그리지 않는 손잡이를 지키는 초록 게이트</b>라, 다음에 주인이 «테가 이상하다» 고 할 때
    /// 이 자의 실패 문구가 엉뚱한 값을 만지라고 시킨다. 그래서 그 상수를 걷고 <b>살아 있는 손잡이 하나</b>(<see cref="TmpFont.OutlineWidth"/>)를 지킨다.
    /// </para>
    /// <para>
    /// <b>SDF 두께는 «비율»(0~1)이라 글자 크기·부모 스케일에 저절로 비례한다</b> — T194 가 세 회차를 태운
    /// «규격 px 이 <c>lossyScale</c> 을 지나 화면에서는 몇 px 인가» 라는 물음 자체가 이 방식에는 없다(결정 522 가 그 함정이다).
    /// 그래서 여기서 재는 것은 «프레임px 띠» 가 아니라 <b>비율의 위·아래 벽</b> 둘이다.
    /// </para>
    /// 씬을 안 띄운다 — 규칙(상수와 식)만 본다. 글자마다 그 테가 실제로 걸렸는지는 <c>TextSizeGateTests</c> 의
    /// «[TextOutlineGate] 어긋난 글자 0» 이 전 화면에서 지킨다(<see cref="TextAudit.OutlineStrict"/>).
    /// </summary>
    public class TextOutlineRuleTests
    {
        /// <summary>
        /// TMP SDF 아웃라인이 «글자를 먹기» 시작하는 선 — `_OutlineWidth` 는 0~1 이고 0.5 에서 획을 절반까지 파고든다.
        /// 여기를 넘으면 ㅇ·ㅂ·8·0 의 속 구멍이 메워져 T204 가 되돌린 그 그림이 된다.
        /// </summary>
        const float CountersDieAbove = 0.35f;
        /// <summary>«있으나 마나» 한 선 — 이 밑이면 540폭 캡처에서 회색 그림자로만 남는다(T194 가 «얇다» 로 부른 자리).</summary>
        const float TooThinAtOrBelow = 0.05f;

        /// <summary>
        /// 테 두께가 «글자를 먹지 않으면서 보이는» 띠 안인가 — 이 자가 지키는 **유일한** 두께 손잡이다(T221).
        /// </summary>
        [Test]
        public void OutlineWidthStaysInTheBandThatShowsWithoutEatingGlyphs()
        {
            Assert.Less(TmpFont.OutlineWidth, CountersDieAbove,
                $"SDF 테 두께 {TmpFont.OutlineWidth:0.000} 이 {CountersDieAbove} 이상이다 — 획을 파고들어 ㅇ·ㅂ·8·0 의 속 구멍이 메워진다(T204 가 되돌린 그림)");
            Assert.Greater(TmpFont.OutlineWidth, TooThinAtOrBelow,
                $"SDF 테 두께 {TmpFont.OutlineWidth:0.000} 이 {TooThinAtOrBelow} 이하다 — 주인이 «얇다» 고 한 자리로 되돌아간다(T194)");
            Debug.Log($"[T221] SDF 테 두께 {TmpFont.OutlineWidth:0.000}(비율 · 크기에 저절로 비례) · 색 {UiKit.OutlineColor} · " +
                      "옛 프레임px 규칙(UiKit.OutlineRatio 등)은 T207 ② 뒤 아무것도 안 그려서 걷었다");
        }

        /// <summary>
        /// 색·α 는 «검정 아웃라인» 그대로인가 — 이쪽은 T207 뒤에도 <b>살아 있는</b> 값이다(<see cref="TmpFont.SetOutline"/> 로 들어간다).
        /// </summary>
        [Test]
        public void OutlineColorIsStillOpaqueBlack()
        {
            Assert.AreEqual(1f, UiKit.OutlineColor.a, 1e-3f,
                "테 α 는 1 이어야 한다 — 0.85 이면 밝은 판 위에서 «완전히 덮인» 픽셀조차 검정에 못 닿는다(T194 회차 2 의 셈 · 결정 507)");
            Assert.Less(UiKit.Luma(UiKit.OutlineColor), 0.15f,
                $"테 색 휘도 {UiKit.Luma(UiKit.OutlineColor):0.000} — 주인 지시는 «모든 글자들 다 검정 아웃라인» 이다(T63-outline)");
        }

        /// <summary>
        /// <b>T224 2항 — 이 자가 «값» 만 보고 «그리나» 를 못 보던 것을 메운다.</b>
        /// <para>
        /// 주인이 T207 뒤에 «검은 아웃라인 없던데 tmpro들» 이라고 했을 때, 이 파일을 포함한 자 셋은 전부 <b>초록</b>이었다 —
        /// 셋 다 <c>_OutlineWidth == 0.20</c> 만 봤기 때문이다. TMP SDF 셰이더는 아웃라인을 <c>OUTLINE_ON</c> 갈래로 가르므로
        /// <b>값이 들어가 있어도 갈래가 꺼져 있으면 한 픽셀도 안 그린다</b>. 그래서 «두께 &gt; 0» 이 아니라 «그리는 상태» 를 묻는다.
        /// </para>
        /// 픽셀로 재는 진짜 판정은 <c>TmpFontProbeTests</c> 의 «흰 판 위 <b>흰</b> 글자» 촬영이다(같은 회차에 세웠다) —
        /// 여기 이 줄은 그 판정이 다시 무너지지 않게 <b>상태</b>를 못 박는 자다.
        /// </summary>
        [Test]
        public void OutlineIsInADrawingStateNotJustAValue()
        {
            var asset = TmpFont.Get();
            if (asset == null || asset.material == null)
            {
                Assert.Ignore("폰트 애셋이 없는 자리 — 픽셀 판정은 TmpFontProbeTests 가 한다(여기는 상수·상태만 본다)");
                return;
            }
            Assert.IsTrue(TmpFont.OutlineDraws(asset.material),
                $"머티리얼이 «테를 그리는 상태» 가 아니다 — 두께 {asset.material.GetFloat(TmpFont.OutlineWidthProp):0.00} · " +
                $"갈래 {TmpFont.OutlineKeyword} 꺼짐. 값만 넣고 갈래를 안 켜면 한 픽셀도 안 그린다(T224 2항 · 주인 «검은 아웃라인 없던데»)");
        }

        /// <summary>
        /// 옛 «프레임px 두께» 규칙이 되살아나지 않았는가 — 되살리면 T194 의 세 회차를 그대로 다시 태운다.
        /// <para>
        /// SDF 두께는 비율이라 크기·스케일에 저절로 비례한다. 누군가 «크기 × 비율 = px» 식을 다시 들여오면
        /// <c>lossyScale</c> 을 지나며 화면 두께가 달라지는 그 함정이 돌아온다(결정 522).
        /// </para>
        /// </summary>
        [Test]
        public void ThicknessRuleIsRatioOnlyNotPixels()
        {
            Assert.Greater(TmpFont.OutlineWidth, 0f, "SDF 두께는 비율(0~1)이다");
            Assert.LessOrEqual(TmpFont.OutlineWidth, 1f,
                $"SDF `_OutlineWidth` 는 0~1 이다 — {TmpFont.OutlineWidth} 는 «픽셀» 을 적은 값으로 보인다(T221 · 결정 522 의 함정)");
        }
    }
}
