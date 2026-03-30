using UnityEngine;

public class ScaleByDistanceEffect : MonoBehaviour
{
    [SerializeField] private Vector3 minScale;
    [SerializeField] private Vector3 maxScale;

    [SerializeField] private float scaleFactor;


    private GameObject cameraObject;

    private void Awake()
    {
        
    }

    private void Start()
    {
        cameraObject = Camera.main.gameObject;
    }

    private void Update()
    {
        if (cameraObject == null) return;

        Vector3 scale = Vector3.one;

        float distance = Vector3.Distance(transform.position, cameraObject.transform.position);

        scale = Vector3.one * scaleFactor * (distance);

        scale = new Vector3(Mathf.Clamp(scale.x, minScale.x, maxScale.x), Mathf.Clamp(scale.y, minScale.y, maxScale.y), Mathf.Clamp(scale.z, minScale.z, maxScale.z));

        transform.localScale = scale;
    }
}
