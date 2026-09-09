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
    }
}
