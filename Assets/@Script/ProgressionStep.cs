using UnityEngine;
using UnityEngine.Events;

public class ProgressionStep : MonoBehaviour
{
    public RadioDialogue dialogueToPlayOnStart;

    public int timeInHours_newLimit = -1;
    public int timeInMinutes_newLimit;

    [Space]

    public int timeInHours_end;
    public int timeInMinutes_end;

    [Space]

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

            if (timeInHours_newLimit != -1) DayNightCycle.Instance.SetLimitTime(timeInHours_newLimit, timeInMinutes_newLimit);

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

            if (timeInHours_end != -1)
            {

                DayNightCycle.Instance.GetTime(out int hours, out int minutes);

                Debug.Log($"Checking time for step completion: {hours}:{minutes} / {timeInHours_end}:{timeInMinutes_end}");

                if ((hours == timeInHours_end && minutes >= timeInMinutes_end))
                {
                    onStepCompleted.Invoke();
                    SetActive(false);
                    return;
                }
            }


            if (neededToCompleteStep.Length == 0)
            {
                return;
            }
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
