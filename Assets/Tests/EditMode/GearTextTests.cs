using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T63-gear — 장비 세트 옵션 설명의 표시 문구(<see cref="GearText"/>). gear.json 의 desc 는 불변이고 세부 팝업(07) 옵션 줄이 본문 40 한 줄에 들어가게 표시 시점에만 줄인다.
    /// 긴 4종(도끼 3 트리거 · 회피 회복)은 기대 문구 그대로, 짧은 문구는 손대지 않고, 멱등이다.
    /// </summary>
    public class GearTextTests
    {
        static readonly Dictionary<string, string> Expected = new Dictionary<string, string>
        {
            { "치명타 시 50% 확률로 도끼 1개 (공격력의 50%)", "치명타 시 50%: 도끼 1개(공격력 50%)" },
            { "피격 시 50% 확률로 도끼 1개 (공격력의 50%)", "피격 시 50%: 도끼 1개(공격력 50%)" },
            { "회피 시 50% 확률로 도끼 1개 (공격력의 50%)", "회피 시 50%: 도끼 1개(공격력 50%)" },
            { "체력 50% 미만일 때 회피 시 30% 확률로 체력 10% 회복", "체력 50% 미만 회피 시 30%: 체력 10% 회복" },
        };

        [Test]
        public void LongDescriptions_ShortenToExpected()
        {
            foreach (var kv in Expected) Assert.That(GearText.Shorten(kv.Key), Is.EqualTo(kv.Value), kv.Key);
        }

        [Test]
        public void EveryGearOption_ShortOnesUnchanged_LongOnesInTable_AndIdempotent()
        {
            var d = TestData.Load();
            int longOnes = 0, total = 0;
            foreach (var kv in d.Gear.Options)
                foreach (var o in kv.Value)
                {
                    total++;
                    string got = GearText.Shorten(o.Desc);
                    if (Expected.TryGetValue(o.Desc, out var exp)) { longOnes++; Assert.That(got, Is.EqualTo(exp), kv.Key + " / " + o.Desc); }
                    // 짧은 문구는 길이를 안 건드린다 — 비율 스탯의 % 만 붙는다(T90 · «치명타 확률 +5» → «치명타 확률 +5%»)
                    else Assert.That(got, Is.EqualTo(StatText.Percent(o.Desc)), "짧은 문구는 % 말고 그대로: " + o.Desc);
                    Assert.That(GearText.Shorten(got), Is.EqualTo(got), "멱등: " + o.Desc);
                    Assert.That(got.Length, Is.LessThanOrEqualTo(30), "한 줄(본문 40 · 770px)에 들어가는 길이: " + got);
                }
            Assert.That(total, Is.GreaterThan(0), "옵션이 로드돼야 한다");
            Assert.That(longOnes, Is.GreaterThan(0), "긴 문구가 하나는 있어야 이 표가 의미 있다");
        }

        [Test]
        public void LockSuffix_DropsTheWordIsang_AndNullSafe()
        {
            Assert.That(GearText.LockSuffix("희귀"), Is.EqualTo(" (희귀)"));
            Assert.That(GearText.LockSuffix("신화 +3강"), Is.EqualTo(" (신화 +3강)"));
            Assert.That(GearText.LockSuffix(""), Is.EqualTo(""));
            Assert.That(GearText.Shorten(null), Is.Null); Assert.That(GearText.Shorten(""), Is.EqualTo(""));
        }
    }
}
