using System.Text;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 플레이어 이름(닉네임) 규칙 — T96-profile 2단계(주인 2026-09-07 «<c>Social_Profile_Nickname</c> 이거 좀 써라 프리팹들»).
    /// <para>
    /// 길이 한도는 <b>주인이 지목한 프리팹에서 실측</b>했다: 글자 세는 칸이 <c>&lt;color=#fb5951&gt;0&lt;/color&gt;/12</c> 라 최대 12,
    /// 안내 글자가 «Enter at least 2 characters.» 라 최소 2 — 화면이 정한 것을 코드가 그대로 따른다(밸런스 수치가 아니다).
    /// </para>
    /// 기본 이름은 <b>지금 아레나 시상대에 서 있던 이름 그대로</b>(«꼬마기사») — 안 고치면 화면이 종전과 똑같다.
    /// 순수 C# 이라 dotnet 테스트(<c>NicknameTests</c>)가 규칙을 그대로 검증한다.
    /// </summary>
    public static class Nickname
    {
        /// <summary>최소 글자 수(프리팹 안내 글자 실측).</summary>
        public const int MinLen = 2;
        /// <summary>최대 글자 수(프리팹 글자 세는 칸 «/12» 실측).</summary>
        public const int MaxLen = 12;
        /// <summary>안 지었을 때 쓰는 이름 — 아레나 시상대가 쓰던 이름 그대로라 «안 고치면 화면 불변».</summary>
        public const string Default = "꼬마기사";

        /// <summary>
        /// 입력한 줄을 이름으로 쓸 수 있게 다듬는다 — 글꼴이 못 그리는 글자(<see cref="TextGlyphs.CanRender"/> · T75 «폭 0 으로 사라짐») 제거 ·
        /// 앞뒤·겹빈칸 정리 · <see cref="MaxLen"/> 로 자르기. 리치 텍스트로 읽히는 <c>&lt;</c>·<c>&gt;</c> 도 뺀다(이름으로 태그를 심지 못하게).
        /// </summary>
        public static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            bool space = false;
            foreach (char c in s)
            {
                char ch = c == '\t' || c == '\n' || c == '\r' ? ' ' : c;
                if (ch == ' ')
                {
                    if (sb.Length > 0) space = true;
                    continue;
                }
                if (ch == '<' || ch == '>') continue;
                if (!TextGlyphs.CanRender(ch)) continue;
                if (space && sb.Length < MaxLen) { sb.Append(' '); }
                space = false;
                if (sb.Length >= MaxLen) break;
                sb.Append(ch);
            }
            var outp = sb.ToString();
            return outp.Length > MaxLen ? outp.Substring(0, MaxLen) : outp;
        }

        /// <summary>다듬은 뒤 길이가 <see cref="MinLen"/>~<see cref="MaxLen"/> 이면 쓸 수 있다.</summary>
        public static bool IsValid(string s)
        {
            var c = Clean(s);
            return c.Length >= MinLen && c.Length <= MaxLen;
        }

        /// <summary>지금 이름 — 세이브에 없거나 규칙에 안 맞으면 <see cref="Default"/>.</summary>
        public static string Of(SaveData s)
        {
            string v = s != null ? s.Nick : null;
            if (string.IsNullOrEmpty(v)) return Default;
            var c = Clean(v);
            return c.Length >= MinLen ? c : Default;
        }

        /// <summary>이름 바꾸기 — 규칙에 맞으면 다듬어 저장하고 참. 아니면 세이브를 건드리지 않고 거짓.</summary>
        public static bool Set(SaveData s, string want)
        {
            if (s == null) return false;
            var c = Clean(want);
            if (c.Length < MinLen) return false;
            s.Nick = c;
            return true;
        }
    }
}
