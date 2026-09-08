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
    /// 수치가 오면 <see cref="Rows"/>·<see cref="Levels"/> 자리에 표(<c>seasonPass.json</c>)를 끼우면 된다 — 그 표가 없는 지금 지어내지 않는다(§1).
    /// </para>
    /// 구도(표 ㊼): 상단 재화 바 → <b>머리 배너</b>(패스 이름 · 시즌 종료 줄 · 메달 + 진행 바 + 육각 레벨 배지) → 안내 띠 →
    /// <b>3열 트랙</b>(무료 파랑 · 유료 1 주황 · 유료 2 자주 · 가운데 노란 줄에 레벨 배지) → 구간 노란 띠(«💎100») →
    /// 버튼 셋(«모두 받기» · ₩9,900 · ₩49,000) → 왼쪽 아래 뒤로 · 오른쪽 아래 패스 아이콘 버튼.
    /// <para>
    /// 그라데이션은 <b>레퍼런스 실측</b>이다(1단계 · 결정 723): 열 셋 <c>passFree</c>·<c>passPaid1</c>·<c>passPaid2</c> ·
    /// 버튼 둘 <c>btnPassPaid1</c>·<c>btnPassPaid2</c> · 배너 <c>passBanner</c> · «아직 못 연 행» 의 단색 어둠 <c>col.passFreeDim</c> 셋.
    /// 레퍼런스의 무지개·성 그림은 주인 에셋에 없다 → 실측한 하늘·풀밭 두 색 판으로 세운다(<b>새 그림 0</b> · ROUTINE ⓑ).
    /// </para>
    /// 이름 계약(스모크 테스트): 배너 <c>Banner</c> · <c>PassName</c> · <c>SeasonEnds</c> · <c>ProgressBar</c> · <c>LevelBadge</c> ·
    /// 안내 띠 <c>Notice</c> · 트랙 <c>Track</c> · 열 <c>Col:free</c>/<c>Col:paid1</c>/<c>Col:paid2</c> · 가운데 줄 <c>Line</c> ·
    /// 레벨 배지 <c>Badge:29</c>… · 보상 칸 <c>Cell:free:29</c>… · 구간 <c>SegBand</c>/<c>SegBadge</c> ·
    /// 버튼 <c>ClaimAllBtn</c>/<c>BuyBtn:1</c>/<c>BuyBtn:2</c>/<c>BackBtn</c>/<c>PassIconBtn</c>.
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
        static readonly Layout.R RSegBand = new Layout.R(1.9f, 73.7f, 96.3f, 1.0f);
        static readonly Layout.R RSegBadge = new Layout.R(26.4f, 71.8f, 15.3f, 3.8f);
        static readonly Layout.R RClaimAll = new Layout.R(4.0f, 86.5f, 28.6f, 4.6f);
        static readonly Layout.R RBuy1 = new Layout.R(36.1f, 86.3f, 28.1f, 4.7f);
        static readonly Layout.R RBuy2 = new Layout.R(68.3f, 86.3f, 27.9f, 4.7f);
        static readonly Layout.R RBack = new Layout.R(3.2f, 94.6f, 15.8f, 4.4f);
        static readonly Layout.R RPassIcon = new Layout.R(80.1f, 92.5f, 19.2f, 7.2f);
        /// <summary>행 피치(%p) — 레퍼런스 실측 160px / 1560 = 10.3%p(배지 중심 간 거리 29→30→31→32 가 160·159·160).</summary>
        const float RowPitch = 10.3f;

        // ───────────────────────── 레퍼런스에 그려진 값(게임 수치 아님) ─────────────────────────
        /// <summary>레퍼런스에 보이는 레벨 다섯(29~33) — 마지막 33 은 구간 띠 <b>아래</b>라 «아직 못 연 행» 이다.</summary>
        static readonly int[] Levels = { 29, 30, 31, 32, 33 };
        /// <summary>행마다 세 열의 «아이콘 키 · 수량 · 상태» — 전부 레퍼런스 그림 그대로다(주인 수치 아님 · T268 ⓑ).</summary>
        static readonly (string Icon, string Qty, bool Claimed)[,] Rows =
        {
            //  무료(파랑)                      유료 1(주황)                      유료 2(자주)
            { ("ui.coin", "5", true),   ("ui.iconScroll", "20", false), ("ui.iconScroll", "60", false) },
            { ("ui.gemRed", "65", true), ("ui.gemRed", "260", false),   ("ui.gemRed", "660", false) },
            { ("ui.coin", "5", true),   ("ui.iconScroll", "20", false), ("ui.iconScroll", "60", false) },
            { ("ui.coin", "5", true),   ("ui.iconTicketBlue", "2", false), ("ui.iconTicketBlue", "4", false) },
            { ("ui.coin", "5", false),  ("ui.iconScroll", "20", false), ("ui.iconScroll", "60", false) },
        };
        /// <summary>구간 띠 아래(아직 못 연 행)의 첫 index — 레퍼런스에서 띠가 4번째와 5번째 행 사이에 있다.</summary>
        const int DimFrom = 4;

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

        void BuildTrack()
        {
            var track = UiKit.Rect(Root, "Track"); UiKit.Pct(track, RTrack);

            Column("Col:free", RColFree, "passFree", "col.passFreeDim");
            Column("Col:paid1", RColPaid1, "passPaid1", "col.passPaid1Dim");
            Column("Col:paid2", RColPaid2, "passPaid2", "col.passPaid2Dim");

            var line = UiKit.Panel(Root, "Line", "fr.rect", App.Assets != null ? App.Assets.Color("col.passLine", Palette.Hex("#FFF43B")) : Palette.Hex("#FFF43B")).rectTransform;
            UiKit.Pct(line, RLine);

            for (int i = 0; i < Levels.Length; i++)
            {
                float dy = RowPitch * i;
                bool dim = i >= DimFrom;
                var cFree = Cell("Cell:free:" + Levels[i], RCellFree, dy, Rows[i, 0], dim);
                var cPaid1 = Cell("Cell:paid1:" + Levels[i], RCellPaid1, dy, Rows[i, 1], dim);
                var cPaid2 = Cell("Cell:paid2:" + Levels[i], RCellPaid2, dy, Rows[i, 2], dim);
                if (i == 0)
                {   // 표 ㊼ 는 «첫 행» 만 재고 아래 행은 피치 10.3%p 로 따라온다 — 이름표도 첫 행에만 단다
                    UiKit.Tag(cFree, "보상 칸(무료 · 첫 행)"); UiKit.Tag(cPaid1, "보상 칸(유료 1 · 첫 행)"); UiKit.Tag(cPaid2, "보상 칸(유료 2 · 첫 행)");
                }

                var b = UiKit.Panel(Root, "Badge:" + Levels[i], "fr.r12", Palette.A(dim ? Palette.Ink : Palette.Hex("#96793B"), 0.95f)).rectTransform;
                UiKit.Pct(b, new Layout.R(RLvBadge.X, RLvBadge.Y + dy, RLvBadge.W, RLvBadge.H)); UiKit.Bordered(b);
                UiKit.Label(b, 0, 0, 100, 100, Levels[i].ToString(), TextSize.Aux, dim ? Palette.CreamDark : Palette.White).name = "BadgeText";
                if (i == 0) UiKit.Tag(b, "레벨 배지(행마다)");   // 표 ㊼ 는 첫 배지를 재고 나머지는 피치로 따라온다
            }

            // 구간 노란 띠 + «💎100» 배지 — 위는 연 구간 · 아래는 아직
            var band = UiKit.Panel(Root, "SegBand", "fr.rect", App.Assets != null ? App.Assets.Color("col.passLine", Palette.Hex("#FFF43B")) : Palette.Hex("#FFF43B")).rectTransform;
            UiKit.Pct(band, RSegBand);
            var seg = UiKit.Panel(Root, "SegBadge", "fr.r12", App.Assets != null ? App.Assets.Color("col.passSegBadge", Palette.Hex("#EE9B19")) : Palette.Hex("#EE9B19")).rectTransform;
            UiKit.Pct(seg, RSegBadge); UiKit.Bordered(seg);
            var segIcon = UiKit.Icon(seg, "Icon", "ui.gemRed", Color.white); UiKit.Pct(segIcon.rectTransform, 6, 12, 30, 76);
            UiKit.Label(seg, 36, 0, 60, 100, "100", TextSize.Body, Palette.White).name = "SegText";

            UiKit.Tag(track, "트랙(3열 전체)"); UiKit.Tag(line, "가운데 노란 줄");
            UiKit.Tag(band, "구간 노란 띠"); UiKit.Tag(seg, "구간 배지(«💎100»)");
        }

        /// <summary>열 하나 — 밝은 구간은 실측 그라데이션, 구간 띠 아래(아직 못 연 행)는 실측 단색 어둠을 덮는다.</summary>
        void Column(string name, Layout.R r, string grad, string dimKey)
        {
            var col = UiKit.Panel(Root, name, "fr.rect", Color.white).rectTransform;
            UiKit.Pct(col, r);
            UiKit.GradientCard(col, grad, alpha: UiKit.GradientCardSolidAlpha);
            // «아직 못 연 행» — 열 안에서 구간 띠 아래쪽만 어둡다(레퍼런스 실측은 위아래로 평평한 단색이라 그라데이션이 아니다)
            float dimTop = (RSegBand.Y - r.Y) / r.H * 100f;
            var dim = UiKit.Panel(col, "Dim", "fr.rect", App.Assets != null ? App.Assets.Color(dimKey, Palette.Ink) : Palette.Ink).rectTransform;
            UiKit.Pct(dim, 0f, dimTop, 100f, 100f - dimTop);
            UiKit.Tag(col, name == "Col:free" ? "무료 열(파랑)" : name == "Col:paid1" ? "유료 1 열(주황)" : "유료 2 열(자주)");
        }

        /// <summary>보상 칸 하나 — 아이콘 + 수량, 받은 칸은 초록 체크, 잠긴 칸은 자물쇠(레퍼런스 그대로).</summary>
        RectTransform Cell(string name, Layout.R r, float dy, (string Icon, string Qty, bool Claimed) v, bool dim)
        {
            var cell = UiKit.Panel(Root, name, "fr.itemBg", Palette.A(Palette.Ink, dim ? 0.75f : 0.5f)).rectTransform;
            UiKit.Pct(cell, new Layout.R(r.X, r.Y + dy, r.W, r.H)); UiKit.Bordered(cell);
            var icon = UiKit.Icon(cell, "Icon", v.Icon, dim ? Palette.A(Color.white, 0.45f) : Color.white);
            UiKit.Pct(icon.rectTransform, 14, 8, 72, 66);
            UiKit.Label(cell, 45, 66, 52, 30, v.Qty, TextSize.Aux, dim ? Palette.CreamDark : Palette.White).name = "Qty";
            if (v.Claimed) { var ck = UiKit.Icon(cell, "Check", "pi.check", Palette.Hex("#3FD214")); UiKit.Pct(ck.rectTransform, 18, 18, 64, 64); }
            else if (name.StartsWith("Cell:paid")) { var lk = UiKit.Icon(cell, "Lock", "pi.lock", dim ? Palette.CreamDark : Color.white); UiKit.Pct(lk.rectTransform, 62, -6, 40, 40); }
            return cell;
        }

        void BuildButtons()
        {
            var all = UiKit.Button(Root, "ui.btnGray", "모두 받기", () => App.Toast(NotReadyToast), RClaimAll); all.name = "ClaimAllBtn";
            var b1 = UiKit.Button(Root, "ui.btnOrange", "₩9,900", () => App.Toast(NotReadyToast), RBuy1); b1.name = "BuyBtn:1";
            UiKit.GradientCard(b1, "btnPassPaid1", alpha: UiKit.GradientCardSolidAlpha);
            var b2 = UiKit.Button(Root, "ui.btnOrange", "₩49,000", () => App.Toast(NotReadyToast), RBuy2); b2.name = "BuyBtn:2";
            UiKit.GradientCard(b2, "btnPassPaid2", alpha: UiKit.GradientCardSolidAlpha);

            var back = UiKit.Button(Root, "ui.btnGray", "", () => App.ShowScreen("lobby"), RBack); back.name = "BackBtn";
            { var t = UiKit.ButtonText(back); if (t != null) t.gameObject.SetActive(false); var ic = UiKit.Icon(back, "Icon", "pi.arrow_left", Palette.Cream); UiKit.Pct(ic.rectTransform, 30, 18, 40, 64); }

            var pass = UiKit.Button(Root, "ui.btnGray", PassTitle, () => App.Toast(NotReadyToast), RPassIcon); pass.name = "PassIconBtn";
            { var ic = UiKit.Icon(pass, "Icon", "ui.iconMedal", Color.white); UiKit.Pct(ic.rectTransform, 26, 2, 48, 48); }

            UiKit.Tag(all, "«모두 받기» 버튼"); UiKit.Tag(b1, "«₩9,900» 버튼"); UiKit.Tag(b2, "«₩49,000» 버튼");
            UiKit.Tag(back, "뒤로 버튼"); UiKit.Tag(pass, "패스 아이콘 버튼");
        }

        public override void Refresh() => _top?.Refresh();
    }
}
