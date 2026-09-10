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
        /// <summary>챕터 보상(Chapter Chest) 수치표 — 이 레포의 <c>Assets/KkomaKnight/chapterChest.json</c>(카탈로그 텍스트 «data.chapterChest» · T98). 로드는 Bootstrap 이 따로 한다 · 없으면 null(페이지가 «--» 로 뜬다).</summary>
        public ChapterChestData ChapterChest;

        /// <summary>던전 티켓·보상 수치표 — aaaw data/ 가 아니라 이 레포의 <c>Assets/KkomaKnight/dungeon.json</c>(카탈로그 텍스트 «data.dungeon» · T99). 로드는 Bootstrap 이 따로 한다 · 없으면 null(던전 티켓이 «--» 로 뜨고 보충·구매가 없다).</summary>
        public DungeonData Dungeon;

        /// <summary>상인(아레나 상점 · 26) 상품표 — aaaw data/ 가 아니라 이 레포의 <c>Assets/KkomaKnight/arenaShop.json</c>(카탈로그 텍스트 «data.arenaShop» · T209). 로드는 Bootstrap 이 따로 한다 · 없으면 null(카드가 종전처럼 «한도 —»·«—» 로 뜬다).</summary>
        public ArenaShopData ArenaShop;

        /// <summary>PvP 순위 보상 구간표 — 이 레포의 <c>Assets/KkomaKnight/arena.json</c>(카탈로그 텍스트 «data.arenaRank» · T237). 로드는 Bootstrap 이 따로 한다 · 없으면 null(25 팝업이 종전 네 줄 껍데기 그대로 뜬다).</summary>
        public ArenaRankData ArenaRank;
        /// <summary>출석(16) 7일 보상표 — 이 레포의 <c>Assets/KkomaKnight/attendance.json</c>(카탈로그 텍스트 «data.attendance» · T253). 로드는 Bootstrap 이 따로 한다 · 없으면 null(출석 팝업이 종전 껍데기 그대로 뜬다).</summary>
        public AttendanceData Attendance;
        /// <summary>특권(11) 카드표 — 이 레포의 <c>Assets/KkomaKnight/privilege.json</c>(카탈로그 텍스트 «data.privilege» · T264). 로드는 Bootstrap 이 따로 한다 · 없으면 null(특권 페이지가 종전 껍데기 그대로 뜬다).</summary>
        public PrivilegeData Privilege;
        /// <summary>퀘스트 표 — 이 레포의 <c>Assets/KkomaKnight/quest.json</c>(카탈로그 텍스트 «data.quest» · T257). 로드는 Bootstrap 이 따로 한다 · 없으면 null(15 팝업이 종전 껍데기 그대로 뜬다).</summary>
        public QuestData Quest;
        /// <summary>아레나 한 판의 규칙표(승점·티어) — 이 레포의 <c>Assets/KkomaKnight/arenaMatch.json</c>(카탈로그 텍스트 «data.arenaMatch» · T240). 로드는 Bootstrap 이 따로 한다 · 없으면 null(승점이 안 움직인다).</summary>
        public ArenaMatchData ArenaMatch;
        /// <summary>
        /// 아레나 <b>상대 스탯</b> 규칙(전투력 → 체력·공격) — <b>같은 파일</b>(<c>arenaMatch.json</c> 의 <c>foe</c> 칸 · T240 3항)에서 읽는다.
        /// 없으면 <c>null</c>(1대1 판이 안 열리고 종전대로 챕터 전투가 열린다 — 아무 수나 지어내지 않는다).
        /// </summary>
        public ArenaFoeData ArenaFoe;
        /// <summary>업적(반복 퀘스트) 표 — 이 레포의 <c>Assets/KkomaKnight/achievement.json</c>(카탈로그 텍스트 «data.achievement» · T258). 로드는 Bootstrap 이 따로 한다 · 없으면 null(업적 탭이 비어 뜨고 받기가 없다).</summary>
        public AchievementData Achievement;
        /// <summary>장비 레시피 표 — 이 레포의 <c>Assets/KkomaKnight/recipe.json</c>(카탈로그 텍스트 «data.recipe» · T290). 로드는 Bootstrap 이 따로 한다 · 없으면 null(레시피가 안 드는 옛 그대로 = 골드만).</summary>
        public RecipeData Recipe;
        /// <summary>
        /// 펫 표 — 이 레포의 <c>Assets/KkomaKnight/pet.json</c>(카탈로그 텍스트 «data.pet» · T293). 로드는 Bootstrap 이 따로 한다.
        /// <para>
        /// <b>없으면 null</b> 이고, 그러면 펫이 <b>통째로 없던 옛 그대로</b>다 — 화면 13 이 «표가 아직 없다» 로 뜨고, 장착 합도 발동도 0 이다.
        /// 규칙 쪽은 이미 그렇게 서 있다(<see cref="Pets.Equipped"/>·<see cref="Pets.EquipPower"/>·<see cref="Pets.Procs(PetData, SaveData)"/> 가 전부 <c>d == null</c> 이면 빈 값).
        /// </para>
        /// <para>⚑ 세이브는 이 표를 <b>안 본다</b> — <see cref="SaveData"/> 가 든 것은 수(<c>PetPulls</c>·<c>PetLv</c>·<c>PetFrag</c>·<c>PetEq</c>)뿐이고, 표가 필요한 정리는 <see cref="Pets.Equipped"/> 가 한다(T293 ⓕ).</para>
        /// </summary>
        public PetData Pet;
        /// <summary>신화 위 «표시 등급» 표(갓·초월·불멸·무한) — 이 레포의 <c>Assets/KkomaKnight/gearTier.json</c>(카탈로그 텍스트 «data.gearTier» · T316). 로드는 Bootstrap 이 따로 한다 · 없으면 null(신화 위 등급이 없던 옛 그대로 = 전부 «신화 +N»).</summary>
        public GearTierData GearTier;
        /// <summary>시즌 패스 보상 표 — 이 레포의 <c>Assets/KkomaKnight/pass.json</c>(카탈로그 텍스트 «data.pass» · T322). 로드는 Bootstrap 이 따로 한다 · 없으면 null(패스 화면이 모든 줄을 «?» 로 그린다 — 화면이 막히지는 않는다).</summary>
        public PassData Pass;

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

        /// <summary>
        /// 디스크 폴더에서 로드 (dotnet 하니스·EditMode 테스트용).
        /// <b>이 레포 전용 덮어쓰기 넷</b>(<see cref="OverrideFiles"/> · <c>Assets/KkomaKnight/*Override.json</c> · T173·T325)도 같이 먹인다 —
        /// 안 그러면 Sim 시드 골든·EditMode 가 «게임과 다른 규칙» 으로 돌아 서로 어긋난다(게임 쪽은 <c>Bootstrap</c> 이 카탈로그로 먹인다).
        /// 파일이 없으면 조용히 정본 그대로다.
        /// <para>⚠ <b>정본 그대로를 원하는 자</b>(T2 이식 동일성 골든)는 이 길로 오면 안 된다 — <see cref="Load"/> 를 직접 쓴다.</para>
        /// </summary>
        public static GameData LoadFromDirectory(string dir)
        {
            var d = Load(f => System.IO.File.ReadAllText(System.IO.Path.Combine(dir, f)));
            // data 폴더는 <레포>/Assets/StreamingAssets/data → 덮어쓰기는 <레포>/Assets/KkomaKnight/
            foreach (var file in OverrideFiles)
            {
                var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(dir, "..", "..", "KkomaKnight", file));
                if (System.IO.File.Exists(path)) d.ApplyOverride(file, System.IO.File.ReadAllText(path));
            }
            d.ValidateOverridden();
            return d;
        }

        /// <summary>이 레포 전용 전투 덮어쓰기 파일 이름(카탈로그 텍스트 키는 <c>data.combatOverride</c>).</summary>
        public const string CombatOverrideFile = "combatOverride.json";
        /// <summary>이 레포 전용 장비 덮어쓰기 파일 이름 — 등급·기여·옵션 칸 수(카탈로그 텍스트 키 <c>data.gearOverride</c> · T325).</summary>
        public const string GearOverrideFile = "gearOverride.json";
        /// <summary>이 레포 전용 상자 덮어쓰기 파일 이름 — 등급 확률·비용·천장(카탈로그 텍스트 키 <c>data.gachaOverride</c> · T325).</summary>
        public const string GachaOverrideFile = "gachaOverride.json";
        /// <summary>이 레포 전용 손잡이 덮어쓰기 파일 이름 — 챕터 수·적 세기 곡선(카탈로그 텍스트 키 <c>data.tuneOverride</c> · T325).</summary>
        public const string TuneOverrideFile = "tuneOverride.json";
        /// <summary>이 레포 전용 <b>적 세기</b> 덮어쓰기 파일 이름 — 챕터별 체력·공격 배수(카탈로그 텍스트 키 <c>data.enemiesOverride</c> · T325 ⓑ).</summary>
        public const string EnemiesOverrideFile = "enemiesOverride.json";

        /// <summary>
        /// 이 레포가 정본 위에 얹는 덮어쓰기 표 넷 — <b>먹이는 순서</b>다(장비가 먼저여야 상자의 «칸 수 = 등급 수» 대조가 새 등급으로 선다).
        /// <para>두 길(게임 = <c>Bootstrap</c> 이 카탈로그로 · 하니스 = <see cref="LoadFromDirectory"/> 가 파일로)이
        /// <b>이 한 목록</b>을 돌아 같은 규칙으로 돈다 — 다섯 번째를 더하는 사람은 여기 한 줄과 <see cref="ApplyOverride"/> 의 가지 하나면 된다.</para>
        /// </summary>
        public static readonly string[] OverrideFiles = { CombatOverrideFile, GearOverrideFile, GachaOverrideFile, TuneOverrideFile, EnemiesOverrideFile };

        /// <summary>덮어쓰기 파일 이름 → 카탈로그 텍스트 키(<c>data.&lt;이름&gt;</c>). 두 곳에서 같은 규칙으로 짓는다.</summary>
        public static string OverrideCatalogKey(string file) => "data." + file.Substring(0, file.Length - ".json".Length);

        /// <summary>
        /// 파일 이름으로 알맞은 덮어쓰기를 먹인다 — <see cref="OverrideFiles"/> 를 도는 두 호출부가 <c>switch</c> 를 각자 안 쓰게.
        /// 모르는 이름은 아무 일도 안 한다(목록에 더하고 가지를 안 더하면 조용히 안 먹으므로 <c>GameDataOverrideTests</c> 가 그 짝을 잰다).
        /// </summary>
        public void ApplyOverride(string file, string json)
        {
            switch (file)
            {
                case CombatOverrideFile: ApplyCombatOverride(json); break;
                case GearOverrideFile: ApplyGearOverride(json); break;
                case GachaOverrideFile: ApplyGachaOverride(json); break;
                case TuneOverrideFile: ApplyTuneOverride(json); break;
                case EnemiesOverrideFile: ApplyEnemiesOverride(json); break;
            }
        }

        /// <summary>
        /// <c>combat.json</c>(aaaw 정본 · 불변) 위에 <b>이 레포가 정한 값만</b> 덮는다 (T173 · 주인 지시로 바뀐 전투 규칙).
        /// 적힌 키만 바뀌고 나머지는 정본 그대로다 — 빈 글이거나 못 읽으면 아무 일도 안 한다(부팅은 막히지 않는다).
        /// </summary>
        public void ApplyCombatOverride(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || Combat == null) return;
            var j = new JNode(MiniJson.Parse(json));
            if (!j.IsObject) return;
            var r = j["range"];
            if (r.Has("spearReach")) Combat.SpearReach = r["spearReach"].Num(Combat.SpearReach);
            if (r.Has("waveReach")) Combat.WaveReach = r["waveReach"].Num(Combat.WaveReach);
            var p = j["pierce"];
            if (p.Has("spear")) Combat.PierceSpear = p["spear"].Int(Combat.PierceSpear);
            if (p.Has("wave")) Combat.PierceWave = p["wave"].Int(Combat.PierceWave);
            if (p.Has("waveBig")) Combat.PierceWaveBig = p["waveBig"].Int(Combat.PierceWaveBig);
        }

        /// <summary>
        /// <c>gear.json</c>(aaaw 정본 · 불변) 위에 <b>등급·기여·옵션 칸 수</b>만 덮는다 (T325 ⓐ · 주인 2026-09-09 «영웅 등급 다시 넣고 … 30씩 증가»).
        /// <para>덮는 키는 <c>rarName</c> · <c>rarLegend</c> · <c>rarMyth</c> · <c>contribution.atk/hp/sh</c> ·
        /// <c>optionLadder.optCount</c> · <c>optionLadder.mythPlusAt</c> · <c>enhance.plusStep/legendToMythPlus/legendMaxPlus</c>
        /// — 적힌 것만 바뀌고 나머지는 정본 그대로다.</para>
        /// <para>⚠ <c>optCount</c> 는 <b>«민 뒤» 의 최종 칸 수</b>다 — <c>GearData.ShiftOptionLadderOneStep</c>(주인 «일반은 옵션 안 열리게»)는
        /// 정본을 읽을 때 이미 돌았고 여기서 다시 돌지 않는다. 곧 «일반 0» 을 원하면 표에 <c>0</c> 이라고 적는다 — 한 칸 더 밀리지 않는다.</para>
        /// <para>⚑ <c>enhance</c> 칸은 T405 가 열었다(주인 2026-09-10 «그 전설 2강보다 신화 0강이 세야 함»). 주인의 등차 표(+30)에서는
        /// 정본 <c>plusStep 2.111</c> 이 등급을 통째로 덮으므로(일반 +2 &gt; 신화 0강) 이 표가 그 배율을 줄인다.
        /// <b>세 칸을 다 연다</b> — 반만 열면 <c>legendToMythPlus</c> 를 적어 놓고 «안 먹는» 제일 조용한 고장이 난다(상자 표가 오타에 던지는 것과 같은 자리).</para>
        /// </summary>
        public void ApplyGearOverride(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || Gear == null) return;
            var j = new JNode(MiniJson.Parse(json));
            if (!j.IsObject) return;
            if (j.Has("rarName")) Gear.RarName = j["rarName"].StrArray();
            if (j.Has("rarLegend")) Gear.RarLegend = j["rarLegend"].Int(Gear.RarLegend);
            if (j.Has("rarMyth")) Gear.RarMyth = j["rarMyth"].Int(Gear.RarMyth);
            var c = j["contribution"];
            if (c.Has("atk")) Gear.Atk = c["atk"].NumArray();
            if (c.Has("hp")) Gear.Hp = c["hp"].NumArray();
            if (c.Has("sh")) Gear.Sh = c["sh"].NumArray();
            var ol = j["optionLadder"];
            if (ol.Has("optCount")) Gear.OptCountByRar = ol["optCount"].IntArray();
            if (ol.Has("mythPlusAt")) Gear.MythPlusOptAt = ol["mythPlusAt"].IntArray();
            var en = j["enhance"];
            if (en.Has("plusStep")) Gear.PlusStep = en["plusStep"].Num(Gear.PlusStep);
            if (en.Has("legendToMythPlus")) Gear.LegendToMythPlus = en["legendToMythPlus"].Int(Gear.LegendToMythPlus);
            if (en.Has("legendMaxPlus")) Gear.LegendMaxPlus = en["legendMaxPlus"].Int(Gear.LegendMaxPlus);
            var lk = j["look"];
            if (lk.Has("rarSprite")) Gear.LookRarTable = lk["rarSprite"].IntArray();
        }

        /// <summary>
        /// <c>gacha.json</c>(aaaw 정본 · 불변) 위에 <b>상자 칸</b>만 덮는다 (T325 ⓐ 3항 · 주인 «전설 상자는 66% 희귀 · 30% 영웅 · 4% 전설»).
        /// <para>덮는 키는 <c>boxes.&lt;상자키&gt;</c> 아래의 <c>rate</c> · <c>cost</c> · <c>pityMyth</c> · <c>pityLegend</c> · <c>pityRare</c>.
        /// 없는 상자 키는 <b>조용히 넘기지 않고 던진다</b> — 오타 하나로 «확률을 바꿨는데 안 바뀌는» 꼴이 제일 조용한 고장이다.</para>
        /// <para>⚠ <c>cum</c> 은 <b>표에 안 적는다</b> — <c>rate</c> 를 덮으면 여기서 다시 계산한다(<see cref="GachaBox.RarRoll"/> 이 보는 것은 <c>cum</c> 이라
        /// 옛 <c>cum</c> 이 남으면 «표는 새 확률인데 굴림은 옛 확률» 이 되고 아무도 안 운다).</para>
        /// </summary>
        public void ApplyGachaOverride(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || Gacha == null) return;
            var j = new JNode(MiniJson.Parse(json));
            if (!j.IsObject) return;
            var bx = j["boxes"];
            if (!bx.IsObject) return;
            foreach (var k in bx.Keys)
            {
                var box = Gacha.Box(k);          // 없으면 KeyNotFoundException — 오타를 조용히 안 넘긴다
                var b = bx[k];
                if (b.Has("rate")) { box.Rate = b["rate"].NumArray(); box.Cum = CumOf(box.Rate); }
                if (b.Has("cost")) box.Cost = b["cost"].Num(box.Cost);
                if (b.Has("pityMyth")) box.PityMyth = b["pityMyth"].Int(box.PityMyth);
                if (b.Has("pityLegend")) box.PityLegend = b["pityLegend"].Int(box.PityLegend);
                if (b.Has("pityRare")) box.PityRare = b["pityRare"].Int(box.PityRare);
            }
        }

        /// <summary>
        /// <c>rate</c> → <c>cum</c> — 정본 <c>gacha.json</c> 이 적어 둔 뜻 그대로다: <c>cum[i]</c> = 등급 <c>i</c> <b>이상</b> 이 나올 확률의 합
        /// (<c>cum[0]</c> 은 늘 100 · 예: rate [66, 30, 4, 0] → cum [100, 34, 4, 0]).
        /// 뒤에서부터 더해 <see cref="GachaBox.RarRoll"/> 의 «높은 등급부터 누적 임계와 비교» 와 짝이 맞는다.
        /// </summary>
        public static double[] CumOf(double[] rate)
        {
            if (rate == null || rate.Length == 0) return rate;
            var cum = new double[rate.Length];
            double acc = 0;
            for (int i = rate.Length - 1; i >= 0; i--) { acc += rate[i]; cum[i] = acc; }
            cum[0] = 100;                        // 첫 칸은 «무엇이든 나온다» 로 못 박는다(정본도 그렇게 적혀 있다)
            return cum;
        }

        /// <summary>
        /// <c>tune.json</c>(aaaw 정본 · 불변) 위에 <b>챕터 수와 적 세기 곡선</b>만 덮는다 (T325 ⓑ · 주인 «챕터 수는 100 으로 하자»).
        /// <para>덮는 키는 <c>maxChapter</c> · <c>eBaseHp</c> · <c>eBaseDmg</c> · <c>eHpSeg</c> · <c>eDmgSeg</c>
        /// (<c>seg</c> 는 정본과 같은 꼴 <c>[[시작 챕터, 챕터당 배율], …]</c>).</para>
        /// <para>⚠ <b>플레이어·장비·특전 값은 여기 없다</b> — 주인이 준 값이라 손잡이가 아니다(§1). 밸런스는 «적 쪽 곡선» 으로만 맞춘다.</para>
        /// </summary>
        public void ApplyTuneOverride(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || Tune == null) return;
            var j = new JNode(MiniJson.Parse(json));
            if (!j.IsObject) return;
            var t = j.Has("tune") ? j["tune"] : j;      // 정본과 같은 «tune» 껍질을 써도 되고 안 써도 된다
            if (t.Has("maxChapter")) Tune.MaxChapter = t["maxChapter"].Int(Tune.MaxChapter);
            if (t.Has("eBaseHp")) Tune.EBaseHp = t["eBaseHp"].Num(Tune.EBaseHp);
            if (t.Has("eBaseDmg")) Tune.EBaseDmg = t["eBaseDmg"].Num(Tune.EBaseDmg);
            if (t.Has("eHpSeg")) Tune.EHpSeg = TuneData.Seg(t["eHpSeg"]);
            if (t.Has("eDmgSeg")) Tune.EDmgSeg = TuneData.Seg(t["eDmgSeg"]);
        }

        /// <summary>
        /// <c>enemies.json</c>(aaaw 정본 · 불변) 이 <b>챕터마다 구워 둔</b> 적 체력·공격에 <b>배수</b>를 먹인다 (T325 ⓑ · 주인 «챕터 밸런스도 다시»).
        /// <para>⚑ <b>왜 이 표가 따로 필요한가(결정 985).</b> 지시서 8항은 «적 세기 = <c>eBaseHp</c> × 구간 성장률» 이라는 <c>tune.json</c> 손잡이로 밸런스를 잡으라 했는데,
        /// <b>이 레포의 전투는 그 식을 안 쓴다</b> — <c>Battle</c> 는 <c>enemies.json</c> 의 <c>waves[].hp/dmg</c>·<c>boss</c> 를 그대로 읽고,
        /// 그 식(<c>ChapterLayout.EnemyStats</c>)을 부르는 것은 «JSON 이 그 공식과 맞는가» 를 재는 <c>LayoutTests</c> 뿐이다.
        /// 실측: 기저를 1/60000 로 낮추고 성장률을 1.0 으로 둬도 노템·3챕터 클리어율이 10.5% → 10.5% 로 <b>한 자도 안 움직였다</b>.
        /// aaaw 의 <c>sim.js</c> 에서는 그 식이 실제로 적을 만들었고, 이식하면서 «값을 미리 굽는» 쪽으로 한 칸 움직인 것이다.</para>
        /// <para>그래서 곡선을 <b>구운 값 위의 배수</b>로 표현한다 — 정본 <c>enemies.json</c> 은 한 줄도 안 바뀌고(§1),
        /// 표가 비면 배수가 1 이라 <b>오늘과 한 톨도 다르지 않다</b>. 8항의 절차(구간마다 배율 하나씩 · 앞 구간부터)는 그대로 살고 <b>닿는 곳만 바뀐다</b>.</para>
        /// <para>칸은 <c>hpSeg</c>·<c>dmgSeg</c> 이고 꼴은 정본 <c>tune.json</c> 과 같다(<c>[[시작 챕터, 챕터당 배율], …]</c>).
        /// 챕터 <c>c</c> 의 배수 = <c>ChapterLayout.SegGrow(seg, c)</c> — 곧 <b>«aaaw 보다 몇 배 센가»</b> 다(1챕터는 늘 1배).</para>
        /// <para>⚠ <b>한 번만 먹인다</b> — 구운 값을 제자리에서 곱하므로 두 번 부르면 배수가 제곱된다.
        /// 두 길(<see cref="LoadFromDirectory"/> · <c>Bootstrap</c>)은 로드마다 한 번씩만 부른다.</para>
        /// </summary>
        public void ApplyEnemiesOverride(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || Enemies == null) return;
            var j = new JNode(MiniJson.Parse(json));
            if (!j.IsObject) return;
            var hp = j.Has("hpSeg") ? TuneData.Seg(j["hpSeg"]) : null;
            var dmg = j.Has("dmgSeg") ? TuneData.Seg(j["dmgSeg"]) : null;
            if (hp == null && dmg == null) return;
            Seg("enemiesOverride.hpSeg", hp ?? new[] { new double[] { 0, 1 } });
            Seg("enemiesOverride.dmgSeg", dmg ?? new[] { new double[] { 0, 1 } });
            for (int i = 0; i < Enemies.Chapters.Count; i++)
            {
                var ch = Enemies.Chapters[i];
                double kh = hp != null ? ChapterLayout.SegGrow(hp, ch.C) : 1;
                double kd = dmg != null ? ChapterLayout.SegGrow(dmg, ch.C) : 1;
                if (kh == 1 && kd == 1) continue;
                foreach (var w in ch.Waves) { w.Hp *= kh; w.Dmg *= kd; }
                if (ch.Boss != null) { ch.Boss.Hp *= kh; ch.Boss.Dmg *= kd; }
            }
        }

        /// <summary>
        /// 덮어쓰기를 다 먹인 <b>뒤</b> 에 «표들끼리 아직 맞는가» 를 다시 잰다 (T325).
        /// <para><see cref="Validate"/> 는 정본을 읽는 순간 한 번 돌고 끝나므로 <b>덮어쓰기가 깬 것은 아무도 못 본다</b> —
        /// 예를 들어 등급을 다섯으로 늘리고 <c>contribution</c> 을 넷으로 두면 신화 장비를 낀 순간에야 <c>IndexOutOfRange</c> 가 나고,
        /// 상자 <c>rate</c> 를 넷으로 두면 <b>영웅이 영영 안 나온다</b>(예외도 없다). 그래서 부팅에서 먼저 던진다.</para>
        /// <para>⚠ 챕터 수는 정본이 «같은가» 로 재지만 여기서는 <b>«정본이 더 많아도 된다»</b> 로 잰다 —
        /// 100 챕터로 줄이는 것이 주인 지시이고 <c>enemies.json</c> 은 420 줄 그대로 남기 때문이다(§1).</para>
        /// </summary>
        public void ValidateOverridden()
        {
            if (Gear != null)
            {
                int n = Gear.RarName != null ? Gear.RarName.Length : 0;
                if (n < 2) throw new FormatException("덮어쓰기 뒤 등급 수가 " + n + " 이다 — rarName 은 둘 이상이어야 한다");
                Len("contribution.atk", Gear.Atk, n); Len("contribution.hp", Gear.Hp, n); Len("contribution.sh", Gear.Sh, n);
                if (Gear.OptCountByRar != null && Gear.OptCountByRar.Length != n)
                    throw new FormatException($"덮어쓰기 뒤 optionLadder.optCount 칸 수 {Gear.OptCountByRar.Length} ≠ 등급 수 {n}");
                if (Gear.RarLegend < 1 || Gear.RarMyth <= Gear.RarLegend || Gear.RarMyth >= n)
                    throw new FormatException($"덮어쓰기 뒤 등급 인덱스가 어긋난다 — rarLegend {Gear.RarLegend} · rarMyth {Gear.RarMyth} · 등급 수 {n}");
                // 문 ⓑ — 등급이 그림 칸보다 많아지면 «영웅은 어느 그림인가» 를 표가 답해야 한다(§1 이 새 그림을 금한다).
                // 답이 없으면 GearLook 이 조용히 마지막 칸으로 눌러 신화가 전설 그림을 쓴다 — 그러니 여기서 먼저 운다.
                if (Gear.LookRarTable == null || Gear.LookRarTable.Length == 0)
                {
                    if (n > GearLook.RarCount)
                        throw new FormatException($"등급이 {n} 인데 외형 그림 칸은 {GearLook.RarCount} 다 — gearOverride.json 의 look.rarSprite 로 «어느 등급이 어느 그림을 쓰는가» 를 적어라(새 그림을 만들지 않으려면 기존 칸을 같이 쓴다)");
                }
                else
                {
                    if (Gear.LookRarTable.Length != n)
                        throw new FormatException($"덮어쓰기 뒤 look.rarSprite 칸 수 {Gear.LookRarTable.Length} ≠ 등급 수 {n}");
                    foreach (var v in Gear.LookRarTable)
                        if (v < 0 || v >= GearLook.RarCount)
                            throw new FormatException($"look.rarSprite 에 그림 칸 {v} 가 있다 — 있는 칸은 0..{GearLook.RarCount - 1} 뿐이다");
                }
                if (Gacha != null)
                    foreach (var b in Gacha.Boxes)
                    {
                        Len($"상자 «{b.Key}» 의 rate", b.Rate, n);
                        double sum = 0; foreach (var r in b.Rate) { if (r < 0) throw new FormatException($"상자 «{b.Key}» 에 음수 확률이 있다"); sum += r; }
                        if (Math.Abs(sum - 100) > 0.01) throw new FormatException($"상자 «{b.Key}» 확률 합이 {sum} 이다 — 100 이어야 한다");
                    }
            }
            if (Tune != null)
            {
                if (Tune.MaxChapter < 1) throw new FormatException("덮어쓰기 뒤 maxChapter 가 " + Tune.MaxChapter + " 이다");
                if (Enemies != null && Enemies.Chapters.Count < Tune.MaxChapter)
                    throw new FormatException($"덮어쓰기 뒤 maxChapter {Tune.MaxChapter} 가 enemies.json 챕터 수 {Enemies.Chapters.Count} 보다 많다");
                Seg("eHpSeg", Tune.EHpSeg); Seg("eDmgSeg", Tune.EDmgSeg);
            }
        }

        static void Len(string what, double[] a, int n)
        {
            if (a == null || a.Length != n) throw new FormatException($"덮어쓰기 뒤 {what} 칸 수 {(a == null ? 0 : a.Length)} ≠ 등급 수 {n}");
        }

        static void Seg(string what, double[][] seg)
        {
            if (seg == null || seg.Length == 0) throw new FormatException($"덮어쓰기 뒤 {what} 가 비었다");
            double prev = double.NegativeInfinity;
            foreach (var row in seg)
            {
                if (row == null || row.Length < 2) throw new FormatException($"{what} 의 줄은 [시작 챕터, 배율] 이어야 한다");
                if (row[0] <= prev) throw new FormatException($"{what} 의 시작 챕터가 오름차순이 아니다 ({prev} → {row[0]})");
                if (row[1] <= 0) throw new FormatException($"{what} 의 배율이 {row[1]} 이다 — 0 보다 커야 한다");
                prev = row[0];
            }
        }

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

        internal static double[][] Seg(JNode a)
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
        /// <summary>
        /// «희귀» 등급 인덱스 — <b>맨 아래(일반 = 0) 바로 위</b>다.
        /// <para>T261 이 «희귀 확정» 천장을 얹으면서 필요해졌다(<c>GearSystem</c> 의 <c>pityR</c> 두 줄).</para>
        /// <para>⚠ <b>여기는 «전설 바로 아래»(<c>RarLegend - 1</c>) 였다 — T325 가 그 뜻을 무너뜨려서 바꿨다.</b>
        /// 옛 표(일반·희귀·전설·신화)에서는 두 뜻이 같은 수 <c>1</c> 이었지만, 주인이 <b>영웅을 가운데</b> 넣으면
        /// (일반·희귀·<b>영웅</b>·전설·신화) «전설 바로 아래» 는 <b>영웅</b> 이 된다 — 그러면 희귀 상자의 «희귀 확정» 천장이
        /// 말없이 <b>«영웅 확정»</b> 이 되고, 컴파일도 되고 빨간 줄도 안 난다(워커 B 가 결정 918 에서 <c>Palette.RarName</c> 사슬을 두고 적은 그 위험이 여기에도 있었다).
        /// 참인 관계는 «희귀 = 일반 바로 위» 다 — 등급이 위쪽에 얼마나 늘든 안 흔들린다. 오늘 값은 <c>1</c> 로 <b>그대로</b>이고
        /// <c>GameDataOverrideTests</c> 가 «넷일 때도 다섯일 때도 희귀» 를 잰다.</para>
        /// </summary>
        public int RarRare => 1;
        /// <summary>
        /// 등급 → <b>외형 그림 칸</b>(<c>gearOverride.json</c> 의 <c>look.rarSprite</c> · null = 항등 · T325 문 ⓑ).
        /// <para>등급 수와 그림 칸 수는 <b>같은 수가 아니다</b> — 주인이 «영웅 등급 다시 넣고» 라고 했지만 <b>파츠 그림은 안 줬고 §1 이 새 그림을 금한다</b>.
        /// 그래서 «영웅은 어느 그림을 쓰는가» 를 이 표가 답한다(회차 1 이 남긴 답 = «기존 넷 중 하나를 같이 쓴다» · 예: <c>[0, 1, 1, 2, 3]</c> = 영웅이 희귀 그림 재사용).</para>
        /// </summary>
        public int[] LookRarTable;
        /// <summary>
        /// 등급 <paramref name="rar"/> 이 쓸 <b>그림 칸</b>. 표가 없으면 항등(오늘 = 등급 넷 · 그림 넷이라 한 톨도 안 바뀐다).
        /// <para>⚠ <c>GearLook</c> 의 등급 인자는 <b>이 함수를 거친 수</b>여야 한다 — 안 거치면 등급이 다섯이 되는 날
        /// 그림이 위로 한 칸씩 밀리는데(신화가 전설 그림을 쓴다) <b>컴파일도 되고 빨간 줄도 안 난다</b>. <c>GearLookWiringTests</c> 가 그 호출부를 센다.</para>
        /// </summary>
        public int LookRar(int rar)
        {
            if (rar < 0) rar = 0;
            var t = LookRarTable;
            if (t == null || t.Length == 0) return rar;
            return rar < t.Length ? t[rar] : t[t.Length - 1];
        }
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
        /// <summary>
        /// T261 — **희귀 상자의 «희귀 확정» 천장**(주인 2026-09-09 «희귀 확정까지 10회 … 실제로 그런 식으로 기능되게»).
        /// <para><b>원본과 다른 유일한 값이다</b> — `data/gacha.json` 의 `rare` 는 `pityLegend 0 · pityMyth 0` 으로 천장이 없다.
        /// 그 파일은 aaaw 정본이라 손대지 않으므로(§1) 읽은 뒤 <see cref="From"/> 한 곳에서만 얹는다. 되돌리려면 이 상수 하나다.</para>
        /// </summary>
        public const int RarePity = 10;
        /// <summary>그 천장을 얹을 상자의 키 — 표의 상자 키다(`rare` · 「희귀 상자」).</summary>
        public const string RareBoxKey = "rare";
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
                var box = new GachaBox { Key = b["key"].Str(k), Name = b["name"].Str(), Cost = b["cost"].Num(), Rate = b["rate"].NumArray(), Cum = b["cum"].NumArray(),
                                        PityMyth = b["pityMyth"].Int(), PityLegend = b["pityLegend"].Int(), PityRare = b["pityRare"].Int() };
                // ⚑ T261 — **원본과 다르다(주인 지시 2026-09-09 06:0X «희귀 상자도 «희귀 확정까지 10회» … 실제로 그런 식으로 기능되게»).**
                //   `data/gacha.json` 은 aaaw 정본이라 손대지 않는다(§1) — 그래서 **읽은 뒤 이 한 곳에서만** 희귀 상자의 천장을 얹는다.
                //   원본이 언젠가 `pityRare` 를 갖게 되면 그 값이 이기고(위에서 이미 읽었다) 이 덮기는 저절로 안 걸린다.
                if (box.PityRare <= 0 && box.Key == RareBoxKey) box.PityRare = RarePity;
                d.Boxes.Add(box);
            }
            d.TenPullCount = j["tenPull"]["count"].Int(10); d.TenPullDiscount = j["tenPull"]["discount"].Num();
            var e = j["economy"]; d.PullCost = e["pullCost"].Num(); d.DailyGem = e["dailyGem"].Num(); d.IapGem = e["iapGem"].Num(); d.RunsPerDay = e["runsPerDay"].Int();
            return d;
        }
    }
    public sealed class GachaBox
    {
        public string Key, Name; public double Cost; public double[] Rate, Cum; public int PityMyth, PityLegend;
        /// <summary>«희귀 확정» 천장 — 이만큼 연속으로 희귀 미만이 나오면 다음 뽑기는 희귀 이상이 된다(0 = 천장 없음 · T261).</summary>
        public int PityRare;
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
        /// <summary><c>Free</c> = 무료 보급 때 가격 버튼이 «Free» 로 바뀌는 줄인가(T259 3항 · 표의 <c>free</c>).</summary>
        public sealed class GemPack { public int Won; public double Gem; public bool Free; }
        /// <summary><c>Free</c> = 위와 같다(주인 «1000골드 부분도 마찬가지»).</summary>
        public sealed class GoldPack { public double Gold; public double Gem; public bool Free; }
        public List<GemPack> GemPacks = new List<GemPack>();
        public List<GoldPack> GoldPacks = new List<GoldPack>();

        /// <summary>
        /// 무료 보급 때 «Free» 가 되는 <b>다이아 줄</b>(주인 2026-09-09 «100다이아 부분 상품도 무료 보급 때마다 1회 Free») — 없으면 null.
        /// <para>
        /// <b>«첫 줄» 이 아니라 «표가 그렇다고 적은 줄» 이다.</b> 순서로 정하면 표에 줄을 하나 끼우는 순간 무료 상품이 조용히 바뀐다 —
        /// 그 사고는 화면에도 자에도 «오류» 로 안 뜨고, 그냥 다른 상품이 공짜가 된다(결정 758).
        /// </para>
        /// </summary>
        public GemPack FreeGemPack { get { foreach (var p in GemPacks) if (p.Free) return p; return null; } }
        /// <summary>무료 보급 때 «Free» 가 되는 <b>골드 줄</b>(주인 «1000골드 부분도 마찬가지») — 없으면 null.</summary>
        public GoldPack FreeGoldPack { get { foreach (var p in GoldPacks) if (p.Free) return p; return null; } }

        public static ShopData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static ShopData From(JNode j)
        {
            var d = new ShopData();
            foreach (var p in j["gemPacks"].Items()) d.GemPacks.Add(new GemPack { Won = (int)p["won"].ReqNum("gemPacks.won"), Gem = p["gem"].ReqNum("gemPacks.gem"), Free = p["free"].Bool() });
            foreach (var p in j["goldPacks"].Items()) d.GoldPacks.Add(new GoldPack { Gold = p["gold"].ReqNum("goldPacks.gold"), Gem = p["gem"].ReqNum("goldPacks.gem"), Free = p["free"].Bool() });
            if (d.GemPacks.Count == 0 && d.GoldPacks.Count == 0) throw new FormatException("shop.json: gemPacks/goldPacks 가 비어 있다");
            // T259 3항 — 무료 줄은 갈래마다 **하나**여야 한다. 둘이면 화면이 어느 것을 «Free» 로 그릴지 조용히 골라 버리고,
            // 그 선택은 표를 봐도 코드를 봐도 안 보인다(«위에서 첫 번째» 라는 규칙이 아무 데도 안 적혀 있으므로).
            int fg = 0; foreach (var p in d.GemPacks) if (p.Free) fg++;
            int fo = 0; foreach (var p in d.GoldPacks) if (p.Free) fo++;
            if (fg > 1) throw new FormatException("shop.json: gemPacks 의 free 가 " + fg + "줄이다 — 무료 보급 줄은 하나여야 한다");
            if (fo > 1) throw new FormatException("shop.json: goldPacks 의 free 가 " + fo + "줄이다 — 무료 보급 줄은 하나여야 한다");
            return d;
        }
    }
}
