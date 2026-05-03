Shader "Markyu/FortStack/CustomPostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _VignettePower ("Vignette Power", Range(0, 3)) = 1.2
        _GrayscaleAmount ("Grayscale Amount", Range(0, 1)) = 1.0
        _ChromaticAmount ("Chromatic Aberration", Range(0, 0.02)) = 0.004
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.06
        _ScanlineFreq ("Scanline Frequency", Range(50, 1000)) = 270
        _ContrastBoost ("Contrast Boost", Range(0.8, 1.5)) = 1.08
        _SaturationBoost ("Saturation Boost", Range(0.8, 2.0)) = 1.15
        _NeonEdgeColor ("Neon Edge Color", Color) = (0, 0.86, 1, 1)
        _NeonEdgeIntensity ("Neon Edge Intensity", Range(0, 1)) = 0.25
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float  _VignettePower;
            float  _GrayscaleAmount;
            float  _ChromaticAmount;
            float  _ScanlineStrength;
            float  _ScanlineFreq;
            float  _ContrastBoost;
            float  _SaturationBoost;
            float4 _NeonEdgeColor;
            float  _NeonEdgeIntensity;

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                float  radDist  = length(centered);

                // 1. Chromatic Aberration — offset R and B channels radially from center
                float2 aberrDir = normalize(centered + 0.0001) * _ChromaticAmount;
                float r = tex2D(_MainTex, i.uv + aberrDir).r;
                float g = tex2D(_MainTex, i.uv).g;
                float b = tex2D(_MainTex, i.uv - aberrDir).b;
                fixed4 col = fixed4(r, g, b, 1.0);

                // 2. Grayscale (Luma Conversion)
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, float3(lum, lum, lum), _GrayscaleAmount);

                // 3. Contrast & Saturation Grade
                col.rgb = saturate((col.rgb - 0.5) * _ContrastBoost + 0.5);
                float lumGraded = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(float3(lumGraded, lumGraded, lumGraded), col.rgb, _SaturationBoost);

                // 4. Dark Vignette (multiplicative)
                float vignette = smoothstep(0.8, 0.2, radDist * _VignettePower);
                col.rgb *= vignette;

                // 5. Neon Edge Glow (additive colored outer ring)
                float edge = smoothstep(0.3, 0.65, radDist) * smoothstep(0.85, 0.55, radDist);
                col.rgb += _NeonEdgeColor.rgb * edge * _NeonEdgeIntensity;

                // 6. CRT Scanlines
                float scanline = 1.0 - _ScanlineStrength * (0.5 + 0.5 * sin(i.uv.y * _ScanlineFreq * 6.28318));
                col.rgb *= scanline;

                return col;
            }
            ENDCG
        }
    }
}
