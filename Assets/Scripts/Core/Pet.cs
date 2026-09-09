using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 펫 <b>표</b>(<c>Assets/KkomaKnight/pet.json</c> · T293 · 주인 2026-09-09 06:0X · 06:1X «이름은 펫으로 다 통일»).
    /// <para>
    /// 9종 = <b>등급 3</b>(일반·희귀·전설) × <b>발동 3</b>(회피·공격·피격). 뽑기 확률 70/25/5 · 슬롯 3(0·100·200회 해금) ·
    /// 발동 33% 로 «도끼 1 / 도끼 2 / 번개 2» 를 랜덤 적에게 · 장착 스탯 = «같은 등급 장비의 절반 · 레벨당 +10%».
    /// <b>값·개수·이름은 전부 파일에서 온다</b>(<see cref="PrivilegeData"/>·<see cref="AchievementData"/> 와 같은 문법 — 새 꼴 안 만든다).
    /// </para>
    /// ⚠ 이 회차(T293 ⓐ)는 <b>세이브를 안 본다</b> — <see cref="SaveData"/> 가 남의 살아 있는 lock 안이라(T290·T258) 배선은 다음 회차의 일이다.
    /// 그래서 여기 있는 것은 전부 «수를 받아 수를 돌려주는» 순수 규칙이고, 세이브가 열리면 그 위에 얹기만 하면 된다.
    /// </summary>
    /// <summary>
    /// 표와 엔진이 <b>같은 낱말</b>을 쓰게 하는 이름들(T293) — 표에 이 밖의 이름이 적히면 엔진에 그 갈래가 없어
    /// <b>발동은 하는데 아무 일도 안 일어나는</b> 펫이 된다. 그래서 <see cref="PetData.From"/> 가 읽는 순간 막는다.
    /// </summary>
    public static class PetKey
    {
        public const string Evade = "evade", Attack = "attack", Hit = "hit";
        public const string ShotAxe = "axe", ShotBolt = "bolt";
    }

    public sealed class PetData
    {
        /// <summary>발동 자리 하나 — 키(<c>evade</c>·<c>attack</c>·<c>hit</c>)와 화면 글자(«회피»·«공격»·«피격»).</summary>
        public sealed class Trigger { public string Key, Name; }

        /// <summary>등급 하나 — 화면 글자 · 장비 등급 번호 · 이 등급이 쏘는 것.</summary>
        public sealed class Grade
        {
            public string Key, Name;
            /// <summary><see cref="GameData.Gear"/> 의 등급 번호(일반 0 · 희귀 1 · 전설 2) — 장착 스탯을 «같은 등급 장비» 에서 뽑을 때 쓴다.</summary>
            public int Rar;
            /// <summary><c>axe</c>(도끼) · <c>bolt</c>(번개) — 엔진의 <c>FireAxe</c>·<c>FireBolts</c> 갈래를 고르는 이름이다.</summary>
            public string Shot;
            /// <summary>한 번 발동에 몇 발인가(일반 1 · 희귀 2 · 전설 2).</summary>
            public int Count;
        }

        /// <summary>펫 하나 — id 는 <c>&lt;등급&gt;_&lt;발동&gt;</c>.</summary>
        public sealed class Pet { public string Id, GradeKey, TriggerKey, Name; }

        /// <summary>등급 뽑기 확률(%) — 합이 100 이어야 한다.</summary>
        public double[] Rate = { 70, 25, 5 };
        /// <summary>장착 칸 수(잠긴 칸 포함).</summary>
        public int Slots = 3;
        /// <summary>칸 i 가 열리는 <b>누적 뽑기 횟수</b>(0·100·200) — 길이는 <see cref="Slots"/> 와 같아야 한다.</summary>
        public int[] SlotUnlockPulls = { 0, 100, 200 };
        /// <summary>발동 확률(%) — 장착 펫마다 <b>따로</b> 굴린다.</summary>
        public double ProcChance = 33;

        public readonly List<Trigger> Triggers = new List<Trigger>();
        public readonly List<Grade> Grades = new List<Grade>();
        public readonly List<Pet> Pets = new List<Pet>();
        /// <summary><c>axe</c> → «도끼» 같은 화면 글자.</summary>
        public readonly Dictionary<string, string> ShotName = new Dictionary<string, string>();

        /// <summary>Lv L → L+1 에 드는 조각 수 = <c>min(NeedBase + NeedStep × (L-1), NeedCap)</c>. <b>레벨 캡은 없다</b>(주인 확정).</summary>
        public int NeedBase = 2, NeedStep = 1, NeedCap = 10;
        /// <summary>장착 스탯 = 같은 등급 장비 기여 × <see cref="GearFactor"/> × (1 + <see cref="PerLevel"/> × (Lv-1)).</summary>
        public double GearFactor = 0.5, PerLevel = 0.10;

        public Pet Of(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var p in Pets) if (p.Id == id) return p;
            return null;
        }
        public Grade GradeOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var g in Grades) if (g.Key == key) return g;
            return null;
        }
        public Trigger TriggerOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var t in Triggers) if (t.Key == key) return t;
            return null;
        }
        /// <summary>그 펫의 등급(없으면 null).</summary>
        public Grade GradeOfPet(Pet p) => p == null ? null : GradeOf(p.GradeKey);

        public static PetData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static PetData From(JNode j)
        {
            var d = new PetData();

            var rate = j["rate"].NumArray();
            if (rate != null && rate.Length > 0) d.Rate = rate;
            double sum = 0; foreach (var r in d.Rate) { if (r < 0) throw new FormatException("pet.json: rate 에 음수가 있다"); sum += r; }
            // 합이 100 이 아니면 «5% 전설» 이 5% 가 아니게 된다 — 조용히 어긋나는 자리라 읽는 순간 운다(T253·T264 와 같은 갈래).
            if (Math.Abs(sum - 100) > 1e-6) throw new FormatException("pet.json: rate 의 합이 100 이 아니다 — 지금 " + sum);

            d.Slots = (int)j["slots"].Num(d.Slots);
            if (d.Slots <= 0) throw new FormatException("pet.json: slots 는 0 보다 커야 한다");
            var un = j["slotUnlockPulls"].NumArray();
            if (un != null && un.Length > 0) { d.SlotUnlockPulls = new int[un.Length]; for (int i = 0; i < un.Length; i++) d.SlotUnlockPulls[i] = (int)un[i]; }
            if (d.SlotUnlockPulls.Length != d.Slots)
                throw new FormatException("pet.json: slotUnlockPulls 길이(" + d.SlotUnlockPulls.Length + ")가 slots(" + d.Slots + ")와 다르다 — 열리지 않는 칸이나 셀 수 없는 칸이 생긴다");
            for (int i = 1; i < d.SlotUnlockPulls.Length; i++)
                if (d.SlotUnlockPulls[i] < d.SlotUnlockPulls[i - 1])
                    throw new FormatException("pet.json: slotUnlockPulls 가 줄어든다 — 뒤 칸이 앞 칸보다 먼저 열린다");

            d.ProcChance = j["proc"]["chance"].Num(d.ProcChance);
            if (d.ProcChance <= 0 || d.ProcChance > 100) throw new FormatException("pet.json: proc.chance 는 0 초과 100 이하여야 한다 — 지금 " + d.ProcChance);

            foreach (var t in j["triggers"].Items())
            {
                var tr = new Trigger { Key = t["key"].Str(""), Name = t["name"].Str("") };
                if (string.IsNullOrEmpty(tr.Key) || string.IsNullOrEmpty(tr.Name)) throw new FormatException("pet.json: triggers[] 의 key·name 이 비었다");
                if (d.TriggerOf(tr.Key) != null) throw new FormatException("pet.json: 발동 키가 겹친다 — " + tr.Key);
                // 엔진에 그 자리가 없으면 «발동은 하는데 아무 일도 안 일어나는» 펫이 된다 — 빨간 줄도 안 난다(결정 818 갈래).
                if (tr.Key != PetKey.Evade && tr.Key != PetKey.Attack && tr.Key != PetKey.Hit)
                    throw new FormatException("pet.json: 엔진이 모르는 발동 «" + tr.Key + "» — 있는 자리는 " + PetKey.Evade + "·" + PetKey.Attack + "·" + PetKey.Hit + " 셋뿐이다");
                d.Triggers.Add(tr);
            }
            if (d.Triggers.Count == 0) throw new FormatException("pet.json: triggers 가 비었다");

            foreach (var g in j["grades"].Items())
            {
                var gr = new Grade
                {
                    Key = g["key"].Str(""), Name = g["name"].Str(""),
                    Rar = (int)g["rar"].Num(-1), Shot = g["shot"].Str(""), Count = (int)g["count"].Num(0),
                };
                if (string.IsNullOrEmpty(gr.Key) || string.IsNullOrEmpty(gr.Name)) throw new FormatException("pet.json: grades[] 의 key·name 이 비었다");
                if (d.GradeOf(gr.Key) != null) throw new FormatException("pet.json: 등급 키가 겹친다 — " + gr.Key);
                if (gr.Rar < 0) throw new FormatException("pet.json: " + gr.Key + " 의 rar 이 없다 — 장착 스탯을 뽑을 등급을 못 정한다");
                if (string.IsNullOrEmpty(gr.Shot)) throw new FormatException("pet.json: " + gr.Key + " 의 shot 이 비었다");
                if (gr.Shot != PetKey.ShotAxe && gr.Shot != PetKey.ShotBolt)
                    throw new FormatException("pet.json: 엔진이 모르는 발사체 «" + gr.Shot + "» — 있는 것은 " + PetKey.ShotAxe + "·" + PetKey.ShotBolt + " 둘뿐이다");
                // 0발이면 «발동은 하는데 아무 일도 안 일어나는» 펫이 된다 — 화면에도 로그에도 안 보인다(결정 818 과 같은 갈래).
                if (gr.Count <= 0) throw new FormatException("pet.json: " + gr.Key + " 의 count 가 0 이다 — 발동해도 아무것도 안 쏜다");
                d.Grades.Add(gr);
            }
            if (d.Grades.Count != d.Rate.Length)
                throw new FormatException("pet.json: 등급 " + d.Grades.Count + "개인데 rate 는 " + d.Rate.Length + "개다 — 확률 없는 등급이나 등급 없는 확률이 생긴다");

            foreach (var k in j["shotName"].Keys) d.ShotName[k] = j["shotName"][k].Str(k);
            foreach (var g in d.Grades)
                if (!d.ShotName.ContainsKey(g.Shot)) throw new FormatException("pet.json: shotName 에 «" + g.Shot + "» 이 없다 — 효과 글자를 못 만든다");

            var lv = j["level"];
            d.NeedBase = (int)lv["needBase"].Num(d.NeedBase);
            d.NeedStep = (int)lv["needStep"].Num(d.NeedStep);
            d.NeedCap = (int)lv["needCap"].Num(d.NeedCap);
            if (d.NeedBase <= 0) throw new FormatException("pet.json: level.needBase 는 0 보다 커야 한다 — 조각 0개로 레벨이 오른다");
            if (d.NeedStep < 0) throw new FormatException("pet.json: level.needStep 은 음수일 수 없다");
            if (d.NeedCap < d.NeedBase) throw new FormatException("pet.json: level.needCap 이 needBase 보다 작다");

            var eq = j["equip"];
            d.GearFactor = eq["gearFactor"].Num(d.GearFactor);
            d.PerLevel = eq["perLevel"].Num(d.PerLevel);
            if (d.GearFactor <= 0) throw new FormatException("pet.json: equip.gearFactor 는 0 보다 커야 한다 — 장착해도 스탯이 0 이다");
            if (d.PerLevel < 0) throw new FormatException("pet.json: equip.perLevel 은 음수일 수 없다 — 레벨을 올릴수록 약해진다");

            foreach (var p in j["pets"].Items())
            {
                var pet = new Pet { Id = p["id"].Str(""), GradeKey = p["grade"].Str(""), TriggerKey = p["trigger"].Str(""), Name = p["name"].Str("") };
                if (string.IsNullOrEmpty(pet.Id)) throw new FormatException("pet.json: pets[].id 가 비었다");
                if (string.IsNullOrEmpty(pet.Name)) throw new FormatException("pet.json: " + pet.Id + " 의 name 이 비었다");
                if (d.Of(pet.Id) != null) throw new FormatException("pet.json: 펫 id 가 겹친다 — " + pet.Id);
                if (d.GradeOf(pet.GradeKey) == null) throw new FormatException("pet.json: " + pet.Id + " 의 grade «" + pet.GradeKey + "» 가 grades 에 없다");
                if (d.TriggerOf(pet.TriggerKey) == null) throw new FormatException("pet.json: " + pet.Id + " 의 trigger «" + pet.TriggerKey + "» 가 triggers 에 없다");
                d.Pets.Add(pet);
            }
            // 등급 × 발동 이 다 채워져 있어야 한다 — 한 칸이 비면 그 등급을 뽑았을 때 «줄 것이 없는» 굴림이 된다.
            if (d.Pets.Count != d.Grades.Count * d.Triggers.Count)
                throw new FormatException("pet.json: 펫이 " + d.Pets.Count + "종인데 등급 " + d.Grades.Count + " × 발동 " + d.Triggers.Count + " = " + (d.Grades.Count * d.Triggers.Count) + " 이어야 한다");
            foreach (var g in d.Grades)
                foreach (var t in d.Triggers)
                {
                    bool found = false;
                    foreach (var p in d.Pets) if (p.GradeKey == g.Key && p.TriggerKey == t.Key) { found = true; break; }
                    if (!found) throw new FormatException("pet.json: " + g.Key + " × " + t.Key + " 짝이 없다 — 그 등급을 뽑아도 줄 펫이 모자란다");
                }
            return d;
        }
    }

    /// <summary>
    /// 펫 <b>규칙</b>(T293 ⓐ) — 뽑기 · 레벨 필요치 · 슬롯 해금 · 장착 스탯 · 효과 글자.
    /// <para>
    /// ⚠ <b>세이브를 안 본다.</b> 전부 «수를 받아 수를 돌려주는» 순수 함수이고, 세이브 배선(<c>SaveData.Pets</c>·<c>PetPulls</c>)은
    /// 그 파일의 lock 이 풀린 뒤 회차가 이 위에 얹는다(그 회차가 규칙을 다시 짜지 않도록 여기서 다 정해 둔다).
    /// </para>
    /// </summary>
    public static class Pets
    {
        /// <summary>등급 하나를 <see cref="PetData.Rate"/> 대로 굴린다(70/25/5). 굴림은 게임 <see cref="IRng"/> 하나만 쓴다.</summary>
        public static PetData.Grade RollGrade(PetData d, IRng rng)
        {
            if (d == null || rng == null || d.Grades.Count == 0) return null;
            double r = rng.Next() * 100, acc = 0;
            for (int i = 0; i < d.Grades.Count; i++)
            {
                acc += d.Rate[i];
                if (r < acc) return d.Grades[i];
            }
            return d.Grades[d.Grades.Count - 1];   // 부동소수 끝자락 — 마지막 등급으로 떨어진다
        }

        /// <summary>
        /// 뽑기 한 번 — 등급을 70/25/5 로 굴린 뒤 <b>그 등급 셋 중 균등</b>으로 하나(주인 지시 그대로).
        /// <para>굴림은 <b>언제나 두 번</b>이다(등급 → 그 안에서 하나) — 시드가 같으면 결과도 같다.</para>
        /// </summary>
        public static PetData.Pet Pull(PetData d, IRng rng)
        {
            var g = RollGrade(d, rng);
            if (g == null) return null;
            var pool = new List<PetData.Pet>();
            foreach (var p in d.Pets) if (p.GradeKey == g.Key) pool.Add(p);
            if (pool.Count == 0) return null;                        // 표가 막아 두지만(짝 검사) 규칙 쪽도 조용히 안 터진다
            int i = (int)(rng.Next() * pool.Count);
            if (i >= pool.Count) i = pool.Count - 1;
            return pool[i];
        }

        /// <summary>Lv <paramref name="lv"/> → Lv+1 에 드는 조각 수(<c>min(base + step×(lv-1), cap)</c>). Lv 1 미만은 1 로 본다.</summary>
        public static int Need(PetData d, int lv)
        {
            if (d == null) return 0;
            if (lv < 1) lv = 1;
            long need = (long)d.NeedBase + (long)d.NeedStep * (lv - 1);
            if (need > d.NeedCap) need = d.NeedCap;
            return (int)need;
        }

        /// <summary>조각이 다음 레벨에 닿았는가.</summary>
        public static bool CanLevelUp(PetData d, int lv, int frag) => d != null && lv >= 1 && frag >= Need(d, lv);

        /// <summary>누적 뽑기 횟수로 열린 장착 칸 수(0 회면 1칸 · 100 회면 2칸 · 200 회면 3칸).</summary>
        public static int SlotsOpen(PetData d, int pulls)
        {
            if (d == null) return 0;
            int n = 0;
            for (int i = 0; i < d.SlotUnlockPulls.Length; i++) if (pulls >= d.SlotUnlockPulls[i]) n++;
            return n > d.Slots ? d.Slots : n;
        }

        /// <summary>칸 <paramref name="slot"/>(0부터)이 열리는 데 남은 뽑기 횟수 — 이미 열렸으면 0.</summary>
        public static int PullsToOpen(PetData d, int slot, int pulls)
        {
            if (d == null || slot < 0 || slot >= d.SlotUnlockPulls.Length) return 0;
            int left = d.SlotUnlockPulls[slot] - pulls;
            return left > 0 ? left : 0;
        }

        /// <summary>
        /// 펫 하나를 장착했을 때 더해지는 공·체·실 — <b>같은 등급 장비 1개(슬롯 Lv0 · 강화 0) 의 절반 · 레벨당 +10%</b>(주인 확정).
        /// <para>장비 쪽 수를 다시 적지 않고 <see cref="GameData.Gear"/> 의 기여표를 그대로 읽는다 — 장비 값이 바뀌면 펫도 같이 움직인다.</para>
        /// </summary>
        public static Power Equip(GameData D, PetData d, PetData.Pet p, int lv)
        {
            var g = d != null ? d.GradeOfPet(p) : null;
            if (D == null || D.Gear == null || g == null) return new Power();
            if (lv < 1) lv = 1;
            var G = D.Gear;
            if (g.Rar < 0 || g.Rar >= G.Atk.Length) return new Power();
            double m = d.GearFactor * (1 + d.PerLevel * (lv - 1));
            return new Power { Atk = G.Atk[g.Rar] * m, Hp = G.Hp[g.Rar] * m, Sh = G.Sh[g.Rar] * m };
        }

        /// <summary>효과 글자 — «회피 시 33% 확률로 도끼 2개 발사». <b>사람이 따로 안 적는다</b>(표가 바뀌면 글자도 따라간다 · 결정 813 과 같은 결).</summary>
        public static string Effect(PetData d, PetData.Pet p)
        {
            var g = d != null ? d.GradeOfPet(p) : null;
            var t = d != null && p != null ? d.TriggerOf(p.TriggerKey) : null;
            if (g == null || t == null) return "";
            string shot = d.ShotName.TryGetValue(g.Shot, out var s) ? s : g.Shot;
            return t.Name + " 시 " + Fmt(d.ProcChance) + "% 확률로 " + shot + " " + g.Count + "개 발사";
        }

        /// <summary>
        /// 장착한 펫 id 목록 → <b>엔진이 읽는 발동 목록</b>(<see cref="RunOptions.Pets"/>). 판을 열 때 한 번 만든다.
        /// <para>
        /// ⚠ <b>빈 목록이면 <c>null</c> 을 돌려준다</b> — 엔진이 «펫이 없으면 굴림 자체를 안 한다» 를 그 <c>null</c> 로 판단하고,
        /// 그것이 시드 골든(T2)의 안전장치다. «빈 목록» 과 «없음» 을 굳이 가르지 않는 것이 이 자리의 계약이다.
        /// </para>
        /// 모르는 id 는 조용히 건너뛴다 — 표에서 펫이 하나 빠지는 날 세이브에 남은 옛 id 때문에 판이 안 열리면 안 된다.
        /// </summary>
        public static List<RunOptions.PetProc> Procs(PetData d, IEnumerable<string> equippedIds)
        {
            if (d == null || equippedIds == null) return null;
            List<RunOptions.PetProc> list = null;
            foreach (var id in equippedIds)
            {
                var p = d.Of(id); if (p == null) continue;
                var g = d.GradeOfPet(p); if (g == null) continue;
                (list ?? (list = new List<RunOptions.PetProc>())).Add(new RunOptions.PetProc
                {
                    Trigger = p.TriggerKey, Shot = g.Shot, Count = g.Count, Chance = d.ProcChance,
                });
            }
            return list;
        }

        static string Fmt(double v) => v == Math.Floor(v) ? ((long)v).ToString() : v.ToString("0.#");
    }
}
