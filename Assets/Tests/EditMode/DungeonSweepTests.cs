using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T228 — 소탕 규칙(<see cref="DungeonSweep"/>)의 자. 주인 2026-09-08 07:4X «도전을 해서 <b>클리어를 했었던 챕터만</b> 소탕이 가능한 건데».
    /// 순수 C# 이라 <c>dotnet test</c> 가 매 커밋 잰다 — 화면 쪽(21 팝업 «소탕» 버튼 · Dim · 토스트 · 도전이 던전 키를 싣는가)은 PlayMode <c>DungeonTicketPlayTests</c> 가 본다.
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
            Assert.AreEqual("아직 클리어한 적이 없습니다", DungeonSweep.Why(s, d, "hell", Today), "화면 토스트가 그대로 쓸 까닭");
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
            Assert.AreEqual("티켓이 없습니다", DungeonSweep.Why(s, d, "hell", Today), "까닭이 «층» 이 아니라 «티켓» 이어야 한다");
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
            Assert.AreEqual("티켓이 없습니다", DungeonSweep.Why(s, d, "hell", Today));
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

        // ───────── T241 — «판을 실제로 깼을 때» 받는 보상(소탕과 다른 길) ─────────

        /// <summary>
        /// 주인 표 그대로: 지옥의 문 <b>첫 클리어 = 펫알 11 · 골드 1,000</b>, 그 뒤 클리어 = 펫알 5 · 골드 1,000.
        /// <para>여태 이 자리는 <b>주는 사람이 없었다</b> — 세부 팝업(21)이 표를 보여 주기만 했다(T241 · 결정 아래).</para>
        /// </summary>
        [Test]
        public void FirstClearPaysFirstAndLaterClearsPayClear()
        {
            var d = Table(); var s = Fresh(d);
            Assert.AreEqual(0, DungeonSweep.Floor(s, "hell"), "아직 깬 적이 없다");

            var first = DungeonSweep.GrantClear(s, d, "hell");
            Assert.IsNotNull(first, "첫 클리어 보상");
            Assert.AreEqual(11, first.PetEgg, 1e-9); Assert.AreEqual(1000, first.Gold, 1e-9);
            Assert.AreEqual(11, s.PetEgg, 1e-9, "세이브에 실제로 들어간다");
            Assert.AreEqual(1000, s.Gold, 1e-9);
            Assert.AreEqual(1, DungeonSweep.Floor(s, "hell"), "«깬 적 있다» 가 같이 남는다 — 소탕의 조건");

            var again = DungeonSweep.GrantClear(s, d, "hell");
            Assert.IsNotNull(again);
            Assert.AreEqual(5, again.PetEgg, 1e-9, "두 번째부터는 clear 다(first 를 다시 주지 않는다)");
            Assert.AreEqual(16, s.PetEgg, 1e-9, "11 + 5");
            Assert.AreEqual(2000, s.Gold, 1e-9);
        }

        /// <summary>«첫» 은 <b>기록보다 먼저</b> 판정된다 — 부르는 쪽이 순서를 틀릴 자리를 없앴다(그 순서가 뒤집히면 첫 클리어가 영영 안 온다).</summary>
        [Test]
        public void TheFirstClearIsJudgedBeforeTheRecordIsWritten()
        {
            var d = Table(); var s = Fresh(d);
            DungeonSweep.Record(s, "hell", 1);          // 누가 먼저 기록을 남겨 버린 세상
            var got = DungeonSweep.GrantClear(s, d, "hell");
            Assert.AreEqual(5, got.PetEgg, 1e-9, "이미 기록이 있으면 그 판은 «첫» 이 아니다");
        }

        /// <summary>소탕과 갈린다 — 소탕은 티켓을 쓰고 <c>sweep</c> 을 주지만, 클리어는 <b>티켓을 안 쓴다</b>(판을 이미 돌았다).</summary>
        [Test]
        public void ClearingDoesNotSpendATicket()
        {
            var d = Table(); var s = Fresh(d);
            int before = DungeonTickets.Tickets(s, d, "hell", Today);
            DungeonSweep.GrantClear(s, d, "hell");
            Assert.AreEqual(before, DungeonTickets.Tickets(s, d, "hell", Today), "클리어 보상은 티켓을 안 먹는다");
        }

        /// <summary>표에 없는 키·빈 표·던전 판이 아닌 경우는 <c>null</c> 이고 재화가 한 톨도 안 움직인다.</summary>
        [Test]
        public void UnknownDungeonsPayNothing()
        {
            var d = Table(); var s = Fresh(d);
            Assert.IsNull(DungeonSweep.GrantClear(s, d, "noSuchDungeon"));
            Assert.IsNull(DungeonSweep.GrantClear(s, d, ""), "던전 판이 아니면(키가 없다) 아무 일도 없다");
            Assert.IsNull(DungeonSweep.GrantClear(s, null, "hell"));
            Assert.AreEqual(0, s.Gold, 1e-9); Assert.AreEqual(0, s.PetEgg, 1e-9);
            Assert.AreEqual(0, DungeonSweep.Floor(s, "noSuchDungeon"), "모르는 키는 기록도 안 남는다");
        }

        /// <summary>원정은 표에 펫알이 없다 — 없는 것을 지어내지 않는다(첫 5,800 → 이후 3,500).</summary>
        [Test]
        public void ExpeditionClearPaysGoldOnly()
        {
            var d = Table(); var s = Fresh(d);
            var first = DungeonSweep.GrantClear(s, d, "expedition");
            Assert.AreEqual(5800, first.Gold, 1e-9); Assert.AreEqual(0, first.PetEgg, 1e-9);
            var next = DungeonSweep.GrantClear(s, d, "expedition");
            Assert.AreEqual(3500, next.Gold, 1e-9);
            Assert.AreEqual(0, s.PetEgg, 1e-9);
        }

    }
}
