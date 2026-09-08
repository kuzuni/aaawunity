using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T259 4항 — 상점의 «하루 1번» 자리 넷(<see cref="ShopFree"/>)이 <b>서로 독립으로</b> 하루를 센다.
    /// <para>
    /// 재는 것: ⓐ 하루 1회 · <b>같은 날 두 번째는 막힌다</b> ⓑ 날이 바뀌면 다시 된다 ⓒ 넷이 서로 안 잠근다
    /// ⓓ 못 쓸 때는 <b>아무것도 안 바꾼다</b> ⓔ 옛 이름 <c>FreeDay</c> 와 새 표가 <b>같은 칸</b>을 본다
    /// ⓕ 세이브 왕복 · 옛 세이브의 <c>freeDay</c> 는 다이아 칸으로 옮겨진다.
    /// </para>
    /// <para>
    /// ⚑ <b>ⓐ 가 이 자의 존재 이유다.</b> 지시서 T259 절 머리가 지목한 구멍이 그것이다 — 빗장은 여태 `ShopScreen.OnFree` 첫 줄에만 있었고
    /// <b>그것을 지워도 자가 전부 초록</b>이었다(= 무제한 보급). 재화를 주는 규칙에 «두 번째는 안 준다» 를 재는 자가 없으면
    /// 그 규칙은 다음 리팩터링에서 조용히 사라진다.
    /// </para>
    /// </summary>
    public class ShopFreeTests
    {
        const string D1 = "2026-09-09", D2 = "2026-09-10";

        static SaveData Fresh() => SaveData.NewSave(TestData.Load());

        [Test]
        public void OncePerDay_TheSecondTryOnTheSameDayIsBlocked()
        {
            var s = Fresh();
            Assert.IsTrue(ShopFree.Can(s, ShopFree.Gem, D1), "받은 적이 없으니 오늘 몫이 있다");
            Assert.IsTrue(ShopFree.Take(s, ShopFree.Gem, D1), "첫 번째는 된다");

            Assert.IsFalse(ShopFree.Can(s, ShopFree.Gem, D1), "같은 날 두 번째는 «있다» 고 하면 안 된다");
            Assert.IsFalse(ShopFree.Take(s, ShopFree.Gem, D1), "같은 날 두 번째는 막힌다 — 이 한 줄이 없으면 무제한 보급이다");
            Assert.AreEqual(D1, ShopFree.DayOf(s, ShopFree.Gem), "막힌 시도는 날짜 도장도 안 건드린다");
        }

        [Test]
        public void ANewDayOpensItAgain()
        {
            var s = Fresh();
            ShopFree.Take(s, ShopFree.Gem, D1);
            Assert.IsTrue(ShopFree.Can(s, ShopFree.Gem, D2), "날이 바뀌면 다시 된다");
            Assert.IsTrue(ShopFree.Take(s, ShopFree.Gem, D2));
            Assert.AreEqual(D2, ShopFree.DayOf(s, ShopFree.Gem));
        }

        /// <summary>
        /// 이 자가 T259 를 부른 까닭이다 — 날짜 도장이 하나뿐이던 시절에는 <b>다이아를 받은 날 골드가 같이 잠겼다</b>.
        /// 자리를 하나씩 쓰면서 «나머지 셋은 그대로인가» 를 매번 확인한다.
        /// </summary>
        [Test]
        public void TheFourSlotsDoNotLockEachOther()
        {
            var s = Fresh();
            for (int i = 0; i < ShopFree.All.Length; i++)
            {
                var mine = ShopFree.All[i];
                Assert.IsTrue(ShopFree.Take(s, mine, D1), mine + " 은 오늘 아직 안 썼다");
                for (int k = i + 1; k < ShopFree.All.Length; k++)
                    Assert.IsTrue(ShopFree.Can(s, ShopFree.All[k], D1),
                        mine + " 을 썼다고 " + ShopFree.All[k] + " 까지 잠기면 안 된다(날짜 도장 하나로 두던 시절의 그 결함이다)");
            }
            foreach (var t in ShopFree.All)
                Assert.IsFalse(ShopFree.Can(s, t, D1), t + " 은 오늘 몫을 다 썼다");
        }

        /// <summary>주인이 준 자리는 넷이고 <b>신화 상자는 없다</b> — 말하지 않은 자리를 만들지 않았다는 것을 자로 못 박는다(지시서 1항).</summary>
        [Test]
        public void TheSlotsAreExactlyTheFourTheOwnerNamed()
        {
            CollectionAssert.AreEquivalent(
                new[] { "freeGem", "freeGold", "adBoxRare", "adBoxLegend" }, ShopFree.All,
                "주인이 말한 것은 다이아 100 · 골드 1,000 · 희귀 상자 · 전설 상자 넷이다(신화 상자는 말한 적 없다)");
            Assert.AreEqual(1, ShopFree.PerDay, "주인이 준 것은 «1회» 뿐이다");
        }

        [Test]
        public void BadArgumentsChangeNothing()
        {
            var s = Fresh();
            Assert.IsFalse(ShopFree.Can(s, ShopFree.Gem, ""), "«오늘» 을 모르면 «된다» 고 하지 않는다 — 모르는 채로 주면 하루에 몇 번이든 된다");
            Assert.IsFalse(ShopFree.Take(s, ShopFree.Gem, ""));
            Assert.IsFalse(ShopFree.Take(s, "", D1));
            Assert.IsFalse(ShopFree.Take(null, ShopFree.Gem, D1));
            Assert.AreEqual(0, s.FreeDays.Count, "막힌 시도는 표에 아무것도 안 남긴다");
        }

        /// <summary>옛 이름과 새 표가 <b>같은 칸</b>을 본다 — 갈라지면 «화면은 받았다는데 규칙은 안 받았다» 가 된다.</summary>
        [Test]
        public void TheOldNameAndTheNewTableAreTheSameCell()
        {
            var s = Fresh();
            s.FreeDay = D1;
            Assert.AreEqual(D1, ShopFree.DayOf(s, ShopFree.Gem), "옛 이름으로 쓰면 새 표에 보인다");
            Assert.IsFalse(ShopFree.Can(s, ShopFree.Gem, D1));

            ShopFree.Take(s, ShopFree.Gem, D2);
            Assert.AreEqual(D2, s.FreeDay, "새 절로 쓰면 옛 이름으로 읽힌다");

            Assert.AreEqual("", Fresh().FreeDay, "한 번도 안 받았으면 빈 값(옛 기본값 그대로)");
        }

        [Test]
        public void SaveRoundTripKeepsAllFourAndMigratesTheOldField()
        {
            var d = TestData.Load();
            var s = SaveData.NewSave(d);
            ShopFree.Take(s, ShopFree.Gem, D1);
            ShopFree.Take(s, ShopFree.BoxLegend, D2);
            var back = SaveData.FromJson(s.ToJson(), d);
            Assert.AreEqual(D1, ShopFree.DayOf(back, ShopFree.Gem));
            Assert.AreEqual(D2, ShopFree.DayOf(back, ShopFree.BoxLegend));
            Assert.AreEqual("", ShopFree.DayOf(back, ShopFree.Gold), "안 쓴 자리는 빈 값이다");

            // 옛 세이브 — `freeDays` 가 없고 `freeDay` 만 있다(T259 4항 마이그레이션)
            var old = SaveData.FromJson("{\"gold\":10,\"freeDay\":\"" + D1 + "\"}", d);
            Assert.AreEqual(D1, ShopFree.DayOf(old, ShopFree.Gem), "옛 freeDay 는 다이아 자리로 옮겨진다");
            Assert.IsFalse(ShopFree.Can(old, ShopFree.Gem, D1), "옛 세이브의 «오늘 이미 받았다» 가 살아남는다 — 안 그러면 판을 바꿀 때마다 한 번씩 더 받는다");
            Assert.IsTrue(ShopFree.Can(old, ShopFree.Gold, D1), "나머지 셋은 «아직 안 받았다»");

            var blank = SaveData.FromJson("{\"gold\":10}", d);
            Assert.AreEqual(0, blank.FreeDays.Count, "아무 날짜도 없는 세이브는 빈 표다(빈 문자열을 채워 넣지 않는다)");
        }

        /// <summary>
        /// 새 판에서 오늘 받은 뒤 <b>옛 판으로 갔다 온</b> 세이브 — 두 값이 다르면 <b>새 표가 이긴다</b>.
        /// 옛 값이 이기면 «오늘 몫이 되살아나는» 자리가 되고, 그것은 정확히 보급을 두 번 받는 길이다.
        /// </summary>
        [Test]
        public void WhenTheTwoDisagreeTheNewTableWins()
        {
            var d = TestData.Load();
            var s = SaveData.FromJson("{\"freeDay\":\"" + D1 + "\",\"freeDays\":{\"freeGem\":\"" + D2 + "\"}}", d);
            Assert.AreEqual(D2, ShopFree.DayOf(s, ShopFree.Gem), "새 표가 이긴다");
            Assert.IsFalse(ShopFree.Can(s, ShopFree.Gem, D2), "그래서 그 날의 몫은 이미 쓴 것으로 남는다");
        }
    }
}
