using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T225 — 합친 배경 한 겹(<c>KkomaKnight/UiMergedBg</c>)의 <b>이음매</b>를 잰다.
    /// <para>
    /// ⚑ 왜 이 자가 필요한가: 워커도 CI 도 <b>셰이더 속성 이름이 틀린 것을 못 잡는다</b>.
    /// <c>Material.SetColor("_TopColorr", …)</c> 는 컴파일도 되고 유니티도 경고 하나 없이 <b>그냥 아무 일도 안 한다</b> —
    /// 화면에서 «그라데이션이 사라졌다» 로만 나타나는데, 판정이 «PNG 37장 차 0» 이라 그때는 이미 원인이 셋(식·유니티·이름) 으로 갈린다.
    /// 이름은 두 파일(<c>UiKit.cs</c> ↔ <c>UiMergedBg.shader</c>)에 나뉘어 있고 <b>둘을 잇는 자가 여기 말고는 없다</b>.
    /// </para>
    /// 셰이더 자체(컴파일·그림)는 이 자가 못 잰다 — 그것은 유니티 잡이 한다.
    /// </summary>
    public class MergedBgShaderTests
    {
        static string Repo()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "docs", "ROUTINE.md"))) return dir.FullName;
            throw new FileNotFoundException("레포 뿌리를 못 찾았다 (cwd=" + Directory.GetCurrentDirectory() + ")");
        }
        static string Read(params string[] parts) => File.ReadAllText(Path.Combine(Repo(), Path.Combine(parts)));

        const string ShaderRel = "Assets/KkomaKnight/Shaders/UiMergedBg.shader";
        const string MatRel = "Assets/KkomaKnight/Shaders/UiMergedBg.mat";

        /// <summary>셰이더의 Properties 블록이 이름 짓는 속성들(«_X (…»).</summary>
        static HashSet<string> ShaderProperties()
        {
            var src = Read(ShaderRel.Split('/'));
            int a = src.IndexOf("\n    Properties");   // ⚠ 머리 주석에도 «SubShader» 같은 낱말이 나온다 — 블록은 «줄 첫머리 + 들여쓰기» 로 찾는다
            Assert.Greater(a, 0, "셰이더에 Properties 블록이 없다");
            int b = src.IndexOf("\n    SubShader", a);
            var set = new HashSet<string>();
            foreach (Match m in Regex.Matches(src.Substring(a, b - a), @"(_\w+)\s*\("))
                set.Add(m.Groups[1].Value);
            return set;
        }

        /// <summary>UiKit 이 <c>Shader.PropertyToID</c> 로 잡아 두는 이름이 전부 셰이더에 <b>실제로 있어야</b> 한다 — 없으면 그 SetXxx 는 조용히 아무 일도 안 한다.</summary>
        [Test]
        public void EveryNameUiKitSetsExistsInTheShader()
        {
            var props = ShaderProperties();
            var uikit = Read("Assets", "Scripts", "Game", "UiKit.cs");
            var names = new List<string>();
            foreach (Match m in Regex.Matches(uikit, @"Merged\w*Id\s*=\s*Shader\.PropertyToID\(""(_\w+)""\)"))
                names.Add(m.Groups[1].Value);

            Assert.GreaterOrEqual(names.Count, 9, "UiKit 에서 Merged…Id 를 하나도 못 읽었다 — 이 자의 정규식이 낡았다(이름 짓는 꼴이 바뀌었나?)");
            foreach (var n in names) Assert.IsTrue(props.Contains(n), "UiKit 이 세우는 «" + n + "» 가 셰이더 Properties 에 없다 — 그 SetXxx 는 조용히 아무 일도 안 한다");
            // RawImage 가 텍스처를 물리는 자리 — 이름이 _MainTex 여야 캔버스가 꽂아 준다(다른 이름이면 무늬가 통째로 안 나온다)
            Assert.IsTrue(props.Contains("_MainTex"), "RawImage 는 텍스처를 _MainTex 로 꽂는다 — 그 이름이 있어야 한다");
        }

        /// <summary>머티리얼이 <b>그</b> 셰이더를 가리켜야 한다(guid) — 어긋나면 유니티가 핑크로 그린다.</summary>
        [Test]
        public void TheMaterialPointsAtThatShader()
        {
            var guid = Regex.Match(Read((ShaderRel + ".meta").Split('/')), @"guid:\s*(\w+)").Groups[1].Value;
            Assert.IsNotEmpty(guid);
            var mat = Read(MatRel.Split('/'));
            Assert.IsTrue(Regex.IsMatch(mat, @"m_Shader:\s*\{fileID:\s*4800000,\s*guid:\s*" + guid),
                "머티리얼의 m_Shader 가 " + ShaderRel + " 의 guid(" + guid + ")를 안 가리킨다");
        }

        /// <summary>
        /// 그 머티리얼은 <b>카탈로그</b>를 통해서만 닿는다 — 등재가 빠지면 <see cref="Read"/> 로는 멀쩡한데 폰에서 배경이 사라진다.
        /// (자작 셰이더는 «어느 머티리얼도 안 쓰면» 빌드에서 통째로 빠진다 · <c>Shader.Find</c> 가 null 이 되는 그 길이다.)
        /// </summary>
        [Test]
        public void TheCatalogCarriesIt()
        {
            var cat = Read("Assets", "KkomaKnight", "catalog.json");
            StringAssert.Contains("\"mat.uiMergedBg\": \"" + MatRel + "\"", cat);
        }
    }
}
