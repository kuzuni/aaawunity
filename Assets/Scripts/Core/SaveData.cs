using System;
using System.Collections.Generic;
using System.Globalization;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 계정 저장 데이터 — index.html 의 세이브 v2(`kkoma-knight-v2`)와 같은 필드. 순수 C# (직렬화는 MiniJson).
    /// 저장 매체(PlayerPrefs·파일)는 게임 층이 준다. 정규화(<see cref="Normalize"/>)는 index.html 의 로드 보정과 같다.
    /// </summary>
    public sealed class SaveData
    {
        public const int Version = 2;
        public double Gold, Gem;
        public int MaxChapter = 1, SelChapter = 1;
        /// <summary>배경음 음소거(T28 — 옛 `muted` 를 여기로 이관 · JSON `muteBgm` · 없으면 `muted` 값) · 효과음 음소거(`muteSfx` · 없으면 false).</summary>
        public bool MuteBgm, MuteSfx;
        /// <summary>옛 이름(소리 전체 = BGM 스위치) — T28 이후 BGM 음소거의 별칭. 기존 호출·테스트 호환용.</summary>
        public bool Muted { get => MuteBgm; set => MuteBgm = value; }
        /// <summary>전투 배속(x1/x2) 기억 — T18. index.html `kkoma-knight-v2` 에 없는 필드라 «없으면 1». 표시·연출 배속이지 게임 수치가 아니다.</summary>
        public int Speed = SpeedMin;
        public const int SpeedMin = 1, SpeedMax = 2;
        public List<GearItem> Inv = new List<GearItem>();
        public Dictionary<string, int> Eq = new Dictionary<string, int>();        // 부위 → uid
        public Dictionary<string, int> Slots = new Dictionary<string, int>();     // 부위 → 슬롯 레벨
        public Dictionary<string, GachaState> GachaBoxes = new Dictionary<string, GachaState>();
        public int Pulls, Fuses, Uid = 1;
        public string FreeDay = "";
        /// <summary>데일리 기프트(T77 · 주인 2026-09-07) — 누적이 살아 있는 날짜(<c>yyyy-MM-dd</c> · 비어 있으면 «아직 한 번도 안 열었다»).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환 · <see cref="Speed"/>·<see cref="FreeDay"/> 와 같은 방식).</summary>
        public string GiftDay = "";
        /// <summary>오늘 본 광고 누적 횟수(상한 = dailyGift.json 마지막 줄).</summary>
        public int GiftAds;
        /// <summary>오늘 «오늘의 선물» 무료 칸을 받았는가.</summary>
        public bool GiftFree;
        /// <summary>줄별 수령 여부(dailyGift.json milestones 순 · 길이는 <see cref="DailyGift.Roll"/> 이 표에 맞춘다).</summary>
        public List<bool> GiftClaimed = new List<bool>();
        /// <summary>프로필 아바타 테두리 색(T96-profile · 주인 2026-09-07 «상단 재화 바의 아바타를 누르면 프로필») —
        /// <c>ui.profileFrame.&lt;색&gt;</c>(ProfileFrame_02 다섯 변형)의 색 이름. 빈 값 = 기본(노랑 · 종전과 같은 조각).
        /// 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환).</summary>
        public string ProfileColor = "";
        /// <summary>플레이어 이름(T96-profile 2단계 · 주인 2026-09-07 «<c>Social_Profile_Nickname</c> 이거 좀 써라 프리팹들») —
        /// 규칙·기본값은 <see cref="Nickname"/> 한 곳이 갖는다. 빈 값 = 안 지었다(= <see cref="Nickname.Default"/>).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환 · <see cref="ProfileColor"/> 와 같은 방식).</summary>
        public string Nick = "";
        /// <summary>탐험(T97 · 주인 2026-09-07) — <b>마지막 정산 시각</b>(UTC 유닉스 초 · 0 이면 «아직 한 번도 안 열었다» → 여는 순간이 시작점).
        /// 쌓인 양을 저장하지 않고 이 시각 하나만 두므로 «켜 두든 꺼 두든»(오프라인) 같은 속도로 쌓인다(<see cref="Expedition"/>).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환 · <see cref="GiftDay"/> 와 같은 방식).</summary>
        public double ExpSettle;
        /// <summary>빠른 탐험 횟수가 살아 있는 날짜(<c>yyyy-MM-dd</c> · 바뀌면 <see cref="ExpQuickUsed"/> 를 0 으로).</summary>
        public string ExpQuickDay = "";
        /// <summary>오늘 쓴 빠른 탐험 횟수(상한 = expedition.json <c>quickAdsPerDay</c>).</summary>
        public int ExpQuickUsed;
        /// <summary>던전 티켓(T99 · 주인 2026-09-07) — 티켓·하루치 횟수가 살아 있는 날짜(<c>yyyy-MM-dd</c> · 바뀌면 <see cref="DungeonTickets.Roll"/> 이 보충한다).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 기본값»(옛 세이브 호환 · <see cref="GiftDay"/> 와 같은 방식).</summary>
        public string DunDay = "";
        /// <summary>던전 키(hell·expedition) → 보유 티켓 수.</summary>
        public Dictionary<string, int> DunTickets = new Dictionary<string, int>();
        /// <summary>던전 키 → 오늘 쓴 «광고 보고 티켓» 횟수(상한 = dungeon.json <c>adPerDay</c>).</summary>
        public Dictionary<string, int> DunAdUsed = new Dictionary<string, int>();
        /// <summary>던전 키 → 오늘 쓴 «다이아로 티켓» 횟수(상한 = dungeon.json <c>gemPerDay</c>).</summary>
        public Dictionary<string, int> DunGemUsed = new Dictionary<string, int>();
        /// <summary>이미 받은 <b>챕터 보상</b>(Chapter Chest) — 챕터 → «받은 단» 비트(T137 · 주인 2026-09-07 «챕터 보상은 챕터당 3개» · 단 하나가 비트 하나).
        /// index.html 세이브에 없는 이 레포 전용 필드라 «없으면 빈 목록»(옛 세이브 호환 · <see cref="DunDay"/> 와 같은 방식) — 세이브 버전은 그대로 둔다.
        /// T98 때의 «챕터 번호 목록» 세이브는 <see cref="ChapterChest.OldSaveAll"/> 로 읽어 두었다가 <see cref="ChapterChest.Normalize"/> 가 «단 다 받음» 으로 옮긴다.</summary>
        public Dictionary<int, int> ChestClaimed = new Dictionary<int, int>();
        /// <summary>챕터 → 그 챕터에서 잡아 본 <b>최고 처치 수</b>(T137 3항 · 챕터 보상 진행도) — <b>이기든 지든</b> <c>BattleScreen.EndRun</c> 한 곳에서 남는다.
        /// 이 레포 전용 필드라 «없으면 빈 목록»(옛 세이브 호환 · 세이브 버전 유지) — 이미 깬 챕터는 값이 없어도 «전멸» 로 친다(<see cref="ChapterChest.Progress"/>).</summary>
        public Dictionary<int, int> ChestKills = new Dictionary<int, int>();

        public static SaveData NewSave(GameData D)
        {
            var s = new SaveData();
            s.Normalize(D);
            return s;
        }

        public GearItem InvById(int u) { foreach (var g in Inv) if (g.Uid == u) return g; return null; }
        public GearItem EquippedGear(string part) => Eq.TryGetValue(part, out var u) ? InvById(u) : null;
        public bool IsEquipped(GearItem g) => Eq.TryGetValue(g.Part, out var u) && u == g.Uid;
        public int SlotLv(string part) => Slots.TryGetValue(part, out var l) ? l : 0;
        public GearItem NewGear(string part, string type, int rar, int plus) => new GearItem { Uid = Uid++, Part = part, Type = type, Rar = rar, Plus = plus };

        public Build CurBuild(GameData D)
        {
            var b = new Build();
            foreach (var pt in D.Gear.Parts) { b.Eq[pt] = EquippedGear(pt); b.Slots[pt] = SlotLv(pt); }
            return b;
        }

        public HashSet<GearItem> EquippedSet(GameData D)
        {
            var set = new HashSet<GearItem>();
            foreach (var pt in D.Gear.Parts) { var g = EquippedGear(pt); if (g != null) set.Add(g); }
            return set;
        }

        /// <summary>index.html 의 로드 보정 — 상한 클램프 · 인벤 검증 · 전설 +3 이상 → 신화 0강 · uid 유일성 · 상자별 피티 카운터.</summary>
        public void Normalize(GameData D)
        {
            MaxChapter = Math.Max(1, Math.Min(MaxChapter, D.Tune.MaxChapter));
            SelChapter = Math.Max(1, Math.Min(SelChapter, MaxChapter));
            Gold = Math.Max(0, Gold); Gem = Math.Max(0, Gem);
            Speed = Math.Max(SpeedMin, Math.Min(Speed, SpeedMax));
            GiftAds = Math.Max(0, GiftAds); if (GiftClaimed == null) GiftClaimed = new List<bool>();   // 표 길이 보정은 DailyGift.Roll (표를 여기서 모른다)
            if (ChestClaimed == null) ChestClaimed = new Dictionary<int, int>(); ChapterChest.Normalize(this, D);
            if (ExpSettle < 0) ExpSettle = 0; ExpQuickUsed = Math.Max(0, ExpQuickUsed);   // 빠른 탐험 상한은 Expedition.Roll (표를 여기서 모른다) · 시계 되돌림도 거기서
            Inv.RemoveAll(g => g == null || Array.IndexOf(D.Gear.Parts, g.Part) < 0 || !D.Gear.Options.ContainsKey(g.Type) || g.Rar < 0 || g.Rar >= D.Gear.RarName.Length);
            foreach (var g in Inv) { g.Plus = Math.Max(0, g.Plus); if (g.Rar == D.Gear.RarLegend && g.Plus >= D.Gear.LegendToMythPlus) { g.Rar = D.Gear.RarMyth; g.Plus = 0; } }
            Uid = Math.Max(1, Uid);
            // 이름은 다듬어 두고, 규칙에 못 미치면 «안 지었다»(빈 값 = 기본 이름)로 되돌린다 — 화면은 Nickname.Of 만 본다
            Nick = Nickname.Clean(Nick);
            if (Nick.Length < Nickname.MinLen) Nick = "";
            var seen = new HashSet<int>();
            foreach (var g in Inv) { if (g.Uid <= 0) continue; if (seen.Contains(g.Uid)) g.Uid = 0; else seen.Add(g.Uid); }
            foreach (var u in seen) if (u >= Uid) Uid = u + 1;
            foreach (var g in Inv) if (g.Uid <= 0) g.Uid = Uid++;
            foreach (var pt in D.Gear.Parts) Slots[pt] = Math.Max(0, Math.Min(SlotLv(pt), D.Gear.SlotLvMax));
            var badEq = new List<string>();
            foreach (var kv in Eq) { var g = InvById(kv.Value); if (g == null || g.Part != kv.Key) badEq.Add(kv.Key); }
            foreach (var k in badEq) Eq.Remove(k);
            foreach (var b in D.Gacha.Boxes)
            {
                if (!GachaBoxes.TryGetValue(b.Key, out var st) || st == null) GachaBoxes[b.Key] = new GachaState();
                else { st.P50 = Math.Max(0, st.P50); st.P10 = Math.Max(0, st.P10); st.Pulls = Math.Max(0, st.Pulls); }
            }
        }

        public string ToJson()
        {
            var o = new Dictionary<string, object>
            {
                ["v"] = (double)Version, ["gold"] = Gold, ["gem"] = Gem, ["maxChapter"] = (double)MaxChapter, ["selChapter"] = (double)SelChapter,
                ["muted"] = MuteBgm, ["muteBgm"] = MuteBgm, ["muteSfx"] = MuteSfx, ["speed"] = (double)Speed, ["pulls"] = (double)Pulls, ["fuses"] = (double)Fuses, ["uid"] = (double)Uid, ["freeDay"] = FreeDay ?? "",
                ["giftDay"] = GiftDay ?? "", ["giftAds"] = (double)GiftAds, ["giftFree"] = GiftFree,
                ["expSettle"] = ExpSettle, ["expQuickDay"] = ExpQuickDay ?? "", ["expQuickUsed"] = (double)ExpQuickUsed,
                ["profileColor"] = ProfileColor ?? "", ["nick"] = Nick ?? "",
            };
            var gc = new List<object>(); foreach (var b in GiftClaimed) gc.Add(b); o["giftClaimed"] = gc;
            o["dunDay"] = DunDay ?? "";
            var inv = new List<object>();
            foreach (var g in Inv)
            {
                var gi = new Dictionary<string, object> { ["u"] = (double)g.Uid, ["part"] = g.Part, ["type"] = g.Type, ["rar"] = (double)g.Rar, ["plus"] = (double)g.Plus };
                if (g.IsNew) gi["nw"] = 1.0;
                inv.Add(gi);
            }
            o["inv"] = inv;
            var eq = new Dictionary<string, object>(); foreach (var kv in Eq) eq[kv.Key] = (double)kv.Value; o["eq"] = eq;
            var sl = new Dictionary<string, object>(); foreach (var kv in Slots) sl[kv.Key] = (double)kv.Value; o["slots"] = sl;
            var dt = new Dictionary<string, object>(); foreach (var kv in DunTickets) dt[kv.Key] = (double)kv.Value; o["dunTickets"] = dt;
            var da = new Dictionary<string, object>(); foreach (var kv in DunAdUsed) da[kv.Key] = (double)kv.Value; o["dunAdUsed"] = da;
            var dgm = new Dictionary<string, object>(); foreach (var kv in DunGemUsed) dgm[kv.Key] = (double)kv.Value; o["dunGemUsed"] = dgm;
            // T137 — «챕터 → 받은 단 비트» 라 목록이 아니라 표로 적는다(옛 세이브의 목록 꼴도 읽는다 · FromJson)
            var cc = new Dictionary<string, object>(); foreach (var kv in ChestClaimed) cc[kv.Key.ToString(CultureInfo.InvariantCulture)] = (double)kv.Value; o["chestClaimed"] = cc;
            var ck = new Dictionary<string, object>(); foreach (var kv in ChestKills) ck[kv.Key.ToString(CultureInfo.InvariantCulture)] = (double)kv.Value; o["chestKills"] = ck;
            var gb = new Dictionary<string, object>();
            foreach (var kv in GachaBoxes) gb[kv.Key] = new Dictionary<string, object> { ["p50"] = (double)kv.Value.P50, ["p10"] = (double)kv.Value.P10, ["pulls"] = (double)kv.Value.Pulls };
            o["gachaBoxes"] = gb;
            return MiniJson.Serialize(o);
        }

        public static SaveData FromJson(string json, GameData D)
        {
            var s = new SaveData();
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var j = new JNode(MiniJson.Parse(json));
                    s.Gold = j["gold"].Num(); s.Gem = j["gem"].Num(); s.MaxChapter = j["maxChapter"].Int(1); s.SelChapter = j["selChapter"].Int(1);
                    s.MuteBgm = j.Has("muteBgm") ? j["muteBgm"].Bool() : j["muted"].Bool(); s.MuteSfx = j["muteSfx"].Bool(); s.Speed = j["speed"].Int(SpeedMin); s.Pulls = j["pulls"].Int(); s.Fuses = j["fuses"].Int(); s.Uid = j["uid"].Int(1); s.FreeDay = j["freeDay"].Str("");
                    s.GiftDay = j["giftDay"].Str(""); s.GiftAds = j["giftAds"].Int(); s.GiftFree = j["giftFree"].Bool();
                    s.ExpSettle = j["expSettle"].Num(); s.ExpQuickDay = j["expQuickDay"].Str(""); s.ExpQuickUsed = j["expQuickUsed"].Int();
                    s.ProfileColor = j["profileColor"].Str(""); s.Nick = j["nick"].Str("");
                    foreach (var c in j["giftClaimed"].Items()) s.GiftClaimed.Add(c.Bool());
                    foreach (var g in j["inv"].Items())
                        s.Inv.Add(new GearItem { Uid = g["u"].Int(), Part = g["part"].Str(), Type = g["type"].Str(), Rar = g["rar"].Int(), Plus = g["plus"].Int(), IsNew = g["nw"].Num() != 0 });
                    foreach (var k in j["eq"].Keys) s.Eq[k] = j["eq"][k].Int();
                    foreach (var k in j["slots"].Keys) s.Slots[k] = j["slots"][k].Int();
                    s.DunDay = j["dunDay"].Str("");
                    foreach (var k in j["dunTickets"].Keys) s.DunTickets[k] = j["dunTickets"][k].Int();
                    foreach (var k in j["dunAdUsed"].Keys) s.DunAdUsed[k] = j["dunAdUsed"][k].Int();
                    foreach (var k in j["dunGemUsed"].Keys) s.DunGemUsed[k] = j["dunGemUsed"][k].Int();
                    // T137 — 새 세이브는 «챕터 → 비트» 표, T98 옛 세이브는 «챕터 번호 목록»(그 챕터는 «단 다 받음» = ChapterChest.Normalize 가 옮긴다)
                    var cc = j["chestClaimed"];
                    if (cc.IsArray) foreach (var c in cc.Items()) s.ChestClaimed[c.Int()] = ChapterChest.OldSaveAll;
                    else foreach (var k in cc.Keys) if (int.TryParse(k, NumberStyles.Integer, CultureInfo.InvariantCulture, out var c)) s.ChestClaimed[c] = cc[k].Int();
                    foreach (var k in j["chestKills"].Keys) if (int.TryParse(k, NumberStyles.Integer, CultureInfo.InvariantCulture, out var c)) s.ChestKills[c] = j["chestKills"][k].Int();
                    foreach (var k in j["gachaBoxes"].Keys) s.GachaBoxes[k] = new GachaState { P50 = j["gachaBoxes"][k]["p50"].Int(), P10 = j["gachaBoxes"][k]["p10"].Int(), Pulls = j["gachaBoxes"][k]["pulls"].Int() };
                }
                catch (Exception) { s = new SaveData(); }
            }
            s.Normalize(D);
            return s;
        }
    }
}
