using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Collects all active UnderwaterLight instances and sends their data
/// to the ocean shader every frame via global shader arrays.
///
/// Shader arrays:
///   _UnderwaterLightPositions[8] : xyz = world pos, w = outerRadius
///   _UnderwaterLightColors[8]    : rgb = color, a = intensity
///   _UnderwaterLightParams[8]    : x = innerRadius, y = concentration, z/w = reserved
///
/// Max 8 simultaneous underwater lights.
/// </summary>
[ExecuteInEditMode]
public class UnderwaterLightManager : MonoBehaviour
{
    public static UnderwaterLightManager Instance { get; private set; }

    private static readonly int ID_Count     = Shader.PropertyToID("_UnderwaterLightCount");
    private static readonly int ID_Positions = Shader.PropertyToID("_UnderwaterLightPositions");
    private static readonly int ID_Colors    = Shader.PropertyToID("_UnderwaterLightColors");
    private static readonly int ID_Params    = Shader.PropertyToID("_UnderwaterLightParams");

    private const int MAX_LIGHTS = 8;

    private Vector4[] positions = new Vector4[MAX_LIGHTS];
    private Vector4[] colors    = new Vector4[MAX_LIGHTS];
    private Vector4[] paramArr  = new Vector4[MAX_LIGHTS];

    private readonly List<UnderwaterLight> lights = new List<UnderwaterLight>();
    private float refreshTimer;

    void OnEnable()
    {
        Instance = this;
        RefreshLightList();
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
        Shader.SetGlobalFloat(ID_Count, 0);
    }

    public void RefreshLightList()
    {
        lights.Clear();
        var found = FindObjectsByType<UnderwaterLight>(FindObjectsSortMode.None);
        lights.AddRange(found);
        refreshTimer = 2f;
    }

    public void Register(UnderwaterLight light)
    {
        if (!lights.Contains(light))
            lights.Add(light);
    }

    public void Unregister(UnderwaterLight light)
    {
        lights.Remove(light);
    }

    [Header("Occlusion")]
    [Tooltip("Y level of the water surface (for shadow raycasts)")]
    public float waterSurfaceY = 0f;

    void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
            RefreshLightList();

        lights.RemoveAll(l => l == null || !l.enabled || !l.gameObject.activeInHierarchy);

        int count = Mathf.Min(lights.Count, MAX_LIGHTS);
        float dt = Time.deltaTime;

        for (int i = 0; i < MAX_LIGHTS; i++)
        {
            if (i < count)
            {
                var light = lights[i];

                if (light == null) return;

                Vector3 pos = light.transform.position;

                // Compute occlusion (raycasts from light toward surface)
                float targetOcclusion = light.ComputeOcclusion(waterSurfaceY);
                // Smooth transition so shadows don't pop
                light.currentOcclusion = Mathf.Lerp(
                    light.currentOcclusion,
                    targetOcclusion,
                    1f - Mathf.Exp(-light.shadowSmoothing * dt)
                );

                positions[i] = new Vector4(pos.x, pos.y, pos.z, light.outerRadius);
                colors[i] = new Vector4(
                    light.lightColor.r,
                    light.lightColor.g,
                    light.lightColor.b,
                    light.GetEffectiveIntensity() // already includes occlusion
                );
                paramArr[i] = new Vector4(
                    light.innerRadius,
                    light.concentration,
                    0f,
                    0f
                );
            }
            else
            {
                positions[i] = Vector4.zero;
                colors[i] = Vector4.zero;
                paramArr[i] = Vector4.zero;
            }
        }

        Shader.SetGlobalFloat(ID_Count, count);
        Shader.SetGlobalVectorArray(ID_Positions, positions);
        Shader.SetGlobalVectorArray(ID_Colors, colors);
        Shader.SetGlobalVectorArray(ID_Params, paramArr);
    }
}
