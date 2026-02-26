using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FishingBarUI : MonoBehaviour
{
    [SerializeField] private bool isActive;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float catchChance = 0.1f;

    [SerializeField] private RectTransform canFishPointTransform;
    [SerializeField] private Image canFishAreaImage;
    [SerializeField] private RectTransform fishCursor;
    private float time;
    private float width;
    private float timeDirection;
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;


    public RectTransform RectTransform => rectTransform;
    public CanvasGroup CanvasGroup => canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        timeDirection = Random.value < 0.5f ? -1f : 1f; // Randomize initial direction
    }

    private void Update()
    {
        if (!isActive) return;

        fishCursor.anchoredPosition = new Vector2(Mathf.Sin(time) * ((width / 2) * .95f), 0);
        
        time += Time.deltaTime * speed * timeDirection; // Adjust speed as needed
    }

    public void Initialize(FishingMinigameData fishingData)
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        width = rectTransform.rect.width;
        speed = fishingData.fishingSpeed;
        catchChance = fishingData.fishingDifficulty;

        float fishPercent = catchChance;
        float placePercent = Random.Range(0 + catchChance, 1f - catchChance);

        canFishPointTransform.anchoredPosition = new Vector2(placePercent * width - width / 2, 0);
        canFishPointTransform.sizeDelta = new Vector2(fishPercent * width, canFishPointTransform.sizeDelta.y);

        isActive = true;
    }

    public bool TryCatchFish()
    {
        if (!isActive) return false;
        float fishPosition = fishCursor.anchoredPosition.x + width / 2;
        float canFishStart = canFishPointTransform.anchoredPosition.x - canFishPointTransform.sizeDelta.x / 2 + width / 2;
        float canFishEnd = canFishPointTransform.anchoredPosition.x + canFishPointTransform.sizeDelta.x / 2 + width / 2;
        bool caughtFish = fishPosition >= canFishStart && fishPosition <= canFishEnd;
        if (caughtFish)
        {
            Debug.Log("Caught a fish!");
            isActive = false; // Deactivate after trying to catch
            // Handle successful catch (e.g., update inventory, play sound, etc.)
        }
        else
        {
            Debug.Log("Missed the fish.");
            // Handle failed catch (e.g., play sound, show feedback, etc.)
        }
        
        return caughtFish;
    }

    public void BlinkWrong()
    {
        if(canFishAreaImage == null) return;

        Color originalColor = canFishAreaImage.color;
        canFishAreaImage.DOColor(Color.red, 0.2f).SetLoops(2, LoopType.Yoyo).onComplete += () =>
        {
            if(canFishAreaImage != null)
                canFishAreaImage.color = originalColor;
        };

        canFishAreaImage.transform.DOShakePosition(0.4f, 10f, 20).SetEase(Ease.InOutSine);

    }

    public void BlinkRight()
    {
        if(canFishAreaImage == null) return;

        Color originalColor = canFishAreaImage.color;
        canFishAreaImage.DOColor((Color.green + Color.white / 2f), 0.2f).SetLoops(2, LoopType.Yoyo).onComplete += () =>
        {
            if(canFishAreaImage != null)
                canFishAreaImage.color = originalColor;
        };

        canFishAreaImage.transform.DOScale(1.1f, 0.2f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine).onComplete += () =>
        {
            if(canFishAreaImage != null)
                canFishAreaImage.transform.localScale = Vector3.one;
        };
    }

    public void KillAllTweens()
    {
        canFishAreaImage.DOKill();
        canFishAreaImage.transform.DOKill();
    }
}
