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
    /// T369(주인 2026-09-10 폰 스샷 «라이트들 이펙트 마스크로 잘렸는데 타이틀 밑으로 라이트 보이는 경우들 … 타이틀 위로만 보이게») —
    /// 리본 제목 팝업의 빛이 <b>리본 몸통 밑단 아래로 새지 않는다</b>.
    /// <para>
    /// <b>무엇이 새고 있었나(실측)</b> — T320 은 마스크 바닥을 리본 <b>rect</b> 바닥에 놓았는데, 리본 조각은 양쪽 꼬리가 몸통보다 아래로 늘어진 그림이라
    /// 몸통은 rect 높이의 18.26%(`Title_01` 계열) 위에서 끝난다. `screens` 런 875 `ev_devil`: 리본 밑 윤곽선 y294 아래 y297~306 이 금빛(가운데 (208,168,28)).
    /// 고침 = <see cref="Overlay.RibbonBodyBottomFrac(string)"/> 만큼 마스크를 올린다(<see cref="Overlay.PlaceRibbonGlow"/> · 특전 갈래는 <see cref="Overlay.RibbonGlowLift"/>).
    /// </para>
    /// <para>
    /// <b>두 자</b> — ⓐ <b>기하(단언)</b>: 마스크 바닥(월드) = 리본 몸통 밑단(월드). ⓑ <b>사진(보고만 · 결정 625 «새 탐침은 먼저 보고만»)</b>:
    /// <see cref="PlayShot"/> 으로 빛을 <b>켠 채/끈 채</b> 두 장 찍어 «몸통 밑단 아래 40px 띠» 와 «리본 위 40px 띠» 의 채널 차 최댓값을 적는다 —
    /// 아래 띠 Δ ≈ 0 · 위 띠 Δ &gt; 0 이 기대값이고, 다음 회차가 CI 로그로 문턱을 확인한 뒤 단언으로 올린다.
    /// 켠 장은 `ui-screens/t369_*.png` 로 남겨 `screens` 가지에서 눈으로도 본다(주인 상시 지시 «스크린샷 찍어서 확인»).
    /// </para>
    /// </summary>
    public class RibbonGlowLeakTests
    {
        App _app; PlayLog _log;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

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
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        /// <summary>재는 띠의 높이(프레임 px · 지시서 3항 «리본 밑단 ~ +40px»).</summary>
        const float BandPx = 40f;

        /// <summary>월드 점 → 찍은 PNG 의 픽셀(x 오른쪽 · y 아래로 · 프레임이 RT 를 채운다는 전제 = <see cref="PlayShot.LastFrameFill"/> ≈ 1).</summary>
        static Vector2 ShotPx(RectTransform frame, Vector3 world, int w, int h)
        {
            var l = frame.InverseTransformPoint(world);
            float px = (l.x - frame.rect.xMin) / frame.rect.width * w;
            float py = (frame.rect.yMax - l.y) / frame.rect.height * h;
            return new Vector2(px, py);
        }

        /// <summary>두 PNG 의 같은 사각형(찍은 px · y 아래로)에서 채널 차의 최댓값(0~255). 되읽기에 실패하면 −1.</summary>
        static int BandDelta(byte[] a, byte[] b, Vector2 topLeft, Vector2 botRight)
        {
            if (a == null || b == null) return -1;
            var ta = new Texture2D(2, 2, TextureFormat.RGBA32, false); var tb = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ta.LoadImage(a) || !tb.LoadImage(b) || ta.width != tb.width || ta.height != tb.height) return -1;
                int w = ta.width, h = ta.height;
                var pa = ta.GetPixels32(); var pb = tb.GetPixels32();
                int x0 = Mathf.Clamp(Mathf.RoundToInt(topLeft.x), 0, w - 1), x1 = Mathf.Clamp(Mathf.RoundToInt(botRight.x), 0, w - 1);
                int y0 = Mathf.Clamp(Mathf.RoundToInt(topLeft.y), 0, h - 1), y1 = Mathf.Clamp(Mathf.RoundToInt(botRight.y), 0, h - 1);
                int max = 0;
                for (int y = y0; y <= y1; y++)
                {
                    // GetPixels32 는 아래에서 위로 담긴다 — «화면 y 아래로» 를 뒤집는다
                    int row = (h - 1 - y) * w;
                    for (int x = x0; x <= x1; x++)
                    {
                        var ca = pa[row + x]; var cb = pb[row + x];
                        int d = Mathf.Max(Mathf.Abs(ca.r - cb.r), Mathf.Max(Mathf.Abs(ca.g - cb.g), Mathf.Abs(ca.b - cb.b)));
                        if (d > max) max = d;
                    }
                }
                return max;
            }
            finally { Object.Destroy(ta); Object.Destroy(tb); }
        }

        /// <summary>
        /// 한 팝업을 잰다 — ⓐ 기하 단언 · ⓑ 사진 두 장(켬/끔)의 띠 Δ 를 로그로.
        /// <paramref name="host"/> = 빛 담개(<c>TitleGlow</c>) · <paramref name="ribbon"/> = 그 리본.
        /// </summary>
        IEnumerator Probe(string name, RectTransform host, RectTransform ribbon)
        {
            Assert.IsNotNull(host, name + ": 빛 담개(TitleGlow)");
            Assert.IsNotNull(ribbon, name + ": 리본");
            var mask = host.Find("Mask") as RectTransform;
            Assert.IsNotNull(mask, name + ": 사각 마스크(TitleGlow/Mask)");
            Canvas.ForceUpdateCanvases(); yield return Frames(1);

            float frac = Overlay.RibbonBodyBottomFrac(ribbon);
            float rh = ribbon.rect.height;
            Assert.Greater(rh, 1f, name + ": 리본 높이가 잡혀 있다");
            float k = _app.UiCanvas.transform.lossyScale.y;
            // ⓐ 기하 — 마스크 바닥 ≥ 리본 몸통 밑단(월드 · 캔버스 배율 × 1px 허용) = «리본 밑으로 새는 빛 0».
            //   리본을 따라가는 팝업(`PlaceRibbonGlow`)은 정확히 같은 선이고, 특전 갈래(주인 값)는 이미 그 위라 «≥» 로 잰다.
            //   마스크 바닥이 리본 위 변보다 아래여야 «위로는 보인다» 도 같이 선다(마스크를 통째로 리본 위로 올려 버린 고침을 가른다).
            float maskBottomW = mask.TransformPoint(new Vector3(0f, mask.rect.yMin, 0f)).y;
            float bodyBottomW = ribbon.TransformPoint(new Vector3(0f, ribbon.rect.yMin + rh * frac, 0f)).y;
            float rectTopW = ribbon.TransformPoint(new Vector3(0f, ribbon.rect.yMax, 0f)).y;
            Assert.GreaterOrEqual(maskBottomW, bodyBottomW - 1f * k, name + ": 마스크 바닥 ≥ 리본 몸통 밑단(T369 · rect 바닥이 아니라 몸통)");
            Assert.Less(maskBottomW, rectTopW, name + ": 마스크 바닥은 리본 위 변보다 아래(빛이 리본 «위로는» 보인다)");

            // ⓑ 사진 — 켠 채 한 장(ui-screens 에 남긴다) · 끈 채 한 장(바이트만).
            var frame = _app.Frame;
            Assert.IsNotNull(frame, name + ": 9:19.5 프레임");
            Assert.IsTrue(PlayShot.Save(_app, "t369_" + name), name + ": 촬영(빛 켬)");
            var on = PlayShot.LastPng; float fill = PlayShot.LastFrameFill;
            host.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases(); yield return Frames(1);
            Assert.IsTrue(PlayShot.Save(_app, "t369_" + name + "_off", null), name + ": 촬영(빛 끔)");
            var off = PlayShot.LastPng;
            host.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases(); yield return Frames(1);

            int w = PlayShot.ShotW, h = PlayShot.ShotH;
            // 띠의 가로 = 리본 가운데 60%(꼬리는 뺀다 · 꼬리는 몸통보다 아래까지 그려지는 조각 몫이다)
            float xl = ribbon.rect.xMin + ribbon.rect.width * 0.2f, xr = ribbon.rect.xMax - ribbon.rect.width * 0.2f;
            var belowTL = ShotPx(frame, ribbon.TransformPoint(new Vector3(xl, ribbon.rect.yMin + rh * frac - 1f, 0f)), w, h);
            var belowBR = ShotPx(frame, ribbon.TransformPoint(new Vector3(xr, ribbon.rect.yMin + rh * frac - 1f - BandPx, 0f)), w, h);
            var aboveTL = ShotPx(frame, ribbon.TransformPoint(new Vector3(xl, ribbon.rect.yMax + BandPx, 0f)), w, h);
            var aboveBR = ShotPx(frame, ribbon.TransformPoint(new Vector3(xr, ribbon.rect.yMax + 1f, 0f)), w, h);
            int dBelow = BandDelta(on, off, belowTL, belowBR);
            int dAbove = BandDelta(on, off, aboveTL, aboveBR);
            Debug.Log($"[T369] {name}: 몸통 밑 여백 비 {frac:0.0000} · 리본 높이 {rh:0.0} · 올림 {rh * frac:0.0}px · " +
                      $"아래띠(몸통 밑단~+{BandPx}px · 켬/끔 Δmax) {dBelow} · 위띠 Δmax {dAbove} · fill {fill:0.###} · 띠 px 아래 {belowTL}~{belowBR} 위 {aboveTL}~{aboveBR}");
            Assert.GreaterOrEqual(dBelow, 0, name + ": 찍은 PNG 두 장을 되읽는다(아래띠)");
            Assert.GreaterOrEqual(dAbove, 0, name + ": 찍은 PNG 두 장을 되읽는다(위띠)");
            // 2회차 — 위 로그는 CI 잡 로그에 안 나온다(런 897 실측 · 초록 자의 Debug.Log 0줄 · 결정 675 의 그 벽) ⇒ 파일로 내보낸다.
            WriteJson(name, frac, rh, rh * frac, dBelow, dAbove, fill, belowTL, belowBR, aboveTL, aboveBR);
            // 3회차 — 문턱을 박는다(2회차 «보고만» 의 값 = 런 907 `screens:t369_*.json`: 아래띠 Δ 1·1·5 · 위띠 Δ 180·149·199 · fill 1.000).
            //   아래 ≤ MaxBelow: 빛을 켜고 끄고의 차가 잡음 수준(PNG 인코딩·안티앨리어싱)이면 «리본 밑으로 새는 빛 0» 이다.
            //   위 ≥ MinAbove: 같은 두 장에서 리본 위 띠는 확실히 달라야 한다 — «빛을 통째로 껐다» 나 «마스크를 리본 위로 올려 버렸다» 를 가른다.
            //   두 문턱 모두 관측값과 한 자릿수 넘게 떨어져 있다(1~5 ↔ 8 · 149~199 ↔ 40) — 러너의 잡음이 아니라 «구조» 만 잰다.
            Assert.LessOrEqual(dBelow, MaxBelow, name + $": 리본 몸통 밑단 ~ +{BandPx}px 띠는 빛을 켜도 안 변한다(Δmax {dBelow} > {MaxBelow} · T369 «타이틀 위로만»)");
            Assert.GreaterOrEqual(dAbove, MinAbove, name + $": 리본 위 {BandPx}px 띠에는 빛이 있다(Δmax {dAbove} < {MinAbove} · «아예 껐다» 와 가른다)");
        }
        /// <summary>아래띠 Δmax 상한 — 관측 1·1·5(런 907)에 잡음 여유. 이 수를 올려서 초록을 만드는 것은 «새는 빛을 허용하는» 것이니 사진(`t369_*.png`)부터 본다.</summary>
        const int MaxBelow = 8;
        /// <summary>위띠 Δmax 하한 — 관측 149~199(런 907). 짙기(<see cref="Overlay.TitleGlowAlpha"/>)를 크게 낮추는 회차가 오면 이 수를 같이 잰다.</summary>
        const int MinAbove = 40;

        /// <summary>
        /// 잰 수를 <c>ui-screens/t369_&lt;이름&gt;.json</c> 으로 남긴다 — <see cref="PlayShot.Dirs"/> 라 `screens` 브랜치로 배포된다(`t242.json`·`tap.json` 과 같은 문법).
        /// <b>왜 파일인가</b>: 초록 런의 <c>Debug.Log</c> 는 워커에게 오지 않는다(잡 로그는 배포 단계 꼬리뿐 · 결정 675). 소수점은 불변 문화권. 실패해도 시험을 안 깬다.
        /// </summary>
        static void WriteJson(string name, float frac, float rh, float lift, int dBelow, int dAbove, float fill, Vector2 bTL, Vector2 bBR, Vector2 aTL, Vector2 aBR)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string json = "{\"_meta\":{\"task\":\"T369\",\"round\":3,\"popup\":\"" + name + "\"},"
                        + "\"bodyBottomFrac\":" + frac.ToString("0.0000", inv)
                        + ",\"ribbonH\":" + rh.ToString("0.0", inv)
                        + ",\"liftPx\":" + lift.ToString("0.0", inv)
                        + ",\"deltaBelow\":" + dBelow.ToString(inv)
                        + ",\"deltaAbove\":" + dAbove.ToString(inv)
                        + ",\"frameFill\":" + fill.ToString("0.000", inv)
                        + ",\"belowPx\":[" + bTL.x.ToString("0", inv) + "," + bTL.y.ToString("0", inv) + "," + bBR.x.ToString("0", inv) + "," + bBR.y.ToString("0", inv) + "]"
                        + ",\"abovePx\":[" + aTL.x.ToString("0", inv) + "," + aTL.y.ToString("0", inv) + "," + aBR.x.ToString("0", inv) + "," + aBR.y.ToString("0", inv) + "]}";
            foreach (var dir in PlayShot.Dirs())
            {
                try { System.IO.Directory.CreateDirectory(dir); System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "t369_" + name + ".json"), json); }
                catch (System.Exception e) { Debug.LogWarning("[T369] t369_" + name + ".json 저장 실패(" + dir + "): " + e.Message); }
            }
        }

        BattleState NewBattle()
        {
            var D = _app.Data;
            var rng = new Mulberry32(11u);
            return new BattleState(D, 1, _app.Save.CurBuild(D), rng, new InteractivePolicy(), new RunOptions { EmitEvents = true });
        }

        /// <summary>특전(레벨 업) — 주인 값(T320 ⓐ)의 자리 · 리본 `Title_01_NoDeco_Tangerine`(몸통 밑 여백 18.26%).</summary>
        [UnityTest]
        public IEnumerator 특전_팝업의_빛은_리본_몸통_아래로_안_샌다()
        {
            yield return Boot();
            Time.timeScale = 0f;
            var G = NewBattle();
            var offer = Perks.Offer(_app.Data, G.Taken, false, new Mulberry32(11u));
            Assert.Greater(offer.Count, 0, "특전 제안");
            G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
            _app.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick));
            yield return Frames(2);
            UiKit.CompleteAllTweens(); yield return Frames(1);
            var host = UiKit.Find(_app.Overlay.Root, "TitleGlow") as RectTransform;
            var ribbon = UiKit.Find(_app.Overlay.Root, "Title_01_NoDeco_Tangerine") as RectTransform;
            yield return Probe("perk", host, ribbon);
            _log.AssertNoRed("특전 팝업 빛");
            yield return Shutdown();
        }

        /// <summary>악마의 거래 — 주인 사진의 그 팝업(공통 `Box` · `ui.title.plum`).</summary>
        [UnityTest]
        public IEnumerator 악마의_거래_팝업의_빛은_리본_몸통_아래로_안_샌다()
        {
            yield return Boot();
            Time.timeScale = 0f;
            var G = NewBattle();
            _app.Overlay.Devil(G, _ => { });
            yield return Frames(2);
            UiKit.CompleteAllTweens(); yield return Frames(1);
            var host = UiKit.Find(_app.Overlay.Root, "TitleGlow") as RectTransform;
            Assert.IsNotNull(host, "악마의 거래: 빛 담개");
            var follow = host.GetComponent<RibbonGlowFollow>();
            Assert.IsNotNull(follow, "악마의 거래: 빛 담개가 리본을 따라간다(T320 ⓑ)");
            yield return Probe("devil", host, follow.Ribbon);
            _log.AssertNoRed("악마의 거래 빛");
            yield return Shutdown();
        }

        /// <summary>확률 팝업 — «공통 리본 팝업» 의 대표(OddsPopupTests 와 같은 자리 · 여기서는 사진까지).</summary>
        [UnityTest]
        public IEnumerator 공통_리본_팝업의_빛은_리본_몸통_아래로_안_샌다()
        {
            yield return Boot();
            var box = OddsPopup.Open(_app, "myth");
            yield return Frames(2);
            UiKit.CompleteAllTweens(); yield return Frames(1);
            Assert.IsNotNull(box, "확률 팝업 상자");
            var host = box.Find("TitleGlow") as RectTransform;
            Assert.IsNotNull(host, "확률 팝업: 빛 담개");
            var follow = host.GetComponent<RibbonGlowFollow>();
            Assert.IsNotNull(follow, "확률 팝업: 빛 담개가 리본을 따라간다");
            yield return Probe("odds", host, follow.Ribbon);
            _log.AssertNoRed("공통 리본 팝업 빛");
            yield return Shutdown();
        }
    }
}
