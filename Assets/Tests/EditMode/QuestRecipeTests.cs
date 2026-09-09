using System;
using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T292 — 주간 트랙 <b>90점 = 무작위 레시피 20개</b>(주인 2026-09-09 «주간 쪽에 90일 때 보상을 레시피 20개로 · 레시피는 무작위로 20개 줌»).
    /// <para>
    /// ⚠ 이 자의 요점 둘.
    /// ⓐ <b>«무작위» 는 «아무렇게나» 가 아니다</b> — 개수 합이 정확히 20 이고, 나온 이름이 전부 표의 부위이고, <b>시드가 같으면 결과도 같다</b>.
    ///   합이 19 여도 21 이어도 빨간 줄은 안 난다(주인 폰에서만 «덜 왔는데?» 로 나타난다).
    /// ⓑ <b>난수 없이 받으면 «아무것도 안 주고» false</b> — 골드만 주고 레시피를 빠뜨리면 그 칸은 «받았다» 로 잠기고 20개는 <b>영영</b> 안 온다.
    ///   반쪽으로 주느니 안 주는 쪽이 되돌릴 수 있다.
    /// </para>
    /// 수를 베끼지 않는다(결정 555) — 20 도 부위 목록도 <b>표에서 읽어</b> 견준다. 주인이 값을 바꾸면 이 자가 그대로 따라간다.
    /// </summary>
    public class QuestRecipeTests
    {
        static QuestData Table() =>
            QuestData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "quest.json"))));
        static RecipeData Recipe() =>
            RecipeData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "recipe.json"))));

        static readonly DateTime Mon = new DateTime(2026, 9, 7, 9, 0, 0);

        /// <summary>주간 트랙에서 «무작위 레시피» 를 주는 칸과 그 개수(자가 «세 번째 칸» 이라고 박지 않는다 — 표가 차례를 바꿔도 따라간다).</summary>
        static int RecipeStep(QuestData d, out int amount)
        {
            amount = 0;
            for (int i = 0; i < d.Weekly.Steps.Count; i++)
                foreach (var r in d.Weekly.Steps[i].Rewards)
                    if (r.Item == QuestRun.ItemRecipeRandom) { amount = (int)Math.Round(r.Amount); return i; }
            return -1;
        }

        /// <summary>그 칸이 열릴 만큼 주간 줄을 깬다(목표는 표에서 읽는다).</summary>
        static SaveData Opened(QuestData d, int step)
        {
            var s = new SaveData();
            QuestRun.Roll(s, d, Mon);
            foreach (var q in d.Weekly.Quests) QuestRun.Bump(s, q.Counter, q.Goal);
            Assert.IsTrue(QuestRun.CanClaim(s, d, false, step), "주간 줄을 다 깨면 그 칸이 열린다");
            return s;
        }

        static int TotalRecipes(SaveData s, RecipeData rd)
        {
            int n = 0; foreach (var p in rd.Parts) n += Recipes.Count(s, p); return n;
        }

        [Test]
        public void 표의_주간_트랙에_무작위_레시피_칸이_있다()
        {
            var d = Table();
            int step = RecipeStep(d, out int amount);
            Assert.GreaterOrEqual(step, 0, "주간 트랙에 «무작위 레시피» 를 주는 칸이 있다");
            Assert.AreEqual(20, amount, "주인이 말로 준 수 — 레시피 20개");
            Assert.AreEqual(90, d.Weekly.Steps[step].Points, "주인이 말로 준 자리 — 90점");
            // 주인 «보상을 레시피 20개로» — 골드는 뺐다. 그 칸에 다른 상품이 같이 남아 있으면 지시와 다르다.
            Assert.AreEqual(1, d.Weekly.Steps[step].Rewards.Count, "그 칸의 상품은 레시피 하나뿐이다(골드 3000 은 뺐다)");
        }

        [Test]
        public void 스무_번을_뽑으면_합이_스물이고_이름은_전부_표의_부위다()
        {
            var d = Table(); var rd = Recipe();
            RecipeStep(d, out int amount);
            var tally = QuestRun.RollRecipes(rd, new Mulberry32(12345), amount);
            int sum = 0; foreach (var kv in tally) sum += kv.Value;
            Assert.AreEqual(amount, sum, "뽑은 개수 합 = 표가 준 개수");
            foreach (var kv in tally)
                Assert.IsTrue(rd.Has(kv.Key), "부위 밖 이름이 나오면 안 된다: " + kv.Key);
        }

        [Test]
        public void 같은_시드면_같은_결과다()
        {
            var rd = Recipe();
            var a = QuestRun.RollRecipes(rd, new Mulberry32(777), 20);
            var b = QuestRun.RollRecipes(rd, new Mulberry32(777), 20);
            CollectionAssert.AreEquivalent(a, b, "시드가 같으면 부위별 개수도 같다");
        }

        [Test]
        public void 받으면_그만큼_레시피가_들어온다()
        {
            var d = Table(); var rd = Recipe();
            int step = RecipeStep(d, out int amount);
            var s = Opened(d, step);
            Assert.AreEqual(0, TotalRecipes(s, rd), "받기 전에는 없다");

            bool ok = QuestRun.Claim(s, d, false, step, new Mulberry32(2026), rd, out var given);
            Assert.IsTrue(ok, "난수와 표를 주면 받아진다");
            Assert.AreEqual(amount, TotalRecipes(s, rd), "세이브에 들어온 합 = 표가 준 개수");

            int sum = 0; foreach (var kv in given) sum += kv.Value;
            Assert.AreEqual(amount, sum, "돌려준 «부위별 몇 개» 의 합도 같다(리워드 팝업이 이 값으로 묶는다)");
            foreach (var kv in given)
                Assert.AreEqual(kv.Value, Recipes.Count(s, kv.Key), "부위마다 돌려준 수 = 세이브에 든 수: " + kv.Key);

            Assert.IsFalse(QuestRun.Claim(s, d, false, step, new Mulberry32(1), rd), "같은 칸을 두 번 받을 수 없다");
            Assert.AreEqual(amount, TotalRecipes(s, rd), "두 번째 시도로 더 들어오지 않는다");
        }

        [Test]
        public void 난수_없이_부르면_아무것도_안_주고_칸도_안_잠긴다()
        {
            var d = Table(); var rd = Recipe();
            int step = RecipeStep(d, out _);
            var s = Opened(d, step);
            double gold0 = s.Gold, gem0 = s.Gem;

            Assert.IsFalse(QuestRun.Claim(s, d, false, step), "난수를 안 주면 false(옛 네 인자 갈래)");
            Assert.IsFalse(QuestRun.Claim(s, d, false, step, null, rd), "난수가 null 이어도 false");
            Assert.IsFalse(QuestRun.Claim(s, d, false, step, new Mulberry32(1), null), "표가 null 이어도 false");

            Assert.AreEqual(0, TotalRecipes(s, rd), "레시피가 한 개도 안 들어온다");
            Assert.AreEqual(gold0, s.Gold, 1e-9, "다른 재화도 안 움직인다");
            Assert.AreEqual(gem0, s.Gem, 1e-9, "다른 재화도 안 움직인다");
            // ⓑ 의 핵심 — 못 준 칸이 «받았다» 로 잠기면 그 20개는 영영 안 온다
            Assert.IsTrue(QuestRun.CanClaim(s, d, false, step), "칸이 아직 열려 있다(«받았다» 로 잠기지 않았다)");
            Assert.IsTrue(QuestRun.Claim(s, d, false, step, new Mulberry32(5), rd), "나중에 제대로 부르면 그때 받아진다");
        }

        /// <summary>
        /// ⚠ 이 회차가 <b>연 덫</b>을 막는 자다 — «<c>recipeRandom</c>» 은 <see cref="Mail"/> 이 모르고 <see cref="QuestRun.Claim"/> 만 아는 이름이고,
        /// <see cref="QuestRun"/> 은 <c>quest.json</c> 만 읽는다. 그래서 이 이름이 <b>다른 보상 표</b>(업적·아레나·출석·데일리 기프트·특권)에 적히면
        /// <see cref="Mail.Give"/> 가 <b>조용히 아무 일도 안 한다</b> — 화면에는 «레시피 20» 이라 뜨고 세이브에는 안 들어온다.
        /// <para>
        /// 표를 베끼는 것은 흔한 일이라(«quest.json 에 이렇게 적혀 있네») 이 덫은 <b>언젠가 밟힌다</b>.
        /// 지금 막는 값이 한 자이고, 안 막으면 그때는 «주인이 못 받았다» 로 나타난다.
        /// </para>
        /// 고치는 길 둘 — 그 표를 읽는 절이 <c>recipeRandom</c> 을 <b>직접 주게</b> 하거나(퀘스트가 그랬다),
        /// 부위가 정해진 <c>recipe.&lt;부위&gt;</c>(그것은 <see cref="Mail"/> 이 안다 · T290)로 적거나.
        /// </summary>
        [Test]
        public void 무작위_레시피_이름은_퀘스트_표에만_있다()
        {
            // Mail 이 «모른다» 는 것이 이 자의 전제다 — 누가 Mail 에 이름만 더하면(담을 자리 없이) 여기서 먼저 운다.
            Assert.IsFalse(Mail.CanPay(QuestRun.ItemRecipeRandom),
                "«" + QuestRun.ItemRecipeRandom + "» 은 재화가 아니다 — Mail 이 알게 만들려면 담을 자리(부위별 보유량)부터 정해야 한다");

            var dir = Path.GetDirectoryName(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "quest.json")));
            var offenders = new List<string>();
            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                if (Path.GetFileName(f) == "quest.json") continue;
                if (File.ReadAllText(f).Contains(QuestRun.ItemRecipeRandom)) offenders.Add(Path.GetFileName(f));
            }
            CollectionAssert.IsEmpty(offenders,
                "이 표들이 «" + QuestRun.ItemRecipeRandom + "» 을 적었는데 그 표를 읽는 절은 그 이름을 못 준다 — 조용히 안 들어간다: "
                + string.Join(", ", offenders));
        }

        [Test]
        public void 다른_칸은_난수_없이도_그대로_받아진다()
        {
            var d = Table(); var rd = Recipe();
            int step = RecipeStep(d, out _);
            var s = Opened(d, step);
            // 레시피가 없는 칸은 이 회차가 바꾼 것이 없다 — 옛 갈래로 계속 받아져야 한다
            for (int i = 0; i < d.Weekly.Steps.Count; i++)
            {
                if (i == step) continue;
                if (!QuestRun.CanClaim(s, d, false, i)) continue;
                Assert.IsTrue(QuestRun.Claim(s, d, false, i), "레시피가 없는 칸 " + i + " 은 옛 갈래 그대로");
            }
            Assert.AreEqual(0, TotalRecipes(s, rd), "그 칸들은 레시피를 주지 않는다");
        }
    }
}
