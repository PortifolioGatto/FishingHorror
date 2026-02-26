
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishingMinigameUI : MonoBehaviour
{
    [SerializeField] private RectTransform fishingBarUIParent;
    [SerializeField] private FishingBarUI fishingBarUIReference;
    private List<FishingBarUI> activeFishingBars = new List<FishingBarUI>();
    private FishingBarUI currentActiveFishingBar;

    [Space]
    [Header("Transform Config")]
    [SerializeField] private float heightSpacing = 50f;
    [SerializeField] private float timeTweenDuration = 0.5f;
    [SerializeField] private Ease tweenEase = Ease.OutQuad;

    public static FishingMinigameUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public FishingBarUI ShowFishingBarUI(FishingMinigameData fishingData)
    {
        if (fishingBarUIReference == null)
        {
            Debug.LogError("FishingBarUI reference is not set in the inspector.");
            return null;
        }
        // Instantiate the fishing bar UI and set it as a child of the specified parent
        currentActiveFishingBar = Instantiate(fishingBarUIReference, fishingBarUIParent);
        currentActiveFishingBar.gameObject.SetActive(true);

        currentActiveFishingBar.Initialize(fishingData);

        currentActiveFishingBar.RectTransform.anchoredPosition = new Vector2(fishingBarUIParent.rect.width / 2f, 0);

        MoveActivesOneStepAbove();
        MoveNewFishingBar(currentActiveFishingBar);

        activeFishingBars.Add(currentActiveFishingBar);

        return currentActiveFishingBar;
    }

    public void MoveNewFishingBar(FishingBarUI newFishingBar)
    {
        newFishingBar.CanvasGroup.alpha = 0f;
        newFishingBar.RectTransform.anchoredPosition = new Vector2(0, -heightSpacing);

        newFishingBar.CanvasGroup.DOFade(1f, timeTweenDuration).SetEase(tweenEase);

        newFishingBar.RectTransform.DOAnchorPosY(0, timeTweenDuration).SetEase(tweenEase);
    }

    public void MoveActivesOneStepAbove()
    {

        for (int i = 0; i < activeFishingBars.Count; i++)
        {
            RectTransform rectTransform = activeFishingBars[i].GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                float targetY = heightSpacing * (activeFishingBars.Count - i);
                rectTransform.DOAnchorPosY(targetY, timeTweenDuration).SetEase(tweenEase);
            }
        }
    }
    
    public bool TryCatchFishInput()
    {
        if (currentActiveFishingBar != null)
        {
            return currentActiveFishingBar.TryCatchFish();
        }

        return false;
    }

    public void ClearUI()
    {
        currentActiveFishingBar = null;

        foreach (var fishingBar in activeFishingBars)
        {
            fishingBar.KillAllTweens();
            Destroy(fishingBar.gameObject);
        }

        activeFishingBars.Clear();
    }
    
    public void BlinkFishingBarWrong()
    {
        currentActiveFishingBar?.BlinkWrong();
    }

    public void BlinkFishingBarRight()
    {
        currentActiveFishingBar?.BlinkRight();
    }
}

public struct FishingMinigameData
{
    public float fishingDifficulty;
    public float fishingSpeed;

    public FishingMinigameData(float difficulty, float speed)
    {
        fishingDifficulty = difficulty;
        fishingSpeed = speed;
    }
}
