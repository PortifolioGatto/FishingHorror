using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// Attach to a GameObject to make it appear as an underwater shadow/silhouette
/// on the ocean surface. Move the GameObject around (via script, animation,
/// NavMesh, whatever) and the shadow follows.
/// 
/// The Y position doesn't matter for the shadow XZ position, but depthBelowSurface
/// controls how "deep" the creature appears (deeper = softer, fainter shadow).
/// </summary>
public class OceanCreature : MonoBehaviour
{
    public static List<OceanCreature> ActiveCreatures = new List<OceanCreature>();

    [Header("Shadow Shape")]
    [Tooltip("Base radius of the shadow on the water surface")]
    [Range(0.5f, 50f)]
    public float shadowRadius = 5f;

    [Tooltip("How deep below the water surface (affects softness and faintness)")]
    [Range(0f, 30f)]
    public float depthBelowSurface = 3f;

    [Tooltip("XZ stretch of the shadow (1 = circle, 2 = twice as wide)")]
    [Range(0.5f, 5f)]
    public float elongation = 1.5f;

    [Tooltip("Shadow opacity (0 = invisible, 1 = full intensity)")]
    [Range(0f, 1f)]
    public float opacity = 1f;

    [Header("Auto Movement (optional)")]
    [Tooltip("Enable simple circular patrol movement")]
    public bool autoMove = false;

    [Tooltip("Center point of the patrol circle")]
    public Vector3 patrolCenter = Vector3.zero;

    [Tooltip("Radius of the patrol circle")]
    public float patrolRadius = 20f;

    [Tooltip("Speed of patrol movement")]
    public float patrolSpeed = 0.3f;

    [Tooltip("Random offset so multiple creatures aren't synced")]
    public float patrolPhaseOffset = 0f;

    [Header("Surfacing (optional)")]
    [Tooltip("Enable the creature to occasionally rise closer to the surface")]
    public bool enableSurfacing = false;

    [Tooltip("How close to surface when surfacing (0 = at surface)")]
    [Range(0f, 5f)]
    public float surfacingMinDepth = 0.5f;

    [Tooltip("Seconds between surfacing events")]
    public float surfacingInterval = 15f;

    [Tooltip("How long each surfacing lasts")]
    public float surfacingDuration = 4f;

    private float baseDepth;
    private float surfaceTimer;

    void OnEnable()
    {
        if (!ActiveCreatures.Contains(this))
            ActiveCreatures.Add(this);
        baseDepth = depthBelowSurface;
    }

    void OnDisable()
    {
        ActiveCreatures.Remove(this);
    }

    void Update()
    {
        // Auto patrol movement
        if (autoMove)
        {
            float t = Time.time * patrolSpeed + patrolPhaseOffset;
            // Figure-8 pattern for more organic movement
            float x = patrolCenter.x + Mathf.Sin(t) * patrolRadius;
            float z = patrolCenter.z + Mathf.Sin(t * 0.7f) * Mathf.Cos(t * 0.4f) * patrolRadius;
            transform.position = new Vector3(x, transform.position.y, z);
        }

        // Surfacing behavior
        if (enableSurfacing)
        {
            surfaceTimer += Time.deltaTime;
            float cycleTime = surfacingInterval + surfacingDuration;
            float cyclePos = Mathf.Repeat(surfaceTimer, cycleTime);

            if (cyclePos > surfacingInterval)
            {
                // Currently surfacing
                float surfaceProgress = (cyclePos - surfacingInterval) / surfacingDuration;
                // Smooth up and back down
                float surfaceCurve = Mathf.Sin(surfaceProgress * Mathf.PI);
                depthBelowSurface = Mathf.Lerp(baseDepth, surfacingMinDepth, surfaceCurve);
            }
            else
            {
                depthBelowSurface = baseDepth;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the shadow area
        Gizmos.color = new Color(0, 0, 0, 0.3f);
        float spread = 1f + depthBelowSurface * 0.3f;
        float r = shadowRadius * spread * elongation;
        Gizmos.DrawWireSphere(transform.position, r);

        // Patrol path
        if (autoMove)
        {
            Gizmos.color = Color.cyan;
            int segments = 64;
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments * Mathf.PI * 2f;
                float x = patrolCenter.x + Mathf.Sin(t) * patrolRadius;
                float z = patrolCenter.z + Mathf.Sin(t * 0.7f) * Mathf.Cos(t * 0.4f) * patrolRadius;
                Vector3 p = new Vector3(x, transform.position.y, z);
                if (i > 0) Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
