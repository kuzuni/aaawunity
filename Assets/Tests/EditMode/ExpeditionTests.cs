using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 탐험(방치·오프라인 보상 · T97 · 주인 2026-09-07 «켜두거나 꺼둬도 쩄든 방치 보상 쌓이고 · 골드·다이아 쌓이게 · 빠른 탐험은 광고 보고»).
    /// 수치표(<c>Assets/KkomaKnight/expedition.json</c>)와 규칙(<see cref="Expedition"/>). 주인이 값을 바꿀 수 있으므로
    /// «표가 주인 지시와 같은가»(상한 8h · 빠른 탐험 5h)는 한 테스트에만 두고, 나머지는 값이 아니라 <b>꼴</b>을 본다 —
    /// 오프라인 경과 · 상한에서 멈춤 · 시계 되돌림 이득 0 · 빠른 탐험은 누적과 별개 · 하루 횟수 초기화.
    /// </summary>
    public class ExpeditionTests
    {
        const string D0 = "2026-09-07", D1 = "2026-09-08";
        const double T0 = 1_800_000_000.0;   // 시험용 «지금»(UTC 유닉스 초)
        const double H = 3600.0;

        static ExpeditionData Load() => ExpeditionData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "expedition.json"))));
        static GameData Data() => TestData.Load();
        static SaveData NewSave() => SaveData.NewSave(TestData.Load());

        [Test]
        public void Json_IsOwnersTable()
        {
            // 주인·레퍼런스 30·31: 상한 «Max Explore Time: 8h» · 빠른 탐험 «Get 5 hours … immediately» · 배지 3회 · 다이아 «10/h».
            var d = Load();
            Assert.That(d.MaxHours, Is.EqualTo(8).Within(1e-9), "상한 8시간(레퍼런스 30)");
            Assert.That(d.QuickHours, Is.EqualTo(5).Within(1e-9), "빠른 탐험 5시간치(레퍼런스 31)");
            Assert.That(d.QuickAdsPerDay, Is.EqualTo(3), "빠른 탐험 하루 3회(레퍼런스 31 배지)");
            Assert.That(d.GemPerHour, Is.GreaterThan(0), "시간당 다이아");
            Assert.That(d.GoldKillsPerHour, Is.GreaterThan(0), "시간당 골드는 처치 수로 유도한다");
        }

        [Test]
        public void GoldPerHour_GrowsWithChapter()
        {
            // 레퍼런스 30 «Later chapters grant better rewards» — 시간당 골드는 진행 챕터의 처치 골드에서 나온다.
            var G = Data(); var d = Load(); var s = NewSave();
            s.MaxChapter = 1; double c1 = Expedition.GoldPerHour(G, s, d);
            s.MaxChapter = 10; double c10 = Expedition.GoldPerHour(G, s, d);
            Assert.That(c1, Is.GreaterThan(0), "1챕터도 0 이 아니다");
            Assert.That(c10, Is.GreaterThan(c1), "뒤 챕터가 더 많이 준다");
            Assert.That(c1, Is.EqualTo(G.Tune.GoldKillBaseAt(1) * d.GoldRandAvg * d.GoldKillsPerHour).Within(1e-6), "= 처치 골드 결정부 × 난수 평균 × 시간당 처치 수");
            Assert.That(Expedition.GemPerHour(d), Is.EqualTo(d.GemPerHour).Within(1e-9), "다이아는 챕터와 무관한 고정");
        }

        [Test]
        public void OfflineSixHours_AccruesThatMuch()
        {
            // 주인: «켜두거나 꺼둬도 쩄든 방치 보상 쌓이고» — 저장하는 것은 마지막 정산 시각뿐이라 앱이 꺼져 있어도 같다.
            var G = Data(); var d = Load(); var s = NewSave(); s.MaxChapter = 10;
            Expedition.Roll(s, d, T0, D0);
            double now = T0 + 6 * H;
            Assert.That(Expedition.ElapsedSec(s, d, now, D0), Is.EqualTo(6 * H).Within(1e-6), "6시간 경과");
            Expedition.Pending(G, s, d, now, D0, out double gold, out double gem);
            Assert.That(gold, Is.EqualTo(System.Math.Floor(Expedition.GoldPerHour(G, s, d) * 6)).Within(1e-6));
            Assert.That(gem, Is.EqualTo(System.Math.Floor(d.GemPerHour * 6)).Within(1e-6));
        }

        [Test]
        public void NineHours_StopsAtTheCap()
        {
            var G = Data(); var d = Load(); var s = NewSave(); s.MaxChapter = 10;
            Expedition.Roll(s, d, T0, D0);
            Expedition.Pending(G, s, d, T0 + 9 * H, D0, out double g9, out double m9);
            Expedition.Pending(G, s, d, T0 + d.MaxHours * H, D0, out double gCap, out double mCap);
            Assert.That(g9, Is.EqualTo(gCap).Within(1e-6), "상한(8h)을 넘으면 그대로 멈춘다");
            Assert.That(m9, Is.EqualTo(mCap).Within(1e-6));
        }

        [Test]
        public void Claim_PaysAndResetsTheTimer()
        {
            var G = Data(); var d = Load(); var s = NewSave(); s.MaxChapter = 10;
            Expedition.Roll(s, d, T0, D0);
            double now = T0 + 3 * H, gold0 = s.Gold, gem0 = s.Gem;
            Expedition.Pending(G, s, d, now, D0, out double pg, out double pm);
            Expedition.Claim(G, s, d, now, D0, out double gg, out double mm);
            Assert.That(gg, Is.EqualTo(pg).Within(1e-6), "받은 값 = 보이던 값");
            Assert.That(s.Gold - gold0, Is.EqualTo(pg).Within(1e-6));
            Assert.That(s.Gem - gem0, Is.EqualTo(pm).Within(1e-6));
            Assert.That(mm, Is.EqualTo(pm).Within(1e-6));
            Expedition.Pending(G, s, d, now, D0, out double after, out double afterGem);
            Assert.That(after, Is.EqualTo(0).Within(1e-9), "받은 직후엔 0 부터 다시");
            Assert.That(afterGem, Is.EqualTo(0).Within(1e-9));
            Assert.That(Expedition.CanClaim(G, s, d, now, D0), Is.False, "최소 누적 전에는 못 받는다");
            Assert.That(Expedition.SecondsToClaim(s, d, now, D0), Is.EqualTo(d.MinClaimSeconds).Within(1e-6), "«다음까지» 카운트다운");
        }

        [Test]
        public void ClockRollback_GivesNothingAndDoesNotGoNegative()
        {
            var G = Data(); var d = Load(); var s = NewSave(); s.MaxChapter = 10;
            Expedition.Roll(s, d, T0, D0);
            double back = T0 - 5 * H;   // 기기 시계를 5시간 뒤로
            Assert.That(Expedition.ElapsedSec(s, d, back, D0), Is.EqualTo(0).Within(1e-9), "음수가 되지 않는다");
            Expedition.Pending(G, s, d, back, D0, out double gold, out double gem);
            Assert.That(gold, Is.EqualTo(0).Within(1e-9)); Assert.That(gem, Is.EqualTo(0).Within(1e-9));
            // 되돌린 시각이 새 기준이 되므로 «되돌렸다 되돌아오면 두 배» 도 없다
            Assert.That(Expedition.ElapsedSec(s, d, back + H, D0), Is.EqualTo(H).Within(1e-6), "되돌린 뒤 1시간이면 1시간치");
        }

        [Test]
        public void QuickExplore_PaysFiveHoursImmediatelyAndIsSeparateFromTheAccrual()
        {
            var G = Data(); var d = Load(); var s = NewSave(); s.MaxChapter = 10;
            Expedition.Roll(s, d, T0, D0);
            double now = T0 + 2 * H;
            Expedition.Pending(G, s, d, now, D0, out double before, out double beforeGem);
            Expedition.QuickReward(G, s, d, out double qg, out double qm);
            Assert.That(qg, Is.EqualTo(System.Math.Floor(Expedition.GoldPerHour(G, s, d) * d.QuickHours)).Within(1e-6), "= 시간당 × 5시간");
            double gold0 = s.Gold;
            Expedition.ClaimQuick(G, s, d, now, D0, out double gg, out double mm);
            Assert.That(gg, Is.EqualTo(qg).Within(1e-6)); Assert.That(mm, Is.EqualTo(qm).Within(1e-6));
            Assert.That(s.Gold - gold0, Is.EqualTo(qg).Within(1e-6), "즉시 지급");
            Expedition.Pending(G, s, d, now, D0, out double after, out double afterGem);
            Assert.That(after, Is.EqualTo(before).Within(1e-6), "쌓이던 누적은 그대로다(빠른 탐험은 누적에 더하지 않는다 · 중복 수령 방지)");
            Assert.That(afterGem, Is.EqualTo(beforeGem).Within(1e-6));
        }

        [Test]
        public void QuickExplore_RunsOutForTheDayAndResetsTomorrow()
        {
            var G = Data(); var d = Load(); var s = NewSave(); s.MaxChapter = 10;
            Expedition.Roll(s, d, T0, D0);
            for (int i = 0; i < d.QuickAdsPerDay; i++)
            {
                Assert.That(Expedition.CanQuick(s, d, T0, D0), Is.True, "남은 횟수 " + (i + 1));
                Expedition.ClaimQuick(G, s, d, T0, D0, out _, out _);
            }
            Assert.That(Expedition.QuickLeft(s, d, T0, D0), Is.EqualTo(0), "오늘은 다 썼다");
            double gold = s.Gold;
            Expedition.ClaimQuick(G, s, d, T0, D0, out double gg, out double mm);
            Assert.That(gg, Is.EqualTo(0).Within(1e-9)); Assert.That(mm, Is.EqualTo(0).Within(1e-9));
            Assert.That(s.Gold, Is.EqualTo(gold).Within(1e-9), "다 쓰면 지급 0");
            Assert.That(Expedition.QuickLeft(s, d, T0 + 24 * H, D1), Is.EqualTo(d.QuickAdsPerDay), "날짜가 바뀌면 초기화");
        }

        [Test]
        public void RedDot_IsOnWhenThereIsSomethingToTake()
        {
            var G = Data(); var d = Load(); var s = NewSave(); s.MaxChapter = 10;
            Expedition.Roll(s, d, T0, D0);
            Assert.That(Expedition.AnyClaimable(G, s, d, T0, D0), Is.True, "빠른 탐험 횟수가 남아 있으면 켠다");
            for (int i = 0; i < d.QuickAdsPerDay; i++) Expedition.ClaimQuick(G, s, d, T0, D0, out _, out _);
            Assert.That(Expedition.AnyClaimable(G, s, d, T0, D0), Is.False, "횟수도 없고 쌓인 것도 없으면 끈다");
            Assert.That(Expedition.AnyClaimable(G, s, d, T0 + H, D0), Is.True, "한 시간 쌓이면 다시 켠다");
        }

        [Test]
        public void OldSave_WithoutExpeditionFields_LoadsAndStartsNow()
        {
            // 옛 세이브(필드 없음) 호환 — T77 과 같은 규칙(«없으면 기본값»).
            var G = Data(); var d = Load();
            var s = SaveData.FromJson("{\"gold\":10,\"gem\":2,\"maxChapter\":5}", G);
            Assert.That(s.ExpSettle, Is.EqualTo(0).Within(1e-9), "필드가 없으면 0");
            Expedition.Roll(s, d, T0, D0);
            Assert.That(s.ExpSettle, Is.EqualTo(T0).Within(1e-9), "여는 순간이 시작점");
            Assert.That(Expedition.ElapsedSec(s, d, T0, D0), Is.EqualTo(0).Within(1e-9), "처음 열면 쌓인 것 0");
            // 왕복 직렬화
            s.ExpQuickUsed = 2; s.ExpQuickDay = D0;
            var back = SaveData.FromJson(s.ToJson(), G);
            Assert.That(back.ExpSettle, Is.EqualTo(s.ExpSettle).Within(1e-6));
            Assert.That(back.ExpQuickDay, Is.EqualTo(D0));
            Assert.That(back.ExpQuickUsed, Is.EqualTo(2));
        }
    }
}
