using System;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T98 — 챕터 보상(Chapter Chest · 주인 2026-09-07 «로비 → 클리어 보상» · 레퍼런스 32).
    /// 목표는 «그 챕터의 적을 전부 처치» 라 <b>새 카운터가 없다</b>(클리어했는가 = <c>maxChapter &gt; C</c>) · 수치는 표에서만 온다.
    /// </summary>
    public class ChapterChestTests
    {
        static GameData Load()
        {
            var d = TestData.Load();
            if (d.ChapterChest == null) d.ChapterChest = ChapterChestData.Parse(System.IO.File.ReadAllText(TestData.RepoFile("Assets/KkomaKnight/chapterChest.json")));
            return d;
        }
        static SaveData Fresh(GameData d) { var s = SaveData.NewSave(d); s.Gold = 0; s.Gem = 0; return s; }

        [Test]
        public void TableComesFromTheFile_NotFromCode()
        {
            var t = Load().ChapterChest;
            Assert.Greater(t.GemPer, 0, "다이아 계수");
            Assert.Greater(t.GoldClearMul, 0, "골드 배수");
            // 레퍼런스 32 의 «챕터 30 = 💎100» 과 같게 맞춘 계수다(그 값이 바뀌면 여기서 알린다)
            Assert.AreEqual(100.0, Math.Floor(t.GemBase + t.GemPer * 30), 1e-9, "챕터 30 다이아 = 레퍼런스 32 의 100");
        }

        [Test]
        public void GoalIsTheChaptersEnemyCount()
        {
            var d = Load(); var s = Fresh(d);
            foreach (var c in new[] { 1, 5, 30, 100 })
            {
                var info = ChapterChest.At(d, s, c);
                Assert.AreEqual(c, info.Chapter);
                Assert.AreEqual(d.Enemies.Chapter(c).EnemyCount, info.Kills, "목표 = 그 챕터 적 수(enemies.json)");
                Assert.Greater(info.Kills, 0);
            }
        }

        [Test]
        public void RewardGrowsWithTheChapter()
        {
            var d = Load(); var s = Fresh(d);
            var a = ChapterChest.At(d, s, 5); var b = ChapterChest.At(d, s, 30);
            Assert.Greater(b.Gem, a.Gem, "뒤 챕터가 다이아가 많다");
            Assert.Greater(b.Gold, a.Gold, "뒤 챕터가 골드가 많다");
            Assert.AreEqual(Math.Floor(d.ChapterChest.GoldClearMul * d.Tune.GoldClear(30)), b.Gold, 1e-9, "골드 = 배수 × goldClear(챕터)");
        }

        [Test]
        public void OnlyClearedChaptersAreClaimable()
        {
            var d = Load(); var s = Fresh(d);
            s.MaxChapter = 5;   // 1~4 를 깼고 5 에 도전 중
            Assert.IsTrue(ChapterChest.At(d, s, 4).Claimable, "깬 챕터는 받을 수 있다");
            Assert.IsFalse(ChapterChest.At(d, s, 5).Cleared, "도전 중인 챕터는 아직 아니다");
            Assert.IsFalse(ChapterChest.At(d, s, 5).Claimable);
            Assert.IsFalse(ChapterChest.At(d, s, 9).Claimable, "안 깬 챕터");
        }

        [Test]
        public void ClaimPaysOnceAndRemembersIt()
        {
            var d = Load(); var s = Fresh(d);
            s.MaxChapter = 5;
            var info = ChapterChest.At(d, s, 3);
            Assert.IsTrue(ChapterChest.Claim(d, s, 3, out var gem, out var gold), "받기");
            Assert.AreEqual(info.Gem, gem); Assert.AreEqual(info.Gold, gold);
            Assert.IsTrue(ChapterChest.At(d, s, 3).Claimed, "받았다고 남는다");
            Assert.IsFalse(ChapterChest.At(d, s, 3).Claimable, "두 번은 못 받는다");
            Assert.IsFalse(ChapterChest.Claim(d, s, 3, out var g2, out var o2), "두 번째 Claim 은 false");
            Assert.AreEqual(0.0, g2); Assert.AreEqual(0.0, o2);
            Assert.IsFalse(ChapterChest.Claim(d, s, 5, out _, out _), "안 깬 챕터는 못 받는다");
        }

        [Test]
        public void RedDotAndFirstOpenFollowTheClaimState()
        {
            var d = Load(); var s = Fresh(d);
            Assert.IsFalse(ChapterChest.AnyClaimable(d, s), "아무것도 안 깼으면 빨간 점 없음");
            s.MaxChapter = 4;
            Assert.IsTrue(ChapterChest.AnyClaimable(d, s));
            Assert.AreEqual(1, ChapterChest.FirstOpen(d, s), "받을 수 있는 가장 앞 챕터");
            ChapterChest.Claim(d, s, 1, out _, out _);
            Assert.AreEqual(2, ChapterChest.FirstOpen(d, s));
            ChapterChest.Claim(d, s, 2, out _, out _); ChapterChest.Claim(d, s, 3, out _, out _);
            Assert.IsFalse(ChapterChest.AnyClaimable(d, s), "다 받으면 빨간 점 없음");
            Assert.AreEqual(4, ChapterChest.FirstOpen(d, s), "받을 게 없으면 도전 중인 챕터 자리");
        }

        [Test]
        public void SaveRoundTripsAndOldSavesStillLoad()
        {
            var d = Load(); var s = Fresh(d);
            s.MaxChapter = 6; ChapterChest.Claim(d, s, 2, out _, out _); ChapterChest.Claim(d, s, 5, out _, out _);
            var back = SaveData.FromJson(s.ToJson(), d);
            Assert.IsTrue(back.ChestClaimed.Contains(2) && back.ChestClaimed.Contains(5), "받은 챕터가 왕복한다");
            Assert.AreEqual(2, back.ChestClaimed.Count);
            // 이 필드가 없던 옛 세이브 — 빈 목록으로 열린다(세이브 버전은 그대로다)
            var old = SaveData.FromJson("{\"v\":2,\"gold\":10,\"gem\":5,\"maxChapter\":3,\"selChapter\":1}", d);
            Assert.IsNotNull(old.ChestClaimed); Assert.AreEqual(0, old.ChestClaimed.Count);
            Assert.IsTrue(ChapterChest.AnyClaimable(d, old), "옛 세이브도 받을 게 있다");
        }

        [Test]
        public void NormalizeDropsChaptersOutOfRange()
        {
            var d = Load(); var s = Fresh(d);
            s.ChestClaimed.Add(0); s.ChestClaimed.Add(-3); s.ChestClaimed.Add(d.Tune.MaxChapter + 1); s.ChestClaimed.Add(2);
            s.Normalize(d);
            Assert.AreEqual(1, s.ChestClaimed.Count); Assert.IsTrue(s.ChestClaimed.Contains(2));
        }

        [Test]
        public void OutOfRangeChapterGivesAnEmptyInfo()
        {
            var d = Load(); var s = Fresh(d);
            Assert.AreEqual(0, ChapterChest.At(d, s, 0).Chapter);
            Assert.AreEqual(0, ChapterChest.At(d, s, d.Tune.MaxChapter + 1).Chapter);
        }
    }
}
