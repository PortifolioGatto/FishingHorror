using DG.Tweening;
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

    public class ShakeData
    {
        public float duration;
        public float magnitude;

        public ShakeData(float duration, float magnitude)
        {
            this.duration = duration;
            this.magnitude = magnitude;
        }
    }

    private ShakeData currentShake;

    private Vector3 shakeOffset;

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

    private bool shaking = false;
    private bool shakeEnding = false;

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
        float mouseX = 0f;
        float mouseY = 0f;

        if (cameraEnabled)
        {
            Vector2 mouseDeltaValue = mouseDelta.action.ReadValue<Vector2>();
            mouseX = mouseDeltaValue.x * sensitivity;
            mouseY = mouseDeltaValue.y * sensitivity;
        }


        HandleHeadBob();

        HandleShake();

        

        horizontalRotation += mouseX;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxVerticalAngle, maxVerticalAngle);
        transform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);

        orientation.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }

    private void LateUpdate()
    {
        transform.position = playerHeadPos.position + shakeOffset + (Vector3.up * headBobOffset);
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

    private void HandleShake()
    {
        if(!shaking) return;

        if (currentShake != null)
        {
            shakeOffset = Random.insideUnitSphere * currentShake.magnitude;
            currentShake.duration -= Time.deltaTime;
            if (currentShake.duration <= 0f && !shakeEnding)
            {
                DOTween.To(() => currentShake.magnitude, x => currentShake.magnitude = x, 0f, .5f).SetEase(Ease.OutQuad).onComplete += () =>
                {
                    shakeOffset = Vector3.zero;
                    currentShake = null;
                    shaking = false;
                    shakeEnding = false;
                };
                shakeEnding = true;
            }
        }
    }

    public void AddHorizontalRotation(float delta)
    {
        horizontalRotation += delta;
    }


    public void ShakeCamera(float duration, float magnitude)
    {
        currentShake = new ShakeData(duration, magnitude);
        shakeOffset = Vector3.zero;
        shaking = true;
        shakeEnding = false;
    }

    public void BumpCamera()
    {
        ShakeCamera(.25f, .1f);
    }


    [ContextMenu("Recenter Camera")]
    public void RecenterCamera()
    {
        DOTween.To(() => verticalRotation, x => verticalRotation = x, 0f, .5f).SetEase(Ease.OutQuad);
        //DOTween.To(() => horizontalRotation, x => horizontalRotation = x, Random.Range(0f,360f), .5f).SetEase(Ease.OutQuad);
    }

    public void OnConfigChanged()
    {
        sensitivity = PlayerPrefs.GetFloat(ConfigsAreaInGame.PlayerPrefsSensitivityKey, 0.5f);
    }
}
