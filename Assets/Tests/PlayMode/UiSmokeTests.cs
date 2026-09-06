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
        /// <summary>T63 화면 단위 «잘림 0» 계약 — root 아래 활성 Text 를 <see cref="TextAudit.Collect"/> 로 판정해 잘림/하한 미달/bestFit 미달이 하나도 없어야 한다(전체 게이트 TextSizeGateTests 는 아직 strict 가 아니라 화면 작업자가 자기 화면을 여기서 잠근다). skipPath 가 든 경로(다른 하위 행 몫)는 제외.</summary>
        static void AssertNoTextClip(string where, Transform root, string skipPath = null)
        {
            Canvas.ForceUpdateCanvases();
            var bad = new List<string>();
            foreach (var r in TextAudit.Collect(where, root))
            {
                if (skipPath != null && r.Path.IndexOf(skipPath, StringComparison.Ordinal) >= 0) continue;
                if (r.Clipped || r.FloorBad || r.BestFitBad) bad.Add(r.ToString());
            }
            Assert.AreEqual(0, bad.Count, $"[{where}] 글자 잘림/넘침·하한 미달(T63 · 화면 잘림 0):\n" + string.Join("\n", bad));
        }
        /// <summary>살아 있는 shine 머티리얼 인스턴스 수(T61 · 카드가 파괴되면 0 이어야 한다 — 에셋 «PerkShine» 자체는 이름이 달라 안 센다).</summary>
        static int CountShineInstances() { int n = 0; foreach (var m in Resources.FindObjectsOfTypeAll<Material>()) if (m != null && m.name == "PerkShine (Instance)") n++; return n; }

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
            Assert.AreEqual(5, NavBar.Keys.Length, "탭 = 상점·장비·전투·던전·펫"); Assert.AreEqual("dungeon", NavBar.Keys[3], "넷째 탭 = 던전(T43 · 탤런트 대체)"); Assert.AreEqual("던전", NavBar.Labels[3], "넷째 탭 라벨");
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
                // T68 ④ 카드 = 프리팹 SampleImage_Map 그림(활성 · 카드 자리 밑 · 스프라이트 있음) · 코드 조립 카드(Stage/Field/Road/Prop) 없음
                {
                    var map = UiKit.Find(lobby, "SampleImage_Map"); Assert.IsNotNull(map, "챕터 카드 그림 = SampleImage_Map"); Assert.IsTrue(map.gameObject.activeInHierarchy, "SampleImage_Map 활성");
                    Assert.AreEqual("ChapterCard", map.parent.name, "SampleImage_Map 은 카드 자리 밑"); Assert.IsNotNull(map.GetComponent<Image>().sprite, "SampleImage_Map 스프라이트");
                    Assert.IsNull(UiKit.Find(UiKit.Find(lobby, "ChapterCard"), "Stage"), "코드 조립 카드(Stage) 폐기");
                }
                // T68 ③ 배경 Deco(흐린 칼 무늬) 전부 비활성 · T68 ② 상단 초상은 정지(Animator 속도 0)
                {
                    var bg = UiKit.Find(lobby, "Background"); Assert.IsNotNull(bg, "배경"); int decoOn = 0;
                    for (int i = 0; i < bg.childCount; i++) if (bg.GetChild(i).name.StartsWith("Deco") && bg.GetChild(i).gameObject.activeSelf) decoOn++;
                    Assert.AreEqual(0, decoOn, "배경 Deco 는 전부 꺼진다(T68 ③)");
                    var hv = UiKit.Find(lobby, "TopBar").GetComponentInChildren<HeroView>(true); Assert.IsNotNull(hv, "상단 초상 HeroView");
                    Assert.IsTrue(hv.Still, "상단 초상 = 정지(T68 ②)"); Assert.AreEqual(0f, hv.Rig.AnimSpeed, 1e-3f, "상단 초상 Animator 속도 0");
                }
                Assert.AreEqual(2, CountNamed(UiKit.Find(lobby, "SubRow"), "Side:"), "보조 버튼 2(탐험·클리어 보상)");
                Assert.IsNotNull(UiKit.Find(lobby, "Castle"), "왼쪽 아래 성"); Assert.IsNotNull(UiKit.Find(lobby, "Events"), "오른쪽 아래 이벤트");
                Assert.IsTrue(HasText(s => s == "스타터팩") && HasText(s => s == "퀘스트") && HasText(s => s == "탐험"), "사이드·보조 라벨은 우리말");
                // T63-lobby — 아이콘 라벨 11개(사이드 6 · 보조 2 · 성 · 이벤트)는 본문 하한(40)으로 2줄까지 잘림 없이: bestFit 이 줄이지 않고(TextGenerator 로 직접 굴려 40) · 선호 높이 ≤ 칸 · «시즌 패스» 도 같음
                {
                    int captions = 0;
                    foreach (var t in lobby.GetComponentsInChildren<Text>(false))
                    {
                        if (t.transform.parent == null || !t.transform.parent.name.StartsWith("Side:")) continue;
                        captions++;
                        // T68 ①: 라벨은 보조 하한(36 · ROUTINE T68 1항) — 아이콘이 칸 폭 75% 를 차지해야 하므로 본문 40 두 줄은 칸에 안 들어간다(결정 128)
                        Assert.AreEqual(TextSize.Aux, t.fontSize, $"라벨 «{t.text}» 크기 = 보조 하한"); Assert.AreEqual(TextKind.Aux, TextAudit.KindOf(t), $"라벨 «{t.text}» 종류 = Aux");
                        var gs = t.GetGenerationSettings(t.rectTransform.rect.size); gs.scaleFactor = 1f;   // 캔버스 배율을 빼고 글자 단위로(fontSizeUsedForBestFit 은 scaleFactor 가 곱해진 값)
                        var gen = new TextGenerator(); gen.Populate(t.text, gs);
                        Assert.GreaterOrEqual(gen.fontSizeUsedForBestFit, TextSize.Aux, $"라벨 «{t.text}» 가 칸({t.rectTransform.rect.width:0}×{t.rectTransform.rect.height:0})에 36 으로 안 들어가 bestFit 이 줄였다");
                        Assert.LessOrEqual(gen.lineCount, 2, $"라벨 «{t.text}» 는 2줄까지");
                        // T68 ① 아이콘 = 칸 폭의 ≥ 75%(주인 «아이콘 너무 작음» · 1.5~1.8배)
                        var cell = (RectTransform)t.transform.parent; var icon = (RectTransform)UiKit.Find(cell, "Icon"); Assert.IsNotNull(icon, $"칸 {cell.name} 아이콘");
                        Assert.GreaterOrEqual(icon.rect.width, cell.rect.width * LobbyScreen.CaptionIconMinW - 1f, $"칸 {cell.name} 아이콘 폭 ≥ 칸 폭 75%");
                    }
                    // T67(CI #98 빨강 후속): «Side:*» 칸은 사이드 6 + 보조 2 + 성 + 이벤트 = 10 — 배너 «시즌 패스» 는 Banner 밑이라 따로 본다
                    Assert.AreEqual(10, captions, "아이콘 라벨 = 사이드 6 + 보조 2 + 성 + 이벤트");
                    Text passLb = null;
                    foreach (var t in UiKit.Find(lobby, "Banner").GetComponentsInChildren<Text>(false)) if (t.text == "시즌 패스") passLb = t;
                    Assert.IsNotNull(passLb, "배너 «시즌 패스» 라벨"); Assert.AreEqual(TextSize.Body, passLb.fontSize, "배너 «시즌 패스» 크기 = 본문 하한");
                }
                // 배치 = 표 ①(±3%p) — START 는 카드와 같은 x·폭, 탭 바는 맨 아래
                var frame = _app.Frame; var start = (RectTransform)UiKit.Find(lobby, "Start"); var card = (RectTransform)UiKit.Find(lobby, "ChapterCard");
                Assert.AreEqual(Layout.LobbyStart.X, start.anchorMin.x * 100f, 0.5f, "START x"); Assert.AreEqual(Layout.LobbyCard.X + Layout.LobbyCard.W, card.anchorMax.x * 100f, 0.5f, "카드 오른쪽 = START 오른쪽");
                Assert.AreEqual(start.anchorMin.x, card.anchorMin.x, 1e-3f, "START 와 카드는 같은 x"); Assert.AreEqual(start.anchorMax.x, card.anchorMax.x, 1e-3f, "START 와 카드는 같은 폭");
                Assert.AreEqual(1f - Layout.TabBar.Y / 100f, ((RectTransform)tabs).anchorMax.y, 1e-3f, "탭 바 = 표 자리");
                // 스타터팩·보조 버튼·성·이벤트는 눌러도 아무 일 없음(껍데기 · 팝업 안 열림 · 빨간 줄 0 · 오른쪽 아래 «이벤트» 는 T43 아레나 페이지(EventsScreenTests))
                foreach (var key in new[] { LobbyScreen.SideStarter, LobbyScreen.SideExplore, LobbyScreen.SideClearReward, LobbyScreen.SideCastle }) Assert.IsTrue(ClickNamed(lobby, "Side:" + key), "껍데기 버튼 " + key);
                yield return Frames(1);
                Assert.IsFalse(_app.Overlay.IsOpen, "껍데기 버튼은 팝업을 열지 않는다"); Assert.AreEqual("lobby", _app.Current.Name, "껍데기 버튼은 화면을 바꾸지 않는다");
            }
            Check("로비");

            // T44 — 로비 사이드 껍데기 6종: 팝업 4(퀘스트·출석·데일리 기프트·7일 챌린지 = 공통 팝업 문법 · 명판 · «탭하여 닫기» · 배경 탭 닫기 · X 없음) + 페이지 2(특권 = 사이드 · 시즌 패스 = 이벤트 배너 · 상단 바 + 뒤로 ◀ · 탭 바 없음)
            {
                (string key, string title, string mark)[] pops = { (LobbyScreen.SideQuest, "퀘스트", "새로고침까지"), (LobbyScreen.SideAttendance, "출석 보상", "7일차"), (LobbyScreen.SideDailyGift, "데일리 기프트", "광고 6회 보기"), (LobbyScreen.SideChallenge7, "7일 챌린지", "7일차") };
                foreach (var p in pops)
                {
                    Assert.IsTrue(ClickNamed(lobby, "Side:" + p.key), "사이드 " + p.key); yield return Frames(2);
                    Check("사이드 팝업 " + p.title, expectOverlay: true);
                    Assert.IsTrue(HasText(s => s == p.title), p.title + ": 명판"); Assert.IsTrue(HasText(s => s.Contains(p.mark)), p.title + ": 내용 «" + p.mark + "»");
                    Assert.IsTrue(HasText(s => s == "탭하여 닫기"), p.title + ": 탭하여 닫기"); Assert.IsNull(UiKit.Find(_app.Overlay.Root, "Button_Close_01"), p.title + ": 닫기 X 없음");
                    Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Dimmed"), p.title + ": 배경 탭"); yield return Frames(2); Assert.IsFalse(_app.Overlay.IsOpen, p.title + " 닫힘");
                }
                // 구도 단언 — 퀘스트: 박스 = 표 ⑬ · 줄 6 · 탭 3 · 트랙 보상 칸 5 / 출석: 칸 7 / 데일리: 선물 그림 · 광고 줄 4 · 타임라인 점 4 / 7일: 배너 · 일차 탭 7 · 과제 줄 4
                LobbyPopups.Quest(_app); yield return Frames(1);
                Assert.AreEqual(6, CountNamed(_app.Overlay.Root, "Quest:"), "퀘스트 줄 6"); Assert.AreEqual(3, CountNamed(_app.Overlay.Root, "Tab:"), "퀘스트 탭 3"); Assert.AreEqual(5, CountNamed(_app.Overlay.Root, "Track:"), "트랙 보상 칸 5(+메달)");
                { var bx = (RectTransform)UiKit.Find(_app.Overlay.Root, "QuestBox"); Assert.IsNotNull(bx, "퀘스트 박스"); Assert.AreEqual(Layout.QsBox.X, bx.anchorMin.x * 100f, 0.5f, "퀘스트 박스 x = 표 ⑬"); Assert.AreEqual(1f - Layout.QsBox.Y / 100f, bx.anchorMax.y, 1e-3f, "퀘스트 박스 y = 표 ⑬"); }
                _app.Overlay.Close(); yield return Frames(1);
                LobbyPopups.Attendance(_app); yield return Frames(1); Assert.AreEqual(7, CountNamed(_app.Overlay.Root, "Day:"), "출석 칸 7"); _app.Overlay.Close(); yield return Frames(1);
                LobbyPopups.DailyGift(_app); yield return Frames(1); Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "GiftPic"), "선물 그림"); Assert.AreEqual(4, CountNamed(_app.Overlay.Root, "Ad:"), "광고 줄 4"); Assert.AreEqual(4, CountNamed(_app.Overlay.Root, "Dot:"), "타임라인 점 4"); _app.Overlay.Close(); yield return Frames(1);
                LobbyPopups.Challenge7(_app); yield return Frames(1); Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "Banner"), "챌린지 배너"); Assert.AreEqual(7, CountNamed(_app.Overlay.Root, "DayTab:"), "일차 탭 7"); Assert.AreEqual(4, CountNamed(_app.Overlay.Root, "Task:"), "과제 줄 4"); _app.Overlay.Close(); yield return Frames(1);
                Check("사이드 팝업 4종 열고 닫음");
                // 페이지 2
                Assert.IsTrue(ClickNamed(lobby, "Side:" + LobbyScreen.SidePrivilege), "특권 아이콘"); yield return Frames(3);
                Assert.AreEqual("privilege", _app.Current.Name, "특권 페이지"); var pv = _app.Current.Root;
                Assert.IsNotNull(UiKit.Find(pv, "TopBar"), "특권: 상단 바"); Assert.AreEqual(4, CountNamed(pv, "Card:"), "특권 카드 4"); Assert.IsTrue(HasText(s => s == "특권") && HasText(s => s == "전체 받기"), "특권: 제목 · 전체 받기");
                Assert.IsNull(UiKit.Find(pv, "ui.tabBar"), "특권: 탭 바 없음"); Assert.IsFalse(HasText(s => s == "START"), "로비는 숨겨져 있다");
                Check("특권 페이지");
                Assert.IsTrue(ClickNamed(pv, "BackBtn"), "특권 뒤로"); yield return Frames(2); Assert.AreEqual("lobby", _app.Current.Name, "뒤로 → 로비");
                Assert.IsTrue(ClickNamed(lobby, "Banner"), "이벤트 배너 → 시즌 패스"); yield return Frames(3);
                Assert.AreEqual("pass", _app.Current.Name, "패스 페이지"); var ps = _app.Current.Root;
                Assert.IsNotNull(UiKit.Find(ps, "TopBar"), "패스: 상단 바"); Assert.AreEqual(Layout.PsRowCount, CountNamed(ps, "Row:"), "패스 트랙 줄"); Assert.IsTrue(HasText(s => s == "시즌 패스") && HasText(s => s == "전체 받기"), "패스: 제목 · 전체 받기");
                Assert.IsNotNull(UiKit.Find(ps, "Buy1Btn"), "패스 구매 버튼 1"); Assert.IsNotNull(UiKit.Find(ps, "Buy2Btn"), "패스 구매 버튼 2"); Assert.IsNull(UiKit.Find(ps, "ui.tabBar"), "패스: 탭 바 없음");
                { var tr = (RectTransform)UiKit.Find(ps, "Track"); Assert.IsNotNull(tr, "트랙"); Assert.AreEqual(Layout.PsTrack.X, tr.anchorMin.x * 100f, 0.5f, "트랙 x = 표 ⑰"); Assert.AreEqual(1f - Layout.PsTrack.Y / 100f, tr.anchorMax.y, 1e-3f, "트랙 y = 표 ⑰"); }
                Check("패스 페이지");
                Assert.IsTrue(ClickNamed(ps, "BackBtn"), "패스 뒤로"); yield return Frames(2); Assert.AreEqual("lobby", _app.Current.Name, "뒤로 → 로비"); Check("로비 복귀");
            }

            // 챕터 ◀▶ (최고 챕터 1 이라 그대로) · 탭 라벨
            Assert.IsTrue(ClickNamed(lobby, "ArrowR"), "챕터 ▶"); Assert.IsTrue(ClickNamed(lobby, "ArrowL"), "챕터 ◀"); yield return Frames(1);
            Check("로비 챕터 이동");

            // 설정 — T41 레퍼런스 12_settings.jpg 구도: 작은 패널 · 명판 «설정» · 음악/효과음 토글(Swich_01) · 언어 버튼 «한국어» · 패널 아래 링크 2 · «데이터 삭제» · «탭하여 닫기»(닫기 X 없음 · 배경 탭)
            _app.Overlay.Settings(); yield return Frames(2);
            Check("설정 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s == "음악"), "설정: 음악 줄"); Assert.IsTrue(HasText(s => s == "효과음"), "설정: 효과음 줄"); Assert.IsTrue(HasText(s => s == "설정"), "설정: 명판");
            Assert.IsTrue(HasText(s => s == "언어") && HasText(s => s == "한국어"), "설정: 언어 줄 + «한국어» 버튼");
            Assert.IsTrue(HasText(s => s == "개인정보 처리방침") && HasText(s => s == "이용약관"), "패널 아래 링크 글자 2");
            Assert.IsTrue(HasText(s => s == "탭하여 닫기"), "탭하여 닫기 안내"); Assert.IsNull(UiKit.Find(_app.Overlay.Root, "Button_Close_01"), "닫기 X 버튼 없음(공통 팝업 문법)");
            {
                var bx = (RectTransform)UiKit.Find(_app.Overlay.Root, "ui.popup"); Assert.IsNotNull(bx, "설정 패널(ui.popup)");
                Assert.AreEqual(Layout.SetBox.X, bx.anchorMin.x * 100f, 0.5f, "패널 x = 표 ⑨"); Assert.AreEqual(1f - Layout.SetBox.Y / 100f, bx.anchorMax.y, 1e-3f, "패널 y = 표 ⑨");
                var lang = (RectTransform)UiKit.Find(_app.Overlay.Root, "Language"); var bgmRow = (RectTransform)UiKit.Find(_app.Overlay.Root, "BGM"); var sfxRow = (RectTransform)UiKit.Find(_app.Overlay.Root, "SFX");
                Assert.IsTrue(bgmRow.anchorMax.y > sfxRow.anchorMax.y && sfxRow.anchorMax.y > lang.anchorMax.y, "줄 순서 = 음악 → 효과음 → 언어");
                Assert.IsNotNull(UiKit.Find(bgmRow, "Swich_01"), "음악 토글"); Assert.IsNotNull(UiKit.Find(sfxRow, "Swich_01"), "효과음 토글"); Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "LangBtn"), "언어 버튼");
            }
            // T63-settings — 설정 팝업 글자 가독성: 줄 라벨 3 = 56(레퍼런스 12 비례 · bestFit 이 안 줄임 · 한 줄) · 링크 2 = 본문 40 이 칸 안에 한 줄 · 언어 버튼 = 버튼 하한 이상으로 안 줄임 · «탭하여 닫기» 세로 안 잘림
            {
                foreach (var rowName in new[] { "BGM", "SFX", "Language" })
                {
                    var row = UiKit.Find(_app.Overlay.Root, rowName); Assert.IsNotNull(row, $"설정 줄 «{rowName}»");
                    var lb = UiKit.Find(row, "Text").GetComponent<Text>(); Assert.IsNotNull(lb, $"«{rowName}» 라벨");
                    Assert.AreEqual(Overlay.SetRowLabelSize, lb.fontSize, $"설정 라벨 «{lb.text}» 크기");
                    var gs = lb.GetGenerationSettings(lb.rectTransform.rect.size); gs.scaleFactor = 1f;
                    var gen = new TextGenerator(); gen.Populate(lb.text, gs);
                    Assert.GreaterOrEqual(gen.fontSizeUsedForBestFit, Overlay.SetRowLabelSize, $"설정 라벨 «{lb.text}» 가 칸({lb.rectTransform.rect.width:0}×{lb.rectTransform.rect.height:0})에 안 들어가 bestFit 이 줄였다");
                    Assert.AreEqual(1, gen.lineCount, $"설정 라벨 «{lb.text}» 는 한 줄");
                }
                foreach (var linkName in new[] { "Privacy", "Terms" })
                {
                    var lk = UiKit.Find(_app.Overlay.Root, linkName).GetComponent<Text>(); Assert.IsNotNull(lk, $"링크 «{linkName}»");
                    Assert.AreEqual(TextSize.Body, lk.fontSize, $"링크 «{lk.text}» 크기 = 본문 하한");
                    var r = lk.rectTransform.rect;
                    Assert.LessOrEqual(lk.preferredWidth, r.width + 1f, $"링크 «{lk.text}» 가 칸({r.width:0}) 밖으로 넘친다");
                    Assert.LessOrEqual(lk.preferredHeight, r.height + 1f, $"링크 «{lk.text}» 가 칸({r.height:0}) 위아래로 잘린다");
                }
                var langTxt = UiKit.ButtonText(UiKit.Find(_app.Overlay.Root, "LangBtn")); Assert.IsNotNull(langTxt, "«한국어» 버튼 글자");
                var lgs = langTxt.GetGenerationSettings(langTxt.rectTransform.rect.size); lgs.scaleFactor = 1f;
                var lgen = new TextGenerator(); lgen.Populate(langTxt.text, lgs);
                Assert.GreaterOrEqual(lgen.fontSizeUsedForBestFit, TextSize.Button, $"«{langTxt.text}» 버튼 글자가 칸({langTxt.rectTransform.rect.height:0})에 안 들어가 bestFit 이 버튼 하한 밑으로 줄였다");
                var tap = UiKit.Find(_app.Overlay.Root, "TapToClose").GetComponent<Text>();
                Assert.AreEqual(TextSize.Body, tap.fontSize, "«탭하여 닫기» 크기 = 본문 하한");
                Assert.LessOrEqual(tap.preferredHeight, tap.rectTransform.rect.height + 1f, "«탭하여 닫기» 가 칸 위아래로 잘린다");
            }
            var sw = UiKit.Find(_app.Overlay.Root, "BGM"); if (sw != null) { ClickNamed(sw, "Swich_01"); yield return Frames(1); Assert.IsTrue(_app.Save.Muted, "음악 스위치 = Save.Muted"); ClickNamed(sw, "Swich_01"); yield return Frames(1); }
            Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Dimmed"), "배경 탭 = 닫기"); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "설정이 닫혀야 한다"); Check("설정 닫힘");

            // T29 — «데이터 삭제»: 설정의 빨간 버튼 → 확인 팝업(«취소» = 설정으로 되돌아감 · «삭제» = 세이브 초기값 · 로비 새로 그림)
            {
                _app.Save.Gold = 12345; _app.Save.Gem = 67; _app.Save.MaxChapter = 3; _app.Save.SelChapter = 3; _app.Save.Speed = SaveData.SpeedMax; Give("weapon"); _app.Persist();
                _app.Overlay.Settings(); yield return Frames(1);
                Assert.IsTrue(HasText(s => s == "데이터 삭제"), "설정: «데이터 삭제» 버튼(빨간 Account Delete 자리)");
                Assert.IsTrue(Click(_app.Overlay.Root, s => s == "데이터 삭제"), "«데이터 삭제» 누름"); yield return Frames(2);
                Check("데이터 삭제 확인 팝업", expectOverlay: true);
                Assert.IsTrue(HasText(s => s.StartsWith("정말 삭제")), "확인 팝업 경고 글");
                Assert.IsTrue(Click(_app.Overlay.Root, s => s == "취소"), "«취소»"); yield return Frames(2);
                Check("데이터 삭제 취소 → 설정", expectOverlay: true);
                Assert.IsTrue(HasText(s => s == "음악"), "취소하면 설정 팝업으로 되돌아간다"); Assert.AreEqual(12345, _app.Save.Gold, 1e-6, "취소는 세이브를 건드리지 않는다");
                Assert.IsTrue(Click(_app.Overlay.Root, s => s == "데이터 삭제"), "«데이터 삭제» 다시"); yield return Frames(1);
                Assert.IsTrue(Click(_app.Overlay.Root, s => s == "삭제"), "«삭제»"); yield return Frames(2);
                Assert.IsFalse(_app.Overlay.IsOpen, "삭제 뒤 팝업 닫힘"); Assert.AreEqual("lobby", _app.Current.Name, "삭제 뒤 로비");
                var S = _app.Save;
                Assert.AreEqual(0, S.Gold, 1e-6, "골드 0"); Assert.AreEqual(0, S.Gem, 1e-6, "보석 0"); Assert.AreEqual(0, S.Inv.Count, "장비 0"); Assert.AreEqual(0, S.Eq.Count, "장착 0");
                Assert.AreEqual(1, S.MaxChapter, "최고 챕터 1"); Assert.AreEqual(1, S.SelChapter, "선택 챕터 1"); Assert.AreEqual(SaveData.SpeedMin, S.Speed, "배속 초기화(x1)"); Assert.IsFalse(S.MuteBgm || S.MuteSfx, "음소거 해제");
                Assert.AreEqual(0, SaveStore.Load(_app.Data).Gold, 1e-6, "PlayerPrefs 의 세이브도 초기값(키 삭제)");
                Assert.IsTrue(HasText(s => s == "데이터를 삭제했습니다"), "토스트");
                Check("데이터 삭제 뒤 로비");
            }

            // «탤런트» 탭·Character_Talent_02 팝업은 T43 이 «던전»(EventsScreen · EventsScreenTests) 으로 바꿨다 — 탭에서 더는 열리지 않는다

            // T42 — 펫 탭 = 레퍼런스 13_pet.jpg 구도(PetScreen · 껍데기): 상단 바 · 4열 격자 9칸(Lv · 진행바) · 합계 줄 · «장착중» 띠 + 슬롯 4 · 회색 2 · 주황 소환 2 · 탭 5 → 칸 클릭 = 세부 팝업(14 · 명판 없음 · 탭하여 닫기)
            {
                _app.ShowScreen("pet"); yield return Frames(2);
                Assert.AreEqual("pet", _app.Current.Name, "펫 탭은 팝업이 아니라 화면(PetScreen)"); Assert.IsFalse(_app.Overlay.IsOpen, "펫 탭 진입에 팝업 없음");
                var pet = _app.Current.Root;
                Check("펫 탭");
                Assert.IsNotNull(UiKit.Find(pet, "TopBar"), "펫 탭 상단 재화 바"); Assert.IsNull(UiKit.Find(pet, "ui.talent"), "Character_Talent_02 통째 스폰 0(부품 규칙)");
                Assert.AreEqual(Layout.PetCount, CountNamed(UiKit.Find(pet, "PetGrid"), "Pet:"), "펫 격자 9칸"); Assert.AreEqual(PetScreen.SlotCount, CountNamed(UiKit.Find(pet, "Slots"), "Slot:"), "장착 슬롯 4");
                Assert.AreEqual(Layout.PetCount, CountNamed(UiKit.Find(pet, "PetGrid"), "Bar"), "칸마다 진행바"); Assert.AreEqual(Layout.PetCount, CountNamed(UiKit.Find(pet, "PetGrid"), "Lv"), "칸마다 Lv 글자");
                Assert.IsTrue(HasText(s => s == "Lv. 0") && HasText(s => s == "0/0"), "숫자는 0(레퍼런스 숫자 베끼지 않음)");
                // T63-pet — 글자 가독성: 진행바 «0/0» 본문 40 이 바 안에 들어가고(바 높이 = Layout.PetBarH · 표 중심 유지) 펫 탭의 활성 Text 에 잘림/넘침 0(게이트 표와 같은 판정)
                Canvas.ForceUpdateCanvases();
                var barTxt0 = UiKit.Find(pet, "Pet:0/Bar").GetComponentInChildren<Text>(true); Assert.IsNotNull(barTxt0, "진행바 글자");
                Assert.GreaterOrEqual(barTxt0.resizeTextMaxSize, TextSize.Body, "진행바 숫자 최대 = 본문 40"); Assert.GreaterOrEqual(TextAudit.BestFitSize(barTxt0), TextSize.Body, "진행바 숫자를 bestFit 이 안 줄인다(40 그대로)");
                Assert.GreaterOrEqual(barTxt0.rectTransform.rect.height + 1f, barTxt0.preferredHeight, "진행바 글자 rect 높이 ≥ 선호 높이(잘림 없음)");
                var bar0Rt = (RectTransform)UiKit.Find(pet, "Pet:0/Bar"); Assert.AreEqual(Layout.PetBarH / 100f * _app.Frame.rect.height, bar0Rt.rect.height, 1.5f, "진행바 높이 = Layout.PetBarH(프레임 %)");
                var petClip = TextAudit.Collect("13_pet", pet).FindAll(r => r.Clipped);
                Assert.AreEqual(0, petClip.Count, "펫 탭 잘림/넘침 0(T63-pet) — " + string.Join(" · ", petClip.ConvertAll(r => r.ToString())));
                Assert.IsTrue(HasText(s => s == "장착중") && HasText(s => s == "전체 강화") && HasText(s => s == "빠른 장착") && HasText(s => s == "소환") && HasText(s => s == "소환 x10"), "라벨 우리말");
                var tabs2 = UiKit.Find(pet, "ui.tabBar"); Assert.IsNotNull(tabs2, "펫 탭 바"); Assert.GreaterOrEqual(tabs2.childCount, NavBar.Keys.Length, "탭 5");
                // 배치 = 표 ⑩(±0.5%p) — 첫 칸 · 슬롯 줄 · 버튼 2줄 · 탭 바
                var c0 = (RectTransform)UiKit.Find(pet, "Pet:0"); Assert.AreEqual(Layout.PetCell.X, c0.anchorMin.x * 100f, 0.5f, "첫 칸 x"); Assert.AreEqual(1f - Layout.PetCell.Y / 100f, c0.anchorMax.y, 1e-3f, "첫 칸 y");
                var c8 = (RectTransform)UiKit.Find(pet, "Pet:8"); Assert.AreEqual(Layout.PetCell.X, c8.anchorMin.x * 100f, 0.5f, "9번째 칸 = 3행 첫 열"); Assert.IsTrue(c8.anchorMax.y < c0.anchorMin.y, "3행은 1행 아래");
                var s0 = (RectTransform)UiKit.Find(pet, "Slot:0"); Assert.AreEqual(Layout.PetSlot.X, s0.anchorMin.x * 100f, 0.5f, "첫 슬롯 x");
                var ua = (RectTransform)UiKit.Find(pet, "UpgradeAllBtn"); var sm = (RectTransform)UiKit.Find(pet, "SummonBtn"); var sm10 = (RectTransform)UiKit.Find(pet, "Summon10Btn");
                Assert.IsTrue(ua.anchorMin.y > sm.anchorMax.y, "회색 줄이 소환 줄 위"); Assert.IsTrue(sm10.anchorMin.x > sm.anchorMax.x, "소환 x10 은 소환 오른쪽");
                Assert.AreEqual(Layout.PetSummon.X, sm.anchorMin.x * 100f, 0.5f, "소환 x"); Assert.AreEqual(1f - Layout.TabBar.Y / 100f, ((RectTransform)tabs2).anchorMax.y, 1e-3f, "탭 바 = 표 자리");
                // 껍데기 버튼·슬롯 — 눌러도 아무 일 없음(팝업 안 열림 · 화면 그대로 · 빨간 줄 0)
                foreach (var n in new[] { "UpgradeAllBtn", "QuickEquipBtn", "SummonBtn", "Summon10Btn", "Slot:0", "Slot:3" }) Assert.IsTrue(ClickNamed(pet, n), "껍데기 " + n);
                yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen, "껍데기 버튼은 팝업을 열지 않는다"); Assert.AreEqual("pet", _app.Current.Name, "화면 그대로");
                Check("펫 껍데기 버튼");
                // 세부 팝업(14) — 칸 클릭 → 명판 없음 · 세부 칸 · «패시브:» · 강화/장착(껍데기) · «탭하여 닫기» · 배경 탭으로 닫힘
                Assert.IsTrue(ClickNamed(pet, "Pet:0"), "펫 칸 클릭"); yield return Frames(2);
                Check("펫 세부 팝업", expectOverlay: true);
                var ov = _app.Overlay.Root;
                Assert.IsNotNull(UiKit.Find(ov, "PetDetailCell"), "세부 칸"); Assert.IsNotNull(UiKit.Find(ov, "Desc"), "설명 박스"); Assert.IsNotNull(UiKit.Find(ov, "PassiveRow"), "패시브 수치 줄");
                Assert.IsTrue(HasText(s => s == "패시브:") && HasText(s => s == "강화") && HasText(s => s == "장착") && HasText(s => s == "탭하여 닫기"), "세부 팝업 글자");
                var rib = UiKit.Find(ov, "ui.title.tangerine"); Assert.IsTrue(rib == null || !rib.gameObject.activeSelf, "세부 팝업은 명판 없음(레퍼런스 14)"); Assert.IsNull(UiKit.Find(ov, "Button_Close_01"), "닫기 X 없음");
                var bx = (RectTransform)UiKit.Find(ov, "ui.popup"); Assert.IsNotNull(bx, "세부 패널(ui.popup)"); Assert.AreEqual(Layout.PdBox.X, bx.anchorMin.x * 100f, 0.5f, "패널 x = 표 ⑪"); Assert.AreEqual(1f - Layout.PdBox.Y / 100f, bx.anchorMax.y, 1e-3f, "패널 y = 표 ⑪");
                // T63-pet — 세부 팝업 글자: 진행바 «0/0» 40 이 바 안에(PdBar 1.4% → Layout.PetBarH) · 팝업 안 활성 Text 잘림/넘침 0
                Canvas.ForceUpdateCanvases();
                var dBar = UiKit.Find(ov, "PetDetailCell/Bar"); Assert.IsNotNull(dBar, "세부 진행바"); var dBarTxt = dBar.GetComponentInChildren<Text>(true); Assert.IsNotNull(dBarTxt, "세부 진행바 글자");
                Assert.GreaterOrEqual(TextAudit.BestFitSize(dBarTxt), TextSize.Body, "세부 진행바 숫자 40 그대로"); Assert.GreaterOrEqual(dBarTxt.rectTransform.rect.height + 1f, dBarTxt.preferredHeight, "세부 진행바 글자 rect 높이 ≥ 선호 높이");
                var pdClip = TextAudit.Collect("14_pet_detail", ov).FindAll(r => r.Clipped);
                Assert.AreEqual(0, pdClip.Count, "펫 세부 팝업 잘림/넘침 0(T63-pet) — " + string.Join(" · ", pdClip.ConvertAll(r => r.ToString())));
                Assert.IsTrue(ClickNamed(ov, "PetUpgradeBtn") && ClickNamed(ov, "PetEquipBtn"), "세부 버튼 2"); yield return Frames(1); Assert.IsTrue(_app.Overlay.IsOpen, "껍데기 버튼은 팝업을 닫지 않는다");
                Assert.IsTrue(ClickNamed(ov, "Dimmed"), "배경 탭 = 닫기"); yield return Frames(2); Assert.IsFalse(_app.Overlay.IsOpen, "세부 팝업 닫힘");
                Check("펫 세부 닫힘");
                _app.ShowScreen("lobby"); yield return Frames(1); Check("펫 → 로비");
            }

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
            // T37 — 레퍼런스 06_gear.jpg 구도 단언: 상단 재화 바 · 무대(들판·길·나무 · 정사각 캐릭터 호스트) · 슬롯 6 = 표 자리(좌 3 / 우 3 · ±0.5%p) · 스탯 3칸 · 상점/대장간 버튼(스탯 줄 아래 · 대장간 오른쪽 끝) · 인벤 5열 · 탭 바
            {
                var top = UiKit.Find(gear, "TopBar"); Assert.IsNotNull(top, "장비 화면 상단 재화 바"); Assert.IsNotNull(UiKit.Find(top, "Avatar"), "아바타"); Assert.IsNotNull(UiKit.Find(top, "ResourceBar_Gem"), "보석 pill");
                var stage = (RectTransform)UiKit.Find(gear, "Stage"); Assert.IsNotNull(stage, "캐릭터 무대");
                Assert.AreEqual(1f - Layout.GearStage.Y / 100f, stage.anchorMax.y, 1e-3f, "무대 = 표 자리(y)"); Assert.AreEqual(1f - (Layout.GearStage.Y + Layout.GearStage.H) / 100f, stage.anchorMin.y, 1e-3f, "무대 높이 = 표(26.5%)");
                Assert.IsNotNull(UiKit.Find(stage, "Field"), "무대 들판"); Assert.IsNotNull(UiKit.Find(stage, "Road"), "무대 길"); Assert.GreaterOrEqual(CountNamed(stage, "Tree"), 3, "무대 나무");
                var hero = (RectTransform)UiKit.Find(stage, "Hero"); Assert.IsNotNull(hero, "캐릭터 호스트");
                var gearHv = hero.GetComponentInChildren<HeroView>(true); Assert.IsNotNull(gearHv, "캐릭터 = HeroView(플레이어 외형)");
                Assert.IsFalse(gearHv.Still, "장비 화면 큰 캐릭터는 움직임 유지(T68 ② 는 로비 상단 초상만)"); Assert.AreEqual(1f, gearHv.Rig.AnimSpeed, 1e-3f, "장비 캐릭터 Animator 속도 1");
                float hh = (hero.anchorMax.y - hero.anchorMin.y) * Layout.GearStage.H, hw = (hero.anchorMax.x - hero.anchorMin.x) * 100f;
                Assert.AreEqual(Layout.GearHero.H, hh, 0.3f, "캐릭터 호스트 높이 = 표(19%)"); Assert.AreEqual(hh * UiKit.FrameH / UiKit.FrameW, hw, 0.3f, "캐릭터 호스트는 정사각(폭 = 높이 환산)");
                Assert.AreEqual(50f, (hero.anchorMin.x + hero.anchorMax.x) * 50f, 0.5f, "캐릭터는 가운데");
                for (int i = 0; i < 6; i++)
                {
                    var sl = (RectTransform)slots.GetChild(i); var col = i < 3 ? Layout.GearSlotColL : Layout.GearSlotColR; float y = col.Y + (i % 3) * Layout.GearSlotPitch;
                    Assert.AreEqual(col.X, sl.anchorMin.x * 100f, 0.5f, "슬롯 " + i + " x"); Assert.AreEqual(y, (1f - sl.anchorMax.y) * 100f, 0.5f, "슬롯 " + i + " y");
                    Assert.AreEqual(Layout.GearSlot.W, (sl.anchorMax.x - sl.anchorMin.x) * 100f, 0.5f, "슬롯 " + i + " 폭"); Assert.AreEqual(Layout.GearSlotH, (sl.anchorMax.y - sl.anchorMin.y) * 100f, 0.5f, "슬롯 " + i + " 높이");
                    Assert.IsTrue(HasText(s => s == "Lv. 0"), "슬롯 위 «Lv. N»");
                }
                // T63-gear — 슬롯 위 «Lv. N» 은 본문 40 한 줄(bestFit 이 안 줄임) · «+N» 배지는 Small(SlotBadgeSize) · 배지(칸 아래 가장자리)와 아래 칸의 «Lv. N» 라벨이 겹치지 않는다(CI #95 screens 06: 피치 7.3 에선 «+1» 위에 «Lv. 0» 이 얹혀 «Lv.10» 으로 읽혔다)
                {
                    var lvRect = new RectTransform[6]; var badgeRect = new RectTransform[6];
                    for (int i = 0; i < 6; i++)
                    {
                        var sl = slots.GetChild(i);
                        foreach (var t in sl.GetComponentsInChildren<Text>(true))
                        {
                            if (t.text.StartsWith("Lv.") && t.transform.parent == sl)
                            {
                                lvRect[i] = t.rectTransform;
                                Assert.AreEqual(TextSize.Body, t.fontSize, "슬롯 " + i + " «Lv. N» 크기 = 본문 하한");
                                var gs = t.GetGenerationSettings(t.rectTransform.rect.size); gs.scaleFactor = 1f;
                                var gen = new TextGenerator(); gen.Populate(t.text, gs);
                                Assert.GreaterOrEqual(gen.fontSizeUsedForBestFit, TextSize.Body, "슬롯 " + i + " «Lv. N» 이 칸에 40 으로 안 들어가 bestFit 이 줄였다"); Assert.AreEqual(1, gen.lineCount, "슬롯 " + i + " «Lv. N» 한 줄");
                            }
                            else if (t.transform.parent != null && t.transform.parent.name == "PlusBadge")
                            {
                                badgeRect[i] = (RectTransform)t.transform.parent;
                                Assert.AreEqual(GearScreen.SlotBadgeSize, t.fontSize, "슬롯 " + i + " «+N» 배지 = Small 크기"); Assert.AreEqual(TextKind.Small, TextAudit.KindOf(t), "«+N» 배지는 Small 표식");
                                Assert.LessOrEqual(t.preferredHeight, badgeRect[i].rect.height + 1f, "슬롯 " + i + " «+N» 이 배지 높이에 들어간다");
                            }
                        }
                        Assert.IsNotNull(lvRect[i], "슬롯 " + i + " «Lv. N» 라벨"); Assert.IsNotNull(badgeRect[i], "슬롯 " + i + " «+N» 배지");
                    }
                    var c = new Vector3[4];
                    for (int i = 0; i < 6; i++)
                    {
                        // 열의 마지막 칸 아래엔 칸이 없다
                        if (i % 3 == 2) continue;
                        badgeRect[i].GetWorldCorners(c); float badgeBottom = c[0].y;
                        lvRect[i + 1].GetWorldCorners(c); float lvBottom = c[0].y, lvH = c[1].y - c[0].y;
                        // «Lv. N» 은 LowerCenter 라 잉크가 rect 아래쪽 ≈70% 안에 있다 — 배지 아래 끝이 그 위여야 잉크가 안 겹친다
                        Assert.GreaterOrEqual(badgeBottom, lvBottom + lvH * 0.7f, $"슬롯 {i} «+N» 배지가 슬롯 {i + 1} «Lv. N» 라벨과 겹친다(T63-gear)");
                    }
                }
                Assert.AreEqual(3, CountNamed(gear, "Stat:"), "스탯 3칸(공·❤·🛡)");
                var forgeB = (RectTransform)UiKit.Find(gear, "ForgeBtn"); var shopB = (RectTransform)UiKit.Find(gear, "ShopBtn"); var statA = (RectTransform)UiKit.Find(gear, "Stat:atk");
                Assert.IsNotNull(forgeB, "«대장간» 버튼"); Assert.IsNotNull(shopB, "«상점» 버튼"); Assert.IsNotNull(statA, "스탯 칸");
                Assert.AreEqual(Layout.GearForgeBtn.X + Layout.GearForgeBtn.W, forgeB.anchorMax.x * 100f, 0.5f, "대장간 = 오른쪽 끝(표 액션바)");
                Assert.Less(forgeB.anchorMax.y, statA.anchorMin.y, "버튼 줄은 스탯 줄 아래"); Assert.AreEqual(forgeB.anchorMax.y, shopB.anchorMax.y, 1e-3f, "상점·대장간 같은 줄");
                Assert.IsTrue(HasText(s => s == "대장간") && HasText(s => s == "상점"), "버튼 라벨 우리말");
                var inv = (RectTransform)UiKit.Find(gear, "InvScroll"); Assert.IsNotNull(inv, "인벤 스크롤");
                Assert.AreEqual(1f - Layout.GearInv.Y / 100f, inv.anchorMax.y, 1e-3f, "인벤 = 표 자리"); Assert.Less(inv.anchorMax.y, forgeB.anchorMin.y + 1e-3f, "인벤은 버튼 줄 아래");
                var grid = content.GetComponent<GridLayoutGroup>(); Assert.IsNotNull(grid, "인벤 격자"); Assert.AreEqual(Layout.GearInvCols, grid.constraintCount, "5열");
                Assert.AreEqual(0, CountNamed(gear, "ui.equipment"), "Character_Hero_Equipment 를 통째로 세우지 않는다(T37)");
            }
            Check("장비 화면");

            // 세부 팝업(미장착) → 장착
            var g0 = items[0];
            GearUi.OpenDetail(_app, g0, _app.Current.Refresh); yield return Frames(2);
            Check("장비 세부 팝업", expectOverlay: true);
            // T38 — 레퍼런스 07 구도(표 ④): 패널 = GdBox · 등급 탭 · 아이콘 칸 · 이름 · pill 2 · 스탯 박스(초록) · 옵션 줄 · 비용 줄 · 버튼 2 · «탭하여 닫기»(X 없음) — Character_Hero_Item_Detail_01 통째 스폰 0
            {
                var ovr = _app.Overlay.Root;
                Assert.IsNull(UiKit.Find(ovr, "ui.itemDetail"), "세부 팝업은 프리팹 통째가 아니다(T38)");
                var bx = (RectTransform)UiKit.Find(ovr, "ui.popup"); Assert.IsNotNull(bx, "세부 패널(ui.popup)");
                Assert.AreEqual(Layout.GdBox.X, bx.anchorMin.x * 100f, 0.5f, "패널 x = 표 ④"); Assert.AreEqual(1f - Layout.GdBox.Y / 100f, bx.anchorMax.y, 1e-3f, "패널 y = 표 ④"); Assert.AreEqual(Layout.GdBox.H, (bx.anchorMax.y - bx.anchorMin.y) * 100f, 0.5f, "패널 높이 = 표 ④");
                Assert.IsNotNull(UiKit.Find(bx, "IconSlot"), "아이콘 칸"); Assert.IsNotNull(UiKit.Find(bx, "gear:" + g0.Uid), "아이콘 칸 = 장비 칸(Cell)");
                Assert.IsNotNull(UiKit.Find(bx, "Name"), "이름줄"); Assert.IsNotNull(UiKit.Find(bx, "Pill1"), "pill «슬롯 Lv»"); Assert.IsNotNull(UiKit.Find(bx, "Pill2"), "pill «부위»");
                Assert.IsTrue(HasText(s => s.StartsWith("슬롯 Lv. ")), "메타 pill 글자"); Assert.IsTrue(HasText(s => s == GearUi.PartName(D, g0.Part)), "부위 pill");
                Assert.IsNotNull(UiKit.Find(bx, "Stats"), "스탯 박스"); Assert.AreEqual(3, CountNamed(UiKit.Find(bx, "Stats"), "Stat:"), "스탯 줄 3(공격력·체력·실드)");
                var opts = UiKit.Find(bx, "Options"); Assert.IsNotNull(opts, "옵션 목록"); Assert.AreEqual(D.Gear.Options.TryGetValue(g0.Type, out var ol0) ? ol0.Count : 0, CountNamed(opts, "Opt:"), "옵션 줄 수 = 세트 옵션 수");
                // T63-gear — 스탯 줄 3 · 옵션 줄 전부 본문 40 이 «한 줄» 로(옵션은 긴 잠금 줄만 bestFit 32~40 허용 · 스탯은 40 그대로) · 스탯 상자와 옵션 목록이 안 겹친다(전엔 39.5+9.5 = 49.0 > 48.0)
                {
                    int statRows = 0, optRows = 0;
                    foreach (var t in UiKit.Find(bx, "Stats").GetComponentsInChildren<Text>(false))
                    {
                        if (!t.name.StartsWith("Stat:")) continue; statRows++;
                        Assert.AreEqual(TextSize.Body, t.fontSize, "스탯 줄 «" + t.text + "» 크기 = 본문 하한");
                        var gs = t.GetGenerationSettings(t.rectTransform.rect.size); gs.scaleFactor = 1f; var gen = new TextGenerator(); gen.Populate(t.text, gs);
                        Assert.GreaterOrEqual(gen.fontSizeUsedForBestFit, TextSize.Body, "스탯 줄 «" + t.text + "» 가 40 으로 안 들어간다"); Assert.AreEqual(1, gen.lineCount, "스탯 줄 한 줄");
                    }
                    foreach (var t in opts.GetComponentsInChildren<Text>(false))
                    {
                        if (t.transform.parent == null || !t.transform.parent.name.StartsWith("Opt:")) continue; optRows++;
                        Assert.AreEqual(TextSize.Body, t.fontSize, "옵션 줄 «" + t.text + "» 크기 = 본문 하한");
                        var gs = t.GetGenerationSettings(t.rectTransform.rect.size); gs.scaleFactor = 1f; var gen = new TextGenerator(); gen.Populate(t.text, gs);
                        Assert.GreaterOrEqual(gen.fontSizeUsedForBestFit, TextSize.BestFitMin, "옵션 줄 «" + t.text + "» 가 bestFit 최소(32) 아래로"); Assert.AreEqual(1, gen.lineCount, "옵션 줄 «" + t.text + "» 는 한 줄(문구 줄이기 = GearText.Shorten)");
                        Assert.IsFalse(t.text.Contains(" 이상)"), "잠금 꼬리는 «(등급)» 으로 줄인다: " + t.text);
                    }
                    Assert.AreEqual(3, statRows, "스탯 줄 3"); Assert.AreEqual(CountNamed(opts, "Opt:"), optRows, "옵션 줄마다 글자 하나");
                    var st = (RectTransform)UiKit.Find(bx, "Stats"); var op = (RectTransform)opts;
                    Assert.GreaterOrEqual(st.anchorMin.y, op.anchorMax.y - 1e-3f, "스탯 상자 아래 끝이 옵션 목록 위 끝보다 위(겹침 0)");
                }
                Assert.IsNotNull(UiKit.Find(bx, "Cost"), "비용 줄"); Assert.IsNotNull(UiKit.Find(bx, "BtnL"), "왼쪽 버튼(장착/해제)"); Assert.IsNotNull(UiKit.Find(bx, "BtnR"), "오른쪽 버튼(슬롯 강화)");
                Assert.IsTrue(HasText(s => s == GearUi.RarName(D, g0.Rar)), "등급 탭 글자"); Assert.IsTrue(HasText(s => s == "탭하여 닫기"), "탭하여 닫기"); Assert.IsNull(UiKit.Find(ovr, "Button_Close_01"), "닫기 X 없음");
                var l = (RectTransform)UiKit.Find(bx, "BtnL"); var r = (RectTransform)UiKit.Find(bx, "BtnR"); Assert.Less(l.anchorMax.x, r.anchorMin.x + 1e-3f, "버튼 2 = 왼쪽 파랑 · 오른쪽 주황");
            }
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
            Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Dimmed"), "세부 팝업 배경 탭 = 닫기(T38)"); yield return Frames(1);
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
                        // T31 — 아이콘은 Thumbnail(cmi.*) · 입는 파츠(cm.*)와 다른 그림 · 128×128 캔버스
                        {
                            var eqG = _app.Save.EquippedGear(part); Assert.IsNotNull(eqG, "슬롯 " + i + " 장착 장비");
                            string ik = GearLook.IconKey(D, eqG), pk = GearLook.PartKey(D, eqG);
                            Assert.IsTrue(ik.StartsWith(GearLook.IconPrefix), "아이콘 키는 cmi.*: " + ik);
                            Assert.AreEqual(_app.Assets.Sprite(ik), im.sprite, "슬롯 " + i + " 아이콘 = 카탈로그 " + ik);
                            Assert.AreNotEqual(_app.Assets.Sprite(pk), im.sprite, "슬롯 " + i + " 아이콘은 입는 파츠(" + pk + ")와 다른 그림(T31)");
                            Assert.AreEqual(128f, im.sprite.rect.width, 0.5f, "Thumbnail 캔버스 128"); Assert.AreEqual(128f, im.sprite.rect.height, 0.5f, "Thumbnail 캔버스 128");
                        }
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
            Assert.IsTrue(HasText(s => s.EndsWith("슬롯")), "슬롯 팝업 등급 탭 = «부위 슬롯»");
            Assert.IsTrue(HasText(s => s == "비어 있음") && HasText(s => s.StartsWith("장착된 장비가 없습니다")), "빈 슬롯 = 같은 구도(이름 «비어 있음» · 옵션 자리 안내)");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "BtnR"), "강화 버튼만"); Assert.IsNull(UiKit.Find(_app.Overlay.Root, "BtnL"), "빈 슬롯엔 장착/해제 없음");
            Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Dimmed"), "슬롯 팝업 배경 탭 = 닫기"); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen); Check("슬롯 팝업 닫힘");
            yield return Shutdown();
        }

        // ───────────────────────── ③ 대장간 — 레퍼런스 08 구도(무대 · 결과 슬롯 · 액션바 · 인벤 · 뒤로) · 인벤 전부 · 빨간 점 · 재료 3개 → 합성 ─────────────────────────
        [UnityTest]
        public IEnumerator ForgeShowsAllAndFuses()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            var a = Give(D.Gear.Parts[0]); var b = Give(D.Gear.Parts[0]); var c = Give(D.Gear.Parts[0]);
            var other = Give(D.Gear.Parts[1], rar: 1); S.Eq[other.Part] = other.Uid;   // 장착분도 대장간엔 보인다(«장착중» 글자 · 흐리지 않음 · 재료 가능 T24)
            Assert.AreEqual(GearUi.Key(a), GearUi.Key(b)); Assert.AreEqual(GearUi.Key(a), GearUi.Key(c));
            _app.ShowScreen("gear"); yield return Frames(1);
            _app.ShowScreen("forge"); yield return Frames(2);
            Assert.AreEqual("forge", _app.Current.Name);
            var forge = _app.Current.Root; var content = UiKit.Find(forge, "Content"); Assert.IsNotNull(content, "대장간 인벤 Content");
            Assert.AreEqual(S.Inv.Count, CountNamed(content, "gear:"), "대장간 인벤에 장비가 전부(장착분 포함) 보여야 한다");
            Assert.GreaterOrEqual(CountNamed(content, "FuseDot"), 3, "합성 가능한 칸의 빨간 점(같은 키 3개)");
            Assert.AreEqual(1, CountNamed(content, "EquippedLabel"), "장착분 칸의 «장착중» 글자(레퍼런스 Equipped)");
            // T63-forge — «장착중» 이 장비 그림 위에서 읽혀야 한다: 본문 하한 40 · bestFit 이 안 줄임 · 한 줄 · 뒤에 어두운 띠(레퍼런스 08 의 «Equipped» 띠)가 글자를 덮는다
            {
                var eqTf = UiKit.Find(content, "EquippedLabel"); Assert.IsNotNull(eqTf, "«장착중» 글자");
                var eqLb = eqTf.GetComponent<Text>(); Assert.IsNotNull(eqLb, "«장착중» Text 컴포넌트");
                Assert.AreEqual(TextSize.Body, eqLb.fontSize, "«장착중» 크기 = 본문 하한");
                var gs = eqLb.GetGenerationSettings(eqLb.rectTransform.rect.size); gs.scaleFactor = 1f;
                var gen = new TextGenerator(); gen.Populate(eqLb.text, gs);
                Assert.GreaterOrEqual(gen.fontSizeUsedForBestFit, TextSize.Body, "«장착중» 이 칸에 40 으로 안 들어가 bestFit 이 줄였다");
                Assert.AreEqual(1, gen.lineCount, "«장착중» 한 줄");
                var plate = UiKit.Find(eqLb.transform.parent, "EquippedPlate");
                Assert.IsNotNull(plate, "«장착중» 뒤 어두운 띠(그림 위에 바로 얹으면 안 읽힌다)");
                var pr = (RectTransform)plate; var lr = eqLb.rectTransform;
                Assert.LessOrEqual(pr.anchorMin.x, lr.anchorMin.x + 1e-3f, "띠가 글자보다 왼쪽까지"); Assert.GreaterOrEqual(pr.anchorMax.x, lr.anchorMax.x - 1e-3f, "띠가 글자보다 오른쪽까지");
                Assert.LessOrEqual(pr.anchorMin.y, lr.anchorMin.y + 1e-3f, "띠가 글자보다 아래까지"); Assert.GreaterOrEqual(pr.anchorMax.y, lr.anchorMax.y - 1e-3f, "띠가 글자보다 위까지");
                Assert.Less(plate.GetSiblingIndex(), eqLb.transform.GetSiblingIndex(), "띠가 글자보다 먼저(= 글자가 띠 위에) 그려져야 한다");
                Assert.Greater(plate.GetComponent<Image>().color.a, 0.6f, "띠는 그림을 가릴 만큼 불투명해야 한다");
            }
            // T39 — 레퍼런스 08_gear_fuse.jpg 구도 단언: 무대(위 41%) · 결과 슬롯(좌상) · 액션바(자동 왼쪽 끝 · 합성 오른쪽 끝 · 회색) · 인벤 = 장비 탭과 같은 자리 · 뒤로 버튼(왼쪽 아래) · 제목 글자·상단 재화 바 없음
            {
                var stage = (RectTransform)UiKit.Find(forge, "Stage"); var result = (RectTransform)UiKit.Find(forge, "Result"); var autoB = (RectTransform)UiKit.Find(forge, "AutoBtn"); var fuseB = (RectTransform)UiKit.Find(forge, "FuseBtn"); var fuseOn = UiKit.Find(forge, "FuseBtnOn"); var back = (RectTransform)UiKit.Find(forge, "BackBtn"); var inv = (RectTransform)UiKit.Find(forge, "InvScroll");
                Assert.IsNotNull(stage, "무대"); Assert.IsNotNull(result, "결과 슬롯"); Assert.IsNotNull(autoB, "«자동»"); Assert.IsNotNull(fuseB, "«합성»(회색)"); Assert.IsNotNull(fuseOn, "«합성»(주황)"); Assert.IsNotNull(back, "뒤로"); Assert.IsNotNull(inv, "인벤");
                Assert.AreEqual(1f - Layout.ForgeStage.H / 100f, stage.anchorMin.y, 1e-3f, "무대 = 위 41%"); Assert.AreEqual(Layout.ForgeResult.X / 100f, result.anchorMin.x, 1e-3f, "결과 슬롯 = 표 자리");
                Assert.AreEqual(Layout.ForgeAuto.X / 100f, autoB.anchorMin.x, 1e-3f, "자동 = 왼쪽 끝"); Assert.AreEqual((Layout.ForgeFuse.X + Layout.ForgeFuse.W) / 100f, fuseB.anchorMax.x, 1e-3f, "합성 = 오른쪽 끝"); Assert.AreEqual(autoB.anchorMax.y, fuseB.anchorMax.y, 1e-3f, "같은 줄");
                Assert.IsFalse(fuseOn.gameObject.activeSelf, "재료 없으면 합성은 회색 버튼"); Assert.IsFalse(fuseB.GetComponent<Button>().interactable, "회색 합성은 비활성");
                Assert.AreEqual(1f - Layout.ForgeInv.Y / 100f, inv.anchorMax.y, 1e-3f, "인벤 = 장비 탭과 같은 자리"); Assert.Less(back.anchorMin.x, 0.05f, "뒤로 = 왼쪽"); Assert.Less(back.anchorMax.y, 0.1f, "뒤로 = 아래(표 ⑥ y93.5 → anchorMax.y ≈ 0.065 · anchorMin.y 는 바닥 기준이라 작을수록 아래 · CI #66 T48)");
                Assert.IsFalse(HasText(s => s == "대장간"), "제목 글자 없음(레퍼런스)"); Assert.IsNull(UiKit.Find(forge, "TopBar"), "상단 재화 바 없음(레퍼런스)");
            }
            Check("대장간");

            foreach (var g in new[] { a, b, c }) { Assert.IsTrue(ClickNamed(content, "gear:" + g.Uid), "재료 칸 클릭 " + g.Uid); yield return Frames(1); }
            Assert.IsTrue(HasText(s => s == "합성 (3/3)"), "재료 3개 고르면 «합성 (3/3)»");
            Assert.IsTrue(UiKit.Find(forge, "FuseBtnOn").gameObject.activeSelf && !UiKit.Find(forge, "FuseBtn").gameObject.activeSelf, "재료 3개면 합성이 주황 버튼으로");
            Check("재료 3개 선택");
            int before = S.Inv.Count;
            Assert.IsTrue(Click(forge, s => s == "합성 (3/3)"), "합성 버튼"); yield return Frames(2);
            Assert.AreEqual(before - 2, S.Inv.Count, "3개 → 1개");
            Assert.AreEqual(1, S.Fuses);
            Check("합성 뒤");
            Assert.IsTrue(Click(forge, s => s == "자동"), "자동 버튼(조합 없음 → 토스트)"); yield return Frames(2);
            Check("자동 합성(조합 없음)");
            Assert.IsTrue(ClickNamed(forge, "BackBtn"), "뒤로(◀ 아이콘 · 글자 없음)"); yield return Frames(2);
            Assert.AreEqual("gear", _app.Current.Name); Check("대장간 → 장비");
            yield return Shutdown();
        }

        // ───────────────────────── ④ 상점 — 레퍼런스 09/10 구도(상자 3 · 다이아 6 · 골드 3) · 뽑기 → 공통 팝업 ─────────────────────────
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
            var bar = UiKit.Find(shop, "ui.tabBar"); if (bar == null) bar = UiKit.Find(shop, "Tab_01_BottomFlushMenu");
            Assert.IsNotNull(bar, "상점 탭 바"); Assert.GreaterOrEqual(bar.childCount, 5, "탭 5");
            // T40 — 레퍼런스 09_shop_1.jpg·10_shop_2.jpg 구도 단언: 상단 재화 바 · 스크롤 안에 상자 카드 3(큰 카드 1 = 가장 비싼 상자 · 작은 카드 2) · «무료 보급» 줄 · 다이아 6 · 골드 3 · 탭 바 = 표 자리
            {
                Assert.IsNotNull(UiKit.Find(shop, "TopBar"), "상단 재화 바(TopBar)");
                var content = UiKit.Find(shop, "Content"); Assert.IsNotNull(content, "세로 스크롤 Content");
                Assert.AreEqual(3, CountNamed(content, "Box:"), "상자 카드 3");
                Assert.AreEqual(D.Shop.GemPacks.Count, CountNamed(content, "GemPack:"), "다이아 카드 = shop.json gemPacks 수"); Assert.AreEqual(6, D.Shop.GemPacks.Count, "다이아 6");
                Assert.AreEqual(D.Shop.GoldPacks.Count, CountNamed(content, "GoldPack:"), "골드 카드 = shop.json goldPacks 수"); Assert.AreEqual(3, D.Shop.GoldPacks.Count, "골드 3");
                Assert.IsNotNull(UiKit.Find(content, "FreeLine"), "«무료 보급까지» 줄");
                Assert.IsTrue(HasText(s => s == "다이아") && HasText(s => s == "골드"), "섹션 제목 «다이아»·«골드»");
                GachaBox big = null; foreach (var b in D.Gacha.Boxes) if (big == null || b.Cost > big.Cost) big = b;
                var bigCard = (RectTransform)UiKit.Find(content, "Box:" + big.Key);
                Assert.IsNotNull(UiKit.Find(bigCard, "Ten"), "큰 카드에만 «10회» 버튼");
                Assert.AreEqual(0.03f, bigCard.anchorMin.x, 1e-3f, "큰 카드 x = 표 ⑤ 배너(3.0)"); Assert.AreEqual(0.97f, bigCard.anchorMax.x, 1e-3f, "큰 카드 폭 = 94");
                Assert.AreEqual(1f - Layout.TabBar.Y / 100f, ((RectTransform)bar).anchorMax.y, 1e-3f, "탭 바 = 표 자리");
                int freeInv = (int)S.Gem; Assert.IsTrue(ClickNamed(bigCard.parent, "Ad"), "작은 카드의 광고(무료 보급) 버튼"); yield return Frames(1);
                Assert.AreEqual(freeInv + D.Gacha.DailyGem, S.Gem, 1e-6, "무료 보급 = dailyGem 지급"); Assert.IsFalse(_app.Overlay.IsOpen, "무료 보급은 팝업 없음");
                Assert.IsTrue(ClickNamed(bigCard, "Info"), "(i) 버튼"); yield return Frames(2);
                Check("상자 정보 팝업", expectOverlay: true); Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.popup"), "정보 팝업 = 공통 팝업 문법");
                AssertNoTextClip("상자 정보 팝업", _app.Overlay.Root);
                _app.Overlay.Close(); yield return Frames(1);
            }
            Check("상점");
            // T63-shop — 글자 가독성(주인 «글씨 너무 작다»): 상자 이름 = 제목(60 · Title 표식) · 섹션 헤더 = 표 ⑤ 높이에서 계산(≈50) · 상품 수량 = 띠 높이에서 계산(≈51) · 가격 버튼 글자 = 버튼 하한(44)
            // · 💎 글리프 0(Jua 에 없어 빈칸으로 그려짐 → hud.gem 아이콘 · 결정 142) · 상점 전 글자 잘림/하한 미달 0(TextAudit · 화면 단위 잘림 0 = 하위 행 ✅ 조건)
            {
                var content = UiKit.Find(shop, "Content");
                foreach (var box in D.Gacha.Boxes)
                {
                    var card = UiKit.Find(content, "Box:" + box.Key); Assert.IsNotNull(card, "상자 카드 " + box.Key);
                    Text title = null; foreach (var t in card.GetComponentsInChildren<Text>(false)) if (t.text == box.Name) title = t;
                    Assert.IsNotNull(title, "상자 이름 글자 " + box.Name);
                    Assert.GreaterOrEqual(title.fontSize, TextSize.Title, "상자 이름 «" + box.Name + "» = 제목 크기(60)"); Assert.AreEqual(TextKind.Title, TextAudit.KindOf(title), "상자 이름은 Title 표식");
                    var one = UiKit.Find(card, "One"); Assert.IsNotNull(one, "«1회» 버튼 " + box.Key);
                    Assert.IsNotNull(UiKit.Find(one, "Gem"), "«1회» 버튼 안 다이아 아이콘(hud.gem · 💎 글리프 대신) " + box.Key);
                    int priceTexts = 0;
                    foreach (var t in one.GetComponentsInChildren<Text>(false)) if (!string.IsNullOrEmpty(t.text)) { priceTexts++; Assert.GreaterOrEqual(t.fontSize, TextSize.Button, "«1회» 버튼 글자 «" + t.text + "» ≥ 버튼 하한(44)"); Assert.AreEqual(TextKind.Button, TextAudit.KindOf(t), "«1회» 버튼 글자 «" + t.text + "» 는 Button 표식"); }
                    Assert.AreEqual(2, priceTexts, "«1회» 버튼 = «1회» + 가격 두 글자 " + box.Key);
                }
                foreach (var t in ActiveTexts()) Assert.IsFalse((t.text ?? "").Contains("💎"), "상점 글자에 💎 글리프(Jua 폰트에 없어 빈칸) — " + PathOf(t.transform) + " :: " + t.text);
                int headers = 0;
                foreach (var t in ActiveTexts()) if (t.text == "다이아" || t.text == "골드") { headers++; Assert.GreaterOrEqual(t.fontSize, UiKit.FontForHeight(Layout.ShopSec1.H), "섹션 헤더 «" + t.text + "» 크기 = 표 ⑤ 헤더 높이(2.5%)에서 계산"); }
                Assert.GreaterOrEqual(headers, 2, "섹션 헤더 «다이아»·«골드»");
                var qty = UiKit.Find(UiKit.Find(content, "GemPack:0"), "Text_Title"); Assert.IsNotNull(qty, "다이아 카드 수량 글자");
                Assert.GreaterOrEqual(qty.GetComponent<Text>().fontSize, ShopScreen.QtySize, "상품 수량 크기 = 수량 띠 높이에서 계산(≈51)");
                AssertNoTextClip("상점", shop);
            }

            int inv = S.Inv.Count;
            Assert.IsTrue(Click(shop, s => s.Contains("1회")), "«1회» 뽑기 버튼"); yield return Frames(2);
            Check("뽑기 결과 팝업(1회)", expectOverlay: true);
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.popup"), "뽑기 결과 = 공통 팝업 문법(Popup_Box 패널 + 명판 + 격자 + 탭하여 닫기 · T40)");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "TapToClose"), "뽑기 결과: «탭하여 닫기»");
            {
                // T63-shop — 결과 팝업 안내 줄은 본문 40 한 줄(문구 줄임) · 팝업 글자 잘림 0(장비 칸 «gear:» 안 글자는 T63-gear 몫이라 제외)
                var note = UiKit.Find(_app.Overlay.Root, "Note"); Assert.IsNotNull(note, "결과 팝업 안내 줄(Note)");
                var nt = note.GetComponent<Text>(); Assert.GreaterOrEqual(nt.fontSize, TextSize.Body, "안내 줄 = 본문 하한");
                AssertNoTextClip("뽑기 결과 팝업", _app.Overlay.Root, skipPath: "gear:");
            }
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
            _app.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick));
            // T49 — 등장 연출: 연 직후엔 연출 중(카드는 존재하되 α 0 · 클릭 막힘) → 배경 탭 = 스킵 → 즉시 전부 표시(α 1 · 스케일 1 · 클릭 열림)
            Assert.IsTrue(_app.Overlay.Revealing, "연 직후엔 등장 연출 중");
            var cards = UiKit.Find(_app.Overlay.Root, "Group_Card"); Assert.IsNotNull(cards, "Group_Card"); Assert.AreEqual(offer.Count, cards.childCount, "카드 수 = 제안 수(연출 중에도 요소는 존재)");
            var cg0 = cards.GetChild(0).GetComponent<CanvasGroup>(); Assert.IsNotNull(cg0, "카드에 CanvasGroup(연출)"); Assert.AreEqual(0f, cg0.alpha, 1e-4f, "연 직후 첫 카드 α 0"); Assert.IsFalse(cg0.blocksRaycasts, "연출 중 카드 클릭 막힘");
            // T61 — 카드 프레임 조각의 Image 전부에 shine 머티리얼(AllIn1SpriteShaderUiMask · SHINE_ON · 카드마다 인스턴스) · shine 시작 시각이 카드 순서대로 단조 증가 · 글자엔 안 붙음
            {
                var starts = _app.Overlay.ShineStarts; Assert.AreEqual(offer.Count, starts.Count, "shine 수 = 카드 수(T61)");
                Assert.AreEqual(2 * UiKit.RevealStep + UiKit.ShineLead, starts[0], 1e-4f, "첫 shine = 첫 카드 Reveal 시작 + ShineLead");
                for (int i = 1; i < starts.Count; i++) Assert.Greater(starts[i], starts[i - 1], $"shine {i} 는 shine {i - 1} 보다 늦게 시작(등장 순서 = 반짝임 순서)");
                for (int i = 0; i < cards.childCount; i++)
                {
                    var c = cards.GetChild(i); var mo = c.GetComponent<UiKit.MaterialOwner>(); Assert.IsNotNull(mo, $"카드 {i} MaterialOwner"); Assert.IsNotNull(mo.Mat, $"카드 {i} shine 인스턴스");
                    Assert.AreEqual("PerkShine (Instance)", mo.Mat.name, $"카드 {i} 인스턴스 이름"); Assert.IsTrue(mo.Mat.IsKeywordEnabled("SHINE_ON"), $"카드 {i} SHINE_ON 키워드(에셋에 박힘 · WebGL 스트리핑 방지)");
                    Assert.AreEqual(UiKit.ShineFrom, mo.Mat.GetFloat(UiKit.ShineLocationId), 1e-4f, $"연 직후 카드 {i} 빛은 카드 밖(시작 값)");
                    var frame = UiKit.Find(c, "CardFrameArea"); Assert.IsNotNull(frame, $"카드 {i} CardFrameArea");
                    var imgs = frame.GetComponentsInChildren<Image>(true); Assert.Greater(imgs.Length, 0, $"카드 {i} 프레임 Image");
                    foreach (var im in imgs) { Assert.AreSame(mo.Mat, im.material, $"카드 {i} 프레임 Image «{im.name}» = 그 카드의 인스턴스"); Assert.IsTrue(im.material.shader.name.Contains("AllIn1SpriteShaderUiMask"), $"카드 {i} 프레임 쉐이더 = UiMask: {im.material.shader.name}"); }
                    var desc = UiKit.Find(c, "Text_Value"); var dt = desc != null ? desc.GetComponent<Text>() : null; Assert.IsNotNull(dt, $"카드 {i} 설명 글자"); Assert.IsFalse(dt.material != null && dt.material.shader != null && dt.material.shader.name.Contains("AllIn1"), $"카드 {i} 글자엔 shine 안 붙음(T52 한 색)");
                    var icon = UiKit.Find(c, "ItemFrameArea"); if (icon != null) foreach (var im in icon.GetComponentsInChildren<Image>(true)) Assert.AreNotSame(mo.Mat, im.material, $"카드 {i} 아이콘 조각 «{im.name}» 엔 shine 안 붙음");
                }
            }
            yield return Frames(2);
            Check("레벨업 팝업(연출 중)", expectOverlay: true);
            var dimT = UiKit.Find(_app.Overlay.Root, "Dimmed"); var skipTap = dimT != null ? dimT.GetComponent<UiKit.Tap>() : null; Assert.IsNotNull(skipTap, "배경 탭 = 스킵 핸들러"); skipTap.Fire(); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.Revealing, "배경 탭 → 연출 스킵"); Assert.IsTrue(_app.Overlay.IsOpen, "스킵은 닫지 않는다");
            for (int i = 0; i < cards.childCount; i++) { var c = (RectTransform)cards.GetChild(i); var cg = c.GetComponent<CanvasGroup>(); Assert.AreEqual(1f, cg.alpha, 1e-4f, $"스킵 뒤 카드 {i} α 1"); Assert.IsTrue(cg.blocksRaycasts, $"스킵 뒤 카드 {i} 클릭 열림"); Assert.AreEqual(1f, c.localScale.x, 1e-3f, $"스킵 뒤 카드 {i} 스케일 1"); }
            foreach (Transform c in cards) { var mo = c.GetComponent<UiKit.MaterialOwner>(); Assert.AreEqual(UiKit.ShineTo, mo.Mat.GetFloat(UiKit.ShineLocationId), 1e-3f, "스킵 뒤 shine 은 끝 값(빛이 카드 밖 · T61)"); }
            Check("레벨업 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s == "레벨 업!"), "제목");
            // T36 — 레퍼런스 04 구도: «새 특전을 고르세요» · 카드 = 등급 탭 + 팔각 아이콘 + 설명(한 색 · 수치 초록은 T52 로 취소) · «새로고침 무료» + «남은 횟수 : N» · 📘 · 상단 스탯 8칸 미니
            Assert.IsTrue(HasText(s => s == "새 특전을 고르세요"), "부제"); Assert.IsTrue(HasText(s => s == "새로고침 무료"), "새로고침 버튼"); Assert.IsTrue(HasText(s => s.StartsWith("남은 횟수 : ")), "남은 횟수");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "Stats"), "상단 스탯 미니 줄"); Assert.AreEqual(BattleScreen.StatDefs.Length, CountNamed(UiKit.Find(_app.Overlay.Root, "Stats"), "ic"), "미니 줄 아이콘 8");
            foreach (var p in offer) Assert.IsTrue(HasText(s => s == PerkText.Format(p.Desc)), $"카드 설명 = «트리거: 내용» 표기(T53) · 한 색(T52): {PerkText.Format(p.Desc)}");
            Assert.IsFalse(HasText(s => s.IndexOf("<color", StringComparison.OrdinalIgnoreCase) >= 0 && !s.StartsWith("남은 횟수")), "특전 글자에 부분 색(<color) 없음(T52 · «남은 횟수 : N» 의 주황 N 만 예외)");
            if (!string.IsNullOrEmpty(offer[0].GradeName)) Assert.IsTrue(HasText(s => s == offer[0].GradeName), "카드 왼쪽 위 등급 탭");
            // T63-perks — 등급 탭 글자: 본문 하한(40)을 최대치로 · bestFit 최소 32 · 세로 여백 0(칸 = 탭 전체라 bestFit 이 덜 줄인다) · 밝은 탭(회색·노랑) 위는 어두운 잉크(흰 글자는 대비가 없어 안 읽혔다)
            for (int i = 0; i < cards.childCount; i++)
            {
                string grade = offer[i].GradeName; if (string.IsNullOrEmpty(grade)) continue;
                Text gt = null;
                foreach (var t in cards.GetChild(i).GetComponentsInChildren<Text>(false)) if (t.text == grade) { gt = t; break; }
                Assert.IsNotNull(gt, $"카드 {i} 등급 탭 글자 «{grade}»");
                Assert.AreEqual(TextSize.Body, gt.resizeTextMaxSize, $"카드 {i} 등급 글자 최대 = 본문 하한 40(T63)");
                Assert.GreaterOrEqual(gt.resizeTextMinSize, TextSize.BestFitMin, $"카드 {i} 등급 글자 bestFit 최소 ≥ 32");
                Assert.IsTrue(gt.color == Palette.OnFrame(Palette.PerkGradeName(offer[i].Grade)), $"카드 {i} 등급 글자색 = 탭 밝기에 맞는 색(밝은 탭이면 잉크): {gt.color}");
                var host = gt.rectTransform.parent as RectTransform;
                Assert.IsNotNull(host, $"카드 {i} 등급 글자 부모(탭)");
                // 탭(TitleBg/Text_Title) 안으로 Stretch 한 경로일 때만 — 탭 조각이 없는 프레임은 Pct 로 자리를 잡으므로 높이가 같을 수 없다
                if (gt.rectTransform.anchorMin.y == 0f && gt.rectTransform.anchorMax.y == 1f)
                    Assert.AreEqual(host.rect.height, gt.rectTransform.rect.height, 0.5f, $"카드 {i} 등급 글자 칸 높이 = 탭 높이(세로 여백 0 · bestFit 이 덜 줄인다)");
                Assert.GreaterOrEqual(TextAudit.BestFitSize(gt), TextSize.BestFitMin, $"카드 {i} 등급 글자 실제 크기 ≥ 32");
            }
            // T63-perks — «남은 횟수 : N» 은 레퍼런스 04 처럼 버튼 «아래»(프리팹 자리 그대로면 버튼 위에 얹혀 아랫줄이 잘리고 주황 숫자가 주황 버튼에 묻힌다)
            {
                var foot = UiKit.Find(_app.Overlay.Root, "Button_02_Orange"); Assert.IsNotNull(foot, "하단 주황 버튼");
                Text remain = null;
                foreach (var t in foot.GetComponentsInChildren<Text>(false)) if (t.text != null && t.text.StartsWith("남은 횟수")) { remain = t; break; }
                Assert.IsNotNull(remain, "«남은 횟수» 글자");
                var c4 = new Vector3[4]; ((RectTransform)foot).GetWorldCorners(c4); float btnBottom = c4[0].y;
                remain.rectTransform.GetWorldCorners(c4); float remTop = c4[1].y, remBottom = c4[0].y;
                Assert.LessOrEqual(remTop, btnBottom, "«남은 횟수» 윗변이 버튼 아래끝보다 아래 = 버튼과 안 겹친다");
                Assert.Greater(remTop - remBottom, 0f, "«남은 횟수» 칸 높이 > 0");
            }
            var cardRts = new List<Transform>(); foreach (Transform c in cards) cardRts.Add(c);
            var first = cards.GetChild(0).GetComponent<Button>(); Assert.IsNotNull(first, "카드는 클릭 가능"); first.onClick.Invoke(); yield return Frames(3);
            Assert.AreEqual(1, G.Taken.Count, "특전 1개 획득"); Assert.IsFalse(_app.Overlay.IsOpen);
            Assert.IsFalse(UiKit.IsTweening(_app.Overlay.Root), "Close 뒤 팝업 층을 겨냥한 연출 시퀀스 0(T49)"); foreach (var c in cardRts) Assert.IsFalse(UiKit.IsTweening(c), "Close 뒤 카드를 겨냥한 트윈 0");
            Assert.AreEqual(0, CountShineInstances(), "Close 뒤 shine 머티리얼 인스턴스 0(카드 파괴 = MaterialOwner 가 인스턴스 파괴 · T61)");
            G.Pending = null;   // 엔진이 3초 동안 쌓아 둔 레벨업이 이어서 열렸을 수 있다 — 여기서는 팝업 하나씩만 본다
            Check("특전 선택 뒤(HUD 특전 줄 갱신)");

            // 보유 특전
            _app.Overlay.PerkBook(G, null); Assert.IsTrue(_app.Overlay.Revealing, "보유 특전도 카드 stagger(T49)"); yield return Frames(2);
            UiKit.CompleteAllTweens(); Assert.IsFalse(_app.Overlay.Revealing, "CompleteAll 뒤 연출 끝");
            Check("보유 특전 팝업", expectOverlay: true);
            // T36 — 레퍼런스 05 구도: 명판 «특전» · 긴 패널 · 카드 세로 나열 · «탭하여 닫기»(닫기 버튼 없음 · 배경 탭으로 닫힘)
            Assert.IsTrue(HasText(s => s == "특전"), "보유 특전 명판"); Assert.IsTrue(HasText(s => s == "탭하여 닫기"), "탭하여 닫기 안내");
            Assert.IsFalse(HasText(s => s == "닫기"), "닫기 버튼 없음(공통 팝업 문법)");
            Assert.AreEqual(1, UiKit.Find(_app.Overlay.Root, "Content").childCount, "얻은 특전 1개 = 카드 1장");
            // T61 — 보유 특전도 같은 규칙: 보이는 카드 1장 = shine 1 · CompleteAll 뒤 끝 값
            Assert.AreEqual(1, _app.Overlay.ShineStarts.Count, "보유 특전 카드 1장 = shine 1(T61)");
            { var bookMo = UiKit.Find(_app.Overlay.Root, "Content").GetChild(0).GetComponent<UiKit.MaterialOwner>(); Assert.IsNotNull(bookMo, "보유 특전 카드 MaterialOwner"); Assert.AreEqual(UiKit.ShineTo, bookMo.Mat.GetFloat(UiKit.ShineLocationId), 1e-3f, "CompleteAll 뒤 shine 끝 값"); }
            Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Dimmed"), "배경 탭"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen, "배경 탭으로 닫힌다");
            yield return Frames(2); Assert.AreEqual(0, CountShineInstances(), "보유 특전 닫은 뒤 shine 인스턴스 0(T61)");

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

            // 일시정지(설정과 같은 팝업 · T41) → «재개» · 배경 탭도 재개
            bool resumed = false;
            _app.Overlay.Pause(() => resumed = true, () => { }); yield return Frames(2);
            Check("일시정지 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s == "일시정지"), "제목"); Assert.IsTrue(HasText(s => s == "포기하고 로비로"), "포기 버튼"); Assert.IsTrue(HasText(s => s == "음악"), "일시정지에도 음악 줄");
            Assert.IsFalse(HasText(s => s == "데이터 삭제"), "전투 중엔 데이터 삭제 없음");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "재개"), "재개"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen); Assert.IsTrue(resumed, "재개 콜백");
            resumed = false; _app.Overlay.Pause(() => resumed = true, () => { }); yield return Frames(1);
            Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Dimmed"), "배경 탭"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen); Assert.IsTrue(resumed, "배경 탭 = 재개");

            // 클리어(Play_Result_Win_01) → 로비로 · 사망(Play_Result_Lose) → 로비로 (콜백은 빈 것 — 화면 전환은 아래서)
            _app.Overlay.Clear(G, false, () => { }, () => { });
            // T49 — 승리 팝업도 순서대로: 연 직후 버튼은 α 0(존재는 한다) → CompleteAll 뒤 α 1 · 골드 숫자 = 최종값
            Assert.IsTrue(_app.Overlay.Revealing, "클리어 팝업 등장 연출 중");
            var winBtns = UiKit.Find(_app.Overlay.Root, "Group_Buttons"); Assert.IsNotNull(winBtns, "Group_Buttons"); Assert.AreEqual(0f, winBtns.GetChild(1).GetComponent<CanvasGroup>().alpha, 1e-4f, "연 직후 «그냥 받기» α 0");
            yield return Frames(2);
            UiKit.CompleteAllTweens(); Assert.IsFalse(_app.Overlay.Revealing, "CompleteAll 뒤 연출 끝");
            Assert.AreEqual(1f, winBtns.GetChild(0).GetComponent<CanvasGroup>().alpha, 1e-4f, "×2 버튼 α 1"); Assert.AreEqual(1f, winBtns.GetChild(1).GetComponent<CanvasGroup>().alpha, 1e-4f, "그냥 받기 α 1");
            var rewardCell = UiKit.Find(_app.Overlay.Root, "Group_RewardItem"); Assert.IsNotNull(rewardCell, "Group_RewardItem"); Assert.AreEqual(UiKit.Fmt(G.Gold), rewardCell.GetChild(0).GetComponentInChildren<Text>(true).text, "골드 카운트업 최종값 = G.Gold");
            Check("클리어 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s == "클리어!"), "제목"); Assert.IsTrue(HasText(s => s == "광고 보고 보상 ×2 받기"), "광고 ×2 버튼(프리팹 Get x2 자리 · T23)");
            Assert.IsFalse(HasText(s => s == "다음 챕터"), "«다음 챕터» 버튼은 없다(T23 · 로비의 챕터 화살표로)");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "그냥 받기"), "그냥 받기(프리팹 Home 자리)"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen);
            Assert.IsFalse(UiKit.IsTweening(_app.Overlay.Root), "Close 뒤 연출 시퀀스 0");
            _app.Overlay.Dead(G, () => { });
            // T49 — 사망 팝업: 팁 3줄이 한 줄씩 · 배경 탭 = 연출 중이면 스킵(닫히지 않음) · 끝난 뒤면 로비로
            Assert.IsTrue(_app.Overlay.Revealing, "사망 팝업 등장 연출 중");
            var tipList = UiKit.Find(_app.Overlay.Root, "Group_List"); Assert.IsNotNull(tipList, "Group_List"); Assert.AreEqual(0f, tipList.GetChild(2).GetComponent<CanvasGroup>().alpha, 1e-4f, "연 직후 셋째 팁 α 0");
            Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Dimmed"), "배경 탭(연출 중)"); yield return Frames(1);
            Assert.IsTrue(_app.Overlay.IsOpen, "연출 중 배경 탭 = 스킵(닫히지 않는다)"); Assert.IsFalse(_app.Overlay.Revealing, "스킵 → 연출 끝");
            for (int i = 0; i < 3 && i < tipList.childCount; i++) Assert.AreEqual(1f, tipList.GetChild(i).GetComponent<CanvasGroup>().alpha, 1e-4f, $"스킵 뒤 팁 {i} α 1");
            yield return Frames(1);
            Check("사망 팝업", expectOverlay: true);
            Assert.IsTrue(HasText(s => s == "쓰러졌다..."), "제목");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "로비로"), "로비로"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen);
            Assert.IsFalse(UiKit.IsTweening(_app.Overlay.Root), "Close 뒤 연출 시퀀스 0");
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
