using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 신화 위 «표시 등급» 표 (<c>Assets/KkomaKnight/gearTier.json</c> · T316 · 주인 2026-09-09 10:3X
    /// «신화 3강 시 <b>갓</b> · 6강 시 <b>초월</b> · 9강 시 <b>불멸</b> · 12강 시 <b>무한</b>으로 바꾸고, 그 뒤에 걍 계속 무한 —
    /// 즉 <b>신화 13강은 무한 1강</b>임»).
    /// <para>
    /// ⚠ <b>엔진 값은 한 자도 안 바뀐다.</b> 이 표가 정하는 것은 <b>이름·색·표기</b>뿐이고,
    /// 기여(<c>1 + plusStep × Plus</c>)·합성 규칙·정렬(<c>GearScore</c>)은 전부 그대로다(주인 «등급 시스템 더 추가» 이지 «수치» 는 안 줬다).
    /// </para>
    /// </summary>
    public sealed class GearTierData
    {
        /// <summary>표 한 줄 — <c>fromPlus</c> 부터 이 이름·색이 된다.</summary>
        public sealed class Tier
        {
            public string Key = "", Name = "", Color = "";
            /// <summary>이 등급이 시작되는 신화 <c>Plus</c>.</summary>
            public int FromPlus;
            /// <summary>끝이 없는 마지막 등급인가(주인 «그 뒤에 걍 계속 무한»).</summary>
            public bool Open;
        }

        /// <summary><c>fromPlus</c> <b>오름차순</b>(파싱이 보장한다).</summary>
        public readonly List<Tier> Tiers = new List<Tier>();

        public static GearTierData Parse(string json) => From(new JNode(MiniJson.Parse(json)));

        public static GearTierData From(JNode j)
        {
            var d = new GearTierData();
            var list = j["tiers"];
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                var t = new Tier
                {
                    Key = r["key"].Str(""),
                    Name = r["name"].Str(""),
                    Color = r["color"].Str(""),
                    FromPlus = (int)r["fromPlus"].ReqNum("fromPlus"),
                    Open = r["open"].Num() != 0,
                };
                if (t.Name.Length == 0) throw new FormatException("gearTier.json: name 이 비었다 — " + t.Key);
                if (t.Color.Length == 0) throw new FormatException("gearTier.json: color 가 비었다 — " + t.Key);
                if (t.FromPlus <= 0) throw new FormatException("gearTier.json: fromPlus 는 0 보다 커야 한다 — " + t.Key);
                // 오름차순이 아니면 «맞는 줄» 고르기가 조용히 어긋난다 — 화면에는 그저 «등급이 하나 건너뛴다» 로만 보인다.
                if (d.Tiers.Count > 0 && t.FromPlus <= d.Tiers[d.Tiers.Count - 1].FromPlus)
                    throw new FormatException("gearTier.json: fromPlus 는 오름차순이어야 한다 — " + t.Key);
                d.Tiers.Add(t);
            }
            if (d.Tiers.Count == 0) throw new FormatException("gearTier.json: tiers 가 비었다");
            // 마지막 줄이 열려 있지 않으면 그 위 강화는 «이름이 없는 장비» 가 된다(주인 «그 뒤에 걍 계속 무한»).
            if (!d.Tiers[d.Tiers.Count - 1].Open) throw new FormatException("gearTier.json: 마지막 등급은 open 이어야 한다(그 위가 끝없이 이어진다)");
            return d;
        }
    }

    /// <summary>
    /// 표시 등급 규칙 (T316 · 순수 C#) — <b>등급 이름·색을 그리는 모든 자리가 이 하나를 부른다</b>.
    /// <para>
    /// 여태 이름은 <c>GearUi.RarName(D, rar)</c>, 색은 <c>Palette.RarName(rar)</c> 두 곳이 <b>rar 만 보고</b> 정했다.
    /// 표시 등급은 <b>같은 rar(신화) 안에서 Plus 로</b> 갈리므로 그 두 자리는 이제 «장비» 를 받아야 한다 —
    /// 안 그러면 갓·초월·불멸·무한이 전부 «신화» 로 보인다.
    /// </para>
    /// </summary>
    public static class GearTier
    {
        /// <summary>그려야 할 등급 하나 — 이름·색·«+N»·표시 등급인가.</summary>
        public struct Shown
        {
            /// <summary>화면에 띄우는 등급 이름(«신화»·«갓» …).</summary>
            public string Name;
            /// <summary><see cref="Palette"/> 가 아는 색 이름 — <c>redGreen</c> 은 «빨강↔초록 그라데이션» 이라는 뜻이다(<see cref="IsGradient"/>).</summary>
            public string Color;
            /// <summary>이름 옆에 붙는 «+N»(주인 «신화 13강 = 무한 1강» → 표시 등급은 전부 +0 부터 다시 센다).</summary>
            public int Plus;
            /// <summary>신화 위 표시 등급인가(= 표가 고른 줄이 있다). 거짓이면 지금까지와 완전히 같다.</summary>
            public bool IsTier;
            /// <summary>색이 한 색이 아니라 그라데이션인가(무한).</summary>
            public bool IsGradient;
        }

        /// <summary>그라데이션을 뜻하는 색 이름 — 표가 이 낱말을 쓰면 그리는 쪽이 두 색으로 읽는다.</summary>
        public const string GradientColor = "redGreen";

        /// <summary>
        /// 이 장비를 <b>무엇으로 그릴 것인가</b>. 신화가 아니거나 표가 없으면 <b>지금까지와 똑같이</b> 돌려준다
        /// (<paramref name="rarName"/>·<paramref name="rarColor"/> = 부르는 쪽이 여태 쓰던 값).
        /// </summary>
        public static Shown Of(GearTierData d, int rar, int plus, int rarMyth, string rarName, string rarColor)
        {
            var s = new Shown { Name = rarName, Color = rarColor, Plus = plus < 0 ? 0 : plus, IsTier = false, IsGradient = false };
            if (d == null || rar != rarMyth || plus <= 0) return s;
            GearTierData.Tier hit = null;
            for (int i = 0; i < d.Tiers.Count; i++) if (plus >= d.Tiers[i].FromPlus) hit = d.Tiers[i];   // 오름차순이라 마지막으로 맞는 줄이 답이다
            if (hit == null) return s;                                                                   // 신화 +0~+2 — 옛 그대로
            s.Name = hit.Name;
            s.Color = hit.Color;
            s.Plus = plus - hit.FromPlus;
            s.IsTier = true;
            s.IsGradient = hit.Color == GradientColor;
            return s;
        }

        /// <summary>표와 <see cref="GameData"/> 를 아는 짧은 꼴 — 그리는 자리가 이것을 부른다.</summary>
        public static Shown Of(GameData D, GearItem g, string rarName, string rarColor)
            => D == null || g == null
                ? new Shown { Name = rarName, Color = rarColor, Plus = g != null && g.Plus > 0 ? g.Plus : 0 }
                : Of(D.GearTier, g.Rar, g.Plus, D.Gear.RarMyth, rarName, rarColor);
    }
}
