using System;

namespace KkomaKnight.Core
{
    /// <summary>
    /// <b>«짧게 쓴 수» 의 낱말표 한 자리</b> — 내는 쪽(<c>UiKit.Fmt</c>)과 되읽는 쪽(<c>RewardPopup.QtyOf</c>)이 **같은 표**를 본다 (T452 · 결정 1271).
    /// <para>
    /// ⚑ <b>왜 Core 인가</b> — 이 표가 `Game` 에 있으면 그 어긋남을 **로컬에서 잴 수가 없다**: 하니스(`tools/dotnet/Tests`)는
    /// `Core` 만 참조하고 `Game` 은 PlayMode 에서만 돈다(워커는 PlayMode 를 못 돌린다 · 결정 143).
    /// 곧 «내는 쪽과 읽는 쪽이 같은 낱말을 아는가» 는 **Core 에 있어야 매 회차 돌아간다.** 그것이 이 파일이 여기 있는 까닭 전부다.
    /// </para>
    /// <para>
    /// ⚑ <b>같은 집안의 고장이 셋이었다</b> — ⓐ «1K» 를 1 로 읽어 <b>출석 다이아 1000 → 구슬 1개</b>(배포까지 갔다 · 결정 1259) ·
    /// ⓑ 짧게 쓴 글자 쪽(T450) · ⓒ <c>Fmt</c> 은 <b>넷</b>(T·B·M·K)을 내는데 <c>QtyOf</c> 는 <b>셋</b>(K·M·B)만 알았다(T452).
    /// 셋 다 «보여 준 글자를 다시 숫자로 읽는» 한 자리에서 났다. 표를 둘로 적는 한 넷째가 온다.
    /// </para>
    /// <para>
    /// ⚠ <b>K 만 «문턱 ≠ 단위» 다</b> — 네 자리(1e4)부터 K 로 쓰되 나누는 것은 1e3 이다(«12.5K» · T81 시절 선택).
    /// 표에 <see cref="Unit"/> 과 <see cref="From"/> 을 따로 둔 까닭이고, 둘을 하나로 합치면 그 자리가 조용히 바뀐다.
    /// </para>
    /// <para>
    /// ⚠ <b>글자 꼴은 한 자도 안 바꿨다</b> — 옛 <c>UiKit.Fmt</c> 의 리터럴 사다리를 그대로 표로 옮긴 것이고,
    /// 문화권 처리도 옛것 그대로 둔다(여기서 <c>InvariantCulture</c> 로 «고치면» 러너 문화권에 따라 화면 글자가 달라진다).
    /// </para>
    /// </summary>
    public static class ShortNum
    {
        /// <summary>낱말 하나 — 접미사 · 나누는 단위 · 그 접미사를 쓰기 시작하는 문턱 · 소수 자릿수 꼴.</summary>
        public readonly struct Unit_
        {
            public readonly char Suffix;
            public readonly double Unit;
            public readonly double From;
            public readonly string Format;
            public Unit_(char suffix, double unit, double from, string format)
            { Suffix = suffix; Unit = unit; From = from; Format = format; }
        }

        /// <summary>표 — <b>큰 것부터</b>. 새 낱말은 여기에만 더한다(내는 쪽·읽는 쪽이 저절로 따라온다).</summary>
        public static readonly Unit_[] Units =
        {
            new Unit_('T', 1e12, 1e12, "0.##"),
            new Unit_('B', 1e9,  1e9,  "0.##"),
            new Unit_('M', 1e6,  1e6,  "0.##"),
            new Unit_('K', 1e3,  1e4,  "0.#"),      // ⚠ 문턱만 1e4 다(위 주석)
        };

        /// <summary>«짧게 쓴 꼴» 로 (옛 <c>UiKit.Fmt</c> 과 글자까지 같다).</summary>
        public static string Fmt(double n)
        {
            n = Math.Round(n);
            double a = Math.Abs(n);
            foreach (var u in Units)
                if (a >= u.From) return (n / u.Unit).ToString(u.Format) + u.Suffix;
            return n.ToString("#,0");
        }

        /// <summary>
        /// <b>칸(프레임) 안에 적는 꼴</b> — <see cref="Fmt"/> 와 <b>같은 표</b>를 돌되 문턱만 <see cref="Unit_.Unit"/> 이다 (T454).
        /// <para>
        /// ⚠ <b>칸은 1e3 부터 줄인다</b>(«1,000» 다섯 자가 칸에 안 들어간다 · 칸 120px · 네 자가 한계 · T443 3회차 실측) —
        /// <see cref="Fmt"/> 의 문턱 1e4 와 <b>다른 것이 뜻</b>이고, 그 차이는 <c>ShortNumCellTests</c> 가 못 박는다(합치려는 손을 막는다).
        /// 주인 레퍼런스 <c>docs/ref/16_attendance.jpg</c> 도 그 자리를 «5000»·«10K» 로 적는다.
        /// </para>
        /// <para>
        /// ⚑ <b>왜 여기 있나</b> — 이 글자는 <c>LobbyPopups:1272</c>(출석 · <c>amount:</c> 없음)를 타고 <c>RewardPopup.QtyOf</c> 로 흘러가
        /// <b>다시 숫자로 읽힌다</b>. 곧 <b>내는 쪽</b>이다. 사다리를 <c>Game</c> 에 따로 적어 두면 읽는 쪽이 표를 봐도 소용이 없다 —
        /// 「같은 표를 본다」 가 <b>한쪽에만</b> 서게 되고, 그 어긋남은 이 하니스가 <c>Game</c> 을 컴파일조차 안 해 <b>배포까지 가서야</b> 드러난다(결정 143 · 1259).
        /// </para>
        /// <para>⚠ <b>옛 <c>GearUi.CellQtyText</c> 사다리와 글자가 같다</b> — 다른 것은 <c>|n| ≥ 1e12</c> 하나뿐이다(옛 «1000B» 다섯 자 → «1T»). 네 자 한계가 이 함수의 존재 이유라 새 쪽이 그 이유에 맞다.</para>
        /// </summary>
        public static string Cell(double n)
        {
            // ⚠ 반올림 자리를 옛 사다리 그대로 둔다 — **가름은 반올림한 값**으로 하고 **나누는 것은 원래 값**이다.
            //   여기서 `n = Math.Round(n)` 을 먼저 하면(그 꼴이 더 깔끔해 보인다) 1049.9 가 «1» → «1.1» 로 바뀐다. 글자를 바꾸는 것은 이 절의 일이 아니다.
            double a = Math.Abs(Math.Round(n));
            foreach (var u in Units)
                if (a >= u.Unit) return (n / u.Unit).ToString("0.#") + u.Suffix;
            return Math.Round(n).ToString("0");
        }

        /// <summary>그 글자가 <b>표에 있는 접미사</b>면 그 배수를, 아니면 0 을 돌려준다(대소문자 가리지 않는다).</summary>
        public static double MultiplierOf(char ch)
        {
            foreach (var u in Units)
                if (ch == u.Suffix || ch == char.ToLowerInvariant(u.Suffix)) return u.Unit;
            return 0.0;
        }

        /// <summary>표가 내놓는 접미사 전부(자가 «내는 쪽 ↔ 읽는 쪽» 을 맞대 보는 자리).</summary>
        public static char[] Suffixes()
        {
            var r = new char[Units.Length];
            for (int i = 0; i < Units.Length; i++) r[i] = Units[i].Suffix;
            return r;
        }
    }
}
