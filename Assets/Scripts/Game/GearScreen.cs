using System;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 장비 탭 = 레퍼런스 <c>docs/ref/06_gear.jpg</c> 구도(T37 · 주인 2026-09-06 «UI 는 무조건 레퍼런스 기준» · T25 흡수). ref-layout ③ 표(<see cref="Layout.GearStage"/> …) 자리에 GUI Pro 조각을 다시 조립한다:
    /// ① 상단 재화 바(<see cref="TopBar"/> · 아바타·전투력·골드·보석) → ② <b>캐릭터 무대</b>(Environment 들판·길·나무 · 가운데 큰 플레이어 <see cref="HeroView"/> · 좌 3 / 우 3 슬롯 = ItemFrame_01 조각 + 위 «Lv. N» + 왼쪽 위 부위(세트) 아이콘 + «+N» 배지 + 빨간 점)
    /// → ③ <b>스탯 3칸 한 줄</b>(공 · ❤ · 🛡 · 어두운 상자) → ④ 갈색 띠 위 <b>«상점»(회색 · 왼쪽) · «대장간»(주황 · 오른쪽 · 합성 가능하면 빨간 !)</b> → ⑤ <b>인벤 5열 격자</b>(<see cref="GearUi.Grid"/> · 장착분 숨김 · 칸 = ListItem_EquipMent) → ⑥ 탭 바.
    /// Character_Hero_Equipment 프리팹은 더 이상 통째로 세우지 않는다(격자 값의 원본으로만 · <see cref="GearUi.CopyEquipmentGrid"/>). 규칙·수치는 그대로(자동 장착 없음 · «균등 보너스» 문구 없음).
    /// 이름 계약(스모크 테스트): 슬롯 묶음 <c>Group_Slot</c> → 자식 6 → 각각 <c>ItemFrame_01/Item</c> · 인벤 <c>Content</c> · 탭 바 <c>ui.tabBar</c> · 무대 <c>Stage</c> · 스탯 <c>Stat:*</c> · 버튼 <c>ShopBtn</c>/<c>ForgeBtn</c>.
    /// </summary>
    public sealed class GearScreen : GameScreen
    {
        public override string Name => "gear";
        const int SlotCount = 6;
        /// <summary>«상점» 버튼 자리 — 표에는 «액션바(Forge)» 만 있어(오른쪽 끝) 왼쪽 끝을 같은 줄·같은 크기로 대칭(주인 T25: «상점·합성 버튼을 스탯 3칸 바로 아래로»).</summary>
        public static readonly Layout.R ShopBtn = new Layout.R(100f - Layout.GearForgeBtn.X - Layout.GearForgeBtn.W, Layout.GearForgeBtn.Y, Layout.GearForgeBtn.W, Layout.GearForgeBtn.H);
        /// <summary>버튼 줄 뒤 갈색 띠 — 스탯 줄 아래(40.5%)부터 인벤 격자 위(47.5%)까지.</summary>
        public static readonly Layout.R Band = new Layout.R(0, Layout.GearStats.Y + Layout.GearStats.H + 0.5f, 100, Layout.GearInv.Y - (Layout.GearStats.Y + Layout.GearStats.H + 0.5f));
        /// <summary>무대 안 길 띠(무대 % · 레퍼런스: 아래 1/3 이 흙길) · 나무 5그루(무대 위쪽 가장자리) · 덤불 3.</summary>
        static readonly Layout.R StageRoad = new Layout.R(0, 58, 100, 24);
        static readonly Layout.R[] StageTrees = { new Layout.R(-4, -6, 22, 58), new Layout.R(16, -10, 22, 60), new Layout.R(39, -8, 22, 58), new Layout.R(61, -10, 22, 60), new Layout.R(82, -6, 22, 58) };
        static readonly Layout.R[] StageBushes = { new Layout.R(24, 44, 12, 16), new Layout.R(66, 46, 12, 16), new Layout.R(45, 84, 10, 14) };
        /// <summary>슬롯 칸(147px) 기준 % — 위 «Lv. N» 라벨(본문 40 · 한 줄 49px 이 36% = 53px 에 들어간다 · 아래 끝 = 프레임 위 2%) · «+N» 배지(칸 아래 가장자리에 걸침 · 74~101%). T63-gear.</summary>
        public static readonly Layout.R SlotLv = new Layout.R(-12, -34, 124, 36), SlotBadge = new Layout.R(22, 74, 56, 27);
        /// <summary>«+N» 배지 글자 — 아이콘 위 배지는 ROUTINE T63 1항의 명시 예외(<see cref="TextKind.Small"/>) · 레퍼런스 06 의 «+1»(≈27px 상당)보다 조금 크게 · 배지 높이 40px 에 한 줄(37px).</summary>
        public const int SlotBadgeSize = 30;

        sealed class SlotUi { public RectTransform Root; public Transform Frame; public Text Lv, Plus; public GameObject PlusBadge, Dot; public Image PartIcon; public string Part; }
        readonly SlotUi[] _slot = new SlotUi[SlotCount];
        TopBar _top; HeroView _hero; Transform _content; Text _atk, _hp, _sh; GameObject _forgeDot;

        static string PartAt(int slotIndex) => slotIndex < 3 ? GearUi.ColLeft[slotIndex] : GearUi.ColRight[slotIndex - 3];

        protected override void Build()
        {
            var bg = UiKit.Ensure<Image>(Root.gameObject); bg.color = Color.Lerp(Palette.Slate, Palette.Dim, 0.45f); bg.raycastTarget = true;   // 인벤 바탕 = 어두운 남색(레퍼런스 · 색은 점수 밖)

            // ① 상단 재화 바 — 공용 헬퍼(아바타 · 전투력 · 골드 · 보석)
            _top = TopBar.Build(App, Root);

            // ② 캐릭터 무대 — Environment 들판(전체) + 나무(위 가장자리) + 덤불 + 길 띠(아래 1/3) · 무대 밖은 잘라낸다
            var stage = UiKit.Rect(Root, "Stage"); UiKit.Pct(stage, Layout.GearStage); UiKit.Ensure<RectMask2D>(stage.gameObject);
            {
                var field = UiKit.Icon(stage, "Field", "env.field"); field.preserveAspect = false; UiKit.Stretch(field.rectTransform);
                for (int i = 0; i < StageTrees.Length; i++) { var t = UiKit.Icon(stage, "Tree" + i, "env.tree"); UiKit.Pct(t.rectTransform, StageTrees[i]); }
                var road = UiKit.Icon(stage, "Road", "env.road"); road.preserveAspect = false; UiKit.Pct(road.rectTransform, StageRoad);
                for (int i = 0; i < StageBushes.Length; i++) { var b = UiKit.Icon(stage, "Bush" + i, "env.bush"); UiKit.Pct(b.rectTransform, StageBushes[i]); }
                // 가운데 큰 플레이어 — 표의 «캐릭터» 행 높이(19%)의 정사각 호스트(텍스처가 정사각이라 찌그러지지 않게 · 폭은 높이에서 환산) · 기본 프레이밍이면 몸이 호스트 세로의 ≈89%(T25 «85~90%»)
                float hostW = Layout.GearHero.H * UiKit.FrameH / UiKit.FrameW;
                var host = UiKit.Rect(stage, "Hero"); UiKit.Pct(host, (Layout.GearHero.X + Layout.GearHero.W * 0.5f - hostW * 0.5f - Layout.GearStage.X) / Layout.GearStage.W * 100f, (Layout.GearHero.Y - Layout.GearStage.Y) / Layout.GearStage.H * 100f, hostW / Layout.GearStage.W * 100f, Layout.GearHero.H / Layout.GearStage.H * 100f);
                _hero = HeroView.Attach(host, HeroView.PlayerSkin(App));
            }
            // 슬롯 6칸 — 표의 좌열(무기·목걸이·갑옷) / 우열(투구·장갑·신발) · 칸 = ItemFrame_01 조각(본래 190px · 배율로 표 크기에) · 위 «Lv. N» · 왼쪽 위 세트 아이콘 · «+N» 노란 배지 · 오른쪽 위 빨간 점
            var grp = UiKit.Rect(Root, "Group_Slot"); UiKit.Stretch(grp);
            for (int i = 0; i < SlotCount; i++)
            {
                var col = i < 3 ? Layout.GearSlotColL : Layout.GearSlotColR; int row = i % 3; string part = PartAt(i);
                var s = _slot[i] = new SlotUi { Part = part };
                s.Root = UiKit.Rect(grp, "Slot:" + part); UiKit.Pct(s.Root, col.X, col.Y + row * Layout.GearSlotPitch, Layout.GearSlot.W, Layout.GearSlotH);
                var frame = UiKit.Spawn("ui.itemFrame.empty", s.Root); frame.name = "ItemFrame_01"; s.Frame = frame.transform;
                UiKit.FitScale((RectTransform)s.Frame, UiKit.PxSize(Layout.GearSlot));
                UiKit.Hide(s.Frame, "Text_Level", "Focus", "Disable", "Lock", "Add_2");   // 조각의 데모 글자·상태 켜짐은 끈다 · Add_1(+) 은 빈 슬롯 표시
                // «Lv. N» = 본문 40(T63-gear · 줄 높이 49px ≤ 칸 36% = 53px · 아래 끝이 칸 위 2% 에 걸쳐 레퍼런스처럼 프레임 바로 위) · «+N» 배지 = 아이콘 위 배지라 Small(SlotBadgeSize)
                // 배지(74~101%)와 다음 칸 «Lv. N»(−34~+2%) 은 피치 8.0(틈 40px)에서 잉크가 안 겹친다(UiSmokeTests ② 가 사각형으로 단언)
                s.Lv = UiKit.Label(s.Root, SlotLv.X, SlotLv.Y, SlotLv.W, SlotLv.H, "Lv. 0", TextSize.Body, Palette.White, TextAnchor.LowerCenter);
                s.PartIcon = UiKit.Icon(s.Root, "PartIcon", "pi.attack"); UiKit.Pct(s.PartIcon.rectTransform, -8, -8, 30, 30);
                var badge = UiKit.Panel(s.Root, "PlusBadge", "fr.r12", Palette.Yellow); UiKit.Pct(badge.rectTransform, SlotBadge); s.PlusBadge = badge.gameObject;
                s.Plus = UiKit.Text(badge.transform, "+0", SlotBadgeSize, Palette.Ink, TextAnchor.MiddleCenter, false, false, TextKind.Small); UiKit.Stretch(s.Plus.rectTransform);
                var dot = UiKit.Spawn("ui.alertDot", s.Root); var dr = (RectTransform)dot.transform; dot.name = "Alert_Dot_01_Red"; dr.anchorMin = dr.anchorMax = new Vector2(1, 1); dr.pivot = new Vector2(0.5f, 0.5f); dr.anchoredPosition = new Vector2(-6, -6); dr.sizeDelta = new Vector2(44, 44); s.Dot = dot;
                int idx = i; UiKit.Clickable(s.Root, () => OnSlot(idx));
            }

            // ③ 스탯 3칸 한 줄 — 공 · ❤ · 🛡 (어두운 상자 · 왼쪽 아이콘 · 큰 숫자)
            var c0 = StatCell(0, "atk", "pi.attack", Palette.White, out _atk); var c1 = StatCell(1, "hp", "pi.heart", Palette.Red, out _hp); var c2 = StatCell(2, "sh", "pi.shield", Palette.Sky, out _sh);

            // ④ 갈색 띠 + «상점»(회색 · 왼쪽) · «대장간»(주황 · 오른쪽 · 빨간 !) — 스탯 3칸 바로 아래(주인 T25)
            var band = UiKit.Panel(Root, "Band", "fr.rect", Palette.InkLight); UiKit.Pct(band.rectTransform, Band); band.raycastTarget = true;
            var shop = UiKit.Button(Root, "ui.btnGray", "상점", () => App.ShowScreen("shop"), ShopBtn); shop.name = "ShopBtn";
            var forge = UiKit.Button(Root, "ui.btnOrange", "대장간", () => App.ShowScreen("forge"), Layout.GearForgeBtn); forge.name = "ForgeBtn";
            { var d = UiKit.Spawn("ui.alertDot", forge); var dr = (RectTransform)d.transform; d.name = "ForgeDot"; dr.anchorMin = dr.anchorMax = new Vector2(1, 1); dr.pivot = new Vector2(0.5f, 0.5f); dr.anchoredPosition = new Vector2(-4, 4); dr.sizeDelta = new Vector2(52, 52); _forgeDot = d; }

            // ⑤ 인벤 5열 격자(장비 화면 프리팹의 격자 값 · 칸 = ListItem_EquipMent) → ⑥ 탭 바
            _content = GearUi.Grid(Root, Layout.GearInv, out _);
            NavBar.Attach(this, Root, "gear");
            // 비평 이름표(T46 · ref-layout ③ 의 «요소» 이름 그대로) — «인벤 1칸» 은 Refresh 가 첫 칸에 단다
            UiKit.Tag(_top.Root, "상단 바"); UiKit.Tag(stage, "장비 무대(캐릭터+슬롯)");
            UiKit.TagGroup(grp, "좌 슬롯열(3칸)", _slot[0].Root, _slot[1].Root, _slot[2].Root); UiKit.TagGroup(grp, "우 슬롯열(3칸)", _slot[3].Root, _slot[4].Root, _slot[5].Root); UiKit.Tag(_slot[0].Root, "슬롯 1칸");
            if (_hero != null) UiKit.Tag(_hero.Body, "캐릭터");
            UiKit.TagGroup(Root, "스탯 요약줄(3칸)", c0, c1, c2); UiKit.Tag(forge, "액션바(Forge)"); UiKit.Tag(_content.parent, "인벤 그리드"); UiKit.Tag(UiKit.Find(Root, "ui.tabBar"), "하단 탭바");
        }

        /// <summary>스탯 칸 하나 — 표의 «스탯 요약줄» 을 3등분(칸 사이 2%) · 어두운 상자 + 왼쪽 아이콘 + 숫자. 이름 <c>Stat:&lt;key&gt;</c>. 돌려주는 값 = 칸 사각형(이름표용) · 숫자 글자는 out.</summary>
        RectTransform StatCell(int i, string key, string icon, Color tint, out Text value)
        {
            var r = Layout.GearStats; float gap = 2f, w = (r.W - gap * 2) / 3f;
            var cell = UiKit.Spawn("ui.frameDark", Root); var crt = (RectTransform)cell.transform; crt.name = "Stat:" + key; UiKit.Pct(crt, r.X + i * (w + gap), r.Y, w, r.H);
            var ic = UiKit.Icon(crt, "Icon", icon, tint); UiKit.Pct(ic.rectTransform, 6, 14, 20, 72);
            value = UiKit.Label(crt, 28, 0, 66, 100, "0", 40, Palette.White, TextAnchor.MiddleCenter);
            return crt;
        }

        void OnSlot(int i)
        {
            var s = _slot[i]; var g = App.Save.EquippedGear(s.Part);
            if (g != null) GearUi.OpenDetail(App, g, Refresh); else GearUi.OpenSlot(App, s.Part, Refresh);
        }

        public override void Refresh()
        {
            var D = App.Data; var S = App.Save;
            // 슬롯 6칸 — 등급색 프레임 · 아이콘(T31 Thumbnail · T17 72% 맞춤) · «Lv. N»(슬롯 레벨) · «+N» · 세트 아이콘 · 빨간 점(인벤에 더 좋은 게 있다 · 자동 장착 없음)
            for (int i = 0; i < SlotCount; i++)
            {
                var s = _slot[i]; if (s == null || s.Frame == null) continue;
                var g = S.EquippedGear(s.Part); int lv = S.SlotLv(s.Part);
                var area = UiKit.Find(s.Frame, "NormalArea");
                if (area != null) { UiKit.Clear(area); if (g != null) { var f = UiKit.Spawn("ui.itemFrame." + Palette.RarName(g.Rar), area); UiKit.Stretch((RectTransform)f.transform); } }
                var item = UiKit.Find(s.Frame, "Item");
                if (item != null) { item.gameObject.SetActive(g != null); if (g != null) { var im = UiKit.SetSprite(s.Frame, "Item", GearLook.IconKey(D, g), Palette.White); GearUi.FitIcon(im, g); } }
                UiKit.Show(s.Frame, "Add_1", g == null);
                if (s.Lv != null) s.Lv.text = $"Lv. {lv}";
                if (s.PlusBadge != null) s.PlusBadge.SetActive(g != null && g.Plus > 0); if (s.Plus != null && g != null) s.Plus.text = "+" + g.Plus;
                if (s.PartIcon != null) { s.PartIcon.gameObject.SetActive(g != null); if (g != null) { s.PartIcon.sprite = App.Assets.Sprite(GearUi.SetIcon(GearUi.Set(D, g))); s.PartIcon.color = Palette.White; } }
                if (s.Dot != null) s.Dot.SetActive(GearUi.BetterInInv(S, s.Part));
            }
            // 스탯 · 상단 바 · 대장간 ! · 캐릭터 외형
            var pw = GearSystem.BuildPower(D, S.CurBuild(D));
            if (_atk != null) _atk.text = UiKit.Fmt(Math.Round(pw.Atk)); if (_hp != null) _hp.text = UiKit.Fmt(Math.Round(pw.Hp)); if (_sh != null) _sh.text = UiKit.Fmt(Math.Round(pw.Sh));
            _top?.Refresh();
            if (_forgeDot != null) _forgeDot.SetActive(GearUi.FusableKeys(S).Count > 0);
            _hero?.SetSkin(HeroView.PlayerSkin(App));
            // 인벤 격자 — 장착 중인 장비는 숨긴다(«장착중» 배지 없음 · 합성 점도 여기서는 끔)
            if (_content != null)
            {
                UiKit.Clear(_content); int shown = 0;
                foreach (var g in GearUi.Sorted(S))
                {
                    if (S.IsEquipped(g)) continue;
                    shown++; var gi = g;
                    var cell = GearUi.Cell(_content, D, g, new GearUi.CellOpts { IsNew = g.IsNew }, () => GearUi.OpenDetail(App, gi, Refresh));
                    if (shown == 1) UiKit.Tag(cell, "인벤 1칸");   // 비평 이름표 — 첫 칸(표 «인벤 1칸» 3.0/47.8/18.4/7.2)
                }
                if (shown == 0) GearUi.Empty(_content, S.Inv.Count == 0 ? "장비가 없습니다.\n상점에서 뽑기로 장비를 얻으세요." : "장착하지 않은 장비가 없습니다.");
            }
            NavBar.Refresh(Root);
        }
    }
}
