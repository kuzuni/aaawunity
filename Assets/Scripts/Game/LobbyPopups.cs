using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 로비 사이드 팝업 껍데기 (T44 · 주인 2026-09-06 «UI 는 무조건 레퍼런스 기준» · ⓔ «시스템이 없는 화면은 레이아웃 껍데기»):
    /// 퀘스트(<c>docs/ref/15_quest.jpg</c> · 표 ⑳) · 출석(<c>16</c> · ㉑) · 데일리 기프트(<c>17</c> · ㉒) 는 <b>팝업</b>(공통 팝업 문법 <see cref="UiKit.Popup"/> · 배경 탭 = 닫기),
    /// 특권(<c>11</c> · ⑲ · <see cref="PrivilegeScreen"/>) 은 <b>페이지</b>(상단 재화 바 + 뒤로 ◀ · 탭 바 없음).
    /// <b>T78(주인 2026-09-07)</b>: 7일 챌린지(18 · ㉓) 팝업과 시즌 패스(19 · ㉔) 페이지는 삭제 · 퀘스트 팝업은 GUI Pro <c>Progression_Mission_02</c> 프리팹으로 교체했다.
    /// 시스템이 없으므로 <b>전부 표시만</b> — 버튼은 눌러도 아무 일 없음 · 숫자는 0(레퍼런스 숫자를 베끼지 않는다 · 타이머는 «--:--:--») · 글자는 레퍼런스 글자를 우리말로.
    /// 재료 = GUI Pro 조각만(ui.popup 패널 · Title_01 리본 · ItemFrame_01 칸 · fr.r12/fr.rect 9-slice · 아이콘 · 버튼 · 슬라이더 · Environment 들판/길/나무) · 코드 도형 0 · 새 그림 0.
    /// 배치 = <see cref="Layout"/> ⑲~㉔ 상수(프레임 % · ±3%p) · 비평 이름표(T46)는 표의 «요소» 글자 그대로.
    /// 진입 = <see cref="LobbyScreen.OnSide"/>(사이드 아이콘 4 · T78 로 배너·성·스타터팩·7일 챌린지 진입은 사라졌다). 시스템이 생기면 각 함수의 글자·숫자 자리에 데이터를 넣는다(배치는 그대로).
    /// </summary>
    public static class LobbyPopups
    {
        public const string Dashes = "--:--:--";

        // ───────────────────────── 공통 조각 ─────────────────────────
        static Layout.R Sh(Layout.R r, float dx, float dy) => new Layout.R(r.X + dx, r.Y + dy, r.W, r.H);

        /// <summary>리본/명판 조각(Popup 이 상자 윗변 가운데에 세운 것)을 표의 자리·크기로 — 아랫변이 상자 윗변에 닿게(리본은 상자 «위»에 걸친다).</summary>
        static RectTransform Ribbon(RectTransform box, string key, Layout.R ribbonR, Layout.R boxR)
        {
            var rib = UiKit.Find(box, key); if (rib == null) return null;
            var rr = (RectTransform)rib; rr.sizeDelta = UiKit.PxSize(ribbonR);
            float cx = (ribbonR.X + ribbonR.W / 2f - (boxR.X + boxR.W / 2f)) / 100f * UiKit.FrameW;
            float cy = (boxR.Y - (ribbonR.Y + ribbonR.H / 2f)) / 100f * UiKit.FrameH;
            rr.anchoredPosition = new Vector2(cx, cy);
            // 명판 글자 = 제목 종류(T63 · 60 · 리본이 좁으면 bestFit 으로 32 까지)
            var t = rr.GetComponentInChildren<Text>(true); if (t != null) { t.fontSize = TextSize.Title; t.resizeTextForBestFit = true; t.resizeTextMinSize = TextSize.BestFitMin; t.resizeTextMaxSize = TextSize.Title; TextAudit.Mark(t, TextKind.Title); RibbonTextFit(t); }
            return rr;
        }

        /// <summary>리본 조각(Title_01)의 글자 rect 는 3.9% 리본에서 56px 인데 제목 60 의 한 줄 선호 높이가 58px 라 위아래 1px 씩 넘쳤다(CI #106 게이트 «출석 보상»·«데일리 기프트» 잘림) → 글자 rect 만 세로로 늘린다(리본 크기·자리 불변 · 글자는 가운데 정렬 그대로).</summary>
        public static void RibbonTextFit(Text t)
        {
            float need = TextSize.Title * 1.2f;
            var tr = t.rectTransform; float h = tr.rect.height;
            if (h > 0f && h < need) tr.sizeDelta = new Vector2(tr.sizeDelta.x, tr.sizeDelta.y + (need - h));
        }

        /// <summary>보상 칸 — ItemFrame_01 조각(본래 190px · 배율로 표 칸에) + 등급색 변형 + 아이콘 (+ 오른쪽 아래 수량 · 오른쪽 위 자물쇠). PetScreen 의 칸과 같은 문법.</summary>
        public static RectTransform Cell(Transform parent, Layout.R parentR, Layout.R cellR, string frameColor, string iconKey, string qty = null, bool locked = false, string name = "Cell")
        {
            var cell = UiKit.Rect(parent, name); UiKit.Pct(cell, cellR.Within(parentR));
            var frame = UiKit.Spawn("ui.itemFrame.empty", cell); frame.name = "ItemFrame_01"; var frt = (RectTransform)frame.transform;
            UiKit.FitScale(frt, UiKit.PxSize(cellR));
            UiKit.Hide(frt, "Text_Level", "Focus", "Disable", "Lock", "Add_1", "Add_2");
            var area = UiKit.Find(frt, "NormalArea");
            if (area != null) { UiKit.Clear(area); var f = UiKit.Spawn("ui.itemFrame." + frameColor, area); UiKit.Stretch((RectTransform)f.transform); }
            var item = UiKit.Find(frt, "Item");
            if (item != null) { item.gameObject.SetActive(true); UiKit.SetSprite(frt, "Item", iconKey, Palette.White); }
            // T69 7항 — 물건 칸은 전부 «장비 화면의 그 프레임» + 공용 DarkFrame 을 거친다(PetScreen·Overlay 보상 칸과 같은 문법).
            // 안 거치면 조각의 제 Border(짙은 갈색 · 선 5px · 가운데 채움)로 «어두운 테두리» 감사만 우연히 통과하고 굵기·결정 184 계약은 안 선다(CI #189 실측).
            GearUi.DarkFrame(frt, frt.localScale.x);
            // 수량 글자 칸 — 보조 36 의 한 줄(TextSize.BoxHeight(36) = 50.4px)이 들어가야 한다(T63 · T77 이 처음 쓴다): 칸 4.0%(93.5px)의 56% = 52.4px · 폭 76%(76px)는 «300»(≈54px)의 141%
            if (!string.IsNullOrEmpty(qty)) { var q = UiKit.Label(cell, 20, 44, 76, 56, qty, TextSize.Aux, Palette.White, TextAnchor.LowerRight, kind: TextKind.Aux); q.name = "Qty"; q.fontStyle = FontStyle.Bold; }
            if (locked) { var lk = UiKit.Icon(cell, "Lock", "ui.iconLock"); UiKit.Pct(lk.rectTransform, 64, -16, 44, 44); }
            return cell;
        }

        /// <summary>⏱ + 글자 한 줄(타이머 자리 · 시스템 없음 → «--:--:--»). 글자 칸은 줄 rect(표 28~39%)보다 오른쪽으로 더 넓게(115%) — 본문 40 의 «종료까지 --:--:--»(≈270px)가 표 폭 28%(302px)의 89% 안에 안 들어가 줄바꿈되던 것(T63 · 줄에는 배경이 없어 이름표 rect 는 그대로).</summary>
        static RectTransform TimerRow(Transform parent, Layout.R parentR, Layout.R r, string text, string name = "Timer")
        {
            var row = UiKit.Rect(parent, name); UiKit.Pct(row, r.Within(parentR));
            var ic = UiKit.Icon(row, "Icon", "pi.time", Palette.White); UiKit.Pct(ic.rectTransform, 0, -10, 9, 120);
            UiKit.Label(row, 11, -20, 115, 140, text, TextSize.Body, Palette.White, TextAnchor.MiddleLeft);
            return row;
        }

        /// <summary>점수 트랙 — 가로 줄(<paramref name="lineColor"/>) 위에 칸 <paramref name="count"/>개(첫 칸 = 점수 메달 · 나머지 = 보상 칸) + 아래 숫자 줄. 이름표 «트랙 아이콘 줄(N칸)» 은 칸 합집합 · «트랙 아이콘(1칸)» 은 첫 칸.</summary>
        static RectTransform Track(Transform parent, Layout.R parentR, Layout.R icon1, float pitch, int count, Layout.R numsR, Color lineColor, string[] icons, string[] nums, string tagRow, string tagCell)
        {
            var host = UiKit.Rect(parent, "Track"); UiKit.Stretch(host);
            float lastX = icon1.X + (count - 1) * pitch;
            var line = UiKit.Panel(host, "Line", "fr.rect", lineColor);
            UiKit.Pct(line.rectTransform, new Layout.R(icon1.X + icon1.W / 2f, icon1.Y + icon1.H * 0.36f, lastX - icon1.X, icon1.H * 0.28f).Within(parentR));
            var cells = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                var r = Sh(icon1, i * pitch, 0);
                // 첫 칸 = 점수 메달 하나(상자 없음) — 레퍼런스 15 도 «60» 메달만 맨몸이라 T69 테두리 담개다(BorderAudit.Exempt «TrackScore»)
                if (i == 0) { var c = UiKit.Rect(host, "TrackScore"); UiKit.Pct(c, r.Within(parentR)); var ic = UiKit.Icon(c, "Icon", icons[0]); UiKit.Stretch(ic.rectTransform); cells[i] = c; }
                else cells[i] = Cell(host, parentR, r, i % 2 == 1 ? "green" : "plum", icons[i % icons.Length], null, false, "Track:" + i);
            }
            var numsRow = UiKit.Rect(host, "Nums"); UiKit.Pct(numsRow, numsR.Within(parentR));
            for (int i = 0; i < count; i++)
            {
                var r = Sh(icon1, i * pitch, 0); float cx = (r.X + r.W / 2f - numsR.X) / numsR.W * 100f;
                UiKit.Label(numsRow, cx - 9, -30, 18, 160, nums[i % nums.Length], TextSize.Body, Palette.Yellow);
            }
            UiKit.TagGroup(host, tagRow, cells); UiKit.Tag(cells[0], tagCell); UiKit.Tag(numsRow, tagRow.StartsWith("트랙") ? "트랙 숫자 줄" : "숫자 줄");
            return host;
        }

        /// <summary>세로 스크롤 창 — <paramref name="viewR"/>(프레임 %) 안에 content(위 앵커 · 높이 <paramref name="contentH"/>%). 자식은 돌려주는 <paramref name="contentR"/> 기준 <c>Within</c> 으로 놓는다.</summary>
        static RectTransform Scroll(Transform parent, Layout.R parentR, Layout.R viewR, float contentH, out Layout.R contentR, out ScrollRect sr)
        {
            var view = UiKit.Rect(parent, "Scroll"); UiKit.Pct(view, viewR.Within(parentR)); UiKit.Ensure<RectMask2D>(view.gameObject);
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            sr = view.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40;
            var content = UiKit.Rect(view, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero; content.sizeDelta = new Vector2(0, contentH / 100f * UiKit.FrameH);
            sr.content = content; sr.viewport = view;
            contentR = new Layout.R(viewR.X, viewR.Y, viewR.W, contentH);
            return content;
        }

        /// <summary>회색 뒤로 버튼(◀ 아이콘 · 글자 없음) — 이름 <c>BackBtn</c>(테스트가 이름으로 누른다).</summary>
        static RectTransform BackButton(Transform parent, Layout.R r, Action onClick)
        {
            var b = UiKit.Button(parent, "ui.btnGray", "", onClick, r); b.name = "BackBtn";
            var ic = UiKit.Icon(b, "Icon", "pi.arrow_left", Palette.Ink); UiKit.Pct(ic.rectTransform, 30, 18, 40, 64);
            return b;
        }
        static Text Head(Transform parent, Layout.R parentR, Layout.R r, string text, Color band, string name = "Head")
        {
            var p = UiKit.Panel(parent, name, "fr.r12", band); UiKit.Pct(p.rectTransform, r.Within(parentR));
            return UiKit.Label(p.transform, 4, 0, 92, 100, text, TextSize.Body, Palette.White);
        }
        static void TagClose(App app)
        {
            var tc = UiKit.Find(app.Overlay.Root, "TapToClose"); if (tc != null) UiKit.Tag(tc, "닫기 안내");
        }

        // ───────────────────────── 15 퀘스트 ─────────────────────────
        static readonly string[] QuestTitles = { "적 50마리 처치", "캠페인 2회 도전", "던전 입장", "오늘 접속", "상자 2개 열기", "장비 2회 강화" };
        static readonly int[] QuestGoals = { 50, 2, 1, 1, 2, 2 };
        static readonly string[] QuestNums = { "0", "20", "40", "60", "80", "100" };
        static readonly string[] TrackIcons = { "ui.iconMedal", "ui.coin", "ui.bookBlue", "ui.gemRed", "pi.magic", "ui.gemRed" };

        /// <summary>퀘스트 보상 점수(레퍼런스 15 의 메달 숫자 — 시스템이 없어 표시만).</summary>
        static readonly string[] QuestScores = { "20", "20", "20", "10", "20", "20" };

        /// <summary>
        /// 프리팹에서 온 글자를 «어두운 조각 위» 에서 읽히게 — 흰 글자 + 어두운 외곽선(<see cref="UiKit.Text"/> 가 새 글자에 붙이는 것과 같은 규격).
        /// <see cref="UiKit.SetText"/> 는 색·크기만 바꾸고 외곽선은 안 붙이므로 조각 글자에는 이걸 한 번 더 부른다(T63 1항의 반대 경우 = 바탕이 어두운 쪽).
        /// </summary>
        static Text OnDark(Text t, Color? color = null)
        {
            if (t == null) return null;
            t.color = color ?? Palette.White;
            var ol = UiKit.Ensure<Outline>(t.gameObject);
            ol.effectColor = new Color(0.1f, 0.06f, 0.05f, 0.85f);
            float d = Mathf.Clamp(t.fontSize * 0.05f, 1.5f, 4f); ol.effectDistance = new Vector2(d, -d); ol.useGraphicAlpha = true;
            return t;
        }

        /// <summary>이름이 <paramref name="prefix"/> 로 시작하는 첫 «직계» 자식 — 프리팹 인스턴스가 «이름 (1)» 처럼 붙어 나올 때 쓴다.</summary>
        static RectTransform ChildStarting(Transform root, string prefix)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++) if (root.GetChild(i).name.StartsWith(prefix, StringComparison.Ordinal)) return (RectTransform)root.GetChild(i);
            return null;
        }

        /// <summary>
        /// 퀘스트 팝업(표 ⑳) — <b>주인 2026-09-07(T78): «퀘스트는 팝업 걍 Progression_Mission_02 이거로 교체»</b>.
        /// GUI Pro <c>Progression_Mission_02</c> 프리팹을 팝업 층에 통째로 세우고(<see cref="Overlay.OpenPrefab"/>) 조각을 표 ⑳ 자리로 <b>옮기기만</b> 한다 —
        /// 상자(<c>Popup_Box_01_Basic</c>) · 제목 리본(<c>Title_Tapered_01_Brown</c>) · 미션 줄(<c>ListFrame_08</c> + <c>ListItem_Mission_02</c>) ·
        /// 보상 칸(줄 안 <c>Group_Price</c> = 아이콘 + 점수) · 받기 표시(<c>Check</c>)가 프리팹 구성 그대로다(새로 그린 조각 0).
        /// 프리팹에 <b>없는</b> 것(점수 트랙 · 새로고침 줄 · 목록 상자 · 탭 3)은 레퍼런스 15 구도 그대로 남긴다(ROUTINE §2 T78 2항이 프리팹에서 가져올 조각을 다섯으로 못박았다).
        /// 줄 배치는 프리팹의 <see cref="GridLayoutGroup"/> 을 1열 · 칸 = 표 ⑳ «퀘스트 줄 1» · 간격 = 피치 − 줄로 바꿔 만든다(줄마다 좌표를 박지 않는다).
        /// 껍데기 규칙(T44)은 그대로 — 진행 0/N · «이동» 은 닫기만 · 완료 줄은 프리팹 ✅.
        /// </summary>
        public static void Quest(App app)
        {
            var ov = app.Overlay; var B = Layout.QsBox;
            var root = (RectTransform)ov.OpenPrefab("ui.progressionMission2").transform;
            // 공통 팝업 문법(ROUTINE) — 배경 탭 = 닫기 · 닫기 X 는 안 쓴다(프리팹 조각은 지우지 않고 끈다)
            var dim = UiKit.Find(root, "Dimmed"); if (dim != null) UiKit.Clickable(dim, () => ov.Close(), false);
            UiKit.Hide(root, "Button_Close_01");
            var tc = UiKit.Text(ov.Root, "탭하여 닫기", TextSize.Body, Palette.White, TextAnchor.MiddleCenter, false, true);
            tc.name = "TapToClose"; tc.fontStyle = FontStyle.Bold; UiKit.Pct(tc.rectTransform, Layout.BookClose);

            var box = (RectTransform)UiKit.Find(root, "Popup"); box.name = "QuestBox"; UiKit.Pct(box, B);
            foreach (var g in box.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = true;   // 상자 뒤로 클릭이 새지 않게(UiKit.Popup 과 같은 처리)
            UiKit.PatternBg(box);   // T72 ① 팝업 배경 패턴

            // 제목 리본(프리팹 Title_Tapered_01_Brown) — 표 ⑳ 제목 자리(박스 윗변에 걸친다 · y 가 박스보다 위라 Within 이 음수)
            var band = ChildStarting(box, "Title_Tapered_01");
            if (band != null)
            {
                UiKit.Pct(band, Layout.QsTitleBand.Within(B));
                var bt = UiKit.SetText(band, "Text (TMP)", "퀘스트", null, TextSize.Title, TextKind.Title);
                if (bt != null) { bt.resizeTextForBestFit = true; bt.resizeTextMinSize = TextSize.BestFitMin; bt.resizeTextMaxSize = TextSize.Title; RibbonTextFit(bt); }
            }

            // 점수 트랙 · 새로고침 줄 · 목록 상자 = 레퍼런스 15 그대로(프리팹에 없는 조각)
            var trackBox = UiKit.Panel(box, "TrackBox", "fr.r12", Palette.A(Palette.Dim, 0.55f)); UiKit.Pct(trackBox.rectTransform, Layout.QsTrackBox.Within(B));
            Track(box, B, Layout.QsTrackIcon, Layout.QsTrackPitch, Layout.QsTrackCount, Layout.QsTrackNums, Palette.Yellow, TrackIcons, QuestNums, "트랙 아이콘 줄(6칸)", "트랙 아이콘(1칸)");
            var refresh = TimerRow(box, B, Layout.QsRefresh, "새로고침까지 " + Dashes, "Refresh");
            var listBox = UiKit.Panel(box, "ListBox", "fr.r12", Palette.A(Palette.Dim, 0.55f)); UiKit.Pct(listBox.rectTransform, Layout.QsListBox.Within(B));

            // 미션 줄 = 프리팹 ScrollView/Content(GridLayoutGroup) — 1열 · 칸 = 표 ⑳ 줄 · 세로 간격 = 피치 − 줄
            var sv = (RectTransform)UiKit.Find(box, "ScrollView");
            var viewR = new Layout.R(Layout.QsRow1.X, Layout.QsRow1.Y, Layout.QsRow1.W, Layout.QsListBox.Y + Layout.QsListBox.H - 0.8f - Layout.QsRow1.Y);
            UiKit.Pct(sv, viewR.Within(B));
            var content = (RectTransform)UiKit.Find(sv, "Content");
            var grid = content != null ? content.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 1;
                grid.cellSize = UiKit.PxSize(Layout.QsRow1);
                grid.spacing = new Vector2(0, UiKit.PxSize(new Layout.R(0, 0, 0, Layout.QsRowPitch - Layout.QsRow1.H)).y);
                grid.padding = new RectOffset(0, 0, 0, 0); grid.childAlignment = TextAnchor.UpperCenter;
            }
            RectTransform row1 = null, row2 = null, medal1 = null, title1 = null, bar1 = null, go1 = null;
            int rows = content != null ? content.childCount : 0, want = Mathf.Min(Layout.QsRowCount, rows);
            for (int i = rows - 1; i >= want; i--) content.GetChild(i).gameObject.SetActive(false);   // 프리팹 줄이 표(6줄)보다 많으면 남는 것은 지우지 말고 끈다
            for (int i = 0; i < want; i++)
            {
                var frame = (RectTransform)content.GetChild(i); frame.name = "Quest:" + i; frame.gameObject.SetActive(true);
                var parts = QuestRow(frame, i, ov);
                if (i == 0) { row1 = frame; medal1 = parts.Medal; title1 = parts.Title; bar1 = parts.Bar; go1 = parts.Go; } else if (i == 1) row2 = frame;
            }

            // 박스 아래 탭 3 = 레퍼런스 15 그대로
            var tabs = new RectTransform[3]; string[] tabNames = { "일일", "주간", "업적" };
            for (int i = 0; i < 3; i++)
            {
                var t = tabs[i] = UiKit.Button(ov.Root, "ui.btnGray", tabNames[i], () => { }, Sh(Layout.QsTab, i * Layout.QsTabPitch, 0)); t.name = "Tab:" + i;
                if (i > 0) foreach (var im in t.GetComponentsInChildren<Image>(true)) im.color = Color.Lerp(im.color, Palette.Dim, 0.45f);   // 비활성 탭은 어둡게(첫 탭 «일일» 활성)
                // T69-lobbypopups — 탭마다 «검은 아웃라인»(레퍼런스 15 도 세 탭이 각자 어두운 외곽선이다) · 어둡게 칠한 «뒤» 에 걸어야 링이 Dim 쪽으로 섞이지 않는다
                UiKit.Bordered(t);
            }
            // 비평 이름표(표 ⑳)
            if (band != null) UiKit.Tag(band, "제목 리본"); UiKit.Tag(box, "팝업 박스"); UiKit.Tag(trackBox.transform, "점수 트랙 상자"); UiKit.Tag(refresh, "새로고침 줄"); UiKit.Tag(listBox.transform, "목록 상자");
            UiKit.Tag(row1, "퀘스트 줄 1"); UiKit.Tag(row2, "퀘스트 줄 2"); UiKit.Tag(medal1, "퀘스트 보상 메달(1줄)"); UiKit.Tag(title1, "퀘스트 제목(1줄)"); UiKit.Tag(bar1, "퀘스트 진행바(1줄)"); UiKit.Tag(go1, "이동 버튼(1줄)");
            UiKit.TagGroup(ov.Root, "탭 줄(3칸)", tabs); UiKit.Tag(tabs[0], "탭(1칸)"); TagClose(app);
            UiKit.PopIn(box);   // 공통 팝업 등장 연출(T49 · UiKit.Popup 이 상자에 거는 것과 같다)
        }

        struct QuestRowParts { public RectTransform Medal, Title, Bar, Go; }

        /// <summary>
        /// 미션 줄 한 개 — 프리팹 <c>ListFrame_08</c>(칸 바탕) 안의 <c>ListItem_Mission_02</c> 조각을 표 ⑳ 의 줄 안 자리로 옮긴다.
        /// 격자 칸 자신이 <c>ListItem_Mission_02</c> 이고 그 안에 바탕 <c>ListFrame_08</c> · 제목 · <c>Slider_02_Yellow</c> · <c>Group_Price</c> · <c>Check</c> 가 있다(이름은 <c>Quest:i</c> 로 바꾼다 · 프리팹 유래 증거는 안쪽 <c>ListFrame_08</c>).
        /// 옮기는 것: 보상(<c>Group_Price</c> = 아이콘 + 점수 · 가로 배치를 끄고 레퍼런스처럼 «아이콘 위 · 숫자 아래») · 제목 · 진행바(<c>Slider_02_Yellow</c>) · 받기 표시(<c>Check</c> · 슬라이더 밑에 있던 것을 줄 오른쪽으로).
        /// 미완 줄(앞 3개)은 레퍼런스 15 처럼 주황 «이동» 버튼(껍데기 = 닫기만) · 완료 줄(뒤 3개)은 프리팹 ✅.
        /// </summary>
        static QuestRowParts QuestRow(RectTransform frame, int i, Overlay ov)
        {
            var parts = new QuestRowParts();
            // 격자 칸 «자신» 이 `ListItem_Mission_02` 이고 `ListFrame_08`(원본의 ListFrame_07 을 갈아 끼운 것)은 그 «안쪽 바탕» 이다 — CI #142 가 잡아 준 계층(결정 173).
            var item = frame;
            bool done = i >= 3;   // 레퍼런스 15 = 앞 3줄 «Go» · 뒤 3줄 ✅

            // 보상 칸(Group_Price) — 가로 레이아웃을 끄고 아이콘 위 · 점수 아래(레퍼런스 15 의 메달 + 숫자)
            var medal = (RectTransform)UiKit.Find(item, "Group_Price");
            if (medal != null)
            {
                var hlg = medal.GetComponent<HorizontalLayoutGroup>(); if (hlg != null) hlg.enabled = false;
                UiKit.Pct(medal, Layout.QsRowMedal.Within(Layout.QsRow1));
                var mi = (RectTransform)UiKit.Find(medal, "Icon");
                if (mi != null) { UiKit.Pct(mi, 0, 0, 100, 100); var img = UiKit.SetSprite(medal, "Icon", "ui.iconMedal"); if (img != null) { img.preserveAspect = true; img.color = Color.white; } }
                // 점수 숫자 = 메달 아래(칸 높이의 72% · 본문 40 한 줄이 안 줄고 들어간다 — 전 코드와 같은 값)
                var mt = OnDark(UiKit.SetText(medal, "Text (TMP)", QuestScores[i], Palette.Yellow, TextSize.Body), Palette.Yellow);
                // 전 코드(UiKit.Label(medal, -20, 98, 140, 72, …))와 같은 자리·규격 — 메달 아래 점수 한 줄
                if (mt != null) { UiKit.Pct(mt.rectTransform, -20, 98, 140, 72); mt.alignment = TextAnchor.MiddleCenter; mt.resizeTextForBestFit = true; mt.resizeTextMinSize = TextSize.BestFitMin; mt.resizeTextMaxSize = TextSize.Body; mt.horizontalOverflow = HorizontalWrapMode.Overflow; }
                parts.Medal = medal;
            }
            // 제목 — 줄의 밝은 바탕 위라 잉크색(T63 1항)
            // 줄 바탕이 프리팹 `ListFrame_08`(어두운 황갈색)이라 Ink 로는 안 읽힌다(screens run 148 눈 확인) → 흰 글자 + 외곽선
            var title = OnDark(UiKit.SetText(item, "Text (TMP)", QuestTitles[i], Palette.White, TextSize.Body));
            if (title != null)
            {
                var tr = title.rectTransform; UiKit.Pct(tr, Layout.QsRowTitle.WithH(Layout.LpLineH).Within(Layout.QsRow1));
                title.alignment = TextAnchor.MiddleLeft; title.name = "Title"; parts.Title = tr;
                // 글자 규격은 전 코드(UiKit.Label)와 같게 — 프리팹에서 온 Text 라 bestFit 설정이 데모 값(10~30)일 수 있다
                title.resizeTextForBestFit = true; title.resizeTextMinSize = TextSize.BestFitMin; title.resizeTextMaxSize = TextSize.Body;
                title.horizontalOverflow = HorizontalWrapMode.Wrap; title.verticalOverflow = VerticalWrapMode.Truncate;
            }
            // 진행바(프리팹 Slider_02_Yellow) — 자리·값·글자만
            var slider = item.GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                var sr = (RectTransform)slider.transform; sr.name = "Bar";
                UiKit.Pct(sr, Layout.QsRowBar.WithH(Layout.LpBarH).Within(Layout.QsRow1));
                slider.value = done ? 1f : 0f;
                var st = sr.GetComponentInChildren<Text>(true);
                // 바 안 숫자는 UiKit.MakeBar 와 같은 규격(bestFit 32~40 · 가로 넘침 허용) — 바 칸(LpBarH 44px)이 40 한 줄(55px)보다 낮다
                if (st != null) { st.text = (done ? QuestGoals[i] : 0) + "/" + QuestGoals[i]; st.fontSize = TextSize.Body; st.resizeTextForBestFit = true; st.resizeTextMinSize = TextSize.BestFitMin; st.resizeTextMaxSize = TextSize.Body; st.horizontalOverflow = HorizontalWrapMode.Overflow; OnDark(st); TextAudit.Mark(st, TextKind.Body); }
                parts.Bar = sr;
            }
            // 받기 표시 / 이동 버튼 — Check 는 프리팹에서 슬라이더 밑에 있어 줄 오른쪽으로 옮긴다
            var check = UiKit.Find(item, "Check");
            if (check != null)
            {
                check.SetParent(item, false); UiKit.Pct((RectTransform)check, Layout.QsRowGo.Within(Layout.QsRow1));
                check.gameObject.SetActive(done);
                if (done) parts.Go = (RectTransform)check;
            }
            if (!done)
            {
                var go = UiKit.Button(item, "ui.btnOrange", "이동", () => ov.Close(), Layout.QsRowGo.Within(Layout.QsRow1));
                go.name = "GoBtn"; parts.Go = go;
            }
            // T69 — 줄 바탕과 보상 칸에 «검은 아웃라인»(레퍼런스 15 도 줄·메달이 검은 외곽선)
            UiKit.Bordered(frame);
            if (parts.Medal != null) UiKit.Bordered(parts.Medal, UiKit.BorderKeySmall);
            return parts;
        }

        // ───────────────────────── 16 출석 ─────────────────────────
        static readonly string[] AttendIcons = { "ui.coin", "ui.potionRed", "ui.gemRed", "ui.bookBlue", "ui.coin", "ui.hourglass" };
        static readonly string[] AttendColors = { "green", "blue", "plum", "green", "green", "plum" };
        /// <summary>하루 칸 보상 수량(껍데기 · 레퍼런스 16 의 숫자를 베끼지 않고 «1» 로 통일 — T44 «숫자는 표시만»).</summary>
        const string AttendQty = "1";

        /// <summary>
        /// 출석 팝업(표 ㉑) — <b>주인 2026-09-07(T76): «출석 보상 Rewards_Daily7_Popup 프리팹 이거로 해줘»</b>.
        /// 프리팹을 팝업 층에 통째로 세우고(<see cref="Overlay.OpenPrefab"/>) 조각을 표 ㉑ 자리로 <b>옮기기만</b> 한다 —
        /// 상자(<c>Popup_Box_01</c>) · 제목 리본(<c>Title_01_Deco_Yellow</c>) · 3×2 격자(<c>Group_DailyList7</c> 의 <see cref="GridLayoutGroup"/>) ·
        /// 하루 칸 6 + 7일차 넓은 칸(<c>DailyFrame_01_l</c> · 그 안의 상태 바탕 <c>Bg_Normal</c>/<c>Bg_Focus1</c>/<c>Bg_Disable</c> 와 ✅ <c>Check</c>)이 프리팹 구성 그대로다.
        /// 칸 머리(«N일차» 자주 띠)와 보상 칸(장비 프레임)은 레퍼런스 16 조각을 그대로 쓴다 — 프리팹 칸에는 폭을 채우는 머리 띠가 없고(<c>Deco</c> 는 129×62 · 9-slice 없음 = 늘리면 찌그러진다),
        /// 보상 칸은 지시서 T76 3항·T69 7항이 «장비 프레임(<c>ui.itemFrame.*</c>)» 으로 못박았다.
        /// 껍데기 규칙(T44) 그대로 — 받은 날 없음(✅ 0) · 오늘 = 1일차만 <c>Bg_Focus1</c> 강조 · 칸을 눌러도 아무 일 없음.
        /// </summary>
        public static void Attendance(App app)
        {
            var ov = app.Overlay; var B = Layout.AtBox;
            var root = (RectTransform)ov.OpenPrefab("ui.rewardsDaily7").transform;
            // 공통 팝업 문법 — 배경 탭 = 닫기 · 닫기 X 는 안 쓴다(프리팹 조각은 지우지 않고 끈다 · 결정 168)
            var dim = UiKit.Find(root, "Dimmed"); if (dim != null) UiKit.Clickable(dim, () => ov.Close(), false);
            UiKit.Hide(root, "Button_Close_01");
            var tc = UiKit.Text(ov.Root, "탭하여 닫기", TextSize.Body, Palette.White, TextAnchor.MiddleCenter, false, true);
            tc.name = "TapToClose"; tc.fontStyle = FontStyle.Bold; UiKit.Pct(tc.rectTransform, Layout.BookClose);

            var box = (RectTransform)UiKit.Find(root, "Popup"); box.name = "AttendanceBox"; UiKit.Pct(box, B);
            foreach (var g in box.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = true;
            UiKit.PatternBg(box);   // T72 ① 팝업 배경 패턴
            // 데모 안내 문구(영문)와 격자 위에 떠 있던 타이머 라벨 — 레퍼런스 16 에 없다(라벨은 screens run 148 에서 3일차 칸을 가리는 것이 보였다)
            UiKit.Hide(box, "Text_Description", "Label_Tail_02_Timer");

            // 제목 리본(프리팹 Title_01_Deco_Yellow) — 표 ㉑ 자리(박스 윗변에 걸친다)
            var rib = ChildStarting(box, "Title_01_Deco");
            if (rib != null)
            {
                UiKit.Pct(rib, Layout.AtRibbon.Within(B));
                var rt = UiKit.SetText(rib, "Text (TMP)", "출석 보상", null, TextSize.Title, TextKind.Title);
                if (rt != null) { rt.resizeTextForBestFit = true; rt.resizeTextMinSize = TextSize.BestFitMin; rt.resizeTextMaxSize = TextSize.Title; RibbonTextFit(rt); }
            }

            // 3열×2행 격자 = 프리팹 Group_DailyList7(GridLayoutGroup) — 칸·피치는 표 ㉑
            var group = (RectTransform)UiKit.Find(box, "Group_DailyList7");
            var cells = new RectTransform[6]; RectTransform head0 = null, icon0 = null;
            if (group != null)
            {
                UiKit.Pct(group, Layout.AtGrid.Within(B));
                var glg = group.GetComponent<GridLayoutGroup>();
                if (glg != null)
                {
                    glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; glg.constraintCount = Layout.AtCols;
                    glg.cellSize = UiKit.PxSize(Layout.AtCell);
                    glg.spacing = new Vector2(UiKit.PxSize(new Layout.R(0, 0, Layout.AtColPitch - Layout.AtCell.W, 0)).x, UiKit.PxSize(new Layout.R(0, 0, 0, Layout.AtRowPitch - Layout.AtCell.H)).y);
                    glg.padding = new RectOffset(0, 0, 0, 0); glg.childAlignment = TextAnchor.UpperLeft;
                }
                int n = Mathf.Min(6, group.childCount);
                for (int i = group.childCount - 1; i >= n; i--) group.GetChild(i).gameObject.SetActive(false);
                for (int i = 0; i < n; i++)
                {
                    var frame = (RectTransform)group.GetChild(i); frame.name = "Day:" + (i + 1); frame.gameObject.SetActive(true);
                    DayFrame(frame, i == 0);
                    var head = Head(frame, Layout.AtCell, Layout.AtCellHead, (i + 1) + "일차", Palette.A(Palette.Plum, 0.8f));
                    var ic = Cell(frame, Layout.AtCell, Layout.AtCellIcon, AttendColors[i], AttendIcons[i], AttendQty);
                    UiKit.Clickable(frame, () => { });
                    cells[i] = frame; if (i == 0) { head0 = head.transform.parent as RectTransform; icon0 = ic; }
                }
            }

            // 7일차 넓은 칸 = 프리팹의 Popup 직계 DailyFrame_01_l(격자 밖 조각)
            var day7 = ChildStarting(box, "DailyFrame_01_l"); RectTransform head7 = null; var r7 = new RectTransform[2];
            if (day7 != null)
            {
                day7.name = "Day:7"; UiKit.Pct(day7, Layout.AtDay7.Within(B)); DayFrame(day7, false);
                head7 = Head(day7, Layout.AtDay7, Layout.AtDay7Head, "7일차", Palette.A(Palette.Plum, 0.8f), "Head7").transform.parent as RectTransform;
                r7[0] = Cell(day7, Layout.AtDay7, Layout.AtDay7Cell, "green", "ui.coin", AttendQty);
                r7[1] = Cell(day7, Layout.AtDay7, Sh(Layout.AtDay7Cell, Layout.AtDay7Pitch, 0), "plum", "ui.gemRed", AttendQty);
                UiKit.Clickable(day7, () => { });
            }
            // 비평 이름표(표 ㉑)
            if (rib != null) UiKit.Tag(rib, "제목 리본"); UiKit.Tag(box, "팝업 박스");
            UiKit.TagGroup(box, "출석 격자(6칸)", cells); UiKit.Tag(cells[0], "출석 칸(1칸)"); UiKit.Tag(head0, "칸 머리(1칸)"); UiKit.Tag(icon0, "칸 보상 아이콘(1칸)");
            UiKit.Tag(day7, "7일 칸"); UiKit.Tag(head7, "7일 칸 머리"); UiKit.TagGroup(box, "7일 보상 줄(2칸)", r7); TagClose(app);
            UiKit.PopIn(box);   // 공통 팝업 등장 연출(T49)
        }

        /// <summary>
        /// 하루 칸 하나 — 프리팹 <c>DailyFrame_01_l</c> 의 상태 바탕을 고르고(오늘 = <c>Bg_Focus1</c> · 나머지 = <c>Bg_Normal</c> · 받은 날은 없다),
        /// 우리가 안 쓰는 데모 조각(가운데 큰 아이콘·수량·반짝임·칸 머리 장식·«DAY» 글자)은 <b>지우지 않고 끈다</b>(결정 168).
        /// 보상 칸(장비 프레임)과 «N일차» 머리 띠는 부르는 쪽이 표 ㉑ 자리에 얹는다.
        /// </summary>
        static void DayFrame(RectTransform frame, bool today)
        {
            UiKit.Show(frame, "Bg_Normal", !today); UiKit.Show(frame, "Bg_Focus1", today);
            UiKit.Hide(frame, "Bg_Focus2", "Bg_Focus3", "Bg_Disable", "SampleEffect", "SampleParticle", "Icon", "Text_Num", "Text_Day", "Check");
            foreach (var deco in frame.GetComponentsInChildren<Transform>(true)) if (deco.name == "Deco") deco.gameObject.SetActive(false);
            UiKit.Bordered(frame);   // T69 — 칸 테두리(레퍼런스 16 도 칸마다 검은 외곽선)
        }

        // ───────────────────────── 17 데일리 기프트 (T77 — 껍데기 → 동작하는 기능) ─────────────────────────

        /// <summary>데일리 기프트 줄의 오른쪽 버튼 상태 — 레퍼런스 17 의 ✅ 자리(표 ㉒ «광고 줄 버튼»).</summary>
        enum GiftBtn
        {
            /// <summary>«받기»(주황 = 주 버튼 색 규칙) — 누적이 닿았고 아직 안 받은 열린 줄.</summary>
            Claim,
            /// <summary>«광고 보기»(파랑 = 광고/정보 색 규칙) — 열린 줄인데 누적이 모자람.</summary>
            Ad,
            /// <summary>이미 받음 — 레퍼런스처럼 초록 ✅(버튼이 아니다).</summary>
            Done,
            /// <summary>«잠금»(회색 · 비활성) — 위 줄을 아직 안 받았다(주인 추가 2026-09-07 00:3X «위에서 아래로 순서대로»).</summary>
            Locked,
        }

        /// <summary>줄 오른쪽 버튼(또는 ✅) 한 개 — 표 ㉒ «광고 줄 버튼» 자리. 이름은 <paramref name="name"/>(스모크 테스트가 찾는다).</summary>
        static RectTransform GiftButton(Transform parent, Layout.R parentR, Layout.R r, string name, GiftBtn st, Action onClick)
        {
            if (st == GiftBtn.Done)
            {
                var ok = UiKit.Icon(parent, name, "pi.check", Palette.Green); UiKit.Pct(ok.rectTransform, r.Within(parentR));
                return ok.rectTransform;
            }
            string key = st == GiftBtn.Claim ? "ui.btnSmallOrange" : st == GiftBtn.Ad ? "ui.btnSmallBlue" : "ui.btnSmallGray";
            string label = st == GiftBtn.Claim ? "받기" : st == GiftBtn.Ad ? "광고 보기" : "잠금";
            var b = UiKit.Button(parent, key, label, st == GiftBtn.Locked ? (Action)(() => { }) : onClick, r.Within(parentR));
            b.name = name;
            if (st == GiftBtn.Locked) UiKit.SetInteractable(b.GetComponent<Button>(), false);
            return b;
        }

        /// <summary>자정까지 남은 시간 — «종료까지 hh:mm:ss»(상점 무료 보급 줄과 같은 문법 · 표에 없는 날은 «--:--:--»).</summary>
        static string GiftEndsIn()
        {
            var left = DateTime.Today.AddDays(1) - DateTime.Now; if (left.Ticks < 0) left = TimeSpan.Zero;
            return $"종료까지 {(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
        }

        /// <summary>
        /// 데일리 기프트 팝업(표 ㉒ · <b>T77 = 실제로 동작한다</b>) — 리본 위 선물 그림 → 노란 리본 «데일리 기프트» → 노란 테두리 박스:
        /// ⏱ 자정까지 남은 시간(1초 갱신) · «오늘의 선물» 무료 1칸(다이아 <c>freeGift.gem</c> · 광고 없이 하루 1회) ·
        /// «광고 N회 보기/선물» 줄(<c>dailyGift.json milestones</c> 개수만큼 · 진행바 <c>min(누적,N)/N</c> · 보상 칸 · 오른쪽 버튼) → «탭하여 닫기».
        /// 줄은 <b>위에서 아래로 순서대로</b> 열린다(무료 칸 → 줄 1 → …) · 광고는 잠긴 줄에서도 누적된다 · 매일 초기화(<see cref="Core.DailyGift.Roll"/>).
        /// 왼쪽 노란 타임라인(선 + 육각 점)은 주인 지시(2026-09-07 00:3X)로 <b>넣지 않는다</b> — 그만큼 줄이 상자 가로 중앙으로 넓어졌다(표 ㉒ 회차 정정).
        /// </summary>
        public static void DailyGift(App app)
        {
            var ov = app.Overlay; var B = Layout.GfBox; var S = app.Save;
            var D = app.Data != null ? app.Data.DailyGift : null;
            string today = SaveStore.Today();
            if (D != null) Core.DailyGift.Roll(S, D, today);

            var box = ov.OpenBox("ui.popup.yellow", "ui.title.yellow", "데일리 기프트", B, () => ov.Close()); box.name = "DailyGiftBox";
            var rib = Ribbon(box, "ui.title.yellow", Layout.GfRibbon, B);
            var pic = UiKit.Icon(ov.Root, "GiftPic", "ui.gift"); UiKit.Pct(pic.rectTransform, Layout.GfPic); pic.transform.SetSiblingIndex(1);   // 어둠 위 · 상자 아래
            var timer = TimerRow(box, B, Layout.GfTimer, GiftEndsIn());
            var timerTxt = timer.GetComponentInChildren<Text>(true);
            // «Ends in» 1초 갱신 — 팝업이 열려 있는 동안만(Overlay.OnTick 은 Begin/Close 가 비운다 · 트윈이 아니라 경고 0)
            float acc = 0f;
            ov.OnTick = () => { acc += Time.unscaledDeltaTime; if (acc < 1f) return; acc = 0f; if (timerTxt != null) timerTxt.text = GiftEndsIn(); };

            var host = UiKit.Rect(box, "GiftRows"); UiKit.Stretch(host);   // «받기» 뒤에 이 안만 다시 그린다(팝업을 다시 열지 않는다 = 열림음 1번)
            Action refresh = null;
            refresh = () => { UiKit.Clear(host); BuildGiftRows(app, host, B, D, today, refresh); };
            refresh();

            // 비평 이름표(표 ㉒) — 다시 그려도 자리가 같으므로 여기서 한 번(줄 조각의 이름표는 BuildGiftRows 안)
            UiKit.Tag(pic.transform, "선물 그림"); if (rib != null) UiKit.Tag(rib, "제목 리본"); UiKit.Tag(box, "팝업 박스"); UiKit.Tag(timer, "종료 시각 줄"); TagClose(app);
        }

        /// <summary>«오늘의 선물» 칸 + 광고 줄 N개를 <paramref name="host"/> 에 그린다(상태가 바뀌면 <paramref name="refresh"/> 로 이 안만 다시 그린다).</summary>
        static void BuildGiftRows(App app, RectTransform host, Layout.R B, DailyGiftData D, string today, Action refresh)
        {
            var S = app.Save; var ov = app.Overlay;
            // ── «오늘의 선물»(무료 1칸 · 주인 확정 «무료 1칸 = 다이아 100» → dailyGift.json freeGift.gem)
            var T = Layout.GfTodayCell;
            var todayCell = UiKit.Panel(host, "Today", "fr.r12", Palette.A(Palette.Sky, 0.55f)); UiKit.Pct(todayCell.rectTransform, T.Within(B));
            UiKit.Bordered(todayCell.rectTransform);   // T69 — 칸 «검은 아웃라인»
            var th = Head(host, B, new Layout.R(T.X, T.Y, T.W, 2.4f), "오늘의 선물", Palette.A(Palette.Dim, 0.5f), "TodayHead");
            th.alignment = TextAnchor.MiddleLeft; UiKit.Pct(th.rectTransform, 8, 0, 90, 100);
            var gi = UiKit.Icon(th.transform.parent, "Icon", "pi.gift", Palette.Yellow); UiKit.Pct(gi.rectTransform, 1.5f, 10, 5, 80);
            double freeGem = D != null ? D.FreeGem : 0;
            Cell(host, B, new Layout.R(T.X + 1.6f, T.Y + 3.0f, 8.2f, 4.5f), "plum", "ui.gemRed", qty: freeGem > 0 ? UiKit.FmtQty(freeGem) : null, name: "TodayCell");
            bool canFree = D != null && Core.DailyGift.CanFree(S, D, today);
            GiftButton(host, B, Layout.GfTodayBtn, "TodayGetBtn", canFree ? GiftBtn.Claim : GiftBtn.Done, () =>
            {
                double g = Core.DailyGift.ClaimFree(S, D, today);
                if (g <= 0) return;
                app.Persist(); app.Current?.Refresh(); app.Toast($"다이아 {UiKit.FmtQty(g)} 수령!");
                refresh(); PopReward(host, "TodayCell");
            });

            // ── 광고 누적 줄(개수·값 전부 dailyGift.json — 코드에 숫자 없음)
            int n = D != null ? D.Milestones.Count : 0;
            RectTransform row1 = null, row2 = null, title1 = null, bar1 = null, reward1 = null, btn1 = null;
            for (int i = 0; i < n; i++)
            {
                var m = D.Milestones[i];
                float dy = i * Layout.GfRowPitch;
                var row = UiKit.Panel(host, "Ad:" + i, "fr.r12", Palette.A(Palette.Brown, 0.75f)); UiKit.Pct(row.rectTransform, Sh(Layout.GfRow1, 0, dy).Within(B));
                UiKit.Bordered(row.rectTransform);   // T69
                bool locked = Core.DailyGift.Locked(S, D, i, today);
                bool claimed = Core.DailyGift.Claimed(S, i);
                bool canClaim = Core.DailyGift.CanClaim(S, D, i, today);
                // 제목 칸 = 표 ㉒ 19.2×1.7%(207×40px) → 폭 GfRowTitleW 24% · 높이 LpLineH 2.2%(본문 40 «광고 6회 보기» ≈214px 가 줄바꿈되고 세로로 넘치던 것 · 이름표는 글자 덩어리를 잰다)
                var tR = Sh(Layout.GfRowTitle, 0, dy); tR = new Layout.R(tR.X, tR.Y, Layout.GfRowTitleW, tR.H).WithH(Layout.LpLineH);
                string label = m.Gift ? $"광고 {m.Ads}회 선물" : $"광고 {m.Ads}회 보기";
                var title = UiKit.Label(host, 0, 0, 100, 100, label, TextSize.Body, Palette.White, TextAnchor.MiddleLeft); title.name = "Title"; UiKit.Pct(title.rectTransform, tR.Within(B));
                int cur = S.GiftAds < m.Ads ? S.GiftAds : m.Ads;
                var bar = UiKit.MakeBar(host, "ui.sliderBlue"); bar.Root.name = "Bar"; UiKit.Pct(bar.Root, Sh(Layout.GfRowBar, 0, dy).WithH(Layout.LpBarH).Within(B));
                bar.Set(m.Ads > 0 ? (double)cur / m.Ads : 0, cur + "/" + m.Ads);
                var reward = Cell(host, B, Sh(Layout.GfRowReward, 0, dy), "plum", "ui.gemRed", qty: UiKit.FmtQty(m.Gem), locked: locked, name: "Reward:" + i);
                var st = claimed ? GiftBtn.Done : locked ? GiftBtn.Locked : canClaim ? GiftBtn.Claim : GiftBtn.Ad;
                int idx = i;
                var btn = GiftButton(host, B, Sh(Layout.GfRowBtn, 0, dy), "AdBtn", st, () =>
                {
                    if (st == GiftBtn.Claim)
                    {
                        double g = Core.DailyGift.Claim(S, D, idx, today);
                        if (g <= 0) return;
                        app.Persist(); app.Current?.Refresh(); app.Toast($"다이아 {UiKit.FmtQty(g)} 수령!");
                        refresh(); PopReward(host, "Reward:" + idx);
                    }
                    else   // 광고 보기 — 실제 광고 SDK 없음: T23 과 같은 모의 카운트다운 3초 뒤 누적 +1 (팝업을 다시 연다)
                    {
                        ov.AdCountdown(GiftAdSeconds, () => { Core.DailyGift.WatchAd(S, D, today); app.Persist(); app.Current?.Refresh(); DailyGift(app); });
                    }
                });
                if (i == 0) { row1 = row.rectTransform; title1 = title.rectTransform; bar1 = bar.Root; reward1 = reward; btn1 = btn; } else if (i == 1) row2 = row.rectTransform;
            }
            // 비평 이름표(표 ㉒ · 타임라인 두 행은 주인 지시로 삭제)
            UiKit.Tag(todayCell.transform, "오늘의 선물 칸"); UiKit.Tag(UiKit.Find(host, "TodayGetBtn"), "오늘의 선물 버튼");
            UiKit.Tag(row1, "광고 줄 1"); UiKit.Tag(row2, "광고 줄 2"); UiKit.Tag(title1, "광고 줄 제목(1줄)", textBounds: true); UiKit.Tag(bar1, "광고 줄 진행바(1줄)"); UiKit.Tag(reward1, "광고 줄 보상 아이콘(1줄)"); UiKit.Tag(btn1, "광고 줄 버튼(1줄)");
        }

        /// <summary>모의 광고 카운트다운 초 — T23(쉼터·천사)과 같은 3초.</summary>
        public const int GiftAdSeconds = 3;

        /// <summary>받은 보상 칸이 «팝» 하고 커졌다 돌아온다(T49 감각 · 다시 그린 뒤라 이름으로 찾는다).</summary>
        static void PopReward(RectTransform host, string name)
        {
            var c = UiKit.Find(host, name); if (c != null) UiKit.PopIn((RectTransform)c, 0.7f, 0.32f);
        }

        // ───────────────────────── 18 7일 챌린지 — T78(주인 2026-09-07 «7일 챌린지 걍 안 하고 싶음»)로 팝업째 삭제 ─────────────────────────

        // ───────────────────────── 30·31 탐험 · 빠른 탐험 (T97 — 방치·오프라인 보상) ─────────────────────────

        /// <summary>지금(UTC 유닉스 초) — 규칙(<see cref="Core.Expedition"/>)은 순수 C# 이라 시계를 게임 층이 준다.</summary>
        public static double NowSec() => (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        /// <summary>«3시간 48분 25초»(레퍼런스 30 의 «3h 48m 25s») — 0 이면 «0초».</summary>
        static string ExClock(double sec)
        {
            var t = TimeSpan.FromSeconds(sec < 0 ? 0 : sec);
            int h = (int)t.TotalHours;
            if (h > 0) return $"{h}시간 {t.Minutes}분 {t.Seconds}초";
            if (t.Minutes > 0) return $"{t.Minutes}분 {t.Seconds}초";
            return $"{t.Seconds}초";
        }

        /// <summary>«mm:ss»(레퍼런스 31 의 «Claim in: 11:21»).</summary>
        static string Mmss(double sec)
        {
            var t = TimeSpan.FromSeconds(sec < 0 ? 0 : Math.Ceiling(sec));
            return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
        }

        /// <summary>회색 제목 명판(레퍼런스 30·31 은 리본이 아니라 상자 폭을 채우는 띠) — 조각 <c>fr.rect</c> + 가운데 제목 글자.</summary>
        static RectTransform Plate(Transform parent, Layout.R parentR, Layout.R r, string title, string name = "Plate")
        {
            var p = UiKit.Panel(parent, name, "fr.rect", Palette.A(Palette.Slate, 0.85f)); var rt = p.rectTransform;
            UiKit.Pct(rt, r.Within(parentR)); UiKit.Bordered(rt);
            var t = UiKit.Label(rt, 6, 0, 88, 100, title, TextSize.Title, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Title);
            t.fontStyle = FontStyle.Bold;
            return rt;
        }

        /// <summary>시간당 비율 pill(레퍼런스 30 의 «🪙 1650/h» · «💎 10/h») — 아이콘 + 글자 한 줄.</summary>
        static RectTransform RatePill(Transform parent, Layout.R parentR, Layout.R r, string icon, string text, string name)
        {
            var pill = UiKit.Panel(parent, name, "fr.r12", Palette.A(Palette.Dim, 0.75f)); var rt = pill.rectTransform;
            UiKit.Pct(rt, r.Within(parentR)); UiKit.Bordered(rt);
            var ic = UiKit.Icon(rt, "Icon", icon); UiKit.Pct(ic.rectTransform, 2, 8, 22, 84);
            UiKit.Label(rt, 26, 0, 70, 100, text, TextSize.Body, Palette.White, TextAnchor.MiddleLeft);
            return rt;
        }

        /// <summary>버튼 오른쪽 위 빨간 배지(레퍼런스 30·31 의 남은 횟수·«!») — 버튼 안 <see cref="Layout.ExBtnBadge"/> 자리.</summary>
        static void BtnBadge(RectTransform btn, string text, string name)
        {
            var bg = UiKit.Panel(btn, name, "fr.r12", Palette.Red); UiKit.Pct(bg.rectTransform, Layout.ExBtnBadge);
            UiKit.Bordered(bg.rectTransform);
            UiKit.Label(bg.rectTransform, 0, 0, 100, 100, text, TextSize.Aux, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Aux);
        }

        /// <summary>상자 맨 위 풍경 띠(레퍼런스 30) — 전투 맵과 같은 Environment 조각(들판 · 길 · 나무 · 덤불)으로 만든 정지 그림. 새 그림 0.</summary>
        static RectTransform Picture(Transform parent, Layout.R parentR, Layout.R r)
        {
            var pic = UiKit.Rect(parent, "Picture"); UiKit.Pct(pic, r.Within(parentR));
            pic.gameObject.AddComponent<RectMask2D>();
            var field = UiKit.Icon(pic, "Field", "env.field"); field.preserveAspect = false; UiKit.Stretch(field.rectTransform);
            var road = UiKit.Icon(pic, "Road", "env.road"); road.preserveAspect = false; UiKit.Pct(road.rectTransform, 0, 62, 100, 38);
            var edge = UiKit.Icon(pic, "RoadUp", "env.roadUp"); edge.preserveAspect = false; UiKit.Pct(edge.rectTransform, 0, 57, 100, 8);
            for (int i = 0; i < 3; i++) { var t = UiKit.Icon(pic, "Tree" + i, "env.tree"); UiKit.Pct(t.rectTransform, 6 + i * 34, 12, 16, 46); }
            var bush = UiKit.Icon(pic, "Bush", "env.bush"); UiKit.Pct(bush.rectTransform, 78, 66, 12, 22);
            UiKit.Bordered(pic);
            return pic;
        }

        /// <summary>
        /// 탐험 팝업(표 ㉕ · <b>T97 = 실제로 동작한다</b>) — 주인 2026-09-07 «탐험은 걍 방치 + 오프라인 보상 · 켜두거나 꺼둬도 쩄든 쌓이고 · 골드·다이아».
        /// 그림 띠 → «탐험 보상» 명판 → 안내 → 경과 시간(1초 갱신) → 시간당 pill 2 → 쌓인 보상 칸(골드·다이아) → 상한 안내 →
        /// «빠른 탐험»(파랑 · 남은 횟수 배지 · <see cref="QuickExplore"/>) + «받기»(초록 · 받을 게 있으면 «!» · 없으면 «다음까지 mm:ss»).
        /// 쌓인 양은 저장하지 않는다 — <see cref="Core.Expedition"/> 이 «마지막 정산 시각» 하나로 계산하므로 앱이 꺼져 있어도 같은 속도로 쌓인다.
        /// </summary>
        public static void Expedition(App app)
        {
            var ov = app.Overlay; var B = Layout.ExBox; var S = app.Save;
            var G = app.Data; var D = G != null ? G.Expedition : null;
            string today = SaveStore.Today();
            if (D != null) Core.Expedition.Roll(S, D, NowSec(), today);

            var box = ov.OpenBox("ui.popup", "ui.title.green", "", B, () => ov.Close()); box.name = "ExpeditionBox";
            var rib = ChildStarting(box, "Title_01"); if (rib != null) rib.gameObject.SetActive(false);   // 레퍼런스 30 은 리본이 아니라 상자 폭 명판이다
            var pic = Picture(box, B, Layout.ExPic);
            var plate = Plate(box, B, Layout.ExPlate, "탐험 보상");
            var info = UiKit.Icon(box, "InfoBtn", "pi.info", Palette.White); UiKit.Pct(info.rectTransform, Layout.ExInfoBtn.Within(B));
            var subR = Layout.ExSub.Within(B);
            UiKit.Label(box, subR.X, subR.Y, subR.W, subR.H, "시간이 지나면 저절로 쌓입니다", TextSize.Aux, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Aux).name = "Sub";

            var host = UiKit.Rect(box, "ExpRows"); UiKit.Stretch(host);   // «받기» 뒤에 이 안만 다시 그린다(팝업을 다시 열지 않는다 = 열림음 1번)
            Action refresh = null;
            refresh = () => { UiKit.Clear(host); BuildExpedition(app, host, B, D, today, refresh); };
            refresh();

            // 비평 이름표(표 ㉕) — 다시 그려도 자리가 같은 것만 여기서(줄 조각의 이름표는 BuildExpedition 안)
            UiKit.Tag(box, "팝업 박스"); UiKit.Tag(pic, "그림 띠"); UiKit.Tag(plate, "제목 명판"); UiKit.Tag(info.rectTransform, "안내 ⓘ"); TagClose(app);
        }

        /// <summary>탐험 팝업의 «변하는» 부분 — 경과 시간·시간당 pill·쌓인 칸·버튼 2개. 1초마다 글자만 고치고, 받은 뒤에는 통째로 다시 그린다.</summary>
        static void BuildExpedition(App app, RectTransform host, Layout.R B, ExpeditionData D, string today, Action refresh)
        {
            var ov = app.Overlay; var S = app.Save; var G = app.Data;
            double now = NowSec();
            double perGold = Core.Expedition.GoldPerHour(G, S, D), perGem = Core.Expedition.GemPerHour(D);
            var timeR = Layout.ExTime.Within(B);
            var timeTxt = UiKit.Label(host, timeR.X, timeR.Y, timeR.W, timeR.H,
                D == null ? "탐험 시간: --" : "탐험 시간: " + ExClock(Core.Expedition.ElapsedSec(S, D, now, today)),
                TextSize.Title, Palette.Green, TextAnchor.MiddleCenter, true, true, TextKind.Title);
            timeTxt.name = "ExpTime"; timeTxt.fontStyle = FontStyle.Bold;
            var p1 = RatePill(host, B, Layout.ExRatePill1, "ui.coin", UiKit.Fmt(Math.Floor(perGold)) + "/시간", "RateGold");
            var p2 = RatePill(host, B, Layout.ExRatePill2, "ui.gemRed", UiKit.FmtQty(Math.Floor(perGem)) + "/시간", "RateGem");

            var gridBg = UiKit.Panel(host, "GridBg", "fr.r12", Palette.A(Palette.Dim, 0.55f));
            UiKit.Pct(gridBg.rectTransform, Layout.ExGridBg.Within(B)); UiKit.Bordered(gridBg.rectTransform);

            double gold = 0, gem = 0; if (D != null) Core.Expedition.Pending(G, S, D, now, today, out gold, out gem);
            // 쌓인 보상 칸 — 우리 시스템의 보상은 골드·다이아 둘뿐이라 레퍼런스의 장비 조각 자리는 비워 둔다(결정 기록)
            Cell(host, B, Layout.ExCell, "green", "ui.coin", UiKit.Fmt(gold), name: "ExpCellGold");
            Cell(host, B, Sh(Layout.ExCell, Layout.ExCellPitchX, 0), "plum", "ui.gemRed", UiKit.FmtQty(gem), name: "ExpCellGem");

            var capR = Layout.ExCapNote.Within(B);
            double maxH = D != null ? D.MaxHours : 0;
            var cap = UiKit.Label(host, capR.X, capR.Y, capR.W, capR.H,
                $"최대 탐험 시간: {UiKit.FmtQty(maxH)}시간\n뒤 챕터일수록 보상이 좋습니다", TextSize.Aux, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Aux);
            cap.name = "CapNote";

            // 빠른 탐험(파랑) — 남은 횟수 배지 · 0 이면 회색 비활성
            int left = D != null ? Core.Expedition.QuickLeft(S, D, now, today) : 0;
            var qb = UiKit.Button(host, left > 0 ? "ui.btnBlue" : "ui.btnGray", "빠른 탐험",
                left > 0 ? (Action)(() => QuickExplore(app, refresh)) : () => { }, Layout.ExQuickBtn.Within(B));
            qb.name = "QuickBtn";
            if (left <= 0) UiKit.SetInteractable(qb.GetComponent<Button>(), false); else BtnBadge(qb, left.ToString(), "QuickBadge");

            // 받기(초록) — 받을 게 있으면 «!» 배지 · 없으면 회색 + «다음까지 mm:ss»
            bool can = D != null && Core.Expedition.CanClaim(G, S, D, now, today);
            var cb = UiKit.Button(host, can ? "ui.btnGreen" : "ui.btnGray",
                can ? "받기" : "다음까지 " + (D != null ? Mmss(Core.Expedition.SecondsToClaim(S, D, now, today)) : "--:--"),
                can ? (Action)(() =>
                {
                    Core.Expedition.Claim(G, S, D, NowSec(), today, out double gg, out double mm);
                    app.Persist();
                    refresh(); PopReward(host, "ExpCellGold");
                    app.Toast($"골드 +{UiKit.Fmt(gg)} · 다이아 +{UiKit.FmtQty(mm)}");
                }) : () => { }, Layout.ExClaimBtn.Within(B));
            cb.name = "ClaimBtn";
            if (!can) UiKit.SetInteractable(cb.GetComponent<Button>(), false); else BtnBadge(cb, "!", "ClaimBadge");

            // 1초 갱신 — 경과 시간과 «다음까지» 만 고친다(오브젝트를 다시 만들지 않는다 · 트윈이 아니라 경고 0)
            var claimTxt = UiKit.ButtonText(cb);
            float acc = 0f;
            ov.OnTick = () =>
            {
                acc += Time.unscaledDeltaTime; if (acc < 1f) return; acc = 0f;
                if (D == null) return;
                double t = NowSec();
                if (timeTxt != null) timeTxt.text = "탐험 시간: " + ExClock(Core.Expedition.ElapsedSec(S, D, t, today));
                bool nowCan = Core.Expedition.CanClaim(G, S, D, t, today);
                if (nowCan != can) { refresh(); return; }                      // 받기가 열리면 버튼 색까지 바뀌므로 그때만 다시 그린다
                if (!nowCan && claimTxt != null) claimTxt.text = "다음까지 " + Mmss(Core.Expedition.SecondsToClaim(S, D, t, today));
            };

            UiKit.Tag(timeTxt.rectTransform, "경과 시간"); UiKit.Tag(p1, "시간당 pill ①"); UiKit.Tag(p2, "시간당 pill ②");
            UiKit.Tag(gridBg.rectTransform, "보상 격자 바탕"); UiKit.Tag(cap.rectTransform, "상한 안내 띠");
            UiKit.Tag(qb, "빠른 탐험 버튼"); UiKit.Tag(cb, "받기 버튼");
            var c0 = UiKit.Find(host, "ExpCellGold"); if (c0 != null) UiKit.Tag(c0, "보상 칸(1칸)");
        }

        /// <summary>
        /// 빠른 탐험 팝업(표 ㉖ · 레퍼런스 31) — 탐험 팝업 «위에» 겹치는 작은 상자. 주인 «빠른 탐험은 광고 보고 얻는 식»:
        /// «받을 보상» 칸(골드·다이아 = 시간당 × <c>quickHours</c>) → «🎬 무료» 버튼 → 모의 광고 3초(T23 <see cref="Overlay.AdCountdown"/>) → 즉시 지급.
        /// 지급은 누적에 더하지 않는다(중복 수령 방지 · ROUTINE T97 4항) · 하루 횟수를 하나 쓴다.
        /// </summary>
        public static void QuickExplore(App app, Action after)
        {
            var ov = app.Overlay; var B = Layout.QxBox; var S = app.Save; var G = app.Data;
            var D = G != null ? G.Expedition : null;
            string today = SaveStore.Today();
            double now = NowSec();
            var box = ov.OpenBox("ui.popup", "ui.title.green", "", B, () => { ov.Close(); if (after != null) { LobbyPopups.Expedition(app); } }); box.name = "QuickExploreBox";
            var rib = ChildStarting(box, "Title_01"); if (rib != null) rib.gameObject.SetActive(false);
            var plate = Plate(box, B, Layout.QxPlate, "빠른 탐험", "QxPlate");
            var subR = Layout.QxSub.Within(B);
            UiKit.Label(box, subR.X, subR.Y, subR.W, subR.H, "탐험 보상을 한 번에 받습니다", TextSize.Aux, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Aux).name = "QxSub";
            var ttR = Layout.QxTitle.Within(B);
            var tt = UiKit.Label(box, ttR.X, ttR.Y, ttR.W, ttR.H, "받을 보상", TextSize.Body, Palette.White, TextAnchor.MiddleCenter); tt.name = "QxTitle"; tt.fontStyle = FontStyle.Bold;

            var gridBg = UiKit.Panel(box, "QxGridBg", "fr.r12", Palette.A(Palette.Dim, 0.55f));
            UiKit.Pct(gridBg.rectTransform, Layout.QxGridBg.Within(B)); UiKit.Bordered(gridBg.rectTransform);
            double gold = 0, gem = 0; if (D != null) Core.Expedition.QuickReward(G, S, D, out gold, out gem);
            Cell(box, B, Layout.QxCell, "green", "ui.coin", UiKit.Fmt(gold), name: "QxCellGold");
            Cell(box, B, Sh(Layout.QxCell, Layout.QxCellPitchX, 0), "plum", "ui.gemRed", UiKit.FmtQty(gem), name: "QxCellGem");

            var noteR = Layout.QxNote.Within(B);
            var note = UiKit.Label(box, noteR.X, noteR.Y, noteR.W, noteR.H,
                D != null ? $"{UiKit.FmtQty(D.QuickHours)}시간치를 즉시 받습니다" : "--", TextSize.Body, Palette.White, TextAnchor.MiddleCenter);
            note.name = "QxNote";

            int left = D != null ? Core.Expedition.QuickLeft(S, D, now, today) : 0;
            var fb = UiKit.Button(box, left > 0 ? "ui.btnBlue" : "ui.btnGray", "광고 보고 무료",
                left > 0 ? (Action)(() =>
                {
                    ov.AdCountdown(GiftAdSeconds, () =>
                    {
                        Core.Expedition.ClaimQuick(G, S, D, NowSec(), today, out double gg, out double mm);
                        app.Persist();
                        app.Toast($"골드 +{UiKit.Fmt(gg)} · 다이아 +{UiKit.FmtQty(mm)}");
                        LobbyPopups.Expedition(app);   // 광고가 끝나면 탐험 팝업으로 돌아간다(남은 횟수·버튼이 갱신된다)
                    });
                }) : () => { }, Layout.QxFreeBtn.Within(B));
            fb.name = "QxFreeBtn";
            if (left <= 0) UiKit.SetInteractable(fb.GetComponent<Button>(), false); else BtnBadge(fb, left.ToString(), "QxBadge");

            UiKit.Tag(box, "팝업 박스"); UiKit.Tag(plate, "제목 명판"); UiKit.Tag(gridBg.rectTransform, "보상 칸 바탕");
            UiKit.Tag(note.rectTransform, "안내 문구"); UiKit.Tag(fb, "광고 버튼"); TagClose(app);
            var qc = UiKit.Find(box, "QxCellGold"); if (qc != null) UiKit.Tag(qc, "보상 칸(1칸)");
        }
    }

    /// <summary>
    /// 특권 페이지 = <c>docs/ref/11_shop_special.jpg</c> 구도(T44 · 표 ⑲ · 껍데기): 상단 재화 바 → ⭐ «특권» 제목 + 밑줄 → 부제 → 카드 4장 세로 스크롤(짧은 카드 «일일 선물» 1 + 긴 카드 3 = 제목 띠(아이콘 · 이름 · «비활성») · 설명 상자(글머리 줄) · 큰 그림 · «매일 수령» 보상 칸 · 버튼) → 바닥 바(뒤로 ◀ · «전체 받기»). 탭 바 없음.
    /// 전부 표시만(버튼은 눌러도 아무 일 없음 · 가격·상태 데이터 없음 → «비활성»·«구매»). 이름 계약(스모크): <c>Card:N</c> · <c>BackBtn</c> · <c>ClaimAllBtn</c>.
    /// </summary>
    public sealed class PrivilegeScreen : GameScreen
    {
        public override string Name => "privilege";
        TopBar _top;
        /// <summary>T72 ①③ 카드 안 질감을 들여 까는 여백(px) — 카드 조각(<c>fr.r12</c>)의 둥근 모서리 반지름 12px 의 1/3(사각 무늬·그라데이션이 모서리 밖으로 안 삐져나오는 선 · EventsScreen 제목 띠와 같은 값).</summary>
        const float CardTextureInset = 4f;
        /// <summary>T72 ② 빛살을 걸 자리 — 배치가 끝난 <b>뒤</b>에 한꺼번에 건다(그 전에는 % 앵커 조각의 rect 가 0 이라 빛살 한 변이 0 이 된다 · 결정 174).</summary>
        readonly List<(RectTransform host, RectTransform icon, string key)> _lightPlan = new List<(RectTransform, RectTransform, string)>();

        static Layout.R Sh(Layout.R r, float dx, float dy) => new Layout.R(r.X + dx, r.Y + dy, r.W, r.H);

        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Color.Lerp(Palette.Slate, Palette.Dim, 0.6f); bg.raycastTarget = true;
            // T72 ① 풀스크린 배경 패턴(주인 «특별 상품들도 마찬가지») — 바탕은 Root 자신의 Image 라 형제 0 = 상단 바·제목·카드·바닥 바 아래 · 어두운 바탕이라 흰 무늬(레퍼런스 11 도 어두운 회색 위 밝은 무늬)
            UiKit.PatternBg(Root, UiKit.PatternTintDark);
            _top = TopBar.Build(App, Root);
            // 제목(⭐ 특권) + 밑줄 + 부제
            var title = UiKit.Rect(Root, "Title"); UiKit.Pct(title, Layout.PrTitle);
            var star = UiKit.Icon(title, "Icon", "pi.star", Palette.Yellow); UiKit.Pct(star.rectTransform, 0, 0, 22, 100);
            // 페이지 제목 = 제목 종류 60(전 52 · 칸 3.0%×120% = 84px ≥ 한 줄 ≈66px)
            var tt = UiKit.Label(title, 25, -10, 75, 120, "특권", TextSize.Title, Palette.White, TextAnchor.MiddleLeft, kind: TextKind.Title); tt.fontStyle = FontStyle.Bold;
            var line = UiKit.Icon(Root, "Underline", "fr.lineDeco", Palette.A(Palette.White, 0.45f)); line.preserveAspect = false; UiKit.Pct(line.rectTransform, Layout.PrUnderline);
            // 부제 = 본문 40 · 칸 표 ⑲ 1.8%(42px) → LpLineH 2.2% · 문구는 «활성화하고»→«활성화해»(40 이면 628px 로 칸 폭 58%(626px)를 1~2px 넘겨 두 줄이 되던 것 · T63 2항 ⓒ 문구 줄이기)
            var sub = UiKit.Label(Root, 0, 0, 100, 100, "특권을 활성화해 놀라운 보상을 받으세요!", TextSize.Body, Palette.CreamDark); sub.name = "Sub"; UiKit.Pct(sub.rectTransform, Layout.PrSub.WithH(Layout.LpLineH));
            // 카드 4장 — 세로 스크롤(카드 4 는 바닥 바에 잘린다 · 레퍼런스 그대로)
            float top = Layout.PrCard1.Y, bottom = Layout.PrCard4.Y + Layout.PrCard4.H + 1.5f;
            var view = UiKit.Rect(Root, "Scroll"); UiKit.Pct(view, new Layout.R(0, top, 100, Layout.PrFootBar.Y - top)); UiKit.Ensure<RectMask2D>(view.gameObject);
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            var sr = view.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40;
            var content = UiKit.Rect(view, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero; content.sizeDelta = new Vector2(0, (bottom - top) / 100f * UiKit.FrameH);
            sr.content = content; sr.viewport = view;
            var C = new Layout.R(0, top, 100, bottom - top);
            // 카드 1 = 일일 선물(짧은 카드)
            var card1 = UiKit.Panel(content, "Card:1", "fr.r12", Palette.Sky); UiKit.Pct(card1.rectTransform, Layout.PrCard1.Within(C));
            CardTexture(card1.rectTransform);
            var head1 = UiKit.Panel(content, "Head:1", "fr.r12", Palette.A(Palette.Blue, 0.9f)); UiKit.Pct(head1.rectTransform, new Layout.R(Layout.PrCard1.X, Layout.PrCard1.Y, Layout.PrCard1.W, 3.6f).Within(C));
            UiKit.Gradient(head1.rectTransform, inset: CardTextureInset);   // T72 ③ 카드 제목 띠(레퍼런스 11 의 띠도 위 밝고 아래 어둡다)
            CardHead(head1.transform, "ui.iconGiftRed", "일일 선물", "초기화까지 " + LobbyPopups.Dashes);
            var reward1 = LobbyPopups.Cell(content, C, Layout.PrCard1Reward, "plum", "ui.gemRed");
            PlanRewardLight(reward1);
            var btn1 = UiKit.Button(content, "ui.btnGray", "받기", () => { }, Layout.PrCard1Btn.Within(C)); btn1.name = "CardBtn:1";
            // 카드 2~4 = 긴 카드
            RectTransform card2 = null, cardTitle2 = null, desc2 = null, pic2 = null, reward2 = null, btn2 = null, card3 = null, card4 = null;
            (Layout.R rect, Color color, string icon, string name, string pic, string[] lines, string btnKey, string btnLabel)[] longs =
            {
                (Layout.PrCard2, Palette.Blue, "ui.ad", "광고 제거 카드", "ui.ad", new[] { "영구 광고 제거 특권", "구매 시 💎 지급" }, "ui.btnGray", "받기"),
                (Layout.PrCard3, Palette.Plum, "ui.iconCalendar", "월간 카드", "ui.iconMedal", new[] { "최대 탐험 시간 24시간", "던전 티켓 +2 / 일", "최대 배속 +1", "구매 시 💎 지급" }, "ui.btnOrange", "구매"),
                (Layout.PrCard4, Palette.Orange, "ui.gemRed", "평생 다이아", "ui.trophy", new[] { "매일 다이아 대량 수령", "구매 시 💎 지급" }, "ui.btnOrange", "구매"),
            };
            for (int k = 0; k < longs.Length; k++)
            {
                var L = longs[k]; float dy = L.rect.Y - Layout.PrCard2.Y;
                var card = UiKit.Panel(content, "Card:" + (k + 2), "fr.r12", L.color); UiKit.Pct(card.rectTransform, L.rect.Within(C));
                CardTexture(card.rectTransform);
                var head = UiKit.Panel(content, "Head:" + (k + 2), "fr.r12", Palette.A(Palette.Dim, 0.35f)); UiKit.Pct(head.rectTransform, Sh(Layout.PrCardTitle, 0, dy).Within(C));
                UiKit.Gradient(head.rectTransform, inset: CardTextureInset);   // T72 ③ 카드 제목 띠
                CardHead(head.transform, L.icon, L.name, "비활성");
                var desc = UiKit.Panel(content, "Desc:" + (k + 2), "fr.r12", Palette.A(Palette.Dim, 0.35f)); UiKit.Pct(desc.rectTransform, Sh(Layout.PrCardDesc, 0, dy).Within(C));
                float lh = 100f / Mathf.Max(2, L.lines.Length);
                for (int i = 0; i < L.lines.Length; i++)
                {
                    float ly = 4 + i * lh * 0.92f;
                    var bullet = UiKit.Icon(desc.transform, "Bullet", "pi.star", Palette.Yellow); UiKit.Pct(bullet.rectTransform, 4, ly + lh * 0.2f, 6, lh * 0.5f);   // 글머리 = 작은 노란 별(레퍼런스 금색 마름모 자리 · 글자 아님)
                    UiKit.Label(desc.transform, 12, ly, 86, lh * 0.9f, L.lines[i], TextSize.Body, Palette.White, TextAnchor.MiddleLeft);
                }
                var pic = UiKit.Icon(content, "Pic:" + (k + 2), L.pic); UiKit.Pct(pic.rectTransform, Sh(Layout.PrCardPic, 0, dy).Within(C));
                var daily = UiKit.Label(content, 0, 0, 100, 100, "매일 수령", TextSize.Body, Palette.Yellow, TextAnchor.MiddleLeft); daily.name = "Daily"; daily.fontStyle = FontStyle.Bold;
                UiKit.Pct(daily.rectTransform, new Layout.R(8.6f, Layout.PrCardReward.Y + dy, 26.0f, Layout.PrCardReward.H).Within(C));
                var reward = LobbyPopups.Cell(content, C, Sh(Layout.PrCardReward, 0, dy), "plum", "ui.gemRed");
                PlanRewardLight(reward);
                // T72 ② 카드 그림 뒤 빛살(주인 «특별 상품 … 아이콘 뒤에 Effect_Light 천천히 회전») — 그림은 카드의 «형제» 라 빛살은 카드 안(칸 밖으로 안 나가게 RectMask2D)에 걸고 그림은 그 위에 그대로 남는다
                _lightPlan.Add((card.rectTransform, pic.rectTransform, UiKit.LightKey));
                var btn = UiKit.Button(content, L.btnKey, L.btnLabel, () => { }, Sh(Layout.PrCardBtn, 0, dy).Within(C)); btn.name = "CardBtn:" + (k + 2);
                if (k == 0) { card2 = card.rectTransform; cardTitle2 = head.rectTransform; desc2 = desc.rectTransform; pic2 = pic.rectTransform; reward2 = reward; btn2 = btn; }
                else if (k == 1) card3 = card.rectTransform; else card4 = card.rectTransform;
            }
            // 바닥 바(뒤로 · 전체 받기)
            var foot = UiKit.Panel(Root, "FootBar", "fr.rect", Palette.A(Palette.Dim, 0.9f)); UiKit.Pct(foot.rectTransform, Layout.PrFootBar);
            var back = UiKit.Button(Root, "ui.btnGray", "", () => App.ShowScreen("lobby"), Layout.PrBack); back.name = "BackBtn";
            var bi = UiKit.Icon(back, "Icon", "pi.arrow_left", Palette.Ink); UiKit.Pct(bi.rectTransform, 30, 18, 40, 64);
            var claim = UiKit.Button(Root, "ui.btnGray", "전체 받기", () => { }, Layout.PrClaimAll); claim.name = "ClaimAllBtn";
            // 비평 이름표(표 ⑲)
            UiKit.Tag(_top.Root, "상단 바"); UiKit.Tag(title, "제목 줄"); UiKit.Tag(line.transform, "제목 밑줄"); UiKit.Tag(sub.transform, "부제");
            UiKit.Tag(card1.transform, "특권 카드 1"); UiKit.Tag(reward1, "카드 1 보상 칸"); UiKit.Tag(btn1, "카드 1 버튼");
            UiKit.Tag(card2, "특권 카드 2"); UiKit.Tag(cardTitle2, "카드 제목 띠(2)"); UiKit.Tag(desc2, "카드 설명 상자(2)"); UiKit.Tag(pic2, "카드 그림(2)"); UiKit.Tag(reward2, "카드 보상 칸(2)"); UiKit.Tag(btn2, "카드 버튼(2)");
            UiKit.Tag(card3, "특권 카드 3"); UiKit.Tag(card4, "특권 카드 4 (참고·컨테이너)");
            UiKit.Tag(foot.transform, "바닥 바"); UiKit.Tag(back, "뒤로 버튼"); UiKit.Tag(claim, "전체 받기 버튼");
            ApplyLights();
        }

        /// <summary>T72 ①③ 특권 카드 안 질감 — 카드 조각 위에 무늬 한 장(밝은 색 카드라 흰 무늬 · 레퍼런스 11 의 카드 안에도 무늬가 어른거린다) + 그라데이션 두 장(위 밝음 → 아래 어둠). 카드의 내용(제목 띠·설명·그림·보상·버튼)은 카드의 <b>형제</b> 라 이 두 층 위에 그대로 남는다.</summary>
        static void CardTexture(RectTransform card)
        {
            if (card == null) return;
            UiKit.PatternBg(card, UiKit.PatternTintDark, UiKit.PatternTileSeconds, 0, UiKit.PatternTilePx, CardTextureInset);
            UiKit.Gradient(card, inset: CardTextureInset);
        }

        /// <summary>T72 ② 보상 칸(다이아) 그림 뒤 빛살 예약 — 칸 조각(ItemFrame_01) 안 «Item» 바로 뒤(프레임 안쪽에서만 보인다 · 작은 조각).</summary>
        void PlanRewardLight(RectTransform cell)
        {
            var item = cell != null ? UiKit.Find(cell, "Item") : null;
            if (item == null || !item.gameObject.activeSelf) return;
            _lightPlan.Add(((RectTransform)item.parent, (RectTransform)item, UiKit.LightKeySmall));
        }

        /// <summary>예약해 둔 빛살을 배치가 끝난 뒤 한꺼번에 건다(결정 174) — 카드 그림 뒤 빛살은 카드 안 질감층(무늬·그라데이션) <b>위</b>로 올린다(질감 층 순서 = 바탕 → 패턴 → 그라데이션 → 빛살 → 내용 · 결정 171).</summary>
        void ApplyLights()
        {
            if (_lightPlan.Count == 0) return;
            Canvas.ForceUpdateCanvases();
            foreach (var l in _lightPlan)
            {
                UiKit.LightBehind(l.host, l.icon, l.key);
                if (l.icon != null && l.icon.parent == l.host) continue;
                var mask = l.host.Find(UiKit.LightMaskName); if (mask != null) mask.SetAsLastSibling();
            }
            _lightPlan.Clear();
        }

        /// <summary>카드 제목 띠 안 — 왼쪽 아이콘 + 이름 · 오른쪽 상태/타이머 글자.</summary>
        static void CardHead(Transform head, string icon, string name, string right)
        {
            var ic = UiKit.Icon(head, "Icon", icon); UiKit.Pct(ic.rectTransform, 2, 10, 8, 80);
            var t = UiKit.Label(head, 11, 0, 50, 100, name, TextSize.Body, Palette.White, TextAnchor.MiddleLeft); t.fontStyle = FontStyle.Bold;
            UiKit.Label(head, 62, 0, 36, 100, right, TextSize.Body, Palette.White, TextAnchor.MiddleRight);
        }

        public override void Refresh() { _top?.Refresh(); }
    }
}
