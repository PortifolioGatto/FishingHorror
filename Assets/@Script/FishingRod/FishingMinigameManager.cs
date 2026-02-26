using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishingMinigameManager : MonoBehaviour
{
    [SerializeField] private FishingMinigameUI uiManager;

    [Space]

    private bool catchFishInputPressed;

    [Header("Input System")]
    [SerializeField] private InputActionReference tryCatchInput;

    private bool isMinigameActive = false;

    private Action<FishData> onFishingSuccess;
    private Action onFishingFailure;

    public static FishingMinigameManager Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (tryCatchInput != null)
        {
            tryCatchInput.action.performed += OnTryCatchPerformed;
        }
    }

    public void StartFishingMinigame(FishData fishData, Action<FishData> onFishingSuccess, Action onFailed)
    {
        if(isMinigameActive)
        {
            Debug.LogWarning("Fishing minigame is already active.");
            return;
        }

        this.onFishingSuccess = onFishingSuccess;
        this.onFishingFailure = onFailed;

        StartCoroutine(EHandleFishingMinigame(fishData));
    }

    private IEnumerator EHandleFishingMinigame(FishData fishData)
    {
        AudioSource reelSource = AudioManager.Instance.PlaySFXLoop("reel", Camera.main.transform.position + Vector3.down * 1f, 0.5f);

        isMinigameActive = true;

        float difficulty = fishData.fishBaseDifficulty - UnityEngine.Random.Range(-0.1f, 0.1f);
        float speed = UnityEngine.Random.Range(1.75f, 2.5f);
        
        int amountOfTriesToSuccessfullyCatchFish = UnityEngine.Random.Range(fishData.minTriesToCatch, fishData.maxTriesToCatch);

        float difficultyIncreasePerSuccessfulCatch = (fishData.fishBaseDifficulty / 2f) / amountOfTriesToSuccessfullyCatchFish;
        float speedIncreasePerSuccessfulCatch = 1.5f / amountOfTriesToSuccessfullyCatchFish;



        int currentTryCount = 0;

        int maxFailsAllowed = 5;
        int currentFailCount = 0;

        while (currentTryCount < amountOfTriesToSuccessfullyCatchFish)
        {
            bool isSuccessfulCatch = false;

            float difficultyForThisTry = Mathf.Max(difficulty - currentTryCount * difficultyIncreasePerSuccessfulCatch, 0.05f);
            float speedForThisTry = speed + currentTryCount * speedIncreasePerSuccessfulCatch;

            FishingMinigameData fishingData = new FishingMinigameData
            {
                fishingDifficulty = difficultyForThisTry,
                fishingSpeed = speedForThisTry
            };


            uiManager.ShowFishingBarUI(fishingData);

            while (!isSuccessfulCatch)
            {
                if (!catchFishInputPressed)
                {
                    yield return null;
                    continue;
                }

                catchFishInputPressed = false;

                bool result = uiManager.TryCatchFishInput();

                if(result)
                {
                    reelSource.Pause();

                    StartCoroutine(WaitToEvent(() =>
                    {
                        if (isMinigameActive)
                            reelSource.UnPause();
                    }, 0.15f));

                    uiManager.BlinkFishingBarRight();
                    isSuccessfulCatch = true;
                }
                else
                {
                    reelSource.Pause();
                    
                    StartCoroutine(WaitToEvent(() =>
                    {
                        if (isMinigameActive)
                            reelSource.UnPause();
                    }, 0.15f));

                    uiManager.BlinkFishingBarWrong();

                    if (currentFailCount >= maxFailsAllowed)
                    {
                        reelSource.Stop();
                        Destroy(reelSource.gameObject);
                        Debug.Log("Too many failed attempts. Ending minigame.");

                        onFishingFailure?.Invoke();

                        isMinigameActive = false;

                        uiManager.ClearUI();

                        yield break; // Exit the coroutine
                    }

                    currentFailCount++;
                }

                yield return null; // Wait for the next frame
                
            }

            currentTryCount++;
        }

        Debug.Log("Fish caught successfully!");
        onFishingSuccess?.Invoke(fishData);

        uiManager.ClearUI();

        reelSource.Stop();
        Destroy(reelSource.gameObject);

        isMinigameActive = false;
    }

    private IEnumerator WaitToEvent(System.Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    private void OnTryCatchPerformed(InputAction.CallbackContext context)
    {
        if (!isMinigameActive)
            return;

        catchFishInputPressed = true;
    }
}
