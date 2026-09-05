using KkomaKnight.Core;
using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>세이브 저장소 — PlayerPrefs 에 JSON 한 줄 (WebGL 은 IndexedDB · Android 는 SharedPreferences).</summary>
    public static class SaveStore
    {
        public const string Key = "kkoma-knight-v2";
        public static SaveData Load(GameData D)
        {
            string raw = null;
            try { raw = PlayerPrefs.GetString(Key, null); } catch { }
            return SaveData.FromJson(raw, D);
        }
        public static void Save(SaveData s)
        {
            try { PlayerPrefs.SetString(Key, s.ToJson()); PlayerPrefs.Save(); } catch (System.Exception e) { Debug.LogWarning("[SaveStore] 저장 실패: " + e.Message); }
        }
        public static string Today() => System.DateTime.Now.ToString("yyyy-MM-dd");
    }
}
