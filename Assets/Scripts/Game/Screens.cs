using System;
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
        Text _gold, _gem, _chap, _sub, _power;
        Transform _tabs;

        protected override void Build()
        {
            var root = UiKit.Spawn("ui.lobby", Root); var rt = (RectTransform)root.transform; UiKit.Stretch(rt);
            _gold = UiKit.SetText(rt, "ResourceBar_Coin/Text (TMP)", "0"); _gem = UiKit.SetText(rt, "ResourceBar_Gem/Text (TMP)", "0");
            // 유저 정보 칸 → 꼬마기사 · 전투력
            var ui = UiKit.FindAny(rt, "UserInfo_01", "Slider_02_Yellow");
            if (ui != null)
            {
                UiKit.SetText(ui, "Text_UserName", "꼬마기사"); UiKit.Hide(ui, "Icon");
                _power = UiKit.SetText(ui, "Text_GuildName", "전투력 0", Palette.InkLight);
                var lv = UiKit.Find(ui, "Slider_Level_01"); if (lv != null) { UiKit.SetText(lv, "Slider_02_Yellow/Text (TMP)", ""); UiKit.Hide(lv, "Icon"); UiKit.SetText(lv, "Level/Text (TMP)", ""); }
                UiKit.SetSprite(ui, "Character", "ui.battle", Palette.White);
            }
            UiKit.Hide(rt, "Button_Menu", "Group_LeftButtons", "ChatBox");
            var right = UiKit.Find(rt, "Group_RightButtons");
            if (right != null) { UiKit.Hide(right, "Button_Mission"); var inv = UiKit.Find(right, "Button_Inventory"); if (inv != null) { UiKit.SetText(inv, "Text (TMP)", "장비"); UiKit.Clickable(inv, () => App.ShowScreen("gear")); } }
            _chap = UiKit.SetText(rt, "Title_LineDeco_01_Blue/Text (TMP)", "챕터 1");
            _sub = UiKit.SetText(rt, "Text (TMP)", "꼬마기사 키우기"); if (_sub != null) { _sub.resizeTextForBestFit = true; _sub.resizeTextMaxSize = 60; }
            var start = UiKit.FindAny(rt, "Button_03_Red", "Button_03_Convex_Red"); if (start != null) { UiKit.SetText(start, "Text (TMP)", "START"); UiKit.Clickable(start, () => App.StartBattle(App.Save.SelChapter)); }
            // 챕터 ◀ ▶
            UiKit.Button(Root, "ui.btnSmallBlue", "◀", () => Shift(-1), Layout.LobbyArrowL);
            UiKit.Button(Root, "ui.btnSmallBlue", "▶", () => Shift(1), Layout.LobbyArrowR);
            // 하단 탭 5칸: 상점 · 장비 · 전투(가운데) · 대장간 · 설정
            _tabs = UiKit.Find(rt, "Tab_01_BottomFlushMenu");
            if (_tabs != null)
            {
                string[] icons = { "ui.shop", "ui.bag", "ui.battle", "ui.anvil", "ui.settings" };
                string[] labels = { "상점", "장비", "전투", "대장간", "설정" };
                Action[] acts = { () => App.ShowScreen("shop"), () => App.ShowScreen("gear"), () => { }, () => App.ShowScreen("forge"), () => App.Overlay.Pause(() => { }, () => { }) };
                for (int i = 0; i < _tabs.childCount && i < 5; i++)
                {
                    var tab = _tabs.GetChild(i); int k = i;
                    UiKit.SetSprite(tab, "Normal/Icon", icons[i], Palette.White); UiKit.SetSprite(tab, "Focus/Icon_Focus", icons[i], Palette.White);
                    UiKit.SetText(tab, "Focus/Text (TMP)", labels[i]);
                    UiKit.Show(tab, "Focus", i == 2); UiKit.Show(tab, "Normal", i != 2);
                    UiKit.Clickable(tab, () => acts[k]());
                }
            }
        }
        void Shift(int d)
        {
            var s = App.Save; int max = Math.Max(1, s.MaxChapter);
            s.SelChapter = Mathf.Clamp(s.SelChapter + d, 1, max); App.Persist(); Refresh();
        }
        public override void Refresh()
        {
            var s = App.Save;
            if (_gold != null) _gold.text = UiKit.Fmt(s.Gold);
            if (_gem != null) _gem.text = UiKit.Fmt(s.Gem);
            if (_chap != null) _chap.text = $"챕터 {s.SelChapter}  (최고 {s.MaxChapter})";
            if (_power != null) _power.text = "전투력 " + UiKit.Fmt(App.Power());
        }
    }

    // 4단계에서 채운다 — 지금은 로비로 돌아가는 뼈대
    public abstract class StubScreen : GameScreen
    {
        protected abstract string Title { get; }
        protected override void Build()
        {
            var bg = Root.gameObject.AddComponent<Image>(); bg.color = Palette.Hex("#86E4FF"); bg.raycastTarget = true;
            var box = UiKit.SpawnRt("ui.popup", Root, new Layout.R(8, 30, 84, 30));
            UiKit.Label(box, 6, 12, 88, 30, Title, 48, Palette.Ink);
            UiKit.Label(box, 6, 42, 88, 20, "4단계에서 채워집니다", 32, Palette.InkLight);
            UiKit.Button(box, "ui.btnBlue", "로비로", () => App.ShowScreen("lobby"), new Layout.R(25, 66, 50, 24));
        }
    }
    public sealed class GearScreen : StubScreen { public override string Name => "gear"; protected override string Title => "장비"; }
    public sealed class ForgeScreen : StubScreen { public override string Name => "forge"; protected override string Title => "대장간"; }
    public sealed class ShopScreen : StubScreen { public override string Name => "shop"; protected override string Title => "상점"; }
}
