using System;
using System.IO;
using KkomaKnight.Core;

namespace KkomaKnight.Sim
{
    /// <summary>이식 검증 하니스 진입점. 2단계에서 sim.js 실험1 재현(시드 11·12·13)이 붙는다.</summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            string dir = FindDataDir();
            var d = GameData.LoadFromDirectory(dir);
            Console.WriteLine($"data: {dir}");
            Console.WriteLine($"source {d.Tune.Source} · chapters {d.Enemies.Chapters.Count} · perks {d.Perks.Perks.Count} · gear types {d.Gear.AllTypes.Count}");
            return 0;
        }

        static string FindDataDir()
        {
            var env = Environment.GetEnvironmentVariable("KKOMA_DATA_DIR");
            if (!string.IsNullOrEmpty(env)) return env;
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                var cand = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "data");
                if (File.Exists(Path.Combine(cand, "tune.json"))) return cand;
            }
            throw new DirectoryNotFoundException("Assets/StreamingAssets/data 를 찾을 수 없다");
        }
    }
}
