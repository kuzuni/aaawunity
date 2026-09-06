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

        /// <summary>캡슐(pill) 칸의 테두리 계약(T69-lobby · 결정 149 마무리) — «Border» Image · 캡슐 조각(<see cref="UiKit.BorderKeyPill"/>) · 가운데 비움 · 선 ≥ 8px · 아이콘은 테두리 위(pill 왼쪽 끝에 걸친다).</summary>
        static void AssertPillBorder(Transform pill, string label)
        {
            Assert.IsNotNull(pill, label);
            Assert.IsTrue(pill.gameObject.activeInHierarchy, label + " 는 켜진 조각이어야 한다(UiKit.Find 는 꺼진 조각도 찾는다 · 결정 162)");
            Transform bt = null; for (int i = 0; i < pill.childCount; i++) if (pill.GetChild(i).name == UiKit.BorderName) { bt = pill.GetChild(i); break; }
            Assert.IsNotNull(bt, label + " 에 «Border» 자식");
            var im = bt.GetComponent<Image>(); Assert.IsNotNull(im, label + " Border 는 Image");
            Assert.IsNotNull(im.sprite, label + " Border 스프라이트");
            Assert.IsTrue(im.sprite.name.Contains("Rectangle_05"), label + " Border 는 캡슐 조각(BasicFrame_Rectangle_05_White_Border · 지금 " + im.sprite.name + ")");
            Assert.AreEqual(Image.Type.Sliced, im.type, label + " Border 는 9-slice"); Assert.IsFalse(im.fillCenter, label + " Border 는 가운데 비움"); Assert.IsFalse(im.raycastTarget, label + " Border raycast 끔");
            Assert.IsTrue(UiKit.HasDarkBorder(pill), label + " 은 어두운 테두리");
            float linePx = UiKit.BorderNativePx(UiKit.BorderKeyPill) / im.pixelsPerUnitMultiplier;
            Assert.GreaterOrEqual(linePx, UiKit.BorderPx - 0.01f, label + " 테두리 선 ≥ 8px(폰 3px) · 지금 " + linePx.ToString("0.0"));
            var icon = UiKit.Find(pill, "Icon");
            if (icon != null && icon.parent == pill) Assert.Greater(icon.GetSiblingIndex(), bt.GetSiblingIndex(), label + " 아이콘은 테두리 위(형제 순서 뒤)");
        }

        /// <summary>아이템 프레임 칸(ItemFrame_01 조각 · T69 7항)의 테두리 계약(T69-gear) — 조각 자체의 «Border» 링(스프라이트 ItemFrame_01_White_Border)이 켜져 있고 Ink(어둡고 α ≥ 0.8)이며, 선이 화면에서 프레임 8px 이상(원본 5px ÷ multiplier × 조각 축소 배율). 새 Image 를 덧대지 않는다.</summary>
        static void AssertItemFrameBorder(Transform cell, string label)
        {
            Assert.IsNotNull(cell, label);
            Assert.IsTrue(UiKit.HasDarkBorder(cell), label + " 은 어두운 테두리(ItemFrame Border → Ink)");
            int seen = 0;
            foreach (var im in cell.GetComponentsInChildren<Image>(false))
            {
                if (im == null || !im.enabled || im.name != UiKit.BorderName || im.sprite == null || !im.sprite.name.StartsWith(GearUi.ItemBorderSprite)) continue;
                seen++;
                Assert.GreaterOrEqual(im.color.a, 0.8f, label + " ItemFrame Border 알파 ≥ 0.8");
                float lum = 0.299f * im.color.r + 0.587f * im.color.g + 0.114f * im.color.b;
                Assert.LessOrEqual(lum, 0.35f, label + " ItemFrame Border 는 Ink(밝기 ≤ 0.35 · 지금 " + lum.ToString("0.00") + ")");
                Assert.AreEqual(Image.Type.Sliced, im.type, label + " ItemFrame Border 는 9-slice");
                float ratio = cell.lossyScale.x > 0f ? im.transform.lossyScale.x / cell.lossyScale.x : 1f;   // 조각이 칸 안에서 축소된 배율(장착 슬롯 FitScale 0.8)
                float linePx = UiKit.BorderNativePx("fr.itemBorder") / Mathf.Max(0.01f, im.pixelsPerUnitMultiplier) * ratio;
                Assert.GreaterOrEqual(linePx, UiKit.BorderPx - 0.05f, label + " ItemFrame 테두리 선 ≥ 8px(폰 3px) · 지금 " + linePx.ToString("0.0"));
            }
            Assert.Greater(seen, 0, label + " 에 켜진 ItemFrame_01_White_Border 링이 하나는 있어야 한다(7항 «물건 칸 = 장비 화면의 그 프레임»)");
        }

        /// <summary>원형(잠금 슬롯) 칸의 테두리 계약(T69-pet) — «Border» Image · 원형 굵은 조각(<see cref="PetScreen.CircleBorderKey"/> · 선 = 지름의 9.8% 실측) · Ink · raycast 끔 · preserveAspect · 지름 ≥ 82 → 선 ≥ 8px(원형은 9-slice 가 없어 조각 굵기 × 지름으로 잰다) · 자물쇠는 테두리 위.</summary>
        static void AssertCircleBorder(Transform slot, string label)
        {
            Assert.IsNotNull(slot, label);
            Transform bt = null; for (int i = 0; i < slot.childCount; i++) if (slot.GetChild(i).name == UiKit.BorderName) { bt = slot.GetChild(i); break; }
            Assert.IsNotNull(bt, label + " 에 «Border» 자식");
            var im = bt.GetComponent<Image>(); Assert.IsNotNull(im, label + " Border 는 Image");
            Assert.IsNotNull(im.sprite, label + " Border 스프라이트"); Assert.IsTrue(im.sprite.name.Contains("Circle") && im.sprite.name.Contains("Border2"), label + " Border 는 굵은 원형 조각(BasicFrame_Circle_H69_White_Border2 · 지금 " + im.sprite.name + ")");
            Assert.IsTrue(im.preserveAspect, label + " Border 는 원(preserveAspect)"); Assert.IsFalse(im.raycastTarget, label + " Border raycast 끔");
            Assert.IsTrue(UiKit.HasDarkBorder(slot), label + " 은 어두운 테두리");
            var brt = (RectTransform)bt; float dia = Mathf.Min(brt.rect.width, brt.rect.height);
            const float lineRatio = 8f / 82f;   // 조각 실측: 82px 지름에 선 8px
            Assert.GreaterOrEqual(dia * lineRatio, UiKit.BorderPx - 0.05f, label + " 테두리 선 ≥ 8px(폰 3px) · 지름 " + dia.ToString("0.0") + " → 선 " + (dia * lineRatio).ToString("0.0"));
            var lk = UiKit.Find(slot, "Lock"); if (lk != null && lk.parent == slot) Assert.Greater(lk.GetSiblingIndex(), bt.GetSiblingIndex(), label + " 자물쇠는 테두리 위(형제 순서 뒤)");
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
            // T69-lobby(strict) — 기둥 상자 4(사이드 2·보조 줄·이벤트) · 챕터 카드 · 상단 재화 pill 2(캡슐 조각)
            // T78(주인 2026-09-07 «시즌 패스도 삭제»·«성 버튼도 삭제») 이 «Banner»(이벤트 배너)·«Castle»(성) 을 로비에서 지웠다 — 없는 조각을 재던 두 줄이 main 빨강이었다(T82)
            var lobbyRoot = _app.Current.Root;
            foreach (var n in new[] { "SideL", "SideR", "SubRow", "Events", "ChapterCard" })
                Assert.IsTrue(UiKit.HasDarkBorder(UiKit.Find(lobbyRoot, n)), "로비 «" + n + "» 에 어두운 테두리(T69-lobby)");
            // pill 은 «TopBar 안» 에서 찾는다 — 로비 프리팹에도 꺼진 ResourceBar_Group 조각이 남아 있고 UiKit.Find 는 꺼진 것도 먼저 집는다(결정 162)
            var topBar = UiKit.Find(lobbyRoot, "TopBar"); Assert.IsNotNull(topBar, "로비 상단 바(TopBar)");
            AssertPillBorder(UiKit.Find(topBar, "ResourceBar_Coin"), "로비 골드 pill");
            AssertPillBorder(UiKit.Find(topBar, "ResourceBar_Gem"), "로비 보석 pill");
            _app.Overlay.Settings(); yield return Check("12_settings"); _app.Overlay.Close(); yield return Frames(1);

            // 11 특권 · 15~19 로비 팝업
            // T78(주인 2026-09-07) — 18_challenge7 · 19_pass 는 화면째 삭제돼 게이트 대상이 아니다
            _app.ShowScreen("privilege"); yield return Frames(2); yield return Check("11_shop_special"); _app.ShowScreen("lobby"); yield return Frames(1);
            LobbyPopups.Quest(_app); yield return Check("15_quest"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.Attendance(_app); yield return Check("16_attendance"); _app.Overlay.Close(); yield return Frames(1);
            LobbyPopups.DailyGift(_app); yield return Check("17_daily_gift"); _app.Overlay.Close(); yield return Frames(1);

            // 13 펫 · 14 펫 세부 (T69-pet · strict) — 격자 칸·빈 장착 슬롯·세부 칸 = ItemFrame Border → Ink 8px(7항) · 잠금 슬롯 = 굵은 원형 조각 · 합계 줄은 맨 글자(Exempt) · T72 ①② 는 있음만
            _app.ShowScreen("pet"); yield return Frames(2); yield return Check("13_pet");
            {
                var petRoot = _app.Current.Root;
                AssertItemFrameBorder(UiKit.Find(petRoot, "Pet:0"), "펫 격자 첫 칸");
                AssertItemFrameBorder(UiKit.Find(petRoot, "Pet:" + (Layout.PetCount - 1)), "펫 격자 마지막 칸");
                AssertCircleBorder(UiKit.Find(petRoot, "Slot:0"), "펫 잠금 슬롯 0"); AssertCircleBorder(UiKit.Find(petRoot, "Slot:1"), "펫 잠금 슬롯 1");
                AssertItemFrameBorder(UiKit.Find(petRoot, "Slot:" + PetScreen.LockedSlots), "펫 빈 장착 슬롯(ItemFrame · Add_1)");
                Assert.IsTrue(UiKit.HasPattern(petRoot), "펫 탭 배경 패턴(T72 ①)");
                Assert.IsTrue(BorderAudit.Exempt.Contains("합계 줄"), "합계 줄은 맨 글자(레퍼런스 13 에 상자 없음) → 감사 예외");
            }
            (_app.Current as PetScreen)?.OpenDetail(0); yield return Check("14_pet_detail");
            {
                var pd = UiKit.Find(_app.Overlay.Root, "PetDetailCell"); AssertItemFrameBorder(pd, "펫 세부 칸");
                var petIcon = UiKit.Find(pd, "Item"); Assert.IsNotNull(petIcon, "세부 칸 아이콘");
                Assert.IsTrue(UiKit.HasLight(petIcon.parent), "펫 세부 아이콘 뒤 빛살(T72 ②)");
            }
            _app.Overlay.Close(); yield return Frames(1);

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
            // T69-gear(strict) — 스탯 3칸 Bordered · 장착 슬롯(변형 프레임 + 빈 칸)·인벤 첫 칸 = ItemFrame Border → Ink(7항) · T72 ① 패턴 배경
            var gearRoot = _app.Current.Root;
            foreach (var n in new[] { "Stat:atk", "Stat:hp", "Stat:sh" }) Assert.IsTrue(UiKit.HasDarkBorder(UiKit.Find(gearRoot, n)), "장비 «" + n + "» 스탯 칸에 어두운 테두리(T69-gear)");
            foreach (var p in D.Gear.Parts) AssertItemFrameBorder(UiKit.Find(gearRoot, "Slot:" + p), "장착 슬롯 " + p);
            var invContent = UiKit.Find(gearRoot, "Content"); Assert.IsNotNull(invContent, "인벤 Content"); Assert.Greater(invContent.childCount, 0, "인벤에 미장착 장비");
            AssertItemFrameBorder(invContent.GetChild(0), "인벤 첫 칸");
            Assert.IsTrue(UiKit.HasPattern(gearRoot), "장비 화면 배경 패턴(T72 ① · T69-gear 가 같이)");
            if (firstFree != null)
            {
                GearUi.OpenDetail(_app, firstFree, null); yield return Check("07_gear_detail");
                var bx = _app.Overlay.Root;
                foreach (var n in new[] { "Pill1", "Pill2", "Stats", "Cost" }) Assert.IsTrue(UiKit.HasDarkBorder(UiKit.Find(bx, n)), "세부 팝업 «" + n + "» 에 어두운 테두리(T69-gear)");
                var opt0 = UiKit.Find(bx, "Opt:0"); if (opt0 != null) Assert.IsTrue(UiKit.HasDarkBorder(opt0), "세부 팝업 옵션 줄 0 에 어두운 테두리(T69-gear)");
                AssertItemFrameBorder(UiKit.Find(bx, "IconSlot"), "세부 팝업 아이콘 칸");
                _app.Overlay.Close(); yield return Frames(1);
                // 빈 슬롯 팝업 — 물건 칸은 ItemFrame_01(7항) · 검은 아웃라인
                S.Eq.Remove(D.Gear.Parts[0]); GearUi.OpenSlot(_app, D.Gear.Parts[0], null); yield return Frames(2);
                var eb = _app.Overlay.Root; Assert.IsNotNull(UiKit.Find(eb, "IconSlot/Empty"), "빈 슬롯 팝업의 아이콘 칸 = ItemFrame_01(Empty)");
                AssertItemFrameBorder(UiKit.Find(eb, "IconSlot"), "빈 슬롯 팝업 아이콘 칸");
                foreach (var n in new[] { "Pill1", "Pill2", "Stats", "Cost" }) Assert.IsTrue(UiKit.HasDarkBorder(UiKit.Find(eb, n)), "빈 슬롯 팝업 «" + n + "» 에 어두운 테두리(T69-gear)");
                _app.Overlay.Close(); yield return Frames(1);
            }
            // 08 대장간(T69-forge · strict) — 같은 키 3개를 만들어 «합성 가능» 칸(초록 프레임 교체)이 하나는 있게 한다: 위 10개 가운데 Parts[0]·rar 0·plus 0 이 이미 2개(i = 0, 6)
            Give(D.Gear.Parts[0], rar: 0, plus: 0);
            _app.ShowScreen("forge"); yield return Frames(2); yield return Check("08_gear_fuse");
            {
                var forgeRoot = _app.Current.Root;
                // 결과 슬롯(초록 프레임 + 모루 · GreenFrame 이 변형을 새로 스폰한 뒤에도 Border 링이 Ink) · 재료 슬롯 첫 칸(빈 칸 Add_1) · 인벤 첫 칸 = 전부 ItemFrame Border → Ink 8px(7항)
                AssertItemFrameBorder(UiKit.Find(forgeRoot, "Result"), "대장간 결과 슬롯(초록 프레임·모루)");
                AssertItemFrameBorder(UiKit.Find(forgeRoot, "Mat0"), "대장간 재료 슬롯 0(빈 칸)");
                var forgeInv = UiKit.Find(forgeRoot, "Content"); Assert.IsNotNull(forgeInv, "대장간 인벤 Content"); Assert.Greater(forgeInv.childCount, 0, "대장간 인벤에 장비");
                AssertItemFrameBorder(forgeInv.GetChild(0), "대장간 인벤 첫 칸");
                Transform fusable = null;
                for (int i = 0; i < forgeInv.childCount; i++) { var c = forgeInv.GetChild(i); if (c.gameObject.activeSelf && c.Find("FuseDot") != null) { fusable = c; break; } }
                Assert.IsNotNull(fusable, "합성 가능 칸(빨간 점 · 같은 키 3개)이 하나는 있어야 한다");
                Assert.IsNotNull(UiKit.Find(fusable, "ui.itemFrame.green"), "합성 가능 칸은 초록 변형 프레임(T39 · UiKit.Spawn 은 카탈로그 키를 이름으로 둔다)");
                AssertItemFrameBorder(fusable, "대장간 합성 가능 칸(초록 프레임)");
                Assert.IsTrue(UiKit.HasPattern(forgeRoot), "대장간 배경 패턴(T72 ① · T69-forge 가 같이)");
            }
            _app.ShowScreen("shop"); yield return Frames(2); yield return Check("10_shop_2");
            (_app.Current as ShopScreen)?.ScrollTo(0f); yield return Check("09_shop_1");
            {
                // T69-shop(09·10 strict) — 상자 카드(대형 배너 1 + 작은 2) · 다이아/골드 상품 카드 = 카드 위에 UiKit.Bordered 한 장(Ink · 8px · 표 % 불변) · «광고/무료 카드 2개» 는 측정용 빈 rect(Exempt · 결정 191) · T72 ① 패턴은 있음만
                var shopRoot = _app.Current.Root; var shopContent = UiKit.Find(shopRoot, "Content"); Assert.IsNotNull(shopContent, "상점 스크롤 Content");
                int boxes = 0;
                for (int i = 0; i < shopContent.childCount; i++) { var c = shopContent.GetChild(i); if (!c.name.StartsWith("Box:")) continue; boxes++; AssertUiBarBorder(shopContent, c.name); }
                Assert.AreEqual(D.Gacha.Boxes.Count, boxes, "상점 상자 카드 수 = gacha.json 상자 수(대형 1 + 작은 2)");
                AssertUiBarBorder(shopContent, "GemPack:0"); AssertUiBarBorder(shopContent, "GoldPack:0");
                Assert.IsTrue(BorderAudit.Exempt.Contains("광고/무료 카드 2개"), "«광고/무료 카드 2개» 는 측정용 담개(레퍼런스 10 에 상자 없음 · 버튼이 각자 외곽선) → 감사 예외");
                Assert.IsTrue(UiKit.HasPattern(shopRoot), "상점 배경 패턴(T72 ① 2단계 2차)");
            }
            _log.AssertNoRed("화면 순회(테두리 감사)");

            // 02 전투 — HUD 바 3개 + 발밑 2단(플레이어) + 첫 웨이브 적 바(8항 · strict)
            _app.StartBattle(1); yield return Frames(3);
            Assert.AreEqual("battle", _app.Current.Name);
            var bs = _app.GetScreen<BattleScreen>(); var G = bs != null ? bs.G : null; Assert.IsNotNull(G, "전투 상태"); var W = bs.World; Assert.IsNotNull(W, "월드");
            foreach (var n in new[] { "Bar:EXP", "Bar:HP", "Bar:SH" }) AssertUiBarBorder(bs.Root, n);
            AssertWorldBarBorder(W.PlayerHpBar, "플레이어 HP 단"); AssertWorldBarBorder(W.PlayerShBar, "플레이어 실드 단");
            foreach (var t in new[] { "stat:" + BattleScreen.StatDefs[0].Key }) Assert.IsTrue(UiKit.HasDarkBorder(UiKit.Find(bs.Root, t)), t + " 스탯 칸 테두리");
            AssertPillBorder(UiKit.Find(bs.Root, "Pill:kills"), "HUD 처치 수 pill"); AssertPillBorder(UiKit.Find(bs.Root, "Pill:gold"), "HUD 골드 pill");
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
