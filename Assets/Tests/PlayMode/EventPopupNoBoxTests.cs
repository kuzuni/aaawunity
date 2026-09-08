using System.Collections;
using System.Collections.Generic;
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
    /// T141 — 이벤트 팝업 3형제(쉼터·악마의 거래·천사의 축복 · 그리고 같은 흐름의 악마의 선물·축복 강화)는
    /// <b>«흰 판»(공통 팝업 상자) 없이 «검정 투명 딤» 위</b>에 얹힌다(주인 2026-09-07 05:1X·05:2X · 특전 선택 04 와 같은 꼴).
    /// <list type="bullet">
    /// <item>ⓐ 팝업 나무에 상자 조각(<c>ui.popup*</c>)이 <b>0개</b>이고 <see cref="UiKit.NoBoxName"/> 투명 칸이 그 자리에 있다.</item>
    /// <item>ⓑ 어둠(<c>Dimmed</c>)은 있고 α ≥ 0.8 이며 클릭을 막는다(판이 하던 몫).</item>
    /// <item>ⓒ 그 팝업이 직접 얹은 글자는 어둠(<see cref="Palette.Dim"/>)과 <b>밝기 차 ≥ 0.35</b> — 판이 사라졌으니 어두운 글자는 안 읽힌다(T111 ⓑ).</item>
    /// <item>ⓓ 판이 없어도 <b>선택 버튼이 그대로 눌린다</b>(악마 «거절» · 천사 «무료 축복») · 어둠 탭으로는 안 닫힌다(선택 강제 팝업).</item>
    /// <item>ⓔ 빨간 줄 0(<see cref="PlayLog"/> · T11 규약).</item>
    /// </list>
    /// 다른 공통 팝업(설정 등)은 <b>판 그대로</b>임을 같이 못 박는다 — «상자 없음» 은 이 다섯 자리만이다.
    /// </summary>
    public class EventPopupNoBoxTests
    {
        PlayLog _log; App _app;
        /// <summary>글자와 어둠의 밝기 차 하한 — T132 의 자(`tools/png_contrast.py`)와 같은 판정선.</summary>
        const float MinContrast = 0.35f;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        /// <summary>나무 «전체» 에서 공통 팝업 상자 조각(카탈로그 키 <c>ui.popup…</c> 이름)을 센다 — 판이 남아 있으면 여기서 걸린다.</summary>
        static List<string> BoxPieces(Transform root)
        {
            var found = new List<string>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("ui.popup")) found.Add(t.name);
            return found;
        }
        static bool Click(Transform root, System.Func<string, bool> match)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(true))
            {
                var t = b.GetComponentInChildren<TMP_Text>(true);
                if (t == null || !match(t.text) || !b.IsInteractable()) continue;
                b.onClick.Invoke(); return true;
            }
            return false;
        }

        /// <summary>한 팝업을 ⓐⓑⓒ 로 재고 «[T141]» 로 남긴다(다음 워커가 CI 로그에서 바로 읽는다).</summary>
        void AssertNoBox(string name)
        {
            // 어둠은 α 0 에서 UiKit.DimAlpha(T199 로 0.85 → 0.985)로 «페이드»(UiKit.FadeIn) 라 연 직후 두 프레임은 아직 옅다(CI #297 실측 0.235) —
            // 비평 PNG 를 찍는 UiShotsTests 와 같은 방법으로 연출을 끝까지 돌린 뒤 잰다(T49 규약).
            UiKit.CompleteAllTweens();
            var root = _app.Overlay.Root;
            var pieces = BoxPieces(root);
            Assert.AreEqual(0, pieces.Count, name + ": 판(공통 팝업 상자) 조각이 남아 있다 — " + string.Join(", ", pieces));
            var noBox = UiKit.Find(root, UiKit.NoBoxName);
            Assert.IsNotNull(noBox, name + ": 판 대신 투명 칸(" + UiKit.NoBoxName + ")이 그 자리에 있어야 한다(내용 % 자리 불변)");
            Assert.IsNull(noBox.GetComponent<Graphic>(), name + ": 투명 칸은 그림이 없다(어둠이 그대로 비친다)");

            var dim = UiKit.Find(root, "Dimmed");
            Assert.IsNotNull(dim, name + ": 어둠(Dimmed)");
            var di = dim.GetComponent<Image>();
            Assert.IsNotNull(di, name + ": 어둠 그림");
            Assert.GreaterOrEqual(di.color.a, 0.8f, name + ": 어둠 α ≥ 0.8(04 와 같은 짙기)");
            Assert.IsTrue(di.raycastTarget, name + ": 판이 없으니 클릭 차단은 어둠이 맡는다");

            // ⓒ 이 팝업이 «직접» 얹은 글자(투명 칸의 자식 Text) — 조각 안 글자(카드·버튼)는 제 바탕을 갖고 있어 여기서 안 센다
            float dimLuma = UiKit.Luma(Palette.Dim);
            int counted = 0;
            for (int i = 0; i < noBox.childCount; i++)
            {
                var t = noBox.GetChild(i).GetComponent<TMP_Text>();
                if (t == null || !t.gameObject.activeInHierarchy || string.IsNullOrEmpty(t.text)) continue;
                float d = Mathf.Abs(UiKit.Luma(t.color) - dimLuma);
                Debug.Log($"[T141] {name} 글자 «{t.text}» 밝기차 {d:0.00} (색 {ColorUtility.ToHtmlStringRGB(t.color)})");
                Assert.GreaterOrEqual(d, MinContrast, name + ": 어둠 위 글자 «" + t.text + "» 가 안 읽힌다(밝기 차 " + d.ToString("0.00") + ")");
                counted++;
            }
            Assert.Greater(counted, 0, name + ": 잰 글자가 하나는 있어야 한다(이 자가 헛돌지 않게)");
        }

        [UnityTest]
        public IEnumerator EventPopupsSitOnTheDimWithoutTheWhitePanel()
        {
            yield return Boot();
            var D = _app.Data;
            _app.StartBattle(1); yield return RealSeconds(2f);
            Assert.AreEqual("battle", _app.Current.Name, "전투 화면");
            var bs = _app.GetScreen<BattleScreen>(); var G = bs != null ? bs.G : null; Assert.IsNotNull(G, "전투 상태");
            Time.timeScale = 0f;
            if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; yield return Frames(1); }

            // ① 쉼터 — 주인이 05:2X 에 «그 쉼터도» 라고 못 박은 자리
            G.Pending = new PendingDecision { Kind = PendingKind.Rest };
            _app.Overlay.Rest(G, _ => { }, () => { }); yield return Frames(2);
            AssertNoBox("쉼터");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s.StartsWith("경험치")), "판이 없어도 «경험치» 가 눌린다");
            yield return Frames(1);
            Assert.IsFalse(_app.Overlay.IsOpen, "고르면 닫힌다"); G.Pending = null;

            // ② 악마의 거래 — 어둠 탭으로는 안 닫힌다(선택 강제) · «거절» 은 눌린다
            var rng = new Mulberry32(7u);
            var devilPerk = Perks.OfferDevil(D, G.Taken, rng);
            G.Pending = new PendingDecision { Kind = PendingKind.Devil, DevilPerk = devilPerk };
            _app.Overlay.Devil(G, _ => { }); yield return Frames(2);
            AssertNoBox("악마의 거래");
            var dimBtn = UiKit.Find(_app.Overlay.Root, "Dimmed").GetComponent<Button>();
            Assert.IsTrue(dimBtn == null || !dimBtn.IsInteractable(), "선택 강제 팝업은 어둠 탭으로 안 닫힌다(종전 그대로)");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "거절"), "«거절» 이 눌린다");
            yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen); G.Pending = null;

            // ③ 악마의 선물(같은 흐름의 뒷 팝업)
            _app.Overlay.DevilGift(devilPerk, null); yield return Frames(2);
            AssertNoBox("악마의 선물");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s == "계속"), "«계속» 이 눌린다");
            yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen);

            // ④ 천사의 축복
            G.Pending = new PendingDecision { Kind = PendingKind.Angel };
            _app.Overlay.Angel(G, _ => { }); yield return Frames(2);
            AssertNoBox("천사의 축복");
            Assert.IsTrue(Click(_app.Overlay.Root, s => s.StartsWith("무료 축복")), "«무료 축복» 이 눌린다");
            yield return Frames(1); Assert.IsFalse(_app.Overlay.IsOpen); G.Pending = null;

            // ⑤ 축복 강화(광고 뒤 팝업) — 직접 열어 같은 꼴인지만 본다
            _app.Overlay.Blessed(_ => { }); yield return Frames(2);
            AssertNoBox("축복 강화");
            _app.Overlay.Close(); yield return Frames(2);   // 닫힌 팝업 조각이 실제로 사라진 뒤에 다음을 연다(Destroy 는 한 프레임 뒤)

            // ⑥ «상자 없음» 은 이 다섯 자리뿐 — 다른 공통 팝업(일시정지 = 설정과 같은 팝업)은 판 그대로다
            _app.Overlay.Pause(() => { }, () => { }); yield return Frames(2);
            Assert.Greater(BoxPieces(_app.Overlay.Root).Count, 0, "일시정지·설정 팝업은 종전대로 판이 있다(이 작업 범위 밖)");
            Assert.IsNull(UiKit.Find(_app.Overlay.Root, UiKit.NoBoxName), "판이 있는 팝업에는 투명 칸이 없다");
            _app.Overlay.Close(); yield return Frames(1);

            Time.timeScale = 1f;
            _log.AssertNoRed("이벤트 팝업 판 없애기(T141)");
            yield return Shutdown();
        }
    }
}
