using UnityEngine;

/// <summary>
/// Underwater light source visible through the water surface.
/// Uses dual radius system:
///   - Inner Radius: bright concentrated core (the "point" of light)
///   - Outer Radius: soft diffuse halo around it
///   - Concentration: balance between core and halo
///
/// Example configurations:
///   Bioluminescent creature: inner=0.5, outer=8, concentration=0.9, intensity=3
///   Sunken lantern:          inner=1,   outer=3, concentration=0.7, intensity=5
///   Deep abyss glow:         inner=2,   outer=30, concentration=0.3, intensity=1
///   Lava vent:               inner=3,   outer=15, concentration=0.5, intensity=4
/// </summary>
public class UnderwaterLight : MonoBehaviour
{
    [Header("Light Settings")]
    [Tooltip("Color of the underwater glow")]
    public Color lightColor = new Color(0.2f, 0.8f, 1f, 1f);

    [Tooltip("Brightness multiplier")]
    [Range(0f, 10f)]
    public float intensity = 2f;

    [Header("Radius")]
    [Tooltip("Inner radius: bright concentrated core. Small = sharp point of light.")]
    public float innerRadius = 1f;

    [Tooltip("Outer radius: soft diffuse halo. Large = wide ambient glow.")]
    public float outerRadius = 10f;

    [Tooltip("How much light is concentrated in the inner core vs spread in the outer halo.\n" +
             "1.0 = all light in the core (laser-like point)\n" +
             "0.0 = all light in the halo (diffuse glow)\n" +
             "0.5 = balanced mix")]
    [Range(0f, 1f)]
    public float concentration = 0.6f;

    [Header("Animation")]
    [Tooltip("Pulse the intensity over time")]
    public bool pulse = false;

    [Tooltip("Pulse speed (oscillations per second)")]
    public float pulseSpeed = 1f;

    [Tooltip("How much the intensity varies (0 = none, 1 = full off/on)")]
    [Range(0f, 1f)]
    public float pulseAmount = 0.3f;

    [Tooltip("Flicker randomly (bioluminescence effect)")]
    public bool flicker = false;

    [Tooltip("Flicker intensity variation")]
    [Range(0f, 1f)]
    public float flickerAmount = 0.2f;

    [Header("Shadow / Occlusion")]
    [Tooltip("Enable occlusion: objects between the light and surface block the glow")]
    public bool castShadow = true;

    [Tooltip("Which layers can block this light")]
    public LayerMask shadowLayers = ~0; // everything by default

    [Tooltip("Number of rays in the cone (more = smoother partial occlusion, more expensive)\n" +
             "1 = single center ray (cheapest)\n" +
             "5 = center + 4 cardinal\n" +
             "9 = center + 8 around")]
    [Range(1, 9)]
    public int shadowRays = 5;

    [Tooltip("Spread angle of the shadow cone in degrees (how wide the sample area is)")]
    [Range(5f, 45f)]
    public float shadowConeAngle = 20f;

    [Tooltip("How smoothly the occlusion transitions (higher = slower response)")]
    [Range(1f, 20f)]
    public float shadowSmoothing = 8f;

    /// <summary>
    /// Current occlusion factor: 1 = fully visible, 0 = fully blocked.
    /// Updated by UnderwaterLightManager each frame.
    /// </summary>
    [HideInInspector]
    public float currentOcclusion = 1f;

    private float flickerOffset;

    void OnEnable()
    {
        flickerOffset = Random.Range(0f, 100f);
        currentOcclusion = 1f;
    }

    public float GetEffectiveIntensity()
    {
        float i = intensity;

        if (pulse)
        {
            float p = Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
            i *= Mathf.Lerp(1f, p, pulseAmount);
        }

        if (flicker)
        {
            float t = Time.time + flickerOffset;
            float n = Mathf.PerlinNoise(t * 5f, flickerOffset) * 0.5f
                    + Mathf.PerlinNoise(t * 13f, flickerOffset + 50f) * 0.3f
                    + Mathf.PerlinNoise(t * 31f, flickerOffset + 100f) * 0.2f;
            i *= Mathf.Lerp(1f, n, flickerAmount);
        }

        // Apply shadow occlusion
        i *= currentOcclusion;

        return Mathf.Max(i, 0f);
    }

    /// <summary>
    /// Compute occlusion via cone raycasts from light toward the surface.
    /// Returns 0 (fully blocked) to 1 (fully visible).
    /// Called by UnderwaterLightManager.
    /// </summary>
    public float ComputeOcclusion(float surfaceY)
    {
        if (!castShadow) return 1f;

        Vector3 pos = transform.position;
        Vector3 toSurface = new Vector3(0f, surfaceY - pos.y, 0f);
        float dist = toSurface.magnitude;
        if (dist < 0.01f) return 1f;

        Vector3 upDir = toSurface.normalized;
        int hits = 0;

        if (shadowRays == 1)
        {
            // Single center ray
            if (Physics.Raycast(pos, upDir, dist, shadowLayers, QueryTriggerInteraction.Ignore))
                hits++;
            return hits == 0 ? 1f : 0f;
        }

        // Cone pattern: center + surrounding rays
        float spreadRad = shadowConeAngle * Mathf.Deg2Rad;
        float spreadDist = Mathf.Tan(spreadRad) * dist;

        // Center ray always included
        if (Physics.Raycast(pos, upDir, dist, shadowLayers, QueryTriggerInteraction.Ignore))
            hits++;

        // Surrounding rays in a cone
        int surroundCount = shadowRays - 1;
        for (int i = 0; i < surroundCount; i++)
        {
            float angle = (float)i / surroundCount * Mathf.PI * 2f;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * spreadDist,
                0f,
                Mathf.Sin(angle) * spreadDist
            );
            Vector3 target = pos + toSurface + offset;
            Vector3 dir = (target - pos).normalized;
            float rayDist = (target - pos).magnitude;

            if (Physics.Raycast(pos, dir, rayDist, shadowLayers, QueryTriggerInteraction.Ignore))
                hits++;
        }

        return 1f - (float)hits / shadowRays;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.6f);
        Gizmos.DrawWireSphere(transform.position, innerRadius);

        Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.15f);
        Gizmos.DrawWireSphere(transform.position, outerRadius);

        Vector3 surfacePos = new Vector3(transform.position.x, 0f, transform.position.z);
        Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.4f);
        Gizmos.DrawLine(transform.position, surfacePos);

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        DrawCircleGizmo(surfacePos, innerRadius);
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        DrawCircleGizmo(surfacePos, outerRadius);
    }

    void DrawCircleGizmo(Vector3 center, float r)
    {
        int segments = 32;
        Vector3 prev = center + new Vector3(r, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
