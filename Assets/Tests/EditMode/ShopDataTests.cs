using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>상점 상품표(Assets/KkomaKnight/shop.json · T9) — 수치는 주인이 바꿀 수 있으므로 값이 아니라 «꼴»(개수 · 양수 · 오름차순)을 본다.</summary>
    public class ShopDataTests
    {
        static ShopData Load() => ShopData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "shop.json"))));

        [Test]
        public void ShopJson_HasSixGemPacksAndThreeGoldPacks()
        {
            var s = Load();
            Assert.That(s.GemPacks.Count, Is.EqualTo(6), "다이아 상품 6종 (₩1,000 · 1만 · 3만 · 5만 · 8만 · 11만)");
            Assert.That(s.GoldPacks.Count, Is.EqualTo(3), "골드 상품 3종 (1,000 · 3,000 · 10,000 골드)");
        }

        [Test]
        public void ShopJson_PacksArePositiveAndAscending()
        {
            var s = Load();
            for (int i = 0; i < s.GemPacks.Count; i++)
            {
                Assert.That(s.GemPacks[i].Won, Is.GreaterThan(0)); Assert.That(s.GemPacks[i].Gem, Is.GreaterThan(0));
                if (i > 0) { Assert.That(s.GemPacks[i].Won, Is.GreaterThan(s.GemPacks[i - 1].Won), "원화 오름차순"); Assert.That(s.GemPacks[i].Gem, Is.GreaterThan(s.GemPacks[i - 1].Gem), "다이아 오름차순"); }
            }
            for (int i = 0; i < s.GoldPacks.Count; i++)
            {
                Assert.That(s.GoldPacks[i].Gold, Is.GreaterThan(0)); Assert.That(s.GoldPacks[i].Gem, Is.GreaterThan(0));
                if (i > 0) { Assert.That(s.GoldPacks[i].Gold, Is.GreaterThan(s.GoldPacks[i - 1].Gold), "골드 오름차순"); Assert.That(s.GoldPacks[i].Gem, Is.GreaterThan(s.GoldPacks[i - 1].Gem), "다이아 가격 오름차순"); }
            }
        }

        [Test]
        public void ShopJson_GoldPackPriceIsBelowGemPackValue()
        {
            // 골드 상품의 다이아 가격은 가장 싼 다이아 상품 하나로도 살 수 있어야 상점이 막히지 않는다(밸런스가 아니라 동선 검사).
            var s = Load();
            Assert.That(s.GoldPacks[0].Gem, Is.LessThanOrEqualTo(s.GemPacks[0].Gem));
        }

        [Test]
        public void Parse_MinimalJson()
        {
            var s = ShopData.Parse("{\"gemPacks\":[{\"won\":1000,\"gem\":100}],\"goldPacks\":[{\"gold\":1000,\"gem\":30}]}");
            Assert.That(s.GemPacks[0].Won, Is.EqualTo(1000)); Assert.That(s.GemPacks[0].Gem, Is.EqualTo(100));
            Assert.That(s.GoldPacks[0].Gold, Is.EqualTo(1000)); Assert.That(s.GoldPacks[0].Gem, Is.EqualTo(30));
        }

        [Test]
        public void Parse_EmptyIsAnError()
        {
            Assert.Throws<System.FormatException>(() => ShopData.Parse("{\"gemPacks\":[],\"goldPacks\":[]}"));
        }
    }
}
