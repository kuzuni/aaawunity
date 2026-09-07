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
    /// 그래서 이 레포가 화면을 만드는 방식 그대로 <b>코드로</b> 세운다(결정 463). 프로파일도 메모리에서 만든다 — 새 에셋 0.</item>
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

        /// <summary>
        /// T181 ⓑ — <b>비용</b> 손잡이 둘(«세기» 가 아니다 · 결정 563).
        /// <para>
        /// ⓐ 판정이 «Bloom 은 공짜가 아니다» 를 확정했다(CI #424 같은 런 A/B: 끄면 <b>1.67배</b> 빠르고 프레임 시간은 2.4배 좋다).
        /// 그런데 <see cref="BloomIntensity"/>·<see cref="BloomThreshold"/> 를 낮춰도 <b>fps 는 그대로다</b> — 그 둘은 «얼마나 밝게» 이지
        /// «몇 픽셀을 칠하나» 가 아니다. 값을 실제로 깎는 것은 <b>번짐 피라미드의 해상도와 층수</b>다:
        /// </para>
        /// <list type="bullet">
        /// <item><b>downscale = Quarter</b> — 번짐을 <b>1/4 해상도</b>에서 굽는다(기본 Half). 픽셀 수가 층마다 1/4 이라 이 작업의 가장 큰 손잡이고,
        /// 그림은 «조금 더 부드러운 번짐» 이 된다 — 주인이 원한 «빛나는 느낌» 은 세기(intensity)가 지키므로 안 깎인다.</item>
        /// <item><b>maxIterations 6 → 4</b> — 가장 넓게 퍼지는 두 층을 뺀다. 넓은 halo 가 조금 좁아지는 대신 전면 패스가 둘 준다.</item>
        /// </list>
        /// <b>⚠ 이 둘은 «이름으로» 넣는다</b>(<see cref="SetLever"/> · 리플렉션). 까닭은 워커에게 유니티가 없어 <b>서명을 확인할 길이 없기 때문</b>이다 —
        /// dotnet 스텁에 추측으로 필드를 더해 타입으로 쓰면 «로컬 초록 · 유니티만 컴파일 실패» 가 되고, 그 꼴이 오늘 아침 이 작업을 여덟 시간 세웠다(결정 457).
        /// 리플렉션은 <b>어느 쪽에서도 컴파일이 깨지지 않고</b>, 진짜 빌드에서 <b>무엇이 있었는지 로그로 알려 준다</b>(그 줄이 다음 회차가 타입으로 옮길 근거다).
        /// </summary>
        public const int BloomMaxIterations = 4;
        /// <summary>번짐을 굽는 해상도(URP <c>BloomDownscaleMode</c> 의 멤버 <b>이름</b> — 열거형 «값» 을 지어내지 않는다).</summary>
        public const string BloomDownscale = "Quarter";

        /// <summary>
        /// 비용 손잡이를 실제로 넣은 결과 한 줄(게이트·스모크가 읽는다) — 예 <c>«downscale=Quarter maxIterations=4»</c>.
        /// 못 넣었으면 까닭이 그 자리에 남는다(<c>=none</c> = 그런 필드가 없다 · <c>=enum(…)</c> = 멤버 이름이 다르다).
        /// </summary>
        public static string LeverReport { get; private set; } = "(아직 안 세움)";
        /// <summary>진짜 빌드의 <c>Bloom</c> 이 가진 필드 이름 전부(다음 회차가 스텁을 타입으로 넓힐 때 볼 목록).</summary>
        public static string BloomFields { get; private set; } = "";

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
                ApplyLevers(bloom);
            }
            v.sharedProfile = profile;
            CurrentBloom = bloom;

            _volume = v;
            Wire(cam);
            return v;
        }

        /// <summary>
        /// 비용 손잡이 둘을 넣고 «무엇이 들어갔는지» 를 <see cref="LeverReport"/> 와 로그 한 줄로 남긴다.
        /// 진짜 빌드에서만 답이 나오는 물음이라(스텁 Bloom 에는 이 필드들이 없다) <b>기록이 이 함수의 절반</b>이다.
        /// </summary>
        static void ApplyLevers(Bloom bloom)
        {
            var names = new System.Text.StringBuilder();
            foreach (var f in bloom.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (names.Length > 0) names.Append(',');
                names.Append(f.Name);
            }
            BloomFields = names.ToString();
            LeverReport = SetLever(bloom, "downscale", 0, BloomDownscale) + " " + SetLever(bloom, "maxIterations", BloomMaxIterations, null);
            Debug.Log("[T181] bloom levers " + LeverReport + " | fields=" + BloomFields);
        }

        /// <summary>
        /// <c>Bloom</c> 의 파라미터 하나를 <b>이름으로</b> 찾아 넣는다 — 넣었으면 «이름=값», 못 넣었으면 «이름=까닭» 을 돌려준다.
        /// <para>
        /// 열거형이면 <paramref name="enumMember"/> 를 <b>이름으로</b> 고른다(0·1 같은 «값» 을 지어내면 URP 가 순서를 바꿀 때 조용히 딴 것이 걸린다).
        /// 없는 멤버면 <b>있는 멤버 목록을 그대로 적어</b> 돌려준다 — 다음 회차가 그 줄만 보면 답을 안다.
        /// </para>
        /// </summary>
        static string SetLever(Bloom bloom, string field, int intValue, string enumMember)
        {
            var f = bloom.GetType().GetField(field, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (f == null) return field + "=none";
            object param = f.GetValue(bloom);
            if (param == null) return field + "=null";
            var vp = param.GetType().GetProperty("value", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (vp == null || !vp.CanWrite) return field + "=novalue";
            object v;
            var t = vp.PropertyType;
            if (t.IsEnum)
            {
                var members = System.Enum.GetNames(t);
                if (System.Array.IndexOf(members, enumMember) < 0) return field + "=enum(" + string.Join("|", members) + ")";
                v = System.Enum.Parse(t, enumMember);
            }
            else if (t == typeof(int)) v = intValue;
            else return field + "=type:" + t.Name;
            var op = param.GetType().GetProperty("overrideState", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (op != null && op.CanWrite) op.SetValue(param, true, null);   // override 를 안 켜면 프로파일 기본값이 그대로다(ⓐ 단언과 같은 함정)
            vp.SetValue(param, v, null);
            return field + "=" + v;
        }

        /// <summary>카메라 쪽 스위치 — 이것을 안 켜면 Volume 이 있어도 아무 일도 안 난다(씬 카메라는 기본이 꺼짐이다).</summary>
        static void Wire(Camera cam)
        {
            if (cam == null) return;
            var data = UiKit.Ensure<UniversalAdditionalCameraData>(cam.gameObject);
            data.renderPostProcessing = true;
        }

        /// <summary>
        /// T181 ⓐ 판정용 스위치 — <b>같은 빌드·같은 런에서</b> Bloom 을 껐다 켜며 fps 를 잰다.
        /// <para>
        /// 왜 필요한가: 지시서 3항이 «WebGL 비용을 반드시 재고 눈에 띄게 떨어지면 Bloom 을 더 얕게» 라고 못 박았는데,
        /// <b>런 사이 절대값은 못 쓴다</b> — 워커 L 이 T129 회차 3 에서 «같은 빌드 두 번이 23.8 ↔ 16.4» 를 보였다(결정 524).
        /// 쓸 수 있는 것은 «한 런 «안» 의 비» 뿐이라, 끄고 켜는 손잡이가 없으면 이 판정은 <b>영영 안 난다</b>.
        /// </para>
        /// 끄는 것은 <b>카메라 스위치 한 줄</b>이다(Volume·프로파일은 그대로 둔다) — 그래야 다시 켤 때 값이 같고,
        /// 게이트가 보는 «Volume 이 하나 · Bloom 이 들어 있다» 계약도 안 흔들린다.
        /// </summary>
        public static bool Enabled
        {
            get
            {
                var cam = App.I != null ? App.I.WorldCamera : null;
                if (cam == null) return false;
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                return data != null && data.renderPostProcessing;
            }
            set
            {
                var cam = App.I != null ? App.I.WorldCamera : null;
                if (cam == null) return;
                var data = UiKit.Ensure<UniversalAdditionalCameraData>(cam.gameObject);
                data.renderPostProcessing = value;
            }
        }
    }
}
