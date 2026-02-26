using UnityEngine;

/// <summary>
/// Manages underwater creature silhouettes visible as dark shadows on the water surface.
/// No mesh needed — the ocean shader darkens the water where creatures are.
/// 
/// Usage:
/// 1. Attach this to any GameObject.
/// 2. Add OceanCreature components to empty GameObjects and move them around
///    (via animation, script, waypoints, whatever you prefer).
/// 3. This manager collects their data each frame and sends it to the shader.
/// </summary>
[ExecuteInEditMode]
public class OceanCreatureManager : MonoBehaviour
{
    public static OceanCreatureManager Instance { get; private set; }

    [Tooltip("Maximum creatures rendered simultaneously (shader limit = 8)")]
    [Range(1, 8)]
    public int maxCreatures = 8;

    private static readonly int ID_CreatureCount = Shader.PropertyToID("_CreatureCount");
    private static readonly int ID_CreaturePositions = Shader.PropertyToID("_CreaturePositions");
    private static readonly int ID_CreatureParams = Shader.PropertyToID("_CreatureParams");

    private Vector4[] positions = new Vector4[8];
    private Vector4[] parameters = new Vector4[8];

    void OnEnable()
    {
        Instance = this;
    }

    void Update()
    {
        var creatures = OceanCreature.ActiveCreatures;
        int count = Mathf.Min(creatures.Count, maxCreatures);

        // Clear arrays
        for (int i = 0; i < 8; i++)
        {
            positions[i] = Vector4.zero;
            parameters[i] = Vector4.zero;
        }

        // Fill from active creatures
        for (int i = 0; i < count; i++)
        {
            var c = creatures[i];
            if (c == null) continue;

            Vector3 pos = c.transform.position;
            positions[i] = new Vector4(pos.x, pos.y, pos.z, c.shadowRadius);
            parameters[i] = new Vector4(c.depthBelowSurface, c.elongation, c.opacity, 0f);
        }

        // Send to shader (global — any shader can read these)
        Shader.SetGlobalFloat(ID_CreatureCount, count);
        Shader.SetGlobalVectorArray(ID_CreaturePositions, positions);
        Shader.SetGlobalVectorArray(ID_CreatureParams, parameters);
    }
}
