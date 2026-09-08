using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T207 ① — «TMP 로 갈아탈 수 있는가» 를 재는 <b>탐사 자</b>(주인 승인 2026-09-07 17:2X «tmpro로 아웃라인 해야지 진짜 메테리얼로»).
    /// <para>
    /// 지시서 3항이 ① 을 «작다 · 되돌리기 쉽다 · 여기서 죽으면 ②③ 을 안 한다» 로 떼어 놓았다. 그 판정 셋을 그대로 잰다:
    /// ⓐ <b>한글이 두부(□)가 아닌가</b> — 동적 굽기가 되는가(«글리프가 실제로 구워졌나» 로 가른다) ·
    /// ⓑ <b>아웃라인이 «머티리얼» 로 나오는가</b>(SDF 셰이더의 <c>_OutlineWidth</c>) ·
    /// ⓒ 그 글자가 <b>화면에 실제로 그려지는가</b>(찍어서 픽셀로 본다 — 컴포넌트가 서 있는 것과 그려지는 것은 다르다).
    /// </para>
    /// <b>이 자가 못 재는 것 하나</b> — <b>WebGL 배포 빌드</b>다(동적 굽기는 네이티브 FreeType 을 타므로 «에디터에서 되고 WebGL 에서 안 될» 수 있다).
    /// 그 판정은 <c>tools/webgl_smoke.sh</c> 몫이고, ② 를 시작하기 전에 <b>그것까지</b> 초록이어야 한다(지시서 3항 ①).
    /// </summary>
    public class TmpFontProbeTests
    {
        App _app; PlayLog _log;
        /// <summary>재는 글자 — 화면에 실제로 쓰는 말에서 골랐다(«레벨 업!» · «장비» · 숫자 · 영문).</summary>
        const string Sample = "레벨 업 장비 특전 0123 Lv";
        /// <summary>
        /// T246 — «회색 판 위에 남은 흰 낯»(밝은 픽셀)의 <b>바닥</b>. 이 밑이면 테가 글자를 파먹은 것이다.
        /// <para>
        /// <b>수의 출처</b>: `screens` 의 <c>tmpfont.json</c> 두 런(524 · 526)이 <b>둘 다 2153</b> 을 냈다 — 이 탐침은 글자·크기·판이 다 고정이라 결정적이다.
        /// 벽을 그 <b>절반</b>에 두어 2배 여유를 남긴다: 흔들려서 빨개질 자리가 없고, 막으려는 그림에서는 이 수가 <b>0 쪽으로 무너진다</b>
        /// (T224 회차 1 의 로비 «챕터 1» 이 밝은 픽셀 250 → <b>2</b> 였다).
        /// </para>
        /// </summary>
        const int FaceBrightFloor = 1000;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static float Luma(Color32 c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;

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

        /// <summary>찍은 PNG 의 «가로 띠»(프레임 세로 <paramref name="y0"/>~<paramref name="y1"/> 비율)에서 어두운 픽셀 수를 센다 — 글자가 그려졌는지 보는 자.</summary>
        static int DarkPixels(byte[] png, float y0, float y1, float threshold)
        {
            if (png == null) return -1;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(png)) { Object.Destroy(tex); return -1; }
            int w = tex.width, h = tex.height, n = 0;
            var px = tex.GetPixels32();
            int yTop = Mathf.Clamp(Mathf.RoundToInt(h * (1f - y1)), 0, h - 1);
            int yBot = Mathf.Clamp(Mathf.RoundToInt(h * (1f - y0)), 0, h - 1);
            for (int y = yTop; y <= yBot; y++)
                for (int x = 0; x < w; x++)
                    if (Luma(px[x + y * w]) < threshold) n++;
            Object.Destroy(tex);
            return n;
        }

        /// <summary>같은 띠에서 «밝은» 픽셀 수 — 테가 정점 색(흰색)으로 물들었는지 가르는 데 쓴다(T224 2항-d).</summary>
        static int BrightPixels(byte[] png, float y0, float y1, float threshold)
        {
            if (png == null) return -1;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(png)) { Object.Destroy(tex); return -1; }
            int w = tex.width, h = tex.height, n = 0;
            var px = tex.GetPixels32();
            int yTop = Mathf.Clamp(Mathf.RoundToInt(h * (1f - y1)), 0, h - 1);
            int yBot = Mathf.Clamp(Mathf.RoundToInt(h * (1f - y0)), 0, h - 1);
            for (int y = yTop; y <= yBot; y++)
                for (int x = 0; x < w; x++)
                    if (Luma(px[x + y * w]) > threshold) n++;
            Object.Destroy(tex);
            return n;
        }

        [UnityTest]
        public IEnumerator JuaTmpFontAssetBakesHangulAndDrawsWithAMaterialOutline()
        {
            yield return Boot();

            // ⓐ 폰트 애셋을 «코드로» 만든다 — 에디터에서 구운 .asset 이 없어도 되는지가 이 단계의 첫 물음이다
            var ttf = _app.Assets.Font(TmpFont.FontKey);
            Assert.IsNotNull(ttf, "카탈로그에 Jua 글꼴(" + TmpFont.FontKey + ")이 있어야 한다");
            var asset = TmpFont.Get(ttf);
            Assert.IsNotNull(asset, "TMP_FontAsset.CreateFontAsset 이 폰트 애셋을 만들어야 한다(못 만들면 ②③ 은 시작하지 않는다)");

            // ⓑ 한글 글리프가 실제로 구워지는가 — 두부(□)의 정체는 «글리프가 없다» 다
            bool baked = TmpFont.HasAll(asset, Sample);
            Debug.Log($"[T207①] 폰트 애셋 «{asset.name}» · 표본 «{Sample}» 굽기 {(baked ? "성공" : "실패")}");
            Assert.IsTrue(baked, "표본 글자(한글 포함)가 전부 구워져야 한다 — 하나라도 없으면 화면에서 두부(□)가 된다");

            // ⓒ 아웃라인이 «머티리얼» 로 나오는가(SDF 셰이더 프로퍼티)
            Assert.IsTrue(TmpFont.SetOutline(asset, Color.black),
                "폰트 애셋 머티리얼에 " + TmpFont.OutlineWidthProp + " 가 있어야 한다 — 그것이 주인이 말한 «진짜 머티리얼» 아웃라인이다");
            Assert.AreEqual(TmpFont.OutlineWidth, asset.material.GetFloat(TmpFont.OutlineWidthProp), 1e-4f, "두께가 그 값으로 들어간다");

            // ⓓ 화면에 실제로 그려지는가 — 밝은 판 위에 TMP 라벨을 얹고 «찍어서» 어두운 픽셀을 센다.
            //    컴포넌트가 서 있는 것과 픽셀이 나오는 것은 다르다(T207 ① 이 재려는 것은 후자다).
            var host = UiKit.Rect(_app.UiCanvas.transform, "T207:Probe");
            UiKit.Pct(host, 5f, 40f, 90f, 10f);
            var bg = host.gameObject.AddComponent<Image>(); bg.color = Color.white; bg.raycastTarget = false;
            host.SetAsLastSibling();
            Canvas.ForceUpdateCanvases(); yield return Frames(1);
            Assert.IsTrue(PlayShot.Save(_app, "t207_before", null), "촬영(글자 전)");
            int before = DarkPixels(PlayShot.LastPng, 0.40f, 0.50f, 0.35f);
            Assert.GreaterOrEqual(before, 0, "찍은 PNG 를 되읽는다(전)");

            var go = new GameObject("T207:Label", typeof(RectTransform));
            go.transform.SetParent(host, false);
            var rt = (RectTransform)go.transform; UiKit.Stretch(rt);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = asset; tmp.fontSharedMaterial = asset.material;
            tmp.text = Sample; tmp.fontSize = 48f; tmp.color = Color.black; tmp.alignment = TextAlignmentOptions.Center;
            Canvas.ForceUpdateCanvases(); yield return Frames(2);
            Assert.IsTrue(PlayShot.Save(_app, "t207_after", null), "촬영(글자 후)");
            int after = DarkPixels(PlayShot.LastPng, 0.40f, 0.50f, 0.35f);
            Assert.GreaterOrEqual(after, 0, "찍은 PNG 를 되읽는다(후)");
            Debug.Log($"[T207①] 흰 판 위 어두운 픽셀 — 글자 전 {before} → 후 {after}(글자가 그려지면 늘어난다)");
            Assert.Greater(after, before + 200, "TMP 라벨이 흰 판 위에 실제로 그려져야 한다(늘어난 어두운 픽셀 = 글자 획)");

            // ⓔ T224 2항 — **테가 «그려지나»** 를 잰다. ⓓ 는 그것을 못 잰다: 저 글자는 `Color.black` 이라
            //    늘어난 어두운 픽셀이 **획 자체**이고, 테도 검정이라 그 셈에 섞여 사라진다.
            //    그래서 같은 판에 **흰 글자**를 얹는다 — 흰 판 위 흰 글자에서 어두워지는 픽셀은 **오직 테**뿐이다.
            //    (주인 2026-09-08 04:0X «검은 아웃라인 없던데 tmpro들 · 왜 그런거지» 가 이 자리를 부른 물음이다.)
            tmp.color = Color.white;
            Canvas.ForceUpdateCanvases(); yield return Frames(2);
            Assert.IsTrue(PlayShot.Save(_app, "t224_white", null), "촬영(흰 글자)");
            int white = DarkPixels(PlayShot.LastPng, 0.40f, 0.50f, 0.35f);
            Assert.GreaterOrEqual(white, 0, "찍은 PNG 를 되읽는다(흰 글자)");
            // T224 2항-b — 이 줄이 «왜 0 인가» 를 한 런에 가른다(결정 617 · 워커 E 가 넘긴 처방).
            //  ratioA 가 0 이면 «곱해서 0» · grad 가 작으면 SDF 여백 부족(3항) · 둘 다 멀쩡하면 «그 밖» 이다.
            Debug.Log($"[T224②] 흰 판 위 «흰» 글자의 어두운 픽셀 {white}(판만 있을 때 {before}) — 이 차이가 곧 **테**다 · " +
                      $"OutlineDraws={TmpFont.OutlineDraws(asset.material)} · {TmpFont.OutlineDiag(asset)}");
            // ⓕ T224 2항-c — **워커 E 가 넘긴 «둘을 가르는» 관측**(6f0fa7da ⑤): 증상이 하나가 아닐 수 있다.
            //   ⓐ 마스크와 무관하게 테가 아예 안 그려지는 무엇 · ⓑ 화면마다 갈리는 «마스킹 사본 머티리얼»
            //   (TMP 는 RectMask2D 아래 글자를 사본으로 그리므로, 공유 쪽에 넣은 값이 그리는 쪽에 없을 수 있다).
            //   이 탐침의 글자는 마스크 밑이 **아니다** — 그러니 여기서 «그리는 머티리얼» 이 공유와 같고 값도 같은데
            //   픽셀이 0 이면 ⓑ 는 이 자리의 원인이 아니고 ⓐ 가 남는다. 로그만 찍는다(막지 않는다 · T226 · 결정 625).
            var matDraw = tmp.materialForRendering;
            bool sameMat = ReferenceEquals(matDraw, asset.material);
            Debug.Log($"[T224③] 그리는 머티리얼 ↔ 공유 머티리얼 같은가 {sameMat} · 이름 «{(matDraw != null ? matDraw.name : "-")}» · " +
                      $"그리는 쪽 {(matDraw != null ? TmpFont.OutlineDiag(matDraw) : "mat=none")}");

            // ⚑ T224 — **되돌린다: 보고(`LogWarning`)에서 다시 막는 자(`Assert`)로 올린다.**
            //   T226(워커 J · 결정 625)이 이 줄을 «보고» 로 내렸던 것은 옳았다 — 아직 답을 모르는 물음이 배포를
            //   4시간 25분 세웠기 때문이다. 그리고 그 규약이 정한 올리는 때가 **바로 이 회차**다:
            //   «ⓑ 고침이 실제로 들어간 회차에서 그 줄을 Assert 로 올린다 — 그것이 그 회차의 마지막 일이다»
            //   (지시서 «⚑ 조사 중인 탐침은 «막는 자» 로 세우지 않는다» 절 · 워커 J 가 답을 넘기며 같은 말을 남겼다).
            //   이제 이 줄은 «아직 모르는 물음» 이 아니라 **아는 계약**이다: 두께 0.70(= em 의 7%)이면 48pt 글자의
            //   테가 캡처에서 ~1.7px 이라 흰 판 위 흰 글자에서 어두운 픽셀이 **획만큼** 는다. 그 계약이 깨지는 길은
            //   «누가 두께를 다시 얇게 돌린다»(그때는 주인이 본 그 그림으로 되돌아간다) 하나뿐이고, 그것은 회귀다.
            Assert.Greater(white, before + 200,
                "흰 판 위 «흰» 글자인데 어두운 픽셀이 " + white + "뿐이다(판만 있을 때 " + before + ") — 검은 테가 픽셀로 안 나온다. " +
                "값을 재는 단언(_OutlineWidth > 0)은 이 경우에도 통과하므로 그 자로는 못 잡는다(T224 · 주인 «검은 아웃라인 없던데 tmpro들»). " +
                "두께 = " + TmpFont.OutlineWidth.ToString("0.00") + " · " + TmpFont.OutlineDiag(asset));

            // ⓖ T224 2항-d — **회색 판**에서 다시 잰다: 여기서 세 갈래가 갈린다(로그만 · 막지 않는다 · 결정 625).
            //   run 480 실측으로 앞의 것이 다 죽었다 — 그리는 머티리얼이 공유와 «같은 객체» 이고(ⓑ 아님)
            //   w=0.2 ratioA=0.9 grad=10 soft=0 dilate=0 kw=on 인데도 흰 판에서 어두운 픽셀이 10 → 10 이다.
            //   손잡이가 다 맞는데 픽셀이 0 이면 남은 갈래는 «정점 색이 테까지 물들이는가» 다 —
            //   uGUI `Outline` 은 테 색이 따로였지만 TMP 셰이더 갈래에 따라 `input.color` 가 테에도 곱해질 수 있고,
            //   그러면 «흰 글자» 로 재는 순간 검은 테가 **흰 테**가 되어 흰 판에서 사라진다(내 ⓔ 설계의 구멍이다).
            //   회색 판 + 흰 글자 + 검은 테로 찍으면 셋이 갈린다:
            //     어두운 픽셀이 는다      → 테가 검게 그려진다(그러면 흰 판에서 안 보인 것은 ⓔ 설계 탓이고 진짜 결함은 다른 화면 쪽)
            //     밝은 픽셀만 는다        → 테가 **정점 색으로 물든다**(고침 = 테 색을 정점 색과 무관하게 넣는 길)
            //     둘 다 안 는다          → 테가 정말 안 그려진다(그때 «그 밖» 이 남는다)
            bg.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            Canvas.ForceUpdateCanvases(); yield return Frames(2);
            Assert.IsTrue(PlayShot.Save(_app, "t224_grey", null), "촬영(회색 판 · 흰 글자)");
            int greyDark = DarkPixels(PlayShot.LastPng, 0.40f, 0.50f, 0.35f);
            int greyBright = BrightPixels(PlayShot.LastPng, 0.40f, 0.50f, 0.65f);
            Debug.Log($"[T224④] 회색 판 · 흰 글자 — 어두운(<0.35) {greyDark} · 밝은(>0.65) {greyBright} · " +
                      $"판만 있을 때(흰 판) 어두운 {before}. 어두운 것이 늘면 «검은 테가 그려진다» · " +
                      "밝은 것만 늘면 «테가 정점 색으로 물든다» · 둘 다 안 늘면 «정말 안 그려진다».");

            // ⓗ ⚑ T224 회차 2 — **«테가 보이나» 의 짝은 «글자가 남았나» 다.** 이 자가 못 잡은 결함이 그 자리에 있었다.
            //   회차 1 은 두께만 0.70 으로 올렸고, TMP 테는 모서리에 걸쳐 그리므로 **흰 획이 통째로 먹혔다**
            //   (`screens` run 487 에서 «챕터 1» 의 밝은 픽셀 250 → 2 · 72pt «START» 도 검은 덩어리).
            //   그런데 위 ⓔ 는 «어두운 픽셀이 늘었나» 만 물으므로 그 회차에 **초록**을 줬다 — 글자가 검어질수록 더 초록이 된다.
            //   그래서 같은 촬영에서 **남은 흰 낯**을 같이 적는다: 회색 판 위 흰 글자의 밝은 픽셀이 곧 그 낯이다.
            //   ⚑ **T246 — 계열이 찼으므로 이 줄을 «보고» 에서 «막는 자» 로 올린다**(T226 규약 ⓑ: «고침이 든 회차» 가 아니라 «수가 선 회차» 가 올리는 때다).
            //   `screens` 의 `tmpfont.json` 이 두 런을 냈고 **두 런이 완전히 같은 수**다(탐침이 결정적이다 — 글자·크기·판이 다 고정이라 흔들릴 자리가 없다):
            //     run 524 · run 526 → faceBright **2153** · whiteGlyphDark 1077 · greyDark 1539 · blackGlyphDark 3728 · plateDark 10
            //   벽은 **절반(1000)** 에 둔다 — 2배 여유라 «흔들려서» 빨개질 자리가 없고, 막으려는 그림(테가 획을 먹는다)에서는
            //   이 수가 **0 쪽으로 무너진다**(T224 회차 1 의 로비 글자가 밝은 픽셀 250 → 2 였다). 즉 «조금 줄었다» 와 «먹혔다» 사이가 넉넉히 갈린다.
            Debug.Log($"[T224⑤] 흰 낯 {greyBright}(계열 2153 · 벽 {FaceBrightFloor}) · 테 {white}(판만 {before}) · " +
                      $"두께 {TmpFont.OutlineWidth:0.00} ↔ 낯 부풀리기 {TmpFont.FaceDilate:0.00}");
            Assert.Greater(greyBright, FaceBrightFloor,
                $"회색 판 위 흰 낯이 {greyBright} 뿐이다(계열 2153 · 벽 {FaceBrightFloor}) — **테가 글자를 파먹고 있다**. " +
                $"먼저 볼 것: 두께 {TmpFont.OutlineWidth:0.00} ↔ 낯 부풀리기 {TmpFont.FaceDilate:0.00} 가 **같은 값인가**"
                + "(TMP 테는 글자 모서리에 «걸쳐» 그려서 짝이 어긋난 만큼 흰 획이 깎인다 · T224 회차 2). " + TmpFont.OutlineDiag(asset));

            // ⓗ T246 — **여기까지 잰 수를 «워커가 읽을 수 있는 자리» 에 남긴다.**
            //   위 다섯 줄은 전부 `Debug.Log` 인데 **초록 런에서는 그 줄이 워커에게 안 온다**(T246 1항 실측):
            //   잡 로그는 끝 30KB 뿐이고 그 창은 `screens` 배포 단계가 차지한다 · `failed_only` 로 오는 68만 자는 **빨간 잡에만** 있다 ·
            //   결과 XML 아티팩트는 프록시가 막는다. 그래서 이 저장소가 이미 세 번 쓴 길(tap.json · overdraw.json · t233.json)을 한 번 더 쓴다.
            WriteTmpFontJson(before, after, white, greyDark, greyBright, asset);

            // ⓕ 그 «상태» 도 같이 못 박는다 — 픽셀 판정이 먼저이고, 이것은 되돌림을 막는 자다.
            Assert.IsTrue(TmpFont.OutlineDraws(asset.material),
                "머티리얼이 «테를 그리는 상태» 여야 한다(두께 > 0 **그리고** 셰이더 갈래 " + TmpFont.OutlineKeyword + " 가 켜짐 · T224 2항)");

            // ⓔ T207 ② 준비 — <b>진짜 TMP 의 API 를 이 자리에서 적어 둔다</b>(결정 573 을 그대로 되풀이한다).
            //    ② 는 파일 45개에서 uGUI `Text` 를 TMP 타입으로 바꾸는 일이고, 그 코드는 dotnet 스텁에 맞춰 컴파일된다.
            //    스텁을 «내가 아는 대로» 적으면 로컬만 초록이고 유니티에서 깨지는데, 이번에는 그 규모가 **전 화면**이다.
            //    Bloom 에서 통한 방법을 그대로 쓴다: 진짜 빌드에게 **제 멤버 목록을 적어 달라고** 한다.
            //    ⓑⓒ 로 분류해 둔 자리(alignment·fontSize·overflow·preferredWidth …)가 실제로 무슨 타입인지
            //    이 한 줄이 답한다 — ② 는 그것을 보고 스텁을 넓힌다(추측 0).
            var sb = new System.Text.StringBuilder();
            foreach (var p in typeof(TMP_Text).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(p.Name).Append(':').Append(p.PropertyType.Name);
            }
            Debug.Log("[T207②] TMP_Text 공개 프로퍼티 — " + sb);

            // T222 — 워커 I 가 «한글이 낱말 한가운데서 끊겼나» 를 잴 자를 세우려는데 스텁에 그 표면이 없어 막혔고,
            // 추측으로 넓히지 않고 나(스텁 임자)에게 넘겼다(옳은 처신 · 결정 565·573). 같은 방법으로 답한다 —
            // **진짜 빌드에게 물어서** 세 타입의 멤버를 그대로 찍는다. 그 줄을 보고 스텁을 확정하면 추측이 0 이다.
            foreach (var ty in new[] { typeof(TMP_TextInfo), typeof(TMP_LineInfo), typeof(TMP_CharacterInfo) })
            {
                var mb = new System.Text.StringBuilder();
                foreach (var f in ty.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    mb.Append(f.Name).Append(':').Append(f.FieldType.Name).Append(' ');
                foreach (var p in ty.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    mb.Append(p.Name).Append(':').Append(p.PropertyType.Name).Append("(prop) ");
                Debug.Log("[T222] " + ty.Name + " — " + mb);
            }
            Assert.Greater(sb.Length, 0, "TMP_Text 의 프로퍼티 목록을 읽어야 한다(② 가 이 목록으로 스텁을 넓힌다)");

            Object.Destroy(go); Object.Destroy(host.gameObject); yield return Frames(1);
            _log.AssertNoRed("TMP 탐사");
        }

        /// <summary>
        /// T246 — 이 탐침이 잰 수를 <c>ui-screens/tmpfont.json</c> 으로 남긴다(<see cref="PlayShot.Dirs"/> · <c>screens</c> 브랜치로 배포된다).
        /// <para>
        /// <b>왜 파일인가</b> — 워커가 이 수를 읽을 수 있는 자리가 여기뿐이다: CI 잡 로그는 <b>끝 30KB</b> 만 오고(그 창은 `screens` 배포 단계가 차지한다),
        /// <c>failed_only</c> 로 오는 68만 자는 <b>빨간 잡에만</b> 있으며(결정 657), 결과 XML 아티팩트는 프록시가 막는다(결정 289).
        /// <c>ui-screens/</c> 는 <b>유니티 잡이 빨개도 배포된다</b>(run 506·511 실측) — 그래서 초록·빨강 어느 쪽에서도 읽힌다.
        /// </para>
        /// <b>이 수로 무엇을 하나</b> — <c>faceBright</c>(회색 판 위 흰 낯)가 이 자의 다음 단계다: T224 회차 1 이 «테를 키우다 글자를 먹인» 것을
        /// 이 탐침이 <b>초록으로 통과시킨</b> 까닭이 «어두운 픽셀만 물어서» 였다(결정 641). 그 수가 두 런 이상 쌓여 계열이 서면
        /// «절반 밑이면 빨강» 으로 올린다 — 그것이 T246 의 마지막 일이다(T226 규약 ⓑ).
        /// <b>실패해도 시험을 안 깬다</b>(경고 한 줄) — 이 자는 «재는 것» 이지 «지키는 것» 이 아니다.
        /// </summary>
        static void WriteTmpFontJson(int plateDark, int blackGlyphDark, int whiteGlyphDark, int greyDark, int greyBright, TMP_FontAsset asset)
        {
            var mat = asset != null ? asset.material : null;
            // ⚠ 두 가지가 JSON 을 조용히 깨뜨린다 — 둘 다 «읽는 쪽이 통째로 못 읽는» 사고라 여기서 막는다:
            //   ⓐ 없는 프로퍼티를 «NaN» 으로 적는 것 → 없으면 `null` 로 적는다.
            //   ⓑ 소수점이 «,» 인 지역 설정 → `0,700` 이 되어 JSON 이 깨진다. 그래서 **불변 문화권**으로 못 박는다.
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string F(string prop) => mat != null && mat.HasProperty(prop) ? mat.GetFloat(prop).ToString("0.000", inv) : "null";
            string json = "{\"_meta\":{\"task\":\"T246\",\"of\":\"T224 픽셀 탐침\"}"
                        + ",\"plateDark\":" + plateDark                      // 판만 있을 때(흰 판) — 나머지 수의 바닥
                        + ",\"blackGlyphDark\":" + blackGlyphDark            // 흰 판 + 검은 글자 = 획이 그려지는가
                        + ",\"whiteGlyphDark\":" + whiteGlyphDark            // 흰 판 + 흰 글자 = 어두워지는 것은 테뿐이다
                        + ",\"greyDark\":" + greyDark                        // 회색 판 + 흰 글자 — 검은 테
                        + ",\"faceBright\":" + greyBright                    // 〃 남은 흰 낯 ← 다음 단계가 막을 수
                        + ",\"outlineWidth\":" + TmpFont.OutlineWidth.ToString("0.000", inv)
                        + ",\"faceDilate\":" + TmpFont.FaceDilate.ToString("0.000", inv)
                        + ",\"matOutlineWidth\":" + F(TmpFont.OutlineWidthProp)
                        + ",\"matFaceDilate\":" + F(TmpFont.FaceDilateProp)
                        + ",\"scaleRatioA\":" + F(TmpFont.ScaleRatioAProp)
                        + ",\"gradientScale\":" + F(TmpFont.GradientScaleProp)
                        + ",\"keywordOn\":" + ((mat != null && mat.IsKeywordEnabled(TmpFont.OutlineKeyword)) ? "true" : "false")
                        + ",\"draws\":" + (TmpFont.OutlineDraws(mat) ? "true" : "false") + "}";
            foreach (var dir in PlayShot.Dirs())
            {
                try { System.IO.Directory.CreateDirectory(dir); System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "tmpfont.json"), json); }
                catch (System.Exception e) { Debug.LogWarning("[T246] tmpfont.json 저장 실패(" + dir + "): " + e.Message); }
            }
            Debug.Log("[T246] tmpfont.json — " + json);
        }
    }
}
