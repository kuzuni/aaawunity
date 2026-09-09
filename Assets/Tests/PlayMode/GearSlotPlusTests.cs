using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T310 — <b>장착 슬롯의 «+N» 이 인벤 칸과 같은 꼴인가</b>(주인 2026-09-09 09:2X «장착한 거는 그렇게 안 돼 있더라 · 통일시켜 아래 거랑»).
    /// <para>
    /// ⓐ 노란 알약(<c>PlusBadge</c>)은 어디에도 없다 · ⓑ 두 자리가 <b>같은 이름의 조각 글자</b>(<c>Text_Level</c>)를 쓴다 ·
    /// ⓒ 칸 안 <b>상대 자리·글자 크기가 같다</b> · ⓓ 강화 0 이면 빈 글자다.
    /// </para>
    /// ⚠ <b>수를 안 박는다</b> — «오른쪽 아래 몇 %» 를 적으면 조각이 바뀌는 날 자가 거짓말을 한다. 두 자리를 <b>재서 서로 견준다</b>(T310 2항).
    /// 그래서 이 자는 «인벤이 옳다» 가 아니라 <b>«둘이 같다»</b> 만 말한다 — 주인이 정본으로 지목한 것이 인벤 꼴이라 그것으로 충분하다.
    /// </summary>
    public class GearSlotPlusTests
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
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>
        /// «+N» 글자 띠의 <b>오른쪽 아래 모서리</b> — <b>눈에 보이는 칸</b>(<c>ItemFrame_01</c>) 기준 0~1 비율.
        /// <para>
        /// ⚠ <b>회차 2에서 부모가 아니라 «보이는 칸» 을 분모로 바꿨다.</b> 종전에는 <c>plus.parent</c> 로 나눴는데 그 사각형이 두 곳에서 다르다
        /// (인벤은 칸 뿌리 <b>188</b> · 슬롯은 프레임 <b>190</b>) — 그러면 이 비율은 «자리가 같은가» 가 아니라 <b>«부모가 같은가»</b> 를 재게 된다(워커 B · 결정 878).
        /// </para>
        /// <para>
        /// ⚠ <b>왼쪽·폭은 안 잰다.</b> 두 조각의 띠 자체가 다르게 파여 있다(프리팹 실측 <c>sizeDelta.x</c> 인벤 <b>−39.02</b> · 슬롯 <b>−29.55</b> ⇒ 왼쪽 여백 19.51 ↔ 14.78px).
        /// 그 띠는 <b>가로로 늘어난 빈 칸</b>이고 글자는 오른쪽에 붙으므로 <b>왼쪽 끝은 눈에 안 보인다</b> — 안 보이는 수를 맞추려고 조각 치수를 덮어쓰는 것은
        /// T310 이 일부러 안 하기로 한 일이다(«조각이 주는 것을 쓴다»). 그래서 눈이 보는 <b>오른쪽·아래</b>만 잰다.
        /// </para>
        /// </summary>
        static Vector2 PlusRightBottom(Transform plus, Transform frame)
        {
            var p = new Vector3[4]; ((RectTransform)plus).GetWorldCorners(p);
            var f = new Vector3[4]; ((RectTransform)frame).GetWorldCorners(f);
            float fw = f[2].x - f[0].x, fh = f[1].y - f[0].y;
            Assert.Greater(fw, 0f); Assert.Greater(fh, 0f);
            return new Vector2((p[2].x - f[0].x) / fw, (p[0].y - f[0].y) / fh);
        }
        /// <summary>그 «+N» 이 놓인 <b>눈에 보이는 칸</b> — 자기 자신이거나(슬롯) 형제다(인벤 칸). 못 찾으면 부모.</summary>
        static Transform VisibleFrame(Transform plus)
        {
            for (var t = plus; t != null; t = t.parent)
            {
                if (t.name == "ItemFrame_01") return t;
                var sib = t.parent != null ? UiKit.Find(t.parent, "ItemFrame_01") : null;
                if (sib != null) return sib;
            }
            return plus.parent;
        }

        [UnityTest]
        public IEnumerator EquippedSlotShowsPlusTheSameWayTheInventoryCellDoes()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;

            // 강화된 장비 하나를 장착하고, 인벤에도 강화된 것이 한 칸 보이게 둔다(둘을 나란히 재려면 둘 다 «+N» 이 켜져야 한다).
            var part = GearUi.ColLeft[0]; string type = D.Gear.Types[part][0];
            var worn = S.NewGear(part, type, 0, 3); S.Inv.Add(worn); S.Eq[part] = worn.Uid;
            var spare = S.NewGear(part, type, 0, 2); S.Inv.Add(spare);   // 인벤은 장착분을 숨기므로 «+N» 이 켜진 칸을 하나 더 둔다
            _app.ShowScreen("gear"); yield return Frames(2);

            var root = _app.Current.Root;
            var slots = UiKit.Find(root, "Group_Slot"); Assert.IsNotNull(slots, "장착 슬롯 묶음");
            var content = UiKit.Find(root, "Content"); Assert.IsNotNull(content, "인벤 격자");

            // ⓐ 노란 알약은 화면 어디에도 없다
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                Assert.AreNotEqual("PlusBadge", t.name, "노란 «+N» 알약이 남아 있다(T310 이 없앤 것)");

            // ⓑ 두 자리 다 조각 글자 «Text_Level» 을 쓴다
            Transform slotPlus = null;
            for (int i = 0; i < slots.childCount && slotPlus == null; i++)
            {
                var p = UiKit.Find(slots.GetChild(i), "Text_Level");
                var x = p != null ? p.GetComponent<TMP_Text>() : null;
                if (x != null && (x.text ?? "").StartsWith("+")) slotPlus = p;
            }
            Assert.IsNotNull(slotPlus, "장착 슬롯의 «+N»(조각의 Text_Level)");

            Transform cellPlus = null;
            for (int i = 0; i < content.childCount && cellPlus == null; i++)
            {
                var p = UiKit.Find(content.GetChild(i), "Text_Level");
                var x = p != null ? p.GetComponent<TMP_Text>() : null;
                if (x != null && (x.text ?? "").StartsWith("+")) cellPlus = p;
            }
            Assert.IsNotNull(cellPlus, "인벤 칸의 «+N»");

            // ⓒ-1 **글자가 같은 쪽에 붙는다** — 회차 1 이 못 닫은 자리다.
            //    두 조각의 Text_Level 은 가로 정렬이 달랐다(인벤 Right ↔ 슬롯 Center) ⇒ 슬롯의 «+N» 만 아래 «가운데» 에 떠 있었다.
            //    ⚑ 이것은 «띠가 어디 있나» 로는 안 잡힌다 — 띠는 양쪽 다 칸을 가로로 채우고, 다른 것은 그 안에서 글자가 붙는 쪽이다.
            //      회차 1 의 자가 초록이었어도 주인 눈에는 여전히 안 통일돼 보였을 자리라, 정렬을 따로 못 박는다.
            var slotTxt = slotPlus.GetComponent<TMP_Text>(); var cellTxt = cellPlus.GetComponent<TMP_Text>();
            Assert.AreEqual(HorizontalAlignmentOptions.Right, cellTxt.horizontalAlignment, "인벤 «+N» 은 오른쪽 정렬이 정본이다(주인 «오른쪽 아래에 +1»)");
            Assert.AreEqual(cellTxt.horizontalAlignment, slotTxt.horizontalAlignment, "«+N» 이 붙는 쪽이 다르다 — 슬롯만 가운데면 주인 눈에는 여전히 안 통일이다");
            Assert.AreEqual(cellTxt.verticalAlignment, slotTxt.verticalAlignment, "«+N» 의 세로 정렬이 다르다");

            // ⓒ-2 칸 안 **오른쪽 아래 자리·글자 크기**가 같다 — 둘을 재서 견준다(수를 안 박는다).
            //    ⚠ 문턱 0.04(칸 190px 의 ≈7.6px)는 일부러 넓다: 두 조각의 띠 여백이 4.7px 다르고 그것은 눈에 안 보인다.
            //      좁게 잡으면 화면이 옳은데 자가 빨개지고 다음 사람이 그 빨강을 «화면을 고치는» 일로 읽는다(결정 877).
            //      정렬이 어긋나면 글자가 칸 절반쯤 움직이므로 이 문턱으로도 그 사고는 ⓒ-1 과 함께 잡힌다.
            var a = PlusRightBottom(slotPlus, VisibleFrame(slotPlus));
            var b = PlusRightBottom(cellPlus, VisibleFrame(cellPlus));
            Assert.AreEqual(b.x, a.x, 0.04f, $"«+N» 띠의 오른쪽 끝이 다르다(슬롯 {a.x:0.000} ↔ 인벤 {b.x:0.000})");
            Assert.AreEqual(b.y, a.y, 0.04f, $"«+N» 띠의 아래 끝이 다르다(슬롯 {a.y:0.000} ↔ 인벤 {b.y:0.000})");
            // 글자 크기 — 회차 2 에서 여기가 빨갰다(런 783 · 인벤 **40** ↔ 슬롯 **32**): 두 조각이 서로 다른 `fontSizeMax`(28 ↔ 32)를 들고 오고
            //   자동 크기가 그 위에서 각자 답을 냈다. 회차 3 이 `SetPlus` 에서 위아래 문턱을 같게 주므로 이제 같은 글자에 같은 크기가 나온다.
            //   ⚠ 문턱 3 은 일부러 넓다 — 띠 폭·높이가 조금 달라(148.98×46.37 ↔ 160.45×50) 자동 크기가 한두 단계 어긋날 여지가 있고,
            //     못 돌려 보고 넣는 값은 «맞히려» 하지 말고 «틀려도 안전한 쪽» 으로 잡는다(결정 877). 잡으려는 어긋남(40 ↔ 32 = 8)은 그대로 잡힌다.
            Assert.AreEqual(cellTxt.fontSize, slotTxt.fontSize, 3f, $"«+N» 글자 크기가 다르다(슬롯 {slotTxt.fontSize:0.0} ↔ 인벤 {cellTxt.fontSize:0.0})");

            _log.AssertNoRed("장비(강화 표시)");
            yield return Shutdown();
        }

        /// <summary>ⓓ 강화 0 이면 빈 글자다 — 알약 시절에는 «배지를 끄는» 일이었고 지금은 «글자를 비우는» 일이다.</summary>
        [UnityTest]
        public IEnumerator PlusZeroLeavesTheLabelEmpty()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            string part0 = GearUi.ColLeft[0];
            var g = S.NewGear(part0, D.Gear.Types[part0][0], 0, 0); S.Inv.Add(g); S.Eq[part0] = g.Uid;
            _app.ShowScreen("gear"); yield return Frames(2);

            var slots = UiKit.Find(_app.Current.Root, "Group_Slot");
            for (int i = 0; i < slots.childCount; i++)
            {
                var p = UiKit.Find(slots.GetChild(i), "Text_Level");
                var x = p != null ? p.GetComponent<TMP_Text>() : null;
                if (x != null) Assert.IsEmpty((x.text ?? "").Trim(), "강화 0 인데 «+N» 이 적혀 있다(슬롯 " + i + ")");
            }
            _log.AssertNoRed("장비(강화 0)");
            yield return Shutdown();
        }
    }
}
