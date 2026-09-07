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
    /// T137 — 챕터 보상(Chapter Chest · 주인 2026-09-07 «챕터 보상은 챕터당 3개 … 받으면 옆으로 스크롤»).
    /// 규칙 자체는 EditMode <c>ChapterChestTests</c> 가 본다 — 여기서는 <b>화면</b>이다:
    /// ⓐ 로비 «클리어 보상» 이 페이지를 연다(빨간 점 포함) ⓑ 조각이 다 있다(리본·부제·배너 셋·보상 칸 2·받기·뒤로)
    /// ⓒ «받기» → 배너가 <b>다음 단</b>으로 넘어가고 버튼이 다시 살아난다 ⓓ 마지막 단을 받으면 빨간 점이 꺼진다
    /// ⓔ 처치 미달이면 «받기» 가 회색·안 눌린다 ⓕ 빨간 줄 0(<see cref="PlayLog"/> · T11 규약).
    /// </summary>
    public class ChapterChestScreenTests
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
        static bool ClickNamed(Transform root, string name)
        {
            var t = Find(root, name); var b = t != null ? t.GetComponent<Button>() : null;
            if (b == null || !b.IsInteractable()) return false;
            b.onClick.Invoke(); return true;
        }
        static string TextOf(Transform root, string name)
        {
            var t = Find(root, name); var x = t != null ? t.GetComponentInChildren<Text>(true) : null;
            return x != null ? x.text : null;
        }

        [UnityTest]
        public IEnumerator ClaimingAStepScrollsToTheNextRewardAndTheLastOneTurnsTheDotOff()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            Assert.IsNotNull(D.ChapterChest, "chapterChest.json 이 카탈로그(data.chapterChest)에서 로드돼야 한다");
            int steps = D.ChapterChest.Steps;
            Assert.AreEqual(3, steps, "주인 지시 = 챕터당 3개");

            // 챕터 1 을 깬 상태(= 전멸로 친다) · 챕터 2 는 한 마리도 안 잡았다
            S.MaxChapter = 2; S.SelChapter = 2; S.Gem = 0; S.Gold = 0; S.ChestClaimed.Clear(); S.ChestKills.Clear(); _app.Persist();
            _app.ShowScreen("lobby"); yield return Frames(2);
            var lobby = _app.Current.Root;

            // ⓐ 로비 — 받을 게 있으니 «클리어 보상» 에 빨간 점 · 누르면 페이지가 열린다
            var dot = Find(lobby, "ChestDot");
            Assert.IsNotNull(dot, "«클리어 보상» 빨간 점 조각");
            Assert.IsTrue(dot.gameObject.activeSelf, "받을 게 있으면 빨간 점이 켜진다");
            Assert.IsTrue(ClickNamed(lobby, "Side:" + LobbyScreen.SideClearReward), "클리어 보상 버튼");
            yield return Frames(2);
            Assert.AreEqual("chapterChest", _app.Current.Name, "챕터 보상 페이지가 열린다");
            var page = _app.Current.Root;

            // ⓑ 조각 — 리본·부제·배너 줄·배너 셋·보상 칸 2·받기·뒤로
            foreach (var n in new[] { "Ribbon", "Sub", "BannerRow", "Banner", "Banner:prev", "Banner:next", "RewardBox", "Cell:gem", "Cell:gold", "ClaimBtn", "BackBtn" })
                Assert.IsNotNull(Find(page, n), "조각 " + n);

            // 받을 수 있는 첫 «단»(챕터 1 · 1단)부터 · 목표는 «적 A/B 처치» · 보상은 단마다 같은 값
            var first = ChapterChest.At(D, S, 1, 1);
            Assert.AreEqual("챕터 1", TextOf(Find(page, "Banner"), "BannerTitle"), "배너 제목");
            StringAssert.Contains($"{first.Kills}/{first.Goal}", TextOf(Find(page, "Banner"), "BannerGoal"), "목표 = 적 A/B 처치");
            Assert.AreEqual(UiKit.Fmt(D.ChapterChest.Gem), TextOf(Find(page, "Cell:gem"), "Qty"), "다이아 칸");
            Assert.AreEqual(UiKit.Fmt(D.ChapterChest.Gold), TextOf(Find(page, "Cell:gold"), "Qty"), "골드 칸");
            Assert.IsFalse(Find(page, "Banner:prev").gameObject.activeSelf, "첫 칸에서는 왼쪽 이웃이 없다");
            Assert.IsTrue(Find(page, "Banner:next").gameObject.activeSelf, "오른쪽 이웃(다음 보상)은 보인다");

            // ⓒ 받기 → 재화가 늘고, 배너가 다음 «단» 으로 넘어가고, 버튼이 다시 살아난다
            for (int st = 1; st <= steps; st++)
            {
                double gem0 = S.Gem, gold0 = S.Gold;
                StringAssert.Contains($"{st}/{steps}", TextOf(page, "Sub"), "부제에 지금 단");
                Assert.AreEqual("받기", TextOf(page, "ClaimBtn"), "아직 안 받은 단");
                Assert.IsTrue(ClickNamed(page, "ClaimBtn"), "«받기» 가 눌린다(단 " + st + ")");
                yield return Frames(2);
                Assert.AreEqual(gem0 + D.ChapterChest.Gem, S.Gem, 1e-6, "다이아가 늘었다");
                Assert.AreEqual(gold0 + D.ChapterChest.Gold, S.Gold, 1e-6, "골드가 늘었다");
                Assert.IsTrue(ChapterChest.ClaimedStep(S, 1, st), "받았다고 남는다");
                Assert.AreEqual("챕터 1", TextOf(Find(page, "Banner"), "BannerTitle"), "아직 챕터 1 안이다");
                if (st < steps) StringAssert.Contains($"{st + 1}/{steps}", TextOf(page, "Sub"), "받으면 옆으로 — 다음 보상이 가운데로");
            }
            Assert.AreEqual(D.ChapterChest.Gem * steps, S.Gem, 1e-6, "한 챕터 다 받으면 다이아 300");
            Assert.AreEqual(D.ChapterChest.Gold * steps, S.Gold, 1e-6, "한 챕터 다 받으면 골드 3000");

            // 마지막 단을 받으면 다음 칸(챕터 2 · 1단)으로 넘어간다 — 한 마리도 안 잡았으니 회색
            Assert.AreEqual("챕터 2", TextOf(Find(page, "Banner"), "BannerTitle"), "다음 챕터의 첫 단으로 넘어간다");
            Assert.IsFalse(ClickNamed(page, "ClaimBtn"), "ⓔ 처치 미달이면 «받기» 가 안 눌린다");
            var cg = Find(page, "ClaimBtn").GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg); Assert.Less(cg.alpha, 1f, "못 받으면 흐리다(레퍼런스 32 의 회색 Claim)");

            // 이전 보상으로도 돌아간다(같은 «옆으로» 이동) — 이미 받은 단이라 «받음»
            Assert.IsTrue(ClickNamed(page, "Banner:prev"), "왼쪽 이웃 배너");
            yield return Frames(2);
            Assert.AreEqual("챕터 1", TextOf(Find(page, "Banner"), "BannerTitle"), "이전 보상으로 돌아온다");
            Assert.AreEqual("받음", TextOf(page, "ClaimBtn"), "이미 받은 단");

            // ⓓ 받을 게 없으면 빨간 점이 꺼진다
            Assert.IsTrue(ClickNamed(page, "BackBtn"), "뒤로");
            yield return Frames(2);
            Assert.AreEqual("lobby", _app.Current.Name, "뒤로 = 로비");
            Assert.IsFalse(ChapterChest.AnyClaimable(D, S), "챕터 2 는 한 마리도 안 잡았다");
            Assert.IsFalse(Find(_app.Current.Root, "ChestDot").gameObject.activeSelf, "다 받으면 빨간 점이 꺼진다");

            // 져도 남는 진행도 — 챕터 2 에서 1/3 을 잡으면 1단이 열리고 점이 다시 켜진다
            ChapterChest.RecordKills(S, 2, ChapterChest.Goal(D, 2, 1));
            _app.ShowScreen("lobby"); yield return Frames(2);
            Assert.IsTrue(Find(_app.Current.Root, "ChestDot").gameObject.activeSelf, "1/3 을 채우면 빨간 점이 켜진다");

            _log.AssertNoRed("챕터 보상 페이지(T137)");
            yield return Shutdown();
        }
    }
}
