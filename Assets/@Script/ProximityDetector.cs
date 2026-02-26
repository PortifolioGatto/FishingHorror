using UnityEngine;
using UnityEngine.Events;

public class ProximityDetector : MonoBehaviour
{
    [Tooltip("Radius within which to detect objects")]
    public float detectionRadius = 5f;
    [Tooltip("Layer mask for objects to detect")]
    public LayerMask detectionLayer;

    public UnityEvent onDetected;

    private bool hasDetected = false;


    private void Update()
    {
        if (hasDetected)
        {
            return;
        }

        // Detect objects within the specified radius and layer
        Collider[] detectedObjects = Physics.OverlapSphere(transform.position, detectionRadius, detectionLayer);
        foreach (Collider obj in detectedObjects)
        {
            // Trigger the event for each detected object
            onDetected?.Invoke();
            hasDetected = true;
            break;
        }
    }
    private void OnDrawGizmosSelected()
    {
        // Visualize the detection radius in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
