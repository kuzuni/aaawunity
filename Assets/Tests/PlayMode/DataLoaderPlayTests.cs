using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>플레이 모드에서 StreamingAssets 경로로 실제 데이터가 올라오는가 (플랫폼 경로 검사).</summary>
    public class DataLoaderPlayTests
    {
        [UnityTest]
        public IEnumerator StreamingAssetsDataLoads()
        {
            GameData got = null; string err = null;
            yield return DataLoader.Load(d => got = d, e => err = e);
            Assert.IsNull(err, err);
            Assert.IsNotNull(got);
            Assert.AreEqual(got.Tune.MaxChapter, got.Enemies.Chapters.Count);
        }
    }
}
