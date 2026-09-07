using System.Collections;
using System.Collections.Generic;
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
    /// T89 — 장비 세부 팝업(07)의 옵션 줄이 «한 칸 뒤로 민» 사다리를 그대로 보여 주는가
    /// (주인 2026-09-07 «일반 등급에서는 옵션 안 열리게 · 희귀에서부터 · 신화 12강에 마지막 흡혈 +8% 개방»).
    /// ⓐ 일반 장비 = 켜진 줄 0 · 잠긴 줄 7 ⓑ 잠긴 줄은 «자물쇠 = 그 줄 등급색»(T160 ⓐ) · «꼬리표 «(신화 +3강)» 없음»(T160 ⓑ) · «글자 #666666»(T177)
    /// ⓒ 신화 +12강 = 7줄 전부 켜짐 ⓓ 신화 +9강은 6줄(마지막 한 줄만 잠김) · 빨간 줄 0.
    /// ⚠ «잠김» 판정은 <b>글자 꼬리표가 아니라 자물쇠 그림</b>이다 — 주인 T160 ⓑ 로 꼬리표를 없앴다.
    /// <see cref="UiSmokeTests"/> 는 남의 lock(T87·T88) 이라 손대지 않고 여기에 따로 둔다.
    /// </summary>
    public class GearOptionRowsTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        /// <summary>n 프레임 — 장비 화면의 살아 있는 HeroView 카메라는 직접 그린다(배치 모드 · UiSmokeTests 와 같은 방식).</summary>
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

        /// <summary>테스트용 장비 — 그 부위의 첫 종류(세트 옵션 7줄이 다 있는 표 그대로).</summary>
        GearItem Give(string part, int rar, int plus)
        {
            foreach (var t in _app.Data.Gear.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, rar, plus); _app.Save.Inv.Add(g); return g; }
            Assert.Fail("gear.json 에 부위가 없다: " + part); return null;
        }

        /// <summary>세부 팝업의 옵션 줄 글자 — 줄 순서(Opt:0 …) 그대로.</summary>
        List<string> OptionRowTexts()
        {
            var opts = UiKit.Find(_app.Overlay.Root, "Options");
            Assert.IsNotNull(opts, "옵션 목록(Options)");
            var rows = new List<string>();
            for (int i = 0; i < _app.Data.Gear.OptMaxCount; i++)
            {
                var row = UiKit.Find(opts, "Opt:" + i); Assert.IsNotNull(row, "옵션 줄 Opt:" + i);
                var t = row.GetComponentInChildren<Text>(true); Assert.IsNotNull(t, "옵션 줄 글자 Opt:" + i);
                rows.Add(t.text ?? "");
            }
            return rows;
        }

        /// <summary>이름으로 버튼 누르기 — 없으면 실패시킨다(<see cref="UiSmokeTests"/> 의 ClickNamed 와 같은 방식).</summary>
        void ClickNamed(Transform root, string name)
        {
            var t = UiKit.Find(root, name); Assert.IsNotNull(t, "버튼 " + name);
            var b = t.GetComponent<Button>(); Assert.IsNotNull(b, "버튼 컴포넌트 " + name);
            b.onClick.Invoke();
        }

        /// <summary>
        /// 잠긴 줄인가 — <b>글자 꼬리표가 아니라 «자물쇠 그림»</b> 으로 판정한다(T160 ⓑ 로 꼬리표 «(신화 +3강)» 을 없앴다 · 주인 지시).
        /// 이제 «잠김» 을 말하는 것은 자물쇠 아이콘 하나이므로 자가 그것을 본다.
        /// </summary>
        bool IsLocked(int i)
        {
            var img = RowIcon(i);
            var lockSprite = _app.Assets.Sprite("ui.iconLock");
            Assert.IsNotNull(lockSprite, "카탈로그 자물쇠 그림(ui.iconLock)");
            return img.sprite == lockSprite;
        }

        /// <summary>옵션 줄의 아이콘(«ic») — 세트 아이콘이거나 자물쇠다.</summary>
        Image RowIcon(int i)
        {
            var opts = UiKit.Find(_app.Overlay.Root, "Options"); Assert.IsNotNull(opts, "옵션 목록(Options)");
            var row = UiKit.Find(opts, "Opt:" + i); Assert.IsNotNull(row, "옵션 줄 Opt:" + i);
            var ic = UiKit.Find(row, "ic"); Assert.IsNotNull(ic, "옵션 줄 아이콘 Opt:" + i + "/ic");
            var img = ic.GetComponent<Image>(); Assert.IsNotNull(img, "옵션 줄 아이콘 Image Opt:" + i);
            return img;
        }

        /// <summary>옵션 줄의 글자 컴포넌트.</summary>
        Text RowText(int i)
        {
            var opts = UiKit.Find(_app.Overlay.Root, "Options"); Assert.IsNotNull(opts, "옵션 목록(Options)");
            var row = UiKit.Find(opts, "Opt:" + i); Assert.IsNotNull(row, "옵션 줄 Opt:" + i);
            var t = row.GetComponentInChildren<Text>(true); Assert.IsNotNull(t, "옵션 줄 글자 Opt:" + i);
            return t;
        }

        /// <summary>그 줄이 «열리는 등급» 의 색 — 게임 코드와 같은 식(T160 ⓐ 는 잠겨 있어도 이 색이어야 한다).</summary>
        Color TierColor(int i)
        {
            var G = _app.Data.Gear;
            return G.OptNeedsMythPlus(i) ? Palette.Plum : Palette.ByName(Palette.RarName(G.OptTierRar(i)));
        }

        /// <summary>T160 ⓐ·ⓑ + T177 을 한 줄에서 잰다 — 잠긴 줄의 자물쇠 색 = 등급색 · 꼬리표 없음 · 글자 #666666.</summary>
        void AssertLockedRowLooksRight(int i)
        {
            var want = TierColor(i); var got = RowIcon(i).color;
            Assert.AreEqual(want.r, got.r, 0.02f, i + "번 잠긴 줄 자물쇠 R = 등급색(T160 ⓐ · 회색으로 죽이지 않는다)");
            Assert.AreEqual(want.g, got.g, 0.02f, i + "번 잠긴 줄 자물쇠 G = 등급색");
            Assert.AreEqual(want.b, got.b, 0.02f, i + "번 잠긴 줄 자물쇠 B = 등급색");
            Assert.AreEqual(1f, got.a, 0.02f, i + "번 잠긴 줄 자물쇠는 불투명(T160 ⓐ)");
            var t = RowText(i);
            // T160 ⓑ 는 «(등급)» 꼬리표를 없앴는지 본다 — 그런데 «)» 로 끝나는지로 재면 안 된다:
            // 옵션 설명 자체가 «치명타 시 50%: 도끼 1개(공격력 50%)» 처럼 괄호로 끝나는 것이 있어 그 줄이 잘못 걸린다(CI #290 실측 · 결정 428).
            // 재야 하는 것은 «GearText.LockSuffix 가 만드는 꼬리(예 « (신화)»)가 붙어 있나» 이므로 그 함수로 만든 꼬리를 그대로 대 본다.
            string suffix = GearText.LockSuffix(_app.Data.Gear.OptTierName(i));
            Assert.IsNotEmpty(suffix, i + "번 줄의 개방 단계 이름이 비어 있다(표를 못 읽었다 · 이 줄은 이 자로 잴 수 없다)");
            Assert.IsFalse(t.text.EndsWith(suffix, System.StringComparison.Ordinal),
                i + "번 잠긴 줄에 «" + suffix.Trim() + "» 꼬리표가 남아 있다(T160 ⓑ) — " + t.text);
            var want2 = Palette.OptLocked;
            Assert.AreEqual(want2.r, t.color.r, 0.01f, i + "번 잠긴 줄 글자 = #666666(T177 · 주인 지정)");
            Assert.AreEqual(want2.g, t.color.g, 0.01f, i + "번 잠긴 줄 글자 = #666666");
            Assert.AreEqual(want2.b, t.color.b, 0.01f, i + "번 잠긴 줄 글자 = #666666");
            Assert.IsNotNull(RowText(i).GetComponent<OwnerDarkTextTag>(), i + "번 잠긴 줄은 «주인 지정 어두운 글자» 표식을 단다(T177 · EnsureBright·TextColorGate 밖)");
        }

        [UnityTest]
        public IEnumerator CommonGearOpensNothingAndTheFirstRowSaysRare()
        {
            yield return Boot();
            var D = _app.Data; string part = D.Gear.Parts[0];

            // ⓐ 일반 — 켜진 줄 0 · 잠긴 줄 7
            var common = Give(part, rar: 0, plus: 0);
            Assert.AreEqual(0, D.Gear.OptCount(common.Rar, common.Plus), "일반 = 옵션 0개(주인 지시 T89)");
            _app.ShowScreen("gear"); yield return Frames(2);
            GearUi.OpenDetail(_app, common, _app.Current.Refresh); yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "세부 팝업이 열린다");
            var rows = OptionRowTexts();
            Assert.AreEqual(D.Gear.OptMaxCount, rows.Count, "옵션 줄 7");
            for (int i = 0; i < rows.Count; i++) Assert.IsTrue(IsLocked(i), "일반 장비는 " + i + "번 줄이 잠겨 있어야 한다 — " + rows[i]);
            // T160 ⓑ 로 꼬리표를 없앴으므로 «첫 줄 = (희귀)»·«마지막 줄 = (신화 +12강)» 단언은 «꼬리표가 없다» + «자물쇠가 등급색» 으로 바뀐다.
            // «몇 등급에서 열리는가» 는 이제 자물쇠 «색» 이 말한다(주인이 두 지시를 같이 준 까닭).
            for (int i = 0; i < rows.Count; i++) AssertLockedRowLooksRight(i);
            Assert.AreNotEqual(TierColor(0), TierColor(rows.Count - 1), "첫 줄(희귀)과 마지막 줄(신화 +12강)의 자물쇠 색이 서로 달라야 «등급» 이 읽힌다");
            _log.AssertNoRed("일반 장비 세부 팝업");
            ClickNamed(_app.Overlay.Root, "Dimmed"); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "배경 탭 = 닫기");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator MythPlusTwelveOpensEveryRowAndPlusNineLeavesOne()
        {
            yield return Boot();
            var D = _app.Data; string part = D.Gear.Parts[0];
            int myth = D.Gear.RarMyth;

            // ⓓ 신화 +9강 = 6줄(마지막 한 줄만 잠김)
            var nine = Give(part, rar: myth, plus: 9);
            _app.ShowScreen("gear"); yield return Frames(2);
            GearUi.OpenDetail(_app, nine, _app.Current.Refresh); yield return Frames(2);
            var rows = OptionRowTexts();
            Assert.AreEqual(D.Gear.OptMaxCount - 1, D.Gear.OptCount(myth, 9), "신화 +9강 = 6줄");
            for (int i = 0; i < rows.Count - 1; i++) Assert.IsFalse(IsLocked(i), "신화 +9강에서 " + i + "번 줄은 켜져 있어야 한다 — " + rows[i]);
            Assert.IsTrue(IsLocked(rows.Count - 1), "마지막 줄만 잠긴다 — " + rows[rows.Count - 1]);
            AssertLockedRowLooksRight(rows.Count - 1);
            Assert.IsNull(RowText(0).GetComponent<OwnerDarkTextTag>(), "켜진 줄은 «주인 지정» 표식을 안 단다(T177 · 예전 색 그대로)");
            _log.AssertNoRed("신화 +9강 세부 팝업");
            ClickNamed(_app.Overlay.Root, "Dimmed"); yield return Frames(2);

            // ⓒ 신화 +12강 = 7줄 전부
            var twelve = Give(part, rar: myth, plus: 12);
            GearUi.OpenDetail(_app, twelve, _app.Current.Refresh); yield return Frames(2);
            rows = OptionRowTexts();
            Assert.AreEqual(D.Gear.OptMaxCount, D.Gear.OptCount(myth, 12), "신화 +12강 = 7줄 전부");
            for (int i = 0; i < rows.Count; i++) Assert.IsFalse(IsLocked(i), "신화 +12강에서 " + i + "번 줄이 잠기면 안 된다 — " + rows[i]);
            _log.AssertNoRed("신화 +12강 세부 팝업");
            ClickNamed(_app.Overlay.Root, "Dimmed"); yield return Frames(2);

            yield return Shutdown();
        }
    }
}
