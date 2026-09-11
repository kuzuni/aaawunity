using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T450 — <b>부르는 쪽이 이미 짧게 쓴 글자</b>(«1K»·«10K»·«1M»)로 구슬을 세는 길의 자(워커 E 등재 · T449 ④ⓑ 가 조건으로 걸어 둔 한 줄).
    /// <para>
    /// T449 의 고장이 정확히 이 길이었다 — 출석·챕터 상자·로비의 다섯 자리가 <c>amount</c> 없이 «1K» 를 넘기고, 옛 <c>QtyOf</c> 는 글자 안의 숫자만 주워 <b>1</b> 로 읽어
    /// 다이아 1000 이 구슬 <b>1개</b>가 됐다(백 배). 워커 K 가 읽는 쪽에 K·M·B 를 가르쳐 고쳤는데(결정 1261) 그 고침을 지키는 줄이 0개였다 —
    /// 고쳤지만 지키는 줄이 없으면 다음 회차에 조용히 되돌아간다(결정 1071).
    /// </para>
    /// <para>
    /// 재는 것: ⓐ «1K» → 1000 은 주인 캡(<see cref="RewardPopup.MaxOrbs"/> = 100)으로 잘려 구슬 100개(옛 «숫자 줍기» 면 1개) ⓑ «10K» → 100(옛 10개) ⓒ «1M» → 100(옛 1개)
    /// ⓓ 기호가 섞인 «×3»·«10%» 는 배수로 안 읽고 3·10 그대로(T448 의 «×3» 판을 «10%» 까지 넓혔다) ⓔ 빨간 줄 0.
    /// 세 값이 다 캡에 걸리는 것은 일부러다 — K·M 은 언제나 캡보다 크므로 «캡» 이 옳은 답이고, 옛 답(1·10·1)과는 셋 다 다르다.
    /// </para>
    /// ⚠ <b>«1K → 1» 을 기댓값으로 박지 않는다</b> — 그것은 고장을 계약으로 굳히는 것이다(워커 B 가 <c>RewardAbsorbTests</c> 에 적어 둔 그 자리).
    /// ⚠ T448 의 자 파일(<c>RewardAbsorbTests.cs</c>)은 검수 Q 의 lock 안이라 이 자는 <b>새 파일</b>에 선다 — 도우미 셋(Boot·Close·OrbCount)은 그 파일과 같은 꼴이다.
    /// </summary>
    public class RewardOrbQtyPlayTests
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
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        int OrbCount()
        {
            int n = 0;
            if (_app == null || _app.UiCanvas == null) return 0;
            foreach (var im in _app.UiCanvas.GetComponentsInChildren<Image>(true))
                if (im.name == RewardOrbs.OrbName) n++;
            return n;
        }
        static bool Close(Overlay ov)
        {
            var dim = UiKit.Find(ov.Root, "Dimmed");
            var b = dim != null ? dim.GetComponent<Button>() : null;
            if (b == null) return false;
            b.onClick.Invoke();
            return true;
        }

        /// <summary>팝업 하나를 «글자만 준 칸» 하나로 띄웠다 닫고, 난 구슬 수를 돌려준다(닫힌 뒤 두 프레임).</summary>
        IEnumerator ShowAndClose(string icon, string qty, System.Action<int> got)
        {
            // ⚠ amount 를 일부러 안 준다 — 게임의 부르는 쪽 다섯이 이 꼴이다(T449).
            RewardPopup.Show(new List<RewardPopup.Item> { RewardPopup.Item.Of(icon, qty) }); yield return Frames(3);
            Assert.AreEqual(1, RewardPopup.LastCellCount, "«" + qty + "» — 칸 하나");
            Assert.IsTrue(Close(_app.Overlay), "«" + qty + "» — 어둠을 눌러 닫는다"); yield return Frames(2);
            got(RewardPopup.LastOrbCount);
        }

        [UnityTest]
        public IEnumerator 부르는_쪽이_이미_짧게_쓴_글자도_구슬은_그_수만큼_난다()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);
            int got = 0;

            // ⓐ «1K» = 1000 → 캡 100 (옛 숫자 줍기면 1 · T449 의 «다이아 1000 이 구슬 1개»)
            yield return ShowAndClose("ui.gem", "1K", v => got = v);
            Assert.AreEqual(RewardPopup.MaxOrbs, got, "«1K» 는 1000 이라 캡(100)까지 난다 — 1 이면 QtyOf 가 K 를 잊은 것(T449 의 백 배)");
            Assert.AreEqual(RewardPopup.MaxOrbs, OrbCount(), "«1K» — 그만큼 실제로 떠 있다");

            // ⓑ «10K» = 10000 → 캡 100 (옛 10 · 골드 10000 이 구슬 10개이던 자리)
            yield return ShowAndClose("ui.coin", "10K", v => got = v);
            Assert.AreEqual(RewardPopup.MaxOrbs, got, "«10K» 는 10000 이라 캡(100)까지 난다 — 10 이면 K 를 잊은 것");

            // ⓒ «1M» → 캡 100 (옛 1)
            yield return ShowAndClose("ui.gem", "1M", v => got = v);
            Assert.AreEqual(RewardPopup.MaxOrbs, got, "«1M» 은 1000000 이라 캡(100)까지 난다 — 1 이면 M 을 잊은 것");

            // ⓓ 기호가 섞이면 배수로 안 읽는다 — «×3» 은 3 · «10%» 는 10 (T448 이 «×3» 을 박았고 «10%» 를 보탠다)
            yield return ShowAndClose("ui.coin", "×3", v => got = v);
            Assert.AreEqual(3, got, "«×3» 에서 3 (T448)");
            yield return ShowAndClose("ui.coin", "10%", v => got = v);
            Assert.AreEqual(10, got, "«10%» 에서 10 — % 는 배수가 아니다");

            _log.AssertNoRed("T450 짧게 쓴 글자로 세는 구슬");
            yield return Shutdown();
        }
    }
}
