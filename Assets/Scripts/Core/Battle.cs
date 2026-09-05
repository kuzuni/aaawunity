using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 전투 엔진 — sim.js `runChapter` 의 이식. 순수 C# (UnityEngine 없음).
    /// · 난수는 <see cref="Rng"/> 하나로만, sim.js 와 **같은 순서**로 소비한다(시드 하니스가 같은 수열을 밟도록).
    /// · 결정(쉼터·악마·천사·3택)은 <see cref="IBattlePolicy"/> 로 뺐다. 정책이 보류하면 <see cref="Pending"/> 이 서고
    ///   Tick 은 시간을 흐르게 하지 않는다(PLAN §2.4 «팝업 중 시간 완전 정지»). Resolve* 가 답을 넣으면 재개한다.
    /// · 수치는 GameData(JSON) 에서만. 이름 없는 동작 리터럴은 EngineConst 한 곳.
    /// </summary>
    public sealed class BattleState
    {
        public readonly GameData D;
        public readonly CombatData C;
        public readonly PerksData PK;
        public readonly IRng Rng;
        public readonly IBattlePolicy Policy;
        public readonly RunOptions Opt;
        public readonly int Chapter;
        public readonly PlayerState P;
        public readonly List<BattleNode> Nodes = new List<BattleNode>();
        public readonly List<Projectile> Projs = new List<Projectile>();
        public readonly List<EnemyArrow> Arrows = new List<EnemyArrow>();
        public readonly List<PerkDef> Taken = new List<PerkDef>();
        public readonly List<BattleEvent> Events = new List<BattleEvent>();
        public readonly List<PerkDef> Base10;
        public double T, Gold; public int Kills, ProcN, PerkChances, Stuns, Misses, AtkTries, Miss, TotalEnemies;
        public bool Dead, Cleared;
        public PendingDecision Pending;
        public int PendingLevelUps;
        /// <summary>천사 축복 등 «특전이 아닌 획득물» 표시용.</summary>
        public readonly List<string> Blessings = new List<string>();
        int _enemyId;

        public bool Over => Dead || Cleared || T >= EngineConst.MaxT;

        public BattleState(GameData data, int chapter, Build build, IRng rng, IBattlePolicy policy, RunOptions opt)
        {
            D = data; C = data.Combat; PK = data.Perks; Rng = rng; Policy = policy ?? new SimPolicy(); Opt = opt ?? new RunOptions();
            Chapter = chapter;
            Base10 = new List<PerkDef>();
            for (int i = 0; i < 10 && i < PK.Perks.Count; i++) Base10.Add(PK.Perks[i]);
            P = MkPlayer(build);
            BuildNodes();
        }

        // ───────────────────────── 플레이어 생성 (sim.js mkPlayer) ─────────────────────────
        PlayerState MkPlayer(Build build)
        {
            var T_ = D.Tune; var G = D.Gear;
            var pw = GearSystem.BuildPower(D, build);
            var p = new PlayerState
            {
                Dmg = pw.Atk, Aspd = T_.PAspd0, CritR = T_.PCrit0, CritF = T_.PCritF0, Def = T_.PDef0, Counter = T_.PCounter0, Evade = T_.PEvade0,
                MaxHp = pw.Hp, Hp = pw.Hp, MaxSh = pw.Sh, Sh = pw.Sh,
            };
            if (Opt.BaseStatsLegacy20) { p.CritR = 20; p.Def = 20; p.Counter = 20; p.Evade = 20; }
            if (Opt.GearOpts)
            {
                foreach (var pt in G.Parts)
                {
                    var g = build.EqAt(pt); if (g == null) continue;
                    if (!G.Options.TryGetValue(g.Type, out var tbl)) continue;
                    int n = G.OptCount(g.Rar, g.Plus);
                    for (int i = 0; i < n && i < tbl.Count; i++) ApplyGearOption(p, tbl[i]);
                }
            }
            p.Dmg *= 1 + p.PxGet("g_atkP") / 100; p.MaxHp *= 1 + p.PxGet("g_hpP") / 100; p.MaxSh *= 1 + p.PxGet("g_shP") / 100;
            p.Hp = p.MaxHp; p.Sh = p.MaxSh = RngUtil.JsRound(p.MaxSh);
            return p;
        }

        /// <summary>장비 세트 옵션 한 칸 적용 — gear.json effect 그대로: stat 은 가산 델타, px 는 누산(g_* 축).</summary>
        static void ApplyGearOption(PlayerState p, GearOption o)
        {
            foreach (var kv in o.Stat)
            {
                switch (kv.Key)
                {
                    case "critR": p.CritR += kv.Value; break;
                    case "critF": p.CritF += kv.Value; break;
                    case "counter": p.Counter += kv.Value; break;
                    case "def": p.Def += kv.Value; break;
                    case "evade": p.Evade += kv.Value; break;
                    case "steal": p.Steal += kv.Value; break;
                    default: throw new InvalidOperationException("gear.json: 모르는 스탯 축 " + kv.Key);
                }
            }
            foreach (var kv in o.Px) p.Px[kv.Key] = p.PxGet(kv.Key) + kv.Value;
        }

        // ───────────────────────── 노드 배치 (sim.js runChapter 머리) ─────────────────────────
        void BuildNodes()
        {
            var E = D.Enemies; var ch = E.Chapter(Chapter);
            double x = E.NodeGap; int wi = 0;
            foreach (var nd in ch.Nodes)
            {
                var node = new BattleNode { Type = nd.Type, X = x };
                if (nd.Type == NodeType.Wave)
                {
                    var st = ch.Waves[wi];
                    for (int j = 0; j < nd.Size; j++)
                    {
                        node.Enemies.Add(new EnemyState
                        {
                            Id = ++_enemyId, WorldX = x + j * E.EnemyGap, Hp = st.Hp, MaxHp = st.Hp, Dmg = st.Dmg, Ranged = nd.Ranged[j],
                            AtkTimer = Rng.Range(EngineConst.EnemyMinAtkTimer, EngineConst.EnemyMaxAtkTimer), Wave = node,
                        });
                    }
                    TotalEnemies += nd.Size;
                    wi++; x += (nd.Size - 1) * E.EnemyGap + E.NodeGap;
                }
                else if (nd.Type == NodeType.Boss)
                {
                    node.Enemies.Add(new EnemyState
                    {
                        Id = ++_enemyId, WorldX = x + EngineConst.BossOffset, Hp = ch.Boss.Hp, MaxHp = ch.Boss.Hp, Dmg = ch.Boss.Dmg,
                        AtkTimer = EngineConst.BossAtkTimer, Wave = node, IsBoss = true,
                    });
                    TotalEnemies += 1;
                }
                else x += E.NodeGapEvent;
                Nodes.Add(node);
            }
        }

        // ───────────────────────── 실효 스탯 ─────────────────────────
        double BSum(string k) { double s = 0; foreach (var b in P.Buffs[k]) s += b.Amt; return s; }
        void AddBuff(string k, double amt, double dur) { P.Buffs[k].Add(new Buff { T = dur, Amt = amt }); }
        void RefreshBuff(string k, double amt, double dur, string tag)
        {
            var arr = P.Buffs[k];
            for (int i = arr.Count - 1; i >= 0; i--) if (arr[i].Tag == tag) arr.RemoveAt(i);
            arr.Add(new Buff { T = dur, Amt = amt, Tag = tag });
        }
        bool Pkk(double ch) => Rng.Next() < ch;
        int PerkCount => Taken.Count;

        public double EffDmg()
        {
            double m = 1 + BSum("atk");
            if (P.Has("p_collAtk")) m *= 1 + PK.C("PERK_COLL_ATK") * PerkCount;
            if (P.Has("p_noShAtk") && P.Sh <= 0) m *= PK.C("PERK_NOSH_ATK");
            return P.Dmg * m;
        }
        public double EffAspd() => P.Aspd * (1 + BSum("aspd")) * (P.Has("p_noShAspd") && P.Sh <= 0 ? PK.C("PERK_NOSH_ASPD") : 1);
        public double EffCritR()
        {
            if (P.Has("p_berserk")) return 0;
            double c = P.CritR + BSum("critR");
            if (P.Has("p_collCrit")) c += PK.C("PERK_COLL_CRIT") * PerkCount;
            if (P.Has("p_critStack")) c += P.CritStk;
            return c;
        }
        public double EffCritF() => P.CritF + BSum("critF");
        public double EffDef() => Math.Min(C.DefCap, P.Def + BSum("def"));
        public double EffEvade() => Math.Min(C.EvadeCap, P.Evade + BSum("evade"));
        public double EffCounter() => P.Counter;
        public double EffSteal() => P.Steal;

        // ───────────────────────── 회복 · 수리 ─────────────────────────
        void Repair(double amt)
        {
            if (amt <= 0) return;
            double before = P.Sh;
            P.Sh = Math.Min(P.MaxSh, P.Sh + amt * (1 + P.RepairAmp));
            Emit(EvKind.Repair, null, P.Sh - before);
        }
        void Heal(double amt, bool noBoost = false)
        {
            if (!noBoost) amt *= 1 + P.HealAmp;
            double before = P.Hp;
            P.Hp = Math.Min(P.MaxHp, P.Hp + amt);
            if (P.Has("p_healRepair") && P.Hp > before) Repair(P.Hp - before);
            if (P.Hp > before) Emit(EvKind.Heal, null, P.Hp - before);
        }

        // ───────────────────────── 적 조회 ─────────────────────────
        public List<EnemyState> AliveList()
        {
            var o = new List<EnemyState>();
            foreach (var n in Nodes) foreach (var e in n.Enemies) if (e.Hp > 0) o.Add(e);
            return o;
        }
        BattleNode FrontNode()
        {
            EnemyState b = null;
            foreach (var n in Nodes) foreach (var e in n.Enemies) if (e.Hp > 0 && (b == null || e.WorldX < b.WorldX)) b = e;
            return b?.Wave;
        }
        EnemyState RandTarget()
        {
            var pool = new List<EnemyState>();
            foreach (var e in AliveList()) { double d = e.WorldX - P.WorldX; if (d > EngineConst.TargetRangeBack && d < EngineConst.TargetRangeFront) pool.Add(e); }
            return pool.Count > 0 ? Rng.Pick(pool) : null;
        }

        // ───────────────────────── 처치 · 경험치 ─────────────────────────
        void OnKill(EnemyState e, double over)
        {
            if (e.Dead) return; e.Dead = true;
            Kills++;
            Gold += RngUtil.JsRound(D.Tune.GoldKillBaseAt(Chapter) * Rng.Range(EngineConst.GoldRandMin, EngineConst.GoldRandMax) * P.GoldMul);
            Emit(EvKind.Kill, e, 0);
            if (P.Has("p_killSpear") && Pkk(P.PxGet("p_killSpear"))) FireSpear(1);
            if (P.Has("p_killBolt") && Pkk(P.PxGet("p_killBolt"))) FireBoltsAll(e.Wave);
            if (P.Has("p_killArrow") && Pkk(P.PxGet("p_killArrow"))) FireArrows(3);
            if (P.Has("p_killAxe") && Pkk(P.PxGet("p_killAxe"))) FireAxe(2);
            if (P.Has("p_overkill") && over > 0) Heal(over);
            if (P.Has("p_killEvBuff")) RefreshBuff("evade", PK.C("PERK_KILLEV_A"), PK.C("PERK_KILLEV_T"), "p_killEvBuff");
            if (P.Has("p_killAtkStk") && Pkk(PK.C("PERK_KSTACK_CH"))) P.Dmg *= 1 + PK.C("PERK_KSTACK_ATK");
            if (P.Has("p_killEvStk") && Pkk(PK.C("PERK_KSTACK_CH"))) P.Evade += PK.C("PERK_KSTACK_EV");
            if (P.Has("p_killHealN") && Pkk(PK.C("PERK_KHEAL_CH"))) Heal(P.MaxHp * PK.C("PERK_KHEAL_F"));
            if (P.Has("p_killRepair") && Pkk(PK.C("PERK_KREPAIR_CH"))) Repair(P.MaxSh * PK.C("PERK_KREPAIR_F"));
            if (P.Has("p_killSureCrit")) P.SureCrit = true;
            if (P.Has("p_berserkStk")) P.BsStk++;
            if (P.Has("p_killDash") && e.Wave != null) { foreach (var x in e.Wave.Enemies) if (x.Hp > 0) { P.Dash = true; break; } }
            if (e.IsBoss) Cleared = true;
            GainExp(e.IsBoss ? D.Tune.ExpBoss : D.Tune.ExpKill);
        }

        void GainExp(int n)
        {
            P.Exp += n;
            while (P.Exp >= D.Tune.ExpNeed(P.Level))
            {
                P.Exp -= D.Tune.ExpNeed(P.Level); P.Level++;
                if (!Cleared) GrantNextPerk();
            }
        }

        public bool HasPerkLeft()
        {
            if (Taken.Count >= PK.PicksPerRun) return false;
            foreach (var p in PK.Perks) if (!Taken.Contains(p)) return true;
            return false;
        }

        void GrantNextPerk()
        {
            PerkChances++;
            if (Opt.NoPerk) return;
            if (Taken.Count >= PK.PicksPerRun) return;
            if (Opt.LadderPerkMode)
            {
                if (Taken.Count < Base10.Count) PickPerk(Base10[Taken.Count]);
                return;
            }
            if (Policy is InteractivePolicy) { PendingLevelUps++; Emit(EvKind.LevelUp, null, P.Level); return; }   // 팝업이 열릴 때 굴린다(index.html 과 같음)
            var offer = Perks.Offer(D, Taken, P.Has("p_nobleEye"), Rng);
            if (offer.Count == 0) return;
            var pick = Policy.PickPerk(this, offer);
            if (pick == null) pick = Perks.SimPick(offer);
            PickPerk(pick);
        }

        public void PickPerk(PerkDef perk)
        {
            Perks.Apply(D, P, perk);
            Taken.Add(perk);
            ApplyCollHp(Taken.Count);
            Emit(EvKind.Perk, null, 0, false, perk.Id);
        }
        void ApplyCollHp(int n)
        {
            if (!P.Has("p_collHp")) return;
            double f = 1 + PK.C("PERK_COLL_HP") * n;
            P.MaxHp = P.MaxHp / P.CollHpF * f;
            P.CollHpF = f;
        }

        PerkDef DevilPerkFor()
        {
            if (Opt.NoPerk) return null;
            if (Taken.Count >= PK.PicksPerRun) return null;
            if (Opt.LadderPerkMode) return Taken.Count < Base10.Count ? Base10[Taken.Count] : null;
            return Perks.OfferDevil(D, Taken, Rng);
        }
        public double PayDevilCost()
        {
            P.MaxHp = Math.Max(1, P.MaxHp - P.MaxHp * PK.DevilCostMaxHp);
            P.Hp = Math.Min(P.Hp, P.MaxHp);
            return P.MaxHp;
        }

        // ───────────────────────── 스턴 · 빗맞음 · 방어막 · 반사 ─────────────────────────
        void ApplyStun(EnemyState e, double sec)
        {
            if (e == null || e.Hp <= 0) return;
            double s = sec; if (e.IsBoss) s *= C.StunBossMul;
            e.Stun = Math.Max(e.Stun, s); Stuns++;
            Emit(EvKind.Stun, e, s);
        }
        void ProcOnMiss(EnemyState e) { Misses++; Emit(EvKind.Miss, e, 0); }
        void GainWard(double ch) { if (ch > 0 && Pkk(ch)) { P.Ward++; Emit(EvKind.Ward, null, P.Ward); } }
        void Reflect(EnemyState src, double amt)
        {
            if (src == null || src.Hp <= 0 || amt <= 0) return;
            src.Hp -= amt; Emit(EvKind.Reflect, src, amt);
            if (src.Hp <= 0) OnKill(src, -src.Hp);
        }

        // ───────────────────────── 데미지 (sim.js dealDmg) ─────────────────────────
        bool DealDmg(EnemyState e, double ratio, bool fromBasic)
        {
            if (e.Hp <= 0) return false;
            bool full = e.Hp >= e.MaxHp - EngineConst.FullHpEps;
            double cr = EffCritR();
            if (fromBasic && P.NextCrit) cr = 100;
            if (fromBasic && P.SureCrit) cr = 100;
            bool crit = Rng.Next() * 100 < cr;
            if (fromBasic && P.NextCrit) P.NextCrit = false;
            if (fromBasic && P.SureCrit) P.SureCrit = false;
            AtkTries++;
            if (Rng.Next() < C.EnemyEvade) { Miss++; ProcOnMiss(e); return false; }
            if (fromBasic && P.Has("p_critStack")) P.CritStk = crit ? 0 : P.CritStk + (int)PK.C("PERK_CSTACK_A");
            double d = EffDmg() * ratio * (crit ? EffCritF() / 100 : 1) * Rng.Range(EngineConst.DmgJitterMin, EngineConst.DmgJitterMax);
            double addBonus = 0;
            if (full && P.Has("p_fullHp")) addBonus += PK.C("PERK_FULLHP_A");
            if (addBonus != 0) d *= 1 + addBonus;
            e.Hp -= d;
            Emit(EvKind.Hit, e, d, crit);
            if (P.Steal > 0) Heal(d * P.Steal / 100, true);
            if (crit)
            {
                if (P.Has("p_stunCritN") && Pkk(PK.C("PERK_STUNC_N"))) ApplyStun(e, PK.C("PERK_STUNC_T"));
                if (P.Has("p_stunCritR") && Pkk(PK.C("PERK_STUNC_R"))) ApplyStun(e, PK.C("PERK_STUNC_T"));
                if (P.Has("p_stunCritL") && Pkk(PK.C("PERK_STUNC_L"))) ApplyStun(e, PK.C("PERK_STUNC_T"));
                if ((P.Has("p_critSpearR") || P.Has("p_critSpearL") || P.Has("p_critBoltL")) && ProcN < C.ProcTickCap)
                {
                    ProcN++;
                    if (P.Has("p_critSpearR") && Pkk(PK.C("PERK_CRITSP_R"))) FireSpear(1);
                    if (P.Has("p_critSpearL") && Pkk(PK.C("PERK_CRITSP_L"))) FireSpear(1);
                    if (P.Has("p_critBoltL") && Pkk(PK.C("PERK_CRITBOLT_L"))) FireBoltsAll(e.Wave);
                }
                int gca = (int)P.PxGet("g_critAxe");
                if (gca > 0 && ProcN < C.ProcTickCap)
                {
                    ProcN++;
                    for (int i = 0; i < gca; i++) if (Pkk(EngineConst.GearAxeCh)) FireAxe(1);
                }
            }
            if (e.Hp <= 0) OnKill(e, -e.Hp);
            if (fromBasic) Cleave(e, d);
            return crit;
        }

        void Cleave(EnemyState tgt, double dmg)
        {
            bool n = P.Has("p_cleaveN"), r = P.Has("p_cleaveR"), l = P.Has("p_cleaveL");
            if (!(n || r || l) || dmg <= 0) return;
            EnemyState back = null;
            foreach (var e in AliveList())
            {
                if (e == tgt || e.Wave != tgt.Wave) continue;
                if (e.WorldX <= tgt.WorldX) continue;
                if (back == null || e.WorldX < back.WorldX) back = e;
            }
            if (back == null) return;
            void Hit()
            {
                if (back.Hp <= 0) return;
                AtkTries++;
                if (Rng.Next() < C.EnemyEvade) { Miss++; ProcOnMiss(back); return; }
                back.Hp -= dmg; Emit(EvKind.Hit, back, dmg, false);
                if (back.Hp <= 0) OnKill(back, -back.Hp);
            }
            if (n && Pkk(PK.C("PERK_CLEAVE_N"))) Hit();
            if (r && Pkk(PK.C("PERK_CLEAVE_R"))) Hit();
            if (l && Pkk(PK.C("PERK_CLEAVE_L"))) Hit();
        }

        // ───────────────────────── 소환 (sim.js fire*) ─────────────────────────
        void SummonHit(EnemyState e, double ratio)
        {
            DealDmg(e, ratio, false);
            if (ProcN < C.ProcTickCap) { ProcN++; ProcOnAttack(e); }
        }
        void ProjHit(Projectile pr, EnemyState e) => SummonHit(e, pr.Kind == ProjKind.Axe ? C.RAxe : pr.Ratio);
        void PushProj(Projectile pr)
        {
            if (Projs.Count < C.ProjCap) { Projs.Add(pr); Emit(EvKind.Proj, pr.Target, 0, false, null, pr); return; }
            if (pr.Hit != null)
            {
                var list = new List<EnemyState>();
                foreach (var e in AliveList()) if ((pr.Node == null || e.Wave == pr.Node) && e.WorldX >= pr.X - EngineConst.ProjHitTol && e.WorldX <= pr.MaxX) list.Add(e);
                list.Sort((a, b) => a.WorldX.CompareTo(b.WorldX));
                for (int i = 0; i < list.Count && i < pr.Pierce; i++) ProjHit(pr, list[i]);
            }
            else if (pr.Target != null && pr.Target.Hp > 0) ProjHit(pr, pr.Target);
        }
        void FireAxe(int n)
        {
            for (int k = 0; k < n; k++)
            {
                var t = RandTarget();
                if (t != null) PushProj(new Projectile { Kind = ProjKind.Axe, X = P.WorldX + EngineConst.ProjSpawnDx, StartX = P.WorldX + EngineConst.ProjSpawnDx, Target = t, TargetX0 = t.WorldX, Ratio = C.RAxe, Spd = C.AxeSpeed });
            }
        }
        void FireArrows(int n)
        {
            if (P.Has("p_spearAvatar")) { FireSpear(n); return; }
            for (int k = 0; k < n; k++)
            {
                var t = RandTarget();
                if (t != null) PushProj(new Projectile { Kind = ProjKind.Arrow, X = P.WorldX + EngineConst.ProjSpawnDx, StartX = P.WorldX + EngineConst.ProjSpawnDx, Target = t, TargetX0 = t.WorldX, Ratio = C.RArrow, Spd = EngineConst.ArrowSpeed });
            }
        }
        void FireBolts(int n)
        {
            for (int k = 0; k < n; k++)
            {
                var t = RandTarget(); if (t == null) continue;
                Emit(EvKind.Bolt, t, 0);
                SummonHit(t, C.RBolt);
            }
        }
        void FireBoltsAll(BattleNode node)
        {
            var nd = node ?? FrontNode(); if (nd == null) return;
            var list = new List<EnemyState>();
            foreach (var e in nd.Enemies) if (e.Hp > 0) list.Add(e);
            foreach (var e in list) if (e.Hp > 0) { Emit(EvKind.Bolt, e, 0); SummonHit(e, C.RBolt); }
        }
        void FireWave(int n)
        {
            for (int k = 0; k < n; k++)
                PushProj(new Projectile { Kind = ProjKind.Wave, X = P.WorldX + EngineConst.ProjSpawnDx, StartX = P.WorldX + EngineConst.ProjSpawnDx, Ratio = C.RWave, Spd = EngineConst.WaveSpeed, MaxX = P.WorldX + C.WaveReach, Hit = new HashSet<EnemyState>(), Pierce = C.PierceWave, Node = FrontNode() });
        }
        void FireSpear(int n)
        {
            for (int k = 0; k < n; k++)
                PushProj(new Projectile { Kind = ProjKind.Spear, X = P.WorldX + EngineConst.ProjSpawnDx, StartX = P.WorldX + EngineConst.ProjSpawnDx, Ratio = C.RSpear, Spd = C.SpearSpeed, MaxX = P.WorldX + C.SpearReach, Hit = new HashSet<EnemyState>(), Pierce = C.PierceSpear, Node = FrontNode() });
        }

        void ProcOnAttack(EnemyState e)
        {
            if (P.Has("p_aspdAtk")) AddBuff("aspd", PK.C("PERK_ASPDATK_A"), PK.C("PERK_ASPDATK_T"));
        }

        // ───────────────────────── 반격 · 피격 (sim.js doCounter · hitPlayer) ─────────────────────────
        void DoCounter(EnemyState src)
        {
            if (src == null || src.Hp <= 0) return;
            AtkTries++;
            if (Rng.Next() < C.EnemyEvade) { Miss++; ProcOnMiss(src); return; }
            double cd = EffDmg() * EngineConst.CounterRatio;
            double ctCr = (P.Has("p_ctCritN") ? PK.C("PERK_CTCRIT_N") : 0) + (P.Has("p_ctCritR") ? PK.C("PERK_CTCRIT_R") : 0);
            bool crit = false;
            if (ctCr > 0 && Rng.Next() * 100 < ctCr) { cd *= EffCritF() / 100; crit = true; }
            if (P.Has("p_ctDmgN")) cd *= PK.C("PERK_CTDMG_N");
            if (P.Has("p_ctDmgR")) cd *= PK.C("PERK_CTDMG_R");
            src.Hp -= cd;
            Emit(EvKind.Counter, src, cd, crit);
            if (P.Has("p_spearCt") && Pkk(PK.C("PERK_SUMMON_CH"))) FireSpear(1);
            if (src.Hp <= 0) OnKill(src, -src.Hp);
        }

        void HitPlayer(double dmg, bool isMelee, EnemyState src)
        {
            if (Rng.Next() * 100 < EffEvade())
            {
                Emit(EvKind.PlayerEvade, src, 0);
                int gev = (int)P.PxGet("g_evAxe");
                for (int i = 0; i < gev; i++) if (Pkk(EngineConst.GearAxeCh)) FireAxe(1);
                int geh = (int)P.PxGet("g_evHeal");
                if (P.Hp < P.MaxHp * EngineConst.LowHpEvHeal) for (int i = 0; i < geh; i++) if (Pkk(EngineConst.GearEvHealCh)) Heal(P.MaxHp * EngineConst.EvHealF);
                if (P.Has("p_evadeHeal") && Pkk(PK.C("PERK_EVHEAL_CH"))) Heal(P.MaxHp * PK.C("PERK_EVHEAL_F"));
                if (P.Has("p_arrowEv") && Pkk(PK.C("PERK_SUMMON_N"))) FireArrows(1);
                if (P.Has("p_arrowEvR") && Pkk(PK.C("PERK_SUMMON_R"))) FireArrows(1);
                if (P.Has("p_arrowEvL") && Pkk(PK.C("PERK_SUMMON_L"))) FireArrows(1);
                if (P.Has("p_spearEvL") && Pkk(PK.C("PERK_SUMMON_SP"))) FireSpear(1);
                if (P.Has("p_evHealR") && Pkk(PK.C("PERK_EVHEAL_R"))) Heal(P.MaxHp * PK.C("PERK_EVHEAL_F"));
                if (P.Has("p_evHealL") && Pkk(PK.C("PERK_EVHEAL_L"))) Heal(P.MaxHp * PK.C("PERK_EVHEAL_F"));
                if (P.Has("p_evRepairR") && Pkk(PK.C("PERK_EVREP_R"))) Repair(P.MaxSh * PK.C("PERK_EVREP_F"));
                if (P.Has("p_evRepairL") && Pkk(PK.C("PERK_EVREP_L"))) Repair(P.MaxSh * PK.C("PERK_EVREP_F"));
                if (src != null && src.Hp > 0 && P.Has("p_evadeStun") && Pkk(PK.C("PERK_EVSTUN_CH"))) ApplyStun(src, PK.C("PERK_STUNC_T"));
                if (src != null && src.Hp > 0 && P.Has("p_execEvN") && Pkk(PK.C("PERK_EXEC_N"))) { src.Hp = 0; OnKill(src, 0); }
                if (src != null && src.Hp > 0 && P.Has("p_execEvR") && Pkk(PK.C("PERK_EXEC_R"))) { src.Hp = 0; OnKill(src, 0); }
                if (src != null && src.Hp > 0 && P.Has("p_execEvL") && Pkk(PK.C("PERK_EXEC_L"))) { src.Hp = 0; OnKill(src, 0); }
                return;
            }
            if (P.Ward > 0) { P.Ward--; Emit(EvKind.Ward, src, -1); return; }
            bool ign1 = P.Has("p_ignoreN") && Pkk(PK.C("PERK_IGN_N"));
            bool ign2 = P.Sh > 0 && P.Has("p_shWallL") && Pkk(PK.C("PERK_SHWALL_L"));
            if (ign1 || ign2) { Emit(EvKind.Ignore, src, 0); return; }
            bool hadSh = P.Sh > 0;
            double d = dmg * (1 - EffDef() / 100);
            double thornBase = d;
            double shDmg = 0;
            if (P.Sh > 0) { double ab = Math.Min(P.Sh, d); P.Sh -= ab; d -= ab; shDmg = ab; }
            if (d > 0)
            {
                P.Hp -= d;
                if (P.Hp <= 0) { P.Hp = 0; Dead = true; Emit(EvKind.PlayerHit, src, shDmg, false, null, null, d); return; }
            }
            Emit(EvKind.PlayerHit, src, shDmg, false, null, null, d);
            double thornM = P.PxGet("p_thorns") + (hadSh ? P.PxGet("g_thornSh") : 0);
            if (thornM != 0 && isMelee && src != null) Reflect(src, thornBase * thornM);
            if (P.Has("p_shRefL") && hadSh && src != null && Pkk(PK.C("PERK_SHREF_L"))) Reflect(src, thornBase);
            GainWard(P.Has("p_wardHitN") ? PK.C("PERK_WARD_N") : 0);
            GainWard(P.Has("p_wardHitR") ? PK.C("PERK_WARD_R") : 0);
            GainWard(P.Has("p_wardHitL") ? PK.C("PERK_WARD_L") : 0);
            if (P.Has("p_axeHit") && Pkk(PK.C("PERK_SUMMON_N"))) FireAxe(1);
            if (P.Has("p_axeHitR") && Pkk(PK.C("PERK_SUMMON_R"))) FireAxe(1);
            if (P.Has("p_axeHitL") && Pkk(PK.C("PERK_SUMMON_L"))) FireAxe(1);
            if (P.Has("p_spearHitL") && Pkk(PK.C("PERK_SUMMON_SP"))) FireSpear(1);
            int gha = (int)P.PxGet("g_hitAxe");
            for (int i = 0; i < gha; i++) if (Pkk(EngineConst.GearAxeCh)) FireAxe(1);
            if (isMelee && src != null && src.Hp > 0)
            {
                bool cc = Rng.Next() * 100 < EffCounter();
                if (cc) DoCounter(src);
            }
        }

        void PlayerStrike(EnemyState e)
        {
            double ratio = 1;
            if (P.NextAtk > 0) { ratio *= 1 + P.NextAtk; P.NextAtk = 0; }
            if (P.Has("p_berserkStk") && P.BsStk > 0) { P.BsStk--; ratio *= PK.C("PERK_BSTK_M"); }
            P.StrikeT = 0.18;
            DealDmg(e, ratio, true);
            ProcOnAttack(e);
            ProcNHit();
        }

        void ProcNHit()
        {
            foreach (var kv in PK.NHitPerks)
            {
                string id = kv.Key; int period = kv.Value;
                if (!P.Has(id)) continue;
                int c = (P.NHit.TryGetValue(id, out var cur) ? cur : 0) + 1;
                if (c >= period) { P.NHit[id] = 0; FireNHit(id); }
                else P.NHit[id] = c;
            }
        }
        /// <summary>«N타마다» 발동 — id 꼬리(N/R/L)가 발수 1/2/3 (perks.json 문구 «화살 1개/2개/3개» 와 같은 구조).</summary>
        void FireNHit(string id)
        {
            int shots = id.EndsWith("L") ? 3 : id.EndsWith("R") ? 2 : 1;
            if (id.StartsWith("p_nArrow")) FireArrows(shots);
            else if (id.StartsWith("p_nAxe")) FireAxe(shots);
            else if (id.StartsWith("p_nBolt")) FireBolts(shots);
            else if (id.StartsWith("p_nSpear")) FireSpear(1);
            else if (id.StartsWith("p_nHeal")) Heal(P.MaxHp * PK.C("PERK_NHEAL_F"));
            else throw new InvalidOperationException("nHitPerks 에 모르는 특전: " + id);
        }

        // ───────────────────────── 이벤트 결정 (팝업) ─────────────────────────
        void OpenLevelUp()
        {
            PerkChances++;
            if (!HasPerkLeft()) { PendingLevelUps = Math.Max(0, PendingLevelUps - 1); if (PendingLevelUps > 0) OpenLevelUp(); return; }
            var offer = Perks.Offer(D, Taken, P.Has("p_nobleEye"), Rng);
            if (offer.Count == 0) { PendingLevelUps = Math.Max(0, PendingLevelUps - 1); return; }
            Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
        }
        void AfterResolve()
        {
            Pending = null;
            if (PendingLevelUps > 0 && !Over) { PendingLevelUps--; OpenLevelUp(); }
        }
        /// <summary>레벨업 3택 새로고침 — 같은 굴림(<see cref="Perks.Offer"/>)을 다시 한다. 팝업 1회당 <see cref="EngineConst.RerollPerLevelUp"/> 번. 성공하면 true.</summary>
        public bool RerollOffer()
        {
            if (Pending == null || Pending.Kind != PendingKind.LevelUp) return false;
            if (Pending.Rerolls >= EngineConst.RerollPerLevelUp) return false;
            var offer = Perks.Offer(D, Taken, P.Has("p_nobleEye"), Rng);
            if (offer.Count == 0) return false;
            Pending.Offer = offer; Pending.Rerolls++;
            return true;
        }
        public int RerollsLeft => Pending != null && Pending.Kind == PendingKind.LevelUp ? Math.Max(0, EngineConst.RerollPerLevelUp - Pending.Rerolls) : 0;
        public void ResolveLevelUp(PerkDef pick)
        {
            if (Pending == null || Pending.Kind != PendingKind.LevelUp) return;
            if (pick != null && Pending.Offer.Contains(pick)) PickPerk(pick);
            AfterResolve();
        }
        public void ResolveRest(bool heal)
        {
            if (Pending == null || Pending.Kind != PendingKind.Rest) return;
            if (heal) Heal(C.RestHeal); else GainExp(C.RestExp);
            AfterResolve();
        }
        public void ResolveDevil(bool accept)
        {
            if (Pending == null || Pending.Kind != PendingKind.Devil) return;
            if (accept && Pending.DevilPerk != null) { PayDevilCost(); PerkChances++; PickPerk(Pending.DevilPerk); }
            AfterResolve();
        }
        public void ResolveAngel(double mult)
        {
            if (Pending == null || Pending.Kind != PendingKind.Angel) return;
            P.Dmg *= mult;
            Blessings.Add("천사의 축복 — 공격력 +" + Math.Round((mult - 1) * 100) + "%");
            AfterResolve();
        }

        /// <summary>이벤트 노드 처리. 정책이 즉답하면 바로 적용, 보류하면 Pending 을 세운다. true = 이벤트가 있었다(틱 나머지 건너뜀).</summary>
        bool HandleEvent(BattleNode n)
        {
            n.Done = true;
            switch (n.Type)
            {
                case NodeType.Rest:
                {
                    var r = Policy.Rest(this);
                    if (r == null) Pending = new PendingDecision { Kind = PendingKind.Rest };
                    else if (r.Value) Heal(C.RestHeal); else GainExp(C.RestExp);
                    break;
                }
                case NodeType.Devil:
                {
                    var dp = DevilPerkFor();
                    if (dp == null) break;   // 남은 전설이 없다 — 거래 불성립(비용 없음)
                    var r = Policy.Devil(this, dp);
                    if (r == null) Pending = new PendingDecision { Kind = PendingKind.Devil, DevilPerk = dp };
                    else if (r.Value) { PayDevilCost(); PerkChances++; PickPerk(dp); }
                    break;
                }
                case NodeType.Angel:
                {
                    var m = Policy.Angel(this);
                    if (m == null) Pending = new PendingDecision { Kind = PendingKind.Angel };
                    else { P.Dmg *= m.Value; Blessings.Add("천사의 축복 — 공격력 +" + Math.Round((m.Value - 1) * 100) + "%"); }
                    break;
                }
            }
            return true;
        }

        // ───────────────────────── 틱 (sim.js while 루프 한 바퀴) ─────────────────────────
        /// <summary>한 틱(dt = 1/30). Pending 이 있으면 아무것도 하지 않는다. 반환 = 진행했는가.</summary>
        public bool Tick()
        {
            if (Pending != null || Over) return false;
            double dt = EngineConst.Dt;
            T += dt;
            ProcN = 0;
            P.StrikeT = Math.Max(0, P.StrikeT - dt); P.HitT = Math.Max(0, P.HitT - dt);
            foreach (var k in BuffKeys) { var arr = P.Buffs[k]; for (int i = arr.Count - 1; i >= 0; i--) { arr[i].T -= dt; if (arr[i].T <= 0) arr.RemoveAt(i); } }
            var alive = AliveList();
            if (alive.Count == 0) { return true; }
            foreach (var n in Nodes)
            {
                if (!n.Done && (n.Type == NodeType.Rest || n.Type == NodeType.Devil || n.Type == NodeType.Angel) && P.WorldX > n.X - EngineConst.EventTriggerDist)
                {
                    HandleEvent(n);
                    if (Pending == null && PendingLevelUps > 0) { PendingLevelUps--; OpenLevelUp(); }
                    return true;
                }
            }
            alive.Sort((a, b) => a.WorldX.CompareTo(b.WorldX));
            var tgt = alive[0];
            double dist = tgt.WorldX - P.WorldX;
            if (dist > C.StopDistance)
            {
                P.WorldX += C.PlayerSpeed * P.WalkMul * (P.Dash ? C.DashMul : 1) * dt;
                P.AtkTimer = Math.Min(P.AtkTimer, EngineConst.WalkAtkTimerCap);
            }
            else
            {
                P.Dash = false;
                P.AtkTimer -= dt * EffAspd();
                if (P.AtkTimer <= 0) { P.AtkTimer += 1; PlayerStrike(tgt); }
            }
            foreach (var e in alive)
            {
                if (e.Hp <= 0) continue;
                e.HitT = Math.Max(0, e.HitT - dt); e.StrikeT = Math.Max(0, e.StrikeT - dt);
                if (e.Stun > 0) { e.Stun -= dt; continue; }
                if (e.Slow > 0) e.Slow -= dt;
                double d = e.WorldX - P.WorldX;
                double ivm = e.Slow > 0 ? C.SlowMul : 1;
                if (!e.Ranged)
                {
                    if (d < C.MeleeEnemy)
                    {
                        e.AtkTimer -= dt;
                        if (e.AtkTimer <= 0)
                        {
                            e.AtkTimer += (e.IsBoss ? C.BossInterval : C.MeleeInterval) * ivm;
                            double dm = e.Dmg;
                            if (e.IsBoss) { e.Hits++; if (e.Hits % C.BossTripleHitEvery == 0) dm *= C.BossTripleHitMul; }
                            e.StrikeT = 0.18;
                            HitPlayer(dm, true, e);
                            if (Dead) break;
                        }
                    }
                }
                else if (d < C.RangedEnemyMax && d > C.RangedEnemyMin)
                {
                    e.AtkTimer -= dt;
                    if (e.AtkTimer <= 0)
                    {
                        e.AtkTimer += C.RangedInterval * ivm;
                        e.StrikeT = 0.18;
                        Arrows.Add(new EnemyArrow { X = e.WorldX + EngineConst.ArrowSpawnDx, Dmg = e.Dmg, Friendly = false, Src = e });
                    }
                }
            }
            if (Dead) return true;
            for (int i = Arrows.Count - 1; i >= 0; i--)
            {
                var a = Arrows[i]; a.X -= C.EnemyArrowSpeed * dt; bool hit = false;
                if (!hit && a.X <= P.WorldX + EngineConst.ArrowHitDx) { HitPlayer(a.Dmg, false, a.Src); hit = true; }
                if (hit || a.X < P.WorldX + EngineConst.ArrowCullDx) Arrows.RemoveAt(i);
            }
            if (Dead) return true;
            for (int i = Projs.Count - 1; i >= 0; i--)
            {
                var pr = Projs[i]; pr.X += pr.Spd * dt; bool done = false;
                if (pr.Kind == ProjKind.Spear || pr.Kind == ProjKind.Wave)
                {
                    foreach (var e in AliveList())
                    {
                        if (pr.Node != null && e.Wave != pr.Node) continue;
                        if (!pr.Hit.Contains(e) && Math.Abs(e.WorldX - pr.X) < EngineConst.ProjHitTol)
                        {
                            pr.Hit.Add(e); ProjHit(pr, e);
                            if (pr.Hit.Count >= pr.Pierce) { done = true; break; }
                        }
                    }
                    if (pr.X > pr.MaxX) done = true;
                }
                else
                {
                    if (pr.Target == null || pr.Target.Hp <= 0) done = true;
                    else if (pr.X >= pr.Target.WorldX - EngineConst.ProjArriveDx) { ProjHit(pr, pr.Target); done = true; }
                }
                if (done) Projs.RemoveAt(i);
            }
            if (Pending == null && PendingLevelUps > 0) { PendingLevelUps--; OpenLevelUp(); }
            return true;
        }
        static readonly string[] BuffKeys = { "atk", "aspd", "critR", "critF", "def", "evade" };

        /// <summary>시뮬용: 끝까지 돌린다(정책이 즉답한다는 전제).</summary>
        public RunResult RunToEnd()
        {
            while (!Over)
            {
                if (Pending != null) throw new InvalidOperationException("RunToEnd: 정책이 보류를 돌려줬다 — 시뮬 정책을 쓸 것");
                var alive = AliveList();
                if (alive.Count == 0) break;
                Tick();
            }
            return Result();
        }

        public RunResult Result()
        {
            var ids = new List<string>(); foreach (var t in Taken) ids.Add(t.Id);
            return new RunResult { Clear = Cleared, Time = T, Gold = Gold, Taken = ids, Level = P.Level, AtkTries = AtkTries, Miss = Miss, Kills = Kills };
        }

        // ───────────────────────── 연출 이벤트 ─────────────────────────
        void Emit(EvKind k, EnemyState e, double v, bool crit = false, string text = null, Projectile pr = null, double v2 = 0)
        {
            if (!Opt.EmitEvents) return;
            if (k == EvKind.Hit && e != null) e.HitT = 0.12;
            if (k == EvKind.PlayerHit) P.HitT = 0.12;
            Events.Add(new BattleEvent { Kind = k, Enemy = e, Value = v, Value2 = v2, Crit = crit, Text = text, Proj = pr });
        }
    }
}
