using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Emits a foam wake trail behind any moving object.
/// Attach to boats, sharks, whales, or anything that moves on water.
/// Requires a WakeTrailManager in the scene to collect and send data to shader.
///
/// Each emitter produces 1 or 2 trails (dual = port + starboard for boats).
/// Points are stored locally and collected by the manager each frame.
/// </summary>
public class WakeTrailEmitter : MonoBehaviour
{
    [Header("Trail Settings")]
    [Tooltip("How wide the foam trail is (meters)")]
    public float trailWidth = 3f;

    [Tooltip("Max foam intensity")]
    [Range(0f, 2f)]
    public float foamIntensity = 1f;

    [Tooltip("How long the trail lasts before fading (seconds)")]
    public float fadeTime = 8f;

    [Tooltip("How often to emit a new trail point (seconds)")]
    public float emitInterval = 0.15f;

    [Tooltip("Minimum speed to start leaving a trail (units/sec)")]
    public float minSpeed = 0.5f;

    [Tooltip("Width multiplier based on speed")]
    public float speedWidthScale = 0.3f;

    [Header("Emit Points")]
    [Tooltip("Local offset from the pivot to the trail origin (e.g. stern for boats, tail for creatures)")]
    public Vector3 emitOffset = new Vector3(0f, 0f, -2f);

    [Tooltip("Emit two parallel trails (port and starboard) — use for boats")]
    public bool dualTrail = false;

    [Tooltip("Lateral offset for dual trail (half width)")]
    public float dualTrailSpread = 1.5f;

    [Header("Points Budget")]
    [Tooltip("Max points this emitter can store per trail line")]
    public int maxPointsPerTrail = 16;

    public struct TrailPoint
    {
        public Vector3 position;
        public float time;
        public float width;
        public float intensity;
        public float velocity;
    }

    // Public so the manager can read them
    public readonly List<TrailPoint> trail1 = new List<TrailPoint>();
    public readonly List<TrailPoint> trail2 = new List<TrailPoint>(); // only used if dualTrail

    private float lastEmitTime;
    private Vector3 lastPosition;
    private float currentSpeed;

    void Start()
    {
        lastPosition = transform.position;
        lastEmitTime = -emitInterval;
    }

    void OnEnable()
    {
        if (WakeTrailManager.Instance != null)
            WakeTrailManager.Instance.Register(this);
    }

    void OnDisable()
    {
        trail1.Clear();
        trail2.Clear();
        if (WakeTrailManager.Instance != null)
            WakeTrailManager.Instance.Unregister(this);
    }

    void Update()
    {
        Vector3 pos = transform.position;
        float dt = Time.deltaTime;

        if (dt > 0.0001f)
        {
            Vector3 vel = (pos - lastPosition) / dt;
            vel.y = 0;
            currentSpeed = vel.magnitude;
        }
        lastPosition = pos;

        float now = Time.time;

        // Remove expired
        trail1.RemoveAll(p => now - p.time > fadeTime);
        trail2.RemoveAll(p => now - p.time > fadeTime);

        // Emit
        if (currentSpeed > minSpeed && now - lastEmitTime >= emitInterval)
        {
            float speedWidth = trailWidth + currentSpeed * speedWidthScale;
            float speedIntensity = Mathf.Clamp01(foamIntensity * Mathf.Clamp01(currentSpeed / (minSpeed * 3f)));

            TrailPoint MakePoint(Vector3 worldPos)
            {
                return new TrailPoint
                {
                    position  = worldPos,
                    time      = now,
                    width     = speedWidth,
                    intensity = speedIntensity,
                    velocity  = currentSpeed
                };
            }

            if (dualTrail)
            {
                Vector3 portWorld = transform.TransformPoint(emitOffset + Vector3.left * dualTrailSpread);
                Vector3 starWorld = transform.TransformPoint(emitOffset + Vector3.right * dualTrailSpread);
                trail1.Add(MakePoint(portWorld));
                trail2.Add(MakePoint(starWorld));
            }
            else
            {
                Vector3 emitWorld = transform.TransformPoint(emitOffset);
                trail1.Add(MakePoint(emitWorld));
            }

            lastEmitTime = now;
        }

        // Trim
        while (trail1.Count > maxPointsPerTrail) trail1.RemoveAt(0);
        while (trail2.Count > maxPointsPerTrail) trail2.RemoveAt(0);
    }

    /// <summary>
    /// Returns the number of trail lines this emitter produces (1 or 2).
    /// </summary>
    public int TrailLineCount => dualTrail ? 2 : 1;

    /// <summary>
    /// Returns the total number of points across all trail lines.
    /// </summary>
    public int TotalPointCount => trail1.Count + trail2.Count;

    void OnDrawGizmosSelected()
    {
        DrawTrailGizmo(trail1, Color.cyan);
        if (dualTrail) DrawTrailGizmo(trail2, Color.magenta);

        Gizmos.color = Color.yellow;
        if (dualTrail)
        {
            Gizmos.DrawWireSphere(transform.TransformPoint(emitOffset + Vector3.left * dualTrailSpread), 0.2f);
            Gizmos.DrawWireSphere(transform.TransformPoint(emitOffset + Vector3.right * dualTrailSpread), 0.2f);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.TransformPoint(emitOffset), 0.2f);
        }
    }

    static void DrawTrailGizmo(List<TrailPoint> trail, Color color)
    {
        if (trail == null || trail.Count < 2) return;
        Gizmos.color = color;
        for (int i = 0; i < trail.Count - 1; i++)
            Gizmos.DrawLine(trail[i].position, trail[i + 1].position);
    }
}
