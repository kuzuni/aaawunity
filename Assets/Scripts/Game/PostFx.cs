using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 월드 포스트 프로세싱(T181 · 주인 2026-09-07 11:4X «glow 같은 거 빛나는 느낌 잘 안 드는 거 같던데 포스트 프로세싱 같은 거 설정해야 하는 듯») —
    /// <b>Bloom</b> 하나를 전역 <see cref="Volume"/> 로 켠다.
    /// <list type="bullet">
    /// <item><b>씬 파일을 손으로 안 고친다</b> — 워커에게는 유니티 에디터가 없어 <c>SampleScene.unity</c> 와 <c>VolumeProfile</c> 에셋의
    /// YAML(스크립트 GUID·fileID)을 <b>추측으로</b> 써 넣어야 하는데, 한 글자만 틀려도 씬이 통째로 안 열려 PlayMode 전체와 WebGL 빌드가 죽는다.
    /// 그래서 이 레포가 화면을 만드는 방식 그대로 <b>코드로</b> 세운다(결정 458). 프로파일도 메모리에서 만든다 — 새 에셋 0.</item>
    /// <item><b>UI 에는 안 먹는다</b>(지시서 2항) — UI 캔버스가 <c>ScreenSpaceOverlay</c> 라 카메라 렌더 «뒤» 에 그려진다.
    /// 아이템 칸 빛살·글로우 서클(T155 ⓓ·T172·T174)·특전 카드 shine 은 전부 UI 라 <b>Bloom 으로는 안 밝아진다</b> — 그쪽은 그 작업들이 UI 층에서 따로 한다.</item>
    /// <item><see cref="HeroView"/> 의 런타임 카메라(로비·장비 초상 · RenderTexture)는 <b>켜지 않는다</b> — 그쪽은 이미
    /// <c>renderPostProcessing = false</c> 로 못 박혀 있고(HeroView.cs), 켜면 초상이 뿌옇고 비용만 는다.</item>
    /// </list>
    /// </summary>
    public static class PostFx
    {
        /// <summary>전역 Volume 오브젝트 이름(게이트가 이 이름으로 찾는다).</summary>
        public const string VolumeName = "PostFxVolume";

        /// <summary>
        /// Bloom 값 — 폰에서 보고 잡는다. 과하면 배경이 뿌옇고 모바일에서 싸지 않다(지시서 3항 «WebGL 비용을 반드시 잰다»).
        /// <c>threshold</c> 는 «이 밝기 위만 번진다» 라 HDR·Linear 인 이 프로젝트에서 1 근처가 기준이고,
        /// 값 셋을 여기 한 곳에 둬 다음 워커가 스샷을 보고 조절할 자리를 한눈에 안다.
        /// </summary>
        public const float BloomThreshold = 1.05f, BloomIntensity = 0.85f, BloomScatter = 0.65f;

        static Volume _volume;

        /// <summary>지금 세워져 있는 전역 Volume(테스트가 본다 · 없으면 null).</summary>
        public static Volume Current => _volume;
        /// <summary>그 프로파일에 넣은 Bloom(테스트가 값을 잰다) — 프로파일을 다시 뒤지지 않고 만든 것을 그대로 들고 있는다.</summary>
        public static Bloom CurrentBloom { get; private set; }

        /// <summary>
        /// 전역 Volume 을 세우고 카메라의 후처리를 켠다 — 씬이 바뀌어도 살아남게 <paramref name="keepAlive"/> 밑에 붙인다.
        /// 두 번 불러도 하나만 선다(이미 있으면 그것을 돌려준다).
        /// </summary>
        public static Volume Enable(Transform keepAlive, Camera cam)
        {
            if (_volume != null) { Wire(cam); return _volume; }

            var go = new GameObject(VolumeName);
            if (keepAlive != null) go.transform.SetParent(keepAlive, false);
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.weight = 1f;
            v.priority = 0f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "PostFxProfile";                 // 에셋이 아니라 메모리 인스턴스다(새 파일 0)
            var bloom = profile.Add<Bloom>(true);
            if (bloom != null)
            {
                bloom.threshold.overrideState = true; bloom.threshold.value = BloomThreshold;
                bloom.intensity.overrideState = true; bloom.intensity.value = BloomIntensity;
                bloom.scatter.overrideState = true; bloom.scatter.value = BloomScatter;
                bloom.highQualityFiltering.overrideState = true; bloom.highQualityFiltering.value = false;   // 모바일 비용(지시서 3항)
            }
            v.sharedProfile = profile;
            CurrentBloom = bloom;

            _volume = v;
            Wire(cam);
            return v;
        }

        /// <summary>카메라 쪽 스위치 — 이것을 안 켜면 Volume 이 있어도 아무 일도 안 난다(씬 카메라는 기본이 꺼짐이다).</summary>
        static void Wire(Camera cam)
        {
            if (cam == null) return;
            var data = UiKit.Ensure<UniversalAdditionalCameraData>(cam.gameObject);
            data.renderPostProcessing = true;
        }
    }
}
