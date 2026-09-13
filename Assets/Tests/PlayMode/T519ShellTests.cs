using System.Collections.Generic;
using System.Reflection;
using KkomaKnight.Game;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    public sealed class T519ShellTests
    {
        [Test]
        public void ChapterArtCyclesInSpecifiedOrder()
        {
            Assert.AreEqual("ui.chapter.autumn", LobbyScreen.ChapterArtKey(1));
            Assert.AreEqual("ui.chapter.deepForest", LobbyScreen.ChapterArtKey(2));
            Assert.AreEqual("ui.chapter.forest", LobbyScreen.ChapterArtKey(3));
            Assert.AreEqual("ui.chapter.desert", LobbyScreen.ChapterArtKey(4));
            Assert.AreEqual("ui.chapter.autumn", LobbyScreen.ChapterArtKey(5));
        }

        [Test]
        public void OptionArtMapCoversOnlyTheElevenRequestedKeys()
        {
            var field = typeof(UiKit).GetField("ChihuahuaOptionIcons", BindingFlags.Static | BindingFlags.NonPublic);
            var map = (Dictionary<string, string>)field.GetValue(null);
            CollectionAssert.AreEquivalent(new[]
            {
                "pi.attack", "pi.defense", "pi.atk_spd", "pi.fist", "pi.critical", "pi.damage",
                "pi.drop", "pi.heart", "pi.shield", "pi.star", "ui.dodge",
            }, map.Keys);
            Assert.AreEqual(11, map.Count);
        }

        [Test]
        public void ProfileFaceSaveKeysRemainNineAndUnchanged()
        {
            CollectionAssert.AreEqual(new[]
            {
                "ui.iconFoe1", "ui.iconFoe2", "ui.iconFoe3", "ui.iconFoe4",
                "ui.face5", "ui.face6", "ui.face7", "ui.face8", "ui.face9",
            }, Profile.Faces);
        }
    }
}
