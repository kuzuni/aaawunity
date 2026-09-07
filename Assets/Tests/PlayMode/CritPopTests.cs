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
    /// T152 — **치명타 데미지 팝은 «치명타 아이콘 + 데미지»**(주인 2026-09-07 «치명타 데미지일시에 데미지 텍스트 치명타 아이콘+데미지 이런식으로 해줘야함»).
    /// 전에는 숫자 뒤에 «!» 만 붙었다. 그림은 스탯 «치명타 확률» 이 쓰는 <c>pi.critical</c> 그대로다(새 그림 0).
    /// <para>
    /// 재는 법 — 실제 전투를 세우고 <see cref="BattleWorld.Handle"/> 로 치명타 이벤트 하나를 흘린다(엔진 0줄 · 이 자는 연출만 본다).
    /// 전투는 제 이벤트로도 팝을 계속 띄우므로 «화면의 아이콘 개수» 가 아니라 **이 이벤트가 새로 만든 것**만 집어 판정한다.
    /// </para>
    /// </summary>
    public class CritPopTests
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

        /// <summary>지금 화면에 떠 있는 팝 아이콘들(이름이 아니라 <b>조각의 그림</b>으로도 확인한다 · T136 383 과 같은 결).</summary>
        static List<Image> PopIcons()
        {
            var found = new List<Image>();
            foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (img != null && img.name == BattleWorld.PopIconName) found.Add(img);
            return found;
        }
        /// <summary>이벤트 하나가 <b>새로</b> 만든 팝 아이콘만 집는다(전투가 제 이벤트로 띄우는 팝과 섞이지 않게).</summary>
        static List<Image> NewSince(List<Image> before)
        {
            var was = new HashSet<int>(); foreach (var i in before) if (i != null) was.Add(i.GetInstanceID());
            var news = new List<Image>();
            foreach (var i in PopIcons()) if (i != null && !was.Contains(i.GetInstanceID())) news.Add(i);
            return news;
        }

        [UnityTest]
        public IEnumerator ACriticalHitPopsTheCritIconNextToTheNumberAndNoBangMark()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            yield return RealSeconds(0.3f);

            var crit = _app.Assets.Sprite(BattleWorld.CritIconKey);
            Assert.IsNotNull(crit, "카탈로그에 " + BattleWorld.CritIconKey + " 가 있어야 한다(새 그림을 만들지 않는다)");

            // ⓐ 치명타 — 아이콘 하나가 숫자 «왼쪽» 에 붙고, 글자에는 «!» 가 없다
            var before = PopIcons();
            world.Handle(new BattleEvent { Kind = EvKind.Hit, Crit = true, Value = 1234 });
            yield return Frames(2);
            var made = NewSince(before);
            Assert.AreEqual(1, made.Count, "치명타 팝 하나에 아이콘도 하나여야 한다(T152)");
            var icon = made[0];
            Assert.AreSame(crit, icon.sprite, "팝 아이콘은 스탯 «치명타 확률» 과 같은 그림(" + BattleWorld.CritIconKey + ")이다");
            Assert.IsTrue(icon.preserveAspect, "그림이 찌그러지지 않는다");
            var irt = icon.rectTransform;
            Assert.AreEqual(irt.rect.width, irt.rect.height, 1f, "아이콘은 정사각(지름 하나)");
            Assert.Less(irt.anchoredPosition.x, 0f, "아이콘은 숫자 «왼쪽» 이다(주인 «아이콘+데미지» 순서)");

            var label = icon.transform.parent != null ? icon.transform.parent.GetComponent<Text>() : null;
            Assert.IsNotNull(label, "아이콘은 팝 글자의 자식이다(트윈 하나에 같이 따라 올라간다)");
            Assert.IsFalse(label.text.Contains("!"), "아이콘이 «치명타» 를 말하므로 숫자 뒤 «!» 는 빼야 한다 — 지금 글자: " + label.text);
            StringAssert.Contains(UiKit.Fmt(1234), label.text, "데미지 숫자는 그대로 뜬다");

            // ⓑ 보통 타격 — 아이콘이 붙지 않고 «!» 도 없다
            before = PopIcons();
            world.Handle(new BattleEvent { Kind = EvKind.Hit, Crit = false, Value = 77 });
            yield return Frames(2);
            Assert.IsEmpty(NewSince(before), "치명타가 아니면 아이콘을 붙이지 않는다");

            // ⓒ 누수 0 — 트윈이 끝나면 아이콘도 글자와 같이 사라진다
            float t0 = Time.realtimeSinceStartup;
            while (icon != null && Time.realtimeSinceStartup - t0 < 4f) yield return null;
            Assert.IsTrue(icon == null, "팝이 사라지면 아이콘도 같이 사라진다(누수 0)");

            _log.AssertNoRed("치명타 팝");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator ACriticalCounterUsesTheSameIconMarkAsTheDamagePop()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            yield return RealSeconds(0.3f);

            // T152 3항(결정) — 반격 팝도 «!» 대신 같은 아이콘으로 맞췄다: 같은 «치명타» 를 두 가지로 적지 않는다
            var before = PopIcons();
            world.Handle(new BattleEvent { Kind = EvKind.Counter, Crit = true, Value = 55 });
            yield return Frames(2);
            var made = NewSince(before);
            Assert.AreEqual(1, made.Count, "치명타 반격도 아이콘 하나");
            var label = made[0].transform.parent != null ? made[0].transform.parent.GetComponent<Text>() : null;
            Assert.IsNotNull(label, "팝 글자");
            Assert.IsFalse(label.text.Contains("!"), "반격 팝에서도 «!» 를 뺀다 — 지금 글자: " + label.text);
            StringAssert.StartsWith("반격", label.text, "«반격 N» 표기는 그대로");

            _log.AssertNoRed("반격 치명타 팝");
            yield return Shutdown();
        }
    }
}
