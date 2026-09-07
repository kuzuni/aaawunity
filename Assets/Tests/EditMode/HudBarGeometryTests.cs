using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T143 — 전투(02) 하단 줄의 «보이는 자리» 산술(주인 2026-09-07 05:3X «스탯이 오른쪽으로 치우쳤다 · 바가 왼쪽으로 치우쳤다 · 바 사이에 틈이 없다»).
    /// <para>
    /// 바는 왼쪽 캡(«EXP» 라벨 · ❤ · 🛡)이 rect <b>밖으로</b> 튀어나오므로(<see cref="Layout.HudBarCapExp"/>·<see cref="Layout.HudBarCapIcon"/>)
    /// «바 사각형» 이 아니라 <b>캡을 포함한 보이는 상자</b>로 재야 눈에 보이는 대칭·틈이 맞는다. 화면 코드가 아니라 상수만 보는 산술이라
    /// 유니티 없이도 돌고, 값이 흔들리면 그 커밋에서 바로 걸린다(실물 rect 는 PlayMode <c>HudBarsTests</c> 가 따로 본다).
    /// </para>
    /// </summary>
    public class HudBarGeometryTests
    {
        /// <summary>바의 «보이는» 왼쪽 끝 = rect.X − 캡이 튀어나온 폭.</summary>
        static float VisibleLeft(Layout.R bar, float cap) => bar.X - cap;
        static float Right(Layout.R bar) => bar.X + bar.W;

        [Test]
        public void ThreeBarsAreCenteredWithVisibleGaps()
        {
            float expL = VisibleLeft(Layout.HudExp, Layout.HudBarCapExp);
            float hpL = VisibleLeft(Layout.HudHp, Layout.HudBarCapIcon);
            float shL = VisibleLeft(Layout.HudSh, Layout.HudBarCapIcon);

            Assert.That(expL, Is.GreaterThanOrEqualTo(1.0f), "보이는 왼쪽 끝이 화면 밖으로 나가면 안 된다(고치기 전 −0.52)");
            Assert.That(expL, Is.EqualTo(100f - Right(Layout.HudSh)).Within(0.15f), "왼쪽 여백 = 오른쪽 여백(줄 전체가 가운데)");
            Assert.That(hpL - Right(Layout.HudExp), Is.GreaterThanOrEqualTo(1.0f), "EXP ↔ HP 사이에 눈에 보이는 틈(고치기 전 −1.33 = 겹침)");
            Assert.That(shL - Right(Layout.HudHp), Is.GreaterThanOrEqualTo(1.0f), "HP ↔ 실드 사이에도 같은 틈");
            Assert.That(shL - Right(Layout.HudHp), Is.EqualTo(hpL - Right(Layout.HudExp)).Within(0.05f), "두 틈은 같다");
            Assert.That(Right(Layout.HudSh), Is.LessThanOrEqualTo(99.0f), "오른쪽 끝이 화면 안");

            // 폭 비율 26 : 32 : 32 는 유지한다(주인이 바꾸라고 한 것은 «자리» 지 «어느 바가 더 긴가» 가 아니다)
            Assert.That(Layout.HudHp.W, Is.EqualTo(Layout.HudSh.W).Within(0.01f), "HP·실드는 같은 폭");
            Assert.That(Layout.HudExp.W / Layout.HudHp.W, Is.EqualTo(26f / 32f).Within(0.02f), "EXP : HP 폭 비율 26:32");
            foreach (var b in new[] { Layout.HudExp, Layout.HudHp, Layout.HudSh })
                Assert.That(b.Y, Is.EqualTo(Layout.HudExp.Y).Within(1e-3f), "세 바는 같은 줄·같은 두께");
        }

        /// <summary>
        /// T154 — 특전 팝업의 «책» 버튼은 <b>정사각</b>이어야 한다(주인 «책 아이콘 찌그러져 있더라»).
        /// %p 는 프레임(1080×2337)에서 가로·세로 길이가 다르므로 «폭 % × 1080 ≈ 높이 % × 2337» 이 되어야 눈에 정사각이다.
        /// 표 ⑦ 의 비고가 이미 «작은 정사각» 이라 이 산술이 그 비고를 지키는 자다.
        /// </summary>
        [Test]
        public void PerkBookButtonIsSquareOnScreen()
        {
            const float frameW = 1080f, frameH = 2337f;
            float wPx = Layout.OvInfo.W / 100f * frameW;
            float hPx = Layout.OvInfo.H / 100f * frameH;
            Assert.That(wPx / hPx, Is.EqualTo(1f).Within(0.05f), "책 버튼 가로:세로 = 1 ± 0.05(고치기 전 0.69 = 세로 1.44배)");
        }

        [Test]
        public void StatGridColumnsAreSymmetric()
        {
            // 화면 코드(BattleScreen)가 칸을 잡는 식 그대로: x = HudStats.X + col × HudStatColR + 0.4 · 폭 = HudStatCellW − 0.8
            float cellW = Layout.HudStatCellW - 0.8f;
            float leftX = Layout.HudStats.X + 0.4f;
            float rightX = Layout.HudStats.X + Layout.HudStatColR + 0.4f;
            float leftMargin = leftX;
            float rightMargin = 100f - (rightX + cellW);

            Assert.That(rightMargin, Is.EqualTo(leftMargin).Within(0.5f), "스탯 8칸의 좌우 여백이 같아야 한다(고치기 전 3.4 / 0.4 = 3%p 치우침)");
            Assert.That(rightX, Is.GreaterThanOrEqualTo(leftX + cellW), "두 열이 겹치지 않는다");
            Assert.That(Layout.HudStatColR, Is.EqualTo(Layout.HudStats.W / 2f).Within(0.01f), "열 피치 = 그리드 제 폭의 절반(화면의 절반이 아니다)");
            Assert.That(rightX + cellW, Is.LessThanOrEqualTo(100f), "오른쪽 열이 화면을 안 넘는다");
        }
    }
}
