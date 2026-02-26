Shader "Custom/OceanLitComplete"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0,0.2,0.4,1)
        _ShallowColor ("Shallow Color", Color) = (0,0.5,0.7,1)
        _FoamColor ("Foam Color", Color) = (1,1,1,1)
        _RippleColor ("Ripple Color", Color) = (0.4,0.7,0.9,1)
        _RippleColorIntensity ("Ripple Color Intensity", Range(0,2)) = 1.0
        _WaveHeight ("Wave Height", Float) = 1
        _WaveSpeed ("Wave Speed", Float) = 1
        _WaveScale ("Wave Scale", Float) = 1
        _FoamThreshold ("Foam Threshold", Float) = 0.6
        _FoamIntensity ("Foam Intensity", Float) = 1

        [Header(Depth Fade)]
        _DepthFadeDistance ("Depth Fade Distance", Range(0.1, 50)) = 5.0
        _DepthShallowColor ("Depth Shallow Tint", Color) = (0.2, 0.6, 0.7, 1)
        _DepthMinAlpha ("Min Alpha (shallow edge)", Range(0, 1)) = 0.15

        [Header(Celestial Reflection)]
        _SunReflectionIntensity ("Sun Reflection Intensity", Range(0, 5)) = 2.0
        _SunReflectionSize ("Sun Reflection Size (tightness)", Range(1, 2048)) = 512
        _SunReflectionColor ("Sun Reflection Color", Color) = (1, 0.9, 0.7, 1)
        _MoonReflectionIntensity ("Moon Reflection Intensity", Range(0, 3)) = 0.8
        _MoonReflectionSize ("Moon Reflection Size (tightness)", Range(1, 2048)) = 256
        _MoonReflectionColor ("Moon Reflection Color", Color) = (0.6, 0.7, 1.0, 1)
        _ReflectionDistortion ("Reflection Wave Distortion", Range(0, 1)) = 0.3

        [Header(Creature Shadows)]
        _CreatureShadowColor ("Shadow Color", Color) = (0.0, 0.02, 0.05, 1)
        _CreatureShadowIntensity ("Shadow Intensity", Range(0, 1)) = 0.8
        _CreatureShadowSoftness ("Shadow Edge Softness", Range(0.1, 20)) = 3.0

        [Header(Underwater Lights)]
        _SubsurfaceIntensity ("Subsurface Glow Intensity", Range(0, 5)) = 1.5
        _SubsurfaceAbsorption ("Water Absorption (depth fade)", Range(0.01, 2.0)) = 0.3
        _SubsurfaceTint ("Water Absorption Tint", Color) = (0.1, 0.5, 0.4, 1)
        _SubsurfaceCausticScale ("Caustic Pattern Scale", Range(0.1, 10.0)) = 2.0
        _SubsurfaceCausticSpeed ("Caustic Anim Speed", Range(0, 3)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

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

            // Depth texture for depth fade
            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float3 positionWS    : TEXCOORD0;
                float  gerstnerHeight: TEXCOORD1;
                float3 normalWS      : TEXCOORD2;
                float  fogFactor     : TEXCOORD3;
                float  rippleAmount  : TEXCOORD4;
                float4 screenPos     : TEXCOORD5;
                float2 worldSampleXZ : TEXCOORD6; // world-anchored XZ for color noise
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float4 _FoamColor;
                float4 _RippleColor;
                float  _RippleColorIntensity;
                float  _WaveHeight;
                float  _WaveSpeed;
                float  _WaveScale;
                float  _FoamThreshold;
                float  _FoamIntensity;

                float  _DepthFadeDistance;
                float4 _DepthShallowColor;
                float  _DepthMinAlpha;

                float  _SunReflectionIntensity;
                float  _SunReflectionSize;
                float4 _SunReflectionColor;
                float  _MoonReflectionIntensity;
                float  _MoonReflectionSize;
                float4 _MoonReflectionColor;
                float  _ReflectionDistortion;

                float4 _CreatureShadowColor;
                float  _CreatureShadowIntensity;
                float  _CreatureShadowSoftness;

                float  _SubsurfaceIntensity;
                float  _SubsurfaceAbsorption;
                float4 _SubsurfaceTint;
                float  _SubsurfaceCausticScale;
                float  _SubsurfaceCausticSpeed;
            CBUFFER_END

            // Globals from OceanManager
            float _RippleStrength;
            float _RippleFrequency;
            float _RippleSpeed;
            float _RippleFalloff;
            float _RippleCount;
            float4 _RipplePoints[8];

            // Globals from DayNightCycle
            float4 _CelestialSunDir;
            float4 _CelestialMoonDir;
            float  _CelestialDayFactor;

            // Globals from OceanCreatureManager
            float  _CreatureCount;
            float4 _CreaturePositions[8];
            float4 _CreatureParams[8];

            // Globals from UnderwaterLightManager
            float  _UnderwaterLightCount;
            float4 _UnderwaterLightPositions[8]; // xyz = world pos, w = outerRadius
            float4 _UnderwaterLightColors[8];    // rgb = color, a = intensity
            float4 _UnderwaterLightParams[8];    // x = innerRadius, y = concentration

            // Globals from WakeTrailManager
            float  _WakeTrailTotalPoints;
            float4 _WakeTrailPoints[128]; // xyz = world pos, w = startTime
            float4 _WakeTrailParams[128]; // x = width, y = foamIntensity, z = velocity, w = unused
            float  _WakeTrailFadeTime;
            float  _WakeTrailRangeCount;  // number of separate trails
            float4 _WakeTrailRanges[16];  // x = startIndex, y = pointCount, z/w = unused

            // =============================================
            // GERSTNER
            // =============================================

            float GerstnerWave(float2 dir, float steepness, float wavelength, float speed, float phase,
                               float3 samplePos, out float dX, out float dZ)
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
            // RIPPLE
            // =============================================

            float ComputeRipple(float3 worldPos, out float rdX, out float rdZ)
            {
                float rippleHeight = 0.0;
                rdX = 0.0;
                rdZ = 0.0;

                int count = (int)_RippleCount;
                for (int i = 0; i < count; i++)
                {
                    float3 ripplePos = _RipplePoints[i].xyz;
                    float startTime = _RipplePoints[i].w;

                    float t = _Time.y - startTime;
                    if (t < 0.0) continue;

                    float2 delta = worldPos.xz - ripplePos.xz;
                    float dist = length(delta);
                    float2 dir = dist > 0.001 ? delta / dist : float2(0, 0);

                    float wave = sin(dist * _RippleFrequency - t * _RippleSpeed);
                    float falloff = exp(-dist * _RippleFalloff) * exp(-t);

                    rippleHeight += wave * falloff;

                    float dWave = cos(dist * _RippleFrequency - t * _RippleSpeed) * _RippleFrequency * falloff
                                - wave * _RippleFalloff * falloff;
                    rdX += dWave * dir.x;
                    rdZ += dWave * dir.y;
                }

                rdX *= _RippleStrength;
                rdZ *= _RippleStrength;
                return rippleHeight * _RippleStrength;
            }

            // =============================================
            // CREATURE SHADOW
            // =============================================

            float ComputeCreatureShadow(float3 worldPos)
            {
                float shadow = 0.0;
                int count = (int)_CreatureCount;

                for (int i = 0; i < count; i++)
                {
                    float3 creaturePos = _CreaturePositions[i].xyz;
                    float  radius      = _CreaturePositions[i].w;
                    float  depth       = _CreatureParams[i].x;    // how deep below surface
                    float  elongation  = _CreatureParams[i].y;    // XZ stretch (1 = circle, >1 = elongated)
                    float  opacity     = _CreatureParams[i].z;    // per-creature opacity

                    // Horizontal distance from water pixel to creature XZ
                    float2 delta = worldPos.xz - creaturePos.xz;
                    float dist = length(delta);

                    // Shadow gets softer and larger the deeper the creature is
                    float depthSpread = 1.0 + depth * 0.3;
                    float effectiveRadius = radius * depthSpread * elongation;

                    // Soft falloff
                    float falloff = 1.0 - saturate(dist / max(effectiveRadius, 0.01));
                    falloff = pow(falloff, _CreatureShadowSoftness);

                    // Deeper = fainter shadow
                    float depthFade = exp(-depth * 0.15);

                    shadow += falloff * depthFade * opacity;
                }

                return saturate(shadow);
            }

            // =============================================
            // UNDERWATER LIGHTS
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

            float3 ComputeUnderwaterLights(float3 surfacePos, float3 viewDir, float sceneDepthEye, float waterDepthEye)
            {
                float3 totalGlow = 0;
                int count = (int)_UnderwaterLightCount;

                // Camera position (needed for distance to light)
                float3 camPos = _WorldSpaceCameraPos;

                for (int i = 0; i < count; i++)
                {
                    float3 lightPos      = _UnderwaterLightPositions[i].xyz;
                    float  outerRadius   = _UnderwaterLightPositions[i].w;
                    float3 color         = _UnderwaterLightColors[i].rgb;
                    float  intensity     = _UnderwaterLightColors[i].a;
                    float  innerRadius   = _UnderwaterLightParams[i].x;
                    float  concentration = _UnderwaterLightParams[i].y;

                    // ---- RAY THROUGH WATER ----
                    float3 rayDir = -viewDir;
                    float3 toLight = lightPos - surfacePos;
                    float t = max(dot(toLight, rayDir), 0.0);
                    float3 closestPoint = surfacePos + rayDir * t;
                    float closestDist = length(lightPos - closestPoint);

                    // ---- DUAL RADIUS GLOW ----
                    float innerGlow = 1.0 - saturate(closestDist / max(innerRadius, 0.001));
                    innerGlow = innerGlow * innerGlow * innerGlow;

                    float outerGlow = 1.0 - saturate(closestDist / max(outerRadius, 0.01));
                    outerGlow = outerGlow * outerGlow * (3.0 - 2.0 * outerGlow);

                    float glow = lerp(outerGlow, innerGlow * 3.0, concentration);

                    // ---- OCCLUSION FROM SCENE GEOMETRY ----
                    // Check if opaque objects in the scene are between the
                    // camera and the light. If sceneDepthEye is less than
                    // the distance from camera to the light, something is blocking.
                    float camToLightDist = length(lightPos - camPos);
                    // sceneDepthEye = depth of the nearest opaque object behind the water
                    // If that object is closer than the light, it occludes
                    // Soft edge transition over 2 units to avoid hard pop
                    float occlusionFade = saturate((sceneDepthEye - camToLightDist) * 0.5 + 1.0);
                    // Also: if the scene depth is very close to the water depth,
                    // there's a solid floor — light coming from below is blocked
                    float floorBlock = saturate((sceneDepthEye - waterDepthEye) / max(outerRadius * 0.5, 1.0));
                    float occlusion = min(occlusionFade, floorBlock);
                    // Only apply occlusion where there's actual scene geometry
                    // (sceneDepthEye near far plane means no occluder)
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

                    // ---- CAUSTICS ----
                    float2 causticUV = surfacePos.xz * _SubsurfaceCausticScale / max(outerRadius, 1.0);
                    float caustic = CausticPattern(causticUV, _Time.y * _SubsurfaceCausticSpeed);
                    float causticContrib = lerp(0.7, 1.0, caustic * outerGlow);

                    totalGlow += tintedColor * intensity * glow * absorption * causticContrib * occlusion;
                }

                return totalGlow * _SubsurfaceIntensity;
            }

            // =============================================
            // WAKE TRAIL (multi-emitter foam trails)
            // =============================================

            float WakeSegment(float3 worldPos, int idxA, int idxB)
            {
                float3 pA = _WakeTrailPoints[idxA].xyz;
                float  tA = _WakeTrailPoints[idxA].w;
                float3 pB = _WakeTrailPoints[idxB].xyz;
                float  tB = _WakeTrailPoints[idxB].w;

                float widthA = _WakeTrailParams[idxA].x;
                float widthB = _WakeTrailParams[idxB].x;
                float foamA  = _WakeTrailParams[idxA].y;
                float foamB  = _WakeTrailParams[idxB].y;
                float velA   = _WakeTrailParams[idxA].z;
                float velB   = _WakeTrailParams[idxB].z;

                if (widthA < 0.001 && widthB < 0.001) return 0.0;

                float2 ab = pB.xz - pA.xz;
                float segLenSq = dot(ab, ab);
                if (segLenSq < 0.0001) return 0.0;

                float2 ap = worldPos.xz - pA.xz;
                float tProj = saturate(dot(ap, ab) / segLenSq);

                float2 closest = pA.xz + ab * tProj;
                float dist = length(worldPos.xz - closest);

                float width = lerp(widthA, widthB, tProj);
                float foamStr = lerp(foamA, foamB, tProj);
                float vel = lerp(velA, velB, tProj);
                float pointTime = lerp(tA, tB, tProj);

                float age = _Time.y - pointTime;
                if (age > _WakeTrailFadeTime) return 0.0;
                float lifeFraction = age / max(_WakeTrailFadeTime, 0.01);
                float timeFade = 1.0 - lifeFraction;
                timeFade = timeFade * timeFade * (3.0 - 2.0 * timeFade);

                float ageWidth = width * (1.0 + lifeFraction * 0.8);

                float centerFoam = 1.0 - saturate(dist / max(ageWidth * 0.35, 0.01));
                centerFoam = centerFoam * centerFoam;

                float spreadNorm = dist / max(ageWidth, 0.01);
                float vEdge = abs(spreadNorm - 0.65);
                float vFoam = 1.0 - saturate(vEdge / 0.2);
                vFoam *= saturate(1.0 - spreadNorm);

                float2 noiseUV = worldPos.xz * 0.5 + float2(_Time.y * 0.3, 0);
                float noise = sin(noiseUV.x * 3.7 + sin(noiseUV.y * 2.3)) * 0.5 + 0.5;
                float turbulence = lerp(0.7, 1.0, noise);

                float velFactor = saturate(vel * 0.5);

                return (centerFoam + vFoam * 0.5) * foamStr * timeFade * turbulence * velFactor;
            }

            float ComputeWakeTrail(float3 worldPos)
            {
                float foam = 0.0;
                int rangeCount = (int)_WakeTrailRangeCount;

                // Each range is a separate trail (different emitter)
                for (int r = 0; r < rangeCount; r++)
                {
                    int start = (int)_WakeTrailRanges[r].x;
                    int count = (int)_WakeTrailRanges[r].y;

                    // Iterate segments within this trail only
                    for (int i = 0; i < count - 1; i++)
                        foam = max(foam, WakeSegment(worldPos, start + i, start + i + 1));
                }

                return saturate(foam);
            }

            // =============================================
            // VERTEX
            // =============================================

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                // Sample waves using world-space XZ so the wave pattern is
                // anchored to the world, not to the object. Moving the object
                // means each vertex samples a different point on the infinite
                // wave field — the waves appear stationary.
                float3 samplePos = worldPos;

                float2 drift = float2(
                    sin(_Time.y * 0.05),
                    cos(_Time.y * 0.037)
                ) * 5.0;

                samplePos.x += drift.x;
                samplePos.z += drift.y;

                float totalDx = 0.0;
                float totalDz = 0.0;
                float dxTemp, dzTemp;
                float gerstner = 0.0;

                gerstner += GerstnerWave(float2(0.8, 0.2),  0.35, 18.0, _WaveSpeed,       1.3, samplePos, dxTemp, dzTemp);
                totalDx += dxTemp; totalDz += dzTemp;

                gerstner += GerstnerWave(float2(-0.4, 0.9), 0.25, 12.0, _WaveSpeed * 0.8, 2.7, samplePos, dxTemp, dzTemp);
                totalDx += dxTemp; totalDz += dzTemp;

                gerstner += GerstnerWave(float2(0.6, -0.7), 0.20, 8.0,  _WaveSpeed * 1.2, 4.1, samplePos, dxTemp, dzTemp);
                totalDx += dxTemp; totalDz += dzTemp;

                gerstner += GerstnerWave(float2(-0.9,-0.3), 0.15, 5.0,  _WaveSpeed * 1.4, 5.9, samplePos, dxTemp, dzTemp);
                totalDx += dxTemp; totalDz += dzTemp;

                gerstner *= _WaveHeight;
                totalDx *= _WaveHeight;
                totalDz *= _WaveHeight;

                float rdX, rdZ;
                float ripple = ComputeRipple(worldPos, rdX, rdZ);

                totalDx += rdX;
                totalDz += rdZ;

                float totalWave = gerstner + ripple;

                // Apply displacement in OBJECT SPACE so the wave offset is
                // relative to the mesh, not baked into world position.
                // This means moving the object in the editor moves the mesh
                // but the wave heights stay world-anchored.
                float4 displacedOS = IN.positionOS;
                displacedOS.y += totalWave;

                float3 displacedWS = TransformObjectToWorld(displacedOS.xyz);

                float3 normal = normalize(float3(-totalDx, 1.0, -totalDz));

                OUT.gerstnerHeight = gerstner;
                OUT.rippleAmount   = saturate(abs(ripple) * 4.0);

                OUT.positionWS     = displacedWS;
                OUT.normalWS       = normal;
                OUT.positionCS     = TransformObjectToHClip(displacedOS.xyz);
                OUT.fogFactor      = ComputeFogFactor(OUT.positionCS.z);
                OUT.screenPos      = ComputeScreenPos(OUT.positionCS);
                OUT.worldSampleXZ  = worldPos.xz; // pre-displacement world XZ

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
                float path = pow(pathDot, tightness * 0.15) * 0.25;

                float heightFade = smoothstep(-0.05, 0.3, celestialDir.y);

                return reflColor * (specular + path) * intensity * heightFade;
            }

            // =============================================
            // FRAGMENT
            // =============================================

            half4 frag (Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);

                // ---- DEPTH FADE ----
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float sceneDepthRaw = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
                float sceneDepthEye = LinearEyeDepth(sceneDepthRaw, _ZBufferParams);
                float waterDepthEye = LinearEyeDepth(IN.positionCS.z, _ZBufferParams);
                float depthDiff = sceneDepthEye - waterDepthEye;
                float depthFactor = saturate(depthDiff / _DepthFadeDistance);

                // ---- COLOR: based on GERSTNER + world noise + depth ----
                float heightFactor = saturate(IN.gerstnerHeight * 0.5 + 0.5);

                // World-space color noise: uses world-anchored XZ coordinates
                // so the color pattern stays fixed in the world even when
                // the ocean mesh moves with the player/boat.
                float2 anchoredXZ = IN.worldSampleXZ;
                float2 colorNoiseUV = anchoredXZ * 0.03 + float2(_Time.y * 0.01, _Time.y * 0.007);
                float colorNoise = sin(colorNoiseUV.x * 2.7 + sin(colorNoiseUV.y * 1.9)) * 0.5 + 0.5;
                float2 colorNoiseUV2 = anchoredXZ * 0.07 + float2(-_Time.y * 0.015, _Time.y * 0.012);
                float colorNoise2 = sin(colorNoiseUV2.x * 3.3 + cos(colorNoiseUV2.y * 2.1)) * 0.5 + 0.5;
                float worldColorVar = colorNoise * 0.6 + colorNoise2 * 0.4;

                // Combine: wave height is primary driver, world noise adds variation
                float combinedFactor = saturate(heightFactor * 0.6 + worldColorVar * 0.4);

                float3 baseColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, combinedFactor);

                // Blend toward shallow tint in shallow areas
                baseColor = lerp(_DepthShallowColor.rgb, baseColor, depthFactor);

                // Foam on gerstner crests only
                float foamMask = saturate((IN.gerstnerHeight - _FoamThreshold) * 4.0) * _FoamIntensity;
                baseColor = lerp(baseColor, _FoamColor.rgb, foamMask);

                // ---- WAKE TRAIL FOAM (boat trail) ----
                float wakeFoam = ComputeWakeTrail(IN.positionWS);
                baseColor = lerp(baseColor, _FoamColor.rgb, wakeFoam);
                foamMask = max(foamMask, wakeFoam);

                // ---- RIPPLE COLOR ----
                float rippleMask = IN.rippleAmount * _RippleColorIntensity;
                baseColor = lerp(baseColor, max(baseColor, _RippleColor.rgb), saturate(rippleMask));

                // ---- CREATURE SHADOWS ----
                float creatureShadow = ComputeCreatureShadow(IN.positionWS);
                if (creatureShadow > 0.001)
                {
                    float shadowBlend = creatureShadow * _CreatureShadowIntensity;
                    baseColor = lerp(baseColor, _CreatureShadowColor.rgb, shadowBlend);
                }

                // View direction
                float3 viewDir = normalize(GetWorldSpaceNormalizeViewDir(IN.positionWS));

                // ---- UNDERWATER LIGHTS (subsurface glow) ----
                // Pass scene depth so lights are occluded by geometry between camera and light
                float3 underwaterGlow = ComputeUnderwaterLights(IN.positionWS, viewDir, sceneDepthEye, waterDepthEye);

                // InputData for Forward+
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDir;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                float3 lighting = 0;

                // Main light
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                lighting += CalcLight(normalWS, viewDir, mainLight);

                // Additional lights
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

                // Ambient
                float3 ambient = SampleSH(normalWS);
                lighting += ambient;

                float3 finalColor = baseColor * lighting;

                // Apply underwater glow (additive, light coming from below)
                // Foam blocks subsurface light (opaque surface)
                finalColor += underwaterGlow * (1.0 - foamMask * 0.7);

                // ---- CELESTIAL REFLECTIONS ----
                float3 sunDir = normalize(_CelestialSunDir.xyz);
                float3 moonDir = normalize(_CelestialMoonDir.xyz);
                float dayFactor = _CelestialDayFactor;

                float3 sunRefl = CalcCelestialReflection(
                    normalWS, viewDir, sunDir,
                    _SunReflectionIntensity, _SunReflectionSize, _SunReflectionColor.rgb,
                    _ReflectionDistortion
                ) * dayFactor;

                float3 moonRefl = CalcCelestialReflection(
                    normalWS, viewDir, moonDir,
                    _MoonReflectionIntensity, _MoonReflectionSize, _MoonReflectionColor.rgb,
                    _ReflectionDistortion
                ) * (1.0 - dayFactor);

                float reflMask = 1.0 - foamMask * 0.8;
                finalColor += (sunRefl + moonRefl) * reflMask;

                // Fog
                finalColor = MixFog(finalColor, IN.fogFactor);

                // ---- ALPHA: depth-based fade ----
                float baseAlpha = lerp(_DepthMinAlpha, _DeepColor.a, depthFactor);
                float alpha = lerp(baseAlpha, 1.0, max(foamMask, rippleMask * 0.5));

                return half4(finalColor, alpha);
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
                float4 _RippleColor;
                float  _RippleColorIntensity;
                float  _WaveHeight;
                float  _WaveSpeed;
                float  _WaveScale;
                float  _FoamThreshold;
                float  _FoamIntensity;

                float  _DepthFadeDistance;
                float4 _DepthShallowColor;
                float  _DepthMinAlpha;

                float  _SunReflectionIntensity;
                float  _SunReflectionSize;
                float4 _SunReflectionColor;
                float  _MoonReflectionIntensity;
                float  _MoonReflectionSize;
                float4 _MoonReflectionColor;
                float  _ReflectionDistortion;

                float4 _CreatureShadowColor;
                float  _CreatureShadowIntensity;
                float  _CreatureShadowSoftness;

                float  _SubsurfaceIntensity;
                float  _SubsurfaceAbsorption;
                float4 _SubsurfaceTint;
                float  _SubsurfaceCausticScale;
                float  _SubsurfaceCausticSpeed;
            CBUFFER_END

            float _RippleStrength;
            float _RippleFrequency;
            float _RippleSpeed;
            float _RippleFalloff;
            float _RippleCount;
            float4 _RipplePoints[8];

            float3 _LightDirection;

            float GerstnerSimple(float2 dir, float steepness, float wavelength, float speed, float phase, float3 samplePos)
            {
                float k = 2.0 * PI / wavelength;
                float c = sqrt(9.8 / k) * speed;
                float2 d = normalize(dir);
                float f = k * dot(d, samplePos.xz) - c * _Time.y + phase;
                return sin(f) * steepness;
            }

            float ComputeRippleSimple(float3 worldPos)
            {
                float rippleHeight = 0.0;
                int count = (int)_RippleCount;
                for (int i = 0; i < count; i++)
                {
                    float3 rp = _RipplePoints[i].xyz;
                    float st = _RipplePoints[i].w;
                    float t = _Time.y - st;
                    if (t < 0.0) continue;
                    float dist = length(worldPos.xz - rp.xz);
                    float wave = sin(dist * _RippleFrequency - t * _RippleSpeed);
                    float falloff = exp(-dist * _RippleFalloff) * exp(-t);
                    rippleHeight += wave * falloff;
                }
                return rippleHeight * _RippleStrength;
            }

            Varyings vertShadow (Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                float3 samplePos = worldPos;
                float2 drift = float2(sin(_Time.y * 0.05), cos(_Time.y * 0.037)) * 5.0;
                samplePos.x += drift.x;
                samplePos.z += drift.y;

                float wave = 0.0;
                wave += GerstnerSimple(float2(0.8, 0.2),  0.35, 18.0, _WaveSpeed,       1.3, samplePos);
                wave += GerstnerSimple(float2(-0.4, 0.9), 0.25, 12.0, _WaveSpeed * 0.8, 2.7, samplePos);
                wave += GerstnerSimple(float2(0.6, -0.7), 0.20, 8.0,  _WaveSpeed * 1.2, 4.1, samplePos);
                wave += GerstnerSimple(float2(-0.9,-0.3), 0.15, 5.0,  _WaveSpeed * 1.4, 5.9, samplePos);
                wave *= _WaveHeight;
                wave += ComputeRippleSimple(worldPos);

                float4 displacedOS = IN.positionOS;
                displacedOS.y += wave;
                float3 displacedWS = TransformObjectToWorld(displacedOS.xyz);

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

            half4 fragShadow (Varyings IN) : SV_Target
            {
                return 0;
            }
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
                float4 _RippleColor;
                float  _RippleColorIntensity;
                float  _WaveHeight;
                float  _WaveSpeed;
                float  _WaveScale;
                float  _FoamThreshold;
                float  _FoamIntensity;

                float  _DepthFadeDistance;
                float4 _DepthShallowColor;
                float  _DepthMinAlpha;

                float  _SunReflectionIntensity;
                float  _SunReflectionSize;
                float4 _SunReflectionColor;
                float  _MoonReflectionIntensity;
                float  _MoonReflectionSize;
                float4 _MoonReflectionColor;
                float  _ReflectionDistortion;

                float4 _CreatureShadowColor;
                float  _CreatureShadowIntensity;
                float  _CreatureShadowSoftness;

                float  _SubsurfaceIntensity;
                float  _SubsurfaceAbsorption;
                float4 _SubsurfaceTint;
                float  _SubsurfaceCausticScale;
                float  _SubsurfaceCausticSpeed;
            CBUFFER_END

            float _RippleStrength;
            float _RippleFrequency;
            float _RippleSpeed;
            float _RippleFalloff;
            float _RippleCount;
            float4 _RipplePoints[8];

            float GerstnerSimple(float2 dir, float steepness, float wavelength, float speed, float phase, float3 samplePos)
            {
                float k = 2.0 * PI / wavelength;
                float c = sqrt(9.8 / k) * speed;
                float2 d = normalize(dir);
                float f = k * dot(d, samplePos.xz) - c * _Time.y + phase;
                return sin(f) * steepness;
            }

            float ComputeRippleSimple(float3 worldPos)
            {
                float rippleHeight = 0.0;
                int count = (int)_RippleCount;
                for (int i = 0; i < count; i++)
                {
                    float3 rp = _RipplePoints[i].xyz;
                    float st = _RipplePoints[i].w;
                    float t = _Time.y - st;
                    if (t < 0.0) continue;
                    float dist = length(worldPos.xz - rp.xz);
                    float wave = sin(dist * _RippleFrequency - t * _RippleSpeed);
                    float falloff = exp(-dist * _RippleFalloff) * exp(-t);
                    rippleHeight += wave * falloff;
                }
                return rippleHeight * _RippleStrength;
            }

            Varyings vertDepth (Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                float3 samplePos = worldPos;
                float2 drift = float2(sin(_Time.y * 0.05), cos(_Time.y * 0.037)) * 5.0;
                samplePos.x += drift.x;
                samplePos.z += drift.y;

                float wave = 0.0;
                wave += GerstnerSimple(float2(0.8, 0.2),  0.35, 18.0, _WaveSpeed,       1.3, samplePos);
                wave += GerstnerSimple(float2(-0.4, 0.9), 0.25, 12.0, _WaveSpeed * 0.8, 2.7, samplePos);
                wave += GerstnerSimple(float2(0.6, -0.7), 0.20, 8.0,  _WaveSpeed * 1.2, 4.1, samplePos);
                wave += GerstnerSimple(float2(-0.9,-0.3), 0.15, 5.0,  _WaveSpeed * 1.4, 5.9, samplePos);
                wave *= _WaveHeight;
                wave += ComputeRippleSimple(worldPos);

                float4 displacedOS = IN.positionOS;
                displacedOS.y += wave;

                OUT.positionCS = TransformObjectToHClip(displacedOS.xyz);
                return OUT;
            }

            half4 fragDepth (Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
