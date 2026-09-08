using System.Collections;
using System.Collections.Generic;
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
    /// T241 1단계 — 공통 «리워드» 획득 팝업(<see cref="RewardPopup"/>)이 <b>레퍼런스 35 의 꼴</b>로 서고, 눌러서 닫히는가.
    /// <para>
    /// 재는 것: ⓐ 칸이 지급한 만큼 선다 ⓑ 칸이 <b>두 노란 줄 사이</b>에 있다(레퍼런스의 그 자리) ⓒ 제목·닫기 안내 글자
    /// ⓓ 어둠을 누르면 닫히고 <c>onClose</c> 가 불린다 ⓔ 빨간 줄 0 ⓕ 빈 목록이면 <b>안 뜬다</b>.
    /// </para>
    /// «닫는 길이 있는가» 를 코드로 안 보고 <b>눌러서</b> 재는 까닭은 <see cref="PopupCloseTests"/> 머리에 적힌 그대로다(T169 2항).
    /// </summary>
    public class RewardPopupTests
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

        /// <summary>세로 가운데(프레임 위에서 %) — 표(<c>ref-layout</c> ㊹)와 같은 잣대로 잰다.</summary>
        static float CenterYPct(RectTransform frame, RectTransform t)
        {
            var c = new Vector3[4]; t.GetWorldCorners(c);
            var f = new Vector3[4]; frame.GetWorldCorners(f);
            float top = f[1].y, h = f[1].y - f[0].y;
            return h <= 0f ? -1f : (top - (c[0].y + c[1].y) * 0.5f) / h * 100f;
        }

        [UnityTest]
        public IEnumerator RewardPopupShowsCellsBetweenRulesAndCloses()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            // ⓕ 빈 목록이면 뜨지 않는다 — «얻은 게 없는데 뜨는» 팝업 금지
            RewardPopup.Show(new List<RewardPopup.Item>()); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen, "빈 목록이면 리워드 팝업은 안 뜬다(T241)");

            bool closed = false;
            RewardPopup.Show(new List<RewardPopup.Item>
            {
                RewardPopup.Item.Of("ui.coin", "1,000"),
                RewardPopup.Item.Of("pi.gem", "50"),
            }, () => closed = true);
            yield return Frames(3);
            UiKit.CompleteAllTweens();   // 등장 연출(stagger)을 끝까지 돌리고 잰다 — 자리는 연출과 무관해야 한다(T49 규약 · EventPopupNoBoxTests 와 같은 방법)
            yield return Frames(1);

            var ov = _app.Overlay.Root;
            Assert.IsTrue(_app.Overlay.IsOpen, "리워드 팝업이 열린다");
            Assert.AreEqual(2, RewardPopup.LastCellCount, "지급한 만큼 칸이 선다");

            // ⓒ 글자 — 제목과 닫기 안내
            var title = UiKit.Find(ov, "RewardTitle")?.GetComponent<TMP_Text>();
            Assert.IsNotNull(title, "제목 «리워드»");
            Assert.AreEqual(TextGlyphs.Safe("리워드"), title.text, "제목 문구(레퍼런스 35 의 «Reward» 자리)");
            Assert.IsNotNull(UiKit.Find(ov, "TapToClose"), "바닥 «탭하여 닫기»");

            // ⓑ 칸이 두 노란 줄 «사이» 에 있다 — 레퍼런스가 그 두 줄로 자리를 못 박는다
            var top = UiKit.Find(ov, "RewardLineTop") as RectTransform;
            var bottom = UiKit.Find(ov, "RewardLineBottom") as RectTransform;
            Assert.IsNotNull(top, "위 노란 줄"); Assert.IsNotNull(bottom, "아래 노란 줄");
            float yTop = CenterYPct(_app.Frame, top), yBottom = CenterYPct(_app.Frame, bottom);
            Assert.Less(yTop, yBottom, "위 줄이 아래 줄보다 위다");
            for (int i = 0; i < 2; i++)
            {
                var cell = UiKit.Find(ov, "RewardCell:" + i) as RectTransform;
                Assert.IsNotNull(cell, "보상 칸 " + i);
                float y = CenterYPct(_app.Frame, cell);
                Assert.Greater(y, yTop, "칸 " + i + " 이 위 줄보다 아래(레퍼런스 35)");
                Assert.Less(y, yBottom, "칸 " + i + " 이 아래 줄보다 위(레퍼런스 35)");
                Assert.IsNotNull(UiKit.Find(cell, "Icon"), "칸 " + i + " 안 그림");
            }
            _log.AssertNoRed("리워드 팝업 열림");

            // ⓓ 어둠을 누르면 닫히고 onClose 가 불린다
            var dim = UiKit.Find(ov, "Dimmed")?.GetComponent<Button>();
            Assert.IsNotNull(dim, "어둠이 눌리는 자리(탭하여 닫기)");
            dim.onClick.Invoke(); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "탭하면 닫힌다");
            Assert.IsTrue(closed, "닫히면 onClose 가 불린다(부른 화면이 뒷일을 잇는다)");
            _log.AssertNoRed("리워드 팝업 닫힘");

            yield return Shutdown();
        }
    }
}
