using System;
using System.Collections.Generic;
using DG.Tweening;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 상점 = <b>docs/ref/09_shop_1.jpg · 10_shop_2.jpg 구도</b>(T40 · 주인 지시 2026-09-06 «UI 는 무조건 레퍼런스 jpg 기준» — «Shop_List 그대로»(T9) 를 대체).
    /// 배치의 정본 = ref-layout.md ⑤ 표(<see cref="Layout.ShopSec1"/> 계열 · 프레임 % · ±3%p) + 표에 없는 자리는 워커가 10_shop_2.jpg 에서 잰 값(아래 상수 · 5% 격자).
    /// 한 화면이 <b>세로 스크롤</b> 하나다(레퍼런스 두 장 = 같은 화면의 위·아래): 상단 재화 바(<see cref="TopBar"/>) → 천막 띠 → [스크롤] 최상위 상자 큰 카드 → 나머지 상자 2칸 나란히 →
    /// «무료 보급까지 hh:mm:ss» → «다이아» 섹션(3열×2행) → «골드» 섹션(3열×1행) → 탭 바.
    /// 그림 재료는 주인 에셋만 — 데모 프리팹은 <b>부품</b>이다: Shop_List 의 Background·Roof(천막) 조각 · CardFrame_04(상자 카드 · 등급색) · ListItem_ShopItem(다이아/골드 칸 · 레퍼런스와 같은 «수량 · 그림 · 이름 · 가격 띠» 구성) ·
    /// Button_Info((i) · 확률·천장 팝업) · BasicFrame TransperDark(설명/천장 pill) · Title_LineDeco(섹션 제목) · GUI Pro ShopItem 의 Gem_1~6/Gold_1~3 그림(수량이 커질수록 큰 더미). 코드 도형 0.
    /// 글자·수치는 우리 데이터(gacha.json 상자·확률·천장·무료 보급 · shop.json 다이아/골드 상품 · 표시 배치만 레퍼런스). 뽑기 결과·정보 팝업 = 공통 팝업 문법(<see cref="UiKit.Popup"/> · 명판 · 패널 · 격자 = <see cref="GearUi.Cell"/> · 탭하여 닫기).
    /// </summary>
    public sealed class ShopScreen : GameScreen
    {
        public override string Name => "shop";

        // ───────────────────────── 자리(프레임 %) — 표 ⑤ + 워커 실측(10_shop_2.jpg) ─────────────────────────
        /// <summary>천막 띠 = 상단 바(3.7+4.5) 바로 아래 · 스크롤 창은 그 밑에서 탭 바(92.6) 까지.</summary>
        static readonly Layout.R RoofBand = new Layout.R(0, 8.0f, 100, 5.0f);
        const float ContentTop = 12.8f;
        static readonly Layout.R ScrollView = new Layout.R(0, ContentTop, 100, 92.6f - ContentTop);
        /// <summary>
        /// «상자» 섹션 헤더 y (T100 · 주인 2026-09-07 «상자 부분도 다이아·골드처럼 섹션 나눠 달라») — 스크롤 맨 위(<see cref="ContentTop"/> 12.8) 바로 아래.
        /// 헤더가 하나 늘어난 만큼 <b>아래 전부</b>가 <see cref="SecShift"/> 만큼 내려간다.
        /// </summary>
        const float SecBoxY = 13.5f + SecTopGap;
        /// <summary>헤더 → 그 아래 첫 내용까지의 간격 — «다이아» 헤더(74.0)와 첫 카드행(78.5)의 간격 그대로. T100 이 «상자» 헤더를 끼우며 아래를 이만큼 민다.</summary>
        const float SecShift = 4.5f;
        /// <summary>
        /// T260 2항 — <b>섹션 제목 «위» 여백 10px</b>(주인 2026-09-09 05:4X «섹션 나누는 타이틀 같은 거 위로 각각 여백 좀 10씩 주기 · 너무 딱딱 붙어 있음»).
        /// 프레임 높이(<see cref="UiKit.FrameH"/> 2337)로 환산한 % — 픽셀을 코드에 박지 않고 «10px» 이라는 주인 말을 그대로 남긴다.
        /// <para>
        /// 헤더가 셋(상자·다이아·골드)이라 <b>아래로 갈수록 쌓인다</b> — 상자 아래는 1칸, 다이아 아래는 2칸, 골드 아래는 3칸.
        /// <see cref="ContentEnd"/> 도 3칸 내려가므로 <b>스크롤 맨 아래에서 골드 섹션은 제자리</b>이고(표 ⑤ 09 의 66.0 · 70.5 불변)
        /// 다이아 섹션만 그 화면에서 <see cref="SecTopGap"/> 만큼 올라온다(22.60 → 22.17 · 27.10 → 26.67 · 47.60 → 47.17).
        /// </para>
        /// </summary>
        const float SecTopGap = 10f / UiKit.FrameH * 100f;
        /// <summary>표 ⑤ «(뽑기 화면) 대형 상자 배너» · «상자 카드 2개(좌우 각 45.5)» · «상자 버튼 2개(배너 안 아래)» — T100 의 «상자» 헤더만큼(<see cref="SecShift"/>) 내려간 자리 + T260 의 «상자» 헤더 위 여백 한 칸.</summary>
        static readonly Layout.R Banner = new Layout.R(3.0f, 13.5f + SecShift + SecTopGap, 94.0f, 26.0f);
        static readonly Layout.R ChestRow = new Layout.R(3.0f, 40.5f + SecShift + SecTopGap, 94.0f, 29.0f);
        const float ChestCardW = 45.5f;
        static readonly Layout.R FreeLine = new Layout.R(3.0f, 70.3f + SecShift + SecTopGap, 94.0f, 2.8f);
        /// <summary>
        /// «다이아» 섹션 헤더 y — 09_shop_1.jpg 의 헤더(22.5)·카드행(27 · 47.5)·둘째 헤더(66)·행(70.5) 간격을 그대로 이어 붙인다(<see cref="Layout.ShopSec1"/> 계열 표값에서 계산).
        /// T100 의 «상자» 헤더만큼 내려가지만 <see cref="ContentEnd"/> 도 같이 내려가므로 <b>스크롤 맨 아래에서 보이는 09 화면은 한 픽셀도 안 바뀐다</b>(표 ⑤ 09 행 불변).
        /// <para>
        /// T260 — 여기에 «위 여백»(<see cref="SecTopGap"/>)이 <b>두 칸</b> 쌓인다: 제 몫 한 칸 + 위에 있는 «상자» 헤더 몫 한 칸.
        /// 골드 헤더는 세 칸이고 <see cref="ContentEnd"/> 도 세 칸이라, 09 화면에서 <b>골드 섹션만 제자리</b>이고 다이아 섹션은 한 칸 올라온다(표 ⑤ 를 그렇게 고쳤다).
        /// </para>
        /// </summary>
        const float SecGemY = 74.0f + SecShift + 2f * SecTopGap;
        static float Row1Y => SecGemY + (Layout.ShopCardRow1.Y - Layout.ShopSec1.Y);          // 78.5
        static float Row2Y => Row1Y + Layout.ShopCardRowPitch;                                  // 99.0
        // T260 — 골드 헤더와 그 아래는 «위 여백» 세 칸째다(상자 · 다이아 · 골드 제 몫).
        static float SecGoldY => SecGemY + (Layout.ShopSec2.Y - Layout.ShopSec1.Y) + SecTopGap;
        static float Row3Y => SecGemY + (Layout.ShopCardRow3.Y - Layout.ShopSec1.Y) + SecTopGap;
        /// <summary>내용 끝 = 골드 행 아래 여백 3.5(레퍼런스 09 의 골드 카드 아래 ~ 탭 바 = 2.9) → 끝까지 내리면 «다이아» 헤더가 표 ⑤ 09 의 22.5 자리에 온다(= 09 스크린샷 = 스크롤 맨 아래).</summary>
        static float ContentEnd => Row3Y + Layout.ShopCardRow3.H + 3.5f;                        // 144.0
        static readonly Layout.R InfoBox = new Layout.R(6.5f, 27.0f, 87.0f, 42.0f);
        const int ResultCols = 4;

        // ───────────────────────── 글자 크기(T63-shop · 주인 «글씨 너무 작다») — 픽셀 리터럴 금지(§1): 종류 하한(TextSize) 또는 표 높이에서 계산 ─────────────────────────
        /// <summary>상품 카드 안 수량 띠 높이(카드 %) — 레퍼런스 09 카드의 «100 / 600 / 1800» 띠(위 5~19%). 수량 글자 크기는 이 띠 높이에서 계산한다(<see cref="QtySize"/>).</summary>
        public const float QtyBandH = 14f;
        /// <summary>상품 수량 글자 = 수량 띠 높이(카드 18.5% × 14%) 에서 계산 ≈ 51(전에는 리터럴 50).</summary>
        public static int QtySize => UiKit.FontForHeight(Layout.ShopCard1.H * QtyBandH / 100f);
        /// <summary>섹션 헤더 «상자»·«다이아»·«골드» = 표 ⑤ «섹션 헤더» 높이(2.5%) 에서 계산 ≈ 50(전에는 40 — 레퍼런스 «Gem/Gold» 는 본문보다 크다).</summary>
        public static int HeaderSize => UiKit.FontForHeight(Layout.ShopSec1.H);
        /// <summary>섹션을 나누는 라인 데코(<c>LineDeco</c>)의 알파 — <b>주인 2026-09-07 · 255 중 13</b>(T100 ⓒ). 제목 글자는 그대로 두고 선만 옅게.</summary>
        public const float SecLineAlpha = 13f / 255f;
        /// <summary>가격 줄의 다이아 아이콘 한 변 = 버튼 글자 크기(정사각 · 글자 높이와 같게).</summary>
        public const int PriceIconSize = TextSize.Button;
        /// <summary>가격 줄 요소 간격(px).</summary>
        const float PriceGap = 10f;

        TopBar _top; RectTransform _content; ScrollRect _scroll;
        TMP_Text _freeTxt; readonly List<Button> _freeBtns = new List<Button>(); readonly List<GameObject> _freeDots = new List<GameObject>();
        readonly Dictionary<string, BoxWidgets> _box = new Dictionary<string, BoxWidgets>();
        readonly List<(Button btn, Func<bool> can)> _gated = new List<(Button, Func<bool>)>();
        /// <summary>빛살이 도는 칸(T72 ② · 4항 «보이는 칸만» — 스크롤 밖 칸은 <see cref="UiKit.SetLightSpinning"/> 으로 멈춘다).</summary>
        readonly List<RectTransform> _lightCells = new List<RectTransform>();
        /// <summary>빛살을 걸 자리(칸 · 아이콘 · 조각 키) — <b>배치가 끝난 뒤</b> 한꺼번에 건다(아이콘 rect 가 % 앵커라 Build 중에는 0 이고, 그러면 빛살 한 변이 0 이 된다).</summary>
        readonly List<(RectTransform host, RectTransform icon, string key)> _lightPlan = new List<(RectTransform, RectTransform, string)>();
        float _timerT;
        sealed class BoxWidgets { public Button One, Ten, Key; public TMP_Text KeyCount, KeyLabel; public readonly List<TMP_Text> Pills = new List<TMP_Text>(); }

        static string Today() => DateTime.Now.ToString("yyyy-MM-dd");
        static bool CanFree(SaveData S) => S.FreeDay != Today();

        protected override void Build()
        {
            var D = App.Data;
            // ⓪ 배경 + 천막 = Shop_List 프리팹의 Background·Roof 조각만(나머지 조각은 통째로 끔) · 배경은 레퍼런스의 어두운 바탕색
            var shell = UiKit.Spawn("ui.shopList", Root); var srt = (RectTransform)shell.transform; UiKit.Stretch(srt);
            var bg = UiKit.Find(srt, "Background"); var roof = UiKit.Find(srt, "Roof");
            var bgImg = bg != null ? bg.GetComponent<Image>() : null;
            if (bg != null) { bg.SetParent(Root, false); UiKit.Stretch((RectTransform)bg); bg.SetAsFirstSibling(); }
            if (bgImg == null) bgImg = UiKit.Ensure<Image>(Root.gameObject);
            bgImg.color = Palette.Hex("#2B2B30"); bgImg.raycastTarget = true;
            if (roof != null) { roof.SetParent(Root, false); UiKit.Pct((RectTransform)roof, RoofBand); var ri = roof.GetComponent<Image>(); if (ri != null) ri.color = Color.Lerp(Palette.Red, Palette.Ink, 0.45f); }
            // 껍데기는 «비활성으로 남기지 않고» 트리에서 떼어 파괴한다 — 비활성 자식도 UiKit.Find(깊이 검색) 에 잡혀 프리팹 안 Content·중첩 Tab_01_BottomFlushMenu 가 우리 것보다 먼저 걸린다(CI #66·#68·#69 상점 2건 · T48)
            shell.transform.SetParent(null, false); shell.SetActive(false); UnityEngine.Object.Destroy(shell);
            // T72 ① 배경 패턴 — 어두운 회색 바탕(#2B2B30) 위 «흰» 무늬가 오른쪽 위로 천천히 흐른다(주인 «거의 모든 UI 에 · 로비 배경처럼»)
            // 배경 조각이 Root 의 첫 자식이므로 그 «바로 위»(형제 1) — 천막·스크롤·상단 바·탭 바는 그 위에 그려진다
            UiKit.PatternBg(Root, UiKit.PatternTintDark, UiKit.PatternTileSeconds, bg != null ? 1 : 0);

            // ① 스크롤 창(천막 아래 ~ 탭 바 위) — 내용은 프레임 % 로 Content 안에 놓는다(<see cref="Place"/>)
            var view = UiKit.Rect(Root, "Scroll"); UiKit.Pct(view, ScrollView); UiKit.Ensure<RectMask2D>(view.gameObject);
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            _scroll = view.gameObject.AddComponent<ScrollRect>(); _scroll.horizontal = false; _scroll.movementType = ScrollRect.MovementType.Clamped; _scroll.scrollSensitivity = 40;
            _content = UiKit.Rect(view, "Content"); _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1); _content.pivot = new Vector2(0.5f, 1);
            _content.offsetMin = Vector2.zero; _content.offsetMax = Vector2.zero; _content.sizeDelta = new Vector2(0, (ContentEnd - ContentTop) / 100f * UiKit.FrameH);
            _scroll.content = _content; _scroll.viewport = view;

            // ② 상자 — 최상위(가장 비싼) 상자 = 큰 카드 · 나머지 2개 = 나란히(gacha.json 순서)
            // T100 ⓑ — «상자» 섹션 헤더(주인 «상자 부분도 다이아·골드 섹션처럼»). 다른 두 헤더와 같은 조각·같은 크기다.
            UiKit.Tag(Header(SecBoxY, "상자"), "(뽑기 화면) 상자 섹션 헤더");
            GachaBox big = BigBox(D);
            var small = new List<GachaBox>(); foreach (var b in D.Gacha.Boxes) if (b != big) small.Add(b);
            RectTransform bigCard = null; var smallCards = new List<RectTransform>(); var smallBottoms = new List<RectTransform>(); var bigBtns = new List<RectTransform>();
            if (big != null) { bigCard = Place(UiKit.Rect(_content, "Box:" + big.Key), Banner); BuildBigCard(bigCard, big, bigBtns); }
            for (int i = 0; i < small.Count && i < 2; i++)
            {
                var card = Place(UiKit.Rect(_content, "Box:" + small[i].Key), new Layout.R(ChestRow.X + i * (ChestRow.W - ChestCardW), ChestRow.Y, ChestCardW, ChestRow.H));
                BuildSmallCard(card, small[i], i == 0 ? ChestGradLeft : ChestGradRight); smallCards.Add(card);
                var bottom = UiKit.Rect(card, "Bottom"); UiKit.Pct(bottom, 0, 78, 100, 22); smallBottoms.Add(bottom);   // 카드 아래 띠(광고+가격 버튼 줄) — 09 에서 보이는 «광고/무료 카드 2개» 행의 측정 자리
            }
            // 비평 이름표(T46 · 표 ⑤) — 10 = «(뽑기 화면)» 행 3 · 09 = 스크롤 맨 아래에서 보이는 행들
            if (bigCard != null) UiKit.Tag(bigCard, "(뽑기 화면) 대형 상자 배너");
            if (smallCards.Count > 0) { UiKit.TagGroup(_content, "(뽑기 화면) 상자 카드 2개", smallCards.ToArray()); UiKit.TagGroup(_content, "광고/무료 카드 2개", smallBottoms.ToArray()); }
            if (bigBtns.Count > 0) UiKit.TagGroup(_content, "(뽑기 화면) 상자 버튼 2개", bigBtns.ToArray());

            // ③ «무료 보급까지 hh:mm:ss» (시계 아이콘 + 글자 · 카드 2칸 아래 왼쪽)
            var fl = Place(UiKit.Rect(_content, "FreeLine"), FreeLine);
            var clock = UiKit.Icon(fl, "Icon", "ui.iconClock"); UiKit.Pct(clock.rectTransform, 0, 0, 6.4f, 100);
            _freeTxt = UiKit.Label(fl, 7.5f, 0, 92, 100, "", TextSize.Body, Palette.White, TextAnchor.MiddleLeft);

            // ④ «다이아» 3열×2행(shop.json gemPacks · 원화 모의 결제 = 누르면 바로 지급) · ⑤ «골드» 3열×1행(goldPacks · 다이아 소모)
            var gems = D.Shop != null ? D.Shop.GemPacks : new List<ShopData.GemPack>();
            var golds = D.Shop != null ? D.Shop.GoldPacks : new List<ShopData.GoldPack>();
            UiKit.Tag(Header(SecGemY, "다이아"), "섹션 헤더");
            for (int i = 0; i < gems.Count && i < 6; i++)
            {
                var p = gems[i]; var slot = Place(UiKit.Rect(_content, "GemPack:" + i), CardRect(i < 3 ? Row1Y : Row2Y, i % 3));
                if (i == 0) UiKit.Tag(slot, "상품 카드(1칸)"); else if (i == 3) UiKit.Tag(slot, "상품 카드 2행");
                BuildPack(slot, UiKit.FmtQty(p.Gem), "shop.gem." + Mathf.Clamp(i + 1, 1, 6), "다이아 · 모의 결제", null, $"{p.Won:#,0}원", Color.Lerp(Palette.Plum, Palette.Ink, 0.35f),
                    () => { App.Save.Gem += p.Gem; App.Persist(); Refresh(); App.Toast($"다이아 {UiKit.FmtQty(p.Gem)} 지급 (모의 결제)"); }, "cardGem");
            }
            UiKit.Tag(Header(SecGoldY, "골드"), "두 번째 섹션 헤더");
            for (int i = 0; i < golds.Count && i < 3; i++)
            {
                var p = golds[i]; var slot = Place(UiKit.Rect(_content, "GoldPack:" + i), CardRect(Row3Y, i));
                if (i == 0) UiKit.Tag(slot, "두 번째 섹션 카드행");
                var btn = BuildPack(slot, UiKit.FmtQty(p.Gold), "shop.gold." + Mathf.Clamp(i + 1, 1, 3), "골드", "hud.gem", UiKit.FmtQty(p.Gem), Color.Lerp(Palette.Sky, Palette.Ink, 0.35f), () =>
                {
                    var S = App.Save; if (S.Gem < p.Gem) { App.Toast("다이아가 부족합니다"); return; }
                    S.Gem -= p.Gem; S.Gold += p.Gold; App.Persist(); Refresh(); Audio.Sfx("snd.coin"); App.Toast($"골드 {UiKit.Fmt(p.Gold)} 구매!");
                }, "cardGold");
                foreach (var b in btn) _gated.Add((b, () => App.Save.Gem >= p.Gem));
            }

            // ⑥ 상단 재화 바(공용 헬퍼 · 스크롤 위에 그린다) + 하단 탭 5칸(상점 활성)
            _top = TopBar.Build(App, Root); UiKit.Tag(_top.Root, "상단 바");
            NavBar.Attach(this, Root, "shop"); UiKit.Tag(UiKit.Find(Root, "ui.tabBar"), "하단 탭바");
            // T72 ② — 배치가 끝난 뒤에 빛살을 건다(아이콘 rect 가 % 앵커라 그 전에는 0)
            Canvas.ForceUpdateCanvases();
            foreach (var l in _lightPlan) UiKit.LightBehind(l.host, l.icon, l.key);
            // T72 4항 — 빛살은 «보이는 칸만» 돈다(상점은 칸이 11개라 전부 돌리면 폰에서 낭비다)
            _scroll.onValueChanged.AddListener(_ => UpdateLightSpin());
            UpdateLightSpin();
        }

        /// <summary>스크롤 위치(1 = 맨 위 = 레퍼런스 10 · 0 = 맨 아래 = 레퍼런스 09). 비평 스크린샷(UiShotsTests)이 두 장을 찍을 때 쓴다.</summary>
        public void ScrollTo(float normalized)
        {
            if (_scroll == null) return;
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = Mathf.Clamp01(normalized);
            // 코드로 옮기면 값이 그대로일 때 onValueChanged 가 안 울린다 — 빛살 회전 상태는 여기서 직접 맞춘다(T72 4항)
            UpdateLightSpin();
        }

        /// <summary>
        /// T72 4항 «보이는 칸만» — 스크롤 창(<c>Scroll</c>) 과 세로로 겹치는 칸의 빛살만 돌리고 나머지는 멈춘다(<see cref="UiKit.SetLightSpinning"/>).
        /// 스크롤할 때마다 부르므로 <see cref="RectTransform.GetWorldCorners"/> 한 번씩만 쓴다(칸 11개).
        /// </summary>
        void UpdateLightSpin()
        {
            if (_scroll == null || _scroll.viewport == null) return;
            var view = _scroll.viewport; view.GetWorldCorners(_corners);
            float vBottom = _corners[0].y, vTop = _corners[1].y;
            foreach (var cell in _lightCells)
            {
                if (cell == null) continue;
                cell.GetWorldCorners(_corners);
                UiKit.SetLightSpinning(cell, _corners[1].y > vBottom && _corners[0].y < vTop);
            }
        }
        readonly Vector3[] _corners = new Vector3[4];

        // ───────────────────────── 배치 도우미 ─────────────────────────
        /// <summary>스크롤 Content 안 자리 — r 은 <b>프레임 %</b>(표값 그대로 · y 는 <see cref="ContentTop"/> 부터 아래로 이어진다). 가로는 Content 폭 % · 세로는 프레임 px.</summary>
        RectTransform Place(RectTransform rt, Layout.R r)
        {
            rt.anchorMin = new Vector2(r.X / 100f, 1f); rt.anchorMax = new Vector2((r.X + r.W) / 100f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0, -(r.Y - ContentTop + r.H) / 100f * UiKit.FrameH); rt.offsetMax = new Vector2(0, -(r.Y - ContentTop) / 100f * UiKit.FrameH);
            rt.localScale = Vector3.one;
            return rt;
        }
        /// <summary>상품 카드 1칸 = 표 ⑤ «상품 카드(1칸)»(폭 30 · 높이 18.5 · 3열 · 가로 간격 2).</summary>
        static Layout.R CardRect(float y, int col) => new Layout.R(Layout.ShopCard1.X + col * (Layout.ShopCardW + Layout.ShopCardGap), y, Layout.ShopCardW, Layout.ShopCard1.H);
        /// <summary>섹션 제목(가운데 흰 굵은 글자 + 양옆 선 = Title_LineDeco 조각) — 표 ⑤ «섹션 헤더»(높이 2.5) 자리에 조각 비례(448×102)로.</summary>
        RectTransform Header(float y, string text)
        {
            var t = UiKit.Spawn("ui.lineTitle", _content); var rt = (RectTransform)t.transform; rt.name = "Sec:" + text;
            Place(rt, new Layout.R(20, y - 0.75f, 60, Layout.ShopSec1.H + 1.5f));
            // 헤더 글자 = 표 높이에서 계산(≈50 · T63-shop) — 조각 안 Text (TMP) 상자(높이 71px)에 한 줄이 들어간다(선호 높이 ≈ 크기 × 0.98)
            var txt = UiKit.SetText(rt, "Text (TMP)", text, Palette.White, HeaderSize); if (txt != null) { txt.fontStyle = FontStyles.Bold; txt.enableAutoSizing = true; txt.fontSizeMin = TextSize.BestFitMin; txt.fontSizeMax = HeaderSize; }
            var line = UiKit.Find(rt, "LineDeco") as RectTransform; if (line != null) line.sizeDelta = new Vector2(UiKit.FrameW * 0.6f, line.sizeDelta.y);   // 선을 레퍼런스처럼 길게(조각은 그대로 · 폭만)
            // T100 ⓒ — 섹션을 나누는 선만 아주 옅게(제목 글자는 그대로). 조각 안에 선이 여럿일 수 있어 이름이 «LineDeco» 로 시작하는 그림 전부.
            foreach (var im in rt.GetComponentsInChildren<Image>(true))
                if (im.name.StartsWith("LineDeco")) { var c = im.color; c.a = SecLineAlpha; im.color = c; }
            return rt;
        }
        /// <summary>어두운 반투명 pill(TransperDark 조각) + 흰 글자 — 설명·천장 줄. 글자 = 본문 하한(40 · 2줄까지 접힘 · pill 높이는 카드 % 로 2줄이 들어가게).</summary>
        TMP_Text Pill(RectTransform card, Layout.R r, string text)
        {
            var p = UiKit.Spawn("ui.frameDark", card); var prt = (RectTransform)p.transform; prt.name = "Pill"; UiKit.Pct(prt, r);
            return UiKit.Label(prt, 3, 0, 94, 100, text, TextSize.Body, Palette.White);
        }
        /// <summary>
        /// 가격 버튼(T63-shop · 결정 142) — Jua 폰트에 💎 글리프가 없어 «1회 💎400» 의 💎 가 <b>빈칸</b>으로 그려졌다(screens run 101 의 10_shop_2.png · «1회  400» = 화폐 표시 없음) →
        /// 다이아는 상단 바와 같은 <b>아이콘 그림</b>(hud.gem) 으로. 큰 카드(<paramref name="twoLine"/>) = 위 «1회» / 아래 [💎 400](레퍼런스 10 의 «Open / 💎400» 두 줄) · 작은 카드 = 한 줄 [1회][💎][80].
        /// 프리팹 버튼 자체의 글자는 비워 숨기고(조각 «그대로» — 숨김) 줄은 <see cref="PriceRow"/>(HorizontalLayoutGroup · 자식 = 선호 크기) 로 — 글자 rect = 선호 크기라 잘림 0 · 글자 종류 = 버튼(44).
        /// </summary>
        RectTransform PriceButton(RectTransform card, string name, string label, double cost, Action onClick, Layout.R rect, bool twoLine)
        {
            var b = UiKit.Button(card, "ui.btnOrange", "", onClick, rect); b.name = name;
            var own = UiKit.ButtonText(b); if (own != null) own.gameObject.SetActive(false);
            if (twoLine)
            {
                var top = UiKit.Label(b, 0, 6, 100, 44, label, TextSize.Button, Palette.White, TextAnchor.MiddleCenter, false, true, TextKind.Button); top.name = "Label";
                PriceRow(b, new Layout.R(0, 50, 100, 44), UiKit.FmtQty(cost));
            }
            else PriceRow(b, new Layout.R(0, 0, 100, 100), UiKit.FmtQty(cost), label);
            return b;
        }
        /// <summary>[라벨][💎 아이콘][가격] 한 줄 — HorizontalLayoutGroup 이 자식을 선호 크기로 가운데 정렬(글자는 Overflow · rect 가 선호 폭과 같아 반올림으로 줄이 접히지 않게).
        /// <para>T255 — 아이콘·글자 크기를 인자로 받는다(키 버튼이 같은 줄 꼴을 쓴다 · 기본값은 종전 그대로라 다이아 버튼은 한 픽셀도 안 바뀐다).</para></summary>
        RectTransform PriceRow(RectTransform parent, Layout.R r, string cost, string before = null, string iconKey = "hud.gem", string iconName = "Gem", int costSize = TextSize.Button, TextKind costKind = TextKind.Button, int iconSize = PriceIconSize)
        {
            var row = UiKit.Rect(parent, "Price"); UiKit.Pct(row, r);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = PriceGap; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false; hl.childControlWidth = true; hl.childControlHeight = true;
            if (!string.IsNullOrEmpty(before)) { var t = UiKit.Text(row, before, TextSize.Button, Palette.White, TextAnchor.MiddleCenter, false, true, TextKind.Button); t.name = "Label"; t.textWrappingMode = TextWrappingModes.NoWrap; }
            var ic = UiKit.Icon(row, iconName, iconKey); ic.preserveAspect = true;
            var le = ic.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = iconSize; le.preferredHeight = iconSize;
            var c = UiKit.Text(row, cost, costSize, Palette.White, TextAnchor.MiddleCenter, false, true, costKind); c.name = "Cost"; c.textWrappingMode = TextWrappingModes.NoWrap;
            return row;
        }

        /// <summary>
        /// 상자 카드의 <b>«키로 열기»</b> 버튼(T255 3항 · 주인 2026-09-09 «파란색 키로 1회 뽑기 가능 …») — 초록 버튼에 [열쇠 아이콘][<b>가진 개수</b>].
        /// <para>
        /// <b>왜 «값» 이 아니라 «가진 개수» 를 찍나</b> — 값은 언제나 1 이라(«키 1개 = 1회») 「1」 은 아무것도 안 알려 준다.
        /// 반면 «몇 개 있나» 는 다른 데서 볼 수 없다: 지시서 5항이 <b>탑바에는 넣지 말라</b>고 못 박고 «그 버튼 옆에 개수를 보여 준다» 라고 적은 자리가 여기다.
        /// </para>
        /// <para>
        /// <b>T275 — 글자가 <c>보유/이번에 쓸 개수</c> 로 넓어졌다</b>(주인 2026-09-08 «있는 열쇠 다 써서 · 캡 10 · 17개면 17/10»).
        /// 큰 카드 윗줄의 «1회» 도 <b>«쓸 개수»회</b> 로 같이 움직인다 — 누르면 10 회가 나가는데 «1회» 라고 적혀 있으면 그것이 거짓말이다.
        /// 0개면 누를 수 없으므로 윗줄은 T255 때의 «1회» 그대로 쉰다.
        /// </para>
        /// 0개면 비활성이고(개수는 그대로 「0/0」 이 보인다 — 어디서 구하는지는 아레나 상인·출석이 안다), 키가 없는 상자면 버튼 자체를 안 만든다.
        /// </summary>
        RectTransform KeyButton(RectTransform card, GachaBox box, Layout.R rect, bool twoLine, BoxWidgets w)
        {
            string item = GachaKeys.KeyOf(box.Key); if (item == null) return null;
            var b = UiKit.Button(card, "ui.btnGreen", "", () => PullWithKey(box.Key), rect); b.name = "Key";
            var own = UiKit.ButtonText(b); if (own != null) own.gameObject.SetActive(false);
            var row = twoLine
                ? PriceRow(b, new Layout.R(0, 50, 100, 44), "0/0", null, GachaKeys.Icon(item), "KeyIcon")
                // T275 ⓑ — 작은 카드는 글자가 Aux(36) 이므로 아이콘도 Aux 로 맞춘다(큰 카드는 Button 44 로 그대로):
                // 글자·아이콘 크기가 한 줄 안에서 어긋날 까닭이 없고, «17/10» 다섯 자가 들어갈 8px 이 여기서 난다(결정 761).
                : PriceRow(b, new Layout.R(0, 0, 100, 100), "0/0", null, GachaKeys.Icon(item), "KeyIcon", TextSize.Aux, TextKind.Aux, TextSize.Aux);
            if (twoLine) { var top = UiKit.Label(b, 0, 6, 100, 44, "1회", TextSize.Button, Palette.White, TextAnchor.MiddleCenter, false, true, TextKind.Button); top.name = "Label"; w.KeyLabel = top; }
            w.Key = b.GetComponent<Button>();
            w.KeyCount = row.Find("Cost") != null ? row.Find("Cost").GetComponent<TMP_Text>() : null;
            return b;
        }
        /// <summary>(i) 버튼 = Button_Info 조각 → 확률·천장 팝업.</summary>
        void InfoButton(RectTransform card, Layout.R r, GachaBox box)
        {
            var i = UiKit.Spawn("ui.btnInfo", card); var irt = (RectTransform)i.transform; irt.name = "Info"; UiKit.Pct(irt, r);
            UiKit.Clickable(irt, () => ShowInfo(box));
        }
        /// <summary>
        /// T100 ⓐ — 상자 카드 조각(CardFrame_04) 안의 제목 바탕 <c>TitleBg</c>·<c>TitleBgBorder</c> 를 끈다(주인 2026-09-07 «필요 없으니까 없애라»).
        /// <b>이 인스턴스만</b> 끄므로 조각 원본과, 같은 조각의 <c>TitleBg</c> 를 제목 자리로 쓰는 다른 팝업(<c>Overlay.cs</c>)은 그대로다.
        /// T69-shop 이 카드 «바깥»에 덧댄 Ink 링은 별개라 남는다 — 없애는 것은 제목 바탕뿐이다.
        /// <para>주인이 부른 «TitleBgBorder» 는 조각(<c>CardFrame_04_BasePrefab</c>) 안에서 실제 이름이 <c>TitleBorder</c> 다(자식 = Bg · Border · InnerBorder · TitleBg · TitleBorder) — 결정 242.</para>
        /// </summary>
        /// <summary>주인이 «TitleBgBorder» 라 부른 조각 자식의 실제 이름(<c>CardFrame_04_BasePrefab</c> 실측 · 결정 242).</summary>
        const string TitleBorderName = "TitleBorder";
        static void HideCardTitleBg(Transform frame) { UiKit.Hide(frame, "TitleBg", TitleBorderName); }
        /// <summary>카드 색 = 그 상자에서 나올 수 있는 최고 등급의 등급색(CardFrame_04 변형 · 희귀 상자 blue · 전설 yellow · 신화 plum).</summary>
        static string BoxColor(GachaBox box) { int top = 0; for (int i = 0; i < box.Rate.Length; i++) if (box.Rate[i] > 0) top = i; return Palette.RarName(top); }
        /// <summary>등급 확률 한 줄(index.html gachaRateText 순서 · 높은 등급부터 · 0% 등급은 안 적는다).</summary>
        string RatesText(GachaBox box)
        {
            var D = App.Data; var o = new List<string>();
            for (int i = box.Rate.Length - 1; i >= 0; i--) if (box.Rate[i] > 0) o.Add($"<color=#{ColorUtility.ToHtmlStringRGB(Palette.ByName(Palette.RarName(i)))}>{GearUi.RarName(D, i)}</color>\u00A0{box.Rate[i]:0.#}%");   // 등급 이름과 값도 안 끊기는 빈칸으로 묶는다 — 끊을 수 있는 자리를 «/ 뒤» 하나로 남긴다
            // T222 ⓑ — 구분자 앞은 «안 끊기는 빈칸»(U+00A0) 이다. `TextGlyphs` 가 «·» 를 «/» 로 바꾸므로(Jua 에 가운뎃점 글리프가 없다)
            // 화면 글자는 «30% / 일반» 이 되고, 줄은 «/» **뒤** 에서만 끊긴다 — TMP 로 갈아탄 뒤 «/» 가 줄 첫 글자로 내려오던 것이 이 자리다.
            // ⚠ TMP 의 금칙 문자 목록(`LineBreaking Following Characters.txt`)으로는 안 잡힌다 — 그 목록은 «공백에서 끊는 자리» 에 안 걸린다(결정 610).
            return string.Join("\u00A0· ", o);   // \u00A0 = 안 끊기는 빈칸(눈에 안 보이므로 이스케이프로 적는다)
        }
        /// <summary>천장 줄들 — 신화 확정 · 전설 확정(있는 것만) 뒤에 «누적 N회» 로 채운다(pill 개수만큼).</summary>
        static List<string> PityLines(GachaBox box, GachaState st, int count)
        {
            var o = new List<string>();
            if (box.PityMyth > 0) o.Add($"신화 확정까지 <color=#FFCC00>{Math.Max(0, box.PityMyth - st.P50)}</color>회");
            if (box.PityLegend > 0) o.Add($"전설 확정까지 <color=#FFCC00>{Math.Max(0, box.PityLegend - st.P10)}</color>회");
            while (o.Count < count) o.Add($"누적 <color=#FFCC00>{st.Pulls}</color>회 열었습니다");
            return o.GetRange(0, count);
        }
        GachaState State(string key) { var S = App.Save; if (!S.GachaBoxes.TryGetValue(key, out var st)) { st = new GachaState(); S.GachaBoxes[key] = st; } return st; }

        // ───────────────────────── 상자 카드 ─────────────────────────
        /// <summary>상자 카드 3장의 그라데이션 이름(레퍼런스 10 실측 · <see cref="GradientPalette"/> · 카탈로그 <c>col.grad.cardChest*</c>) — 문자열을 두 곳에 안 박는다(§1).</summary>
        public const string ChestGradBig = "cardChestLegend", ChestGradLeft = "cardChestRare", ChestGradRight = "cardChestEpic";
        /// <summary>대형 카드가 되는 상자 = 가장 비싼 것(<c>gacha.json</c> 순서와 무관) — <see cref="Build"/> 와 테스트가 같은 표를 본다.</summary>
        public static GachaBox BigBox(GameData d)
        {
            GachaBox big = null;
            if (d != null && d.Gacha != null && d.Gacha.Boxes != null) foreach (var b in d.Gacha.Boxes) if (big == null || b.Cost > big.Cost) big = b;
            return big;
        }
        /// <summary>상자 키 → 그 카드의 그라데이션 이름(대형 = 가장 비싼 상자 · 나머지 둘은 <c>gacha.json</c> 순서로 왼쪽·오른쪽) — 없는 키면 null.</summary>
        public static string ChestGradName(GameData d, string boxKey)
        {
            var big = BigBox(d); if (big == null) return null;
            if (big.Key == boxKey) return ChestGradBig;
            int i = 0;
            foreach (var b in d.Gacha.Boxes) { if (b == big) continue; if (b.Key == boxKey) return i == 0 ? ChestGradLeft : ChestGradRight; i++; }
            return null;
        }
        /// <summary>최상위 상자 큰 카드(10_shop_2.jpg 위) — 그림 왼쪽 · 오른쪽에 이름 + (i) · 확률 한 줄 · 천장 pill 2 · 아래 «1회 💎» · «10회 💎» 주황 2개(표 ⑤ «상자 버튼 2개» 자리).</summary>
        /// <summary>
        /// 카드 조각 «안» 그라데이션(T100 ⓓ · 주인 2026-09-07 08:5X «상자들 카드 부분에도 그라디안트 · 레퍼런스랑 같은 색감») —
        /// 조각의 바탕(<paramref name="bgName"/>) «바로 위» 형제에 <see cref="UiKit.GradientCard"/> 두 장을 깐다(글자·아이콘·버튼·테두리는 그 위 · T72 ③ 층 순서).
        /// 색은 <see cref="GradientPalette"/> 의 <b>레퍼런스 실측 두 색</b>(T116) — 코드에 색을 박지 않는다(§1).
        /// <para>
        /// <paramref name="alpha"/> — 상품 카드(09)는 조각 바탕이 어두운 제 색이라 덧칠(<see cref="UiKit.GradientCardAlpha"/>)로 레퍼런스와 맞는데,
        /// <b>상자 카드(10)는 조각 바탕이 회색</b>이라 덧칠이면 색이 죽는다(회차 1 실측: 레퍼런스 «Rare» #0182C3 → 우리 #8997A2) →
        /// 몸통을 <see cref="UiKit.GradientCardSolidAlpha"/> 로 덮는다(T100 ⓓ 회차 2 · 결정 338).
        /// </para>
        /// <para>
        /// T147 — 바탕은 <b>직계 자식이 아닐 수 있다</b>. 다이아·골드 카드(<c>ui.shopItem</c>)의 계층은 «카드 → <c>ShopFrame_01</c> → <c>Bg(Mask)</c>» 라
        /// 바탕이 <b>손자</b>다(프리팹 실측: <c>Bg(Mask)</c> 는 중첩 조각 <c>ShopFrame_01</c> 안에 있고 Image + Mask 를 달고 있다).
        /// 그래서 ⓐ 바탕을 <b>깊이 찾고</b> ⓑ 그라데이션을 <b>바탕의 부모</b>에 건다(바탕이 직계면 부모 = <paramref name="piece"/> 라 종전과 같은 자리 = 10 상자 카드 불변).
        /// 못 찾으면 <b>조용히 형제 0 으로 떨어지지 않는다</b> — 그것이 이 결함이었다(형제 0 = 불투명 프레임 «뒤» 라 그라데이션이 안 보인다 · 결정 374).
        /// </para>
        /// </summary>
        static void CardGradient(Transform piece, string paletteName, string bgName, float alpha = UiKit.GradientCardAlpha)
        {
            if (piece == null) return;
            var bg = FindDeep(piece, bgName);
            if (bg == null)
            {
                Debug.LogWarning("[T147] 카드 바탕 «" + bgName + "» 을 " + piece.name + " 아래에서 못 찾았다 — 그라데이션을 안 깐다(형제 0 = 프레임 뒤라 안 보인다)");
                return;
            }
            // 몸통을 채우는 자리는 바탕까지 그 계열색으로 물들인다 — 위·아래 두 조각은 서로 반대 방향 램프라
            // «가운데» 에서 둘 다 반쯤만 덮는다(실측: 회차 2 의 희귀 카드 채도 0.60 · 레퍼런스 0.98).
            // 비치는 것이 조각의 «회색» 이면 색이 죽고, 두 색의 «가운데 색» 이면 그대로 레퍼런스의 가운데다(결정 344).
            if (alpha >= UiKit.GradientCardSolidAlpha && GradientPalette.Has(paletteName))
            {
                var img = bg.GetComponent<Image>();
                if (img != null) { var p = GradientPalette.Of(paletteName); img.color = Color.Lerp(p.Top, p.Bottom, 0.5f); }
            }
            UiKit.GradientCard((RectTransform)bg.parent, paletteName, null, UiKit.PopupPatternInset, bg.GetSiblingIndex() + 1, alpha);
        }
        /// <summary>
        /// 이름이 <paramref name="name"/> 인 <b>보이는</b> 자손을 너비 우선으로 찾는다(T147).
        /// 얕은 것 우선 = 바탕이 여러 겹일 때 «카드에 가장 가까운» 것. <b>꺼진 가지는 건너뛴다</b> —
        /// 상품 카드는 <c>CardGradient</c> 바로 앞에서 <c>ItemFrameArea</c> 를 끄는데(<see cref="UiKit.Hide"/>),
        /// 꺼진 조각 안의 같은 이름 바탕을 집으면 그라데이션이 <b>안 보이는 가지</b>에 깔린다(이 작업이 고친 결함과 같은 꼴).
        /// </summary>
        static Transform FindDeep(Transform root, string name)
        {
            var q = new Queue<Transform>(); q.Enqueue(root);
            while (q.Count > 0)
            {
                var t = q.Dequeue();
                for (int i = 0; i < t.childCount; i++)
                {
                    var c = t.GetChild(i);
                    if (!c.gameObject.activeSelf) continue;
                    if (c.name == name) return c;
                    q.Enqueue(c);
                }
            }
            return null;
        }

        void BuildBigCard(RectTransform card, GachaBox box, List<RectTransform> btnsOut)
        {
            var D = App.Data; var w = new BoxWidgets(); string key = box.Key;
            var frame = UiKit.Spawn(Palette.FrameKey("ui.cardFrame", BoxColor(box)), card); UiKit.Stretch((RectTransform)frame.transform);
            // T100 ⓐ — 제목 바탕 끄기(§1 «문장 끝 // 주석 금지» 대로 주석은 윗줄에)
            HideCardTitleBg(frame.transform);
            // T100 ⓓ — 레퍼런스 10 의 대형 «Legendary Chest» 카드 색(위 연보라 → 아래 자홍 · 몸통을 꽉 채운다 · 회차 2)
            CardGradient(frame.transform, ChestGradBig, "Bg", UiKit.GradientCardSolidAlpha);
            // 상자 이름 = 제목(60 · T63-shop · 레퍼런스 «Legendary Chest» 는 카드에서 가장 큰 글자) — 칸 13% × 배너 26% = 79px ≥ 선호 59
            var title = UiKit.SetText(frame.transform, "Text_Title", box.Name, Palette.Yellow, TextSize.Title, TextKind.Title);
            if (title != null) { UiKit.Pct(title.rectTransform, 42, 3, 49, 13); title.alignment = UiKit.TmpAlign(TextAnchor.MiddleRight); title.fontStyle = FontStyles.Bold; title.enableAutoSizing = true; title.fontSizeMin = TextSize.BestFitMin; title.fontSizeMax = TextSize.Title; }
            InfoButton(card, new Layout.R(91.5f, 4, 7, 12), box);
            var chest = UiKit.Icon(card, "Chest", "chest." + box.Key); UiKit.Pct(chest.rectTransform, 4, 8, 36, 56);
            // T72 ② 특별 상품(대형 상자) 그림 뒤 빛살 — 큰 조각(Effect_Light_01)
            _lightPlan.Add((card, chest.rectTransform, UiKit.LightKey)); _lightCells.Add(card);
            // 확률 줄 = 본문 40(4 등급이면 2줄 · 칸 16% × 배너 26% = 97px ≥ 2줄 88)
            UiKit.Label(card, 42, 20, 54, 16, RatesText(box), TextSize.Body, Palette.White);
            w.Pills.Add(Pill(card, new Layout.R(42, 45, 55, 10), ""));
            w.Pills.Add(Pill(card, new Layout.R(42, 58.5f, 55, 10), ""));
            // T255 3항 — 세 갈래가 «같은 줄» 에 선다(높이·y 는 종전 그대로 · 폭만 나눈다). 큰 카드는 1,015px 라 «💎4,000» 도 305px 칸에 넉넉히 든다.
            var one = PriceButton(card, "One", "1회", box.Cost, () => Pull(1, key), new Layout.R(2.5f, 74, 30, 21), true);
            var ten = PriceButton(card, "Ten", $"{D.Gacha.TenPullCount}회", box.Cost * D.Gacha.TenPullCount, () => Pull(D.Gacha.TenPullCount, key), new Layout.R(34, 74, 30, 21), true);
            w.One = one.GetComponent<Button>(); w.Ten = ten.GetComponent<Button>();
            btnsOut.Add(one); btnsOut.Add(ten);
            var bigKey = KeyButton(card, box, new Layout.R(65.5f, 74, 32, 21), true, w);
            if (bigKey != null) btnsOut.Add(bigKey);
            // T69-shop «검은 아웃라인» — CardFrame_04 조각의 제 외곽선은 프레임 3~4px 라 폰에서 1px 남짓(8px 규칙 미달) → 카드 위에 Ink 링 한 장(가운데 비움 · raycast 끔 · 표 % 불변)
            UiKit.Bordered(card);
            _box[key] = w;
        }
        /// <summary>나머지 상자 작은 카드(10_shop_2.jpg 가운데) — 이름 + (i) · 확률 pill · 그림 · 천장 pill · 아래 <b>광고(파랑 · 무료 보급 수령)</b> + <b>«💎가격»(1회)</b>.</summary>
        void BuildSmallCard(RectTransform card, GachaBox box, string gradName)
        {
            var D = App.Data; var w = new BoxWidgets(); string key = box.Key;
            var frame = UiKit.Spawn(Palette.FrameKey("ui.cardFrame", BoxColor(box)), card); UiKit.Stretch((RectTransform)frame.transform);
            // T100 ⓐ — 제목 바탕 끄기(§1 «문장 끝 // 주석 금지» 대로 주석은 윗줄에)
            HideCardTitleBg(frame.transform);
            // T100 ⓓ — 레퍼런스 10 의 작은 카드 색(왼쪽 «Rare» 파랑 → 하늘 · 오른쪽 «Epic» 남보라 → 자주 · 몸통을 꽉 채운다 · 회차 2)
            CardGradient(frame.transform, gradName, "Bg", UiKit.GradientCardSolidAlpha);
            // 상자 이름 = 제목(60 · T63-shop) — 칸 10% × 카드 29% = 68px ≥ 선호 59 · 확률 pill 은 그 아래(12.5~26.5% · 95px ≥ 2줄 88 — 회차 1 의 13% = 88px 은 딱 맞아 bestFit 이 39 로 눌렀다 · CI #110 표 «최소 크기(실제) 39»)
            var title = UiKit.SetText(frame.transform, "Text_Title", box.Name, Palette.White, TextSize.Title, TextKind.Title);
            if (title != null) { UiKit.Pct(title.rectTransform, 6, 2, 76, 10); title.alignment = UiKit.TmpAlign(TextAnchor.MiddleCenter); title.fontStyle = FontStyles.Bold; title.enableAutoSizing = true; title.fontSizeMin = TextSize.BestFitMin; title.fontSizeMax = TextSize.Title; }
            InfoButton(card, new Layout.R(84, 2, 12, 9), box);
            Pill(card, new Layout.R(6, 12.5f, 88, 14), RatesText(box));
            var chest = UiKit.Icon(card, "Chest", "chest." + box.Key); UiKit.Pct(chest.rectTransform, 22, 28, 56, 37);
            // T72 ② 상자 카드 그림 뒤 빛살(작은 칸이라 Effect_Light_02)
            _lightPlan.Add((card, chest.rectTransform, UiKit.LightKeySmall)); _lightCells.Add(card);
            w.Pills.Add(Pill(card, new Layout.R(6, 67, 88, 14), ""));
            // 광고 버튼(파랑 · 클래퍼) = 일일 무료 보급(gacha.json dailyGem · 하루 1회) — 받을 수 있으면 빨간 점
            // T255 3항 — 작은 카드는 폭이 324px 뿐이라 셋을 같은 줄에 세울 때 «글자를 가진 칸» 을 먼저 지켰다:
            // 광고는 원래 아이콘 하나뿐이라 좁혀도 잘릴 글자가 없고(20% = 65px · 아이콘 44), 키는 [아이콘][개수] 라 짧다.
            // 다이아 버튼(«1회 💎80»)만 종전 폭에 가깝게 남긴다 — 여기서 한 자라도 줄면 그 줄이 먼저 줄어든다(T63 하한).
            // T275 ⓑ — 광고 칸을 18% → 13% 로 좁혀 그 5%p 를 키 버튼에 준다(결정 761 · `screens:t275.json` 실측 «넘침 +16.9px»).
            // 좁혀도 잃는 것이 없는 칸이 여기뿐이다: 광고는 아이콘 하나뿐이라 잘릴 글자가 없고, 칸 안 비율을 48% → 62% 로 올려
            // 그려지는 아이콘 크기는 전과 거의 같게(42 → 40px) 뒀다. 다이아 버튼(«1회 💎80»)은 T255 경고대로 한 자도 안 건드린다.
            var ad = UiKit.Button(card, "ui.btnBlue", "", OnFree, new Layout.R(6, 83, 13, 14)); ad.name = "Ad";
            var adIc = UiKit.Icon(ad, "Icon", "ui.ad"); UiKit.Pct(adIc.rectTransform, 19, 12, 62, 76);
            var dot = UiKit.AlertDot(ad, "FreeDot", new Vector2(1, 1), new Vector2(-6, -2), 44);   // T136
            _freeBtns.Add(ad.GetComponent<Button>()); _freeDots.Add(dot);
            KeyButton(card, box, new Layout.R(21, 83, 31, 14), false, w);   // T275 ⓑ — 26% → 31%(127.8 → 152.4px · 필요 136.7px · 결정 761)
            var one = PriceButton(card, "One", "1회", box.Cost, () => Pull(1, key), new Layout.R(54, 83, 40, 14), false);
            w.One = one.GetComponent<Button>();
            // T69-shop — 큰 카드와 같은 Ink 링(광고·가격 버튼 줄은 카드 «안» 이라 따로 상자를 두지 않는다 · 레퍼런스 10 도 그렇다 · BorderAudit.Exempt)
            UiKit.Bordered(card);
            _box[key] = w;
        }

        // ───────────────────────── 상품 카드 (ListItem_ShopItem 부품 · 수량 → 그림 → 이름 → 가격 띠) ─────────────────────────
        /// <summary>다이아/골드 카드 1칸 — 09_shop_1.jpg 카드 안 비례: 수량(위 5~19%) · 그림(20~64%) · 이름(66~77%) · 가격 띠(80~97%). 카드 전체와 가격 버튼이 같은 일을 한다. priceIconKey 가 null 이면 가격 아이콘을 끈다(₩).</summary>
        List<Button> BuildPack(RectTransform slot, string qty, string iconKey, string name, string priceIconKey, string price, Color tint, Action onClick, string gradName = null)
        {
            var cell = UiKit.Spawn("ui.shopItem", slot); var crt = (RectTransform)cell.transform; UiKit.Stretch(crt);
            foreach (var im in cell.GetComponentsInChildren<Image>(true)) { if (im.name == "Bg(Mask)") im.color = tint; else if (im.name == "Botton") im.color = Palette.Cream; }
            UiKit.Hide(crt, "ItemFrameArea", "Text_ItemNum");
            // T100 ⓓ — 상품 카드도 같은 규칙(한 화면에서 상자만 화려하면 어색하다 · 지시서 ⓓⓒ 기본값 · 결정 313)
            CardGradient(crt, gradName, "Bg(Mask)");
            // 수량 = 띠 높이에서 계산(≈51 · T63-shop) — 띠 14% × 카드 18.5% = 60px ≥ 선호 50
            var q = UiKit.SetText(crt, "Text_Title", qty, Palette.White, QtySize);
            if (q != null) { UiKit.Pct(q.rectTransform, 5, 5, 90, QtyBandH); q.fontStyle = FontStyles.Bold; q.enableAutoSizing = true; q.fontSizeMin = TextSize.BestFitMin; q.fontSizeMax = QtySize; }
            var im2 = UiKit.SetSprite(crt, "Icon", iconKey, Palette.White);
            if (im2 != null)
            {
                im2.preserveAspect = true; UiKit.Pct(im2.rectTransform, 14, 20, 72, 44);
                // T72 ② 상점 상품 아이콘 뒤 빛살 — 아이콘은 조각(ListItem_ShopItem)의 바로 아래 자식이라 «아이콘 앞 형제» 가 곧 «그림 뒤»
                _lightPlan.Add(((RectTransform)im2.rectTransform.parent, im2.rectTransform, UiKit.LightKeySmall)); _lightCells.Add(crt);
            }
            var nm = UiKit.SetText(crt, "Text_Limit", name, Palette.White, TextSize.Body); if (nm != null) { UiKit.Pct(nm.rectTransform, 4, 66, 92, 11); nm.enableAutoSizing = true; nm.fontSizeMin = TextSize.BestFitMin; nm.fontSizeMax = TextSize.Body; }
            var btns = new List<Button>();
            var btn = UiKit.Find(crt, "Button_Price");
            if (btn != null)
            {
                UiKit.Pct((RectTransform)btn, 6, 80, 88, 17);
                var pi = UiKit.Find(btn, "GroupArea/Group/Icon"); if (pi != null) { pi.gameObject.SetActive(priceIconKey != null); if (priceIconKey != null) UiKit.SetSprite(btn, "GroupArea/Group/Icon", priceIconKey, Palette.White); }
                // 가격 띠 글자 = 버튼 하한(44 · T63-shop) — Group(HorizontalLayoutGroup) 이 글자 rect 를 선호 폭으로 잡으므로 Overflow 유지(T40 · Wrap 이면 글자마다 줄이 접힌다)
                var pt = UiKit.SetText(btn, "GroupArea/Group/Text (TMP)", price, null, TextSize.Button, TextKind.Button); if (pt != null) { pt.enableAutoSizing = true; pt.fontSizeMin = TextSize.BestFitMin; pt.fontSizeMax = TextSize.Button; pt.textWrappingMode = TextWrappingModes.NoWrap; }
                var inner = UiKit.Find(btn, "Button_02_Yellow"); if (inner != null) { var it = inner.Find("Text (TMP)"); if (it != null) it.gameObject.SetActive(false); }   // 버튼 프리팹 자체의 «Button» 글자 — 값은 GroupArea 의 글자가 맡는다
                btns.Add(UiKit.Clickable(btn, onClick));
            }
            btns.Add(UiKit.Clickable(crt, onClick, false));
            // T69-shop — 상품 카드(ListItem_ShopItem 조각)의 제 외곽선은 프레임 2px 남짓이라 폰에서 안 보인다 → 칸 위에 Ink 링 8px(레퍼런스 09 의 카드 검은 외곽선 · 표 «상품 카드» % 불변)
            // 7항 «아이템류 칸 = ItemFrame» 은 여기엔 안 쓴다 — 레퍼런스 09 의 상품 칸은 정사각 아이템 프레임이 아니라 세로 카드(수량·그림·이름·가격 띠)다(결정 196)
            UiKit.Bordered(slot);
            return btns;
        }

        // ───────────────────────── 갱신 ─────────────────────────
        public override void Refresh()
        {
            var D = App.Data; var S = App.Save;
            _top?.Refresh();
            bool canFree = CanFree(S);
            foreach (var b in _freeBtns) UiKit.SetInteractable(b, canFree);
            foreach (var d in _freeDots) if (d != null) d.SetActive(canFree);
            foreach (var box in D.Gacha.Boxes)
            {
                if (!_box.TryGetValue(box.Key, out var w)) continue;
                var st = State(box.Key); var lines = PityLines(box, st, w.Pills.Count);
                for (int i = 0; i < w.Pills.Count; i++) if (w.Pills[i] != null) w.Pills[i].text = lines[i];
                UiKit.SetInteractable(w.One, S.Gem >= box.Cost); UiKit.SetInteractable(w.Ten, S.Gem >= box.Cost * D.Gacha.TenPullCount);
                // T255·T275 — 키 버튼은 «보유/이번에 쓸 개수» 를 찍고 0개면 비활성(개수는 그대로 보인다 · 지시서 5항)
                // 쓸 개수 = min(보유, 캡) 이고 캡은 표 값이다(코드에 10 을 안 박는다 · §1). 뽑고 나면 Pull 이 이 Refresh 를 불러 17 → 7/7 로 바로 갈린다.
                if (w.Key != null)
                {
                    var item = GachaKeys.KeyOf(box.Key);
                    double have = GachaKeys.Count(S, item);
                    int use = GachaKeys.UseCount(S, box.Key, D.Gacha.TenPullCount);
                    if (w.KeyCount != null) w.KeyCount.text = UiKit.FmtQty(have) + "/" + use;
                    if (w.KeyLabel != null) w.KeyLabel.text = (use > 0 ? use : 1) + "회";
                    UiKit.SetInteractable(w.Key, use > 0);
                }
            }
            foreach (var g in _gated) UiKit.SetInteractable(g.btn, g.can());
            UpdateTimer(); UpdateLightSpin();
        }
        public override void Tick(float dt) { _timerT += dt; if (_timerT >= 1f) { _timerT = 0f; UpdateTimer(); } }
        /// <summary>«무료 보급까지 hh:mm:ss»(자정 리셋) · 받을 수 있으면 «지금 수령 가능».</summary>
        void UpdateTimer()
        {
            if (_freeTxt == null) return;
            // 💎 글리프 없음(결정 142) → «다이아» 글자로 · 40 한 줄이 줄(934px)에 들어가게 문구를 줄임
            // T75 ⓒ — 이 두 줄은 Text.text 에 «직접» 넣어서 UiKit 입구의 TextGlyphs.Safe 를 안 거친다(«—» 는 Jua 에 글리프가 없어 폭 0 으로 사라졌다 · «[GlyphGate]» 표의 09·10 두 줄)
            if (CanFree(App.Save)) { _freeTxt.text = TextGlyphs.Safe($"무료 보급 다이아 {UiKit.FmtQty(App.Data.Gacha.DailyGem)} — 지금 수령 가능"); return; }
            var left = DateTime.Today.AddDays(1) - DateTime.Now; if (left.Ticks < 0) left = TimeSpan.Zero;
            _freeTxt.text = TextGlyphs.Safe($"무료 보급까지 {(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}");
        }

        void OnFree()
        {
            var S = App.Save; if (!CanFree(S)) { App.Toast("오늘 무료 보급은 받았습니다 — 내일 다시"); return; }
            S.Gem += App.Data.Gacha.DailyGem; S.FreeDay = Today(); App.Persist(); Refresh(); App.Toast($"다이아 {UiKit.FmtQty(App.Data.Gacha.DailyGem)} 수령!");
        }

        // ───────────────────────── 정보 팝업 (확률 · 천장) ─────────────────────────
        void ShowInfo(GachaBox box)
        {
            var D = App.Data; var st = State(box.Key);
            var b = App.Overlay.OpenBox("ui.popup", "ui.title.tangerine", box.Name, InfoBox, () => App.Overlay.Close());
            var chest = UiKit.Icon(b, "Chest", "chest." + box.Key); UiKit.Pct(chest.rectTransform, 32, 7, 36, 30);
            // 글자 = 본문 40(T63-shop) · 최대 9줄 × 선호 49px = 441 ≤ 칸 54% × 상자 42% = 530px — 빈 줄을 빼고 마지막 줄을 둘로 나눠(💎 글리프 없음 · 결정 142) 한 줄이 접히지 않게
            var lines = new List<string> { "<b>등급 확률</b>" };
            for (int i = box.Rate.Length - 1; i >= 0; i--) if (box.Rate[i] > 0) lines.Add($"<color=#{ColorUtility.ToHtmlStringRGB(Palette.ByName(Palette.RarName(i)))}>{GearUi.RarName(D, i)}</color>  {box.Rate[i]:0.#}%");
            if (box.PityMyth > 0) lines.Add($"신화 확정: {box.PityMyth}회마다 (남은 {Math.Max(0, box.PityMyth - st.P50)}회)");
            if (box.PityLegend > 0) lines.Add($"전설 확정: {box.PityLegend}회마다 (남은 {Math.Max(0, box.PityLegend - st.P10)}회)");
            if (box.PityMyth == 0 && box.PityLegend == 0) lines.Add("천장 없음");
            lines.Add($"누적 {st.Pulls}회 열었습니다");
            lines.Add($"1회 다이아 {UiKit.FmtQty(box.Cost)} · {D.Gacha.TenPullCount}회 다이아 {UiKit.FmtQty(box.Cost * D.Gacha.TenPullCount)}");
            UiKit.Label(b, 8, 40, 84, 54, string.Join("\n", lines), TextSize.Body, Palette.InkSoft, TextAnchor.UpperCenter, true, false);
        }

        // ───────────────────────── 뽑기 → 결과 팝업 (공통 팝업 문법 · 명판 · 열린 상자 · 격자 = GearUi.Cell · 탭하여 닫기) ─────────────────────────
        /// <summary>
        /// 키로 그 상자를 <b>가진 만큼(캡까지) 한 번에</b> 연다(T255 3항 → T275 · 주인 2026-09-08 «있는 열쇠 다 써서 열쇠 개수만큼 · 캡이 10»).
        /// 키는 «비용 수단» 만 바꾸므로(<see cref="GachaKeys"/>) 확률·천장·결과가 다이아로 연 것과 한 톨도 다르지 않다.
        /// <para>
        /// <b>한 판으로 n 회</b>다 — <c>Pull(n, …)</c> 한 번(1회씩 n 번 부르면 결과 창이 n 번 뜬다 · T275 4항).
        /// 다이아 N회 뽑기가 쓰는 그 길 그대로라 결과 창도 새 꼴이 아니다.
        /// </para>
        /// <b>캡은 표 값</b>(<c>D.Gacha.TenPullCount</c>) — «10회 버튼» 이 이미 그 값으로 글자와 값을 만든다. 코드에 10 을 안 박는다(§1).
        /// </summary>
        void PullWithKey(string boxKey)
        {
            var item = GachaKeys.KeyOf(boxKey);
            if (item == null) return;
            int n = GachaKeys.UseCount(App.Save, boxKey, App.Data.Gacha.TenPullCount);
            if (n <= 0) { App.Toast(GachaKeys.Name(item) + "가 없습니다"); return; }
            Pull(n, boxKey, true);
        }

        void Pull(int n, string boxKey, bool withKey = false)
        {
            var D = App.Data; var S = App.Save;
            GachaBox box = null; foreach (var b in D.Gacha.Boxes) if (b.Key == boxKey) box = b; if (box == null) return;
            var st = State(boxKey);
            if (withKey) { if (!GachaKeys.Open(S, boxKey, n)) return; }
            else
            {
                double cost = box.Cost * n; if (S.Gem < cost) { App.Toast("다이아가 부족합니다"); return; }
                S.Gem -= cost;
            }
            var rng = new Mulberry32((uint)Environment.TickCount ^ 0x5bd1e995u);
            var got = new List<GearItem>();
            for (int i = 0; i < n; i++)
            {
                foreach (var raw in GearSystem.GachaPull(D, st, box, rng)) { var g = S.NewGear(raw.Part, raw.Type, raw.Rar, raw.Plus); g.IsNew = true; S.Inv.Add(g); got.Add(g); }
                S.Pulls++;
            }
            Quests.Bump(App, Quests.ChestOpen, n);   // T257 — «상자 2번 오픈»(일일)·«30회»(주간) · n 연차면 n 번이다
            App.Persist(); Refresh();
            // 소리는 «착지하는 순간» 에 난다 — ChestResult 의 연출 시퀀스가 낸다(T180 · 결정 420). 여기서 미리 내면 상자가 아직 공중이다.
            var best = got[0]; foreach (var g in got) if (GearSystem.GearScore(g) > GearSystem.GearScore(best)) best = g;
            ChestResult(box, n, got, best);
        }

        /// <summary>결과 창 조각(<c>Shop_Chest_Open</c>)의 «상자» 묶음 자리 — 프리팹 실측(가운데에서 y −427.8 · 565×493). 격자는 그 «위» 에 놓는다(T95).</summary>
        public const float ChestGroupY = -427.84f;
        /// <summary>조각이 «얻은 것은 여기» 라고 준 칸(T157) — 자리·크기를 여기서 읽는다(코드에 수를 안 박는다).</summary>
        public const string ChestSlotName = "ItemFrame_01";
        /// <summary>그 칸이 없는 조각(옛 빌드·조각 교체)일 때만 쓰는 예전 자리 — 평소에는 안 쓴다.</summary>
        public const float ChestGridFallbackTopPct = 24f;
        /// <summary>
        /// 상자가 «작았다 → 커졌다 → 제 크기» 로 서는 시작 배율(T158 ⓐ · 주인 2026-09-07 08:0X «상자가 작았었는데 커졌다가 원래 사이즈로 되는 애니메이션 돼야 함»).
        /// 낙하(<see cref="ChestFallSec"/>) 와 <b>같은 시간</b>에 <c>Ease.OutBack</c> 으로 1 까지 자란다 — OutBack 이 «커졌다가 제 크기» 오버슛을 한 번에 준다.
        /// 착지 «쿵» 펀치는 그 뒤라 둘이 겹치지 않는다(T180 순서 불변).
        /// </summary>
        public const float ChestScaleFrom = 0.6f;
        /// <summary>
        /// 마지막으로 연 결과 창이 상자에 <b>실제로 넣은</b> 시작 배율 — 0 이면 «연출이 안 걸렸다»(T158 ⓐ 회차 3 · 결정 329 와 같은 방법).
        /// <para>
        /// 배율 연출은 0.24초에 지나가는 것이라 «지금 작나» 로는 못 잰다(회차 1 이 그러다 CI 를 빨갛게 했다).
        /// «도는 중인가» 로도 못 잰다 — 이 트윈은 <see cref="DG.Tweening.Sequence"/> 안에 <b>끼워져</b> 있어서
        /// <c>DOTween.IsTweening(대상)</c> 이 못 본다(중첩 트윈은 활성 목록에서 빠진다). 회차 2 가 그래서 또 빨갰다.
        /// 그래서 <b>«그 일이 일어났다» 를 코드가 기록</b>한다 — 로딩 화면이 <c>LastShownWasPrefab</c> 으로 같은 함정을 푼 그 방법이다.
        /// </para>
        /// </summary>
        public static float LastChestScale;
        /// <summary>
        /// 연출 상수 — <b>T180 순서(주인 2026-09-07 11:3X «닫힌 게 위에서 떨어져서 착지하고 열린 상태 이미지로 바뀐 다음에 장비들»)</b>:
        /// 낙하(<see cref="ChestFallSec"/> · <see cref="ChestFallFrom"/> px 위에서) → 착지 «쿵»(<see cref="ChestShake"/>) →
        /// 열린 그림 교체 + 빛 폭발(<see cref="ChestOpenAt"/>) → 장비 칸이 하나씩(<see cref="ChestCellStep"/> 간격 · 시작 스케일 <see cref="ChestCellFrom"/>).
        /// <para>
        /// 낙하가 앞에 붙는 만큼 뒤를 당겼다 — 칸 간격 0.08 → 0.05, 흔들림 0.25 → 0.18. 10회(10칸) 기준 마지막 칸이
        /// 0.36 + 9×0.05 = 0.81 에 시작해 <see cref="UiKit.RevealDur"/> 뒤 <b>≈1.03s</b> 에 끝난다(≤ <see cref="UiKit.RevealMaxResult"/> 언저리 · 예전 0.08 간격은 1.25s 였다).
        /// </para>
        /// </summary>
        public const float ChestFallSec = 0.24f, ChestFallFrom = 420f;
        /// <summary>
        /// T202(주인 2026-09-07 09:0X «상자가 바닥에 <b>착지하고 1초 뒤</b>에 열리는 애니메이션 떠야 함») — 착지와 열림 <b>사이의 정지</b>.
        /// 그 사이가 비어 보이지 않게 아주 작은 «두근두근»(<see cref="ChestBeat"/>) 두 번이 들어간다(지시서 2항 «스케일 ±2% 두 번»).
        /// 무작위를 안 쓰므로 <c>screens</c> 스샷이 회차마다 안 흔들린다(T174 가 알갱이에서 정한 것과 같은 규약).
        /// </summary>
        public const float ChestHoldSec = 1.0f, ChestBeat = 0.02f, ChestBeatSec = 0.22f;
        /// <summary>열림 시각 = <b>착지 + 정지</b>. 리터럴(옛 0.30)이 아니라 <b>관계</b>로 적는다 — 낙하 시간을 누가 바꾸면 «1초 뒤» 가 저절로 따라간다(§1).</summary>
        public const float ChestShake = 0.18f, ChestOpenAt = ChestFallSec + ChestHoldSec, ChestCellStep = 0.05f, ChestCellFrom = 0.55f;

        /// <summary>
        /// 소환(뽑기) 결과 창 — <b>주인 지정 조각 <c>Shop_Chest_Open</c> 그대로</b>(T95 · 2026-09-07 «소환 결과 창이 이 프리팹으로 돼야 하는데 안 됐더라»).
        /// 조각이 주는 것: 어둠 + 무늬 배경 · 열린 상자 그림(그림자·빛·반짝이 4) · 아래 «터치» 안내. 조각에 <b>격자는 없어서</b> 얻은 장비 칸만 우리가 상자 «위» 에 얹는다(등급색 ItemFrame · T69 7항).
        /// 연출(«찰지게» · T49 결): 상자가 짧게 흔들리고 → 빛이 터지고 → 칸이 한 장씩 오버슛으로 튀어나오고 → 최고 등급 한 칸이 한 번 더 튀고 → 제목·안내·터치 안내. 전부 unscaled + SetLink.
        /// 확률·천장·비용·결과 목록은 한 줄도 안 건드린다(T26 이 검증한 엔진·gacha.json · T95 3항).
        /// </summary>
        /// <summary>뽑기 결과 조각(<c>Shop_Chest_Open</c>)의 제목 리본 이름 — 조각 그대로다(그 안 «Text (TMP)» 가 데모 글자 «Reward» 를 들고 있다 · T95).</summary>
        const string ChestRibbonName = "Title_01_NoDeco_Tangerine";

        void ChestResult(GachaBox box, int n, List<GearItem> got, GearItem best)
        {
            var D = App.Data;
            LastChestScale = 0f;   // 이번 창이 배율 연출을 걸었는지 기록한다(T158 ⓐ) — 아래에서 실제로 걸 때 값이 들어간다
            var rootGo = App.Overlay.OpenPrefab("ui.chestOpen"); var root = (RectTransform)rootGo.transform;
            // 조각의 어둠+무늬 배경 = 프레임 밖까지(T104 와 같은 값) · 배경 탭 = 닫기
            Sequence seq = null;   // 아래에서 만든다 — 배경 탭이 «아직 도는 중이면 건너뛰기» 를 하려면 그 손잡이가 필요하다(T202 4항)
            var bg = UiKit.Find(root, "Background") as RectTransform;
            if (bg != null)
            {
                UiKit.Stretch(bg, -UiKit.DimOverscan, -UiKit.DimOverscan, -UiKit.DimOverscan, -UiKit.DimOverscan);
                // T202 — **첫 탭은 «건너뛰기», 그 다음 탭이 «닫기»**(주인이 여러 번 돌릴 때 2.4초를 매번 안 기다리게).
                // 연출이 1초 길어졌으므로 이 갈래가 없으면 탭 한 번에 결과를 못 보고 창이 닫힌다.
                // `Complete(true)` = 콜백까지 실행 → 열린 그림 교체·빛·칸이 전부 최종 상태가 된다(Overlay.Skip 과 같은 문법).
                UiKit.Clickable(bg, () =>
                {
                    if (seq != null && seq.IsActive() && seq.IsPlaying()) { seq.Complete(true); return; }
                    App.Overlay.Close(); Refresh();
                }, false);
            }
            // ⓑ T157 — 주인 «뽑기 결과에서도 패턴들 움직여야 함». 조각의 «Pattern» 은 정적 Image 고 우리 흐름은 RawImage 의 uvRect 트윈(T72 ①)인데,
            // 한 GameObject 는 Graphic 을 하나만 가지므로 그 조각에 RawImage 를 덧붙일 수 없다. 그래서 조각 것은 **이름을 바꿔 끄고**(같은 이름이면
            // PatternBg 가 그것을 찾아 RawImage 가 없다고 또 하나를 만들어 «Pattern» 이 둘이 된다) 같은 부모·같은 사각형에 흐르는 무늬를 깐다(결정 408).
            // 어두운 딤 위라 tint 는 PatternTintDark. **T110 ⓑ·T140 의 «패턴 없음» 과는 다른 화면이다** — 여기서는 주인이 흐르라고 했다.
            var oldPat = UiKit.Find(root, UiKit.PatternName) as RectTransform;
            if (oldPat != null && oldPat.GetComponent<RawImage>() == null)
            {
                oldPat.name = "PatternStatic"; oldPat.gameObject.SetActive(false);
                var host = oldPat.parent as RectTransform;
                if (host != null) UiKit.PatternBg(host, UiKit.PatternTintDark, siblingIndex: oldPat.GetSiblingIndex());
            }
            // 상자 그림만 우리 상자 종류로(자리·크기는 조각 그대로)
            var chestGrp = UiKit.Find(root, "Chest") as RectTransform;
            // T180 — 처음에는 «닫힌» 그림이다. 착지 뒤(ChestOpenAt)에 열린 그림으로 바뀐다(주인 «닫힌 게 … 착지하고 열린 상태 이미지로»).
            // 카탈로그에 닫힘/열림이 짝으로 있다(chest.<key> · chest.<key>.open) — 새 그림 0.
            var chestImg = UiKit.SetSprite(root, "Image_Chest", "chest." + box.Key, Palette.White);
            var openSprite = App.Assets != null ? App.Assets.Sprite("chest." + box.Key + ".open") : null;
            var touch = UiKit.SetText(root, "Text_TouchContionue", "탭하여 닫기");
            // 제목은 **조각 제 리본**에 쓴다(T95 1항 «프리팹 그대로») — 예전엔 리본을 그대로 둔 채 글자를 따로 얹어
            // 조각의 데모 글자(«Reward»)가 화면에 남았다(CI #235 «데모 프리팹 잔여 글자 1건»). 리본이 없는 조각이면 예전처럼 글자를 얹는다.
            string titleText = $"{box.Name} {n}회" + (got.Count > n ? $" · {got.Count}개" : "");
            var ribbon = UiKit.Find(root, ChestRibbonName);
            TMP_Text title;
            if (ribbon != null)
            {
                Overlay.FitRibbonText(ribbon);   // T75 4항 — 리본 글자 칸을 제목 60 한 줄(84px)로 올린다
                title = UiKit.SetText(ribbon, "Text (TMP)", titleText, Palette.Cream, TextSize.Title, TextKind.Title);
                if (title == null) title = ribbon.GetComponentInChildren<TMP_Text>(true);
            }
            else title = UiKit.Label(root, 6, 12, 88, 7, titleText, TextSize.Title, Palette.White, TextAnchor.MiddleCenter, true, false, kind: TextKind.Title);
            if (title != null) title.name = "Title";
            // T158 ⓑ — 안내 줄(«최고 등급 … · 장착은 장비 탭에서»)은 주인 지시로 없앴다(«이런 텍스트 빼셈 소환결과 부분»).
            // «최고 등급 한 칸 더 튀기» 연출은 그대로다 — best/bestCell 은 글자와 무관하다.
            // ⓐ T157 — 격자 자리는 **조각이 «여기» 라고 준 칸**(ItemFrame_01)에서 읽는다(주인 «ItemFrame_01 있는 곳에 아이템이 떠야 하는데 썡둥맞은 위치에 뜬다»).
            // 예전에는 그 칸을 쳐다보지도 않고 «화면 위 24%» 에 제 격자를 얹었다. 수(190×190 · y +217)는 **조각이 들고 있으니 코드에 안 박는다** — 자리·부모를 런타임에 읽는다.
            var slot = UiKit.Find(root, ChestSlotName) as RectTransform;
            // 격자 = ListItem_EquipMent 본래 크기(188 · 비례 고정) · 4열 — 10개면 3행
            float cs = GearUi.CellSize(App.Assets), gap = 12f; int rows = Mathf.Max(1, (got.Count + ResultCols - 1) / ResultCols);
            var grid = UiKit.Rect(slot != null ? slot.parent : root, "Got");
            grid.sizeDelta = new Vector2(ResultCols * cs + (ResultCols - 1) * gap, rows * cs + (rows - 1) * gap);
            if (slot != null)
            {
                // 조각 칸과 «가운데가 같게» — 한 개면 그 칸에 그대로 앉고, 10개면 그 자리를 가운데로 펼친다(지시서 ⓘⓙ).
                grid.anchorMin = slot.anchorMin; grid.anchorMax = slot.anchorMax; grid.pivot = new Vector2(0.5f, 0.5f);
                grid.anchoredPosition = slot.anchoredPosition + (slot.pivot - new Vector2(0.5f, 0.5f)) * -slot.rect.size;
                // 조각의 초록 프레임은 «자» 로만 쓰고 감춘다 — 우리 칸(GearUi.Cell)이 제 등급색 프레임을 세우므로
                // 그대로 두면 한 개일 때 초록 링이 등급색 밑에 비치고, 10개일 때는 격자 한복판에 홀로 남는다(결정 407).
                slot.gameObject.SetActive(false);
            }
            else
            {
                grid.anchorMin = new Vector2(0.5f, 1f); grid.anchorMax = new Vector2(0.5f, 1f); grid.pivot = new Vector2(0.5f, 1f);
                grid.anchoredPosition = new Vector2(0, -UiKit.FrameH * ChestGridFallbackTopPct / 100f);
            }
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(cs, cs); gl.spacing = new Vector2(gap, gap); gl.childAlignment = TextAnchor.MiddleCenter; gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = ResultCols;
            var cells = new List<RectTransform>();
            RectTransform bestCell = null;
            foreach (var g in got) { var c = GearUi.Cell(grid, D, g, new GearUi.CellOpts { IsNew = true }, null); cells.Add(c); if (g == best) bestCell = c; }
            // T158 ⓒ — 주인 «칸 눌러도 어두워지는 거 없애셈». 여기 칸에는 우리가 클릭을 안 붙이는데(onClick: null)
            // 조각(ListItem_EquipMent)이 제 Button 을 달고 와서 누르면 눌림 표시가 돈다. 그래서 이 창의 칸에서만 Button 을 떼어 낸다 —
            // transition = None 으로 죽이면 «모든 Button 은 눌림 표시» 계약(PressFeedbackTests)과 부딪히고, 아예 없으면 PressFeedback 도 첫 줄에서 되돌아간다(Btn == null).
            // 떼는 자리를 GearUi 가 아니라 여기로 잡은 까닭: 주인이 부른 것은 «소환결과 부분» 이고, 장비·인벤 칸의 눌림은 그대로 두어야 한다(결정 기록).
            foreach (var c in cells)
                foreach (var b in c.GetComponentsInChildren<Button>(true))
                    UnityEngine.Object.DestroyImmediate(b);
            // T190 — ⚑ **주인 13:4X 재확인**(«소환 결과에 아이템 슬롯 «내부» 빛 효과라든가 그런 거 없게 해») 이라
            // 여기 있던 «얻은 장비 칸 그림 뒤 빛살»(T72 ②)을 **아예 안 부른다**. 이 창을 만지는 T157·T158·T180 워커도 다시 넣지 말 것.
            // ── 연출(«찰지게» T95 · 순서는 T180) : 닫힌 상자 낙하 → 착지 «쿵» → 열린 그림 + 빛 폭발 → 장비 칸 ──
            seq = DOTween.Sequence().SetUpdate(true).SetTarget(root).SetLink(rootGo);
            if (chestGrp != null)
            {
                // «떨어진다» — 제자리(조각이 준 자리 · ChestGroupY)는 그대로 두고 그 «위» 에서 내려온다(자리를 바꾸는 것이 아니다 · 지시서 3항).
                // Ease.InQuad = 갈수록 빨라진다 = 떨어지는 느낌(OutQuad 는 느려져서 «내려놓는» 느낌이 된다).
                var home = chestGrp.anchoredPosition;
                chestGrp.anchoredPosition = home + new Vector2(0f, ChestFallFrom);
                // T158 ⓐ — 떨어지는 «동안» 작은 것이 커진다 · OutBack 이 끝에서 살짝 넘겼다가 제 크기로 돌아온다(주인 문장 그대로)
                chestGrp.localScale = Vector3.one * ChestScaleFrom;
                LastChestScale = ChestScaleFrom;
                seq.Insert(0f, chestGrp.DOScale(1f, ChestFallSec).SetEase(Ease.OutBack).SetUpdate(true).SetLink(chestGrp.gameObject));
                seq.Insert(0f, chestGrp.DOAnchorPos(home, ChestFallSec).SetEase(Ease.InQuad).SetUpdate(true).SetLink(chestGrp.gameObject));
                // 착지 «쿵» — 예전에는 이 펀치가 0초에 있었다(떨어지기 전에 흔들렸다). 이제 «닿는 순간» 이다.
                seq.Insert(ChestFallSec, chestGrp.DOPunchAnchorPos(new Vector2(0f, 22f), ChestShake, 12, 1f).SetUpdate(true).SetLink(chestGrp.gameObject));
                seq.InsertCallback(ChestFallSec, () => Audio.Sfx("snd.gacha"));   // 착지음(T28) — 뽑기 직후가 아니라 «닿는 순간»(결정 420)
                // T202 2항 — 착지와 열림 사이 «1초 정지» 가 죽은 시간으로 보이지 않게 아주 작은 두근두근 두 번(±2%).
                // 자리(anchoredPosition)가 아니라 **배율**을 건드리므로 착지 펀치와 겹쳐도 서로 안 밀어낸다.
                seq.Insert(ChestFallSec + ChestHoldSec * 0.30f, chestGrp.DOPunchScale(Vector3.one * ChestBeat, ChestBeatSec, 1, 1f).SetUpdate(true).SetLink(chestGrp.gameObject));
                seq.Insert(ChestFallSec + ChestHoldSec * 0.65f, chestGrp.DOPunchScale(Vector3.one * ChestBeat, ChestBeatSec, 1, 1f).SetUpdate(true).SetLink(chestGrp.gameObject));
            }
            // 닫힘 → 열림은 한 프레임에 톡 바뀌지만 같은 시각의 빛 폭발이 그 순간을 덮는다.
            // InsertCallback 이라 탭 스킵(DOTween.CompleteAll(true) · withCallbacks)에서도 «열린 상자» 로 끝난다(지시서 4항 ⓓ).
            if (chestImg != null && openSprite != null) seq.InsertCallback(ChestOpenAt, () => { if (chestImg != null) chestImg.sprite = openSprite; });
            var light = chestGrp != null ? UiKit.Find(chestGrp, "Light") as RectTransform : null;
            if (light != null)
            {
                // 낙하 동안에는 빛이 없어야 «열리면서 터진다» 로 읽힌다 → 0 에서 시작(예전 0.55 는 처음부터 보였다).
                light.localScale = Vector3.zero;
                seq.Insert(ChestOpenAt, light.DOScale(1f, 0.32f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(light.gameObject));
            }
            float end = UiKit.Stagger(seq, cells, ChestOpenAt + 0.06f, ChestCellStep, ChestCellFrom);
            // 최고 등급 한 칸만 한 번 더 튄다(등급이 여럿이어도 하나 · 연출 길이는 그대로)
            if (bestCell != null) seq.Insert(end, bestCell.DOPunchScale(Vector3.one * 0.12f, 0.22f, 8, 1f).SetUpdate(true).SetLink(bestCell.gameObject));
            seq.Insert(end, UiKit.Reveal(title.rectTransform));
            if (touch != null) seq.Insert(end + 0.12f, UiKit.Reveal(touch.rectTransform));
        }
    }
}
