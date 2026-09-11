using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>T462 — 패스 레벨은 «깬 챕터 수»(<see cref="SaveData.MaxChapter"/> − 1)다. 갓 시작한 세이브(MaxChapter 1)는 0 레벨 = 받을 것 없음.</summary>
    public class PassLevelFromChapterTests
    {
        static PassData Load() => PassData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "pass.json"))));

        [Test]
        public void 깬_챕터_수가_곧_패스_레벨이다()
        {
            var d = Load();
            Assert.AreEqual(0, Pass.Lv(new SaveData { MaxChapter = 1 }, d), "아무것도 안 깼으면 0(T462 · 갓 시작한 세이브에 공짜 없음)");
            Assert.AreEqual(1, Pass.Lv(new SaveData { MaxChapter = 2 }, d), "1챕터 깸 = 1레벨");
            Assert.AreEqual(37, Pass.Lv(new SaveData { MaxChapter = 38 }, d), "37개 깸 = 37레벨");
            Assert.AreEqual(d.MaxLevel, Pass.Lv(new SaveData { MaxChapter = 500 }, d), "표 상한에서 자른다");
        }

        [Test]
        public void 옛_PassLv_칸은_더_이상_안_본다()
        {
            var d = Load();
            var s = new SaveData { MaxChapter = 1, PassLv = 32 };
            Assert.AreEqual(0, Pass.Lv(s, d), "PassLv 32 여도 챕터를 안 깼으면 0(T462 · 옛 «32» 가 다시 공짜를 주지 않는다)");
            Assert.IsFalse(Pass.AnyClaimable(s, d), "받을 것도 없다");
        }
    }
}
