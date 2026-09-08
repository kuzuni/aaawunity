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
    /// T240 4항 — PvP 결과 화면(<see cref="ArenaResult"/>)이 <b>레퍼런스 34 의 꼴</b>로 서고, «계속» 로 닫히는가.
    /// <para>
    /// 재는 것: ⓐ 제목이 «승리»/«패배» 로 갈린다 ⓑ 티어 명판이 <b>표에서 온 이름</b>을 그대로 띄운다
    /// ⓒ 승점 글자가 <b>실제로 움직인 값</b>(<c>Delta</c>)이다 — 바닥에 걸린 판은 «−6» 이 아니다
    /// ⓓ 두 아바타·VS 배지·이름 둘이 선다 ⓔ «계속» 을 <b>눌러서</b> 닫히고 <c>onContinue</c> 가 불린다 ⓕ 빨간 줄 0.
    /// </para>
    /// <para>
    /// ⚠ 여기 <b>«전투가 돈다» 는 없다</b> — 이 회차는 화면만 세웠고 배선(도전 → 전투 → 이 화면)은 다음 회차다.
    /// 그래서 이 자는 <see cref="ArenaMatch.Settle"/> 이 낸 값을 <b>손으로 만들어</b> 넣는다(판을 기다리지 않는다 · §1 «그 판에 그 일이 일어난다» 규약).
    /// </para>
    /// «닫는 길이 있는가» 를 코드로 안 보고 <b>눌러서</b> 재는 까닭은 <see cref="PopupCloseTests"/> 머리에 적힌 그대로다(T169 2항).
    /// </summary>
    public class ArenaResultTests
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
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static bool Click(Transform root, string name)
        {
            var b = UiKit.Find(root, name)?.GetComponent<Button>();
            if (b == null) return false;
            b.onClick.Invoke(); return true;
        }

        static TMP_Text Text(Transform root, string name)
        {
            var t = UiKit.Find(root, name);
            return t == null ? null : t.GetComponent<TMP_Text>();
        }

        /// <summary>표를 실제로 태워 «내가 지어낸 값» 이 아니라 <b>그 판의 값</b>으로 화면을 세운다.</summary>
        ArenaMatch.Outcome Settle(double from, bool win)
        {
            var D = _app.Data;
            _app.Save.ArenaScore = from;
            return ArenaMatch.Settle(_app.Save, D != null ? D.ArenaMatch : null, D != null ? D.ArenaDummy : null, win);
        }

        [UnityTest]
        public IEnumerator WinScreenShowsTheTitleTierAndTheScoreItActuallyGained()
        {
            yield return Boot();
            var o = Settle(1000, true);
            bool continued = false;
            ArenaResult.Show(o, "나", "도전자 3", null, null, () => continued = true);
            yield return Frames(2);

            var root = _app.Overlay.Root;
            Assert.IsTrue(ArenaResult.Open, "떠 있어야 한다");
            Assert.AreEqual(ArenaResult.WinTitle, Text(root, "ResultTitle")?.text, "이긴 판의 제목");
            Assert.AreEqual(o.Tier, Text(root, "TierName")?.text, "명판은 표에서 온 티어 이름 그대로다");
            Assert.AreEqual(ArenaResult.Sign(o.Delta), Text(root, "MyDelta")?.text, "승점 글자는 실제로 움직인 값이다");
            Assert.AreEqual("나", Text(root, "MyName")?.text);
            Assert.AreEqual("도전자 3", Text(root, "FoeName")?.text);
            Assert.IsNotNull(UiKit.Find(root, "Emblem"), "방패 엠블럼");
            Assert.IsNotNull(UiKit.Find(root, "VsBadge"), "VS 배지");
            Assert.IsNotNull(UiKit.Find(root, "MyFace"), "내 초상 칸");
            Assert.IsNotNull(UiKit.Find(root, "FoeFace"), "상대 초상 칸");

            // 눌러서 닫힌다 — «닫는 길이 코드에 있다» 가 아니라 «눌리면 닫힌다» 를 잰다(T169 2항)
            var btn = UiKit.Find(root, "ContinueBtn")?.GetComponent<Button>();
            Assert.IsNotNull(btn, "«계속» 버튼");
            btn.onClick.Invoke();
            yield return Frames(2);
            Assert.IsFalse(ArenaResult.Open, "«계속» 을 누르면 닫힌다");
            Assert.IsTrue(continued, "onContinue 가 불린다(아레나 화면으로 돌아가는 자리)");

            _log.AssertNoRed("PvP 결과(승리)");
            yield return Shutdown();
        }

        /// <summary>
        /// T240 배선 — 아레나 «줄 도전» 을 누르면 <b>판이 실제로 열리고</b> 그 판이 «아레나 판» 으로 표시된다.
        /// <para>여기서 재는 것은 <b>«열리는가» 까지</b>다 — 판을 끝까지 돌리지 않는다(판이 언제 끝나는지는 매번 다르고,
        /// 그것을 기다리는 단언이 오늘 배포를 세운 그 꼴이다 · §1 «그 판에 그 일이 일어난다» 규약).</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ArenaChallengeActuallyOpensAMatchMarkedAsArena()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageArena); yield return Frames(2);
            var ar = _app.Current.Root;
            Assert.IsTrue(Click(ar, "ChallengeBtn"), "«도전» 을 눌러 팝업을 연다"); yield return Frames(2);
            var ov = _app.Overlay.Root;
            Assert.IsTrue(Click(ov, "FoeBtn:0"), "상대 줄의 «도전»"); yield return Frames(2);

            Assert.AreEqual("battle", _app.Current.Name, "판이 열린다(여태 이 버튼은 Noop 이었다)");
            var bs = _app.GetScreen<BattleScreen>();
            Assert.IsNotNull(bs);
            Assert.IsTrue(bs.IsArena, "그 판은 «아레나 판» 으로 표시된다 — 이 표식 하나가 끝났을 때 승점 갈래를 켠다");
            Assert.AreEqual(EventsScreen.PageArena, bs.ExitPage, "끝나면 아레나 화면으로 돌아간다");

            _log.AssertNoRed("아레나 도전 → 판 열림");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator LoseAtTheFloorWritesWhatItTookNotWhatTheTableSays()
        {
            yield return Boot();
            // 승점 4 에서 지면 표는 −6 이지만 바닥(0)에 걸려 실제로는 −4 만 간다 — 화면이 «−6» 이라 적으면 거짓말이다
            var o = Settle(4, false);
            ArenaResult.Show(o, "나", "도전자 9", null, null);
            yield return Frames(2);

            var root = _app.Overlay.Root;
            Assert.AreEqual(ArenaResult.LoseTitle, Text(root, "ResultTitle")?.text, "진 판의 제목");
            Assert.AreEqual(0, _app.Save.ArenaScore, "0 미만으로는 안 내려간다(주인 문장)");
            Assert.AreEqual("−4", Text(root, "MyDelta")?.text, "표의 −6 이 아니라 실제로 간 −4 다");

            _log.AssertNoRed("PvP 결과(패배·바닥)");
            yield return Shutdown();
        }
    }
}
