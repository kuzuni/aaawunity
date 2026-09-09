using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    /// T300 — <b>플레이 봇</b>(주인 2026-09-09 «플레이해서 에러 테스트도 하라»). 각본은 <see cref="Playthrough.Stages"/> 한 벌이고
    /// 단계마다 <b>독립 자</b>다 — 하나가 죽어도 나머지는 돌고, <c>[CI실패]</c> 에 «어느 단계» 인지 이름으로 뜬다(1항).
    /// <para>
    /// ⚠ <b>봇이 재는 것은 셋뿐</b>(3항 ⓐ): 죽지 않고 지나가는가 · 빨간 줄 0 · 도달했는가.
    /// «값이 맞는가» 는 각 절의 자 몫이다 — 여기에 값 단언을 얹으면 표가 바뀌는 날 봇이 먼저 운다.
    /// </para>
    /// <para>
    /// ⚠ <b>봇이 빨개지면 봇을 고치지 않는다</b>(6항) — 그 단계의 에러를 새 번호로 등재한다(T288 방식).
    /// 각본 자체가 틀린 것이 아니라면, 초록으로 만드는 것은 «찾은 고장을 덮는 것» 이다.
    /// </para>
    /// </summary>
    public class PlaythroughTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        /// <summary>배치 모드에는 GameView 가 없어 <see cref="HeroView"/> 카메라가 저절로 안 그려진다 — 손으로 한 번씩 그린다(T12 idiom).</summary>
        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        void Tap(Transform root, string name)
        {
            var t = UiKit.Find(root, name); Assert.IsNotNull(t, "누를 자리 " + name);
            var b = t.GetComponent<Button>(); Assert.IsNotNull(b, name + " 은 눌리는 것이어야 한다");
            Assert.IsTrue(b.interactable, name + " 이 잠겨 있으면 각본이 못 지나간다");
            b.onClick.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // P1 로비 — START 가 있고, 하단 탭 다섯을 왕복하고, 챕터 ◀▶ 를 눌러도 죽지 않는가.
        // ─────────────────────────────────────────────────────────────────────────────
        /// <summary>P1 로비(T300 1항) — 노는 것: 탭 다섯 왕복 · 챕터 ◀▶. 재는 것: 이름 계약 · HeroView ≥ 1 · 빨간 줄 0.</summary>
        [UnityTest]
        public IEnumerator P1_로비를_돌아다녀도_죽지_않는다()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(3);
            var lobby = _app.Current.Root;

            // 도달 — 로비의 이름 계약(이것이 없으면 아래 «놀기» 가 헛돈다)
            Assert.IsNotNull(UiKit.Find(lobby, "Start"), "로비 START");
            Assert.IsNotNull(UiKit.Find(lobby, "ChapterCard"), "챕터 카드");
            Assert.Greater(UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length, 0,
                "로비에 플레이어 초상(HeroView)이 있다 — 주인이 바로 보는 자리다");

            // 놀기 ⓐ 하단 탭 다섯을 한 바퀴 돌고 로비로 돌아온다
            foreach (var key in NavBar.Keys)
            {
                Tap(_app.Current.Root, NavBar.TabName(key)); yield return Frames(3);
                Assert.IsNotNull(_app.Current, "탭 «" + key + "» 뒤에도 화면이 서 있다");
                _log.AssertNoRed("P1 탭 " + key);
            }
            _app.ShowScreen("lobby"); yield return Frames(3);
            lobby = _app.Current.Root;

            // 놀기 ⓑ 챕터 ◀▶ — 경계(1챕터)에서 왼쪽을 눌러도 죽지 않아야 한다(Clamp)
            for (int i = 0; i < 3; i++) { Tap(lobby, "ArrowR"); yield return Frames(1); }
            for (int i = 0; i < 5; i++) { Tap(lobby, "ArrowL"); yield return Frames(1); }
            Assert.GreaterOrEqual(_app.Save.SelChapter, 1, "챕터는 1 아래로 안 내려간다");

            _log.AssertNoRed("P1 로비");
            yield return Shutdown();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // 각본 ↔ 자 대조 — 목록과 실제 자가 어긋나면 «봇이 도는 줄 알았는데 안 노는» 단계가 생긴다(4항).
        // ─────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// T300 4항 — <b>각본 목록과 이 파일의 자가 짝이 맞는가.</b>
        /// <para>
        /// ⚠ 지금은 «없는 단계» 를 빨강으로 세우지 <b>않는다</b> — P2~P11 이 아직 안 쓰였고, 그것을 빨강으로 두면
        /// 이 절이 끝날 때까지 CI 가 계속 빨개서 <b>진짜 빨강이 안 보인다</b>. 대신 두 가지를 잰다:
        /// ⓐ <b>목록에 없는 자가 있으면 빨강</b>(오타·유령 단계) ⓑ 아직 없는 단계는 <b>로그로 이름을 부른다</b>.
        /// </para>
        /// <para><b>P11 이 들어오는 커밋에서</b> ⓑ 를 «전부 있다» 단언으로 올린다 — 그 한 줄이 이 절의 마지막 일이다(절 4항에 적어 뒀다).</para>
        /// </summary>
        [Test]
        public void 각본_목록과_자가_짝이_맞는다()
        {
            var ids = new HashSet<string>();
            foreach (var s in Playthrough.Stages) ids.Add(s.Id);
            Assert.AreEqual(11, Playthrough.Stages.Length, "각본은 11단계다(T300 1항 표)");

            var written = new HashSet<string>();
            foreach (var m in typeof(PlaythroughTests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                int us = m.Name.IndexOf('_');
                if (us <= 0 || m.Name[0] != 'P') continue;
                string id = m.Name.Substring(0, us);
                bool numbered = id.Length > 1;
                for (int i = 1; i < id.Length && numbered; i++) numbered = char.IsDigit(id[i]);
                if (!numbered) continue;
                Assert.IsTrue(ids.Contains(id), "자 «" + m.Name + "» 이 각본에 없는 단계를 부른다 — 목록(Playthrough.Stages)과 이름을 맞춘다");
                written.Add(id);
            }
            Assert.Greater(written.Count, 0, "단계 자가 하나도 없다");

            var missing = new List<string>();
            foreach (var s in Playthrough.Stages) if (!written.Contains(s.Id)) missing.Add(s.ToString());
            if (missing.Count > 0)
                Debug.Log("[T300] 아직 안 쓴 단계 " + missing.Count + "개 — " + string.Join(" · ", missing.ToArray())
                          + " (P11 이 들어오는 커밋에서 이 로그를 «전부 있다» 단언으로 올린다)");
        }

        /// <summary>배포 스모크가 세는 줄의 꼴(T300 2항) — 그 글자를 <c>webgl_smoke.js</c> 가 문자열로 찾으므로 여기서 못 박는다.</summary>
        [Test]
        public void 스모크가_세는_줄의_꼴이_고정이다()
        {
            Assert.AreEqual("[KkomaKnight] play P1 ok", Playthrough.Line("P1", true));
            StringAssert.StartsWith("[KkomaKnight] play P5 fail ", Playthrough.Line("P5", false, "티켓 0"));
            Assert.IsTrue(Playthrough.TryFind("P11", out var last) && last.Name == "설정", "번호로 단계를 찾는다");
            Assert.IsFalse(Playthrough.TryFind("P12", out _), "없는 번호는 못 찾는다고 말한다");
        }
    }
}
