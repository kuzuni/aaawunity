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

        /// <summary>
        /// T494 — <b>회복 식구(<see cref="EvKind.Heal"/>·<see cref="EvKind.Repair"/>)에 «때리는» 장비 낱말이 실리면 운다.</b>
        /// <para>
        /// ⚑ <b>왜 이 자가 있어야 하나</b> — T491 이 «gear» 한 낱말을 <c>SrcGearAxe</c>·<c>SrcGearHeal</c>·<c>SrcGearThorns</c> 로 쪼개
        /// «장비가 준 회복에 도끼가 붙어 뜨던» 사고를 고쳤는데, 그때 박은 것은 <b>화면 표</b>(낱말 → 그림)뿐이었다.
        /// 검수 Q 가 자를 한 줄씩 읽어 갈랐다(T494 등재 ③): <c>Battle.cs:585</c> 의 <c>Heal(…, src: SrcGearHeal)</c> 을 <c>SrcGearAxe</c> 로 되돌려도
        /// <b>그 절의 자 넷이 다 초록</b>이다 — <see cref="AssertLooksLikeSource"/> 는 «아는 낱말인가» 만 보고,
        /// 결정론 검사는 <b>같은 빌드를 두 번</b> 맞대므로 둘 다 틀리면 조용하며, 나머지는 <c>Hit</c>·<c>Bolt</c>·<c>Proj</c> 만 훑는다.
        /// ⇒ <b>엔진이 낱말을 싣는 자리와 화면이 그림으로 옮기는 자리 사이</b>에 아무도 안 선 한 칸이 있었다.
        /// </para>
        /// <para>
        /// ⚠ <b>표를 베끼지 않는다</b> — «Axe·Thorns 면 운다» 로 적으면 내일 여섯째 낱말이 생기는 날 이 자가 <b>조용히</b> 그것을 놓친다
        /// (T449·결정 1264 ⑤ 가 값을 치른 그 꼴: 지금 있는 다섯을 세고 다음에 생길 여섯째를 안 세는 손).
        /// 그래서 <b>식구</b>로 잰다 — «<c>gear</c> 로 시작하는 낱말 가운데 회복의 것이 아닌 것». 그러면 새 장비 낱말은 <b>태어나는 순간부터</b> 이 줄 안에 든다.
        /// 그 규칙이 조용해지지 않게 <b>«회복 낱말도 그 집안 이름을 쓴다»</b> 를 먼저 못 박는다 — 상수를 갈아 접두사가 달라지면 식구 판정이 아무도 안 걸러 낸다.
        /// </para>
        /// <para>
        /// ⚠ <b>판을 세워서 잰다</b>(등재 ④ⓑ) — <c>:585</c> 는 «회피 + 낮은 피 + 장비 옵션 + 확률» 넷이 겹쳐야 나는 자리라 보통 판에서는 한 번도 안 지난다
        /// (실측: 등급·슬롯·시드·챕터를 훑어도 <c>g_evHeal</c> 이 붙는 조합이 <b>안 나왔다</b>). 그래서 옵션과 회피를 직접 세우고 피를 낮게 잡아 그 문을 연다 —
        /// <b>무엇이 실리는가는 여전히 엔진이 정한다</b>(내가 세운 것은 «그 길을 지나게 하는 조건» 뿐이다).
        /// 그리고 <b>지났다는 수</b>를 같이 단언한다: 0이면 초록이 아니라 «못 쟀다» 로 운다(T278 · 결정 930).
        /// </para>
        /// </summary>
        [Test]
        public void HealsNeverCarryAnAttackFlavouredGearSource()
        {
            // ⓐ 식구 판정이 조용해지지 않게 — 장비 낱말 셋이 같은 집안 이름을 쓴다는 것부터 못 박는다.
            const string GearFamily = "gear";
            Assert.That(BattleState.SrcGearHeal.StartsWith(GearFamily), Is.True,
                $"회복 장비 낱말 «{BattleState.SrcGearHeal}» 이 «{GearFamily}» 집안이 아니다 — 아래 식구 판정이 아무도 안 거른다(T494)");
            Assert.That(BattleState.SrcGearAxe.StartsWith(GearFamily) && BattleState.SrcGearThorns.StartsWith(GearFamily), Is.True,
                "때리는 장비 낱말도 같은 집안이어야 식구로 잴 수 있다(T494)");

            // ⓑ 그 문이 열리는 판을 세운다 — 옵션·회피·피만 세우고 «무엇이 실리는가» 는 엔진에 맡긴다.
            var d = TestData.PreBalance();
            var G = new BattleState(d, 3, GearSystem.MkBuild(d, -1, 0, 0), new Mulberry32(11), new SimPolicy(), Ladder());
            G.P.Px["g_evHeal"] = 3;     // 장비 옵션 «회피 시 회복»(gear.json 의 그 키) — 이것이 없으면 :585 의 반복문이 0바퀴다
            G.P.Evade = 90;             // 회피가 나야 그 문 앞까지 간다

            var evs = new List<BattleEvent>();
            int guard = 0;
            while (!G.Over && guard++ < 200000)
            {
                if (G.P.Hp > G.P.MaxHp * LowHpForEvHeal) G.P.Hp = G.P.MaxHp * LowHpForEvHeal;   // «낮은 피» 를 유지한다(:585 의 앞 조건)
                G.Tick();
                if (G.Events.Count > 0) { evs.AddRange(G.Events); G.Events.Clear(); }
            }

            int healFamily = 0, gearHeals = 0;
            foreach (var ev in evs)
            {
                if (ev.Kind != EvKind.Heal && ev.Kind != EvKind.Repair) continue;
                healFamily++;
                if (ev.Src == null) continue;                       // 출처를 모르는 회복은 그림 없이 뜬다 — 옳다
                if (!ev.Src.StartsWith(GearFamily)) continue;       // 특전(«p_…»)·펫은 이 줄이 가리는 자리가 아니다
                Assert.That(ev.Src, Is.EqualTo(BattleState.SrcGearHeal),
                    $"회복({ev.Kind})에 «{ev.Src}» 가 실렸다 — 장비가 준 회복 숫자에 **때리는 것의 그림**이 붙어 뜬다(T491 이 고친 그 사고 · T494). "
                    + "고칠 곳은 이 자가 아니라 그 회복을 부르는 자리의 src 다.");
                gearHeals++;
            }

            Assert.That(healFamily, Is.GreaterThan(0), "회복 사건이 하나도 안 났다 — 판이 안 돌았거나 EmitEvents 가 꺼졌다");
            Assert.That(gearHeals, Is.GreaterThan(0),
                "장비가 준 회복이 한 번도 안 났다 — 이 자는 **초록이 아니라 «못 쟀다»** 다(T278 · 결정 930). "
                + "엔진의 그 갈래(회피 → 낮은 피 → g_evHeal → 확률)가 움직였는지 먼저 보라 — 문턱을 낮춰 초록을 만들지 마라.");
        }

        /// <summary><c>Battle.cs</c> <c>EngineConst.LowHpEvHeal</c> 아래를 유지하려고 쓰는 값 — 상수를 읽지 않고 <b>더 낮게</b> 잡는다(상수가 움직여도 이 판은 계속 열린다).</summary>
        const double LowHpForEvHeal = 0.30;

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
