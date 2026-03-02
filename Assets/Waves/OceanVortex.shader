Shader "Custom/OceanVortex"
{
    Properties
    {
        // ---- SAME AS OCEAN ----
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

        // ---- VORTEX SPECIFIC ----
        [Header(Vortex Colors)]
        _VortexCoreColor ("Vortex Core Color (abyss)", Color) = (0, 0.02, 0.05, 1)

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

        [Header(Vortex Pull)]
        _VortexPullStrength ("Inward Pull Strength", Range(0.0, 5.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            Cull Back

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
                float  foamMask      : TEXCOORD4;
                float  vortexBlend   : TEXCOORD5;
                float2 vortexDistort : TEXCOORD6;
                float2 worldSampleXZ : TEXCOORD7;
                float4 screenPos     : TEXCOORD8;
            };

            // ---- CBUFFER: EXACT MATCH WITH OCEAN + VORTEX EXTRAS ----
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

                // Vortex-specific
                float4 _VortexCoreColor;
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
            CBUFFER_END

            // ---- GLOBALS: EXACT MATCH WITH OCEAN ----
            float _RippleStrength;
            float _RippleFrequency;
            float _RippleSpeed;
            float _RippleFalloff;
            float _RippleCount;
            float4 _RipplePoints[8];

            float4 _CelestialSunDir;
            float4 _CelestialMoonDir;
            float  _CelestialDayFactor;

            float  _CreatureCount;
            float4 _CreaturePositions[8];
            float4 _CreatureParams[8];

            float  _UnderwaterLightCount;
            float4 _UnderwaterLightPositions[8];
            float4 _UnderwaterLightColors[8];
            float4 _UnderwaterLightParams[8];

            float  _WakeTrailTotalPoints;
            float4 _WakeTrailPoints[128];
            float4 _WakeTrailParams[128];
            float  _WakeTrailFadeTime;
            float  _WakeTrailRangeCount;
            float4 _WakeTrailRanges[16];

            // Globals from UnderwaterDecalManager
            float  _UnderwaterDecalCount;
            float4 _UnderwaterDecalPositions[8];
            float4 _UnderwaterDecalParams[8];
            float4 _UnderwaterDecalColors[8];
            float4 _UnderwaterDecalExtra[8];
            float4 _UnderwaterDecalMatRow0[8];
            float4 _UnderwaterDecalMatRow1[8];
            float4 _UnderwaterDecalMatRow2[8];
            TEXTURE2D_ARRAY(_UnderwaterDecalTextures);
            SAMPLER(sampler_UnderwaterDecalTextures);

            // =============================================
            // NOISE (for vortex turbulence)
            // =============================================
            float hash2D(float2 p) { p = frac(p * float2(443.897, 441.423)); p += dot(p, p + 19.19); return frac(p.x * p.y); }
            float noise2D(float2 p) { float2 i = floor(p); float2 f = frac(p); f = f*f*(3.0-2.0*f); float a=hash2D(i); float b=hash2D(i+float2(1,0)); float c=hash2D(i+float2(0,1)); float d=hash2D(i+float2(1,1)); return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y); }
            float fbm2D(float2 p, int octaves) { float v=0,a=0.5; for(int i=0;i<octaves;i++){v+=noise2D(p)*a;p*=2;a*=0.5;} return v; }

            // =============================================
            // GERSTNER (same as ocean)
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
            // RIPPLE (same as ocean)
            // =============================================
            float ComputeRipple(float3 worldPos, out float rdX, out float rdZ)
            {
                float rippleHeight = 0.0; rdX = 0.0; rdZ = 0.0;
                int count = (int)_RippleCount;
                for (int i = 0; i < count; i++)
                {
                    float3 rp = _RipplePoints[i].xyz; float st = _RipplePoints[i].w;
                    float t = _Time.y - st; if (t < 0.0) continue;
                    float2 delta = worldPos.xz - rp.xz; float dist = length(delta);
                    float2 dir = dist > 0.001 ? delta / dist : float2(0, 0);
                    float wave = sin(dist * _RippleFrequency - t * _RippleSpeed);
                    float falloff = exp(-dist * _RippleFalloff) * exp(-t);
                    rippleHeight += wave * falloff;
                    float dWave = cos(dist * _RippleFrequency - t * _RippleSpeed) * _RippleFrequency * falloff - wave * _RippleFalloff * falloff;
                    rdX += dWave * dir.x; rdZ += dWave * dir.y;
                }
                rdX *= _RippleStrength; rdZ *= _RippleStrength;
                return rippleHeight * _RippleStrength;
            }

            // =============================================
            // CREATURE SHADOW (same as ocean)
            // =============================================
            float ComputeCreatureShadow(float3 worldPos)
            {
                float shadow = 0.0; int count = (int)_CreatureCount;
                for (int i = 0; i < count; i++)
                {
                    float3 cp = _CreaturePositions[i].xyz; float r = _CreaturePositions[i].w;
                    float depth = _CreatureParams[i].x; float elong = _CreatureParams[i].y; float opa = _CreatureParams[i].z;
                    float dist = length(worldPos.xz - cp.xz);
                    float er = r * (1.0 + depth * 0.3) * elong;
                    float fo = 1.0 - saturate(dist / max(er, 0.01));
                    fo = pow(fo, _CreatureShadowSoftness);
                    shadow += fo * exp(-depth * 0.15) * opa;
                }
                return saturate(shadow);
            }

            // =============================================
            // UNDERWATER LIGHTS (with vortex distortion)
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
                                           float2 vortexDistort, float vortexBlend)
            {
                float3 totalGlow = 0;
                int count = (int)_UnderwaterLightCount;
                for (int i = 0; i < count; i++)
                {
                    float3 lp = _UnderwaterLightPositions[i].xyz; float outerR = _UnderwaterLightPositions[i].w;
                    float3 col = _UnderwaterLightColors[i].rgb; float inten = _UnderwaterLightColors[i].a;
                    float innerR = _UnderwaterLightParams[i].x; float conc = _UnderwaterLightParams[i].y;

                    float3 dsp = surfacePos; dsp.xz += vortexDistort * vortexBlend * 3.0;
                    float3 rayDir = -viewDir; float3 toL = lp - dsp;
                    float t = max(dot(toL, rayDir), 0.0);
                    float cd = length(lp - (dsp + rayDir * t));

                    float vStretch = 1.0 + vortexBlend * 1.5;
                    float ig = 1.0 - saturate(cd / max(innerR * vStretch, 0.001)); ig = ig*ig*ig;
                    float og = 1.0 - saturate(cd / max(outerR * (1.0 + vortexBlend * 0.5), 0.01)); og = og*og*(3.0-2.0*og);
                    float glow = lerp(og, ig * 3.0, conc);

                    float wd = length(toL); float absorp = exp(-wd * _SubsurfaceAbsorption * 0.05);
                    float dep = max(surfacePos.y - lp.y, 0.0);
                    float3 wa = float3(exp(-dep*0.06), exp(-dep*0.02), exp(-dep*0.005));
                    float3 tc = col * lerp(wa, _SubsurfaceTint.rgb, 0.2);

                    float2 cuv = (surfacePos.xz + vortexDistort * vortexBlend) * _SubsurfaceCausticScale / max(outerR,1);
                    float caus = CausticPattern(cuv, _Time.y * _SubsurfaceCausticSpeed);
                    float cc = lerp(0.7, 1.0, caus * og);
                    totalGlow += tc * inten * glow * absorp * cc;
                }
                return totalGlow * _SubsurfaceIntensity;
            }

            // =============================================
            // WAKE TRAIL (same as ocean)
            // =============================================
            float WakeSegment(float3 worldPos, int idxA, int idxB)
            {
                float3 pA = _WakeTrailPoints[idxA].xyz; float tA = _WakeTrailPoints[idxA].w;
                float3 pB = _WakeTrailPoints[idxB].xyz; float tB = _WakeTrailPoints[idxB].w;
                float wA = _WakeTrailParams[idxA].x; float wB = _WakeTrailParams[idxB].x;
                float fA = _WakeTrailParams[idxA].y; float fB = _WakeTrailParams[idxB].y;
                float vA = _WakeTrailParams[idxA].z; float vB = _WakeTrailParams[idxB].z;
                if (wA < 0.001 && wB < 0.001) return 0;
                float2 ab = pB.xz - pA.xz; float sl = dot(ab,ab); if (sl < 0.0001) return 0;
                float tp = saturate(dot(worldPos.xz - pA.xz, ab) / sl);
                float dist = length(worldPos.xz - (pA.xz + ab * tp));
                float w = lerp(wA,wB,tp); float fi = lerp(fA,fB,tp); float v = lerp(vA,vB,tp);
                float age = _Time.y - lerp(tA,tB,tp);
                if (age > _WakeTrailFadeTime) return 0;
                float lf = age / max(_WakeTrailFadeTime, 0.01);
                float tf = 1.0 - lf; tf = tf*tf*(3.0-2.0*tf);
                float aw = w * (1.0 + lf * 0.8);
                float cf = 1.0 - saturate(dist / max(aw * 0.35, 0.01)); cf = cf*cf;
                float sn = dist / max(aw, 0.01);
                float vf = 1.0 - saturate(abs(sn - 0.65) / 0.2); vf *= saturate(1.0 - sn);
                float2 nuv = worldPos.xz * 0.5 + float2(_Time.y * 0.3, 0);
                float turb = lerp(0.7, 1.0, sin(nuv.x*3.7+sin(nuv.y*2.3))*0.5+0.5);
                return (cf + vf * 0.5) * fi * tf * turb * saturate(v * 0.5);
            }
            float ComputeWakeTrail(float3 worldPos)
            {
                float foam = 0; int rc = (int)_WakeTrailRangeCount;
                for (int r = 0; r < rc; r++) {
                    int s = (int)_WakeTrailRanges[r].x; int c = (int)_WakeTrailRanges[r].y;
                    for (int i = 0; i < c - 1; i++) foam = max(foam, WakeSegment(worldPos, s+i, s+i+1));
                }
                return saturate(foam);
            }

            // =============================================
            // UNDERWATER DECALS (same as ocean)
            // =============================================
            float4 ComputeUnderwaterDecals(float3 worldPos, float3 viewDir)
            {
                float3 totalColor = 0;
                float totalAlpha = 0;
                int count = (int)_UnderwaterDecalCount;
                float3 camPos = _WorldSpaceCameraPos;

                for (int i = 0; i < count; i++)
                {
                    float opa = _UnderwaterDecalParams[i].x;
                    float ef  = _UnderwaterDecalParams[i].y;
                    float distAmt = _UnderwaterDecalParams[i].z;
                    float distSpd = _UnderwaterDecalParams[i].w;
                    float3 tint = _UnderwaterDecalColors[i].rgb;
                    float wtStr = _UnderwaterDecalColors[i].a;
                    float darken = _UnderwaterDecalExtra[i].x;
                    float desat = _UnderwaterDecalExtra[i].y;

                    if (opa < 0.001) continue;

                    float3 rayDir = normalize(worldPos - camPos);

                    float4 camW = float4(camPos, 1.0);
                    float3 localCam = float3(
                        dot(_UnderwaterDecalMatRow0[i], camW),
                        dot(_UnderwaterDecalMatRow1[i], camW),
                        dot(_UnderwaterDecalMatRow2[i], camW)
                    );
                    float4 dirW = float4(rayDir, 0.0);
                    float3 localDir = float3(
                        dot(_UnderwaterDecalMatRow0[i], dirW),
                        dot(_UnderwaterDecalMatRow1[i], dirW),
                        dot(_UnderwaterDecalMatRow2[i], dirW)
                    );

                    if (abs(localDir.y) < 0.0001) continue;
                    float t = -localCam.y / localDir.y;
                    if (t < 0) continue;

                    float3 localHit = localCam + localDir * t;
                    float2 uv = localHit.xz + 0.5;
                    if (uv.x < -0.05 || uv.x > 1.05 || uv.y < -0.05 || uv.y > 1.05) continue;

                    float tw = _Time.y * distSpd;
                    float2 dUV = worldPos.xz * 2.0;
                    float2 distort = float2(
                        sin(dUV.x * 2.7 + tw * 1.1 + sin(dUV.y * 1.9 + tw * 0.7)),
                        cos(dUV.y * 3.1 + tw * 0.9 + sin(dUV.x * 2.3 + tw * 1.3))
                    ) * distAmt;
                    uv = saturate(uv + distort);

                    float4 texCol = SAMPLE_TEXTURE2D_ARRAY(_UnderwaterDecalTextures,
                                    sampler_UnderwaterDecalTextures, uv, i);

                    float3 col = texCol.rgb * tint;
                    col = lerp(col, col * float3(0.0, 0.4, 0.6), wtStr);
                    col *= 1.0 - darken;
                    float lum = dot(col, float3(0.299, 0.587, 0.114));
                    col = lerp(col, float3(lum, lum, lum), desat);

                    float2 edgeDist = min(uv, 1.0 - uv);
                    float edgeMask = (ef > 0.001) ? saturate(min(edgeDist.x, edgeDist.y) / ef) : 1.0;
                    float alpha = texCol.a * opa * edgeMask;

                    totalColor = lerp(totalColor, col, alpha);
                    totalAlpha = saturate(totalAlpha + alpha * (1.0 - totalAlpha));
                }

                return float4(totalColor, totalAlpha);
            }

            // =============================================
            // VERTEX (vortex-specific displacement)
            // =============================================
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 vortexCenter = TransformObjectToWorld(float3(0, 0, 0));

                float2 delta = worldPos.xz - vortexCenter.xz;
                float dist = length(delta);
                float2 dir = dist > 0.001 ? delta / dist : float2(0, 0);
                float normDist = saturate(dist / max(_VortexRadius, 0.01));

                // Funnel
                float funnelT = 1.0 - saturate((normDist - _VortexInnerRadius) / (1.0 - _VortexInnerRadius));
                float funnel = pow(funnelT, _VortexFunnelPower);
                float depression = -funnel * _VortexDepth;

                // Spiral
                float angle = atan2(delta.y, delta.x);
                float spiralPhase = angle * _VortexSpiralCount + dist * _VortexSpiralTightness / max(_VortexRadius, 1.0) - _Time.y * _VortexSpinSpeed;
                float spiral = sin(spiralPhase) * 0.5 + 0.5;
                float spiralStrength = (1.0 - normDist) * _VortexSpiralHeight;
                float spiralDisp = spiral * spiralStrength;

                // Turbulence
                float2 turbUV = worldPos.xz * _VortexTurbulenceScale / max(_VortexRadius, 1.0) + float2(_Time.y * 0.3, _Time.y * 0.2);
                float turb = (fbm2D(turbUV, 3) - 0.5) * 2.0;
                float turbDisp = turb * _VortexTurbulence * normDist;

                // Gerstner (same 4 waves as ocean, bent inward)
                float3 samplePos = worldPos;
                samplePos.x += sin(_Time.y * 0.05) * 2.0;
                samplePos.z += cos(_Time.y * 0.037) * 2.0;
                float totalDx = 0, totalDz = 0, dxT, dzT, gerstner = 0;
                float gerstnerAtten = smoothstep(0.0, 0.4, normDist);
                float2 inwardDir = -dir;
                float bendAmount = 1.0 - normDist;

                float2 wd0 = normalize(lerp(float2(0.8,0.2), inwardDir, bendAmount));
                float2 wd1 = normalize(lerp(float2(-0.4,0.9), inwardDir, bendAmount));
                float2 wd2 = normalize(lerp(float2(0.6,-0.7), inwardDir, bendAmount));
                float2 wd3 = normalize(lerp(float2(-0.9,-0.3), inwardDir, bendAmount));

                gerstner += GerstnerWave(wd0, 0.35, 18.0, _WaveSpeed, 1.3, samplePos, dxT, dzT); totalDx+=dxT; totalDz+=dzT;
                gerstner += GerstnerWave(wd1, 0.25, 12.0, _WaveSpeed*0.8, 2.7, samplePos, dxT, dzT); totalDx+=dxT; totalDz+=dzT;
                gerstner += GerstnerWave(wd2, 0.20, 8.0, _WaveSpeed*1.2, 4.1, samplePos, dxT, dzT); totalDx+=dxT; totalDz+=dzT;
                gerstner += GerstnerWave(wd3, 0.15, 5.0, _WaveSpeed*1.4, 5.9, samplePos, dxT, dzT); totalDx+=dxT; totalDz+=dzT;

                gerstner *= _WaveHeight * gerstnerAtten;
                totalDx *= _WaveHeight * gerstnerAtten;
                totalDz *= _WaveHeight * gerstnerAtten;

                // Ripple (same as ocean)
                float rdX, rdZ;
                float ripple = ComputeRipple(worldPos, rdX, rdZ);
                totalDx += rdX; totalDz += rdZ;

                // Pull + combine
                float pullAmount = (1.0 - normDist) * _VortexPullStrength;
                float2 pullOffset = -dir * pullAmount;
                float totalY = depression + spiralDisp + turbDisp + gerstner + ripple;
                float3 displacedWS = worldPos + float3(pullOffset.x, totalY, pullOffset.y);

                // Normal
                float fns = funnel * _VortexDepth * 0.05;
                float3 funnelN = normalize(float3(dir.x * fns, 1.0, dir.y * fns));
                float3 waveN = normalize(float3(-totalDx, 1.0, -totalDz));
                float3 combinedN = normalize(lerp(waveN, funnelN, funnel * 0.6));

                // Foam: spiral + turbulence + gerstner crests
                float foam = 0;
                foam += saturate((spiral * spiralStrength - _FoamThreshold) * 3.0);
                foam += saturate(abs(turb) - 0.3) * normDist;
                foam += saturate((gerstner - _FoamThreshold) * 4.0);
                foam *= _FoamIntensity * gerstnerAtten;

                // Vortex distortion vector for underwater lights
                float spiralTangentX = -sin(angle + spiralPhase * 0.3);
                float spiralTangentZ = cos(angle + spiralPhase * 0.3);
                float distortStr = 1.0 - normDist;
                float2 distortDir = (-dir * pullAmount + float2(spiralTangentX, spiralTangentZ) * spiralStrength * 0.5) * distortStr;

                OUT.positionWS    = displacedWS;
                OUT.normalWS      = combinedN;
                OUT.positionCS    = TransformWorldToHClip(displacedWS);
                OUT.fogFactor     = ComputeFogFactor(OUT.positionCS.z);
                OUT.gerstnerHeight = gerstner;
                OUT.foamMask      = saturate(foam);
                OUT.vortexBlend   = 1.0 - normDist;
                OUT.vortexDistort = distortDir;
                OUT.worldSampleXZ = worldPos.xz;
                OUT.screenPos     = ComputeScreenPos(OUT.positionCS);

                return OUT;
            }

            // =============================================
            // LIGHTING (same as ocean)
            // =============================================
            float3 CalcLight(float3 normalWS, float3 viewDir, Light light)
            {
                float NdotL = saturate(dot(normalWS, normalize(light.direction)));
                float3 atten = light.color * light.distanceAttenuation * light.shadowAttenuation;
                float3 diffuse = atten * NdotL;
                float3 halfDir = normalize(normalize(light.direction) + viewDir);
                float spec = pow(saturate(dot(normalWS, halfDir)), 128.0);
                return diffuse + atten * spec * 0.5;
            }

            // =============================================
            // CELESTIAL REFLECTION (same as ocean)
            // =============================================
            float3 CalcCelestialReflection(float3 normalWS, float3 viewDir, float3 celestialDir,
                                            float intensity, float tightness, float3 reflColor, float distortion)
            {
                if (celestialDir.y < -0.05) return 0;
                float3 dn = normalize(lerp(float3(0,1,0), normalWS, distortion));
                float3 rd = reflect(-viewDir, dn);
                float sp = pow(saturate(dot(rd, celestialDir)), tightness);
                float3 frd = normalize(float3(rd.x, rd.y*0.3, rd.z));
                float pp = pow(saturate(dot(frd, normalize(float3(celestialDir.x, celestialDir.y*0.3, celestialDir.z)))), tightness*0.15) * 0.25;
                return reflColor * (sp + pp) * intensity * smoothstep(-0.05, 0.3, celestialDir.y);
            }

            // =============================================
            // FRAGMENT (matches ocean + vortex extras)
            // =============================================
            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float vortexBlend = IN.vortexBlend;

                // ---- DEPTH FADE (same as ocean — silhouettes of objects below) ----
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float sceneDepthRaw = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
                float sceneDepthEye = LinearEyeDepth(sceneDepthRaw, _ZBufferParams);
                float waterDepthEye = LinearEyeDepth(IN.positionCS.z, _ZBufferParams);
                // Only count positive depth diff (object behind vortex surface)
                float depthDiff = max(sceneDepthEye - waterDepthEye, 0.0);
                // Far plane = no object behind = full deep color
                float depthFactor = (sceneDepthEye > 900.0) ? 1.0 : saturate(depthDiff / _DepthFadeDistance);

                // ---- COLOR (same logic as ocean: gerstner + world noise) ----
                float heightFactor = saturate(IN.gerstnerHeight * 0.5 + 0.5);

                // Use pre-displacement world XZ for color noise,
                // not positionWS which is distorted by funnel/pull/spiral
                float2 anchoredXZ = IN.worldSampleXZ;
                float2 colorNoiseUV = anchoredXZ * 0.03 + float2(_Time.y * 0.01, _Time.y * 0.007);
                float colorNoise = sin(colorNoiseUV.x * 2.7 + sin(colorNoiseUV.y * 1.9)) * 0.5 + 0.5;
                float2 colorNoiseUV2 = anchoredXZ * 0.07 + float2(-_Time.y * 0.015, _Time.y * 0.012);
                float colorNoise2 = sin(colorNoiseUV2.x * 3.3 + cos(colorNoiseUV2.y * 2.1)) * 0.5 + 0.5;
                float worldColorVar = colorNoise * 0.6 + colorNoise2 * 0.4;

                float combinedFactor = saturate(heightFactor * 0.6 + worldColorVar * 0.4);
                float3 baseColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, combinedFactor);

                // Shallow tint where objects are close to the surface (same as ocean)
                baseColor = lerp(_DepthShallowColor.rgb, baseColor, depthFactor);

                // ---- VORTEX CORE COLOR ----
                float coreMask = smoothstep(0.6, 0.95, vortexBlend);
                float innerDarken = smoothstep(0.3, 0.8, vortexBlend);
                baseColor = lerp(baseColor, _DeepColor.rgb, innerDarken * 0.5);
                baseColor = lerp(baseColor, _VortexCoreColor.rgb, coreMask);

                // Foam (from vertex)
                float foamMask = IN.foamMask;
                baseColor = lerp(baseColor, _FoamColor.rgb, foamMask);

                // ---- WAKE TRAIL (same as ocean) ----
                float wakeFoam = ComputeWakeTrail(IN.positionWS);
                baseColor = lerp(baseColor, _FoamColor.rgb, wakeFoam);
                foamMask = max(foamMask, wakeFoam);

                // ---- CREATURE SHADOWS (same as ocean) ----
                float creatureShadow = ComputeCreatureShadow(IN.positionWS);
                if (creatureShadow > 0.001)
                {
                    float shadowBlend = creatureShadow * _CreatureShadowIntensity;
                    baseColor = lerp(baseColor, _CreatureShadowColor.rgb, shadowBlend);
                }

                // ---- VIEW (needed for decals and lights) ----
                float3 viewDir = normalize(GetWorldSpaceNormalizeViewDir(IN.positionWS));

                // ---- UNDERWATER DECALS ----
                float4 decalResult = ComputeUnderwaterDecals(IN.positionWS, viewDir);
                if (decalResult.a > 0.001)
                {
                    baseColor = lerp(baseColor, decalResult.rgb, decalResult.a);
                }

                // ---- UNDERWATER LIGHTS ----
                float3 underwaterGlow = ComputeUnderwaterLights(
                    IN.positionWS, viewDir,
                    IN.vortexDistort, vortexBlend
                );

                // ---- LIGHTING (same as ocean) ----
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDir;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                float3 lighting = 0;
                // Use main light WITHOUT shadow map to avoid cascade ring artifacts.
                // The vortex displaces vertices deep into the funnel, causing them
                // to fall into different shadow cascades and create visible rings.
                // Ocean water in the open doesn't receive meaningful shadow map shadows anyway.
                Light mainLight = GetMainLight();
                lighting += CalcLight(normalWS, viewDir, mainLight);

                #if defined(_ADDITIONAL_LIGHTS)
                #if USE_CLUSTER_LIGHT_LOOP
                UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                { Light al = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1)); lighting += CalcLight(normalWS, viewDir, al); }
                #endif
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light al = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    lighting += CalcLight(normalWS, viewDir, al);
                LIGHT_LOOP_END
                #endif

                lighting += SampleSH(normalWS);

                // Vortex core darkening
                float coreDarken = lerp(1.0, 0.3, coreMask);
                lighting *= coreDarken;

                float3 finalColor = baseColor * lighting;

                // Underwater glow
                finalColor += underwaterGlow * (1.0 - foamMask * 0.7);

                // ---- CELESTIAL REFLECTIONS (same as ocean, fades toward core) ----
                float3 sunDir = normalize(_CelestialSunDir.xyz);
                float3 moonDir = normalize(_CelestialMoonDir.xyz);
                float dayFactor = _CelestialDayFactor;
                float reflFade = 1.0 - smoothstep(0.3, 0.8, vortexBlend);

                float3 sunRefl = CalcCelestialReflection(normalWS, viewDir, sunDir,
                    _SunReflectionIntensity, _SunReflectionSize, _SunReflectionColor.rgb, _ReflectionDistortion) * dayFactor * reflFade;
                float3 moonRefl = CalcCelestialReflection(normalWS, viewDir, moonDir,
                    _MoonReflectionIntensity, _MoonReflectionSize, _MoonReflectionColor.rgb, _ReflectionDistortion) * (1.0-dayFactor) * reflFade;

                finalColor += (sunRefl + moonRefl) * (1.0 - foamMask * 0.8);

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
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor; float4 _ShallowColor; float4 _FoamColor; float4 _RippleColor;
                float _RippleColorIntensity; float _WaveHeight; float _WaveSpeed; float _WaveScale;
                float _FoamThreshold; float _FoamIntensity;
                float _DepthFadeDistance; float4 _DepthShallowColor; float _DepthMinAlpha;
                float _SunReflectionIntensity; float _SunReflectionSize; float4 _SunReflectionColor;
                float _MoonReflectionIntensity; float _MoonReflectionSize; float4 _MoonReflectionColor;
                float _ReflectionDistortion;
                float4 _CreatureShadowColor; float _CreatureShadowIntensity; float _CreatureShadowSoftness;
                float _SubsurfaceIntensity; float _SubsurfaceAbsorption; float4 _SubsurfaceTint;
                float _SubsurfaceCausticScale; float _SubsurfaceCausticSpeed;
                float4 _VortexCoreColor;
                float _VortexRadius; float _VortexDepth; float _VortexInnerRadius; float _VortexFunnelPower;
                float _VortexSpinSpeed; float _VortexSpiralTightness; float _VortexSpiralHeight; float _VortexSpiralCount;
                float _VortexTurbulence; float _VortexTurbulenceScale; float _VortexPullStrength;
            CBUFFER_END

            float3 _LightDirection;

            Varyings vertShadow(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 vc = TransformObjectToWorld(float3(0,0,0));
                float2 d = worldPos.xz - vc.xz; float dist = length(d);
                float2 dir = dist > 0.001 ? d/dist : float2(0,0);
                float nd = saturate(dist / max(_VortexRadius, 0.01));
                float ft = 1.0 - saturate((nd - _VortexInnerRadius)/(1.0-_VortexInnerRadius));
                float dep = -pow(ft, _VortexFunnelPower) * _VortexDepth;
                float pa = (1.0-nd)*_VortexPullStrength;
                float3 dws = worldPos + float3(-dir.x*pa, dep, -dir.y*pa);
                float3 nws = TransformObjectToWorldNormal(IN.normalOS);
                float4 pcs = TransformWorldToHClip(ApplyShadowBias(dws, nws, _LightDirection));
                #if UNITY_REVERSED_Z
                    pcs.z = min(pcs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    pcs.z = max(pcs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = pcs;
                return OUT;
            }
            half4 fragShadow(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
