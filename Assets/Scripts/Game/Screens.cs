using System;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 로비 — 주인 지정 GUI Pro 데모 프리팹 Lobby_Default 를 그대로 세우고 글자·버튼만 이 게임 것으로 바꾼다.
    /// (3단계: 챕터 선택 ◀▶ · START · 하단 탭. 장비/대장간/상점 탭 내용은 4단계.)
    /// </summary>
    public sealed class LobbyScreen : GameScreen
    {
        public override string Name => "lobby";
        Text _chap, _sub;
        Transform _tabs; TopBar _top;

        protected override void Build()
        {
            var root = UiKit.Spawn("ui.lobby", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            // ⑧ 상단 바(아바타 + 골드·젬·전투력 pill) — 프리팹의 ResourceBar/UserInfo 대신 공통 TopBar 를 표 자리에
            UiKit.Hide(rt, "ResourceBar_Group", "Group_LeftButtons", "ChatBox"); var ui = UiKit.FindAny(rt, "UserInfo_01", "Slider_02_Yellow"); if (ui != null) ui.gameObject.SetActive(false);
            _top = TopBar.Attach(Root, App);
            var menu = UiKit.Find(rt, "Button_Menu"); if (menu != null) { UiKit.Pct((RectTransform)menu, Layout.LobbyMenu); UiKit.Clickable(menu, () => App.Overlay.Pause(() => { }, () => { })); }
            var right = UiKit.Find(rt, "Group_RightButtons");
            if (right != null) { UiKit.Pct((RectTransform)right, Layout.LobbySideR); UiKit.Hide(right, "Button_Mission"); var inv = UiKit.Find(right, "Button_Inventory"); if (inv != null) { UiKit.SetText(inv, "Text (TMP)", "장비"); UiKit.Clickable(inv, () => App.ShowScreen("gear")); } }
            // 챕터 제목 + 밑줄 (표 ① 제목 34.7/27.2 · 밑줄 29.6/30.0 → 한 프리팹이 둘을 덮는다)
            var title = UiKit.Find(rt, "Title_LineDeco_01_Blue");
            if (title != null) UiKit.Pct((RectTransform)title, Layout.LobbyChapUnderline.X, Layout.LobbyChapTitle.Y, Layout.LobbyChapUnderline.W, Layout.LobbyChapUnderline.Y + Layout.LobbyChapUnderline.H - Layout.LobbyChapTitle.Y);
            _chap = UiKit.SetText(rt, "Title_LineDeco_01_Blue/Text (TMP)", "챕터 1");
            // 이벤트 배너 자리(형태만) — 게임 이름
            var banner = UiKit.SpawnRt("ui.frameIvory", rt, Layout.LobbyBanner);
            _sub = UiKit.SetText(rt, "Text (TMP)", "꼬마기사 키우기", Palette.Ink, 40); if (_sub != null) { _sub.transform.SetParent(banner, false); UiKit.Stretch(_sub.rectTransform, 12, 4, 12, 4); _sub.resizeTextForBestFit = true; _sub.resizeTextMaxSize = 40; }
            var map = UiKit.Find(rt, "SampleImage_Map"); if (map != null) { UiKit.Pct((RectTransform)map, Layout.LobbyCard); var mi = map.GetComponent<Image>(); if (mi != null) mi.preserveAspect = true; }
            var start = UiKit.FindAny(rt, "Button_03_Red", "Button_03_Convex_Red"); if (start != null) { UiKit.Pct((RectTransform)start, Layout.LobbyStart); UiKit.SetText(start, "Text (TMP)", "START"); UiKit.Clickable(start, () => App.StartBattle(App.Save.SelChapter)); }
            // 챕터 ◀ ▶
            UiKit.Button(Root, "ui.btnSmallBlue", "◀", () => Shift(-1), Layout.LobbyArrowL);
            UiKit.Button(Root, "ui.btnSmallBlue", "▶", () => Shift(1), Layout.LobbyArrowR);
            // 하단 탭 5칸: 상점 · 장비 · 전투(가운데) · 대장간 · 설정
            _tabs = UiKit.Find(rt, "Tab_01_BottomFlushMenu");
            if (_tabs != null) { UiKit.Pct((RectTransform)_tabs, Layout.TabBar); NavBar.Wire(App, _tabs, "lobby"); }
        }
        void Shift(int d)
        {
            var s = App.Save; int max = Math.Max(1, s.MaxChapter);
            s.SelChapter = Mathf.Clamp(s.SelChapter + d, 1, max); App.Persist(); Refresh();
        }
        public override void Refresh()
        {
            var s = App.Save;
            _top?.Refresh(App);
            if (_chap != null) _chap.text = $"챕터 {s.SelChapter}  (최고 {s.MaxChapter})";
        }
    }
}
