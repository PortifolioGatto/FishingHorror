using DG.Tweening;
using Unity.Hierarchy;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBoatManager : MonoBehaviour
{
    [SerializeField] private Transform drivingPoint;
    [SerializeField] private Blocker playerBlocker;
    [SerializeField] private PlayerFishingSystem fishingSystem;
    [SerializeField] private PlayerCamera playerCamera;

    [SerializeField] private BoatMovement boatMovement;

    [SerializeField] private DayNightCycle dayNightCycle;

    [Space]
    [SerializeField] private float drivingTimeScale = 1f;
    [SerializeField] private float fishingTimeScale = .25f;
    private float lastBoatYaw;

    public bool isDriving = false;
    public bool holdingWheel = false;
    public bool canDrive = false;

    public static PlayerBoatManager Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        dayNightCycle.SetTimeScale(fishingTimeScale);
    }


    private void Update()
    {
        isDriving = boatMovement.Throttle != 0;

        if(holdingWheel)
        {
            float currentBoatYaw = boatMovement.transform.eulerAngles.y;
            float deltaYaw = Mathf.DeltaAngle(lastBoatYaw, currentBoatYaw);
            playerCamera.AddHorizontalRotation(deltaYaw);
            lastBoatYaw = currentBoatYaw;

        }
    }

    public void SetCanDrive(bool canDrive)
    {
        this.canDrive = canDrive;
    }

    public void ToggleDrivingMode()
    {
        if(!canDrive) return;

        if (holdingWheel)
        {
            ExitDrivingMode();
        }
        else
        {
            EnterDrivingMode();
        }
    }
    public void EnterDrivingMode()
    {
        if (!canDrive) return;

        playerBlocker.isBlocking = true;
        holdingWheel = true;

        fishingSystem.SetVisible(false);
        dayNightCycle.SetTimeScale(drivingTimeScale);

        // Alinha o horizontalRotation da câmera ao forward do barco
        playerCamera.AddHorizontalRotation(
            Mathf.DeltaAngle(playerCamera.GetHorizontalRotation(), boatMovement.transform.eulerAngles.y)
        );

        lastBoatYaw = boatMovement.transform.eulerAngles.y;
        playerBlocker.transform.position = drivingPoint.position;

    }

    public void ExitDrivingMode()
    {
        if (!canDrive) return;

        playerBlocker.isBlocking = false;
        playerCamera.cameraEnabled = true;

        holdingWheel = false;

        fishingSystem.SetVisible(true);

        dayNightCycle.SetTimeScale(fishingTimeScale);
    }

    private void ApplyBoatRotationToCamera()
    {
        float currentBoatYaw = boatMovement.transform.eulerAngles.y;

        // Delta de rotação do barco nesse frame
        float deltaYaw = Mathf.DeltaAngle(lastBoatYaw, currentBoatYaw);

        // Aplica o delta no yaw da câmera
        playerCamera.transform.rotation *= Quaternion.Euler(0f, deltaYaw, 0f);

        lastBoatYaw = currentBoatYaw;
    }

}
