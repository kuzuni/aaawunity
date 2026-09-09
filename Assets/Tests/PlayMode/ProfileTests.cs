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
    /// T96-profile 1단계 — 상단 재화 바의 <b>아바타를 누르면</b> 주인이 지목한 <c>Social_Profile_Avatar</c> 팝업이 뜨고,
    /// 고른 테두리 색이 세이브에 남아 탑바 조각이 그 색으로 선다.
    /// ⓐ 아바타 칸에 버튼이 붙어 있다(조각에는 버튼이 없어 <see cref="UiKit.Clickable"/> 이 붙인다 — 결정 303 의 함정)
    /// ⓑ 팝업 = <c>ui.profileAvatar</c> 조각 · 칸 다섯(<c>Avatar:&lt;색&gt;</c>) · 영문 «Avatar»·«Choose» 0
    /// ⓒ 다른 <b>초상 아이콘</b>을 고르고 «선택» 하면 <see cref="SaveData.ProfileIcon"/> 가 바뀌고 탑바가 그 초상으로 다시 선다 · 빨간 줄 0.
    /// <para>
    /// ⚠ T262 ⓐ 뒤로 이 자의 계약이 뒤집혔다 — 아바타 자리에 <see cref="HeroView"/> 가 <b>없어야</b> 한다(주인 «플레이어 이미지 말고»).
    /// 옛 계약(«초상(HeroView)은 그대로 그 안에»)을 지운 줄은 <b>새 줄 바로 옆에 남아 서로를 부정하고 있었다</b> — 라이선스가 풀린 첫 완주 런에서 그대로 빨개졌을 자리다 — <b>검수 Q 가 먼저 정적으로 짚었고</b>(결정 804) 임자가 지웠다(결정 805).
    /// </para>
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
            // 기본 = 첫 색 테두리 + 첫 초상 아이콘(T262 ⓐ · 주인 «플레이어 이미지 말고»)
            Assert.AreEqual(Profile.Colors[0], Profile.Current(_app.Save), "안 고르면 기본 색");
            Assert.IsNotNull(UiKit.Find(avatar, "ui.profileFrame." + Profile.Colors[0]), "기본 테두리 조각이 탑바에 서 있다");
            Assert.AreEqual(Profile.Icons[0], Profile.CurrentIcon(_app.Save), "안 고르면 기본 초상 아이콘");
            Assert.IsNotNull(UiKit.Find(avatar, Profile.FaceName), "탑바 초상은 아이콘 그림이다");
            Assert.IsNull(avatar.GetComponentInChildren<HeroView>(true), "내 캐릭터 그림(HeroView)은 아바타 자리에 없다(주인 «플레이어 이미지 말고»)");

            // ⓑ 누르면 주인 지목 팝업
            avBtn.onClick.Invoke(); yield return Frames(2); Canvas.ForceUpdateCanvases();
            Assert.IsTrue(_app.Overlay.IsOpen, "프로필은 팝업");
            var ov = _app.Overlay.Root;
            Assert.IsNotNull(UiKit.Find(ov, "ui.profileAvatar"), "Social_Profile_Avatar 조각(주인 지목)");
            int rows = 0;
            foreach (var t in ov.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(Profile.RowPrefix, StringComparison.Ordinal) && t.gameObject.activeInHierarchy) rows++;
            Assert.AreEqual(Profile.Icons.Length, rows, "칸 = 고를 수 있는 초상 아이콘 넷(남는 칸은 끈다 · T262 ⓐ)");
            foreach (var t in ov.GetComponentsInChildren<TMP_Text>(true))
            {
                string s = (t.text ?? "").Trim();
                Assert.AreNotEqual("Avatar", s, "영문 데모 글자 0(제목은 «아바타»)");
                Assert.AreNotEqual("Choose", s, "영문 데모 글자 0(버튼은 «선택»)");
            }

            // ⓒ 두 번째 초상을 고르고 «선택»
            string want = Profile.Icons[1];
            var row = UiKit.Find(ov, Profile.RowPrefix + want);
            Assert.IsNotNull(row, "그 색 칸");
            var rowBtn = row.GetComponent<Button>(); Assert.IsNotNull(rowBtn, "칸에 버튼(Clickable 이 붙인다)");
            rowBtn.onClick.Invoke(); yield return Frames(1);
            var choose = UiKit.Find(_app.Overlay.Root, Profile.ChooseName);
            Assert.IsNotNull(choose, "«선택» 버튼");
            var chooseBtn = choose.GetComponent<Button>(); Assert.IsNotNull(chooseBtn, "그 버튼의 Button");
            chooseBtn.onClick.Invoke(); yield return Frames(3); Canvas.ForceUpdateCanvases();

            Assert.AreEqual(want, _app.Save.ProfileIcon, "고른 초상이 세이브에 남는다");
            Assert.IsFalse(_app.Overlay.IsOpen, "고르면 닫힌다");
            var avatar2 = UiKit.Find(_app.Current.Root, "Avatar");
            Assert.IsNotNull(avatar2, "탑바 아바타 칸(다시)");
            Assert.IsNotNull(UiKit.Find(avatar2, Profile.FaceName), "탑바에 초상 아이콘이 선다");
            Assert.AreEqual(want, Profile.CurrentIcon(_app.Save), "그 초상이 고른 것이다");
            Assert.IsNull(avatar2.GetComponentInChildren<HeroView>(true), "고른 뒤에도 HeroView 는 안 돌아온다");

            _log.AssertNoRed("T96-profile 아바타 고르기");
            yield return Shutdown();
        }

        /// <summary>
        /// T96-profile 2단계 — 아바타 팝업 제목(= 지금 내 이름)을 누르면 주인 지목 <c>Social_Profile_Nickname</c> 이 뜨고,
        /// TMP 입력칸이 uGUI <see cref="InputField"/> 로 서 있어 이름을 지을 수 있다.
        /// ⓐ 제목이 «Avatar» 가 아니라 내 이름 ⓑ 눌러서 열리는 조각 = <c>ui.profileNick</c> · 입력칸·확인·글자 수가 다 있다
        /// ⓒ 2자 미만이면 «확인» 이 흐리고 안 눌린다 ⓓ 지으면 세이브에 남고 아바타 팝업 제목이 새 이름이 된다 ⓔ 빨간 줄 0.
        /// </summary>
        [UnityTest]
        public IEnumerator NicknameEditsThroughThePrefabInputField()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            var avatar = UiKit.Find(_app.Current.Root, "Avatar");
            Assert.IsNotNull(avatar, "탑바 아바타 칸");
            avatar.GetComponent<Button>().onClick.Invoke(); yield return Frames(2); Canvas.ForceUpdateCanvases();

            // ⓐ 제목 = 지금 내 이름(안 지었으면 기본 이름)
            var nickBtn = UiKit.Find(_app.Overlay.Root, Profile.NickName);
            Assert.IsNotNull(nickBtn, "아바타 팝업 제목 = 이름 바꾸기 입구(NickBtn)");
            var nickLabel = nickBtn.GetComponentInChildren<TMP_Text>(true);
            Assert.IsNotNull(nickLabel, "제목 글자");
            Assert.AreEqual(Nickname.Default, nickLabel.text, "안 지었으면 기본 이름이 제목에 선다");

            // ⓑ 누르면 주인 지목 이름 조각
            var nickBtnB = nickBtn.GetComponent<Button>();
            Assert.IsNotNull(nickBtnB, "제목 줄에 버튼(Clickable 이 붙인다)");
            nickBtnB.onClick.Invoke(); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var ov = _app.Overlay.Root;
            Assert.IsNotNull(UiKit.Find(ov, "ui.profileNick"), "Social_Profile_Nickname 조각(주인 지목)");
            var input = UiKit.Find(ov, Profile.NickInputName);
            Assert.IsNotNull(input, "입력칸(NickInput)");
            // T207 ② — 조각의 입력칸을 부수지 않으므로 «TMP_InputField 그대로» 가 새 계약이다(전에는 uGUI InputField 로 갈아 끼웠다).
            var field = input.GetComponent<TMP_InputField>();
            Assert.IsNotNull(field, "조각의 TMP 입력칸이 그대로 서 있다(T207 ② — 부수지 않는다)");
            Assert.IsNotNull(field.textComponent, "제 글자 컴포넌트를 갖고 있다");
            Assert.AreEqual(Nickname.MaxLen, field.characterLimit, "한도 = 조각 실측 12");
            Assert.AreEqual(Nickname.Default, field.text, "지금 이름이 채워져 있다");
            var okT = UiKit.Find(ov, Profile.NickOkName);
            Assert.IsNotNull(okT, "«확인» 버튼(NickOkBtn)");
            var ok = okT.GetComponent<Button>(); Assert.IsNotNull(ok, "그 버튼의 Button");
            var count = UiKit.Find(ov, Profile.NickCountName);
            Assert.IsNotNull(count, "글자 수 표시(NickCount)");
            Assert.AreEqual($"{Nickname.Default.Length}/{Nickname.MaxLen}", count.GetComponent<TMP_Text>().text, "글자 수가 지금 이름 길이");
            foreach (var t in ov.GetComponentsInChildren<TMP_Text>(true))
            {
                string s = (t.text ?? "").Trim();
                Assert.AreNotEqual("Nickname", s, "영문 데모 글자 0(제목은 «이름 바꾸기»)");
                Assert.AreNotEqual("Choose", s, "영문 데모 글자 0(버튼은 «확인»)");
            }

            // ⓒ 한 자면 못 누른다
            field.text = "가"; field.onValueChanged.Invoke(field.text); yield return Frames(1);
            Assert.IsFalse(ok.IsInteractable(), "2자 미만이면 «확인» 이 안 눌린다");
            Assert.AreEqual($"1/{Nickname.MaxLen}", count.GetComponent<TMP_Text>().text, "글자 수가 따라간다");

            // ⓓ 지으면 세이브에 남고 아바타 팝업 제목이 새 이름
            field.text = "  용감한 기사  "; field.onValueChanged.Invoke(field.text); yield return Frames(1);
            Assert.IsTrue(ok.IsInteractable(), "2자 이상이면 눌린다");
            ok.onClick.Invoke(); yield return Frames(3); Canvas.ForceUpdateCanvases();
            Assert.AreEqual("용감한 기사", _app.Save.Nick, "다듬어 저장한다(앞뒤 빈칸 제거)");
            Assert.IsTrue(_app.Overlay.IsOpen, "지으면 왔던 아바타 팝업으로 돌아간다");
            var back = UiKit.Find(_app.Overlay.Root, Profile.NickName);
            Assert.IsNotNull(back, "돌아온 아바타 팝업의 제목");
            Assert.AreEqual("용감한 기사", back.GetComponentInChildren<TMP_Text>(true).text, "제목이 새 이름");

            _log.AssertNoRed("T96-profile 이름 바꾸기");
            yield return Shutdown();
        }
    }
}
