using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class FishIceBox : MonoBehaviour, IInteractable
{

    public static FishIceBox Instance;
    public bool isHovering { get; set; } = false;
    public bool canInteract { get; set; } = true;

    [SerializeField] private TextMeshPro quantityText;
    [Space]
    [SerializeField] private Transform iceBoxLid;
    [SerializeField] private float lidOpenAngle = 90f;
    [SerializeField] private float lidClosedAngle = 0f;

    [SerializeField] private string interactionText = "Ice Box";
    [SerializeField] private string storeFishInteractionText = "Store fish in ice box";

    private bool waitingTween;
    private bool isOpen;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!waitingTween && !isOpen && isHovering) OpenChestAnimation();
        if (!waitingTween && isOpen && !isHovering) CloseChestAnimation();

        UpdateQuantityText();
    }

    public void Interact()
    {
        if(PlayerFishingSystem.Instance.IsHoldingFish())
        {
            PlayerFishingSystem.Instance.StoreFishInIceBox();
        }
    }

    public void UpdateQuantityText()
    {
        quantityText.text = PlayerFishingSystem.Instance.CurrentFishInIceBox;
    }

    public void OnHover()
    {
        isHovering = true;

        
    }
    public void OnHoverExit()
    {
        isHovering = false;


        
    }


    private void OpenChestAnimation()
    {
        
        waitingTween = true;
        iceBoxLid.DOLocalRotate(new Vector3(lidOpenAngle, 0f, 0f), 0.5f).SetEase(Ease.OutBack).onComplete += () =>
        {
            waitingTween = false;
            isOpen = true;
        };

        quantityText.color = new Color(quantityText.color.r, quantityText.color.g, quantityText.color.b, 0f);
        quantityText.DOFade(1f, 0.5f).SetEase(Ease.OutQuad);

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

        quantityText.DOFade(0f, 0.5f).SetEase(Ease.InQuad);


    }

    public string GetInteractionText()
    {
        if (PlayerFishingSystem.Instance.IsHoldingFish())
        {
            return storeFishInteractionText;
        }

        return interactionText;
    }
}