using System;
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
    /// T23 — 쉼터 «광고 보고 둘 다 얻기» · 클리어 팝업 = 골드만 + «광고 보고 보상 ×2 받기» / «그냥 받기». 실제 씬의 전투에서 BattleScreen 배선을 그대로 지난다:
    /// ① 쉼터 보류 → BattleScreen.OpenPending 이 Rest 팝업(버튼 3개)을 연다 → 광고 버튼 → 카운트다운(«광고 시청 중...») → 회복 + 경험치 둘 다 · 보류 해제
    /// ② 보스 처치(Cleared) → EndRun → 클리어 팝업: 보상 칸은 골드 1개만 활성 · «다음 챕터» 없음 · «광고 보고 보상 ×2 받기» → 카운트다운 → 세이브 골드가 한 번 더 들어와 2배 · 로비
    /// ③ 다시 클리어 → «그냥 받기» → 1배 그대로 · 로비. 지점마다 빨간 줄 0(<see cref="PlayLog"/>).
    /// </summary>
    public class RestClearAdTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }
        IEnumerator UntilClosed(float maxSec, string what)
        {
            float t0 = Time.realtimeSinceStartup;
            while (_app.Overlay.IsOpen && Time.realtimeSinceStartup - t0 < maxSec) yield return null;
            Assert.IsFalse(_app.Overlay.IsOpen, what + " — 카운트다운 뒤 팝업이 닫혀야 한다");
        }
        static bool Click(Transform root, Func<string, bool> label)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(false))
                foreach (var t in b.GetComponentsInChildren<Text>(false))
                    if (label(t.text ?? "")) { b.onClick.Invoke(); return true; }
            return false;
        }
        bool HasText(Func<string, bool> pred) { foreach (var t in _app.UiCanvas.GetComponentsInChildren<Text>(false)) if (pred(t.text ?? "")) return true; return false; }

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

        [UnityTest]
        public IEnumerator RestAdGivesBothAndClearAdDoublesGold()
        {
            yield return Boot();
            _app.StartBattle(1); yield return Frames(2);
            var bs = _app.GetScreen<BattleScreen>(); var G = bs.G; Assert.IsNotNull(G, "전투 상태");

            // ① 쉼터 — 엔진 보류를 세우면 BattleScreen.Tick 이 OpenPending → Overlay.Rest(…, onBoth) 를 연다
            G.P.Hp = G.P.MaxHp * 0.5; double hp0 = G.P.Hp; int exp0 = G.P.Exp, lv0 = G.P.Level;
            G.Pending = new PendingDecision { Kind = PendingKind.Rest };
            yield return Frames(3);
            Assert.IsTrue(_app.Overlay.IsOpen, "쉼터 팝업");
            Assert.IsTrue(HasText(s => s.StartsWith("체력 회복")), "체력 회복 버튼"); Assert.IsTrue(HasText(s => s.StartsWith("경험치")), "경험치 버튼");
            Assert.IsTrue(HasText(s => s == "광고 보고 둘 다 얻기"), "T23 «광고 보고 둘 다 얻기» 버튼");
            _log.AssertNoRed("쉼터 팝업");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "광고 보고 둘 다 얻기"), "광고 버튼 클릭"); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen && HasText(s => s == "광고 시청 중..."), "광고 카운트다운(천사와 같은 AdCountdown)");
            Assert.IsNotNull(G.Pending, "카운트다운 동안은 아직 보류(시간 정지)");
            _log.AssertNoRed("쉼터 광고 카운트다운");
            yield return UntilClosed(6f, "쉼터 광고");
            yield return Frames(1);
            Assert.IsTrue(G.Pending == null || G.Pending.Kind == PendingKind.LevelUp, "쉼터 보류 해제(레벨업이 이어질 수는 있다)");
            Assert.Greater(G.P.Hp, hp0, "체력이 회복됐다");
            Assert.IsTrue(G.P.Level > lv0 || G.P.Exp >= exp0 + G.C.RestExp, "경험치도 받았다(둘 다)");
            _log.AssertNoRed("쉼터 둘 다 적용");
            if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; }

            // ② 클리어 → «광고 보고 보상 ×2 받기»
            G.Cleared = true; yield return Frames(4);
            float t0 = Time.realtimeSinceStartup; while (!_app.Overlay.IsOpen && Time.realtimeSinceStartup - t0 < 5f) yield return null;   // 타격 연출(Busy)이 끝나면 EndRun
            Assert.IsTrue(_app.Overlay.IsOpen, "클리어 팝업");
            Assert.IsTrue(HasText(s => s == "클리어!"), "제목");
            Assert.IsTrue(HasText(s => s == "광고 보고 보상 ×2 받기"), "«광고 보고 보상 ×2 받기» 버튼(프리팹 Get x2 자리)");
            Assert.IsTrue(HasText(s => s == "그냥 받기"), "«그냥 받기» 버튼(프리팹 Home 자리)");
            Assert.IsFalse(HasText(s => s == "다음 챕터"), "«다음 챕터» 버튼 없음");
            var items = UiKit.Find(_app.Overlay.Root, "Group_RewardItem"); Assert.IsNotNull(items, "Group_RewardItem");
            int activeCells = 0; foreach (Transform c in items) if (c.gameObject.activeSelf) activeCells++;
            Assert.AreEqual(1, activeCells, "보상 칸은 골드 1개만 보인다(나머지 칸은 끔)");
            double runGold = Math.Round(G.Gold); double bank1 = _app.Save.Gold;
            Assert.GreaterOrEqual(bank1, runGold, "1배는 팝업이 뜰 때 이미 은행에 들어가 있다");
            _log.AssertNoRed("클리어 팝업");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "광고 보고 보상 ×2 받기"), "×2 클릭"); yield return Frames(2);
            Assert.IsTrue(HasText(s => s == "광고 시청 중..."), "광고 카운트다운");
            yield return UntilClosed(6f, "클리어 광고");
            yield return Frames(2);
            Assert.AreEqual(bank1 + runGold, _app.Save.Gold, 0.5, "광고 뒤 이 판의 골드가 한 번 더 들어와 2배");
            Assert.AreEqual("lobby", _app.Current.Name, "×2 받고 로비로");
            var stored = SaveData.FromJson(PlayerPrefs.GetString(SaveStore.Key, null), _app.Data);
            Assert.AreEqual(_app.Save.Gold, stored.Gold, 0.5, "세이브에 기록");
            _log.AssertNoRed("×2 → 로비");

            // ③ 다시 클리어 → «그냥 받기» = 1배
            _app.StartBattle(1); yield return Frames(2);
            bs = _app.GetScreen<BattleScreen>(); G = bs.G; Assert.IsNotNull(G);
            double bank0 = _app.Save.Gold;
            G.Cleared = true; yield return Frames(4);
            t0 = Time.realtimeSinceStartup; while (!_app.Overlay.IsOpen && Time.realtimeSinceStartup - t0 < 5f) yield return null;
            Assert.IsTrue(_app.Overlay.IsOpen && HasText(s => s == "클리어!"), "둘째 클리어 팝업");
            double runGold2 = Math.Round(G.Gold);
            Assert.AreEqual(bank0 + runGold2, _app.Save.Gold, 0.5, "1배 은행");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "그냥 받기"), "그냥 받기 클릭"); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen); Assert.AreEqual("lobby", _app.Current.Name, "그냥 받기 → 로비");
            Assert.AreEqual(bank0 + runGold2, _app.Save.Gold, 0.5, "그냥 받기는 1배 그대로");
            _log.AssertNoRed("그냥 받기 → 로비");
        }
    }
}
