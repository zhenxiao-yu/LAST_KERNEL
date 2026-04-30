Shader "Kamgam/UIToolkit/URP/Color"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0.0, 1.0)) = 1
        
        // Blend modes
        _SrcBlend ("SrcBlend", Int) = 5   // SrcAlpha
        _DstBlend ("DstBlend", Int) = 10  // OneMinusSourceAlpha
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Lighting Off ZWrite Off Cull Off Fog { Mode Off }
        Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
            };

            float4 _Color;
            float _Opacity;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.color.a = v.color.a * _Color.a * _Opacity;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}