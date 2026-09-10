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
            // 주인 지시 원문 — 챕터당 3개 · 챕터 1 의 한 단마다 다이아 100 · 골드 1000(T137)
            Assert.AreEqual(3, t.Steps, "챕터당 보상 칸 수");
            Assert.AreEqual(100.0, t.GemAt(1), 1e-9, "챕터 1 한 단 다이아");
            Assert.AreEqual(1000.0, t.GoldAt(1), 1e-9, "챕터 1 한 단 골드");
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
                    Assert.AreEqual(d.ChapterChest.GemAt(c), info.Gem, 1e-9, "한 챕터 안에서는 단마다 같은 다이아");
                    Assert.AreEqual(d.ChapterChest.GoldAt(c), info.Gold, 1e-9, "한 챕터 안에서는 단마다 같은 골드");
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
            Assert.AreEqual(d.ChapterChest.GemAt(1) * 3, gemAll, 1e-9, "챕터 1 을 다 받으면 다이아 300");
            Assert.AreEqual(d.ChapterChest.GoldAt(1) * 3, goldAll, 1e-9, "챕터 1 을 다 받으면 골드 3000");
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

        // ───────────────────────── T268 ⓒ — 챕터 100까지의 보상 곡선 ─────────────────────────
        // 주인 2026-09-09 «챕터 100까지 수치 만들어 놓으쇼». 곡선 자체는 워커가 정한 것이라(결정 기록)
        // 자는 «주인이 입으로 못 박은 것» 만 잰다(결정 555): 챕터 1 = 옛 값 · 오를수록 커진다 ·
        // 100챕터가 1챕터의 수십 배 · 딱 떨어지는 수 · 표 밖 규칙. 짚은 값 몇 개는 곡선이 조용히
        // 바뀌는 것을 잡으려고 같이 박아 둔다(바꿀 때 이 줄이 같이 빨개져야 «바꿨다» 가 보인다).

        [Test]
        public void RewardGrowsWithTheChapter_AndStartsAtTheOldFixedValue()
        {
            var t = Load().ChapterChest;
            Assert.AreEqual(100.0, t.GemAt(1), 1e-9, "챕터 1 은 T137 이 준 값에서 시작한다(다이아)");
            Assert.AreEqual(1000.0, t.GoldAt(1), 1e-9, "챕터 1 은 T137 이 준 값에서 시작한다(골드)");

            // 짚은 값(이 곡선의 실제 표) — gemGrowth 1.035 · goldGrowth 1.05 · 반올림 10·100
            Assert.AreEqual(1100.0, t.GoldAt(2), 1e-9, "챕터 2 골드");
            Assert.AreEqual(540.0, t.GemAt(50), 1e-9, "챕터 50 다이아");
            Assert.AreEqual(10900.0, t.GoldAt(50), 1e-9, "챕터 50 골드");
            Assert.AreEqual(2910.0, t.GemAt(99), 1e-9, "챕터 99 다이아");
            Assert.AreEqual(119300.0, t.GoldAt(99), 1e-9, "챕터 99 골드");
            Assert.AreEqual(3010.0, t.GemAt(100), 1e-9, "챕터 100 다이아");
            Assert.AreEqual(125200.0, t.GoldAt(100), 1e-9, "챕터 100 골드");

            // 주인이 못 박은 «수십 배» — 100챕터가 1챕터의 20배 이상(지금 다이아 30.1배 · 골드 125.2배)
            Assert.GreaterOrEqual(t.GemAt(100) / t.GemAt(1), 20.0, "100챕터 다이아가 1챕터의 수십 배");
            Assert.GreaterOrEqual(t.GoldAt(100) / t.GoldAt(1), 20.0, "100챕터 골드가 1챕터의 수십 배");
        }

        [Test]
        public void RewardNeverGoesDown_AndLandsOnRoundNumbers()
        {
            var t = Load().ChapterChest;
            double pg = -1, po = -1;
            for (int c = 1; c <= 420; c++)
            {
                double g = t.GemAt(c), o = t.GoldAt(c);
                // ⚠ «비감소» 다 — 반올림 단위보다 복리 증가분이 작은 앞머리(챕터 1~2 다이아)는 같은 값이 이어진다.
                Assert.GreaterOrEqual(g, pg, "챕터 " + c + " 다이아가 앞 챕터보다 작아졌다");
                Assert.GreaterOrEqual(o, po, "챕터 " + c + " 골드가 앞 챕터보다 작아졌다");
                Assert.AreEqual(0.0, g % t.GemRound, 1e-9, "챕터 " + c + " 다이아가 " + t.GemRound + " 단위로 안 떨어진다");
                Assert.AreEqual(0.0, o % t.GoldRound, 1e-9, "챕터 " + c + " 골드가 " + t.GoldRound + " 단위로 안 떨어진다");
                pg = g; po = o;
            }
            Assert.Greater(t.GemAt(100), t.GemAt(1), "끝내 커지긴 한다(다이아)");
            Assert.Greater(t.GoldAt(100), t.GoldAt(1), "끝내 커지긴 한다(골드)");
        }

        [Test]
        public void OutsideTheTableRepeatsTheLastChapter()
        {
            var d = Load(); var t = d.ChapterChest;
            Assert.AreEqual(100, t.TableMax, "표가 덮는 마지막 챕터(주인 «챕터 100까지»)");
            // ⚑ «표 밖 챕터가 실제로 있다» 로는 안 잰다(T325) — 챕터 수는 주인이 420 → 100 으로 줄이기로 한 값이라
            //   그 문장은 밸런스가 바뀌는 날 거짓이 되고, 그때 이 자는 «규칙이 깨졌다» 가 아니라 «수가 바뀌었다» 로 운다.
            //   대신 표 밖이 있든 없든 늘 참인 것을 잰다 — **게임의 모든 챕터가 보상 값을 받는다**:
            //   표가 게임보다 좁으면 마지막 칸이 그 뒤를 다 답하고, 게임 전체를 덮으면 표가 직접 답한다.
            for (int c = 1; c <= d.Tune.MaxChapter; c += 7)
                Assert.Greater(t.GemAt(c), 0.0, "챕터 " + c + " 가 보상 없는 칸이다");
            Assert.AreEqual(t.GemAt(t.TableMax), t.GemAt(d.Tune.MaxChapter), 1e-9, "마지막 챕터는 표의 마지막 칸이 답한다");
            foreach (var c in new[] { 101, 200, 420 })
            {
                Assert.AreEqual(t.GemAt(t.TableMax), t.GemAt(c), 1e-9, "챕터 " + c + " 다이아 = 100챕터 값 그대로");
                Assert.AreEqual(t.GoldAt(t.TableMax), t.GoldAt(c), 1e-9, "챕터 " + c + " 골드 = 100챕터 값 그대로");
            }
            Assert.AreEqual(0.0, t.GemAt(0), 1e-9, "챕터 0 은 없는 칸");
            Assert.AreEqual(0.0, t.GoldAt(-1), 1e-9, "음수 챕터도 없는 칸");
        }

        [Test]
        public void AWholeHighChapterGivesThatChaptersValueTimesSteps()
        {
            var d = Load(); var s = Fresh(d); int steps = d.ChapterChest.Steps;
            const int C = 50;
            s.MaxChapter = C + 1;                       // C 는 이미 깬 챕터 = 전멸로 친다
            double gemAll = 0, goldAll = 0;
            for (int st = 1; st <= steps; st++)
            {
                Assert.IsTrue(ChapterChest.Claim(d, s, C, st, out var g, out var o), "챕터 " + C + " 단 " + st);
                gemAll += g; goldAll += o;
            }
            Assert.AreEqual(d.ChapterChest.GemAt(C) * steps, gemAll, 1e-9, "그 챕터 값 × 단 수(다이아)");
            Assert.AreEqual(d.ChapterChest.GoldAt(C) * steps, goldAll, 1e-9, "그 챕터 값 × 단 수(골드)");
            Assert.Greater(gemAll, d.ChapterChest.GemAt(1) * steps, "챕터 1 을 다 받은 것보다 많다");
        }

        [Test]
        public void BrokenCurveIsAnExceptionAtParseTime()
        {
            // 조용히 반대로 도는 표를 만들지 못하게 — 읽는 순간 예외다.
            const string ok = "{\"steps\":3,\"gem1\":100,\"gold1\":1000,\"gemGrowth\":1.035,\"goldGrowth\":1.05,\"gemRound\":10,\"goldRound\":100,\"tableMax\":100}";
            Assert.DoesNotThrow(() => ChapterChestData.Parse(ok), "멀쩡한 표");
            Assert.Throws<FormatException>(() => ChapterChestData.Parse(ok.Replace("\"gemGrowth\":1.035", "\"gemGrowth\":0.9")), "복리가 1 보다 작으면 챕터가 오를수록 작아진다");
            Assert.Throws<FormatException>(() => ChapterChestData.Parse(ok.Replace("\"goldRound\":100", "\"goldRound\":0")), "반올림 단위 0");
            Assert.Throws<FormatException>(() => ChapterChestData.Parse(ok.Replace("\"tableMax\":100", "\"tableMax\":0")), "표 밖 규칙이 가리킬 챕터가 없다");
            Assert.Throws<FormatException>(() => ChapterChestData.Parse(ok.Replace("\"gem1\":100", "\"gem1\":-1")), "음수 첫값");
        }
    }
}
