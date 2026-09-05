using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace KkomaKnight.Game
{
    /// <summary>
    /// StreamingAssets/data/*.json 을 읽어 <see cref="GameData"/> 로 올린다.
    /// WebGL·Android 는 StreamingAssets 가 파일 시스템이 아니라 UnityWebRequest 로 읽는다 — 그래서 코루틴이다.
    /// 에디터·데스크톱은 File 로 바로 읽는다.
    /// </summary>
    public static class DataLoader
    {
        public const string DataFolder = "data";

        public static GameData Loaded { get; private set; }

        public static string PathOf(string file) => Path.Combine(Application.streamingAssetsPath, DataFolder, file);

        static bool NeedsWebRequest()
        {
            var p = Application.streamingAssetsPath;
            return p.Contains("://") || p.Contains(":///");
        }

        public static IEnumerator Load(Action<GameData> onDone, Action<string> onError)
        {
            var texts = new Dictionary<string, string>();
            foreach (var f in GameData.Files)
            {
                string url = PathOf(f);
                if (NeedsWebRequest())
                {
                    using (var req = UnityWebRequest.Get(url))
                    {
                        yield return req.SendWebRequest();
                        if (req.result != UnityWebRequest.Result.Success)
                        {
                            onError?.Invoke($"데이터 로드 실패: {f} — {req.error}");
                            yield break;
                        }
                        texts[f] = req.downloadHandler.text;
                    }
                }
                else
                {
                    string err = null;
                    try { texts[f] = File.ReadAllText(url); }
                    catch (Exception e) { err = $"데이터 로드 실패: {f} — {e.Message}"; }
                    if (err != null) { onError?.Invoke(err); yield break; }
                }
            }
            GameData data = null; string perr = null;
            try { data = GameData.Load(f => texts[f]); }
            catch (Exception e) { perr = "데이터 파싱 실패: " + e.Message; }
            if (perr != null) { onError?.Invoke(perr); yield break; }
            Loaded = data;
            onDone?.Invoke(data);
        }
    }
}
