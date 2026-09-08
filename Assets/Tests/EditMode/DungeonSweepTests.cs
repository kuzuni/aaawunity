using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T228 — 소탕 규칙(<see cref="DungeonSweep"/>)의 자. 주인 2026-09-08 07:4X «도전을 해서 <b>클리어를 했었던 챕터만</b> 소탕이 가능한 건데».
    /// 순수 C# 이라 <c>dotnet test</c> 가 매 커밋 잰다 — 화면 배선(21 팝업 «소탕» 버튼)은 다음 회차 몫이고 여기서는 규칙만 못 박는다.
    /// <b>표를 그대로 쓴다</b>: 값은 <c>dungeon.json</c> 에서 오고 이 자는 «어느 갈래를 읽는가»(<c>sweep</c> ↔ <c>first</c>)를 지킨다.
    /// </summary>
    public class DungeonSweepTests
    {
        const string Today = "2026-09-08";

        static DungeonData Table()
        {
            var d = new DungeonData { DailyRefill = 2, GemCost = 50, AdPerDay = 1, GemPerDay = 1 };
            d.Dungeons.Add(new DungeonData.Entry
            {
                Key = "hell",
                First = new DungeonData.Reward { PetEgg = 11, Gold = 1000 },
                Clear = new DungeonData.Reward { PetEgg = 5, Gold = 1000 },
                Sweep = new DungeonData.Reward { PetEgg = 5, Gold = 1000 },
            });
            d.Dungeons.Add(new DungeonData.Entry
            {
                Key = "expedition",
                First = new DungeonData.Reward { Gold = 5800 },
                Clear = new DungeonData.Reward { Gold = 3500 },
                Sweep = new DungeonData.Reward { Gold = 3500 },
            });
            return d;
        }

        static SaveData Fresh(DungeonData d)
        {
            var s = new SaveData();
            DungeonTickets.Roll(s, d, Today);   // 티켓 2 개로 채운다
            return s;
        }

        [Test]
        public void 클리어한_층이_없으면_소탕을_못_한다()
        {
            var d = Table(); var s = Fresh(d);
            Assert.AreEqual(0, DungeonSweep.Floor(s, "hell"), "새 세이브는 클리어한 층 0");
            Assert.IsFalse(DungeonSweep.Can(s, d, "hell", Today), "층이 0 이면 티켓이 있어도 못 한다");
            Assert.AreEqual("클리어한 층이 없다", DungeonSweep.Why(s, d, "hell", Today), "화면 토스트가 그대로 쓸 까닭");
            Assert.IsNull(DungeonSweep.Prize(s, d, "hell", Today), "못 하면 보상도 없다");
        }

        [Test]
        public void 클리어한_층이_있으면_되고_보상은_sweep_이다()
        {
            var d = Table(); var s = Fresh(d);
            DungeonSweep.Record(s, "hell", 3);
            Assert.IsTrue(DungeonSweep.Can(s, d, "hell", Today));
            Assert.AreEqual("", DungeonSweep.Why(s, d, "hell", Today), "될 때는 까닭이 없다");
            var p = DungeonSweep.Prize(s, d, "hell", Today);
            Assert.IsNotNull(p);
            Assert.AreEqual(5, p.PetEgg, "지옥의 문 소탕 = 펫알 5(첫 클리어 11 이 아니다)");
            Assert.AreEqual(1000, p.Gold);
        }

        [Test]
        public void 첫클리어_총액은_소탕에서_절대_안_읽는다()
        {
            var d = Table(); var s = Fresh(d);
            DungeonSweep.Record(s, "hell", 1);
            var p = DungeonSweep.Prize(s, d, "hell", Today);
            var first = d.Of("hell").First;
            Assert.AreNotEqual(first.PetEgg, p.PetEgg, "주인 «최초 보상은 안 주고 나머지만»");
            Assert.AreEqual(d.Of("hell").Sweep.PetEgg, p.PetEgg, "소탕은 표의 sweep 갈래");
            // 원정은 골드만 준다 — 첫 클리어 5800 ↔ 소탕 3500
            DungeonSweep.Record(s, "expedition", 1);
            Assert.AreEqual(3500, DungeonSweep.Prize(s, d, "expedition", Today).Gold);
            Assert.AreEqual(5800, d.Of("expedition").First.Gold, "표의 first 는 그대로 남아 있다(다른 길이 쓴다)");
        }

        [Test]
        public void 층은_올라가기만_한다()
        {
            var d = Table(); var s = Fresh(d);
            Assert.IsTrue(DungeonSweep.Record(s, "hell", 3), "처음 3층");
            Assert.IsFalse(DungeonSweep.Record(s, "hell", 2), "더 낮은 층을 다시 깨도 기록은 안 내려간다");
            Assert.AreEqual(3, DungeonSweep.Floor(s, "hell"));
            Assert.IsTrue(DungeonSweep.Record(s, "hell", 5), "더 높으면 올라간다");
            Assert.AreEqual(5, DungeonSweep.Floor(s, "hell"));
            Assert.IsFalse(DungeonSweep.Record(s, "hell", 0), "0 이하는 기록이 아니다");
            Assert.IsFalse(DungeonSweep.Record(s, "", 1), "키가 없으면 아무 일도 없다");
        }

        [Test]
        public void 던전마다_따로_센다()
        {
            var d = Table(); var s = Fresh(d);
            DungeonSweep.Record(s, "hell", 2);
            Assert.AreEqual(2, DungeonSweep.Floor(s, "hell"));
            Assert.AreEqual(0, DungeonSweep.Floor(s, "expedition"), "지옥문을 깼다고 원정이 열리지 않는다");
            Assert.IsFalse(DungeonSweep.Can(s, d, "expedition", Today));
        }

        [Test]
        public void 티켓이_없으면_층이_있어도_못_한다()
        {
            var d = Table(); var s = Fresh(d);
            DungeonSweep.Record(s, "hell", 1);
            while (DungeonTickets.Tickets(s, d, "hell", Today) > 0) DungeonTickets.Spend(s, d, "hell", Today);
            Assert.IsFalse(DungeonSweep.Can(s, d, "hell", Today));
            Assert.AreEqual("티켓이 없다", DungeonSweep.Why(s, d, "hell", Today), "까닭이 «층» 이 아니라 «티켓» 이어야 한다");
        }

        [Test]
        public void 세이브에_담기고_다시_읽힌다()
        {
            var d = Table(); var s = Fresh(d);
            DungeonSweep.Record(s, "hell", 4);
            var back = SaveData.FromJson(s.ToJson(), TestData.Load());
            Assert.AreEqual(4, DungeonSweep.Floor(back, "hell"), "던전별 최고 층이 세이브를 건너간다");
            Assert.AreEqual(0, DungeonSweep.Floor(back, "expedition"));
        }

        [Test]
        public void 옛_세이브에_그_칸이_없어도_안_깨진다()
        {
            var d = Table();
            var old = SaveData.FromJson("{\"gold\":10}", TestData.Load());   // dunFloor·petEgg 가 없는 옛 세이브
            Assert.AreEqual(0, DungeonSweep.Floor(old, "hell"), "없으면 0 — 옛 세이브 호환");
            Assert.AreEqual(0, old.PetEgg, 1e-9, "펫알 칸도 «없으면 0»");
            Assert.IsFalse(DungeonSweep.Can(old, d, "hell", Today));
        }

        // ───────────────────────── 2단계 ⓐ — «주기»(Grant · 펫알 저장 자리) ─────────────────────────

        [Test]
        public void 소탕하면_티켓_한_장을_쓰고_표의_sweep_을_받는다()
        {
            var d = Table(); var s = Fresh(d);
            DungeonSweep.Record(s, "hell", 3);
            int before = DungeonTickets.Tickets(s, d, "hell", Today);
            var got = DungeonSweep.Grant(s, d, "hell", Today);
            Assert.IsNotNull(got, "될 때는 보상을 돌려준다(화면이 그대로 띄운다)");
            Assert.AreEqual(5, got.PetEgg, 1e-9); Assert.AreEqual(1000, got.Gold, 1e-9);
            Assert.AreEqual(before - 1, DungeonTickets.Tickets(s, d, "hell", Today), "티켓 정확히 1 장");
            Assert.AreEqual(1000, s.Gold, 1e-9, "골드가 세이브에 들어갔다");
            Assert.AreEqual(5, s.PetEgg, 1e-9, "펫알도 «버리지 않고» 세이브에 쌓인다 — 1단계에서 뗐던 반쪽");
        }

        [Test]
        public void 못_하는_판이면_티켓도_보상도_안_움직인다()
        {
            var d = Table(); var s = Fresh(d);   // 층 0 = 못 한다
            int before = DungeonTickets.Tickets(s, d, "hell", Today);
            Assert.IsNull(DungeonSweep.Grant(s, d, "hell", Today), "못 하면 null");
            Assert.AreEqual(before, DungeonTickets.Tickets(s, d, "hell", Today), "티켓이 그대로다(«쓰고 못 받는» 일이 없어야 한다)");
            Assert.AreEqual(0, s.Gold, 1e-9); Assert.AreEqual(0, s.PetEgg, 1e-9);
        }

        [Test]
        public void 티켓이_다_떨어질_때까지만_되고_그_뒤로는_안_준다()
        {
            var d = Table(); var s = Fresh(d);
            DungeonSweep.Record(s, "hell", 1);
            int n = 0;
            while (DungeonSweep.Grant(s, d, "hell", Today) != null) { n++; Assert.Less(n, 10, "무한히 주면 안 된다"); }
            Assert.AreEqual(2, n, "하루 티켓 2 장 = 소탕 2 번");
            Assert.AreEqual(2000, s.Gold, 1e-9); Assert.AreEqual(10, s.PetEgg, 1e-9, "두 번치가 쌓인다");
            Assert.AreEqual("티켓이 없다", DungeonSweep.Why(s, d, "hell", Today));
        }

        [Test]
        public void 펫알은_세이브를_건너간다()
        {
            var d = Table(); var s = Fresh(d);
            DungeonSweep.Record(s, "hell", 2);
            DungeonSweep.Grant(s, d, "hell", Today);
            var back = SaveData.FromJson(s.ToJson(), TestData.Load());
            Assert.AreEqual(5, back.PetEgg, 1e-9, "펫알이 세이브 왕복을 견딘다");
            Assert.AreEqual(1000, back.Gold, 1e-9);
        }

        [Test]
        public void 원정_소탕은_골드만_주고_펫알은_안_준다()
        {
            var d = Table(); var s = Fresh(d);
            DungeonSweep.Record(s, "expedition", 1);
            var got = DungeonSweep.Grant(s, d, "expedition", Today);
            Assert.AreEqual(3500, got.Gold, 1e-9, "표의 sweep(첫 클리어 5800 이 아니다)");
            Assert.AreEqual(0, s.PetEgg, 1e-9, "원정은 표에 펫알이 없다 — 없는 것을 지어내지 않는다");
        }
    }
}
