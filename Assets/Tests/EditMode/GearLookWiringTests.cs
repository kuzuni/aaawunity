using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T325 문 ⓑ — <b>«등급» 을 «그림 칸» 자리에 그냥 넘기는 게임 코드가 없는가.</b>
    /// <para>
    /// <c>GearLook.PartKey(part, set, rar)</c> 의 <c>rar</c> 는 <b>등급이 아니라 그림 칸</b>이고, 범위 밖은 조용히 마지막 칸으로 눌린다.
    /// 오늘은 등급 넷·그림 넷이라 둘이 같은 수여서 <b>아무 차이도 안 난다</b> — 그래서 이 어긋남은 지금 어떤 자로도 안 잡히고,
    /// 주인이 «영웅 등급 다시 넣고» 라고 한 것이 들어가는 날에야 <b>신화가 전설 그림을 입는</b> 꼴로 나타난다.
    /// 그때도 컴파일은 되고 빨간 줄은 안 난다 — 사람이 폰에서 «어? 그림이 이상한데» 로 알아채는 것이 전부다.
    /// </para>
    /// 그래서 <b>지금</b> 못 박는다. 게임 코드가 이 함수를 부르는 길은 둘뿐이어야 한다 —
    /// ⓐ <c>GameData</c> 갈래(<c>PartKey(D, g)</c> · 그 안에서 <c>LookRar</c> 를 거친다) 또는
    /// ⓑ 인자에 <c>LookRar(</c> 가 눈에 보이게 든 갈래.
    /// <para>⚠ 자(<c>Assets/Tests</c>)는 안 센다 — 거기는 «그림 칸» 을 일부러 직접 다루는 자리다(<c>PartKey("weapon","crit",99)</c> 같은 경계 시험).</para>
    /// </summary>
    public class GearLookWiringTests
    {
        /// <summary>«GearLook.PartKey(» / «GearLook.IconKey(» 호출 한 건 — 여는 괄호부터 짝이 맞는 닫는 괄호까지를 인자로 본다.</summary>
        static readonly Regex Call = new Regex(@"GearLook\.(PartKey|IconKey)\s*\(", RegexOptions.Compiled);

        static string ArgsOf(string src, int openParen)
        {
            int depth = 0;
            for (int i = openParen; i < src.Length; i++)
            {
                if (src[i] == '(') depth++;
                else if (src[i] == ')') { depth--; if (depth == 0) return src.Substring(openParen + 1, i - openParen - 1); }
            }
            return src.Substring(openParen + 1);   // 짝이 안 맞으면(있을 수 없다) 남은 것을 준다 — 판정은 아래에서 한다
        }

        /// <summary>줄 주석·블록 주석을 지운다 — 주석 안의 보기(«PartKey(part, set, rar) 는 …»)를 호출로 세면 거짓 빨강이 난다.</summary>
        static string StripComments(string src)
        {
            src = Regex.Replace(src, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return Regex.Replace(src, @"//[^\n]*", "");
        }

        [Test]
        public void NoGameCodePassesARawGradeWhereASpriteSlotIsExpected()
        {
            var scripts = TestData.RepoFile("Assets/Scripts");
            Assert.IsTrue(Directory.Exists(scripts), "Assets/Scripts 를 찾아야 한다");

            var bad = new List<string>();
            int seen = 0;
            foreach (var path in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                var rel = path.Substring(path.IndexOf("Assets", System.StringComparison.Ordinal)).Replace('\\', '/');
                if (rel.EndsWith("Core/GearLook.cs")) continue;   // 정의한 파일 자신 — 여기서 «그림 칸» 을 다루는 것이 제 일이다
                var src = StripComments(File.ReadAllText(path));
                foreach (Match m in Call.Matches(src))
                {
                    seen++;
                    var args = ArgsOf(src, m.Index + m.Length - 1);
                    // «GameData 갈래» 인지는 **인자 개수**로 가른다 — 그 갈래는 (GameData, GearItem) 둘뿐이고,
                    // 이름(D·d·app.Data …)으로 가르면 부르는 쪽이 변수 이름을 바꾸는 날 조용히 안 세어진다.
                    int commas = 0, depth = 0;
                    foreach (var ch in args) { if (ch == '(') depth++; else if (ch == ')') depth--; else if (ch == ',' && depth == 0) commas++; }
                    bool twoArgs = commas == 1;
                    bool hasLookRar = args.Contains("LookRar(");
                    if (twoArgs || hasLookRar) continue;
                    bad.Add($"{rel} — GearLook.{m.Groups[1].Value}({args.Trim()})");
                }
            }

            Assert.That(seen, Is.GreaterThan(0), "호출을 하나도 못 찾았다 — 이 자가 정규식만 고장 난 채 조용히 초록일 수 있다");
            Assert.That(bad, Is.Empty,
                "등급을 «그림 칸» 자리에 그대로 넘기는 게임 코드가 있다 — `D.Gear.LookRar(등급)` 을 거쳐라.\n" +
                "  (등급이 넷인 오늘은 아무 차이도 안 나고, 다섯이 되는 날 그림만 위로 한 칸씩 밀린다)\n  " + string.Join("\n  ", bad));
        }
    }
}
