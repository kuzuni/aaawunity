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


        [UnityTest]
        public IEnumerator 세부_팝업이_표와_세이브를_그대로_말하고_강화가_돈다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Pet : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("펫 표가 없다"); }

            // 표의 첫 펫을 «가진 것 + 강화할 수 있는 것» 으로 만든다(규칙은 Core 가 낸다 — 자가 수를 안 적는다).
            var pet = d.Pets[0];
            Pets.Gain(_app.Save, pet.Id);
            int need = Pets.Need(d, 1);
            for (int i = 0; i < need; i++) Pets.Gain(_app.Save, pet.Id);
            Assert.IsTrue(Pets.CanLevelUp(d, _app.Save, pet.Id), "전제 — 조각이 찼다");
            _app.Persist();

            _app.ShowScreen("pet"); yield return Frames(1);
            ((PetScreen)_app.Current).OpenDetail(0); yield return Frames(1);
            var ov = _app.Overlay.Root;

            var up = UiKit.Find(ov, "PetUpgradeBtn"); Assert.IsNotNull(up, "강화 버튼");
            Assert.IsTrue(up.GetComponent<Button>().interactable, "강화할 수 있으면 눌린다(주인 «가능할 때는 주황»)");
            int questBefore = QuestRun.Count(_app.Save, true, Quests.PetUpgrade);

            up.GetComponent<Button>().onClick.Invoke(); yield return Frames(2);

            Assert.AreEqual(2, Pets.Lv(_app.Save, pet.Id), "강화하면 Lv 가 하나 오른다");
            Assert.AreEqual(0, Pets.Frag(_app.Save, pet.Id), "쓴 조각만큼 빠진다");
            Assert.AreEqual(questBefore + 1, QuestRun.Count(_app.Save, true, Quests.PetUpgrade),
                            "퀘스트 «펫 강화» 카운터는 이 자리 하나에서 오른다(T257 ⓑ)");

            // 다시 열린 팝업은 새 레벨을 말한다(팝업이 스스로 다시 열린다)
            var ov2 = _app.Overlay.Root;
            var up2 = UiKit.Find(ov2, "PetUpgradeBtn"); Assert.IsNotNull(up2, "다시 열린 팝업의 강화 버튼");
            Assert.AreEqual(Pets.CanLevelUp(d, _app.Save, pet.Id), up2.GetComponent<Button>().interactable,
                            "옷·눌림은 규칙 하나를 따른다(주황인데 안 눌리는 자리를 안 만든다)");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 장착_버튼이_칸을_채우고_합계가_따라_오른다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Pet : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("펫 표가 없다"); }

            var pet = d.Pets[0];
            Pets.Gain(_app.Save, pet.Id); _app.Persist();
            Assert.AreEqual(0, Pets.Equipped(d, _app.Save).Count, "전제 — 아직 아무것도 안 꼈다");

            _app.ShowScreen("pet"); yield return Frames(1);
            ((PetScreen)_app.Current).OpenDetail(0); yield return Frames(1);
            var eq = UiKit.Find(_app.Overlay.Root, "PetEquipBtn"); Assert.IsNotNull(eq, "장착 버튼");
            eq.GetComponent<Button>().onClick.Invoke(); yield return Frames(2);

            var worn = Pets.Equipped(d, _app.Save);
            Assert.AreEqual(1, worn.Count, "열린 칸에 한 마리가 낀다");
            Assert.AreEqual(pet.Id, worn[0], "낀 것은 그 펫이다");
            var sum = Pets.EquipPower(_app.Data, d, _app.Save);
            Assert.Greater(sum.Atk, 0, "합계가 0 이면 «더하는 자리» 가 안 붙은 것이다");

            // 한 번 더 누르면 해제 — 같은 버튼이 두 뜻을 갖는다(낀 칸이 있으면 «해제»)
            var eq2 = UiKit.Find(_app.Overlay.Root, "PetEquipBtn"); Assert.IsNotNull(eq2, "다시 열린 팝업의 장착 버튼");
            eq2.GetComponent<Button>().onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(0, Pets.Equipped(d, _app.Save).Count, "다시 누르면 해제된다");

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
