using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 아레나 <b>한 판의 규칙표</b> (<c>Assets/KkomaKnight/arenaMatch.json</c> · T240 · 주인 2026-09-08 11:2X
    /// «이기면 승점 올라가고 순위 올라가고 그런 느낌, 지면 승점 떨어지고»).
    /// <para>
    /// <b>주인이 준 것</b> = 승 <c>+8</c> · 패 <c>-6</c>(레퍼런스 <c>34_pvp_win.jpg</c> 의 숫자 그대로) · «0 미만으로는 안 내려간다».
    /// <b>워커가 정한 것</b> = 시작 승점 0 과 티어 여섯 구간(ROUTINE §2 T240 5항이 «승점 구간으로 정하고» 로 맡긴 자리 · 결정 659).
    /// </para>
    /// 값·개수는 전부 파일에서 온다 — 코드 상수 없음(<see cref="ArenaRankData"/>·<see cref="ArenaShopData"/> 와 같은 문법 · 새 꼴 안 만든다).
    /// </summary>
    public sealed class ArenaMatchData
    {
        /// <summary>티어 한 줄 — <c>from</c> 승점부터 이 이름이다(다음 줄의 <c>from</c> 직전까지).</summary>
        public sealed class Tier
        {
            /// <summary>결과 화면 명판에 뜨는 글자(레퍼런스 34 의 «Silver Battle» 자리).</summary>
            public string Name = "";
            /// <summary>이 티어가 시작하는 승점.</summary>
            public double From;
        }

        /// <summary>이겼을 때 더하는 승점(양수).</summary>
        public double Win = 8;
        /// <summary>졌을 때 더하는 승점(<b>음수</b> — 표에 «-6» 으로 적혀 있고 코드는 부호를 뒤집지 않는다).</summary>
        public double Lose = -6;
        /// <summary>승점 바닥 — 주인 «0 미만으로는 안 내려간다».</summary>
        public double MinScore;
        /// <summary>새 세이브의 시작 승점.</summary>
        public double StartScore;
        /// <summary>승점 오름차순 티어 구간(첫 줄의 <c>From</c> 은 <see cref="MinScore"/> 와 같다).</summary>
        public readonly List<Tier> Tiers = new List<Tier>();

        public static ArenaMatchData Parse(string json) => From(new JNode(MiniJson.Parse(json)));

        public static ArenaMatchData From(JNode j)
        {
            var m = new ArenaMatchData();
            m.Win = j["win"].ReqNum("win");
            m.Lose = j["lose"].ReqNum("lose");
            m.MinScore = j["minScore"].Num();
            m.StartScore = j["startScore"].Num();
            foreach (var t in j["tiers"].Items())
                m.Tiers.Add(new Tier { Name = t["name"].Str(""), From = t["from"].ReqNum("tiers[].from") });

            // 이 표의 사고는 조용하다 — 한 줄만 어긋나도 «승점은 도는데 이름이 안 뜬다» 로만 보인다. 읽는 순간 우는 편이 낫다(T237 전례).
            if (m.Win <= 0) throw new FormatException("arenaMatch.json: win 은 양수여야 한다(이기면 오른다)");
            if (m.Lose >= 0) throw new FormatException("arenaMatch.json: lose 는 음수여야 한다(지면 내린다)");
            if (m.StartScore < m.MinScore) throw new FormatException("arenaMatch.json: startScore 는 minScore 이상이어야 한다");
            if (m.Tiers.Count == 0) throw new FormatException("arenaMatch.json: tiers 가 비어 있다");
            if (m.Tiers[0].From != m.MinScore) throw new FormatException("arenaMatch.json: 첫 티어의 from 은 minScore 와 같아야 한다(바닥에 이름이 없으면 안 된다)");
            for (int i = 0; i < m.Tiers.Count; i++)
            {
                if (string.IsNullOrEmpty(m.Tiers[i].Name)) throw new FormatException("arenaMatch.json: tiers[" + i + "].name 이 비었다");
                if (i > 0 && m.Tiers[i].From <= m.Tiers[i - 1].From) throw new FormatException("arenaMatch.json: tiers 의 from 은 오름차순이어야 한다");
            }
            return m;
        }
    }

    /// <summary>
    /// 아레나 한 판의 <b>승점·순위·티어</b> 셈 (T240 5항 · 순수 C# · 결정적 · 서버 없음).
    /// <list type="bullet">
    /// <item><b>승점</b> — 이기면 <c>+win</c> · 지면 <c>+lose</c>(음수) · <c>minScore</c> 아래로 안 내려간다.</item>
    /// <item><b>순위</b> — 주인 문장 그대로 «내 승점보다 높은 더미 수 + 1». 더미 승점은 <see cref="ArenaDummy.Score"/> 가 순위마다 내는 <b>고정</b> 값이고
    /// <b>내 승점만 움직인다</b>. 그래서 이기면 순위가 오르고 지면 내린다 — 별도의 «순위 테이블» 을 두지 않는다(두면 승점과 어긋날 자리가 생긴다).</item>
    /// <item><b>티어</b> — 승점 구간의 이름. 표에서 온다.</item>
    /// </list>
    /// ⚠ <b>이 파일에 전투는 없다</b> — T240 1·2·4항(콜로세움 무대·양쪽 플레이어·결과 화면)은 <c>EventsScreen.cs</c> 안이라
    /// 그 파일의 lock(T236)이 풀린 뒤 배선 회차에서 붙인다. 여기는 «값이 없어도 세울 수 있는 절반» 이다(T237 ⓐⓑ 와 같은 순서).
    /// </summary>
    public static class ArenaMatch
    {
        /// <summary>한 판의 결과를 승점에 반영한다 — 바닥(<see cref="ArenaMatchData.MinScore"/>)에서 멈춘다.</summary>
        public static double Apply(ArenaMatchData m, double score, bool win)
        {
            if (m == null) return score;
            double v = score + (win ? m.Win : m.Lose);
            return v < m.MinScore ? m.MinScore : v;
        }

        /// <summary>이 판으로 실제로 움직인 승점(화면의 «+8» / «−6» 은 이 값이다 — 바닥에 걸리면 −6 이 아니라 그만큼만 줄었다고 적어야 정직하다).</summary>
        public static double Delta(ArenaMatchData m, double score, bool win) => Apply(m, score, win) - score;

        /// <summary>
        /// 더미가 승점 바닥에 닿는 순위 — 그 아래는 전부 같은 값이라 더 세어 봐야 아무것도 안 바뀐다(무한 탐색을 막는 상한).
        /// 순위마다 <c>stepMin</c> 이상 빠지므로 <c>(top − min) / stepMin</c> 걸음이면 반드시 바닥이다.
        /// </summary>
        static int SearchCap(ArenaDummyData d)
        {
            double span = d.ScoreTop - d.ScoreMin;
            if (span <= 0 || d.ScoreStepMin <= 0) return 1;
            return (int)Math.Ceiling(span / d.ScoreStepMin) + 2;
        }

        /// <summary>내 순위 = <b>«내 승점보다 높은 더미 수 + 1»</b>(주인 문장 그대로 · 1 이 가장 높다).</summary>
        public static int RankOf(ArenaDummyData d, double score)
        {
            if (d == null) return 1;
            int above = 0, cap = SearchCap(d);
            for (int r = 1; r <= cap; r++)
            {
                if (ArenaDummy.Score(d, r) <= score) break;   // 더미 승점은 순위가 내려갈수록 단조 감소한다
                above++;
            }
            return above + 1;
        }

        /// <summary>승점 구간의 티어 줄(표가 비었으면 null — 파싱이 그 꼴을 막지만 호출부가 null 을 봐도 안 깨지게 한다).</summary>
        public static ArenaMatchData.Tier TierRowOf(ArenaMatchData m, double score)
        {
            if (m == null || m.Tiers.Count == 0) return null;
            var hit = m.Tiers[0];
            for (int i = 0; i < m.Tiers.Count; i++) if (score >= m.Tiers[i].From) hit = m.Tiers[i];
            return hit;
        }

        /// <summary>승점 구간의 티어 이름(결과 화면 명판 · 없으면 빈 글자).</summary>
        public static string TierOf(ArenaMatchData m, double score)
        {
            var t = TierRowOf(m, score);
            return t == null ? "" : t.Name;
        }

        /// <summary>둘 중 «더 높은» 순위(수가 작은 쪽) — 0 은 «아직 없다» 라 상대가 이긴다(세이브의 최고 순위에 쓴다).</summary>
        public static int BetterRank(int a, int b)
        {
            if (a <= 0) return b;
            if (b <= 0) return a;
            return a < b ? a : b;
        }
    }
}
