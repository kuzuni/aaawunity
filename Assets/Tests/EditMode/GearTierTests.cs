using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// 신화 위 «표시 등급»(T316 · 주인 2026-09-09 10:3X «신화 3강 시 갓 · 6강 초월 · 9강 불멸 · 12강 무한 · 그 뒤 계속 무한 —
    /// 즉 신화 13강은 무한 1강»). 지시서 5항의 EditMode 몫 — <b>경계</b>와 <b>안 바뀌는 것</b>.
    /// <para>
    /// 값(3·6·9·12·이름·색)은 <b>주인이 준 것</b>이라 «표가 주인 값인가» 를 한 자리에만 두고,
    /// 나머지는 값이 아니라 <b>꼴</b>을 본다 — 주인이 수를 바꿔도 규칙 자가 안 깨지게(결정 555 갈래).
    /// </para>
    /// </summary>
    public class GearTierTests
    {
        static GearTierData Load() => GearTierData.Parse(
            File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "gearTier.json"))));

        /// <summary>신화 <paramref name="plus"/> 강 장비를 그린 결과(부르는 쪽이 여태 쓰던 값 = «신화»·«plum»).</summary>
        static GearTier.Shown Myth(GearTierData d, int plus) => GearTier.Of(d, 3, plus, 3, "신화", "plum");

        [Test]
        public void Json_IsOwnersTable()
        {
            // 주인이 말로 준 넷 그대로 — 이름·시작 강화·색.
            var d = Load();
            Assert.That(d.Tiers.Count, Is.EqualTo(4), "갓·초월·불멸·무한");
            var want = new[] { ("갓", 3, "red"), ("초월", 6, "pink"), ("불멸", 9, "brown"), ("무한", 12, "redGreen") };
            for (int i = 0; i < want.Length; i++)
            {
                Assert.That(d.Tiers[i].Name, Is.EqualTo(want[i].Item1), $"{i}번째 이름");
                Assert.That(d.Tiers[i].FromPlus, Is.EqualTo(want[i].Item2), $"{want[i].Item1} 은 신화 +{want[i].Item2} 부터");
                Assert.That(d.Tiers[i].Color, Is.EqualTo(want[i].Item3), $"{want[i].Item1} 색");
            }
            Assert.That(d.Tiers[3].Open, Is.True, "무한만 끝이 없다(주인 «그 뒤에 걍 계속 무한»)");
        }

        [Test]
        public void Boundaries_FollowTheOwnersLadder()
        {
            var d = Load();
            // 신화 +0~+2 — 아직 옛 그대로다(표가 안 건드린다).
            foreach (int p in new[] { 0, 1, 2 })
            {
                var s = Myth(d, p);
                Assert.That(s.IsTier, Is.False, $"신화 +{p} 는 표시 등급이 아니다");
                Assert.That(s.Name, Is.EqualTo("신화")); Assert.That(s.Color, Is.EqualTo("plum"));
                Assert.That(s.Plus, Is.EqualTo(p), "«+N» 도 그대로");
            }
            // 주인 «신화 3강 → 갓 0강 이런 식» — 표시 +N 은 전부 0 부터 다시 센다.
            Assert.That(Myth(d, 3).Name, Is.EqualTo("갓")); Assert.That(Myth(d, 3).Plus, Is.EqualTo(0));
            Assert.That(Myth(d, 5).Name, Is.EqualTo("갓")); Assert.That(Myth(d, 5).Plus, Is.EqualTo(2), "갓의 마지막 칸");
            Assert.That(Myth(d, 6).Name, Is.EqualTo("초월")); Assert.That(Myth(d, 6).Plus, Is.EqualTo(0), "한 칸 더 강화하면 다음 등급 +0");
            Assert.That(Myth(d, 9).Name, Is.EqualTo("불멸")); Assert.That(Myth(d, 11).Plus, Is.EqualTo(2));
            Assert.That(Myth(d, 12).Name, Is.EqualTo("무한")); Assert.That(Myth(d, 12).Plus, Is.EqualTo(0));
            Assert.That(Myth(d, 13).Plus, Is.EqualTo(1), "주인 «신화 13강은 무한 1강»");
            Assert.That(Myth(d, 100).Name, Is.EqualTo("무한")); Assert.That(Myth(d, 100).Plus, Is.EqualTo(88), "무한은 끝이 없다");
        }

        [Test]
        public void Colors_AndTheGradientAreSaidByTheTable()
        {
            var d = Load();
            Assert.That(Myth(d, 3).Color, Is.EqualTo("red"));
            Assert.That(Myth(d, 6).Color, Is.EqualTo("pink"));
            Assert.That(Myth(d, 9).Color, Is.EqualTo("brown"));
            // 무한만 «한 색이 아니다» — 그리는 쪽이 두 색으로 읽어야 하므로 그 사실을 규칙이 말한다.
            Assert.That(Myth(d, 12).IsGradient, Is.True, "무한 = 빨강↔초록 그라데이션");
            Assert.That(Myth(d, 9).IsGradient, Is.False, "나머지는 한 색");
            Assert.That(Myth(d, 12).Color, Is.EqualTo(GearTier.GradientColor));
        }

        [Test]
        public void NonMythAndMissingTableAreUntouched()
        {
            var d = Load();
            // 신화가 아니면 강화가 아무리 높아도 옛 그대로다 — 표는 신화 위만 다룬다.
            foreach (int rar in new[] { 0, 1, 2 })
            {
                var s = GearTier.Of(d, rar, 20, 3, "전설", "yellow");
                Assert.That(s.IsTier, Is.False, $"rar {rar} 는 표시 등급이 아니다");
                Assert.That(s.Name, Is.EqualTo("전설")); Assert.That(s.Plus, Is.EqualTo(20));
            }
            // 표를 못 읽은 판(카탈로그에 없음) — 신화 +100 이어도 옛 그대로여야 한다(화면이 막히지 않는다).
            var off = GearTier.Of(null, 3, 100, 3, "신화", "plum");
            Assert.That(off.IsTier, Is.False); Assert.That(off.Name, Is.EqualTo("신화")); Assert.That(off.Plus, Is.EqualTo(100));
        }

        [Test]
        public void Table_RejectsRowsThatWouldBreakQuietly()
        {
            // 이 넷은 «게임은 도는데 등급이 조용히 어긋나는» 종류라 읽는 순간 울어야 한다.
            Assert.Throws<System.FormatException>(() => GearTierData.Parse("{\"tiers\": []}"), "빈 표");
            Assert.Throws<System.FormatException>(() => GearTierData.Parse(
                "{\"tiers\": [{\"key\":\"a\",\"name\":\"갓\",\"fromPlus\":3,\"color\":\"red\"}]}"), "마지막 줄이 안 열려 있으면 그 위가 이름 없는 장비가 된다");
            Assert.Throws<System.FormatException>(() => GearTierData.Parse(
                "{\"tiers\": [{\"key\":\"a\",\"name\":\"갓\",\"fromPlus\":6,\"color\":\"red\"},{\"key\":\"b\",\"name\":\"초월\",\"fromPlus\":3,\"color\":\"pink\",\"open\":true}]}"),
                "오름차순이 아니면 «맞는 줄» 고르기가 조용히 어긋난다");
            Assert.Throws<System.FormatException>(() => GearTierData.Parse(
                "{\"tiers\": [{\"key\":\"a\",\"name\":\"\",\"fromPlus\":3,\"color\":\"red\",\"open\":true}]}"), "이름이 비면 등급 칸이 빈 글자로 뜬다");
        }
    }
}
