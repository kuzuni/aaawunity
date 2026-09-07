using System.Collections;
using System.Collections.Generic;
using System.Text;
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
    /// T163 — 빨간 알림 점은 <b>어느 화면에서도 원</b>이어야 한다(주인 2026-09-07 «여전히 탐험, 클리어 보상 쪽에 빨간 점이 찌그러져 있음»).
    /// <para>
    /// T136 이 <see cref="UiKit.AlertDot"/> 한 곳으로 모으면서 rect 는 정사각(«지름» 하나)이 됐지만 <b>게이트가 없었다</b> —
    /// 그래서 이 자가 지시서 T163 4항대로 «<b>화면에 그려지는</b> 사각형» 을 잰다. <c>rect</c> 만 재면 안 된다:
    /// 조상 어딘가에 <c>lossyScale.x ≠ lossyScale.y</c> 가 걸리면 정사각 rect 도 타원으로 그려진다(4항 ⓐ).
    /// </para>
    /// 점을 찾는 법은 <b>이름이 아니라 그림</b>이다 — 조각(<see cref="UiKit.AlertDotKey"/>)의 스프라이트를 그대로 쓰는 <see cref="Image"/> 를 전부 센다.
    /// <see cref="UiKit.AlertDot"/> 이 인스턴스 이름을 화면마다 다르게 붙이므로(<c>MenuDot</c>·<c>ExpDot</c>·<c>ChestDot</c>…) 이름으로는 다 못 찾는다.
    /// 꺼져 있는 점(받을 것이 없을 때)도 센다 — 자리·크기는 켜지기 전에 이미 정해져 있고, 앵커가 한 점이라 <c>rect</c> 는 레이아웃과 무관하게 <c>sizeDelta</c> 그대로다.
    /// </summary>
    public class AlertDotTests
    {
        App _app; PlayLog _log;
        /// <summary>«원이다» 로 볼 허용 오차(프레임 px) — 반올림 한 픽셀.</summary>
        const float Tol = 1f;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; Assert.IsNotNull(_app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }

        /// <summary>조각의 스프라이트 — 점 인스턴스는 전부 이것을 쓴다.</summary>
        Sprite DotSprite()
        {
            var prefab = _app.Assets.Prefab(UiKit.AlertDotKey);
            Assert.IsNotNull(prefab, "빨간 점 조각(" + UiKit.AlertDotKey + ")이 카탈로그에 있어야 한다");
            var img = prefab.GetComponentInChildren<Image>(true);
            Assert.IsNotNull(img, "그 조각에 Image 가 있어야 한다");
            Assert.IsNotNull(img.sprite, "그 Image 에 스프라이트가 물려 있어야 한다");
            // 그림 자체가 정사각인지도 여기서 본다(4항 ⓒ) — 그림이 눌려 있으면 rect 를 아무리 맞춰도 원이 안 된다
            var r = img.sprite.rect;
            Assert.AreEqual(r.width, r.height, Tol, "조각 스프라이트가 정사각이어야 한다(" + r.width + "x" + r.height + ")");
            return img.sprite;
        }

        /// <summary>
        /// 지금 씬의 점을 전부 재서 «그려지는 사각형» 을 표로 남기고, <b>우리가 세운 점</b>은 정사각인지 단언한다.
        /// <para>
        /// 실패로 세는 것은 <see cref="UiKit.AlertDot"/> 이 세운 것 = 이름이 <c>…Dot</c> 인 인스턴스뿐이다(<c>MenuDot</c>·<c>GiftDot</c>·<c>ExpDot</c>·<c>ChestDot</c>…).
        /// 같은 그림을 <b>조각이 스스로 달고 온</b> 자리(장비 칸·슬롯 프리팹 안의 점)는 표에만 남긴다 — 그 자리는 그 화면 묶음 워커 몫이고,
        /// 여기서 바로 실패로 세면 남의 화면 때문에 main 이 빨개진다(<c>TextAudit.ClipStrict</c>·<c>GlyphStrict</c> 가 밟은 «표 먼저 · 0 이면 strict» 순서 · 결정 기록).
        /// </para>
        /// </summary>
        void CheckDots(Sprite dot, string where, List<string> log, ref int seen, ref int ours)
        {
            foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (img == null || img.sprite != dot) continue;
                var rt = img.rectTransform; if (rt == null) continue;
                var ls = rt.lossyScale;
                float w = rt.rect.width * Mathf.Abs(ls.x), h = rt.rect.height * Mathf.Abs(ls.y);
                bool mine = rt.name.EndsWith("Dot", System.StringComparison.Ordinal);
                seen++;
                if (mine) ours++;
                log.Add($"| {where} | {Path(rt)} | {(mine ? "AlertDot" : "조각")} | {rt.rect.width:0.#}x{rt.rect.height:0.#} | {ls.x:0.###}/{ls.y:0.###} | {w:0.#}x{h:0.#} |");
                if (!mine) continue;
                Assert.AreEqual(w, h, Tol,
                    $"[{where}] «{Path(rt)}» 의 그려지는 점이 정사각이 아니다 — rect {rt.rect.width:0.#}x{rt.rect.height:0.#} · lossyScale {ls.x:0.###}/{ls.y:0.###} → {w:0.#}x{h:0.#}");
            }
        }
        static string Path(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        /// <summary>
        /// T163 — 점이 서는 화면을 훑어 «그려지는 사각형» 이 전부 정사각인지.
        /// 주인이 부른 자리(로비 보조 줄 «탐험»·«클리어 보상»)는 이름으로도 따로 못 박는다.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryAlertDotIsDrawnAsACircle()
        {
            yield return Boot();
            var dot = DotSprite();
            var log = new List<string> { "| 화면 | 자리 | 누가 세웠나 | rect | lossyScale | 그려지는 크기 |", "|---|---|---|---|---|---|" };
            int seen = 0, ours = 0;

            foreach (var scr in new[] { "lobby", "gear", "shop", "pet", "events" })
            {
                _app.ShowScreen(scr); yield return Frames(2); Canvas.ForceUpdateCanvases();
                CheckDots(dot, scr, log, ref seen, ref ours);
            }

            // 주인이 짚은 두 자리 — 로비 보조 줄. 켜져 있든 꺼져 있든 «그려질 크기» 는 지금 정해져 있다.
            _app.ShowScreen("lobby"); yield return Frames(2); Canvas.ForceUpdateCanvases();
            foreach (var n in new[] { "ExpDot", "ChestDot" })
            {
                var t = UiKit.Find(_app.Current.Root, n);
                Assert.IsNotNull(t, "로비 보조 줄의 «" + n + "»(주인이 짚은 자리)");
                var rt = (RectTransform)t; var ls = rt.lossyScale;
                Assert.AreEqual(rt.rect.width * Mathf.Abs(ls.x), rt.rect.height * Mathf.Abs(ls.y), Tol, n + " 이 원이어야 한다");
                Assert.AreEqual(rt.anchorMin, rt.anchorMax, "점은 «모서리 + px» 로 잰다(앵커가 벌어지면 칸 비율대로 늘어난다 · T136)");
            }

            Assert.Greater(ours, 0, "훑은 화면에 우리가 세운 점이 하나도 없으면 이 게이트가 아무것도 안 재고 있는 것이다");
            Debug.Log($"[AlertDotGate] 점 {seen}개(그중 AlertDot 이 세운 것 {ours}개 = 실패로 세는 것) · 나머지는 조각이 달고 온 것이라 표로만 남긴다\n" + string.Join("\n", log));
            _log.AssertNoRed("빨간 점 게이트(T163)");
            yield return Shutdown();
        }
    }
}
