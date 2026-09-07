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
        /// <summary>
        /// 같은 글자가 <paramref name="root"/> 아래 <b>몇 군데</b> 있는가 — «중복이니 빼라»(T168) 처럼 «하나만 있어야 한다» 를 재는 자리에 쓴다.
        /// <para>
        /// <b>꺼진 것도 센다.</b> 탭 바는 «켜진 탭만 글자»(조각의 Focus/Normal 전환)라 다른 탭의 라벨은 꺼져 있다 —
        /// 켜진 것만 세면 «탭에 이벤트 라벨이 있다» 가 로비에서 0 으로 나온다(CI #322 에서 내가 그렇게 틀렸다).
        /// 우리가 재려는 것은 «그 글자를 가진 자리가 몇 개인가» 이지 «지금 보이는가» 가 아니다.
        /// </para>
        /// </summary>
        static int CountTextIn(Transform root, Func<string, bool> pred)
        {
            int n = 0;
            foreach (var t in root.GetComponentsInChildren<Text>(true)) if (t != null && pred(t.text ?? "")) n++;
            return n;
        }

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
        /// <summary>
        /// T120 — 이 사각형이 <b>프레임 안</b>(0~100%)에 들어오는가. 프레임 가장자리에 붙는 요소(모서리 버튼·기둥)가 밖으로 나가면
        /// 글자·테두리·잘림 게이트는 전부 통과하는데 화면에서만 잘려 보인다(칸 «안» 은 멀쩡하고 칸이 화면 밖이기 때문).
        /// 앵커는 <see cref="UiKit.Pct"/> 가 넣은 프레임 비율이라 그대로 재면 된다. 반올림 몫으로 0.1%p 는 봐준다.
        /// </summary>
        static void AssertInsideFrame(string what, RectTransform rt)
        {
            const float eps = 1e-3f;
            Assert.GreaterOrEqual(rt.anchorMin.x, -eps, what + " 왼쪽이 화면 밖으로 나갔다(" + (rt.anchorMin.x * 100f).ToString("0.0") + "%)");
            Assert.LessOrEqual(rt.anchorMax.x, 1f + eps, what + " 오른쪽이 화면 밖으로 나갔다(" + (rt.anchorMax.x * 100f).ToString("0.0") + "%)");
            Assert.GreaterOrEqual(rt.anchorMin.y, -eps, what + " 아래가 화면 밖으로 나갔다");
            Assert.LessOrEqual(rt.anchorMax.y, 1f + eps, what + " 위가 화면 밖으로 나갔다");
        }

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
        /// <summary>T63 «bestFit 이 안 줄임» 계약 — root 아래 이름이 <paramref name="name"/> 인 활성 Text 전부의 실제 크기(<see cref="TextAudit.BestFitSize"/> · bestFit 이 아니면 fontSize)가 <paramref name="min"/> 이상. 하나도 없으면 실패(이름 계약이 깨진 것).</summary>
        static void AssertUsedAtLeast(string where, Transform root, string name, int min)
        {
            Canvas.ForceUpdateCanvases();
            int n = 0; var bad = new List<string>();
            foreach (var t in root.GetComponentsInChildren<Text>(false))
            {
                if (t.name != name || !t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                n++; int used = t.resizeTextForBestFit ? TextAudit.BestFitSize(t) : t.fontSize;
                if (used < min) bad.Add($"«{t.text}» 실제 {used} < {min} (rect {t.rectTransform.rect.width:0}×{t.rectTransform.rect.height:0} · pref {t.preferredWidth:0}×{t.preferredHeight:0})");
            }
            Assert.Greater(n, 0, $"[{where}] 이름 «{name}» 인 활성 Text 가 없다");
            Assert.AreEqual(0, bad.Count, $"[{where}] «{name}» 글자가 {min} 아래로 줄었다(T63):\n" + string.Join("\n", bad));
        }
        /// <summary>특전 카드의 오브젝트 이름 — <see cref="UiKit.Spawn"/> 이 카탈로그 키를 그대로 이름으로 준다(프리팹은 ListItem_StageBuff_02).
        /// 카드 안 글자는 T63-perks 몫이라 결과·이벤트 팝업의 «잘림 0» 에서 뺀다.</summary>
        const string PerkCardName = "ui.card";
        /// <summary>T63-results — 글자가 bestFit 에 눌려 하한 밑으로 그려지지 않는지 + 칸 안에 들어가는지(줄 수 포함). <paramref name="min"/> 은 종류 하한.</summary>
        /// <summary>
        /// 팝업 어둠이 프레임 «밖» 까지 덮는가(T104) — 상단·하단 프레임 띠(T106)는 프레임 사각형 밖으로 <see cref="UiKit.DimOverscan"/> 만큼 뻗어 있어
        /// 어둠이 프레임까지만이면 팝업이 떠도 그 띠만 밝게 남는다(레퍼런스 12 는 재화 바·탭 바까지 전부 어둡다).
        /// </summary>
        static void AssertDimCoversFrame(Transform dim, string what)
        {
            Assert.IsNotNull(dim, what);
            var rt = dim as RectTransform; Assert.IsNotNull(rt, what + " 는 RectTransform");
            Assert.IsTrue(rt.gameObject.activeInHierarchy, what + " 는 켜져 있어야 한다");
            var frame = App.I.Frame;
            Assert.GreaterOrEqual(rt.rect.width, frame.rect.width + UiKit.DimOverscan, what + " 폭 = 프레임 + 밖 여유");
            Assert.GreaterOrEqual(rt.rect.height, frame.rect.height + UiKit.DimOverscan, what + " 높이 = 프레임 + 밖 여유");
            var img = rt.GetComponent<Image>(); Assert.IsNotNull(img, what + " 는 Image");
            Assert.IsTrue(img.raycastTarget, what + " 는 뒤 화면 클릭을 막는다");
        }

        /// <summary>
        /// 결과 팝업 보상 줄(Group_RewardItem)의 «값» 글자 — 첫 칸(GetItem_Reward)의 <b>직계</b> Text(«Text (TMP)» 자리 · 골드 숫자).
        /// 깊은 검색(<c>GetComponentInChildren&lt;Text&gt;(true)</c>)으로 집으면 T69-overlay 가 칸 맨 뒤에 깐 `ItemFrame_01` 조각의
        /// 장식 글자를 먼저 집는다(T91 — 그 조각의 프리팹 자리 글자가 «Text» 라 골드 값과 비교가 깨졌고 배포까지 막혔다).
        /// </summary>
        static Text RewardValueText(Transform group)
        {
            Assert.Greater(group.childCount, 0, "보상 줄에 칸");
            var cell = group.GetChild(0);
            for (int i = 0; i < cell.childCount; i++) { var t = cell.GetChild(i).GetComponent<Text>(); if (t != null) return t; }
            var deep = cell.GetComponentInChildren<Text>(true); Assert.IsNotNull(deep, "보상 칸의 값 글자");
            return deep;
        }
        /// <summary>
        /// «몸통이 곧 그라데이션» 인 카드(T100 ⓓ 회차 2 · 레퍼런스 10 의 상자 카드) — 위·아래 두 조각의 tint 알파가 <b>1</b> 이어야 한다(결정 338).
        /// 회차 1 은 덧칠(0.55)이라 조각의 회색 바탕이 비쳐 색이 죽었다(레퍼런스 «Rare» #0182C3 ↔ 우리 #8997A2 실측).
        /// 두 조각은 서로 반대 방향 알파 램프라(<c>ui.gradTop1</c> 흰→투명 · <c>ui.gradBottom</c> 투명→흰) 둘 다 1 이라야 몸통이 그 두 색으로 덮인다.
        /// </summary>
        /// <summary>
        /// T164 — 화면 어디에도 켜진 «HighLight*»(아이템 칸 조각의 데모 하이라이트)가 없어야 한다.
        /// 세는 잣대는 <see cref="GearUi.HighlightPrefix"/> 로 끄는 쪽과 <b>같은 것</b>을 쓴다(둘이 갈리면 조각이 늘 때 게이트만 빨개진다).
        /// </summary>
        void AssertNoHighlights(string where)
        {
            int n = 0; string first = null;
            foreach (var t in _app.UiCanvas.GetComponentsInChildren<Transform>(false))
                if (t != null && t.name.StartsWith(GearUi.HighlightPrefix, StringComparison.Ordinal))
                { n++; if (first == null) first = t.name + "(부모 " + (t.parent != null ? t.parent.name : "-") + ")"; }
            Assert.AreEqual(0, n, "[" + where + "] 켜진 하이라이트가 " + n + "개 있다(T164 · 첫 자리 " + (first ?? "-") + ")");
        }
        /// <summary>이름이 <paramref name="name"/> 인 자손을 너비 우선으로(얕은 것 먼저) 찾는다 — T147 검사용.</summary>
        static Transform DeepFind(Transform root, string name)
        {
            var q = new Queue<Transform>(); q.Enqueue(root);
            while (q.Count > 0)
            {
                var t = q.Dequeue();
                for (int i = 0; i < t.childCount; i++) { var c = t.GetChild(i); if (c.name == name) return c; q.Enqueue(c); }
            }
            return null;
        }
        /// <summary>
        /// T147 — 그라데이션이 <b>보이는가</b>. 종전 검사(<see cref="UiKit.HasGradient"/>)는 «조각 안에 있는가» 만 봐서
        /// 다이아·골드 카드가 <b>초록인 채로 안 보였다</b>: 바탕 <c>Bg(Mask)</c> 가 중첩 조각 <c>ShopFrame_01</c> 안의 <b>손자</b>라
        /// 직계 검색이 못 찾고 형제 0(= 불투명 프레임 «뒤»)으로 떨어졌던 것이다(주인 2026-09-07 05:5X 지적 · 결정 374).
        /// 그래서 «있다» 가 아니라 <b>«바탕과 같은 부모에서 바탕보다 뒤에 그려진다»</b> 를 못 박는다 — 이 한 줄이 그 결함을 그대로 잡는다.
        /// </summary>
        static void AssertGradientAboveBg(Transform piece, string bgName, string what)
        {
            var bg = DeepFind(piece, bgName);
            Assert.IsNotNull(bg, what + " 카드 조각의 바탕 «" + bgName + "»");
            foreach (var name in new[] { UiKit.GradientTopName, UiKit.GradientBottomName })
            {
                var g = DeepFind(piece, name);
                Assert.IsNotNull(g, what + " 카드 «" + name + "» 조각");
                Assert.AreSame(bg.parent, g.parent,
                    what + " 카드 «" + name + "» 은 바탕(" + bgName + ")과 같은 부모에 있어야 한다 — 다른 부모면 불투명 프레임 뒤로 떨어져 안 보인다(T147)");
                Assert.Greater(g.GetSiblingIndex(), bg.GetSiblingIndex(),
                    what + " 카드 «" + name + "» 은 바탕보다 뒤에 그려져야 한다(형제 번호가 커야 위에 보인다 · T147)");
            }
        }
        /// <summary>
        /// T157 — 뽑기 결과 창의 두 가지(주인 2026-09-07 07:5X).
        /// ⓐ 얻은 칸 격자의 <b>가운데가 조각이 준 칸(<c>ItemFrame_01</c>)의 가운데</b>와 같은가 — 예전에는 그 칸을 안 보고
        ///   «화면 위 24%» 에 제 격자를 얹어 주인이 «썡둥맞은 위치» 라고 했다. 자리 수를 코드에 안 박았으므로 시험도 <b>조각에서 읽어</b> 맞댄다.
        /// ⓑ 배경 무늬가 <b>흐르는가</b> — 조각의 것은 정적 <c>Image</c> 라 안 움직인다. 우리 흐름은 <c>RawImage</c> 의 uvRect 트윈(T72 ①)이다.
        ///   «있는가» 가 아니라 «도는가» 를 잰다(T147 에서 배운 것 — 있기만 하면 초록인 자는 결함을 놓친다).
        /// </summary>
        void AssertChestSlotAndPattern(string what)
        {
            var root = _app.Overlay.Root;
            var slot = UiKit.Find(root, ShopScreen.ChestSlotName) as RectTransform;
            Assert.IsNotNull(slot, what + ": 조각이 준 칸(" + ShopScreen.ChestSlotName + ")이 있어야 한다 — 자리의 근거다");
            var grid = UiKit.Find(root, "Got") as RectTransform;
            Assert.IsNotNull(grid, what + ": 얻은 칸 격자(Got)");
            Canvas.ForceUpdateCanvases();
            var sc = (Vector2)slot.TransformPoint(slot.rect.center);
            var gc = (Vector2)grid.TransformPoint(grid.rect.center);
            Assert.AreEqual(sc.x, gc.x, 1.0f, what + ": 격자 가운데 x 가 조각 칸 가운데와 같아야 한다(T157 ⓐ · 주인 «ItemFrame_01 있는 곳에 아이템이 떠야»)");
            Assert.AreEqual(sc.y, gc.y, 1.0f, what + ": 격자 가운데 y 가 조각 칸 가운데와 같아야 한다(T157 ⓐ)");

            var pat = UiKit.Find(root, UiKit.PatternName) as RectTransform;
            Assert.IsNotNull(pat, what + ": 흐르는 무늬(" + UiKit.PatternName + ")");
            var raw = pat.GetComponent<RawImage>();
            Assert.IsNotNull(raw, what + ": 무늬는 RawImage 라야 uvRect 가 흐른다 — 조각의 정적 Image 그대로면 안 움직인다(T157 ⓑ)");
            Assert.IsTrue(UiKit.IsTweening(raw), what + ": 무늬가 실제로 돌아야 한다(주인 «뽑기 결과에서도 패턴들 움직여야 함» · T157 ⓑ)");
        }
        static void AssertSolidGradient(Transform piece, string what, string paletteName)
        {
            // 회차 3 — 바탕도 두 색의 «가운데 색» 이라야 한다(결정 344). 두 조각이 가운데서 교차하며 반쯤만 덮으므로
            // 비치는 것이 조각의 회색이면 채도가 죽는다(실측: 희귀 0.60 ↔ 레퍼런스 0.98).
            var bg = UiKit.Find(piece, "Bg");
            Assert.IsNotNull(bg, what + " 카드 조각의 «Bg»");
            var bgImg = bg.GetComponent<Image>();
            Assert.IsNotNull(bgImg, what + " 카드 «Bg» 그림");
            var pair = GradientPalette.Of(paletteName);
            var mid = Color.Lerp(pair.Top, pair.Bottom, 0.5f);
            Assert.Less(Mathf.Abs(bgImg.color.r - mid.r) + Mathf.Abs(bgImg.color.g - mid.g) + Mathf.Abs(bgImg.color.b - mid.b), 0.02f,
                what + " 카드 바탕 = 두 색의 가운데 색(회색 바탕이 비치면 색이 죽는다 · T100 ⓓ 회차 3)");
            foreach (var name in new[] { UiKit.GradientTopName, UiKit.GradientBottomName })
            {
                var g = UiKit.Find(piece, name);
                Assert.IsNotNull(g, what + " 카드 «" + name + "» 조각");
                var img = g.GetComponent<Image>();
                Assert.IsNotNull(img, what + " 카드 «" + name + "» 그림");
                Assert.GreaterOrEqual(img.color.a, 0.99f, what + " 카드 «" + name + "» 는 몸통을 꽉 채운다(덧칠이면 조각의 회색 바탕이 비쳐 색이 죽는다 · T100 ⓓ 회차 2)");
            }
        }
        static void AssertReadable(Text t, int min, string what)
        {
            Assert.IsNotNull(t, what + " 글자가 있어야 한다");
            Assert.GreaterOrEqual(TextAudit.BestFitSize(t), min, $"{what} «{t.text}» 를 bestFit 이 {min} 밑으로 줄인다(실제 {TextAudit.BestFitSize(t)})");
            Assert.LessOrEqual(t.preferredHeight, t.rectTransform.rect.height + 1f, $"{what} «{t.text}» 가 칸({t.rectTransform.rect.height:0}) 을 넘친다");
        }
        /// <summary>살아 있는 shine 머티리얼 인스턴스 수(T61 · 카드가 파괴되면 0 이어야 한다 — 에셋 «PerkShine» 자체는 이름이 달라 안 센다).</summary>
        /// <summary>
        /// <b>임자 없는</b> shine 머티리얼 인스턴스 수(T61 계약 «카드가 죽으면 인스턴스도 죽는다» 의 자).
        /// <para>
        /// 전에는 씬 전체의 «PerkShine (Instance)» 를 세었다 — 특전 카드가 <b>유일한</b> 사용자일 때는 그것이 곧 누수 개수였다.
        /// T166 ⓑ 로 <b>일부러 오래 사는</b> 두 번째 사용자(로비 챕터 카드의 되풀이 shine)가 생기면서 그 셈은 «살아 있는 정상 인스턴스» 까지 세어 버렸다.
        /// 그래서 재는 것을 <b>성질</b>로 바꾼다: 살아 있는 <see cref="UiKit.MaterialOwner"/> 가 <b>아무도 안 쥔</b> 인스턴스 = 누수.
        /// 이 자는 종전보다 <b>더 세다</b> — 다른 카드가 살아 있는 동안에도 누수를 잡는다(종전에는 개수가 0 이 아니면 무조건 빨강이라 그런 상황을 아예 못 쟀다).
        /// </para>
        /// </summary>
        static int CountShineInstances()
        {
            var owned = new HashSet<Material>();
            foreach (var mo in Resources.FindObjectsOfTypeAll<UiKit.MaterialOwner>()) if (mo != null && mo.Mat != null) owned.Add(mo.Mat);
            int n = 0;
            foreach (var m in Resources.FindObjectsOfTypeAll<Material>()) if (m != null && m.name == "PerkShine (Instance)" && !owned.Contains(m)) n++;
            return n;
        }

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
            // T107 이 «맨 오른쪽 = 탤런트» 로 정한 것을 **T168 이 주인 지시로 «이벤트» 로 뒤집었다**(«탤런트 대신 이벤트를 하단 네비 탤런트 자리에»).
            Assert.AreEqual(5, NavBar.Keys.Length, "탭 = 상점·장비·전투·펫·이벤트");
            CollectionAssert.AreEqual(new[] { "shop", "gear", "battle", "pet", "events" }, NavBar.Keys, "탭 순서(T107 → 맨 오른쪽은 T168)");
            CollectionAssert.AreEqual(new[] { "상점", "장비", "전투", "펫", "이벤트" }, NavBar.Labels, "탭 라벨(T168)");
            CollectionAssert.DoesNotContain(NavBar.Keys, "dungeon", "던전 탭 없음(T107 · 로비 «이벤트» 로만 연다)");
            // T167 — 하단 탭 다섯에 빨간 점(주인 «장비 쪽에 점 있는 상황이면 장비 하단 네비에도 · 상점도 · 다른 모든 하단 네비 다 마찬가지로»).
            // 점은 «그 탭에 들어가면» 이 아니라 **조건이 사라져야** 꺼진다 → 조건을 만들고 없애며 켜짐/꺼짐을 잰다.
            {
                for (int i = 0; i < NavBar.Keys.Length; i++)
                {
                    var d = UiKit.Find(tabs.GetChild(i), NavBar.TabDotName);
                    Assert.IsNotNull(d, "탭 «" + NavBar.Keys[i] + "» 에 점 조각이 서 있어야 한다(꺼져 있어도 있다 · T167)");
                    var drt = (RectTransform)d; var sz = drt.rect.size;
                    Assert.AreEqual(sz.x, sz.y, 1f, "탭 점은 정사각이라야 한다(T136 계약 · 지금 " + sz.x.ToString("0") + "×" + sz.y.ToString("0") + ")");
                }
                Transform GearDot() => UiKit.Find(tabs.GetChild(System.Array.IndexOf(NavBar.Keys, "gear")), NavBar.TabDotName);
                // 조건을 없앤다 — 인벤을 비우면 «새것도 합성거리도 없다»
                _app.Save.Inv.Clear();
                _app.ShowScreen("lobby"); yield return Frames(1);
                Assert.IsFalse(GearDot().gameObject.activeSelf, "장비에 할 일이 없으면 장비 탭 점이 꺼진다(T167)");
                // 조건을 만든다 — 안 본 새 장비 하나
                Give("weapon").IsNew = true;
                _app.ShowScreen("lobby"); yield return Frames(1);
                Assert.IsTrue(GearDot().gameObject.activeSelf, "안 본 새 장비가 있으면 장비 탭 점이 켜진다(T167)");
                _app.Save.Inv.Clear();
                _app.ShowScreen("lobby"); yield return Frames(1);
                Assert.IsFalse(GearDot().gameObject.activeSelf, "조건이 사라지면 다시 꺼진다 — «봤다» 상태를 새로 만들지 않는다(T167 4항)");
            }
            Assert.GreaterOrEqual(UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length, 1, "로비 초상(HeroView · 상단 바 아바타)");
            Assert.IsTrue(HasText(s => s == "START"), "START 버튼");
            // T120 — «모서리 요소가 화면 밖으로 나가지 않는다» 게이트(주인·워커 눈에만 보이던 종류 · 배치 표에 이름표가 없는 자리는 ui_score 도 못 잰다).
            // 프레임 가장자리에 붙는 것들이 대상이다 — 하나라도 0~100% 밖으로 삐져나오면 여기서 잡는다.
            // («Events» = 로비 오른쪽 아래 모서리 버튼이었는데 T168 로 삭제됐다 → 목록에서 뺐다.)
            foreach (var n in new[] { "SubRow", "ChapterCard", "Menu", "Start" })
            {
                var t = UiKit.Find(lobby, n) as RectTransform;
                if (t == null) continue;
                AssertInsideFrame("로비 «" + n + "»", t);
            }
            Assert.IsTrue(HasText(s => s.StartsWith("챕터")), "챕터 제목");
            // T34 — 레퍼런스 01_lobby.jpg 구도 단언: 상단 바(아바타·전투력·골드·보석) · 메뉴 · 사이드 1+3 · 카드+◀▶ · 보조 2 · START · 이벤트 · 탭 5 (T78 로 배너·성·스타터팩·7일 챌린지 삭제)
            {
                var top = UiKit.Find(lobby, "TopBar"); Assert.IsNotNull(top, "상단 재화 바(TopBar)");
                Assert.IsNotNull(UiKit.Find(top, "Avatar"), "상단 바 아바타 칸"); Assert.IsNotNull(UiKit.Find(top, "Power"), "상단 바 전투력 숫자");
                Assert.IsNotNull(UiKit.Find(top, "ResourceBar_Coin"), "골드 pill"); Assert.IsNotNull(UiKit.Find(top, "ResourceBar_Gem"), "보석 pill");
                // T78 — 이벤트 배너(시즌 패스)는 삭제 · 그 자리는 비워 둔다
                Assert.IsNull(UiKit.Find(lobby, "Banner"), "이벤트 배너는 삭제됐다(T78)"); Assert.IsNotNull(UiKit.Find(lobby, "Button_Menu"), "메뉴(≡)");
                // T148(주인 2026-09-07 «데일리기프트, 퀘스트, 출석, 특권은 로비에 걍 꺼내놓는게 나은듯 · 전처럼») — T96-menu 가 지웠던 사이드 기둥 둘이 돌아왔다
                Assert.IsNotNull(UiKit.Find(lobby, "SideL"), "왼쪽 사이드 기둥(특권 · T148)"); Assert.IsNotNull(UiKit.Find(lobby, "SideR"), "오른쪽 사이드 기둥(출석·데일리 기프트·퀘스트 · T148)");
                Assert.IsNotNull(UiKit.Find(UiKit.Find(lobby, "Button_Menu"), "MenuDot"), "메뉴(≡) 알림 점 자리(T96 ⓔ)");
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
                Assert.IsNull(UiKit.Find(lobby, "Castle"), "왼쪽 아래 «성» 은 삭제됐다(T78)");
                // T168 — 오른쪽 아래 «이벤트» 도 삭제됐다(주인 «중복이니 빼 주고»). 같은 입구는 하단 탭 맨 오른쪽에 있다.
                Assert.IsNull(UiKit.Find(lobby, "Events"), "오른쪽 아래 «이벤트» 는 삭제됐다(T168 · 탭과 중복)");
                Assert.IsTrue(HasText(s => s == "탐험") && HasText(s => s == "클리어 보상"), "보조 줄 라벨은 우리말");
                // 주인 지시의 진짜 계약은 «중복이니 빼라» 다 → **탭 바 «밖»에는 «이벤트» 글자가 하나도 없어야** 하고,
                // 탭 바 «안»에는 있어야 한다. 이렇게 나눠 재면 로비 프리팹에 남은 다른 조각이 무엇을 들고 있든 흔들리지 않는다.
                // 꺼진 것도 세는 까닭 = 탭 라벨은 «켜진 탭만» 보이므로 로비(전투 탭)에서 이벤트 라벨은 꺼져 있다(CI #322 에서 내가 이걸 놓쳐 빨갰다).
                int evInTabs = CountTextIn(tabs, s => s == "이벤트");
                Assert.GreaterOrEqual(evInTabs, 1, "탭 바에 «이벤트» 라벨이 있어야 한다(T168 · 다섯째 칸)");
                Assert.AreEqual(evInTabs, CountTextIn(lobby, s => s == "이벤트"), "«이벤트» 글자는 탭 바 밖에 하나도 없어야 한다(T168 · 주인 «중복이니 빼 주고»)");
                // T148(주인 «데일리기프트·퀘스트·출석·특권은 로비에 걍 꺼내놓는게 나은듯» → «전처럼») 이 **T96-menu 를 뒤집었다** —
                // 넷은 이제 로비 사이드 기둥에 «있어야» 한다. 예전 단언(«로비에 두 번 안 나온다»)은 그 지시 «전»의 계약이라 뒤집는다.
                // 꺼진 것도 세는 자로 본다(탭 라벨에서 겪은 함정과 같은 이유 · 결정 441).
                foreach (var n in new[] { "특권", "퀘스트", "출석" })
                    Assert.GreaterOrEqual(CountTextIn(lobby, t => t == n), 1, "«" + n + "» 은 로비에 나온다(T148 이 T96-menu 를 뒤집었다)");
                Assert.IsFalse(HasText(s => s == "스타터팩") || HasText(s => s == "7일 챌린지") || HasText(s => s == "시즌 패스") || HasText(s => s == "성"), "T78 삭제분 라벨 0");
                // T63-lobby — 아이콘 라벨(사이드 4 · 보조 2 · 이벤트)은 보조 하한(36)으로 2줄까지 잘림 없이: bestFit 이 줄이지 않고(TextGenerator 로 직접 굴려 36) · 선호 높이 ≤ 칸
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
                    // T67(CI #98 빨강 후속) · T78 · T168(이벤트 칸 삭제) · T148(사이드 기둥 4 복귀): «Side:*» 칸은 보조 2(탐험·클리어 보상) + 좌 1 + 우 3 = 6
                    Assert.AreEqual(6, captions, "아이콘 라벨 = 보조 2 + 사이드 좌 1 · 우 3(T148)");
                }
                // 배치 = 표 ①(±3%p) — START 는 카드와 같은 x·폭, 탭 바는 맨 아래
                var frame = _app.Frame; var start = (RectTransform)UiKit.Find(lobby, "Start"); var card = (RectTransform)UiKit.Find(lobby, "ChapterCard");
                Assert.AreEqual(Layout.LobbyStart.X, start.anchorMin.x * 100f, 0.5f, "START x"); Assert.AreEqual(Layout.LobbyCard.X + Layout.LobbyCard.W, card.anchorMax.x * 100f, 0.5f, "카드 오른쪽 = START 오른쪽");
                Assert.AreEqual(start.anchorMin.x, card.anchorMin.x, 1e-3f, "START 와 카드는 같은 x"); Assert.AreEqual(start.anchorMax.x, card.anchorMax.x, 1e-3f, "START 와 카드는 같은 폭");
                Assert.AreEqual(1f - Layout.TabBar.Y / 100f, ((RectTransform)tabs).anchorMax.y, 1e-3f, "탭 바 = 표 자리");
                // T98 — «클리어 보상» 도 더 이상 껍데기가 아니다(챕터 보상 페이지 · ChapterChestScreenTests 가 따로 본다) →
                // 보조 줄에 남은 껍데기 버튼은 이제 **하나도 없다**. 대신 둘 다 «제 것을 연다» 를 여기서 못 박는다.
                Assert.IsTrue(ClickNamed(lobby, "Side:" + LobbyScreen.SideClearReward), "클리어 보상 버튼");
                yield return Frames(2);
                Assert.AreEqual("chapterChest", _app.Current.Name, "«클리어 보상» 은 챕터 보상 페이지를 연다(T98)");
                Assert.IsNotNull(UiKit.Find(_app.Current.Root, "Banner"), "챕터 배너(T98)");
                _app.ShowScreen("lobby"); yield return Frames(2);
                lobby = _app.Current.Root;
                // T97 — «탐험» 은 팝업을 연다(껍데기 목록에서 빠진 것이 «눌러도 아무 일 없음» 으로 되돌아가지 않게 여기서 못 박는다)
                Assert.IsTrue(ClickNamed(lobby, "Side:" + LobbyScreen.SideExplore), "탐험 버튼");
                yield return Frames(1);
                Assert.IsTrue(_app.Overlay.IsOpen, "«탐험» 은 방치 보상 팝업을 연다(T97)");
                Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ExpeditionBox"), "탐험 팝업 상자(T97)");
                _app.Overlay.Close(); yield return Frames(1);
            }
            Check("로비");

            // T44 — 로비 사이드 껍데기: 팝업 3(퀘스트·출석·데일리 기프트 = 공통 팝업 문법 · 명판 · «탭하여 닫기» · 배경 탭 닫기 · X 없음) + 페이지 1(특권 · 상단 바 + 뒤로 ◀ · 탭 바 없음) — T78 로 7일 챌린지 팝업·시즌 패스 페이지는 삭제
            {
                // T148 — 다시 «로비 사이드 칸»(«Side:*»)이 연다(T96-menu 가 메뉴로 옮겼던 것을 주인 지시로 되돌렸다)
                (string key, string title, string mark)[] pops = { (LobbyScreen.SideQuest, "퀘스트", "새로고침까지"), (LobbyScreen.SideAttendance, "출석 보상", "7일차"), (LobbyScreen.SideDailyGift, "데일리 기프트", "광고 1회 보기") };
                foreach (var p in pops)
                {
                    Assert.IsTrue(ClickNamed(lobby, "Side:" + p.key), "로비 사이드 칸 " + p.key); yield return Frames(2);
                    Check("사이드 팝업 " + p.title, expectOverlay: true);
                    Assert.IsTrue(HasText(s => s == p.title), p.title + ": 명판"); Assert.IsTrue(HasText(s => s.Contains(p.mark)), p.title + ": 내용 «" + p.mark + "»");
                    // 닫기 X 없음 — 퀘스트 팝업은 프리팹(Progression_Mission_02)에 X 조각이 딸려 오므로 «지우지 않고 끈다»(T78) · UiKit.Find 는 꺼진 것도 집는다(결정 162)
                    Assert.IsTrue(HasText(s => s == "탭하여 닫기"), p.title + ": 탭하여 닫기");
                    { var x = UiKit.Find(_app.Overlay.Root, "Button_Close_01"); Assert.IsTrue(x == null || !x.gameObject.activeInHierarchy, p.title + ": 닫기 X 없음"); }
                    Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Dimmed"), p.title + ": 배경 탭"); yield return Frames(2); Assert.IsFalse(_app.Overlay.IsOpen, p.title + " 닫힘");
                }
                // 구도 단언 — 퀘스트: 박스 = 표 ⑳ · 줄 6 · 탭 3 · 트랙 보상 칸 5 / 출석: 칸 7 / 데일리: 선물 그림 · 광고 줄 4 · 타임라인 점 4
                LobbyPopups.Quest(_app); yield return Frames(1);
                Assert.AreEqual(6, CountNamed(_app.Overlay.Root, "Quest:"), "퀘스트 줄 6"); Assert.AreEqual(3, CountNamed(_app.Overlay.Root, "Tab:"), "퀘스트 탭 3"); Assert.AreEqual(5, CountNamed(_app.Overlay.Root, "Track:"), "트랙 보상 칸 5(+메달)");
                // T78 — 줄은 GUI Pro `Progression_Mission_02` 프리팹 조각이다: 줄마다 ListItem_Mission_02(제목·Slider·Group_Price) 가 살아 있고 · 앞 3줄은 «이동» · 뒤 3줄은 프리팹 ✅ · 영문 데모 문구 0(꺼진 여분 줄 제외)
                {
                    var q0 = UiKit.Find(_app.Overlay.Root, "Quest:0"); Assert.IsNotNull(q0, "퀘스트 줄 0");
                    Assert.IsNotNull(UiKit.Find(q0, "ListFrame_08"), "줄 = 프리팹 ListItem_Mission_02 조각(안쪽 바탕 ListFrame_08)");
                    Assert.IsNotNull(q0.GetComponentInChildren<Slider>(true), "줄 진행바 = 프리팹 Slider_02_Yellow");
                    Assert.IsNotNull(UiKit.Find(q0, "Group_Price"), "줄 보상 칸 = 프리팹 Group_Price");
                    Assert.AreEqual(3, CountNamed(_app.Overlay.Root, "GoBtn"), "미완 줄 «이동» 3(레퍼런스 15)");
                    int checks = 0; foreach (var t in _app.Overlay.Root.GetComponentsInChildren<Transform>(false)) if (t.name == "Check") checks++;
                    Assert.AreEqual(3, checks, "완료 줄 ✅ 3(프리팹 Check · 레퍼런스 15)");
                    Assert.IsTrue(HasText(s => s == "적 50마리 처치"), "줄 제목은 우리말");
                    // T78 — 줄 바탕(프리팹 ListFrame_08)이 어두워 제목은 흰 글자 + 외곽선이어야 읽힌다(screens run 148 눈 확인)
                    { var t0 = UiKit.Find(q0, "Title").GetComponent<Text>(); Assert.IsNotNull(t0, "줄 제목 글자"); Assert.IsNotNull(t0.GetComponent<TextOutline8>(), "줄 제목 외곽선"); Assert.Greater(t0.color.r + t0.color.g + t0.color.b, 2.4f, "줄 제목은 밝은 글자"); }
                }
                { var bx = (RectTransform)UiKit.Find(_app.Overlay.Root, "QuestBox"); Assert.IsNotNull(bx, "퀘스트 박스"); Assert.AreEqual(Layout.QsBox.X, bx.anchorMin.x * 100f, 0.5f, "퀘스트 박스 x = 표 ⑬"); Assert.AreEqual(1f - Layout.QsBox.Y / 100f, bx.anchorMax.y, 1e-3f, "퀘스트 박스 y = 표 ⑬"); }
                // T63-lobbypopups — 글자 잘림 0 + 제목/카운터가 본문 40 아래로 안 줄어듦(팝업 4종) · 리본 명판 60 이 안 잘림
                AssertNoTextClip("퀘스트 팝업", _app.Overlay.Root); AssertUsedAtLeast("퀘스트 제목", _app.Overlay.Root, "Title", TextSize.Body);
                _app.Overlay.Close(); yield return Frames(1);
                LobbyPopups.Attendance(_app); yield return Frames(1); Assert.AreEqual(7, CountNamed(_app.Overlay.Root, "Day:"), "출석 칸 7");
                // T133 — 출석 팝업(16) 두 가지가 되돌아가면 여기서 잡는다(둘 다 게이트가 못 재는 «대비·읽힘» 이라 단언으로 못 박는다).
                {
                    var ovA = _app.Overlay.Root;
                    // ⓑ «N일차» 머리 띠는 흰 글자가 뜰 만큼 어두워야 한다 — 레퍼런스 16 은 짙은 자주(휘도 ≈0.2)다.
                    var headT = UiKit.Find(ovA, "Head"); Assert.IsNotNull(headT, "«N일차» 머리 띠");
                    var headImg = headT.GetComponent<Image>(); Assert.IsNotNull(headImg, "머리 띠 그림");
                    float luma = 0.299f * headImg.color.r + 0.587f * headImg.color.g + 0.114f * headImg.color.b;
                    Assert.LessOrEqual(luma, 0.30f, $"머리 띠가 너무 밝다(휘도 {luma:0.00}) — 흰 글자와 대비가 없다(T133 ⓑ · 레퍼런스 ≈0.2)");
                    Assert.GreaterOrEqual(headImg.color.a, 0.99f, "머리 띠는 불투명이어야 한다 — 반투명이면 밝은 칸이 비쳐 다시 떠 버린다(T133 ⓑ)");
                    // ⓐ 수량 = 레퍼런스 16 처럼 «아이콘 오른쪽 아래에 굵게 얹힌 큰 숫자»(회차 2 · 결정 403).
                    // 회차 1 의 단언(«MiddleCenter · fontSize ≥ 하한 · 칸 폭 80%»)은 **초록인 채로 화면은 안 고쳐졌다** —
                    // bestFit 이 rect 안에서 글자를 다시 누르므로 `fontSize`(상한)는 «그려지는 크기» 가 아니었다.
                    // 그래서 이번에는 **rect 가 실제로 그만큼 크다** 를 잰다(그것이 bestFit 뒤 글자 크기의 상한이다).
                    var cellT = UiKit.Find(ovA, "Cell"); Assert.IsNotNull(cellT, "출석 보상 칸");
                    var qtyT = UiKit.Find(cellT, "Qty"); Assert.IsNotNull(qtyT, "보상 수량 글자");
                    var qty = qtyT.GetComponent<Text>(); Assert.IsNotNull(qty, "수량 Text");
                    Assert.AreEqual(TextAnchor.LowerRight, qty.alignment, "수량은 아이콘 오른쪽 아래(레퍼런스 16 · T133 ⓐ)");
                    Assert.GreaterOrEqual(qty.fontSize, TextSize.Body, $"수량 글자 상한이 본문 하한보다 작다({qty.fontSize} · T133 ⓐ)");
                    var qrt = qty.rectTransform;
                    // 앵커로 잰다 — 「칸 높이의 몇 %인가」가 곧 앵커 차이라, 레이아웃이 언제 잡히든 값이 같다(회차 1 단언이 쓴 방식 그대로).
                    float qtyPct = (qrt.anchorMax.y - qrt.anchorMin.y) * 100f;
                    Assert.GreaterOrEqual(qtyPct, LobbyPopups.QtyMinHeightPct,
                        $"수량 글자 칸이 칸 높이의 {qtyPct:0.0}% 뿐이다 — bestFit 이 여기까지 글자를 눌러 그림에 먹힌다"
                        + $"(회차 1 이 30% 라 21~25px 이 됐다 · 하한 {LobbyPopups.QtyMinHeightPct}% · T133 ⓐ)");
                }
                AssertNoTextClip("출석 팝업", _app.Overlay.Root);
                // T76 — 출석 팝업은 GUI Pro `Rewards_Daily7_Popup` 프리팹이다: 리본은 프리팹 Title_01_Deco_Yellow · 칸은 DailyFrame(상태 바탕) · 오늘(1일차)만 Bg_Focus1 · 받은 날 ✅ 0
                { var rib = UiKit.Find(_app.Overlay.Root, "Title_01_Deco_Yellow"); Assert.IsNotNull(rib, "출석 리본 = 프리팹 Title_01_Deco_Yellow"); var rt = rib.GetComponentInChildren<Text>(true); Assert.IsNotNull(rt, "출석 리본 글자"); Assert.AreEqual(TextKind.Title, TextAudit.KindOf(rt), "리본 = 제목 종류"); Assert.GreaterOrEqual(rt.rectTransform.rect.height, rt.preferredHeight, "리본 글자 rect ≥ 선호 높이(RibbonTextFit)"); }
                {
                    var d1 = UiKit.Find(_app.Overlay.Root, "Day:1"); Assert.IsNotNull(d1, "1일차 칸");
                    Assert.IsNotNull(UiKit.Find(d1, "Bg_Focus1"), "칸 = 프리팹 DailyFrame 조각(상태 바탕)");
                    Assert.IsTrue(UiKit.Find(d1, "Bg_Focus1").gameObject.activeInHierarchy, "오늘(1일차)은 Focus 바탕으로 강조");
                    var d2 = UiKit.Find(_app.Overlay.Root, "Day:2"); Assert.IsFalse(UiKit.Find(d2, "Bg_Focus1").gameObject.activeInHierarchy, "2일차는 보통 바탕");
                    Assert.IsTrue(UiKit.Find(d2, "Bg_Normal").gameObject.activeInHierarchy, "2일차 보통 바탕 켜짐");
                    int checks = 0; foreach (var t in _app.Overlay.Root.GetComponentsInChildren<Transform>(false)) if (t.name == "Check") checks++;
                    Assert.AreEqual(0, checks, "받은 날 없음(시스템 없음 · ✅ 0 · T44)");
                    Assert.IsNotNull(UiKit.Find(d1, "ItemFrame_01"), "보상 칸 = 장비 프레임(T76 3항 · T69 7항)");
                    Assert.IsTrue(HasText(s => s == "1일차") && HasText(s => s == "7일차"), "칸 머리는 우리말");
                    // T76 — 프리팹의 타이머 라벨(격자 위에 떠 3일차 칸을 가렸다)은 꺼져 있어야 한다(screens run 148)
                    { var tl = UiKit.Find(_app.Overlay.Root, "Label_Tail_02_Timer"); Assert.IsTrue(tl == null || !tl.gameObject.activeInHierarchy, "출석: 프리팹 타이머 라벨 잔재 꺼짐"); }
                }
                _app.Overlay.Close(); yield return Frames(1);
                // T77 — 데일리 기프트는 이제 «동작하는» 화면이다(껍데기 아님): 하루 상태를 초기화한 뒤 무료 칸 → 줄 1 순서로 연다
                var GD = _app.Data.DailyGift; Assert.IsNotNull(GD, "dailyGift.json 이 카탈로그(data.dailyGift)로 로드됐다");
                _app.Save.GiftDay = ""; _app.Save.GiftAds = 0; _app.Save.GiftFree = false; _app.Save.GiftClaimed.Clear();
                KkomaKnight.Core.DailyGift.Roll(_app.Save, GD, SaveStore.Today());
                double gem0 = _app.Save.Gem;
                LobbyPopups.DailyGift(_app); yield return Frames(1);
                Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "GiftPic"), "선물 그림");
                Assert.AreEqual(GD.Milestones.Count, CountNamed(_app.Overlay.Root, "Ad:"), "광고 줄 = dailyGift.json milestones 수(코드에 개수 없음)");
                // 주인 추가(2026-09-07 00:3X) — 왼쪽 노란 타임라인(선 + 육각 점)은 넣지 않는다 · 행은 상자 가로 중앙
                Assert.AreEqual(0, CountNamed(_app.Overlay.Root, "Dot:"), "타임라인 점 없음"); Assert.IsNull(UiKit.Find(_app.Overlay.Root, "Timeline"), "타임라인 선 없음");
                {
                    // 줄은 팝업 상자 안에 놓이므로 앵커는 «상자 기준 %» — 표 ㉒ 를 Within(GfBox) 로 환산해 대조한다(좌우 여백이 같아야 = 중앙 정렬)
                    var r0 = (RectTransform)UiKit.Find(_app.Overlay.Root, "Ad:0"); Assert.IsNotNull(r0, "광고 줄 1");
                    var inBox = Layout.GfRow1.Within(Layout.GfBox);
                    Assert.AreEqual(inBox.X, r0.anchorMin.x * 100f, 0.5f, "광고 줄 x = 표 ㉒(상자 기준)");
                    Assert.AreEqual(inBox.X, 100f - (inBox.X + inBox.W), 0.5f, "광고 줄 좌우 여백이 같다(중앙 정렬 · 주인 2026-09-07)");
                }
                AssertNoTextClip("데일리 기프트 팝업", _app.Overlay.Root); AssertUsedAtLeast("광고 줄 제목", _app.Overlay.Root, "Title", TextSize.Body);
                { var bar = (RectTransform)UiKit.Find(_app.Overlay.Root, "Bar"); Assert.IsNotNull(bar, "광고 줄 진행바"); Assert.AreEqual(Layout.LpBarH, (bar.anchorMax.y - bar.anchorMin.y) * ((RectTransform)bar.parent).rect.height / UiKit.FrameH * 100f, 0.05f, "진행바 높이 = LpBarH(프레임 %)"); }
                // ① 시작 = 무료 칸만 열려 있고 줄은 전부 «잠금»(위에서 아래로 순서대로)
                Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "TodayGetBtn"), "오늘의 선물 버튼");
                Assert.IsTrue(HasText(s => s == "잠금"), "무료 칸 전에는 줄이 잠겨 있다");
                Assert.IsFalse(HasText(s => s == "광고 보기"), "잠긴 줄에는 광고 버튼을 두지 않는다(한 번에 하나)");
                // ② 무료 칸 받기 → 다이아 + freeGift.gem · 줄 1 이 열려 «광고 보기» 로 바뀐다
                Assert.IsTrue(ClickNamed(_app.Overlay.Root, "TodayGetBtn"), "무료 칸 받기"); yield return Frames(2);
                Assert.AreEqual(gem0 + GD.FreeGem, _app.Save.Gem, 0.001, "무료 칸 = dailyGift.json freeGift.gem");
                Assert.IsTrue(HasText(s => s == "광고 보기"), "줄 1 이 열렸다");
                Check("데일리 기프트 무료 칸 수령", expectOverlay: true);
                // ③ 광고(모의 3초) → 누적 +1 → «받기» → 다이아 + milestones[0].gem
                Assert.IsTrue(ClickNamed(_app.Overlay.Root, "AdBtn"), "광고 보기"); yield return Frames(2);
                Assert.IsTrue(HasText(s => s == "광고 시청 중..."), "모의 광고 카운트다운(T23 과 같은 자리)");
                { float t0g = Time.realtimeSinceStartup; while (_app.Save.GiftAds == 0 && Time.realtimeSinceStartup - t0g < 10f) yield return Frames(1); }
                Assert.AreEqual(1, _app.Save.GiftAds, "광고 1회 누적");
                yield return Frames(2);
                Assert.IsTrue(HasText(s => s == "받기"), "누적이 닿아 «받기» 로 바뀐다");
                double gem1 = _app.Save.Gem;
                Assert.IsTrue(ClickNamed(_app.Overlay.Root, "AdBtn"), "줄 1 받기"); yield return Frames(2);
                Assert.AreEqual(gem1 + GD.Milestones[0].Gem, _app.Save.Gem, 0.001, "줄 1 = dailyGift.json milestones[0].gem");
                Assert.IsTrue(KkomaKnight.Core.DailyGift.Claimed(_app.Save, 0), "줄 1 수령 기록");
                Assert.IsFalse(KkomaKnight.Core.DailyGift.CanClaim(_app.Save, GD, 0, SaveStore.Today()), "같은 줄 두 번은 못 받는다");
                AssertNoTextClip("데일리 기프트 팝업(수령 뒤)", _app.Overlay.Root);
                Check("데일리 기프트 광고 → 수령", expectOverlay: true);
                _app.Overlay.Close(); yield return Frames(1);
                Check("사이드 팝업 3종 열고 닫음");
                // 페이지 1(특권)
                Assert.IsTrue(ClickNamed(lobby, "Side:" + LobbyScreen.SidePrivilege), "로비 «특권» 칸(T148)"); yield return Frames(3);
                Assert.AreEqual("privilege", _app.Current.Name, "특권 페이지"); var pv = _app.Current.Root;
                Assert.IsNotNull(UiKit.Find(pv, "TopBar"), "특권: 상단 바"); Assert.AreEqual(4, CountNamed(pv, "Card:"), "특권 카드 4"); Assert.IsTrue(HasText(s => s == "특권") && HasText(s => s == "전체 받기"), "특권: 제목 · 전체 받기");
                Assert.IsNull(UiKit.Find(pv, "ui.tabBar"), "특권: 탭 바 없음"); Assert.IsFalse(HasText(s => s == "START"), "로비는 숨겨져 있다");
                // T63-lobbypopups — 특권: 잘림 0 · 부제 40 안 줄어듦(문구 «활성화해») · 제목 «특권» 은 제목 종류 60
                AssertNoTextClip("특권 페이지", pv); AssertUsedAtLeast("특권 부제", pv, "Sub", TextSize.Body);
                { Text pt = null; foreach (var t in pv.GetComponentsInChildren<Text>(false)) if (t.text == "특권") pt = t; Assert.IsNotNull(pt, "«특권» 글자"); Assert.AreEqual(TextKind.Title, TextAudit.KindOf(pt), "«특권» = 제목 종류"); Assert.GreaterOrEqual(TextAudit.BestFitSize(pt), TextSize.Title, "«특권» 실제 크기 ≥ 60");
                  // T170 회차 3 — 위 한 줄이 55 로 빨개졌을 때 «왜» 를 바로 말해 주는 자(결정 490): 60 이 들어가려면 칸이 한 줄(≈66px)보다 커야 하고
                  // 그 칸을 세운 것은 Label(…, -10, …, 120) 이다. 가운데 정렬 함수가 세로를 0/100 으로 덮으면 70px 이 되어 bestFit 이 글자를 줄인다.
                  float ptH = pt.rectTransform.rect.height;   // 앵커 비율이 아니라 «놓이고 난 실제 px» 를 잰다(부모가 줄이라 비율만 보면 헛값이다)
                  Assert.GreaterOrEqual(ptH, TextSize.Title * 1.2f, "«특권» 글자 칸 세로(px) — 가운데 정렬은 가로만 옮긴다(세로를 덮으면 여기서 먼저 빨개진다)"); }
                Check("특권 페이지");
                Assert.IsTrue(ClickNamed(pv, "BackBtn"), "특권 뒤로"); yield return Frames(2); Assert.AreEqual("lobby", _app.Current.Name, "뒤로 → 로비");
                // T78 — 시즌 패스 페이지(이벤트 배너 진입)는 삭제됐다
                Check("로비 복귀");
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
            AssertDimCoversFrame(UiKit.Find(_app.Overlay.Root, "Dimmed"), "설정 팝업 어둠");   // T104 — 프레임 밖(레터박스·노치·상하 프레임 띠)까지
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

            // T168 — 맨 오른쪽(다섯째) 탭은 **«이벤트»** 이고 누르면 **던전 페이지**가 열린다(T107 이 정한 «이벤트는 무조건 던전부터» 를 지킨다).
            // 앞 블록이 세이브를 지우며 로비를 다시 세우므로 탭 바를 새로 찾는다. 탭은 자리번호가 아니라 **이름**으로 집는다(T168 의 «Tab:<키>»).
            {
                _app.ShowScreen("lobby"); yield return Frames(2);
                var tabs2 = UiKit.Find(_app.Current.Root, "Tab_01_BottomFlushMenu"); Assert.IsNotNull(tabs2, "로비 탭 바(다시)");
                var evTab = UiKit.Find(tabs2, NavBar.TabName("events")); Assert.IsNotNull(evTab, "다섯째 탭 = 이벤트(T168)");
                Assert.AreSame(tabs2.GetChild(4), evTab, "이벤트 탭은 맨 오른쪽 칸이다(주인 «탤런트 자리에»)");
                var evBtn = evTab.GetComponent<Button>(); Assert.IsNotNull(evBtn, "이벤트 탭 버튼");
                evBtn.onClick.Invoke(); yield return Frames(2);
                Assert.AreEqual("events", _app.Current.Name, "이벤트 탭 = 이벤트 화면(팝업이 아니다)");
                Assert.AreEqual(EventsScreen.PageDungeon, ((EventsScreen)_app.Current).Page, "이벤트는 던전 페이지부터(T107)");
                Assert.IsFalse(_app.Overlay.IsOpen, "팝업은 안 뜬다");
                // 「탤런트」 팝업은 코드로 남아 있고 부르는 곳만 없어졌다(결정 416) — 조각 자체는 그대로임을 여기서 한 번 확인해 둔다.
                _app.Overlay.TalentPet("talent"); yield return Frames(2);
                Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.talent"), "Character_Talent_02 조각은 그대로 있다(입구만 없다 · 주인이 자리를 정하면 붙인다)");
                Check("탤런트 팝업(입구 없음 · 조각만 확인)", expectOverlay: true);
                _app.Overlay.Close(); yield return Frames(2);
                Assert.IsFalse(_app.Overlay.IsOpen, "닫힘");
                _app.ShowScreen("lobby"); yield return Frames(2);
            }

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
                // T164 회차 2 — 펫 화면은 칸 조각을 **겹쳐** 세운다(바깥 `ui.itemFrame.empty` 안 `NormalArea` 에 색 변형 하나 더).
                // 회차 1 은 `UiKit.Find`(이름마다 «첫 하나»)로 껐기 때문에 겹친 칸에서 **둘이 남아 켜져 있었다** — 던전(20)이 그래서 빨갰다.
                // 겹쳐 세우는 자리를 직접 재 둔다(20 은 EventsScreenTests 가 잰다 · 결정 433).
                AssertNoHighlights("13_pet");
                // T178 — 주인이 던전 20 에서 지우라고 한 «준비 중» 표기가 이 화면에도 남아 있었다(소환 버튼 가격 자리).
                // 값을 지어내지 않고 «흐리게 + 누르면 까닭을 토스트» 로 바꿨다(T99 티켓과 같은 문법 · 결정 432).
                foreach (var n in new[] { "SummonBtn", "Summon10Btn" })
                {
                    var btn = UiKit.Find(pet, n);
                    Assert.IsNotNull(btn, "펫 소환 버튼 " + n);
                    foreach (var t in btn.GetComponentsInChildren<Text>(true))
                        StringAssert.DoesNotContain("준비 중", t.text, n + " 안에 «준비 중» 글자가 남으면 안 된다(T178 · 주인이 던전에서 지우라 한 그 표기)");
                    var cg = btn.GetComponent<CanvasGroup>();
                    Assert.IsNotNull(cg, n + " 는 «못 누르는 것» 으로 보여야 한다(CanvasGroup 알파 · T178)");
                    Assert.AreEqual(0.5f, cg.alpha, 0.01f, n + " 알파 0.5(흐리게 · T99 티켓과 같은 문법)");
                }
                // 껍데기 버튼·슬롯 — 눌러도 아무 일 없음(팝업 안 열림 · 화면 그대로 · 빨간 줄 0)
                foreach (var n in new[] { "UpgradeAllBtn", "QuickEquipBtn", "SummonBtn", "Summon10Btn", "Slot:0", "Slot:3" }) Assert.IsTrue(ClickNamed(pet, n), "껍데기 " + n);
                yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen, "껍데기 버튼은 팝업을 열지 않는다"); Assert.AreEqual("pet", _app.Current.Name, "화면 그대로");
                // 소환을 «마지막에» 눌렀으므로 토스트가 그 까닭을 말하고 있어야 한다(T178 · 팝업이 아니라 토스트라 위 «팝업 안 열림» 과 어긋나지 않는다)
                Assert.IsTrue(HasText(s => s.Contains(PetScreen.NotReadyMsg)),
                    "소환을 누르면 «" + PetScreen.NotReadyMsg + "» 토스트로 까닭을 말해야 한다(T178)");
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
            // T176 ⓐ(주인 «왼쪽 하단에 N 표시 … 그거 필요 없음 장비 부분») — 칸에 «New» 그림이 없다. 값(GearItem.IsNew)은 그대로 살아 있다(T167 이 쓴다).
            Assert.AreEqual(0, CountNamed(content, "New"), "인벤 칸에 «N»(NEW) 표시가 남아 있다(T176 ⓐ)");
            Assert.IsTrue(HasText(s => s == "장비"), "제목 «장비»");
            // T37 — 레퍼런스 06_gear.jpg 구도 단언: 상단 재화 바 · 무대(들판·길·나무 · 정사각 캐릭터 호스트) · 슬롯 6 = 표 자리(좌 3 / 우 3 · ±0.5%p) · 스탯 3칸 · 상점/대장간 버튼(스탯 줄 아래 · 대장간 오른쪽 끝) · 인벤 5열 · 탭 바
            {
                var top = UiKit.Find(gear, "TopBar"); Assert.IsNotNull(top, "장비 화면 상단 재화 바"); Assert.IsNotNull(UiKit.Find(top, "Avatar"), "아바타"); Assert.IsNotNull(UiKit.Find(top, "ResourceBar_Gem"), "보석 pill");
                var stage = (RectTransform)UiKit.Find(gear, "Stage"); Assert.IsNotNull(stage, "캐릭터 무대");
                Assert.AreEqual(1f - Layout.GearStage.Y / 100f, stage.anchorMax.y, 1e-3f, "무대 = 표 자리(y)"); Assert.AreEqual(1f - (Layout.GearStage.Y + Layout.GearStage.H) / 100f, stage.anchorMin.y, 1e-3f, "무대 높이 = 표(26.5%)");
                Assert.IsNotNull(UiKit.Find(stage, "Field"), "무대 들판"); Assert.IsNotNull(UiKit.Find(stage, "Road"), "무대 길"); Assert.GreaterOrEqual(CountNamed(stage, "Tree"), 3, "무대 나무");
                {
                    // T71 ③ — 길 띠 위·아래 물결 경계(Road_up · 아래는 y 반전) · 줄이 길 가장자리에 걸친다 · 타일 여러 장(스프라이트 비례)
                    var road = (RectTransform)UiKit.Find(stage, "Road"); var ru = (RectTransform)UiKit.Find(stage, "RoadUp"); var rd = (RectTransform)UiKit.Find(stage, "RoadDown");
                    Assert.IsNotNull(ru, "무대 길 위 물결 경계 RoadUp"); Assert.IsNotNull(rd, "무대 길 아래 물결 경계 RoadDown");
                    Assert.Greater(ru.localScale.y, 0f, "위 경계는 반전 없음"); Assert.Less(rd.localScale.y, 0f, "아래 경계는 y 반전(localScale.y −1)");
                    Assert.IsTrue(ru.anchorMin.y < road.anchorMax.y && ru.anchorMax.y > road.anchorMax.y, "RoadUp 이 길 위 가장자리에 걸친다");
                    Assert.IsTrue(rd.anchorMin.y < road.anchorMin.y && rd.anchorMax.y > road.anchorMin.y, "RoadDown 이 길 아래 가장자리에 걸친다");
                    Assert.GreaterOrEqual(ru.childCount, 2, "RoadUp 타일 여러 장"); Assert.AreEqual(ru.childCount, rd.childCount, "위·아래 타일 수 같음");
                    var tile = ru.GetChild(0).GetComponent<Image>(); Assert.IsNotNull(tile != null ? tile.sprite : null, "물결 경계 타일 스프라이트(env.roadUp)"); Assert.IsFalse(tile.preserveAspect, "타일은 줄 높이에 맞춰 늘린다");
                }
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
                // T112 — ⓐ 무대가 갈색 띠까지 내려와 스탯 3칸이 무대 «안» 에 있고, 무대 그림은 늘리기 전과 같은 자리
                {
                    Assert.GreaterOrEqual(Layout.GearStage.Y + Layout.GearStage.H, GearScreen.Band.Y - 0.3f, "무대 아래 끝이 갈색 띠 위와 맞닿는다(T112 ⓐ)");
                    var stageRt = (RectTransform)UiKit.Find(gear, "Stage"); Assert.IsNotNull(stageRt, "무대");
                    var sc = new Vector3[4]; stageRt.GetWorldCorners(sc);
                    float stageH = sc[1].y - sc[0].y;
                    foreach (var n in new[] { "Stat:atk", "Stat:hp", "Stat:sh" })
                    {
                        var cell = (RectTransform)UiKit.Find(gear, n); Assert.IsNotNull(cell, n);
                        var cc = new Vector3[4]; cell.GetWorldCorners(cc);
                        Assert.LessOrEqual(cc[1].y, sc[1].y + 1f, n + " 스탯 칸 위 끝이 무대 안");
                        Assert.GreaterOrEqual(cc[0].y, sc[0].y - 1f, n + " 스탯 칸 아래 끝이 무대 안(T112 ⓐ)");
                    }
                    // 길 띠가 화면에서 차지하는 높이(프레임 %) = 옛 무대 높이 기준 값 그대로 — 무대를 늘려도 풍경은 안 커진다
                    var roadRt = (RectTransform)UiKit.Find(stageRt, "Road"); Assert.IsNotNull(roadRt, "무대 길 띠");
                    var rc = new Vector3[4]; roadRt.GetWorldCorners(rc);
                    float roadPct = (rc[1].y - rc[0].y) / stageH * Layout.GearStage.H;
                    Assert.AreEqual(24f * GearScreen.StageArtH / 100f, roadPct, 0.3f, "길 띠 높이(프레임 %)는 무대를 늘려도 그대로(T112 ⓐ · StageArtH 환산)");
                }
                var inv = (RectTransform)UiKit.Find(gear, "InvScroll"); Assert.IsNotNull(inv, "인벤 스크롤");
                Assert.AreEqual(1f - Layout.GearInv.Y / 100f, inv.anchorMax.y, 1e-3f, "인벤 = 표 자리"); Assert.Less(inv.anchorMax.y, forgeB.anchorMin.y + 1e-3f, "인벤은 버튼 줄 아래");
                var grid = content.GetComponent<GridLayoutGroup>(); Assert.IsNotNull(grid, "인벤 격자"); Assert.AreEqual(Layout.GearInvCols, grid.constraintCount, "5열");
                // T112 ⓑ — 인벤 첫 줄이 갈색 띠에 붙지 않는다(주인 «Content 에 탑에 패딩 20 정도»)
                {
                    Assert.GreaterOrEqual(grid.padding.top, GearUi.InvTopPadPx, "인벤 격자 위 패딩 ≥ " + GearUi.InvTopPadPx + "px(T112 ⓑ)");
                    var firstCell = content.childCount > 0 ? (RectTransform)content.GetChild(0) : null;
                    if (firstCell != null)
                    {
                        var fc = new Vector3[4]; firstCell.GetWorldCorners(fc);
                        var bandBottomPct = GearScreen.Band.Y + GearScreen.Band.H;
                        var invRt = (RectTransform)UiKit.Find(gear, "InvScroll"); var ic = new Vector3[4]; invRt.GetWorldCorners(ic);
                        Assert.LessOrEqual(fc[1].y, ic[1].y - GearUi.InvTopPadPx * (ic[1].y - ic[0].y) / (UiKit.FrameH * Layout.GearInv.H / 100f) + 1f,
                            "첫 줄 칸 위 끝이 인벤 위 끝에서 패딩만큼 내려와 있다(T112 ⓑ · 띠 아래 " + bandBottomPct.ToString("0.#") + "%)");
                    }
                }
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
                // T88 — 스탯 줄은 부위 역할에 따라: 공격 부위(무기·목걸이·반지) = «공격력» 한 줄 · 방어 부위(투구·갑옷·신발) = «체력»·«실드» 두 줄(빈 줄 없음)
                Assert.IsNotNull(UiKit.Find(bx, "Stats"), "스탯 박스");
                Assert.AreEqual(GearRole.IsAttack(g0.Part) ? 1 : 2, CountNamed(UiKit.Find(bx, "Stats"), "Stat:"), "스탯 줄 = 부위 역할(T88)");
                Assert.IsTrue(HasText(s => s.StartsWith(GearRole.IsAttack(g0.Part) ? "공격력" : "체력")), "스탯 줄 라벨 = 부위 역할(T88)");
                if (GearRole.IsAttack(g0.Part)) Assert.IsFalse(HasText(s => s.StartsWith("실드  ")), "공격 부위엔 실드 줄이 없다(T88)");
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
                        // T84 — 어두운 pill 위 글자는 밝은 색 + 검은 아웃라인이어야 읽힌다(주인 상시 지시 · screens run 148 의 07 눈 확인에서 회색 글자가 안 읽혔다)
                        Assert.IsNotNull(t.GetComponent<TextOutline8>(), "옵션 줄 «" + t.text + "» 에 검은 아웃라인(T63 0항 «예외 없이»)");
                        // T177(주인 2026-09-07 «잠긴 옵션 줄 글씨는 #666666»)이 «잠긴» 줄만 일부러 어둡게 만든다 —
                        // 그 자리는 `OwnerDarkTextTag` 를 달고 있으므로 T84 의 «밝아야 한다» 에서 뺀다(안 빼면 주인 지시가 게이트에 막힌다 · 결정 428).
                        // 대신 «표식이 있으면 색이 정말 그 지정색인가» 를 재서 표식이 «아무 어두운 글자나 봐 주는 뒷문» 이 되지 않게 한다.
                        if (t.GetComponent<OwnerDarkTextTag>() != null)
                            Assert.AreEqual(Palette.OptLocked.grayscale, t.color.grayscale, 0.02f,
                                "옵션 줄 «" + t.text + "» 은 주인 지정 어두운 글자 표식이 붙었으니 색이 #666666 이어야 한다(T177)");
                        else
                            Assert.GreaterOrEqual(t.color.grayscale, 0.55f, "옵션 줄 «" + t.text + "» 글자가 어두운 pill 에서 읽힐 만큼 밝아야 한다(T84)");
                    }
                    Assert.AreEqual(GearRole.IsAttack(g0.Part) ? 1 : 2, statRows, "스탯 줄 = 부위 역할(T88)"); Assert.AreEqual(CountNamed(opts, "Opt:"), optRows, "옵션 줄마다 글자 하나");
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

            // T88 — 부위 재편: 방어 부위(갑옷)는 «체력»·«실드» 두 줄이고 공격력 줄이 없다 · «장갑» 부위는 이름이 «반지»
            {
                var gArmor = items.Find(x => x.Part == "armor"); Assert.IsNotNull(gArmor, "갑옷 장비");
                GearUi.OpenDetail(_app, gArmor, _app.Current.Refresh); yield return Frames(2);
                var bx2 = (RectTransform)UiKit.Find(_app.Overlay.Root, "ui.popup");
                Assert.AreEqual(2, CountNamed(UiKit.Find(bx2, "Stats"), "Stat:"), "방어 부위 = 체력·실드 두 줄(T88)");
                Assert.IsTrue(HasText(s => s.StartsWith("체력  ")) && HasText(s => s.StartsWith("실드  ")), "방어 부위 줄 라벨 = 체력·실드(T88)");
                Assert.IsFalse(HasText(s => s.StartsWith("공격력  ")), "방어 부위엔 공격력 줄이 없다(T88)");
                Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Dimmed"), "닫기"); yield return Frames(1);

                var gRing = items.Find(x => x.Part == "glove"); Assert.IsNotNull(gRing, "반지(glove) 장비");
                GearUi.OpenDetail(_app, gRing, _app.Current.Refresh); yield return Frames(2);
                Assert.IsTrue(HasText(s => s == "반지"), "부위 pill = «반지»(T88 · gear.json 의 «장갑» 을 표시에서만 덮는다)");
                Assert.IsFalse(HasText(s => s == "장갑"), "«장갑» 이 화면에 남아 있으면 안 된다(T88)");
                Assert.AreEqual(1, CountNamed(UiKit.Find((RectTransform)UiKit.Find(_app.Overlay.Root, "ui.popup"), "Stats"), "Stat:"), "반지 = 공격력 한 줄(T88)");
                Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Dimmed"), "닫기"); yield return Frames(1);
                Check("T88 부위 재편 뒤 장비 화면");
            }

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
                // T105 — 슬롯 6칸의 «부위» 아이콘: 열 순서(왼쪽 무기·목걸이·반지 / 오른쪽 투구·갑옷·신발) · 세트 아이콘이 아니다 · 빈 슬롯에도 흐리게 켜져 있다
                {
                    Assert.AreEqual(new[] { "weapon", "neck", "glove" }, GearUi.ColLeft, "왼쪽 열 = 무기·목걸이·반지(T105 주인 지정)");
                    Assert.AreEqual(new[] { "helm", "armor", "boot" }, GearUi.ColRight, "오른쪽 열 = 투구·갑옷·신발(T105 주인 지정)");
                    var setIcons = new[] { "pi.critical", "pi.heart", "ui.dodge" };
                    for (int i = 0; i < 6; i++)
                    {
                        string part = i < 3 ? GearUi.ColLeft[i] : GearUi.ColRight[i - 3];
                        var pi = UiKit.Find(slotGrp.GetChild(i), "PartIcon"); Assert.IsNotNull(pi, "슬롯 " + i + " 부위 아이콘");
                        Assert.IsTrue(pi.gameObject.activeInHierarchy, "슬롯 " + i + "(" + part + ") 부위 아이콘은 늘 켜져 있다(T105)");
                        var pim = pi.GetComponent<Image>();
                        Assert.AreEqual(_app.Assets.Sprite(GearLook.PartIcon(part)), pim.sprite, "슬롯 " + i + " 부위 아이콘 = " + GearLook.PartIcon(part));
                        foreach (var k in setIcons) Assert.AreNotEqual(_app.Assets.Sprite(k), pim.sprite, "슬롯 " + i + " 배지에 세트 아이콘(" + k + ")이 남아 있으면 안 된다(T105)");
                        Assert.AreEqual(1f, pim.color.a, 1e-3f, "전부 장착 상태라 또렷하다");
                    }
                    // T176 ⓑ(주인 «장착 슬롯이랑 인벤 칸의 부위 표시 형식을 통일해 줘») — 슬롯도 인벤 칸과 **같은 배지 조각**을 **같은 비율**로 단다.
                    for (int i = 0; i < 6; i++)
                    {
                        var badge = UiKit.Find(slotGrp.GetChild(i), "PartBadge");
                        Assert.IsNotNull(badge, "슬롯 " + i + " 부위 배지(T176 ⓑ · 인벤 칸의 TypeArea 와 같은 조각)");
                        var brt = (RectTransform)badge; var frame2 = (RectTransform)UiKit.Find(slotGrp.GetChild(i), "ItemFrame_01");
                        float cell2 = Mathf.Min(frame2.rect.width, frame2.rect.height);
                        Assert.AreEqual(GearUi.PartBadge.W / 100f, brt.rect.width / cell2, 0.03f,
                            "슬롯 " + i + " 배지 지름 = 칸의 " + GearUi.PartBadge.W + "%(인벤 칸 실측과 같은 비율 · T176 ⓑ)");
                        Assert.IsNotNull(UiKit.Find(badge, "PartIcon"), "슬롯 " + i + " 배지 안에 부위 아이콘");
                    }

                    // 하나를 빼면 그 칸만 흐려진다
                    var offPart = GearUi.ColLeft[0]; var offUid = S.Eq[offPart]; S.Eq.Remove(offPart);
                    _app.ShowScreen("gear"); yield return Frames(2);
                    var grp2 = UiKit.Find(_app.Current.Root, "Group_Slot");
                    var pi0 = UiKit.Find(grp2.GetChild(0), "PartIcon").GetComponent<Image>();
                    Assert.IsTrue(pi0.gameObject.activeInHierarchy, "빈 슬롯에도 부위 아이콘은 켜져 있다(T105 3항)");
                    Assert.AreEqual(GearScreen.PartIconEmptyAlpha, pi0.color.a, 1e-3f, "빈 슬롯의 부위 아이콘은 흐리다");
                    S.Eq[offPart] = offUid; _app.ShowScreen("gear"); yield return Frames(2);
                }
                // T105 — 인벤 칸의 다이아 배지도 부위 아이콘이다(세트 아이콘 아님)
                {
                    var anyG = S.Inv.Find(x => !S.IsEquipped(x)) ?? S.Inv[0];
                    var cell = GearUi.Cell(_app.Current.Root, D, anyG, new GearUi.CellOpts(), null); yield return Frames(1);
                    var ta = UiKit.Find(cell, "TypeArea"); Assert.IsNotNull(ta, "칸 다이아 배지");
                    var icon = UiKit.Find(ta, "Icon").GetComponent<Image>();
                    Assert.AreEqual(_app.Assets.Sprite(GearLook.PartIcon(anyG.Part)), icon.sprite, "칸 배지 = 부위 아이콘(T105)");
                    UnityEngine.Object.Destroy(cell.gameObject); yield return Frames(1);
                }
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
                Assert.Greater(plate.GetComponent<Image>().color.a, 0.9f, "띠는 장비 그림이 안 비칠 만큼 불투명해야 한다(0.82 로는 절반쯤 비쳤다 — screens run 106)");
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
                // T100(주인 2026-09-07) — ⓐ 상자 카드의 제목 바탕 끄기 · ⓑ «상자» 섹션 헤더 · ⓒ 섹션 라인 데코 알파 13/255
                {
                    Assert.AreEqual(3, CountNamed(content, "Sec:"), "섹션 헤더 3(상자·다이아·골드) — T100 ⓑ");
                    Assert.IsNotNull(UiKit.Find(content, "Sec:상자"), "«상자» 섹션 헤더 — T100 ⓑ");
                    foreach (Transform c in content)
                    {
                        if (!c.name.StartsWith("Box:")) continue;
                        foreach (var n in new[] { "TitleBg", "TitleBorder" })   // 주인이 부른 «TitleBgBorder» 의 실제 조각 이름 = TitleBorder(결정 242)
                        {
                            var t = UiKit.Find(c, n);
                            if (t != null) Assert.IsFalse(t.gameObject.activeSelf, $"{c.name} 의 «{n}» 은 꺼져 있어야 한다(T100 ⓐ · 주인 «필요 없으니까 없애라»)");
                        }
                        bool ring = false; foreach (Transform k in c) if (k.name == "Border") ring = true;
                        Assert.IsTrue(ring, $"{c.name} 의 T69-shop 링(카드 직계 «Border»)은 그대로 — 없애는 것은 제목 바탕뿐이다");
                    }
                    int lines = 0;
                    foreach (Transform c in content)
                    {
                        if (!c.name.StartsWith("Sec:")) continue;
                        foreach (var im in c.GetComponentsInChildren<Image>(true))
                        {
                            if (!im.name.StartsWith("LineDeco")) continue;
                            lines++;
                            Assert.AreEqual(ShopScreen.SecLineAlpha, im.color.a, 1f / 255f, $"{c.name}/{im.name} 라인 데코 알파 = 255 중 13(T100 ⓒ)");
                        }
                    }
                    Assert.GreaterOrEqual(lines, 3, "섹션 헤더 3개가 저마다 라인 데코를 갖는다");
                }
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
                // T73 — 헤더는 조각 «Sec:<이름>» 안의 글자만 본다: 글자 «골드» 로 고르면 골드 상품 카드의 이름 라벨(Text_Limit · 본문 40)까지 걸려 CI #110 이 «≥ 50 But was 40» 으로 빨갰다
                int headers = 0;
                foreach (var hn in new[] { "다이아", "골드" })
                {
                    var sec = UiKit.Find(content, "Sec:" + hn); Assert.IsNotNull(sec, "섹션 헤더 조각 Sec:" + hn);
                    var ht = UiKit.Find(sec, "Text (TMP)"); var htx = ht != null ? ht.GetComponent<Text>() : null; Assert.IsNotNull(htx, "섹션 헤더 «" + hn + "» 글자");
                    Assert.AreEqual(hn, htx.text, "섹션 헤더 글자 = " + hn); headers++;
                    Assert.GreaterOrEqual(htx.fontSize, UiKit.FontForHeight(Layout.ShopSec1.H), "섹션 헤더 «" + hn + "» 크기 = 표 ⑤ 헤더 높이(2.5%)에서 계산");
                }
                Assert.GreaterOrEqual(headers, 2, "섹션 헤더 «다이아»·«골드»");
                // bestFit 이 종류 하한 아래로 누른 글자 0 — CI #110 표의 «최소 크기(실제) 39»(작은 카드 확률 pill 88px 에 2줄 88 이 딱 맞아 눌림) 재발 방지 · 상단 바(T63-lobby)·탭 바는 제외
                var shrunk = new List<string>();
                foreach (var r in TextAudit.Collect("shop", shop))
                    if (r.Kind != TextKind.Small && r.Used > 0 && r.Used < r.Min && r.Path.IndexOf("TopBar", StringComparison.Ordinal) < 0 && r.Path.IndexOf("ui.tabBar", StringComparison.Ordinal) < 0) shrunk.Add(r.ToString());
                Assert.AreEqual(0, shrunk.Count, "상점 글자가 bestFit 으로 종류 하한 아래로 줄었다(T63-shop):\n" + string.Join("\n", shrunk));
                // T190 ⓑ — **남기는 자리를 못 박는다**: 상점 상품 카드의 빛은 그대로다(주인 13:1X «상점은 바꾸기 전이 맞았음»).
                // 이 줄이 없으면 «빛 효과 없게» 를 읽은 다음 워커가 상점 것까지 지운다. 상품 카드는 조각이 달라(ListItem_ShopItem) «아이템 칸» 판정에도 안 걸린다.
                {
                    var pack = UiKit.Find(content, "GemPack:0"); Assert.IsNotNull(pack, "다이아 상품 카드");
                    var icon = UiKit.Find(pack, "Icon"); Assert.IsNotNull(icon, "상품 아이콘");
                    Assert.IsTrue(UiKit.HasLight(icon.parent), "상점 상품 아이콘 뒤 빛살은 **남는다**(T190 ⓑ · 주인 13:1X)");
                    Assert.IsFalse(UiKit.IsItemCell(icon.parent), "상품 카드는 «아이템 칸»(ItemFrame_01) 이 아니다 — 판정이 상점을 안 건드린다는 근거(T190 1항)");
                }
                var qty = UiKit.Find(UiKit.Find(content, "GemPack:0"), "Text_Title"); Assert.IsNotNull(qty, "다이아 카드 수량 글자");
                Assert.GreaterOrEqual(qty.GetComponent<Text>().fontSize, ShopScreen.QtySize, "상품 수량 크기 = 수량 띠 높이에서 계산(≈51)");
                // T100 ⓓ(주인 2026-09-07 «상자들 카드 부분에도 그라디안트 · 레퍼런스랑 같은 색감») — 카드 조각 «안»(바탕 바로 위)에 실측 두 색 그라데이션
                int gradCards = 0;
                for (int i = 0; i < content.childCount; i++)
                {
                    var c = content.GetChild(i);
                    bool isCard = c.name.StartsWith("Box:") || c.name.StartsWith("GemPack:") || c.name.StartsWith("GoldPack:");
                    if (!isCard || c.childCount == 0) continue;
                    // T147 — «있는가» 가 아니라 «바탕 바로 위에 있는가» 를 잰다(있기만 하면 프레임 뒤라도 초록이었다 · 결정 374).
                    // 바탕 이름은 조각마다 다르다: 상자 카드(ui.cardFrame)는 «Bg» 가 직계 · 상품 카드(ui.shopItem)는 «Bg(Mask)» 가 ShopFrame_01 안의 손자.
                    AssertGradientAboveBg(c.GetChild(0), c.name.StartsWith("Box:") ? "Bg" : "Bg(Mask)", c.name);
                    // T100 ⓓ 회차 2 — 상자 카드(10)는 조각 바탕이 회색이라 «덧칠» 이면 색이 죽는다(실측: 레퍼런스 «Rare» #0182C3 → 회차 1 의 우리 #8997A2).
                    // 몸통을 꽉 채워야 레퍼런스 색감이 난다 → 위·아래 두 조각의 tint 알파가 1(결정 338). 상품 카드(09)는 덧칠 그대로라 여기서 안 잰다.
                    if (c.name.StartsWith("Box:"))
                    {
                        var grad = ShopScreen.ChestGradName(D, c.name.Substring("Box:".Length));
                        Assert.IsNotNull(grad, c.name + " 의 그라데이션 이름(ShopScreen.ChestGradName)");
                        AssertSolidGradient(c.GetChild(0), c.name, grad);
                    }
                    gradCards++;
                }
                Assert.GreaterOrEqual(gradCards, D.Gacha.Boxes.Count, "상자 카드 3장 + 상품 카드에 전부 그라데이션");
                AssertNoTextClip("상점", shop);
            }

            int inv = S.Inv.Count;
            Assert.IsTrue(Click(shop, s => s.Contains("1회")), "«1회» 뽑기 버튼"); yield return Frames(2);
            Check("뽑기 결과 팝업(1회)", expectOverlay: true);
            // T95(주인 2026-09-07 «소환 결과 창이 Shop_Chest_Open 이거로 돼야 하는데 안 됐더라») — 조각 그대로 · 우리 격자는 그 위에
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.chestOpen"), "뽑기 결과 = 주인 지정 조각(Shop_Chest_Open) 그대로(T95)");
            Assert.IsNull(UiKit.Find(_app.Overlay.Root, "ui.popup"), "공통 팝업 상자로 다시 조립하지 않는다(T95 1항)");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "Image_Chest"), "조각의 상자 그림");
            // T180 — 연출 «전» 을 여기서 잰다(아래 CompleteAllTweens 뒤에는 이미 열려 있다).
            // 주인 «닫힌 게 위에서 떨어져서 착지하고 열린 상태 이미지로 바뀐 다음에 장비들 뭐 나왔는지».
            var chestImg0 = UiKit.Find(_app.Overlay.Root, "Image_Chest").GetComponent<Image>();
            Assert.IsNotNull(chestImg0, "상자 그림(Image)");
            Assert.IsNotNull(chestImg0.sprite, "상자 스프라이트");
            StringAssert.DoesNotContain("open", chestImg0.sprite.name.ToLowerInvariant(),
                "팝업이 뜬 직후에는 «닫힌» 상자여야 한다 — 지금 스프라이트: " + chestImg0.sprite.name + " (T180)");
            // T202(주인 2026-09-07 09:0X «상자가 바닥에 착지하고 **1초 뒤**에 열리는 애니메이션») —
            // «지금 몇 초냐» 를 묻지 않는다(그 함정은 T158 ⓐ 가 세 회차에 걸쳐 밟았다 · 결정 395). 상수 사이의 **관계**로 잰다:
            Assert.AreEqual(1.0f, ShopScreen.ChestOpenAt - ShopScreen.ChestFallSec, 0.2f,
                "착지(ChestFallSec) 와 열림(ChestOpenAt) 사이가 «1초» 여야 한다 — 지금 "
                + (ShopScreen.ChestOpenAt - ShopScreen.ChestFallSec).ToString("0.##") + "s (T202 2항)");
            Assert.LessOrEqual(ShopScreen.ChestOpenAt + 1.2f, 2.7f,
                "연출 총 길이(착지 + 정지 + 열림 ≈ 1.2s)가 2.7s 를 넘으면 주인이 여러 번 돌릴 때 답답하다(T202 4항)");
            var chestGrp0 = UiKit.Find(_app.Overlay.Root, "Chest") as RectTransform;
            Assert.IsNotNull(chestGrp0, "조각의 상자 묶음(Chest)");
            float chestY0 = chestGrp0.anchoredPosition.y;   // «떨어지기 전» 높이 — 연출이 끝난 뒤와 맞대 본다(상수에 안 기댄다)
            // T158 ⓐ — «작았다» 는 여기서 잰다(가장 이른 자리). 뒤에서 재면 커지는 중이라 값이 흐른다.
            float chestScale0 = chestGrp0.localScale.x;
            // T202 4항 — **첫 탭 = 건너뛰기**(그 다음 탭이 닫기). 연출이 1초 길어졌으므로 이 갈래가 없으면 탭 한 번에 창이 닫혀 결과를 못 본다.
            // ⚠ 자리는 «재기 전» 값(chestY0·chestScale0)을 다 읽은 «뒤» 여야 한다 — 건너뛰기는 낙하를 끝까지 돌려 놓으므로
            //    앞에 두면 «떨어지기 전 높이» 가 이미 착지 높이가 되어 아래 «떨어져 내려왔나» 단언이 스스로 빨개진다(CI #404 에서 실제로 그랬다 · 결정 527).
            // ⚠ 그리고 시계에 안 매이게 «둘 중 하나» 로 단언한다 — 느린 기계에서 이미 연출이 끝났다면 «닫히는 것» 이 옳은 동작이라 거짓 빨강이 되면 안 된다.
            Assert.IsTrue(ClickNamed(_app.Overlay.Root, "Background"), "결과 창 배경(탭 자리)"); yield return Frames(1);
            Assert.IsTrue(_app.Overlay.IsOpen || chestImg0.sprite.name.ToLowerInvariant().Contains("open"),
                "연출이 도는 중에 배경을 탭하면 «건너뛰기» 여야 한다(창이 닫히면 안 된다 · T202 4항). "
                + "연출이 이미 끝난 뒤였다면 닫히는 것이 옳으므로 그때는 «열린 상자» 로 통과한다.");
            Assert.IsTrue(HasText(s => s == "탭하여 닫기"), "결과 창: «탭하여 닫기»(조각의 Text_TouchContionue)");
            // T95 — 제목은 **조각 제 리본**에 쓴다(글자를 따로 얹으면 리본의 데모 글자 «Reward» 가 화면에 남는다 · CI #235)
            {
                var rib = UiKit.Find(_app.Overlay.Root, "Title_01_NoDeco_Tangerine");
                Assert.IsNotNull(rib, "조각의 제목 리본");
                var rt2 = rib.GetComponentInChildren<Text>(true);
                Assert.IsNotNull(rt2, "리본 글자"); StringAssert.Contains("회", rt2.text, "리본에 우리 제목(«… N회»)이 들어간다");
                Assert.AreNotEqual("Reward", rt2.text, "리본에 데모 글자가 남으면 안 된다");
            }
            {
                // T158 ⓑ — 안내 줄(«최고 등급 … · 장착은 장비 탭에서»)은 주인 지시로 없앴다. 되살아나면 여기서 빨개진다(T63-shop 의 «본문 하한» 단언을 뒤집은 자리)
                Assert.IsNull(UiKit.Find(_app.Overlay.Root, "Note"), "결과 창에 안내 줄이 없어야 한다(T158 ⓑ · 주인 «이런 텍스트 빼셈»)");
                // 글자 잘림 0(장비 칸 «gear:» 안 글자는 T63-gear 몫이라 제외)
                AssertNoTextClip("뽑기 결과 창", _app.Overlay.Root, skipPath: "gear:");
            }
            {
                // T158 ⓐ — 상자는 «작았다 → 커졌다 → 제 크기»(주인 «작았었는데 커졌다가»).
                // ⚠ 회차 1 은 «연 직후 배율 < 1» 로 쟀다가 CI 를 빨갛게 했다(#324 · «Expected less than 1.0f · But was 1.00306165f»).
                // 까닭은 코드가 아니라 **자**다: 배율 트윈은 ChestFallSec(0.24초)뿐인데 팝업을 연 뒤 여기까지 오는 데
                // 그보다 오래 걸린다(헤드리스에서 Find·Check 가 캔버스를 훑는다) → 이미 OutBack 의 «살짝 넘긴» 구간(1.003)을 잰다.
                // 시계와 경주하는 단언이라 기계 부하에 따라 빨갛다 말았다 한다(결정 395 에 적어 둔 그 함정).
                // 그래서 «언제 재느냐» 에 안 흔들리는 꼴로 바꾼다 — 셋 중 어느 것도 시각에 안 매인다:
                Assert.Less(ShopScreen.ChestScaleFrom, 1f, "시작 배율은 1보다 작아야 «작았다가» 가 성립한다(T158 ⓐ · 지금 " + ShopScreen.ChestScaleFrom + ")");
                // ⚠ 회차 2 는 «배율 < 1 || 도는 중» 으로 고쳤는데 그것도 빨갰다(#332 · «배율 1.006 인데 트윈도 없다»).
                // 까닭: 이 배율 트윈은 Sequence «안» 에 끼워져 있어 DOTween.IsTweening(대상) 이 못 본다(중첩 트윈은 활성 목록에서 빠진다).
                // → 회차 3 은 «지금 어떤 상태냐» 를 아예 안 묻는다. 화면이 «그 일을 했다» 를 기록으로 남기고 그것을 본다(결정 329 · 로딩 화면과 같은 방법).
                Assert.AreEqual(ShopScreen.ChestScaleFrom, ShopScreen.LastChestScale, 0.001f,
                    "결과 창이 상자에 시작 배율을 안 넣었다 = «작았다 커졌다» 연출이 안 걸렸다(T158 ⓐ · 기록값 " + ShopScreen.LastChestScale.ToString("0.###") + "). "
                    + "이 값은 ChestResult 가 배율을 실제로 넣을 때만 채워진다 — 연출을 지우면 0 이 되어 여기서 빨개진다.");
                // OutBack 의 넘김 폭 — 기본 계수(1.70158)로 0→1 을 밀면 봉우리가 약 1.099 다. 여유를 두어 0.12.
                const float ChestOvershootMax = 0.12f;
                Assert.Less(chestScale0, 1f + ChestOvershootMax,
                    "연 직후 배율은 시작(작음)~넘김(OutBack) 사이여야 한다 — 지금 " + chestScale0.ToString("0.###") + "(T158 ⓐ · 이 줄은 «언제 재느냐» 에 안 흔들리게 넘김까지 허용한다)");
                // T158 ⓒ — 결과 칸은 눌러도 어두워지지 않는다(우리가 클릭을 안 붙인 칸이라 조각이 달고 온 Button 을 뗀다)
                var got0 = UiKit.Find(_app.Overlay.Root, "Got");
                Assert.IsNotNull(got0, "얻은 장비 격자(Got)");
                Assert.AreEqual(0, got0.GetComponentsInChildren<Button>(true).Length, "결과 칸에 Button 이 남아 있으면 눌림 표시가 돈다(T158 ⓒ)");
            }
            Assert.GreaterOrEqual(S.Inv.Count, inv + 1, "뽑은 장비가 인벤에 담겨야 한다");
            {
                // T190 — ⚑ **주인 13:4X**(«소환 결과에 아이템 슬롯 «내부» 빛 효과라든가 그런 거 없게 해») 로 **뒤집혔다**:
                // 여기 있던 «빛살이 있다»(T72 ② · 주인 «상점 아이템 … 아이콘 뒤에 Effect_Light»)를 **없다** 로 바꾼다.
                // 담개(`LightMask`)까지 없어야 한다 — 빛살만 끄고 글로우 서클(T155 ⓓ)이 남으면 «빛 효과» 는 그대로다.
                // 상자 그림의 빛은 조각이 제 «Light» 로 내는 것이라 여기와 무관하다(T95).
                var first = UiKit.Find(_app.Overlay.Root, "Got").GetChild(0);
                var itemFrame = UiKit.Find(first, "Item").parent;
                Assert.IsFalse(UiKit.HasLight(itemFrame), "뽑기 결과 칸에는 빛살이 없다(T190 · 주인 13:4X)");
                Assert.IsFalse(UiKit.HasLightMask(itemFrame), "뽑기 결과 칸에는 빛 담개(글로우 서클 포함)도 안 선다(T190)");
                Assert.IsTrue(UiKit.IsItemCell(itemFrame), "그 칸이 «아이템 칸» 이라는 판정 자체를 못 박는다(UiKit.IsItemCell · T190 1항)");
                // T95 «찰지게» — 연출이 끝나면 모든 칸이 제 크기·불투명(트윈이 중간에 멈춘 채 남지 않는다)
                UiKit.CompleteAllTweens(); yield return Frames(1);
                // T180 — 끝난 상태 = «착지한 열린 상자». 스킵(CompleteAll(true))으로도 같은 상태라야 한다(지시서 4항 ⓓ).
                StringAssert.Contains("open", chestImg0.sprite.name.ToLowerInvariant(),
                    "연출이 끝나면 «열린» 상자여야 한다 — 지금 스프라이트: " + chestImg0.sprite.name + " (T180)");
                Assert.Less(chestGrp0.anchoredPosition.y, chestY0 - 1f,
                    "상자는 위에서 «떨어져» 내려와 있어야 한다(시작 y=" + chestY0.ToString("0.0") + " → 끝 y=" + chestGrp0.anchoredPosition.y.ToString("0.0") + " · T180)");
                // T158 ⓐ — 커지는 연출이 끝나면 «제 크기»(오버슛이 남아 있으면 안 된다)
                Assert.AreEqual(1f, chestGrp0.localScale.x, 0.02f, "연출이 끝나면 상자는 제 크기(T158 ⓐ)");
                var got = UiKit.Find(_app.Overlay.Root, "Got");
                for (int i = 0; i < got.childCount; i++)
                {
                    var c = (RectTransform)got.GetChild(i);
                    Assert.AreEqual(1f, c.localScale.x, 0.02f, $"칸 {i} 스케일 1");
                    var cg = c.GetComponent<CanvasGroup>(); if (cg != null) Assert.AreEqual(1f, cg.alpha, 0.02f, $"칸 {i} 알파 1");
                }
            }
            Assert.AreEqual(CountNamed(_app.Overlay.Root, "gear:"), S.Inv.Count - inv, "결과 팝업의 장비 칸 수 = 얻은 수");
            AssertChestSlotAndPattern("뽑기 결과(1회)");
            _app.Overlay.Close(); yield return Frames(1);
            inv = S.Inv.Count;
            Assert.IsTrue(Click(shop, s => s.Contains("10회")), "«10회» 뽑기 버튼"); yield return Frames(2);
            Check("뽑기 결과 팝업(10회)", expectOverlay: true);
            Assert.GreaterOrEqual(S.Inv.Count, inv + D.Gacha.TenPullCount, "10회 = 10개 이상");
            AssertChestSlotAndPattern("뽑기 결과(10회)");
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
                    // T153(주인 07:0X «샤인이 일정한 두께로 쭉 지나가야 하는데 얇→두꺼→얇») — 빛은 이제 프레임 Image **한 장**에만 문다.
                    // 예전 계약(«프레임 Image 전부»)이 그 «얇→두꺼→얇» 의 원인이었다: 층마다 폭이 달라 띠 다섯이 겹쳐 지나갔다.
                    // 새 지시가 옛 단언을 이긴다(T94 ⓑ vs T69 · T168 vs T107 이 밟은 길) — 여기서는 «한 장 · 그 한 장이 ShineTarget» 을 잰다.
                    var lit = new List<Image>(); foreach (var im in imgs) if (im != null && im.material == mo.Mat) lit.Add(im);
                    Assert.AreEqual(1, lit.Count, $"카드 {i}: 빛을 문 프레임 Image 는 한 장이어야 한다(T153) — 지금 {lit.Count}장");
                    Assert.AreSame(UiKit.ShineTarget(frame), lit[0], $"카드 {i}: 빛은 UiKit.ShineTarget 이 고른 한 장(카드 몸통 «Bg»)에 문다");
                    Assert.IsTrue(lit[0].material.shader.name.Contains("AllIn1SpriteShaderUiMask"), $"카드 {i} 프레임 쉐이더 = UiMask: {lit[0].material.shader.name}");
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
            // 기댓값에 TextGlyphs.Safe 를 씌운다 — 특전 설명 몇 개에 가운뎃점이 있고(perks.json «공격력의 100% · 8마리 관통»), 화면에 나갈 때 UiKit 이 «/» 로 바꾼다(T75 · Jua 에 «·» 글리프가 없어 폭 0 으로 사라진다)
            foreach (var p in offer) Assert.IsTrue(HasText(s => s == TextGlyphs.Safe(PerkText.Format(p.Desc))), $"카드 설명 = «트리거: 내용» 표기(T53) · 한 색(T52): {TextGlyphs.Safe(PerkText.Format(p.Desc))}");
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
                Assert.IsTrue(gt.color == Palette.OnFrame(Palette.PerkGradeName(offer[i].Grade)), $"카드 {i} 등급 글자색 = Palette.OnFrame(밝은 글자 + 검은 아웃라인 · T63 0항 · 결정 259): {gt.color}");
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
            Assert.AreEqual(0, CountShineInstances(), "Close 뒤 «임자 없는» shine 인스턴스 0(카드 파괴 = MaterialOwner 가 인스턴스 파괴 · T61 · 로비 카드처럼 일부러 사는 것은 임자가 있어 안 센다 · T166 ⓑ)");
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
            yield return Frames(2); Assert.AreEqual(0, CountShineInstances(), "보유 특전 닫은 뒤 «임자 없는» shine 인스턴스 0(T61)");

            // 쉼터
            G.Pending = new PendingDecision { Kind = PendingKind.Rest };
            _app.Overlay.Rest(G, heal => G.ResolveRest(heal)); yield return Frames(2);
            Check("쉼터 팝업", expectOverlay: true);
            AssertNoTextClip("쉼터 팝업", _app.Overlay.Root);   // T63-results
            Assert.IsTrue(Click(_app.Overlay.Root, s => s.StartsWith("경험치")), "경험치 선택"); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen); G.Pending = null;

            // 악마의 거래 → 거절 · 악마의 선물
            var dp = Perks.OfferDevil(D, G.Taken, rng);
            G.Pending = new PendingDecision { Kind = PendingKind.Devil, DevilPerk = dp };
            _app.Overlay.Devil(G, accept => G.ResolveDevil(accept)); yield return Frames(2);
            Check("악마 팝업", expectOverlay: true);
            AssertNoTextClip("악마 팝업", _app.Overlay.Root, PerkCardName);   // T63-results — 카드 안 글자는 T63-perks 몫
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "거절"), "거절"); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen); G.Pending = null;
            _app.Overlay.DevilGift(dp, null); yield return Frames(2);
            Check("악마의 선물 팝업", expectOverlay: true);
            AssertNoTextClip("악마의 선물 팝업", _app.Overlay.Root, PerkCardName);   // T63-results
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "계속"), "계속"); yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen);

            // 천사 → 무료 축복 · 광고 카운트다운(1초)
            G.Pending = new PendingDecision { Kind = PendingKind.Angel };
            _app.Overlay.Angel(G, m => G.ResolveAngel(m)); yield return Frames(2);
            Check("천사 팝업", expectOverlay: true);
            AssertNoTextClip("천사 팝업", _app.Overlay.Root);   // T63-results
            Assert.IsTrue(Click(_app.Overlay.Root, s => s.StartsWith("무료 축복")), "무료 축복"); yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen); G.Pending = null;
            bool adDone = false; _app.Overlay.AdCountdown(1, () => adDone = true); yield return Frames(2);
            Check("광고 카운트다운 팝업", expectOverlay: true);
            AssertNoTextClip("광고 카운트다운 팝업", _app.Overlay.Root);   // T63-results
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
            AssertDimCoversFrame(UiKit.Find(_app.Overlay.Root, "Dimmed"), "클리어 팝업 어둠");   // T104 — 프리팹 팝업의 조각 어둠도 프레임 밖까지(Overlay.DimFull)
            var rewardCell = UiKit.Find(_app.Overlay.Root, "Group_RewardItem"); Assert.IsNotNull(rewardCell, "Group_RewardItem"); Assert.AreEqual(UiKit.Fmt(G.Gold), RewardValueText(rewardCell).text, "골드 카운트업 최종값 = G.Gold");
            Check("클리어 팝업", expectOverlay: true);
            // 기댓값에 TextGlyphs.Safe 를 씌운다 — 화면에 나갈 때 UiKit 이 «×» 를 «x» 로 바꾼다(T75 · Jua 에 글리프가 없어 폭 0 으로 사라진다)
            Assert.IsTrue(HasText(s => s == "클리어!"), "제목"); Assert.IsTrue(HasText(s => s == TextGlyphs.Safe(Overlay.ClearAdLabel)), "광고 ×2 버튼(프리팹 Get x2 자리 · T23 · 문구는 T63-results 에서 한 줄로)");
            Assert.IsFalse(HasText(s => s == "다음 챕터"), "«다음 챕터» 버튼은 없다(T23 · 로비의 챕터 화살표로)");
            // T63-results — 프리팹 칸이 좁아 눌리던 세 곳: ×2 버튼 글자(300×100) · 해금 줄(528×61) · 보상 값 칸(여백 15→2px)
            AssertReadable(winBtns.GetChild(0).GetComponentInChildren<Text>(true), TextSize.Button, "×2 버튼");
            AssertReadable(winBtns.GetChild(1).GetComponentInChildren<Text>(true), TextSize.Button, "그냥 받기 버튼");
            var unlockT = UiKit.Find(_app.Overlay.Root, "Text (1)"); Assert.IsNotNull(unlockT, "해금 줄(프리팹 «Text (1)»)");
            AssertReadable(unlockT.GetComponent<Text>(), TextSize.Body, "해금 줄");
            AssertReadable(RewardValueText(rewardCell), TextSize.Body, "클리어 보상 골드");
            AssertNoTextClip("클리어 팝업", _app.Overlay.Root);
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
            // T63-results — 팁 3줄은 프리팹 줄 글자 칸(730×82)에 «본문 40 한 줄» 로 들어가야 한다(전엔 둘째 줄이 35 로 눌렸다)
            for (int i = 0; i < 3 && i < tipList.childCount; i++) AssertReadable(tipList.GetChild(i).GetComponentInChildren<Text>(true), TextSize.Body, $"팁 {i}");
            AssertNoTextClip("사망 팝업", _app.Overlay.Root);
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
