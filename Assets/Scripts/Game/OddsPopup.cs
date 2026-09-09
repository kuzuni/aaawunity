using System.Text;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 상자 «확률 정보» 팝업 (T267 2단계 · 레퍼런스 <c>36_box_rates_1.jpg</c>·<c>37_box_rates_2.jpg</c> ·
    /// 주인 2026-09-09 «상점에 상자 부분에 인포 버튼 클릭 시 이런 게 떠야 함»).
    /// <para>
    /// 구도(레퍼런스 36): 명판 <b>«확률»</b> → 등급 구간이 <b>세로로</b> 이어진다. 구간 머리 = 등급 이름(그 등급 색) +
    /// «기본 확률(확정 보상 제외): NN.NN%»(퍼센트는 초록) → 그 아래 <b>5열 칸 격자</b>(칸마다 아이콘 + 아래에 개별 확률) →
    /// 길면 세로 스크롤 → 맨 아래 회색 띠 «확정 보상도 같은 확률을 쓴다» → «탭하여 닫기».
    /// </para>
    /// <para>
    /// <b>수는 하나도 안 박혀 있다</b>(지시서 3항) — 구간·개별 확률은 전부 <see cref="GachaOdds"/>(1단계 · 순수 C#)가 낸다:
    /// 등급 확률 = <c>gacha.json</c> 의 그 상자 <c>rate</c> · 아이템 수 = <c>gear.json</c> 의 <c>AllTypes</c> ·
    /// 개별 = 등급 ÷ 아이템 수. <b>그 분모는 «화면에 그릴 칸 수» 가 아니라 «뽑기가 실제로 고르는 목록» 이다</b>(결정 746) —
    /// 그래서 이 화면은 그 목록을 <b>그대로</b> 그린다(칸을 따로 고르지 않는다). 둘이 어긋나면 적힌 확률이 거짓말이 된다.
    /// </para>
    /// 이름 계약(테스트): 상자 표식 <c>OddsBox</c>(<b>루트가 아니라 꺼진 표식 자식</b> — 루트 이름은 프리팹 키 <c>ui.popup</c> 그대로다 · 아래 <see cref="Open"/> 주석) ·
    /// 구간 머리 <c>Sec:&lt;등급&gt;</c>(이름 <c>SecName</c> · 확률 <c>SecRate</c>) ·
    /// 칸 <c>Odds:&lt;등급&gt;:&lt;번호&gt;</c>(개별 확률 <c>Pct</c>) · 스크롤 <c>OddsScroll</c> · 바닥 띠 <c>OddsFoot</c>.
    /// </summary>
    public static class OddsPopup
    {
        // ───────── 자리 — 전부 레퍼런스 36 실측(표 ㊾) ─────────
        // 처음엔 눈대중으로 두었다가 표 ㊾ 를 재고 나서 그 값으로 바꿨다 — 재기 전에 놓은 수는 «잰 값» 이 아니다.
        /// <summary>팝업 상자(화면 %) — 실측 px 67~653 · 383~1185.</summary>
        static readonly Layout.R RBox = new Layout.R(9.3f, 24.6f, 81.5f, 51.4f);
        /// <summary>스크롤 창(상자 %) — 명판 아래(화면 28.9%)부터 바닥 띠 위(71.2%)까지.</summary>
        static readonly Layout.R RScroll = new Layout.R(0.5f, 8.4f, 98.8f, 82.3f);
        /// <summary>바닥 회색 띠(상자 %) — 실측 화면 y 71.2 h 4.0.</summary>
        static readonly Layout.R RFoot = new Layout.R(0.5f, 90.7f, 98.8f, 7.8f);

        /// <summary>격자 열 수 — 레퍼런스 36 실측(한 줄에 다섯 칸).</summary>
        const int Cols = 5;
        /// <summary>구간 머리 띠 높이 · 칸 한 줄(행 피치) · 구간 사이 틈 — 전부 레퍼런스 36 실측(px · <b>720×1560 그림 기준</b> · 표 ㊾).
        /// 머리 띠 677~743 = 67px · 행 피치 = 칸 위 끝 간격 126px · 틈은 구간이 붙어 보이지 않을 만큼만.
        /// <para>⚠ <b>이 수는 그림 px 이지 캔버스 px 이 아니다</b> — 캔버스 기준 해상도는 <see cref="UiKit.FrameH"/>(2337)라
        /// 그림(1560)의 <b>1.498배</b>다. 그대로 쓰면 세로가 전부 2/3 로 눌린다(§5 표 ㊾ 에서 구간 머리 h 가 4.3 → 2.9 로 나온 그 수 ·
        /// 칸이 정사각이 아니라 납작해지고 확률 글자가 아랫줄 칸에 겹친다). <see cref="RefY"/> 로 한 번 옮겨 쓴다.</para></summary>
        const float HeadRefPx = 67f, RowRefPx = 126f, GapRefPx = 18f;
        /// <summary>레퍼런스 그림(720×1560)에서 잰 <b>세로</b> px → 캔버스 px. 가로는 % 로 놓으므로 세로만 옮기면 된다.</summary>
        static float RefY(float refPx) => refPx / 1560f * UiKit.FrameH;
        /// <summary>머리 띠 바닥 → 그 아래 첫 칸 위(그림 px 743 → 775 = 32px · 표 ㊾ 비고).</summary>
        const float CellTopRefPx = 32f;
        /// <summary>위 실측을 캔버스 px 로 옮긴 것 — 자리를 놓는 곳은 <b>이쪽만</b> 쓴다(원값은 «무엇을 쟀나» 를 남기려고 둔다).</summary>
        static readonly float HeadPx = RefY(HeadRefPx), RowPx = RefY(RowRefPx), GapPx = RefY(GapRefPx), CellTopPx = RefY(CellTopRefPx);

        // ───────── 칸 격자 — 스크롤 창(그림 px 70~650 = 580px) 안에서 잰 값이다 ─────────
        // 레퍼런스 첫 줄 조각: 왼쪽 끝 102 · 폭 84 · 열 피치 106(102·208·314·420·526) · 마지막 오른쪽 끝 610.
        // 스크롤 창을 그냥 5등분하면 열이 116px 이 되어 조각이 가로로 늘어난다 — 여백이 있는 격자다.
        const float ScrollRefPx = 580f, ColLeftRefPx = 32f, ColPitchRefPx = 106f, TileWRefPx = 84f, TileHRefPx = 80f;
        /// <summary>칸 왼쪽 첫 자리 · 열 피치 · 칸 폭 — 전부 스크롤 창 폭에 대한 %(가로는 % 로 놓으므로 배율이 필요 없다).</summary>
        const float ColX0 = ColLeftRefPx / ScrollRefPx * 100f,
                    ColPitch = ColPitchRefPx / ScrollRefPx * 100f,
                    ColW = TileWRefPx / ScrollRefPx * 100f;
        /// <summary>칸 안에서 조각이 차지하는 세로 몫 · 그 아래 확률 글자가 시작하는 자리(행 피치에 대한 %).</summary>
        const float TilePct = TileHRefPx / RowRefPx * 100f, TextTopPct = 71f;

        /// <summary>퍼센트 글자 — 등급 확률은 «39.68%» 꼴, 개별은 작아서 «1.89%»·«0.189%» 처럼 유효숫자를 하나 더 준다(레퍼런스와 같은 꼴).</summary>
        public static string Pct(double v) => v >= 1.0 ? v.ToString("0.00") + "%" : v.ToString("0.000") + "%";

        /// <summary>연다 — 그 상자의 확률 구간을 그린다. 상자를 못 찾으면 <b>빈 구간</b>이라 «구간 0» 으로 뜬다(안 죽는다 · 결정 746 ⑤).</summary>
        public static RectTransform Open(App app, string boxKey)
        {
            if (app == null) return null;
            var D = app.Data;
            var box = GachaOdds.BoxOf(D, boxKey);
            string title = box != null && !string.IsNullOrEmpty(box.Name) ? box.Name + " 확률" : "확률";
            var b = app.Overlay.OpenBox(UiKit.PopupKeyPlain, "ui.title.tangerine", title, RBox, app.Overlay.Close);
            if (b == null) return null;
            // 두 계약이 «루트 이름» 한 칸을 두고 부딪쳤던 자리다(T288 · 워커 J 가 blame 으로 짚었다).
            //   ⓐ 공통 문법 — 팝업 루트에 프리팹 키 이름 `ui.popup` 이 있어야 한다(UiSmokeTests «정보 팝업 = 공통 팝업 문법»).
            //   ⓑ 이 팝업만의 이름 — 세부 팝업(38)이 이 위로 오가므로 «지금 어느 쪽이 떠 있나» 를 이름으로 갈라야 한다(OddsPopupTests).
            // 루트 이름을 덮으면 ⓐ 가 깨지고, ⓑ 를 버리면 두 팝업을 못 가린다 ⇒ **표식만 자식으로 내린다.**
            // UiKit.Find 는 «이름이 같은 자식» 을 깊이 검색하고 꺼진 것도 본다 — 그래서 표식은 꺼 둔다(그리지도 막지도 않는다).
            // ⚠ UiKit.Tag(UiTag 컴포넌트)로는 안 된다 — 그것은 오브젝트 «이름» 이 아니라 표식 이름이라 Find 가 못 집는다.
            UiKit.Rect(b, "OddsBox").gameObject.SetActive(false);

            // 스크롤 창 — 내용은 «구간 머리 + 칸 줄» 을 세로로 이어 붙인 것이라 격자 레이아웃이 아니라 자리로 놓는다.
            var view = UiKit.Rect(b, "OddsScroll"); UiKit.Pct(view, RScroll);
            view.gameObject.AddComponent<RectMask2D>();
            var vimg = UiKit.Ensure<Image>(view.gameObject); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            var content = UiKit.Rect(view, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            scroll.content = content; scroll.viewport = view;

            var rows = GachaOdds.Of(D, boxKey);
            int n = GachaOdds.ItemCount(D);
            int lines = n > 0 ? (n + Cols - 1) / Cols : 0;
            float y = 0f;
            foreach (var r in rows)
            {
                y += Section(app, D, content, r, y, n, lines, boxKey);
                y += GapPx;
            }
            // 천장·누적 — 레퍼런스에는 없지만 **옛 ⓘ 팝업이 보여 주던 것**이라 지우면 정보가 준다(T125 천장 · T261 희귀 천장).
            //   레퍼런스의 바닥 문장이 «확정 보상도 같은 확률을 쓴다» 인데 그 «확정 보상» 이 바로 천장이다 —
            //   그래서 목록 맨 끝에 한 덩이로 붙인다(스크롤 안이라 자리를 뺏지 않는다 · 결정 기록).
            if (box != null) y += Pity(app, content, box, y);
            content.sizeDelta = new Vector2(0f, Mathf.Max(0f, y));

            // 바닥 띠 — 주인 문장(«확정 보상 제외» 와 짝이 되는 안내)
            var foot = UiKit.Panel(b, "OddsFoot", "fr.r12", Palette.A(Palette.Ink, 0.45f)).rectTransform;
            UiKit.Pct(foot, RFoot);
            UiKit.Label(foot, 2, 0, 96, 100, "확정 보상도 같은 확률을 쓴다", TextSize.Aux, Palette.CreamDark).name = "FootText";

            // §5 는 이름표를 **표 ㊾ 의 행 이름 그대로** 맞춘다(결정 592) — 한 글자만 달라도 그 행이 0 점이 된다(결정 756 이 여섯 건을 그렇게 잡았다).
            UiKit.Tag(b, "팝업 상자"); UiKit.Tag(foot, "바닥 회색 띠");
            // 명판은 공통 팝업(`UiKit.Popup`)이 세운 조각이라 이름표가 없다 — 표 ㊾ 가 그 자리를 재므로 여기서 붙인다(GearUi 가 등급 배지에 하는 것과 같은 꼴).
            var plate = UiKit.Find(b, "ui.title.tangerine"); if (plate != null) UiKit.Tag(plate, "명판(«확률»)");
            return b;
        }

        /// <summary>천장·누적 덩이 — 옛 ⓘ 팝업이 보여 주던 것을 목록 끝에 잇는다(레퍼런스에는 없다 · 지우면 정보가 준다).</summary>
        static float Pity(App app, RectTransform content, GachaBox box, float y)
        {
            // 세이브의 그 상자 상태 — 없으면 «아직 안 연 상자» 로 읽는다(여기서 만들지 않는다 · 보여 주기 팝업이 세이브를 늘리면 안 된다).
            var st = app.Save != null && app.Save.GachaBoxes.TryGetValue(box.Key, out var s0) ? s0 : new GachaState();
            var lines = new System.Collections.Generic.List<string>();
            if (box.PityMyth > 0) lines.Add("신화 확정: " + box.PityMyth + "회마다 (남은 " + System.Math.Max(0, box.PityMyth - st.P50) + "회)");
            if (box.PityLegend > 0) lines.Add("전설 확정: " + box.PityLegend + "회마다 (남은 " + System.Math.Max(0, box.PityLegend - st.P10) + "회)");
            if (lines.Count == 0) lines.Add("천장 없음");
            lines.Add("누적 " + st.Pulls + "회 열었습니다");
            lines.Add("1회 다이아 " + UiKit.FmtQty(box.Cost) + " · " + app.Data.Gacha.TenPullCount + "회 다이아 " + UiKit.FmtQty(box.Cost * app.Data.Gacha.TenPullCount));
            float h = HeadPx + lines.Count * 44f;
            var blk = UiKit.Rect(content, "OddsPity"); Place(blk, 0f, y + GapPx, 100f, h);
            var bg = UiKit.Panel(blk, "PityBg", "fr.r12", Palette.A(Palette.Ink, 0.35f)).rectTransform; UiKit.Stretch(bg);
            UiKit.Label(blk, 3, 4, 94, 92, string.Join("\n", lines), TextSize.Aux, Palette.Cream, TextAnchor.UpperLeft, true, false).name = "PityText";
            return GapPx + h;
        }

        /// <summary>구간 하나(등급 머리 + 칸 격자)를 <paramref name="y"/> 아래에 놓고 그 높이를 돌려준다.</summary>
        static float Section(App app, GameData D, RectTransform content, GachaOddsRow r, float y, int n, int lines, string boxKey)
        {
            string color = Palette.RarName(r.Rar);
            var head = UiKit.Rect(content, "Sec:" + r.Rar);
            Place(head, 0f, y, 100f, HeadPx);
            var hb = UiKit.Panel(head, "HeadBg", "fr.r12", Palette.A(Palette.Ink, 0.35f)).rectTransform; UiKit.Stretch(hb);
            UiKit.Label(head, 2, 0, 26, 100, r.Name, TextSize.Body, Palette.ByName(color), TextAnchor.MiddleLeft).name = "SecName";
            if (r.Rar == D.Gear.RarRare) UiKit.Tag(head, "구간 머리 띠");   // 표 ㊾ 는 «희귀» 구간 하나를 잰다(스크롤에 따라 움직이는 행이라 표 꼬리에 ⚑ 로 적어 뒀다)
            // «기본 확률(확정 보상 제외): NN.NN%» — 퍼센트만 초록(레퍼런스 36 그대로). 리치 텍스트 한 조각이라 T52 의 «섞어 쓰지 마라» 와 다르다:
            // 여기서 색이 갈라 주는 것은 «수» 이고 그 수가 이 줄의 요점이다.
            var sb = new StringBuilder("기본 확률(확정 보상 제외): <color=#3FD214>").Append(Pct(r.Percent)).Append("</color>");
            var rate = UiKit.Label(head, 28, 0, 70, 100, sb.ToString(), TextSize.Aux, Palette.Cream, TextAnchor.MiddleRight);
            rate.name = "SecRate"; rate.richText = true;

            float gy = y + HeadPx + CellTopPx;
            for (int i = 0; i < n; i++)
            {
                var t = D.Gear.AllTypes[i];
                var cell = UiKit.Rect(content, "Odds:" + r.Rar + ":" + i);
                // 칸은 스크롤 폭을 5등분한 것이 **아니다** — 레퍼런스는 좌우에 여백을 두고 106px 피치로 84px 조각을 놓는다.
                //   5등분(20%)으로 놓으면 조각이 열 폭을 다 먹어 «가로로 늘어난 칸» 이 된다(표 ㊾ 의 그 행이 가리키던 것).
                Place(cell, ColX0 + (i % Cols) * ColPitch, gy + (i / Cols) * RowPx, ColW, RowPx);
                // T288-8 — 물건 칸은 **두 겹**이다: 바깥 `ui.itemFrame.empty`(여기에만 `Item`·`NormalArea` 가 있다) 안 `NormalArea` 에 등급색 변형.
                //   등급색 변형을 «바로» 세우면 그 조각에는 `Item` 자식이 없어 그림이 아예 안 그려지고
                //   칸 수만큼 «[UiKit] 이미지 없음: ui.itemFrame.<색>/Item» 이 뜬다(run 649 에서 72건 · 확률 팝업이 빈 테두리로 떴다).
                //   이 두 겹은 이 파일이 정하는 것이 아니라 레포의 정본 문법이다 — `LobbyPopups.Cell` · `PetScreen` 이 같은 꼴을 쓴다.
                var frame = UiKit.Spawn("ui.itemFrame.empty", cell); frame.name = "ItemFrame_01";
                // 조각은 칸을 가로로 꽉 채우고 세로는 «칸 위 80px ÷ 행 피치 126px»(레퍼런스 36 실측) — 그래야 거의 정사각이다.
                var frt = (RectTransform)frame.transform; UiKit.Pct(frt, 0, 0, 100, TilePct);
                UiKit.Hide(frt, "Text_Level", "Focus", "Disable", "Lock", "Add_1", "Add_2");
                var area = UiKit.Find(frt, "NormalArea");
                if (area != null) { UiKit.Clear(area); var f = UiKit.Spawn("ui.itemFrame." + color, area); UiKit.Stretch((RectTransform)f.transform); }
                var pic = UiKit.Find(frt, "Item");
                if (pic != null) { pic.gameObject.SetActive(true); UiKit.SetSprite(frt, "Item", GearLook.IconKey(t.Part, D.Gear.SetOf(t.Type), r.Rar), Palette.White); }
                GearUi.DarkFrame(frt, frt.localScale.x);   // T69 7항 — 물건 칸은 전부 이 문을 지난다(조각 제 Border 로는 굵기 계약이 안 선다)
                // ⚠ 글자 칸 세로는 «크기 × 1.4» 여야 잘리지 않는다(T63 · TextSize.LineBox) — Aux 36 → 50.4 캔버스 px.
                //   행 피치가 캔버스 188.8px(그림 126px)이라 여기 29% = 54.7px 로 그 하한을 넘는다.
                //   ⚠ 그림 px 을 그대로 쓰던 때는 이 자리가 50.4px «딱» 이었다 — 한 눈금만 줄어도 잘리는 자리였다(결정 839).
                UiKit.Label(cell, 0, TextTopPct, 100, 100f - TextTopPct, Pct(r.Each), TextSize.Aux, Palette.White).name = "Pct";
                // 이름표는 **칸 상자가 아니라 조각**에 단다 — 표 ㊾ 의 그 행이 잰 것은 레퍼런스의 «파란 타일 bbox»(84×80px)이지
                //   그 아래 확률 글자까지 낀 행 상자가 아니다. 칸 상자로 재면 h 가 8.1(ref 5.2 · +2.9)로 «턱걸이 통과» 라
                //   무엇이 맞았는지 알 수 없다 — 같은 것끼리 재야 다음 사람이 그 수를 믿는다(결정 848).
                if (i == 0 && r.Rar == D.Gear.RarRare) UiKit.Tag(frt, "보상 칸(구간 첫 칸)");
                // 4항 — 칸을 누르면 «보기 전용» 세부 팝업. 닫으면 **이 팝업으로 돌아온다**(프로필 팝업 둘이 쓰는 그 꼴 · 표 ㉟).
                //   Overlay 는 한 겹이라 «겹쳐 뜨기» 가 아니라 «갔다 돌아오기» 로 같은 결과를 낸다(결정 기록).
                var item = new GearItem { Part = t.Part, Type = t.Type, Rar = r.Rar, Plus = 0 };
                string key = boxKey;
                UiKit.Clickable(cell, () => GearUi.OpenInfo(app, item, () => Open(app, key)));
            }
            return HeadPx + CellTopPx + lines * RowPx;   // 머리 띠 + 그 아래 여백 + 칸 줄(여백을 빼면 다음 구간이 마지막 줄을 덮는다)
        }

        /// <summary>스크롤 Content 안의 자리 — x·w 는 %, y·h 는 px(위에서 아래로 쌓는다).</summary>
        static void Place(RectTransform rt, float xPct, float yPx, float wPct, float hPx)
        {
            rt.anchorMin = new Vector2(xPct / 100f, 1f); rt.anchorMax = new Vector2((xPct + wPct) / 100f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -(yPx + hPx)); rt.offsetMax = new Vector2(0f, -yPx);
        }
    }
}
