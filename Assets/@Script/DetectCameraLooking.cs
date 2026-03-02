using UnityEngine;
using UnityEngine.Events;

public class DetectCameraLooking : MonoBehaviour
{
    [SerializeField] private float sightRange = 0.9f;
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField] private UnityEvent onCameraLookedAt;

    private bool triggered;

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (_mainCamera == null) return;

        Transform camTransform = _mainCamera.transform;

        // Direção DA CÂMERA PARA O OBJETO
        Vector3 directionToObject = (transform.position - camTransform.position).normalized;

        // Dot product entre o forward da câmera e a direção até o objeto
        // Quanto mais perto de 1, mais alinhado (câmera olhando direto pro objeto)
        float dot = Vector3.Dot(camTransform.forward, directionToObject);

        if (dot < sightRange)
        {
            Debug.Log("Câmera não está olhando pro objeto. Dot: " + dot);
            return;
        }

        float distance = Vector3.Distance(camTransform.position, transform.position);

        // Raycast DA CÂMERA em direção ao objeto, checando obstáculos
        if (Physics.Raycast(camTransform.position, directionToObject, distance, obstacleMask))
        {
            Debug.Log("Câmera olhando pro objeto, mas há obstáculo no caminho.");
            return;
        }

        if (!triggered)
        {
            onCameraLookedAt?.Invoke();
            triggered = true;
        }

        Debug.Log("Câmera está olhando pro objeto! Dot: " + dot);
    }
}