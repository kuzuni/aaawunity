using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T458 1항 — 전투 사건에 <b>«왜 떴는지»</b> 한 칸(<see cref="BattleEvent.Src"/>).
    /// <para>
    /// 주인 2026-09-12: «해당 특전이나 해당 장비로 인해 번개 나왔으면 <b>해당 꺼 아이콘</b>이 데미지 텍스트에 떠야 함».
    /// 화면(2항)이 그 아이콘을 그리려면 <b>엔진이 출처를 같이 실어 줘야</b> 한다 — 이 자는 그 칸이 실제로 실리는지를 잰다.
    /// </para>
    /// <para>
    /// ⚠ <b>이 자는 «아이콘» 을 안 잰다</b> — 그림은 2항(<c>BattleWorld</c>)의 몫이고 여기서 재는 것은 «출처가 사건에 붙어서 온다» 하나다.
    /// 값(데미지·난수)은 <see cref="BattleTests"/> 의 황금값이 지킨다: 이 회차는 칸을 <b>더하기만</b> 했으므로 그 자들이 그대로 초록이어야 한다.
    /// </para>
    /// </summary>
    public class BattleEventSourceTests
    {
        /// <summary>사다리 모드(3택 없음 · 표 차례대로 자동 획득)로 돌린다 — 시드가 같으면 늘 같은 길이라 «어떤 특전이 붙는가» 가 흔들리지 않는다.</summary>
        static RunOptions Ladder() => new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false, EmitEvents = true };

        static List<BattleEvent> RunAndCollect(int chapter, uint seed, int maxTicks = 200000)
        {
            var d = TestData.PreBalance();
            var rng = new Mulberry32(seed);
            var b = GearSystem.MkBuild(d, -1, 0, 0);
            var G = new BattleState(d, chapter, b, rng, new SimPolicy(), Ladder());
            var all = new List<BattleEvent>();
            int guard = 0;
            while (!G.Over && guard++ < maxTicks)
            {
                G.Tick();
                if (G.Events.Count > 0) { all.AddRange(G.Events); G.Events.Clear(); }   // 화면이 매 프레임 하는 그 일
            }
            return all;
        }

        /// <summary>출처로 쓸 수 있는 낱말인가 — 특전 id(«p_…») 이거나 장비·펫 둘 중 하나다(아무 글자나 실리면 화면이 엉뚱한 아이콘을 찾는다).</summary>
        static void AssertLooksLikeSource(string src, string what)
        {
            Assert.That(src, Is.Not.Empty, what + " 의 출처가 빈 글자다 — 모르면 null 이어야 한다(빈 글자는 «있다» 로 읽힌다)");
            Assert.That(src.StartsWith("p_") || src == BattleState.SrcGearAxe || src == BattleState.SrcGearHeal
                        || src == BattleState.SrcGearThorns || src == BattleState.SrcPet, Is.True,
                $"{what} 의 출처 «{src}» 가 특전 id 도 장비·펫도 아니다 — 화면은 이 낱말로 아이콘을 찾는다(T458 1항)");
        }

        [Test]
        public void SummonedHitsCarryTheSourceThatFiredThem()
        {
            var evs = RunAndCollect(3, 11);
            Assert.That(evs.Count, Is.GreaterThan(0), "사건이 하나도 안 왔다 — EmitEvents 가 꺼졌거나 판이 안 돌았다");

            int projs = 0, srcHits = 0, plainHits = 0;
            foreach (var ev in evs)
            {
                switch (ev.Kind)
                {
                    case EvKind.Proj:
                        projs++;
                        Assert.That(ev.Proj, Is.Not.Null, "Proj 사건에는 투사체가 실린다");
                        AssertLooksLikeSource(ev.Proj.Src, "투사체(" + ev.Proj.Kind + ")");
                        break;
                    case EvKind.Bolt:
                        AssertLooksLikeSource(ev.Src, "번개");
                        break;
                    case EvKind.Hit:
                        if (ev.Src == null) plainHits++;
                        else { srcHits++; AssertLooksLikeSource(ev.Src, "타격"); }
                        break;
                }
            }
            // 사다리 3챕터 시드 11 은 p_arrowEv·p_axeHit 를 집는다(BattleTests 황금값에 그 목록이 박혀 있다) ⇒ 투사체가 반드시 난다.
            Assert.That(projs, Is.GreaterThan(0), "이 판에서 투사체가 하나도 안 났다 — 자가 재려던 길을 안 지났다");
            Assert.That(srcHits, Is.GreaterThan(0), "출처가 붙은 타격이 하나도 없다 — 소환 타격에 출처가 안 실린다(T458 1항)");
            Assert.That(plainHits, Is.GreaterThan(0), "기본 공격 타격이 하나도 없다 — «아이콘 없음» 갈래를 못 쟀다");
        }

        /// <summary>투사체가 낸 타격의 출처 = <b>그 투사체를 쏜 것</b>. 쏘는 자리와 맞는 자리가 프레임으로 떨어져 있어 실을 놓치기 쉬운 자리다.</summary>
        [Test]
        public void AHitFromAProjectileNamesTheSameSourceAsTheProjectile()
        {
            var evs = RunAndCollect(3, 11);
            var fired = new HashSet<string>();
            foreach (var ev in evs) if (ev.Kind == EvKind.Proj && ev.Proj?.Src != null) fired.Add(ev.Proj.Src);
            Assert.That(fired.Count, Is.GreaterThan(0), "쏜 투사체가 없다");

            int matched = 0;
            foreach (var ev in evs)
            {
                if (ev.Kind != EvKind.Hit || ev.Src == null) continue;
                // 출처가 붙은 타격은 «소환» 이 낸 것이고, 그 낱말은 이 판에서 실제로 쏜 것들 가운데 있어야 한다
                // (번개는 투사체가 아니라 즉시 타격이라 여기 안 걸릴 수 있다 — 그래서 «하나라도» 로 잰다).
                if (fired.Contains(ev.Src)) matched++;
            }
            Assert.That(matched, Is.GreaterThan(0), "투사체가 맞혔는데 그 출처가 타격에 안 실렸다 — 쏜 자리와 맞는 자리 사이에서 끊긴다");
        }

        /// <summary>칸을 더하기만 했다는 것 — 같은 시드로 두 번 돌리면 사건 줄이 글자 하나까지 같다(난수·순서 불변 · T2 이식 동일성).</summary>
        [Test]
        public void AddingTheSourceDoesNotMoveTheEngine()
        {
            var a = RunAndCollect(3, 11);
            var b = RunAndCollect(3, 11);
            Assert.That(b.Count, Is.EqualTo(a.Count), "같은 시드인데 사건 수가 다르다");
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(b[i].Kind, Is.EqualTo(a[i].Kind), $"{i}번째 사건의 종류가 다르다");
                Assert.That(b[i].Value, Is.EqualTo(a[i].Value).Within(1e-9), $"{i}번째 사건의 값이 다르다");
                Assert.That(b[i].Src, Is.EqualTo(a[i].Src), $"{i}번째 사건의 출처가 다르다");
            }
        }
    }
}
