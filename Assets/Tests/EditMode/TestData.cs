using System;
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

        /// <summary>레포 루트(= data 폴더의 세 단계 위) 기준 파일 경로 — 이 레포 전용 JSON(Assets/KkomaKnight/shop.json 등)을 읽을 때.</summary>
        public static string RepoFile(string relPath) => Path.GetFullPath(Path.Combine(Dir, "..", "..", "..", relPath));
    }
}
