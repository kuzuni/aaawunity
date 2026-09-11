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
    /// T472(주인 2026-09-12 사진 · «상자 확률 부분에 어떤 거는 저렇게 이미지 존내 크게 되더라 · 다 제대로 되게 해») —
    /// 상자 확률 팝업(36)의 <b>모든 칸 아이콘이 칸 안에, 같은 눈높이로</b> 선다.
    /// <para>
    /// 병의 뿌리는 <see cref="GearUi.FitIcon(Image, bool)"/> 의 두 갈래다: 파츠(투구·무기·갑옷)는 불투명 bbox 를 칸의 72% 로 맞추는데,
    /// GUI Pro 아이콘(부츠·반지·목걸이)은 <b>프리팹 값(인벤 188px 칸용 · ≈157px)</b>을 그대로 되돌려 확률 칸(짧은 변 ≈120px)에서 131% 로 넘쳤다.
    /// 고침 = 칸이 인벤 칸보다 작으면 그 비율로 줄인다(늘리지는 않는다).
    /// </para>
    /// <list type="bullet">
    /// <item><b>실물</b> — 신화 상자 확률을 실제로 열어 «Odds:*» 칸 전부의 불투명 bbox(FitIcon 과 같은 셈 · Tight 메시 정점)가 칸 짧은 변의
    ///   <see cref="MaxFill"/> 이하 · <see cref="MinFill"/> 이상 · 가운데(<see cref="CenterTol"/>)인가. 부위 여섯이 다 있어야 한다.
    ///   잰 수는 <c>ui-screens/t472_odds.json</c> 으로 남긴다(<see cref="PlayShot.Dirs"/> → `screens` 가지 · Debug.Log 는 CI 로그에 안 온다 · 결정 675).</item>
    /// <item><b>규칙 단위</b> — 가짜 칸 하나에 GUI 아이콘을 넣고 칸을 188 → 120 → 300 으로 바꿔 «188 이면 프리팹 그대로 · 120 이면 120/188 배 · 300 이면 그대로(안 늘린다)»,
    ///   파츠는 어느 칸에서든 bbox = 72% 를 잰다 — 인벤(06)·장착 슬롯이 안 바뀌었음을 이 줄이 보증한다.</item>
    /// </list>
    /// </summary>
    public class OddsIconFitTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        /// <summary>칸 짧은 변 대비 불투명 bbox 상한 — 파츠는 72%(<see cref="GearLook.PartIconFill"/>) · GUI Pro 는 인벤 비율(157/188 ≈ 83.5%) × 제 불투명 비율.</summary>
        const float MaxFill = 0.90f;
        /// <summary>하한 — «작아져서 안 보이는» 고침을 막는다.</summary>
        const float MinFill = 0.40f;
        /// <summary>bbox 가운데 ↔ 칸 가운데 허용 어긋남(칸 짧은 변 대비).</summary>
        const float CenterTol = 0.08f;

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);
            _log.AssertNoRed("부팅");
        }

        static Rect WorldRect(RectTransform rt)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        /// <summary>
        /// 그려진 그림의 <b>불투명 bbox</b>(세계 좌표) — <see cref="GearUi.FitIcon(Image, bool)"/> 과 같은 셈(Tight 메시 정점 · 픽셀 → rect 비율)이고,
        /// preserveAspect 여백은 «그려진 스프라이트 영역» 을 먼저 구해 뺀다. 정점이 없으면 rect 그대로.
        /// </summary>
        static Rect OpaqueWorld(Image im)
        {
            var r = WorldRect(im.rectTransform); var sp = im.sprite;
            if (sp == null) return r;
            float rw = sp.rect.width, rh = sp.rect.height;
            float dx = r.xMin, dy = r.yMin, dw = r.width, dh = r.height;
            if (im.preserveAspect && rw > 0f && rh > 0f && dw > 0f && dh > 0f)
            {
                float a = rw / rh;
                if (dw / dh > a) { float w2 = dh * a; dx += (dw - w2) * 0.5f; dw = w2; }
                else { float h2 = dw / a; dy += (dh - h2) * 0.5f; dh = h2; }
            }
            var verts = sp.vertices;
            if (verts == null || verts.Length < 3 || rw <= 0f || rh <= 0f) return new Rect(dx, dy, dw, dh);
            float ppu = sp.pixelsPerUnit > 0 ? sp.pixelsPerUnit : 100f; var piv = sp.pivot;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var v in verts) { float px = v.x * ppu + piv.x, py = v.y * ppu + piv.y; if (px < x0) x0 = px; if (py < y0) y0 = py; if (px > x1) x1 = px; if (py > y1) y1 = py; }
            return new Rect(dx + dw * (x0 / rw), dy + dh * (y0 / rh), dw * (x1 - x0) / rw, dh * (y1 - y0) / rh);
        }

        [UnityTest]
        public IEnumerator 확률_팝업의_모든_칸_아이콘이_칸_안에_같은_눈높이로_선다()
        {
            yield return Boot();
            var D = _app.Data; Assert.IsNotNull(D, "표");
            OddsPopup.Open(_app, "myth"); yield return Frames(2);
            var root = _app.Overlay.Root;

            int n = 0; var parts = new HashSet<string>(); var rows = new StringBuilder();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Odds:") || !t.gameObject.activeInHierarchy) continue;
                var frame = UiKit.Find(t, "ItemFrame_01") as RectTransform; Assert.IsNotNull(frame, t.name + " 의 프레임");
                var item = UiKit.Find(frame, "Item"); Assert.IsNotNull(item, t.name + " 의 Item");
                var im = item.GetComponent<Image>(); Assert.IsNotNull(im, t.name + " 의 Image"); Assert.IsNotNull(im.sprite, t.name + " 의 그림");
                // 칸 이름 «Odds:<등급>:<i>» — i 는 표(AllTypes)의 차례이고 그 부위가 이 칸의 부위다(OddsPopup.Section 이 그렇게 세운다).
                var seg = t.name.Split(':'); int idx = int.Parse(seg[seg.Length - 1], inv);
                string part = idx < D.Gear.AllTypes.Count ? D.Gear.AllTypes[idx].Part : "?";
                var fr = WorldRect(frame); float side = Mathf.Min(fr.width, fr.height);
                Assert.Greater(side, 1f, t.name + " 프레임이 레이아웃됐어야 한다");
                var ob = OpaqueWorld(im);
                float fill = Mathf.Max(ob.width, ob.height) / side;
                float dx = (ob.center.x - fr.center.x) / side, dy = (ob.center.y - fr.center.y) / side;
                rows.Append(rows.Length > 0 ? "," : "").Append("{\"cell\":\"").Append(t.name).Append("\",\"part\":\"").Append(part)
                    .Append("\",\"fill\":").Append(fill.ToString("0.000", inv)).Append(",\"dx\":").Append(dx.ToString("0.000", inv)).Append(",\"dy\":").Append(dy.ToString("0.000", inv))
                    .Append(",\"side\":").Append(side.ToString("0.0", inv)).Append("}");
                Assert.LessOrEqual(fill, MaxFill, t.name + "(" + part + ") 의 그림이 칸을 넘게 크다 — 불투명 bbox " + (fill * 100f).ToString("0", inv) + "% (상한 " + (MaxFill * 100f).ToString("0", inv) + "%) · 주인 사진의 그 부츠·장갑");
                Assert.GreaterOrEqual(fill, MinFill, t.name + "(" + part + ") 의 그림이 너무 작다 — " + (fill * 100f).ToString("0", inv) + "%");
                Assert.LessOrEqual(Mathf.Abs(dx), CenterTol, t.name + "(" + part + ") 가 가로로 치우쳤다 dx=" + dx.ToString("0.000", inv));
                Assert.LessOrEqual(Mathf.Abs(dy), CenterTol, t.name + "(" + part + ") 가 세로로 치우쳤다 dy=" + dy.ToString("0.000", inv));
                n++; parts.Add(part);
            }
            Assert.GreaterOrEqual(n, 15, "신화 상자 확률에는 칸이 열다섯은 있어야 이 자가 뜻이 있다(5×4 격자)");
            Assert.AreEqual(6, parts.Count, "부위 여섯이 다 있어야 «다 제대로» 를 잰 셈이다: " + string.Join(",", parts));

            string json = "{\"_meta\":{\"task\":\"T472\",\"round\":1,\"box\":\"myth\",\"maxFill\":" + MaxFill.ToString("0.00", inv) + "},\"cells\":[" + rows + "]}";
            foreach (var dir in PlayShot.Dirs())
            {
                try { System.IO.Directory.CreateDirectory(dir); System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "t472_odds.json"), json); }
                catch (System.Exception e) { Debug.LogWarning("[T472] t472_odds.json 저장 실패(" + dir + "): " + e.Message); }
            }
            _app.Overlay.Close(); yield return Frames(1);
        }

        [UnityTest]
        public IEnumerator GUI_아이콘은_인벤_칸보다_작은_칸에서만_그_비율로_줄고_파츠는_어디서나_72퍼센트다()
        {
            yield return Boot();
            var D = _app.Data;
            float cellPx = GearUi.CellSize(_app.Assets);
            Assert.Greater(cellPx, 100f, "인벤 칸 한 변(프리팹 188)");

            // 가짜 칸 — 오버레이 루트 아래 한 변 N px 의 사각(앵커 가운데 · 스케일 1).
            var host = new GameObject("T472Host", typeof(RectTransform)).GetComponent<RectTransform>();
            host.SetParent(_app.Overlay.Root, false); host.anchorMin = host.anchorMax = new Vector2(0.5f, 0.5f); host.pivot = new Vector2(0.5f, 0.5f);
            host.sizeDelta = new Vector2(cellPx, cellPx);
            var frame = (RectTransform)UiKit.Spawn("ui.itemFrame.empty", host).transform; UiKit.Stretch(frame);
            var item = UiKit.Find(frame, "Item"); Assert.IsNotNull(item, "프레임의 Item"); item.gameObject.SetActive(true);
            string boot = null, helm = null;
            foreach (var ty in D.Gear.AllTypes)
            {
                string key = GearLook.IconKey(ty.Part, D.Gear.SetOf(ty.Type), D.Gear.LookRar(D.Gear.RarRare));
                if (boot == null && !GearLook.HasLook(ty.Part) && ty.Part == "boot") boot = key;
                if (helm == null && GearLook.HasLook(ty.Part)) helm = key;
            }
            Assert.IsNotNull(boot, "부츠(GUI Pro) 아이콘 키"); Assert.IsNotNull(helm, "파츠(투구 등) 아이콘 키");

            // ── GUI 아이콘 · 188 칸 = 프리팹 그대로(인벤·대장간·상점 칸이 안 바뀐다).
            var im = UiKit.SetSprite(frame, "Item", boot, Palette.White); Assert.IsNotNull(im);
            Canvas.ForceUpdateCanvases(); GearUi.FitIcon(im, false); yield return Frames(1);
            var st = im.GetComponent<PartIconFit>(); Assert.IsNotNull(st, "FitIcon 이 프리팹 값을 기억해 둔다");
            Assert.AreEqual(st.Size.x, im.rectTransform.sizeDelta.x, 0.5f, "인벤 크기 칸에서는 프리팹 값 그대로");
            Assert.AreEqual(st.Size.y, im.rectTransform.sizeDelta.y, 0.5f, "인벤 크기 칸에서는 프리팹 값 그대로");

            // ── 120 칸(확률 팝업 크기) = 120/188 배.
            host.sizeDelta = new Vector2(120f, 120f); Canvas.ForceUpdateCanvases(); GearUi.FitIcon(im, false); yield return Frames(1);
            float k = 120f / cellPx;
            Assert.AreEqual(st.Size.x * k, im.rectTransform.sizeDelta.x, 0.5f, "작은 칸에서는 그 비율로 준다(T472)");
            Assert.AreEqual(st.Size.y * k, im.rectTransform.sizeDelta.y, 0.5f, "작은 칸에서는 그 비율로 준다(T472)");
            var ob = OpaqueWorld(im); var fr = WorldRect(frame);
            Assert.LessOrEqual(Mathf.Max(ob.width, ob.height) / Mathf.Min(fr.width, fr.height), MaxFill, "작은 칸에서 GUI 아이콘의 불투명 bbox 가 칸 안에 든다");

            // ── 300 칸 = 늘리지 않는다(프리팹 그대로).
            host.sizeDelta = new Vector2(300f, 300f); Canvas.ForceUpdateCanvases(); GearUi.FitIcon(im, false); yield return Frames(1);
            Assert.AreEqual(st.Size.x, im.rectTransform.sizeDelta.x, 0.5f, "큰 칸에서는 늘리지 않는다");

            // ── 파츠 · 120 칸에서도 bbox = 72%(GearLook.PartIconFill) — 종전 규칙이 그대로다.
            host.sizeDelta = new Vector2(120f, 120f); Canvas.ForceUpdateCanvases();
            im = UiKit.SetSprite(frame, "Item", helm, Palette.White); GearUi.FitIcon(im, true); yield return Frames(1);
            ob = OpaqueWorld(im); fr = WorldRect(frame);
            float fill = Mathf.Max(ob.width, ob.height) / Mathf.Min(fr.width, fr.height);
            Assert.AreEqual((float)GearLook.PartIconFill, fill, 0.03f, "파츠는 불투명 bbox 가 칸의 72%(T17 규칙 · 바뀌지 않았다)");

            Object.Destroy(host.gameObject); yield return Frames(1);
        }
    }
}
