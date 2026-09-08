using System;

namespace KkomaKnight.Core
{
    /// <summary>
    /// T240 3항 — 아레나 상대(더미)의 «전투력 하나» 를 <b>싸울 수 있는 스탯</b>으로 푸는 규칙표.
    /// 주인 지시는 «더미 데이터로 일단» 이고 지시서 3항이 «이름·아바타·전투력에서 스탯을 <b>규칙으로</b> 뽑아» 로 맡긴 자리다.
    /// <para>
    /// ⚠ <b>이 파일은 아무것도 «시키지» 않는다</b> — 순수 계산기다. 엔진(<see cref="Battle"/>)에 한 줄도 안 닿으므로
    /// 챕터 전투의 시드 골든(T2)이 움직일 자리가 없다(지시서 3항 «PvP 는 별도 진입점 · 시드 골든 건드리면 안 된다»).
    /// 1대1 진입점이 서는 회차가 이 값을 받아 쓰면 된다.
    /// </para>
    /// </summary>
    public sealed class ArenaFoeData
    {
        /// <summary>
        /// 전투력 = 공격 × <see cref="AtkWeight"/> + (체력 + 실드) × <see cref="HpWeight"/>.
        /// <para>⚠ <b>지어낸 식이 아니다</b> — <c>App.Power()</c> 가 쓰는 그 식 그대로다(공격 8 · 체력 1.5).
        /// 표에 둔 까닭은 둘이 갈리면 «내 전투력 100» 과 «상대 전투력 100» 이 서로 다른 세기가 되기 때문이다.
        /// <b>App.Power() 의 파일이 자유로워지는 회차가 이 값을 읽도록 바꾸면</b> 갈릴 자리가 아예 없어진다.</para>
        /// </summary>
        public double AtkWeight = 8, HpWeight = 1.5;
        /// <summary>
        /// 내 빌드에서 «공격 몫» 을 못 구할 때 쓰는 기본 몫(0~1). 평소에는 <b>내 빌드의 실제 몫</b>을 쓴다 —
        /// 그래야 «전투력이 같으면 대등한 판» 이 된다(내가 체력형이면 상대도 체력형).
        /// </summary>
        public double FallbackAtkShare = 0.5;
        /// <summary>공격 몫이 아무리 치우쳐도 이 밖으로는 안 나간다 — 한쪽이 0 이면 «때리지 못하는 상대» 나 «한 대에 죽는 상대» 가 된다.</summary>
        public double MinAtkShare = 0.15, MaxAtkShare = 0.85;
        /// <summary>스탯을 통째로 올리고 내리는 손잡이(1 = 전투력이 말하는 그대로). 주인이 «세다/약하다» 를 말하면 여기만 고친다.</summary>
        public double HpMul = 1, DmgMul = 1;

        public static ArenaFoeData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static ArenaFoeData From(JNode j)
        {
            var f = new ArenaFoeData();
            // JNode 는 struct 다 — null 비교가 아니라 IsNull 로 본다.
            if (j.IsNull) return f;
            // «foe 칸이 없다» = 기본값 그대로. 이 칸이 생기기 전의 표와도 그대로 맞는다.
            var n = j["foe"];
            if (n.IsNull) return f;
            f.AtkWeight = n["atkWeight"].Num(f.AtkWeight);
            f.HpWeight = n["hpWeight"].Num(f.HpWeight);
            f.FallbackAtkShare = n["fallbackAtkShare"].Num(f.FallbackAtkShare);
            f.MinAtkShare = n["minAtkShare"].Num(f.MinAtkShare);
            f.MaxAtkShare = n["maxAtkShare"].Num(f.MaxAtkShare);
            f.HpMul = n["hpMul"].Num(f.HpMul);
            f.DmgMul = n["dmgMul"].Num(f.DmgMul);

            // 이 표의 사고도 조용하다 — 무게가 0 이면 «상대가 안 때린다»/«한 대에 죽는다» 로만 보인다(ArenaMatch.From 과 같은 까닭 · T237 전례).
            if (f.AtkWeight <= 0 || f.HpWeight <= 0) throw new FormatException("arenaMatch.json: foe.atkWeight·foe.hpWeight 는 양수여야 한다(전투력을 스탯으로 못 나눈다)");
            if (f.MinAtkShare < 0 || f.MaxAtkShare > 1 || f.MinAtkShare > f.MaxAtkShare) throw new FormatException("arenaMatch.json: foe 의 공격 몫 하한·상한은 0 ≤ min ≤ max ≤ 1 이어야 한다");
            if (f.HpMul <= 0 || f.DmgMul <= 0) throw new FormatException("arenaMatch.json: foe.hpMul·foe.dmgMul 은 양수여야 한다");
            return f;
        }
    }

    /// <summary>T240 3항 — 상대 전투력 → 스탯. <see cref="ArenaFoeData"/> 참고.</summary>
    public static class ArenaFoe
    {
        /// <summary>한 상대의 싸울 수 있는 값 — 엔진의 적이 실제로 갖는 것만 담는다.</summary>
        public struct Stats
        {
            /// <summary>최대 체력.</summary>
            public double MaxHp;
            /// <summary>한 대의 공격력.</summary>
            public double Dmg;
        }

        /// <summary>
        /// 스탯 → 전투력(<c>App.Power()</c> 와 같은 식). 이 방향이 있어야 <see cref="Of"/> 가 «되돌아오는가» 를 잴 수 있다.
        /// </summary>
        public static double PowerOf(ArenaFoeData f, double atk, double hp, double shield = 0)
        {
            if (f == null) return 0;
            return atk * f.AtkWeight + (hp + shield) * f.HpWeight;
        }

        /// <summary>
        /// 내 빌드의 «공격 몫» — 전투력 중 공격이 낸 비율(0~1). 표의 하한·상한 안으로 자른다.
        /// <para>빌드가 없거나 전투력이 0 이면 표의 <see cref="ArenaFoeData.FallbackAtkShare"/>(그것도 하한·상한 안으로 자른다).</para>
        /// </summary>
        public static double AtkShareOf(ArenaFoeData f, double myAtk, double myHp, double myShield = 0)
        {
            if (f == null) return 0.5;
            double total = PowerOf(f, myAtk, myHp, myShield);
            double share = total > 0 ? myAtk * f.AtkWeight / total : f.FallbackAtkShare;
            return Clamp(share, f.MinAtkShare, f.MaxAtkShare);
        }

        /// <summary>
        /// <b>상대 전투력 하나</b>를 스탯으로 푼다 — 몫은 <paramref name="atkShare"/>(평소에는 <see cref="AtkShareOf"/> 로 낸 <b>내 빌드의 몫</b>)다.
        /// <para>
        /// 왜 내 몫을 쓰나 — 그래야 <b>«전투력이 같으면 대등한 판»</b> 이 된다. 상대 몫을 따로 지어내면
        /// 같은 전투력인데 어떤 상대는 한 방에 죽고 어떤 상대는 안 죽는, 설명할 수 없는 판이 된다(주인은 몫을 준 적이 없다).
        /// </para>
        /// <para>표가 없으면(<paramref name="f"/> 가 <c>null</c>) 전부 0 인 스탯을 돌려준다 — <b>못 읽었다고 아무 수나 지어내지 않는다</b>.</para>
        /// </summary>
        public static Stats Of(ArenaFoeData f, double foePower, double atkShare)
        {
            var s = new Stats();
            if (f == null || foePower <= 0) return s;
            double share = Clamp(atkShare, f.MinAtkShare, f.MaxAtkShare);
            if (f.AtkWeight > 0) s.Dmg = foePower * share / f.AtkWeight * f.DmgMul;
            if (f.HpWeight > 0) s.MaxHp = foePower * (1 - share) / f.HpWeight * f.HpMul;
            return s;
        }

        /// <summary>내 빌드에서 몫을 뽑아 그대로 <see cref="Of"/> 에 넣는 지름길 — 부르는 쪽이 두 줄을 안 쓰게.</summary>
        public static Stats Of(ArenaFoeData f, double foePower, double myAtk, double myHp, double myShield)
            => Of(f, foePower, AtkShareOf(f, myAtk, myHp, myShield));

        static double Clamp(double v, double lo, double hi)
        {
            if (hi < lo) { var t = lo; lo = hi; hi = t; }
            return v < lo ? lo : v > hi ? hi : v;
        }
    }
}
