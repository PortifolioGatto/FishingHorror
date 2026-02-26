using UnityEngine;

public class EquipmentUnlocker : MonoBehaviour
{
    public void UnlockDifficultyDecrease(float decreaseAmount)
    {
        EquipmentController.Instance.difficultyDecreaseAmount += decreaseAmount;
    }
    public void UnlockBoatSpeed(float multiplier)
    {
        EquipmentController.Instance.boatSpeedMultiplier += multiplier;
    }
    public void UnlockExtraSpace(int extraSpace)
    {
        EquipmentController.Instance.extraSpaceOnBox += extraSpace;
    }
    public void UnlockSonarRange(float rangeIncrease)
    {
        EquipmentController.Instance.sonarRangeIncrease += rangeIncrease;
    }
    public void UnlockTrowableBait()
    {
        EquipmentController.Instance.hasTrowableBait = true;
    }
    public void UnlockFlashlight()
    {
        EquipmentController.Instance.hasFlashlight = true;
    }
    public void UnlockRadio()
    {
        EquipmentController.Instance.hasRadio = true;
    }
}
