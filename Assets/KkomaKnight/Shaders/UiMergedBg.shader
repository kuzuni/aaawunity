// T225 — 배경 «무늬 + 위 그라데이션 + 아래 그라데이션» 세 겹을 한 겹으로(주인 2026-09-10 «로비 최적화는 해봐라»).
//
// ⚑ 왜 한 겹으로 합쳐도 되나 — over 연산의 결합법칙이고, 그것을 **이 레포의 실제 파일·tint·화면 높이로 재서** 확인했다:
//    세 겹 따로 얹기 ↔ 한 겹으로 합쳐 한 번 얹기 = 최대 차 2.8e-17 = 0.000000 LSB(8비트) · 결정 1090 · §2 T225 6항 ⓒ.
//
// ⚑ 이 레포의 **첫 자작 셰이더**다(나머지 28개는 전부 TMP·Layer Lab·AllIn1). 그래서 뼈대를 새로 쓰지 않고
//    이 프로젝트에서 **이미 컴파일되는 것이 확인된** AllIn1SpriteShaderUiMask 의 UI SubShader 블록을 그대로 옮겨 왔다
//    (Tags·Cull·ZWrite·ZTest·ColorMask·Stencil) — 워커는 셰이더를 로컬에서 컴파일해 볼 수 없다(§2 T225 6항 ⓓ①).
//
// ⚠ 프리멀티플라이드로 내보내고 `Blend One OneMinusSrcAlpha` 로 얹는다 — 스트레이트 알파로 내보내면
//    합성 식에 `/a` 가 들어가는데, 무늬 알파가 3/255 라 그 나눗셈이 오차를 키운다. 프리멀티플라이드는 나눗셈이 아예 없다.
//
// ⚠ 클립 사각형(_ClipRect)은 **일부러 안 쓴다** — 쓰려면 UNITY_UI_CLIP_RECT 키워드를 같이 걸어야 하고,
//    안 걸면 기본값 0 이 화면을 통째로 잘라 버린다. 이 겹이 RectMask2D 안으로 들어가는 날 그 키워드를 같이 넣는다.
Shader "KkomaKnight/UiMergedBg"
{
    Properties
    {
        [PerRendererData] _MainTex ("Pattern (tiled · Repeat)", 2D) = "white" {}
        _TopTex ("Gradient Top", 2D) = "white" {}
        _BotTex ("Gradient Bottom", 2D) = "white" {}
        _PatternColor ("Pattern Tint", Color) = (1,1,1,0.011765)
        _TopColor ("Top Tint", Color) = (1,1,1,0.12)
        _BotColor ("Bottom Tint", Color) = (0.203922,0.105882,0.098039,0.18)
        // RawImage.uvRect 와 같은 뜻 — uv = texcoord * Tile + Offset (UiKit.MergedBg 가 매 프레임 넣는다)
        _PatternTile ("Pattern Tile (xy)", Vector) = (1,1,0,0)
        _PatternOffset ("Pattern Offset (xy)", Vector) = (0,0,0,0)
        // 조각(Sprite)이 아틀라스 안에 들어 있어도 맞도록 «그 조각이 텍스처의 어느 사각형인가»(xy = 왼아래 · zw = 크기 · 0~1)를 받는다.
        // 지금 둘 다 단독 텍스처라 (0,0,1,1) 이지만, 언젠가 묶이면 이 값만 달라지고 식은 그대로다.
        _TopRect ("Top Sprite Rect (xywh)", Vector) = (0,0,1,1)
        _BotRect ("Bottom Sprite Rect (xywh)", Vector) = (0,0,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _TopTex;
            sampler2D _BotTex;
            float4 _PatternColor;
            float4 _TopColor;
            float4 _BotColor;
            float4 _PatternTile;
            float4 _PatternOffset;
            float4 _TopRect;
            float4 _BotRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            // s over d — 둘 다 **프리멀티플라이드**(rgb 에 이미 a 가 곱해져 있다). 나눗셈이 없다.
            float4 PreOver(float4 s, float4 d)
            {
                return float4(s.rgb + d.rgb * (1.0 - s.a), s.a + d.a * (1.0 - s.a));
            }

            // 스트레이트(tint × 조각 알파) → 프리멀티플라이드. 조각들은 RGB 가 순백이라 rgb 는 tint 그대로다.
            float4 Pre(float4 straight)
            {
                return float4(straight.rgb * straight.a, straight.a);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 pu = i.texcoord * _PatternTile.xy + _PatternOffset.xy;
                float4 p = Pre(tex2D(_MainTex, pu) * _PatternColor);
                // 두 곡선은 폭 4px 세로 띠다 — 가로는 한가운데 한 줄만 읽고(가로로 늘여도 값이 같다) 세로만 사각형 높이에 맞춘다.
                float2 tu = float2(_TopRect.x + _TopRect.z * 0.5, _TopRect.y + _TopRect.w * i.texcoord.y);
                float2 bu = float2(_BotRect.x + _BotRect.z * 0.5, _BotRect.y + _BotRect.w * i.texcoord.y);
                float4 t = Pre(tex2D(_TopTex, tu) * _TopColor);
                float4 b = Pre(tex2D(_BotTex, bu) * _BotColor);

                // 쌓이는 차례 = 바탕 → 무늬 → 위 그라데이션 → 아래 그라데이션(UiKit.Gradient 의 층 순서 · 결정 171)
                float4 c = PreOver(t, p);
                c = PreOver(b, c);

                // 그래픽 자신의 색(정점 색) — 이 겹은 흰색으로 세우므로 보통 1 이다.
                c.rgb *= i.color.rgb * i.color.a;
                c.a *= i.color.a;
                return c;
            }
            ENDCG
        }
    }
}
