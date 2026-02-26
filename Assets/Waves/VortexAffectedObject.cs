using UnityEngine;

/// <summary>
/// Attach to any object to make it spiral toward a vortex center,
/// riding the water surface. Matches the vortex shader displacement
/// so the object follows the funnel shape exactly.
///
/// Usage:
/// 1. Attach to any GameObject (boat, debris, barrel, etc.)
/// 2. Assign the vortex object (the plane with OceanVortex shader)
/// 3. Object will spiral inward, matching the water surface
/// 4. When it reaches the core, onReachedCore fires and the object
///    can be destroyed, teleported, or whatever you want.
///
/// The object does NOT need a Rigidbody. Movement is purely kinematic.
/// </summary>
public class VortexAffectedObject : MonoBehaviour
{
    [Header("Vortex Reference")]
    [Tooltip("The GameObject with the OceanVortex material (center = pivot)")]
    public Transform vortexCenter;

    [Tooltip("If null, auto-fetches from vortexCenter's Renderer")]
    public Material vortexMaterial;

    [Header("Spiral Movement")]
    [Tooltip("How fast the object orbits around the vortex (degrees/sec)")]
    public float orbitSpeed = 45f;

    [Tooltip("How fast the object is pulled inward (units/sec)")]
    public float pullSpeed = 2f;

    [Tooltip("Pull accelerates as object gets closer to center")]
    public float pullAcceleration = 1.5f;

    [Tooltip("Orbit speeds up as object gets closer (whirlpool effect)")]
    public float orbitAcceleration = 3f;

    [Header("Surface Following")]
    [Tooltip("Object bobs with the wave surface")]
    public bool followSurface = true;

    [Tooltip("Extra Y offset above the water surface")]
    public float surfaceOffset = 0.0f;

    [Tooltip("How smoothly the object follows the surface height")]
    [Range(1f, 20f)]
    public float surfaceSmoothing = 8f;

    [Header("Tilt / Rotation")]
    [Tooltip("Object tilts toward the vortex center (leaning into the funnel)")]
    public bool tiltTowardCenter = true;

    [Tooltip("How much the object tilts (degrees at the core)")]
    [Range(0f, 60f)]
    public float maxTilt = 35f;

    [Tooltip("Object rotates to face its movement direction")]
    public bool faceMovementDirection = true;

    [Tooltip("Smoothing for rotation changes")]
    [Range(1f, 20f)]
    public float rotationSmoothing = 6f;

    [Header("Wobble (organic feel)")]
    [Tooltip("Random wobble added to the orbit angle")]
    public float wobbleAmount = 5f;

    [Tooltip("Speed of the wobble oscillation")]
    public float wobbleSpeed = 2f;

    [Header("Core Behavior")]
    [Tooltip("Distance from center considered 'reached core' (normalized 0-1)")]
    [Range(0.01f, 0.3f)]
    public float coreThreshold = 0.08f;

    [Tooltip("What happens when the object reaches the core")]
    public CoreAction coreAction = CoreAction.Destroy;

    [Tooltip("Seconds to sink before core action (object goes below surface)")]
    public float sinkDuration = 1.5f;

    public enum CoreAction
    {
        Nothing,
        Destroy,
        Disable,
        Respawn
    }

    [Header("Respawn Settings (if CoreAction = Respawn)")]
    [Tooltip("Respawn at this distance from center (normalized 0-1)")]
    [Range(0.5f, 1.0f)]
    public float respawnDistance = 0.9f;

    // Events
    public System.Action onReachedCore;

    // Cached vortex params
    private float vortexRadius;
    private float vortexDepth;
    private float vortexInnerRadius;
    private float vortexFunnelPower;
    private float vortexSpinSpeed;
    private float vortexSpiralTightness;
    private float vortexSpiralHeight;
    private float vortexSpiralCount;
    private float vortexPullStrength;
    private float waveHeight;
    private float waveSpeed;

    // State
    private float currentAngle;
    private float currentRadius;
    private float currentSurfaceY;
    private Vector3 lastPosition;
    private Quaternion targetRotation;
    private bool isSinking;
    private float sinkTimer;
    private float sinkStartY;

    private static readonly int ID_VortexRadius = Shader.PropertyToID("_VortexRadius");
    private static readonly int ID_VortexDepth = Shader.PropertyToID("_VortexDepth");
    private static readonly int ID_VortexInnerRadius = Shader.PropertyToID("_VortexInnerRadius");
    private static readonly int ID_VortexFunnelPower = Shader.PropertyToID("_VortexFunnelPower");
    private static readonly int ID_VortexSpinSpeed = Shader.PropertyToID("_VortexSpinSpeed");
    private static readonly int ID_VortexSpiralTight = Shader.PropertyToID("_VortexSpiralTightness");
    private static readonly int ID_VortexSpiralHeight = Shader.PropertyToID("_VortexSpiralHeight");
    private static readonly int ID_VortexSpiralCount = Shader.PropertyToID("_VortexSpiralCount");
    private static readonly int ID_VortexPullStrength = Shader.PropertyToID("_VortexPullStrength");
    private static readonly int ID_WaveHeight = Shader.PropertyToID("_WaveHeight");
    private static readonly int ID_WaveSpeed = Shader.PropertyToID("_WaveSpeed");

    void Start()
    {
        if (vortexCenter == null)
        {
            Debug.LogWarning("VortexAffectedObject: no vortex center assigned!", this);
            enabled = false;
            return;
        }

        // Auto-get material
        if (vortexMaterial == null)
        {
            var renderer = vortexCenter.GetComponent<Renderer>();
            if (renderer != null)
                vortexMaterial = renderer.sharedMaterial;
        }

        ReadVortexParams();

        // Initialize angle and radius from current position
        Vector3 delta = transform.position - vortexCenter.position;
        currentAngle = Mathf.Atan2(delta.z, delta.x);
        currentRadius = new Vector2(delta.x, delta.z).magnitude;
        currentSurfaceY = transform.position.y;
        lastPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void ReadVortexParams()
    {
        if (vortexMaterial == null) return;

        vortexRadius = vortexMaterial.GetFloat(ID_VortexRadius);
        vortexDepth = vortexMaterial.GetFloat(ID_VortexDepth);
        vortexInnerRadius = vortexMaterial.GetFloat(ID_VortexInnerRadius);
        vortexFunnelPower = vortexMaterial.GetFloat(ID_VortexFunnelPower);
        vortexSpinSpeed = vortexMaterial.GetFloat(ID_VortexSpinSpeed);
        vortexSpiralTightness = vortexMaterial.GetFloat(ID_VortexSpiralTight);
        vortexSpiralHeight = vortexMaterial.GetFloat(ID_VortexSpiralHeight);
        vortexSpiralCount = vortexMaterial.GetFloat(ID_VortexSpiralCount);
        vortexPullStrength = vortexMaterial.GetFloat(ID_VortexPullStrength);
        waveHeight = vortexMaterial.GetFloat(ID_WaveHeight);
        waveSpeed = vortexMaterial.GetFloat(ID_WaveSpeed);
    }

    private float paramTimer = 0f;

    void LateUpdate()
    {
        if (vortexCenter == null) return;

        // Re-read material params once per second, not every frame
        paramTimer -= Time.deltaTime;
        if (paramTimer <= 0f)
        {
            ReadVortexParams();
            paramTimer = .01f;
        }

        float dt = Time.deltaTime;
        Vector3 center = vortexCenter.position;

        if (isSinking)
        {
            UpdateSinking(dt, center);
            return;
        }

        float normDist = Mathf.Clamp01(currentRadius / Mathf.Max(vortexRadius, 0.01f));

        // ---- INWARD PULL (constant, smooth) ----
        float pullMult = 1.0f + (1.0f - normDist) * pullAcceleration;
        currentRadius -= pullSpeed * pullMult * dt;
        currentRadius = Mathf.Max(currentRadius, 0f);

        // ---- ORBIT (constant, smooth) ----
        float orbitMult = 1.0f + (1.0f - normDist) * orbitAcceleration;
        float wobbleMult = 1.0f + Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount * 0.1f;
        currentAngle += orbitSpeed * orbitMult * wobbleMult * Mathf.Deg2Rad * dt;

        // ---- XZ POSITION ----
        float x = center.x + Mathf.Cos(currentAngle) * currentRadius;
        float z = center.z + Mathf.Sin(currentAngle) * currentRadius;

        // ---- SURFACE Y (direct, no smoothing) ----
        float y = center.y;
        if (followSurface)
        {
            y = ComputeVortexSurfaceY(center, x, z);
        }
        y += surfaceOffset;

        Vector3 newPos = new Vector3(x, y, z);

        // ---- ROTATION ----
        UpdateRotation(newPos, center, normDist, dt);

        lastPosition = transform.position;
        transform.position = newPos;

        // ---- CORE CHECK ----
        normDist = Mathf.Clamp01(currentRadius / Mathf.Max(vortexRadius, 0.01f));
        if (normDist <= coreThreshold)
        {
            OnReachedCore();
        }
    }

    /// <summary>
    /// Computes the Y position on the vortex surface at a given XZ point.
    /// Mirrors the shader's vertex displacement math.
    /// </summary>
    float ComputeVortexSurfaceY(Vector3 center, float worldX, float worldZ)
    {
        float baseY = center.y;

        float dx = worldX - center.x;
        float dz = worldZ - center.z;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);
        float normDist = Mathf.Clamp01(dist / Mathf.Max(vortexRadius, 0.01f));

        // Funnel depression only (smooth curve, no rapid oscillations)
        float funnelT = 1.0f - Mathf.Clamp01((normDist - vortexInnerRadius) / (1.0f - vortexInnerRadius));
        float funnel = Mathf.Pow(funnelT, vortexFunnelPower);
        float depression = -funnel * vortexDepth;

        return baseY + depression;
    }

    void UpdateRotation(Vector3 newPos, Vector3 center, float normDist, float dt)
    {
        if (faceMovementDirection)
        {
            // Compute tangent direction analytically from the orbit angle.
            // The orbit moves along (cos(angle), sin(angle)), so the tangent
            // (derivative) is (-sin(angle), cos(angle)). This is always smooth
            // regardless of orbit speed — no frame delta, no atan2 jumps.
            float tangentX = -Mathf.Sin(currentAngle);
            float tangentZ = Mathf.Cos(currentAngle);

            // Blend in a small inward component so the object leans into the spiral
            float inwardBlend = Mathf.Lerp(0f, 0.3f, 1f - normDist);
            Vector3 toCenter = (center - newPos);
            toCenter.y = 0;
            Vector3 inward = toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector3.zero;

            Vector3 facingDir = new Vector3(tangentX, 0f, tangentZ).normalized;
            facingDir = Vector3.Lerp(facingDir, inward, inwardBlend).normalized;

            if (facingDir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(facingDir, Vector3.up);
                targetRotation = lookRot;
            }
        }

        // Tilt toward center
        if (tiltTowardCenter)
        {
            Vector3 toCenter = center - newPos;
            toCenter.y = 0;
            if (toCenter.sqrMagnitude > 0.01f)
            {
                float tiltAngle = maxTilt * (1.0f - normDist);
                Vector3 tiltAxis = Vector3.Cross(Vector3.up, toCenter.normalized);
                Quaternion tilt = Quaternion.AngleAxis(tiltAngle, tiltAxis);
                targetRotation = tilt * targetRotation;
            }
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, dt * rotationSmoothing);
    }

    void OnReachedCore()
    {
        onReachedCore?.Invoke();

        switch (coreAction)
        {
            case CoreAction.Nothing:
                break;

            case CoreAction.Destroy:
                if (sinkDuration > 0)
                {
                    StartSinking();
                }
                else
                {
                    Destroy(gameObject);
                }
                break;

            case CoreAction.Disable:
                if (sinkDuration > 0)
                {
                    StartSinking();
                }
                else
                {
                    gameObject.SetActive(false);
                }
                break;

            case CoreAction.Respawn:
                Respawn();
                break;
        }
    }

    void StartSinking()
    {
        isSinking = true;
        sinkTimer = 0f;
        sinkStartY = transform.position.y;
    }

    void UpdateSinking(float dt, Vector3 center)
    {
        sinkTimer += dt;
        float t = Mathf.Clamp01(sinkTimer / sinkDuration);

        // Keep orbiting while sinking
        float normDist = Mathf.Clamp01(currentRadius / Mathf.Max(vortexRadius, 0.01f));
        float orbitMult = 1.0f + (1.0f - normDist) * orbitAcceleration;
        currentAngle += orbitSpeed * orbitMult * 2f * Mathf.Deg2Rad * dt; // spin fast

        float x = center.x + Mathf.Cos(currentAngle) * currentRadius;
        float z = center.z + Mathf.Sin(currentAngle) * currentRadius;

        // Sink below surface
        float sinkDepth = Mathf.Lerp(0f, vortexDepth * 0.5f, t * t);
        float y = sinkStartY - sinkDepth;

        transform.position = new Vector3(x, y, z);

        // Scale down while sinking
        float scale = Mathf.Lerp(1f, 0.3f, t);
        transform.localScale = Vector3.one * scale;

        if (t >= 1f)
        {
            switch (coreAction)
            {
                case CoreAction.Destroy:
                    Destroy(gameObject);
                    break;
                case CoreAction.Disable:
                    gameObject.SetActive(false);
                    break;
                default:
                    isSinking = false;
                    break;
            }
        }
    }

    void Respawn()
    {
        // Reset to outer edge at random angle
        currentAngle = Random.Range(0f, Mathf.PI * 2f);
        currentRadius = vortexRadius * respawnDistance;
        isSinking = false;
        sinkTimer = 0f;
        transform.localScale = Vector3.one;

        Vector3 center = vortexCenter.position;
        float x = center.x + Mathf.Cos(currentAngle) * currentRadius;
        float z = center.z + Mathf.Sin(currentAngle) * currentRadius;
        float y = ComputeVortexSurfaceY(center, x, z);

        transform.position = new Vector3(x, y, z);
    }

    // =============================================
    // PUBLIC API
    // =============================================

    /// <summary>
    /// Instantly place the object at a specific normalized distance from center.
    /// </summary>
    public void SetNormalizedDistance(float normDist)
    {
        normDist = Mathf.Clamp01(normDist);
        currentRadius = vortexRadius * normDist;
    }

    /// <summary>
    /// Kick the object outward (e.g. it fought the current).
    /// </summary>
    public void PushOutward(float amount)
    {
        currentRadius = Mathf.Min(currentRadius + amount, vortexRadius);
    }

    /// <summary>
    /// Get how far the object is from the center (0=core, 1=edge).
    /// </summary>
    public float GetNormalizedDistance()
    {
        return Mathf.Clamp01(currentRadius / Mathf.Max(vortexRadius, 0.01f));
    }

    // =============================================
    // GIZMOS
    // =============================================

    void OnDrawGizmosSelected()
    {
        if (vortexCenter == null) return;

        // Draw current orbit circle
        Gizmos.color = Color.yellow;
        DrawCircleGizmo(vortexCenter.position, currentRadius, 48);

        // Draw core threshold
        Gizmos.color = Color.red;
        float coreR = (vortexMaterial != null ? vortexMaterial.GetFloat(ID_VortexRadius) : 50f) * coreThreshold;
        DrawCircleGizmo(vortexCenter.position, coreR, 24);

        // Line from object to center
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawLine(transform.position, vortexCenter.position);
    }

    void DrawCircleGizmo(Vector3 center, float radius, int segments)
    {
        Vector3 prev = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = (float)i / segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}