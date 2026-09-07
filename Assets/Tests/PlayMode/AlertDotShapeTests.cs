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
    /// T136 게이트 — **빨간 알림 점은 어디서나 «원» 이다**(주인 2026-09-07 «보니까 빨간점들이 찌그러져있더라»).
    /// 조각 <c>Alert_Dot_01_Red</c> 는 그림 하나(47×47 · <c>preserveAspect</c> 꺼짐)라 rect 가 정사각이 아니면 그림이 그대로 늘어난다 —
    /// 로비 보조 줄의 점이 <b>55×24 = 2.26:1 타원</b>이었던 까닭이 그것이다(점을 부모 «칸의 %» 로 쟀다).
    ///
    /// 그래서 «어떻게 세웠는가» 가 아니라 **화면에 선 결과**를 잰다: 점 조각의 그림(스프라이트)으로 인스턴스를 전부 찾아
    /// 세계 크기(rect × <c>lossyScale</c>)의 |가로 − 세로| 가 1px 이하인지 본다. 이름이 제각각(<c>MenuDot</c>·<c>GiftDot</c>·<c>FuseDot</c>·<c>New</c>·
    /// <c>Alert_Dot_01_Red</c>…)이라 이름으로 찾으면 새로 생긴 점을 놓친다.
    /// 켜져 있든 꺼져 있든(받을 것이 없어 <c>SetActive(false)</c> 인 점도) 다 잰다 — 찌그러짐은 켜지는 순간 보이므로 «지금 꺼져 있다» 는 면죄가 아니다.
    /// </summary>
    public class AlertDotShapeTests
    {
        App _app; PlayLog _log;
        /// <summary>«원» 으로 치는 가로·세로 차이 한계(px · 프레임 1080×2337 기준).</summary>
        const float Tol = 1f;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
        }

        /// <summary>점 조각이 쓰는 그림 — 이것으로 인스턴스를 찾는다(이름이 아니라 조각으로 · ROUTINE T136 3항).</summary>
        Sprite DotSprite()
        {
            var prefab = _app.Assets.Prefab(UiKit.AlertDotKey);
            Assert.IsNotNull(prefab, "카탈로그에 " + UiKit.AlertDotKey + " 가 있어야 한다");
            var img = prefab.GetComponentInChildren<Image>(true);
            Assert.IsNotNull(img, "점 조각은 그림 하나다");
            return img.sprite;
        }

        /// <summary>한 화면을 훑어 찌그러진 점을 적는다(발견 = 실패 · 잰 값은 표로 남겨 CI 로그에서 바로 읽는다).</summary>
        IEnumerator Scan(string screen, Sprite dot, List<string> bad, StringBuilder table)
        {
            UiKit.CompleteAllTweens();
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cv == null || !cv.isRootCanvas) continue;
                foreach (var img in cv.GetComponentsInChildren<Image>(true))
                {
                    if (img == null || img.sprite != dot) continue;
                    var rt = img.rectTransform; var s = rt.lossyScale;
                    float w = rt.rect.width * Mathf.Abs(s.x), h = rt.rect.height * Mathf.Abs(s.y);
                    string where = screen + "\t" + Path(rt) + "\t" + w.ToString("0.0") + "×" + h.ToString("0.0");
                    table.Append("[AlertDot] ").Append(where).Append('\n');
                    if (Mathf.Abs(w - h) > Tol) bad.Add(where + " ← 가로:세로 " + (h > 0.01f ? (w / h).ToString("0.00") : "?") + " : 1");
                }
            }
            yield return Frames(1);
        }
        static string Path(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        [UnityTest]
        public IEnumerator EveryAlertDotIsRoundOnEveryScreen()
        {
            yield return Boot();
            var dot = DotSprite();
            var bad = new List<string>(); var table = new StringBuilder();
            var S = _app.Save; var D = _app.Data;

            // 01 로비 — 주인이 본 그 자리(보조 줄 «탐험»·«클리어 보상» · ≡ 버튼)
            Assert.AreEqual("lobby", _app.Current.Name);
            yield return Scan("01_lobby", dot, bad, table);

            // ≡ 메뉴 팝업(줄마다 점 하나)
            LobbyMenu.Open(_app); yield return Frames(2);
            yield return Scan("01_lobby_menu", dot, bad, table);
            _app.Overlay.Close(); yield return Frames(1);

            // 06 장비 — 슬롯 6칸 점 + «대장간» 버튼 점 · 08 대장간 — «자동» 점 · 인벤 칸의 합성/NEW 점
            foreach (var p in D.Gear.Parts)
                foreach (var t in D.Gear.AllTypes) if (t.Part == p) { var g = S.NewGear(t.Part, t.Type, 0, 0); S.Inv.Add(g); S.Eq[p] = g.Uid; break; }
            {
                var t0 = D.Gear.AllTypes[0];
                for (int i = 0; i < 3; i++) S.Inv.Add(S.NewGear(t0.Part, t0.Type, 0, 0));
            }
            _app.ShowScreen("gear"); yield return Frames(2); yield return Scan("06_gear", dot, bad, table);
            _app.ShowScreen("forge"); yield return Frames(2); yield return Scan("08_gear_fuse", dot, bad, table);

            // 09·10 상점(무료 칸 점) · 20 던전 탭 점 · 01 이벤트 버튼 점
            _app.ShowScreen("shop"); yield return Frames(2); yield return Scan("10_shop", dot, bad, table);
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(2); yield return Scan("20_dungeon", dot, bad, table);
            _app.ShowScreen("lobby"); yield return Frames(2);

            Debug.Log("[AlertDot] 잰 점(화면\t자리\t가로×세로 px)\n" + table);
            Assert.IsEmpty(bad, "빨간 알림 점이 «원» 이 아니다(T136) — 점은 부모 칸의 % 가 아니라 UiKit.AlertDot(모서리 + px · 지름)로 세운다:\n"
                + string.Join("\n", bad.ToArray()));
            _log.AssertNoRed("빨간 점 모양");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator TheHelperMakesASquareDotWhateverTheParentShape()
        {
            yield return Boot();
            // 가로로 아주 넓은 칸 안에서도 점은 정사각이다 — 전에는 이런 칸에서 %가 타원을 만들었다
            var host = UiKit.CreateRootCanvas("AlertDotTestCanvas");
            var cell = UiKit.Rect(host.transform, "WideCell");
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 0.5f); cell.pivot = new Vector2(0.5f, 0.5f);
            cell.sizeDelta = new Vector2(600, 100);
            var go = UiKit.AlertDot(cell, "TestDot", new Vector2(1, 1), new Vector2(-38, -22), 44);
            yield return Frames(1);
            var rt = (RectTransform)go.transform;
            Assert.AreEqual(44f, rt.rect.width, 1e-3f, "지름(가로)");
            Assert.AreEqual(44f, rt.rect.height, 1e-3f, "지름(세로)");
            var img = go.GetComponentInChildren<Image>(true);
            Assert.IsNotNull(img, "점 그림");
            Assert.IsTrue(img.preserveAspect, "마지막 방어선 — rect 가 찌그러져도 그림은 원으로 남는다");
            Object.Destroy(host.gameObject);
            yield return Frames(2);
            _log.AssertNoRed("점 헬퍼");
            yield return Shutdown();
        }
    }
}
