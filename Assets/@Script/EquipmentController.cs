using UnityEngine;

public class EquipmentController : MonoBehaviour
{
    public static EquipmentController Instance { get; private set; }

    public float boatSpeedMultiplier = 1f;
    public float difficultyDecreaseAmount = 0f;
    public int extraSpaceOnBox = 0;

    public float sonarRangeIncrease = 0f;

    public bool hasTrowableBait = false;
    public bool hasFlashlight = false;
    public bool hasRadio = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

}
