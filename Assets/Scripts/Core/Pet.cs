using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 표와 엔진이 <b>같은 낱말</b>을 쓰게 하는 이름들(T293) — 표에 이 밖의 이름이 적히면 엔진에 그 갈래가 없어
    /// <b>발동은 하는데 아무 일도 안 일어나는</b> 펫이 된다. 그래서 <see cref="PetData.From"/> 가 읽는 순간 막는다.
    /// </summary>
    public static class PetKey
    {
        public const string Evade = "evade", Attack = "attack", Hit = "hit";
        public const string ShotAxe = "axe", ShotBolt = "bolt";
    }

    /// <summary>
    /// 펫 <b>표</b>(<c>Assets/KkomaKnight/pet.json</c> · T293 · 주인 2026-09-09 06:0X · 06:1X «이름은 펫으로 다 통일»).
    /// <para>
    /// 9종 = <b>등급 3</b>(일반·희귀·전설) × <b>발동 3</b>(회피·공격·피격). 뽑기 확률 70/25/5 · 슬롯 3(0·100·200회 해금) ·
    /// 발동 33% 로 «도끼 1 / 도끼 2 / 번개 2» 를 랜덤 적에게 · 장착 스탯 = «같은 등급 장비의 절반 · 레벨당 +10%».
    /// <b>값·개수·이름은 전부 파일에서 온다</b>(<see cref="PrivilegeData"/>·<see cref="AchievementData"/> 와 같은 문법 — 새 꼴 안 만든다).
    /// </para>
    /// ⚠ 아직 <b>세이브를 안 본다</b> — <see cref="SaveData"/> 가 남의 살아 있는 lock 안이라(T258) 배선은 다음 회차의 일이다.
    /// 그래서 여기 있는 것은 전부 «수를 받아 수를 돌려주는» 순수 규칙이고, 세이브가 열리면 그 위에 얹기만 하면 된다.
    /// </summary>
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

        /// <summary>다이아 소환 값 — 1회 · 10회(주인 2026-09-09 09:3X «1회 소환 다이아 100개 · 10회는 1,000개»). 펫알로 뽑을 때는 안 쓴다.</summary>
        public double CostOne = 100, CostTen = 1000;

        /// <summary>
        /// 전투에서 <b>플레이어 뒤를 따라 걷는</b> 값(T293 9항 · 주인 «플레이어 뒤에 따라오는 느낌»):
        /// <c>BattleGapDx</c> = 펫 사이 월드 x 간격(sim.js 좌표) · <c>BattleScale</c> = 플레이어 키 대비 배율.
        /// <para>둘 다 <b>보이기 값</b>이라 엔진은 안 본다 — 시뮬은 펫이 있든 없든 한 톨도 안 달라진다.</para>
        /// </summary>
        public double BattleGapDx = 16, BattleScale = 0.8;

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

            d.BattleGapDx = j["battle"]["gapDx"].Num(d.BattleGapDx);
            d.BattleScale = j["battle"]["scale"].Num(d.BattleScale);
            // 0 이하면 펫이 플레이어와 같은 자리에 겹쳐 서거나(간격 0) 아예 안 보인다(배율 0) — 눈으로만 드러나는 자리라 여기서 운다.
            if (d.BattleGapDx <= 0 || d.BattleScale <= 0) throw new FormatException("pet.json: battle.gapDx·battle.scale 은 0 보다 커야 한다 — 지금 " + d.BattleGapDx + "·" + d.BattleScale);

            d.CostOne = j["cost"]["one"].Num(d.CostOne);
            d.CostTen = j["cost"]["ten"].Num(d.CostTen);
            // 값이 0 이하면 «공짜 소환» 이 되고 아무도 안 운다 — 표가 조용히 어긋나는 자리라 읽는 순간 운다(결정 818 갈래).
            if (d.CostOne <= 0 || d.CostTen <= 0) throw new FormatException("pet.json: cost.one·cost.ten 은 0 보다 커야 한다 — 지금 " + d.CostOne + "·" + d.CostTen);

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
    /// <b>두 켜가 있다</b>(T293 ⓕ · 2026-09-09 17:5X):
    /// <list type="number">
    /// <item><b>순수 규칙</b>(ⓐ~ⓔ 가 세운 것) — 수를 받아 수를 돌려준다. 세이브를 안 본다.</item>
    /// <item><b>세이브를 보는 것</b>(이 파일 아래쪽 «세이브» 묶음) — 가진 펫·조각·장착 칸·누적 뽑기를 <see cref="SaveData"/> 에서 읽고 쓴다.</item>
    /// </list>
    /// 두 켜를 <b>한 클래스에 두되 순서로 가른다</b> — 화면·엔진이 «펫 규칙» 을 찾을 자리가 하나여야 하고(<see cref="Achievement"/> 와 같은 꼴),
    /// 아래 묶음은 전부 위의 순수 규칙을 <b>부르기만</b> 한다(규칙을 두 번 적지 않는다).
    /// </para>
    /// </summary>
    public static class Pets
    {
        /// <summary>
        /// 소환 버튼 한 개가 «지금 무엇으로 몇 번» 뽑는지 — 화면이 글자·아이콘·눌림을 전부 여기서 읽는다(T293 5항 ⓖ).
        /// <para>버튼이 스스로 세지 않게 하려는 것이다 — 두 버튼이 각자 세면 «소환은 펫알인데 x10 은 다이아» 같은 어긋남이 화면에서만 산다.</para>
        /// </summary>
        public struct PullOffer
        {
            /// <summary>펫알로 뽑나(false = 다이아).</summary>
            public bool ByEgg;
            /// <summary>이번 누름에 뽑는 횟수.</summary>
            public int Count;
            /// <summary>치를 펫알(<see cref="ByEgg"/> 면 <see cref="Count"/> 와 같다 · 아니면 0).</summary>
            public double Egg;
            /// <summary>치를 다이아(<see cref="ByEgg"/> 면 0).</summary>
            public double Diamond;
        }

        /// <summary>
        /// <b>이번에 한 번에 쓸 펫알 개수</b> = <c>min(가진 개수, 캡)</c> — <see cref="GachaKeys.UseCount"/> 와 <b>같은 규칙</b>이다
        /// (주인 «키로 상자 소환할 때랑 같은 느낌» · T275 «17개면 10회 뽑고 7/7»).
        /// <para><b>캡은 부르는 쪽이 준다</b> — 표 값(<c>gacha.json</c> 의 <c>tenPull.count</c>)이라 여기에 10 을 안 박는다(§1).
        /// 0 이하로 들어오면 1 로 본다 — 표가 비었다고 버튼을 죽이는 것보다 «펫알 1개 = 1회»(T273)로 물러서는 쪽이 덜 다친다.</para>
        /// </summary>
        public static int EggUse(double eggs, int cap)
        {
            if (eggs < 1) return 0;
            int c = cap > 0 ? cap : 1;
            return eggs >= c ? c : (int)eggs;
        }

        /// <summary>
        /// 소환 버튼 하나의 값 — 주인 09:3X 확정. 펫알 K 개일 때:
        /// <list type="bullet">
        /// <item><b>K = 0</b> — 둘 다 다이아(1회 <c>cost.one</c> · x10 <c>cost.ten</c>).</item>
        /// <item><b>1 ≤ K &lt; 캡</b> — «소환» 만 펫알로 <b>K 회</b>(가진 것을 다 쓴다) · «x10» 은 그대로 다이아.</item>
        /// <item><b>K ≥ 캡</b> — <b>둘 다</b> 펫알로 캡(10)회.</item>
        /// </list>
        /// <para>
        /// ⚑ 갈림길은 <b>«펫알을 쓸 수 있나» 하나뿐이고, x10 쪽만 «캡을 채웠나» 를 더 본다.</b>
        /// 두 버튼이 같은 함수를 부르므로 규칙이 한 곳에 있다 — T289 가 상점에서 세운 그 꼴이다.
        /// </para>
        /// <para>펫알 모드는 <b>다이아가 모자라도 눌린다</b>(T289 3항) — 치르는 것이 다이아가 아니기 때문이다.</para>
        /// </summary>
        /// <param name="ten">«x10» 버튼인가(false = «소환»).</param>
        /// <param name="eggs">가진 펫알(<c>SaveData.PetEgg</c>).</param>
        /// <param name="cap">한 번에 쓸 수 있는 최대 개수(<c>D.Gacha.TenPullCount</c> · 표 값).</param>
        public static PullOffer Offer(PetData d, bool ten, double eggs, int cap)
        {
            int use = EggUse(eggs, cap);
            int c = cap > 0 ? cap : 1;
            bool byEgg = ten ? use >= c : use >= 1;
            if (byEgg) return new PullOffer { ByEgg = true, Count = use, Egg = use };
            double one = d != null ? d.CostOne : 0, tenCost = d != null ? d.CostTen : 0;
            return new PullOffer { ByEgg = false, Count = ten ? c : 1, Diamond = ten ? tenCost : one };
        }

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

        // ───────────────────────────── 세이브를 보는 묶음 (T293 ⓕ) ─────────────────────────────
        // 위쪽 순수 규칙을 «부르기만» 한다 — 필요치·해금·장착 스탯을 여기서 다시 세지 않는다.

        /// <summary>그 펫의 레벨 — <b>0 이면 아직 안 가진 것</b>(첫 획득이 곧 Lv 1 이라 «가졌나» 를 따로 안 적는다).</summary>
        public static int Lv(SaveData s, string id)
        {
            if (s == null || s.PetLv == null || string.IsNullOrEmpty(id)) return 0;
            int v; return s.PetLv.TryGetValue(id, out v) && v > 0 ? v : 0;
        }

        /// <summary>그 펫에 쌓인 조각(중복으로 나온 수 · 레벨업 재료).</summary>
        public static int Frag(SaveData s, string id)
        {
            if (s == null || s.PetFrag == null || string.IsNullOrEmpty(id)) return 0;
            int v; return s.PetFrag.TryGetValue(id, out v) && v > 0 ? v : 0;
        }

        /// <summary>가진 펫인가.</summary>
        public static bool Has(SaveData s, string id) => Lv(s, id) >= 1;

        /// <summary>
        /// 펫 하나를 <b>얻는다</b> — 처음이면 Lv 1, 이미 가진 것이면 조각 +1(주인 «같은 게 또 나오면 조각»).
        /// <para>표를 안 본다 — 무엇이 나왔는지는 <see cref="Pull"/> 이 이미 정했고, 여기는 그것을 세이브에 담기만 한다.</para>
        /// </summary>
        public static void Gain(SaveData s, string id)
        {
            if (s == null || string.IsNullOrEmpty(id)) return;
            if (s.PetLv == null) s.PetLv = new Dictionary<string, int>();
            if (s.PetFrag == null) s.PetFrag = new Dictionary<string, int>();
            if (Has(s, id)) s.PetFrag[id] = Frag(s, id) + 1;
            else s.PetLv[id] = 1;
        }

        /// <summary>지금 이 펫을 강화할 수 있는가(가졌고 조각이 <see cref="Need"/> 에 닿았다).</summary>
        public static bool CanLevelUp(PetData d, SaveData s, string id)
        {
            int lv = Lv(s, id);
            return lv >= 1 && CanLevelUp(d, lv, Frag(s, id));
        }

        /// <summary>강화 한 번 — 조각을 <see cref="Need"/> 만큼 <b>빼고</b> Lv +1(했으면 true). 저장은 호출부가 한다(<see cref="Dungeon"/> 규약과 같다).</summary>
        public static bool LevelUp(PetData d, SaveData s, string id)
        {
            if (!CanLevelUp(d, s, id)) return false;
            int lv = Lv(s, id);
            s.PetFrag[id] = Frag(s, id) - Need(d, lv);
            s.PetLv[id] = lv + 1;
            return true;
        }

        /// <summary>세이브의 누적 뽑기 횟수로 열린 장착 칸 수.</summary>
        public static int SlotsOpen(PetData d, SaveData s) => SlotsOpen(d, s != null ? s.PetPulls : 0);

        /// <summary>
        /// 지금 <b>실제로</b> 장착된 펫 id 들 — 칸 순서 그대로.
        /// <para>
        /// ⚑ <b>거르는 것이 이 함수의 일이다</b>: 잠긴 칸 · 빈 칸 · 표에서 사라진 id · 안 가진 펫 · 같은 펫이 두 칸.
        /// 세이브는 펫 표를 못 보므로(<see cref="GameData"/> 가 아직 그 표를 안 든다) <b>표가 필요한 정리는 전부 여기</b>서 한다 —
        /// 그래야 «해금 전에 끼워 둔 칸» 이 뽑기 횟수가 줄어드는 날에도 조용히 살아 있지 않는다.
        /// </para>
        /// </summary>
        public static List<string> Equipped(PetData d, SaveData s)
        {
            var list = new List<string>();
            if (d == null || s == null || s.PetEq == null) return list;
            int open = SlotsOpen(d, s);
            for (int i = 0; i < open && i < s.PetEq.Count; i++)
            {
                var id = s.PetEq[i];
                if (string.IsNullOrEmpty(id) || d.Of(id) == null || !Has(s, id) || list.Contains(id)) continue;
                list.Add(id);
            }
            return list;
        }

        /// <summary>칸 <paramref name="slot"/> 에 낀 펫 id(빈 칸·잠긴 칸이면 빈 글자) — 화면이 슬롯 하나를 그릴 때 본다.</summary>
        public static string EquippedAt(PetData d, SaveData s, int slot)
        {
            if (d == null || s == null || s.PetEq == null || slot < 0 || slot >= SlotsOpen(d, s) || slot >= s.PetEq.Count) return "";
            var id = s.PetEq[slot];
            return !string.IsNullOrEmpty(id) && d.Of(id) != null && Has(s, id) ? id : "";
        }

        /// <summary>
        /// 펫을 칸에 <b>낀다</b>(꼈으면 true). 잠긴 칸·모르는 id·안 가진 펫은 거절한다.
        /// <para>같은 펫이 다른 칸에 있으면 그 칸을 <b>비운다</b> — 한 마리가 두 칸에서 두 번 세어지지 않게(효과·스탯 둘 다).</para>
        /// </summary>
        public static bool Equip(PetData d, SaveData s, string id, int slot)
        {
            if (d == null || s == null || string.IsNullOrEmpty(id)) return false;
            if (slot < 0 || slot >= SlotsOpen(d, s)) return false;
            if (d.Of(id) == null || !Has(s, id)) return false;
            if (s.PetEq == null) s.PetEq = new List<string>();
            while (s.PetEq.Count <= slot) s.PetEq.Add("");
            for (int i = 0; i < s.PetEq.Count; i++) if (i != slot && s.PetEq[i] == id) s.PetEq[i] = "";
            s.PetEq[slot] = id;
            return true;
        }

        /// <summary>칸을 <b>비운다</b>(비웠으면 true).</summary>
        public static bool Unequip(SaveData s, int slot)
        {
            if (s == null || s.PetEq == null || slot < 0 || slot >= s.PetEq.Count) return false;
            if (string.IsNullOrEmpty(s.PetEq[slot])) return false;
            s.PetEq[slot] = "";
            return true;
        }

        /// <summary>장착한 펫들이 더해 주는 공·체·실 <b>합</b> — 화면 13 의 «+0 ❤ | +0 🛡 | +0 🗡» 줄이 이것을 찍는다.</summary>
        public static Power EquipPower(GameData D, PetData d, SaveData s)
        {
            var sum = new Power();
            if (D == null || d == null || s == null) return sum;
            foreach (var id in Equipped(d, s))
            {
                var p = Equip(D, d, d.Of(id), Lv(s, id));
                sum.Atk += p.Atk; sum.Hp += p.Hp; sum.Sh += p.Sh;
            }
            return sum;
        }

        /// <summary>
        /// <b>보여 주는 힘 = 장비 + 장착 펫</b>(주인 2026-09-10 «펫 전투력 숫자에 들어가야지 · 장착할 때 공체실 늘어나게»).
        /// <para>
        /// ⚑ <see cref="GearSystem.BuildPower"/> 는 <b>손대지 않는다</b> — 그 함수는 «<b>기저 + 장비</b>»(<c>(T.PSh0 + sh) * ev</c> · 펫이 안 든 힘)이고 시뮬·재적합 자·시드 골든(T2)이 전부 그 뜻으로 부른다(<see cref="RunOptions.PetPower"/> 주석과 같은 까닭).
        /// ⚠ 이름이 «Build(장비)Power» 라 «장비만» 으로 읽히는데 <b>아니다</b> — 표의 기저(<c>pAtk0/pHp0/pSh0</c>)가 이미 들어 있다. 앞 회차가 그렇게 읽고 자에 «새 세이브는 장비 실드가 0» 을 박아 런 962 가 빨갰다(결정 1108).
        /// 펫은 <b>여기서 더한다</b>. 그래서 화면(전투력 숫자 · 장비 화면 스탯 3칸)과 판(<see cref="RunOptions.PetPower"/>)이 **같은 두 함수의 합**이 되어 갈릴 자리가 없다.
        /// </para>
        /// <para>표(<c>D.Pet</c>)가 없거나 낀 펫이 없으면 <see cref="EquipPower"/> 가 0 을 주므로 값은 장비 그대로다.</para>
        /// </summary>
        public static Power TotalPower(GameData D, SaveData s)
        {
            if (D == null || s == null) return new Power();
            return GearSystem.Plus(GearSystem.BuildPower(D, s.CurBuild(D)), EquipPower(D, D.Pet, s));
        }

        /// <summary>엔진에 들려 보낼 발동 목록 — 지금 장착한 것들로 만든다(<see cref="Procs(PetData, IEnumerable{string})"/> 의 짧은 길).</summary>
        public static List<RunOptions.PetProc> Procs(PetData d, SaveData s) => Procs(d, Equipped(d, s));

        /// <summary>이 값(<see cref="Offer"/>)으로 지금 뽑을 수 있는가 — 치를 것이 있고 횟수가 1 이상이다.</summary>
        public static bool CanDraw(SaveData s, PullOffer o)
        {
            if (s == null || o.Count <= 0) return false;
            return o.ByEgg ? s.PetEgg >= o.Egg : s.Gem >= o.Diamond;
        }

        /// <summary>
        /// 소환 — <b>치르고</b>(펫알 또는 다이아) <b>뽑고</b> <b>담고</b> 누적 횟수를 올린다. 못 치르면 <c>null</c>(세이브는 한 글자도 안 바뀐다).
        /// <para>
        /// ⚑ 값·횟수를 여기서 다시 세지 않는다 — <see cref="Offer"/> 가 정한 것을 그대로 치른다. 그래서 버튼 둘이 무엇으로 몇 번 뽑는지는
        /// 화면에도 여기에도 두 번 적히지 않는다(T293 ⓓ 가 세운 계약).
        /// </para>
        /// <para>돌려주는 목록은 <b>뽑힌 순서</b>다 — 결과 창(T158 길)이 그대로 그린다. 저장은 호출부가 한다.</para>
        /// </summary>
        public static List<PetData.Pet> Draw(PetData d, SaveData s, IRng rng, PullOffer o)
        {
            if (d == null || rng == null || !CanDraw(s, o)) return null;
            if (o.ByEgg) s.PetEgg -= o.Egg; else s.Gem -= o.Diamond;
            var got = new List<PetData.Pet>();
            for (int i = 0; i < o.Count; i++)
            {
                var p = Pull(d, rng);
                if (p == null) continue;
                Gain(s, p.Id);
                got.Add(p);
            }
            s.PetPulls += o.Count;   // 해금은 «뽑은 횟수» 로 센다(x10 = 10 · 주인 확정) — 얻은 마리 수가 아니다
            return got;
        }

        /// <summary>
        /// 세이브만 보고 하는 정리(<see cref="SaveData.Normalize"/> 가 부른다) — 음수·빈 id·«레벨 0 인데 조각만 있는» 자리를 없앤다.
        /// <para>⚠ <b>표가 필요한 정리는 안 한다</b>(모르는 id·잠긴 칸) — 세이브 층은 펫 표를 못 보고, 그 몫은 <see cref="Equipped"/> 가 한다.</para>
        /// </summary>
        public static void NormalizeSave(SaveData s)
        {
            if (s == null) return;
            if (s.PetLv == null) s.PetLv = new Dictionary<string, int>();
            if (s.PetFrag == null) s.PetFrag = new Dictionary<string, int>();
            if (s.PetEq == null) s.PetEq = new List<string>();
            var drop = new List<string>();
            foreach (var kv in s.PetLv) if (string.IsNullOrEmpty(kv.Key) || kv.Value < 1) drop.Add(kv.Key);
            foreach (var k in drop) s.PetLv.Remove(k);
            drop.Clear();
            foreach (var kv in s.PetFrag) if (string.IsNullOrEmpty(kv.Key) || kv.Value < 1 || !s.PetLv.ContainsKey(kv.Key)) drop.Add(kv.Key);
            foreach (var k in drop) s.PetFrag.Remove(k);
            for (int i = 0; i < s.PetEq.Count; i++)
            {
                var id = s.PetEq[i];
                if (string.IsNullOrEmpty(id) || !s.PetLv.ContainsKey(id)) { s.PetEq[i] = ""; continue; }
                for (int k = 0; k < i; k++) if (s.PetEq[k] == id) { s.PetEq[i] = ""; break; }
            }
        }
    }
}
