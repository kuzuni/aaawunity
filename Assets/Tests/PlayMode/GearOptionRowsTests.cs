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
    /// ⓐ 일반 장비 = 켜진 줄 0 · 잠긴 줄 7 · 첫 줄 꼬리표 «(희귀)» ⓑ 마지막 줄 꼬리표 «(신화 +12강)»
    /// ⓒ 신화 +12강 = 7줄 전부 켜짐 · 잠금 꼬리표 0 ⓓ 신화 +9강은 6줄(마지막 한 줄만 잠김) · 빨간 줄 0.
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

        /// <summary>잠긴 줄 = 꼬리표 «(단계)» 로 끝나는 줄(<see cref="GearText.LockSuffix"/> · 글자 필터 T75 를 거친 뒤 비교).</summary>
        bool IsLocked(string rowText, int i) => rowText.EndsWith(TextGlyphs.Safe(GearText.LockSuffix(_app.Data.Gear.OptTierName(i))));

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
            for (int i = 0; i < rows.Count; i++) Assert.IsTrue(IsLocked(rows[i], i), "일반 장비는 " + i + "번 줄이 잠겨 있어야 한다 — " + rows[i]);
            Assert.IsTrue(rows[0].EndsWith(TextGlyphs.Safe(" (" + D.Gear.RarName[1] + ")")), "첫 줄 꼬리표 = «(희귀)» — " + rows[0]);
            Assert.IsTrue(rows[rows.Count - 1].EndsWith(TextGlyphs.Safe(" (" + D.Gear.RarName[D.Gear.RarMyth] + " +12강)")), "마지막 줄 꼬리표 = «(신화 +12강)» — " + rows[rows.Count - 1]);
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
            for (int i = 0; i < rows.Count - 1; i++) Assert.IsFalse(IsLocked(rows[i], i), "신화 +9강에서 " + i + "번 줄은 켜져 있어야 한다 — " + rows[i]);
            Assert.IsTrue(IsLocked(rows[rows.Count - 1], rows.Count - 1), "마지막 줄만 잠긴다 — " + rows[rows.Count - 1]);
            _log.AssertNoRed("신화 +9강 세부 팝업");
            ClickNamed(_app.Overlay.Root, "Dimmed"); yield return Frames(2);

            // ⓒ 신화 +12강 = 7줄 전부
            var twelve = Give(part, rar: myth, plus: 12);
            GearUi.OpenDetail(_app, twelve, _app.Current.Refresh); yield return Frames(2);
            rows = OptionRowTexts();
            Assert.AreEqual(D.Gear.OptMaxCount, D.Gear.OptCount(myth, 12), "신화 +12강 = 7줄 전부");
            for (int i = 0; i < rows.Count; i++) Assert.IsFalse(IsLocked(rows[i], i), "신화 +12강에서 " + i + "번 줄이 잠기면 안 된다 — " + rows[i]);
            _log.AssertNoRed("신화 +12강 세부 팝업");
            ClickNamed(_app.Overlay.Root, "Dimmed"); yield return Frames(2);

            yield return Shutdown();
        }
    }
}
