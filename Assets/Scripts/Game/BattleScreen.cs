using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 인게임 화면 — 엔진(<see cref="BattleState"/>) 을 고정 틱(1/30초 · sim.js dt)으로 돌리고 <see cref="BattleWorld"/> 와 HUD 를 갱신한다.
    /// 팝업(Overlay)이 열려 있거나 일시정지면 시간이 흐르지 않는다(T79 규칙). 배치는 <see cref="Layout"/> ② 인게임 자.
    /// </summary>
    public sealed class BattleScreen : GameScreen
    {
        public override string Name => "battle";

        /// <summary>index.html STAT_DEFS — 8칸 · 같은 순서 · 같은 값 표기. HUD 하단 그리드와 특전 팝업 상단 줄이 함께 쓴다.</summary>
        public sealed class StatDef
        {
            public string Key, Label; public Func<BattleState, double> Cur; public Func<double, string> Show;
            public string Fmt(BattleState G) => Show(Cur(G));
            public bool Up(BattleState G, Dictionary<string, double> baseStats) => baseStats != null && baseStats.TryGetValue(Key, out var b) && Cur(G) > b + 0.001;
        }
        public static readonly StatDef[] StatDefs =
        {
            new StatDef { Key = "dmg", Label = "공격력", Cur = g => g.EffDmg(), Show = v => UiKit.Fmt(v) },
            new StatDef { Key = "def", Label = "방어력", Cur = g => g.EffDef(), Show = v => v.ToString("0.0") + "%" },
            new StatDef { Key = "aspd", Label = "공격속도", Cur = g => g.EffAspd(), Show = v => v.ToString("0.00") + "/s" },
            new StatDef { Key = "counter", Label = "반격 확률", Cur = g => g.EffCounter(), Show = v => v.ToString("0.0") + "%" },
            new StatDef { Key = "critR", Label = "치명타 확률", Cur = g => g.EffCritR(), Show = v => v.ToString("0") + "%" },
            new StatDef { Key = "evade", Label = "회피", Cur = g => g.EffEvade(), Show = v => v.ToString("0.0") + "%" },
            new StatDef { Key = "critF", Label = "치명타 배율", Cur = g => g.EffCritF(), Show = v => v.ToString("0") + "%" },
            new StatDef { Key = "steal", Label = "흡혈", Cur = g => g.EffSteal(), Show = v => v.ToString("0") + "%" },
        };

        public BattleState G { get; private set; }
        public Dictionary<string, double> BaseStats { get; private set; }
        BattleWorld _world;
        double _acc; int _speed = 1; bool _paused, _ended;
        RectTransform _pops;

        // HUD
        Text _gold, _gem, _chapTitle, _round, _speedTxt;
        UiKit.Bar _prog, _exp, _hp, _sh;
        RectTransform _buffBar, _perkStrip;
        readonly Text[] _statVals = new Text[StatDefs.Length];
        string _perkStripKey = "", _buffKey = "";

        protected override void Build()
        {
            _pops = UiKit.Rect(Root, "Pops"); UiKit.Stretch(_pops);
            // 상단 재화 (ResourceBar_Group) · 일시정지 · 챕터 제목 · 진행도
            var pills = UiKit.SpawnRt("ui.resourceBar", Root, new Layout.R(Layout.HudPills.X, Layout.HudPills.Y, 48, Layout.HudPills.H));
            _gold = UiKit.SetText(pills, "ResourceBar_Coin/Text (TMP)", "0"); _gem = UiKit.SetText(pills, "ResourceBar_Gem/Text (TMP)", "0");
            var pause = UiKit.SpawnRt("ui.btnPause", Root, Layout.HudMenu); UiKit.Clickable(pause, OnPause);
            var title = UiKit.SpawnRt("ui.lineTitle", Root, new Layout.R(Layout.HudChapTitle.X - 6, Layout.HudChapTitle.Y - 1.2f, Layout.HudChapTitle.W + 12, Layout.HudChapTitle.H + 2.4f));
            _chapTitle = UiKit.SetText(title, "Text (TMP)", "챕터 1");
            _prog = UiKit.MakeBar(Root, "ui.sliderYellow"); UiKit.Pct(_prog.Root, Layout.HudProgress); if (_prog.Txt != null) _prog.Txt.gameObject.SetActive(false);
            // 버프 바 (왼쪽 세로)
            _buffBar = UiKit.Rect(Root, "BuffBar"); UiKit.Pct(_buffBar, Layout.HudBuffBar);
            var vl = _buffBar.gameObject.AddComponent<VerticalLayoutGroup>(); vl.childAlignment = TextAnchor.UpperLeft; vl.spacing = 10; vl.childForceExpandWidth = false; vl.childForceExpandHeight = false; vl.childControlWidth = false; vl.childControlHeight = false;
            // 배속 · 라운드
            var spd = UiKit.SpawnRt("ui.btnSmallBlue", Root, Layout.HudSpeed); _speedTxt = UiKit.ButtonText(spd); UiKit.Clickable(spd, () => { _speed = _speed == 1 ? 2 : 1; RefreshHud(); });
            var round = UiKit.SpawnRt("ui.frameDark", Root, Layout.HudRound);
            _round = UiKit.Text(round, "", 30, Palette.White, TextAnchor.MiddleCenter, true); UiKit.Stretch(_round.rectTransform, 6, 6, 6, 6);
            // 하단 패널
            UiKit.SpawnRt("ui.frameDark", Root, Layout.HudPanel);
            _exp = UiKit.MakeBar(Root, "ui.sliderSky", "pi.star"); UiKit.Pct(_exp.Root, Layout.HudExp);
            _hp = UiKit.MakeBar(Root, "ui.sliderRed", "pi.heart"); UiKit.Pct(_hp.Root, Layout.HudHp);
            _sh = UiKit.MakeBar(Root, "ui.sliderBlue", "pi.shield"); UiKit.Pct(_sh.Root, Layout.HudSh);
            for (int i = 0; i < StatDefs.Length; i++)
            {
                int col = i % 2, row = i / 2;
                var cell = UiKit.Rect(Root, "stat:" + StatDefs[i].Key);
                UiKit.Pct(cell, Layout.HudStats.X + col * Layout.HudStatColR, Layout.HudStats.Y + row * Layout.HudStatRowPitch, Layout.HudStatCellW, Layout.HudStatCellH);
                var ic = UiKit.Icon(cell, "ic", Icons.Stat(StatDefs[i].Key)); UiKit.Pct(ic.rectTransform, 2, 12, 14, 76);
                UiKit.Label(cell, 19, 0, 46, 100, StatDefs[i].Label, 28, Palette.CreamDark, TextAnchor.MiddleLeft);
                _statVals[i] = UiKit.Label(cell, 60, 0, 40, 100, "", 34, Palette.White, TextAnchor.MiddleRight);
            }
            var info = UiKit.SpawnRt("ui.btnInfo", Root, Layout.HudInfo); UiKit.Clickable(info, () => { if (G != null && !App.Overlay.IsOpen) App.Overlay.PerkBook(G, null); });
            _perkStrip = UiKit.Rect(Root, "PerkStrip"); UiKit.Pct(_perkStrip, Layout.HudPerkStrip);
            var hl = _perkStrip.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.childAlignment = TextAnchor.MiddleLeft; hl.spacing = 8; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false; hl.childControlWidth = false; hl.childControlHeight = false;
            var stripHit = _perkStrip.gameObject.AddComponent<Image>(); stripHit.color = new Color(0, 0, 0, 0); UiKit.Clickable(_perkStrip, () => { if (G != null && !App.Overlay.IsOpen) App.Overlay.PerkBook(G, null); }, false);
        }

        // ───────────────────────── 시작 · 종료 ─────────────────────────
        public void Start(int chapter)
        {
            var D = App.Data;
            var rng = new Mulberry32((uint)Environment.TickCount ^ 0x9E3779B9u);
            G = new BattleState(D, chapter, App.Save.CurBuild(D), rng, new InteractivePolicy(), new RunOptions { EmitEvents = true });
            BaseStats = new Dictionary<string, double>(); foreach (var d in StatDefs) BaseStats[d.Key] = d.Cur(G);
            _world?.Dispose(); _world = new BattleWorld(App, G, _pops);
            _acc = 0; _speed = 1; _paused = false; _ended = false; _perkStripKey = ""; _buffKey = "";
            UiKit.Clear(_pops);
            RefreshHud();
        }
        protected override void OnHide() { _world?.Dispose(); _world = null; UiKit.Clear(_pops); }

        void EndToLobby()
        {
            if (G != null && !_ended) { _ended = true; App.Save.Gold += Math.Round(G.Gold); App.Persist(); }
            App.Overlay.Close();
            App.ShowScreen("lobby");
        }
        void OnPause()
        {
            if (G == null || G.Over || App.Overlay.IsOpen) return;
            _paused = true;
            App.Overlay.Pause(() => _paused = false, () => { _paused = false; EndToLobby(); });
        }

        // ───────────────────────── 매 프레임 ─────────────────────────
        public override void Tick(float dt)
        {
            if (G == null || _world == null) return;
            if (!App.Overlay.IsOpen && !_paused && !G.Over)
            {
                _acc += dt * _speed;
                int guard = 0;
                while (_acc >= EngineConst.Dt && guard++ < 8)
                {
                    if (G.Pending != null) { OpenPending(); _acc = 0; break; }
                    _world.BeforeTick(); G.Tick(); _world.AfterTick();
                    _acc -= EngineConst.Dt;
                    if (G.Pending != null) { OpenPending(); _acc = 0; break; }
                    if (G.Over) break;
                }
                if (G.Pending == null && G.PendingLevelUps > 0 && !G.Over) { /* 엔진이 다음 틱에 스스로 연다 */ }
            }
            foreach (var ev in G.Events) _world.Handle(ev);
            G.Events.Clear();
            _world.Sync(dt);
            RefreshHud();
            if (G.Over && !_ended && !App.Overlay.IsOpen) EndRun();
        }

        void OpenPending()
        {
            var p = G.Pending; if (p == null) return;
            switch (p.Kind)
            {
                case PendingKind.LevelUp: App.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick)); break;
                case PendingKind.Rest: App.Overlay.Rest(G, heal => G.ResolveRest(heal)); break;
                case PendingKind.Devil:
                {
                    var perk = p.DevilPerk;
                    App.Overlay.Devil(G, accept => { G.ResolveDevil(accept); if (accept) App.Overlay.DevilGift(perk, null); });
                    break;
                }
                case PendingKind.Angel: App.Overlay.Angel(G, mult => G.ResolveAngel(mult)); break;
            }
        }

        void EndRun()
        {
            _ended = true;
            var D = App.Data; var S = App.Save;
            if (G.Cleared)
            {
                double bonus = Math.Round(D.Tune.GoldClear(G.Chapter));   // index.html openClear: 클리어 보너스 = TUNE.goldClear(chapter)
                G.Gold += bonus;
                bool last = G.Chapter >= D.Tune.MaxChapter;
                int next = Math.Min(G.Chapter + 1, D.Tune.MaxChapter);
                S.MaxChapter = Math.Min(Math.Max(S.MaxChapter, G.Chapter + 1), D.Tune.MaxChapter);
                S.SelChapter = next; S.Gold += Math.Round(G.Gold); App.Persist();
                App.Overlay.Clear(G, last, () => { App.Overlay.Close(); Start(next); _ended = false; }, () => { App.Overlay.Close(); App.ShowScreen("lobby"); });
            }
            else
            {
                S.Gold += Math.Round(G.Gold); App.Persist();
                App.Overlay.Dead(G, () => { App.Overlay.Close(); App.ShowScreen("lobby"); });
            }
        }

        // ───────────────────────── HUD ─────────────────────────
        void RefreshHud()
        {
            if (G == null) return;
            var P = G.P; var D = App.Data;
            if (_gold != null) _gold.text = UiKit.Fmt(G.Gold);
            if (_gem != null) _gem.text = UiKit.Fmt(App.Save.Gem);
            if (_chapTitle != null) _chapTitle.text = $"챕터 {G.Chapter}";
            double lastX = G.Nodes.Count > 0 ? G.Nodes[G.Nodes.Count - 1].X : 1;
            _prog.Set(Math.Min(1, P.WorldX / Math.Max(1, lastX)), null);
            int waves = 0, done = 0; foreach (var n in G.Nodes) if (n.Type == NodeType.Wave || n.Type == NodeType.Boss) { waves++; bool alive = false; foreach (var e in n.Enemies) if (!e.Dead) { alive = true; break; } if (!alive) done++; }
            if (_round != null) _round.text = $"웨이브\n{Math.Min(done + 1, waves)}/{waves}";
            if (_speedTxt != null) _speedTxt.text = "x" + _speed;
            int need = D.Tune.ExpNeed(P.Level);
            _exp.Set(need > 0 ? (double)P.Exp / need : 0, $"Lv {P.Level}  {P.Exp}/{need}");
            _hp.Set(P.MaxHp > 0 ? P.Hp / P.MaxHp : 0, $"{UiKit.Fmt(P.Hp)}/{UiKit.Fmt(P.MaxHp)}");
            _sh.Set(P.MaxSh > 0 ? P.Sh / P.MaxSh : 0, P.MaxSh > 0 ? $"{UiKit.Fmt(P.Sh)}/{UiKit.Fmt(P.MaxSh)}" : "실드 없음");
            for (int i = 0; i < StatDefs.Length; i++) { var d = StatDefs[i]; _statVals[i].text = d.Fmt(G); _statVals[i].color = d.Up(G, BaseStats) ? Palette.Green : Palette.White; }
            RefreshPerkStrip(); RefreshBuffBar();
        }
        void RefreshPerkStrip()
        {
            // 얻은 순서대로 · 중복은 ×N · 넘치면 +N (index.html 주인 지시 ②)
            var order = new List<string>(); var count = new Dictionary<string, int>();
            foreach (var p in G.Taken) { if (!count.ContainsKey(p.Id)) { count[p.Id] = 0; order.Add(p.Id); } count[p.Id]++; }
            string key = string.Join(",", order) + "|" + G.Taken.Count; if (key == _perkStripKey) return; _perkStripKey = key;
            UiKit.Clear(_perkStrip);
            const int max = 11; int shown = 0;
            foreach (var id in order)
            {
                if (shown >= max) { var more = UiKit.Text(_perkStrip, "+" + (order.Count - shown), 28, Palette.CreamDark); more.rectTransform.sizeDelta = new Vector2(70, 80); break; }
                var perk = App.Data.Perks.Perks.Find(x => x.Id == id); if (perk == null) continue;
                var cell = UiKit.Rect(_perkStrip, id); cell.sizeDelta = new Vector2(72, 80);
                var ring = cell.gameObject.AddComponent<Image>(); ring.sprite = UiKit.Circle(); ring.color = Palette.PerkColor(perk);
                var ic = UiKit.Icon(cell, "ic", Icons.Perk(id), Palette.White); UiKit.Pct(ic.rectTransform, 18, 18, 64, 64);
                if (count[id] > 1) { var n = UiKit.Text(cell, count[id].ToString(), 24, Palette.White); UiKit.Pct(n.rectTransform, 50, 50, 55, 50); }
                shown++;
            }
        }
        void RefreshBuffBar()
        {
            // 발동 중 버프 — 출처(특전) 별 묶음 · 등급색 테두리 + 중첩 수 (index.html renderBuffBar)
            var groups = new List<KeyValuePair<string, int>>(); var idx = new Dictionary<string, int>();
            foreach (var kv in G.P.Buffs) foreach (var b in kv.Value)
            {
                string id = b.Tag ?? ("#" + kv.Key);
                if (idx.TryGetValue(id, out var i)) groups[i] = new KeyValuePair<string, int>(id, groups[i].Value + 1);
                else { idx[id] = groups.Count; groups.Add(new KeyValuePair<string, int>(id, 1)); }
            }
            var sb = new System.Text.StringBuilder(); foreach (var g in groups) sb.Append(g.Key).Append(':').Append(g.Value).Append(',');
            string key = sb.ToString(); if (key == _buffKey) return; _buffKey = key;
            UiKit.Clear(_buffBar);
            foreach (var g in groups)
            {
                var perk = App.Data.Perks.Perks.Find(x => x.Id == g.Key);
                var cell = UiKit.Rect(_buffBar, g.Key); cell.sizeDelta = new Vector2(88, 88);
                var slot = UiKit.Spawn("ui.buffSlot", cell); UiKit.Stretch((RectTransform)slot.transform);
                var bg = slot.GetComponentInChildren<Image>(true); if (bg != null) bg.color = perk != null ? Palette.PerkColor(perk) : Palette.Gray;
                string icon = perk != null ? Icons.Perk(perk.Id) : Icons.Stat(g.Key.TrimStart('#') == "atk" ? "dmg" : g.Key.TrimStart('#'));
                var ic = UiKit.Icon(cell, "ic", icon, Palette.White); UiKit.Pct(ic.rectTransform, 20, 20, 60, 60);
                if (g.Value > 1) { var n = UiKit.Text(cell, g.Value.ToString(), 24, Palette.White); UiKit.Pct(n.rectTransform, 55, 55, 45, 45); }
            }
        }
    }
}
