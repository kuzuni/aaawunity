using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T96-profile 2단계 — 플레이어 이름 규칙(<see cref="Nickname"/>).
    /// 한도 2~12 는 주인이 지목한 <c>Social_Profile_Nickname</c> 조각 실측(«/12» · «Enter at least 2 characters.») 이고,
    /// 기본값은 아레나가 쓰던 이름 그대로라 <b>안 고치면 화면이 종전과 같다</b>.
    /// </summary>
    public class NicknameTests
    {
        static SaveData Fresh() => SaveData.NewSave(TestData.Load());

        [Test]
        public void LimitsComeFromThePrefabAndDefaultKeepsTheOldName()
        {
            Assert.AreEqual(2, Nickname.MinLen);
            Assert.AreEqual(12, Nickname.MaxLen);
            Assert.AreEqual("꼬마기사", Nickname.Default, "안 지었을 때 = 아레나 시상대가 쓰던 이름 그대로");
        }

        [Test]
        public void CleanTrimsSqueezesAndCuts()
        {
            Assert.AreEqual("홍길동", Nickname.Clean("  홍길동  "), "앞뒤 빈칸");
            Assert.AreEqual("홍 길동", Nickname.Clean("홍   길동"), "겹빈칸은 하나로");
            Assert.AreEqual("", Nickname.Clean(null));
            Assert.AreEqual("", Nickname.Clean("   "));
            Assert.AreEqual(Nickname.MaxLen, Nickname.Clean(new string('가', 30)).Length, "한도에서 자른다");
            Assert.AreEqual("abcDEF12", Nickname.Clean("abcDEF12"), "ASCII 는 그대로");
        }

        [Test]
        public void CleanDropsWhatTheFontCannotDraw()
        {
            // T75 — 글꼴에 없는 글자는 폭 0 으로 사라진다 → 이름으로 받지 않는다
            Assert.AreEqual("용사", Nickname.Clean("용💎사"), "이모지");
            Assert.AreEqual("용사", Nickname.Clean("용·사"), "가운뎃점");
            Assert.AreEqual("bc", Nickname.Clean("<b>c"), "리치 텍스트 꺾쇠는 뺀다(이름으로 태그를 심지 못하게)");
        }

        [Test]
        public void IsValidNeedsTwoDrawableCharacters()
        {
            Assert.IsFalse(Nickname.IsValid(null));
            Assert.IsFalse(Nickname.IsValid("가"), "한 자는 짧다");
            Assert.IsFalse(Nickname.IsValid("💎💎"), "그릴 수 없는 글자만 있으면 0자");
            Assert.IsTrue(Nickname.IsValid("가나"));
            Assert.IsTrue(Nickname.IsValid(new string('가', 30)), "길면 잘라서 받는다");
        }

        [Test]
        public void SetStoresTheCleanedNameOrRefuses()
        {
            var s = Fresh();
            Assert.AreEqual(Nickname.Default, Nickname.Of(s), "안 지었으면 기본 이름");
            Assert.IsTrue(Nickname.Set(s, "  용감한 기사  "));
            Assert.AreEqual("용감한 기사", s.Nick);
            Assert.AreEqual("용감한 기사", Nickname.Of(s));
            Assert.IsFalse(Nickname.Set(s, "가"), "짧으면 거절");
            Assert.AreEqual("용감한 기사", s.Nick, "거절하면 세이브는 그대로");
            Assert.IsFalse(Nickname.Set(null, "가나"));
        }

        [Test]
        public void SaveRoundTripsAndOldSavesFallBackToTheDefault()
        {
            var d = TestData.Load();
            var s = SaveData.NewSave(d);
            Nickname.Set(s, "꼬마용사");
            var back = SaveData.FromJson(s.ToJson(), d);
            Assert.AreEqual("꼬마용사", back.Nick, "이름이 왕복한다");
            // 이 필드가 없던 옛 세이브 — 빈 값으로 열리고 화면은 기본 이름을 쓴다(세이브 버전은 그대로다)
            var old = SaveData.FromJson("{\"v\":2,\"gold\":10,\"gem\":5,\"maxChapter\":3,\"selChapter\":1}", d);
            Assert.AreEqual("", old.Nick);
            Assert.AreEqual(Nickname.Default, Nickname.Of(old));
        }

        [Test]
        public void NormalizeCleansOrClearsABadName()
        {
            var d = TestData.Load();
            var s = SaveData.NewSave(d);
            s.Nick = "  용💎사  ";
            s.Normalize(d);
            Assert.AreEqual("용사", s.Nick, "다듬어 둔다");
            s.Nick = "가";
            s.Normalize(d);
            Assert.AreEqual("", s.Nick, "규칙에 못 미치면 «안 지었다» 로");
            Assert.AreEqual(Nickname.Default, Nickname.Of(s));
        }
    }
}
