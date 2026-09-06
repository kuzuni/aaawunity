using System;
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
            Application.runInBackground = true;   // 창/탭이 포커스를 잃어도 계속 돈다(주인: «유튜브 보면서») · 탭이 숨겨져 브라우저가 멈추면 BattleScreen 이 돌아올 때 그 시간을 따라잡는다
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
            d.Shop = LoadShop(catalog);
            d.DailyGift = LoadDailyGift(catalog);
            d.ArenaDummy = LoadArenaDummy(catalog);
            d.Expedition = LoadExpedition(catalog);
            App.Create(d, catalog, uiFont, Camera.main);
            if (_boot != null) Destroy(_boot.gameObject);
        }

        /// <summary>상점 상품표 — 이 레포 전용 <c>Assets/KkomaKnight/shop.json</c>(카탈로그 텍스트 «data.shop» · T9). aaaw 동기 폴더(StreamingAssets/data)가 아니라 카탈로그 참조로 빌드에 들어간다. 못 읽으면 null(상점이 상품 없이 뜨고 에러 로그 1줄).</summary>
        static ShopData LoadShop(AssetCatalog catalog)
        {
            var ta = catalog != null ? catalog.Text("data.shop") : null;
            if (ta == null) { Debug.LogError("[KkomaKnight] shop.json 이 카탈로그(data.shop)에 없다 — 상점 상품표 없음"); return null; }
            try { return ShopData.Parse(ta.text); }
            catch (Exception e) { Debug.LogError("[KkomaKnight] shop.json 파싱 실패: " + e.Message); return null; }
        }

        /// <summary>데일리 기프트 수치표 — 이 레포 전용 <c>Assets/KkomaKnight/dailyGift.json</c>(카탈로그 텍스트 «data.dailyGift» · T77). 못 읽으면 null(팝업이 줄 없이 뜨고 에러 로그 1줄).</summary>
        static DailyGiftData LoadDailyGift(AssetCatalog catalog)
        {
            var ta = catalog != null ? catalog.Text("data.dailyGift") : null;
            if (ta == null) { Debug.LogError("[KkomaKnight] dailyGift.json 이 카탈로그(data.dailyGift)에 없다 — 데일리 기프트 표 없음"); return null; }
            try { return DailyGiftData.Parse(ta.text); }
            catch (Exception e) { Debug.LogError("[KkomaKnight] dailyGift.json 파싱 실패: " + e.Message); return null; }
        }

        /// <summary>탐험 수치표 — 이 레포 전용 <c>Assets/KkomaKnight/expedition.json</c>(카탈로그 텍스트 «data.expedition» · T97). 못 읽으면 null(탐험 팝업이 «--» 로 뜬다).</summary>
        static ExpeditionData LoadExpedition(AssetCatalog catalog)
        {
            var ta = catalog != null ? catalog.Text("data.expedition") : null;
            if (ta == null) { Debug.LogError("[KkomaKnight] expedition.json 이 카탈로그(data.expedition)에 없다 — 탐험 표 없음"); return null; }
            try { return ExpeditionData.Parse(ta.text); }
            catch (Exception e) { Debug.LogError("[KkomaKnight] expedition.json 파싱 실패: " + e.Message); return null; }
        }

        /// <summary>아레나 더미 계수 — 이 레포 전용 <c>Assets/KkomaKnight/arenaDummy.json</c>(카탈로그 텍스트 «data.arenaDummy» · T81). 못 읽으면 null(23·24 숫자가 «—» 로 남는다).</summary>
        static ArenaDummyData LoadArenaDummy(AssetCatalog catalog)
        {
            var ta = catalog != null ? catalog.Text("data.arenaDummy") : null;
            if (ta == null) { Debug.LogError("[KkomaKnight] arenaDummy.json 이 카탈로그(data.arenaDummy)에 없다 — 아레나 더미 계수 없음"); return null; }
            try { return ArenaDummyData.Parse(ta.text); }
            catch (Exception e) { Debug.LogError("[KkomaKnight] arenaDummy.json 파싱 실패: " + e.Message); return null; }
        }

        void OnError(string msg)
        {
            _status.text = msg;
            _status.color = new Color(1f, 0.45f, 0.45f);
            Debug.LogError("[KkomaKnight] " + msg);
        }
    }
}
