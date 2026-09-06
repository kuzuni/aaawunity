using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// UI 안에 «내 플레이어 모습»(CharacterMaker Character 프리팹 · Idle) 을 그리는 헬퍼 — 로비 상단 초상(T6) · 장비 화면 가운데(T7) 공용.
    /// 방법: 전용 레이어(<see cref="Layer"/>) 에 리그를 세우고, 그 레이어만 비추는 카메라가 RenderTexture 로 찍어 RawImage 에 붙인다.
    /// 월드 카메라(전투)는 이 레이어를 컬링 마스크에서 뺀다. 화면이 꺼지면(RawImage 비활성) 무대·카메라도 같이 꺼진다.
    /// 외형(스킨)은 <see cref="SetSkin"/> 으로 바꾼다 — T7 의 GearLook(장착 장비 → 파츠) 표가 여기로 스킨을 넣는다.
    /// <para>
    /// ⚠ URP 2D(Renderer2D · <c>m_UseDepthStencilBuffer: 1</c>) 호환(T12 · 주인 콘솔 에러 ①②): 카메라의 targetTexture 는 반드시
    /// <b>깊이/스텐실 버퍼가 있는</b> RenderTexture 여야 한다. 깊이 0 텍스처를 주면 렌더그래프가 없는 깊이 표면을 attachment 로 붙이려다
    /// «Renderer2D Pass: Fake or uninitialized surface is not supported for attachment 0» + «EndRenderPass: Not inside a Renderpass» 가 매 프레임 뜬다.
    /// 런타임 카메라에는 <see cref="UniversalAdditionalCameraData"/>(Base · 후처리 없음) 를 명시적으로 붙인다.
    /// </para>
    /// </summary>
    public sealed class HeroView : MonoBehaviour
    {
        /// <summary>TagManager 에 이름 없는 빈 레이어(30) — 이름이 없어도 정수 레이어는 컬링에 쓸 수 있다.</summary>
        public const int Layer = 30;
        const float StageX = 400f;   // 전투 월드(x ±30 안)에서 멀리 떨어진 자리
        static int _count;
        /// <summary>에디터 «도메인 리로드 끔» 에서도 무대 번호가 새 판마다 0 부터(UiKit.ResetStatics 와 같은 규약).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _count = 0; }

        RawImage _img; RenderTexture _tex; Camera _cam; Transform _stage; CharacterRig _rig;
        CharacterRig.Skin _skin;
        float _zoom = 1f, _yBias = 0f;
        public CharacterRig Rig => _rig;
        /// <summary>테스트/진단용 — 이 뷰가 찍는 텍스처·카메라.</summary>
        public RenderTexture Texture => _tex;
        public Camera Cam => _cam;
        /// <summary>RawImage 안에서 <b>몸(리그 스프라이트 경계)이 실제로 보이는 사각형</b> — 그림 없는 빈 Rect(앵커 = 텍스처 정규 좌표 · <see cref="Fit"/> 마다 갱신). UI 비평 이름표(«캐릭터» 행 · T46)가 이걸 잰다.</summary>
        public RectTransform Body { get; private set; }

        /// <summary>host(프리팹의 초상 마스크 등) 안을 가득 채우는 RawImage 로 세운다. skin 이 null 이면 기본 기사 외형.</summary>
        public static HeroView Attach(RectTransform host, CharacterRig.Skin skin = null, int texSize = 512)
        {
            var rt = UiKit.Rect(host, "HeroView"); UiKit.Stretch(rt);
            var img = UiKit.Ensure<RawImage>(rt.gameObject); img.raycastTarget = false; img.color = Color.white;
            var hv = UiKit.Ensure<HeroView>(rt.gameObject);
            hv._img = img;
            hv.Body = UiKit.Rect(rt, "Body"); UiKit.Stretch(hv.Body);
            hv.BuildStage(texSize);
            hv.SetSkin(skin ?? DefaultKnightSkin());
            return hv;
        }

        /// <summary>내 플레이어의 현재 외형 = <see cref="CharacterRig.PlayerSkin"/>(장착 투구·무기·갑옷 → GearLook 표 · 실드가 있으면 방패) — 전투(BattleWorld)와 같은 함수(T7).</summary>
        public static CharacterRig.Skin PlayerSkin(App app)
        {
            if (app == null || app.Data == null || app.Save == null) return DefaultKnightSkin();
            bool shield = KkomaKnight.Core.GearSystem.BuildPower(app.Data, app.Save.CurBuild(app.Data)).Sh > 0;
            return CharacterRig.PlayerSkin(app.Data, app.Save, shield);
        }

        /// <summary>장비 반영 전 기본 외형(전투의 KnightSkin 과 같은 파츠).</summary>
        public static CharacterRig.Skin DefaultKnightSkin()
            => new CharacterRig.Skin { Helmet = "cm.knight.helmet", HairHelmet = "cm.knight.hairHelmet", Chest = "cm.knight.chest", Sword = "cm.knight.sword" };

        /// <summary>
        /// URP 2D 카메라가 그릴 수 있는 RenderTexture — 색 ARGB32 + <b>깊이 24 · 스텐실 8</b>(Renderer2D 의 깊이/스텐실 사용 설정과 맞춤).
        /// 플랫폼이 D24S8 을 못 주면 <see cref="GraphicsFormatUtility.GetDepthStencilFormat(int,int)"/> 가 대응 포맷(D32S8)을 고른다.
        /// </summary>
        public static RenderTexture CreateTargetTexture(int size, string name)
        {
            var desc = new RenderTextureDescriptor(size, size, RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = 1, useMipMap = false, autoGenerateMips = false, volumeDepth = 1, dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            };
            var ds = GraphicsFormatUtility.GetDepthStencilFormat(24, 8);
            if (ds != GraphicsFormat.None) desc.depthStencilFormat = ds;
            var tex = new RenderTexture(desc) { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.Create();
            return tex;
        }

        void BuildStage(int texSize)
        {
            int idx = _count++;
            _stage = new GameObject("HeroStage:" + idx).transform;
            _stage.position = new Vector3(StageX + idx * 20f, 0, 0);
            var prefab = App.I != null && App.I.Assets != null ? App.I.Assets.Prefab("cm.character") : null;
            var go = prefab != null ? Instantiate(prefab, _stage) : new GameObject("Character");
            go.name = "Hero"; go.transform.SetParent(_stage, false); go.transform.localPosition = Vector3.zero;
            _rig = CharacterRig.Attach(go);
            SetLayerDeep(_stage, Layer);

            _tex = CreateTargetTexture(texSize, "HeroView" + idx);
            var camGo = new GameObject("HeroCam"); camGo.transform.SetParent(_stage, false); camGo.layer = Layer;
            _cam = camGo.AddComponent<Camera>();
            _cam.enabled = false;   // 설정을 다 넣은 뒤 켠다(반쯤 설정된 카메라가 한 프레임 그리지 않게)
            _cam.orthographic = true; _cam.cullingMask = 1 << Layer; _cam.clearFlags = CameraClearFlags.SolidColor; _cam.backgroundColor = new Color(0, 0, 0, 0);
            _cam.depth = -5; _cam.nearClipPlane = 0.1f; _cam.farClipPlane = 50f; _cam.allowHDR = false; _cam.allowMSAA = false; _cam.useOcclusionCulling = false;
            _cam.rect = new Rect(0, 0, 1, 1);
            _cam.targetTexture = _tex;
            // URP: 런타임 카메라도 Base 카메라 데이터가 있어야 파이프라인 기본값(렌더러 인덱스·후처리 등)이 명확하다 — 파이프라인이 늦게 만들어 주는 것에 기대지 않는다.
            var urp = UiKit.Ensure<UniversalAdditionalCameraData>(camGo);
            urp.renderType = CameraRenderType.Base; urp.renderPostProcessing = false; urp.renderShadows = false;
            urp.requiresDepthOption = CameraOverrideOption.Off; urp.requiresColorOption = CameraOverrideOption.Off;
            urp.antialiasing = AntialiasingMode.None;
            _cam.enabled = true;
            // 전투 카메라는 이 레이어를 안 본다
            var world = App.I != null ? App.I.WorldCamera : null; if (world != null) world.cullingMask &= ~(1 << Layer);
            _img.texture = _tex;
        }

        /// <summary>외형 교체 — 파츠를 꽂고 Idle 을 다시 틀고, 카메라를 몸에 맞춘다.</summary>
        public void SetSkin(CharacterRig.Skin skin)
        {
            _skin = skin ?? DefaultKnightSkin();
            if (_rig == null) return;
            _rig.Apply(_skin); _rig.Face(true); _rig.SetScale(1f);
            PlayIdle();
            Fit();
        }

        /// <summary>Idle 재생 — 무대가 꺼져 있으면(화면 숨김) Animator 가 초기화 전이라 Play 가 «not playing an AnimatorController» 경고를 낼 수 있어 켜질 때(<see cref="OnEnable"/>) 다시 튼다.</summary>
        void PlayIdle()
        {
            if (_rig == null || _stage == null || !_stage.gameObject.activeInHierarchy) return;
            _rig.Play(CharacterRig.Idle, true); _rig.SetSpeed(1f);
        }

        /// <summary>
        /// 카메라 프레이밍(T34 · 로비 상단 바의 정사각 초상은 전신이 아니라 <b>가슴 위</b>가 보여야 레퍼런스 아바타처럼 보인다).
        /// zoom = 전신 맞춤 대비 확대 배율(1 = 전신 · 1.6 ≈ 가슴 위) · yBias = 카메라 중심을 몸 높이의 이 비율만큼 위로(0 = 몸 가운데 · 0.45 ≈ 머리 쪽). 장비 화면은 기본값(전신) 그대로.
        /// </summary>
        public void SetFraming(float zoom, float yBias) { _zoom = Mathf.Max(0.1f, zoom); _yBias = yBias; Fit(); }

        /// <summary>리그의 스프라이트 경계에 카메라를 맞춘다(정사각 텍스처 · 여유 12% · <see cref="SetFraming"/> 배율/편향 반영).</summary>
        void Fit()
        {
            if (_cam == null || _rig == null) return;
            var b = _rig.Bounds();
            float half = Mathf.Max(b.extents.x, b.extents.y, 0.2f) * 1.12f / _zoom;
            _cam.orthographicSize = half;
            _cam.transform.position = new Vector3(b.center.x, b.center.y + b.extents.y * _yBias, b.center.z - 10f);
            if (Body != null && half > 0f)
            {
                // 정사각 텍스처에서 몸이 차지하는 정규 사각형(0~1 · 카메라 중심이 0.5) — RawImage 가 host 를 채우므로 그대로 RawImage 안 앵커가 된다
                var c = _cam.transform.position; float s = 2f * half;
                Body.anchorMin = new Vector2(Mathf.Clamp01((b.min.x - c.x) / s + 0.5f), Mathf.Clamp01((b.min.y - c.y) / s + 0.5f));
                Body.anchorMax = new Vector2(Mathf.Clamp01((b.max.x - c.x) / s + 0.5f), Mathf.Clamp01((b.max.y - c.y) / s + 0.5f));
                Body.offsetMin = Body.offsetMax = Vector2.zero;
            }
        }

        static void SetLayerDeep(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerDeep(t.GetChild(i), layer);
        }

        void OnEnable() { if (_stage != null) { _stage.gameObject.SetActive(true); PlayIdle(); } }
        void OnDisable() { if (_stage != null) _stage.gameObject.SetActive(false); }
        void OnDestroy()
        {
            // 순서가 중요하다: 카메라를 먼저 끄고 타깃을 떼야 파이프라인이 해제된 텍스처를 그리려 들지 않는다(«EndRenderPass» 류 에러 방지).
            if (_cam != null) { _cam.enabled = false; _cam.targetTexture = null; }
            if (_img != null) { _img.enabled = false; _img.texture = null; }
            if (_stage != null) Destroy(_stage.gameObject);
            if (_tex != null) { _tex.Release(); Destroy(_tex); _tex = null; }
            _cam = null; _rig = null; _stage = null;
        }
    }
}
