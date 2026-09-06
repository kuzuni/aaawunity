using System;
using System.Collections.Generic;

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
            Inv.RemoveAll(g => g == null || Array.IndexOf(D.Gear.Parts, g.Part) < 0 || !D.Gear.Options.ContainsKey(g.Type) || g.Rar < 0 || g.Rar >= D.Gear.RarName.Length);
            foreach (var g in Inv) { g.Plus = Math.Max(0, g.Plus); if (g.Rar == D.Gear.RarLegend && g.Plus >= D.Gear.LegendToMythPlus) { g.Rar = D.Gear.RarMyth; g.Plus = 0; } }
            Uid = Math.Max(1, Uid);
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
            };
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
                    foreach (var g in j["inv"].Items())
                        s.Inv.Add(new GearItem { Uid = g["u"].Int(), Part = g["part"].Str(), Type = g["type"].Str(), Rar = g["rar"].Int(), Plus = g["plus"].Int(), IsNew = g["nw"].Num() != 0 });
                    foreach (var k in j["eq"].Keys) s.Eq[k] = j["eq"][k].Int();
                    foreach (var k in j["slots"].Keys) s.Slots[k] = j["slots"][k].Int();
                    foreach (var k in j["gachaBoxes"].Keys) s.GachaBoxes[k] = new GachaState { P50 = j["gachaBoxes"][k]["p50"].Int(), P10 = j["gachaBoxes"][k]["p10"].Int(), Pulls = j["gachaBoxes"][k]["pulls"].Int() };
                }
                catch (Exception) { s = new SaveData(); }
            }
            s.Normalize(D);
            return s;
        }
    }
}
