using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T237 — PvP 순위 보상 <b>구간표</b>(<see cref="ArenaRankData"/>)의 자. 주인 2026-09-08 09:3X «pvp 순위 보상은 등수별로 좀 있게 해 줘».
    /// <para>
    /// 자가 지키는 것은 둘이다 — ⓐ <b>주인이 준 구간 16개가 글자·수 그대로 파일에 있다</b> · ⓑ 어떤 등수도 <b>표 밖으로 떨어지지 않는다</b>(빈틈·겹침 0 · 마지막은 꼴등까지).
    /// ⓑ 가 이 표의 진짜 위험이다: 한 줄만 빠뜨려도 그 구간의 사람이 <b>조용히 아무것도 못 받는다</b>.
    /// </para>
    /// ⚠ <b>보상 «값» 은 주인이 아직 안 줬다</b> — 그래서 «지금은 비어 있다» 를 자로 못 박는다(누가 값을 지어내면 이 자가 먼저 운다 · §1).
    /// </summary>
    public class ArenaRankTests
    {
        static ArenaRankData Load() => ArenaRankData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "arena.json"))));

        static readonly string[] Labels =
        {
            "1", "2", "3", "4-5", "6-10", "11-20", "21-50", "51-100",
            "101-200", "201-500", "501-1000", "1001-3000", "3001-5000", "5001-10000", "10001-50000", "50001~꼴등",
        };

        [Test]
        public void 주인이_준_구간_16개가_글자_그대로_있다()
        {
            var d = Load();
            Assert.AreEqual(16, d.Tiers.Count, "주인이 준 구간 수");
            for (int i = 0; i < Labels.Length; i++)
                Assert.AreEqual(Labels[i], d.Tiers[i].Label, i + "번째 구간의 글자는 주인이 쓴 그대로여야 한다(화면이 이 글자를 띄운다)");
        }

        [Test]
        public void 어떤_등수도_표_밖으로_안_떨어진다()
        {
            var d = Load();
            Assert.AreEqual(1, d.Tiers[0].From, "1등부터 시작");
            for (int i = 1; i < d.Tiers.Count; i++)
                Assert.AreEqual(d.Tiers[i - 1].To + 1, d.Tiers[i].From, "구간이 빈틈·겹침 없이 이어져야 한다: " + d.Tiers[i].Label);
            Assert.IsTrue(d.Tiers[d.Tiers.Count - 1].Open, "마지막은 «꼴등까지»(to 0)");
            foreach (int rank in new[] { 1, 2, 3, 4, 5, 6, 10, 11, 20, 21, 50, 51, 100, 101, 200, 201, 500, 501, 1000, 1001, 3000, 3001, 5000, 5001, 10000, 10001, 50000, 50001, 999999 })
                Assert.IsNotNull(d.TierOf(rank), rank + "등이 드는 구간이 있어야 한다");
        }

        [Test]
        public void 경계_등수가_옳은_구간으로_간다()
        {
            var d = Load();
            Assert.AreEqual("1", d.TierOf(1).Label);
            Assert.AreEqual("3", d.TierOf(3).Label);
            Assert.AreEqual("4-5", d.TierOf(4).Label);
            Assert.AreEqual("4-5", d.TierOf(5).Label);
            Assert.AreEqual("6-10", d.TierOf(6).Label, "5 다음은 «4-5» 가 아니라 «6-10»");
            Assert.AreEqual("10001-50000", d.TierOf(50000).Label);
            Assert.AreEqual("50001~꼴등", d.TierOf(50001).Label);
            Assert.AreEqual("50001~꼴등", d.TierOf(12345678).Label, "꼴등 쪽은 끝이 없다");
            Assert.IsNull(d.TierOf(0), "0 등은 없다");
            Assert.IsNull(d.TierOf(-3), "음수 등수도 없다");
        }

        [Test]
        public void 주인이_못_박은_네_자리는_그_값_그대로다()
        {
            // 주인 2026-09-08 09:4X «1등 다이아 3000개 2등 2500개 3등 2300개 이런 식으로 줘 · 꼴등은 한 500 정도».
            // ⚠ 열여섯 줄을 다 베끼지 않는다 — 그러면 자가 표의 «거울» 이 되어 아무것도 못 지킨다(결정 555).
            //    주인이 입으로 말한 네 자리만 못 박고, 나머지는 아래 «단조 감소» 규칙이 지킨다.
            var d = Load();
            Assert.AreEqual(3000, Amount(d, 1), 1e-9, "1등");
            Assert.AreEqual(2500, Amount(d, 2), 1e-9, "2등");
            Assert.AreEqual(2300, Amount(d, 3), 1e-9, "3등");
            Assert.AreEqual(500, Amount(d, 50001), 1e-9, "꼴등 줄");
            Assert.AreEqual(500, Amount(d, 12345678), 1e-9, "꼴등 줄은 끝이 없다");
        }

        [Test]
        public void 위로_갈수록_많이_받는다()
        {
            // 주인 «이런 식으로 줘» 의 «식» 이 이것이다 — 위 구간이 아래 구간보다 적게 받는 일은 없어야 한다.
            // 값 자체는 표에서 오므로 자가 숫자를 안 베끼고 «규칙» 만 잰다(오타 한 자리는 여기서 걸린다).
            var d = Load();
            double prev = double.MaxValue;
            foreach (var t in d.Tiers)
            {
                Assert.AreEqual(1, t.Rewards.Count, t.Label + " 줄은 보상 한 칸(다이아)이다");
                Assert.AreEqual("gem", t.Rewards[0].Item, t.Label + " 줄의 보상 종류 = 다이아(주인 «1등 다이아 3000개»)");
                Assert.LessOrEqual(t.Rewards[0].Amount, prev, t.Label + " 줄이 윗줄보다 많이 받는다");
                Assert.Greater(t.Rewards[0].Amount, 0, t.Label + " 줄이 0 을 준다");
                prev = t.Rewards[0].Amount;
            }
            Assert.IsTrue(d.AnyRewards, "이제 표에 값이 있다(주인이 09:4X 에 줬다)");
        }

        /// <summary>그 등수가 받는 다이아(구간을 찾아 첫 칸을 읽는다).</summary>
        static double Amount(ArenaRankData d, int rank)
        {
            var t = d.TierOf(rank);
            Assert.IsNotNull(t, rank + "등이 드는 구간");
            Assert.AreEqual(1, t.Rewards.Count, t.Label + " 줄은 한 칸");
            return t.Rewards[0].Amount;
        }

        [Test]
        public void 값이_들어오면_그대로_읽힌다()
        {
            // 값이 온 뒤의 모습 — 파일만 채우면 코드도 화면도 안 고친다는 것을 여기서 미리 보인다.
            var d = ArenaRankData.Parse(@"{ ""tiers"": [
                { ""label"": ""1"", ""from"": 1, ""to"": 1, ""rewards"": [ { ""item"": ""arenaCoin"", ""amount"": 10000 }, { ""item"": ""gem"", ""amount"": 300 } ] },
                { ""label"": ""2~꼴등"", ""from"": 2, ""to"": 0, ""rewards"": [ { ""item"": ""arenaCoin"", ""amount"": 100 } ] } ] }");
            var top = d.TierOf(1);
            Assert.AreEqual(2, top.Rewards.Count, "줄마다 칸 수가 달라도 된다");
            Assert.AreEqual("arenaCoin", top.Rewards[0].Item); Assert.AreEqual(10000, top.Rewards[0].Amount, 1e-9);
            Assert.AreEqual("gem", top.Rewards[1].Item); Assert.AreEqual(300, top.Rewards[1].Amount, 1e-9);
            Assert.AreEqual(1, d.TierOf(9999).Rewards.Count);
            Assert.IsTrue(d.AnyRewards);
        }

        [Test]
        public void 표가_어긋나면_읽는_순간_운다()
        {
            // 빈틈 — 5 등이 어느 구간에도 안 든다
            Assert.Throws<System.FormatException>(() => ArenaRankData.Parse(
                @"{ ""tiers"": [ { ""label"": ""1-4"", ""from"": 1, ""to"": 4 }, { ""label"": ""6~꼴등"", ""from"": 6, ""to"": 0 } ] }"), "빈틈");
            // 겹침 — 3 등이 두 구간에 든다
            Assert.Throws<System.FormatException>(() => ArenaRankData.Parse(
                @"{ ""tiers"": [ { ""label"": ""1-3"", ""from"": 1, ""to"": 3 }, { ""label"": ""3~꼴등"", ""from"": 3, ""to"": 0 } ] }"), "겹침");
            // 1 등부터가 아니다
            Assert.Throws<System.FormatException>(() => ArenaRankData.Parse(
                @"{ ""tiers"": [ { ""label"": ""2~꼴등"", ""from"": 2, ""to"": 0 } ] }"), "1등이 빠졌다");
            // 마지막이 열려 있지 않다 — 그 아래 등수가 표 밖으로 떨어진다
            Assert.Throws<System.FormatException>(() => ArenaRankData.Parse(
                @"{ ""tiers"": [ { ""label"": ""1-100"", ""from"": 1, ""to"": 100 } ] }"), "꼴등까지가 없다");
            // 열린 구간이 가운데 있다
            Assert.Throws<System.FormatException>(() => ArenaRankData.Parse(
                @"{ ""tiers"": [ { ""label"": ""1~꼴등"", ""from"": 1, ""to"": 0 }, { ""label"": ""덤"", ""from"": 2, ""to"": 3 } ] }"), "열린 구간은 맨 끝에만");
            // 글자가 비었다 · 표가 비었다
            Assert.Throws<System.FormatException>(() => ArenaRankData.Parse(
                @"{ ""tiers"": [ { ""label"": """", ""from"": 1, ""to"": 0 } ] }"), "label 이 비었다");
            Assert.Throws<System.FormatException>(() => ArenaRankData.Parse(@"{ ""tiers"": [] }"), "표가 비었다");
        }
    }
}
