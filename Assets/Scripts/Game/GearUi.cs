using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 장비 그리기 공통 — 인벤 칸(ItemFrame_01_Normal_* 등급색) · 이름 · 정렬 · 합성 가능 키. 장비 탭·대장간·뽑기 결과가 같은 함수를 쓴다
    /// (index.html invCellHTML/invSorted/fusableKeys 와 같은 구조 — 세 화면이 갈라지지 않게).
    /// </summary>
    public static class GearUi
    {
        /// <summary>
        /// T176 ⓑ — <b>부위 표시 배지</b>의 칸 안 자리(칸 한 변의 %). 인벤 칸 조각(<c>ui.equipCell</c> = 188×188)의 <c>TypeArea</c> 실측을
        /// «지름 48px = 25.5% · 가운데가 왼쪽에서 31.7px(16.9%) · 위에서 32.5px(17.3%)» 로 옮긴 값이다.
        /// 장착 슬롯도 <b>같은 조각(<c>ui.partBadge</c>)·같은 비율</b>로 세워 두 자리의 꼴을 맞춘다
        /// (주인 «장착 슬롯 부분이랑 아래에 있는 장비 모양일 때랑 형식이 똑같지가 않네 통일해 줘»).
        /// </summary>
        public static readonly Layout.R PartBadge = new Layout.R(16.9f - 25.5f / 2f, 17.3f - 25.5f / 2f, 25.5f, 25.5f);
        /// <summary>
        /// 칸·슬롯의 <b>«+N» 강화 표시</b> — 조각(GUI Pro 아이템 프레임)이 이미 갖고 있는 <c>Text_Level</c> 에 쓴다(오른쪽 아래 흰 글자). 0 이면 빈 글자.
        /// <para>
        /// <b>T310(주인 2026-09-09 09:2X «합성 강화 시 오른쪽 아래에 +1 표시가 있는데 장착한 거는 그렇게 안 돼 있더라 · 통일시켜 아래 거랑»)</b> —
        /// 장착 슬롯은 여태 <b>따로 만든 노란 알약 배지</b>(`PlusBadge`)를 아이콘 아래에 달고 있었다. 주인이 «아래 거랑 통일» 이라 했으므로 <b>인벤 꼴이 정본</b>이고,
        /// 그 알약을 지운 자리를 이 함수가 대신한다. <b>두 곳이 같은 함수를 부르는 것이 핵심</b>이다 — 자리·크기·색을 두 곳에 각각 적으면 한쪽만 고쳐지는 날 다시 갈린다(T176 이 부위 배지에서 겪은 그 자리).
        /// </para>
        /// </summary>
        /// <remarks>
        /// <b>T310 회차 2(sess-1027-30514 · 워커 F · 첫 완주 런이 빨갛게 알려 준 자리)</b> — 위 글에 «오른쪽 아래 흰 글자» 라고 적혀 있었는데 <b>한쪽만 참이었다</b>.
        /// 두 조각의 <c>Text_Level</c> 은 <b>가로 정렬이 다르다</b>(프리팹 실측 <c>m_HorizontalAlignment</c>):
        /// 인벤 칸 <c>ListItem_EquipMent</c> = <b>4 = Right</b> · 장착 슬롯 <c>ItemFrame_01</c> = <b>2 = Center</b>.
        /// 곧 슬롯의 «+N» 은 아래 <b>가운데</b>에 떠 있었다 — 주인이 «장착한 거는 그렇게 안 돼 있더라» 라고 한 것이 바로 이것이고,
        /// 알약을 조각 글자로 바꾼 회차 1 만으로는 <b>안 닫혔다</b>. 세로(<c>512 = Middle</c>)는 둘이 같으므로 <b>가로만</b> 못 박는다(조각은 안 고친다).
        /// </remarks>
        /// <summary>
        /// <b>이 장비를 무엇으로 그릴 것인가</b>(T316 3회차) — 이름·«+N»·색을 <see cref="GearTier.Of(GameData, GearItem, string, string)"/> 하나가 정한다.
        /// <para>여태 이름은 <see cref="RarName"/>, 색은 <see cref="Palette.RarName"/> 이 <b>rar 만 보고</b> 정했다 —
        /// 표시 등급(갓·초월·불멸·무한)은 같은 신화 안에서 <c>Plus</c> 로 갈리므로 그 둘로는 넷이 전부 «신화» 로 보인다.</para>
        /// </summary>
        public static GearTier.Shown Tier(GameData D, GearItem g)
            => GearTier.Of(D, g, g != null ? RarName(D, g.Rar) : "", g != null ? Palette.RarName(g.Rar) : Palette.RarColors[0]);

        /// <summary>
        /// 등급색 <b>칸 조각</b>(<c>ui.itemFrame.&lt;색&gt;</c>) 의 색 이름 — <b>조각이 있는 색만</b> 쓰고 없으면 여태 등급색으로 물러선다.
        /// <para>
        /// ⚑ <b>T316 3회차의 실측</b>: 카탈로그의 칸 조각은 <c>gray·blue·green·plum·red·yellow</c> <b>여섯</b>뿐이다.
        /// 주인이 준 표시 등급 색 넷 중 <b>갓(red)만 조각이 있고</b> 초월(<c>pink</c>)·불멸(<c>brown</c>)·무한(<c>redGreen</c> 그라데이션)은 <b>없다</b>.
        /// 새 그림을 지어내는 것은 §1 이 막았으므로, <b>있는 것은 쓰고 없는 것은 신화 그대로</b> 둔다 —
        /// 없는 색을 문자열로 조립해 넘기면 카탈로그에 없는 키가 되어 부팅이 운다(CI #66).
        /// </para>
        /// <para>⛔ <b>주인이 그림을 줘야 열리는 자리</b>: 초월·불멸·무한의 칸 조각. 그때 이 함수는 <b>한 줄도 안 바뀌고</b> 저절로 그 색을 쓴다.</para>
        /// </summary>
        public static string FrameColor(GameData D, GearItem g)
        {
            if (g == null) return Palette.RarColors[0];
            string rar = Palette.RarName(g.Rar), tier = Tier(D, g).Color;
            if (tier == rar) return rar;
            var cat = App.I != null ? App.I.Assets : null;
            return cat != null && cat.Prefab("ui.itemFrame." + tier) != null ? tier : rar;
        }

        /// <summary>이름 옆 «+N» — <b>표시 등급 기준</b>이다(주인 «신화 13강 = 무한 1강»). 0 이면 빈 글자.</summary>
        public static string PlusText(GameData D, GearItem g)
        {
            if (g == null) return "";
            int p = Tier(D, g).Plus;
            return p > 0 ? " +" + p : "";
        }

        public static void SetPlus(Transform frameOrCell, GearItem g) => SetPlus(frameOrCell, null, g);
        /// <summary>표를 아는 꼴 — «+N» 이 <b>표시 등급 기준</b>이 된다(T316 · <paramref name="D"/> 가 null 이면 옛 그대로).</summary>
        public static void SetPlus(Transform frameOrCell, GameData D, GearItem g)
        {
            // T316 3회차 — 적는 수는 «표시 등급 안에서 몇 강인가» 다: 신화 +13 은 «무한 +1»(주인 «신화 13강 = 무한 1강»).
            //   D 가 없으면(옛 서명) 예전처럼 g.Plus 를 그대로 적는다 — 부르는 쪽이 표를 아는 자리부터 바뀐다.
            int plus = D != null ? Tier(D, g).Plus : (g != null ? g.Plus : 0);
            var t = UiKit.SetText(frameOrCell, "Text_Level", g != null && plus > 0 ? "+" + plus : "");
            if (t == null) return;
            t.horizontalAlignment = HorizontalAlignmentOptions.Right;
            // T310 회차 3·4 — 남은 어긋남 하나는 **글자 크기**였다(런 783·795 실측: 인벤 **40** ↔ 슬롯 **32**).
            //   회차 3 은 «두 조각의 자동 크기 문턱이 다르다»(`m_fontSizeMax` 28 ↔ 32)고 보고 **문턱을 같게** 줬는데(min 32 · max 40)
            //   런 795 에서 값이 **한 자도 안 움직였다**(여전히 40 ↔ 32). 곧 갈림은 문턱이 아니라 **자동 크기가 각 칸에 맞춰 스스로 정하는 답**이다 —
            //   두 조각의 글자 띠가 서로 다르고(148.98×46.37 ↔ 160.45×50) 슬롯은 배율(`FitScale` ≈ 0.8)까지 받는다.
            //   ⇒ **맞춤을 끄고 크기를 못 박는다.** «같게 그린다» 를 맞춤 알고리즘에 맡기는 한 두 칸은 계속 각자 답을 낸다.
            //   값은 인벤이 실제로 그리던 크기(`TextSize.Body` 40)다 — 주인이 정본으로 지목한 쪽이고 새 수가 아니다.
            //   넘칠 걱정은 셈으로 닫았다: «+12» 도 40 에서 ≈72px 라 좁은 쪽 띠(148.98)의 절반이다.
            t.enableAutoSizing = false; t.fontSize = TextSize.Body;
            // T460(주인 2026-09-12 «장비에 상단에 장착한 거 보면 +1 표시가 폰트 느낌이 다름 · 두께가 다른 건지 뭔지 · 쨌든 수정») — 크기는 T310 이 맞췄는데 **글꼴이 달랐다**(프리팹 YAML 실측): 슬롯 조각 `ItemFrame_01` 의 `Text_Level` 은
            //   `AfacadFlux-ExtraBold SDF`(외곽선 없음) · 인벤 조각 `ListItem_EquipMent` 의 `Text_Level` 은 `LTAvocado-Bold SDF_OutlineBlack`(검정 외곽선).
            //   두께·외곽선이 달라 «폰트 느낌이 다르다». 인벤 쪽이 주인 정본(T310 «인벤과 같게»)이라 **인벤 조각의 글꼴·재질을 그대로 씌운다** —
            //   새 에셋 0 · 조각 원본 불변 · 인벤 조각에서 한 번 읽어 캐시.
            var pf = PlusFont(); if (pf.font != null) { t.font = pf.font; if (pf.mat != null) t.fontSharedMaterial = pf.mat; }
        }

        static (TMP_FontAsset font, Material mat) _plusFont; static bool _plusFontTried;
        /// <summary>T460 — «+N» 글꼴의 정본 = 인벤 조각(<c>ui.equipCell</c>)의 <c>Text_Level</c> 이 쓰는 글꼴·재질(한 번 읽어 캐시 · 조각이 없으면 (null,null)).</summary>
        public static (TMP_FontAsset font, Material mat) PlusFont()
        {
            if (_plusFontTried) return _plusFont;
            _plusFontTried = true;
            var cat = App.I != null ? App.I.Assets : null;
            var prefab = cat != null ? cat.Prefab("ui.equipCell") : null;
            var lvl = prefab != null ? UiKit.Find(prefab.transform, "Text_Level") : null;
            var tmp = lvl != null ? lvl.GetComponent<TMP_Text>() : null;
            if (tmp != null) _plusFont = (tmp.font, tmp.fontSharedMaterial);
            return _plusFont;
        }
        /// <summary>
        /// 등급 탭(<see cref="Layout.GdBadge"/>)의 <b>세로</b>에만 더하는 여유(px · T214) — 리본 글자가 제목 60 이라 칸이 <see cref="TextSize.BoxHeight"/>(84px) 는 돼야 하는데
        /// 표 높이 2.3%(53.8px)로는 못 담는다(<c>UiKit.RibbonFit</c> 이 공통 팝업 리본에 거는 규칙과 같다 · T75 4항). <b>가로에는 아무것도 안 더한다</b> — 폭은 표 22.0% 그대로다.
        /// </summary>
        public const float BadgeTitlePadPx = 36f;

        /// <summary>장착 슬롯 두 열 — <b>왼쪽 = 무기·목걸이·반지(공격) · 오른쪽 = 투구·갑옷·신발(방어)</b>(T105 · 주인 2026-09-07 지정 · T88 의 역할 묶음과 같다 · 예전 index.html GEAR_COL 은 갑옷↔반지가 반대였다).</summary>
        public static readonly string[] ColLeft = { "weapon", "neck", "glove" }, ColRight = { "helm", "armor", "boot" };

        /// <summary>
        /// 장비 이름 = «<b>세트 별칭</b>의 <b>부위 표시 이름</b>» (T161 · 주인 2026-09-07 «치명 관련 장비는 암살자의 장갑 …» + «반지가 장갑으로 이름 되어 있더라»).
        /// <c>gear.json</c> 의 <c>typeName</c>(«치명 장갑»)은 <b>더 안 쓴다</b> — 그 표가 두 결함의 뿌리였다:
        /// 세트가 «치명» 으로 나오고, T88 의 «장갑 → 반지» 덮어쓰기(<see cref="GearRole.DisplayName"/>)를 안 거쳐 제목만 «장갑» 으로 남아
        /// 같은 팝업 안의 부위 pill(«반지»)과 어긋났다. 이제 둘 다 <see cref="GearRole"/> 한 곳을 지난다.
        /// </summary>
        public static string Name(GameData D, GearItem g) => GearRole.SetDisplayName(D, Set(D, g)) + "의 " + PartName(D, g.Part);
        public static string Set(GameData D, GearItem g) => D.Gear.SetOf(g.Type);
        /// <summary>세트 라벨 — 이름과 같은 별칭을 쓴다(«암살자 세트» · T161 결정: 한 화면에서 «암살자의 반지» 와 «치명 세트» 가 나란히 나오지 않게).</summary>
        public static string SetLabel(GameData D, GearItem g) => GearRole.SetDisplayName(D, Set(D, g)) + " 세트";
        /// <summary>부위 이름 — T88 덮어쓰기(장갑 → «반지» · gear.json 은 aaaw 정본이라 불변)를 거친다.</summary>
        public static string PartName(GameData D, string part) => GearRole.DisplayName(D, part);
        public static string RarName(GameData D, int rar) => rar >= 0 && rar < D.Gear.RarName.Length ? D.Gear.RarName[rar] : rar.ToString();
        /// <summary>장비 아이콘 = <see cref="GearLook"/> 표의 **아이콘 키**(T31 · 투구·무기·갑옷은 CharacterMaker Thumbnail <c>cmi.gear.*</c> — 입는 파츠 <c>cm.gear.*</c> 와 분리 · 목걸이·장갑·신발은 GUI Pro 아이콘(임시)).</summary>
        public static string IconKey(GameData D, GearItem g) => GearLook.IconKey(D, g);
        public static string SetIcon(string set) => set == "crit" ? "pi.critical" : set == "hpsh" ? "pi.heart" : "ui.dodge";
        /// <summary>합성 묶음 키 — 판정은 <see cref="GearSystem.FuseKey"/> 한 곳에 있다(T114 · 전설 미만은 부위·등급만 = 종류 무관). 표(등급 경계)가 필요하므로 데이터를 받는다.</summary>
        public static string Key(GameData D, GearItem g) => GearSystem.FuseKey(D, g);
        /// <summary>데이터를 안 넘기는 호출부(기존 화면·테스트)용 — 지금 돌고 있는 <see cref="App"/> 의 데이터를 쓴다.</summary>
        public static string Key(GearItem g) => GearSystem.FuseKey(App.I != null ? App.I.Data : null, g);

        /// <summary>인벤 정렬 — 장착분 먼저, 그다음 등급·강화 내림차순, 부위 이름.</summary>
        public static List<GearItem> Sorted(SaveData S)
        {
            var list = new List<GearItem>(S.Inv);
            list.Sort((a, b) =>
            {
                int ea = S.IsEquipped(a) ? 1 : 0, eb = S.IsEquipped(b) ? 1 : 0; if (ea != eb) return eb - ea;
                int sa = GearSystem.GearScore(a), sb = GearSystem.GearScore(b); if (sa != sb) return sb - sa;
                return string.CompareOrdinal(a.Part, b.Part);
            });
            return list;
        }
        /// <summary>합성 묶음(<see cref="Key(GameData, GearItem)"/>)이 3개 이상인 키 — T114 로 전설 미만은 «같은 부위·등급» 3개면 종류가 달라도 걸린다.</summary>
        public static HashSet<string> FusableKeys(GameData D, SaveData S)
        {
            var cnt = new Dictionary<string, int>(); foreach (var g in S.Inv) { var k = Key(D, g); cnt[k] = (cnt.TryGetValue(k, out var c) ? c : 0) + 1; }
            var set = new HashSet<string>(); foreach (var kv in cnt) if (kv.Value >= 3) set.Add(kv.Key); return set;
        }
        /// <summary>데이터를 안 넘기는 호출부(장비 화면의 «합성 가능» 빨간 !)용.</summary>
        public static HashSet<string> FusableKeys(SaveData S) => FusableKeys(App.I != null ? App.I.Data : null, S);
        /// <summary>인벤에 더 좋은 게 있다(↑) — 자동 장착은 없고 표시만.</summary>
        public static bool BetterInInv(SaveData S, string part)
        {
            var cur = S.EquippedGear(part); GearItem best = null;
            foreach (var g in S.Inv) { if (g.Part != part || S.IsEquipped(g)) continue; if (best == null || GearSystem.GearScore(g) > GearSystem.GearScore(best)) best = g; }
            return best != null && (cur == null || GearSystem.GearScore(best) > GearSystem.GearScore(cur));
        }

        /// <summary>칸 옵션 — Equipped/Fusable 은 «상태», EquippedMark/FusableDot 은 «표기 켬»(장비 화면은 둘 다 끔 · 대장간(T8)은 켬 — ROUTINE T7.7).</summary>
        public sealed class CellOpts { public bool Equipped, IsNew, Fusable, Selected, Off; public bool EquippedMark, FusableDot; }

        /// <summary>
        /// 장비 칸 하나 = 주인 지정 **ListItem_EquipMent**(카탈로그 <c>ui.equipCell</c> · 188×188) 그대로 — 프리팹 요소를 옮기지 않고 등급색 프레임·아이콘·«+N»·세트 다이아 아이콘만 우리 데이터로 바꾼다.
        /// 크기는 부모가 정한다(격자 188 · 다른 자리는 anchors stretch). 장착중 = 프리팹의 Check(옵션) · 합성 가능 = 오른쪽 위 빨간 점(Alert_Dot_01_Red · 옵션) · NEW = 왼쪽 아래 점.
        /// </summary>
        public static RectTransform Cell(Transform parent, GameData D, GearItem g, CellOpts o, Action onClick)
        {
            o = o ?? new CellOpts();
            var cell = (RectTransform)UiKit.Spawn("ui.equipCell", parent).transform; cell.name = "gear:" + (g != null ? g.Uid.ToString() : "empty");
            var frame = UiKit.Find(cell, "ItemFrame_01");
            if (frame != null)
            {
                var area = UiKit.Find(frame, "NormalArea");
                if (area != null) { UiKit.Clear(area); if (g != null) { var f = UiKit.Spawn("ui.itemFrame." + FrameColor(D, g), area); UiKit.Stretch((RectTransform)f.transform, -1, -1, -1, -1); } }   // 프리팹의 Normal_Plum 자리(+2px) 에 등급색 변형
                var item = UiKit.Find(frame, "Item");
                if (item != null) { item.gameObject.SetActive(g != null); if (g != null) { var im = UiKit.SetSprite(frame, "Item", IconKey(D, g), Palette.White); FitIcon(im, g); } }   // GUI Pro 아이콘은 프리팹 Item 크기 그대로 · 파츠 아이콘은 같은 눈높이로 맞춤(T17)
                UiKit.Show(frame, "Add_1", g == null); UiKit.Show(frame, "Add_2", false); UiKit.Show(frame, "Lock", false); UiKit.Show(frame, "Disable", false);
                UiKit.Show(frame, "Focus", g != null && o.Selected);   // 프리팹의 Focus(테두리 글로우) = 선택
                DarkFrame(frame);   // T69-gear · 7항: 아이템 칸의 테두리 링 = 검은 아웃라인(등급색은 Bg·InnerBorder 가 낸다)
            }
            SetPlus(cell, D, g);
            var type = UiKit.Find(cell, "TypeArea");
            // 다이아 배지 = **부위** 아이콘(T105 · 주인 «무슨 장비 부위인지 알려주는 아이콘» · 세트 아이콘은 세부 팝업 옵션 줄에서만 쓴다)
            if (type != null) { type.gameObject.SetActive(g != null); if (g != null) UiKit.SetSprite(type, "Icon", GearLook.PartIcon(g.Part), Palette.White); }
            // T360 ⓑ(주인 «체크 모양은 Toggle_Check_02_On 으로 통일») — 조각이 달고 온 체크 그림을 공용 ✓(pi.check)으로 갈아 끼운다(새 그림 0).
            UiKit.SetSprite(cell, "Check", "pi.check");
            UiKit.Show(cell, "Check", g != null && o.Equipped && o.EquippedMark);
            if (g != null && o.Fusable && o.FusableDot) UiKit.AlertDot(cell, "FuseDot", new Vector2(1, 1), new Vector2(-14, -14), 47);   // T136
            // T176 ⓐ(주인 2026-09-07 11:0X «왼쪽 하단에 N 표시 … 그거 필요 없음 장비 부분») — **그림만** 없앤다.
            // 세이브 값 <see cref="GearItem.IsNew"/> 는 그대로 둔다: 세부 팝업을 열면 «봤다» 로 끄는 흐름(OpenDetail 첫 줄)과 T167(장비 탭 빨간 점)이 그 값을 쓴다.
            if (o.Off) { var cg = UiKit.Ensure<CanvasGroup>(cell.gameObject); cg.alpha = 0.4f; }
            if (onClick != null) UiKit.Clickable(cell, onClick);
            return cell;
        }

        /// <summary>아이템 프레임 조각(ItemFrame_01 · 등급 변형 ItemFrame_01_Normal_*)의 테두리 링 스프라이트 이름 앞머리 — <c>ItemFrame_01_White_Border</c>(79×79 · 9-slice 39/40 · 선 5px 실측). FocusBorder 는 다른 이름이라 안 걸린다.</summary>
        public const string ItemBorderSprite = "ItemFrame_01_White_Border";
        /// <summary>
        /// 아이템 칸 조각(<c>ItemFrame_01_Normal_*</c>)을 «보이게만» 손질한다 — <b>색·굵기는 조각 그대로 둔다</b>.
        /// <para>
        /// ⚑ T103(주인 2026-09-07 06:4X «<c>Character_Hero_Item_Detail_03</c> 의 <c>ItemFrame_01_Normal_Red</c> 기준으로 · <b>여기서 색깔만 바뀌는 식이면 딱 맞는 거임</b>»):
        /// 전에는 이 함수가 조각의 «Border» 링을 <see cref="UiKit.BorderInk"/> 로 덮어칠하고 선을 8px 로 굵혔다(T69 7항 · 결정 163).
        /// 주인이 «조각 그대로 · 색 변형만» 을 정본으로 못 박았으므로 <b>덧칠과 굵히기를 뺐다</b> — 등급은 <c>ui.itemFrame.&lt;색&gt;</c> 조각을 갈아 끼우는 것으로만 나타난다.
        /// </para>
        /// <para>
        /// 남긴 것은 결정 184(«단언은 통과하는데 눈에는 없다» 를 막는 세 가지)뿐이라 조각 그림을 바꾸지 않는다:
        /// 링을 자기 부모의 <b>맨 뒤(맨 위)</b>로 올려 형제(<c>InnerBorder3</c>·<c>Glow</c>)에 가려지지 않게 하고, 9-slice 가운데를 비워 아이콘·«+» 를 덮지 않게 하고, raycast 를 꺼 칸 클릭을 막지 않는다.
        /// </para>
        /// 장착 슬롯(GearScreen) · 인벤/대장간/뽑기 결과/세부 팝업 칸(<see cref="Cell"/>) · 빈 슬롯 팝업(<see cref="OpenSlot"/>) · 펫·상점·던전·아레나·로비 팝업의 물건 칸이 전부 이 함수를 거친다.
        /// <paramref name="scale"/> 는 굵히기를 하던 시절의 인자다 — 호출부 열세 곳을 건드리지 않으려고 자리만 남겼다(T103 · 워커 결정 기록).
        /// </summary>
        /// <summary>
        /// 아이템 칸 조각이 달고 오는 «튀는» 하이라이트 자식의 이름 앞머리(T164 · 우리 코드가 쓰는 자리는 없다 · grep 0건).
        /// 이름을 <b>둘로 나열하지 않고 앞머리로</b> 잡는다 — 게이트(<c>EventsScreenTests.AssertNoHighlights</c>)가 «HighLight 로 시작하는 것» 을 세므로
        /// 끄는 쪽과 재는 쪽의 잣대가 같아야 «조각에 HighLight3 이 생기면 게이트만 빨개지는» 어긋남이 안 난다.
        /// </summary>
        public const string HighlightPrefix = "HighLight";
        public static void DarkFrame(Transform frame, float scale = 1f)
        {
            if (frame == null) return;
            // T164(주인 09:0X «원정 부분에 뜬금없게 HighLight1,2 있는데 튀기만 하고 이상함») — 조각
            // `ItemFrame_01_Normal_BasePrefab` 이 달고 오는 하이라이트 둘은 칸마다 알파·스케일이 제각각이라 번쩍인다.
            // 우리 코드가 그것을 쓰는 자리는 **한 곳도 없다**(grep 0건) → 조각을 세우는 공용 자리인 여기서 끈다
            // (조각 원본은 안 고친다 · §1 «프리팹은 부품 · 원본 불변»). 장비·대장간·던전·아레나·뽑기 결과가 같이 조용해진다.
            // ⚠ 회차 1(`16def030`)은 `UiKit.Find` 로 껐는데 그것은 **이름마다 «첫 하나»** 만 돌려준다.
            // 우리 칸은 조각을 **겹쳐** 세우는 자리가 많다 — 바깥 `ui.itemFrame.empty` 안 `NormalArea` 에
            // 등급색 `ui.itemFrame.<색>` 을 하나 더 넣는다(GearScreen·LobbyPopups·Overlay·PetScreen).
            // 두 조각이 각각 HighLight1·2 를 달고 오므로 한 칸에 넷이 있고, 첫 하나씩만 꺼져 **둘이 남아 켜져 있었다**
            // (CI #311·#315 «켜진 하이라이트 3개»). 그래서 이름이 «HighLight» 로 시작하는 **자손 전부**를 끈다(결정 433).
            foreach (var t in frame.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name.StartsWith(HighlightPrefix, StringComparison.Ordinal)) t.gameObject.SetActive(false);
            // T342(주인 2026-09-10 «모든 장비 부분 프레임 들에 있는 글로우 부분 완전 흰색에 완전 불투명으로 해줘야함») —
            // 조각 `ItemFrame_01_Normal_BasePrefab` 이 달고 오는 «Glow» 는 **원본은 흰색 α1** 인데
            // 등급 변형(`ItemFrame_01_Normal_<색>`)이 그 색을 제 등급색으로 덮어 온다(그래서 칸마다 다른 색·다른 짙기로 보였다).
            // 여기서 **다시 흰색 α1** 로 되돌린다 — 등급은 테두리(`Border`)와 조각 교체로만 나타내고, 글로우는 어느 등급에서나 같다.
            // ⚠ 조각 원본은 안 고친다(§1 «프리팹은 부품 · 원본 불변») — 세우는 공용 자리인 여기서만 칠한다.
            // ⚠ 우리 빛 담개(`LightMask`) 안의 글로우 서클은 **이름만 같은 다른 물건**이라 건너뛴다(T155 ⓓ · 짙기 UiKit.GlowAlpha).
            foreach (var im in frame.GetComponentsInChildren<Image>(true))
            {
                if (im == null) continue;
                if (im.name == UiKit.GlowName)
                {
                    var par = im.transform.parent;
                    if (par != null && par.name == UiKit.LightMaskName) continue;
                    // 완전 흰색 · 완전 불투명(Palette.White = Color.white · α1)
                    im.color = Palette.White;
                    continue;
                }
                if (im.name != UiKit.BorderName || im.sprite == null || !im.sprite.name.StartsWith(ItemBorderSprite, StringComparison.Ordinal)) continue;
                im.type = Image.Type.Sliced;
                im.fillCenter = false; im.raycastTarget = false; im.transform.SetAsLastSibling();
            }
        }

        // ─────────────────────────── 주인 «칸 문법»(T443) ───────────────────────────
        /// <summary>
        /// <b>칸 안 아이콘이 차지하는 비율</b>(칸 %) — 주인 2026-09-11 «프레임 내부 가운데에 <b>적당히 크게</b> 재화 아이콘».
        /// <para>
        /// ⚠ 이 수는 «보기 좋은 값» 이 아니라 <b>주인이 꼴을 말한 자리</b>다. 종전 칸들은 56~68% 였고, 수량을 아래에 <b>떼어 놓느라</b>
        /// 그림이 칸의 절반으로 눌려 있었다 — 주인의 말이 바로 그것이다(«안 겹치게 떨어뜨려 놓으니까 너무 안 보임 작아 보임»).
        /// </para>
        /// </summary>
        public const float CellIconPct = 80f;
        /// <summary>
        /// 수량 글자 칸(칸 %) — <b>가운데 아래</b>(T459 · 주인이 «오른쪽 아래» 를 취소했다)에서 <see cref="CellQtyOver"/> 만큼 밖으로 걸친다(아래로 · 좌우 대칭).
        /// <para>
        /// ⛑ <b>폭이 왜 100% 를 넘는가</b>(T443 1회차가 런 1070 에서 값을 치른 자리) — 글자는 <b>오른쪽 정렬</b>이라 rect 가 넓어도 자리를 안 먹고 <b>왼쪽으로 자랄 여지</b>만 준다.
        /// 82% 로 뒀더니 던전 세부 칸(119px)에서 rect 가 98px 뿐이라 «1,000» 이 <c>bestFit</c> 으로 <b>35 까지 눌렸고</b>, 그러면 T63 가독성 게이트(보조 36)가 운다 —
        /// 즉 «수량을 작은 칸에 가둔다» 는 주인이 말한 병(«너무 안 보임 작아 보임»)을 <b>다른 꼴로 되풀이하는 것</b>이다.
        /// ⛑⛑ <b>그런데 2회차의 132% 는 틀렸다</b>(런 1072 그림 실측 · 3회차가 되돌린다) — 넓힌 rect 가 <b>이웃 칸을 덮었다</b>:
        /// 던전 세부(21)는 «11 1,000 … 5 1,000» 이 한 줄로 붙어 읽혔고 출석(16) 7일차는 «10,0001,000» 이 됐다.
        /// «작아서 안 읽힘» 을 «붙어서 안 읽힘» 으로 바꾼 것이라 <b>주인이 말한 병의 세 번째 얼굴</b>이다.
        /// ⇒ 폭은 <b>104%</b>(칸 + 걸침 4%)로 되돌리고, 다섯 자가 안 들어가는 문제는 <b>글자를 짧게</b> 써서 푼다(<see cref="CellQtyText"/>) —
        /// 120px 칸에 다섯 자를 하한 크기로 넣는 길은 원리적으로 없다(36 × 3.77px/pt = 136px > 칸).
        /// </para>
        /// <para>⚠ <b>한계</b>: 더 긴 수(여섯 자 이상)는 이 폭으로도 눌린다. 표가 그런 수를 내는 날에는 폭이 아니라 <b>짧게 쓰는 꼴</b>(<c>UiKit.Fmt</c> 의 «10K»)이 답이다 — 레퍼런스 16 도 그 꼴이다.</para>
        /// <para>높이 50% 는 T133 ⓙ 가 레퍼런스 16 에서 잡은 값 그대로다 — 40 × 1.4 = 56px 이 들어가야 «잘림» 판정을 안 받는다.</para>
        /// </summary>
        public const float CellQtyW = 104f, CellQtyH = 50f, CellQtyOver = 4f;
        /// <summary>
        /// 칸 가운데에 아이콘 하나(<see cref="CellIconPct"/> 정사각 · <c>preserveAspect</c>). 이름은 <c>Icon</c>.
        /// <para>⚠ 수량을 피해 위로 올리지 <b>않는다</b> — 주인 지시가 «겹치는 식» 이다. 읽히게 하는 것은 자리가 아니라 <see cref="CellQty"/> 의 검은 외곽선이다.</para>
        /// </summary>
        public static Image CellIcon(Transform cell, string iconKey, string name = "Icon", Color? tint = null)
        {
            var ic = UiKit.Icon(cell, name, iconKey, tint);
            float m = (100f - CellIconPct) * 0.5f;
            UiKit.Pct(ic.rectTransform, m, m, CellIconPct, CellIconPct);
            return ic;
        }
        /// <summary>
        /// <b>칸에 적는 수 — 짧게 쓴다</b>(T443 3회차). 천 이상은 «1K»·«12.5K»·«3M» 꼴.
        /// <para>
        /// ⚠ <c>UiKit.Fmt</c> 를 그냥 못 쓴다 — 그쪽 문턱은 <b>1e4</b> 라 «1,000» 이 다섯 자 그대로 남고, 그 다섯 자가 칸에 안 들어가는 것이 이 절의 문제다.
        /// 칸은 120px 안팎이고 하한 36 에서 한 글자가 ≈0.75px/pt 를 먹으니 <b>네 자가 한계</b>다(런 1070·1072 실측).
        /// </para>
        /// <para>⚑ 주인 레퍼런스 <c>docs/ref/16_attendance.jpg</c> 도 그 자리를 «5000»·<b>«10K»</b> 로 적는다 — 짧게 쓰는 것은 이 게임의 본래 꼴이지 내가 정한 편법이 아니다.</para>
        /// <para>
        /// ⚑⚑ <b>셈과 낱말표는 <see cref="KkomaKnight.Core.ShortNum.Cell"/> 한 자리다</b>(T454) — 여기 사다리를 따로 적으면 <b>읽는 쪽과 어긋난다.</b>
        /// 이 글자는 <c>LobbyPopups:1272</c>(출석 · <c>amount:</c> 없음)를 타고 <c>RewardPopup.QtyOf</c> 로 가 <b>다시 숫자로 읽히는데</b>,
        /// 그쪽은 <c>ShortNum</c> 의 표를 본다. 둘이 다른 표를 보면 «출석 다이아 1000 → 구슬 1개»(결정 1259 · <b>배포까지 갔던 것</b>)가 되살아난다.
        /// ⚠ <c>Game</c> 은 `dotnet` 하니스가 컴파일조차 안 하므로(결정 143) <b>여기 사다리를 되살려도 로컬 자는 전부 초록이다</b> — 그것을 막는 것은 <c>ShortNumCellTests</c> 의 소스 글자 자 하나뿐이다.
        /// </para>
        /// </summary>
        public static string CellQtyText(double n) => KkomaKnight.Core.ShortNum.Cell(n);
        /// <summary>
        /// <b>이미 글자로 된 수</b>를 칸 꼴로 다시 쓴다 — «1,000» → «1K». 숫자와 콤마뿐일 때만 손대고, «×3»·«10%» 처럼 다른 글자가 섞이면 <b>그대로 둔다</b>.
        /// <para>
        /// ⚠ <b>원본 글자를 바꾸지 않으려고</b> 이 길을 둔다 — <c>RewardPopup.QtyOf</c> 는 <c>Amount</c> 가 없을 때 <b>그 글자에서 수를 되읽어</b> 구슬 개수를 정한다.
        /// 부르는 쪽의 문자열을 «1K» 로 갈아 버리면 구슬이 1,000개 대신 <b>1개</b> 난다(<c>Item.Of</c> 호출부 일곱이 <c>amount:</c> 를 안 준다 · 실측).
        /// ⇒ <b>보여 주는 자리에서만</b> 다시 쓴다.
        /// </para>
        /// </summary>
        public static string CellQtyShorten(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            double n = 0; bool any = false;
            foreach (var ch in s)
            {
                if (ch >= '0' && ch <= '9') { any = true; n = n * 10 + (ch - '0'); continue; }
                if (ch == ',' || ch == ' ') continue;
                return s;   // 숫자·콤마 말고 다른 것이 섞였다 — 부르는 쪽의 뜻이 있는 글자다
            }
            return any ? CellQtyText(n) : s;
        }
        /// <summary>
        /// 칸 <b>가운데 아래</b> 수량 글자(T459 — 주인 2026-09-12 «중앙 아래가 나은 듯» · 종전은 오른쪽 아래였다) — 아이콘 위로 겹쳐 얹고(그래서 <see cref="CellIcon"/> 뒤에 부른다) 굵게 + 검은 외곽선. 이름은 <c>Qty</c>.
        /// <para>
        /// <paramref name="cellHPct"/>(칸 높이 = <b>프레임 높이의 %</b>)를 주면 그 높이에서 글자 크기를 뽑고(칸이 클수록 숫자도 큰다), 0 이면 보조 크기 그대로.
        /// ⚠ <c>UiKit.FontForHeight</c> 는 <b>px 가 아니라 프레임 %</b> 를 받는다 — px 를 넘기면 글자가 화면만 한 크기로 뽑힌다(T133 ⓙ 가 값을 치른 자리).
        /// 빈 글자면 아무것도 안 세운다 — «0» 과 «없음» 을 가르는 것은 부르는 쪽 몫이다.
        /// </para>
        /// </summary>
        public static TMP_Text CellQty(Transform cell, string qty, float cellHPct = 0f, Color? color = null)
        {
            if (cell == null || string.IsNullOrEmpty(qty)) return null;
            int size = cellHPct > 0f ? UiKit.FontForHeight(cellHPct * CellQtyH / 100f) : TextSize.Aux;
            // ⛳ T459(주인 2026-09-12 «오른쪽 아래에 텍스트 있으라 했는데 **중앙 아래**가 나은 듯») —
            //   자리만 바뀐다: 폭(104%)·높이(50%)·걸침(4%)·겹침·외곽선은 T443 이 값을 치르고 정한 그대로다.
            //   ⚠ 왼쪽 시작점을 «(100 − 폭)/2» 로 두어 **걸침이 좌우 대칭**이 된다(종전은 오른쪽으로만 4% 걸쳤다).
            //     그래야 가운데 정렬 글자가 칸 한가운데에 서고, 긴 수는 양쪽으로 고르게 자란다.
            var q = UiKit.Label(cell, (100f - CellQtyW) * 0.5f, 100f - CellQtyH + CellQtyOver, CellQtyW, CellQtyH,
                                qty, size, color ?? Palette.White, TextAnchor.LowerCenter, kind: TextKind.Body);
            q.name = "Qty"; q.fontStyle = FontStyles.Bold;
            return q;
        }
        /// <summary>
        /// 재화·아이템 칸 한 장 = 틀(<paramref name="frameKey"/> + <see cref="DarkFrame"/>) + 가운데 아이콘 + <b>가운데 아래</b> 수량(T459).
        /// <b>모든 UI 의 재화 칸이 이 문법 하나를 쓴다</b>(주인 2026-09-11 «모든 UI 부분 재화 부분 다 바꾸셈»).
        /// </summary>
        public static void CellArt(Transform cell, string frameKey, string iconKey, string qty = null, float cellHPct = 0f)
        {
            if (cell == null) return;
            if (!string.IsNullOrEmpty(frameKey))
            {
                var f = UiKit.Spawn(frameKey, cell); UiKit.Stretch((RectTransform)f.transform);
                DarkFrame(f.transform);   // T115 · T69 7항 — 조각 제 Border 링을 Ink 로(결정 184 계약)
            }
            CellIcon(cell, iconKey);
            CellQty(cell, qty, cellHPct);
        }

        /// <summary>
        /// 이 칸이 «아이템 칸»(<c>ItemFrame_01</c> 조각을 쓰는 물건 칸)인가 — 켜진 «Border» 링의 스프라이트가 조각의 것(<see cref="ItemBorderSprite"/>)이면 그렇다.
        /// T69 «검은 아웃라인» 감사는 이 칸을 <b>면제</b>한다(T103 3항 ⓐ · 주인 최신 지시가 T69 보다 뒤다) — 조각 제 링이 등급색·밝기 그대로여야 «색깔만 바뀌는 식» 이 성립하기 때문이다.
        /// </summary>
        public static bool HasItemFrame(Transform cell)
        {
            if (cell == null) return false;
            foreach (var im in cell.GetComponentsInChildren<Image>(false))
                if (im != null && im.enabled && im.name == UiKit.BorderName && im.sprite != null && im.sprite.name.StartsWith(ItemBorderSprite, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// 장비 아이콘 맞춤(T17 · 주인 «투구·갑옷·무기 아이콘만 작다»). 프리팹 Item(256×256 · 스케일 0.6149)은 GUI Pro 128px 아이콘용이라 그림이 rect 의 ~85% 를 채우지만,
        /// CharacterMaker 그림은 그림이 캔버스를 채우는 비율이 제각각(입는 파츠 33~70% · T31 의 Thumbnail 128×128 도 여백이 있다) → 같은 rect 에서 작게 보였다.
        /// 파츠 아이콘(T31 부터 Thumbnail)은 스프라이트의 **불투명 bbox**(Tight 메시 정점 · <c>Sprite.vertices</c>)가 칸의 <see cref="GearLook.PartIconFill"/>(72%) 를 채우도록 Item 의 sizeDelta·pivot 을 계산(<see cref="GearLook.FitPartIcon"/>)하고,
        /// 회전은 하지 않는다(주인 2026-09-06 «45° 취소» · 프리팹 회전 그대로). GUI Pro 아이콘(목걸이·장갑·신발)은 프리팹 값으로 되돌린다.
        /// 장착 슬롯·인벤 칸·세부 팝업·대장간·뽑기 결과가 전부 이 함수를 거친다(Cell 과 GearScreen 슬롯). 프리팹 값은 <see cref="PartIconFit"/> 이 처음 한 번 기억해 두고 복원한다(슬롯 Item 재사용).
        /// </summary>
        public static void FitIcon(Image im, GearItem g) => FitIcon(im, g != null && GearLook.HasLook(g.Part));
        public static void FitIcon(Image im, bool isPart)
        {
            if (im == null) return;
            var rt = im.rectTransform; var st = UiKit.Ensure<PartIconFit>(im.gameObject); st.Capture(rt);
            var sp = im.sprite;
            if (!isPart || sp == null) { st.Restore(rt); return; }
            var rect = sp.rect; float ppu = sp.pixelsPerUnit > 0 ? sp.pixelsPerUnit : 100f; var piv = sp.pivot;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            var verts = sp.vertices;   // Tight 메시(파츠 .meta spriteMeshType 1) → 정점의 min/max = 불투명 bbox (rect 왼쪽아래 원점 픽셀)
            if (verts != null && verts.Length >= 3)
                foreach (var v in verts) { float px = v.x * ppu + piv.x, py = v.y * ppu + piv.y; if (px < x0) x0 = px; if (py < y0) y0 = py; if (px > x1) x1 = px; if (py > y1) y1 = py; }
            else { x0 = 0; y0 = 0; x1 = rect.width; y1 = rect.height; }
            var frame = rt.parent as RectTransform;
            float fw = frame != null ? Mathf.Min(frame.rect.width, frame.rect.height) : 0f;
            if (fw <= 0f) fw = CellSize(App.I != null ? App.I.Assets : null);   // 레이아웃 전이면 ListItem_EquipMent 본래 한 변(188)
            var fit = GearLook.FitPartIcon(rect.width, rect.height, x0, y0, x1, y1, fw, GearLook.PartIconFill, rt.localScale.x);
            im.preserveAspect = true;
            rt.pivot = new Vector2((float)fit.PivotX, (float)fit.PivotY);
            rt.sizeDelta = new Vector2((float)fit.W, (float)fit.H);
            rt.anchoredPosition = st.Pos;   // 프리팹 자리(가운데) — pivot 이 그림 가운데라 그림이 칸 가운데에 온다
            rt.localRotation = st.Rot;   // 프리팹 회전 그대로(무기 45° 는 주인이 취소)
        }

        /// <summary>
        /// 인벤 격자 — ScrollRect + GridLayout. 돌려주는 Content 에 Cell 을 채운다.
        /// 격자 값(칸 188×188 · 5열 · 세로 간격 · 위아래 패딩)은 **장비 화면 프리팹(Character_Hero_Equipment) 의 Content GridLayoutGroup 에서 그대로 복사**한다 —
        /// 대장간 칸이 장비 화면 칸(ListItem_EquipMent)과 같은 크기·비례가 되게(T8 · 찌그러짐 0). 가로 간격만 view 폭에 맞춰 5열이 딱 들어가게 다시 계산한다.
        /// (예전엔 ref-layout 표 % 로 18.4×7.2 = 199×168 칸을 만들어 정사각 프리팹이 찌그러졌다.)
        /// </summary>
        public static RectTransform Grid(Transform parent, Layout.R rect, out ScrollRect scroll)
        {
            var view = UiKit.Rect(parent, "InvScroll"); UiKit.Pct(view, rect);
            view.gameObject.AddComponent<RectMask2D>();
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            var content = UiKit.Rect(view, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1); content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            CopyEquipmentGrid(grid, UiKit.FrameW * rect.W / 100f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.childAlignment = TextAnchor.UpperLeft; grid.startCorner = GridLayoutGroup.Corner.UpperLeft; grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content; scroll.viewport = view;
            return content;
        }
        /// <summary>
        /// 인벤 격자 위쪽 여백(프레임 px) — 주인 2026-09-07 «아래에 장비 아이템들이 Band 에 너무 딱 붙어 있음 · Content 에 탑에 패딩 20 정도»(T112 ⓑ).
        /// 장비 화면(06)과 대장간(08)이 같은 <see cref="Grid"/> 를 쓰므로 한 곳에서 둘 다 받는다.
        /// </summary>
        public const int InvTopPadPx = 20;

        /// <summary>장비 화면 프리팹 Content 의 격자 값을 복사한다(칸·세로 간격·패딩·열 수). 프리팹이 없으면 ListItem_EquipMent 의 본래 크기(정사각)로 · 그것도 없으면 표 % 폭의 정사각. 가로 간격 = (폭 − 열×칸) / (열 − 1).</summary>
        static void CopyEquipmentGrid(GridLayoutGroup grid, float viewW)
        {
            var cat = App.I != null ? App.I.Assets : null;
            GridLayoutGroup src = null;
            var eq = cat != null ? cat.Prefab("ui.equipment") : null;
            if (eq != null) { var c = UiKit.Find(eq.transform, "Content"); if (c != null) src = c.GetComponent<GridLayoutGroup>(); }
            if (src != null)
            {
                grid.cellSize = src.cellSize; grid.spacing = src.spacing; grid.constraintCount = src.constraintCount;
                grid.padding = new RectOffset(src.padding.left, src.padding.right, src.padding.top + InvTopPadPx, src.padding.bottom);
            }
            else
            {
                float s = CellSize(cat);
                grid.cellSize = new Vector2(s, s); grid.spacing = new Vector2(0, UiKit.FrameH * (Layout.GearInvRowPitch - Layout.GearInvCellH) / 100f); grid.constraintCount = Layout.GearInvCols;
                grid.padding = new RectOffset(0, 0, (int)(UiKit.FrameH * (Layout.GearInvCell.Y - Layout.GearInv.Y) / 100f) + InvTopPadPx, 0);
            }
            int cols = Mathf.Max(1, grid.constraintCount);
            float gapX = cols > 1 ? (viewW - cols * grid.cellSize.x) / (cols - 1) : 0f;
            grid.spacing = new Vector2(Mathf.Max(0f, gapX), grid.spacing.y);   // 5열이 폭에 딱 — 칸 크기는 절대 줄이지 않는다(비례 고정)
        }
        /// <summary>ListItem_EquipMent 프리팹의 본래 한 변(188). 프리팹이 없으면 ref-layout 표의 칸 폭.</summary>
        public static float CellSize(AssetCatalog cat)
        {
            var cell = cat != null ? cat.Prefab("ui.equipCell") : null; var rt = cell != null ? cell.transform as RectTransform : null;
            return rt != null && rt.sizeDelta.x > 0 ? rt.sizeDelta.x : UiKit.FrameW * Layout.GearInvCellW / 100f;
        }
        public static void Empty(Transform content, string msg)
        {
            var t = UiKit.Text(content, msg, TextSize.Body, Palette.InkLight, TextAnchor.MiddleCenter);
            var le = t.gameObject.AddComponent<LayoutElement>(); le.ignoreLayout = true;
            t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1); t.rectTransform.pivot = new Vector2(0.5f, 1); t.rectTransform.anchoredPosition = new Vector2(0, -20); t.rectTransform.sizeDelta = new Vector2(0, 160);
        }

        // ───────────────────────── 장비 세부 팝업 — 레퍼런스 docs/ref/07_gear_detail.jpg 구도(표 ④ · 공통 팝업 문법 UiKit.Popup · T38 · T27 «Character_Hero_Item_Detail_01 그대로» 폐기) ─────────────────────────
        /// <summary>
        /// 박스 윗변 등급 탭(Title_01 명판) 조각 — <b>등급과 무관하게 갈색 하나</b>.
        /// <para>
        /// <b>T324(주인 2026-09-09 12:3X «일반 부분의 <c>ui.titleBrown</c> 디자인이 맘에 드는데 그 디자인대로 나머지 등급들도 그거로 써 줘. 걍»)</b> —
        /// 전에는 <c>BadgeKey(colorName)</c> 이 등급색으로 조각을 갈아 끼웠다:
        /// <c>blue → ui.title.sky · yellow → ui.title.yellow · plum → ui.title.plum · red → ui.title.red · green → ui.title.green · 그 밖(gray) → ui.titleBrown</c>.
        /// 주인이 고른 것은 그 <b>갈색 조각의 디자인</b> 하나이므로 갈래를 지웠다 — <b>되돌리려면 그 여섯 줄을 되살리면 된다</b>
        /// (카탈로그의 <c>ui.title.*</c> 변형은 다른 화면이 쓰므로 그대로 있다 · 문자열 조립은 여전히 금지 · CI #66).
        /// </para>
        /// <para>
        /// ⚠ <b>등급이 안 보이게 된 것이 아니다</b> — 배지 «글자»(«희귀»·«전설»…) · 이름 색(<c>nameColor</c>) · 팝업 상자 색(<c>ui.popup.&lt;색&gt;</c>)은 <b>한 줄도 안 바뀐다</b>.
        /// 주인이 바꾸라 한 것은 «제목 조각» 하나이고, 그 셋이 남아 있어서 이 화면은 여전히 등급을 말한다. <b>T316</b>(갓·초월·불멸·무한)의 제목 조각도 이 규칙을 그대로 받는다.
        /// </para>
        /// </summary>
        public const string TitleBadge = "ui.titleBrown";
        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
        /// <summary>어두운 pill(fr.r12 · 잉크색) + 글자 — 메타줄 «슬롯 Lv. N/최대»·«부위», 스탯 박스, 옵션 줄, 비용 줄이 같은 조각을 쓴다. 전부 «검은 아웃라인»(T69-gear · 레퍼런스 07 의 pill·스탯 상자·옵션 줄·비용 줄은 모두 검은 외곽선) — <see cref="UiKit.Bordered"/> 를 먼저 덧대고 글자·아이콘은 그 뒤에 얹혀 테두리 위에 온다.</summary>
        /// <summary>
        /// 어두운 pill 위 글자색 — 등급색을 흰 쪽으로 올린다(T84 · 주인 상시 지시 «밝은 글자색 + 검은 아웃라인»).
        /// 일반 등급 회색(#A39B9D)이 잉크 pill 위에서 안 읽히던 것이 이유다(screens run 148 의 07 눈 확인).
        /// </summary>
        static Color OnDarkPill(Color c) => Color.Lerp(c, Palette.White, 0.35f);
        /// <summary>
        /// 팝업 상자 위 이름줄 글자색(T130) — 상자가 <b>어두운 회색</b>이 되면서(<see cref="Palette.PopupBox"/>) «밝은 바탕이니 글자를 누른다» 는 전제가 사라졌다.
        /// <b>레퍼런스 07 의 아이템 이름(«Shadow Treads»)은 등급색이 아니라 흰색**이고, 등급은 위 리본(«EPIC»)과 아이콘 테두리가 말한다 — 그래서 흰색을 쓴다
        /// (T63 0항 «밝은 글자 + 검은 아웃라인» 과도 같은 방향 · 종전 <c>OnCream</c> 은 어두운 상자 위에서 등급색을 더 눌러 안 읽혔다).
        /// 인자는 안 쓰지만 «이 자리 색은 등급에서 온다» 는 호출부 뜻을 남겨 둔다.
        /// </summary>
        static Color OnPopupBox(Color rarity) => Palette.White;
        /// <summary>
        /// 팝업 «안» 의 띠 하나(스탯 상자 · 옵션 줄 · 비용 줄 · 메타 pill) — <paramref name="alpha"/> 는 «얼마나 도드라지나» 다(0.6 옅음 ~ 0.85 진함).
        /// <para>
        /// T130 회차 2 — 종전에는 <b>잉크(#341B19)를 알파로</b> 얹었다. 크림 상자 위에서는 그 갈색이 안 보였지만
        /// 상자가 어두워지자(<see cref="Palette.PopupBox"/>) <b>띠가 갈색기를 띠어</b> 레퍼런스의 중성 회색과 어긋났다(`screens` run 257 실측).
        /// 그래서 알파 합성 대신 <b>상자색 → 레퍼런스 줄색(<see cref="Palette.PopupRow"/> · 07 실측 #201E1F) 사이를 <paramref name="alpha"/> 로 보간</b>한다 —
        /// 세기의 뜻(0.6~0.85 의 위계)은 그대로고, 결과는 <b>불투명</b>이라 상자 색이 또 바뀌어도 안 흔들린다(결정 344 와 같은 갈래: 비치는 것을 계열색으로 바꾼다).
        /// </para>
        /// </summary>
        static RectTransform Pill(RectTransform parent, string name, Layout.R r, float alpha = 0.85f)
        {
            var bg = Color.Lerp(Palette.PopupBox, Palette.PopupRow, Mathf.Clamp01(alpha));
            var p = UiKit.Panel(parent, name, "fr.r12", bg); UiKit.Pct(p.rectTransform, r); UiKit.Bordered(p.rectTransform); return p.rectTransform;
        }
        /// <summary>
        /// 표 ④ 의 공통 뼈대: 어둠 + 패널(GdBox) + 박스 윗변 <b>등급 탭</b>(GdBadge · 등급색 명판) → 왼쪽 <b>아이콘 칸</b>(GdIcon · 장비 칸 Cell «+N» 포함 · 빈 슬롯은 빈 프레임) · 오른쪽 <b>이름 굵게</b>(GdName) + <b>pill 2</b>(GdMeta · «슬롯 Lv. N/최대» · «부위») → «탭하여 닫기»(배경 탭 = 닫기 · 닫기 X 없음).
        /// 돌려주는 box 안에 스탯 박스(GdStats) · 옵션 줄(GdOpts) · 비용 줄(GdCost) · 버튼 2(GdBtnL/R) 를 Pct 로 놓는다(<see cref="OpenDetail"/> · <see cref="OpenSlot"/>).
        /// </summary>
        static RectTransform DetailFrame(App app, string badge, GearItem g, string name, Color nameColor, string pill1, string pill2, Layout.R? boxOverride = null, Action onTapClose = null)
        {
            // boxOverride — 아래 두 버튼이 없는 «보기 전용» 모드(T267 4항 · 주인 «아래 두 버튼만 없애고 레이아웃 좀만 조절하면 똑같음»)에서
            //   상자를 짧게 자른다. 안쪽 자리는 전부 «화면 %» 를 `.Within(B)` 로 상자 안 %로 다시 그리는 꼴이라,
            //   **x·w 를 그대로 두고 h 만 줄이면 잘린 선 위의 요소는 화면에서 한 픽셀도 안 움직인다** — 단 그것은
            //   `.Within` 에 **실제로 그려질 상자** 를 넘겼을 때만이다(T267 6단계에서 이 조건이 깨져 있던 것을 실측으로 잡았다 · 결정 809).
            var ov = app.Overlay; var B = boxOverride ?? Layout.GdBox;
            // T324 — 등급색 갈래는 없다(위 주석에 옛 표와 되돌리는 법이 있다).
            //   그래서 이 함수가 받던 `colorName` 도 같이 뺐다 — 안 쓰는 인자는 «이 값이 상자 꼴을 정한다» 는 거짓말을 남긴다.
            //   등급은 부르는 쪽이 넘기는 `badge`(«희귀»·«전설» 글자)와 `nameColor`(이름 색)가 그대로 말한다.
            string bk = TitleBadge;
            var box = ov.OpenBox("ui.popup", bk, badge, B, onTapClose ?? (Action)(() => ov.Close()));
            // 등급 탭 = 표 ④ 배지 크기. **가로는 표 그대로**(T214 · 예전에는 +70px 를 더해 폭이 22.0 → 28.5%(+6.5%p)로 벌어져 §5 에서 0점이었다 · 결정 96 이 «22×2.3» 이라고 적어 둔 자리다).
            // **세로만 +36px 를 남긴다** — 리본 글자는 제목 60 이고 그 칸은 84px 이 필요한데(<see cref="TextSize.BoxHeight"/> · T75 4항 · <c>UiKit.RibbonFit</c> 이 공통 팝업에 거는 것과 같은 규칙)
            // 표 h 2.3%(53.8px)로는 못 담는다. 세로 차 +1.5%p·자리 −1.7%p 는 §5 판정 ±3%p 안이라 이 한 줄로 행이 0 → 1 점이 된다.
            var rib = UiKit.Find(box, bk); if (rib != null) { var rr = (RectTransform)rib; rr.sizeDelta = UiKit.PxSize(Layout.GdBadge) + new Vector2(0, BadgeTitlePadPx); rr.anchoredPosition = new Vector2(0, 6); Overlay.FitRibbonText(rr); }
            var slot = UiKit.Rect(box, "IconSlot"); UiKit.Pct(slot, Layout.GdIcon.Within(B));
            if (g != null) { var cell = Cell(slot, app.Data, g, new CellOpts(), null); UiKit.Stretch(cell); }
            else
            {
                // 빈 슬롯도 «물건 칸» 이라 장비 화면의 그 프레임(ItemFrame_01 · Add_1 = 빈 칸 «+» · T69 7항 통일) — 예전 fr.itemBg 회색 판은 폐기 · 검은 아웃라인은 DarkFrame
                var e = UiKit.Spawn("ui.itemFrame.empty", slot); e.name = "Empty"; var ert = (RectTransform)e.transform;
                UiKit.FitScale(ert, UiKit.PxSize(Layout.GdIcon));
                UiKit.Hide(ert, "Item", "Text_Level", "Focus", "Disable", "Lock", "Add_2"); UiKit.Show(ert, "Add_1", true);
                DarkFrame(ert, ert.localScale.x);
            }
            var nmR = Layout.GdName.Within(B);
            var nm = UiKit.Label(box, nmR.X, nmR.Y, nmR.W, nmR.H, name, 44, nameColor, TextAnchor.MiddleLeft, true, true); nm.name = "Name"; nm.fontStyle = FontStyles.Bold;
            var meta = Layout.GdMeta.Within(B);
            // pill 글자 = 본문 40(T63-gear) · pill 폭 47 → 48%(«슬롯 Lv. 0/150» 40 ≈ 290px 이 안쪽 294px 에)
            var p1 = Pill(box, "Pill1", new Layout.R(meta.X, meta.Y, meta.W * 0.48f, meta.H)); var t1 = UiKit.Text(p1, pill1, TextSize.Body, Palette.Cream, TextAnchor.MiddleCenter, true, true); UiKit.Stretch(t1.rectTransform, 8, 2, 8, 2);
            var p2 = Pill(box, "Pill2", new Layout.R(meta.X + meta.W * 0.52f, meta.Y, meta.W * 0.48f, meta.H)); var t2 = UiKit.Text(p2, pill2, TextSize.Body, Palette.Cream, TextAnchor.MiddleCenter, true, true); UiKit.Stretch(t2.rectTransform, 8, 2, 8, 2);
            // T46 이름표(표 ④ «요소» 글자 그대로 · 하니스 layout.json)
            UiKit.Tag(box, "팝업 박스"); if (rib != null) UiKit.Tag(rib, "등급 배지"); UiKit.Tag(slot, "아이템 아이콘(정사각)"); UiKit.Tag(nm.rectTransform, "이름줄"); UiKit.TagGroup(box, "메타줄(레벨·부위)", p1, p2);
            var tap = UiKit.Find(ov.Root, "TapToClose"); if (tap != null) UiKit.Tag(tap, "닫기 안내");
            return box;
        }
        /// <summary>
        /// 스탯 박스(GdStats) — 머리 «스탯» + 부위 역할에 맞는 줄(T88 · 공격 부위 = «공격력» 한 줄 · 방어 부위 = «체력»·«실드» 두 줄 · 값은 초록 «+N»). 빈 슬롯은 안내 한 줄.
        /// 값은 <see cref="GearSystem.ContributionIn"/> 재분배 결과 — 이 장비를 그 부위에 넣은 빌드에서 «같은 역할끼리 원래 비율대로 나눠 가진» 몫이라 합계는 그대로다.
        /// 줄 수가 달라지므로 남은 높이(76%)를 줄 수로 다시 나눈다(빈 줄 없음 · 아이콘 크기는 그대로 두고 줄 가운데에).
        /// </summary>
        static RectTransform StatsBox(RectTransform box, GameData D, SaveData S, GearItem g, string part, int lv, bool eqd, Layout.R? boxOverride = null)
        {
            // ⚠ `.Within` 에는 **이 요소가 실제로 들어앉을 상자**를 넘겨야 한다 — 넘기는 상자와 그려지는 상자가 다르면
            //    안쪽 자리가 그 높이 비(比)만큼 조용히 눌린다(T267 6단계 실측 · 결정 809).
            var st = Layout.GdStats.Within(boxOverride ?? Layout.GdBox);
            var sp = Pill(box, "Stats", st, 0.75f); UiKit.Tag(sp, "스탯 섹션");
            string gh = Hex(Palette.Green);
            // 글자 전부 본문 40(T63-gear) — 상자 9.0% = 210px: 머리 24%(50px) + 줄 3 × 25%(52px ≥ 한 줄 49px) = 99%
            UiKit.Label(sp, 3, 0, 60, 24, "스탯", TextSize.Body, Palette.Cream, TextAnchor.MiddleLeft, true, true).fontStyle = FontStyles.Bold;
            if (g == null)
            {
                string what = GearRole.IsAttack(part) ? "공격력" : "체력·실드";   // T88 — 부위 역할에 맞는 안내
                UiKit.Label(sp, 3, 26, 94, 72, $"슬롯 1레벨당 이 부위 장비의 {what} +{D.Gear.SlotStep * 100:0.#}% (상한 Lv.{D.Gear.SlotLvMax})", TextSize.Body, Palette.CreamDark, TextAnchor.MiddleLeft, true, true);
                return sp;
            }
            // 이 장비를 자기 부위에 넣은 빌드에서 재분배한 몫(장착중이면 실제 기여 그대로 · 인벤 장비면 «끼우면 이만큼» )
            var b = S != null ? S.CurBuild(D) : new Build();
            b.Eq[g.Part] = g; b.Slots[g.Part] = lv;
            var c = GearSystem.ContributionIn(D, b, g.Part);
            var rows = GearRole.IsAttack(g.Part)
                ? new (string icon, string label, double v)[] { (Icons.Stat("dmg"), "공격력", c.Atk) }
                : new (string icon, string label, double v)[] { ("pi.heart", "체력", c.Hp), ("pi.shield", "실드", c.Sh) };
            float rh = 76f / rows.Length;   // 머리 24% 아래를 줄 수로 나눈다(빈 줄 남기지 않기 · T88 4항)
            for (int i = 0; i < rows.Length; i++)
            {
                float y = 24 + i * rh;
                var ic = UiKit.Icon(sp, "ic", rows[i].icon); UiKit.Pct(ic.rectTransform, 3, y + (rh - 22) * 0.5f, 6, 22);
                var t = UiKit.Label(sp, 11, y, 86, rh, $"{rows[i].label}  <color=#{gh}>+{UiKit.Fmt(rows[i].v)}</color>", TextSize.Body, Palette.Cream, TextAnchor.MiddleLeft, true, true); t.name = "Stat:" + i;
            }
            if (eqd) { var s2 = UiKit.Label(box, st.X + st.W * 0.55f, st.Y, st.W * 0.44f, st.H * 0.24f, $"슬롯 Lv당 +{D.Gear.SlotStep * 100:0.#}%", TextSize.Body, Palette.CreamDark, TextAnchor.MiddleRight, true, true); s2.name = "SlotHint"; }
            return sp;
        }
        /// <summary>
        /// 옵션 줄(GdOpts · 줄 피치 ≤ 2.4%) — 해금 = 등급색 세트 아이콘 + 등급색 글자 · 잠금 = 자물쇠 + 흐린 글자 «(등급)». 규칙(OptCount) 은 기존 그대로.
        /// 글자 = 본문 40 한 줄(T63-gear): 7줄 × 53px 피치(16%) · 줄 94% = 50px ≥ 한 줄 49px · 문구는 <see cref="GearText.Shorten"/>(«치명타 시 50%: 도끼 1개(공격력 50%)») · 잠금 꼬리 «(희귀)»(«이상» 은 자물쇠가 대신) — 가장 긴 잠금 줄도 bestFit 36 이상.
        /// </summary>
        static void OptionRows(RectTransform box, GameData D, GearItem g, Layout.R? boxOverride = null)
        {
            // ⚠ 위 <see cref="StatsBox"/> 와 같은 까닭 — 여기서 눌리면 줄 높이까지 같이 눌려 **본문 40 이 잘린다**
            //    (보기 전용 팝업에서 16.0% → 13.25% · 줄 53px → 44px · 결정 809).
            var region = Layout.GdOpts.Within(boxOverride ?? Layout.GdBox);
            var opts = D.Gear.Options.TryGetValue(g.Type, out var ol) ? ol : new List<GearOption>();
            int n = D.Gear.OptCount(g.Rar, g.Plus);
            var host = UiKit.Rect(box, "Options"); UiKit.Pct(host, region); UiKit.Tag(host, "옵션 목록");
            if (opts.Count == 0) { UiKit.Label(host, 2, 0, 96, 100, "세트 옵션 없음", TextSize.Body, Palette.CreamDark); return; }
            float pitch = Mathf.Min(Layout.GdOptPitch, Layout.GdOpts.H / opts.Count);   // 프레임 % → 줄 하나가 차지하는 비율
            float rowPct = pitch / Layout.GdOpts.H * 100f;
            for (int i = 0; i < opts.Count; i++)
            {
                bool on = i < n; bool mythPlus = D.Gear.OptNeedsMythPlus(i);
                var color = mythPlus ? Palette.Plum : Palette.ByName(Palette.RarName(D.Gear.OptTierRar(i)));
                var row = Pill(host, "Opt:" + i, new Layout.R(0, i * rowPct, 100, rowPct * OptRowFill), on ? 0.7f : 0.6f);
                // T160 ⓐ(주인 «자물쇠 색을 해당 열리는 등급 색으로») — 잠겨 있어도 색은 죽이지 않는다.
                // «잠김» 은 자물쇠 «그림» 이 말한다(T69-gear ⓕ 원문) · 흐림(CanvasGroup)은 T177 로 걷었다.
                var ic = UiKit.Icon(row, "ic", on ? SetIcon(Set(D, g)) : "ui.iconLock", color); UiKit.Pct(ic.rectTransform, 1.5f, 12, 5, 76);
                // T160 ⓑ(주인 «옵션에 «(신화)» «(신화 +3)» 이런 거 넣지 말라») — 꼬리를 안 붙인다.
                // GearText.LockSuffix 자체는 남긴다(순수 함수 · EditMode 테스트가 쓴다) — 부르는 곳이 여기 하나뿐이라 여기서만 뗀다(지시서 T160 2항 ⓘ).
                string desc = GearText.Shorten(opts[i].Desc);
                var t = UiKit.Label(row, 7.5f, 0, 91.5f, 100, desc, TextSize.Body, on ? OnDarkPill(color) : Palette.OptLocked, TextAnchor.MiddleLeft, true, true);
                // T177(주인 «잠겨 있는 거는 글씨가 색이 666666») — Label 이 만들면서 EnsureBright(T111 ⓑ)로 흰색이 됐으므로 표식을 붙여 다시 넣는다.
                // 흐림(CanvasGroup 0.9)은 걷었다 — 색과 겹치면 두 번 흐려져 주인이 준 값이 안 나온다(지시서 T177 1항).
                if (!on) UiKit.DarkText(t, Palette.OptLocked);
            }
        }
        /// <summary>옵션 줄이 피치에서 차지하는 비율 — 16% ÷ 7줄 = 53px 피치 × 0.94 = 50px(본문 40 한 줄 49px 이 들어간다 · 줄 사이 3px).</summary>
        public const float OptRowFill = 0.94f;
        /// <summary>
        /// 비용 줄(GdCost) — 🪙 «보유/비용»(보유가 모자라면 빨강 · 충분하면 초록) · MAX 면 «슬롯 MAX (Lv.N)».
        /// <para>
        /// T290 3항 — 드는 것이 둘이면(골드 + 그 부위 레시피) <b>줄 하나를 두 칸으로 나눈다</b>. 나누는 것은 <b>폭뿐</b>이다:
        /// 줄 자체(<see cref="Layout.GdCost"/>)도, 글자 크기(<see cref="TextSize.Body"/>)도, 골드 글자칸의 폭(40%)도 그대로라
        /// 표 ④ «비용줄» 행과 <c>LayoutSpecTests</c> 가 흔들리지 않는다 — 옮긴 것은 골드 묶음의 <b>시작 x</b>(30 → 6)뿐이고
        /// 그렇게 비운 오른쪽 절반에 레시피 칸이 들어간다.
        /// </para>
        /// <para>
        /// <b>레시피가 안 드는 판</b>(표가 없거나 <c>perLevel 0</c> · <c>need == 0</c>)이면 <b>옛 자리 그대로</b> 골드 한 칸만 그린다 —
        /// 안 드는 것을 «0/0» 으로 그리면 사람은 그것을 «못 채운 조건» 으로 읽는다.
        /// </para>
        /// </summary>
        static void CostRow(RectTransform box, GameData D, SaveData S, string part, double cost, bool maxed, int maxLv)
        {
            var r = Layout.GdCost.Within(Layout.GdBox);
            var row = Pill(box, "Cost", r, 0.75f); UiKit.Tag(row, "비용줄");
            int need = maxed ? 0 : Recipes.Need(D != null ? D.Recipe : null, S.SlotLv(part));
            // T159(주인 «가격 표시 옆에 재화가 골드 아이콘이어야») — 이 게임의 골드 재화 아이콘은 ui.coin 이다
            // (클리어·사망 보상 · 출석 · 탐험 · 챕터 보상 · 던전 보상이 전부 같은 키를 쓴다 · pi.coins 는 픽토 그림이라 이 팝업만 달랐다).
            // 칸은 줄 높이에 맞춘 정사각으로 — 폭 5%(36.6px) × 높이 84%(27.4px) 라 가로로 남던 자리를 지운다(그림 크기는 그대로 27.4px · T136 과 같은 갈래).
            float goldIconX = need > 0 ? 6f : 30f, goldTextX = goldIconX + 6f;
            var ic = UiKit.Icon(row, "ic", "ui.coin"); UiKit.Pct(ic.rectTransform, goldIconX, 8, CostIconWPct, 84);
            string s = maxed ? $"슬롯 MAX (Lv.{maxLv})" : $"<color=#{Hex(S.Gold >= cost ? Palette.Green : Palette.Red)}>{UiKit.Fmt(S.Gold)}</color>/{UiKit.Fmt(cost)}";
            var t = UiKit.Label(row, maxed ? 36f : goldTextX, 0, 40, 100, s, TextSize.Body, Palette.Cream, TextAnchor.MiddleLeft, true, true); t.name = "CostText";
            if (need <= 0) return;
            // 레시피 칸 — 골드와 같은 색 규칙(모자라면 빨강)·같은 글자 크기. 개수라 K·M 으로 줄이지 않는다(FmtQty).
            var ic2 = UiKit.Icon(row, "icRecipe", Recipes.Icon(part)); UiKit.Pct(ic2.rectTransform, 55, 8, CostIconWPct, 84);
            int have = Recipes.Count(S, part);
            string s2 = $"<color=#{Hex(have >= need ? Palette.Green : Palette.Red)}>{UiKit.FmtQty(have)}</color>/{UiKit.FmtQty(need)}";
            var t2 = UiKit.Label(row, 61, 0, 37, 100, s2, TextSize.Body, Palette.Cream, TextAnchor.MiddleLeft, true, true); t2.name = "RecipeText";
        }
        /// <summary>
        /// «보기 전용» 세부 팝업의 상자 — <see cref="Layout.GdBox"/> 에서 비용 줄·버튼 자리(아래 8%p)를 잘라 낸 것이다(T267 4항 · 표 ㊿).
        /// <para>바닥 66.5% 는 주인 그림 `38_box_item_detail.jpg` 실측 바닥 66.74% 와 0.24%p 차다 — <b>자른 자리는 레퍼런스와 같다</b>.
        /// 윗변만 레퍼런스(33.2%)보다 5.2%p 위인데, 그것은 이 표가 아니라 <b>표 ④ 가 T63-gear 로 안쪽 칸을 키운 몫</b>이 그대로 따라온 것이다(표 ㊿ 의 ⚑).</para>
        /// </summary>
        public static readonly Layout.R InfoBox = new Layout.R(6.5f, 28.0f, 87.0f, 38.5f);

        /// <summary>비용 줄 재화 아이콘 칸의 폭(줄 %) — 줄이 732.9×32.6px 이라 높이 84%(27.4px)와 같은 폭이 되는 값이다(T159 · 정사각).</summary>
        public const float CostIconWPct = 3.74f;
        /// <summary>표 ④ «장비 세부 팝업»: 등급 탭 → 아이콘 칸(+N) · 이름 · «슬롯 Lv. N/최대»·«부위» pill → 스탯 박스(초록 +값) → 옵션 줄(등급색 · 잠금 흐림) → 비용 줄 → 해제/장착(파랑) · 슬롯 강화(주황) → «탭하여 닫기». 규칙·수치는 예전 그대로.</summary>
        public static void OpenDetail(App app, GearItem g, Action onChanged)
        {
            var D = app.Data; var S = app.Save; var ov = app.Overlay;
            if (g == null) { ov.Close(); return; }
            if (g.IsNew) { g.IsNew = false; app.Persist(); onChanged?.Invoke(); }
            int lv = S.SlotLv(g.Part); double cost = D.Gear.SlotCost(lv); bool eqd = S.IsEquipped(g); bool maxed = lv >= D.Gear.SlotLvMax;
            var shown = Tier(D, g); string colorName = FrameColor(D, g);   // T316 — 이름·«+N» 은 표시 등급(갓·초월·불멸·무한) 기준
            var box = DetailFrame(app, shown.Name, g, Name(D, g) + PlusText(D, g), OnPopupBox(Palette.ByName(colorName)), $"슬롯 Lv. {lv}/{D.Gear.SlotLvMax}", PartName(D, g.Part));
            StatsBox(box, D, S, g, g.Part, lv, eqd);
            OptionRows(box, D, g);
            CostRow(box, D, S, g.Part, cost, maxed, D.Gear.SlotLvMax);
            var B = Layout.GdBox;
            RectTransform left;
            if (eqd) left = UiKit.Button(box, "ui.btnBlue", "해제", () => { S.Eq.Remove(g.Part); app.Persist(); Audio.Sfx("snd.equip"); ov.Close(); onChanged?.Invoke(); }, Layout.GdBtnL.Within(B));
            else left = UiKit.Button(box, "ui.btnBlue", "장착", () => { S.Eq[g.Part] = g.Uid; g.IsNew = false; app.Persist(); Audio.Sfx("snd.equip"); ov.Close(); onChanged?.Invoke(); }, Layout.GdBtnL.Within(B));
            left.name = "BtnL";
            var up = UiKit.Button(box, "ui.btnOrange", maxed ? "슬롯 MAX" : "슬롯 강화", () =>
            {
                // T290 — 거래는 GearSystem.SlotUp 한 곳이다(골드·레시피를 같이 보고 같이 뺀다 · 모자라면 아무것도 안 바뀐다).
                if (!GearSystem.SlotUp(D, S, g.Part, out string why)) { app.Toast(why); return; }
                app.Persist(); onChanged?.Invoke(); OpenDetail(app, g, onChanged);
            }, Layout.GdBtnR.Within(B)); up.name = "BtnR";
            UiKit.SetInteractable(up.GetComponent<Button>(), GearSystem.CanSlotUp(D, S, g.Part, out _));   // T290 — 누를 수 있는 조건도 같은 함수가 정한다
            UiKit.TagGroup(box, "버튼 2개", left, up);
        }

        /// <summary>«보기 전용» 장비 세부 팝업 — 아래 두 버튼(장착/해제 · 슬롯 강화)과 비용 줄이 없다.
/// (T267 4항 · 주인 2026-09-09 «거기서 아이템 클릭 시 세부 정보 뜨는 팝업도 잘 만드쇼. 아마 원래 장비 클릭 시 세부 정보 뜨는 거에서
/// 아래 두 버튼만 없애고 레이아웃 좀만 조절하면 똑같음.»)
/// <para>
/// <b>새로 만들지 않고 인자 하나로 갈랐다</b>(지시서 4항이 그렇게 권했다) — 같은 <see cref="DetailFrame"/>·<see cref="StatsBox"/>·<see cref="OptionRows"/> 를 그대로 쓰고
/// 상자만 짧게 잘라(<see cref="InfoBox"/>) 비용 줄·버튼 자리를 없앤다. 안쪽 자리는 «화면 %» 라 잘린 선 위는 한 픽셀도 안 움직인다.
/// <b>그러려면 셋 다에 같은 상자를 넘겨야 한다</b> — 4항에서는 <see cref="DetailFrame"/> 에만 넘기고 나머지 둘은 <see cref="Layout.GdBox"/> 를 박아 두어
/// 스탯·옵션이 38.5/46.5 = 0.83 배로 눌려 있었다(옵션 줄 53px → 44px 라 <b>본문 40 이 잘릴 자리</b>였다). T267 6단계 실측이 잡았다 — 결정 809.
/// </para>
/// <paramref name="onClose"/> — 닫을 때 돌아갈 곳(확률 팝업이 제 자신을 다시 연다 · 프로필 팝업 둘이 쓰는 그 꼴 · 표 ㉟).
/// ⚠ 이 팝업은 <b>아무것도 안 바꾼다</b> — 세이브도 지갑도 안 만진다(보여 주기 전용).</summary>
        public static void OpenInfo(App app, GearItem g, Action onClose = null)
        {
            if (app == null || g == null) return;
            var D = app.Data; var S = app.Save; var ov = app.Overlay;
            var shown = Tier(D, g); string colorName = FrameColor(D, g);   // T316 — 보기 전용 팝업(38)도 같은 규칙
            var box = DetailFrame(app, shown.Name, g, Name(D, g) + PlusText(D, g), OnPopupBox(Palette.ByName(colorName)),
                PartName(D, g.Part), shown.Name, InfoBox, onClose != null ? onClose : (Action)(() => ov.Close()));
            // 스탯·옵션은 «이 등급의 이 부위» 가 어떤 물건인지를 보여 준다 — 슬롯 레벨은 내 세이브 것이라 0 으로 본다(남의 상자 안 물건이다).
            StatsBox(box, D, S, g, g.Part, 0, false, InfoBox);
            OptionRows(box, D, g, InfoBox);
        }

        /// <summary>빈 부위 팝업 — 같은 구도(장비 없는 상태 · 등급 탭 = «부위 슬롯» · 빈 아이콘 칸 · 스탯 박스에 슬롯 안내 · 옵션 자리에 «장착된 장비가 없습니다» · 비용 줄 · 강화만 · 탭하여 닫기).</summary>
        public static void OpenSlot(App app, string part, Action onChanged)
        {
            var D = app.Data; var S = app.Save; var ov = app.Overlay;
            int lv = S.SlotLv(part); double cost = D.Gear.SlotCost(lv); bool maxed = lv >= D.Gear.SlotLvMax;
            var box = DetailFrame(app, $"{PartName(D, part)} 슬롯", null, "비어 있음", Palette.InkLight, $"슬롯 Lv. {lv}/{D.Gear.SlotLvMax}", PartName(D, part));
            StatsBox(box, D, S, null, part, lv, false);
            var region = Layout.GdOpts.Within(Layout.GdBox);
            UiKit.Label(box, region.X, region.Y, region.W, region.H, "장착된 장비가 없습니다\n인벤에서 이 부위의 장비를 골라 장착하세요", TextSize.Body, Palette.InkLight, TextAnchor.MiddleCenter, true, false).name = "EmptyHint";
            CostRow(box, D, S, part, cost, maxed, D.Gear.SlotLvMax);
            var up = UiKit.Button(box, "ui.btnOrange", maxed ? "슬롯 MAX" : "슬롯 강화", () =>
            {
                // T290 — 거래는 GearSystem.SlotUp 한 곳이다(위 OpenDetail 의 강화 버튼과 같은 함수를 쓴다).
                if (!GearSystem.SlotUp(D, S, part, out string why)) { app.Toast(why); return; }
                app.Persist(); onChanged?.Invoke(); OpenSlot(app, part, onChanged);
            }, Layout.GdBtnR.Within(Layout.GdBox)); up.name = "BtnR";
            UiKit.SetInteractable(up.GetComponent<Button>(), GearSystem.CanSlotUp(D, S, part, out _));   // T290
            UiKit.TagGroup(box, "버튼 2개", up);
        }
    }
}
