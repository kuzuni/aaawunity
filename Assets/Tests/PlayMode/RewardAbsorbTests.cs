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
    /// T269 — 리워드 팝업을 <b>닫으면 받은 것이 파티클로 흡수</b>(주인 2026-09-09 «화면 끄면 이제 해당 재화들 파티클로 돼서 흡수되는 거로 · 개수만큼 · 캡은 100개 파티클»).
    /// <para>
    /// 재는 것: ⓐ 닫기 <b>전에는</b> 구슬이 없다(팝업이 뜬 것만으로 날아가면 안 된다) ⓑ 닫으면 <b>받은 개수만큼</b> 생긴다
    /// ⓒ 개수가 많아도 <b>100개를 안 넘는다</b>(주인 캡) ⓓ 구슬 그림이 그 칸의 아이콘과 <b>같은 스프라이트</b>다(3항) ⓔ 빨간 줄 0.
    /// </para>
    /// ⚠ <b>연출만 재고 값은 안 잰다</b> — 지급은 팝업이 뜨기 전에 이미 끝나 있다(T243 «즉시 지급»). 이 자가 재화를 세면 «흡수가 지급» 이라는 거짓 계약이 생긴다.
    /// </summary>
    public class RewardAbsorbTests
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
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

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

        [UnityTest]
        public IEnumerator ClosingThePopupSendsOneParticlePerUnit()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            var items = new List<RewardPopup.Item>
            {
                RewardPopup.Item.Of("ui.coin", "3", null, 3),
                RewardPopup.Item.Of("hud.gem", "5", null, 5),
            };
            RewardPopup.Show(items); yield return Frames(2);
            Assert.AreEqual(2, RewardPopup.LastCellCount, "칸 둘");
            Assert.AreEqual(0, OrbCount(), "팝업이 떠 있는 동안에는 아무것도 안 날아간다");

            // 칸의 아이콘 스프라이트를 닫기 «전에» 잡아 둔다 — 닫으면 칸이 파괴된다(3항 «그림 그대로» 판정용).
            var cell = UiKit.Find(_app.Overlay.Root, "RewardCell:0");
            var cellIcon = cell != null ? UiKit.Find(cell, "Icon") : null;
            var want = cellIcon != null ? cellIcon.GetComponent<Image>().sprite : null;
            Assert.IsNotNull(want, "보상 칸의 아이콘");

            Assert.IsTrue(Close(_app.Overlay), "어둠을 눌러 닫는다"); yield return Frames(2);

            Assert.AreEqual(8, RewardPopup.LastOrbCount, "받은 개수만큼(3 + 5)");
            Assert.AreEqual(8, OrbCount(), "그만큼 실제로 떠 있다");
            // T354(주인 2026-09-10 «재화 흡수 이펙트가 팝업들보다 레이어가 낮아서 안 보여») — 구슬 층은 프레임 밑이고 오버레이보다 «앞» 이다.
            //   화면 루트 밑이면 그 안에서 맨 위여도 오버레이 아래다(부모가 다르다) — 그래서 부모와 형제 번호, 둘을 잰다.
            {
                var layer = UiKit.Find(_app.Frame, RewardPopup.OrbLayerName);
                Assert.IsNotNull(layer, "구슬 층(" + RewardPopup.OrbLayerName + ")은 프레임 밑에 선다(T354)");
                Assert.AreEqual(_app.Frame, layer.parent, "구슬 층의 부모 = 프레임(화면 루트가 아니다 · T354)");
                Assert.Greater(layer.GetSiblingIndex(), _app.Overlay.Root.GetSiblingIndex(), "구슬 층은 오버레이보다 앞에 그려진다(T354)");
            }
            // T367(주인 2026-09-10 «다이아는 다이아 쪽, 골드는 골드 쪽으로 흡수») — 보상 칸이 실제로 쓰는 키(ui.gemRed · ui.coin)가 제 pill 로 간다.
            //   옛 판정은 다이아 키 셋만 알아 `ui.gemRed`(출석·데일리·챕터 상자·퀘스트 트랙)가 가운데 아래 «자리 없음» 으로 빨려 들어갔다.
            Assert.AreEqual("ResourceBar_Gem", RewardPopup.PillFor("ui.gemRed"), "ui.gemRed(보상 칸의 다이아) → 다이아 pill(T367)");
            Assert.AreEqual("ResourceBar_Gem", RewardPopup.PillFor("hud.gem"), "hud.gem → 다이아 pill");
            Assert.AreEqual("ResourceBar_Coin", RewardPopup.PillFor("ui.coin"), "ui.coin → 골드 pill");
            Assert.AreEqual("ResourceBar_Coin", RewardPopup.PillFor("hud.gold"), "hud.gold → 골드 pill");
            Assert.IsNull(RewardPopup.PillFor("ui.bookBlue"), "재화 아닌 것은 pill 없음(가운데 아래로)");
            Assert.IsNull(RewardPopup.PillFor("ui.keyBlue"), "열쇠는 pill 없음");
            Assert.IsFalse(_app.Overlay.IsOpen, "팝업은 닫혔다");

            bool same = false;
            foreach (var im in _app.UiCanvas.GetComponentsInChildren<Image>(true))
                if (im.name == RewardOrbs.OrbName && im.sprite == want) same = true;
            Assert.IsTrue(same, "파티클 그림 = 그 칸의 아이콘(3항)");
            _log.AssertNoRed("리워드 흡수");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator TheParticleCountIsCappedAtAHundred()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            var items = new List<RewardPopup.Item>
            {
                RewardPopup.Item.Of("ui.coin", "5,000", null, 5000),
                RewardPopup.Item.Of("hud.gem", "300", null, 300),
            };
            RewardPopup.Show(items); yield return Frames(2);
            Assert.IsTrue(Close(_app.Overlay), "닫는다"); yield return Frames(2);

            Assert.AreEqual(RewardPopup.MaxOrbs, RewardPopup.LastOrbCount, "주인 «캡은 100개 파티클»");
            Assert.AreEqual(RewardPopup.MaxOrbs, OrbCount(), "화면에 뜬 것도 그만큼이다");
            _log.AssertNoRed("리워드 흡수(캡)");
            yield return Shutdown();
        }
    }
}
