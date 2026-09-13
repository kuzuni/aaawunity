using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    public class ChihuahuaEventsArtTests
    {
        [TestCase("weapon", "recipe.weapon")]
        [TestCase("armor", "recipe.armor")]
        [TestCase("helm", "recipe.helmet")]
        [TestCase("boot", "recipe.shoes")]
        [TestCase("glove", "recipe.ring")]
        [TestCase("neck", "recipe.necklace")]
        public void RecipePartsUseTheirArtworkKey(string part, string expected)
            => Assert.That(Recipes.Icon(part), Is.EqualTo(expected));

        [TestCase(null)]
        [TestCase("")]
        [TestCase("unknown")]
        public void RandomOrUnknownRecipeKeepsTheScroll(string part)
            => Assert.That(Recipes.Icon(part), Is.EqualTo(Recipes.IconKey));
    }
}
