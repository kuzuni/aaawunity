using System;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T452 — <b>«짧게 쓴 수» 를 내는 쪽과 되읽는 쪽이 같은 낱말을 아는가</b>(결정 1271).
    /// <para>
    /// ⚑ <b>이 자가 없으면 아무도 안 지킨다.</b> 내는 쪽은 <c>UiKit.Fmt</c>(Game · PlayMode 에서만 돈다)이고
    /// 읽는 쪽은 <c>RewardPopup.QtyOf</c>(역시 Game)다 — 워커는 PlayMode 를 못 돌리므로(결정 143)
    /// 그 둘의 어긋남은 <b>배포까지 가서야</b> 드러났다(«출석 다이아 1000 → 구슬 1개» · 결정 1259).
    /// 그래서 셈과 표를 <see cref="ShortNum"/>(Core)로 내리고 그 위에 이 자를 세운다 — <b>Core 는 매 회차 로컬에서 돈다.</b>
    /// </para>
    /// </summary>
    public class ShortNumTests
    {
        /// <summary>옛 <c>UiKit.Fmt</c> 의 리터럴 사다리 — <b>글자가 한 자도 안 바뀌었는가</b> 를 재는 기준이다.</summary>
        static string Legacy(double n)
        {
            n = Math.Round(n);
            double a = Math.Abs(n);
            if (a >= 1e12) return (n / 1e12).ToString("0.##") + "T";
            if (a >= 1e9) return (n / 1e9).ToString("0.##") + "B";
            if (a >= 1e6) return (n / 1e6).ToString("0.##") + "M";
            if (a >= 1e4) return (n / 1e3).ToString("0.#") + "K";
            return n.ToString("#,0");
        }

        [Test]
        public void FmtIsByteIdenticalToTheLadderItReplaced()
        {
            // 문턱 언저리를 촘촘히 — 특히 K 의 «문턱 1e4 · 단위 1e3» 이 어긋나면 여기서 잡힌다.
            double[] xs =
            {
                0, 1, 9, 99, 999, 1000, 1234, 9999, 10000, 10001, 12500, 99999,
                999999, 1000000, 1500000, 999999999, 1000000000, 1500000000,
                999999999999.0, 1e12, 1.5e12, 2e12, 1.234e13,
                -1, -9999, -10000, -1.5e12,
            };
            foreach (var x in xs)
                Assert.That(ShortNum.Fmt(x), Is.EqualTo(Legacy(x)),
                    $"«{x}» 의 글자가 옛 사다리와 다르다 — 화면 글자가 바뀌는 것은 이 절이 하려던 일이 아니다");
        }

        /// <summary>
        /// ⚑ <b>이 절이 태어난 까닭 그 자체</b> — 내는 쪽이 쓰는 접미사 전부를 읽는 쪽이 알아야 한다.
        /// <para>표에 낱말을 더하고 한쪽만 고치면 <b>여기서 빨개진다</b>(T452 이전에는 그 자리가 조용했다).</para>
        /// </summary>
        [Test]
        public void EverySuffixTheFormatterEmitsIsOneTheReaderKnows()
        {
            foreach (var u in ShortNum.Units)
            {
                string s = ShortNum.Fmt(u.From);
                char last = s[s.Length - 1];
                Assert.That(char.IsLetter(last), Is.True, $"문턱 {u.From} 에서 «{s}» — 접미사가 안 붙었다");
                Assert.That(ShortNum.MultiplierOf(last), Is.GreaterThan(0.0),
                    $"내는 쪽이 «{last}» 를 쓰는데 읽는 쪽은 그 낱말을 모른다 — T452 가 바로 이것이었다(1.5T → 2)");
                Assert.That(ShortNum.MultiplierOf(char.ToLowerInvariant(last)), Is.GreaterThan(0.0),
                    $"«{last}» 의 소문자를 읽는 쪽이 모른다 — 글자는 어디서든 소문자로 올 수 있다");
            }
        }

        /// <summary>표에 없는 글자는 0 이다 — 그래야 «아무 글자나 배수» 가 되지 않는다.</summary>
        [Test]
        public void LettersOutsideTheTableAreNotMultipliers()
        {
            foreach (var ch in new[] { 'A', 'z', 'G', '원', '개', 'x', '#' })
                Assert.That(ShortNum.MultiplierOf(ch), Is.EqualTo(0.0), $"«{ch}» 를 배수로 읽으면 안 된다");
        }

        /// <summary>
        /// ⚑⚑ <b>두 쪽이 정말 이 표를 쓰는가 — 글로 확인한다.</b>
        /// <para>
        /// 위 자들은 <see cref="ShortNum"/> 안에서만 돈다. 정작 화면이 부르는 것은 <c>UiKit.Fmt</c> 와 <c>RewardPopup.QtyOf</c> 인데
        /// 그 둘은 `Game` 이라 <b>이 하니스가 컴파일하지 않는다</b>(PlayMode 몫 · 결정 143) — 곧 «둘이 이 표를 안 쓰고 제 사다리를 다시 적어도»
        /// 위 자들은 **전부 초록**이다. 그것이 바로 T452 가 태어난 꼴이다(출처를 주석에 적어 놓고 손으로 옮겼다).
        /// </para>
        /// <para>⇒ 그래서 <b>소스 글자</b>를 읽어 «위임하고 있는가» 를 잰다. 자가 닿을 수 없는 자리는 <b>글로라도 잰다</b> — 안 재는 것보다 낫다.</para>
        /// </summary>
        [Test]
        public void TheTwoCallersDelegateToThisTableInsteadOfKeepingTheirOwn()
        {
            string uiKit = TestData.ReadRepo("Assets/Scripts/Game/UiKit.cs");
            string reward = TestData.ReadRepo("Assets/Scripts/Game/RewardPopup.cs");

            StringAssert.Contains("ShortNum.Fmt(n)", uiKit,
                "UiKit.Fmt 이 ShortNum 에 위임하지 않는다 — 제 사다리를 다시 적으면 읽는 쪽과 또 어긋난다(T452)");
            StringAssert.Contains("ShortNum.MultiplierOf(ch)", reward,
                "RewardPopup.QtyOf 가 ShortNum 의 배수를 안 쓴다 — 손으로 적은 K·M·B 로 돌아가면 «1.5T → 2» 가 되살아난다(T452)");

            // 그리고 그 둘 중 어느 쪽도 «1e12» 를 손에 들고 있지 않아야 한다 — 표는 한 자리다.
            StringAssert.DoesNotContain("1e12", uiKit, "UiKit 에 1e12 리터럴이 돌아왔다 — 낱말표는 ShortNum 한 자리다");
            StringAssert.DoesNotContain("1000000000L", reward, "RewardPopup 에 배수 리터럴이 돌아왔다 — 낱말표는 ShortNum 한 자리다");
        }

        /// <summary>⚠ K 의 «문턱 ≠ 단위» 를 못 박는다 — 둘을 하나로 합치려는 손이 여기서 멈춘다.</summary>
        [Test]
        public void TheKilobyteRowKeepsItsThresholdApartFromItsUnit()
        {
            var k = Array.Find(ShortNum.Units, u => u.Suffix == 'K');
            Assert.That(k.From, Is.EqualTo(1e4), "K 는 네 자리(1e4)부터 쓴다");
            Assert.That(k.Unit, Is.EqualTo(1e3), "그런데 나누는 것은 1e3 이다(«12.5K»)");
            Assert.That(ShortNum.Fmt(9999), Is.EqualTo(Legacy(9999)), "문턱 바로 아래는 아직 콤마 표기다");
            Assert.That(ShortNum.Fmt(10000), Does.EndWith("K"), "문턱에서 K 로 넘어간다");
        }
    }
}
