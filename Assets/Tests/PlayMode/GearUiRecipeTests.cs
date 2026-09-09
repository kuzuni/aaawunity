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
    /// T290 3항 — 장비 세부 팝업(07)의 <b>비용 줄이 두 칸</b>인가(골드 · 그 부위 레시피)이고,
    /// 레시피가 모자라면 «슬롯 강화» 가 <b>안 눌리는가</b>(주인 «강화하려면 레시피도 필요하게»).
    /// <list type="bullet">
    /// <item>ⓐ 레시피 0 = 칸에 «0/2» 가 빨강 · 버튼 비활성 — <b>골드는 넉넉한데도</b> 그렇다(막은 것이 골드가 아님을 못 박는다).</item>
    /// <item>ⓑ 레시피를 채우면 = 초록 · 버튼 활성 · 눌러서 Lv+1 이고 개수가 <b>정확히 그만큼</b> 준다.</item>
    /// <item>ⓒ 빈 부위 팝업(장비 없는 슬롯)도 <b>같은 줄·같은 규칙</b>이다 — 그 화면이 강화 버튼을 따로 그리기 때문이다.</item>
    /// </list>
    /// <para>
    /// <see cref="UiSmokeTests"/> 는 «눌리면 Lv 이 오른다» 만 재고(그 줄은 레시피를 채워 두고 누른다) —
    /// «모자라면 안 눌린다» 는 그 자의 물음이 아니라 이 자의 물음이다.
    /// </para>
    /// </summary>
    public class GearUiRecipeTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }

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
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        GearItem Give(string part)
        {
            foreach (var t in _app.Data.Gear.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, 1, 0); _app.Save.Inv.Add(g); return g; }
            Assert.Fail("gear.json 에 부위가 없다: " + part); return null;
        }

        /// <summary>비용 줄의 레시피 칸 글자(«보유/필요») — 없으면 null.</summary>
        TMP_Text RecipeText()
        {
            var row = UiKit.Find(_app.Overlay.Root, "Cost"); Assert.IsNotNull(row, "비용 줄(Cost)");
            var t = UiKit.Find(row, "RecipeText");
            return t == null ? null : t.GetComponentInChildren<TMP_Text>(true);
        }

        static bool IsRed(string rich) => rich != null && rich.Contains(ColorUtility.ToHtmlStringRGB(Palette.Red));
        static bool IsGreen(string rich) => rich != null && rich.Contains(ColorUtility.ToHtmlStringRGB(Palette.Green));
        /// <summary>색 표시를 벗긴 글자 — 재는 것은 «몇/몇» 이고 색은 따로 본다.</summary>
        static string Plain(string rich) => System.Text.RegularExpressions.Regex.Replace(rich ?? "", "<[^>]*>", "");

        Button UpButton()
        {
            var t = UiKit.Find(_app.Overlay.Root, "BtnR"); Assert.IsNotNull(t, "슬롯 강화 버튼(BtnR)");
            var b = t.GetComponent<Button>(); Assert.IsNotNull(b, "슬롯 강화 버튼 컴포넌트");
            return b;
        }

        [UnityTest]
        public IEnumerator CostRowShowsRecipesAndBlocksWhenShort()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            Assert.IsNotNull(D.Recipe, "레시피 표(data.recipe)가 실려 있어야 한다");
            int need = Recipes.Need(D.Recipe, 0);
            Assert.Greater(need, 0, "지금 표는 레시피가 드는 판이다(perLevel > 0) — 안 드는 판이면 이 자가 잴 것이 없다");

            var g = Give("helm");
            S.Gold = 1e12;                      // 골드는 넉넉하다 — 막는 것이 레시피임을 못 박는다
            S.Recipes.Remove("helm");
            GearUi.OpenDetail(_app, g, _app.Current.Refresh); yield return Frames(2);

            // ⓐ 모자란 판 — 칸이 있고 빨갛고 버튼이 죽어 있다
            var rt = RecipeText(); Assert.IsNotNull(rt, "비용 줄에 레시피 칸(RecipeText)");
            Assert.AreEqual($"0/{need}", Plain(rt.text), "«보유/필요»");
            Assert.IsTrue(IsRed(rt.text), "모자라면 빨강(골드 칸과 같은 색 규칙)");
            Assert.IsFalse(UpButton().interactable, "레시피가 모자라면 «슬롯 강화» 가 안 눌린다(골드는 넉넉한데도)");
            Assert.AreEqual(0, S.SlotLv("helm"), "여기까지 Lv 은 그대로");

            // ⓑ 채운 판 — 초록 · 눌리고 · 정확히 need 만큼 준다
            Recipes.Add(S, "helm", need + 3);
            GearUi.OpenDetail(_app, g, _app.Current.Refresh); yield return Frames(2);
            rt = RecipeText(); Assert.IsNotNull(rt, "레시피 칸");
            Assert.AreEqual($"{need + 3}/{need}", Plain(rt.text), "보유가 필요보다 많아도 그대로 보여 준다");
            Assert.IsTrue(IsGreen(rt.text), "충분하면 초록");
            var up = UpButton(); Assert.IsTrue(up.interactable, "둘 다 충분하면 눌린다");
            double gold0 = S.Gold;
            up.onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(1, S.SlotLv("helm"), "슬롯 Lv 0 → 1");
            Assert.AreEqual(3, Recipes.Count(S, "helm"), "레시피가 정확히 need 만큼 준다");
            Assert.Less(S.Gold, gold0, "골드도 같이 빠진다");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator EmptySlotPopupUsesTheSameRule()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            int need = Recipes.Need(D.Recipe, 0);
            S.Gold = 1e12; S.Recipes.Remove("boot");

            // 빈 부위 팝업 — 강화 버튼만 있는 화면이라 규칙이 갈리기 쉬운 자리다(T290 이 거래를 한 곳으로 모은 까닭).
            GearUi.OpenSlot(_app, "boot", () => { }); yield return Frames(2);
            var rt = RecipeText(); Assert.IsNotNull(rt, "빈 부위 팝업에도 레시피 칸");
            Assert.IsTrue(IsRed(rt.text), "모자라면 빨강");
            Assert.IsFalse(UpButton().interactable, "모자라면 안 눌린다");

            Recipes.Add(S, "boot", need);
            GearUi.OpenSlot(_app, "boot", () => { }); yield return Frames(2);
            Assert.IsTrue(UpButton().interactable, "채우면 눌린다");
            UpButton().onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(1, S.SlotLv("boot"), "빈 부위 팝업에서도 Lv 이 오른다");
            Assert.AreEqual(0, Recipes.Count(S, "boot"), "레시피도 같이 빠진다");

            yield return Shutdown();
        }
    }
}
