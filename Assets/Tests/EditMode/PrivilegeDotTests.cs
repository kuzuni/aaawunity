using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T366 — 로비 «특권» 칸·카드 «받기» 버튼의 빨간 점이 보는 <b>판정 하나</b>(<see cref="Privilege.AnyClaimable"/>)의 자.
    /// 지시서 2항의 셋: ⓐ 오늘 아직 안 받은 «매일 수령» 이 있으면 켜진다 ⓑ 받으면 꺼진다(T167 «조건이 사라져야 꺼진다»)
    /// ⓒ <b>안 산 카드는 안 센다</b> — 공짜 카드를 받고 나면, 산 적 없는 세 카드가 남아 있어도 점은 꺼져 있어야 한다(안 그러면 점이 영영 안 꺼진다).
    /// 카드별 판정(<see cref="Privilege.Can"/>)은 <c>PrivilegeTests</c> 가 이미 못 박았다 — 여기는 «점» 이 묻는 질문만 잰다.
    /// </summary>
    public class PrivilegeDotTests
    {
        const string D1 = "2026-09-10", D2 = "2026-09-11";
        static PrivilegeData Load() => PrivilegeData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "privilege.json"))));
        static SaveData Fresh() => new SaveData();

        [Test]
        public void 새_세이브는_공짜_카드_때문에_점이_켜진다()
        {
            var d = Load(); var s = Fresh();
            Assert.IsTrue(Privilege.AnyClaimable(s, d, D1), "데일리 기프트(공짜)는 누구나 오늘 받을 수 있다 → 점 켜짐");
        }

        [Test]
        public void 안_산_카드는_점을_못_켠다()
        {
            var d = Load(); var s = Fresh();
            Assert.IsNotNull(Privilege.Claim(s, d, d.Of("dailyGift"), D1));
            // 남은 셋(광고 제거·월간·평생)은 산 적이 없다 — «살 수 있다» 는 받을 것이 아니다
            Assert.IsFalse(Privilege.AnyClaimable(s, d, D1), "안 산 카드만 남았으면 점은 꺼져 있어야 한다");
            Assert.IsTrue(Privilege.AnyClaimable(s, d, D2), "날이 바뀌면 공짜 카드가 다시 켠다");
        }

        [Test]
        public void 산_카드는_받을_때까지_점을_켠다()
        {
            var d = Load(); var s = Fresh();
            Privilege.Claim(s, d, d.Of("dailyGift"), D1);
            Assert.IsFalse(Privilege.AnyClaimable(s, d, D1));
            Assert.IsTrue(Privilege.Buy(s, d.Of("monthly"), D1));
            Assert.IsTrue(Privilege.AnyClaimable(s, d, D1), "산 날에도 그날치 매일 보상이 남아 있다 → 점 켜짐");
            Assert.IsNotNull(Privilege.Claim(s, d, d.Of("monthly"), D1));
            Assert.IsFalse(Privilege.AnyClaimable(s, d, D1), "받으면 꺼진다(T167)");
        }
    }
}
