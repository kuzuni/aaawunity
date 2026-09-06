using System.Collections;
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
    /// T111 ⓐ — 주인 2026-09-07 07:5X «<b>챕터 아래에 LineDeco 들은 없애줘. 로비, 전투 화면 둘 다.</b>»:
    /// 챕터 제목은 프리팹 조각(<c>Title_LineDeco_01_*</c> · «글자 + 밑줄 장식»)을 쓰는데 그 안의 <b>밑줄만</b> 끈다(글자·자리·크기는 그대로).
    /// 같이 지키는 회귀: <b>상점 섹션 헤더</b>의 선(<c>LineDeco</c>)은 주인이 «투명도 255 중 13» 으로 살려 두라고 한 것이라(T100 ⓒ) 켜져 있어야 한다.
    /// 빨간 줄 0 은 <see cref="PlayLog"/>(ROUTINE §1).
    /// </summary>
    public class ChapterLineDecoTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
        }

        /// <summary>«챕터 N» 글자가 든 조각(제목 묶음)을 그 글자에서 거슬러 찾는다 — 조각 이름(Blue/l 변형)에 매이지 않는다.</summary>
        static Transform ChapterTitlePiece(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Text>(true))
            {
                if (t.text == null || !t.text.StartsWith("챕터")) continue;
                var p = t.transform.parent;
                while (p != null && p != root)
                {
                    if (p.name.StartsWith("Title_LineDeco")) return p;
                    p = p.parent;
                }
                return t.transform.parent;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator ChapterUnderlineIsGoneInLobbyAndBattleButShopSectionLinesStay()
        {
            yield return Boot();

            // ⓐ 로비(01) — 챕터 제목 조각은 살아 있고 그 안의 밑줄만 꺼져 있다
            _app.ShowScreen("lobby"); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var lobbyPiece = ChapterTitlePiece(_app.Current.Root);
            Assert.IsNotNull(lobbyPiece, "로비 챕터 제목 조각");
            var lobbyDeco = UiKit.Find(lobbyPiece, "LineDeco");
            Assert.IsNotNull(lobbyDeco, "조각의 밑줄 장식(LineDeco)은 지우지 않고 «끈다»");
            Assert.IsFalse(lobbyDeco.gameObject.activeInHierarchy, "로비 챕터 밑줄은 꺼져 있어야 한다(T111 ⓐ · 주인 «챕터 아래 LineDeco 없애줘»)");
            bool lobbyTitleAlive = false;
            foreach (var t in lobbyPiece.GetComponentsInChildren<Text>(false)) if (t.text != null && t.text.StartsWith("챕터")) lobbyTitleAlive = true;
            Assert.IsTrue(lobbyTitleAlive, "챕터 글자는 그대로 있어야 한다(밑줄만 끈다)");

            // ⓐ 전투(02) — 같은 조각을 쓰는 HUD 챕터 제목
            _app.StartBattle(1); yield return Frames(3); Canvas.ForceUpdateCanvases();
            var battle = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(battle, "전투 화면");
            var battlePiece = ChapterTitlePiece(battle.Root);
            Assert.IsNotNull(battlePiece, "전투 챕터 제목 조각");
            var battleDeco = UiKit.Find(battlePiece, "LineDeco");
            Assert.IsNotNull(battleDeco, "전투 조각의 밑줄 장식");
            Assert.IsFalse(battleDeco.gameObject.activeInHierarchy, "전투 챕터 밑줄도 꺼져 있어야 한다(주인 «로비, 전투 화면 둘 다»)");
            _app.ShowScreen("lobby"); yield return Frames(2);

            // 회귀 — 상점 섹션 헤더의 선은 살아 있다(주인 2026-09-07 «섹션 나누는 라인 데코는 투명도 255 중 13» · T100 ⓒ)
            _app.ShowScreen("shop"); yield return Frames(2); Canvas.ForceUpdateCanvases();
            int shopLines = 0;
            foreach (var im in _app.Current.Root.GetComponentsInChildren<Image>(false))
                if (im.name.StartsWith("LineDeco") && im.isActiveAndEnabled) shopLines++;
            Assert.Greater(shopLines, 0, "상점 섹션 헤더의 선(LineDeco)은 켜져 있어야 한다 — T111 ⓐ 는 «챕터 아래» 밑줄만 끈다(T100 ⓒ 회귀)");

            _log.AssertNoRed("T111 ⓐ 챕터 밑줄 제거");
            yield return Shutdown();
        }
    }
}
