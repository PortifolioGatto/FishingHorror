using UnityEngine;

public class BillboardEffect : MonoBehaviour
{
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    private void LateUpdate()
    {
        // Make the object face the camera
        if (Camera.main != null)
        {
            Vector3 direction = Camera.main.transform.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(rotationOffset);
            transform.rotation = targetRotation;
        }
    }
}
