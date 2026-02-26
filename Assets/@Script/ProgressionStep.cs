using UnityEngine;
using UnityEngine.Events;

public class ProgressionStep : MonoBehaviour
{
    public RadioDialogue dialogueToPlayOnStart;

    public int timeInHours;
    public int timeInMinutes;

    public UnityEvent onStepActivated;
    public UnityEvent onStepDeactivated;
    public UnityEvent onStepCompleted;

    public GameObject[] enableObjects;
    public GameObject[] disableObjects;

    private bool isActive = false;

    public FishingSpot[] neededToCompleteStep;

    public void SetActive(bool isActive)
    {
        if(this.isActive == isActive)
            return;

        this.isActive = isActive;

        if (isActive)
        {
            onStepActivated.Invoke();

            if (dialogueToPlayOnStart != null)
                RadioManager.Instance.PlayDialogue(dialogueToPlayOnStart);

            if (timeInHours != -1) DayNightCycle.Instance.SetLimitTime(timeInHours, timeInMinutes);

            Debug.Log($"Activating step: {gameObject.name}");

            foreach (var obj in enableObjects)
            {
                if(obj != null)
                    obj.SetActive(true);
            }
        }
        else
        {
            onStepDeactivated.Invoke();
            foreach (var obj in disableObjects)
            {
                if(obj != null)
                    obj.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if(isActive)
        {
            bool allCompleted = true;
            foreach (var spot in neededToCompleteStep)
            {
                if (spot != null)
                {
                    allCompleted = false;
                    break;
                }
            }
            if (allCompleted)
            {
                onStepCompleted.Invoke();
                SetActive(false);
            }
        }
    }
}
