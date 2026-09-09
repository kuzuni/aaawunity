using System.Collections;
using System.IO;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T316 3항 — <b>표가 말하는 색 이름을 팔레트가 전부 아는가.</b>
    /// <para>
    /// <c>Palette.ByName</c> 의 마지막 줄은 <c>default: return Gray</c> 다 — 표에 «pink» 라고 적고 팔레트가 그 낱말을 모르면
    /// <b>빨간 줄도 예외도 없이 회색으로 그려진다.</b> «초월» 이 회색으로 뜨는 것을 잡아 줄 것은 사람 눈뿐이고,
    /// 사람은 회색을 보고 «아직 안 만들었나 보다» 로 읽는다(결정 851 과 같은 갈래 — 떨어지는 갈래는 고장을 조용하게 만든다).
    /// </para>
    /// 그래서 <b>표와 팔레트를 맞대어</b> 잰다 — 표에 등급을 더하는 사람은 색도 같이 더하게 된다.
    /// <para>⚠ 색 <b>값</b>(hex)은 안 베낀다 — 주인이 값을 바꾸면 <c>catalog.json</c> 한 줄이고 이 자는 그대로여야 한다.</para>
    /// </summary>
    public class GearTierColorTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }

        [UnityTest]
        public IEnumerator EveryTierColorTheTableNamesIsKnownToThePalette()
        {
            yield return Boot();
            var d = _app.Data.GearTier;
            Assert.IsNotNull(d, "표시 등급 표(data.gearTier)가 실려 있어야 한다 — 카탈로그에 없으면 등급이 통째로 안 뜬다");

            var gray = Palette.ByName("그런 색 없음");   // 떨어지는 갈래가 무엇을 돌려주는지 여기서 얻는다(값을 안 베낀다)
            foreach (var t in d.Tiers)
            {
                if (t.Color == GearTier.GradientColor)
                {
                    // 무한 — 한 색이 아니라 두 색이다. 팔레트가 그 이름을 모르면 Of 가 «없는 쌍» 을 돌려주고 칸이 검게 뜬다.
                    var pair = GradientPalette.Of("tierInfinite");
                    Assert.AreNotEqual(pair.Top, pair.Bottom, $"{t.Name} 은 그라데이션이라 위·아래 색이 달라야 한다");
                    Assert.That(System.Array.IndexOf(GradientPalette.Names, "tierInfinite"), Is.GreaterThanOrEqualTo(0),
                        "tierInfinite 이 GradientPalette.Names 에 있어야 한다(그 목록을 훑는 자·화면이 있다)");
                    continue;
                }
                var c = Palette.ByName(t.Color);
                Assert.AreNotEqual(gray, c,
                    $"«{t.Name}» 의 색 이름 «{t.Color}» 을 팔레트가 모른다 — 그러면 그 등급이 **회색으로 조용히** 뜬다(Palette.ByName 에 가지를 더해라)");
            }

            // 등급마다 색이 서로 달라야 «무엇인지» 를 색으로 읽을 수 있다(같은 색 둘이면 한쪽은 색이 없는 것과 같다).
            for (int i = 0; i < d.Tiers.Count; i++)
                for (int j = i + 1; j < d.Tiers.Count; j++)
                {
                    if (d.Tiers[i].Color == GearTier.GradientColor || d.Tiers[j].Color == GearTier.GradientColor) continue;
                    Assert.AreNotEqual(Palette.ByName(d.Tiers[i].Color), Palette.ByName(d.Tiers[j].Color),
                        $"«{d.Tiers[i].Name}» 과 «{d.Tiers[j].Name}» 이 같은 색이다");
                }

            // 신화(지금 색)와도 달라야 한다 — 안 그러면 «신화» 와 «갓» 이 같은 색으로 나란히 선다.
            foreach (var t in d.Tiers)
            {
                if (t.Color == GearTier.GradientColor) continue;
                Assert.AreNotEqual(Palette.ByName("plum"), Palette.ByName(t.Color), $"«{t.Name}» 이 신화와 같은 색이다");
            }

            _log.AssertNoRed("표시 등급 색");
            yield return Shutdown();
        }
    }
}
