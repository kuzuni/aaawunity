using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 씬의 유일한 손수 배치 오브젝트. 데이터를 올린 뒤 게임 UI 를 코드로 세운다
    /// (프리팹·씬을 텍스트로 손수 쓰는 대신 런타임 생성 — 에디터 없는 작업 환경의 기본 전략).
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        /// <summary>씬에서 주입되는 UI 폰트 (Assets/Fonts/Jua-Regular.ttf · Google Fonts OFL). 비면 내장 폰트.</summary>
        public Font uiFont;

        Text _status;

        void Awake()
        {
            if (uiFont != null) UiKit.DefaultFont = uiFont;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void Start()
        {
            UiKit.EnsureEventSystem();
            var canvas = UiKit.CreateRootCanvas("BootCanvas");
            _status = UiKit.Text(canvas.transform, "데이터 로드 중…", 28, Color.white);
            UiKit.Stretch(_status.rectTransform);
            StartCoroutine(DataLoader.Load(OnLoaded, OnError));
        }

        void OnLoaded(GameData d)
        {
            _status.text = $"데이터 로드 완료\n챕터 {d.Enemies.Chapters.Count} · 특전 {d.Perks.Perks.Count} · 장비 {d.Gear.AllTypes.Count}종\n{d.Tune.Source}";
            Debug.Log($"[KkomaKnight] data loaded: chapters={d.Enemies.Chapters.Count} perks={d.Perks.Perks.Count} source={d.Tune.Source}");
        }

        void OnError(string msg)
        {
            _status.text = msg;
            _status.color = new Color(1f, 0.45f, 0.45f);
            Debug.LogError("[KkomaKnight] " + msg);
        }
    }
}
