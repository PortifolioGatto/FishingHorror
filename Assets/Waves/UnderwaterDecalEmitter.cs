using UnityEngine;

/// <summary>
/// Attach to any GameObject to project a texture onto the ocean surface.
///
/// Transform controls:
///   Position XZ = where the decal appears on the water
///   Position Y  = depth below water (auto-adjusts darken/tint)
///   Rotation    = full 3-axis rotation of the decal projection
///   Scale XZ    = size of the decal in world units
///
/// The decal is drawn BY the ocean shader — waves never cover it.
///
/// Requires UnderwaterDecalManager in the scene.
/// </summary>
[ExecuteInEditMode]
public class UnderwaterDecalEmitter : MonoBehaviour
{
    [Header("Texture")]
    [Tooltip("The texture to project onto the water surface")]
    public Texture2D texture;

    [Header("Appearance")]
    [Tooltip("Tint color applied to the texture")]
    public Color tintColor = Color.white;

    [Tooltip("Overall opacity (0 = invisible, 1 = fully visible)")]
    [Range(0f, 1f)]
    public float opacity = 0.8f;

    [Tooltip("How much the water tints the decal")]
    [Range(0f, 1f)]
    public float waterTintStrength = 0.3f;

    [Header("Depth Effect")]
    [Tooltip("Extra darken on top of auto depth darken")]
    [Range(0f, 1f)]
    public float extraDarken = 0f;

    [Tooltip("Extra desaturation on top of auto depth desat")]
    [Range(0f, 1f)]
    public float extraDesaturate = 0f;

    [Tooltip("How much Y position affects darken/desat (0 = manual only)")]
    [Range(0f, 1f)]
    public float autoDepthEffect = 0.5f;

    [Header("Wave Distortion")]
    [Tooltip("How much the image distorts with waves")]
    [Range(0f, 0.1f)]
    public float waveDistortion = 0.015f;

    [Tooltip("Speed of distortion animation")]
    [Range(0f, 3f)]
    public float distortionSpeed = 1f;

    [Header("Edge")]
    [Tooltip("Soft fade at the edges")]
    [Range(0f, 0.5f)]
    public float edgeFade = 0.1f;

    private bool registered;

    void OnEnable()
    {
        TryRegister();
    }

    void Update()
    {
        if (!registered)
            TryRegister();
    }

    void TryRegister()
    {
        if (registered) return;
        if (UnderwaterDecalManager.Instance != null)
        {
            UnderwaterDecalManager.Instance.Register(this);
            registered = true;
        }
    }

    void OnDisable()
    {
        if (UnderwaterDecalManager.Instance != null)
            UnderwaterDecalManager.Instance.Unregister(this);
        registered = false;
    }

    /// <summary>
    /// Compute the depth below a given water surface Y.
    /// Used by the manager to auto-calculate darken/desat.
    /// </summary>
    public float GetDepthBelow(float waterSurfaceY)
    {
        return Mathf.Max(waterSurfaceY - transform.position.y, 0f);
    }

    // =========================================
    // PUBLIC API
    // =========================================

    public void SetTexture(Texture2D tex) { texture = tex; }
    public void SetOpacity(float v) { opacity = Mathf.Clamp01(v); }

    // =========================================
    // GIZMOS
    // =========================================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0.01f, 1f));
        Gizmos.matrix = Matrix4x4.identity;

        // Draw projection line to water surface (y=0 assumed)
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Vector3 p = transform.position;
        Gizmos.DrawLine(p, new Vector3(p.x, 0, p.z));
    }
}
