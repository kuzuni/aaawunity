using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 챕터 보상 수치표 (<c>Assets/KkomaKnight/chapterChest.json</c> · T137 · 주인 2026-09-07 «챕터 보상은 챕터당 3개»
    /// · T268 ⓒ · 주인 2026-09-09 «챕터 100까지 수치 만들어 놓으쇼»).
    /// 값은 전부 파일에서 온다 — 코드 상수 없음(<see cref="ExpeditionData"/>·<see cref="DailyGiftData"/> 와 같은 방식).
    /// <para>보상은 <b>챕터마다 다르다</b> — <see cref="GemAt"/>·<see cref="GoldAt"/> 로만 읽는다.
    /// «표의 첫값» 인 <see cref="Gem1"/>·<see cref="Gold1"/> 을 그대로 화면에 쓰면 전 챕터가 챕터 1 값이 된다(T268 이전의 사고).</para>
    /// </summary>
    public sealed class ChapterChestData
    {
        /// <summary>한 챕터가 갖는 보상 «단» 수(주인 지시 = 3 · 목표는 적 1/3 · 2/3 · 전멸).</summary>
        public int Steps = 1;
        /// <summary>
        /// <b>챕터 1</b> 의 한 단이 주는 다이아·골드(주인이 T137 에 준 값 = 100 · 1000) — 곡선의 시작점일 뿐이다.
        /// <b>어느 챕터의 값이 필요하면 <see cref="GemAt"/>·<see cref="GoldAt"/> 를 쓴다.</b>
        /// </summary>
        public double Gem1, Gold1;
        /// <summary>챕터당 복리 배수(다이아 1.035 · 골드 1.05) — 주인이 곡선만 바꾸고 싶으면 이 두 수만 고친다.</summary>
        public double GemGrowth = 1, GoldGrowth = 1;
        /// <summary>«딱 떨어지는 수» 로 만드는 반올림 단위(다이아 10 · 골드 100 · 주인 지시 ⓒ).</summary>
        public int GemRound = 1, GoldRound = 1;
        /// <summary>표가 덮는 마지막 챕터(= 100) — <b>그 밖 챕터는 이 챕터 값을 그대로</b> 쓴다(결정 기록 참고 · 게임 데이터의 챕터는 420까지 · T1).</summary>
        public int TableMax = 1;

        /// <summary>단 수 상한 — 수치가 아니라 «수령 기록을 int 비트로 담는다» 는 저장 방식의 한계다.</summary>
        public const int MaxSteps = 30;
        /// <summary>«그 챕터를 다 받았다» 를 뜻하는 비트(단 <see cref="Steps"/> 개가 전부 1).</summary>
        public int FullMask => (1 << Steps) - 1;

        /// <summary>챕터 <paramref name="chapter"/> 의 한 단이 주는 다이아(챕터 &lt; 1 이면 0).</summary>
        public double GemAt(int chapter) => ValueAt(Gem1, GemGrowth, GemRound, chapter);
        /// <summary>챕터 <paramref name="chapter"/> 의 한 단이 주는 골드(챕터 &lt; 1 이면 0).</summary>
        public double GoldAt(int chapter) => ValueAt(Gold1, GoldGrowth, GoldRound, chapter);

        /// <summary>
        /// 값(C) = round(첫값 × 복리^(C-1), 반올림단위) · <b>C 는 <see cref="TableMax"/> 에서 멈춘다</b>(표 밖 = 마지막 챕터 값 그대로).
        /// ⚠ 반올림 때문에 복리 증가분이 반올림 단위보다 작은 앞머리 구간은 <b>같은 값이 몇 챕터 이어진다</b> — 줄어들지는 않는다(비감소).
        /// </summary>
        double ValueAt(double first, double growth, int unit, int chapter)
        {
            if (chapter < 1) return 0;
            int c = Math.Min(chapter, TableMax);
            double raw = first * Math.Pow(growth, c - 1);
            return Math.Round(raw / unit, MidpointRounding.AwayFromZero) * unit;
        }

        public static ChapterChestData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static ChapterChestData From(JNode j)
        {
            var d = new ChapterChestData();
            d.Steps = (int)j["steps"].ReqNum("steps");
            d.Gem1 = j["gem1"].ReqNum("gem1");
            d.Gold1 = j["gold1"].ReqNum("gold1");
            d.GemGrowth = j["gemGrowth"].ReqNum("gemGrowth");
            d.GoldGrowth = j["goldGrowth"].ReqNum("goldGrowth");
            d.GemRound = (int)j["gemRound"].ReqNum("gemRound");
            d.GoldRound = (int)j["goldRound"].ReqNum("goldRound");
            d.TableMax = (int)j["tableMax"].ReqNum("tableMax");
            if (d.Steps < 1 || d.Steps > MaxSteps) throw new FormatException("chapterChest.json: steps 는 1~" + MaxSteps + " 여야 한다(수령 기록이 세이브의 비트라서)");
            if (d.Gem1 < 0 || d.Gold1 < 0) throw new FormatException("chapterChest.json: gem1·gold1 은 0 이상이어야 한다");
            // 복리가 1 보다 작으면 «챕터가 오를수록 커진다»(주인 지시 ⓒ)가 뒤집힌다 — 표가 조용히 반대로 도는 것을 막는다.
            if (d.GemGrowth < 1 || d.GoldGrowth < 1) throw new FormatException("chapterChest.json: gemGrowth·goldGrowth 는 1 이상이어야 한다(챕터가 오를수록 커진다 · 주인 지시)");
            if (d.GemRound < 1 || d.GoldRound < 1) throw new FormatException("chapterChest.json: gemRound·goldRound 는 1 이상이어야 한다");
            if (d.TableMax < 1) throw new FormatException("chapterChest.json: tableMax 는 1 이상이어야 한다(표 밖 챕터가 쓰는 값이다)");
            return d;
        }
    }

    /// <summary>챕터 보상 한 칸(= 챕터 하나의 «단» 하나) — 목표·진행도·보상, 그리고 지금 받을 수 있는지.</summary>
    public struct ChapterChestInfo
    {
        /// <summary>챕터 번호(1부터 · 0 이면 «없는 칸»).</summary>
        public int Chapter;
        /// <summary>단 번호(1부터 <see cref="ChapterChestData.Steps"/> 까지 · 마지막 단 = 전멸).</summary>
        public int Step;
        /// <summary>목표 처치 수 = <c>ceil(적 수 × 단 / 단 수)</c>.</summary>
        public int Goal;
        /// <summary>지금 진행도 = 그 챕터에서 잡아 본 최고 처치 수(깬 챕터는 «전멸» 로 친다).</summary>
        public int Kills;
        /// <summary>그 챕터의 적 수(일반 + 보스) — <c>enemies.json</c> 그대로.</summary>
        public int EnemyCount;
        public double Gem, Gold;
        /// <summary>목표를 채웠는가.</summary>
        public bool Reached => Kills >= Goal;
        /// <summary>이미 받았는가.</summary>
        public bool Claimed;
        /// <summary>지금 «받기» 가 눌리는가.</summary>
        public bool Claimable => Chapter != 0 && Reached && !Claimed;
    }

    /// <summary>
    /// 챕터 보상(Chapter Chest) 규칙 (T137 · 순수 C# · 저장은 <see cref="SaveData.ChestClaimed"/>·<see cref="SaveData.ChestKills"/>).
    /// <list type="bullet">
    /// <item><b>한 챕터에 보상 칸이 <see cref="ChapterChestData.Steps"/> 개</b>(주인 지시 = 3) — 목표는 그 챕터 적의 1/3 · 2/3 · 전부 처치이고
    /// 정확히는 <c>ceil(적 수 × 단 / 단 수)</c> 다. 보상은 <b>한 챕터 안에서는</b> 단마다 같고,
    /// <b>챕터가 오를수록 커진다</b>(<see cref="ChapterChestData.GemAt"/>·<see cref="ChapterChestData.GoldAt"/> · T268 ⓒ).</item>
    /// <item>진행도는 «그 챕터에서 잡아 본 최고 처치 수»(<see cref="SaveData.ChestKills"/>) 다 — <b>지든 이기든</b> 남는다.
    /// 이미 깬 챕터(<c>maxChapter &gt; C</c>)는 «전멸» 로 친다(옛 세이브가 손해 보지 않게).</item>
    /// <item>수령은 <b>단마다 한 번</b> — 챕터 → 비트(단 하나가 비트 하나)로 담는다.</item>
    /// <item>수치는 표에서만 온다(<see cref="ChapterChestData"/>) — 코드에 숫자가 없다.</item>
    /// </list>
    /// 재화를 실제로 더하는 것은 게임 층이다(<see cref="Claim"/> 이 준 값을 지갑에 넣는다) — 순수 C# 규칙이라 여기서 지갑을 만지지 않는다.
    /// <para>결정 308(다이아 = 10+3×C · 골드 = goldClear) · 309(«새 처치 카운터를 만들지 않는다»)는 주인 지시가 덮었다 — 결정 기록 참고.</para>
    /// </summary>
    public static class ChapterChest
    {
        /// <summary>옛 세이브(«챕터 하나 = 한 번» 이던 T98 기록)를 뜻하는 표시 — <see cref="Normalize"/> 가 «단 다 받음» 으로 옮긴다.</summary>
        public const int OldSaveAll = -1;

        static int StepsOf(GameData D) => D != null && D.ChapterChest != null ? D.ChapterChest.Steps : 0;
        static int MaxChapterOf(GameData D) => D != null ? D.Tune.MaxChapter : 0;

        /// <summary>페이지에서 오갈 수 있는 마지막 챕터 = 지금 도전 중인 챕터.</summary>
        public static int LastChapter(GameData D, SaveData s)
        {
            if (D == null || s == null) return 1;
            return Math.Max(1, Math.Min(s.MaxChapter, MaxChapterOf(D)));
        }

        /// <summary>그 챕터의 적 수(일반 + 보스) — <c>enemies.json</c> 그대로.</summary>
        public static int EnemyCount(GameData D, int chapter)
        {
            if (D == null || chapter < 1 || chapter > MaxChapterOf(D)) return 0;
            return D.Enemies.Chapter(chapter).EnemyCount;
        }

        /// <summary>단 <paramref name="step"/> 의 목표 = <c>ceil(적 수 × 단 / 단 수)</c>(마지막 단 = 전멸).</summary>
        public static int Goal(GameData D, int chapter, int step)
        {
            int steps = StepsOf(D), n = EnemyCount(D, chapter);
            if (steps <= 0 || n <= 0 || step < 1 || step > steps) return 0;
            return (int)Math.Ceiling((double)n * step / steps);
        }

        /// <summary>그 챕터의 진행도 = 잡아 본 최고 처치 수(깬 챕터는 전멸 · 적 수를 넘지 않는다).</summary>
        public static int Progress(GameData D, SaveData s, int chapter)
        {
            int n = EnemyCount(D, chapter);
            if (n <= 0 || s == null) return 0;
            if (s.MaxChapter > chapter) return n;
            int k = s.ChestKills != null && s.ChestKills.TryGetValue(chapter, out var v) ? v : 0;
            return Math.Max(0, Math.Min(k, n));
        }

        /// <summary>전투가 끝날 때 진행도를 남긴다 — <b>이기든 지든</b> <c>max(기존, 이번 처치)</c>(<c>BattleScreen.EndRun</c> 한 곳).</summary>
        public static void RecordKills(SaveData s, int chapter, int kills)
        {
            if (s == null || chapter < 1 || kills <= 0) return;
            if (s.ChestKills == null) s.ChestKills = new Dictionary<int, int>();
            s.ChestKills[chapter] = s.ChestKills.TryGetValue(chapter, out var old) ? Math.Max(old, kills) : kills;
        }

        /// <summary>그 단을 이미 받았는가.</summary>
        public static bool ClaimedStep(SaveData s, int chapter, int step)
        {
            if (s == null || s.ChestClaimed == null || step < 1 || step > ChapterChestData.MaxSteps) return false;
            if (!s.ChestClaimed.TryGetValue(chapter, out var mask)) return false;
            return mask == OldSaveAll || (mask & (1 << (step - 1))) != 0;
        }

        /// <summary>한 칸(챕터 + 단)의 목표·진행도·보상·상태. 범위를 벗어나면 <c>Chapter = 0</c> 인 빈 값.</summary>
        public static ChapterChestInfo At(GameData D, SaveData s, int chapter, int step)
        {
            if (D == null || s == null || D.ChapterChest == null) return default;
            if (chapter < 1 || chapter > MaxChapterOf(D)) return default;
            if (step < 1 || step > D.ChapterChest.Steps) return default;
            var t = D.ChapterChest;
            return new ChapterChestInfo
            {
                Chapter = chapter,
                Step = step,
                Goal = Goal(D, chapter, step),
                Kills = Progress(D, s, chapter),
                EnemyCount = EnemyCount(D, chapter),
                Gem = t.GemAt(chapter),
                Gold = t.GoldAt(chapter),
                Claimed = ClaimedStep(s, chapter, step),
            };
        }

        // ───────────────────────── 칸 번호(옆으로 스크롤) ─────────────────────────
        /// <summary>칸 번호(0부터) → 챕터·단. 페이지는 이 번호 하나로 «옆으로» 움직인다.</summary>
        public static bool Cell(GameData D, int index, out int chapter, out int step)
        {
            chapter = 0; step = 0;
            int steps = StepsOf(D);
            if (steps <= 0 || index < 0) return false;
            chapter = index / steps + 1; step = index % steps + 1;
            if (chapter > MaxChapterOf(D)) { chapter = 0; step = 0; return false; }
            return true;
        }

        /// <summary>챕터·단 → 칸 번호(0부터).</summary>
        public static int Index(GameData D, int chapter, int step)
        {
            int steps = StepsOf(D);
            if (steps <= 0 || chapter < 1 || step < 1) return 0;
            return (chapter - 1) * steps + (step - 1);
        }

        /// <summary>페이지가 보여 주는 마지막 칸 번호(= 도전 중인 챕터의 마지막 단).</summary>
        public static int LastIndex(GameData D, SaveData s)
        {
            int steps = StepsOf(D);
            if (steps <= 0) return 0;
            return Index(D, LastChapter(D, s), steps);
        }

        /// <summary>한 칸의 정보를 칸 번호로 — 범위 밖이면 빈 값.</summary>
        public static ChapterChestInfo AtIndex(GameData D, SaveData s, int index)
            => Cell(D, index, out var c, out var st) ? At(D, s, c, st) : default;

        // ───────────────────────── 수령·빨간 점 ─────────────────────────
        /// <summary>
        /// 받는다 — 받을 수 없으면 <c>false</c> 를 돌려주고 아무것도 안 바꾼다(같은 단을 두 번 받지 못한다).
        /// 성공하면 세이브에 «그 단을 받았다» 를 적고 줄 재화를 <paramref name="gem"/>·<paramref name="gold"/> 로 돌려준다(지갑은 게임 층).
        /// </summary>
        public static bool Claim(GameData D, SaveData s, int chapter, int step, out double gem, out double gold)
        {
            gem = 0; gold = 0;
            var info = At(D, s, chapter, step);
            if (!info.Claimable) return false;
            gem = info.Gem; gold = info.Gold;
            if (s.ChestClaimed == null) s.ChestClaimed = new Dictionary<int, int>();
            int mask = s.ChestClaimed.TryGetValue(chapter, out var m) ? (m == OldSaveAll ? D.ChapterChest.FullMask : m) : 0;
            s.ChestClaimed[chapter] = mask | (1 << (step - 1));
            return true;
        }

        /// <summary>받을 수 있는 «단» 이 하나라도 있는가 — 로비 빨간 점(T96 ⓔ 와 같은 규칙).</summary>
        public static bool AnyClaimable(GameData D, SaveData s) => FirstClaimable(D, s) >= 0;

        /// <summary>받을 수 있는 가장 앞 칸 번호(없으면 -1).</summary>
        public static int FirstClaimable(GameData D, SaveData s)
        {
            int steps = StepsOf(D);
            if (steps <= 0 || s == null) return -1;
            int last = LastChapter(D, s);
            for (int c = 1; c <= last; c++)
                for (int st = 1; st <= steps; st++)
                    if (At(D, s, c, st).Claimable) return Index(D, c, st);
            return -1;
        }

        /// <summary>페이지를 열 때 보여 줄 칸 — 받을 수 있는 첫 단(없으면 도전 중인 챕터의 못 받은 첫 단 · 그것도 없으면 마지막 단).</summary>
        public static int FirstOpen(GameData D, SaveData s)
        {
            int first = FirstClaimable(D, s);
            if (first >= 0) return first;
            int steps = StepsOf(D);
            if (steps <= 0 || s == null) return 0;
            int cur = LastChapter(D, s);
            for (int st = 1; st <= steps; st++) if (!ClaimedStep(s, cur, st)) return Index(D, cur, st);
            return Index(D, cur, steps);
        }

        /// <summary>
        /// 세이브 정리 — 범위 밖 챕터를 버리고, 옛 세이브(T98 의 «챕터 하나 = 한 번»)를 <b>«단 다 받음»</b> 으로 옮긴다(T137 4항).
        /// 진행도(<see cref="SaveData.ChestKills"/>)도 챕터 범위·적 수로 다듬는다.
        /// </summary>
        public static void Normalize(SaveData s, GameData D)
        {
            if (s == null || D == null) return;
            int maxChapter = MaxChapterOf(D);
            int full = D.ChapterChest != null ? D.ChapterChest.FullMask : 0;
            if (s.ChestClaimed == null) s.ChestClaimed = new Dictionary<int, int>();
            var drop = new List<int>();
            var fix = new List<KeyValuePair<int, int>>();
            foreach (var kv in s.ChestClaimed)
            {
                if (kv.Key < 1 || kv.Key > maxChapter) { drop.Add(kv.Key); continue; }
                if (D.ChapterChest == null) continue;   // 표를 아직 못 읽었으면 손대지 않는다(옛 표시도 그대로 둔다)
                int mask = kv.Value == OldSaveAll ? full : (kv.Value & full);
                if (mask == 0) drop.Add(kv.Key);
                else if (mask != kv.Value) fix.Add(new KeyValuePair<int, int>(kv.Key, mask));
            }
            foreach (var c in drop) s.ChestClaimed.Remove(c);
            foreach (var kv in fix) s.ChestClaimed[kv.Key] = kv.Value;

            if (s.ChestKills == null) s.ChestKills = new Dictionary<int, int>();
            drop.Clear(); fix.Clear();
            foreach (var kv in s.ChestKills)
            {
                int n = EnemyCount(D, kv.Key);
                if (kv.Key < 1 || kv.Key > maxChapter || kv.Value <= 0 || n <= 0) { drop.Add(kv.Key); continue; }
                if (kv.Value > n) fix.Add(new KeyValuePair<int, int>(kv.Key, n));
            }
            foreach (var c in drop) s.ChestKills.Remove(c);
            foreach (var kv in fix) s.ChestKills[kv.Key] = kv.Value;
        }
    }
}
