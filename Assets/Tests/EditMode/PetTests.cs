using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T293 ⓐ — 펫 표(<c>Assets/KkomaKnight/pet.json</c>)와 규칙(<see cref="Pets"/>)의 자.
    /// <para>
    /// <b>주인이 준 값은 그대로 못 박는다</b>(70/25/5 · 슬롯 3 = 0·100·200회 · 33% · 도끼 1/도끼 2/번개 2 · 레벨 2·3·…·10) —
    /// 이 값들은 워커가 고른 것이 아니라 주인이 준 것이라 «자가 표의 거울» 문제가 없다(결정 555).
    /// </para>
    /// 그 위에 <b>규칙</b>을 잰다 — 등급 분포가 정말 70/25/5 인가 · 등급 안이 균등한가 · 레벨 필요치가 캡에서 멈추는가 ·
    /// 슬롯이 누적 뽑기로 열리는가 · 장착 스탯이 «같은 등급 장비의 절반» 인가 · 효과 글자가 표에서 조립되는가.
    /// </summary>
    public class PetTests
    {
        static PetData Load() => PetData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "pet.json"))));

        /// <summary>펫 표까지 실은 표 — 게임에서는 <c>Bootstrap</c> 이 <c>D.Pet</c> 을 따로 싣는다(<c>data/</c> 밖의 이 레포 전용 표라 <see cref="TestData.Load"/> 는 안 싣는다).
        /// <para>⚠ <see cref="TestData.Load"/> 가 돌려주는 것은 <b>모든 자가 나눠 쓰는 한 채</b>라 거기에 <c>Pet</c> 을 꽂으면 남의 자가 모르는 사이에 펫을 가진 판을 재게 된다 — 그래서 여기서 <b>따로 싣는다</b>.</para></summary>
        static GameData WithPetTable(PetData pd) { var D = GameData.LoadFromDirectory(TestData.Dir); D.Pet = pd; return D; }

        /// <summary>돌려주는 값이 정해진 자 — 분포를 재는 데 쓴다(게임 <see cref="IRng"/> 계약 그대로 [0,1)).</summary>
        sealed class FixedRng : IRng
        {
            readonly double[] _v; int _i;
            public FixedRng(params double[] v) { _v = v; }
            public double Next() { var x = _v[_i % _v.Length]; _i++; return x; }
        }

        [Test]
        public void 주인이_준_값이_그대로_있다()
        {
            var d = Load();
            Assert.AreEqual(new double[] { 70, 25, 5 }, d.Rate, "뽑기 확률 70/25/5(주인)");
            Assert.AreEqual(3, d.Slots, "장착 최대 3개(주인)");
            Assert.AreEqual(new[] { 0, 100, 200 }, d.SlotUnlockPulls, "처음 1칸 · 100회 · 200회 해금(주인)");
            Assert.AreEqual(33, d.ProcChance, 1e-9, "발동 33%(주인)");

            Assert.AreEqual(3, d.Grades.Count, "등급 셋"); Assert.AreEqual(3, d.Triggers.Count, "발동 셋");
            Assert.AreEqual(9, d.Pets.Count, "9종 = 등급 3 × 발동 3(주인 «일반 3 · 희귀 3 · 전설 3»)");

            Shot(d, "common", "axe", 1, 0);      // 일반 = 도끼 1개
            Shot(d, "rare", "axe", 2, 1);        // 희귀 = 도끼 2개
            Shot(d, "legend", "bolt", 2, 2);     // 전설 = 번개 2개

            Assert.AreEqual(2, d.NeedBase); Assert.AreEqual(1, d.NeedStep); Assert.AreEqual(10, d.NeedCap);
            Assert.AreEqual(0.5, d.GearFactor, 1e-9, "같은 등급 장비의 절반(주인 확정 06:1X)");
            Assert.AreEqual(0.10, d.PerLevel, 1e-9, "레벨당 +10%(주인 확정 06:1X)");
        }

        static void Shot(PetData d, string key, string shot, int count, int rar)
        {
            var g = d.GradeOf(key);
            Assert.IsNotNull(g, key);
            Assert.AreEqual(shot, g.Shot, key + " 가 쏘는 것");
            Assert.AreEqual(count, g.Count, key + " 발수");
            Assert.AreEqual(rar, g.Rar, key + " 의 장비 등급 번호(장착 스탯을 여기서 뽑는다)");
        }

        [Test]
        public void 등급은_칠십_이십오_오_로_갈린다()
        {
            var d = Load();
            // 경계를 콕 짚는다 — 0.699 는 일반, 0.70 은 희귀(70 까지가 일반), 0.949 는 희귀, 0.95 는 전설.
            Assert.AreEqual("common", Pets.RollGrade(d, new FixedRng(0.0)).Key);
            Assert.AreEqual("common", Pets.RollGrade(d, new FixedRng(0.699)).Key);
            Assert.AreEqual("rare", Pets.RollGrade(d, new FixedRng(0.70)).Key, "70 은 이미 일반 몫을 다 쓴 자리다");
            Assert.AreEqual("rare", Pets.RollGrade(d, new FixedRng(0.949)).Key);
            Assert.AreEqual("legend", Pets.RollGrade(d, new FixedRng(0.95)).Key);
            Assert.AreEqual("legend", Pets.RollGrade(d, new FixedRng(0.9999)).Key);
        }

        [Test]
        public void 많이_굴리면_분포가_표에_붙는다()
        {
            var d = Load();
            var rng = new Mulberry32(12345);   // 시드 고정 — 같은 씨앗이면 언제나 같은 수열이다(sim.js 와 같은 자)
            var n = new Dictionary<string, int>();
            const int N = 20000;
            for (int i = 0; i < N; i++)
            {
                var g = Pets.RollGrade(d, rng);
                n[g.Key] = n.TryGetValue(g.Key, out var c) ? c + 1 : 1;
            }
            // ±2%p — 2만 회면 이보다 훨씬 좁게 붙지만, 자가 «우연히» 빨개지는 일이 없게 넉넉히 둔다.
            Assert.AreEqual(70, n["common"] * 100.0 / N, 2.0, "일반 70%");
            Assert.AreEqual(25, n["rare"] * 100.0 / N, 2.0, "희귀 25%");
            Assert.AreEqual(5, n["legend"] * 100.0 / N, 2.0, "전설 5%");
        }

        [Test]
        public void 등급을_고른_뒤에는_그_안에서_균등하다()
        {
            var d = Load();
            var rng = new Mulberry32(777);
            var n = new Dictionary<string, int>();
            const int N = 30000;
            for (int i = 0; i < N; i++)
            {
                var p = Pets.Pull(d, rng);
                Assert.IsNotNull(p, "뽑기는 언제나 하나를 준다");
                n[p.Id] = n.TryGetValue(p.Id, out var c) ? c + 1 : 1;
            }
            Assert.AreEqual(9, n.Count, "9종이 전부 한 번은 나온다");
            // 같은 등급 셋은 서로 같은 몫이어야 한다(주인 «일반 3 · 희귀 3 · 전설 3» 이고 등급 안 가중치는 안 줬다)
            foreach (var g in d.Grades)
            {
                var ids = new List<string>();
                foreach (var p in d.Pets) if (p.GradeKey == g.Key) ids.Add(p.Id);
                double want = N * (d.Rate[d.Grades.IndexOf(g)] / 100.0) / ids.Count;
                foreach (var id in ids)
                    Assert.AreEqual(want, n[id], want * 0.15,
                        g.Key + " 안에서는 균등해야 한다 — " + id + " 가 " + n[id] + " 회(기대 " + want.ToString("0") + ")");
            }
        }

        [Test]
        public void 레벨_필요치는_이_삼_사_로_늘다가_십에서_멈춘다()
        {
            var d = Load();
            Assert.AreEqual(2, Pets.Need(d, 1), "1 → 2 는 조각 2개");
            Assert.AreEqual(3, Pets.Need(d, 2));
            Assert.AreEqual(4, Pets.Need(d, 3));
            Assert.AreEqual(10, Pets.Need(d, 9), "9 → 10 은 10개");
            Assert.AreEqual(10, Pets.Need(d, 10), "그 뒤로는 계속 10 — 레벨 캡은 없다(주인 확정)");
            Assert.AreEqual(10, Pets.Need(d, 999), "아주 높은 레벨에서도 10 에서 멈춘다(넘침 없음)");

            Assert.IsFalse(Pets.CanLevelUp(d, 1, 1), "조각 1개로는 못 올린다");
            Assert.IsTrue(Pets.CanLevelUp(d, 1, 2));
            Assert.IsFalse(Pets.CanLevelUp(d, 2, 2), "2 → 3 은 3개가 든다");
            Assert.IsTrue(Pets.CanLevelUp(d, 2, 3));
        }

        [Test]
        public void 슬롯은_누적_뽑기_횟수로_열린다()
        {
            var d = Load();
            Assert.AreEqual(1, Pets.SlotsOpen(d, 0), "처음에는 1칸(주인)");
            Assert.AreEqual(1, Pets.SlotsOpen(d, 99));
            Assert.AreEqual(2, Pets.SlotsOpen(d, 100), "100회에 둘째 칸(주인)");
            Assert.AreEqual(2, Pets.SlotsOpen(d, 199));
            Assert.AreEqual(3, Pets.SlotsOpen(d, 200), "200회에 셋째 칸(주인)");
            Assert.AreEqual(3, Pets.SlotsOpen(d, 10000), "표보다 더 열리지는 않는다");

            Assert.AreEqual(0, Pets.PullsToOpen(d, 0, 0), "첫 칸은 처음부터 열려 있다");
            Assert.AreEqual(100, Pets.PullsToOpen(d, 1, 0), "잠긴 칸에 «뽑기 100회 해금» 을 쓸 수 있어야 한다");
            Assert.AreEqual(1, Pets.PullsToOpen(d, 1, 99));
            Assert.AreEqual(0, Pets.PullsToOpen(d, 1, 100));
            Assert.AreEqual(200, Pets.PullsToOpen(d, 2, 0));
        }

        [Test]
        public void 장착_스탯은_같은_등급_장비의_절반이고_레벨당_십퍼센트_는다()
        {
            var d = Load(); var D = TestData.Load();
            var G = D.Gear;

            var common = d.Of("common_evade");
            var p1 = Pets.Equip(D, d, common, 1);
            Assert.AreEqual(G.Atk[0] * 0.5, p1.Atk, 1e-6, "일반 펫 Lv1 = 일반 장비 기여의 절반");
            Assert.AreEqual(G.Hp[0] * 0.5, p1.Hp, 1e-6);
            Assert.AreEqual(G.Sh[0] * 0.5, p1.Sh, 1e-6);

            var p3 = Pets.Equip(D, d, common, 3);
            Assert.AreEqual(G.Atk[0] * 0.5 * 1.2, p3.Atk, 1e-6, "Lv3 = 절반 × (1 + 0.10 × 2)");

            // 등급이 오르면 «그 등급 장비» 를 따라 오른다 — 펫 표에 장비 수를 베껴 적지 않았다는 뜻이다.
            var legend = d.Of("legend_hit");
            Assert.AreEqual(G.Atk[2] * 0.5, Pets.Equip(D, d, legend, 1).Atk, 1e-6, "전설 펫 = 전설 장비의 절반");
            Assert.Greater(Pets.Equip(D, d, legend, 1).Atk, Pets.Equip(D, d, common, 1).Atk, "전설이 일반보다 세다");

            Assert.AreEqual(0, Pets.Equip(D, d, null, 1).Atk, 1e-9, "없는 펫은 0 을 더한다(터지지 않는다)");
        }

        [Test]
        public void 효과_글자는_표에서_조립된다()
        {
            var d = Load();
            Assert.AreEqual("회피 시 33% 확률로 도끼 1개 발사", Pets.Effect(d, d.Of("common_evade")));
            Assert.AreEqual("공격 시 33% 확률로 도끼 2개 발사", Pets.Effect(d, d.Of("rare_attack")));
            Assert.AreEqual("피격 시 33% 확률로 번개 2개 발사", Pets.Effect(d, d.Of("legend_hit")));
            // 사람이 따로 적은 글자가 아니라 표에서 나온다 — 표를 고치면 글자가 따라온다(결정 813 과 같은 결)
            Assert.AreEqual("", Pets.Effect(d, null), "없는 펫은 빈 글자(터지지 않는다)");
        }

        [Test]
        public void 아홉_종의_이름과_짝이_다_있다()
        {
            var d = Load();
            var seen = new HashSet<string>();
            foreach (var g in d.Grades)
                foreach (var t in d.Triggers)
                {
                    var p = d.Of(g.Key + "_" + t.Key);
                    Assert.IsNotNull(p, g.Key + " × " + t.Key + " 짝");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(p.Name), p.Id + " 의 이름");
                    Assert.IsTrue(seen.Add(p.Name), "이름이 겹친다 — " + p.Name);
                }
            Assert.AreEqual(9, seen.Count);
        }

        [Test]
        public void 조용히_어긋나는_표는_읽는_순간_운다()
        {
            // 확률 합이 100 이 아니면 «전설 5%» 가 5% 가 아니게 되는데 화면에는 그대로 «5%» 로 적힌다.
            Assert.Throws<System.FormatException>(() => PetData.Parse(Bad("\"rate\": [70, 25, 5]", "\"rate\": [70, 25, 10]")));
            // 칸 수와 해금 표의 길이가 다르면 «열리지 않는 칸» 이나 «셀 수 없는 칸» 이 생긴다.
            Assert.Throws<System.FormatException>(() => PetData.Parse(Bad("\"slotUnlockPulls\": [0, 100, 200]", "\"slotUnlockPulls\": [0, 100]")));
            // 발수 0 이면 «발동은 하는데 아무 일도 안 일어나는» 펫 — 화면에도 로그에도 안 보인다(결정 818 갈래).
            Assert.Throws<System.FormatException>(() => PetData.Parse(Bad("\"shot\": \"axe\",  \"count\": 1", "\"shot\": \"axe\",  \"count\": 0")));
            // 등급 × 발동 한 짝이 비면 그 등급을 뽑았을 때 줄 것이 모자란다.
            Assert.Throws<System.FormatException>(() => PetData.Parse(Bad(
                "    { \"id\": \"legend_hit\",    \"grade\": \"legend\", \"trigger\": \"hit\",    \"name\": \"방패 번개술사\" }\n", "")));
        }

        // ───────────────────────── 엔진(T293 ⓑ) — 장착 펫이 실제로 쏘는가 · 안 끼면 난수 열이 한 톨도 안 움직이는가 ─────────────────────────

        static RunOptions Ladder() => new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false };

        [Test]
        public void 펫을_안_끼면_난수_열이_한_톨도_안_움직인다()
        {
            // ⚑ 이 자가 이 절에서 가장 중요한 자다 — 펫 갈래가 «펫이 없어도» 굴림을 한 번 하면
            //    시드 골든(T2 · BattleTests)이 통째로 밀린다. 그 사고는 «펫» 과 아무 상관없어 보이는 자리에서 터진다.
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, -1, 0, 0);

            var a1 = new BattleState(d, 3, b, new Mulberry32(11), new SimPolicy(), Ladder()).RunToEnd();
            var a2 = new BattleState(d, 3, b, new Mulberry32(11), new SimPolicy(), new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false, Pets = null }).RunToEnd();
            var a3 = new BattleState(d, 3, b, new Mulberry32(11), new SimPolicy(), new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false, Pets = new List<RunOptions.PetProc>() }).RunToEnd();

            Assert.AreEqual(a1.Time, a2.Time, 1e-9, "Pets = null 은 종전과 같은 판이어야 한다");
            Assert.AreEqual(a1.AtkTries, a2.AtkTries); Assert.AreEqual(a1.Miss, a2.Miss); Assert.AreEqual(a1.Kills, a2.Kills);
            Assert.AreEqual(a1.Time, a3.Time, 1e-9, "빈 목록도 «없음» 과 같아야 한다 — 빈 목록에서 굴리면 그것도 밀린다");
            Assert.AreEqual(a1.AtkTries, a3.AtkTries); Assert.AreEqual(a1.Miss, a3.Miss);
        }

        [Test]
        public void 장착_스탯이_0이면_판이_한_톨도_안_달라진다()
        {
            // T293 ⓖ — 발동 목록(ⓑ)과 **같은 안전장치**를 스탯 쪽에도 세운다.
            //   `RunOptions.PetPower` 는 구조체라 기본값이 0/0/0 이고, 그 판은 종전과 완전히 같아야 한다.
            //   ⚑ 이것이 «BuildPower 를 안 고치고 옵션으로 들려 보낸» 까닭 그 자체다 — 여기가 무너지면 시드 골든(T2)이 통째로 밀린다.
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, -1, 0, 0);
            var a1 = new BattleState(d, 3, b, new Mulberry32(23), new SimPolicy(), Ladder()).RunToEnd();
            var o = Ladder(); o.PetPower = new Power();          // 명시적으로 0 을 줘도 같아야 한다
            var a2 = new BattleState(d, 3, b, new Mulberry32(23), new SimPolicy(), o).RunToEnd();
            Assert.AreEqual(a1.Time, a2.Time, 1e-9, "PetPower 0 은 종전과 같은 판이어야 한다");
            Assert.AreEqual(a1.AtkTries, a2.AtkTries); Assert.AreEqual(a1.Miss, a2.Miss); Assert.AreEqual(a1.Kills, a2.Kills);
        }

        [Test]
        public void 장착_스탯을_주면_판이_실제로_세진다()
        {
            // «안 달라진다» 만 재면 «갈래가 아예 안 붙었다» 도 통과한다 — 반대쪽을 같이 잰다(ⓑ 와 같은 짝).
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, -1, 0, 0);
            var a1 = new BattleState(d, 3, b, new Mulberry32(23), new SimPolicy(), Ladder()).RunToEnd();
            var o = Ladder(); o.PetPower = new Power { Atk = 500, Hp = 3000, Sh = 300 };
            var a2 = new BattleState(d, 3, b, new Mulberry32(23), new SimPolicy(), o).RunToEnd();
            Assert.AreNotEqual(a1.Time, a2.Time, "장착 스탯을 주면 판이 달라져야 한다 — 같으면 더하는 줄이 안 붙은 것이다");
        }

        [Test]
        public void 보여_주는_힘은_장비와_낀_펫을_같이_센다()
        {
            // T293 ⓖ 마지막 어긋남(주인 2026-09-10 «펫 전투력 숫자에 들어가야지 · 장착할 때 공체실 늘어나게»).
            //   판은 이미 `RunOptions.PetPower` 로 세고 있었는데 **화면은 안 셌다** — 그 둘을 `Pets.TotalPower` 한 곳으로 묶었다.
            //   ⚑ 이 자가 지키는 것은 «수가 얼마인가» 가 아니라 «두 자리가 같은 데서 나오는가» 다: 기댓값을 여기서 다시 안 적고
            //     `BuildPower + EquipPower` 로 되짚는다(값이 바뀌면 자도 같이 움직인다).
            var pd = Load(); var D = WithPetTable(pd);
            Assert.IsNotNull(D.Pet, "표(D.Pet)가 실려 있어야 이 자가 뜻이 있다 — 안 실리면 펫 몫이 늘 0 이라 늘 통과한다");
            var s = SaveData.NewSave(D);

            var gearOnly = GearSystem.BuildPower(D, s.CurBuild(D));
            var before = Pets.TotalPower(D, s);
            Assert.AreEqual(gearOnly.Atk, before.Atk, 1e-9, "낀 펫이 없으면 장비 그대로");
            Assert.AreEqual(gearOnly.Hp, before.Hp, 1e-9); Assert.AreEqual(gearOnly.Sh, before.Sh, 1e-9);

            var id = pd.Pets[0].Id;
            Pets.Gain(s, id);
            var owned = Pets.TotalPower(D, s);
            Assert.AreEqual(before.Atk, owned.Atk, 1e-9, "가지고만 있고 안 끼면 힘은 안 오른다(주인 «장착할 때»)");
            Assert.AreEqual(before.Hp, owned.Hp, 1e-9); Assert.AreEqual(before.Sh, owned.Sh, 1e-9);

            Assert.IsTrue(Pets.Equip(D.Pet, s, id, 0), "새 세이브에서 첫 칸은 열려 있다");
            var withPet = Pets.TotalPower(D, s);
            var add = Pets.EquipPower(D, D.Pet, s);
            Assert.Greater(add.Atk + add.Hp + add.Sh, 0, "낀 펫은 공·체·실 중 무엇이든 실제로 더해야 한다 — 0 이면 아래 셋이 늘 통과한다");
            Assert.AreEqual(gearOnly.Atk + add.Atk, withPet.Atk, 1e-9, "보여 주는 공격력 = 장비 + 낀 펫");
            Assert.AreEqual(gearOnly.Hp + add.Hp, withPet.Hp, 1e-9, "체력도 같은 셈");
            Assert.AreEqual(gearOnly.Sh + add.Sh, withPet.Sh, 1e-9, "실드도 같은 셈");
        }

        [Test]
        public void 화면이_더하는_값과_판이_더하는_값이_같다()
        {
            // «갈리지 않는다» 를 말로만 두지 않고 잰다 — 화면은 `Pets.TotalPower`, 판은 `RunOptions.PetPower` 로 더하는데
            //   그 둘의 **펫 몫이 같은 함수(`Pets.EquipPower`)에서 나오는가**를 확인한다(BattleScreen 이 옵션에 담는 그 값).
            var pd = Load(); var D = WithPetTable(pd); var s = SaveData.NewSave(D);
            Pets.Gain(s, pd.Pets[0].Id); Pets.Equip(D.Pet, s, pd.Pets[0].Id, 0);

            var shownAdd = GearSystem.Plus(GearSystem.BuildPower(D, s.CurBuild(D)), Pets.EquipPower(D, D.Pet, s));
            var total = Pets.TotalPower(D, s);
            Assert.AreEqual(shownAdd.Atk, total.Atk, 1e-9, "화면 쪽 합");
            Assert.AreEqual(shownAdd.Hp, total.Hp, 1e-9); Assert.AreEqual(shownAdd.Sh, total.Sh, 1e-9);

            // 판 쪽 — 같은 세이브로 만든 `PetPower` 를 들려 보내면 플레이어가 그만큼 세진다(0 이 아니라는 것까지).
            var opt = Ladder(); opt.PetPower = Pets.EquipPower(D, D.Pet, s);
            Assert.Greater(opt.PetPower.Atk + opt.PetPower.Hp + opt.PetPower.Sh, 0, "판에 들려 보내는 펫 몫이 0 이면 아래가 뜻이 없다");
            Assert.AreEqual(total.Atk - GearSystem.BuildPower(D, s.CurBuild(D)).Atk, opt.PetPower.Atk, 1e-9,
                            "화면이 더한 몫 = 판에 들려 보내는 몫 — 여기가 갈리면 «전투력은 100인데 판은 80» 이 된다");
        }

        [Test]
        public void 펫을_끼면_판이_실제로_달라진다()
        {
            // «안 움직인다» 만 재면 «갈래가 아예 안 붙었다» 도 통과한다 — 반대쪽도 같이 잰다(결정 818 의 «빈 칸» 갈래).
            var d = TestData.Load(); var b = GearSystem.MkBuild(d, -1, 0, 0);
            var pd = Load();
            var procs = Pets.Procs(pd, new[] { "legend_hit", "legend_evade", "legend_attack" });
            Assert.IsNotNull(procs); Assert.AreEqual(3, procs.Count);

            var plain = new BattleState(d, 3, b, new Mulberry32(11), new SimPolicy(), Ladder()).RunToEnd();
            var withPets = new BattleState(d, 3, b, new Mulberry32(11), new SimPolicy(),
                new RunOptions { LadderPerkMode = true, BaseStatsLegacy20 = true, GearOpts = false, Pets = procs }).RunToEnd();

            Assert.AreNotEqual(plain.Time, withPets.Time, "펫 셋을 끼면 판이 달라져야 한다 — 같으면 갈래가 안 붙은 것이다");
        }

        [Test]
        public void 발동_목록은_표에서_그대로_온다()
        {
            var d = Load();
            var procs = Pets.Procs(d, new[] { "common_evade", "legend_hit" });
            Assert.AreEqual(2, procs.Count);
            Assert.AreEqual(PetKey.Evade, procs[0].Trigger); Assert.AreEqual(PetKey.ShotAxe, procs[0].Shot);
            Assert.AreEqual(1, procs[0].Count); Assert.AreEqual(33, procs[0].Chance, 1e-9);
            Assert.AreEqual(PetKey.Hit, procs[1].Trigger); Assert.AreEqual(PetKey.ShotBolt, procs[1].Shot);
            Assert.AreEqual(2, procs[1].Count);

            Assert.IsNull(Pets.Procs(d, new string[0]), "빈 목록은 null — 엔진이 «없음» 으로 읽는 그 값이다");
            Assert.IsNull(Pets.Procs(d, new[] { "없는펫" }), "모르는 id 만 있으면 null(표에서 펫이 빠져도 판은 열린다)");
        }

        [Test]
        public void 표에_엔진이_모르는_이름이_적히면_읽는_순간_운다()
        {
            // 이름이 한 글자 틀리면 «발동은 하는데 아무 일도 안 일어나는» 펫이 된다 — 빨간 줄도 안 난다(결정 818 갈래).
            Assert.Throws<System.FormatException>(() => PetData.Parse(Bad("\"key\": \"evade\",  \"name\": \"회피\"", "\"key\": \"dodge\",  \"name\": \"회피\"")));
            Assert.Throws<System.FormatException>(() => PetData.Parse(Bad("\"shot\": \"bolt\", \"count\": 2", "\"shot\": \"lightning\", \"count\": 2")));
        }

        /// <summary>표 한 곳만 망가뜨린 사본 — 자가 «무엇을 막는가» 를 그 자리에서 보여 준다.</summary>
        static string Bad(string from, string to)
        {
            string src = File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "pet.json")));
            Assert.IsTrue(src.Contains(from), "표에서 «" + from + "» 를 못 찾았다 — 표가 바뀌면 이 자도 같이 고친다");
            return src.Replace(from, to);
        }
    }
}
