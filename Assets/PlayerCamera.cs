using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    public bool cameraEnabled = true;

    [SerializeField] private Transform playerHeadPos;

    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [SerializeField] private InputActionReference mouseDelta;

    [SerializeField] private Transform orientation;

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
        instance = this;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if(!cameraEnabled) return;
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
        transform.position = playerHeadPos.position;
    }

    public void AddHorizontalRotation(float delta)
    {
        horizontalRotation += delta;
    }
}
