using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T392 — 패스 «모두 받기»(<see cref="Pass.ClaimAll"/>): 받을 수 있는 칸 전부를 받고 재화를 주며, 두 번째는 아무것도 안 준다. 산 열만 · 표가 아는 칸만.
    /// 표는 실제 <c>pass.json</c>(T266 ⓑ 주인 규칙 · 세 열 다이아 · 100/500 · 유료2 2배)이다.
    /// </summary>
    public class PassClaimAllTests
    {
        static PassData Load() => PassData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "pass.json"))));

        [Test]
        public void 무료_열을_지금_레벨까지_전부_받고_두_번째는_없다()
        {
            // T462 — 패스 레벨 = 깬 챕터 수(MaxChapter − 1) · 값은 절반(50/250)
            var d = Load(); var s = new SaveData { MaxChapter = 8 };
            double gem0 = s.Gem;
            var got = Pass.ClaimAll(s, d);
            Assert.AreEqual(7, got.Count, "무료 열 1~7 = 일곱 칸(챕터 7개 깸)");
            Assert.AreEqual(50 * 6 + 250, s.Gem - gem0, 1e-9, "50×6 + 5레벨 250 = 550 다이아(주인 규칙 ½ · T462)");
            for (int lv = 1; lv <= 7; lv++) Assert.IsTrue(Pass.Claimed(s, lv, PassData.ColFree), "받은 칸으로 적힌다 " + lv);
            Assert.IsFalse(Pass.Claimed(s, 8, PassData.ColFree), "레벨 위는 안 받는다");
            Assert.IsFalse(Pass.AnyClaimable(s, d), "다 받았으니 더 없다");
            Assert.AreEqual(0, Pass.ClaimAll(s, d).Count, "두 번째는 아무것도 안 준다");
            Assert.AreEqual(50 * 6 + 250, s.Gem - gem0, 1e-9, "두 번째에 재화가 안 는다");
        }

        [Test]
        public void 산_열만_받는다_유료2는_2배()
        {
            var d = Load(); var s = new SaveData { MaxChapter = 6 };
            Pass.ClaimAll(s, d);
            double after1 = s.Gem;
            s.PassPaid2 = true;
            var got = Pass.ClaimAll(s, d);
            Assert.AreEqual(5, got.Count, "유료2 를 사면 1~5 다섯 칸이 새로 열린다");
            Assert.AreEqual((50 * 4 + 250) * 2, s.Gem - after1, 1e-9, "유료2 = 2배(주인 «더 비싼 거는 2배» · 값은 ½ T462)");
            Assert.IsFalse(Pass.Claimed(s, 3, PassData.ColPaid1), "안 산 유료1 은 그대로 잠겨 있다");
        }

        [Test]
        public void 그림_키가_재화가_아니면_그_칸은_안_받고_남긴다()
        {
            Assert.AreEqual(Mail.ItemGem, Pass.ItemOf("ui.gemRed"));
            Assert.AreEqual(Mail.ItemGold, Pass.ItemOf("ui.coin"));
            Assert.IsNull(Pass.ItemOf("ui.iconScroll"), "두루마리(«?»)는 재화가 아니다 — 못 준다");
            var d = PassData.Parse("{\"maxLevel\": 3, \"levels\": {\"1\": {\"free\": {\"icon\": \"ui.iconScroll\", \"qty\": \"1\"}, \"paid1\": {\"icon\": \"\", \"qty\": \"\"}, \"paid2\": {\"icon\": \"\", \"qty\": \"\"}}}}");
            var s = new SaveData { MaxChapter = 4 };
            Assert.AreEqual(0, Pass.ClaimAll(s, d).Count, "재화가 아닌 칸은 안 받는다");
            Assert.IsFalse(Pass.Claimed(s, 1, PassData.ColFree), "받은 것으로도 안 적는다(값이 서면 그때 받힌다)");
        }
    }
}
