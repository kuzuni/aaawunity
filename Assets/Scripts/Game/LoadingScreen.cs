using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 부팅 로딩 화면(T96-loading · 주인 2026-09-07 «`Title_Loading` … 이거 좀 써라 프리팹들») — 데모 프리팹 <c>Title_Loading</c> **그대로**.
    /// 데이터·에셋을 읽는 동안 뜨고 다 읽으면 사라진다(진행 바 = 실제 로드 진행 · <see cref="DataLoader.Load"/> 의 진행률).
    /// <list type="bullet">
    /// <item>유니티 로더(WebGL 첫 로딩)가 **끝난 뒤** 우리 로딩이라 겹치지 않는다 — 이 화면은 <see cref="Bootstrap"/> 의 <c>Start</c> 에서 뜬다.</item>
    /// <item><see cref="MinSeconds"/>(0.3s) 는 «깜빡임 방지» — 데이터가 순식간에 읽혀도 화면이 한 프레임만 번쩍이지 않는다.</item>
    /// <item>조각이 없으면(카탈로그 결손) <c>null</c> 을 돌려주고 <see cref="Bootstrap"/> 이 예전처럼 글자 한 줄을 띄운다 — 부팅은 무슨 일이 있어도 막히지 않는다.</item>
    /// </list>
    /// </summary>
    public sealed class LoadingScreen
    {
        /// <summary>카탈로그 키(주인 지목 프리팹).</summary>
        public const string Key = "ui.titleLoading";
        /// <summary>진행 바 조각 이름(프리팹 그대로).</summary>
        public const string BarName = "Slider_01_Yellow";
        /// <summary>깜빡임 방지 최소 표시 시간(초 · ROUTINE T96 ⓓ «0.3s 정도»).</summary>
        public const float MinSeconds = 0.3f;

        /// <summary>세운 조각(테스트·Bootstrap 이 본다).</summary>
        public GameObject Root { get; private set; }
        Slider _bar; Text _pct;
        float _shownAt;

        /// <summary>화면에 떠 있은 시간(초) — <see cref="MinSeconds"/> 를 채웠는지 <see cref="Bootstrap"/> 이 본다.</summary>
        public float Elapsed => Time.realtimeSinceStartup - _shownAt;

        /// <summary>로딩 화면을 띄운다 — 조각이 없으면 null(부팅은 계속된다).</summary>
        public static LoadingScreen Show(Transform parent, AssetCatalog cat)
        {
            if (parent == null || cat == null || cat.Prefab(Key) == null) return null;
            var go = UiKit.SpawnWith(cat, Key, parent);
            var rt = go.transform as RectTransform; if (rt != null) UiKit.Stretch(rt);
            var s = new LoadingScreen { Root = go, _shownAt = Time.realtimeSinceStartup };
            s._bar = go.GetComponentInChildren<Slider>(true);
            if (s._bar != null) { s._bar.minValue = 0f; s._bar.maxValue = 1f; s._bar.value = 0f; s._bar.interactable = false; }
            // 프리팹의 버전 글자(«Ver. 1.0.130») 자리를 진행 글자로 쓴다 — 새 글자를 만들지 않는다(프리팹 그대로)
            foreach (var t in go.GetComponentsInChildren<Text>(true)) { s._pct = t; break; }
            s.SetProgress(0f);
            return s;
        }

        /// <summary>진행률 0~1 — 바와 글자를 같이 움직인다.</summary>
        public void SetProgress(float p)
        {
            p = Mathf.Clamp01(p);
            if (_bar != null) _bar.value = p;
            if (_pct != null) _pct.text = TextGlyphs.Safe($"불러오는 중… {Mathf.RoundToInt(p * 100f)}%");
        }

        /// <summary>로딩 화면을 지운다(부팅 캔버스째 지우는 것은 <see cref="Bootstrap"/> 몫).</summary>
        public void Hide()
        {
            if (Root != null) Object.Destroy(Root);
            Root = null; _bar = null; _pct = null;
        }
    }
}
