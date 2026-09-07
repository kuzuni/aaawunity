using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T182 <b>3단계</b> 셈(<see cref="Stretch"/>)의 자 — «위/아래 고정 · 가운데 신축» 이 정말 그렇게 도는가.
    /// 유니티 없이 도는 순수 C# 자라 <c>dotnet test</c> 로 매 커밋 확인된다(붙이기 전에 셈부터 맞춘다).
    /// </summary>
    public class StretchTests
    {
        const float Eps = 0.01f;
        /// 9:21 폰의 신축 배수(주인이 말한 범위의 위 끝).
        static float K21 => Stretch.K(9f, 21f);

        [Test]
        public void K_기준비율은_1이고_넓은화면도_1이다()
        {
            Assert.AreEqual(1f, Stretch.K(1080f, 2337f), Eps, "기준 프레임은 신축 0");
            Assert.AreEqual(1f, Stretch.K(9f, 16f), Eps, "9:16 은 기준보다 납작 — 2단계(폭 상한 · 좌우 띠) 몫이라 1");
            Assert.AreEqual(1f, Stretch.K(3f, 4f), Eps, "3:4 태블릿도 1");
            Assert.Greater(K21, 1f, "9:21 은 늘어난다");
            Assert.AreEqual((21f / 9f) / (2337f / 1080f), K21, Eps, "9:21 배수 = 두 세로비의 비");
            Assert.AreEqual(1.0782f, K21, 0.001f, "9:21 은 7.8% 길다 — 1단계 표의 «남는 띠 7.3%» 와 같은 자리다");
        }

        [Test]
        public void K_상한을_넘는_화면은_상한에서_멈춘다()
        {
            float k = Stretch.K(9f, 30f);
            Assert.AreEqual(K21, k, Eps, "9:21 위는 더 안 늘인다 — 남는 만큼은 2단계 띠가 채운다");
        }

        [Test]
        public void 기준비율에서는_모든_줄이_한치도_안_움직인다()
        {
            foreach (var y in new[] { 0f, 3.7f, 14.2f, 30f, 50f, 69.5f, 92.6f, 100f })
                Assert.AreEqual(y, Stretch.MapY(y, 1f), Eps, "k=1 은 항등이어야 붙이기 전 화면이 그대로다");
            Stretch.MapRow(69.5f, 30.5f, 1f, out var ry, out var rh);
            Assert.AreEqual(69.5f, ry, Eps); Assert.AreEqual(30.5f, rh, Eps);
            Assert.AreEqual(0f, Stretch.MiddleGainPct(1f), Eps);
        }

        [Test]
        public void 위_띠는_픽셀을_지킨다()
        {
            float k = K21;
            // 위 띠의 줄은 «늘어난 프레임 안에서의 픽셀» 이 기준과 같아야 한다 → % × k 가 기준 % 로 돌아온다
            foreach (var y in new[] { 0f, 3.7f, 8.2f, 14.2f, Stretch.TopFixedPct })
                Assert.AreEqual(y, Stretch.MapY(y, k) * k, Eps, y + "% 줄이 위에서 잰 픽셀을 지켜야 한다");
            Stretch.MapRow(3.7f, 4.5f, k, out _, out var h);
            Assert.AreEqual(4.5f, h * k, Eps, "상단 바 높이는 픽셀 그대로(4.5% × 기준 높이)");
        }

        [Test]
        public void 아래_띠는_아래에서_잰_픽셀을_지킨다()
        {
            float k = K21;
            foreach (var y in new[] { Stretch.BottomFixedPct, 69.5f, 92.6f, 100f })
                Assert.AreEqual(100f - y, (100f - Stretch.MapY(y, k)) * k, Eps, y + "% 줄이 아래에서 잰 픽셀을 지켜야 한다");
            Assert.AreEqual(100f, Stretch.MapY(100f, k), Eps, "프레임 아래 끝은 그대로 아래 끝");
            Stretch.MapRow(92.6f, 7.4f, k, out _, out var h);
            Assert.AreEqual(7.4f, h * k, Eps, "탭 바 높이는 픽셀 그대로");
        }

        [Test]
        public void 가운데가_남는_높이를_전부_가져간다()
        {
            float k = K21;
            float y0 = Stretch.MapY(Stretch.TopFixedPct, k), y1 = Stretch.MapY(Stretch.BottomFixedPct, k);
            float refMiddle = Stretch.BottomFixedPct - Stretch.TopFixedPct;
            Assert.Greater((y1 - y0) * k, refMiddle + 1f, "가운데가 늘어난 몫을 먹어야 한다");
            // 위·아래가 픽셀을 지켰으니 늘어난 전부(= (k−1) × 기준 높이)가 가운데 몫이다
            Assert.AreEqual((k - 1f) * 100f, Stretch.MiddleGainPct(k), Eps, "벌어들인 높이 전부가 가운데로");
            Assert.AreEqual(7.82f, Stretch.MiddleGainPct(k), 0.05f, "9:21 에서 가운데가 기준 프레임의 7.8%p 를 더 갖는다");
        }

        [Test]
        public void 순서가_뒤집히지_않고_이어진다()
        {
            float k = K21;
            float prev = -1f;
            for (float y = 0f; y <= 100f; y += 0.5f)
            {
                float m = Stretch.MapY(y, k);
                Assert.GreaterOrEqual(m, prev - Eps, y + "% 에서 순서가 뒤집혔다");
                Assert.GreaterOrEqual(m, -Eps); Assert.LessOrEqual(m, 100f + Eps, y + "% 가 프레임 밖으로 나갔다");
                prev = m;
            }
            // 띠 경계에서 끊기지 않는다(가운데 식과 띠 식이 같은 값을 낸다)
            Assert.AreEqual(Stretch.MapY(Stretch.TopFixedPct, k), Stretch.MapY(Stretch.TopFixedPct + 0.001f, k), 0.01f);
            Assert.AreEqual(Stretch.MapY(Stretch.BottomFixedPct, k), Stretch.MapY(Stretch.BottomFixedPct - 0.001f, k), 0.01f);
        }

        [Test]
        public void 띠를_걸친_칸도_두_변으로_잰다()
        {
            float k = K21;
            // 전투 마당 지면 띠(30.0~51.0)는 가운데에만 있고, 버프 바(17.5~47.5)는 위 띠를 걸친다
            Stretch.MapRow(17.5f, 30.0f, k, out var y, out var h);
            Assert.AreEqual(Stretch.MapY(17.5f, k), y, Eps);
            Assert.AreEqual(Stretch.MapY(47.5f, k) - Stretch.MapY(17.5f, k), h, Eps, "높이는 두 변 사이 — 걸친 칸도 어긋나지 않는다");
            Assert.Greater(h * k, 30.0f, "가운데를 품은 칸은 픽셀로도 커진다");
        }
    }
}
