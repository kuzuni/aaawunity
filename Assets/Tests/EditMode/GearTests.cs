using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    public class GearTests
    {
        // PLAN §11.7 사다리 7점 확정 스탯 (공/체/실) — 엔진 실측이 0.5% 안에서 같아야 한다 (sim.js LADDER_STAT).
        static readonly object[] Ladder =
        {
            new object[] { -1, 0, 0, 25.0, 150.0, 250.0 },
            new object[] { 0, 0, 0, 50.0, 250.0, 400.0 },
            new object[] { 1, 0, 5, 108.9, 543.4, 868.9 },
            new object[] { 2, 0, 15, 524.7, 2619.1, 4188.9 },
            new object[] { 3, 0, 25, 3742.2, 18703.1, 29921.9 },
            new object[] { 3, 9, 50, 106912.0, 533475.0, 853125.0 },
            new object[] { 3, 9, 100, 190050.0, 948300.0, 1516500.0 },
        };

        [TestCaseSource(nameof(Ladder))]
        public void BuildPowerMatchesLadderTable(int rar, int plus, int slot, double atk, double hp, double sh)
        {
            // ⚑ 표는 `PreBalance()` — 위 사다리는 **aaaw sim.js 실측**(T2 이식 동일성 골든)이고 `rar` 칸의 −1·0·1·2·3 도
            //   그 시절 네 등급 표의 자리다. `Load()` 로 두면 주인이 밸런스를 고치는 날 «이식이 틀어졌다» 고 거짓말한다 —
            //   틀어진 것은 이식이 아니라 값이고, 그 값은 `BattleTests` 와 같은 손으로 갈라 둔다(T325 · 결정 1033).
            var d = TestData.PreBalance();
            var pw = GearSystem.BuildPower(d, GearSystem.MkBuild(d, rar, plus, slot));
            Assert.That(pw.Atk, Is.EqualTo(atk).Within(0.5).Percent);
            Assert.That(pw.Hp, Is.EqualTo(hp).Within(0.5).Percent);
            Assert.That(pw.Sh, Is.EqualTo(sh).Within(0.5).Percent);
        }

        [Test]
        public void PlusNineIsExactlyTwentyTimes()
        {
            // ⚑ **정본(aaaw) 쪽 자다** — «+9강 = ×20 정확» 은 `gear.json enhance.formula` 가 적어 둔 이식 동일성이고,
            //   이 레포가 도는 표는 T405 로 배율이 0.1 이다(주인 «그 전설 2강보다 신화 0강이 세야 함»).
            //   `Load()` 로 두면 주인이 제약을 지키라고 한 날 «이식이 틀어졌다» 고 거짓말한다 —
            //   틀어진 것은 이식이 아니라 값이고, 그 값은 `BuildPowerMatchesLadderTable` 과 같은 손으로 갈라 둔다(결정 1033 과 같은 자리).
            var d = TestData.PreBalance();
            Assert.That(1 + d.Gear.PlusStep * 9, Is.EqualTo(20.0));
        }

        /// <summary>이 레포가 실제로 도는 배율은 <b>주인 제약 안</b>이다 — 경계는 <c>1 + 2·plusStep &lt; 1.25</c>(전설 120 → 신화 150).</summary>
        [Test]
        public void ThisRepoPlusStepStaysInsideTheOwnersConstraint()
        {
            var G = TestData.Load().Gear;
            double ratio = G.Atk[G.RarMyth] / G.Atk[G.RarLegend];              // 등급 사이 비율의 최솟값(등차 표라 맨 위가 가장 좁다)
            Assert.That(1 + G.PlusStep * (G.LegendToMythPlus - 1), Is.LessThan(ratio),
                $"강화 배율 {G.PlusStep} 이 전설→신화 비율({ratio:F3})을 넘는다 — 강화가 등급을 덮는다(T405)");
        }

        [Test]
        public void FuseRules()
        {
            var d = TestData.Load(); var G = d.Gear;
            // 전설 아래 등급은 «한 등급 위» 가 나온다 — 표가 넷이든 다섯이든(영웅이 끼어도) 같은 규칙이다.
            // ⚠ 옛 줄은 «희귀를 합치면 전설» 이라고 적혀 있었는데, 그것은 규칙이 아니라 **네 등급 표에서만 참인 우연**이다
            //   (영웅이 끼면 희귀 위는 영웅이다). 규칙으로 적으면 표가 넓어져도 뜻이 산다.
            for (int r = 0; r < G.RarLegend; r++)
            {
                var it = new GearItem { Part = "weapon", Type = "crit_weapon", Rar = r };
                Assert.That(GearSystem.FuseMake(d, it).Rar, Is.EqualTo(r + 1), "«" + G.RarName[r] + "» 를 합치면 한 칸 위가 나온다");
            }
            var leg = new GearItem { Part = "weapon", Type = "crit_weapon", Rar = G.RarLegend, Plus = 0 };
            var l1 = GearSystem.FuseMake(d, leg);
            Assert.That(l1.Rar, Is.EqualTo(G.RarLegend)); Assert.That(l1.Plus, Is.EqualTo(1));
            var legMax = new GearItem { Part = "weapon", Type = "crit_weapon", Rar = G.RarLegend, Plus = G.LegendToMythPlus - 1 };
            var m0 = GearSystem.FuseMake(d, legMax);
            Assert.That(m0.Rar, Is.EqualTo(G.RarMyth)); Assert.That(m0.Plus, Is.EqualTo(0));
            var myth = new GearItem { Part = "weapon", Type = "crit_weapon", Rar = G.RarMyth, Plus = 4 };
            Assert.That(GearSystem.FuseMake(d, myth).Plus, Is.EqualTo(5));
            // 등급이 오르면 노강 공격이 오른다 — 표가 몇 칸이든 이것은 서야 한다.
            Assert.That(G.Atk[G.RarMyth], Is.GreaterThan(G.Atk[G.RarLegend]), "신화 노강 > 전설 노강");
            //
            // ⚑⚑ **주인 확정 제약(T405 · 주인 2026-09-10 «그 전설 2강보다 신화 0강이 세야 함»)** — 이 자가 T405 의 잣대다.
            //   T325 가 주인의 등차 표(30·60·90·120·150)를 넣으며 이 줄을 «못 선다» 고 지웠었다(정본 plusStep 2.111 이면
            //   전설 +2 = 626.7 > 신화 150 이고 **일반 +2(156.7)조차 신화 노강을 넘었다** — 강화가 등급을 통째로 덮었다).
            //   주인 답은 «제약을 지킨다» 였고, 그래서 gearOverride 의 enhance.plusStep 이 0.1 이 되었다(경계는 1 + 2p < 1.25 → p < 0.125).
            //   ⇒ 그 줄을 **되세운다**. 이 자가 빨개지면 뜻은 «강화 배율이 다시 등급을 덮었다» 이지 «표가 이상하다» 가 아니다.
            double legMaxAtk = G.Atk[G.RarLegend] * (1 + G.PlusStep * (G.LegendToMythPlus - 1));
            Assert.That(G.Atk[G.RarMyth], Is.GreaterThan(legMaxAtk),
                $"신화 노강({G.Atk[G.RarMyth]:F1}) > 전설 최대강(+{G.LegendToMythPlus - 1} = {legMaxAtk:F1}) — 주인 확정 제약(T405)");
            // 그리고 그 아래 등급도 같다 — «한 등급 최대강 < 다음 등급 노강» 이 표 전체에서 서야 강화가 등급을 안 덮는다.
            for (int r = 0; r < G.RarLegend; r++)
                Assert.That(G.Atk[r + 1], Is.GreaterThan(G.Atk[r] * (1 + G.PlusStep * (G.LegendToMythPlus - 1))),
                    $"«{G.RarName[r]}» 최대강 < «{G.RarName[r + 1]}» 노강");
        }

        [Test]
        public void FuseAllConsumesThreeAndSkipsEquipped()
        {
            var d = TestData.Load();
            var inv = new List<GearItem>();
            for (int i = 0; i < 4; i++) inv.Add(new GearItem { Uid = i + 1, Part = "helm", Type = "hpsh_helm", Rar = 0 });
            var equipped = new HashSet<GearItem> { inv[0] };
            int n = GearSystem.FuseAll(d, inv, equipped);
            Assert.That(n, Is.EqualTo(1));
            Assert.That(inv.Count, Is.EqualTo(2));
            Assert.That(inv.Contains(equipped.First()), Is.True);
        }

        // ───────── T24 — 장착 중 장비도 합성 재료(주인 2026-09-06) · 재료가 된 장착분의 슬롯엔 산출물(같은 부위)을 장착(승인 대기 29 기본값) ─────────
        static SaveData SaveWith(int n, string part, string type, int rar)
        {
            var S = new SaveData();
            for (int i = 0; i < n; i++) S.Inv.Add(S.NewGear(part, type, rar, 0));
            return S;
        }

        [Test]
        public void FuseAllWithoutExclusionConsumesEquippedAndEquipsTheResult()
        {
            var d = TestData.Load();
            var S = SaveWith(3, "helm", "hpsh_helm", 0);
            S.Eq["helm"] = S.Inv[0].Uid;                                                       // 재료 중 하나가 장착 중
            int n = GearSystem.FuseAll(d, S.Inv, null, g => S.Uid++, (mats, made) => GearSystem.ReEquipAfterFuse(S, mats, made));
            Assert.That(n, Is.EqualTo(1));
            Assert.That(S.Inv.Count, Is.EqualTo(1));
            var eq = S.EquippedGear("helm");
            Assert.That(eq, Is.Not.Null, "장착 슬롯이 비면 안 된다 — 산출물이 그 자리에");
            Assert.That(eq.Rar, Is.EqualTo(1)); Assert.That(eq.Uid, Is.GreaterThan(0)); Assert.That(S.IsEquipped(S.Inv[0]), Is.True);
        }

        [Test]
        public void FuseAllChainKeepsTheSlotOnTheFinalProduct()
        {
            var d = TestData.Load();
            var S = SaveWith(9, "helm", "hpsh_helm", 0);                                         // 9×일반 → 3×(한 칸 위) → 1×(두 칸 위)
            S.Eq["helm"] = S.Inv[4].Uid;
            int n = GearSystem.FuseAll(d, S.Inv, null, g => S.Uid++, (mats, made) => GearSystem.ReEquipAfterFuse(S, mats, made));
            Assert.That(n, Is.EqualTo(4));
            Assert.That(S.Inv.Count, Is.EqualTo(1));
            var eq = S.EquippedGear("helm");
            // 일반(0)에서 두 번 합쳐 올라간 자리 — «전설» 이라고 적으면 영웅이 끼는 날 틀린다(합성은 «한 칸씩» 이다).
            Assert.That(eq, Is.Not.Null); Assert.That(eq.Rar, Is.EqualTo(2)); Assert.That(eq, Is.SameAs(S.Inv[0]));
        }

        [Test]
        public void FuseDoesNotTouchSlotsWhoseGearWasNotAMaterial()
        {
            var d = TestData.Load();
            var S = SaveWith(3, "helm", "hpsh_helm", 0);
            var other = S.NewGear("helm", "hpsh_helm", 1, 0); S.Inv.Add(other); S.Eq["helm"] = other.Uid;   // 장착분은 다른 키(희귀) — 재료 아님
            var boot = S.NewGear("boot", "crit_boot", 0, 0); S.Inv.Add(boot); S.Eq["boot"] = boot.Uid;
            int n = GearSystem.FuseAll(d, S.Inv, null, g => S.Uid++, (mats, made) => GearSystem.ReEquipAfterFuse(S, mats, made));
            Assert.That(n, Is.EqualTo(1));
            Assert.That(S.Eq["helm"], Is.EqualTo(other.Uid), "재료가 아닌 장착분은 그대로(자동 장착 없음)");
            Assert.That(S.Eq["boot"], Is.EqualTo(boot.Uid));
        }

        [Test]
        public void ReEquipAfterFuseEmptiesTheSlotWhenTheProductIsAnotherPartOrHasNoUid()
        {
            var S = new SaveData();
            var a = S.NewGear("helm", "hpsh_helm", 0, 0); S.Inv.Add(a); S.Eq["helm"] = a.Uid;
            GearSystem.ReEquipAfterFuse(S, new List<GearItem> { a }, new GearItem { Uid = 99, Part = "armor", Type = "hpsh_armor", Rar = 1 });
            Assert.That(S.Eq.ContainsKey("helm"), Is.False, "부위가 다르면 빈 슬롯");
            var b = S.NewGear("helm", "hpsh_helm", 0, 0); S.Inv.Add(b); S.Eq["helm"] = b.Uid;
            GearSystem.ReEquipAfterFuse(S, new List<GearItem> { b }, new GearItem { Uid = 0, Part = "helm", Type = "hpsh_helm", Rar = 1 });
            Assert.That(S.Eq.ContainsKey("helm"), Is.False, "uid 없는 산출물은 가리킬 수 없다 → 빈 슬롯");
            GearSystem.ReEquipAfterFuse(S, null, null); GearSystem.ReEquipAfterFuse(null, new List<GearItem>(), null);   // 퇴화 입력에 예외 없음
        }

        [Test]
        public void GachaPityGivesMythAtCeilingAndLegendBonusWhenOverlapping()
        {
            var d = TestData.Load(); var box = d.Gacha.Box("myth");
            var st = new GachaState { P50 = box.PityMyth - 1, P10 = box.PityLegend - 1 };
            var got = GearSystem.GachaPull(d, st, box, new Mulberry32(1));
            Assert.That(got.Count, Is.EqualTo(2));
            Assert.That(got[0].Rar, Is.EqualTo(d.Gear.RarMyth));
            Assert.That(got[1].Rar, Is.EqualTo(d.Gear.RarLegend));
            Assert.That(st.P50, Is.EqualTo(0)); Assert.That(st.P10, Is.EqualTo(0));
        }

        [Test]
        public void GachaRateDistributionRoughlyMatchesTable()
        {
            var d = TestData.Load(); var box = d.Gacha.Box("rare");
            var st = new GachaState(); var rng = new Mulberry32(7); var cnt = new int[4];
            for (int i = 0; i < 20000; i++) foreach (var g in GearSystem.GachaPull(d, st, box, rng)) cnt[g.Rar]++;
            Assert.That(cnt[1] / 20000.0 * 100, Is.EqualTo(box.Rate[1]).Within(1.5));
            Assert.That(cnt[2] + cnt[3], Is.EqualTo(0));
        }

        // ───────── T26 — 뽑기 확률 검증(주인 «확률에 안 맞게 뽑히는 것 같다») · index.html gachaPull 과 줄 단위 동일 · 통계는 시드 고정 Mulberry32 ─────────
        const int GachaSample = 10000; const double GachaTolPct = 1.5;

        /// <summary>천장·피티가 절대 안 걸리게(매번 새 카운터) 뽑아 «자연 굴림» 만 센다 — 등급별 % 를 돌려준다.</summary>
        static double[] NaturalRollPct(GameData d, GachaBox box, IRng rng, int n)
        {
            var cnt = new int[box.Rate.Length];
            for (int i = 0; i < n; i++) { var got = GearSystem.GachaPull(d, new GachaState(), box, rng); Assert.That(got.Count, Is.EqualTo(1)); cnt[got[0].Rar]++; }
            var pct = new double[cnt.Length]; for (int r = 0; r < cnt.Length; r++) pct[r] = cnt[r] * 100.0 / n; return pct;
        }
        static void AssertPctMatchesTable(GachaBox box, double[] pct, string what)
        {
            for (int r = 0; r < box.Rate.Length; r++)
            {
                if (box.Rate[r] <= 0) Assert.That(pct[r], Is.EqualTo(0), $"{box.Key} {what}: 확률 0 인 등급 {r} 이 나왔다");
                else Assert.That(pct[r], Is.EqualTo(box.Rate[r]).Within(GachaTolPct), $"{box.Key} {what}: 등급 {r} 관측 {pct[r]:0.00}% ↔ 표 {box.Rate[r]}%");
            }
        }

        [Test]
        public void GachaNaturalRollMatchesRateTableForEveryBox()
        {
            var d = TestData.Load(); uint seed = 26;
            foreach (var box in d.Gacha.Boxes)
            {
                var pct = NaturalRollPct(d, box, new Mulberry32(seed++), GachaSample);
                AssertPctMatchesTable(box, pct, "자연 굴림 10,000회");
            }
        }

        [Test]
        public void GachaPityFiresExactlyAtTheCeilingAndNeverLater()
        {
            var d = TestData.Load(); var G = d.Gear;
            foreach (var box in d.Gacha.Boxes)
            {
                var st = new GachaState(); var rng = new Mulberry32(2026);
                int sinceLegend = 0, sinceMyth = 0, pityLegendHits = 0, pityMythHits = 0, overlaps = 0;
                for (int i = 0; i < 2 * GachaSample; i++)
                {
                    sinceLegend++; sinceMyth++;
                    bool expectPityL = box.PityLegend > 0 && sinceLegend >= box.PityLegend, expectPityM = box.PityMyth > 0 && sinceMyth >= box.PityMyth;
                    if (box.PityLegend > 0) Assert.That(sinceLegend, Is.LessThanOrEqualTo(box.PityLegend), $"{box.Key}: 전설 피티가 {box.PityLegend}회를 넘겨 걸렸다");
                    if (box.PityMyth > 0) Assert.That(sinceMyth, Is.LessThanOrEqualTo(box.PityMyth), $"{box.Key}: 신화 천장이 {box.PityMyth}회를 넘겨 걸렸다");
                    var got = GearSystem.GachaPull(d, st, box, rng); int rar = got[0].Rar;
                    if (expectPityM) { Assert.That(rar, Is.EqualTo(G.RarMyth), $"{box.Key}: {box.PityMyth}회째는 신화 확정"); pityMythHits++; }
                    if (expectPityL) { Assert.That(rar, Is.GreaterThanOrEqualTo(G.RarLegend), $"{box.Key}: {box.PityLegend}회째는 전설 이상 확정"); pityLegendHits++; }
                    if (expectPityM && expectPityL) { Assert.That(got.Count, Is.EqualTo(2)); Assert.That(got[1].Rar, Is.EqualTo(G.RarLegend)); overlaps++; }
                    else Assert.That(got.Count, Is.EqualTo(1), $"{box.Key}: 겹침이 아닌데 2개");
                    if (rar == G.RarMyth) sinceMyth = 0;
                    if (rar >= G.RarLegend) sinceLegend = 0;
                    Assert.That(st.P10, Is.EqualTo(sinceLegend)); Assert.That(st.P50, Is.EqualTo(sinceMyth)); Assert.That(st.Pulls, Is.EqualTo(i + 1));
                }
                if (box.PityLegend > 0) Assert.That(pityLegendHits, Is.GreaterThan(0), $"{box.Key}: 전설 피티가 한 번도 안 걸림(표본 부족?)");
                if (box.PityMyth > 0) Assert.That(pityMythHits, Is.GreaterThan(0), $"{box.Key}: 신화 천장이 한 번도 안 걸림(표본 부족?)");
                if (box.PityMyth > 0 && box.PityLegend > 0) Assert.That(overlaps, Is.GreaterThan(0), $"{box.Key}: 천장×피티 겹침이 한 번도 없음(표본 부족?)");
            }
        }

        // ───────── T261 — 희귀 상자의 «희귀 확정» 천장(주인 2026-09-09 «희귀 확정까지 10회 … 실제로 그런 식으로 기능되게») ─────────
        //  ⚠ 위 통계 자들은 이 천장을 **안 탄다** — `NaturalRollPct` 가 뽑을 때마다 새 카운터를 쓰기 때문이다(일부러 그렇게 만들었다:
        //    «자연 굴림» 만 재려고). 그래서 «덮기가 정말 걸렸는가» 는 저 자들이 초록인 것으로는 알 수 없고, 여기서 따로 잰다.

        [Test]
        public void RareBoxHasTheOwnersRarePityEvenThoughTheSourceTableHasNone()
        {
            var d = TestData.Load();
            var rare = d.Gacha.Box(GachaData.RareBoxKey);
            // 원본(`data/gacha.json`)에는 천장이 없다 — 그 사실을 여기 못 박아 둔다. 어느 날 원본이 갖게 되면 이 줄이 알려 준다.
            Assert.That(rare.PityMyth, Is.EqualTo(0), "희귀 상자에는 신화 천장이 없다(원본 그대로)");
            Assert.That(rare.PityLegend, Is.EqualTo(0), "희귀 상자에는 전설 피티가 없다(원본 그대로)");
            Assert.That(rare.PityRare, Is.EqualTo(GachaData.RarePity),
                        "희귀 상자의 «희귀 확정» 천장은 유니티 쪽에서 얹는다(T261 · 원본과 다름 · 주인 지시)");
            // 다른 상자는 안 건드린다 — 얹는 자리가 «rare 한 곳» 인지 확인한다.
            Assert.That(d.Gacha.Box("legend").PityRare, Is.EqualTo(0), "전설 상자에는 희귀 천장을 안 얹는다");
            Assert.That(d.Gacha.Box("myth").PityRare, Is.EqualTo(0), "신화 상자에도 안 얹는다");
            // ⚑ «전설 바로 아래» 로 적으면 안 된다 — 영웅이 끼는 날 그 자리가 영웅이 되어
            //   T261 의 «희귀 확정» 천장이 조용히 «영웅 확정» 이 된다(그래서 GearData.RarRare 의 유도식을 바꿨다 · 결정 969).
            //   재야 할 것은 자리가 아니라 **그 자리가 가리키는 등급**이다.
            Assert.That(d.Gear.RarName[d.Gear.RarRare], Is.EqualTo("희귀"), "«희귀 확정» 천장이 가리키는 등급은 희귀여야 한다");
        }

        [Test]
        public void RarePityFiresExactlyAtTheTenthMissAndResetsOnAnyRareOrBetter()
        {
            var d = TestData.Load(); var G = d.Gear;
            var box = d.Gacha.Box(GachaData.RareBoxKey);
            var st = new GachaState(); var rng = new Mulberry32(7);
            int since = 0, hits = 0;
            for (int i = 0; i < 20 * GachaSample / 100; i++)          // 2,000회면 천장이 여러 번 걸린다
            {
                since++;
                bool expect = since >= box.PityRare;
                Assert.That(since, Is.LessThanOrEqualTo(box.PityRare), "희귀 천장이 정해진 회수를 넘겨 걸렸다");
                int rar = GearSystem.GachaPull(d, st, box, rng)[0].Rar;
                if (expect) { Assert.That(rar, Is.GreaterThanOrEqualTo(G.RarRare), $"{box.PityRare}회째는 희귀 이상 확정"); hits++; }
                if (rar >= G.RarRare) since = 0;                       // 희귀 «이상» 이면 되돌린다(전설·신화도 포함)
                Assert.That(st.PRare, Is.EqualTo(since), "카운터가 시험이 세는 것과 같이 움직인다");
            }
            Assert.That(hits, Is.GreaterThan(0), "천장이 한 번도 안 걸렸다(표본 부족?)");
        }

        [Test]
        public void RarePityRaisesRareShareWithoutBreakingTheRestOfTheDistribution()
        {
            // 천장은 «희귀 비율» 만 올린다 — 확률 0 인 등급(전설·신화)이 희귀 상자에서 나오면 안 된다.
            var d = TestData.Load(); var G = d.Gear;
            var box = d.Gacha.Box(GachaData.RareBoxKey);
            var st = new GachaState(); var rng = new Mulberry32(99);
            var cnt = new int[box.Rate.Length];
            for (int i = 0; i < GachaSample; i++) cnt[GearSystem.GachaPull(d, st, box, rng)[0].Rar]++;
            double rarePct = cnt[G.RarRare] * 100.0 / GachaSample;
            Assert.That(rarePct, Is.GreaterThan(box.Rate[G.RarRare]),
                        $"천장이 있으니 희귀 비율({rarePct:0.00}%)이 표({box.Rate[G.RarRare]}%)보다 높아야 한다");
            for (int r = 0; r < box.Rate.Length; r++)
                if (box.Rate[r] <= 0) Assert.That(cnt[r], Is.EqualTo(0), $"확률 0 인 등급 {r} 은 천장이 있어도 안 나온다");
            Assert.That(cnt[G.RarRare] + cnt[0], Is.EqualTo(GachaSample), "희귀 상자는 일반·희귀 둘만 나온다");
        }

        [Test]
        public void RarePityCounterSurvivesASaveRoundTripAndOldSavesStartAtZero()
        {
            // 옛 세이브에는 `pRare` 키가 없다 — 0 으로 읽혀야 하고(천장이 늦게 오지 빨리 오면 안 된다) 새 세이브는 값을 지켜야 한다.
            var s = new SaveData();
            s.GachaBoxes[GachaData.RareBoxKey] = new GachaState { P50 = 1, P10 = 2, Pulls = 3, PRare = 7 };
            var D = TestData.Load();
            var back = SaveData.FromJson(s.ToJson(), D);
            Assert.That(back.GachaBoxes[GachaData.RareBoxKey].PRare, Is.EqualTo(7), "저장했다 읽으면 그대로다");
            var old = SaveData.FromJson("{\"gachaBoxes\":{\"rare\":{\"p50\":1,\"p10\":2,\"pulls\":3}}}", D);
            Assert.That(old.GachaBoxes[GachaData.RareBoxKey].PRare, Is.EqualTo(0), "옛 세이브는 0 에서 시작한다(손해 없음)");
        }

        [Test]
        public void TenPullIsTenSinglePullsAndFreshCounterGuaranteesLegendPerTen()
        {
            var d = TestData.Load(); var G = d.Gear; int ten = d.Gacha.TenPullCount; Assert.That(ten, Is.EqualTo(10));
            foreach (var box in d.Gacha.Boxes)
            {
                // ⓐ 같은 시드·같은 상태에서 «10연차(한 스트림으로 10번)» 와 «1회 ×10(같은 스트림을 이어 씀)» 은 같은 결과 — ShopScreen.Pull(n) 은 GachaPull 을 n 번 도는 것뿐이다.
                var a = new List<string>(); var b = new List<string>();
                { var st = new GachaState(); var rng = new Mulberry32(99); for (int i = 0; i < ten; i++) foreach (var g in GearSystem.GachaPull(d, st, box, rng)) a.Add(g.Rar + ":" + g.Type); }
                { var st = new GachaState(); var rng = new Mulberry32(99); for (int i = 0; i < ten; i++) { var one = GearSystem.GachaPull(d, st, box, rng); foreach (var g in one) b.Add(g.Rar + ":" + g.Type); } }
                Assert.That(a, Is.EqualTo(b), box.Key);
                // ⓑ 10연차 묶음(자연 굴림)의 분포도 표와 같다 — 1,000묶음 × 10 = 10,000
                var cnt = new int[box.Rate.Length]; var rng2 = new Mulberry32(1000);
                for (int k = 0; k < GachaSample / ten; k++) for (int i = 0; i < ten; i++) foreach (var g in GearSystem.GachaPull(d, new GachaState(), box, rng2)) cnt[g.Rar]++;
                var pct = new double[cnt.Length]; for (int r = 0; r < cnt.Length; r++) pct[r] = cnt[r] * 100.0 / GachaSample;
                AssertPctMatchesTable(box, pct, "10연차 1,000묶음");
                // ⓒ 피티가 있는 상자는 카운터 0 에서 시작한 10연차마다 전설 이상이 최소 1개(10회째 피티) · 묶음 크기는 10(겹침이면 11)
                if (box.PityLegend == ten)
                {
                    var rng3 = new Mulberry32(3);
                    for (int k = 0; k < 1000; k++)
                    {
                        var st = new GachaState(); int legendPlus = 0, n = 0;
                        for (int i = 0; i < ten; i++) foreach (var g in GearSystem.GachaPull(d, st, box, rng3)) { n++; if (g.Rar >= G.RarLegend) legendPlus++; }
                        Assert.That(legendPlus, Is.GreaterThanOrEqualTo(1), $"{box.Key}: 새 카운터의 10연차 #{k} 에 전설 이상이 0개");
                        Assert.That(n, Is.EqualTo(ten).Or.EqualTo(ten + 1), box.Key);
                    }
                }
            }
        }

        /// <summary>
        /// ShopScreen.Pull 은 원본(index.html `grand = Math.random`)과 달리 뽑을 때마다 `Mulberry32(TickCount ^ 0x5bd1e995)` 를 새로 만든다 —
        /// «연속 시계 시드의 첫 굴림» 이 편향되면 실제 게임의 1회 연타가 표와 어긋난다. 1ms 간격·16ms 간격(Windows 틱) 둘 다 표 ±1.5%p 여야 한다.
        /// </summary>
        [TestCase(1)]
        [TestCase(16)]
        public void GachaClockSeededSinglePullsAreUnbiased(int tickStep)
        {
            var d = TestData.Load(); uint tick0 = 0x12345678u;
            foreach (var box in d.Gacha.Boxes)
            {
                var cnt = new int[box.Rate.Length];
                for (int k = 0; k < GachaSample; k++)
                {
                    var rng = new Mulberry32((tick0 + (uint)(k * tickStep)) ^ 0x5bd1e995u);
                    cnt[GearSystem.GachaPull(d, new GachaState(), box, rng)[0].Rar]++;
                }
                var pct = new double[cnt.Length]; for (int r = 0; r < cnt.Length; r++) pct[r] = cnt[r] * 100.0 / GachaSample;
                AssertPctMatchesTable(box, pct, $"시계 시드 {tickStep}ms 간격 1회 ×10,000");
            }
        }

        [Test]
        public void GachaTypePickIsUniformAcrossAllTypes()
        {
            var d = TestData.Load(); var box = d.Gacha.Box("rare"); var types = d.Gear.AllTypes; int n = types.Count * 2000;
            var cnt = new Dictionary<string, int>(); foreach (var t in types) cnt[t.Part + "|" + t.Type] = 0;
            var rng = new Mulberry32(77);
            for (int i = 0; i < n; i++) foreach (var g in GearSystem.GachaPull(d, new GachaState(), box, rng)) cnt[g.Part + "|" + g.Type]++;
            double expect = 100.0 / types.Count;
            foreach (var kv in cnt) Assert.That(kv.Value * 100.0 / n, Is.EqualTo(expect).Within(GachaTolPct), $"종류 {kv.Key} 관측 ↔ 균등 {expect:0.00}%");
        }
    }

    static class SetExt { public static T First<T>(this HashSet<T> s) { foreach (var x in s) return x; return default; } }
}
