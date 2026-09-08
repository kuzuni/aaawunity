using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T240 3항 — 아레나 <b>1대1 진입점</b>의 자. 주인 «PvP 인게임 만들어줘 · 더미 데이터로 일단» ·
    /// 지시서 3항 «웨이브 없음 · 1대1 · 엔진은 새로 만들지 말고 지금 것을 재사용».
    /// <para>
    /// 여기서 재는 것은 넷이다 — ⓐ 판이 <b>정말 하나짜리</b> 인가 ⓑ 스탯이 <b>준 그대로</b> 들어갔나 ⓒ 이기고 지는 것이 <b>규칙대로</b> 갈리나
    /// ⓓ <b>기본값(일반 챕터 전투)이 한 자도 안 움직였나</b>.
    /// </para>
    /// <para>
    /// ⚠ ⓓ 가 이 자의 핵심이다 — 지시서 3항이 «기존 챕터 전투의 시드 골든(T2)은 건드리면 안 된다» 라고 못 박았고,
    /// 그것을 지키는 방법이 «조심한다» 가 아니라 <b>«기본값이 <c>null</c> 이면 엔진이 지나는 길이 그대로다»</b> 이기 때문이다.
    /// </para>
    /// </summary>
    public class ArenaDuelTests
    {
        static RunOptions Duel(double hp, double dmg) => new RunOptions
        {
            ArenaDuelFoe = new ArenaFoe.Stats { MaxHp = hp, Dmg = dmg },
        };

        [Test]
        public void ADuelIsOneNodeWithExactlyOneFoeAndNoChapterTable()
        {
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, 1, 0, 5);
            var g = new BattleState(d, 3, b, new Mulberry32(11), new SimPolicy(), Duel(500, 7));

            Assert.AreEqual(1, g.Nodes.Count, "1대1 은 노드 하나다 — 웨이브·이벤트·보스가 없다");
            Assert.AreEqual(1, g.Nodes[0].Enemies.Count, "적은 하나다");
            Assert.AreEqual(1, g.TotalEnemies);
            var foe = g.Nodes[0].Enemies[0];
            Assert.AreEqual(500, foe.MaxHp, "체력은 준 그대로");
            Assert.AreEqual(500, foe.Hp);
            Assert.AreEqual(7, foe.Dmg, "공격력은 준 그대로");
            Assert.IsFalse(foe.IsBoss, "보스가 아니다 — 보스로 만들면 보스 규칙·보스 외형이 딸려 오고, 2항의 «상대는 플레이어 캐릭터» 와 어긋난다");
            Assert.IsFalse(foe.Ranged);
        }

        [Test]
        public void KillingTheOneFoeIsTheWinAndDyingToItIsTheLoss()
        {
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, 1, 0, 5);

            // 이기는 판 — 상대를 아주 약하게 세운다(«그 판에 그 일이 일어나겠지» 가 아니라 **내가 그렇게 세운다** · §1 ⓐ).
            var win = new BattleState(d, 3, b, new Mulberry32(11), new SimPolicy(), Duel(1, 0)).RunToEnd();
            Assert.IsTrue(win.Clear, "적 하나를 잡으면 이긴 것이다(보스가 없어도 «클리어» 가 선다)");
            Assert.AreEqual(1, win.Kills, "그 하나 말고는 잡을 것이 없다");

            // 지는 판 — 상대를 아주 세게 세운다. 체력이 커서 못 잡고, 공격이 커서 내가 먼저 죽는다.
            var lose = new BattleState(d, 3, b, new Mulberry32(11), new SimPolicy(), Duel(1e9, 1e9)).RunToEnd();
            Assert.IsFalse(lose.Clear, "못 잡으면 «클리어» 가 아니다");
            Assert.AreEqual(0, lose.Kills);
        }

        [Test]
        public void TheDefaultRunIsUntouched_SameSeedSameNumbersAsTheGolden()
        {
            // ⓓ — 이 자가 빨개지면 «PvP 를 붙이다가 챕터 전투를 건드렸다» 는 뜻이다.
            // 값은 BattleTests 의 시드 골든과 **같은 수**다(sim.js 실측) — 여기서 다시 적는 까닭은
            // 그쪽 자는 «이식이 맞나» 를 보고, 이쪽 자는 «내 갈래가 그것을 안 흔들었나» 를 보기 때문이다.
            var d = TestData.Load(); var rng = new Mulberry32(11); var b = GearSystem.MkBuild(d, -1, 0, 0);
            var opt = new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false };
            Assert.IsFalse(opt.IsArenaDuel, "기본값은 «아레나 아님» 이다 — 이것이 골든의 안전장치다");

            var r0 = new BattleState(d, 3, b, rng, new SimPolicy(), opt).RunToEnd();
            Assert.That(r0.Clear, Is.False); Assert.That(r0.Time, Is.EqualTo(83.17).Within(0.01));
            Assert.That(r0.Level, Is.EqualTo(6)); Assert.That(r0.AtkTries, Is.EqualTo(73)); Assert.That(r0.Miss, Is.EqualTo(14));
            var r1 = new BattleState(d, 3, b, rng, new SimPolicy(), opt).RunToEnd();
            Assert.That(r1.Clear, Is.True); Assert.That(r1.Time, Is.EqualTo(88.43).Within(0.01)); Assert.That(r1.AtkTries, Is.EqualTo(83));
        }

        [Test]
        public void TheChapterNumberStopsMatteringOnceItIsADuel()
        {
            // 1대1 은 챕터 표를 아예 안 읽는다 — 그래서 «어느 챕터에서 들어왔나» 가 판을 바꾸지 않는다.
            // (아레나는 `App.StartBattle(SelChapter, …)` 로 들어오므로 사람마다 챕터가 다르다. 그것이 판을 바꾸면 «같은 상대» 가 사람마다 달라진다.)
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, 1, 0, 5);
            var a = new BattleState(d, 1, b, new Mulberry32(7), new SimPolicy(), Duel(400, 5));
            var z = new BattleState(d, 60, b, new Mulberry32(7), new SimPolicy(), Duel(400, 5));
            Assert.AreEqual(a.Nodes.Count, z.Nodes.Count);
            Assert.AreEqual(a.Nodes[0].Enemies[0].MaxHp, z.Nodes[0].Enemies[0].MaxHp);
            Assert.AreEqual(a.Nodes[0].Enemies[0].Dmg, z.Nodes[0].Enemies[0].Dmg);
            Assert.AreEqual(a.TotalEnemies, z.TotalEnemies);
        }

        [Test]
        public void TheFoeStatsComeStraightFromThePowerRuleWithNothingInvented()
        {
            // 표 → 스탯 → 판. 이 세 칸이 한 줄로 이어지는지 본다(중간에 «워커가 고른 수» 가 끼면 여기서 갈린다).
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, 1, 0, 5);
            var f = ArenaFoeData.Parse(System.IO.File.ReadAllText(
                TestData.RepoFile(System.IO.Path.Combine("Assets", "KkomaKnight", "arenaMatch.json"))));
            var stats = ArenaFoe.Of(f, 14_730, 0.5);

            var g = new BattleState(d, 3, b, new Mulberry32(11), new SimPolicy(), new RunOptions { ArenaDuelFoe = stats });
            var foe = g.Nodes[0].Enemies[0];
            Assert.AreEqual(stats.MaxHp, foe.MaxHp, "규칙이 낸 체력이 그대로 판에 선다");
            Assert.AreEqual(stats.Dmg, foe.Dmg, "규칙이 낸 공격력이 그대로 판에 선다");
            Assert.Greater(foe.MaxHp, 0); Assert.Greater(foe.Dmg, 0);
        }
    }
}
