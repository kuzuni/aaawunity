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
            public static Item Of(string icon, string qty = null, string frame = null) => new Item { Icon = icon, Qty = qty, Frame = frame };
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
            UiKit.LightBehind(glow, null, UiKit.LightKeySmall, UiKit.LightPeriod, Palette.A(Palette.Reward, GlowAlpha),
                              sidePx: UiKit.FrameW * GlowSide, clip: false);
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
            UiKit.Clickable(dim, () => { ov.Close(); onClose?.Invoke(); }, false);

            Reveal(title.rectTransform, glow, cells);
        }

        /// <summary>같은 것을 «아이콘 키 → 개수» 로 부르는 짧은 길(대부분의 지급 자리가 이 꼴이다).</summary>
        public static void Show(IDictionary<string, int> gained, Action onClose = null)
        {
            if (gained == null) { LastCellCount = 0; return; }
            var list = new List<Item>();
            foreach (var kv in gained) if (kv.Value > 0) list.Add(Item.Of(kv.Key, UiKit.FmtQty(kv.Value)));
            Show(list, onClose);
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
