using UnityEngine;

[ExecuteAlways]
public class OceanManager : MonoBehaviour
{
    public static OceanManager Instance;

    [Header("Wave Global Settings")]
    [Range(0f, 2f)]
    public float waveHeight = 0.6f;

    [Range(0f, 5f)]
    public float waveSpeed = 1.0f;

    [Header("Ripple Settings")]
    public float rippleStrength = 0.5f;
    public float rippleFrequency = 8f;
    public float rippleSpeed = 4f;
    public float rippleFalloff = 1.5f;

    const int MAX_RIPPLES = 8;

    Vector4[] rippleData = new Vector4[MAX_RIPPLES];
    int rippleIndex = 0;
    int activeRipples = 0;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Globals para o shader
        Shader.SetGlobalFloat("_WaveHeight", waveHeight);
        Shader.SetGlobalFloat("_WaveSpeed", waveSpeed);

        Shader.SetGlobalFloat("_RippleStrength", rippleStrength);
        Shader.SetGlobalFloat("_RippleFrequency", rippleFrequency);
        Shader.SetGlobalFloat("_RippleSpeed", rippleSpeed);
        Shader.SetGlobalFloat("_RippleFalloff", rippleFalloff);

        Shader.SetGlobalVectorArray("_RipplePoints", rippleData);
        Shader.SetGlobalFloat("_RippleCount", activeRipples);
    }

    // ============================================
    // GERSTNER (IDÊNTICO AO SHADER)
    // ============================================

    float GerstnerWave(
        Vector2 dir,
        float steepness,
        float wavelength,
        float speed,
        float phase,
        Vector3 worldPos)
    {
        float k = 2f * Mathf.PI / wavelength;
        float c = Mathf.Sqrt(9.8f / k) * speed;

        float f = k * Vector2.Dot(dir, new Vector2(worldPos.x, worldPos.z))
                  - c * Time.time
                  + phase;

        return Mathf.Sin(f) * steepness;
    }

    // ============================================
    // RIPPLE (IDÊNTICO AO SHADER)
    // ============================================

    float ComputeRipple(Vector3 worldPos)
    {
        float rippleHeight = 0f;

        for (int i = 0; i < activeRipples; i++)
        {
            Vector3 ripplePos = rippleData[i];
            float startTime = rippleData[i].w;

            float time = Time.time - startTime;
            if (time < 0f) continue;

            float dist = Vector2.Distance(
                new Vector2(worldPos.x, worldPos.z),
                new Vector2(ripplePos.x, ripplePos.z)
            );

            float wave = Mathf.Sin(dist * rippleFrequency - time * rippleSpeed);

            float falloff = Mathf.Exp(-dist * rippleFalloff) * Mathf.Exp(-time);

            rippleHeight += wave * falloff;
        }

        return rippleHeight * rippleStrength;
    }

    // ============================================
    // FUNÇÃO PRINCIPAL
    // ============================================

    public float GetWaveHeight(Vector3 worldPos)
    {
        Vector3 samplePos = worldPos;

        // Drift (igual shader)
        Vector2 drift = new Vector2(
            Mathf.Sin(Time.time * 0.05f),
            Mathf.Cos(Time.time * 0.037f)
        ) * 5f;

        samplePos.x += drift.x;
        samplePos.z += drift.y;

        Vector2 d1 = new Vector2(0.8f, 0.2f).normalized;
        Vector2 d2 = new Vector2(-0.4f, 0.9f).normalized;
        Vector2 d3 = new Vector2(0.6f, -0.7f).normalized;
        Vector2 d4 = new Vector2(-0.9f, -0.3f).normalized;

        float wave = 0f;

        wave += GerstnerWave(d1, 0.35f, 18f, waveSpeed, 1.3f, samplePos);
        wave += GerstnerWave(d2, 0.25f, 12f, waveSpeed * 0.8f, 2.7f, samplePos);
        wave += GerstnerWave(d3, 0.20f, 8f, waveSpeed * 1.2f, 4.1f, samplePos);
        wave += GerstnerWave(d4, 0.15f, 5f, waveSpeed * 1.4f, 5.9f, samplePos);

        wave *= waveHeight;

        // Ripple adicional
        wave += ComputeRipple(worldPos);

        return wave;
    }

    // ============================================
    // SPAWN RIPPLE
    // ============================================

    public void SpawnRipple(Vector3 worldPosition)
    {
        rippleData[rippleIndex] = new Vector4(
            worldPosition.x,
            worldPosition.y,
            worldPosition.z,
            Time.time
        );

        rippleIndex = (rippleIndex + 1) % MAX_RIPPLES;
        activeRipples = Mathf.Min(activeRipples + 1, MAX_RIPPLES);
    }
}