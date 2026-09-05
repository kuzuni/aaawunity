using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T14 — 전투 캐릭터 크기 2/3 배율 · 공격 애니 속도 = 클립 길이 ÷ 간격(하한 1 · 상한 없음). 순수 계산(Core)이라 dotnet 으로 돈다.
    /// 표 상수(PlayerHeight·EnemyHeight)는 LayoutSpecTests 가 ref-layout 표와 대조하므로 여기서는 «배율이 따로 곱해진다» 만 본다.
    /// </summary>
    public class CharScaleAnimTests
    {
        const float AttackClip = 1.8333334f;   // CharacterMaker Attack.anim m_StopTime (조사값)

        [Test]
        public void CharScaleIsTwoThirdsAndTableConstantsUntouched()
        {
            Assert.That(Layout.CharScale, Is.EqualTo(2f / 3f).Within(1e-6f));
            Assert.That(Layout.PlayerHeight, Is.EqualTo(9.0f).Within(1e-6f), "표 상수는 그대로(LayoutSpecTests 대조)");
            Assert.That(Layout.EnemyHeight, Is.EqualTo(9.0f).Within(1e-6f));
            Assert.That(Layout.CharHeightPct(Layout.PlayerHeight), Is.EqualTo(6.0f).Within(1e-5f), "플레이어 실제 키 = 9% × 2/3");
            Assert.That(Layout.CharHeightPct(Layout.EnemyHeight * 1.5f), Is.EqualTo(9.0f).Within(1e-5f), "보스(×sizeMul)도 같은 배율");
        }

        [Test]
        public void AttackAnimSpeedFitsClipIntoInterval()
        {
            // 간격이 클립보다 길면 속도 1(느리게 돌리지 않는다)
            Assert.That(Layout.AttackAnimSpeed(AttackClip, 5.0), Is.EqualTo(1f));
            Assert.That(Layout.AttackAnimSpeed(AttackClip, AttackClip), Is.EqualTo(1f).Within(1e-5f));
            // 간격이 짧으면 «간격 안에 끝나도록» = 클립/간격
            Assert.That(Layout.AttackAnimSpeed(AttackClip, 1.0), Is.EqualTo(AttackClip).Within(1e-5f));
            Assert.That(Layout.AttackAnimSpeed(AttackClip, 0.5), Is.EqualTo(AttackClip / 0.5f).Within(1e-4f));
            foreach (var iv in new[] { 2.0, 1.0, 0.61, 0.3, 0.1, 0.05 })
            {
                float s = Layout.AttackAnimSpeed(AttackClip, iv);
                Assert.That(AttackClip / s, Is.LessThanOrEqualTo(iv + 1e-5), $"간격 {iv}s 안에 클립이 끝나야 한다");
            }
        }

        [Test]
        public void AttackAnimSpeedHasNoUpperCap()
        {
            // 예전 상한 ×3 폐기 — 공속이 빠르면(간격 0.3s) 6배 이상
            Assert.That(Layout.AttackAnimSpeed(AttackClip, 0.3), Is.GreaterThan(3f));
            Assert.That(Layout.AttackAnimSpeed(AttackClip, 0.3), Is.EqualTo(AttackClip / 0.3f).Within(1e-4f));
            Assert.That(Layout.AttackAnimSpeed(AttackClip, 0.05), Is.GreaterThan(30f));
        }

        [Test]
        public void AttackAnimSpeedIsSafeOnDegenerateInputs()
        {
            Assert.That(Layout.AttackAnimSpeed(0f, 1.0), Is.EqualTo(1f), "클립 길이를 못 읽으면 1");
            Assert.That(float.IsInfinity(Layout.AttackAnimSpeed(AttackClip, 0.0)), Is.False, "간격 0 에도 유한");
            Assert.That(float.IsNaN(Layout.AttackAnimSpeed(AttackClip, -1.0)), Is.False);
            Assert.That(Layout.AttackAnimSpeed(AttackClip, -1.0), Is.GreaterThanOrEqualTo(1f));
        }
    }
}
