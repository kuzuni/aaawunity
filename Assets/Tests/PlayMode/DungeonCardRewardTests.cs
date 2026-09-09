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
    /// T251 — 던전 카드(20)의 «획득 가능» 줄이 <b>세부 팝업(21)과 같은 원천</b>에서 오는가.
    /// (주인 2026-09-08 «던전 행에 획득 가능 부분에 <b>실제 던전 클리어하면 획득하는 거</b>를 놔줘야 함 · <b>세부 팝업에 있는 거 종류로</b>».)
    /// <para>
    /// <b>고침 전</b>: 카드는 <c>Dungeons</c> 표에 <b>손으로 박은 아이콘 배열</b>(원정 = 두루마리 + 열쇠 셋)을 그렸고,
    /// 팝업은 <c>dungeon.json</c>(펫알·골드)에서 칸을 만들었다 — <b>두 곳이 다른 원천</b>이라 카드가 실제 보상과 어긋났다.
    /// </para>
    /// <para>
    /// <b>아이콘 «키» 를 글자로 안 적는다</b> — 이 자는 «카드가 무엇을 그리나» 가 아니라 <b>«둘이 같은 데서 오나»</b> 를 재는 것이라,
    /// 두 곳의 <see cref="Sprite"/> 집합을 <b>맞대어</b> 본다. 키를 적어 두면 표가 바뀔 때 <b>고침이 옳은데도 자가 빨개지고</b>,
    /// 더 나쁘게는 «둘 다 틀린 같은 값» 을 통과시킨다(카드만 고치고 팝업을 안 고쳐도 초록이 된다).
    /// </para>
    /// </summary>
    public class DungeonCardRewardTests
    {
        App _app; PlayLog _log;
        static readonly string[] Keys = { "hell", "expedition" };

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
            _app = null;
            yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static bool ClickNamed(Transform root, string name) { var t = root != null ? UiKit.Find(root, name) : null; var b = t != null ? t.GetComponent<Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }

        /// <summary>줄(또는 칸 묶음) 안의 «Icon» 스프라이트를 순서대로 — 같은 것이 두 번 나오면 한 번만.</summary>
        static List<Sprite> IconKinds(Transform root, string cellPrefix)
        {
            var kinds = new List<Sprite>();
            if (root == null) return kinds;
            foreach (var t in root.GetComponentsInChildren<Transform>(false))
            {
                if (!t.name.StartsWith(cellPrefix)) continue;
                var iconT = UiKit.Find(t, "Icon");                       // `?.` 를 안 쓴다 — 유니티의 «가짜 null» 은 그 연산자를 안 지난다(T11)
                var img = iconT != null ? iconT.GetComponent<Image>() : null;
                if (img != null && img.sprite != null && !kinds.Contains(img.sprite)) kinds.Add(img.sprite);
            }
            return kinds;
        }
        static List<RectTransform> Cells(Transform root, string cellPrefix)
        {
            var res = new List<RectTransform>();
            if (root == null) return res;
            foreach (var t in root.GetComponentsInChildren<RectTransform>(false)) if (t.name.StartsWith(cellPrefix)) res.Add(t);
            return res;
        }
        static Rect World(RectTransform rt) { var c = new Vector3[4]; rt.GetWorldCorners(c); return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y); }

        /// <summary>
        /// T251 ⓐ — <b>카드의 종류 집합 = 팝업의 종류 집합</b>. 던전 둘 다 본다: 하나만 보면
        /// «표가 있는 쪽만 고친» 상태를 못 가른다(고침 전 어긋남이 가장 컸던 것은 원정이다).
        /// </summary>
        [UnityTest]
        public IEnumerator CardRewardIconsAreTheSameKindsAsTheDetailPopup()
        {
            yield return Boot();
            foreach (var key in Keys)
            {
                EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(3);
                var pg = UiKit.Find(_app.Current.Root, "Page:" + EventsScreen.PageDungeon);
                var card = UiKit.Find(pg, "Card:" + key); Assert.IsNotNull(card, key + " 카드");
                var cardKinds = IconKinds(UiKit.Find(card, "Rewards"), "Cell:");
                Assert.Greater(cardKinds.Count, 0, key + " 카드의 «획득 가능» 줄에 아이콘이 있다");

                Assert.IsTrue(ClickNamed(card, "EnterBtn"), key + " 세부 팝업 열기"); yield return Frames(3);
                var popKinds = IconKinds(_app.Overlay.Root, "RewardCell:");
                Assert.Greater(popKinds.Count, 0, key + " 팝업의 보상 칸에 아이콘이 있다");

                CollectionAssert.AreEquivalent(popKinds, cardKinds,
                    key + " — 카드의 «획득 가능» 종류가 세부 팝업 보상 종류와 같아야 한다(원천이 하나여야 한다)");
                Assert.AreEqual(popKinds.Count, cardKinds.Count,
                    key + " — 같은 종류가 «첫 클리어»·«이후» 로 두 번 나와도 카드에는 한 번만 그린다");

                _app.Overlay.Close(); yield return Frames(2);
            }
            _log.AssertNoRed("던전 카드 ↔ 세부 팝업 보상 종류");
            yield return Shutdown();
        }

        /// <summary>
        /// T314(주인 2026-09-09 10:1X «던전 팝업 보상 칸 2개가 양 끝으로 벌어짐 → 붙여서 가운데» · 원정 2층 스샷) —
        /// 세부 팝업(21)의 보상 칸은 <b>칸 수와 상관없이 가운데로 모인다</b>.
        /// <para>
        /// ⚠ px 을 안 박는다(결정 555). 재는 것은 <b>관계</b> 둘이다: ⓐ 묶음이 줄 가운데인가(왼쪽 여백 ≈ 오른쪽 여백)
        /// ⓑ 칸과 칸 사이가 «칸 하나» 보다 좁은가. 종전 <c>fill: true</c> 는 남는 폭을 전부 틈에 줘서
        /// <b>칸이 둘일 때 틈이 칸의 세 배</b>가 됐다 — 주인이 본 그 그림이고, 이 자가 그것을 잡는다.
        /// </para>
        /// <para>
        /// <b>넷일 때도 같이 잰다</b> — 지옥의 문(넉 장)은 레퍼런스 21 대로 줄을 꽉 채우는 자리라(T214),
        /// 이 고침이 그쪽을 좁혀 놓지 않았는지 같은 자가 함께 본다. 두 던전을 한 자에서 보는 까닭이다.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator PopupRewardCellsStayCenteredWhateverTheCount()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(3);
            var pg = UiKit.Find(_app.Current.Root, "Page:" + EventsScreen.PageDungeon);

            foreach (var key in new[] { "hell", "expedition" })
            {
                var card = UiKit.Find(pg, "Card:" + key); Assert.IsNotNull(card, key + " 카드");
                Assert.IsTrue(ClickNamed(card, "EnterBtn"), key + " 세부 팝업 열기"); yield return Frames(3);

                var rowRt = UiKit.Find(_app.Overlay.Root, "RewardCells"); Assert.IsNotNull(rowRt, key + " 보상 줄");
                var cells = Cells(_app.Overlay.Root, "RewardCell:");
                Assert.Greater(cells.Count, 0, key + " 보상 칸");
                cells.Sort((a, b) => World(a).xMin.CompareTo(World(b).xMin));

                var rowR = World((RectTransform)rowRt);
                var firstR = World(cells[0]); var lastR = World(cells[cells.Count - 1]);
                float left = firstR.xMin - rowR.xMin, right = rowR.xMax - lastR.xMax;
                Assert.AreEqual(left, right, rowR.width * 0.02f,
                    key + ": 보상 묶음이 줄 가운데다(왼쪽 " + left.ToString("0") + " ↔ 오른쪽 " + right.ToString("0") + "px)");

                for (int i = 1; i < cells.Count; i++)
                {
                    float gap = World(cells[i]).xMin - World(cells[i - 1]).xMax;
                    Assert.LessOrEqual(gap, firstR.width,
                        key + ": 칸 사이가 «칸 하나» 보다 넓으면 양 끝으로 벌어진 것이다(틈 " + gap.ToString("0") + "px · 칸 " + firstR.width.ToString("0") + "px)");
                }
                _app.Overlay.Close(); yield return Frames(2);
            }

            _log.AssertNoRed("던전 세부 보상 칸 가운데 모임(T314)");
            yield return Shutdown();
        }

        /// <summary>
        /// T251 ⓑ — 카드는 <b>«무엇이 나오나» 만</b> 말한다: 수량 글자도 «최초» 배지도 없다(그것은 팝업 몫 · T123).
        /// <para>같은 화면에서 팝업 쪽에는 <b>있다</b>는 것도 같이 잰다 — 안 그러면 «둘 다 없어진» 회귀를 이 자가 통과시킨다.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CardShowsKindsOnlyWhileThePopupKeepsAmountsAndFirstBadges()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(3);
            var pg = UiKit.Find(_app.Current.Root, "Page:" + EventsScreen.PageDungeon);
            var row = UiKit.Find(UiKit.Find(pg, "Card:hell"), "Rewards"); Assert.IsNotNull(row, "카드 보상 줄");

            foreach (var t in row.GetComponentsInChildren<TMP_Text>(true))
                Assert.Fail("카드의 보상 줄에 글자가 있으면 안 된다(수량은 세부 팝업 몫): «" + (t.text ?? "") + "»");
            foreach (var t in row.GetComponentsInChildren<Transform>(true))
                Assert.AreNotEqual("First", t.name, "카드에는 «최초» 배지가 없다");

            Assert.IsTrue(ClickNamed(UiKit.Find(pg, "Card:hell"), "EnterBtn"), "세부 팝업 열기"); yield return Frames(3);
            var cells = Cells(_app.Overlay.Root, "RewardCell:");
            Assert.Greater(cells.Count, 0, "팝업 보상 칸");
            bool amount = false, first = false;
            foreach (var c in cells)
            {
                foreach (var t in c.GetComponentsInChildren<TMP_Text>(true)) if (!string.IsNullOrEmpty(t.text) && t.text != "최초") amount = true;
                if (UiKit.Find(c, "First") != null) first = true;
            }
            Assert.IsTrue(amount, "세부 팝업에는 수량 글자가 그대로 있다(카드에서만 뺀 것이다)");
            Assert.IsTrue(first, "세부 팝업에는 «최초» 배지가 그대로 있다(T123)");

            _log.AssertNoRed("카드는 종류만 · 팝업은 수량·배지");
            yield return Shutdown();
        }

        /// <summary>
        /// T251 ⓒ — <b>개수가 달라도 칸 크기는 같고 서로 안 겹친다.</b>
        /// <para>
        /// 옛 줄은 폭을 «둘이냐 아니냐»(<c>Length == 2 ? W : 40</c>)로만 갈랐다. <see cref="EventsScreen"/> 의 칸은
        /// <b>줄 높이로 정사각</b>을 만들고 남는 폭을 틈으로 쓰므로, 개수가 그 두 갈래를 벗어나면 칸이 커지거나 붙는다 —
        /// 지금 카드는 <b>둘(지옥)과 하나(원정)</b>라 옛 규칙이었으면 원정 줄이 40% 폭에 한 칸이 됐다.
        /// 그래서 «폭 = 개수 × 한 칸» 으로 고쳤고, 이 자가 그 성질을 <b>수를 안 적고</b> 잰다(두 카드를 맞대어 본다).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator CellsKeepTheirSizeAndDoNotOverlapWhateverTheCount()
        {
            yield return Boot();
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(3);
            var pg = UiKit.Find(_app.Current.Root, "Page:" + EventsScreen.PageDungeon);

            float size = -1f; int counts = 0;
            foreach (var key in Keys)
            {
                var row = UiKit.Find(UiKit.Find(pg, "Card:" + key), "Rewards");
                var cells = Cells(row, "Cell:"); Assert.Greater(cells.Count, 0, key + " 칸");
                counts += cells.Count;
                var rects = new List<Rect>();
                foreach (var c in cells)
                {
                    var w = World(c);
                    Assert.Greater(w.width, 1f, key + " 칸이 접히지 않았다");
                    Assert.AreEqual(w.width, w.height, w.width * 0.02f, key + " 칸은 정사각이다");
                    if (size < 0) size = w.width;
                    else Assert.AreEqual(size, w.width, size * 0.02f, key + " — 개수가 달라도 칸 크기는 같다(폭 = 개수 × 한 칸)");
                    foreach (var o in rects) Assert.IsFalse(o.Overlaps(w), key + " 칸끼리 겹치면 안 된다");
                    rects.Add(w);
                }
            }
            Assert.Greater(counts, Keys.Length, "두 카드의 칸 수가 서로 달라야 이 자가 뜻이 있다(하나씩이면 «개수가 달라도» 를 안 잰 것이다)");

            _log.AssertNoRed("보상 줄 자리");
            yield return Shutdown();
        }
    }
}
