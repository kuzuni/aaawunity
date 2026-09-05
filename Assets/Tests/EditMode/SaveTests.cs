using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    public class SaveTests
    {
        [Test]
        public void RoundTripAndNormalize()
        {
            var d = TestData.Load();
            var s = SaveData.NewSave(d);
            s.Gold = 1234.5; s.Gem = 80; s.MaxChapter = 999; s.SelChapter = 5;
            var g = s.NewGear("weapon", "crit_weapon", d.Gear.RarLegend, 5); g.IsNew = true; s.Inv.Add(g);
            s.Inv.Add(new GearItem { Uid = g.Uid, Part = "helm", Type = "hpsh_helm", Rar = 0 });   // 중복 uid
            s.Eq["weapon"] = g.Uid; s.Eq["boot"] = 777;                                            // 죽은 장착 참조
            s.Slots["neck"] = 9999; s.GachaBoxes["myth"].P50 = 12;
            var back = SaveData.FromJson(s.ToJson(), d);
            Assert.That(back.Gold, Is.EqualTo(1234.5)); Assert.That(back.Gem, Is.EqualTo(80));
            Assert.That(back.MaxChapter, Is.EqualTo(d.Tune.MaxChapter)); Assert.That(back.SelChapter, Is.EqualTo(5));
            Assert.That(back.Inv.Count, Is.EqualTo(2));
            Assert.That(back.Inv[0].Rar, Is.EqualTo(d.Gear.RarMyth), "전설 +3 이상은 신화 0강으로");
            Assert.That(back.Inv[0].Plus, Is.EqualTo(0)); Assert.That(back.Inv[0].IsNew, Is.True);
            Assert.That(back.Inv[1].Uid, Is.Not.EqualTo(back.Inv[0].Uid), "uid 유일성");
            Assert.That(back.Eq.ContainsKey("boot"), Is.False); Assert.That(back.EquippedGear("weapon"), Is.Not.Null);
            Assert.That(back.SlotLv("neck"), Is.EqualTo(d.Gear.SlotLvMax));
            Assert.That(back.GachaBoxes["myth"].P50, Is.EqualTo(12)); Assert.That(back.GachaBoxes.ContainsKey("rare"), Is.True);
        }

        [Test]
        public void CorruptJsonFallsBackToFreshSave()
        {
            var d = TestData.Load();
            var s = SaveData.FromJson("{not json", d);
            Assert.That(s.MaxChapter, Is.EqualTo(1)); Assert.That(s.Inv.Count, Is.EqualTo(0)); Assert.That(s.GachaBoxes.Count, Is.EqualTo(3));
        }
    }
}
