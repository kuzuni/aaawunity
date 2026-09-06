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
                Assert.IsFalse(im.fillCenter, label + " ItemFrame Border 는 가운데 비움(링 위로 올리므로 아이콘을 덮으면 안 된다 · 결정 184)");
                Assert.IsFalse(im.raycastTarget, label + " ItemFrame Border raycast 끔(결정 184)");
                // 결정 184 — «있다»(단언 통과)가 «보인다» 를 보장하지 않는다: 링이 형제 뒤에 있으면 Bg/InnerBorder3/Glow 가 덮어 눈에는 테두리가 없다. 링은 자기 부모의 맨 뒤(맨 위)여야 한다.
                var ringParent = im.transform.parent;
                Assert.IsNotNull(ringParent, label + " ItemFrame Border 의 부모");
                Assert.AreEqual(ringParent.childCount - 1, im.transform.GetSiblingIndex(),
                    label + " ItemFrame Border 링은 형제 맨 뒤(맨 위)여야 눈에 보인다 · 지금 " + im.transform.GetSiblingIndex() + "/" + (ringParent.childCount - 1) + " (부모 " + ringParent.name + " · 결정 184)");
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

        /// <summary>덧댄 링(<see cref="UiKit.Bordered"/>)의 계약(T69-overlay · 스탯 칸·팁 줄) — 직계 «Border» Image · 9-slice · 가운데 비움 · raycast 끔 · Ink(α ≥ 0.8) · 선 ≥ 8px · 사각형 = 칸 rect − 안쪽 여백 2× <paramref name="inset"/>.</summary>
        static void AssertRingBorder(Transform cell, string label, string key, float inset = 0f)
        {
            Assert.IsNotNull(cell, label);
            Transform bt = null; for (int i = 0; i < cell.childCount; i++) if (cell.GetChild(i).name == UiKit.BorderName) { bt = cell.GetChild(i); break; }
            Assert.IsNotNull(bt, label + " 에 «Border» 자식");
            var im = bt.GetComponent<Image>(); Assert.IsNotNull(im, label + " Border 는 Image");
            Assert.IsNotNull(im.sprite, label + " Border 스프라이트"); Assert.IsTrue(im.sprite.name.Contains("Border"), label + " Border 스프라이트 = BasicFrame *Border* (" + im.sprite.name + ")");
            Assert.AreEqual(Image.Type.Sliced, im.type, label + " Border 는 9-slice"); Assert.IsFalse(im.fillCenter, label + " Border 는 가운데 비움"); Assert.IsFalse(im.raycastTarget, label + " Border raycast 끔");
            Assert.GreaterOrEqual(im.color.a, 0.8f, label + " Border 알파 ≥ 0.8"); Assert.IsTrue(UiKit.HasDarkBorder(cell), label + " 은 어두운 테두리");
            float linePx = UiKit.BorderNativePx(key) / im.pixelsPerUnitMultiplier;
            Assert.GreaterOrEqual(linePx, UiKit.BorderPx - 0.01f, label + " 테두리 선 ≥ 8px(폰 3px) · 지금 " + linePx.ToString("0.0"));
            var brt = (RectTransform)bt; var prt = (RectTransform)cell;
            Assert.AreEqual(prt.rect.width - inset * 2f, brt.rect.width, 0.5f, label + " Border 폭 = 칸 폭 − 안쪽 여백");
            Assert.AreEqual(prt.rect.height - inset * 2f, brt.rect.height, 0.5f, label + " Border 높이 = 칸 높이 − 안쪽 여백");
        }

        /// <summary>특전 카드(CardFrame_04 조각)의 테두리 계약(T69-overlay) — 조각 제 «Border» 링(스프라이트 CardFrame_04_White_Border)이 Ink(밝기 ≤ 0.35 · α ≥ 0.8)이고 선이 프레임 8px 이상. 새 Image 를 덧대지 않는다(1항).</summary>
        static void AssertCardBorder(Transform card, string label)
        {
            Assert.IsNotNull(card, label);
            Assert.IsTrue(UiKit.HasDarkBorder(card), label + " 은 어두운 테두리(CardFrame Border → Ink)");
            int seen = 0;
            foreach (var im in card.GetComponentsInChildren<Image>(false))
            {
                if (im == null || !im.enabled || im.name != UiKit.BorderName || im.sprite == null || !im.sprite.name.StartsWith("CardFrame_04_White_Border")) continue;
                seen++;
                Assert.GreaterOrEqual(im.color.a, 0.8f, label + " 카드 Border 알파 ≥ 0.8");
                float lum = 0.299f * im.color.r + 0.587f * im.color.g + 0.114f * im.color.b;
                Assert.LessOrEqual(lum, 0.35f, label + " 카드 Border 는 Ink(밝기 ≤ 0.35 · 지금 " + lum.ToString("0.00") + ")");
                Assert.AreEqual(Image.Type.Sliced, im.type, label + " 카드 Border 는 9-slice");
                float linePx = Overlay.CardBorderNativePx / Mathf.Max(0.01f, im.pixelsPerUnitMultiplier);
                Assert.GreaterOrEqual(linePx, UiKit.BorderPx - 0.05f, label + " 카드 테두리 선 ≥ 8px(폰 3px) · 지금 " + linePx.ToString("0.0"));
            }
            Assert.Greater(seen, 0, label + " 에 켜진 CardFrame_04_White_Border 링이 하나는 있어야 한다(T69 1항)");
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
            // T94 ⓑ(주인 2026-09-07 «메인 로비에 Border 있는 것들은 걍 없애셈») — 로비만 **반대 방향** 단언이다: 검은 링이 없어야 한다.
            // (T96-menu 로 사이드 기둥 둘은 사라졌고 남는 상자는 보조 줄 · 이벤트 · 챕터 카드뿐이다.)
            foreach (var n in new[] { "SubRow", "Events", "ChapterCard" })
                Assert.IsFalse(UiKit.HasDarkBorder(UiKit.Find(lobbyRoot, n)), "로비 «" + n + "» 에는 검은 테두리가 없어야 한다(T94 ⓑ)");
            // 상단 재화 바는 예외 — 주인 07:0X 지시(T106 «탑바를 프레임으로 감싸라»)가 더 최신이라 띠·pill 테두리는 남긴다(결정 기록).
            // pill 은 «TopBar 안» 에서 찾는다 — 로비 프리팹에도 꺼진 ResourceBar_Group 조각이 남아 있고 UiKit.Find 는 꺼진 것도 먼저 집는다(결정 162)
            var topBar = UiKit.Find(lobbyRoot, "TopBar"); Assert.IsNotNull(topBar, "로비 상단 바(TopBar)");
            AssertPillBorder(UiKit.Find(topBar, "ResourceBar_Coin"), "로비 골드 pill");
            AssertPillBorder(UiKit.Find(topBar, "ResourceBar_Gem"), "로비 보석 pill");
            _app.Overlay.Settings(); yield return Check("12_settings");
            {
                // T69-settings(strict) — 줄 3(음악·효과음·언어) = UiKit.Bordered 링(Ink · 8px · 줄 rect 그대로) + 옅은 바탕 · 아이콘은 링 안쪽 · 토글·언어 버튼은 링 위(형제 순서 뒤 · 조각 제 외곽선) · 상자 안 패턴은 UiKit.Popup 이 깐다(T72)
                var ov = _app.Overlay.Root;
                foreach (var n in new[] { "BGM", "SFX", "Language" })
                {
                    AssertUiBarBorder(ov, n);
                    var row = UiKit.Find(ov, n); var rrt = (RectTransform)row;
                    var bgT = UiKit.Find(row, UiKit.BorderName + "Bg"); Assert.IsNotNull(bgT, n + " 줄 바탕(BorderBg)"); Assert.AreEqual(0, bgT.GetSiblingIndex(), n + " 바탕은 맨 뒤");
                    var ic = UiKit.Find(row, "Icon") as RectTransform; Assert.IsNotNull(ic, n + " 아이콘");
                    Assert.GreaterOrEqual(ic.anchorMin.x * rrt.rect.width, UiKit.BorderPx - 0.05f, n + " 아이콘은 링 안쪽(왼쪽 여백 ≥ 8px)");
                    var bt = UiKit.Find(row, UiKit.BorderName);
                    var tg = UiKit.Find(row, "ToggleHost"); if (tg != null && tg.parent == row) Assert.Greater(tg.GetSiblingIndex(), bt.GetSiblingIndex(), n + " 토글은 링 위(형제 순서 뒤)");
                }
                // 토글(Swich_01)·언어 버튼(Button_02)은 조각 그림 자체에 검은 외곽선이 있다(«Border» 오브젝트 없음 · 레퍼런스 12 와 같은 꼴) — 여기서는 «링 위에 있다» 만 본다
                var popBox = UiKit.Find(ov, "BGM").parent; Assert.IsTrue(UiKit.HasPattern(popBox), "설정 팝업 상자 안 패턴(T72 · UiKit.Popup)");
            }
            _app.Overlay.Close(); yield return Frames(1);

            // 11 특권 · 15~19 로비 팝업
            // T78(주인 2026-09-07) — 18_challenge7 · 19_pass 는 화면째 삭제돼 게이트 대상이 아니다
            // 11 특권(T69-lobbypopups · strict) — 레퍼런스 11 은 카드 4장과 카드 «안» 설명 상자가 각자 어두운 외곽선이다(카드 그림은 상자가 없어 담개 = BorderAudit.Exempt)
            _app.ShowScreen("privilege"); yield return Frames(2); yield return Check("11_shop_special");
            {
                var prRoot = _app.Current.Root;
                for (int i = 1; i <= 4; i++) AssertUiBarBorder(prRoot, "Card:" + i);
                for (int i = 2; i <= 4; i++) AssertUiBarBorder(prRoot, "Desc:" + i);
                var pic2t = UiKit.Find(prRoot, "Pic:2"); Assert.IsNotNull(pic2t, "카드 그림(2)");
                Assert.IsFalse(UiKit.HasDarkBorder(pic2t), "카드 그림에는 링을 걸지 않는다(레퍼런스 11 · 그림은 카드 위에 떠 있고 카드가 제 외곽선을 낸다 · 담개)");
            }
            _app.ShowScreen("lobby"); yield return Frames(1);
            // 15 퀘스트(T69-lobbypopups · strict) — 레퍼런스 15 는 아래 탭 3칸이 각자 어두운 외곽선이다(트랙 첫 메달·새로고침 줄은 상자가 없어 담개 = BorderAudit.Exempt)
            LobbyPopups.Quest(_app); yield return Check("15_quest");
            {
                var qov = _app.Overlay.Root;
                for (int i = 0; i < 3; i++) AssertUiBarBorder(qov, "Tab:" + i);
                var qTrack = UiKit.Find(qov, "Track"); Assert.IsNotNull(qTrack, "퀘스트 점수 트랙");
                Assert.IsNotNull(UiKit.Find(qTrack, "TrackScore"), "트랙 첫 칸은 «TrackScore»(담개 이름 · BorderAudit.Exempt 가 이 이름으로 뺀다)");
                for (int i = 1; i < Layout.QsTrackCount; i++) AssertItemFrameBorder(UiKit.Find(qTrack, "Track:" + i), "퀘스트 트랙 보상 칸 " + i);
                var refresh = UiKit.Find(qov, "Refresh"); Assert.IsNotNull(refresh, "새로고침 줄");
                Assert.IsFalse(UiKit.HasDarkBorder(refresh), "새로고침 줄에는 링을 걸지 않는다(레퍼런스 15 에 상자가 없고 글자 칸이 줄 rect 보다 넓어 링이 글자를 가로지른다 · 담개)");
            }
            _app.Overlay.Close(); yield return Frames(1);
            // 16 출석(T69-lobbypopups · strict) — 레퍼런스 16 은 하루 칸 하나가 통째로 외곽선(머리 띠는 그 안의 구역 = 담개)
            LobbyPopups.Attendance(_app); yield return Check("16_attendance");
            {
                var aov = _app.Overlay.Root;
                for (int i = 1; i <= 6; i++) { var day = UiKit.Find(aov, "Day:" + i); Assert.IsNotNull(day, "출석 칸 " + i); Assert.IsTrue(UiKit.HasDarkBorder(day), "출석 칸 " + i + " 은 어두운 테두리(DayFrame 의 UiKit.Bordered)"); }
                var d7 = UiKit.Find(aov, "Day:7"); Assert.IsNotNull(d7, "7일 칸"); Assert.IsTrue(UiKit.HasDarkBorder(d7), "7일 칸은 어두운 테두리");
                var head1 = UiKit.Find(aov, "Day:1/Head"); Assert.IsNotNull(head1, "1일차 머리 띠");
                Assert.IsFalse(UiKit.HasDarkBorder(head1), "머리 띠에는 링을 따로 걸지 않는다(레퍼런스 16 · 칸 하나가 통째로 외곽선 · 담개)");
            }
            _app.Overlay.Close(); yield return Frames(1);
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
            // T69-events(strict) — 던전 카드 2장의 그림 띠 + 하단 던전/PvP 탭 2칸(레퍼런스 20 은 그림과 탭이 각자 검은 외곽선)
            foreach (var c in new[] { "Card:hell", "Card:expedition" })
            {
                var card = UiKit.Find(evRoot, c); Assert.IsNotNull(card, c);
                Assert.IsTrue(UiKit.HasDarkBorder(UiKit.Find(card, "Pic")), c + " 그림 띠에 어두운 테두리(T69-events)");
            }
            foreach (var t in new[] { "Tab:dungeon", "Tab:pvp" }) Assert.IsTrue(UiKit.HasDarkBorder(UiKit.Find(evRoot, t)), t + " 탭 칸에 어두운 테두리(T69-events)");
            // T115 — 던전·아레나의 «물건 칸» 도 공용 GearUi.DarkFrame 을 거쳐 7항·결정 184 계약을 지킨다(전에는 조각 제 갈색 Border 로 «우연히» 통과했다)
            AssertItemFrameBorder(UiKit.Find(UiKit.Find(evRoot, "Card:hell"), "Cell:0"), "던전 카드 보상 칸 1");
            if (Press(UiKit.Find(evRoot, "Card:hell"), "EnterBtn"))
            {
                yield return Check("21_dungeon_detail");
                AssertItemFrameBorder(UiKit.Find(_app.Overlay.Root, "RewardCell:0"), "던전 세부 보상 칸 1");   // T115
                _app.Overlay.Close(); yield return Frames(1);
            }
            ev.ShowPage(EventsScreen.PagePvp); yield return Check("22_arena");
            Assert.IsTrue(UiKit.HasDarkBorder(UiKit.Find(UiKit.Find(evRoot, "Card:arena"), "Pic")), "아레나 카드 그림 띠에 어두운 테두리(T69-events)");
            ev.ShowPage(EventsScreen.PageArena); yield return Check("23_arena_enter");
            // T69-events — 도전 팝업(24)의 상대 5줄과 순위 보상 팝업(25)의 4줄은 «줄 자체» 가 링을 가진다(줄 안 초상·보상 칸의 프레임으로 통과하던 결정 184 함정을 막는다)
            if (Press(evRoot, "ChallengeBtn"))
            {
                yield return Check("24_arena_challenge");
                for (int i = 0; i < 5; i++) AssertUiBarBorder(_app.Overlay.Root, "FoeRow:" + i);
                AssertItemFrameBorder(UiKit.Find(UiKit.Find(_app.Overlay.Root, "FoeRow:0"), "Face"), "도전 상대 줄 초상");   // T115
                _app.Overlay.Close(); yield return Frames(1);
            }
            if (Press(evRoot, "RewardsBtn"))
            {
                yield return Check("25_arena_rank_reward");
                for (int i = 0; i < 4; i++) AssertUiBarBorder(_app.Overlay.Root, "RewardRow:" + i);
                _app.Overlay.Close(); yield return Frames(1);
            }
            ev.ShowPage(EventsScreen.PageMerchant); yield return Check("26_arena_shop");
            AssertItemFrameBorder(UiKit.Find(UiKit.Find(evRoot, "Goods:0"), "IconCell"), "상인 상품 칸 1");   // T115
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
            // 08 대장간(T69-forge · strict) — 같은 키 3개를 만들어 «합성 가능» 칸이 하나는 있게 한다: 위 10개 가운데 Parts[0]·rar 0·plus 0 이 이미 2개(i = 0, 6)
            Give(D.Gear.Parts[0], rar: 0, plus: 0);
            _app.ShowScreen("forge"); yield return Frames(2); yield return Check("08_gear_fuse");
            {
                var forgeRoot = _app.Current.Root;
                // 결과 슬롯(빈 칸 + 모루 아이콘) · 재료 슬롯 첫 칸(빈 칸 Add_1) · 인벤 첫 칸 = 전부 ItemFrame Border → Ink 8px(7항)
                AssertItemFrameBorder(UiKit.Find(forgeRoot, "Result"), "대장간 결과 슬롯");
                AssertItemFrameBorder(UiKit.Find(forgeRoot, "Mat0"), "대장간 재료 슬롯 0(빈 칸)");
                var forgeInv = UiKit.Find(forgeRoot, "Content"); Assert.IsNotNull(forgeInv, "대장간 인벤 Content"); Assert.Greater(forgeInv.childCount, 0, "대장간 인벤에 장비");
                AssertItemFrameBorder(forgeInv.GetChild(0), "대장간 인벤 첫 칸");
                Transform fusable = null;
                for (int i = 0; i < forgeInv.childCount; i++) { var c = forgeInv.GetChild(i); if (c.gameObject.activeSelf && c.Find("FuseDot") != null) { fusable = c; break; } }
                Assert.IsNotNull(fusable, "합성 가능 칸(빨간 점 · 같은 키 3개)이 하나는 있어야 한다");
                // T113 ⓑ — 초록 프레임은 없앴다(주인 «완성됐을 때의 슬롯 부분이 초록인데 그러지 말고 색 통일»).
                // «합성 가능» 은 색이 아니라 빨간 점(FuseDot · 바로 위에서 그것으로 칸을 찾았다)으로 알린다.
                Assert.IsNull(UiKit.Find(fusable, "ui.itemFrame.green"), "합성 가능 칸에 초록 변형 프레임이 남으면 안 된다(T113 ⓑ · 색 통일)");
                AssertItemFrameBorder(fusable, "대장간 합성 가능 칸");
                Assert.IsTrue(UiKit.HasPattern(forgeRoot), "대장간 배경 패턴(T72 ① · T69-forge 가 같이)");

                // T113 ⓐ — 큰 모루 그림(AnvilArt)은 없앴다. 결과 칸 «안» 의 작은 모루 아이콘은 다른 것이라 남는다.
                Assert.IsNull(UiKit.Find(forgeRoot, "AnvilArt"), "대장간 모루 그림(AnvilArt)은 없어야 한다(T113 ⓐ · 주인 «AnvilArt 빼셈»)");
                // T113 ⓒ — 액션바가 장비 화면 갈색 띠(GearScreen.Band)와 같은 자리·크기(표 ⑥ = 표 ③ · 결정 274)
                var forgeBar = UiKit.Find(forgeRoot, "ActionBar"); Assert.IsNotNull(forgeBar, "대장간 액션바");
                Assert.AreEqual(GearScreen.Band.Y, Layout.ForgeActionBar.Y, 0.1f, "액션바 y = 장비 Band y(T113 ⓒ)");
                Assert.AreEqual(GearScreen.Band.H, Layout.ForgeActionBar.H, 0.1f, "액션바 높이 = 장비 Band 높이(T113 ⓒ)");
                Assert.AreEqual(GearScreen.Band.X, Layout.ForgeActionBar.X, 0.1f, "액션바 x = 장비 Band x(T113 ⓒ)");
                Assert.AreEqual(GearScreen.Band.W, Layout.ForgeActionBar.W, 0.1f, "액션바 폭 = 장비 Band 폭(T113 ⓒ)");
            }
            _app.ShowScreen("shop"); yield return Frames(2); yield return Check("10_shop_2");
            (_app.Current as ShopScreen)?.ScrollTo(0f); yield return Check("09_shop_1");
            {
                // T69-shop(09·10 strict) — 상자 카드(대형 배너 1 + 작은 2) · 다이아/골드 상품 카드 = 카드 위에 UiKit.Bordered 한 장(Ink · 8px · 표 % 불변) · «광고/무료 카드 2개» 는 측정용 빈 rect(Exempt · 결정 195) · T72 ① 패턴은 있음만
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

            // 04 레벨업 3택 · 05 보유 특전 · 결과 팝업 2종 (T69-overlay · strict) — 레퍼런스 04 의 «카드마다·스탯 8칸마다 어두운 상자»
            var rngB = new Mulberry32(7u);
            var offer = Perks.Offer(D, G.Taken, false, rngB);
            Assert.Greater(offer.Count, 0, "3택 후보(특전 표)");
            G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
            _app.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick)); yield return Check("04_perks");
            {
                var ov = _app.Overlay.Root;
                var group = UiKit.Find(ov, "Group_Card"); Assert.IsNotNull(group, "3택 카드 묶음(Group_Card)");
                Assert.Greater(group.childCount, 0, "특전 카드가 하나는 있어야 한다");
                for (int i = 0; i < group.childCount; i++) AssertCardBorder(group.GetChild(i), "3택 특전 카드 " + (i + 1));
                var stats = UiKit.Find(ov, "Stats"); Assert.IsNotNull(stats, "상단 스탯 줄(Stats)");
                foreach (var d in BattleScreen.StatDefs)
                {
                    var cellT = UiKit.Find(stats, Overlay.OvStatCellPrefix + d.Key);
                    AssertRingBorder(cellT, "상단 스탯 칸 " + d.Key, UiKit.BorderKeySmall, Overlay.OvStatInset);
                }
            }
            _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
            for (int i = 0; i < offer.Count && i < 3; i++) G.Taken.Add(offer[i]);
            _app.Overlay.PerkBook(G, null); yield return Check("05_perks_list");
            {
                var content = UiKit.Find(_app.Overlay.Root, "Content"); Assert.IsNotNull(content, "보유 특전 목록(Content)");
                Assert.Greater(content.childCount, 0, "보유 특전 카드");
                AssertCardBorder(content.GetChild(0), "보유 특전 첫 카드");
            }
            _app.Overlay.Close(); yield return Frames(1);
            G.Gold = 12750; G.Kills = 137;
            _app.Overlay.Clear(G, false, () => { }, () => { }); yield return Check("res_win");
            var winFrame = UiKit.Find(_app.Overlay.Root, "ItemFrame_01"); Assert.IsNotNull(winFrame, "클리어 보상 칸에 ItemFrame_01 조각(T69 7항)");
            AssertItemFrameBorder(winFrame.parent, "클리어 보상 칸(골드)");
            _app.Overlay.Close(); yield return Frames(1);
            _app.Overlay.Dead(G, () => { }); yield return Check("res_lose");
            {
                var ov = _app.Overlay.Root;
                AssertItemFrameBorder(UiKit.Find(ov, "Reward"), "패배 보상 칸(골드)");
                var list = UiKit.Find(ov, "Group_List"); Assert.IsNotNull(list, "패배 팝업 팁 줄 묶음(Group_List)");
                int tipRows = 0;
                for (int i = 0; i < list.childCount; i++)
                {
                    var row = list.GetChild(i); if (!row.gameObject.activeInHierarchy || UiKit.Find(row, UiKit.BorderName) == null) continue;
                    AssertRingBorder(row, "패배 팁 줄 " + (i + 1), UiKit.BorderKey); tipRows++;
                }
                Assert.Greater(tipRows, 0, "팁 줄에 링이 하나는 있어야 한다(T69 2항)");
            }
            _app.Overlay.Close(); yield return Frames(1);
            _log.AssertNoRed("특전·결과 팝업(테두리)");

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
