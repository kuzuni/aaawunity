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

        /// <summary>
        /// T275 ⓑ — <b>«17/10» 이 버튼 칸을 넘치는가</b> 를 <b>재기만</b> 한다(막지 않는다 · 결정 741 ③).
        /// <para>
        /// 이 물음은 <c>screens</c> PNG 로 못 닫는다 — 찍는 판은 <b>새 세이브</b>라 버튼에 언제나 «0/0» 만 뜬다.
        /// 글자가 가장 길어지는 «보유 ≥ 캡» 은 사진에 아예 안 나온다. 그래서 그 판을 자가 만들어 <b>줄의 선호 폭 ↔ 버튼 폭</b>을 재고,
        /// 초록 런의 <c>Debug.Log</c> 는 워커가 읽을 수 없으므로(결정 289 · T246) <c>ui-screens/t275.json</c> 으로 내보낸다.
        /// </para>
        /// <b>단언은 없다</b> — 넘치는지 «몰라서» 재는 자리다. 다음 회차가 이 수를 보고 넓힐지 정한다(넓히면 그 회차에 자를 세운다).
        /// </summary>
        [UnityTest]
        public IEnumerator KeyButtonWidthAtTheLongestTextIsOnlyMeasured()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            int cap = D.Gacha.TenPullCount;

            // 글자가 가장 길어지는 판 — 상자마다 «보유 = 캡 + 7»(주인 예의 17 꼴 · «17/10» 다섯 자)
            S.KeyBlue = S.KeyPurple = S.KeyYellow = cap + 7;
            _app.ShowScreen("shop"); yield return Frames(2);
            var content = UiKit.Find(_app.Current.Root, "Content");

            var sb = new System.Text.StringBuilder();
            sb.Append("{\"_meta\":{\"task\":\"T275\",\"note\":\"글자가 가장 길 때의 키 버튼 폭 — 재기만 한다\"},\"cap\":").Append(cap).Append(",\"boxes\":[");
            bool first = true;
            foreach (var box in D.Gacha.Boxes)
            {
                if (GachaKeys.KeyOf(box.Key) == null) continue;
                var card = UiKit.Find(content, "Box:" + box.Key); if (card == null) continue;
                var key = UiKit.Find(card, "Key"); if (key == null) continue;
                var row = UiKit.Find(key, "Price"); if (row == null) continue;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)row);

                float btnW = ((RectTransform)key).rect.width;
                float needW = LayoutUtility.GetPreferredWidth((RectTransform)row);
                string txt = CountOf(key);
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"box\":\"").Append(box.Key).Append("\",\"text\":\"").Append(txt)
                  .Append("\",\"btnW\":").Append(btnW.ToString("0.0"))
                  .Append(",\"needW\":").Append(needW.ToString("0.0"))
                  .Append(",\"overPx\":").Append((needW - btnW).ToString("0.0")).Append('}');
                Debug.Log($"[T275] {box.Key} «{txt}» 버튼 {btnW:0.0} · 줄 선호 {needW:0.0} · 넘침 {needW - btnW:+0.0;-0.0;0}");
            }
            sb.Append("]}");
            foreach (var dir in PlayShot.Dirs())
            {
                try { System.IO.Directory.CreateDirectory(dir); System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "t275.json"), sb.ToString()); }
                catch (Exception e) { Debug.LogWarning("[T275] t275.json 저장 실패(" + dir + "): " + e.Message); }
            }
            _log.AssertNoRed("키 버튼 폭 재기");
            yield return Shutdown();
        }
    }
}
