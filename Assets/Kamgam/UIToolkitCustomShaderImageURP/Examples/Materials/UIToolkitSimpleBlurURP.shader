Shader "Kamgam/UIToolkit/URP/SimpleBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0.0,1.0)) = 1
        
        _BlurStrength("Blur Strength", Range(0, 30)) = 3
        [KeywordEnum(Low, Medium, High)]
        _Samples("Sample Amount", Float) = 1
        _AdditiveColor("Additive Color", Color) = (0, 0, 0, 0)
        
        // Blend modes
        _SrcBlend ("SrcBlend", Int) = 5   // SrcAlpha
        _DstBlend ("DstBlend", Int) = 10  // OneMinusSourceAlpha
    }
    
    HLSLINCLUDE

    #include "UnityCG.cginc"

    #if _SAMPLES_LOW
        #define SAMPLES 1
    #elif _SAMPLES_MEDIUM
        #define SAMPLES 2
    #else
        #define SAMPLES 3
    #endif
    
    struct appdata_t
    {
        float4 vertex   : POSITION;
        float2 uv       : TEXCOORD0;
        float4 color    : COLOR;
    };

    struct v2f
    {
        float2 uv       : TEXCOORD0;
        float4 vertex   : SV_POSITION;
        float4 color    : COLOR;
    };

    sampler2D _MainTex;
    float4 _MainTex_ST; 
    float4 _MainTex_TexelSize;

    sampler2D _RenderTex;
    float4 _RenderTex_ST;
    
    float4 _Color;
    float4 _AdditiveColor;
    float _BlurStrength;
    float _Opacity;

    v2f vert (appdata_t v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.color = v.color * _Color;
        return o;
    }
    
    half4 blur(v2f input) : SV_Target
    {
        float2 uv = input.uv;
        float2 texel = _MainTex_TexelSize.xy * _BlurStrength;

        // star form, blur with a sample for every step
        half4 color = half4(0, 0, 0, 0);

        // 2 pass blur would be much faster and better quality but for demo purposes this is fine.


        if (_BlurStrength < 0.01)
        {
            color = tex2D(_MainTex, uv);
            color.a *= _Opacity;
            color += _AdditiveColor;

            // Discard pixels below alpha threshold
            if (color.a < 0.001)
                    discard;

            return color;
        }
        
#if _SAMPLES_LOW
        // 3x3 Gaussian blur kernel
        float kernel[9] = { 0.0625,  0.125,  0.0625,  
                            0.125,   0.25,   0.125,  
                            0.0625,  0.125,  0.0625 };
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                float2 offset = float2(x-1, y-1) * texel;
                float weight = kernel[x + y * 3];
                color += tex2D(_MainTex, uv + offset) * weight;
            }
        }

#elif _SAMPLES_MEDIUM
        // 5x5 Gaussian blur kernel
        float kernel[25] = { 0.0039,  0.0156,  0.0234,  0.0156,  0.0039,  
                             0.0156,  0.0625,  0.0938,  0.0625,  0.0156,  
                             0.0234,  0.0938,  0.1406,  0.0938,  0.0234,  
                             0.0156,  0.0625,  0.0938,  0.0625,  0.0156,  
                             0.0039,  0.0156,  0.0234,  0.0156,  0.0039 };
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                float2 offset = float2(x-2, y-2) * texel;
                float weight = kernel[x + y * 5];
                color += tex2D(_MainTex, uv + offset) * weight;
            }
        }

#else
        // 11x11 Gaussian blur kernel
        float kernel[121] = {
            0.00008, 0.00024, 0.00057, 0.00107, 0.00156, 0.00177, 0.00156, 0.00107, 0.00057, 0.00024, 0.00008,
            0.00024, 0.00074, 0.00177, 0.00330, 0.00480, 0.00544, 0.00480, 0.00330, 0.00177, 0.00074, 0.00024,
            0.00057, 0.00177, 0.00424, 0.00792, 0.01153, 0.01306, 0.01153, 0.00792, 0.00424, 0.00177, 0.00057,
            0.00107, 0.00330, 0.00792, 0.01480, 0.02153, 0.02440, 0.02153, 0.01480, 0.00792, 0.00330, 0.00107,
            0.00156, 0.00480, 0.01153, 0.02153, 0.03133, 0.03550, 0.03133, 0.02153, 0.01153, 0.00480, 0.00156,
            0.00177, 0.00544, 0.01306, 0.02440, 0.03550, 0.04024, 0.03550, 0.02440, 0.01306, 0.00544, 0.00177,
            0.00156, 0.00480, 0.01153, 0.02153, 0.03133, 0.03550, 0.03133, 0.02153, 0.01153, 0.00480, 0.00156,
            0.00107, 0.00330, 0.00792, 0.01480, 0.02153, 0.02440, 0.02153, 0.01480, 0.00792, 0.00330, 0.00107,
            0.00057, 0.00177, 0.00424, 0.00792, 0.01153, 0.01306, 0.01153, 0.00792, 0.00424, 0.00177, 0.00057,
            0.00024, 0.00074, 0.00177, 0.00330, 0.00480, 0.00544, 0.00480, 0.00330, 0.00177, 0.00074, 0.00024,
            0.00008, 0.00024, 0.00057, 0.00107, 0.00156, 0.00177, 0.00156, 0.00107, 0.00057, 0.00024, 0.00008
        };
        for (int x = 0; x < 11; x++)
        {
            for (int y = 0; y < 11; y++)
            {
                float2 offset = float2(x-5, y-5) * texel;
                float weight = kernel[x + y * 11];
                color += tex2D(_MainTex, uv + offset) * weight;
            }
        }
#endif
        
        color.a *= _Opacity;
        color += _AdditiveColor;

        // Discard pixels below alpha threshold
        if (color.a < 0.001)
                discard;
        
        return color;
    }
    ENDHLSL

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Lighting Off ZWrite Off Cull Off Fog { Mode Off }
        
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            Name "Blur Horizontal"

            HLSLPROGRAM

            #pragma multi_compile _SAMPLES_LOW _SAMPLES_MEDIUM _SAMPLES_HIGH
            #pragma vertex vert
            #pragma fragment blur

            ENDHLSL
        }
    }
}