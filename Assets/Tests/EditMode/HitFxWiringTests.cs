using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T504 — <b>«만드는 자가 옳은 것을 만드나» 와 «전투가 그 자를 부르나» 는 다른 물음이다.</b>
    /// <para>
    /// 주인 2026-09-12 «힛 이펙트가 너무 반짝임 · 너무 글로우임» → 옛 CFXR 프리팹(<c>fx.hit</c>·<c>fx.crit</c> · 가산 재질 + Point Light) 대신
    /// <c>Fx</c>(`Assets/Scripts/Game/Fx.cs`)의 알파 블렌드 알갱이 버스트(<c>HitBurst</c>)를 쓰기로 했다. 그런데 <c>EvKind.Hit</c>·<c>EvKind.PlayerHit</c> 만 옮기고
    /// <b><c>EvKind.Counter</c>(반격) 한 자리가 남아</b>, 적을 반격으로 때릴 때마다 주인이 말한 그 반짝임이 **한 글자도 안 바뀐 채** 떴다(검수 Q 실측 · <c>f2756fbb</c>).
    /// </para>
    /// <para>
    /// ⚑ <b>그때 세워 둔 자(<c>HitBurstTests</c>)는 이것을 원리적으로 못 본다</b> — 그 자는 <c>Fx.HitBurst(…)</c> 를 <b>직접 불러</b> 산물을 잰다(가산 아님 · 라이트 0 · 정렬).
    /// 좋은 단언이고 다 옳지만, 그 물음은 «만드는 자» 에 대한 것이라 <b>부르는 자리가 옛것을 부르는 한 영원히 초록</b>이다(T491 이 남긴 그 꼴 — 표는 지키고 실리는 자리는 안 지킨다).
    /// </para>
    /// <para>⚠ 재는 것은 <b>게임 코드</b>(<c>Assets/Scripts</c>)뿐이다 — 카탈로그·프리팹에 그 키가 남아 있는 것은 이 자의 일이 아니다(지우는 일은 따로다).</para>
    /// </summary>
    public class HitFxWiringTests
    {
        /// <summary>옛 피격 프리팹 키 — 이 글자가 게임 코드에 있으면 그 자리는 아직 가산 프리팹을 띄운다.</summary>
        static readonly Regex OldKey = new Regex("\"fx\\.(hit|crit)\"", RegexOptions.Compiled);

        /// <summary>줄 주석·블록 주석을 지운다 — 주석 안의 인용(«fx.hit 대신 …»)을 호출로 세면 거짓 빨강이 난다(`GearLookWiringTests` 와 같은 손).</summary>
        static string StripComments(string src)
        {
            src = Regex.Replace(src, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return Regex.Replace(src, @"//[^\n]*", "");
        }

        [Test]
        public void NoGameCodeStillSpawnsTheOldAdditiveHitPrefab()
        {
            var scripts = TestData.RepoFile("Assets/Scripts");
            Assert.IsTrue(Directory.Exists(scripts), "Assets/Scripts 를 찾아야 한다");

            var bad = new List<string>();
            int files = 0, burst = 0;
            foreach (var path in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                files++;
                var rel = path.Substring(path.IndexOf("Assets", System.StringComparison.Ordinal)).Replace('\\', '/');
                var src = StripComments(File.ReadAllText(path));
                burst += Regex.Matches(src, @"Fx\.HitBurst\s*\(").Count;
                foreach (Match m in OldKey.Matches(src))
                {
                    int line = 1; for (int i = 0; i < m.Index && i < src.Length; i++) if (src[i] == '\n') line++;
                    bad.Add(rel + " — " + m.Value + " (주석 지운 뒤 " + line + "째 줄)");
                }
            }

            // ⚑ 공허 방지 — 이 자가 «0건» 이라고 말하려면 **실제로 읽었어야** 한다.
            //   길이 틀렸거나 정규식이 고장 나도 결과는 똑같이 «0건» 이라, 그 둘을 가르는 줄이 없으면 이 자는 조용한 거짓이 된다
            //   (T455 2회차가 «가드가 스스로 공허했다» 로 치른 값 · 결정 1379).
            Assert.That(files, Is.GreaterThan(50), "게임 코드 파일을 " + files + "개밖에 못 읽었다 — 길이 틀렸다(이 자의 «0건» 은 못 믿는다)");
            Assert.That(burst, Is.GreaterThanOrEqualTo(3),
                "`Fx.HitBurst(` 호출을 " + burst + "건 찾았다 — 셋(Hit · PlayerHit · Counter)보다 적으면 **옮기다 만 것**이거나 이 자가 소스를 못 읽는 것이다");

            Assert.That(bad, Is.Empty,
                "게임 코드가 아직 옛 CFXR 가산 프리팹을 띄운다(주인 «너무 반짝임 · 너무 글로우» · T504) — `Fx.HitBurst(...)` 로 옮겨라.\n" +
                "  치명타 갈래가 있는 자리는 `EvKind.Hit` 과 같은 손으로 맞춘다: `ev.Crit ? Palette.PopCrit : Fx.HitGrain` · `ev.Crit ? 1.4f : 1f` · `ev.Crit ? 16 : 10`\n  "
                + string.Join("\n  ", bad));
        }
    }
}
