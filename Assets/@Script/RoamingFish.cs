using UnityEngine;

public class RoamingFish : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private float turnSpeed = 1f;
    [SerializeField] private float surfaceLevel = -2.88f;

    [SerializeField] private float minY = -3.5f;
    [SerializeField] private float maxY = -2f;

    private Vector3 orbitPoint;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();

        orbitPoint = transform.position + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
    }

    private void Update()
    {
        // Move towards the orbit point
        Vector3 direction = (orbitPoint - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        // Rotate to face the movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
        // If close to the orbit point, choose a new one
        if (Vector3.Distance(transform.position, orbitPoint) < 0.5f)
        {
            orbitPoint = transform.position + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
            orbitPoint.y = Mathf.Clamp(orbitPoint.y, minY, maxY);
        }
        // Ensure the fish stays at the surface level
        Vector3 pos = transform.position;
        pos.y = surfaceLevel;
        transform.position = pos;
    }
}
