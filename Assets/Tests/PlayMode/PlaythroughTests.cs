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
        // P5 던전 — 지옥의 문 도전 → 클리어 → 리워드 → 소탕 → 티켓 0 → 광고 · 다이아 → 원정 1층 → 2층.
        // ─────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// 지금 켜진 던전·아레나 페이지의 루트. <b>도달</b>을 같이 잰다(화면이 «events» · 보고 있는 페이지가 그것).
        /// <para>⚠ <see cref="UiKit.Find"/> 는 <b>꺼진 형제 페이지까지</b> 뒤진다 — «BackBtn»·«ChallengeBtn» 은 페이지마다 하나씩이라
        /// 화면 루트에서 찾으면 다른 페이지의 것을 누를 수 있다. 그래서 늘 <c>Page:…</c> 안에서 찾는다(이름 계약 · <c>EventsScreen</c> 머리).</para>
        /// </summary>
        Transform Page(string page)
        {
            Assert.AreEqual("events", _app.Current.Name, "도달 — 던전·아레나 화면이 켜져 있다(«" + page + "» 를 기다렸다)");
            var ev = _app.GetScreen<EventsScreen>(); Assert.IsNotNull(ev, "던전·아레나 화면");
            Assert.AreEqual(page, ev.Page, "보고 있는 페이지");
            var pg = UiKit.Find(_app.Current.Root, "Page:" + page); Assert.IsNotNull(pg, "페이지 «" + page + "» 의 루트");
            return pg;
        }
        /// <summary>«탭하여 닫기» 어둠을 눌러 팝업을 닫는다 — 리워드 팝업(T241)은 이 길로만 닫히고, 그 뒤 <c>onClose</c> 가 세부 팝업을 다시 연다.</summary>
        IEnumerator TapDimmed(string what)
        {
            var dim = UiKit.Find(_app.Overlay.Root, "Dimmed")?.GetComponent<Button>();
            Assert.IsNotNull(dim, what + " — «탭하여 닫기» 어둠");
            dim.onClick.Invoke(); yield return Frames(2);
        }
        /// <summary>
        /// 세부 팝업(21)이 떠 있는 상태에서 던전 판 하나를 «도전 → 잠깐 굴리고 → 깨고 → «그냥 받기» → 던전 페이지» 로 지나간다.
        /// 판을 나간 뒤 리워드 팝업(T241)이 떴으면 닫는다 — 표에 보상이 없으면 안 뜨는 것이 맞다(«얻은 게 없는데 뜨는 팝업 금지»)라 있을 때만.
        /// <para>재는 것은 배선 둘뿐 — 판이 «어느 던전» 인지 안다(<c>DungeonKey</c>) · 나가면 로비가 아니라 던전 페이지다. 보상 수는 안 잰다(3항 ⓐ).</para>
        /// </summary>
        IEnumerator ClearDungeonRun(string key)
        {
            Tap(_app.Overlay.Root, "ChallengeBtn"); yield return Frames(2);
            Assert.AreEqual("battle", _app.Current.Name, "«도전» 이 판을 연다(" + key + ")");
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면"); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            Assert.AreEqual(key, bs.DungeonKey, "판이 «어느 던전» 인지 안다 — 이것이 없으면 클리어를 아무도 안 적는다(T228 ⓓ)");
            _log.AssertNoRed("P5 " + key + " 판 열기");

            // 잠깐 굴린다(배속 3 · 실제 0.5초) — 시작 특전·레벨(원정 · T183)을 실은 판이 실제로 돌기는 하는가. 죽는 것은 이 단계의 몫이 아니라 체력을 받쳐 준다.
            Time.timeScale = 3f;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 0.5f && !G.Over && !_app.Overlay.IsOpen)
            {
                if (G.P.Hp < G.P.MaxHp * 0.5) G.P.Hp = G.P.MaxHp;
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.Greater(G.T, 0.0, "엔진이 실제로 돌았다");
            if (!G.Over)
            {
                if (_app.Overlay.IsOpen) { _app.Overlay.Close(); yield return Frames(1); }   // 엔진이 스스로 연 레벨업이면 여기서 정리한다(P2 와 같은 이유)
                G.Pending = null; G.PendingLevelUps = 0;
                G.Cleared = true;
            }
            else Assert.IsTrue(G.Cleared, "체력을 받쳐 준 0.5초 안에 판이 끝났다면 이긴 쪽이어야 한다");
            yield return UntilOpen(8f, "클리어 팝업(" + key + ")");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "그냥 받기"), "«그냥 받기»");
            yield return Frames(3);
            Page(EventsScreen.PageDungeon);   // 도달 — 던전 판을 나가면 던전 페이지로 돌아온다(로비가 아니다 · ExitPage)
            if (_app.Overlay.IsOpen)
            {
                Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "RewardTitle"), "판을 나간 뒤 떠 있는 것은 «리워드» 팝업(T241)이어야 한다");
                yield return TapDimmed("리워드 팝업(" + key + ")");
            }
            else Debug.Log("[T300] P5 " + key + " — 리워드 팝업이 없다(표에 클리어 보상이 없으면 그것이 맞다)");
            Assert.IsFalse(_app.Overlay.IsOpen, "리워드 팝업을 닫으면 던전 페이지만 남는다");
            _log.AssertNoRed("P5 " + key + " 클리어 → 리워드");
        }

        /// <summary>
        /// P5 던전(T300 1항) — 노는 것: 지옥의 문 도전 → 클리어 → 리워드 → 소탕 → 티켓 0 → 광고 1 → 다이아 티켓 → 원정 1층 → 2층(T291).
        /// 재는 것: <b>도달 · 팝업이 스스로 열리고 닫힘 · 배선(티켓이 줄고 늘고 · 층이 오른다) · 빨간 줄 0</b>.
        /// <para>
        /// ⚠ 표 값(보충 2 · 광고 1 · 다이아 50 · 보상 수)은 <b>안 잰다</b>(3항 ⓐ · P2 와 같은 까닭). 티켓을 «0 으로 만드는 것» 도 표를 세지 않고
        /// 세이브에 직접 놓는다(<c>DunTickets</c> · <c>DungeonTicketPlayTests</c> 의 꼴) — 하루 보충이 2 가 아니어도 각본은 그대로다.
        /// </para>
        /// <para>⚠ 팝업은 손으로 안 연다 — «입장» 을 눌러 세부 팝업이, «도전» 이 판을, 판이 끝나야 클리어·리워드 팝업이 스스로 뜬다(T280 규칙).</para>
        /// </summary>
        [UnityTest]
        public IEnumerator P5_던전을_한_바퀴_놀아도_죽지_않는다()
        {
            yield return Boot();
            var D = _app.Data.Dungeon; Assert.IsNotNull(D, "dungeon.json 이 카탈로그(data.dungeon)로 실려야 한다");
            var S = _app.Save; string today = SaveStore.Today();
            // 각 단계가 제 조건을 만들어 시작한다(1항) — 다이아 티켓을 살 만큼(표 값 · 수를 안 박는다)
            S.Gem = D.GemCost * 2; _app.Persist();

            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(3);
            var pg = Page(EventsScreen.PageDungeon);
            Assert.IsNotNull(UiKit.Find(pg, "Card:hell"), "지옥의 문 카드"); Assert.IsNotNull(UiKit.Find(pg, "Card:expedition"), "원정 카드");
            _log.AssertNoRed("P5 던전 페이지");

            // ⓐ 지옥의 문 — 입장 → 세부 팝업(21) → 도전 → 클리어 → «그냥 받기» → 던전 페이지 + 리워드
            Tap(UiKit.Find(pg, "Card:hell"), "EnterBtn"); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "던전 세부 팝업(21)이 열린다");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "FloorCircle"), "세부 팝업의 층수 원");
            int floor0 = DungeonSweep.Floor(S, "hell");
            yield return ClearDungeonRun("hell");
            Assert.Greater(DungeonSweep.Floor(S, "hell"), floor0, "«깬 적 있다» 가 남았다 — 이것 하나가 소탕의 조건이다(배선 · T228)");

            // ⓑ 소탕 — 클리어한 던전이라 되고, 누르면 리워드 팝업 → 닫으면 세부 팝업이 다시 선다
            // 소탕할 티켓 한 장은 있게(보충 수를 안 믿는다)
            if (DungeonTickets.Tickets(S, D, "hell", today) < 1) S.DunTickets["hell"] = 1;
            Tap(UiKit.Find(Page(EventsScreen.PageDungeon), "Card:hell"), "EnterBtn"); yield return Frames(2);
            int tk0 = DungeonTickets.Tickets(S, D, "hell", today);
            Tap(_app.Overlay.Root, "SweepBtn"); yield return Frames(2);
            if (UiKit.Find(_app.Overlay.Root, "RewardTitle") != null) yield return TapDimmed("소탕 리워드 팝업");
            else Debug.Log("[T300] P5 소탕 — 리워드 팝업이 없다(표의 sweep 이 비었으면 그것이 맞다)");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "SweepBtn"), "소탕 뒤 세부 팝업이 다시 서 있다");
            Assert.Less(DungeonTickets.Tickets(S, D, "hell", today), tk0, "소탕이 티켓을 썼다(배선) — 안 줄었으면 «소탕» 이 그림이다");
            _log.AssertNoRed("P5 소탕");

            // ⓒ 티켓 0 → 두 버튼이 «광고 · 다이아» 가 된다(T99 3항) → 광고(모의 카운트다운)로 1 → 다시 0 → 다이아로 1
            _app.Overlay.Close(); yield return Frames(1);
            S.DunTickets["hell"] = 0;
            Tap(UiKit.Find(Page(EventsScreen.PageDungeon), "Card:hell"), "EnterBtn"); yield return Frames(2);
            // 티켓 0 이면 왼쪽 = 광고
            Tap(_app.Overlay.Root, "SweepBtn"); yield return Frames(2);
            { float t0 = Time.realtimeSinceStartup; while (DungeonTickets.Tickets(S, D, "hell", today) == 0 && Time.realtimeSinceStartup - t0 < 8f) yield return null; }
            Assert.Greater(DungeonTickets.Tickets(S, D, "hell", today), 0, "광고 카운트다운이 끝나면 티켓이 들어온다(배선)");
            yield return Frames(2);
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "SweepBtn"), "광고 뒤 세부 팝업이 다시 선다");
            _log.AssertNoRed("P5 광고 티켓");

            _app.Overlay.Close(); yield return Frames(1);
            S.DunTickets["hell"] = 0; double gem0 = S.Gem;
            Tap(UiKit.Find(Page(EventsScreen.PageDungeon), "Card:hell"), "EnterBtn"); yield return Frames(2);
            // 티켓 0 이면 오른쪽 = 다이아
            Tap(_app.Overlay.Root, "ChallengeBtn"); yield return Frames(2);
            Assert.Greater(DungeonTickets.Tickets(S, D, "hell", today), 0, "다이아로 티켓을 샀다(배선)");
            Assert.Less(S.Gem, gem0, "다이아가 빠졌다(배선)");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "SweepBtn"), "산 뒤 세부 팝업이 다시 선다");
            _log.AssertNoRed("P5 다이아 티켓");

            // ⓓ 원정 — 1층을 깨면 다음 도전은 2층이다(T291 층). 층이 없는 지옥의 문과 달리 여기서만 «올라간다» 가 보인다.
            _app.Overlay.Close(); yield return Frames(1);
            if (DungeonTickets.Tickets(S, D, "expedition", today) < 1) S.DunTickets["expedition"] = 1;
            int ch0 = DungeonSweep.Challenge(S, D, "expedition");
            Tap(UiKit.Find(Page(EventsScreen.PageDungeon), "Card:expedition"), "EnterBtn"); yield return Frames(2);
            yield return ClearDungeonRun("expedition");
            int ch1 = DungeonSweep.Challenge(S, D, "expedition");
            Assert.Greater(ch1, ch0, "1층을 깨면 도전 층이 올라간다(배선 · T291)");
            if (DungeonTickets.Tickets(S, D, "expedition", today) < 1) S.DunTickets["expedition"] = 1;
            Tap(UiKit.Find(Page(EventsScreen.PageDungeon), "Card:expedition"), "EnterBtn"); yield return Frames(2);
            yield return ClearDungeonRun("expedition");
            Assert.Greater(DungeonSweep.Challenge(S, D, "expedition"), ch1, "2층도 깨면 또 올라간다");
            _log.AssertNoRed("P5 원정 1층 → 2층");

            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("P5 던전");
            yield return Shutdown();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // P6 아레나 — 도전 팝업 → PvP 판 → 결과 화면 → 순위 보상 팝업 → 상인.
        // ─────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// P6 아레나(T300 1항) — 노는 것: 도전 팝업(24)에서 줄 «도전» → 아레나 판(33)을 굴리고 이긴다 → 결과 화면(34) «계속» → 순위 보상 팝업(25) → 상인(26).
        /// 재는 것: <b>도달 · 판이 «아레나 판» 으로 열림 · 결과 화면이 스스로 뜨고 «계속» 으로 아레나 페이지로 돌아옴 · 빨간 줄 0</b>. 승점·순위 값은 안 잰다(3항 ⓐ).
        /// <para>⚠ 상인의 «구매» 는 <b>아직 배선이 없다</b>(<c>EventsScreen</c> 머리 «전부 표시만» · 카드 = <c>Clickable(card, Noop)</c>) — 각본은 «눌러도 죽지 않는가» 까지만 논다.
        /// 구매가 서는 회차가 이 줄을 «산 것이 세이브에 닿았는가» 로 올린다(절 «다른 워커» 조항).</para>
        /// </summary>
        [UnityTest]
        public IEnumerator P6_아레나를_한_판_놀아도_죽지_않는다()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageArena); yield return Frames(3);
            var pg = Page(EventsScreen.PageArena);
            Assert.IsNotNull(UiKit.Find(pg, "Podium"), "시상대");
            _log.AssertNoRed("P6 아레나 입장 페이지");

            // ⓐ 도전 팝업(24) → 줄 «도전» → 판이 열린다(아레나 판)
            Tap(pg, "ChallengeBtn"); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "도전 팝업(24)이 열린다");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "FoeRow:0"), "상대 줄");
            Tap(_app.Overlay.Root, "FoeBtn:0"); yield return Frames(2);
            Assert.AreEqual("battle", _app.Current.Name, "줄 «도전» 이 판을 연다");
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면"); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            Assert.IsTrue(bs.IsArena, "그 판은 «아레나 판» 이다 — 이 표식 하나가 끝났을 때 승점 갈래를 켠다(T240)");
            _log.AssertNoRed("P6 아레나 판 열기");

            // ⓑ 굴리고(배속 3 · 실제 1초) → 이긴다 → 결과 화면(34)이 스스로 뜬다 → «계속» → 아레나 페이지
            //    1대1 이라 1초 안에 실제로 끝날 수도 있다 — 그러면 결과 화면이 이미 뜨는 중이니 손대지 않는다(레벨업 팝업과 가른다).
            Time.timeScale = 3f;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 1.0f && !G.Over && !_app.Overlay.IsOpen)
            {
                if (G.P.Hp < G.P.MaxHp * 0.5) G.P.Hp = G.P.MaxHp;
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.Greater(G.T, 0.0, "엔진이 실제로 돌았다");
            if (!G.Over)
            {
                if (_app.Overlay.IsOpen) { _app.Overlay.Close(); yield return Frames(1); }
                G.Pending = null; G.PendingLevelUps = 0;
                G.Cleared = true;
            }
            else Assert.IsTrue(G.Cleared, "체력을 받쳐 준 1초 안에 판이 끝났다면 이긴 쪽이어야 한다");
            yield return UntilOpen(8f, "PvP 결과 화면(34)");
            Assert.IsTrue(ArenaResult.Open, "떠 있는 것은 PvP 결과 화면이다 — 아레나 판은 클리어 팝업이 아니라 승점 결과로 끝난다(T240)");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ResultTitle"), "결과 제목");
            Tap(_app.Overlay.Root, "ContinueBtn"); yield return Frames(3);
            Assert.IsFalse(ArenaResult.Open, "«계속» 으로 닫힌다");
            pg = Page(EventsScreen.PageArena);   // 도달 — 결과를 지나 아레나 페이지로 돌아온다(로비가 아니다 · ExitPage)
            _log.AssertNoRed("P6 판 → 결과 → 아레나");

            // ⓒ 순위 보상 팝업(25) → 닫기 → 상인(26) → 상품 한 칸(표시만) → 뒤로
            Tap(pg, "RewardsBtn"); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "순위 보상 팝업(25)이 열린다");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "RewardRow:0"), "보상 줄");
            _app.Overlay.Close(); yield return Frames(1);
            _log.AssertNoRed("P6 순위 보상");

            Tap(pg, "MerchantBtn"); yield return Frames(2);
            pg = Page(EventsScreen.PageMerchant);
            Assert.IsNotNull(UiKit.Find(pg, "Goods:0"), "상품 칸");
            Tap(pg, "Goods:0"); yield return Frames(2);
            Tap(pg, "BackBtn"); yield return Frames(2);
            Page(EventsScreen.PageArena);
            _log.AssertNoRed("P6 상인");

            _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("P6 아레나");
            yield return Shutdown();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // P11 설정 — ≡ 메뉴 → 설정 → 프로필 아바타 바꾸기 · 다시 설정 → 데이터 삭제 → 확인 → 로비.
        // ─────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// P11 설정(T300 1항) — 노는 것: 로비 ≡ → «설정» → 프로필 «변경» → 다른 초상을 고르고 «선택» → 다시 ≡ → «설정» → «데이터 삭제» → «삭제».
        /// 재는 것: <b>도달(로비로 돌아온다) · 팝업이 닫힌다 · 배선(고른 초상이 세이브에 붙고, 삭제 뒤 세이브가 새것이다) · 빨간 줄 0</b>.
        /// <para>
        /// ⚠ <b>팝업을 손으로 안 연다</b>(P2 · 결정 922 ③) — <c>Overlay.Settings()</c>·<c>Profile.OpenAvatar()</c> 를 직접 부르면
        /// 메뉴 줄·«변경» 버튼의 배선이 끊겨 있어도 초록이다. 봇은 사람이 누르는 자리(<c>Button_Menu</c> → <c>Menu:settings</c> → <c>ProfileBtn</c>)를 누른다.
        /// </para>
        /// <para>
        /// ⚠ <b>값은 안 잰다</b>(3항 ⓐ) — «삭제 뒤 골드 0» 같은 것은 <c>SaveStore.Reset</c> 의 자(EditMode) 몫이다. 여기서 보는 것은
        /// «삭제를 눌렀더니 세이브 <b>객체가 갈렸다</b>» 는 배선 하나뿐이다 — 그것이 끊기면 팝업은 그대로 닫히고 로비도 그대로 서서 다른 자는 아무도 안 운다.
        /// </para>
        /// <para>
        /// ⚠ <b>버튼은 이름보다 글자로 집는다</b> — 설정 팝업의 «데이터 삭제»·확인 팝업의 «삭제» 는 <c>UiKit.Button</c> 이 이름을 안 준다(조각 키가 이름이 된다).
        /// 이름 계약이 있는 것(<c>ProfileBtn</c>·<c>Avatar:…</c>·<c>ChooseBtn</c>)만 이름으로 — <c>ProfileTests</c>·<c>LobbyMenuTests</c> 가 이미 밟은 관용구 그대로다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator P11_설정에서_아바타를_바꾸고_데이터를_지워도_죽지_않는다()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(3);
            Assert.AreEqual("lobby", _app.Current.Name, "도달 — 로비");

            // ⓐ ≡ → «설정» → 프로필 «변경» — 사람이 누르는 길 그대로
            Tap(_app.Current.Root, "Button_Menu"); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "≡ 메뉴가 열린다");
            Tap(_app.Overlay.Root, "Menu:" + LobbyMenu.ItemSettings); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "«설정» 줄이 설정 팝업을 연다");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "BGM"), "설정 팝업(음악 줄) — 메뉴 줄이 설정으로 이어졌다");
            _log.AssertNoRed("P11 설정 팝업");

            Tap(_app.Overlay.Root, "ProfileBtn"); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "프로필 «변경» 이 아바타 팝업을 연다");
            // 지금 것이 아닌 초상을 고른다 — 같은 것을 고르면 «붙었다» 를 못 가른다
            string before = Profile.CurrentIcon(_app.Save);
            string want = null;
            foreach (var icon in Profile.Icons) if (icon != before) { want = icon; break; }
            Assert.IsNotNull(want, "고를 다른 초상이 하나는 있다");
            Tap(_app.Overlay.Root, Profile.RowPrefix + want); yield return Frames(1);
            Tap(_app.Overlay.Root, Profile.ChooseName); yield return Frames(3);
            yield return UntilClosed(3f, "아바타 «선택»");
            Assert.AreEqual(want, _app.Save.ProfileIcon, "고른 초상이 세이브에 붙었다 — «선택» 이 그림이 아니라 길인가는 여기서만 갈린다");
            Assert.AreEqual("lobby", _app.Current.Name, "아바타를 바꾸고도 로비에 서 있다");
            _log.AssertNoRed("P11 아바타 바꾸기");

            // ⓑ 다시 ≡ → «설정» → «데이터 삭제» → «삭제» → 로비
            Tap(_app.Current.Root, "Button_Menu"); yield return Frames(2);
            Tap(_app.Overlay.Root, "Menu:" + LobbyMenu.ItemSettings); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "설정 팝업(다시)");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "데이터 삭제"), "설정 아래 «데이터 삭제» 버튼");
            yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "확인 팝업이 선다(바로 지우지 않는다)");
            var saveBefore = _app.Save;
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "삭제"), "확인 팝업 «삭제»");
            yield return Frames(3);
            yield return UntilClosed(3f, "데이터 삭제");
            Assert.AreEqual("lobby", _app.Current.Name, "지우면 로비로 돌아온다");
            Assert.AreNotSame(saveBefore, _app.Save, "세이브가 새것으로 갈렸다 — «삭제» 가 ResetSave 에 닿았는가는 여기서만 갈린다");
            Assert.IsNotNull(UiKit.Find(_app.Current.Root, "Start"), "새 세이브로 그린 로비에도 START 가 있다");
            _log.AssertNoRed("P11 데이터 삭제 → 로비");

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
        /// <para><b>마지막 남은 단계가 들어오는 커밋에서</b> ⓑ 를 «전부 있다» 단언으로 올린다 — 그 한 줄이 이 절의 마지막 일이다(절 4항에 적어 뒀다).
        /// ⚠ 처음엔 «P11 커밋에서» 라 적혀 있었는데 P11 이 P3~P10 보다 먼저 들어왔다(설정이 가장 잠잠한 자리라서) — 번호가 아니라 «빈 칸이 0 이 되는 커밋» 이 그 자리다.</para>
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
                          + " (마지막 단계가 들어오는 커밋에서 이 로그를 «전부 있다» 단언으로 올린다)");
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
