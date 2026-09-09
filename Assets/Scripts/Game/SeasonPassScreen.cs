using System.Collections.Generic;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 시즌 패스 페이지 — 레퍼런스 <c>19_pass.jpg</c>(표 ㊼ · T266 · 주인 2026-09-09 «전에 패스를 폐지했었는데 걍 다시 넣기.
    /// 그 레퍼런스 이미지 그대로 만들고, 그 그라데이션도 잘 해서 만들기»). T78(2026-09-07 «시즌 패스도 삭제»)을 뒤집는 지시다.
    /// <para>
    /// ⚠ <b>이 화면은 «디자인만» 이다</b>(T268 ⓑ · 주인 2026-09-09 «시즌 패스 수치는 뭐 말하는 거지? 일단 걍 디자인만 해놔 걍»).
    /// 레벨 수·보상 내용·가격은 주인이 나중에 준다 — 그래서 여기 보이는 수(레벨 29~33 · 수량 5·20·60 · ₩9,900·₩49,000 · «15/45»)는
    /// <b>레퍼런스에 그렇게 그려져 있다는 기록</b>이지 게임 수치가 아니다. 세이브에 아무것도 안 쌓고, 버튼은 «준비 중» 토스트만 띄운다.
    /// 수치가 오면 <b>표(<c>Assets/KkomaKnight/pass.json</c>)에 줄을 채우면 된다</b>(T322 ⓒ) — 그 표가 모르는 줄은 화면이 «?» 로 그리고 <b>지어내지 않는다</b>(§1).
    /// </para>
    /// 구도(표 ㊼): 상단 재화 바 → <b>머리 배너</b>(패스 이름 · 시즌 종료 줄 · 메달 + 진행 바 + 육각 레벨 배지) → 안내 띠 →
    /// <b>3열 트랙</b>(무료 파랑 · 유료 1 주황 · 유료 2 자주 · 가운데 노란 줄에 레벨 배지 · <b>1~100 세로 스크롤</b> · T322) →
    /// 버튼 셋(«모두 받기» · ₩9,900 · ₩49,000) → 왼쪽 아래 뒤로. (오른쪽 아래 «방랑자의 보상» 탭은 <b>주인이 지웠다</b> — T304 · 2026-09-09 «그 버튼이 필요 없음».)
    /// <para>
    /// 그라데이션은 <b>레퍼런스 실측</b>이다(1단계 · 결정 723): 열 셋 <c>passFree</c>·<c>passPaid1</c>·<c>passPaid2</c> ·
    /// 버튼 둘 <c>btnPassPaid1</c>·<c>btnPassPaid2</c> · 배너 <c>passBanner</c> · «아직 못 연 행» 의 단색 어둠 <c>col.passFreeDim</c> 셋.
    /// 레퍼런스의 무지개·성 그림은 주인 에셋에 없다 → 실측한 하늘·풀밭 두 색 판으로 세운다(<b>새 그림 0</b> · ROUTINE ⓑ).
    /// </para>
    /// 이름 계약(스모크 테스트): 배너 <c>Banner</c> · <c>PassName</c> · <c>SeasonEnds</c> · <c>ProgressBar</c> · <c>LevelBadge</c> ·
    /// 안내 띠 <c>Notice</c> · 트랙 <c>Track</c> · 열 <c>Col:free</c>/<c>Col:paid1</c>/<c>Col:paid2</c> · 가운데 줄 <c>Line</c> ·
    /// 레벨 배지 <c>Badge:N</c> · 보상 칸 <c>Cell:free:N</c>… · 줄 <c>Row:N</c> · 어둠 <c>Dim:free</c>… ·
    /// 버튼 <c>ClaimAllBtn</c>/<c>BuyBtn:1</c>/<c>BuyBtn:2</c>/<c>BackBtn</c>.
    /// </summary>
    public sealed class SeasonPassScreen : GameScreen
    {
        public override string Name => "seasonPass";

        /// <summary>우리말 패스 이름 — 레퍼런스의 «Wayfarer's Bounty» 를 옮긴 것이고 <b>워커가 정했다</b>(주인 미제공 · 결정 기록).</summary>
        public const string PassTitle = "방랑자의 보상";
        /// <summary>껍데기 버튼이 띄우는 말 — 수치가 오기 전에는 아무것도 지급하지 않는다(T268 ⓑ).</summary>
        public const string NotReadyToast = "준비 중입니다";

        // ───────────────────────── 자리(표 ㊼ 실측 · 화면 %) ─────────────────────────
        // ⚠ 이 rect 들을 Core/Layout 에 두지 않은 까닭: 그 파일이 T260 의 살아 있는 lock 안이다(«같은 파일이면 뒤 번호가 기다린다»).
        //    T260 이 반납하면 Layout 으로 옮긴다 — 값은 표 ㊼ 그대로라 옮겨도 한 자도 안 바뀐다(결정 기록).
        static readonly Layout.R RBanner = new Layout.R(0f, 7.9f, 100f, 19.9f);
        // §5 회차 1(9.2/10) 에서 이 행이 ✗ 였다 — 표 ㊼ 의 ref 는 «흰 글자 잉크 bbox»(실측 px 192~527)인데 내가 우리말을 담으려고 80% 로 벌려 뒀었다.
        // 재어 보니 벌릴 까닭이 없었다: 46.7% = 504px 이고 «방랑자의 보상» 7자는 Title 60 에서 ≈420px 라 84px 이 남는다 → ref 그대로 되돌린다.
        static readonly Layout.R RName = new Layout.R(26.7f, 13.0f, 46.7f, 2.6f);
        // ⚠ 이 행은 §5 에서 ✗ 로 둔다(표 ㊼ 의 ⚑ 참고) — ref 33.3%(=360px)는 영문 «Season ends in 20d 8h» 의 잉크다.
        //    우리말 «시즌 종료까지 20일 8시간» 은 그 폭에서 bestFit 이 하한(32) 밑으로 내려갈 셈이 나온다 — 점수 한 행보다 글자 잘림 0(T63)이 먼저다.
        //    ⇒ 좁히지 않고 **실제로 몇 으로 놓였는지**를 스모크가 로그로 찍게 했다(결정 739·T260 4단계와 같은 순서: 못 재는 자리에 단언을 안 세운다).
        static readonly Layout.R REnds = new Layout.R(20f, 19.2f, 60f, 2.0f);
        static readonly Layout.R RMedal = new Layout.R(15.8f, 20.6f, 7.2f, 2.9f);
        static readonly Layout.R RBar = new Layout.R(22.9f, 21.3f, 54.9f, 1.7f);
        static readonly Layout.R RBadge = new Layout.R(75.0f, 20.4f, 11.1f, 3.8f);
        static readonly Layout.R RNotice = new Layout.R(1.9f, 24.2f, 96.3f, 3.5f);
        static readonly Layout.R RTrack = new Layout.R(1.9f, 31.4f, 96.3f, 53.2f);
        static readonly Layout.R RColFree = new Layout.R(1.9f, 31.4f, 30.0f, 53.2f);
        static readonly Layout.R RLine = new Layout.R(32.2f, 31.4f, 2.6f, 53.2f);
        static readonly Layout.R RColPaid1 = new Layout.R(35.3f, 31.4f, 30.6f, 53.2f);
        static readonly Layout.R RColPaid2 = new Layout.R(66.7f, 31.4f, 31.5f, 53.2f);
        static readonly Layout.R RCellFree = new Layout.R(11.7f, 35.1f, 17.5f, 6.5f);
        static readonly Layout.R RCellPaid1 = new Layout.R(41.7f, 35.1f, 19.4f, 6.5f);
        static readonly Layout.R RCellPaid2 = new Layout.R(75.0f, 35.1f, 17.8f, 6.5f);
        static readonly Layout.R RLvBadge = new Layout.R(29.6f, 35.9f, 7.9f, 4.5f);
        static readonly Layout.R RClaimAll = new Layout.R(4.0f, 86.5f, 28.6f, 4.6f);
        static readonly Layout.R RBuy1 = new Layout.R(36.1f, 86.3f, 28.1f, 4.7f);
        static readonly Layout.R RBuy2 = new Layout.R(68.3f, 86.3f, 27.9f, 4.7f);
        static readonly Layout.R RBack = new Layout.R(3.2f, 94.6f, 15.8f, 4.4f);
        /// <summary>행 피치(%p) — 레퍼런스 실측 160px / 1560 = 10.3%p(배지 중심 간 거리 29→30→31→32 가 160·159·160).</summary>
        const float RowPitch = 10.3f;

        // ───────────────────────── 레퍼런스에 그려진 값(게임 수치 아님) ─────────────────────────

        TopBar _top;

        /// <summary>페이지를 연다 — 로비 배너(T78 이 비워 둔 자리)와 하단 «패스» 아이콘 버튼이 부른다.</summary>
        public static void Open(App app) => app?.ShowScreen("seasonPass");

        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Palette.Hex("#242424"); bg.raycastTarget = true;   // 트랙 밖 바탕 실측
            UiKit.PatternBg(Root, UiKit.PatternTintDark);
            _top = TopBar.Build(App, Root);
            UiKit.Tag(_top.Root, "상단 재화 바");   // §5 는 이름표를 **이름 그대로** 맞춘다(결정 592) — 표 ㊼ 의 행 이름과 한 글자도 다르면 그 행이 0 점이 된다

            BuildBanner();
            BuildNotice();
            BuildTrack();
            BuildButtons();
        }

        void BuildBanner()
        {
            var banner = UiKit.Panel(Root, "Banner", "fr.rect", Color.white).rectTransform;
            UiKit.Pct(banner, RBanner);
            // 무지개·성 그림은 주인 에셋에 없다 → 실측한 하늘·풀밭 두 색 판(새 그림 0). 그림 조각이 생기면 이 판만 갈아 끼운다.
            UiKit.GradientCard(banner, "passBanner", alpha: UiKit.GradientCardSolidAlpha);

            var name = UiKit.Label(banner, RName.X, (RName.Y - RBanner.Y) / RBanner.H * 100f, RName.W, RName.H / RBanner.H * 100f,
                PassTitle, TextSize.Title, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Title);
            name.name = "PassName"; name.fontStyle = FontStyles.Bold;

            var ends = UiKit.Label(Root, REnds.X, REnds.Y, REnds.W, REnds.H, "시즌 종료까지 20일 8시간", TextSize.Body, Palette.White);
            ends.name = "SeasonEnds";

            var medal = UiKit.Icon(Root, "Medal", "ui.iconMedal", Color.white); UiKit.Pct(medal.rectTransform, RMedal);

            // 진행 바 — 레퍼런스는 «15/45» 가 바 가운데에 얹힌 꼴이다(채움은 실측 31%)
            var bar = UiKit.Panel(Root, "ProgressBar", "fr.sliderBg", Palette.A(Palette.Ink, 0.75f)).rectTransform;
            UiKit.Pct(bar, RBar); UiKit.Bordered(bar);
            var fill = UiKit.Panel(bar, "Fill", "fr.sliderBg", Palette.Hex("#3FD214")).rectTransform;
            UiKit.Pct(fill, 0f, 0f, 31f, 100f);
            UiKit.Tag(fill, "진행 바 채움(초록)");
            UiKit.Label(bar, 0, 0, 100, 100, "15/45", TextSize.Aux, Palette.White).name = "BarText";

            var badge = UiKit.Panel(Root, "LevelBadge", "fr.r12", Palette.A(Palette.Ink, 0.85f)).rectTransform;
            UiKit.Pct(badge, RBadge); UiKit.Bordered(badge);
            UiKit.Label(badge, 0, 0, 100, 100, "32", TextSize.Body, Palette.White).name = "LevelText";

            UiKit.Tag(banner, "머리 배너(그림)"); UiKit.Tag(name.rectTransform, "패스 이름");
            UiKit.Tag(ends.rectTransform, "«시즌 종료까지 20일 8시간»"); UiKit.Tag(medal.rectTransform, "메달 아이콘(진행 바 왼쪽)");
            UiKit.Tag(bar, "진행 바(전체)"); UiKit.Tag(badge, "레벨 육각 배지(머리)");
        }

        void BuildNotice()
        {
            var strip = UiKit.Panel(Root, "Notice", "fr.rect", App.Assets != null ? App.Assets.Color("col.passNotice", Palette.Hex("#182210")) : Palette.Hex("#182210")).rectTransform;
            UiKit.Pct(strip, RNotice); UiKit.Bordered(strip);
            UiKit.Label(strip, 0, 0, 100, 100, "일일 퀘스트를 깨면 보상이 열린다", TextSize.Body, Palette.Cream).name = "NoticeText";
            UiKit.Tag(strip, "안내 띠");
        }

        /// <summary>행 하나가 차지하는 세로 px — 표 ㊼ 의 행 피치(%p)를 캔버스 px 로 옮긴 값이다(그림 px 을 그대로 쓰지 않는다 · 결정 839).</summary>
        static float PitchPx => UiKit.FrameH * RowPitch / 100f;
        /// <summary>레퍼런스 그림의 «지금 레벨» — 배너 배지도 이 수를 쓴다(주인 수치가 오면 세이브에서 온다 · T266 ⓑ).</summary>
        public const int CurLevel = 32;
        /// <summary>열었을 때 <b>맨 위에 보이는 줄</b> — 레퍼런스 19 가 29~33 을 보여 주는 상태(= 지금 레벨에서 셋 위)다. §5 표 ㊼ 의 «첫 행» 이 이 줄이다.</summary>
        public static int TopLevel => Mathf.Max(1, CurLevel - 3);

        ScrollRect _scroll;
        RectTransform _content;
        PassData _pass;
        readonly Dictionary<int, RectTransform> _rows = new Dictionary<int, RectTransform>();

        int MaxLevel => _pass != null ? _pass.MaxLevel : 100;

        /// <summary>
        /// 트랙 — 세 열 그라데이션은 <b>스크롤 뒤에 붙박이</b>로 서고(레퍼런스도 그렇다 · T302 가 고친 그 열이다),
        /// 줄 1~<see cref="MaxLevel"/> 은 스크롤 안에서 <b>보이는 것만</b> 만들어진다(주인 «1~100까지 있어야» · T322).
        /// <para>
        /// ⚠ <b>100×3 칸을 한 번에 세우지 않는다</b> — 300칸을 미리 만들면 페이지를 열 때 눈에 띄게 멈춘다.
        /// 그래서 스크롤이 움직일 때마다 <see cref="RefreshRows"/> 가 «보이는 구간 ± 한 줄» 만 남기고 나머지를 지운다.
        /// </para>
        /// «아직 못 연 줄» 의 어둠도 <b>스크롤 안</b>에 있다 — 붙박이로 두면 줄이 움직일 때 어둠만 제자리에 남아 엉뚱한 줄을 덮는다.
        /// </summary>
        void BuildTrack()
        {
            _pass = App != null && App.Data != null ? App.Data.Pass : null;

            // ① 붙박이 배경 — 열 셋 + 가운데 노란 줄(스크롤과 무관하게 선다)
            Column("Col:free", RColFree, "passFree");
            Column("Col:paid1", RColPaid1, "passPaid1");
            Column("Col:paid2", RColPaid2, "passPaid2");
            var line = UiKit.Panel(Root, "Line", "fr.rect", App.Assets != null ? App.Assets.Color("col.passLine", Palette.Hex("#FFF43B")) : Palette.Hex("#FFF43B")).rectTransform;
            UiKit.Pct(line, RLine);

            // ② 스크롤 창 — 자리는 표 ㊼ 의 트랙 그대로다
            var track = UiKit.Rect(Root, "Track"); UiKit.Pct(track, RTrack);
            track.gameObject.AddComponent<RectMask2D>();
            var timg = UiKit.Ensure<Image>(track.gameObject); timg.color = new Color(0, 0, 0, 0); timg.raycastTarget = true;
            _scroll = track.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false; _scroll.movementType = ScrollRect.MovementType.Clamped; _scroll.scrollSensitivity = 40;
            _content = UiKit.Rect(track, "Content");
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1); _content.pivot = new Vector2(0.5f, 1);
            _content.offsetMin = Vector2.zero; _content.offsetMax = Vector2.zero;
            _content.sizeDelta = new Vector2(0, MaxLevel * PitchPx);
            _scroll.content = _content; _scroll.viewport = track;

            // ③ «아직 못 연 줄» 어둠 — 지금 레벨 **아래 줄부터** 끝까지(구간 띠 기준이 아니라 레벨 기준 · T322 ⓑ)
            Dim("Dim:free", RColFree, "col.passFreeDim");
            Dim("Dim:paid1", RColPaid1, "col.passPaid1Dim");
            Dim("Dim:paid2", RColPaid2, "col.passPaid2Dim");

            // ④ 열자마자 레퍼런스와 같은 자리(맨 위 = TopLevel)로 굴려 둔다
            _content.anchoredPosition = new Vector2(0, (TopLevel - 1) * PitchPx);
            _scroll.onValueChanged.AddListener(_ => RefreshRows());
            RefreshRows();

            UiKit.Tag(track, "트랙(3열 전체)"); UiKit.Tag(line, "가운데 노란 줄");
        }

        /// <summary>스크롤 안 자리 — x·w 는 트랙 안 %(표 값을 그대로 옮긴다) · y 는 «레벨 번호 × 행 피치» px.</summary>
        void PlaceRow(RectTransform rt, Layout.R screenR, int level)
        {
            var loc = screenR.Within(RTrack);
            rt.anchorMin = new Vector2(loc.X / 100f, 1f);
            rt.anchorMax = new Vector2((loc.X + loc.W) / 100f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            float hPx = screenR.H / 100f * UiKit.FrameH;
            float topPx = (level - 1) * PitchPx + (screenR.Y - RTrack.Y) / 100f * UiKit.FrameH;
            rt.sizeDelta = new Vector2(0, hPx);
            rt.anchoredPosition = new Vector2(0, -topPx);
        }

        /// <summary>«아직 못 연 줄» 어둠 한 열 — 지금 레벨 아래 줄 꼭대기부터 내용 끝까지. 스크롤 안이라 줄과 같이 움직인다.</summary>
        void Dim(string name, Layout.R colR, string dimKey)
        {
            float topPx = CurLevel * PitchPx;                       // 레벨 CurLevel+1 줄의 꼭대기
            float hPx = Mathf.Max(0f, MaxLevel * PitchPx - topPx);
            var loc = colR.Within(RTrack);
            var d = UiKit.Panel(_content, name, "fr.rect", App.Assets != null ? App.Assets.Color(dimKey, Palette.Ink) : Palette.Ink).rectTransform;
            d.anchorMin = new Vector2(loc.X / 100f, 1f);
            d.anchorMax = new Vector2((loc.X + loc.W) / 100f, 1f);
            d.pivot = new Vector2(0.5f, 1f);
            d.sizeDelta = new Vector2(0, hPx);
            d.anchoredPosition = new Vector2(0, -topPx);
        }

        /// <summary>
        /// 보이는 줄만 남긴다 — 위아래로 한 줄씩 넉넉히 두고(스크롤이 빨라도 빈 칸이 안 보이게) 나머지는 지운다.
        /// <para>줄을 만드는 값은 전부 표(<see cref="PassData"/>)에서 온다 — <b>표가 모르는 줄은 «?» 로 그리고 수를 지어내지 않는다</b>(T322 ⓒ).</para>
        /// </summary>
        void RefreshRows()
        {
            if (_content == null || _scroll == null) return;
            float top = _content.anchoredPosition.y;
            float viewH = ((RectTransform)_scroll.viewport).rect.height;
            int first = Mathf.Max(1, Mathf.FloorToInt(top / PitchPx));
            int last = Mathf.Min(MaxLevel, Mathf.CeilToInt((top + viewH) / PitchPx) + 1);

            var drop = new List<int>();
            foreach (var kv in _rows) if (kv.Key < first || kv.Key > last) drop.Add(kv.Key);
            foreach (var k in drop) { if (_rows[k] != null) Object.Destroy(_rows[k].gameObject); _rows.Remove(k); }

            for (int lv = first; lv <= last; lv++) if (!_rows.ContainsKey(lv)) _rows[lv] = BuildRow(lv);
        }

        /// <summary>줄 하나 = 레벨 배지 + 세 칸. 이름은 옛 계약 그대로(<c>Badge:N</c> · <c>Cell:free:N</c> …).</summary>
        RectTransform BuildRow(int level)
        {
            bool dim = level > CurLevel;
            var row = UiKit.Rect(_content, "Row:" + level);
            row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(1, 1); row.pivot = new Vector2(0.5f, 1);
            row.sizeDelta = new Vector2(0, PitchPx);
            row.anchoredPosition = new Vector2(0, -(level - 1) * PitchPx);

            var cFree = Cell(row, "Cell:free:" + level, RCellFree, level, PassData.ColFree, dim);
            var cPaid1 = Cell(row, "Cell:paid1:" + level, RCellPaid1, level, PassData.ColPaid1, dim);
            var cPaid2 = Cell(row, "Cell:paid2:" + level, RCellPaid2, level, PassData.ColPaid2, dim);
            if (level == TopLevel)
            {   // 표 ㊼ 는 «첫 행» 만 재고 아래 행은 피치로 따라온다 — 열었을 때 맨 위에 오는 줄이 그 «첫 행» 이다
                UiKit.Tag(cFree, "보상 칸(무료 · 첫 행)"); UiKit.Tag(cPaid1, "보상 칸(유료 1 · 첫 행)"); UiKit.Tag(cPaid2, "보상 칸(유료 2 · 첫 행)");
            }

            var b = UiKit.Panel(row, "Badge:" + level, "fr.r12", Palette.A(dim ? Palette.Ink : Palette.Hex("#96793B"), 0.95f)).rectTransform;
            PlaceRowIn(b, row, RLvBadge, level); UiKit.Bordered(b);
            UiKit.Label(b, 0, 0, 100, 100, level.ToString(), TextSize.Aux, dim ? Palette.CreamDark : Palette.White).name = "BadgeText";
            if (level == TopLevel) UiKit.Tag(b, "레벨 배지(행마다)");
            return row;
        }

        /// <summary>줄 상자 안 자리 — 줄이 이미 제 y 에 있으므로 여기서는 <b>줄 안 오프셋</b>만 준다.</summary>
        void PlaceRowIn(RectTransform rt, RectTransform row, Layout.R screenR, int level)
        {
            var loc = screenR.Within(RTrack);
            rt.anchorMin = new Vector2(loc.X / 100f, 1f);
            rt.anchorMax = new Vector2((loc.X + loc.W) / 100f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0, screenR.H / 100f * UiKit.FrameH);
            rt.anchoredPosition = new Vector2(0, -(screenR.Y - RTrack.Y) / 100f * UiKit.FrameH);
        }

        /// <summary>
        /// 열 하나 — 밝은 구간은 실측 그라데이션, 구간 띠 아래(아직 못 연 행)는 실측 단색 어둠을 덮는다.
        /// <para>
        /// ⚠ <b>바탕을 흰색으로 깔면 안 된다</b>(T302 · 주인 2026-09-09 «패스 부분 그라데이션이 전혀 레퍼런스랑 다름»).
        /// <see cref="UiKit.GradientCard"/> 가 얹는 것은 <b>«페이드» 조각 둘</b>(위에서 아래로 · 아래에서 위로 투명해진다)이라
        /// <b>가운데는 거의 비어 있고 바탕이 그대로 비친다</b> — 바탕이 흰색이면 열 전체가 «표 색 ½ + 흰색 ½» 이 되어 파스텔로 죽는다
        /// (run 696 실측: 파랑 위가 표 <c>(32,65,172)</c> 인데 화면은 <c>(139,148,199)</c> — 정확히 흰색과 반씩이다).
        /// <b>표 값은 맞았고 틀린 것은 그리는 법이었다.</b>
        /// </para>
        /// 그래서 바탕을 <b>표의 위·아래 가운데 색</b>으로 깐다 — 위는 위 색, 아래는 아래 색, 가운데는 그 중간이 되어 흰색이 비칠 자리가 없다.
        /// </summary>
        void Column(string name, Layout.R r, string grad)
        {
            var col = UiKit.Panel(Root, name, "fr.rect", ColumnBase(grad)).rectTransform;
            UiKit.Pct(col, r);
            UiKit.GradientCard(col, grad, alpha: UiKit.GradientCardSolidAlpha);
            // T328(주인 2026-09-09 «그것들도 다 패턴 효과 있어야 하는데 없네») — 열 위에 흐르는 무늬 한 장.
            // ⚠ **그라데이션 «위» 에 얹는다** — 이 레포의 공통 순서는 «바탕 → 무늬 → 그라데이션»(결정 171)인데,
            //    그 순서가 통하는 것은 공통 팝업처럼 그라데이션이 **옅을 때**(알파 0.12·0.18)다.
            //    여기 그라데이션은 `GradientCardSolidAlpha`(1) 라 위아래 끝이 불투명해서, 밑에 깔면 무늬가 가운데만 비친다
            //    (T302 가 «페이드 조각은 가운데가 비어 있다» 로 밝힌 그 성질이 이번엔 반대로 작용한다).
            //    ⇒ 순서를 바꾼 것이 아니라 **«바탕» 이 무엇인지가 다른 자리**다. 무늬 위로는 어둠·칸이 온다(그 둘은 스크롤 안이라 저절로 위다).
            UiKit.PatternBg(col, Palette.A(Color.white, PatternAlpha), UiKit.PatternTileSeconds, col.childCount, UiKit.PatternTilePx);
            // ⚠ «아직 못 연 줄» 어둠은 **여기 없다** — 줄이 스크롤을 타므로 어둠도 같이 움직여야 한다(<see cref="Dim"/> · T322 ⓑ).
            //    붙박이로 두면 줄만 지나가고 어둠은 제자리에 남아 **엉뚱한 줄을 덮는다**.
            UiKit.Tag(col, name == "Col:free" ? "무료 열(파랑)" : name == "Col:paid1" ? "유료 1 열(주황)" : "유료 2 열(자주)");
        }

        /// <summary>
        /// 열 무늬의 알파(T328) — <b>레퍼런스 실측</b>이다. 공통 <see cref="UiKit.PatternAlpha"/>(3/255)보다 진하다.
        /// <para>
        /// <b>어떻게 쟀나</b>: 그라데이션은 <b>세로로만</b> 변하므로 <b>같은 y 에서 가로로</b> 훑으면 남는 흔들림이 곧 무늬다
        /// (`19_pass.jpg` · 열의 칸 없는 왼쪽 여백 · y 34·38·42%). 세 열 모두 <b>밝기 폭 ≈ 9/255</b> 였고 바탕이 ≈ 90 이라
        /// 흰 무늬가 그만큼 들어 올리려면 알파 ≈ 9 ÷ (255 − 90) ≈ 0.05 다.
        /// </para>
        /// <para>
        /// ⚑ <b>첫 값 13/255 는 2.3배 셌다 — 찍힌 그림으로 고쳤다</b>(run 820 실측 · 결정 926).
        /// 레퍼런스에서 «무늬가 만드는 밝기 폭» 을 재어 알파를 거꾸로 셈했는데, <b>우리 무늬 그림은 레퍼런스와 다른 그림</b>이다
        /// (레퍼런스는 선물 상자·별이 촘촘하고 우리 공통 무늬는 활·화살이 크고 성기다 — 새 그림은 안 만든다).
        /// 조각이 크면 같은 알파라도 <b>대비가 더 뭉쳐 보인다</b> ⇒ 알파를 «레퍼런스 알파» 로 맞출 게 아니라
        /// <b>«찍힌 밝기 폭» 이 레퍼런스와 같아지게</b> 맞춰야 한다. 폭 비(우리 26·35·30 ↔ ref 16·8·13 · 가운데값 2.3배)로 나눠 6/255.
        /// </para>
        /// ⚠ 이 수는 <b>내가 PlayMode 를 못 돌리는 자리</b>(결정 143)라 자는 이 상수를 견주지 «픽셀이 몇이더라» 를 견주지 않는다.
        /// 눈·픽셀 확인은 `screens` 19 로 한다 — 실제로 그렇게 해서 이 값을 고쳤다.
        /// </summary>
        public const float PatternAlpha = 6f / 255f;

        /// <summary>
        /// 열 바탕색 = 표(<c>col.grad.&lt;이름&gt;</c>)의 <b>위·아래 가운데</b>(T302). 표에 그 이름이 없으면 예전대로 흰색이다.
        /// <para>수를 여기 안 적는다(결정 555) — <see cref="GradientPalette"/> 한 곳에서 읽으므로 주인이 색을 바꾸면 바탕도 같이 따라간다.</para>
        /// </summary>
        static Color ColumnBase(string grad)
        {
            if (string.IsNullOrEmpty(grad) || !GradientPalette.Has(grad)) return Color.white;
            var p = GradientPalette.Of(grad);
            return Color.Lerp(p.Top, p.Bottom, 0.5f);
        }

        /// <summary>보상 칸 하나 — 아이콘 + 수량, 받은 칸은 초록 체크, 잠긴 칸은 자물쇠(레퍼런스 그대로).</summary>
        /// <summary>
        /// 보상 칸 하나 — 아이콘 + 수량, 받은 칸은 초록 체크, 잠긴 칸은 자물쇠(레퍼런스 그대로).
        /// <para>
        /// ⚠ <b>표가 모르는 줄은 «?» 로 그린다</b>(T322 ⓒ) — 주인이 아직 값을 안 줬고(T266 ⓑ), 없는 수를 그리면
        /// 주인이 그것을 «정한 값» 으로 읽는다. 빈 칸에는 두루마리 «?» + 자물쇠만 둔다.
        /// </para>
        /// «받았다» 는 <b>지금 레벨보다 위 줄의 무료 열</b>만이다 — 세이브가 없으므로(디자인만) 그 이상은 꾸미지 않는다.
        /// </summary>
        RectTransform Cell(RectTransform row, string name, Layout.R r, int level, int col, bool dim)
        {
            var v = _pass != null ? _pass.At(level, col) : default;
            bool claimed = !dim && col == PassData.ColFree && level < CurLevel;
            var cell = UiKit.Panel(row, name, "fr.itemBg", Palette.A(Palette.Ink, dim ? 0.75f : 0.5f)).rectTransform;
            PlaceRowIn(cell, row, r, level); UiKit.Bordered(cell);
            var icon = UiKit.Icon(cell, "Icon", v.Known ? v.Icon : UnknownIcon, dim || !v.Known ? Palette.A(Color.white, 0.45f) : Color.white);
            UiKit.Pct(icon.rectTransform, 14, 8, 72, 66);
            UiKit.Label(cell, 45, 66, 52, 30, v.Known ? v.Qty : "?", TextSize.Aux, dim || !v.Known ? Palette.CreamDark : Palette.White).name = "Qty";
            if (claimed) { var ck = UiKit.Icon(cell, "Check", "pi.check", Palette.Hex("#3FD214")); UiKit.Pct(ck.rectTransform, 18, 18, 64, 64); }
            else if (col != PassData.ColFree) { var lk = UiKit.Icon(cell, "Lock", "pi.lock", dim ? Palette.CreamDark : Color.white); UiKit.Pct(lk.rectTransform, 62, -6, 40, 40); }
            return cell;
        }
        /// <summary>표가 값을 모르는 칸의 그림 — 레퍼런스의 «두루마리» 그대로다(수는 «?» 로 적는다).</summary>
        const string UnknownIcon = "ui.iconScroll";

        void BuildButtons()
        {
            var all = UiKit.Button(Root, "ui.btnGray", "모두 받기", () => App.Toast(NotReadyToast), RClaimAll); all.name = "ClaimAllBtn";
            var b1 = UiKit.Button(Root, "ui.btnOrange", "₩9,900", () => App.Toast(NotReadyToast), RBuy1); b1.name = "BuyBtn:1";
            UiKit.GradientCard(b1, "btnPassPaid1", alpha: UiKit.GradientCardSolidAlpha);
            var b2 = UiKit.Button(Root, "ui.btnOrange", "₩49,000", () => App.Toast(NotReadyToast), RBuy2); b2.name = "BuyBtn:2";
            UiKit.GradientCard(b2, "btnPassPaid2", alpha: UiKit.GradientCardSolidAlpha);

            var back = UiKit.Button(Root, "ui.btnGray", "", () => App.ShowScreen("lobby"), RBack); back.name = "BackBtn";
            { var t = UiKit.ButtonText(back); if (t != null) t.gameObject.SetActive(false); var ic = UiKit.Icon(back, "Icon", "pi.arrow_left", Palette.Cream); UiKit.Pct(ic.rectTransform, 30, 18, 40, 64); }


            UiKit.Tag(all, "«모두 받기» 버튼"); UiKit.Tag(b1, "«₩9,900» 버튼"); UiKit.Tag(b2, "«₩49,000» 버튼");
            UiKit.Tag(back, "뒤로 버튼");
        }

        public override void Refresh() => _top?.Refresh();
    }
}
