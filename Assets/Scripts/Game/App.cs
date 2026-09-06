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
            Audio.Create(app);   // 배경음·효과음(T28) — 세이브(음소거)와 카탈로그(bgm.*/snd.*)를 읽는다 · BuildUi 의 첫 ShowScreen 이 로비 곡을 튼다
            app.BuildUi();
            return app;
        }

        void BuildUi()
        {
            UiKit.EnsureEventSystem();
            UiCanvas = UiKit.CreateRootCanvas("UI", 10);
            Frame = UiKit.CreateFrame(UiCanvas.transform);
            Frame.GetComponent<Image>().enabled = false;   // 프레임 안은 각 화면이 채운다 · 전투는 카메라가 보인다
            if (WorldCamera != null)
            {
                WorldCam.Attach(WorldCamera, Frame);
                WorldCamera.clearFlags = CameraClearFlags.SolidColor; WorldCamera.backgroundColor = Palette.Hex("#86E4FF");   // GUI Pro 로비 배경 하늘색
            }
            Register(new LobbyScreen()); Register(new GearScreen()); Register(new ForgeScreen()); Register(new ShopScreen()); Register(new BattleScreen());
            Overlay = new Overlay(this);
            // 토스트 (GUI Pro ToastMessage_01)
            _toastRt = (RectTransform)UiKit.Spawn("ui.toast", Frame).transform; UiKit.Pct(_toastRt, 4, 84, 92, 5);
            _toastText = _toastRt.GetComponentInChildren<Text>(true);
            _toastRt.gameObject.SetActive(false);
            ShowScreen("lobby");
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

        public void Toast(string msg)
        {
            if (_toastText != null) _toastText.text = msg;
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
