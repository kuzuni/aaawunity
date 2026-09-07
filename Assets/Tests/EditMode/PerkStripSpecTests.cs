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
            // 1080×2337 프레임에서 HudPerkStrip(T142 주인 값 = 80.4% × 4.186%) 의 실제 크기
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
            // 종전 상수(78×84 셀 · 간격 8 · 최대 11개 = 938px) 가 줄 폭을 넘쳤던 것과 달리, 셀은 «줄 높이의 28/34» 로 따라간다.
            // 픽셀 숫자를 박지 않고 줄 높이에서 뽑는다 — 줄 높이는 주인이 인스펙터로 바꿀 수 있다(T142 에서 4.0% → 4.186% 로 바뀌었다).
            float rowPx = 2337f * Layout.HudPerkStrip.H / 100f;
            Assert.That(m.Cell, Is.EqualTo(rowPx * 28f / 34f).Within(0.5f));
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
            Assert.That(fit, Is.GreaterThanOrEqualTo(8).And.LessThanOrEqualTo(11), "868px 줄에 81px 셀+12px 간격 → 9개(T142 값)");
            Assert.That(m.Shown(fit), Is.EqualTo(fit), "딱 들어가면 «+N» 없이 전부");
            Assert.That(m.Shown(fit + 1), Is.LessThan(fit + 1), "하나 더 생기면 «+N» 으로 접힌다");
            Assert.That(m.Shown(fit + 1), Is.GreaterThanOrEqualTo(fit - 2), "«+N» 칸 하나 때문에 셀이 두 개 넘게 빠지진 않는다");
            Assert.That(m.Shown(12), Is.GreaterThan(0)); Assert.That(m.Shown(0), Is.EqualTo(0)); Assert.That(m.Shown(1), Is.EqualTo(1));
        }

        /// <summary>
        /// T142 — 주인이 인스펙터로 준 두 사각형(<see cref="Layout.HudPerkStrip"/> 줄 · <see cref="Layout.HudInfo"/> 책 버튼)이
        /// 프레임(0~100%) 안에 들고, 화면 바닥에 붙고, 서로 안 겹치는가. 값이 흔들리면 여기서 바로 걸린다.
        /// </summary>
        [Test]
        public void OwnerGivenBottomRectsFitTheFrameAndDoNotOverlap()
        {
            var strip = Layout.HudPerkStrip; var book = Layout.HudInfo;
            foreach (var r in new[] { strip, book })
            {
                Assert.That(r.X, Is.GreaterThanOrEqualTo(0f)); Assert.That(r.Y, Is.GreaterThanOrEqualTo(0f));
                Assert.That(r.X + r.W, Is.LessThanOrEqualTo(100.01f), "가로가 프레임을 안 넘는다");
                Assert.That(r.Y + r.H, Is.LessThanOrEqualTo(100.01f), "세로가 프레임을 안 넘는다");
            }
            Assert.That(strip.Y + strip.H, Is.EqualTo(100f).Within(0.05f), "줄은 화면 바닥에 붙는다(주인 앵커 Max.y = 0.04186 · Min.y = 0)");
            Assert.That(book.Y + book.H, Is.EqualTo(100f).Within(0.05f), "책 버튼도 바닥에 붙는다");
            Assert.That(book.X + book.W, Is.EqualTo(100f).Within(0.05f), "책 버튼은 오른쪽 끝까지 간다(Max.x = 1)");
            Assert.That(strip.X + strip.W, Is.LessThanOrEqualTo(book.X + 0.01f), "줄 오른쪽 끝(83.4) ≤ 책 왼쪽(84.0) — 둘은 안 겹친다");
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
