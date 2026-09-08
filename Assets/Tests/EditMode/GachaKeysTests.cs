using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T255 1단계 — 뽑기 <b>«키» 재화</b>(주인 2026-09-09 «희귀 상자는 파란색 키로 1회 뽑기 가능. 보라색 키로는 전설 상자 1회 가능.
    /// 노랑 키로는 신화 상자 1회 가능»).
    /// <para>
    /// 재는 것: ⓐ 주인이 말한 <b>색 → 상자</b> 짝 ⓑ 키 1개로 한 번 열리고 그 재화가 1 줄어든다 ⓒ 0개면 못 열고 <b>한 개도 안 깎인다</b>
    /// ⓓ 키가 없는 상자는 언제나 못 연다 ⓔ 세이브 왕복 · 옛 세이브는 0 ⓕ 보상 이름으로 지급된다(출석·던전·우편이 그 길을 쓴다).
    /// </para>
    /// ⚠ <b>뽑기 엔진은 안 본다</b> — 키는 «비용 수단» 만 바꾼다(지시서 2항). 확률·천장은 <c>GachaTests</c> 계열이 그대로 지킨다.
    /// </summary>
    public class GachaKeysTests
    {
        [Test]
        public void ColorToBoxIsTheOwnersPairing()
        {
            Assert.AreEqual(GachaKeys.BoxRare, GachaKeys.BoxOf(GachaKeys.Blue), "파랑 = 희귀 상자");
            Assert.AreEqual(GachaKeys.BoxLegend, GachaKeys.BoxOf(GachaKeys.Purple), "보라 = 전설 상자");
            Assert.AreEqual(GachaKeys.BoxMyth, GachaKeys.BoxOf(GachaKeys.Yellow), "노랑 = 신화 상자");
            Assert.AreEqual(GachaKeys.Blue, GachaKeys.KeyOf(GachaKeys.BoxRare));
            Assert.AreEqual(GachaKeys.Purple, GachaKeys.KeyOf(GachaKeys.BoxLegend));
            Assert.AreEqual(GachaKeys.Yellow, GachaKeys.KeyOf(GachaKeys.BoxMyth));
            Assert.IsFalse(GachaKeys.IsKey(GachaKeys.Egg), "펫알은 상자를 여는 키가 아니다");
        }

        [Test]
        public void OneKeyOpensThatBoxOnceAndOnlyThatOne()
        {
            var s = new SaveData { KeyBlue = 1 };
            Assert.IsTrue(GachaKeys.CanOpen(s, GachaKeys.BoxRare));
            Assert.IsFalse(GachaKeys.CanOpen(s, GachaKeys.BoxLegend), "파란 키로 전설 상자를 열 수는 없다");

            Assert.IsTrue(GachaKeys.Open(s, GachaKeys.BoxRare));
            Assert.AreEqual(0, s.KeyBlue, "쓴 만큼만 줄어든다");
            Assert.IsFalse(GachaKeys.Open(s, GachaKeys.BoxRare), "이제 없다");
        }

        [Test]
        public void NotEnoughKeysSpendsNothing()
        {
            var s = new SaveData { KeyYellow = 1 };
            Assert.IsFalse(GachaKeys.Open(s, GachaKeys.BoxMyth, 2), "두 번 열 만큼은 없다");
            Assert.AreEqual(1, s.KeyYellow, "못 열었으면 한 개도 안 깎인다");

            var empty = new SaveData();
            Assert.IsFalse(GachaKeys.CanOpen(empty, GachaKeys.BoxRare));
            Assert.IsFalse(GachaKeys.Open(empty, GachaKeys.BoxRare));
            Assert.AreEqual(0, empty.KeyBlue);
        }

        [Test]
        public void ABoxWithNoKeyCanNeverBeOpenedWithOne()
        {
            var s = new SaveData { KeyBlue = 9, KeyPurple = 9, KeyYellow = 9 };
            Assert.IsNull(GachaKeys.KeyOf("noSuchBox"));
            Assert.IsFalse(GachaKeys.CanOpen(s, "noSuchBox"));
            Assert.IsFalse(GachaKeys.Open(s, "noSuchBox"));
            Assert.AreEqual(9, s.KeyBlue, "아무 키도 안 줄어든다");
            Assert.AreEqual(9, s.KeyPurple);
            Assert.AreEqual(9, s.KeyYellow);
        }

        [Test]
        public void KeysRoundTripInTheSaveAndOldSavesHaveNone()
        {
            var gd = TestData.Load();
            var s = new SaveData { KeyBlue = 2, KeyPurple = 3, KeyYellow = 4 };
            var back = SaveData.FromJson(s.ToJson(), gd);
            Assert.AreEqual(2, back.KeyBlue);
            Assert.AreEqual(3, back.KeyPurple);
            Assert.AreEqual(4, back.KeyYellow);

            var old = SaveData.FromJson("{\"v\":2,\"gold\":1}", gd);
            Assert.AreEqual(0, old.KeyBlue + old.KeyPurple + old.KeyYellow, "옛 세이브는 0(마이그레이션)");
        }

        [Test]
        public void RewardPathsCanPayKeys()
        {
            // 출석(T253 2·6일차)·던전·아레나 우편이 전부 이 이름으로 지급한다 —
            // 담을 자리가 없으면 그 보상이 조용히 버려진다(결정 633 과 같은 갈래).
            Assert.IsTrue(Mail.CanPay(GachaKeys.Blue));
            Assert.IsTrue(Mail.CanPay(GachaKeys.Purple));
            Assert.IsTrue(Mail.CanPay(GachaKeys.Yellow));
            Assert.AreEqual("희귀 열쇠", Mail.Name(GachaKeys.Blue), "말은 아레나 상인 표와 같다");

            var s = new SaveData();
            GachaKeys.Add(s, GachaKeys.Purple, 2);
            GachaKeys.Add(s, GachaKeys.Egg, 5);
            GachaKeys.Add(s, "noSuchItem", 99);
            GachaKeys.Add(s, GachaKeys.Blue, -3);
            Assert.AreEqual(2, s.KeyPurple);
            Assert.AreEqual(5.0, s.PetEgg, "펫알은 종전 자리 그대로다(T228)");
            Assert.AreEqual(0, s.KeyBlue, "음수·모르는 이름은 아무 일도 안 한다");
        }

        // ───────── T275 — «가진 만큼 한 번에»(캡은 표 값) ─────────

        /// <summary>주인이 든 예를 그대로 잰다: 1→1 · 8→8 · 17→10(그 뒤 7→7) · 0→0. 경계는 캡 언저리(9·10·11)다.</summary>
        [Test]
        public void UseCountIsHaveCappedAtTheTableCap()
        {
            const int Cap = 10;   // 표 값(gacha.json tenPullCount)을 흉내낸 것 — 코드가 아니라 부르는 쪽이 준다
            var s = new SaveData();
            foreach (var (have, want) in new[] { (0, 0), (1, 1), (8, 8), (9, 9), (10, 10), (11, 10), (17, 10), (7, 7) })
            {
                s.KeyBlue = have;
                Assert.AreEqual(want, GachaKeys.UseCount(s, GachaKeys.BoxRare, Cap), $"{have}개 → {want}회");
            }
        }

        /// <summary>17개로 한 번 누르면 10 이 빠지고 남은 7 이 그대로 «다음에 쓸 개수» 가 된다(주인 예의 뒷부분).</summary>
        [Test]
        public void SeventeenBecomesSevenAfterOnePress()
        {
            const int Cap = 10;
            var s = new SaveData { KeyBlue = 17 };
            int n = GachaKeys.UseCount(s, GachaKeys.BoxRare, Cap);
            Assert.AreEqual(10, n);
            Assert.IsTrue(GachaKeys.Open(s, GachaKeys.BoxRare, n));
            Assert.AreEqual(7, s.KeyBlue, "쓴 만큼만 빠진다");
            Assert.AreEqual(7, GachaKeys.UseCount(s, GachaKeys.BoxRare, Cap), "다음 번은 7/7");
        }

        /// <summary>못 쓰는 자리는 0 이다 — 키 없는 상자·모르는 상자·세이브 없음. 캡이 0 이하로 와도 «1회» 로 물러설 뿐 넘치지 않는다.</summary>
        [Test]
        public void UseCountIsZeroWhereKeysCannotBeSpent()
        {
            var s = new SaveData { KeyBlue = 5 };
            Assert.AreEqual(0, GachaKeys.UseCount(s, "noSuchBox", 10), "키가 없는 상자는 언제나 0");
            Assert.AreEqual(0, GachaKeys.UseCount(s, GachaKeys.BoxLegend, 10), "파란 키는 전설 상자를 못 연다");
            Assert.AreEqual(0, GachaKeys.UseCount(null, GachaKeys.BoxRare, 10));
            Assert.AreEqual(1, GachaKeys.UseCount(s, GachaKeys.BoxRare, 0), "표가 비면 T255 의 본디 «1회» 로 물러선다");
            Assert.AreEqual(1, GachaKeys.UseCount(s, GachaKeys.BoxRare, -3));
        }

        /// <summary>아주 큰 보유(우편 오지급 같은 사고)에도 캡이 먼저 걸린다 — <c>double → int</c> 로 넘겨 넘치는 자리가 없다.</summary>
        [Test]
        public void AbsurdlyManyKeysStillSpendOnlyTheCap()
        {
            var s = new SaveData { KeyYellow = int.MaxValue };
            Assert.AreEqual(10, GachaKeys.UseCount(s, GachaKeys.BoxMyth, 10));
            Assert.IsTrue(GachaKeys.Open(s, GachaKeys.BoxMyth, 10));
            Assert.AreEqual(int.MaxValue - 10, s.KeyYellow);
        }
    }
}
