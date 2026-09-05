using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 게임 루트 — 데이터·세이브·카탈로그·프레임·화면 전환·오버레이·토스트. index.html 의 «전역 + showScreen» 에 해당.
    /// 화면(Screen) 은 각자 프레임 안에 자기 RectTransform 을 세우고, App 이 하나만 켠다.
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
            if (font != null) UiKit.DefaultFont = font;
            app.Save = SaveStore.Load(data);
            app.BuildUi();
            return app;
        }

        void BuildUi()
        {
            UiKit.EnsureEventSystem();
            UiCanvas = UiKit.CreateRootCanvas("UI", 10);
            Frame = UiKit.CreateFrame(UiCanvas.transform);
            Frame.GetComponent<Image>().color = Palette.Bg;   // 프레임 밖(letterbox)은 카메라 배경, 안은 각 화면이 채운다
            Frame.GetComponent<Image>().enabled = false;
            Register(new LobbyScreen()); Register(new GearScreen()); Register(new ForgeScreen()); Register(new ShopScreen()); Register(new BattleScreen());
            Overlay = new Overlay(this);
            // 토스트 (index.html #toast — 하단 탭바 위)
            _toastRt = UiKit.Rect(Frame, "Toast");
            var bg = _toastRt.gameObject.AddComponent<Image>(); bg.sprite = UiKit.Round(); bg.type = Image.Type.Sliced; bg.color = new Color(0.08f, 0.08f, 0.1f, 0.92f); bg.raycastTarget = false;
            UiKit.Pct(_toastRt, 6, 84, 88, 4.5f);
            _toastText = UiKit.Text(_toastRt, "", 15, Palette.Ink, TextAnchor.MiddleCenter, true); UiKit.Stretch(_toastText.rectTransform, 8, 2, 8, 2);
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
            if (WorldCamera != null) WorldCamera.gameObject.SetActive(name == "battle");
            _toastRt.SetAsLastSibling();
        }
        public GameScreen Current => _current;
        public T GetScreen<T>() where T : GameScreen { foreach (var s in _screens.Values) if (s is T t) return t; return null; }

        public void Persist() => SaveStore.Save(Save);

        public void Toast(string msg)
        {
            _toastText.text = msg; _toastRt.gameObject.SetActive(true); _toastRt.SetAsLastSibling(); _toastT = 1.8f;
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
