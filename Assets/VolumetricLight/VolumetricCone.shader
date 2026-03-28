Shader "Custom/VolumetricCone"
{
    Properties
    {
        _Color ("Light Color", Color) = (1, 0.95, 0.8, 1)
        _Intensity ("Intensity", Range(0, 5)) = 1.0
        _FalloffExp ("Edge Falloff", Range(0.5, 8)) = 2.0
        _NoiseTex ("Noise Texture (Optional)", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Range(0, 5)) = 1.0
        _NoiseSpeed ("Noise Scroll Speed", Range(0, 2)) = 0.3
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.3
        _SoftFactor ("Soft Particle Factor", Range(0.01, 3)) = 1.0
        _RenderStart ("Render Start (0=base)", Range(0, 1)) = 0.0
        _RenderEnd ("Render End (1=tip)", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "VolumetricCone"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // vertex color: R = normalizedHeight (0=base, 1=tip)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float normalizedHeight : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _FalloffExp;
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseStrength;
                float _SoftFactor;
                float _RenderStart;
                float _RenderEnd;
                float4 _NoiseTex_ST;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.normalizedHeight = input.color.r;

                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float h = input.normalizedHeight;

                // Clip pixels outside the render range
                float renderStart = _RenderStart;
                float renderEnd = _RenderEnd;
                clip(h - renderStart);
                clip(renderEnd - h);

                // Remap h to 0-1 within the visible range
                float range = renderEnd - renderStart;
                float remapped = (h - renderStart) / max(range, 0.001);

                // Radial falloff: edges of the cone are more transparent
                float2 centeredUV = input.uv * 2.0 - 1.0;
                float radialDist = length(centeredUV);
                float radialFalloff = 1.0 - saturate(pow(radialDist, _FalloffExp));

                // Height-based fade: fade out near tip and base edges
                float baseFade = smoothstep(0.0, 0.15, remapped);
                float tipFade = 1.0 - smoothstep(0.85, 1.0, remapped);
                float heightFade = baseFade * tipFade;

                // Optional noise for variation
                float2 noiseUV = input.positionWS.xz * _NoiseScale + _Time.y * _NoiseSpeed;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                noise = lerp(1.0, noise, _NoiseStrength);

                // Soft particles (depth fade)
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = LinearEyeDepth(
                    SampleSceneDepth(screenUV),
                    _ZBufferParams
                );
                float fragDepth = input.screenPos.w;
                float depthFade = saturate((sceneDepth - fragDepth) / _SoftFactor);

                // Final alpha
                float alpha = radialFalloff * heightFade * noise * depthFade * _Intensity * _Color.a;
                alpha = saturate(alpha);

                half3 color = _Color.rgb * _Intensity;

                // Apply fog
                color = MixFog(color, input.fogFactor);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
