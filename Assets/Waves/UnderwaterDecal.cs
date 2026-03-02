using UnityEngine;

/// <summary>
/// Controller for underwater decals (textures visible through the water surface).
/// Attach to a Quad or Plane positioned at/slightly below the water surface.
///
/// Setup:
/// 1. Create a Quad (GameObject > 3D Object > Quad)
/// 2. Rotate it to face up (X=90, Y=0, Z=0)
/// 3. Position at the water surface Y (or slightly below)
/// 4. Add this component — it auto-creates the material
/// 5. Assign your texture (sprite, logo, blood splat, seaweed, rune, etc.)
///
/// The decal renders AFTER the ocean (Transparent+1) so it appears
/// on top of the water with configurable opacity and water tint.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class UnderwaterDecal : MonoBehaviour
{
    [Header("Texture")]
    [Tooltip("The texture to display underwater")]
    public Texture2D texture;

    [Tooltip("Tint color applied to the texture")]
    public Color tintColor = Color.white;

    [Header("Visibility")]
    [Tooltip("Overall opacity (0 = invisible, 1 = fully visible)")]
    [Range(0f, 1f)]
    public float opacity = 0.8f;

    [Tooltip("How much the water tints the decal")]
    [Range(0f, 1f)]
    public float waterTintStrength = 0.4f;

    [Tooltip("Water tint color (blended over the texture)")]
    public Color waterTintColor = new Color(0f, 0.3f, 0.5f, 1f);

    [Header("Depth Effect")]
    [Tooltip("Darken the decal to simulate depth")]
    [Range(0f, 1f)]
    public float depthDarken = 0.3f;

    [Tooltip("Desaturate the decal (water absorbs color at depth)")]
    [Range(0f, 1f)]
    public float depthDesaturate = 0.2f;

    [Header("Wave Distortion")]
    [Tooltip("How much the water distorts the image")]
    [Range(0f, 0.1f)]
    public float waveDistortion = 0.02f;

    [Tooltip("Speed of the distortion animation")]
    [Range(0f, 3f)]
    public float distortionSpeed = 1f;

    [Tooltip("Scale of the distortion pattern")]
    [Range(0.1f, 10f)]
    public float distortionScale = 2f;

    [Header("Edge")]
    [Tooltip("Soften the edges of the decal")]
    [Range(0f, 0.5f)]
    public float edgeFade = 0.1f;

    [Header("Positioning")]
    [Tooltip("Offset above the water surface (positive = above). Must be > 0 to be visible.")]
    public float heightOffset = 0.15f;

    // Internal
    private Material mat;
    private MeshRenderer meshRenderer;
    private static Shader decalShader;

    static readonly int ID_MainTex = Shader.PropertyToID("_MainTex");
    static readonly int ID_Color = Shader.PropertyToID("_Color");
    static readonly int ID_Opacity = Shader.PropertyToID("_Opacity");
    static readonly int ID_WaterTint = Shader.PropertyToID("_WaterTint");
    static readonly int ID_WaterTintStrength = Shader.PropertyToID("_WaterTintStrength");
    static readonly int ID_DepthDarken = Shader.PropertyToID("_DepthDarken");
    static readonly int ID_DepthDesaturate = Shader.PropertyToID("_DepthDesaturate");
    static readonly int ID_WaveDistortion = Shader.PropertyToID("_WaveDistortion");
    static readonly int ID_WaveDistortionSpeed = Shader.PropertyToID("_WaveDistortionSpeed");
    static readonly int ID_WaveDistortionScale = Shader.PropertyToID("_WaveDistortionScale");
    static readonly int ID_EdgeFade = Shader.PropertyToID("_EdgeFade");

    void OnEnable()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        EnsureMaterial();
    }

    void EnsureMaterial()
    {
        if (decalShader == null)
            decalShader = Shader.Find("Custom/UnderwaterDecal");

        if (decalShader == null)
        {
            Debug.LogWarning("UnderwaterDecal: shader 'Custom/UnderwaterDecal' not found!", this);
            return;
        }

        // Check if current material already uses our shader
        if (meshRenderer.sharedMaterial != null &&
            meshRenderer.sharedMaterial.shader == decalShader)
        {
            mat = Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;
            return;
        }

        // Create new material
        mat = new Material(decalShader);
        mat.name = "UnderwaterDecal_" + gameObject.name;
        meshRenderer.material = mat;
    }

    void Update()
    {
        if (mat == null) return;

        // Apply all properties
        if (texture != null)
            mat.SetTexture(ID_MainTex, texture);

        mat.SetColor(ID_Color, tintColor);
        mat.SetFloat(ID_Opacity, opacity);
        mat.SetColor(ID_WaterTint, waterTintColor);
        mat.SetFloat(ID_WaterTintStrength, waterTintStrength);
        mat.SetFloat(ID_DepthDarken, depthDarken);
        mat.SetFloat(ID_DepthDesaturate, depthDesaturate);
        mat.SetFloat(ID_WaveDistortion, waveDistortion);
        mat.SetFloat(ID_WaveDistortionSpeed, distortionSpeed);
        mat.SetFloat(ID_WaveDistortionScale, distortionScale);
        mat.SetFloat(ID_EdgeFade, edgeFade);
    }

    // =========================================
    // PUBLIC API
    // =========================================

    /// <summary>
    /// Set texture at runtime.
    /// </summary>
    public void SetTexture(Texture2D tex)
    {
        texture = tex;
    }

    /// <summary>
    /// Fade in over duration seconds. Call once, it uses coroutine.
    /// </summary>
    public void FadeIn(float duration = 1f)
    {
        StartCoroutine(FadeCoroutine(0f, opacity > 0.01f ? opacity : 0.8f, duration));
    }

    /// <summary>
    /// Fade out over duration seconds.
    /// </summary>
    public void FadeOut(float duration = 1f)
    {
        StartCoroutine(FadeCoroutine(opacity, 0f, duration));
    }

    private System.Collections.IEnumerator FadeCoroutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            opacity = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        opacity = to;
    }

    /// <summary>
    /// Set opacity immediately.
    /// </summary>
    public void SetOpacity(float value)
    {
        opacity = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Position the decal at a world XZ point on the water surface.
    /// </summary>
    public void SetWorldPosition(float x, float z, float waterSurfaceY)
    {
        transform.position = new Vector3(x, waterSurfaceY + heightOffset, z);
    }

    /// <summary>
    /// Set the size of the decal in world units.
    /// </summary>
    public void SetSize(float width, float height)
    {
        transform.localScale = new Vector3(width, height, 1f);
    }

    /// <summary>
    /// Set size uniformly.
    /// </summary>
    public void SetSize(float size)
    {
        SetSize(size, size);
    }
}
