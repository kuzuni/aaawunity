using System.Collections;
using System.Collections.Generic;
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
    /// T269 — 리워드 팝업을 <b>닫으면 받은 것이 파티클로 흡수</b>(주인 2026-09-09 «화면 끄면 이제 해당 재화들 파티클로 돼서 흡수되는 거로 · 개수만큼 · 캡은 100개 파티클»).
    /// <para>
    /// 재는 것: ⓐ 닫기 <b>전에는</b> 구슬이 없다(팝업이 뜬 것만으로 날아가면 안 된다) ⓑ 닫으면 <b>받은 개수만큼</b> 생긴다
    /// ⓒ 개수가 많아도 <b>100개를 안 넘는다</b>(주인 캡) ⓓ 구슬 그림이 그 칸의 아이콘과 <b>같은 스프라이트</b>다(3항) ⓔ 빨간 줄 0.
    /// </para>
    /// ⚠ <b>연출만 재고 값은 안 잰다</b> — 지급은 팝업이 뜨기 전에 이미 끝나 있다(T243 «즉시 지급»). 이 자가 재화를 세면 «흡수가 지급» 이라는 거짓 계약이 생긴다.
    /// </summary>
    public class RewardAbsorbTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        int OrbCount()
        {
            int n = 0;
            if (_app == null || _app.UiCanvas == null) return 0;
            foreach (var im in _app.UiCanvas.GetComponentsInChildren<Image>(true))
                if (im.name == RewardOrbs.OrbName) n++;
            return n;
        }
        static bool Close(Overlay ov)
        {
            var dim = UiKit.Find(ov.Root, "Dimmed");
            var b = dim != null ? dim.GetComponent<Button>() : null;
            if (b == null) return false;
            b.onClick.Invoke();
            return true;
        }

        [UnityTest]
        public IEnumerator ClosingThePopupSendsOneParticlePerUnit()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            var items = new List<RewardPopup.Item>
            {
                RewardPopup.Item.Of("ui.coin", "3", null, 3),
                RewardPopup.Item.Of("hud.gem", "5", null, 5),
            };
            RewardPopup.Show(items); yield return Frames(2);
            Assert.AreEqual(2, RewardPopup.LastCellCount, "칸 둘");
            Assert.AreEqual(0, OrbCount(), "팝업이 떠 있는 동안에는 아무것도 안 날아간다");

            // 칸의 아이콘 스프라이트를 닫기 «전에» 잡아 둔다 — 닫으면 칸이 파괴된다(3항 «그림 그대로» 판정용).
            var cell = UiKit.Find(_app.Overlay.Root, "RewardCell:0");
            var cellIcon = cell != null ? UiKit.Find(cell, "Icon") : null;
            var want = cellIcon != null ? cellIcon.GetComponent<Image>().sprite : null;
            Assert.IsNotNull(want, "보상 칸의 아이콘");

            Assert.IsTrue(Close(_app.Overlay), "어둠을 눌러 닫는다"); yield return Frames(2);

            Assert.AreEqual(8, RewardPopup.LastOrbCount, "받은 개수만큼(3 + 5)");
            Assert.AreEqual(8, OrbCount(), "그만큼 실제로 떠 있다");
            // T354(주인 2026-09-10 «재화 흡수 이펙트가 팝업들보다 레이어가 낮아서 안 보여») — 구슬 층은 프레임 밑이고 오버레이보다 «앞» 이다.
            //   화면 루트 밑이면 그 안에서 맨 위여도 오버레이 아래다(부모가 다르다) — 그래서 부모와 형제 번호, 둘을 잰다.
            {
                var layer = UiKit.Find(_app.Frame, RewardPopup.OrbLayerName);
                Assert.IsNotNull(layer, "구슬 층(" + RewardPopup.OrbLayerName + ")은 프레임 밑에 선다(T354)");
                Assert.AreEqual(_app.Frame, layer.parent, "구슬 층의 부모 = 프레임(화면 루트가 아니다 · T354)");
                Assert.Greater(layer.GetSiblingIndex(), _app.Overlay.Root.GetSiblingIndex(), "구슬 층은 오버레이보다 앞에 그려진다(T354)");
                // T428 — 형제 순서는 앞 팝업이 다시 열리면(onClose) 뒤집힌다 → 층은 제 Canvas(overrideSorting · sortingOrder 100)로 늘 위여야 한다.
                var cv = layer.GetComponent<Canvas>();
                Assert.IsNotNull(cv, "구슬 층에 제 Canvas 가 있다(T428 · 주인 «캔버스를 따로»)");
                Assert.IsTrue(cv.overrideSorting, "구슬 층 Canvas 는 overrideSorting(T428)");
                Assert.AreEqual(RewardPopup.OrbSortingOrder, cv.sortingOrder, "구슬 층 sortingOrder = 최상위(T428)");
                // 앞 팝업이 다시 열려 오버레이가 맨 위로 가도(형제 순서로는 구슬이 뒤) Canvas 정렬로 이긴다 — 그 상황을 만들어 재확인
                int ovIndex0 = _app.Overlay.Root.GetSiblingIndex();
                _app.Overlay.Root.SetAsLastSibling();
                Assert.Less(layer.GetSiblingIndex(), _app.Overlay.Root.GetSiblingIndex(), "(상황 재현) 형제 순서로는 오버레이가 위");
                // ⛑ T430 — «유일하다» 가 아니라 «**이긴다**» 를 잰다.
                //   종전 줄은 `overrideSorting && sortingOrder > 0` 이었는데 그 둘은 **세 줄 위에서 이미 단언한 그 필드**다 —
                //   `SetAsLastSibling()` 은 형제 번호만 바꾸고 Canvas 필드는 못 건드리므로 **재현은 코드에 있고 잰 것은 글에만 있었다**.
                //   «위» 를 만드는 진짜 까닭은 «정렬을 무르는 경쟁자가 없다» 는 사실인데, 그것이 주석에만 있었다.
                //   ⚠ 그 전제를 깰 손이 **주인의 말 그 자체**다 — 처방이 «캔버스를 따로 하던지» 였으니, 다음에 무엇이 가려질 때
                //     누가 그 팝업에 `overrideSorting` 을 다는 것은 이 레포에서 가장 자연스러운 손이다. 그날 구슬은 다시 가려지는데
                //     종전 단언은 **한 줄도 안 빨개진다** — 주인이 T354·T428 로 **두 번** 말한 자리라 조용한 되돌아감이 특히 비싸다.
                //   ⚠ 재는 것은 «그리기 결과» 가 아니라 **정렬 값**이다(유니티가 Canvas 정렬로 형제 순서를 이긴다는 규칙 자체는 안 돌려 봤다 · 검수 Q 7항).
                int rivalTop = int.MinValue; string rivalName = null;
                foreach (var c in _app.Frame.GetComponentsInChildren<Canvas>(true))
                {
                    if (c == cv || !c.overrideSorting || c.sortingOrder <= rivalTop) continue;
                    rivalTop = c.sortingOrder; rivalName = c.name;
                }
                if (rivalName != null)
                    Assert.Greater(cv.sortingOrder, rivalTop,
                        $"정렬을 무르는(overrideSorting) Canvas 가운데 구슬 층이 가장 위여야 한다 — «{rivalName}»({rivalTop})가 구슬({cv.sortingOrder})을 덮는다(T430)");
                // ⛑ T430 — 만든 상황을 되돌린다. 지금은 뒤 단언이 순수 계산이라 탈이 없지만,
                //   뒤에 형제 순서를 보는 단언이 붙는 날 **조용히 바뀐 판**을 보게 된다.
                _app.Overlay.Root.SetSiblingIndex(ovIndex0);
            }
            // T367(주인 2026-09-10 «다이아는 다이아 쪽, 골드는 골드 쪽으로 흡수») — 보상 칸이 실제로 쓰는 키(ui.gemRed · ui.coin)가 제 pill 로 간다.
            //   옛 판정은 다이아 키 셋만 알아 `ui.gemRed`(출석·데일리·챕터 상자·퀘스트 트랙)가 가운데 아래 «자리 없음» 으로 빨려 들어갔다.
            Assert.AreEqual("ResourceBar_Gem", RewardPopup.PillFor("ui.gemRed"), "ui.gemRed(보상 칸의 다이아) → 다이아 pill(T367)");
            Assert.AreEqual("ResourceBar_Gem", RewardPopup.PillFor("hud.gem"), "hud.gem → 다이아 pill");
            Assert.AreEqual("ResourceBar_Coin", RewardPopup.PillFor("ui.coin"), "ui.coin → 골드 pill");
            Assert.AreEqual("ResourceBar_Coin", RewardPopup.PillFor("hud.gold"), "hud.gold → 골드 pill");
            Assert.IsNull(RewardPopup.PillFor("ui.bookBlue"), "재화 아닌 것은 pill 없음(가운데 아래로)");
            Assert.IsNull(RewardPopup.PillFor("ui.keyBlue"), "열쇠는 pill 없음");
            // T440(주인 «다이아가 현재 다이아 개수 표시되는 쪽으로 흡수되어야 하는데 안 그리 되네») — 과녁은 **탑바 안의 켜진** pill 이다(꺼진 동명이인이 아니라).
            {
                var gemT = RewardPopup.TargetFor(_app, "ui.gemRed"); var coinT = RewardPopup.TargetFor(_app, "ui.coin");
                Assert.IsNotNull(gemT, "다이아 과녁"); Assert.IsNotNull(coinT, "골드 과녁");
                Assert.AreEqual("ResourceBar_Gem", gemT.name, "다이아 과녁 = 다이아 pill(T440)");
                Assert.IsTrue(gemT.gameObject.activeInHierarchy, "다이아 과녁은 켜진 조각이다 — 꺼진 동명이인이면 구슬이 엉뚱한 자리로 간다(T440)");
                Assert.IsNotNull(gemT.GetComponentInParent<Transform>().root, "루트");
                Transform p = gemT; bool underBar = false; while (p != null) { if (p.name == TopBar.RootName) { underBar = true; break; } p = p.parent; }
                Assert.IsTrue(underBar, "다이아 과녁은 탑바(" + TopBar.RootName + ") 밑의 그 pill 이다(T440)");
                Assert.AreNotEqual(RewardPopup.OrbSinkName, gemT.name, "다이아는 sink(가운데 아래)로 가면 안 된다(T440)");
            }
            Assert.IsFalse(_app.Overlay.IsOpen, "팝업은 닫혔다");

            bool same = false;
            foreach (var im in _app.UiCanvas.GetComponentsInChildren<Image>(true))
                if (im.name == RewardOrbs.OrbName && im.sprite == want) same = true;
            Assert.IsTrue(same, "파티클 그림 = 그 칸의 아이콘(3항)");
            _log.AssertNoRed("리워드 흡수");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator TheParticleCountIsCappedAtAHundred()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            var items = new List<RewardPopup.Item>
            {
                RewardPopup.Item.Of("ui.coin", "5,000", null, 5000),
                RewardPopup.Item.Of("hud.gem", "300", null, 300),
            };
            RewardPopup.Show(items); yield return Frames(2);
            Assert.IsTrue(Close(_app.Overlay), "닫는다"); yield return Frames(2);

            Assert.AreEqual(RewardPopup.MaxOrbs, RewardPopup.LastOrbCount, "주인 «캡은 100개 파티클»");
            Assert.AreEqual(RewardPopup.MaxOrbs, OrbCount(), "화면에 뜬 것도 그만큼이다");
            _log.AssertNoRed("리워드 흡수(캡)");
            yield return Shutdown();
        }

        /// <summary>
        /// T445 — 주인 «흡수 이펙트 재화 <b>크기 2배</b>로 키워»(2026-09-11 · T440 이 고친 주인 말 <b>둘 중 둘째</b>).
        /// <para>
        /// 재는 것: 구슬의 <c>sizeDelta</c> = 그 칸 아이콘 높이 × <b>2</b>. 고침은 T440 이 이미 넣었는데(<c>RewardPopup.OrbSizeMul</c>) <b>재는 자가 없었다</b> —
        /// 누가 그 상수를 1 로 되돌리거나 <c>sizePx</c> 셈을 손봐도 한 줄도 안 빨개졌다(워커 F 등재 · 결정 1248).
        /// </para>
        /// ⚠ <b>기댓값에 <see cref="RewardPopup.OrbSizeMul"/> 을 쓰지 않는다</b> — 그 상수를 되돌리는 손이 바로 이 자가 잡아야 할 손인데,
        /// 상수로 기대를 세우면 기대가 <b>같이 따라가 조용히 초록</b>이 된다(결정 1220 이 이름 붙인 «자기 값을 자기가 읽는» 자리).
        /// 그래서 주인의 말 «2배» 를 <b>글자 그대로</b> 박는다 — 이 수를 바꾸려면 주인 말이 바뀌어야 한다.
        /// <para>
        /// ⚠ 칸은 닫으면 파괴되므로 높이는 <b>닫기 «전»</b>에 재 둔다. <c>src</c> 차례(아이콘, 없으면 칸 자신)는 <c>RewardPopup</c> 의 그것을 그대로 흉내 낸다.
        /// 잔상(<see cref="RewardOrbs.TrailName"/>)은 이름이 달라 섞이지 않는다. 구슬 크기는 태어날 때 <c>sizeDelta</c> 에 박히고 뒤 트윈은 자리만 건드린다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheOrbIsTwiceTheCellIconHeight()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            var items = new List<RewardPopup.Item> { RewardPopup.Item.Of("ui.coin", "3", null, 3) };
            RewardPopup.Show(items); yield return Frames(3);
            Assert.AreEqual(1, RewardPopup.LastCellCount, "칸 하나");

            var cell = UiKit.Find(_app.Overlay.Root, "RewardCell:0") as RectTransform;
            Assert.IsNotNull(cell, "보상 칸");
            var icon = UiKit.Find(cell, "Icon") as RectTransform;
            var src = icon != null ? icon : cell;
            float h = Mathf.Max(16f, src.rect.height);   // 닫기 전에 재 둔다
            Assert.Greater(h, 0f, "칸(또는 그 아이콘) 높이");

            Assert.IsTrue(Close(_app.Overlay), "어둠을 눌러 닫는다"); yield return Frames(2);

            float want = h * 2f;   // ⚠ 주인의 «2배» — 상수를 읽지 않는다
            int seen = 0;
            foreach (var im in _app.UiCanvas.GetComponentsInChildren<Image>(true))
            {
                if (im.name != RewardOrbs.OrbName) continue;
                var rt = im.transform as RectTransform;
                Assert.IsNotNull(rt, "구슬은 RectTransform 이다");
                Assert.AreEqual(want, rt.sizeDelta.x, 1f, $"구슬 가로 = 칸 아이콘 높이({h}) × 2 — 주인 «크기 2배로 키워»(T445)");
                Assert.AreEqual(want, rt.sizeDelta.y, 1f, $"구슬 세로 = 칸 아이콘 높이({h}) × 2 — 주인 «크기 2배로 키워»(T445)");
                seen++;
            }
            Assert.AreEqual(3, seen, "받은 개수(3)만큼 떠 있다 — 셋 다 재 봤다");
            Assert.AreEqual(2f, RewardPopup.OrbSizeMul, 0.001f, "배율 상수도 주인 말 그대로 2 다(T445)");
            _log.AssertNoRed("구슬 크기");
            yield return Shutdown();
        }

        /// <summary>
        /// T448 — 구슬 개수를 정하는 길은 <b>둘</b>인데(<c>Amount</c> · 없으면 <b>보여 주는 글자 되읽기</b>) 위 셋은 <b>늘 첫째만</b> 밟았다.
        /// <para>
        /// 게임이 실제로 쓰는 쪽은 둘째다 — <c>RewardPopup.Item.Of</c> 부름 스물넷 중 <b>열하나</b>가 <c>amount</c> 를 안 준다(검수 Q 실측 · T448 등재).
        /// 그 열한 자리를 재는 단언이 하나도 없었다.
        /// </para>
        /// <para>
        /// ⚑ <b>이 자가 잡으려는 손은 «글자를 짧게 쓰는 손»</b>이다. 칸은 «1,000» 을 «1K» 로 <b>보여만</b> 주는데(<c>GearUi.CellQtyShorten</c> · T443),
        /// 누가 그 짧게 쓰기를 <b>부르는 쪽 문자열 자체</b>에 옮기면 <c>QtyOf</c> 가 «1K» 를 되읽어 <b>구슬이 1,000개 대신 1개</b> 난다 — <b>백 배</b>다.
        /// 그것은 짐작이 아니라 T443 5회차(워커 K · 결정 1257)가 <b>간발로 비켜 간 사고</b>이고, 비켜 갔을 뿐 구멍은 그대로였다.
        /// 그래서 이 자는 그 둘을 <b>한 판에서 같이</b> 잰다: <b>글자는 짧아져 있고(«1K») 구슬은 원본에서 세어야 한다(100)</b>.
        /// 한쪽만 재면 그 손이 안 잡힌다 — 글자만 재면 개수가 조용히 무너지고, 개수만 재면 «짧게 보여 주기» 가 사라진 것을 못 본다.
        /// </para>
        /// ⚠ <b>«1K» 를 넣어 «구슬 1개» 를 단언하지 않았다</b>(등재 글의 셋째 줄) — 그것은 <b>고장을 계약으로 굳히는</b> 것이다.
        /// «1K» 가 1개가 되는 것은 옳은 거동이 아니라 지금 <b>살아 있는 고장</b>이고(부르는 쪽 셋이 이미 그 꼴을 넘긴다 · T449),
        /// 그 자리를 고치는 손이 이 자에서 빨강을 만나면 안 된다(결정 930 의 뒤집힌 꼴).
        /// </summary>
        [UnityTest]
        public IEnumerator 수량을_글자로만_준_칸도_짧게_보여_주되_구슬은_원본_수만큼_난다()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            // ⚠ amount 를 **일부러 안 준다** — 게임의 열한 자리가 이 꼴이다.
            var items = new List<RewardPopup.Item> { RewardPopup.Item.Of("ui.coin", "1,000") };
            RewardPopup.Show(items); yield return Frames(3);
            Assert.AreEqual(1, RewardPopup.LastCellCount, "칸 하나");

            // ⓐ 보여 주는 글자는 짧아져 있다 — 이것이 이 자가 만드는 «상황» 이다(닫으면 칸이 파괴되므로 먼저 본다).
            var cell = UiKit.Find(_app.Overlay.Root, "RewardCell:0");
            Assert.IsNotNull(cell, "보상 칸");
            var qty = UiKit.Find(cell, "Qty");
            var q = qty != null ? qty.GetComponent<TMP_Text>() : null;
            Assert.IsNotNull(q, "칸 오른쪽 아래 수량 글자(이름 «Qty» · T443)");
            Assert.AreEqual("1K", q.text, "보여 줄 때는 짧게 쓴다(GearUi.CellQtyShorten · 주인 레퍼런스 16 의 «10K» 꼴 · T443)");

            Assert.IsTrue(Close(_app.Overlay), "어둠을 눌러 닫는다"); yield return Frames(2);

            // ⓑ 그런데 구슬은 **원본 글자**(«1,000»)에서 센다 — 1000 은 주인 캡 100 으로 잘린다.
            Assert.AreEqual(RewardPopup.MaxOrbs, RewardPopup.LastOrbCount,
                            "구슬은 보여 주는 «1K» 가 아니라 원본 «1,000» 에서 세어야 한다 — 이 줄이 빨가면 누가 짧게 쓰기를 부르는 쪽 문자열로 옮긴 것이고, 그때 구슬은 100개가 아니라 1개다(T448)");
            Assert.AreEqual(RewardPopup.MaxOrbs, OrbCount(), "그만큼 실제로 떠 있다");
            _log.AssertNoRed("글자로만 준 수량");
            yield return Shutdown();
        }

        /// <summary>
        /// T448 — 숫자·콤마 말고 <b>다른 글자가 섞인</b> 수량(«×3»)은 짧게 쓰지 않고 그대로 두며, 구슬은 그 안의 수만큼 난다.
        /// <para>
        /// ⚠ 이 줄이 값진 까닭: <c>QtyOf</c> 는 글자 안의 <b>모든 숫자를 이어 붙인다</b>(«×3» → 3 · «10%» → 10 · «1.5M» → <b>15</b>).
        /// 곧 «수를 되읽는다» 는 것은 <b>파싱이 아니라 숫자 줍기</b>이고, 그 성질이 위 <see cref="수량을_글자로만_준_칸도_짧게_보여_주되_구슬은_원본_수만큼_난다"/> 가 막는 사고의 뿌리다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator 기호가_섞인_수량은_그대로_두고_그_안의_수만큼_구슬이_난다()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            var items = new List<RewardPopup.Item> { RewardPopup.Item.Of("ui.coin", "×3") };
            RewardPopup.Show(items); yield return Frames(3);
            Assert.AreEqual(1, RewardPopup.LastCellCount, "칸 하나");

            var cell = UiKit.Find(_app.Overlay.Root, "RewardCell:0");
            var qty = cell != null ? UiKit.Find(cell, "Qty") : null;
            var q = qty != null ? qty.GetComponent<TMP_Text>() : null;
            Assert.IsNotNull(q, "칸 오른쪽 아래 수량 글자");
            Assert.AreEqual("×3", q.text, "숫자·콤마 말고 다른 글자가 섞이면 부르는 쪽의 뜻이라 손대지 않는다(GearUi.CellQtyShorten)");

            Assert.IsTrue(Close(_app.Overlay), "어둠을 눌러 닫는다"); yield return Frames(2);

            Assert.AreEqual(3, RewardPopup.LastOrbCount, "«×3» 에서 3 을 읽어 구슬 셋(T448)");
            Assert.AreEqual(3, OrbCount(), "그만큼 실제로 떠 있다");
            _log.AssertNoRed("기호 섞인 수량");
            yield return Shutdown();
        }
    }
}
