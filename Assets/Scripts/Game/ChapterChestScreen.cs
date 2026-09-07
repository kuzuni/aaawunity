using System;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 챕터 보상(Chapter Chest) 페이지 — 레퍼런스 <c>32_lobby_clear.jpg</c> (T98 · 주인 2026-09-07 «로비 → 클리어 보상»).
    /// 구도(표 ㉝): 상단 재화 바 → 파란 리본 «챕터 보상» → 부제 «챕터 N 에서 적 M 처치» → <b>큰 금테 배너</b>(제목 pill «챕터 N» + 목표 두 줄)
    /// 와 좌우로 살짝 보이는 이웃 챕터 → «보상» 상자(다이아·골드 칸) → «받기»(못 받으면 회색) → 바닥 띠 + 왼쪽 아래 뒤로.
    /// <para>
    /// 레퍼런스의 «금테 방패» 그림은 주인 에셋에 없다 → <c>CardFrame_04_Yellow</c> 조각으로 같은 구도를 세운다
    /// (ROUTINE ⓑ «부품으로 뜯어 레퍼런스 구도로 재조립» · 새 그림 0). 규칙·수치는 전부 <see cref="ChapterChest"/>(순수 C#)에 있고 여기서는 그리기만 한다.
    /// </para>
    /// 이름 계약(스모크 테스트): 배너 <c>Banner</c>(이웃 = <c>Banner:prev</c>/<c>Banner:next</c>) · 제목 <c>BannerTitle</c> · 목표 <c>BannerGoal</c> ·
    /// 부제 <c>Sub</c> · 보상 칸 <c>Cell:gem</c>/<c>Cell:gold</c> · 버튼 <c>ClaimBtn</c>/<c>BackBtn</c>.
    /// </summary>
    public sealed class ChapterChestScreen : GameScreen
    {
        public override string Name => "chapterChest";

        /// <summary>부모가 화면 전체인 rect(칸 % 를 그대로 프레임 % 로 쓴다).</summary>
        static readonly Layout.R Screen100 = new Layout.R(0, 0, 100, 100);
        /// <summary>아직 값이 없을 때(데이터 표가 없거나 범위 밖) 보여 주는 자리표.</summary>
        const string Dash = "--";

        TopBar _top;
        RectTransform _banner, _prev, _next, _title, _goal, _rewardBox;
        Text _sub, _titleText, _goalText, _gemQty, _goldQty;
        RectTransform _claim;
        int _sel = 1;

        /// <summary>페이지를 연다 — 받을 수 있는 가장 앞 챕터부터 보여 준다(없으면 도전 중인 챕터).</summary>
        public static void Open(App app)
        {
            var sc = app != null ? app.GetScreen<ChapterChestScreen>() : null;
            if (sc != null) sc._sel = ChapterChest.FirstOpen(app.Data, app.Save);
            app?.ShowScreen("chapterChest");
        }

        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Color.Lerp(Palette.Slate, Palette.Dim, 0.6f); bg.raycastTarget = true;
            UiKit.PatternBg(Root, UiKit.PatternTintDark);   // T72 ① 풀스크린 배경 무늬(레퍼런스 32 도 어두운 바탕에 옅은 무늬)
            _top = TopBar.Build(App, Root);

            // 제목 리본 — 레퍼런스 32 의 파란 «Chapter Chest»
            var rib = UiKit.Spawn("ui.title.sky", Root); var rrt = (RectTransform)rib.transform; rib.name = "Ribbon";
            UiKit.Pct(rrt, Layout.CcRibbon); Overlay.FitRibbonText(rrt);
            UiKit.SetText(rrt, "Text (TMP)", "챕터 보상", Palette.Cream, TextSize.Title, TextKind.Title);

            _sub = UiKit.Label(Root, Layout.CcSub.X, Layout.CcSub.Y, Layout.CcSub.W, Layout.CcSub.H, "", TextSize.Body, Palette.CreamDark);
            _sub.name = "Sub";

            // 배너 셋 — 가운데(현재)와 좌우 이웃(가장자리만 보인다 · 누르면 그 챕터로 넘어간다)
            _prev = Banner("Banner:prev", -Layout.CcBannerPitch, () => Shift(-1));
            _next = Banner("Banner:next", Layout.CcBannerPitch, () => Shift(+1));
            _banner = Banner("Banner", 0, null);
            _title = UiKit.Find(_banner, "BannerTitle") as RectTransform;
            _goal = UiKit.Find(_banner, "BannerGoal") as RectTransform;
            _titleText = _title != null ? _title.GetComponentInChildren<Text>(true) : null;
            _goalText = _goal != null ? _goal.GetComponentInChildren<Text>(true) : null;

            // 보상 상자 — 어두운 판 + 머리 «보상» + 칸 2(다이아·골드)
            var box = UiKit.Panel(Root, "RewardBox", "fr.r12", Palette.A(Palette.Ink, 0.55f));
            _rewardBox = box.rectTransform; UiKit.Pct(_rewardBox, Layout.CcRewardBox);
            UiKit.Bordered(_rewardBox);   // T69 — 칸·상자에는 검은 아웃라인
            UiKit.Label(_rewardBox, Layout.CcRewardHead.X, Layout.CcRewardHead.Y, Layout.CcRewardHead.W, Layout.CcRewardHead.H, "보상", TextSize.Body, Palette.Cream).fontStyle = FontStyle.Bold;
            var gemCell = LobbyPopups.Cell(Root, Screen100, Layout.CcRewardCell, "plum", "ui.gemRed", Dash, false, "Cell:gem");
            var goldRect = new Layout.R(Layout.CcRewardCell.X + Layout.CcRewardPitch, Layout.CcRewardCell.Y, Layout.CcRewardCell.W, Layout.CcRewardCell.H);
            var goldCell = LobbyPopups.Cell(Root, Screen100, goldRect, "green", "ui.coin", Dash, false, "Cell:gold");
            _gemQty = QtyOf(gemCell); _goldQty = QtyOf(goldCell);

            _claim = UiKit.Button(Root, "ui.btnOrange", "받기", OnClaim, Layout.CcClaim); _claim.name = "ClaimBtn";

            // 바닥 띠 + 왼쪽 아래 뒤로(대장간과 같은 이름 계약)
            var foot = UiKit.Panel(Root, "FootBar", "fr.rect", Palette.A(Palette.Slate, 0.95f)); UiKit.Pct(foot.rectTransform, Layout.CcFootBar);
            var back = UiKit.Button(Root, "ui.btnGray", "", () => App.ShowScreen("lobby"), Layout.CcBack); back.name = "BackBtn";
            { var t = UiKit.ButtonText(back); if (t != null) t.gameObject.SetActive(false); var ic = UiKit.Icon(back, "Icon", "pi.arrow_left", Palette.Cream); UiKit.Pct(ic.rectTransform, 30, 18, 40, 64); }

            // 비평 이름표(T46 · 표 ㉝ 의 «요소» 글자 그대로)
            UiKit.Tag(rrt, "제목 리본(챕터 보상)"); UiKit.Tag(_sub.rectTransform, "부제(챕터 N 에서 적 M 처치)");
            UiKit.Tag(_banner, "챕터 배너(가운데)"); if (_title != null) UiKit.Tag(_title, "배너 제목 pill(챕터 N)"); if (_goal != null) UiKit.Tag(_goal, "배너 목표 글자(적 M 처치)");
            UiKit.Tag(_rewardBox, "보상 상자"); UiKit.Tag(gemCell, "보상 칸(다이아)"); UiKit.Tag(_claim, "받기 버튼");
            UiKit.Tag(foot.transform, "바닥 띠"); UiKit.Tag(back, "뒤로(◀)");
        }

        /// <summary>배너 한 장 = <c>CardFrame_04_Yellow</c> 조각(제목 띠 + 몸통). 이웃은 흐리게 깔고 누르면 그 챕터로 넘어간다.</summary>
        RectTransform Banner(string name, float dx, Action onClick)
        {
            var go = UiKit.Spawn("ui.cardFrame.yellow", Root); go.name = name;
            var rt = (RectTransform)go.transform;
            UiKit.Pct(rt, new Layout.R(Layout.CcBanner.X + dx, Layout.CcBanner.Y, Layout.CcBanner.W, Layout.CcBanner.H));
            // 조각의 제목 띠(TitleBg) 자리에 «챕터 N» · 몸통에 목표 두 줄 — 조각 요소를 옮기지 않고 글자만 얹는다
            var title = UiKit.Rect(rt, "BannerTitle"); UiKit.Pct(title, Layout.CcBannerTitle);
            UiKit.Label(title, 0, 0, 100, 100, "", TextSize.Body, Palette.Cream).name = "TitleText";
            var goal = UiKit.Rect(rt, "BannerGoal"); UiKit.Pct(goal, Layout.CcBannerGoal);
            var gt = UiKit.Label(goal, 0, 0, 100, 100, "", TextSize.Title, Palette.White, TextAnchor.MiddleCenter, true, true, TextKind.Title);
            gt.name = "GoalText"; gt.fontStyle = FontStyle.Bold;
            if (onClick != null) UiKit.Clickable(rt, onClick);
            else { var cg = UiKit.Ensure<CanvasGroup>(go); cg.blocksRaycasts = false; }
            return rt;
        }

        static Text QtyOf(RectTransform cell)
        {
            var t = UiKit.Find(cell, "Qty");
            return t != null ? t.GetComponent<Text>() : cell.GetComponentInChildren<Text>(true);
        }

        void Shift(int d)
        {
            int max = Math.Max(1, Math.Min(App.Save.MaxChapter, App.Data.Tune.MaxChapter));
            _sel = Mathf.Clamp(_sel + d, 1, max); Refresh();
        }

        void OnClaim()
        {
            if (!ChapterChest.Claim(App.Data, App.Save, _sel, out var gem, out var gold)) { App.Toast("아직 받을 수 없습니다"); return; }
            App.Save.Gem += gem; App.Save.Gold += gold; App.Persist();
            Audio.Sfx("snd.coin");
            App.Toast($"챕터 {_sel} 보상 — 다이아 {UiKit.Fmt(gem)} · 골드 {UiKit.Fmt(gold)}");
            Refresh();
        }

        public override void Refresh()
        {
            var D = App.Data; var S = App.Save;
            int max = Math.Max(1, Math.Min(S.MaxChapter, D.Tune.MaxChapter));
            _sel = Mathf.Clamp(_sel, 1, max);
            var info = ChapterChest.At(D, S, _sel);
            bool ok = info.Chapter != 0;

            if (_sub != null) _sub.text = ok ? $"챕터 {info.Chapter} 에서 적 {info.Kills} 처치" : Dash;
            if (_titleText != null) _titleText.text = ok ? $"챕터 {info.Chapter}" : Dash;
            if (_goalText != null) _goalText.text = ok ? $"적 {info.Kills}\n처치" : Dash;
            if (_gemQty != null) _gemQty.text = ok ? UiKit.Fmt(info.Gem) : Dash;
            if (_goldQty != null) _goldQty.text = ok ? UiKit.Fmt(info.Gold) : Dash;

            // 이웃 배너 — 범위 밖이면 감춘다(첫 챕터 왼쪽 · 마지막 챕터 오른쪽)
            if (_prev != null) _prev.gameObject.SetActive(_sel > 1);
            if (_next != null) _next.gameObject.SetActive(_sel < max);
            Dim(_prev); Dim(_next);
            NeighborText(_prev, _sel - 1); NeighborText(_next, _sel + 1);

            // 받기 — 받을 수 있으면 주황·눌림 · 이미 받았으면 «받음» · 아직이면 회색
            if (_claim != null)
            {
                var t = UiKit.ButtonText(_claim);
                if (t != null) t.text = info.Claimed ? "받음" : "받기";
                UiKit.SetInteractable(_claim.GetComponent<Button>(), info.Claimable);
                var cg = UiKit.Ensure<CanvasGroup>(_claim.gameObject); cg.alpha = info.Claimable ? 1f : 0.5f;
            }
            _top?.Refresh();
        }

        static void Dim(RectTransform rt)
        {
            if (rt == null) return;
            var cg = UiKit.Ensure<CanvasGroup>(rt.gameObject); cg.alpha = 0.55f;
        }

        void NeighborText(RectTransform rt, int chapter)
        {
            if (rt == null || !rt.gameObject.activeSelf) return;
            var tt = UiKit.Find(rt, "BannerTitle"); var gg = UiKit.Find(rt, "BannerGoal");
            var info = ChapterChest.At(App.Data, App.Save, chapter);
            var t1 = tt != null ? tt.GetComponentInChildren<Text>(true) : null;
            var t2 = gg != null ? gg.GetComponentInChildren<Text>(true) : null;
            if (t1 != null) t1.text = info.Chapter != 0 ? $"챕터 {info.Chapter}" : "";
            if (t2 != null) t2.text = info.Chapter != 0 ? $"적 {info.Kills}\n처치" : "";
        }
    }
}
