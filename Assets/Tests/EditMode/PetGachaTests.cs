using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T293 ⓓ — 소환 값과 «펫알 모드» 의 자 (주인 2026-09-09 09:3X
    /// «1회 소환 다이아 100개 · 10회는 1,000개. 펫알 있을 때 10개 미만이면 1회 소환 버튼이 펫알 버튼이 되고,
    /// 10개 이상이면 1회·10회 둘 다 펫알 버튼. 키로 상자 소환할 때랑 같은 느낌»).
    /// <para>
    /// <b>주인이 준 값은 그대로 못 박는다</b>(100 · 1,000 · 세 갈래) — 워커가 고른 것이 아니라 주인이 준 것이라
    /// «자가 표의 거울» 문제가 없다(결정 555).
    /// </para>
    /// <para>
    /// ⚠ <b>캡(10)은 자에도 안 박는다</b> — 캡은 <c>gacha.json</c> 의 <c>tenPull.count</c> 라 부르는 쪽이 준다.
    /// 그래서 캡을 3 으로 줘 보고도 규칙이 그대로 굴러가는지 잰다(캡이 표에서 바뀌는 날 이 자가 먼저 안다).
    /// </para>
    /// </summary>
    public class PetGachaTests
    {
        static PetData Load() => PetData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "pet.json"))));

        const int Cap = 10;   // 이 자에서만 쓰는 «지금 표의 값» — 규칙에는 안 들어간다(아래 캡_이_바뀌어도 가 그것을 지킨다)

        [Test]
        public void 다이아_소환_값은_주인이_준_100과_1000이다()
        {
            var d = Load();
            Assert.AreEqual(100, d.CostOne, 1e-9, "1회 소환 다이아(주인 09:3X)");
            Assert.AreEqual(1000, d.CostTen, 1e-9, "10회 소환 다이아(주인 09:3X)");
        }

        [Test]
        public void 펫알이_없으면_두_버튼_다_다이아다()
        {
            var d = Load();
            var one = Pets.Offer(d, false, 0, Cap);
            var ten = Pets.Offer(d, true, 0, Cap);
            Assert.IsFalse(one.ByEgg); Assert.AreEqual(1, one.Count); Assert.AreEqual(100, one.Diamond, 1e-9); Assert.AreEqual(0, one.Egg, 1e-9);
            Assert.IsFalse(ten.ByEgg); Assert.AreEqual(Cap, ten.Count); Assert.AreEqual(1000, ten.Diamond, 1e-9);
        }

        [Test]
        public void 펫알이_캡보다_적으면_소환만_펫알이고_x10_은_다이아다()
        {
            var d = Load();
            foreach (var k in new[] { 1, 2, 9 })
            {
                var one = Pets.Offer(d, false, k, Cap);
                var ten = Pets.Offer(d, true, k, Cap);
                Assert.IsTrue(one.ByEgg, "펫알 " + k + "개면 «소환» 은 펫알 버튼이다");
                Assert.AreEqual(k, one.Count, "가진 것을 다 쓴다 — " + k + "회");
                Assert.AreEqual(k, one.Egg, 1e-9); Assert.AreEqual(0, one.Diamond, 1e-9);
                Assert.IsFalse(ten.ByEgg, "펫알 " + k + "개(캡 미만)면 «x10» 은 아직 다이아다");
                Assert.AreEqual(1000, ten.Diamond, 1e-9);
            }
        }

        [Test]
        public void 펫알이_캡_이상이면_두_버튼_다_펫알이고_캡만큼만_쓴다()
        {
            var d = Load();
            foreach (var k in new[] { 10, 17, 100 })
                foreach (var ten in new[] { false, true })
                {
                    var o = Pets.Offer(d, ten, k, Cap);
                    Assert.IsTrue(o.ByEgg, "펫알 " + k + "개면 둘 다 펫알이다(ten=" + ten + ")");
                    Assert.AreEqual(Cap, o.Count, "한 번에 캡만큼만 쓴다(17개면 10회 뽑고 7 남는다 · T275)");
                    Assert.AreEqual(Cap, o.Egg, 1e-9); Assert.AreEqual(0, o.Diamond, 1e-9);
                }
        }

        [Test]
        public void 캡이_바뀌어도_규칙은_그대로다()
        {
            // 캡은 표(gacha.json tenPull.count)에서 온다 — 코드에도 자에도 10 이 박혀 있으면 안 된다.
            var d = Load();
            Assert.IsTrue(Pets.Offer(d, true, 3, 3).ByEgg, "캡이 3이면 펫알 3개로도 «x10» 이 펫알이 된다");
            Assert.AreEqual(3, Pets.Offer(d, true, 3, 3).Count);
            Assert.IsFalse(Pets.Offer(d, true, 2, 3).ByEgg, "캡이 3이면 펫알 2개로는 아직 아니다");
            // 표가 비어 0 이 들어와도 버튼을 죽이지 않는다 — «펫알 1개 = 1회»(T273)로 물러선다
            Assert.AreEqual(1, Pets.EggUse(5, 0), "캡 0 은 1 로 본다");
        }

        [Test]
        public void 반쪽_펫알은_한_번도_못_뽑는다()
        {
            // 펫알은 Mail.Give 로 소수도 들어올 수 있다(GachaKeys.Add 가 double 이다) — 0.9개로 뽑히면 재화가 새 나간다.
            var d = Load();
            Assert.AreEqual(0, Pets.EggUse(0.9, Cap));
            Assert.IsFalse(Pets.Offer(d, false, 0.9, Cap).ByEgg, "0.9개면 아직 다이아 버튼이다");
            Assert.AreEqual(1, Pets.EggUse(1.9, Cap), "1.9개는 1회다 — 남은 0.9 는 그대로 있는다");
        }

        [Test]
        public void 전투_따라_걷기_값이_표에서_온다()
        {
            // T293 9항 — «플레이어 뒤에 따라오는 느낌». 두 수 다 보이기 값이라 엔진은 안 본다.
            var d = Load();
            Assert.Greater(d.BattleGapDx, 0, "간격이 0 이면 펫이 플레이어와 겹쳐 선다");
            Assert.Greater(d.BattleScale, 0, "배율이 0 이면 아무것도 안 보인다");
            Assert.LessOrEqual(d.BattleScale, 1.0, "펫이 플레이어보다 크면 «뒤에 따라오는» 으로 안 읽힌다");
            // 세 마리가 다 화면 안에 서는가 — 플레이어는 화면 왼쪽 16%(ui.json camera.playerX · 레이아웃 540 의 86.4)에 붙어 서고
            // 뒤쪽은 Spread 가 1배라 레이아웃 거리 = gapDx × zoom(1.5). 셋째까지가 0 보다 커야 «줄지어 따라오는» 그림이 된다.
            double lastX = 86.4 - d.BattleGapDx * 1.5 * d.Slots;
            Assert.Greater(lastX, 0, "마지막 펫이 화면 왼쪽 밖으로 나간다 — gapDx 를 줄여야 한다(지금 " + d.BattleGapDx + ")");
        }

        [Test]
        public void 표의_소환_값이_0이면_읽는_순간_운다()
        {
            // 조용히 어긋나면 «공짜 소환» 이 되고 아무도 안 운다(결정 818 갈래).
            var bad = "{\"rate\":[70,25,5],\"slots\":1,\"slotUnlockPulls\":[0],\"cost\":{\"one\":0,\"ten\":1000}," +
                      "\"proc\":{\"chance\":33},\"triggers\":[{\"key\":\"evade\",\"name\":\"회피\"}]," +
                      "\"grades\":[{\"key\":\"common\",\"name\":\"일반\",\"rar\":0,\"shot\":\"axe\",\"count\":1}]," +
                      "\"pets\":[{\"id\":\"common_evade\",\"grade\":\"common\",\"trigger\":\"evade\",\"name\":\"ㄱ\"}]}";
            Assert.Throws<System.FormatException>(() => PetData.Parse(bad));
        }
    }
}
