using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T258 4항 — 퀘스트 팝업(15)의 <b>«업적» 탭</b>. 규칙은 <c>Core/Achievement</c> 가 갖고 EditMode 가 이미 잰다(<c>AchievementTests</c>) —
    /// 여기서 재는 것은 <b>화면이 그 규칙을 그대로 그리는가</b> 뿐이다.
    /// <list type="bullet">
    /// <item>줄 수·글자·목표가 <b>표에서</b> 온다(수를 안 적는다 — 주인이 표를 고치면 화면이 따라가야 하고 자는 안 깨져야 한다).</item>
    /// <item><b>메달 트랙·새로고침 줄이 없다</b>(주인 «메달 없음» · 누적은 초기화되지 않아 «새로고침까지» 가 거짓말이 된다).</item>
    /// <item>«받기» 는 <b>깬 단계가 있을 때만</b> 눌린다 — 눌리는데 아무 일도 안 나는 자리를 안 만든다(결정 771).</item>
    /// <item>받으면 <b>한 단계만</b> 들어오고(주인 «순차») 누적은 안 줄며 다음 목표가 «첫 목표 × 2» 로 바뀐다.</item>
    /// </list>
    /// </summary>
    public class AchievementTabTests
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
            _app = null;
            yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static int CountNamed(Transform t, string head)
        {
            int n = 0;
            foreach (var tr in t.GetComponentsInChildren<Transform>(true))
                if (tr.name.StartsWith(head) && tr.gameObject.activeInHierarchy) n++;
            return n;
        }
        static string BarText(Transform row)
        {
            var sl = row.GetComponentInChildren<Slider>(true); if (sl == null) return null;
            var t = sl.GetComponentInChildren<TMP_Text>(true); return t != null ? t.text : null;
        }

        [UnityTest]
        public IEnumerator 업적탭은_표를_그리고_트랙이_없다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Achievement : null;
            Assert.IsNotNull(d, "업적 표(achievement.json)가 실려야 한다");

            LobbyPopups.Achievements(_app);
            yield return Frames(1);
            var root = _app.Overlay.Root;

            Assert.AreEqual(d.List.Count, CountNamed(root, "Ach:"), "줄 수 = 표의 줄 수(수를 안 적는다)");
            Assert.AreEqual(0, CountNamed(root, "Quest:"), "업적 탭에는 퀘스트 줄이 없다");
            Assert.IsNull(UiKit.Find(root, "TrackBox"), "업적에는 메달 트랙이 없다(주인 «메달 없음»)");
            Assert.IsNull(UiKit.Find(root, "Refresh"), "업적 누적은 초기화되지 않으니 «새로고침까지» 도 없다");
            Assert.AreEqual(3, CountNamed(root, "Tab:"), "탭은 셋 그대로");

            var row0 = UiKit.Find(root, "Ach:0"); Assert.IsNotNull(row0, "첫 줄");
            var title = UiKit.Find(row0, "Title"); Assert.IsNotNull(title, "줄 제목");
            Assert.AreEqual(d.List[0].Label, title.GetComponent<TMP_Text>().text, "제목은 표의 글자 그대로(주인이 쓴 말)");
            Assert.AreEqual("0/" + d.List[0].Goal, BarText(row0), "새 세이브의 진행도 = «0/첫 목표»");

            // 새 세이브에서는 깬 단계가 없으니 «받기» 가 하나도 안 눌린다.
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                if (b.name == "AchBtn") Assert.IsFalse(b.interactable, "깬 단계가 없으면 «받기» 는 안 눌린다");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 깨면_받기가_살아나고_한_단계씩_들어온다()
        {
            yield return Boot();
            var d = _app.Data.Achievement;
            var row = d.List[0];                                  // 어느 줄이든 규칙은 같다 — 표의 첫 줄로 잰다
            Achievement.Add(_app.Save, row.Counter, row.Goal * 2);  // 두 단계치를 한꺼번에 쌓아 둔다(주인 «이미 20회 뽑았고» 의 그 자리)
            _app.Persist();

            LobbyPopups.Achievements(_app);
            yield return Frames(1);
            var root = _app.Overlay.Root;
            var btn = UiKit.Find(UiKit.Find(root, "Ach:0"), "AchBtn").GetComponent<Button>();
            Assert.IsTrue(btn.interactable, "깬 단계가 있으면 «받기» 가 산다");

            double gemBefore = _app.Save.Gem;
            int haveBefore = Achievement.Count(_app.Save, row.Counter);
            btn.onClick.Invoke();
            yield return Frames(1);

            Assert.AreEqual(gemBefore + row.Amount, _app.Save.Gem, 1e-6, "한 단계 몫만 들어온다(주인 «순차»)");
            Assert.AreEqual(haveBefore, Achievement.Count(_app.Save, row.Counter), "받아도 누적은 안 준다(«평생 기록»)");
            Assert.AreEqual(1, Achievement.Claimed(_app.Save, row.Counter), "받은 단계만 하나 오른다");
            Assert.IsTrue(Achievement.CanClaim(_app.Save, d, row.Counter), "두 단계치를 쌓았으니 아직 한 번 더 받을 수 있다");

            yield return Shutdown();
        }
    }
}
