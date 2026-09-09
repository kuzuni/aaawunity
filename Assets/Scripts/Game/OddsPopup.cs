using System.Text;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 상자 «확률 정보» 팝업 (T267 2단계 · 레퍼런스 <c>36_box_rates_1.jpg</c>·<c>37_box_rates_2.jpg</c> ·
    /// 주인 2026-09-09 «상점에 상자 부분에 인포 버튼 클릭 시 이런 게 떠야 함»).
    /// <para>
    /// 구도(레퍼런스 36): 명판 <b>«확률»</b> → 등급 구간이 <b>세로로</b> 이어진다. 구간 머리 = 등급 이름(그 등급 색) +
    /// «기본 확률(확정 보상 제외): NN.NN%»(퍼센트는 초록) → 그 아래 <b>5열 칸 격자</b>(칸마다 아이콘 + 아래에 개별 확률) →
    /// 길면 세로 스크롤 → 맨 아래 회색 띠 «확정 보상도 같은 확률을 쓴다» → «탭하여 닫기».
    /// </para>
    /// <para>
    /// <b>수는 하나도 안 박혀 있다</b>(지시서 3항) — 구간·개별 확률은 전부 <see cref="GachaOdds"/>(1단계 · 순수 C#)가 낸다:
    /// 등급 확률 = <c>gacha.json</c> 의 그 상자 <c>rate</c> · 아이템 수 = <c>gear.json</c> 의 <c>AllTypes</c> ·
    /// 개별 = 등급 ÷ 아이템 수. <b>그 분모는 «화면에 그릴 칸 수» 가 아니라 «뽑기가 실제로 고르는 목록» 이다</b>(결정 746) —
    /// 그래서 이 화면은 그 목록을 <b>그대로</b> 그린다(칸을 따로 고르지 않는다). 둘이 어긋나면 적힌 확률이 거짓말이 된다.
    /// </para>
    /// 이름 계약(테스트): 상자 <c>OddsBox</c> · 구간 머리 <c>Sec:&lt;등급&gt;</c>(이름 <c>SecName</c> · 확률 <c>SecRate</c>) ·
    /// 칸 <c>Odds:&lt;등급&gt;:&lt;번호&gt;</c>(개별 확률 <c>Pct</c>) · 스크롤 <c>OddsScroll</c> · 바닥 띠 <c>OddsFoot</c>.
    /// </summary>
    public static class OddsPopup
    {
        /// <summary>팝업 상자 자리(화면 %) — 공통 정보 팝업 문법(뽑기 결과와 같은 결). 표 ㊾ 는 이 값을 잰 것이다.</summary>
        static readonly Layout.R RBox = new Layout.R(6f, 22f, 88f, 56f);
        /// <summary>스크롤 창(상자 %) — 머리 명판 아래부터 바닥 띠 위까지.</summary>
        static readonly Layout.R RScroll = new Layout.R(3f, 13f, 94f, 74f);
        /// <summary>바닥 회색 띠(상자 %).</summary>
        static readonly Layout.R RFoot = new Layout.R(3f, 88f, 94f, 9f);

        /// <summary>격자 열 수 — 레퍼런스 36 실측(한 줄에 다섯 칸).</summary>
        const int Cols = 5;
        /// <summary>구간 머리 높이 · 칸 한 줄 높이 · 구간 사이 틈(스크롤 창 폭 대비 px 로 환산해 쓴다).</summary>
        const float HeadPx = 78f, RowPx = 168f, GapPx = 18f;

        /// <summary>퍼센트 글자 — 등급 확률은 «39.68%» 꼴, 개별은 작아서 «1.89%»·«0.189%» 처럼 유효숫자를 하나 더 준다(레퍼런스와 같은 꼴).</summary>
        public static string Pct(double v) => v >= 1.0 ? v.ToString("0.00") + "%" : v.ToString("0.000") + "%";

        /// <summary>연다 — 그 상자의 확률 구간을 그린다. 상자를 못 찾으면 <b>빈 구간</b>이라 «구간 0» 으로 뜬다(안 죽는다 · 결정 746 ⑤).</summary>
        public static RectTransform Open(App app, string boxKey)
        {
            if (app == null) return null;
            var D = app.Data;
            var box = GachaOdds.BoxOf(D, boxKey);
            string title = box != null && !string.IsNullOrEmpty(box.Name) ? box.Name + " 확률" : "확률";
            var b = app.Overlay.OpenBox(UiKit.PopupKeyPlain, "ui.title.tangerine", title, RBox, app.Overlay.Close);
            if (b == null) return null;
            b.name = "OddsBox";

            // 스크롤 창 — 내용은 «구간 머리 + 칸 줄» 을 세로로 이어 붙인 것이라 격자 레이아웃이 아니라 자리로 놓는다.
            var view = UiKit.Rect(b, "OddsScroll"); UiKit.Pct(view, RScroll);
            view.gameObject.AddComponent<RectMask2D>();
            var vimg = UiKit.Ensure<Image>(view.gameObject); vimg.color = new Color(0, 0, 0, 0); vimg.raycastTarget = true;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            var content = UiKit.Rect(view, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            scroll.content = content; scroll.viewport = view;

            var rows = GachaOdds.Of(D, boxKey);
            int n = GachaOdds.ItemCount(D);
            int lines = n > 0 ? (n + Cols - 1) / Cols : 0;
            float y = 0f;
            foreach (var r in rows)
            {
                y += Section(app, D, content, r, y, n, lines, boxKey);
                y += GapPx;
            }
            content.sizeDelta = new Vector2(0f, Mathf.Max(0f, y));

            // 바닥 띠 — 주인 문장(«확정 보상 제외» 와 짝이 되는 안내)
            var foot = UiKit.Panel(b, "OddsFoot", "fr.r12", Palette.A(Palette.Ink, 0.45f)).rectTransform;
            UiKit.Pct(foot, RFoot);
            UiKit.Label(foot, 2, 0, 96, 100, "확정 보상도 같은 확률을 쓴다", TextSize.Aux, Palette.CreamDark).name = "FootText";

            UiKit.Tag(b, "확률 팝업 상자"); UiKit.Tag(view, "확률 목록(스크롤)"); UiKit.Tag(foot, "바닥 안내 띠");
            return b;
        }

        /// <summary>구간 하나(등급 머리 + 칸 격자)를 <paramref name="y"/> 아래에 놓고 그 높이를 돌려준다.</summary>
        static float Section(App app, GameData D, RectTransform content, GachaOddsRow r, float y, int n, int lines, string boxKey)
        {
            string color = Palette.RarName(r.Rar);
            var head = UiKit.Rect(content, "Sec:" + r.Rar);
            Place(head, 0f, y, 100f, HeadPx);
            var hb = UiKit.Panel(head, "HeadBg", "fr.r12", Palette.A(Palette.Ink, 0.35f)).rectTransform; UiKit.Stretch(hb);
            UiKit.Label(head, 2, 0, 26, 100, r.Name, TextSize.Body, Palette.ByName(color), TextAnchor.MiddleLeft).name = "SecName";
            // «기본 확률(확정 보상 제외): NN.NN%» — 퍼센트만 초록(레퍼런스 36 그대로). 리치 텍스트 한 조각이라 T52 의 «섞어 쓰지 마라» 와 다르다:
            // 여기서 색이 갈라 주는 것은 «수» 이고 그 수가 이 줄의 요점이다.
            var sb = new StringBuilder("기본 확률(확정 보상 제외): <color=#3FD214>").Append(Pct(r.Percent)).Append("</color>");
            var rate = UiKit.Label(head, 28, 0, 70, 100, sb.ToString(), TextSize.Aux, Palette.Cream, TextAnchor.MiddleRight);
            rate.name = "SecRate"; rate.richText = true;

            float gy = y + HeadPx;
            for (int i = 0; i < n; i++)
            {
                var t = D.Gear.AllTypes[i];
                var cell = UiKit.Rect(content, "Odds:" + r.Rar + ":" + i);
                float cw = 100f / Cols;
                Place(cell, (i % Cols) * cw, gy + (i / Cols) * RowPx, cw, RowPx);
                var frame = UiKit.Spawn("ui.itemFrame." + color, cell);
                var frt = (RectTransform)frame.transform; UiKit.Pct(frt, 6, 2, 88, 66);
                UiKit.SetSprite(frt, "Item", GearLook.IconKey(t.Part, D.Gear.SetOf(t.Type), r.Rar), Palette.White);
                UiKit.Label(cell, 0, 70, 100, 26, Pct(r.Each), TextSize.Aux, Palette.White).name = "Pct";
                // 4항 — 칸을 누르면 «보기 전용» 세부 팝업. 닫으면 **이 팝업으로 돌아온다**(프로필 팝업 둘이 쓰는 그 꼴 · 표 ㉟).
                //   Overlay 는 한 겹이라 «겹쳐 뜨기» 가 아니라 «갔다 돌아오기» 로 같은 결과를 낸다(결정 기록).
                var item = new GearItem { Part = t.Part, Type = t.Type, Rar = r.Rar, Plus = 0 };
                string key = boxKey;
                UiKit.Clickable(cell, () => GearUi.OpenInfo(app, item, () => Open(app, key)));
            }
            return HeadPx + lines * RowPx;
        }

        /// <summary>스크롤 Content 안의 자리 — x·w 는 %, y·h 는 px(위에서 아래로 쌓는다).</summary>
        static void Place(RectTransform rt, float xPct, float yPx, float wPct, float hPx)
        {
            rt.anchorMin = new Vector2(xPct / 100f, 1f); rt.anchorMax = new Vector2((xPct + wPct) / 100f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -(yPx + hPx)); rt.offsetMax = new Vector2(0f, -yPx);
        }
    }
}
