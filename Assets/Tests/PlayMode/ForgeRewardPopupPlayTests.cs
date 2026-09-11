using System;
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
    /// T470 — <b>대장간 합성 결과도 리워드 팝업(35)으로</b>(주인 2026-09-12 «장비 합성할 때도 리워드 팝업 떠야 함»).
    /// <para>
    /// 실제 씬에서 재료 3개를 골라 «합성» 을 누르고 ⓐ 리워드 팝업이 <b>칸 하나</b>로 뜨는가 ⓑ 그 칸이 <b>그 장비</b>인가(아이콘 키 · 등급색 프레임 · «+N»)
    /// ⓒ 닫으면 <b>구슬이 한 개도 안 나는가</b>(재화가 아니라 갈 곳이 없다 · <see cref="RewardPopup.Item.NoOrb"/>) ⓓ 닫으면 대장간이 그대로 서 있고 산출물이 인벤에 있는가 ⓔ 빨간 줄 0.
    /// </para>
    /// <para>
    /// ⚑ <b>«팝업이 떴다» 만 재면 이 절이 반만 선다</b> — 여태 이 집안의 고장은 전부 «떴는데 다른 것이 떴다» 였다
    /// (T473 주인 «흡수 이펙트 아이콘이 칸이랑 다른 게 뜨더라» · 결정 1259 «구슬이 백 배 적다»). 그래서 <b>칸의 세 조각을 값으로</b> 맞댄다.
    /// </para>
    /// </summary>
    public class ForgeRewardPopupPlayTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; Assert.IsNotNull(_app.Assets, "AssetCatalog");
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
                foreach (var t in b.GetComponentsInChildren<TMP_Text>(false))
                    if (label(t.text ?? "")) { b.onClick.Invoke(); return true; }
            return false;
        }
        static bool ClickNamed(Transform root, string name) { var t = UiKit.Find(root, name); var b = t != null ? t.GetComponent<Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        static bool CloseOverlay(Overlay ov)
        {
            var dim = UiKit.Find(ov.Root, "Dimmed");
            var b = dim != null ? dim.GetComponent<Button>() : null;
            if (b == null) return false;
            b.onClick.Invoke(); return true;
        }
        GearItem Give(string part, int rar = 0)
        {
            foreach (var t in _app.Data.Gear.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, rar, 0); _app.Save.Inv.Add(g); return g; }
            Assert.Fail("gear.json 에 부위가 없다: " + part); return null;
        }

        [UnityTest]
        public IEnumerator 합성하면_리워드_팝업이_그_장비_한_칸으로_뜨고_구슬은_안_난다()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            string part = D.Gear.Parts[0];
            var a = Give(part); var b = Give(part); var c = Give(part);

            _app.ShowScreen("forge"); yield return Frames(2);
            Assert.AreEqual("forge", _app.Current.Name);
            var forge = _app.Current.Root; var content = UiKit.Find(forge, "Content"); Assert.IsNotNull(content, "대장간 인벤 Content");
            foreach (var g in new[] { a, b, c }) { Assert.IsTrue(ClickNamed(content, "gear:" + g.Uid), "재료 칸 " + g.Uid); yield return Frames(1); }

            int before = S.Inv.Count;
            Assert.IsTrue(Click(forge, s => s == "합성 (3/3)"), "합성 버튼"); yield return Frames(3);
            Assert.AreEqual(before - 2, S.Inv.Count, "3개 → 1개");

            // ⓐ 팝업이 칸 하나로 떴다
            Assert.AreEqual(1, RewardPopup.LastCellCount, "합성 결과는 리워드 팝업 한 칸으로 뜬다(T470 · 주인 «장비 합성할 때도 리워드 팝업 떠야 함»)");
            var made = S.Inv[S.Inv.Count - 1];
            Assert.AreEqual(a.Rar + 1, made.Rar, "산출물은 한 등급 위 — 아래 칸 비교가 뜻을 가지려면 이것이 먼저 참이어야 한다");

            // ⓑ 그 칸이 «그 장비» 인가 — 세 조각을 값으로 맞댄다
            var ov = _app.Overlay; Assert.IsNotNull(ov, "오버레이");
            var cell = UiKit.Find(ov.Root, "RewardCell:0");
            Assert.IsNotNull(cell, "팝업 첫 칸(`RewardCell:0`) — 못 찾으면 아래가 다 뜻이 없다");
            var iconT = UiKit.Find(cell, "Icon");
            var iconImg = iconT != null ? iconT.GetComponent<Image>() : null;
            Assert.IsNotNull(iconImg, "칸 안 아이콘");
            string iconKey = GearLook.IconKey(D, made);
            var want = _app.Assets.Sprite(iconKey);
            Assert.IsNotNull(want, "그 장비의 아이콘 키가 카탈로그에 있어야 한다: " + iconKey);
            Assert.AreSame(want, iconImg.sprite, "팝업 칸 그림 = 그 장비의 부위 아이콘(«떴는데 다른 것이 떴다» 가 이 집안의 단골이다 · T473)");

            string frameKey = "ui.itemFrame." + GearUi.FrameColor(D, made);
            Assert.IsNotNull(_app.Assets.Prefab(frameKey), "칸 테두리로 고른 등급색 조각이 카탈로그에 있어야 한다: " + frameKey
                + " — 없는 색을 글자로 조립해 넘기면 부팅이 운다(GearUi.FrameColor 의 그 자리 · CI #66)");
            var qtyT = UiKit.Find(cell, "Qty");
            string wantQty = GearUi.PlusText(D, made).Trim();
            if (wantQty.Length > 0)
                Assert.AreEqual(wantQty, (qtyT != null ? qtyT.GetComponent<TMP_Text>() : null)?.text,
                    "강화 수치가 있으면 칸 글자는 «+N» 이다(앞 빈칸 없이 — PlusText 는 이름 옆에 붙이려고 빈칸을 둔다)");

            // ⓒ 닫으면 구슬이 한 개도 안 난다 — 장비는 갈 pill 이 없어 가운데 아래 빈 자리로 빨려 들면 안 된다
            Assert.IsTrue(CloseOverlay(ov), "어둠을 눌러 닫는다"); yield return Frames(3);
            Assert.AreEqual(0, RewardPopup.LastOrbCount, "장비 칸은 흡수 구슬을 안 낸다(RewardPopup.Item.NoOrb · T470) — 나면 화면 가운데 아래의 빈 자리로 날아간다");

            // ⓓ 닫으면 대장간 그대로 · 산출물이 인벤에
            Assert.AreEqual("forge", _app.Current.Name, "닫으면 대장간으로 돌아온다");
            Assert.IsNotNull(S.InvById(made.Uid), "산출물이 인벤에 있다");
            _log.AssertNoRed("합성 → 리워드 팝업 → 닫기");
            yield return Shutdown();
        }
    }
}
