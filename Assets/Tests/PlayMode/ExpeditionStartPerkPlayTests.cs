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
    /// T437 — <b>«화면이 그 다섯을 실제로 띄우는가» 를 재는 자가 0개였다.</b>
    /// <para>
    /// 엔진 쪽(<see cref="KkomaKnight.Tests.ExpeditionStartPerkTests"/> · EditMode)은 이미 남김없이 잰다 —
    /// 판이 서면 <c>Taken 0 · PendingLevelUps 5</c>, 다섯 번 연달아 <c>LevelUp</c>, 다 고르면 <c>Taken 5 · 줄 0</c>.
    /// 그런데 그 줄을 <b>화면이 팝업 다섯으로 바꿔 주는</b> 배선(<see cref="BattleScreen"/> 의 <c>OpenPending</c> →
    /// <see cref="Overlay"/> 의 3택)은 아무도 안 재고 있었다. 그래서 주인의 <c>👀</c> 표 ⑩ 이 «원정 들어가서 3택이
    /// 다섯 번 뜨는지 봐 달라» 로 서 있었다 — <b>자가 이미 세는 수를 주인 눈으로 세게 하고 있었다</b>(결정 1236).
    /// </para>
    /// <para>
    /// ⚑ <b>진짜 문</b>으로 들어간다 — <see cref="App.StartBattle"/> 에 표의 <see cref="DungeonData.RunRule"/> 를 실어서.
    /// 3택을 직접 <c>Overlay.LevelUp(…)</c> 으로 띄우면(<see cref="PerkShineTests"/> 가 그렇게 한다) 재려는 그 배선을
    /// 건너뛴다 — 그러면 <c>OpenPending</c> 이 통째로 죽어도 이 자는 초록이다.
    /// </para>
    /// <para>
    /// ⚠ <b>수를 자에 박지 않는다</b> — 다섯은 <c>dungeon.json</c> 의 <c>startPerks</c> 에서 읽는다. 손으로 «5» 라 적으면
    /// 주인이 표를 고치는 날 이 자가 «고장» 이라며 옳은 화면을 빨갛게 만든다(T42·결정 1159 의 문법).
    /// </para>
    /// </summary>
    public class ExpeditionStartPerkPlayTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);
            _log.AssertNoRed("부팅");
        }

        /// <summary>지금 서 있는 팝업이 3택인가 — 카드 담는 자리(<c>Group_Card</c>)로 가른다(<c>Playthrough.Poke</c> 와 같은 잣대).</summary>
        Transform OpenCardGroup()
        {
            if (_app == null || _app.Overlay == null || !_app.Overlay.IsOpen) return null;
            var root = _app.Overlay.Root; if (root == null) return null;
            return UiKit.Find(root, "Group_Card");
        }

        /// <summary>그 3택에서 첫 카드를 고른다 — 눌리는 카드가 없으면 아직 등장 연출 중이라 거짓을 준다.</summary>
        static bool PickFirstCard(Transform group)
        {
            if (group == null) return false;
            foreach (var b in group.GetComponentsInChildren<Button>(false))
                if (b.interactable && b.gameObject.activeInHierarchy) { b.onClick.Invoke(); return true; }
            return false;
        }

        /// <summary>켜져 있고 눌리는 카드 수 — «고르는» 것이려면 둘보다 많아야 한다(하나면 고를 것이 없다).</summary>
        static int LiveCards(Transform group)
        {
            int n = 0;
            if (group == null) return 0;
            foreach (var b in group.GetComponentsInChildren<Button>(false))
                if (b.interactable && b.gameObject.activeInHierarchy) n++;
            return n;
        }

        /// <summary>
        /// 원정으로 판에 들어가면 <b>3택이 표가 정한 수만큼 연달아 서고</b>, 매번 고를 수 있고, 다 고르면 줄이 빈다.
        /// </summary>
        [UnityTest]
        public IEnumerator 원정으로_들어가면_3택이_표의_수만큼_서고_다_고르면_줄이_빈다()
        {
            yield return Boot();

            var dun = _app.Data != null ? _app.Data.Dungeon : null;
            Assert.IsNotNull(dun, "던전 표(data.dungeon)");
            var e = dun.Of("expedition");
            Assert.IsNotNull(e, "원정 항목(dungeon.json 의 «expedition»)");
            Assert.IsNotNull(e.Run, "원정 판 규칙(RunRule) — 이것이 없으면 일반 전투와 같은 판이다");
            int want = e.Run.StartPerks;
            Assert.Greater(want, 0,
                "표가 원정의 시작 특전을 0 으로 두면 이 자가 잴 것이 없다 — 주인 «시작부터 특전 5개 선택하고 시작»(T411)이 그 칸이다");

            // ⚑ 진짜 문으로 들어간다 — EventsScreen 의 «도전» 이 부르는 그 줄과 같은 꼴이다(티켓만 안 쓴다).
            _app.StartBattle(1, e.Run, "expedition");
            yield return Frames(2);

            var bs = _app.GetScreen<BattleScreen>();
            Assert.IsNotNull(bs, "전투 화면");
            Assert.IsNotNull(bs.G, "판(BattleState) — StartBattle 이 세운다");
            Assert.AreEqual(0, bs.G.Taken.Count,
                "판이 서자마자 특전이 붙어 있으면 «고르는» 것이 아니라 «주는» 것이다 — 주인이 «안 돼 있네» 라 한 그 꼴(T411)");
            Assert.AreEqual(want, bs.G.PendingLevelUps,
                "고를 기회가 표의 수(" + want + ")만큼 줄에 서야 화면이 그 수만큼 띄운다");

            // 다섯을 세는 동안 «진짜 렙업»(판을 돌다 경험치로 오르는 것)이 섞이면 세는 수가 거짓이 된다.
            // 그래서 줄이 빌 때까지만 돌리고, 빈 뒤에는 엔진을 세워 놓고 «더 안 선다» 를 본다.
            int opened = 0, guard = 0;
            while (opened < want && guard++ < 1800)
            {
                var group = OpenCardGroup();
                if (group != null)
                {
                    int cards = LiveCards(group);
                    if (cards > 0)
                    {
                        Assert.Greater(cards, 1,
                            (opened + 1) + "번째 3택에 고를 카드가 하나뿐이다 — 그것은 «고르는» 것이 아니라 «주는» 것이다");
                        Assert.IsTrue(PickFirstCard(group), (opened + 1) + "번째 3택의 카드를 누른다");
                        opened++;
                    }
                }
                yield return null;
            }

            Assert.AreEqual(want, opened,
                "원정에 들어가면 3택이 표의 수만큼 서야 한다 — " + opened + "번만 섰다면 그 줄(PendingLevelUps)을 화면이 다 안 풀었다는 뜻이고, "
                + "그때 주인은 «특전을 덜 고르고» 판을 시작한다(BattleScreen.OpenPending → Overlay.LevelUp 배선을 보라)");
            Assert.AreEqual(want, bs.G.Taken.Count, "고른 수만큼 실제로 붙어야 한다(엔진 쪽 짝 = ExpeditionStartPerkTests)");
            Assert.AreEqual(0, bs.G.PendingLevelUps, "다 고르면 줄이 빈다");

            // ⚠ «여섯 번째가 없다» 는 엔진을 세워 놓고 본다 — 안 그러면 판을 돌다 오르는 **진짜 렙업**을 여섯 번째로 잘못 센다.
            //   (그 진짜 렙업은 이 자가 재는 것이 아니다 — 여기서 재는 것은 «시작 특전» 다섯뿐이다.)
            Time.timeScale = 0f;
            yield return Frames(6);
            Assert.IsNull(OpenCardGroup(),
                "줄이 비었는데 3택이 또 서 있다 — 시작 특전이 표의 수보다 많이 열린다는 뜻이다");

            _log.AssertNoRed("원정 시작 3택");
        }
    }
}
