using System;
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
    /// T24 — 대장간에서 **장착 중인 장비도 합성 재료**(주인 2026-09-06 «대장간에 장착중인 거도 합성 가능하게» · aaaw T125 ①-c 를 주인이 뒤집음).
    /// 실제 씬(SampleScene → App)에서 ⓐ 장착분 칸이 배지(Check)만 있고 흐리지 않은가 ⓑ 눌러도 거부 토스트 없이 재료로 들어가는가
    /// ⓒ 수동 합성 뒤 산출물(같은 부위)이 그 슬롯에 장착되고 다른 부위 장착은 그대로인가 · 세이브(PlayerPrefs)에도 그렇게 적혔는가
    /// ⓓ «자동» 도 장착분을 포함해 합성하고 슬롯을 산출물로 잇는가 ⓔ 장비 화면으로 돌아가도 예외 0(슬롯·전투력·외형 갱신).
    /// 빨간 줄 0 은 <see cref="PlayLog"/>(ROUTINE §1 · LogAssert.NoUnexpectedReceived 금지). UiSmokeTests 는 손대지 않는다(T17 이 만지는 중).
    /// </summary>
    public class ForgeEquippedFuseTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        /// <summary>n 프레임 — 살아 있는 HeroView 카메라는 직접 그린다(배치 모드 · UiSmokeTests 와 같은 방식 · 장비 화면 왕복에서 외형 갱신이 실제로 렌더되게).</summary>
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

        static bool Click(Transform root, Func<string, bool> label)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(false))
                foreach (var t in b.GetComponentsInChildren<Text>(false))
                    if (label(t.text ?? "")) { b.onClick.Invoke(); return true; }
            return false;
        }
        static bool ClickNamed(Transform root, string name) { var t = UiKit.Find(root, name); var b = t != null ? t.GetComponent<Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        static int CountNamed(Transform root, string prefix) { int n = 0; foreach (var t in root.GetComponentsInChildren<Transform>(false)) if (t.name.StartsWith(prefix)) n++; return n; }
        bool HasText(Func<string, bool> pred) { foreach (var t in _app.UiCanvas.GetComponentsInChildren<Text>(false)) if (pred(t.text ?? "")) return true; return false; }

        /// <summary>테스트용 장비 — gear.json 의 부위×종류 표(AllTypes)에서 그 부위의 첫 종류(같은 부위면 항상 같은 종류 = 같은 합성 키).</summary>
        GearItem Give(string part, int rar = 0, int plus = 0)
        {
            foreach (var t in _app.Data.Gear.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, rar, plus); _app.Save.Inv.Add(g); return g; }
            Assert.Fail("gear.json 에 부위가 없다: " + part); return null;
        }

        [UnityTest]
        public IEnumerator EquippedGearIsAMaterialAndTheProductTakesItsSlot()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            string part = D.Gear.Parts[0], otherPart = D.Gear.Parts[1];
            var a = Give(part); var b = Give(part); var c = Give(part); S.Eq[part] = a.Uid;          // 재료 후보 3개 중 a 가 장착 중
            var keep = Give(otherPart, rar: 1); S.Eq[otherPart] = keep.Uid;                             // 다른 부위 장착 — 합성과 무관하게 그대로여야 한다
            Assert.IsTrue(S.IsEquipped(a)); Assert.AreEqual(GearUi.Key(a), GearUi.Key(b)); Assert.AreEqual(GearUi.Key(a), GearUi.Key(c));

            _app.ShowScreen("gear"); yield return Frames(2);
            _log.AssertNoRed("장비 화면(장착분 2개)");
            _app.ShowScreen("forge"); yield return Frames(2);
            Assert.AreEqual("forge", _app.Current.Name);
            var forge = _app.Current.Root; var content = UiKit.Find(forge, "Content"); Assert.IsNotNull(content, "대장간 인벤 Content");
            Assert.AreEqual(S.Inv.Count, CountNamed(content, "gear:"), "대장간 인벤에 장비가 전부(장착분 포함) 보여야 한다");

            // ⓐ 장착분 칸 = 장착중 표기(T39 레퍼런스 구도 «장착중» 글자 EquippedLabel · 예전엔 프리팹 Check 배지) 켜짐 · 흐림 없음(재료 가능)
            var cellA = UiKit.Find(content, "gear:" + a.Uid); Assert.IsNotNull(cellA, "장착분 칸");
            var mark = UiKit.Find(cellA, "EquippedLabel"); if (mark == null) mark = UiKit.Find(cellA, "Check");
            Assert.IsTrue(mark != null && mark.gameObject.activeSelf, "장착중 표기(«장착중» 글자 또는 Check 배지)는 유지");
            var cg = cellA.GetComponent<CanvasGroup>(); Assert.IsTrue(cg == null || cg.alpha >= 0.99f, "장착분을 흐리지 않는다(재료가 될 수 있으므로)");
            Assert.GreaterOrEqual(CountNamed(content, "FuseDot"), 3, "장착분 포함 같은 키 3개 → 빨간 점");
            _log.AssertNoRed("대장간(장착분 표시)");

            // ⓑ 장착분을 눌러 재료로 — 거부 토스트 없음
            Assert.IsTrue(ClickNamed(content, "gear:" + a.Uid), "장착분 칸 클릭"); yield return Frames(2);
            Assert.IsFalse(HasText(s => s.Contains("먼저 해제")), "예전의 «장착 중인 장비는 재료가 되지 않습니다» 토스트가 없어야 한다");
            Assert.IsTrue(HasText(s => s == "합성 (1/3)"), "장착분이 재료 1개로 들어갔다");
            foreach (var g in new[] { b, c }) { Assert.IsTrue(ClickNamed(content, "gear:" + g.Uid), "재료 칸 클릭 " + g.Uid); yield return Frames(1); }
            Assert.IsTrue(HasText(s => s == "합성 (3/3)"), "재료 3개");
            _log.AssertNoRed("재료 3개 선택(장착분 포함)");

            // ⓒ 수동 합성 → 산출물이 a 의 슬롯에 · 다른 부위는 그대로 · 세이브에도
            int before = S.Inv.Count;
            Assert.IsTrue(Click(forge, s => s == "합성 (3/3)"), "합성 버튼"); yield return Frames(2);
            Assert.AreEqual(before - 2, S.Inv.Count, "3개 → 1개"); Assert.AreEqual(1, S.Fuses);
            Assert.IsNull(S.InvById(a.Uid), "장착분 a 는 재료로 사라졌다");
            var made = S.EquippedGear(part);
            Assert.IsNotNull(made, "슬롯이 비면 안 된다 — 산출물(같은 부위)이 그 자리에 장착(승인 대기 29 기본값)");
            Assert.AreEqual(a.Rar + 1, made.Rar, "산출물 = 한 등급 위"); Assert.AreEqual(part, made.Part); Assert.AreNotEqual(a.Uid, made.Uid);
            Assert.AreEqual(keep.Uid, S.Eq[otherPart], "재료가 아닌 다른 부위 장착은 그대로");
            var stored = SaveData.FromJson(PlayerPrefs.GetString(SaveStore.Key, null), D);
            Assert.IsTrue(stored.Eq.TryGetValue(part, out var storedUid) && storedUid == made.Uid, "PlayerPrefs 세이브에도 산출물이 장착으로 적힌다");
            _log.AssertNoRed("수동 합성(장착분 재료)");

            // ⓓ 자동 — 산출물(장착 중)에 같은 키 2개를 더해 3개 → «자동» 이 장착분을 포함해 합성하고 슬롯을 잇는다
            Give(part, rar: made.Rar); Give(part, rar: made.Rar); _app.Current.Refresh(); yield return Frames(1);
            // T39 레퍼런스 구도: «자동» 글자는 고정 · 조합이 있으면 버튼 오른쪽 위 빨간 점(AutoDot) 이 켜지고 버튼이 활성(예전엔 «자동 (N) !» 글자)
            var autoDot = UiKit.Find(forge, "AutoDot"); var autoBtn = UiKit.Find(forge, "AutoBtn");
            Assert.IsTrue(autoDot != null && autoDot.gameObject.activeSelf, "합성 가능 조합이 있으면 «자동» 의 빨간 점(AutoDot)");
            Assert.IsTrue(autoBtn != null && autoBtn.GetComponent<Button>().interactable, "합성 가능 조합이 있으면 «자동» 버튼 활성");
            Assert.IsTrue(Click(forge, s => s.StartsWith("자동")), "자동 버튼"); yield return Frames(2);
            Assert.AreEqual(2, S.Fuses, "자동 합성 1회");
            var made2 = S.EquippedGear(part);
            Assert.IsNotNull(made2, "자동 합성 뒤에도 슬롯은 산출물로"); Assert.AreEqual(made.Rar + 1, made2.Rar); Assert.AreNotEqual(made.Uid, made2.Uid);
            Assert.AreEqual(keep.Uid, S.Eq[otherPart]);
            _log.AssertNoRed("자동 합성(장착분 재료)");

            // ⓔ 장비 화면으로 — 슬롯·전투력·외형(HeroView 스킨)이 새 장착으로 그려지며 예외 0
            Assert.IsTrue(ClickNamed(forge, "BackBtn"), "뒤로(◀ 아이콘 · 글자 없음 — T39 가 «← 장비» 글자 버튼을 없앰 · UiSmokeTests ③ 과 같은 이름 계약 · T48)"); yield return Frames(3);
            Assert.AreEqual("gear", _app.Current.Name);
            var gearContent = UiKit.Find(_app.Current.Root, "Content"); Assert.IsNotNull(gearContent, "장비 화면 인벤 Content");
            Assert.AreEqual(0, CountNamed(gearContent, "gear:" + made2.Uid), "장착 중인 산출물은 장비 화면 인벤 리스트에 없다(장착분 숨김)");
            _log.AssertNoRed("대장간 → 장비 화면(새 장착 반영)");
            yield return Shutdown();
        }
    }
}
