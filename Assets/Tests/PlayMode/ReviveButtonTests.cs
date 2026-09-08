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
    /// T280 — <b>«부활» 버튼을 실제로 눌러 본다.</b> 주인이 못 박은 규칙(«부활권 1개로 게임 1회 부활» · T254)에서
    /// <b>규칙</b>은 <see cref="Revive"/> 와 <c>ReviveTests</c> 가 촘촘히 재고 <b>그림</b>은 T274 가 쟀는데,
    /// 그 둘 사이 — <b>«버튼이 그 규칙에 닿아 있는가»</b> — 만 아무도 안 재고 있었다(검수 Q 실측 등재: PlayMode 어디에도 <c>"ReviveBtn"</c> 문자열이 없다).
    /// <para>
    /// <b>왜 그 자리가 조용한가</b> — 배선이 끊기면(<c>onRevive</c> 에 <c>null</c>·엉뚱한 람다가 가면)
    /// Core 자는 <b>전부 초록인데 눌러도 아무 일이 없다.</b> 빨간 줄도 안 나고 게이트도 안 문다.
    /// 주인은 그때 «부활이 안 되는데» 라고 말하게 된다 — 즉 <b>사람이 유일한 자</b>인 자리였다.
    /// </para>
    /// <para>
    /// <b>손으로 만든 콜백을 안 쓴다</b>(지시서 T280 4항이 준 대안 중 어려운 쪽) — <see cref="Overlay.Dead"/> 에
    /// 진짜 같은 람다를 내가 넘기면 <b>내가 넘긴 그 람다</b>가 불리는 것만 재어지고, 정작 <c>BattleScreen</c> 이
    /// 무엇을 넘기는지는 그대로 안 재어진다(끊어져 있어도 이 자는 초록이다). 그래서 <b>진짜 판을 태우고</b>
    /// (<c>StartBattle</c> → 죽음 → 화면이 스스로 연 팝업) 그 버튼을 누른다.
    /// 재는 사슬: <c>ReviveBtn</c> → <c>BattleScreen.ReviveNow</c> → <c>Revive.Use</c>(티켓 −1 · 체력·실드 · <c>Dead</c> 내림) → <c>App.Persist()</c>.
    /// </para>
    /// <para>
    /// ⚠ <b>«티켓 0 이면 버튼이 없다» 로 재면 틀린다</b>(T274 실측) — 자리를 낼지는 <c>canRevive</c>(«이 판에 아직 안 썼나»)가 정하고
    /// 개수는 <b>비활성 + 안내 줄</b>로 말한다. 0개 판은 그 꼴을 그대로 잰다.
    /// </para>
    /// </summary>
    public class ReviveButtonTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

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
            _app = null;
            yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static Button Btn(Transform root, string name)
        {
            var t = root != null ? UiKit.Find(root, name) : null;
            return t != null ? t.GetComponent<Button>() : null;
        }

        /// <summary>판을 열고 «죽었다» 로 만든 뒤, 화면이 <b>스스로</b> 사망 팝업을 열 때까지 기다린다.</summary>
        IEnumerator Die(BattleState G)
        {
            G.P.Hp = 0; G.Dead = true;
            float t0 = Time.realtimeSinceStartup;
            while (!_app.Overlay.IsOpen && Time.realtimeSinceStartup - t0 < 10f) yield return null;
            Assert.IsTrue(_app.Overlay.IsOpen, "죽으면 화면이 스스로 사망 팝업을 연다(BattleScreen.EndRun)");
            yield return Frames(2);
        }

        /// <summary>
        /// T280 ⓐ — <b>누르면 실제로 살아난다</b>: 티켓이 줄고 체력·실드가 차고 판이 이어지고 <b>디스크에도 남는다</b>.
        /// <para>마지막 하나(디스크)가 이 자의 값이다 — <see cref="Revive"/> 는 순수 C# 이라 저장을 안 하므로,
        /// 세이브가 바뀌었다는 것은 <b>화면 쪽 <c>ReviveNow</c> 가 실제로 불렸다</b>는 뜻이다(Core 만으로는 설명이 안 된다).</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ReviveButtonRevivesTheRunAndSpendsATicketOnDisk()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            var S = _app.Save; S.Revive = 2; _app.Persist();

            yield return Die(G);

            var b = Btn(_app.Overlay.Root, "ReviveBtn");
            Assert.IsNotNull(b, "«부활» 버튼이 사망 팝업에 선다");
            Assert.IsTrue(b.interactable, "부활권이 있으면 누를 수 있다");

            b.onClick.Invoke();
            yield return Frames(2);

            Assert.AreEqual(1, S.Revive, "부활권이 1 줄어든다(2 → 1)");
            Assert.AreEqual(1, bs.RevivesUsed, "이 판의 부활 횟수가 1 이 된다");
            Assert.IsFalse(G.Dead, "판이 이어진다 — «죽었다» 가 내려간다");
            Assert.AreEqual(G.P.MaxHp, G.P.Hp, 1e-6, "체력이 가득 찬다");
            Assert.AreEqual(G.P.MaxSh, G.P.Sh, 1e-6, "실드가 가득 찬다");
            Assert.IsFalse(_app.Overlay.IsOpen, "사망 팝업이 닫힌다");

            // 디스크 — 여기서 갈린다. Revive.Use 만 불렸다면 세이브 파일은 2 그대로다.
            var disk = SaveStore.Load(_app.Data);
            Assert.AreEqual(1, disk.Revive, "저장까지 갔다(App.Persist) — 껐다 켜도 1 이다");

            _log.AssertNoRed("부활 버튼을 눌러 판이 이어짐");
            yield return Shutdown();
        }

        /// <summary>
        /// T280 ⓑ — <b>0개 판</b>: 버튼은 서지만 <b>죽은 자리</b>다. 회색으로 «보이기만» 하는 잠금은
        /// <c>onClick.Invoke()</c>(<c>interactable</c> 을 건너뛴다)로 눌러 봐야만 걸린다 — T272 가 빠른 탐험에서 세운 그 꼴 그대로다.
        /// </summary>
        [UnityTest]
        public IEnumerator ReviveButtonWithNoTicketsIsLockedAndPressingItChangesNothing()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            var S = _app.Save; S.Revive = 0; _app.Persist();

            yield return Die(G);

            var b = Btn(_app.Overlay.Root, "ReviveBtn");
            Assert.IsNotNull(b, "0개여도 자리는 낸다 — 자리를 낼지는 개수가 아니라 «이 판에 아직 안 썼나» 가 정한다(T274 실측)");
            Assert.IsFalse(b.interactable, "부활권이 0 이면 못 누른다");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ReviveHint"), "어디서 구하는지 한 줄이 같이 뜬다 — 회색 버튼만 있으면 사람이 «고장» 으로 읽는다");

            b.onClick.Invoke();   // 잠금이 «그림» 이 아니라 «길» 인가는 이 한 줄에서만 갈린다
            yield return Frames(2);

            Assert.AreEqual(0, S.Revive, "티켓이 음수로 내려가지 않는다");
            Assert.AreEqual(0, bs.RevivesUsed, "이 판의 부활 횟수도 그대로다");
            Assert.IsTrue(G.Dead, "죽은 판은 그대로 죽어 있다");
            Assert.IsTrue(_app.Overlay.IsOpen, "팝업도 안 닫힌다");

            _log.AssertNoRed("부활권 0 판");
            yield return Shutdown();
        }

        /// <summary>
        /// T280 ⓒ — <b>«한 판에 한 번»</b>(<see cref="Revive.PerRun"/>)이 화면에서도 지켜지는가:
        /// 한 번 살아난 뒤 또 죽으면 <b>버튼 자리 자체가 안 뜬다</b>(«눌러도 안 되는 버튼» 을 안 만든다는 T254 3항의 규약).
        /// <para>티켓은 아직 남겨 둔 채로 잰다 — 안 그러면 «0개라서 없는 것» 과 구별이 안 된다.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SecondDeathInTheSameRunOffersNoReviveEvenWithTicketsLeft()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            var S = _app.Save; S.Revive = 3; _app.Persist();
            Assert.AreEqual(1, Revive.PerRun, "이 자는 «한 판에 한 번» 을 전제로 쓴 것이다 — PerRun 이 늘면 여기부터 고친다");

            yield return Die(G);
            var first = Btn(_app.Overlay.Root, "ReviveBtn");
            Assert.IsNotNull(first, "첫 죽음에는 자리가 있다");
            first.onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual(1, bs.RevivesUsed, "한 번 썼다");
            Assert.Greater(S.Revive, 0, "티켓은 아직 남아 있다 — 이 다음이 «개수» 가 아니라 «횟수» 로 막히는가를 잰다");

            yield return Die(G);
            Assert.IsNull(Btn(_app.Overlay.Root, "ReviveBtn"), "두 번째 죽음에는 자리 자체가 없다(한 판에 한 번)");
            Assert.IsNull(UiKit.Find(_app.Overlay.Root, "ReviveHint"), "안내 줄도 같이 없다 — 자리가 통째로 안 뜬 것이다");

            _log.AssertNoRed("한 판 두 번째 죽음");
            yield return Shutdown();
        }
    }
}
