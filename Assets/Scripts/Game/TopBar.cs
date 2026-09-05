using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 상단 바 — ref-layout ①③⑤ 공통(⑧: 모든 탭에서 같은 y 3.7 · h 4.5). 아바타(x2.5 w10.2) + 재화 pill 줄(x13.2 w85.4 · 골드·젬 2칸 + 전투력).
    /// 그림은 GUI Pro ResourceBar_Group(3번째 칸을 전투력으로 켠다) + ItemFrame_01_Normal_Blue(아바타 틀).
    /// </summary>
    public sealed class TopBar
    {
        public RectTransform Root; Text _gold, _gem, _power;

        public static TopBar Attach(RectTransform parent, App app)
        {
            var t = new TopBar();
            t.Root = UiKit.Rect(parent, "TopBar"); UiKit.Pct(t.Root, Layout.LobbyTopBar);
            // 아바타
            var av = UiKit.Spawn("ui.itemFrame.blue", parent); var ar = (RectTransform)av.transform; UiKit.Pct(ar, Layout.LobbyAvatar);
            var ic = UiKit.Icon(ar, "Knight", "ui.battle"); UiKit.Pct(ic.rectTransform, 12, 12, 76, 76);
            av.transform.SetParent(t.Root, true); ar.localScale = Vector3.one;
            // 재화 pill 줄 (ResourceBar_Group · 3칸)
            var bar = UiKit.Spawn("ui.resourceBar", t.Root); var br = (RectTransform)bar.transform;
            var brWithin = Layout.LobbyPills.Within(Layout.LobbyTopBar); UiKit.Pct(br, brWithin);
            var hl = bar.GetComponent<HorizontalLayoutGroup>();
            if (hl != null) { hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandHeight = true; hl.spacing = 12; hl.padding = new RectOffset(0, 0, 0, 0); }
            var le = bar.GetComponent<LayoutElement>(); if (le != null) le.ignoreLayout = true;
            t._gold = UiKit.SetText(br, "ResourceBar_Coin/Text (TMP)", "0");
            t._gem = UiKit.SetText(br, "ResourceBar_Gem/Text (TMP)", "0");
            var pw = UiKit.Find(br, "ResourceBar_GemStone");
            if (pw != null) { pw.gameObject.SetActive(true); var pr = (RectTransform)pw; pr.anchorMin = pr.anchorMax = new Vector2(0, 0); UiKit.SetSprite(pw, "Icon", "ui.battle", Palette.White); t._power = UiKit.SetText(pw, "Text (TMP)", "0"); }
            t.Refresh(app);
            return t;
        }
        public void Refresh(App app)
        {
            if (_gold != null) _gold.text = UiKit.Fmt(app.Save.Gold);
            if (_gem != null) _gem.text = UiKit.Fmt(app.Save.Gem);
            if (_power != null) _power.text = UiKit.Fmt(app.Power());
        }
    }
}
