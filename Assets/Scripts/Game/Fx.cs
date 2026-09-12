using System.Collections.Generic;
using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>이펙트 도우미 — CFXR 프리팹(카탈로그 fx.*)을 월드/UI 자리에 뿌린다. 프리팹이 없으면 조용히 건너뛴다.</summary>
    public static class Fx
    {
        public const int SortingOrder = 400;   // 캐릭터(≤ 300) 위

        // ───────────────────────── T504 알갱이 힛 버스트 ─────────────────────────
        /// <summary>
        /// T504(주인 2026-09-12 «힛 이펙트가 너무 반짝임 · 너무 글로우임») — 피격 이펙트를 CFXR «Hit A (Red)»/«Impact Glowing HDR» 에서 <b>코드로 세운 알갱이 파티클</b>로 바꾼다.
        /// 반짝임의 정체는 그 프리팹의 재질(«… add.mat» = 가산 블렌드 · 밝은 맵 위에서 흰빛으로 뜬다)과 «Point Light» 였다.
        /// 여기 알갱이는 <b>알파 블렌드</b>(`Sprites/Default`) + 라이트 0 — 색이 그 색대로 보이고 겹쳐도 하얘지지 않는다.
        /// 모양은 T340 의 UI 알갱이(<see cref="UiParticles"/>)와 같은 결(원 안 어디서나 · 튀어나가 잦아들며 · 알파·크기가 준다) · 중력만 조금 준다(«튄다» 느낌).
        /// </summary>
        public const string HitBurstName = "HitBurst";
        /// <summary>적 피격 알갱이 색(붉은 기 — 종전 «Hit A (Red)» 의 자리).</summary>
        public static readonly Color HitGrain = new Color(1f, 0.42f, 0.32f);
        static Material _grainMat; static Texture2D _grainTex;
        /// <summary>알갱이 재질 — 알파 블렌드 스프라이트 기본 셰이더에 둥근 알갱이 텍스처. 한 번 만들어 같이 쓴다.</summary>
        public static Material GrainMaterial()
        {
            if (_grainMat != null) return _grainMat;
            var sh = Shader.Find("Sprites/Default");
            _grainMat = new Material(sh) { name = "HitGrain", mainTexture = GrainTexture() };
            return _grainMat;
        }
        static Texture2D GrainTexture()
        {
            if (_grainTex != null) return _grainTex;
            const int N = 16; float c = (N - 1) * 0.5f;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "HitGrainTex", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                // 가장자리 한 픽셀만 부드럽게 — «빛 번짐» 이 아니라 «알갱이» 로 보이게 테두리를 세운다
                float a = Mathf.Clamp01(7.0f - d);
                px[y * N + x] = new Color(1f, 1f, 1f, a);
            }
            t.SetPixels(px); t.Apply(false, true); _grainTex = t;
            return t;
        }
        /// <summary>
        /// 알갱이 힛 버스트 — <paramref name="pos"/>(월드)에서 <paramref name="count"/> 알갱이가 사방으로 튀고 0.35초 안에 잦아든다.
        /// <paramref name="scale"/> 은 크기·속도 배율(치명타 1.4 · 일반 1 · 플레이어 피격 0.8). 다 끝나면 스스로 사라진다.
        /// </summary>
        public static GameObject HitBurst(Vector3 pos, Color tint, float scale = 1f, int count = 10, Transform parent = null)
        {
            var go = new GameObject(HitBurstName);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // T348 — 값을 주기 «전에» 세운다(Assert 회피)
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.material = GrainMaterial(); psr.sortingOrder = SortingOrder; psr.renderMode = ParticleSystemRenderMode.Billboard;
            var main = ps.main;
            main.duration = 0.35f; main.loop = false; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f * scale, 3.4f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f * scale, 0.13f * scale);
            main.startColor = tint;
            main.gravityModifier = 1.2f;
            main.maxParticles = Mathf.Max(count, 8);
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shp = ps.shape; shp.enabled = true; shp.shapeType = ParticleSystemShapeType.Circle;
            shp.radius = 0.06f * scale; shp.radiusThickness = 1f; shp.arc = 360f; shp.arcMode = ParticleSystemShapeMultiModeValue.Random;
            var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.dampen = 0.25f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));
            ps.Play(true);
            Object.Destroy(go, 0.8f);
            return go;
        }

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

        // ───────────────────────── 스프라이트 시트 (T70 번개) ─────────────────────────
        /// <summary>번개 시트(`fx.lightning` · CC0)의 규격 — 칸 수 · 재생 순서 · 기울기. 근거는 <c>Assets/KkomaKnight/Fx/Lightning/LICENSES.md</c>.</summary>
        public const int LightningCols = 6;
        /// <summary>재생 순서 — 칸 2(새까만 반전 섬광)는 밝은 숲 맵에서 «그림 깨짐» 으로 보여 뺀다(원본 파일은 그대로).</summary>
        public static readonly int[] LightningFrames = { 0, 1, 3, 4 };
        /// <summary>시트가 이미 «왼쪽 아래로 비스듬히» 그려져 있어 이만큼 돌리면 하늘에서 수직으로 내리꽂힌다.</summary>
        public const float LightningTiltDeg = 55f;
        public const float LightningFps = 13f;   // 4칸 / 13fps ≈ 0.31초 (지시서 0.25~0.35초)

        /// <summary>
        /// 세로로 세운 시트 한 칸의 «돌린 뒤» 높이(스케일 1 · 유니티 단위) — 140×86px 을 55° 돌린 외접 상자 = 140·sin55 + 86·cos55 ≈ 164px.
        /// 칸 크기는 시트에서 실제로 읽는다(시트를 바꿔도 길이 계산이 따라간다) · 아직 못 읽으면 지금 시트의 실측값.
        /// </summary>
        public static float LightningSpanAtScale1
        {
            get
            {
                float w = 140f, h = 86f, ppu = 100f;
                var sliced = Slice("fx.lightning", LightningCols);
                if (sliced != null && sliced[0] != null) { w = sliced[0].rect.width; h = sliced[0].rect.height; ppu = sliced[0].pixelsPerUnit; }
                float r = LightningTiltDeg * Mathf.Deg2Rad;
                return (w * Mathf.Abs(Mathf.Sin(r)) + h * Mathf.Abs(Mathf.Cos(r))) / ppu;
            }
        }

        static readonly Dictionary<string, Sprite[]> _sheets = new Dictionary<string, Sprite[]>();

        /// <summary>시트 한 장을 가로 <paramref name="cols"/> 칸으로 잘라 둔다(한 번만 · 키마다 캐시). 텍스처를 CPU 로 읽지 않으므로 isReadable 이 꺼져 있어도 된다(WebGL 포함).</summary>
        static Sprite[] Slice(string key, int cols)
        {
            if (_sheets.TryGetValue(key, out var cached))
            {
                bool ok = cached != null && cached.Length == cols;
                if (ok) foreach (var s in cached) if (s == null) { ok = false; break; }
                if (ok) return cached;
                _sheets.Remove(key);
            }
            var sheet = App.I != null && App.I.Assets != null ? App.I.Assets.Sprite(key) : null;
            if (sheet == null || sheet.texture == null || cols <= 0) return null;
            var tex = sheet.texture;
            int fw = tex.width / cols, fh = tex.height;
            var frames = new Sprite[cols];
            for (int i = 0; i < cols; i++)
                frames[i] = Sprite.Create(tex, new Rect(i * fw, 0, fw, fh), new Vector2(0.5f, 0.5f), sheet.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            _sheets[key] = frames;
            return frames;
        }

        /// <summary>
        /// 가로로 이어 붙인 스프라이트 시트를 한 번 재생한다(<see cref="SheetAnim"/>). 시트가 없으면 조용히 null.
        /// </summary>
        /// <param name="frames">재생할 칸 번호 순서(예: 0·1·3·4) — null 이면 0부터 차례로 전부.</param>
        /// <param name="scale">오브젝트 배율(1 = 한 칸이 폭 <c>fw</c>/100 유니티 단위).</param>
        /// <param name="rotZ">z 회전(도).</param>
        /// <param name="delay">이 초 동안은 안 보이게 기다린다(적마다 어긋나게 떨어뜨릴 때).</param>
        /// <param name="onEnd">마지막 칸이 끝난 순간 한 번(«닿는» 순간의 튀김 이펙트 자리).</param>
        public static GameObject PlaySheet(string key, int cols, int[] frames, float fps, Vector3 pos, float scale,
                                           float rotZ = 0f, float delay = 0f, Transform parent = null,
                                           string name = null, System.Action onEnd = null)
        {
            var sliced = Slice(key, cols);
            if (sliced == null) return null;
            var order = frames;
            if (order == null) { order = new int[cols]; for (int i = 0; i < cols; i++) order[i] = i; }
            var list = new Sprite[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                int c = order[i];
                if (c < 0 || c >= cols) return null;
                list[i] = sliced[c];
            }
            var go = new GameObject(name ?? key);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 0, rotZ);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = SortingOrder; sr.enabled = false;
            var anim = go.AddComponent<SheetAnim>();
            anim.Frames = list; anim.Fps = fps; anim.Delay = delay; anim.OnEnd = onEnd;
            return go;
        }
    }
}
