using System.Collections.Generic;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T322 ⓓ — 시즌 패스 <b>세이브 배선</b>(주인 값과 무관한 배관).
    /// <para>
    /// 이 절이 서기 전에는 화면이 «지금 레벨» 을 <c>const 32</c> 로 들고 있었고 «받았다» 는
    /// «무료 열이고 지금 레벨보다 아래면 받은 것» 이라는 <b>짐작</b>이었다 — 담을 자리가 아예 없었다.
    /// </para>
    /// <para>
    /// ⚠ <b>보상 값은 아직 주인 몫이다</b>(T266 ⓑ · T377) — 그래서 «준다» 를 안 재고 «받았다고 적는다» 만 잰다.
    /// 그리고 <b>표가 모르는 칸은 못 받는다</b>: 값이 없는데 받아지면 화면에 «눌렀는데 아무 일도 안 나는 버튼» 이 생긴다(T257 · 결정 768).
    /// </para>
    /// </summary>
    public class PassSaveTests
    {
        static PassData Table(params (int lv, int col)[] known)
        {
            var d = new PassData { MaxLevel = 100 };
            foreach (var (lv, col) in known)
            {
                if (!d.Levels.TryGetValue(lv, out var row)) { row = new PassData.Reward[PassData.Cols]; d.Levels[lv] = row; }
                row[col] = new PassData.Reward("ui.gold", "100");
            }
            return d;
        }

        [Test]
        public void 저장했다_불러오면_그대로다()
        {
            var d = TestData.Load();
            var s = SaveData.NewSave(d); s.PassLv = 37; s.PassPaid1 = true; s.PassPaid2 = false;
            s.PassClaimed[30] = Pass.Bit(PassData.ColFree) | Pass.Bit(PassData.ColPaid1);
            s.PassClaimed[31] = Pass.Bit(PassData.ColFree);

            var back = SaveData.FromJson(s.ToJson(), d);

            Assert.AreEqual(37, back.PassLv, "레벨");
            Assert.IsTrue(back.PassPaid1, "유료 1 을 샀다");
            Assert.IsFalse(back.PassPaid2, "유료 2 는 안 샀다");
            Assert.IsTrue(Pass.Claimed(back, 30, PassData.ColFree), "30 무료 = 받음");
            Assert.IsTrue(Pass.Claimed(back, 30, PassData.ColPaid1), "30 유료1 = 받음");
            Assert.IsFalse(Pass.Claimed(back, 30, PassData.ColPaid2), "30 유료2 = 안 받음");
            Assert.IsTrue(Pass.Claimed(back, 31, PassData.ColFree), "31 무료 = 받음");
            Assert.IsFalse(Pass.Claimed(back, 32, PassData.ColFree), "32 는 적은 적이 없다");
        }

        [Test]
        public void 패스_칸이_없는_옛_세이브는_1레벨에_아무것도_안_받은_것이다()
        {
            // 옛 세이브 = 이 절이 서기 전에 저장된 것(패스 칸이 통째로 없다).
            var back = SaveData.FromJson("{\"gold\":5}", TestData.Load());
            Assert.AreEqual(1, back.PassLv, "없으면 1 레벨(0 이 아니다 — 패스 레벨은 1부터다)");
            Assert.IsFalse(back.PassPaid1); Assert.IsFalse(back.PassPaid2);
            Assert.AreEqual(0, back.PassClaimed.Count, "받은 칸도 없다");
        }

        [Test]
        public void 받을_수_있는_칸의_조건은_넷이고_하나만_빠져도_못_받는다()
        {
            var d = Table((10, PassData.ColFree), (10, PassData.ColPaid1), (20, PassData.ColFree));
            var s = new SaveData(); s.PassLv = 10;

            Assert.IsTrue(Pass.CanClaim(s, d, 10, PassData.ColFree), "ⓐ 레벨에 닿았고 ⓑ 무료 열이고 ⓒ 안 받았고 ⓓ 표가 안다");
            Assert.IsFalse(Pass.CanClaim(s, d, 20, PassData.ColFree), "ⓐ 레벨을 못 넘었다");
            Assert.IsFalse(Pass.CanClaim(s, d, 10, PassData.ColPaid1), "ⓑ 그 열을 안 샀다");
            Assert.IsFalse(Pass.CanClaim(s, d, 10, PassData.ColPaid2), "ⓓ 표가 그 칸을 모른다(그리고 안 샀다)");

            s.PassPaid1 = true;
            Assert.IsTrue(Pass.CanClaim(s, d, 10, PassData.ColPaid1), "사면 열린다");

            Assert.IsTrue(Pass.Claim(s, d, 10, PassData.ColFree), "받으면 참을 돌려준다");
            Assert.IsFalse(Pass.CanClaim(s, d, 10, PassData.ColFree), "ⓒ 두 번은 못 받는다");
            Assert.IsFalse(Pass.Claim(s, d, 10, PassData.ColFree), "두 번째 Claim 은 거짓이고 아무것도 안 바꾼다");
            Assert.IsTrue(Pass.Claimed(s, 10, PassData.ColFree), "받은 표시는 남는다");
        }

        [Test]
        public void 표가_값을_안_주면_아무_칸도_못_받는다()
        {
            // 지금 pass.json 이 딱 이 꼴이다 — 주인이 값을 안 줬고 워커는 지어내지 않는다(T266 ⓑ).
            var d = new PassData { MaxLevel = 100 };
            var s = new SaveData(); s.PassLv = 100; s.PassPaid1 = true; s.PassPaid2 = true;
            for (int lv = 1; lv <= 3; lv++)
                for (int col = 0; col < PassData.Cols; col++)
                    Assert.IsFalse(Pass.CanClaim(s, d, lv, col), $"표가 모르는 칸({lv}:{col})은 못 받는다");
            Assert.IsFalse(Pass.AnyClaimable(s, d), "그러니 «모두 받기» 도 켜질 것이 없다");
        }

        [Test]
        public void 레벨은_표의_상한으로_잘리고_세이브는_상한을_모른다()
        {
            var s = new SaveData(); s.PassLv = 500;
            Assert.AreEqual(100, Pass.Lv(s, new PassData { MaxLevel = 100 }), "표가 100 이면 100");
            Assert.AreEqual(50, Pass.Lv(s, new PassData { MaxLevel = 50 }), "표가 줄면 저절로 따라간다(세이브를 안 고친다)");
            Assert.AreEqual(500, s.PassLv, "자르는 것은 «읽을 때» 다 — 세이브 값은 그대로 남는다");
        }

        [Test]
        public void 망가진_세이브를_정리한다()
        {
            var s = SaveData.NewSave(TestData.Load()); s.PassLv = -3;
            s.PassClaimed[0] = 1;      // 레벨 0 = 없는 줄
            s.PassClaimed[-5] = 1;     // 음수 레벨
            s.PassClaimed[7] = 0;      // 아무 열도 안 든 빈 항목
            s.PassClaimed[8] = 1 | 64; // 모르는 비트가 섞였다(열이 셋인데 일곱째 열?)

            s.Normalize(TestData.Load());

            Assert.AreEqual(1, s.PassLv, "레벨은 1 아래로 안 내려간다");
            Assert.IsFalse(s.PassClaimed.ContainsKey(0), "레벨 0 은 지운다");
            Assert.IsFalse(s.PassClaimed.ContainsKey(-5), "음수 레벨도");
            Assert.IsFalse(s.PassClaimed.ContainsKey(7), "빈 항목도");
            Assert.AreEqual(1, s.PassClaimed[8], "모르는 비트만 지우고 아는 것은 남긴다");
        }
    }
}
