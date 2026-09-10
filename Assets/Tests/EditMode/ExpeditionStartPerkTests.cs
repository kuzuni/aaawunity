using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T411 — 주인 2026-09-10 «원정 던전은 모든 등급 특전 다 나오게 하되 · <b>시작부터 특전 5개 선택하고 시작하는 거로</b>
    /// 하라고 했는데 안 돼 있네».
    /// <para>
    /// <b>«안 돼 있네» 의 정체는 한 낱말이었다</b> — T183 이 세운 것은 «다섯 개를 <b>준다</b>»(<see cref="Perks.SimPick"/> 이
    /// 판이 설 때 자동으로 집었다)이고 주인 뜻은 «다섯 번 <b>고른다</b>» 였다. 표(<c>dungeon.json</c>)는 처음부터 옳았다
    /// (<c>startPerks 5 · startLevel 5 · minPerkGrade 0</c>) — 그래서 <c>DungeonTicketTests</c> 는 내내 초록이었고
    /// <b>표만 보는 자는 이 고장을 못 본다.</b> 이 자가 재는 것은 표가 아니라 <b>판이 설 때 엔진이 하는 짓</b>이다.
    /// </para>
    /// <para>
    /// ⚑ <b>갈래가 둘인 것이 핵심이다</b> — 사람이 보는 판(<see cref="InteractivePolicy"/>)은 <see cref="BattleState.PendingLevelUps"/>
    /// 에 쌓아 화면이 3택을 다섯 번 띄우게 하고, 봇(<see cref="SimPolicy"/> · 헤드리스 · 골든·시뮬 대조)은 옛길 그대로 자동으로 집는다.
    /// 봇까지 쌓아 두면 팝업을 볼 눈이 없어 <b>판이 서지도 못한 채 멎는다</b> — 그래서 «둘 다» 를 재고, 봇 쪽이 이 회차의
    /// 회귀 관문이다(챕터 판 sim.js 골든이 그 길로 돈다).
    /// </para>
    /// <para>워커는 PlayMode 를 못 돌리므로(결정 143) 화면 배선이 아니라 엔진 규칙을 순수 C# 으로 잰다.</para>
    /// </summary>
    public class ExpeditionStartPerkTests
    {
        /// <summary>원정 판의 규칙 — 표(<c>dungeon.json</c>)의 그 세 칸을 그대로 옮긴 것이다.</summary>
        static RunOptions Expedition() => new RunOptions { StartPerks = 5, StartLevel = 5, MinPerkGrade = 0 };

        static BattleState Mk(IBattlePolicy policy, RunOptions opt, uint seed = 5)
        {
            var d = TestData.Load();
            return new BattleState(d, 1, GearSystem.MkBuild(d, 3, 9, 100), new Mulberry32(seed), policy, opt);
        }

        /// <summary>판이 서는 순간 특전이 <b>붙어 있으면 안 된다</b> — 다섯 번 «고를 기회» 가 줄에 서 있어야 한다.</summary>
        [Test]
        public void 원정_판이_서면_특전이_붙지_않고_고를_기회_다섯이_줄에_선다()
        {
            var G = Mk(new InteractivePolicy(), Expedition());

            Assert.That(G.Taken.Count, Is.EqualTo(0),
                "판이 서자마자 특전이 붙어 있다 — 주인이 «안 돼 있네» 라 한 그 꼴(자동 지급)이다");
            Assert.That(G.PendingLevelUps, Is.EqualTo(5),
                "고를 기회 다섯이 줄에 서야 화면이 3택을 다섯 번 띄운다");
            Assert.That(G.Pending, Is.Null, "판이 서는 그 자리에서는 아직 굴리지 않는다 — 창이 열릴 때 굴린다");
        }

        /// <summary>다섯 번 연달아 뜨고, 다 고르면 특전 다섯에 줄이 빈다 — <b>여섯 번째는 없다</b>.</summary>
        [Test]
        public void 원정_시작_3택이_다섯_번_연달아_뜨고_다_고르면_특전이_다섯이다()
        {
            var G = Mk(new InteractivePolicy(), Expedition());
            var picked = new List<PerkDef>();

            for (int i = 1; i <= 5; i++)
            {
                int guard = 0;
                while (G.Pending == null && !G.Over && guard++ < 200000) G.Tick();
                Assert.That(G.Pending, Is.Not.Null, $"{i}번째 3택이 안 떴다");
                Assert.That(G.Pending.Kind, Is.EqualTo(PendingKind.LevelUp), $"{i}번째 멈춤이 3택이 아니다");
                Assert.That(G.Pending.Offer.Count, Is.GreaterThan(1), $"{i}번째 후보가 하나뿐이면 «고르는» 것이 아니다");
                var pick = G.Pending.Offer[0];
                picked.Add(pick);
                G.ResolveLevelUp(pick);
            }

            Assert.That(G.Taken.Count, Is.EqualTo(5), "다섯 번 골랐는데 붙은 특전이 다섯이 아니다");
            Assert.That(picked, Is.EqualTo(G.Taken), "고른 것과 붙은 것이 다르다");
            Assert.That(G.PendingLevelUps, Is.EqualTo(0), "다 고른 뒤에도 줄이 남았다 — 여섯 번째 창이 뜬다");
        }

        /// <summary>
        /// 고른 뒤 <b>레벨 5</b> 이고 다음 렙업 요구량도 레벨 5 기준이다(주인 재차 «경험치 필요량도 특전 5개 얻었을 때
        /// 필요량처럼 · 레벨 5 느낌»). 레벨은 <c>StartLevel</c> 이 세우므로 이 회차가 만든 것이 아니라 <b>안 깨졌음을 지킨다</b>.
        /// </summary>
        [Test]
        public void 원정은_레벨_5_로_서고_다음_렙업_요구량도_레벨_5_기준이다()
        {
            var d = TestData.Load();
            var G = Mk(new InteractivePolicy(), Expedition());

            Assert.That(G.P.Level, Is.EqualTo(5), "원정은 레벨 5 로 시작한다(주인)");
            Assert.That(G.P.Exp, Is.EqualTo(0), "레벨만 올리고 경험치는 0 에서 센다");
            Assert.That(d.Tune.ExpNeed(G.P.Level), Is.EqualTo(d.Tune.ExpNeed(5)),
                "다음 렙업까지의 요구량이 레벨 5 의 그 값이어야 한다");
        }

        /// <summary>
        /// 원정 후보에는 <b>낮은 등급도 뜬다</b>(주인 «모든 등급 특전 다 나오게») — 지옥의 문(<c>minGrade 2</c>)과 갈리는 자리다.
        /// 한 시드로는 우연을 못 가르므로 여러 시드에서 시작 3택만 모아 «가장 낮은 등급이 한 번이라도 떴는가» 를 본다.
        /// </summary>
        [Test]
        public void 원정_시작_후보에는_가장_낮은_등급도_뜬다()
        {
            var grades = new HashSet<int>();
            for (uint seed = 1; seed <= 12; seed++)
            {
                var G = Mk(new InteractivePolicy(), Expedition(), seed);
                for (int i = 0; i < 5; i++)
                {
                    int guard = 0;
                    while (G.Pending == null && !G.Over && guard++ < 200000) G.Tick();
                    if (G.Pending == null) break;
                    foreach (var p in G.Pending.Offer) grades.Add(p.Grade);
                    G.ResolveLevelUp(G.Pending.Offer[0]);
                }
            }

            Assert.That(grades, Does.Contain(0),
                "가장 낮은 등급이 한 번도 안 떴다 — 원정의 minPerkGrade 가 0 이 아니게 됐다(주인 «모든 등급»)");
            Assert.That(grades.Count, Is.GreaterThan(1), "등급이 한 가지뿐이면 «모든 등급» 이 아니다");
        }

        /// <summary>
        /// ⚑ <b>이 회차의 회귀 관문</b> — 봇 판은 <b>옛길 그대로</b> 판이 설 때 자동으로 집는다.
        /// 여기까지 «쌓아 두기» 로 바꾸면 헤드리스가 첫 틱에서 <c>Pending</c> 에 걸려 <b>영영 안 끝나고</b>,
        /// 챕터 판 골든(sim.js 21칸)이 통째로 흔들린다.
        /// </summary>
        [Test]
        public void 봇_판은_옛길_그대로_판이_설_때_자동으로_집는다()
        {
            var G = Mk(new SimPolicy(), Expedition());

            Assert.That(G.Taken.Count, Is.EqualTo(5), "봇은 판이 설 때 다섯을 집어야 한다(옛길)");
            Assert.That(G.PendingLevelUps, Is.EqualTo(0), "봇에게 3택을 쌓아 두면 볼 눈이 없어 판이 멎는다");
            Assert.That(G.Pending, Is.Null, "봇 판이 시작부터 멈춰 섰다");
        }

        /// <summary>일반 챕터 전투(시작 특전 0)는 한 치도 안 바뀐다 — 이 고침이 새는 곳이 없는지 본다.</summary>
        [Test]
        public void 일반_챕터_판은_한_치도_안_바뀐다()
        {
            foreach (var policy in new IBattlePolicy[] { new InteractivePolicy(), new SimPolicy() })
            {
                var G = Mk(policy, new RunOptions());
                Assert.That(G.Taken.Count, Is.EqualTo(0), "일반 판에 시작 특전이 붙었다");
                Assert.That(G.PendingLevelUps, Is.EqualTo(0), "일반 판에 시작 3택이 줄에 섰다");
                Assert.That(G.P.Level, Is.EqualTo(1), "일반 판은 레벨 1 로 시작한다");
            }
        }
    }
}
