using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 장비 «레시피»(T290 · 주인 2026-09-09 05:5X «강화하려면 레시피도 필요하게 · 부위마다 · 1강 2개 + 골드 · 강화마다 2개씩 늘어남»).
    /// 지시서 5항의 EditMode 몫 — ⓐ <c>Need</c> 경계 · ⓑ <c>SlotUp</c> 이 골드·레시피를 <b>같이</b> 빼고 모자라면 <b>아무것도</b> 안 뺀다
    /// · ⓒ <c>Mail.Give("recipe.helm", 3)</c> 이 든다 · ⓓ 모르는 부위는 0 · ⓔ <c>recipe.json parts == gear.json parts</c>.
    /// <para>
    /// «표가 지금 얼마인가»(<c>perLevel</c>)는 <b>한 자리에만</b> 두고(<see cref="Json_PerLevelIsZeroUntilRecipesAreGiven"/>)
    /// 나머지는 값이 아니라 <b>꼴</b>을 본다 — T291 이 들어오는 커밋에서 그 수가 0 → 2 로 바뀌어도 규칙 자는 안 깨져야 한다.
    /// 그래서 규칙 자는 <b>손으로 지은 표</b>(<see cref="Tbl"/>)로 재고, 진짜 표는 «지금 0 인가» 와 «부위가 gear.json 과 같은가» 만 본다.
    /// </para>
    /// </summary>
    public class RecipeTests
    {
        static RecipeData Load() => RecipeData.Parse(
            File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "recipe.json"))));
        static GameData Data() => TestData.Load();
        static SaveData NewSave() => SaveData.NewSave(TestData.Load());

        /// <summary>주인 규칙(<c>perLevel 2</c>)대로 도는 표 — 진짜 표의 수가 T291 에서 바뀌어도 규칙 자가 안 흔들리게 여기서 짓는다.</summary>
        static RecipeData Tbl(int perLevel = 2) => RecipeData.Parse(
            "{\"perLevel\": " + perLevel + ", \"parts\": [\"weapon\",\"helm\",\"armor\",\"glove\",\"boot\",\"neck\"], \"name\": {\"helm\": \"투구 레시피\"}}");

        [Test]
        public void Json_PerLevelIsZeroUntilRecipesAreGiven()
        {
            // T290 4항 — 레시피를 **주는 곳**(T291 원정 층 · T292 주간 트랙)이 아직 없다. 2 인 채로 먼저 배포되면 슬롯 강화가 «영원히 부족» 이 된다.
            // T291 이 같은 초록 런에 들어가는 커밋이 이 수를 2 로 올리고, 그때 이 자는 그 커밋과 함께 고쳐진다(그것이 이 자의 목적이다).
            Assert.That(Load().PerLevel, Is.EqualTo(0), "지금은 0 — 주는 곳이 서기 전에는 레시피가 들면 안 된다");
        }

        [Test]
        public void Json_PartsMatchGearTable()
        {
            // 부위가 어긋나면 «레시피가 없는 부위» 나 «어떤 강화도 안 쓰는 레시피» 가 조용히 생긴다.
            var d = Load(); var gear = Data().Gear.Parts;
            Assert.That(d.Parts, Is.EquivalentTo(gear), "recipe.json parts 는 gear.json parts 와 같은 여섯이어야 한다");
            foreach (var pt in gear) Assert.That(Recipes.Name(d, pt), Is.Not.EqualTo("레시피"), $"{pt} 의 이름이 표에 있어야 한다");
        }

        [Test]
        public void Need_IsPerLevelTimesNextLevel()
        {
            // 주인 «1강 = 2개 · 강화마다 2개씩 늘어남» → Lv L → L+1 에 perLevel × (L+1).
            var d = Tbl();
            Assert.That(Recipes.Need(d, 0), Is.EqualTo(2), "1강 = 2개(주인)");
            Assert.That(Recipes.Need(d, 1), Is.EqualTo(4), "2강 = 4개(주인)");
            Assert.That(Recipes.Need(d, 2), Is.EqualTo(6), "3강 = 6개");
            Assert.That(Recipes.Need(d, 9), Is.EqualTo(20), "10강 = 20개");
            Assert.That(Recipes.Need(Tbl(0), 5), Is.EqualTo(0), "perLevel 0 = 아예 안 든다");
            Assert.That(Recipes.Need(null, 5), Is.EqualTo(0), "표를 못 읽었으면 안 든다(강화가 막히지 않는다)");
            Assert.That(Recipes.Need(d, -3), Is.EqualTo(2), "음수 레벨은 0 으로 본다");
        }

        [Test]
        public void Item_NamesRoundTripAndUnknownIsZero()
        {
            Assert.That(Recipes.Item("helm"), Is.EqualTo("recipe.helm"));
            Assert.That(Recipes.IsRecipe("recipe.helm"), Is.True);
            Assert.That(Recipes.IsRecipe("recipe."), Is.False, "부위 없는 밑동만으로는 레시피가 아니다");
            Assert.That(Recipes.IsRecipe("gold"), Is.False);
            Assert.That(Recipes.IsRecipe(null), Is.False);
            Assert.That(Recipes.PartOf("recipe.neck"), Is.EqualTo("neck"));
            Assert.That(Recipes.PartOf("gold"), Is.Null);

            var s = NewSave();
            Assert.That(Recipes.Count(s, "helm"), Is.EqualTo(0), "새 세이브는 0");
            Assert.That(Recipes.Count(s, "없는부위"), Is.EqualTo(0), "모르는 부위는 0");
            Assert.That(Recipes.Count(null, "helm"), Is.EqualTo(0));
        }

        [Test]
        public void Mail_GivesAndHoldsRecipes()
        {
            // 주는 길은 Mail.Pay 한 곳이다 — 우편·퀘스트·던전·출석이 전부 그 길로 준다(T291·T292 가 이 길을 쓴다).
            var s = NewSave();
            Assert.That(Mail.CanPay("recipe.helm"), Is.True, "우편함에 들어갈 수 있어야 한다");
            Mail.Give(s, "recipe.helm", 3);
            Assert.That(Recipes.Count(s, "helm"), Is.EqualTo(3));
            Assert.That(Mail.Held(s, "recipe.helm"), Is.EqualTo(3).Within(1e-9), "보유 조회도 같은 자리를 봐야 한다");
            Mail.Give(s, "recipe.helm", 2);
            Assert.That(Recipes.Count(s, "helm"), Is.EqualTo(5), "쌓인다");
            Mail.Give(s, "recipe.helm", -1);
            Assert.That(Recipes.Count(s, "helm"), Is.EqualTo(5), "음수는 아무 일도 안 한다");
            Assert.That(Recipes.Count(s, "weapon"), Is.EqualTo(0), "다른 부위는 안 는다");
        }

        [Test]
        public void SlotUp_SpendsGoldAndRecipesTogether()
        {
            var D = Data(); var s = NewSave();
            D.Recipe = Tbl();   // 주인 규칙이 켜진 상태(진짜 표는 아직 0 이다 · 위 자 참조)
            try
            {
                s.Gold = 1e12; s.Slots["helm"] = 0;
                double cost = D.Gear.SlotCost(0);

                // ⓐ 레시피가 모자라면 아무것도 안 바뀐다 — 골드도 안 빠지고 레벨도 그대로다.
                Recipes.Add(s, "helm", 1);
                Assert.That(GearSystem.SlotUp(D, s, "helm", out string why), Is.False);
                Assert.That(why, Is.EqualTo("투구 레시피가 부족합니다"), "무엇이 모자란지 이름으로 말한다(주인 폰에 뜨는 글)");
                Assert.That(s.Gold, Is.EqualTo(1e12).Within(1e-6), "실패했는데 골드가 빠지면 안 된다");
                Assert.That(s.SlotLv("helm"), Is.EqualTo(0));
                Assert.That(Recipes.Count(s, "helm"), Is.EqualTo(1), "실패했는데 레시피가 빠지면 안 된다");

                // ⓑ 둘 다 있으면 둘 다 빠지고 레벨이 하나 오른다.
                Recipes.Add(s, "helm", 1);   // 2개 = 1강 값
                Assert.That(GearSystem.SlotUp(D, s, "helm", out why), Is.True, why);
                Assert.That(s.SlotLv("helm"), Is.EqualTo(1));
                Assert.That(Recipes.Count(s, "helm"), Is.EqualTo(0), "레시피 2개가 빠진다");
                Assert.That(s.Gold, Is.EqualTo(1e12 - cost).Within(1e-6), "골드도 같이 빠진다");

                // ⓒ 다음 단계는 4개 — 3개로는 안 된다.
                Recipes.Add(s, "helm", 3);
                Assert.That(GearSystem.SlotUp(D, s, "helm", out why), Is.False, "2강은 4개다");
                Recipes.Add(s, "helm", 1);
                Assert.That(GearSystem.SlotUp(D, s, "helm", out why), Is.True, why);
                Assert.That(s.SlotLv("helm"), Is.EqualTo(2));
                Assert.That(Recipes.Count(s, "helm"), Is.EqualTo(0));

                // ⓓ 골드가 모자라면 레시피도 안 빠진다(둘 중 하나만 빠지는 자리를 만들지 않는다).
                s.Gold = 0; Recipes.Add(s, "helm", 999);
                Assert.That(GearSystem.SlotUp(D, s, "helm", out why), Is.False);
                Assert.That(why, Is.EqualTo("골드가 부족합니다"));
                Assert.That(Recipes.Count(s, "helm"), Is.EqualTo(999), "골드로 막혔으면 레시피는 그대로다");

                // ⓔ 최대 레벨에서는 아무것도 안 빠진다.
                s.Gold = 1e12; s.Slots["helm"] = D.Gear.SlotLvMax;
                Assert.That(GearSystem.SlotUp(D, s, "helm", out why), Is.False);
                Assert.That(why, Is.EqualTo("이미 최대 레벨입니다"));
                Assert.That(Recipes.Count(s, "helm"), Is.EqualTo(999));
                Assert.That(s.Gold, Is.EqualTo(1e12).Within(1e-6));
            }
            finally { D.Recipe = null; }   // TestData.Load 가 GameData 를 캐시로 돌려준다 — 남의 자에 이 표를 흘리지 않는다
        }

        [Test]
        public void SlotUp_WithoutTable_CostsGoldOnly()
        {
            // 표가 없거나 perLevel 0 이면 «레시피 갈래가 통째로 없는 것» 과 같아야 한다 — 지금 배포되는 모습이 이것이다(T290 4항).
            var D = Data(); var s = NewSave();
            Assert.That(D.Recipe, Is.Null, "EditMode 의 GameData 에는 이 표가 안 실린다(Bootstrap 이 싣는다)");
            s.Gold = 1e12; s.Slots["helm"] = 0;
            Assert.That(GearSystem.SlotUp(D, s, "helm", out string why), Is.True, why);
            Assert.That(s.SlotLv("helm"), Is.EqualTo(1), "표 없이도 옛 그대로 골드만으로 오른다");
            Assert.That(Recipes.Count(s, "helm"), Is.EqualTo(0));
        }

        [Test]
        public void Save_RoundTripsRecipes()
        {
            var s = NewSave();
            Recipes.Add(s, "helm", 7); Recipes.Add(s, "weapon", 2);
            var back = SaveData.FromJson(s.ToJson(), TestData.Load());
            Assert.That(Recipes.Count(back, "helm"), Is.EqualTo(7));
            Assert.That(Recipes.Count(back, "weapon"), Is.EqualTo(2));
            Assert.That(Recipes.Count(back, "boot"), Is.EqualTo(0));
        }

        [Test]
        public void Table_RejectsBrokenRows()
        {
            Assert.Throws<System.FormatException>(() => RecipeData.Parse("{\"perLevel\": -1, \"parts\": [\"helm\"]}"), "음수면 강화할수록 레시피가 생긴다");
            Assert.Throws<System.FormatException>(() => RecipeData.Parse("{\"perLevel\": 2, \"parts\": []}"), "부위가 비면 어떤 강화도 이 표를 못 찾는다");
        }
    }
}
