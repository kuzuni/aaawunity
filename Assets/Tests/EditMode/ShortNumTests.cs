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

        /// <summary>
        /// 옛 사다리와 <b>바이트가 같은가</b> — ⚠ <b>사다리가 서는 범위 안에서만</b>이다(T463).
        /// <para>
        /// T463 이 사다리 끝에 상한을 두었다: <c>|n| ≥ T의 문턱 × <see cref="ShortNum.MantissaCap"/></c>(= 1e15)이면 자리수 꼴로 떨어진다.
        /// 그 위에서 옛 사다리는 가수가 그대로 자라 <b>23자</b>를 냈고 그것이 고치려던 병이다 — 그러니 <b>거기서는 달라야 옳다</b>.
        /// 아래 표본을 그 경계 바로 밑까지 채워 두었고, 경계 위의 갈림은 <see cref="TheLadderStopsGrowingAtItsTopWord"/> 가 잰다.
        /// </para>
        /// </summary>
        [Test]
        public void FmtIsByteIdenticalToTheLadderItReplaced()
        {
            // 문턱 언저리를 촘촘히 — 특히 K 의 «문턱 1e4 · 단위 1e3» 이 어긋나면 여기서 잡힌다.
            double[] xs =
            {
                0, 1, 9, 99, 999, 1000, 1234, 9999, 10000, 10001, 12500, 99999,
                999999, 1000000, 1500000, 999999999, 1000000000, 1500000000,
                999999999999.0, 1e12, 1.5e12, 2e12, 1.234e13,
                9.98e14, 9.99e14,                  // 자리수 꼴로 넘어가기 «바로 밑» — 여기까지는 옛 글자 그대로다
                -1, -9999, -10000, -1.5e12, -9.99e14,
            };
            double cap = ShortNum.Units[0].From * ShortNum.MantissaCap;
            foreach (var x in xs)
            {
                Assert.That(Math.Abs(x), Is.LessThan(cap),
                    $"«{x}» 는 사다리 밖이다 — 이 표본은 «사다리가 서는 범위» 만 담는다(T463)");
                Assert.That(ShortNum.Fmt(x), Is.EqualTo(Legacy(x)),
                    $"«{x}» 의 글자가 옛 사다리와 다르다 — 화면 글자가 바뀌는 것은 이 절이 하려던 일이 아니다");
            }
        }

        /// <summary>
        /// T463 — <b>사다리가 끝나는 자리에서 글자가 자라지 않는가</b>.
        /// <para>
        /// 실측이 이 자를 불렀다(<c>origin/screens:highlevel.json</c> · 장비 슬롯 149/150 · 비용 <b>1.556e+33</b>):
        /// 옛 사다리는 끝 낱말 <c>T</c> 에 매달려 <b>«1556000000000000000000T»(23자)</b>를 냈고,
        /// <c>Cost/CostText</c> 는 rect 337 ↔ pref 1095(<b>3.25배</b>) · 탑바 골드는 241 ↔ 538(<b>2.23배</b>)로 <c>bestFit</c> 바닥에서도 잘렸다.
        /// </para>
        /// ⚠ 재는 것은 «어떤 글자인가» 가 아니라 <b>«수가 아무리 커도 글자가 짧게 머무는가»</b> 다 — 꼴을 바꾸고 싶은 다음 사람을 막지 않으려고 그렇게 잡았다.
        /// </summary>
        [Test]
        public void TheLadderStopsGrowingAtItsTopWord()
        {
            double cap = ShortNum.Units[0].From * ShortNum.MantissaCap;

            // ⓐ 경계 «바로 밑» 은 아직 사다리다(맨 윗 낱말이 붙는다) — 경계가 조용히 내려오면 여기서 잡힌다.
            string below = ShortNum.Fmt(cap * 0.999);
            Assert.That(below, Does.EndWith(ShortNum.Units[0].Suffix.ToString()),
                $"경계 바로 밑({cap * 0.999})은 아직 사다리여야 한다 — «{below}»");

            // ⓑ 경계부터는 «자리수 꼴» 이고, 그 뒤로는 아무리 커져도 글자가 안 자란다.
            double[] big = { cap, 1.556e33, 1e60, 1e120, 1e300, -1.556e33, -1e300 };
            int longest = 0; string worst = null;
            foreach (var x in big)
            {
                string s = ShortNum.Fmt(x);
                Assert.That(s, Does.Contain("e"), $"«{x}» 는 사다리 밖이라 자리수 꼴이어야 한다 — «{s}»");
                if (s.Length > longest) { longest = s.Length; worst = s; }
            }
            // 옛 꼴이 이 자리에서 낸 것이 23자다. 상한은 «-9.99e308» = 9자.
            Assert.That(longest, Is.LessThanOrEqualTo(12),
                $"자리수 꼴이 열두 자를 넘었다(«{worst}») — 길이에 상한이 없으면 이 절이 한 일이 없다(T463)");

            // ⓒ 가수가 «10» 으로 밀려 올라가는 자리 — «10e32» 는 자리수 꼴이 아니다.
            string round = ShortNum.Fmt(9.999e32);
            Assert.That(round, Does.StartWith("1"), $"9.999e32 의 가수가 10 으로 밀렸다 — «{round}»");

            // ⓓ 칸 꼴(Cell)도 같은 상한을 쓴다 — 칸이 Fmt 보다 좁으니 여기서 새면 더 크게 샌다.
            Assert.That(ShortNum.Cell(1.556e33), Does.Contain("e"), "칸 글자도 사다리 끝에서 자리수 꼴이다(T463)");
            Assert.That(ShortNum.Cell(1.556e33).Length, Is.LessThanOrEqualTo(12), "칸 글자 길이에도 상한이 있다");
        }

        /// <summary>
        /// T463 — 자리수 꼴의 «e» 를 <b>읽는 쪽이 배수로 알면 안 된다</b>.
        /// <para>
        /// 알게 하면 «e» 가 든 아무 글자나(«best»·«게임») 배수가 된다. 이 꼴이 되읽히는 자리(<c>RewardPopup.QtyOf</c> · 구슬)는 상한이 100이라
        /// 이만큼 큰 수가 애초에 안 오지만, <b>«안 온다» 를 글로만 두지 않고 여기 박아 둔다</b> — 나중에 되읽기가 필요해지면 표에 «e» 를 더할 것이 아니라 파서를 내야 한다.
        /// </para>
        /// </summary>
        [Test]
        public void TheExponentFormIsNotAUnitTheReaderCanMistake()
        {
            Assert.That(ShortNum.MultiplierOf('e'), Is.EqualTo(0.0), "«e» 는 배수가 아니다");
            Assert.That(ShortNum.MultiplierOf('E'), Is.EqualTo(0.0), "«E» 도 배수가 아니다");
            foreach (var u in ShortNum.Units)
                Assert.That(u.Suffix, Is.Not.EqualTo('e').And.Not.EqualTo('E'),
                    "표에 «e» 를 낱말로 더하면 자리수 꼴과 부딪힌다 — 그 길은 파서로 간다(T463)");
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
