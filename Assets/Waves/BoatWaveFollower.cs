using UnityEngine;

public class WaveFollower : MonoBehaviour
{
    [Header("Height Follow")]
    public float heightOffset = 0f;
    public float followSpeed = 5f;

    [Header("Tilt Settings")]
    public float sampleDistance = 1.2f;
    public float tiltStrength = 1.0f;
    public float tiltSmoothness = 5f;
    public float maxTiltAngle = 20f;

    // Shake state (privado)
    private float _shakeMagnitude;
    private float _shakeDuration;
    private float _shakeTimer;
    private Vector3 _shakeOffset;
    private Quaternion _shakeRotOffset = Quaternion.identity;

    [Header("Shake Settings")]
    [Tooltip("Quanto do shake vira rotação (graus por unidade de magnitude)")]
    public float shakeRotationFactor = 15f;


    /// <summary>
    /// Chama pra disparar um bump/shake.
    /// magnitude: intensidade (ex: 0.3 = leve, 1.0 = forte)
    /// duration: duração em segundos antes de decair totalmente
    /// </summary>
    public void Shake(float magnitude, float duration)
    {
        // Permite empilhar: pega o maior entre o atual e o novo
        _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
        _shakeDuration = Mathf.Max(_shakeDuration, duration);
        _shakeTimer = _shakeDuration;
    }

    void Update()
    {
        if (OceanManager.Instance == null)
            return;

        Vector3 pos = transform.position;

        // ==========================
        // SHAKE / BUMP (decai linear)
        // ==========================
        if (_shakeTimer > 0f)
        {
            _shakeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_shakeTimer / _shakeDuration); // 1→0
            float currentMag = _shakeMagnitude * t;

            _shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * currentMag,
                Random.Range(-1f, 1f) * currentMag,
                Random.Range(-1f, 1f) * currentMag
            );

            _shakeRotOffset = Quaternion.Euler(
                Random.Range(-1f, 1f) * currentMag * shakeRotationFactor,
                Random.Range(-1f, 1f) * currentMag * shakeRotationFactor * 0.5f,
                Random.Range(-1f, 1f) * currentMag * shakeRotationFactor
            );
        }
        else
        {
            _shakeOffset = Vector3.zero;
            _shakeRotOffset = Quaternion.identity;
            _shakeMagnitude = 0f;
        }

        // ==========================
        // ALTURA
        // ==========================
        float waveY = OceanManager.Instance.GetWaveHeight(pos);
        Vector3 targetPos = new Vector3(pos.x, waveY + heightOffset, pos.z);
        transform.position = Vector3.Lerp(pos, targetPos, Time.deltaTime * followSpeed)
                             + _shakeOffset;

        // ==========================
        // AMOSTRAGEM PARA TILT
        // ==========================
        Vector3 forwardSample = pos + transform.forward * sampleDistance;
        Vector3 rightSample = pos + transform.right * sampleDistance;

        float forwardY = OceanManager.Instance.GetWaveHeight(forwardSample);
        float rightY = OceanManager.Instance.GetWaveHeight(rightSample);

        float pitch = (forwardY - waveY) * tiltStrength;
        float roll = (rightY - waveY) * tiltStrength;

        float pitchAngle = Mathf.Clamp(-pitch * 30f, -maxTiltAngle, maxTiltAngle);
        float rollAngle = Mathf.Clamp(roll * 30f, -maxTiltAngle, maxTiltAngle);

        Quaternion targetRot =
            Quaternion.Euler(pitchAngle, transform.eulerAngles.y, rollAngle);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot * _shakeRotOffset,
            Time.deltaTime * tiltSmoothness);
    }
}