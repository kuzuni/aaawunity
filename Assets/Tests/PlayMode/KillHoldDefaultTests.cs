using KkomaKnight.Game;
using NUnit.Framework;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T457 — 게임의 기본은 «킬 연출 중에도 엔진이 돈다»(주인 2026-09-12 «그 특전 팝업 뜨기 전에 전투나 이동은 바로 전까지 계속 됐어야 함» · «여전히 특전 뜨기 전에 계속 움직이는 거랑 되고 있게 하라니까 · 특전 딱 떴을 때 게임 정지되는 느낌으로» · «경험치 흡수하는 효과 나오면서부터 이미 이동하는 거랑 공격 멈추고 있잖아»).
    /// T50 의 킬 보류는 <see cref="BattleWorld.HoldEngineOnKill"/> 이 참일 때만 살아나고, 그것을 켜는 것은 그 꼴을 재는 자(<c>BattleWorldTests</c>)뿐이다.
    /// 이 자가 없으면 누군가 «연출이 어긋난다» 며 기본을 다시 켜도 아무 자도 안 운다.
    /// </summary>
    public class KillHoldDefaultTests
    {
        [Test]
        public void 게임_기본은_킬_연출_중에도_엔진이_돈다()
        {
            Assert.IsFalse(BattleWorld.HoldEngineOnKill, "T457 — 주인 «특전 딱 떴을 때 게임 정지되는 느낌으로»: 킬 보류(T50)는 기본 꺼짐이다. 켜는 것은 옛 꼴을 재는 자뿐.");
        }
    }
}
