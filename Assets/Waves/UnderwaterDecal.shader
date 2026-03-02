Shader "Custom/UnderwaterDecal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.8

        [Header(Water Tint)]
        _WaterTint ("Water Tint Color", Color) = (0, 0.3, 0.5, 1)
        _WaterTintStrength ("Water Tint Strength", Range(0, 1)) = 0.4

        [Header(Depth Fade)]
        _DepthDarken ("Depth Darken", Range(0, 1)) = 0.3
        _DepthDesaturate ("Depth Desaturate", Range(0, 1)) = 0.2

        [Header(Wave Distortion)]
        _WaveDistortion ("Wave Distortion Amount", Range(0, 0.1)) = 0.02
        _WaveDistortionSpeed ("Distortion Speed", Range(0, 3)) = 1.0
        _WaveDistortionScale ("Distortion Scale", Range(0.1, 10)) = 2.0

        [Header(Edge Fade)]
        _EdgeFade ("Edge Fade (soften borders)", Range(0, 0.5)) = 0.1

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-5" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
                float4 vertColor  : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _Opacity;
                float4 _WaterTint;
                float  _WaterTintStrength;
                float  _DepthDarken;
                float  _DepthDesaturate;
                float  _WaveDistortion;
                float  _WaveDistortionSpeed;
                float  _WaveDistortionScale;
                float  _EdgeFade;
                float  _Cull;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = worldPos;
                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                OUT.vertColor = IN.color;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Wave distortion on UV (makes it look like it's under moving water)
                float2 distortUV = IN.positionWS.xz * _WaveDistortionScale;
                float t = _Time.y * _WaveDistortionSpeed;
                float2 distort = float2(
                    sin(distortUV.x * 2.7 + t * 1.1 + sin(distortUV.y * 1.9 + t * 0.7)) * 0.5,
                    cos(distortUV.y * 3.1 + t * 0.9 + sin(distortUV.x * 2.3 + t * 1.3)) * 0.5
                ) * _WaveDistortion;

                float2 uv = IN.uv + distort;

                // Sample texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                texColor *= _Color * IN.vertColor;

                // Edge fade (soft borders using UV distance from edges)
                float2 edgeDist = min(uv, 1.0 - uv);
                float edgeFade = _EdgeFade > 0.001
                    ? saturate(min(edgeDist.x, edgeDist.y) / _EdgeFade)
                    : 1.0;

                // Water tint (blend texture color toward water color)
                float3 col = texColor.rgb;
                col = lerp(col, col * _WaterTint.rgb, _WaterTintStrength);

                // Depth darken (simulate being underwater — darken)
                col *= 1.0 - _DepthDarken;

                // Depth desaturate (water absorbs color)
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(col, float3(lum, lum, lum), _DepthDesaturate);

                // Final alpha
                float alpha = texColor.a * _Opacity * edgeFade;

                col = MixFog(col, IN.fogFactor);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
