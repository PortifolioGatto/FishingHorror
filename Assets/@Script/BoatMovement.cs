using UnityEngine;
using UnityEngine.InputSystem;

public class BoatMovement : MonoBehaviour
{
    public bool holdingWheel = false;
    public bool movementEnabled = false;
    public bool respectLimit = true;

    [SerializeField] private Vector3 minSizeSea;
    [SerializeField] private Vector3 maxSizeSea;

    [Space]

    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float damping = 0.1f;

    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float turnDamping = 0.1f;
    [SerializeField] private float steeringWheelRot = 30f;

    [SerializeField] private Transform steeringWheel;
    [SerializeField] private AcceleratorPoints acceleratorRight;
    [SerializeField] private AcceleratorPoints acceleratorLeft;

    [SerializeField] private AudioSource boatAudioSource;

    private int throttleDirection = 0; // -1 for reverse, 0 for idle, 1 for forward
    public int Throttle => throttleDirection;
    private float lastVerticalInput = 0f;

    private float currentSpeed = 0f;

    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference turnAction;

    public static BoatMovement Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        acceleratorLeft.GoToPoint(1);
        acceleratorRight.GoToPoint(1);
    }

    private void Update()
    {
        if (!movementEnabled)
        {
            return;
        }

        float inputVertical = moveAction.action.ReadValue<float>();
        float inputHorizontal = turnAction.action.ReadValue<float>();

        if (PlayerBoatManager.Instance.holdingWheel == false)
        {
            inputHorizontal = 0f;
            inputVertical = 0f;
        }

        if (throttleDirection != 0)
        {
            if (!boatAudioSource.isPlaying)
                boatAudioSource.Play();
        }
        else
        {
            if (boatAudioSource.isPlaying)
                boatAudioSource.Stop();
        }

        if (lastVerticalInput != inputVertical && inputVertical != 0f)
        {
            if (throttleDirection == 0)
            {
                throttleDirection = inputVertical > 0 ? 1 : -1;
            }
            else if ((throttleDirection == 1 && inputVertical < 0) || (throttleDirection == -1 && inputVertical > 0))
            {
                throttleDirection = 0;
            }

            if (throttleDirection == -1)
            {
                acceleratorLeft.GoToPoint(0);
                acceleratorRight.GoToPoint(0);
            }
            else if (throttleDirection == 0)
            {
                acceleratorLeft.GoToPoint(1);
                acceleratorRight.GoToPoint(1);
            }
            else if(throttleDirection == 1)
            {
                acceleratorLeft.GoToPoint(2);
                acceleratorRight.GoToPoint(2);
            }

        }
        lastVerticalInput = inputVertical;

        // Handle forward/backward movement using transform.Translate
        float targetSpeed = throttleDirection * maxSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        transform.Translate(Vector3.forward * targetSpeed * Time.deltaTime);

        if(throttleDirection != 0)
        {
            // Handle turning using transform.Rotate
            float turnAmount = inputHorizontal * turnSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, turnAmount);
        }

        // Handle steering wheel rotation
        float targetWheelRotation = inputHorizontal * steeringWheelRot; // Rotate up to 30 degrees based on input
        float currentWheelRotation = steeringWheel.localEulerAngles.z;

        // Smoothly rotate the steering wheel towards the target rotation
        float newWheelRotation = Mathf.LerpAngle(currentWheelRotation, targetWheelRotation, 25f * Time.deltaTime);

        steeringWheel.localEulerAngles = new Vector3(steeringWheel.localEulerAngles.x, steeringWheel.localEulerAngles.y, newWheelRotation);


        LimitPos();
    }

    private void LimitPos()
    {
        if (!respectLimit)
            return;
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minSizeSea.x, maxSizeSea.x);
        pos.z = Mathf.Clamp(pos.z, minSizeSea.z, maxSizeSea.z);
        transform.position = pos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Vector3 center = (minSizeSea + maxSizeSea) / 2f;
        Vector3 size = maxSizeSea - minSizeSea;
        Gizmos.DrawWireCube(center, size);
    }
}
