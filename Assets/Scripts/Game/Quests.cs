using System;
using KkomaKnight.Core;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 퀘스트 훅의 <b>단 하나의 입구</b> (T257 2단계 ⓑ) — 게임 코드는 «무슨 일이 일어났다» 만 알리고, 규칙은 <see cref="QuestRun"/> 이 갖는다.
    /// <para>
    /// 훅을 거는 자리마다 <c>Roll</c> → <c>Bump</c> → <c>Persist</c> 를 손으로 쓰면 <b>한 자리만 빠뜨려도</b>
    /// «세긴 세는데 날이 안 바뀐다» 거나 «껐다 켜면 사라진다» 가 조용히 난다. 그래서 그 셋을 여기 한 줄로 묶는다.
    /// </para>
    /// <para>
    /// ⚠ <b>표가 없으면(로드 실패) 아무 일도 안 한다</b> — 세어 봐야 읽을 줄이 없고, 화면도 종전 껍데기 그대로 뜬다.
    /// 게임 흐름을 막지 않는 것이 옳다(<see cref="GameData.Quest"/> 는 못 읽으면 null · <c>Bootstrap</c>).
    /// </para>
    /// </summary>
    public static class Quests
    {
        /// <summary>세는 이름 — <c>quest.json</c> 의 <c>counter</c> 와 <b>같은 글자</b>여야 한다(표가 정본이고 여기는 부르는 쪽이다).</summary>
        public const string Kill = "kill", ChapterTry = "chapterTry", ChestOpen = "chestOpen", GearFuse = "gearFuse",
                            DungeonTry = "dungeonTry", DungeonClear = "dungeonClear",
                            ExpeditionClaim = "expeditionClaim", ExpeditionFastClaim = "expeditionFastClaim",
                            PetUpgrade = "petUpgrade";

        /// <summary>
        /// 사건 하나를 센다 — <b>훅은 이 한 줄이 전부다</b>. 날·주가 넘어갔으면 먼저 밀고(<see cref="QuestRun.Roll"/>) 세고 저장한다.
        /// <paramref name="n"/> 이 0 이하면 아무 일도 안 한다(«0회 합성» 같은 자리에서 부르는 쪽이 안 따져도 되게).
        /// </summary>
        public static void Bump(App app, string counter, int n = 1)
        {
            if (app == null || n <= 0) return;
            var d = app.Data != null ? app.Data.Quest : null;
            var s = app.Save;
            if (d == null || s == null) return;   // 표가 없으면 셀 자리도 뜻이 없다(화면도 종전 껍데기다)
            QuestRun.Roll(s, d, DateTime.Now);
            QuestRun.Bump(s, counter, n);
            app.Persist();
        }

        /// <summary>접속했다 — 일일 «로그인하기» 와 주간 «로그인 5일» 은 세는 규칙이 달라서(하루를 하루로) <see cref="QuestRun.Login"/> 이 따로 있다.</summary>
        public static void Login(App app)
        {
            if (app == null) return;
            var d = app.Data != null ? app.Data.Quest : null;
            var s = app.Save;
            if (d == null || s == null) return;
            QuestRun.Login(s, d, DateTime.Now);
            app.Persist();
        }
    }
}
