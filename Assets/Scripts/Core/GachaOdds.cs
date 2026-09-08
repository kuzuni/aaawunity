using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>«확률 정보» 팝업의 한 구간 = 등급 하나(T267 · 레퍼런스 <c>36_box_rates_1.jpg</c>).</summary>
    public struct GachaOddsRow
    {
        /// <summary>등급 인덱스(0 일반 · 1 희귀 · 2 전설 · 3 신화 — <c>gear.json</c> 의 <c>rarName</c> 차례).</summary>
        public int Rar;
        /// <summary>등급 이름 — 표(<c>gear.json rarName</c>)에서 온다(코드에 «희귀» 같은 글자를 박지 않는다).</summary>
        public string Name;
        /// <summary>그 등급이 나올 기본 확률(%) — <c>gacha.json</c> 의 그 상자 <c>rate</c> 그대로다.</summary>
        public double Percent;
        /// <summary>그 등급에서 나올 수 있는 아이템 수(부위 × 세트).</summary>
        public int Count;
        /// <summary>아이템 하나가 나올 확률(%) = <see cref="Percent"/> ÷ <see cref="Count"/>.</summary>
        public double Each;
    }

    /// <summary>
    /// 상자 «확률 정보» 계산 (T267 1단계 · 주인 2026-09-09 «상점에 상자 부분에 인포 버튼 클릭 시 이런 게 떠야 함» · 레퍼런스 36·37).
    /// <para>
    /// <b>수를 하나도 안 박는다</b>(지시서 3항) — 등급 확률은 <c>gacha.json</c> 의 그 상자 <c>rate</c>,
    /// 아이템 수는 <c>gear.json</c> 의 <see cref="GearData.AllTypes"/>(부위 × 세트) 다. 확률표가 바뀌면 화면이 같이 바뀐다.
    /// </para>
    /// <para>
    /// <b>«등급 하나에 아이템이 몇 종인가» 를 왜 <see cref="GearData.AllTypes"/> 로 세는가</b> —
    /// 뽑기가 실제로 그렇게 고르기 때문이다: <c>GearSystem</c> 의 <c>Mk(rr)</c> 이 등급을 먼저 굴린 뒤
    /// 종류는 <b>AllTypes 에서 고르게(균등)</b> 뽑는다. 즉 등급마다 나올 수 있는 목록이 같고, 개별 확률은 등급 확률을 그 수로 나눈 값이다.
    /// 레퍼런스도 같은 꼴이다(Rare 39.68% ÷ 21종 ≈ 1.89% — 종 수만 그 게임 것이다).
    /// <b>이 셈이 뽑기 코드와 어긋나면 화면이 거짓말을 한다</b> — 그래서 «AllTypes 로 센다» 를 자가 직접 잰다.
    /// </para>
    /// 차례는 <b>높은 등급부터</b>다(레퍼런스 36 에서 보라 구간이 파랑 구간 위에 있다 · 실측).
    /// <c>rate</c> 가 0 인 등급은 <b>구간 자체를 안 낸다</b> — 희귀 상자에 «전설 0.00%» 를 그리면 있을 수 있는 것처럼 읽힌다.
    /// </summary>
    public static class GachaOdds
    {
        /// <summary>그 상자의 구간 목록(높은 등급부터 · <c>rate</c> 0 인 등급은 뺀다). 표가 없거나 상자를 못 찾으면 빈 목록.</summary>
        public static List<GachaOddsRow> Of(GameData d, string boxKey)
        {
            var rows = new List<GachaOddsRow>();
            var box = BoxOf(d, boxKey);
            if (box == null || box.Rate == null) return rows;
            int count = ItemCount(d);
            var names = d != null && d.Gear != null ? d.Gear.RarName : null;
            for (int r = box.Rate.Length - 1; r >= 0; r--)
            {
                double p = box.Rate[r];
                if (p <= 0) continue;                       // 없는 등급은 구간을 안 낸다
                rows.Add(new GachaOddsRow
                {
                    Rar = r,
                    Name = names != null && r < names.Length ? names[r] : r.ToString(),
                    Percent = p,
                    Count = count,
                    Each = count > 0 ? p / count : 0,
                });
            }
            return rows;
        }

        /// <summary>한 등급에서 나올 수 있는 아이템 수 — 뽑기가 고르는 그 목록(<see cref="GearData.AllTypes"/>)이다.</summary>
        public static int ItemCount(GameData d) => d != null && d.Gear != null && d.Gear.AllTypes != null ? d.Gear.AllTypes.Count : 0;

        /// <summary>구간 확률의 합(%) — 표가 성하면 100 이다(부동소수 오차는 부르는 쪽이 감안한다).</summary>
        public static double TotalPercent(GameData d, string boxKey)
        {
            double sum = 0;
            foreach (var r in Of(d, boxKey)) sum += r.Percent;
            return sum;
        }

        /// <summary>그 상자 — 없으면 <c>null</c>.</summary>
        public static GachaBox BoxOf(GameData d, string boxKey)
        {
            if (d == null || d.Gacha == null || d.Gacha.Boxes == null || string.IsNullOrEmpty(boxKey)) return null;
            // GachaData.Box(key) 는 없으면 던진다 — 화면이 «없는 상자» 를 물어도 팝업이 죽지 않게 여기서는 null 로 돌려준다.
            foreach (var b in d.Gacha.Boxes) if (b.Key == boxKey) return b;
            return null;
        }
    }
}
