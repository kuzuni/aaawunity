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
        public static GameData Load() => _cached ?? (_cached = GameData.LoadFromDirectory(Dir));
        /// <summary>레포 루트(= data 폴더의 세 단계 위) 기준 파일 경로 — 이 레포 전용 JSON(Assets/KkomaKnight/shop.json 등)을 읽을 때.</summary>
        public static string RepoFile(string relPath) => Path.GetFullPath(Path.Combine(Dir, "..", "..", "..", relPath));
    }
}
