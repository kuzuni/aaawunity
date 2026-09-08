using System;
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
    /// T253 4항 — 출석(16) 팝업의 <b>화면</b> 쪽. 표·규칙은 EditMode <c>AttendanceTests</c> 가 못 박고 여기서는
    /// ⓐ 칸의 <b>수량 글자가 표에서 온다</b>(코드에 박힌 «1» 이 아니다) ⓑ 오늘 칸이 강조되고 ⓒ 누르면 <b>즉시 지급</b>되며
    /// (우편함으로 안 간다 · T243) 받은 칸에 ✅ 가 붙고 ⓓ 로비 «출석» 아이콘의 빨간 점이 «오늘 받을 게 있을 때만» 켜지는 것을 본다.
    /// </summary>
    public class AttendancePlayTests
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

        static bool HasTextIn(Transform root, string want)
        {
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                if ((t.text ?? "").Trim() == want) return true;
            return false;
        }
        static bool ShownIn(Transform root, string name)
        {
            var t = UiKit.Find(root, name);
            return t != null && t.gameObject.activeInHierarchy;
        }

        [UnityTest]
        public IEnumerator AttendanceCellsComeFromTheTableAndClaimingPaysAtOnce()
        {
            yield return Boot();
            var AT = _app.Data.Attendance;
            Assert.IsNotNull(AT, "attendance.json 이 카탈로그(data.attendance)로 실려야 한다");
            string today = SaveStore.Today();
            var S = _app.Save;
            S.Gold = 0; S.Gem = 0; S.PetEgg = 0; S.KeyBlue = 0; S.KeyPurple = 0;

            // ⓐ 칸의 수량이 표에서 온다 — 1일차(골드 3,000)와 2일차(파란 키 2)를 글자로 확인
            LobbyPopups.Attendance(_app); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var ov = _app.Overlay.Root;
            var day1 = UiKit.Find(ov, "Day:1"); Assert.IsNotNull(day1, "1일차 칸");
            string q1 = Math.Round(AT.Of(1).Rewards[0].Amount).ToString("#,0");
            string q2 = Math.Round(AT.Of(2).Rewards[0].Amount).ToString("#,0");
            Assert.IsTrue(HasTextIn(day1, q1), "1일차 수량 = 표의 값(" + q1 + ") — 코드에 박힌 «1» 이 아니다");
            Assert.IsTrue(HasTextIn(UiKit.Find(ov, "Day:2"), q2), "2일차 수량 = 표의 값(" + q2 + ")");
            // ⓑ 오늘 칸(= 1일차) 강조 · 아직 받은 칸이 없으니 ✅ 는 하나도 없다
            Assert.IsTrue(ShownIn(day1, "Bg_Focus1"), "오늘 받을 칸이 강조된다");
            Assert.IsFalse(ShownIn(day1, "Check"), "아직 ✅ 는 없다");
            _log.AssertNoRed("출석 팝업(표에서 그린다)");

            // ⓒ 누르면 즉시 지급 — 재화가 실제로 들어오고 우편함에는 아무것도 안 쌓인다(T243)
            double gold0 = S.Gold;
            var btn = day1.GetComponentInChildren<Button>(true); Assert.IsNotNull(btn, "칸이 눌린다");
            btn.onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(gold0 + AT.Of(1).Rewards[0].Amount, S.Gold, 1e-9, "1일차 골드가 바로 들어온다");
            Assert.AreEqual(1, S.AttDone, "받은 칸 수가 1");
            Assert.AreEqual(today, S.AttDay, "받은 날짜가 오늘");
            Assert.AreEqual(0, Mail.Pending(S).Count, "우편함에는 아무것도 안 쌓인다(주인 «나머지는 즉시 지급»)");

            // ⓒ-2 같은 날 또 누르면 «이미 받았습니다» — 재화가 두 배가 안 된다
            double gold1 = S.Gold;
            LobbyPopups.Attendance(_app); yield return Frames(2);
            ov = _app.Overlay.Root;
            day1 = UiKit.Find(ov, "Day:1"); Assert.IsNotNull(day1);
            Assert.IsTrue(ShownIn(day1, "Check"), "받은 칸에 ✅ 가 붙는다");
            Assert.IsFalse(ShownIn(day1, "Bg_Focus1"), "오늘은 이미 받았으니 강조 칸이 없다");
            day1.GetComponentInChildren<Button>(true).onClick.Invoke(); yield return Frames(2);
            Assert.AreEqual(gold1, S.Gold, 1e-9, "같은 날 두 번 눌러도 안 준다");
            _log.AssertNoRed("출석 두 번 누르기");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓓ 로비 «출석» 빨간 점 — 오늘은 받았으니 꺼져 있고, 날짜를 되돌리면 켜진다
            _app.ShowScreen("lobby"); yield return Frames(2);
            var root = _app.Current.Root;
            var dot = UiKit.Find(root, "AttendDot");
            Assert.IsNotNull(dot, "«출석» 칸의 빨간 점 조각");
            Assert.IsFalse(dot.gameObject.activeSelf, "오늘 받을 게 없으면 점이 꺼진다");
            S.AttDay = "2000-01-01";                    // 어제까지만 받은 상태로
            _app.Current.Refresh(); yield return Frames(2);
            Assert.IsTrue(dot.gameObject.activeSelf, "오늘 받을 칸이 있으면 점이 켜진다");
            _log.AssertNoRed("출석 알림 점");

            yield return Shutdown();
        }
    }
}
