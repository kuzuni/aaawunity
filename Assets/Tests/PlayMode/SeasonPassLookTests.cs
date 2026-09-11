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

            // T462 ⛑ — **갓 시작한 세이브로는 이 자가 그라데이션을 볼 수 없다.**
            //   주인 «챕터 완료한 만큼 열리게 하기» 로 `Pass.Lv = MaxChapter − 1` 이 됐고(결정 1284),
            //   `SeasonPassScreen.Dim` 은 `topPx = CurLevel * PitchPx` 부터 어둠을 깐다 ⇒ `CurLevel == 0` 이면 **열 전체**가 덮인다.
            //   그래서 런 1096~1098 이 «Col:free 위쪽 실제 #0D223B» 로 울었다 — 그 수는 `col.passFreeDim`(#0D233B) 그것이다.
            //   ⚠ 고침은 «문턱을 넓히는 것» 도 «표본을 옮기는 것» 도 아니다(자리는 한 픽셀도 안 움직였다 — `origin/screens:layout.json` 의
            //   `무료 열` = y 31.4 · h 53.2 로 이 파일 주석의 수 그대로다). **재려는 것이 보이는 판을 세우는 것**이다.
            _app.Save.MaxChapter = 2;          // 깬 챕터 1 ⇒ CurLevel 1 ⇒ 1레벨 줄(31.4~42%)은 어둠 밖 — 표본 y 32.6% 가 거기다
            yield return Frames(1);

            SeasonPassScreen.Open(_app);
            yield return Frames(3);
            UiKit.CompleteAllTweens();
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();

            // ⚑ «잰 것이 진짜인가» 를 먼저 묻는다(결정 1276·1279 가 오늘 두 번 값을 치른 자리) —
            //   이 자가 재는 것은 «열 색» 인데, 어둠이 덮인 판에서는 **어둠 색**을 재고도 «그라데이션이 죽었다» 로 운다.
            //   위 한 줄이 안 먹히는 날 그 갈래로 떨어지지 않게, 색을 재기 **전에** 여기서 그 까닭을 말하고 선다.
            Assert.GreaterOrEqual(SeasonPassScreen.CurLevel, 1,
                "1레벨 줄이 열려 있어야 그라데이션이 보인다 — CurLevel 0 이면 «아직 못 연 줄» 어둠(col.passFreeDim)이 열 전체를 덮는다(T462)");

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

                // 줄은 표가 말하는 대로 그린다 — 아는 줄은 그 수를, 모르는 줄은 «?»(수를 지어내지 않는다 · T322 ⓒ).
                //   ⚑ 이 갈래는 «주인이 값을 안 줬다» 를 **전제로** 서 있었다(«마지막 줄은 주인이 값을 안 준 줄이다»).
                //     오늘 주인이 값을 주자(T266 ⓑ · 100줄) 그 전제가 거짓이 되어 런 996 에서 빨갰다 — 화면이 옳고 자가 낡았다(T184).
                //     기댓값을 낮추지 않고 **둘 다 실제로 돌게** 바꿨다: 아는 줄은 표의 수로 재고, «?» 는 표에 구멍을 내서 잰다.
                //     («표에 모르는 줄이 있으면» 같은 조건문으로 두면 표가 꽉 찬 오늘은 그 갈래가 통째로 안 돈다 · 결정 953 ③)
                var pass = _app.Data != null ? _app.Data.Pass : null;
                Assert.IsNotNull(pass, "패스 표(pass.json)가 실렸다");
                Assert.IsTrue(pass.Known(last), "주인이 값을 줬으므로 마지막 줄도 표가 안다(T266 ⓑ)");
                var qtyT = UiKit.Find(UiKit.Find(sp, "Cell:free:" + last), "Qty")?.GetComponent<TMPro.TMP_Text>();
                Assert.IsNotNull(qtyT, "그 줄 무료 칸의 수량 글자");
                // T443 — 칸의 수는 이제 **칸 꼴**로 다시 쓰인다(«1000» → «1K» · 숫자·콤마뿐일 때만 · 주인 «오른쪽 아래에 겹쳐서»).
                //   기댓값을 표의 원문 그대로 두면 이 자는 «표에서 오는가» 가 아니라 «표의 서식 그대로인가» 를 재게 된다 —
                //   재려는 것은 앞엣것이므로 화면이 쓰는 그 함수로 표의 값을 옮겨 맞댄다(수는 여전히 자에 안 박힌다).
                Assert.AreEqual(GearUi.CellQtyShorten(pass.At(last, PassData.ColFree).Qty), qtyT.text,
                                "아는 줄은 **표가 말한 수**를 그대로 그린다(칸 꼴로 다시 쓰되 수는 표에서 온다)");

                // 그리고 표에 구멍을 내면 그 줄은 «?» 다 — 주인이 표를 줄이는 날 화면이 수를 지어내지 않는 것이 이 줄의 값이다.
                var kept = pass.Levels[last];
                pass.Levels.Remove(last);
                try
                {
                    SeasonPassScreen.Open(_app); yield return Frames(2);          // 다시 열면 줄을 다시 그린다(T322 ⛑2)
                    sp = _app.Current.Root;
                    var scroll2 = UiKit.Find(sp, "Track").GetComponent<UnityEngine.UI.ScrollRect>();
                    scroll2.verticalNormalizedPosition = 0f; Canvas.ForceUpdateCanvases(); yield return Frames(2);
                    var holeT = UiKit.Find(UiKit.Find(sp, "Cell:free:" + last), "Qty")?.GetComponent<TMPro.TMP_Text>();
                    Assert.IsNotNull(holeT, "구멍 난 줄에도 칸은 선다(줄을 통째로 빼지 않는다)");
                    Assert.AreEqual("?", holeT.text, "표가 모르는 줄은 «?» 로 그린다(수를 지어내지 않는다 · T322 ⓒ)");
                }
                finally { pass.Levels[last] = kept; }                              // ⚠ 뒤 갈래·다음 자가 같은 표를 읽는다 — 반드시 되돌린다
                SeasonPassScreen.Open(_app); yield return Frames(2); sp = _app.Current.Root;

                // ⚑ T322 ⛑ — «갓 시작한 세이브로 열면 레퍼런스 자리에 선다».
                //   ⓓ 가 «지금 레벨» 을 세이브로 옮기며 그 수를 지워, 주인 폰에서 이 화면이 표가 아는 줄(29~33)에서
                //   스물여덟 줄 아래에 서서 «?» 만 보이는 상태였다. 그 사실을 자가 아무도 안 재고 있었고,
                //   사진(19_pass)은 내가 찍기 직전에 세이브를 세워 둔 탓에 멀쩡해 보였다 — 사진이 게임을 가렸다.
                //   ⚑ T322 ⛑3 로 답이 바뀌었다 — 한때 «세이브가 말이 없으면 **표**(startLevel)가 답한다» 였고 그 수가 32 였다.
                //     주인 값이 100줄을 채우자 그 32 가 «갓 시작한 세이브에 32칸 = 다이아 5,600» 이 됐다(실측) ⇒ 표에서 걷어냈다.
                //     이제 화면이 1레벨에 서도 «?» 가 아니다 — 표가 1레벨부터 값을 알기 때문이다. 그것이 여기서 재는 것이다.
                Assert.AreEqual(0, _app.Save.PassLv, "이 자는 갓 시작한 세이브로 돈다(패스 레벨을 올린 적이 없다)");
                // T462 — 레벨 = 깬 챕터 수: 갓 시작한 세이브(MaxChapter 1)는 0 레벨 = 공짜 없음(T322 ⛑3 의 «5,600» 갈래가 이걸로 닫힌다)
                Assert.AreEqual(0, SeasonPassScreen.CurLevel, "갓 시작한 세이브는 0 레벨(챕터를 하나도 안 깼다 · T462)");
                Assert.IsTrue(pass.Known(1), "1레벨 줄은 표가 값을 아는 줄이어야 한다(아니면 «?» 만 보인다)");
                var lvT0 = UiKit.Find(sp, "LevelBadge")?.Find("LevelText")?.GetComponent<TMPro.TMP_Text>();
                Assert.IsNotNull(lvT0, "머리 배지 글자");
                Assert.AreEqual(SeasonPassScreen.CurLevel.ToString(), lvT0.text, "머리 배지도 같은 수를 말한다");
            }

            // ── T322 ⛑ — «받았다» 표시는 세이브가 말한다(전에는 사진 셋업이 이 갈래를 대신 보여 줬다)
            //   ⚑ 이 갈래가 런 962 에서 빨갰고, 그 빨강이 **화면 고장 둘**을 드러냈다(둘 다 «두 번째로 열 때» 만 난다):
            //     ⓐ 앞 갈래가 트랙을 맨 아래로 굴려 뒀는데 **다시 열어도 그 자리에 그대로 섰다** —
            //        표 ㊼ 의 계약 «열자마자 맨 위 = TopLevel» 이 **첫 열림에만** 참이었다(자도 첫 열림만 재고 있었다).
            //     ⓑ 줄은 세이브를 **태어날 때 한 번** 그리므로, 다시 그리지 않으면 세이브가 바뀌어도 옛 그림이 남는다 —
            //        값이 서서 «받기» 가 붙는 날 «눌렀는데 체크가 안 뜬다» 로 나타났을 자리다.
            //   ⇒ 고침은 화면 쪽(Refresh 가 자리를 되돌리고 줄을 다시 그린다). 이 갈래는 그 둘을 **동시에** 잰다.
            {
                int lv = SeasonPassScreen.CurLevel;
                var claimed0 = new System.Collections.Generic.Dictionary<int, int>(_app.Save.PassClaimed);
                SeasonPassScreen.Open(_app); yield return Frames(2);
                Assert.IsNotNull(UiKit.Find(_app.Current.Root, "Badge:" + SeasonPassScreen.TopLevel),
                    "다시 열면 트랙이 «지금 레벨» 자리로 돌아온다 — 앞 갈래가 맨 아래로 굴려 뒀다(ⓐ)");
                Assert.IsNull(UiKit.Find(UiKit.Find(_app.Current.Root, "Cell:free:" + lv), "Check"), "안 받은 칸에는 체크가 없다");
                _app.Save.PassClaimed[lv] = Pass.Bit(PassData.ColFree);
                SeasonPassScreen.Open(_app); yield return Frames(2);
                Assert.IsNotNull(UiKit.Find(UiKit.Find(_app.Current.Root, "Cell:free:" + lv), "Check"), "세이브가 «받았다» 면 그 칸에 체크가 선다(ⓑ)");
                _app.Save.PassClaimed.Clear(); foreach (var kv in claimed0) _app.Save.PassClaimed[kv.Key] = kv.Value;
                SeasonPassScreen.Open(_app); yield return Frames(2);        // 뒤 갈래들이 읽으므로 되돌린다(T299 ⓑ)
                Assert.IsNull(UiKit.Find(UiKit.Find(_app.Current.Root, "Cell:free:" + lv), "Check"), "되돌리면 체크도 사라진다(옛 줄이 남지 않는다)");
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
