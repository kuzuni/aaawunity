using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// PvP <b>순위 보상 구간표</b> (<c>Assets/KkomaKnight/arena.json</c> · T237 · 주인 2026-09-08 09:3X «pvp 순위 보상은 등수별로 좀 있게 해 줘»).
    /// <para>
    /// 주인이 준 것은 <b>구간 16개</b>다 — <c>1 · 2 · 3 · 4-5 · 6-10 · 11-20 · 21-50 · 51-100 · 101-200 · 201-500 · 501-1000 · 1001-3000 · 3001-5000 · 5001-10000 · 10001-50000 · 50001~꼴등</c>.
    /// 지금 화면(25 «순위 보상» 팝업)은 <b>네 줄을 코드로 박아</b> 그리고 등수도 <c>i+1</c> 이라 표가 없다 — 이 파일이 그 표다.
    /// </para>
    /// ⚠ <b>보상 «값» 은 주인이 아직 안 줬다</b>(§1 «수치는 주인이 준다» · T99 전례). 그래서 표의 <c>rewards</c> 가 전부 비어 있고
    /// 이 클래스는 «비어 있음» 을 <b>정상</b>으로 읽는다 — 값이 오면 <c>arena.json</c> 만 채우면 되고 여기도 화면도 안 고친다.
    /// <para>값·개수는 전부 파일에서 온다 — 코드 상수 없음(<see cref="ArenaShopData"/>·<see cref="DungeonData"/> 와 같은 문법 · 새 꼴 안 만든다).</para>
    /// </summary>
    public sealed class ArenaRankData
    {
        /// <summary>보상 한 칸 — <c>item</c> 은 게임 쪽 이름이고 아이콘 짝짓기는 화면 몫이다(<c>dungeon.json</c> 이 <c>petEgg</c>·<c>gold</c> 만 적는 것과 같다).</summary>
        public sealed class Reward
        {
            public string Item = "";
            public double Amount;
            public bool Any => Amount > 0;
        }

        /// <summary>등수 구간 한 줄.</summary>
        public sealed class Tier
        {
            /// <summary>화면에 띄우는 글자 — <b>주인이 쓴 그대로</b>다(«4-5» · «50001~꼴등»). 코드가 다시 짓지 않는다.</summary>
            public string Label = "";
            /// <summary>구간의 첫 등수(1 이상).</summary>
            public int From;
            /// <summary>구간의 끝 등수 — <b>0 이면 «꼴등까지»</b>(끝이 없다 · 주인의 «50001~꼴등» 을 수로 지어내지 않는다).</summary>
            public int To;
            public readonly List<Reward> Rewards = new List<Reward>();
            /// <summary>끝이 없는 마지막 구간인가.</summary>
            public bool Open => To <= 0;
            /// <summary>이 등수가 이 구간에 드는가.</summary>
            public bool Has(int rank) => rank >= From && (Open || rank <= To);
            /// <summary>줄 수 있는 것이 하나라도 적혀 있는가 — 지금은 전부 false 다(값 대기).</summary>
            public bool HasRewards
            {
                get { foreach (var r in Rewards) if (r.Any) return true; return false; }
            }
        }

        public readonly List<Tier> Tiers = new List<Tier>();

        /// <summary>이 등수가 드는 구간(못 찾으면 null — 표 밖의 등수).</summary>
        public Tier TierOf(int rank)
        {
            if (rank < 1) return null;
            foreach (var t in Tiers) if (t.Has(rank)) return t;
            return null;
        }

        /// <summary>표에 값이 하나라도 채워졌는가 — false 면 화면이 보상 칸을 «—» 로 낸다(주인 값 대기 상태).</summary>
        public bool AnyRewards
        {
            get { foreach (var t in Tiers) if (t.HasRewards) return true; return false; }
        }

        public static ArenaRankData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static ArenaRankData From(JNode j)
        {
            var d = new ArenaRankData();
            foreach (var e in j["tiers"].Items())
            {
                var t = new Tier
                {
                    Label = e["label"].Str(""),
                    From = (int)e["from"].ReqNum("tiers[].from"),
                    To = (int)e["to"].Num(0),
                };
                if (string.IsNullOrEmpty(t.Label)) throw new FormatException("arena.json: tiers[].label 이 비었다");
                if (t.From < 1) throw new FormatException("arena.json: " + t.Label + ".from 은 1 이상이어야 한다");
                if (t.To != 0 && t.To < t.From) throw new FormatException("arena.json: " + t.Label + ".to 는 from 이상이거나 0(꼴등까지)이어야 한다");
                foreach (var r in e["rewards"].Items())
                {
                    var item = r["item"].Str("");
                    if (string.IsNullOrEmpty(item)) throw new FormatException("arena.json: " + t.Label + ".rewards[].item 이 비었다");
                    double amt = r["amount"].Num(0);
                    if (amt < 0) throw new FormatException("arena.json: " + t.Label + "." + item + " 은 0 이상이어야 한다");
                    t.Rewards.Add(new Reward { Item = item, Amount = amt });
                }
                d.Tiers.Add(t);
            }
            if (d.Tiers.Count == 0) throw new FormatException("arena.json: tiers 가 비어 있다");
            Check(d);
            return d;
        }

        /// <summary>
        /// 구간이 <b>1 부터 빈틈·겹침 없이 이어지고</b> 마지막이 «꼴등까지» 인가 — 표를 손으로 고치다 한 줄을 빠뜨리면
        /// 어떤 등수가 <b>조용히 아무 보상도 못 받는</b> 자리에 떨어진다. 그 사고를 파일을 읽는 순간 잡는다.
        /// </summary>
        static void Check(ArenaRankData d)
        {
            if (d.Tiers[0].From != 1) throw new FormatException("arena.json: 첫 구간은 1 등부터여야 한다(지금 " + d.Tiers[0].From + ")");
            for (int i = 1; i < d.Tiers.Count; i++)
            {
                var prev = d.Tiers[i - 1]; var cur = d.Tiers[i];
                if (prev.Open) throw new FormatException("arena.json: «꼴등까지»(to 0) 구간은 맨 마지막에만 올 수 있다 — " + prev.Label);
                if (cur.From != prev.To + 1)
                    throw new FormatException("arena.json: 구간이 안 이어진다 — " + prev.Label + " 다음은 " + (prev.To + 1) + " 등부터여야 하는데 " + cur.From + " 이다");
            }
            var last = d.Tiers[d.Tiers.Count - 1];
            if (!last.Open) throw new FormatException("arena.json: 마지막 구간은 «꼴등까지»(to 0)여야 한다 — " + last.Label + " 아래 등수가 표 밖으로 떨어진다");
        }
    }
}
