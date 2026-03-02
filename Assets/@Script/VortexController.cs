using UnityEngine;

/// <summary>
/// Central controller for the OceanVortex shader.
/// Attach to the vortex GameObject (the one with the MeshRenderer).
/// Exposes all key vortex parameters in the Inspector and via public API.
/// Changes are applied to the material every frame so you can animate
/// values from scripts, timeline, or tweening libraries.
///
/// Usage from script:
///   vortexController.depth = 30f;
///   vortexController.spinSpeed = 2f;
///   vortexController.SetIntensity(0.5f); // scales depth + pull + spin together
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class VortexController : MonoBehaviour
{
    /// <summary>
    /// Singleton instance. If multiple vortices exist, this is the last enabled one.
    /// For multi-vortex scenarios, reference the specific VortexController directly.
    /// </summary>
    public static VortexController Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    // =========================================
    // SHAPE
    // =========================================
    [Header("Vortex Shape")]
    [Tooltip("Outer edge radius in world units")]
    public float radius = 50f;

    [Tooltip("How deep the funnel goes")]
    public float depth = 20f;

    [Tooltip("Size of the inner hole (normalized 0-1)")]
    [Range(0.01f, 0.5f)]
    public float innerRadius = 0.08f;

    [Tooltip("Curve power — higher = steeper walls, flatter center")]
    [Range(0.5f, 6f)]
    public float funnelPower = 2f;

    // =========================================
    // ROTATION
    // =========================================
    [Header("Vortex Rotation")]
    [Tooltip("How fast the vortex spins")]
    public float spinSpeed = 1f;

    [Tooltip("How tight the spiral arms are")]
    [Range(0.5f, 20f)]
    public float spiralTightness = 5f;

    [Tooltip("Height of the spiral wave ridges")]
    [Range(0f, 5f)]
    public float spiralHeight = 1.5f;

    [Tooltip("Number of spiral arms")]
    [Range(1, 8)]
    public int spiralCount = 3;

    // =========================================
    // TURBULENCE
    // =========================================
    [Header("Turbulence")]
    [Tooltip("Strength of turbulent noise on the surface")]
    [Range(0f, 3f)]
    public float turbulence = 0.8f;

    [Tooltip("Scale of the turbulence pattern")]
    [Range(0.1f, 10f)]
    public float turbulenceScale = 2f;

    // =========================================
    // PULL
    // =========================================
    [Header("Pull")]
    [Tooltip("How strongly vertices are pulled inward (XZ displacement)")]
    [Range(0f, 5f)]
    public float pullStrength = 1f;

    // =========================================
    // WAVES
    // =========================================
    [Header("Waves")]
    public float waveHeight = 1f;
    public float waveSpeed = 1f;

    // =========================================
    // COLORS
    // =========================================
    [Header("Colors")]
    public Color deepColor = new Color(0f, 0.2f, 0.4f, 1f);
    public Color shallowColor = new Color(0f, 0.5f, 0.7f, 1f);
    public Color coreColor = new Color(0f, 0.02f, 0.05f, 1f);
    public Color foamColor = Color.white;

    [Header("Foam")]
    [Range(0f, 2f)]
    public float foamThreshold = 0.6f;
    [Range(0f, 3f)]
    public float foamIntensity = 1f;

    // =========================================
    // DEPTH FADE
    // =========================================
    [Header("Depth Fade (Silhouettes)")]
    [Range(0.1f, 50f)]
    public float depthFadeDistance = 5f;
    public Color depthShallowColor = new Color(0.2f, 0.6f, 0.7f, 1f);

    // =========================================
    // UNDERWATER LIGHTS
    // =========================================
    [Header("Underwater Lights")]
    [Range(0f, 5f)]
    public float subsurfaceIntensity = 1.5f;
    [Range(0.01f, 2f)]
    public float subsurfaceAbsorption = 0.3f;

    // =========================================
    // INTERNALS
    // =========================================
    private Material mat;
    private MaterialPropertyBlock mpb;

    // Shader property IDs (cached for performance)
    static readonly int ID_VortexRadius = Shader.PropertyToID("_VortexRadius");
    static readonly int ID_VortexDepth = Shader.PropertyToID("_VortexDepth");
    static readonly int ID_VortexInnerRadius = Shader.PropertyToID("_VortexInnerRadius");
    static readonly int ID_VortexFunnelPower = Shader.PropertyToID("_VortexFunnelPower");
    static readonly int ID_VortexSpinSpeed = Shader.PropertyToID("_VortexSpinSpeed");
    static readonly int ID_VortexSpiralTightness = Shader.PropertyToID("_VortexSpiralTightness");
    static readonly int ID_VortexSpiralHeight = Shader.PropertyToID("_VortexSpiralHeight");
    static readonly int ID_VortexSpiralCount = Shader.PropertyToID("_VortexSpiralCount");
    static readonly int ID_VortexTurbulence = Shader.PropertyToID("_VortexTurbulence");
    static readonly int ID_VortexTurbulenceScale = Shader.PropertyToID("_VortexTurbulenceScale");
    static readonly int ID_VortexPullStrength = Shader.PropertyToID("_VortexPullStrength");
    static readonly int ID_WaveHeight = Shader.PropertyToID("_WaveHeight");
    static readonly int ID_WaveSpeed = Shader.PropertyToID("_WaveSpeed");
    static readonly int ID_DeepColor = Shader.PropertyToID("_DeepColor");
    static readonly int ID_ShallowColor = Shader.PropertyToID("_ShallowColor");
    static readonly int ID_VortexCoreColor = Shader.PropertyToID("_VortexCoreColor");
    static readonly int ID_FoamColor = Shader.PropertyToID("_FoamColor");
    static readonly int ID_FoamThreshold = Shader.PropertyToID("_FoamThreshold");
    static readonly int ID_FoamIntensity = Shader.PropertyToID("_FoamIntensity");
    static readonly int ID_DepthFadeDistance = Shader.PropertyToID("_DepthFadeDistance");
    static readonly int ID_DepthShallowColor = Shader.PropertyToID("_DepthShallowColor");
    static readonly int ID_SubsurfaceIntensity = Shader.PropertyToID("_SubsurfaceIntensity");
    static readonly int ID_SubsurfaceAbsorption = Shader.PropertyToID("_SubsurfaceAbsorption");

    void OnEnable()
    {
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Use sharedMaterial in editor, material at runtime
            mat = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
        }
        mpb = new MaterialPropertyBlock();
        ReadFromMaterial();
    }

    /// <summary>
    /// Reads current values from the material into the Inspector fields.
    /// Call this if you changed the material externally.
    /// </summary>
    public void ReadFromMaterial()
    {
        if (mat == null) return;

        radius = mat.GetFloat(ID_VortexRadius);
        depth = mat.GetFloat(ID_VortexDepth);
        innerRadius = mat.GetFloat(ID_VortexInnerRadius);
        funnelPower = mat.GetFloat(ID_VortexFunnelPower);
        spinSpeed = mat.GetFloat(ID_VortexSpinSpeed);
        spiralTightness = mat.GetFloat(ID_VortexSpiralTightness);
        spiralHeight = mat.GetFloat(ID_VortexSpiralHeight);
        spiralCount = Mathf.RoundToInt(mat.GetFloat(ID_VortexSpiralCount));
        turbulence = mat.GetFloat(ID_VortexTurbulence);
        turbulenceScale = mat.GetFloat(ID_VortexTurbulenceScale);
        pullStrength = mat.GetFloat(ID_VortexPullStrength);
        waveHeight = mat.GetFloat(ID_WaveHeight);
        waveSpeed = mat.GetFloat(ID_WaveSpeed);
        deepColor = mat.GetColor(ID_DeepColor);
        shallowColor = mat.GetColor(ID_ShallowColor);
        coreColor = mat.GetColor(ID_VortexCoreColor);
        foamColor = mat.GetColor(ID_FoamColor);
        foamThreshold = mat.GetFloat(ID_FoamThreshold);
        foamIntensity = mat.GetFloat(ID_FoamIntensity);
        depthFadeDistance = mat.GetFloat(ID_DepthFadeDistance);
        depthShallowColor = mat.GetColor(ID_DepthShallowColor);
        subsurfaceIntensity = mat.GetFloat(ID_SubsurfaceIntensity);
        subsurfaceAbsorption = mat.GetFloat(ID_SubsurfaceAbsorption);
    }

    void Update()
    {
        ApplyToMaterial();
    }

    void ApplyToMaterial()
    {
        if (mat == null) return;

        mat.SetFloat(ID_VortexRadius, radius);
        mat.SetFloat(ID_VortexDepth, depth);
        mat.SetFloat(ID_VortexInnerRadius, innerRadius);
        mat.SetFloat(ID_VortexFunnelPower, funnelPower);
        mat.SetFloat(ID_VortexSpinSpeed, spinSpeed);
        mat.SetFloat(ID_VortexSpiralTightness, spiralTightness);
        mat.SetFloat(ID_VortexSpiralHeight, spiralHeight);
        mat.SetFloat(ID_VortexSpiralCount, spiralCount);
        mat.SetFloat(ID_VortexTurbulence, turbulence);
        mat.SetFloat(ID_VortexTurbulenceScale, turbulenceScale);
        mat.SetFloat(ID_VortexPullStrength, pullStrength);
        mat.SetFloat(ID_WaveHeight, waveHeight);
        mat.SetFloat(ID_WaveSpeed, waveSpeed);
        mat.SetColor(ID_DeepColor, deepColor);
        mat.SetColor(ID_ShallowColor, shallowColor);
        mat.SetColor(ID_VortexCoreColor, coreColor);
        mat.SetColor(ID_FoamColor, foamColor);
        mat.SetFloat(ID_FoamThreshold, foamThreshold);
        mat.SetFloat(ID_FoamIntensity, foamIntensity);
        mat.SetFloat(ID_DepthFadeDistance, depthFadeDistance);
        mat.SetColor(ID_DepthShallowColor, depthShallowColor);
        mat.SetFloat(ID_SubsurfaceIntensity, subsurfaceIntensity);
        mat.SetFloat(ID_SubsurfaceAbsorption, subsurfaceAbsorption);
    }

    // =========================================
    // PUBLIC API
    // =========================================

    /// <summary>
    /// Sets overall vortex intensity (0 = calm, 1 = full).
    /// Scales depth, pull, spin, turbulence, and spiral height together.
    /// </summary>
    public void SetIntensity(float t)
    {
        t = Mathf.Clamp01(t);
        depth = Mathf.Lerp(0f, 40f, t);
        pullStrength = Mathf.Lerp(0f, 5f, t);
        spinSpeed = Mathf.Lerp(0f, 3f, t);
        turbulence = Mathf.Lerp(0f, 2f, t);
        spiralHeight = Mathf.Lerp(0f, 4f, t);
    }

    /// <summary>
    /// Smoothly transitions the vortex from current state to target intensity.
    /// Call this every frame with the same target for smooth lerp.
    /// </summary>
    public void LerpIntensity(float target, float speed)
    {
        target = Mathf.Clamp01(target);
        float current = GetIntensity();
        float t = Mathf.Lerp(current, target, Time.deltaTime * speed);
        SetIntensity(t);
    }

    /// <summary>
    /// Gets approximate current intensity (0-1) based on depth.
    /// </summary>
    public float GetIntensity()
    {
        return Mathf.Clamp01(depth / 40f);
    }

    /// <summary>
    /// Instantly grows or shrinks the vortex radius.
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(1f, newRadius);
    }

    /// <summary>
    /// Smoothly changes radius over time. Call every frame.
    /// </summary>
    public void LerpRadius(float target, float speed)
    {
        radius = Mathf.Lerp(radius, Mathf.Max(1f, target), Time.deltaTime * speed);
    }

    /// <summary>
    /// Sets water colors (deep, shallow, core) at once.
    /// </summary>
    public void SetColors(Color deep, Color shallow, Color core)
    {
        deepColor = deep;
        shallowColor = shallow;
        coreColor = core;
    }

    /// <summary>
    /// Stops the vortex (no spin, no depth, no pull) without disabling the object.
    /// </summary>
    public void Stop()
    {
        SetIntensity(0f);
    }

    /// <summary>
    /// Resets to default values.
    /// </summary>
    public void ResetDefaults()
    {
        radius = 50f;
        depth = 20f;
        innerRadius = 0.08f;
        funnelPower = 2f;
        spinSpeed = 1f;
        spiralTightness = 5f;
        spiralHeight = 1.5f;
        spiralCount = 3;
        turbulence = 0.8f;
        turbulenceScale = 2f;
        pullStrength = 1f;
        waveHeight = 1f;
        waveSpeed = 1f;
    }
}