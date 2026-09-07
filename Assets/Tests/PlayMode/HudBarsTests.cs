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
    /// T35 — 주인 강조 «전투 화면의 HP 바·실드 바는 메인 게임화면(02_battle.jpg·03_battle_enemy.jpg)처럼»:
    /// ⓐ HUD 아래 <b>바 3개 한 줄</b>(EXP · ❤ HP · 🛡 실드) 각각 «현재/최대» 숫자가 바 안에 · Slider 값 = 비율
    /// ⓑ 플레이어 <b>발밑 2단 바</b>(빨강 HP 위 · 파랑 실드 아래) 각 단 안에 흰 숫자 · 실드 0 이면 파란 단 숨김 · 바 폭은 캐릭터 배율(2/3)
    /// ⓒ 적 발밑 빨간 숫자 바(적은 실드 없음) · 적 조우 시 <c>Engaged</c> → 진행바 주황
    /// ⓓ 스탯 8칸(아이콘 · 이름 · 값) · 상단 pill(처치 수 = 엔진 Kills)
    /// 빨간 줄 0 은 <see cref="PlayLog"/>(ROUTINE §1).
    /// </summary>
    public class HudBarsTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; Assert.IsNotNull(_app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");
            yield return Frames(2);
            _log.AssertNoRed("부팅");
        }

        /// <summary>바(Slider_02 조각)의 숫자 글자 — EXP 바의 왼쪽 캡 라벨(«EXP»)은 뺀다.</summary>
        static Text BarText(Transform root, string name)
        {
            var b = UiKit.Find(root, name); Assert.IsNotNull(b, name); Assert.IsNotNull(b.GetComponent<Slider>(), name + " 는 Slider 조각");
            foreach (var t in b.GetComponentsInChildren<Text>(true)) if (t.text != "EXP") return t;
            return null;
        }
        static float BarValue(Transform root, string name) => UiKit.Find(root, name).GetComponent<Slider>().value;

        [UnityTest]
        public IEnumerator ThreeBarsFootBarsEnemyBarsAndStats()
        {
            yield return Boot();
            _app.StartBattle(1); yield return Frames(3);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G, "전투 상태"); var P = G.P;
            var hud = bs.Root; var W = bs.World; Assert.IsNotNull(W, "월드");
            _log.AssertNoRed("전투 진입");

            // ⓐ 바 3개 한 줄 — 숫자가 바 안에(«현재/최대») · Slider 값 = 비율 · 자리 = 표 ②(같은 y · 같은 두께)
            var exp = BarText(hud, "Bar:EXP"); var hp = BarText(hud, "Bar:HP"); var sh = BarText(hud, "Bar:SH");
            Assert.IsNotNull(exp, "EXP 바 숫자"); Assert.IsNotNull(hp, "HP 바 숫자"); Assert.IsNotNull(sh, "실드 바 숫자");
            int need = _app.Data.Tune.ExpNeed(P.Level);
            Assert.AreEqual($"{P.Exp}/{need}", exp.text, "EXP 바 안 «현재/필요»");
            Assert.AreEqual($"{UiKit.Fmt(W.ShownHp)}/{UiKit.Fmt(P.MaxHp)}", hp.text, "HP 바 안 «현재/최대»");
            Assert.AreEqual(P.MaxSh > 0 ? $"{UiKit.Fmt(W.ShownSh)}/{UiKit.Fmt(P.MaxSh)}" : "실드 없음", sh.text, "실드 바 안 «현재/최대»");
            Assert.AreEqual(P.MaxHp > 0 ? (float)(W.ShownHp / P.MaxHp) : 0f, BarValue(hud, "Bar:HP"), 0.01f, "HP 바 값");
            foreach (var t in hud.GetComponentsInChildren<Text>(false)) if (t.text == "EXP") { Assert.IsTrue(t.transform.IsChildOf(UiKit.Find(hud, "Bar:EXP")), "«EXP» 라벨은 EXP 바 왼쪽 캡"); break; }
            var re = (RectTransform)UiKit.Find(hud, "Bar:EXP"); var rh = (RectTransform)UiKit.Find(hud, "Bar:HP"); var rs = (RectTransform)UiKit.Find(hud, "Bar:SH");
            Assert.AreEqual(re.anchorMin.y, rh.anchorMin.y, 1e-3f, "세 바는 같은 줄"); Assert.AreEqual(rh.anchorMin.y, rs.anchorMin.y, 1e-3f, "세 바는 같은 줄");
            Assert.AreEqual(1f - Layout.HudHp.Y / 100f, rh.anchorMax.y, 1e-3f, "바 줄 = 표 ② HP 바 y");
            Assert.Less(re.anchorMax.x, rh.anchorMin.x + 1e-3f, "EXP → HP 순서"); Assert.Less(rh.anchorMax.x, rs.anchorMin.x + 1e-3f, "HP → 실드 순서");

            // ⓑ 플레이어 발밑 2단 — 빨강 HP(숫자) 위 · 파랑 실드(숫자) 아래 · 실드 0 이면 파란 단 숨김 · 바 폭 = 표 폭 × 2/3
            Assert.IsNotNull(W.PlayerHpBar, "플레이어 HP 바"); Assert.IsTrue(W.PlayerHpBar.gameObject.activeSelf, "HP 단 보임");
            Assert.IsNotNull(W.PlayerHpText, "HP 단 숫자(팝 층 uGUI)"); Assert.IsTrue(W.PlayerHpText.gameObject.activeSelf, "HP 숫자 보임");
            // 발밑 숫자는 레퍼런스 02·03 처럼 «천 단위 콤마 없이»(T125 ⓑ 회차 1 · BattleWorld.FootNum) — K/M 꼬리표는 Fmt 그대로 남는다
            Assert.AreEqual(UiKit.Fmt(System.Math.Ceiling(W.ShownHp)).Replace(",", ""), W.PlayerHpText.text, "HP 단 숫자 = 표시 체력(콤마 없이)");
            Assert.AreEqual(P.MaxSh > 0, W.PlayerShBar.gameObject.activeSelf, "실드 단은 실드가 있을 때만");
            if (P.MaxSh > 0) { Assert.IsTrue(W.PlayerShText.gameObject.activeSelf, "실드 숫자 보임"); Assert.AreEqual(UiKit.Fmt(System.Math.Ceiling(W.ShownSh)).Replace(",", ""), W.PlayerShText.text, "실드 단 숫자(콤마 없이)"); }
            else Assert.IsTrue(W.PlayerShText == null || !W.PlayerShText.gameObject.activeSelf, "실드 0 이면 파란 단의 숫자도 숨김");
            Assert.Less(W.PlayerShBar.transform.position.y, W.PlayerHpBar.transform.position.y, "파란 단은 빨간 단 아래");
            Assert.AreEqual(WorldCam.PctW(Layout.PlayerFootBarW) * Layout.FootBarScale, W.PlayerHpBar.size.x, 1e-3f, "HP 단 폭 = 표 폭 × FootBarScale(T63-battle · 숫자 36 이 들어가게)");
            // 발밑 숫자(T63-battle · T125 ⓑ 회차 3): 크기는 «바 높이에서 잰 값»(BattleWorld.FootFontSize) 그대로 — T63 보조 하한(36)을 일부러 벗어난다.
            // 왜 = 레퍼런스 02 는 숫자 잉크가 바 높이의 0.50 인데 우리는 Aux 하한에 끌려 올라가 0.82 였고, 그래서 숫자가 단을 덮어 채움색이 안 보였다(결정 361).
            Assert.AreEqual(BattleWorld.FootFontSize, W.PlayerHpText.fontSize, "발밑 숫자 크기 = 바 높이에서 잰 값(Aux 하한 아님)");
            Assert.Less(W.PlayerHpText.fontSize, TextSize.Aux, "발밑 숫자는 보조 하한보다 작다 — 이 자리만의 T63 예외(결정 361)");
            Assert.GreaterOrEqual(W.PlayerHpText.fontSize, 26, "그래도 배지급 하한(BattleWorld.MinFootFont 26) 아래로는 안 내려간다");
            Assert.LessOrEqual(W.PlayerHpText.fontSize, UiKit.FrameH * Layout.FootBarH / 100f * 0.8f, "발밑 숫자 ≤ 단 높이 × 0.8 — 넘으면 숫자가 단을 덮어 빨강·파랑 채움이 안 보인다(결정 361)");
            Assert.GreaterOrEqual(W.PlayerHpText.rectTransform.rect.height, W.PlayerHpText.preferredHeight - 1f, "발밑 숫자 칸 높이 ≥ 선호 높이(잘림 0)");
            Assert.LessOrEqual(W.PlayerHpText.preferredWidth, W.PlayerHpBar.size.x * WorldCam.PPU * (UiKit.FrameW / WorldCam.LayoutW) + 1f, "발밑 숫자 «" + W.PlayerHpText.text + "» 가 바 폭 안에");
            Assert.AreEqual(W.PlayerHpBar.size.x, W.PlayerShBar.size.x, 1e-3f, "두 단은 같은 폭"); Assert.AreEqual(W.PlayerHpBar.size.y, W.PlayerShBar.size.y, 1e-3f, "두 단은 같은 높이");
            Assert.AreEqual(WorldCam.PctH(Layout.FootBarH), W.PlayerHpBar.size.y, 1e-3f, "단 높이 = FootBarH");
            // 숫자 글자는 바 한가운데(프레임 px ↔ 월드 변환이 Pop 과 같음)
            {
                var p = W.PlayerHpBar.transform.position; float lx = p.x * WorldCam.PPU + WorldCam.LayoutW / 2f; float yFrac = 0.5f - p.y * WorldCam.PPU / WorldCam.LayoutH;
                var ap = W.PlayerHpText.rectTransform.anchoredPosition;
                Assert.AreEqual(lx * (UiKit.FrameW / WorldCam.LayoutW), ap.x, 0.5f, "HP 숫자 x = 바 중심"); Assert.AreEqual((1f - yFrac) * UiKit.FrameH, ap.y, 0.5f, "HP 숫자 y = 바 중심");
            }
            _log.AssertNoRed("바 3개 · 발밑 2단");

            // ⓓ 스탯 8칸(아이콘 · 이름 · 값) · 상단 pill 처치 수 = 엔진 Kills
            int cells = 0;
            foreach (var t in hud.GetComponentsInChildren<Transform>(false))
                if (t.name.StartsWith("stat:")) { cells++; Assert.IsNotNull(UiKit.Find(t, "ic"), t.name + " 아이콘"); Assert.IsFalse(string.IsNullOrEmpty(UiKit.Find(t, "Label").GetComponent<Text>().text), t.name + " 이름"); Assert.IsFalse(string.IsNullOrEmpty(UiKit.Find(t, "Value").GetComponent<Text>().text), t.name + " 값"); }
            Assert.AreEqual(BattleScreen.StatDefs.Length, cells, "스탯 8칸");
            Assert.AreEqual(G.Kills.ToString(), UiKit.Find(hud, "Pill:kills").GetComponentInChildren<Text>(true).text, "처치 수 pill");
            Assert.AreEqual(0f, (float)BattleScreen.ChapterProgress(G), 1e-3f, "시작 직후 진행 0");

            // ⓒ 첫 웨이브까지 걷는다(엔진 틱은 dt 로 · 배속 3) → 적 발밑 빨간 숫자 바 · 조우 중 Engaged · 진행바 주황
            Time.timeScale = 3f;
            float t0 = Time.realtimeSinceStartup;
            while (W.EnemyBarCount == 0 && !G.Over && Time.realtimeSinceStartup - t0 < 25f) { if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; } yield return null; }
            Time.timeScale = 1f;
            Assert.Greater(W.EnemyBarCount, 0, "25초 안에 첫 웨이브 적이 화면에 들어와 발밑 바가 보여야 한다");
            Assert.IsTrue(W.EnemyBarTextsConsistent(), "적 바마다 안에 흰 숫자 = 표시 체력");
            Assert.IsTrue(W.Engaged, "살아 있는 적이 화면 안 = 조우 중");
            yield return Frames(1);
            var fill = UiKit.Find(hud, "Bar:Progress").GetComponent<Slider>().fillRect.GetComponent<Image>();
            Assert.AreEqual(Palette.Orange, fill.color, "조우 중 진행바는 주황");
            Assert.AreEqual((float)BattleScreen.ChapterProgress(G), BarValue(hud, "Bar:Progress"), 0.01f, "진행바 값 = 노드 진행");
            Assert.AreEqual(G.Kills.ToString(), UiKit.Find(hud, "Pill:kills").GetComponentInChildren<Text>(true).text, "처치 수 pill 갱신");
            _log.AssertNoRed("적 조우");

            // 로비로 — 월드와 함께 발밑 숫자(팝 층)도 사라진다 · 빨간 줄 0
            _app.Overlay.Close(); _app.ShowScreen("lobby"); yield return Frames(2);
            Assert.AreEqual("lobby", _app.Current.Name);
            foreach (var t in _app.UiCanvas.GetComponentsInChildren<Text>(true)) Assert.IsFalse(t.name.StartsWith("FootTxt:"), "발밑 숫자 글자가 남아 있다: " + t.name);
            _log.AssertNoRed("로비 복귀");
        }
    }
}
