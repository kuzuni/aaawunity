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
    /// T363·T364 — 퀘스트 팝업(15)의 <b>목록 상자 알파</b>와 <b>«받을 것이 있다» 를 말하는 세 자리</b>(줄 차례 · 탭 점 · 버튼 점).
    /// <list type="bullet">
    /// <item><b>T363</b> — 목록 상자 바탕 알파 = 1/255(주인 «255 중에 1»). 0 이 아니라 1 이라 조각은 그 자리에 살아 있다.</item>
    /// <item><b>T364 ⓐ</b> — 업적 판은 «받을 수 있는 줄» 이 위로 온다. <b>수를 안 적고 규칙으로 잰다</b>: 화면을 위에서 훑을 때
    ///   «받을 수 있음» 이 한 번 false 가 되면 다시 true 가 되면 안 된다(안정 정렬이라 그 안의 차례는 표 그대로).</item>
    /// <item><b>T364 ⓑ</b> — 탭 점은 <b>그 탭</b>의 판정을 따른다(일일·주간은 그 판의 점수 트랙 · 업적은 업적 판정).</item>
    /// <item><b>T364 ⓒ</b> — 줄의 «받기» 버튼 점도 같은 판정 하나에서 나온다(옷·눌림·점이 갈라지면 «주황인데 안 눌리는» 자리가 생긴다).</item>
    /// </list>
    /// <para>⚠ <b>«켠 것만으로 출석 업적 하나가 받을 수 있다»</b>(App.Create → Quests.Login · T288-1 이 치른 값) — 그래서 이 자는
    /// «새 세이브면 받을 것이 없다» 를 절대 전제하지 않는다. 대신 <b>판정과 화면이 같은가</b>만 잰다.</para>
    /// </summary>
    public class QuestClaimDotTests
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
            _app = null;
            yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>그 자리(버튼·탭) 아래에 켜진 빨간 점이 있는가 — 이름은 세우는 쪽이 정한다(`ClaimDot`·`TabDot`).</summary>
        static bool HasDot(Transform t, string name)
        {
            if (t == null) return false;
            foreach (var k in t.GetComponentsInChildren<Transform>(true))
                if (k.name == name && k.gameObject.activeInHierarchy) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator 목록_상자는_거의_투명하다_255분의_1()
        {
            yield return Boot();
            LobbyPopups.Quest(_app);
            yield return Frames(1);

            int seen = 0;
            foreach (var t in _app.Overlay.Root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "ListBox") continue;
                var im = t.GetComponent<Image>(); if (im == null) continue;
                seen++;
                Assert.AreEqual(LobbyPopups.ListBoxAlpha, im.color.a, 1e-3,
                                "목록 상자 바탕은 알파 1/255(주인 2026-09-10 «255 중에 1»)");
            }
            Assert.GreaterOrEqual(seen, 1, "목록 상자가 한 개는 있어야 이 자가 뜻이 있다");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 업적판은_받을_수_있는_줄이_위로_오고_버튼에_점이_붙는다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Achievement : null;
            Assert.IsNotNull(d, "업적 표가 실려야 한다");

            // 표의 **마지막** 줄을 깬다 — 차례가 정말 바뀌는지 보려면 «원래 아래에 있던 줄» 이어야 한다.
            var last = d.List[d.List.Count - 1];
            Achievement.Add(_app.Save, last.Counter, Achievement.Goal(_app.Save, d, last.Counter));
            _app.Persist();
            Assert.IsTrue(Achievement.CanClaim(_app.Save, d, last.Counter), "깼으니 받을 수 있어야 한다(전제)");

            LobbyPopups.Achievements(_app);
            yield return Frames(1);
            var root = _app.Overlay.Root;

            // ⓐ 규칙 — 위에서 훑어 «받을 수 있음» 이 꺼진 뒤에 다시 켜지면 안 된다(받을 수 있는 것이 전부 위에 모여 있다).
            bool seenNotClaimable = false; int claimTop = 0; int lastRowIndex = -1;
            for (int i = 0; i < d.List.Count; i++)
            {
                var r = UiKit.Find(root, "Ach:" + i); Assert.IsNotNull(r, "줄 " + i);
                var title = UiKit.Find(r, "Title"); Assert.IsNotNull(title, "줄 " + i + " 의 제목");
                string label = title.GetComponent<TMP_Text>().text;
                var btn = UiKit.Find(r, "AchBtn"); Assert.IsNotNull(btn, "줄 " + i + " 의 «받기»");
                bool can = btn.GetComponent<Button>().interactable;   // 눌림 = CanClaim(AchievementTabTests 가 그 짝을 이미 못 박았다)

                if (can) { Assert.IsFalse(seenNotClaimable, "받을 수 있는 줄(" + label + ")이 못 받는 줄 아래에 있다"); claimTop++; }
                else seenNotClaimable = true;

                // ⓒ — 버튼 점은 «받을 수 있을 때만»
                Assert.AreEqual(can, HasDot(btn, "ClaimDot"), "«" + label + "» 의 받기 점은 받을 수 있을 때만 뜬다");
                if (label == last.Label) lastRowIndex = i;
            }
            Assert.GreaterOrEqual(claimTop, 1, "받을 수 있는 줄이 하나는 있어야 이 자가 뜻이 있다");
            Assert.Less(lastRowIndex, d.List.Count - 1, "표의 마지막 줄이 깼으니 제 자리보다 위로 올라와야 한다");

            // ⓑ — 업적 탭 점
            Assert.IsTrue(HasDot(UiKit.Find(root, "Tab:2"), "TabDot"), "받을 업적이 있으면 «업적» 탭에 점");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 다_받으면_점이_꺼지고_줄이_표_차례로_돌아간다()
        {
            yield return Boot();
            var d = _app.Data.Achievement;
            foreach (var row in d.List)
                while (Achievement.CanClaim(_app.Save, d, row.Counter))
                    Achievement.Claim(_app.Save, d, row.Counter, out _, out _);
            _app.Persist();

            LobbyPopups.Achievements(_app);
            yield return Frames(1);
            var root = _app.Overlay.Root;

            Assert.IsFalse(HasDot(UiKit.Find(root, "Tab:2"), "TabDot"), "받을 것이 없으면 탭 점은 꺼진다(T167 — «봤다» 칸이 없다)");
            for (int i = 0; i < d.List.Count; i++)
            {
                var r = UiKit.Find(root, "Ach:" + i);
                Assert.IsFalse(HasDot(UiKit.Find(r, "AchBtn"), "ClaimDot"), "줄 " + i + " 의 받기 점도 꺼진다");
                var title = UiKit.Find(r, "Title");
                Assert.AreEqual(d.List[i].Label, title.GetComponent<TMP_Text>().text, "받을 것이 없으면 줄은 표 차례 그대로다");
            }
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 일일_주간_탭_점은_그_판의_트랙을_따른다()
        {
            yield return Boot();
            var q = _app.Data != null ? _app.Data.Quest : null;
            Assert.IsNotNull(q, "퀘스트 표가 실려야 한다");

            // 일일 판의 줄을 전부 깨서 메달을 쌓는다 — 그러면 그 판의 첫 트랙 칸이 열린다(받을 수 있다).
            foreach (var quest in q.Daily.Quests) QuestRun.Bump(_app.Save, quest.Counter, quest.Goal);
            _app.Persist();
            Assert.IsTrue(QuestRun.AnyClaimable(_app.Save, q, true), "일일 트랙에 받을 칸이 생겨야 한다(전제)");

            LobbyPopups.Quest(_app, true);
            yield return Frames(1);
            var root = _app.Overlay.Root;
            Assert.IsTrue(HasDot(UiKit.Find(root, "Tab:0"), "TabDot"), "일일에 받을 것이 있으면 «일일» 탭에 점");
            Assert.AreEqual(QuestRun.AnyClaimable(_app.Save, q, false), HasDot(UiKit.Find(root, "Tab:1"), "TabDot"),
                            "«주간» 탭 점은 주간 판의 판정만 따른다(일일 것을 빌려 오지 않는다)");

            yield return Shutdown();
        }

        /// <summary>
        /// T359 — 포인트 트랙 넷(주인 2026-09-10). 재는 것은 <b>«화면이 규칙과 같은 말을 하는가»</b> 다:
        /// ⓒ 맨 왼쪽 메달 글자 = <see cref="QuestRun.Medal"/>(여태 «0» 이 글자 그대로 박혀 있었다) ·
        /// ⓓ <c>LineFill</c> 게이지가 0 에서 시작해 점수만큼 찬다 · ⓐ 받을 수 있는 칸에 점이 켜지고 <b>못 받는 칸에는 없다</b>.
        /// <para>⚠ «켜졌다» 만 재면 늘 켜져 있는 코드도 통과한다 — 그래서 **점수 0 인 판**을 먼저 재고 그 다음 쌓는다.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator 포인트_트랙이_지금_점수를_말한다()
        {
            yield return Boot();
            var q = _app.Data != null ? _app.Data.Quest : null;
            Assert.IsNotNull(q, "퀘스트 표가 실려야 한다");
            Assert.Greater(q.Daily.Steps.Count, 0, "일일 트랙에 칸이 있어야 이 자가 성립한다");

            // ── 점수 0 인 판 — 셈이 «지금 점수» 를 읽는지 보려면 «0 이 아닌 판» 만으로는 모자라다(0 이 박혀 있어도 통과하니까).
            LobbyPopups.Quest(_app, true); yield return Frames(1);
            var root = _app.Overlay.Root;
            int m0 = QuestRun.Medal(_app.Save, q.Daily, true);
            Assert.AreEqual(m0.ToString(), TrackScoreText(root), "맨 왼쪽 메달 글자 = 지금 점수(T359 ⓒ)");
            float f0 = Fill(root);
            Assert.GreaterOrEqual(f0, 0f, "게이지가 서 있어야 한다(T359 ⓓ)");
            _app.Overlay.Close(); yield return Frames(1);

            // ── 일일 줄을 전부 깨서 점수를 쌓는다.
            foreach (var quest in q.Daily.Quests) QuestRun.Bump(_app.Save, quest.Counter, quest.Goal);
            _app.Persist();
            int m1 = QuestRun.Medal(_app.Save, q.Daily, true);
            Assert.Greater(m1, m0, "쌓았으니 점수가 늘어야 한다(전제)");

            LobbyPopups.Quest(_app, true); yield return Frames(1);
            root = _app.Overlay.Root;
            Assert.AreEqual(m1.ToString(), TrackScoreText(root),
                            "점수가 늘면 메달 글자도 같이 는다 — 여기 «0» 이 박혀 있던 것이 주인이 짚은 그 자리다(T359 ⓒ)");
            int last = q.Daily.Steps[q.Daily.Steps.Count - 1].Points;
            Assert.AreEqual(Mathf.Clamp01(m1 / (float)last), Fill(root), 0.001f,
                            "게이지는 «지금 점수 ÷ 마지막 문턱» 만큼 찬다(T359 ⓓ)");
            Assert.Greater(Fill(root), f0, "점수가 늘었으니 게이지도 더 차 있어야 한다");

            // ── ⓐ 점은 «받을 수 있는 칸» 에만.
            int dots = 0, canCount = 0;
            for (int k = 0; k < q.Daily.Steps.Count; k++)
            {
                var cell = UiKit.Find(root, "Track:" + (k + 1)); if (cell == null) continue;
                bool can = QuestRun.CanClaim(_app.Save, q, true, k);
                if (can) canCount++;
                if (HasDot(cell, "TrackDot")) dots++;
                Assert.AreEqual(can, HasDot(cell, "TrackDot"),
                                "칸 " + k + ": 점은 «지금 받을 수 있는가»(QuestRun.CanClaim) 하나만 따른다 — 받는 쪽이 쓰는 그 판정이다(T359 ⓐ)");
            }
            Assert.Greater(canCount, 0, "이 판에는 받을 수 있는 칸이 있어야 한다(전제 · 없으면 위 단언이 전부 공허하다)");
            Assert.AreEqual(canCount, dots, "점 수 = 받을 수 있는 칸 수");

            _log.AssertNoRed("퀘스트 포인트 트랙");
            yield return Shutdown();
        }

        /// <summary>맨 왼쪽 메달 아래 숫자(트랙 숫자 줄의 첫 글자) — 화면이 그린 그대로 읽는다.</summary>
        static string TrackScoreText(Transform root)
        {
            var nums = UiKit.Find(root, "Nums");
            var t = nums != null ? nums.GetComponentInChildren<TMP_Text>(true) : null;
            return t != null ? t.text.Trim() : null;
        }

        /// <summary>
        /// T391(주인 2026-09-10 «퀘스트 전부 받는 버튼도 만들어 줘») — «전부 받기» 가 <b>받을 수 있는 트랙 칸을 하나도 안 남기고</b> 받는다.
        /// <para>
        /// ⚑ <b>버튼을 실제로 누른다</b> — `ClaimTrackStep` 을 직접 부르면 «버튼이 그 함수에 이어져 있는가» 를 안 재게 된다(T280 «화면이 스스로 열어야 한다»).
        /// ⚑ 그리고 <b>못 받을 때는 손잡이가 아예 없어야 한다</b> — 눌리는데 아무 일도 안 나는 것이 제일 나쁘다(결정 771).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator 퀘스트_전부_받기가_받을_수_있는_칸을_다_받는다()
        {
            yield return Boot();
            var q = _app.Data != null ? _app.Data.Quest : null;
            Assert.IsNotNull(q, "퀘스트 표가 실려야 한다");

            // ── ⓐ 아무것도 못 받는 새 판에서는 회색이고 «눌러도 아무 일 없는» 손잡이가 없다.
            LobbyPopups.Quest(_app, true); yield return Frames(1);
            var root0 = _app.Overlay.Root;
            var btn0 = UiKit.Find(root0, "QuestClaimAll");
            Assert.IsNotNull(btn0, "«전부 받기» 단추는 일일 판에 서 있어야 한다(T391)");
            Assert.IsFalse(QuestRun.AnyClaimable(_app.Save, q, true), "새 판은 받을 것이 없다(전제)");
            // ⚠ «손잡이가 없다» 로 재면 안 된다 — `UiKit.Clickable` 이 `Ensure<Button>` 이라 **늘 붙는다**.
            //   재야 하는 것은 «눌리는가» 다(결정 771 · 업적 줄의 «받기» 와 같은 꼴 · CI 런 996 이 이것을 잡았다).
            var b0 = btn0.GetComponent<Button>();
            Assert.IsNotNull(b0, "버튼 조각에는 Button 이 붙어 있다(UiKit.Clickable 의 계약)");
            Assert.IsFalse(b0.interactable, "받을 것이 없으면 안 눌린다(결정 771)");
            Assert.IsFalse(HasDot(btn0, "ClaimAllDot"), "받을 것이 없으면 점도 없다(T364 ⓒ)");
            _app.Overlay.Close(); yield return Frames(1);

            // ── ⓑ 일일 줄을 전부 깨면 트랙 칸 여럿이 열린다.
            foreach (var quest in q.Daily.Quests) QuestRun.Bump(_app.Save, quest.Counter, quest.Goal);
            _app.Persist();
            int canBefore = 0;
            for (int k = 0; k < q.Daily.Steps.Count; k++) if (QuestRun.CanClaim(_app.Save, q, true, k)) canBefore++;
            Assert.Greater(canBefore, 1, "이 자가 뜻이 있으려면 받을 칸이 둘 이상이어야 한다(전제 · 한 칸이면 «전부» 를 안 재는 셈이다)");

            LobbyPopups.Quest(_app, true); yield return Frames(1);
            var root = _app.Overlay.Root;
            var btn = UiKit.Find(root, "QuestClaimAll");
            Assert.IsNotNull(btn, "«전부 받기» 단추");
            Assert.IsTrue(HasDot(btn, "ClaimAllDot"), "받을 것이 있으면 빨간 점(T364 ⓒ)");
            var click = btn.GetComponent<Button>();
            Assert.IsNotNull(click, "«전부 받기» 에 손잡이");
            Assert.IsTrue(click.interactable, "받을 것이 있으면 눌린다");

            // ── ⓒ 눌러 본다 — 받을 수 있던 칸이 **하나도 안 남아야** 한다.
            click.onClick.Invoke(); yield return Frames(2);
            for (int k = 0; k < q.Daily.Steps.Count; k++)
                Assert.IsFalse(QuestRun.CanClaim(_app.Save, q, true, k),
                               "«전부 받기» 뒤에는 받을 수 있는 칸이 없어야 한다 — 칸 " + k + " 가 남았다");
            Assert.IsFalse(QuestRun.AnyClaimable(_app.Save, q, true), "그 판에 받을 것이 하나도 없다");
            yield return Shutdown();
        }

        /// <summary>게이지가 찬 정도 — 없으면 -1(그러면 위 단언이 바로 운다).</summary>
        static float Fill(Transform root)
        {
            var t = UiKit.Find(root, "LineFill");
            var im = t != null ? t.GetComponent<Image>() : null;
            return im != null ? im.fillAmount : -1f;
        }
    }
}
