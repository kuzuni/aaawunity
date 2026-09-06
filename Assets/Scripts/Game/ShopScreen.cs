using System;
using System.Collections.Generic;
using KkomaKnight.Core;
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
        const float SecBoxY = 13.5f;
        /// <summary>헤더 → 그 아래 첫 내용까지의 간격 — «다이아» 헤더(74.0)와 첫 카드행(78.5)의 간격 그대로. T100 이 «상자» 헤더를 끼우며 아래를 이만큼 민다.</summary>
        const float SecShift = 4.5f;
        /// <summary>표 ⑤ «(뽑기 화면) 대형 상자 배너» · «상자 카드 2개(좌우 각 45.5)» · «상자 버튼 2개(배너 안 아래)» — T100 의 «상자» 헤더만큼(<see cref="SecShift"/>) 내려간 자리.</summary>
        static readonly Layout.R Banner = new Layout.R(3.0f, 13.5f + SecShift, 94.0f, 26.0f);
        static readonly Layout.R ChestRow = new Layout.R(3.0f, 40.5f + SecShift, 94.0f, 29.0f);
        const float ChestCardW = 45.5f;
        static readonly Layout.R FreeLine = new Layout.R(3.0f, 70.3f + SecShift, 94.0f, 2.8f);
        /// <summary>
        /// «다이아» 섹션 헤더 y — 09_shop_1.jpg 의 헤더(22.5)·카드행(27 · 47.5)·둘째 헤더(66)·행(70.5) 간격을 그대로 이어 붙인다(<see cref="Layout.ShopSec1"/> 계열 표값에서 계산).
        /// T100 의 «상자» 헤더만큼 내려가지만 <see cref="ContentEnd"/> 도 같이 내려가므로 <b>스크롤 맨 아래에서 보이는 09 화면은 한 픽셀도 안 바뀐다</b>(표 ⑤ 09 행 불변).
        /// </summary>
        const float SecGemY = 74.0f + SecShift;
        static float Row1Y => SecGemY + (Layout.ShopCardRow1.Y - Layout.ShopSec1.Y);          // 78.5
        static float Row2Y => Row1Y + Layout.ShopCardRowPitch;                                  // 99.0
        static float SecGoldY => SecGemY + (Layout.ShopSec2.Y - Layout.ShopSec1.Y);            // 117.5
        static float Row3Y => SecGemY + (Layout.ShopCardRow3.Y - Layout.ShopSec1.Y);           // 122.0
        /// <summary>내용 끝 = 골드 행 아래 여백 3.5(레퍼런스 09 의 골드 카드 아래 ~ 탭 바 = 2.9) → 끝까지 내리면 «다이아» 헤더가 표 ⑤ 09 의 22.5 자리에 온다(= 09 스크린샷 = 스크롤 맨 아래).</summary>
        static float ContentEnd => Row3Y + Layout.ShopCardRow3.H + 3.5f;                        // 144.0
        /// <summary>팝업 = 폭 87 · 좌우 여백 6.5(표 ⑧ 공통).</summary>
        static readonly Layout.R ResultBox = new Layout.R(6.5f, 20.0f, 87.0f, 56.0f);
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
        Text _freeTxt; readonly List<Button> _freeBtns = new List<Button>(); readonly List<GameObject> _freeDots = new List<GameObject>();
        readonly Dictionary<string, BoxWidgets> _box = new Dictionary<string, BoxWidgets>();
        readonly List<(Button btn, Func<bool> can)> _gated = new List<(Button, Func<bool>)>();
        /// <summary>빛살이 도는 칸(T72 ② · 4항 «보이는 칸만» — 스크롤 밖 칸은 <see cref="UiKit.SetLightSpinning"/> 으로 멈춘다).</summary>
        readonly List<RectTransform> _lightCells = new List<RectTransform>();
        /// <summary>빛살을 걸 자리(칸 · 아이콘 · 조각 키) — <b>배치가 끝난 뒤</b> 한꺼번에 건다(아이콘 rect 가 % 앵커라 Build 중에는 0 이고, 그러면 빛살 한 변이 0 이 된다).</summary>
        readonly List<(RectTransform host, RectTransform icon, string key)> _lightPlan = new List<(RectTransform, RectTransform, string)>();
        float _timerT;
        sealed class BoxWidgets { public Button One, Ten; public readonly List<Text> Pills = new List<Text>(); }

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
            GachaBox big = null; foreach (var b in D.Gacha.Boxes) if (big == null || b.Cost > big.Cost) big = b;
            var small = new List<GachaBox>(); foreach (var b in D.Gacha.Boxes) if (b != big) small.Add(b);
            RectTransform bigCard = null; var smallCards = new List<RectTransform>(); var smallBottoms = new List<RectTransform>(); var bigBtns = new List<RectTransform>();
            if (big != null) { bigCard = Place(UiKit.Rect(_content, "Box:" + big.Key), Banner); BuildBigCard(bigCard, big, bigBtns); }
            for (int i = 0; i < small.Count && i < 2; i++)
            {
                var card = Place(UiKit.Rect(_content, "Box:" + small[i].Key), new Layout.R(ChestRow.X + i * (ChestRow.W - ChestCardW), ChestRow.Y, ChestCardW, ChestRow.H));
                BuildSmallCard(card, small[i]); smallCards.Add(card);
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

            // ④ «다이아» 3열×2행(shop.json gemPacks · ₩ 모의 결제 = 누르면 바로 지급) · ⑤ «골드» 3열×1행(goldPacks · 다이아 소모)
            var gems = D.Shop != null ? D.Shop.GemPacks : new List<ShopData.GemPack>();
            var golds = D.Shop != null ? D.Shop.GoldPacks : new List<ShopData.GoldPack>();
            UiKit.Tag(Header(SecGemY, "다이아"), "섹션 헤더");
            for (int i = 0; i < gems.Count && i < 6; i++)
            {
                var p = gems[i]; var slot = Place(UiKit.Rect(_content, "GemPack:" + i), CardRect(i < 3 ? Row1Y : Row2Y, i % 3));
                if (i == 0) UiKit.Tag(slot, "상품 카드(1칸)"); else if (i == 3) UiKit.Tag(slot, "상품 카드 2행");
                BuildPack(slot, UiKit.FmtQty(p.Gem), "shop.gem." + Mathf.Clamp(i + 1, 1, 6), "다이아 · 모의 결제", null, $"₩{p.Won:#,0}", Color.Lerp(Palette.Plum, Palette.Ink, 0.35f),
                    () => { App.Save.Gem += p.Gem; App.Persist(); Refresh(); App.Toast($"다이아 {UiKit.FmtQty(p.Gem)} 지급 (모의 결제)"); });
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
                });
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
            var txt = UiKit.SetText(rt, "Text (TMP)", text, Palette.White, HeaderSize); if (txt != null) { txt.fontStyle = FontStyle.Bold; txt.resizeTextForBestFit = true; txt.resizeTextMinSize = TextSize.BestFitMin; txt.resizeTextMaxSize = HeaderSize; }
            var line = UiKit.Find(rt, "LineDeco") as RectTransform; if (line != null) line.sizeDelta = new Vector2(UiKit.FrameW * 0.6f, line.sizeDelta.y);   // 선을 레퍼런스처럼 길게(조각은 그대로 · 폭만)
            // T100 ⓒ — 섹션을 나누는 선만 아주 옅게(제목 글자는 그대로). 조각 안에 선이 여럿일 수 있어 이름이 «LineDeco» 로 시작하는 그림 전부.
            foreach (var im in rt.GetComponentsInChildren<Image>(true))
                if (im.name.StartsWith("LineDeco")) { var c = im.color; c.a = SecLineAlpha; im.color = c; }
            return rt;
        }
        /// <summary>어두운 반투명 pill(TransperDark 조각) + 흰 글자 — 설명·천장 줄. 글자 = 본문 하한(40 · 2줄까지 접힘 · pill 높이는 카드 % 로 2줄이 들어가게).</summary>
        Text Pill(RectTransform card, Layout.R r, string text)
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
        /// <summary>[라벨][💎 아이콘][가격] 한 줄 — HorizontalLayoutGroup 이 자식을 선호 크기로 가운데 정렬(글자는 Overflow · rect 가 선호 폭과 같아 반올림으로 줄이 접히지 않게).</summary>
        RectTransform PriceRow(RectTransform parent, Layout.R r, string cost, string before = null)
        {
            var row = UiKit.Rect(parent, "Price"); UiKit.Pct(row, r);
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = PriceGap; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false; hl.childControlWidth = true; hl.childControlHeight = true;
            if (!string.IsNullOrEmpty(before)) { var t = UiKit.Text(row, before, TextSize.Button, Palette.White, TextAnchor.MiddleCenter, false, true, TextKind.Button); t.name = "Label"; t.horizontalOverflow = HorizontalWrapMode.Overflow; }
            var ic = UiKit.Icon(row, "Gem", "hud.gem"); ic.preserveAspect = true;
            var le = ic.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = PriceIconSize; le.preferredHeight = PriceIconSize;
            var c = UiKit.Text(row, cost, TextSize.Button, Palette.White, TextAnchor.MiddleCenter, false, true, TextKind.Button); c.name = "Cost"; c.horizontalOverflow = HorizontalWrapMode.Overflow;
            return row;
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
            for (int i = box.Rate.Length - 1; i >= 0; i--) if (box.Rate[i] > 0) o.Add($"<color=#{ColorUtility.ToHtmlStringRGB(Palette.ByName(Palette.RarName(i)))}>{GearUi.RarName(D, i)}</color> {box.Rate[i]:0.#}%");
            return string.Join(" · ", o);
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
        /// <summary>최상위 상자 큰 카드(10_shop_2.jpg 위) — 그림 왼쪽 · 오른쪽에 이름 + (i) · 확률 한 줄 · 천장 pill 2 · 아래 «1회 💎» · «10회 💎» 주황 2개(표 ⑤ «상자 버튼 2개» 자리).</summary>
        void BuildBigCard(RectTransform card, GachaBox box, List<RectTransform> btnsOut)
        {
            var D = App.Data; var w = new BoxWidgets(); string key = box.Key;
            var frame = UiKit.Spawn(Palette.FrameKey("ui.cardFrame", BoxColor(box)), card); UiKit.Stretch((RectTransform)frame.transform);
            // T100 ⓐ — 제목 바탕 끄기(§1 «문장 끝 // 주석 금지» 대로 주석은 윗줄에)
            HideCardTitleBg(frame.transform);
            // 상자 이름 = 제목(60 · T63-shop · 레퍼런스 «Legendary Chest» 는 카드에서 가장 큰 글자) — 칸 13% × 배너 26% = 79px ≥ 선호 59
            var title = UiKit.SetText(frame.transform, "Text_Title", box.Name, Palette.Yellow, TextSize.Title, TextKind.Title);
            if (title != null) { UiKit.Pct(title.rectTransform, 42, 3, 49, 13); title.alignment = TextAnchor.MiddleRight; title.fontStyle = FontStyle.Bold; title.resizeTextForBestFit = true; title.resizeTextMinSize = TextSize.BestFitMin; title.resizeTextMaxSize = TextSize.Title; }
            InfoButton(card, new Layout.R(91.5f, 4, 7, 12), box);
            var chest = UiKit.Icon(card, "Chest", "chest." + box.Key); UiKit.Pct(chest.rectTransform, 4, 8, 36, 56);
            // T72 ② 특별 상품(대형 상자) 그림 뒤 빛살 — 큰 조각(Effect_Light_01)
            _lightPlan.Add((card, chest.rectTransform, UiKit.LightKey)); _lightCells.Add(card);
            // 확률 줄 = 본문 40(4 등급이면 2줄 · 칸 16% × 배너 26% = 97px ≥ 2줄 88)
            UiKit.Label(card, 42, 20, 54, 16, RatesText(box), TextSize.Body, Palette.White);
            w.Pills.Add(Pill(card, new Layout.R(42, 45, 55, 10), ""));
            w.Pills.Add(Pill(card, new Layout.R(42, 58.5f, 55, 10), ""));
            var one = PriceButton(card, "One", "1회", box.Cost, () => Pull(1, key), new Layout.R(2.5f, 74, 46, 21), true);
            var ten = PriceButton(card, "Ten", $"{D.Gacha.TenPullCount}회", box.Cost * D.Gacha.TenPullCount, () => Pull(D.Gacha.TenPullCount, key), new Layout.R(51.5f, 74, 46, 21), true);
            w.One = one.GetComponent<Button>(); w.Ten = ten.GetComponent<Button>();
            btnsOut.Add(one); btnsOut.Add(ten);
            // T69-shop «검은 아웃라인» — CardFrame_04 조각의 제 외곽선은 프레임 3~4px 라 폰에서 1px 남짓(8px 규칙 미달) → 카드 위에 Ink 링 한 장(가운데 비움 · raycast 끔 · 표 % 불변)
            UiKit.Bordered(card);
            _box[key] = w;
        }
        /// <summary>나머지 상자 작은 카드(10_shop_2.jpg 가운데) — 이름 + (i) · 확률 pill · 그림 · 천장 pill · 아래 <b>광고(파랑 · 무료 보급 수령)</b> + <b>«💎가격»(1회)</b>.</summary>
        void BuildSmallCard(RectTransform card, GachaBox box)
        {
            var D = App.Data; var w = new BoxWidgets(); string key = box.Key;
            var frame = UiKit.Spawn(Palette.FrameKey("ui.cardFrame", BoxColor(box)), card); UiKit.Stretch((RectTransform)frame.transform);
            // T100 ⓐ — 제목 바탕 끄기(§1 «문장 끝 // 주석 금지» 대로 주석은 윗줄에)
            HideCardTitleBg(frame.transform);
            // 상자 이름 = 제목(60 · T63-shop) — 칸 10% × 카드 29% = 68px ≥ 선호 59 · 확률 pill 은 그 아래(12.5~26.5% · 95px ≥ 2줄 88 — 회차 1 의 13% = 88px 은 딱 맞아 bestFit 이 39 로 눌렀다 · CI #110 표 «최소 크기(실제) 39»)
            var title = UiKit.SetText(frame.transform, "Text_Title", box.Name, Palette.White, TextSize.Title, TextKind.Title);
            if (title != null) { UiKit.Pct(title.rectTransform, 6, 2, 76, 10); title.alignment = TextAnchor.MiddleCenter; title.fontStyle = FontStyle.Bold; title.resizeTextForBestFit = true; title.resizeTextMinSize = TextSize.BestFitMin; title.resizeTextMaxSize = TextSize.Title; }
            InfoButton(card, new Layout.R(84, 2, 12, 9), box);
            Pill(card, new Layout.R(6, 12.5f, 88, 14), RatesText(box));
            var chest = UiKit.Icon(card, "Chest", "chest." + box.Key); UiKit.Pct(chest.rectTransform, 22, 28, 56, 37);
            // T72 ② 상자 카드 그림 뒤 빛살(작은 칸이라 Effect_Light_02)
            _lightPlan.Add((card, chest.rectTransform, UiKit.LightKeySmall)); _lightCells.Add(card);
            w.Pills.Add(Pill(card, new Layout.R(6, 67, 88, 14), ""));
            // 광고 버튼(파랑 · 클래퍼) = 일일 무료 보급(gacha.json dailyGem · 하루 1회) — 받을 수 있으면 빨간 점
            var ad = UiKit.Button(card, "ui.btnBlue", "", OnFree, new Layout.R(6, 83, 42, 14)); ad.name = "Ad";
            var adIc = UiKit.Icon(ad, "Icon", "ui.ad"); UiKit.Pct(adIc.rectTransform, 26, 12, 48, 76);
            var dot = UiKit.Spawn("ui.alertDot", ad); var drt = (RectTransform)dot.transform; dot.name = "FreeDot"; drt.anchorMin = drt.anchorMax = new Vector2(1, 1); drt.pivot = new Vector2(0.5f, 0.5f); drt.anchoredPosition = new Vector2(-6, -2); drt.sizeDelta = new Vector2(44, 44);
            _freeBtns.Add(ad.GetComponent<Button>()); _freeDots.Add(dot);
            var one = PriceButton(card, "One", "1회", box.Cost, () => Pull(1, key), new Layout.R(52, 83, 42, 14), false);
            w.One = one.GetComponent<Button>();
            // T69-shop — 큰 카드와 같은 Ink 링(광고·가격 버튼 줄은 카드 «안» 이라 따로 상자를 두지 않는다 · 레퍼런스 10 도 그렇다 · BorderAudit.Exempt)
            UiKit.Bordered(card);
            _box[key] = w;
        }

        // ───────────────────────── 상품 카드 (ListItem_ShopItem 부품 · 수량 → 그림 → 이름 → 가격 띠) ─────────────────────────
        /// <summary>다이아/골드 카드 1칸 — 09_shop_1.jpg 카드 안 비례: 수량(위 5~19%) · 그림(20~64%) · 이름(66~77%) · 가격 띠(80~97%). 카드 전체와 가격 버튼이 같은 일을 한다. priceIconKey 가 null 이면 가격 아이콘을 끈다(₩).</summary>
        List<Button> BuildPack(RectTransform slot, string qty, string iconKey, string name, string priceIconKey, string price, Color tint, Action onClick)
        {
            var cell = UiKit.Spawn("ui.shopItem", slot); var crt = (RectTransform)cell.transform; UiKit.Stretch(crt);
            foreach (var im in cell.GetComponentsInChildren<Image>(true)) { if (im.name == "Bg(Mask)") im.color = tint; else if (im.name == "Botton") im.color = Palette.Cream; }
            UiKit.Hide(crt, "ItemFrameArea", "Text_ItemNum");
            // 수량 = 띠 높이에서 계산(≈51 · T63-shop) — 띠 14% × 카드 18.5% = 60px ≥ 선호 50
            var q = UiKit.SetText(crt, "Text_Title", qty, Palette.White, QtySize);
            if (q != null) { UiKit.Pct(q.rectTransform, 5, 5, 90, QtyBandH); q.fontStyle = FontStyle.Bold; q.resizeTextForBestFit = true; q.resizeTextMinSize = TextSize.BestFitMin; q.resizeTextMaxSize = QtySize; }
            var im2 = UiKit.SetSprite(crt, "Icon", iconKey, Palette.White);
            if (im2 != null)
            {
                im2.preserveAspect = true; UiKit.Pct(im2.rectTransform, 14, 20, 72, 44);
                // T72 ② 상점 상품 아이콘 뒤 빛살 — 아이콘은 조각(ListItem_ShopItem)의 바로 아래 자식이라 «아이콘 앞 형제» 가 곧 «그림 뒤»
                _lightPlan.Add(((RectTransform)im2.rectTransform.parent, im2.rectTransform, UiKit.LightKeySmall)); _lightCells.Add(crt);
            }
            var nm = UiKit.SetText(crt, "Text_Limit", name, Palette.White, TextSize.Body); if (nm != null) { UiKit.Pct(nm.rectTransform, 4, 66, 92, 11); nm.resizeTextForBestFit = true; nm.resizeTextMinSize = TextSize.BestFitMin; nm.resizeTextMaxSize = TextSize.Body; }
            var btns = new List<Button>();
            var btn = UiKit.Find(crt, "Button_Price");
            if (btn != null)
            {
                UiKit.Pct((RectTransform)btn, 6, 80, 88, 17);
                var pi = UiKit.Find(btn, "GroupArea/Group/Icon"); if (pi != null) { pi.gameObject.SetActive(priceIconKey != null); if (priceIconKey != null) UiKit.SetSprite(btn, "GroupArea/Group/Icon", priceIconKey, Palette.White); }
                // 가격 띠 글자 = 버튼 하한(44 · T63-shop) — Group(HorizontalLayoutGroup) 이 글자 rect 를 선호 폭으로 잡으므로 Overflow 유지(T40 · Wrap 이면 글자마다 줄이 접힌다)
                var pt = UiKit.SetText(btn, "GroupArea/Group/Text (TMP)", price, null, TextSize.Button, TextKind.Button); if (pt != null) { pt.resizeTextForBestFit = true; pt.resizeTextMinSize = TextSize.BestFitMin; pt.resizeTextMaxSize = TextSize.Button; pt.horizontalOverflow = HorizontalWrapMode.Overflow; }
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
            if (CanFree(App.Save)) { _freeTxt.text = $"무료 보급 다이아 {UiKit.FmtQty(App.Data.Gacha.DailyGem)} — 지금 수령 가능"; return; }
            var left = DateTime.Today.AddDays(1) - DateTime.Now; if (left.Ticks < 0) left = TimeSpan.Zero;
            _freeTxt.text = $"무료 보급까지 {(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
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
        void Pull(int n, string boxKey)
        {
            var D = App.Data; var S = App.Save;
            GachaBox box = null; foreach (var b in D.Gacha.Boxes) if (b.Key == boxKey) box = b; if (box == null) return;
            var st = State(boxKey);
            double cost = box.Cost * n; if (S.Gem < cost) { App.Toast("다이아가 부족합니다"); return; }
            S.Gem -= cost;
            var rng = new Mulberry32((uint)Environment.TickCount ^ 0x5bd1e995u);
            var got = new List<GearItem>();
            for (int i = 0; i < n; i++)
            {
                foreach (var raw in GearSystem.GachaPull(D, st, box, rng)) { var g = S.NewGear(raw.Part, raw.Type, raw.Rar, raw.Plus); g.IsNew = true; S.Inv.Add(g); got.Add(g); }
                S.Pulls++;
            }
            App.Persist(); Refresh();
            Audio.Sfx("snd.gacha");   // 상자 열림(T28)
            var best = got[0]; foreach (var g in got) if (GearSystem.GearScore(g) > GearSystem.GearScore(best)) best = g;
            var popup = App.Overlay.OpenBox("ui.popup", "ui.title.tangerine", $"{box.Name} {n}회" + (got.Count > n ? $" · {got.Count}개" : ""), ResultBox, () => { App.Overlay.Close(); Refresh(); });
            var chest = UiKit.Icon(popup, "Chest", "chest." + box.Key + ".open"); UiKit.Pct(chest.rectTransform, 30, 5, 40, 22);
            // 안내 한 줄 = 본문 40(T63-shop) — 칸 7% × 상자 56% = 92px · 문구를 줄여(«인벤토리에 담겼습니다» 삭제) 40 으로 한 줄(≈ 18자 · 650px ≤ 846px)
            var note = UiKit.Label(popup, 5, 28, 90, 7, $"최고 등급 <color=#{ColorUtility.ToHtmlStringRGB(Palette.ByName(Palette.RarName(best.Rar)))}>{GearUi.RarName(D, best.Rar)}</color> · 장착은 장비 탭에서", TextSize.Body, Palette.InkSoft, TextAnchor.MiddleCenter, true, false); note.name = "Note";
            // 격자 = ListItem_EquipMent 본래 크기(188 · 비례 고정) · 4열 — 10개면 3행이 상자 안에 들어간다
            float cs = GearUi.CellSize(App.Assets), gap = 12f; int rows = Mathf.Max(1, (got.Count + ResultCols - 1) / ResultCols);
            var grid = UiKit.Rect(popup, "Got"); grid.anchorMin = new Vector2(0.5f, 1f); grid.anchorMax = new Vector2(0.5f, 1f); grid.pivot = new Vector2(0.5f, 1f);
            grid.sizeDelta = new Vector2(ResultCols * cs + (ResultCols - 1) * gap, rows * cs + (rows - 1) * gap); grid.anchoredPosition = new Vector2(0, -ResultBox.H / 100f * UiKit.FrameH * 0.36f);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>(); gl.cellSize = new Vector2(cs, cs); gl.spacing = new Vector2(gap, gap); gl.childAlignment = TextAnchor.UpperCenter; gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = ResultCols;
            var cells = new List<RectTransform>();
            foreach (var g in got) cells.Add(GearUi.Cell(grid, D, g, new GearUi.CellOpts { IsNew = true }, null));
            // T72 ② 뽑기 결과 — 상자 열림 그림 뒤(큰 조각) · 얻은 장비 칸의 그림 뒤(작은 조각 · ItemFrame 안쪽에서만 보인다)
            // 격자 칸은 GridLayoutGroup 이 배치한 뒤에야 크기가 정해지므로 여기서도 배치를 한 번 돌리고 건다(결정 174)
            Canvas.ForceUpdateCanvases();
            UiKit.LightBehind(popup, chest.rectTransform, UiKit.LightKey);
            foreach (var c in cells)
            {
                var item = UiKit.Find(c, "Item");
                if (item != null && item.gameObject.activeSelf) UiKit.LightBehind((RectTransform)item.parent, (RectTransform)item, UiKit.LightKeySmall);
            }
            UiKit.PopIn(chest.rectTransform, 0.5f, 0.5f);
        }
    }
}
