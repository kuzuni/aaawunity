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
    /// T301(주인 2026-09-09 09:0X «프로필 이미지 바꿨는데 적용 안 되더라 해결하소» · T262 ⓐ 의 회귀) —
    /// <b>세이브의 초상 아이콘을 바꾸고 화면을 <c>Refresh</c> 하면 탑바 얼굴이 그것으로 바뀌는가.</b>
    /// <para>
    /// 왜 이 자가 따로 서는가 — <see cref="ProfileTests"/> 는 «탑바에 초상 아이콘이 <b>있다</b>» 를 재고 있었고,
    /// 그 물음은 <b>옛 얼굴이 남아 있어도 참</b>이다. 이 절이 고친 고장이 정확히 그 틈이었다:
    /// <c>Screens.AvatarFrame</c> 이 <b>테두리 색</b> 조각 이름으로 «이미 있다» 를 판정해, 색이 그대로면 얼굴을 안 갈아 끼웠다.
    /// «있다» 와 «맞다» 는 다른 물음이고, 그 둘이 갈리는 자리에 회귀가 숨는다.
    /// </para>
    /// ⓐ 아이콘을 바꾸고 <c>Refresh</c> → 탑바 얼굴 스프라이트가 <b>새 아이콘의 것</b> · ⓑ 테두리 색 조각은 <b>그대로</b>(고친 것이 얼굴뿐임을 못 박는다)
    /// · ⓒ 같은 아이콘으로 <c>Refresh</c> 를 두 번 더 불러도 얼굴 그림은 <b>하나</b>(부수고 다시 세우지 않는다).
    /// </summary>
    public class TopBarAvatarTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I;
            yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            if (_app != null) { if (_app.UiCanvas != null) UnityEngine.Object.Destroy(_app.UiCanvas.gameObject); UnityEngine.Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        Transform Avatar()
        {
            var a = UiKit.Find(_app.Current.Root, "Avatar");
            Assert.IsNotNull(a, "탑바 아바타 칸");
            return a;
        }

        /// <summary>탑바 얼굴 그림의 스프라이트(없으면 null).</summary>
        Sprite FaceSprite()
        {
            var f = UiKit.Find(Avatar(), Profile.FaceName);
            var im = f != null ? f.GetComponent<Image>() : null;
            return im != null ? im.sprite : null;
        }

        /// <summary>지금 살아 있는 얼굴 그림의 개수 — 부수고 다시 세우면 같은 프레임에 둘이 걸린다(그 사고가 이 절의 뿌리 중 하나다).</summary>
        int FaceCount()
        {
            int n = 0;
            foreach (var t in Avatar().GetComponentsInChildren<Transform>(true))
                if (t != null && t.name == Profile.FaceName) n++;
            return n;
        }

        [UnityTest]
        public IEnumerator ChangingTheProfileIconRepaintsTheTopBarFace()
        {
            yield return Boot();
            var S = _app.Save;
            var cat = _app.Assets; Assert.IsNotNull(cat, "AssetCatalog");

            // 기본 얼굴 = Icons[0] (CurrentIcon 의 기본값)
            Assert.AreEqual(cat.Sprite(Profile.Icons[0]), FaceSprite(), "처음에는 첫 초상");
            var frameBefore = Profile.FrameKey(S);

            // 팝업의 «선택» 이 하는 일 그대로 — 세이브에 아이콘을 남기고 화면을 Refresh 한다(색은 안 건드린다).
            S.ProfileIcon = Profile.Icons[2];
            _app.Current.Refresh(); yield return Frames(2);

            Assert.AreEqual(cat.Sprite(Profile.Icons[2]), FaceSprite(),
                "T301 — 아이콘을 바꾸고 Refresh 하면 탑바 얼굴이 그것이어야 한다(고치기 전에는 옛 얼굴로 남았다)");
            Assert.AreEqual(frameBefore, Profile.FrameKey(S), "테두리 색은 안 건드렸다");
            Assert.AreEqual(1, FaceCount(), "얼굴 그림은 하나");

            // 같은 아이콘으로 두 번 더 — 아무 일도 안 일어나야 한다(매 Refresh 마다 그림이 죽었다 살아나지 않는다).
            _app.Current.Refresh(); yield return Frames(1);
            _app.Current.Refresh(); yield return Frames(2);
            Assert.AreEqual(cat.Sprite(Profile.Icons[2]), FaceSprite(), "그대로");
            Assert.AreEqual(1, FaceCount(), "같은 얼굴이면 갈아 끼우지 않는다");

            // 되돌리기도 된다(한 방향으로만 되는 고침이 아니다).
            S.ProfileIcon = Profile.Icons[1];
            _app.Current.Refresh(); yield return Frames(2);
            Assert.AreEqual(cat.Sprite(Profile.Icons[1]), FaceSprite(), "다시 바꿔도 따라온다");
            Assert.AreEqual(1, FaceCount(), "갈아 낀 뒤에도 하나(옛 그림을 떼고 지운다)");

            yield return Shutdown();
        }
    }
}
