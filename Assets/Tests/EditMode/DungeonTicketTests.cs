using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 던전 티켓 규칙(T99 · 주인 2026-09-07 «하루 2개 보충 — 2개 미만일 시에 2개로» · «광고 1개 · 50다이아 1개 · 둘 다 하루에 각각 1번씩, 던전당»)과
    /// 보상 표(지옥의 문 첫 클리어 펫알 11 + 골드 1,000 · 이후 펫알 5 + 골드 1,000 · 원정 첫 5,800 · 이후 3,500)를 못 박는다.
    /// 수치는 전부 <c>Assets/KkomaKnight/dungeon.json</c> 에서 온다 — 이 테스트는 그 파일의 값을 읽어 규칙만 검사한다(코드 상수 0).
    /// </summary>
    public class DungeonTicketTests
    {
        const string Json = @"{
          ""dailyRefill"": 2, ""gemCost"": 50, ""adPerDay"": 1, ""gemPerDay"": 1,
          ""dungeons"": [
            { ""key"": ""hell"", ""first"": { ""petEgg"": 11, ""gold"": 1000 }, ""clear"": { ""petEgg"": 5, ""gold"": 1000 }, ""sweep"": { ""petEgg"": 5, ""gold"": 1000 } },
            { ""key"": ""expedition"", ""first"": { ""gold"": 5800 }, ""clear"": { ""gold"": 3500 }, ""sweep"": { ""gold"": 3500 } }
          ]
        }";

        static DungeonData D() => DungeonData.Parse(Json);
        static SaveData S() => SaveData.NewSave(TestData.Load());

        [Test]
        public void RealFileMatchesTheOwnersNumbers()
        {
            var d = DungeonData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "dungeon.json"))));
            Assert.AreEqual(2, d.DailyRefill, "하루 보충 2개");
            Assert.AreEqual(50, d.GemCost, 1e-9, "티켓 다이아 값 50");
            Assert.AreEqual(1, d.AdPerDay, "광고 하루 1회"); Assert.AreEqual(1, d.GemPerDay, "다이아 하루 1회");
            var hell = d.Of("hell"); Assert.IsNotNull(hell, "지옥의 문");
            Assert.AreEqual(11, hell.First.PetEgg, 1e-9, "지옥의 문 첫 클리어 펫알 11(기본 5 + 첫 보너스 6)");
            Assert.AreEqual(1000, hell.First.Gold, 1e-9, "지옥의 문 첫 클리어 골드 1,000");
            Assert.AreEqual(5, hell.Clear.PetEgg, 1e-9, "이후 클리어 펫알 5"); Assert.AreEqual(1000, hell.Clear.Gold, 1e-9, "이후 클리어 골드 1,000");
            Assert.AreEqual(5, hell.Sweep.PetEgg, 1e-9, "소탕 펫알 5"); Assert.AreEqual(1000, hell.Sweep.Gold, 1e-9, "소탕 골드 1,000");
            var exp = d.Of("expedition"); Assert.IsNotNull(exp, "원정");
            Assert.AreEqual(5800, exp.First.Gold, 1e-9, "원정 첫 클리어 골드 5,800");
            Assert.AreEqual(3500, exp.Clear.Gold, 1e-9, "원정 클리어 골드 3,500"); Assert.AreEqual(3500, exp.Sweep.Gold, 1e-9, "원정 소탕 골드 3,500");
            Assert.AreEqual(0, exp.First.PetEgg, 1e-9, "원정은 펫알을 주지 않는다");
        }

        [Test]
        public void DayRollFillsUpToTwoButNeverAdds()
        {
            var d = D(); var s = S();
            DungeonTickets.Roll(s, d, "2026-09-06");
            Assert.AreEqual(2, DungeonTickets.Tickets(s, d, "hell", "2026-09-06"), "첫 접근 0 → 2");

            s.DunTickets["hell"] = 1;
            DungeonTickets.Roll(s, d, "2026-09-07");
            Assert.AreEqual(2, DungeonTickets.Tickets(s, d, "hell", "2026-09-07"), "1 → 2(모자란 만큼만 채운다)");

            DungeonTickets.Roll(s, d, "2026-09-08");
            Assert.AreEqual(2, DungeonTickets.Tickets(s, d, "hell", "2026-09-08"), "2 → 2(그대로 · 더하지 않는다)");

            s.DunTickets["hell"] = 3;
            DungeonTickets.Roll(s, d, "2026-09-09");
            Assert.AreEqual(3, DungeonTickets.Tickets(s, d, "hell", "2026-09-09"), "3 → 3(줄이지도 않는다)");
        }

        [Test]
        public void SameDayDoesNotRefill()
        {
            var d = D(); var s = S();
            DungeonTickets.Roll(s, d, "2026-09-06");
            s.DunTickets["hell"] = 0;
            DungeonTickets.Roll(s, d, "2026-09-06");
            Assert.AreEqual(0, DungeonTickets.Tickets(s, d, "hell", "2026-09-06"), "같은 날에는 다시 채우지 않는다");
        }

        [Test]
        public void AdGivesOneTicketOncePerDayPerDungeon()
        {
            var d = D(); var s = S();
            DungeonTickets.Roll(s, d, "2026-09-06"); s.DunTickets["hell"] = 0; s.DunTickets["expedition"] = 0;

            Assert.IsTrue(DungeonTickets.CanAd(s, d, "hell", "2026-09-06"), "첫 광고는 가능");
            Assert.IsTrue(DungeonTickets.ClaimAd(s, d, "hell", "2026-09-06"), "광고 1회 → 티켓 +1");
            Assert.AreEqual(1, DungeonTickets.Tickets(s, d, "hell", "2026-09-06"));
            Assert.IsFalse(DungeonTickets.CanAd(s, d, "hell", "2026-09-06"), "같은 날 두 번째 광고는 거부");
            Assert.IsFalse(DungeonTickets.ClaimAd(s, d, "hell", "2026-09-06"), "거부되면 티켓도 안 는다");
            Assert.AreEqual(1, DungeonTickets.Tickets(s, d, "hell", "2026-09-06"));

            Assert.IsTrue(DungeonTickets.CanAd(s, d, "expedition", "2026-09-06"), "던전마다 따로 센다");
            Assert.IsTrue(DungeonTickets.ClaimAd(s, d, "expedition", "2026-09-06"));

            DungeonTickets.Roll(s, d, "2026-09-07");
            Assert.IsTrue(DungeonTickets.CanAd(s, d, "hell", "2026-09-07"), "날짜가 바뀌면 다시 가능");
        }

        [Test]
        public void GemBuysOneTicketOncePerDayAndCostsGems()
        {
            var d = D(); var s = S(); s.Gem = 120;
            DungeonTickets.Roll(s, d, "2026-09-06"); s.DunTickets["hell"] = 0;

            Assert.IsTrue(DungeonTickets.CanBuyGem(s, d, "hell", "2026-09-06"), "다이아 120 ≥ 50");
            Assert.IsTrue(DungeonTickets.BuyGem(s, d, "hell", "2026-09-06"), "50 다이아 → 티켓 +1");
            Assert.AreEqual(70, s.Gem, 1e-9, "다이아 50 차감");
            Assert.AreEqual(1, DungeonTickets.Tickets(s, d, "hell", "2026-09-06"));
            Assert.IsFalse(DungeonTickets.GemLeft(s, d, "hell", "2026-09-06"), "같은 날 두 번째 구매는 거부");
            Assert.IsFalse(DungeonTickets.BuyGem(s, d, "hell", "2026-09-06"));
            Assert.AreEqual(70, s.Gem, 1e-9, "거부되면 다이아도 안 준다");

            Assert.IsTrue(DungeonTickets.GemLeft(s, d, "expedition", "2026-09-06"), "던전마다 따로");
            s.Gem = 10;
            Assert.IsFalse(DungeonTickets.CanBuyGem(s, d, "expedition", "2026-09-06"), "다이아가 모자라면 못 산다");
            Assert.IsFalse(DungeonTickets.BuyGem(s, d, "expedition", "2026-09-06"));
            Assert.AreEqual(10, s.Gem, 1e-9);
        }

        [Test]
        public void ReadyMeansTicketsOrAnAdTicketWaiting()
        {
            var d = D(); var s = S();
            DungeonTickets.Roll(s, d, "2026-09-06");
            Assert.IsTrue(DungeonTickets.Ready(s, d, "hell", "2026-09-06"), "티켓 2 → 빨간 점");
            Assert.IsTrue(DungeonTickets.AnyReady(s, d, "2026-09-06"));

            s.DunTickets["hell"] = 0; s.DunTickets["expedition"] = 0;
            Assert.IsTrue(DungeonTickets.Ready(s, d, "hell", "2026-09-06"), "티켓 0 이라도 광고가 남았으면 빨간 점");
            DungeonTickets.ClaimAd(s, d, "hell", "2026-09-06"); s.DunTickets["hell"] = 0;
            Assert.IsFalse(DungeonTickets.Ready(s, d, "hell", "2026-09-06"), "티켓 0 · 광고도 다 썼으면 점 없음");
            Assert.IsTrue(DungeonTickets.AnyReady(s, d, "2026-09-06"), "다른 던전이 남아 있으면 여전히 있다");
        }

        [Test]
        public void SaveKeepsTicketsAcrossJsonRoundTrip()
        {
            var d = D(); var s = S(); s.Gem = 500;
            DungeonTickets.Roll(s, d, "2026-09-06");
            DungeonTickets.ClaimAd(s, d, "hell", "2026-09-06");
            DungeonTickets.BuyGem(s, d, "expedition", "2026-09-06");

            var back = SaveData.FromJson(s.ToJson(), TestData.Load());
            Assert.AreEqual("2026-09-06", back.DunDay, "날짜가 살아남는다");
            Assert.AreEqual(3, DungeonTickets.Tickets(back, d, "hell", "2026-09-06"), "보충 2 + 광고 1 = 3 이 저장·복원된다");
            Assert.AreEqual(3, DungeonTickets.Tickets(back, d, "expedition", "2026-09-06"), "보충 2 + 다이아 1 = 3");
            Assert.IsFalse(DungeonTickets.CanAd(back, d, "hell", "2026-09-06"), "오늘 쓴 광고 횟수도 살아남는다");
            Assert.IsFalse(DungeonTickets.GemLeft(back, d, "expedition", "2026-09-06"), "오늘 쓴 다이아 횟수도 살아남는다");
            Assert.AreEqual(450, back.Gem, 1e-9, "다이아 500 − 50");
        }

        // ───────────────────────── T183 1단계 — 던전 «판 규칙»(run) 표 ─────────────────────────

        const string RunJson = @"{
          ""dailyRefill"": 2, ""gemCost"": 50, ""adPerDay"": 1, ""gemPerDay"": 1,
          ""dungeons"": [
            { ""key"": ""hell"", ""first"": { ""petEgg"": 11 }, ""clear"": { ""petEgg"": 5 }, ""sweep"": { ""petEgg"": 5 },
              ""run"": { ""startPerks"": 0, ""startLevel"": 1, ""minPerkGrade"": 2 } },
            { ""key"": ""expedition"", ""first"": { ""gold"": 5800 }, ""clear"": { ""gold"": 3500 }, ""sweep"": { ""gold"": 3500 },
              ""run"": { ""startPerks"": 5, ""startLevel"": 5, ""minPerkGrade"": 0 } }
          ]
        }";

        /// <summary>T183 — 판 규칙은 표에서 온다(코드에 5·2 를 안 박는다) · «run» 이 없으면 일반 전투와 같은 기본값.</summary>
        [Test]
        public void RunRule_ComesFromTheTableAndDefaultsToAPlainRun()
        {
            var d = DungeonData.Parse(RunJson);
            var hell = d.Of("hell"); var exp = d.Of("expedition");
            Assert.That(hell.Run.StartPerks, Is.EqualTo(0), "지옥의 문: 시작 특전 없음(주인)");
            Assert.That(hell.Run.StartLevel, Is.EqualTo(1), "지옥의 문: 레벨 1 로 시작");
            Assert.That(hell.Run.MinPerkGrade, Is.EqualTo(2), "지옥의 문: 맨 위 등급만(주인 «전설·신화만»)");
            Assert.That(hell.Run.IsPlain, Is.False, "등급 하한이 있으면 «일반 판» 이 아니다");
            Assert.That(exp.Run.StartPerks, Is.EqualTo(5), "원정: 시작 특전 5개(주인)");
            Assert.That(exp.Run.StartLevel, Is.EqualTo(5), "원정: 레벨 5 로 시작(주인)");
            Assert.That(exp.Run.MinPerkGrade, Is.EqualTo(0), "원정: 일반 특전도 뜬다(주인)");

            // «run» 이 아예 없는 표(이 파일 위쪽 Json)는 기본값 = 일반 챕터 전투와 똑같은 판이다.
            var plain = DungeonData.Parse(Json).Of("hell");
            Assert.That(plain.Run.IsPlain, Is.True, "run 이 없으면 일반 판(시작 특전 0 · 레벨 1 · 등급 제한 없음)");
            Assert.That(plain.Run.StartLevel, Is.EqualTo(1));
        }

        /// <summary>T183 — 등급 하한이 있으면 그 아래 등급은 안 뜨고, 하한이 0 이면 지금과 한 치도 안 다르다.</summary>
        [Test]
        public void PerkOffer_MinGradeKeepsTheLowGradesOutAndZeroChangesNothing()
        {
            var data = TestData.Load();
            // ⓐ 하한 2 = 맨 위 등급만
            {
                var rng = new Mulberry32(11); var taken = new List<PerkDef>();
                for (int i = 0; i < 60; i++)
                    foreach (var p in Perks.Offer(data, taken, false, rng, 2))
                        Assert.That(p.Grade, Is.EqualTo(2), "하한 2 면 맨 위 등급만 뜬다(T183 · 지옥의 문)");
            }
            // ⓑ 하한 0 = 종전과 같은 굴림(같은 시드·같은 taken 이면 결과가 글자까지 같다)
            {
                var a = new Mulberry32(7); var b = new Mulberry32(7);
                var ta = new List<PerkDef>(); var tb = new List<PerkDef>();
                for (int i = 0; i < 40; i++)
                {
                    var oa = Perks.Offer(data, ta, false, a);
                    var ob = Perks.Offer(data, tb, false, b, 0);
                    Assert.That(ob.Count, Is.EqualTo(oa.Count), "하한 0 은 굴림을 안 바꾼다");
                    for (int k = 0; k < oa.Count; k++) Assert.That(ob[k].Id, Is.EqualTo(oa[k].Id), "하한 0 은 뽑히는 특전까지 같다");
                    foreach (var p in oa) ta.Add(p);
                    foreach (var p in ob) tb.Add(p);
                }
            }
            // ⓒ 하한 위 등급을 다 가져갔으면 제한을 풀어 «못 고르는 판» 이 안 된다
            {
                var rng = new Mulberry32(3); var taken = new List<PerkDef>();
                foreach (var p in data.Perks.Perks) if (p.Grade == 2) taken.Add(p);
                var o = Perks.Offer(data, taken, false, rng, 2);
                Assert.That(o.Count, Is.GreaterThan(0), "맨 위 등급이 동나면 아래 등급으로라도 준다(막힌 판 방지)");
            }
        }

        /// <summary>T183 2단계 — 판 «시작 조건»: 원정은 특전 5개 + 레벨 5 로 시작하고, 기본값이면 지금과 똑같이 시작한다.</summary>
        [Test]
        public void StartRun_GivesThePerksAndTheLevelFromTheTable()
        {
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, -1, 0, 0);
            var run = DungeonData.Parse(RunJson);

            // ⓐ 원정 = 시작 특전 5 · 레벨 5 (등급 제한 없음)
            {
                var e = run.Of("expedition").Run;
                var st = new BattleState(d, 3, b, new Mulberry32(21), new SimPolicy(),
                    new RunOptions { StartPerks = e.StartPerks, StartLevel = e.StartLevel, MinPerkGrade = e.MinPerkGrade });
                Assert.That(st.P.Level, Is.EqualTo(5), "원정은 레벨 5 로 시작한다(주인)");
                Assert.That(st.P.Exp, Is.EqualTo(0), "시작 경험치 0 — 다음 렙업은 ExpNeed(5) 가 기준이 된다");
                Assert.That(st.Taken.Count, Is.EqualTo(5), "시작하자마자 특전 5개(주인)");
            }
            // ⓑ 지옥의 문 = 시작 특전 없음 · 레벨 1 · 맨 위 등급만
            {
                var e = run.Of("hell").Run;
                var st = new BattleState(d, 3, b, new Mulberry32(21), new SimPolicy(),
                    new RunOptions { StartPerks = e.StartPerks, StartLevel = e.StartLevel, MinPerkGrade = e.MinPerkGrade });
                Assert.That(st.P.Level, Is.EqualTo(1), "지옥의 문은 레벨 1(주인)");
                Assert.That(st.Taken.Count, Is.EqualTo(0), "지옥의 문은 시작 특전 없음(주인)");
                var r = st.RunToEnd();
                foreach (var id in r.Taken)
                    Assert.That(d.Perks.ById(id).Grade, Is.EqualTo(2), "지옥의 문에서는 맨 위 등급 특전만 뜬다(T183 · id " + id + ")");
            }
            // ⓒ 기본 RunOptions = 지금과 똑같은 판(레벨 1 · 시작 특전 0) — 시드 골든이 흔들리지 않는 근거
            {
                var st = new BattleState(d, 3, b, new Mulberry32(21), new SimPolicy(), new RunOptions());
                Assert.That(st.P.Level, Is.EqualTo(1)); Assert.That(st.Taken.Count, Is.EqualTo(0));
            }
        }
    }
}
