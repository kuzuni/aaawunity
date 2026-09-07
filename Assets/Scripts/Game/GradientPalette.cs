using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 그라데이션 «두 색» 표(T116 · 주인 2026-09-07 08:4X «그라디안트 레퍼런스 부분 참고해서 <b>더 화려하게 색깔</b> 해줘»).
    /// <para>
    /// 지금 <see cref="UiKit.Gradient"/> 는 위에 흰 α 0.12 / 아래에 잉크 α 0.18 을 덧대는 <b>무채색</b> 방식이라 레퍼런스의 알록달록한 느낌이 안 난다.
    /// 레퍼런스를 <c>tools/ref_color.py</c> 로 실제로 재 보면 규칙이 셋이다(실측값은 <c>catalog.json</c> 의 <c>col.grad.*</c> · 잰 자리는 그 <c>_notes</c>):
    /// <list type="bullet">
    /// <item><b>카드·타일</b>(상점 상품 · 특권 카드 …) = <b>어두운 위 → 밝은 아래</b>, 같은 계열의 <b>두 색</b>(예 다이아 #40116D → #AA0CB8 · 골드 #183D6A → #1683BE).
    /// 지금 헬퍼의 «위 밝음 → 아래 어둠» 과 <b>방향이 반대</b>고, 이 «두 색» 이 주인이 말한 화려함의 정체다.</item>
    /// <item><b>화면 배경</b>(색이 있는 배경 · 로비 초록) = <b>밝은 위 → 어두운 아래</b>(#3C6833 → #315529 · 아주 은은하다).</item>
    /// <item><b>버튼·패널·띠</b> = 레퍼런스에서는 사실상 <b>단색</b>이다(주황 버튼 #FB9F00 · 보라 배너 #6950C8 · 팝업 패널 #2C2829). 덧칠을 세게 하면 오히려 멀어진다.</item>
    /// </list>
    /// </para>
    /// 표에 없는 요소는 <see cref="CardWay"/>·<see cref="BackgroundWay"/> 로 그 요소의 바탕색에서 두 색을 만든다(계열색 유지 · 밝기만 벌린다).
    /// <b>2단계(적용)</b>: <c>UiKit.Gradient</c> 가 «흰/잉크 덧칠» 대신 이 표의 두 색을 tint 로 쓰게 하고, 카드류는 방향을 뒤집는다 —
    /// 그 파일이 지금 다른 워커의 살아 있는 lock(T111·T75) 안이라 1단계에서는 표와 자만 넣는다(ROUTINE §2 T116 · 결정 기록 참조).
    /// </summary>
    public static class GradientPalette
    {
        /// <summary>표의 한 칸 — 위 색과 아래 색.</summary>
        public readonly struct Pair
        {
            public readonly Color Top, Bottom;
            public Pair(Color top, Color bottom) { Top = top; Bottom = bottom; }
        }

        /// <summary>카드류의 밝기 비율(어두운 위 → 밝은 아래) — 실측 평균(다이아 0.41배 · 골드 0.47배 · 특권 0.80배)에서 가운데를 잡았다.</summary>
        public const float CardTopMul = 0.55f, CardBottomMul = 1.10f;
        /// <summary>배경의 밝기 비율(밝은 위 → 어두운 아래) — 로비 실측(#3C6833 → #315529 = 0.83배)에 맞췄다.</summary>
        public const float BgTopMul = 1.00f, BgBottomMul = 0.83f;

        static Color Cat(string key, string fallback) =>
            App.I != null && App.I.Assets != null ? App.I.Assets.Color("col.grad." + key, Palette.Hex(fallback)) : Palette.Hex(fallback);

        /// <summary>실측 표(카탈로그 <c>col.grad.&lt;이름&gt;.top/bottom</c>) — 이름은 <see cref="Names"/>.</summary>
        public static Pair Of(string name)
        {
            switch (name)
            {
                case "cardGem": return new Pair(Cat("cardGem.top", "#40116D"), Cat("cardGem.bottom", "#AA0CB8"));
                case "cardGold": return new Pair(Cat("cardGold.top", "#183D6A"), Cat("cardGold.bottom", "#1683BE"));
                // T100 ⓓ(주인 2026-09-07 08:5X «상자들 카드 부분에도 그라디안트 · 레퍼런스랑 같은 색감») — docs/ref/10_shop_2.jpg 의 카드 오른쪽 띠를 tools/ref_color.py 로 5등분해 잰 값
                case "cardChestLegend": return new Pair(Cat("cardChestLegend.top", "#BA8BFF"), Cat("cardChestLegend.bottom", "#DA15EB"));
                case "cardChestRare": return new Pair(Cat("cardChestRare.top", "#015AB8"), Cat("cardChestRare.bottom", "#18B2E6"));
                case "cardChestEpic": return new Pair(Cat("cardChestEpic.top", "#3F14A1"), Cat("cardChestEpic.bottom", "#C959E1"));
                case "cardBlue": return new Pair(Cat("cardBlue.top", "#50A1E0"), Cat("cardBlue.bottom", "#5CC6F8"));
                // T116 3단계 ⓑ(특권 11 카드 2~4) — docs/ref/11_shop_special.jpg 실측. 이 카드들은 몸통이 «왼 여백은 어둡고 그림 쪽은 밝은» 두 색인데
                // 우리 헬퍼는 세로로만 깔 수 있어 «어두운 위 → 밝은 아래» 로 옮겼다(카드류의 방향 · 결정 357).
                case "cardPrivAd": return new Pair(Cat("cardPrivAd.top", "#1A61C9"), Cat("cardPrivAd.bottom", "#3A91FA"));
                case "cardPrivMonth": return new Pair(Cat("cardPrivMonth.top", "#5115C5"), Cat("cardPrivMonth.bottom", "#7436DE"));
                case "cardPrivLife": return new Pair(Cat("cardPrivLife.top", "#FF6501"), Cat("cardPrivLife.bottom", "#FFB833"));
                case "bgLobby": return new Pair(Cat("bgLobby.top", "#3C6833"), Cat("bgLobby.bottom", "#315529"));
                case "btnBlue": return new Pair(Cat("btnBlue.top", "#188AFA"), Cat("btnBlue.bottom", "#096CFD"));
                case "btnOrange": return new Pair(Cat("btnOrange.top", "#FDA406"), Cat("btnOrange.bottom", "#F09600"));
                case "panelDark": return new Pair(Cat("panelDark.top", "#2C2829"), Cat("panelDark.bottom", "#201E1F"));
                default: return new Pair(Color.white, Color.white);
            }
        }

        /// <summary>표에 든 이름 전부(테스트·감사용 · 순서는 «카드 → 배경 → 버튼 → 패널»).</summary>
        public static readonly string[] Names = { "cardGem", "cardGold", "cardBlue", "cardChestLegend", "cardChestRare", "cardChestEpic", "cardPrivAd", "cardPrivMonth", "cardPrivLife", "bgLobby", "btnBlue", "btnOrange", "panelDark" };

        /// <summary>표에 이름이 있는가.</summary>
        public static bool Has(string name)
        {
            foreach (var n in Names) if (n == name) return true;
            return false;
        }

        /// <summary>표에 없는 <b>카드·타일</b> — 그 칸의 바탕색에서 «어두운 위 → 밝은 아래» 두 색을 만든다(계열색 유지 · 알파는 그대로).</summary>
        public static Pair CardWay(Color baseColor) => new Pair(Mul(baseColor, CardTopMul), Mul(baseColor, CardBottomMul));

        /// <summary>표에 없는 <b>화면 배경</b> — «밝은 위 → 어두운 아래»(레퍼런스 로비와 같은 방향·세기).</summary>
        public static Pair BackgroundWay(Color baseColor) => new Pair(Mul(baseColor, BgTopMul), Mul(baseColor, BgBottomMul));

        static Color Mul(Color c, float m) => new Color(Mathf.Clamp01(c.r * m), Mathf.Clamp01(c.g * m), Mathf.Clamp01(c.b * m), c.a);
    }
}
