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
            // ⚠ T226 — **이 줄은 «실패» 가 아니라 «보고» 다**(sess-1917-23930 · 워커 J · 결정 625).
            //   임자(워커 G)의 관측은 그대로 두고 «막는 것» 만 뗐다: 이 탐침이 실패로 서 있는 동안
            //   유니티 잡이 빨갛고, `build-webgl` 이 `needs: [unity-test]` 라 **배포가 통째로 멈춘다**
            //   (오늘만 4시간 25분 · 그 사이 주인 폰은 04:48 빌드에 머물렀다).
            //   이 저장소는 그 순서를 이미 셋에서 정해 놓았다 — ClipStrict · PercentAudit.Strict ·
            //   BorderAudit.StrictScreens 전부 «먼저 보고만 → 값이 0 이 되면 strict» 였다(결정 493).
            //   **고침이 들어가면 아래 한 줄을 Assert 로 되돌리는 것이 그 회차의 마지막 일이다**(임자 몫).
            if (white <= before + 200)
                Debug.LogWarning("[T224②] ⛔ 테가 안 그려진다 — 흰 판 위 흰 글자인데 어두운 픽셀이 " + white +
                                 "(판만 있을 때 " + before + "). 값을 재는 단언(_OutlineWidth == 0.20)은 이 경우에도 통과하므로 그 자로는 못 잡는다. " +
                                 "고치면 이 줄을 Assert.Greater 로 되돌려라(T226 · 결정 625).");

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
    }
}
