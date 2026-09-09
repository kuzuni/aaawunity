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
    /// T361 게이트 — **리본 제목과 팝업 상자 사이가 떠 보이면 안 된다**(주인 2026-09-10 «타이틀 감싸는 프레임들이 아래 팝업이랑 거리가 떨어져 있어 거슬림 · 팝업에 상단 부분만 늘려서 이어진 것처럼»).
    /// <para>
    /// 앞 네 회차가 못 박은 함정 둘을 자에 그대로 옮겼다:
    /// ⓐ «리본 rect 밑단 ↔ 상자 rect 윗변» 은 표부터 붙어 있어 늘 초록이다(결정 998) ⇒ 재는 것은 <b>상자 그림 조각의 윗변</b>(<see cref="LobbyPopups.SealPieces"/>)과
    /// <b>리본 몸통 밑단</b>(<see cref="LobbyPopups.RibbonBodyBottomWorldY"/> · rect 밑단이 아니다 · T369 의 표)이다.
    /// ⓑ 상자 rect 를 키우면 안쪽 내용이 전부 밀린다(결정 1015) ⇒ 상자 rect(앵커)는 표 그대로여야 한다.
    /// </para>
    /// 세 팝업(퀘스트 15 · 출석 16 · 데일리 기프트 17)마다 ① 조각 윗변이 «리본 몸통 밑단 + 조각 위 테두리» 이상(검은 선이 몸통 아래로 숨었다) ·
    /// ② 조각 윗변이 리본 rect 윗변보다 위로 나가지 않는다(상자가 리본 위로 삐져나오지 않는다) · ③ 리본이 조각보다 나중에 그려진다(형제 번호) · ④ 상자 앵커 = 표.
    /// 눈 확인은 `screens` 15·16·17(3항).
    /// </summary>
    public class RibbonSeamTests
    {
        App _app; PlayLog _log;
        /// <summary>월드 px 오차 한계(프레임 1080×2337 기준 · 셈은 float).</summary>
        const float Tol = 0.5f;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

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

        [UnityTest]
        public IEnumerator RibbonPopupsSealTheSeam()
        {
            yield return Boot();

            // 15 퀘스트 — 프리팹(Progression_Mission_02)에 박힌 Popup_Box_01_Basic + Title_Tapered_01 띠
            LobbyPopups.Quest(_app); yield return Frames(1);
            AssertSealed("퀘스트 15", "QuestBox", "Title_Tapered_01", Layout.QsBox);
            _app.Overlay.Close(); yield return Frames(1);

            // 16 출석 — 프리팹(Rewards_Daily7_Popup)에 박힌 Popup_Box_01 + Title_01_Deco(꼬리가 긴 조각 · 18%)
            LobbyPopups.Attendance(_app); yield return Frames(1);
            AssertSealed("출석 16", "AttendanceBox", "Title_01_Deco", Layout.AtBox);
            _app.Overlay.Close(); yield return Frames(1);

            // 17 데일리 기프트 — 공통 Overlay.OpenBox 상자(Bg·Border·DecoLine) + Ribbon() 이 세운 리본
            var GD = _app.Data.DailyGift; Assert.IsNotNull(GD, "dailyGift.json 이 카탈로그(data.dailyGift)로 로드됐다");
            _app.Save.GiftDay = ""; _app.Save.GiftAds = 0; _app.Save.GiftFree = false; _app.Save.GiftClaimed.Clear();
            KkomaKnight.Core.DailyGift.Roll(_app.Save, GD, SaveStore.Today());
            LobbyPopups.DailyGift(_app); yield return Frames(1);
            AssertSealed("데일리 기프트 17", "DailyGiftBox", "Title_", Layout.GfBox);
            _app.Overlay.Close(); yield return Frames(1);

            _log.AssertNoRed("리본 팝업 셋 열고 닫기");
            yield return Shutdown();
        }

        /// <summary>상자 안에서 이름이 <paramref name="prefix"/> 로 시작하는 첫 자식(리본 조각).</summary>
        static RectTransform ChildStarting(Transform root, string prefix)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name.StartsWith(prefix, System.StringComparison.Ordinal) && c.GetComponent<Image>() != null) return (RectTransform)c;
            }
            return null;
        }

        void AssertSealed(string what, string boxName, string ribbonPrefix, Layout.R table)
        {
            var box = (RectTransform)UiKit.Find(_app.Overlay.Root, boxName);
            Assert.IsNotNull(box, what + ": 상자 " + boxName);
            var ribbon = ChildStarting(box, ribbonPrefix);
            Assert.IsNotNull(ribbon, what + ": 리본 조각(" + ribbonPrefix + "*)이 상자의 자식이다");
            Assert.Greater(Overlay.RibbonBodyBottomFrac(ribbon), 0f, what + ": 리본 조각이 몸통 표(RibbonBodyBottomFrac)에 있다 — 새 조각이면 표에 한 줄 보탠다");

            // ④ 상자 rect 는 표 그대로(안쪽 내용이 안 밀렸다는 뜻 · 결정 1015 의 함정)
            Assert.AreEqual(table.X, box.anchorMin.x * 100f, 0.05f, what + ": 상자 앵커 x = 표");
            Assert.AreEqual(100f - (table.Y + table.H), box.anchorMin.y * 100f, 0.05f, what + ": 상자 앵커 아래 = 표");
            Assert.AreEqual(100f - table.Y, box.anchorMax.y * 100f, 0.05f, what + ": 상자 앵커 위 = 표(상자 rect 는 안 키웠다)");

            var pieces = LobbyPopups.SealPieces(box);
            Assert.Greater(pieces.Count, 0, what + ": 상자 그림 조각(Popup_Box_* 또는 Bg·Border)이 있다");
            float border = 0f; foreach (var pc in pieces) border = Mathf.Max(border, LobbyPopups.SealBorderPx(pc));
            Assert.Greater(border, 0f, what + ": 상자 조각이 9-slice 다(위 테두리 > 0 · 올릴 검은 선이 있다)");
            Assert.IsTrue(LobbyPopups.SealedPx.TryGetValue(boxName, out var lifted) && lifted > 0f, what + ": SealRibbonSeam 이 이 상자를 올렸다");

            float bodyBottom = LobbyPopups.RibbonBodyBottomWorldY(ribbon);
            var rr = ribbon.rect; float ribbonTop = ribbon.TransformPoint(new Vector3(rr.center.x, rr.yMax, 0f)).y;
            foreach (var pc in pieces)
            {
                float top = LobbyPopups.PieceTopWorldY(pc);
                float need = bodyBottom + LobbyPopups.SealBorderPx(pc) * pc.lossyScale.y;
                // ① 조각 윗변 ≥ 리본 몸통 밑단 + 테두리 — 검은 선이 통째로 몸통 아래로 들어갔다
                Assert.GreaterOrEqual(top + Tol, need, what + ": 조각 " + pc.name + " 윗변(" + top + ")이 리본 몸통 밑단 + 테두리(" + need + ") 이상 — 틈 0");
                // ② 리본 rect 위로는 안 나간다 — 상자가 리본 위로 삐져나오면 그것이 새 «거슬림» 이다
                Assert.LessOrEqual(top, ribbonTop + Tol, what + ": 조각 " + pc.name + " 윗변이 리본 rect 윗변(" + ribbonTop + ") 아래");
                // ③ 리본이 조각보다 나중에 그려진다(올린 부분을 리본이 덮는다)
                Assert.Greater(ribbon.GetSiblingIndex(), pc.GetSiblingIndex(), what + ": 리본이 조각 " + pc.name + " 보다 뒤 형제(앞에 그려진다)");
            }
        }
    }
}
