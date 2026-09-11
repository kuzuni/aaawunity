using System;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T454 — <b>칸(프레임) 글자를 「내는」 쪽도 같은 낱말표를 보는가.</b>
    /// <para>
    /// T452(결정 1271)가 표를 <see cref="ShortNum"/> 한 자리로 내렸지만 옮긴 것은 <c>UiKit.Fmt</c> 와 <c>RewardPopup.QtyOf</c> 둘이었다.
    /// <b>세 번째 사다리</b>가 <c>GearUi.CellQtyText</c> 에 남아 있었고, 그 글자는 <c>LobbyPopups:1272</c>(출석 · <c>amount:</c> 없음)를 타고
    /// <c>QtyOf</c> 로 가 <b>다시 숫자로 읽힌다</b> — 곧 「같은 표를 본다」 가 <b>읽는 쪽에만</b> 서 있었다.
    /// </para>
    /// <para>
    /// ⚑ 이 집안의 고장은 오늘까지 셋이다 — 「1K」→1 로 <b>출석 다이아 1000 → 구슬 1개</b>(결정 1259 · <b>배포까지 갔다</b>) · T450 · T452.
    /// 셋 다 <b>배포까지 가서야</b> 드러났다: 내는 쪽·읽는 쪽이 전부 <c>Game</c> 이라 이 하니스가 컴파일조차 안 한다(결정 143).
    /// 그래서 셈을 <c>Core</c> 로 내리고 그 위에 자를 세운다 — <b>Core 는 매 회차 로컬에서 돈다.</b>
    /// </para>
    /// </summary>
    public class ShortNumCellTests
    {
        /// <summary>옛 <c>GearUi.CellQtyText</c> 의 리터럴 사다리 — <b>글자가 안 바뀌었는가</b> 를 재는 기준이다(T443 3회차의 그 꼴).</summary>
        static string Legacy(double n)
        {
            double a = Math.Abs(Math.Round(n));
            if (a >= 1e9) return (n / 1e9).ToString("0.#") + "B";
            if (a >= 1e6) return (n / 1e6).ToString("0.#") + "M";
            if (a >= 1e3) return (n / 1e3).ToString("0.#") + "K";
            return Math.Round(n).ToString("0");
        }

        /// <summary>
        /// 문턱 언저리에서 옛 사다리와 <b>글자가 한 자도 안 다르다</b>(1e12 미만).
        /// <para>⚠ 1e12 이상은 <b>일부러 다르다</b> — 아래 <see cref="AtATrillionTheCellFinallyGetsTheSuffixItAlwaysLacked"/> 가 그 자리를 따로 잰다.</para>
        /// </summary>
        [Test]
        public void CellIsByteIdenticalToTheLadderItReplaced()
        {
            double[] xs =
            {
                0, 1, 9, 99, 999, 1000, 1001, 1049.9, 1050, 1234, 5000, 9999, 10000, 12500, 99999,
                999999, 1000000, 1250000, 1500000, 999999999, 1000000000, 1500000000, 999999999999.0,
                -1, -999, -1000, -12500, -1500000,
            };
            foreach (var x in xs)
                Assert.That(ShortNum.Cell(x), Is.EqualTo(Legacy(x)),
                    "«" + x + "» 의 칸 글자가 옛 사다리와 다르다 — 화면 글자를 바꾸는 것은 이 절이 하려던 일이 아니다");
        }

        /// <summary>
        /// ⚑ <b>이 절이 태어난 까닭 그 자체</b> — 칸이 쓰는 접미사 전부를 <c>QtyOf</c> 가 아는 낱말이어야 한다.
        /// <para>칸 쪽에 낱말을 하나 더하고 읽는 쪽을 안 고치면 <b>여기서 빨개진다</b>. 지금까지 그 자리는 조용했고, 그래서 세 번 났다.</para>
        /// </summary>
        [Test]
        public void EverySuffixTheCellEmitsIsOneTheReaderKnows()
        {
            foreach (var u in ShortNum.Units)
            {
                string s = ShortNum.Cell(u.Unit);
                char last = s[s.Length - 1];
                Assert.That(char.IsLetter(last), Is.True, "단위 " + u.Unit + " 에서 «" + s + "» — 접미사가 안 붙었다");
                Assert.That(ShortNum.MultiplierOf(last), Is.EqualTo(u.Unit),
                    "칸이 «" + last + "» 를 쓰는데 읽는 쪽(RewardPopup.QtyOf)이 그 낱말을 같은 배수로 안 읽는다 — "
                    + "출석 보상은 amount 없이 이 글자로만 가므로(LobbyPopups:1272) 그 어긋남이 곧 구슬 개수다(결정 1259)");
            }
        }

        /// <summary>
        /// ⚠ <b>칸과 <c>Fmt</c> 의 문턱이 다른 것은 뜻이다</b> — 칸은 1e3 부터 줄이고(네 자 한계) <c>Fmt</c> 는 1e4 부터다(T81).
        /// <para>표를 「정리」하며 둘을 하나로 합치려는 손이 여기서 멈춘다 — <b>없앨 뻔한 예외는 자로 박아 둔다</b>(T452 가 K 행에 한 그대로).</para>
        /// </summary>
        [Test]
        public void TheCellShortensEarlierThanTheFormatterAndThatIsDeliberate()
        {
            Assert.That(ShortNum.Cell(1000), Is.EqualTo("1K"), "칸은 1,000 을 «1K» 로 쓴다(주인 레퍼런스 16 · 칸 네 자 한계 · T443 3회차)");
            Assert.That(ShortNum.Fmt(1000), Is.EqualTo("1,000"), "같은 수를 Fmt 은 아직 안 줄인다(문턱 1e4 · T81)");
            Assert.That(ShortNum.Cell(9999), Does.EndWith("K"), "칸은 9,999 도 이미 K 다");
            Assert.That(ShortNum.Fmt(9999), Is.EqualTo("9,999"), "Fmt 의 문턱 바로 아래 — 여기가 둘이 갈리는 자리다");
            Assert.That(ShortNum.Cell(999), Is.EqualTo("999"), "세 자리는 그대로 — 줄일 까닭이 없다");
        }

        /// <summary>
        /// 1e12 에서 <b>칸도 「T」 를 얻는다</b> — 옛 사다리는 B 까지라 «1000B» <b>다섯 자</b>를 냈다.
        /// <para>⚑ 네 자 한계가 이 함수의 존재 이유인데 옛 꼴은 가장 큰 수에서 그 이유를 스스로 깼다. 표가 하나가 되니 그 구멍이 같이 막혔다.</para>
        /// </summary>
        [Test]
        public void AtATrillionTheCellFinallyGetsTheSuffixItAlwaysLacked()
        {
            Assert.That(Legacy(1e12), Is.EqualTo("1000B"), "옛 사다리는 여기서 다섯 자를 냈다(이 자가 그 사실을 기록한다)");
            Assert.That(ShortNum.Cell(1e12), Is.EqualTo("1T"), "표를 한 자리로 모으니 칸도 T 를 쓴다");
            Assert.That(ShortNum.Cell(1e12).Length, Is.LessThanOrEqualTo(4), "칸 글자는 네 자를 넘지 않는다(T443 3회차 실측)");
            Assert.That(ShortNum.MultiplierOf('T'), Is.EqualTo(1e12), "그리고 읽는 쪽이 그 T 를 안다 — 아니면 이 개선이 곧 고장이다");
        }

        /// <summary>
        /// ⚑⚑ <b>화면 쪽이 정말 이 표를 쓰는가 — 글로 확인한다.</b>
        /// <para>
        /// 위 자들은 <see cref="ShortNum"/> 안에서만 돈다. 정작 화면이 부르는 <c>GearUi.CellQtyText</c> 는 <c>Game</c> 이라
        /// <b>이 하니스가 컴파일하지 않는다</b>(결정 143) — 곧 그쪽이 <b>제 사다리를 다시 적어도 위 자들은 전부 초록</b>이다.
        /// <b>T452·T454 가 태어난 꼴이 바로 그것</b>이므로, 자가 못 닿는 자리는 <b>소스 글자로라도 잰다.</b>
        /// </para>
        /// </summary>
        [Test]
        public void TheCellCallerDelegatesToThisTableInsteadOfKeepingItsOwn()
        {
            string gearUi = TestData.ReadRepo("Assets/Scripts/Game/GearUi.cs");

            StringAssert.Contains("ShortNum.Cell(n)", gearUi,
                "GearUi.CellQtyText 가 ShortNum 에 위임하지 않는다 — 사다리를 여기 다시 적으면 읽는 쪽과 또 어긋난다(T454)");

            // 옛 사다리의 지문 — 돌아왔으면 여기서 잡는다. (⚠ 바늘은 «주석에도 안 나오는» 꼴로 고른다: 이 자가 제 주석에 걸리면 안 된다)
            StringAssert.DoesNotContain("(n / 1e9).ToString(\"0.#\")", gearUi,
                "GearUi 에 B·M·K 리터럴 사다리가 돌아왔다 — 낱말표는 ShortNum 한 자리다(T454)");
            StringAssert.DoesNotContain("(n / 1e6).ToString(\"0.#\")", gearUi,
                "GearUi 에 B·M·K 리터럴 사다리가 돌아왔다 — 낱말표는 ShortNum 한 자리다(T454)");
        }
    }
}
