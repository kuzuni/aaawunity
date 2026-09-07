// dotnet 검사 빌드 전용 스텁 — Unity 의 Unity.TextMeshPro(com.unity.ugui 2.0) 중 이 레포가 쓰는 표면만 서명을 맞춘다.
// Assets 에는 들어가지 않는다(유니티에서는 진짜 TMP 가 잡힌다). 새 API 를 쓰면 여기에도 같은 서명을 더한다.
using UnityEngine;
using UnityEngine.UI;

namespace TMPro
{
    public enum TextAlignmentOptions
    {
        TopLeft = 0x101, Top = 0x102, TopRight = 0x104, TopJustified = 0x108,
        Left = 0x201, Center = 0x202, Right = 0x204, Justified = 0x208,
        BottomLeft = 0x401, Bottom = 0x402, BottomRight = 0x404, BottomJustified = 0x408,
        MidlineLeft = 0x1001, Midline = 0x1002, MidlineRight = 0x1004,
    }
    [System.Flags] public enum FontStyles { Normal = 0, Bold = 1, Italic = 2, Underline = 4 }
    public class TMP_FontAsset : ScriptableObject
    {
        public Material material;
        // T207 ① — 런타임 폰트 애셋 만들기(에디터 없이 굽는 길). 진짜 TMP 의 서명 그대로다:
        //   public static TMP_FontAsset CreateFontAsset(Font font)   (기본 = sampling 90 · padding 9 · SDFAA · 1024² · Dynamic)
        // ⚠ 긴 오버로드(padding·아틀라스 크기를 직접 주는 것)는 아직 안 쓴다 — 스텁에 없는 서명을 쓰면
        //   dotnet 은 초록인데 유니티에서만 죽는다(결정 465 가 남긴 자리). 두께가 잘리면 그때 같이 넓힌다.
        public static TMP_FontAsset CreateFontAsset(Font font) { return font != null ? CreateInstance<TMP_FontAsset>() : null; }
        public bool HasCharacter(char c) { return false; }
        public bool TryAddCharacters(string chars) { return false; }
    }
    public abstract class TMP_Text : MaskableGraphic
    {
        public virtual string text { get; set; }
        public float fontSize { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public bool enableAutoSizing { get; set; }
        public float fontSizeMin { get; set; }
        public float fontSizeMax { get; set; }
        public FontStyles fontStyle { get; set; }
        public bool richText { get; set; }
        public Material fontSharedMaterial { get; set; }
        public TMP_FontAsset font { get; set; }
    }
    public class TextMeshProUGUI : TMP_Text { }
    // 프리팹 입력칸 — UiKit.Adopt 가 이것을 떼고 uGUI InputField 로 갈아 끼운다(T96-profile 2단계)
    public class TMP_InputField : Selectable
    {
        public enum LineType { SingleLine = 0, MultiLineSubmit = 1, MultiLineNewline = 2 }
        public TMP_Text textComponent { get; set; }
        public Graphic placeholder { get; set; }
        public int characterLimit { get; set; }
        public LineType lineType { get; set; }
    }
}
