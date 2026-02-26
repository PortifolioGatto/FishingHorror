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

    void Update()
    {
        if (OceanManager.Instance == null)
            return;

        Vector3 pos = transform.position;

        // ==========================
        // ALTURA
        // ==========================
        float waveY = OceanManager.Instance.GetWaveHeight(pos);

        Vector3 targetPos = new Vector3(pos.x, waveY + heightOffset, pos.z);
        transform.position = Vector3.Lerp(pos, targetPos, Time.deltaTime * followSpeed);

        // ==========================
        // AMOSTRAGEM PARA TILT
        // ==========================
        Vector3 forwardSample = pos + transform.forward * sampleDistance;
        Vector3 rightSample = pos + transform.right * sampleDistance;

        float forwardY = OceanManager.Instance.GetWaveHeight(forwardSample);
        float rightY = OceanManager.Instance.GetWaveHeight(rightSample);

        float pitch = (forwardY - waveY) * tiltStrength;
        float roll = (rightY - waveY) * tiltStrength;

        // Converter para ângulo
        float pitchAngle = Mathf.Clamp(-pitch * 30f, -maxTiltAngle, maxTiltAngle);
        float rollAngle = Mathf.Clamp(roll * 30f, -maxTiltAngle, maxTiltAngle);

        Quaternion targetRot =
            Quaternion.Euler(pitchAngle, transform.eulerAngles.y, rollAngle);


        transform.rotation = Quaternion.Slerp(
        transform.rotation,
        targetRot,
        Time.deltaTime * tiltSmoothness);
    }
}
