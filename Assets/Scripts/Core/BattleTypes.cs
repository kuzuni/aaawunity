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
        public const int RerollPerLevelUp = 1;          // 레벨업 3택 «새로고침» 허용 횟수(팝업 1회당 · 주인 지시 2026-09-05 · 원본 index.html 에 없는 기능 → 승인 대기 21)
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
        /// <summary>이 투사체를 <b>쏜 것</b>(특전 id·«gear»·«pet») — 맞은 뒤 뜨는 글자가 그 아이콘을 쓴다(T458 1항). 모르면 <c>null</c>.</summary>
        public string Src;
        public HashSet<EnemyState> Hit; public BattleNode Node;
        public double TargetX0; // 연출용(도끼 포물선)
    }

    public sealed class EnemyArrow { public double X, Dmg; public bool Friendly; public EnemyState Src; }

    public enum PendingKind { None, Rest, Devil, Angel, LevelUp }

    /// <summary>엔진이 결정을 기다리는 자리(팝업). 이것이 살아 있는 동안 Tick 은 시간을 한 틱도 흐르게 하지 않는다.</summary>
    public sealed class PendingDecision
    {
        public PendingKind Kind; public PerkDef DevilPerk; public List<PerkDef> Offer;
        public int Rerolls;   // 이 팝업에서 쓴 새로고침 수 (≤ EngineConst.RerollPerLevelUp)
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
        /// <summary>이 수를 <b>낸 것</b> — 특전 id(«p_killBolt») 또는 장비·펫(«gear»·«pet») · 기본 공격은 <c>null</c>(T458 1항 · 주인 «왜 이게 떴는지»).
        /// <para>⚠ <b>칸을 더하기만 한다</b> — 난수도 틱 순서도 안 건드린다(파리티 21칸). 채우는 쪽이 모르면 <c>null</c> 이고, 그때 화면은 아이콘을 안 붙인다.</para></summary>
        public string Src;
    }

    public sealed class RunOptions
    {
        public bool NoPerk;
        public bool LadderPerkMode;      // base10 — 기존 일반 10종을 표 순서대로 자동 획득(3택 없음)
        public bool BaseStatsLegacy20;   // 치확·반격·방어·회피 기본치 20 (재적합 자 전용)
        public bool GearOpts = true;     // false = 세트 옵션 끔
        public bool EmitEvents;          // 연출 이벤트 생성(게임)

        // ───────── T183 던전 판(주인 2026-09-07 12:0X) — 셋 다 기본값이 «일반 챕터 전투와 똑같은 판» 이다 ─────────
        /// <summary>굴릴 수 있는 특전 등급의 <b>하한</b>(0 = 제한 없음 · 2 = 맨 위 등급만 = 지옥의 문 «전설·신화만»).</summary>
        public int MinPerkGrade;
        /// <summary>판을 시작하자마자 <b>자동으로</b> 집어 주는 특전 수(원정 5 · 3택 팝업 없이 같은 <c>Rng</c> 로 굴린다).</summary>
        public int StartPerks;
        /// <summary>시작 레벨(원정 5) — 다음 렙업 필요 경험치는 엔진이 <c>ExpNeed(Level)</c> 를 보므로 저절로 그 레벨 기준이 된다.</summary>
        public int StartLevel = 1;

        // ───────── T240 3항 아레나 1대1(주인 2026-09-08 11:2X «PvP 인게임 만들어줘 · 더미 데이터로 일단») ─────────
        /// <summary>
        /// 아레나 <b>1대1</b> 판이면 그 «상대 하나» 의 스탯, <c>null</c>(기본값)이면 <b>지금까지와 똑같은 챕터 전투</b>다.
        /// <para>
        /// 값은 <see cref="ArenaFoe.Of(ArenaFoeData, double, double)"/> 가 상대 전투력에서 푼 것이다.
        /// 켜지면 챕터 표(웨이브·이벤트·보스)를 <b>아예 안 읽고</b> 적 하나짜리 판을 세운다 —
        /// 지시서 §2 T240 3항 «웨이브 없음 · 1대1» 그대로이고, 7항의 «전부 로컬 더미» 도 그대로다(서버·네트워크 0).
        /// </para>
        /// <para>
        /// ⚠ <b>기본값이 <c>null</c> 인 것이 시드 골든(T2)의 안전장치다</b> — 이 값이 없으면 엔진이 지나는 길이
        /// 한 줄도 안 달라진다(지시서 3항 «기존 챕터 전투의 시드 골든은 건드리면 안 된다 · PvP 는 별도 진입점»).
        /// T183 던전 판이 같은 자리에 같은 방식으로 붙어 있다.
        /// </para>
        /// </summary>
        public ArenaFoe.Stats? ArenaDuelFoe;
        /// <summary>이 판이 아레나 1대1 인가 — 엔진이 갈래를 볼 때 쓰는 한 줄.</summary>
        public bool IsArenaDuel => ArenaDuelFoe.HasValue;

        /// <summary>
        /// T293 — <b>장착 펫의 발동 한 줄</b>(«회피 시 33% 로 도끼 2개»). 표에서 나온 것을 엔진이 읽는 꼴로만 옮긴 것이라 수가 없다.
        /// </summary>
        public struct PetProc
        {
            /// <summary>어느 자리에서 굴리나 — <see cref="PetKey.Evade"/>·<see cref="PetKey.Attack"/>·<see cref="PetKey.Hit"/>.</summary>
            public string Trigger;
            /// <summary>무엇을 쏘나 — <see cref="PetKey.ShotAxe"/>·<see cref="PetKey.ShotBolt"/>.</summary>
            public string Shot;
            /// <summary>몇 발인가(1·2).</summary>
            public int Count;
            /// <summary>발동 확률(%) — 펫마다 <b>따로</b> 굴린다.</summary>
            public double Chance;
        }

        /// <summary>
        /// T293 — 이 판에 데리고 들어간 <b>장착 펫</b>들(<see cref="Pets.Procs"/> 가 만든다). <c>null</c>·빈 목록이면 엔진이 <b>굴림 자체를 안 한다</b>.
        /// <para>
        /// ⚠ <b>기본값이 <c>null</c> 인 것이 시드 골든(T2)의 안전장치다</b> — 위 <see cref="ArenaDuelFoe"/> 와 같은 자리·같은 까닭이다.
        /// 펫을 안 낀 판은 난수 열이 한 톨도 안 달라진다(지시서 §2 T293 3항 «시뮬 동일성»).
        /// </para>
        /// </summary>
        public List<PetProc> Pets;

        /// <summary>
        /// 장착 펫이 더해 주는 공·체·실(T293 ⓖ · 주인 «장착 효과 있음 — 공·체·실 채워 줌» · 값은 <see cref="KkomaKnight.Core.Pets.EquipPower"/>).
        /// <para>
        /// ⚑ <b>기본값이 0 이라 지금까지의 판은 한 톨도 안 달라진다</b> — 시드 골든(T2)이 걸리는 자리라
        /// <see cref="Pets"/>(발동 목록)와 <b>같은 안전장치</b>를 쓴다: 펫이 없으면 더하는 것도 0 이다.
        /// </para>
        /// <para>
        /// ⚑ <b>왜 <see cref="GearSystem.BuildPower"/> 를 안 고쳤나</b> — 그 함수는 «장비만으로 나오는 힘» 이고
        /// 시뮬(<c>tools/sim</c>)·재적합 자·골든이 전부 그 뜻으로 부른다. 거기에 펫을 섞으면 <b>장비를 재는 모든 자리가 같이 흔들린다.</b>
        /// 펫은 «이 판에 들고 들어가는 것» 이므로 판의 옵션으로 들려 보낸다(ⓑ 의 발동 목록과 같은 결).
        /// </para>
        /// </summary>
        public Power PetPower;
    }

    public struct RunResult
    {
        public bool Clear; public double Time; public double Gold; public List<string> Taken; public int Level, AtkTries, Miss, Kills;
    }
}
