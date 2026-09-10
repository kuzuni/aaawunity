using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 업적(반복 퀘스트 · T258 · 주인 2026-09-09 «업적 부분은 메달 없음. 걍 보상 바로 받음. 업적은 일종의 반복 퀘스트 느낌»).
    /// 지시서 5항의 Core 몫 다섯 — ⓐ 깨고 받으면 다음 단계 목표가 «첫 목표 × 2» ⓑ 누적 20 · 안 받았으면 «받기» 네 번
    /// ⓒ 받아도 누적은 안 준다 ⓓ 출석은 같은 날 두 번 안 센다 ⓔ 17줄의 첫 목표·보상이 표와 정확히 같다.
    /// <para>«표가 주인 값인가»(ⓔ)는 한 자리에만 두고 나머지는 값이 아니라 <b>꼴</b>을 본다 —
    /// 그래야 주인이 수를 바꿔도 규칙 자가 안 깨진다(T270 이 빠른 탐험에서 실제로 그랬다).</para>
    /// </summary>
    public class AchievementTests
    {
        const string D0 = "2026-09-09", D1 = "2026-09-10";
        const string Key = "chestOpenRare";   // 표 첫 줄 = «희귀 상자 5회 오픈»

        static AchievementData Load() => AchievementData.Parse(
            File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "achievement.json"))));
        static SaveData NewSave() => SaveData.NewSave(TestData.Load());

        [Test]
        public void Json_IsOwnersTable()
        {
            // 주인이 2026-09-09 05:2X 에 준 17줄 그대로 — 값·차례·보상이 다 그의 것이다.
            var d = Load();
            Assert.That(d.List.Count, Is.EqualTo(17), "목록 17개(주인)");
            var want = new[]
            {
                ("희귀 상자 5회 오픈", 5, 5.0), ("전설 상자 5회 오픈", 5, 10.0), ("신화 상자 5회 오픈", 5, 15.0),
                ("광고 10회 시청", 10, 5.0), ("적 처치 100명", 100, 10.0), ("장비 합성 10회", 10, 5.0),
                ("지옥문 던전 도전 5회", 5, 5.0), ("원정 던전 도전 5회", 5, 5.0), ("탐험 5회", 5, 5.0),
                ("빠른 탐험 5회", 5, 5.0), ("클리어 보상 수령 5회", 5, 5.0), ("출석 1회", 1, 5.0),
                ("출석 보상 1회 수령", 1, 5.0), ("데일리 기프트 5회 수령", 5, 5.0), ("펫 업그레이드 10회", 10, 5.0),
                ("펫 뽑기 10회", 10, 5.0), ("아레나 10회 도전", 10, 5.0),
            };
            for (int i = 0; i < want.Length; i++)
            {
                Assert.That(d.List[i].Label, Is.EqualTo(want[i].Item1), "줄 " + i + " 이름(주인 글자 그대로)");
                Assert.That(d.List[i].Goal, Is.EqualTo(want[i].Item2), want[i].Item1 + " 첫 목표");
                Assert.That(d.List[i].Amount, Is.EqualTo(want[i].Item3).Within(1e-9), want[i].Item1 + " 보상");
                Assert.That(d.List[i].Item, Is.EqualTo("gem"), "주인은 전부 다이아로 줬다");
            }
            // 카운터가 겹치면 두 줄이 같은 누적을 나눠 쓰게 되어 한쪽이 조용히 남의 진행도로 깨진다.
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var r in d.List) Assert.That(seen.Add(r.Counter), Is.True, "카운터가 겹친다: " + r.Counter);
        }

        [Test]
        public void Stage_GoalGrowsByOneFirstGoalEachTime()
        {
            // ⓐ 주인: «상자 5회 → 10 → 15 → 20 …» — 단계 N 목표 = 첫 목표 × N.
            var d = Load(); var s = NewSave();
            var row = d.Find(Key);
            Assert.That(Achievement.Goal(s, d, Key), Is.EqualTo(row.Goal), "처음에는 1단계 목표");

            Achievement.Add(s, Key, row.Goal);
            Assert.That(Achievement.CanClaim(s, d, Key), Is.True, "첫 목표를 채우면 받을 수 있다");
            Assert.That(Achievement.Shown(s, d, Key), Is.EqualTo(row.Goal), "«5/5»");

            Assert.That(Achievement.Claim(s, d, Key, out string item, out double amt), Is.True);
            Assert.That(item, Is.EqualTo(row.Item)); Assert.That(amt, Is.EqualTo(row.Amount).Within(1e-9));

            Assert.That(Achievement.Goal(s, d, Key), Is.EqualTo(row.Goal * 2), "다음 단계 목표 = 첫 목표 × 2");
            Assert.That(Achievement.CanClaim(s, d, Key), Is.False, "새 목표를 아직 못 채웠다");
            Assert.That(Achievement.Shown(s, d, Key), Is.EqualTo(row.Goal), "달성률은 «누적/새 목표»(5/10)");
        }

        [Test]
        public void BacklogIsClaimedOneStageAtATime()
        {
            // ⓑ 주인: «20회 상태에서 처음 열면 네 번 받는다» — 자동 일괄이 아니라 순차다.
            var d = Load(); var s = NewSave();
            var row = d.Find(Key);
            Achievement.Add(s, Key, row.Goal * 4);
            Assert.That(Achievement.Pending(s, d, Key), Is.EqualTo(4), "밀린 단계 넷");

            for (int n = 1; n <= 4; n++)
            {
                Assert.That(Achievement.CanClaim(s, d, Key), Is.True, n + "번째 받기가 살아 있다");
                Assert.That(Achievement.Claim(s, d, Key, out _, out double a), Is.True, n + "번째 받기");
                Assert.That(a, Is.EqualTo(row.Amount).Within(1e-9), "보상은 단계마다 같다");
                Assert.That(Achievement.Pending(s, d, Key), Is.EqualTo(4 - n), "남은 단계");
            }
            Assert.That(Achievement.CanClaim(s, d, Key), Is.False, "다 받으면 받기가 죽는다");
            Assert.That(Achievement.Claim(s, d, Key, out string it, out double am), Is.False, "더 눌러도 안 준다");
            Assert.That(it, Is.EqualTo("")); Assert.That(am, Is.EqualTo(0).Within(1e-9));
            Assert.That(Achievement.Goal(s, d, Key), Is.EqualTo(row.Goal * 5), "다음은 5단계 목표");
        }

        [Test]
        public void ClaimingDoesNotSpendTheLifetimeCount()
        {
            // ⓒ 누적은 «평생 기록» 이다 — 받을 때 깎으면 화면의 «누적/목표» 가 뒤로 가고 그 말이 거짓이 된다.
            var d = Load(); var s = NewSave();
            var row = d.Find(Key);
            Achievement.Add(s, Key, row.Goal * 3);
            int before = Achievement.Count(s, Key);
            Achievement.Claim(s, d, Key, out _, out _);
            Assert.That(Achievement.Count(s, Key), Is.EqualTo(before), "받아도 누적은 그대로");
            Assert.That(Achievement.Claimed(s, Key), Is.EqualTo(1), "받은 단계만 오른다");
        }

        [Test]
        public void AttendanceCountsOnlyOncePerDay()
        {
            // ⓓ 주인 명시: «출석은 하루 한 번만 오른다». 다른 것은 이벤트마다 +1.
            var d = Load(); var s = NewSave();
            Achievement.AddOncePerDay(s, Achievement.DailyOnce, D0);
            Achievement.AddOncePerDay(s, Achievement.DailyOnce, D0);
            Achievement.AddOncePerDay(s, Achievement.DailyOnce, D0);
            Assert.That(Achievement.Count(s, Achievement.DailyOnce), Is.EqualTo(1), "같은 날은 한 번");
            Achievement.AddOncePerDay(s, Achievement.DailyOnce, D1);
            Assert.That(Achievement.Count(s, Achievement.DailyOnce), Is.EqualTo(2), "날이 바뀌면 하나 더");

            // 거꾸로 — 하루 한 번이 아닌 것은 부를 때마다 오른다(둘을 같은 자로 묶으면 한쪽이 조용히 틀린다).
            Achievement.Add(s, "kill", 40); Achievement.Add(s, "kill", 60);
            Assert.That(Achievement.Count(s, "kill"), Is.EqualTo(100), "적 처치는 마릿수로 쌓인다");
        }

        [Test]
        public void SaveRoundTripsAndOldSavesStartEmpty()
        {
            // 안 실리면 껐다 켤 때마다 누적이 0 이 되어 «평생 기록» 이 하루살이가 된다.
            var G = TestData.Load(); var d = Load(); var s = NewSave();
            Achievement.Add(s, Key, 7);
            Achievement.Claim(s, d, Key, out _, out _);
            Achievement.AddOncePerDay(s, Achievement.DailyOnce, D0);

            var back = SaveData.FromJson(s.ToJson(), G);
            Assert.That(Achievement.Count(back, Key), Is.EqualTo(7), "누적 왕복");
            Assert.That(Achievement.Claimed(back, Key), Is.EqualTo(1), "받은 단계 왕복");
            Assert.That(Achievement.Count(back, Achievement.DailyOnce), Is.EqualTo(1));
            Achievement.AddOncePerDay(back, Achievement.DailyOnce, D0);
            Assert.That(Achievement.Count(back, Achievement.DailyOnce), Is.EqualTo(1), "«센 날짜» 도 왕복해야 같은 날 두 번 안 센다");

            // 옛 세이브(필드 없음) — 빈 표로 시작하고 아무것도 안 깨진다(T77 규칙 «없으면 기본값»).
            var old = SaveData.FromJson("{\"gold\":10,\"gem\":2,\"maxChapter\":5}", G);
            Assert.That(Achievement.Count(old, Key), Is.EqualTo(0));
            Assert.That(Achievement.CanClaim(old, d, Key), Is.False);
            Assert.That(Achievement.Goal(old, d, Key), Is.EqualTo(d.Find(Key).Goal), "1단계부터 시작");
        }

        [Test]
        public void BrokenTableThrowsInsteadOfGoingQuiet()
        {
            // 목표 0 이면 «영영 못 깨는 줄», 보상 0 이면 «눌러도 아무 일 없는 줄» 인데
            // 화면에는 그저 «0/0» 으로만 보인다(결정 633 과 같은 갈래) — 읽는 순간 울어야 한다.
            Assert.Throws<System.FormatException>(() => AchievementData.Parse(
                "{\"list\":[{\"label\":\"x\",\"counter\":\"c\",\"goal\":0,\"item\":\"gem\",\"amount\":5}]}"), "goal 0");
            Assert.Throws<System.FormatException>(() => AchievementData.Parse(
                "{\"list\":[{\"label\":\"x\",\"counter\":\"c\",\"goal\":5,\"item\":\"gem\",\"amount\":0}]}"), "amount 0");
            Assert.Throws<System.FormatException>(() => AchievementData.Parse(
                "{\"list\":[{\"label\":\"x\",\"counter\":\"\",\"goal\":5,\"item\":\"gem\",\"amount\":5}]}"), "counter 빈 글자");
            Assert.Throws<System.FormatException>(() => AchievementData.Parse("{\"list\":[]}"), "빈 목록");
        }

        [Test]
        public void RedDotIsOnWhenAnyRowCanBeClaimed()
        {
            var d = Load(); var s = NewSave();
            Assert.That(Achievement.AnyClaimable(s, d), Is.False, "처음에는 받을 게 없다");
            Achievement.Add(s, "kill", d.Find("kill").Goal);
            Assert.That(Achievement.AnyClaimable(s, d), Is.True, "한 줄이라도 차면 켠다");
            Achievement.Claim(s, d, "kill", out _, out _);
            Assert.That(Achievement.AnyClaimable(s, d), Is.False, "받고 나면 다시 끈다");
        }

        /// <summary>
        /// T401 2항 — «전부 받기» 의 Core 몫(<see cref="Achievement.ClaimAll"/>): 세 줄에 단계 1·2·0 이 쌓인 판을 한 번에 받으면
        /// ⓐ 받은 단계가 셋 ⓑ 담긴 (물건, 수량) 의 합 = 줄별 «보상 × 쌓인 단계» 의 합 ⓒ 그 뒤엔 받을 것이 없다 ⓓ 0 이면 아무 일도 없다.
        /// 표의 셋째 줄(단계 0)은 **손대지 않았음**까지 잰다 — 반복이 «받을 수 있는 동안» 에서 멈추는가가 이 자의 값이다.
        /// </summary>
        [Test]
        public void 전부_받기는_줄_차례로_쌓인_단계를_다_받고_합이_줄별_합과_같다()
        {
            var d = Load(); var s = NewSave();
            // 갓 만든 판에는 받을 것이 없다 — 0 이면 아무 일도 없고 목록도 빈 채다.
            var got0 = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, double>>();
            Assert.That(Achievement.ClaimAll(s, d, got0), Is.EqualTo(0), "받을 것이 없으면 0");
            Assert.That(got0, Is.Empty, "받은 것이 없으니 담긴 것도 없다");

            var r1 = d.List[0]; var r2 = d.List[1]; var r3 = d.List[2];
            Achievement.Add(s, r1.Counter, r1.Goal);          // 단계 1
            Achievement.Add(s, r2.Counter, r2.Goal * 2);      // 단계 2 가 쌓였다(«20회 상태에서 처음 열면» 의 꼴)
            Achievement.Add(s, r3.Counter, r3.Goal - 1);      // 한 칸 모자라다 — 받으면 안 된다
            Assert.That(Achievement.Pending(s, d, r1.Counter) + Achievement.Pending(s, d, r2.Counter) + Achievement.Pending(s, d, r3.Counter), Is.EqualTo(3), "전제: 밀린 단계 셋");

            var got = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, double>>();
            Assert.That(Achievement.ClaimAll(s, d, got), Is.EqualTo(3), "한 번에 세 단계");
            Assert.That(got.Count, Is.EqualTo(3), "단계마다 하나씩 담는다");
            double sum = 0; foreach (var p in got) sum += p.Value;
            Assert.That(sum, Is.EqualTo(r1.Amount * 1 + r2.Amount * 2).Within(1e-9), "합 = 줄별 «보상 × 쌓인 단계» 의 합");
            Assert.That(got[0].Key, Is.EqualTo(r1.Item), "표 차례 — 첫 줄 것이 먼저");
            Assert.That(got[1].Key, Is.EqualTo(r2.Item)); Assert.That(got[2].Key, Is.EqualTo(r2.Item), "둘째 줄은 두 번");
            Assert.That(Achievement.Claimed(s, d.List[0].Counter), Is.EqualTo(1)); Assert.That(Achievement.Claimed(s, r2.Counter), Is.EqualTo(2));
            Assert.That(Achievement.Claimed(s, r3.Counter), Is.EqualTo(0), "모자란 줄은 손대지 않았다");
            Assert.That(Achievement.Count(s, r2.Counter), Is.EqualTo(r2.Goal * 2), "누적(평생)은 안 준다(ⓒ 와 같은 규약)");
            Assert.That(Achievement.AnyClaimable(s, d), Is.False, "다 받으면 받을 것이 없다(탭 점·단추 옷이 보는 그 수)");
            Assert.That(Achievement.ClaimAll(s, d, null), Is.EqualTo(0), "한 번 더 눌러도 아무 일 없다(목록 없이 불러도 된다)");
        }

        [Test]
        public void 표의_이름과_훅이_대는_이름이_같다()
        {
            // 표의 counter 는 게임 코드가 부르는 이름이다(`Game/Quests` 의 상수). 한 글자만 달라도 그 줄은
            // **영원히 0** 인 채로 화면에 뜬다 — 화면에는 «아직 안 했나 보다» 로 보여서 아무도 안 잡는다.
            //  ⚠ 자가 Game 어셈블리를 못 보므로(테스트는 Core 만 참조) 이름을 글자로 적는다 —
            //     `Game/Quests` 의 상수를 고치면 이 목록도 같이 고쳐야 한다(그 번거로움이 이 자의 값이다 · T257 ⑸ 와 같은 꼴).
            // 두 갈래로 나눠 적는 까닭: «아직 못 건» 줄이 몇이고 왜인지가 표에 남아야 다음 사람이 그 자리를 찾는다.
            var hooked = new System.Collections.Generic.HashSet<string>
            {
                // Game/Quests.Bump 가 퀘스트 이름에서 이어 주는 것(AchName)
                "kill", "gearFuse", "expeditionClaim", "petUpgrade", "expeditionQuick",
                // Game/Quests.Ach 로 부르는 자리가 있는 것
                "dungeonTryHell", "dungeonTryExpd", "arenaTry", "adWatch",
                "attend", "attendClaim", "giftClaim", "chapterChestClaim",
                "chestOpenRare", "chestOpenEpic", "chestOpenMythic",
                "petGacha",   // T293 ⓘ — 펫 소환이 서면서 걸렸다(PetScreen.Pull · 뽑기 입구는 그 한 곳뿐이다). T258 이 마지막까지 기다린 훅이다.
            };
            // 이제 «아직 걸 자리가 없는» 카운터는 하나도 없다. 표에 새 줄이 생기면 여기 두 목록 중 하나에 들어가야 이 자가 초록이다.
            var notYet = new System.Collections.Generic.HashSet<string>();
            var d = Load();
            foreach (var r in d.List)
                Assert.IsTrue(hooked.Contains(r.Counter) || notYet.Contains(r.Counter),
                              "표에 «" + r.Counter + "» 이 들어왔는데 부르는 자리도 없고 «아직» 목록에도 없다 — 그 줄은 영원히 0 이다(«" + r.Label + "»)");
            foreach (var name in hooked)
                Assert.IsNotNull(d.Find(name), "훅은 «" + name + "» 을 세는데 표에는 그 줄이 없다 — 세는 값을 아무도 안 읽는다");
            // petUpgrade 는 퀘스트에도 업적에도 있는데 **양쪽 다 걸 자리가 없다**(펫 시스템 미구현 · T273).
            // 그래도 «훅» 쪽에 둔 까닭은 Quests.Bump 의 이름 잇기(AchName)가 이미 그 이름을 이어 두어,
            // T273 이 열려 Bump 한 줄이 생기는 순간 업적까지 같이 살아나기 때문이다.
        }
    }
}
