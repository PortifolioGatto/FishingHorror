using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Localization;

public class FishIceBox : MonoBehaviour, IInteractable
{

    public static FishIceBox Instance;
    public bool isHovering { get; set; } = false;
    public bool canInteract { get; set; } = true;

    [SerializeField] private TextMeshPro moneyText;
    [Space]
    [SerializeField] private Transform iceBoxLid;
    [SerializeField] private float lidOpenAngle = 90f;
    [SerializeField] private float lidClosedAngle = 0f;

    [SerializeField] private LocalizedString interactionText;
    [SerializeField] private LocalizedString storeFishInteractionText;

    private bool waitingTween;
    private bool isOpen;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateMoneyText();
    }

    private void Update()
    {
        if (!waitingTween && !isOpen && isHovering) OpenChestAnimation();
        if (!waitingTween && isOpen && !isHovering) CloseChestAnimation();
    }

    public void Interact()
    {
        if(PlayerFishingSystem.Instance.IsHoldingFish())
        {
            PlayerFishingSystem.Instance.StoreFishInIceBox();
            UpdateMoneyText();
        }
    }

    public void UpdateMoneyText()
    {
        moneyText.text = PlayerFishingSystem.Instance.CurrentMoneyInBox;
    }

    public void OnHover()
    {
        isHovering = true;
        moneyText.gameObject.SetActive(true);

        
    }
    public void OnHoverExit()
    {
        isHovering = false;
        moneyText.gameObject.SetActive(false);

        
    }


    private void OpenChestAnimation()
    {
        
        waitingTween = true;
        iceBoxLid.DOLocalRotate(new Vector3(lidOpenAngle, 0f, 0f), 0.5f).SetEase(Ease.OutBack).onComplete += () =>
        {
            waitingTween = false;
            isOpen = true;
        };

        moneyText.color = new Color(moneyText.color.r, moneyText.color.g, moneyText.color.b, 0f);
        moneyText.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);

        AudioManager.Instance.PlaySFX("chestopen", transform.position, 0.5f);
    }

    private void CloseChestAnimation()
    {
        
        waitingTween = true;
        iceBoxLid.DOLocalRotate(new Vector3(lidClosedAngle, 0f, 0f), 0.5f).SetEase(Ease.InBack).onComplete += () =>
        {
            AudioManager.Instance.PlaySFX("chestclose", transform.position, 0.5f);
            waitingTween = false;
            isOpen = false;
        };

        moneyText.DOFade(0f, 0.5f).SetEase(Ease.InQuad);


    }

    public string GetInteractionText()
    {
        if (PlayerFishingSystem.Instance.IsHoldingFish())
        {
            return storeFishInteractionText.GetLocalizedString();
        }

        return interactionText.GetLocalizedString();
    }
}