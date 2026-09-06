using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// StreamingAssets/data/*.json (= aaaw 레포 data/ 의 복사본) 을 타입 있게 올린 것.
    /// **수치는 전부 여기서 읽는다** — 엔진·UI 코드에 숫자를 직접 박지 않는다 (주인 지시).
    /// 로드는 순수 C# 이라 EditMode 테스트·dotnet 하니스에서도 같은 코드로 돈다.
    /// </summary>
    public sealed class GameData
    {
        public TuneData Tune;
        public EnemiesData Enemies;
        public PerksData Perks;
        public GearData Gear;
        public GachaData Gacha;
        public CombatData Combat;
        public UiData Ui;
        /// <summary>상점 상품표(다이아 6 · 골드 3) — aaaw data/ 가 아니라 이 레포의 <c>Assets/KkomaKnight/shop.json</c>(카탈로그 텍스트 «data.shop»). 로드는 Bootstrap 이 따로 한다 · 없으면 null(상점이 상품 없이 뜬다).</summary>
        public ShopData Shop;
        /// <summary>데일리 기프트 수치표(무료 칸 + 광고 누적 줄 4) — aaaw data/ 가 아니라 이 레포의 <c>Assets/KkomaKnight/dailyGift.json</c>(카탈로그 텍스트 «data.dailyGift» · T77). 로드는 Bootstrap 이 따로 한다 · 없으면 null(데일리 기프트 팝업이 줄 없이 뜬다).</summary>
        public DailyGiftData DailyGift;
        /// <summary>아레나 껍데기의 상대 승점·전투력 표시용 계수 — aaaw data/ 가 아니라 이 레포의 <c>Assets/KkomaKnight/arenaDummy.json</c>(카탈로그 텍스트 «data.arenaDummy» · T81). 전투 엔진은 안 쓴다 · 없으면 null(숫자가 «—» 로 남는다).</summary>
        public ArenaDummyData ArenaDummy;
        /// <summary>탐험(방치·오프라인 보상) 수치표 — aaaw data/ 가 아니라 이 레포의 <c>Assets/KkomaKnight/expedition.json</c>(카탈로그 텍스트 «data.expedition» · T97). 로드는 Bootstrap 이 따로 한다 · 없으면 null(탐험 팝업이 «--» 로 뜬다).</summary>
        public ExpeditionData Expedition;

        /// <summary>던전 티켓·보상 수치표 — aaaw data/ 가 아니라 이 레포의 <c>Assets/KkomaKnight/dungeon.json</c>(카탈로그 텍스트 «data.dungeon» · T99). 로드는 Bootstrap 이 따로 한다 · 없으면 null(던전 티켓이 «--» 로 뜨고 보충·구매가 없다).</summary>
        public DungeonData Dungeon;

        public static readonly string[] Files = { "tune.json", "enemies.json", "perks.json", "gear.json", "gacha.json", "combat.json", "ui.json" };

        /// <param name="read">파일명(예: "tune.json") → 본문 텍스트. 플랫폼별 읽기(StreamingAssets·File) 는 호출부가 준다.</param>
        public static GameData Load(Func<string, string> read)
        {
            var d = new GameData();
            d.Tune = TuneData.From(new JNode(MiniJson.Parse(read("tune.json"))));
            d.Enemies = EnemiesData.From(new JNode(MiniJson.Parse(read("enemies.json"))));
            d.Perks = PerksData.From(new JNode(MiniJson.Parse(read("perks.json"))));
            d.Gear = GearData.From(new JNode(MiniJson.Parse(read("gear.json"))));
            d.Gacha = GachaData.From(new JNode(MiniJson.Parse(read("gacha.json"))));
            d.Combat = CombatData.From(new JNode(MiniJson.Parse(read("combat.json"))));
            d.Ui = UiData.From(new JNode(MiniJson.Parse(read("ui.json"))));
            d.Validate();
            return d;
        }

        /// <summary>디스크 폴더에서 로드 (dotnet 하니스·EditMode 테스트용).</summary>
        public static GameData LoadFromDirectory(string dir)
            => Load(f => System.IO.File.ReadAllText(System.IO.Path.Combine(dir, f)));

        void Validate()
        {
            if (Enemies.Chapters.Count != Tune.MaxChapter)
                throw new FormatException($"enemies.json 챕터 수 {Enemies.Chapters.Count} ≠ tune.maxChapter {Tune.MaxChapter}");
            if (Perks.Perks.Count != Perks.Count)
                throw new FormatException("perks.json count 불일치");
            foreach (var pt in Gear.Parts)
                if (!Gear.Types.ContainsKey(pt)) throw new FormatException("gear.json types 에 부위 없음: " + pt);
            foreach (var kv in Gear.Types)
                foreach (var ty in kv.Value)
                    if (!Gear.Options.ContainsKey(ty)) throw new FormatException("gear.json optionLadder 에 종류 없음: " + ty);
        }
    }

    // ───────────────────────── tune.json ─────────────────────────
    public sealed class TuneData
    {
        public string Source;
        public double EBaseHp, EBaseDmg;
        public double[][] EHpSeg, EDmgSeg;      // [ [fromChapter, rate], ... ]
        public double WallHp, WallDmg, Wall2Hp, Wall2Dmg, WaveHp, WaveDmg, Wall3Hp, Wall3Dmg, Wall4Hp, Wall4Dmg;
        public int Wall4At, MaxChapter;
        public double PAtk0, PHp0, PSh0, PAspd0, PCrit0, PCritF0, PCounter0, PDef0, PEvade0;
        public double GoldKillBase, GoldKillPer, GoldClearPer, GoldGrowth;
        public int ExpKill, ExpBoss;
        /// <summary>레벨 → 그 레벨에서 다음 레벨까지 필요 경험치 (표 순서 = 레벨 1..N).</summary>
        public int[] ExpNeedTable;

        public static TuneData From(JNode j)
        {
            var t = j.Req("tune");
            var d = new TuneData
            {
                Source = j["_source"].Str(),
                EBaseHp = t.Req("eBaseHp").Num(), EBaseDmg = t.Req("eBaseDmg").Num(),
                EHpSeg = Seg(t.Req("eHpSeg")), EDmgSeg = Seg(t.Req("eDmgSeg")),
                WallHp = t["wallHp"].Num(), WallDmg = t["wallDmg"].Num(),
                Wall2Hp = t["wall2Hp"].Num(), Wall2Dmg = t["wall2Dmg"].Num(),
                WaveHp = t["waveHp"].Num(), WaveDmg = t["waveDmg"].Num(),
                Wall3Hp = t["wall3Hp"].Num(), Wall3Dmg = t["wall3Dmg"].Num(),
                Wall4Hp = t["wall4Hp"].Num(), Wall4Dmg = t["wall4Dmg"].Num(),
                Wall4At = t["wall4At"].Int(), MaxChapter = t.Req("maxChapter").Int(),
                PAtk0 = t["pAtk0"].Num(), PHp0 = t["pHp0"].Num(), PSh0 = t["pSh0"].Num(), PAspd0 = t["pAspd0"].Num(),
                PCrit0 = t["pCrit0"].Num(), PCritF0 = t["pCritF0"].Num(), PCounter0 = t["pCounter0"].Num(),
                PDef0 = t["pDef0"].Num(), PEvade0 = t["pEvade0"].Num(),
                GoldKillBase = t["goldKillBase"].Num(), GoldKillPer = t["goldKillPer"].Num(),
                GoldClearPer = t["goldClearPer"].Num(), GoldGrowth = t["goldGrowth"].Num(),
                ExpKill = t["expKill"].Int(), ExpBoss = t["expBoss"].Int(),
            };
            var tbl = new List<int>();
            foreach (var row in j.Req("expNeedTable").Items()) tbl.Add(row["need"].Int());
            d.ExpNeedTable = tbl.ToArray();
            if (d.ExpNeedTable.Length < 2) throw new FormatException("tune.json expNeedTable 이 비었다");
            return d;
        }

        static double[][] Seg(JNode a)
        {
            var list = new List<double[]>();
            foreach (var row in a.Items()) list.Add(row.NumArray());
            return list.ToArray();
        }

        /// <summary>레벨 lv 에서 다음 레벨까지 필요 경험치. 표 밖은 표의 마지막 두 칸 차이로 선형 연장(표 = 정본).</summary>
        public int ExpNeed(int lv)
        {
            if (lv < 1) lv = 1;
            var tb = ExpNeedTable;
            if (lv <= tb.Length) return tb[lv - 1];
            int step = tb[tb.Length - 1] - tb[tb.Length - 2];
            return tb[tb.Length - 1] + step * (lv - tb.Length);
        }

        /// <summary>sim.js goldKill(c) 의 «rand(1,1.8)» 을 뺀 결정부.</summary>
        public double GoldKillBaseAt(int c) => (GoldKillBase + GoldKillPer * c) * Math.Pow(GoldGrowth, c - 1);
        public double GoldClear(int c) => GoldClearPer * c * Math.Pow(GoldGrowth, c - 1);
    }

    // ───────────────────────── enemies.json ─────────────────────────
    public sealed class EnemiesData
    {
        public int Waves, Rests, MaxEnemy;
        public int CurveEarly, CurveFrom, CurveCap;
        public int RangedZeroUntil, RangedJitter; public double RangedRate;
        public double NodeGap, NodeGapEvent, EnemyGap;
        public double BossHpMul, BossDmgMul, BossSizeMul, BossTripleHitMul, BossStunMul; public int BossTripleHitEvery;
        public List<ChapterData> Chapters = new List<ChapterData>();

        public ChapterData Chapter(int c) => Chapters[c - 1];

        public static EnemiesData From(JNode j)
        {
            var L = j.Req("layout"); var B = j.Req("boss");
            var d = new EnemiesData
            {
                Waves = L["waves"].Int(), Rests = L["rests"].Int(), MaxEnemy = L["maxEnemy"].Int(),
                CurveEarly = L["enemyCurve"]["early"].Int(), CurveFrom = L["enemyCurve"]["from"].Int(), CurveCap = L["enemyCurve"]["cap"].Int(),
                RangedZeroUntil = L["rangedCurve"]["zeroUntil"].Int(), RangedRate = L["rangedCurve"]["rate"].Num(), RangedJitter = L["rangedCurve"]["jitter"].Int(),
                NodeGap = L.Req("nodeGap").Num(), NodeGapEvent = L.Req("nodeGapEvent").Num(), EnemyGap = L.Req("enemyGap").Num(),
                BossHpMul = B["hpMul"].Num(), BossDmgMul = B["dmgMul"].Num(), BossSizeMul = B["sizeMul"].Num(),
                BossTripleHitMul = B["tripleHitMul"].Num(), BossTripleHitEvery = B["tripleHitEvery"].Int(), BossStunMul = B["stunMul"].Num(),
            };
            foreach (var c in j.Req("chapters").Items())
            {
                var ch = new ChapterData { C = c["c"].Int(), EnemyCount = c["enemyCount"].Int(), WaveSizes = c["waveSizes"].IntArray(), RangedCount = c["rangedCount"].Int() };
                foreach (var n in c["nodes"].Items())
                {
                    var t = n["t"].Str();
                    ch.Nodes.Add(new NodeData { Type = ParseNode(t), Size = n["size"].Int(), Ranged = n["ranged"].BoolArray() });
                }
                foreach (var w in c["waves"].Items())
                    ch.Waves.Add(new WaveStat { W = w["w"].Int(), Size = w["size"].Int(), Hp = w["hp"].Num(), Dmg = w["dmg"].Num() });
                var b = c["boss"];
                ch.Boss = new WaveStat { W = b["w"].Int(), Size = 1, Hp = b["hp"].Num(), Dmg = b["dmg"].Num() };
                d.Chapters.Add(ch);
            }
            return d;
        }

        static NodeType ParseNode(string t)
        {
            switch (t)
            {
                case "wave": return NodeType.Wave;
                case "rest": return NodeType.Rest;
                case "devil": return NodeType.Devil;
                case "angel": return NodeType.Angel;
                case "boss": return NodeType.Boss;
            }
            throw new FormatException("enemies.json: 알 수 없는 노드 " + t);
        }
    }

    public enum NodeType { Wave, Rest, Devil, Angel, Boss }

    public sealed class ChapterData
    {
        public int C, EnemyCount, RangedCount;
        public int[] WaveSizes;
        public List<NodeData> Nodes = new List<NodeData>();
        public List<WaveStat> Waves = new List<WaveStat>();
        public WaveStat Boss;
    }
    public sealed class NodeData { public NodeType Type; public int Size; public bool[] Ranged; }
    public sealed class WaveStat { public int W, Size; public double Hp, Dmg; }

    // ───────────────────────── perks.json ─────────────────────────
    public sealed class PerksData
    {
        public int Count;
        public double[] GradeRate; public string[] GradeName;
        public int OfferPerLevel, PicksPerRun, DevilGrade, DevilOffer;
        public double DevilCostMaxHp;
        public Dictionary<string, double> Consts = new Dictionary<string, double>();
        public Dictionary<string, double[]> ConstArrays = new Dictionary<string, double[]>();
        public List<PerkDef> Perks = new List<PerkDef>();
        /// <summary>[px 키, 주기 N] — «N타마다» 특전표(순서 = 엔진 순서).</summary>
        public List<KeyValuePair<string, int>> NHitPerks = new List<KeyValuePair<string, int>>();
        readonly Dictionary<string, PerkDef> _byId = new Dictionary<string, PerkDef>();

        public PerkDef ById(string id) => _byId.TryGetValue(id, out var p) ? p : null;
        public double C(string name)
        {
            if (Consts.TryGetValue(name, out var v)) return v;
            throw new KeyNotFoundException("perks.json constants 에 없음: " + name);
        }

        public static PerksData From(JNode j)
        {
            var d = new PerksData { Count = j["count"].Int() };
            var r = j.Req("rules");
            d.GradeRate = r["gradeRate"].NumArray(); d.GradeName = r["gradeName"].StrArray();
            d.OfferPerLevel = r["offerPerLevel"].Int(); d.PicksPerRun = r["picksPerRun"].Int();
            d.DevilGrade = r["devilGrade"].Int(); d.DevilOffer = r["devilOffer"].Int(); d.DevilCostMaxHp = r["devilCostMaxHp"].Num();
            var c = j.Req("constants");
            foreach (var k in c.Keys)
            {
                var v = c[k];
                if (v.Raw is double) d.Consts[k] = v.Num();
                else if (v.IsArray && v.Count > 0 && v[0].Raw is double) d.ConstArrays[k] = v.NumArray();
            }
            foreach (var n in j["nHitPerks"].Items()) d.NHitPerks.Add(new KeyValuePair<string, int>(n[0].Str(), n[1].Int()));
            foreach (var p in j.Req("perks").Items())
            {
                var pd = new PerkDef { Order = p["order"].Int(), Id = p["id"].Str(), Name = p["name"].Str(), Desc = p["desc"].Str(), Grade = p["grade"].Int(), GradeName = p["gradeName"].Str() };
                var eff = p["effect"];
                foreach (var k in eff["px"].Keys) pd.Px[k] = eff["px"][k].Num();
                foreach (var k in eff["stat"].Keys) pd.Stat[k] = new StatDelta { From = eff["stat"][k]["from"].Num(), To = eff["stat"][k]["to"].Num() };
                d.Perks.Add(pd); d._byId[pd.Id] = pd;
            }
            return d;
        }
    }
    public struct StatDelta { public double From, To; }
    public sealed class PerkDef
    {
        public int Order, Grade; public string Id, Name, Desc, GradeName;
        /// <summary>실측 px 플래그(탐침) — 엔진은 이 값을 «그대로» 더/최댓값 갱신한다.</summary>
        public Dictionary<string, double> Px = new Dictionary<string, double>();
        public Dictionary<string, StatDelta> Stat = new Dictionary<string, StatDelta>();
        public override string ToString() => Id;
    }

    // ───────────────────────── gear.json ─────────────────────────
    public sealed class GearData
    {
        public string[] Parts; public Dictionary<string, string> PartName = new Dictionary<string, string>();
        public string[] Sets; public Dictionary<string, string> SetName = new Dictionary<string, string>();
        public Dictionary<string, string[]> Types = new Dictionary<string, string[]>();
        public Dictionary<string, string> TypeName = new Dictionary<string, string>();
        public string[] RarName; public int RarLegend, RarMyth;
        public double[] Atk, Hp, Sh;
        public double PlusStep; public int LegendToMythPlus, LegendMaxPlus;
        public double SlotStep; public int SlotLvMax; public double SlotCostBase, SlotCostG; public double[] SlotCostTable;
        public double EvenStep; public int EvenPer;
        public int OptMaxCount; public int[] OptCountByRar; public int[] MythPlusOptAt;   // 신화 +3/+6/+9
        public Dictionary<string, List<GearOption>> Options = new Dictionary<string, List<GearOption>>();
        public Dictionary<string, double> SummonRatio = new Dictionary<string, double>();
        public List<GearType> AllTypes = new List<GearType>();

        public static GearData From(JNode j)
        {
            var d = new GearData { Parts = j.Req("parts").StrArray(), Sets = j.Req("sets").StrArray(), RarName = j.Req("rarName").StrArray(), RarLegend = j["rarLegend"].Int(), RarMyth = j["rarMyth"].Int() };
            foreach (var k in j["partName"].Keys) d.PartName[k] = j["partName"][k].Str();
            foreach (var k in j["setName"].Keys) d.SetName[k] = j["setName"][k].Str();
            foreach (var k in j["types"].Keys) d.Types[k] = j["types"][k].StrArray();
            foreach (var k in j["typeName"].Keys) d.TypeName[k] = j["typeName"][k].Str();
            var c = j.Req("contribution"); d.Atk = c["atk"].NumArray(); d.Hp = c["hp"].NumArray(); d.Sh = c["sh"].NumArray();
            var e = j.Req("enhance"); d.PlusStep = e.Req("plusStep").Num(); d.LegendToMythPlus = e["legendToMythPlus"].Int(); d.LegendMaxPlus = e["legendMaxPlus"].Int();
            var s = j.Req("slot"); d.SlotStep = s["step"].Num(); d.SlotLvMax = s["lvMax"].Int(); d.SlotCostBase = s["costBase"].Num(); d.SlotCostG = s["costG"].Num();
            d.SlotCostTable = s["costTable"].NumArray(); d.EvenStep = s["evenStep"].Num(); d.EvenPer = s["evenPer"].Int();
            var ol = j.Req("optionLadder"); d.OptMaxCount = ol["maxCount"].Int();
            var oc = new List<int>(); var mythAt = new List<int>();
            foreach (var row in ol["optCount"].Items())
            {
                oc.Add(row["plus0"].Int());
                if (row["rar"].Int() == d.RarMyth)
                    foreach (var k in row.Keys) if (k.StartsWith("plus") && k != "plus0") mythAt.Add(int.Parse(k.Substring(4)));
            }
            d.OptCountByRar = oc.ToArray(); mythAt.Sort(); d.MythPlusOptAt = mythAt.ToArray();
            ShiftOptionLadderOneStep(d);
            var opts = ol.Req("options");
            foreach (var ty in opts.Keys)
            {
                var list = new List<GearOption>();
                foreach (var o in opts[ty].Items())
                {
                    var go = new GearOption { Slot = o["slot"].Int(), Desc = o["desc"].Str() };
                    var eff = o["effect"];
                    foreach (var k in eff["px"].Keys) go.Px[k] = eff["px"][k].Num();
                    foreach (var k in eff["stat"].Keys) go.Stat[k] = eff["stat"][k]["to"].Num() - eff["stat"][k]["from"].Num();
                    list.Add(go);
                }
                d.Options[ty] = list;
            }
            foreach (var k in j["summonRatio"].Keys) d.SummonRatio[k] = j["summonRatio"][k].Num();
            foreach (var pt in d.Parts) foreach (var ty in d.Types[pt]) d.AllTypes.Add(new GearType { Part = pt, Type = ty });
            return d;
        }

        /// <summary>
        /// ⚑ 주인 지시 2026-09-07 «일반 등급에서는 옵션 안 열리게 · 희귀에서부터 · 마지막(흡혈 +8%)은 신화 12강» —
        /// **aaaw 원본(`data/gear.json`)과 의도적으로 다르다**(정본 JSON 은 손대지 않고 여기서만 한 칸씩 뒤로 민다).
        /// 등급 기본 개수를 하나씩 줄이고(일반 1→0 · 희귀 2→1 · 전설 3→2 · 신화 4→3)
        /// 신화 강화 단계에 한 칸(+12)을 더해(+3/+6/+9 → +3/+6/+9/+12) 줄 수(<see cref="OptMaxCount"/> = 7)는 그대로 둔다.
        /// 마지막 칸 간격은 표에서 읽어 더한다(코드에 수치를 박지 않는다 · §1).
        /// 세트 옵션은 <see cref="RunOptions.GearOpts"/> 가 켜진 판에만 들어가고 T2 시드 골든은 그 스위치가 꺼져 있어 흔들리지 않는다.
        /// </summary>
        static void ShiftOptionLadderOneStep(GearData d)
        {
            if (d.OptCountByRar == null || d.OptCountByRar.Length == 0) return;
            if (d.OptCountByRar[0] <= 0) return;                       // 원본이 이미 «일반 0» 이면 그대로 둔다(두 번 밀지 않는다)
            for (int r = 0; r < d.OptCountByRar.Length; r++) d.OptCountByRar[r] = Math.Max(0, d.OptCountByRar[r] - 1);
            var at = new List<int>(d.MythPlusOptAt);
            if (at.Count > 0)
            {
                int step = at.Count >= 2 ? at[at.Count - 1] - at[at.Count - 2] : at[0];
                at.Add(at[at.Count - 1] + step);
            }
            d.MythPlusOptAt = at.ToArray();
        }

        public string SetOf(string type) => type.Substring(0, type.IndexOf('_'));
        public double SlotMul(int L) => 1 + SlotStep * Math.Min(L, SlotLvMax);
        public double SlotCost(int L) => L < SlotCostTable.Length ? SlotCostTable[L] : Math.Floor(SlotCostBase * Math.Pow(SlotCostG, L));
        /// <summary>옵션 개수 — 등급별 + 신화 강화 보너스 (optionLadder.optCount 그대로).</summary>
        public int OptCount(int rar, int plus)
        {
            int n = OptCountByRar[rar];
            if (rar == RarMyth) foreach (var at in MythPlusOptAt) if (plus >= at) n++;
            return n;
        }
        /// <summary>옵션 줄 i 가 열리는 등급 index — 신화 강화에서야 열리는 줄이면 <see cref="RarMyth"/>.</summary>
        public int OptTierRar(int i)
        {
            for (int r = 0; r < OptCountByRar.Length; r++) if (OptCountByRar[r] > i) return r;
            return RarMyth;
        }
        /// <summary>옵션 줄 i 가 신화 «강화» 단계에서야 열리는 줄인가(= 등급만으로는 안 열린다).</summary>
        public bool OptNeedsMythPlus(int i) => i >= OptCountByRar[RarMyth];
        /// <summary>
        /// 옵션 줄 i 의 개방 단계 이름 — «희귀» 같은 등급 이름이거나 «신화 +12강».
        /// 강화 칸은 <see cref="MythPlusOptAt"/> 표에서 읽는다(예전처럼 «(i−등급수+1)×3» 으로 계산하지 않는다 · T89).
        /// </summary>
        public string OptTierName(int i)
        {
            if (!OptNeedsMythPlus(i)) return RarName[OptTierRar(i)];
            int k = i - OptCountByRar[RarMyth];
            return k >= 0 && k < MythPlusOptAt.Length ? RarName[RarMyth] + " +" + MythPlusOptAt[k] + "강" : "";
        }
    }
    public sealed class GearOption
    {
        public int Slot; public string Desc;
        public Dictionary<string, double> Px = new Dictionary<string, double>();     // 누산 (g_* 축)
        public Dictionary<string, double> Stat = new Dictionary<string, double>();   // 가산 델타 (critR/critF/counter/def/evade/steal)
    }
    public struct GearType { public string Part, Type; }

    // ───────────────────────── gacha.json ─────────────────────────
    public sealed class GachaData
    {
        public List<GachaBox> Boxes = new List<GachaBox>();
        public int TenPullCount; public double TenPullDiscount;
        public double PullCost, DailyGem, IapGem; public int RunsPerDay;
        public GachaBox Box(string key) { foreach (var b in Boxes) if (b.Key == key) return b; throw new KeyNotFoundException("gacha box " + key); }

        public static GachaData From(JNode j)
        {
            var d = new GachaData();
            var bx = j.Req("boxes");
            foreach (var k in bx.Keys)
            {
                var b = bx[k];
                d.Boxes.Add(new GachaBox { Key = b["key"].Str(k), Name = b["name"].Str(), Cost = b["cost"].Num(), Rate = b["rate"].NumArray(), Cum = b["cum"].NumArray(), PityMyth = b["pityMyth"].Int(), PityLegend = b["pityLegend"].Int() });
            }
            d.TenPullCount = j["tenPull"]["count"].Int(10); d.TenPullDiscount = j["tenPull"]["discount"].Num();
            var e = j["economy"]; d.PullCost = e["pullCost"].Num(); d.DailyGem = e["dailyGem"].Num(); d.IapGem = e["iapGem"].Num(); d.RunsPerDay = e["runsPerDay"].Int();
            return d;
        }
    }
    public sealed class GachaBox
    {
        public string Key, Name; public double Cost; public double[] Rate, Cum; public int PityMyth, PityLegend;
        /// <summary>sim.js `rarRoll(r)` — r 은 [0,100). 높은 등급부터 누적 임계와 비교.</summary>
        public int RarRoll(double r) { for (int i = Rate.Length - 1; i > 0; i--) if (r < Cum[i]) return i; return 0; }
    }

    // ───────────────────────── combat.json ─────────────────────────
    public sealed class CombatData
    {
        public double PlayerSpeed, StopDistance, DashMul;
        public double MeleeEnemy, RangedEnemyMax, RangedEnemyMin, EnemyGap, NodeGap, NodeGapEvent, SpearReach, WaveReach, WaveReachKing;
        public int PierceWave, PierceWaveBig, PierceSpear;
        public double AxeSpeed, SpearSpeed, EnemyArrowSpeed; public int ProjCap, ProcTickCap;
        public double MeleeInterval, BossInterval, RangedInterval, SlowMul, BossTripleHitMul, EnemyEvade; public int BossTripleHitEvery;
        public double StunBossMul; public double[] StunDurations;
        public double DefCap, EvadeCap;
        public double RestHeal, DevilCostMaxHp; public int RestExp;
        public double RAxe, RArrow, RWave, RBolt, RSpear;

        public static CombatData From(JNode j)
        {
            var m = j.Req("move"); var r = j.Req("range"); var p = j.Req("pierce"); var pj = j.Req("projectile");
            var ea = j.Req("enemyAttack"); var st = j.Req("stun"); var cp = j.Req("caps"); var ev = j.Req("events"); var sr = j.Req("summonRatio");
            return new CombatData
            {
                PlayerSpeed = m.Req("playerSpeed").Num(), StopDistance = m.Req("stopDistance").Num(), DashMul = m["dashMul"].Num(1),
                MeleeEnemy = r.Req("meleeEnemy").Num(), RangedEnemyMax = r.Req("rangedEnemyMax").Num(), RangedEnemyMin = r.Req("rangedEnemyMin").Num(),
                EnemyGap = r.Req("enemyGap").Num(), NodeGap = r.Req("nodeGap").Num(), NodeGapEvent = r.Req("nodeGapEvent").Num(),
                SpearReach = r.Req("spearReach").Num(), WaveReach = r.Req("waveReach").Num(), WaveReachKing = r["waveReachKing"].Num(),
                PierceWave = p["wave"].Int(), PierceWaveBig = p["waveBig"].Int(), PierceSpear = p.Req("spear").Int(),
                AxeSpeed = pj.Req("axeSpeed").Num(), SpearSpeed = pj.Req("spearSpeed").Num(), EnemyArrowSpeed = pj.Req("enemyArrowSpeed").Num(),
                ProjCap = pj.Req("cap").Int(), ProcTickCap = pj.Req("procTickCap").Int(),
                MeleeInterval = ea.Req("meleeInterval").Num(), BossInterval = ea.Req("bossInterval").Num(), RangedInterval = ea.Req("rangedInterval").Num(),
                SlowMul = ea["slowMul"].Num(1), BossTripleHitEvery = ea.Req("bossTripleHitEvery").Int(), BossTripleHitMul = ea.Req("bossTripleHitMul").Num(), EnemyEvade = ea.Req("evade").Num(),
                StunBossMul = st.Req("boss").Num(), StunDurations = st["durations"].NumArray(),
                DefCap = cp.Req("def").Num(), EvadeCap = cp.Req("evade").Num(),
                RestHeal = ev.Req("restHeal").Num(), RestExp = ev.Req("restExp").Int(), DevilCostMaxHp = ev.Req("devilCostMaxHp").Num(),
                RAxe = sr.Req("axe").Num(), RArrow = sr.Req("arrow").Num(), RWave = sr.Req("wave").Num(), RBolt = sr.Req("bolt").Num(), RSpear = sr.Req("spear").Num(),
            };
        }
    }

    // ───────────────────────── ui.json ─────────────────────────
    public sealed class UiData
    {
        public double CameraZoom, PlayerX, FootBarW, EnemyBarW, BossBarW, AxeArc, PopShieldDx, PopShieldDy;
        public string PopShield, PopHp;
        public int DesignWidth, DesignHeight, MinWidth;
        public static UiData From(JNode j)
        {
            return new UiData
            {
                CameraZoom = j["camera"]["zoom"].Num(1), PlayerX = j["camera"]["playerX"].Num(0.16),
                FootBarW = j["bars"]["footBarW"].Num(1), EnemyBarW = j["bars"]["enemyBarW"].Num(), BossBarW = j["bars"]["bossBarW"].Num(),
                AxeArc = j["fx"]["axeArc"].Num(), PopShield = j["fx"]["popShield"].Str("#6CC0F0"), PopHp = j["fx"]["popHp"].Str("#FF8A80"),
                PopShieldDx = j["fx"]["popShieldDx"].Num(), PopShieldDy = j["fx"]["popShieldDy"].Num(),
                DesignWidth = j["frame"]["designWidth"].Int(390), DesignHeight = j["frame"]["designHeight"].Int(844), MinWidth = j["frame"]["minWidth"].Int(360),
            };
        }
    }

    /// <summary>
    /// 상점 상품표 (<c>Assets/KkomaKnight/shop.json</c> · T9 · 승인 대기 25 의 기본값을 주인이 확정).
    /// 다이아 상품 = 원화 모의 결제(누르면 바로 지급) · 골드 상품 = 다이아 소모. 수치는 파일에서만 온다 — 코드 상수 없음.
    /// </summary>
    public sealed class ShopData
    {
        public sealed class GemPack { public int Won; public double Gem; }
        public sealed class GoldPack { public double Gold; public double Gem; }
        public List<GemPack> GemPacks = new List<GemPack>();
        public List<GoldPack> GoldPacks = new List<GoldPack>();

        public static ShopData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static ShopData From(JNode j)
        {
            var d = new ShopData();
            foreach (var p in j["gemPacks"].Items()) d.GemPacks.Add(new GemPack { Won = (int)p["won"].ReqNum("gemPacks.won"), Gem = p["gem"].ReqNum("gemPacks.gem") });
            foreach (var p in j["goldPacks"].Items()) d.GoldPacks.Add(new GoldPack { Gold = p["gold"].ReqNum("goldPacks.gold"), Gem = p["gem"].ReqNum("goldPacks.gem") });
            if (d.GemPacks.Count == 0 && d.GoldPacks.Count == 0) throw new FormatException("shop.json: gemPacks/goldPacks 가 비어 있다");
            return d;
        }
    }
}
