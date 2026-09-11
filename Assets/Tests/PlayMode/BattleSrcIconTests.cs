using System.Collections;
using System.Collections.Generic;
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
    /// T458 2항 — 뜬 글자 옆의 <b>«왜 떴는지»</b> 그림(주인 2026-09-12 «해당 특전이나 해당 장비로 인해 번개 나왔으면 해당 꺼 아이콘이 데미지 텍스트에 떠야 함»).
    /// <para>
    /// 재는 법은 <see cref="CritPopTests"/> 와 같다 — 실제 전투를 세우고 <c>world.Handle</c> 로 <b>사건 하나</b>를 흘려 그 사건이 «새로» 만든 팝만 집는다(엔진 0줄).
    /// 1항이 엔진에 실어 준 <see cref="BattleEvent.Src"/> 가 화면에서 <b>그 특전의 그림</b>으로 나오는지가 이 자의 물음이다.
    /// </para>
    /// ⚠ <b>«모르면 안 붙인다» 도 같이 잰다</b> — 출처가 없을 때 아무 그림이나 붙으면 화면이 «아는 척» 을 한다(1항이 엔진에서 지킨 그 규칙).
    /// </summary>
    public class BattleSrcIconTests
    {
        App _app; PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; Assert.IsNotNull(_app.Assets, "AssetCatalog");
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }

        static List<Image> PopIcons()
        {
            var found = new List<Image>();
            foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (img != null && img.name == BattleWorld.PopIconName) found.Add(img);
            return found;
        }
        static List<Image> NewSince(List<Image> before)
        {
            var was = new HashSet<int>(); foreach (var i in before) if (i != null) was.Add(i.GetInstanceID());
            var news = new List<Image>();
            foreach (var i in PopIcons()) if (i != null && !was.Contains(i.GetInstanceID())) news.Add(i);
            return news;
        }

        [UnityTest]
        public IEnumerator ADamagePopWearsTheIconOfThePerkThatCausedIt()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            yield return RealSeconds(0.3f);

            // ⓐ 번개 특전이 낸 타격 — 그 특전 카드가 쓰는 그림 그대로(새 그림 0)
            string boltIcon = BattleWorld.SrcIcon("p_killBolt");
            var want = _app.Assets.Sprite(boltIcon);
            Assert.IsNotNull(want, "카탈로그에 " + boltIcon + " 가 있어야 한다 — 특전 카드가 쓰는 그 키다");

            var before = PopIcons();
            world.Handle(new BattleEvent { Kind = EvKind.Hit, Value = 321, Src = "p_killBolt" });
            yield return Frames(2);
            var made = NewSince(before);
            Assert.AreEqual(1, made.Count, "출처가 실린 타격 팝에는 아이콘이 하나 붙는다(T458 2항)");
            Assert.AreSame(want, made[0].sprite, "번개 특전이 낸 데미지에는 그 특전의 그림이 붙는다(주인 «해당 꺼 아이콘»)");
            Assert.Less(made[0].rectTransform.anchoredPosition.x, 0f, "그림은 숫자 «왼쪽» 이다(T152 의 자리 그대로)");

            // ⓑ 출처가 없으면 안 붙인다 — «아는 척» 을 막는 갈래
            before = PopIcons();
            world.Handle(new BattleEvent { Kind = EvKind.Hit, Value = 77 });
            yield return Frames(2);
            Assert.IsEmpty(NewSince(before), "출처도 치명타도 없는 기본 타격에는 아무 그림도 안 붙는다");

            // ⓒ 출처와 치명타가 겹치면 «출처» 가 이긴다 — 치명타는 색(PopCrit)과 크기로도 말한다(결정 1305)
            before = PopIcons();
            world.Handle(new BattleEvent { Kind = EvKind.Hit, Value = 999, Crit = true, Src = "p_killBolt" });
            yield return Frames(2);
            made = NewSince(before);
            Assert.AreEqual(1, made.Count, "겹쳐도 그림은 하나다");
            Assert.AreSame(want, made[0].sprite, "겹치면 출처가 이긴다 — 치명타는 색·크기로도 말하지만 «왜 떴는지» 는 이 그림뿐이다");

            _log.AssertNoRed("출처 아이콘");
            yield return Shutdown();
        }

        /// <summary>장비·펫이 낸 것도 제 그림을 쓴다 — 특전 id 가 아닌 두 낱말(<c>gear</c>·<c>pet</c>)이 화면에서 안 흘러 떨어지는지 잰다.</summary>
        [UnityTest]
        public IEnumerator GearAndPetSourcesMapToTheirOwnIcons()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            yield return RealSeconds(0.3f);

            foreach (var src in new[] { BattleState.SrcGear, BattleState.SrcPet })
            {
                string key = BattleWorld.SrcIcon(src);
                Assert.IsNotNull(key, "«" + src + "» 의 그림 키가 없다");
                var want = _app.Assets.Sprite(key);
                Assert.IsNotNull(want, "카탈로그에 " + key + " 가 있어야 한다(«" + src + "»)");

                var before = PopIcons();
                world.Handle(new BattleEvent { Kind = EvKind.Hit, Value = 12, Src = src });
                yield return Frames(2);
                var made = NewSince(before);
                Assert.AreEqual(1, made.Count, "«" + src + "» 가 낸 타격에도 그림 하나");
                Assert.AreSame(want, made[0].sprite, "«" + src + "» 는 제 그림을 쓴다");
            }

            // 모르는 낱말은 안 붙인다(엔진이 언젠가 새 낱말을 실어도 화면이 엉뚱한 그림을 그리지 않는다)
            Assert.IsNull(BattleWorld.SrcIcon("무언가_새로운_것"), "모르는 출처에는 그림을 안 고른다");
            Assert.IsNull(BattleWorld.SrcIcon(null), "출처가 없으면 그림도 없다");

            _log.AssertNoRed("장비·펫 출처 아이콘");
            yield return Shutdown();
        }
    }
}
