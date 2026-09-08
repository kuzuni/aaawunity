using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 상점 상품표(Assets/KkomaKnight/shop.json · T9) — 수치는 주인이 바꿀 수 있으므로 대개 값이 아니라 «꼴»(개수 · 양수 · 오름차순)을 본다.
    /// <para>
    /// ⚠ <b>골드 상품 셋만 예외로 값을 그대로 지킨다</b>(T260) — 주인이 2026-09-09 에 세 줄을 직접 줬기 때문이다
    /// («1000골드 → 100다이아 · 5000 → 500 · 25000 → 2500»). 그 전 값은 워커가 고른 것이라 «꼴» 만 봤다.
    /// <b>값을 준 자리와 워커가 고른 자리는 지키는 방식이 다르다</b> — 준 값이 조용히 바뀌면 그것은 주인 지시가 지워진 것이다.
    /// 다이아 상품(gemPacks)은 아직 워커가 고른 값이라 종전대로 «꼴» 만 본다.
    /// </para>
    /// </summary>
    public class ShopDataTests
    {
        static ShopData Load() => ShopData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "shop.json"))));

        [Test]
        public void ShopJson_HasSixGemPacksAndThreeGoldPacks()
        {
            var s = Load();
            Assert.That(s.GemPacks.Count, Is.EqualTo(6), "다이아 상품 6종 (₩1,000 · 1만 · 3만 · 5만 · 8만 · 11만)");
            Assert.That(s.GoldPacks.Count, Is.EqualTo(3), "골드 상품 3종 (1,000 · 5,000 · 25,000 골드 · 주인 2026-09-09)");
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

        /// <summary>
        /// T260 — 주인이 준 골드 상품 세 줄(2026-09-09 05:4X)이 표에 그대로 있는가.
        /// 수량과 가격을 <b>둘 다</b> 본다: 하나만 지키면 «5,000골드를 100다이아에» 같은 짝이 조용히 살아남는다.
        /// </summary>
        [Test]
        public void ShopJson_GoldPacksAreExactlyWhatTheOwnerGave()
        {
            var s = Load();
            var want = new[] { (gold: 1000d, gem: 100d), (gold: 5000d, gem: 500d), (gold: 25000d, gem: 2500d) };
            Assert.That(s.GoldPacks.Count, Is.EqualTo(want.Length), "골드 칸은 셋이다(레퍼런스 10 도 골드는 3칸)");
            for (int i = 0; i < want.Length; i++)
            {
                Assert.That(s.GoldPacks[i].Gold, Is.EqualTo(want[i].gold), $"{i + 1}번째 골드 상품의 수량(주인 지시 2026-09-09)");
                Assert.That(s.GoldPacks[i].Gem, Is.EqualTo(want[i].gem), $"{i + 1}번째 골드 상품의 다이아 가격(주인 지시 2026-09-09)");
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

        // ───────────────────────── T259 3항 — «무료 보급 때 Free 로 바뀌는 줄» (주인 2026-09-09) ─────────────────────────

        /// <summary>
        /// 주인이 이름 대고 지목한 두 줄이 표에 그렇게 적혀 있는가 — «다이아 100» 과 «골드 1,000».
        /// <para>이 자가 지키는 것은 <b>어느 줄이냐</b>이지 수량이 아니다(수량은 위 자들이 이미 지킨다).</para>
        /// </summary>
        [Test]
        public void ShopJson_TheFreeRowsAreTheOnesTheOwnerNamed()
        {
            var s = Load();
            Assert.IsNotNull(s.FreeGemPack, "무료 보급으로 «Free» 가 되는 다이아 줄이 있어야 한다(주인 «100다이아 부분 상품»)");
            Assert.IsNotNull(s.FreeGoldPack, "골드 쪽도 마찬가지(주인 «1000골드 부분도 마찬가지»)");
            Assert.AreEqual(100, s.FreeGemPack.Gem, "무료가 되는 다이아 줄 = 100다이아");
            Assert.AreEqual(1000, s.FreeGoldPack.Gold, "무료가 되는 골드 줄 = 1,000골드");
        }

        /// <summary>
        /// <b>«첫 줄» 이 아니라 «표가 그렇다고 적은 줄» 이다</b> — 순서를 바꿔도 무료 줄이 따라오면 안 된다.
        /// 순서로 정했으면 표에 줄을 하나 끼우는 순간 다른 상품이 조용히 공짜가 된다(그 사고는 아무 데도 «오류» 로 안 뜬다).
        /// </summary>
        [Test]
        public void TheFreeRowIsNotJustTheFirstOne()
        {
            var s = ShopData.Parse("{\"gemPacks\":[{\"won\":500,\"gem\":50},{\"won\":1000,\"gem\":100,\"free\":true}],\"goldPacks\":[{\"gold\":1000,\"gem\":100}]}");
            Assert.AreEqual(100, s.FreeGemPack.Gem, "표가 지목한 둘째 줄이 무료다(첫 줄이 아니다)");
            Assert.IsFalse(s.GemPacks[0].Free);
            Assert.IsNull(s.FreeGoldPack, "아무 줄도 안 적혀 있으면 «무료 줄이 없다» 다 — 마음대로 하나 고르지 않는다");
        }

        /// <summary>무료 줄이 갈래마다 둘이면 <b>읽는 순간 운다</b> — 안 그러면 화면이 둘 중 하나를 조용히 고른다.</summary>
        [Test]
        public void TwoFreeRowsInOneListIsAnError()
        {
            Assert.Throws<System.FormatException>(() => ShopData.Parse(
                "{\"gemPacks\":[{\"won\":1000,\"gem\":100,\"free\":true},{\"won\":2000,\"gem\":200,\"free\":true}],\"goldPacks\":[{\"gold\":1000,\"gem\":100}]}"));
            Assert.Throws<System.FormatException>(() => ShopData.Parse(
                "{\"gemPacks\":[{\"won\":1000,\"gem\":100}],\"goldPacks\":[{\"gold\":1000,\"gem\":100,\"free\":true},{\"gold\":5000,\"gem\":500,\"free\":true}]}"));
        }

        /// <summary>«무엇이 무료인가»(이 표)와 «오늘 몫을 썼는가»(<see cref="ShopFree"/>)는 <b>다른 물음</b>이고, 둘이 만나야 «Free» 가 뜬다.</summary>
        [Test]
        public void TheTableSaysWhatIsFreeAndShopFreeSaysWhetherItIsStillAvailable()
        {
            var d = TestData.Load();
            var save = SaveData.NewSave(d);
            var s = Load();
            const string today = "2026-09-09";

            Assert.IsTrue(ShopFree.Can(save, ShopFree.Gem, today) && s.FreeGemPack != null, "오늘 아직 안 받았고 무료 줄도 있다 → «Free»");
            ShopFree.Take(save, ShopFree.Gem, today);
            Assert.IsFalse(ShopFree.Can(save, ShopFree.Gem, today), "받고 나면 그 줄은 원래 가격으로 돌아간다");
            Assert.IsTrue(ShopFree.Can(save, ShopFree.Gold, today), "골드 줄은 그대로다 — 둘은 각각 하루 1번이다(주인 08:3X 재정정)");
        }
    }
}
