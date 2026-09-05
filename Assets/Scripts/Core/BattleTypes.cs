using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// sim.js 엔진에 **이름 없는 리터럴**로만 있어 data/*.json 에 아직 안 뽑힌 값들. (밸런스 노브가 아닌 «동작 상수»)
    /// ⚑ 이 클래스가 이 레포에서 유일하게 게임 숫자를 코드에 두는 자리다 — aaaw `tools/exportData.js` 에 축이 추가되면
    ///   `CombatData` 로 옮기고 여기서 지운다 (PROGRESS «주인 승인 대기» 등재).
    /// </summary>
    public static class EngineConst
    {
        public const double EventTriggerDist = 95;      // sim.js runChapter: p.worldX > n.x - 95 → 이벤트 발동
        public const double BossOffset = 60;            // 보스 배치 x + 60
        public const double TargetRangeBack = -30;      // randTarget: d > -30
        public const double TargetRangeFront = 540;     // randTarget: d < 540
        public const double ProjSpawnDx = 14;           // 아군 투사체 생성 x 오프셋
        public const double ProjHitTol = 16;            // 관통형 적중 판정 |e.x - pr.x| < 16
        public const double ProjArriveDx = 10;          // 유도형 도달 판정 pr.x >= tgt.x - 10
        public const double ArrowSpawnDx = -18;         // 적 화살 생성 x 오프셋
        public const double ArrowHitDx = 8;             // 적 화살 명중 a.x <= p.x + 8
        public const double ArrowCullDx = -60;          // 적 화살 소멸 a.x < p.x - 60
        public const double ArrowSpeed = 560;           // fireArrows 투사체 속도 (combat.json 미수록)
        public const double WaveSpeed = 470;            // fireWave 투사체 속도 (combat.json 미수록)
        public const double EnemyMinAtkTimer = 0.4, EnemyMaxAtkTimer = 1.2, BossAtkTimer = 1.2;
        public const double WalkAtkTimerCap = 0.35;     // 이동 중 공격 타이머 상한
        public const double DmgJitterMin = 0.92, DmgJitterMax = 1.08;
        public const double GoldRandMin = 1.0, GoldRandMax = 1.8;
        public const double CounterRatio = 0.7;         // 반격 데미지 = 공격력 × 0.7
        public const double FullHpEps = 0.5;            // «체력이 가득 찬 적» 판정 e.hp >= maxHp - 0.5
        public const double LowHpEvHeal = 0.50, EvHealF = 0.10, GearAxeCh = 0.50, GearEvHealCh = 0.30;  // 장비 세트 옵션 c·f 의 조건/확률 (gear.json 문구에만 있음)
        public const double Dt = 1.0 / 30;              // 시뮬 틱 (sim.js dt=1/30)
        public const double MaxT = 900;                 // 한 판 시간 상한(초)
        public const int WaveKingPierce = 20;
    }

    public sealed class Buff { public double T, Amt; public string Tag; }

    public sealed class PlayerState
    {
        public double WorldX, AtkTimer, NextAtk; public bool NextCrit;
        public double Dmg, Aspd, CritR, CritF, Def, Counter, Evade, Steal, GoldMul = 1, WalkMul = 1, HealAmp, RepairAmp;
        public double MaxHp, Hp, MaxSh, Sh;
        public int Level = 1, Exp, Ward;
        public int CritStk, BsStk; public bool SureCrit, Dash; public double CollHpF = 1;
        /// <summary>연출용 타이머(공격 런지 · 피격 플래시).</summary>
        public double HitT, StrikeT;
        public readonly Dictionary<string, int> NHit = new Dictionary<string, int>();
        public readonly Dictionary<string, double> Px = new Dictionary<string, double>();
        public readonly Dictionary<string, List<Buff>> Buffs = new Dictionary<string, List<Buff>>
        {
            { "atk", new List<Buff>() }, { "aspd", new List<Buff>() }, { "critR", new List<Buff>() },
            { "critF", new List<Buff>() }, { "def", new List<Buff>() }, { "evade", new List<Buff>() },
        };
        public double PxGet(string k) => Px.TryGetValue(k, out var v) ? v : 0;
        public bool Has(string k) => PxGet(k) != 0;
    }

    public sealed class EnemyState
    {
        public int Id; public double WorldX, Hp, MaxHp, Dmg, AtkTimer, Stun, Slow;
        public bool Ranged, Dead, IsBoss; public int Hits; public BattleNode Wave;
        /// <summary>연출용: 스킨 인덱스(판 난수 X — 뷰가 정한다).</summary>
        public int Skin;
        public double HitT, StrikeT;
    }

    public sealed class BattleNode
    {
        public NodeType Type; public double X; public bool Done;
        public readonly List<EnemyState> Enemies = new List<EnemyState>();
    }

    public enum ProjKind { Axe, Arrow, Wave, Spear }

    public sealed class Projectile
    {
        public ProjKind Kind; public double X, Ratio, Spd, MaxX, StartX; public EnemyState Target; public int Pierce;
        public HashSet<EnemyState> Hit; public BattleNode Node;
        public double TargetX0; // 연출용(도끼 포물선)
    }

    public sealed class EnemyArrow { public double X, Dmg; public bool Friendly; public EnemyState Src; }

    public enum PendingKind { None, Rest, Devil, Angel, LevelUp }

    /// <summary>엔진이 결정을 기다리는 자리(팝업). 이것이 살아 있는 동안 Tick 은 시간을 한 틱도 흐르게 하지 않는다.</summary>
    public sealed class PendingDecision
    {
        public PendingKind Kind; public PerkDef DevilPerk; public List<PerkDef> Offer;
    }

    /// <summary>결정 정책. null 을 돌려주면 «보류» — 엔진이 Pending 을 세우고 멈춘다(게임). 시뮬 정책은 즉답한다.</summary>
    public interface IBattlePolicy
    {
        bool? Rest(BattleState G);                             // true = 체력 회복 · false = 경험치
        bool? Devil(BattleState G, PerkDef offered);           // true = 수락
        double? Angel(BattleState G);                          // 공격력 배수 (1.05 무료 / 1.15 광고)
        PerkDef PickPerk(BattleState G, List<PerkDef> offer);  // 고른 카드
    }

    /// <summary>sim.js 측정 정책 — 쉼터 «항상 경험치» · 악마 «항상 수락» · 천사 «항상 +5%» · 3택 «표 순서 앞선 것».</summary>
    public sealed class SimPolicy : IBattlePolicy
    {
        public const double AngelFree = 1.05, AngelAd = 1.15;
        public bool? Rest(BattleState G) => false;
        public bool? Devil(BattleState G, PerkDef offered) => true;
        public double? Angel(BattleState G) => AngelFree;
        public PerkDef PickPerk(BattleState G, List<PerkDef> offer) => Perks.SimPick(offer);
    }

    /// <summary>게임 정책 — 전부 보류(팝업이 답한다).</summary>
    public sealed class InteractivePolicy : IBattlePolicy
    {
        public bool? Rest(BattleState G) => null;
        public bool? Devil(BattleState G, PerkDef offered) => null;
        public double? Angel(BattleState G) => null;
        public PerkDef PickPerk(BattleState G, List<PerkDef> offer) => null;
    }

    public enum EvKind { Hit, Miss, Kill, PlayerHit, PlayerEvade, Ward, Ignore, Heal, Repair, Stun, Bolt, Reflect, Counter, LevelUp, Proj, Perk, BossWarn, Text }

    /// <summary>연출용 이벤트 (뷰가 매 프레임 비운다). 시뮬은 EmitEvents=false 라 만들지 않는다.</summary>
    public struct BattleEvent
    {
        public EvKind Kind; public EnemyState Enemy; public double Value, Value2; public bool Crit; public string Text; public Projectile Proj;
    }

    public sealed class RunOptions
    {
        public bool NoPerk;
        public bool LadderPerkMode;      // base10 — 기존 일반 10종을 표 순서대로 자동 획득(3택 없음)
        public bool BaseStatsLegacy20;   // 치확·반격·방어·회피 기본치 20 (재적합 자 전용)
        public bool GearOpts = true;     // false = 세트 옵션 끔
        public bool EmitEvents;          // 연출 이벤트 생성(게임)
    }

    public struct RunResult
    {
        public bool Clear; public double Time; public double Gold; public List<string> Taken; public int Level, AtkTries, Miss, Kills;
    }
}
