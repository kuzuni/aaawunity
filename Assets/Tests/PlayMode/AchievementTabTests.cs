using System.Collections;
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
    /// T258 4항 — 퀘스트 팝업(15)의 <b>«업적» 탭</b>. 규칙은 <c>Core/Achievement</c> 가 갖고 EditMode 가 이미 잰다(<c>AchievementTests</c>) —
    /// 여기서 재는 것은 <b>화면이 그 규칙을 그대로 그리는가</b> 뿐이다.
    /// <list type="bullet">
    /// <item>줄 수·글자·목표가 <b>표에서</b> 온다(수를 안 적는다 — 주인이 표를 고치면 화면이 따라가야 하고 자는 안 깨져야 한다).</item>
    /// <item><b>메달 트랙·새로고침 줄이 없다</b>(주인 «메달 없음» · 누적은 초기화되지 않아 «새로고침까지» 가 거짓말이 된다).</item>
    /// <item>«받기» 는 <b>깬 단계가 있을 때만</b> 눌린다 — 눌리는데 아무 일도 안 나는 자리를 안 만든다(결정 771).</item>
    /// <item>받으면 <b>한 단계만</b> 들어오고(주인 «순차») 누적은 안 줄며 다음 목표가 «첫 목표 × 2» 로 바뀐다.</item>
    /// </list>
    /// </summary>
    public class AchievementTabTests
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
            _app = null;
            yield return Frames(3);
        }
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static int CountNamed(Transform t, string head)
        {
            int n = 0;
            foreach (var tr in t.GetComponentsInChildren<Transform>(true))
                if (tr.name.StartsWith(head) && tr.gameObject.activeInHierarchy) n++;
            return n;
        }
        /// <summary>
        /// 제목이 그 글자인 줄 — T364 ⓐ 뒤로 <b>화면 줄 번호 ≠ 표 줄 번호</b>라(받을 수 있는 줄이 위로 온다) 자는 <b>이름으로</b> 집는다.
        /// <para>«화면의 정렬을 그대로 베끼는 자»(<c>_aOrder[0]</c> 를 쓰는 꼴)는 정렬이 틀려도 늘 통과한다 — 그 병을 여기서 피한다(결정 986·992).
        /// 정렬 자체는 따로 잰다(<c>QuestClaimDotTests</c> — 위에서 훑어 «받을 수 있음» 이 꺼진 뒤 다시 켜지면 빨강).</para>
        /// </summary>
        static Transform RowByLabel(Transform root, string label)
        {
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                if (!tr.name.StartsWith("Ach:") || !tr.gameObject.activeInHierarchy) continue;
                var t = UiKit.Find(tr, "Title"); if (t == null) continue;
                var tx = t.GetComponent<TMP_Text>(); if (tx != null && tx.text == label) return tr;
            }
            return null;
        }
        static string BarText(Transform row)
        {
            var sl = row.GetComponentInChildren<Slider>(true); if (sl == null) return null;
            var t = sl.GetComponentInChildren<TMP_Text>(true); return t != null ? t.text : null;
        }

        [UnityTest]
        public IEnumerator 업적탭은_표를_그리고_트랙이_없다()
        {
            yield return Boot();
            var d = _app.Data != null ? _app.Data.Achievement : null;
            Assert.IsNotNull(d, "업적 표(achievement.json)가 실려야 한다");

            LobbyPopups.Achievements(_app);
            yield return Frames(1);
            var root = _app.Overlay.Root;

            Assert.AreEqual(d.List.Count, CountNamed(root, "Ach:"), "줄 수 = 표의 줄 수(수를 안 적는다)");
            Assert.AreEqual(0, CountNamed(root, "Quest:"), "업적 탭에는 퀘스트 줄이 없다");
            Assert.IsNull(UiKit.Find(root, "TrackBox"), "업적에는 메달 트랙이 없다(주인 «메달 없음»)");
            Assert.IsNull(UiKit.Find(root, "Refresh"), "업적 누적은 초기화되지 않으니 «새로고침까지» 도 없다");
            Assert.AreEqual(3, CountNamed(root, "Tab:"), "탭은 셋 그대로");

            // T364 ⓐ 뒤(런 900 빨강 · 워커 B 진단 · 결정 1023) — **줄 차례는 더 이상 표 차례가 아니다**(받을 수 있는 줄이 위로 온다).
            //   ⚠ 그렇다고 자를 `_aOrder[0]` 으로 «옮기면» 안 된다 — 화면의 정렬을 그대로 베끼는 자는 정렬이 틀려도 늘 통과한다(결정 986·992).
            //   ⇒ **자리로 집지 말고 이름으로 집는다**: 표의 첫 줄을 «그 제목을 가진 줄» 로 찾아 그 줄의 진행도를 본다.
            //   (부팅만으로 «출석 1회» 가 받을 수 있게 되어 맨 위로 오므로 `Ach:0` 은 그 줄이 아니다 · T288-1 의 그 함정)
            var row0 = RowByLabel(root, d.List[0].Label);
            Assert.IsNotNull(row0, "표의 첫 줄(«" + d.List[0].Label + "»)이 화면 어딘가에 있어야 한다 — 제목은 표의 글자 그대로(주인이 쓴 말)");
            Assert.AreEqual("0/" + d.List[0].Goal, BarText(row0), "새 세이브의 진행도 = «0/첫 목표»");

            // T288-1 — 처음에 «새 세이브면 «받기» 가 하나도 안 눌린다» 로 적었다가 CI run 645 에서 빨개졌다.
            //   까닭: **앱을 켠 것만으로 «출석 1회»(목표 1)가 깨진다**(App.Create → Quests.Login → Achievement.AddOncePerDay).
            //   즉 자가 틀렸고 화면은 옳았다. 퀘스트 탭에서 «로그인하기» 로 한 번 밟은 함정을 업적에서 그대로 다시 밟은 것이다.
            //   ⇒ 수를 적지 않고 **규칙**으로 잰다: 줄마다 «눌리는가» = `CanClaim` 이어야 한다.
            //   그리고 규칙만 재면 «훅이 통째로 빠져도 양쪽이 같이 false» 라 초록이므로, 출석 한 줄은 **못 박아** 둔다.
            //   ⚠ T364 뒤 — 여기서도 줄을 **번호가 아니라 이름으로** 집는다(화면 i 는 이제 표 i 가 아니다).
            for (int i = 0; i < d.List.Count; i++)
            {
                var r = RowByLabel(root, d.List[i].Label); Assert.IsNotNull(r, "표의 줄 «" + d.List[i].Label + "» 이 화면에 있어야 한다");
                var rb = UiKit.Find(r, "AchBtn"); Assert.IsNotNull(rb, "«" + d.List[i].Label + "» 의 «받기»");
                bool can = Achievement.CanClaim(_app.Save, d, d.List[i].Counter);
                Assert.AreEqual(can, rb.GetComponent<Button>().interactable,
                                "«" + d.List[i].Label + "» 의 «받기» 는 받을 수 있을 때만 눌린다");
            }
            Assert.IsTrue(Achievement.CanClaim(_app.Save, d, Achievement.DailyOnce),
                          "켠 것만으로 «출석» 업적은 받을 수 있어야 한다(T258 훅 · App.Create → Quests.Login)");

            // T258 — 프리팹이 데모용 «Disabled» 덮개(크림색 알파 0.70)를 둘 달고 온다. 일일 판에서는 안 띄는 자리라 아무도 안 껐는데,
            //   목록이 위로 늘어난 업적 판에서 **아래 두 줄을 덮은 반투명 사각형**으로 드러났다(run 694 사진).
            //   §5 는 **이름표 없는 조각을 못 재므로** 표 점수는 그때도 10.0 이었다 — 그래서 그 갈래를 자로 옮겨 둔다.
            //   ⚠ **꺼진 것까지 세면 안 된다** — 고침은 조각을 지우는 것이 아니라 «끄는» 것이라(프리팹 조각은 안 지운다 · 공통 문법)
            //   `activeInHierarchy` 만 본다. 이것을 안 가리면 고쳐도 자가 빨갛다.
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.gameObject.activeInHierarchy)
                    Assert.AreNotEqual("Disabled", t.name, "프리팹 데모 덮개(«Disabled»)가 켜져 있으면 줄을 가린다");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 깨면_받기가_살아나고_한_단계씩_들어온다()
        {
            yield return Boot();
            var d = _app.Data.Achievement;
            var row = d.List[0];                                  // 어느 줄이든 규칙은 같다 — 표의 첫 줄로 잰다
            Achievement.Add(_app.Save, row.Counter, row.Goal * 2);  // 두 단계치를 한꺼번에 쌓아 둔다(주인 «이미 20회 뽑았고» 의 그 자리)
            _app.Persist();

            LobbyPopups.Achievements(_app);
            yield return Frames(1);
            var root = _app.Overlay.Root;
            // T364 ⓐ 뒤 — 줄은 이름으로 집는다(받을 수 있는 줄이 위로 오므로 «0번» 이 이 줄이라는 보장이 없다).
            var btn = UiKit.Find(RowByLabel(root, row.Label), "AchBtn").GetComponent<Button>();
            Assert.IsTrue(btn.interactable, "깬 단계가 있으면 «받기» 가 산다");

            double gemBefore = _app.Save.Gem;
            int haveBefore = Achievement.Count(_app.Save, row.Counter);
            btn.onClick.Invoke();
            yield return Frames(1);

            Assert.AreEqual(gemBefore + row.Amount, _app.Save.Gem, 1e-6, "한 단계 몫만 들어온다(주인 «순차»)");
            Assert.AreEqual(haveBefore, Achievement.Count(_app.Save, row.Counter), "받아도 누적은 안 준다(«평생 기록»)");
            Assert.AreEqual(1, Achievement.Claimed(_app.Save, row.Counter), "받은 단계만 하나 오른다");
            Assert.IsTrue(Achievement.CanClaim(_app.Save, d, row.Counter), "두 단계치를 쌓았으니 아직 한 번 더 받을 수 있다");

            yield return Shutdown();
        }

        [UnityTest]
        public IEnumerator 퀘스트_사이드_아이콘의_빨간_점이_업적도_본다()
        {
            // T258 6항 — 판정(`Notify`)은 T257 이 세웠는데 **그것을 켜는 점이 로비에 없었다**(기프트·출석·탐험·챕터 보상 넷뿐).
            //   판정만 있고 보여 주는 조각이 없으면 사용자에게는 없는 기능이다 — 그래서 점을 달고 그 점을 여기서 잰다.
            //   퀘스트 탭과 업적 탭은 **같은 팝업**이라 점도 하나다(둘 중 아무거나 받을 것이 있으면 켠다).
            yield return Boot();
            var d = _app.Data.Achievement;
            var dot = UiKit.Find(_app.Current.Root, "QuestDot");
            Assert.IsNotNull(dot, "로비 «퀘스트» 칸에 빨간 점이 있어야 한다");
            Assert.IsTrue(dot.gameObject.activeSelf, "켠 것만으로 «출석» 업적을 받을 수 있으니 점이 켜져 있다");

            // 받을 것을 다 받으면 꺼진다 — 새 세이브의 퀘스트 트랙은 아직 한 칸도 안 열린다(메달 10 < 첫 구간 20).
            while (Achievement.AnyClaimable(_app.Save, d))
                foreach (var r in d.List) Achievement.Claim(_app.Save, d, r.Counter, out _, out _);
            _app.Persist(); _app.Current.Refresh(); yield return Frames(1);
            Assert.IsFalse(dot.gameObject.activeSelf, "받을 것이 없으면 점이 꺼진다");

            yield return Shutdown();
        }

        /// <summary>
        /// T258 탐침 — <b>줄 바탕을 실제로 칠하는 조각이 무엇인가</b>를 «가정» 대신 «실측» 으로 남긴다(<c>ui-screens/t258rows.json</c> → `screens` 브랜치).
        /// <para>
        /// 두 회차 연속으로 틀렸다: 처음엔 `Find(frame,"Bg")` 가 **안 보이는 «Focus» 가지**를 잡았고(결정 906), 길을 `Nomal/Bg` 로 못 박은 뒤에도
        /// 사진의 색이 **한 단계도 안 움직였다**(실측 `#B49E4C` 그대로 · run 810). 즉 보이는 면은 그 둘 중 어느 것도 아니다.
        /// </para>
        /// 그래서 <b>줄 안의 모든 <see cref="Image"/></b> 를 이름·자리·색·스프라이트·켜짐과 함께 적어 둔다 — 다음 회차가 그 표를 읽고 **한 번에** 고친다.
        /// 자는 아무것도 단언하지 않는다(늘 초록): 이것은 «자» 가 아니라 «자 대신 사진을 읽는 우리를 위한 계기» 다(T233 의 `t233.json` 과 같은 꼴).
        /// </summary>
        [UnityTest]
        public IEnumerator 줄바탕_조각_탐침_t258rows_json()
        {
            yield return Boot();
            LobbyPopups.Quest(_app);   // 일일 판 — 첫 줄은 깬 줄(로그인) · 둘째는 못 깬 줄이라 «두 상태» 가 한 화면에 있다
            yield return Frames(1);
            var root = _app.Overlay.Root;
            var sb = new System.Text.StringBuilder();
            sb.Append("{\n  \"rows\": [");
            for (int i = 0; i < 3; i++)
            {
                var row = UiKit.Find(root, "Quest:" + i); if (row == null) continue;
                if (i > 0) sb.Append(',');
                sb.Append("\n    { \"row\": ").Append(i).Append(", \"images\": [");
                bool first = true;
                foreach (var im in row.GetComponentsInChildren<Image>(true))
                {
                    var rt = (RectTransform)im.transform;
                    var c = im.color;
                    string path = im.name; var t = im.transform.parent;
                    while (t != null && t != row) { path = t.name + "/" + path; t = t.parent; }
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append("\n      { \"path\": \"").Append(path).Append("\", \"on\": ").Append(im.gameObject.activeInHierarchy ? "true" : "false")
                      .Append(", \"color\": \"").Append(ColorUtility.ToHtmlStringRGBA(c)).Append("\", \"sprite\": \"").Append(im.sprite != null ? im.sprite.name : "-")
                      .Append("\", \"w\": ").Append(rt.rect.width.ToString("0.0")).Append(", \"h\": ").Append(rt.rect.height.ToString("0.0")).Append(" }");
                }
                sb.Append("\n    ] }");
            }
            sb.Append("\n  ]\n}\n");
            foreach (var dir in PlayShot.Dirs())
            {
                try { System.IO.Directory.CreateDirectory(dir); System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "t258rows.json"), sb.ToString()); }
                catch (System.Exception e) { Debug.LogWarning("[T258] 탐침 저장 실패: " + e.Message); }
            }
            Debug.Log("[T258] 줄 조각 탐침:\n" + sb);
            yield return Shutdown();
        }
    }
}
