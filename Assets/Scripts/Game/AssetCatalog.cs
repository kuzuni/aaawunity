using System;
using System.Collections.Generic;
using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 주인 에셋(Layer Lab · CFXR · …)을 키 이름으로 잡는 카탈로그. 에디터 없이 작업하므로
    /// `Assets/KkomaKnight/AssetCatalog.asset` 은 `tools/gen_catalog.py` 가 `Assets/KkomaKnight/catalog.json`(용도 → 에셋 경로)
    /// 에서 GUID·fileID 를 읽어 YAML 로 생성한다. 같은 스크립트가 `docs/assets-map.md` 표도 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "KkomaKnight/Asset Catalog")]
    public sealed class AssetCatalog : ScriptableObject
    {
        [Serializable] public struct SpriteEntry { public string key; public Sprite sprite; }
        [Serializable] public struct PrefabEntry { public string key; public GameObject prefab; }
        [Serializable] public struct ControllerEntry { public string key; public RuntimeAnimatorController controller; }
        [Serializable] public struct MaterialEntry { public string key; public Material material; }
        [Serializable] public struct FontEntry { public string key; public Font font; }
        [Serializable] public struct ColorEntry { public string key; public Color color; }

        public List<SpriteEntry> sprites = new List<SpriteEntry>();
        public List<PrefabEntry> prefabs = new List<PrefabEntry>();
        public List<ControllerEntry> controllers = new List<ControllerEntry>();
        public List<MaterialEntry> materials = new List<MaterialEntry>();
        public List<FontEntry> fonts = new List<FontEntry>();
        public List<ColorEntry> colors = new List<ColorEntry>();

        Dictionary<string, Sprite> _s; Dictionary<string, GameObject> _p; Dictionary<string, RuntimeAnimatorController> _c; Dictionary<string, Material> _m; Dictionary<string, Font> _f; Dictionary<string, Color> _col;

        void Build()
        {
            _s = new Dictionary<string, Sprite>(); foreach (var e in sprites) if (!string.IsNullOrEmpty(e.key)) _s[e.key] = e.sprite;
            _p = new Dictionary<string, GameObject>(); foreach (var e in prefabs) if (!string.IsNullOrEmpty(e.key)) _p[e.key] = e.prefab;
            _c = new Dictionary<string, RuntimeAnimatorController>(); foreach (var e in controllers) if (!string.IsNullOrEmpty(e.key)) _c[e.key] = e.controller;
            _m = new Dictionary<string, Material>(); foreach (var e in materials) if (!string.IsNullOrEmpty(e.key)) _m[e.key] = e.material;
            _f = new Dictionary<string, Font>(); foreach (var e in fonts) if (!string.IsNullOrEmpty(e.key)) _f[e.key] = e.font;
            _col = new Dictionary<string, Color>(); foreach (var e in colors) if (!string.IsNullOrEmpty(e.key)) _col[e.key] = e.color;
        }

        public Sprite Sprite(string key) { if (_s == null) Build(); if (_s.TryGetValue(key, out var v) && v != null) return v; Debug.LogWarning("[AssetCatalog] sprite 없음: " + key); return null; }
        public GameObject Prefab(string key) { if (_p == null) Build(); if (_p.TryGetValue(key, out var v) && v != null) return v; Debug.LogWarning("[AssetCatalog] prefab 없음: " + key); return null; }
        public RuntimeAnimatorController Controller(string key) { if (_c == null) Build(); if (_c.TryGetValue(key, out var v) && v != null) return v; Debug.LogWarning("[AssetCatalog] controller 없음: " + key); return null; }
        public Material Material(string key) { if (_m == null) Build(); if (_m.TryGetValue(key, out var v) && v != null) return v; return null; }
        public Font Font(string key) { if (_f == null) Build(); if (_f.TryGetValue(key, out var v) && v != null) return v; return null; }
        public Color Color(string key, Color fallback) { if (_col == null) Build(); return _col.TryGetValue(key, out var v) ? v : fallback; }
        public bool Has(string key) { if (_s == null) Build(); return _s.ContainsKey(key) || _p.ContainsKey(key) || _c.ContainsKey(key) || _m.ContainsKey(key) || _f.ContainsKey(key); }
    }
}
