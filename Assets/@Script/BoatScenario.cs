using UnityEngine;

public class BoatScenario : MonoBehaviour
{
    [SerializeField] private Transform stayAwayFrom;
    [SerializeField] private float stayAwayDistanceMin = 5f;
    [SerializeField] private float stayAwayDistanceMax = 5f;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float turnSpeed = 2f;
    private Vector3 moveDirection;
    [Space]
    [SerializeField] private float minMoveTime = 2f;
    [SerializeField] private float maxMoveTime = 10f;

    private float moveTimer;

    private void Start()
    {
        moveTimer = Random.Range(minMoveTime, maxMoveTime);
    }

    private void Update()
    {
        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0f)
        {
            moveDirection = Random.insideUnitCircle.normalized;
            moveDirection = new Vector3(moveDirection.x, 0f, moveDirection.y);
            moveTimer = Random.Range(minMoveTime, maxMoveTime);

            if(Random.value < 0.33f)
            {
                moveDirection = Vector3.zero;
            }
        }

        Vector3 targetDirection = moveDirection;
        if (stayAwayFrom != null)
        {
            Vector3 toStayAway = transform.position - stayAwayFrom.position;

            float distance = toStayAway.magnitude;
            if (distance < stayAwayDistanceMin)
            {
                transform.position = stayAwayFrom.position + toStayAway.normalized * stayAwayDistanceMin;
            }
            else if (distance > stayAwayDistanceMax)
            {
                transform.position = stayAwayFrom.position + toStayAway.normalized * stayAwayDistanceMax;

            }
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                transform.position += transform.forward * speed * Time.deltaTime;
            }
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (stayAwayFrom != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(stayAwayFrom.position, stayAwayDistanceMin);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(stayAwayFrom.position, stayAwayDistanceMax);
        }
    }
}
