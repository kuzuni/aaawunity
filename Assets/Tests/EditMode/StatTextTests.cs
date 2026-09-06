using System.Collections.Generic;
using System.Text.RegularExpressions;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T90 — «퍼센트로 써야 할 값에 % 를 붙인다»(주인 2026-09-07). 표는 <see cref="StatText.Ratio"/> 한 곳이고,
    /// 정본(aaaw perks.json·gear.json)의 desc 를 표시 함수(<see cref="PerkText.Format"/>·<see cref="GearText.Shorten"/>)에 통과시키면
    /// 비율 스탯 줄에 % 가 빠진 것이 <b>하나도 없어야</b> 한다. 절대값 스탯(공격력·체력·실드)에는 안 붙는다.
    /// </summary>
    public class StatTextTests
    {
        [Test]
        public void RatioStats_GetPercent_AbsoluteStats_DoNot()
        {
            Assert.That(StatText.Percent("회피 +8"), Is.EqualTo("회피 +8%"));
            Assert.That(StatText.Percent("회피율 +8"), Is.EqualTo("회피율 +8%"));       // 긴 이름이 먼저 걸린다
            Assert.That(StatText.Percent("치명타 확률 +5"), Is.EqualTo("치명타 확률 +5%"));
            Assert.That(StatText.Percent("치명타 피해 +20"), Is.EqualTo("치명타 피해 +20%"));
            Assert.That(StatText.Percent("반격률 +10"), Is.EqualTo("반격률 +10%"));
            Assert.That(StatText.Percent("처치 시 2초간 회피율 +40"), Is.EqualTo("처치 시 2초간 회피율 +40%"));
            Assert.That(StatText.Percent("평타 적중마다 치명타 확률 +1(치명타 시 초기화)"), Is.EqualTo("평타 적중마다 치명타 확률 +1%(치명타 시 초기화)"));
            // 절대값 · 부호 없는 값 · 다른 이름은 그대로
            Assert.That(StatText.Percent("공격력 +1234"), Is.EqualTo("공격력 +1234"));
            Assert.That(StatText.Percent("체력 1055"), Is.EqualTo("체력 1055"));
            Assert.That(StatText.Percent("신화 +3강"), Is.EqualTo("신화 +3강"));
            Assert.That(StatText.Percent("회피 시 33% 확률로 화살 1개 (공격력의 30%)"), Is.EqualTo("회피 시 33% 확률로 화살 1개 (공격력의 30%)"));
        }

        [Test]
        public void AlreadyPercent_IsUntouched_AndIdempotent()
        {
            foreach (var s in new[] { "흡혈 +8%", "최대 체력 +10%", "방어력 +8%", "가시갑옷 +100%", "치명타 배율 +30%", "회피 +8.5%" })
                Assert.That(StatText.Percent(s), Is.EqualTo(s), s);
            Assert.That(StatText.Percent(StatText.Percent("회피 +8")), Is.EqualTo("회피 +8%"), "멱등");
            Assert.That(StatText.Percent(null), Is.Null); Assert.That(StatText.Percent(""), Is.EqualTo(""));
            Assert.That(StatText.Missing("회피 +8"), Is.EqualTo("회피 +8"));
            Assert.That(StatText.Missing("회피 +8%"), Is.EqualTo(""));
            Assert.That(StatText.Missing(null), Is.EqualTo(""));
        }

        [Test]
        public void Signed_PutsPercentOnRatioStatsOnly()
        {
            Assert.That(StatText.Signed("회피", 8), Is.EqualTo("회피 +8%"));
            Assert.That(StatText.Signed("치명타 확률", -2.5), Is.EqualTo("치명타 확률 -2.5%"));
            Assert.That(StatText.Signed("공격력", 1234), Is.EqualTo("공격력 +1234"));
            Assert.That(StatText.IsRatio("회피"), Is.True); Assert.That(StatText.IsRatio("공격력"), Is.False);
        }

        [Test]
        public void EveryPerkDescription_ShownWithPercent()
        {
            var d = TestData.Load();
            int fixedUp = 0, total = 0;
            foreach (var p in d.Perks.Perks)
            {
                total++;
                string shown = PerkText.Format(p.Desc);
                Assert.That(StatText.Missing(shown), Is.EqualTo(""), "특전 «" + p.Id + "» 에 % 가 빠진 비율 스탯이 있다: " + shown);
                if (StatText.Missing(p.Desc) != "") fixedUp++;   // 원문에 % 가 빠져 있던 줄(정본 실측 14)
            }
            Assert.That(total, Is.GreaterThan(0), "특전이 로드돼야 한다");
            Assert.That(fixedUp, Is.GreaterThan(0), "원문에 % 가 빠진 특전이 있어야 이 게이트가 의미 있다(정본 실측 14줄)");
        }

        [Test]
        public void EveryGearOptionDescription_ShownWithPercent()
        {
            var d = TestData.Load();
            int total = 0, fixedUp = 0;
            foreach (var kv in d.Gear.Options)
                foreach (var o in kv.Value)
                {
                    total++;
                    string shown = GearText.Shorten(o.Desc);
                    Assert.That(StatText.Missing(shown), Is.EqualTo(""), kv.Key + " 옵션에 % 가 빠진 비율 스탯이 있다: " + shown);
                    if (StatText.Missing(o.Desc) != "") fixedUp++;
                }
            Assert.That(total, Is.GreaterThan(0), "옵션이 로드돼야 한다");
            Assert.That(fixedUp, Is.GreaterThan(0), "원문에 % 가 빠진 옵션이 있어야 한다(정본 실측: 치명타 확률 +5 · 치명타 피해 +20/+25 · 반격률 +10 · 회피 +8)");
        }

        [Test]
        public void RatioTable_IsOrderedLongestFirst()
        {
            // «회피율» 이 «회피» 보다 뒤에 있으면 «회피율 +8» 이 «회피» 로 먼저 걸려 못 붙는다 — 표를 늘릴 때의 함정을 여기서 막는다.
            var r = StatText.Ratio;
            for (int i = 0; i < r.Length; i++)
                for (int j = i + 1; j < r.Length; j++)
                    Assert.IsFalse(r[j].StartsWith(r[i], System.StringComparison.Ordinal),
                        "긴 이름이 앞이어야 한다: «" + r[j] + "» 가 «" + r[i] + "» 보다 뒤에 있다");
            foreach (var name in r) Assert.IsFalse(Regex.IsMatch(name, @"\d"), "이름에 숫자를 넣지 않는다: " + name);
        }
        /// <summary>
        /// 표(<see cref="StatText.Ratio"/>) <b>밖</b>의 이름이 정본에 새로 생기면 잡는다 — T90 1단계 테스트 둘은 «표에 든 이름» 만 보므로
        /// 나중에 aaaw 가 «공격 속도 +10» 같은 줄을 추가하면 <see cref="StatText.Missing"/> 이 아무것도 못 찾아 조용히 통과한다.
        /// 그래서 표시 함수를 거친 문구에서 «한글 이름 + 부호 숫자» 가 남는지 통째로 훑고, 남으면 <b>절대값 스탯 흰 목록</b>에 든 것만 봐준다.
        /// 지금 정본에서는 하나도 안 남는다(실측 0) — 새 이름이 나오면 여기서 빨개지고, 워커가 «비율이면 <see cref="StatText.Ratio"/> 에,
        /// 절대값이면 아래 흰 목록에» 한 줄 넣으면 된다.
        /// </summary>
        [Test]
        public void NoUnknownStatNameKeepsABareSignedNumber()
        {
            // 절대값이라 % 가 붙으면 안 되는 이름(지금 정본에는 이런 줄 자체가 없다 · 새로 생길 때를 위한 자리)
            var absolute = new[] { "공격력", "체력", "실드", "골드", "다이아", "경험치", "레벨" };
            // «이름 +N» 인데 뒤에 % · × · 배 · 강 · 숫자가 안 오는 자리(«신화 +3강» 같은 강화 표기는 뺀다)
            var rx = new Regex(@"(?<name>[가-힣]+(?: [가-힣]+)?)\s*[+\-]\d+(?:\.\d+)?(?![\d.]|\s*[%×배강])", RegexOptions.CultureInvariant);
            // 자기 점검 — 정본이 깨끗해서 «아무것도 안 걸려» 통과하는 것과, 정규식이 고장 나 «못 걸어» 통과하는 것을 가른다
            Assert.That(rx.IsMatch("공격 속도 +10"), Is.True, "표 밖의 새 이름을 잡아야 한다");
            Assert.That(rx.IsMatch("신화 +3강"), Is.False, "강화 표기는 안 걸린다");
            Assert.That(rx.IsMatch("회피 +8%"), Is.False, "이미 % 가 붙은 줄은 안 걸린다");
            var d = TestData.Load();
            var left = new List<string>();
            void Scan(string shown, string where)
            {
                if (string.IsNullOrEmpty(shown)) return;
                foreach (Match m in rx.Matches(shown))
                {
                    string name = m.Groups["name"].Value;
                    if (System.Array.IndexOf(absolute, name) >= 0) continue;
                    // 표에 든 이름인데 여기 걸렸다면 표시 함수를 안 거친 것이다 — 그것도 실패다
                    left.Add(where + " «" + m.Value + "» (" + shown + ")");
                }
            }
            foreach (var p in d.Perks.Perks) Scan(PerkText.Format(p.Desc), "특전 " + p.Id);
            foreach (var kv in d.Gear.Options) foreach (var o in kv.Value) Scan(GearText.Shorten(o.Desc), "장비 옵션 " + kv.Key);
            Assert.That(left, Is.Empty, "표시 문구에 % 없이 남은 «이름 +N» 이 있다 — 비율이면 StatText.Ratio 에, 절대값이면 이 테스트의 흰 목록에 넣어라:\n" + string.Join("\n", left.ToArray()));
        }

    }
}
