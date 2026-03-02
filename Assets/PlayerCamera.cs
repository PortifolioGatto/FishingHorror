using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour, IListenConfigChanged
{
    public bool cameraEnabled = true;

    [SerializeField] private Transform playerHeadPos;

    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [SerializeField] private InputActionReference mouseDelta;

    [SerializeField] private Transform orientation;

    [Space]

    [SerializeField] private bool headBobEnabled = true;
    [SerializeField] private float headBobFrequency = 1.5f;
    [SerializeField] private float headBobAmplitude = 0.05f;
    [SerializeField] private float headBobSmoothing = 5f;
    
    private Vector3 initialCameraPosition;
    private float headBobOffset = 0f;
    private float headBobTimer = 0f;

    public static Transform Orientation => Instance.orientation;

    private static PlayerCamera instance;
    public static PlayerCamera Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<PlayerCamera>();
            }
            return instance;
        }
    }

    private float verticalRotation = 0f;
    private float horizontalRotation = 0f;

    public float GetHorizontalRotation() => horizontalRotation;

    private void Start()
    {
        // Lock the cursor to the center of the screen and hide it

        sensitivity = PlayerPrefs.GetFloat(ConfigsAreaInGame.PlayerPrefsSensitivityKey, 0.5f);

        instance = this;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if(!cameraEnabled) return;


        HandleHeadBob();

        

        Vector2 mouseDeltaValue = mouseDelta.action.ReadValue<Vector2>();
        float mouseX = mouseDeltaValue.x * sensitivity;
        float mouseY = mouseDeltaValue.y * sensitivity;

        horizontalRotation += mouseX;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxVerticalAngle, maxVerticalAngle);
        transform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);

        orientation.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }

    private void LateUpdate()
    {
        transform.position = playerHeadPos.position + Vector3.up * headBobOffset;
    }

    private void HandleHeadBob()
    {
        if (!headBobEnabled) return;
        if (PlayerMovement.Instance.IsMoving)
        {
            headBobTimer += Time.deltaTime * headBobFrequency;
            headBobOffset = Mathf.Sin(headBobTimer) * headBobAmplitude;
        }
        else
        {
            headBobOffset = Mathf.Lerp(headBobOffset, 0f, Time.deltaTime * headBobSmoothing);
            headBobTimer = 0f; // Reset timer when not moving
        }
    }

    public void AddHorizontalRotation(float delta)
    {
        horizontalRotation += delta;
    }

    public void OnConfigChanged()
    {
        sensitivity = PlayerPrefs.GetFloat(ConfigsAreaInGame.PlayerPrefsSensitivityKey, 0.5f);
    }
}
