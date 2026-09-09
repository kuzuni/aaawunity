using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T366 — 로비 왼쪽 사이드 «특권» 칸의 빨간 점(<c>PrivDot</c>)이 판정(<see cref="Privilege.AnyClaimable"/>)을 따른다.
    /// 새 세이브 = 공짜 «데일리 기프트» 카드를 오늘 받을 수 있으므로 <b>켜져</b> 있고, 오늘 것을 다 받고 로비를 다시 그리면 <b>꺼진다</b>.
    /// (카드 «받기» 버튼의 점은 <c>LobbyPopups.PrivilegeScreen</c> 몫 — 그 파일이 열리는 회차에 같은 판정으로 붙는다.)
    /// </summary>
    public class PrivilegeDotPlayTests
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
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator 로비_특권_칸_점은_받을_것이_있을_때만_켜진다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Privilege : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("특권 표가 없다 — 잴 것이 없다"); }
            _app.ShowScreen("lobby"); yield return Frames(1);
            var root = _app.Current.Root;
            var cell = UiKit.Find(root, "Side:" + LobbyScreen.SidePrivilege);
            Assert.IsNotNull(cell, "로비 왼쪽 사이드 «특권» 칸");
            var dot = UiKit.Find(cell, "PrivDot");
            Assert.IsNotNull(dot, "«특권» 칸의 빨간 점(PrivDot)이 그 칸의 자식이어야 한다");
            string today = SaveStore.Today();
            Assert.IsTrue(Privilege.AnyClaimable(_app.Save, d, today), "새 세이브는 공짜 카드를 오늘 받을 수 있다(전제)");
            Assert.IsTrue(dot.gameObject.activeInHierarchy, "받을 것이 있으면 점이 켜진다");

            // 오늘 받을 수 있는 카드를 전부 받는다(공짜 카드 하나) → 조건이 사라진다 → 로비를 다시 그리면 꺼진다(T167)
            foreach (var c in d.Cards) if (Privilege.Can(_app.Save, d, c, today)) Privilege.Claim(_app.Save, d, c, today);
            Assert.IsFalse(Privilege.AnyClaimable(_app.Save, d, today), "다 받았으면 판정이 거짓(전제)");
            _app.Current.Refresh(); yield return Frames(1);
            Assert.IsFalse(dot.gameObject.activeInHierarchy, "받고 나면 점이 꺼진다");

            _log.AssertNoRed("T366 로비 특권 점");
            yield return Shutdown();
        }
    }
}
