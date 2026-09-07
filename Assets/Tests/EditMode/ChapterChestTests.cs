using System;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T137 — 챕터 보상(Chapter Chest · 주인 2026-09-07 «챕터 보상은 챕터당 3개 · 적 1/3 · 2/3 · 전부 처치 · 각 다이아 100 · 골드 1000»).
    /// 목표 셋은 <c>ceil(적 수 × 단 / 단 수)</c> 이고 진행도는 «그 챕터에서 잡아 본 최고 처치 수»(이기든 지든 남는다) · 수치는 표에서만 온다.
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
            // 주인 지시 원문 — 챕터당 3개 · 한 단마다 다이아 100 · 골드 1000
            Assert.AreEqual(3, t.Steps, "챕터당 보상 칸 수");
            Assert.AreEqual(100.0, t.Gem, 1e-9, "한 단 다이아");
            Assert.AreEqual(1000.0, t.Gold, 1e-9, "한 단 골드");
            Assert.AreEqual((1 << t.Steps) - 1, t.FullMask, "다 받음 비트");
        }

        [Test]
        public void ThreeGoalsAreOneThirdTwoThirdsAndAll()
        {
            var d = Load(); var s = Fresh(d); int steps = d.ChapterChest.Steps;
            foreach (var c in new[] { 1, 5, 30, 100 })
            {
                int n = d.Enemies.Chapter(c).EnemyCount;
                Assert.Greater(n, 0, "챕터 적 수");
                Assert.AreEqual((int)Math.Ceiling(n / 3.0), ChapterChest.Goal(d, c, 1), "1단 = 적 1/3");
                Assert.AreEqual((int)Math.Ceiling(n * 2 / 3.0), ChapterChest.Goal(d, c, 2), "2단 = 적 2/3");
                Assert.AreEqual(n, ChapterChest.Goal(d, c, steps), "마지막 단 = 전멸");
                for (int st = 2; st <= steps; st++)
                    Assert.GreaterOrEqual(ChapterChest.Goal(d, c, st), ChapterChest.Goal(d, c, st - 1), "목표는 단마다 커진다");
                Assert.AreEqual(0, ChapterChest.Goal(d, c, steps + 1), "없는 단");
                var info = ChapterChest.At(d, s, c, 1);
                Assert.AreEqual(c, info.Chapter); Assert.AreEqual(1, info.Step);
                Assert.AreEqual(n, info.EnemyCount, "적 수 = enemies.json 그대로");
            }
        }

        [Test]
        public void RewardIsTheSameEveryStep()
        {
            var d = Load(); var s = Fresh(d);
            foreach (var c in new[] { 1, 30 })
                for (int st = 1; st <= d.ChapterChest.Steps; st++)
                {
                    var info = ChapterChest.At(d, s, c, st);
                    Assert.AreEqual(d.ChapterChest.Gem, info.Gem, 1e-9, "단마다 같은 다이아");
                    Assert.AreEqual(d.ChapterChest.Gold, info.Gold, 1e-9, "단마다 같은 골드");
                }
        }

        [Test]
        public void ProgressComesFromKills_WinOrLose_AndClearedChaptersCountAsAll()
        {
            var d = Load(); var s = Fresh(d);
            int n = d.Enemies.Chapter(3).EnemyCount;
            s.MaxChapter = 3;   // 1~2 를 깼고 3 에 도전 중
            Assert.AreEqual(n, ChapterChest.Progress(d, s, 2), "깬 챕터는 전멸로 친다");
            Assert.AreEqual(0, ChapterChest.Progress(d, s, 3), "아직 도전 중인 챕터는 0 부터");

            ChapterChest.RecordKills(s, 3, 1);                        // 져도 남는다
            Assert.AreEqual(1, ChapterChest.Progress(d, s, 3));
            ChapterChest.RecordKills(s, 3, n);                        // 더 많이 잡았다
            Assert.AreEqual(n, ChapterChest.Progress(d, s, 3));
            ChapterChest.RecordKills(s, 3, 2);                        // 더 적게 잡은 판은 최고 기록을 못 깎는다
            Assert.AreEqual(n, ChapterChest.Progress(d, s, 3), "max(기존, 이번 처치)");
        }

        [Test]
        public void StepsUnlockOneByOneAsKillsGrow()
        {
            var d = Load(); var s = Fresh(d);
            s.MaxChapter = 2; s.SelChapter = 2;   // 2 에 도전 중
            int g1 = ChapterChest.Goal(d, 2, 1), g2 = ChapterChest.Goal(d, 2, 2), g3 = ChapterChest.Goal(d, 2, 3);
            Assert.IsFalse(ChapterChest.At(d, s, 2, 1).Claimable, "한 마리도 안 잡았으면 못 받는다");

            ChapterChest.RecordKills(s, 2, g1);
            Assert.IsTrue(ChapterChest.At(d, s, 2, 1).Claimable, "1/3 을 채우면 1단");
            Assert.IsFalse(ChapterChest.At(d, s, 2, 2).Claimable, "2단은 아직");
            ChapterChest.RecordKills(s, 2, g2);
            Assert.IsTrue(ChapterChest.At(d, s, 2, 2).Claimable, "2/3 을 채우면 2단");
            Assert.IsFalse(ChapterChest.At(d, s, 2, 3).Claimable, "전멸은 아직");
            ChapterChest.RecordKills(s, 2, g3);
            Assert.IsTrue(ChapterChest.At(d, s, 2, 3).Claimable, "전멸이면 3단");
        }

        [Test]
        public void ClaimPaysOncePerStepAndAWholeChapterGivesThreeTimes()
        {
            var d = Load(); var s = Fresh(d);
            s.MaxChapter = 3;   // 1~2 는 전멸로 친다
            double gemAll = 0, goldAll = 0;
            for (int st = 1; st <= d.ChapterChest.Steps; st++)
            {
                Assert.IsTrue(ChapterChest.Claim(d, s, 1, st, out var g, out var o), "단 " + st);
                gemAll += g; goldAll += o;
                Assert.IsFalse(ChapterChest.Claim(d, s, 1, st, out var g2, out var o2), "같은 단을 두 번 못 받는다");
                Assert.AreEqual(0.0, g2); Assert.AreEqual(0.0, o2);
            }
            Assert.AreEqual(d.ChapterChest.Gem * 3, gemAll, 1e-9, "한 챕터 다 받으면 다이아 300");
            Assert.AreEqual(d.ChapterChest.Gold * 3, goldAll, 1e-9, "한 챕터 다 받으면 골드 3000");
            Assert.IsFalse(ChapterChest.Claim(d, s, 3, 1, out _, out _), "처치 미달이면 못 받는다");
        }

        [Test]
        public void RedDotAndFirstOpenFollowTheStepState()
        {
            var d = Load(); var s = Fresh(d);
            Assert.IsFalse(ChapterChest.AnyClaimable(d, s), "한 마리도 안 잡았으면 빨간 점 없음");
            s.MaxChapter = 2;   // 1 을 깼다 → 1 챕터 세 단이 열린다
            Assert.IsTrue(ChapterChest.AnyClaimable(d, s));
            Assert.AreEqual(ChapterChest.Index(d, 1, 1), ChapterChest.FirstOpen(d, s), "받을 수 있는 첫 단");
            ChapterChest.Claim(d, s, 1, 1, out _, out _);
            Assert.AreEqual(ChapterChest.Index(d, 1, 2), ChapterChest.FirstOpen(d, s), "다음 단으로 넘어간다");
            ChapterChest.Claim(d, s, 1, 2, out _, out _); ChapterChest.Claim(d, s, 1, 3, out _, out _);
            Assert.IsFalse(ChapterChest.AnyClaimable(d, s), "다 받으면 빨간 점 없음");
            Assert.AreEqual(ChapterChest.Index(d, 2, 1), ChapterChest.FirstOpen(d, s), "받을 게 없으면 도전 중인 챕터의 첫 단");
        }

        [Test]
        public void CellIndexWalksChapterByChapterStepByStep()
        {
            var d = Load(); var s = Fresh(d); int steps = d.ChapterChest.Steps;
            Assert.AreEqual(0, ChapterChest.Index(d, 1, 1));
            Assert.AreEqual(steps, ChapterChest.Index(d, 2, 1), "다음 챕터는 단 수만큼 뒤");
            Assert.IsTrue(ChapterChest.Cell(d, steps + 1, out var c, out var st));
            Assert.AreEqual(2, c); Assert.AreEqual(2, st);
            Assert.IsFalse(ChapterChest.Cell(d, d.Tune.MaxChapter * steps, out _, out _), "마지막 챕터 뒤는 없다");
            s.MaxChapter = 3;
            Assert.AreEqual(ChapterChest.Index(d, 3, steps), ChapterChest.LastIndex(d, s), "페이지 끝 = 도전 중인 챕터의 마지막 단");
        }

        [Test]
        public void SaveRoundTripsAndOldSavesGetAllThreeSteps()
        {
            var d = Load(); var s = Fresh(d);
            s.MaxChapter = 6;
            ChapterChest.Claim(d, s, 2, 1, out _, out _); ChapterChest.Claim(d, s, 5, 3, out _, out _);
            ChapterChest.RecordKills(s, 6, 4);
            var back = SaveData.FromJson(s.ToJson(), d);
            Assert.IsTrue(ChapterChest.ClaimedStep(back, 2, 1), "받은 단이 왕복한다");
            Assert.IsFalse(ChapterChest.ClaimedStep(back, 2, 2));
            Assert.IsTrue(ChapterChest.ClaimedStep(back, 5, 3));
            Assert.AreEqual(4, back.ChestKills[6], "진행도가 왕복한다");

            // T98 옛 세이브(«받은 챕터 번호 목록») — 그 챕터는 «3단 다 받음» 으로 옮긴다(손해 0)
            var old = SaveData.FromJson("{\"v\":2,\"gold\":10,\"gem\":5,\"maxChapter\":4,\"selChapter\":1,\"chestClaimed\":[1,3]}", d);
            for (int st = 1; st <= d.ChapterChest.Steps; st++)
            {
                Assert.IsTrue(ChapterChest.ClaimedStep(old, 1, st), "옛 세이브 챕터 1 · 단 " + st);
                Assert.IsTrue(ChapterChest.ClaimedStep(old, 3, st), "옛 세이브 챕터 3 · 단 " + st);
                Assert.IsFalse(ChapterChest.ClaimedStep(old, 2, st), "안 받았던 챕터는 그대로 열려 있다");
            }
            Assert.AreEqual(d.ChapterChest.FullMask, old.ChestClaimed[1], "비트가 «다 받음» 으로 바뀐다");
            Assert.IsTrue(ChapterChest.AnyClaimable(d, old), "안 받은 챕터가 남아 빨간 점은 켜져 있다");

            // 이 필드가 아예 없던 세이브 — 빈 값으로 열린다(세이브 버전은 그대로다)
            var none = SaveData.FromJson("{\"v\":2,\"gold\":10,\"gem\":5,\"maxChapter\":3,\"selChapter\":1}", d);
            Assert.IsNotNull(none.ChestClaimed); Assert.AreEqual(0, none.ChestClaimed.Count);
            Assert.IsNotNull(none.ChestKills); Assert.AreEqual(0, none.ChestKills.Count);
            Assert.IsTrue(ChapterChest.AnyClaimable(d, none), "옛 세이브도 받을 게 있다");
        }

        [Test]
        public void NormalizeDropsWhatIsOutOfRange()
        {
            var d = Load(); var s = Fresh(d);
            s.ChestClaimed[0] = 1; s.ChestClaimed[-3] = 1; s.ChestClaimed[d.Tune.MaxChapter + 1] = 1;
            s.ChestClaimed[2] = 1 | (1 << 20);            // 없는 단 비트는 지운다
            s.ChestKills[0] = 5; s.ChestKills[d.Tune.MaxChapter + 1] = 5;
            s.ChestKills[3] = 99999;                      // 적 수를 넘지 않는다
            s.Normalize(d);
            Assert.AreEqual(1, s.ChestClaimed.Count); Assert.AreEqual(1, s.ChestClaimed[2]);
            Assert.AreEqual(1, s.ChestKills.Count);
            Assert.AreEqual(d.Enemies.Chapter(3).EnemyCount, s.ChestKills[3]);
        }

        [Test]
        public void OutOfRangeCellGivesAnEmptyInfo()
        {
            var d = Load(); var s = Fresh(d);
            Assert.AreEqual(0, ChapterChest.At(d, s, 0, 1).Chapter);
            Assert.AreEqual(0, ChapterChest.At(d, s, 1, 0).Chapter);
            Assert.AreEqual(0, ChapterChest.At(d, s, 1, d.ChapterChest.Steps + 1).Chapter);
            Assert.AreEqual(0, ChapterChest.At(d, s, d.Tune.MaxChapter + 1, 1).Chapter);
            Assert.IsFalse(ChapterChest.At(d, s, 0, 1).Claimable, "빈 칸은 못 받는다");
        }
    }
}
