using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>이펙트 생성 도우미 — CFXR 프리팹(카탈로그 키)을 위치에 뿌리고 수명 뒤 파괴한다. 프리팹이 없으면 조용히 건너뛴다.</summary>
    public static class Fx
    {
        public static GameObject Spawn(string key, Vector3 pos, float scale = 1f, float life = 2.5f, Transform parent = null)
        {
            var prefab = App.I != null && App.I.Assets != null ? App.I.Assets.Prefab(key) : null;
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, pos, Quaternion.identity, parent);
            go.transform.localScale = prefab.transform.localScale * scale;
            var ps = go.GetComponentInChildren<ParticleSystem>();
            if (ps != null && !ps.isPlaying) ps.Play(true);
            Object.Destroy(go, life);
            return go;
        }
    }
}
