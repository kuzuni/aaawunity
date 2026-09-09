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

        /// <summary>조각 안에서 «+N» 글자가 차지하는 자리 — 칸(부모 프레임) 기준 0~1 비율로 돌려준다(크기가 달라도 견줄 수 있게).</summary>
        static Rect PlusInFrame(Transform plus)
        {
            var prt = (RectTransform)plus; var frame = (RectTransform)plus.parent;
            var p = new Vector3[4]; prt.GetWorldCorners(p);
            var f = new Vector3[4]; frame.GetWorldCorners(f);
            float fw = f[2].x - f[0].x, fh = f[1].y - f[0].y;
            Assert.Greater(fw, 0f); Assert.Greater(fh, 0f);
            return new Rect((p[0].x - f[0].x) / fw, (p[0].y - f[0].y) / fh, (p[2].x - p[0].x) / fw, (p[1].y - p[0].y) / fh);
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

            // ⓒ 칸 안 상대 자리·글자 크기가 같다 — 둘을 재서 견준다(수를 안 박는다)
            var a = PlusInFrame(slotPlus); var b = PlusInFrame(cellPlus);
            Assert.AreEqual(b.x, a.x, 0.02f, $"«+N» 가로 자리가 다르다(슬롯 {a.x:0.000} ↔ 인벤 {b.x:0.000})");
            Assert.AreEqual(b.y, a.y, 0.02f, $"«+N» 세로 자리가 다르다(슬롯 {a.y:0.000} ↔ 인벤 {b.y:0.000})");
            Assert.AreEqual(b.width, a.width, 0.02f, "«+N» 칸 폭 비율이 다르다");
            Assert.AreEqual(b.height, a.height, 0.02f, "«+N» 칸 높이 비율이 다르다");
            Assert.AreEqual(cellPlus.GetComponent<TMP_Text>().fontSize, slotPlus.GetComponent<TMP_Text>().fontSize, 0.5f, "«+N» 글자 크기가 다르다");

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
