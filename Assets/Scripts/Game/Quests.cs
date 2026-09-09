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
            var s = app.Save;
            if (s == null) return;
            var d = app.Data != null ? app.Data.Quest : null;
            if (d != null)   // 퀘스트 표가 없어도 업적은 센다 — 둘은 표가 따로다(한쪽이 못 실려도 다른 쪽이 멎을 까닭이 없다)
            {
                QuestRun.Roll(s, d, DateTime.Now);
                QuestRun.Bump(s, counter, n);
            }
            string ach = AchName(counter);
            if (ach != null) Achievement.Add(s, ach, n);   // T258 3항 — 같은 사건을 업적의 «누적(평생)» 에도 담는다
            app.Persist();
        }

        /// <summary>
        /// 퀘스트 이름 → 업적 이름 (T258 3항) — <b>같은 사건인데 이름이 1:1 이 아니다</b>.
        /// <para>
        /// 그대로 쓰는 넷(<c>kill</c>·<c>gearFuse</c>·<c>expeditionClaim</c>·<c>petUpgrade</c>) 과 이름만 다른 하나
        /// (<c>expeditionFastClaim</c> → <c>expeditionQuick</c>)만 여기서 잇는다. <b>나머지는 일부러 안 잇는다</b> —
        /// <c>chestOpen</c> 은 업적에서 등급 셋으로, <c>dungeonTry</c> 는 던전 둘로 <b>갈라지므로</b> 여기서는 어느 쪽인지 알 길이 없다.
        /// 모르는 채로 «가장 그럴듯한 쪽» 에 담으면 셈이 조용히 틀린다 — 그 둘은 부르는 쪽이 <see cref="Ach"/> 로 직접 이름을 댄다.
        /// </para>
        /// <c>chapterTry</c>·<c>dungeonClear</c>·<c>chestOpen</c>·<c>login</c> 처럼 <b>업적 표에 아예 없는</b> 이름은 null 이다(담을 자리가 없다).
        /// </summary>
        static string AchName(string counter)
        {
            if (counter == Kill || counter == GearFuse || counter == ExpeditionClaim || counter == PetUpgrade) return counter;
            if (counter == ExpeditionFastClaim) return AchExpeditionQuick;
            return null;
        }

        /// <summary>
        /// 업적만 세는 사건 (T258 3항) — 퀘스트 표에 그 줄이 <b>없거나</b>, 있어도 <b>이름이 갈라지는</b>(등급·던전별) 자리에서 부른다.
        /// <see cref="Bump"/> 와 갈라 둔 까닭은 «두 번 세는 덫» 을 막기 위해서다 — 한 사건에 둘 다 부르면 업적이 두 번 오른다.
        /// </summary>
        public static void Ach(App app, string counter, int n = 1)
        {
            if (app == null || n <= 0 || string.IsNullOrEmpty(counter)) return;
            var s = app.Save; if (s == null) return;
            Achievement.Add(s, counter, n);
            app.Persist();
        }

        /// <summary>업적 «출석»(<see cref="Achievement.DailyOnce"/>) 처럼 <b>하루 한 번만</b> 오르는 것(주인 명시) — 같은 날 두 번 불러도 한 번이다.</summary>
        public static void AchOncePerDay(App app, string counter)
        {
            if (app == null || string.IsNullOrEmpty(counter)) return;
            var s = app.Save; if (s == null) return;
            Achievement.AddOncePerDay(s, counter, DateTime.Now.ToString("yyyy-MM-dd"));
            app.Persist();
        }

        /// <summary>업적 전용 이름 — <c>achievement.json</c> 의 <c>counter</c> 와 <b>같은 글자</b>여야 한다(표가 정본이다).</summary>
        public const string AchChestRare = "chestOpenRare", AchChestEpic = "chestOpenEpic", AchChestMythic = "chestOpenMythic",
                            AchAdWatch = "adWatch", AchDungeonHell = "dungeonTryHell", AchDungeonExpd = "dungeonTryExpd",
                            AchExpeditionQuick = "expeditionQuick", AchChapterChestClaim = "chapterChestClaim",
                            AchAttendClaim = "attendClaim", AchGiftClaim = "giftClaim",
                            AchPetGacha = "petGacha", AchArenaTry = "arenaTry";

        /// <summary>접속했다 — 일일 «로그인하기» 와 주간 «로그인 5일» 은 세는 규칙이 달라서(하루를 하루로) <see cref="QuestRun.Login"/> 이 따로 있다.</summary>
        public static void Login(App app)
        {
            if (app == null) return;
            var d = app.Data != null ? app.Data.Quest : null;
            var s = app.Save;
            if (s == null) return;
            if (d != null) QuestRun.Login(s, d, DateTime.Now);
            // T258 — 업적 «출석 1회» 도 여기서 오른다(주인 «출석은 하루 한 번만»). 같은 사건이라 부르는 자리를 둘로 두지 않는다.
            Achievement.AddOncePerDay(s, Achievement.DailyOnce, DateTime.Now.ToString("yyyy-MM-dd"));
            app.Persist();
        }
    }
}
