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
    /// T267 2단계 — 상자 «확률 정보» 팝업(주인 2026-09-09 «상점에 상자 부분에 인포 버튼 클릭 시 이런 게 떠야 함» · 레퍼런스 36·37).
    /// 규칙(구간·개별 확률)은 EditMode <c>GachaOddsTests</c> 가 본다 — 여기서 재는 것은 <b>화면이 그 수를 그대로 그리는가</b> 하나다.
    /// <para>
    /// ⚠ 이 자의 요점: **칸 수 = 뽑기가 고르는 목록 수** · **칸에 적힌 수 = `GachaOdds` 가 낸 개별 확률**.
    /// 화면이 제 나름대로 칸을 고르거나 수를 반올림하면 «적힌 확률이 거짓말» 이 되는데 그것은 빨간 줄도 자도 안 난다(결정 746 ②).
    /// </para>
    /// </summary>
    public class OddsPopupTests
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
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I; yield return Frames(2);
            _log.AssertNoRed("부팅");
        }

        /// <summary>
        /// 등장 연출(<see cref="UiKit.PopIn"/> 스케일 0.82 → 1)이 <b>끝났는가</b>. T288 ④ 로 고친 자리다.
        /// <para>
        /// ⚠ <b>`lossyScale` 을 1 과 견주면 안 된다</b> — 그 값에는 <b>캔버스 배율</b>이 곱해져 있다(CanvasScaler 가
        /// 기준 해상도에 맞춰 늘이고 줄인다). CI 러너의 창은 우리 기준 해상도가 아니라 배율이 1 이 아니고,
        /// 그래서 <b>연출이 멀쩡히 끝났는데도</b> «스케일 1 기대 · 실제 0.205» 로 빨갰다(run 675 실측 · 결정 838).
        /// 0.205 는 «연출 중간» 이 아니라 <b>그 런의 캔버스 배율 그 자체</b>였다.
        /// </para>
        /// 그래서 <b>캔버스 배율을 기준</b>으로 견준다 — 그러면 창 크기가 어떻든 «지역 스케일이 1 인가» 만 재어진다.
        /// </summary>
        float CanvasScaleY() => _app != null && _app.UiCanvas != null ? _app.UiCanvas.transform.lossyScale.y : 1f;
        /// <summary>연출이 끝날 때까지 트윈을 마저 돌린다(넉넉히 · 한 번의 `CompleteAll` 로 안 끝나는 판을 위해).</summary>
        IEnumerator Settle(Transform t)
        {
            float t0 = Time.realtimeSinceStartup;
            do
            {
                UiKit.CompleteAllTweens(); yield return null;
                if (t == null) yield break;
            } while (Mathf.Abs(t.lossyScale.y - CanvasScaleY()) > 1e-3f && Time.realtimeSinceStartup - t0 < 3f);
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++) { var r = Find(root.GetChild(i), name); if (r != null) return r; }
            return null;
        }
        static string TextOf(Transform root, string name)
        {
            var t = Find(root, name); var x = t != null ? t.GetComponentInChildren<TMP_Text>(true) : null;
            return x != null ? x.text : null;
        }

        [UnityTest]
        public IEnumerator PopupDrawsTheGradeSectionsAndTheNumbersComeFromTheData()
        {
            yield return Boot();
            var D = _app.Data;

            // 신화 상자 — 네 등급이 다 rate > 0 이라 구간이 제일 많다(희귀 상자는 둘뿐이라 «구간 0» 을 못 잡는다)
            var box = OddsPopup.Open(_app, "myth");
            yield return Frames(2);
            Assert.IsNotNull(box, "확률 팝업 상자");
            Assert.IsTrue(_app.Overlay.IsOpen, "팝업이 열려 있다");
            Assert.IsNotNull(Find(box, "OddsScroll"), "스크롤 창");
            Assert.IsNotNull(Find(box, "OddsFoot"), "바닥 안내 띠");

            // T288 — 두 이름 계약이 **같이** 선다(한쪽을 세우려고 다른 쪽을 덮었던 자리다).
            //   ⓐ 루트는 공통 팝업 프리팹 이름 그대로다 — UiSmokeTests 의 «정보 팝업 = 공통 팝업 문법» 이 이것을 잰다.
            //   ⓑ 표식 OddsBox 는 루트가 아니라 «꺼진 자식» 이라, 있어도 그리거나 막지 않는다.
            // 여기서 같이 재는 까닭: ⓐ 만 재는 자는 다른 파일에 있어서, 이 파일을 고치는 사람 눈에 안 들어온다.
            Assert.AreEqual(UiKit.PopupKeyPlain, box.name, "팝업 루트 이름 = 공통 팝업 프리팹 키(덮지 않는다)");
            var mark = Find(box, "OddsBox");
            Assert.IsNotNull(mark, "«확률 팝업이다» 표식");
            Assert.IsFalse(mark.gameObject.activeSelf, "표식은 꺼져 있다(그리지도 막지도 않는다)");

            var rows = GachaOdds.Of(D, "myth");
            int n = GachaOdds.ItemCount(D);
            Assert.Greater(rows.Count, 1, "신화 상자는 구간이 여럿이다");
            Assert.Greater(n, 0, "아이템 목록");

            // T320 ⓑ(주인 4항 «같은 것을 리본 제목 팝업 전부에») — 이 팝업은 «공통 리본 팝업» 의 대표로 선다.
            //   부르는 곳이 `Overlay.Box` 한 곳이라 여기서 서면 목록(퀘스트·출석·기프트·우편·던전·아레나·확률 …)이 같이 선다.
            //   ⚠ 재는 것은 px 이 아니라 **관계**다: «마스크 바닥 = 리본 바닥» · «빛판 가운데가 마스크 바닥보다 아래»(= 아래 절반이 잘린다).
            {
                var ribbon = Find(box, "ui.title.tangerine") as RectTransform;
                Assert.IsNotNull(ribbon, "공통 팝업 리본");
                var host = box.Find("TitleGlow") as RectTransform;
                Assert.IsNotNull(host, "리본 뒤 빛 담개(T320 ⓑ)");
                Assert.Less(host.GetSiblingIndex(), ribbon.GetSiblingIndex(), "빛은 리본 «뒤»(형제 순서 앞)");
                var gm = host.Find("Mask") as RectTransform;
                Assert.IsNotNull(gm, "사각 마스크");
                Assert.IsTrue(UiKit.HasLight(gm), "마스크 안 도는 빛살");
                Assert.IsTrue(UiKit.HasGlow(gm), "마스크 안 글로우 서클");
                float maskBottom = gm.anchoredPosition.y - gm.sizeDelta.y * 0.5f;   // 리본 사각형 기준
                Assert.AreEqual(-ribbon.rect.height * 0.5f, maskBottom, 1.5f, "마스크 바닥 = 리본 바닥(ⓐ 에서 뽑은 관계 · 늘림 앵커라 rect 로 잰다)");
                var gp = gm.Find(UiKit.LightMaskName) as RectTransform;
                Assert.IsNotNull(gp, "빛판");
                float plateCenterY = (gp.offsetMin.y + gp.offsetMax.y) * 0.5f;
                Assert.Less(plateCenterY, -gm.sizeDelta.y * 0.5f + 20f, "빛의 아래 절반이 잘린다(주인 «반 잘리는 식으로»)");
            }

            // T267 — 명판은 **상자 «안» 맨 위**에 붙고 상자 폭을 거의 다 쓴다(레퍼런스 36 · 표 ㊾).
            //   공통 팝업이 세워 주는 리본은 좁고 상자 «위로» 걸쳐 있어서, 자리를 다시 안 잡으면 그 행이 혼자 ✗ 다.
            //   px 이 아니라 «상자 안인가 · 상자 폭을 쓰는가» 두 가지를 잰다 — 표가 바뀌어도 이 뜻은 안 바뀐다.
            {
                var plate = Find(box, "ui.title.tangerine");
                Assert.IsNotNull(plate, "명판(공통 팝업 리본)");
                var prt = (RectTransform)plate;
                Assert.LessOrEqual(prt.rect.height, box.rect.height * 0.2f, "명판은 상자 한 귀퉁이다(상자를 덮지 않는다)");
                Assert.GreaterOrEqual(prt.rect.width, box.rect.width * 0.9f, "명판이 상자 폭을 거의 다 쓴다(레퍼런스 36)");
                // 상자 «안» = 명판 위 끝이 상자 위 끝보다 아래다. 상자 기준 지역 좌표로 잰다
                //   (anchoredPosition 은 늘어난 rect 에서 0 근처라 못 쓴다 — 늘림 앵커의 «중심 어긋남» 이라서다).
                float plateTop = prt.localPosition.y + prt.rect.height * (1f - prt.pivot.y);
                Assert.LessOrEqual(plateTop, box.rect.height * 0.52f, "명판 위 끝이 상자 «안» 이다(상자 위로 안 걸친다)");
            }

            // T267 — **그림 px 이 캔버스 px 로 옮겨졌는가**(결정 839). 레퍼런스 36(720×1560)에서 잰 세로 수를 그대로 쓰면
            //   캔버스 기준(2337)이 그림(1560)의 1.498배라 세로가 전부 2/3 로 눌린다 — §5 표 ㊾ 가 «구간 머리 h 4.3 → 2.9» 로
            //   그 눌림을 이미 가리키고 있었다. 여기서 재는 것은 **비율**이지 px 이 아니다: 잰 값(67/1560)이 화면에서 같은 몫을 차지하는가.
            //   px 을 베끼지 않는다(결정 555) — 그림의 수가 바뀌면 이 자도 같이 따라간다.
            {
                var head0 = Find(box, "Sec:" + rows[0].Rar);
                Assert.IsNotNull(head0, "맨 위 구간 머리");
                float share = ((RectTransform)head0).rect.height / UiKit.FrameH;   // 화면 높이에서 머리 띠가 차지하는 몫
                Assert.AreEqual(67f / 1560f, share, 0.003f, "구간 머리 띠 높이 몫 = 레퍼런스 36 실측(67/1560) — 그림 px 을 캔버스로 옮겨 쓴다");
            }

            // ⓙ(주인 2026-09-09 «아이템들이 비율이 실제 다른 곳이랑 다르네 · 아이콘이 걍 존나 크게 표시돼 있네»)
            //   — 파츠 아이콘(투구·무기·갑옷)은 인벤 칸과 **같은 조합**(`GearUi.FitIcon`)으로 앉아야 한다.
            //   ⚠ 조합을 빼면 아이콘이 프리팹 `Item` 크기 그대로 프레임을 꽉 채우므로 이 자가 바로 빨개진다.
            {
                int pi = -1;
                for (int i = 0; i < n; i++) if (GearLook.HasLook(D.Gear.AllTypes[i].Part)) { pi = i; break; }
                Assert.GreaterOrEqual(pi, 0, "파츠 아이콘을 쓰는 부위가 목록에 있다");
                var cell = Find(box, "Odds:" + rows[0].Rar + ":" + pi);
                var frame = (RectTransform)Find(cell, "ItemFrame_01");
                Assert.IsNotNull(frame, "물건 칸 조각");
                var icon = (RectTransform)Find(frame, "Item");
                Assert.IsNotNull(icon, "칸 안 그림");
                Assert.IsNotNull(icon.GetComponent<PartIconFit>(), "GearUi.FitIcon 을 지났다(인벤 칸과 같은 조합)");

                // 그리고 **그 문을 실제로 지나서 크기가 바뀌었다** — `PartIconFit` 은 프리팹 원래 값을 담아 두므로
                //   «담긴 값과 지금 값이 다르다» 가 곧 «맞춤이 돌았다» 다(파츠 아이콘이라 `Restore` 갈래가 아니다).
                //   ⚠ 여기서 px 로 «프레임을 안 넘는다» 를 재지 않는 까닭: `FitIcon` 이 맞추는 것은 **불투명 bbox** 라
                //   여백이 넓은 그림은 rect 가 프레임보다 커도 **보이는 그림은 안 넘친다**. 자가 rect 를 재면
                //   화면이 옳은데도 빨개진다 — 눈으로 볼 것은 `screens` 36 ↔ 06 을 나란히 놓는 쪽이다.
                var fit = icon.GetComponent<PartIconFit>();
                Assert.AreNotEqual(fit.Size, icon.sizeDelta, "프리팹 크기 그대로가 아니라 «같은 눈높이» 로 다시 잡혔다");
            }

            foreach (var r in rows)
            {
                var sec = Find(box, "Sec:" + r.Rar);
                Assert.IsNotNull(sec, "등급 " + r.Rar + " 구간");
                Assert.AreEqual(r.Name, TextOf(sec, "SecName"), "구간 이름은 표(gear.json rarName)에서 온다");
                StringAssert.Contains(OddsPopup.Pct(r.Percent), TextOf(sec, "SecRate"), "구간 확률 글자 = gacha.json rate");

                // 칸은 «뽑기가 고르는 목록» 그대로다 — 수도 개수도 화면이 따로 안 고른다
                for (int i = 0; i < n; i++)
                {
                    var cell = Find(box, "Odds:" + r.Rar + ":" + i);
                    Assert.IsNotNull(cell, "등급 " + r.Rar + " 칸 " + i);
                    Assert.AreEqual(OddsPopup.Pct(r.Each), TextOf(cell, "Pct"), "칸에 적힌 확률 = 등급 확률 ÷ 목록 수");
                }
                Assert.IsNull(Find(box, "Odds:" + r.Rar + ":" + n), "목록보다 많은 칸을 그리지 않는다");
            }

            // 없는 등급은 구간이 없다 — «전설 0.00%» 를 그리면 «나올 수 있는데 드물다» 로 읽힌다
            var rare = GachaOdds.Of(D, "rare");
            _app.Overlay.Close(); yield return Frames(2);
            var box2 = OddsPopup.Open(_app, "rare"); yield return Frames(2);
            Assert.IsNotNull(box2, "희귀 상자 확률 팝업");
            for (int rr = 0; rr < D.Gear.RarName.Length; rr++)
            {
                bool has = rare.Exists(x => x.Rar == rr);
                Assert.AreEqual(has, Find(box2, "Sec:" + rr) != null, "희귀 상자 등급 " + rr + " 구간은 rate > 0 일 때만 선다");
            }

            // 4항 — 칸을 누르면 «보기 전용» 세부 팝업이 뜨고 **아래 두 버튼이 없다**(주인 «아래 두 버튼만 없애고»).
            //   닫으면 이 확률 팝업으로 돌아온다(Overlay 가 한 겹이라 «겹쳐 뜨기» 를 «갔다 돌아오기» 로 낸다).
            {
                var cell = Find(box2, "Odds:" + rare[0].Rar + ":0");
                Assert.IsNotNull(cell, "누를 칸");
                var btn = cell.GetComponent<Button>(); Assert.IsNotNull(btn, "칸이 눌린다(4항)");
                btn.onClick.Invoke(); yield return Frames(2);
                var info = _app.Overlay.Root;
                Assert.IsNull(Find(info, "OddsBox"), "확률 목록이 아니라 세부 팝업이 떠 있다");
                Assert.IsNotNull(Find(info, "Name"), "세부 팝업의 이름줄");
                Assert.IsNull(Find(info, "BtnL"), "장착/해제 버튼이 없다(보기 전용)");
                Assert.IsNull(Find(info, "BtnR"), "슬롯 강화 버튼이 없다(보기 전용)");
                // 아무것도 안 바꾼다 — 세이브도 지갑도
                double g0 = _app.Save.Gold, m0 = _app.Save.Gem;
                var back = Find(info, "Dimmed")?.GetComponent<Button>(); Assert.IsNotNull(back, "세부 팝업 배경 탭");
                back.onClick.Invoke(); yield return Frames(2);
                Assert.IsNotNull(Find(_app.Overlay.Root, "OddsBox"), "닫으면 확률 팝업으로 돌아온다");
                Assert.AreEqual(g0, _app.Save.Gold, 1e-9, "보기 전용 팝업은 지갑을 안 만진다");
                Assert.AreEqual(m0, _app.Save.Gem, 1e-9, "보기 전용 팝업은 지갑을 안 만진다");
            }

            // «탭하여 닫기» — 공통 정보 팝업 문법
            var dim = Find(_app.Overlay.Root, "Dimmed")?.GetComponent<Button>();
            Assert.IsNotNull(dim, "배경 탭으로 닫힌다");
            dim.onClick.Invoke(); yield return Frames(2);
            Assert.IsFalse(_app.Overlay.IsOpen, "탭하면 닫힌다");

            _log.AssertNoRed("확률 팝업(T267)");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(2);
        }

        /// <summary>
        /// T267 6단계 — «보기 전용» 세부 팝업은 <b>아래(비용 줄·버튼)만 잘라 낸 것</b>이다(표 ㊿ · 주인 «아래 두 버튼만 없애고»).
        /// <para>
        /// ⚠ 이 자가 재는 것: <b>스탯 박스·옵션 목록이 장비 세부 팝업과 같은 자리·같은 높이인가.</b>
        /// 상자만 짧게(46.5 → 38.5%) 잘라 놓고 안쪽 자리를 <b>긴 상자 기준</b>으로 계산하면 안쪽이 0.83 배로 눌리는데,
        /// 팝업은 멀쩡히 뜨고 빨간 줄도 안 난다 — 옵션 줄이 53px → 44px 이 되어 <b>본문 40 이 조용히 잘릴 뿐</b>이다(결정 809).
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ViewOnlyPopupCutsOnlyTheBottom()
        {
            yield return Boot();
            var D = _app.Data;
            var t0 = D.Gear.AllTypes[0];
            var g = new GearItem { Part = t0.Part, Type = t0.Type, Rar = 0, Plus = 0 };

            // ⚠ **자리를 재기 전에 등장 연출을 끝낸다** — 공통 팝업은 `UiKit.PopIn`(스케일 0.82 → 1 · 0.28초 · OutBack)으로 뜬다.
            //   두 팝업은 **상자 높이가 다르므로**(46.5 vs 38.5%) 옵션 목록이 상자 한가운데서 떨어진 거리도 다르고,
            //   그래서 «연출 중간» 에 재면 같은 자리인데도 두 값이 어긋난다(런 645 실측 2px · 결정 826).
            //   `UiShotsTests.Shot` 이 PNG 를 찍기 전에 부르는 그 줄과 같은 까닭이다(T49).
            GearUi.OpenInfo(_app, g); yield return Frames(2);
            var oi = Find(_app.Overlay.Root, "Options"); Assert.IsNotNull(oi, "보기 전용 팝업의 옵션 목록");
            var si = Find(_app.Overlay.Root, "Stats"); Assert.IsNotNull(si, "보기 전용 팝업의 스탯 박스");
            yield return Settle(oi);
            Assert.AreEqual(CanvasScaleY(), oi.lossyScale.y, 1e-3f, "연출이 끝난 뒤에 잰다(보기 전용) — 캔버스 배율과 다르면 지역 스케일이 1 이 아니다 = 연출 중간");
            Assert.IsNull(Find(_app.Overlay.Root, "Cost"), "비용 줄은 잘려 나간 쪽이다");
            float optH = ((RectTransform)oi).rect.height, stH = ((RectTransform)si).rect.height;
            float optY = oi.position.y, stY = si.position.y;
            int optRows = oi.childCount;
            float rowH = optRows > 0 ? ((RectTransform)oi.GetChild(0)).rect.height : 0f;
            _app.Overlay.Close(); yield return Frames(2);

            GearUi.OpenDetail(_app, g, null); yield return Frames(2);
            var od = Find(_app.Overlay.Root, "Options"); Assert.IsNotNull(od, "장비 세부 팝업의 옵션 목록");
            var sd = Find(_app.Overlay.Root, "Stats"); Assert.IsNotNull(sd, "장비 세부 팝업의 스탯 박스");
            yield return Settle(od);
            Assert.AreEqual(CanvasScaleY(), od.lossyScale.y, 1e-3f, "연출이 끝난 뒤에 잰다(장비 세부) — 캔버스 배율과 다르면 지역 스케일이 1 이 아니다 = 연출 중간");
            Assert.AreEqual(((RectTransform)od).rect.height, optH, 1.5f, "옵션 목록 높이가 두 팝업에서 같다");
            Assert.AreEqual(od.position.y, optY, 1.5f, "옵션 목록 자리가 두 팝업에서 같다");
            Assert.AreEqual(((RectTransform)sd).rect.height, stH, 1.5f, "스탯 박스 높이가 두 팝업에서 같다");
            Assert.AreEqual(sd.position.y, stY, 1.5f, "스탯 박스 자리가 두 팝업에서 같다");

            // 그리고 그 결과가 무엇을 지키는지 — **줄 하나의 높이**까지 같다(여기가 눌리면 본문 40 이 잘린다).
            // ⚠ «몇 px 이상» 으로 안 적는다 — 그 수는 캔버스 기준 해상도에 매인 값이라 베껴 두면 화면 규격이 바뀔 때
            //    자가 «틀린 채로 초록» 이 된다(결정 555). 지켜야 할 것은 «장비 세부 팝업과 같다» 이고 그쪽은 T63-gear 가 이미 재고 있다.
            if (optRows > 0 && od.childCount > 0)
                Assert.AreEqual(((RectTransform)od.GetChild(0)).rect.height, rowH, 1.5f, "옵션 줄 하나의 높이가 두 팝업에서 같다");

            _log.AssertNoRed("보기 전용 세부 팝업(T267 6단계)");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(2);
        }
    }
}
