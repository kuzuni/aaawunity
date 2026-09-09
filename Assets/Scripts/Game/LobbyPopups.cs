using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using TMPro;
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
            var t = rr.GetComponentInChildren<TMP_Text>(true); if (t != null) { t.fontSize = TextSize.Title; t.enableAutoSizing = true; t.fontSizeMin = TextSize.BestFitMin; t.fontSizeMax = TextSize.Title; TextAudit.Mark(t, TextKind.Title); RibbonTextFit(t); UiKit.EnsureOutline(t); }
            return rr;
        }

        // T221 — 여기 있던 `RibbonOutlineRatio`(= UiKit.OutlineRatio 별칭)를 걷었다.
        //  T186 ⓒ 는 «이 리본만 테를 두껍게» 를 시도했다가 되돌린 자리이고(효과 0.006 · 결정 483),
        //  T194 가 그 뒤 공통 규칙을 올렸다. 그런데 T207 ② 가 테를 SDF 머티리얼로 옮기면서
        //  그 규칙 자체가 아무것도 안 그리게 됐다 — 두께는 이제 TmpFont.OutlineWidth 하나뿐이다(T221 · 결정 595).
        //  이 리본이 «공통 규격 그대로» 라는 결론은 그대로 살아 있다(DailyGiftLookTests 가 그것을 잰다).

        /// <summary>리본 조각(Title_01)의 글자 rect 는 3.9% 리본에서 56px 인데 제목 60 의 한 줄 선호 높이가 58px 라 위아래 1px 씩 넘쳤다(CI #106 게이트 «출석 보상»·«데일리 기프트» 잘림) → 글자 rect 만 세로로 늘린다(리본 크기·자리 불변 · 글자는 가운데 정렬 그대로).</summary>
        public static void RibbonTextFit(TMP_Text t)
        {
            // T75 4항 게이트가 «제목 60 한 줄(BoxHeight = 60×1.4 = 84px)» 을 요구한다 — 예전 1.2배(72px)로는 bestFit 이 말없이 줄인다(CI #210 «[17_daily_gift] rect 769×72»).
            float need = TextSize.BoxHeight(TextSize.Title);
            var tr = t.rectTransform; float h = tr.rect.height;
            if (h > 0f && h < need) tr.sizeDelta = new Vector2(tr.sizeDelta.x, tr.sizeDelta.y + (need - h));
        }

        /// <summary>보상 칸 — ItemFrame_01 조각(본래 190px · 배율로 표 칸에) + 등급색 변형 + 아이콘 (+ 오른쪽 아래 수량 · 오른쪽 위 자물쇠). PetScreen 의 칸과 같은 문법.</summary>
        /// <summary>T133 ⓐ — «칸 아래를 가로지르는» 수량 띠의 높이(칸 %). 글자 크기는 이 높이에서 계산한다.</summary>
        /// <summary>
        /// T133 ⓐ — 출석 칸 수량 글자의 칸(칸 크기의 %). 레퍼런스 `16_attendance.jpg` 실측:
        /// 수량은 아이콘 **오른쪽 아래**에 굵게 얹혀 프레임 밖으로 조금 걸치고, 글자 잉크가 아이콘 높이의 ≈25% 다.
        /// bestFit 이 rect 안에서 글자를 다시 누르므로 **rect 높이가 곧 글자 크기의 상한**이다 — 하한(T63)이 뜻을 가지려면 rect 가 그만큼 커야 한다.
        /// <para><see cref="QtyOver"/> = 프레임 밖으로 걸치는 양(레퍼런스도 숫자가 칸 모서리를 조금 넘는다).</para>
        /// </summary>
        public const float QtyW = 82f, QtyH = 50f, QtyOver = 4f;
        /// <summary>수량 글자가 «너무 작아 그림에 먹히지» 않는 하한 — 칸 높이 대비 비율(게이트가 이 값으로 잰다 · 회차 1 은 30% 라 25px 로 눌렸다).</summary>
        public const float QtyMinHeightPct = 40f;
        /// <summary>
        /// T133 ⓑ — 출석 칸 머리(«N일차») 띠 색을 누르는 비율. 레퍼런스 16 의 머리 띠는 <b>짙은 자주(휘도 ≈0.2)</b> 인데
        /// 우리 것은 <c>Palette.Plum</c> α0.8 이라 밝은 칸 위에서 #D493E1(휘도 0.69)로 떠 흰 글자와 대비가 0.31 밖에 안 됐다.
        /// <b>Ink 로 섞지 않고 밝기만 누른다</b> — 섞으면 회색이 되어 «자주» 가 사라진다(0.36 배 = 휘도 ≈0.22 · 대비 ≈0.78).
        /// </summary>
        public const float HeadBandDark = 0.36f;
        /// <summary>레퍼런스 16 머리 띠 색 — <see cref="HeadBandDark"/> 로 누른 <c>Palette.Plum</c>(불투명).</summary>
        public static Color HeadBand { get { var c = Palette.Plum; return new Color(c.r * HeadBandDark, c.g * HeadBandDark, c.b * HeadBandDark, 1f); } }

        public static RectTransform Cell(Transform parent, Layout.R parentR, Layout.R cellR, string frameColor, string iconKey, string qty = null, bool locked = false, string name = "Cell", bool qtyBand = false)
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
            if (!string.IsNullOrEmpty(qty))
            {
                TMP_Text q;
                if (qtyBand)
                {
                    // T133 ⓐ 회차 2 — 회차 1(가운데 «띠»)은 실제 화면에서 **안 됐다**(`screens` run 283 의 16_attendance.png 확대 · 결정 403):
                    // 아이콘 밑단에 걸린 가느다란 흰 «1» 한 획이라 그림에 먹혔다. 두 가지가 틀렸었다 —
                    //  ⓘ **자리**: 등재 메모의 «칸 아래를 가로지르는 큰 글자» 는 레퍼런스를 잘못 읽은 것이다.
                    //     `docs/ref/16_attendance.jpg` 를 다시 보면 수량(«5000»·«10K»·«1»)은 **아이콘 오른쪽 아래**에
                    //     굵게 얹혀 프레임 밖으로 살짝 걸친다 — 원래 코드의 LowerRight 가 맞았다.
                    //  ⓙ **크기**: `FontForHeight` 는 «프레임 높이의 %» 를 받는데 «칸 높이의 %» 를 넘겨 21px 이 나왔고,
                    //     30% 짜리 rect(≈25px)가 bestFit 으로 글자를 다시 눌러 하한 40 이 아무 뜻이 없었다.
                    // 그래서 rect 를 «칸 높이의 절반» 으로 키우고 자리를 레퍼런스대로 오른쪽 아래(살짝 걸침)로 되돌린다.
                    // 읽히게 하는 것은 띠가 아니라 **검은 아웃라인**이다(T63 0항이 모든 글자에 무조건 붙인다 · 레퍼런스도 같은 방식).
                    q = UiKit.Label(cell, 100f - QtyW + QtyOver, 100f - QtyH + QtyOver, QtyW, QtyH, qty,
                                    UiKit.FontForHeight(cellR.H * QtyH / 100f), Palette.White, TextAnchor.LowerRight, kind: TextKind.Body);
                }
                else q = UiKit.Label(cell, 20, 44, 76, 56, qty, TextSize.Aux, Palette.White, TextAnchor.LowerRight, kind: TextKind.Aux);
                q.name = "Qty"; q.fontStyle = FontStyles.Bold;
            }
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

        /// <summary>세로 스크롤 창 — <paramref name="viewR"/>(프레임 %) 안에 content(위 앵커 · 높이 <paramref name="contentH"/>%). 자식은 돌려주는 <paramref name="contentR"/> 기준 <c>Within</c> 으로 놓는다.
        /// <para>팝업 안 세로 스크롤의 <b>문법 한 곳</b>이다 — T237 이 25 팝업(순위 보상 16줄)에서 그대로 쓴다(새 꼴을 만들지 않는다).</para></summary>
        public static RectTransform Scroll(Transform parent, Layout.R parentR, Layout.R viewR, float contentH, out Layout.R contentR, out ScrollRect sr)
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
        static TMP_Text Head(Transform parent, Layout.R parentR, Layout.R r, string text, Color band, string name = "Head")
        {
            var p = UiKit.Panel(parent, name, "fr.r12", band); UiKit.Pct(p.rectTransform, r.Within(parentR));
            return UiKit.Label(p.transform, 4, 0, 92, 100, text, TextSize.Body, Palette.White);
        }
        static void TagClose(App app)
        {
            var tc = UiKit.Find(app.Overlay.Root, "TapToClose"); if (tc != null) UiKit.Tag(tc, "닫기 안내");
        }

        // ───────────────────────── 15 퀘스트 ─────────────────────────
        // ⚠ T257 — 아래 넷은 이제 **표가 없을 때의 대비**다(로드 실패 · `GameData.Quest == null`). 실물은 `quest.json` 이 정한다.
        //   지우지 않는 까닭: 표를 못 읽는 판에서 화면이 빈 상자로 뜨는 것보다 종전 껍데기가 낫고, 그 갈래를 코드가 명시하는 편이 읽기 쉽다.
        static readonly string[] QuestTitles = { "적 50마리 처치", "캠페인 2회 도전", "던전 입장", "오늘 접속", "상자 2개 열기", "장비 2회 강화" };
        static readonly int[] QuestGoals = { 50, 2, 1, 1, 2, 2 };

        /// <summary>
        /// T257 — 이 팝업이 지금 그리는 «판»(일일/주간). 표가 있으면 줄·점수·트랙이 전부 여기서 나오고, 없으면 <c>null</c> 이라 껍데기 배열로 돌아간다.
        /// <para>정적으로 두는 까닭은 <see cref="QuestRow"/> 가 줄 번호만 받는 옛 꼴이라서다 — 한 번에 한 팝업만 뜨므로 섞일 자리가 없다.</para>
        /// </summary>
        static QuestData _qd; static QuestData.Track _qt; static SaveData _qs; static bool _qDaily = true;

        /// <summary>
        /// T258 4항 — 지금 «업적» 탭을 그리는 중인가(표 <c>_ad</c> 가 같이 선다). 업적은 <b>일일·주간과 다른 표</b>라 판이 셋이 된다.
        /// <para>업적 탭은 <b>메달 트랙도 새로고침 줄도 안 그린다</b>(주인 «메달 없음» · 누적은 초기화되지 않으니 «새로고침까지» 가 거짓말이 된다).</para>
        /// </summary>
        static bool _qAch; static AchievementData _ad;

        /// <summary>지금 판의 줄 수 — 업적이면 표의 17줄 · 퀘스트면 표가 정하고(일일·주간 8줄) 표가 없으면 종전 6줄이다.</summary>
        static int QuestRows => _qAch ? (_ad != null ? _ad.List.Count : 0)
                              : _qt != null ? _qt.Quests.Count : Layout.QsRowCount;

        /// <summary>
        /// T257 — 트랙 칸이 주는 물건의 아이콘. <b>이름 → 그림</b> 짝짓기는 화면 몫이라 여기 있다(표는 이름만 적는다 · <c>quest.json</c> 의 그 주석).
        /// 키 셋은 <see cref="GachaKeys.Icon"/> 한 곳이 갖고(T255), 나머지는 이 레포가 이미 쓰는 그림이다.
        /// 모르는 이름이면 메달로 — <b>빈 칸을 그리지 않는다</b>(그림이 없다고 트랙이 무너지면 안 된다).
        /// </summary>
        static string QuestRewardIcon(QuestData.Reward r)
        {
            if (r == null) return "ui.iconMedal";
            if (GachaKeys.IsKey(r.Item)) return GachaKeys.Icon(r.Item);
            switch (r.Item)
            {
                case Core.Mail.ItemGold: return "ui.coin";
                case Core.Mail.ItemGem: return "hud.gem";
                case Core.Mail.ItemPetEgg: return "pet.egg";   // 던전 보상 칸(EventsScreen:940)이 쓰는 그 그림
                case QuestRun.ItemTicket: return "ui.iconTokenRed";
                default: return "ui.iconMedal";
            }
        }
        static readonly string[] QuestNums = { "0", "20", "40", "60", "80", "100" };
        static readonly string[] TrackIcons = { "ui.iconMedal", "ui.coin", "ui.bookBlue", "ui.gemRed", "pi.magic", "ui.gemRed" };

        /// <summary>퀘스트 보상 점수(레퍼런스 15 의 메달 숫자 — 시스템이 없어 표시만).</summary>
        static readonly string[] QuestScores = { "20", "20", "20", "10", "20", "20" };

        /// <summary>
        /// 프리팹에서 온 글자를 «어두운 조각 위» 에서 읽히게 — 흰 글자 + 어두운 외곽선(<see cref="UiKit.Text"/> 가 새 글자에 붙이는 것과 같은 규격).
        /// <see cref="UiKit.SetText"/> 는 색·크기만 바꾸고 외곽선은 안 붙이므로 조각 글자에는 이걸 한 번 더 부른다(T63 1항의 반대 경우 = 바탕이 어두운 쪽).
        /// </summary>
        static TMP_Text OnDark(TMP_Text t, Color? color = null)
        {
            if (t == null) return null;
            t.color = color ?? Palette.White;
            // T194 — 여기에 규격이 «손으로 베껴» 적혀 있었다(색 리터럴 + 두께 0.05·1.5~4px). 그래서 UiKit 의 규칙을 올려도 이 자리만 옛 두께로 남고
            // TextAudit(«두께 ≠ UiKit.OutlineWidth»)가 그 줄들을 어긋남으로 세어 TextSizeGateTests 가 빨개진다 → 규격은 한 함수에서만 나온다.
            // 베낀 줄은 bestFit 글자에서 «최대 크기» 가 아니라 fontSize 로 재던 차이도 있었다(자는 최대 크기로 잰다) — 그 어긋남도 같이 사라진다.
            UiKit.EnsureOutline(t);
            return t;
        }

        /// <summary>이름이 <paramref name="prefix"/> 로 시작하는 첫 «직계» 자식 — 프리팹 인스턴스가 «이름 (1)» 처럼 붙어 나올 때 쓴다.</summary>
        /// <summary>
        /// 공통 팝업이 세운 제목 리본을 끈다(T146 ⓐ) — 레퍼런스 30·31 에는 리본이 없고 상자 폭 «명판»(<see cref="Plate"/>)이 제목이다.
        /// <para>
        /// <b>이름으로 찾지 않는다</b>: <see cref="UiKit.Spawn"/> 이 조각 이름을 «카탈로그 키»(예 <c>ui.title.green</c>)로 바꿔 놓으므로
        /// 종전 줄(<c>ChildStarting(box, "Title_01")</c>)은 프리팹 이름을 찾다 한 번도 안 맞았고, 그래서 글자 없는 초록 리본이 그림 띠 위에 그대로 떠 있었다
        /// (`screens` run 257 의 30 PNG 실측 · 결정 388). <see cref="PopupRibbonTag"/> 는 <see cref="UiKit.Popup"/> 이 리본에만 붙이는 표라 이름이 바뀌어도 맞는다.
        /// </para>
        /// </summary>
        /// <summary>T146 ⓑ 의 적 외형 — 전투의 «곤봉 적»(<c>BattleWorld.EnemySkin</c> 갈래 1)과 **같은 조각 세 개**다(새 그림·새 카탈로그 키 0 · 레퍼런스 30 의 적도 같은 꼴이다).</summary>
        static CharacterRig.Skin FoeSkin() => new CharacterRig.Skin { Helmet = "cm.meleeB.helmet", Chest = "cm.meleeB.chest", Axe = "cm.meleeB.axe" };

        static void HideRibbon(RectTransform box)
        {
            if (box == null) return;
            foreach (var tag in box.GetComponentsInChildren<PopupRibbonTag>(true)) tag.gameObject.SetActive(false);
        }

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
        /// 껍데기 규칙(T44)은 표가 없을 때만 — 진행 0/N · «이동» 은 닫기만 · 완료 줄은 프리팹 ✅. 표가 있으면 «이동» 은 T318 의 길잡이(<see cref="App.Hint(QuestData.Go)"/>)로 간다.
        /// </summary>
        /// <param name="daily">true = 일일(동메달) · false = 주간(은메달). 탭은 <b>닫고 다시 여는</b> 길로 갈아탄다 —
        /// 이미 잘 도는 길이라 조립을 반쯤 되돌리는 것보다 안전하다(줄 수·트랙 칸 수가 판마다 다르다).</param>
        public static void Quest(App app, bool daily = true) { _qAch = false; QuestPopup(app, daily); }

        /// <summary>
        /// T258 4항 — 같은 팝업의 «업적» 판. 퀘스트와 <b>한 함수를 같이 쓴다</b> — 상자·리본·목록·탭이 전부 같은 자리라
        /// 따로 짜면 «같은 것 두 벌» 이 되어 한쪽만 고쳐지는 날이 온다(T267 3단계가 같은 자리에서 한 판단).
        /// </summary>
        public static void Achievements(App app) { _qAch = true; QuestPopup(app, true); }

        static void QuestPopup(App app, bool daily)
        {
            var ov = app.Overlay; var B = Layout.QsBox;
            // T257 — 그리기 전에 표를 잡고 날·주를 민다(어제 셈이 오늘 화면에 남지 않게 · 표가 없으면 종전 껍데기 그대로 뜬다).
            _qd = app.Data != null ? app.Data.Quest : null; _qs = app.Save; _qDaily = daily;
            _ad = app.Data != null ? app.Data.Achievement : null;   // T258 — 업적은 표가 따로다(못 읽으면 줄이 0 이고 다른 탭은 멀쩡하다)
            if (_qd != null && _qs != null) QuestRun.Roll(_qs, _qd, System.DateTime.Now);
            _qt = _qd != null ? (_qDaily ? _qd.Daily : _qd.Weekly) : null;
            var root = (RectTransform)ov.OpenPrefab("ui.progressionMission2").transform;
            // 공통 팝업 문법(ROUTINE) — 배경 탭 = 닫기 · 닫기 X 는 안 쓴다(프리팹 조각은 지우지 않고 끈다)
            var dim = UiKit.Find(root, "Dimmed"); if (dim != null) UiKit.Clickable(dim, () => ov.Close(), false);
            // T258 — 프리팹에 **«Disabled» 덮개 둘**이 들어 있다(크림색 알파 0.70 · 439×284 = 상자 폭의 절반쯤). 데모에서 «잠긴 자리» 를
            //   흐리게 보이려고 둔 조각이라 우리 화면에는 뜻이 없는데 여태 아무도 안 껐다. 일일 판에서는 눈에 안 띄는 자리에 있었고,
            //   **업적 판(줄 17 · 목록이 트랙 자리까지 늘었다)에서 드러났다** — `screens/15b_quest_ach.png` 의 «아래 두 줄을 덮은
            //   반투명 사각형» 이 그것이다(실측 화면 x29.6~67.5% · y64.6~75.3%). §5 는 **이름표 없는 조각을 못 재므로** 표 점수는 10.0 이었다.
            //   ⚠ 이름으로 **전부** 끈다(`UiKit.Hide` 는 첫 하나만 찾는다) · 줄을 복제하기 **전에** 끄므로 복제본도 꺼진 채로 태어난다.
            foreach (var dis in root.GetComponentsInChildren<Transform>(true)) if (dis.name == "Disabled") dis.gameObject.SetActive(false);
            UiKit.Hide(root, "Button_Close_01");
            var tc = UiKit.Text(ov.Root, "탭하여 닫기", TextSize.Body, Palette.White, TextAnchor.MiddleCenter, false, true);
            tc.name = "TapToClose"; tc.fontStyle = FontStyles.Bold; UiKit.Pct(tc.rectTransform, Layout.BookClose);

            var box = (RectTransform)UiKit.Find(root, "Popup"); box.name = "QuestBox"; UiKit.Pct(box, B);
            foreach (var g in box.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = true;   // 상자 뒤로 클릭이 새지 않게(UiKit.Popup 과 같은 처리)
            UiKit.PatternBg(box);   // T72 ① 팝업 배경 패턴

            // 제목 리본(프리팹 Title_Tapered_01_Brown) — 표 ⑳ 제목 자리(박스 윗변에 걸친다 · y 가 박스보다 위라 Within 이 음수)
            var band = ChildStarting(box, "Title_Tapered_01");
            if (band != null)
            {
                UiKit.Pct(band, Layout.QsTitleBand.Within(B));
                var bt = UiKit.SetText(band, "Text (TMP)", _qAch ? "업적" : "퀘스트", null, TextSize.Title, TextKind.Title);   // T258 — 같은 리본, 판 이름만 바뀐다
                if (bt != null) { bt.enableAutoSizing = true; bt.fontSizeMin = TextSize.BestFitMin; bt.fontSizeMax = TextSize.Title; RibbonTextFit(bt); }
                // T320 ⓑ(주인 «퀘스트, 출석 … 리본 제목 팝업 전부에») — 리본 뒤 «반 잘린» 빛.
                //   이 팝업은 제 프리팹으로 서서 `Overlay.Box` 를 안 지나므로 공통 배선이 안 닿는다 ⇒ 리본을 놓은 다음 한 줄로 부른다.
                Overlay.RibbonGlowOn(box, (RectTransform)band);
            }

            // 점수 트랙 · 새로고침 줄 · 목록 상자 = 레퍼런스 15 그대로(프리팹에 없는 조각)
            // T258 4항 — **업적 탭에는 트랙도 새로고침 줄도 없다**: 메달이 없고(주인 «메달 없음») 누적은 초기화되지 않으니
            //   «새로고침까지 mm:ss» 는 그 탭에서 **거짓말**이 된다. 대신 목록이 그 자리까지 올라와 빈 구멍이 안 생긴다.
            RectTransform trackBox = null, refresh = null;
            if (!_qAch)
            {
            var trackPanel = UiKit.Panel(box, "TrackBox", "fr.r12", Palette.A(Palette.Dim, 0.55f)); UiKit.Pct(trackPanel.rectTransform, Layout.QsTrackBox.Within(B)); trackBox = trackPanel.rectTransform;
            // T257 — 트랙 숫자·아이콘은 표가 정한다(첫 칸 «0» 은 시작점이라 표에 없다 · 상품 아이콘은 그 칸이 주는 물건에서).
            string[] trackNums = QuestNums, trackIcons = TrackIcons;
            if (_qt != null)
            {
                trackNums = new string[_qt.Steps.Count + 1]; trackIcons = new string[_qt.Steps.Count + 1];
                trackNums[0] = "0"; trackIcons[0] = "ui.iconMedal";
                for (int i = 0; i < _qt.Steps.Count; i++)
                {
                    trackNums[i + 1] = _qt.Steps[i].Points.ToString();
                    var rw = _qt.Steps[i].Rewards.Count > 0 ? _qt.Steps[i].Rewards[0] : null;
                    trackIcons[i + 1] = rw == null ? "ui.iconMedal" : QuestRewardIcon(rw);
                }
            }
            Track(box, B, Layout.QsTrackIcon, Layout.QsTrackPitch, trackNums.Length, Layout.QsTrackNums, Palette.Yellow, trackIcons, trackNums, "트랙 아이콘 줄(" + trackNums.Length + "칸)", "트랙 아이콘(1칸)");
            // T257 — 주인 «20포인트 채워지면 퀘스트 팝업 상단에 20포인트 부분 것 얻을 수 있고». **채운 칸만** 눌린다.
            //  받으면 즉시 지급(`QuestRun.Claim`)하고 T241 리워드 팝업을 띄운 뒤, 닫을 때 이 팝업을 **다시 연다**(결정 671 의 그 꼴).
            //  못 받는 칸은 아예 안 걸어 둔다 — 눌리는데 아무 일도 안 나는 것이 제일 나쁘다.
            if (!_qAch && _qd != null && _qt != null && _qs != null)
                for (int k = 0; k < _qt.Steps.Count; k++)
                {
                    if (!QuestRun.CanClaim(_qs, _qd, _qDaily, k)) continue;
                    var cell = UiKit.Find(box, "Track:" + (k + 1));   // 0번 칸은 «0점» 표시라 한 칸 민다
                    if (cell == null) continue;
                    int idx = k; bool dailyNow = _qDaily;
                    UiKit.Clickable(cell, () =>
                    {
                        var d2 = app.Data != null ? app.Data.Quest : null; if (d2 == null) return;
                        var step = (dailyNow ? d2.Daily : d2.Weekly).Steps[idx];
                        var got = new List<RewardPopup.Item>();
                        foreach (var rw in step.Rewards) got.Add(RewardPopup.Item.Of(QuestRewardIcon(rw), UiKit.FmtQty(rw.Amount), amount: (int)rw.Amount));
                        if (!QuestRun.Claim(app.Save, d2, dailyNow, idx)) return;
                        app.Persist(); app.Current?.Refresh();
                        RewardPopup.Show(got, () => Quest(app, dailyNow));   // 닫으면 이 팝업을 다시(받은 칸이 꺼진 채로)
                    });
                }
            refresh = TimerRow(box, B, Layout.QsRefresh, "새로고침까지 " + Dashes, "Refresh");
            }
            // 업적이면 목록 상자가 트랙 자리까지 올라온다 — 표에 새 수를 넣지 않고 **있는 두 수로** 만든다(위=트랙 상자의 위 · 아래=목록 상자의 아래).
            var listR = _qAch ? new Layout.R(Layout.QsListBox.X, Layout.QsTrackBox.Y, Layout.QsListBox.W,
                                             Layout.QsListBox.Y + Layout.QsListBox.H - Layout.QsTrackBox.Y)
                              : Layout.QsListBox;
            var listBox = UiKit.Panel(box, "ListBox", "fr.r12", Palette.A(Palette.Dim, 0.55f)); UiKit.Pct(listBox.rectTransform, listR.Within(B));

            // 미션 줄 = 프리팹 ScrollView/Content(GridLayoutGroup) — 1열 · 칸 = 표 ⑳ 줄 · 세로 간격 = 피치 − 줄
            var sv = (RectTransform)UiKit.Find(box, "ScrollView");
            float rowTop = _qAch ? listR.Y + (Layout.QsRow1.Y - Layout.QsListBox.Y) : Layout.QsRow1.Y;   // 상자 안 여백은 그대로 두고 위만 올린다
            var viewR = new Layout.R(Layout.QsRow1.X, rowTop, Layout.QsRow1.W, listR.Y + listR.H - 0.8f - rowTop);
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
            // T257 — 표가 프리팹 줄(6)보다 많으면(일일·주간 8줄) 첫 줄을 복제해 채운다. 목록은 ScrollView 안이라 넘치면 스크롤된다.
            if (content != null && content.childCount > 0)
                while (content.childCount < QuestRows)
                    UnityEngine.Object.Instantiate(content.GetChild(0).gameObject, content).name = RowName + content.childCount;   // `using System;` 때문에 «Object» 가 모호하다
            int rows = content != null ? content.childCount : 0, want = Mathf.Min(QuestRows, rows);
            for (int i = rows - 1; i >= want; i--) content.GetChild(i).gameObject.SetActive(false);   // 프리팹 줄이 표(6줄)보다 많으면 남는 것은 지우지 말고 끈다
            for (int i = 0; i < want; i++)
            {
                var frame = (RectTransform)content.GetChild(i); frame.name = RowName + i; frame.gameObject.SetActive(true);
                // T258 — 프리팹 데모 줄 여덟 중 **둘(7·8번)에 «Disabled» 덮개**가 달려 있다(크림색 알파 0.70 · 439×284).
                //   데모에서 «잠긴 줄» 을 흐리게 보이려고 둔 조각이라 우리 화면에는 뜻이 없는데, 여태 아무도 안 껐다.
                //   일일 판(줄 8)에서는 그 두 줄이 목록 밖으로 밀려 안 보였고, **업적 판(줄 17 · 목록이 위로 늘었다)에서 드러났다**
                //   — `screens/15b_quest_ach.png` 의 «아래 두 줄을 덮은 반투명 사각형» 이 이것이다(실측 x29.6~67.5% · y64.6~75.3%).
                //   판을 안 가리고 **모든 줄에서** 끈다: 일일 판에도 같은 덮개가 살아 있었고 보이지 않았을 뿐이다.
                UiKit.Hide(frame, "Disabled");
                var parts = _qAch ? AchRow(app, frame, i) : QuestRow(app, frame, i, ov);
                if (i == 0) { row1 = frame; medal1 = parts.Medal; title1 = parts.Title; bar1 = parts.Bar; go1 = parts.Go; } else if (i == 1) row2 = frame;
            }

            // 박스 아래 탭 3 = 레퍼런스 15 그대로
            var tabs = new RectTransform[3]; string[] tabNames = { "일일", "주간", "업적" };
            for (int i = 0; i < 3; i++)
            {
                // T257 — «일일»·«주간» 은 판을 갈아탄다(닫고 다시 연다) · «업적» 은 T258 절이라 여기서는 껍데기 그대로 둔다.
                int ti = i;
                System.Action onTab = ti == 0 ? (System.Action)(() => Quest(app, true))
                                    : ti == 1 ? (System.Action)(() => Quest(app, false))
                                              : (System.Action)(() => Achievements(app));   // T258 4항 — «업적» 도 이제 판을 갈아탄다
                var t = tabs[i] = UiKit.Button(ov.Root, "ui.btnGray", tabNames[i], () => { if (onTab != null) onTab(); }, Sh(Layout.QsTab, i * Layout.QsTabPitch, 0)); t.name = "Tab:" + i;
                // 지금 보는 판만 밝다 — 표가 없으면 종전처럼 첫 탭이 밝다.
                bool tabOn = _qAch ? i == 2 : _qd == null ? i == 0 : (i == (_qDaily ? 0 : 1));
                if (!tabOn) foreach (var im in t.GetComponentsInChildren<Image>(true)) im.color = Color.Lerp(im.color, Palette.Dim, 0.45f);   // 비활성 탭은 어둡게(첫 탭 «일일» 활성)
                // T69-lobbypopups — 탭마다 «검은 아웃라인»(레퍼런스 15 도 세 탭이 각자 어두운 외곽선이다) · 어둡게 칠한 «뒤» 에 걸어야 링이 Dim 쪽으로 섞이지 않는다
                UiKit.Bordered(t);
            }
            // 비평 이름표(표 ⑳)
            if (band != null) UiKit.Tag(band, "제목 리본"); UiKit.Tag(box, "팝업 박스"); UiKit.Tag(listBox.transform, "목록 상자");
            if (trackBox != null) UiKit.Tag(trackBox, "점수 트랙 상자");   // 업적 탭에는 없는 조각이다(§5 는 판마다 재는 표가 다르다)
            if (refresh != null) UiKit.Tag(refresh, "새로고침 줄");
            // T258 §5 — 이름표는 **판마다 다르다**: 업적 판의 오른쪽 버튼은 «이동» 이 아니라 «받기» 이고 보상 칸은 메달이 아니라 상품이다.
            //   같은 이름을 쓰면 표 ⓐ 와 ⑳ 이 서로의 값을 잰다(자리는 같아도 «무엇인가» 가 다르다 · 결정 756 의 이름표 대조가 그것을 잡는다).
            if (_qAch)
            {
                UiKit.Tag(row1, "업적 줄 1"); UiKit.Tag(row2, "업적 줄 2"); UiKit.Tag(medal1, "업적 보상 칸(1줄)"); UiKit.Tag(title1, "업적 제목(1줄)"); UiKit.Tag(bar1, "업적 진행바(1줄)"); UiKit.Tag(go1, "받기 버튼(1줄)");
            }
            else
            {
                UiKit.Tag(row1, "퀘스트 줄 1"); UiKit.Tag(row2, "퀘스트 줄 2"); UiKit.Tag(medal1, "퀘스트 보상 메달(1줄)"); UiKit.Tag(title1, "퀘스트 제목(1줄)"); UiKit.Tag(bar1, "퀘스트 진행바(1줄)"); UiKit.Tag(go1, "이동 버튼(1줄)");
            }
            UiKit.TagGroup(ov.Root, "탭 줄(3칸)", tabs); UiKit.Tag(tabs[0], "탭(1칸)"); TagClose(app);
            UiKit.PopIn(box);   // 공통 팝업 등장 연출(T49 · UiKit.Popup 이 상자에 거는 것과 같다)
        }

        /// <summary>
        /// 줄 바탕색 — <b>레퍼런스 15 는 «할 일 남은 줄 = 밝은 크림 · 다 한 줄 = 어두운 회갈»</b> 이고, 그 색이 «다 했는가» 를 한눈에 말한다.
        /// <para>
        /// 우리 화면은 여태 <b>줄 번호</b>를 따르고 있었다(실측: 앞 두 줄 <c>#B49E4C</c> · 나머지 <c>#A8917A</c> — 프리팹이 들고 온 두 가지 꼴).
        /// 즉 «로그인하기»(완료)도 밝고 «적 50개»(미완)도 밝아 <b>색이 아무 뜻이 없었다</b>.
        /// </para>
        /// 값은 눈대중이 아니라 <c>tools/ref_color.py</c> 로 주인 그림에서 잰 것이다(미완 <c>#F1E2C1</c> · 완료 <c>#625A4F</c>).
        /// 조각(테두리·9-slice)은 그대로 두고 <b>바탕 한 장만</b> 칠한다 — `EventsScreen.DarkenListFrame`(T62)이 이미 쓰는 꼴이다.
        /// <para>⚠ 업적 판은 «영영 완료» 가 없다(단계가 계속 늘어난다) — 그래서 <b>늘 밝다</b>. «밝음 = 할 일이 있다» 로 읽으면 두 판이 한 규칙이다.</para>
        /// </summary>
        static void RowTint(Transform item, bool done)
        {
            var frame = UiKit.Find(item, "ListFrame_08"); if (frame == null) return;
            // ⚠ 이 조각은 **가지가 둘**이다 — `Nomal/{Bg,Border,BottomBar}` 과 `Focus/{Bg,Border,BottomBar}`(이름이 똑같다 · 철자도 프리팹 그대로 «Nomal»).
            //   `UiKit.Find(frame, "Bg")` 는 **먼저 찾은 하나**를 주는데 그것이 «Focus» 쪽이라, 첫 고침(결정 894)은 **안 보이는 가지를 칠했다**
            //   — 사진에서 색이 거의 안 바뀐 까닭이 이것이다(실측: 완료 줄 #B49E4C → #B49D4C). **보이는 가지를 이름으로 짚는다.**
            // ⚑ **탐침이 답을 줬다**(`screens/t258rows.json` · run 822) — 내 색은 제대로 들어가 있었다(`Nomal/Bg` = `625A4F`/`F1E2C1`).
            //   그런데 **프리팹 줄 0·1 은 «Focus» 가지가 켜진 채로** 온다(데모의 «선택된 줄» 표시 · `Focus/Bg` = `FFF88F` 연노랑).
            //   그 가지가 `Nomal` **위에** 그려져 색을 통째로 덮는다 — 줄 0·1 만 올리브였던 까닭이고, 두 번의 헛고침이 이것을 못 본 까닭이다.
            //   ⇒ **가지를 끈다.** 「같은 이름의 상태 가지(Nomal/Focus/Disabled)를 들고 오는 조각은 «어느 가지가 켜져 있나» 부터 본다」(결정 906·916).
            var focus = UiKit.Find(frame, "Focus"); if (focus != null) focus.gameObject.SetActive(false);
            // ⚑ 탐침 2회차(run 832 사진 + `t258rows.json`) — 가지를 끄자 올리브는 사라졌는데 **색은 여전히 안 바뀌었다**(#A8917A ↔ #AA9F88).
            //   표를 다시 읽으면 까닭이 보인다: `Nomal/Bg` 는 **스프라이트가 없고**(`"sprite": "-"`) 그 **뒤에 오는 형제** `BottomBar`·`Border`
            //   가 **줄 전체 크기**(820.2 × 138.0)에 실제 그림(`ListFrame_08_White_BorderNomal`)을 들고 **위에** 그린다.
            //   즉 «바탕» 한 장이 아니라 **세 장이 겹친 면**이고, 보이는 것은 마지막 장이다. ⇒ 셋을 같이 칠한다.
            var col = done ? RowDoneColor : RowTodoColor;
            foreach (var part in new[] { "Nomal/Bg", "Nomal/BottomBar", "Nomal/Border" })
            {
                var t = UiKit.Find(frame, part); if (t == null) continue;
                var im = t.GetComponent<Image>(); if (im != null) im.color = col;
            }
        }
        /// <summary>할 일이 남은 줄의 바탕 — 주인 그림 실측 <c>#F1E2C1</c>.</summary>
        static Color RowTodoColor => Palette.Hex("#F1E2C1");
        /// <summary>다 한 줄의 바탕 — 주인 그림 실측 <c>#625A4F</c>.</summary>
        static Color RowDoneColor => Palette.Hex("#625A4F");

        struct QuestRowParts { public RectTransform Medal, Title, Bar, Go; }

        /// <summary>줄 이름의 머리 — 판마다 다르다(자·§5 가 «Quest:0» 과 «Ach:0» 을 헷갈리지 않게).</summary>
        static string RowName => _qAch ? "Ach:" : "Quest:";

        /// <summary>
        /// T258 4항 — 업적 줄 하나. 퀘스트 줄(<see cref="QuestRow"/>)과 <b>같은 프리팹 조각</b>을 쓰고 갈리는 것만 갈린다:
        /// <list type="bullet">
        /// <item>보상 칸이 <b>메달이 아니라 실제 상품</b>이다(주인은 전부 다이아로 줬다 — 표가 정한다).</item>
        /// <item>진행도가 «누적 / <b>이번 단계</b> 목표» 다(단계 N 목표 = 첫 목표 × N · 5/10 처럼).</item>
        /// <item>오른쪽이 «이동» 이 아니라 <b>«받기»</b> 다 — 깬 단계가 있으면 살아 있고, 없으면 눌리지 않는다(«눌리는데 아무 일도 안 나는 것» 금지 · 결정 771).</item>
        /// </list>
        /// 밀린 단계는 한 번에 하나씩이라(주인 «순차») 받고 나면 이 팝업을 <b>다시 연다</b> — 남은 단계가 있으면 «받기» 가 그대로 살아 있다.
        /// </summary>
        static QuestRowParts AchRow(App app, RectTransform frame, int i)
        {
            var parts = new QuestRowParts();
            var item = frame;
            var row = _ad != null && i < _ad.List.Count ? _ad.List[i] : null;
            if (row == null) return parts;
            var save = app.Save;
            int have = Core.Achievement.Count(save, row.Counter);
            int goal = Core.Achievement.Goal(save, _ad, row.Counter);
            int shown = Core.Achievement.Shown(save, _ad, row.Counter);
            bool can = Core.Achievement.CanClaim(save, _ad, row.Counter);
            AttendArt(row.Item, out _, out string icon);   // 이름 → 그림 짝짓기는 이 화면이 이미 한 곳에서 한다(T253 4항)
            RowTint(item, false);   // T258 — 업적에는 «영영 완료» 가 없다(단계가 계속 는다) ⇒ 늘 밝다(= 할 일이 있다)

            // 보상 칸(Group_Price) — 퀘스트의 «메달 + 점수» 자리에 «상품 + 수량» 을 둔다(자리·규격은 그대로).
            var medal = (RectTransform)UiKit.Find(item, "Group_Price");
            if (medal != null)
            {
                var hlg = medal.GetComponent<HorizontalLayoutGroup>(); if (hlg != null) hlg.enabled = false;
                UiKit.Pct(medal, Layout.QsRowMedal.Within(Layout.QsRow1));
                var mi = (RectTransform)UiKit.Find(medal, "Icon");
                if (mi != null) { UiKit.Pct(mi, 0, 0, 100, 100); var img = UiKit.SetSprite(medal, "Icon", icon); if (img != null) { img.preserveAspect = true; img.color = Color.white; } }
                var mt = OnDark(UiKit.SetText(medal, "Text (TMP)", UiKit.FmtQty(row.Amount), Palette.Yellow, TextSize.Body), Palette.Yellow);
                if (mt != null) { UiKit.Pct(mt.rectTransform, -20, 98, 140, 72); mt.alignment = UiKit.TmpAlign(TextAnchor.MiddleCenter); mt.enableAutoSizing = true; mt.fontSizeMin = TextSize.BestFitMin; mt.fontSizeMax = TextSize.Body; mt.textWrappingMode = TextWrappingModes.NoWrap; }
                parts.Medal = medal;
            }
            var title = OnDark(UiKit.SetText(item, "Text (TMP)", row.Label, Palette.White, TextSize.Body));
            if (title != null)
            {
                var tr = title.rectTransform; UiKit.Pct(tr, Layout.QsRowTitle.WithH(Layout.LpLineH).Within(Layout.QsRow1));
                title.alignment = UiKit.TmpAlign(TextAnchor.MiddleLeft); title.name = "Title"; parts.Title = tr;
                title.enableAutoSizing = true; title.fontSizeMin = TextSize.BestFitMin; title.fontSizeMax = TextSize.Body;
                title.textWrappingMode = TextWrappingModes.Normal; title.overflowMode = TextOverflowModes.Truncate;
            }
            var slider = item.GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                var sr = (RectTransform)slider.transform; sr.name = "Bar";
                UiKit.Pct(sr, Layout.QsRowBar.WithH(Layout.LpBarH).Within(Layout.QsRow1));
                slider.value = goal > 0 ? Mathf.Clamp01((float)shown / goal) : 0f;
                // «초록 = 열림/완료» 관례(T212)는 여기서 «받을 수 있다» 를 뜻한다 — 업적에는 «영영 완료» 가 없다(단계가 계속 늘어난다).
                if (can) { var fill = BarFill(slider); if (fill != null) fill.color = Palette.Green; }
                var st = sr.GetComponentInChildren<TMP_Text>(true);
                if (st != null) { st.text = shown + "/" + goal; st.fontSize = TextSize.Body; st.enableAutoSizing = true; st.fontSizeMin = TextSize.BestFitMin; st.fontSizeMax = TextSize.Body; st.textWrappingMode = TextWrappingModes.NoWrap; OnDark(st); TextAudit.Mark(st, TextKind.Body); }
                parts.Bar = sr;
            }
            var check = UiKit.Find(item, "Check"); if (check != null) check.gameObject.SetActive(false);   // ✅ 는 업적에 없다(끝이 없다)
            string counter = row.Counter;
            var btn = UiKit.Button(item, can ? "ui.btnOrange" : "ui.btnGray", "받기", () =>
            {
                var d2 = app.Data != null ? app.Data.Achievement : null; if (d2 == null) return;
                if (!Core.Achievement.Claim(app.Save, d2, counter, out string it, out double amt)) return;
                Core.Mail.Give(app.Save, it, amt);   // 이름 → 담는 자리는 Mail.Give 한 곳이다(«우편으로 받은 다이아» 와 갈라지지 않게 · T257 과 같은 규약)
                app.Persist(); app.Current?.Refresh();
                AttendArt(it, out _, out string ic);
                RewardPopup.Show(new List<RewardPopup.Item> { RewardPopup.Item.Of(ic, UiKit.FmtQty(amt), amount: (int)amt) }, () => Achievements(app));
            }, Layout.QsRowGo.Within(Layout.QsRow1));
            btn.name = "AchBtn"; parts.Go = btn;
            if (!can) UiKit.SetInteractable(btn.GetComponent<Button>(), false);
            UiKit.Bordered(frame);
            if (parts.Medal != null) UiKit.Bordered(parts.Medal, UiKit.BorderKeySmall);
            return parts;
        }

        /// <summary>
        /// 슬라이더의 «채움» 그림 — <see cref="Slider.fillRect"/> 를 먼저 보고, 그 참조가 비어 있으면 이름 <c>Fill</c> 로 찾는다(T212).
        /// <para>둘을 다 보는 까닭 = <c>fillRect</c> 는 <b>프리팹에 직렬화된 참조</b>라 조각을 다시 저장하거나 중첩 프리팹을
        /// 갈아 끼우면 조용히 비는 자리다. 그때 «색이 안 걸린 채로» 지나가지 않게 한다(그림 이름은 `Slider_02_BasePrefab` 부터 <c>Fill</c> 로 고정이다).</para>
        /// </summary>
        public static Image BarFill(Slider slider)
        {
            if (slider == null) return null;
            if (slider.fillRect != null) { var im = slider.fillRect.GetComponent<Image>(); if (im != null) return im; }
            var f = UiKit.Find(slider.transform, "Fill");
            return f != null ? f.GetComponent<Image>() : null;
        }

        /// <summary>
        /// 미션 줄 한 개 — 프리팹 <c>ListFrame_08</c>(칸 바탕) 안의 <c>ListItem_Mission_02</c> 조각을 표 ⑳ 의 줄 안 자리로 옮긴다.
        /// 격자 칸 자신이 <c>ListItem_Mission_02</c> 이고 그 안에 바탕 <c>ListFrame_08</c> · 제목 · <c>Slider_02_Yellow</c> · <c>Group_Price</c> · <c>Check</c> 가 있다(이름은 <c>Quest:i</c> 로 바꾼다 · 프리팹 유래 증거는 안쪽 <c>ListFrame_08</c>).
        /// 옮기는 것: 보상(<c>Group_Price</c> = 아이콘 + 점수 · 가로 배치를 끄고 레퍼런스처럼 «아이콘 위 · 숫자 아래») · 제목 · 진행바(<c>Slider_02_Yellow</c>) · 받기 표시(<c>Check</c> · 슬라이더 밑에 있던 것을 줄 오른쪽으로).
        /// 미완 줄(앞 3개)은 레퍼런스 15 처럼 주황 «이동» 버튼(표가 있으면 T318 길잡이 · 껍데기면 닫기만) · 완료 줄(뒤 3개)은 프리팹 ✅.
        /// <b>진행바 채움은 «완료» 에만 초록</b>(T212 · 우리가 이미 쓰는 «초록 = 열림/완료» — 레퍼런스 15 도 완료 줄이 초록이다) ·
        /// 미완 줄은 프리팹이 달고 온 노랑 그대로다. <b>다른 화면의 노란 진행바(로딩·경험치 등)는 이 관례에 안 걸린다.</b>
        /// </summary>
        static QuestRowParts QuestRow(App app, RectTransform frame, int i, Overlay ov)
        {
            var parts = new QuestRowParts();
            // 격자 칸 «자신» 이 `ListItem_Mission_02` 이고 `ListFrame_08`(원본의 ListFrame_07 을 갈아 끼운 것)은 그 «안쪽 바탕» 이다 — CI #142 가 잡아 준 계층(결정 173).
            var item = frame;
            // T257 — 표가 있으면 줄의 «무엇을 · 얼마나 · 얼마 받나» 가 전부 표와 세이브에서 온다.
            //  표가 없을 때만 레퍼런스 15 의 «앞 3줄 Go · 뒤 3줄 ✅» 껍데기로 돌아간다.
            var q = _qt != null && i < _qt.Quests.Count ? _qt.Quests[i] : null;
            int have = q != null ? QuestRun.Count(_qs, _qDaily, q.Counter) : 0;
            int goal = q != null ? q.Goal : (i < QuestGoals.Length ? QuestGoals[i] : 1);
            bool done = q != null ? q.Done(have) : i >= 3;

            RowTint(item, done);   // T258 — 줄 바탕은 «줄 번호» 가 아니라 «다 했는가» 를 말한다(레퍼런스 15)

            // 보상 칸(Group_Price) — 가로 레이아웃을 끄고 아이콘 위 · 점수 아래(레퍼런스 15 의 메달 + 숫자)
            var medal = (RectTransform)UiKit.Find(item, "Group_Price");
            if (medal != null)
            {
                var hlg = medal.GetComponent<HorizontalLayoutGroup>(); if (hlg != null) hlg.enabled = false;
                UiKit.Pct(medal, Layout.QsRowMedal.Within(Layout.QsRow1));
                var mi = (RectTransform)UiKit.Find(medal, "Icon");
                if (mi != null) { UiKit.Pct(mi, 0, 0, 100, 100); var img = UiKit.SetSprite(medal, "Icon", "ui.iconMedal"); if (img != null) { img.preserveAspect = true; img.color = Color.white; } }
                // 점수 숫자 = 메달 아래(칸 높이의 72% · 본문 40 한 줄이 안 줄고 들어간다 — 전 코드와 같은 값)
                var mt = OnDark(UiKit.SetText(medal, "Text (TMP)", q != null ? q.Medal.ToString() : QuestScores[i], Palette.Yellow, TextSize.Body), Palette.Yellow);
                // 전 코드(UiKit.Label(medal, -20, 98, 140, 72, …))와 같은 자리·규격 — 메달 아래 점수 한 줄
                if (mt != null) { UiKit.Pct(mt.rectTransform, -20, 98, 140, 72); mt.alignment = UiKit.TmpAlign(TextAnchor.MiddleCenter); mt.enableAutoSizing = true; mt.fontSizeMin = TextSize.BestFitMin; mt.fontSizeMax = TextSize.Body; mt.textWrappingMode = TextWrappingModes.NoWrap; }
                parts.Medal = medal;
            }
            // 제목 — 줄의 밝은 바탕 위라 잉크색(T63 1항)
            // 줄 바탕이 프리팹 `ListFrame_08`(어두운 황갈색)이라 Ink 로는 안 읽힌다(screens run 148 눈 확인) → 흰 글자 + 외곽선
            var title = OnDark(UiKit.SetText(item, "Text (TMP)", q != null ? q.Label : QuestTitles[i], Palette.White, TextSize.Body));
            if (title != null)
            {
                var tr = title.rectTransform; UiKit.Pct(tr, Layout.QsRowTitle.WithH(Layout.LpLineH).Within(Layout.QsRow1));
                title.alignment = UiKit.TmpAlign(TextAnchor.MiddleLeft); title.name = "Title"; parts.Title = tr;
                // 글자 규격은 전 코드(UiKit.Label)와 같게 — 프리팹에서 온 Text 라 bestFit 설정이 데모 값(10~30)일 수 있다
                title.enableAutoSizing = true; title.fontSizeMin = TextSize.BestFitMin; title.fontSizeMax = TextSize.Body;
                title.textWrappingMode = TextWrappingModes.Normal; title.overflowMode = TextOverflowModes.Truncate;
            }
            // 진행바(프리팹 Slider_02_Yellow) — 자리·값·글자만
            var slider = item.GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                var sr = (RectTransform)slider.transform; sr.name = "Bar";
                UiKit.Pct(sr, Layout.QsRowBar.WithH(Layout.LpBarH).Within(Layout.QsRow1));
                slider.value = q != null ? Mathf.Clamp01(goal > 0 ? (float)q.Shown(have) / goal : 0f) : (done ? 1f : 0f);
                // T212 — 완료 줄만 채움을 «초록» 으로. 프리팹이 달고 온 노랑(`Slider_02_Yellow`)은 **미완** 에만 남긴다.
                // 조각을 갈아 끼우지 않는 까닭: `Slider_02_LightGreen` 은 같은 흰 조각(`Slider_02_BasePrefab` 의 `Fill`)을
                // rgb(130,215,60) 으로 tint 한 것뿐이라(두 프리팹의 차이는 `m_Color` 세 줄) **색 한 줄이면 그 프리팹과 같은 그림**이다.
                // 색은 우리 팔레트 `Palette.Green`(#85D048 = rgb(133,208,72)) — 그 프리팹 값과 눈으로 같은 초록이고,
                // «초록 = 열림/완료» 를 이미 쓰는 다른 자리(장비 세부 열린 줄)와 **한 곳에서** 나온다.
                if (done) { var fill = BarFill(slider); if (fill != null) fill.color = Palette.Green; }
                var st = sr.GetComponentInChildren<TMP_Text>(true);
                // 바 안 숫자는 UiKit.MakeBar 와 같은 규격(bestFit 32~40 · 가로 넘침 허용) — 바 칸(LpBarH 44px)이 40 한 줄(55px)보다 낮다
                if (st != null) { st.text = (q != null ? q.Shown(have) : (done ? goal : 0)) + "/" + goal; st.fontSize = TextSize.Body; st.enableAutoSizing = true; st.fontSizeMin = TextSize.BestFitMin; st.fontSizeMax = TextSize.Body; st.textWrappingMode = TextWrappingModes.NoWrap; OnDark(st); TextAudit.Mark(st, TextKind.Body); }
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
                // T318 — «이동» 은 닫고 **표의 목적지로 간다**(`app.Hint(go)` = `QuestGo.Open` · 그 화면의 버튼에 손가락 힌트).
                //   갈 데가 없는 줄(로그인류 · 표의 `go` 가 명시적 null)은 **눌리지 않는다**(지시서 1항 «버튼은 ✓ 나 비활성» · 눌리는데 아무 일도 안 나는 자리 금지 · 결정 771).
                //   표가 없는 껍데기(q == null)는 종전 그대로 «닫기만» 이다.
                var goTo = q != null ? q.Go : null;
                var go = UiKit.Button(item, "ui.btnOrange", "이동", () => { ov.Close(); if (goTo != null) app.Hint(goTo); }, Layout.QsRowGo.Within(Layout.QsRow1));
                go.name = "GoBtn"; parts.Go = go;
                if (q != null && goTo == null) UiKit.SetInteractable(go.GetComponent<Button>(), false);
            }
            // T69 — 줄 바탕과 보상 칸에 «검은 아웃라인»(레퍼런스 15 도 줄·메달이 검은 외곽선)
            UiKit.Bordered(frame);
            if (parts.Medal != null) UiKit.Bordered(parts.Medal, UiKit.BorderKeySmall);
            return parts;
        }

        // ───────────────────────── 16 출석 ─────────────────────────
        /// <summary>
        /// T253 4항 — 보상 이름 → 칸의 «틀 색 + 아이콘»(표는 게임 쪽 이름만 적고 짝짓기는 화면 몫이다 · <c>arena.json</c>·<c>dungeon.json</c> 과 같은 갈래).
        /// <b>새 그림 0</b> — 전부 이미 카탈로그에 있는 키다. 모르는 이름이면 코인으로 둔다(아이콘 때문에 칸이 안 뜨는 일은 없게).
        /// </summary>
        static void AttendArt(string item, out string color, out string icon)
        {
            if (item == Core.Mail.ItemGem) { color = "plum"; icon = "ui.gemRed"; return; }
            if (item == Core.Mail.ItemRevive) { color = "blue"; icon = "ui.iconRevive"; return; }
            var keyIcon = Core.GachaKeys.Icon(item);
            if (keyIcon != null) { color = item == Core.GachaKeys.Purple ? "plum" : "blue"; icon = keyIcon; return; }
            color = "green"; icon = "ui.coin";
        }

        /// <summary>칸의 수량 글자 — «3,000» · «×2» 처럼 표의 값 그대로(지시서 T253 4항).</summary>
        static string AttendQtyText(double amount) => System.Math.Round(amount).ToString("#,0");

        static readonly string[] AttendIcons = { "ui.coin", "ui.potionRed", "ui.gemRed", "ui.bookBlue", "ui.coin", "ui.hourglass" };
        static readonly string[] AttendColors = { "green", "blue", "plum", "green", "green", "plum" };
        /// <summary>하루 칸 보상 수량(껍데기 · 레퍼런스 16 의 숫자를 베끼지 않고 «1» 로 통일 — T44 «숫자는 표시만»).</summary>
        const string AttendQty = "1";

        /// <summary>
        /// T305 — <b>받은 칸의 바탕</b>(주인 2026-09-09 09:0X «받은 다음에는 해당 칸 꺼매지면서 … 지금 거의 구분이 안 감»).
        /// <c>docs/ref/16_attendance.jpg</c> 실측 = 받은 칸 (97,91,79) ↔ 오늘 칸 (237,222,189) — 즉 <b>휘도가 3분의 1 이하</b>로 떨어진다.
        /// 프리팹의 <c>Bg_Disable</c> 을 켜기만 하던 것이 «거의 구분이 안 가던» 자리라, 그 조각에 <b>레퍼런스에서 온 색을 실제로 칠한다</b>.
        /// </summary>
        static Color ClaimedBg => Palette.Hex("#615B4F");
        /// <summary>
        /// T305 — 받은 칸 안의 그림을 누르는 배수. 레퍼런스에서 받은 칸 아이콘 (102,106,89) ↔ 안 받은 칸 아이콘 (255,255,207) 을 채널마다 나누면
        /// 0.40 · 0.42 · 0.43 이라 <b>가운데 값</b>을 쓴다. <b>색을 갈아치우지 않고 곱하는</b> 까닭 = 무엇을 받았는지(금·다이아·물약)는 여전히 보여야 한다.
        /// </summary>
        const float ClaimedDark = 0.42f;
        /// <summary>T305 — 받은 표시 ✅ 의 초록. 시즌 패스가 이미 쓰는 그 값이다(두 화면의 «받았다» 가 다른 초록이면 그것이 더 이상하다).</summary>
        static Color ClaimedCheck => Palette.Hex("#3FD214");
        /// <summary>T305 — ✅ 가 재화 칸에서 먹는 자리(칸의 64% · <c>SeasonPassScreen</c> 과 같은 수). 주인 말 «재화 부분에 체크 표시».</summary>
        static readonly Layout.R ClaimedCheckRect = new Layout.R(18, 18, 64, 64);

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
            tc.name = "TapToClose"; tc.fontStyle = FontStyles.Bold; UiKit.Pct(tc.rectTransform, Layout.BookClose);

            // T253 4항 — 칸의 아이콘·수량·상태가 전부 표(attendance.json)에서 온다. 표가 없으면(로드 실패) 종전 껍데기 그대로다.
            var AT = app.Data != null ? app.Data.Attendance : null;
            int todayNo = AT != null ? Core.Attendance.Today(app.Save, AT, SaveStore.Today()) : 1;

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
                if (rt != null) { rt.enableAutoSizing = true; rt.fontSizeMin = TextSize.BestFitMin; rt.fontSizeMax = TextSize.Title; RibbonTextFit(rt); }
                // T320 ⓑ — 퀘스트와 같은 자리(이 팝업도 제 프리팹으로 선다).
                Overlay.RibbonGlowOn(box, (RectTransform)rib);
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
                    int no = i + 1;
                    var frame = (RectTransform)group.GetChild(i); frame.name = "Day:" + no; frame.gameObject.SetActive(true);
                    bool got = AT != null && Core.Attendance.Claimed(app.Save, no);
                    DayFrame(frame, no == todayNo, got);
                    var head = Head(frame, Layout.AtCell, Layout.AtCellHead, no + "일차", HeadBand);
                    var day = AT != null ? AT.Of(no) : null;
                    string color = AttendColors[i], icon = AttendIcons[i], qty = AttendQty;
                    if (day != null && day.Rewards.Count > 0) { AttendArt(day.Rewards[0].Item, out color, out icon); qty = AttendQtyText(day.Rewards[0].Amount); }
                    var ic = Cell(frame, Layout.AtCell, Layout.AtCellIcon, color, icon, qty, qtyBand: true);
                    ClaimedMark(ic, got);   // T305 — 받은 날이면 칸을 누르고 ✅ 를 얹는다(칸을 만든 뒤에)
                    UiKit.Clickable(frame, () => ClaimAttendance(app));
                    cells[i] = frame; if (i == 0) { head0 = head.transform.parent as RectTransform; icon0 = ic; }
                }
            }

            // 7일차 넓은 칸 = 프리팹의 Popup 직계 DailyFrame_01_l(격자 밖 조각)
            var day7 = ChildStarting(box, "DailyFrame_01_l"); RectTransform head7 = null; var r7 = new RectTransform[2];
            if (day7 != null)
            {
                day7.name = "Day:7"; UiKit.Pct(day7, Layout.AtDay7.Within(B));
                bool got7 = AT != null && Core.Attendance.Claimed(app.Save, 7);
                DayFrame(day7, todayNo == 7, got7);
                head7 = Head(day7, Layout.AtDay7, Layout.AtDay7Head, "7일차", HeadBand, "Head7").transform.parent as RectTransform;
                var d7 = AT != null ? AT.Of(7) : null;
                string c70 = "green", i70 = "ui.coin", q70 = AttendQty, c71 = "plum", i71 = "ui.gemRed", q71 = AttendQty;
                if (d7 != null && d7.Rewards.Count > 0) { AttendArt(d7.Rewards[0].Item, out c70, out i70); q70 = AttendQtyText(d7.Rewards[0].Amount); }
                if (d7 != null && d7.Rewards.Count > 1) { AttendArt(d7.Rewards[1].Item, out c71, out i71); q71 = AttendQtyText(d7.Rewards[1].Amount); }
                r7[0] = Cell(day7, Layout.AtDay7, Layout.AtDay7Cell, c70, i70, q70, qtyBand: true);
                r7[1] = Cell(day7, Layout.AtDay7, Sh(Layout.AtDay7Cell, Layout.AtDay7Pitch, 0), c71, i71, q71, qtyBand: true);
                ClaimedMark(r7[0], got7); ClaimedMark(r7[1], got7);   // T305 — 7일 칸은 재화가 둘이라 ✅ 도 둘이다(주인 «재화 부분에»)
                UiKit.Clickable(day7, () => ClaimAttendance(app));
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
        static void DayFrame(RectTransform frame, bool today, bool claimed = false)
        {
            // T253 4항 — 받은 날은 바탕을 «다 쓴» 것으로, 오늘 받을 칸은 Bg_Focus1 로 강조한다.
            UiKit.Show(frame, "Bg_Normal", !today && !claimed); UiKit.Show(frame, "Bg_Focus1", today);
            UiKit.Hide(frame, "Bg_Focus2", "Bg_Focus3", "SampleEffect", "SampleParticle", "Icon", "Text_Num", "Text_Day");
            // T305 — 「켜기만」 하던 것을 「칠한다」. 주인이 «거의 구분이 안 감» 이라고 한 자리가 정확히 여기다:
            //   조각의 Bg_Disable 은 제 색이 옅어 크림 바탕과 거의 같아 보였다. 레퍼런스 16 의 받은 칸은 (97,91,79) 로
            //   오늘 칸(237,222,189)보다 휘도가 3분의 1 이하다 — 그 값을 실제로 입힌다.
            UiKit.Show(frame, "Bg_Disable", claimed);
            if (claimed) Paint(frame, "Bg_Disable", ClaimedBg);
            // 프리팹의 작은 Check 는 안 쓴다 — 주인 말은 «재화 부분에» 이고, 그 조각은 칸 구석에 작게 붙어 그림에 먹힌다.
            // ✅ 는 ClaimedMark 가 보상 칸 위에 크게 얹는다(부르는 쪽이 칸을 만든 «뒤» 에 부른다).
            // ⚠ 끄기만 하지 않고 **이름도 바꾼다** — 이름이 «Check» 인 조각이 둘이면 이름으로 찾는 자·하니스가
            //    어느 쪽을 보는지 알 수 없고, 하필 꺼진 쪽을 집으면 «✅ 가 없다» 는 거짓 판정이 난다(결정 906 이 값을 치른 자리).
            var oldCheck = UiKit.Find(frame, "Check");
            if (oldCheck != null) { oldCheck.gameObject.SetActive(false); oldCheck.name = "Check_Prefab"; }
            foreach (var deco in frame.GetComponentsInChildren<Transform>(true)) if (deco.name == "Deco") deco.gameObject.SetActive(false);
            UiKit.Bordered(frame);   // T69 — 칸 테두리(레퍼런스 16 도 칸마다 검은 외곽선)
        }

        /// <summary>조각 하나의 그림 색을 바꾼다 — 없으면 아무 일도 안 한다(조각이 빠진 프리팹에서도 화면이 안 깨지게).</summary>
        static void Paint(Transform root, string path, Color c)
        {
            var im = Ink(UiKit.Find(root, path)); if (im != null) im.color = c;
        }

        /// <summary>
        /// T305 회차 2 — <b>상태 바탕(<c>Bg_Normal</c>·<c>Bg_Focus1</c>·<c>Bg_Disable</c>)은 «껍데기» 이고 그림은 그 안 <c>Bg</c> 가 갖는다.</b>
        /// <para>
        /// 프리팹 실측(<c>DailyFrame_01_BasePrefab</c>): <c>Bg_Disable</c> 의 컴포넌트는 <b>RectTransform 하나뿐</b>이고 자식이 <c>Bg</c>·<c>Border</c> 다.
        /// 그래서 그 조각에서 <c>GetComponent&lt;Image&gt;()</c> 를 물으면 <b>null</b> 이고, 칠하는 쪽은 «아무 일도 안 하고» 조용히 지나간다 —
        /// 주인이 «받은 칸이 거의 구분이 안 감» 이라 한 뒤 1회차가 «칠했다» 고 적었지만 <b>실제로는 한 픽셀도 안 칠해졌다</b>(run 816 이 그것을 잡았다).
        /// </para>
        /// <b>채움을 고른다 — 테두리가 아니라</b>: 이름이 <c>Bg</c> 인 자식이 있으면 그것, 없으면 제 것, 그것도 없으면 처음 만나는 그림.
        /// <para>자(<c>AttendancePlayTests</c>)도 <b>이 함수로 같은 잉크를 읽는다</b> — 칠하는 쪽과 재는 쪽이 다른 곳을 보면 자가 초록인 채로 화면만 틀린다(결정 858).</para>
        /// </summary>
        public static Image Ink(Transform t)
        {
            if (t == null) return null;
            var fill = UiKit.Find(t, "Bg");
            var im = fill != null ? fill.GetComponent<Image>() : null;
            if (im != null) return im;
            im = t.GetComponent<Image>();
            return im != null ? im : t.GetComponentInChildren<Image>(true);
        }

        /// <summary>
        /// T305 — <b>받은 날의 보상 칸 표시</b>(주인 «재화 부분에 체크 표시 되면서 수령 이미 됐다는 표시»).
        /// <para>
        /// 칸 안의 그림을 <see cref="ClaimedDark"/> 로 <b>곱해서</b> 누르고(색을 갈아치우지 않으므로 무엇을 받았는지는 그대로 보인다)
        /// 그 위에 큰 ✅ 를 얹는다. <b>누르는 것이 먼저</b>여야 ✅ 자신이 같이 어두워지지 않는다.
        /// </para>
        /// <para>
        /// ⚠ 이 함수는 보상 칸을 <b>만든 뒤</b>에 부른다 — <see cref="DayFrame"/> 은 칸이 생기기 전에 도므로 거기서는 아이콘을 못 만진다.
        /// </para>
        /// </summary>
        static void ClaimedMark(RectTransform cell, bool claimed)
        {
            if (cell == null || !claimed) return;
            foreach (var im in cell.GetComponentsInChildren<Image>(true))
            {
                if (im == null) continue;
                var c = im.color; im.color = new Color(c.r * ClaimedDark, c.g * ClaimedDark, c.b * ClaimedDark, c.a);
            }
            var ck = UiKit.Icon(cell, "Check", "pi.check", ClaimedCheck);
            UiKit.Pct(ck.rectTransform, ClaimedCheckRect);
            ck.transform.SetAsLastSibling();
        }

        /// <summary>
        /// T253 — 출석 칸을 누르면 <b>오늘 칸을 받는다</b>(주인 2항 «지급 = 즉시» · 우편함으로 안 간다).
        /// 규칙·지급은 <see cref="Core.Attendance"/> 한 곳이고, 받은 것은 <b>T241 공통 리워드 팝업</b>으로 보여 준 뒤 출석 팝업을 다시 그린다.
        /// <para>못 받으면 까닭을 토스트로 알린다 — 문구는 <see cref="Core.Attendance.Why"/> 가 갖는다(화면이 다시 짓지 않는다).</para>
        /// ⚠ <b>어느 칸을 눌러도 «오늘 칸» 을 받는다</b> — 표의 순서가 곧 차례라 «3일차만 골라 받기» 같은 것은 없다(주인이 말한 적도 없다).
        /// </summary>
        static void ClaimAttendance(App app)
        {
            var AT = app.Data != null ? app.Data.Attendance : null;
            if (AT == null) return;                                  // 표가 없으면 종전 껍데기 — 아무 일도 안 한다
            string today = SaveStore.Today();
            string why = Core.Attendance.Why(app.Save, AT, today);
            if (why.Length > 0) { app.Toast(why); return; }
            var got = Core.Attendance.Claim(app.Save, AT, today);
            if (got == null) return;
            Quests.Ach(app, Quests.AchAttendClaim);   // T258 — 업적 «출석 보상 1회 수령»(«출석 1회» 는 접속 자체라 App.Create 쪽이다)
            app.Persist(); app.Current?.Refresh();
            var items = new List<RewardPopup.Item>();
            foreach (var r in got.Rewards) { AttendArt(r.Item, out _, out string icon); items.Add(RewardPopup.Item.Of(icon, AttendQtyText(r.Amount))); }
            RewardPopup.Show(items, () => Attendance(app));           // 닫으면 출석 팝업을 다시 그린다(✅ 가 붙은 채로)
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
            if (st == GiftBtn.Locked) LockLook(b);
            return b;
        }

        /// <summary>«잠금» 버튼 판 색 — 흰 글자가 뜨는 어두운 판(<see cref="Palette.Ink"/> 계열 · <see cref="LockLook"/> 설명 참조).</summary>
        public static Color LockedPlate => Palette.Ink;

        /// <summary>
        /// T186 ⓓ — «잠금»(비활성) 버튼을 <b>알파로 흐리게</b> 하지 않고 <b>어두운 판 + 흰 글자</b>로 만든다.
        /// <para>
        /// 실측(`screens` run 333 의 17): 판 #B49B8E <b>0.63</b> · 글자 #CEC8C6 <b>0.79</b> = 대비 <b>0.16</b>.
        /// 원인은 색이 아니라 <b>흐리게 하는 방식</b>이다 — <c>CanvasGroup</c> α 0.5(+ 유니티 <c>disabledColor</c> α 0.5)는 판과 글자를
        /// <b>같은 비율로</b> 바탕 쪽으로 끌어당기므로 <b>둘의 차이도 그만큼 줄어든다</b>(α 를 곱한 만큼 대비가 곱해진다).
        /// 즉 회색 조각 위 흰 글자는 α 를 어떻게 잡아도 0.35 를 못 넘는다 — 판 자체를 어둡게 해야 한다.
        /// </para>
        /// 그래서 ⓘ <c>interactable</c> 은 <b>false 그대로</b>(누름 차단) ⓙ 전이는 <b>ColorTint 그대로 둔다</b>
        /// (<see cref="UiKit.PressColors"/> 의 <c>disabledColor</c> 가 <b>흰색</b>이라 비활성이어도 판 색이 안 흐려진다 ·
        /// «모든 버튼은 ColorTint» 는 <c>PressFeedbackTests</c> 의 계약이라 <c>None</c> 으로 바꾸면 그 자가 빨개진다)
        /// ⓚ 판(<see cref="PlateOf"/>)을 <see cref="LockedPlate"/> 로 tint. 글자는 T63·T111 그대로 흰색 + 검은 아웃라인이고
        /// «잠긴 줄» 이라는 뜻은 «잠금» 글자와 보상 칸의 자물쇠가 낸다.
        /// </summary>
        static void LockLook(RectTransform b)
        {
            if (b == null) return;
            var btn = b.GetComponent<Button>(); if (btn != null) btn.interactable = false;
            var plate = PlateOf(b); if (plate != null) plate.color = LockedPlate;
        }

        /// <summary>
        /// 버튼의 «보이는 판» — <see cref="UiKit.Clickable"/> 이 루트에 붙이는 것은 <b>투명 히트 영역</b>이고 실제 그림은 조각의 자식(«Bg»)이다.
        /// 그 조각이 곧 <c>targetGraphic</c>(<see cref="UiKit.PressTarget"/> 이 «보이는 첫 자식» 으로 고른 것)이라 그것을 먼저 쓰고,
        /// 없으면 알파가 있는 첫 자식 Image 로 내려간다. <b>게이트도 이 함수로 판을 찾는다</b> — 코드와 자가 서로 다른 조각을 보면 판정이 어긋난다.
        /// </summary>
        public static Image PlateOf(RectTransform b)
        {
            if (b == null) return null;
            var btn = b.GetComponent<Button>();
            var tg = btn != null ? btn.targetGraphic as Image : null;
            if (tg != null && tg.color.a > 0.01f) return tg;
            foreach (var img in b.GetComponentsInChildren<Image>(true))
                if (img.transform != b && img.color.a > 0.01f) return img;
            return tg;
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
            DarkenGiftBoxBody(box);   // T186 ⓐ — 이 화면만 상자가 크림으로 남아 있었다(T130 은 «ui.popup» 만 어둡게 한다)
            var rib = Ribbon(box, "ui.title.yellow", Layout.GfRibbon, B);
            // T343(주인 2026-09-10 «선물상자 이미지가 라이트보다 뒤에 있네 이거 수정해») — 그림을 **맨 위**로 올린다.
            //   여태는 `SetSiblingIndex(1)`(«어둠 위 · 상자 아래»)였는데, 주인이 말한 «라이트» 는 제목 리본 뒤 빛(`TitleGlow`)이고
            //   그것은 **상자의 자식**이다. **상자보다 아래에 있는 그림은 형제 번호를 어떻게 만져도 그 빛 뒤다** — 자식은 늘 제 부모와 함께 올라간다.
            //   ⚠ 그림을 상자 «안» 으로 옮기는 길은 안 쓴다 — `Layout.GfPic` 은 **화면(ov.Root) 백분율**이라 부모를 바꾸면 자리가 통째로 어긋난다.
            var pic = UiKit.Icon(ov.Root, "GiftPic", "ui.gift"); UiKit.Pct(pic.rectTransform, Layout.GfPic); pic.transform.SetAsLastSibling();
            var timer = TimerRow(box, B, Layout.GfTimer, GiftEndsIn());
            var timerTxt = timer.GetComponentInChildren<TMP_Text>(true);
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

        /// <summary>
        /// T186 ⓐ — 데일리 기프트 상자 <b>몸통만</b> 레퍼런스대로 어둡게(<see cref="Palette.PopupBox"/>).
        /// <para>
        /// T130 이 공통 팝업을 어둡게 했지만 그 손질은 «색을 안 쓰는» 키(<see cref="UiKit.PopupKeyPlain"/>)에만 닿는다 —
        /// 17 은 <c>ui.popup.yellow</c> 로 제 상자를 세우는 화면이라 혼자 크림으로 남았다(`screens` run 333 실측 휘도 <b>0.92</b> · 같은 런 07 은 0.353 · 16 은 0.212).
        /// 그 탓에 «⏱ 종료까지 …» 흰 글자가 대비 <b>0.08</b> 로 안 읽혔다(T186 ⓑ 는 이 한 줄로 같이 풀린다).
        /// </para>
        /// <b>«DecoLine» 은 건드리지 않는다</b> — 이 조각에서 그 선이 곧 <b>금색 테</b>(조각 변형이 유일하게 덮어쓴 색 #F7AE29)이고
        /// 레퍼런스 17 도 «어두운 몸통 + 금색 테» 다. T130 이 공통 팝업에서 DecoLine 까지 어둡게 한 것은 그쪽 선이 살구색이라서다.
        /// </summary>
        static void DarkenGiftBoxBody(RectTransform box)
        {
            if (box == null) return;
            for (int i = 0; i < box.childCount; i++)
            {
                var c = box.GetChild(i); if (c.name != UiKit.CardBodyName) continue;   // 조각의 몸통 자식 이름 = «Bg»(T130·T135 와 같은 이름)
                var img = c.GetComponent<Image>(); if (img != null) img.color = Palette.PopupBox;
            }
        }

        /// <summary>«오늘의 선물» 칸 + 광고 줄 N개를 <paramref name="host"/> 에 그린다(상태가 바뀌면 <paramref name="refresh"/> 로 이 안만 다시 그린다).</summary>
        /// <summary>
        /// 보상 이름 → 칸의 «틀 + 아이콘»(T254 · 표는 재화 이름만 적고 아이콘 짝짓기는 화면 몫이다 — <c>EventsScreen.RewardArt</c> 와 같은 규약).
        /// <para>모르는 이름도 그림 하나는 준다 — 아이콘이 없어서 칸이 통째로 안 뜨는 일은 만들지 않는다.</para>
        /// </summary>
        static void GiftArt(string item, out string frame, out string icon)
        {
            if (item == Core.Mail.ItemGem) { frame = "plum"; icon = "ui.gemRed"; return; }
            if (item == Core.Mail.ItemGold) { frame = "green"; icon = "ui.coin"; return; }
            if (item == Core.Mail.ItemRevive) { frame = "plum"; icon = "ui.iconRevive"; return; }
            var k = Core.GachaKeys.Icon(item);
            if (k != null) { frame = item == Core.GachaKeys.Purple ? "plum" : "green"; icon = k; return; }   // 키 3종·펫알 = T255 가 아는 그림
            frame = "green"; icon = "ui.iconArenaCoin";
        }

        static void BuildGiftRows(App app, RectTransform host, Layout.R B, DailyGiftData D, string today, Action refresh)
        {
            var S = app.Save; var ov = app.Overlay;
            // ── «오늘의 선물»(무료 1칸 · 주인 확정 «무료 1칸 = 다이아 100» → dailyGift.json freeGift.gem)
            var T = Layout.GfTodayCell;
            var todayCell = UiKit.Panel(host, "Today", "fr.r12", Palette.A(Palette.Sky, 0.55f)); UiKit.Pct(todayCell.rectTransform, T.Within(B));
            UiKit.Bordered(todayCell.rectTransform);   // T69 — 칸 «검은 아웃라인»
            var th = Head(host, B, new Layout.R(T.X, T.Y, T.W, 2.4f), "오늘의 선물", Palette.A(Palette.Dim, 0.5f), "TodayHead");
            th.alignment = UiKit.TmpAlign(TextAnchor.MiddleLeft); UiKit.Pct(th.rectTransform, 8, 0, 90, 100);
            var gi = UiKit.Icon(th.transform.parent, "Icon", "pi.gift", Palette.Yellow); UiKit.Pct(gi.rectTransform, 1.5f, 10, 5, 80);
            double freeAmt = D != null ? D.FreeAmount : 0;
            string freeItem = D != null ? D.FreeItem : Core.Mail.ItemGem;
            GiftArt(freeItem, out var freeFrame, out var freeIcon);   // T254 — 무료 칸도 표가 정한 것을 그린다(늘 다이아가 아니다)
            Cell(host, B, new Layout.R(T.X + 1.6f, T.Y + 3.0f, 8.2f, 4.5f), freeFrame, freeIcon, qty: freeAmt > 0 ? UiKit.FmtQty(freeAmt) : null, name: "TodayCell");
            bool canFree = D != null && Core.DailyGift.CanFree(S, D, today);
            GiftButton(host, B, Layout.GfTodayBtn, "TodayGetBtn", canFree ? GiftBtn.Claim : GiftBtn.Done, () =>
            {
                double g = Core.DailyGift.ClaimFree(S, D, today);
                if (g <= 0) return;
                Quests.Ach(app, Quests.AchGiftClaim);   // T258 — 업적 «데일리 기프트 5회 수령»(무료 칸도 수령이다)
                app.Persist(); app.Current?.Refresh();
                // T241 — 받은 것은 공통 «리워드» 팝업이 보여 준다(토스트 대신 · 지시서 2항 «각 화면이 제 나름의 토스트·팝업을 따로 만들지 않는다»).
                // 닫으면 이 팝업을 다시 연다 — 아래 «광고 보기» 길이 이미 쓰는 그 꼴(`DailyGift(app)`)이라 흐름이 하나로 모인다.
                RewardPopup.Show(new List<RewardPopup.Item> { RewardPopup.Item.Of(freeIcon, UiKit.FmtQty(g)) }, () => DailyGift(app));
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
                GiftArt(m.Item, out var mFrame, out var mIcon);   // T254 — 줄마다 다른 것을 준다(펫알·보라 키·부활권·다이아)
                var reward = Cell(host, B, Sh(Layout.GfRowReward, 0, dy), mFrame, mIcon, qty: UiKit.FmtQty(m.Amount), locked: locked, name: "Reward:" + i);
                var st = claimed ? GiftBtn.Done : locked ? GiftBtn.Locked : canClaim ? GiftBtn.Claim : GiftBtn.Ad;
                int idx = i;
                var btn = GiftButton(host, B, Sh(Layout.GfRowBtn, 0, dy), "AdBtn", st, () =>
                {
                    if (st == GiftBtn.Claim)
                    {
                        double g = Core.DailyGift.Claim(S, D, idx, today);
                        if (g <= 0) return;
                        Quests.Ach(app, Quests.AchGiftClaim);   // T258 — 위 무료 칸과 같은 사건이다(줄이 다르다고 다른 업적이 아니다)
                        app.Persist(); app.Current?.Refresh();
                        RewardPopup.Show(new List<RewardPopup.Item> { RewardPopup.Item.Of(mIcon, UiKit.FmtQty(g)) }, () => DailyGift(app));   // T241 · 아이콘은 그 줄이 실제로 준 것(T254)
                    }
                    else   // 광고 보기 — 실제 광고 SDK 없음: T23 과 같은 모의 카운트다운 3초 뒤 누적 +1 (팝업을 다시 연다)
                    {
                        // T258 — 업적 «광고 10회 시청» 은 **광고를 부르는 자리마다** 센다. `Overlay.AdCountdown` 안에 걸면
                        //   나중에 누가 광고를 퀘스트로도 세는 날 두 번 세는 덫이 된다(워커 A 가 썼다가 되돌린 자리 · 결정 763).
                        ov.AdCountdown(GiftAdSeconds, () => { Core.DailyGift.WatchAd(S, D, today); Quests.Ach(app, Quests.AchAdWatch); app.Persist(); app.Current?.Refresh(); DailyGift(app); });
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

        // T241 — 여기 있던 `PopReward`(받은 칸이 «팝» 하고 커졌다 돌아오는 연출)를 **지웠다**: 부르던 세 자리가 전부
        // 공통 «리워드» 팝업(`RewardPopup.Show`)으로 갈아 끼워져 부르는 곳이 0 이 됐다. 받은 것을 알리는 연출은 이제
        // 그 팝업의 칸 stagger 가 맡는다(T232 가 밟은 «부르는 코드가 사라진 자리는 남기지 않는다» 와 같은 정리).

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

        /// <summary>«h:mm:ss»(T265 충전 카운트다운 — 3시간짜리라 <see cref="Mmss"/> 의 «mm:ss» 로는 «180:00» 이 된다).</summary>
        static string Hhmmss(double sec)
        {
            var t = TimeSpan.FromSeconds(sec < 0 ? 0 : Math.Ceiling(sec));
            return $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
        }

        /// <summary>회색 제목 명판(레퍼런스 30·31 은 리본이 아니라 상자 폭을 채우는 띠) — 조각 <c>fr.rect</c> + 가운데 제목 글자.</summary>
        static RectTransform Plate(Transform parent, Layout.R parentR, Layout.R r, string title, string name = "Plate")
        {
            var p = UiKit.Panel(parent, name, "fr.rect", Palette.A(Palette.Slate, 0.85f)); var rt = p.rectTransform;
            UiKit.Pct(rt, r.Within(parentR)); UiKit.Bordered(rt);
            var t = UiKit.Label(rt, 6, 0, 88, 100, title, TextSize.Title, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Title);
            t.fontStyle = FontStyles.Bold;
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
        /// <summary>
        /// T146 ⓑ — 그림 띠에 세우는 두 캐릭터의 자리(띠 안 %)와 적 외형. 레퍼런스 <c>docs/ref/30_expedition.jpg</c>(720×1560) 실측을 띠 사각형(x 32~690 · y 305~565)의 %로 옮긴 값이다:
        /// 기사 x 38.4~49.8 · y 38.5~86.5 · 적 x 58.2~69.6 · y 51.9~84.6 — 둘의 발이 길(<c>Road</c> 62% 아래) 위 같은 높이에 선다.
        /// 적 외형은 전투의 «곤봉 적»(<see cref="BattleWorld"/> 의 <c>EnemySkin</c> 갈래 1)과 같은 조각이라 새 그림 0이다.
        /// </summary>
        // 탐험 무대의 기사·적 칸(무대 안 %) — T303 ⓑ. 값은 **레퍼런스 30 실측**에서 셈해 나온다(결정 891 의 표 · 아래 셈).
        //   ⓐ 회차가 `HeroView` 그림판을 정사각으로 만들었으므로 **폭은 높이가 정한다** — 여기 `w` 는 칸의 자리만 잡고,
        //   실제로 그려지는 그림판은 «높이 × 높이» 다(그래서 칸보다 옆으로 넓게 나온다 · 여백은 투명이다).
        //   ⚑ 그림은 칸을 가득 채우지 않는다 — 실측하면 세로의 **66.0%** 만 쓰고 아래에 **21.5%** 의 투명 여백이 남는다.
        //     그래서 «레퍼런스처럼 키우고 땅에 세우는» 셈이 두 줄이다:
        //       칸 높이 = 레퍼런스 키 44.9% ÷ 0.660 = **68.0%**
        //       칸 바닥 = 레퍼런스 발밑 83.5% + 0.215 × 68.0 = **98.1%** → y = 98.1 − 68.0 = **30.1%**
        //     적은 레퍼런스 실측이 뒤 나무와 겹쳐 ±3%p 라, **기사와 같은 배율(×1.4167)** 로 키우고 발밑만 83.5% 에 맞췄다
        //     (h 32.7 → 46.3 · y = 83.5 + 0.215 × 46.3 − 46.3 = 47.2). 두 그림의 발이 **같은 줄**에 선다.
        //   ⚑ 중심 x 는 안 건드렸다 — 기사 44.1% ↔ 레퍼런스 45.9% · 적 63.9% ↔ 63.5% 로 이미 맞다(결정 891).
        //     키우면 그림 폭도 늘지만 기사 37.1~50.7% · 적 60.0~67.8% 로 **9.3%p 떨어져** 안 겹친다(레퍼런스는 3.6%p).
        static readonly Layout.R ExKnight = new Layout.R(38.4f, 30.1f, 11.4f, 68.0f), ExFoe = new Layout.R(58.2f, 47.2f, 11.4f, 46.3f);

        static RectTransform Picture(Transform parent, Layout.R parentR, Layout.R r, App app = null)
        {
            var pic = UiKit.Rect(parent, "Picture"); UiKit.Pct(pic, r.Within(parentR));
            pic.gameObject.AddComponent<RectMask2D>();
            var field = UiKit.Icon(pic, "Field", "env.field"); field.preserveAspect = false; UiKit.Stretch(field.rectTransform);
            var road = UiKit.Icon(pic, "Road", "env.road"); road.preserveAspect = false; UiKit.Pct(road.rectTransform, 0, 62, 100, 38);
            var edge = UiKit.Icon(pic, "RoadUp", "env.roadUp"); edge.preserveAspect = false; UiKit.Pct(edge.rectTransform, 0, 57, 100, 8);
            for (int i = 0; i < 3; i++) { var t = UiKit.Icon(pic, "Tree" + i, "env.tree"); UiKit.Pct(t.rectTransform, 6 + i * 34, 12, 16, 46); }
            var bush = UiKit.Icon(pic, "Bush", "env.bush"); UiKit.Pct(bush.rectTransform, 78, 66, 12, 22);
            // T146 ⓑ — 레퍼런스 30 의 띠에는 기사와 적이 길 위를 걸어간다. 우리 띠는 나무·길뿐이었다(screens run 257 실측).
            // 조각은 이미 있는 것뿐이다 — 기사는 장착 외형(HeroView.PlayerSkin = 전투·장비 화면과 같은 표), 적은 전투의 «곤봉 적» 외형.
            if (app != null)
            {
                var kh = UiKit.Rect(pic, "Knight"); UiKit.Pct(kh, ExKnight);
                HeroView.Attach(kh, HeroView.PlayerSkin(app), 256);
                var fh = UiKit.Rect(pic, "Foe"); UiKit.Pct(fh, ExFoe);
                HeroView.Attach(fh, FoeSkin(), 256);
            }
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
            HideRibbon(box);   // T146 ⓐ — 레퍼런스 30 은 리본이 아니라 상자 폭 명판이다(«Title_01» 로 찾던 종전 줄은 이름이 달라 한 번도 안 맞았다 · 결정 388)
            var pic = Picture(box, B, Layout.ExPic, app);   // T146 ⓑ — app 을 주면 띠에 기사·적이 선다
            var plate = Plate(box, B, Layout.ExPlate, "탐험 보상");
            var info = UiKit.Icon(box, "InfoBtn", "pi.info", Palette.White); UiKit.Pct(info.rectTransform, Layout.ExInfoBtn.Within(B));
            var subR = Layout.ExSub.Within(B);
            // 레퍼런스 30 은 글자 구역이 «어두운 띠» 다 — 우리 상자(ui.popup)는 크림이라 흰 글자가 묻힌다(T63 0항 «밝은 글자 + 검은 아웃라인» 의 짝).
            // 부제~시간당 pill 을 한 장의 어두운 띠 위에 얹는다(자리·크기는 표 그대로 · 띠는 이름표를 달지 않아 채점 밖).
            var infoBand = UiKit.Panel(box, "InfoBand", "fr.r12", Palette.A(Palette.Dim, 0.55f));
            UiKit.Pct(infoBand.rectTransform, new Layout.R(Layout.ExSub.X, Layout.ExSub.Y - 0.6f, Layout.ExSub.W, (Layout.ExRatePill1.Y + Layout.ExRatePill1.H + 0.8f) - Layout.ExSub.Y).Within(B));
            UiKit.Bordered(infoBand.rectTransform);
            var sub = UiKit.Label(box, subR.X, subR.Y, subR.W, subR.H, "시간이 지나면 저절로 쌓입니다", TextSize.Aux, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Aux);
            sub.name = "Sub"; UiKit.Tag(sub.rectTransform, "부제");

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
            timeTxt.name = "ExpTime"; timeTxt.fontStyle = FontStyles.Bold;
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
            var capBand = UiKit.Panel(host, "CapBand", "fr.r12", Palette.A(Palette.Dim, 0.55f));
            UiKit.Pct(capBand.rectTransform, Layout.ExCapNote.Within(B)); UiKit.Bordered(capBand.rectTransform);
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
                    Quests.Bump(app, Quests.ExpeditionClaim);   // T257 4항 — 일일 «탐험 보상 받기» · 주간 «탐험 7번 보상 받기»(빠른 탐험은 주인이 다른 줄로 썼다 = 다른 counter)
                    app.Persist(); app.Current?.Refresh();
                    // T241 — 골드·다이아 두 칸을 공통 «리워드» 팝업이 보여 준다(토스트 대신) · 닫으면 탐험 팝업이 다시 뜬다(칸은 0 부터 다시 쌓인다)
                    RewardPopup.Show(new List<RewardPopup.Item>
                    {
                        RewardPopup.Item.Of("ui.coin", UiKit.Fmt(gg)),
                        RewardPopup.Item.Of("ui.gemRed", UiKit.FmtQty(mm)),
                    }, () => Expedition(app));
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
        /// 지급은 누적에 더하지 않는다(중복 수령 방지 · ROUTINE T97 4항) · <b>보유 충전을 하나 쓴다</b>(T265 — 하루 횟수가 아니다).
        /// </summary>
        public static void QuickExplore(App app, Action after)
        {
            var ov = app.Overlay; var B = Layout.QxBox; var S = app.Save; var G = app.Data;
            var D = G != null ? G.Expedition : null;
            string today = SaveStore.Today();
            double now = NowSec();
            var box = ov.OpenBox("ui.popup", "ui.title.green", "", B, () => { ov.Close(); if (after != null) { LobbyPopups.Expedition(app); } }); box.name = "QuickExploreBox";
            HideRibbon(box);   // T146 ⓐ — 31 도 30 과 같은 꼴(명판이 제목이다)
            var plate = Plate(box, B, Layout.QxPlate, "빠른 탐험", "QxPlate");
            var subR = Layout.QxSub.Within(B);
            var qxBand = UiKit.Panel(box, "QxBand", "fr.r12", Palette.A(Palette.Dim, 0.55f));
            UiKit.Pct(qxBand.rectTransform, new Layout.R(Layout.QxSub.X, Layout.QxSub.Y - 0.5f, Layout.QxSub.W, (Layout.QxTitle.Y + Layout.QxTitle.H + 0.5f) - Layout.QxSub.Y).Within(B));
            UiKit.Bordered(qxBand.rectTransform);
            var qsub = UiKit.Label(box, subR.X, subR.Y, subR.W, subR.H, "탐험 보상을 한 번에 받습니다", TextSize.Aux, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Aux);
            qsub.name = "QxSub"; UiKit.Tag(qsub.rectTransform, "부제");
            var ttR = Layout.QxTitle.Within(B);
            var tt = UiKit.Label(box, ttR.X, ttR.Y, ttR.W, ttR.H, "받을 보상", TextSize.Body, Palette.White, TextAnchor.MiddleCenter); tt.name = "QxTitle"; tt.fontStyle = FontStyles.Bold;
            UiKit.Tag(tt.rectTransform, "«받을 보상» 제목");

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
                        Quests.Bump(app, Quests.ExpeditionFastClaim);   // T257 4항 — 일일 «빠른 탐험 보상 받기» 만 센다(위 «받기» 와 한 줄로 묶지 않는다 · 결정 705 ⓒ) · T258 업적 `expeditionQuick` 도 이 줄이 잇는다
                        Quests.Ach(app, Quests.AchAdWatch);             // T258 — 이 길은 광고를 본 길이기도 하다(«광고 10회 시청»)
                        app.Persist();
                        app.Toast($"골드 +{UiKit.Fmt(gg)} · 다이아 +{UiKit.FmtQty(mm)}");
                        LobbyPopups.Expedition(app);   // 광고가 끝나면 탐험 팝업으로 돌아간다(남은 횟수·버튼이 갱신된다)
                    });
                }) : () => { }, Layout.QxFreeBtn.Within(B));
            fb.name = "QxFreeBtn";
            if (left <= 0) UiKit.SetInteractable(fb.GetComponent<Button>(), false); else BtnBadge(fb, left.ToString(), "QxBadge");

            // T265 — 주인 «빠른 탐험 팝업 내에 그렇게 써 주면 됨, 그런 정보».
            // 규칙과 «다음 충전까지» 를 한 줄로 적는다. 수는 전부 표에서 온다(코드에 3 을 안 박는다).
            var ruleR = Layout.QxRule.Within(B);
            string ruleTxt = "--";
            if (D != null)
            {
                double next = Core.Expedition.NextQuickSec(S, D, now);
                // T270 ⓐ — 주인 정정 «2시간마다 3개 전부 리필»(1개씩 충전이 아니다). 수는 전부 표에서 온다.
                ruleTxt = $"{UiKit.FmtQty(D.QuickChargeHours)}시간마다 {UiKit.FmtQty(D.QuickMax)}회 전부 충전 · "
                        + (next > 0 ? "다음 충전까지 " + Hhmmss(next) : "충전 완료");
            }
            var rule = UiKit.Label(box, ruleR.X, ruleR.Y, ruleR.W, ruleR.H, ruleTxt,
                TextSize.Aux, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Aux);
            rule.name = "QxRule";

            UiKit.Tag(box, "팝업 박스"); UiKit.Tag(plate, "제목 명판"); UiKit.Tag(gridBg.rectTransform, "보상 칸 바탕");
            UiKit.Tag(note.rectTransform, "안내 문구"); UiKit.Tag(fb, "광고 버튼"); UiKit.Tag(rule.rectTransform, "충전 규칙"); TagClose(app);
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
        /// <summary>
        /// T116 3단계 ⓑ — 특권 카드 4장의 그라데이션 이름(<see cref="GradientPalette"/> · 카탈로그 <c>col.grad.*</c> · 레퍼런스 11 실측).
        /// 카드마다 계열색이 달라 한 이름으로 묶을 수 없다(1 = 하늘 · 2 = 파랑 · 3 = 자주 · 4 = 주황→금). 문자열을 두 곳에 안 박는다(§1).
        /// </summary>
        public const string GradCard1 = "cardBlue", GradCard2 = "cardPrivAd", GradCard3 = "cardPrivMonth", GradCard4 = "cardPrivLife";

        static Layout.R Sh(Layout.R r, float dx, float dy) => new Layout.R(r.X + dx, r.Y + dy, r.W, r.H);

        /// <summary>
        /// T264 2단계 — 카드 넷의 <b>표 키</b>(화면 순서 그대로 · <c>privilege.json</c> 의 <c>cards[].key</c>).
        /// 맨 위가 주인이 말한 «데일리 기프트»(공짜 · 하루 다이아 30)이고 아래 셋이 구매형이다.
        /// ⚠ <b>로비 팝업의 «데일리 기프트»(17 · T254 의 5칸)와는 다른 것</b>이다 — 이름만 같고 세이브 칸도 따로다(ROUTINE §2 T264 1항).
        /// </summary>
        static readonly string[] CardKeys = { "dailyGift", "adRemove", "monthly", "lifetime" };

        // T264 2단계 — Refresh 가 글자·상태만 갈아 끼우려고 붙잡아 두는 것들(ChapterChestScreen 과 같은 문법).
        readonly SkinPair[] _cardBtn = new SkinPair[4];
        readonly TMP_Text[] _cardQty = new TMP_Text[4];
        readonly TMP_Text[] _cardState = new TMP_Text[4];
        SkinPair _claimAll;

        /// <summary>
        /// T306 — <b>주황·회색 두 벌을 같은 자리에 겹쳐 두고 한 벌만 켜는 버튼</b>(주인 2026-09-09 09:1X 특권 버튼 색 규칙).
        /// <para>
        /// <b>왜 옷을 «갈아입히지» 않나</b> — 버튼 스킨은 그림 한 장이 아니라 <c>ButtonGradient</c> 덧칠까지 딸려 오므로
        /// 런타임에 sprite 만 바꾸면 색이 두 곳에서 갈리고, 되돌릴 때 한쪽만 남는다(T289 가 상점 열쇠 옷에서 실측한 자리).
        /// 겹쳐 두면 두 벌이 각자 제 옷·제 덧칠을 갖고, <see cref="Set"/> 은 «어느 쪽을 켜나» 만 고른다.
        /// </para>
        /// <para>
        /// ⚠ <b>글자·눌림은 두 벌을 늘 같게 둔다</b> — 꺼진 쪽도 같이 맞춰야 «이름으로 버튼을 집는» 자·하니스가
        /// 어느 벌을 집든 같은 답을 본다(꺼진 벌만 낡으면 그 어긋남은 화면에 안 보여 아무도 못 찾는다).
        /// </para>
        /// <para>
        /// ⚑ <b>이 자리에 두는 까닭</b>: 공용으로 올릴 곳(<c>UiKit</c>)이 지금 남의 살아 있는 lock(T320) 안이다.
        /// 두 번째 쓸 곳(T305 출석 등)이 생기는 회차에 <c>UiKit.TwoSkinButton</c> 으로 올리면 된다 — 지금 올리면 남의 절과 부딪힌다.
        /// </para>
        /// </summary>
        sealed class SkinPair
        {
            public readonly RectTransform Warm;   // 주황 — «할 것이 있다»
            public readonly RectTransform Cold;   // 회색 — «할 것이 없다»
            public SkinPair(RectTransform warm, RectTransform cold) { Warm = warm; Cold = cold; }

            /// <summary>규칙이 준 «주황인가» 로 한 벌만 켠다. 글자·눌림은 두 벌 다 같은 값으로 둔다.</summary>
            public void Set(bool warm, string label, bool interactable)
            {
                Dress(Warm, label, interactable); Dress(Cold, label, interactable);
                if (Warm != null) Warm.gameObject.SetActive(warm);
                if (Cold != null) Cold.gameObject.SetActive(!warm);
            }
            static void Dress(RectTransform rt, string label, bool interactable)
            {
                if (rt == null) return;
                var t = UiKit.ButtonText(rt); if (t != null) t.text = TextGlyphs.Safe(label);
                var b = rt.GetComponent<UnityEngine.UI.Button>(); if (b != null) b.interactable = interactable;
            }
        }

        /// <summary>꺼져 있는 회색 벌의 오브젝트 이름 — 자·하니스가 «두 벌 중 어느 쪽이 켜져 있나» 를 물을 때 쓰는 손잡이다.</summary>
        public static string GrayName(string name) => name + "#Gray";

        /// <summary>두 벌을 같은 자리에 세운다 — 태어날 때는 주황만 보이고, 고르는 것은 <see cref="Refresh"/> 하나뿐이다.</summary>
        SkinPair TwoSkin(Transform parent, Layout.R rect, string label, System.Action onClick, string name)
        {
            var warm = UiKit.Button(parent, "ui.btnOrange", label, onClick, rect); warm.name = name;
            var cold = UiKit.Button(parent, "ui.btnGray", label, onClick, rect); cold.name = GrayName(name);
            cold.gameObject.SetActive(false);
            return new SkinPair(warm, cold);
        }

        PrivilegeData PD => App != null && App.Data != null ? App.Data.Privilege : null;
        PrivilegeData.Card CardOf(int i) { var d = PD; return d == null || i < 0 || i >= CardKeys.Length ? null : d.Of(CardKeys[i]); }

        /// <summary>설명 줄 가운데 «구매 즉시 보상» 을 적을 자리의 표식 — 표가 있으면 실제 수로 갈아 끼운다.</summary>
        const string BuyLine = "구매 시 💎 지급";

        /// <summary>«구매 시 다이아 2,400 지급» — 표가 없으면 종전 문구 그대로(껍데기라도 안 깨진다).</summary>
        static string BuyNowText(PrivilegeData.Card c)
        {
            if (c == null || c.BuyNow.Count == 0) return BuyLine;
            string s = "";
            foreach (var r in c.BuyNow)
            {
                if (s.Length > 0) s += " · ";
                s += Mail.Name(r.Item) + " " + System.Math.Round(r.Amount).ToString("#,0");
            }
            return "구매 시 " + s;
        }

        /// <summary>그 카드가 매일 주는 다이아(표가 없거나 카드가 없으면 0) — 보상 칸 수량이 이 값이다(코드에 안 박는다).</summary>
        static double DailyGem(PrivilegeData.Card c)
        {
            if (c == null) return 0;
            foreach (var r in c.Daily) if (r.Item == Mail.ItemGem) return r.Amount;
            return c.Daily.Count > 0 ? c.Daily[0].Amount : 0;
        }

        /// <summary>
        /// 카드 버튼 하나를 누른 결과 — <b>안 샀으면 «구매», 샀으면 «받기»</b>다(주인 원문의 두 동작이 카드 하나에 같이 산다).
        /// 못 하는 자리면 <see cref="App.Toast"/> 한 줄로 까닭을 말하고 <b>아무것도 안 바꾼다</b>(<see cref="Privilege.Why"/> 가 그 글자를 준다 · T228 갈래).
        /// </summary>
        void TapCard(int i)
        {
            var d = PD; var c = CardOf(i);
            if (d == null || c == null) return;                       // 표가 없으면 종전 껍데기 — 아무 일도 안 한다
            string today = SaveStore.Today();
            if (!Privilege.Owned(App.Save, c))
            {
                if (!Privilege.Buy(App.Save, c, today)) return;
                App.Persist(); Refresh();
                Pay(c.BuyNow);
                return;
            }
            string why = Privilege.Why(App.Save, d, c, today);
            if (why.Length > 0) { App.Toast(why); return; }
            if (Privilege.Claim(App.Save, d, c, today) == null) return;
            App.Persist(); Refresh();
            Pay(c.Daily);
        }

        /// <summary>«전체 받기» — 오늘 받을 수 있는 카드를 <b>전부</b> 받고 한 번에 보여 준다(하나도 없으면 토스트 한 줄).</summary>
        void TapClaimAll()
        {
            var d = PD; if (d == null) return;
            string today = SaveStore.Today();
            var got = new List<ArenaRankData.Reward>();
            foreach (var c in d.Cards)
                if (Privilege.Claim(App.Save, d, c, today) != null) got.AddRange(c.Daily);
            if (got.Count == 0) { App.Toast("지금 받을 수 있는 특권 보상이 없습니다"); return; }
            App.Persist(); Refresh();
            Pay(got);
        }

        /// <summary>받은 것을 공용 리워드 팝업으로 띄운다(T241) — 지급 자체는 이미 <see cref="Privilege"/> 가 <see cref="Mail.Give"/> 로 했다(T243).</summary>
        static void Pay(List<ArenaRankData.Reward> rewards)
        {
            if (rewards == null || rewards.Count == 0) return;
            var items = new List<RewardPopup.Item>();
            foreach (var r in rewards) items.Add(RewardPopup.Item.Of(RewardIcon(r.Item), UiKit.FmtQty(r.Amount)));
            RewardPopup.Show(items);
        }

        /// <summary>
        /// 보상 이름 → 아이콘 키(특권 표는 지금 전부 다이아지만 표가 바뀌어도 안 깨지게 갈라 둔다).
        /// <para>
        /// ⚠ <b>같은 매핑이 이 레포에 벌써 셋이다</b> — <c>Mailbox.cs:47</c> · <c>EventsScreen.cs:939</c> · 여기.
        /// 하나로 모으는 것이 옳지만 그 셋은 각각 다른 작업의 lock 안이라 여기서 건드리지 않는다(규약 3항).
        /// 처음에 없는 키(<c>ui.petEgg</c>)를 지어 썼다가 <c>check_catalog_keys</c> 가 잡았다 — 이 레포의 펫알 키는 <c>pet.egg</c> 다.
        /// </para>
        /// </summary>
        static string RewardIcon(string item)
        {
            if (item == Mail.ItemGold) return "ui.coin";
            if (item == Mail.ItemPetEgg) return "pet.egg";
            return "ui.gemRed";
        }


        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Color.Lerp(Palette.Slate, Palette.Dim, 0.6f); bg.raycastTarget = true;
            // T72 ① 풀스크린 배경 패턴(주인 «특별 상품들도 마찬가지») — 바탕은 Root 자신의 Image 라 형제 0 = 상단 바·제목·카드·바닥 바 아래 · 어두운 바탕이라 흰 무늬(레퍼런스 11 도 어두운 회색 위 밝은 무늬)
            UiKit.PatternBg(Root, UiKit.PatternTintDark);
            _top = TopBar.Build(App, Root);
            // 제목(⭐ 특권) + 밑줄 + 부제
            var title = UiKit.Rect(Root, "Title"); UiKit.Pct(title, Layout.PrTitle);
            var star = UiKit.Icon(title, "Icon", "pi.star", Palette.Yellow);
            // 페이지 제목 = 제목 종류 60(전 52 · 칸 3.0%×120% = 84px ≥ 한 줄 ≈66px)
            var tt = UiKit.Label(title, 25, -10, 75, 120, "특권", TextSize.Title, Palette.White, TextAnchor.MiddleLeft, kind: TextKind.Title); tt.fontStyle = FontStyles.Bold;
            // T170(주인 2026-09-07 10:0X «타이틀이 왼쪽으로 치우친다 · 특권·던전·PvP · 모든 타이틀 다 점검») —
            // 여기는 «아이콘을 줄 왼쪽 끝(x 0)에 못 박고 글자를 25% 부터 왼쪽 정렬» 이라 덩어리가 왼쪽에 쏠려 있었다.
            // 자리 계산은 UiKit 한 곳(CenterIconTitle)이고 던전·PvP·아레나 티어가 같은 함수를 쓴다.
            UiKit.CenterIconTitle(star.rectTransform, tt, Layout.PrTitle.W);
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
            CardTexture(card1.rectTransform, GradCard1, Palette.Sky);
            var head1 = UiKit.Panel(content, "Head:1", "fr.r12", Palette.A(Palette.Blue, 0.9f)); UiKit.Pct(head1.rectTransform, new Layout.R(Layout.PrCard1.X, Layout.PrCard1.Y, Layout.PrCard1.W, 3.6f).Within(C));
            UiKit.Gradient(head1.rectTransform, inset: CardTextureInset);   // T72 ③ 카드 제목 띠(레퍼런스 11 의 띠도 위 밝고 아래 어둡다)
            // T264 — 주인이 «맨 위에 «데일리 기프트» 라는 섹션» 이라고 부른 그 카드다. 이름·수량은 표에서 온다(코드에 안 박는다).
            var c1 = CardOf(0);
            _cardState[0] = CardHead(head1.transform, "ui.iconGiftRed", c1 != null ? c1.Name : "일일 선물", "초기화까지 " + LobbyPopups.Dashes);
            var reward1 = LobbyPopups.Cell(content, C, Layout.PrCard1Reward, "plum", "ui.gemRed", c1 != null ? UiKit.FmtQty(DailyGem(c1)) : null);
            _cardQty[0] = QtyOf(reward1);
            PlanRewardLight(reward1);
            // T306 — 옷 두 벌(주황·회색)을 겹쳐 둔다. 어느 쪽을 켜는지는 Refresh 가 규칙으로 정한다(여기서 색을 정하지 않는다).
            var skin1 = TwoSkin(content, Layout.PrCard1Btn.Within(C), "받기", () => TapCard(0), "CardBtn:1");
            var btn1 = skin1.Warm;
            _cardBtn[0] = skin1;
            // 카드 2~4 = 긴 카드
            RectTransform card2 = null, cardTitle2 = null, desc2 = null, pic2 = null, reward2 = null, btn2 = null, card3 = null, card4 = null;
            // T306 — 옛 `btnKey`(카드마다 박아 둔 버튼 옷)를 뺐다: 색은 이제 «상태» 가 정하므로 여기 적을 것이 없다.
            // 남긴 `btnLabel` 은 표가 없을 때(껍데기)의 첫 글자일 뿐이고, 표가 있으면 Refresh 가 곧바로 갈아 끼운다.
            (Layout.R rect, Color color, string icon, string name, string pic, string[] lines, string btnLabel, string grad)[] longs =
            {
                (Layout.PrCard2, Palette.Blue, "ui.ad", "광고 제거 카드", "ui.ad", new[] { "영구 광고 제거 특권", "구매 시 💎 지급" }, "받기", GradCard2),
                (Layout.PrCard3, Palette.Plum, "ui.iconCalendar", "월간 카드", "ui.iconMedal", new[] { "최대 탐험 시간 24시간", "던전 티켓 +2 / 일", "최대 배속 +1", "구매 시 💎 지급" }, "구매", GradCard3),
                (Layout.PrCard4, Palette.Orange, "ui.gemRed", "평생 다이아", "ui.trophy", new[] { "매일 다이아 대량 수령", "구매 시 💎 지급" }, "구매", GradCard4),
            };
            for (int k = 0; k < longs.Length; k++)
            {
                var L = longs[k]; float dy = L.rect.Y - Layout.PrCard2.Y;
                var card = UiKit.Panel(content, "Card:" + (k + 2), "fr.r12", L.color); UiKit.Pct(card.rectTransform, L.rect.Within(C));
                CardTexture(card.rectTransform, L.grad, L.color);
                var head = UiKit.Panel(content, "Head:" + (k + 2), "fr.r12", Palette.A(Palette.Dim, 0.35f)); UiKit.Pct(head.rectTransform, Sh(Layout.PrCardTitle, 0, dy).Within(C));
                UiKit.Gradient(head.rectTransform, inset: CardTextureInset);   // T72 ③ 카드 제목 띠
                var ck = CardOf(k + 1);
                _cardState[k + 1] = CardHead(head.transform, L.icon, ck != null ? ck.Name : L.name, "비활성");
                var desc = UiKit.Panel(content, "Desc:" + (k + 2), "fr.r12", Palette.A(Palette.Dim, 0.35f)); UiKit.Pct(desc.rectTransform, Sh(Layout.PrCardDesc, 0, dy).Within(C));
                float lh = 100f / Mathf.Max(2, L.lines.Length);
                for (int i = 0; i < L.lines.Length; i++)
                {
                    float ly = 4 + i * lh * 0.92f;
                    // T264 — «구매 시 지급» 줄에는 주인이 준 즉시 보상 수를 넣는다.
                    // 껍데기 때는 수가 없어 «구매 시 💎 지급» 이었는데, 그러면 주인이 준 2,400·600·4,000 이
                    // 화면 어디에도 안 보인다(카드가 보여 주는 것은 «매일» 쪽뿐이다 · run 580 PNG 실측).
                    string lineText = L.lines[i] == BuyLine ? BuyNowText(ck) : L.lines[i];
                    var bullet = UiKit.Icon(desc.transform, "Bullet", "pi.star", Palette.Yellow); UiKit.Pct(bullet.rectTransform, 4, ly + lh * 0.2f, 6, lh * 0.5f);   // 글머리 = 작은 노란 별(레퍼런스 금색 마름모 자리 · 글자 아님)
                    UiKit.Label(desc.transform, 12, ly, 86, lh * 0.9f, lineText, TextSize.Body, Palette.White, TextAnchor.MiddleLeft);
                }
                var pic = UiKit.Icon(content, "Pic:" + (k + 2), L.pic); UiKit.Pct(pic.rectTransform, Sh(Layout.PrCardPic, 0, dy).Within(C));
                var daily = UiKit.Label(content, 0, 0, 100, 100, "매일 수령", TextSize.Body, Palette.Yellow, TextAnchor.MiddleLeft); daily.name = "Daily"; daily.fontStyle = FontStyles.Bold;
                UiKit.Pct(daily.rectTransform, new Layout.R(8.6f, Layout.PrCardReward.Y + dy, 26.0f, Layout.PrCardReward.H).Within(C));
                var reward = LobbyPopups.Cell(content, C, Sh(Layout.PrCardReward, 0, dy), "plum", "ui.gemRed", ck != null ? UiKit.FmtQty(DailyGem(ck)) : null);
                _cardQty[k + 1] = QtyOf(reward);
                PlanRewardLight(reward);
                // T72 ② 카드 그림 뒤 빛살(주인 «특별 상품 … 아이콘 뒤에 Effect_Light 천천히 회전») — 그림은 카드의 «형제» 라 빛살은 카드 안(칸 밖으로 안 나가게 RectMask2D)에 걸고 그림은 그 위에 그대로 남는다
                _lightPlan.Add((card.rectTransform, pic.rectTransform, UiKit.LightKey));
                int ki = k + 1;   // 람다가 붙잡는 것은 «지금 값» 이어야 한다(루프 변수를 그대로 넘기면 넷이 다 마지막 카드를 누른다)
                // T306 — 표의 `btnKey`(고정 색)를 안 쓴다: 색은 카드마다 상태가 정한다(주황/회색 두 벌 · Refresh 가 고른다).
                var skin = TwoSkin(content, Sh(Layout.PrCardBtn, 0, dy).Within(C), L.btnLabel, () => TapCard(ki), "CardBtn:" + (k + 2));
                var btn = skin.Warm;
                _cardBtn[ki] = skin;
                _prBordered.Add(card.rectTransform); _prBordered.Add(desc.rectTransform);
                if (k == 0) { card2 = card.rectTransform; cardTitle2 = head.rectTransform; desc2 = desc.rectTransform; pic2 = pic.rectTransform; reward2 = reward; btn2 = btn; }
                else if (k == 1) card3 = card.rectTransform; else card4 = card.rectTransform;
            }
            _prBordered.Add(card1.rectTransform);
            // 바닥 바(뒤로 · 전체 받기)
            var foot = UiKit.Panel(Root, "FootBar", "fr.rect", Palette.A(Palette.Dim, 0.9f)); UiKit.Pct(foot.rectTransform, Layout.PrFootBar);
            var back = UiKit.Button(Root, "ui.btnGray", "", () => App.ShowScreen("lobby"), Layout.PrBack); back.name = "BackBtn";
            var bi = UiKit.Icon(back, "Icon", "pi.arrow_left", Palette.Ink); UiKit.Pct(bi.rectTransform, 30, 18, 40, 64);
            // T306 — «전체 받기» 도 두 벌이다(받을 것이 있으면 주황 · 없으면 회색).
            // ⚑ 다만 «회색이어도 눌린다» 는 그대로 둔다 — 누르면 까닭 한 줄(App.Toast)이 뜨고, 그 말은 주인이 지우라고 한 적이 없다.
            //    색만 바꾸라는 지시였으므로 색만 바꾼다(카드 버튼의 잠금은 옛날부터 있던 것이라 그대로).
            _claimAll = TwoSkin(Root, Layout.PrClaimAll, "전체 받기", TapClaimAll, "ClaimAllBtn");
            var claim = _claimAll.Warm;
            // 비평 이름표(표 ⑲)
            UiKit.Tag(_top.Root, "상단 바"); UiKit.Tag(title, "제목 줄"); UiKit.Tag(line.transform, "제목 밑줄"); UiKit.Tag(sub.transform, "부제");
            UiKit.Tag(card1.transform, "특권 카드 1"); UiKit.Tag(reward1, "카드 1 보상 칸"); UiKit.Tag(btn1, "카드 1 버튼");
            UiKit.Tag(card2, "특권 카드 2"); UiKit.Tag(cardTitle2, "카드 제목 띠(2)"); UiKit.Tag(desc2, "카드 설명 상자(2)"); UiKit.Tag(pic2, "카드 그림(2)"); UiKit.Tag(reward2, "카드 보상 칸(2)"); UiKit.Tag(btn2, "카드 버튼(2)");
            UiKit.Tag(card3, "특권 카드 3"); UiKit.Tag(card4, "특권 카드 4 (참고·컨테이너)");
            UiKit.Tag(foot.transform, "바닥 바"); UiKit.Tag(back, "뒤로 버튼"); UiKit.Tag(claim, "전체 받기 버튼");
            // T306 — 겹쳐 둔 회색 벌에도 **같은 이름표**를 단다. 이름표는 «켜져 있는 것» 만 모이므로(UiTag 수집은 active 만)
            // 표에 잡히는 것은 늘 하나지만, 회색이 켜진 화면에서 이름표가 통째로 사라지면 §5 하니스가 그 자리를 «빈 칸» 으로 적는다.
            UiKit.Tag(_cardBtn[0].Cold, "카드 1 버튼"); UiKit.Tag(_cardBtn[1].Cold, "카드 버튼(2)"); UiKit.Tag(_claimAll.Cold, "전체 받기 버튼");
            ApplyLights();
            // T69-lobbypopups(11) — 카드 4장과 설명 상자에 «검은 아웃라인»(레퍼런스 11 도 카드마다·설명 상자마다 외곽선이다).
            // 질감(무늬·그라데이션)과 빛살을 다 건 «뒤» 에 걸어야 링이 그 층들 위에 남는다(빛살은 ApplyLights 가 카드 안에 끼워 넣는다 · 결정 224).
            // 카드 그림(Pic)은 카드의 형제로 떠 있는 그림이고 레퍼런스에도 상자가 없어 담개다(BorderAudit.Exempt).
            foreach (var rt in _prBordered) if (rt != null) UiKit.Bordered(rt);
            _prBordered.Clear();
            Refresh();   // T264 — 버튼 글자·«오늘 받기 완료» 상태를 세이브에서 한 번 칠한다
        }
        /// <summary>특권 카드·설명 상자 — 질감·빛살을 다 건 뒤에 테두리를 걸려고 모아 둔다(T69-lobbypopups).</summary>
        readonly List<RectTransform> _prBordered = new List<RectTransform>();

        /// <summary>
        /// T72 ①③ 특권 카드 안 질감 — 카드 조각 위에 무늬 한 장(밝은 색 카드라 흰 무늬 · 레퍼런스 11 의 카드 안에도 무늬가 어른거린다) + 그라데이션 두 장.
        /// 카드의 내용(제목 띠·설명·그림·보상·버튼)은 카드의 <b>형제</b> 라 이 두 층 위에 그대로 남는다.
        /// <para>
        /// T116 3단계 ⓑ — 그라데이션은 무채색 덧칠(<see cref="UiKit.Gradient"/>)이 아니라 <b>레퍼런스에서 잰 두 색</b>(<paramref name="gradName"/> · <see cref="GradientPalette"/>)이다.
        /// 세기는 처음엔 덧칠(<see cref="UiKit.GradientCardAlpha"/>)이었는데 주인이 «티가 안 난다» 고 해(T345) 몸통 채우기(<see cref="UiKit.GradientCardSolidAlpha"/>)로 올렸다. 이름이 표에 없으면 카드 바탕색에서 만든다.
        /// </para>
        /// </summary>
        static void CardTexture(RectTransform card, string gradName, Color baseColor)
        {
            if (card == null) return;
            UiKit.PatternBg(card, UiKit.PatternTintDark, UiKit.PatternTileSeconds, 0, UiKit.PatternTilePx, CardTextureInset);
            // T345(주인 2026-09-10 «특권 부분도 그라디언트들 있는데 티가 안 남») — 덧칠(GradientCardAlpha 0.55)이면 카드 바탕색이 두 색을 눌러
            //   주인이 지목한 쌍(초록→파랑 · 하늘→보라 · 빨강→노랑)이 «있는지 없는지» 가 안 보인다 → 상자 카드(10)·상품 카드(09 · T341)와 같은
            //   몸통 채우기(GradientCardSolidAlpha · 결정 338). 맨 위 데일리 기프트 카드(GradCard1)도 같은 호출이라 같은 세기로 간다(절 2항 · 결정 966).
            //   ⚠ 이 세기를 재는 자는 UiTextureTests.AssertGradTint 하나다 — 여기를 되돌리면 그 자가 먼저 운다.
            UiKit.GradientCard(card, gradName, baseColor, CardTextureInset, alpha: UiKit.GradientCardSolidAlpha);
        }

        /// <summary>T72 ② 보상 칸(다이아) 그림 뒤 빛살 예약 — 칸 조각(ItemFrame_01) 안 «Item» 바로 뒤(프레임 안쪽에서만 보인다 · 작은 조각).</summary>
        void PlanRewardLight(RectTransform cell)
        {
            var item = cell != null ? UiKit.Find(cell, "Item") : null;
            if (item == null || !item.gameObject.activeSelf) return;
            // T190(주인 13:3X) — 출석·탐험·챕터 보상의 «보상 칸» 은 조각 ItemFrame_01 이라 여기서 걸린다(빛 예약 안 함).
            if (UiKit.IsItemCell(item.parent)) return;
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
        /// <summary>카드 제목 띠 — 오른쪽 글자(상태)를 돌려준다(T264 의 <see cref="Refresh"/> 가 그것만 갈아 끼운다).</summary>
        static TMP_Text CardHead(Transform head, string icon, string name, string right)
        {
            var ic = UiKit.Icon(head, "Icon", icon); UiKit.Pct(ic.rectTransform, 2, 10, 8, 80);
            var t = UiKit.Label(head, 11, 0, 50, 100, name, TextSize.Body, Palette.White, TextAnchor.MiddleLeft); t.fontStyle = FontStyles.Bold;
            return UiKit.Label(head, 62, 0, 36, 100, right, TextSize.Body, Palette.White, TextAnchor.MiddleRight);
        }

        /// <summary>보상 칸의 수량 글자(없으면 null) — <see cref="Refresh"/> 가 표 값을 다시 쓸 자리다.</summary>
        static TMP_Text QtyOf(RectTransform cell)
        {
            if (cell == null) return null;
            var q = UiKit.Find(cell, "Qty");
            return q != null ? q.GetComponent<TMP_Text>() : cell.GetComponentInChildren<TMP_Text>(true);
        }

        /// <summary>
        /// T264 2단계 — 카드 넷의 <b>버튼 글자·상태 글자</b>를 세이브에서 다시 칠한다(배치·조각은 안 건드린다 · ChapterChestScreen 과 같은 문법).
        /// <list type="bullet">
        /// <item>안 산 카드 → «구매»(공짜 카드는 언제나 가진 것이라 여기 안 온다)</item>
        /// <item>오늘 받을 수 있으면 → «받기» · 상태 «오늘 받기 가능»</item>
        /// <item>오늘 이미 받았으면 → 버튼을 <b>회색·못 누름</b>으로 두고 상태 «오늘 받기 완료»(주인 «오늘 받기 완료» 상태)</item>
        /// <item>월간 카드 기간이 끝났으면 → 상태 «기간 만료» · 다시 «구매» 로 (카드는 남는다 · 결정 712 ⓐ)</item>
        /// </list>
        /// </summary>
        public override void Refresh()
        {
            _top?.Refresh();
            var d = PD; if (d == null) return;                        // 표가 없으면 종전 껍데기 그대로
            string today = SaveStore.Today();
            for (int i = 0; i < CardKeys.Length; i++)
            {
                var c = d.Of(CardKeys[i]); if (c == null) continue;
                bool owned = Privilege.Owned(App.Save, c);
                bool expired = Privilege.Expired(App.Save, c, d.MonthlyDays, today);
                bool can = Privilege.Can(App.Save, d, c, today);
                if (_cardQty[i] != null) _cardQty[i].text = UiKit.FmtQty(DailyGem(c));
                if (_cardState[i] != null)
                    _cardState[i].text = !owned ? "비활성" : expired ? "기간 만료" : can ? "오늘 받기 가능" : "오늘 받기 완료";
                var s = _cardBtn[i]; if (s == null) continue;
                // T306 — 주인 규칙: «구매 전 주황 · 받기 전 주황 · 받은 뒤/못 받는 상태 회색».
                // 셋을 하나로 적으면 «지금 할 것이 있는가» 다 — 그것이 옛날부터 있던 «눌리는가» 와 정확히 같은 값이라
                // 색과 잠금을 **한 값으로** 둔다(둘로 적으면 언젠가 «회색인데 눌리는» 칸이 생긴다).
                // 오늘 몫을 이미 받은 «가진 카드» 만 잠근다 — 안 샀거나 기간이 끝난 카드는 «구매» 로 눌려야 한다.
                bool warm = !owned || expired || can;
                s.Set(warm, !owned || expired ? "구매" : "받기", warm);
            }
            // T306 — «전체 받기» = 받을 것이 있으면 주황, 없으면 회색(누르는 것은 늘 되고, 없으면 까닭을 말한다).
            if (_claimAll != null) _claimAll.Set(Privilege.AnyClaimable(App.Save, d, today), "전체 받기", true);
        }
    }
}
