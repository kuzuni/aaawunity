using System;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 로비 — 주인 지정 GUI Pro 데모 프리팹 **Lobby_Default 를 원형 그대로** 세운다(T6 · 주인 지시 2026-09-05 «레이아웃 좋은데 바꿔버렸다»).
    /// 프리팹 안 요소는 자리를 옮기지 않고(Pct 재앵커링 없음) 글자·숫자만 우리 데이터로 바꾼다.
    /// ● 상단 바(UserInfo_01): 맨 왼쪽 초상 = 내 플레이어 모습(<see cref="HeroView"/> · CharacterMaker) · «25 / 55» 자리 = 전투력 · 그 위 ResourceBar = 골드 · 보석.
    /// ● 왼쪽 상단 아이콘·오른쪽 상단 아이콘 2개: 보이되 기능 없음(나중 업데이트). 메뉴(≡) = 일시정지/설정 팝업.
    /// ● 챕터 제목(«Battle 1» 자리) · 카드 · START · 하단 탭 = 프리팹 자리 그대로. 좌우 ◀▶ 는 프리팹에 없어 카드 양옆에 작은 버튼 2개만 둔다(유일한 추가).
    /// ● 배너(«꼬마기사 키우기»)·공통 TopBar·오른쪽 상단 «장비» 배선은 폐기. ChatBox(데모 채팅 줄)만 끈다(이 게임에 채팅이 없다).
    /// </summary>
    public sealed class LobbyScreen : GameScreen
    {
        public override string Name => "lobby";
        Text _chap, _sub, _guild, _power, _level, _gold, _gem;
        HeroView _hero;
        Transform _tabs;

        protected override void Build()
        {
            var root = UiKit.Spawn("ui.lobby", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            UiKit.Hide(rt, "ChatBox");

            // 상단 바 = 프리팹의 UserInfo_01(초상 · 이름 · «25 / 55» 슬라이더) — 초상 자리에 내 플레이어, 슬라이더 자리에 전투력
            var ui = UiKit.FindAny(rt, "UserInfo_01", "UserInfo_01_Slider");
            if (ui != null)
            {
                ui.gameObject.SetActive(true);
                var mask = UiKit.FindAny(ui, "Bg_MainColor(Mask)", "ProfileFrame_02_Yellow");
                if (mask != null) { UiKit.Hide(mask, "Character"); _hero = HeroView.Attach((RectTransform)mask, HeroView.PlayerSkin(App)); }
                UiKit.SetText(ui, "Text_UserName", "꼬마기사");
                _guild = UiKit.SetText(ui, "Text_GuildName", "최고 챕터 1");
                var lv = UiKit.Find(ui, "Slider_Level_01");
                if (lv != null)
                {
                    var bar = UiKit.Find(lv, "Slider_02_Yellow");
                    if (bar != null)
                    {
                        var sl = bar.GetComponentInChildren<Slider>(true);   // 전투력은 비율이 아니라 값 — 게이지는 꽉 채워 둔다
                        if (sl != null) { sl.interactable = false; sl.transition = Selectable.Transition.None; sl.minValue = 0; sl.maxValue = 1; sl.value = 1; }
                        _power = bar.GetComponentInChildren<Text>(true);
                        if (_power != null) { _power.text = "전투력 0"; _power.resizeTextForBestFit = true; _power.resizeTextMinSize = 12; _power.horizontalOverflow = HorizontalWrapMode.Overflow; }
                    }
                    _level = UiKit.SetText(lv, "Level/Text (TMP)", "1");
                }
            }
            // 재화 = 프리팹의 ResourceBar_Group(골드 · 보석) 그대로 · 세 번째 칸(GemStone)은 프리팹처럼 꺼진 채
            var res = UiKit.Find(rt, "ResourceBar_Group");
            if (res != null) { _gold = UiKit.SetText(res, "ResourceBar_Coin/Text (TMP)", "0"); _gem = UiKit.SetText(res, "ResourceBar_Gem/Text (TMP)", "0"); }
            // 메뉴(≡) → 일시정지/설정 팝업 (T10 이 Settings «그대로» 로 다듬는다)
            var menu = UiKit.Find(rt, "Button_Menu"); if (menu != null) UiKit.Clickable(menu, () => App.Overlay.Pause(() => { }, () => { }));
            // 왼쪽·오른쪽 상단 아이콘 — 프리팹 그대로 보이고 기능 없음(주인: «나중 업데이트»). 데모 영문 라벨만 우리말로.
            UiKit.SetText(rt, "Button_Ticket/Text (TMP)", "티켓"); UiKit.SetText(rt, "Button_ADRemove/Text (TMP)", "광고 제거");
            UiKit.SetText(rt, "Button_Mission/Text (TMP)", "미션"); UiKit.SetText(rt, "Button_Inventory/Text (TMP)", "가방");
            // 챕터 제목(«Battle 1» 자리) · 부제(«Whisperwood» 자리 = 이번 챕터의 전투 맵 이름)
            _chap = UiKit.SetText(rt, "Title_LineDeco_01_Blue/Text (TMP)", "챕터 1");
            var subT = UiKit.Find(rt, "Text (TMP)"); if (subT != null && subT.parent == rt) _sub = subT.GetComponent<Text>();
            // START — 프리팹 자리 그대로
            var start = UiKit.FindAny(rt, "Button_03_Red", "Button_03_Convex_Red"); if (start != null) { UiKit.SetText(start, "Text (TMP)", "START"); UiKit.Clickable(start, () => App.StartBattle(App.Save.SelChapter)); }
            // 챕터 ◀ ▶ — 프리팹에 없는 유일한 추가 · 카드(SampleImage_Map) 양옆 세로 가운데
            var map = UiKit.Find(rt, "SampleImage_Map") as RectTransform;
            float cardCx = UiKit.FrameW / 2f, cardCy = UiKit.FrameH / 2f, cardHalfW = 286f;
            if (map != null) { cardCx += map.anchoredPosition.x; cardCy -= map.anchoredPosition.y; cardHalfW = map.sizeDelta.x / 2f; }
            var left = UiKit.Button(rt, "ui.btnSmallBlue", "◀", () => Shift(-1)); UiKit.Px(left, cardCx - cardHalfW - 78f, cardCy, 128f, 70f);
            var right = UiKit.Button(rt, "ui.btnSmallBlue", "▶", () => Shift(1)); UiKit.Px(right, cardCx + cardHalfW + 78f, cardCy, 128f, 70f);
            // 하단 탭 5칸 — 프리팹 자리 그대로 (탭 구성은 T10)
            _tabs = UiKit.Find(rt, "Tab_01_BottomFlushMenu");
            if (_tabs != null) NavBar.Wire(App, _tabs, "lobby");
        }
        void Shift(int d)
        {
            var s = App.Save; int max = Math.Max(1, s.MaxChapter);
            s.SelChapter = Mathf.Clamp(s.SelChapter + d, 1, max); App.Persist(); Refresh();
        }
        /// <summary>전투 맵 테마(BattleWorld.Theme · 챕터 (n−1)%4 순환) 의 우리말 이름 — 로비 부제.</summary>
        static string ThemeLabel(int chapter)
        {
            switch (BattleWorld.Theme.ForChapter(chapter).Name)
            {
                case "autumn": return "가을 숲";
                case "deepForest": return "깊은 숲";
                case "forest": return "숲";
                case "desert": return "사막";
                default: return "";
            }
        }
        public override void Refresh()
        {
            var s = App.Save;
            if (_chap != null) _chap.text = $"챕터 {s.SelChapter}";
            if (_sub != null) _sub.text = ThemeLabel(s.SelChapter);
            if (_guild != null) _guild.text = $"최고 챕터 {s.MaxChapter}";
            if (_level != null) _level.text = s.MaxChapter.ToString();
            if (_power != null) _power.text = $"전투력 {UiKit.Fmt(App.Power())}";
            if (_gold != null) _gold.text = UiKit.Fmt(s.Gold);
            if (_gem != null) _gem.text = UiKit.Fmt(s.Gem);
            _hero?.SetSkin(HeroView.PlayerSkin(App));
        }
    }
}
