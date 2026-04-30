Shader "Kamgam/UIToolkit/URP/HSV"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Opacity ("Opacity", Range(0.0, 1.0)) = 1
        _Intensity ("Intensity", Range(0.0, 1.0)) = 1
        // Used by the CustomShader Image to make proper UV mapping and tiling possible.
        [HideInInspector]_UVMinMax("UVMinMax", Vector) = (0, 0, 1, 1)  // xy = min, zw = max
        
        _Hue ("Hue", Range(-1.0, 1.0)) = 0
        _Value ("Value", Range(-1.0, 10.0)) = 0
        _Saturation ("Saturation", Range(-1.0, 1.0)) = 0
        
        // Blend modes
        _SrcBlend ("SrcBlend", Int) = 5   // SrcAlpha
        _DstBlend ("DstBlend", Int) = 10  // OneMinusSourceAlpha
        
        // These are the closest I could get to the original UI Toolkit shader.
        // Sadly there is no simple way to support stencils in UI Toolkit.
        // Source: https://discussions.unity.com/t/ui-toolkit-default-shader-source/1647930/4
        
        [HideInInspector]_StencilCompFront("__scf", Float) = 3.0   // Equal
        [HideInInspector]_StencilPassFront("__spf", Float) = 0.0   // Keep
        [HideInInspector]_StencilZFailFront("__szf", Float) = 1.0  // Zero
        [HideInInspector]_StencilFailFront("__sff", Float) = 0.0   // Keep

        [HideInInspector]_StencilCompBack("__scb", Float) = 8.0    // Always
        [HideInInspector]_StencilPassBack("__spb", Float) = 0.0    // Keep
        [HideInInspector]_StencilZFailBack("__szb", Float) = 2.0   // Replace
        [HideInInspector]_StencilFailBack("__sfb", Float) = 0.0    // Keep

        _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ColorMask [_ColorMask]
        
        Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha

        Stencil
        {
            Ref         0
            ReadMask    255
            WriteMask   255

            CompFront[_StencilCompFront]
            PassFront[_StencilPassFront]
            ZFailFront[_StencilZFailFront]
            FailFront[_StencilFailFront]

            CompBack[_StencilCompBack]
            PassBack[_StencilPassBack]
            ZFailBack[_StencilZFailBack]
            FailBack[_StencilFailBack]
        }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile BLEND_ALPHA BLEND_ADDITIVE
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color  : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _UVMinMax;
            float _Opacity;
            float _Intensity;
            float _Hue;
            float _Value;
            float _Saturation;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                
                return o;
            }

            float3 rgb_to_hsv(float3 c)
            {
                const float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                const float4 p = lerp(float4(c.bg, k.wz), float4(c.gb, k.xy), step(c.b, c.g));
                const float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                const float d = q.x - min(q.w, q.y);
                const float e = 1.0e-4;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 hsv_to_rgb(float3 c)
            {
                const float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                const float3 p = abs(frac(c.xxx + k.xyz) * 6.0 - k.www);
                return c.z * lerp(k.xxx, saturate(p - k.xxx), c.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uvMin = _UVMinMax.xy;
                float2 uvMax = _UVMinMax.zw;
                float2 delta = uvMax - uvMin;

                // Apply tiling and offset
                float2 normalizedUV = (i.uv - uvMin) / delta;
                float2 uvOffset = float2(-_MainTex_ST.z, _MainTex_ST.w);
                float2 normalizedTiledUV = frac(normalizedUV * _MainTex_ST.xy + uvOffset);
                float2 tiledUV = uvMin + (normalizedTiledUV * delta);
                
                fixed4 col = tex2D(_MainTex, tiledUV) * i.color;

                // Greyscale effect
                float3 hsv = rgb_to_hsv(col.rgb);
                hsv.x += _Hue;
                hsv.y += _Saturation;
                hsv.z += _Value;

                float4 color = col;
                color.rgb = lerp(col.rgb, hsv_to_rgb(hsv), _Intensity);
                color.a *= _Opacity;
                
                // Discard pixels below alpha threshold
                if (color.a < 0.001)
                    discard;

                return color;
            }
            ENDCG
        }
    }
}