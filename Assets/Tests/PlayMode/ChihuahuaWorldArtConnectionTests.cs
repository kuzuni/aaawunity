using NUnit.Framework;
using KkomaKnight.Game;

public sealed class ChihuahuaWorldArtConnectionTests
{
    [TestCase("expedition")]
    [TestCase("hell")]
    public void DungeonTheme_UsesContractKeys(string dungeonKey)
    {
        var theme = BattleWorld.Theme.ForRun(3, dungeonKey, false);
        Assert.That(theme.Name, Is.EqualTo(dungeonKey));
        Assert.That(theme.Field, Is.EqualTo("env." + dungeonKey + ".field"));
        Assert.That(theme.Road, Is.EqualTo("env." + dungeonKey + ".road"));
        Assert.That(DungeonMapLayouts.Of(dungeonKey), Has.Length.EqualTo(3));
        for (int i = 1; i <= 3; i++) Assert.That(DungeonMapLayouts.Key(dungeonKey, i), Is.EqualTo("env." + dungeonKey + ".prop" + i));
    }

    [Test]
    public void NonDungeonRuns_KeepExistingThemeSelection()
    {
        Assert.That(BattleWorld.Theme.ForRun(2, null, false).Name, Is.EqualTo("deepForest"));
        Assert.That(BattleWorld.Theme.ForRun(4, "unknown", false).Name, Is.EqualTo("desert"));
        Assert.That(BattleWorld.Theme.ForRun(2, "hell", true), Is.SameAs(BattleWorld.Theme.Arena));
    }
}
