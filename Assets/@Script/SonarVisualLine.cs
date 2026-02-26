using UnityEngine;

public class SonarVisualLine : MonoBehaviour
{
    [SerializeField] private float scanLineRadius = 0.16f;
    [SerializeField] private LineRenderer scanLineRenderer;
    [Space]
    [SerializeField] private int speedX = 1;
    [SerializeField] private int speedY = 1;


    private void Update()
    {
        Vector3 center = scanLineRenderer.GetPosition(1);
        Vector3 point = center;

        point.x = Mathf.Sin(Time.time * speedX) * scanLineRadius;
        point.y = Mathf.Cos(Time.time * speedY) * scanLineRadius;

        scanLineRenderer.SetPosition(1, point);
    }

    public Vector2 GetScanDirection()
    {
        Vector3 center = scanLineRenderer.GetPosition(0);
        Vector3 point = scanLineRenderer.GetPosition(1);
        Vector2 dir = new Vector2(point.x - center.x, point.y - center.y).normalized;
        return dir;
    }

}
