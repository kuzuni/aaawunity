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
    /// T98 — 챕터 보상(Chapter Chest · 주인 2026-09-07 «로비 → 클리어 보상» · 레퍼런스 32).
    /// 규칙 자체는 EditMode <c>ChapterChestTests</c> 가 본다 — 여기서는 <b>화면</b>이다:
    /// ⓐ 로비 «클리어 보상» 이 페이지를 연다(빨간 점 포함) ⓑ 조각이 다 있다(리본·부제·배너 셋·보상 칸 2·받기·뒤로)
    /// ⓒ 목표 미달이면 «받기» 가 회색·안 눌린다 ⓓ 깬 챕터면 눌러서 재화가 늘고 두 번은 못 받는다 ⓔ 옆 챕터로 넘어간다
    /// ⓕ 빨간 줄 0(<see cref="PlayLog"/> · T11 규약).
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
        public IEnumerator ChapterChestPageClaimsOnceAndPagesBetweenChapters()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            Assert.IsNotNull(D.ChapterChest, "chapterChest.json 이 카탈로그(data.chapterChest)에서 로드돼야 한다");

            // 챕터 1~4 를 깬 상태 — 1~4 는 받을 수 있고 5 는 도전 중
            S.MaxChapter = 5; S.SelChapter = 5; S.Gem = 0; S.Gold = 0; _app.Persist();
            _app.ShowScreen("lobby"); yield return Frames(2);
            var lobby = _app.Current.Root;

            // ⓐ 로비 — 받을 게 있으니 «클리어 보상» 에 빨간 점 · 누르면 페이지가 열린다
            var dot = Find(lobby, "ChestDot");
            Assert.IsNotNull(dot, "«클리어 보상» 빨간 점 조각(T98 3항)");
            Assert.IsTrue(dot.gameObject.activeSelf, "받을 게 있으면 빨간 점이 켜진다");
            Assert.IsTrue(ClickNamed(lobby, "Side:" + LobbyScreen.SideClearReward), "클리어 보상 버튼");
            yield return Frames(2);
            Assert.AreEqual("chapterChest", _app.Current.Name, "챕터 보상 페이지가 열린다");
            var page = _app.Current.Root;

            // ⓑ 조각 — 리본·부제·배너 셋·보상 칸 2·받기·뒤로
            foreach (var n in new[] { "Ribbon", "Sub", "Banner", "Banner:prev", "Banner:next", "RewardBox", "Cell:gem", "Cell:gold", "ClaimBtn", "BackBtn" })
                Assert.IsNotNull(Find(page, n), "조각 " + n);

            // 받을 수 있는 가장 앞 챕터(1)부터 보여 준다 · 목표·보상이 규칙대로 찍힌다
            var info1 = ChapterChest.At(D, S, 1);
            Assert.AreEqual($"챕터 {info1.Chapter}", TextOf(Find(page, "Banner"), "BannerTitle"), "배너 제목");
            StringAssert.Contains(info1.Kills.ToString(), TextOf(page, "Sub"), "부제에 목표 적 수");
            Assert.AreEqual(UiKit.Fmt(info1.Gem), TextOf(Find(page, "Cell:gem"), "Qty"), "다이아 칸");
            Assert.AreEqual(UiKit.Fmt(info1.Gold), TextOf(Find(page, "Cell:gold"), "Qty"), "골드 칸");
            // 첫 챕터라 왼쪽 이웃은 감춰지고 오른쪽 이웃은 보인다
            Assert.IsFalse(Find(page, "Banner:prev").gameObject.activeSelf, "챕터 1 에서는 왼쪽 이웃이 없다");
            Assert.IsTrue(Find(page, "Banner:next").gameObject.activeSelf, "오른쪽 이웃은 보인다");

            // ⓓ 받기 — 재화가 규칙대로 늘고, 두 번째는 안 눌린다
            double gem0 = S.Gem, gold0 = S.Gold;
            Assert.IsTrue(ClickNamed(page, "ClaimBtn"), "«받기» 가 눌린다(깬 챕터)");
            yield return Frames(2);
            Assert.AreEqual(gem0 + info1.Gem, S.Gem, 1e-6, "다이아가 늘었다");
            Assert.AreEqual(gold0 + info1.Gold, S.Gold, 1e-6, "골드가 늘었다");
            Assert.IsTrue(S.ChestClaimed.Contains(1), "받았다고 남는다");
            Assert.AreEqual("받음", TextOf(page, "ClaimBtn"), "받은 뒤 버튼 글자");
            Assert.IsFalse(ClickNamed(page, "ClaimBtn"), "두 번째는 안 눌린다");
            Assert.AreEqual(gem0 + info1.Gem, S.Gem, 1e-6, "두 번 받아도 재화는 그대로");

            // ⓔ 옆 챕터로 — 오른쪽 이웃을 누르면 다음 챕터가 가운데로 온다
            Assert.IsTrue(ClickNamed(page, "Banner:next"), "오른쪽 이웃 배너");
            yield return Frames(2);
            Assert.AreEqual("챕터 2", TextOf(Find(page, "Banner"), "BannerTitle"), "옆 챕터로 넘어간다");

            // ⓒ 아직 못 깬 챕터(5) — «받기» 가 회색이고 안 눌린다
            for (int i = 0; i < 3; i++) { ClickNamed(page, "Banner:next"); yield return Frames(1); }
            Assert.AreEqual("챕터 5", TextOf(Find(page, "Banner"), "BannerTitle"), "도전 중인 챕터까지 넘어간다");
            Assert.IsFalse(Find(page, "Banner:next").gameObject.activeSelf, "그 뒤 챕터는 없다");
            Assert.IsFalse(ChapterChest.At(D, S, 5).Claimable, "도전 중인 챕터는 못 받는다");
            Assert.IsFalse(ClickNamed(page, "ClaimBtn"), "목표 미달이면 «받기» 가 안 눌린다");
            var cg = Find(page, "ClaimBtn").GetComponent<CanvasGroup>();
            Assert.IsNotNull(cg); Assert.Less(cg.alpha, 1f, "못 받으면 흐리다(레퍼런스 32 의 회색 Claim)");

            // 뒤로 → 로비 · 남은 챕터가 있으니 빨간 점은 그대로
            Assert.IsTrue(ClickNamed(page, "BackBtn"), "뒤로");
            yield return Frames(2);
            Assert.AreEqual("lobby", _app.Current.Name, "뒤로 = 로비");
            Assert.IsTrue(Find(_app.Current.Root, "ChestDot").gameObject.activeSelf, "2~4 가 남아 빨간 점은 켜져 있다");

            // 남은 것을 다 받으면 빨간 점이 꺼진다
            ChapterChest.Claim(D, S, 2, out _, out _); ChapterChest.Claim(D, S, 3, out _, out _); ChapterChest.Claim(D, S, 4, out _, out _);
            _app.ShowScreen("lobby"); yield return Frames(2);
            Assert.IsFalse(Find(_app.Current.Root, "ChestDot").gameObject.activeSelf, "다 받으면 빨간 점이 꺼진다");

            _log.AssertNoRed("챕터 보상 페이지(T98)");
            yield return Shutdown();
        }
    }
}
