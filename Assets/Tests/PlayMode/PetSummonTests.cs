using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;
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
            // T380 — 윗줄 «N회» 는 Offer 의 Count 다(펫알 3개 ⇒ «3회» · «1회» 라 적고 3회가 나가면 거짓말).
            Assert.AreEqual(PetScreen.CountLabel(3), UiKit.Find(btn, "Label").GetComponent<TMP_Text>().text, "펫알 3개면 윗줄이 «3회» 다(T380)");

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

            // 패시브 줄 — **공·체·실 셋**이 표에서 온 값 그대로다(주인 «장착 효과 있음 — 공·체·실 채워 줌»).
            //   ⚑ 레퍼런스 14 는 둘(🗡·🛡)뿐이라 화면도 둘만 그리고 있었다 — 옛 HTML 판의 펫은 체력을 안 줬고, 우리 펫은 준다(그림이 아니라 지시를 따른다 · 슬롯 4 → 3 과 같은 자리).
            //   기댓값은 자가 다시 안 적고 화면이 읽는 그 함수(`Pets.Equip`)로 되짚는다 · 차례는 13 의 합계 줄과 같다(❤ · 🛡 · 🗡).
            var pv = UiKit.Find(ov, "PassiveRow"); Assert.IsNotNull(pv, "패시브 수치 줄");
            var eq = Pets.Equip(_app.Data, d, pet, Pets.Lv(_app.Save, pet.Id));
            Assert.Greater(System.Math.Round(eq.Hp), 0, "이 펫은 체력을 준다 — 0 이면 아래가 «둘만 그린다» 와 구별이 안 된다");
            var nums = new System.Collections.Generic.List<string>();
            foreach (var t in pv.GetComponentsInChildren<TMP_Text>(true)) if (t != null && t.text != "|") nums.Add(t.text);
            Assert.AreEqual(3, nums.Count, "패시브는 셋(공·체·실)이다 — 둘이면 체력이 화면에서 사라진 것이다");
            Assert.AreEqual("+" + UiKit.FmtQty(System.Math.Round(eq.Hp)), nums[0], "첫 칸 = 체력(13 의 합계 줄과 같은 차례)");
            Assert.AreEqual("+" + UiKit.FmtQty(System.Math.Round(eq.Sh)), nums[1], "둘째 칸 = 실드");
            Assert.AreEqual("+" + UiKit.FmtQty(System.Math.Round(eq.Atk)), nums[2], "셋째 칸 = 공격");
            // 세부 칸도 그 펫의 등급색(주인 5항 ⓘ) — 표의 첫 펫은 일반이라 파랑이면 옛 «파랑 하나» 가 남은 것이다.
            var gd = d.GradeOfPet(pet); Assert.IsNotNull(gd, "표의 등급");
            Assert.IsNotNull(UiKit.Find(UiKit.Find(ov, "PetDetailCell"), "ui.itemFrame." + Palette.RarName(gd.Rar)), "세부 칸 프레임 = 그 펫의 등급색");

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


        [UnityTest]
        public IEnumerator 전체_강화와_빠른_장착이_실제로_돈다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Pet : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("펫 표가 없다"); }

            // 두 마리를 가지게 하고, 한 마리는 강화까지 되게 조각을 채운다(수는 Core 가 낸다).
            var a = d.Pets[0]; var b = d.Pets[d.Pets.Count - 1];
            Pets.Gain(_app.Save, a.Id); Pets.Gain(_app.Save, b.Id);
            for (int i = 0; i < Pets.Need(d, 1); i++) Pets.Gain(_app.Save, a.Id);
            _app.Persist();
            int upsWanted = 0;
            foreach (var p in d.Pets) if (Pets.CanLevelUp(d, _app.Save, p.Id)) upsWanted++;
            Assert.Greater(upsWanted, 0, "전제 — 올릴 수 있는 펫이 있다");

            _app.ShowScreen("pet"); yield return Frames(1);
            var root = _app.Current.Root;
            int questBefore = QuestRun.Count(_app.Save, true, Quests.PetUpgrade);

            var up = UiKit.Find(root, "UpgradeAllBtn"); Assert.IsNotNull(up, "전체 강화 버튼");
            // T380(주인 «강화할 거리 있으면 빨간점») — 올릴 것이 있으면 점이 켜져 있고, 다 올린 뒤에는 꺼진다. 판정은 화면과 같은 한 값(UpgradableCount)이다.
            var upDot = UiKit.Find(up, PetScreen.UpgradeDotName); Assert.IsNotNull(upDot, "전체 강화 버튼의 빨간 점(이름 계약)");
            Assert.IsTrue(upDot.gameObject.activeSelf, "올릴 펫이 있으면 «전체 강화» 점이 켜진다(T380)");
            up.GetComponent<Button>().onClick.Invoke(); yield return Frames(1);
            foreach (var p in d.Pets)
                Assert.IsFalse(Pets.CanLevelUp(d, _app.Save, p.Id), "«전체» 는 더 못 올릴 때까지 올린다 — " + p.Name);
            Assert.IsFalse(upDot.gameObject.activeSelf, "다 올린 뒤에는 «전체 강화» 점이 꺼진다(T380)");
            Assert.AreEqual(questBefore + upsWanted, QuestRun.Count(_app.Save, true, Quests.PetUpgrade),
                            "퀘스트 카운터는 «올린 횟수만큼» 오른다(세부 팝업의 강화와 같은 수)");

            // 빠른 장착 — 열린 빈 칸을 «등급 → 레벨 → 표 차례» 로 채운다(이미 낀 것은 안 건드린다)
            int open = Pets.SlotsOpen(d, _app.Save);
            Assert.Greater(open, 0, "전제 — 첫 칸은 0회부터 열려 있다");
            var qe = UiKit.Find(root, "QuickEquipBtn"); Assert.IsNotNull(qe, "빠른 장착 버튼");
            var qeDot = UiKit.Find(qe, PetScreen.QuickEquipDotName); Assert.IsNotNull(qeDot, "빠른 장착 버튼의 빨간 점(이름 계약)");
            Assert.IsTrue(qeDot.gameObject.activeSelf, "빈 칸 + 안 낀 펫이 있으면 «빠른 장착» 점이 켜진다(T380)");
            qe.GetComponent<Button>().onClick.Invoke(); yield return Frames(1);
            Assert.IsFalse(qeDot.gameObject.activeSelf, "채운 뒤(빈 칸이 없거나 남은 펫이 없으면) «빠른 장착» 점이 꺼진다(T380)");
            var worn = Pets.Equipped(d, _app.Save);
            Assert.AreEqual(Mathf.Min(open, 2), worn.Count, "열린 칸만큼(가진 만큼) 채운다");
            var g0 = d.GradeOfPet(d.Of(worn[0]));
            foreach (var id in worn)
            {
                var g = d.GradeOfPet(d.Of(id));
                Assert.LessOrEqual(g != null ? g.Rar : -1, g0 != null ? g0.Rar : -1, "등급 높은 것부터 들어간다");
            }

            // 한 번 더 눌러도 이미 찼으면 아무 일도 안 난다(«빠른» 은 다시 짜기가 아니다)
            var before = _app.Save.ToJson();
            qe.GetComponent<Button>().onClick.Invoke(); yield return Frames(1);
            Assert.AreEqual(before, _app.Save.ToJson(), "채울 칸이 없으면 세이브는 그대로다(까닭은 토스트가 말한다)");

            yield return Shutdown();
        }


        [UnityTest]
        public IEnumerator 격자는_가진_펫만_그리고_0마리면_한_줄로_말한다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Pet : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("펫 표가 없다"); }

            // ⓐ 0마리 — 칸이 하나도 안 켜지고 «무엇을 하면 되는지» 한 줄이 뜬다(주인 5항 ⓙ «얻은 거만 보이게»)
            _app.ShowScreen("pet"); yield return Frames(1);
            var root = _app.Current.Root;
            Assert.AreEqual(0, ShownCells(root), "아무것도 안 가졌으면 칸도 하나도 없다(빈 칸을 안 남긴다)");
            var hint = UiKit.Find(root, "EmptyHint");
            Assert.IsNotNull(hint, "0마리 안내 줄");
            Assert.IsTrue(hint.gameObject.activeInHierarchy, "0마리면 안내 줄이 뜬다");

            // ⓑ 두 마리 — 칸이 딱 둘, 안내 줄은 꺼지고, 앞자리가 등급이 높다
            var low = d.Pets[0]; var high = d.Pets[d.Pets.Count - 1];
            Pets.Gain(_app.Save, low.Id); Pets.Gain(_app.Save, high.Id); _app.Persist();
            _app.Current.Refresh(); yield return Frames(1);
            Assert.AreEqual(2, ShownCells(root), "가진 만큼만 켜진다");
            Assert.IsFalse(hint.gameObject.activeInHierarchy, "한 마리라도 있으면 안내 줄은 꺼진다");

            var g0 = d.GradeOfPet(low); var g1 = d.GradeOfPet(high);
            if (g0 != null && g1 != null && g0.Rar != g1.Rar)
            {
                // 앞 칸을 눌러 세부 팝업을 열면 «등급 높은 쪽» 이 나온다 — 격자 차례가 «빠른 장착» 과 같은 규칙이라는 뜻이다
                ((PetScreen)_app.Current).OpenDetail(0); yield return Frames(1);
                var eq = UiKit.Find(_app.Overlay.Root, "PetEquipBtn");
                Assert.IsNotNull(eq, "세부 팝업이 열린다");
                eq.GetComponent<Button>().onClick.Invoke(); yield return Frames(2);
                var worn = Pets.Equipped(d, _app.Save);
                Assert.AreEqual(1, worn.Count, "그 칸의 펫이 장착된다");
                Assert.AreEqual(g0.Rar > g1.Rar ? low.Id : high.Id, worn[0], "앞 칸 = 등급이 높은 펫(격자 차례 = 등급 내림차순)");
            }

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 펫을_끼면_전투력_숫자와_장비_스탯_셋이_같이_오른다()
        {
            // T293 ⓖ 마지막 어긋남 — 주인 2026-09-10 «펫 전투력 숫자에 들어가야지 · 장착할 때 공체실 늘어나게».
            //   판은 이미 펫을 세고 있었는데(`RunOptions.PetPower`) 화면은 안 셌다. 이 자는 **화면 쪽**을 잰다:
            //   ⓐ 전투력 숫자(`App.Power()` · 상단 바가 찍는 그 값) · ⓑ 장비 화면 스탯 3칸.
            //   ⚑ 기댓값을 자가 다시 세지 않는다 — 화면이 읽는 그 함수(`Pets.TotalPower`)로 되짚는다(값이 바뀌면 자도 같이 움직인다).
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Pet : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("펫 표가 없다 — 부팅이 안 들었다(T293 ⓗ)"); }

            _app.ShowScreen("gear"); yield return Frames(2);
            var gear = _app.Current.Root;
            double power0 = _app.Power();
            string atk0 = StatText(gear, "atk"), hp0 = StatText(gear, "hp"), sh0 = StatText(gear, "sh");

            // 전설 한 마리를 얻어 첫 칸에 끼운다(새 세이브에서 첫 칸은 열려 있다 · 등급이 높을수록 더하는 몫이 커서 숫자가 확실히 움직인다)
            var id = d.Pets[d.Pets.Count - 1].Id;
            Pets.Gain(_app.Save, id);
            Assert.IsTrue(Pets.Equip(d, _app.Save, id, 0), "새 세이브의 첫 장착 칸은 열려 있다");
            _app.Persist();
            var add = Pets.EquipPower(_app.Data, d, _app.Save);
            Assert.Greater(add.Atk + add.Hp + add.Sh, 0, "낀 펫이 더하는 몫이 0 이면 아래 단언들이 뜻이 없다");

            _app.Current.Refresh(); yield return Frames(1);
            Assert.Greater(_app.Power(), power0, "펫을 끼면 전투력 숫자가 오른다(주인 «전투력 숫자에 들어가야지»)");

            // ⓒ 실드 — «실드가 있으면 방패»(`HeroView.PlayerSkin`)가 읽는 그 수에 **낀 펫이 든다**.
            //   ⛑ 런 962 빨강(워커 I · 결정 1108) — 앞 회차에 내가 여기 «새 세이브는 장비 실드가 0» 을 공허 방지로 박았는데 **그 전제가 거짓**이었다:
            //     `GearSystem.BuildPower` 는 이름과 달리 «장비만» 이 아니라 **기저 + 장비**다(`(T.PSh0 + sh) * ev` · 표 `pSh0` = 250).
            //     ⇒ 전제를 «장비가 실드를 **더하지 않는다**»(무장비 기준선과 같다)로 바로 적는다 — 그것이 이 자리에서 참인 문장이다.
            //   ⚠ 그리고 «방패가 있다» 는 **안 잰다** — 기저 실드가 250 이라 `TotalPower.Sh > 0` 은 **펫이 있든 없든 참**이고,
            //     그 자리에 단언을 두면 «지키는 자» 를 세운 기분만 남는다(결정 1108 ②). 대신 **그 수가 실제로 펫만큼 오르는가**를 잰다.
            var bare = GearSystem.BuildPower(_app.Data, GearSystem.MkBuild(_app.Data, -1, 0, 0));
            var geared = GearSystem.BuildPower(_app.Data, _app.Save.CurBuild(_app.Data));
            Assert.AreEqual(bare.Sh, geared.Sh, 1e-6, "새 세이브는 장비가 실드를 안 더한다(기저 기준선 그대로) — 여기가 갈리면 아래가 «펫 때문» 을 못 말한다");
            Assert.Greater(add.Sh, 0, "낀 펫이 실드를 줘야 아래가 뜻이 있다");
            Assert.AreEqual(geared.Sh + add.Sh, Pets.TotalPower(_app.Data, _app.Save).Sh, 1e-6,
                            "«실드가 있으면 방패» 가 읽는 그 수(TotalPower.Sh)에 낀 펫의 실드가 든다");

            var pw = Pets.TotalPower(_app.Data, _app.Save);
            Assert.AreEqual(UiKit.Fmt(System.Math.Round(pw.Atk)), StatText(gear, "atk"), "장비 화면 공격력 = 장비 + 낀 펫");
            Assert.AreEqual(UiKit.Fmt(System.Math.Round(pw.Hp)), StatText(gear, "hp"), "체력도 같은 값");
            Assert.AreEqual(UiKit.Fmt(System.Math.Round(pw.Sh)), StatText(gear, "sh"), "실드도 같은 값");
            Assert.AreNotEqual(atk0 + "/" + hp0 + "/" + sh0,
                               StatText(gear, "atk") + "/" + StatText(gear, "hp") + "/" + StatText(gear, "sh"),
                               "세 칸이 하나도 안 움직였으면 펫 몫이 화면에 안 들어온 것이다(주인 «장착할 때 공체실 늘어나게»)");

            // 빼면 되돌아온다 — 더하기만 붙고 «끼는 동안만» 이 안 지켜지면 여기서 운다.
            Assert.IsTrue(Pets.Unequip(_app.Save, 0), "칸을 비운다"); _app.Persist();
            _app.Current.Refresh(); yield return Frames(1);
            Assert.AreEqual(power0, _app.Power(), 1e-6, "빼면 전투력이 원래대로");
            Assert.AreEqual(atk0, StatText(gear, "atk"), "빼면 스탯도 원래대로");
            Assert.AreEqual(geared.Sh, Pets.TotalPower(_app.Data, _app.Save).Sh, 1e-6, "빼면 실드도 기저 + 장비로 돌아온다");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 칸_프레임은_그_펫의_등급색이다()
        {
            // T293 5항 ⓘ(주인 2026-09-09 11:1X «슬롯 부분 등급마다 색» + «격자 칸도 등급색») — 격자 칸은 여태 **파랑 하나**로 서 있었다.
            //   ⚑ 색 이름은 자가 다시 안 적는다 — 표의 등급 번호 → `Palette.RarName` 으로 되짚는다(색을 바꾸면 자도 따라간다).
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Pet : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("펫 표가 없다 — 부팅이 안 들었다(T293 ⓗ)"); }

            PetData.Pet low = null, high = null;
            foreach (var p in d.Pets)
            {
                var g = d.GradeOfPet(p); if (g == null) continue;
                if (low == null || g.Rar < d.GradeOfPet(low).Rar) low = p;
                if (high == null || g.Rar > d.GradeOfPet(high).Rar) high = p;
            }
            Assert.IsNotNull(low); Assert.IsNotNull(high);
            string kLow = "ui.itemFrame." + Palette.RarName(d.GradeOfPet(low).Rar);
            string kHigh = "ui.itemFrame." + Palette.RarName(d.GradeOfPet(high).Rar);
            Assert.AreNotEqual(kLow, kHigh, "등급 둘의 색 키가 같으면 이 자는 아무것도 안 가른다(공허 방지)");

            Pets.Gain(_app.Save, low.Id); Pets.Gain(_app.Save, high.Id); _app.Persist();
            _app.ShowScreen("pet"); yield return Frames(2);
            var root = _app.Current.Root;

            // 격자 — 차례가 등급 내림차순이라 첫 칸이 높은 등급이다(그 차례 자체는 다른 자가 잰다).
            var c0 = UiKit.Find(root, "PetGrid/Pet:0/ItemFrame_01"); Assert.IsNotNull(c0, "격자 첫 칸");
            Assert.IsNotNull(UiKit.Find(c0, kHigh), "격자 첫 칸은 그 펫의 등급색 변형을 쓴다(주인 «격자 칸도 등급색»)");
            Assert.IsNull(UiKit.Find(c0, kLow), "옛 색 조각이 같이 남아 있으면 «갈아 끼우기» 가 아니라 «겹치기» 다");
            var c1 = UiKit.Find(root, "PetGrid/Pet:1/ItemFrame_01"); Assert.IsNotNull(c1, "격자 둘째 칸");
            Assert.IsNotNull(UiKit.Find(c1, kLow), "둘째 칸은 낮은 등급의 색");

            // 장착 칸 — 끼우면 그 등급색, 빼면 빈 칸(회색)으로 돌아온다.
            Assert.IsTrue(Pets.Equip(d, _app.Save, high.Id, 0), "새 세이브의 첫 칸은 열려 있다");
            _app.Persist(); _app.Current.Refresh(); yield return Frames(1);
            var slot0 = UiKit.Find(root, "Slots/Slot:0/FramePart/ItemFrame_01"); Assert.IsNotNull(slot0, "열린 장착 칸");
            Assert.IsNotNull(UiKit.Find(slot0, kHigh), "낀 펫의 등급색(주인 «슬롯 부분 등급마다 색»)");
            Assert.IsTrue(Pets.Unequip(_app.Save, 0), "칸을 비운다");
            _app.Persist(); _app.Current.Refresh(); yield return Frames(1);
            Assert.IsNotNull(UiKit.Find(slot0, "ui.itemFrame." + Palette.RarColors[0]), "빈 칸은 회색으로 돌아온다");
            Assert.IsNull(UiKit.Find(slot0, kHigh), "뺐는데 옛 등급색이 남아 있으면 안 된다");

            yield return Shutdown();
        }

        /// <summary>장비 화면 스탯 칸의 숫자 글자(<c>Stat:atk</c>·<c>Stat:hp</c>·<c>Stat:sh</c>).</summary>
        static string StatText(Transform gear, string key)
        {
            var cell = UiKit.Find(gear, "Stat:" + key); Assert.IsNotNull(cell, "스탯 칸 " + key);
            var t = cell.GetComponentInChildren<TMP_Text>(true); Assert.IsNotNull(t, "스탯 칸 " + key + " 숫자");
            return t.text;
        }

        [UnityTest]
        public IEnumerator 칸_그림은_메이커로_만든_그_펫이다()
        {
            // T396(주인 2026-09-10 «아이콘에도 해당 메이커로 만든 펫 모양으로 아이콘 · 플레이어 모양 펫인 겨 걍») —
            //   칸 그림이 GUI Pro 아이콘 아홉이었다. 이제 `CharacterRig.PetSkin` 으로 세운 **초상**(HeroView)이 선다.
            //   ⚑ 기댓값을 자가 다시 안 적는다 — 화면이 쓰는 그 함수(`CharacterRig.PetSkin`)로 되짚는다.
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Pet : null;
            if (d == null) { yield return Shutdown(); Assert.Ignore("펫 표가 없다 — 부팅이 안 들었다(T293 ⓗ)"); }

            PetData.Pet low = null, high = null;
            foreach (var p in d.Pets)
            {
                var g = d.GradeOfPet(p); if (g == null) continue;
                if (low == null || g.Rar < d.GradeOfPet(low).Rar) low = p;
                if (high == null || g.Rar > d.GradeOfPet(high).Rar) high = p;
            }
            Pets.Gain(_app.Save, low.Id); Pets.Gain(_app.Save, high.Id); _app.Persist();
            _app.ShowScreen("pet"); yield return Frames(2);
            var root = _app.Current.Root;

            var want = CharacterRig.PetSkin(d, high);
            Assert.IsNotEmpty(want.Helmet ?? "", "표가 그 펫의 투구 키를 줘야 아래가 뜻이 있다(PetLook)");
            var face0 = Face(root, "PetGrid/Pet:0");
            Assert.IsNotNull(face0, "격자 첫 칸 그림 = 메이커 펫 초상");
            Assert.AreEqual(want.Helmet, face0.Skin.Helmet, "그 펫의 투구");
            Assert.AreEqual(want.Chest, face0.Skin.Chest, "그 펫의 갑옷");
            Assert.IsTrue(face0.Still, "격자 초상은 정지다 — 칸 아홉이 저마다 걸으면 격자가 시끄럽다(로비 아바타 T68 ② 와 같은 규칙)");
            var im0 = UiKit.Find(root, "PetGrid/Pet:0/ItemFrame_01/Item").GetComponent<UnityEngine.UI.Image>();
            Assert.IsFalse(im0 != null && im0.enabled, "조각 아이콘은 꺼진다 — 안 끄면 옛 그림과 초상이 겹쳐 그려진다");

            // 칸마다 제 펫이 선다(한 벌을 돌려쓰면 둘째 칸이 첫째와 같아진다)
            var face1 = Face(root, "PetGrid/Pet:1");
            Assert.IsNotNull(face1, "둘째 칸 초상");
            Assert.AreNotEqual(face0.Skin.Helmet + "|" + face0.Skin.Chest, face1.Skin.Helmet + "|" + face1.Skin.Chest, "칸마다 제 펫이 선다");

            // 장착 칸·세부 팝업도 같은 그림이다(주인 «장착 시 …» · 한 화면에서 같은 펫이 다른 모습이면 안 된다)
            Assert.IsTrue(Pets.Equip(d, _app.Save, high.Id, 0), "새 세이브의 첫 칸은 열려 있다");
            _app.Persist(); _app.Current.Refresh(); yield return Frames(1);
            var slotFace = Face(root, "Slots/Slot:0/FramePart");
            Assert.IsNotNull(slotFace, "낀 장착 칸 그림도 메이커 펫");
            Assert.AreEqual(want.Helmet, slotFace.Skin.Helmet, "장착 칸도 그 펫");

            ((PetScreen)_app.Current).OpenDetail(0); yield return Frames(1);
            var detFace = Face(_app.Overlay.Root, "PetDetailCell");
            Assert.IsNotNull(detFace, "세부 팝업 칸 그림도 메이커 펫");
            Assert.AreEqual(want.Helmet, detFace.Skin.Helmet, "세부 팝업도 그 펫");
            _app.Overlay.Close(); yield return Frames(1);

            yield return Shutdown();
        }

        /// <summary>그 칸 안에 선 메이커 초상(<see cref="HeroView"/>) — 없으면 null.</summary>
        static HeroView Face(Transform root, string path)
        {
            var t = UiKit.Find(root, path); if (t == null) return null;
            return t.GetComponentInChildren<HeroView>(true);
        }

        /// <summary>지금 켜져 있는 격자 칸 수 — 꺼진 칸은 «안 그린 것» 이다(이름·자리는 그대로 살아 있다).</summary>
        static int ShownCells(Transform root)
        {
            var grid = UiKit.Find(root, "PetGrid"); if (grid == null) return 0;
            int n = 0;
            foreach (var t in grid.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Pet:") && t.gameObject.activeInHierarchy) n++;
            return n;
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
