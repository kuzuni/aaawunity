using System.Collections;
using System.Collections.Generic;
using System.Text;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T90 «퍼센트 표기» 게이트(주인 2026-09-07 «회피 +8» → «회피 +8%») — 비율 스탯 값이 화면에 나갈 때 <c>%</c> 가 붙는지 본다.
    /// 특전 카드(04·05)·장비 세부 옵션 줄(07)은 <see cref="PerkText.Format"/>·<see cref="GearText.Shorten"/> 을 거치므로 <b>실패로</b> 센다(0 이어야 한다).
    /// 나머지 화면은 «[PercentGate]» 표로 CI 로그에만 남긴다 — 화면 코드가 직접 이어 붙이는 자리는 그 화면 묶음 워커가 <see cref="StatText.Signed"/> 로 바꾼 뒤
    /// <see cref="PercentAudit.Strict"/> 를 켠다(<c>TextAudit.ClipStrict</c> 와 같은 방식 · ROUTINE §2 T90 3항).
    /// </summary>
    public class PercentGateTests
    {
        App _app; PlayLog _log;
        readonly List<PercentAudit.Row> _rows = new List<PercentAudit.Row>();
        readonly List<string> _texts = new List<string>();
        /// <summary>
        /// % 가 빠지면 실패인 화면. <b>T90-gear(2단계)에서 06·08·02 를 보태 <see cref="PercentAudit.Strict"/> 를 켰다</b> —
        /// 이 목록은 이제 «표가 0 이 아니어도 반드시 실패하는» 화면을 적어 두는 기록에 가깝다(Strict 가 켜져 있으면 훑은 화면 전부가 0 이어야 한다).
        /// </summary>
        static readonly string[] StrictScreens = { "02_battle", "04_perks", "05_perks_list", "06_gear", "07_gear_detail", "08_gear_fuse" };

        [SetUp] public void SetUp() { _log = new PlayLog(); _rows.Clear(); _texts.Clear(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
        }
        IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }
        IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return Frames(1); }

        /// <summary>한 화면 — 연출을 끝내고 레이아웃을 굳힌 뒤 모든 루트 캔버스의 활성 Text 를 훑는다.</summary>
        IEnumerator Check(string name)
        {
            UiKit.CompleteAllTweens();
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (cv == null || !cv.isRootCanvas) continue;
                _rows.AddRange(PercentAudit.Collect(name, cv.transform));
                foreach (var t in cv.GetComponentsInChildren<UnityEngine.UI.Text>(false))
                    if (t != null && t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text)) _texts.Add(name + "\t" + t.text);
            }
            yield return Frames(1);
        }
        List<PercentAudit.Row> Rows(string screen) { var l = new List<PercentAudit.Row>(); foreach (var r in _rows) if (r.Screen == screen) l.Add(r); return l; }
        bool AnyText(string screen, System.Func<string, bool> pred)
        {
            foreach (var s in _texts) { int i = s.IndexOf('\t'); if (i > 0 && s.Substring(0, i) == screen && pred(s.Substring(i + 1))) return true; }
            return false;
        }
        static bool ClickNamed(Transform root, string name) { var t = UiKit.Find(root, name); var b = t != null ? t.GetComponent<UnityEngine.UI.Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        GearItem Give(string part, int rar = 0, int plus = 0)
        {
            foreach (var t in _app.Data.Gear.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, rar, plus); _app.Save.Inv.Add(g); return g; }
            return null;
        }

        [UnityTest]
        public IEnumerator RatioStatsAreWrittenWithPercentEverywhere()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            S.Gold = 11540; S.Gem = 543;

            // 06 장비 · 07 세부(세트 옵션 7줄 = «치명타 확률 +5» 계열이 나오는 자리)
            GearItem firstFree = null;
            foreach (var p in D.Gear.Parts) { var g = Give(p, rar: 1, plus: 1); S.Eq[p] = g.Uid; }
            for (int i = 0; i < 6; i++) { var g = Give(D.Gear.Parts[i % D.Gear.Parts.Length], rar: 3, plus: 9); if (firstFree == null) firstFree = g; }
            _app.ShowScreen("gear"); yield return Frames(2); yield return Check("06_gear");
            Assert.IsNotNull(firstFree, "인벤 장비가 하나는 있어야 세부 팝업을 연다");
            GearUi.OpenDetail(_app, firstFree, null); yield return Check("07_gear_detail");
            _app.Overlay.Close(); yield return Frames(1);

            // 08 대장간 — 안내 문구(«N개 더 고르세요» · 합성 결과 줄)가 화면 코드에서 조립되는 자리다(T90 2단계 · T90-gear 묶음)
            GearItem m0 = null;
            {
                var t0 = D.Gear.AllTypes[0];
                for (int i = 0; i < 3; i++) { var g = S.NewGear(t0.Part, t0.Type, 0, 0); S.Inv.Add(g); if (m0 == null) m0 = g; }
            }
            _app.ShowScreen("forge"); yield return Frames(2);
            Assert.AreEqual("forge", _app.Current.Name, "대장간 화면");
            yield return Check("08_gear_fuse");
            var forgeContent = UiKit.Find(_app.Current.Root, "Content");
            if (forgeContent != null && m0 != null && ClickNamed(forgeContent, "gear:" + m0.Uid)) { yield return Frames(2); yield return Check("08_gear_fuse"); }

            // 02 전투 HUD · 04 레벨업 3택 · 05 보유 특전(«회피율 +8» 계열이 나오는 자리)
            _app.StartBattle(1); yield return RealSeconds(3f);
            Assert.AreEqual("battle", _app.Current.Name);
            var bs = _app.GetScreen<BattleScreen>(); var G = bs != null ? bs.G : null; Assert.IsNotNull(G, "전투 상태");
            Time.timeScale = 0f;
            if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; yield return Frames(1); }
            yield return Check("02_battle");
            var rng = new Mulberry32(7u);
            var offer = Perks.Offer(D, G.Taken, false, rng);
            if (offer.Count > 0)
            {
                G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
                _app.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick)); yield return Check("04_perks");
                _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
                for (int i = 0; i < offer.Count && i < 3; i++) G.Taken.Add(offer[i]);
            }
            // 보유 특전 목록은 «퍼센트가 빠져 있던» 특전을 일부러 넣어 확인한다(정본 실측 = 회피율·치명타 확률·치명타 피해·반격률)
            foreach (var p in D.Perks.Perks) { if (StatText.Missing(p.Desc) != "" && !G.Taken.Contains(p)) G.Taken.Add(p); if (G.Taken.Count >= 12) break; }
            _app.Overlay.PerkBook(G, null); yield return Check("05_perks_list"); _app.Overlay.Close(); yield return Frames(1);
            Time.timeScale = 1f; _app.ShowScreen("lobby"); yield return Frames(2);

            // 표(«[PercentGate]») 를 먼저 찍고 판정한다 — 먼저 터지면 다른 워커가 자기 화면 수를 못 읽는다(T63-toast 가 CI #119 에서 겪은 함정)
            var sb = new StringBuilder();
            sb.AppendLine($"[PercentGate] % 가 빠진 줄 {_rows.Count}(strict={PercentAudit.Strict} · 실패로 세는 화면 = {string.Join(", ", StrictScreens)} · T90)");
            sb.Append(PercentAudit.Summary(_rows));
            if (_rows.Count > 0) { sb.AppendLine("[PercentGate] 목록(그 화면 묶음 워커가 StatText.Signed 로 고친다):"); foreach (var r in _rows) sb.AppendLine("  " + r); }
            Debug.Log(sb.ToString());

            // 데이터 문구가 표시 함수를 거치는 화면은 0 이어야 한다
            foreach (var screen in StrictScreens)
            {
                var bad = Rows(screen);
                Assert.AreEqual(0, bad.Count, "% 가 빠진 비율 스탯 줄(T90 · " + screen + "):\n" + string.Join("\n", bad.ConvertAll(r => r.ToString()).ToArray()));
            }
            // «붙었다» 를 눈으로도 확인 — 옵션 줄·특전 줄에 실제로 % 가 찍혀야 한다(줄 자체가 안 그려져 0 이 된 것과 구분한다)
            Assert.IsTrue(AnyText("07_gear_detail", s => s.Contains("%")), "장비 세부(07) 옵션 줄에 % 가 하나는 찍혀야 한다");
            Assert.IsTrue(AnyText("05_perks_list", s => s.Contains("%")), "보유 특전 목록(05)에 % 가 하나는 찍혀야 한다");

            if (PercentAudit.Strict) Assert.AreEqual(0, _rows.Count, "% 가 빠진 비율 스탯 줄(전 화면):\n" + string.Join("\n", _rows.ConvertAll(r => r.ToString()).ToArray()));
            _log.AssertNoRed("퍼센트 표기 게이트(T90)");
            yield return Shutdown();
        }
    }
}
