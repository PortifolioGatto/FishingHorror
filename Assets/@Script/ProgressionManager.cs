using UnityEngine;

public class ProgressionManager : MonoBehaviour
{
    [SerializeField]
    public ProgressionStep[] steps;

    private int currentStep = 0;


    private void Start()
    {
        InitializeStep();

        ShowCurrentStep();
    }

    private void InitializeStep()
    {
        currentStep = 0;
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i].SetActive(false);
        }
    }

    public void ShowCurrentStep()
    {
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i].SetActive(i == currentStep);
        }
    }

    public void NextStep()
    {
        if (currentStep < steps.Length - 1)
        {
            currentStep++;
            ShowCurrentStep();
        }
    }
    
    public void PreviousStep()
    {
        if (currentStep > 0)
        {
            currentStep--;
            ShowCurrentStep();
        }
    }
}
