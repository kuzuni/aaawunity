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
        /// <summary>P1 로비(T300 1항) — 노는 것: 탭 다섯 왕복 · 챕터 ◀▶. 재는 것: 이름 계약 · 아바타 칸의 초상 · 빨간 줄 0.</summary>
        [UnityTest]
        public IEnumerator P1_로비를_돌아다녀도_죽지_않는다()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(3);
            var lobby = _app.Current.Root;

            // 도달 — 로비의 이름 계약(이것이 없으면 아래 «놀기» 가 헛돈다)
            Assert.IsNotNull(UiKit.Find(lobby, "Start"), "로비 START");
            Assert.IsNotNull(UiKit.Find(lobby, "ChapterCard"), "챕터 카드");
            // T300-p1 — 여기 있던 «로비에 `HeroView` 가 ≥ 1» 은 **주인이 지우라고 한 것을 단언하고 있었다**(런 711 빨강 1건).
            //   `Screens.cs:453` — **T262 ⓐ**(주인 2026-09-09 «플레이어 이미지 말고»)가 로비 아바타의 `HeroView`(내 캐릭터를
            //   실시간으로 그린 초상)를 **프로필에서 고른 초상 아이콘**(`Profile.Face`)으로 갈아 끼웠다. 즉 로비에 그것이 **없는 것이
            //   지금 옳은 상태**이고, 이 단언은 로비가 옳을수록 빨개진다. 재려던 것(«주인이 바로 보는 자리»)은 살리고
            //   **재는 대상만 실제로 서 있는 것**으로 바꾼다 — 아바타 칸의 초상(`Profile.FaceName`)이 그 자리다.
            //   ⚠ `FindObjectsByType` 는 **씬 전체**를 뒤진다 — 이름이 «로비에 …» 여도 로비 안인지는 안 본다.
            //      화면 하나를 재려면 그 화면의 루트에서 찾아야 한다(로비 밖 `HeroView` 가 켜져 있으면 통과해 버린다).
            //   ⚠ **그리고 이 고침은 이미 한 번 있었다** — `HeroViewTests` 가 같은 줄을 T262 ⓐ 때 같은 까닭으로 이미 갈아 끼웠다.
            //      그래서 새로 짓지 않고 **그 자의 꼴을 그대로** 쓴다(칸을 집고 → 그 안의 초상 → 그 자리에 `HeroView` 는 없다).
            //      `check_stale_asserts` 는 이 부류를 못 잡는다 — 그 자는 **이 diff 가 지운 값**을 보는데, 여기서는
            //      «이미 지워진 것을 **새로 단언**» 했다. 지운 쪽이 아니라 **더한 쪽**이라 자의 눈 밖이다.
            {
                var av = UiKit.Find(lobby, "Avatar"); Assert.IsNotNull(av, "로비 상단 바 아바타 칸");
                Assert.IsNotNull(UiKit.Find(av, Profile.FaceName),
                                 "아바타 칸에 내 초상이 서 있다 — 주인이 바로 보는 자리다(T262 ⓐ 뒤로 그것은 프로필 초상 아이콘이다)");
                Assert.IsNull(av.GetComponentInChildren<HeroView>(true), "내 캐릭터 그림은 아바타 자리에 없다(주인 «플레이어 이미지 말고»)");
            }

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
        // P2 전투 — 판을 굴리고 · 특전을 고르고 · 이벤트 셋을 지나고 · 죽고 부활하고 또 죽고 포기하고 · 이겨서 결과 화면까지.
        // ─────────────────────────────────────────────────────────────────────────────
        /// <summary>화면이 <b>스스로</b> 팝업을 열 때까지 기다린다 — 손으로 <c>Overlay.…</c> 를 부르면 배선이 끊겨 있어도 초록이다(T280 이 값 주고 세운 규칙).</summary>
        IEnumerator UntilOpen(float maxSec, string what)
        {
            float t0 = Time.realtimeSinceStartup;
            while (!_app.Overlay.IsOpen && Time.realtimeSinceStartup - t0 < maxSec) yield return null;
            Assert.IsTrue(_app.Overlay.IsOpen, what + " — 화면이 스스로 열어야 한다");
            UiKit.CompleteAllTweens();
            yield return Frames(2);
        }
        IEnumerator UntilClosed(float maxSec, string what)
        {
            float t0 = Time.realtimeSinceStartup;
            while (_app.Overlay.IsOpen && Time.realtimeSinceStartup - t0 < maxSec) yield return null;
            Assert.IsFalse(_app.Overlay.IsOpen, what + " — 고르고 나면 팝업이 닫혀야 한다");
        }
        /// <summary>글자로 버튼을 찾아 누른다(<c>RestClearAdTests</c> 의 꼴 그대로 — 이벤트 팝업들은 이름이 아니라 글자로 갈린다).</summary>
        static bool Click(Transform root, Func<string, bool> label)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(false))
                foreach (var t in b.GetComponentsInChildren<TMP_Text>(false))
                    if (label(t.text ?? "")) { b.onClick.Invoke(); return true; }
            return false;
        }

        /// <summary>
        /// P2 전투(T300 1항) — 노는 것: 배속 3 으로 판을 굴리고 · 특전 3택에서 하나 고르고 · 쉼터(광고)·천사·악마를 지나고 ·
        /// 죽고 부활하고 또 죽고 포기하고 · 다시 이겨서 결과 화면까지. 재는 것: <b>도달 · 팝업이 스스로 열고 닫힘 · 빨간 줄 0</b>.
        /// <para>
        /// ⚠ <b>표의 «잰다» 칸(«골드·경험치가 늘었다»)을 그대로 안 옮겼다</b> — 절 3항 ⓐ 와 이 파일 머리가 «봇은 규칙을 확인하지 않는다» 로
        /// 못 박고 있고, P1 도 그렇게 섰다. 값을 여기서 재면 밸런스 회차(T325)가 표를 바꾸는 날 <b>봇이 먼저 운다</b> —
        /// 그때 빨개지는 것은 «놀 수 없게 됐다» 가 아니라 «수가 달라졌다» 라, 이 자가 잡으려던 고장이 그 빨강에 묻힌다.
        /// 대신 <b>«누른 것이 게임에 닿았는가»</b> 는 잰다(특전이 <c>Taken</c> 에 붙는다 · 부활 횟수가 1 이 된다) —
        /// 그것은 값이 아니라 <b>배선</b>이고, 끊기면 팝업은 그대로 열리고 닫히므로 다른 자는 아무도 안 운다(T280 이 부활 버튼에서 밝힌 그 자리).
        /// </para>
        /// <para>
        /// ⚠ <b>이 단계는 «화면 이름 계약» 을 거의 안 잰다</b> — 지금 전투 HUD 는 주인 지시(T3xx)로 자주 바뀌는 중이고,
        /// 이름 하나가 바뀔 때마다 봇이 빨개지면 이 절이 잡으려는 진짜 고장이 그 빨강에 묻힌다(P1 이 런 711 에서 값 주고 배운 것).
        /// 봇이 붙잡는 것은 «판이 열리고(<c>Current.Name</c>) 끝까지 지나가는가» 다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator P2_전투를_한_판_놀아도_죽지_않는다()
        {
            yield return Boot();
            var S = _app.Save; S.Revive = 1; _app.Persist();   // 각 단계가 제 조건을 만들어 시작한다(1항) — 부활권 하나

            _app.StartBattle(1); yield return Frames(2);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            Assert.AreEqual("battle", _app.Current.Name, "도달 — «도전» 이 전투 화면을 연다");
            _log.AssertNoRed("P2 판 열기");

            // ⓐ 실제로 굴린다(배속 3 · 실제 1.5초) — 여기서 죽는 것은 ⓓ 의 몫이라 체력을 받쳐 준다.
            //    ⚠ 엔진이 스스로 레벨업 팝업을 열 수도 있다 — 그러면 거기서 멈추고 아래에서 정리한다(그것도 «지나간» 것이다).
            Time.timeScale = 3f;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 1.5f && !G.Over && !_app.Overlay.IsOpen)
            {
                if (G.P.Hp < G.P.MaxHp * 0.5) G.P.Hp = G.P.MaxHp;
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.Greater(G.T, 0.0, "엔진이 실제로 돌았다 — 이 한 줄이 없으면 아래 전부가 «판을 안 굴린 채» 통과한다");
            if (_app.Overlay.IsOpen) { _app.Overlay.Close(); yield return Frames(1); }
            // ⚠ 엔진이 쌓아 둔 레벨업(`PendingLevelUps`)까지 비운다 — 안 비우면 아래 각본 «사이»에 팝업 하나가 저 혼자 끼어들고,
            //    그러면 봇이 «어느 팝업의 버튼을 눌렀는지» 가 흐려진다. 봇은 각본 순서대로 놀아야 잡은 고장을 이름으로 말할 수 있다.
            G.Pending = null; G.PendingLevelUps = 0;
            // 짧은 챕터라 그 사이에 판이 끝났으면 새 판으로 이어 간다 — «끝난 판» 위에 아래 각본을 얹으면 재는 것이 달라진다.
            if (G.Over)
            {
                _app.StartBattle(1); yield return Frames(2);
                bs = _app.GetScreen<BattleScreen>(); G = bs.G; Assert.IsNotNull(G, "이어 붙인 판");
            }
            _log.AssertNoRed("P2 판 굴리기");

            // ⓑ 특전 3택 — 화면이 스스로 열고(`BattleScreen.OpenPending`), 카드를 누르면 그 특전이 실제로 붙는다.
            var rng = new Mulberry32(7u);
            var offer = Perks.Offer(_app.Data, G.Taken, false, rng);
            Assert.Greater(offer.Count, 0, "특전 제안이 하나는 있다");
            G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
            yield return UntilOpen(5f, "특전 3택 팝업");
            var cards = UiKit.Find(_app.Overlay.Root, "Group_Card");
            Assert.IsNotNull(cards, "3택 카드 담개(Group_Card)");
            Assert.Greater(cards.childCount, 0, "고를 카드가 있다");
            var cardBtn = cards.GetChild(0).GetComponent<Button>();
            Assert.IsNotNull(cardBtn, "카드는 눌리는 것이어야 한다");
            int taken0 = G.Taken.Count;
            cardBtn.onClick.Invoke(); yield return Frames(2);
            yield return UntilClosed(3f, "특전 3택");
            Assert.Greater(G.Taken.Count, taken0, "고른 특전이 실제로 붙었다 — 카드가 «그림» 이 아니라 «길» 인가는 여기서만 갈린다");
            G.Pending = null; G.PendingLevelUps = 0;
            _log.AssertNoRed("P2 특전 3택");

            // ⓒ 이벤트 — 쉼터(광고 카운트다운까지) · 천사 · 악마(수락 → 선물 «계속»)
            G.P.Hp = G.P.MaxHp * 0.5;
            G.Pending = new PendingDecision { Kind = PendingKind.Rest };
            yield return UntilOpen(5f, "쉼터 팝업");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "광고 보고 둘 다 얻기"), "쉼터 «광고 보고 둘 다 얻기»");
            yield return Frames(2);
            yield return UntilClosed(8f, "쉼터 광고");   // AdCountdown 3초 — 봇이 지나가는 유일한 «광고» 자리다
            if (_app.Overlay.IsOpen) { _app.Overlay.Close(); yield return Frames(1); }   // 쉼터 뒤에 레벨업이 이어 뜰 수 있다
            G.Pending = null; G.PendingLevelUps = 0;
            _log.AssertNoRed("P2 쉼터+광고");

            G.Pending = new PendingDecision { Kind = PendingKind.Angel };
            yield return UntilOpen(5f, "천사 팝업");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s.StartsWith("무료 축복")), "천사 «무료 축복»");
            yield return Frames(2);
            yield return UntilClosed(3f, "천사");
            G.Pending = null; G.PendingLevelUps = 0;
            _log.AssertNoRed("P2 천사");

            var devilPerk = Perks.OfferDevil(_app.Data, G.Taken, rng);
            if (devilPerk != null)   // 줄 특전이 남아 있을 때만 악마가 나온다 — 없으면 «지어내지 않고» 이 조각을 건너뛴다
            {
                G.Pending = new PendingDecision { Kind = PendingKind.Devil, DevilPerk = devilPerk };
                yield return UntilOpen(5f, "악마 팝업");
                Assert.IsTrue(Click(_app.Overlay.Root, s => s == "거래 수락"), "악마 «거래 수락»");
                yield return Frames(2);
                // 수락하면 «선물» 팝업이 이어 뜬다 — 그 «계속» 까지 눌러야 판으로 돌아온다(둘을 한 짝으로 안 보면 다음 조각이 엉킨다)
                if (_app.Overlay.IsOpen)
                {
                    Assert.IsTrue(Click(_app.Overlay.Root, s => s == "계속"), "악마 선물 «계속»");
                    yield return Frames(2);
                    yield return UntilClosed(3f, "악마 선물");
                }
                G.Pending = null; G.PendingLevelUps = 0;
                _log.AssertNoRed("P2 악마");
            }

            // ⓓ 죽는다 → 부활 → 다시 죽는다 → 포기
            G.P.Hp = 0; G.Dead = true;
            yield return UntilOpen(10f, "사망 팝업");
            var revive = UiKit.Find(_app.Overlay.Root, "ReviveBtn");
            Assert.IsNotNull(revive, "부활권 1 개 · 이 판 첫 죽음이면 자리가 있다(T254)");
            var rb = revive.GetComponent<Button>(); Assert.IsNotNull(rb, "부활 버튼");
            rb.onClick.Invoke(); yield return Frames(2);
            Assert.IsFalse(G.Dead, "부활 — 판이 이어진다");
            Assert.AreEqual(1, bs.RevivesUsed, "이 판의 부활 횟수가 1 이 된다");
            _log.AssertNoRed("P2 부활");

            G.P.Hp = 0; G.Dead = true;
            yield return UntilOpen(10f, "두 번째 사망 팝업");
            Assert.IsNull(UiKit.Find(_app.Overlay.Root, "ReviveBtn"), "한 판에 한 번 — 두 번째 죽음에는 자리 자체가 없다");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "로비로"), "«로비로»(= 포기)");
            yield return Frames(3);
            Assert.AreEqual("lobby", _app.Current.Name, "포기하면 로비로 돌아온다");
            _log.AssertNoRed("P2 죽음 → 부활 → 죽음 → 포기");

            // ⓔ 이겨서 결과 화면(res_win)까지 한 번 — «그냥 받기» 로 로비까지 돌아온다
            _app.StartBattle(1); yield return Frames(2);
            bs = _app.GetScreen<BattleScreen>(); G = bs.G; Assert.IsNotNull(G, "둘째 판");
            G.Cleared = true;
            yield return UntilOpen(8f, "클리어 팝업");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "그냥 받기"), "«그냥 받기»");
            yield return Frames(3);
            Assert.AreEqual("lobby", _app.Current.Name, "결과 화면을 지나 로비로 돌아온다");
            _log.AssertNoRed("P2 승리 결과 화면");

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
