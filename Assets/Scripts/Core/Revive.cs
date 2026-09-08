namespace KkomaKnight.Core
{
    /// <summary>
    /// <b>부활권</b> — 1개로 전투 한 번 부활 (T254 · 주인 2026-09-09 «부활권 1개로 게임 1회 부활 가능하게 하기»).
    /// <para>
    /// <b>엔진을 안 바꾼다</b>(지시서 2항) — 이 절은 <see cref="BattleState.Dead"/> 를 내리고 체력·실드를 채우는 <b>새 경로</b>일 뿐,
    /// 판정식·난수·시드 골든(T2)은 한 줄도 안 건드린다. 부활을 안 쓰면 지금까지와 완전히 같은 판이다.
    /// </para>
    /// <para>
    /// <b>남기는 것 / 되돌리는 것</b> — 웨이브·진행도·특전·적 상태는 <b>그대로 둔다</b>(주인 «그 자리에서 이어서»).
    /// 되돌리는 것은 «죽었다» 표식과 <b>내 체력·실드</b>뿐이다.
    /// </para>
    /// </summary>
    public static class Revive
    {
        /// <summary>
        /// 한 판에 쓸 수 있는 부활 횟수 — 주인 «게임 <b>1회</b> 부활» 을 그대로 읽은 값이다(결정 698).
        /// 늘리려면 <b>이 한 줄</b>만 고치면 된다(부르는 쪽은 전부 이 상수를 본다).
        /// </summary>
        public const int PerRun = 1;

        /// <summary>지금 부활할 수 있는가 — ⓐ 죽어 있고 ⓑ 부활권이 있고 ⓒ 이 판의 부활 횟수가 아직 남았다.</summary>
        public static bool Can(SaveData s, BattleState g, int usedThisRun)
        {
            if (s == null || g == null) return false;
            if (!g.Dead) return false;          // 클리어·시간 초과로 끝난 판은 부활거리가 아니다
            if (usedThisRun >= PerRun) return false;
            return s.Revive > 0;
        }

        /// <summary>
        /// 부활권 1 을 쓰고 그 자리에서 일으킨다 — 됐으면 true(<paramref name="usedThisRun"/> 가 1 늘어난다).
        /// <para>저장(디스크 쓰기)은 부르는 쪽(게임 층) 몫이다 — 이 절은 순수 C# 이다(<see cref="ArenaTickets.Spend"/> 와 같은 규약).</para>
        /// </summary>
        public static bool Use(SaveData s, BattleState g, ref int usedThisRun)
        {
            if (!Can(s, g, usedThisRun)) return false;
            s.Revive--;
            usedThisRun++;
            g.Dead = false;                     // «죽었다» 만 내린다 — Cleared·T·웨이브·적은 그대로다
            g.P.Hp = g.P.MaxHp;                 // 주인 «HP·실드 가득»
            g.P.Sh = g.P.MaxSh;
            return true;
        }
    }
}
