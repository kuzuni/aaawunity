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
        /// <summary>
        /// «데이터 삭제»(T29 · 주인 2026-09-06) — PlayerPrefs 의 세이브 키를 지우고 <b>새 세이브</b>(골드 0 · 장비 0 · 챕터 1 · 배속 x1 · 음소거 해제 — 설정값도 초기화)를 돌려준다.
        /// 지운 뒤 바로 쓰지는 않는다(다음 <see cref="Save"/> 가 새 세이브를 쓴다). 호출은 <see cref="App.ResetSave"/> 한 곳.
        /// </summary>
        public static SaveData Reset(GameData D)
        {
            try { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); } catch (System.Exception e) { Debug.LogWarning("[SaveStore] 삭제 실패: " + e.Message); }
            return SaveData.FromJson(null, D);
        }
        public static string Today() => System.DateTime.Now.ToString("yyyy-MM-dd");
    }
}
