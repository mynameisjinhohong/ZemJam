Shader "Custom/UI/WaveDistortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.1)) = 0.02
        _WaveFrequency ("Wave Frequency", Range(0, 30)) = 10
        _WaveSpeed ("Wave Speed", Range(0, 10)) = 2

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex      : SV_POSITION;
                fixed4 color       : COLOR;
                float2 texcoord    : TEXCOORD0;
                float4 worldPos    : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            fixed4    _TextureSampleAdd;
            float4    _ClipRect;
            float     _WaveAmplitude;
            float     _WaveFrequency;
            float     _WaveSpeed;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color    = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                // X축: Y 위치 기반 수평 왜곡
                uv.x += sin(uv.y * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveAmplitude;
                // Y축: X 위치 기반 수직 왜곡 (보조, 강도 낮게)
                uv.y += sin(uv.x * _WaveFrequency + _Time.y * _WaveSpeed * 0.8) * _WaveAmplitude * 0.5;

                fixed4 color = (tex2D(_MainTex, uv) + _TextureSampleAdd) * i.color;
                color.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                clip(color.a - 0.001);
                return color;
            }
            ENDCG
        }
    }
}
