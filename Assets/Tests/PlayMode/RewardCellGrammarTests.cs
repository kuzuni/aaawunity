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
    /// T443 — <b>주인이 꼴을 못 박은 «칸 문법»</b>이 실제 화면의 칸에 서 있는가.
    /// (주인 2026-09-11 «모든 재화나 아이템들이 프레임 안에 있는 경우 가독성 떨어지네» → 재차
    /// «프레임 내부 가운데에 적당히 크게 재화 아이콘이 표시되게 하고 · 프레임 오른쪽 아래에 개수 표시 ·
    /// 아이콘과 텍스트가 <b>겹치는 식으로</b> · 안 겹치게 떨어뜨려 놓으니까 너무 안 보임 작아 보임 · 던전 보상 같은 거».)
    /// <para>
    /// ⚑ <b>재는 것은 «가독성» 이 아니라 주인이 말한 그 세 가지다</b> — ⓐ 아이콘이 칸을 충분히 채운다 ⓑ 수량이 오른쪽 아래다
    /// ⓒ 수량이 아이콘과 <b>겹친다</b>. 밝기·대비를 문턱으로 박지 않는다: 그것은 그림(<c>screens</c> PNG)이 답하는 물음이고,
    /// 못 재 본 값으로 문턱을 박으면 애먼 빨강이 뜬다(결정 930).
    /// </para>
    /// <para>
    /// ⚠ <b>공용 함수를 직접 부르지 않고 «진짜 문» 으로 들어간다</b> — 던전 세부 팝업(21)의 보상 칸이 주인이 이름을 대고 말한 자리다.
    /// <c>GearUi.CellArt</c> 를 여기서 손으로 부르면 «함수는 옳은데 화면이 그 함수를 안 쓰는» 갈래를 통째로 못 본다(결정 1237 ②).
    /// </para>
    /// </summary>
    public class RewardCellGrammarTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

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
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static bool ClickNamed(Transform root, string name) { var t = root != null ? UiKit.Find(root, name) : null; var b = t != null ? t.GetComponent<Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        static Rect World(RectTransform rt) { var c = new Vector3[4]; rt.GetWorldCorners(c); return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y); }
        static List<RectTransform> Cells(Transform root, string prefix)
        {
            var res = new List<RectTransform>();
            if (root == null) return res;
            foreach (var t in root.GetComponentsInChildren<RectTransform>(false)) if (t.name.StartsWith(prefix)) res.Add(t);
            return res;
        }

        [UnityTest]
        public IEnumerator 재화칸은_아이콘이_가운데_크게_수량은_오른쪽_아래에_겹쳐_얹힌다()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(3);
            var pg = UiKit.Find(_app.Current.Root, "Page:" + EventsScreen.PageDungeon);
            Assert.IsTrue(ClickNamed(UiKit.Find(pg, "Card:hell"), "EnterBtn"), "던전 세부 팝업(21) 열기"); yield return Frames(3);

            var cells = Cells(_app.Overlay.Root, "RewardCell:");
            Assert.Greater(cells.Count, 0, "세부 팝업의 보상 칸 — 이 자가 재려는 자리다");

            int withQty = 0;
            foreach (var cell in cells)
            {
                var cr = World(cell);
                Assert.Greater(cr.width * cr.height, 0f, cell.name + " 의 칸이 잡혀 있다");

                // ⓐ 아이콘이 칸을 채운다 — 주인 «적당히 크게». 문턱은 «수량을 피해 눌린 옛 크기(56%)» 와
                //    지금 값(GearUi.CellIconPct = 80%) 을 가르는 자리에 둔다. 수를 자에 박지 않고 그 상수에서 읽는다.
                var iconT = UiKit.Find(cell, "Icon");
                Assert.IsNotNull(iconT, cell.name + " 의 아이콘");
                var ir = World((RectTransform)iconT);
                float fill = Mathf.Min(ir.width / Mathf.Max(1e-3f, cr.width), ir.height / Mathf.Max(1e-3f, cr.height));
                Assert.GreaterOrEqual(fill, (GearUi.CellIconPct - 10f) / 100f,
                                      cell.name + " — 아이콘이 칸의 " + (GearUi.CellIconPct - 10f) + "% 는 채워야 한다(지금 "
                                      + Mathf.RoundToInt(fill * 100f) + "%). 수량에 자리를 내주느라 그림이 눌리면 주인이 본 그 화면이다");

                // ⓑ·ⓒ 수량 — 칸 오른쪽 아래 · 아이콘과 겹친다 · 검은 외곽선.
                var qtyT = UiKit.Find(cell, "Qty");
                if (qtyT == null) continue;   // 수량이 없는 칸(종류만 보여 주는 자리)은 이 문법 밖이다
                withQty++;
                var q = qtyT.GetComponent<TMP_Text>();
                Assert.IsNotNull(q, cell.name + " 의 «Qty» 는 글자여야 한다");
                var qrt = (RectTransform)qtyT;
                Assert.GreaterOrEqual(qrt.anchorMax.x, 0.9f, cell.name + " — 수량은 칸 **오른쪽** 끝에 붙는다(anchorMax.x)");
                Assert.LessOrEqual(qrt.anchorMin.y, 0.1f, cell.name + " — 수량은 칸 **아래** 끝에 붙는다(anchorMin.y)");

                var qr = World(qrt);
                Assert.IsTrue(qr.Overlaps(ir),
                              cell.name + " — 수량 글자가 아이콘과 **겹쳐야** 한다(주인 «겹치는 식으로»). "
                              + "떼어 놓으면 둘 다 작아진다 — 아이콘 " + ir + " · 수량 " + qr);
                Assert.AreNotEqual(FontStyles.Normal, q.fontStyle & FontStyles.Bold, cell.name + " — 수량은 굵게(겹쳐도 읽히게)");

                // ⛑ **그려지는 크기**(bestFit 결과)가 보조 하한 밑으로 내려가면 안 된다 — 1회차가 여기서 빨갰다(런 1070 · «used 35 < 36»).
                //    수량 rect 를 칸 안에 가두면 긴 수가 눌리고, 그것은 주인이 말한 병(«너무 안 보임 작아 보임»)을 다른 꼴로 되풀이하는 것이다.
                //    ⚠ 이 줄이 없으면 같은 고장을 **다른 절의 자**(EventsScreenTests 가독성 게이트)가 대신 잡는다 — 그러면 빨강이 «칸 문법» 이 아니라 «던전 화면» 의 얼굴로 온다.
                //    자동 크기가 켜져 있든 아니든 `fontSize` 가 곧 «지금 그려지는 크기» 다(자동 크기면 TMP 가 그 값을 눌러 적는다).
                float used = q.fontSize;
                Assert.GreaterOrEqual(used, TextSize.Aux,
                                      cell.name + " — 수량이 실제로 그려지는 크기 " + used + " 가 보조 하한 " + TextSize.Aux + " 밑이다(T63 · 주인 «글씨가 너무 작아 안 읽힌다»). "
                                      + "rect " + qrt.rect.width + "×" + qrt.rect.height + " · 선호 " + q.preferredWidth + "×" + q.preferredHeight);

                // ⛑ **그리고 이웃 칸을 덮으면 안 된다** — 2회차가 여기서 그림을 망쳤다(런 1072 · 던전 «11 1,000 … 5 1,000» · 출석 «10,0001,000»).
                //    크기를 지키려고 rect 를 132% 로 넓혔더니 글자가 옆 칸 위로 올라가 **붙어 읽혔다** — 주인이 말한 병의 세 번째 얼굴이다.
                //    ⚠ 재는 것은 rect 가 아니라 **글자 덩이**(`preferredWidth`)다: rect 는 넓어도 글자가 짧으면 아무 데도 안 닿는다.
                //       오른쪽 아래 정렬이라 글자는 칸 오른쪽 끝(+걸침)에서 왼쪽으로 자란다.
                float over = cr.width * GearUi.CellQtyOver / 100f;
                float inkLeft = cr.xMax + over - q.preferredWidth;
                Assert.GreaterOrEqual(inkLeft, cr.xMin - over - 1f,
                                      cell.name + " — 수량 글자가 칸 왼쪽으로 넘쳐 이웃 칸을 덮는다(글자 폭 " + q.preferredWidth + " · 칸 폭 " + cr.width + "). "
                                      + "칸에 안 들어가는 수는 넓히지 말고 **짧게 쓴다**(GearUi.CellQtyText · 주인 레퍼런스 16 의 «10K» 꼴)");

                // 외곽선은 **글자마다**가 아니라 폰트 애셋의 공유 머티리얼 한 장에 걸려 있다(T207 ② · `EnsureOutline`).
                // 그래서 `TMP_Text.outlineWidth`(개체별 덮어쓰기)를 보면 0 이라 늘 빨갛다 — 실제로 칠하는 그 자리를 본다.
                var mat = q.fontSharedMaterial;
                Assert.IsNotNull(mat, cell.name + " — 글자에 머티리얼이 있다");
                Assert.IsTrue(mat.HasProperty(TmpFont.OutlineWidthProp), cell.name + " — 그 머티리얼이 TMP SDF 테를 가진 것이다");
                Assert.Greater(mat.GetFloat(TmpFont.OutlineWidthProp), 0f,
                               cell.name + " — 겹쳐 얹는 글자는 **검은 외곽선**으로 읽힌다(주인 상시 지시 · T63-outline)");
            }
            Assert.Greater(withQty, 0, "보상 칸 가운데 수량이 적힌 칸이 하나는 있어야 이 자가 ⓑⓒ 를 실제로 잰 것이다");

            _log.AssertNoRed("T443 칸 문법");
            yield return Shutdown();
        }
    }
}
