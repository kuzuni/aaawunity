using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T88 — 부위 재편(무기·목걸이·반지 = 공격력만 · 투구·갑옷·신발 = 체력·실드만)이 <b>총합을 한 자리도 바꾸지 않는다</b>는 것을 지키는 테스트.
    /// 재분배는 «보여 주는 값»(<see cref="GearSystem.ContributionIn"/>)에서만 일어나고 전투에 들어가는 <see cref="GearSystem.BuildPower"/> 는 그대로다.
    /// </summary>
    public class GearRoleTests
    {
        static Build RandomBuild(GameData D, Mulberry32 r)
        {
            var b = new Build();
            foreach (var pt in D.Gear.Parts)
            {
                b.Slots[pt] = (int)Math.Floor(r.Next() * (D.Gear.SlotLvMax + 1));
                if (r.Next() < 0.2) { b.Eq[pt] = null; continue; }   // 빈 슬롯도 섞는다
                var types = D.Gear.Types[pt];
                b.Eq[pt] = new GearItem
                {
                    Part = pt,
                    Type = types[(int)Math.Floor(r.Next() * types.Length)],
                    Rar = (int)Math.Floor(r.Next() * D.Gear.RarName.Length),
                    Plus = (int)Math.Floor(r.Next() * 10),
                };
            }
            return b;
        }

        /// <summary>임의 조합 100개 — 공격 부위는 체력·실드가 0, 방어 부위는 공격력이 0.</summary>
        [Test]
        public void AttackPartsShowOnlyAtkAndDefensePartsOnlyHpSh()
        {
            var D = TestData.Load(); var r = new Mulberry32(8801u);
            for (int i = 0; i < 100; i++)
            {
                var b = RandomBuild(D, r);
                foreach (var pt in D.Gear.Parts)
                {
                    if (b.EqAt(pt) == null) continue;
                    var c = GearSystem.ContributionIn(D, b, pt);
                    if (GearRole.IsAttack(pt)) { Assert.AreEqual(0.0, c.Hp, "«" + pt + "» 공격 부위 체력 0"); Assert.AreEqual(0.0, c.Sh, "«" + pt + "» 공격 부위 실드 0"); Assert.Greater(c.Atk, 0.0, "«" + pt + "» 공격 부위 공격력 > 0"); }
                    else { Assert.AreEqual(0.0, c.Atk, "«" + pt + "» 방어 부위 공격력 0"); Assert.Greater(c.Hp, 0.0, "«" + pt + "» 방어 부위 체력 > 0"); Assert.Greater(c.Sh, 0.0, "«" + pt + "» 방어 부위 실드 > 0"); }
                }
            }
        }

        /// <summary>임의 조합 100개 — 부위별 기여의 합 = 재분배 전(날 기여)의 합, 그리고 그 합으로 다시 만든 총합 = <see cref="GearSystem.BuildPower"/>.</summary>
        [Test]
        public void PartContributionsSumToTheUnchangedTotal()
        {
            var D = TestData.Load(); var r = new Mulberry32(8802u);
            for (int i = 0; i < 100; i++)
            {
                var b = RandomBuild(D, r);
                bool hasAtkPart = false, hasDefPart = false;
                double rawA = 0, rawH = 0, rawS = 0, newA = 0, newH = 0, newS = 0;
                foreach (var pt in D.Gear.Parts)
                {
                    var g = b.EqAt(pt); if (g == null) continue;
                    if (GearRole.IsAttack(pt)) hasAtkPart = true; else hasDefPart = true;
                    var raw = GearSystem.Contribution(D, g, b.SlotAt(pt)); rawA += raw.Atk; rawH += raw.Hp; rawS += raw.Sh;
                    var c = GearSystem.ContributionIn(D, b, pt); newA += c.Atk; newH += c.Hp; newS += c.Sh;
                }
                if (!hasAtkPart || !hasDefPart) continue;   // 한쪽 역할이 통째로 비면 그 몫은 보여 줄 자리가 없다(총합 표시는 BuildPower 라 그대로)
                Assert.That(newA, Is.EqualTo(rawA).Within(1e-9).Percent, "공격력 합 불변");
                Assert.That(newH, Is.EqualTo(rawH).Within(1e-9).Percent, "체력 합 불변");
                Assert.That(newS, Is.EqualTo(rawS).Within(1e-9).Percent, "실드 합 불변");

                var pw = GearSystem.BuildPower(D, b); double ev = GearSystem.EvenBonus(D, b);
                Assert.That((D.Tune.PAtk0 + newA) * ev, Is.EqualTo(pw.Atk).Within(1e-9).Percent, "부위 합으로 되짚은 총 공격력 = BuildPower");
                Assert.That((D.Tune.PHp0 + newH) * ev, Is.EqualTo(pw.Hp).Within(1e-9).Percent, "부위 합으로 되짚은 총 체력 = BuildPower");
                Assert.That((D.Tune.PSh0 + newS) * ev, Is.EqualTo(pw.Sh).Within(1e-9).Percent, "부위 합으로 되짚은 총 실드 = BuildPower");
            }
        }

        /// <summary>
        /// 총합 골든 — T88 <b>이전</b> 코드에서 뜬 값(같은 시드·같은 조합). 재편이 <see cref="GearSystem.BuildPower"/> 를 건드리면 여기서 빨개진다.
        /// 표 = {시드, Atk, Hp, Sh} (조합은 <see cref="RandomBuild"/> 가 그 시드로 만든 첫 빌드).
        /// </summary>
        static readonly object[] PowerGolden =
        {
            new object[] { 8811u, 19051.92360355556, 94672.51889911112, 151241.24914888892 },
            new object[] { 8812u, 1342.8472944444447, 6184.640971111112, 9683.650767777779 },
            new object[] { 8813u, 159.015876, 696.0325426666667, 1074.0333333333333 },
            new object[] { 8814u, 29807.628956, 148558.53264466667, 237501.787978 },
            new object[] { 8815u, 5030.740926666667, 24816.758071111108, 39572.06618888889 },
        };

        [TestCaseSource(nameof(PowerGolden))]
        public void BuildPowerIsUnchangedByTheRoleSplit(uint seed, double atk, double hp, double sh)
        {
            var D = TestData.Load();
            var pw = GearSystem.BuildPower(D, RandomBuild(D, new Mulberry32(seed)));
            Assert.That(pw.Atk, Is.EqualTo(atk).Within(1e-6).Percent, "총 공격력 불변");
            Assert.That(pw.Hp, Is.EqualTo(hp).Within(1e-6).Percent, "총 체력 불변");
            Assert.That(pw.Sh, Is.EqualTo(sh).Within(1e-6).Percent, "총 실드 불변");
        }

        /// <summary>부위 이름 — «장갑» 은 표시상 «반지»(데이터 파일은 aaaw 정본 그대로 «장갑»).</summary>
        [Test]
        public void GlovePartIsDisplayedAsRing()
        {
            var D = TestData.Load();
            Assert.AreEqual("장갑", D.Gear.PartName["glove"], "gear.json 원본은 그대로");
            Assert.AreEqual("반지", GearRole.DisplayName(D, "glove"), "표시는 «반지»(T88)");
            foreach (var pt in D.Gear.Parts)
            {
                if (pt == "glove") continue;
                Assert.AreEqual(D.Gear.PartName[pt], GearRole.DisplayName(D, pt), "«" + pt + "» 은 원본 이름 그대로");
            }
        }

        /// <summary>역할 표는 6부위를 3+3 으로 정확히 나눈다(빠지거나 겹치는 부위 없음).</summary>
        [Test]
        public void EveryPartHasExactlyOneRole()
        {
            var D = TestData.Load();
            var seen = new HashSet<string>();
            foreach (var pt in GearRole.AttackParts) Assert.IsTrue(seen.Add(pt), "중복 " + pt);
            foreach (var pt in GearRole.DefenseParts) Assert.IsTrue(seen.Add(pt), "중복 " + pt);
            Assert.AreEqual(D.Gear.Parts.Length, seen.Count, "역할 표 = gear.json 부위 수");
            foreach (var pt in D.Gear.Parts) Assert.IsTrue(GearRole.IsAttack(pt) ^ GearRole.IsDefense(pt), "«" + pt + "» 은 한 역할만");
        }

        /// <summary>같은 역할 안에서는 «원래 비율» 대로 나눈다 — 두 배 센 장비가 두 배를 가져간다.</summary>
        [Test]
        public void ShareFollowsTheOriginalRatioInsideARole()
        {
            var D = TestData.Load();
            var b = new Build();
            foreach (var pt in D.Gear.Parts) { b.Slots[pt] = 0; b.Eq[pt] = new GearItem { Part = pt, Type = D.Gear.Types[pt][0], Rar = 0, Plus = 0 }; }
            b.Eq["weapon"].Plus = 1;   // plusStep 만큼 «날» 공격력이 큰 무기
            double rawW = GearSystem.Contribution(D, b.Eq["weapon"], 0).Atk, rawN = GearSystem.Contribution(D, b.Eq["neck"], 0).Atk;
            double gotW = GearSystem.ContributionIn(D, b, "weapon").Atk, gotN = GearSystem.ContributionIn(D, b, "neck").Atk;
            Assert.That(gotW / gotN, Is.EqualTo(rawW / rawN).Within(1e-9).Percent, "몫의 비 = 날 기여의 비");
            Assert.Greater(gotW, rawW, "재분배로 공격 부위 몫은 늘어난다(방어 부위 몫을 나눠 받는다)");
        }
    }
}
