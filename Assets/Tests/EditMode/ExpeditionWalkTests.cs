using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T394 — 탐험 팝업 «걷는 배경» 의 속도 셈(<see cref="Expedition.WalkSpeedUi"/>)이 전투 걷기 속도와 같은 표 값에서 나온다.
    /// 화면(uvRect 가 실제로 움직인다 · 캐릭터가 걷기 상태)은 PlayMode <c>ExpeditionWalkPlayTests</c> 가 본다.
    /// </summary>
    public class ExpeditionWalkTests
    {
        [Test]
        public void 걷기_속도는_엔진_속도_x_줌을_프레임_px_로_옮긴_값이다()
        {
            // combat.json playerSpeed 132 · ui.json zoom 1.5 · 레이아웃 540 → 프레임 1080: 132 × 1.5 × 2 = 396 px/초
            Assert.AreEqual(396.0, Expedition.WalkSpeedUi(132, 1.5, 1080, 540), 1e-9);
            Assert.AreEqual(0.0, Expedition.WalkSpeedUi(132, 1.5, 1080, 0), "레이아웃 폭 0 이면 0(나누기 방어)");
            Assert.AreEqual(0.0, Expedition.WalkSpeedUi(0, 1.5, 1080, 540), "속도 0 이면 안 흐른다");
        }
    }
}
