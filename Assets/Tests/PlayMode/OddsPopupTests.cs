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
    /// T267 2단계 — 상자 «확률 정보» 팝업(주인 2026-09-09 «상점에 상자 부분에 인포 버튼 클릭 시 이런 게 떠야 함» · 레퍼런스 36·37).
    /// 규칙(구간·개별 확률)은 EditMode <c>GachaOddsTests</c> 가 본다 — 여기서 재는 것은 <b>화면이 그 수를 그대로 그리는가</b> 하나다.
    /// <para>
    /// ⚠ 이 자의 요점: **칸 수 = 뽑기가 고르는 목록 수** · **칸에 적힌 수 = `GachaOdds` 가 낸 개별 확률**.
    /// 화면이 제 나름대로 칸을 고르거나 수를 반올림하면 «적힌 확률이 거짓말» 이 되는데 그것은 빨간 줄도 자도 안 난다(결정 746 ②).
    /// </para>
    /// </summary>
    public class OddsPopupTests
    {
        PlayLog _log; App _app;
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

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { var r = Find(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }
        static string TextOf(Transform root, string name)
        {
            var t = Find(root, name); var x = t != null ? t.GetComponentInChildren<TMP_Text>(true) : null;
            return x != null ? x.text : null;
        }

        [UnityTest]
        public IEnumerator PopupDrawsTheGradeSectionsAndTheNumbersComeFromTheData()
        {
            yield return Boot();
            var D = _app.Data;

            // 신화 상자 — 네 등급이 다 rate > 0 이라 구간이 제일 많다(희귀 상자는 둘뿐이라 «구간 0» 을 못 잡는다)
            var box = OddsPopup.Open(_app, "myth");
            yield return Frames(2);
            Assert.IsNotNull(box, "확률 팝업 상자");
            Assert.IsTrue(_app.Overlay.IsOpen, "팝업이 열려 있다");
            Assert.IsNotNull(Find(box, "OddsScroll"), "스크롤 창");
            Assert.IsNotNull(Find(box, "OddsFoot"), "바닥 안내 띠");

            // T288 — 두 이름 계약이 **같이** 선다(한쪽을 세우려고 다른 쪽을 덮었던 자리다).
            //   ⓐ 루트는 공통 팝업 프리팹 이름 그대로다 — UiSmokeTests 의 «정보 팝업 = 공통 팝업 문법» 이 이것을 잰다.
            //   ⓑ 표식 OddsBox 는 루트가 아니라 «꺼진 자식» 이라, 있어도 그리거나 막지 않는다.
            // 여기서 같이 재는 까닭: ⓐ 만 재는 자는 다른 파일에 있어서, 이 파일을 고치는 사람 눈에 안 들어온다.
            Assert.AreEqual(UiKit.PopupKeyPlain, box.name, "팝업 루트 이름 = 공통 팝업 프리팹 키(덮지 않는다)");
            var mark = Find(box, "OddsBox");
            Assert.IsNotNull(mark, "«확률 팝업이다» 표식");
            Assert.IsFalse(mark.gameObject.activeSelf, "표식은 꺼져 있다(그리지도 막지도 않는다)");

            var rows = GachaOdds.Of(D, "myth");
            int n = GachaOdds.ItemCount(D);
            Assert.Greater(rows.Count, 1, "신화 상자는 구간이 여럿이다");
            Assert.Greater(n, 0, "아이템 목록");

            foreach (var r in rows)
            {
                var sec = Find(box, "Sec:" + r.Rar);
                Assert.IsNotNull(sec, "등급 " + r.Rar + " 구간");
                Assert.AreEqual(r.Name, TextOf(sec, "SecName"), "구간 이름은 표(gear.json rarName)에서 온다");
                StringAssert.Contains(OddsPopup.Pct(r.Percent), TextOf(sec, "SecRate"), "구간 확률 글자 = gacha.json rate");

                // 칸은 «뽑기가 고르는 목록» 그대로다 — 수도 개수도 화면이 따로 안 고른다
                for (int i = 0; i < n; i++)
                {
                    var cell = Find(box, "Odds:" + r.Rar + ":" + i);
                    Assert.IsNotNull(cell, "등급 " + r.Rar + " 칸 " + i);
                    Assert.AreEqual(OddsPopup.Pct(r.Each), TextOf(cell, "Pct"), "칸에 적힌 확률 = 등급 확률 ÷ 목록 수");
                }
                Assert.IsNull(Find(box, "Odds:" + r.Rar + ":" + n), "목록보다 많은 칸을 그리지 않는다");
            }

            // 없는 등급은 구간이 없다 — «전설 0.00%» 를 그리면 «나올 수 있는데 드물다» 로 읽힌다
            var rare = GachaOdds.Of(D, "rare");
            _app.Overlay.Close(); yield return Frames(2);
            var box2 = OddsPopup.Open(_app, "rare"); yield return Frames(2);
            Assert.IsNotNull(box2, "희귀 상자 확률 팝업");
            for (int rr = 0; rr < D.Gear.RarName.Length; rr++)
            {
                bool has = rare.Exists(x => x.Rar == rr);
                Assert.AreEqual(has, Find(box2, "Sec:" + rr) != null, "희귀 상자 등급 " + rr + " 구간은 rate > 0 일 때만 선다");
            }

            // 4항 — 칸을 누르면 «보기 전용» 세부 팝업이 뜨고 **아래 두 버튼이 없다**(주인 «아래 두 버튼만 없애고»).
            //   닫으면 이 확률 팝업으로 돌아온다(Overlay 가 한 겹이라 «겹쳐 뜨기» 를 «갔다 돌아오기» 로 낸다).
            {
                var cell = Find(box2, "Odds:" + rare[0].Rar + ":0");
                Assert.IsNotNull(cell, "누를 칸");
                var btn = cell.GetComponent<Button>(); Assert.IsNotNull(btn, "칸이 눌린다(4항)");
                btn.onClick.Invoke(); yield return Frames(2);
                var info = _app.Overlay.Root;
                Assert.IsNull(Find(info, "OddsBox"), "확률 목록이 아니라 세부 팝업이 떠 있다");
                Assert.IsNotNull(Find(info, "Name"), "세부 팝업의 이름줄");
                Assert.IsNull(Find(info, "BtnL"), "장착/해제 버튼이 없다(보기 전용)");
                Assert.IsNull(Find(info, "BtnR"), "슬롯 강화 버튼이 없다(보기 전용)");
                // 아무것도 안 바꾼다 — 세이브도 지갑도
                double g0 = _app.Save.Gold, m0 = _app.Save.Gem;
                var back = Find(info, "Dimmed")?.GetComponent<Button>(); Assert.IsNotNull(back, "세부 팝업 배경 탭");
                back.onClick.Invoke(); yield return Frames(2);
                Assert.IsNotNull(Find(_app.Overlay.Root, "OddsBox"), "닫으면 확률 팝업으로 돌아온다");
                Assert.AreEqual(g0, _app.Save.Gold, 1e-9, "보기 전용 팝업은 지갑을 안 만진다");
                Assert.AreEqual(m0, _app.Save.Gem, 1e-9, "보기 전용 팝업은 지갑을 안 만진다");
            }

            // «탭하여 닫기» — 공통 정보 팝업 문법
            var dim = Find(_app.Overlay.Root, "Dimmed")?.GetComponent<Button>();
            Assert.IsNotNull(dim, "배경 탭으로 닫힌다");
            dim.onClick.Invoke(); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "탭하면 닫힌다");

            _log.AssertNoRed("확률 팝업(T267)");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(2);
        }

        /// <summary>
        /// T267 6단계 — «보기 전용» 세부 팝업은 <b>아래(비용 줄·버튼)만 잘라 낸 것</b>이다(표 ㊿ · 주인 «아래 두 버튼만 없애고»).
        /// <para>
        /// ⚠ 이 자가 재는 것: <b>스탯 박스·옵션 목록이 장비 세부 팝업과 같은 자리·같은 높이인가.</b>
        /// 상자만 짧게(46.5 → 38.5%) 잘라 놓고 안쪽 자리를 <b>긴 상자 기준</b>으로 계산하면 안쪽이 0.83 배로 눌리는데,
        /// 팝업은 멀쩡히 뜨고 빨간 줄도 안 난다 — 옵션 줄이 53px → 44px 이 되어 <b>본문 40 이 조용히 잘릴 뿐</b>이다(결정 809).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ViewOnlyPopupCutsOnlyTheBottom()
        {
            yield return Boot();
            var D = _app.Data;
            var t0 = D.Gear.AllTypes[0];
            var g = new GearItem { Part = t0.Part, Type = t0.Type, Rar = 0, Plus = 0 };

            // ⚠ **자리를 재기 전에 등장 연출을 끝낸다** — 공통 팝업은 `UiKit.PopIn`(스케일 0.82 → 1 · 0.28초 · OutBack)으로 뜬다.
            //   두 팝업은 **상자 높이가 다르므로**(46.5 vs 38.5%) 옵션 목록이 상자 한가운데서 떨어진 거리도 다르고,
            //   그래서 «연출 중간» 에 재면 같은 자리인데도 두 값이 어긋난다(런 645 실측 2px · 결정 826).
            //   `UiShotsTests.Shot` 이 PNG 를 찍기 전에 부르는 그 줄과 같은 까닭이다(T49).
            GearUi.OpenInfo(_app, g); yield return Frames(2);
            UiKit.CompleteAllTweens(); yield return Frames(1);
            var oi = Find(_app.Overlay.Root, "Options"); Assert.IsNotNull(oi, "보기 전용 팝업의 옵션 목록");
            var si = Find(_app.Overlay.Root, "Stats"); Assert.IsNotNull(si, "보기 전용 팝업의 스탯 박스");
            Assert.AreEqual(1f, oi.lossyScale.y, 1e-3f, "연출이 끝난 뒤에 잰다(보기 전용) — 스케일이 1 이 아니면 아래 자리 값은 연출 중간이다");
            Assert.IsNull(Find(_app.Overlay.Root, "Cost"), "비용 줄은 잘려 나간 쪽이다");
            float optH = ((RectTransform)oi).rect.height, stH = ((RectTransform)si).rect.height;
            float optY = oi.position.y, stY = si.position.y;
            int optRows = oi.childCount;
            float rowH = optRows > 0 ? ((RectTransform)oi.GetChild(0)).rect.height : 0f;
            _app.Overlay.Close(); yield return Frames(2);

            GearUi.OpenDetail(_app, g, null); yield return Frames(2);
            UiKit.CompleteAllTweens(); yield return Frames(1);
            var od = Find(_app.Overlay.Root, "Options"); Assert.IsNotNull(od, "장비 세부 팝업의 옵션 목록");
            var sd = Find(_app.Overlay.Root, "Stats"); Assert.IsNotNull(sd, "장비 세부 팝업의 스탯 박스");
            Assert.AreEqual(1f, od.lossyScale.y, 1e-3f, "연출이 끝난 뒤에 잰다(장비 세부) — 스케일이 1 이 아니면 아래 자리 값은 연출 중간이다");
            Assert.AreEqual(((RectTransform)od).rect.height, optH, 1.5f, "옵션 목록 높이가 두 팝업에서 같다");
            Assert.AreEqual(od.position.y, optY, 1.5f, "옵션 목록 자리가 두 팝업에서 같다");
            Assert.AreEqual(((RectTransform)sd).rect.height, stH, 1.5f, "스탯 박스 높이가 두 팝업에서 같다");
            Assert.AreEqual(sd.position.y, stY, 1.5f, "스탯 박스 자리가 두 팝업에서 같다");

            // 그리고 그 결과가 무엇을 지키는지 — **줄 하나의 높이**까지 같다(여기가 눌리면 본문 40 이 잘린다).
            // ⚠ «몇 px 이상» 으로 안 적는다 — 그 수는 캔버스 기준 해상도에 매인 값이라 베껴 두면 화면 규격이 바뀔 때
            //    자가 «틀린 채로 초록» 이 된다(결정 555). 지켜야 할 것은 «장비 세부 팝업과 같다» 이고 그쪽은 T63-gear 가 이미 재고 있다.
            if (optRows > 0 && od.childCount > 0)
                Assert.AreEqual(((RectTransform)od.GetChild(0)).rect.height, rowH, 1.5f, "옵션 줄 하나의 높이가 두 팝업에서 같다");

            _log.AssertNoRed("보기 전용 세부 팝업(T267 6단계)");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(2);
        }
    }
}
