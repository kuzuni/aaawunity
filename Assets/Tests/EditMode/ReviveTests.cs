using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T254 1·2항 — <b>부활권</b>(주인 2026-09-09 «부활권 1개로 게임 1회 부활 가능하게 하기»).
    /// <para>
    /// 재는 것: ⓐ 1개로 한 번 부활 ⓑ 0개면 못 한다 ⓒ <b>판당 1회</b> 상한 ⓓ 부활하면 체력·실드가 가득이고 «죽었다» 가 내려간다
    /// ⓔ 클리어로 끝난 판은 부활거리가 아니다 ⓕ 세이브 왕복 · 옛 세이브는 0.
    /// </para>
    /// ⚠ <b>엔진은 안 건드린다</b>(지시서 2항) — 이 자도 판정식·난수를 안 본다. 시드 골든(T2)은 <c>SimGoldenTests</c> 가 그대로 지킨다.
    /// </summary>
    public class ReviveTests
    {
        static BattleState Fresh()
        {
            var d = TestData.Load();
            var s = SaveData.NewSave(d);
            return new BattleState(d, 1, s.CurBuild(d), new Mulberry32(12345u), new SimPolicy(), new RunOptions());
        }

        [Test]
        public void OneTicketRevivesOnceAndFillsHpAndShield()
        {
            var g = Fresh();
            var s = new SaveData { Revive = 1 };
            g.Dead = true; g.P.Hp = 0; g.P.Sh = 0;

            int used = 0;
            Assert.IsTrue(Revive.Can(s, g, used), "죽었고 부활권이 있으면 된다");
            Assert.IsTrue(Revive.Use(s, g, ref used));
            Assert.AreEqual(0, s.Revive, "부활권 1 이 줄었다");
            Assert.AreEqual(1, used);
            Assert.IsFalse(g.Dead, "«죽었다» 가 내려간다");
            Assert.AreEqual(g.P.MaxHp, g.P.Hp, "체력 가득");
            Assert.AreEqual(g.P.MaxSh, g.P.Sh, "실드 가득");
        }

        [Test]
        public void WithoutATicketNothingHappens()
        {
            var g = Fresh(); var s = new SaveData { Revive = 0 };
            g.Dead = true; g.P.Hp = 0;
            int used = 0;
            Assert.IsFalse(Revive.Can(s, g, used));
            Assert.IsFalse(Revive.Use(s, g, ref used));
            Assert.IsTrue(g.Dead, "죽은 채 그대로다");
            Assert.AreEqual(0, used);
        }

        [Test]
        public void OnlyOncePerRunEvenWithTicketsLeft()
        {
            var g = Fresh(); var s = new SaveData { Revive = 5 };
            g.Dead = true;
            int used = 0;
            Assert.IsTrue(Revive.Use(s, g, ref used));
            g.Dead = true;                                  // 또 죽었다
            Assert.AreEqual(Revive.PerRun, used);
            Assert.IsFalse(Revive.Can(s, g, used), "판당 " + Revive.PerRun + "회 — 표가 남아 있어도 안 된다");
            Assert.IsFalse(Revive.Use(s, g, ref used));
            Assert.AreEqual(4, s.Revive, "못 썼으니 표도 안 줄어든다");
        }

        [Test]
        public void ARunThatEndedByClearingIsNotSomethingToReviveFrom()
        {
            var g = Fresh(); var s = new SaveData { Revive = 3 };
            g.Cleared = true;   // 죽은 게 아니라 깼다
            int used = 0;
            Assert.IsFalse(Revive.Can(s, g, used), "클리어로 끝난 판은 부활거리가 아니다");
            Assert.AreEqual(3, s.Revive);
        }

        [Test]
        public void ReviveRoundTripsInTheSaveAndOldSavesHaveNone()
        {
            var gd = TestData.Load();
            var s = new SaveData { Revive = 4 };
            Assert.AreEqual(4, SaveData.FromJson(s.ToJson(), gd).Revive);
            Assert.AreEqual(0, SaveData.FromJson("{\"v\":2,\"gold\":1}", gd).Revive, "옛 세이브는 0(마이그레이션)");
        }
    }
}
