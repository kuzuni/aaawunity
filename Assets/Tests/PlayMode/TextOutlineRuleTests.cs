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
        /// <b>T224 에서 두 벽을 다시 쟀다 — 그 전 값(0.35 · 0.05)은 «단위를 모르고» 찍은 것이었다.</b>
        /// <para>
        /// 이 자를 T221 에 세울 때 나는 <c>_OutlineWidth</c> 가 «0~1 이니 0.5 면 획을 절반 파고들겠지» 라고 <b>어림했다</b>.
        /// T224 가 그 단위를 실제로 알아냈다: <c>TMP_FontAsset.CreateFontAsset(Font)</c> 기본은 표본 90pt · 여백 9 이고
        /// TMP 는 <c>_OutlineWidth</c> <b>1.0 을 em 의 10%</b>(= 여백/표본)로 매핑한다 ⇒ <b>옛 uGUI 비율 r ↔ <c>_OutlineWidth</c> = r × 10</b>.
        /// 그 환산으로 T204 의 실측을 그대로 옮기면 두 벽이 열 배 위에 있다 — <b>옛 띠는 «두꺼워서 위험한» 자리를 하나도 안 덮고 있었고,
        /// 도리어 T224 의 고침값(0.70)을 빨강으로 막아 배포를 세울 뻔했다</b>(이 자가 이 회차의 진짜 위험이었다).
        /// </para>
        /// <b>⛔ 그리고 그 «환산» 도 반쪽이었다 — 회차 2 가 실측으로 잡았다(run 487).</b> 옛 uGUI 테는 글자 <b>밖</b>으로 나갔는데
        /// TMP SDF 테는 모서리에 <b>걸쳐</b>(반은 안) 그린다 — 그래서 0.70 은 «옛 두께» 가 아니라 <b>획을 반이나 파먹는 값</b>이었고
        /// `01_lobby` «챕터 1» 의 밝은 획이 250 → 2 픽셀로 사라졌다. 고침은 <see cref="TmpFont.FaceDilate"/> 와 <b>짝짓는 것</b>이고,
        /// 짝지으면 보이는 검은 띠는 «바깥 한 겹» 이라 눈금이 <b>절반</b>이 된다 ⇒ 두 벽도 그 눈금으로 다시 옮긴다.
        /// <para>
        /// <b>위 벽 0.60</b> — T204 실측 «0.07 × 크기 가 상한 · 0.12 는 ㅇ·ㅂ·8·0 속을 메운다» 를 «바깥 한 겹» 눈금으로 옮긴 자리다.
        /// (이 벽이 지키는 것은 «속 구멍» 하나뿐이다 — «획을 파먹는가» 는 이제 값이 아니라 <b>짝</b>이 지킨다: 아래 <see cref="FaceDilateIsPairedWithOutlineWidth"/>.)
        /// </para>
        /// </summary>
        const float CountersDieAbove = 0.60f;
        /// <summary>
        /// «있으나 마나» 한 선 — <b>주인이 «검은 아웃라인 없던데» 라고 한 그 자리가 정확히 이 밑이다</b>(옛 0.20 = em 의 2%).
        /// <para>
        /// 셈으로 벽을 둔다: 본문 크기 40 에서 <c>screens</c>(540폭 = 프레임 절반)에 <b>적어도 1px</b> 은 남아야 눈에 걸린다.
        /// 짝지은 뒤의 «바깥 한 겹» 눈금으로 40 × (w/10) × 0.5 ≥ 1 ⇒ <b>w ≥ 0.50</b> 인데, 그 벽은 <b>고침값 자신</b>이라
        /// 다음 회차가 실측으로 한 눈금 내리면 그대로 빨강이 된다 — 벽은 «틀림없이 안 보이는» 자리에 둔다: <b>0.25</b>(= 그 절반 · 0.5px).
        /// 그 밑은 안티에일리어싱에 먹혀 «회색 그림자» 조차 아니고 <b>없는 것과 같다</b>(T194 가 «얇다» 로 부른 자리 · T224 가 픽셀로 확인했다:
        /// 0.20 은 본문에서 0.40px 이었고 주인이 «검은 아웃라인 없던데» 라고 했다).
        /// </para>
        /// </summary>
        const float TooThinAtOrBelow = 0.25f;

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
        /// <b>T224 회차 2 — 테 두께와 낯 부풀리기는 «짝» 이다. 하나만 올리면 테가 글자를 파먹는다.</b>
        /// <para>
        /// 이 계약은 <b>어림이 아니라 셰이더에 적혀 있다</b> — <c>TMP_SDF-Mobile.shader</c> 는 낯 갈래를 <c>bias + outline</c> 부터 그리고
        /// (155·192줄) <c>_FaceDilate</c> 는 같은 배율로 <c>weight</c> 를 민다(149줄) ⇒ <b>둘이 같을 때만</b> 원래 글자 굵기가 남고
        /// 검은 띠가 통째로 바깥으로 간다. 짝이 어긋나면 그 차이만큼 흰 낯이 깎인다.
        /// </para>
        /// <b>그리고 그것이 «가정» 이 아닌 까닭</b>: 회차 1 이 두께만 0.70 으로 올려 내보냈고, <c>screens</c> run 487 에서
        /// `01_lobby` «챕터 1» 의 밝은 획이 <b>250 → 2 픽셀</b>로 사라졌다(72pt «START» 조차 검은 덩어리였다). 그 그림을 다시 못 만들게 막는 자다.
        /// </summary>
        [Test]
        public void FaceDilateIsPairedWithOutlineWidth()
        {
            Assert.AreEqual(TmpFont.OutlineWidth, TmpFont.FaceDilate, 1e-3f,
                $"낯 부풀리기 {TmpFont.FaceDilate:0.000} 가 테 두께 {TmpFont.OutlineWidth:0.000} 와 다르다 — " +
                "TMP 테는 모서리에 «걸쳐» 그려서 반은 글자를 파먹는다. 짝이 어긋난 만큼 흰 획이 깎이고, " +
                "작은 글자부터 검은 덩어리가 된다(T224 회차 1 이 run 487 에서 실제로 그렇게 나갔다)");

            var asset = TmpFont.Get();
            if (asset == null || asset.material == null) return;   // 픽셀·머티리얼 판정은 TmpFontProbeTests 몫
            var mat = asset.material;
            if (!mat.HasProperty(TmpFont.FaceDilateProp)) return;  // 셰이더 갈래가 다른 자리 — 값 계약은 위에서 이미 봤다
            Assert.AreEqual(TmpFont.FaceDilate, mat.GetFloat(TmpFont.FaceDilateProp), 1e-3f,
                $"머티리얼의 {TmpFont.FaceDilateProp} 가 규격과 다르다 — {nameof(TmpFont)}.{nameof(TmpFont.SetOutline)} 이 둘을 같이 넣어야 한다");
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
