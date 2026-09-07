using System;
using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T96-profile 1단계 — 상단 재화 바의 <b>아바타를 누르면</b> 주인이 지목한 <c>Social_Profile_Avatar</c> 팝업이 뜨고,
    /// 고른 테두리 색이 세이브에 남아 탑바 조각이 그 색으로 선다.
    /// ⓐ 아바타 칸에 버튼이 붙어 있다(조각에는 버튼이 없어 <see cref="UiKit.Clickable"/> 이 붙인다 — 결정 303 의 함정)
    /// ⓑ 팝업 = <c>ui.profileAvatar</c> 조각 · 칸 다섯(<c>Avatar:&lt;색&gt;</c>) · 영문 «Avatar»·«Choose» 0
    /// ⓒ 다른 색을 고르고 «선택» 하면 <see cref="SaveData.ProfileColor"/> 가 바뀌고 탑바가 그 조각으로 다시 선다 · 빨간 줄 0.
    /// </summary>
    public class ProfileTests
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

        [UnityTest]
        public IEnumerator AvatarOpensProfileAndKeepsTheChosenFrame()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            // ⓐ 아바타 칸이 눌린다
            var avatar = UiKit.Find(_app.Current.Root, "Avatar");
            Assert.IsNotNull(avatar, "탑바 아바타 칸");
            var avBtn = avatar.GetComponent<Button>();
            Assert.IsNotNull(avBtn, "아바타 칸에 버튼(조각에는 없어 Clickable 이 붙인다)");
            // 기본 색 = 첫 색(노랑) 조각이 서 있다
            Assert.AreEqual(Profile.Colors[0], Profile.Current(_app.Save), "안 고르면 기본 색");
            Assert.IsNotNull(UiKit.Find(avatar, "ui.profileFrame." + Profile.Colors[0]), "기본 테두리 조각이 탑바에 서 있다");

            // ⓑ 누르면 주인 지목 팝업
            avBtn.onClick.Invoke(); yield return Frames(2); Canvas.ForceUpdateCanvases();
            Assert.IsTrue(_app.Overlay.IsOpen, "프로필은 팝업");
            var ov = _app.Overlay.Root;
            Assert.IsNotNull(UiKit.Find(ov, "ui.profileAvatar"), "Social_Profile_Avatar 조각(주인 지목)");
            int rows = 0;
            foreach (var t in ov.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(Profile.RowPrefix, StringComparison.Ordinal) && t.gameObject.activeInHierarchy) rows++;
            Assert.AreEqual(Profile.Colors.Length, rows, "칸 = 우리 색 다섯(남는 칸은 끈다)");
            foreach (var t in ov.GetComponentsInChildren<Text>(true))
            {
                string s = (t.text ?? "").Trim();
                Assert.AreNotEqual("Avatar", s, "영문 데모 글자 0(제목은 «아바타»)");
                Assert.AreNotEqual("Choose", s, "영문 데모 글자 0(버튼은 «선택»)");
            }

            // ⓒ 두 번째 색을 고르고 «선택»
            string want = Profile.Colors[1];
            var row = UiKit.Find(ov, Profile.RowPrefix + want);
            Assert.IsNotNull(row, "그 색 칸");
            var rowBtn = row.GetComponent<Button>(); Assert.IsNotNull(rowBtn, "칸에 버튼(Clickable 이 붙인다)");
            rowBtn.onClick.Invoke(); yield return Frames(1);
            var choose = UiKit.Find(_app.Overlay.Root, Profile.ChooseName);
            Assert.IsNotNull(choose, "«선택» 버튼");
            var chooseBtn = choose.GetComponent<Button>(); Assert.IsNotNull(chooseBtn, "그 버튼의 Button");
            chooseBtn.onClick.Invoke(); yield return Frames(3); Canvas.ForceUpdateCanvases();

            Assert.AreEqual(want, _app.Save.ProfileColor, "고른 색이 세이브에 남는다");
            Assert.IsFalse(_app.Overlay.IsOpen, "고르면 닫힌다");
            var avatar2 = UiKit.Find(_app.Current.Root, "Avatar");
            Assert.IsNotNull(avatar2, "탑바 아바타 칸(다시)");
            Assert.IsNotNull(UiKit.Find(avatar2, "ui.profileFrame." + want), "탑바 테두리가 고른 색 조각으로 선다");
            Assert.IsNotNull(avatar2.GetComponentInChildren<HeroView>(true), "초상(HeroView)은 그대로 그 안에");

            _log.AssertNoRed("T96-profile 아바타 고르기");
            yield return Shutdown();
        }
    }
}
