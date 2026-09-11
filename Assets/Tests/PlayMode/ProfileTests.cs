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
            // T370 — 목록이 넷에서 열둘로 늘었고 조각이 들고 온 칸은 일곱이라 **모자란 칸은 찍어 낸다**.
            //   그래서 이 줄은 «넷» 이 아니라 **«목록만큼»** 을 잰다 — 늘리는 회차가 이 자를 안 고쳐도 되고,
            //   «표는 열둘인데 화면은 일곱» 이 되는 순간 여기서 운다(그 꼴은 빨간 줄이 안 난다).
            Assert.AreEqual(Profile.Faces.Length, rows, "칸 = 고를 수 있는 초상 수(모자라면 찍어 내고 남으면 끈다 · T370)");
            foreach (var t in ov.GetComponentsInChildren<TMP_Text>(true))
            {
                string s = (t.text ?? "").Trim();
                Assert.AreNotEqual("Avatar", s, "영문 데모 글자 0(제목은 «아바타»)");
                Assert.AreNotEqual("Choose", s, "영문 데모 글자 0(버튼은 «선택»)");
            }

            // ⓒ 두 번째 초상을 고르고 «선택»
            string want = Profile.Faces[Profile.Faces.Length - 1];   // T370 — **새로 늘어난 쪽**을 고른다(늘린 것이 실제로 골라지는가)
            var row = UiKit.Find(ov, Profile.RowPrefix + want);
            Assert.IsNotNull(row, "그 색 칸");
            var rowBtn = row.GetComponent<Button>(); Assert.IsNotNull(rowBtn, "칸에 버튼(Clickable 이 붙인다)");
            rowBtn.onClick.Invoke(); yield return Frames(1);
            // T360 ⓑ — 고른 칸의 ✓ 는 조각이 달고 온 그림이 아니라 공용 «Toggle_Check_02_On»(pi.check)이다. «켜졌다» 만 재면 옛 그림도 통과하므로 스프라이트 이름까지 본다.
            var pickedCheck = UiKit.Find(row, "Check");
            Assert.IsNotNull(pickedCheck, "초상 칸의 «Check» 조각");
            Assert.IsTrue(pickedCheck.gameObject.activeSelf, "고른 칸의 ✓ 가 켜진다");
            var pickedImg = pickedCheck.GetComponent<Image>();
            Assert.IsNotNull(pickedImg, "«Check» 는 Image");
            Assert.IsNotNull(pickedImg.sprite, "✓ 에 그림이 있다");
            Assert.IsTrue(pickedImg.sprite.name.StartsWith("Toggle_Check_02_On", StringComparison.Ordinal), "✓ 그림 = Toggle_Check_02_On(T360) · 실제: " + pickedImg.sprite.name);
            var otherCheck = UiKit.Find(UiKit.Find(ov, Profile.RowPrefix + Profile.Faces[0]), "Check");
            Assert.IsNotNull(otherCheck, "안 고른 칸의 «Check» 조각");
            Assert.IsFalse(otherCheck.gameObject.activeSelf, "안 고른 칸의 ✓ 는 꺼져 있다");
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
        /// T262 ⓑ — 아레나 23·도전 팝업 24 의 초상 <b>프레임이 프로필 프레임과 같은 조각</b>인가
        /// (주인 2026-09-09 «프레임 부분이 실제 프로필 프레임이랑 디자인이 다르네 수정» · «도전 부분 팝업도 마찬가지»).
        /// <para>
        /// 종전에는 <c>ItemFrame_01</c>(모서리를 자른 팔각 물건 칸)이었고 탑바 아바타는 <c>ProfileFrame_02</c>(둥근 네모 + 금테)였다 — <b>조각 자체가 달랐다.</b>
        /// 이 자는 이름만 보고 넘기지 않는다: <b>1위 초상의 그림과 탑바 아바타의 그림이 같은 <see cref="Sprite"/> 인가</b>까지 맞댄다
        /// («같은 키를 적었다» 가 아니라 «같은 것이 섰다» 를 재야 조각이 바뀌는 날 이 자가 먼저 말한다 · 결정 785 와 같은 결).
        /// </para>
        /// ⓐ 시상대 초상 셋 = 프로필 프레임 + 초상 아이콘 · 옛 물건 칸 0 ⓑ 1위는 «내» 자리라 내가 고른 초상 그대로
        /// ⓒ 도전 팝업 줄 초상도 같은 조각 ⓓ 더미 초상 목록의 정본은 <see cref="Profile.Icons"/> 하나다 ⓔ 빨간 줄 0.
        /// </summary>
        [UnityTest]
        public IEnumerator ArenaPortraitsUseTheSameFrameAsTheProfile()
        {
            yield return Boot();
            _app.Save.ProfileIcon = Profile.Icons[2];        // 기본값이 아닌 초상을 골라 둔다 — 기본값이면 «따라왔다» 를 못 가른다
            _app.ShowScreen("events"); yield return Frames(2);
            var ev = _app.Current as EventsScreen; Assert.IsNotNull(ev, "이벤트 화면");
            ev.ShowPage(EventsScreen.PageArena); yield return Frames(3);
            var ar = UiKit.Find(_app.Current.Root, "Page:arena"); Assert.IsNotNull(ar, "아레나 입장 페이지(23)");

            // ⓐ 시상대 초상 셋 — 프로필 프레임 조각 + 그 안의 초상 · 옛 물건 칸(ItemFrame_01)은 한 칸도 없다
            for (int i = 1; i <= 3; i++)
            {
                var p = UiKit.Find(ar, "Portrait:" + i); Assert.IsNotNull(p, "시상대 초상 " + i);
                Assert.IsTrue(HasProfileFrame(p), "시상대 초상 " + i + " 은 프로필 프레임 조각이어야 한다(주인 «실제 프로필 프레임이랑 디자인이 다르네»)");
                Assert.IsNotNull(UiKit.Find(p, Profile.FaceName), "시상대 초상 " + i + " 안에 초상 아이콘");
                Assert.IsFalse(GearUi.HasItemFrame(p), "시상대 초상 " + i + " 에 옛 물건 칸(ItemFrame_01)이 남으면 안 된다");
            }

            // ⓑ 1위는 «나» — 내가 고른 프레임 색·초상이 그대로 선다(그림을 맞댄다)
            var me = UiKit.Find(ar, "Portrait:1");
            Assert.IsNotNull(UiKit.Find(me, Profile.FrameKey(_app.Save)), "1위 프레임 = 내가 고른 프로필 프레임 색");
            var top = UiKit.Find(_app.Current.Root, "Avatar"); Assert.IsNotNull(top, "탑바 아바타 칸");
            Assert.AreEqual(FaceSprite(top), FaceSprite(me), "1위 초상과 탑바 아바타는 같은 그림이어야 한다(둘 다 «프로필에서 고른 초상»)");
            Assert.AreEqual(_app.Assets.Sprite(Profile.Icons[2]), FaceSprite(me), "그 그림이 내가 고른 그것이다");
            Assert.AreEqual(0, UnityEngine.Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length,
                "아레나에도 내 캐릭터 그림은 없다(T262 ⓐ 와 같은 까닭 · 주인 «플레이어 이미지 말고»)");

            // ⓑ' 순위 줄(4위~)도 같은 조각이다 — 주인 지시 2항은 시상대와 «순위 줄» 을 같이 든다
            {
                var r4 = UiKit.Find(ar, "RankRow:4"); Assert.IsNotNull(r4, "순위 줄 4위");
                var f4 = UiKit.Find(r4, "Face"); Assert.IsNotNull(f4, "순위 줄 초상");
                Assert.IsTrue(HasProfileFrame(f4), "순위 줄 초상도 프로필 프레임 조각(조각이 달고 온 제 그림이 아니다)");
                Assert.AreEqual(_app.Assets.Sprite(Profile.Icons[0]), FaceSprite(f4), "4위 더미 초상 = 목록 첫 아이콘");
            }

            // ⓒ·ⓓ 도전 팝업(24) 줄 초상 — 같은 조각이고, 그림은 «프로필이 고르는 그 넷»(정본 하나)에서 온다
            var btn = UiKit.Find(ar, "ChallengeBtn"); Assert.IsNotNull(btn, "도전 버튼");
            btn.GetComponent<Button>().onClick.Invoke(); yield return Frames(2); Canvas.ForceUpdateCanvases();
            var ov = _app.Overlay.Root;
            for (int i = 0; i < 5; i++)
            {
                var face = UiKit.Find(UiKit.Find(ov, "FoeRow:" + i), "Face"); Assert.IsNotNull(face, "상대 줄 초상 " + i);
                Assert.IsTrue(HasProfileFrame(face), "상대 줄 초상 " + i + " 도 프로필 프레임(주인 «도전 부분 팝업도 마찬가지»)");
                Assert.IsFalse(GearUi.HasItemFrame(face), "상대 줄 초상 " + i + " 에 옛 물건 칸이 남으면 안 된다");
                // ⚠ 줄 번호가 아니라 «순위» 로 고른다(T262 3항 · 결정 813) — 도전 팝업의 줄 i 는 순위 i+2 다.
                //    이 줄 자신이 ⓑ 회차에 «i % 4» 로 적혀 있었고 3항이 규칙을 바꾸자 그대로 빨개졌다:
                //    내가 «계약을 뒤집으면 자를 같이 고쳐라» 를 적어 둔 그 다음 커밋에서 내가 그것을 어겼다(결정 825).
                Assert.AreEqual(_app.Assets.Sprite(Profile.DummyIcon(i + 2)), FaceSprite(face),
                    "더미 초상은 «순위 하나가 언제나 같은 얼굴» 이다 — 23 목록과 24 팝업이 같은 «도전자 N» 에 다른 얼굴을 내면 안 된다(T262 3항)");
            }

            _log.AssertNoRed("T262 ⓑ 아레나 초상 프레임");
            yield return Shutdown();
        }

        /// <summary>칸 안에 «프로필 프레임» 조각이 서 있는가(색은 안 따진다 — 조각 계열만 본다).</summary>
        static bool HasProfileFrame(Transform cell)
        {
            if (cell == null) return false;
            foreach (var t in cell.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name.StartsWith(Profile.FrameKeyPrefix, StringComparison.Ordinal)) return true;
            return false;
        }
        /// <summary>칸 안 초상 그림(<see cref="Profile.FaceName"/>)의 스프라이트 — 없으면 null.</summary>
        static Sprite FaceSprite(Transform cell)
        {
            var f = UiKit.Find(cell, Profile.FaceName);
            var im = f != null ? f.GetComponent<Image>() : null;
            return im != null ? im.sprite : null;
        }

        /// <summary>
        /// T96-profile 2단계 — 아바타 팝업 제목(= 지금 내 이름)을 누르면 주인 지목 <c>Social_Profile_Nickname</c> 이 뜨고,
        /// TMP 입력칸이 uGUI <see cref="InputField"/> 로 서 있어 이름을 지을 수 있다.
        /// ⓐ 제목이 «Avatar» 가 아니라 «프로필 선택»(T370 1항 · 종전에는 내 이름이었다) ⓑ 눌러서 열리는 조각 = <c>ui.profileNick</c> · 입력칸·확인·글자 수가 다 있다
        /// ⓒ 2자 미만이면 «확인» 이 흐리고 안 눌린다 ⓓ 지으면 세이브에 남고 아바타 팝업으로 돌아온다 ⓔ 빨간 줄 0.
        /// </summary>
        /// <summary>
        /// T370 3항 — <b>목록 자체</b>를 잰다(화면을 안 켜고). ⓐ 여덟 장 이상 ⓑ 중복 0 ⓒ 앞 넷은 종전 그대로(이미 고른 사람의 초상이 안 바뀐다)
        /// ⓓ <b><see cref="Profile.Icons"/> 는 안 늘었다</b> — 그 배열은 아레나 더미의 얼굴을 정하는 자라(<see cref="Profile.DummyIcon"/> · T262 3항)
        /// 길이가 바뀌면 <c>rank % n</c> 이 통째로 달라져 22~26·33·34 의 얼굴이 한꺼번에 조용히 바뀐다.
        /// <b>이 자가 그 함정을 이름으로 지킨다</b> — «늘려라» 는 지시를 받은 다음 사람이 그 배열에 손대면 여기서 먼저 운다.
        /// </summary>
        [Test]
        public void FaceListIsLongEnoughAndDoesNotMoveTheArenaFaces()
        {
            Assert.AreEqual(9, Profile.Faces.Length, "고를 수 있는 초상은 정확히 아홉 장이다(T474 · 주인 «프로필 이미지 9개로 해줘 · 12개 말고» · T370 «4개밖에 없던데 좀 늘려봐라» 의 뒤)");
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var k in Profile.Faces)
            {
                Assert.IsFalse(string.IsNullOrEmpty(k), "빈 키 0");
                Assert.IsTrue(seen.Add(k), "같은 초상이 두 번 있으면 안 된다: " + k);
            }
            Assert.AreEqual(4, Profile.Icons.Length,
                            "아레나 더미 얼굴 목록은 넷 그대로다 — 늘리면 DummyIcon 의 rank % n 이 달라져 22~26·33·34 의 얼굴이 한꺼번에 바뀐다(T262 3항)");
            for (int i = 0; i < Profile.Icons.Length; i++)
                Assert.AreEqual(Profile.Icons[i], Profile.Faces[i], "앞 넷은 종전 그대로 — 이미 고른 사람의 초상이 안 바뀐다");
        }

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
            // T370 1항(주인 «프로필 선택 부분에 꼬마기사라고 타이틀 뜨지 말고 프로필 선택이라 떠야지») —
            //   제목은 이제 이름이 아니라 «프로필 선택» 이다. **누르는 길(이름 바꾸기 입구)은 그대로**라 아래 ⓑ 는 안 바뀐다.
            Assert.AreEqual(Profile.AvatarTitle, nickLabel.text, "제목은 «프로필 선택»(이름이 아니다 · T370 1항)");

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
            // T370 1항 뒤로 **제목은 이름이 아니다** — 지운 것이 아니라 **재는 대상을 옮긴다**:
            //   «지은 이름이 실제로 반영된다» 는 바로 위 `_app.Save.Nick` 이 이미 재고 있고,
            //   이 줄이 재던 나머지(«돌아온 팝업이 제 자리에 섰다»)는 제목이 그 팝업의 것인가로 잰다.
            Assert.AreEqual(Profile.AvatarTitle, back.GetComponentInChildren<TMP_Text>(true).text, "돌아온 곳은 아바타 팝업이다(제목 «프로필 선택» · T370 1항)");

            _log.AssertNoRed("T96-profile 이름 바꾸기");
            yield return Shutdown();
        }
    }
}
