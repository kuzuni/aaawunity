using System.Collections;
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
    /// T93 — 특전 카드 한 장이 데모 프리팹 <c>Play_Perk_Selection_02</c>(= <c>ListItem_StageBuff_02</c> + <c>CardFrame_04</c> + <c>ItemFrame_04</c>)와
    /// 같은 조각·비례·색인가(주인 2026-09-07 «특전 행들 디자인이 … 다르더라? 같게 해 · 색깔이 회색·노란색·빨간색 느낌이면 되는 거임»).
    /// 카드가 화면 어디에 놓이는가(자리·줄 간격)는 레퍼런스 04 표 ⑦ 가 정본이라 여기서 재지 않는다.
    /// </summary>
    public class PerkCardTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

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

        RectTransform Host()
        {
            var host = UiKit.Rect(_app.Frame, "PerkCardTestHost"); UiKit.Pct(host, 2, 20, 96, 12);
            return host;
        }
        PerkDef AnyPerk() { foreach (var p in _app.Data.Perks.Perks) return p; return null; }

        [UnityTest]
        public IEnumerator CardUsesThePrefabPiecesAndThePrefabTextColumn()
        {
            yield return Boot();
            var p = AnyPerk(); Assert.IsNotNull(p, "perks.json 에 특전이 있다");
            var card = _app.Overlay.PerkCard(Host(), p, "yellow", null);
            yield return Frames(1);

            // 조각 구성 = 프리팹 그대로: 카드 = ListItem_StageBuff_02 · 프레임 자리 = CardFrameArea · 아이콘 자리 = ItemFrameArea
            var frameArea = UiKit.Find(card, "CardFrameArea"); Assert.IsNotNull(frameArea, "CardFrameArea(프리팹 조각)");
            Assert.Greater(frameArea.childCount, 0, "카드 프레임 조각(CardFrame_04_*)이 들어 있다");
            var itemArea = UiKit.Find(card, "ItemFrameArea"); Assert.IsNotNull(itemArea, "ItemFrameArea(프리팹 조각)");
            Assert.Greater(itemArea.childCount, 0, "아이콘 프레임 조각(ItemFrame_04_*)이 들어 있다");

            // 특전 «이름» 줄은 끈다(주인 2026-09-05 «제목은 빼고 · 내용만») — 프리팹과 다른 유일한 점
            var nameRow = card.Find("Text");
            Assert.IsTrue(nameRow == null || !nameRow.gameObject.activeSelf, "특전 이름 줄은 꺼져 있다");

            // 설명 글자 칸 좌우 = 프리팹 실측(215.86 / 33.86 여백)
            var desc = UiKit.Find(card, "Text_Value"); Assert.IsNotNull(desc, "설명 글자(Text_Value)");
            var dr = (RectTransform)desc;
            Assert.AreEqual(Overlay.PerkDescLeft, dr.anchorMin.x, 1e-3f, "설명 칸 왼쪽 = 프리팹 실측");
            Assert.AreEqual(Overlay.PerkDescRight, dr.anchorMax.x, 1e-3f, "설명 칸 오른쪽 = 프리팹 실측");
            var dt = desc.GetComponent<Text>(); Assert.IsNotNull(dt, "설명 글자 컴포넌트");
            Assert.IsFalse(string.IsNullOrEmpty(dt.text), "설명 글자가 비어 있지 않다");
            _log.AssertNoRed("특전 카드 한 장");
        }

        /// <summary>
        /// T155 ⓑ — 카드의 «비율» 이 레퍼런스 04 와 같은가(주인 «특전들 레이아웃이 레퍼런스랑 다른 느낌임 비율 비례 등등»).
        /// <para>
        /// 실측 근거(`docs/ref/04_perks.jpg` 720×1560): 카드 세 장 y 577~714 · 763~901 · 950~1087px = h <b>8.78/8.85/8.78%p</b> ·
        /// 피치 <b>11.92/11.99%p</b> · 왼쪽 팔각 아이콘 <b>104×98px ≈ 정사각</b>(회차 1 에서 1.26 으로 잰 것은 탭에 가린 위쪽을 잘라 잰 오류다 · 결정 477).
        /// </para>
        /// 아이콘은 <b>코드가 크기를 잡지 않는다</b>(카드 안은 조각의 앵커·비례) — 카드 세로를 줄여도 조각이 정사각을 지키는지가 이 자의 물음이다.
        /// </summary>
        [UnityTest]
        public IEnumerator CardAndItsIconKeepTheReferenceProportions()
        {
            yield return Boot();
            var p = AnyPerk(); Assert.IsNotNull(p, "perks.json 에 특전이 있다");

            // 카드 자리·크기를 «표 그대로»(레퍼런스 실측) 세운다 — 3택 팝업이 LayoutElement 로 넣는 것과 같은 세로다
            var host = UiKit.Rect(_app.Frame, "PerkRatioHost");
            UiKit.Pct(host, Layout.OvCard1.X, Layout.OvCard1.Y, Layout.OvCard1.W, Layout.OvCard1.H);
            var card = _app.Overlay.PerkCard(host, p, "yellow", null);
            UiKit.Stretch((RectTransform)card);
            yield return Frames(1); Canvas.ForceUpdateCanvases();

            // ⓐ 표 값 자체가 레퍼런스 실측인가(값이 되돌아가면 이 줄이 잡는다)
            Assert.AreEqual(8.8f, Layout.OvCard1.H, 0.05f, "카드 세로 = 레퍼런스 실측 8.8%p(종전 11.0 은 25% 두꺼웠다 · T155 ⓑ)");
            Assert.AreEqual(11.95f, Layout.OvCardPitch, 0.05f, "카드 피치 = 레퍼런스 실측 11.95%p(종전 13.0)");
            Assert.AreEqual(Layout.OvCard3.Y + Layout.OvCard3.H, 69.2f, 0.6f, "카드3 아래끝 = 레퍼런스 69.68%p ±(종전 73.5 는 3.8%p 밖이었다)");

            // ⓑ 그 세로에서도 팔각 아이콘이 «정사각» 을 지키는가 — 레퍼런스도 사실상 정사각이다(104×98px · 결정 477)
            var itemArea = UiKit.Find(card, "ItemFrameArea"); Assert.IsNotNull(itemArea, "아이콘 자리(ItemFrameArea)");
            var ir = (RectTransform)itemArea; var rect = ir.rect;
            Assert.Greater(rect.width, 1f, "아이콘 자리 폭 > 0(배치가 끝난 뒤에 잰다)");
            float ratio = rect.width / Mathf.Max(1f, rect.height);
            Debug.Log($"[T155ⓑ] 카드 {((RectTransform)card).rect.width:0.0}x{((RectTransform)card).rect.height:0.0} · 아이콘 {rect.width:0.0}x{rect.height:0.0} = 가로:세로 {ratio:0.00}(레퍼런스 104×98px ≈ 1.06)");
            // ⚠ 회차 1 의 내 단언(«1.26»)은 **내 실측이 틀린 것**이었다 — 레퍼런스 팔각의 «위» 가 노란 탭에 가려
            // 같은 노란색이라 잘라 재는 바람에 세로가 짧게 나왔다. 탭 색이 아니라 «배경이 아닌 것» 으로 다시 재니
            // 팔각은 x 61~164 · y ≈600~698 = **104×98px ≈ 1.06**(사실상 정사각)이고 우리 것(0.99)과 같다(결정 477).
            // 그래서 이 줄은 «정사각을 지킨다» 로 바꾼다 — 조각이 바뀌어 아이콘이 납작해지면 이 자가 잡는다.
            Assert.AreEqual(1.0f, ratio, 0.15f, "팔각 아이콘은 정사각(레퍼런스 실측 104×98px = 1.06) — 카드 세로를 줄여도 조각이 비율을 지킨다");
            _log.AssertNoRed("특전 카드 비율");
        }

        [UnityTest]
        public IEnumerator GradeTabIsGrayYellowRedAndTheThreeDiffer()
        {
            yield return Boot();
            var p = AnyPerk();
            var host = Host();
            var colors = new[] { "gray", "yellow", "red" };
            var tabs = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var card = _app.Overlay.PerkCard(host, p, colors[i], null);
                yield return Frames(1);
                var tb = UiKit.Find(card, "TitleBg"); Assert.IsNotNull(tb, "등급 탭(TitleBg) — " + colors[i]);
                var img = tb.GetComponent<Image>(); Assert.IsNotNull(img, "등급 탭 그림 — " + colors[i]);
                tabs[i] = img.color;
                var want = Palette.PerkTabColor(colors[i]);
                Assert.AreEqual(want.r, img.color.r, 0.02f, colors[i] + " 탭 R"); Assert.AreEqual(want.g, img.color.g, 0.02f, colors[i] + " 탭 G"); Assert.AreEqual(want.b, img.color.b, 0.02f, colors[i] + " 탭 B");
                UiKit.Clear(host);
            }
            // 셋이 서로 다른 색이어야 «회색·노란색·빨간색» 이 구분된다
            for (int i = 0; i < tabs.Length; i++)
                for (int j = i + 1; j < tabs.Length; j++)
                {
                    float d = Mathf.Abs(tabs[i].r - tabs[j].r) + Mathf.Abs(tabs[i].g - tabs[j].g) + Mathf.Abs(tabs[i].b - tabs[j].b);
                    Assert.Greater(d, 0.15f, colors[i] + " 와 " + colors[j] + " 탭 색이 너무 비슷하다");
                }
            // 일반(회색)은 무채색이어야 한다
            var gray = Palette.PerkTabColor("gray");
            Assert.AreEqual(gray.r, gray.g, 0.08f, "일반 탭은 무채색(R≈G)"); Assert.AreEqual(gray.g, gray.b, 0.08f, "일반 탭은 무채색(G≈B)");
            _log.AssertNoRed("등급 탭 색 3종");
        }
    }
}
