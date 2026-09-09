using System;
using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;

namespace KkomaKnight.Tests
{
    /// <summary>테스트용 데이터 로더 — 유니티(에디터 cwd = 프로젝트 루트)와 dotnet(tools/dotnet/Tests/bin/…) 둘 다에서 data 폴더를 찾는다.</summary>
    public static class TestData
    {
        static GameData _cached;
        public static string Dir
        {
            get
            {
                var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
                {
                    var cand = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "data");
                    if (File.Exists(Path.Combine(cand, "tune.json"))) return cand;
                }
                var env = Environment.GetEnvironmentVariable("KKOMA_DATA_DIR");
                if (!string.IsNullOrEmpty(env)) return env;
                throw new DirectoryNotFoundException("Assets/StreamingAssets/data 를 찾을 수 없다 (cwd=" + Directory.GetCurrentDirectory() + ")");
            }
        }
        /// <summary>이 레포가 실제로 도는 표 — 정본 + 덮어쓰기 다섯(<c>Assets/KkomaKnight/*Override.json</c>). 화면·규칙 자는 거의 다 이쪽이다.</summary>
        public static GameData Load() => _cached ?? (_cached = GameData.LoadFromDirectory(Dir));

        static GameData _preBalance;
        /// <summary>
        /// <b>T325 의 밸런스 개편만 빼고</b> 실은 표 — 곧 «주인이 밸런스를 바꾸기 전의 이 레포» 다
        /// (§2 T325 5항이 «T2 시드 골든은 «옛 표» 로 돌리는 자를 따로 둔다» 고 적은 그것).
        /// <para>⚠ <b>정본만 싣는 것이 아니다.</b> <c>combatOverride</c>(T173 · 주인 «창은 화면 넘어서 10 …»)는 <b>남긴다</b> —
        /// 그것은 주인이 바꾼 <b>전투 규칙</b>이고 <b>T2 시드 골든은 그 규칙 위에서 잰 수</b>다.
        /// 실측: 그것까지 빼면 <c>GoldenRate_Seed11_Chapter60_Myth_ThreePick</c> 이 83 → 82 로 어긋난다.</para>
        /// <para>빼는 것은 T325 가 더한 넷뿐이다 — <c>gearOverride</c>(등급·기여) · <c>gachaOverride</c>(상자) ·
        /// <c>tuneOverride</c>(챕터 수) · <c>enemiesOverride</c>(적 곡선). 그것들이 밸런스이고, 이식 동일성에 대해 아무 말도 안 한다.</para>
        /// <para>⚠ <b>«편해서» 이 갈래를 쓰지 마라</b> — 여기로 오는 자는 «aaaw 와 같은 수인가» 를 재는 자뿐이다.
        /// 나머지가 이리로 오면 <b>주인이 시킨 값이 하나도 안 걸린 게임</b>을 재게 된다.</para>
        /// </summary>
        public static GameData PreBalance()
        {
            if (_preBalance != null) return _preBalance;
            var d = GameData.Load(f => File.ReadAllText(Path.Combine(Dir, f)));
            var combat = Path.GetFullPath(Path.Combine(Dir, "..", "..", "KkomaKnight", GameData.CombatOverrideFile));
            if (File.Exists(combat)) d.ApplyCombatOverride(File.ReadAllText(combat));
            d.ValidateOverridden();
            return _preBalance = d;
        }

        /// <summary>
        /// <b>지금 표에 등급 하나를 «전설 바로 앞» 에 끼운</b> 덮어쓰기 JSON — 표가 넷이든 다섯이든 <b>«한 칸 늘어난 표»</b> 를 만든다.
        /// <para>이 레포의 자 여럿이 «등급이 가운데에 하나 늘면 …» 을 재는데, 그것을 <c>[일반, 희귀, 영웅, 전설, 신화]</c> 처럼
        /// <b>손으로 적으면 주인 값이 들어가는 날 그 자들이 통째로 뜻을 잃는다</b>(이미 다섯인 표에 «다섯으로 넓힌다» 는 아무 뜻이 없다).
        /// 그래서 <b>지금 표에서 낸다</b> — 값 커밋 전에도 뒤에도 같은 것을 잰다.</para>
        /// <para>끼우는 자리는 <c>RarLegend</c>(= 전설 바로 앞)다. 새 표에서 그 칸이 «전설 바로 아래» 가 되므로
        /// <c>SaveData.MigrateGearRar</c> 가 쓰는 <c>RarLegend - 1</c> 과 같은 자리를 가리킨다.</para>
        /// <para>칸 값은 <b>아무 수나 안 지어낸다</b> — 기여·옵션은 <b>바로 아래 칸을 복사</b>하고, 그림 칸도 아래 칸을 같이 쓰고(§1 새 그림 0),
        /// 상자 확률은 <b>0</b> 을 끼운다(합 100 이 그대로 선다). 이 자들이 재는 것은 «칸 수와 자리» 이지 값이 아니다.</para>
        /// </summary>
        public static string WidenByOneGrade(GameData D)
        {
            int at = D.Gear.RarLegend;                       // 새 등급이 끼는 자리(전설 바로 앞)
            var names = new List<string>(D.Gear.RarName); names.Insert(at, "새등급");
            string Nums(double[] a) { var l = new List<double>(a); l.Insert(at, a[at - 1]); return "[" + string.Join(", ", l.ConvertAll(v => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture))) + "]"; }
            string Ints(int[] a) { var l = new List<int>(a); l.Insert(at, a[at - 1]); return "[" + string.Join(", ", l) + "]"; }
            var looks = new List<int>();
            for (int r = 0; r < D.Gear.RarName.Length; r++) looks.Add(D.Gear.LookRar(r));
            looks.Insert(at, looks[at - 1]);                 // 새 등급은 아래 칸의 그림을 같이 쓴다(새 그림 0)
            var sb = new System.Text.StringBuilder();
            sb.Append("{ \"rarName\": [");
            for (int i = 0; i < names.Count; i++) { if (i > 0) sb.Append(", "); sb.Append('"').Append(names[i]).Append('"'); }
            sb.Append("], \"rarLegend\": ").Append(D.Gear.RarLegend + 1).Append(", \"rarMyth\": ").Append(D.Gear.RarMyth + 1);
            sb.Append(", \"contribution\": { \"atk\": ").Append(Nums(D.Gear.Atk)).Append(", \"hp\": ").Append(Nums(D.Gear.Hp)).Append(", \"sh\": ").Append(Nums(D.Gear.Sh)).Append(" }");
            sb.Append(", \"optionLadder\": { \"optCount\": ").Append(Ints(D.Gear.OptCountByRar)).Append(" }");
            sb.Append(", \"look\": { \"rarSprite\": [").Append(string.Join(", ", looks)).Append("] } }");
            return sb.ToString();
        }

        /// <summary>위와 짝 — 상자 확률에도 같은 자리에 <c>0</c> 을 끼운 덮어쓰기 JSON(칸 수를 등급 수와 맞춘다).</summary>
        public static string WidenBoxesByOneGrade(GameData D)
        {
            int at = D.Gear.RarLegend;
            var sb = new System.Text.StringBuilder("{ \"boxes\": {");
            bool first = true;
            foreach (var b in D.Gacha.Boxes)
            {
                var l = new List<double>(b.Rate); l.Insert(at, 0);
                if (!first) sb.Append(',');
                first = false;
                sb.Append(" \"").Append(b.Key).Append("\": { \"rate\": [").Append(string.Join(", ", l.ConvertAll(v => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture)))).Append("] }");
            }
            return sb.Append(" } }").ToString();
        }

        /// <summary>레포 루트(= data 폴더의 세 단계 위) 기준 파일 경로 — 이 레포 전용 JSON(Assets/KkomaKnight/shop.json 등)을 읽을 때.</summary>
        public static string RepoFile(string relPath) => Path.GetFullPath(Path.Combine(Dir, "..", "..", "..", relPath));
    }
}
