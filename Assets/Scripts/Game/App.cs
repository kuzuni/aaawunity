using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 게임 루트 — 데이터·세이브·카탈로그·프레임·화면 전환·오버레이·토스트. index.html 의 «전역 + showScreen» 에 해당.
    /// 화면(GameScreen) 은 각자 프레임 안에 자기 RectTransform 을 세우고, App 이 하나만 켠다.
    /// </summary>
    public sealed class App : MonoBehaviour
    {
        public static App I { get; private set; }
        public GameData Data { get; private set; }
        public SaveData Save { get; private set; }
        public AssetCatalog Assets { get; private set; }
        public RectTransform Frame { get; private set; }
        /// <summary>T106 — 노치·펀치홀을 피한 영역(<see cref="SafeAreaRoot"/>). <see cref="Frame"/> 의 부모다.</summary>
        public RectTransform SafeArea { get; private set; }
        public Canvas UiCanvas { get; private set; }
        public Overlay Overlay { get; private set; }
        public Camera WorldCamera { get; private set; }

        readonly Dictionary<string, GameScreen> _screens = new Dictionary<string, GameScreen>();
        GameScreen _current;
        RectTransform _toastRt; Text _toastText; float _toastT;

        public static App Create(GameData data, AssetCatalog catalog, Font font, Camera worldCamera)
        {
            var go = new GameObject("App");
            var app = go.AddComponent<App>();
            I = app;
            app.Data = data; app.Assets = catalog; app.WorldCamera = worldCamera;
            if (font == null && catalog != null) font = catalog.Font("font.ui");
            if (font != null) UiKit.DefaultFont = font;
            app.Save = SaveStore.Load(data);
            Debug.Log("[KkomaKnight] boot: save");   // T59 진단 마커 — 브라우저 콘솔에서 어디까지 왔는지(WebGL 크래시 위치 좁히기 · 릴리스에서도 무해한 한 줄)
            Audio.Create(app);   // 배경음·효과음(T28) — 세이브(음소거)와 카탈로그(bgm.*/snd.*)를 읽는다 · BuildUi 의 첫 ShowScreen 이 로비 곡을 튼다
            Debug.Log("[KkomaKnight] boot: audio");
            app.BuildUi();
            return app;
        }

        void BuildUi()
        {
            UiKit.EnsureEventSystem();
            UiCanvas = UiKit.CreateRootCanvas("UI", 10);
            SafeArea = UiKit.CreateSafeArea(UiCanvas.transform);   // T106 — 노치를 피한 영역 · 화면 UI 는 전부 이 안(상단·하단 프레임 띠만 이 밖으로 뻗는다)
            Frame = UiKit.CreateFrame(SafeArea);
            Frame.GetComponent<Image>().enabled = false;   // 프레임 안은 각 화면이 채운다 · 전투는 카메라가 보인다
            if (WorldCamera != null)
            {
                WorldCam.Attach(WorldCamera, Frame);
                WorldCamera.clearFlags = CameraClearFlags.SolidColor; WorldCamera.backgroundColor = Palette.Hex("#86E4FF");   // GUI Pro 로비 배경 하늘색
            }
            Register(new LobbyScreen()); Register(new GearScreen()); Register(new ForgeScreen()); Register(new ShopScreen()); Register(new PetScreen()); Register(new BattleScreen()); Register(new EventsScreen());
            // T44 로비 사이드 페이지(특권 · 껍데기) · 시즌 패스 페이지는 T78(주인 2026-09-07)로 삭제 · T98 챕터 보상 페이지(레퍼런스 32)
            Register(new PrivilegeScreen()); Register(new ChapterChestScreen());
            Overlay = new Overlay(this);
            // 토스트 (GUI Pro ToastMessage_01) — 칸 세로는 본문 40 두 줄이 들어가는 Layout.Toast (T63-toast · 전 5.0% 에선 긴 문구가 bestFit 으로 32 까지 줄었다)
            _toastRt = (RectTransform)UiKit.Spawn("ui.toast", Frame).transform; UiKit.Pct(_toastRt, Layout.Toast);
            _toastText = _toastRt.GetComponentInChildren<Text>(true);
            _toastRt.gameObject.SetActive(false);
            Debug.Log("[KkomaKnight] boot: ui");
            ShowScreen("lobby");
            Debug.Log("[KkomaKnight] ready lobby");   // T60 배포 스모크가 기다리는 마커(tools/webgl_smoke.js) — 문구 바꾸면 스크립트도 같이
        }

        void Register(GameScreen s) { s.App = this; _screens[s.Name] = s; }

        public void ShowScreen(string name)
        {
            if (!_screens.TryGetValue(name, out var s)) { Debug.LogError("화면 없음: " + name); return; }
            if (_current != null && _current != s) _current.Hide();
            _current = s;
            s.Show();
            Audio.Bgm(name == "battle" ? "bgm.battle" : "bgm.lobby");   // 화면 전환 시 로비/전투 곡 자동 교체(T28 · 같은 곡이면 무시 · 보스 곡은 BattleWorld 가)
            Overlay?.Root.SetAsLastSibling();
            _toastRt.SetAsLastSibling();
        }
        public GameScreen Current => _current;
        public T GetScreen<T>() where T : GameScreen { foreach (var s in _screens.Values) if (s is T t) return t; return null; }

        public void StartBattle(int chapter)
        {
            chapter = Mathf.Clamp(chapter, 1, Math.Max(1, Save.MaxChapter));
            ShowScreen("battle");
            GetScreen<BattleScreen>().Start(chapter);
            Debug.Log("[KkomaKnight] ready battle");   // T60 배포 스모크 마커
        }

        /// <summary>
        /// 배포 스모크 진단 훅(T60) — 브라우저 JS 가 <c>unityInstance.SendMessage("App", "DebugGo", "battle")</c> 로 부른다(GameObject 이름 = «App»).
        /// «battle» = 선택 챕터로 전투 진입 · «lobby» = 로비 · «perf» = 지금 도는 트윈 수를 로그 한 줄로(T129 ⓑ · 화면은 안 바뀐다). 그 외는 무시(로그 한 줄).
        /// 게임 로직은 StartBattle/ShowScreen 그대로 — 새 기능이 아니라 진입 경로만 연다.
        /// </summary>
        public void DebugGo(string what)
        {
            if (Save == null || Data == null) { Debug.LogWarning("[KkomaKnight] DebugGo: 아직 준비 전 — " + what); return; }
            switch (what)
            {
                case "battle": StartBattle(Save.SelChapter); break;
                case "lobby": Overlay?.Close(); GetScreen<BattleScreen>()?.Abort(); ShowScreen("lobby"); break;
                // T129 ⓑ — «지금 몇 개가 도나» 한 줄. 스모크가 fps 를 재기 직전에 불러 fps 옆에 같이 적는다(문구 바꾸면 tools/webgl_smoke.js 도 같이).
                case "perf": Debug.Log("[KkomaKnight] perf tweens=" + UiKit.PlayingTweens() + " screen=" + (_current != null ? _current.Name : "-")); break;
                default: Debug.Log("[KkomaKnight] DebugGo: 모르는 목적지 — " + what); break;
            }
        }

        public void Persist() => SaveStore.Save(Save);

        /// <summary>
        /// «데이터 삭제»(T29) — 세이브 키 삭제 → 새 세이브로 교체 → 전투 중이면 판을 버리고(골드 은행 없음) → 로비를 새로 그린다. 설정 팝업의 확인(«삭제»)에서만 부른다.
        /// 화면들은 전부 <see cref="Save"/> 를 매번 읽으므로(캐시 없음) 교체 뒤 <see cref="ShowScreen"/> 의 Refresh 가 새 값을 그린다. 음소거는 새 세이브(해제)로 바로 반영.
        /// </summary>
        public void ResetSave()
        {
            Overlay?.Close();
            GetScreen<BattleScreen>()?.Abort();
            Save = SaveStore.Reset(Data);
            Audio.ApplyMute();
            ShowScreen("lobby");
            Toast("데이터를 삭제했습니다");
        }

        /// <summary>
        /// 화면 아래 토스트 한 줄. 문구는 <see cref="TextGlyphs.Safe"/> 로 거른다 — Jua 에 없는 «·»·«×»·«→»·이모지는 유니티가 <b>폭 0</b> 으로 흘려
        /// «같은 부위·종류·등급만» 이 «같은 부위종류등급만» 으로 붙어 나왔다(T63-toast). 문구 자체는 부르는 화면 코드의 것이라 여기서 한 번에 거른다.
        /// </summary>
        public void Toast(string msg)
        {
            if (_toastText != null) _toastText.text = TextGlyphs.Safe(msg);
            _toastRt.gameObject.SetActive(true); _toastRt.SetAsLastSibling(); _toastT = 1.8f; UiKit.PopIn(_toastRt, 0.9f, 0.2f);
        }

        void Update()
        {
            if (_toastT > 0) { _toastT -= Time.unscaledDeltaTime; if (_toastT <= 0) _toastRt.gameObject.SetActive(false); }
            _current?.Tick(Time.deltaTime);
            Overlay?.Tick(Time.unscaledDeltaTime);
        }

        /// <summary>전투력 표시식 (index.html `power()` · 주인 확정 2026-09-03) = 공×8 + (체+실)×1.5 — 표시 전용.</summary>
        public double Power()
        {
            var pw = GearSystem.BuildPower(Data, Save.CurBuild(Data));
            return Math.Round(pw.Atk * 8 + (pw.Hp + pw.Sh) * 1.5);
        }
    }

    /// <summary>한 화면(UnityEngine.Screen 과 이름이 겹치지 않게 GameScreen). Show 에서 처음 한 번 Build 하고, 이후 Refresh 로 다시 그린다.</summary>
    public abstract class GameScreen
    {
        public App App;
        public abstract string Name { get; }
        public RectTransform Root { get; private set; }
        bool _built;
        public void Show()
        {
            if (!_built) { Root = UiKit.Rect(App.Frame, "Screen:" + Name); UiKit.Stretch(Root); Build(); _built = true; }
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            Refresh();
        }
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); OnHide(); }
        protected abstract void Build();
        public virtual void Refresh() { }
        public virtual void Tick(float dt) { }
        protected virtual void OnHide() { }
    }
}
