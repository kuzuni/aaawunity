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
    /// T255 3항 — 상자 카드의 <b>«키로 열기»</b> 버튼(주인 2026-09-09 «희귀 상자는 파란색 키로 1회 뽑기 가능 …»).
    /// <para>
    /// 규칙 자체는 EditMode <c>GachaKeysTests</c> 가 못 박고, 여기서는 <b>화면</b>만 본다:
    /// ⓐ 상자 카드 셋에 저마다 키 버튼이 있고 열쇠 아이콘을 갖는다 ⓑ 글자가 <b>«보유/쓸 개수»</b> 꼴이다(0 이면 «0/0» · 비활성)
    /// ⓒ 키를 넣고 다시 열면 활성이고 개수가 맞는다 ⓓ 누르면 <b>키만</b> 줄고 다이아는 그대로이며 뽑기 결과 창이 뜬다 ⓔ 빨간 줄 0.
    /// </para>
    /// <para>
    /// <b>T275 — «가진 만큼 한 번에»(캡 10)</b>(주인 2026-09-08 «17개면 17/10 이 돼서 누르면 10회 뽑고 7/7 이 되고»).
    /// 주인이 든 그 예를 <see cref="SeventeenKeysPullTenAtOnceAndThenReadSevenBySeven"/> 가 그대로 잰다 — 캡은 <b>표 값</b>이라 자도 표에서 읽는다(코드에 10 을 안 박는다).
    /// </para>
    /// ⚠ <b>«키가 여는 상자» 짝은 여기서 안 정한다</b> — <see cref="GachaKeys"/> 한 곳이 갖고 이 자는 그 절에 물어본다.
    /// 두 곳에 같은 표를 적으면 한쪽만 고쳐질 때 자가 거짓말을 한다(결정 704).
    /// </summary>
    public class ShopKeyPlayTests
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

        static string CountOf(Transform keyBtn)
        {
            var t = keyBtn != null ? UiKit.Find(keyBtn, "Cost") : null;
            var x = t != null ? t.GetComponent<TMP_Text>() : null;
            return x != null ? (x.text ?? "").Trim() : null;
        }
        static Button ButtonOf(Transform t) => t != null ? t.GetComponent<Button>() : null;

        [UnityTest]
        public IEnumerator EveryBoxCardHasAKeyButtonThatShowsHowManyYouHave()
        {
            yield return Boot();
            var D = _app.Data;

            _app.ShowScreen("shop"); yield return Frames(2);
            var content = UiKit.Find(_app.Current.Root, "Content");
            Assert.IsNotNull(content, "상점 Content");

            foreach (var box in D.Gacha.Boxes)
            {
                string item = GachaKeys.KeyOf(box.Key);
                var card = UiKit.Find(content, "Box:" + box.Key); Assert.IsNotNull(card, "상자 카드 " + box.Key);
                var key = UiKit.Find(card, "Key");
                if (item == null) { Assert.IsNull(key, "여는 키가 없는 상자에는 키 버튼도 없다 " + box.Key); continue; }

                Assert.IsNotNull(key, "«키로 열기» 버튼 " + box.Key);
                Assert.IsNotNull(UiKit.Find(key, "KeyIcon"), "키 버튼 안 열쇠 아이콘 " + box.Key);
                Assert.AreEqual("0/0", CountOf(key), "가진 키가 0 이면 «0/0»(T275 — 글자는 «보유/쓸 개수») " + box.Key);
                Assert.IsFalse(ButtonOf(key).interactable, "0개면 비활성 " + box.Key);
            }
            // T275 — 0개일 때 눌러도(비활성이라 손으로는 못 누르지만 자는 부를 수 있다) «없습니다» 로 물러설 뿐 한 판도 안 나간다.
            {
                var key0 = UiKit.Find(UiKit.Find(content, "Box:" + GachaKeys.BoxOf(GachaKeys.Blue)), "Key");
                int pulls0 = _app.Save.Pulls;
                ButtonOf(key0).onClick.Invoke(); yield return Frames(2);
                Assert.AreEqual(pulls0, _app.Save.Pulls, "키가 0 이면 뽑지 않는다(T255 의 토스트 자리)");
                Assert.IsFalse(_app.Overlay.IsOpen, "결과 창도 안 뜬다");
            }
            _log.AssertNoRed("상점(키 0개)");
            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator PressingTheKeyButtonSpendsOneKeyAndNoGems()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;

            // 파란 키 두 개 — 주인이 정한 짝대로 «희귀 상자» 가 열려야 한다(짝은 GachaKeys 가 갖는다).
            string boxKey = GachaKeys.BoxOf(GachaKeys.Blue);
            S.KeyBlue = 2; S.Gem = 0;
            _app.ShowScreen("shop"); yield return Frames(2);
            var content = UiKit.Find(_app.Current.Root, "Content");
            var card = UiKit.Find(content, "Box:" + boxKey); Assert.IsNotNull(card, "희귀 상자 카드");
            var key = UiKit.Find(card, "Key"); Assert.IsNotNull(key, "키 버튼");

            // T275 — 캡(10)보다 적게 가졌으므로 «2/2» 이고, 누르면 두 개를 한 판에 다 쓴다(주인 «있는 열쇠 다 써서»).
            Assert.AreEqual("2/2", CountOf(key), "보유/쓸 개수 — 캡보다 적으면 둘이 같다");
            Assert.IsTrue(ButtonOf(key).interactable, "키가 있으면 활성 — 다이아가 0 이어도 눌린다");

            int pulls = S.Pulls; int inv = S.Inv.Count;
            ButtonOf(key).onClick.Invoke(); yield return Frames(2);

            Assert.AreEqual(0, S.KeyBlue, "가진 둘을 한 번에 쓴다(T275 · T255 때는 한 개였다)");
            Assert.AreEqual(0, S.Gem, 1e-9, "다이아는 한 톨도 안 빠진다");
            Assert.AreEqual(pulls + 2, S.Pulls, "쓴 개수만큼 뽑는다 — 엔진은 다이아로 연 것과 같은 경로다");
            Assert.Greater(S.Inv.Count, inv, "장비를 얻는다");
            Assert.IsTrue(_app.Overlay.IsOpen, "뽑기 결과 창이 뜬다 — 한 판이라 창도 하나다");
            _log.AssertNoRed("키로 뽑기");

            _app.Overlay.Close(); yield return Frames(1);
            yield return Shutdown();
        }

        /// <summary>
        /// T275 — <b>주인이 든 예 그대로</b>: 열쇠 17개면 «17/10» 이고, 누르면 <b>10회를 한 판에</b> 뽑고 남은 7 로 «7/7» 이 된다.
        /// <para>캡은 <c>gacha.json</c> 의 <c>tenPullCount</c> 다 — 자도 표에서 읽는다(표가 바뀌면 자가 같이 움직인다 · 코드에 10 을 안 박는다).</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SeventeenKeysPullTenAtOnceAndThenReadSevenBySeven()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            int cap = D.Gacha.TenPullCount;
            Assert.Greater(cap, 1, "표의 «N회» 값이 캡이다");

            string boxKey = GachaKeys.BoxOf(GachaKeys.Blue);
            int have = cap + 7;                       // 주인 예의 17 = 캡 10 + 7
            S.KeyBlue = have; S.Gem = 0;
            _app.ShowScreen("shop"); yield return Frames(2);
            var content = UiKit.Find(_app.Current.Root, "Content");
            var card = UiKit.Find(content, "Box:" + boxKey);
            var key = UiKit.Find(card, "Key"); Assert.IsNotNull(key, "키 버튼");

            Assert.AreEqual(have + "/" + cap, CountOf(key), "가진 것이 캡보다 많으면 «보유/캡»");

            int pulls = S.Pulls;
            ButtonOf(key).onClick.Invoke(); yield return Frames(2);

            Assert.AreEqual(have - cap, S.KeyBlue, "캡만큼만 빠진다");
            Assert.AreEqual(pulls + cap, S.Pulls, "한 번 눌러 캡 회 — 한 판이다");
            Assert.IsTrue(_app.Overlay.IsOpen, "결과 창은 하나(1회씩 여러 번 부르면 창이 여러 번 뜬다)");
            _app.Overlay.Close(); yield return Frames(1);

            // 뽑은 뒤 글자가 «바로» 새로 그려져야 한다(T275 4항) — 화면을 다시 열지 않고 그대로 읽는다.
            Assert.AreEqual((have - cap) + "/" + (have - cap), CountOf(key), "남은 7 이면 «7/7»");
            _log.AssertNoRed("키로 캡 회 뽑기");
            yield return Shutdown();
        }
    }
}
