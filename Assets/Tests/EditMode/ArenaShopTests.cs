using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 상인(아레나 상점 · 26) 상품표(T209) — 값의 정본은 <b>주인 레퍼런스 <c>docs/ref/26_arena_shop.jpg</c></b> 이고
    /// 파일은 <c>Assets/KkomaKnight/arenaShop.json</c> 하나뿐이다(코드에 숫자 없음 · ROUTINE §1).
    /// <para>
    /// 이 테스트가 못 박는 것 셋: ⓐ 파일의 수가 레퍼런스에서 읽은 수와 같다 ⓑ <b>모르는 칸은 «모른다» 로 남아 있다</b>
    /// (전설 열쇠는 표에 아예 없고, 부활 토큰은 배지만 있고 한도·값이 없다 — 누가 나중에 «빈 칸이니 채우자» 며 수를 지어내면 여기서 빨개진다)
    /// ⓒ 한도 글자가 «한도 5/5» 로 조립된다.
    /// </para>
    /// </summary>
    public class ArenaShopTests
    {
        static ArenaShopData Real() =>
            ArenaShopData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "arenaShop.json"))));

        [Test]
        public void RealFileMatchesTheOwnersReference()
        {
            var d = Real();
            // ⓐ 레퍼런스 26 에서 읽은 값 — 카드마다 «Limit 5/5» 와 아레나 코인 값이 찍혀 있다
            var gem = d.Of("gem"); Assert.IsNotNull(gem, "다이아(Gems)");
            Assert.AreEqual(5, gem.Max, "Gems 한도 5"); Assert.AreEqual(10000, gem.Cost, 1e-9, "Gems 10000"); Assert.AreEqual(100, gem.Badge, "Gems 배지 100");
            foreach (var k in new[] { "recipeWeapon", "recipeArmor", "recipeHelmet", "recipeShoes", "recipeRing", "recipeNecklace" })
            {
                var e = d.Of(k); Assert.IsNotNull(e, k);
                Assert.AreEqual(5, e.Max, k + " 한도 5"); Assert.AreEqual(5000, e.Cost, 1e-9, k + " 5000"); Assert.AreEqual(20, e.Badge, k + " 배지 20");
            }
            var rare = d.Of("rareKey"); Assert.IsNotNull(rare, "희귀 열쇠");
            Assert.AreEqual(5000, rare.Cost, 1e-9, "Rare Key 5000"); Assert.IsFalse(rare.HasBadge, "열쇠에는 개수 배지가 없다(레퍼런스)");
            var epic = d.Of("epicKey"); Assert.IsNotNull(epic, "에픽 열쇠");
            Assert.AreEqual(20000, epic.Cost, 1e-9, "Epic Key 20000"); Assert.IsFalse(epic.HasBadge, "열쇠에는 개수 배지가 없다");
        }

        /// <summary>레퍼런스에서 «잘려 안 보이는» 두 칸은 채우지 않는다 — 없는 수를 만드는 것이 «—» 보다 나쁘다(T209 4항).</summary>
        [Test]
        public void TheTwoCardsCutOffInTheReferenceStayUnknown()
        {
            var d = Real();
            Assert.IsNull(d.Of("legendKey"),
                "전설 열쇠는 레퍼런스에서 스크롤 밖으로 잘려 한도·값이 안 보인다 — 표에 넣지 않는다(넣으려면 주인에게 값을 받아야 한다)");
            var rev = d.Of("revive"); Assert.IsNotNull(rev, "부활 토큰은 «배지 3» 만 레퍼런스에 보인다");
            Assert.AreEqual(3, rev.Badge, "부활 토큰 배지 3(레퍼런스에 보인다)");
            Assert.IsFalse(rev.HasLimit, "부활 토큰 한도는 레퍼런스에서 잘렸다 — 모른다로 남긴다");
            Assert.IsFalse(rev.HasCost, "부활 토큰 값도 잘렸다 — 모른다로 남긴다");
        }

        [Test]
        public void LimitTextIsBuiltFromTheFileAndFallsBackToDash()
        {
            var d = Real();
            Assert.AreEqual("한도 5/5", d.Limit(d.Of("gem"), "한도 —"), "한도 글자는 파일의 틀로 조립한다");
            Assert.AreEqual("한도 —", d.Limit(d.Of("revive"), "한도 —"), "모르는 칸은 «—»");
            Assert.AreEqual("한도 —", d.Limit(null, "한도 —"), "표에 없는 키도 «—»");
        }

        [Test]
        public void EmptyGoodsIsRejected()
        {
            Assert.Throws<System.FormatException>(() => ArenaShopData.Parse(@"{ ""goods"": [] }"), "goods 가 비면 조용히 넘어가지 않는다");
            Assert.Throws<System.FormatException>(() => ArenaShopData.Parse(@"{ ""goods"": [ { ""max"": 5 } ] }"), "key 없는 줄은 조용히 넘어가지 않는다");
        }
    }
}
