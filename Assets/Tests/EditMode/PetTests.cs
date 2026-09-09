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

        /// <summary>표 한 곳만 망가뜨린 사본 — 자가 «무엇을 막는가» 를 그 자리에서 보여 준다.</summary>
        static string Bad(string from, string to)
        {
            string src = File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "pet.json")));
            Assert.IsTrue(src.Contains(from), "표에서 «" + from + "» 를 못 찾았다 — 표가 바뀌면 이 자도 같이 고친다");
            return src.Replace(from, to);
        }
    }
}
