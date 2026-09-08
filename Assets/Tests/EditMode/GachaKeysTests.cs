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
    }
}
