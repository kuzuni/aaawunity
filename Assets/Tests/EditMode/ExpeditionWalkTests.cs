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

        // 화면(LobbyPopups.ExProps)과 같은 값 — 나무 넷(6·40·74·108 · 폭 16) · 한 바퀴 136 · 여유 16
        static readonly double[] TreeX = { 6, 40, 74, 108 };
        const double Span = 136, Margin = 16, W = 16;

        /// <summary>
        /// T394 2회차(주인 «나무가 생성되는 장면들 보이는데 그러면 안 됨») — 한 바퀴를 촘촘히 밟아, 자리가 튀는 프레임이 있으면 그 전후가 모두 띠 밖(왼쪽으로 다 나갔다 → 오른쪽 밖에서 태어난다)이어야 한다.
        /// 1회차 값(나무 셋 · 한 바퀴 102)은 86% 에서 태어나 이 자가 빨갛다 — 그것이 주인이 본 장면이다.
        /// </summary>
        [Test]
        public void 나무는_띠_밖에서_태어나_흘러_들어온다_띠_안에서_튀는_프레임_0()
        {
            const int steps = 4000; int wraps = 0;
            foreach (var bx in TreeX)
            {
                double prev = Expedition.PropX(bx, 0, Span, Margin);
                for (int i = 1; i <= steps; i++)
                {
                    double x = Expedition.PropX(bx, Span * i / steps, Span, Margin);
                    double d = x - prev;
                    if (d > 0)
                    {
                        wraps++;
                        Assert.LessOrEqual(prev + W, Span / steps + 1e-9, "되돌아오기 직전에는 나무가 왼쪽 밖으로 다 나가 있어야 한다(한 걸음 오차 안 · x=" + prev + ")");
                        Assert.GreaterOrEqual(x, 100.0, "되돌아오는 자리는 띠 밖(≥ 100)이어야 한다 — 안이면 «생성되는 장면» 이다(x=" + x + ")");
                    }
                    else Assert.Less(-d, Span / steps * 1.5, "튐이 아닌 걸음은 한 걸음 크기다");
                    prev = x;
                }
            }
            Assert.AreEqual(TreeX.Length, wraps, "한 바퀴에 나무마다 딱 한 번 되돌아온다");
            Assert.AreEqual(TreeX[0], Expedition.PropX(TreeX[0], Span, Span, Margin), 1e-9, "한 바퀴 뒤 제자리(바퀴 경계에서 이어진다)");
        }

        [Test]
        public void 옛_되돌림_값은_띠_안에서_태어났다_이_자가_그것을_잡는다()
        {
            // 1회차: 한 바퀴 102 · 여유 16 → 범위 −16 ~ 86. 왼쪽으로 다 나간 직후의 자리가 86 < 100 이다.
            double bornAt = Expedition.PropX(6, 6 + 16 + 1e-9, 102, 16);
            Assert.Less(bornAt, 100.0, "옛 값은 띠 안(86%)에서 태어난다 — 주인이 본 «생성되는 장면»");
            Assert.AreEqual(86.0, bornAt, 1e-6);
            double nowAt = Expedition.PropX(6, 6 + 16 + 1e-9, Span, Margin);
            Assert.GreaterOrEqual(nowAt, 100.0, "지금 값은 밖(120%)에서 태어난다");
        }
    }
}
