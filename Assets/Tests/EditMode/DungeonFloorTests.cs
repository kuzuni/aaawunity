using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T291 1회차 — 원정의 <b>«층»</b> 과 층별 보상(주인 2026-09-09 05:5X «원정 보상은 레시피를 부위 순서대로 —
    /// 1층 투구 2개 첫클리어, 아닌 거 1개 / 2층 신발 4·2 / 3층 무기 6·3 … · 3의 배수 층마다 파랑·보라·노랑 키»).
    /// <para>
    /// 재는 것: ⓐ 주인이 든 표 그대로(1·2·3·6·7·9·12층 · 첫 / 그 뒤) ⓑ 키는 3의 배수 층 첫 클리어에만, 차례대로 순환
    /// ⓒ <see cref="DungeonSweep.Record"/> 가 층을 올리고 내려가지 않는다 ⓓ 소탕 = 최고층의 «첫 아님» ⓔ <c>floors</c> 가 없는 던전(지옥의 문)은 옛 그대로
    /// ⓕ <c>recipeOrder</c> 여섯이 <c>gear.json</c> 의 부위 집합과 같다.
    /// </para>
    /// ⚠ <b>수는 표에서 읽는다</b>(<c>dungeon.json</c>) — 자에 2·1·3 을 다시 적으면 표를 고칠 때 자가 거짓말을 한다(결정 555).
    /// 다만 <b>주인이 말로 준 표</b>(1층 투구 2 · 3층 파랑 …)는 «그 말이 지켜지는가» 를 재는 것이라 그대로 적는다 — 그 둘은 다른 물음이다.
    /// </summary>
    public class DungeonFloorTests
    {
        static DungeonData D() => DungeonData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "dungeon.json"))));
        static DungeonData.Entry Exp() => D().Of("expedition");
        static DungeonData.Entry Hell() => D().Of("hell");

        [Test]
        public void ExpeditionHasFloorsAndHellDoesNot()
        {
            Assert.IsNotNull(Exp().Floors, "원정에는 층이 있다(주인이 원정만 말했다)");
            Assert.IsNull(Hell().Floors, "지옥의 문은 층을 안 센다 — floors 가 없으면 옛 한 벌 보상 그대로다");
        }

        /// <summary>주인이 말로 준 표를 그대로 — 부위·개수·키.</summary>
        [Test]
        public void TheOwnersTableFloorByFloor()
        {
            var e = Exp();
            // 층 → (부위 · 첫클리어 개수 · 그 뒤 개수 · 첫클리어 키)
            var want = new List<(int floor, string part, double first, double clear, string key)>
            {
                (1,  "helm",   2,  1,  null),
                (2,  "boot",   4,  2,  null),
                (3,  "weapon", 6,  3,  GachaKeys.Blue),
                (4,  "armor",  8,  4,  null),
                (5,  "glove",  10, 5,  null),
                (6,  "neck",   12, 6,  GachaKeys.Purple),
                (7,  "helm",   14, 7,  null),      // 여섯을 돌아 다시 투구
                (9,  "weapon", 18, 9,  GachaKeys.Yellow),
                (12, "neck",   24, 12, GachaKeys.Blue),   // 키도 셋을 돌아 다시 파랑
            };
            foreach (var w in want)
            {
                var f = DungeonSweep.FloorReward(e, w.floor, first: true);
                var c = DungeonSweep.FloorReward(e, w.floor, first: false);
                Assert.AreEqual(w.part, f.RecipePart, $"{w.floor}층 부위");
                Assert.AreEqual(w.first, f.Recipe, 1e-9, $"{w.floor}층 첫 클리어 레시피 개수");
                Assert.AreEqual(w.part, c.RecipePart, $"{w.floor}층 부위(그 뒤)");
                Assert.AreEqual(w.clear, c.Recipe, 1e-9, $"{w.floor}층 그 뒤 레시피 개수");

                if (w.key == null) Assert.AreEqual(0, f.Key, 1e-9, $"{w.floor}층은 키를 안 준다");
                else { Assert.AreEqual(w.key, f.KeyItem, $"{w.floor}층 키"); Assert.Greater(f.Key, 0, $"{w.floor}층 키 개수"); }
                Assert.AreEqual(0, c.Key, 1e-9, $"{w.floor}층 키는 첫 클리어에만 난다(keyFirstOnly)");
            }
        }

        /// <summary>골드는 모든 층에 표 값 그대로 얹힌다 — 주인이 «골드 대신» 이라고 하지 않았다.</summary>
        [Test]
        public void GoldRidesOnEveryFloorUnchanged()
        {
            var e = Exp();
            for (int floor = 1; floor <= 13; floor++)
            {
                Assert.AreEqual(e.First.Gold, DungeonSweep.FloorReward(e, floor, true).Gold, 1e-9, $"{floor}층 첫 클리어 골드");
                Assert.AreEqual(e.Clear.Gold, DungeonSweep.FloorReward(e, floor, false).Gold, 1e-9, $"{floor}층 클리어 골드");
            }
        }

        /// <summary>표를 절대 안 건드린다 — 셈이 표 객체를 고치면 그다음 층이 오염된다.</summary>
        [Test]
        public void TheTableIsNeverMutatedByTheMath()
        {
            var e = Exp();
            double gold0 = e.Clear.Gold;
            var a = DungeonSweep.FloorReward(e, 5, false);
            a.Gold = -999; a.Recipe = -1; a.RecipePart = "허깨비";
            Assert.AreEqual(gold0, e.Clear.Gold, 1e-9, "표의 골드가 그대로여야 한다");
            Assert.AreEqual("", e.Clear.RecipePart, "표의 한 벌에는 층 레시피가 안 적힌다");
            var b = DungeonSweep.FloorReward(e, 5, false);
            Assert.AreEqual(gold0, b.Gold, 1e-9, "다음 셈이 앞 셈에 안 물든다");
        }

        /// <summary>도전 층 = 최고층 + 1 · 기록은 내려가지 않는다 · 층 없는 던전은 언제나 1.</summary>
        [Test]
        public void ChallengeIsTopPlusOneAndRecordNeverGoesDown()
        {
            var d = D(); var s = new SaveData();
            Assert.AreEqual(1, DungeonSweep.Challenge(s, d, "expedition"), "아무것도 안 깼으면 1층에 도전한다");
            Assert.AreEqual(1, DungeonSweep.Challenge(s, d, "hell"), "층이 없는 던전은 언제나 1");

            DungeonSweep.Record(s, "expedition", 1);
            Assert.AreEqual(2, DungeonSweep.Challenge(s, d, "expedition"));
            DungeonSweep.Record(s, "expedition", 5);
            Assert.AreEqual(6, DungeonSweep.Challenge(s, d, "expedition"));
            Assert.IsFalse(DungeonSweep.Record(s, "expedition", 3), "더 낮은 층을 다시 깨도 최고 기록은 그대로");
            Assert.AreEqual(5, DungeonSweep.Floor(s, "expedition"));
        }

        /// <summary>깨면 그 층 보상이 실제로 세이브에 들어온다 — 첫 클리어 뒤 같은 층을 다시 깨면 «그 뒤» 값이다.</summary>
        [Test]
        public void ClearingAFloorActuallyPaysRecipesAndKeys()
        {
            var d = D(); var s = new SaveData();

            var p1 = DungeonSweep.GrantClear(s, d, "expedition");          // 1층 첫 클리어
            Assert.AreEqual("helm", p1.RecipePart);
            Assert.AreEqual(2, Recipes.Count(s, "helm"), "투구 레시피 2개가 실제로 들어온다");
            Assert.AreEqual(1, DungeonSweep.Floor(s, "expedition"), "최고층이 1 이 된다");

            DungeonSweep.GrantClear(s, d, "expedition");                   // 2층 첫 클리어
            Assert.AreEqual(4, Recipes.Count(s, "boot"), "2층은 신발 4개");

            var p3 = DungeonSweep.GrantClear(s, d, "expedition");          // 3층 첫 클리어 = 파란 키
            Assert.AreEqual(6, Recipes.Count(s, "weapon"), "3층은 무기 6개");
            Assert.AreEqual(GachaKeys.Blue, p3.KeyItem);
            Assert.AreEqual(1, GachaKeys.Count(s, GachaKeys.Blue), 1e-9, "3층 첫 클리어에 파란 키 1");

            // 같은 3층을 다시 깬다(도전 층은 4 이므로 층을 직접 준다) — 이번엔 «그 뒤» 값이고 키는 없다
            double gold0 = s.Gold;
            var again = DungeonSweep.GrantClear(s, d, "expedition", 3);
            Assert.AreEqual(3, again.Recipe, 1e-9, "다시 깨면 무기 3개");
            Assert.AreEqual(9, Recipes.Count(s, "weapon"), "6 + 3");
            Assert.AreEqual(1, GachaKeys.Count(s, GachaKeys.Blue), 1e-9, "키는 두 번 안 난다");
            Assert.Greater(s.Gold, gold0, "골드는 다시 깨도 들어온다");
            Assert.AreEqual(3, DungeonSweep.Floor(s, "expedition"), "낮은 층을 다시 깨도 최고층은 그대로");
        }

        /// <summary>소탕 = 최고층의 «첫 아님» 보상(주인 «클리어한 최고층 보상 · 최초 보상은 안 줌»).</summary>
        [Test]
        public void SweepPaysTheTopFloorsNonFirstReward()
        {
            var d = D(); var s = new SaveData(); const string Today = "2026-09-09";
            DungeonSweep.Record(s, "expedition", 3);
            DungeonTickets.Roll(s, d, Today);

            var prize = DungeonSweep.Prize(s, d, "expedition", Today);
            Assert.AreEqual("weapon", prize.RecipePart, "3층 = 무기");
            Assert.AreEqual(3, prize.Recipe, 1e-9, "소탕은 «첫 아님» 이라 3개");
            Assert.AreEqual(0, prize.Key, 1e-9, "소탕으로는 키가 안 난다(keyFirstOnly)");

            var got = DungeonSweep.Grant(s, d, "expedition", Today);
            Assert.IsNotNull(got, "티켓이 있으면 소탕이 된다");
            Assert.AreEqual(3, Recipes.Count(s, "weapon"), "소탕 보상이 실제로 들어온다");
        }

        /// <summary>층이 없는 던전(지옥의 문)은 한 톨도 안 바뀐다 — 옛 표 한 벌 그대로다.</summary>
        [Test]
        public void HellIsUntouchedByTheFloorRules()
        {
            var d = D(); var e = Hell(); var s = new SaveData();
            Assert.AreSame(e.First, DungeonSweep.FloorReward(e, 1, true), "층이 없으면 표의 한 벌을 그대로 준다");
            Assert.AreSame(e.Clear, DungeonSweep.FloorReward(e, 7, false), "층수를 줘도 표 그대로");

            var p = DungeonSweep.GrantClear(s, d, "hell");
            Assert.AreEqual(e.First.PetEgg, p.PetEgg, 1e-9, "첫 클리어 = 표의 first");
            Assert.AreEqual(1, DungeonSweep.Floor(s, "hell"), "«깬 적 있다» 는 1 그대로");
            Assert.AreEqual(0, Recipes.Count(s, "helm"), "레시피는 안 준다");
            var p2 = DungeonSweep.GrantClear(s, d, "hell");
            Assert.AreEqual(e.Clear.PetEgg, p2.PetEgg, 1e-9, "두 번째부터는 clear");
            Assert.AreEqual(1, DungeonSweep.Floor(s, "hell"), "층은 여전히 1");
        }

        /// <summary>표의 부위 여섯이 <c>gear.json</c> 의 부위 집합과 같다 — 한쪽만 늘면 없는 부위 레시피가 난다.</summary>
        [Test]
        public void RecipeOrderMatchesTheGearParts()
        {
            var floors = Exp().Floors;
            var parts = new List<string>(RecipeData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "recipe.json")))).Parts);
            Assert.AreEqual(parts.Count, floors.RecipeOrder.Count, "부위 수가 같아야 한다");
            foreach (var p in floors.RecipeOrder) Assert.Contains(p, parts, "층 표의 부위가 레시피 표에 있어야 한다: " + p);
            foreach (var p in parts) Assert.Contains(p, floors.RecipeOrder, "레시피 표의 부위가 층 차례에 있어야 한다: " + p);
        }

        /// <summary>
        /// T291 6회차 — <b>층의 적 세기 = 챕터 몇</b>(기본 «N층 = 챕터 N» · 수는 표의 <c>chapterPerFloor</c>).
        /// <para>표의 마지막 챕터를 넘지 않는다 — 넘으면 적 표에 없는 칸을 묻게 된다. 층이 없는 던전은 <b>0</b>(= 부르는 쪽이 정하던 대로).</para>
        /// </summary>
        [Test]
        public void FloorChapterFollowsTheTableAndStopsAtTheLastChapter()
        {
            var e = Exp(); const int Max = 420;
            Assert.AreEqual(1, DungeonSweep.FloorChapter(e, 1, Max), "1층 = 챕터 1");
            Assert.AreEqual(5, DungeonSweep.FloorChapter(e, 5, Max), "5층 = 챕터 5");
            Assert.AreEqual(37, DungeonSweep.FloorChapter(e, 37, Max));
            Assert.AreEqual(Max, DungeonSweep.FloorChapter(e, Max + 50, Max), "표의 마지막 챕터를 넘지 않는다");
            Assert.AreEqual(1, DungeonSweep.FloorChapter(e, 1, 0), "상한을 모르면(0) 깎지 않는다");
            Assert.AreEqual(0, DungeonSweep.FloorChapter(Hell(), 3, Max), "층이 없는 던전은 0 — 부르는 쪽이 정하던 대로 간다");
            Assert.AreEqual(0, DungeonSweep.FloorChapter(e, 0, Max), "층이 0 이면 셀 것이 없다");
        }

        /// <summary>수는 표에 있다 — 코드에 «1» 이 박혀 있지 않다는 것을 «표를 바꾸면 답이 바뀐다» 로 잰다.</summary>
        [Test]
        public void TheDifficultyNumberLivesInTheTableNotInTheCode()
        {
            var e = Exp();
            double keep = e.Floors.ChapterPerFloor;
            try
            {
                e.Floors.ChapterPerFloor = 3;
                Assert.AreEqual(15, DungeonSweep.FloorChapter(e, 5, 420), "표를 3 으로 바꾸면 5층 = 챕터 15");
                e.Floors.ChapterPerFloor = 0.5;
                Assert.AreEqual(3, DungeonSweep.FloorChapter(e, 5, 420), "0.5 면 5층 = 챕터 2.5 → 반올림 3");
                e.Floors.ChapterPerFloor = 0;
                Assert.AreEqual(1, DungeonSweep.FloorChapter(e, 5, 420), "0 이어도 챕터 1 아래로는 안 간다");
            }
            finally { e.Floors.ChapterPerFloor = keep; }
        }

        /// <summary>키 이름은 <see cref="GachaKeys"/> 가 아는 것이어야 한다 — 모르는 이름이면 지급이 조용히 사라진다.</summary>
        [Test]
        public void KeyNamesAreOnesTheKeyCodeKnows()
        {
            foreach (var k in Exp().Floors.KeyOrder)
                Assert.IsTrue(GachaKeys.IsKey(k), "층 표의 키 이름을 GachaKeys 가 알아야 한다: " + k);
        }
    }
}
