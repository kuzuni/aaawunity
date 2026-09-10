using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T302 — 패스(19) 열 그라데이션이 <b>표 색 그대로</b> 그려지는가(주인 2026-09-09 «패스 부분 그라데이션이 전혀 레퍼런스랑 다름 · 해결하소»).
    /// <para>
    /// ⚠ <b>이 자는 «찍힌 픽셀» 을 본다</b> — 조각이 제대로 섰는가가 아니라 <b>사람 눈에 무슨 색이 보이는가</b> 가 물음이라서다.
    /// 원래 고장은 «열 바탕을 흰색으로 깔고 그 위에 <b>페이드</b> 조각을 얹은» 것이었는데, 코드만 읽으면 멀쩡해 보인다
    /// (표 값도 맞았다 · <c>GradientCard</c> 도 제대로 불렀다). 화면에서만 «표 색 ½ + 흰색 ½» 로 죽어 있었다.
    /// </para>
    /// <para>
    /// 그래서 재는 것은 <b>«표 색에 가까운가, 아니면 흰색과 반씩 섞인 색에 가까운가»</b> 두 갈래다 —
    /// 페이드 조각의 농도 곡선을 모르니 가운데 값을 못 박지 않고, <b>고장 났을 때의 색</b>을 같이 견주어 갈래를 가른다.
    /// 수는 하나도 안 베낀다(결정 555): 표(<see cref="GradientPalette"/>)에서 읽어 온다.
    /// </para>
    /// 표본 자리는 <b>열의 왼쪽 여백</b>이다 — 보상 칸이 열 가운데를 덮으므로 칸 없는 띠에서 재야 «칸 색» 을 물지 않는다
    /// (T266 1단계에서 그라데이션을 두 번 잘못 잰 자리가 바로 이것이다 · 결정 723).
    /// </summary>
    public class SeasonPassLookTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>열 이름 → (표 이름, 칸이 없는 왼쪽 여백의 x%) — 자리 수는 화면 코드의 rect 와 같은 뜻이다(열 왼쪽 끝 ~ 칸 왼쪽 끝 사이).</summary>
        static readonly (string Col, string Grad, float X)[] Cols =
        {
            ("Col:free",  "passFree",  5.0f),    // 열 1.9~31.9 · 칸 11.7~29.2
            ("Col:paid1", "passPaid1", 38.0f),   // 열 35.3~65.9 · 칸 41.7~61.1
            ("Col:paid2", "passPaid2", 70.0f),   // 열 66.7~98.2 · 칸 75.0~92.8
        };
        /// <summary>열 위쪽 표본 — 열 윗변(31.4%) 바로 아래이면서 첫 칸(35.1%)보다 위.</summary>
        const float TopY = 32.6f;

        static float Dist(Color32 a, Color b)
        {
            float dr = a.r - b.r * 255f, dg = a.g - b.g * 255f, db = a.b - b.b * 255f;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        [UnityTest]
        public IEnumerator ColumnGradientsKeepTheTableColorInsteadOfFadingToWhite()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I;
            yield return Frames(2);

            SeasonPassScreen.Open(_app);
            yield return Frames(3);
            UiKit.CompleteAllTweens();
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();

            Assert.IsTrue(PlayShot.Save(_app, "t302_pass_columns", null), "촬영이 PNG 를 만들어야 한다");
            var png = PlayShot.LastPng;
            Assert.IsNotNull(png, "PlayShot.LastPng");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(png), "PNG 디코딩");
            var px = tex.GetPixels32();
            int W = tex.width, H = tex.height;

            foreach (var c in Cols)
            {
                Assert.IsTrue(GradientPalette.Has(c.Grad), "표에 " + c.Grad + " 가 있다");
                var pair = GradientPalette.Of(c.Grad);
                // T344(주인 2026-09-10 «그라디언트가 왼쪽 오른쪽 이어야 하는데 상하로 되어 있네») — 열의 두 겹이 **눕혀져** 있는가.
                //   눕히면 표의 Top 이 **왼쪽** 색이 된다 — 아래 픽셀 표본(c.X = 열의 왼쪽 가장자리 근처)이 여전히 pair.Top 을 재는 까닭이다.
                //   «가로» 는 픽셀로 안 재고 관계로 잰다(90° · 가운데 앵커 · 가로·세로가 부모와 바뀜) — 워커 환경에서도 읽히는 단언.
                {
                    var colRt = UiKit.Find(_app.Current.Root, c.Col) as RectTransform;
                    Assert.IsNotNull(colRt, c.Col);
                    foreach (var layerName in new[] { UiKit.GradientTopName, UiKit.GradientBottomName })
                    {
                        var layer = colRt.Find(layerName) as RectTransform;
                        Assert.IsNotNull(layer, c.Col + " 의 " + layerName);
                        Assert.IsTrue(GradientSideways.IsSideways(layer), c.Col + " 의 " + layerName + " 은 가로(90° · 가로·세로 바꿈)여야 한다(T344) — z=" + layer.localEulerAngles.z + " size=" + layer.sizeDelta + " parent=" + colRt.rect.size);
                    }
                }

                int ix = Mathf.Clamp(Mathf.RoundToInt(W * c.X / 100f), 0, W - 1);
                int iy = Mathf.Clamp(Mathf.RoundToInt(H * (1f - TopY / 100f)), 0, H - 1);   // 화면 %는 위에서, 텍스처는 아래에서 센다
                var got = px[iy * W + ix];

                float toTable = Dist(got, pair.Top);
                float toWashed = Dist(got, Color.Lerp(pair.Top, Color.white, 0.5f));
                string where = c.Col + " 위쪽(" + c.X + "%, " + TopY + "%) 실제 "
                             + string.Format("#{0:X2}{1:X2}{2:X2}", got.r, got.g, got.b);

                // ⓐ 고장 났을 때의 색(표 색 ½ + 흰색 ½)보다 **표 색에 더 가깝다** — 이것이 주인이 본 그 차이다.
                Assert.Less(toTable, toWashed,
                    where + " — 표 색보다 «흰색과 반씩 섞인 색» 에 더 가깝다(열 바탕이 흰색으로 비치고 있다 · T302)");
                // ⓑ 그리고 아주 멀지는 않다. ⚠ **문턱을 좁게 잡지 않았다** — 워커는 PlayMode 를 못 돌리므로(결정 143)
                //   이 수가 실제로 어디쯤 떨어지는지 내가 못 보고 넣는다. 좁게 잡으면 **화면이 옳은데 자가 빨개진다**.
                //   갈래를 가르는 일은 ⓐ 가 이미 한다(문턱이 없다) — ⓑ 는 «영 딴 색» 만 잡으면 된다.
                //   고장 났을 때의 거리는 흰색과 반씩이라 ≈192 이므로 90 이면 그 갈래와 겹치지 않는다.
                Assert.Less(toTable, 90f, where + " — 표 색에서 너무 멀다(기대 " + ColorUtility.ToHtmlStringRGB(pair.Top) + ")");
            }
            Object.Destroy(tex);

            // ── T322: 줄 1~100 스크롤 · «💎100» 삭제 · 표가 모르는 줄은 «?»
            {
                var sp = _app.Current.Root;
                var track = UiKit.Find(sp, "Track");
                Assert.IsNotNull(track, "트랙");
                var scroll = track.GetComponent<UnityEngine.UI.ScrollRect>();
                Assert.IsNotNull(scroll, "트랙이 세로 스크롤이다(주인 «1~100까지 있어야»)");
                Assert.IsFalse(scroll.horizontal, "가로로는 안 굴린다");
                Assert.IsNull(UiKit.Find(sp, "SegBand"), "구간 노란 띠는 지웠다(주인 T322)");
                Assert.IsNull(UiKit.Find(sp, "SegBadge"), "«💎100» 배지는 지웠다(주인 T322)");

                // 열었을 때 레퍼런스와 같은 자리 — 맨 위 줄이 보인다
                int top = SeasonPassScreen.TopLevel;
                Assert.IsNotNull(UiKit.Find(sp, "Badge:" + top), "열자마자 맨 위 줄(" + top + ")이 서 있다");
                Assert.IsNotNull(UiKit.Find(sp, "Cell:free:" + top), "그 줄의 무료 칸");
                // T353(주인 2026-09-10 «패스들 다 중앙에 셀 있어야 함 · 파란색 쪽이 오른쪽에 치우쳐 있더라») — 첫 줄 세 칸의 가운데가 제 열의 가운데다.
                //   픽셀이 아니라 **월드 x** 로 잰다(칸은 줄 안 앵커 · 열은 화면 % 라 부모가 다르다) · 여유 2px = 반올림 몫.
                foreach (var (colName, cellName) in new[] { ("Col:free", "Cell:free:"), ("Col:paid1", "Cell:paid1:"), ("Col:paid2", "Cell:paid2:") })
                {
                    var colRt = UiKit.Find(sp, colName) as RectTransform; var cellRt = UiKit.Find(sp, cellName + top) as RectTransform;
                    Assert.IsNotNull(colRt, colName); Assert.IsNotNull(cellRt, cellName + top);
                    float cx = colRt.TransformPoint(colRt.rect.center).x, ex = cellRt.TransformPoint(cellRt.rect.center).x;
                    Assert.AreEqual(cx, ex, 2f, cellName + top + " 은 " + colName + " 의 가운데에 서야 한다(T353) — 열 " + cx.ToString("0.0") + " · 칸 " + ex.ToString("0.0"));
                }

                // ⚠ **미리 다 만들지 않는다** — 100줄 × 3칸을 한 번에 세우면 페이지가 멈춘다(T322 ⓐ).
                //    그러니 «맨 아래 줄» 은 지금 없어야 하고, 굴리면 생겨야 한다. 둘 다 재야 «재활용» 이 증명된다.
                int last = _app.Data != null && _app.Data.Pass != null ? _app.Data.Pass.MaxLevel : 100;
                Assert.IsNull(UiKit.Find(sp, "Badge:" + last), "맨 아래 줄(" + last + ")은 아직 안 만든다(보이는 줄만)");
                scroll.verticalNormalizedPosition = 0f;                 // 끝까지 내린다
                Canvas.ForceUpdateCanvases();
                yield return Frames(2);
                Assert.IsNotNull(UiKit.Find(sp, "Badge:" + last), "끝까지 내리면 마지막 줄 " + last + " 이 선다(주인 «1~100까지»)");
                Assert.IsNull(UiKit.Find(sp, "Badge:" + (last + 1)), last + " 을 넘는 줄은 없다");

                // 표가 모르는 줄은 «?» — 수를 지어내지 않는다(T322 ⓒ · 주인 값 미제공)
                var pass = _app.Data != null ? _app.Data.Pass : null;
                Assert.IsNotNull(pass, "패스 표(pass.json)가 실렸다");
                Assert.IsFalse(pass.Known(last), "마지막 줄은 주인이 값을 안 준 줄이다");
                var qty = UiKit.Find(UiKit.Find(sp, "Cell:free:" + last), "Qty");
                Assert.IsNotNull(qty, "그 줄 무료 칸의 수량 글자");
                Assert.AreEqual("?", qty.GetComponent<TMPro.TMP_Text>().text, "표가 모르는 줄은 «?» 로 그린다(수를 지어내지 않는다)");

                // ⚑ T322 ⛑ — «갓 시작한 세이브로 열면 레퍼런스 자리에 선다».
                //   ⓓ 가 «지금 레벨» 을 세이브로 옮기며 그 수를 지워, 주인 폰에서 이 화면이 표가 아는 줄(29~33)에서
                //   스물여덟 줄 아래에 서서 «?» 만 보이는 상태였다. 그 사실을 자가 아무도 안 재고 있었고,
                //   사진(19_pass)은 내가 찍기 직전에 세이브를 세워 둔 탓에 멀쩡해 보였다 — 사진이 게임을 가렸다.
                //   ⇒ 이제 여기서 잰다: 세이브가 아직 아무 말도 안 하면(PassLv 0) **표**(startLevel)가 답한다.
                Assert.AreEqual(0, _app.Save.PassLv, "이 자는 갓 시작한 세이브로 돈다(패스 레벨을 올린 적이 없다)");
                Assert.AreEqual(pass.StartLevel, SeasonPassScreen.CurLevel,
                    "세이브가 말이 없으면 화면은 표의 시작 레벨에 선다 — 그래야 주인이 폰에서 레퍼런스 구도를 본다");
                Assert.IsTrue(pass.Known(SeasonPassScreen.CurLevel), "그 자리는 표가 값을 아는 줄이어야 한다(아니면 «?» 만 보인다)");
                var lvT0 = UiKit.Find(sp, "LevelBadge")?.Find("LevelText")?.GetComponent<TMPro.TMP_Text>();
                Assert.IsNotNull(lvT0, "머리 배지 글자");
                Assert.AreEqual(SeasonPassScreen.CurLevel.ToString(), lvT0.text, "머리 배지도 같은 수를 말한다");
            }

            // ── T322 ⛑ — «받았다» 표시는 세이브가 말한다(전에는 사진 셋업이 이 갈래를 대신 보여 줬다)
            {
                int lv = SeasonPassScreen.CurLevel;
                var claimed0 = new System.Collections.Generic.Dictionary<int, int>(_app.Save.PassClaimed);
                SeasonPassScreen.Open(_app); yield return Frames(2);        // 앞 갈래가 트랙을 맨 아래로 굴려 뒀다 — 새로 열어 그 줄을 다시 세운다
                Assert.IsNull(UiKit.Find(UiKit.Find(_app.Current.Root, "Cell:free:" + lv), "Check"), "안 받은 칸에는 체크가 없다");
                _app.Save.PassClaimed[lv] = Pass.Bit(PassData.ColFree);
                SeasonPassScreen.Open(_app); yield return Frames(2);
                Assert.IsNotNull(UiKit.Find(UiKit.Find(_app.Current.Root, "Cell:free:" + lv), "Check"), "세이브가 «받았다» 면 그 칸에 체크가 선다");
                _app.Save.PassClaimed.Clear(); foreach (var kv in claimed0) _app.Save.PassClaimed[kv.Key] = kv.Value;
                SeasonPassScreen.Open(_app); yield return Frames(2);        // 뒤 갈래들이 읽으므로 되돌린다(T299 ⓑ)
            }

            // ── T328(주인 «그것들도 다 패턴 효과 있어야 하는데 없네») — 열마다 흐르는 무늬 한 장
            {
                var sp = _app.Current.Root;
                foreach (var c in Cols)
                {
                    var col = UiKit.Find(sp, c.Col);
                    Assert.IsNotNull(col, c.Col);
                    var pat = col.Find(UiKit.PatternName);
                    Assert.IsNotNull(pat, c.Col + " 에 무늬 한 장(T328)");
                    var raw = pat.GetComponent<UnityEngine.UI.RawImage>();
                    Assert.IsNotNull(raw, "무늬는 RawImage(uvRect 로 흐른다)");
                    Assert.AreEqual(SeasonPassScreen.PatternAlpha, raw.color.a, 1e-4f, "무늬 알파 = 레퍼런스 실측 상수");
                    // ⚠ **그라데이션 «위»** 여야 한다 — 밑에 깔면 불투명한 페이드 조각에 가려 가운데만 비친다(T328 주석).
                    var grad = col.Find(UiKit.GradientBottomName) ?? col.Find(UiKit.GradientTopName);
                    Assert.IsNotNull(grad, "그라데이션 조각");
                    Assert.Greater(pat.GetSiblingIndex(), grad.GetSiblingIndex(), c.Col + " 무늬는 그라데이션 위다");
                }
                // 흐른다 — 두 프레임 사이에 uvRect 가 움직인다(«한 장 깔아 두고 안 돌리는» 것과 갈린다)
                var p0 = UiKit.Find(sp, Cols[0].Col).Find(UiKit.PatternName).GetComponent<UnityEngine.UI.RawImage>();
                var uv0 = p0.uvRect;
                yield return Frames(3);
                Assert.AreNotEqual(uv0.x, p0.uvRect.x, "무늬가 흐른다(uvRect 가 움직인다)");
            }

            _log.AssertNoRed("패스 열 그라데이션(T302)");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(2);
        }
    }
}
