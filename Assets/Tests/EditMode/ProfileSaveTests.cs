using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T96-profile — 고른 아바타 테두리 색은 세이브에 남는다(<see cref="SaveData.ProfileColor"/>).
    /// 이 레포 전용 필드라 **옛 세이브에는 없다** → «없으면 기본값»(빈 문자열 = 기본 노랑)이 지켜져야 한다(<see cref="SaveData.GiftDay"/> 와 같은 규칙).
    /// </summary>
    public class ProfileSaveTests
    {
        [Test]
        public void ProfileColorSurvivesJsonRoundTrip()
        {
            var d = TestData.Load();
            var s = new SaveData { Gold = 10, Gem = 5, ProfileColor = "blue" };
            var back = SaveData.FromJson(s.ToJson(), d);
            Assert.That(back.ProfileColor, Is.EqualTo("blue"), "고른 색이 저장·복원된다");
        }

        [Test]
        public void OldSaveWithoutProfileColorFallsBackToDefault()
        {
            var d = TestData.Load();
            var old = SaveData.FromJson("{\"v\":2,\"gold\":10,\"gem\":5,\"maxChapter\":3,\"selChapter\":1}", d);
            Assert.That(old.ProfileColor, Is.EqualTo(""), "옛 세이브에는 없다 → 빈 값(= 기본 노랑)");
            Assert.That(old.Gold, Is.EqualTo(10), "옛 세이브의 다른 값은 그대로");
        }
    }
}
