using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T340 — <b>진짜 <c>ParticleSystem</c></b> 을 UI 캔버스 «위» 에 그린다(주인 2026-09-10 «왜 파티클 시스템 안 쓰냐 · 파티클 시스템으로 하지»).
    /// <para>
    /// ⚠ <b>여태 못 쓴 까닭과, 그 벽을 넘는 법.</b> 이 게임의 캔버스는 <c>ScreenSpaceOverlay</c> 라(<see cref="UiKit.CreateRootCanvas"/>)
    /// <c>ParticleSystemRenderer</c> 가 그린 것은 <b>언제나 UI 뒤</b>로 간다 — T144·T174·T181 이 같은 벽에서 돌아섰고,
    /// 그래서 «알갱이» 는 <c>Image</c> 몇 장을 트윈으로 흘리는 <b>흉내</b>였다(<c>UiKit.DustOver</c>).
    /// 여기서는 <b>그리는 자만 갈아 끼운다</b> — 나게 하고 움직이는 것은 유니티 <c>ParticleSystem</c> 그대로 두고
    /// (분출·모양·수명·색·크기 곡선 전부 그것이 돈다), <c>ParticleSystemRenderer</c> 는 <b>꺼</b> 두고
    /// 매 프레임 살아 있는 알갱이를 읽어 <c>CanvasRenderer</c> 사각형으로 굽는다. 그러면 캔버스 층 규칙을 그대로 받아 <b>UI 위</b>에 뜬다.
    /// </para>
    /// <list type="bullet">
    /// <item><b>시간</b> — 팝업이 뜨면 <c>Time.timeScale</c> 이 0 이다(T3). 그래서 <c>main.useUnscaledTime = true</c> ·
    /// 이것을 빼면 알갱이가 «난 자리에 얼어붙는다».</item>
    /// <item><b>자리</b> — <c>simulationSpace = Local</c> 이라 알갱이 좌표가 곧 이 사각형의 UI 픽셀이다(속도·크기도 px 로 준다).</item>
    /// <item><b>무작위 0</b>(T174 규약) — <c>useAutoRandomSeed = false</c> + 붙박이 씨앗이라 같은 판이면 늘 같은 그림이다.
    /// 안 그러면 <c>screens</c> PNG 가 회차마다 흔들려 «고쳤나» 를 그림으로 못 잰다.</item>
    /// <item><b>겹칠수록 밝다</b> — <see cref="UiKit.LightMaterial"/>(加算)을 쓴다. 셰이더가 없으면 조용히 보통 합성.</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UiParticles : MaskableGraphic
    {
        /// <summary>알갱이 그림 — 둥근 빛 한 점(<see cref="UiKit.GlowKey"/>).</summary>
        public const string GrainKey = UiKit.GlowKey;
        /// <summary>씨앗 — 붙박이(같은 판 = 같은 그림 · T174).</summary>
        public const uint Seed = 20260910u;
        /// <summary>이 오브젝트의 이름 — 자가 «떴는가» 를 이 이름으로 찾는다.</summary>
        public const string ObjName = "UiParticles";

        ParticleSystem _ps;
        ParticleSystem.Particle[] _buf;
        Sprite _sprite;
        int _frames;

        public override Texture mainTexture { get { return _sprite != null && _sprite.texture != null ? _sprite.texture : s_WhiteTexture; } }

        /// <summary>지금 살아 있는 알갱이 수(자 전용 · 0 이면 다 사라졌다).</summary>
        public int Alive { get { return _ps != null ? _ps.particleCount : 0; } }

        /// <summary>
        /// <paramref name="layer"/> 의 <paramref name="at"/>(층의 왼쪽 아래 0,0 기준 px)에서 알갱이 <paramref name="count"/>개가 <b>한 번</b> 터진다.
        /// </summary>
        /// <param name="radius">알갱이가 나는 자리의 반지름(px · 0 이면 한 점에서).</param>
        /// <param name="speed">퍼지는 빠르기(px/초) — 끝에서 <paramref name="damping"/> 만큼 잦아든다.</param>
        /// <param name="sizePx">알갱이 한 변(px · 수명 동안 거의 0 으로 줄어든다).</param>
        /// <param name="sec">한 알갱이가 나서 사라지기까지(초).</param>
        public static UiParticles Burst(RectTransform layer, Vector2 at, Color tint, int count,
                                        float radius, float speed, float sizePx, float sec, float damping = 3f)
        {
            if (layer == null || count <= 0) return null;
            var rt = UiKit.Rect(layer, ObjName);
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = at;
            var p = rt.gameObject.AddComponent<UiParticles>();
            // 터지는 것이 탭을 먹으면 스킵·닫기가 안 먹는다(T307 ⓑ 와 같은 까닭)
            p.raycastTarget = false;
            p.Init(tint, count, radius, speed, sizePx, sec, damping);
            return p;
        }

        void Init(Color tint, int count, float radius, float speed, float sizePx, float sec, float damping)
        {
            _sprite = App.I != null && App.I.Assets != null ? App.I.Assets.Sprite(GrainKey) : null;
            { var lm = UiKit.LightMaterial(); if (lm != null) material = lm; }
            color = tint;

            _ps = gameObject.AddComponent<ParticleSystem>();
            // ⚠ T348 — `AddComponent<ParticleSystem>()` 로 붙인 것은 **이미 돌고 있다**(새 ParticleSystem 의 playOnAwake
            //    기본값이 true 라 붙는 그 프레임에 Awake 가 Play 를 부른다). 돌고 있는 동안 `main.duration` 을 주면
            //    유니티가 «Setting the duration while system is still playing is not supported» 를 **Assert** 로 뱉고,
            //    그것은 콘솔 빨강이라 `PlayLog.AssertNoRed` 를 쓰는 자가 전부 빨개진다(§1 «플레이 콘솔 에러 0»).
            //    아래 `main.playOnAwake = false` 는 **다음 Awake** 를 막을 뿐 이미 시작한 이 판을 못 멈춘다 —
            //    그래서 값을 하나라도 주기 «전에» 여기서 세운다. 유니티가 시키는 그 말대로 Stop 한다.
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var psr = GetComponent<ParticleSystemRenderer>();
            // 월드로는 한 점도 안 그린다 — 그리는 것은 아래 OnPopulateMesh 뿐
            if (psr != null) psr.enabled = false;

            var main = _ps.main;
            main.duration = Mathf.Max(0.05f, sec);
            main.loop = false;
            main.playOnAwake = false;
            // 팝업 동안 timeScale = 0 (T3) — 이 줄이 없으면 알갱이가 난 자리에 얼어붙는다
            main.useUnscaledTime = true;
            // 알갱이 좌표 = 이 사각형의 UI px
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(sec * 0.65f, sec);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(sizePx * 0.6f, sizePx);
            main.startColor = tint;
            main.gravityModifier = 0f;
            main.maxParticles = Mathf.Max(count, 8);
            // 씨앗은 MainModule 이 아니라 ParticleSystem 본체에 있다(main.useAutoRandomSeed 는 없다 — 컴파일 오류)
            _ps.useAutoRandomSeed = false;
            _ps.randomSeed = Seed;

            var em = _ps.emission;
            em.enabled = true;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var sh = _ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = Mathf.Max(0.01f, radius);
            // 원 «안» 어디서나(테두리만이면 고리로 보인다)
            sh.radiusThickness = 1f;
            sh.arc = 360f;
            sh.arcMode = ParticleSystemShapeMultiModeValue.Random;

            // 퍼지다 잦아든다 — «펑 하고 밀려났다 멎는» 결
            var lim = _ps.limitVelocityOverLifetime;
            lim.enabled = true;
            lim.dampen = Mathf.Clamp01(damping / 10f);

            // 알파가 0 으로 진다(끝 45% 구간) — 갑자기 없어지면 «사라진» 이 아니라 «끊긴» 으로 보인다
            var col = _ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // 크기도 같이 줄어든다(알갱이가 «식는» 느낌)
            var sz = _ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

            _buf = new ParticleSystem.Particle[main.maxParticles];
            _ps.Play(true);
        }

        void LateUpdate()
        {
            if (_ps == null) return;
            // 알갱이가 움직였으니 이번 프레임 사각형을 다시 굽는다
            SetVerticesDirty();
            // 마지막 알갱이가 사라지면 스스로 치운다(창이 먼저 닫히면 자식이라 같이 죽는다).
            // ⚠ `Destroy(go, 초)` 는 **안 쓴다** — 그 지연은 `Time.timeScale` 을 타는데 팝업 동안 그것이 0 이라(T3) 영영 안 지워진다.
            // ⚠ 첫 두 프레임은 안 묻는다 — 분출 전에는 `IsAlive` 가 거짓일 수 있어 «나기도 전에» 지워진다.
            if (++_frames > 2 && !_ps.IsAlive(true)) Destroy(gameObject);
        }

        /// <summary>살아 있는 알갱이 하나 = 사각형 하나. 크기·색은 <b>파티클 시스템이 이번 프레임에 낸 값</b>을 그대로 읽는다.</summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_ps == null || _buf == null) return;
            int n = _ps.GetParticles(_buf);
            if (n <= 0) return;
            var uv = _sprite != null ? UnityEngine.Sprites.DataUtility.GetOuterUV(_sprite) : new Vector4(0f, 0f, 1f, 1f);
            for (int i = 0; i < n; i++)
            {
                var pt = _buf[i];
                var at = (Vector2)pt.position;
                float h = pt.GetCurrentSize(_ps) * 0.5f;
                if (h <= 0.01f) continue;
                var c = (Color32)pt.GetCurrentColor(_ps);
                int v = vh.currentVertCount;
                vh.AddVert(new Vector3(at.x - h, at.y - h), c, new Vector2(uv.x, uv.y));
                vh.AddVert(new Vector3(at.x - h, at.y + h), c, new Vector2(uv.x, uv.w));
                vh.AddVert(new Vector3(at.x + h, at.y + h), c, new Vector2(uv.z, uv.w));
                vh.AddVert(new Vector3(at.x + h, at.y - h), c, new Vector2(uv.z, uv.y));
                vh.AddTriangle(v, v + 1, v + 2);
                vh.AddTriangle(v, v + 2, v + 3);
            }
        }
    }
}
