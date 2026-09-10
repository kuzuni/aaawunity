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
    /// T240 4항 — PvP 결과 화면(<see cref="ArenaResult"/>)이 <b>레퍼런스 34 의 꼴</b>로 서고, «계속» 로 닫히는가.
    /// <para>
    /// 재는 것: ⓐ 제목이 «승리»/«패배» 로 갈린다 ⓑ 티어 명판이 <b>표에서 온 이름</b>을 그대로 띄운다
    /// ⓒ 승점 글자가 <b>실제로 움직인 값</b>(<c>Delta</c>)이다 — 바닥에 걸린 판은 «−6» 이 아니다
    /// ⓓ 두 아바타·VS 배지·이름 둘이 선다 ⓔ «계속» 을 <b>눌러서</b> 닫히고 <c>onContinue</c> 가 불린다 ⓕ 빨간 줄 0.
    /// </para>
    /// <para>
    /// ⚠ 여기 <b>«전투가 돈다» 는 없다</b> — 이 회차는 화면만 세웠고 배선(도전 → 전투 → 이 화면)은 다음 회차다.
    /// 그래서 이 자는 <see cref="ArenaMatch.Settle"/> 이 낸 값을 <b>손으로 만들어</b> 넣는다(판을 기다리지 않는다 · §1 «그 판에 그 일이 일어난다» 규약).
    /// </para>
    /// «닫는 길이 있는가» 를 코드로 안 보고 <b>눌러서</b> 재는 까닭은 <see cref="PopupCloseTests"/> 머리에 적힌 그대로다(T169 2항).
    /// </summary>
    public class ArenaResultTests
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

        static bool Click(Transform root, string name)
        {
            var b = UiKit.Find(root, name)?.GetComponent<Button>();
            if (b == null) return false;
            b.onClick.Invoke(); return true;
        }

        static TMP_Text Text(Transform root, string name)
        {
            var t = UiKit.Find(root, name);
            return t == null ? null : t.GetComponent<TMP_Text>();
        }

        /// <summary>표를 실제로 태워 «내가 지어낸 값» 이 아니라 <b>그 판의 값</b>으로 화면을 세운다.</summary>
        ArenaMatch.Outcome Settle(double from, bool win)
        {
            var D = _app.Data;
            _app.Save.ArenaScore = from;
            return ArenaMatch.Settle(_app.Save, D != null ? D.ArenaMatch : null, D != null ? D.ArenaDummy : null, win);
        }

        [UnityTest]
        public IEnumerator WinScreenShowsTheTitleTierAndTheScoreItActuallyGained()
        {
            yield return Boot();
            var o = Settle(1000, true);
            bool continued = false;
            ArenaResult.Show(o, "나", "도전자 3", null, null, () => continued = true);
            yield return Frames(2);

            var root = _app.Overlay.Root;
            Assert.IsTrue(ArenaResult.Open, "떠 있어야 한다");
            Assert.AreEqual(ArenaResult.WinTitle, Text(root, "ResultTitle")?.text, "이긴 판의 제목");
            Assert.AreEqual(o.Tier, Text(root, "TierName")?.text, "명판은 표에서 온 티어 이름 그대로다");
            Assert.AreEqual(ArenaResult.Sign(o.Delta), Text(root, "MyDelta")?.text, "승점 글자는 실제로 움직인 값이다");
            Assert.AreEqual("나", Text(root, "MyName")?.text);
            Assert.AreEqual("도전자 3", Text(root, "FoeName")?.text);
            Assert.IsNotNull(UiKit.Find(root, "Emblem"), "방패 엠블럼");
            Assert.IsNotNull(UiKit.Find(root, "VsBadge"), "VS 배지");
            Assert.IsNotNull(UiKit.Find(root, "MyFace"), "내 초상 칸");
            Assert.IsNotNull(UiKit.Find(root, "FoeFace"), "상대 초상 칸");
            // T262 3항 — 34 도 23·24·33 과 같은 조각·같은 얼굴이다(주인 «프레임이 실제 프로필 프레임이랑 디자인이 다르네»).
            Assert.IsNotNull(UiKit.Find(UiKit.Find(root, "MyFace"), Profile.FrameKey(_app.Save)), "내 칸은 내가 고른 프로필 프레임");
            Assert.IsNotNull(UiKit.Find(UiKit.Find(root, "MyFace"), Profile.FaceName), "내 칸에 초상이 서 있다(종전에는 빈 칸이었다)");
            Assert.IsNotNull(UiKit.Find(UiKit.Find(root, "FoeFace"), Profile.FaceName), "상대 칸에도 초상이 선다");
            Assert.IsFalse(GearUi.HasItemFrame(UiKit.Find(root, "MyFace")), "옛 물건 칸(ItemFrame_01)이 남으면 안 된다");

            // T384 — 컨페티. 표 ㊻ 의 유일한 0점이던 자리라 «있다» 만 재면 다시 사라져도 아무 자도 안 운다.
            {
                var conf = UiKit.Find(root, "Confetti") as RectTransform;
                Assert.IsNotNull(conf, "컨페티 한 장(표 ㊻ 의 그 행 · T384)");
                var ci = conf.GetComponent<Image>(); Assert.IsNotNull(ci, "컨페티는 Image 한 장이다");
                // ⓐ 자리 = 표 그대로(§5 가 이 값을 잰다 · 앵커로 본다 — UiKit.Pct 가 놓는 자리)
                Assert.AreEqual(Layout.ArrConfetti.X, conf.anchorMin.x * 100f, 0.5f, "컨페티 x = 표 ㊻");
                Assert.AreEqual(1f - Layout.ArrConfetti.Y / 100f, conf.anchorMax.y, 1e-3f, "컨페티 y = 표 ㊻");
                Assert.AreEqual(Layout.ArrConfetti.W, (conf.anchorMax.x - conf.anchorMin.x) * 100f, 0.5f, "컨페티 폭 = 표 ㊻(가로 전부)");
                Assert.AreEqual(Layout.ArrConfetti.H, (conf.anchorMax.y - conf.anchorMin.y) * 100f, 0.5f, "컨페티 높이 = 표 ㊻(위 81%)");
                // ⓑ 클릭을 안 먹는다 — 위 81% 를 통째로 덮으므로 켜 두면 그 아래 자리들의 탭을 이 장이 가로챈다
                Assert.IsFalse(ci.raycastTarget, "컨페티는 클릭을 먹지 않는다(위 81% 를 덮는 장이다)");
                // ⓒ 층 — 어둠보다 위 · 엠블럼보다 아래(가려 버리면 «장식» 이 «가림막» 이 된다)
                var dimT = UiKit.Find(root, "Dimmed"); var emT = UiKit.Find(root, "Emblem");
                Assert.IsNotNull(dimT, "어둠"); Assert.IsNotNull(emT, "엠블럼");
                Assert.Greater(conf.GetSiblingIndex(), dimT.GetSiblingIndex(), "컨페티는 어둠 위에 있다");
                Assert.Less(conf.GetSiblingIndex(), emT.GetSiblingIndex(), "컨페티는 엠블럼 아래에 있다(엠블럼을 덮으면 안 된다)");
                // ⓓ ⚠ «머문다» 를 못 박는다 — 사라지는 연출로 바꾸면 사진 찍는 순간에 따라 §5 의 그 0점이 되돌아온다.
                yield return Frames(30);
                Assert.IsTrue(conf.gameObject.activeInHierarchy, "컨페티는 연출 뒤에도 서 있다(터뜨렸다 사라지는 조각이 아니다 · T384)");
                Assert.Greater(ci.color.a, 0.5f, "컨페티는 연출 뒤에도 보인다 — 흐려져 사라지면 표 ㊻ 의 그 행이 다시 0점이 된다");
            }

            // 눌러서 닫힌다 — «닫는 길이 코드에 있다» 가 아니라 «눌리면 닫힌다» 를 잰다(T169 2항)
            var btn = UiKit.Find(root, "ContinueBtn")?.GetComponent<Button>();
            Assert.IsNotNull(btn, "«계속» 버튼");
            btn.onClick.Invoke();
            yield return Frames(2);
            Assert.IsFalse(ArenaResult.Open, "«계속» 을 누르면 닫힌다");
            Assert.IsTrue(continued, "onContinue 가 불린다(아레나 화면으로 돌아가는 자리)");

            _log.AssertNoRed("PvP 결과(승리)");
            yield return Shutdown();
        }

        /// <summary>
        /// T240 배선 — 아레나 «줄 도전» 을 누르면 <b>판이 실제로 열리고</b> 그 판이 «아레나 판» 으로 표시된다.
        /// <para>여기서 재는 것은 <b>«열리는가» 까지</b>다 — 판을 끝까지 돌리지 않는다(판이 언제 끝나는지는 매번 다르고,
        /// 그것을 기다리는 단언이 오늘 배포를 세운 그 꼴이다 · §1 «그 판에 그 일이 일어난다» 규약).</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ArenaChallengeActuallyOpensAMatchMarkedAsArena()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageArena); yield return Frames(2);
            var ar = _app.Current.Root;
            Assert.IsTrue(Click(ar, "ChallengeBtn"), "«도전» 을 눌러 팝업을 연다"); yield return Frames(2);
            var ov = _app.Overlay.Root;
            Assert.IsTrue(Click(ov, "FoeBtn:0"), "상대 줄의 «도전»"); yield return Frames(2);

            Assert.AreEqual("battle", _app.Current.Name, "판이 열린다(여태 이 버튼은 Noop 이었다)");
            var bs = _app.GetScreen<BattleScreen>();
            Assert.IsNotNull(bs);
            Assert.IsTrue(bs.IsArena, "그 판은 «아레나 판» 으로 표시된다 — 이 표식 하나가 끝났을 때 승점 갈래를 켠다");
            Assert.AreEqual(EventsScreen.PageArena, bs.ExitPage, "끝나면 아레나 화면으로 돌아간다");

            _log.AssertNoRed("아레나 도전 → 판 열림");
            yield return Shutdown();
        }

        /// <summary>
        /// T240 1항 — 아레나 판은 챕터 전투와 <b>다른 무대</b>에서 돈다: 주인 레퍼런스 <c>33_pvp_battle.jpg</c> 의 가운데는 <b>모래 마당</b>이다.
        /// <para>
        /// 두 판을 <b>나란히</b> 잰다 — «아레나가 사막이다» 만 재면 <b>그냥 그 챕터가 사막이었을 때도 통과</b>한다(챕터 4·8… 은 원래 사막이다).
        /// 그래서 같은 챕터로 일반 판을 한 번 더 열어 «같은 챕터인데 무대가 갈린다» 를 재는 것이 이 자의 전부다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ArenaRunFightsOnTheSandYardNotTheChapterMap()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageArena); yield return Frames(2);
            Assert.IsTrue(Click(_app.Current.Root, "ChallengeBtn")); yield return Frames(2);
            Assert.IsTrue(Click(_app.Overlay.Root, "FoeBtn:0")); yield return Frames(2);

            var bs = _app.GetScreen<BattleScreen>();
            Assert.IsNotNull(bs); Assert.IsTrue(bs.IsArena, "먼저 «아레나 판» 인 것을 확인한다(아니면 아래 비교가 뜻이 없다)");
            Assert.IsNotNull(bs.World, "월드가 서 있다");
            int chapter = bs.G.Chapter;
            Assert.AreEqual(BattleWorld.Theme.Arena.Name, bs.World.MapTheme.Name, "아레나 판의 무대 = 모래 마당");

            // 같은 챕터로 일반 판 — 여기서 갈려야 «챕터 때문이 아니라 아레나라서» 가 증명된다.
            _app.StartBattle(chapter); yield return Frames(2);
            var normal = _app.GetScreen<BattleScreen>();
            Assert.IsFalse(normal.IsArena, "이 판은 아레나가 아니다");
            Assert.AreEqual(BattleWorld.Theme.ForChapter(chapter).Name, normal.World.MapTheme.Name, "일반 판은 종전대로 챕터가 무대를 정한다");
            if (BattleWorld.Theme.ForChapter(chapter).Name == BattleWorld.Theme.Arena.Name)
                Assert.Ignore("이 챕터의 무대가 마침 사막이라 이 판으로는 둘을 못 가른다 — 잴 것이 없는 판은 통과시킨다(§1 ⓑ)");
            Assert.AreNotEqual(normal.World.MapTheme.Name, BattleWorld.Theme.Arena.Name, "같은 챕터인데 무대가 갈린다");

            _log.AssertNoRed("아레나 무대 ↔ 챕터 무대");
            yield return Shutdown();
        }

        /// <summary>
        /// T240 3항 — 아레나 «도전» 이 여는 판이 실제로 <b>1대1</b> 인가(웨이브가 아니라).
        /// <para>
        /// 앞 회차까지 엔진·규칙은 다 서 있었지만 <b>아무도 그것을 안 불렀다</b> — 도전을 눌러도 종전 챕터 전투가 열렸다.
        /// 이 자가 재는 것은 «순위 → 상대 전투력 → 스탯 → 판» 네 칸이 <b>한 줄로 이어졌는가</b> 하나다.
        /// </para>
        /// <para>⚠ 판을 끝까지 돌리지 않는다 — «열린 판의 모양» 만 본다(§1 «그 판에 그 일이 일어난다» 를 전제로 두지 않는다).</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ArenaChallengeOpensAOneOnOneNotAChapterOfWaves()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageArena); yield return Frames(2);
            Assert.IsTrue(Click(_app.Current.Root, "ChallengeBtn")); yield return Frames(2);
            Assert.IsTrue(Click(_app.Overlay.Root, "FoeBtn:0")); yield return Frames(2);

            var bs = _app.GetScreen<BattleScreen>();
            Assert.IsNotNull(bs); Assert.IsTrue(bs.IsArena, "먼저 «아레나 판» 인 것을 확인한다");
            Assert.IsNotNull(bs.G);

            // 표가 없으면 종전 챕터 전투가 열리는 것이 «맞는» 동작이라, 그 경우는 가를 것이 없다(§1 ⓑ).
            if (_app.Data == null || _app.Data.ArenaFoe == null || _app.Data.ArenaDummy == null)
                Assert.Ignore("아레나 상대 규칙표가 없다 — 그러면 1대1 이 아니라 종전 챕터 전투가 열리는 것이 맞는 동작이다");

            Assert.AreEqual(1, bs.G.Nodes.Count, "1대1 이면 노드가 하나다 — 웨이브·이벤트·보스가 줄줄이 서 있으면 챕터 판이 열린 것이다");
            Assert.AreEqual(1, bs.G.TotalEnemies, "적은 하나다");
            var foe = bs.G.Nodes[0].Enemies[0];
            Assert.Greater(foe.MaxHp, 0, "상대 체력이 규칙에서 나왔다");
            Assert.Greater(foe.Dmg, 0, "상대 공격력이 규칙에서 나왔다");
            Assert.IsFalse(foe.IsBoss, "보스가 아니다");

            _log.AssertNoRed("아레나 도전 → 1대1");
            yield return Shutdown();
        }

        /// <summary>
        /// T240 2항 — 아레나 판의 상대는 <b>몹이 아니라 «플레이어 캐릭터»</b> 다(주인 메모 · 레퍼런스 <c>33_pvp_battle.jpg</c>).
        /// <para>
        /// «기사 투구를 썼다» 만 재면 <b>몹도 투구를 쓰므로</b>(주인 지시 «적들은 전부 모자 쓴 상태») 갈리지 않는다 —
        /// 그래서 <b>같은 챕터의 일반 판 적과 나란히</b> 놓고 «둘이 다른 것을 입었나» 를 잰다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheArenaFoeWearsAKnightNotAMobSkin()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageArena); yield return Frames(2);
            Assert.IsTrue(Click(_app.Current.Root, "ChallengeBtn")); yield return Frames(2);
            Assert.IsTrue(Click(_app.Overlay.Root, "FoeBtn:0")); yield return Frames(4);

            var bs = _app.GetScreen<BattleScreen>();
            Assert.IsTrue(bs.IsArena); Assert.IsNotNull(bs.World);
            Assert.IsTrue(bs.World.IsArena, "월드도 이 판을 아레나로 안다");
            int chapter = bs.G.Chapter;

            var arenaFoe = FirstEnemyRig(bs.World);
            if (arenaFoe == null) Assert.Ignore("아직 적 리그가 안 섰다 — 잴 것이 없는 판은 통과시킨다(§1 ⓑ)");
            Assert.AreEqual("cm.knight.helmet", arenaFoe.Wearing.Helmet, "아레나 상대는 기사 투구를 쓴다");
            Assert.AreEqual("cm.knight.sword", arenaFoe.Wearing.Sword, "기사 검을 든다");
            Assert.IsNull(arenaFoe.Wearing.Bow, "몹 궁수 갈래로 가지 않는다");

            // 같은 챕터의 일반 판 — 여기서 갈려야 «아레나라서» 가 증명된다.
            _app.StartBattle(chapter); yield return Frames(4);
            var normal = _app.GetScreen<BattleScreen>();
            Assert.IsFalse(normal.IsArena);
            var mob = FirstEnemyRig(normal.World);
            if (mob == null) Assert.Ignore("일반 판에 아직 적 리그가 없다 — 가를 것이 없다");
            Assert.AreNotEqual(arenaFoe.Wearing.Helmet, mob.Wearing.Helmet, "같은 챕터인데 상대 외형이 갈린다(아레나 = 기사 · 챕터 = 몹)");

            _log.AssertNoRed("아레나 상대 외형");
            yield return Shutdown();
        }

        /// <summary>월드에 선 첫 적 리그(아직 없으면 <c>null</c>) — 이름 규약 «Enemy&lt;id&gt;» 로 찾는다.</summary>
        static CharacterRig FirstEnemyRig(BattleWorld world)
        {
            if (world == null || world.Root == null) return null;
            foreach (Transform t in world.Root)
                if (t.name.StartsWith("Enemy"))
                {
                    var rig = t.GetComponent<CharacterRig>();
                    if (rig != null && rig.Wearing != null) return rig;
                }
            return null;
        }

        [UnityTest]
        public IEnumerator LoseAtTheFloorWritesWhatItTookNotWhatTheTableSays()
        {
            yield return Boot();
            // 승점 4 에서 지면 표는 −6 이지만 바닥(0)에 걸려 실제로는 −4 만 간다 — 화면이 «−6» 이라 적으면 거짓말이다
            var o = Settle(4, false);
            ArenaResult.Show(o, "나", "도전자 9", null, null);
            yield return Frames(2);

            var root = _app.Overlay.Root;
            Assert.AreEqual(ArenaResult.LoseTitle, Text(root, "ResultTitle")?.text, "진 판의 제목");
            Assert.AreEqual(0, _app.Save.ArenaScore, "0 미만으로는 안 내려간다(주인 문장)");
            Assert.AreEqual("−4", Text(root, "MyDelta")?.text, "표의 −6 이 아니라 실제로 간 −4 다");

            _log.AssertNoRed("PvP 결과(패배·바닥)");
            yield return Shutdown();
        }
    }
}
