using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T19 — 주인 재지적 «맵 디자인을 DemoScene_Autumn/DeepForest/Desert/Forest 씬에 있는 거 그대로». 실제 씬에서 챕터 1~4(테마 4종)의 전투를 열어
    /// ⓐ 바닥·길이 데모 인스턴스 스케일 × <see cref="Layout.MapScale"/> 로 놓이고 길 띠가 발 줄(40%)을 품는가 ⓑ 물결 경계(Road_up)가 길 <b>위·아래 양쪽</b>에 있는가(전엔 아래만)
    /// ⓒ 소품 전부가 표 스케일 × MapScale 인가 ⓓ 한 화면(5.4u 창 · HUD 사이 띠)에 소품이 데모 밀도로 보이는가(휑하지 않음) ⓔ 테마마다 빨간 줄 0(<see cref="PlayLog"/>).
    /// </summary>
    public class MapThemeTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

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
            _log.AssertNoRed("부팅");
        }

        static List<SpriteRenderer> Under(Transform root, string child)
        {
            var t = root.Find(child); Assert.IsNotNull(t, "World/" + child);
            var list = new List<SpriteRenderer>(); t.GetComponentsInChildren(true, list); return list;
        }

        [UnityTest]
        public IEnumerator AllFourThemesMatchDemoSceneComposition()
        {
            yield return Boot();
            _app.Save.MaxChapter = 4;   // 챕터 1~4 = Autumn · DeepForest · Forest · Desert
            float footY = WorldCam.ToWorld(0, Layout.PlayerFootY / 100f).y;
            float halfW = WorldCam.LayoutW / WorldCam.PPU / 2f;                                   // 창 반폭 2.7u
            float bandTop = WorldCam.ToWorld(0, (Layout.HudBuffBar.Y) / 100f).y, bandBot = WorldCam.ToWorld(0, Layout.HudPanel.Y / 100f).y;   // HUD 사이 보이는 띠(17.5% ~ 69.5%)
            for (int ch = 1; ch <= 4; ch++)
            {
                _app.StartBattle(ch); yield return Frames(2);
                var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs.G, "전투 상태 " + ch);
                var world = bs.World; Assert.IsNotNull(world, "BattleWorld " + ch);
                var theme = world.MapTheme; Assert.AreEqual(BattleWorld.Theme.ForChapter(ch).Name, theme.Name, "챕터 → 테마 순환");
                string where = $"챕터 {ch} ({theme.Name})";
                var root = world.Root; Assert.IsNotNull(root, "World 루트");

                // ⓐ 바닥·길 = 데모 인스턴스 스케일 × MapScale · 길 띠가 발 줄을 품는다
                var ground = Under(root, "Ground");
                int fields = 0, roads = 0; Bounds roadB = new Bounds();
                foreach (var sr in ground)
                {
                    if (sr.name == "field")
                    {
                        fields++; Assert.IsNotNull(sr.sprite, where + " 바닥 그림"); Assert.AreEqual(Color.white, sr.color, where + " 바닥을 어둡게 하지 않는다(데모처럼 평면)");
                        Assert.AreEqual(MapLayouts.FieldScaleX * Layout.MapScale, sr.transform.localScale.x, 1e-3f, where + " 바닥 스케일 x = Field 인스턴스 × MapScale");
                        Assert.AreEqual(MapLayouts.FieldScaleY * Layout.MapScale, sr.transform.localScale.y, 1e-3f, where + " 바닥 스케일 y = Field 인스턴스 × MapScale");
                    }
                    if (sr.name == "road")
                    {
                        roads++; Assert.IsNotNull(sr.sprite, where + " 길 그림"); roadB = sr.bounds;
                        Assert.AreEqual(MapLayouts.RoadScaleX * Layout.MapScale, sr.transform.localScale.x, 1e-3f, where + " 길 스케일 x = Road 인스턴스 × MapScale");
                        Assert.AreEqual(MapLayouts.RoadScaleY * Layout.MapScale, sr.transform.localScale.y, 1e-3f, where + " 길 스케일 y = Road 인스턴스 × MapScale(2.46 × 0.6)");
                    }
                }
                Assert.Greater(fields, 0, where + " 바닥 타일"); Assert.Greater(roads, 0, where + " 길 타일");
                Assert.IsTrue(roadB.min.y < footY && footY < roadB.max.y, $"{where} 길 띠({roadB.min.y:0.00}~{roadB.max.y:0.00})가 발 줄({footY:0.00})을 품는다");
                float roadCenterY = WorldCam.ToWorld(0, BattleWorld.DemoY(MapLayouts.RoadCenterY)).y;
                Assert.AreEqual(roadCenterY, roadB.center.y, 0.02f, where + " 길 중심 = 데모 y(−0.402) 자리");
                // 바닥이 프레임을 위아래로 다 덮는다
                float top = WorldCam.ToWorld(0, 0).y, bottom = WorldCam.ToWorld(0, 1).y; float fMax = float.MinValue, fMin = float.MaxValue;
                foreach (var sr in ground) if (sr.name == "field") { fMax = Mathf.Max(fMax, sr.bounds.max.y); fMin = Mathf.Min(fMin, sr.bounds.min.y); }
                Assert.IsTrue(fMax >= top - 1e-3f && fMin <= bottom + 1e-3f, $"{where} 바닥이 화면 전체({bottom:0.00}~{top:0.00})를 덮는다: {fMin:0.00}~{fMax:0.00}");

                // ⓑ 물결 경계 위·아래 · ⓒ 스케일 · ⓓ 밀도
                var props = Under(root, "Props"); Assert.Greater(props.Count, 0, where + " 소품");
                var table = MapLayouts.Of(theme.Name); var sys = new HashSet<float>(); foreach (var p in table) sys.Add(Mathf.Abs(p.Sy));
                int upAbove = 0, upBelow = 0, visible = 0, visibleRoadUp = 0;
                foreach (var sr in props)
                {
                    Assert.IsNotNull(sr.sprite, where + " 소품 그림 없음: " + sr.name);
                    float sy = Mathf.Abs(sr.transform.localScale.y) / Layout.MapScale; bool known = false;
                    foreach (var s in sys) if (Mathf.Abs(s - sy) < 1e-3f) { known = true; break; }
                    Assert.IsTrue(known, $"{where} 소품 세로 스케일 {sr.transform.localScale.y} 가 표 × MapScale 이 아니다 ({sr.sprite.name})");
                    bool onScreen = Mathf.Abs(sr.transform.position.x) < halfW && sr.transform.position.y < bandTop && sr.transform.position.y > bandBot;
                    if (sr.sprite.name.StartsWith("Road_up"))
                    {
                        if (sr.transform.position.y > roadB.center.y) upAbove++; else upBelow++;
                        if (onScreen) visibleRoadUp++;
                        Assert.AreEqual(-16, sr.sortingOrder, where + " 물결 경계는 길 바로 위(납작 · 데모 렌더 순서)");
                    }
                    else if (onScreen) visible++;
                }
                Assert.Greater(upAbove, 0, where + " 물결 경계가 길 위쪽에도 있어야 한다(데모 Road_Up 그룹 y +1.17)");
                Assert.Greater(upBelow, 0, where + " 물결 경계가 길 아래쪽에 있어야 한다(데모 Road_Down 그룹 y −1.97)");
                Assert.GreaterOrEqual(visibleRoadUp, 2, where + " 시작 화면 창(5.4u)에 물결 경계가 위·아래로 보인다");
                Assert.GreaterOrEqual(visible, 6, $"{where} 시작 화면 창(5.4u × HUD 사이 띠)에 소품이 {visible}개 — 데모 밀도(17.8u 창에 30~60개 = 5.4u 에 ≥ 6)여야 한다(«휑하다» 재발)");
                _log.AssertNoRed(where);
                _app.Overlay.Close(); _app.ShowScreen("lobby"); yield return Frames(2);
                _log.AssertNoRed(where + " → 로비");
            }
        }
    }
}
