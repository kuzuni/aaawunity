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
    /// T289 — <b>열쇠 버튼은 없다. 1회·10회 버튼이 «열쇠 모드» 로 갈아입는다</b>(주인 2026-09-09 05:4X
    /// «열쇠 버튼은 없어야 하고 … 10개 미만이면 1회 버튼만 · 10개 이상이면 둘 다 · 이유는 열쇠 먼저 소진시키려고»).
    /// <para>
    /// 규칙(캡·소진·«가진 만큼 한 판»)은 EditMode <c>GachaKeysTests</c> 가 못 박고, 여기서는 <b>화면이 어느 옷을 입는가</b>만 본다:
    /// ⓐ K=0 둘 다 다이아 · ⓑ K=7 1회만 열쇠(«7회» · «7/7») · ⓒ K=17 둘 다 열쇠(«17/10») → 누르면 K=7 이 되어 10회 자리는 다이아로 돌아온다
    /// · ⓓ 작은 카드는 1회 자리 하나가 갈아입고 옛 «🔑 0/0» 칸은 없다 · ⓔ 다이아가 0 이어도 열쇠 옷은 눌린다.
    /// </para>
    /// <para>
    /// <b>T275 의 자를 지우지 않고 뜻만 옮겼다</b>(T259 3회차 방식) — 그 회차가 «17/10» 다섯 자가 칸에 드는지를 수로 재고 있었고,
    /// 그 물음은 버튼이 옮겨졌을 뿐 그대로 살아 있다(이제 재는 대상이 «Key» 가 아니라 «OneKey»·«TenKey» 다).
    /// </para>
    /// ⚠ <b>«키가 여는 상자» 짝은 여기서 안 정한다</b> — <see cref="GachaKeys"/> 한 곳이 갖고 이 자는 그 절에 물어본다(결정 704).
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

        static string CountOf(Transform btn)
        {
            var t = btn != null ? UiKit.Find(btn, "Cost") : null;
            var x = t != null ? t.GetComponent<TMP_Text>() : null;
            return x != null ? (x.text ?? "").Trim() : null;
        }
        static string LabelOf(Transform btn)
        {
            var t = btn != null ? UiKit.Find(btn, "Label") : null;
            var x = t != null ? t.GetComponent<TMP_Text>() : null;
            return x != null ? (x.text ?? "").Trim() : null;
        }
        static Button ButtonOf(Transform t) => t != null ? t.GetComponent<Button>() : null;
        /// <summary>켜져 있는 조각만 돌려준다 — 옷 두 벌이 같은 rect 에 겹쳐 있으므로 «있다» 가 아니라 «켜졌다» 를 물어야 한다.</summary>
        static Transform Live(Transform card, string name)
        {
            var t = UiKit.Find(card, name);
            return t != null && t.gameObject.activeInHierarchy ? t : null;
        }
        Transform Card(string boxKey)
        {
            var content = UiKit.Find(_app.Current.Root, "Content");
            Assert.IsNotNull(content, "상점 Content");
            var card = UiKit.Find(content, "Box:" + boxKey);
            Assert.IsNotNull(card, "상자 카드 " + boxKey);
            return card;
        }

        /// <summary>ⓐ 열쇠가 0 이면 옛 화면 그대로 — 두 자리 다 다이아이고, 열쇠 옷은 어디에도 안 켜져 있다.</summary>
        [UnityTest]
        public IEnumerator WithNoKeysBothButtonsStayOnGems()
        {
            yield return Boot();
            _app.ShowScreen("shop"); yield return Frames(2);

            foreach (var box in _app.Data.Gacha.Boxes)
            {
                var card = Card(box.Key);
                Assert.IsNotNull(Live(card, "One"), "열쇠가 없으면 1회 자리는 다이아다 " + box.Key);
                Assert.IsNull(Live(card, "OneKey"), "열쇠 옷은 안 켜진다 " + box.Key);
                Assert.IsNull(Live(card, "TenKey"), "10회 열쇠 옷도 안 켜진다 " + box.Key);
                // T289 — 옛 «🔑 0/0» 칸은 아예 없다(주인 «열쇠 버튼은 없어야 하고»).
                Assert.IsNull(UiKit.Find(card, "Key"), "따로 선 열쇠 버튼은 지웠다 " + box.Key);
            }
            _log.AssertNoRed("상점(열쇠 0)");
            yield return Shutdown();
        }

        /// <summary>ⓑ·ⓔ 캡보다 적게 가지면 <b>1회 자리만</b> 열쇠가 되고, 누르면 가진 것을 다 쓴다 — 다이아가 0 이어도 눌린다.</summary>
        [UnityTest]
        public IEnumerator BelowTheCapOnlyTheOnePullButtonWearsTheKey()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            int cap = D.Gacha.TenPullCount; Assert.Greater(cap, 1, "표의 «N회» 값이 캡이다");
            string boxKey = ShopScreen.BigBox(D).Key;          // 10회 버튼이 있는 카드라야 «둘 중 하나만» 을 잴 수 있다
            string item = GachaKeys.KeyOf(boxKey); Assert.IsNotNull(item, "그 상자를 여는 키");

            int have = cap - 3;                                 // 캡 10 이면 7 — 주인 예의 «10개 미만»
            GachaKeys.Add(S, item, have); S.Gem = 0;
            _app.ShowScreen("shop"); yield return Frames(2);
            var card = Card(boxKey);

            var oneKey = Live(card, "OneKey");
            Assert.IsNotNull(oneKey, "10개 미만이면 1회 자리가 열쇠 옷을 입는다");
            Assert.IsNull(Live(card, "One"), "그 자리의 다이아 옷은 꺼진다(둘이 겹쳐 보이면 안 된다)");
            Assert.IsNotNull(Live(card, "Ten"), "10회 자리는 다이아 그대로");
            Assert.IsNull(Live(card, "TenKey"), "10개 미만이면 10회 자리는 열쇠가 아니다");
            Assert.AreEqual(have + "/" + have, CountOf(oneKey), "가진 것을 다 쓴다 — «보유/쓸 개수»");
            Assert.AreEqual(have + "회", LabelOf(oneKey), "윗줄도 «쓸 개수»회 여야 한다(«1회» 면 거짓말이다)");
            Assert.IsTrue(ButtonOf(oneKey).interactable, "다이아가 0 이어도 열쇠 옷은 눌린다 — 값을 열쇠로 치른다");

            int pulls = S.Pulls;
            ButtonOf(oneKey).onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(0, GachaKeys.Count(S, item), 1e-9, "가진 열쇠를 한 판에 다 쓴다");
            Assert.AreEqual(0, S.Gem, 1e-9, "다이아는 한 톨도 안 빠진다");
            Assert.AreEqual(pulls + have, S.Pulls, "쓴 개수만큼 뽑는다");
            Assert.IsTrue(_app.Overlay.IsOpen, "한 판이라 결과 창도 하나");
            _app.Overlay.Close(); yield return Frames(1);
            Assert.IsNotNull(Live(card, "One"), "다 쓰고 나면 다시 다이아 옷으로 돌아온다");
            _log.AssertNoRed("1회 자리 열쇠 모드");
            yield return Shutdown();
        }

        /// <summary>
        /// ⓒ 주인이 든 예 그대로 — 열쇠 17개면 <b>두 자리 다</b> 열쇠 «17/10» 이고, 누르면 캡만큼 한 판에 나가 7 이 남는다.
        /// <para>그때 10회 자리는 다이아로 돌아오고 1회 자리만 «7/7» 로 남는다(T289 1항 표의 마지막 줄).</para>
        /// <para>캡은 <c>gacha.json</c> 의 <c>tenPull.count</c> 다 — 자도 표에서 읽는다(코드에 10 을 안 박는다).</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SeventeenKeysDressBothButtonsAndThenFallBackToSevenBySeven()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            int cap = D.Gacha.TenPullCount;
            string boxKey = ShopScreen.BigBox(D).Key;
            string item = GachaKeys.KeyOf(boxKey);

            int have = cap + 7;                                  // 주인 예의 17 = 캡 10 + 7
            GachaKeys.Add(S, item, have); S.Gem = 0;
            _app.ShowScreen("shop"); yield return Frames(2);
            var card = Card(boxKey);

            var oneKey = Live(card, "OneKey"); var tenKey = Live(card, "TenKey");
            Assert.IsNotNull(oneKey, "10개 이상이면 1회 자리도 열쇠");
            Assert.IsNotNull(tenKey, "10개 이상이면 10회 자리도 열쇠 — 주인이 그렇게 시켰다(다이아 길을 막는다)");
            Assert.IsNull(Live(card, "Ten"), "그동안 다이아 10회는 꺼진다");
            Assert.AreEqual(have + "/" + cap, CountOf(tenKey), "캡보다 많으면 «보유/캡»");
            Assert.AreEqual(have + "/" + cap, CountOf(oneKey), "이 판에서는 1회 자리도 같은 일을 한다(같은 글자)");

            int pulls = S.Pulls;
            ButtonOf(tenKey).onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(have - cap, GachaKeys.Count(S, item), 1e-9, "캡만큼만 빠진다");
            Assert.AreEqual(pulls + cap, S.Pulls, "한 번 눌러 캡 회 — 한 판이다");
            Assert.IsTrue(_app.Overlay.IsOpen, "결과 창은 하나");
            _app.Overlay.Close(); yield return Frames(1);

            // 화면을 다시 열지 않고 그대로 읽는다 — Pull 이 Refresh 를 부르므로 갈아입기는 저절로 된다(T289 2항).
            Assert.IsNull(Live(card, "TenKey"), "7 이 남으면 10회 자리는 다이아로 돌아온다");
            Assert.IsNotNull(Live(card, "Ten"), "그 자리에 다이아 옷이 다시 켜진다");
            var oneAfter = Live(card, "OneKey");
            Assert.IsNotNull(oneAfter, "1회 자리는 아직 열쇠");
            Assert.AreEqual((have - cap) + "/" + (have - cap), CountOf(oneAfter), "남은 7 이면 «7/7»");
            _log.AssertNoRed("열쇠 캡 회 뽑기");
            yield return Shutdown();
        }

        /// <summary>ⓓ 작은 카드(10회 버튼이 없다) — 1회 자리 하나가 갈아입고, 옛 «🔑 0/0» 칸은 사라졌다.</summary>
        [UnityTest]
        public IEnumerator SmallCardSwapsItsOnlyButtonAndHasNoLeftoverKeySlot()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            string big = ShopScreen.BigBox(D).Key;
            string boxKey = null;
            foreach (var b in D.Gacha.Boxes) if (b.Key != big && GachaKeys.KeyOf(b.Key) != null) { boxKey = b.Key; break; }
            Assert.IsNotNull(boxKey, "열쇠로 여는 작은 카드가 하나는 있다");

            GachaKeys.Add(S, GachaKeys.KeyOf(boxKey), 3); S.Gem = 0;
            _app.ShowScreen("shop"); yield return Frames(2);
            var card = Card(boxKey);

            var oneKey = Live(card, "OneKey");
            Assert.IsNotNull(oneKey, "작은 카드도 1회 자리가 열쇠 옷을 입는다");
            Assert.AreEqual("3/3", CountOf(oneKey), "가진 것을 다 쓴다");
            Assert.IsNull(UiKit.Find(card, "Ten"), "작은 카드에는 10회 자리가 없다");
            Assert.IsNull(UiKit.Find(card, "Key"), "옛 «🔑 0/0» 칸은 지웠다(주인 «열쇠 버튼은 없어야 하고»)");
            Assert.IsNotNull(UiKit.Find(card, "Ad"), "광고 버튼은 그대로 — 이 줄은 이제 둘이다");
            _log.AssertNoRed("작은 카드 열쇠 모드");
            yield return Shutdown();
        }

        /// <summary>
        /// T275 ⓑ에서 옮겨 온 자 — <b>글자가 가장 길 때(«17/10») 그 칸이 줄을 담는가</b>.
        /// <para>
        /// 이 물음은 <c>screens</c> PNG 로 못 닫는다: 찍는 판은 <b>새 세이브</b>라 언제나 열쇠 0 이고(결정 753) 이제는 열쇠 옷이 아예 안 켜진다.
        /// 그래서 자가 그 판을 만들어 <b>줄의 선호 폭 ↔ 버튼 폭</b>을 재고, 수를 <c>ui-screens/t275.json</c> 으로도 내보낸다(빨개졌을 때 «얼마나» 를 그 파일이 말한다 · 결정 289·T246).
        /// </para>
        /// <para><b>T289 로 재는 대상이 바뀌었다</b> — 옛 «Key» 칸이 아니라 갈아입은 «OneKey»·«TenKey» 다. 칸이 T255 이전 폭(46%·42%)으로 넓어져 여유가 더 크다.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator KeyModeButtonsAreWideEnoughForTheLongestText()
        {
            yield return Boot();
            var D = _app.Data; var S = _app.Save;
            int cap = D.Gacha.TenPullCount;

            // 글자가 가장 길어지는 판 — 상자마다 «보유 = 캡 + 7»(주인 예의 17 꼴 · «17/10» 다섯 자)
            S.KeyBlue = S.KeyPurple = S.KeyYellow = cap + 7;
            _app.ShowScreen("shop"); yield return Frames(2);
            var content = UiKit.Find(_app.Current.Root, "Content");

            var sb = new System.Text.StringBuilder();
            sb.Append("{\"_meta\":{\"task\":\"T289(T275 에서 옮김)\",\"note\":\"글자가 가장 길 때 열쇠 옷 칸 폭 — 넘치면 빨갛다\"},\"cap\":").Append(cap).Append(",\"boxes\":[");
            var over = new List<string>();
            bool first = true;
            foreach (var box in D.Gacha.Boxes)
            {
                if (GachaKeys.KeyOf(box.Key) == null) continue;
                var card = UiKit.Find(content, "Box:" + box.Key); if (card == null) continue;
                foreach (var name in new[] { "OneKey", "TenKey" })
                {
                    var key = Live(card, name); if (key == null) continue;
                    var row = UiKit.Find(key, "Price"); if (row == null) continue;
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)row);
                    float btnW = ((RectTransform)key).rect.width;
                    float needW = LayoutUtility.GetPreferredWidth((RectTransform)row);
                    string txt = CountOf(key);
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append("{\"box\":\"").Append(box.Key).Append("\",\"btn\":\"").Append(name).Append("\",\"text\":\"").Append(txt)
                      .Append("\",\"btnW\":").Append(btnW.ToString("0.0"))
                      .Append(",\"needW\":").Append(needW.ToString("0.0"))
                      .Append(",\"overPx\":").Append((needW - btnW).ToString("0.0")).Append('}');
                    Debug.Log($"[T289] {box.Key}/{name} «{txt}» 버튼 {btnW:0.0} · 줄 선호 {needW:0.0} · 넘침 {needW - btnW:+0.0;-0.0;0}");
                    if (needW > btnW) over.Add($"{box.Key}/{name} «{txt}» 버튼 {btnW:0.0} < 줄 {needW:0.0}(넘침 {needW - btnW:0.0}px)");
                }
            }
            sb.Append("]}");
            foreach (var dir in PlayShot.Dirs())
            {
                try { System.IO.Directory.CreateDirectory(dir); System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "t275.json"), sb.ToString()); }
                catch (Exception e) { Debug.LogWarning("[T289] t275.json 저장 실패(" + dir + "): " + e.Message); }
            }
            _log.AssertNoRed("열쇠 옷 폭");
            // 실측이 든 파일(`screens:t275.json`)이 먼저 쓰이고 나서 막는다 — 빨개도 «얼마나» 를 읽을 수 있게.
            Assert.IsEmpty(over, "열쇠 옷이 «보유/쓸 개수» 를 못 담는다(칸을 넓히거나 줄을 줄여야 한다 · 수는 screens 의 t275.json): "
                                 + string.Join(" · ", over));
            yield return Shutdown();
        }
    }
}
