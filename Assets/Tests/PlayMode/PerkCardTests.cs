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
