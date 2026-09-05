using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>GearLook(장비 외형·아이콘 표) ↔ catalog.json — 부위×세트×등급 키가 전부 카탈로그에 실재하는가 (T7 · 승인 대기 26).</summary>
    public class GearLookTests
    {
        static HashSet<string> CatalogSprites()
        {
            var path = Path.GetFullPath(Path.Combine(TestData.Dir, "..", "..", "KkomaKnight", "catalog.json"));
            var root = new JNode(MiniJson.Parse(File.ReadAllText(path)));
            var set = new HashSet<string>(); foreach (var k in root["sprites"].Keys) set.Add(k); return set;
        }

        [Test]
        public void RarCountMatchesGearJson()
        {
            var d = TestData.Load();
            Assert.That(d.Gear.RarName.Length, Is.EqualTo(GearLook.RarCount));
        }

        [Test]
        public void EveryLookPartSetRarityHasCatalogSprite()
        {
            var d = TestData.Load(); var sprites = CatalogSprites();
            foreach (var part in GearLook.LookParts)
                foreach (var set in d.Gear.Sets)
                    for (int r = 0; r < d.Gear.RarName.Length; r++)
                    {
                        var key = GearLook.PartKey(part, set, r);
                        Assert.That(key, Is.Not.Null, part + "/" + set + "/" + r);
                        Assert.That(sprites.Contains(key), Is.True, "catalog.json 에 없음: " + key);
                        Assert.That(GearLook.IconKey(part, set, r), Is.EqualTo(key));
                    }
        }

        [Test]
        public void PartsWithoutLookUseGuiProIcons()
        {
            var d = TestData.Load(); var sprites = CatalogSprites();
            foreach (var part in d.Gear.Parts)
            {
                if (GearLook.HasLook(part)) continue;
                foreach (var set in d.Gear.Sets)
                {
                    Assert.That(GearLook.PartKey(part, set, 0), Is.Null, part);
                    var icon = GearLook.IconKey(part, set, 3);
                    Assert.That(icon.StartsWith("gi."), Is.True, icon);
                    Assert.That(sprites.Contains(icon), Is.True, "catalog.json 에 없음: " + icon);
                }
            }
        }

        [Test]
        public void ItemKeysFollowTypeSet()
        {
            var d = TestData.Load();
            var g = new GearItem { Part = "weapon", Type = "hpsh_weapon", Rar = 2, Plus = 0 };
            Assert.That(GearLook.PartKey(d, g), Is.EqualTo("cm.gear.weapon.hpsh.2"));
            Assert.That(GearLook.PartKey("weapon", "crit", 99), Is.EqualTo("cm.gear.weapon.crit.3"));
            Assert.That(GearLook.PartKey("weapon", "crit", -1), Is.EqualTo("cm.gear.weapon.crit.0"));
        }

        [Test]
        public void WeaponSlotBySet()
        {
            Assert.That(GearLook.WeaponSlot("crit"), Is.EqualTo("Sword"));
            Assert.That(GearLook.WeaponSlot("hpsh"), Is.EqualTo("Blunt"));
            Assert.That(GearLook.WeaponSlot("evade"), Is.EqualTo("Spear"));
        }
    }
}
