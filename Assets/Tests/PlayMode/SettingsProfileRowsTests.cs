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
    /// T156 — 설정 팝업의 <b>«프로필 아이콘»·«닉네임» 줄 둘</b>(주인 2026-09-07 07:4X «설정에 프로필 아이콘 설정, 닉네임 설정 있어야함»).
    /// <list type="bullet">
    /// <item>ⓐ 설정을 열면 줄이 <b>다섯</b>(음악·효과음·언어·프로필 아이콘·닉네임)이고 새 줄 둘에 «변경» 버튼이 있다.</item>
    /// <item>ⓑ «프로필 아이콘 → 변경» 이 아바타 팝업(<c>ui.profileAvatar</c>)을 연다.</item>
    /// <item>ⓒ «닉네임 → 변경» 이 이름 짓기 팝업(<c>ui.profileNick</c>)을 열고 <b>입력칸이 살아 있다</b>.</item>
    /// <item>ⓓ 새 줄 둘은 상자 «안» 이고 링크는 상자 «밖» 아래다(상자는 9.6%p 길어지되 <b>위로</b> 늘어 링크 자리는 레퍼런스 그대로 · 결정 445).</item>
    /// <item>ⓔ 빨간 줄 0(<see cref="PlayLog"/> · T11 규약).</item>
    /// </list>
    /// 새로 만든 기능은 0 이다 — 두 팝업은 T96-profile 이 이미 만들었고 <b>상단 재화 바의 아바타 입구도 그대로</b>다(여기서는 입구를 하나 더 낸 것).
    /// </summary>
    public class SettingsProfileRowsTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        static bool ClickNamed(Transform root, string name)
        {
            var t = UiKit.Find(root, name); var b = t != null ? t.GetComponent<Button>() : null;
            if (b == null || !b.IsInteractable()) return false;
            b.onClick.Invoke(); return true;
        }
        /// <summary>
        /// 화면에 그려진 «월드» 사각형(y 는 위로 증가) — 앵커 % 로 재면 안 된다: 줄은 <b>상자의 자식</b>이라 그 % 는 «상자 안 %» 이고
        /// 링크는 <c>Root</c> 의 자식이라 «프레임 %» 다(CI #333 이 이 실수로 빨갰다 · 결정 462). 월드 사각형은 부모가 달라도 같은 자로 잰다.
        /// </summary>
        static Rect WorldRect(RectTransform rt)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        [UnityTest]
        public IEnumerator SettingsHasProfileAndNicknameRowsThatOpenTheirPopups()
        {
            yield return Boot();
            _app.ShowScreen("lobby"); yield return Frames(2);

            // ⓐ 줄 다섯 + 새 줄 둘의 «변경» 버튼
            _app.Overlay.Settings(); yield return Frames(2);
            var ov = _app.Overlay.Root;
            foreach (var n in new[] { "BGM", "SFX", "Language", "Profile", "Nickname", "LangBtn", "ProfileBtn", "NickBtn" })
                Assert.IsNotNull(UiKit.Find(ov, n), "설정 조각 " + n);
            Assert.AreEqual("프로필 아이콘", UiKit.Find(UiKit.Find(ov, "Profile"), "Text").GetComponent<Text>().text, "프로필 줄 라벨");
            Assert.AreEqual("닉네임", UiKit.Find(UiKit.Find(ov, "Nickname"), "Text").GetComponent<Text>().text, "닉네임 줄 라벨");

            // ⓓ 새 줄 둘은 상자 «안» · 상자 밖 아래 링크와 안 겹친다
            var box = UiKit.Find(ov, "ui.popup") as RectTransform; Assert.IsNotNull(box, "설정 = 공통 팝업 상자");
            Canvas.ForceUpdateCanvases();
            var boxR = WorldRect(box); float eps = boxR.height * 0.02f;
            foreach (var n in new[] { "Profile", "Nickname" })
            {
                var r = WorldRect((RectTransform)UiKit.Find(ov, n));
                Assert.GreaterOrEqual(r.yMin, boxR.yMin - eps, n + " 줄은 상자 안(아래 끝)");
                Assert.LessOrEqual(r.yMax, boxR.yMax + eps, n + " 줄은 상자 안(위 끝)");
            }
            // 링크는 상자 «밖» 아래 = 상자 밑변보다 낮다(T156 회차 2 는 상자를 «위» 로 늘려 링크를 레퍼런스 자리에 그대로 두었다 · 결정 445)
            var privacy = WorldRect((RectTransform)UiKit.Find(ov, "Privacy"));
            Assert.LessOrEqual(privacy.yMax, boxR.yMin + eps, "링크는 상자 «밖» 아래");

            // ⓑ 프로필 아이콘 → 변경
            Assert.IsTrue(ClickNamed(ov, "ProfileBtn"), "«프로필 아이콘» 줄의 변경 버튼");
            yield return Frames(2);
            Assert.IsTrue(_app.Overlay.IsOpen, "팝업이 열려 있다");
            Assert.IsNotNull(UiKit.Find(_app.Overlay.Root, "ui.profileAvatar"), "아바타 팝업(T96-profile)이 열린다");
            _app.Overlay.Close(); yield return Frames(2);

            // ⓒ 닉네임 → 변경(입력칸이 살아 있다)
            _app.Overlay.Settings(); yield return Frames(2);
            Assert.IsTrue(ClickNamed(_app.Overlay.Root, "NickBtn"), "«닉네임» 줄의 변경 버튼");
            yield return Frames(2);
            var nick = UiKit.Find(_app.Overlay.Root, "ui.profileNick");
            Assert.IsNotNull(nick, "이름 짓기 팝업이 열린다");
            // 입력칸은 이름으로 찾는다(ProfileTests 와 같은 계약 · Adopt 가 TMP 입력칸을 uGUI InputField 로 갈아 끼운 것)
            var inputT = UiKit.Find(_app.Overlay.Root, Profile.NickInputName);
            Assert.IsNotNull(inputT, "이름 입력칸(" + Profile.NickInputName + ")");
            var input = inputT.GetComponent<InputField>();
            Assert.IsNotNull(input, "입력칸이 uGUI InputField 로 서 있다");
            Assert.IsTrue(input.IsActive() && input.IsInteractable(), "입력칸이 살아 있다");
            _app.Overlay.Close(); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "닫힌다");

            // 상단 재화 바의 아바타 입구는 그대로다(이 작업은 «입구를 하나 더» 낸 것 · 지시서 1항)
            var avatar = UiKit.Find(_app.Current.Root, "Avatar");
            Assert.IsNotNull(avatar, "상단 바 아바타(종전 입구)");

            _log.AssertNoRed("설정 프로필·닉네임 줄(T156)");
            yield return Shutdown();
        }
    }
}
