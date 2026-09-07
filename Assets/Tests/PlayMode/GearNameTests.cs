using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T161 — 장비 이름이 «<b>세트 별칭</b>의 <b>부위</b>» 꼴이고(주인 2026-09-07 «치명 관련 장비는 암살자의 장갑 이런 식으로»),
    /// <b>한 팝업 안에서 부위 이름이 어긋나지 않는다</b>(주인 «반지가 장갑으로 이름 되어 있더라»).
    /// <para>
    /// 어긋남의 뿌리는 «이름은 <c>gear.json</c> 의 <c>typeName</c> · 부위 pill 은 T88 덮어쓰기» 로 <b>두 곳</b>을 보던 것이었다.
    /// 그래서 이 자는 이름 «꼴» 만 보지 않고 <b>같은 화면에 뜬 두 글자가 서로 맞는지</b>를 잰다 — 그것이 주인이 본 결함이다.
    /// </para>
    /// </summary>
    public class GearNameTests
    {
        App _app; PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }
        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }

        /// <summary>팝업 층에 지금 떠 있는 글자들(빈 줄 제외).</summary>
        List<string> OverlayTexts()
        {
            var l = new List<string>();
            var root = _app.Overlay.Root;
            if (root == null) return l;
            foreach (var t in root.GetComponentsInChildren<Text>(false))
                if (t != null && t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text)) l.Add(t.text);
            return l;
        }

        [UnityTest]
        public IEnumerator EveryGearNameIsAliasPlusPartAndTheRingNeverReadsAsGlove()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;

            // ⓐ 이름 꼴 — 세트 × 부위 모든 조합(«암살자의 반지» · «전사의 투구» · «도둑의 신발»)
            var seen = new List<string>();
            foreach (var part in D.Gear.Parts)
                foreach (var type in D.Gear.Types[part])
                {
                    var g = new GearItem { Part = part, Type = type, Rar = 0, Plus = 0 };
                    string got = GearUi.Name(D, g);
                    string want = GearRole.SetDisplayName(D, D.Gear.SetOf(type)) + "의 " + GearUi.PartName(D, part);
                    Assert.AreEqual(want, got, "장비 이름은 «별칭의 부위» 꼴이다(T161) — " + type);
                    // gear.json 의 옛 이름(«치명 장갑»)이 그대로 새면 여기서 걸린다
                    if (D.Gear.TypeName.TryGetValue(type, out var old))
                        Assert.AreNotEqual(old, got, "옛 typeName 이 그대로 나온다(T161 이 안 걸린 자리) — " + type);
                    seen.Add(got);
                }
            Debug.Log("[T161] 이름 " + seen.Count + "개 · 보기: " + string.Join(" / ", seen.GetRange(0, Mathf.Min(6, seen.Count)).ToArray()));

            // ⓑ 세트 라벨도 같은 별칭을 쓴다(한 화면에 «암살자의 반지» 와 «치명 세트» 가 나란히 나오지 않게 · 결정 기록)
            {
                var g = new GearItem { Part = "glove", Type = D.Gear.Types["glove"][0], Rar = 0, Plus = 0 };
                StringAssert.StartsWith(GearRole.SetDisplayName(D, GearUi.Set(D, g)), GearUi.SetLabel(D, g), "세트 라벨도 별칭을 쓴다");
            }

            // ⓒ 주인이 본 그것 — «반지»(glove) 세부 팝업 안에서 이름과 부위 pill 이 서로 맞는가
            GearItem ring = null;
            foreach (var t in D.Gear.Types["glove"]) { ring = S.NewGear("glove", t, 0, 0); S.Inv.Add(ring); break; }
            Assert.IsNotNull(ring, "반지(glove) 장비 하나");
            _app.ShowScreen("gear"); yield return Frames(2);
            GearUi.OpenDetail(_app, ring, null); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "세부 팝업이 열린다");

            var ov = _app.Overlay.Root;
            string ringWord = GearUi.PartName(D, "glove");           // T88 덮어쓰기 = «반지»
            string rawWord = D.Gear.PartName["glove"];               // gear.json 정본 = «장갑»(불변)
            Assert.AreEqual("반지", ringWord, "T88 — glove 의 표시 이름은 «반지»");

            var nameT = UiKit.Find(ov, "Name");                      // 팝업 제목(DetailFrame 이 "Name" 으로 세운다)
            var pill2 = UiKit.Find(ov, "Pill2");                     // 오른쪽 pill = 부위 이름
            Assert.IsNotNull(nameT, "세부 팝업 제목(Name)");
            Assert.IsNotNull(pill2, "세부 팝업 부위 pill(Pill2)");
            var title = nameT.GetComponent<Text>();
            var partT = pill2.GetComponentInChildren<Text>(true);
            Assert.IsNotNull(title, "제목 글자");
            Assert.IsNotNull(partT, "부위 pill 글자");
            Debug.Log("[T161] 세부 팝업 — 제목 «" + title.text + "» · 부위 pill «" + partT.text + "» · 팝업 글자 "
                + string.Join(" | ", OverlayTexts().ToArray()));

            // 주인이 본 어긋남 그대로: 제목과 pill 이 같은 부위 이름을 써야 한다
            StringAssert.Contains(ringWord, partT.text, "부위 pill 은 «" + ringWord + "»");
            StringAssert.Contains(ringWord, title.text, "제목도 «" + ringWord + "» 여야 한다(주인 «반지가 장갑으로 이름 되어 있더라»)");
            Assert.IsFalse(title.text.Contains(rawWord), "제목에 정본 부위 이름 «" + rawWord + "» 이 새면 안 된다 — 지금 제목: " + title.text);
            StringAssert.StartsWith(GearRole.SetDisplayName(D, GearUi.Set(D, ring)), title.text, "제목은 세트 별칭으로 시작한다");

            _app.Overlay.Close(); yield return Frames(1);
            _log.AssertNoRed("T161 장비 이름");
            yield return Shutdown();
        }
    }
}
