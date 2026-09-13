using System.Collections;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ChihuahuaWorldArtConnectionTests
{
    static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

    static bool HasSprite(Transform root, Sprite sprite)
    {
        if (root == null || sprite == null) return false;
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            if (renderer.sprite == sprite) return true;
        return false;
    }

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
    [UnityTest]
    public IEnumerator RuntimeBuildsBothDungeonThemesAndAllEventNodeArt()
    {
        try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
        yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
        float until = Time.realtimeSinceStartup + 60f;
        while (App.I == null && Time.realtimeSinceStartup < until) yield return null;
        var app = App.I; Assert.IsNotNull(app, "Bootstrap"); Assert.IsNotNull(app.Assets, "AssetCatalog");

        foreach (var dungeonKey in new[] { "hell", "expedition" })
        {
            var dungeon = app.Data.Dungeon.Of(dungeonKey); Assert.IsNotNull(dungeon, dungeonKey + " dungeon data");
            app.StartBattle(1, dungeon.Run, dungeonKey); yield return Frames(3);
            var screen = app.GetScreen<BattleScreen>(); Assert.IsNotNull(screen, dungeonKey + " BattleScreen");
            var world = screen.World; Assert.IsNotNull(world, dungeonKey + " BattleWorld");
            Assert.AreEqual(dungeonKey, world.MapTheme.Name, dungeonKey + " runtime theme");
            foreach (var key in new[]
            {
                "env." + dungeonKey + ".field", "env." + dungeonKey + ".road",
                "env." + dungeonKey + ".prop1", "env." + dungeonKey + ".prop2", "env." + dungeonKey + ".prop3",
            })
            {
                var sprite = app.Assets.Sprite(key); Assert.IsNotNull(sprite, key + " catalog sprite");
                Assert.IsTrue(HasSprite(world.Root, sprite), key + " 실제 월드 SpriteRenderer");
            }
            app.ShowScreen("lobby"); yield return Frames(2);
        }

        app.StartBattle(1); yield return Frames(3);
        var chapterWorld = app.GetScreen<BattleScreen>().World; Assert.IsNotNull(chapterWorld, "chapter BattleWorld");
        foreach (var key in new[] { "node.rest", "node.devil", "node.angel" })
        {
            var sprite = app.Assets.Sprite(key); Assert.IsNotNull(sprite, key + " catalog sprite");
            Assert.IsTrue(HasSprite(chapterWorld.Root, sprite), key + " 실제 월드 노드 SpriteRenderer");
        }
        app.ShowScreen("lobby"); yield return Frames(2);
    }

}
