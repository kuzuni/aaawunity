using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace KkomaKnight.Tests.Play
{
    /// <summary>모든 실제 특전이 임포트된 전용 그림에 연결되는지 확인한다.</summary>
    public class PerkIconMapTests
    {
        PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown()
        {
            Time.timeScale = 1f; _log?.Dispose(); _log = null;
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
        }

        [UnityTest]
        public IEnumerator EveryPerkHasImportedEffectArtwork()
        {
            GameData data = null; string err = null;
            yield return DataLoader.Load(d => data = d, e => err = e);
            Assert.IsNull(err, err);
            Assert.IsNotNull(data);
            Assert.That(data.Perks.Perks.Count, Is.GreaterThan(0));
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float start = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - start < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap이 App을 초기화해야 한다");
            yield return null;
            var cat = App.I.Assets;
            Assert.IsNotNull(cat, "빌드에서 AssetCatalog를 읽을 수 있어야 한다");
            var sprites = new HashSet<Sprite>();
            foreach (var p in data.Perks.Perks)
            {
                var key = PerkArtwork.Key(p.Id);
                Assert.IsNotNull(key, p.Id + " 전용 그림 매핑 누락");
                Assert.AreEqual(key, Icons.Perk(p.Id));
                var sprite = cat.Sprite(key);
                Assert.IsNotNull(sprite, p.Id + " 스프라이트 임포트/카탈로그 연결 누락");
                Assert.Greater(sprite.rect.width, 0);
                Assert.AreEqual(sprite.rect.width, sprite.rect.height, 1f, "정사각 아이콘");
                sprites.Add(sprite);
            }
            _log.AssertNoRed("특전 그림 전체 연결");
            Assert.Greater(sprites.Count, 40, "서로 다른 효과가 다시 소수 공용 그림에 합쳐지면 안 된다");
        }

        [Test]
        public void TriggersAndCollectorStatsRemainVisuallyDistinct()
        {
            Assert.AreNotEqual(Icons.Perk("p_collAtk"), Icons.Perk("p_collHp"));
            Assert.AreNotEqual(Icons.Perk("p_collAtk"), Icons.Perk("p_collCrit"));
            Assert.AreNotEqual(Icons.Perk("p_arrowEv"), Icons.Perk("p_killArrowN"));
            Assert.AreNotEqual(Icons.Perk("p_nArrowN"), Icons.Perk("p_killArrowN"));
            Assert.AreNotEqual(Icons.Perk("p_evadeHeal"), Icons.Perk("p_killHealN"));
            Assert.AreEqual(Icons.Perk("p_evadeHeal"), Icons.Perk("p_evHealL"));
            Assert.AreEqual(Icons.Perk("p_critR"), Icons.Perk("p_critRR"));
            Assert.AreEqual(Icons.Perk("p_thorns"), Icons.Perk("p_thornsL"));
            Assert.IsNull(PerkArtwork.Key("p_unknown_future_perk"));
        }
    }
}
