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
    /// T318 — 퀘스트 «이동» 의 <b>화면</b> 쪽. 표·규칙은 EditMode <c>QuestGoTests</c> 가 못 박고 여기서는
    /// ⓐ 표의 <b>모든 목적지</b>가 실제로 열리고 손가락이 <b>그 이름의 켜진 버튼</b> 자식으로 선다(이름 계약을 표의 줄마다 밟는다 — 화면이 버튼 이름을 바꾸면 여기서 운다)
    /// ⓑ 그 버튼을 누르면 사라진다 ⓒ 표의 <c>lifeSec</c> 이 지나면 사라진다 ⓓ 화면을 벗어나면 사라진다 ⓔ 로그인류(<c>go = null</c>)는 아무 데도 안 간다 ⓕ 빨간 줄 0.
    /// <para>⚠ «이동» 버튼(<c>LobbyPopups.cs:586</c>)이 이 길잡이를 부르는 배선은 그 파일이 남의 lock 이라 다음 회차다 — 그때 T300 P7 각본에 «이동» 한 번이 붙는다.</para>
    /// </summary>
    public class QuestGoPlayTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

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
            QuestGo.Dismiss();
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>캔버스 전체에서 이름이 <paramref name="name"/> 이고 켜진 오브젝트 수(«한 번에 손가락 하나» 를 이걸로 센다).</summary>
        int CountActive(string name)
        {
            int n = 0;
            foreach (var t in _app.UiCanvas.GetComponentsInChildren<Transform>(true))
                if (t.name == name && t.gameObject.activeInHierarchy) n++;
            return n;
        }

        // 표의 화면 낱말 → 실제로 열려야 하는 GameScreen.Name (탐험은 로비 위 팝업)
        static readonly Dictionary<string, string> ScreenOf = new Dictionary<string, string>
        {
            { "lobby", "lobby" }, { "dungeon", "events" }, { "chest", "shop" }, { "forge", "forge" }, { "pet", "pet" }, { "expedition", "lobby" },
        };

        [UnityTest]
        public IEnumerator 표의_모든_목적지가_실제로_열리고_손가락이_그_버튼에_선다()
        {
            yield return Boot();
            var qt = _app.Data != null ? _app.Data.Quest : null;
            if (qt == null) { yield return Shutdown(); Assert.Ignore("퀘스트 표가 없다 — 잴 것이 없다"); }
            var seen = new HashSet<string>();
            var quests = new List<QuestData.Quest>(qt.Daily.Quests); quests.AddRange(qt.Weekly.Quests);
            int walked = 0;
            foreach (var q in quests)
            {
                if (q.Go == null) continue;
                string key = q.Go.Screen + "›" + string.Join("|", q.Go.Points);
                if (!seen.Add(key)) continue;   // 같은 목적지는 한 번만(일일·주간이 겹친다)
                var target = _app.Hint(q.Go);
                yield return Frames(2);
                Assert.IsNotNull(target, "«" + q.Label + "» 의 이동 — " + key + " 에서 손가락이 가리킬 버튼을 못 찾았다(이름 계약이 바뀌었나)");
                Assert.IsTrue(System.Array.IndexOf(q.Go.Points, target.name) >= 0, "«" + q.Label + "» — 찾은 버튼 이름 " + target.name);
                Assert.IsTrue(target.gameObject.activeInHierarchy, "«" + q.Label + "» — 가리킨 버튼은 켜져 있어야 한다");
                Assert.AreEqual(ScreenOf[q.Go.Screen], _app.Current.Name, "«" + q.Label + "» 의 이동이 연 화면");
                if (q.Go.Screen == "dungeon") Assert.AreEqual(EventsScreen.PageDungeon, _app.GetScreen<EventsScreen>().Page, "이벤트 화면은 던전 페이지");
                if (q.Go.Screen == "expedition") { Assert.IsTrue(_app.Overlay.IsOpen, "탐험 팝업이 열려야 한다"); Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ExpeditionBox"), "ExpeditionBox"); }
                var hint = UiKit.Find(target, QuestGo.HintName);
                Assert.IsNotNull(hint, "«" + q.Label + "» — 손가락(" + QuestGo.HintName + ")이 그 버튼의 자식이어야 한다");
                Assert.IsTrue(hint.gameObject.activeInHierarchy && hint.GetComponent<Image>() != null && hint.GetComponent<Image>().sprite != null, "«" + q.Label + "» — 손가락 그림(pi.hand)");
                Assert.IsNotNull(hint.GetComponent<HintFinger>(), "«" + q.Label + "» — 수명 컴포넌트");
                Assert.AreEqual(1, CountActive(QuestGo.HintName), "«" + q.Label + "» — 한 번에 손가락은 하나(앞 것은 치워진다)");
                Assert.AreSame(hint.GetComponent<HintFinger>(), QuestGo.Current, "Current 는 방금 세운 것");
                walked++;
            }
            Assert.Greater(walked, 0, "표에 go 가 있는 줄이 하나는 있어야 한다");
            _log.AssertNoRed("T318 이동 " + walked + "곳");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 가리킨_버튼을_누르면_손가락이_사라진다()
        {
            yield return Boot();
            if (_app.Data == null || _app.Data.Quest == null) { yield return Shutdown(); Assert.Ignore("퀘스트 표가 없다"); }
            // 펫 «전체 강화» — 누르면 아무 일도 안 하는 버튼이라(껍데기) 판을 벌이지 않고 «눌렀다» 만 낸다.
            var target = _app.Hint(new QuestData.Go { Screen = "pet", Points = new[] { "UpgradeAllBtn" } });
            yield return Frames(1);
            Assert.IsNotNull(target); Assert.AreEqual(1, CountActive(QuestGo.HintName));
            var b = target.GetComponent<Button>(); Assert.IsNotNull(b, "가리킨 것은 버튼");
            b.onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual(0, CountActive(QuestGo.HintName), "누르면 손가락이 사라진다");
            Assert.IsNull(QuestGo.Current, "Current 도 비운다");
            var glow = UiKit.Find(target.parent, QuestGo.GlowName);
            Assert.IsTrue(glow == null || !glow.gameObject.activeInHierarchy, "빛 테두리도 같이 치운다");
            _log.AssertNoRed("T318 누름");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 표의_lifeSec_이_지나면_스스로_사라진다()
        {
            yield return Boot();
            if (_app.Data == null || _app.Data.Quest == null) { yield return Shutdown(); Assert.Ignore("퀘스트 표가 없다"); }
            _app.ShowScreen("pet"); yield return Frames(1);
            var target = QuestGo.FindActive(_app.Current.Root, new[] { "UpgradeAllBtn" }); Assert.IsNotNull(target);
            // 8초를 실제로 기다리는 자는 느린 자다 — 수명만 0.15초로 준다(값은 표 그대로 두고 인자만).
            var f = QuestGo.Hint(_app, target, 0.15f);
            Assert.IsNotNull(f); Assert.AreEqual(1, CountActive(QuestGo.HintName));
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 0.6f) yield return null;
            Assert.AreEqual(0, CountActive(QuestGo.HintName), "lifeSec 이 지나면 사라진다");
            Assert.IsNull(QuestGo.Current);
            // 표 값 그대로면 0.6초 뒤에도 살아 있다(«스스로 사라짐» 이 시간 때문이지 다른 이유가 아님을 가른다)
            var g = QuestGo.Hint(_app, target);
            t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 0.4f) yield return null;
            Assert.AreEqual(1, CountActive(QuestGo.HintName), "표의 lifeSec(" + _app.Data.Quest.Hint.LifeSec + "s) 안에는 살아 있다");
            Assert.Greater(g.Age, 0.3f);
            QuestGo.Dismiss(); yield return Frames(1);
            Assert.AreEqual(0, CountActive(QuestGo.HintName));
            _log.AssertNoRed("T318 수명");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 화면을_벗어나면_손가락이_사라진다()
        {
            yield return Boot();
            if (_app.Data == null || _app.Data.Quest == null) { yield return Shutdown(); Assert.Ignore("퀘스트 표가 없다"); }
            var target = _app.Hint(new QuestData.Go { Screen = "forge", Points = new[] { "FuseBtnOn", "FuseBtn" } });
            yield return Frames(1);
            Assert.IsNotNull(target); Assert.AreEqual("forge", _app.Current.Name);
            var forgeRoot = _app.Current.Root;
            _app.ShowScreen("lobby");
            yield return Frames(2);
            Assert.IsNull(QuestGo.Current, "화면을 벗어나면 손가락은 죽는다(OnDisable)");
            var stale = UiKit.Find(forgeRoot, QuestGo.HintName);
            Assert.IsTrue(stale == null, "대장간에 옛 손가락이 남아 다음에 켤 때 되살아나면 안 된다");
            _app.ShowScreen("forge"); yield return Frames(1);
            Assert.AreEqual(0, CountActive(QuestGo.HintName), "다시 들어와도 손가락은 없다");
            _log.AssertNoRed("T318 벗어남");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 갈_데_없는_줄은_아무_데도_안_가고_손가락도_없다()
        {
            yield return Boot();
            Assert.AreEqual("lobby", _app.Current.Name);
            var r = _app.Hint((QuestData.Go)null);
            yield return Frames(1);
            Assert.IsNull(r); Assert.AreEqual("lobby", _app.Current.Name); Assert.AreEqual(0, CountActive(QuestGo.HintName));
            // 이름을 모르는 버튼 — 화면은 그대로, 손가락 없음, 빨간 줄 없음(경고 한 줄뿐)
            var r2 = _app.Hint("이런버튼은없다");
            Assert.IsNull(r2); Assert.AreEqual(0, CountActive(QuestGo.HintName));
            _log.AssertNoRed("T318 null");
            yield return Shutdown();
        }
    }
}
