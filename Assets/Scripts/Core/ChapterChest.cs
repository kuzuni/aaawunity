using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 챕터 보상 수치표 (<c>Assets/KkomaKnight/chapterChest.json</c> · T98 · 주인 2026-09-07 «로비 → 클리어 보상» · 레퍼런스 <c>32_lobby_clear.jpg</c>).
    /// 값은 전부 파일에서 온다 — 코드 상수 없음(<see cref="ExpeditionData"/>·<see cref="DailyGiftData"/> 와 같은 방식).
    /// </summary>
    public sealed class ChapterChestData
    {
        /// <summary>다이아 = <see cref="GemBase"/> + <see cref="GemPer"/> × 챕터.</summary>
        public double GemBase, GemPer;
        /// <summary>골드 = 이 배수 × <c>tune.json</c> 의 <c>goldClear(챕터)</c> — 우리 경제와 같은 곡선을 탄다.</summary>
        public double GoldClearMul = 1;

        public static ChapterChestData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static ChapterChestData From(JNode j)
        {
            var d = new ChapterChestData();
            d.GemBase = j["gemBase"].ReqNum("gemBase");
            d.GemPer = j["gemPer"].ReqNum("gemPer");
            d.GoldClearMul = j["goldClearMul"].ReqNum("goldClearMul");
            if (d.GemBase < 0 || d.GemPer < 0) throw new FormatException("chapterChest.json: gemBase·gemPer 는 0 이상이어야 한다");
            if (d.GoldClearMul <= 0) throw new FormatException("chapterChest.json: goldClearMul 은 0 보다 커야 한다");
            return d;
        }
    }

    /// <summary>챕터 보상 한 칸 — 목표(그 챕터의 적 수)와 보상(다이아·골드), 그리고 지금 받을 수 있는지.</summary>
    public struct ChapterChestInfo
    {
        /// <summary>챕터 번호(1부터).</summary>
        public int Chapter;
        /// <summary>목표 = 그 챕터의 적 수(일반 + 보스) — <c>enemies.json</c> 의 <c>enemyCount</c> 그대로.</summary>
        public int Kills;
        public double Gem, Gold;
        /// <summary>그 챕터를 깼는가(= 적을 다 죽였는가).</summary>
        public bool Cleared;
        /// <summary>이미 받았는가.</summary>
        public bool Claimed;
        /// <summary>지금 «받기» 가 눌리는가.</summary>
        public bool Claimable => Cleared && !Claimed;
    }

    /// <summary>
    /// 챕터 보상(Chapter Chest) 규칙 (T98 · 순수 C# · 저장은 <see cref="SaveData.ChestClaimed"/> 한 필드).
    /// <list type="bullet">
    /// <item><b>목표는 «그 챕터의 적을 전부 처치»</b> 이고 그 수는 <c>enemies.json</c> 에서 읽는다 — <b>새 카운터를 만들지 않는다</b>.
    /// 챕터를 깼다는 것이 곧 그 챕터의 적을 다 죽였다는 뜻이라 <c>maxChapter &gt; C</c> 로 판정한다(레퍼런스 32 의 «Kill 37 Enemies on Chapter 30» 과 같은 꼴 · 숫자만 우리 데이터).</item>
    /// <item>한 챕터는 <b>한 번만</b> 받는다 — 받은 챕터 번호를 <see cref="SaveData.ChestClaimed"/> 에 담는다.</item>
    /// <item>보상은 표에서만 온다(<see cref="ChapterChestData"/>) — 코드에 숫자가 없다.</item>
    /// </list>
    /// 재화를 실제로 더하는 것은 게임 층이다(<see cref="Claim"/> 이 준 값을 지갑에 넣는다) — 순수 C# 규칙이라 여기서 지갑을 만지지 않는다.
    /// </summary>
    public static class ChapterChest
    {
        /// <summary>한 챕터의 목표·보상·상태. 챕터 범위를 벗어나면 <c>Chapter = 0</c> 인 빈 값.</summary>
        public static ChapterChestInfo At(GameData D, SaveData s, int chapter)
        {
            if (D == null || s == null || D.ChapterChest == null) return default;
            if (chapter < 1 || chapter > D.Tune.MaxChapter) return default;
            var t = D.ChapterChest;
            return new ChapterChestInfo
            {
                Chapter = chapter,
                Kills = D.Enemies.Chapter(chapter).EnemyCount,
                Gem = Math.Floor(t.GemBase + t.GemPer * chapter),
                Gold = Math.Floor(t.GoldClearMul * D.Tune.GoldClear(chapter)),
                Cleared = s.MaxChapter > chapter,
                Claimed = s.ChestClaimed.Contains(chapter),
            };
        }

        /// <summary>받을 수 있는 챕터가 하나라도 있는가 — 로비 빨간 점(T98 3항 · T96 ⓔ 와 같은 규칙).</summary>
        public static bool AnyClaimable(GameData D, SaveData s)
        {
            if (D == null || s == null || D.ChapterChest == null) return false;
            int last = Math.Min(s.MaxChapter - 1, D.Tune.MaxChapter);
            for (int c = 1; c <= last; c++) if (!s.ChestClaimed.Contains(c)) return true;
            return false;
        }

        /// <summary>받을 수 있는 가장 앞 챕터(없으면 «지금 도전 중인 챕터» = 페이지를 열었을 때 보여 줄 자리).</summary>
        public static int FirstOpen(GameData D, SaveData s)
        {
            if (D == null || s == null) return 1;
            int last = Math.Min(s.MaxChapter - 1, D.Tune.MaxChapter);
            for (int c = 1; c <= last; c++) if (!s.ChestClaimed.Contains(c)) return c;
            return Math.Max(1, Math.Min(s.MaxChapter, D.Tune.MaxChapter));
        }

        /// <summary>
        /// 받는다 — 받을 수 없으면 <c>false</c> 를 돌려주고 아무것도 안 바꾼다(같은 챕터를 두 번 받지 못한다).
        /// 성공하면 세이브에 «받았다» 를 적고 줄 재화를 <paramref name="gem"/>·<paramref name="gold"/> 로 돌려준다(지갑에 넣는 것은 게임 층).
        /// </summary>
        public static bool Claim(GameData D, SaveData s, int chapter, out double gem, out double gold)
        {
            gem = 0; gold = 0;
            var info = At(D, s, chapter);
            if (info.Chapter == 0 || !info.Claimable) return false;
            gem = info.Gem; gold = info.Gold;
            s.ChestClaimed.Add(chapter);
            return true;
        }

        /// <summary>세이브 정리 — 범위 밖 챕터 번호를 버린다(옛 세이브·손댄 파일 방어).</summary>
        public static void Normalize(SaveData s, int maxChapter)
        {
            if (s == null || s.ChestClaimed == null) return;
            var bad = new List<int>();
            foreach (var c in s.ChestClaimed) if (c < 1 || c > maxChapter) bad.Add(c);
            foreach (var c in bad) s.ChestClaimed.Remove(c);
        }
    }
}
