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
    /// T97 — 탐험(방치·오프라인 보상 · 주인 2026-09-07 «켜두거나 꺼둬도 쩄든 방치 보상 쌓이고 · 골드·다이아 쌓이게 · 빠른 탐험은 광고 보고»).
    /// 규칙 자체는 EditMode <c>ExpeditionTests</c> 가 본다 — 여기서는 <b>화면</b>이다:
    /// ⓐ 로비 «탐험» 보조 버튼이 팝업(표 ㉕)을 연다 ⓑ 조각이 다 있다(그림 띠·명판·경과 시간·시간당 pill 2·보상 칸 2·버튼 2)
    /// ⓒ 8시간 전으로 «마지막 정산» 을 돌려 두면 «받기» 가 열리고 누르면 재화가 늘고 칸이 0 으로 돌아간다
    /// ⓓ 빠른 탐험 팝업(표 ㉖)이 뜨고 보상 칸·광고 버튼이 있다 ⓔ 로비 «탐험» 아이콘 빨간 점 ⓕ 영문 데모 글자 0 · 빨간 줄 0(<see cref="PlayLog"/> · T11 규약).
    /// </summary>
    public class ExpeditionScreenTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; Assert.IsNotNull(_app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");
            yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { var r = Find(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }
        static string CellQty(Transform root, string cell)
        {
            var c = Find(root, cell); if (c == null) return null;
            var q = Find(c, "Qty"); var t = q != null ? q.GetComponent<Text>() : null;
            return t != null ? t.text : null;
        }
        /// <summary>화면의 모든 활성 글자에서 «영문 데모 글자»(우리 문구는 전부 한국어·숫자·기호)를 찾는다 — T44 규칙.</summary>
        static string EnglishLeftOver(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Text>(true))
            {
                if (t == null || !t.gameObject.activeInHierarchy || string.IsNullOrEmpty(t.text)) continue;
                foreach (var ch in t.text) if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')) return t.name + " «" + t.text + "»";
            }
            return null;
        }

        /// <summary>ⓐⓑⓒⓕ — 로비에서 팝업이 열리고, 8시간치가 쌓인 상태면 «받기» 로 골드·다이아가 실제로 는다.</summary>
        [UnityTest]
        public IEnumerator ExpeditionPopupShowsAccruedRewardsAndClaimPaysThem()
        {
            yield return Boot();
            var D = _app.Data != null ? _app.Data.Expedition : null;
            Assert.IsNotNull(D, "expedition.json 이 카탈로그(data.expedition)로 실려야 한다");
            _app.ShowScreen("lobby"); yield return Frames(2);

            // 로비의 «탐험» 보조 버튼(Side:explore)이 팝업을 연다
            var side = Find(_app.UiCanvas.transform, "Side:" + LobbyScreen.SideExplore);
            Assert.IsNotNull(side, "로비 보조 줄에 «탐험» 칸이 있어야 한다");
            var S = _app.Save;
            S.MaxChapter = 10; S.Gold = 0; S.Gem = 0;
            S.ExpSettle = LobbyPopups.NowSec() - D.MaxHours * 3600.0;   // 8시간 방치(오프라인) 상태로 열어 본다
            LobbyPopups.Expedition(_app); yield return Frames(2);
            var ov = _app.Overlay.Root;
            Assert.IsTrue(_app.Overlay.IsOpen, "탐험 팝업이 열린다");
            Assert.IsNotNull(Find(ov, "ExpeditionBox"), "팝업 상자");
            foreach (var n in new[] { "Picture", "Plate", "ExpTime", "RateGold", "RateGem", "ExpCellGold", "ExpCellGem", "QuickBtn", "ClaimBtn", "CapNote" })
                Assert.IsNotNull(Find(ov, n), "조각 " + n + " (표 ㉕)");

            // 쌓인 값이 화면에 «0» 이 아니라 실제 계산값으로 찍힌다
            Expedition.Pending(_app.Data, S, D, LobbyPopups.NowSec(), SaveStore.Today(), out double pg, out double pm);
            Assert.Greater(pg, 0, "8시간이면 골드가 쌓여 있다"); Assert.Greater(pm, 0, "다이아도 쌓여 있다");
            Assert.AreEqual(UiKit.Fmt(pg), CellQty(ov, "ExpCellGold"), "골드 칸 숫자 = 규칙이 계산한 값");
            Assert.AreEqual(UiKit.FmtQty(pm), CellQty(ov, "ExpCellGem"), "다이아 칸 숫자 = 규칙이 계산한 값");
            Assert.IsNull(EnglishLeftOver(ov), "영문 데모 글자 0 (T44)");

            // «받기» — 실제로 재화가 늘고, 칸은 0 으로 돌아간다
            var claim = Find(ov, "ClaimBtn"); Assert.IsNotNull(claim, "받기 버튼");
            var btn = claim.GetComponent<Button>(); Assert.IsNotNull(btn); Assert.IsTrue(btn.interactable, "쌓인 게 있으면 받기가 열린다");
            double gold0 = S.Gold, gem0 = S.Gem;
            btn.onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(pg, S.Gold - gold0, 1.0, "받기 = 보이던 골드만큼 지급");
            Assert.AreEqual(pm, S.Gem - gem0, 1.0, "받기 = 보이던 다이아만큼 지급");
            Assert.AreEqual("0", CellQty(_app.Overlay.Root, "ExpCellGold"), "받은 뒤에는 0 부터 다시 쌓인다");
            var claim2 = Find(_app.Overlay.Root, "ClaimBtn");
            Assert.IsFalse(claim2.GetComponent<Button>().interactable, "받은 직후에는 받기가 잠긴다(«다음까지 mm:ss»)");
            _log.AssertNoRed("탐험 팝업");

            _app.Overlay.Close(); yield return Frames(1);
            yield return Shutdown();
        }

        /// <summary>ⓓⓔⓕ — 빠른 탐험 팝업(표 ㉖)과 로비 빨간 점.</summary>
        [UnityTest]
        public IEnumerator QuickExplorePopupAndLobbyRedDot()
        {
            yield return Boot();
            var D = _app.Data != null ? _app.Data.Expedition : null; Assert.IsNotNull(D);
            var S = _app.Save; S.MaxChapter = 10;
            S.ExpQuickDay = SaveStore.Today(); S.ExpQuickUsed = 0;
            _app.ShowScreen("lobby"); yield return Frames(2);

            // ⓔ 빨간 점 — 빠른 탐험 횟수가 남아 있으면 켜져 있다
            var dot = Find(_app.UiCanvas.transform, "ExpDot");
            Assert.IsNotNull(dot, "«탐험» 칸에 알림 점 조각이 있어야 한다");
            Assert.IsTrue(dot.gameObject.activeInHierarchy, "받을 게 있으면(빠른 탐험 횟수) 빨간 점이 켜진다");

            // ⓓ 빠른 탐험 팝업
            LobbyPopups.QuickExplore(_app, null); yield return Frames(2);
            var ov = _app.Overlay.Root;
            Assert.IsNotNull(Find(ov, "QuickExploreBox"), "빠른 탐험 상자");
            foreach (var n in new[] { "QxPlate", "QxSub", "QxTitle", "QxGridBg", "QxCellGold", "QxCellGem", "QxNote", "QxFreeBtn" })
                Assert.IsNotNull(Find(ov, n), "조각 " + n + " (표 ㉖)");
            Expedition.QuickReward(_app.Data, S, D, out double qg, out double qm);
            Assert.AreEqual(UiKit.Fmt(qg), CellQty(ov, "QxCellGold"), "빠른 탐험 골드 = 시간당 × quickHours");
            Assert.AreEqual(UiKit.FmtQty(qm), CellQty(ov, "QxCellGem"), "빠른 탐험 다이아");
            Assert.IsTrue(Find(ov, "QxFreeBtn").GetComponent<Button>().interactable, "횟수가 남으면 광고 버튼이 열린다");
            Assert.IsNotNull(Find(ov, "QxBadge"), "남은 횟수 배지");
            Assert.IsNull(EnglishLeftOver(ov), "영문 데모 글자 0 (T44)");
            _log.AssertNoRed("빠른 탐험 팝업");

            _app.Overlay.Close(); yield return Frames(1);
            yield return Shutdown();
        }
    }
}
