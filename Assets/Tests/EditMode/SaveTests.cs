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
        public void SpeedIsRememberedAndDefaultsToOne()
        {
            // T18 — 배속(x2) 기억: 왕복 유지 · 옛 세이브(필드 없음)는 1 · 범위 밖은 1~2 로 클램프
            var d = TestData.Load();
            var s = SaveData.NewSave(d);
            Assert.That(s.Speed, Is.EqualTo(1), "새 세이브 = x1");
            s.Speed = 2;
            var back = SaveData.FromJson(s.ToJson(), d);
            Assert.That(back.Speed, Is.EqualTo(2), "x2 가 왕복으로 남는다");
            Assert.That(s.ToJson().Contains("\"speed\""), Is.True, "세이브 JSON 에 speed 필드");
            var legacy = SaveData.FromJson("{\"v\":2,\"gold\":10,\"maxChapter\":3,\"selChapter\":2}", d);   // index.html 세이브에는 speed 가 없다
            Assert.That(legacy.Speed, Is.EqualTo(1), "필드가 없으면 1"); Assert.That(legacy.Gold, Is.EqualTo(10));
            Assert.That(SaveData.FromJson("{\"speed\":7}", d).Speed, Is.EqualTo(SaveData.SpeedMax), "상한 클램프");
            Assert.That(SaveData.FromJson("{\"speed\":0}", d).Speed, Is.EqualTo(SaveData.SpeedMin), "하한 클램프");
            Assert.That(SaveData.FromJson("{\"speed\":\"x2\"}", d).Speed, Is.EqualTo(1), "숫자가 아니면 1");
        }

        [Test]
        public void MuteBgmAndSfxRoundTripAndLegacyMutedMigratesToBgm()
        {
            // T28 — 음소거 2개(배경음·효과음). 옛 세이브의 `muted`(소리 전체) 는 배경음 음소거로 이관 · muteSfx 없으면 false · Muted 별칭 = MuteBgm
            var d = TestData.Load();
            var s = SaveData.NewSave(d);
            Assert.That(s.MuteBgm, Is.False); Assert.That(s.MuteSfx, Is.False); Assert.That(s.Muted, Is.False);
            s.MuteBgm = true; s.MuteSfx = true;
            var back = SaveData.FromJson(s.ToJson(), d);
            Assert.That(back.MuteBgm, Is.True, "muteBgm 왕복"); Assert.That(back.MuteSfx, Is.True, "muteSfx 왕복"); Assert.That(back.Muted, Is.True, "Muted 별칭");
            Assert.That(s.ToJson().Contains("\"muted\":true"), Is.True, "index.html 호환 필드 muted 도 같은 값으로 쓴다");
            var legacy = SaveData.FromJson("{\"v\":2,\"muted\":true,\"gold\":5}", d);   // index.html 세이브 — muteBgm/muteSfx 없음
            Assert.That(legacy.MuteBgm, Is.True, "옛 muted → MuteBgm"); Assert.That(legacy.MuteSfx, Is.False, "효과음은 켜진 채");
            var newer = SaveData.FromJson("{\"v\":2,\"muted\":true,\"muteBgm\":false,\"muteSfx\":true}", d);
            Assert.That(newer.MuteBgm, Is.False, "muteBgm 이 있으면 그것이 우선"); Assert.That(newer.MuteSfx, Is.True);
            s.Muted = false; Assert.That(s.MuteBgm, Is.False, "Muted 쓰기 = MuteBgm 쓰기");
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
