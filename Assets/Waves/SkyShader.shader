Shader "Custom/PixelatedSkybox"
{
    Properties
    {
        [Header(Pixelation)]
        _PixelSize ("Pixel Size", Range(1, 1028)) = 64
        
        [Header(Sky Colors Day)]
        _DayTopColor ("Day Top Color", Color) = (0.2, 0.5, 0.95, 1)
        _DayHorizonColor ("Day Horizon Color", Color) = (0.7, 0.85, 1.0, 1)
        _DayBottomColor ("Day Bottom Color", Color) = (0.85, 0.92, 1.0, 1)
        
        [Header(Sky Colors Night)]
        _NightTopColor ("Night Top Color", Color) = (0.01, 0.01, 0.05, 1)
        _NightHorizonColor ("Night Horizon Color", Color) = (0.05, 0.05, 0.15, 1)
        _NightBottomColor ("Night Bottom Color", Color) = (0.02, 0.02, 0.08, 1)
        
        [Header(Sunset Colors)]
        _SunsetColor ("Sunset/Sunrise Color", Color) = (1.0, 0.4, 0.1, 1)
        _SunsetRange ("Sunset Range", Range(0.0, 1.0)) = 0.3
        _SunsetIntensity ("Sunset Intensity", Range(0.0, 2.0)) = 1.0
        
        [Header(Sun)]
        _SunTex ("Sun Texture", 2D) = "white" {}
        _SunSize ("Sun Size", Range(0.01, 0.5)) = 0.08
        _SunIntensity ("Sun Intensity", Range(0.5, 5.0)) = 1.5
        
        [Header(Sun Orbit  2 Axis)]
        _SunOrbitPitch ("Sun Orbit Pitch (tilt)", Range(-90, 90)) = 0
        _SunOrbitYaw ("Sun Orbit Yaw (rotation)", Range(-180, 180)) = 0
        
        [Header(Moon)]
        _MoonTex ("Moon Texture", 2D) = "white" {}
        _MoonSize ("Moon Size", Range(0.01, 0.5)) = 0.06
        _MoonIntensity ("Moon Intensity", Range(0.5, 10.0)) = 2.0
        
        [Header(Moon Orbit  2 Axis)]
        _MoonOrbitPitch ("Moon Orbit Pitch (tilt)", Range(-90, 90)) = 0
        _MoonOrbitYaw ("Moon Orbit Yaw (rotation)", Range(-180, 180)) = 0
        [Toggle] _MoonOppositeSun ("Moon Opposite Sun", Float) = 1
        
        [Header(Moon Glow)]
        _MoonGlowColor ("Moon Glow Color", Color) = (0.15, 0.2, 0.4, 1)
        _MoonGlowIntensity ("Moon Glow Intensity", Range(0.0, 5.0)) = 1.5
        _MoonGlowSize ("Moon Glow Size", Range(0.01, 1.0)) = 0.25
        _MoonSkyTint ("Moon Sky Tint Strength", Range(0.0, 1.0)) = 0.3
        
        [Header(Stars)]
        _StarDensity ("Star Density (quantity)", Range(1, 200)) = 80
        _StarBrightness ("Star Brightness", Range(0.0, 3.0)) = 1.5
        [Toggle] _StarTwinkle ("Star Twinkle", Float) = 1
        _StarTwinkleSpeed ("Star Twinkle Speed", Range(0.0, 10.0)) = 3.0
        _StarTwinkleMin ("Star Twinkle Min Brightness", Range(0.0, 1.0)) = 0.5
        _StarSize ("Star Size (visual)", Range(0.000001, 0.005)) = 0.001
        
        [Header(Star Movement)]
        _StarMoveSpeed ("Star Move Speed", Range(0.0, 2.0)) = 0.05
        _StarMoveAngle ("Star Move Direction (degrees)", Range(-180, 180)) = 15
        
        [Header(Day Night Cycle)]
        _TimeOfDay ("Time of Day", Range(0.0, 1.0)) = 0.5
        [Toggle] _AutoCycle ("Auto Cycle", Float) = 0
        _CycleSpeed ("Cycle Speed", Range(0.001, 0.1)) = 0.02
        
        [Header(Gradient Steps)]
        _GradientSteps ("Gradient Steps (0 = smooth)", Range(0, 128)) = 8
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Background" 
            "Queue" = "Background" 
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Skybox"
        }
        
        Cull Off
        ZWrite Off
        
        Pass
        {
            Name "PixelatedSkybox"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // ----- Properties -----
            float _PixelSize;
            
            float4 _DayTopColor;
            float4 _DayHorizonColor;
            float4 _DayBottomColor;
            
            float4 _NightTopColor;
            float4 _NightHorizonColor;
            float4 _NightBottomColor;
            
            float4 _SunsetColor;
            float _SunsetRange;
            float _SunsetIntensity;
            
            TEXTURE2D(_SunTex);
            SAMPLER(sampler_SunTex);
            float _SunSize;
            float _SunIntensity;
            float _SunOrbitPitch;
            float _SunOrbitYaw;
            
            TEXTURE2D(_MoonTex);
            SAMPLER(sampler_MoonTex);
            float _MoonSize;
            float _MoonIntensity;
            float _MoonOrbitPitch;
            float _MoonOrbitYaw;
            float _MoonOppositeSun;
            
            float4 _MoonGlowColor;
            float _MoonGlowIntensity;
            float _MoonGlowSize;
            float _MoonSkyTint;
            
            float _StarDensity;
            float _StarBrightness;
            float _StarTwinkle;
            float _StarTwinkleSpeed;
            float _StarTwinkleMin;
            float _StarSize;
            float _StarMoveSpeed;
            float _StarMoveAngle;
            
            float _TimeOfDay;
            float _AutoCycle;
            float _CycleSpeed;
            
            float _GradientSteps;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };
            
            // ----- Utility -----
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            
            float hash31(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }
            
            float hash31b(float3 p)
            {
                p = frac(p * float3(127.1, 311.7, 74.7));
                p += dot(p, p.zyx + 31.32);
                return frac((p.y + p.z) * p.x);
            }
            
            // Rotate a 3D direction around the Y axis
            float3 RotateAroundY(float3 d, float angle)
            {
                float s = sin(angle); float c = cos(angle);
                return float3(d.x * c + d.z * s, d.y, -d.x * s + d.z * c);
            }
            
            // Rotate a 3D direction around an arbitrary axis (for star tilt)
            float3 RotateAroundAxis(float3 d, float3 axis, float angle)
            {
                float s = sin(angle); float c = cos(angle);
                return d * c + cross(axis, d) * s + axis * dot(axis, d) * (1.0 - c);
            }
            
            float3 Quantize(float3 col, float steps)
            {
                if (steps <= 0) return col;
                return floor(col * steps) / steps;
            }
            
            float3 PixelateDir(float3 dir, float pixelSize)
            {
                float theta = atan2(dir.z, dir.x);
                float phi = asin(clamp(dir.y / length(dir), -1.0, 1.0));
                
                float angularStep = PI / pixelSize;
                theta = floor(theta / angularStep) * angularStep;
                phi = floor(phi / angularStep) * angularStep;
                
                float cosPhi = cos(phi);
                return float3(cos(theta) * cosPhi, sin(phi), sin(theta) * cosPhi);
            }
            
            // ----- Rotation matrices -----
            float3x3 RotX(float a)
            {
                float s = sin(a); float c = cos(a);
                return float3x3(1,0,0, 0,c,-s, 0,s,c);
            }
            
            float3x3 RotY(float a)
            {
                float s = sin(a); float c = cos(a);
                return float3x3(c,0,s, 0,1,0, -s,0,c);
            }
            
            // ----- Orbit calculation -----
            // Time drives main rotation in YZ plane; pitch & yaw tilt the orbit plane.
            float3 ComputeOrbitDir(float mainAngle, float pitchDeg, float yawDeg)
            {
                float3 baseDir = float3(0, sin(mainAngle), cos(mainAngle));
                float3x3 rot = mul(RotY(yawDeg * PI / 180.0), RotX(pitchDeg * PI / 180.0));
                return normalize(mul(rot, baseDir));
            }
            
            float3 GetSunDir(float t, float pitch, float yaw)
            {
                return ComputeOrbitDir(t * TWO_PI - HALF_PI, pitch, yaw);
            }
            
            float3 GetMoonDir(float t, float3 sunDir, float pitch, float yaw, float opposite)
            {
                if (opposite > 0.5) return -sunDir;
                return ComputeOrbitDir(t * TWO_PI + HALF_PI, pitch, yaw);
            }
            
            // ----- Vertex -----
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.viewDir = IN.positionOS.xyz;
                return OUT;
            }
            
            // ----- Fragment -----
            float4 frag(Varyings IN) : SV_Target
            {
                // Time
                float timeOfDay = _TimeOfDay;
                if (_AutoCycle > 0.5)
                    timeOfDay = frac(_Time.y * _CycleSpeed);
                
                // Pixelate
                float3 viewDir = normalize(IN.viewDir);
                float3 normPixDir = normalize(PixelateDir(viewDir, _PixelSize));
                float height = normPixDir.y * 0.5 + 0.5;
                
                // Celestial directions (2-axis)
                float3 sunDir = GetSunDir(timeOfDay, _SunOrbitPitch, _SunOrbitYaw);
                float3 moonDir = GetMoonDir(timeOfDay, sunDir, _MoonOrbitPitch, _MoonOrbitYaw, _MoonOppositeSun);
                
                // Day factor
                float dayFactor = saturate(sunDir.y * 3.0 + 0.5);
                float nightAlpha = 1.0 - dayFactor;
                
                // Sunset factor
                float sunsetFactor = pow(saturate(1.0 - abs(sunDir.y)), 2.0) * saturate(1.0 - abs(sunDir.y * 2.0));
                sunsetFactor = saturate(sunsetFactor * 2.0);
                
                // ======== SKY GRADIENT ========
                float3 dayColor = height > 0.5
                    ? lerp(_DayHorizonColor.rgb, _DayTopColor.rgb, (height - 0.5) * 2.0)
                    : lerp(_DayBottomColor.rgb, _DayHorizonColor.rgb, height * 2.0);
                
                float3 nightColor = height > 0.5
                    ? lerp(_NightHorizonColor.rgb, _NightTopColor.rgb, (height - 0.5) * 2.0)
                    : lerp(_NightBottomColor.rgb, _NightHorizonColor.rgb, height * 2.0);
                
                float3 skyColor = lerp(nightColor, dayColor, dayFactor);
                
                // ======== SUNSET TINT ========
                float horizonMask = pow(saturate(1.0 - abs(normPixDir.y)), 1.5);
                float3 sunFlat = normalize(float3(sunDir.x, 0, sunDir.z) + 0.0001);
                float3 viewFlat = normalize(float3(normPixDir.x, 0, normPixDir.z) + 0.0001);
                float sunInfluence = saturate(dot(viewFlat, sunFlat) * 0.5 + 0.5);
                float sunsetMask = horizonMask * sunsetFactor * sunInfluence * _SunsetIntensity;
                skyColor = lerp(skyColor, _SunsetColor.rgb, saturate(sunsetMask * _SunsetRange * 3.0));
                
                // ======== MOON GLOW & SKY TINT ========
                if (nightAlpha > 0.01 && moonDir.y > -0.1)
                {
                    float moonProximity = saturate(dot(normPixDir, moonDir));
                    float moonUp = saturate(moonDir.y);
                    float moonVisBase = nightAlpha * saturate(moonDir.y * 3.0 + 0.5);
                    
                    // Radial glow around moon
                    float glowFalloff = pow(moonProximity, 1.0 / max(_MoonGlowSize, 0.001));
                    skyColor += _MoonGlowColor.rgb * glowFalloff * _MoonGlowIntensity * moonVisBase;
                    
                    // Global sky tint from moonlight
                    skyColor = lerp(skyColor, skyColor + _MoonGlowColor.rgb * 0.15, _MoonSkyTint * nightAlpha * moonUp);
                }
                
                // ======== QUANTIZE ========
                if (_GradientSteps > 0)
                    skyColor = Quantize(skyColor, _GradientSteps);
                
                // ======== STARS (3D cell-based, no seam, truly moving) ========
                // Strategy: stars live at fixed positions on a sphere. We rotate
                // that sphere over time. The pixel checks which rotated star
                // positions land near it. Stars genuinely slide across the sky.
                if (nightAlpha > 0.01 && normPixDir.y > -0.1)
                {
                    // Build the rotation that moves the star sphere over time
                    float moveRad = _StarMoveAngle * PI / 180.0;
                    float3 moveAxis = normalize(float3(sin(moveRad), cos(moveRad), 0.0));
                    float moveAmount = _Time.y * _StarMoveSpeed * 0.1;
                    
                    // INVERSE rotation: rotate the view direction backwards into
                    // the star field's rest frame, so we can do the cell lookup
                    // in a static grid. The stars themselves move because the
                    // mapping from screen to grid shifts smoothly over time.
                    // But crucially we compare in WORLD space after rotating
                    // star positions FORWARD — so the star dot slides on screen.
                    
                    // We'll iterate a grid in the UN-rotated frame to find candidate stars,
                    // then rotate each star's position forward and compare to the actual pixel.
                    
                    // To know which cells to check, rotate pixel INTO rest frame
                    float3 restDir = RotateAroundAxis(normPixDir, moveAxis, -moveAmount);
                    float3 scaledRest = restDir * _StarDensity;
                    float3 cellID = floor(scaledRest);
                    
                    float totalStar = 0.0;
                    
                    for (int ox = -1; ox <= 1; ox++)
                    for (int oy = -1; oy <= 1; oy++)
                    for (int oz = -1; oz <= 1; oz++)
                    {
                        float3 neighbor = cellID + float3(ox, oy, oz);
                        
                        // Density controls probability: higher density = more stars per cell
                        float prob = hash31(neighbor);
                        float spawnChance = saturate(_StarDensity * 0.006);
                        if (prob > spawnChance) continue;
                        
                        // Star's rest-frame position (fixed, never changes)
                        float3 starRestPos = neighbor + float3(
                            hash31(neighbor + 1.0),
                            hash31(neighbor + 2.0),
                            hash31(neighbor + 3.0)
                        );
                        float3 starRestDir = normalize(starRestPos);
                        
                        // Rotate star FORWARD into world frame (this is the actual movement)
                        float3 starWorldDir = RotateAroundAxis(starRestDir, moveAxis, moveAmount);
                        
                        // Size controls the angular radius of the star dot on screen
                        float angDist = 1.0 - dot(normPixDir, starWorldDir);
                        float starRadius = _StarSize;
                        
                        if (angDist < starRadius)
                        {
                            float randBright = hash31b(neighbor);
                            
                            // Twinkle
                            float twinkle = 1.0;
                            if (_StarTwinkle > 0.5)
                            {
                                float twinklePhase = hash31(neighbor + 50.0) * TWO_PI;
                                float twinkleFreq = hash31(neighbor + 60.0) * 1.5 + 0.5;
                                float twinkleRaw = sin(_Time.y * _StarTwinkleSpeed * twinkleFreq + twinklePhase) * 0.5 + 0.5;
                                twinkle = lerp(_StarTwinkleMin, 1.0, twinkleRaw);
                            }
                            
                            float sizeVar = lerp(0.3, 1.0, hash31(neighbor + 70.0));
                            
                            totalStar += randBright * sizeVar * twinkle * _StarBrightness;
                        }
                    }
                    
                    // Fade near horizon with smooth falloff
                    float horizonFade = smoothstep(0.0, 1.0, saturate(normPixDir.y * 4.0));
                    
                    skyColor += totalStar * nightAlpha * horizonFade;
                }
                
                // ======== SUN TEXTURE ========
                if (sunDir.y > -0.2)
                {
                    float sunAngle = acos(saturate(dot(normPixDir, sunDir)));
                    if (sunAngle < _SunSize * PI)
                    {
                        float3 up = abs(sunDir.y) > 0.999 ? float3(0, 0, 1) : float3(0, 1, 0);
                        float3 right = normalize(cross(sunDir, up));
                        float3 upDir = normalize(cross(right, sunDir));
                        
                        float3 delta = normPixDir - sunDir;
                        float2 uv = float2(dot(delta, right), dot(delta, upDir)) / (_SunSize * 2.0) + 0.5;
                        
                        if (all(uv > 0.0) && all(uv < 1.0))
                        {
                            float4 s = SAMPLE_TEXTURE2D_LOD(_SunTex, sampler_SunTex, uv, 0);
                            float vis = saturate(sunDir.y * 5.0 + 1.0);
                            skyColor = lerp(skyColor, s.rgb * _SunIntensity, s.a * vis);
                        }
                    }
                }
                
                // ======== MOON TEXTURE (enhanced emission) ========
                if (moonDir.y > -0.2)
                {
                    float moonAngle = acos(saturate(dot(normPixDir, moonDir)));
                    if (moonAngle < _MoonSize * PI)
                    {
                        float3 up = abs(moonDir.y) > 0.999 ? float3(0, 0, 1) : float3(0, 1, 0);
                        float3 right = normalize(cross(moonDir, up));
                        float3 upDir = normalize(cross(right, moonDir));
                        
                        float3 delta = normPixDir - moonDir;
                        float2 uv = float2(dot(delta, right), dot(delta, upDir)) / (_MoonSize * 2.0) + 0.5;
                        
                        if (all(uv > 0.0) && all(uv < 1.0))
                        {
                            float4 m = SAMPLE_TEXTURE2D_LOD(_MoonTex, sampler_MoonTex, uv, 0);
                            float vis = saturate(moonDir.y * 5.0 + 1.0);
                            
                            // Enhanced moon: base texture * intensity + glow color emission
                            float3 moonColor = m.rgb * _MoonIntensity + _MoonGlowColor.rgb * m.a * _MoonGlowIntensity * 0.3;
                            
                            // Moon stays slightly visible even during day (0.15 minimum)
                            float fade = max(nightAlpha, 0.15);
                            skyColor = lerp(skyColor, moonColor, m.a * vis * fade);
                        }
                    }
                }
                
                return float4(skyColor, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack Off
}
