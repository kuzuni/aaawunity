using System.Collections;
using System.Text.RegularExpressions;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T261 2단계 — <b>희귀 천장을 화면이 말하는가</b>(1단계가 넣은 동작의 «표기» 쪽).
    /// <para>
    /// 규칙은 EditMode <c>GearTests</c> 가 못 박는다(굴림·리셋·세이브 왕복). 여기서는 <b>상자 카드 pill</b> 하나만 본다 —
    /// ⓐ 희귀 상자 카드에 «희귀 확정까지 N회» 가 뜨고 ⓑ 그 N 이 <b>표 값 − 지금까지 센 수</b>이며 ⓒ 천장을 넘겨도 음수로 안 내려간다.
    /// </para>
    /// ⚠ <b>수를 안 박는다</b> — 자도 `box.PityRare` 를 읽는다(§1 «수치는 표로»). 표가 10 에서 바뀌면 이 자가 같이 움직인다.
    /// ⚠ 희귀 상자는 <b>작은 카드(pill 1개)</b>라 pill 이 한 줄뿐이다 — 신화·전설 천장이 0 이라 그 한 줄을 희귀가 갖는다
    ///   (`gacha.json` 실측: rare 는 pityMyth·pityLegend 둘 다 0). 그 셋이 다 켜진 상자가 생기면 pill 이 모자라 뒤가 잘리므로,
    ///   이 자가 «없다» 고 말하기 시작하면 그때는 표가 아니라 <see cref="ShopScreen"/> 의 pill 개수를 봐야 한다.
    /// </summary>
    public class ShopPityPillTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

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
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static readonly Regex Tags = new Regex("<[^>]*>");
        /// <summary>희귀 상자 카드의 pill 글자들 중 «희귀 확정까지» 로 시작하는 줄(태그를 뗀 것) — 없으면 null.</summary>
        string RarePillLine()
        {
            var content = UiKit.Find(_app.Current.Root, "Content");
            Assert.IsNotNull(content, "상점 Content");
            var card = UiKit.Find(content, "Box:" + GachaData.RareBoxKey);
            Assert.IsNotNull(card, "희귀 상자 카드(Box:" + GachaData.RareBoxKey + ")");
            foreach (var t in card.GetComponentsInChildren<TMP_Text>(true))
            {
                string s = Tags.Replace(t.text ?? "", "").Trim();
                if (s.StartsWith("희귀 확정까지")) return s;
            }
            return null;
        }
        static int NumberIn(string s)
        {
            var m = Regex.Match(s ?? "", @"-?\d+");
            Assert.IsTrue(m.Success, "줄에 수가 있어야 한다: " + s);
            return int.Parse(m.Value);
        }

        [UnityTest]
        public IEnumerator RareBoxCardSaysHowManyPullsUntilTheGuaranteedRare()
        {
            yield return Boot();
            var box = _app.Data.Gacha.Box(GachaData.RareBoxKey);
            Assert.IsNotNull(box, "희귀 상자가 표에 있다");
            Assert.Greater(box.PityRare, 0, "1단계가 얹은 희귀 천장이 살아 있다(GachaData.From)");

            _app.ShowScreen("shop"); yield return Frames(2);
            string line = RarePillLine();
            Assert.IsNotNull(line, "희귀 상자 카드에 «희귀 확정까지 N회» pill 이 뜬다");
            Assert.AreEqual(box.PityRare, NumberIn(line), "새 세이브에서는 천장 값 그대로다 — 수는 표에서 온다(" + line + ")");
            _log.AssertNoRed("상점(희귀 천장 표기)");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator TheNumberCountsDownAndNeverGoesBelowZero()
        {
            yield return Boot();
            var box = _app.Data.Gacha.Box(GachaData.RareBoxKey);
            var S = _app.Save;

            // 셋을 센 판 — 남은 수는 «천장 − 3» 이어야 한다(자가 뺄셈을 하지 않고 화면이 한 자리에서 한다).
            S.GachaBoxes[GachaData.RareBoxKey] = new GachaState { PRare = 3 };
            _app.ShowScreen("shop"); yield return Frames(2);
            Assert.AreEqual(box.PityRare - 3, NumberIn(RarePillLine()), "센 만큼 줄어든다");

            // 천장보다 많이 센 판(굴림이 리셋하기 전 한 프레임에 있을 수 있는 자리) — 음수 대신 0 이다.
            S.GachaBoxes[GachaData.RareBoxKey] = new GachaState { PRare = box.PityRare + 5 };
            _app.ShowScreen("shop"); yield return Frames(2);
            Assert.AreEqual(0, NumberIn(RarePillLine()), "천장을 넘겨도 «-5회» 를 보여 주지 않는다");

            _log.AssertNoRed("상점(희귀 천장 셈)");
            yield return Shutdown();
        }
    }
}
