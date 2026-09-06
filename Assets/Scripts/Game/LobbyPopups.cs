using System;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 로비 사이드 팝업 껍데기 6종 (T44 · 주인 2026-09-06 «UI 는 무조건 레퍼런스 기준» · ⓔ «시스템이 없는 화면은 레이아웃 껍데기»):
    /// 퀘스트(<c>docs/ref/15_quest.jpg</c> · 표 ⑳) · 출석(<c>16</c> · ㉑) · 데일리 기프트(<c>17</c> · ㉒) · 7일 챌린지(<c>18</c> · ㉓) 는 <b>팝업</b>(공통 팝업 문법 <see cref="UiKit.Popup"/> · 배경 탭 = 닫기),
    /// 특권(<c>11</c> · ⑲ · <see cref="PrivilegeScreen"/>) · 시즌 패스(<c>19</c> · ㉔ · <see cref="PassScreen"/>) 는 <b>페이지</b>(상단 재화 바 + 뒤로 ◀ · 탭 바 없음).
    /// 시스템이 없으므로 <b>전부 표시만</b> — 버튼은 눌러도 아무 일 없음 · 숫자는 0(레퍼런스 숫자를 베끼지 않는다 · 타이머는 «--:--:--») · 글자는 레퍼런스 글자를 우리말로.
    /// 재료 = GUI Pro 조각만(ui.popup 패널 · Title_01 리본 · ItemFrame_01 칸 · fr.r12/fr.rect 9-slice · 아이콘 · 버튼 · 슬라이더 · Environment 들판/길/나무) · 코드 도형 0 · 새 그림 0.
    /// 배치 = <see cref="Layout"/> ⑲~㉔ 상수(프레임 % · ±3%p) · 비평 이름표(T46)는 표의 «요소» 글자 그대로.
    /// 진입 = <see cref="LobbyScreen.OnSide"/>(사이드 아이콘 6 + 이벤트 배너). 시스템이 생기면 각 함수의 글자·숫자 자리에 데이터를 넣는다(배치는 그대로).
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
            var t = rr.GetComponentInChildren<Text>(true); if (t != null) { t.resizeTextMinSize = 20; t.resizeTextMaxSize = 56; }
            return rr;
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
            if (!string.IsNullOrEmpty(qty)) { var q = UiKit.Label(cell, 24, 58, 72, 40, qty, 26, Palette.White, TextAnchor.LowerRight); q.name = "Qty"; q.fontStyle = FontStyle.Bold; }
            if (locked) { var lk = UiKit.Icon(cell, "Lock", "ui.iconLock"); UiKit.Pct(lk.rectTransform, 64, -16, 44, 44); }
            return cell;
        }

        /// <summary>⏱ + 글자 한 줄(타이머 자리 · 시스템 없음 → «--:--:--»).</summary>
        static RectTransform TimerRow(Transform parent, Layout.R parentR, Layout.R r, string text, string name = "Timer")
        {
            var row = UiKit.Rect(parent, name); UiKit.Pct(row, r.Within(parentR));
            var ic = UiKit.Icon(row, "Icon", "pi.time", Palette.White); UiKit.Pct(ic.rectTransform, 0, -10, 9, 120);
            UiKit.Label(row, 11, -20, 89, 140, text, 30, Palette.White, TextAnchor.MiddleLeft);
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
                if (i == 0) { var c = UiKit.Rect(host, "Score"); UiKit.Pct(c, r.Within(parentR)); var ic = UiKit.Icon(c, "Icon", icons[0]); UiKit.Stretch(ic.rectTransform); cells[i] = c; }
                else cells[i] = Cell(host, parentR, r, i % 2 == 1 ? "green" : "plum", icons[i % icons.Length], null, false, "Track:" + i);
            }
            var numsRow = UiKit.Rect(host, "Nums"); UiKit.Pct(numsRow, numsR.Within(parentR));
            for (int i = 0; i < count; i++)
            {
                var r = Sh(icon1, i * pitch, 0); float cx = (r.X + r.W / 2f - numsR.X) / numsR.W * 100f;
                UiKit.Label(numsRow, cx - 9, -30, 18, 160, nums[i % nums.Length], 30, Palette.Yellow);
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
            return UiKit.Label(p.transform, 4, 0, 92, 100, text, 30, Palette.White);
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

        /// <summary>퀘스트 팝업(표 ⑳) — 파란 제목 띠 «퀘스트»(리본이 아니라 박스 폭 · 박스 윗변에 붙음) → 점수 트랙 상자(메달 + 보상 5 · 숫자) → ⏱ 새로고침 줄 → 목록 상자(줄 6 · 스크롤 · 메달·제목·진행바·«이동») → 박스 아래 탭 3(일일 활성 · 주간 · 업적) → «탭하여 닫기».</summary>
        public static void Quest(App app)
        {
            var ov = app.Overlay; var B = Layout.QsBox;
            var box = ov.OpenBox("ui.popup", "ui.title.sky", "퀘스트", B, () => ov.Close()); box.name = "QuestBox";
            var band = Ribbon(box, "ui.title.sky", Layout.QsTitleBand, B);
            var trackBox = UiKit.Panel(box, "TrackBox", "fr.r12", Palette.A(Palette.Dim, 0.55f)); UiKit.Pct(trackBox.rectTransform, Layout.QsTrackBox.Within(B));
            Track(box, B, Layout.QsTrackIcon, Layout.QsTrackPitch, Layout.QsTrackCount, Layout.QsTrackNums, Palette.Yellow, TrackIcons, QuestNums, "트랙 아이콘 줄(6칸)", "트랙 아이콘(1칸)");
            var refresh = TimerRow(box, B, Layout.QsRefresh, "새로고침까지 " + Dashes, "Refresh");
            var listBox = UiKit.Panel(box, "ListBox", "fr.r12", Palette.A(Palette.Dim, 0.55f)); UiKit.Pct(listBox.rectTransform, Layout.QsListBox.Within(B));
            var viewR = new Layout.R(Layout.QsRow1.X, Layout.QsRow1.Y, Layout.QsRow1.W, Layout.QsListBox.Y + Layout.QsListBox.H - 0.8f - Layout.QsRow1.Y);
            var content = Scroll(box, B, viewR, Layout.QsRowCount * Layout.QsRowPitch, out var C, out _);
            RectTransform row1 = null, row2 = null, medal1 = null, title1 = null, bar1 = null, go1 = null;
            for (int i = 0; i < Layout.QsRowCount; i++)
            {
                float dy = i * Layout.QsRowPitch;
                var row = UiKit.Panel(content, "Quest:" + i, "fr.r12", Palette.Cream); UiKit.Pct(row.rectTransform, Sh(Layout.QsRow1, 0, dy).Within(C));
                var rr = row.rectTransform;
                var medal = UiKit.Rect(content, "Medal"); UiKit.Pct(medal, Sh(Layout.QsRowMedal, 0, dy).Within(C));
                var mi = UiKit.Icon(medal, "Icon", "ui.iconMedal"); UiKit.Stretch(mi.rectTransform);
                UiKit.Label(medal, -20, 100, 140, 60, "20", 24, Palette.Yellow);
                var title = UiKit.Label(content, 0, 0, 100, 100, QuestTitles[i], 30, Palette.InkSoft, TextAnchor.MiddleLeft); title.name = "Title"; UiKit.Pct(title.rectTransform, Sh(Layout.QsRowTitle, 0, dy).Within(C));
                var bar = UiKit.MakeBar(content, "ui.sliderGreen"); bar.Root.name = "Bar"; UiKit.Pct(bar.Root, Sh(Layout.QsRowBar, 0, dy).Within(C)); bar.Set(0, "0/" + QuestGoals[i]);
                var go = UiKit.Button(content, "ui.btnOrange", "이동", () => { }, Sh(Layout.QsRowGo, 0, dy).Within(C)); go.name = "GoBtn";
                if (i == 0) { row1 = rr; medal1 = medal; title1 = title.rectTransform; bar1 = bar.Root; go1 = go; } else if (i == 1) row2 = rr;
            }
            var tabs = new RectTransform[3]; string[] tabNames = { "일일", "주간", "업적" };
            for (int i = 0; i < 3; i++)
            {
                var t = tabs[i] = UiKit.Button(ov.Root, "ui.btnGray", tabNames[i], () => { }, Sh(Layout.QsTab, i * Layout.QsTabPitch, 0)); t.name = "Tab:" + i;
                if (i > 0) foreach (var im in t.GetComponentsInChildren<Image>(true)) im.color = Color.Lerp(im.color, Palette.Dim, 0.45f);   // 비활성 탭은 어둡게(첫 탭 «일일» 활성)
            }
            // 비평 이름표(표 ⑳)
            if (band != null) UiKit.Tag(band, "제목 띠"); UiKit.Tag(box, "팝업 박스"); UiKit.Tag(trackBox.transform, "점수 트랙 상자"); UiKit.Tag(refresh, "새로고침 줄"); UiKit.Tag(listBox.transform, "목록 상자");
            UiKit.Tag(row1, "퀘스트 줄 1"); UiKit.Tag(row2, "퀘스트 줄 2"); UiKit.Tag(medal1, "퀘스트 보상 메달(1줄)"); UiKit.Tag(title1, "퀘스트 제목(1줄)"); UiKit.Tag(bar1, "퀘스트 진행바(1줄)"); UiKit.Tag(go1, "이동 버튼(1줄)");
            UiKit.TagGroup(ov.Root, "탭 줄(3칸)", tabs); UiKit.Tag(tabs[0], "탭(1칸)"); TagClose(app);
        }

        // ───────────────────────── 16 출석 ─────────────────────────
        static readonly string[] AttendIcons = { "ui.coin", "ui.potionRed", "ui.gemRed", "ui.bookBlue", "ui.coin", "ui.hourglass" };
        static readonly string[] AttendColors = { "green", "blue", "plum", "green", "green", "plum" };

        /// <summary>출석 팝업(표 ㉑) — 노란 리본 «출석 보상»(박스 윗변에 걸침) → 3열×2행 칸(자주 머리 «N일차» + 보상 칸) + 7일차 넓은 칸(보상 2) → «탭하여 닫기». 받은 날 없음(시스템 없음 · ✅ 0).</summary>
        public static void Attendance(App app)
        {
            var ov = app.Overlay; var B = Layout.AtBox;
            var box = ov.OpenBox("ui.popup", "ui.title.yellow", "출석 보상", B, () => ov.Close()); box.name = "AttendanceBox";
            var rib = Ribbon(box, "ui.title.yellow", Layout.AtRibbon, B);
            var grid = UiKit.Rect(box, "Days"); UiKit.Stretch(grid);
            var cells = new RectTransform[6]; RectTransform head0 = null, icon0 = null;
            for (int i = 0; i < 6; i++)
            {
                float dx = (i % Layout.AtCols) * Layout.AtColPitch, dy = (i / Layout.AtCols) * Layout.AtRowPitch;
                var cell = UiKit.Panel(grid, "Day:" + (i + 1), "fr.r12", Palette.Cream); UiKit.Pct(cell.rectTransform, Sh(Layout.AtCell, dx, dy).Within(B));
                var head = Head(grid, B, Sh(Layout.AtCellHead, dx, dy), (i + 1) + "일차", Palette.A(Palette.Plum, 0.8f));
                var ic = Cell(grid, B, Sh(Layout.AtCellIcon, dx, dy), AttendColors[i], AttendIcons[i]);
                UiKit.Clickable(cell.rectTransform, () => { });
                cells[i] = cell.rectTransform; if (i == 0) { head0 = head.transform.parent as RectTransform; icon0 = ic; }
            }
            var day7 = UiKit.Panel(grid, "Day:7", "fr.r12", Palette.Cream); UiKit.Pct(day7.rectTransform, Layout.AtDay7.Within(B));
            var head7 = Head(grid, B, Layout.AtDay7Head, "7일차", Palette.A(Palette.Plum, 0.8f), "Head7");
            var r7 = new RectTransform[2];
            r7[0] = Cell(grid, B, Layout.AtDay7Cell, "green", "ui.coin"); r7[1] = Cell(grid, B, Sh(Layout.AtDay7Cell, Layout.AtDay7Pitch, 0), "plum", "ui.gemRed");
            UiKit.Clickable(day7.rectTransform, () => { });
            // 비평 이름표(표 ㉑)
            if (rib != null) UiKit.Tag(rib, "제목 리본"); UiKit.Tag(box, "팝업 박스"); UiKit.TagGroup(grid, "출석 격자(6칸)", cells); UiKit.Tag(cells[0], "출석 칸(1칸)"); UiKit.Tag(head0, "칸 머리(1칸)"); UiKit.Tag(icon0, "칸 보상 아이콘(1칸)");
            UiKit.Tag(day7.transform, "7일 칸"); UiKit.Tag(head7.transform.parent, "7일 칸 머리"); UiKit.TagGroup(grid, "7일 보상 줄(2칸)", r7); TagClose(app);
        }

        // ───────────────────────── 17 데일리 기프트 ─────────────────────────
        static readonly int[] GiftAds = { 1, 2, 3, 6 };
        static readonly string[] GiftIcons = { "ui.bookBlue", "ui.hourglass", "ui.potionRed", "ui.gemRed" };

        /// <summary>데일리 기프트 팝업(표 ㉒) — 리본 위 선물 그림 → 노란 리본 «데일리 기프트» → 노란 테두리 박스: ⏱ 종료 줄 · «오늘의 선물» 칸(보상 + 받기) · «광고 N회 보기» 줄 4(진행바 · 보상 · 광고 버튼) · 왼쪽 노란 타임라인(선 + 점 4) → «탭하여 닫기».</summary>
        public static void DailyGift(App app)
        {
            var ov = app.Overlay; var B = Layout.GfBox;
            var box = ov.OpenBox("ui.popup.yellow", "ui.title.yellow", "데일리 기프트", B, () => ov.Close()); box.name = "DailyGiftBox";
            var rib = Ribbon(box, "ui.title.yellow", Layout.GfRibbon, B);
            var pic = UiKit.Icon(ov.Root, "GiftPic", "ui.gift"); UiKit.Pct(pic.rectTransform, Layout.GfPic); pic.transform.SetSiblingIndex(1);   // 어둠 위 · 상자 아래
            var timer = TimerRow(box, B, Layout.GfTimer, "종료까지 " + Dashes);
            var today = UiKit.Panel(box, "Today", "fr.r12", Palette.A(Palette.Sky, 0.55f)); UiKit.Pct(today.rectTransform, Layout.GfTodayCell.Within(B));
            {
                var T = Layout.GfTodayCell;
                var th = Head(box, B, new Layout.R(T.X, T.Y, T.W, 2.4f), "오늘의 선물", Palette.A(Palette.Dim, 0.5f), "TodayHead");
                th.alignment = TextAnchor.MiddleLeft; UiKit.Pct(th.rectTransform, 8, 0, 90, 100);
                var gi = UiKit.Icon(th.transform.parent, "Icon", "pi.gift", Palette.Yellow); UiKit.Pct(gi.rectTransform, 1.5f, 10, 5, 80);
                Cell(box, B, new Layout.R(T.X + 1.6f, T.Y + 3.0f, 8.2f, 4.5f), "plum", "ui.gemRed");
                var get = UiKit.Button(box, "ui.btnSmallOrange", "받기", () => { }, new Layout.R(T.X + T.W - 13.5f, T.Y + 3.2f, 12.0f, 4.0f).Within(B)); get.name = "TodayGetBtn";
            }
            RectTransform row1 = null, row2 = null, title1 = null, bar1 = null, reward1 = null, check1 = null;
            for (int i = 0; i < Layout.GfRowCount; i++)
            {
                float dy = i * Layout.GfRowPitch;
                var row = UiKit.Panel(box, "Ad:" + i, "fr.r12", Palette.A(Palette.Brown, 0.75f)); UiKit.Pct(row.rectTransform, Sh(Layout.GfRow1, 0, dy).Within(B));
                var title = UiKit.Label(box, 0, 0, 100, 100, $"광고 {GiftAds[i]}회 보기", 30, Palette.White, TextAnchor.MiddleLeft); title.name = "Title"; UiKit.Pct(title.rectTransform, Sh(Layout.GfRowTitle, 0, dy).Within(B));
                var bar = UiKit.MakeBar(box, "ui.sliderBlue"); bar.Root.name = "Bar"; UiKit.Pct(bar.Root, Sh(Layout.GfRowBar, 0, dy).Within(B)); bar.Set(0, "0/" + GiftAds[i]);
                var reward = Cell(box, B, Sh(Layout.GfRowReward, 0, dy), i % 2 == 0 ? "green" : "plum", GiftIcons[i]);
                var ad = UiKit.Button(box, "ui.btnSmallBlue", "광고", () => { }, Sh(Layout.GfRowCheck, 0, dy).Within(B)); ad.name = "AdBtn";
                if (i == 0) { row1 = row.rectTransform; title1 = title.rectTransform; bar1 = bar.Root; reward1 = reward; check1 = ad; } else if (i == 1) row2 = row.rectTransform;
            }
            var line = UiKit.Panel(box, "Timeline", "fr.rect", Palette.Yellow); UiKit.Pct(line.rectTransform, Layout.GfTimeline.Within(B));
            RectTransform dot0 = null;
            for (int i = 0; i < Layout.GfRowCount; i++)
            {
                var dot = UiKit.Icon(box, "Dot:" + i, "fr.circle", Palette.Yellow); UiKit.Pct(dot.rectTransform, Sh(Layout.GfTimelineDot, 0, i * Layout.GfRowPitch).Within(B));
                if (i == 0) dot0 = dot.rectTransform;
            }
            // 비평 이름표(표 ㉒)
            UiKit.Tag(pic.transform, "선물 그림"); if (rib != null) UiKit.Tag(rib, "제목 리본"); UiKit.Tag(box, "팝업 박스"); UiKit.Tag(timer, "종료 시각 줄"); UiKit.Tag(today.transform, "오늘의 선물 칸");
            UiKit.Tag(row1, "광고 줄 1"); UiKit.Tag(row2, "광고 줄 2"); UiKit.Tag(title1, "광고 줄 제목(1줄)"); UiKit.Tag(bar1, "광고 줄 진행바(1줄)"); UiKit.Tag(reward1, "광고 줄 보상 아이콘(1줄)"); UiKit.Tag(check1, "광고 줄 체크(1줄)");
            UiKit.Tag(line.transform, "타임라인 선"); UiKit.Tag(dot0, "타임라인 점(1개)"); TagClose(app);
        }

        // ───────────────────────── 18 7일 챌린지 ─────────────────────────
        static readonly string[] ChallengeTitles = { "챕터 30 클리어", "상자 15개 열기", "적 1000마리 처치", "전투력 8000 달성" };
        static readonly int[] ChallengeGoals = { 30, 15, 1000, 8000 };
        static readonly string[] ChallengeNums = { "0", "40", "150", "270", "390", "500" };

        /// <summary>7일 챌린지 팝업(표 ㉓) — 빨간 리본 «7일 챌린지» → 긴 박스: ⏱ 종료 줄 · 배너 그림(들판·길·나무·트로피 + (i)) · 점수 트랙(빨간 줄) · 왼쪽 «N일차» 탭 7(1일차 활성) + 오른쪽 과제 목록(줄 4 · 스크롤 · 제목·(0/N)·보상 2·«이동») → «탭하여 닫기».</summary>
        public static void Challenge7(App app)
        {
            var ov = app.Overlay; var B = Layout.C7Box;
            var box = ov.OpenBox("ui.popup", "ui.title.red", "7일 챌린지", B, () => ov.Close()); box.name = "Challenge7Box";
            var rib = Ribbon(box, "ui.title.red", Layout.C7Ribbon, B);
            var timer = TimerRow(box, B, Layout.C7Timer, "종료까지 " + Dashes);
            var banner = UiKit.Rect(box, "Banner"); UiKit.Pct(banner, Layout.C7Banner.Within(B)); UiKit.Ensure<RectMask2D>(banner.gameObject);
            {
                var field = UiKit.Icon(banner, "Field", "env.field"); field.preserveAspect = false; UiKit.Stretch(field.rectTransform);
                var road = UiKit.Icon(banner, "Road", "env.road"); road.preserveAspect = false; UiKit.Pct(road.rectTransform, 0, 62, 100, 22);
                var t1 = UiKit.Icon(banner, "Tree1", "env.tree"); UiKit.Pct(t1.rectTransform, 2, 20, 22, 50);
                var t2 = UiKit.Icon(banner, "Tree2", "env.tree"); UiKit.Pct(t2.rectTransform, 74, 30, 20, 46);
                var tr = UiKit.Icon(banner, "Trophy", "ui.trophy"); UiKit.Pct(tr.rectTransform, 60, 6, 18, 40);
            }
            var info = UiKit.Spawn("ui.btnInfo", box); var irt = (RectTransform)info.transform; irt.name = "InfoBtn"; UiKit.Pct(irt, Layout.C7Info.Within(B)); UiKit.Clickable(irt, () => { });
            Track(box, B, Layout.C7TrackIcon, Layout.C7TrackPitch, Layout.C7TrackCount, Layout.C7TrackNums, Palette.Red, TrackIcons, ChallengeNums, "트랙 아이콘 줄(6칸)", "트랙 아이콘(1칸)");
            var tabs = new RectTransform[Layout.C7DayCount];
            for (int i = 0; i < Layout.C7DayCount; i++)
            {
                var t = tabs[i] = UiKit.Button(box, i == 0 ? "ui.btnRed" : "ui.btnGray", (i + 1) + "일차", () => { }, Sh(Layout.C7DayTab, 0, i * Layout.C7DayPitch).Within(B)); t.name = "DayTab:" + (i + 1);
            }
            var listBox = UiKit.Panel(box, "ListBox", "fr.r12", Palette.A(Palette.Dim, 0.55f)); UiKit.Pct(listBox.rectTransform, Layout.C7ListBox.Within(B));
            var viewR = new Layout.R(Layout.C7Row1.X, Layout.C7Row1.Y, Layout.C7Row1.W, Layout.C7ListBox.Y + Layout.C7ListBox.H - 0.6f - Layout.C7Row1.Y);
            var content = Scroll(box, B, viewR, Layout.C7RowCount * Layout.C7RowPitch, out var C, out _);
            RectTransform row1 = null, row2 = null, title1 = null, go1 = null; var rewards1 = new RectTransform[2];
            for (int i = 0; i < Layout.C7RowCount; i++)
            {
                float dy = i * Layout.C7RowPitch;
                var row = UiKit.Panel(content, "Task:" + i, "fr.r12", Palette.Cream); UiKit.Pct(row.rectTransform, Sh(Layout.C7Row1, 0, dy).Within(C));
                var title = UiKit.Label(content, 0, 0, 100, 100, ChallengeTitles[i], 28, Palette.InkSoft, TextAnchor.MiddleLeft); title.name = "Title"; UiKit.Pct(title.rectTransform, Sh(Layout.C7RowTitle, 0, dy).Within(C));
                var prog = UiKit.Label(content, 0, 0, 100, 100, $"(0/{ChallengeGoals[i]})", 24, Palette.InkLight, TextAnchor.MiddleRight); prog.name = "Progress";
                UiKit.Pct(prog.rectTransform, new Layout.R(Layout.C7RowGo.X, Layout.C7RowTitle.Y + dy, Layout.C7RowGo.W, Layout.C7RowTitle.H).Within(C));
                var c1 = Cell(content, C, Sh(Layout.C7RowReward, 0, dy), "blue", "ui.coin"); var c2 = Cell(content, C, Sh(Layout.C7RowReward, Layout.C7RowRewardPitch, dy), "plum", "ui.gemRed");
                var go = UiKit.Button(content, "ui.btnOrange", "이동", () => { }, Sh(Layout.C7RowGo, 0, dy).Within(C)); go.name = "GoBtn";
                if (i == 0) { row1 = row.rectTransform; title1 = title.rectTransform; go1 = go; rewards1[0] = c1; rewards1[1] = c2; } else if (i == 1) row2 = row.rectTransform;
            }
            // 비평 이름표(표 ㉓)
            if (rib != null) UiKit.Tag(rib, "제목 리본"); UiKit.Tag(box, "팝업 박스"); UiKit.Tag(timer, "종료 시각 줄"); UiKit.Tag(banner, "배너 그림"); UiKit.Tag(irt, "정보 버튼");
            UiKit.TagGroup(box, "일차 탭 열(7칸)", tabs); UiKit.Tag(tabs[0], "일차 탭(1칸)"); UiKit.Tag(listBox.transform, "과제 목록 상자");
            UiKit.Tag(row1, "과제 줄 1"); UiKit.Tag(row2, "과제 줄 2"); UiKit.Tag(title1, "과제 제목(1줄)"); UiKit.TagGroup(content, "과제 보상 줄(1줄)", rewards1); UiKit.Tag(go1, "이동 버튼(1줄)"); TagClose(app);
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

        static Layout.R Sh(Layout.R r, float dx, float dy) => new Layout.R(r.X + dx, r.Y + dy, r.W, r.H);

        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Color.Lerp(Palette.Slate, Palette.Dim, 0.6f); bg.raycastTarget = true;
            _top = TopBar.Build(App, Root);
            // 제목(⭐ 특권) + 밑줄 + 부제
            var title = UiKit.Rect(Root, "Title"); UiKit.Pct(title, Layout.PrTitle);
            var star = UiKit.Icon(title, "Icon", "pi.star", Palette.Yellow); UiKit.Pct(star.rectTransform, 0, 0, 22, 100);
            var tt = UiKit.Label(title, 25, -10, 75, 120, "특권", 52, Palette.White, TextAnchor.MiddleLeft); tt.fontStyle = FontStyle.Bold;
            var line = UiKit.Icon(Root, "Underline", "fr.lineDeco", Palette.A(Palette.White, 0.45f)); line.preserveAspect = false; UiKit.Pct(line.rectTransform, Layout.PrUnderline);
            var sub = UiKit.Label(Root, 0, 0, 100, 100, "특권을 활성화하고 놀라운 보상을 받으세요!", 28, Palette.CreamDark); sub.name = "Sub"; UiKit.Pct(sub.rectTransform, Layout.PrSub);
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
            var head1 = UiKit.Panel(content, "Head:1", "fr.r12", Palette.A(Palette.Blue, 0.9f)); UiKit.Pct(head1.rectTransform, new Layout.R(Layout.PrCard1.X, Layout.PrCard1.Y, Layout.PrCard1.W, 3.6f).Within(C));
            CardHead(head1.transform, "ui.iconGiftRed", "일일 선물", "초기화까지 " + LobbyPopups.Dashes);
            var reward1 = LobbyPopups.Cell(content, C, Layout.PrCard1Reward, "plum", "ui.gemRed");
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
                var head = UiKit.Panel(content, "Head:" + (k + 2), "fr.r12", Palette.A(Palette.Dim, 0.35f)); UiKit.Pct(head.rectTransform, Sh(Layout.PrCardTitle, 0, dy).Within(C));
                CardHead(head.transform, L.icon, L.name, "비활성");
                var desc = UiKit.Panel(content, "Desc:" + (k + 2), "fr.r12", Palette.A(Palette.Dim, 0.35f)); UiKit.Pct(desc.rectTransform, Sh(Layout.PrCardDesc, 0, dy).Within(C));
                float lh = 100f / Mathf.Max(2, L.lines.Length);
                for (int i = 0; i < L.lines.Length; i++)
                {
                    float ly = 4 + i * lh * 0.92f;
                    var bullet = UiKit.Icon(desc.transform, "Bullet", "pi.star", Palette.Yellow); UiKit.Pct(bullet.rectTransform, 4, ly + lh * 0.2f, 6, lh * 0.5f);   // 글머리 = 작은 노란 별(레퍼런스 금색 마름모 자리 · 글자 아님)
                    UiKit.Label(desc.transform, 12, ly, 86, lh * 0.9f, L.lines[i], 26, Palette.White, TextAnchor.MiddleLeft);
                }
                var pic = UiKit.Icon(content, "Pic:" + (k + 2), L.pic); UiKit.Pct(pic.rectTransform, Sh(Layout.PrCardPic, 0, dy).Within(C));
                var daily = UiKit.Label(content, 0, 0, 100, 100, "매일 수령", 30, Palette.Yellow, TextAnchor.MiddleLeft); daily.name = "Daily"; daily.fontStyle = FontStyle.Bold;
                UiKit.Pct(daily.rectTransform, new Layout.R(8.6f, Layout.PrCardReward.Y + dy, 26.0f, Layout.PrCardReward.H).Within(C));
                var reward = LobbyPopups.Cell(content, C, Sh(Layout.PrCardReward, 0, dy), "plum", "ui.gemRed");
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
        }

        /// <summary>카드 제목 띠 안 — 왼쪽 아이콘 + 이름 · 오른쪽 상태/타이머 글자.</summary>
        static void CardHead(Transform head, string icon, string name, string right)
        {
            var ic = UiKit.Icon(head, "Icon", icon); UiKit.Pct(ic.rectTransform, 2, 10, 8, 80);
            var t = UiKit.Label(head, 11, 0, 50, 100, name, 36, Palette.White, TextAnchor.MiddleLeft); t.fontStyle = FontStyle.Bold;
            UiKit.Label(head, 62, 0, 36, 100, right, 26, Palette.White, TextAnchor.MiddleRight);
        }

        public override void Refresh() { _top?.Refresh(); }
    }

    /// <summary>
    /// 시즌 패스 페이지 = <c>docs/ref/19_pass.jpg</c> 구도(T44 · 표 ㉔ · 껍데기): 상단 재화 바 → 시즌 배너(들판 그림 · 제목 · 남은 기간 · 진행바 + 메달 · 레벨 배지 · 안내 띠) → 갈색 띠 → 3열 세로 트랙(무료 파랑 · 유료1 주황 · 유료2 보라 · 가운데 노란 레벨 줄 + 육각 뱃지 · 칸은 자물쇠 · 스크롤) → 트랙 위 «현재 레벨» pill → 바닥 버튼 3(전체 받기 · 패스 2) → 뒤로 ◀ + 오른쪽 아래 배너 탭. 탭 바 없음.
    /// 전부 표시만(패스 시스템 없음 · 레벨 1 · 진행 «준비 중» · 가격 데이터 없음 → «준비 중»). 이름 계약(스모크): <c>BackBtn</c> · <c>ClaimAllBtn</c> · <c>Buy1Btn/Buy2Btn</c> · <c>Row:N</c>.
    /// </summary>
    public sealed class PassScreen : GameScreen
    {
        public override string Name => "pass";
        TopBar _top;
        static Layout.R Sh(Layout.R r, float dx, float dy) => new Layout.R(r.X + dx, r.Y + dy, r.W, r.H);
        static readonly string[] FreeIcons = { "ui.coin", "ui.bookBlue", "ui.coin", "ui.potionRed", "ui.coin", "ui.bookBlue", "ui.coin", "ui.hourglass" };
        static readonly string[] PaidIcons = { "ui.bookBlue", "ui.gemRed", "ui.bookBlue", "pi.magic", "ui.bookBlue", "ui.gemRed", "ui.bookBlue", "pi.magic" };

        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Color.Lerp(Palette.Slate, Palette.Dim, 0.6f); bg.raycastTarget = true;
            _top = TopBar.Build(App, Root);
            // 시즌 배너 — Environment 들판 + 나무(성·무지개 조각은 팩에 없다) · 제목 · 남은 기간 · 진행바(메달) · 레벨 배지 · 안내 띠
            var banner = UiKit.Rect(Root, "Banner"); UiKit.Pct(banner, Layout.PsBanner); UiKit.Ensure<RectMask2D>(banner.gameObject);
            {
                var field = UiKit.Icon(banner, "Field", "env.field"); field.preserveAspect = false; UiKit.Stretch(field.rectTransform);
                var road = UiKit.Icon(banner, "Road", "env.road"); road.preserveAspect = false; UiKit.Pct(road.rectTransform, 55, 55, 45, 30);
                var t1 = UiKit.Icon(banner, "Tree1", "env.tree"); UiKit.Pct(t1.rectTransform, 0, 30, 18, 60);
                var t2 = UiKit.Icon(banner, "Tree2", "env.tree"); UiKit.Pct(t2.rectTransform, 14, 45, 14, 45);
                var t3 = UiKit.Icon(banner, "Tree3", "env.tree"); UiKit.Pct(t3.rectTransform, 84, 20, 16, 55);
            }
            var title = UiKit.Label(Root, 0, 0, 100, 100, "시즌 패스", 52, Palette.White); title.name = "SeasonTitle"; title.fontStyle = FontStyle.Bold; UiKit.Pct(title.rectTransform, Layout.PsTitle);
            var remain = UiKit.Label(Root, 0, 0, 100, 100, "시즌 종료까지 " + LobbyPopups.Dashes, 28, Palette.White); remain.name = "Remain"; UiKit.Pct(remain.rectTransform, Layout.PsRemain);
            var bar = UiKit.MakeBar(Root, "ui.sliderGreen"); bar.Root.name = "SeasonBar"; UiKit.Pct(bar.Root, Layout.PsBar); bar.Set(0, "준비 중");
            var medal = UiKit.Icon(Root, "BarIcon", "ui.iconMedal"); UiKit.Pct(medal.rectTransform, Layout.PsBarIcon);
            var badge = UiKit.Panel(Root, "LevelBadge", "fr.circle", Palette.Ink); UiKit.Pct(badge.rectTransform, Layout.PsLevelBadge);
            UiKit.Label(badge.transform, 0, 0, 100, 100, "1", 34, Palette.White);
            var hint = UiKit.Panel(Root, "Hint", "fr.rect", Palette.A(Palette.Dim, 0.75f)); UiKit.Pct(hint.rectTransform, Layout.PsHint);
            UiKit.Label(hint.transform, 4, 0, 92, 100, "일일 퀘스트를 완료해 보상을 해금하세요!", 28, Palette.White);
            var brown = UiKit.Panel(Root, "BrownBand", "fr.rect", Palette.A(Palette.InkSoft, 0.9f)); UiKit.Pct(brown.rectTransform, Layout.PsBrownBand);
            // 트랙 — 열 배경 3 + 노란 레벨 줄은 창에 고정 · 칸·뱃지만 스크롤
            var view = UiKit.Rect(Root, "Track"); UiKit.Pct(view, Layout.PsTrack); UiKit.Ensure<RectMask2D>(view.gameObject);
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            var T = Layout.PsTrack;
            var colF = UiKit.Panel(view, "ColFree", "fr.rect", Palette.A(Palette.Blue, 0.9f)); UiKit.Pct(colF.rectTransform, Layout.PsColFree.Within(T));
            var colP1 = UiKit.Panel(view, "ColPaid1", "fr.rect", Palette.A(Palette.Orange, 0.9f)); UiKit.Pct(colP1.rectTransform, Layout.PsColPaid1.Within(T));
            var colP2 = UiKit.Panel(view, "ColPaid2", "fr.rect", Palette.A(Palette.Plum, 0.9f)); UiKit.Pct(colP2.rectTransform, Layout.PsColPaid2.Within(T));
            var line = UiKit.Panel(view, "LevelLine", "fr.rect", Palette.Yellow); UiKit.Pct(line.rectTransform, Layout.PsLevelLine.Within(T));
            var sr = view.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40;
            var content = UiKit.Rect(view, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            float contentH = Layout.PsRowCount * Layout.PsRowPitch + (Layout.PsRow1.Y - T.Y);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero; content.sizeDelta = new Vector2(0, contentH / 100f * UiKit.FrameH);
            sr.content = content; sr.viewport = view;
            var C = new Layout.R(T.X, T.Y, T.W, contentH);
            RectTransform badge1 = null, free1 = null, paid1 = null; var row1 = new RectTransform[3];
            for (int i = 0; i < Layout.PsRowCount; i++)
            {
                float dy = i * Layout.PsRowPitch;
                var row = UiKit.Rect(content, "Row:" + (i + 1)); UiKit.Stretch(row);
                var b = UiKit.Panel(row, "Badge", "fr.circle", Palette.Yellow); UiKit.Pct(b.rectTransform, Sh(Layout.PsLevelBadgeRow, 0, dy).Within(C));
                var bt = UiKit.Label(b.transform, 0, 0, 100, 100, (i + 1).ToString(), 30, Palette.Ink); bt.fontStyle = FontStyle.Bold;
                var f = LobbyPopups.Cell(row, C, Sh(Layout.PsCellFree, 0, dy), "gray", FreeIcons[i % FreeIcons.Length], null, i > 0, "Free");
                var p1 = LobbyPopups.Cell(row, C, Sh(Layout.PsCellFree, Layout.PsColPitch, dy), "yellow", PaidIcons[i % PaidIcons.Length], null, true, "Paid1");
                var p2 = LobbyPopups.Cell(row, C, Sh(Layout.PsCellFree, 2 * Layout.PsColPitch, dy), "plum", PaidIcons[i % PaidIcons.Length], null, true, "Paid2");
                if (i == 0) { badge1 = b.rectTransform; free1 = f; paid1 = p1; row1[0] = f; row1[1] = p1; row1[2] = p2; }
            }
            // «현재 레벨» 구분선 + pill(💎 준비 중) — 표 자리 그대로(레벨 데이터 없음)
            var sep = UiKit.Panel(content, "CurLine", "fr.rect", Palette.Yellow); UiKit.Pct(sep.rectTransform, new Layout.R(T.X, Layout.PsCurPill.Y + Layout.PsCurPill.H * 0.55f, T.W, 0.6f).Within(C));
            var pill = UiKit.Panel(content, "CurPill", "fr.r12", Palette.Yellow); UiKit.Pct(pill.rectTransform, Layout.PsCurPill.Within(C));
            var pg = UiKit.Icon(pill.transform, "Gem", "ui.gemRed"); UiKit.Pct(pg.rectTransform, 6, 12, 22, 76);
            UiKit.Label(pill.transform, 30, 0, 66, 100, "준비 중", 30, Palette.Ink, TextAnchor.MiddleLeft, true, false);
            // 바닥 버튼 3(트랙 위에 겹친다) · 하단 띠 · 뒤로 · 배너 탭
            var claim = UiKit.Button(Root, "ui.btnGray", "전체 받기", () => { }, Layout.PsClaimAll); claim.name = "ClaimAllBtn";
            var buy1 = UiKit.Button(Root, "ui.btnOrange", "패스 · 준비 중", () => { }, Layout.PsBuy1); buy1.name = "Buy1Btn";
            var buy2 = UiKit.Button(Root, "ui.btnPlum", "패스+ · 준비 중", () => { }, Layout.PsBuy2); buy2.name = "Buy2Btn";
            var foot = UiKit.Panel(Root, "FootBand", "fr.rect", Palette.A(Palette.Dim, 0.9f)); UiKit.Pct(foot.rectTransform, Layout.PsFootBand);
            var back = UiKit.Button(Root, "ui.btnGray", "", () => App.ShowScreen("lobby"), Layout.PsBack); back.name = "BackBtn";
            var bi = UiKit.Icon(back, "Icon", "pi.arrow_left", Palette.Ink); UiKit.Pct(bi.rectTransform, 30, 18, 40, 64);
            var tab = UiKit.Panel(Root, "BannerTab", "fr.r12", Palette.A(Palette.InkSoft, 0.9f)); UiKit.Pct(tab.rectTransform, Layout.PsBannerTab);
            var tabIc = UiKit.Icon(tab.transform, "Icon", "ui.iconMedal"); UiKit.Pct(tabIc.rectTransform, 22, 4, 56, 56);
            UiKit.Label(tab.transform, 2, 60, 96, 38, "시즌 패스", 24, Palette.White, TextAnchor.UpperCenter);
            UiKit.Clickable(tab.rectTransform, () => { });
            // 비평 이름표(표 ㉔)
            UiKit.Tag(_top.Root, "상단 바"); UiKit.Tag(banner, "시즌 배너"); UiKit.Tag(title.transform, "시즌 제목"); UiKit.Tag(remain.transform, "남은 기간 줄"); UiKit.Tag(bar.Root, "시즌 진행바");
            UiKit.Tag(badge.transform, "배너 레벨 배지"); UiKit.Tag(hint.transform, "안내 줄"); UiKit.Tag(brown.transform, "갈색 띠 (참고·컨테이너)"); UiKit.Tag(view, "트랙 영역(3열)"); UiKit.Tag(line.transform, "레벨 줄");
            UiKit.Tag(badge1, "레벨 뱃지(1개)"); UiKit.TagGroup(content, "트랙 줄 1(3칸)", row1); UiKit.Tag(free1, "트랙 칸(무료 1칸)"); UiKit.Tag(paid1, "트랙 칸(유료 1칸)"); UiKit.Tag(pill.transform, "현재 레벨 pill");
            UiKit.Tag(claim, "전체 받기 버튼"); UiKit.Tag(buy1, "패스 구매 버튼 1"); UiKit.Tag(buy2, "패스 구매 버튼 2"); UiKit.Tag(back, "뒤로 버튼"); UiKit.Tag(tab.transform, "배너 탭"); UiKit.Tag(foot.transform, "하단 띠 (참고·컨테이너)");
        }

        public override void Refresh() { _top?.Refresh(); }
    }
}
