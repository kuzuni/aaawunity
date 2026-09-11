using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T455 ⑥ — <b>진행도가 높은 세이브</b>로 화면을 열고 가독성 자를 한 번 더 돌린다(«알리는 자» · 결정 493).
    /// <para>
    /// ⚑⚑ <b>이 자가 사는 까닭은 «한 줄» 이 아니라 눈먼 자리다.</b> 화면 가독성 자 전부가 <b>초반 세이브</b>에서 돈다 —
    /// 실측(T455 ④): <see cref="TextSizeGateTests"/>·<c>UiShotsTests</c>·<c>BorderGateTests</c>·<c>PercentGateTests</c> 는 <c>Gold = 11540</c>,
    /// <c>UiSmokeTests</c> 는 100000, 나머지 넷은 <b>0</b> 이다. <c>SlotLvMax</c> 를 세우는 자는 EditMode 둘뿐이고 <b>둘 다 화면을 안 연다</b>.
    /// ⇒ <b>길이가 진행도에 따라 자라는 글자는 구조적으로 자의 눈 밖</b>이고, 그것은 «한 줄을 고치면 되는 것» 이 아니라 <b>재는 자리가 없는 것</b>이다.
    /// </para>
    /// <para>
    /// 재는 것: <c>gear.json</c> 의 <c>slot.costTable</c> 은 <b>150칸</b>이고 마지막이 <b>1.556e33</b> 인데
    /// 짧은 꼴 사다리(<c>ShortNum</c>)의 꼭대기는 <b>T = 1e12</b> 다 — 위에 칸이 없으니 1e15 를 넘는 순간부터
    /// <b>열 배마다 한 자씩 끝없이 자란다</b>. 그 글자가 서는 자리가 장비 세부(07)의 비용 줄(<c>GearUi.CostRow</c> · 폭 40% 한 줄)이다.
    /// </para>
    /// <para>
    /// ⚠ <b>일부러 «막는 자» 로 안 세웠다</b>(<see cref="Strict"/> = false). 등재 글 ⑧ⓐ 가 «47자가 그 줄에서 <b>실제로</b> 잘리거나
    /// 하한을 깨는가 — <b>안 쟀다</b>» 로 남긴 자리라, 여기서 막으면 <b>재 보지도 않은 값으로 모두의 배포를 세우는</b> 것이 된다
    /// (결정 930·1007 이 값을 치른 그 갈래 · <c>ClipStrict</c>·<c>GlyphStrict</c> 가 지나온 길과 같다).
    /// <b>이 회차가 사는 것은 판정이 아니라 «그 수가 로그에 놓인다» 하나</b>다 — 고침(T455 ⑤)을 든 사람이 그 수를 읽고 <see cref="Strict"/> 를 켠다.
    /// </para>
    /// <para>
    /// ⚠ <b>고침은 이 절이 안 한다</b> — ⑤ 의 셋(사다리 늘리기 · 지수 꼴 · 그 줄만 좁히기)은 전부 <c>ShortNum.cs</c>·<c>GearUi.cs</c> 이고
    /// 그 둘이 통째로 <c>T454.lock</c>(워커 A · 살아 있다)의 범위다. 한 병을 두 커밋으로 쪼개지 않는다(결정 1263 ④).
    /// </para>
    /// </summary>
    public class HighLevelTextGateTests
    {
        /// <summary>
        /// 켜면 «넘침·하한 미달 0» 을 <b>막는 자</b>로 단언한다. <b>지금은 꺼 둔다</b> — 위 ⚠ 를 보라.
        /// <para>켤 자격: T455 ⑤ 의 고침이 서고, 이 자가 찍는 표에서 <b>07 의 넘침·하한 미달이 0</b> 이 된 회차.</para>
        /// </summary>
        /// <remarks>⚠ <c>const</c> 가 아니라 <c>static readonly</c> 인 까닭: <c>const false</c> 면 아래 판정 블록이 <b>CS0162(닿지 않는 코드)</b> 경고를 낸다.
        /// 켜고 끄는 자리가 경고를 내면 다음 사람이 «경고를 없애려고» 블록을 지운다 — 그러면 켤 것이 없어진다.</remarks>
        public static readonly bool Strict = false;

        App _app; PlayLog _log;
        readonly List<TextAudit.Row> _rows = new List<TextAudit.Row>();

        [SetUp] public void SetUp() { _log = new PlayLog(); _rows.Clear(); }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
        }
        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        /// <summary>한 화면 — 연출을 끝내고 레이아웃을 굳힌 뒤 모든 루트 캔버스의 활성 Text 를 모은다(<see cref="TextSizeGateTests"/> 와 같은 손).</summary>
        IEnumerator Check(string name)
        {
            UiKit.CompleteAllTweens();
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (cv != null && cv.isRootCanvas) _rows.AddRange(TextAudit.Collect(name, cv.transform));
            yield return Frames(1);
        }

        GearItem Give(string part, int rar = 0, int plus = 0)
        {
            foreach (var t in _app.Data.Gear.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, rar, plus); _app.Save.Inv.Add(g); return g; }
            return null;
        }

        [UnityTest]
        public IEnumerator 끝까지_키운_세이브에서도_장비_세부의_글자가_읽힌다()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;

            // ⚑ 수를 자에 안 박는다 — «끝» 은 표(gear.json slot.lvMax)가 정한다. 표가 줄면 이 자도 같이 줄어든다.
            int maxLv = D.Gear.SlotLvMax;
            Assert.Greater(maxLv, 1, "표가 «끝» 을 말해야 이 자가 뜻이 있다(gear.json slot.lvMax)");
            int lv = maxLv - 1;                       // MAX 면 비용 줄이 «슬롯 MAX» 글자로 바뀐다 — 수가 서는 마지막 칸이 여기다
            double cost = D.Gear.SlotCost(lv);
            Assert.Greater(cost, 0, "그 칸의 비용이 0 이면 잴 것이 없다");

            var part = D.Gear.Parts[0];
            var g = Give(part, rar: 1, plus: 1);
            Assert.IsNotNull(g, "장비 한 벌");
            S.Eq[part] = g.Uid;
            S.Slots[part] = lv;
            S.Gold = cost;                            // 양쪽 다 가장 긴 꼴이 되는 자리 — 실제로 그 칸을 사려면 이만큼 들고 있어야 한다

            _app.ShowScreen("gear"); yield return Frames(2);
            GearUi.OpenDetail(_app, g, null);
            yield return Check("07_gear_detail_maxed");

            // ⓐ 잰 것이 진짜인가 — 이 단언이 없으면 아래 표가 «빈 화면» 을 재고도 조용하다(T278 의 결).
            Assert.Greater(_rows.Count, 5, "세부 팝업의 글자가 거의 안 모였다(수집 실패 — 팝업이 안 열렸을 수 있다)");
            TextAudit.Row costRow = null;
            foreach (var r in _rows) if (r.Path != null && r.Path.EndsWith("CostText")) { costRow = r; break; }
            Assert.IsNotNull(costRow, "비용 줄 글자(«CostText» · GearUi.CostRow)를 못 찾았다 — 이 자가 재려는 그 줄이다");
            Assert.Greater(costRow.Text.Length, 12,
                           "비용 줄이 짧다 — 끝까지 키운 세이브를 세운 손이 안 먹혔다(슬롯 Lv " + lv + " · 비용 " + cost + "): " + costRow);

            // ⓑ 표를 찍는다 — 이 회차가 사는 것이 이것이다(판정이 아니라 수).
            var floorBad = new List<string>(); var fitBad = new List<string>(); var clipped = new List<string>();
            foreach (var r in _rows)
            {
                if (r.FloorBad) floorBad.Add(r.ToString());
                if (r.BestFitBad) fitBad.Add(r.ToString());
                if (r.Clipped) clipped.Add(r.ToString());
            }
            Debug.Log("[HighLevelTextGate] 슬롯 Lv " + lv + "/" + maxLv + " · 비용 " + cost.ToString("0.###e+0") +
                      " · 비용 줄 «" + costRow.Text + "» (" + costRow.Text.Length + "자)\n" +
                      "[HighLevelTextGate] 글자 " + _rows.Count + "개 · 하한 미달 " + floorBad.Count +
                      " · bestFit 최소 미달 " + fitBad.Count + " · 넘침 " + clipped.Count +
                      " (보고만 · Strict=" + Strict + " · T455)\n" +
                      "[HighLevelTextGate] " + costRow);
            foreach (var s in clipped) Debug.Log("[HighLevelTextGate] ⚠넘침 " + s);
            foreach (var s in floorBad) Debug.Log("[HighLevelTextGate] ⛔하한 " + s);

            // ⓒ 판정 — 켤 때까지 안 막는다(위 ⚠).
            if (Strict)
            {
                Assert.AreEqual(0, floorBad.Count, "끝까지 키운 세이브에서 글자 하한 미달:\n" + string.Join("\n", floorBad));
                Assert.AreEqual(0, fitBad.Count, "끝까지 키운 세이브에서 bestFit 최소 미달:\n" + string.Join("\n", fitBad));
                Assert.AreEqual(0, clipped.Count, "끝까지 키운 세이브에서 잘림/넘침:\n" + string.Join("\n", clipped));
            }

            // ⓓ 켜고 끄는 것과 무관하게 늘 참이어야 하는 것 — 빨간 줄은 어느 세이브에서도 안 난다.
            _log.AssertNoRed("끝까지 키운 세이브의 장비 세부");
            _app.Overlay.Close(); yield return Frames(1);
            yield return Shutdown();
        }
    }
}
