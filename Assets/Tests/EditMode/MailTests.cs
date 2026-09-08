using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T243 — 우편함의 <b>내용물 규칙</b>(<see cref="Mail"/>). 주인 2026-09-08 11:5X
    /// «우편함으로는 <b>아레나 보상만</b> 오게 하고 나머지는 걍 <b>즉시 지급</b>해 … 우편함은 아레나 보상만.»
    /// <para>
    /// 이 자가 지키는 것 셋 — ⓐ <b>아레나가 아닌 것은 못 들어온다</b>(주인 문장을 코드가 «거절» 로 들고 있다)
    /// ⓑ <b>담을 자리가 없는 보상은 애초에 안 받는다</b>(받기를 눌러도 아무 일 없는 우편 = 조용히 잃는 보상 · T228 결정 633 과 같은 갈래)
    /// ⓒ <b>같은 우편이 두 번 쌓이지 않는다</b>(정산이 두 번 돌아도 두 배로 안 준다).
    /// </para>
    /// </summary>
    public class MailTests
    {
        static MailItem Arena(string id, params (string item, double amount)[] rewards)
        {
            var m = new MailItem { Id = id, Kind = Mail.KindArena, Title = "아레나 순위 보상" };
            foreach (var r in rewards) m.Rewards.Add(new ArenaRankData.Reward { Item = r.item, Amount = r.amount });
            return m;
        }

        [Test]
        public void 아레나_보상만_들어온다()
        {
            var s = new SaveData();
            Assert.IsTrue(Mail.Add(s, Arena("a1", (Mail.ItemGold, 1000))), "아레나 우편은 들어온다");
            var other = Arena("b1", (Mail.ItemGold, 1000)); other.Kind = "expedition";
            Assert.IsFalse(Mail.Add(s, other), "탐험 보상은 우편으로 안 온다(주인 «나머지는 즉시 지급»)");
            other.Kind = "dailyGift";
            Assert.IsFalse(Mail.Add(s, other), "데일리 기프트도 마찬가지");
            Assert.AreEqual(1, Mail.Pending(s).Count, "들어온 것은 아레나 하나뿐");
        }

        [Test]
        public void 담을_자리가_없는_보상은_애초에_안_받는다()
        {
            var s = new SaveData();
            Assert.IsFalse(Mail.Add(s, Arena("a1", ("도장", 3))), "세이브에 자리가 없는 이름은 거절 — 받아 두고 조용히 버리지 않는다");
            Assert.IsFalse(Mail.Add(s, Arena("a2", (Mail.ItemGold, 1000), ("도장", 3))), "한 칸만 못 담아도 통째로 거절(반쯤 주기 금지)");
            Assert.AreEqual(0, Mail.Pending(s).Count, "거절된 우편은 흔적도 안 남는다");
            Assert.IsFalse(Mail.Add(s, Arena("a3")), "빈 우편은 안 넣는다");
            Assert.IsFalse(Mail.Add(s, Arena("a4", (Mail.ItemGold, 0))), "0 짜리도 우편이 아니다");
            Assert.IsFalse(Mail.Add(s, Arena("", (Mail.ItemGold, 1))), "id 없는 우편은 두 번 막을 수가 없다");
            foreach (var item in new[] { Mail.ItemGold, Mail.ItemGem, Mail.ItemPetEgg, Mail.ItemArenaCoin })
                Assert.IsTrue(Mail.CanPay(item), item + " 은 세이브에 자리가 있다");
        }

        [Test]
        public void 같은_우편은_두_번_안_쌓인다()
        {
            var s = new SaveData();
            Assert.IsTrue(Mail.Add(s, Arena("s1", (Mail.ItemGold, 1000))));
            Assert.IsFalse(Mail.Add(s, Arena("s1", (Mail.ItemGold, 1000))), "정산이 두 번 돌아도 두 배로 안 준다");
            Assert.AreEqual(1, Mail.Pending(s).Count);
        }

        [Test]
        public void 받으면_세이브에_들어가고_목록에서_사라진다()
        {
            var s = new SaveData();
            Mail.Add(s, Arena("s1", (Mail.ItemGold, 1000), (Mail.ItemArenaCoin, 30)));
            Mail.Add(s, Arena("s2", (Mail.ItemGem, 50), (Mail.ItemPetEgg, 2)));
            Assert.IsTrue(Mail.Any(s));

            var got = Mail.Claim(s, "s1");
            Assert.AreEqual("골드 1,000 · 아레나 코인 30", got, "받은 것을 그대로 말한다(화면 토스트가 이 글자를 쓴다)");
            Assert.AreEqual(1000, s.Gold, 1e-9); Assert.AreEqual(30, s.ArenaCoin, 1e-9);
            Assert.AreEqual(1, Mail.Pending(s).Count, "받은 우편은 사라진다");

            Mail.Claim(s, "s2");
            Assert.AreEqual(50, s.Gem, 1e-9); Assert.AreEqual(2, s.PetEgg, 1e-9);
            Assert.IsFalse(Mail.Any(s), "다 받으면 빈 함");
            Assert.IsNull(Mail.Claim(s, "s1"), "없는 우편을 받으면 null — 두 번 지급되지 않는다");
            Assert.AreEqual(1000, s.Gold, 1e-9, "그리고 재화도 안 늘어난다");
        }

        [Test]
        public void 세이브를_건너간다()
        {
            var s = new SaveData();
            Mail.Add(s, Arena("s1", (Mail.ItemGold, 1000), (Mail.ItemArenaCoin, 30)));
            s.ArenaCoin = 7;
            var back = SaveData.FromJson(s.ToJson(), TestData.Load());
            Assert.AreEqual(1, Mail.Pending(back).Count, "우편이 세이브 왕복을 견딘다");
            var m = Mail.Pending(back)[0];
            Assert.AreEqual("s1", m.Id); Assert.AreEqual(Mail.KindArena, m.Kind); Assert.AreEqual("아레나 순위 보상", m.Title);
            Assert.AreEqual(2, m.Rewards.Count);
            Assert.AreEqual(Mail.ItemGold, m.Rewards[0].Item); Assert.AreEqual(1000, m.Rewards[0].Amount, 1e-9);
            Assert.AreEqual(7, back.ArenaCoin, 1e-9, "아레나 코인 칸도 건너간다");
        }

        [Test]
        public void 옛_세이브에_그_칸이_없어도_안_깨진다()
        {
            var old = SaveData.FromJson("{\"gold\":10}", TestData.Load());
            Assert.AreEqual(0, Mail.Pending(old).Count, "없으면 빈 함 — 옛 세이브 호환");
            Assert.IsFalse(Mail.Any(old));
            Assert.AreEqual(0, old.ArenaCoin, 1e-9);
            Assert.IsNull(Mail.Claim(old, "없는키"));
        }
    }
}
