using UnityEngine;

/// <summary>
/// Attach to any object to make it spiral toward a vortex center,
/// riding the water surface. Reads parameters from VortexController.
///
/// Usage:
/// 1. Attach to any GameObject (boat, debris, barrel, etc.)
/// 2. Assign vortexController (or leave null to auto-find VortexController.Instance)
/// 3. Object will spiral inward, matching the water surface
/// 4. When it reaches the core, onReachedCore fires
///
/// The object does NOT need a Rigidbody. Movement is purely kinematic.
/// </summary>
public class VortexAffectedObject : MonoBehaviour
{
    [Header("Vortex Reference")]
    [Tooltip("If null, uses VortexController.Instance automatically")]
    public VortexController vortexController;

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

    // State
    private float currentAngle;
    private float currentRadius;
    private Vector3 lastPosition;
    private Quaternion targetRotation;
    private bool isSinking;
    private float sinkTimer;
    private float sinkStartY;

    // Shorthand
    private Transform vortexCenter;

    void Start()
    {
        if (vortexController == null)
            vortexController = VortexController.Instance;

        if (vortexController == null)
        {
            var go = GameObject.FindGameObjectWithTag("VortexCenter");
            if (go != null)
                vortexController = go.GetComponent<VortexController>();
        }

        if (vortexController == null)
        {
            Debug.LogWarning("VortexAffectedObject: no VortexController found!", this);
            enabled = false;
            return;
        }

        vortexCenter = vortexController.transform;

        Vector3 delta = transform.position - vortexCenter.position;
        currentAngle = Mathf.Atan2(delta.z, delta.x);
        currentRadius = new Vector2(delta.x, delta.z).magnitude;
        lastPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (vortexController == null || vortexCenter == null) return;

        float dt = Time.deltaTime;
        Vector3 center = vortexCenter.position;

        if (isSinking)
        {
            UpdateSinking(dt, center);
            return;
        }

        float vRadius = vortexController.radius;
        float normDist = Mathf.Clamp01(currentRadius / Mathf.Max(vRadius, 0.01f));

        // ---- INWARD PULL ----
        float pullMult = 1.0f + (1.0f - normDist) * pullAcceleration;
        currentRadius -= pullSpeed * pullMult * dt;
        currentRadius = Mathf.Max(currentRadius, 0f);

        // ---- ORBIT ----
        float orbitMult = 1.0f + (1.0f - normDist) * orbitAcceleration;
        float wobbleMult = 1.0f + Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount * 0.1f;
        currentAngle += orbitSpeed * orbitMult * wobbleMult * Mathf.Deg2Rad * dt;

        // ---- XZ POSITION ----
        float x = center.x + Mathf.Cos(currentAngle) * currentRadius;
        float z = center.z + Mathf.Sin(currentAngle) * currentRadius;

        // ---- SURFACE Y ----
        float y = center.y;
        if (followSurface)
            y = ComputeVortexSurfaceY(center, x, z);
        y += surfaceOffset;

        Vector3 newPos = new Vector3(x, y, z);

        // ---- ROTATION ----
        UpdateRotation(newPos, center, normDist, dt);

        lastPosition = transform.position;
        transform.position = newPos;

        // ---- CORE CHECK ----
        normDist = Mathf.Clamp01(currentRadius / Mathf.Max(vRadius, 0.01f));
        if (normDist <= coreThreshold)
            OnReachedCore();
    }

    /// <summary>
    /// Computes the Y on the vortex surface. Reads from VortexController fields directly.
    /// </summary>
    float ComputeVortexSurfaceY(Vector3 center, float worldX, float worldZ)
    {
        float dx = worldX - center.x;
        float dz = worldZ - center.z;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);
        float vRadius = vortexController.radius;
        float normDist = Mathf.Clamp01(dist / Mathf.Max(vRadius, 0.01f));

        float funnelT = 1.0f - Mathf.Clamp01(
            (normDist - vortexController.innerRadius) /
            (1.0f - vortexController.innerRadius)
        );
        float funnel = Mathf.Pow(funnelT, vortexController.funnelPower);
        float depression = -funnel * vortexController.depth;

        return center.y + depression;
    }

    void UpdateRotation(Vector3 newPos, Vector3 center, float normDist, float dt)
    {
        if (faceMovementDirection)
        {
            float tangentX = -Mathf.Sin(currentAngle);
            float tangentZ = Mathf.Cos(currentAngle);

            float inwardBlend = Mathf.Lerp(0f, 0.3f, 1f - normDist);
            Vector3 toCenter = center - newPos;
            toCenter.y = 0;
            Vector3 inward = toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector3.zero;

            Vector3 facingDir = new Vector3(tangentX, 0f, tangentZ).normalized;
            facingDir = Vector3.Lerp(facingDir, inward, inwardBlend).normalized;

            if (facingDir.sqrMagnitude > 0.001f)
                targetRotation = Quaternion.LookRotation(facingDir, Vector3.up);
        }

        if (tiltTowardCenter)
        {
            Vector3 toCenter = center - newPos;
            toCenter.y = 0;
            if (toCenter.sqrMagnitude > 0.01f)
            {
                float tiltAngle = maxTilt * (1.0f - normDist);
                Vector3 tiltAxis = Vector3.Cross(Vector3.up, toCenter.normalized);
                targetRotation = Quaternion.AngleAxis(tiltAngle, tiltAxis) * targetRotation;
            }
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, dt * rotationSmoothing);
    }

    void OnReachedCore()
    {
        onReachedCore?.Invoke();

        switch (coreAction)
        {
            case CoreAction.Nothing: break;
            case CoreAction.Destroy:
                if (sinkDuration > 0) StartSinking();
                else Destroy(gameObject);
                break;
            case CoreAction.Disable:
                if (sinkDuration > 0) StartSinking();
                else gameObject.SetActive(false);
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

        float vRadius = vortexController.radius;
        float normDist = Mathf.Clamp01(currentRadius / Mathf.Max(vRadius, 0.01f));
        float orbitMult = 1.0f + (1.0f - normDist) * orbitAcceleration;
        currentAngle += orbitSpeed * orbitMult * 2f * Mathf.Deg2Rad * dt;

        float x = center.x + Mathf.Cos(currentAngle) * currentRadius;
        float z = center.z + Mathf.Sin(currentAngle) * currentRadius;

        float sinkDepth = Mathf.Lerp(0f, vortexController.depth * 0.5f, t * t);
        transform.position = new Vector3(x, sinkStartY - sinkDepth, z);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, t);

        if (t >= 1f)
        {
            switch (coreAction)
            {
                case CoreAction.Destroy: Destroy(gameObject); break;
                case CoreAction.Disable: gameObject.SetActive(false); break;
                default: isSinking = false; break;
            }
        }
    }

    void Respawn()
    {
        float vRadius = vortexController.radius;
        currentAngle = Random.Range(0f, Mathf.PI * 2f);
        currentRadius = vRadius * respawnDistance;
        isSinking = false;
        sinkTimer = 0f;
        transform.localScale = Vector3.one;

        Vector3 center = vortexCenter.position;
        float x = center.x + Mathf.Cos(currentAngle) * currentRadius;
        float z = center.z + Mathf.Sin(currentAngle) * currentRadius;
        transform.position = new Vector3(x, ComputeVortexSurfaceY(center, x, z), z);
    }

    // =============================================
    // PUBLIC API
    // =============================================

    public void SetNormalizedDistance(float normDist)
    {
        float vRadius = vortexController != null ? vortexController.radius : 50f;
        currentRadius = vRadius * Mathf.Clamp01(normDist);
    }

    public void PushOutward(float amount)
    {
        float vRadius = vortexController != null ? vortexController.radius : 50f;
        currentRadius = Mathf.Min(currentRadius + amount, vRadius);
    }

    public float GetNormalizedDistance()
    {
        float vRadius = vortexController != null ? vortexController.radius : 50f;
        return Mathf.Clamp01(currentRadius / Mathf.Max(vRadius, 0.01f));
    }

    // =============================================
    // GIZMOS
    // =============================================

    void OnDrawGizmosSelected()
    {
        Transform vc = vortexController != null ? vortexController.transform : null;
        if (vc == null) return;

        Gizmos.color = Color.yellow;
        DrawCircleGizmo(vc.position, currentRadius, 48);

        Gizmos.color = Color.red;
        DrawCircleGizmo(vc.position, vortexController.radius * coreThreshold, 24);

        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawLine(transform.position, vc.position);
    }

    void DrawCircleGizmo(Vector3 center, float r, int segments)
    {
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