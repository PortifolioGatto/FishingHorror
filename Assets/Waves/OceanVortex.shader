Shader "Custom/OceanVortex"
{
    Properties
    {
        [Header(Water Colors)]
        _DeepColor ("Deep Color", Color) = (0, 0.1, 0.25, 1)
        _ShallowColor ("Shallow Color", Color) = (0, 0.4, 0.6, 1)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _VortexCoreColor ("Vortex Core Color (abyss)", Color) = (0, 0.02, 0.05, 1)

        [Header(Waves on the Vortex surface)]
        _WaveHeight ("Wave Height", Float) = 0.5
        _WaveSpeed ("Wave Speed", Float) = 1
        _FoamThreshold ("Foam Threshold", Float) = 0.4
        _FoamIntensity ("Foam Intensity", Float) = 1.5

        [Header(Vortex Shape)]
        _VortexRadius ("Vortex Radius (outer edge)", Float) = 50
        _VortexDepth ("Vortex Depth (how deep the funnel goes)", Float) = 20
        _VortexInnerRadius ("Inner Hole Radius", Range(0.01, 0.5)) = 0.08
        _VortexFunnelPower ("Funnel Curve Power", Range(0.5, 6.0)) = 2.0

        [Header(Vortex Rotation)]
        _VortexSpinSpeed ("Spin Speed", Float) = 1.0
        _VortexSpiralTightness ("Spiral Tightness", Range(0.5, 20.0)) = 5.0
        _VortexSpiralHeight ("Spiral Wave Height", Range(0.0, 5.0)) = 1.5
        _VortexSpiralCount ("Spiral Arms", Range(1, 8)) = 3

        [Header(Vortex Turbulence)]
        _VortexTurbulence ("Turbulence Strength", Range(0.0, 3.0)) = 0.8
        _VortexTurbulenceScale ("Turbulence Scale", Range(0.1, 10.0)) = 2.0

        [Header(Vortex Pull  XZ displacement)]
        _VortexPullStrength ("Inward Pull Strength", Range(0.0, 5.0)) = 1.0

        [Header(Celestial Reflection)]
        _SunReflectionIntensity ("Sun Reflection Intensity", Range(0, 5)) = 1.5
        _SunReflectionSize ("Sun Reflection Size", Range(1, 2048)) = 256
        _SunReflectionColor ("Sun Reflection Color", Color) = (1, 0.9, 0.7, 1)
        _MoonReflectionIntensity ("Moon Reflection Intensity", Range(0, 3)) = 0.6
        _MoonReflectionSize ("Moon Reflection Size", Range(1, 2048)) = 128
        _MoonReflectionColor ("Moon Reflection Color", Color) = (0.6, 0.7, 1.0, 1)
        _ReflectionDistortion ("Reflection Distortion", Range(0, 1)) = 0.5

        [Header(Underwater Lights)]
        _SubsurfaceIntensity ("Subsurface Glow Intensity", Range(0, 5)) = 1.5
        _SubsurfaceAbsorption ("Water Absorption (depth fade)", Range(0.01, 2.0)) = 0.3
        _SubsurfaceTint ("Water Absorption Tint", Color) = (0.1, 0.5, 0.4, 1)
        _SubsurfaceCausticScale ("Caustic Pattern Scale", Range(0.1, 10.0)) = 2.0
        _SubsurfaceCausticSpeed ("Caustic Anim Speed", Range(0, 3)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float3 positionWS    : TEXCOORD0;
                float3 normalWS      : TEXCOORD1;
                float  fogFactor     : TEXCOORD2;
                float  vortexBlend   : TEXCOORD3; // 0=outer edge, 1=core
                float  foamMask      : TEXCOORD4;
                float  gerstnerHeight: TEXCOORD5;
                float4 screenPos     : TEXCOORD6;
                float2 vortexDistort : TEXCOORD7; // xy = spiral pull direction for light distortion
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FoamColor;
                float4 _VortexCoreColor;

                float  _WaveHeight;
                float  _WaveSpeed;
                float  _FoamThreshold;
                float  _FoamIntensity;

                float  _VortexRadius;
                float  _VortexDepth;
                float  _VortexInnerRadius;
                float  _VortexFunnelPower;

                float  _VortexSpinSpeed;
                float  _VortexSpiralTightness;
                float  _VortexSpiralHeight;
                float  _VortexSpiralCount;

                float  _VortexTurbulence;
                float  _VortexTurbulenceScale;

                float  _VortexPullStrength;

                float  _SunReflectionIntensity;
                float  _SunReflectionSize;
                float4 _SunReflectionColor;
                float  _MoonReflectionIntensity;
                float  _MoonReflectionSize;
                float4 _MoonReflectionColor;
                float  _ReflectionDistortion;

                float  _SubsurfaceIntensity;
                float  _SubsurfaceAbsorption;
                float4 _SubsurfaceTint;
                float  _SubsurfaceCausticScale;
                float  _SubsurfaceCausticSpeed;
            CBUFFER_END

            // Globals from DayNightCycle
            float4 _CelestialSunDir;
            float4 _CelestialMoonDir;
            float  _CelestialDayFactor;

            // Globals from UnderwaterLightManager
            float  _UnderwaterLightCount;
            float4 _UnderwaterLightPositions[8]; // xyz = world pos, w = outerRadius
            float4 _UnderwaterLightColors[8];    // rgb = color, a = intensity
            float4 _UnderwaterLightParams[8];    // x = innerRadius, y = concentration

            // =============================================
            // NOISE (simple gradient noise for turbulence)
            // =============================================
            float hash2D(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = hash2D(i);
                float b = hash2D(i + float2(1, 0));
                float c = hash2D(i + float2(0, 1));
                float d = hash2D(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm2D(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < octaves; i++)
                {
                    value += noise2D(p) * amplitude;
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            // =============================================
            // GERSTNER (surface ripples on the vortex)
            // =============================================
            float GerstnerWave(float2 dir, float steepness, float wavelength, float speed,
                               float phase, float3 samplePos, out float dX, out float dZ)
            {
                float k = 2.0 * PI / wavelength;
                float c = sqrt(9.8 / k) * speed;
                float2 d = normalize(dir);
                float f = k * dot(d, samplePos.xz) - c * _Time.y + phase;

                float deriv = k * steepness * cos(f);
                dX = deriv * d.x;
                dZ = deriv * d.y;

                return sin(f) * steepness;
            }

            // =============================================
            // UNDERWATER LIGHTS (bioluminescence)
            // =============================================
            float CausticPattern(float2 uv, float time)
            {
                float2 p1 = uv * 1.0 + float2(time * 0.8, time * 0.3);
                float2 p2 = uv * 1.4 + float2(-time * 0.5, time * 0.7);
                float v1 = sin(p1.x * 3.1 + sin(p1.y * 2.7)) * 0.5 + 0.5;
                float v2 = sin(p2.x * 2.3 + sin(p2.y * 3.9)) * 0.5 + 0.5;
                float v3 = sin((p1.x + p2.y) * 1.7) * 0.5 + 0.5;
                return saturate(v1 * v2 + v3 * 0.3);
            }

            float3 ComputeUnderwaterLights(float3 surfacePos, float3 viewDir,
                                          float sceneDepthEye, float waterDepthEye,
                                          float2 vortexDistort, float vortexBlend)
            {
                float3 totalGlow = 0;
                int count = (int)_UnderwaterLightCount;
                float3 camPos = _WorldSpaceCameraPos;

                for (int i = 0; i < count; i++)
                {
                    float3 lightPos      = _UnderwaterLightPositions[i].xyz;
                    float  outerRadius   = _UnderwaterLightPositions[i].w;
                    float3 color         = _UnderwaterLightColors[i].rgb;
                    float  intensity     = _UnderwaterLightColors[i].a;
                    float  innerRadius   = _UnderwaterLightParams[i].x;
                    float  concentration = _UnderwaterLightParams[i].y;

                    // ---- VORTEX DISTORTION ----
                    // The vortex warps the apparent position of the light
                    // as seen through the water. The spiral pull drags the
                    // light's image along with the current.
                    float3 distortedSurfacePos = surfacePos;
                    distortedSurfacePos.xz += vortexDistort * vortexBlend * 3.0;

                    // ---- RAY THROUGH WATER ----
                    float3 rayDir = -viewDir;
                    float3 toLight = lightPos - distortedSurfacePos;
                    float t = max(dot(toLight, rayDir), 0.0);
                    float3 closestPoint = distortedSurfacePos + rayDir * t;
                    float closestDist = length(lightPos - closestPoint);

                    // ---- DUAL RADIUS GLOW ----
                    // Vortex stretches the inner radius along the spiral direction
                    float vortexStretch = 1.0 + vortexBlend * 1.5;
                    float effectiveInner = innerRadius * vortexStretch;
                    float effectiveOuter = outerRadius * (1.0 + vortexBlend * 0.5);

                    float innerGlow = 1.0 - saturate(closestDist / max(effectiveInner, 0.001));
                    innerGlow = innerGlow * innerGlow * innerGlow;

                    float outerGlow = 1.0 - saturate(closestDist / max(effectiveOuter, 0.01));
                    outerGlow = outerGlow * outerGlow * (3.0 - 2.0 * outerGlow);

                    float glow = lerp(outerGlow, innerGlow * 3.0, concentration);

                    // ---- OCCLUSION FROM SCENE GEOMETRY ----
                    float camToLightDist = length(lightPos - camPos);
                    float occlusionFade = saturate((sceneDepthEye - camToLightDist) * 0.5 + 1.0);
                    float floorBlock = saturate((sceneDepthEye - waterDepthEye) / max(outerRadius * 0.5, 1.0));
                    float occlusion = min(occlusionFade, floorBlock);
                    occlusion = (sceneDepthEye > 900.0) ? 1.0 : occlusion;

                    // ---- DEPTH ABSORPTION ----
                    float waterDist = length(toLight);
                    float absorption = exp(-waterDist * _SubsurfaceAbsorption * 0.05);

                    // ---- COLOR SHIFT ----
                    float depth = max(surfacePos.y - lightPos.y, 0.0);
                    float3 waterAbsorb = float3(
                        exp(-depth * 0.06),
                        exp(-depth * 0.02),
                        exp(-depth * 0.005)
                    );
                    float3 tintedColor = color * lerp(waterAbsorb, _SubsurfaceTint.rgb, 0.2);

                    // ---- CAUSTICS (also distorted by vortex) ----
                    float2 causticUV = (surfacePos.xz + vortexDistort * vortexBlend) 
                                     * _SubsurfaceCausticScale / max(outerRadius, 1.0);
                    float caustic = CausticPattern(causticUV, _Time.y * _SubsurfaceCausticSpeed);
                    float causticContrib = lerp(0.7, 1.0, caustic * outerGlow);

                    totalGlow += tintedColor * intensity * glow * absorption * causticContrib * occlusion;
                }

                return totalGlow * _SubsurfaceIntensity;
            }

            // =============================================
            // VORTEX VERTEX DISPLACEMENT
            // =============================================

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                // Object center in world space = vortex center
                float3 vortexCenter = TransformObjectToWorld(float3(0, 0, 0));

                // Vector from vortex center to this vertex (XZ plane)
                float2 delta = worldPos.xz - vortexCenter.xz;
                float dist = length(delta);
                float2 dir = dist > 0.001 ? delta / dist : float2(0, 0);

                // Normalized distance (0 = center, 1 = outer edge)
                float normDist = saturate(dist / max(_VortexRadius, 0.01));

                // ---- FUNNEL DEPRESSION ----
                // Funnel curve: deeper toward center, flat at edge
                // innerRadius creates the hole at the bottom
                float funnelT = 1.0 - saturate((normDist - _VortexInnerRadius) / (1.0 - _VortexInnerRadius));
                float funnel = pow(funnelT, _VortexFunnelPower);
                float depression = -funnel * _VortexDepth;

                // ---- SPIRAL RIDGES ----
                // Angle from center
                float angle = atan2(delta.y, delta.x);
                // Spiral: angle + distance creates spiral arms that rotate over time
                float spiralPhase = angle * _VortexSpiralCount
                                  + dist * _VortexSpiralTightness / max(_VortexRadius, 1.0)
                                  - _Time.y * _VortexSpinSpeed;
                float spiral = sin(spiralPhase) * 0.5 + 0.5;
                // Spiral is stronger toward center, fades at edge
                float spiralStrength = (1.0 - normDist) * _VortexSpiralHeight;
                float spiralDisp = spiral * spiralStrength;

                // ---- TURBULENCE (noise-based churn) ----
                float2 turbUV = worldPos.xz * _VortexTurbulenceScale / max(_VortexRadius, 1.0);
                turbUV += float2(_Time.y * 0.3, _Time.y * 0.2);
                float turb = (fbm2D(turbUV, 3) - 0.5) * 2.0;
                // Turbulence stronger near the edge (churning water), less in the core
                float turbDisp = turb * _VortexTurbulence * normDist;

                // ---- SURFACE GERSTNER (waves pulled toward vortex center) ----
                float3 samplePos = worldPos;
                samplePos.x += sin(_Time.y * 0.05) * 2.0;
                samplePos.z += cos(_Time.y * 0.037) * 2.0;

                float totalDx = 0.0, totalDz = 0.0;
                float dxT, dzT;
                float gerstner = 0.0;

                // Attenuate Gerstner toward center (smooth water in the deep funnel)
                float gerstnerAtten = smoothstep(0.0, 0.4, normDist);

                // Inward direction: from this vertex toward vortex center
                float2 inwardDir = -dir; // dir points outward, so negate

                // How much to bend waves inward (0 at edge = ocean-like, 1 at center = fully inward)
                float bendAmount = 1.0 - normDist;

                // 4 waves matching the ocean, but directions bend toward center
                // Each wave's base direction is lerped toward inwardDir
                float2 baseDir0 = float2(0.8, 0.2);
                float2 baseDir1 = float2(-0.4, 0.9);
                float2 baseDir2 = float2(0.6, -0.7);
                float2 baseDir3 = float2(-0.9, -0.3);

                float2 waveDir0 = normalize(lerp(baseDir0, inwardDir, bendAmount));
                float2 waveDir1 = normalize(lerp(baseDir1, inwardDir, bendAmount));
                float2 waveDir2 = normalize(lerp(baseDir2, inwardDir, bendAmount));
                float2 waveDir3 = normalize(lerp(baseDir3, inwardDir, bendAmount));

                gerstner += GerstnerWave(waveDir0, 0.35, 18.0, _WaveSpeed,       1.3, samplePos, dxT, dzT);
                totalDx += dxT; totalDz += dzT;
                gerstner += GerstnerWave(waveDir1, 0.25, 12.0, _WaveSpeed * 0.8, 2.7, samplePos, dxT, dzT);
                totalDx += dxT; totalDz += dzT;
                gerstner += GerstnerWave(waveDir2, 0.20,  8.0, _WaveSpeed * 1.2, 4.1, samplePos, dxT, dzT);
                totalDx += dxT; totalDz += dzT;
                gerstner += GerstnerWave(waveDir3, 0.15,  5.0, _WaveSpeed * 1.4, 5.9, samplePos, dxT, dzT);
                totalDx += dxT; totalDz += dzT;

                gerstner *= _WaveHeight * gerstnerAtten;
                totalDx  *= _WaveHeight * gerstnerAtten;
                totalDz  *= _WaveHeight * gerstnerAtten;

                // ---- INWARD XZ PULL (vertices pulled toward center) ----
                float pullAmount = (1.0 - normDist) * _VortexPullStrength;
                float2 pullOffset = -dir * pullAmount;

                // ---- COMBINE ALL DISPLACEMENT ----
                float totalY = depression + spiralDisp + turbDisp + gerstner;

                float4 displacedOS = IN.positionOS;
                // Convert world-space XZ pull back to object space offset
                float3 worldPull = float3(pullOffset.x, 0, pullOffset.y);
                // Approximate: apply in world then convert back
                float3 displacedWS = worldPos + float3(pullOffset.x, totalY, pullOffset.y);

                // Normal: combine funnel slope with wave normal
                // Funnel contributes an inward-tilting normal
                float funnelNormalStrength = funnel * _VortexDepth * 0.05;
                float3 funnelNormal = normalize(float3(dir.x * funnelNormalStrength, 1.0, dir.y * funnelNormalStrength));
                float3 waveNormal = normalize(float3(-totalDx, 1.0, -totalDz));
                float3 combinedNormal = normalize(lerp(waveNormal, funnelNormal, funnel * 0.6));

                // Foam: on spiral crests and turbulent zones
                float foam = 0.0;
                foam += saturate((spiral * spiralStrength - _FoamThreshold) * 3.0);
                foam += saturate(abs(turb) - 0.3) * normDist;
                foam *= _FoamIntensity;
                foam *= gerstnerAtten; // no foam in the deep core

                OUT.positionWS   = displacedWS;
                OUT.normalWS     = combinedNormal;
                OUT.positionCS   = TransformWorldToHClip(displacedWS);
                OUT.fogFactor    = ComputeFogFactor(OUT.positionCS.z);
                OUT.vortexBlend  = 1.0 - normDist;
                OUT.foamMask     = saturate(foam);
                OUT.gerstnerHeight = gerstner;
                OUT.screenPos    = ComputeScreenPos(OUT.positionCS);

                // Vortex distortion vector: inward pull + spiral tangent
                // This tells the fragment shader how the vortex warps space at this point
                float spiralTangentX = -sin(angle + spiralPhase * 0.3);
                float spiralTangentZ =  cos(angle + spiralPhase * 0.3);
                float distortStrength = (1.0 - normDist); // stronger toward center
                float2 distortDir = (-dir * pullAmount + float2(spiralTangentX, spiralTangentZ) * spiralStrength * 0.5) * distortStrength;
                OUT.vortexDistort = distortDir;

                return OUT;
            }

            // =============================================
            // LIGHTING
            // =============================================
            float3 CalcLight(float3 normalWS, float3 viewDir, Light light)
            {
                float NdotL = saturate(dot(normalWS, normalize(light.direction)));
                float3 atten = light.color * light.distanceAttenuation * light.shadowAttenuation;
                float3 diffuse = atten * NdotL;

                float3 halfDir = normalize(normalize(light.direction) + viewDir);
                float spec = pow(saturate(dot(normalWS, halfDir)), 128.0);
                float3 specular = atten * spec * 0.5;

                return diffuse + specular;
            }

            // =============================================
            // CELESTIAL REFLECTION
            // =============================================
            float3 CalcCelestialReflection(float3 normalWS, float3 viewDir, float3 celestialDir,
                                            float intensity, float tightness, float3 reflColor,
                                            float distortion)
            {
                if (celestialDir.y < -0.05) return 0;

                float3 distortedNormal = normalize(lerp(float3(0, 1, 0), normalWS, distortion));
                float3 reflDir = reflect(-viewDir, distortedNormal);

                float reflDot = saturate(dot(reflDir, celestialDir));
                float specular = pow(reflDot, tightness);

                float3 flatReflDir = normalize(float3(reflDir.x, reflDir.y * 0.3, reflDir.z));
                float pathDot = saturate(dot(flatReflDir, normalize(float3(celestialDir.x, celestialDir.y * 0.3, celestialDir.z))));
                float path = pow(pathDot, tightness * 0.15) * 0.2;

                float heightFade = smoothstep(-0.05, 0.3, celestialDir.y);
                return reflColor * (specular + path) * intensity * heightFade;
            }

            // =============================================
            // FRAGMENT
            // =============================================
            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float vortexBlend = IN.vortexBlend;

                // ---- COLOR: same as ocean base (heightFactor) ----
                // This ensures the outer edges look identical to the normal ocean
                float heightFactor = saturate(IN.gerstnerHeight * 0.5 + 0.5);
                float3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, heightFactor);

                // Vortex core: only in the deep center, blend to core color
                float coreMask = smoothstep(0.6, 0.95, vortexBlend);
                waterColor = lerp(waterColor, _VortexCoreColor.rgb, coreMask);

                // Also push toward deep color as we go inward (gradual)
                waterColor = lerp(waterColor, _DeepColor.rgb, vortexBlend * 0.5);

                // Foam
                float foam = IN.foamMask;
                waterColor = lerp(waterColor, _FoamColor.rgb, foam);

                // ---- LIGHTING ----
                float3 viewDir = normalize(GetWorldSpaceNormalizeViewDir(IN.positionWS));

                // ---- DEPTH (for underwater light occlusion) ----
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float sceneDepthRaw = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
                float sceneDepthEye = LinearEyeDepth(sceneDepthRaw, _ZBufferParams);
                float waterDepthEye = LinearEyeDepth(IN.positionCS.z, _ZBufferParams);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDir;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                float3 lighting = 0;

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                lighting += CalcLight(normalWS, viewDir, mainLight);

                #if defined(_ADDITIONAL_LIGHTS)
                #if USE_CLUSTER_LIGHT_LOOP
                UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    lighting += CalcLight(normalWS, viewDir, additionalLight);
                }
                #endif
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    lighting += CalcLight(normalWS, viewDir, additionalLight);
                LIGHT_LOOP_END
                #endif

                float3 ambient = SampleSH(normalWS);
                lighting += ambient;

                // Core gets progressively less light (deeper = darker)
                float coreDarken = lerp(1.0, 0.3, coreMask);
                lighting *= coreDarken;

                float3 finalColor = waterColor * lighting;

                // ---- UNDERWATER LIGHTS (bioluminescence + occlusion + vortex distortion) ----
                float3 underwaterGlow = ComputeUnderwaterLights(
                    IN.positionWS, viewDir,
                    sceneDepthEye, waterDepthEye,
                    IN.vortexDistort, vortexBlend
                );
                // Additive glow, foam partially blocks it
                finalColor += underwaterGlow * (1.0 - foam * 0.7);

                // ---- CELESTIAL REFLECTIONS ----
                // Reflections fade out toward the vortex core
                float reflFade = 1.0 - smoothstep(0.3, 0.8, vortexBlend);

                float3 sunDir = normalize(_CelestialSunDir.xyz);
                float3 moonDir = normalize(_CelestialMoonDir.xyz);
                float dayFactor = _CelestialDayFactor;

                float3 sunRefl = CalcCelestialReflection(
                    normalWS, viewDir, sunDir,
                    _SunReflectionIntensity, _SunReflectionSize, _SunReflectionColor.rgb,
                    _ReflectionDistortion
                ) * dayFactor * reflFade;

                float3 moonRefl = CalcCelestialReflection(
                    normalWS, viewDir, moonDir,
                    _MoonReflectionIntensity, _MoonReflectionSize, _MoonReflectionColor.rgb,
                    _ReflectionDistortion
                ) * (1.0 - dayFactor) * reflFade;

                finalColor += (sunRefl + moonRefl) * (1.0 - foam * 0.8);

                // Fog
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // =============================================
        // SHADOW CASTER
        // =============================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FoamColor;
                float4 _VortexCoreColor;
                float  _WaveHeight;
                float  _WaveSpeed;
                float  _FoamThreshold;
                float  _FoamIntensity;
                float  _VortexRadius;
                float  _VortexDepth;
                float  _VortexInnerRadius;
                float  _VortexFunnelPower;
                float  _VortexSpinSpeed;
                float  _VortexSpiralTightness;
                float  _VortexSpiralHeight;
                float  _VortexSpiralCount;
                float  _VortexTurbulence;
                float  _VortexTurbulenceScale;
                float  _VortexPullStrength;
                float  _SunReflectionIntensity;
                float  _SunReflectionSize;
                float4 _SunReflectionColor;
                float  _MoonReflectionIntensity;
                float  _MoonReflectionSize;
                float4 _MoonReflectionColor;
                float  _ReflectionDistortion;
                float  _SubsurfaceIntensity;
                float  _SubsurfaceAbsorption;
                float4 _SubsurfaceTint;
                float  _SubsurfaceCausticScale;
                float  _SubsurfaceCausticSpeed;
            CBUFFER_END

            float3 _LightDirection;

            Varyings vertShadow(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 vortexCenter = TransformObjectToWorld(float3(0, 0, 0));

                float2 delta = worldPos.xz - vortexCenter.xz;
                float dist = length(delta);
                float2 dir = dist > 0.001 ? delta / dist : float2(0, 0);
                float normDist = saturate(dist / max(_VortexRadius, 0.01));

                float funnelT = 1.0 - saturate((normDist - _VortexInnerRadius) / (1.0 - _VortexInnerRadius));
                float funnel = pow(funnelT, _VortexFunnelPower);
                float depression = -funnel * _VortexDepth;

                float pullAmount = (1.0 - normDist) * _VortexPullStrength;
                float2 pullOffset = -dir * pullAmount;

                float3 displacedWS = worldPos + float3(pullOffset.x, depression, pullOffset.y);

                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(displacedWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = posCS;
                return OUT;
            }

            half4 fragShadow(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // =============================================
        // DEPTH ONLY
        // =============================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FoamColor;
                float4 _VortexCoreColor;
                float  _WaveHeight;
                float  _WaveSpeed;
                float  _FoamThreshold;
                float  _FoamIntensity;
                float  _VortexRadius;
                float  _VortexDepth;
                float  _VortexInnerRadius;
                float  _VortexFunnelPower;
                float  _VortexSpinSpeed;
                float  _VortexSpiralTightness;
                float  _VortexSpiralHeight;
                float  _VortexSpiralCount;
                float  _VortexTurbulence;
                float  _VortexTurbulenceScale;
                float  _VortexPullStrength;
                float  _SunReflectionIntensity;
                float  _SunReflectionSize;
                float4 _SunReflectionColor;
                float  _MoonReflectionIntensity;
                float  _MoonReflectionSize;
                float4 _MoonReflectionColor;
                float  _ReflectionDistortion;
                float  _SubsurfaceIntensity;
                float  _SubsurfaceAbsorption;
                float4 _SubsurfaceTint;
                float  _SubsurfaceCausticScale;
                float  _SubsurfaceCausticSpeed;
            CBUFFER_END

            Varyings vertDepth(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 vortexCenter = TransformObjectToWorld(float3(0, 0, 0));

                float2 delta = worldPos.xz - vortexCenter.xz;
                float dist = length(delta);
                float2 dir = dist > 0.001 ? delta / dist : float2(0, 0);
                float normDist = saturate(dist / max(_VortexRadius, 0.01));

                float funnelT = 1.0 - saturate((normDist - _VortexInnerRadius) / (1.0 - _VortexInnerRadius));
                float funnel = pow(funnelT, _VortexFunnelPower);
                float depression = -funnel * _VortexDepth;

                float pullAmount = (1.0 - normDist) * _VortexPullStrength;
                float2 pullOffset = -dir * pullAmount;

                float3 displacedWS = worldPos + float3(pullOffset.x, depression, pullOffset.y);

                OUT.positionCS = TransformWorldToHClip(displacedWS);
                return OUT;
            }

            half4 fragDepth(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
