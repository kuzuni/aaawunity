using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T293 ⓕ — 펫이 <b>세이브에 담기는</b> 자리의 자(가진 펫·조각·장착 칸·누적 뽑기).
    /// <para>
    /// 앞 다섯 회차(ⓐ~ⓔ)는 «세이브를 안 보는 순수 규칙» 만 세웠다 — <c>Core/SaveData.cs</c> 가 남의 lock 안이었기 때문이다.
    /// 이 자는 그 규칙 위에 얹은 <b>담는 층</b>만 잰다: 규칙(70/25/5 · 필요치 · 해금 횟수)은 여기서 다시 안 센다(<c>PetTests</c> 몫).
    /// </para>
    /// <para>
    /// ⚑ 이 자가 지키는 것 셋 — ① <b>옛 세이브가 열린다</b>(이 필드들이 없던 세이브) · ② <b>한 바퀴 돌려도 같다</b>(저장 ↔ 불러오기) ·
    /// ③ <b>못 치르면 세이브가 한 글자도 안 바뀐다</b>(반쯤 치르고 실패하는 자리가 없다).
    /// </para>
    /// </summary>
    public class PetSaveTests
    {
        static PetData Load() => PetData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "pet.json"))));
        static SaveData NewSave() => SaveData.NewSave(TestData.Load());

        const int Cap = 10;   // 이 자에서만 쓰는 «지금 표의 값»(gacha.json tenPull.count) — 규칙에는 안 들어간다(PetGachaTests 가 그것을 지킨다)

        static string FirstId(PetData d) => d.Pets[0].Id;

        [Test]
        public void 펫_자리가_없던_옛_세이브도_그대로_열린다()
        {
            var G = TestData.Load();
            var old = SaveData.FromJson("{\"gold\":10,\"gem\":2,\"maxChapter\":5}", G);
            Assert.IsNotNull(old.PetLv, "없으면 빈 표(옛 세이브 호환)");
            Assert.AreEqual(0, old.PetLv.Count);
            Assert.AreEqual(0, old.PetFrag.Count);
            Assert.AreEqual(0, old.PetEq.Count);
            Assert.AreEqual(0, old.PetPulls);
        }

        [Test]
        public void 세이브를_한_바퀴_돌려도_가진_펫과_낀_칸이_그대로다()
        {
            var G = TestData.Load(); var d = Load(); var s = NewSave();
            var id = FirstId(d);
            Pets.Gain(s, id); Pets.Gain(s, id);         // Lv 1 + 조각 1
            s.PetPulls = 137;                            // 칸 둘이 열린 자리
            Assert.IsTrue(Pets.Equip(d, s, id, 1), "137회면 두 번째 칸이 열려 있다");

            var back = SaveData.FromJson(s.ToJson(), G);
            Assert.AreEqual(1, Pets.Lv(back, id), "레벨");
            Assert.AreEqual(1, Pets.Frag(back, id), "조각");
            Assert.AreEqual(137, back.PetPulls, "누적 뽑기");
            Assert.AreEqual(id, Pets.EquippedAt(d, back, 1), "낀 칸은 번호까지 그대로다");
            Assert.AreEqual("", Pets.EquippedAt(d, back, 0), "안 낀 칸은 빈 글자");
        }

        [Test]
        public void 처음_나오면_Lv1_이고_또_나오면_조각이_된다()
        {
            var d = Load(); var s = NewSave(); var id = FirstId(d);
            Assert.IsFalse(Pets.Has(s, id), "처음엔 안 가진 것");
            Pets.Gain(s, id);
            Assert.AreEqual(1, Pets.Lv(s, id), "첫 획득이 곧 Lv 1(주인)");
            Assert.AreEqual(0, Pets.Frag(s, id), "첫 마리는 조각이 아니다");
            Pets.Gain(s, id); Pets.Gain(s, id);
            Assert.AreEqual(1, Pets.Lv(s, id), "중복은 레벨을 안 올린다 — 강화가 올린다");
            Assert.AreEqual(2, Pets.Frag(s, id), "중복 = 조각(주인 «같은 게 또 나오면»)");
        }

        [Test]
        public void 강화는_필요치만큼_조각을_빼고_레벨을_하나_올린다()
        {
            var d = Load(); var s = NewSave(); var id = FirstId(d);
            Pets.Gain(s, id);
            Assert.IsFalse(Pets.CanLevelUp(d, s, id), "조각 0 이면 못 올린다");
            Assert.IsFalse(Pets.LevelUp(d, s, id));

            int need = Pets.Need(d, 1);
            for (int i = 0; i < need + 1; i++) Pets.Gain(s, id);   // 조각 need+1
            Assert.IsTrue(Pets.CanLevelUp(d, s, id));
            Assert.IsTrue(Pets.LevelUp(d, s, id));
            Assert.AreEqual(2, Pets.Lv(s, id), "Lv 2");
            Assert.AreEqual(1, Pets.Frag(s, id), "쓴 만큼만 빠진다(남은 조각은 그대로)");
            Assert.IsFalse(Pets.CanLevelUp(d, s, id), "Lv 2 → 3 은 더 든다");
        }

        [Test]
        public void 안_가진_펫도_잠긴_칸도_못_끼운다()
        {
            var d = Load(); var s = NewSave(); var id = FirstId(d);
            Assert.IsFalse(Pets.Equip(d, s, id, 0), "안 가진 펫은 못 낀다");
            Pets.Gain(s, id);
            Assert.IsTrue(Pets.Equip(d, s, id, 0), "첫 칸은 0회부터 열려 있다(주인)");
            Assert.IsFalse(Pets.Equip(d, s, id, 1), "뽑기 0회면 둘째 칸은 잠겨 있다");
            Assert.IsFalse(Pets.Equip(d, s, "없는_펫", 0), "표에 없는 id 는 못 낀다");
        }

        [Test]
        public void 같은_펫이_두_칸을_먹지_않는다()
        {
            var d = Load(); var s = NewSave(); var id = FirstId(d);
            Pets.Gain(s, id); s.PetPulls = 200;   // 칸 셋 다 열림
            Assert.IsTrue(Pets.Equip(d, s, id, 0));
            Assert.IsTrue(Pets.Equip(d, s, id, 2), "같은 펫을 다른 칸으로 옮긴다");
            Assert.AreEqual("", Pets.EquippedAt(d, s, 0), "옮기면 옛 칸은 비워진다");
            Assert.AreEqual(1, Pets.Equipped(d, s).Count, "한 마리가 두 번 세어지면 안 된다(스탯도 발동도)");
        }

        [Test]
        public void 잠긴_칸에_남아_있던_펫은_읽을_때_빠진다()
        {
            var d = Load(); var s = NewSave(); var id = FirstId(d);
            Pets.Gain(s, id); s.PetPulls = 100;
            Assert.IsTrue(Pets.Equip(d, s, id, 1), "100회면 둘째 칸이 열린다");
            Assert.AreEqual(1, Pets.Equipped(d, s).Count);

            s.PetPulls = 0;   // 표의 해금 횟수가 바뀌는 날과 같은 자리
            Assert.AreEqual(0, Pets.Equipped(d, s).Count, "잠긴 칸의 펫은 조용히 살아 있으면 안 된다");
            Assert.AreEqual("", Pets.EquippedAt(d, s, 1));
        }

        [Test]
        public void 장착_합은_한_마리씩_더한_것과_같다()
        {
            var G = TestData.Load(); var d = Load(); var s = NewSave();
            var a = d.Pets[0].Id; var b = d.Pets[d.Pets.Count - 1].Id;   // 등급이 다른 둘
            Pets.Gain(s, a); Pets.Gain(s, b); s.PetPulls = 100;
            Assert.IsTrue(Pets.Equip(d, s, a, 0)); Assert.IsTrue(Pets.Equip(d, s, b, 1));

            var one = Pets.Equip(G, d, d.Of(a), 1);
            var two = Pets.Equip(G, d, d.Of(b), 1);
            var sum = Pets.EquipPower(G, d, s);
            Assert.AreEqual(one.Atk + two.Atk, sum.Atk, 1e-9, "공");
            Assert.AreEqual(one.Hp + two.Hp, sum.Hp, 1e-9, "체");
            Assert.AreEqual(one.Sh + two.Sh, sum.Sh, 1e-9, "실");
            Assert.Greater(sum.Atk, 0, "합이 0 이면 «더하는 자리» 가 아예 안 붙은 것이다");
        }

        [Test]
        public void 소환은_치르고_뽑고_담고_누적을_올린다()
        {
            var d = Load(); var s = NewSave();
            s.PetEgg = 3;
            var offer = Pets.Offer(d, false, s.PetEgg, Cap);
            Assert.IsTrue(offer.ByEgg, "펫알 3개면 «소환» 은 펫알 버튼(T293 ⓓ)");

            var got = Pets.Draw(d, s, new Mulberry32(11), offer);
            Assert.IsNotNull(got); Assert.AreEqual(3, got.Count, "값이 정한 횟수만큼 뽑는다");
            Assert.AreEqual(0, s.PetEgg, 1e-9, "치른 만큼 빠진다");
            Assert.AreEqual(3, s.PetPulls, "누적은 «뽑은 횟수»");

            int owned = 0; foreach (var kv in s.PetLv) owned += kv.Value >= 1 ? 1 : 0;
            int frags = 0; foreach (var kv in s.PetFrag) frags += kv.Value;
            Assert.AreEqual(3, owned + frags, "세 번 뽑았으면 «가진 마리 + 조각» 이 셋이다(한 마리도 안 버려진다)");
        }

        [Test]
        public void 못_치르면_세이브가_한_글자도_안_바뀐다()
        {
            var d = Load(); var s = NewSave();
            s.Gem = 10; s.PetEgg = 0;
            var offer = Pets.Offer(d, false, 0, Cap);
            Assert.IsFalse(offer.ByEgg, "펫알이 없으면 다이아 값이다");
            Assert.IsFalse(Pets.CanDraw(s, offer), "다이아 10 으로는 100 짜리를 못 뽑는다");

            var before = s.ToJson();
            Assert.IsNull(Pets.Draw(d, s, new Mulberry32(11), offer), "못 치르면 아무것도 안 준다");
            Assert.AreEqual(before, s.ToJson(), "반쯤 치르고 실패하는 자리가 없다");
        }

        [Test]
        public void 다이아_소환도_같은_길로_치른다()
        {
            var d = Load(); var s = NewSave();
            s.Gem = 1000; s.PetEgg = 0;
            var ten = Pets.Offer(d, true, 0, Cap);
            Assert.IsFalse(ten.ByEgg);
            var got = Pets.Draw(d, s, new Mulberry32(7), ten);
            Assert.IsNotNull(got); Assert.AreEqual(Cap, got.Count);
            Assert.AreEqual(1000 - d.CostTen, s.Gem, 1e-9, "다이아를 표 값만큼 뺀다");
            Assert.AreEqual(Cap, s.PetPulls, "x10 은 10회로 센다(주인 확정)");
        }

        [Test]
        public void 해금은_얻은_마리가_아니라_뽑은_횟수로_열린다()
        {
            var d = Load(); var s = NewSave();
            s.PetEgg = Cap; s.PetPulls = 95;
            Assert.AreEqual(1, Pets.SlotsOpen(d, s), "95회면 아직 한 칸");
            Pets.Draw(d, s, new Mulberry32(3), Pets.Offer(d, true, s.PetEgg, Cap));
            Assert.AreEqual(105, s.PetPulls);
            Assert.AreEqual(2, Pets.SlotsOpen(d, s), "100회를 넘으면 두 칸(중복으로 나와 마릿수가 적어도 열린다)");
        }

        [Test]
        public void 정규화는_말이_안_되는_자리를_지운다()
        {
            var G = TestData.Load(); var d = Load(); var s = NewSave(); var id = FirstId(d);
            Pets.Gain(s, id);
            s.PetLv["엉터리"] = 0;            // 레벨 0 = 안 가진 것
            s.PetFrag["엉터리"] = 5;          // 가진 적 없는 펫의 조각
            s.PetEq.Add("엉터리");            // 안 가진 펫이 낀 칸
            s.PetPulls = -3;

            s.Normalize(G);
            Assert.IsFalse(s.PetLv.ContainsKey("엉터리"));
            Assert.IsFalse(s.PetFrag.ContainsKey("엉터리"), "가진 적 없는 펫의 조각은 남을 자리가 없다");
            Assert.AreEqual("", s.PetEq[0], "안 가진 펫이 낀 칸은 비워진다");
            Assert.AreEqual(0, s.PetPulls, "누적은 음수가 될 수 없다");
            Assert.AreEqual(1, Pets.Lv(s, id), "멀쩡한 자리는 안 건드린다");
        }
    }
}
