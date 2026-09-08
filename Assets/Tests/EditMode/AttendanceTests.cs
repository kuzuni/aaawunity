using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T253 — 출석(16) 7일 보상의 자. 주인 2026-09-09 04:3X
    /// «출석 날짜별 (7일) 보상 — 1. 3000골드 2. 파란키 2 3. 100다이아 4. 펫알 10 5. 5000골드 6. 보라키 1 7. 10k골드, 1000다이아 이렇게 하기.»
    /// <para>
    /// 이 표는 <b>일곱 줄 전부 주인이 준 값</b>이라 자가 그대로 못 박는다 — 위임으로 채운 칸이 없어서 «자가 표의 거울이 된다»(결정 555)가 여기서는 해당 없다.
    /// 대신 <b>지급이 실제로 그 재화에 들어가는가</b>(키·펫알까지)와 <b>하루 한 칸</b>을 잰다.
    /// </para>
    /// </summary>
    public class AttendanceTests
    {
        const string D1 = "2026-09-09", D2 = "2026-09-10", D3 = "2026-09-12";

        static AttendanceData Load() => AttendanceData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "attendance.json"))));

        [Test]
        public void 주인이_준_일곱_줄이_그대로_있다()
        {
            var d = Load();
            Assert.AreEqual(7, d.Days.Count, "7일치");
            void One(int no, string item, double amount)
            {
                var day = d.Of(no);
                Assert.IsNotNull(day, no + "일차");
                Assert.AreEqual(1, day.Rewards.Count, no + "일차는 한 칸");
                Assert.AreEqual(item, day.Rewards[0].Item, no + "일차 보상 종류");
                Assert.AreEqual(amount, day.Rewards[0].Amount, 1e-9, no + "일차 수량");
            }
            One(1, Mail.ItemGold, 3000);
            One(2, GachaKeys.Blue, 2);
            One(3, Mail.ItemGem, 100);
            One(4, Mail.ItemPetEgg, 10);
            One(5, Mail.ItemGold, 5000);
            One(6, GachaKeys.Purple, 1);
            // 7일차만 둘이다(레퍼런스 16 의 넓은 칸)
            var d7 = d.Of(7);
            Assert.AreEqual(2, d7.Rewards.Count, "7일차는 두 칸");
            Assert.AreEqual(Mail.ItemGold, d7.Rewards[0].Item); Assert.AreEqual(10000, d7.Rewards[0].Amount, 1e-9);
            Assert.AreEqual(Mail.ItemGem, d7.Rewards[1].Item); Assert.AreEqual(1000, d7.Rewards[1].Amount, 1e-9);
        }

        [Test]
        public void 하루에_한_칸만_받는다()
        {
            var d = Load(); var s = new SaveData();
            Assert.AreEqual(1, Attendance.Next(s, d), "처음엔 1일차");
            Assert.IsTrue(Attendance.Can(s, d, D1));
            Assert.AreEqual("", Attendance.Why(s, d, D1), "될 때는 까닭이 없다");

            Assert.IsNotNull(Attendance.Claim(s, d, D1), "1일차 받음");
            Assert.IsFalse(Attendance.Can(s, d, D1), "같은 날 두 번은 못 받는다");
            Assert.AreEqual("오늘 출석 보상은 이미 받았습니다", Attendance.Why(s, d, D1), "화면 토스트가 그대로 쓸 까닭");
            Assert.IsNull(Attendance.Claim(s, d, D1), "두 번째는 null");
            Assert.AreEqual(3000, s.Gold, 1e-9, "그리고 골드도 두 배가 안 된다");

            Assert.IsTrue(Attendance.Can(s, d, D2), "날짜가 바뀌면 다음 칸");
            Assert.AreEqual(2, Attendance.Next(s, d));
        }

        [Test]
        public void 하루_걸러_와도_다음_칸이다()
        {
            // 주인이 «연속이 끊기면 처음으로» 를 말한 적이 없다 — 지어내지 않는다(§1). 칸은 «받은 수» 로 나아간다.
            var d = Load(); var s = new SaveData();
            Attendance.Claim(s, d, D1);
            Attendance.Claim(s, d, D3);   // 이틀 건너뛰었다
            Assert.AreEqual(2, s.AttDone, "두 칸 받았다");
            Assert.AreEqual(3, Attendance.Next(s, d), "다음은 3일차 — 1일차로 되돌아가지 않는다");
        }

        [Test]
        public void 일곱_칸이_표대로_지급된다()
        {
            var d = Load(); var s = new SaveData();
            string[] days = { "2026-09-01", "2026-09-02", "2026-09-03", "2026-09-04", "2026-09-05", "2026-09-06", "2026-09-07" };
            for (int i = 0; i < days.Length; i++)
            {
                var got = Attendance.Claim(s, d, days[i]);
                Assert.IsNotNull(got, (i + 1) + "일차");
                Assert.AreEqual(i + 1, got.No);
            }
            Assert.AreEqual(3000 + 5000 + 10000, s.Gold, 1e-9, "골드 세 칸의 합");
            Assert.AreEqual(100 + 1000, s.Gem, 1e-9, "다이아 두 칸의 합");
            Assert.AreEqual(10, s.PetEgg, 1e-9, "펫알 4일차");
            Assert.AreEqual(2, s.KeyBlue, "파란 키 2일차");
            Assert.AreEqual(1, s.KeyPurple, "보라 키 6일차");
            Assert.AreEqual(0, s.KeyYellow, "노란 키는 표에 없다 — 지어내지 않았다");
        }

        [Test]
        public void 다_받으면_끝난다()
        {
            var d = Load(); var s = new SaveData { AttDone = 7, AttDay = "2026-09-07" };
            Assert.AreEqual(0, Attendance.Next(s, d), "0 = 다 받았다");
            Assert.IsFalse(Attendance.Can(s, d, "2026-09-08"), "8일째에 무엇을 하는지는 주인이 말한 적이 없다 — 되돌려 다시 돌리지 않는다");
            Assert.AreEqual("출석 보상을 모두 받았습니다", Attendance.Why(s, d, "2026-09-08"));
            Assert.IsNull(Attendance.Claim(s, d, "2026-09-08"));
            Assert.AreEqual(0, s.Gold, 1e-9);
        }

        [Test]
        public void 화면이_묻는_것들_받은_칸과_오늘_칸()
        {
            var d = Load(); var s = new SaveData();
            Assert.AreEqual(1, Attendance.Today(s, d, D1), "오늘 강조할 칸 = 1일차");
            Assert.IsFalse(Attendance.Claimed(s, 1), "아직 받은 칸이 없다");
            Attendance.Claim(s, d, D1);
            Assert.IsTrue(Attendance.Claimed(s, 1), "✅ 는 1일차에");
            Assert.IsFalse(Attendance.Claimed(s, 2));
            Assert.AreEqual(0, Attendance.Today(s, d, D1), "오늘 이미 받았으면 강조할 칸이 없다");
            Assert.AreEqual(2, Attendance.Today(s, d, D2), "날짜가 바뀌면 2일차를 강조한다");
            Assert.AreEqual("골드 10,000 · 다이아 1,000", Attendance.Summary(d.Of(7)), "받은 것 한 줄(토스트가 그대로 쓴다)");
        }

        [Test]
        public void 세이브를_건너간다()
        {
            var d = Load(); var s = new SaveData();
            Attendance.Claim(s, d, D1);
            var back = SaveData.FromJson(s.ToJson(), TestData.Load());
            Assert.AreEqual(1, back.AttDone); Assert.AreEqual(D1, back.AttDay);
            Assert.AreEqual(2, Attendance.Next(back, d), "이어서 2일차");
            var old = SaveData.FromJson("{\"gold\":10}", TestData.Load());
            Assert.AreEqual(0, old.AttDone, "없으면 0 — 옛 세이브 호환");
            Assert.AreEqual(1, Attendance.Next(old, d));
        }

        [Test]
        public void 표가_어긋나면_읽는_순간_운다()
        {
            // 담을 자리가 없는 이름 — 주면 조용히 사라지는 보상이 된다(T243 결정 663 갈래)
            Assert.Throws<System.FormatException>(() => AttendanceData.Parse(
                @"{ ""days"": [ { ""day"": 1, ""rewards"": [ { ""item"": ""도장"", ""amount"": 3 } ] } ] }"), "모르는 재화");
            // 0 짜리 · 빈 칸 · 빈 표
            Assert.Throws<System.FormatException>(() => AttendanceData.Parse(
                @"{ ""days"": [ { ""day"": 1, ""rewards"": [ { ""item"": ""gold"", ""amount"": 0 } ] } ] }"), "0 은 보상이 아니다");
            Assert.Throws<System.FormatException>(() => AttendanceData.Parse(
                @"{ ""days"": [ { ""day"": 1, ""rewards"": [] } ] }"), "빈 칸");
            Assert.Throws<System.FormatException>(() => AttendanceData.Parse(@"{ ""days"": [] }"), "빈 표");
            // 일차가 1 부터 하나씩 안 이어진다 — 그 칸이 영원히 안 나온다
            Assert.Throws<System.FormatException>(() => AttendanceData.Parse(
                @"{ ""days"": [ { ""day"": 1, ""rewards"": [ { ""item"": ""gold"", ""amount"": 1 } ] },
                                { ""day"": 3, ""rewards"": [ { ""item"": ""gold"", ""amount"": 1 } ] } ] }"), "2일차가 빠졌다");
        }
    }
}
