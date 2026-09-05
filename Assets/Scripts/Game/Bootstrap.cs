using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 씬의 유일한 손수 배치 오브젝트. 데이터를 올린 뒤 <see cref="App"/> 을 세운다
    /// (프리팹·씬을 텍스트로 손수 쓰는 대신 런타임 생성 — 에디터 없는 작업 환경의 기본 전략).
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        /// <summary>씬에서 주입되는 UI 폰트 (Assets/Fonts/Jua-Regular.ttf · Google Fonts OFL). 비면 카탈로그 font.ui → 내장 폰트.</summary>
        public Font uiFont;
        /// <summary>주인 에셋 카탈로그 (Assets/KkomaKnight/AssetCatalog.asset — tools/gen_catalog.py 생성).</summary>
        public AssetCatalog catalog;

        Text _status; Canvas _boot;

        void Awake()
        {
            if (uiFont != null) UiKit.DefaultFont = uiFont;
            // 주인 지시(2026-09-05): 60fps · 백그라운드에서도 실행.
            // vSync 를 끄지 않으면 targetFrameRate 가 무시된다(Android 기본 품질 Medium = vSync 1). WebGL 은 -1(브라우저 rAF = 화면 주사율 · 60Hz 에서 60fps)
            // 가 유니티 권고 — 60 을 박으면 setTimeout 루프로 바뀌어 오히려 프레임이 고르지 않다.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Application.platform == RuntimePlatform.WebGLPlayer ? -1 : 60;
            Application.runInBackground = true;   // 창/탭 포커스를 잃어도 계속 돈다 (ProjectSettings runInBackground 도 1)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void Start()
        {
            UiKit.EnsureEventSystem();
            _boot = UiKit.CreateRootCanvas("BootCanvas");
            _status = UiKit.Text(_boot.transform, "데이터 로드 중…", 60, Color.white);
            UiKit.Stretch(_status.rectTransform);
            StartCoroutine(DataLoader.Load(OnLoaded, OnError));
        }

        void OnLoaded(GameData d)
        {
            Debug.Log($"[KkomaKnight] data loaded: chapters={d.Enemies.Chapters.Count} perks={d.Perks.Perks.Count} source={d.Tune.Source}");
            if (catalog == null) Debug.LogError("[KkomaKnight] AssetCatalog 이 씬에 연결되지 않았다 — Bootstrap.catalog");
            App.Create(d, catalog, uiFont, Camera.main);
            if (_boot != null) Destroy(_boot.gameObject);
        }

        void OnError(string msg)
        {
            _status.text = msg;
            _status.color = new Color(1f, 0.45f, 0.45f);
            Debug.LogError("[KkomaKnight] " + msg);
        }
    }
}
