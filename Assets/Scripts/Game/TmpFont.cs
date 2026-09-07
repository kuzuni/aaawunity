using TMPro;
using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T207 ① — 주인 글꼴(Jua)의 <b>TMP 폰트 애셋</b>을 <b>코드로</b> 만든다(주인 승인 2026-09-07 17:2X «tmpro로 아웃라인 해야지 진짜 메테리얼로»).
    /// <para>
    /// <b>왜 코드인가</b> — 폰트 애셋은 보통 에디터에서 굽는 <c>.asset</c> 인데 워커에게는 유니티 에디터가 없다.
    /// 손으로 YAML 을 지어내면 씬이 통째로 안 열릴 수 있다(워커 L 이 T181 에서 그 길을 피하고 «코드로» 세운 전례 · 결정 458).
    /// TMP 는 <see cref="TMP_FontAsset.CreateFontAsset(Font)"/> 로 <b>런타임에</b> 같은 것을 만들어 주고, 그렇게 만든 애셋은
    /// 기본이 <b>Dynamic</b>(쓰는 글리프를 그때 굽는다)이라 한글 11,172자를 미리 굽지 않아도 된다 — 지시서 3항 ① 이 요구한 그 성질이다.
    /// </para>
    /// <para>
    /// <b>이 파일이 하는 일은 «만들어 주는 것» 뿐이다.</b> 화면 글자를 TMP 로 갈아 끼우는 일(②)과 지표 차이 정리(③)는
    /// 이 단계가 초록이 된 뒤 다른 회차가 한다 — ① 에서 죽으면 ②③ 을 아예 안 하기로 지시서가 정해 두었다.
    /// </para>
    /// <b>남은 위험(이 단계가 재는 것)</b> — 동적 글리프 굽기는 네이티브 FreeType 을 타므로 <b>에디터에서 되고 WebGL 에서 안 될 수 있다</b>.
    /// 그래서 판정이 둘이다: PlayMode(<c>TmpFontProbeTests</c>)와 <b>배포 빌드 스모크</b>(<c>tools/webgl_smoke.sh</c>).
    /// </summary>
    public static class TmpFont
    {
        /// <summary>Jua 글꼴의 카탈로그 키(uGUI <c>Text</c> 가 쓰던 것과 같은 글꼴이다 — 화면 인상이 안 바뀐다).</summary>
        public const string FontKey = "font.ui";
        /// <summary>SDF 아웃라인 두께(0~1 의 <b>SDF 비율</b> · 픽셀이 아니다 · 지시서 4항 ⓑ). 글자 크기·스케일에 저절로 비례한다.</summary>
        public const float OutlineWidth = 0.20f;
        /// <summary>TMP SDF 셰이더의 아웃라인 두께·색 프로퍼티 이름(머티리얼로 두르는 «진짜» 아웃라인이 이 둘이다).</summary>
        public const string OutlineWidthProp = "_OutlineWidth", OutlineColorProp = "_OutlineColor";

        static TMP_FontAsset _asset;

        /// <summary>
        /// Jua TMP 폰트 애셋(한 번 만들고 다시 쓴다 · 만들 수 없으면 <c>null</c>).
        /// <paramref name="ttf"/> 를 안 주면 카탈로그의 <see cref="FontKey"/> 에서 가져온다.
        /// </summary>
        public static TMP_FontAsset Get(Font ttf = null)
        {
            if (_asset != null) return _asset;
            var src = ttf != null ? ttf : (App.I != null && App.I.Assets != null ? App.I.Assets.Font(FontKey) : null);
            if (src == null) { Debug.LogWarning("[T207] Jua 글꼴(" + FontKey + ")을 못 찾았다 — TMP 폰트 애셋을 만들 수 없다"); return null; }
            _asset = TMP_FontAsset.CreateFontAsset(src);
            if (_asset == null) { Debug.LogWarning("[T207] TMP_FontAsset.CreateFontAsset 이 null 을 돌려줬다(이 플랫폼에서 동적 굽기가 안 된다는 뜻일 수 있다)"); return null; }
            _asset.name = "Jua SDF (Runtime)";
            return _asset;
        }

        /// <summary>이 글자들이 실제로 구워졌는가 — 동적 굽기가 되는 플랫폼인지 가르는 유일한 신호다(두부 □ 는 «글리프가 없다» 다).</summary>
        public static bool HasAll(TMP_FontAsset asset, string chars)
        {
            if (asset == null || string.IsNullOrEmpty(chars)) return false;
            asset.TryAddCharacters(chars);
            foreach (var c in chars) if (!asset.HasCharacter(c)) return false;
            return true;
        }

        /// <summary>그 애셋의 공유 머티리얼에 «진짜» 아웃라인을 두른다(자리마다 인스턴스를 만들지 않는다 — 지시서 4항 ⓒ).</summary>
        public static bool SetOutline(TMP_FontAsset asset, Color color, float width = OutlineWidth)
        {
            var mat = asset != null ? asset.material : null;
            if (mat == null || !mat.HasProperty(OutlineWidthProp)) return false;
            mat.SetFloat(OutlineWidthProp, width);
            if (mat.HasProperty(OutlineColorProp)) mat.SetColor(OutlineColorProp, color);
            return true;
        }
    }
}
