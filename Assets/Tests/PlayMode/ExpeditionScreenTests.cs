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
    /// T97 — 탐험(방치·오프라인 보상 · 주인 2026-09-07 «켜두거나 꺼둬도 쩄든 방치 보상 쌓이고 · 골드·다이아 쌓이게 · 빠른 탐험은 광고 보고»).
    /// 규칙 자체는 EditMode <c>ExpeditionTests</c> 가 본다 — 여기서는 <b>화면</b>이다:
    /// ⓐ 로비 «탐험» 보조 버튼이 팝업(표 ㉕)을 연다 ⓑ 조각이 다 있다(그림 띠·명판·경과 시간·시간당 pill 2·보상 칸 2·버튼 2)
    /// ⓒ 8시간 전으로 «마지막 정산» 을 돌려 두면 «받기» 가 열리고 누르면 재화가 늘고 칸이 0 으로 돌아간다
    /// ⓓ 빠른 탐험 팝업(표 ㉖)이 뜨고 보상 칸·광고 버튼이 있다 ⓔ 로비 «탐험» 아이콘 빨간 점 ⓕ 영문 데모 글자 0 · 빨간 줄 0(<see cref="PlayLog"/> · T11 규약).
    /// </summary>
    public class ExpeditionScreenTests
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
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; Assert.IsNotNull(_app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");
            yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        /// <summary>T146 ⓐ — 이 팝업에는 공통 제목 리본(<see cref="PopupRibbonTag"/>)이 «켜진 채로» 남아 있으면 안 된다(레퍼런스 30·31 은 명판이 제목이다).</summary>
        static void AssertNoPopupRibbon(Transform popupRoot, string what)
        {
            int on = 0;
            foreach (var tag in popupRoot.GetComponentsInChildren<PopupRibbonTag>(true))
                if (tag.gameObject.activeInHierarchy) on++;
            Assert.AreEqual(0, on, what + " 에 글자 없는 공통 제목 리본이 떠 있다(T146 ⓐ · 레퍼런스에는 없다)");
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { var r = Find(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }
        static string CellQty(Transform root, string cell)
        {
            var c = Find(root, cell); if (c == null) return null;
            var q = Find(c, "Qty"); var t = q != null ? q.GetComponent<TMP_Text>() : null;
            return t != null ? t.text : null;
        }
        /// <summary>
        /// 화면의 모든 활성 글자에서 «영문 데모 글자»(우리 문구는 전부 한국어·숫자·기호)를 찾는다 — T44 규칙.
        /// 단위 꼬리표(<see cref="UiKit.Fmt"/> 의 «12.9K»·«3.4M»)는 우리 숫자 표기라 영문으로 세지 않는다 — 숫자 뒤에 붙은 K/M/B 한 글자만 봐준다(CI #187 실측).
        /// </summary>
        static string EnglishLeftOver(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t == null || !t.gameObject.activeInHierarchy || string.IsNullOrEmpty(t.text)) continue;
                string s = t.text;
                for (int i = 0; i < s.Length; i++)
                {
                    char ch = s[i];
                    if (!((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'))) continue;
                    bool unitTail = (ch == 'K' || ch == 'M' || ch == 'B') && i > 0 && (char.IsDigit(s[i - 1]) || s[i - 1] == '.');
                    if (unitTail) continue;
                    return t.name + " «" + s + "»";
                }
            }
            return null;
        }

        /// <summary>ⓐⓑⓒⓕ — 로비에서 팝업이 열리고, 8시간치가 쌓인 상태면 «받기» 로 골드·다이아가 실제로 는다.</summary>
        [UnityTest]
        public IEnumerator ExpeditionPopupShowsAccruedRewardsAndClaimPaysThem()
        {
            yield return Boot();
            var D = _app.Data != null ? _app.Data.Expedition : null;
            Assert.IsNotNull(D, "expedition.json 이 카탈로그(data.expedition)로 실려야 한다");
            _app.ShowScreen("lobby"); yield return Frames(2);

            // 로비의 «탐험» 보조 버튼(Side:explore)이 팝업을 연다
            var side = Find(_app.UiCanvas.transform, "Side:" + LobbyScreen.SideExplore);
            Assert.IsNotNull(side, "로비 보조 줄에 «탐험» 칸이 있어야 한다");
            var S = _app.Save;
            S.MaxChapter = 10; S.Gold = 0; S.Gem = 0;
            S.ExpSettle = LobbyPopups.NowSec() - D.MaxHours * 3600.0;   // 8시간 방치(오프라인) 상태로 열어 본다
            LobbyPopups.Expedition(_app); yield return Frames(2);
            var ov = _app.Overlay.Root;
            Assert.IsTrue(_app.Overlay.IsOpen, "탐험 팝업이 열린다");
            Assert.IsNotNull(Find(ov, "ExpeditionBox"), "팝업 상자");
            foreach (var n in new[] { "Picture", "Plate", "ExpTime", "RateGold", "RateGem", "ExpCellGold", "ExpCellGem", "QuickBtn", "ClaimBtn", "CapNote" })
                Assert.IsNotNull(Find(ov, n), "조각 " + n + " (표 ㉕)");
            // T146 ⓐ — 레퍼런스 30 에는 공통 제목 리본이 없다(제목은 상자 폭 «명판»). 종전 코드는 리본을 «Title_01» 이라는 프리팹 이름으로 찾아 껐는데
            // UiKit.Spawn 이 이름을 카탈로그 키로 바꿔 놓아 한 번도 안 맞았고, 글자 없는 초록 리본이 그림 띠 위에 떠 있었다(결정 388).
            AssertNoPopupRibbon(ov, "탐험 팝업(30)");
            // T146 ⓑ — 레퍼런스 30 의 띠에는 기사와 적이 길 위를 걸어간다. 조각(HeroView)이 둘 다 서 있고 발이 같은 높이(길 위)인지 본다.
            var picRt = (RectTransform)Find(ov, "Picture");
            var knight = Find(picRt, "Knight"); var foe = Find(picRt, "Foe");
            Assert.IsNotNull(knight, "그림 띠의 기사(T146 ⓑ)"); Assert.IsNotNull(foe, "그림 띠의 적(T146 ⓑ)");
            Assert.IsNotNull(knight.GetComponentInChildren<HeroView>(true), "기사는 HeroView 조각으로 세운다(새 그림 0)");
            Assert.IsNotNull(foe.GetComponentInChildren<HeroView>(true), "적도 HeroView 조각으로 세운다");
            float KneeY(Transform t) { var c = new Vector3[4]; ((RectTransform)t).GetWorldCorners(c); return picRt.InverseTransformPoint(c[0]).y; }
            Assert.AreEqual(KneeY(knight), KneeY(foe), picRt.rect.height * 0.06f, "기사와 적의 발이 길 위 같은 높이에 선다(레퍼런스 30)");
            Assert.Less(((RectTransform)knight).anchorMin.x, ((RectTransform)foe).anchorMin.x, "기사가 왼쪽 · 적이 오른쪽(레퍼런스 30)");

            // 쌓인 값이 화면에 «0» 이 아니라 실제 계산값으로 찍힌다
            Expedition.Pending(_app.Data, S, D, LobbyPopups.NowSec(), SaveStore.Today(), out double pg, out double pm);
            Assert.Greater(pg, 0, "8시간이면 골드가 쌓여 있다"); Assert.Greater(pm, 0, "다이아도 쌓여 있다");
            Assert.AreEqual(UiKit.Fmt(pg), CellQty(ov, "ExpCellGold"), "골드 칸 숫자 = 규칙이 계산한 값");
            Assert.AreEqual(UiKit.FmtQty(pm), CellQty(ov, "ExpCellGem"), "다이아 칸 숫자 = 규칙이 계산한 값");
            Assert.IsNull(EnglishLeftOver(ov), "영문 데모 글자 0 (T44)");

            // «받기» — 실제로 재화가 늘고, 칸은 0 으로 돌아간다
            var claim = Find(ov, "ClaimBtn"); Assert.IsNotNull(claim, "받기 버튼");
            var btn = claim.GetComponent<Button>(); Assert.IsNotNull(btn); Assert.IsTrue(btn.interactable, "쌓인 게 있으면 받기가 열린다");
            double gold0 = S.Gold, gem0 = S.Gem;
            btn.onClick.Invoke(); yield return Frames(2);
            // T241 — 받으면 **공통 «리워드» 팝업**이 먼저 뜬다(골드·다이아 두 칸). 탭해서 닫으면 탐험 팝업이 다시 열린다.
            {
                var rv = _app.Overlay.Root;
                Assert.IsNotNull(Find(rv, "RewardTitle"), "받기 → 리워드 팝업(T241)");
                Assert.AreEqual(2, RewardPopup.LastCellCount, "골드·다이아 두 칸");
                var dim = Find(rv, "Dimmed")?.GetComponent<Button>(); Assert.IsNotNull(dim, "리워드 팝업의 탭하여 닫기");
                dim.onClick.Invoke(); yield return Frames(2);
                Assert.IsNotNull(Find(_app.Overlay.Root, "ExpeditionBox"), "닫으면 탐험 팝업으로 돌아온다");
            }
            Assert.AreEqual(pg, S.Gold - gold0, 1.0, "받기 = 보이던 골드만큼 지급");
            Assert.AreEqual(pm, S.Gem - gem0, 1.0, "받기 = 보이던 다이아만큼 지급");
            Assert.AreEqual("0", CellQty(_app.Overlay.Root, "ExpCellGold"), "받은 뒤에는 0 부터 다시 쌓인다");
            var claim2 = Find(_app.Overlay.Root, "ClaimBtn");
            Assert.IsFalse(claim2.GetComponent<Button>().interactable, "받은 직후에는 받기가 잠긴다(«다음까지 mm:ss»)");
            _log.AssertNoRed("탐험 팝업");

            _app.Overlay.Close(); yield return Frames(1);
            yield return Shutdown();
        }

        /// <summary>ⓓⓔⓕ — 빠른 탐험 팝업(표 ㉖)과 로비 빨간 점.</summary>
        [UnityTest]
        public IEnumerator QuickExplorePopupAndLobbyRedDot()
        {
            yield return Boot();
            var D = _app.Data != null ? _app.Data.Expedition : null; Assert.IsNotNull(D);
            var S = _app.Save; S.MaxChapter = 10;
            S.ExpQuickDay = SaveStore.Today(); S.ExpQuickUsed = 0;   // 옛 필드(T265 로 안 쓴다) — 그래도 «가득» 이어야 한다: 아래 Roll 이 충전을 채운다
            _app.ShowScreen("lobby"); yield return Frames(2);

            // ⓔ 빨간 점 — 빠른 탐험 횟수가 남아 있으면 켜져 있다
            var dot = Find(_app.UiCanvas.transform, "ExpDot");
            Assert.IsNotNull(dot, "«탐험» 칸에 알림 점 조각이 있어야 한다");
            Assert.IsTrue(dot.gameObject.activeInHierarchy, "받을 게 있으면(빠른 탐험 횟수) 빨간 점이 켜진다");

            // ⓓ 빠른 탐험 팝업
            LobbyPopups.QuickExplore(_app, null); yield return Frames(2);
            var ov = _app.Overlay.Root;
            Assert.IsNotNull(Find(ov, "QuickExploreBox"), "빠른 탐험 상자");
            foreach (var n in new[] { "QxPlate", "QxSub", "QxTitle", "QxGridBg", "QxCellGold", "QxCellGem", "QxNote", "QxFreeBtn", "QxRule" })
                Assert.IsNotNull(Find(ov, n), "조각 " + n + " (표 ㉖)");
            Expedition.QuickReward(_app.Data, S, D, out double qg, out double qm);
            Assert.AreEqual(UiKit.Fmt(qg), CellQty(ov, "QxCellGold"), "빠른 탐험 골드 = 시간당 × quickHours");
            Assert.AreEqual(UiKit.FmtQty(qm), CellQty(ov, "QxCellGem"), "빠른 탐험 다이아");
            Assert.IsTrue(Find(ov, "QxFreeBtn").GetComponent<Button>().interactable, "횟수가 남으면 광고 버튼이 열린다");
            Assert.IsNotNull(Find(ov, "QxBadge"), "남은 횟수 배지");
            Assert.AreEqual(Expedition.QuickLeft(S, D, LobbyPopups.NowSec(), SaveStore.Today()).ToString(),
                Find(ov, "QxBadge").GetComponentInChildren<TMP_Text>().text, "배지 숫자 = 보유 충전(T265)");

            // T265 — 주인 «빠른 탐험 팝업 내에 그렇게 써 주면 됨, 그런 정보».
            // «규칙이 적혀 있다» 를 글자 한 조각이 아니라 **세 가지가 다 있는가** 로 잰다 —
            // 시간·최대 횟수는 표에서 온 값이어야 하고(코드에 3 을 박으면 표를 고쳐도 화면이 안 바뀐다),
            // 꽉 차 있으므로 «충전 완료» 여야 한다(카운트다운은 다 쓴 판에서 잰다).
            string ruleTxt = Find(ov, "QxRule").GetComponent<TMP_Text>().text;
            StringAssert.Contains(UiKit.FmtQty(D.QuickChargeHours) + "시간마다", ruleTxt, "충전 주기가 표 값으로 적혀 있다");
            // T270 ⓐ — 주인 재정정(«2시간마다 3개 전부 리필»)이 글자에도 와야 한다. 옛 글자는 «1회 충전» 이었다.
            StringAssert.Contains(UiKit.FmtQty(D.QuickMax) + "회 전부 충전", ruleTxt, "«N회 전부 충전» — 한 칸씩이 아니다");
            StringAssert.DoesNotContain("1회 충전", ruleTxt, "옛 글자(«1회 충전»)가 남아 있으면 안 된다");
            StringAssert.Contains("최대 " + D.QuickMax + "회", ruleTxt, "최대 보유가 표 값으로 적혀 있다");
            StringAssert.Contains("충전 완료", ruleTxt, "가득 차 있으면 «충전 완료»");
            AssertNoPopupRibbon(ov, "빠른 탐험 팝업(31)");   // T146 ⓐ — 31 도 레퍼런스에 리본이 없다(명판이 제목)
            Assert.IsNull(EnglishLeftOver(ov), "영문 데모 글자 0 (T44)");

            // ⓕ ⚑ T272(T270 이 흡수) — **여태 이 버튼을 아무 자도 «눌러 보지» 않았다.**
            //   위 줄들은 «있다 + interactable 이다» 까지만 본다. 지급 버튼 가운데 이것 하나만 그랬다(검수 Q 등재).
            //   그래서 «열려 있는데 눌러도 아무 일 없는» 꼴이 나도 이 자는 초록이다 — T228·T243 이 같은 갈래로 값을 치렀다.
            //   여기서는 **누르고 나서 세 가지가 같이 움직이는가**를 잰다: 재화 · 보유 충전 · 배지 숫자.
            //   ⚠ **이 버튼은 «그 자리에서» 주지 않는다** — 코드로 확인하고 그 순서대로 잰다(`LobbyPopups.cs:954`):
            //   누르면 먼저 모의 광고(`Overlay.AdCountdown` · 3초)가 뜨고, **광고가 끝난 뒤에** `ClaimQuick` 이 불린다.
            //   그래서 «누르고 두 프레임 뒤 골드» 를 재면 그 자가 빨개진다 — 자를 쓰기 전에 그 순서를 읽지 않았으면
            //   오늘 배포를 두 번 세운 그 꼴(전제를 안 세운 단언)을 또 냈을 것이다. 기다리는 꼴은 `RestClearAdTests` 가 이미 쓴다.
            double goldBefore = S.Gold, gemBefore = S.Gem;
            int leftBefore = Expedition.QuickLeft(S, D, LobbyPopups.NowSec(), SaveStore.Today());
            Expedition.QuickReward(_app.Data, S, D, out double wantG, out double wantM);
            Assert.Greater(leftBefore, 0, "이 판은 충전이 남아 있어야 눌러 볼 수 있다(위에서 가득이라고 쟀다)");

            Find(ov, "QxFreeBtn").GetComponent<Button>().onClick.Invoke();
            yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "누르면 먼저 모의 광고가 뜬다(T23 AdCountdown · 여기서 바로 주지 않는다)");

            float adT0 = Time.realtimeSinceStartup;                       // 광고 3초 + 여유
            while (Expedition.QuickLeft(S, D, LobbyPopups.NowSec(), SaveStore.Today()) == leftBefore
                   && Time.realtimeSinceStartup - adT0 < 8f) yield return null;
            yield return Frames(2);

            Assert.AreEqual(goldBefore + wantG, S.Gold, 1e-6,
                "광고가 끝나면 골드가 실제로 는다 — Expedition.ClaimQuick 이 불렸는가(버튼이 열려 있는 것과 주는 것은 다르다)");
            Assert.AreEqual(gemBefore + wantM, S.Gem, 1e-6, "다이아도 표대로 는다");
            Assert.AreEqual(leftBefore - 1, Expedition.QuickLeft(S, D, LobbyPopups.NowSec(), SaveStore.Today()),
                "보유 충전이 한 칸 준다(안 줄면 무한으로 받을 수 있다)");
            _log.AssertNoRed("빠른 탐험 «광고 보고 무료» 지급");

            _app.Overlay.Close(); yield return Frames(1);
            yield return Shutdown();
        }
    }
}
