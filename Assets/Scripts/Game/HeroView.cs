using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// UI 안에 «내 플레이어 모습»(CharacterMaker Character 프리팹 · Idle) 을 그리는 헬퍼 — 로비 상단 초상(T6) · 장비 화면 가운데(T7) 공용.
    /// 방법: 전용 레이어(<see cref="Layer"/>) 에 리그를 세우고, 그 레이어만 비추는 카메라가 RenderTexture 로 찍어 RawImage 에 붙인다.
    /// 월드 카메라(전투)는 이 레이어를 컬링 마스크에서 뺀다. 화면이 꺼지면(RawImage 비활성) 무대·카메라도 같이 꺼진다.
    /// 외형(스킨)은 <see cref="SetSkin"/> 으로 바꾼다 — T7 의 GearLook(장착 장비 → 파츠) 표가 여기로 스킨을 넣는다.
    /// </summary>
    public sealed class HeroView : MonoBehaviour
    {
        /// <summary>TagManager 에 이름 없는 빈 레이어(30) — 이름이 없어도 정수 레이어는 컬링에 쓸 수 있다.</summary>
        public const int Layer = 30;
        const float StageX = 400f;   // 전투 월드(x ±30 안)에서 멀리 떨어진 자리
        static int _count;

        RawImage _img; RenderTexture _tex; Camera _cam; Transform _stage; CharacterRig _rig;
        CharacterRig.Skin _skin;
        public CharacterRig Rig => _rig;

        /// <summary>host(프리팹의 초상 마스크 등) 안을 가득 채우는 RawImage 로 세운다. skin 이 null 이면 기본 기사 외형.</summary>
        public static HeroView Attach(RectTransform host, CharacterRig.Skin skin = null, int texSize = 512)
        {
            var rt = UiKit.Rect(host, "HeroView"); UiKit.Stretch(rt);
            var img = UiKit.Ensure<RawImage>(rt.gameObject); img.raycastTarget = false; img.color = Color.white;
            var hv = UiKit.Ensure<HeroView>(rt.gameObject);
            hv._img = img;
            hv.BuildStage(texSize);
            hv.SetSkin(skin ?? DefaultKnightSkin());
            return hv;
        }

        /// <summary>내 플레이어의 현재 외형 — T7 이 여기서 GearLook(장착 투구·무기·갑옷 → 파츠) 표를 적용한다. 지금은 기본 기사 외형.</summary>
        public static CharacterRig.Skin PlayerSkin(App app) => DefaultKnightSkin();

        /// <summary>장비 반영 전 기본 외형(전투의 KnightSkin 과 같은 파츠).</summary>
        public static CharacterRig.Skin DefaultKnightSkin()
            => new CharacterRig.Skin { Helmet = "cm.knight.helmet", HairHelmet = "cm.knight.hairHelmet", Chest = "cm.knight.chest", Sword = "cm.knight.sword" };

        void BuildStage(int texSize)
        {
            int idx = _count++;
            _stage = new GameObject("HeroStage:" + idx).transform;
            _stage.position = new Vector3(StageX + idx * 20f, 0, 0);
            var prefab = App.I != null && App.I.Assets != null ? App.I.Assets.Prefab("cm.character") : null;
            var go = prefab != null ? Instantiate(prefab, _stage) : new GameObject("Character");
            go.name = "Hero"; go.transform.localPosition = Vector3.zero;
            _rig = CharacterRig.Attach(go);
            SetLayerDeep(_stage, Layer);

            _tex = new RenderTexture(texSize, texSize, 0, RenderTextureFormat.ARGB32) { name = "HeroView" + idx, antiAliasing = 1 };
            _tex.Create();
            var camGo = new GameObject("HeroCam"); camGo.transform.SetParent(_stage, false); camGo.layer = Layer;
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true; _cam.cullingMask = 1 << Layer; _cam.clearFlags = CameraClearFlags.SolidColor; _cam.backgroundColor = new Color(0, 0, 0, 0);
            _cam.targetTexture = _tex; _cam.depth = -5; _cam.nearClipPlane = 0.1f; _cam.farClipPlane = 50f; _cam.allowHDR = false; _cam.allowMSAA = false;
            _cam.rect = new Rect(0, 0, 1, 1);
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
            _rig.Play(CharacterRig.Idle, true); _rig.SetSpeed(1f);
            Fit();
        }

        /// <summary>리그의 스프라이트 경계에 카메라를 맞춘다(정사각 텍스처 · 여유 12%).</summary>
        void Fit()
        {
            if (_cam == null || _rig == null) return;
            var b = _rig.Bounds();
            float half = Mathf.Max(b.extents.x, b.extents.y, 0.2f) * 1.12f;
            _cam.orthographicSize = half;
            _cam.transform.position = new Vector3(b.center.x, b.center.y, b.center.z - 10f);
        }

        static void SetLayerDeep(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerDeep(t.GetChild(i), layer);
        }

        void OnEnable() { if (_stage != null) _stage.gameObject.SetActive(true); }
        void OnDisable() { if (_stage != null) _stage.gameObject.SetActive(false); }
        void OnDestroy()
        {
            if (_cam != null) _cam.targetTexture = null;
            if (_stage != null) Destroy(_stage.gameObject);
            if (_tex != null) { _tex.Release(); Destroy(_tex); }
        }
    }
}
