using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T13 — 전투 HUD «얻은 특전 미리보기 줄» 치수(<see cref="Layout.PerkStripSpec"/>)가 index.html CSS 비례(34/28/4/14/7/12)를 따르고,
    /// 표시 개수가 줄 폭에서 계산돼 «+N» 까지 포함해도 절대 넘치지 않는가. (프리팹·화면은 PlayMode PerkStripTests 가 실물 rect 로 다시 검사한다.)
    /// </summary>
    public class PerkStripSpecTests
    {
        static Layout.PerkStripSpec RefFrame()
        {
            // 1080×2337 프레임에서 HudPerkStrip(80% × 4.0%) 의 실제 크기
            return new Layout.PerkStripSpec(1080f * Layout.HudPerkStrip.W / 100f, 2337f * Layout.HudPerkStrip.H / 100f);
        }

        [Test]
        public void ProportionsFollowReferenceCss()
        {
            var m = RefFrame();
            Assert.That(m.Cell / m.Height, Is.EqualTo(28f / 34f).Within(1e-4), "셀 한 변 = 줄 높이의 28/34");
            Assert.That(m.Gap / m.Height, Is.EqualTo(4f / 34f).Within(1e-4), "간격 = 4/34");
            Assert.That(m.Badge / m.Height, Is.EqualTo(14f / 34f).Within(1e-4), "개수 배지 = 14/34");
            Assert.That(m.Pad / m.Height, Is.EqualTo(7f / 34f).Within(1e-4), "«+N» 안쪽 여백 = 7/34");
            Assert.That(m.Font, Is.EqualTo(System.Math.Max(TextSize.Aux, System.Math.Round(m.Height * 12f / 34f))).Within(0.5), "«+N» 글자 = 12/34 · 보조 하한(36) 이상(T63)");
            Assert.That(m.Cell, Is.LessThan(m.Height), "셀은 줄보다 낮다(세로로 안 넘침)");
            // 종전 상수(78×84 셀 · 간격 8 · 최대 11개 = 938px) 가 864px 줄을 넘쳤던 것과 달리, 셀은 줄 높이(≈93px)의 82% ≈ 77px
            Assert.That(m.Cell, Is.EqualTo(93.48f * 28f / 34f).Within(0.5f));
        }

        [Test]
        public void ScalesWithRowHeightNotPixels()
        {
            var a = new Layout.PerkStripSpec(864f, 93.48f);
            var b = new Layout.PerkStripSpec(432f, 46.74f);   // 절반 해상도
            Assert.That(b.Cell, Is.EqualTo(a.Cell / 2f).Within(1e-3));
            Assert.That(b.Gap, Is.EqualTo(a.Gap / 2f).Within(1e-3));
            Assert.That(b.Badge, Is.EqualTo(a.Badge / 2f).Within(1e-3));
            var css = new Layout.PerkStripSpec(390f * 0.8f, 34f);   // 레퍼런스 프레임 자체
            Assert.That(css.Cell, Is.EqualTo(28f).Within(1e-4)); Assert.That(css.Gap, Is.EqualTo(4f).Within(1e-4)); Assert.That(css.Badge, Is.EqualTo(14f).Within(1e-4)); Assert.That(css.Font, Is.EqualTo(System.Math.Max(12f, (float)TextSize.Aux)).Within(1e-4), "글자만은 보조 하한(T63)이 비례를 이긴다");
        }

        [Test]
        public void NeverOverflowsForAnyCount()
        {
            var m = RefFrame();
            for (int total = 0; total <= 100; total++)
            {
                int shown = m.Shown(total);
                Assert.That(shown, Is.LessThanOrEqualTo(total));
                Assert.That(m.UsedWidth(total), Is.LessThanOrEqualTo(m.Width + 0.02f), $"total={total} shown={shown} 가 줄 폭을 넘친다");
                if (shown < total) Assert.That(shown, Is.LessThanOrEqualTo(total - 1), "«+N» 이 있으면 최소 1개는 숨겨져 있어야 한다");
            }
        }

        [Test]
        public void ShowsAllWhenTheyFitAndCollapsesWhenNot()
        {
            var m = RefFrame();
            int fit = m.Fit;
            Assert.That(fit, Is.GreaterThanOrEqualTo(8).And.LessThanOrEqualTo(11), "864px 줄에 77px 셀+11px 간격 → 9개");
            Assert.That(m.Shown(fit), Is.EqualTo(fit), "딱 들어가면 «+N» 없이 전부");
            Assert.That(m.Shown(fit + 1), Is.LessThan(fit + 1), "하나 더 생기면 «+N» 으로 접힌다");
            Assert.That(m.Shown(fit + 1), Is.GreaterThanOrEqualTo(fit - 2), "«+N» 칸 하나 때문에 셀이 두 개 넘게 빠지진 않는다");
            Assert.That(m.Shown(12), Is.GreaterThan(0)); Assert.That(m.Shown(0), Is.EqualTo(0)); Assert.That(m.Shown(1), Is.EqualTo(1));
        }

        [Test]
        public void TinyRowDegradesSafely()
        {
            var m = new Layout.PerkStripSpec(0f, 0f);
            Assert.That(m.Fit, Is.EqualTo(0)); Assert.That(m.Shown(5), Is.EqualTo(0)); Assert.That(m.Font, Is.GreaterThanOrEqualTo(8f));
            var narrow = new Layout.PerkStripSpec(60f, 93.48f);   // 셀 하나도 안 들어가는 폭
            Assert.That(narrow.Shown(3), Is.EqualTo(0)); Assert.That(narrow.UsedWidth(3), Is.LessThanOrEqualTo(narrow.Width + narrow.MoreWidth(3)));
        }
    }
}
