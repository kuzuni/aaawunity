using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T63-toast — 글꼴에 없는 글자 거르기(<see cref="TextGlyphs"/>) 와 토스트 칸 세로(<see cref="Layout.Toast"/>).
    /// <para>
    /// 사실: <c>Assets/Fonts/Jua-Regular.ttf</c> 의 cmap 은 U+0020~U+D79D 중 2,519자(ASCII + 한글) 뿐이고 <c>fallbackFontReferences</c> 가 비어 있다.
    /// 가운뎃점 «·»(U+00B7) 은 없다 — Latin-1 보충 블록에서 이 글꼴이 가진 것은 U+00A0 하나다. 유니티는 없는 글자를 폭 0 으로 흘리므로 글자가 붙어 나온다
    /// (T63-shop 이 상점 «💎» 에서 PNG 로 같은 것을 확인 · 결정 142).
    /// </para>
    /// </summary>
    public class TextGlyphsTests
    {
        [Test]
        public void MiddleDotBecomesSlash()
        {
            // 대장간 재료 안내 토스트 — 전에는 «같은 부위종류등급만 재료가 됩니다» 로 붙어 나왔다
            Assert.AreEqual("같은 부위/종류/등급만 재료가 됩니다", TextGlyphs.Safe("같은 부위·종류·등급만 재료가 됩니다"));
            Assert.AreEqual("(목걸이 / 체력실드 목걸이 / 신화)", TextGlyphs.Safe("(목걸이 · 체력실드 목걸이 · 신화)"));
        }

        [Test]
        public void OtherMissingSignsGetAsciiStandIns()
        {
            Assert.AreEqual("광고 보상 x2 / +12,345 G", TextGlyphs.Safe("광고 보상 ×2 · +12,345 G"));
            Assert.AreEqual("오늘 무료 보급은 받았습니다 - 내일 다시", TextGlyphs.Safe("오늘 무료 보급은 받았습니다 — 내일 다시"));
            Assert.AreEqual("12회 합성 (장비 137 > 101)", TextGlyphs.Safe("🔨 12회 합성 (장비 137 → 101)"));
        }

        [Test]
        public void DecorationEmojiAreDroppedWithTheirLeftoverSpace()
        {
            // 지운 자리에 겹빈칸·앞빈칸이 남으면 안 된다
            Assert.AreEqual("신화 목걸이 +9 완성!", TextGlyphs.Safe("🔨 신화 목걸이 +9 완성!"));
            Assert.AreEqual("가 나", TextGlyphs.Safe("가 ❤ 나"));
            Assert.AreEqual("가", TextGlyphs.Safe("  가  🛡  "));
        }

        [Test]
        public void PlainKoreanAndLineBreaksSurvive()
        {
            Assert.AreEqual("골드가 부족합니다", TextGlyphs.Safe("골드가 부족합니다"));
            Assert.AreEqual("장비, 골드, 보석, 진행이 모두 사라집니다.\n되돌릴 수 없습니다.",
                            TextGlyphs.Safe("장비, 골드, 보석, 진행이 모두 사라집니다.\n되돌릴 수 없습니다."));
            Assert.AreEqual("", TextGlyphs.Safe(""));
            Assert.IsNull(TextGlyphs.Safe(null));
        }

        [Test]
        public void ToastBoxHoldsTwoBodyLines()
        {
            // 1080×2337 프레임 · 프리팹(ToastMessage_01) 은 글자 칸을 상자보다 세로 17.26px 작게 잡는다
            float boxPx = 2337f * Layout.Toast.H / 100f;
            float cellPx = boxPx - Layout.ToastTextInsetY;
            Assert.GreaterOrEqual(cellPx, TextSize.BoxHeight(TextSize.Body, 2),
                "가장 긴 토스트(대장간 재료 안내 · 합성 완료)가 본문 40 으로 두 줄이라 칸이 112px 이상이어야 한다 — 모자라면 bestFit 이 말없이 32 까지 줄인다");
            // 세로 중심은 전(y84 h5 → 86.5%)과 같게 둔다 — 하단 탭 바·특전 미리보기 줄과의 관계가 안 바뀐다
            Assert.AreEqual(86.5f, Layout.Toast.Y + Layout.Toast.H / 2f, 0.01f);
        }

        [Test]
        public void LineBoxRuleIsTheOneFromDecision141()
        {
            Assert.AreEqual(1.4f, TextSize.LineBox, 1e-6f);
            Assert.AreEqual(56f, TextSize.BoxHeight(TextSize.Body), 1e-6f);
            Assert.AreEqual(112f, TextSize.BoxHeight(TextSize.Body, 2), 1e-6f);
            Assert.AreEqual(56f, TextSize.BoxHeight(TextSize.Body, 0), 1e-6f);   // 0 줄은 1 줄로
        }
    }
}
