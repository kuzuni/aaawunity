using System;
using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T169 2항 — <b>«열었으면 닫을 수 있다»</b> 를 아무도 안 재고 있었다(게이트 구멍).
    /// <para>
    /// 여태 PlayMode 스모크는 팝업을 «열고 빨간 줄 0» 만 봤다. 그래서 우편함이 <b>닫는 길이 하나도 없는 막힌 창</b>이 된 채로
    /// 모든 게이트가 초록이었다(주인 «우편함 팝업 안 닫힌다 수정 좀»). 이 자는 <c>Overlay.OpenPrefab</c> 으로 여는 팝업을
    /// 하나씩 열어 <b>사람이 누를 수 있는 것을 실제로 눌러</b> <c>Overlay.IsOpen == false</c> 가 되는지 본다.
    /// </para>
    /// <para>
    /// «배선이 돼 있는가» 를 코드로 들여다보지 않고 <b>눌러서</b> 판정하는 까닭 — 런타임에 붙인
    /// <c>onClick.AddListener</c> 는 세어 볼 방법이 없다(<c>GetPersistentEventCount</c> 는 직렬화된 것만 센다).
    /// 눌러 보는 것이 주인이 겪는 일과 같기도 하다.
    /// </para>
    /// <para>
    /// ⚠ 뽑기 결과 창(<c>ui.chestOpen</c>)은 여기 없다 — 상점 화면에서 실제로 구매를 태워야 열리는 자리라
    /// 한 줄로 못 연다. 그 창은 조각의 <c>Background</c> 탭이 이미 <c>Overlay.Close()</c> 로 배선돼 있고
    /// (<c>ShopScreen.cs</c> «배경 탭 = 닫기»), 여는 흐름은 <c>UiSmokeTests.ShopBoxesAndChestOpenPopup</c> 가 탄다.
    /// 그 자를 이리로 옮기는 것은 <c>UiSmokeTests</c> 를 여러 워커가 동시에 만지는 동안은 피한다(결정 387).
    /// </para>
    /// </summary>
    public class PopupCloseTests
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
            _app = null;
            yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>사람이 «닫으려고» 누를 만한 자리 — 조각의 닫기 버튼 · 어둠 · (조각에 따라) 배경. 이름 순서가 곧 시도 순서다.</summary>
        static readonly string[] CloseSpots = { "Button_Close", "Dimmed", "Background" };

        /// <summary>이름이 <paramref name="prefix"/> 로 시작하는, 화면에 실제로 보이고 누를 수 있는 첫 자리.</summary>
        static Button Spot(Transform root, string prefix)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || !t.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (!t.gameObject.activeInHierarchy) continue;
                var b = t.GetComponent<Button>();
                if (b != null && b.IsActive() && b.IsInteractable()) return b;
            }
            return null;
        }

        /// <summary>지금 팝업 층에 서 있는 조각의 이름(= 카탈로그 키 · <c>UiKit.Spawn</c> 이 그 이름으로 세운다) — «다른 팝업으로 갔는가» 를 이것으로 가른다.</summary>
        static string Top(Transform root)
        {
            if (root == null) return "";
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c != null && c.gameObject.activeSelf) return c.name;
            }
            return "";
        }

        /// <summary>
        /// <paramref name="open"/> 으로 연 팝업이 <b>«막힌 창» 이 아닌지</b> 본다 — <see cref="CloseSpots"/> 중 하나를 눌러
        /// 팝업 층이 닫히거나, 적어도 <b>닫을 수 있는 다른 팝업으로 돌아가는지</b>.
        /// <para>
        /// «바로 닫힘» 만 보면 안 되는 까닭 — 이름 바꾸기 팝업의 닫기는 <b>«뒤로»</b>(아바타 팝업으로 되돌아감)다
        /// (<c>Profile.cs</c> «close → OpenAvatar»). 그것은 결함이 아니라 일부러 그렇게 만든 길이고,
        /// 돌아간 자리에서 닫히면 주인은 갇히지 않는다. 우리가 재려는 것은 «갇히지 않는가» 다.
        /// </para>
        /// 후보마다 <b>새로 열어서</b> 하나씩 시도한다 — 안 닫히는 후보가 다른 일(예: 스킵)을 해 버려도 다음 판정이 안 흔들린다.
        /// </summary>
        IEnumerator AssertClosable(string what, Action open)
        {
            var found = new List<string>();
            var seen = new List<string>();
            foreach (var prefix in CloseSpots)
            {
                _app.Overlay.Close(); yield return Frames(1);
                open(); yield return Frames(2); Canvas.ForceUpdateCanvases();
                Assert.IsTrue(_app.Overlay.IsOpen, what + " 가 열려야 한다");
                string before = Top(_app.Overlay.Root);
                var b = Spot(_app.Overlay.Root, prefix);
                if (b == null) continue;
                seen.Add(prefix);
                b.onClick.Invoke(); yield return Frames(2);
                if (!_app.Overlay.IsOpen) { found.Add(prefix); continue; }

                // 아직 열려 있다 — 아무 데도 안 갔으면 «닫는 길» 이 아니다
                string after = Top(_app.Overlay.Root);
                if (after == before) continue;

                // 다른 팝업으로 «뒤로» 갔다 → 거기서 닫히면 갇힌 것이 아니다
                foreach (var p2 in CloseSpots)
                {
                    var b2 = Spot(_app.Overlay.Root, p2);
                    if (b2 == null) continue;
                    b2.onClick.Invoke(); yield return Frames(2);
                    if (!_app.Overlay.IsOpen) break;
                }
                if (!_app.Overlay.IsOpen) found.Add(prefix + "→" + after);
            }
            _app.Overlay.Close(); yield return Frames(1);
            Debug.Log("[PopupClose] " + what + " — 누를 수 있는 자리 [" + string.Join(",", seen.ToArray())
                      + "] · 그중 빠져나가지는 것 [" + string.Join(",", found.ToArray()) + "]");
            Assert.Greater(found.Count, 0,
                what + " 에서 빠져나갈 길이 하나도 없다(막힌 창) — 눌러 본 자리: [" + string.Join(",", seen.ToArray()) + "] · "
                + "조각의 닫기 버튼을 배선하거나 OpenPrefab 을 closeOnDim: true 로 부른다(T169)");
        }

        /// <summary>T169 2항 — <c>OpenPrefab</c> 으로 여는 팝업은 전부 «눌러서 닫을 수» 있어야 한다.</summary>
        [UnityTest]
        public IEnumerator EveryPrefabPopupCanBeClosedByHand()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            // T169 — 주인이 말한 그 창. 조각의 Button_Close_01 배선 + 어둠 둘 다 들어갔다.
            yield return AssertClosable("우편함", () => Mailbox.Open(_app));
            // 나머지 OpenPrefab 자리 — 같은 구멍이 다른 팝업에도 있는지 이 참에 드러난다.
            yield return AssertClosable("로비 메뉴(≡)", () => LobbyMenu.Open(_app));
            yield return AssertClosable("퀘스트", () => LobbyPopups.Quest(_app));
            yield return AssertClosable("출석", () => LobbyPopups.Attendance(_app));
            yield return AssertClosable("프로필 아바타", () => Profile.OpenAvatar(_app));
            yield return AssertClosable("닉네임", () => Profile.OpenNickname(_app));

            _log.AssertNoRed("T169 팝업 닫기");
            yield return Shutdown();
        }
    }
}
