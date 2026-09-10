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
    /// T241 — 공통 «리워드» 획득 팝업(주인 2026-09-08 «퀘스트나 뭐 보상 받거나 할 때 리워드 팝업 저런 식으로 뜨게» ·
    /// 레퍼런스 <c>docs/ref/35_reward_popup.jpg</c>). <b>보상을 주는 모든 곳이 이 한 함수만 부른다</b> — 화면마다 제 나름의
    /// 토스트·팝업을 따로 만들지 않는다(지시서 T241 2항).
    /// <para>
    /// 모양은 «상자» 가 아니라 <b>화면을 가로지르는 줄</b>이다: 어둠 → 빛살 → 노란 «리워드» → 위 노란 줄 → 칸 줄 → 아래 노란 줄 → «탭하여 닫기».
    /// 자리는 전부 <see cref="Layout"/> 의 <c>Rw*</c>(레퍼런스 35 실측)이고 이 파일에 수치를 박지 않는다.
    /// </para>
    /// <para>
    /// ⚠ <b>왜 <c>Overlay</c> 안이 아니라 새 파일인가</b> — 지시서는 «<c>Overlay.Reward(items)</c> 같은 한 함수» 라고 적었는데
    /// 이 회차에 <c>Game/Overlay.cs</c> 가 다른 워커의 살아 있는 lock(T234·T236 «리본 뒤 빛») 안이었다.
    /// claims 규약 «같은 파일을 두 작업이 만지면 번호가 큰 쪽이 기다린다» 대로 그 파일을 안 건드리고 <b>부르는 자리 하나</b>라는
    /// 지시서의 뜻만 지켰다. 그 lock 이 풀리면 <c>Overlay.Reward(...) =&gt; RewardPopup.Show(...)</c> 한 줄을 얹어도 된다(그때도 여기가 본체다).
    /// </para>
    /// </summary>
    public static class RewardPopup
    {
        /// <summary>팝업이 보여 주는 «얻은 것» 한 칸.</summary>
        public struct Item
        {
            /// <summary>칸 안 그림(카탈로그 스프라이트 키 · 예 <c>ui.coin</c>).</summary>
            public string Icon;
            /// <summary>칸 아래 개수 글자(«×3» 처럼 부르는 쪽이 다듬어 넘긴다 · 비면 안 쓴다).</summary>
            public string Qty;
            /// <summary>칸 테두리 조각 키(등급색 · 비면 <see cref="DefaultFrame"/>).</summary>
            public string Frame;
            /// <summary>
            /// 받은 <b>개수</b>(T269 · 파티클을 이만큼 띄운다 · 0 이면 <see cref="Qty"/> 의 숫자에서 읽는다).
            /// 글자(<see cref="Qty"/>)는 «×3»·«1,000» 처럼 보여 주려고 다듬은 것이라 세는 데 쓰기엔 약하다 — 아는 쪽이 수를 그대로 넘기는 길을 둔다.
            /// </summary>
            public int Amount;
            /// <summary>
            /// 칸 그림을 <b>스프라이트 대신 직접 세우고 싶을 때</b>(T396 «메이커 펫 초상») — 칸의 «Icon» rect 를 받아 그 안에 세운다. <c>null</c> 이면 <see cref="Icon"/> 스프라이트 그대로.
            /// <para>⚠ 그래도 <see cref="Icon"/> 은 채워 둔다 — 흡수 구슬(<see cref="RewardOrbs"/>)이 날아갈 때 쓰는 그림이 그 키이고, 초상은 날 수 없다.</para>
            /// </summary>
            public Action<RectTransform> Face;
            public static Item Of(string icon, string qty = null, string frame = null, int amount = 0) => new Item { Icon = icon, Qty = qty, Frame = frame, Amount = amount };
        }

        /// <summary>
        /// T291 — 던전 보상 한 벌(<see cref="DungeonData.Reward"/>) → 팝업 칸들. <b>이 자리는 하나여야 한다.</b>
        /// <para>
        /// 여태 소탕(<c>EventsScreen.SweepItems</c>)과 판 클리어(<c>BattleScreen.ExitBattleWithPrize</c>)가 <b>같은 셈을 각자</b> 하고 있었고,
        /// 층 보상으로 <b>레시피·키</b> 두 칸이 늘자 **둘 다 그것을 안 그렸다** — 세이브에는 들어오는데 «무엇을 받았는지» 화면이 말을 안 한다.
        /// 주는 손을 <c>DungeonSweep.Pay</c> 하나로 모은 것과 같은 까닭으로 <b>보여 주는 손도</b> 하나로 모은다(결정 836 ④의 짝).
        /// </para>
        /// <b>개수는 글자에서 읽지 않고 그대로 넘긴다</b>(<see cref="Item.Amount"/>) — 골드는 «3,500» 처럼 쉼표가 든 글자라
        /// 거기서 수를 세면 파티클이 3 개만 뜬다(소탕 쪽이 그랬다).
        /// </summary>
        public static List<Item> ItemsOf(DungeonData.Reward r)
        {
            var list = new List<Item>();
            if (r == null) return list;
            if (r.PetEgg > 0) list.Add(Item.Of("pet.egg", UiKit.FmtComma(r.PetEgg), amount: (int)Math.Round(r.PetEgg)));
            if (r.Gold > 0) list.Add(Item.Of("ui.coin", UiKit.FmtComma(r.Gold), amount: (int)Math.Round(r.Gold)));
            if (r.Recipe > 0 && !string.IsNullOrEmpty(r.RecipePart))
                list.Add(Item.Of(Recipes.Icon(r.RecipePart), UiKit.FmtComma(r.Recipe), amount: (int)Math.Round(r.Recipe)));
            if (r.Key > 0 && !string.IsNullOrEmpty(r.KeyItem))
                list.Add(Item.Of(GachaKeys.Icon(r.KeyItem), UiKit.FmtComma(r.Key), amount: (int)Math.Round(r.Key)));
            return list;
        }

        /// <summary>기본 칸 테두리 — 레퍼런스 35 의 두 칸이 <b>파랑</b>이다(T103 정본 <c>ItemFrame_01_Normal_*</c> 계열).</summary>
        public const string DefaultFrame = "ui.itemFrame.blue";

        // ✂ T241 4단계(결정 692) — 이 팝업만 쓰던 «옅은 어둠» 상수(DimAlpha 0.65 → 0.88)를 없앴다. 내 첫 판단이 틀렸다.
        //   «주인 그림은 뒤 화면이 보이니 공통 UiKit.DimAlpha(0.985)보다 옅어야 한다» 고 봤는데, 같은 런(screens 539)의
        //   다른 팝업과 나란히 재니 수가 그 짐작을 무너뜨렸다:
        //     17_daily_gift(공통 0.985)  상단 바 25.5 · 특권 칸 28.9 · 하단 네비 28.6
        //     레퍼런스 35 의 뒤 화면      26~32              ← 같은 띠다
        //     이 팝업(0.88)              30.9 · 47.9 · 52.0 ← 훨씬 밝다
        //   레퍼런스에서 대장간이 «보이는» 것은 어둠이 옅어서가 아니라 그 화면 자체가 밝아서다 — 덮고 나면 같은 26~32 에 온다.
        //   그래서 특례를 지우고 UiKit.DimAlpha 를 쓴다(팝업 스무 곳과 같은 값 · 상수 하나가 줄었다).

        /// <summary>
        /// 빛살 조각의 한 변(프레임 폭의 비) — ⚠ <b>이것은 «자리» 가 아니라 «그림» 이라 표 ㊹ 가 못 잡는다.</b>
        /// 첫 회차는 자리 rect 의 폭(0.84)을 그대로 한 변으로 줬는데, 조각이 <b>정사각</b>이라 907px 짜리 별빛이 되어
        /// 화면 세로의 4할을 삼켰다(<c>screens</c> run 533 눈 확인 · 레퍼런스의 빛은 제목에 붙은 <b>납작한</b> 무리다).
        /// 정사각 조각으로 납작한 무리를 낼 수는 없으니(새 그림은 §1 이 막는다) <b>한 변을 줄이고 옅게</b> 해서
        /// 제목 둘레에만 머물게 한다 — T234 가 리본 뒤 빛에서 밟은 그 절충이다.
        /// </summary>
        public const float GlowSide = 0.40f;
        /// <summary>빛살 짙기 — 위와 같은 까닭으로 0.55 → 0.35(뒤 화면을 지우지 않을 만큼).</summary>
        public const float GlowAlpha = 0.35f;

        /// <summary>칸이 하나씩 뜨는 간격(초 · T95/T202 와 같은 결 · unscaled).</summary>
        public const float CellStagger = 0.05f;
        /// <summary>등장 연출 길이(초).</summary>
        public const float RevealSec = 0.25f;

        /// <summary>마지막으로 띄운 칸 수 — 테스트가 «지급한 만큼 칸이 섰나» 를 이걸로도 볼 수 있다.</summary>
        public static int LastCellCount { get; private set; }

        // ───────────────────────── T269 · 닫으면 파티클로 흡수 ─────────────────────────
        /// <summary>파티클 상한 — 주인 2026-09-09 «캡은 100개 파티클». 개수가 넘으면 <b>비율대로 줄이고</b> 값은 그대로다(지급은 이미 끝나 있다).</summary>
        public const int MaxOrbs = 100;
        /// <summary>
        /// 튀어나온 자리에서 머무는 시간 — 전투(T109)는 «죽은 그 자리에 1초 남았다가» 지만 여기서는 <b>그 자리가 닫히며 사라진다</b>.
        /// 주인 문장이 «화면 끄면 … 흡수되는 거로» 라 «끄는 동작 → 흡수» 가 바로 이어져야 한다(결정 기록).
        /// </summary>
        public const float AbsorbHoldSec = 0.06f;
        /// <summary>구슬 층 이름 — 화면·팝업보다 위(마지막 형제)에 있어야 날아가는 길이 안 가려진다.</summary>
        public const string OrbLayerName = "RewardOrbs";
        /// <summary>탑바에 자리가 없는 재화(장비·키·펫알…)가 사라지는 자리 — 화면 가운데 아래(지시서 4항).</summary>
        public const string OrbSinkName = "RewardOrbSink";
        /// <summary>마지막으로 띄운 파티클 수 — PlayMode 자가 «개수만큼(캡 100)» 을 이걸로 본다.</summary>
        public static int LastOrbCount { get; private set; }

        static RewardOrbs _orbs; static RectTransform _orbLayer, _orbSink;

        /// <summary>
        /// 골드·다이아는 탑바의 그 pill 로 간다(지시서 4항) — 조각 이름은 <see cref="TopBar"/> 가 붙이는 그것이다.
        /// <para>
        /// T367(주인 2026-09-10 «다이아 흡수, 골드 흡수는 상단에 재화들 각각 표시되는 부분 — 다이아는 다이아 쪽, 골드는 골드 쪽으로 흡수되어야 함») —
        /// 여태는 다이아 키를 **셋만** 알았다(`hud.gem`·`ui.iconGemPurple`·`ui.iconGemBlue`). 그런데 보상 칸 대부분(출석·데일리 기프트·챕터 상자·퀘스트 트랙)은
        /// <b>`ui.gemRed`</b> 를 쓴다 — 그 다이아는 이름을 못 알아봐 «자리 없는 재화» 로 화면 가운데 아래(<see cref="OrbSinkName"/>)로 빨려 들어갔다.
        /// 키를 하나 더 외우는 대신 <b>뜻으로</b> 가른다: 키에 <c>gem</c> 이 들어 있으면 다이아, <c>coin</c>/<c>gold</c> 가 들어 있으면 골드(카탈로그의 재화 키가 전부 그 꼴이다).
        /// 새 다이아 그림이 생겨도 여기 한 줄은 안 바뀐다.
        /// </para>
        /// </summary>
        public static string PillFor(string icon)
        {
            if (string.IsNullOrEmpty(icon)) return null;
            var k = icon.ToLowerInvariant();
            if (k.Contains("gem")) return "ResourceBar_Gem";
            if (k.Contains("coin") || k.Contains("gold")) return "ResourceBar_Coin";
            return null;
        }

        /// <summary>«×3»·«1,000» 같은 글자에서 수를 읽는다 — 숫자가 아니면 1(칸이 하나라도 날아가게).</summary>
        static int QtyOf(Item it)
        {
            if (it.Amount > 0) return it.Amount;
            string s = it.Qty ?? "";
            long n = 0; bool any = false;
            foreach (var ch in s) { if (ch >= '0' && ch <= '9') { any = true; n = n * 10 + (ch - '0'); if (n > MaxOrbs) return MaxOrbs; } }
            return any && n > 0 ? (int)n : 1;
        }

        /// <summary>보상 팝업을 띄운다. <paramref name="items"/> 가 비면 아무것도 안 한다(«얻은 게 없는데 뜨는» 팝업 금지).</summary>
        public static void Show(IList<Item> items, Action onClose = null)
        {
            var app = App.I;
            if (app == null || app.Overlay == null || items == null || items.Count == 0) { LastCellCount = 0; return; }
            var ov = app.Overlay;
            ov.Close();   // 앞 팝업이 있으면 트윈까지 깨끗이 죽인다(Overlay.Close = KillReveal + Clear + 끄기) — Overlay.cs 를 안 건드리는 길이다
            var root = ov.Root;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            Audio.Sfx("snd.popup");

            // ⓐ 어둠 — 프레임 밖(레터박스·노치)까지(T104). 누르면 닫힌다(레퍼런스 바닥 «탭하여 닫기»).
            var dim = UiKit.Rect(root, "Dimmed");
            UiKit.Stretch(dim, -UiKit.DimOverscan, -UiKit.DimOverscan, -UiKit.DimOverscan, -UiKit.DimOverscan);
            var di = dim.gameObject.AddComponent<Image>();
            di.color = Palette.A(Palette.Dim, UiKit.DimAlpha); di.raycastTarget = true;
            UiKit.FadeIn(di, UiKit.DimAlpha);
            // ⚠ 어둠에는 이름표를 안 단다 — 프레임 «밖»(레터박스·노치)까지 덮으므로 자리로 재면 x−370 · w840 이 나온다(§5 가 0점을 준다).
            //    어둠은 «자리» 가 아니라 «세기» 로 판정하는 것이라 표 ㊹ 머리에 그 수(알파·뒤 화면 밝기)를 적어 두었다.

            // ⓑ 빛살 — 제목 뒤. 여기는 «칸» 이 아니라 리본 자리와 같은 갈래라 clip 을 끈다(T189 예외 · Overlay 의 레벨업 빛과 같은 호출 꼴).
            var glow = UiKit.Rect(root, "RewardGlow");
            UiKit.Pct(glow, Layout.RwGlow);
            // ⚠ 조각은 `ui.light2`(LightKeySmall)다 — `ui.light1` 은 살이 꽉 찬 «수레바퀴» 라 제목 뒤가 **노란 원반**이 된다
            //    (`screens` run 542 눈 확인). 워커 G 가 T234 회차 3 에서 리본 뒤 빛에 같은 교체를 하고 «부채처럼 살이 퍼진다» 를 확인했다 —
            //    레퍼런스 35 의 빛도 그 꼴이라 같은 조각을 쓴다. 크기·짙기는 이 회차에 안 건드린다(한 번에 손잡이 하나 · T234 회차 2).
            // ⚑ T320(주인 2026-09-09 12:1X «리워드 부분도 반 잘린 마스크») — 리본 팝업들과 **같은 «반 잘림»** 을 여기에도.
            //   이 팝업엔 리본이 없다(글자 제목 + 노란 가로줄) ⇒ 자르는 줄은 **위 노란 줄**(표 ㊹ `RwLineTop`)로 잡는다.
            //   그러면 빛이 제목 «리워드» 뒤에서 위로만 오르고 줄 아래로는 한 픽셀도 안 샌다 — 리본 팝업의 «리본 바닥» 과 같은 뜻이다.
            //   배율은 빛 한 변에서 되돌려 낸다(마스크가 빛보다 좁아야 옆도 살짝 잘린다 — ⓐ 의 555 ↔ 584.3 그 비).
            float glowSide = UiKit.FrameW * GlowSide;
            float glowScale = glowSide / (Overlay.TitleMaskW * Overlay.TitleLightSidePerMask);
            var gmask = Overlay.TitleGlowMask(glow, glowScale);
            {
                float hostH = UiKit.FrameH * Layout.RwGlow.H / 100f;
                float cutFromTop = (Layout.RwLineTop.Y - Layout.RwGlow.Y) / Layout.RwGlow.H * hostH;   // 담개 위끝 → 노란 줄
                float cutY = hostH * 0.5f - cutFromTop;                                                // 담개 가운데 기준(위가 +)
                gmask.anchoredPosition = new Vector2(0f, cutY + gmask.sizeDelta.y * 0.5f);             // 마스크 «바닥» 이 그 줄
            }
            UiKit.LightBehind(gmask, null, UiKit.LightKeySmall, UiKit.LightPeriod, Palette.A(Palette.Reward, GlowAlpha),
                              sidePx: glowSide, clip: false);   // 자르는 것은 위 Mask 다(RectMask2D 를 겹쳐 걸면 마스크가 둘)
            Overlay.TitleGlowPlate(gmask, glowScale);
            UiKit.Tag(glow, "빛살");

            // ⓒ 제목 — 노란 굵은 «리워드»
            var title = UiKit.Label(root, Layout.RwTitle.X, Layout.RwTitle.Y, Layout.RwTitle.W, Layout.RwTitle.H,
                                    "리워드", TextSize.Title, Palette.Reward, TextAnchor.MiddleCenter, true, true, TextKind.Title);
            title.name = "RewardTitle"; title.fontStyle = FontStyles.Bold;
            UiKit.Tag(title.transform, "리워드 제목");

            // ⓓ 노란 가로줄 둘 — 그 사이가 «얻은 것» 자리다. 가운데가 밝고 양 끝으로 사라진다(레퍼런스 실측).
            Rule(root, "RewardLineTop", Layout.RwLineTop, "위 노란 줄");
            Rule(root, "RewardLineBottom", Layout.RwLineBottom, "아래 노란 줄");

            // ⓔ 칸 줄 — 정사각 칸을 가운데로 모은다(폭은 개수만큼 · 넘치면 칸을 줄인다).
            var row = UiKit.Rect(root, "RewardCells"); UiKit.Pct(row, Layout.RwCells);
            var cells = Cells(row, items);
            LastCellCount = cells.Count;
            UiKit.TagGroup(row, "보상 칸(" + cells.Count + "칸)", cells.ToArray());
            if (cells.Count > 0) UiKit.Tag(cells[0], "보상 칸(1칸)");   // 표 ㊹ 는 «한 칸» 과 «묶음» 을 따로 잰다(칸 수가 달라져도 «한 칸» 은 안 흔들린다)

            // ⓕ 바닥 «탭하여 닫기»
            var close = UiKit.Label(root, Layout.RwClose.X, Layout.RwClose.Y, Layout.RwClose.W, Layout.RwClose.H,
                                    "탭하여 닫기", TextSize.Body, Palette.White, TextAnchor.MiddleCenter, true, true);
            close.name = "TapToClose"; close.fontStyle = FontStyles.Bold;
            UiKit.Tag(close.transform, "닫기 안내");

            // ⓖ 어둠을 누르면 닫힌다 — punch(눌림 연출)는 끈다: 어둠은 «버튼처럼 보이는 것» 이 아니다(T139 ⓐ 와 같은 호출 꼴).
            //    T269 — 닫기 «직전» 에 파티클을 띄운다: 칸이 살아 있어야 «어디서 튀어나오는가» 를 잴 수 있다(닫으면 칸이 파괴된다).
            UiKit.Clickable(dim, () => { Absorb(app, items, cells); ov.Close(); onClose?.Invoke(); }, false);

            Reveal(title.rectTransform, glow, cells);
        }

        /// <summary>같은 것을 «아이콘 키 → 개수» 로 부르는 짧은 길(대부분의 지급 자리가 이 꼴이다).</summary>
        public static void Show(IDictionary<string, int> gained, Action onClose = null)
        {
            if (gained == null) { LastCellCount = 0; return; }
            var list = new List<Item>();
            foreach (var kv in gained) if (kv.Value > 0) list.Add(Item.Of(kv.Key, UiKit.FmtQty(kv.Value), amount: kv.Value));   // T269 — 수를 아는 자리라 그대로 넘긴다(글자에서 되읽지 않는다)
            Show(list, onClose);
        }

        /// <summary>
        /// T269 — 팝업을 닫는 순간 <b>칸마다 그 아이콘의 파티클</b>이 튀어나와 제자리로 날아간다(주인 2026-09-09 «화면 끄면 이제 해당 재화들 파티클로 돼서 흡수되는 거로»).
        /// <para>
        /// <b>새 연출을 짓지 않는다</b> — 곡선·트레일·시간은 <see cref="RewardOrbs"/>(T109 · 주인이 «랜덤 곡선 · 트레일 · 0.8초» 로 정한 그것) 그대로다.
        /// 다른 것은 셋뿐: 상한 100(주인) · 머무름 거의 0(<see cref="AbsorbHoldSec"/> · 튀어나온 자리가 닫히며 사라진다) · 도착해도 <b>값을 안 더한다</b>(지급은 이미 끝났다 · 연출만).
        /// </para>
        /// <b>층은 지금 화면 안</b>에 둔다 — 화면을 옮기면 그 화면이 꺼지며 날아가던 것도 같이 사라진다(지시서 6항 «화면을 옮기면 즉시 정리» · 앱 층에 두면 새 화면 위로 엉뚱한 구슬이 날아간다).
        /// </summary>
        static void Absorb(App app, IList<Item> items, List<RectTransform> cells)
        {
            LastOrbCount = 0;
            if (app == null || items == null || cells == null || cells.Count == 0) return;
            var layer = Layer(app);
            if (layer == null || _orbs == null) return;
            _orbs.Clear();   // 앞 팝업의 잔여물부터 비운다(연타로 닫아도 겹치지 않는다 · 지시서 6항)

            int n = Mathf.Min(items.Count, cells.Count);
            var want = new int[n]; int total = 0;
            for (int i = 0; i < n; i++) { want[i] = Mathf.Max(1, QtyOf(items[i])); total += want[i]; }
            // 상한을 넘으면 «비율대로» 줄인다(칸마다 최소 하나는 난다) — 반올림으로 넘친 몫은 많은 칸부터 깎는다.
            if (total > MaxOrbs)
            {
                int sum = 0;
                for (int i = 0; i < n; i++) { want[i] = Mathf.Max(1, Mathf.RoundToInt(want[i] * (float)MaxOrbs / total)); sum += want[i]; }
                while (sum > MaxOrbs)
                {
                    int mx = 0; for (int i = 1; i < n; i++) if (want[i] > want[mx]) mx = i;
                    if (want[mx] <= 1) break;
                    want[mx]--; sum--;
                }
            }

            for (int i = 0; i < n; i++)
            {
                var cell = cells[i]; if (cell == null) continue;
                var icon = UiKit.Find(cell, "Icon") as RectTransform;
                var src = icon != null ? icon : cell;
                var target = TargetFor(app, items[i].Icon);
                if (target == null) continue;
                float sizePx = Mathf.Max(16f, src.rect.height);   // 3항 «크기도 팝업 칸 그대로»
                // T313(주인 «흡수 파티클이 느리다 — 1초 안에 전부 흡수») — 예산을 넘긴다. 개수가 적어 이미 예산 안이면 종전 연출 그대로다.
                LastOrbCount += _orbs.Fly(_orbs.TargetPos(src), target, items[i].Icon, Color.white, want[i], want[i], sizePx, 1f, null, AbsorbHoldSec, RewardOrbs.PopupBudgetSec);
            }
        }

        /// <summary>구슬 층 — <b>프레임(<c>app.Frame</c>) 밑 맨 위</b>. 없으면 만든다.
        /// <para>
        /// T354(주인 2026-09-10 «재화 흡수 이펙트가 레이어가 너무 낮음 · 리워드 팝업 닫고 나서 안 보여 · 팝업들보다 레이어가 낮아서») —
        /// 여태는 <b>화면 루트(<c>app.Current.Root</c>)</b> 밑에 세웠다. 그런데 <see cref="Overlay.Root"/> 는 <c>app.Frame</c> 밑 <b>형제</b>(열 때마다 맨 위)라
        /// 화면 루트 «안에서» 맨 위인 것은 오버레이 «아래» 다 — 형제 번호가 아니라 <b>부모가 달랐다</b>. 그래서 프레임 밑으로 올린다
        /// (좌표는 둘 다 프레임 stretch 라 <see cref="RewardOrbs.TargetPos"/> 는 그대로 · 화면이 바뀌어도 층이 살아남는다).
        /// </para>
        /// </summary>
        static RectTransform Layer(App app)
        {
            var host = app.Frame != null ? app.Frame : (app.Current != null ? app.Current.Root : null);
            if (host == null) return null;
            if (_orbLayer == null || _orbLayer.parent != host)
            {
                _orbLayer = UiKit.Rect(host, OrbLayerName); UiKit.Stretch(_orbLayer);
                _orbs = new RewardOrbs(_orbLayer, MaxOrbs);
                _orbSink = null;
            }
            _orbLayer.SetAsLastSibling();
            return _orbLayer;
        }

        /// <summary>이 아이콘이 날아갈 곳 — 골드·다이아는 탑바의 그 pill, 그 밖은 화면 가운데 아래로 사라진다(지시서 4항).</summary>
        static RectTransform TargetFor(App app, string icon)
        {
            string pill = PillFor(icon);
            if (pill != null && app.Current != null && app.Current.Root != null)
            {
                var t = UiKit.Find(app.Current.Root, pill) as RectTransform;
                if (t != null) return t;
            }
            if (_orbSink == null && _orbLayer != null)
            {
                _orbSink = UiKit.Rect(_orbLayer, OrbSinkName);
                UiKit.Pct(_orbSink, 48, 88, 4, 4);
            }
            return _orbSink;
        }

        static void Rule(RectTransform root, string name, Layout.R r, string tag)
        {
            var line = UiKit.Rect(root, name); UiKit.Pct(line, r);
            var img = line.gameObject.AddComponent<Image>();
            img.color = Palette.Reward; img.raycastTarget = false;   // 줄은 장식이다 — 탭은 어둠이 받는다(T227: 위에 덮은 그림이 탭을 먹지 않게)
            UiKit.Tag(line, tag);
        }

        /// <summary>칸을 정사각으로 만들어 가운데로 모은다 — 개수가 많아 줄을 넘치면 칸과 틈을 같은 비로 줄인다.</summary>
        static List<RectTransform> Cells(RectTransform row, IList<Item> items)
        {
            var res = new List<RectTransform>();
            int n = items.Count;
            float cellW = Layout.RwCellW, gap = Layout.RwCellGap;
            float need = n * cellW + (n - 1) * gap;
            if (need > 100f) { float k = 100f / need; cellW *= k; gap *= k; need = 100f; }
            float start = Mathf.Max(0f, (100f - need) * 0.5f);
            for (int i = 0; i < n; i++)
            {
                var it = items[i];
                var cell = UiKit.Rect(row, "RewardCell:" + i);
                UiKit.Pct(cell, start + i * (cellW + gap), 0, cellW, 100);
                var f = UiKit.Spawn(string.IsNullOrEmpty(it.Frame) ? DefaultFrame : it.Frame, cell);
                UiKit.Stretch((RectTransform)f.transform);
                GearUi.DarkFrame(f.transform);   // T115 · 결정 184 — 조각 제 링을 Ink 로 + 가운데 비움 · raycast 끔
                bool qty = !string.IsNullOrEmpty(it.Qty);
                var ic = UiKit.Icon(cell, "Icon", it.Icon);
                UiKit.Pct(ic.rectTransform, qty ? 22 : 16, qty ? 4 : 16, qty ? 56 : 68, qty ? 56 : 68);
                // T396 — 부르는 쪽이 «그림을 직접 세우겠다» 고 하면(메이커 펫 초상) 스프라이트는 끄고 그 자리를 내준다.
                //   자리·크기는 위에서 이미 잡혔으므로 초상은 그 rect 를 그대로 쓴다(칸 문법을 두 벌로 만들지 않는다).
                if (it.Face != null) { ic.enabled = false; it.Face(ic.rectTransform); }
                if (qty) UiKit.Label(cell, 0, 58, 100, 42, it.Qty, TextSize.Aux, Palette.White, kind: TextKind.Aux).fontStyle = FontStyles.Bold;
                res.Add(cell);
            }
            return res;
        }

        /// <summary>
        /// 등장 — 제목이 살짝 튀고(<see cref="UiKit.PopIn"/> = OutBack) 칸이 <b>하나씩</b>(stagger).
        /// 전부 unscaled + <c>SetLink</c>(팝업이 닫혀도 트윈이 먼저 죽는다 · T56).
        /// ⚠ <see cref="UiKit.Reveal"/> 의 셋째 인자는 «지연» 이 아니라 «길이» 다 — 지연은 마스터 시퀀스에 <c>Insert</c> 해서 준다
        /// (<c>Overlay.At</c> 가 쓰는 그 꼴 · 여기서는 그 자리를 못 쓰므로 같은 식으로 직접 만든다).
        /// </summary>
        static void Reveal(RectTransform title, RectTransform glow, List<RectTransform> cells)
        {
            UiKit.PopIn(title, dur: RevealSec);
            if (glow != null) UiKit.PopIn(glow, dur: RevealSec);
            if (cells.Count == 0) return;
            var seq = DOTween.Sequence().SetUpdate(true).SetTarget(cells[0]).SetLink(cells[0].gameObject);
            for (int i = 0; i < cells.Count; i++) seq.Insert(CellStagger * i, UiKit.Reveal(cells[i]));
        }
    }
}
