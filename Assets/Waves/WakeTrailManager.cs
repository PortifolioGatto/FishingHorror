using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Collects wake trail points from all active WakeTrailEmitter instances
/// and sends them to the ocean shader as packed global arrays.
///
/// Shader arrays:
///   _WakeTrailPoints[128]  : xyz = world pos, w = startTime
///   _WakeTrailParams[128]  : x = width, y = foamIntensity, z = velocity
///   _WakeTrailRanges[16]   : x = startIndex, y = pointCount per trail line
///   _WakeTrailRangeCount   : number of trail lines
///   _WakeTrailFadeTime     : max fade time across all emitters
///
/// Budget: 128 points total, 16 trail lines max.
/// Points are allocated first-come with priority to emitters with more points.
/// </summary>
[ExecuteInEditMode]
public class WakeTrailManager : MonoBehaviour
{
    public static WakeTrailManager Instance { get; private set; }

    private const int MAX_POINTS = 128;
    private const int MAX_RANGES = 16;

    private readonly List<WakeTrailEmitter> emitters = new List<WakeTrailEmitter>();

    private Vector4[] shaderPoints = new Vector4[MAX_POINTS];
    private Vector4[] shaderParams = new Vector4[MAX_POINTS];
    private Vector4[] shaderRanges = new Vector4[MAX_RANGES];

    private static readonly int ID_TotalPoints = Shader.PropertyToID("_WakeTrailTotalPoints");
    private static readonly int ID_Points      = Shader.PropertyToID("_WakeTrailPoints");
    private static readonly int ID_Params      = Shader.PropertyToID("_WakeTrailParams");
    private static readonly int ID_RangeCount  = Shader.PropertyToID("_WakeTrailRangeCount");
    private static readonly int ID_Ranges      = Shader.PropertyToID("_WakeTrailRanges");
    private static readonly int ID_FadeTime    = Shader.PropertyToID("_WakeTrailFadeTime");

    private float refreshTimer;

    void OnEnable()
    {
        Instance = this;
        RefreshEmitters();
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
        Shader.SetGlobalFloat(ID_RangeCount, 0);
    }

    public void Register(WakeTrailEmitter emitter)
    {
        if (!emitters.Contains(emitter))
            emitters.Add(emitter);
    }

    public void Unregister(WakeTrailEmitter emitter)
    {
        emitters.Remove(emitter);
    }

    public void RefreshEmitters()
    {
        emitters.Clear();
        var found = FindObjectsByType<WakeTrailEmitter>(FindObjectsSortMode.None);
        emitters.AddRange(found);
        refreshTimer = 3f;
    }

    void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
            RefreshEmitters();

        emitters.RemoveAll(e => e == null || !e.enabled || !e.gameObject.activeInHierarchy);

        // Clear arrays
        for (int i = 0; i < MAX_POINTS; i++)
        {
            shaderPoints[i] = Vector4.zero;
            shaderParams[i] = Vector4.zero;
        }
        for (int i = 0; i < MAX_RANGES; i++)
            shaderRanges[i] = Vector4.zero;

        int pointCursor = 0;
        int rangeCursor = 0;
        float maxFadeTime = 1f;

        foreach (var emitter in emitters)
        {
            if (rangeCursor >= MAX_RANGES) break;
            if (emitter.TotalPointCount < 2) continue;

            maxFadeTime = Mathf.Max(maxFadeTime, emitter.fadeTime);

            // Pack trail1
            if (emitter.trail1.Count >= 2)
            {
                int available = Mathf.Min(emitter.trail1.Count, MAX_POINTS - pointCursor);
                if (available >= 2 && rangeCursor < MAX_RANGES)
                {
                    shaderRanges[rangeCursor] = new Vector4(pointCursor, available, 0, 0);
                    rangeCursor++;

                    for (int i = 0; i < available; i++)
                    {
                        var p = emitter.trail1[i];
                        shaderPoints[pointCursor] = new Vector4(p.position.x, p.position.y, p.position.z, p.time);
                        shaderParams[pointCursor] = new Vector4(p.width, p.intensity, p.velocity, 0f);
                        pointCursor++;
                    }
                }
            }

            // Pack trail2 (dual trail)
            if (emitter.dualTrail && emitter.trail2.Count >= 2)
            {
                int available = Mathf.Min(emitter.trail2.Count, MAX_POINTS - pointCursor);
                if (available >= 2 && rangeCursor < MAX_RANGES)
                {
                    shaderRanges[rangeCursor] = new Vector4(pointCursor, available, 0, 0);
                    rangeCursor++;

                    for (int i = 0; i < available; i++)
                    {
                        var p = emitter.trail2[i];
                        shaderPoints[pointCursor] = new Vector4(p.position.x, p.position.y, p.position.z, p.time);
                        shaderParams[pointCursor] = new Vector4(p.width, p.intensity, p.velocity, 0f);
                        pointCursor++;
                    }
                }
            }
        }

        Shader.SetGlobalFloat(ID_TotalPoints, pointCursor);
        Shader.SetGlobalVectorArray(ID_Points, shaderPoints);
        Shader.SetGlobalVectorArray(ID_Params, shaderParams);
        Shader.SetGlobalFloat(ID_RangeCount, rangeCursor);
        Shader.SetGlobalVectorArray(ID_Ranges, shaderRanges);
        Shader.SetGlobalFloat(ID_FadeTime, maxFadeTime);
    }
}
