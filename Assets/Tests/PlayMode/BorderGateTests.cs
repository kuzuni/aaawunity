using System.Collections;
using System.Collections.Generic;
using System.Text;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T69 «검은 아웃라인» 게이트(주인 2026-09-06 «행·카드·칸마다 Border») — TextSizeGateTests 와 같은 순서로 모든 화면·팝업을 열고
    /// ⓐ «행·카드·칸» 이름표(<see cref="BorderAudit"/>)마다 어두운 테두리가 있는지 모아 «[BorderGate]» 표로 로그에 남기고, <see cref="BorderAudit.StrictScreens"/> 에 든 화면은 없으면 실패
    /// ⓑ 전투 HUD 바 3개(EXP·HP·실드)와 발밑 2단 바(플레이어 HP·실드 · 적 HP · SpriteRenderer)에 «Border» 가 있고 선이 프레임 8px 이상(ROUTINE T69 3항 «폰 3px») 인지 단언한다(8항).
    /// 빨간 줄 0 은 <see cref="PlayLog"/>.
    /// </summary>
    public class BorderGateTests
    {
        App _app; PlayLog _log;
        readonly List<BorderAudit.Row> _rows = new List<BorderAudit.Row>();

        [SetUp] public void SetUp() { _log = new PlayLog(); _rows.Clear(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

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

        IEnumerator Check(string name)
        {
            UiKit.CompleteAllTweens();
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (cv != null && cv.isRootCanvas) _rows.AddRange(BorderAudit.Collect(name, cv.transform));
            yield return Frames(1);
        }
        static bool Press(Transform root, string name) { if (root == null) return false; var t = UiKit.Find(root, name); var b = t != null ? t.GetComponent<Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        GearItem Give(string part, int rar = 0, int plus = 0)
        {
            foreach (var t in _app.Data.Gear.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, rar, plus); _app.Save.Inv.Add(g); return g; }
            return null;
        }

        /// <summary>uGUI 바 하나의 테두리 계약 — «Border» Image · 스프라이트 이름에 Border · Sliced · 가운데 비움 · raycast 끔 · Ink 알파 ≥ 0.8 · 선 = 원본 px ÷ multiplier ≥ 8 · 바 rect 와 같은 크기(Stretch) · 캡 아이콘은 테두리 위(형제 순서 뒤).</summary>
        static void AssertUiBarBorder(Transform root, string barName)
        {
            var bar = UiKit.Find(root, barName); Assert.IsNotNull(bar, barName);
            Transform bt = null; for (int i = 0; i < bar.childCount; i++) if (bar.GetChild(i).name == UiKit.BorderName) { bt = bar.GetChild(i); break; }
            Assert.IsNotNull(bt, barName + " 에 «Border» 자식");
            var im = bt.GetComponent<Image>(); Assert.IsNotNull(im, barName + " Border 는 Image");
            Assert.IsNotNull(im.sprite, barName + " Border 스프라이트"); Assert.IsTrue(im.sprite.name.Contains("Border"), barName + " Border 스프라이트 = BasicFrame *Border* (" + im.sprite.name + ")");
            Assert.AreEqual(Image.Type.Sliced, im.type, barName + " Border 는 9-slice"); Assert.IsFalse(im.fillCenter, barName + " Border 는 가운데 비움"); Assert.IsFalse(im.raycastTarget, barName + " Border raycast 끔");
            Assert.GreaterOrEqual(im.color.a, 0.8f, barName + " Border 알파 ≥ 0.8"); Assert.IsTrue(UiKit.HasDarkBorder(bar), barName + " 은 어두운 테두리");
            float linePx = UiKit.BorderNativePx(UiKit.BorderKey) / im.pixelsPerUnitMultiplier;
            Assert.GreaterOrEqual(linePx, UiKit.BorderPx - 0.01f, barName + " 테두리 선 ≥ 8px(폰 3px) · 지금 " + linePx.ToString("0.0"));
            var brt = (RectTransform)bt; var prt = (RectTransform)bar;
            Assert.AreEqual(prt.rect.width, brt.rect.width, 0.5f, barName + " Border 폭 = 바 폭"); Assert.AreEqual(prt.rect.height, brt.rect.height, 0.5f, barName + " Border 높이 = 바 높이");
            var cap = UiKit.Find(bar, "Cap"); if (cap != null && cap.parent == bar) Assert.Greater(cap.GetSiblingIndex(), bt.GetSiblingIndex(), barName + " 캡 아이콘은 테두리 위(형제 순서 뒤)");
        }
        /// <summary>월드(SpriteRenderer) 바 하나의 테두리 계약 — «Border» 자식 · Sliced · 크기 = 바 · Ink · sortingOrder > fill · 선 = 프레임 8px 의 월드 길이.</summary>
        static void AssertWorldBarBorder(SpriteRenderer bg, string name)
        {
            Assert.IsNotNull(bg, name);
            Transform bt = null; for (int i = 0; i < bg.transform.childCount; i++) if (bg.transform.GetChild(i).name == UiKit.BorderName) { bt = bg.transform.GetChild(i); break; }
            Assert.IsNotNull(bt, name + " 에 «Border» 자식(SpriteRenderer)");
            var sr = bt.GetComponent<SpriteRenderer>(); Assert.IsNotNull(sr, name + " Border 는 SpriteRenderer");
            Assert.IsNotNull(sr.sprite, name + " Border 스프라이트"); Assert.IsTrue(sr.sprite.name.Contains("Border"), name + " Border 스프라이트 = BasicFrame *Border* (" + sr.sprite.name + ")");
            Assert.AreEqual(SpriteDrawMode.Sliced, sr.drawMode, name + " Border 는 Sliced");
            Assert.AreEqual(bg.size.x, sr.size.x, 1e-3f, name + " Border 폭 = 바 폭"); Assert.AreEqual(bg.size.y, sr.size.y, 1e-3f, name + " Border 높이 = 바 높이");
            Assert.GreaterOrEqual(sr.color.a, 0.8f, name + " Border 알파 ≥ 0.8");
            if (bg.gameObject.activeInHierarchy) Assert.IsTrue(UiKit.HasDarkBorder(bg.transform), name + " 은 어두운 테두리");   // 실드 0 이면 파란 단이 꺼져 있다(HudBarsTests ⓑ) — 켜진 바만 색까지 본다
            SpriteRenderer fill = null; for (int i = 0; i < bg.transform.childCount; i++) if (bg.transform.GetChild(i).name == "BarFill") fill = bg.transform.GetChild(i).GetComponent<SpriteRenderer>();
            Assert.IsNotNull(fill, name + " fill"); Assert.Greater(sr.sortingOrder, fill.sortingOrder, name + " Border 는 fill 위");
            float lineWorld = UiKit.BorderNativePx(UiKit.BorderKey) / sr.sprite.pixelsPerUnit;
            Assert.AreEqual(UiKit.WorldBorderLine, lineWorld, 1e-4f, name + " 테두리 선 = 프레임 8px 의 월드 길이");
            Assert.Less(lineWorld * 2f, bg.size.y, name + " 선 2줄이 바 높이 안(위·아래 선이 겹치지 않음)");
        }

        [UnityTest]
        public IEnumerator BattleBarsHaveBordersAndCellTagsAreAudited()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            S.Gold = 11540; S.Gem = 543;

            // 01 로비 · 12 설정
            Assert.AreEqual("lobby", _app.Current.Name);
            yield return Check("01_lobby");
            _app.Overlay.Settings(); yield return Check("12_settings"); _app.Overlay.Close(); yield return Frames(1);

            // 11 특권 · 15~19 로비 팝업
            _app.ShowScreen("privilege"); yield return Frames(2); yield return Check("11_shop_special"); _app.ShowScreen("lobby"); yield return Frames(1);
            LobbyPopups.Quest(_app); yield return Check("15_quest"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.Attendance(_app); yield return Check("16_attendance"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.DailyGift(_app); yield return Check("17_daily_gift"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.Challenge7(_app); yield return Check("18_challenge7"); _app.Overlay.Close(); yield return Frames(1);
            _app.ShowScreen("pass"); yield return Frames(2); yield return Check("19_pass"); _app.ShowScreen("lobby"); yield return Frames(1);

            // 13 펫 · 14 펫 세부
            _app.ShowScreen("pet"); yield return Frames(2); yield return Check("13_pet");
            (_app.Current as PetScreen)?.OpenDetail(0); yield return Check("14_pet_detail"); _app.Overlay.Close(); yield return Frames(1);

            // 20~26 던전·아레나
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(2); yield return Check("20_dungeon");
            var ev = _app.GetScreen<EventsScreen>(); var evRoot = _app.Current.Root;
            if (Press(UiKit.Find(evRoot, "Card:hell"), "EnterBtn")) { yield return Check("21_dungeon_detail"); _app.Overlay.Close(); yield return Frames(1); }
            ev.ShowPage(EventsScreen.PagePvp); yield return Check("22_arena");
            ev.ShowPage(EventsScreen.PageArena); yield return Check("23_arena_enter");
            if (Press(evRoot, "ChallengeBtn")) { yield return Check("24_arena_challenge"); _app.Overlay.Close(); yield return Frames(1); }
            if (Press(evRoot, "RewardsBtn")) { yield return Check("25_arena_rank_reward"); _app.Overlay.Close(); yield return Frames(1); }
            ev.ShowPage(EventsScreen.PageMerchant); yield return Check("26_arena_shop");
            _app.ShowScreen("lobby"); yield return Frames(1);

            // 06 장비 · 07 세부 · 08 대장간 · 09/10 상점
            GearItem firstFree = null;
            foreach (var p in D.Gear.Parts) { var g = Give(p, rar: 1, plus: 1); S.Eq[p] = g.Uid; }
            for (int i = 0; i < 10; i++) { var g = Give(D.Gear.Parts[i % D.Gear.Parts.Length], rar: i % 3, plus: i % 2); if (firstFree == null) firstFree = g; }
            _app.ShowScreen("gear"); yield return Frames(2); yield return Check("06_gear");
            if (firstFree != null) { GearUi.OpenDetail(_app, firstFree, null); yield return Check("07_gear_detail"); _app.Overlay.Close(); yield return Frames(1); }
            _app.ShowScreen("forge"); yield return Frames(2); yield return Check("08_gear_fuse");
            _app.ShowScreen("shop"); yield return Frames(2); yield return Check("10_shop_2");
            (_app.Current as ShopScreen)?.ScrollTo(0f); yield return Check("09_shop_1");
            _log.AssertNoRed("화면 순회(테두리 감사)");

            // 02 전투 — HUD 바 3개 + 발밑 2단(플레이어) + 첫 웨이브 적 바(8항 · strict)
            _app.StartBattle(1); yield return Frames(3);
            Assert.AreEqual("battle", _app.Current.Name);
            var bs = _app.GetScreen<BattleScreen>(); var G = bs != null ? bs.G : null; Assert.IsNotNull(G, "전투 상태"); var W = bs.World; Assert.IsNotNull(W, "월드");
            foreach (var n in new[] { "Bar:EXP", "Bar:HP", "Bar:SH" }) AssertUiBarBorder(bs.Root, n);
            AssertWorldBarBorder(W.PlayerHpBar, "플레이어 HP 단"); AssertWorldBarBorder(W.PlayerShBar, "플레이어 실드 단");
            foreach (var t in new[] { "stat:" + BattleScreen.StatDefs[0].Key }) Assert.IsTrue(UiKit.HasDarkBorder(UiKit.Find(bs.Root, t)), t + " 스탯 칸 테두리");
            _log.AssertNoRed("전투 진입(테두리)");
            Time.timeScale = 3f;
            float t0 = Time.realtimeSinceStartup;
            while (W.EnemyBarCount == 0 && !G.Over && Time.realtimeSinceStartup - t0 < 25f) { if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; } yield return null; }
            Time.timeScale = 0f;
            Assert.Greater(W.EnemyBarCount, 0, "25초 안에 첫 웨이브 적 발밑 바");
            int enemyBars = 0;
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (sr != null && sr.name == "BarBg" && sr.gameObject.activeInHierarchy && sr != W.PlayerHpBar && sr != W.PlayerShBar) { AssertWorldBarBorder(sr, "적 발밑 바 " + sr.transform.GetSiblingIndex()); enemyBars++; }
            Assert.Greater(enemyBars, 0, "적 발밑 바가 하나는 보여야 한다");
            if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; yield return Frames(1); }
            yield return Check("02_battle");
            _log.AssertNoRed("적 조우(테두리)");
            Time.timeScale = 1f; _app.ShowScreen("lobby"); yield return Frames(2);

            // 판정 — strict 화면만 실패 · 나머지는 표
            var bad = new List<string>();
            foreach (var r in _rows) if (!r.HasBorder && BorderAudit.StrictScreens.Contains(r.Screen)) bad.Add(r.ToString());
            var sb = new StringBuilder();
            int missing = 0; foreach (var r in _rows) if (!r.HasBorder) missing++;
            sb.AppendLine($"[BorderGate] 행·카드·칸 이름표 {_rows.Count} · 테두리 없음 {missing} · strict 화면 = {string.Join(",", BorderAudit.StrictScreens)}");
            sb.Append(BorderAudit.Summary(_rows));
            if (missing > 0) { sb.AppendLine("[BorderGate] 테두리 없는 칸 목록(화면 묶음 T69-* 이 0 으로 만든다):"); foreach (var r in _rows) if (!r.HasBorder) sb.AppendLine("  " + r); }
            Debug.Log(sb.ToString());
            Assert.AreEqual(0, bad.Count, "strict 화면에 테두리 없는 행·카드·칸(T69):\n" + string.Join("\n", bad));
            _log.AssertNoRed("테두리 게이트(전 화면)");
            yield return Shutdown();
        }
    }
}
