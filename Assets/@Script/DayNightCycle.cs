using TMPro;
using UnityEngine;

/// <summary>
/// Complete day-night cycle controller for the Custom/PixelatedSkybox shader.
/// Controls time, 2-axis sun/moon orbits, directional light, ambient, and all shader properties.
/// </summary>
[ExecuteInEditMode]
public class DayNightCycle : MonoBehaviour
{
    public float timeScale = 1f; // Multiplier for time progression speed (1 = normal, 2 = double speed, etc.)
    public float dayDuration;

    [SerializeField] private TextMeshProUGUI timeDisplay; // Optional UI text to show current time of day (for testing)

    public static DayNightCycle Instance { get; private set; }

    // ===================== TIME =====================
    [Header("Time of Day")]
    [Tooltip("0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset")]
    [Range(0f, 1f)]
    public float timeOfDay = 0.5f;

    [Tooltip("Duration of a full day cycle in seconds. 0 = manual only.")]
    public float cycleDuration = 120f;

    public float timeLimit = -1;

    public bool paused = false;

    // ===================== SUN ORBIT (2-axis) =====================
    [Header("Sun Orbit")]
    [Tooltip("Tilts the sun's orbit plane (think seasons / latitude)")]
    [Range(-90f, 90f)]
    public float sunOrbitPitch = 0f;

    [Tooltip("Rotates the sun's orbit around the Y axis")]
    [Range(-180f, 180f)]
    public float sunOrbitYaw = 0f;

    // ===================== MOON ORBIT (2-axis) =====================
    [Header("Moon Orbit")]
    [Tooltip("If true, moon is always opposite the sun (ignores independent orbit)")]
    public bool moonOppositeSun = true;

    [Tooltip("Tilts the moon's independent orbit plane")]
    [Range(-90f, 90f)]
    public float moonOrbitPitch = 5f;

    [Tooltip("Rotates the moon's independent orbit around Y")]
    [Range(-180f, 180f)]
    public float moonOrbitYaw = 10f;

    // ===================== REFERENCES =====================
    [Header("References")]
    [Tooltip("Skybox material (Custom/PixelatedSkybox). Auto-detects from RenderSettings if null.")]
    public Material skyboxMaterial;

    [Tooltip("Main directional light (rotates with sun)")]
    public Light sunLight;

    [Tooltip("Optional secondary directional light for moonlight")]
    public Light moonLight;

    // ===================== SUN LIGHT =====================
    [Header("Sun Light Settings")]
    public Gradient sunLightColorGradient;
    public AnimationCurve sunIntensityCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [Range(0f, 5f)]
    public float maxSunIntensity = 1.3f;

    // ===================== MOON LIGHT =====================
    [Header("Moon Light Settings")]
    public Color moonLightColor = new Color(0.15f, 0.18f, 0.35f);
    [Range(0f, 2f)]
    public float moonLightIntensity = 0.15f;

    // ===================== AMBIENT =====================
    [Header("Ambient Light")]
    public Gradient ambientColorGradient;
    [Range(0f, 2f)]
    public float ambientIntensity = 1f;

    public AnimationCurve ambientIntensityCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // ===================== FOG =====================
    [Header("Fog (optional)")]
    public bool controlFog = false;
    public Gradient fogColorGradient;

    // ===================== SHADER IDS =====================
    static readonly int ID_TimeOfDay = Shader.PropertyToID("_TimeOfDay");
    static readonly int ID_AutoCycle = Shader.PropertyToID("_AutoCycle");
    static readonly int ID_SunOrbitPitch = Shader.PropertyToID("_SunOrbitPitch");
    static readonly int ID_SunOrbitYaw = Shader.PropertyToID("_SunOrbitYaw");
    static readonly int ID_MoonOrbitPitch = Shader.PropertyToID("_MoonOrbitPitch");
    static readonly int ID_MoonOrbitYaw = Shader.PropertyToID("_MoonOrbitYaw");
    static readonly int ID_MoonOpposite = Shader.PropertyToID("_MoonOppositeSun");

    // Global IDs for ocean/water reflection
    static readonly int ID_CelestialSunDir = Shader.PropertyToID("_CelestialSunDir");
    static readonly int ID_CelestialMoonDir = Shader.PropertyToID("_CelestialMoonDir");
    static readonly int ID_CelestialDayFactor = Shader.PropertyToID("_CelestialDayFactor");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple DayNightCycle instances found. Destroying duplicate.");
            DestroyImmediate(this);
            return;
        }
        Instance = this;
    }

    // ===================== INIT =====================
    void OnEnable()
    {
        SetupDefaults();
    }

    void Start()
    {
        SetupDefaults();
    }

    void SetupDefaults()
    {
        // Auto-find skybox material
        if (skyboxMaterial == null && RenderSettings.skybox != null)
        {
            if (RenderSettings.skybox.shader.name == "Custom/PixelatedSkybox")
                skyboxMaterial = RenderSettings.skybox;
        }

        // Disable shader auto-cycle (script controls it)
        if (skyboxMaterial != null)
            skyboxMaterial.SetFloat(ID_AutoCycle, 0);

        // Setup default gradient if empty
        if (sunLightColorGradient == null || sunLightColorGradient.colorKeys.Length <= 1)
        {
            sunLightColorGradient = new Gradient();
            sunLightColorGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 0.0f),   // midnight
                    new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.24f),     // pre-sunrise
                    new GradientColorKey(new Color(1f, 0.7f, 0.4f), 0.26f),     // sunrise
                    new GradientColorKey(new Color(1f, 0.96f, 0.88f), 0.35f),   // morning
                    new GradientColorKey(new Color(1f, 0.98f, 0.92f), 0.5f),    // noon
                    new GradientColorKey(new Color(1f, 0.96f, 0.88f), 0.65f),   // afternoon
                    new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.74f),     // pre-sunset
                    new GradientColorKey(new Color(1f, 0.4f, 0.15f), 0.76f),    // sunset
                    new GradientColorKey(new Color(0.1f, 0.1f, 0.2f), 1.0f),    // midnight
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
            );
        }

        if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length <= 1)
        {
            ambientColorGradient = new Gradient();
            ambientColorGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.04f, 0.04f, 0.08f), 0.0f),
                    new GradientColorKey(new Color(0.3f, 0.25f, 0.2f), 0.25f),
                    new GradientColorKey(new Color(0.5f, 0.55f, 0.65f), 0.5f),
                    new GradientColorKey(new Color(0.3f, 0.25f, 0.2f), 0.75f),
                    new GradientColorKey(new Color(0.04f, 0.04f, 0.08f), 1.0f),
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
            );
        }

        if (fogColorGradient == null || fogColorGradient.colorKeys.Length <= 1)
        {
            fogColorGradient = new Gradient();
            fogColorGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0.0f),
                    new GradientColorKey(new Color(0.7f, 0.5f, 0.3f), 0.25f),
                    new GradientColorKey(new Color(0.7f, 0.8f, 0.9f), 0.5f),
                    new GradientColorKey(new Color(0.7f, 0.5f, 0.3f), 0.75f),
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 1.0f),
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
            );
        }
    }

    // ===================== UPDATE =====================
    void Update()
    {

        //Calculate how much time still needs to pass until the next day starts using timeScale cycleDuration and timeOfDay, in seconds
        dayDuration = (1f - timeOfDay) * cycleDuration / timeScale;

        // Advance time
        if (!paused && cycleDuration > 0f)
        {
            float dt = Application.isPlaying ? Time.deltaTime * timeScale : 0f;
            timeOfDay += dt / cycleDuration;

            if(timeLimit > 0f)
            {
                if (timeOfDay >= timeLimit)
                {
                    timeOfDay = timeLimit;
                }
            }

            if (timeOfDay > 1f) timeOfDay -= 1f;
        }

        UpdateShader();
        UpdateSunLight();
        UpdateMoonLight();
        UpdateAmbient();
        UpdateFog();

        UpdateUI();
    }

    void UpdateUI()
    {
        if (timeDisplay != null)
        {
            int hours = Mathf.FloorToInt(timeOfDay * 24f);
            int minutes = Mathf.FloorToInt((timeOfDay * 24f - hours) * 60f);
            timeDisplay.text = string.Format("{0:00}:{1:00}", hours, minutes);
        }
    }

    // ===================== SHADER =====================
    void UpdateShader()
    {
        if (skyboxMaterial == null) return;

        skyboxMaterial.SetFloat(ID_TimeOfDay, timeOfDay);
        skyboxMaterial.SetFloat(ID_SunOrbitPitch, sunOrbitPitch);
        skyboxMaterial.SetFloat(ID_SunOrbitYaw, sunOrbitYaw);
        skyboxMaterial.SetFloat(ID_MoonOrbitPitch, moonOrbitPitch);
        skyboxMaterial.SetFloat(ID_MoonOrbitYaw, moonOrbitYaw);
        skyboxMaterial.SetFloat(ID_MoonOpposite, moonOppositeSun ? 1f : 0f);

        // Expose sun/moon directions globally for any shader (ocean, water, etc.)
        Vector3 sunDir = GetSunDirection();
        Vector3 moonDir = GetMoonDirection();
        float dayFactor = Mathf.Clamp01(sunDir.y * 3f + 0.5f);

        Shader.SetGlobalVector(ID_CelestialSunDir, new Vector4(sunDir.x, sunDir.y, sunDir.z, 0f));
        Shader.SetGlobalVector(ID_CelestialMoonDir, new Vector4(moonDir.x, moonDir.y, moonDir.z, 0f));
        Shader.SetGlobalFloat(ID_CelestialDayFactor, dayFactor);
    }

    // ===================== SUN DIRECTION =====================
    /// <summary>
    /// Computes sun direction using the same math as the shader (2-axis orbit).
    /// </summary>
    public Vector3 GetSunDirection()
    {
        float mainAngle = timeOfDay * Mathf.PI * 2f - Mathf.PI * 0.5f;
        Vector3 baseDir = new Vector3(0f, Mathf.Sin(mainAngle), Mathf.Cos(mainAngle));

        Quaternion rot = Quaternion.Euler(sunOrbitPitch, sunOrbitYaw, 0f);
        return (rot * baseDir).normalized;
    }

    /// <summary>
    /// Computes moon direction using the same math as the shader (2-axis orbit).
    /// </summary>
    public Vector3 GetMoonDirection()
    {
        if (moonOppositeSun)
            return -GetSunDirection();

        float mainAngle = timeOfDay * Mathf.PI * 2f + Mathf.PI * 0.5f;
        Vector3 baseDir = new Vector3(0f, Mathf.Sin(mainAngle), Mathf.Cos(mainAngle));

        Quaternion rot = Quaternion.Euler(moonOrbitPitch, moonOrbitYaw, 0f);
        return (rot * baseDir).normalized;
    }

    // ===================== SUN LIGHT =====================
    void UpdateSunLight()
    {
        if (sunLight == null) return;

        Vector3 sunDir = GetSunDirection();
        sunLight.transform.forward = -sunDir;

        // Evaluate gradient at current time
        sunLight.color = sunLightColorGradient.Evaluate(timeOfDay);

        // Intensity: use curve mapped to sun height
        float sunHeight = Mathf.Clamp01(sunDir.y);
        sunLight.intensity = sunIntensityCurve.Evaluate(sunHeight) * maxSunIntensity;

        // Disable when below horizon
        sunLight.enabled = sunDir.y > -0.05f;
    }

    // ===================== MOON LIGHT =====================
    void UpdateMoonLight()
    {
        if (moonLight == null) return;

        Vector3 moonDir = GetMoonDirection();
        moonLight.transform.forward = -moonDir;

        moonLight.color = moonLightColor;

        float dayFactor = Mathf.Clamp01(GetSunDirection().y * 3f + 0.5f);
        float moonHeight = Mathf.Clamp01(moonDir.y);
        moonLight.intensity = moonLightIntensity * moonHeight * (1f - dayFactor);

        moonLight.enabled = moonDir.y > -0.05f && dayFactor < 0.8f;
    }

    // ===================== AMBIENT =====================
    void UpdateAmbient()
    {
        RenderSettings.ambientLight = ambientColorGradient.Evaluate(timeOfDay) * ambientIntensity;
        // modulate ambient intensity with sun height for better contrast at night 0 = night, .5 = day, 1 = night again
        RenderSettings.ambientIntensity = ambientIntensityCurve.Evaluate(GetSunDirection().y * 0.5f + 0.5f) * ambientIntensity;
    }

    // ===================== FOG =====================
    void UpdateFog()
    {
        if (!controlFog) return;
        RenderSettings.fogColor = fogColorGradient.Evaluate(timeOfDay);
    }

    // ===================== PUBLIC API =====================

    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Max(0f, scale);
    }

    /// <summary>Set time instantly. 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.</summary>
    public void SetTime(float t)
    {
        timeOfDay = Mathf.Repeat(t, 1f);
    }

    public void GetTime(out int hours, out int minutes)
    {
        float totalHours = timeOfDay * 24f;
        hours = Mathf.FloorToInt(totalHours);
        minutes = Mathf.FloorToInt((totalHours - hours) * 60f);
    }

    public void SetTime(int hours, int minutes)
    {
        hours = Mathf.Clamp(hours, 0, 23);
        minutes = Mathf.Clamp(minutes, 1, 59);
        float t = (hours + minutes / 60f) / 24f;
        SetTime(t);
    }

    public void SetLimitTime(int hours, int minutes)
    {
        if (hours < -1)
        {
            timeLimit = -1; 
            return;
        }

        hours = Mathf.Clamp(hours, 0, 23);
        minutes = Mathf.Clamp(minutes, 1, 59);
        timeLimit = (hours + minutes / 60f) / 24f;
    }

    public float GetTimeOfDay()
    {
        return timeOfDay;
    }

    /// <summary>Is it currently daytime?</summary>
    public bool IsDaytime()
    {
        return GetSunDirection().y > 0f;
    }

    /// <summary>Returns 0 at midnight, 1 at noon. Useful for blending.</summary>
    public float GetDayFactor()
    {
        return Mathf.Clamp01(GetSunDirection().y * 3f + 0.5f);
    }

    /// <summary>Set sun orbit angles at runtime.</summary>
    public void SetSunOrbit(float pitch, float yaw)
    {
        sunOrbitPitch = Mathf.Clamp(pitch, -90f, 90f);
        sunOrbitYaw = Mathf.Clamp(yaw, -180f, 180f);
    }

    /// <summary>Set moon orbit angles at runtime.</summary>
    public void SetMoonOrbit(float pitch, float yaw)
    {
        moonOrbitPitch = Mathf.Clamp(pitch, -90f, 90f);
        moonOrbitYaw = Mathf.Clamp(yaw, -180f, 180f);
    }

    // ===================== CLEANUP =====================
    void OnDisable()
    {
        // Reset to noon so material preview looks good
        if (skyboxMaterial != null)
            skyboxMaterial.SetFloat(ID_TimeOfDay, 0.5f);
    }

    // ===================== GIZMOS =====================
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 sunDir = GetSunDirection();
        Vector3 moonDir = GetMoonDirection();

        // Draw sun orbit arc
        Gizmos.color = Color.yellow;
        DrawOrbitGizmo(sunOrbitPitch, sunOrbitYaw, 10f);
        Gizmos.DrawRay(Vector3.zero, sunDir * 12f);
        Gizmos.DrawWireSphere(sunDir * 12f, 0.5f);

        // Draw moon orbit arc
        Gizmos.color = new Color(0.4f, 0.5f, 0.9f);
        if (!moonOppositeSun)
            DrawOrbitGizmo(moonOrbitPitch, moonOrbitYaw, 8f);
        Gizmos.DrawRay(Vector3.zero, moonDir * 10f);
        Gizmos.DrawWireSphere(moonDir * 10f, 0.4f);
    }

    void DrawOrbitGizmo(float pitch, float yaw, float radius)
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        int segments = 48;
        Vector3 prev = rot * new Vector3(0, Mathf.Sin(0), Mathf.Cos(0)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            Vector3 next = rot * new Vector3(0, Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
