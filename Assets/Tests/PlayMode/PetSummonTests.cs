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
    /// T293 ⓘ 첫 조각 — 펫 화면(13)의 <b>소환 버튼 두 벌</b>이 Core 규칙에 배선됐는가.
    /// <list type="bullet">
    /// <item>«무엇으로 몇 번» 은 화면이 세지 않는다 — <see cref="Pets.Offer"/> 가 답하고 <see cref="Pets.Draw"/> 가 치른다(T293 ⓓ 의 계약).</item>
    /// <item>못 치르면 <b>세이브가 한 글자도 안 바뀐다</b>(눌리는데 아무 일도 안 나는 자리를 안 만든다 · 결정 771 — 대신 까닭을 토스트로).</item>
    /// <item>업적 <c>petGacha</c> 는 <b>뽑은 횟수</b>로 오른다(누른 횟수가 아니다 · x10 = 10) — T258 이 마지막까지 기다린 훅이 이 자리다.</item>
    /// </list>
    /// <para>⚠ 값(100·1,000·70/25/5)은 여기서 안 잰다 — 그것은 EditMode <c>PetGachaTests</c>·<c>PetTests</c> 몫이고, 이 자는 <b>배선</b>만 본다.</para>
    /// </summary>
    public class PetSummonTests
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
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>가진 펫 수 + 조각 수 = 여태 뽑힌 마리 수(중복은 조각으로 쌓인다).</summary>
        static int Drawn(SaveData s)
        {
            int n = 0;
            foreach (var kv in s.PetLv) if (kv.Value >= 1) n++;
            foreach (var kv in s.PetFrag) n += kv.Value;
            return n;
        }

        [UnityTest]
        public IEnumerator 펫알로_소환하면_알이_줄고_펫이_들어오고_업적이_오른다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Pet : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("펫 표가 없다 — 부팅이 안 들었다(T293 ⓗ)"); }

            _app.Save.PetEgg = 3;                       // 캡보다 적다 ⇒ «소환» 버튼이 펫알로 3회(T293 ⓓ)
            _app.ShowScreen("pet"); yield return Frames(1);
            var btn = UiKit.Find(_app.Current.Root, "SummonBtn");
            Assert.IsNotNull(btn, "소환 버튼");

            int achBefore = Achievement.Count(_app.Save, Quests.AchPetGacha);
            btn.GetComponent<Button>().onClick.Invoke();
            yield return Frames(1);

            Assert.AreEqual(0, _app.Save.PetEgg, 1e-9, "치른 만큼 펫알이 빠진다");
            Assert.AreEqual(3, _app.Save.PetPulls, "누적 뽑기는 «뽑은 횟수»");
            Assert.AreEqual(3, Drawn(_app.Save), "세 번 뽑았으면 «가진 마리 + 조각» 이 셋이다(한 마리도 안 버려진다)");
            Assert.AreEqual(achBefore + 3, Achievement.Count(_app.Save, Quests.AchPetGacha),
                            "업적 petGacha 는 뽑은 횟수로 오른다(T258 이 기다린 훅)");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 치를_것이_없으면_세이브가_한_글자도_안_바뀐다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Pet : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("펫 표가 없다"); }

            _app.Save.PetEgg = 0; _app.Save.Gem = 0;    // 펫알도 다이아도 없다 ⇒ 다이아 값이 뜨고 못 치른다
            _app.ShowScreen("pet"); yield return Frames(1);
            var btn = UiKit.Find(_app.Current.Root, "SummonBtn");
            Assert.IsNotNull(btn, "소환 버튼");

            string before = _app.Save.ToJson();
            btn.GetComponent<Button>().onClick.Invoke();
            yield return Frames(1);
            Assert.AreEqual(before, _app.Save.ToJson(), "못 치르면 반쯤 치르는 자리도 없다(까닭은 토스트가 말한다)");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 소환_버튼의_값은_세이브를_따라_펫알과_다이아를_오간다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Pet : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("펫 표가 없다"); }
            int cap = _app.Data.Gacha != null ? _app.Data.Gacha.TenPullCount : 10;

            _app.Save.PetEgg = 0;
            _app.ShowScreen("pet"); yield return Frames(1);
            var root = _app.Current.Root;
            Assert.AreEqual("hud.gem", CostIcon(root, "SummonBtn"), "펫알이 없으면 다이아 값이다");
            Assert.AreEqual("hud.gem", CostIcon(root, "Summon10Btn"), "x10 도 다이아");

            _app.Save.PetEgg = cap; _app.Current.Refresh(); yield return Frames(1);
            Assert.AreEqual("pet.egg", CostIcon(root, "SummonBtn"), "펫알이 캡만큼 있으면 둘 다 펫알로 바뀐다");
            Assert.AreEqual("pet.egg", CostIcon(root, "Summon10Btn"), "두 버튼이 «같은 함수» 로 같이 바뀐다(따로 세지 않는다)");

            yield return Shutdown();
        }

        /// <summary>버튼의 값 줄이 지금 어느 그림을 쓰고 있나 — 카탈로그 키로 되짚는다(그림 파일이 아니라 «무엇으로 치르나» 를 재는 자리).</summary>
        static string CostIcon(Transform root, string btnName)
        {
            var btn = UiKit.Find(root, btnName); Assert.IsNotNull(btn, btnName);
            var cost = UiKit.Find(btn, "Cost"); Assert.IsNotNull(cost, btnName + " 의 값 줄");
            var img = UiKit.Find(cost, "Icon"); Assert.IsNotNull(img, btnName + " 의 값 아이콘");
            var sp = img.GetComponent<Image>().sprite;
            // 카탈로그가 그 키로 주는 그림과 견준다 — 파일 이름을 자에 박지 않는다(그림을 갈아 끼워도 이 자는 그대로 산다).
            var cat = App.I != null ? App.I.Assets : null;
            return sp != null && cat != null && sp == cat.Sprite("pet.egg") ? "pet.egg" : "hud.gem";
        }
    }
}
