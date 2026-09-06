using System;
using System.Collections;
using System.Collections.Generic;
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
    /// T11 «UI 스모크» — 실제 씬(SampleScene · Bootstrap → App)을 올려 **모든 화면(로비·장비·대장간·상점·전투)과 팝업**
    /// (레벨업·보유 특전·쉼터·악마·악마의 선물·천사·광고·사망·클리어·일시정지·설정·탤런트·펫·세부·슬롯·뽑기 결과·토스트)을 하나씩 열고 검사한다.
    /// 화면/팝업마다 ⓐ 예외·에러 로그 0(<see cref="LogAssert.NoUnexpectedReceived"/>) + 프리팹 경로/카탈로그 키 경고(<c>[UiKit]</c>·<c>[AssetCatalog]</c>) 0
    /// ⓑ 데모 프리팹 잔여 글자(«Text»·«Remain»·영문 데모 문구)가 활성 Text 에 없음 ⓒ 핵심 요소 존재(장비 슬롯 6 · 상점 상자 3 · 탭 5 …) ⓓ 전투는 3초 틱 뒤 예외 0.
    /// 배치 모드(CI)는 GameView 를 안 그리므로 HeroView/월드 카메라를 <see cref="Camera.Render"/> 로 직접 돌린다(HeroViewTests 와 같은 방식 · WaitForEndOfFrame 금지).
    /// 주인 상시 지시(2026-09-05) «플레이 콘솔 에러 0» 의 상시 게이트 — 화면 코드를 바꾸면 이 테스트가 그 화면을 열어야 한다(ROUTINE §1·§3).
    /// </summary>
    public class UiSmokeTests
    {
        // ───────────────────────── 데모 잔여 글자 표 (GUI Pro 데모 프리팹 YAML 의 m_text · m_Modifications 에서 뽑음 · T11) ─────────────────────────
        // 이 글자가 «활성» Text 에 그대로 남아 있으면 우리 데이터로 안 바뀐 것. 숫자만인 것(«100»·«999»)과 토글 «ON/OFF»·«START»·«BOSS» 는 의도된 것이라 뺐다.
        static readonly string[] DemoExact =
        {
            "Text", "New Text", "Buff", "Upgrade", "Equip", "Equip All", "Level Up", "Name", "Mission", "Inventory", "AD Skip",
            "English", "Language", "Privacy", "SFX", "BGM", "Hapti", "Setting", "Rate", "Sign In", "Support", "Account Delete",
            "Lv.3", "Lv.7", "Lv.9", "Lv.10", "Lv.15", "Lv.20", "836.99A", "28d 1h", "Wave 5/10", "Battle 1", "Battle 10", "Middle Age", "Whisperwood",
            "Wood Chest", "Limit 5/5", "Gear Stats", "Sword of Courage", "+300%", "+100 HP", "+20 Defense", "+2% Attack Speed", "+5% Critical Chance",
            "Increases Attack Speed", "Choose a Stage Buff", "Stage Buff", "Refresh", "Epic", "Rare", "Magic", "HP Increase", "Food Production Speed",
            "Bring A New Warrior", "Try Upgrading", "Version 1.10", "Terms of Service", "Clear Reward", "Get x2", "Hom", "VICTORY", "Reward", "Talent",
            "Hero", "Battle", "Research", "Shop", "Dungeon",
        };
        static readonly string[] DemoContains = { "Remain", "Layerlab", "Touch to Continue", "Title text", "Toast Message", "Player ID", "Research Artifacts" };

        App _app;
        PlayLog _log;   // 빨간 줄(Error·Exception·Assert) 수집 — LogAssert.NoUnexpectedReceived 는 Debug.Log 도 실패로 보므로 쓰지 않는다(PlayLog 주석)
        readonly List<string> _warn = new List<string>();

        [SetUp] public void SetUp() { _warn.Clear(); _log = new PlayLog(); Application.logMessageReceived += OnLog; }
        [TearDown] public void TearDown() { Application.logMessageReceived -= OnLog; _log?.Dispose(); _log = null; Time.timeScale = 1f; }
        void OnLog(string msg, string stack, LogType type)
        {
            if (type != LogType.Warning || msg == null) return;
            if (msg.StartsWith("[UiKit]") || msg.StartsWith("[AssetCatalog]")) _warn.Add(msg);
        }

        // ───────────────────────── 공통 ─────────────────────────
        /// <summary>새 세이브로 SampleScene 을 올리고 App 이 서기를 기다린다(데이터 로드 포함 · 60초 상한).</summary>
        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I;
            Assert.IsNotNull(_app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");
            _warn.Clear();
            yield return Frames(2);
            _log.AssertNoRed("부팅(Bootstrap → App → 로비)");
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
            _log.AssertNoRed("종료(App·캔버스 파괴 뒤)");
        }
        /// <summary>
        /// n 프레임 — 매 프레임 살아 있는 HeroView 카메라(RenderTexture 타깃)를 강제로 그린다(배치 모드에서도 URP 2D 패스가 돈다).
        /// ⚠ 메인(월드) 카메라는 수동으로 <c>Render()</c> 하지 않는다 — 배치 모드에서 화면 타깃 카메라를 수동 렌더하면 URP 최종 블릿이
        /// «BlitFinalToBackBuffer/Draw UIToolkit/uGUI Overlay: The dimensions … do not match RenderPass specifications (461×578) vs (640×480)» 에러를 스스로 만든다(CI #34 · 도구 오탐).
        /// </summary>
        IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }
        IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return Frames(1); }

        static string PathOf(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
        static bool IsDemo(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (var d in DemoExact) if (s == d) return true;
            foreach (var d in DemoContains) if (s.IndexOf(d, StringComparison.Ordinal) >= 0) return true;
            return false;
        }
        IEnumerable<Text> ActiveTexts() => _app.UiCanvas.GetComponentsInChildren<Text>(false);
        bool HasText(Func<string, bool> pred) { foreach (var t in ActiveTexts()) if (pred(t.text ?? "")) return true; return false; }

        /// <summary>검사 지점 — ⓐ 빨간 줄 0 + 경로/키 경고 0 ⓑ 데모 잔여 글자 0 (+ 팝업 열림 여부).</summary>
        void Check(string where, bool expectOverlay = false, bool demoText = true)
        {
            _log.AssertNoRed(where);
            if (_warn.Count > 0) { var w = string.Join("\n", _warn); _warn.Clear(); Assert.Fail($"[{where}] 프리팹 경로/카탈로그 키 경고 {w.Split('\n').Length}건(잘못된 자식 경로·없는 키 = 빈 그림/글자):\n{w}"); }
            if (demoText)
            {
                var bad = new List<string>();
                foreach (var t in ActiveTexts()) { string s = (t.text ?? "").Trim(); if (IsDemo(s)) bad.Add(PathOf(t.transform) + " :: " + s); }
                if (bad.Count > 0) Assert.Fail($"[{where}] 데모 프리팹 잔여 글자 {bad.Count}건(우리 데이터로 안 바뀜):\n" + string.Join("\n", bad));
            }
            if (expectOverlay) Assert.IsTrue(_app.Overlay.IsOpen, $"[{where}] 팝업이 열려 있어야 한다");
        }

        /// <summary>자식 글자 중 하나가 라벨 조건에 맞는 첫 버튼을 누른다(onClick 직접 호출 · 입력 장치 없이).</summary>
        static bool Click(Transform root, Func<string, bool> label)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(false))
                foreach (var t in b.GetComponentsInChildren<Text>(false))
                    if (label(t.text ?? "")) { b.onClick.Invoke(); return true; }
            return false;
        }
        static bool ClickNamed(Transform root, string name) { var t = UiKit.Find(root, name); var b = t != null ? t.GetComponent<Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        static int CountNamed(Transform root, string prefix) { int n = 0; foreach (var t in root.GetComponentsInChildren<Transform>(false)) if (t.name.StartsWith(prefix)) n++; return n; }

        /// <summary>테스트용 장비 — gear.json 의 부위×종류 표(AllTypes)에서 만든다(뽑기와 같은 규칙 · 등급 0).</summary>
        GearItem Give(string part, int rar = 0, int plus = 0)
        {
            var G = _app.Data.Gear;
            foreach (var t in G.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, rar, plus); _app.Save.Inv.Add(g); return g; }
            Assert.Fail("gear.json 에 부위가 없다: " + part); return null;
        }

        // ───────────────────────── ① 로비 · 설정 · 탤런트/펫 · 토스트 ─────────────────────────
        [UnityTest]
        public IEnumerator LobbySettingsTalentPetToast()
        {
            yield return Boot();
            Assert.AreEqual("lobby", _app.Current.Name);
            var lobby = _app.Current.Root;
            var tabs = UiKit.Find(lobby, "Tab_01_BottomFlushMenu");
            Assert.IsNotNull(tabs, "로비 프리팹(Lobby_Default)의 하단 탭 바 조각이 표 자리(TabBar)에 있어야 한다");
            Assert.GreaterOrEqual(tabs.childCount, NavBar.Keys.Length, "하단 탭 5칸");
            Assert.AreEqual(5, NavBar.Keys.Length, "탭 = 상점·장비·전투·탤런트·펫");
            Assert.GreaterOrEqual(UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length, 1, "로비 초상(HeroView · 상단 바 아바타)");
            Assert.IsTrue(HasText(s => s == "START"), "START 버튼");
            Assert.IsTrue(HasText(s => s.StartsWith("챕터")), "챕터 제목");
            // T34 — 레퍼런스 01_lobby.jpg 구도 단언: 상단 바(아바타·전투력·골드·보석) · 배너+메뉴 · 사이드 3+3 · 카드+◀▶ · 보조 2 · START · 성·이벤트 · 탭 5
            {
                var top = UiKit.Find(lobby, "TopBar"); Assert.IsNotNull(top, "상단 재화 바(TopBar)");
                Assert.IsNotNull(UiKit.Find(top, "Avatar"), "상단 바 아바타 칸"); Assert.IsNotNull(UiKit.Find(top, "Power"), "상단 바 전투력 숫자");
                Assert.IsNotNull(UiKit.Find(top, "ResourceBar_Coin"), "골드 pill"); Assert.IsNotNull(UiKit.Find(top, "ResourceBar_Gem"), "보석 pill");
                Assert.IsNotNull(UiKit.Find(lobby, "Banner"), "이벤트 배너"); Assert.IsNotNull(UiKit.Find(lobby, "Button_Menu"), "메뉴(≡)");
                var sideL = UiKit.Find(lobby, "SideL"); var sideR = UiKit.Find(lobby, "SideR");
                Assert.IsNotNull(sideL, "왼쪽 사이드 기둥"); Assert.IsNotNull(sideR, "오른쪽 사이드 기둥");
                Assert.AreEqual(3, CountNamed(sideL, "Side:"), "왼쪽 사이드 아이콘 3"); Assert.AreEqual(3, CountNamed(sideR, "Side:"), "오른쪽 사이드 아이콘 3");
                Assert.IsNotNull(UiKit.Find(lobby, "ChapterCard"), "챕터 카드"); Assert.IsNotNull(UiKit.Find(lobby, "ArrowL"), "◀"); Assert.IsNotNull(UiKit.Find(lobby, "ArrowR"), "▶");
                Assert.AreEqual(2, CountNamed(UiKit.Find(lobby, "SubRow"), "Side:"), "보조 버튼 2(탐험·클리어 보상)");
                Assert.IsNotNull(UiKit.Find(lobby, "Castle"), "왼쪽 아래 성"); Assert.IsNotNull(UiKit.Find(lobby, "Events"), "오른쪽 아래 이벤트");
                Assert.IsTrue(HasText(s => s == "스타터팩") && HasText(s => s == "퀘스트") && HasText(s => s == "탐험"), "사이드·보조 라벨은 우리말");
                // 배치 = 표 ①(±3%p) — START 는 카드와 같은 x·폭, 탭 바는 맨 아래
                var frame = _app.Frame; var start = (RectTransform)UiKit.Find(lobby, "Start"); var card = (RectTransform)UiKit.Find(lobby, "ChapterCard");
                Assert.AreEqual(Layout.LobbyStart.X, start.anchorMin.x * 100f, 0.5f, "START x"); Assert.AreEqual(Layout.LobbyCard.X + Layout.LobbyCard.W, card.anchorMax.x * 100f, 0.5f, "카드 오른쪽 = START 오른쪽");
                Assert.AreEqual(start.anchorMin.x, card.anchorMin.x, 1e-3f, "START 와 카드는 같은 x"); Assert.AreEqual(start.anchorMax.x, card.anchorMax.x, 1e-3f, "START 와 카드는 같은 폭");
                Assert.AreEqual(1f - Layout.TabBar.Y / 100f, ((RectTransform)tabs).anchorMax.y, 1e-3f, "탭 바 = 표 자리");
                // 사이드 아이콘·보조 버튼·배너는 눌러도 아무 일 없음(껍데기 · 팝업 안 열림 · 빨간 줄 0)
                foreach (var key in new[] { LobbyScreen.SideStarter, LobbyScreen.SideQuest, LobbyScreen.SideExplore, LobbyScreen.SideEvents }) Assert.IsTrue(ClickNamed(lobby, "Side:" + key), "껍데기 버튼 " + key);
                Assert.IsTrue(ClickNamed(lobby, "Banner"), "배너"); yield return Frames(1);
                Assert.IsFalse(_app.Overlay.IsOpen, "껍데기 버튼은 팝업을 열지 않는다(T43·T44 전)");
            }
            Check("로비");

            // 챕터 ◀▶ (최고 챕터 1 이라 그대로) · 탭 라벨
            Assert.IsTrue(ClickNamed(lobby, "ArrowR"), "챕터 ▶"); Assert.IsTrue(ClickNamed(lobby, "ArrowL"), "챕터 ◀"); yield return Frames(1);
            Check("로비 챕터 이동");

            // 설정 (Settings 프리팹 그대로) — 배경음 스위치 · 닫기
            _app.Overlay.Settings(); yield return Frames(2);
            Check("설정 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s == "배경음"), "설정: 배경음 줄"); Assert.IsTrue(HasText(s => s == "설정"), "설정: 제목");
            var sw = UiKit.Find(_app.Overlay.Root, "BGM"); if (sw != null) { ClickNamed(sw, "Swich_01"); yield return Frames(1); Assert.IsTrue(_app.Save.Muted, "배경음 스위치 = Save.Muted"); ClickNamed(sw, "Swich_01"); yield return Frames(1); }
            Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Button_Close_01"), "설정 닫기(X)"); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "설정이 닫혀야 한다"); Check("설정 닫힘");

            // 탤런트 · 펫 (Character_Talent_02 통째로 · 데모 내용 그대로라 잔여 글자 검사는 뺀다)
            foreach (var kind in new[] { "talent", "pet" })
            {
                _app.Overlay.TalentPet(kind); yield return Frames(2);
                Check("팝업 " + kind, expectOverlay: true, demoText: false);
                string label = NavBar.Labels[Array.IndexOf(NavBar.Keys, kind)];
                Assert.IsTrue(HasText(s => s == label), $"{kind} 팝업의 켜진 탭 라벨 = {label}");
                Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.talent"), "Character_Talent_02(ui.talent) 프리팹이 팝업 층에 있어야 한다");
                _app.Overlay.Close(); yield return Frames(1);
            }
            Check("탤런트/펫 닫힘");

            // 토스트
            _app.Toast("스모크 테스트"); yield return Frames(2);
            Assert.IsTrue(HasText(s => s == "스모크 테스트"), "토스트 글자");
            Check("토스트");
            yield return Shutdown();
        }

        // ───────────────────────── ② 장비 화면 · 세부 팝업 · 슬롯 팝업 · 장착 외형 ─────────────────────────
        [UnityTest]
        public IEnumerator GearScreenDetailSlotAndEquip()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            var items = new List<GearItem>();
            foreach (var p in D.Gear.Parts) items.Add(Give(p, rar: 1));
            S.Gold = 100000;
            _app.ShowScreen("gear"); yield return Frames(2);
            Assert.AreEqual("gear", _app.Current.Name);
            var gear = _app.Current.Root;
            var slots = UiKit.Find(gear, "Group_Slot");
            Assert.IsNotNull(slots, "Character_Hero_Equipment 의 Group_Slot"); Assert.GreaterOrEqual(slots.childCount, 6, "장착 슬롯 6칸");
            var bar = UiKit.Find(gear, "ui.tabBar"); Assert.IsNotNull(bar, "장비 화면 탭 바"); Assert.GreaterOrEqual(bar.childCount, 5, "탭 5");
            var content = UiKit.Find(gear, "Content"); Assert.IsNotNull(content, "인벤 Content");
            Assert.AreEqual(items.Count, CountNamed(content, "gear:"), "인벤 칸 = 장비 수(장착 없음)");
            Assert.IsTrue(HasText(s => s == "장비"), "제목 «장비»");
            Check("장비 화면");

            // 세부 팝업(미장착) → 장착
            var g0 = items[0];
            GearUi.OpenDetail(_app, g0, _app.Current.Refresh); yield return Frames(2);
            Check("장비 세부 팝업", expectOverlay: true);
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.itemDetail"), "세부 팝업 = Character_Hero_Item_Detail_01(ui.itemDetail)");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "장착"), "«장착» 버튼"); yield return Frames(2);
            Assert.IsTrue(S.IsEquipped(g0), "장착됐어야 한다"); Assert.IsFalse(_app.Overlay.IsOpen);
            Assert.AreEqual(items.Count - 1, CountNamed(content, "gear:"), "장착한 장비는 인벤 리스트에서 숨긴다");
            Check("장착 뒤 장비 화면");

            // 세부 팝업(장착중) — 해제/슬롯 강화 · 닫기(X)
            GearUi.OpenDetail(_app, g0, _app.Current.Refresh); yield return Frames(2);
            Check("장비 세부 팝업(장착중)", expectOverlay: true);
            Assert.IsTrue(HasText(s => s == "해제"), "«해제» 버튼"); Assert.IsTrue(HasText(s => s.StartsWith("슬롯 강화") || s == "슬롯 MAX"), "슬롯 강화 버튼");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "슬롯 강화"), "슬롯 강화 클릭"); yield return Frames(2);
            Assert.AreEqual(1, S.SlotLv(g0.Part), "슬롯 Lv 0 → 1"); Check("슬롯 강화 뒤(팝업 다시 열림)", expectOverlay: true);
            Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Button_Close_01"), "세부 팝업 닫기(X)"); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen);

            // 투구·무기·갑옷 전부 장착 → 외형(GearLook) 반영 · 슬롯 아이콘
            foreach (var g in items) S.Eq[g.Part] = g.Uid;
            _app.ShowScreen("gear"); yield return Frames(3);
            Assert.AreEqual(0, CountNamed(content, "gear:"), "전부 장착 → 인벤 리스트 비어야 한다");
            Assert.IsTrue(HasText(s => s.StartsWith("장착하지 않은")), "빈 인벤 안내");
            Check("전부 장착 뒤 장비 화면(외형 반영)");
            // T17 — 슬롯 6칸의 아이콘: 파츠(투구·무기·갑옷)는 불투명 그림이 칸의 72%(±3%p) · 회전 0(주인이 무기 45° 취소) · GUI Pro 아이콘(목걸이·장갑·신발)은 프리팹 Item 그대로
            {
                var slotGrp = UiKit.Find(_app.Current.Root, "Group_Slot"); Assert.IsNotNull(slotGrp, "Group_Slot");   // 바깥 범위의 slots 와 이름이 겹치면 CS0136(CI #41)
                int parts = 0, guis = 0;
                for (int i = 0; i < slotGrp.childCount && i < 6; i++)
                {
                    var frame = UiKit.Find(slotGrp.GetChild(i), "ItemFrame_01"); var item = frame != null ? UiKit.Find(frame, "Item") : null;
                    Assert.IsNotNull(item, "슬롯 " + i + " Item"); Assert.IsTrue(item.gameObject.activeSelf, "슬롯 " + i + " 아이콘 켜짐(전부 장착)");
                    var im = item.GetComponent<Image>(); var rt = (RectTransform)item; var fr = (RectTransform)frame;
                    Assert.IsNotNull(im.sprite, "슬롯 " + i + " 스프라이트");
                    string part = i < 3 ? GearUi.ColLeft[i] : GearUi.ColRight[i - 3];
                    float rot = Mathf.DeltaAngle(0f, rt.localEulerAngles.z);
                    if (GearLook.HasLook(part))
                    {
                        parts++;
                        // 그림 bbox(정점) 의 긴 변 × (Item 크기/rect) × localScale = 칸 한 변 × 0.72
                        var sp = im.sprite; float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                        foreach (var v in sp.vertices) { float px = v.x * sp.pixelsPerUnit + sp.pivot.x, py = v.y * sp.pixelsPerUnit + sp.pivot.y; x0 = Mathf.Min(x0, px); y0 = Mathf.Min(y0, py); x1 = Mathf.Max(x1, px); y1 = Mathf.Max(y1, py); }
                        float shown = Mathf.Max(x1 - x0, y1 - y0) * (rt.sizeDelta.x / sp.rect.width) * rt.localScale.x;
                        float cell = Mathf.Min(fr.rect.width, fr.rect.height);
                        Assert.AreEqual((float)GearLook.PartIconFill, shown / cell, 0.03f, $"슬롯 {i}({part}) 파츠 아이콘 그림 크기 = 칸의 72% (그림 {shown:0}px / 칸 {cell:0}px)");
                        Assert.IsTrue(shown <= cell, $"슬롯 {i}({part}) 아이콘이 칸을 넘지 않는다");
                        Assert.AreEqual(0f, rot, 0.5f, $"슬롯 {i}({part}) 회전 0(무기 45° 취소)");
                        Assert.IsTrue(im.preserveAspect, "preserveAspect");
                    }
                    else { guis++; Assert.AreEqual(0f, rot, 0.5f, $"슬롯 {i}({part}) GUI Pro 아이콘은 회전 없음"); Assert.AreEqual(new Vector2(0.5f, 0.5f), rt.pivot, $"슬롯 {i}({part}) GUI Pro 아이콘은 프리팹 pivot"); }
                }
                Assert.AreEqual(3, parts, "파츠 아이콘 3(투구·무기·갑옷)"); Assert.AreEqual(3, guis, "GUI Pro 아이콘 3(목걸이·장갑·신발)");
            }
            Check("슬롯 아이콘 크기(T17)");

            // 빈 슬롯 팝업 — 해제 뒤 그 부위
            S.Eq.Remove(items[1].Part);
            GearUi.OpenSlot(_app, items[1].Part, _app.Current.Refresh); yield return Frames(2);
            Check("슬롯 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s.EndsWith("슬롯")), "슬롯 팝업 제목");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "닫기"), "슬롯 팝업 닫기"); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen); Check("슬롯 팝업 닫힘");
            yield return Shutdown();
        }

        // ───────────────────────── ③ 대장간 — 인벤 전부 · 빨간 점 · 재료 3개 → 합성 ─────────────────────────
        [UnityTest]
        public IEnumerator ForgeShowsAllAndFuses()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            var a = Give(D.Gear.Parts[0]); var b = Give(D.Gear.Parts[0]); var c = Give(D.Gear.Parts[0]);
            var other = Give(D.Gear.Parts[1], rar: 1); S.Eq[other.Part] = other.Uid;   // 장착분도 대장간엔 보인다(체크 + 흐림)
            Assert.AreEqual(GearUi.Key(a), GearUi.Key(b)); Assert.AreEqual(GearUi.Key(a), GearUi.Key(c));
            _app.ShowScreen("gear"); yield return Frames(1);
            _app.ShowScreen("forge"); yield return Frames(2);
            Assert.AreEqual("forge", _app.Current.Name);
            var forge = _app.Current.Root; var content = UiKit.Find(forge, "Content"); Assert.IsNotNull(content, "대장간 인벤 Content");
            Assert.AreEqual(S.Inv.Count, CountNamed(content, "gear:"), "대장간 인벤에 장비가 전부(장착분 포함) 보여야 한다");
            Assert.GreaterOrEqual(CountNamed(content, "FuseDot"), 3, "합성 가능한 칸의 빨간 점(같은 키 3개)");
            Assert.IsTrue(HasText(s => s == "대장간"), "제목");
            Check("대장간");

            foreach (var g in new[] { a, b, c }) { Assert.IsTrue(ClickNamed(content, "gear:" + g.Uid), "재료 칸 클릭 " + g.Uid); yield return Frames(1); }
            Assert.IsTrue(HasText(s => s == "합성 (3/3)"), "재료 3개 고르면 «합성 (3/3)»");
            Check("재료 3개 선택");
            int before = S.Inv.Count;
            Assert.IsTrue(Click(forge, s => s == "합성 (3/3)"), "합성 버튼"); yield return Frames(2);
            Assert.AreEqual(before - 2, S.Inv.Count, "3개 → 1개");
            Assert.AreEqual(1, S.Fuses);
            Check("합성 뒤");
            Assert.IsTrue(Click(forge, s => s == "자동"), "자동 버튼(조합 없음 → 토스트)"); yield return Frames(2);
            Check("자동 합성(조합 없음)");
            Assert.IsTrue(Click(forge, s => s == "← 장비"), "뒤로"); yield return Frames(2);
            Assert.AreEqual("gear", _app.Current.Name); Check("대장간 → 장비");
            yield return Shutdown();
        }

        // ───────────────────────── ④ 상점 — 상자 3종 · 뽑기 → Shop_Chest_Open ─────────────────────────
        [UnityTest]
        public IEnumerator ShopBoxesAndChestOpenPopup()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            S.Gem = 1000000;
            _app.ShowScreen("shop"); yield return Frames(2);
            Assert.AreEqual("shop", _app.Current.Name);
            var shop = _app.Current.Root;
            Assert.AreEqual(3, D.Gacha.Boxes.Count, "gacha.json 상자 3종");
            foreach (var box in D.Gacha.Boxes) Assert.IsTrue(HasText(s => s.Contains(box.Name)), "상점에 상자 이름이 보여야 한다: " + box.Name);
            var bar = UiKit.Find(shop, "ui.tabBar"); if (bar == null) bar = UiKit.Find(shop, "Tab_01_BottomFlushMenu");   // T9 가 Shop_List 프리팹의 탭 바를 쓰면 뒤쪽 이름
            Assert.IsNotNull(bar, "상점 탭 바"); Assert.GreaterOrEqual(bar.childCount, 5, "탭 5");
            Check("상점");

            int inv = S.Inv.Count;
            Assert.IsTrue(Click(shop, s => s.Contains("1회")), "«1회» 뽑기 버튼"); yield return Frames(2);
            Check("뽑기 결과 팝업(1회)", expectOverlay: true);
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.chestOpen"), "뽑기 결과 = Shop_Chest_Open(ui.chestOpen) 그대로 — 주인: «하라 했는데 안 했네»");
            Assert.GreaterOrEqual(S.Inv.Count, inv + 1, "뽑은 장비가 인벤에 담겨야 한다");
            Assert.AreEqual(CountNamed(_app.Overlay.Root, "gear:"), S.Inv.Count - inv, "결과 팝업의 장비 칸 수 = 얻은 수");
            _app.Overlay.Close(); yield return Frames(1);
            inv = S.Inv.Count;
            Assert.IsTrue(Click(shop, s => s.Contains("10회")), "«10회» 뽑기 버튼"); yield return Frames(2);
            Check("뽑기 결과 팝업(10회)", expectOverlay: true);
            Assert.GreaterOrEqual(S.Inv.Count, inv + D.Gacha.TenPullCount, "10회 = 10개 이상");
            _app.Overlay.Close(); yield return Frames(1);
            Check("상점(뽑기 뒤)");
            yield return Shutdown();
        }

        // ───────────────────────── ⑤ 전투 3초 + 전투 팝업 전부 ─────────────────────────
        [UnityTest]
        public IEnumerator BattleTicksAndAllBattlePopups()
        {
            yield return Boot();
            var D = _app.Data;
            _app.StartBattle(1);
            yield return RealSeconds(3f);
            Assert.AreEqual("battle", _app.Current.Name);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            Assert.Greater(G.T, 0, "3초 동안 엔진 시간이 흘러야 한다(팝업이 안 떠 있는 한)");
            Assert.IsTrue(HasText(s => s.StartsWith("챕터")), "HUD 챕터 제목"); Assert.IsFalse(HasText(s => s.StartsWith("웨이브")), "HUD 웨이브 수 표시는 없다(T33 주인 지시)");
            // T35 — 레퍼런스 02/03 구도 단언: pill 2 · 메뉴(≡) · 진행바 · 배속 · 펫 둥근 버튼 · 바 3개 한 줄(EXP 라벨) · 스탯 8칸 · 📘 · 특전 줄 (세부 = HudBarsTests)
            {
                var hud = bs.Root;
                Assert.IsNotNull(UiKit.Find(hud, "Pill:kills"), "상단 왼쪽 pill(처치 수)"); Assert.IsNotNull(UiKit.Find(hud, "Pill:gold"), "상단 왼쪽 pill(골드)");
                Assert.IsNotNull(UiKit.Find(hud, "Button_Menu"), "상단 오른쪽 메뉴(≡)"); Assert.IsNotNull(UiKit.Find(hud, "Bar:Progress"), "챕터 진행바");
                Assert.IsNotNull(UiKit.Find(hud, "SpeedBtn"), "왼쪽 아래 배속"); Assert.IsNotNull(UiKit.Find(hud, "PetBtn"), "오른쪽 아래 펫 둥근 버튼(껍데기)");
                Assert.IsNotNull(UiKit.Find(hud, "Bar:EXP"), "EXP 바"); Assert.IsNotNull(UiKit.Find(hud, "Bar:HP"), "HP 바"); Assert.IsNotNull(UiKit.Find(hud, "Bar:SH"), "실드 바");
                Assert.IsTrue(HasText(s => s == "EXP"), "EXP 초록 라벨");
                Assert.AreEqual(BattleScreen.StatDefs.Length, CountNamed(hud, "stat:"), "스탯 8칸(2열×4행)");
                Assert.IsNotNull(UiKit.Find(hud, "PerkBook"), "📘 보유 특전"); Assert.IsNotNull(UiKit.Find(hud, "PerkStrip"), "특전 미리보기 줄");
                Assert.IsTrue(ClickNamed(hud, "PetBtn"), "펫 버튼 클릭"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen, "펫 버튼은 껍데기 — 팝업 안 열림");
            }
            Check("전투 3초");
            // 팝업 검사 동안 엔진을 멈춘다(Time.deltaTime = 0 → 틱 없음 · 팝업 카운트다운은 unscaled) — 엔진이 스스로 띄우는 레벨업과 섞이지 않게
            Time.timeScale = 0f; _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
            var rng = new Mulberry32(7u);

            // 레벨업 3택 (Play_Perk_Selection_02) → 첫 카드 선택
            var offer = Perks.Offer(D, G.Taken, false, rng); Assert.Greater(offer.Count, 0, "특전 제안");
            G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
            _app.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick)); yield return Frames(2);
            Check("레벨업 팝업", expectOverlay: true);
            var cards = UiKit.Find(_app.Overlay.Root, "Group_Card"); Assert.IsNotNull(cards, "Group_Card"); Assert.AreEqual(offer.Count, cards.childCount, "카드 수 = 제안 수");
            Assert.IsTrue(HasText(s => s == "레벨 업!"), "제목");
            var first = cards.GetChild(0).GetComponent<Button>(); Assert.IsNotNull(first, "카드는 클릭 가능"); first.onClick.Invoke(); yield return Frames(3);
            Assert.AreEqual(1, G.Taken.Count, "특전 1개 획득"); Assert.IsFalse(_app.Overlay.IsOpen);
            G.Pending = null;   // 엔진이 3초 동안 쌓아 둔 레벨업이 이어서 열렸을 수 있다 — 여기서는 팝업 하나씩만 본다
            Check("특전 선택 뒤(HUD 특전 줄 갱신)");

            // 보유 특전
            _app.Overlay.PerkBook(G, null); yield return Frames(2);
            Check("보유 특전 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s.StartsWith("이번 원정에서 얻은 특전")), "보유 특전 부제");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "닫기"), "닫기"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen);

            // 쉼터
            G.Pending = new PendingDecision { Kind = PendingKind.Rest };
            _app.Overlay.Rest(G, heal => G.ResolveRest(heal)); yield return Frames(2);
            Check("쉼터 팝업", expectOverlay: true);
            Assert.IsTrue(Click(_app.Overlay.Root, s => s.StartsWith("경험치")), "경험치 선택"); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen); G.Pending = null;

            // 악마의 거래 → 거절 · 악마의 선물
            var dp = Perks.OfferDevil(D, G.Taken, rng);
            G.Pending = new PendingDecision { Kind = PendingKind.Devil, DevilPerk = dp };
            _app.Overlay.Devil(G, accept => G.ResolveDevil(accept)); yield return Frames(2);
            Check("악마 팝업", expectOverlay: true);
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "거절"), "거절"); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen); G.Pending = null;
            _app.Overlay.DevilGift(dp, null); yield return Frames(2);
            Check("악마의 선물 팝업", expectOverlay: true);
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "계속"), "계속"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen);

            // 천사 → 무료 축복 · 광고 카운트다운(1초)
            G.Pending = new PendingDecision { Kind = PendingKind.Angel };
            _app.Overlay.Angel(G, m => G.ResolveAngel(m)); yield return Frames(2);
            Check("천사 팝업", expectOverlay: true);
            Assert.IsTrue(Click(_app.Overlay.Root, s => s.StartsWith("무료 축복")), "무료 축복"); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen); G.Pending = null;
            bool adDone = false; _app.Overlay.AdCountdown(1, () => adDone = true); yield return Frames(2);
            Check("광고 카운트다운 팝업", expectOverlay: true);
            float t0 = Time.realtimeSinceStartup; while (!adDone && Time.realtimeSinceStartup - t0 < 5f) yield return Frames(1);
            Assert.IsTrue(adDone, "카운트다운이 끝나야 한다"); _app.Overlay.Close(); yield return Frames(1);

            // 일시정지(Settings 프리팹) → 재개
            _app.Overlay.Pause(() => { }, () => { }); yield return Frames(2);
            Check("일시정지 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s == "일시정지"), "제목"); Assert.IsTrue(HasText(s => s == "포기하고 로비로"), "포기 버튼");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "재개"), "재개"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen);

            // 클리어(Play_Result_Win_01) → 로비로 · 사망(Play_Result_Lose) → 로비로 (콜백은 빈 것 — 화면 전환은 아래서)
            _app.Overlay.Clear(G, false, () => { }, () => { }); yield return Frames(2);
            Check("클리어 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s == "클리어!"), "제목"); Assert.IsTrue(HasText(s => s == "광고 보고 보상 ×2 받기"), "광고 ×2 버튼(프리팹 Get x2 자리 · T23)");
            Assert.IsFalse(HasText(s => s == "다음 챕터"), "«다음 챕터» 버튼은 없다(T23 · 로비의 챕터 화살표로)");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "그냥 받기"), "그냥 받기(프리팹 Home 자리)"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen);
            _app.Overlay.Dead(G, () => { }); yield return Frames(2);
            Check("사망 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s == "쓰러졌다..."), "제목");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "로비로"), "로비로"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen);
            Check("전투 팝업 전부 닫힘");

            // 엔진 재개 0.5초 → 전투 이탈 → 로비 (월드 해제)
            Time.timeScale = 1f; yield return RealSeconds(0.5f);
            _app.ShowScreen("lobby"); yield return Frames(3);
            Assert.AreEqual("lobby", _app.Current.Name);
            Check("전투 → 로비");
            yield return Shutdown();
        }
    }
}
