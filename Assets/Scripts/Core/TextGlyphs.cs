using System.Text;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 글꼴에 없는 글자를 있는 글자로 바꾼다(T63-toast · 주인 «안 읽힌다»).
    /// <para>
    /// 우리 UI 글꼴은 <c>Assets/Fonts/Jua-Regular.ttf</c> 하나뿐이고 <c>fallbackFontReferences</c> 가 비어 있다. Jua 의 cmap 은 ASCII + 한글(2,519자)뿐이라
    /// 가운뎃점 <c>·</c>(U+00B7) · 곱하기 <c>×</c> · 줄표 <c>—</c> · 화살표 <c>→</c> · 이모지(🔨 💎 …) 에 글리프가 없다. 유니티는 없는 글자를 <b>폭 0</b> 으로 흘리므로
    /// «같은 부위·종류·등급만» 이 «같은 부위종류등급만» 으로 붙어 나온다(빈칸조차 안 남는다). T63-shop 이 상점 «💎» 에서 같은 것을 PNG 로 확인했다(결정 142).
    /// </para>
    /// 그래서 화면에 나가기 전에 한 번 거른다 — 바꿀 수 있는 것은 있는 글자로, 장식뿐인 이모지는 지우고, 그 자리에 남는 겹빈칸을 정리한다.
    /// 순수 C# 이라 dotnet 테스트(<c>TextGlyphsTests</c>)가 표를 그대로 검증한다.
    /// </summary>
    public static class TextGlyphs
    {
        /// <summary>Jua 에 없어서 폭 0 으로 사라지는 글자 → 대신 쓸 글자. 빈 문자열이면 지운다.</summary>
        public static readonly (char From, string To)[] Table =
        {
            ('·', "/"),      // 가운뎃점 — 열거·구분자. «부위·종류·등급» → «부위/종류/등급»
            ('•', "/"),
            ('×', "x"),      // «광고 보상 ×2» → «광고 보상 x2»
            ('—', "-"),      // 줄표
            ('–', "-"),
            ('→', ">"),      // «장비 137 → 101» → «장비 137 > 101»
            ('←', "<"),
            ('…', "..."),
            ('«', "\""),
            ('»', "\""),
            ('≠', "!="),
            ('≤', "<="),
            ('≥', ">="),
        };

        /// <summary>표에 없지만 글리프도 없는 장식 문자(이모지) — 지운다. 뜻을 나르는 기호(₩ 등)는 여기 넣지 않는다(문구를 고쳐야 한다).</summary>
        static bool IsDecoration(char c)
        {
            // 서로게이트(🔨 💎 같은 이모지 대부분) · 기타 기호/도형 블록 · 이모지 변이 선택자
            if (char.IsSurrogate(c)) return true;
            return (c >= '\u2600' && c <= '\u27BF')     // 기타 기호 · 딩뱃(❤ 🛡 ⚠ ⛔ …)
                || (c >= '\u2B00' && c <= '\u2BFF')     // 기타 기호·화살표 B
                || c == '\uFE0F';                     // VARIATION SELECTOR-16
        }

        /// <summary>
        /// 화면에 나갈 한 줄을 글꼴이 그릴 수 있는 글자로 바꾼다 — 표대로 치환 · 장식 이모지 제거 · 그 자리에 남은 겹빈칸과 앞뒤 빈칸 정리.
        /// 줄바꿈(<c>\n</c>)은 그대로 둔다.
        /// </summary>
        public static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                string rep = null;
                for (int i = 0; i < Table.Length; i++) if (Table[i].From == c) { rep = Table[i].To; break; }
                if (rep != null) { sb.Append(rep); continue; }
                if (IsDecoration(c)) continue;
                sb.Append(c);
            }
            return Squeeze(sb.ToString());
        }

        /// <summary>지운 자리에 남는 겹빈칸을 하나로, 줄 앞뒤 빈칸은 없앤다(줄 단위).</summary>
        static string Squeeze(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool sp = false, lineStart = true;
            foreach (char c in s)
            {
                if (c == '\n')
                {
                    while (sb.Length > 0 && sb[sb.Length - 1] == ' ') sb.Length--;
                    sb.Append('\n'); sp = false; lineStart = true; continue;
                }
                if (c == ' ')
                {
                    if (!sp && !lineStart) { sb.Append(' '); sp = true; }
                    continue;
                }
                sb.Append(c); sp = false; lineStart = false;
            }
            while (sb.Length > 0 && sb[sb.Length - 1] == ' ') sb.Length--;
            return sb.ToString();
        }
    }
}
