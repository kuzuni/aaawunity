using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>이펙트 도우미 — CFXR 프리팹(카탈로그 fx.*)을 월드/UI 자리에 뿌린다. 프리팹이 없으면 조용히 건너뛴다.</summary>
    public static class Fx
    {
        public const int SortingOrder = 400;   // 캐릭터(≤ 300) 위

        public static GameObject Spawn(string key, Vector3 pos, float scale = 1f, float life = 2.5f, Transform parent = null, bool loop = false)
        {
            var prefab = App.I != null && App.I.Assets != null ? App.I.Assets.Prefab(key) : null;
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, pos, Quaternion.identity, parent);
            go.transform.localScale = prefab.transform.localScale * scale;
            foreach (var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true)) { r.sortingOrder = SortingOrder; }
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (loop) { var m = ps.main; m.loop = true; }
                if (!ps.isPlaying) ps.Play(true);
            }
            if (!loop) Object.Destroy(go, life);
            return go;
        }

    }
}
